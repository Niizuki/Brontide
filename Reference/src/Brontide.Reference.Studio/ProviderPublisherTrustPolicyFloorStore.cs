using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public sealed record ProviderPublisherTrustPolicyFloorStoreResult(
    string Code,
    DurableProviderPublisherTrustPolicyFloorStore? Store);

public sealed record ProviderPublisherTrustPolicyFloorRetentionResult(
    string Code,
    ProviderPublisherTrustPolicyRecoveryFloor Stored)
{
    public bool IsRetained => Code is "policy-floor-retained" or "policy-floor-unchanged";
}

public sealed record ProviderPublisherTrustPolicyCustodyResult(
    string Code,
    string? CheckpointCode,
    DurableProviderPublisherTrustPolicyRegistry? Registry,
    DurableProviderPublisherTrustPolicyFloorStore? Floors)
{
    public bool IsOpened => Code == "policy-floor-opened";
}

/// <summary>
/// Durable custody of the CBI38 recovery floor. The integrity tag detects corruption and truncation;
/// it is not a defence against an adversary who can write this file, because such an adversary
/// recomputes the tag. Real custody is a separate privilege domain and is not implemented here.
/// </summary>
public sealed class DurableProviderPublisherTrustPolicyFloorStore : IProviderPublisherTrustPolicyFloorSink
{
    private const int MaxBytes = 64 * 1024;
    private const int TagBytes = 32;
    private readonly object sync = new();
    private readonly string path;
    private readonly ProviderPublisherTrustPolicyAuthorityId authority;
    private ProviderPublisherTrustPolicyRecoveryFloor stored;

    private DurableProviderPublisherTrustPolicyFloorStore(
        string path,
        ProviderPublisherTrustPolicyAuthorityId authority,
        ProviderPublisherTrustPolicyRecoveryFloor stored)
    {
        this.path = path;
        this.authority = authority;
        this.stored = stored;
    }

    public ProviderPublisherTrustPolicyRecoveryFloor Stored
    {
        get { lock (sync) return stored; }
    }

