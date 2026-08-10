using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Experimental.PersistentInformation;

public readonly record struct CorpusId
{
    private CorpusId(string value) => Value = value;
    public string Value { get; }
    public static CorpusId Parse(string value) => new(Identity.Validate(value, nameof(value)));
    public override string ToString() => Value;
}

public readonly record struct DatasetId
{
    private DatasetId(string value) => Value = value;
    public string Value { get; }
    public static DatasetId Parse(string value) => new(Identity.Validate(value, nameof(value)));
    public override string ToString() => Value;
}

public readonly record struct StoreRoleId
{
    private StoreRoleId(string value) => Value = value;
    public string Value { get; }
    public static StoreRoleId Parse(string value) => new(Identity.Validate(value, nameof(value)));
    public override string ToString() => Value;
}

public readonly record struct StoreId
{
    private StoreId(string value) => Value = value;
    public string Value { get; }
    public static StoreId Parse(string value) => new(Identity.Validate(value, nameof(value)));
    public override string ToString() => Value;
}

public readonly record struct RouterId
{
    private RouterId(string value) => Value = value;
    public string Value { get; }
    public static RouterId Parse(string value) => new(Identity.Validate(value, nameof(value)));
    public override string ToString() => Value;
}

internal static class Identity
{
    internal static string Validate(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("An identity cannot contain leading or trailing whitespace.", parameterName);
        }

        return value;
    }
}

public enum ConcurrentAccessMode
{
    SingleWriter,
    ExternalCoordination,
}

public enum StoreRoleAbsenceBehavior
{
    DatasetUnavailable,
    RoleUnavailable,
}

public enum EndpointGuarantee
{
    Durable,
    Encrypted,
    Local,
}

public sealed record StoreRoleDefinition(
    StoreRoleId Id,
    bool IdentityBearing,
    bool Required,
    StoreRoleAbsenceBehavior AbsenceBehavior);

public sealed record PersistentFailure(string Code, string Reason);

public sealed record PersistentResult<T>(T? Value, PersistentFailure? Failure)
{
    public bool IsSuccess => Failure is null;
    public string Code => Failure?.Code ?? "ok";

    public static PersistentResult<T> Success(T value) => new(value, null);
    public static PersistentResult<T> Refused(string code, string reason) => new(default, new(code, reason));
}

public sealed record OpaqueCorpus
{
    private OpaqueCorpus(
        CorpusId id,
        string version,
        ConcurrentAccessMode concurrentAccess,
        ImmutableArray<StoreRoleDefinition> roles)
    {
        Id = id;
        Version = version;
        ConcurrentAccess = concurrentAccess;
        Roles = roles;
    }

    public CorpusId Id { get; }
    public string Version { get; }
    public ConcurrentAccessMode ConcurrentAccess { get; }
    public ImmutableArray<StoreRoleDefinition> Roles { get; }

    public static PersistentResult<OpaqueCorpus> Create(
        CorpusId id,
        string version,
        ConcurrentAccessMode? concurrentAccess,
        IEnumerable<StoreRoleDefinition> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (string.IsNullOrWhiteSpace(version) || concurrentAccess is null)
        {
            return PersistentResult<OpaqueCorpus>.Refused("corpus-invalid", "A Corpus requires a version and concurrent-access declaration.");
        }

        if (concurrentAccess != ConcurrentAccessMode.SingleWriter)
        {
            return PersistentResult<OpaqueCorpus>.Refused("concurrency-unsupported", "This experiment enforces only single-writer access.");
        }

        var declared = roles.ToImmutableArray();
        if (declared.IsDefaultOrEmpty || declared.Any(role => role is null) ||
            declared.Select(role => role.Id).Distinct().Count() != declared.Length ||
            !declared.Any(role => role.IdentityBearing))
        {
            return PersistentResult<OpaqueCorpus>.Refused(
                "corpus-invalid",
                "A Corpus requires distinct Store roles and at least one identity-bearing role.");
        }

        return PersistentResult<OpaqueCorpus>.Success(new(id, version, concurrentAccess.Value, declared));
    }
}

public interface IStoreEndpoint
{
    IReadOnlySet<EndpointGuarantee> Guarantees { get; }
    bool IsAvailable { get; }
    PersistentResult<int> Append(string value);
    PersistentResult<IReadOnlyList<string>> ReadResult();
}

public sealed class InMemoryStore : IStoreEndpoint
{
    private readonly List<string> _values = [];
    private readonly ImmutableHashSet<EndpointGuarantee> _guarantees;

