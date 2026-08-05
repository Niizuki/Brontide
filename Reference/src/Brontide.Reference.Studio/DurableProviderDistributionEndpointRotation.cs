using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Studio;

public sealed record ProviderDistributionEndpointRotationStatement(
    long Generation,
    ProviderPublisherTrustPolicyDistributionEndpointId PreviousEndpoint,
    ProviderPublisherTrustPolicyDistributionEndpointId NextEndpoint,
    string Algorithm,
    string PreviousEndpointPublicKeySpkiBase64,
    string SignatureBase64);

public sealed record ProviderDistributionEndpointRotationSnapshot(
    ProviderPublisherTrustPolicyDistributionEndpointId InitialEndpoint,
    long Generation,
    ProviderPublisherTrustPolicyDistributionEndpointId ActiveEndpoint,
    long? StagedGeneration,
    ProviderPublisherTrustPolicyDistributionEndpointId? StagedEndpoint);

public sealed record ProviderDistributionEndpointRotationFloor
{
    private ProviderDistributionEndpointRotationFloor(
        long generation,
        ProviderPublisherTrustPolicyDistributionEndpointId activeEndpoint)
    {
        Generation = generation;
        ActiveEndpoint = activeEndpoint;
    }

    public long Generation { get; }
    public ProviderPublisherTrustPolicyDistributionEndpointId ActiveEndpoint { get; }

    internal static ProviderDistributionEndpointRotationFloor Issue(
        ProviderDistributionEndpointRotationSnapshot snapshot) =>
        new(snapshot.Generation, snapshot.ActiveEndpoint);

    public static ProviderDistributionEndpointRotationFloor Restore(
        long generation,
        ProviderPublisherTrustPolicyDistributionEndpointId activeEndpoint)
    {
        if (generation < 0 || activeEndpoint.Value is null)
            throw new ArgumentException("A valid endpoint-rotation floor is required.");
        return new(generation, activeEndpoint);
    }
}

public sealed record ProviderDistributionEndpointRotationOpenResult(
    string Code,
    DurableProviderDistributionEndpointRotation? Rotation,
    ProviderDistributionEndpointRotationSnapshot? Snapshot,
    ProviderDistributionEndpointRotationFloor? Floor);

public sealed record ProviderDistributionEndpointRotationResult(
    string Code,
    ProviderDistributionEndpointRotationSnapshot Snapshot,
    ProviderDistributionEndpointRotationFloor Floor,
    string DistributionCode);

public static class ProviderDistributionEndpointRotationManifest
{
    public static byte[] Encode(
        long generation,
        ProviderPublisherTrustPolicyDistributionEndpointId previousEndpoint,
        ProviderPublisherTrustPolicyDistributionEndpointId nextEndpoint)
    {
        if (generation <= 0 || previousEndpoint.Value is null || nextEndpoint.Value is null)
            throw new ArgumentException("A valid endpoint rotation transition is required.");
        using var output = new MemoryStream();
        Append(output, "CBI56");
        Append(output, generation);
        Append(output, previousEndpoint.Value);
        Append(output, nextEndpoint.Value);
        return output.ToArray();
    }

    private static void Append(Stream output, long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Append(Stream output, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        output.Write(length);
        output.Write(bytes);
    }
}

/// <summary>
/// Host-local durable CBI56 anchor for one active CBI39 endpoint and one authenticated successor.
/// Integrity detects damage; rollback resistance depends on retaining the returned floor separately.
/// </summary>
public sealed class DurableProviderDistributionEndpointRotation
{
    private const int MaxBytes = 16 * 1024;
    private const int TagBytes = 32;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string path;
    private RotationState state;

    private DurableProviderDistributionEndpointRotation(string path, RotationState state)
    {
        this.path = path;
        this.state = state;
    }

    public ProviderDistributionEndpointRotationSnapshot Snapshot
    {
        get
        {
            gate.Wait();
            try { return Project(state); }
            finally { gate.Release(); }
        }
    }

    public ProviderDistributionEndpointRotationFloor Floor =>
        ProviderDistributionEndpointRotationFloor.Issue(Snapshot);

