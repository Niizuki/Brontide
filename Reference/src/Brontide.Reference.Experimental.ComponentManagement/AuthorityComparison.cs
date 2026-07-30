using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Brontide.Reference.Experimental.ComponentManagement;

public sealed record AuthorityComparisonScenario(
    string Id,
    string ExpectedOutcome,
    string Json);

public sealed class AuthorityComparisonProtocolException : Exception
{
    public AuthorityComparisonProtocolException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>
/// CM6 JSON-lines process seam. Strings become Reference-native identifiers before CM5 executes,
/// and only the canonical, implementation-neutral profile participates in parity.
/// </summary>
public static class FakeAuthorityComparisonEndpoint
{
    private const int SchemaVersion = 1;
    private const int MaximumLineCharacters = 1_048_576;

    public static IReadOnlyList<AuthorityComparisonScenario> LoadFixture(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = Parse(json);
        var root = document.RootElement;
        RequireObject(root, "fixture");
        RequireProperties(root, "fixture", "schemaVersion", "fixture", "scenarios");
        RequireSchema(root, "fixture");
        if (Text(root, "fixture", "fixture") != "cm6-authority-comparison-vectors")
        {
            throw Invalid("fixture: unknown fixture name");
        }

        var scenarios = Elements(root, "scenarios", "fixture")
            .Select((scenario, index) =>
            {
                RequireObject(scenario, $"fixture.scenarios[{index}]");
                var id = Text(scenario, "id", $"fixture.scenarios[{index}]");
                var expected = Text(scenario, "expectedOutcome", $"fixture.scenarios[{index}]");
                if (expected is not ("admitted" or "partially-admitted" or "denied" or "invalid-request"))
                {
                    throw Unknown($"fixture.scenarios[{index}].expectedOutcome: unknown token '{expected}'");
                }

                return new AuthorityComparisonScenario(id, expected, JsonSerializer.Serialize(scenario));
            })
            .ToArray();
        var duplicate = scenarios.GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid($"fixture: duplicate scenario identity '{duplicate.Key}'");
        }

        return Array.AsReadOnly(scenarios);
    }

    public static string Evaluate(string scenarioJson, string implementation)
    {
        ArgumentNullException.ThrowIfNull(scenarioJson);
        if (string.IsNullOrWhiteSpace(implementation))
        {
            throw new ArgumentException("implementation identity is required", nameof(implementation));
        }

        if (scenarioJson.Length > MaximumLineCharacters)
        {
            return WriteProtocolError(
                implementation,
                "invalid-envelope",
                $"input line exceeds {MaximumLineCharacters} characters");
        }

        try
        {
            using var document = Parse(scenarioJson);
            var scenario = document.RootElement;
            var (id, request) = ReadScenario(scenario);
            var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);
            return WriteProfileResponse(implementation, id, outcome);
        }
        catch (AuthorityComparisonProtocolException exception)
        {
            return WriteProtocolError(implementation, exception.Code, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return WriteProtocolError(implementation, "invalid-envelope", exception.Message);
        }
    }

