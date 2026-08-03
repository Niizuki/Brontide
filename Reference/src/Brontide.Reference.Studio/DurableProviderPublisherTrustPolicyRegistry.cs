using System.Buffers.Binary;
using System.Text;

namespace Brontide.Reference.Studio;

public sealed record ProviderPublisherTrustPolicyRecoveryFloor
{
    private ProviderPublisherTrustPolicyRecoveryFloor(
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        long sequence,
        ProviderPublisherTrustPolicyId? policyIdentity)
    {
        AuthorityIdentity = authorityIdentity;
        Sequence = sequence;
        PolicyIdentity = policyIdentity;
    }

    public ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity { get; }
    public long Sequence { get; }
    public ProviderPublisherTrustPolicyId? PolicyIdentity { get; }

    internal static ProviderPublisherTrustPolicyRecoveryFloor Issue(
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        VerifiedProviderPublisherTrustPolicySnapshot? current) =>
        new(authorityIdentity, current?.Sequence ?? 0, current?.Policy.Identity);
}

public sealed record DurableProviderPublisherTrustPolicyResult(
    string Code,
    DurableProviderPublisherTrustPolicyRegistry? Registry,
    ProviderPublisherTrustPolicyRecoveryFloor? Floor);

public sealed record DurableProviderPublisherTrustPolicyUpdateResult(
    string Code,
    VerifiedProviderPublisherTrustPolicySnapshot? Current,
    ProviderPublisherTrustPolicyRecoveryFloor Floor)
{
    public bool IsApplied => Code == "policy-update-applied";
}

public sealed class DurableProviderPublisherTrustPolicyRegistry
{
    private readonly object _sync = new();
    private readonly string _path;
    private readonly ProviderPublisherTrustPolicyRegistry _registry;
    private readonly List<ProviderPublisherTrustPolicyUpdate> _updates;

    private DurableProviderPublisherTrustPolicyRegistry(
        string path,
        ProviderPublisherTrustPolicyRegistry registry,
        List<ProviderPublisherTrustPolicyUpdate> updates)
    {
        _path = path;
        _registry = registry;
        _updates = updates;
    }

    public VerifiedProviderPublisherTrustPolicySnapshot? Current => _registry.Current;

    public static DurableProviderPublisherTrustPolicyResult Open(
        string path,
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        ProviderPublisherTrustPolicyRecoveryFloor? floor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = Path.GetFullPath(path);
        if (floor is not null && floor.AuthorityIdentity != authorityIdentity)
            return new("policy-checkpoint-authority-mismatch", null, null);
        var temporary = path + ".tmp";
        TryDelete(temporary);
        if (!File.Exists(path))
        {
            if (floor is not null && floor.Sequence > 0)
                return new("policy-checkpoint-rollback-detected", null, null);
            var emptyRegistry = new ProviderPublisherTrustPolicyRegistry(authorityIdentity);
            var empty = new DurableProviderPublisherTrustPolicyRegistry(path, emptyRegistry, []);
            return new("policy-checkpoint-empty", empty,
                ProviderPublisherTrustPolicyRecoveryFloor.Issue(authorityIdentity, null));
        }

        if (!CheckpointCodec.TryRead(path, out var storedAuthority, out var updates))
            return new("policy-checkpoint-corrupt", null, null);
        if (storedAuthority != authorityIdentity)
            return new("policy-checkpoint-authority-mismatch", null, null);
        var registry = new ProviderPublisherTrustPolicyRegistry(authorityIdentity);
        foreach (var update in updates)
        {
            var applied = registry.Apply(update);
            if (!applied.IsApplied) return new("policy-checkpoint-invalid-chain", null, null);
        }
        var current = registry.Current;
        if (floor is not null && IsRollback(current, floor))
            return new("policy-checkpoint-rollback-detected", null, null);
        var durable = new DurableProviderPublisherTrustPolicyRegistry(path, registry, updates);
        return new("policy-checkpoint-recovered", durable,
            ProviderPublisherTrustPolicyRecoveryFloor.Issue(authorityIdentity, current));
    }

    public DurableProviderPublisherTrustPolicyUpdateResult Apply(ProviderPublisherTrustPolicyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        lock (_sync)
        {
            var shadow = new ProviderPublisherTrustPolicyRegistry(_registry.AuthorityIdentity);
            foreach (var existing in _updates) shadow.Apply(existing);
            var validation = shadow.Apply(update);
            var floor = ProviderPublisherTrustPolicyRecoveryFloor.Issue(_registry.AuthorityIdentity, _registry.Current);
            if (!validation.IsApplied) return new(validation.Code, _registry.Current, floor);
            ProviderPublisherTrustEvaluator.TrySnapshot(update.Policy, out var policy);
            var snapshot = update with { Policy = policy };
            var successor = new List<ProviderPublisherTrustPolicyUpdate>(_updates) { snapshot };
            if (!CheckpointCodec.TryWrite(_path, _registry.AuthorityIdentity, successor))
                return new("policy-checkpoint-write-failed", _registry.Current, floor);
            var applied = _registry.Apply(snapshot);
            if (!applied.IsApplied) throw new InvalidOperationException("A published checkpoint must apply to its unchanged live registry.");
            _updates.Add(snapshot);
            return new(applied.Code, applied.Current,
                ProviderPublisherTrustPolicyRecoveryFloor.Issue(_registry.AuthorityIdentity, applied.Current));
        }
    }

