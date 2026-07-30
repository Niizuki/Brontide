using System.Text.Json;
using Brontide.Reference.Experimental.ComponentManagement;
using NUnit.Framework;

namespace Brontide.Reference.ComponentManagement.Tests;

[TestFixture]
public sealed class AuthorityAdmissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 10, 0, 0, TimeSpan.Zero);
    private static readonly ActorId Participant = ActorId.Create("actor.device");
    private static readonly ActorId Target = ActorId.Create("actor.input");
    private static readonly EvidenceId Evidence = EvidenceId.Create("evidence.pairing");
    private static readonly IssuerId Issuer = IssuerId.Create("issuer.host");
    private static readonly RelationshipRequestId Relationship = RelationshipRequestId.Create("relationship.pointer");
    private static readonly AuthorityRequestId Authority = AuthorityRequestId.Create("authority.publish");
    private static readonly CapabilityId Capability = CapabilityId.Create("capability.publish-pointer");
    private static readonly OperationId Operation = OperationId.Create("input.pointer.publish");
    private static readonly CapabilityScopeId Scope = CapabilityScopeId.Create("scope.session");

    [Test]
    public void Neutral_vector_inventory_is_complete_and_data_only()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cm5-authority-admission-vectors.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var ids = root.GetProperty("vectors")
            .EnumerateArray()
            .Select(vector => vector.GetProperty("id").GetString())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm5-authority-admission-vectors"));
            Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 20).Select(index => $"cm5-{index:00}").ToArray()));
            Assert.That(root.GetRawText(), Does.Not.Contain("algorithm"));
        });
    }

    [Test]
    public void Local_rules_establish_relationship_then_exact_narrow_grant()
    {
        var request = Request();

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Admitted));
            Assert.That(outcome.Observation.Relationships, Has.Count.EqualTo(1));
            Assert.That(outcome.Observation.Grants, Has.Count.EqualTo(1));
            Assert.That(outcome.Observation.Grants[0], Is.EqualTo(new LocalCapabilityGrant(
                CapabilityGrantId.Create("grant.authority.publish"),
                Authority,
                LocalActorReferenceId.Create("local.pointer"),
                Capability,
                Target,
                Operation,
                Scope,
                AuthorityPolicyId.Create("policy.host"),
                PolicyRuleId.Create("rule.publish"))));
            Assert.That(outcome.Observation.EvidenceDecisions.Single().Kind, Is.EqualTo(AdmissionEvidenceDecisionKind.Accepted));
        });
    }

    [Test]
    public void Claims_without_local_mappings_are_powerless()
    {
        var request = Request() with
        {
            Policy = Policy([], []),
        };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Denied));
            Assert.That(outcome.Observation.Relationships, Is.Empty);
            Assert.That(outcome.Observation.Grants, Is.Empty);
            Assert.That(outcome.Observation.RelationshipDecisions.Single().Reason, Does.Contain("no relationship mapping"));
        });
    }

    [TestCase(AdmissionEvidenceVerification.Unverified, AdmissionEvidenceState.Current, -1, 1, AdmissionEvidenceDecisionKind.Unverified)]
    [TestCase(AdmissionEvidenceVerification.Verified, AdmissionEvidenceState.Revoked, -1, 1, AdmissionEvidenceDecisionKind.Revoked)]
    [TestCase(AdmissionEvidenceVerification.Verified, AdmissionEvidenceState.Current, -2, -1, AdmissionEvidenceDecisionKind.Expired)]
    [TestCase(AdmissionEvidenceVerification.Verified, AdmissionEvidenceState.Current, 1, 2, AdmissionEvidenceDecisionKind.NotYetValid)]
    public void Invalid_evidence_blocks_relationship_and_dependent_grant(
        AdmissionEvidenceVerification verification,
        AdmissionEvidenceState state,
        int starts,
        int expires,
        AdmissionEvidenceDecisionKind expected)
    {
        var request = Request() with
        {
            Evidence = new[]
            {
                new AdmissionEvidence(Evidence, Issuer, Participant, verification, Now.AddHours(starts), Now.AddHours(expires), state),
            },
        };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Denied));
            Assert.That(outcome.Observation.EvidenceDecisions.Single().Kind, Is.EqualTo(expected));
            Assert.That(outcome.Observation.Relationships, Is.Empty);
            Assert.That(outcome.Observation.Grants, Is.Empty);
        });
    }

    [Test]
    public void Untrusted_or_subject_mismatched_evidence_fails_closed()
    {
        var untrusted = Request() with
        {
            Policy = Request().Policy with { TrustedIssuers = new[] { IssuerId.Create("issuer.other") } },
        };
        var mismatched = Request() with
        {
            Evidence = new[]
            {
                ValidEvidence() with { Subject = ActorId.Create("actor.other") },
            },
        };

        var first = new FakeAuthorityAdmissionEvaluator().Evaluate(untrusted);
        var second = new FakeAuthorityAdmissionEvaluator().Evaluate(mismatched);

        Assert.Multiple(() =>
        {
            Assert.That(first.Observation.EvidenceDecisions.Single().Kind, Is.EqualTo(AdmissionEvidenceDecisionKind.UntrustedIssuer));
            Assert.That(second.Observation.EvidenceDecisions.Single().Kind, Is.EqualTo(AdmissionEvidenceDecisionKind.SubjectMismatch));
            Assert.That(first.Observation.Grants, Is.Empty);
            Assert.That(second.Observation.Grants, Is.Empty);
        });
    }

    [Test]
    public void Additional_unrequired_evidence_is_recorded_but_does_not_poison_required_evidence()
    {
        var unrelated = ValidEvidence() with
        {
            Evidence = EvidenceId.Create("evidence.unrelated"),
            Verification = AdmissionEvidenceVerification.Unverified,
        };
        var request = Request() with
        {
            Evidence = new[] { unrelated, ValidEvidence() },
            Relationships = new[]
            {
                RelationshipRequest() with { Evidence = new[] { unrelated.Evidence, Evidence } },
            },
        };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.Admitted));
            Assert.That(outcome.Observation.EvidenceDecisions.Single(item => item.Evidence == Evidence).Kind, Is.EqualTo(AdmissionEvidenceDecisionKind.Accepted));
            Assert.That(outcome.Observation.EvidenceDecisions.Single(item => item.Evidence == unrelated.Evidence).Kind, Is.EqualTo(AdmissionEvidenceDecisionKind.Unverified));
            Assert.That(outcome.Observation.Grants, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Unlimited_and_unknown_authority_are_denied_without_erasing_relationship()
    {
        var unlimited = Request() with
        {
            Authority = new[] { AuthorityRequest() with { Unlimited = true } },
        };
        var unknown = Request() with
        {
            Authority = new[] { AuthorityRequest() with { Capability = CapabilityId.Create("capability.unknown") } },
        };

        var first = new FakeAuthorityAdmissionEvaluator().Evaluate(unlimited);
        var second = new FakeAuthorityAdmissionEvaluator().Evaluate(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.PartiallyAdmitted));
            Assert.That(second.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.PartiallyAdmitted));
            Assert.That(first.Observation.Relationships, Has.Count.EqualTo(1));
            Assert.That(second.Observation.Relationships, Has.Count.EqualTo(1));
            Assert.That(first.Observation.Grants, Is.Empty);
            Assert.That(second.Observation.Grants, Is.Empty);
            Assert.That(first.Observation.AuthorityDecisions.Single().Reason, Does.Contain("unlimited"));
        });
    }

    [TestCase(PolicyDisposition.Allow, AuthorityAdmissionOutcomeKind.Admitted, 1)]
    [TestCase(PolicyDisposition.Deny, AuthorityAdmissionOutcomeKind.PartiallyAdmitted, 0)]
    public void Known_authority_policy_mistake_is_applied_and_attributed(
        PolicyDisposition disposition,
        AuthorityAdmissionOutcomeKind expected,
        int grants)
    {
        var policy = Request().Policy;
        var mistaken = policy.AuthorityRules.Single() with
        {
            Disposition = disposition,
            KnownMistake = true,
            Rationale = "fixture marks this local decision as mistaken",
        };
        var request = Request() with { Policy = policy with { AuthorityRules = new[] { mistaken } } };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(expected));
            Assert.That(outcome.Observation.Grants, Has.Count.EqualTo(grants));
            Assert.That(outcome.Observation.PolicyMistakes, Has.Count.EqualTo(1));
            Assert.That(outcome.Observation.PolicyMistakes[0].Policy, Is.EqualTo(policy.Policy));
            Assert.That(outcome.Observation.PolicyMistakes[0].Rule, Is.EqualTo(mistaken.Rule));
            Assert.That(outcome.Observation.PolicyMistakes[0].Request, Is.EqualTo(Authority.Value));
        });
    }

    [Test]
    public void Missing_evidence_reference_is_invalid_and_effect_free()
    {
        var request = Request() with
        {
            Relationships = new[]
            {
                RelationshipRequest() with { Evidence = new[] { EvidenceId.Create("evidence.missing") } },
            },
        };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.InvalidRequest));
            Assert.That(outcome.Failure, Does.Contain("unknown evidence"));
            Assert.That(outcome.Observation.Relationships, Is.Empty);
            Assert.That(outcome.Observation.Grants, Is.Empty);
        });
    }

    [Test]
    public void Ambiguous_policy_or_unpresented_evidence_is_invalid_and_effect_free()
    {
        var baseline = Request();
        var repeatedRule = baseline.Policy.AuthorityRules.Single() with
        {
            Rule = baseline.Policy.RelationshipRules.Single().Rule,
        };
        var ambiguous = baseline with
        {
            Policy = baseline.Policy with { AuthorityRules = new[] { repeatedRule } },
        };
        var unpresented = baseline with
        {
            Evidence = new[]
            {
                ValidEvidence(),
                ValidEvidence() with { Evidence = EvidenceId.Create("evidence.unpresented") },
            },
        };

        var first = new FakeAuthorityAdmissionEvaluator().Evaluate(ambiguous);
        var second = new FakeAuthorityAdmissionEvaluator().Evaluate(unpresented);

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.InvalidRequest));
            Assert.That(first.Failure, Does.Contain("rule identity"));
            Assert.That(second.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.InvalidRequest));
            Assert.That(second.Failure, Does.Contain("not presented"));
            Assert.That(first.Observation.Grants, Is.Empty);
            Assert.That(second.Observation.Grants, Is.Empty);
        });
    }

    [Test]
    public void Independent_authority_requests_partially_admit_and_preserve_exact_reasons()
    {
        var denied = AuthorityRequest() with
        {
            Request = AuthorityRequestId.Create("authority.storage"),
            Capability = CapabilityId.Create("capability.storage"),
            Operation = OperationId.Create("storage.write"),
        };
        var request = Request() with { Authority = new[] { denied, AuthorityRequest() } };

        var outcome = new FakeAuthorityAdmissionEvaluator().Evaluate(request);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Kind, Is.EqualTo(AuthorityAdmissionOutcomeKind.PartiallyAdmitted));
            Assert.That(outcome.Observation.Grants.Select(item => item.Request), Is.EqualTo(new[] { Authority }));
            Assert.That(outcome.Observation.AuthorityDecisions.Single(item => item.Request == denied.Request).Reason, Does.Contain("no exact"));
            Assert.That(outcome.Observation.DecisionLog, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void Permuted_semantic_inputs_produce_equal_observations()
    {
        var secondEvidence = ValidEvidence() with { Evidence = EvidenceId.Create("evidence.signature") };
        var relationship = RelationshipRequest() with
        {
            Evidence = new[] { secondEvidence.Evidence, Evidence },
        };
        var request = Request() with
        {
            Evidence = new[] { secondEvidence, ValidEvidence() },
            Relationships = new[] { relationship },
        };
        var permuted = request with
        {
            Evidence = request.Evidence.Reverse().ToArray(),
            Relationships = new[] { relationship with { Evidence = relationship.Evidence.Reverse().ToArray() } },
            Policy = request.Policy with
            {
                TrustedIssuers = request.Policy.TrustedIssuers.Reverse().ToArray(),
                RelationshipRules = request.Policy.RelationshipRules.Reverse().ToArray(),
                AuthorityRules = request.Policy.AuthorityRules.Reverse().ToArray(),
            },
        };

        var first = new FakeAuthorityAdmissionEvaluator().Evaluate(request);
        var second = new FakeAuthorityAdmissionEvaluator().Evaluate(permuted);

        Assert.Multiple(() =>
        {
            Assert.That(second.Kind, Is.EqualTo(first.Kind));
            Assert.That(second.Observation.EvidenceDecisions, Is.EqualTo(first.Observation.EvidenceDecisions));
            Assert.That(second.Observation.RelationshipDecisions, Is.EqualTo(first.Observation.RelationshipDecisions));
            Assert.That(second.Observation.AuthorityDecisions, Is.EqualTo(first.Observation.AuthorityDecisions));
            Assert.That(second.Observation.Relationships, Is.EqualTo(first.Observation.Relationships));
            Assert.That(second.Observation.Grants, Is.EqualTo(first.Observation.Grants));
            Assert.That(second.Observation.PolicyMistakes, Is.EqualTo(first.Observation.PolicyMistakes));
            Assert.That(second.Observation.DecisionLog, Is.EqualTo(first.Observation.DecisionLog));
        });
    }

    private static AuthorityAdmissionRequest Request() =>
        new(
            AdmissionRequestId.Create("admission.one"),
            Participant,
            Now,
            new[] { ValidEvidence() },
            new[] { RelationshipRequest() },
            new[] { AuthorityRequest() },
            Policy(
                new[]
                {
                    new RelationshipPolicyRule(
                        PolicyRuleId.Create("rule.pointer"),
                        Participant,
                        ActorRelationshipKind.AttachedDevice,
                        PolicyDisposition.Allow,
                        LocalActorReferenceId.Create("local.pointer"),
                        new[] { Evidence },
                        false,
                        "paired pointer admitted"),
                },
                new[]
                {
                    new AuthorityPolicyRule(
                        PolicyRuleId.Create("rule.publish"),
                        ActorRelationshipKind.AttachedDevice,
                        Capability,
                        Target,
                        Operation,
                        Scope,
                        PolicyDisposition.Allow,
                        false,
                        "narrow pointer publication admitted"),
                }));

    private static AdmissionEvidence ValidEvidence() =>
        new(
            Evidence,
            Issuer,
            Participant,
            AdmissionEvidenceVerification.Verified,
            Now.AddHours(-1),
            Now.AddHours(1),
            AdmissionEvidenceState.Current);

    private static ActorRelationshipRequest RelationshipRequest() =>
        new(Relationship, Participant, ActorRelationshipKind.AttachedDevice, new[] { Evidence });

    private static AuthorityRequest AuthorityRequest() =>
        new(Authority, Relationship, Capability, Target, Operation, Scope, false);

    private static LocalAuthorityPolicy Policy(
        IReadOnlyList<RelationshipPolicyRule> relationships,
        IReadOnlyList<AuthorityPolicyRule> authority) =>
        new(AuthorityPolicyId.Create("policy.host"), new[] { Issuer }, relationships, authority);
}