    public static ProviderDistributionEndpointRotationOpenResult Open(
        string path,
        ProviderPublisherTrustPolicyDistributionEndpointId initialEndpoint,
        ProviderDistributionEndpointRotationFloor? floor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (initialEndpoint.Value is null)
            throw new ArgumentException("An initial endpoint identity is required.", nameof(initialEndpoint));
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path))
        {
            if (floor is not null && (floor.Generation > 0 || floor.ActiveEndpoint != initialEndpoint))
                return new("endpoint-rotation-rollback-detected", null, null, null);
            var initial = new RotationState
            {
                Format = "CBI56",
                InitialEndpoint = initialEndpoint.Value,
                Generation = 0,
                ActiveEndpoint = initialEndpoint.Value,
            };
            if (!TryWrite(path, initial))
                return new("endpoint-rotation-write-failed", null, null, null);
            var established = new DurableProviderDistributionEndpointRotation(path, initial);
            return Opened("endpoint-rotation-established", established);
        }

        if (!TryRead(path, out var stored))
            return new("endpoint-rotation-corrupt", null, null, null);
        if (stored.InitialEndpoint != initialEndpoint.Value)
            return new("endpoint-rotation-initial-mismatch", null, Project(stored), null);
        if (floor is not null && (stored.Generation < floor.Generation
            || stored.Generation == floor.Generation && stored.ActiveEndpoint != floor.ActiveEndpoint.Value))
            return new("endpoint-rotation-rollback-detected", null, Project(stored), null);
        var recovered = new DurableProviderDistributionEndpointRotation(path, stored);
        return Opened("endpoint-rotation-recovered", recovered);
    }

    public ProviderDistributionEndpointRotationResult Stage(
        ProviderDistributionEndpointRotationStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        gate.Wait();
        try
        {
            var current = Project(state);
            if (state.StagedGeneration.HasValue)
            {
                if (state.StagedGeneration == statement.Generation
                    && state.ActiveEndpoint == statement.PreviousEndpoint.Value
                    && state.StagedEndpoint == statement.NextEndpoint.Value)
                    return Result("endpoint-rotation-already-staged", current);
                return Result("endpoint-rotation-successor-conflict", current);
            }
            if (statement.Generation != state.Generation + 1)
                return Result("endpoint-rotation-generation-invalid", current);
            if (statement.PreviousEndpoint.Value != state.ActiveEndpoint)
                return Result("endpoint-rotation-predecessor-mismatch", current);
            if (statement.NextEndpoint == statement.PreviousEndpoint)
                return Result("endpoint-rotation-self-refused", current);
            var evidence = Verify(statement);
            if (evidence is not null) return Result(evidence, current);

            var next = state.Copy();
            next.StagedGeneration = statement.Generation;
            next.StagedEndpoint = statement.NextEndpoint.Value;
            if (!TryWrite(path, next)) return Result("endpoint-rotation-write-failed", current);
            state = next;
            return Result("endpoint-rotation-staged", Project(state));
        }
        finally { gate.Release(); }
    }

    public ProviderPublisherTrustPolicyDistributionClient CreateCurrentClient(
        DurableProviderPublisherTrustPolicyRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        gate.Wait();
        try
        {
            return new ProviderPublisherTrustPolicyDistributionClient(registry,
                ProviderPublisherTrustPolicyDistributionEndpointId.Create(state.ActiveEndpoint));
        }
        finally { gate.Release(); }
    }

    public async Task<ProviderDistributionEndpointRotationResult> ConfirmAsync(
        DurableProviderPublisherTrustPolicyRegistry registry,
        IProviderPublisherTrustPolicyDistributionSource source,
        DateTimeOffset now,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(source);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var before = Project(state);
            if (!state.StagedGeneration.HasValue || state.StagedEndpoint is null)
                return Result("endpoint-rotation-not-staged", before);
            var client = new ProviderPublisherTrustPolicyDistributionClient(registry,
                ProviderPublisherTrustPolicyDistributionEndpointId.Create(state.StagedEndpoint));
            var distribution = await client.SynchronizeAsync(source, now, timeout, cancellationToken)
                .ConfigureAwait(false);
            if (distribution.Code is not "policy-distribution-current" and not "policy-distribution-applied")
                return Result("endpoint-rotation-confirmation-refused", before, distribution.Code);

            var next = state.Copy();
            next.Generation = state.StagedGeneration.Value;
            next.ActiveEndpoint = state.StagedEndpoint;
            next.StagedGeneration = null;
            next.StagedEndpoint = null;
            if (!TryWrite(path, next))
                return Result("endpoint-rotation-write-failed", before, distribution.Code);
            state = next;
            return Result("endpoint-rotation-applied", Project(state), distribution.Code);
        }
        finally { gate.Release(); }
    }

    private ProviderDistributionEndpointRotationResult Result(
        string code,
        ProviderDistributionEndpointRotationSnapshot snapshot,
        string distributionCode = "not-attempted") =>
        new(code, snapshot, ProviderDistributionEndpointRotationFloor.Issue(snapshot), distributionCode);

    private static ProviderDistributionEndpointRotationOpenResult Opened(
        string code,
        DurableProviderDistributionEndpointRotation rotation)
    {
        var snapshot = rotation.Snapshot;
        return new(code, rotation, snapshot, ProviderDistributionEndpointRotationFloor.Issue(snapshot));
    }

    private static string? Verify(ProviderDistributionEndpointRotationStatement statement)
    {
        if (statement.Algorithm != "ECDSA-P256-SHA256"
            || string.IsNullOrWhiteSpace(statement.PreviousEndpointPublicKeySpkiBase64)
            || string.IsNullOrWhiteSpace(statement.SignatureBase64))
            return "endpoint-rotation-evidence-invalid";
        try
        {
            var publicKey = Convert.FromBase64String(statement.PreviousEndpointPublicKeySpkiBase64);
            var signature = Convert.FromBase64String(statement.SignatureBase64);
            var identity = ProviderPublisherTrustPolicyDistributionEndpointId.Create(
                Convert.ToHexString(SHA256.HashData(publicKey)));
            if (identity != statement.PreviousEndpoint)
                return "endpoint-rotation-key-mismatch";
            using var verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);
            var parameters = verifier.ExportParameters(false);
            if (bytesRead != publicKey.Length || verifier.KeySize != 256
                || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7")
                return "endpoint-rotation-evidence-invalid";
            return verifier.VerifyData(ProviderDistributionEndpointRotationManifest.Encode(
                    statement.Generation, statement.PreviousEndpoint, statement.NextEndpoint),
                signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                ? null : "endpoint-rotation-signature-invalid";
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException
            or ArgumentException or NullReferenceException)
        {
            return "endpoint-rotation-evidence-invalid";
        }
    }

    private static ProviderDistributionEndpointRotationSnapshot Project(RotationState value) => new(
        ProviderPublisherTrustPolicyDistributionEndpointId.Create(value.InitialEndpoint),
        value.Generation,
        ProviderPublisherTrustPolicyDistributionEndpointId.Create(value.ActiveEndpoint),
        value.StagedGeneration,
        value.StagedEndpoint is null
            ? null
            : ProviderPublisherTrustPolicyDistributionEndpointId.Create(value.StagedEndpoint));

    private static bool IsValid(RotationState value) =>
        value.Format == "CBI56"
        && ContentAddressedProviderStore.IsDigest(value.InitialEndpoint)
        && ContentAddressedProviderStore.IsDigest(value.ActiveEndpoint)
        && value.Generation >= 0
        && ((value.StagedGeneration is null && value.StagedEndpoint is null)
            || (value.StagedGeneration == value.Generation + 1
                && ContentAddressedProviderStore.IsDigest(value.StagedEndpoint ?? "")
                && value.StagedEndpoint != value.ActiveEndpoint));

    private static bool TryWrite(string path, RotationState value)
    {
        var temporary = path + ".tmp";
        try
        {
            if (!IsValid(value)) return false;
            var record = JsonSerializer.SerializeToUtf8Bytes(value);
            if (record.Length + TagBytes > MaxBytes) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write,
                       FileShare.None, 4096, FileOptions.WriteThrough))
            {
                output.Write(record);
                output.Write(SHA256.HashData(record));
                output.Flush(true);
            }
            File.Move(temporary, path, true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            TryDelete(temporary);
            return false;
        }
    }

    private static bool TryRead(string path, out RotationState value)
    {
        value = null!;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes))) return false;
            value = JsonSerializer.Deserialize<RotationState>(record)!;
            return value is not null && IsValid(value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or JsonException or NotSupportedException or ArgumentException)
        {
            value = null!;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private sealed class RotationState
    {
        public string Format { get; set; } = "";
        public string InitialEndpoint { get; set; } = "";
        public long Generation { get; set; }
        public string ActiveEndpoint { get; set; } = "";
        public long? StagedGeneration { get; set; }
        public string? StagedEndpoint { get; set; }

        public RotationState Copy() => new()
        {
            Format = Format,
            InitialEndpoint = InitialEndpoint,
            Generation = Generation,
            ActiveEndpoint = ActiveEndpoint,
            StagedGeneration = StagedGeneration,
            StagedEndpoint = StagedEndpoint,
        };
    }
}
