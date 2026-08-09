using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Brontide.Reference.Studio;

/// <summary>
/// Why the host stopped one occurrence's provider, as the store issued it. There is no public
/// construction path: CBI51 used to take a <see cref="ProviderRestartCause"/> the caller chose, and
/// two of its four values are refusals, so a caller could select which refusal applied to it. The
/// only way to obtain one of these is to ask the store about an activation.
/// </summary>
public sealed record ProviderStopAttribution
{
    internal ProviderStopAttribution(
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity,
        DateTimeOffset? instant,
        ProviderRestartCause cause)
    {
        Occurrence = occurrence;
        StagedIdentity = stagedIdentity;
        Instant = instant;
        Cause = cause;
    }

    public Brontide.Reference.Experimental.ComponentManagement.OccurrenceId Occurrence { get; }
    public ProviderArtifactSetId StagedIdentity { get; }

    /// <summary>Absent when the host holds no record, which is what an unexpected exit looks like.</summary>
    public DateTimeOffset? Instant { get; }

    public ProviderRestartCause Cause { get; }
}

public sealed record ProviderStopAttributionResult(string Code, ProviderStopAttribution? Attribution);

public sealed record ProviderStopAttributionStoreResult(
    string Code,
    DurableProviderStopAttributionStore? Store);

/// <summary>
/// A host-local record of why the host stopped each occurrence's provider. Every path in the host that
/// stops one writes here after the effect is complete, and CBI51 reads the cause from here instead of
/// being told it.
///
/// The integrity tag detects corruption and truncation, exactly as CBI42's floor store does and with
/// the same limit: it is not a defence against an adversary who can write this file, because such an
/// adversary recomputes the tag.
/// </summary>
public sealed class DurableProviderStopAttributionStore
{
    public const int MaximumRecords = 64;
    private const int MaxBytes = 64 * 1024;
    private const int TagBytes = 32;
    private readonly object sync = new();
    private readonly string path;
    private Dictionary<string, Entry> records;

    private DurableProviderStopAttributionStore(string path, Dictionary<string, Entry> records)
    {
        this.path = path;
        this.records = records;
    }

