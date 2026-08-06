using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

public sealed record ProviderPolicyAuthorityFloorStoreResult(
    string Code,
    DurableProviderPolicyAuthorityFloorStore? Store);

public sealed record ProviderPolicyAuthorityFloorRetentionResult(
    string Code,
    ProviderPolicyAuthorityFloor Stored)
{
    public bool IsRetained => Code is "policy-authority-floor-retained" or "policy-authority-floor-unchanged";
}

public sealed record ProviderPolicyAuthorityCustodyResult(
    string Code,
    string? CheckpointCode,
    DurableProviderPublisherTrustPolicyRegistry? Registry,
    DurableProviderPublisherTrustPolicyFloorStore? Floors,
    DurableProviderPolicyAuthorityFloorStore? AuthorityFloors)
{
    public bool IsOpened => Code is "policy-authority-floor-opened" or "policy-authority-floor-adopted";
}

/// <summary>
/// Durable custody of the CBI38 authority floor. The integrity tag detects corruption and truncation;
/// it is not a defence against an adversary who can write this file, because such an adversary
/// recomputes the tag. Real custody is a separate privilege domain and is not implemented here.
/// </summary>
public sealed class DurableProviderPolicyAuthorityFloorStore : IProviderPolicyAuthorityFloorSink
{
    private const int MaxBytes = 64 * 1024;
    private const int TagBytes = 32;
    private readonly object sync = new();
    private readonly string path;
    private readonly ProviderPublisherTrustPolicyAuthorityId pin;
    private ProviderPolicyAuthorityFloor stored;

    private DurableProviderPolicyAuthorityFloorStore(
        string path,
        ProviderPublisherTrustPolicyAuthorityId pin,
        ProviderPolicyAuthorityFloor stored)
    {
        this.path = path;
        this.pin = pin;
        this.stored = stored;
    }

    public ProviderPolicyAuthorityFloor Stored
    {
        get { lock (sync) return stored; }
    }

