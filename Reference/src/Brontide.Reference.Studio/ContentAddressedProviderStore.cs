using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public readonly record struct ProviderArtifactSetId
{
    private ProviderArtifactSetId(string value) => Value = value;

    public string Value { get; }

    public static ProviderArtifactSetId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!ContentAddressedProviderStore.IsDigest(value))
        {
            throw new ArgumentException("A provider artifact set identity must be an uppercase SHA-256 digest.", nameof(value));
        }

        return new(value);
    }

    public override string ToString() => Value;
}

public sealed record ProviderArtifactFile(string RelativePath, string Sha256);

public sealed record ProviderArtifactSet(
    ProviderArtifactSetId Identity,
    string SourceRoot,
    IReadOnlyList<ProviderArtifactFile> Files,
    string ExecutablePath,
    IReadOnlyList<string> Arguments);

public static class ProviderArtifactSetIdentity
{
    public static ProviderArtifactSetId Compute(
        IEnumerable<ProviderArtifactFile> files,
        string executablePath,
        IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, "CBI32");
        var orderedFiles = files.OrderBy(file => file.RelativePath, StringComparer.Ordinal).ToArray();
        Append(hash, orderedFiles.Length);
        foreach (var file in orderedFiles)
        {
            Append(hash, file.RelativePath);
            Append(hash, file.Sha256);
        }

        Append(hash, executablePath);
        var argumentArray = arguments.ToArray();
        Append(hash, argumentArray.Length);
        foreach (var argument in argumentArray)
        {
            Append(hash, argument);
        }

        return ProviderArtifactSetId.Create(Convert.ToHexString(hash.GetHashAndReset()));
    }

    private static void Append(IncrementalHash hash, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        hash.AppendData(bytes);
    }

    private static void Append(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Append(hash, bytes.Length);
        hash.AppendData(bytes);
    }
}

public sealed record ProviderArtifactSetFailure(string Code, string Reason);

public sealed record StagedProviderArtifactSet(
    ProviderArtifactSetId Identity,
    string RootPath,
    IReadOnlyList<ProviderArtifactFile> Files,
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    bool Reused);

public sealed record ProviderArtifactStagingResult
{
    private ProviderArtifactStagingResult(StagedProviderArtifactSet? staged, ProviderArtifactSetFailure? failure)
    {
        Staged = staged;
        Failure = failure;
    }

    public StagedProviderArtifactSet? Staged { get; }

    public ProviderArtifactSetFailure? Failure { get; }

    public bool IsStaged => Staged is not null;

    public string Code => IsStaged ? "staged" : Failure!.Code;

    internal static ProviderArtifactStagingResult Success(StagedProviderArtifactSet staged) => new(staged, null);

    internal static ProviderArtifactStagingResult Refused(string code, string reason) => new(null, new(code, reason));
}

public sealed record ProviderArtifactRemoval(string Code, bool Removed);

public sealed record StagedProviderActivation
{
    private StagedProviderActivation(StagedProviderProcess? owner, ProviderArtifactSetFailure? failure)
    {
        Owner = owner;
        Failure = failure;
    }

    public StagedProviderProcess? Owner { get; }

    public ProviderArtifactSetFailure? Failure { get; }

    public bool IsLaunched => Owner is not null;

    internal static StagedProviderActivation Launched(StagedProviderProcess owner) => new(owner, null);

    internal static StagedProviderActivation Refused(string code, string reason) => new(null, new(code, reason));
}

public sealed class StagedProviderProcess : IAsyncDisposable
{
    private readonly LocalProviderProcess _inner;
    private readonly Action _release;
    private bool _disposed;

    internal StagedProviderProcess(
        LocalProviderProcess inner,
        StagedProviderArtifactSet stagedArtifacts,
        Action release)
    {
        _inner = inner;
        StagedArtifacts = stagedArtifacts;
        _release = release;
    }

    internal StagedProviderArtifactSet StagedArtifacts { get; }

    public Experimental.Binding.Portable.IPortableProviderConversation Conversation => _inner.Conversation;

    public string Isolation => _inner.Isolation;

    public bool HasExited => _inner.HasExited;

    public Task<bool> WaitForExitAsync(TimeSpan timeout) => _inner.WaitForExitAsync(timeout);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _inner.DisposeAsync().ConfigureAwait(false);
        _release();
        _disposed = true;
    }
}