    public InMemoryStore(StoreId id, params EndpointGuarantee[] guarantees)
    {
        ArgumentNullException.ThrowIfNull(guarantees);
        Id = id;
        _guarantees = guarantees.ToImmutableHashSet();
    }

    public StoreId Id { get; }
    public IReadOnlySet<EndpointGuarantee> Guarantees => _guarantees;
    public bool IsAvailable { get; set; } = true;
    public int AppendCount { get; private set; }

    public PersistentResult<int> Append(string value)
    {
        if (!IsAvailable)
        {
            return PersistentResult<int>.Refused("store-unavailable", $"Store '{Id}' is unavailable.");
        }

        ArgumentNullException.ThrowIfNull(value);
        _values.Add(value);
        AppendCount++;
        return PersistentResult<int>.Success(_values.Count);
    }

    public PersistentResult<IReadOnlyList<string>> ReadResult() => IsAvailable
        ? PersistentResult<IReadOnlyList<string>>.Success(_values.ToImmutableArray())
        : PersistentResult<IReadOnlyList<string>>.Refused("store-unavailable", $"Store '{Id}' is unavailable.");

    public IReadOnlyList<string> Read() => _values.ToImmutableArray();
    public void Clear() => _values.Clear();
}

public sealed record DatasetIssuance(ActorReference Issuer, OperationReference IssuingOperation);

public sealed record DatasetRecord(
    DatasetId Id,
    CorpusId Corpus,
    string CorpusVersion,
    ActorReference Issuer,
    OperationReference IssuingOperation,
    ConcurrentAccessMode ConcurrentAccess,
    ImmutableDictionary<StoreRoleId, IStoreEndpoint> RoleBindings,
    ImmutableHashSet<StoreRoleId> IdentityBearingRoles);

public sealed class DatasetRegistry
{
    private readonly Dictionary<DatasetId, DatasetRecord> _datasets = [];

    public IReadOnlyCollection<DatasetRecord> Datasets => _datasets.Values.ToImmutableArray();

    public PersistentResult<DatasetRecord> Issue(
        DatasetIssuance issuance,
        OpaqueCorpus corpus,
        DatasetId dataset,
        IReadOnlyDictionary<StoreRoleId, IStoreEndpoint> bindings)
    {
        ArgumentNullException.ThrowIfNull(issuance);
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(bindings);
        if (_datasets.ContainsKey(dataset))
        {
            return PersistentResult<DatasetRecord>.Refused("dataset-invalid", $"Dataset '{dataset}' already exists.");
        }

        foreach (var role in corpus.Roles)
        {
            if (role.Required && (!bindings.TryGetValue(role.Id, out var endpoint) || endpoint is null))
            {
                return PersistentResult<DatasetRecord>.Refused("role-unavailable", $"Required role '{role.Id}' has no logical Store endpoint.");
            }
        }

        if (bindings.Keys.Any(role => corpus.Roles.All(declaration => declaration.Id != role)))
        {
            return PersistentResult<DatasetRecord>.Refused("role-not-found", "A binding names a role the Corpus does not declare.");
        }

        var record = new DatasetRecord(
            dataset,
            corpus.Id,
            corpus.Version,
            issuance.Issuer,
            issuance.IssuingOperation,
            corpus.ConcurrentAccess,
            bindings.ToImmutableDictionary(),
            corpus.Roles.Where(role => role.IdentityBearing).Select(role => role.Id).ToImmutableHashSet());
        _datasets.Add(dataset, record);
        return PersistentResult<DatasetRecord>.Success(record);
    }

    public PersistentResult<int> Append(
        DatasetId dataset,
        StoreRoleId role,
        ConcurrentAccessMode requestedConcurrency,
        string value)
    {
        var endpoint = Resolve(dataset, role, requestedConcurrency);
        return endpoint.IsSuccess
            ? endpoint.Value!.Append(value)
            : PersistentResult<int>.Refused(endpoint.Code, endpoint.Failure!.Reason);
    }

    public PersistentResult<IReadOnlyList<string>> Read(
        DatasetId dataset,
        StoreRoleId role,
        ConcurrentAccessMode requestedConcurrency)
    {
        var endpoint = Resolve(dataset, role, requestedConcurrency);
        return endpoint.IsSuccess
            ? endpoint.Value!.ReadResult()
            : PersistentResult<IReadOnlyList<string>>.Refused(endpoint.Code, endpoint.Failure!.Reason);
    }