    public static ProviderPublisherTrustPolicyFloorStoreResult Open(
        string path,
        ProviderPublisherTrustPolicyAuthorityId authority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path))
        {
            var empty = ProviderPublisherTrustPolicyRecoveryFloor.Restore(authority, 0, null);
            // Establishing at zero before anything is published is what lets a later absence mean
            // "the guard was removed" rather than "nothing has happened yet".
            if (!TryWrite(path, empty)) return new("policy-floor-write-failed", null);
            return new("policy-floor-established", new(path, authority, empty));
        }
        if (!TryRead(path, out var sequence, out var policyIdentity, out var storedAuthority))
            return new("policy-floor-corrupt", null);
        if (storedAuthority != authority) return new("policy-floor-authority-mismatch", null);
        return new("policy-floor-recovered",
            new(path, authority, ProviderPublisherTrustPolicyRecoveryFloor.Restore(authority, sequence, policyIdentity)));
    }

    public ProviderPublisherTrustPolicyFloorRetentionResult Retain(ProviderPublisherTrustPolicyRecoveryFloor floor)
    {
        ArgumentNullException.ThrowIfNull(floor);
        lock (sync)
        {
            if (floor.AuthorityIdentity != authority)
                return new("policy-floor-authority-mismatch", stored);
            if (floor.Sequence == stored.Sequence && floor.PolicyIdentity == stored.PolicyIdentity)
                return new("policy-floor-unchanged", stored);
            // Same sequence under a different identity is a fork, which is a regression rather than
            // an advance: the floor would stop recognising the chain it was retained from.
            if (floor.Sequence <= stored.Sequence)
                return new("policy-floor-regressed", stored);
            if (!TryWrite(path, floor)) return new("policy-floor-write-failed", stored);
            stored = floor;
            return new("policy-floor-retained", stored);
        }
    }

    /// <summary>
    /// A refused retention reaches CBI41 as a failed handoff rather than being swallowed, so the
    /// cycle reports an advanced-but-unretained floor instead of claiming custody it does not have.
    /// </summary>
    public Task RetainAsync(ProviderPublisherTrustPolicyRecoveryFloor floor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Retain(floor);
        if (!result.IsRetained)
            throw new InvalidOperationException($"The recovery floor was not retained: {result.Code}.");
        return Task.CompletedTask;
    }

    public static byte[] EncodeRecord(
        ProviderPublisherTrustPolicyAuthorityId authority,
        long sequence,
        ProviderPublisherTrustPolicyId? policyIdentity)
    {
        using var output = new MemoryStream();
        Write(output, "CBI42");
        Write(output, authority.Value);
        Write(output, sequence);
        Write(output, policyIdentity.HasValue ? 1 : 0);
        if (policyIdentity.HasValue) Write(output, policyIdentity.Value.Value);
        var record = output.ToArray();
        return [.. record, .. SHA256.HashData(record)];
    }

    private static bool TryWrite(string path, ProviderPublisherTrustPolicyRecoveryFloor floor)
    {
        var temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = EncodeRecord(floor.AuthorityIdentity, floor.Sequence, floor.PolicyIdentity);
            if (bytes.Length > MaxBytes) return false;
            using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            {
                output.Write(bytes);
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

    private static bool TryRead(
        string path,
        out long sequence,
        out ProviderPublisherTrustPolicyId? policyIdentity,
        out ProviderPublisherTrustPolicyAuthorityId authority)
    {
        sequence = 0;
        policyIdentity = null;
        authority = default;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes)))
                return false;
            var reader = new Reader(record);
            if (reader.String() != "CBI42") return false;
            authority = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
            sequence = reader.Int64();
            if (sequence < 0) return false;
            var presence = reader.Int32();
            if (presence is not 0 and not 1) return false;
            policyIdentity = presence == 1 ? ProviderPublisherTrustPolicyId.Create(reader.String()) : null;
            // A sequence above zero without an identity, or the reverse, is not a floor any issuer
            // produces, so it is refused rather than half-read.
            if ((sequence == 0) != (policyIdentity is null)) return false;
            return reader.End;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException or ArgumentException or DecoderFallbackException)
        {
            sequence = 0;
            policyIdentity = null;
            authority = default;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private static void Write(Stream output, int value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Write(Stream output, long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes, value);
        output.Write(bytes);
    }

    private static void Write(Stream output, string value)
    {
        var bytes = new UTF8Encoding(false, true).GetBytes(value);
        Write(output, bytes.Length);
        output.Write(bytes);
    }

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes;
        private int offset = 0;
        internal bool End => offset == bytes.Length;
        internal int Int32() { Ensure(4); var value = BinaryPrimitives.ReadInt32BigEndian(bytes[offset..]); offset += 4; return value; }
        internal long Int64() { Ensure(8); var value = BinaryPrimitives.ReadInt64BigEndian(bytes[offset..]); offset += 8; return value; }
        internal string String()
        {
            var length = Int32();
            if (length < 0 || length > MaxBytes) throw new InvalidDataException();
            Ensure(length);
            var value = new UTF8Encoding(false, true).GetString(bytes.Slice(offset, length));
            offset += length;
            return value;
        }
        private void Ensure(int length) { if (length > bytes.Length - offset) throw new InvalidDataException(); }
    }
}

public static class ProviderPublisherTrustPolicyCustody
{
    /// <summary>
    /// Opens the durable registry under the floor its own store holds. The floor CBI38 reports back
    /// is never written to the store: a checkpoint that could raise its own guard would let a forged
    /// chain reaching further than the true one refuse every genuine successor afterwards.
    /// </summary>
    public static ProviderPublisherTrustPolicyCustodyResult Open(
        string checkpointPath,
        string floorPath,
        ProviderPublisherTrustPolicyAuthorityId authority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(floorPath);
        var checkpointExists = File.Exists(Path.GetFullPath(checkpointPath));
        if (!File.Exists(Path.GetFullPath(floorPath)) && checkpointExists)
            return new("policy-floor-missing", null, null, null);

        var floors = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, authority);
        if (floors.Store is null) return new(floors.Code, null, null, null);

        var opened = DurableProviderPublisherTrustPolicyRegistry.Open(checkpointPath, authority, floors.Store.Stored);
        return opened.Registry is null
            ? new(opened.Code, opened.Code, null, null)
            : new("policy-floor-opened", opened.Code, opened.Registry, floors.Store);
    }
}
