namespace Brontide.Reference.Studio;

public readonly record struct ProviderArtifactSourceId
{
    private ProviderArtifactSourceId(string value) => Value = value;

    public string Value { get; }

    public static ProviderArtifactSourceId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public override string ToString() => Value;
}

public sealed record ProviderArtifactAcquisitionFile(string RelativePath, string Sha256, long Length);

public sealed record ProviderArtifactAcquisitionRequest(
    ProviderArtifactSourceId ExpectedSource,
    ProviderArtifactSetId Identity,
    IReadOnlyList<ProviderArtifactAcquisitionFile> Files,
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    long MaxTotalBytes);

public interface IProviderArtifactSource
{
    ProviderArtifactSourceId Identity { get; }

    Stream? OpenRead(string relativePath);
}

public sealed record ProviderArtifactAcquisitionResult
{
    private ProviderArtifactAcquisitionResult(
        ProviderArtifactSourceId sourceIdentity,
        string transportCode,
        string admissionCode,
        StagedProviderArtifactSet? staged)
    {
        SourceIdentity = sourceIdentity;
        TransportCode = transportCode;
        AdmissionCode = admissionCode;
        Staged = staged;
    }

    public ProviderArtifactSourceId SourceIdentity { get; }

    public string TransportCode { get; }

    public string PublisherEvidenceCode => "publisher-evidence-not-evaluated";

    public string AdmissionCode { get; }

    public StagedProviderArtifactSet? Staged { get; }

    public bool IsStaged => Staged is not null;

    internal static ProviderArtifactAcquisitionResult TransportRefused(
        ProviderArtifactSourceId sourceIdentity,
        string code) => new(sourceIdentity, code, "admission-not-attempted", null);

    internal static ProviderArtifactAcquisitionResult Admitted(
        ProviderArtifactSourceId sourceIdentity,
        ProviderArtifactStagingResult staging) =>
        new(sourceIdentity, "transport-completed", staging.Code, staging.Staged);
}

public sealed class ProviderArtifactAcquirer
{
    private readonly object _sync = new();
    private readonly ContentAddressedProviderStore _store;
    private readonly string _transactionRoot;

    public ProviderArtifactAcquirer(ContentAddressedProviderStore store, string transactionRoot)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRoot);
        _store = store;
        _transactionRoot = Path.GetFullPath(transactionRoot);
        Directory.CreateDirectory(_transactionRoot);
    }

    public ProviderArtifactAcquisitionResult Acquire(
        ProviderArtifactAcquisitionRequest request,
        IProviderArtifactSource source)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(source);
        lock (_sync)
        {
            if (request.Files is null || request.Arguments is null)
            {
                return ProviderArtifactAcquisitionResult.TransportRefused(
                    request.ExpectedSource,
                    "acquisition-invalid");
            }

            request = request with
            {
                Files = Array.AsReadOnly(request.Files.ToArray()),
                Arguments = Array.AsReadOnly(request.Arguments.ToArray()),
            };
            if (!IsValid(request))
            {
                return ProviderArtifactAcquisitionResult.TransportRefused(
                    request.ExpectedSource,
                    "acquisition-invalid");
            }

            if (source.Identity != request.ExpectedSource)
            {
                return ProviderArtifactAcquisitionResult.TransportRefused(
                    request.ExpectedSource,
                    "acquisition-source-mismatch");
            }

            var transaction = Path.Combine(_transactionRoot, $".acquire-{Guid.NewGuid():N}");
            Directory.CreateDirectory(transaction);
            try
            {
                foreach (var member in request.Files.OrderBy(file => file.RelativePath, StringComparer.Ordinal))
                {
                    Stream? input;
                    try
                    {
                        input = source.OpenRead(member.RelativePath);
                    }
                    catch (Exception exception) when (IsTransportException(exception))
                    {
                        return ProviderArtifactAcquisitionResult.TransportRefused(
                            request.ExpectedSource,
                            "acquisition-transport-failed");
                    }

                    if (input is null)
                    {
                        return ProviderArtifactAcquisitionResult.TransportRefused(
                            request.ExpectedSource,
                            "acquisition-member-unavailable");
                    }

                    using (input)
                    {
                        var destination = CombineRelative(transaction, member.RelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                        try
                        {
                            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                            if (!CopyExact(input, output, member.Length))
                            {
                                return ProviderArtifactAcquisitionResult.TransportRefused(
                                    request.ExpectedSource,
                                    "acquisition-length-mismatch");
                            }
                        }
                        catch (Exception exception) when (IsTransportException(exception))
                        {
                            return ProviderArtifactAcquisitionResult.TransportRefused(
                                request.ExpectedSource,
                                "acquisition-transport-failed");
                        }
                    }
                }

                var staging = _store.Stage(new ProviderArtifactSet(
                    request.Identity,
                    transaction,
                    request.Files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)).ToArray(),
                    request.ExecutablePath,
                    request.Arguments));
                return ProviderArtifactAcquisitionResult.Admitted(request.ExpectedSource, staging);
            }
            catch (Exception exception) when (IsTransportException(exception))
            {
                return ProviderArtifactAcquisitionResult.TransportRefused(
                    request.ExpectedSource,
                    "acquisition-transport-failed");
            }
            finally
            {
                DeleteTree(transaction);
            }
        }
    }

    private static bool IsValid(ProviderArtifactAcquisitionRequest request)
    {
        if (request.Files.Count == 0
            || string.IsNullOrWhiteSpace(request.ExpectedSource.Value)
            || request.MaxTotalBytes <= 0
            || request.Arguments.Any(string.IsNullOrEmpty)
            || !IsSafeRelativePath(request.ExecutablePath)
            || request.Files.Any(file =>
                file is null
                || !IsSafeRelativePath(file.RelativePath)
                || !ContentAddressedProviderStore.IsDigest(file.Sha256)
                || file.Length < 0)
            || request.Files.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count()
                != request.Files.Count
            || !request.Files.Any(file => file.RelativePath == request.ExecutablePath))
        {
            return false;
        }

        long total = 0;
        foreach (var file in request.Files)
        {
            if (file.Length > request.MaxTotalBytes - total)
            {
                return false;
            }

            total += file.Length;
        }

        var identity = ProviderArtifactSetIdentity.Compute(
            request.Files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256)),
            request.ExecutablePath,
            request.Arguments);
        return identity == request.Identity;
    }

    private static bool CopyExact(Stream input, Stream output, long length)
    {
        var remaining = length;
        var buffer = new byte[81920];
        while (remaining > 0)
        {
            var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
            {
                return false;
            }

            output.Write(buffer, 0, read);
            remaining -= read;
        }

        return input.ReadByte() == -1;
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathFullyQualified(path) || path.Contains('\\'))
        {
            return false;
        }

        return path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static string CombineRelative(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    private static bool IsTransportException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ObjectDisposedException or NotSupportedException;

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
