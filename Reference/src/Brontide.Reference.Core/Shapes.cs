using System.Collections.Immutable;

namespace Brontide.Reference.Core;

public enum ShapeKind
{
    Unit,
    Scalar,
    Record,
    Sequence,
    Choice,
    Opaque
}

public enum FragmentPolicy
{
    Open,
    Closed
}

public static class BuiltInShapes
{
    public static readonly ShapeReference Unit = ShapeReference.Parse("Unit", 1);
    public static readonly ShapeReference Boolean = ShapeReference.Parse("Boolean", 1);
    public static readonly ShapeReference Signed64 = ShapeReference.Parse("Integer.Signed64", 1);
    public static readonly ShapeReference Text = ShapeReference.Parse("Text", 1);
    public static readonly ShapeReference Bytes = ShapeReference.Parse("Opaque.Bytes", 1);
    public static readonly ShapeReference OperationSet = ShapeReference.Parse("Brontide:OperationSet", 1);
    public static readonly ShapeReference TimeWindow = ShapeReference.Parse("Brontide:TimeWindow", 1);
    public static readonly ShapeReference Lease = ShapeReference.Parse("Brontide:Lease", 1);
    public static readonly ShapeReference OriginClass = ShapeReference.Parse("Brontide:OriginClass", 1);
    public static readonly ShapeReference Details = ShapeReference.Parse("Brontide:Details", 1);
    public static readonly ShapeReference Activity = ShapeReference.Parse("Brontide:Activity", 1);
}

public sealed class ShapeContract
{
    private ShapeContract(ShapeReference canonical, IEnumerable<FragmentReference> requiredFragments)
    {
        Canonical = canonical;
        RequiredFragments = requiredFragments
            .Distinct()
            .OrderBy(fragment => fragment.Name)
            .ThenBy(fragment => fragment.Version)
            .ToImmutableArray();
    }

    public ShapeReference Canonical { get; }
    public ImmutableArray<FragmentReference> RequiredFragments { get; }
    public static ShapeContract Unit { get; } = For(BuiltInShapes.Unit);

    public static ShapeContract For(ShapeReference canonical, params FragmentReference[] requiredFragments) =>
        new(canonical, requiredFragments);

    public override string ToString() => RequiredFragments.Length == 0
        ? Canonical.ToString()
        : $"{Canonical} + {string.Join(" + ", RequiredFragments)}";
}

public sealed record RecordField(string Name, ShapeReference Shape, bool IsRequired)
{
    public static RecordField Required(string name, ShapeReference shape) => new(ValidateName(name), shape, true);
    public static RecordField Optional(string name, ShapeReference shape) => new(ValidateName(name), shape, false);

    private static string ValidateName(string name) => string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("A field name is required.", nameof(name))
        : name;
}

public sealed class ShapeDefinition
{
    private ShapeDefinition(
        ShapeReference reference,
        ShapeKind kind,
        FragmentPolicy? fragmentPolicy = null,
        IEnumerable<RecordField>? fields = null,
        ShapeReference? elementShape = null,
        IEnumerable<KeyValuePair<string, ShapeReference>>? alternatives = null,
        IEnumerable<FragmentReference>? includedFragments = null,
        Type? scalarRepresentation = null)
    {
        Reference = reference;
        Kind = kind;
        FragmentPolicy = fragmentPolicy;
        Fields = (fields ?? []).ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
        ElementShape = elementShape;
        Alternatives = (alternatives ?? []).ToImmutableDictionary(StringComparer.Ordinal);
        IncludedFragments = (includedFragments ?? []).Distinct().ToImmutableArray();
        ScalarRepresentation = scalarRepresentation;
    }

    public ShapeReference Reference { get; }
    public ShapeKind Kind { get; }
    public FragmentPolicy? FragmentPolicy { get; }
    public ImmutableDictionary<string, RecordField> Fields { get; }
    public ShapeReference? ElementShape { get; }
    public ImmutableDictionary<string, ShapeReference> Alternatives { get; }
    public ImmutableArray<FragmentReference> IncludedFragments { get; }
    internal Type? ScalarRepresentation { get; }