    private PersistentResult<IStoreEndpoint> Resolve(
        DatasetId dataset,
        StoreRoleId role,
        ConcurrentAccessMode requestedConcurrency)
    {
        if (!_datasets.TryGetValue(dataset, out var record))
        {
            return PersistentResult<IStoreEndpoint>.Refused("dataset-not-found", $"Dataset '{dataset}' is unknown.");
        }

        if (!record.RoleBindings.TryGetValue(role, out var endpoint))
        {
            return PersistentResult<IStoreEndpoint>.Refused("role-not-found", $"Role '{role}' is not bound for Dataset '{dataset}'.");
        }

        if (requestedConcurrency != record.ConcurrentAccess)
        {
            return PersistentResult<IStoreEndpoint>.Refused("concurrency-mismatch", "The requested access mode differs from the Corpus declaration.");
        }

        return PersistentResult<IStoreEndpoint>.Success(endpoint);
    }
}

public sealed record RouterDescription(
    RouterId Id,
    ImmutableHashSet<EndpointGuarantee> Guarantees,
    StoreId? SelectedBacking);

public sealed class RouterEndpoint : IStoreEndpoint
{
    private readonly ImmutableArray<InMemoryStore> _backings;
    private readonly ImmutableHashSet<EndpointGuarantee> _guarantees;
    private readonly bool _exposeTopology;
    private int _selectedIndex;

    private RouterEndpoint(
        RouterId id,
        ImmutableHashSet<EndpointGuarantee> guarantees,
        ImmutableArray<InMemoryStore> backings,
        bool exposeTopology)
    {
        Id = id;
        _guarantees = guarantees;
        _backings = backings;
        _exposeTopology = exposeTopology;
    }

    public RouterId Id { get; }
    public IReadOnlySet<EndpointGuarantee> Guarantees => _guarantees;
    public bool IsAvailable => OrderedBackings().Any(store => store.IsAvailable);

    public static PersistentResult<RouterEndpoint> Create(
        RouterId id,
        IEnumerable<EndpointGuarantee> guarantees,
        IEnumerable<InMemoryStore> backings,
        bool exposeTopology)
    {
        ArgumentNullException.ThrowIfNull(guarantees);
        ArgumentNullException.ThrowIfNull(backings);
        var declared = guarantees.ToImmutableHashSet();
        var stores = backings.ToImmutableArray();
        if (stores.IsDefaultOrEmpty || stores.Select(store => store.Id).Distinct().Count() != stores.Length)
        {
            return PersistentResult<RouterEndpoint>.Refused("router-invalid", "A Router requires distinct declared backing Stores.");
        }

        if (stores.Any(store => !declared.IsSubsetOf(store.Guarantees)))
        {
            return PersistentResult<RouterEndpoint>.Refused(
                "router-guarantee-unsupported",
                "Every backing and fallback path must uphold every Router endpoint guarantee.");
        }

        return PersistentResult<RouterEndpoint>.Success(new(id, declared, stores, exposeTopology));
    }

    public PersistentResult<RouterEndpoint> Select(StoreId store)
    {
        var index = -1;
        for (var candidateIndex = 0; candidateIndex < _backings.Length; candidateIndex++)
        {
            if (_backings[candidateIndex].Id == store)
            {
                index = candidateIndex;
                break;
            }
        }
        if (index < 0)
        {
            return PersistentResult<RouterEndpoint>.Refused("router-invalid", $"Store '{store}' is not a declared backing.");
        }

        _selectedIndex = index;
        return PersistentResult<RouterEndpoint>.Success(this);
    }

    public PersistentResult<int> Append(string value)
    {
        var store = OrderedBackings().FirstOrDefault(candidate => candidate.IsAvailable);
        return store is null
            ? PersistentResult<int>.Refused("store-unavailable", "No declared Router backing is available.")
            : store.Append(value);
    }

    public PersistentResult<IReadOnlyList<string>> ReadResult()
    {
        var store = OrderedBackings().FirstOrDefault(candidate => candidate.IsAvailable);
        return store is null
            ? PersistentResult<IReadOnlyList<string>>.Refused("store-unavailable", "No declared Router backing is available.")
            : store.ReadResult();
    }

    public RouterDescription Describe(bool managementAuthorized) => new(
        Id,
        _guarantees,
        managementAuthorized && _exposeTopology ? _backings[_selectedIndex].Id : null);

    private IEnumerable<InMemoryStore> OrderedBackings()
    {
        yield return _backings[_selectedIndex];
        for (var index = 0; index < _backings.Length; index++)
        {
            if (index != _selectedIndex)
            {
                yield return _backings[index];
            }
        }
    }
}
