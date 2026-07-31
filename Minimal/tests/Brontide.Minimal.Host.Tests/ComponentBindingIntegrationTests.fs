namespace Brontide.Minimal.Host.Tests

open System
open System.IO
open System.Text.Json
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

type private FailingRetirementConversation(inner: IPortableProviderConversation) =
    interface IPortableProviderConversation with
        member _.Realization = inner.Realization
        member _.Establish(required, hostEndpoint, channel) =
            inner.Establish(required, hostEndpoint, channel)
        member _.AwaitReady channel = inner.AwaitReady channel
        member _.Request(plan, channel, request, execution, designation, inputShape, input, resources) =
            inner.Request(
                plan,
                channel,
                request,
                execution,
                designation,
                inputShape,
                input,
                resources)
        member _.Withdraw _ =
            stateViolation
                "withdraw-refused"
                "the test peer refused withdrawal"
            |> Task.FromResult
        member _.Terminate channel = inner.Terminate channel
        member _.Close() = inner.Close()

[<TestFixture>]
type ComponentBindingIntegrationTests() =
    let consumer = DefinitionId.create "def.test.cooling-consumer"
    let provider = DefinitionId.create "def.test.cooling-provider"
    let requirementId = RequirementId.create "req.cooling"
    let contractId = ContractId.create "brontide.fake.cooling"
    let version = VersionLiteral.create "1.0"
    let participant = ActorId.create "actor.cooling-provider"
    let authorityTarget = ActorId.create "actor.cooling-target"
    let authorityEvidence = EvidenceId.create "evidence.cooling-provider"
    let authorityIssuer = IssuerId.create "issuer.integration-host"
    let relationshipId = RelationshipRequestId.create "relationship.cooling-provider"
    let authorityId = AuthorityRequestId.create "authority.cooling-control"
    let capability = CapabilityId.create "capability.cooling-control"
    let operation = OperationId.create "cooling.set-enabled"
    let authorityScope = CapabilityScopeId.create "scope.cooling-session"
    let evaluationTime = DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero)
    let multiple action = Assert.Multiple(Action action)

    let requestFor cardinality declaredAuthority scope =
        let requirement =
            { Requirement = requirementId
              Contract = contractId
              Version = version
              Scope = scope
              Cardinality = cardinality
              AllowSharing = false
              Exposure = ProviderExposure.Distinct
              Mediation = None
              Constraints = []
              ContainingRegion = None
              ContainingPort = None
              RuntimeAttachment = false
              RequiredImports = []
              RequiredExports = []
              RequiredFailurePolicy = None
              RequiredRollbackBoundary = None
              RequestedAuthority = []
              TopologyRequirements = [] }
        let consumerDefinition =
            { Definition = consumer
              Publisher = PublisherId.create "pub.test"
              Provides = []
              Requirements = [ requirement ]
              CompositionParameters = []
              ActivationParameters = []
              RequestedAuthority = [] }
        let providerDefinition =
            { Definition = provider
              Publisher = PublisherId.create "pub.test"
              Provides = [ { Contract = contractId; Version = version } ]
              Requirements = []
              CompositionParameters = []
              ActivationParameters = []
              RequestedAuthority = declaredAuthority }
        let candidate =
            { Definition = provider
              Source = SourceId.create "src.test"
              Publisher = PublisherId.create "pub.test"
              Package = PackageId.create "pkg.test"
              Provides = [ { Contract = contractId; Version = version } ]
              Generic = false
              Sharing =
                { IsolationCompatible = true
                  LifecycleCompatible = true
                  AuthorityCompatible = true }
              Policy = [ { Domain = Trust; Accepted = true; Reason = "trusted test candidate" } ]
              Evidence = [ EvidenceId.create "ev.test" ]
              Authority = []
              FailureDomain = "failure.test"
              AttachmentNode = None }
        { Request = ResolutionRequestId.create "resolution.integration"
          Generation = GenerationId.create "gen.integration"
          ActiveGeneration = None
          RestartScope = RestartScopeId.create "restart.integration"
          Roots = [ consumer ]
          Definitions = [ consumerDefinition; providerDefinition ]
          Candidates = [ candidate ]
          ExistingOccurrences = []
          OccupiedBindings = []
          Preferences = []
          AuthorisedReplacements = []
          CompositionParameters = []
          ActivationParameters = []
          PreselectedProviders = []
          Ports = []
          TopologyClaims = [] }

    let requestWith cardinality declaredAuthority =
        requestFor
            cardinality
            declaredAuthority
            (Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.create "scope.cooling")

    let request cardinality = requestWith cardinality []

    let resolve cardinality =
        request cardinality |> FakeGenerationResolver.resolve

    let memberOf = function
        | ResolutionOutcome.Resolved(_, generation) ->
            generation.ProviderSets |> List.exactlyOne |> fun set -> set.Members |> List.exactlyOne
        | outcome -> failwithf "Expected a resolved generation, got %A." outcome

    let selection (memberValue: ProviderSetMember) =
        { Requirement = requirementId
          Definition = memberValue.Definition
          Occurrence = memberValue.Occurrence
          Component = CoolingFixture.component'
          Provider = CoolingFixture.provider
          HostEndpoint = "minimal-component-host"
          ProviderEndpoint = "cooling-provider"
          RequiredContract = CoolingFixture.contract }

    let preparedWith declaredAuthority =
        let resolution =
            requestWith (Cardinality.parse "1..1") declaredAuthority
            |> FakeGenerationResolver.resolve
        let memberValue = memberOf resolution
        resolution, selection memberValue, memberValue.Occurrence

    let prepared () = preparedWith []

    let activationMember occurrence =
        { Occurrence = occurrence
          Definition = DefinitionId.create (sprintf "def.%s" (OccurrenceId.value occurrence))
          Region = RegionId.create "region.integration"
          Provides = [ { Contract = contractId; Version = version } ]
          RequiredReadyInputs = []
          AvailableReadyInputs = []
          WaitsForReadyOf = [] }

    let plan occurrences =
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.integration"
              Generation = GenerationId.create "gen.lifecycle"
              RestartScope = RestartScopeId.create "restart.lifecycle"
              Members = occurrences |> List.map activationMember
              Edges = []
              Protocols = []
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "Expected a CM3 plan, got %A." outcome

    let runtimeRequest planValue =
        let retained = GenerationId.create "gen.retained"
        { Attempt = ActivationAttemptId.create "activation.integration"
          Plan = planValue
          RequestedRestartScope = planValue.RestartScope
          RetainedGeneration = retained
          ActiveScopes =
            [ { Scope = planValue.RestartScope
                Generation = retained
                Status = RuntimeScopeStatus.ActiveScope } ]
          Preparation = None
          StageOutcomes = []
          InteractionAttempts = []
          BindingExercises = []
          Release =
            { Release = ReleaseId.create "release.integration"
              FailureMoment = ReleaseFailureMoment.NoReleaseFailure }
          Rollback = RollbackAvailability.Available
          RetainedDisposition = RetainedGenerationDisposition.TerminateAfterRelease
          Child = None }

    let directCooling document =
        PortableDirectConversation(
            PortableProviderEndpoint(document, CoolingHandler(), Realization.FixedDirectCall))
        :> IPortableProviderConversation

    let expectProvider name =
        match PortableProviderRef.tryCreate name 1 with
        | Ok value -> value
        | Error error -> failwithf "Expected provider reference, got %A." error

    let admission () : AuthorityAdmissionRequest =
        let relationship =
            { Request = relationshipId
              ProposedActor = participant
              Kind = ActorRelationshipKind.ComponentParticipant
              Evidence = [ authorityEvidence ] }
        let authority =
            { Request = authorityId
              Relationship = relationshipId
              Capability = capability
              Target = authorityTarget
              Operation = operation
              Scope = authorityScope
              Unlimited = false }
        { Request = AdmissionRequestId.create "admission.integration"
          Participant = participant
          EvaluationTime = evaluationTime
          Evidence =
            [ { Evidence = authorityEvidence
                Issuer = authorityIssuer
                Subject = participant
                Verification = AdmissionEvidenceVerification.Verified
                ValidFrom = evaluationTime.AddHours(-1.0)
                ExpiresAt = evaluationTime.AddHours(1.0)
                State = AdmissionEvidenceState.Current } ]
          Relationships = [ relationship ]
          Authority = [ authority ]
          Policy =
            { Policy = AuthorityPolicyId.create "policy.integration"
              TrustedIssuers = [ authorityIssuer ]
              RelationshipRules =
                [ { Rule = PolicyRuleId.create "rule.component-participant"
                    ProposedActor = participant
                    Kind = ActorRelationshipKind.ComponentParticipant
                    Disposition = PolicyDisposition.Allow
                    LocalActor = Some(LocalActorReferenceId.create "local.cooling-provider")
                    RequiredEvidence = [ authorityEvidence ]
                    KnownMistake = false
                    Rationale = "component participant admitted" } ]
              AuthorityRules =
                [ { Rule = PolicyRuleId.create "rule.cooling-control"
                    RelationshipKind = ActorRelationshipKind.ComponentParticipant
                    Capability = capability
                    Target = authorityTarget
                    Operation = operation
                    Scope = authorityScope
                    Disposition = PolicyDisposition.Allow
                    KnownMistake = false
                    Rationale = "narrow cooling control admitted" } ] } }

    let supervisor = ActorId.create "actor.cooling-supervisor"
    let supervisorEvidence = EvidenceId.create "evidence.cooling-supervisor"
    let supervisorRelationshipId = RelationshipRequestId.create "relationship.cooling-supervisor"
    let reportAuthorityId = AuthorityRequestId.create "authority.cooling-report"
    let auditAuthorityId = AuthorityRequestId.create "authority.cooling-audit"
    let reportCapability = CapabilityId.create "capability.cooling-report"
    let auditCapability = CapabilityId.create "capability.cooling-audit"
    let reportOperation = OperationId.create "cooling.read-state"
    let auditOperation = OperationId.create "cooling.read-log"
    let providerLocalActor = LocalActorReferenceId.create "local.cooling-provider"
    let supervisorLocalActor = LocalActorReferenceId.create "local.cooling-supervisor"
    let observer = ActorId.create "actor.cooling-observer"
    let observerEvidence = EvidenceId.create "evidence.cooling-observer"
    let observerRelationshipId = RelationshipRequestId.create "relationship.cooling-observer"
    let observeAuthorityId = AuthorityRequestId.create "authority.cooling-observe"
    let observeCapability = CapabilityId.create "capability.cooling-observe"
    let observeOperation = OperationId.create "cooling.observe"
    let observerLocalActor = LocalActorReferenceId.create "local.cooling-observer"
    let deputy = ActorId.create "actor.cooling-deputy"
    let deputyEvidence = EvidenceId.create "evidence.cooling-deputy"
    let deputyRelationshipId = RelationshipRequestId.create "relationship.cooling-deputy"
    let deputyAuthorityId = AuthorityRequestId.create "authority.cooling-deputy-audit"
    let deputyLocalActor = LocalActorReferenceId.create "local.cooling-deputy"
    let declaredAuthority = [ "cooling.control"; "cooling.audit" ]

    let setEvidence evidence subject : AdmissionEvidence =
        { Evidence = evidence
          Issuer = authorityIssuer
          Subject = subject
          Verification = AdmissionEvidenceVerification.Verified
          ValidFrom = evaluationTime.AddHours(-1.0)
          ExpiresAt = evaluationTime.AddHours(1.0)
          State = AdmissionEvidenceState.Current }

    let setPolicyFor supervisorActor observerActor deputyActor : LocalAuthorityPolicy =
        { Policy = AuthorityPolicyId.create "policy.integration-set"
          TrustedIssuers = [ authorityIssuer ]
          RelationshipRules =
            [ { Rule = PolicyRuleId.create "rule.component-participant"
                ProposedActor = participant
                Kind = ActorRelationshipKind.ComponentParticipant
                Disposition = PolicyDisposition.Allow
                LocalActor = Some providerLocalActor
                RequiredEvidence = [ authorityEvidence ]
                KnownMistake = false
                Rationale = "component participant admitted" }
              { Rule = PolicyRuleId.create "rule.component-supervisor"
                ProposedActor = supervisor
                Kind = ActorRelationshipKind.ComponentParticipant
                Disposition = PolicyDisposition.Allow
                LocalActor = Some supervisorActor
                RequiredEvidence = [ supervisorEvidence ]
                KnownMistake = false
                Rationale = "component supervisor admitted" }
              { Rule = PolicyRuleId.create "rule.component-observer"
                ProposedActor = observer
                Kind = ActorRelationshipKind.ComponentParticipant
                Disposition = PolicyDisposition.Allow
                LocalActor = Some observerActor
                RequiredEvidence = [ observerEvidence ]
                KnownMistake = false
                Rationale = "component observer admitted" }
              { Rule = PolicyRuleId.create "rule.component-deputy"
                ProposedActor = deputy
                Kind = ActorRelationshipKind.ComponentParticipant
                Disposition = PolicyDisposition.Allow
                LocalActor = Some deputyActor
                RequiredEvidence = [ deputyEvidence ]
                KnownMistake = false
                Rationale = "component deputy admitted" } ]
          AuthorityRules =
            [ { Rule = PolicyRuleId.create "rule.cooling-control"
                RelationshipKind = ActorRelationshipKind.ComponentParticipant
                Capability = capability
                Target = authorityTarget
                Operation = operation
                Scope = authorityScope
                Disposition = PolicyDisposition.Allow
                KnownMistake = false
                Rationale = "narrow cooling control admitted" }
              { Rule = PolicyRuleId.create "rule.cooling-report"
                RelationshipKind = ActorRelationshipKind.ComponentParticipant
                Capability = reportCapability
                Target = authorityTarget
                Operation = reportOperation
                Scope = authorityScope
                Disposition = PolicyDisposition.Allow
                KnownMistake = false
                Rationale = "narrow cooling reporting admitted" }
              { Rule = PolicyRuleId.create "rule.cooling-audit"
                RelationshipKind = ActorRelationshipKind.ComponentParticipant
                Capability = auditCapability
                Target = authorityTarget
                Operation = auditOperation
                Scope = authorityScope
                Disposition = PolicyDisposition.Allow
                KnownMistake = false
                Rationale = "narrow cooling audit admitted" }
              { Rule = PolicyRuleId.create "rule.cooling-observe"
                RelationshipKind = ActorRelationshipKind.ComponentParticipant
                Capability = observeCapability
                Target = authorityTarget
                Operation = observeOperation
                Scope = authorityScope
                Disposition = PolicyDisposition.Allow
                KnownMistake = false
                Rationale = "narrow cooling observation admitted" } ] }

    let setPolicyWith supervisorActor observerActor =
        setPolicyFor supervisorActor observerActor deputyLocalActor

    let setPolicy supervisorActor = setPolicyWith supervisorActor observerLocalActor

    let deputyRequest policy : AuthorityAdmissionRequest =
        { Request = AdmissionRequestId.create "admission.set-deputy"
          Participant = deputy
          EvaluationTime = evaluationTime
          Evidence = [ setEvidence deputyEvidence deputy ]
          Relationships =
            [ { Request = deputyRelationshipId
                ProposedActor = deputy
                Kind = ActorRelationshipKind.ComponentParticipant
                Evidence = [ deputyEvidence ] } ]
          Authority =
            [ { Request = deputyAuthorityId
                Relationship = deputyRelationshipId
                Capability = auditCapability
                Target = authorityTarget
                Operation = auditOperation
                Scope = authorityScope
                Unlimited = false } ]
          Policy = policy }

    let dependency definition : ComponentGrantDependency =
        { Definition = definition
          Entries =
            [ { DeclaredAuthority = "cooling.control"
                Capability = capability
                Target = authorityTarget
                Operation = operation
                Scope = authorityScope }
              { DeclaredAuthority = "cooling.audit"
                Capability = auditCapability
                Target = authorityTarget
                Operation = auditOperation
                Scope = authorityScope } ] }

    let secondaryRequirementId = RequirementId.create "req.cooling-secondary"
    let secondaryProvider = DefinitionId.create "def.test.cooling-secondary"
    let secondaryContractId = ContractId.create "brontide.fake.cooling-secondary"

    /// Two independent requirements, so the generation resolves two distinct occurrences.
    let pairRequest () =
        let single = request (Cardinality.parse "1..1")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let providerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = provider)
        let candidate = List.exactlyOne single.Candidates
        let secondaryRequirement =
            { List.exactlyOne consumerDefinition.Requirements with
                Requirement = secondaryRequirementId
                Contract = secondaryContractId }
        { single with
            Definitions =
                [ { consumerDefinition with
                      Requirements =
                        consumerDefinition.Requirements @ [ secondaryRequirement ] }
                  providerDefinition
                  { providerDefinition with
                      Definition = secondaryProvider
                      Provides = [ { Contract = secondaryContractId; Version = version } ] } ]
            Candidates =
                [ candidate
                  { candidate with
                      Definition = secondaryProvider
                      Provides = [ { Contract = secondaryContractId; Version = version } ] } ] }

    /// A genuinely cyclic group: one strongly connected component carrying protocols.
    let protocolPlan (occurrences: OccurrenceId list) =
        let forward = ActivationEdgeId.create "edge.forward"
        let backward = ActivationEdgeId.create "edge.backward"
        let forwardProtocol = LifecycleProtocolId.create "protocol.forward"
        let backwardProtocol = LifecycleProtocolId.create "protocol.backward"
        let declare identity edge from' target : LifecycleProtocolDeclaration =
            { Protocol = identity
              Edge = edge
              From = from'
              To = target
              Operation = LifecycleOperationId.create "lifecycle.handshake"
              Authority = [ CapabilityId.create "capability.lifecycle-handshake" ]
              InputShape = ShapeId.create "shape.handshake-in"
              OutputShape = ShapeId.create "shape.handshake-out"
              Ordering = "ordered"
              TimeoutMilliseconds = 1000
              RetryLimit = 0
              Idempotent = true
              Completion = "acknowledged"
              Failure = "abort"
              Rollback = "release" }
        let edge identity from' target protocol : ActivationDependency =
            { Edge = identity
              From = from'
              To = target
              Kind = ActivationDependencyKind.RelationalInitialisation
              Contract = contractId
              Version = version
              ObservedBeforeRelease = true
              Protocol = Some protocol
              CrossingPort = None
              AllowWiderRegionProposal = false }
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.integration"
              Generation = GenerationId.create "gen.lifecycle"
              RestartScope = RestartScopeId.create "restart.lifecycle"
              Members = occurrences |> List.map activationMember
              Edges =
                [ edge forward (List.item 0 occurrences) (List.item 1 occurrences) forwardProtocol
                  edge backward (List.item 1 occurrences) (List.item 0 occurrences) backwardProtocol ]
              Protocols =
                [ declare forwardProtocol forward (List.item 0 occurrences) (List.item 1 occurrences)
                  declare backwardProtocol backward (List.item 1 occurrences) (List.item 0 occurrences) ]
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "Expected a cyclic CM3 plan, got %A." outcome

    /// The receiving-domain policy, with the participant's own local Actor overridable.
    let groupPolicy participantActor supervisorActor : LocalAuthorityPolicy =
        let policy = setPolicyWith supervisorActor observerLocalActor
        { policy with
            RelationshipRules =
                policy.RelationshipRules
                |> List.map (fun rule ->
                    if rule.ProposedActor = participant then
                        { rule with LocalActor = Some participantActor }
                    else
                        rule) }

    let providerAuthority policy authority : AuthorityAdmissionRequest =
        { Request = AdmissionRequestId.create "admission.group-provider"
          Participant = participant
          EvaluationTime = evaluationTime
          Evidence = [ setEvidence authorityEvidence participant ]
          Relationships =
            [ { Request = relationshipId
                ProposedActor = participant
                Kind = ActorRelationshipKind.ComponentParticipant
                Evidence = [ authorityEvidence ] } ]
          Authority =
            [ if authority = authorityId then
                  { Request = authorityId
                    Relationship = relationshipId
                    Capability = capability
                    Target = authorityTarget
                    Operation = operation
                    Scope = authorityScope
                    Unlimited = false }
              else
                  { Request = authority
                    Relationship = relationshipId
                    Capability = reportCapability
                    Target = authorityTarget
                    Operation = reportOperation
                    Scope = authorityScope
                    Unlimited = false } ]
          Policy = policy }

    let supervisorAuthority policy authority revoked : AuthorityAdmissionRequest =
        let evidence = setEvidence supervisorEvidence supervisor
        { Request = AdmissionRequestId.create "admission.group-supervisor"
          Participant = supervisor
          EvaluationTime = evaluationTime
          Evidence =
            [ if revoked then
                  { evidence with State = AdmissionEvidenceState.Revoked }
              else
                  evidence ]
          Relationships =
            [ { Request = supervisorRelationshipId
                ProposedActor = supervisor
                Kind = ActorRelationshipKind.ComponentParticipant
                Evidence = [ supervisorEvidence ] } ]
          Authority =
            [ { Request = authority
                Relationship = supervisorRelationshipId
                Capability = auditCapability
                Target = authorityTarget
                Operation = auditOperation
                Scope = authorityScope
                Unlimited = false } ]
          Policy = policy }

    let groupRevalidationToken kind =
        match kind with
        | ComponentGroupRevalidationKind.Continued -> "continued"
        | ComponentGroupRevalidationKind.Withdrawn -> "withdrawn"
        | ComponentGroupRevalidationKind.RetirementFailed -> "retirement-failed"
        | ComponentGroupRevalidationKind.ActivationUnavailable -> "activation-unavailable"

    let groupAuthorityToken kind =
        match kind with
        | ComponentGroupAuthorityFailureKind.IdentityNotDistinct -> "identity-not-distinct"
        | ComponentGroupAuthorityFailureKind.MemberAuthorityRefused -> "member-authority-refused"
        | ComponentGroupAuthorityFailureKind.ActorMappingInconsistent -> "actor-mapping-inconsistent"
        | ComponentGroupAuthorityFailureKind.ActivationRefused -> "activation-refused"

    let groupFailureToken kind =
        match kind with
        | ComponentGroupActivationFailureKind.PlanUnsupported -> "plan-unsupported"
        | ComponentGroupActivationFailureKind.PreparationUnavailable -> "preparation-unavailable"
        | ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart -> "runtime-refused-before-start"
        | ComponentGroupActivationFailureKind.MemberEstablishmentRefused ->
            "member-establishment-refused"
        | ComponentGroupActivationFailureKind.MemberReleaseRefused -> "member-release-refused"

    let controlOnlyDependency definition : ComponentGrantDependency =
        { Definition = definition
          Entries =
            [ { DeclaredAuthority = "cooling.control"
                Capability = capability
                Target = authorityTarget
                Operation = operation
                Scope = authorityScope } ] }

    let successionToken kind =
        match kind with
        | ComponentDeclarationSuccessionKind.Narrowed -> "narrowed"
        | ComponentDeclarationSuccessionKind.Declined -> "declined"
        | ComponentDeclarationSuccessionKind.ActivationUnavailable -> "activation-unavailable"

    let verdictToken kind =
        match kind with
        | ComponentInteractionVerdictKind.Consistent -> "consistent"
        | ComponentInteractionVerdictKind.UndeclaredUse -> "undeclared-use"
        | ComponentInteractionVerdictKind.UngrantedUse -> "ungranted-use"
        | ComponentInteractionVerdictKind.RetirementFailed -> "retirement-failed"
        | ComponentInteractionVerdictKind.Declined -> "declined"
        | ComponentInteractionVerdictKind.ActivationUnavailable -> "activation-unavailable"

    let revisionToken kind =
        match kind with
        | ComponentParticipantRevisionKind.Revised -> "revised"
        | ComponentParticipantRevisionKind.Declined -> "declined"
        | ComponentParticipantRevisionKind.Withdrawn -> "withdrawn"
        | ComponentParticipantRevisionKind.RetirementFailed -> "retirement-failed"
        | ComponentParticipantRevisionKind.ActivationUnavailable -> "activation-unavailable"

    let observerRequest policy : AuthorityAdmissionRequest =
        { Request = AdmissionRequestId.create "admission.set-observer"
          Participant = observer
          EvaluationTime = evaluationTime
          Evidence = [ setEvidence observerEvidence observer ]
          Relationships =
            [ { Request = observerRelationshipId
                ProposedActor = observer
                Kind = ActorRelationshipKind.ComponentParticipant
                Evidence = [ observerEvidence ] } ]
          Authority =
            [ { Request = observeAuthorityId
                Relationship = observerRelationshipId
                Capability = observeCapability
                Target = authorityTarget
                Operation = observeOperation
                Scope = authorityScope
                Unlimited = false } ]
          Policy = policy }

    let participantSetWith occurrence supervisorActor observerActor : ComponentParticipantRequest list =
        let policy = setPolicyWith supervisorActor observerActor
        [ { Mapping =
              { Occurrence = occurrence
                Participant = participant }
            Request =
              { Request = AdmissionRequestId.create "admission.set-provider"
                Participant = participant
                EvaluationTime = evaluationTime
                Evidence = [ setEvidence authorityEvidence participant ]
                Relationships =
                  [ { Request = relationshipId
                      ProposedActor = participant
                      Kind = ActorRelationshipKind.ComponentParticipant
                      Evidence = [ authorityEvidence ] } ]
                Authority =
                  [ { Request = authorityId
                      Relationship = relationshipId
                      Capability = capability
                      Target = authorityTarget
                      Operation = operation
                      Scope = authorityScope
                      Unlimited = false }
                    { Request = reportAuthorityId
                      Relationship = relationshipId
                      Capability = reportCapability
                      Target = authorityTarget
                      Operation = reportOperation
                      Scope = authorityScope
                      Unlimited = false } ]
                Policy = policy } }
          { Mapping =
              { Occurrence = occurrence
                Participant = supervisor }
            Request =
              { Request = AdmissionRequestId.create "admission.set-supervisor"
                Participant = supervisor
                EvaluationTime = evaluationTime
                Evidence = [ setEvidence supervisorEvidence supervisor ]
                Relationships =
                  [ { Request = supervisorRelationshipId
                      ProposedActor = supervisor
                      Kind = ActorRelationshipKind.ComponentParticipant
                      Evidence = [ supervisorEvidence ] } ]
                Authority =
                  [ { Request = auditAuthorityId
                      Relationship = supervisorRelationshipId
                      Capability = auditCapability
                      Target = authorityTarget
                      Operation = auditOperation
                      Scope = authorityScope
                      Unlimited = false } ]
                Policy = policy } } ]

    let participantSet occurrence supervisorActor =
        participantSetWith occurrence supervisorActor observerLocalActor

    let participantTrio occurrence policy : ComponentParticipantRequest list =
        let pair =
            participantSet occurrence supervisorLocalActor
            |> List.map (fun entry ->
                { entry with
                    Request = { entry.Request with Policy = policy } })
        pair
        @ [ { Mapping =
                { Occurrence = occurrence
                  Participant = observer }
              Request = observerRequest policy } ]

    let revoked (entry: ComponentParticipantRequest) =
        { entry with
            Request =
              { entry.Request with
                  Evidence =
                    entry.Request.Evidence
                    |> List.map (fun evidence ->
                        { evidence with State = AdmissionEvidenceState.Revoked }) } }

    let withAuthority (entry: ComponentParticipantRequest) authority =
        { entry with
            Request = { entry.Request with Authority = authority } }

    let revokedRequest (request: AuthorityAdmissionRequest) =
        { request with
            Evidence =
              request.Evidence
              |> List.map (fun evidence ->
                  { evidence with State = AdmissionEvidenceState.Revoked }) }

    let expiredRequest (request: AuthorityAdmissionRequest) =
        { request with
            EvaluationTime = request.Evidence |> List.map _.ExpiresAt |> List.max }

    let relabelled (request: AuthorityAdmissionRequest) actor =
        { request with
            Request = AdmissionRequestId.create (sprintf "admission.set-%s" (ActorId.value actor))
            Participant = actor
            Evidence = request.Evidence |> List.map (fun evidence -> { evidence with Subject = actor })
            Relationships =
              request.Relationships
              |> List.map (fun relationship -> { relationship with ProposedActor = actor }) }

    let setRevalidationToken kind =
        match kind with
        | ComponentParticipantRevalidationKind.Continued -> "continued"
        | ComponentParticipantRevalidationKind.Withdrawn -> "withdrawn"
        | ComponentParticipantRevalidationKind.RetirementFailed -> "retirement-failed"
        | ComponentParticipantRevalidationKind.ActivationUnavailable -> "activation-unavailable"

    let extensionToken kind =
        match kind with
        | ComponentParticipantExtensionKind.Extended -> "extended"
        | ComponentParticipantExtensionKind.Declined -> "declined"
        | ComponentParticipantExtensionKind.Withdrawn -> "withdrawn"
        | ComponentParticipantExtensionKind.RetirementFailed -> "retirement-failed"
        | ComponentParticipantExtensionKind.ActivationUnavailable -> "activation-unavailable"

    let participantFailureToken kind =
        match kind with
        | ComponentParticipantAdmissionFailureKind.ParticipantSetInvalid -> "participant-set-invalid"
        | ComponentParticipantAdmissionFailureKind.AuthorityShapeUnsupported ->
            "authority-shape-unsupported"
        | ComponentParticipantAdmissionFailureKind.AuthorityRefused -> "authority-refused"
        | ComponentParticipantAdmissionFailureKind.LocalIdentityConflict -> "local-identity-conflict"
        | ComponentParticipantAdmissionFailureKind.LifecycleRefused -> "lifecycle-refused"

    let portableFacts (result: ComponentParticipantAdmissionResult) =
        let memberValue = result.Lifecycle.Value.Member.Value
        let planFacts =
            memberValue.TryPlan
            |> Option.map (fun plan ->
                BindingPlan.factNames plan
                |> List.choose (fun name ->
                    BindingPlan.tryFact name plan |> Option.map (fun value -> name, value))
                |> Map.ofList)
            |> Option.defaultValue Map.empty
            |> Map.remove "planId"
        (memberValue.ResolutionFacts, planFacts)
        ||> Map.fold (fun state key value -> Map.add key value state)

    [<Test>]
    member _.``completed direct one to one resolution enters portable preflight``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let result =
            memberOf resolution
            |> selection
            |> ComponentBindingIntegration.prepare resolution
        match result with
        | Prepared memberValue ->
            multiple (fun () ->
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "local-initialisation")
                Assert.That(memberValue.TryPlan, Is.EqualTo(None))
                Assert.That(memberValue.TryFact "bindingScope", Is.EqualTo(Some "scope.cooling"))
                Assert.That(
                    memberValue.TryFact "selectedProvision",
                    Is.EqualTo(Some(PortableProviderRef.text CoolingFixture.provider)))
                Assert.That(resolution.Effects, Is.EqualTo Cm2EffectObservation.none))
        | Refused failure -> Assert.Fail(sprintf "Expected preparation, got %A." failure)

    [<Test>]
    member _.``explicit mapping cannot name a different occurrence``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let mapping =
            { selection (memberOf resolution) with
                Occurrence = OccurrenceId.create "occ.unselected" }
        match ComponentBindingIntegration.prepare resolution mapping with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.SelectionMismatch)
                Assert.That(failure.Code, Is.EqualTo "selection-mismatch"))
        | Prepared _ -> Assert.Fail "A mismatched occurrence reached portable preflight."

    [<Test>]
    member _.``wider provider set is refused instead of narrowed``() =
        let resolution = resolve (Cardinality.parse "1..2")
        match ComponentBindingIntegration.prepare resolution (selection (memberOf resolution)) with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.CardinalityUnsupported)
                Assert.That(failure.Code, Is.EqualTo "cardinality-unsupported"))
        | Prepared _ -> Assert.Fail "A wider Provider Set was narrowed into a portable member."

    [<Test>]
    member _.``refused resolution never reaches portable preflight``() =
        let resolution =
            FakeGenerationResolver.resolve
                { request (Cardinality.parse "1..1") with Candidates = [] }
        let synthetic =
            { Definition = provider
              Occurrence = OccurrenceId.create "occ.synthetic"
              Source = None
              Publisher = PublisherId.create "pub.test"
              Package = None
              Retained = false
              Evidence = []
              Authority = []
              FailureDomain = "failure.synthetic"
              AttachmentNode = None }
        match ComponentBindingIntegration.prepare resolution (selection synthetic) with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.ResolutionNotComplete)
                Assert.That(resolution.Effects, Is.EqualTo Cm2EffectObservation.none))
        | Prepared _ -> Assert.Fail "A refused resolution reached portable preflight."

    [<Test>]
    member _.``missing endpoint designation is refused before portable preflight``() =
        let resolution = resolve (Cardinality.parse "1..1")
        let mapping =
            { selection (memberOf resolution) with ProviderEndpoint = "" }
        match ComponentBindingIntegration.prepare resolution mapping with
        | Refused failure ->
            multiple (fun () ->
                Assert.That(
                    failure.Kind,
                    Is.EqualTo ComponentBindingIntegrationFailureKind.MappingInvalid)
                Assert.That(failure.Code, Is.EqualTo "endpoint-invalid"))
        | Prepared _ -> Assert.Fail "An empty endpoint reached portable preflight."

    [<Test>]
    member _.``singleton lifecycle derives CM4 stages and releases only after Active``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let planValue = plan [ occurrence ]
            let group = planValue.Groups |> List.exactlyOne
            let request =
                { runtimeRequest planValue with
                    StageOutcomes =
                        [ { Group = group.Group
                            Member = occurrence
                            Stage = ActivationStage.LocalInitialisation
                            Succeeded = false
                            Detail = "untrusted caller claim" } ] }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    request
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, None ->
                multiple (fun () ->
                    Assert.That(runtime.Kind, Is.EqualTo ActivationRuntimeOutcomeKind.Active)
                    Assert.That(runtime.Observation.Effects.Released, Is.True)
                    Assert.That(runtime.Observation.Effects.CapabilityGranted, Is.False)
                    Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released")
                    Assert.That(memberValue.TryPlan.IsSome, Is.True))
            | state -> Assert.Fail(sprintf "Expected Active lifecycle, got %A." state)
        }

    [<Test>]
    member _.``CM4 preflight refusal prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let request =
                { runtimeRequest (plan [ occurrence ]) with
                    Release =
                        { Release = ReleaseId.create "release.integration"
                          FailureMoment = ReleaseFailureMoment.BeforeCutover } }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    request
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart)
                    Assert.That(
                        runtime.Kind,
                        Is.EqualTo ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation")
                    Assert.That(memberValue.TryPlan, Is.EqualTo None))
            | state -> Assert.Fail(sprintf "Expected preflight refusal, got %A." state)
        }

    [<Test>]
    member _.``unsupported activation group is refused before provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let extra = OccurrenceId.create "occ.extra"
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ occurrence; extra ]))
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, None, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PlanUnsupported)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation"))
            | state -> Assert.Fail(sprintf "Expected unsupported-plan refusal, got %A." state)
        }

    [<Test>]
    member _.``activation plan cannot replace the CBI1 selected occurrence``() =
        task {
            let resolution, selected, _ = prepared ()
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ OccurrenceId.create "occ.replacement" ]))
                    (directCooling CoolingFixture.contract)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, None, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PlanUnsupported)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo "local-initialisation")
                    Assert.That(memberValue.TryPlan, Is.EqualTo None))
            | state -> Assert.Fail(sprintf "Expected selected-occurrence refusal, got %A." state)
        }

    [<Test>]
    member _.``portable interconnection refusal becomes CM4 establishment failure``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let! result =
                ComponentBindingLifecycle.activate
                    resolution
                    selected
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling substituted)

            match result.Member, result.Runtime, result.Failure with
            | Some memberValue, Some runtime, Some failure ->
                multiple (fun () ->
                    Assert.That(
                        failure.Kind,
                        Is.EqualTo ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused)
                    Assert.That(
                        runtime.Kind,
                        Is.EqualTo ActivationRuntimeOutcomeKind.EstablishmentFailed)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.Not.EqualTo "released"))
            | state -> Assert.Fail(sprintf "Expected establishment refusal, got %A." state)
        }

    [<Test>]
    member _.``exact CM5 admission gates one released Active member``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling CoolingFixture.contract)

            match result.Authority, result.Lifecycle, result.Failure with
            | Some authority, Some lifecycle, None ->
                let memberValue = lifecycle.Member |> Option.get
                let bindingPlan = memberValue.TryPlan |> Option.get
                multiple (fun () ->
                    Assert.That(authority.Kind, Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
                    Assert.That(authority.Observation.Relationships, Has.Length.EqualTo 1)
                    Assert.That(authority.Observation.Grants, Has.Length.EqualTo 1)
                    Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released")
                    Assert.That((BindingPlan.authority bindingPlan).NoCapabilityTransfer, Is.True))
            | state -> Assert.Fail(sprintf "Expected authority-gated activation, got %A." state)
        }

    [<Test>]
    member _.``authority mapping mismatch stops before CM5 and portable preflight``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence
                      Participant = ActorId.create "actor.other" }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.MappingInvalid)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``revoked CM5 evidence prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let denied =
                { admission () with
                    Evidence =
                        (admission ()).Evidence
                        |> List.map (fun evidence ->
                            { evidence with State = AdmissionEvidenceState.Revoked }) }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    denied
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityRefused)
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.Denied)
                Assert.That(result.Authority.Value.Observation.Grants, Is.Empty)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``additional authority request is refused before CM5 evaluation``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let baseline = admission ()
            let additional =
                { List.exactlyOne baseline.Authority with
                    Request = AuthorityRequestId.create "authority.additional" }
            let wider = { baseline with Authority = baseline.Authority @ [ additional ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    wider
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``caller authored CM4 binding authority is refused before CM5 evaluation``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let runtime =
                { runtimeRequest (plan [ occurrence ]) with
                    BindingExercises =
                        [ { Exercise = BindingExerciseId.create "exercise.caller"
                            Binding = BindingId.create "binding.caller"
                            Consumer = occurrence
                            Provider = occurrence
                            Source = SourceId.create "source.caller"
                            Exposure = BindingExposureKind.Distinct
                            Mediation = None
                            Routing = RoutingDecisionId.create "routing.caller"
                            AuthorityAdmitted = true
                            Delivery = BindingDeliveryResult.Delivered
                            Failure = None } ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    runtime
                    (admission ())
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported)
                Assert.That(result.Authority, Is.EqualTo None)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``structurally invalid CM5 request prevents provider contact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let baseline = admission ()
            let invalid =
                { baseline with
                    Evidence =
                        [ List.exactlyOne baseline.Evidence
                          List.exactlyOne baseline.Evidence ] }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    invalid
                    (directCooling CoolingFixture.contract)

            multiple (fun () ->
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.AuthorityRefused)
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.InvalidRequest)
                Assert.That(result.Lifecycle, Is.EqualTo None))
        }

    [<Test>]
    member _.``portable failure remains inactive after CM5 admission``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let! result =
                ComponentAuthorityIntegration.activate
                    resolution
                    selected
                    { Occurrence = occurrence; Participant = participant }
                    (runtimeRequest (plan [ occurrence ]))
                    (admission ())
                    (directCooling substituted)

            multiple (fun () ->
                Assert.That(
                    result.Authority.Value.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
                Assert.That(
                    result.Failure.Value.Kind,
                    Is.EqualTo ComponentAuthorityIntegrationFailureKind.LifecycleRefused)
                Assert.That(
                    CompositionStage.token result.Lifecycle.Value.Member.Value.Stage,
                    Is.Not.EqualTo "released"))
        }

    [<Test>]
    member _.``shared CBI4 vectors pin the complete native profiles``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi4-integrated-comparison-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI4 vector identity must be a string"
                    | value -> value
                let resolution, originalSelection, occurrence = prepared ()
                let selected =
                    { originalSelection with
                        HostEndpoint = "component-comparison-host" }
                let mutable mapping =
                    { Occurrence = occurrence
                      Participant = participant }
                let mutable authority = admission ()
                let mutable document = CoolingFixture.contract
                match scenario with
                | "cbi4-01-active" -> ()
                | "cbi4-02-authority-denied" ->
                    authority <-
                        { authority with
                            Evidence =
                                authority.Evidence
                                |> List.map (fun evidence ->
                                    { evidence with
                                        State = AdmissionEvidenceState.Revoked }) }
                | "cbi4-03-authority-shape" ->
                    authority <-
                        { authority with
                            Authority =
                                authority.Authority
                                @ [ { List.exactlyOne authority.Authority with
                                        Request =
                                            AuthorityRequestId.create "authority.additional" } ] }
                | "cbi4-04-mapping" ->
                    mapping <-
                        { mapping with
                            Participant = ActorId.create "actor.other" }
                | "cbi4-05-lifecycle" ->
                    document <-
                        { document with
                            Provider = expectProvider "brontide.fake.substituted" }
                | other -> invalidArg (nameof scenario) (sprintf "unknown CBI4 vector %s" other)
                let! result =
                    ComponentAuthorityIntegration.activate
                        resolution
                        selected
                        mapping
                        (runtimeRequest (plan [ occurrence ]))
                        authority
                        (directCooling document)
                let profile = ComponentAuthorityComparison.profile scenario result
                let digest = ComponentAuthorityComparison.digest profile
                use parsedProfile = JsonDocument.Parse profile
                multiple (fun () ->
                    Assert.That(
                        digest,
                        Is.EqualTo(vector.GetProperty("expectedProfileSha256").GetString()),
                        scenario)
                    Assert.That(
                        parsedProfile.RootElement.GetProperty("active").GetBoolean(),
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    let expectedFailure =
                        let value = vector.GetProperty("expectedIntegrationFailure")
                        if value.ValueKind = JsonValueKind.Null then null else value.GetString()
                    let actualFailure =
                        let value = parsedProfile.RootElement.GetProperty("integrationFailure")
                        if value.ValueKind = JsonValueKind.Null then
                            null
                        else
                            value.GetProperty("kind").GetString()
                    Assert.That(actualFailure, Is.EqualTo(expectedFailure), scenario))
        }

    [<Test>]
    member _.``shared CBI5 vectors revalidate or close the released member``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi5-authority-withdrawal-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI5 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = prepared ()
                let handler = CoolingHandler()
                let baselineConversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let conversation =
                    if scenario = "cbi5-05-retirement-failure" then
                        FailingRetirementConversation(baselineConversation)
                        :> IPortableProviderConversation
                    else
                        baselineConversation
                let! active =
                    ComponentAuthorityIntegration.activate
                        resolution
                        selected
                        { Occurrence = occurrence; Participant = participant }
                        (runtimeRequest (plan [ occurrence ]))
                        (admission ())
                        conversation
                let memberValue = active.Lifecycle.Value.Member.Value
                let mutable request = admission ()
                match scenario with
                | "cbi5-01-current"
                | "cbi5-05-retirement-failure" -> ()
                | "cbi5-02-revoked" ->
                    request <-
                        { request with
                            Evidence =
                                request.Evidence
                                |> List.map (fun evidence ->
                                    { evidence with
                                        State = AdmissionEvidenceState.Revoked }) }
                | "cbi5-03-expired" ->
                    request <-
                        { request with
                            EvaluationTime = (List.exactlyOne request.Evidence).ExpiresAt }
                | "cbi5-04-request-mismatch" ->
                    request <-
                        { request with
                            Authority =
                                [ { List.exactlyOne request.Authority with
                                      Capability = CapabilityId.create "capability.other" } ] }
                | other -> invalidArg (nameof scenario) (sprintf "unknown CBI5 vector %s" other)
                if scenario = "cbi5-05-retirement-failure" then
                    request <-
                        { request with
                            Evidence =
                                request.Evidence
                                |> List.map (fun evidence ->
                                    { evidence with
                                        State = AdmissionEvidenceState.Revoked }) }
                let! result =
                    ComponentAuthorityRevalidation.revalidate
                        active
                        request
                        (sprintf "authority revalidation %s" scenario)
                let! afterWithdrawal =
                    if result.Kind = ComponentAuthorityRevalidationKind.Continued then
                        Task.FromResult None
                    else
                        task {
                            let! attempted =
                                memberValue.Invoke(
                                    CoolingFixture.setEnabled,
                                    CoolingFixture.commandV1,
                                    CoolingFixture.authorizedCommand "primary" true,
                                    PortableConstraint.AllOf
                                        [ PortableConstraint.Atom PortableTruth.Satisfied
                                          PortableConstraint.Atom PortableTruth.Satisfied ])
                            return
                                match attempted with
                                | Ok interaction -> Some interaction
                                | Error error -> failwithf "Expected a shaped gate refusal, got %A." error
                        }
                let kind =
                    match result.Kind with
                    | ComponentAuthorityRevalidationKind.Continued -> "continued"
                    | ComponentAuthorityRevalidationKind.Withdrawn -> "withdrawn"
                    | ComponentAuthorityRevalidationKind.RetirementFailed -> "retirement-failed"
                    | ComponentAuthorityRevalidationKind.ActivationUnavailable ->
                        "activation-unavailable"
                multiple (fun () ->
                    Assert.That(
                        kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo(if result.Kind = ComponentAuthorityRevalidationKind.Continued then
                                       "released"
                                   else
                                       "retired"),
                        scenario)
                    Assert.That(
                        result.Replacement.IsSome,
                        Is.EqualTo(result.Kind = ComponentAuthorityRevalidationKind.Withdrawn),
                        scenario)
                    Assert.That(
                        afterWithdrawal |> Option.bind _.Category,
                        Is.EqualTo(
                            if result.Kind = ComponentAuthorityRevalidationKind.Continued then
                                None
                            else
                                Some ProtocolCategory.StateViolation),
                        scenario)
                    Assert.That(handler.ProviderEffectCount, Is.Zero, scenario))
        }

    [<Test>]
    member _.``shared CBI6 vectors gate the participant set before provider contact``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi6-participant-admission-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI6 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = prepared ()
                let handler = CoolingHandler()
                let conversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let supervisorActor =
                    if scenario = "cbi6-08-shared-local-actor" then
                        providerLocalActor
                    else
                        supervisorLocalActor
                let baseline = participantSet occurrence supervisorActor
                let first = List.item 0 baseline
                let second = List.item 1 baseline
                let participants =
                    match scenario with
                    | "cbi6-01-two-participants"
                    | "cbi6-08-shared-local-actor" -> baseline
                    | "cbi6-02-second-participant-denied" -> [ first; revoked second ]
                    | "cbi6-03-repeated-participant" -> [ first; first ]
                    | "cbi6-04-shared-authority-identity" ->
                        [ first
                          withAuthority
                              second
                              [ { List.exactlyOne second.Request.Authority with
                                    Request = authorityId } ] ]
                    | "cbi6-05-repeated-grant-tuple" ->
                        let control = List.head first.Request.Authority
                        [ withAuthority
                              first
                              [ control
                                { control with
                                    Request =
                                        AuthorityRequestId.create "authority.cooling-control-again" } ]
                          second ]
                    | "cbi6-06-unlimited-grant" ->
                        [ first
                          withAuthority
                              second
                              [ { List.exactlyOne second.Request.Authority with Unlimited = true } ] ]
                    | "cbi6-07-empty-set" -> []
                    | "cbi6-09-foreign-occurrence" ->
                        [ first
                          { second with
                              Mapping =
                                { second.Mapping with
                                    Occurrence = OccurrenceId.create "occ.unselected" } } ]
                    | other -> invalidArg (nameof scenario) (sprintf "unknown CBI6 vector %s" other)
                let! result =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        participants
                        (runtimeRequest (plan [ occurrence ]))
                        conversation
                let expectedFailure: string | null =
                    let value = vector.GetProperty("expectedFailureKind")
                    if value.ValueKind = JsonValueKind.Null then null else value.GetString()
                let expectedCode: string | null =
                    let value = vector.GetProperty("expectedCode")
                    if value.ValueKind = JsonValueKind.Null then null else value.GetString()
                let actualFailure: string | null =
                    match result.Failure with
                    | None -> null
                    | Some failure -> participantFailureToken failure.Kind
                let actualCode: string | null =
                    match result.Failure with
                    | None -> null
                    | Some failure -> failure.Code
                multiple (fun () ->
                    Assert.That(
                        ComponentParticipantAdmission.isActive result,
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    Assert.That(actualFailure, Is.EqualTo expectedFailure, scenario)
                    Assert.That(actualCode, Is.EqualTo expectedCode, scenario)
                    Assert.That(
                        result.Admissions.Length,
                        Is.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Grants.Length,
                        Is.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Lifecycle.IsSome,
                        Is.EqualTo(ComponentParticipantAdmission.isActive result),
                        sprintf "%s: a refused participant set must not reach the provider." scenario)
                    Assert.That(handler.ProviderEffectCount, Is.Zero, scenario))
        }

    [<Test>]
    member _.``admitted participant set holds distinct local Actors and every grant``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let! result =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    (participantSet occurrence supervisorLocalActor)
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let holders = result.Grants |> List.map _.Holder |> List.distinct
            let memberValue = result.Lifecycle.Value.Member.Value
            multiple (fun () ->
                Assert.That(ComponentParticipantAdmission.isActive result, Is.True)
                Assert.That(
                    result.Admissions |> List.map _.Participant,
                    Is.EqualTo<ActorId> [ participant; supervisor ])
                Assert.That(
                    result.Grants |> List.map (fun grant -> AuthorityRequestId.value grant.Request),
                    Is.EqualTo<string>(
                        [ authorityId; reportAuthorityId; auditAuthorityId ]
                        |> List.map AuthorityRequestId.value
                        |> List.sortWith (fun left right -> String.CompareOrdinal(left, right))))
                Assert.That(holders.Length, Is.EqualTo 2)
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released")
                Assert.That(
                    (BindingPlan.authority (memberValue.TryPlan |> Option.get)).NoCapabilityTransfer,
                    Is.True))
        }

    [<Test>]
    member _.``participant set size cannot change any portable fact``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let set = participantSet occurrence supervisorLocalActor
            let! wide =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    set
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let resolution, selected, occurrence = prepared ()
            let! narrow =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    [ List.head set ]
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            multiple (fun () ->
                Assert.That(ComponentParticipantAdmission.isActive wide, Is.True)
                Assert.That(ComponentParticipantAdmission.isActive narrow, Is.True)
                Assert.That(wide.Grants.Length, Is.EqualTo 3)
                Assert.That(narrow.Grants.Length, Is.EqualTo 2)
                Assert.That(
                    portableFacts wide |> Map.toList,
                    Is.EqualTo<string * string>(portableFacts narrow |> Map.toList)))
        }

    [<Test>]
    member _.``shared CBI7 vectors revalidate or retire the shared member``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi7-participant-withdrawal-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI7 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = prepared ()
                let handler = CoolingHandler()
                let baselineConversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let conversation =
                    if scenario = "cbi7-08-retirement-failure" then
                        FailingRetirementConversation(baselineConversation)
                        :> IPortableProviderConversation
                    else
                        baselineConversation
                let participants = participantSet occurrence supervisorLocalActor
                let! active =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        participants
                        (runtimeRequest (plan [ occurrence ]))
                        conversation
                let memberValue = active.Lifecycle.Value.Member.Value
                let baseline = participants |> List.map _.Request
                let providerRequest = List.item 0 baseline
                let supervisorRequest = List.item 1 baseline
                let fresh =
                    match scenario with
                    | "cbi7-01-current" -> baseline
                    | "cbi7-02-one-revoked" -> [ providerRequest; revokedRequest supervisorRequest ]
                    | "cbi7-03-all-expired" -> baseline |> List.map expiredRequest
                    | "cbi7-04-tuple-mismatch" ->
                        [ providerRequest
                          { supervisorRequest with
                              Authority =
                                [ { List.exactlyOne supervisorRequest.Authority with
                                      Capability = CapabilityId.create "capability.other" } ] } ]
                    | "cbi7-05-grant-dropped" ->
                        [ { providerRequest with
                              Authority = [ List.head providerRequest.Authority ] }
                          supervisorRequest ]
                    | "cbi7-06-participant-removed" -> [ providerRequest ]
                    | "cbi7-07-participant-added" ->
                        baseline @ [ relabelled supervisorRequest observer ]
                    | "cbi7-08-retirement-failure" -> baseline |> List.map revokedRequest
                    | other -> invalidArg (nameof scenario) (sprintf "unknown CBI7 vector %s" other)
                let! result =
                    ComponentParticipantRevalidation.revalidate
                        active
                        fresh
                        (sprintf "set authority revalidation %s" scenario)
                let continued = result.Kind = ComponentParticipantRevalidationKind.Continued
                let! afterWithdrawal =
                    if continued then
                        Task.FromResult None
                    else
                        task {
                            let! attempted =
                                memberValue.Invoke(
                                    CoolingFixture.setEnabled,
                                    CoolingFixture.commandV1,
                                    CoolingFixture.authorizedCommand "primary" true,
                                    PortableConstraint.AllOf
                                        [ PortableConstraint.Atom PortableTruth.Satisfied
                                          PortableConstraint.Atom PortableTruth.Satisfied ])
                            return
                                match attempted with
                                | Ok interaction -> Some interaction
                                | Error error ->
                                    failwithf "Expected a shaped gate refusal, got %A." error
                        }
                multiple (fun () ->
                    Assert.That(
                        setRevalidationToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CurrentAuthority.Length,
                        Is.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Unrenewed.Length,
                        Is.EqualTo(vector.GetProperty("expectedUnrenewed").GetInt32()),
                        scenario)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo(if continued then "released" else "retired"),
                        scenario)
                    Assert.That(
                        result.Replacement.IsSome,
                        Is.EqualTo(result.Kind = ComponentParticipantRevalidationKind.Withdrawn),
                        scenario)
                    Assert.That(
                        afterWithdrawal |> Option.bind _.Category,
                        Is.EqualTo(
                            if continued then
                                None
                            else
                                Some ProtocolCategory.StateViolation),
                        scenario)
                    Assert.That(handler.ProviderEffectCount, Is.Zero, scenario))
        }

    [<Test>]
    member _.``one participant losing authority never narrows the set``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let participants = participantSet occurrence supervisorLocalActor
            let! active =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    participants
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let memberValue = active.Lifecycle.Value.Member.Value
            let baseline = participants |> List.map _.Request
            let! result =
                ComponentParticipantRevalidation.revalidate
                    active
                    [ List.item 0 baseline; revokedRequest (List.item 1 baseline) ]
                    "one participant lost authority"
            let unaffected =
                result.CurrentAuthority
                |> List.find (fun observation -> observation.Participant = participant)
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentParticipantRevalidationKind.Withdrawn)
                Assert.That(result.Unrenewed, Is.EqualTo<ActorId> [ supervisor ])
                // The unaffected participant is still admitted; that is what makes retirement a choice.
                Assert.That(
                    unaffected.Authority.Kind,
                    Is.EqualTo AuthorityAdmissionOutcomeKind.Admitted)
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "retired"))
        }

    [<Test>]
    member _.``shared CBI8 vectors extend or decline without disturbing the member``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi8-participant-extension-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI8 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = prepared ()
                let handler = CoolingHandler()
                let baselineConversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let conversation =
                    if scenario = "cbi8-11-retirement-failure" then
                        FailingRetirementConversation(baselineConversation)
                        :> IPortableProviderConversation
                    else
                        baselineConversation
                let observerActor =
                    if scenario = "cbi8-09-added-shared-local-actor" then
                        supervisorLocalActor
                    else
                        observerLocalActor
                let participants = participantSetWith occurrence supervisorLocalActor observerActor
                let! active =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        participants
                        (runtimeRequest (plan [ occurrence ]))
                        conversation
                let memberValue = active.Lifecycle.Value.Member.Value
                let baseline = participants |> List.map _.Request
                let providerRequest = List.item 0 baseline
                let supervisorRequest = List.item 1 baseline
                let observerBaseline =
                    observerRequest (setPolicyWith supervisorLocalActor observerActor)
                let intended =
                    match scenario with
                    | "cbi8-01-added"
                    | "cbi8-09-added-shared-local-actor" -> baseline @ [ observerBaseline ]
                    | "cbi8-02-participant-removed" -> [ providerRequest ]
                    | "cbi8-03-participant-substituted" -> [ providerRequest; observerBaseline ]
                    | "cbi8-04-unchanged" -> baseline
                    | "cbi8-05-added-identity-collision" ->
                        baseline
                        @ [ { observerBaseline with
                                Authority =
                                  [ { List.exactlyOne observerBaseline.Authority with
                                        Request = authorityId } ] } ]
                    | "cbi8-06-added-unlimited-grant" ->
                        baseline
                        @ [ { observerBaseline with
                                Authority =
                                  [ { List.exactlyOne observerBaseline.Authority with
                                        Unlimited = true } ] } ]
                    | "cbi8-07-retained-identity-drift" ->
                        [ providerRequest
                          { supervisorRequest with
                              Authority =
                                [ { List.exactlyOne supervisorRequest.Authority with
                                      Capability = CapabilityId.create "capability.other" } ] }
                          observerBaseline ]
                    | "cbi8-08-added-participant-denied" ->
                        baseline @ [ revokedRequest observerBaseline ]
                    | "cbi8-10-retained-participant-revoked"
                    | "cbi8-11-retirement-failure" ->
                        [ providerRequest
                          revokedRequest supervisorRequest
                          observerBaseline ]
                    | other -> invalidArg (nameof scenario) (sprintf "unknown CBI8 vector %s" other)
                let! result =
                    ComponentParticipantExtension.extend
                        active
                        intended
                        (sprintf "set extension %s" scenario)
                let released = vector.GetProperty("expectedReleased").GetBoolean()
                multiple (fun () ->
                    Assert.That(
                        extensionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CurrentAuthority.Length,
                        Is.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.InForce
                        |> Option.map (fun inForce -> inForce.Admissions.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                        scenario)
                    Assert.That(
                        result.InForce
                        |> Option.map (fun inForce -> inForce.Grants.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedInForceGrants").GetInt32()),
                        scenario)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo(if released then "released" else "retired"),
                        scenario)
                    // A set is in force exactly while the member is released.
                    Assert.That(result.InForce.IsSome, Is.EqualTo released, scenario)
                    Assert.That(handler.ProviderEffectCount, Is.Zero, scenario))
        }

    [<Test>]
    member _.``an extended set is revalidated as one set``() =
        task {
            let resolution, selected, occurrence = prepared ()
            let participants = participantSet occurrence supervisorLocalActor
            let! active =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    participants
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let memberValue = active.Lifecycle.Value.Member.Value
            let intended =
                (participants |> List.map _.Request)
                @ [ observerRequest (setPolicy supervisorLocalActor) ]
            let! extension =
                ComponentParticipantExtension.extend active intended "extend with an observer"
            let extended = extension.InForce.Value
            let! revalidated =
                ComponentParticipantRevalidation.revalidate
                    extended
                    intended
                    "extended set revalidation"
            multiple (fun () ->
                Assert.That(extension.Kind, Is.EqualTo ComponentParticipantExtensionKind.Extended)
                Assert.That(
                    revalidated.Kind,
                    Is.EqualTo ComponentParticipantRevalidationKind.Continued)
                Assert.That(revalidated.CurrentAuthority.Length, Is.EqualTo 3)
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released"))
        }

    [<Test>]
    member _.``shared CBI9 vectors revise the set only while the declaration stays covered``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi9-dependency-revision-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI9 vector identity must be a string"
                    | value -> value
                let declared =
                    if scenario = "cbi9-07-declaration-empty" then [] else declaredAuthority
                let resolution, selected, occurrence = preparedWith declared
                let handler = CoolingHandler()
                let baselineConversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let conversation =
                    if scenario = "cbi9-13-retirement-failure" then
                        FailingRetirementConversation(baselineConversation)
                        :> IPortableProviderConversation
                    else
                        baselineConversation
                let deputyActor =
                    if scenario = "cbi9-11-added-shared-local-actor" then
                        observerLocalActor
                    else
                        deputyLocalActor
                let policy = setPolicyFor supervisorLocalActor observerLocalActor deputyActor
                let participants = participantTrio occurrence policy
                let! active =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        participants
                        (runtimeRequest (plan [ occurrence ]))
                        conversation
                let memberValue = active.Lifecycle.Value.Member.Value
                let providerRequest = (List.item 0 participants).Request
                let supervisorRequest = (List.item 1 participants).Request
                let observerBaseline = (List.item 2 participants).Request
                let deputyBaseline = deputyRequest policy
                let declaration =
                    match scenario with
                    | "cbi9-06-declaration-mismatch" ->
                        { Definition = selected.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = authorityScope }
                              { DeclaredAuthority = "cooling.other"
                                Capability = auditCapability
                                Target = authorityTarget
                                Operation = auditOperation
                                Scope = authorityScope } ] }
                    | "cbi9-07-declaration-empty" ->
                        { Definition = selected.Definition; Entries = [] }
                    | "cbi9-08-declaration-unsatisfied" ->
                        { Definition = selected.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = CapabilityId.create "capability.other"
                                Target = authorityTarget
                                Operation = operation
                                Scope = authorityScope }
                              { DeclaredAuthority = "cooling.audit"
                                Capability = auditCapability
                                Target = authorityTarget
                                Operation = auditOperation
                                Scope = authorityScope } ] }
                    | _ -> dependency selected.Definition
                let intended =
                    match scenario with
                    | "cbi9-01-drop-undepended" -> [ providerRequest; supervisorRequest ]
                    | "cbi9-02-drop-depended" -> [ providerRequest; observerBaseline ]
                    | "cbi9-03-substitute-holder"
                    | "cbi9-11-added-shared-local-actor" ->
                        [ providerRequest; observerBaseline; deputyBaseline ]
                    | "cbi9-04-unchanged" ->
                        [ providerRequest; supervisorRequest; observerBaseline ]
                    | "cbi9-05-empty" -> []
                    | "cbi9-06-declaration-mismatch"
                    | "cbi9-07-declaration-empty"
                    | "cbi9-08-declaration-unsatisfied" -> [ providerRequest; supervisorRequest ]
                    | "cbi9-09-retained-identity-drift" ->
                        [ providerRequest
                          { supervisorRequest with
                              Authority =
                                [ { List.exactlyOne supervisorRequest.Authority with
                                      Capability = CapabilityId.create "capability.other" } ] } ]
                    | "cbi9-10-added-participant-denied" ->
                        [ providerRequest; observerBaseline; revokedRequest deputyBaseline ]
                    | "cbi9-12-retained-participant-revoked"
                    | "cbi9-13-retirement-failure" ->
                        [ providerRequest; revokedRequest supervisorRequest ]
                    | other -> invalidArg (nameof scenario) (sprintf "unknown CBI9 vector %s" other)
                let! result =
                    ComponentParticipantRevision.revise
                        resolution
                        selected
                        active
                        declaration
                        intended
                        (sprintf "set revision %s" scenario)
                let released = vector.GetProperty("expectedReleased").GetBoolean()
                multiple (fun () ->
                    Assert.That(
                        revisionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CurrentAuthority.Length,
                        Is.EqualTo(vector.GetProperty("expectedParticipantsEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.InForce
                        |> Option.map (fun inForce -> inForce.Admissions.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                        scenario)
                    Assert.That(
                        result.InForce
                        |> Option.map (fun inForce -> inForce.Grants.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedInForceGrants").GetInt32()),
                        scenario)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo(if released then "released" else "retired"),
                        scenario)
                    // A set is in force exactly while the member is released.
                    Assert.That(result.InForce.IsSome, Is.EqualTo released, scenario)
                    Assert.That(handler.ProviderEffectCount, Is.Zero, scenario))
        }

    [<Test>]
    member _.``shared CBI12 vectors open ordinary interaction for every member or none``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi12-group-activation-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI12 vector identity must be a string"
                    | value -> value
                let resolution = pairRequest () |> FakeGenerationResolver.resolve
                let providerSets =
                    match resolution with
                    | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                    | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                let positionFor requirement =
                    providerSets
                    |> List.find (fun item -> item.Requirement = requirement)
                    |> fun item -> List.exactlyOne item.Members
                let handlers = [ CoolingHandler(); CoolingHandler() ]
                let substituted =
                    { CoolingFixture.contract with
                        Provider = expectProvider "brontide.fake.substituted" }
                let secondContract =
                    if scenario = "cbi12-02-second-member-refused" then
                        substituted
                    else
                        CoolingFixture.contract
                let conversationFor document handler =
                    PortableDirectConversation(
                        PortableProviderEndpoint(document, handler, Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let primary =
                    { Selection =
                        { selection (positionFor requirementId) with
                            HostEndpoint = "group-host-primary" }
                      Conversation =
                        conversationFor CoolingFixture.contract (List.item 0 handlers) }
                let secondarySelection =
                    { selection (positionFor secondaryRequirementId) with
                        Requirement =
                            if scenario = "cbi12-03-preparation-refused" then
                                RequirementId.create "req.absent"
                            else
                                secondaryRequirementId
                        HostEndpoint = "group-host-secondary" }
                let secondary =
                    { Selection = secondarySelection
                      Conversation = conversationFor secondContract (List.item 1 handlers) }
                let members = [ primary; secondary ]
                let occurrences = members |> List.map _.Selection.Occurrence
                let planValue =
                    match scenario with
                    | "cbi12-04-unselected-member" ->
                        plan (occurrences @ [ OccurrenceId.create "occ.extra" ])
                    | "cbi12-05-protocol-group" -> protocolPlan occurrences
                    | _ -> plan occurrences
                let runtime =
                    if scenario = "cbi12-06-runtime-refused" then
                        { runtimeRequest planValue with
                            Release =
                                { Release = ReleaseId.create "release.integration"
                                  FailureMoment = ReleaseFailureMoment.BeforeCutover } }
                    else
                        runtimeRequest planValue
                let! result = ComponentGroupLifecycle.activate resolution members runtime
                let expectedFailure =
                    let value = vector.GetProperty("expectedFailureKind")
                    if value.ValueKind = JsonValueKind.Null then None else Some(value.GetString())
                let actualFailure =
                    result.Failure |> Option.map (fun failure -> groupFailureToken failure.Kind)
                let expectedCode =
                    let value = vector.GetProperty("expectedCode")
                    if value.ValueKind = JsonValueKind.Null then None else Some(value.GetString())
                let expectedRuntimeActive =
                    let value = vector.GetProperty("expectedRuntimeActive")
                    if value.ValueKind = JsonValueKind.Null then
                        None
                    else
                        Some(value.GetBoolean())
                let actualRuntimeActive =
                    result.Runtime
                    |> Option.map (fun outcome ->
                        outcome.Kind = ActivationRuntimeOutcomeKind.Active)
                let released = result.Members |> List.filter _.Member.IsReleased
                let retired =
                    result.Members
                    |> List.filter (fun outcome ->
                        CompositionStage.token outcome.Member.Stage = "retired")
                multiple (fun () ->
                    Assert.That(
                        ComponentGroupLifecycle.isActive result,
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    Assert.That(actualFailure, Is.EqualTo expectedFailure, scenario)
                    Assert.That(
                        result.Failure |> Option.map _.Code,
                        Is.EqualTo expectedCode,
                        scenario)
                    Assert.That(
                        result.Members.Length,
                        Is.EqualTo(vector.GetProperty("expectedMembers").GetInt32()),
                        scenario)
                    Assert.That(
                        released.Length,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        retired.Length,
                        Is.EqualTo(vector.GetProperty("expectedRetired").GetInt32()),
                        scenario)
                    Assert.That(actualRuntimeActive, Is.EqualTo expectedRuntimeActive, scenario)
                    // Either every member is released or none is.
                    Assert.That(
                        released.Length = result.Members.Length || released.IsEmpty,
                        Is.True,
                        scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo 0L,
                        scenario))
        }

    [<Test>]
    member _.``shared CBI13 vectors admit every member before any provider is reached``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi13-group-authority-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI13 vector identity must be a string"
                    | value -> value
                let resolution = pairRequest () |> FakeGenerationResolver.resolve
                let providerSets =
                    match resolution with
                    | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                    | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                let positionFor requirement =
                    providerSets
                    |> List.find (fun item -> item.Requirement = requirement)
                    |> fun item -> List.exactlyOne item.Members
                let handlers = [ CoolingHandler(); CoolingHandler() ]
                let conversationFor document handler =
                    PortableDirectConversation(
                        PortableProviderEndpoint(document, handler, Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let secondContract =
                    if scenario = "cbi13-07-activation-refused-after-admission" then
                        { CoolingFixture.contract with
                            Provider = expectProvider "brontide.fake.substituted" }
                    else
                        CoolingFixture.contract
                let firstMember =
                    { Selection =
                        { selection (positionFor requirementId) with
                            HostEndpoint = "authority-host-primary" }
                      Conversation =
                        conversationFor CoolingFixture.contract (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "authority-host-secondary" }
                      Conversation = conversationFor secondContract (List.item 1 handlers) }
                // The second member's participant differs by default, so each member admits its own
                // party.
                let sharesParticipant =
                    scenario = "cbi13-02-shared-participant-consistent"
                    || scenario = "cbi13-05-participant-two-local-actors"
                let secondaryActor = if sharesParticipant then participant else supervisor
                let secondaryLocalActor =
                    match scenario with
                    | "cbi13-05-participant-two-local-actors" -> supervisorLocalActor
                    | "cbi13-06-participants-one-local-actor" -> providerLocalActor
                    | _ -> if sharesParticipant then providerLocalActor else supervisorLocalActor
                // Only the second member's policy varies, so the activation-level mapping rules are
                // what the vectors exercise rather than any one member's admission.
                let policy =
                    if scenario = "cbi13-05-participant-two-local-actors" then
                        groupPolicy supervisorLocalActor supervisorLocalActor
                    else
                        groupPolicy providerLocalActor secondaryLocalActor
                let secondaryAuthorityId =
                    if scenario = "cbi13-04-authority-identity-shared" then
                        authorityId
                    else
                        reportAuthorityId
                let firstParticipant =
                    { Mapping =
                        { Occurrence = firstMember.Selection.Occurrence
                          Participant = participant }
                      Request = providerAuthority (groupPolicy providerLocalActor supervisorLocalActor) authorityId }
                let secondRequest =
                    if secondaryActor = participant then
                        let relationship = RelationshipRequestId.create "relationship.group-secondary"
                        { providerAuthority policy secondaryAuthorityId with
                            Request = AdmissionRequestId.create "admission.group-secondary"
                            Relationships =
                                [ { Request = relationship
                                    ProposedActor = participant
                                    Kind = ActorRelationshipKind.ComponentParticipant
                                    Evidence = [ authorityEvidence ] } ]
                            Authority =
                                [ { Request = secondaryAuthorityId
                                    Relationship = relationship
                                    Capability = reportCapability
                                    Target = authorityTarget
                                    Operation = reportOperation
                                    Scope = authorityScope
                                    Unlimited = false } ] }
                    else
                        supervisorAuthority
                            policy
                            secondaryAuthorityId
                            (scenario = "cbi13-03-second-member-denied")
                let secondParticipant =
                    { Mapping =
                        { Occurrence = secondMember.Selection.Occurrence
                          Participant = secondaryActor }
                      Request = secondRequest }
                let occurrences =
                    [ firstMember.Selection.Occurrence; secondMember.Selection.Occurrence ]
                let! result =
                    ComponentGroupAuthority.activate
                        resolution
                        [ { Member = firstMember; Participants = [ firstParticipant ] }
                          { Member = secondMember; Participants = [ secondParticipant ] } ]
                        (runtimeRequest (plan occurrences))
                let expectedFailure =
                    let value = vector.GetProperty("expectedFailureKind")
                    if value.ValueKind = JsonValueKind.Null then None else Some(value.GetString())
                let actualFailure =
                    result.Failure |> Option.map (fun failure -> groupAuthorityToken failure.Kind)
                let released =
                    result.Lifecycle
                    |> Option.map (fun lifecycle ->
                        lifecycle.Members |> List.filter _.Member.IsReleased |> List.length)
                    |> Option.defaultValue 0
                multiple (fun () ->
                    Assert.That(
                        ComponentGroupAuthority.isActive result,
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    Assert.That(actualFailure, Is.EqualTo expectedFailure, scenario)
                    Assert.That(
                        result.Admissions.Length,
                        Is.EqualTo(vector.GetProperty("expectedMembersAdmitted").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Grants.Length,
                        Is.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo(int64 (vector.GetProperty("expectedProviderEffects").GetInt32())),
                        scenario)
                    // The authority barrier is earlier than the release barrier: an authority
                    // refusal never reaches a provider at all.
                    match result.Failure with
                    | Some failure when
                        failure.Kind <> ComponentGroupAuthorityFailureKind.ActivationRefused
                        ->
                        Assert.That(result.Lifecycle, Is.EqualTo None, scenario)
                    | _ -> ())
        }

    [<Test>]
    member _.``shared CBI14 vectors retire every member or none``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi14-group-withdrawal-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI14 vector identity must be a string"
                    | value -> value
                let failCleanup = scenario = "cbi14-06-retirement-failure"
                let resolution = pairRequest () |> FakeGenerationResolver.resolve
                let providerSets =
                    match resolution with
                    | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                    | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                let positionFor requirement =
                    providerSets
                    |> List.find (fun item -> item.Requirement = requirement)
                    |> fun item -> List.exactlyOne item.Members
                let handlers = [ CoolingHandler(); CoolingHandler() ]
                let baseConversation document handler =
                    PortableDirectConversation(
                        PortableProviderEndpoint(document, handler, Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let secondConversation =
                    let inner = baseConversation CoolingFixture.contract (List.item 1 handlers)
                    if failCleanup then
                        FailingRetirementConversation inner :> IPortableProviderConversation
                    else
                        inner
                let firstMember =
                    { Selection =
                        { selection (positionFor requirementId) with
                            HostEndpoint = "withdrawal-host-primary" }
                      Conversation =
                        baseConversation CoolingFixture.contract (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "withdrawal-host-secondary" }
                      Conversation = secondConversation }
                let participants =
                    [ providerAuthority (groupPolicy providerLocalActor supervisorLocalActor) authorityId
                      supervisorAuthority
                          (groupPolicy providerLocalActor supervisorLocalActor)
                          reportAuthorityId
                          false ]
                let occurrences =
                    [ firstMember.Selection.Occurrence; secondMember.Selection.Occurrence ]
                let! active =
                    ComponentGroupAuthority.activate
                        resolution
                        [ { Member = firstMember
                            Participants =
                              [ { Mapping =
                                    { Occurrence = firstMember.Selection.Occurrence
                                      Participant = participant }
                                  Request = List.item 0 participants } ] }
                          { Member = secondMember
                            Participants =
                              [ { Mapping =
                                    { Occurrence = secondMember.Selection.Occurrence
                                      Participant = supervisor }
                                  Request = List.item 1 participants } ] } ]
                        (runtimeRequest (plan occurrences))
                let first = (List.item 0 active.Admissions).Occurrence
                let second = (List.item 1 active.Admissions).Occurrence
                let lapse =
                    scenario = "cbi14-02-one-member-lapsed"
                    || scenario = "cbi14-06-retirement-failure"
                    || scenario = "cbi14-03-both-members-lapsed"
                let firstRequest =
                    if scenario = "cbi14-03-both-members-lapsed" then
                        revokedRequest (List.item 0 participants)
                    else
                        List.item 0 participants
                let secondRequest =
                    if lapse then
                        revokedRequest (List.item 1 participants)
                    else
                        List.item 1 participants
                let requests =
                    match scenario with
                    | "cbi14-04-member-set-changed" ->
                        [ { Occurrence = first; Requests = [ firstRequest ] } ]
                    | "cbi14-05-participant-drift" ->
                        [ { Occurrence = first; Requests = [ firstRequest ] }
                          { Occurrence = second
                            Requests =
                              [ { List.item 1 participants with
                                    Authority =
                                      [ { List.exactlyOne (List.item 1 participants).Authority with
                                            Capability = CapabilityId.create "capability.other" } ] } ] } ]
                    | _ ->
                        [ { Occurrence = first; Requests = [ firstRequest ] }
                          { Occurrence = second; Requests = [ secondRequest ] } ]
                let! result =
                    ComponentGroupRevalidation.revalidate
                        active
                        requests
                        (sprintf "group revalidation %s" scenario)
                let lifecycle = active.Lifecycle.Value
                let released = lifecycle.Members |> List.filter _.Member.IsReleased |> List.length
                multiple (fun () ->
                    Assert.That(
                        groupRevalidationToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Members.Length,
                        Is.EqualTo(vector.GetProperty("expectedMembersEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Lapsed.Length,
                        Is.EqualTo(vector.GetProperty("expectedLapsed").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Replacements.Length,
                        Is.EqualTo(vector.GetProperty("expectedReplacements").GetInt32()),
                        scenario)
                    // The activation shares a restart scope, so it shares a fate.
                    Assert.That(
                        released = lifecycle.Members.Length || released = 0,
                        Is.True,
                        scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo 0L,
                        scenario))
        }

    [<Test>]
    member _.``a failed member leaves no other member reachable``() =
        task {
            let resolution = pairRequest () |> FakeGenerationResolver.resolve
            let providerSets =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let positionFor requirement =
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> List.exactlyOne item.Members
            let handlers = [ CoolingHandler(); CoolingHandler() ]
            let conversationFor document handler =
                PortableDirectConversation(
                    PortableProviderEndpoint(document, handler, Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let members =
                [ { Selection =
                      { selection (positionFor requirementId) with
                          HostEndpoint = "group-host-primary" }
                    Conversation = conversationFor CoolingFixture.contract (List.item 0 handlers) }
                  { Selection =
                      { selection (positionFor secondaryRequirementId) with
                          Requirement = secondaryRequirementId
                          HostEndpoint = "group-host-secondary" }
                    Conversation =
                      conversationFor
                          { CoolingFixture.contract with
                              Provider = expectProvider "brontide.fake.substituted" }
                          (List.item 1 handlers) } ]
            let occurrences = members |> List.map _.Selection.Occurrence
            let! result =
                ComponentGroupLifecycle.activate resolution members (runtimeRequest (plan occurrences))
            let survivor = (List.item 0 result.Members).Member
            let! attempted =
                survivor.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            let interaction =
                match attempted with
                | Ok value -> value
                | Error error -> failwithf "Expected a shaped gate refusal, got %A." error
            multiple (fun () ->
                Assert.That(ComponentGroupLifecycle.isActive result, Is.False)
                Assert.That(
                    result.Failure.Value.Member,
                    Is.EqualTo(Some (List.item 1 result.Members).Occurrence))
                Assert.That(CompositionStage.token survivor.Stage, Is.EqualTo "retired")
                Assert.That(interaction.Category, Is.EqualTo(Some ProtocolCategory.StateViolation)))
        }

    [<Test>]
    member _.``a substitute satisfies the declaration a different holder used to satisfy``() =
        task {
            let resolution, selected, occurrence = preparedWith declaredAuthority
            let policy = setPolicy supervisorLocalActor
            let participants = participantTrio occurrence policy
            let! active =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    participants
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let memberValue = active.Lifecycle.Value.Member.Value
            let intended =
                [ (List.item 0 participants).Request
                  (List.item 2 participants).Request
                  deputyRequest policy ]
            let! result =
                ComponentParticipantRevision.revise
                    resolution
                    selected
                    active
                    (dependency selected.Definition)
                    intended
                    "substitute the audit holder"
            let inForce = result.InForce.Value
            let auditGrants =
                inForce.Grants
                |> List.filter (fun grant ->
                    grant.Capability = auditCapability && grant.Operation = auditOperation)
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentParticipantRevisionKind.Revised)
                // The participant that used to satisfy the declared audit dependency is gone.
                Assert.That(
                    inForce.Admissions |> List.exists (fun item -> item.Participant = supervisor),
                    Is.False)
                Assert.That(auditGrants.Length, Is.EqualTo 1)
                // A different receiving-domain Actor now satisfies it.
                Assert.That((List.exactlyOne auditGrants).Holder, Is.EqualTo deputyLocalActor)
                Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released"))
        }

    [<Test>]
    member _.``shared CBI10 vectors verify the declaration against what the member did``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi10-observed-interaction-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI10 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = preparedWith declaredAuthority
                let handler = CoolingHandler()
                let baselineConversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let conversation =
                    if scenario = "cbi10-07-retirement-failure" then
                        FailingRetirementConversation(baselineConversation)
                        :> IPortableProviderConversation
                    else
                        baselineConversation
                let policy = setPolicy supervisorLocalActor
                let participants =
                    participantSet occurrence supervisorLocalActor
                    |> List.map (fun entry ->
                        { entry with
                            Request = { entry.Request with Policy = policy } })
                let runtime = runtimeRequest (plan [ occurrence ])
                let! active =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        participants
                        runtime
                        conversation
                let memberValue = active.Lifecycle.Value.Member.Value
                // The observations are real: the host invokes the released member and records what
                // came back.
                let! observations =
                    if scenario = "cbi10-02-nothing-observed" then
                        Task.FromResult []
                    else
                        task {
                            let constraintValue =
                                if scenario = "cbi10-03-denied-before-any-frame" then
                                    PortableConstraint.Atom PortableTruth.Unsatisfied
                                else
                                    PortableConstraint.Atom PortableTruth.Satisfied
                            let! attempted =
                                memberValue.Invoke(
                                    CoolingFixture.setEnabled,
                                    CoolingFixture.commandV1,
                                    CoolingFixture.authorizedCommand "primary" true,
                                    constraintValue)
                            return
                                match attempted with
                                | Ok interaction ->
                                    [ { Operation = CoolingFixture.setEnabled
                                        Result = interaction } ]
                                | Error error ->
                                    failwithf "Expected an observable interaction, got %A." error
                        }
                let declaration =
                    match scenario with
                    | "cbi10-06-ungranted-authority" ->
                        { Definition = selected.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = CapabilityScopeId.create "scope.other" }
                              { DeclaredAuthority = "cooling.audit"
                                Capability = auditCapability
                                Target = authorityTarget
                                Operation = auditOperation
                                Scope = authorityScope } ] }
                    | "cbi10-08-declaration-mismatch" ->
                        { Definition = selected.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = authorityScope }
                              { DeclaredAuthority = "cooling.other"
                                Capability = auditCapability
                                Target = authorityTarget
                                Operation = auditOperation
                                Scope = authorityScope } ] }
                    | _ -> dependency selected.Definition
                let attribution =
                    match scenario with
                    | "cbi10-04-undeclared-authority"
                    | "cbi10-07-retirement-failure" ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.other" } ]
                    | "cbi10-05-unmapped-operation" -> []
                    | "cbi10-09-mapping-not-distinct" ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.control" }
                          { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.audit" } ]
                    | _ ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.control" } ]
                let! verdict =
                    ComponentInteractionVerification.verify
                        resolution
                        selected
                        active
                        declaration
                        attribution
                        observations
                        runtime
                        (sprintf "observed interaction %s" scenario)
                let released = vector.GetProperty("expectedReleased").GetBoolean()
                let expectedRuntimeActive =
                    let value = vector.GetProperty("expectedRuntimeActive")
                    if value.ValueKind = JsonValueKind.Null then
                        None
                    else
                        Some(value.GetBoolean())
                let actualRuntimeActive =
                    verdict.Runtime
                    |> Option.map (fun outcome ->
                        outcome.Kind = ActivationRuntimeOutcomeKind.Active)
                multiple (fun () ->
                    Assert.That(
                        verdictToken verdict.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        verdict.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        verdict.Exercises.Length,
                        Is.EqualTo(vector.GetProperty("expectedExercises").GetInt32()),
                        scenario)
                    Assert.That(
                        verdict.Unexercised.Length,
                        Is.EqualTo(vector.GetProperty("expectedUnexercised").GetInt32()),
                        scenario)
                    Assert.That(
                        verdict.Uncovered.Length,
                        Is.EqualTo(vector.GetProperty("expectedUncovered").GetInt32()),
                        scenario)
                    Assert.That(actualRuntimeActive, Is.EqualTo expectedRuntimeActive, scenario)
                    Assert.That(
                        CompositionStage.token memberValue.Stage,
                        Is.EqualTo(if released then "released" else "retired"),
                        scenario)
                    Assert.That(
                        handler.ProviderEffectCount,
                        Is.EqualTo(vector.GetProperty("expectedProviderEffects").GetInt32()),
                        scenario)
                    // The runtime accepts the projection exactly when the verification is consistent.
                    match actualRuntimeActive with
                    | Some runtimeActive ->
                        Assert.That(
                            runtimeActive,
                            Is.EqualTo(
                                verdict.Kind = ComponentInteractionVerdictKind.Consistent),
                            scenario)
                    | None -> ())
        }

    [<Test>]
    member _.``shared CBI11 vectors narrow only when a successor declares less``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi11-declaration-succession-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI11 vector identity must be a string"
                    | value -> value
                let resolution, selected, occurrence = preparedWith declaredAuthority
                let! active =
                    ComponentParticipantAdmission.activate
                        resolution
                        selected
                        (participantSet occurrence supervisorLocalActor)
                        (runtimeRequest (plan [ occurrence ]))
                        (directCooling CoolingFixture.contract)
                let memberValue = active.Lifecycle.Value.Member.Value
                let! attempted =
                    memberValue.Invoke(
                        CoolingFixture.setEnabled,
                        CoolingFixture.commandV1,
                        CoolingFixture.authorizedCommand "primary" true,
                        PortableConstraint.Atom PortableTruth.Satisfied)
                let observations =
                    match attempted with
                    | Ok interaction ->
                        [ { Operation = CoolingFixture.setEnabled
                            Result = interaction } ]
                    | Error error -> failwithf "Expected an observable interaction, got %A." error
                let successor =
                    match scenario with
                    | "cbi11-06-position-mismatch" ->
                        requestFor
                            (Cardinality.parse "1..1")
                            [ "cooling.control" ]
                            (Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.create
                                "scope.other")
                        |> FakeGenerationResolver.resolve
                    | "cbi11-07-successor-declares-nothing" ->
                        requestWith (Cardinality.parse "1..1") [] |> FakeGenerationResolver.resolve
                    | "cbi11-03-unchanged"
                    | "cbi11-08-successor-mapping-mismatch" ->
                        requestWith (Cardinality.parse "1..1") declaredAuthority
                        |> FakeGenerationResolver.resolve
                    | "cbi11-04-wider" ->
                        requestWith
                            (Cardinality.parse "1..1")
                            [ "cooling.control"; "cooling.audit"; "cooling.extra" ]
                        |> FakeGenerationResolver.resolve
                    | _ ->
                        requestWith (Cardinality.parse "1..1") [ "cooling.control" ]
                        |> FakeGenerationResolver.resolve
                let successorDeclaration =
                    match scenario with
                    | "cbi11-03-unchanged" -> dependency selected.Definition
                    | "cbi11-04-wider" ->
                        { Definition = selected.Definition
                          Entries =
                            (dependency selected.Definition).Entries
                            @ [ { DeclaredAuthority = "cooling.extra"
                                  Capability = reportCapability
                                  Target = authorityTarget
                                  Operation = reportOperation
                                  Scope = authorityScope } ] }
                    | "cbi11-05-tuple-changed" ->
                        { Definition = selected.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = CapabilityScopeId.create "scope.other" } ] }
                    | "cbi11-07-successor-declares-nothing" ->
                        { Definition = selected.Definition; Entries = [] }
                    | "cbi11-08-successor-mapping-mismatch" ->
                        controlOnlyDependency selected.Definition
                    | _ -> controlOnlyDependency selected.Definition
                let attribution =
                    match scenario with
                    | "cbi11-02-use-vetoed" ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.audit" } ]
                    | "cbi11-09-ambiguous-attribution" ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.control" }
                          { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.audit" } ]
                    | _ ->
                        [ { Operation = CoolingFixture.setEnabled
                            DeclaredAuthority = "cooling.control" } ]
                let result =
                    ComponentDeclarationSuccession.succeed
                        resolution
                        successor
                        selected
                        active
                        (dependency selected.Definition)
                        successorDeclaration
                        attribution
                        observations
                multiple (fun () ->
                    Assert.That(
                        successionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Dropped.Length,
                        Is.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Vetoed.Length,
                        Is.EqualTo(vector.GetProperty("expectedVetoed").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Declaration.Value.Entries.Length,
                        Is.EqualTo(vector.GetProperty("expectedDeclaredInForce").GetInt32()),
                        scenario)
                    // CBI11 has no retirement path.
                    Assert.That(CompositionStage.token memberValue.Stage, Is.EqualTo "released", scenario)
                    Assert.That(
                        vector.GetProperty("expectedReleased").GetBoolean(),
                        Is.True,
                        scenario))
        }

    [<Test>]
    member _.``a narrowed declaration lets CBI9 release the participant it kept``() =
        task {
            let resolution, selected, occurrence = preparedWith declaredAuthority
            let participants = participantSet occurrence supervisorLocalActor
            let! active =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    participants
                    (runtimeRequest (plan [ occurrence ]))
                    (directCooling CoolingFixture.contract)
            let providerRequest = (List.item 0 participants).Request
            let! before =
                ComponentParticipantRevision.revise
                    resolution
                    selected
                    active
                    (dependency selected.Definition)
                    [ providerRequest ]
                    "drop the audit holder before succession"
            let successor =
                requestWith (Cardinality.parse "1..1") [ "cooling.control" ]
                |> FakeGenerationResolver.resolve
            let narrowed =
                ComponentDeclarationSuccession.succeed
                    resolution
                    successor
                    selected
                    active
                    (dependency selected.Definition)
                    (controlOnlyDependency selected.Definition)
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.control" } ]
                    []
            let! after =
                ComponentParticipantRevision.revise
                    successor
                    selected
                    active
                    narrowed.Declaration.Value
                    [ providerRequest ]
                    "drop the audit holder after succession"
            multiple (fun () ->
                Assert.That(before.Code, Is.EqualTo "dependency-not-covered")
                Assert.That(narrowed.Kind, Is.EqualTo ComponentDeclarationSuccessionKind.Narrowed)
                Assert.That(narrowed.Dropped, Is.EqualTo<string> [ "cooling.audit" ])
                Assert.That(after.Kind, Is.EqualTo ComponentParticipantRevisionKind.Revised)
                Assert.That(after.InForce.Value.Admissions.Length, Is.EqualTo 1))
        }

    [<Test>]
    member _.``undeclared use is condemned by the runtime rather than by the verifier``() =
        task {
            let resolution, selected, occurrence = preparedWith declaredAuthority
            let policy = setPolicy supervisorLocalActor
            let participants =
                participantSet occurrence supervisorLocalActor
                |> List.map (fun entry ->
                    { entry with
                        Request = { entry.Request with Policy = policy } })
            let runtime = runtimeRequest (plan [ occurrence ])
            let! active =
                ComponentParticipantAdmission.activate
                    resolution
                    selected
                    participants
                    runtime
                    (directCooling CoolingFixture.contract)
            let memberValue = active.Lifecycle.Value.Member.Value
            let! attempted =
                memberValue.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            let observation =
                match attempted with
                | Ok interaction ->
                    { Operation = CoolingFixture.setEnabled
                      Result = interaction }
                | Error error -> failwithf "Expected an observable interaction, got %A." error
            let! verdict =
                ComponentInteractionVerification.verify
                    resolution
                    selected
                    active
                    (dependency selected.Definition)
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.other" } ]
                    [ observation ]
                    runtime
                    "undeclared use"
            let exercise = List.exactlyOne verdict.Exercises
            multiple (fun () ->
                Assert.That(verdict.Kind, Is.EqualTo ComponentInteractionVerdictKind.UndeclaredUse)
                // CM4's own rule refuses a delivered exercise the authority check denied.
                Assert.That(
                    verdict.Runtime.Value.Kind,
                    Is.EqualTo ActivationRuntimeOutcomeKind.BindingObservationConflict)
                Assert.That(exercise.AuthorityAdmitted, Is.False)
                Assert.That(exercise.Delivery, Is.EqualTo BindingDeliveryResult.Delivered)
                Assert.That(verdict.Replacement.IsSome, Is.True))
        }

    [<Test>]
    member _.``refused CBI6 set cannot be revised``() =
        task {
            let resolution, selected, _ = preparedWith declaredAuthority
            let unavailable: ComponentParticipantAdmissionResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let! result =
                ComponentParticipantRevision.revise
                    resolution
                    selected
                    unavailable
                    (dependency selected.Definition)
                    []
                    "set revision unavailable"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentParticipantRevisionKind.ActivationUnavailable)
                Assert.That(result.InForce, Is.EqualTo None)
                Assert.That(result.CurrentAuthority, Is.Empty))
        }

    [<Test>]
    member _.``refused CBI6 set cannot be extended``() =
        task {
            let unavailable: ComponentParticipantAdmissionResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let _, _, occurrence = prepared ()
            let! result =
                ComponentParticipantExtension.extend
                    unavailable
                    (participantSet occurrence supervisorLocalActor |> List.map _.Request)
                    "set extension unavailable"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentParticipantExtensionKind.ActivationUnavailable)
                Assert.That(result.InForce, Is.EqualTo None)
                Assert.That(result.CurrentAuthority, Is.Empty))
        }

    [<Test>]
    member _.``refused CBI6 set cannot be revalidated as active``() =
        task {
            let unavailable: ComponentParticipantAdmissionResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let _, _, occurrence = prepared ()
            let! result =
                ComponentParticipantRevalidation.revalidate
                    unavailable
                    (participantSet occurrence supervisorLocalActor |> List.map _.Request)
                    "set authority unavailable"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentParticipantRevalidationKind.ActivationUnavailable)
                Assert.That(result.CurrentAuthority, Is.Empty)
                Assert.That(result.Unrenewed, Is.Empty)
                Assert.That(result.Replacement, Is.EqualTo None))
        }

    [<Test>]
    member _.``refused CBI3 result cannot be revalidated as active``() =
        task {
            let unavailable =
                { Authority = None
                  Lifecycle = None
                  Failure = None }
            let! result =
                ComponentAuthorityRevalidation.revalidate
                    unavailable
                    (admission ())
                    "authority unavailable"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentAuthorityRevalidationKind.ActivationUnavailable)
                Assert.That(result.CurrentAuthority, Is.EqualTo None)
                Assert.That(result.Replacement, Is.EqualTo None))
        }