    public GovernedProviderArtifactAcquirer Govern(TrustedProviderArtifactAcquirer acquirer) =>
        new(_registry, acquirer);

    private static bool IsRollback(
        VerifiedProviderPublisherTrustPolicySnapshot? current,
        ProviderPublisherTrustPolicyRecoveryFloor floor) =>
        current is null
            ? floor.Sequence > 0
            : current.Sequence < floor.Sequence
              || (current.Sequence == floor.Sequence && current.Policy.Identity != floor.PolicyIdentity);

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
    }

    private static class CheckpointCodec
    {
        private const int MaxBytes = 1024 * 1024;
        private const int MaxUpdates = 4096;
        private const int MaxEntries = 4096;

        internal static bool TryWrite(
            string path,
            ProviderPublisherTrustPolicyAuthorityId authority,
            IReadOnlyList<ProviderPublisherTrustPolicyUpdate> updates)
        {
            var temporary = path + ".tmp";
            try
            {
                if (updates.Count > MaxUpdates || updates.Any(update => update.Policy.Entries.Count > MaxEntries))
                    return false;
                var parent = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(parent);
                using var memory = new MemoryStream();
                Write(memory, "CBI38");
                Write(memory, authority.Value);
                Write(memory, updates.Count);
                foreach (var update in updates)
                {
                    Write(memory, update.Sequence);
                    Write(memory, update.PreviousPolicyIdentity.HasValue ? 1 : 0);
                    if (update.PreviousPolicyIdentity.HasValue) Write(memory, update.PreviousPolicyIdentity.Value.Value);
                    Write(memory, update.Policy.Identity.Value);
                    var entries = update.Policy.Entries.OrderBy(value => value.PublisherKeyId.Value, StringComparer.Ordinal).ToArray();
                    Write(memory, entries.Length);
                    foreach (var entry in entries)
                    {
                        Write(memory, entry.PublisherKeyId.Value);
                        Write(memory, entry.Disposition == ProviderPublisherTrustDisposition.Admitted ? "admitted" : "revoked");
                    }
                    Write(memory, update.Algorithm);
                    Write(memory, update.AuthorityPublicKeySpkiBase64);
                    Write(memory, update.SignatureBase64);
                }
                if (memory.Length > MaxBytes) return false;
                using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None,
                           4096, FileOptions.WriteThrough))
                {
                    memory.Position = 0;
                    memory.CopyTo(output);
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

        internal static bool TryRead(
            string path,
            out ProviderPublisherTrustPolicyAuthorityId authority,
            out List<ProviderPublisherTrustPolicyUpdate> updates)
        {
            authority = default;
            updates = [];
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0 || bytes.Length > MaxBytes) return false;
                var reader = new Reader(bytes);
                if (reader.String() != "CBI38") return false;
                authority = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
                var count = reader.Int32();
                if (count < 0 || count > MaxUpdates) return false;
                for (var index = 0; index < count; index++)
                {
                    var sequence = reader.Int64();
                    var hasPrevious = reader.Int32();
                    if (hasPrevious is not 0 and not 1) return false;
                    ProviderPublisherTrustPolicyId? previous = hasPrevious == 1
                        ? ProviderPublisherTrustPolicyId.Create(reader.String()) : null;
                    var identity = ProviderPublisherTrustPolicyId.Create(reader.String());
                    var entryCount = reader.Int32();
                    if (entryCount < 0 || entryCount > MaxEntries) return false;
                    var entries = new ProviderPublisherTrustEntry[entryCount];
                    for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
                    {
                        var key = ProviderPublisherKeyId.Create(reader.String());
                        entries[entryIndex] = new(key, reader.String() switch
                        {
                            "admitted" => ProviderPublisherTrustDisposition.Admitted,
                            "revoked" => ProviderPublisherTrustDisposition.Revoked,
                            _ => throw new InvalidDataException(),
                        });
                    }
                    updates.Add(new(sequence, previous, new(identity, entries), reader.String(), reader.String(), reader.String()));
                }
                return reader.End;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or InvalidDataException or ArgumentException or DecoderFallbackException)
            {
                authority = default;
                updates = [];
                return false;
            }
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
            var bytes = Encoding.UTF8.GetBytes(value);
            Write(output, bytes.Length);
            output.Write(bytes);
        }

        private ref struct Reader
        {
            private readonly ReadOnlySpan<byte> _bytes;
            private int _offset;
            internal Reader(ReadOnlySpan<byte> bytes) { _bytes = bytes; _offset = 0; }
            internal bool End => _offset == _bytes.Length;
            internal int Int32() { Ensure(4); var value = BinaryPrimitives.ReadInt32BigEndian(_bytes[_offset..]); _offset += 4; return value; }
            internal long Int64() { Ensure(8); var value = BinaryPrimitives.ReadInt64BigEndian(_bytes[_offset..]); _offset += 8; return value; }
            internal string String()
            {
                var length = Int32();
                if (length < 0 || length > MaxBytes) throw new InvalidDataException();
                Ensure(length);
                var value = new UTF8Encoding(false, true).GetString(_bytes.Slice(_offset, length));
                _offset += length;
                return value;
            }
            private void Ensure(int length) { if (length > _bytes.Length - _offset) throw new InvalidDataException(); }
        }
    }
}