    public static ShapeDefinition Unit(ShapeReference reference) => new(reference, ShapeKind.Unit);
    public static ShapeDefinition Scalar<T>(ShapeReference reference) =>
        new(reference, ShapeKind.Scalar, scalarRepresentation: ScalarRepresentations.RequireSupported(typeof(T)));
    public static ShapeDefinition Opaque(ShapeReference reference) => new(reference, ShapeKind.Opaque);
    public static ShapeDefinition Sequence(ShapeReference reference, ShapeReference element) =>
        new(reference, ShapeKind.Sequence, elementShape: element);

    public static ShapeDefinition Choice(
        ShapeReference reference,
        params KeyValuePair<string, ShapeReference>[] alternatives) =>
        new(reference, ShapeKind.Choice, alternatives: alternatives);

    public static ShapeDefinition Record(
        ShapeReference reference,
        FragmentPolicy fragmentPolicy,
        params RecordField[] fields) =>
        new(reference, ShapeKind.Record, fragmentPolicy, fields);

    public static ShapeDefinition RecordIncluding(
        ShapeReference reference,
        FragmentPolicy fragmentPolicy,
        IEnumerable<RecordField> fields,
        IEnumerable<FragmentReference> includedFragments) =>
        new(reference, ShapeKind.Record, fragmentPolicy, fields, includedFragments: includedFragments);
}

internal static class ScalarRepresentations
{
    private static readonly ImmutableHashSet<Type> Supported = new[]
    {
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(char),
        typeof(string),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan)
    }.ToImmutableHashSet();

    public static Type RequireSupported(Type representation)
    {
        ArgumentNullException.ThrowIfNull(representation);
        if (!Supported.Contains(representation))
        {
            throw new ArgumentException(
                $"Scalar representation {representation.FullName} is not an approved immutable carrier. " +
                "Use records, sequences, choices, or opaque bytes for other values.",
                nameof(representation));
        }

        return representation;
    }

    public static object RequireSupportedValue(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _ = RequireSupported(value.GetType());
        return value;
    }
}

public sealed class DeclaredFragmentDefinition
{
    private DeclaredFragmentDefinition(
        FragmentReference reference,
        ShapeReference? earliestHost,
        bool isAuthoredAttachment,
        IEnumerable<RecordField> fields)
    {
        Reference = reference;
        EarliestHost = earliestHost;
        IsAuthoredAttachment = isAuthoredAttachment;
        Fields = fields.ToImmutableDictionary(field => field.Name, StringComparer.Ordinal);
    }

    public FragmentReference Reference { get; }
    public ShapeReference? EarliestHost { get; }
    public bool IsAuthoredAttachment { get; }
    public ImmutableDictionary<string, RecordField> Fields { get; }

    public static DeclaredFragmentDefinition Attached(
        FragmentReference reference,
        ShapeReference earliestHost,
        params RecordField[] fields) => new(reference, earliestHost, true, fields);

    public static DeclaredFragmentDefinition Reusable(
        FragmentReference reference,
        params RecordField[] fields) => new(reference, null, false, fields);
}

public abstract class ShapeValue
{
    protected ShapeValue(ShapeReference reference) => Reference = reference;

    public ShapeReference Reference { get; }

    public virtual IReadOnlyDictionary<FragmentReference, IReadOnlyDictionary<string, ShapeValue>> Fragments =>
        ImmutableDictionary<FragmentReference, IReadOnlyDictionary<string, ShapeValue>>.Empty;

    public static ShapeValue Unit { get; } = new UnitShapeValue();

    public static ShapeValue Scalar<T>(ShapeReference reference, T value) =>
        new ScalarShapeValue(reference, ScalarRepresentations.RequireSupportedValue(value));