    public static ProviderStopAttributionStoreResult Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = Path.GetFullPath(path);
        TryDelete(path + ".tmp");
        if (!File.Exists(path))
        {
            return TryWrite(path, [])
                ? new("provider-stop-attribution-established", new(path, []))
                : new("provider-stop-attribution-write-failed", null);
        }
        if (!TryRead(path, out var stored)) return new("provider-stop-attribution-corrupt", null);
        return new("provider-stop-attribution-opened", new(path, stored));
    }

    /// <summary>
    /// Records one stop. Callers invoke this once the effect is complete, never before: a record is a
    /// statement about something that happened, so it cannot precede the thing it describes — CBI41's
    /// rule about its own floor, in its third instance. A record written first and then interrupted
    /// would claim a stop that did not occur, and CBI52 would launch a second provider for an
    /// occurrence that is still serving.
    /// </summary>
    public string Record(
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity,
        DateTimeOffset instant,
        ProviderRestartCause cause)
    {
        if (cause is not (ProviderRestartCause.OfflineAvailability
            or ProviderRestartCause.PublisherTrustWithdrawal
            or ProviderRestartCause.OperatorRetirement))
            throw new ArgumentOutOfRangeException(nameof(cause));

        lock (sync)
        {
            var next = new Dictionary<string, Entry>(records, StringComparer.Ordinal)
            {
                [occurrence.Value] = new(occurrence.Value, stagedIdentity.Value, instant, cause),
            };
            if (next.Count > MaximumRecords) return "provider-stop-attribution-full";
            if (!TryWrite(path, next)) return "provider-stop-attribution-write-failed";
            records = next;
            return "provider-stop-attribution-recorded";
        }
    }

    /// <summary>Records one stop for an activation, which is what every writer in the host holds.</summary>
    public string Record(
        ProviderServingActivation activation,
        DateTimeOffset instant,
        ProviderRestartCause cause)
    {
        ArgumentNullException.ThrowIfNull(activation);
        return activation.Chain.StagedIdentity is { } stagedIdentity
            ? Record(activation.Occurrence, stagedIdentity, instant, cause)
            : "provider-stop-attribution-activation-unavailable";
    }

    /// <summary>Removes the record a successful reconstruction consumed.</summary>
    public string Clear(Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence)
    {
        lock (sync)
        {
            if (!records.ContainsKey(occurrence.Value)) return "provider-stop-attribution-absent";
            var next = new Dictionary<string, Entry>(records, StringComparer.Ordinal);
            next.Remove(occurrence.Value);
            if (!TryWrite(path, next)) return "provider-stop-attribution-write-failed";
            records = next;
            return "provider-stop-attribution-cleared";
        }
    }

    /// <summary>
    /// Issues the attribution for one activation. A record the store holds for that occurrence under a
    /// different staged identity describes a different deployment and is refused rather than resolved
    /// either way. No record at all is an unexpected exit, because every stop the host performs writes
    /// one.
    /// </summary>
    public ProviderStopAttributionResult Attribute(
        Brontide.Reference.Experimental.ComponentManagement.OccurrenceId occurrence,
        ProviderArtifactSetId stagedIdentity)
    {
        lock (sync)
        {
            if (!records.TryGetValue(occurrence.Value, out var record))
                return new("provider-stop-attribution-unrecorded",
                    new(occurrence, stagedIdentity, null, ProviderRestartCause.UnexpectedExit));
            if (!StringComparer.Ordinal.Equals(record.StagedIdentity, stagedIdentity.Value))
                return new("provider-restart-attribution-stale", null);
            return new("provider-stop-attribution-issued",
                new(occurrence, stagedIdentity, record.Instant, record.Cause));
        }
    }

    /// <summary>Issues the attribution for one activation, which is what a host holds.</summary>
    public ProviderStopAttributionResult Attribute(ProviderServingActivation activation)
    {
        ArgumentNullException.ThrowIfNull(activation);
        return activation.Chain.StagedIdentity is { } stagedIdentity
            ? Attribute(activation.Occurrence, stagedIdentity)
            : new("provider-stop-attribution-activation-unavailable", null);
    }

    private sealed record Entry(
        string Occurrence, string StagedIdentity, DateTimeOffset Instant, ProviderRestartCause Cause);

    private static bool TryWrite(string path, Dictionary<string, Entry> records)
    {
        var temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var output = new MemoryStream();
            Write(output, "CBI67");
            Write(output, records.Count);
            // Ordered, so one set of records has one encoding and a rewrite that changed nothing
            // produces the same bytes.
            foreach (var record in records.Values.OrderBy(value => value.Occurrence, StringComparer.Ordinal))
            {
                Write(output, record.Occurrence);
                Write(output, record.StagedIdentity);
                Write(output, record.Instant.ToUnixTimeMilliseconds());
                Write(output, (int)record.Cause);
            }
            var body = output.ToArray();
            byte[] bytes = [.. body, .. SHA256.HashData(body)];
            if (bytes.Length > MaxBytes) return false;
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                       4096, FileOptions.WriteThrough))
            {
                file.Write(bytes);
                file.Flush(true);
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

    private static bool TryRead(string path, out Dictionary<string, Entry> records)
    {
        records = new(StringComparer.Ordinal);
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length <= TagBytes || bytes.Length > MaxBytes) return false;
            var body = bytes.AsSpan(0, bytes.Length - TagBytes);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(body), bytes.AsSpan(bytes.Length - TagBytes)))
                return false;
            var reader = new Reader(body);
            if (reader.String() != "CBI67") return false;
            var count = reader.Int32();
            if (count is < 0 or > MaximumRecords) return false;
            var parsed = new Dictionary<string, Entry>(StringComparer.Ordinal);
            for (var index = 0; index < count; index++)
            {
                var occurrence = reader.String();
                var stagedIdentity = reader.String();
                var instant = DateTimeOffset.FromUnixTimeMilliseconds(reader.Int64());
                var cause = (ProviderRestartCause)reader.Int32();
                if (occurrence.Length == 0 || stagedIdentity.Length == 0
                    || !Enum.IsDefined(cause) || cause == ProviderRestartCause.UnexpectedExit
                    || !parsed.TryAdd(occurrence, new(occurrence, stagedIdentity, instant, cause)))
                    return false;
            }
            if (!reader.End) return false;
            records = parsed;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or InvalidDataException or ArgumentException or DecoderFallbackException)
        {
            records = new(StringComparer.Ordinal);
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

/// <summary>
/// The one path by which an operator retirement becomes attributable. A retirement issued outside the
/// host leaves no record and an exited process, which is indistinguishable from an unexpected exit —
/// that is the bound on what this slice can attribute, and it is stated rather than implied away.
/// </summary>
public static class ProviderOperatorRetirement
{
    public static async ValueTask<string> RetireAsync(
        DurableProviderStopAttributionStore attributions,
        ProviderServingActivation activation,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attributions);
        ArgumentNullException.ThrowIfNull(activation);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (activation.Chain.StagedIdentity is not { } stagedIdentity)
            return "provider-stop-attribution-activation-unavailable";

        if (activation.IsServing) await activation.RetireAsync(reason, cancellationToken).ConfigureAwait(false);
        if (activation.Chain.Provider is { HasExited: false } provider)
            await provider.DisposeAsync().ConfigureAwait(false);

        return attributions.Record(
            activation.Occurrence, stagedIdentity, now, ProviderRestartCause.OperatorRetirement);
    }
}