    public static async Task RunAsync(
        TextReader input,
        TextWriter output,
        string implementation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        while (await input.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                await output.WriteLineAsync(
                    WriteProtocolError(implementation, "invalid-envelope", "input line must contain one JSON object"))
                    .ConfigureAwait(false);
            }
            else
            {
                await output.WriteLineAsync(Evaluate(line, implementation)).ConfigureAwait(false);
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static (string Id, AuthorityAdmissionRequest Request) ReadScenario(JsonElement scenario)
    {
        RequireObject(scenario, "scenario");
        RequireProperties(
            scenario,
            "scenario",
            "schemaVersion",
            "id",
            "expectedOutcome",
            "evaluationTime",
            "participant",
            "evidence",
            "relationships",
            "authority",
            "policy");
        RequireSchema(scenario, "scenario");
        var id = Text(scenario, "id", "scenario");
        var expected = Text(scenario, "expectedOutcome", "scenario");
        if (expected is not ("admitted" or "partially-admitted" or "denied" or "invalid-request"))
        {
            throw Unknown($"scenario.expectedOutcome: unknown token '{expected}'");
        }
        var participant = ActorId.Create(Text(scenario, "participant", "scenario"));
        var evidence = Elements(scenario, "evidence", "scenario")
            .Select((item, index) => ReadEvidence(item, $"scenario.evidence[{index}]"))
            .ToArray();
        var relationships = Elements(scenario, "relationships", "scenario")
            .Select((item, index) => ReadRelationship(item, $"scenario.relationships[{index}]"))
            .ToArray();
        var authority = Elements(scenario, "authority", "scenario")
            .Select((item, index) => ReadAuthority(item, $"scenario.authority[{index}]"))
            .ToArray();
        var policyElement = Property(scenario, "policy", "scenario");
        var policy = ReadPolicy(policyElement, "scenario.policy");

        return (
            id,
            new AuthorityAdmissionRequest(
                AdmissionRequestId.Create($"admission.{id}"),
                participant,
                Timestamp(scenario, "evaluationTime", "scenario"),
                Array.AsReadOnly(evidence),
                Array.AsReadOnly(relationships),
                Array.AsReadOnly(authority),
                policy));
    }

    private static AdmissionEvidence ReadEvidence(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(item, path, "id", "issuer", "subject", "verification", "validFrom", "expiresAt", "state");
        return new(
            EvidenceId.Create(Text(item, "id", path)),
            IssuerId.Create(Text(item, "issuer", path)),
            ActorId.Create(Text(item, "subject", path)),
            Token(
                item,
                "verification",
                path,
                ("verified", AdmissionEvidenceVerification.Verified),
                ("unverified", AdmissionEvidenceVerification.Unverified)),
            Timestamp(item, "validFrom", path),
            Timestamp(item, "expiresAt", path),
            Token(
                item,
                "state",
                path,
                ("current", AdmissionEvidenceState.Current),
                ("revoked", AdmissionEvidenceState.Revoked)));
    }

    private static ActorRelationshipRequest ReadRelationship(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(item, path, "id", "actor", "kind", "evidence");
        return new(
            RelationshipRequestId.Create(Text(item, "id", path)),
            ActorId.Create(Text(item, "actor", path)),
            RelationshipKind(item, "kind", path),
            Array.AsReadOnly(Elements(item, "evidence", path)
                .Select((value, index) => EvidenceId.Create(ScalarText(value, $"{path}.evidence[{index}]")))
                .ToArray()));
    }

    private static AuthorityRequest ReadAuthority(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(item, path, "id", "relationship", "capability", "target", "operation", "scope", "unlimited");
        return new(
            AuthorityRequestId.Create(Text(item, "id", path)),
            RelationshipRequestId.Create(Text(item, "relationship", path)),
            CapabilityId.Create(Text(item, "capability", path)),
            ActorId.Create(Text(item, "target", path)),
            OperationId.Create(Text(item, "operation", path)),
            CapabilityScopeId.Create(Text(item, "scope", path)),
            Boolean(item, "unlimited", path));
    }

    private static LocalAuthorityPolicy ReadPolicy(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(item, path, "id", "trustedIssuers", "relationships", "authority");
        var trusted = Elements(item, "trustedIssuers", path)
            .Select((value, index) => IssuerId.Create(ScalarText(value, $"{path}.trustedIssuers[{index}]")))
            .ToArray();
        var relationships = Elements(item, "relationships", path)
            .Select((value, index) => ReadRelationshipRule(value, $"{path}.relationships[{index}]"))
            .ToArray();
        var authority = Elements(item, "authority", path)
            .Select((value, index) => ReadAuthorityRule(value, $"{path}.authority[{index}]"))
            .ToArray();
        return new(
            AuthorityPolicyId.Create(Text(item, "id", path)),
            Array.AsReadOnly(trusted),
            Array.AsReadOnly(relationships),
            Array.AsReadOnly(authority));
    }

    private static RelationshipPolicyRule ReadRelationshipRule(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(
            item,
            path,
            "id",
            "actor",
            "kind",
            "disposition",
            "localActor",
            "requiredEvidence",
            "knownMistake",
            "rationale");
        var localActor = Property(item, "localActor", path);
        if (localActor.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
        {
            throw Invalid($"{path}.localActor: expected string or null");
        }

        return new(
            PolicyRuleId.Create(Text(item, "id", path)),
            ActorId.Create(Text(item, "actor", path)),
            RelationshipKind(item, "kind", path),
            Disposition(item, "disposition", path),
            localActor.ValueKind == JsonValueKind.Null
                ? null
                : LocalActorReferenceId.Create(ScalarText(localActor, $"{path}.localActor")),
            Array.AsReadOnly(Elements(item, "requiredEvidence", path)
                .Select((value, index) => EvidenceId.Create(ScalarText(value, $"{path}.requiredEvidence[{index}]")))
                .ToArray()),
            Boolean(item, "knownMistake", path),
            Text(item, "rationale", path));
    }

    private static AuthorityPolicyRule ReadAuthorityRule(JsonElement item, string path)
    {
        RequireObject(item, path);
        RequireProperties(
            item,
            path,
            "id",
            "relationshipKind",
            "capability",
            "target",
            "operation",
            "scope",
            "disposition",
            "knownMistake",
            "rationale");
        return new(
            PolicyRuleId.Create(Text(item, "id", path)),
            RelationshipKind(item, "relationshipKind", path),
            CapabilityId.Create(Text(item, "capability", path)),
            ActorId.Create(Text(item, "target", path)),
            OperationId.Create(Text(item, "operation", path)),
            CapabilityScopeId.Create(Text(item, "scope", path)),
            Disposition(item, "disposition", path),
            Boolean(item, "knownMistake", path),
            Text(item, "rationale", path));
    }

    private static string WriteProfileResponse(
        string implementation,
        string scenario,
        AuthorityAdmissionOutcome outcome) =>
        Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("implementation", implementation);
            writer.WriteString("scenario", scenario);
            writer.WritePropertyName("profile");
            WriteProfile(writer, outcome);
            writer.WriteEndObject();
        });

    private static void WriteProfile(Utf8JsonWriter writer, AuthorityAdmissionOutcome outcome)
    {
        var observation = outcome.Observation;
        writer.WriteStartObject();
        writer.WriteString("outcome", Outcome(outcome.Kind));
        StringOrNull(writer, "failure", outcome.Failure);
        writer.WriteString("request", observation.Request.Value);
        writer.WriteString("policy", observation.Policy.Value);
        writer.WriteString("evaluationTime", Time(observation.EvaluationTime));
        WriteArray(writer, "evidenceDecisions", observation.EvidenceDecisions, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("relationship", item.Relationship.Value);
            json.WriteString("evidence", item.Evidence.Value);
            json.WriteString("kind", EvidenceDecision(item.Kind));
            json.WriteString("reason", item.Reason);
            json.WriteEndObject();
        });
        WriteArray(writer, "relationshipDecisions", observation.RelationshipDecisions, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("request", item.Request.Value);
            json.WriteString("actor", item.ProposedActor.Value);
            json.WriteString("kind", Relationship(item.Kind));
            json.WriteBoolean("admitted", item.Admitted);
            StringOrNull(json, "localActor", item.LocalActor?.Value);
            StringOrNull(json, "rule", item.Rule?.Value);
            json.WriteString("reason", item.Reason);
            json.WriteEndObject();
        });
        WriteArray(writer, "authorityDecisions", observation.AuthorityDecisions, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("request", item.Request.Value);
            json.WriteString("relationship", item.Relationship.Value);
            json.WriteBoolean("admitted", item.Admitted);
            StringOrNull(json, "rule", item.Rule?.Value);
            json.WriteString("reason", item.Reason);
            json.WriteEndObject();
        });
        WriteArray(writer, "relationships", observation.Relationships, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("request", item.Request.Value);
            json.WriteString("actor", item.ProposedActor.Value);
            json.WriteString("kind", Relationship(item.Kind));
            json.WriteString("localActor", item.LocalActor.Value);
            json.WriteString("policy", item.Policy.Value);
            json.WriteString("rule", item.Rule.Value);
            json.WriteEndObject();
        });
        WriteArray(writer, "grants", observation.Grants, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("grant", item.Grant.Value);
            json.WriteString("request", item.Request.Value);
            json.WriteString("holder", item.Holder.Value);
            json.WriteString("capability", item.Capability.Value);
            json.WriteString("target", item.Target.Value);
            json.WriteString("operation", item.Operation.Value);
            json.WriteString("scope", item.Scope.Value);
            json.WriteString("policy", item.Policy.Value);
            json.WriteString("rule", item.Rule.Value);
            json.WriteEndObject();
        });
        WriteArray(writer, "policyMistakes", observation.PolicyMistakes, (json, item) =>
        {
            json.WriteStartObject();
            json.WriteString("policy", item.Policy.Value);
            json.WriteString("rule", item.Rule.Value);
            json.WriteString("request", item.Request);
            json.WriteString("decision", Disposition(item.Decision));
            json.WriteString("rationale", item.Rationale);
            json.WriteEndObject();
        });
        writer.WritePropertyName("decisionLog");
        writer.WriteStartArray();
        foreach (var item in observation.DecisionLog)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string WriteProtocolError(string implementation, string code, string detail) =>
        Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("implementation", implementation);
            writer.WritePropertyName("protocolError");
            writer.WriteStartObject();
            writer.WriteString("code", code);
            writer.WriteString("detail", detail);
            writer.WriteEndObject();
            writer.WriteEndObject();
        });

    private static string Write(Action<Utf8JsonWriter> action)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            action(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteArray<T>(
        Utf8JsonWriter writer,
        string name,
        IEnumerable<T> items,
        Action<Utf8JsonWriter, T> writeItem)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var item in items)
        {
            writeItem(writer, item);
        }

        writer.WriteEndArray();
    }

    private static void StringOrNull(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static JsonDocument Parse(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new AuthorityComparisonProtocolException("malformed-json", exception.Message);
        }
    }

    private static void RequireSchema(JsonElement root, string path)
    {
        var schema = Property(root, "schemaVersion", path);
        if (schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var value))
        {
            throw Invalid($"{path}.schemaVersion: expected integer");
        }

        if (value != SchemaVersion)
        {
            throw new AuthorityComparisonProtocolException(
                "unsupported-schema",
                $"schema version {value} is not supported");
        }
    }

    private static void RequireObject(JsonElement item, string path)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw Invalid($"{path}: expected object");
        }
    }

    private static void RequireProperties(JsonElement item, string path, params string[] expected)
    {
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        var actual = item.EnumerateObject().Select(property => property.Name).ToArray();
        var duplicate = actual.GroupBy(name => name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw Invalid($"{path}: duplicate property '{duplicate.Key}'");
        }

        var unknown = actual.FirstOrDefault(name => !expectedSet.Contains(name));
        if (unknown is not null)
        {
            throw Invalid($"{path}: unknown property '{unknown}'");
        }

        var missing = expected.FirstOrDefault(name => !actual.Contains(name, StringComparer.Ordinal));
        if (missing is not null)
        {
            throw Invalid($"{path}: missing property '{missing}'");
        }
    }

    private static JsonElement Property(JsonElement item, string name, string path)
    {
        if (!item.TryGetProperty(name, out var value))
        {
            throw Invalid($"{path}: missing property '{name}'");
        }

        return value;
    }

    private static string Text(JsonElement item, string name, string path) =>
        ScalarText(Property(item, name, path), $"{path}.{name}");

    private static string ScalarText(JsonElement item, string path) =>
        item.ValueKind == JsonValueKind.String && item.GetString() is { } value
            ? value
            : throw Invalid($"{path}: expected string");

    private static bool Boolean(JsonElement item, string name, string path)
    {
        var value = Property(item, name, path);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Invalid($"{path}.{name}: expected boolean"),
        };
    }

    private static JsonElement.ArrayEnumerator Elements(JsonElement item, string name, string path)
    {
        var value = Property(item, name, path);
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw Invalid($"{path}.{name}: expected array");
        }

        return value.EnumerateArray();
    }

    private static DateTimeOffset Timestamp(JsonElement item, string name, string path)
    {
        var value = Text(item, name, path);
        if (!DateTimeOffset.TryParseExact(
                value,
                "yyyy-MM-dd'T'HH:mm:ssK",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            throw Invalid($"{path}.{name}: expected an RFC 3339 whole-second timestamp");
        }

        return timestamp;
    }

    private static T Token<T>(
        JsonElement item,
        string name,
        string path,
        params (string Token, T Value)[] choices)
    {
        var value = Text(item, name, path);
        foreach (var choice in choices)
        {
            if (value == choice.Token)
            {
                return choice.Value;
            }
        }

        throw Unknown($"{path}.{name}: unknown token '{value}'");
    }

    private static ActorRelationshipKind RelationshipKind(JsonElement item, string name, string path) =>
        Token(
            item,
            name,
            path,
            ("attached-device", ActorRelationshipKind.AttachedDevice),
            ("external-peer", ActorRelationshipKind.ExternalPeer),
            ("component-participant", ActorRelationshipKind.ComponentParticipant));

    private static PolicyDisposition Disposition(JsonElement item, string name, string path) =>
        Token(
            item,
            name,
            path,
            ("allow", PolicyDisposition.Allow),
            ("deny", PolicyDisposition.Deny));

    private static string Time(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string Outcome(AuthorityAdmissionOutcomeKind value) =>
        value switch
        {
            AuthorityAdmissionOutcomeKind.Admitted => "admitted",
            AuthorityAdmissionOutcomeKind.PartiallyAdmitted => "partially-admitted",
            AuthorityAdmissionOutcomeKind.Denied => "denied",
            AuthorityAdmissionOutcomeKind.InvalidRequest => "invalid-request",
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };

    private static string EvidenceDecision(AdmissionEvidenceDecisionKind value) =>
        value switch
        {
            AdmissionEvidenceDecisionKind.Accepted => "accepted",
            AdmissionEvidenceDecisionKind.Unverified => "unverified",
            AdmissionEvidenceDecisionKind.UntrustedIssuer => "untrusted-issuer",
            AdmissionEvidenceDecisionKind.NotYetValid => "not-yet-valid",
            AdmissionEvidenceDecisionKind.Expired => "expired",
            AdmissionEvidenceDecisionKind.Revoked => "revoked",
            AdmissionEvidenceDecisionKind.SubjectMismatch => "subject-mismatch",
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };

    private static string Relationship(ActorRelationshipKind value) =>
        value switch
        {
            ActorRelationshipKind.AttachedDevice => "attached-device",
            ActorRelationshipKind.ExternalPeer => "external-peer",
            ActorRelationshipKind.ComponentParticipant => "component-participant",
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };

    private static string Disposition(PolicyDisposition value) =>
        value switch
        {
            PolicyDisposition.Allow => "allow",
            PolicyDisposition.Deny => "deny",
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };

    private static AuthorityComparisonProtocolException Invalid(string message) =>
        new("invalid-envelope", message);

    private static AuthorityComparisonProtocolException Unknown(string message) =>
        new("unknown-token", message);
}
