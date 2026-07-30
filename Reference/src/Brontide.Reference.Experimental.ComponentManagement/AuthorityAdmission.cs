using System.Collections.ObjectModel;

namespace Brontide.Reference.Experimental.ComponentManagement;

public enum ActorRelationshipKind
{
    AttachedDevice,
    ExternalPeer,
    ComponentParticipant,
}

public enum AdmissionEvidenceVerification
{
    Verified,
    Unverified,
}

public enum AdmissionEvidenceState
{
    Current,
    Revoked,
}

public enum AdmissionEvidenceDecisionKind
{
    Accepted,
    Unverified,
    UntrustedIssuer,
    NotYetValid,
    Expired,
    Revoked,
    SubjectMismatch,
}

public enum PolicyDisposition
{
    Allow,
    Deny,
}

public enum AuthorityAdmissionOutcomeKind
{
    Admitted,
    PartiallyAdmitted,
    Denied,
    InvalidRequest,
}

public sealed record AdmissionEvidence(
    EvidenceId Evidence,
    IssuerId Issuer,
    ActorId Subject,
    AdmissionEvidenceVerification Verification,
    DateTimeOffset ValidFrom,
    DateTimeOffset ExpiresAt,
    AdmissionEvidenceState State);

public sealed record ActorRelationshipRequest(
    RelationshipRequestId Request,
    ActorId ProposedActor,
    ActorRelationshipKind Kind,
    IReadOnlyList<EvidenceId> Evidence);

public sealed record AuthorityRequest(
    AuthorityRequestId Request,
    RelationshipRequestId Relationship,
    CapabilityId Capability,
    ActorId Target,
    OperationId Operation,
    CapabilityScopeId Scope,
    bool Unlimited);

public sealed record RelationshipPolicyRule(
    PolicyRuleId Rule,
    ActorId ProposedActor,
    ActorRelationshipKind Kind,
    PolicyDisposition Disposition,
    LocalActorReferenceId? LocalActor,
    IReadOnlyList<EvidenceId> RequiredEvidence,
    bool KnownMistake,
    string Rationale);

public sealed record AuthorityPolicyRule(
    PolicyRuleId Rule,
    ActorRelationshipKind RelationshipKind,
    CapabilityId Capability,
    ActorId Target,
    OperationId Operation,
    CapabilityScopeId Scope,
    PolicyDisposition Disposition,
    bool KnownMistake,
    string Rationale);

public sealed record LocalAuthorityPolicy(
    AuthorityPolicyId Policy,
    IReadOnlyList<IssuerId> TrustedIssuers,
    IReadOnlyList<RelationshipPolicyRule> RelationshipRules,
    IReadOnlyList<AuthorityPolicyRule> AuthorityRules);

public sealed record AuthorityAdmissionRequest(
    AdmissionRequestId Request,
    ActorId Participant,
    DateTimeOffset EvaluationTime,
    IReadOnlyList<AdmissionEvidence> Evidence,
    IReadOnlyList<ActorRelationshipRequest> Relationships,
    IReadOnlyList<AuthorityRequest> Authority,
    LocalAuthorityPolicy Policy);

public sealed record AdmissionEvidenceDecision(
    RelationshipRequestId Relationship,
    EvidenceId Evidence,
    AdmissionEvidenceDecisionKind Kind,
    string Reason);

public sealed record ActorRelationshipDecision(
    RelationshipRequestId Request,
    ActorId ProposedActor,
    ActorRelationshipKind Kind,
    bool Admitted,
    LocalActorReferenceId? LocalActor,
    PolicyRuleId? Rule,
    string Reason);

public sealed record AuthorityPolicyDecision(
    AuthorityRequestId Request,
    RelationshipRequestId Relationship,
    bool Admitted,
    PolicyRuleId? Rule,
    string Reason);

public sealed record EstablishedActorRelationship(
    RelationshipRequestId Request,
    ActorId ProposedActor,
    ActorRelationshipKind Kind,
    LocalActorReferenceId LocalActor,
    AuthorityPolicyId Policy,
    PolicyRuleId Rule);

public sealed record LocalCapabilityGrant(
    CapabilityGrantId Grant,
    AuthorityRequestId Request,
    LocalActorReferenceId Holder,
    CapabilityId Capability,
    ActorId Target,
    OperationId Operation,
    CapabilityScopeId Scope,
    AuthorityPolicyId Policy,
    PolicyRuleId Rule);