    public static ProviderPolicyAuthorityFloorStoreResult Open(
        string path,
        ProviderPublisherTrustPolicyAuthorityId pin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (pin.Value is null) throw new ArgumentException("An authority pin is required.", nameof(pin));
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path))
        {
            // Generation zero names the pin itself, because that is the only authority floor an
            // unrotated checkpoint can satisfy.
            var empty = ProviderPolicyAuthorityFloor.Restore(0, pin);
            if (!TryWrite(path, pin, empty)) return new("policy-authority-floor-write-failed", null);
            return new("policy-authority-floor-established", new(path, pin, empty));
        }
        if (!TryRead(path, out var storedPin, out var generation, out var active))
            return new("policy-authority-floor-corrupt", null);
        if (storedPin != pin) return new("policy-authority-floor-authority-mismatch", null);
        return new("policy-authority-floor-recovered",
            new(path, pin, ProviderPolicyAuthorityFloor.Restore(generation, active)));
    }

    public ProviderPolicyAuthorityFloorRetentionResult Retain(ProviderPolicyAuthorityFloor floor)
    {
        ArgumentNullException.ThrowIfNull(floor);
        lock (sync)
        {
            if (floor.Generation == 0 && floor.ActiveAuthority != pin)
                return new("policy-authority-floor-authority-mismatch", stored);
            if (floor.Generation == stored.Generation && floor.ActiveAuthority == stored.ActiveAuthority)
                return new("policy-authority-floor-unchanged", stored);
            // An equal generation naming a different active authority is a fork rather than an
            // advance: the floor would stop recognising the chain it was retained from.
            if (floor.Generation <= stored.Generation)
                return new("policy-authority-floor-regressed", stored);
            if (!TryWrite(path, pin, floor)) return new("policy-authority-floor-write-failed", stored);
            stored = floor;
            return new("policy-authority-floor-retained", stored);
        }
    }

    /// <summary>
    /// A refused retention reaches CBI60's cycle as a failed handoff rather than being swallowed, so
    /// the cycle reports an advanced-but-unretained floor instead of claiming custody it does not
    /// have.
    /// </summary>
    public Task RetainAsync(ProviderPolicyAuthorityFloor floor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Retain(floor);
        if (!result.IsRetained)
            throw new InvalidOperationException($"The authority floor was not retained: {result.Code}.");
        return Task.CompletedTask;
    }

    public static byte[] EncodeRecord(
        ProviderPublisherTrustPolicyAuthorityId pin,
        long generation,
        ProviderPublisherTrustPolicyAuthorityId activeAuthority)
    {
        using var output = new MemoryStream();
        Write(output, "CBI60");
        Write(output, pin.Value);
        Write(output, generation);
        Write(output, activeAuthority.Value);
        var record = output.ToArray();
        return [.. record, .. SHA256.HashData(record)];
    }

    private static bool TryWrite(
        string path,
        ProviderPublisherTrustPolicyAuthorityId pin,
        ProviderPolicyAuthorityFloor floor)
    {
        var temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = EncodeRecord(pin, floor.Generation, floor.ActiveAuthority);
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
        out ProviderPublisherTrustPolicyAuthorityId pin,
        out long generation,
        out ProviderPublisherTrustPolicyAuthorityId activeAuthority)
    {
        pin = default;
        generation = 0;
        activeAuthority = default;
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var record = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(record), bytes.AsSpan(bytes.Length - TagBytes)))
                return false;
            var reader = new Reader(record);
            if (reader.String() != "CBI60") return false;
            pin = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
            generation = reader.Int64();
            if (generation < 0) return false;
            activeAuthority = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
            // Generation zero under any authority but the pin is not a floor an issuer produces, and
            // it would refuse the empty checkpoint it is supposed to admit.
            if (generation == 0 && activeAuthority != pin) return false;
            return reader.End;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException or ArgumentException or DecoderFallbackException)
        {
            pin = default;
            generation = 0;
            activeAuthority = default;
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

public static class ProviderPolicyAuthorityCustody
{
    /// <summary>
    /// Opens the durable registry under both guards. CBI42 establishes its policy floor before the
    /// checkpoint exists, which is what lets a later absence mean the guard was removed; a guard
    /// introduced after those checkpoints already exist cannot use that ordering, so an absent
    /// authority floor is adopted at zero and reported as such rather than being refused or being
    /// reported as a recovery the host never made.
    /// </summary>
    public static ProviderPolicyAuthorityCustodyResult Open(
        string checkpointPath,
        string floorPath,
        string authorityFloorPath,
        ProviderPublisherTrustPolicyAuthorityId pin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(floorPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorityFloorPath);
        var checkpointExists = File.Exists(Path.GetFullPath(checkpointPath));
        if (!File.Exists(Path.GetFullPath(floorPath)) && checkpointExists)
            return new("policy-floor-missing", null, null, null, null);
        var adopted = !File.Exists(Path.GetFullPath(authorityFloorPath)) && checkpointExists;

        var floors = DurableProviderPublisherTrustPolicyFloorStore.Open(floorPath, pin);
        if (floors.Store is null) return new(floors.Code, null, null, null, null);
        var authorityFloors = DurableProviderPolicyAuthorityFloorStore.Open(authorityFloorPath, pin);
        if (authorityFloors.Store is null) return new(authorityFloors.Code, null, null, floors.Store, null);

        var opened = DurableProviderPublisherTrustPolicyRegistry.Open(
            checkpointPath, pin, floors.Store.Stored, authorityFloors.Store.Stored);
        return opened.Registry is null
            ? new(opened.Code, opened.Code, null, floors.Store, authorityFloors.Store)
            : new(adopted ? "policy-authority-floor-adopted" : "policy-authority-floor-opened",
                opened.Code, opened.Registry, floors.Store, authorityFloors.Store);
    }
}
