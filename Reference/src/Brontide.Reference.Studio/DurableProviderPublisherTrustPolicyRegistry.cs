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

    /// <summary>
    /// Rebuilds a floor from a durable record. A stored floor names a policy identity without the
    /// policy behind it, so it cannot be reissued from a snapshot the way a live one is.
    /// </summary>
    internal static ProviderPublisherTrustPolicyRecoveryFloor Restore(
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        long sequence,
        ProviderPublisherTrustPolicyId? policyIdentity) =>
        new(authorityIdentity, sequence, policyIdentity);
}

/// <summary>
/// Guards the authority generation the retained chain reaches. It is deliberately separate from the
/// policy recovery floor: the two describe different monotone facts and have different custodians.
/// </summary>
public sealed record ProviderPolicyAuthorityFloor
{
    private ProviderPolicyAuthorityFloor(
        long generation,
        ProviderPublisherTrustPolicyAuthorityId activeAuthority)
    {
        Generation = generation;
        ActiveAuthority = activeAuthority;
    }

    public long Generation { get; }
    public ProviderPublisherTrustPolicyAuthorityId ActiveAuthority { get; }

    internal static ProviderPolicyAuthorityFloor Issue(
        long generation,
        ProviderPublisherTrustPolicyAuthorityId activeAuthority) => new(generation, activeAuthority);

    public static ProviderPolicyAuthorityFloor Restore(
        long generation,
        ProviderPublisherTrustPolicyAuthorityId activeAuthority)
    {
        if (generation < 0 || activeAuthority.Value is null)
            throw new ArgumentException("A valid policy authority floor is required.");
        return new(generation, activeAuthority);
    }
}

public sealed record DurableProviderPublisherTrustPolicyResult(
    string Code,
    DurableProviderPublisherTrustPolicyRegistry? Registry,
    ProviderPublisherTrustPolicyRecoveryFloor? Floor,
    ProviderPolicyAuthorityFloor? AuthorityFloor = null);

public sealed record DurableProviderPublisherTrustPolicyUpdateResult(
    string Code,
    VerifiedProviderPublisherTrustPolicySnapshot? Current,
    ProviderPublisherTrustPolicyRecoveryFloor Floor)
{
    public bool IsApplied => Code == "policy-update-applied";
}

public sealed record DurableProviderPolicyAuthorityRotationResult(
    string Code,
    long Generation,
    ProviderPublisherTrustPolicyAuthorityId ActiveAuthority,
    ProviderPolicyAuthorityFloor Floor)
{
    public bool IsApplied => Code == "policy-authority-rotation-applied";
}

/// <summary>
/// One retained link: either a policy update or the authority rotation that decides who may sign the
/// updates after it. Both live in one record because recovery has to re-verify each update against
/// the authority in force at its own position, which only their shared order states.
/// </summary>
internal sealed class ProviderPolicyChainLink
{
    private ProviderPolicyChainLink(
        ProviderPublisherTrustPolicyUpdate? update,
        ProviderPolicyAuthorityRotationStatement? rotation)
    {
        Update = update;
        Rotation = rotation;
    }

    internal ProviderPublisherTrustPolicyUpdate? Update { get; }
    internal ProviderPolicyAuthorityRotationStatement? Rotation { get; }

    internal static ProviderPolicyChainLink Of(ProviderPublisherTrustPolicyUpdate update) => new(update, null);
    internal static ProviderPolicyChainLink Of(ProviderPolicyAuthorityRotationStatement rotation) => new(null, rotation);
}

public sealed class DurableProviderPublisherTrustPolicyRegistry
{
    private readonly object _sync = new();
    private readonly string _path;
    private readonly ProviderPublisherTrustPolicyRegistry _registry;
    private readonly List<ProviderPolicyChainLink> _links;

    private DurableProviderPublisherTrustPolicyRegistry(
        string path,
        ProviderPublisherTrustPolicyRegistry registry,
        List<ProviderPolicyChainLink> links)
    {
        _path = path;
        _registry = registry;
        _links = links;
    }

    public VerifiedProviderPublisherTrustPolicySnapshot? Current => _registry.Current;

    public ProviderPublisherTrustPolicyAuthorityId AuthorityIdentity => _registry.AuthorityIdentity;

    public ProviderPublisherTrustPolicyAuthorityId ActiveAuthorityIdentity => _registry.ActiveAuthorityIdentity;

    public long AuthorityGeneration => _registry.AuthorityGeneration;

    public ProviderPublisherTrustPolicyRecoveryFloor Floor =>
        ProviderPublisherTrustPolicyRecoveryFloor.Issue(_registry.AuthorityIdentity, _registry.Current);

    public ProviderPolicyAuthorityFloor AuthorityFloor =>
        ProviderPolicyAuthorityFloor.Issue(_registry.AuthorityGeneration, _registry.ActiveAuthorityIdentity);