public sealed record PolicyMistakeFinding(
    AuthorityPolicyId Policy,
    PolicyRuleId Rule,
    string Request,
    PolicyDisposition Decision,
    string Rationale);

public sealed record AuthorityAdmissionObservation(
    AdmissionRequestId Request,
    AuthorityPolicyId Policy,
    DateTimeOffset EvaluationTime,
    IReadOnlyList<AdmissionEvidenceDecision> EvidenceDecisions,
    IReadOnlyList<ActorRelationshipDecision> RelationshipDecisions,
    IReadOnlyList<AuthorityPolicyDecision> AuthorityDecisions,
    IReadOnlyList<EstablishedActorRelationship> Relationships,
    IReadOnlyList<LocalCapabilityGrant> Grants,
    IReadOnlyList<PolicyMistakeFinding> PolicyMistakes,
    IReadOnlyList<string> DecisionLog);

public sealed record AuthorityAdmissionOutcome(
    AuthorityAdmissionOutcomeKind Kind,
    AuthorityAdmissionObservation Observation,
    string? Failure);

/// <summary>
/// Deterministic fake receiving-domain admission boundary. Evidence, policy, Actor mapping, and
/// Capability creation remain distinct so no participant-supplied value becomes authority.
/// </summary>
public sealed class FakeAuthorityAdmissionEvaluator
{
    public AuthorityAdmissionOutcome Evaluate(AuthorityAdmissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Policy);

        var snapshot = Snapshot(request);
        var invalid = Validate(snapshot);
        if (invalid is not null)
        {
            return Invalid(snapshot, invalid);
        }

        var evidenceById = snapshot.Evidence.ToDictionary(item => item.Evidence);
        var trustedIssuers = snapshot.Policy.TrustedIssuers.ToHashSet();
        var relationshipRules = snapshot.Policy.RelationshipRules.ToDictionary(
            item => (item.ProposedActor, item.Kind));
        var authorityRules = snapshot.Policy.AuthorityRules.ToDictionary(
            item => (item.RelationshipKind, item.Capability, item.Target, item.Operation, item.Scope));

        var evidenceDecisions = new List<AdmissionEvidenceDecision>();
        var relationshipDecisions = new List<ActorRelationshipDecision>();
        var established = new Dictionary<RelationshipRequestId, EstablishedActorRelationship>();
        var authorityDecisions = new List<AuthorityPolicyDecision>();
        var grants = new List<LocalCapabilityGrant>();
        var mistakes = new List<PolicyMistakeFinding>();
        var log = new List<string>();

        foreach (var relationship in snapshot.Relationships.OrderBy(item => item.Request.Value, StringComparer.Ordinal))
        {
            var decisions = relationship.Evidence
                .OrderBy(item => item.Value, StringComparer.Ordinal)
                .Select(id => EvaluateEvidence(
                    relationship,
                    evidenceById[id],
                    trustedIssuers,
                    snapshot.EvaluationTime))
                .ToArray();
            evidenceDecisions.AddRange(decisions);
            foreach (var decision in decisions)
            {
                log.Add($"evidence:{relationship.Request}:{decision.Evidence}:{Token(decision.Kind)}");
            }

            if (!relationshipRules.TryGetValue((relationship.ProposedActor, relationship.Kind), out var rule))
            {
                relationshipDecisions.Add(new(
                    relationship.Request,
                    relationship.ProposedActor,
                    relationship.Kind,
                    false,
                    null,
                    null,
                    "receiving policy has no relationship mapping"));
                log.Add($"relationship:{relationship.Request}:denied:no-policy-mapping");
                continue;
            }

            var evidenceAccepted = rule.RequiredEvidence.All(required =>
                decisions.Any(item =>
                    item.Evidence == required
                    && item.Kind == AdmissionEvidenceDecisionKind.Accepted));
            if (!evidenceAccepted)
            {
                relationshipDecisions.Add(new(
                    relationship.Request,
                    relationship.ProposedActor,
                    relationship.Kind,
                    false,
                    null,
                    rule.Rule,
                    "required evidence was not accepted"));
                log.Add($"relationship:{relationship.Request}:denied:evidence");
                continue;
            }

            RecordMistake(mistakes, snapshot.Policy.Policy, rule.Rule, relationship.Request.Value, rule.Disposition, rule.KnownMistake, rule.Rationale);
            if (rule.Disposition == PolicyDisposition.Deny)
            {
                relationshipDecisions.Add(new(
                    relationship.Request,
                    relationship.ProposedActor,
                    relationship.Kind,
                    false,
                    null,
                    rule.Rule,
                    rule.Rationale));
                log.Add($"relationship:{relationship.Request}:denied:{rule.Rule}");
                continue;
            }

            var accepted = new EstablishedActorRelationship(
                relationship.Request,
                relationship.ProposedActor,
                relationship.Kind,
                rule.LocalActor!.Value,
                snapshot.Policy.Policy,
                rule.Rule);
            established.Add(relationship.Request, accepted);
            relationshipDecisions.Add(new(
                relationship.Request,
                relationship.ProposedActor,
                relationship.Kind,
                true,
                rule.LocalActor,
                rule.Rule,
                rule.Rationale));
            log.Add($"relationship:{relationship.Request}:admitted:{rule.Rule}:{rule.LocalActor.Value}");
        }

