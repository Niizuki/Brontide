namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type AuthorityAdmissionTests() =
    let now = DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero)
    let participant = ActorId.create "actor.device"
    let target = ActorId.create "actor.input"
    let evidenceId = EvidenceId.create "evidence.pairing"
    let issuer = IssuerId.create "issuer.host"
    let relationshipId = RelationshipRequestId.create "relationship.pointer"
    let authorityId = AuthorityRequestId.create "authority.publish"
    let capability = CapabilityId.create "capability.publish-pointer"
    let operation = OperationId.create "input.pointer.publish"
    let scope = CapabilityScopeId.create "scope.session"
    let multiple action = Assert.Multiple(Action action)

    let validEvidence () : AdmissionEvidence =
        { Evidence = evidenceId
          Issuer = issuer
          Subject = participant
          Verification = Verified
          ValidFrom = now.AddHours(-1.0)
          ExpiresAt = now.AddHours(1.0)
          State = Current }

    let relationshipRequest () : ActorRelationshipRequest =
        { Request = relationshipId
          ProposedActor = participant
          Kind = AttachedDevice
          Evidence = [ evidenceId ] }

    let authorityRequest () : AuthorityRequest =
        { Request = authorityId
          Relationship = relationshipId
          Capability = capability
          Target = target
          Operation = operation
          Scope = scope
          Unlimited = false }

    let policy relationshipRules authorityRules =
        { Policy = AuthorityPolicyId.create "policy.host"
          TrustedIssuers = [ issuer ]
          RelationshipRules = relationshipRules
          AuthorityRules = authorityRules }

    let request () : AuthorityAdmissionRequest =
        { Request = AdmissionRequestId.create "admission.one"
          Participant = participant
          EvaluationTime = now
          Evidence = [ validEvidence () ]
          Relationships = [ relationshipRequest () ]
          Authority = [ authorityRequest () ]
          Policy =
            policy
                [ { Rule = PolicyRuleId.create "rule.pointer"
                    ProposedActor = participant
                    Kind = AttachedDevice
                    Disposition = Allow
                    LocalActor = Some(LocalActorReferenceId.create "local.pointer")
                    RequiredEvidence = [ evidenceId ]
                    KnownMistake = false
                    Rationale = "paired pointer admitted" } ]
                [ { Rule = PolicyRuleId.create "rule.publish"
                    RelationshipKind = AttachedDevice
                    Capability = capability
                    Target = target
                    Operation = operation
                    Scope = scope
                    Disposition = Allow
                    KnownMistake = false
                    Rationale = "narrow pointer publication admitted" } ] }

    [<Test>]
    member _.``neutral vector inventory is complete and data only``() =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cm5-authority-admission-vectors.json"
            )
        use document = JsonDocument.Parse(File.ReadAllText path)
        let root = document.RootElement
        let ids =
            root.GetProperty("vectors").EnumerateArray()
            |> Seq.map (fun vector -> vector.GetProperty("id").GetString())
            |> Seq.toList
        multiple (fun () ->
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo 1)
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo "cm5-authority-admission-vectors")
            Assert.That(ids, Is.EqualTo<string list>([ 1..20 ] |> List.map (sprintf "cm5-%02d")))
            Assert.That(root.GetRawText(), Does.Not.Contain "algorithm"))

    [<Test>]
    member _.``local rules establish relationship then exact narrow grant``() =
        let outcome = request () |> FakeAuthorityAdmission.evaluate
        let grant = outcome.Observation.Grants |> List.exactlyOne
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
            Assert.That(List.length outcome.Observation.Relationships, Is.EqualTo 1)
            Assert.That(List.length outcome.Observation.Grants, Is.EqualTo 1)
            Assert.That(grant.Request, Is.EqualTo authorityId)
            Assert.That(grant.Holder, Is.EqualTo(LocalActorReferenceId.create "local.pointer"))
            Assert.That(grant.Capability, Is.EqualTo capability)
            Assert.That(grant.Target, Is.EqualTo target)
            Assert.That(grant.Operation, Is.EqualTo operation)
            Assert.That(grant.Scope, Is.EqualTo scope)
            Assert.That(
                (outcome.Observation.EvidenceDecisions |> List.exactlyOne).Kind,
                Is.EqualTo AdmissionEvidenceDecisionKind.Accepted
            ))

    [<Test>]
    member _.``claims without local mappings are powerless``() =
        let input = { request () with Policy = policy [] [] }
        let outcome = FakeAuthorityAdmission.evaluate input
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Denied)
            Assert.That(outcome.Observation.Relationships, Is.Empty)
            Assert.That(outcome.Observation.Grants, Is.Empty)
            Assert.That(
                (outcome.Observation.RelationshipDecisions |> List.exactlyOne).Reason,
                Does.Contain "no relationship mapping"
            ))

    [<Test>]
    member _.``unverified revoked expired and future evidence block authority``() =
        let cases =
            [ { validEvidence () with Verification = Unverified }, UnverifiedEvidence
              { validEvidence () with State = Revoked }, RevokedEvidence
              { validEvidence () with
                    ValidFrom = now.AddHours(-2.0)
                    ExpiresAt = now.AddHours(-1.0) },
                Expired
              { validEvidence () with
                    ValidFrom = now.AddHours(1.0)
                    ExpiresAt = now.AddHours(2.0) },
                NotYetValid ]
        cases
        |> List.iter (fun (evidence, expected) ->
            let outcome =
                FakeAuthorityAdmission.evaluate { request () with Evidence = [ evidence ] }
            multiple (fun () ->
                Assert.That(
                    (outcome.Observation.EvidenceDecisions |> List.exactlyOne).Kind,
                    Is.EqualTo expected
                )
                Assert.That(outcome.Observation.Relationships, Is.Empty)
                Assert.That(outcome.Observation.Grants, Is.Empty)))

    [<Test>]
    member _.``untrusted and subject mismatched evidence fail closed``() =
        let baseline = request ()
        let untrusted =
            { baseline with
                Policy =
                    { baseline.Policy with
                        TrustedIssuers = [ IssuerId.create "issuer.other" ] } }
            |> FakeAuthorityAdmission.evaluate
        let mismatched =
            { baseline with
                Evidence = [ { validEvidence () with Subject = ActorId.create "actor.other" } ] }
            |> FakeAuthorityAdmission.evaluate
        multiple (fun () ->
            Assert.That(
                (untrusted.Observation.EvidenceDecisions |> List.exactlyOne).Kind,
                Is.EqualTo UntrustedIssuer
            )
            Assert.That(
                (mismatched.Observation.EvidenceDecisions |> List.exactlyOne).Kind,
                Is.EqualTo SubjectMismatch
            )
            Assert.That(untrusted.Observation.Grants, Is.Empty)
            Assert.That(mismatched.Observation.Grants, Is.Empty))

    [<Test>]
    member _.``additional unrequired evidence does not poison required evidence``() =
        let unrelated =
            { validEvidence () with
                Evidence = EvidenceId.create "evidence.unrelated"
                Verification = Unverified }
        let input =
            { request () with
                Evidence = [ unrelated; validEvidence () ]
                Relationships =
                    [ { relationshipRequest () with
                          Evidence = [ unrelated.Evidence; evidenceId ] } ] }
        let outcome = FakeAuthorityAdmission.evaluate input
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
            Assert.That(
                (outcome.Observation.EvidenceDecisions
                 |> List.find (fun item -> item.Evidence = evidenceId)).Kind,
                Is.EqualTo AdmissionEvidenceDecisionKind.Accepted
            )
            Assert.That(
                (outcome.Observation.EvidenceDecisions
                 |> List.find (fun item -> item.Evidence = unrelated.Evidence)).Kind,
                Is.EqualTo UnverifiedEvidence
            )
            Assert.That(List.length outcome.Observation.Grants, Is.EqualTo 1))

    [<Test>]
    member _.``unlimited and unknown authority are denied independently``() =
        let baseline = request ()
        let unlimited =
            { baseline with
                Authority = [ { authorityRequest () with Unlimited = true } ] }
            |> FakeAuthorityAdmission.evaluate
        let unknown =
            { baseline with
                Authority =
                    [ { authorityRequest () with
                          Capability = CapabilityId.create "capability.unknown" } ] }
            |> FakeAuthorityAdmission.evaluate
        multiple (fun () ->
            Assert.That(unlimited.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.PartiallyAdmitted)
            Assert.That(unknown.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.PartiallyAdmitted)
            Assert.That(List.length unlimited.Observation.Relationships, Is.EqualTo 1)
            Assert.That(List.length unknown.Observation.Relationships, Is.EqualTo 1)
            Assert.That(unlimited.Observation.Grants, Is.Empty)
            Assert.That(unknown.Observation.Grants, Is.Empty))

    [<Test>]
    member _.``known policy mistakes are applied and attributed locally``() =
        let baseline = request ()
        let rule = baseline.Policy.AuthorityRules |> List.exactlyOne
        let mistaken =
            { rule with
                KnownMistake = true
                Rationale = "fixture marks this local decision as mistaken" }
        let outcome =
            { baseline with
                Policy = { baseline.Policy with AuthorityRules = [ mistaken ] } }
            |> FakeAuthorityAdmission.evaluate
        let finding = outcome.Observation.PolicyMistakes |> List.exactlyOne
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
            Assert.That(List.length outcome.Observation.Grants, Is.EqualTo 1)
            Assert.That(finding.Policy, Is.EqualTo baseline.Policy.Policy)
            Assert.That(finding.Rule, Is.EqualTo mistaken.Rule)
            Assert.That(finding.Request, Is.EqualTo(AuthorityRequestId.value authorityId)))

    [<Test>]
    member _.``missing evidence reference is invalid and effect free``() =
        let input =
            { request () with
                Relationships =
                    [ { relationshipRequest () with
                          Evidence = [ EvidenceId.create "evidence.missing" ] } ] }
        let outcome = FakeAuthorityAdmission.evaluate input
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.InvalidRequest)
            Assert.That(outcome.Failure.Value, Does.Contain "unknown evidence")
            Assert.That(outcome.Observation.Relationships, Is.Empty)
            Assert.That(outcome.Observation.Grants, Is.Empty))

    [<Test>]
    member _.``ambiguous policy or unpresented evidence is invalid``() =
        let baseline = request ()
        let relationshipRule = baseline.Policy.RelationshipRules |> List.exactlyOne
        let authorityRule = baseline.Policy.AuthorityRules |> List.exactlyOne
        let ambiguous =
            { baseline with
                Policy =
                    { baseline.Policy with
                        AuthorityRules = [ { authorityRule with Rule = relationshipRule.Rule } ] } }
            |> FakeAuthorityAdmission.evaluate
        let unpresented =
            { baseline with
                Evidence =
                    [ validEvidence ()
                      { validEvidence () with
                          Evidence = EvidenceId.create "evidence.unpresented" } ] }
            |> FakeAuthorityAdmission.evaluate
        multiple (fun () ->
            Assert.That(ambiguous.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.InvalidRequest)
            Assert.That(ambiguous.Failure.Value, Does.Contain "rule identity")
            Assert.That(unpresented.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.InvalidRequest)
            Assert.That(unpresented.Failure.Value, Does.Contain "not presented")
            Assert.That(ambiguous.Observation.Grants, Is.Empty)
            Assert.That(unpresented.Observation.Grants, Is.Empty))

    [<Test>]
    member _.``independent authority requests partially admit``() =
        let denied =
            { authorityRequest () with
                Request = AuthorityRequestId.create "authority.storage"
                Capability = CapabilityId.create "capability.storage"
                Operation = OperationId.create "storage.write" }
        let outcome =
            { request () with Authority = [ denied; authorityRequest () ] }
            |> FakeAuthorityAdmission.evaluate
        multiple (fun () ->
            Assert.That(outcome.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.PartiallyAdmitted)
            Assert.That(
                outcome.Observation.Grants |> List.map (fun item -> item.Request),
                Is.EqualTo<AuthorityRequestId list> [ authorityId ]
            )
            Assert.That(
                (outcome.Observation.AuthorityDecisions
                 |> List.find (fun item -> item.Request = denied.Request)).Reason,
                Does.Contain "no exact"
            ))

    [<Test>]
    member _.``permuted semantic inputs produce equal observations``() =
        let second = { validEvidence () with Evidence = EvidenceId.create "evidence.signature" }
        let relationship =
            { relationshipRequest () with Evidence = [ second.Evidence; evidenceId ] }
        let baseline =
            { request () with
                Evidence = [ second; validEvidence () ]
                Relationships = [ relationship ] }
        let permuted =
            { baseline with
                Evidence = List.rev baseline.Evidence
                Relationships = [ { relationship with Evidence = List.rev relationship.Evidence } ]
                Policy =
                    { baseline.Policy with
                        TrustedIssuers = List.rev baseline.Policy.TrustedIssuers
                        RelationshipRules = List.rev baseline.Policy.RelationshipRules
                        AuthorityRules = List.rev baseline.Policy.AuthorityRules } }
        Assert.That(
            FakeAuthorityAdmission.evaluate permuted,
            Is.EqualTo(FakeAuthorityAdmission.evaluate baseline)
        )