    public static ShapeValue Text(string value) => Scalar(BuiltInShapes.Text, value);
    public static ShapeValue Signed64(long value) => Scalar(BuiltInShapes.Signed64, value);
    public static ShapeValue Boolean(bool value) => Scalar(BuiltInShapes.Boolean, value);

    public static ShapeValue Record(ShapeReference reference, params (string Name, ShapeValue Value)[] fields) =>
        Record(reference, fields.AsEnumerable(), []);

    public static ShapeValue Record(
        ShapeReference reference,
        IEnumerable<(string Name, ShapeValue Value)> fields,
        IEnumerable<(FragmentReference Fragment, IReadOnlyDictionary<string, ShapeValue> Fields)> fragments) =>
        new RecordShapeValue(reference, fields, fragments);

    public static ShapeValue Sequence(ShapeReference reference, params ShapeValue[] items) =>
        new SequenceShapeValue(reference, items);

    public static ShapeValue Choice(ShapeReference reference, string alternative, ShapeValue value) =>
        new ChoiceShapeValue(reference, alternative, value);

    public static ShapeValue Opaque(ShapeReference reference, ReadOnlySpan<byte> bytes) =>
        new OpaqueShapeValue(reference, bytes.ToArray());

    public T RequireScalar<T>()
    {
        if (this is not ScalarShapeValue scalar || scalar.Value is not T value)
        {
            throw new InvalidOperationException($"Value shaped as {Reference} is not a {typeof(T).Name} scalar.");
        }

        return value;
    }

    public ShapeValue RequireField(string name)
    {
        if (this is not RecordShapeValue record || !record.Fields.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException($"Value shaped as {Reference} has no field '{name}'.");
        }

        return value;
    }
}

public sealed class UnitShapeValue : ShapeValue
{
    internal UnitShapeValue() : base(BuiltInShapes.Unit) { }
}

public sealed class ScalarShapeValue : ShapeValue
{
    internal ScalarShapeValue(ShapeReference reference, object value)
        : base(reference) => Value = ScalarRepresentations.RequireSupportedValue(value);

    public object Value { get; }
}

public sealed class RecordShapeValue : ShapeValue
{
    internal RecordShapeValue(
        ShapeReference reference,
        IEnumerable<(string Name, ShapeValue Value)> fields,
        IEnumerable<(FragmentReference Fragment, IReadOnlyDictionary<string, ShapeValue> Fields)> fragments)
        : base(reference)
    {
        Fields = fields.ToImmutableDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);
        Fragments = fragments.ToImmutableDictionary(
            fragment => fragment.Fragment,
            fragment => (IReadOnlyDictionary<string, ShapeValue>)fragment.Fields.ToImmutableDictionary(StringComparer.Ordinal));
    }

    public ImmutableDictionary<string, ShapeValue> Fields { get; }
    public override IReadOnlyDictionary<FragmentReference, IReadOnlyDictionary<string, ShapeValue>> Fragments { get; }
}

public sealed class SequenceShapeValue(ShapeReference reference, IEnumerable<ShapeValue> items) : ShapeValue(reference)
{
    public ImmutableArray<ShapeValue> Items { get; } = items.ToImmutableArray();
}

public sealed class ChoiceShapeValue(ShapeReference reference, string alternative, ShapeValue value) : ShapeValue(reference)
{
    public string Alternative { get; } = alternative;
    public ShapeValue Value { get; } = value;
}

public sealed class OpaqueShapeValue(ShapeReference reference, byte[] bytes) : ShapeValue(reference)
{
    public ReadOnlyMemory<byte> Bytes { get; } = bytes;
}

public sealed record ShapeProjectionResult(
    bool IsValid,
    string Message,
    ShapeValue? Value,
    ImmutableArray<FragmentReference> UnderstoodFragments,
    ImmutableArray<FragmentReference> IgnoredFragments)
{
    internal static ShapeProjectionResult Invalid(string message) =>
        new(false, message, null, [], []);
}