        var relationshipById = snapshot.Relationships.ToDictionary(item => item.Request);
        foreach (var authority in snapshot.Authority.OrderBy(item => item.Request.Value, StringComparer.Ordinal))
        {
            if (!established.TryGetValue(authority.Relationship, out var relationship))
            {
                authorityDecisions.Add(new(
                    authority.Request,
                    authority.Relationship,
                    false,
                    null,
                    "dependent Actor relationship was not admitted"));
                log.Add($"authority:{authority.Request}:denied:relationship");
                continue;
            }

            if (authority.Unlimited)
            {
                authorityDecisions.Add(new(
                    authority.Request,
                    authority.Relationship,
                    false,
                    null,
                    "unlimited authority is not a locally recognisable narrow grant"));
                log.Add($"authority:{authority.Request}:denied:unlimited");
                continue;
            }

            var relationshipRequest = relationshipById[authority.Relationship];
            var key = (
                relationshipRequest.Kind,
                authority.Capability,
                authority.Target,
                authority.Operation,
                authority.Scope);
            if (!authorityRules.TryGetValue(key, out var rule))
            {
                authorityDecisions.Add(new(
                    authority.Request,
                    authority.Relationship,
                    false,
                    null,
                    "receiving policy has no exact authority mapping"));
                log.Add($"authority:{authority.Request}:denied:no-policy-mapping");
                continue;
            }

            RecordMistake(mistakes, snapshot.Policy.Policy, rule.Rule, authority.Request.Value, rule.Disposition, rule.KnownMistake, rule.Rationale);
            if (rule.Disposition == PolicyDisposition.Deny)
            {
                authorityDecisions.Add(new(
                    authority.Request,
                    authority.Relationship,
                    false,
                    rule.Rule,
                    rule.Rationale));
                log.Add($"authority:{authority.Request}:denied:{rule.Rule}");
                continue;
            }

            authorityDecisions.Add(new(
                authority.Request,
                authority.Relationship,
                true,
                rule.Rule,
                rule.Rationale));
            grants.Add(new(
                CapabilityGrantId.Create($"grant.{authority.Request.Value}"),
                authority.Request,
                relationship.LocalActor,
                authority.Capability,
                authority.Target,
                authority.Operation,
                authority.Scope,
                snapshot.Policy.Policy,
                rule.Rule));
            log.Add($"authority:{authority.Request}:admitted:{rule.Rule}");
        }

        var deniedCount =
            relationshipDecisions.Count(item => !item.Admitted)
            + authorityDecisions.Count(item => !item.Admitted);
        var admittedCount =
            relationshipDecisions.Count(item => item.Admitted)
            + authorityDecisions.Count(item => item.Admitted);
        var kind = (admittedCount, deniedCount) switch
        {
            (> 0, 0) => AuthorityAdmissionOutcomeKind.Admitted,
            (> 0, > 0) => AuthorityAdmissionOutcomeKind.PartiallyAdmitted,
            _ => AuthorityAdmissionOutcomeKind.Denied,
        };

