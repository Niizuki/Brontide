using Brontide.Reference.Experimental.Binding.Portable;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Brontide.Reference.Studio;

public sealed record LocalProviderArtifact(
    string Identity,
    string SourcePath,
    string Sha256,
    IReadOnlyList<string> Arguments);

public sealed record LocalProviderLaunchPolicy(
    string AllowedRoot,
    IReadOnlyList<string> AllowedArguments);

public sealed record LocalProviderArtifactFailure(string Code, string Reason);

public sealed record LocalProviderActivation
{
    private LocalProviderActivation(LocalProviderProcess? owner, LocalProviderArtifactFailure? failure)
    {
        Owner = owner;
        Failure = failure;
    }

    public LocalProviderProcess? Owner { get; }

    public LocalProviderArtifactFailure? Failure { get; }

    public bool IsLaunched => Owner is not null;

    internal static LocalProviderActivation Launched(LocalProviderProcess owner) => new(owner, null);

    internal static LocalProviderActivation Refused(string code, string reason) =>
        new(null, new(code, reason));
}

public sealed class LocalProviderProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly PortableProcessConversation _conversation;
    private bool _disposed;
    private bool _exited;

    internal LocalProviderProcess(Process process, LocalProviderArtifact artifact, string digest)
    {
        _process = process;
        Artifact = artifact;
        VerifiedSha256 = digest;
        _conversation = new PortableProcessConversation(
            new PortableStreamDuplex(
                process.StandardOutput.BaseStream,
                process.StandardInput.BaseStream,
                PortableLimits.Declared,
                ownsStreams: false),
            PortableLimits.Declared);
    }

    public LocalProviderArtifact Artifact { get; }

    public string VerifiedSha256 { get; }

    public string Isolation => "dedicated-process";

    public bool UsesShell => _process.StartInfo.UseShellExecute;

    public bool RedirectsStandardStreams =>
        _process.StartInfo.RedirectStandardInput
        && _process.StartInfo.RedirectStandardOutput
        && _process.StartInfo.RedirectStandardError;

    public IPortableProviderConversation Conversation => _conversation;

    public bool HasExited => _exited || (!_disposed && _process.HasExited);

    public async Task<bool> WaitForExitAsync(TimeSpan timeout)
    {
        if (_process.HasExited)
        {
            return true;
        }

        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await _process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _conversation.DisposeAsync().ConfigureAwait(false);
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }

        await _process.WaitForExitAsync().ConfigureAwait(false);
        _exited = true;
        _process.Dispose();
        _disposed = true;
    }
}

public static class LocalProviderArtifactActivator
{
    public static LocalProviderActivation AcquireAndLaunch(
        LocalProviderArtifact artifact,
        LocalProviderLaunchPolicy policy) =>
        AcquireAndLaunch(artifact, policy, null);

    internal static LocalProviderActivation AcquireAndLaunch(
        LocalProviderArtifact artifact,
        LocalProviderLaunchPolicy policy,
        IReadOnlyDictionary<string, string>? environment)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(artifact.Arguments);
        ArgumentNullException.ThrowIfNull(policy.AllowedArguments);

        var sourcePath = Path.GetFullPath(artifact.SourcePath);
        if (!File.Exists(sourcePath) || (File.GetAttributes(sourcePath) & FileAttributes.Directory) != 0)
        {
            return LocalProviderActivation.Refused(
                "artifact-unavailable",
                $"Artifact '{artifact.Identity}' is not an available regular file.");
        }

        string digest;
        try
        {
            using var stream = File.OpenRead(sourcePath);
            digest = Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return LocalProviderActivation.Refused(
                "artifact-unavailable",
                $"Artifact '{artifact.Identity}' could not be read.");
        }

        if (!string.Equals(digest, artifact.Sha256, StringComparison.Ordinal))
        {
            return LocalProviderActivation.Refused(
                "artifact-integrity-failed",
                $"Artifact '{artifact.Identity}' does not match its expected SHA-256 digest.");
        }

        var allowedRoot = Path.GetFullPath(policy.AllowedRoot);
        var relative = Path.GetRelativePath(allowedRoot, sourcePath);
        var outsideRoot = Path.IsPathRooted(relative)
            || relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
        if (outsideRoot
            || artifact.Arguments.Any(string.IsNullOrEmpty)
            || policy.AllowedArguments.Any(string.IsNullOrEmpty)
            || !artifact.Arguments.SequenceEqual(policy.AllowedArguments, StringComparer.Ordinal))
        {
            return LocalProviderActivation.Refused(
                "launch-policy-refused",
                $"Launch policy refused artifact '{artifact.Identity}'.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = sourcePath,
            WorkingDirectory = allowedRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in artifact.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        if (environment is not null)
        {
            foreach (var item in environment)
                startInfo.Environment[item.Key] = item.Value;
        }

        var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                process.Dispose();
                return LocalProviderActivation.Refused(
                    "provider-process-start-failed",
                    $"Artifact '{artifact.Identity}' did not start.");
            }

            return LocalProviderActivation.Launched(new LocalProviderProcess(process, artifact, digest));
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            return LocalProviderActivation.Refused(
                "provider-process-start-failed",
                $"Artifact '{artifact.Identity}' could not be started.");
        }
    }
}