public sealed class ShapeRegistrationException(string message) : InvalidOperationException(message);

/// <summary>Registry and directional projection engine for semantic Shape contracts.</summary>
public sealed class ShapeRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<ShapeReference, ShapeDefinition> _shapes = [];
    private readonly Dictionary<FragmentReference, DeclaredFragmentDefinition> _fragments = [];

    public static ShapeRegistry CreateWithBuiltIns()
    {
        var registry = new ShapeRegistry();
        registry.Register(ShapeDefinition.Unit(BuiltInShapes.Unit));
        registry.Register(ShapeDefinition.Scalar<bool>(BuiltInShapes.Boolean));
        registry.Register(ShapeDefinition.Scalar<long>(BuiltInShapes.Signed64));
        registry.Register(ShapeDefinition.Scalar<string>(BuiltInShapes.Text));
        registry.Register(ShapeDefinition.Opaque(BuiltInShapes.Bytes));
        registry.Register(ShapeDefinition.Sequence(BuiltInShapes.OperationSet, BuiltInShapes.Text));
        registry.Register(ShapeDefinition.Record(BuiltInShapes.TimeWindow, FragmentPolicy.Closed,
            RecordField.Optional("not-before", BuiltInShapes.Text),
            RecordField.Optional("not-after", BuiltInShapes.Text)));
        registry.Register(ShapeDefinition.Scalar<string>(BuiltInShapes.Lease));
        registry.Register(ShapeDefinition.Scalar<string>(BuiltInShapes.OriginClass));
        registry.Register(ShapeDefinition.Record(BuiltInShapes.Details, FragmentPolicy.Closed,
            RecordField.Required("message", BuiltInShapes.Text)));
        registry.Register(ShapeDefinition.Record(BuiltInShapes.Activity, FragmentPolicy.Closed,
            RecordField.Required("id", BuiltInShapes.Text),
            RecordField.Required("kind", BuiltInShapes.Text)));
        return registry;
    }

    public IReadOnlyCollection<ShapeDefinition> Shapes
    {
        get { lock (_gate) { return _shapes.Values.ToArray(); } }
    }

    public IReadOnlyCollection<DeclaredFragmentDefinition> Fragments
    {
        get { lock (_gate) { return _fragments.Values.ToArray(); } }
    }

    public bool Recognizes(ShapeContract contract)
    {
        lock (_gate)
        {
            return _shapes.ContainsKey(contract.Canonical) &&
                contract.Required÷¾-¢G§²ÚîÆ­yÕ.Name == host.Name)
                    .SelectMany(fragment => fragment.Fields.Keys)
                    .FirstOrDefault(definition.Fields.ContainsKey);
                if (siblingCollision is not null)
                {
                    throw new ShapeRegistrationException(
                        $"Fragment field '{siblingCollision}' overlaps another authored Fragment.");
                }
            }

            var lineage = _fragments.Values
                .Where(candidate => candidate.Reference.Name == definition.Reference.Name)
                .Append(definition)
                .OrderBy(candidate => candidate.Reference.Version)
                .ToArray();
            for (var index = 1; index < lineage.Length; index++)
            {
                EnsureFragmentAdditive(lineage[index - 1], lineage[index]);
            }

            _fragments.Add(definition.Reference, definition);
        }
    }

    public ShapeProjectionResult Project(ShapeValue value, ShapeContract accepted)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(accepted);
        lock (_gate)
        {
            return ProjectCore(value, accepted);
        }
    }

    /// <summary>Transparent forwarding preserves the original complete value, including unknown Fragments.</summary>
    public static ShapeValue PreserveForForwarding(ShapeValue value) =>
        value ?? throw new ArgumentNullException(nameof(value));

    private ShapeProjectionResult ProjectCore(ShapeValue value, ShapeContract accepted)
    {
        if (!_shapes.TryGetValue(accepted.Canonical, out var acceptedDefinition))
        {
            return ShapeProjectionResult.Invalid($"Target does not recognise Shape {accepted.Canonical}.");
        }

        if (!_shapes.TryGetValue(value.Reference, out var valueDefinition))
        {
            return ShapeProjectionResult.Invalid($"Presented value declares unknown Shape {value.Reference}.");
        }

        if (value.Reference.Name != accepted.Canonical.Name || value.Reference.Version < accepted.Canonical.Version)
        {
            return ShapeProjectionResult.Invalid($"Shape {value.Reference} cannot project to {accepted.Canonical}.");
        }

        if (valueDefinition.Kind != acceptedDefinition.Kind)
        {
            return ShapeProjectionResult.Invalid("Shape kind differs within a canonical lineage.");
        }

        return acceptedDefinition.Kind switch
        {
            ShapeKind.Unit => value is UnitShapeValue
                ? Valid(value)
                : ShapeProjectionResult.Invalid($"{accepted.Canonical} requires unit."),
            ShapeKind.Scalar => value is ScalarShapeValue scalar &&
                scalar.Value.GetType() == acceptedDefinition.ScalarRepresentation
                ? Valid(new ScalarShapeValue(accepted.Canonical, scalar.Value))
                : ShapeProjectionResult.Invalid(
                    $"{accepted.Canonical} requires a {acceptedDefinition.ScalarRepresentation?.Name} scalar."),
            ShapeKind.Opaque => value is OpaqueShapeValue opaque
                ? Valid(new OpaqueShapeValue(accepted.Canonical, opaque.Bytes.ToArray()))
                : ShapeProjectionResult.Invalid($"{accepted.Canonical} requires opaque data."),
            ShapeKind.Sequence => ProjectSequence(value, acceptedDefinition),
            ShapeKind.Choice => ProjectChoice(value, acceptedDefinition),
            ShapeKind.Record => ProjectRecord(value, acceptedDefinition, accepted),
            _ => ShapeProjectionResult.Invalid("Unsupported Shape kind.")
        };
    }

    private ShapeProjectionResult ProjectSequence(ShapeValue value, ShapeDefinition accepted)
    {
        if (value is not SequenceShapeValue sequence)
        {
            return ShapeProjectionResult.Invalid($"{accepted.Reference} requires a sequence.");
        }

        var projected = ImmutableArray.CreateBuilder<ShapeValue>();
        foreach (var item in sequence.Items)
        {
            var result = ProjectCore(item, ShapeContract.For(accepted.ElementShape!.Value));
            if (!result.IsValid)
            {
                return result;
            }

            projected.Add(result.Value!);
        }

        return Valid(new SequenceShapeValue(accepted.Reference, projected));
    }

    private ShapeProjectionResult ProjectChoice(ShapeValue value, ShapeDefinition accepted)
    {
        if (value is not ChoiceShapeValue choice || !accepted.Alternatives.TryGetValue(choice.Alternative, out var alternative))
        {
            return ShapeProjectionResult.Invalid($"{accepted.Reference} does not recognise the presented choice.");
        }

        var projected = ProjectCore(choice.Value, ShapeContract.For(alternative));
        return projected.IsValid
            ? Valid(new ChoiceShapeValue(accepted.Reference, choice.Alternative, projected.Value!))
            : projected;
    }

    private ShapeProjectionResult ProjectRecord(ShapeValue value, ShapeDefinition accepted, ShapeContract contract)
    {
        if (value is not RecordShapeValue record)
        {
            return ShapeProjectionResult.Invalid($"{accepted.Reference} requires a record.");
        }

        var valueDefinition = _shapes[value.Reference];
        foreach (var presentedField in record.Fields.Keys)
        {
            if (!valueDefinition.Fields.ContainsKey(presentedField))
            {
                return ShapeProjectionResult.Invalid($"Unknown canonical field '{presentedField}' for {value.Reference}.");
            }
        }

        foreach (var field in valueDefinition.Fields.Values.Where(field => field.IsRequired))
        {
            if (!record.Fields.ContainsKey(field.Name))
            {
                return ShapeProjectionResult.Invalid($"Missing required field '{field.Name}' for {value.Reference}.");
            }
        }

        var projectedFields = ImmutableArray.CreateBuilder<(string Name, ShapeValue Value)>();
        foreach (var field in accepted.Fields.Values)
        {
            if (!record.Fields.TryGetValue(field.Name, out var fieldValue))
            {
                if (field.IsRequired)
                {
                    return ShapeProjectionResult.Invalid($"Missing required field '{field.Name}' for {accepted.Reference}.");
                }

                continue;
            }

            var projected = ProjectCore(fieldValue, ShapeContract.For(field.Shape));
            if (!projected.IsValid)
            {
                return ShapeProjectionResult.Invalid($"Field '{field.Name}': {projected.Message}");
            }

            projectedFields.Add((field.Name, projected.Value!));
        }

        var understood = ImmutableArray.CreateBuilder<FragmentReference>();
        var ignored = ImmutableArray.CreateBuilder<FragmentReference>();
        var projectedFragments = ImmutableArray.CreateBuilder<(
            FragmentReference Fragment,
            IReadOnlyDictionary<string, ShapeValue> Fields)>();
        var requiredFragments = accepted.IncludedFragments
            .Concat(contract.RequiredFragments)
            .Distinct()
            .ToImmutableArray();

        foreach (var presented in record.Fragments)
        {
            var required = requiredFragments
                .FirstOrDefault(candidate => candidate.Name == presented.Key.Name);
            if (!_fragments.TryGetValue(presented.Key, out var fragment))
            {
                if (accepted.FragmentPolicy == Brontide.Reference.Core.FragmentPolicy.Closed)
                {
                    return ShapeProjectionResult.Invalid($"Closed Shape {accepted.Reference} rejects authored Fragments.");
                }

                if (required != default)
                {
                    return ShapeProjectionResult.Invalid($"Required Fragment {required} is not recognised.");
                }

                ignored.Add(presented.Key);
                continue;
            }

            if (fragment.IsAuthoredAttachment && accepted.FragmentPolicy == Brontide.Reference.Core.FragmentPolicy.Closed)
            {
                return ShapeProjectionResult.Invalid($"Closed Shape {accepted.Reference} rejects authored Fragments.");
            }

            if (!fragment.IsAuthoredAttachment &&
                !accepted.IncludedFragments.Any(included => included.Name == fragment.Reference.Name))
            {
                return ShapeProjectionResult.Invalid(
                    $"Reusable Fragment {fragment.Reference} was not explicitly included by {accepted.Reference}.");
            }

            if (fragment.IsAuthoredAttachment &&
                (fragment.EarliestHost!.Value.Name != value.Reference.Name ||
                 fragment.EarliestHost.Value.Version > value.Reference.Version))
            {
                return ShapeProjectionResult.Invalid($"Fragment {presented.Key} is not attachable to {value.Reference}.");
            }

            var fragmentValidation = ValidateFragment(fragment, presented.Value);
            if (fragmentValidation is not null)
            {
                return ShapeProjectionResult.Invalid(fragmentValidation);
            }

            if (required != default)
            {
                if (presented.Key.Version < required.Version)
                {
                    return ShapeProjectionResult.Invalid($"Fragment {presented.Key} is older than required {required}.");
                }

                understood.Add(presented.Key);
                projectedFragments.Add((presented.Key, presented.Value));
            }
            else
            {
                ignored.Add(presented.Key);
            }
        }

        foreach (var required in requiredFragments)
        {
            if (!record.Fragments.Keys.Any(presented =>
                    presented.Name == required.Name && presented.Version >= required.Version))
            {
                return ShapeProjectionResult.Invalid($"Missing required Fragment {required}.");
            }
        }

        return new ShapeProjectionResult(
            true,
            "Shape is compatible.",
            new RecordShapeValue(accepted.Reference, projectedFields, projectedFragments),
            understood.ToImmutable(),
            ignored.ToImmutable());
    }

    private string? ValidateFragment(
        DeclaredFragmentDefinition fragment,
        IReadOnlyDictionary<string, ShapeValue> values)
    {
        foreach (var name in values.Keys)
        {
            if (!fragment.Fields.ContainsKey(name))
            {
                return $"Unknown field '{name}' in Fragment {fragment.Reference}.";
            }
        }

        foreach (var field in fragment.Fields.Values)
        {
            if (!values.TryGetValue(field.Name, out var value))
            {
                if (field.IsRequired)
                {
                    return $"Missing required field '{field.Name}' in Fragment {fragment.Reference}.";
                }

                continue;
            }

            var result = ProjectCore(value, ShapeContract.For(field.Shape));
            if (!result.IsValid)
            {
                return $"Fragment field '{field.Name}': {result.Message}";
            }
        }

        return null;
    }

    private static ShapeProjectionResult Valid(ShapeValue value) =>
        new(true, "Shape is compatible.", value, [], []);

    private static void EnsureAdditive(ShapeDefinition earlier, ShapeDefinition later)
    {
        if (earlier.Kind != later.Kind)
        {
            throw new ShapeRegistrationException($"Shape {later.Reference} changes kind from {earlier.Kind}.");
        }

        if (earlier.Kind == ShapeKind.Record)
        {
            if (earlier.FragmentPolicy != later.FragmentPolicy)
            {
                throw new ShapeRegistrationException($"Shape {later.Reference} changes Fragment policy.");
            }

            foreach (var previous in earlier.Fields.Values)
            {
                if (!later.Fields.TryGetValue(previous.Name, out var current) || current != previous)
                {
                    throw new ShapeRegistrationException(
                        $"Shape {later.Reference} removes or redefines field '{previous.Name}'.");
                }
            }

            var newlyRequired = later.Fields.Values
                .Where(field => !earlier.Fields.ContainsKey(field.Name) && field.IsRequired)
                .Select(field => field.Name)
                .FirstOrDefault();
            if (newlyRequired is not null)
            {
                throw new ShapeRegistrationException(
                    $"Shape {later.Reference} adds required field '{newlyRequired}'.");
            }

            if (!earlier.IncludedFragments.SequenceEqual(later.IncludedFragments))
            {
                throw new ShapeRegistrationException($"Shape {later.Reference} changes explicit Fragment inclusion.");
            }
        }
        else if (earlier.Kind == ShapeKind.Scalar && earlier.ScalarRepresentation != later.ScalarRepresentation)
        {
            throw new ShapeRegistrationException($"Shape {later.Reference} changes its scalar representation.");
        }
        else if (earlier.ElementShape != later.ElementShape ||
                 !earlier.Alternatives.OrderBy(pair => pair.Key).SequenceEqual(later.Alternatives.OrderBy(pair => pair.Key)))
        {
            throw new ShapeRegistrationException($"Shape {later.Reference} is not an additive version.");
        }
    }

    private static void EnsureFragmentAdditive(
        DeclaredFragmentDefinition earlier,
        DeclaredFragmentDefinition later)
    {
        if (earlier.IsAuthoredAttachment != later.IsAuthoredAttachment ||
            earlier.EarliestHost?.Name != later.EarliestHost?.Name)
        {
            throw new ShapeRegistrationException($"Fragment {later.Reference} changes its host semantics.");
        }

        foreach (var previous in earlier.Fields.Values)
        {
            if (!later.Fields.TryGetValue(previous.Name, out var current) || current != previous)
            {
                throw new ShapeRegistrationException(
                    $"Fragment {later.Reference} removes or redefines field '{previous.Name}'.");
            }
        }

        if (later.Fields.Values.Any(field => !earlier.Fields.ContainsKey(field.Name) && field.IsRequired))
        {
            throw new ShapeRegistrationException($"Fragment {later.Reference} adds required structure.");
        }
    }
}