public sealed class ContentAddressedProviderStore
{
    private readonly object _sync = new();
    private readonly Dictionary<ProviderArtifactSetId, int> _leases = [];
    private readonly string _rootPath;

    public ContentAddressedProviderStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    internal static bool IsDigest(string? value) =>
        value is not null
        && value.Length == 64
        && value.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F');

    public ProviderArtifactStagingResult Stage(ProviderArtifactSet declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        lock (_sync)
        {
            if (declaration.Files is null || declaration.Arguments is null)
            {
                return ProviderArtifactStagingResult.Refused(
                    "artifact-set-invalid",
                    "The provider artifact set manifest is not canonical and complete.");
            }

            declaration = declaration with
            {
                Files = Array.AsReadOnly(declaration.Files.ToArray()),
                Arguments = Array.AsReadOnly(declaration.Arguments.ToArray()),
            };
            var validation = Validate(declaration);
            if (validation is not null)
            {
                return validation;
            }

            var finalRoot = Path.Combine(_rootPath, declaration.Identity.Value);
            if (Directory.Exists(finalRoot))
            {
                return VerifyExisting(declaration, finalRoot, reused: true);
            }

            var temporaryRoot = Path.Combine(_rootPath, $".stage-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryRoot);
            try
            {
                foreach (var file in declaration.Files.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
                {
                    var source = CombineRelative(declaration.SourceRoot, file.RelativePath);
                    if (!File.Exists(source) || (File.GetAttributes(source) & FileAttributes.Directory) != 0)
                    {
                        DeleteTree(temporaryRoot);
                        return ProviderArtifactStagingResult.Refused(
                            "artifact-set-unavailable",
                            $"Artifact set member '{file.RelativePath}' is unavailable.");
                    }

                    var destination = CombineRelative(temporaryRoot, file.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    if (!CopyVerified(source, destination, file.Sha256))
                    {
                        DeleteTree(temporaryRoot);
                        return ProviderArtifactStagingResult.Refused(
                            "artifact-set-integrity-failed",
                            $"Artifact set member '{file.RelativePath}' does not match its declared digest.");
                    }

                    File.SetAttributes(destination, File.GetAttributes(destination) | FileAttributes.ReadOnly);
                }

                MoveDirectory(temporaryRoot, finalRoot);
                return ProviderArtifactStagingResult.Success(Snapshot(declaration, finalRoot, reused: false));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                DeleteTree(temporaryRoot);
                return ProviderArtifactStagingResult.Refused(
                    "artifact-set-stage-failed",
                    $"Artifact set '{declaration.Identity}' could not be staged transactionally.");
            }
        }
    }

    public StagedProviderActivation Activate(
        StagedProviderArtifactSet staged,
        IReadOnlyList<string> allowedArguments)
    {
        ArgumentNullException.ThrowIfNull(staged);
        ArgumentNullException.ThrowIfNull(allowedArguments);
        lock (_sync)
        {
            var declaration = new ProviderArtifactSet(
                staged.Identity,
                staged.RootPath,
                staged.Files,
                staged.ExecutablePath,
                staged.Arguments);
            var verification = VerifyExisting(declaration, ExpectedRoot(staged.Identity), staged.Reused);
            if (!verification.IsStaged)
            {
                return StagedProviderActivation.Refused(verification.Code, verification.Failure!.Reason);
            }

            var executable = staged.Files.Single(file => file.RelativePath == staged.ExecutablePath);
            var activation = LocalProviderArtifactActivator.AcquireAndLaunch(
                new LocalProviderArtifact(
                    staged.Identity.Value,
                    CombineRelative(staged.RootPath, staged.ExecutablePath),
                    executable.Sha256,
                    staged.Arguments),
                new LocalProviderLaunchPolicy(staged.RootPath, allowedArguments));
            if (!activation.IsLaunched)
            {
                return StagedProviderActivation.Refused(activation.Failure!.Code, activation.Failure.Reason);
            }

            _leases[staged.Identity] = _leases.GetValueOrDefault(staged.Identity) + 1;
            return StagedProviderActivation.Launched(
                new StagedProviderProcess(activation.Owner!, verification.Staged!, () => Release(staged.Identity)));
        }
    }

    public ProviderArtifactRemoval Remove(ProviderArtifactSetId identity)
    {
        lock (_sync)
        {
            if (_leases.GetValueOrDefault(identity) > 0)
            {
                return new("artifact-set-in-use", false);
            }

            var path = ExpectedRoot(identity);
            if (!Directory.Exists(path))
            {
                return new("artifact-set-not-staged", false);
            }

            try
            {
                DeleteTree(path);
                return new("removed", true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return new("artifact-set-removal-failed", false);
            }
        }
    }

    private ProviderArtifactStagingResult? Validate(ProviderArtifactSet declaration)
    {
        if (declaration.Files is null
            || declaration.Arguments is null
            || declaration.Files.Count == 0
            || declaration.Arguments.Any(string.IsNullOrEmpty)
            || !IsSafeRelativePath(declaration.ExecutablePath)
            || declaration.Files.Any(file =>
                file is null || !IsSafeRelativePath(file.RelativePath) || !IsDigest(file.Sha256))
            || declaration.Files.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count()
                != declaration.Files.Count
            || !declaration.Files.Any(file => file.RelativePath == declaration.ExecutablePath)
            || ProviderArtifactSetIdentity.Compute(
                declaration.Files,
                declaration.ExecutablePath,
                declaration.Arguments) != declaration.Identity)
        {
            return ProviderArtifactStagingResult.Refused(
                "artifact-set-invalid",
                "The provider artifact set manifest is not canonical and complete.");
        }

        return null;
    }

    private ProviderArtifactStagingResult VerifyExisting(
        ProviderArtifactSet declaration,
        string root,
        bool reused)
    {
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!string.Equals(Path.GetFullPath(root), ExpectedRoot(declaration.Identity), pathComparison)
            || !Directory.Exists(root))
        {
            return ProviderArtifactStagingResult.Refused(
                "staged-artifact-integrity-failed",
                "The staged artifact set is absent or does not belong to its content identity.");
        }

        try
        {
            var actualPaths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var declaredPaths = declaration.Files.Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (!actualPaths.SequenceEqual(declaredPaths, StringComparer.Ordinal)
                || declaration.Files.Any(file =>
                    !string.Equals(Digest(CombineRelative(root, file.RelativePath)), file.Sha256, StringComparison.Ordinal)))
            {
                return ProviderArtifactStagingResult.Refused(
                    "staged-artifact-integrity-failed",
                    "The existing staged artifact set does not match its content identity.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ProviderArtifactStagingResult.Refused(
                "staged-artifact-integrity-failed",
                "The existing staged artifact set could not be verified.");
        }

        return ProviderArtifactStagingResult.Success(Snapshot(declaration, root, reused));
    }

    private static StagedProviderArtifactSet Snapshot(ProviderArtifactSet declaration, string root, bool reused) =>
        new(
            declaration.Identity,
            root,
            Array.AsReadOnly(declaration.Files.Select(file => file with { }).ToArray()),
            declaration.ExecutablePath,
            Array.AsReadOnly(declaration.Arguments.ToArray()),
            reused);

    private void Release(ProviderArtifactSetId identity)
    {
        lock (_sync)
        {
            var count = _leases.GetValueOrDefault(identity);
            if (count <= 1)
            {
                _leases.Remove(identity);
            }
            else
            {
                _leases[identity] = count - 1;
            }
        }
    }

    private string ExpectedRoot(ProviderArtifactSetId identity) => Path.Combine(_rootPath, identity.Value);

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path) || path.Contains('\\'))
        {
            return false;
        }

        var segments = path.Split('/');
        return segments.All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static string CombineRelative(string root, string relative) =>
        Path.GetFullPath(Path.Combine(Path.GetFullPath(root), relative.Replace('/', Path.DirectorySeparatorChar)));

    private static string Digest(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool CopyVerified(string source, string destination, string expectedDigest)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.Copy(source, destination, overwrite: false);
                var matches = string.Equals(Digest(destination), expectedDigest, StringComparison.Ordinal);
                if (matches)
                {
                    File.SetAttributes(destination, File.GetAttributes(destination) | FileAttributes.ReadOnly);
                }

                return matches;
            }
            catch (Exception exception) when (
                attempt < 4 && exception is IOException or UnauthorizedAccessException)
            {
                if (File.Exists(destination))
                {
                    File.SetAttributes(destination, FileAttributes.Normal);
                    File.Delete(destination);
                }

                Thread.Sleep(25);
            }
        }
    }

    private static void MoveDirectory(string source, string destination)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Move(source, destination);
                return;
            }
            catch (Exception exception) when (
                attempt < 4 && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(25);
            }
        }
    }

    private static void DeleteTree(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception exception) when (
                attempt < 4 && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(25);
            }
        }
    }
}