        var observation = Observation(
            snapshot,
            evidenceDecisions,
            relationshipDecisions,
            authorityDecisions,
            established.Values,
            grants,
            mistakes,
            log);
        return new(kind, observation, null);
    }

    private static AdmissionEvidenceDecision EvaluateEvidence(
        ActorRelationshipRequest relationship,
        AdmissionEvidence evidence,
        HashSet<IssuerId> trustedIssuers,
        DateTimeOffset evaluationTime)
    {
        var (kind, reason) =
            evidence.Subject != relationship.ProposedActor
                ? (AdmissionEvidenceDecisionKind.SubjectMismatch, "evidence subject does not match the proposed Actor")
                : evidence.Verification != AdmissionEvidenceVerification.Verified
                    ? (AdmissionEvidenceDecisionKind.Unverified, "evidence verification did not succeed")
                    : !trustedIssuers.Contains(evidence.Issuer)
                        ? (AdmissionEvidenceDecisionKind.UntrustedIssuer, "issuer is not trusted by the receiving policy")
                        : evidence.State == AdmissionEvidenceState.Revoked
                            ? (AdmissionEvidenceDecisionKind.Revoked, "evidence is revoked")
                            : evaluationTime < evidence.ValidFrom
                                ? (AdmissionEvidenceDecisionKind.NotYetValid, "evidence is not yet valid")
                                : evaluationTime >= evidence.ExpiresAt
                                    ? (AdmissionEvidenceDecisionKind.Expired, "evidence has expired")
                                    : (AdmissionEvidenceDecisionKind.Accepted, "evidence accepted for local policy evaluation");
        return new(relationship.Request, evidence.Evidence, kind, reason);
    }

    private static void RecordMistake(
        List<PolicyMistakeFinding> findings,
        AuthorityPolicyId policy,
        PolicyRuleId rule,
        string request,
        PolicyDisposition disposition,
        bool knownMistake,
        string rationale)
    {
        if (knownMistake)
        {
            findings.Add(new(policy, rule, request, disposition, rationale));
        }
    }

    private static AuthorityAdmissionRequest Snapshot(AuthorityAdmissionRequest request) =>
        request with
        {
            Evidence = Array.AsReadOnly(request.Evidence.Select(item => item with { }).ToArray()),
            Relationships = Array.AsReadOnly(request.Relationships
                .Select(item => item with { Evidence = Array.AsReadOnly(item.Evidence.ToArray()) })
                .ToArray()),
            Authority = Array.AsReadOnly(request.Authority.Select(item => item with { }).ToArray()),
            Policy = request.Policy with
            {
                TrustedIssuers = Array.AsReadOnly(request.Policy.TrustedIssuers.ToArray()),
                RelationshipRules = Array.AsReadOnly(request.Policy.RelationshipRules
                    .Select(item => item with { RequiredEvidence = Array.AsReadOnly(item.RequiredEvidence.ToArray()) })
                    .ToArray()),
                AuthorityRules = Array.AsReadOnly(request.Policy.AuthorityRules.Select(item => item with { }).ToArray()),
            },
        };

    private static string? Validate(AuthorityAdmissionRequest request)
    {
        if (request.EvaluationTime == default)
        {
            return "evaluation time must be supplied explicitly";
        }

        var duplicate = Duplicate(request.Evidence.Select(item => item.Evidence.Value));
        if (duplicate is not null)
        {
            return $"duplicate evidence identity '{duplicate}'";
        }

        duplicate = Duplicate(request.Relationships.Select(item => item.Request.Value));
        if (duplicate is not null)
        {
            return $"duplicate relationship request identity '{duplicate}'";
        }

        duplicate = Duplicate(request.Authority.Select(item => item.Request.Value));
        if (duplicate is not null)
        {
            return $"duplicate authority request identity '{duplicate}'";
        }

        duplicate = Duplicate(request.Policy.RelationshipRules.Select(item => $"{item.ProposedActor.Value}|{item.Kind}"));
        if (duplicate is not null)
        {
            return $"duplicate relationship policy mapping '{duplicate}'";
        }

        duplicate = Duplicate(request.Policy.AuthorityRules.Select(item =>
            $"{item.RelationshipKind}|{item.Capability.Value}|{item.Target.Value}|{item.Operation.Value}|{item.Scope.Value}"));
        if (duplicate is not null)
        {
            return $"duplicate authority policy mapping '{duplicate}'";
        }

        duplicate = Duplicate(request.Policy.TrustedIssuers.Select(item => item.Value));
        if (duplicate is not null)
        {
            return $"trusted issuer '{duplicate}' is repeated";
        }

        duplicate = Duplicate(
            request.Policy.RelationshipRules.Select(item => item.Rule.Value)
                .Concat(request.Policy.AuthorityRules.Select(item => item.Rule.Value)));
        if (duplicate is not null)
        {
            return $"policy rule identity '{duplicate}' is repeated";
        }

        foreach (var rule in request.Policy.RelationshipRules)
        {
            if (rule.Disposition == PolicyDisposition.Allow && rule.LocalActor is null)
            {
                return $"admitting relationship rule '{rule.Rule}' does not assign a local Actor reference";
            }

            if (rule.Disposition == PolicyDisposition.Deny && rule.LocalActor is not null)
            {
                return $"denying relationship rule '{rule.Rule}' must not assign a local Actor reference";
            }

            duplicate = Duplicate(rule.RequiredEvidence.Select(item => item.Value));
            if (duplicate is not null)
            {
                return $"relationship rule '{rule.Rule}' repeats required evidence '{duplicate}'";
            }
        }

        var evidenceIds = request.Evidence.Select(item => item.Evidence).ToHashSet();
        foreach (var evidence in request.Evidence)
        {
            if (evidence.ValidFrom >= evidence.ExpiresAt)
            {
                return $"evidence '{evidence.Evidence}' has an empty or negative validity interval";
            }
        }

        foreach (var relationship in request.Relationships)
        {
            if (relationship.ProposedActor != request.Participant)
            {
                return $"relationship '{relationship.Request}' does not name the request participant";
            }

            duplicate = Duplicate(relationship.Evidence.Select(item => item.Value));
            if (duplicate is not null)
            {
                return $"relationship '{relationship.Request}' repeats evidence '{duplicate}'";
            }

            var missing = relationship.Evidence.FirstOrDefault(item => !evidenceIds.Contains(item));
            if (missing != default)
            {
                return $"relationship '{relationship.Request}' names unknown evidence '{missing}'";
            }
        }

        var referencedEvidence = request.Relationships.SelectMany(item => item.Evidence).ToHashSet();
        var unusedEvidence = request.Evidence.FirstOrDefault(item => !referencedEvidence.Contains(item.Evidence));
        if (unusedEvidence is not null)
        {
            return $"evidence '{unusedEvidence.Evidence}' is not presented by any relationship request";
        }

        var relationshipIds = request.Relationships.Select(item => item.Request).ToHashSet();
        var missingRelationship = request.Authority.FirstOrDefault(item => !relationshipIds.Contains(item.Relationship));
        if (missingRelationship is not null)
        {
            return $"authority request '{missingRelationship.Request}' names unknown relationship '{missingRelationship.Relationship}'";
        }

        return null;
    }

    private static string? Duplicate(IEnumerable<string> values) =>
        values.GroupBy(item => item, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(item => item, StringComparer.Ordinal)
            .FirstOrDefault();

    private static AuthorityAdmissionOutcome Invalid(AuthorityAdmissionRequest request, string reason)
    {
        var observation = Observation(
            request,
            [],
            [],
            [],
            [],
            [],
            [],
            [$"invalid:{reason}"]);
        return new(AuthorityAdmissionOutcomeKind.InvalidRequest, observation, reason);
    }

    private static AuthorityAdmissionObservation Observation(
        AuthorityAdmissionRequest request,
        IEnumerable<AdmissionEvidenceDecision> evidence,
        IEnumerable<ActorRelationshipDecision> relationships,
        IEnumerable<AuthorityPolicyDecision> authority,
        IEnumerable<EstablishedActorRelationship> established,
        IEnumerable<LocalCapabilityGrant> grants,
        IEnumerable<PolicyMistakeFinding> mistakes,
        IEnumerable<string> log) =>
        new(
            request.Request,
            request.Policy.Policy,
            request.EvaluationTime,
            ReadOnly(evidence),
            ReadOnly(relationships),
            ReadOnly(authority),
            ReadOnly(established.OrderBy(item => item.Request.Value, StringComparer.Ordinal)),
            ReadOnly(grants),
            ReadOnly(mistakes),
            ReadOnly(log));

    private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> items) =>
        Array.AsReadOnly(items.ToArray());

    private static string Token(AdmissionEvidenceDecisionKind kind) =>
        kind switch
        {
            AdmissionEvidenceDecisionKind.Accepted => "accepted",
            AdmissionEvidenceDecisionKind.Unverified => "unverified",
            AdmissionEvidenceDecisionKind.UntrustedIssuer => "untrusted-issuer",
            AdmissionEvidenceDecisionKind.NotYetValid => "not-yet-valid",
            AdmissionEvidenceDecisionKind.Expired => "expired",
            AdmissionEvidenceDecisionKind.Revoked => "revoked",
            AdmissionEvidenceDecisionKind.SubjectMismatch => "subject-mismatch",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
}