    public static DurableProviderPublisherTrustPolicyResult Open(
        string path,
        ProviderPublisherTrustPolicyAuthorityId authorityIdentity,
        ProviderPublisherTrustPolicyRecoveryFloor? floor = null,
        ProviderPolicyAuthorityFloor? authorityFloor = null)
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
            if (authorityFloor is not null && (authorityFloor.Generation > 0
                || authorityFloor.ActiveAuthority != authorityIdentity))
                return new("policy-authority-rollback-detected", null, null);
            var emptyRegistry = new ProviderPublisherTrustPolicyRegistry(authorityIdentity);
            var empty = new DurableProviderPublisherTrustPolicyRegistry(path, emptyRegistry, []);
            return new("policy-checkpoint-empty", empty,
                ProviderPublisherTrustPolicyRecoveryFloor.Issue(authorityIdentity, null),
                empty.AuthorityFloor);
        }

        if (!CheckpointCodec.TryRead(path, out var storedAuthority, out var links))
            return new("policy-checkpoint-corrupt", null, null);
        if (storedAuthority != authorityIdentity)
            return new("policy-checkpoint-authority-mismatch", null, null);
        if (!TryReplay(authorityIdentity, links, out var registry))
            return new("policy-checkpoint-invalid-chain", null, null);
        var current = registry.Current;
        if (floor is not null && IsRollback(current, floor))
            return new("policy-checkpoint-rollback-detected", null, null);
        if (authorityFloor is not null && (registry.AuthorityGeneration < authorityFloor.Generation
            || (registry.AuthorityGeneration == authorityFloor.Generation
                && registry.ActiveAuthorityIdentity != authorityFloor.ActiveAuthority)))
            return new("policy-authority-rollback-detected", null, null);
        var durable = new DurableProviderPublisherTrustPolicyRegistry(path, registry, links);
        return new("policy-checkpoint-recovered", durable,
            ProviderPublisherTrustPolicyRecoveryFloor.Issue(authorityIdentity, current),
            durable.AuthorityFloor);
    }

    public DurableProviderPublisherTrustPolicyUpdateResult Apply(ProviderPublisherTrustPolicyUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        lock (_sync)
        {
            var floor = ProviderPublisherTrustPolicyRecoveryFloor.Issue(_registry.AuthorityIdentity, _registry.Current);
            TryReplay(_registry.AuthorityIdentity, _links, out var shadow);
            var validation = shadow.Apply(update);
            if (!validation.IsApplied) return new(validation.Code, _registry.Current, floor);
            ProviderPublisherTrustEvaluator.TrySnapshot(update.Policy, out var policy);
            var snapshot = update with { Policy = policy };
            var successor = new List<ProviderPolicyChainLink>(_links) { ProviderPolicyChainLink.Of(snapshot) };
            if (!CheckpointCodec.TryWrite(_path, _registry.AuthorityIdentity, successor))
                return new("policy-checkpoint-write-failed", _registry.Current, floor);
            var applied = _registry.Apply(snapshot);
            if (!applied.IsApplied) throw new InvalidOperationException("A published checkpoint must apply to its unchanged live registry.");
            _links.Add(ProviderPolicyChainLink.Of(snapshot));
            return new(applied.Code, applied.Current,
                ProviderPublisherTrustPolicyRecoveryFloor.Issue(_registry.AuthorityIdentity, applied.Current));
        }
    }

    /// <summary>
    /// Publishes one authority transition and only then advances the live authority, in the order
    /// CBI38 applies to a policy update: a live authority no checkpoint records would be forgotten by
    /// the next recovery, which is the direction that loses work rather than the one that repeats it.
    /// </summary>
    public DurableProviderPolicyAuthorityRotationResult Rotate(ProviderPolicyAuthorityRotationStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        lock (_sync)
        {
            var floor = AuthorityFloor;
            TryReplay(_registry.AuthorityIdentity, _links, out var shadow);
            var validation = shadow.Rotate(statement);
            if (!validation.IsApplied)
                return new(validation.Code, _registry.AuthorityGeneration, _registry.ActiveAuthorityIdentity, floor);
            var successor = new List<ProviderPolicyChainLink>(_links) { ProviderPolicyChainLink.Of(statement) };
            if (!CheckpointCodec.TryWrite(_path, _registry.AuthorityIdentity, successor))
                return new("policy-checkpoint-write-failed", _registry.AuthorityGeneration,
                    _registry.ActiveAuthorityIdentity, floor);
            var applied = _registry.Rotate(statement);
            if (!applied.IsApplied) throw new InvalidOperationException("A published rotation must apply to its unchanged live registry.");
            _links.Add(ProviderPolicyChainLink.Of(statement));
            return new(applied.Code, applied.Generation, applied.ActiveAuthority, AuthorityFloor);
        }
    }

    public GovernedProviderArtifactAcquirer Govern(TrustedProviderArtifactAcquirer acquirer) =>
        new(_registry, acquirer);

    /// <summary>
    /// Replays the retained chain in stored order, so each update is verified against the authority
    /// the preceding rotations put in force rather than against the pin alone.
    /// </summary>
    private static bool TryReplay(
        ProviderPublisherTrustPolicyAuthorityId pin,
        IReadOnlyList<ProviderPolicyChainLink> links,
        out ProviderPublisherTrustPolicyRegistry registry)
    {
        registry = new(pin);
        foreach (var link in links)
        {
            var applied = link.Update is not null
                ? registry.Apply(link.Update).IsApplied
                : registry.Rotate(link.Rotation!).IsApplied;
            if (!applied) return false;
        }
        return true;
    }

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
        private const int MaxLinks = 4096;
        private const int MaxEntries = 4096;
        private const int UpdateTag = 0;
        private const int RotationTag = 1;

        /// <summary>
        /// Writes the CBI38 record while the chain holds only updates and the tagged CBI57 record once
        /// it holds a rotation, so a host that never rotates keeps a record shape earlier evidence
        /// pins and an existing checkpoint stays readable.
        /// </summary>
        internal static bool TryWrite(
            string path,
            ProviderPublisherTrustPolicyAuthorityId authority,
            IReadOnlyList<ProviderPolicyChainLink> links)
        {
            var temporary = path + ".tmp";
            try
            {
                if (links.Count > MaxLinks
                    || links.Any(link => link.Update?.Policy.Entries.Count > MaxEntries))
                    return false;
                var rotated = links.Any(link => link.Rotation is not null);
                var parent = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(parent);
                using var memory = new MemoryStream();
                Write(memory, rotated ? "CBI57" : "CBI38");
                Write(memory, authority.Value);
                Write(memory, links.Count);
                foreach (var link in links)
                {
                    if (rotated) Write(memory, link.Rotation is null ? UpdateTag : RotationTag);
                    if (link.Rotation is { } rotation) { WriteRotation(memory, rotation); continue; }
                    var update = link.Update!;
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
            out List<ProviderPolicyChainLink> links)
        {
            authority = default;
            links = [];
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0 || bytes.Length > MaxBytes) return false;
                var reader = new Reader(bytes);
                var format = reader.String();
                if (format is not "CBI38" and not "CBI57") return false;
                var tagged = format == "CBI57";
                authority = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
                var count = reader.Int32();
                if (count < 0 || count > MaxLinks) return false;
                for (var index = 0; index < count; index++)
                {
                    var tag = tagged ? reader.Int32() : UpdateTag;
                    if (tag is not UpdateTag and not RotationTag) return false;
                    if (tag == RotationTag)
                    {
                        links.Add(ProviderPolicyChainLink.Of(ReadRotation(ref reader)));
                        continue;
                    }
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
                    links.Add(ProviderPolicyChainLink.Of(new ProviderPublisherTrustPolicyUpdate(
                        sequence, previous, new(identity, entries), reader.String(), reader.String(), reader.String())));
                }
                return reader.End;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or InvalidDataException or ArgumentException or DecoderFallbackException)
            {
                authority = default;
                links = [];
                return false;
            }
        }

        private static void WriteRotation(Stream output, ProviderPolicyAuthorityRotationStatement rotation)
        {
            Write(output, rotation.Generation);
            Write(output, rotation.PolicySequence);
            Write(output, rotation.PolicyIdentity.HasValue ? 1 : 0);
            if (rotation.PolicyIdentity.HasValue) Write(output, rotation.PolicyIdentity.Value.Value);
            Write(output, rotation.PreviousAuthority.Value);
            Write(output, rotation.NextAuthority.Value);
            Write(output, rotation.Algorithm);
            Write(output, rotation.PreviousAuthorityPublicKeySpkiBase64);
            Write(output, rotation.NextAuthorityPublicKeySpkiBase64);
            Write(output, rotation.PreviousSignatureBase64);
            Write(output, rotation.NextSignatureBase64);
        }

        private static ProviderPolicyAuthorityRotationStatement ReadRotation(ref Reader reader)
        {
            var generation = reader.Int64();
            var policySequence = reader.Int64();
            var hasPolicy = reader.Int32();
            if (hasPolicy is not 0 and not 1) throw new InvalidDataException();
            ProviderPublisherTrustPolicyId? policyIdentity = hasPolicy == 1
                ? ProviderPublisherTrustPolicyId.Create(reader.String()) : null;
            var previous = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
            var next = ProviderPublisherTrustPolicyAuthorityId.Create(reader.String());
            return new(generation, policySequence, policyIdentity, previous, next,
                reader.String(), reader.String(), reader.String(), reader.String(), reader.String());
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
