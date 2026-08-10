using System.Text.Json;
using System.Text.Json.Nodes;
using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Composition;
using Brontide.Reference.Experimental.PersistentInformation;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: architecture07 <fixture.json> <observations.json>");
    return 2;
}

try
{
    using var fixture = JsonDocument.Parse(File.ReadAllText(args[0]));
    var observations = new JsonArray();
    foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
    {
        observations.Add(Observe(vector));
    }

    File.WriteAllText(args[1], observations.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static JsonObject Observe(JsonElement vector)
{
    var id = vector.GetProperty("id").GetString()!;
    var operation = vector.GetProperty("operation").GetString();
    var input = vector.GetProperty("input");
    return operation switch
    {
        "constraint" => ObserveConstraint(id, input.GetProperty("scenario").GetString()!),
        "canonical-name" => ObserveCanonicalName(id, input.GetProperty("text").GetString()!),
        "attribute-binding" => ObserveBinding(id, input.GetProperty("scenario").GetString()!),
        "persistent-information" => ObservePersistentInformation(id, input.GetProperty("scenario").GetString()!),
        _ => throw new InvalidOperationException($"Unknown comparison operation '{operation}'.")
    };
}

static JsonObject ObserveConstraint(string id, string scenario)
{
    var yes = new ValueConstraint(CanonicalName.Parse("Constraint.Yes"), ShapeValue.Text("yes"));
    var no = new ValueConstraint(CanonicalName.Parse("Constraint.No"), ShapeValue.Text("no"));
    var unknown = new ValueConstraint(CanonicalName.Parse("Constraint.Unknown"), ShapeValue.Text("unknown"));
    ConstraintExpression expression = scenario switch
    {
        "conjunction-satisfied" => new AllOfConstraintExpression(yes, yes),
        "unsupported-poisons-disjunction" => new AnyOfConstraintExpression(yes, unknown),
        "unsatisfied" => no,
        _ => throw new InvalidOperationException($"Unknown constraint scenario '{scenario}'.")
    };
    var evaluation = ConstraintExpressionEvaluator.Evaluate(expression, atom => atom.Name.ToString() switch
    {
        "Constraint.Yes" => ConstraintAtomEvaluation.Satisfied(),
        "Constraint.No" => ConstraintAtomEvaluation.Unsatisfied(),
        _ => ConstraintAtomEvaluation.Unsupported(atom.Name)
    });
    return evaluation.Outcome == ConstraintEvaluationOutcome.Satisfied
        ? Accepted(id, "satisfied")
        : Denied(id, Diagnostic(evaluation.DiagnosticCategory));
}

static JsonObject ObserveCanonicalName(string id, string text) =>
    CanonicalMemberName.TryParse(text, out var name)
        ? Accepted(id, name.ToString())
        : Denied(id, "name-invalid");

static JsonObject ObserveBinding(string id, string scenario)
{
    var binding = CanonicalName.Parse("Binding.Cooling");
    var region = CanonicalName.Parse("Attribute.Region");
    var exotic = CanonicalName.Parse("Attribute.Exotic");
    ConstraintExpression constraint = scenario == "unsupported-then-selected"
        ? new AllOfConstraintExpression(AttributeConstraint(region, "north"), AttributeConstraint(exotic, "yes"))
        : AttributeConstraint(region, "north");
    var candidates = scenario switch
    {
        "ordinal-selection" => new[] { Candidate("Provider.B", (region, "north")), Candidate("Provider.A", (region, "north")) },
        "unsupported-then-selected" => new[] { Candidate("Provider.A", (region, "north")), Candidate("Provider.B", (region, "north"), (exotic, "yes")) },
        "restore-recorded-selection" => new[] { Candidate("Provider.A", (region, "north")) },
        _ => throw new InvalidOperationException($"Unknown binding scenario '{scenario}'.")
    };
    var resolved = AttributeConstrainedBinding.Resolve(binding, constraint, candidates);
    if (!resolved.IsResolved)
    {
        return Denied(id, resolved.Provenance.LastOrDefault()?.DiagnosticCategory is { } category
            ? Diagnostic(category)
            : "unsatisfied");
    }

    var result = Accepted(id, resolved.Binding!.SelectedProvider.ToString());
    result["provenance"] = new JsonArray(resolved.Provenance.Select(item =>
        JsonValue.Create($"{item.Provider}:{item.Disposition.ToString().ToLowerInvariant()}")).ToArray());
    if (scenario == "restore-recorded-selection")
    {
        var restored = AttributeConstrainedBinding.Restore(constraint, resolved.Binding);
        result["restoration"] = restored.Binding!.SelectedProvider.ToString();
    }
    return result;
}

static JsonObject ObservePersistentInformation(string id, string scenario)
{
    var role = StoreRoleId.Parse("core");
    var roleDefinition = new StoreRoleDefinition(role, true, true, StoreRoleAbsenceBehavior.DatasetUnavailable);
    if (scenario == "corpus-rejects-external-coordination")
    {
        return Denied(id, OpaqueCorpus.Create(CorpusId.Parse("settings"), "1", ConcurrentAccessMode.ExternalCoordination, [roleDefinition]).Code);
    }

    if (scenario == "router-rejects-unsupported-guarantee")
    {
        var store = new InMemoryStore(StoreId.Parse("only"), EndpointGuarantee.Durable);
        return Denied(id, RouterEndpoint.Create(RouterId.Parse("router"), [EndpointGuarantee.Encrypted], [store], true).Code);
    }

    if (scenario is "router-fallback" or "router-redacts-topology")
    {
        var first = new InMemoryStore(StoreId.Parse("first"), EndpointGuarantee.Durable);
        var second = new InMemoryStore(StoreId.Parse("second"), EndpointGuarantee.Durable);
        first.IsAvailable = scenario == "router-redacts-topology";
        var router = RouterEndpoint.Create(RouterId.Parse("router"), [EndpointGuarantee.Durable], [first, second], false).Value!;
        var result = scenario == "router-fallback"
            ? router.Append("value").IsSuccess ? Accepted(id, second.Read().Single() == "value" ? "second" : "unexpected") : Denied(id, "store-unavailable")
            : Accepted(id, router.Describe(true).SelectedBacking is null ? "redacted" : "visible");
        result["guarantees"] = new JsonArray(JsonValue.Create("durable"));
        return result;
    }

    var corpus = OpaqueCorpus.Create(CorpusId.Parse("settings"), "1", ConcurrentAccessMode.SingleWriter, [roleDefinition]).Value!;
    var storeForDataset = new InMemoryStore(StoreId.Parse("primary"), EndpointGuarantee.Durable);
    var registry = new DatasetRegistry();
    var dataset = DatasetId.Parse("dataset-1");
    registry.Issue(new DatasetIssuance(Actor(), OperationReference.Parse("Dataset.Create")), corpus, dataset,
        new Dictionary<StoreRoleId, IStoreEndpoint> { [role] = storeForDataset });
    if (scenario == "dataset-concurrency-mismatch")
    {
        var refused = registry.Append(dataset, role, ConcurrentAccessMode.ExternalCoordination, "value");
        var denied = Denied(id, refused.Code);
        denied["effects"] = storeForDataset.AppendCount;
        return denied;
    }

    registry.Append(dataset, role, ConcurrentAccessMode.SingleWriter, "value");
    storeForDataset.Clear();
    var identity = registry.Datasets.Single().Id.ToString();
    var empty = registry.Read(dataset, role, ConcurrentAccessMode.SingleWriter).Value!.Count == 0;
    var accepted = Accepted(id, identity);
    accepted["restoration"] = empty ? "empty-content" : "content-present";
    return accepted;
}

static AttributeConstraint AttributeConstraint(CanonicalName name, string value) => new(name, ShapeValue.Text(value));

static AttributeCandidate Candidate(string provider, params (CanonicalName Name, string Value)[] attributes) =>
    new(CanonicalName.Parse(provider), attributes.Select(attribute => new AttributeValue(
        attribute.Name,
        CanonicalName.Parse("Operation.ReadAttribute"),
        "1",
        BuiltInShapes.Text,
        "/value",
        ShapeValue.Text(attribute.Value))).ToArray());

static ActorReference Actor()
{
    ActorReference actor = null!;
    _ = AuthorityDomain.Create("architecture-07-comparison", genesis => actor = genesis.Actor("Issuer"));
    return actor;
}

static JsonObject Accepted(string id, string value) => new()
{
    ["id"] = id,
    ["status"] = "accepted",
    ["value"] = value,
    ["diagnostic"] = "none"
};

static JsonObject Denied(string id, string diagnostic) => new()
{
    ["id"] = id,
    ["status"] = "denied",
    ["diagnostic"] = diagnostic
};

static string Diagnostic(ConstraintDiagnosticCategory category) => category switch
{
    ConstraintDiagnosticCategory.Satisfied => "none",
    ConstraintDiagnosticCategory.Unsatisfied => "unsatisfied",
    ConstraintDiagnosticCategory.UnsupportedConstraint => "unsupported-constraint",
    ConstraintDiagnosticCategory.InvalidConstraintValue => "invalid-constraint-value",
    ConstraintDiagnosticCategory.EvaluatorFailure => "evaluator-failure",
    ConstraintDiagnosticCategory.InvalidConstraintExpression => "invalid-constraint-expression",
    _ => throw new ArgumentOutOfRangeException(nameof(category))
};
