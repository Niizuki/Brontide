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

    let planFor generation restartScope occurrences =
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.integration"
              Generation = generation
              RestartScope = restartScope
              Members = occurrences |> List.map activationMember
              Edges = []
              Protocols = []
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "Expected a CM3 plan, got %A." outcome

    let plan occurrences =
        planFor
            (GenerationId.create "gen.lifecycle")
            (RestartScopeId.create "restart.lifecycle")
            occurrences

    let runtimeRequestFor planValue retained =
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

    let runtimeRequest planValue =
        runtimeRequestFor planValue (GenerationId.create "gen.retained")

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
    let pairRequestFor firstAuthority secondAuthority =
        let single = requestWith (Cardinality.parse "1..1") firstAuthority
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
                      Provides = [ { Contract = secondaryContractId; Version = version } ]
                      RequestedAuthority = secondAuthority } ]
            Candidates =
                [ candidate
                  { candidate with
                      Definition = secondaryProvider
                      Provides = [ { Contract = secondaryContractId; Version = version } ] } ] }

    /// A strongly connected group that declares no protocol: the members interact ordinarily, which
    /// is enough to make one component of the graph and nothing more.
    let cyclePlan (cycle: OccurrenceId list) (isolated: OccurrenceId list) =
        let members = cycle @ isolated |> List.map activationMember
        let edges =
            cycle
            |> List.mapi (fun index occurrence ->
                { Edge = ActivationEdgeId.create (sprintf "edge.cycle-%d" index)
                  From = occurrence
                  To = List.item ((index + 1) % cycle.Length) cycle
                  Kind = ActivationDependencyKind.OrdinaryInteraction
                  Contract = contractId
                  Version = version
                  // Ordinary traffic observed before Release is what CM3 refuses; this only declares
                  // that the members interact once both are serving.
                  ObservedBeforeRelease = false
                  Protocol = None
                  CrossingPort = None
                  AllowWiderRegionProposal = false })
        let groupRequest =
            { Request = ActivationGroupRequestId.create "group.integration"
              Generation = GenerationId.create "gen.lifecycle"
              RestartScope = RestartScopeId.create "restart.lifecycle"
              Members = members
              Edges = edges
              Protocols = []
              RegionCrossings = [] }
        match FakeActivationGroupPlanner.plan groupRequest with
        | Planned value -> value
        | outcome -> failwithf "CM3 refused the ordinary cycle: %A" outcome

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

    let pairRequest () = pairRequestFor [] []

    /// The receiving-domain policy, with the participant's own local Actor overridable.
    let groupPolicyFor participantActor supervisorActor observerActor : LocalAuthorityPolicy =
        let policy = setPolicyFor supervisorActor observerActor deputyLocalActor
        { policy with
            RelationshipRules =
                policy.RelationshipRules
                |> List.map (fun rule ->
                    if rule.ProposedActor = participant then
                        { rule with LocalActor = Some participantActor }
                    else
                        rule) }

    let groupPolicy participantActor supervisorActor =
        groupPolicyFor participantActor supervisorActor observerLocalActor

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

    let groupRevisionToken kind =
        match kind with
        | ComponentGroupRevisionKind.Revised -> "revised"
        | ComponentGroupRevisionKind.Declined -> "declined"
        | ComponentGroupRevisionKind.Withdrawn -> "withdrawn"
        | ComponentGroupRevisionKind.RetirementFailed -> "retirement-failed"
        | ComponentGroupRevisionKind.ActivationUnavailable -> "activation-unavailable"

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

    let groupVerificationToken kind =
        match kind with
        | ComponentGroupVerificationKind.Consistent -> "consistent"
        | ComponentGroupVerificationKind.UndeclaredUse -> "undeclared-use"
        | ComponentGroupVerificationKind.UngrantedUse -> "ungranted-use"
        | ComponentGroupVerificationKind.RetirementFailed -> "retirement-failed"
        | ComponentGroupVerificationKind.Declined -> "declined"
        | ComponentGroupVerificationKind.ActivationUnavailable -> "activation-unavailable"

    let groupVerificationResult scenario =
        task {
            let resolution =
                pairRequestFor [ "cooling.control" ] [ "cooling.audit" ]
                |> FakeGenerationResolver.resolve
            let providerSets =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let positionFor requirement =
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> List.exactlyOne item.Members
            let handlers = [ CoolingHandler(); CoolingHandler() ]
            let conversationFor handler =
                PortableDirectConversation(
                    PortableProviderEndpoint(
                        CoolingFixture.contract,
                        handler,
                        Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let firstMember =
                { Selection =
                    { selection (positionFor requirementId) with
                        HostEndpoint = "verification-host-primary" }
                  Conversation = conversationFor (List.item 0 handlers) }
            let secondMember =
                { Selection =
                    { selection (positionFor secondaryRequirementId) with
                        Requirement = secondaryRequirementId
                        HostEndpoint = "verification-host-secondary" }
                  Conversation =
                    let inner = conversationFor (List.item 1 handlers)
                    if scenario = "cbi16-08-retirement-failure" then
                        FailingRetirementConversation inner :> IPortableProviderConversation
                    else
                        inner }
            let runtime =
                runtimeRequest (
                    plan [ firstMember.Selection.Occurrence; secondMember.Selection.Occurrence ])
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let! active =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member = firstMember
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstMember.Selection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member = secondMember
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondMember.Selection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorAuthority policy auditAuthorityId false } ] } ]
                    runtime
            // The observations are real: the host invokes each released member and records what
            // came back.
            let silent =
                scenario = "cbi16-01-one-member-interacted"
                || scenario = "cbi16-03-nothing-observed"
                || scenario = "cbi16-04-denied-before-any-frame"
            let observe index =
                task {
                    if scenario = "cbi16-03-nothing-observed" || (index = 1 && silent) then
                        return []
                    else
                        let memberValue = (List.item index active.Lifecycle.Value.Members).Member
                        let constraintValue =
                            if scenario = "cbi16-04-denied-before-any-frame" then
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
            let! firstObservations = observe 0
            let! secondObservations = observe 1
            let auditScope =
                if
                    scenario = "cbi16-06-one-member-ungranted"
                    || scenario = "cbi16-07-undeclared-outranks-ungranted"
                then
                    CapabilityScopeId.create "scope.other"
                else
                    authorityScope
            let firstAttribution =
                match scenario with
                | "cbi16-07-undeclared-outranks-ungranted" ->
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.other" } ]
                | "cbi16-10-mapping-not-distinct" ->
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.control" }
                      { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.other" } ]
                | _ ->
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.control" } ]
            let secondAttribution =
                match scenario with
                | "cbi16-05-one-member-undeclared"
                | "cbi16-08-retirement-failure" ->
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.other" } ]
                | _ ->
                    [ { Operation = CoolingFixture.setEnabled
                        DeclaredAuthority = "cooling.audit" } ]
            let interactions =
                let first =
                    { Selection = firstMember.Selection
                      Dependency =
                        { Definition = firstMember.Selection.Definition
                          Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = authorityScope } ] }
                      Attribution = firstAttribution
                      Observations = firstObservations }
                let second =
                    { Selection = secondMember.Selection
                      Dependency =
                        { Definition = secondMember.Selection.Definition
                          Entries =
                            [ { DeclaredAuthority =
                                  if scenario = "cbi16-11-declaration-mismatch" then
                                      "cooling.other"
                                  else
                                      "cooling.audit"
                                Capability = auditCapability
                                Target = authorityTarget
                                Operation = auditOperation
                                Scope = auditScope } ] }
                      Attribution = secondAttribution
                      Observations = secondObservations }
                if scenario = "cbi16-09-member-set-changed" then
                    [ first ]
                else
                    [ first; second ]
            let! verdict =
                ComponentGroupVerification.verify
                    resolution
                    active
                    interactions
                    runtime
                    (sprintf "group verification %s" scenario)
            return verdict, active, handlers
        }

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

    let revokedRequest (request: AuthorityAdmissionRequest) =
        { request with
            Evidence =
              request.Evidence
              |> List.map (fun evidence ->
                  { evidence with State = AdmissionEvidenceState.Revoked }) }

    let replacementToken kind =
        match kind with
        | ComponentGroupReplacementKind.Replaced -> "replaced"
        | ComponentGroupReplacementKind.CleanupFailed -> "cleanup-failed"
        | ComponentGroupReplacementKind.Declined -> "declined"
        | ComponentGroupReplacementKind.ActivationUnavailable -> "activation-unavailable"

    /// The activation being replaced: released, and expected to stay so until cutover.
    let replacementRetained failCleanup =
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
            let conversationFor index =
                let inner =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            List.item index handlers,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                if failCleanup then
                    FailingRetirementConversation inner :> IPortableProviderConversation
                else
                    inner
            let firstSelection =
                { selection (positionFor requirementId) with
                    HostEndpoint = "retained-host-primary" }
            let secondSelection =
                { selection (positionFor secondaryRequirementId) with
                    Requirement = secondaryRequirementId
                    HostEndpoint = "retained-host-secondary" }
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let! retained =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member =
                          { Selection = firstSelection
                            Conversation = conversationFor 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member =
                          { Selection = secondSelection
                            Conversation = conversationFor 1 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondSelection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorAuthority policy auditAuthorityId false } ] } ]
                    (runtimeRequest (plan [ firstSelection.Occurrence; secondSelection.Occurrence ]))
            return retained, handlers
        }

    let replacementResult scenario =
        task {
            let! retained, _ =
                replacementRetained (scenario = "cbi19-09-retained-cleanup-fails-after-cutover")
            let successorResolution = pairRequest () |> FakeGenerationResolver.resolve
            let providerSets =
                match successorResolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let positionFor requirement =
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> List.exactlyOne item.Members
            let handlers = [ CoolingHandler(); CoolingHandler() ]
            // A provider the required contract does not match never reports Ready.
            let secondDocument =
                if scenario = "cbi19-07-successor-member-never-ready" then
                    { CoolingFixture.contract with
                        Provider = expectProvider "brontide.fake.substituted" }
                else
                    CoolingFixture.contract
            let conversationFor document index =
                PortableDirectConversation(
                    PortableProviderEndpoint(document, List.item index handlers, Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let firstSelection =
                { selection (positionFor requirementId) with
                    HostEndpoint = "replacement-host-primary" }
            let secondSelection =
                { selection (positionFor secondaryRequirementId) with
                    Requirement = secondaryRequirementId
                    HostEndpoint = "replacement-host-secondary" }
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let providerRequest =
                if scenario = "cbi19-05-surviving-occurrence-authority-changed" then
                    let baseline = providerAuthority policy authorityId
                    { baseline with
                        Authority =
                          [ { List.exactlyOne baseline.Authority with
                                Capability = CapabilityId.create "capability.other" } ] }
                else
                    providerAuthority policy authorityId
            let supervisorRequest =
                supervisorAuthority
                    policy
                    auditAuthorityId
                    (scenario = "cbi19-06-successor-authority-denied")
            let occurrences = [ firstSelection.Occurrence; secondSelection.Occurrence ]
            let planValue =
                match scenario with
                | "cbi19-02-scope-mismatch" ->
                    planFor
                        (GenerationId.create "gen.successor")
                        (RestartScopeId.create "restart.other")
                        occurrences
                | "cbi19-03-generation-not-successor" ->
                    planFor
                        (GenerationId.create "gen.lifecycle")
                        (RestartScopeId.create "restart.lifecycle")
                        occurrences
                | _ ->
                    planFor
                        (GenerationId.create "gen.successor")
                        (RestartScopeId.create "restart.lifecycle")
                        occurrences
            let retainedGeneration =
                if scenario = "cbi19-04-retained-generation-mismatch" then
                    GenerationId.create "gen.retained"
                else
                    GenerationId.create "gen.lifecycle"
            let baseRequest = runtimeRequestFor planValue retainedGeneration
            let request =
                if scenario = "cbi19-08-release-fails-before-cutover" then
                    { baseRequest with
                        Release =
                            { baseRequest.Release with
                                FailureMoment = ReleaseFailureMoment.BeforeCutover } }
                else
                    baseRequest
            let! result =
                ComponentGroupReplacement.replace
                    successorResolution
                    retained
                    [ { Member =
                          { Selection = firstSelection
                            Conversation = conversationFor CoolingFixture.contract 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerRequest } ] }
                      { Member =
                          { Selection = secondSelection
                            Conversation = conversationFor secondDocument 1 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondSelection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorRequest } ] } ]
                    request
                    (sprintf "scoped replacement %s" scenario)
            return result, retained
        }

    let groupExtensionToken kind =
        match kind with
        | ComponentGroupExtensionKind.Extended -> "extended"
        | ComponentGroupExtensionKind.Declined -> "declined"
        | ComponentGroupExtensionKind.Withdrawn -> "withdrawn"
        | ComponentGroupExtensionKind.RetirementFailed -> "retirement-failed"
        | ComponentGroupExtensionKind.ActivationUnavailable -> "activation-unavailable"

    /// Two released members holding one participant each, so growth is observable.
    let extensionActivation failCleanup =
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
            let baseConversation handler =
                PortableDirectConversation(
                    PortableProviderEndpoint(
                        CoolingFixture.contract,
                        handler,
                        Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let secondConversation =
                let inner = baseConversation (List.item 1 handlers)
                if failCleanup then
                    FailingRetirementConversation inner :> IPortableProviderConversation
                else
                    inner
            let firstSelection =
                { selection (positionFor requirementId) with
                    HostEndpoint = "extension-host-primary" }
            let secondSelection =
                { selection (positionFor secondaryRequirementId) with
                    Requirement = secondaryRequirementId
                    HostEndpoint = "extension-host-secondary" }
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let admitted =
                [ providerAuthority policy authorityId
                  supervisorAuthority policy auditAuthorityId false ]
            let! active =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member =
                          { Selection = firstSelection
                            Conversation = baseConversation (List.item 0 handlers) }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = List.item 0 admitted } ] }
                      { Member =
                          { Selection = secondSelection
                            Conversation = secondConversation }
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondSelection.Occurrence
                                  Participant = supervisor }
                              Request = List.item 1 admitted } ] } ]
                    (runtimeRequest (plan [ firstSelection.Occurrence; secondSelection.Occurrence ]))
            return resolution, active, admitted, policy, handlers
        }

    let groupExtensionResult scenario =
        task {
            let failCleanup = scenario = "cbi18-14-retirement-failure"
            let! _, active, admitted, policy, _ = extensionActivation failCleanup
            let prior = active.Admissions
            let first = (List.item 0 prior).Occurrence
            let second = (List.item 1 prior).Occurrence
            // A second request for a party already live in the first member: distinct identities
            // throughout, and the Actor its own policy establishes.
            let sharedParty actorPolicy : AuthorityAdmissionRequest =
                let relationship =
                    RelationshipRequestId.create "relationship.group-provider-second"
                { providerAuthority actorPolicy authorityId with
                    Request = AdmissionRequestId.create "admission.group-provider-second"
                    Relationships =
                      [ { Request = relationship
                          ProposedActor = participant
                          Kind = ActorRelationshipKind.ComponentParticipant
                          Evidence = [ authorityEvidence ] } ]
                    Authority =
                      [ { Request = AuthorityRequestId.create "authority.cooling-control-second"
                          Relationship = relationship
                          Capability = capability
                          Target = authorityTarget
                          Operation = operation
                          Scope = authorityScope
                          Unlimited = false } ] }
            let observer' = observerRequest policy
            let firstRequests =
                match scenario with
                | "cbi18-03-shared-party-added-to-second-member"
                | "cbi18-04-shared-party-mapped-onto-a-second-actor"
                | "cbi18-07-activation-unchanged" -> [ List.item 0 admitted ]
                | "cbi18-05-removal-declined" -> []
                | "cbi18-06-substitution-declined" -> [ observer' ]
                | "cbi18-09-identity-shared-across-members" ->
                    [ List.item 0 admitted
                      { observer' with
                          Authority =
                            [ { List.exactlyOne observer'.Authority with
                                  Request = auditAuthorityId } ] } ]
                | "cbi18-10-local-actor-shared-across-members" ->
                    [ List.item 0 admitted
                      observerRequest (
                          groupPolicyFor providerLocalActor supervisorLocalActor supervisorLocalActor) ]
                | "cbi18-11-addition-denied"
                | "cbi18-15-lapse-outranks-a-denied-addition" ->
                    [ List.item 0 admitted; revokedRequest observer' ]
                | "cbi18-12-retained-identity-drift" ->
                    [ { List.item 0 admitted with
                          Authority =
                            [ { List.exactlyOne (List.item 0 admitted).Authority with
                                  Capability = CapabilityId.create "capability.other" } ] }
                      observer' ]
                | _ -> [ List.item 0 admitted; observer' ]
            let secondBase =
                if
                    scenario = "cbi18-13-untouched-member-lapsed"
                    || scenario = "cbi18-14-retirement-failure"
                    || scenario = "cbi18-15-lapse-outranks-a-denied-addition"
                then
                    revokedRequest (List.item 1 admitted)
                else
                    List.item 1 admitted
            let secondRequests =
                match scenario with
                | "cbi18-02-both-members-grown" -> [ secondBase; deputyRequest policy ]
                | "cbi18-03-shared-party-added-to-second-member" ->
                    [ secondBase; sharedParty policy ]
                | "cbi18-04-shared-party-mapped-onto-a-second-actor" ->
                    [ secondBase
                      sharedParty (groupPolicy deputyLocalActor supervisorLocalActor) ]
                | _ -> [ secondBase ]
            let requests =
                let entries =
                    [ { Occurrence = first; Requests = firstRequests }
                      { Occurrence = second; Requests = secondRequests } ]
                if scenario = "cbi18-08-member-set-changed" then
                    [ List.item 0 entries ]
                else
                    entries
            let! result =
                ComponentGroupExtension.extend
                    active
                    requests
                    (sprintf "group extension %s" scenario)
            return result, active, prior
        }

    let groupSuccessionToken kind =
        match kind with
        | ComponentGroupSuccessionKind.Narrowed -> "narrowed"
        | ComponentGroupSuccessionKind.Declined -> "declined"
        | ComponentGroupSuccessionKind.ActivationUnavailable -> "activation-unavailable"

    /// Two released members, the first covering its two declared authorities with two participants
    /// so a later CBI15 revision has one to release.
    let successionActivation () =
        task {
            let resolution =
                pairRequestFor
                    [ "cooling.control"; "cooling.audit" ]
                    [ "cooling.observe"; "cooling.report" ]
                |> FakeGenerationResolver.resolve
            let providerSets =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let positionFor requirement =
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> List.exactlyOne item.Members
            let handlers = [ CoolingHandler(); CoolingHandler() ]
            let conversationFor handler =
                PortableDirectConversation(
                    PortableProviderEndpoint(
                        CoolingFixture.contract,
                        handler,
                        Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let firstSelection =
                { selection (positionFor requirementId) with
                    HostEndpoint = "succession-host-primary" }
            let secondSelection =
                { selection (positionFor secondaryRequirementId) with
                    Requirement = secondaryRequirementId
                    HostEndpoint = "succession-host-secondary" }
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let observerRequest' =
                { observerRequest policy with
                    Authority =
                      [ { Request = observeAuthorityId
                          Relationship = observerRelationshipId
                          Capability = observeCapability
                          Target = authorityTarget
                          Operation = observeOperation
                          Scope = authorityScope
                          Unlimited = false }
                        { Request = reportAuthorityId
                          Relationship = observerRelationshipId
                          Capability = reportCapability
                          Target = authorityTarget
                          Operation = reportOperation
                          Scope = authorityScope
                          Unlimited = false } ] }
            let firstParticipants =
                [ providerAuthority policy authorityId
                  supervisorAuthority policy auditAuthorityId false ]
            let secondDeclaration: ComponentGrantDependency =
                { Definition = secondSelection.Definition
                  Entries =
                    [ { DeclaredAuthority = "cooling.observe"
                        Capability = observeCapability
                        Target = authorityTarget
                        Operation = observeOperation
                        Scope = authorityScope }
                      { DeclaredAuthority = "cooling.report"
                        Capability = reportCapability
                        Target = authorityTarget
                        Operation = reportOperation
                        Scope = authorityScope } ] }
            let! active =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member =
                          { Selection = firstSelection
                            Conversation = conversationFor (List.item 0 handlers) }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = List.item 0 firstParticipants }
                            { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = supervisor }
                              Request = List.item 1 firstParticipants } ] }
                      { Member =
                          { Selection = secondSelection
                            Conversation = conversationFor (List.item 1 handlers) }
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondSelection.Occurrence
                                  Participant = observer }
                              Request = observerRequest' } ] } ]
                    (runtimeRequest (plan [ firstSelection.Occurrence; secondSelection.Occurrence ]))
            // Each member interacts once, so each has an exercised authority of its own.
            let observeMember index =
                task {
                    let memberValue = (List.item index active.Lifecycle.Value.Members).Member
                    let! attempted =
                        memberValue.Invoke(
                            CoolingFixture.setEnabled,
                            CoolingFixture.commandV1,
                            CoolingFixture.authorizedCommand "primary" true,
                            PortableConstraint.Atom PortableTruth.Satisfied)
                    return
                        match attempted with
                        | Ok interaction ->
                            [ { Operation = CoolingFixture.setEnabled
                                Result = interaction } ]
                        | Error error ->
                            failwithf "Expected an observable interaction, got %A." error
                }
            let! firstObservations = observeMember 0
            let! secondObservations = observeMember 1
            let declarations =
                [ dependency firstSelection.Definition; secondDeclaration ]
            let attributions =
                [ [ { Operation = CoolingFixture.setEnabled
                      DeclaredAuthority = "cooling.control" } ]
                  [ { Operation = CoolingFixture.setEnabled
                      DeclaredAuthority = "cooling.observe" } ] ]
            return
                resolution,
                active,
                [ firstSelection; secondSelection ],
                declarations,
                attributions,
                [ firstObservations; secondObservations ],
                [ firstParticipants; [ observerRequest' ] ],
                handlers
        }

    /// The declaration a member narrows to, in the shape its successor generation records.
    let narrowedDeclaration
        (declaration: ComponentGrantDependency)
        index
        scenario
        : ComponentGrantDependency =
        let kept =
            if index = 0 then "cooling.control"
            elif scenario = "cbi17-03-use-vetoed-in-other-member" then "cooling.report"
            else "cooling.observe"
        if scenario = "cbi17-02-one-member-unchanged" && index = 1 then
            declaration
        else
            { declaration with
                Entries =
                    declaration.Entries
                    |> List.filter (fun entry -> entry.DeclaredAuthority = kept) }

    /// A successor that resolves the secondary position under a different binding scope.
    let rescopedSecondary () =
        let request = pairRequestFor [ "cooling.control" ] [ "cooling.observe" ]
        { request with
            Definitions =
                request.Definitions
                |> List.map (fun definition ->
                    if definition.Definition = consumer then
                        { definition with
                            Requirements =
                                definition.Requirements
                                |> List.map (fun requirement ->
                                    if requirement.Requirement = secondaryRequirementId then
                                        { requirement with
                                            Scope =
                                                Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.create
                                                    "scope.cooling-successor" }
                                    else
                                        requirement) }
                    else
                        definition) }

    let groupSuccessionResult scenario =
        task {
            let! resolution, active, selections, declarations, attributions, observations, _, handlers =
                successionActivation ()
            let before = handlers |> List.sumBy _.ProviderEffectCount
            let successorRequest =
                match scenario with
                | "cbi17-02-one-member-unchanged" ->
                    pairRequestFor [ "cooling.control" ] [ "cooling.observe"; "cooling.report" ]
                | "cbi17-03-use-vetoed-in-other-member" ->
                    pairRequestFor [ "cooling.control" ] [ "cooling.report" ]
                | "cbi17-04-activation-unchanged" ->
                    pairRequestFor
                        [ "cooling.control"; "cooling.audit" ]
                        [ "cooling.observe"; "cooling.report" ]
                | "cbi17-05-wider-in-one-member" ->
                    pairRequestFor
                        [ "cooling.control"; "cooling.audit"; "cooling.observe" ]
                        [ "cooling.observe" ]
                | "cbi17-07-member-position-absent" -> rescopedSecondary ()
                | "cbi17-08-successor-declares-nothing" ->
                    pairRequestFor [ "cooling.control" ] []
                | _ -> pairRequestFor [ "cooling.control" ] [ "cooling.observe" ]
            let successor = successorRequest |> FakeGenerationResolver.resolve
            let successorDeclaration index (declaration: ComponentGrantDependency) =
                match scenario, index with
                | "cbi17-04-activation-unchanged", _ -> declaration
                | "cbi17-05-wider-in-one-member", 0 ->
                    { declaration with
                        Entries =
                            declaration.Entries
                            @ [ { DeclaredAuthority = "cooling.observe"
                                  Capability = observeCapability
                                  Target = authorityTarget
                                  Operation = observeOperation
                                  Scope = authorityScope } ] }
                | "cbi17-06-tuple-changed", 0 ->
                    { declaration with
                        Entries =
                            [ { DeclaredAuthority = "cooling.control"
                                Capability = capability
                                Target = authorityTarget
                                Operation = operation
                                Scope = CapabilityScopeId.create "scope.other" } ] }
                | "cbi17-08-successor-declares-nothing", 1 ->
                    { declaration with Entries = [] }
                | _ -> narrowedDeclaration declaration index scenario
            let successions =
                selections
                |> List.mapi (fun index selection ->
                    { Selection = selection
                      Declaration = List.item index declarations
                      SuccessorDeclaration =
                        successorDeclaration index (List.item index declarations)
                      Attribution =
                        if scenario = "cbi17-10-ambiguous-attribution" && index = 0 then
                            List.item index attributions
                            @ [ { Operation = CoolingFixture.setEnabled
                                  DeclaredAuthority = "cooling.audit" } ]
                        else
                            List.item index attributions
                      Observations = List.item index observations })
            let intended =
                if scenario = "cbi17-09-member-set-changed" then
                    [ List.item 0 successions ]
                else
                    successions
            let result = ComponentGroupSuccession.succeed resolution successor active intended
            return result, active, before, (handlers |> List.sumBy _.ProviderEffectCount)
        }

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

    let tertiaryRequirementId = RequirementId.create "req.cooling-tertiary"
    let tertiaryProvider = DefinitionId.create "def.test.cooling-tertiary"
    let tertiaryContractId = ContractId.create "brontide.fake.cooling-tertiary"

    /// The independent positions a membership can be drawn from, one provider each.
    let positionCatalog =
        [ requirementId, provider, contractId
          secondaryRequirementId, secondaryProvider, secondaryContractId
          tertiaryRequirementId, tertiaryProvider, tertiaryContractId ]

    /// One independent requirement per named position, so the generation resolves exactly those and
    /// a membership can be drawn from any subset of them.
    let requestForPositions requirements =
        let single = request (Cardinality.parse "1..1")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let providerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = provider)
        let template = List.exactlyOne consumerDefinition.Requirements
        let candidate = List.exactlyOne single.Candidates
        let chosen =
            positionCatalog
            |> List.filter (fun (requirement, _, _) -> List.contains requirement requirements)
        { single with
            Definitions =
                [ { consumerDefinition with
                      Requirements =
                        chosen
                        |> List.map (fun (requirement, _, contract) ->
                            { template with
                                Requirement = requirement
                                Contract = contract }) }
                  yield!
                      chosen
                      |> List.map (fun (_, definition, contract) ->
                          { providerDefinition with
                              Definition = definition
                              Provides = [ { Contract = contract; Version = version } ] }) ]
            Candidates =
                chosen
                |> List.map (fun (_, definition, contract) ->
                    { candidate with
                        Definition = definition
                        Provides = [ { Contract = contract; Version = version } ] }) }

    /// One CBI21 scenario: the plan it is given and the members it selects.
    let stronglyConnectedResult scenario =
        task {
            let requirements =
                if scenario = "cbi21-02-mixed-grouping-activated" then
                    [ requirementId; secondaryRequirementId; tertiaryRequirementId ]
                else
                    [ requirementId; secondaryRequirementId ]
            let resolution = requestForPositions requirements |> FakeGenerationResolver.resolve
            let providerSets =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let handlers = requirements |> List.map (fun _ -> CoolingHandler())
            let members =
                requirements
                |> List.mapi (fun index requirement ->
                    let position =
                        providerSets
                        |> List.find (fun item -> item.Requirement = requirement)
                        |> fun item -> List.exactlyOne item.Members
                    { Selection =
                        { selection position with
                            Requirement = requirement
                            HostEndpoint = sprintf "cycle-host-%d" index }
                      Conversation =
                        PortableDirectConversation(
                            PortableProviderEndpoint(
                                CoolingFixture.contract,
                                List.item index handlers,
                                Realization.FixedDirectCall))
                        :> IPortableProviderConversation })
            let occurrences = members |> List.map _.Selection.Occurrence
            let planValue, selected =
                match scenario with
                | "cbi21-02-mixed-grouping-activated" ->
                    cyclePlan [ List.item 0 occurrences; List.item 1 occurrences ] [ List.item 2 occurrences ],
                    members
                | "cbi21-03-protocol-group-refused" -> protocolPlan occurrences, members
                | "cbi21-04-member-not-planned" -> cyclePlan [ List.item 0 occurrences ] [], members
                | "cbi21-05-member-not-selected" -> cyclePlan occurrences [], [ List.item 0 members ]
                | "cbi21-06-member-not-distinct" ->
                    cyclePlan [ List.item 0 occurrences ] [],
                    [ List.item 0 members; List.item 0 members ]
                | _ -> cyclePlan occurrences [], members
            let! result =
                ComponentGroupLifecycle.activate resolution selected (runtimeRequest planValue)
            return result, planValue, handlers
        }

    let childRegion = RegionId.create "region.child"
    let childPortId = PortId.create "port.child"
    let parentScopeId = RestartScopeId.create "restart.lifecycle"
    let childScopeId = RestartScopeId.create "restart.child"

    let portEnvelope port contract lifecycle : PortEnvelope =
        { Region = childRegion
          Port = port
          Lifecycle = lifecycle
          Contracts = [ { Contract = contract; Version = version } ]
          Cardinality = Cardinality.parse "1..1"
          Imports = []
          Exports = []
          AuthorityCeiling = []
          TopologyRequirements = []
          FailurePolicy = "isolate"
          RollbackBoundary = "scope"
          AllowWiderGenerationProposal = false }

    /// One position CM2 resolved inside a child Port of the named lifecycle.
    let childPosition lifecycle =
        let single = request (Cardinality.parse "1..1")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let contained =
            { List.exactlyOne consumerDefinition.Requirements with
                ContainingRegion = Some childRegion
                ContainingPort = Some childPortId
                RuntimeAttachment = lifecycle = PortLifecycleMode.RuntimeOpen }
        let resolution =
            { single with
                Definitions =
                    { consumerDefinition with Requirements = [ contained ] }
                    :: (single.Definitions |> List.filter (fun item -> item.Definition <> consumer))
                Ports = [ portEnvelope childPortId contractId lifecycle ] }
            |> FakeGenerationResolver.resolve
        let position =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) ->
                generation.ProviderSets |> List.exactlyOne |> fun item -> List.exactlyOne item.Members
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        resolution, { selection position with HostEndpoint = "child-host" }

    /// A position resolved outside any Port, for the attachment that has nothing to attach.
    let looseChildPosition () =
        let resolution = request (Cardinality.parse "1..1") |> FakeGenerationResolver.resolve
        let position =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) ->
                generation.ProviderSets |> List.exactlyOne |> fun item -> List.exactlyOne item.Members
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        resolution, { selection position with HostEndpoint = "child-host" }

    /// Two positions, each resolved into a Port of its own.
    let twoPortPositions () =
        let secondPort = PortId.create "port.child-secondary"
        let pair = requestForPositions [ requirementId; secondaryRequirementId ]
        let consumerDefinition =
            pair.Definitions |> List.find (fun item -> item.Definition = consumer)
        let contained =
            consumerDefinition.Requirements
            |> List.map (fun item ->
                { item with
                    ContainingRegion = Some childRegion
                    ContainingPort =
                        Some(if item.Requirement = requirementId then childPortId else secondPort)
                    RuntimeAttachment = true })
        let resolution =
            { pair with
                Definitions =
                    { consumerDefinition with Requirements = contained }
                    :: (pair.Definitions |> List.filter (fun item -> item.Definition <> consumer))
                Ports =
                    [ portEnvelope childPortId contractId PortLifecycleMode.RuntimeOpen
                      portEnvelope secondPort secondaryContractId PortLifecycleMode.RuntimeOpen ] }
            |> FakeGenerationResolver.resolve
        let providerSets =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        let selections =
            [ requirementId; secondaryRequirementId ]
            |> List.mapi (fun index requirement ->
                let position =
                    providerSets
                    |> List.find (fun item -> item.Requirement = requirement)
                    |> fun item -> List.exactlyOne item.Members
                { selection position with
                    Requirement = requirement
                    HostEndpoint = sprintf "two-port-host-%d" index })
        resolution, selections

    /// The parent activation a child attaches to: two members over the parent scope.
    let childParent fail =
        task {
            let resolution =
                requestForPositions [ requirementId; secondaryRequirementId ]
                |> FakeGenerationResolver.resolve
            let providerSets =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let handlers = [ CoolingHandler(); CoolingHandler() ]
            let secondDocument =
                if fail then
                    { CoolingFixture.contract with
                        Provider = expectProvider "brontide.fake.substituted" }
                else
                    CoolingFixture.contract
            let members =
                [ requirementId; secondaryRequirementId ]
                |> List.mapi (fun index requirement ->
                    let position =
                        providerSets
                        |> List.find (fun item -> item.Requirement = requirement)
                        |> fun item -> List.exactlyOne item.Members
                    { Selection =
                        { selection position with
                            Requirement = requirement
                            HostEndpoint = sprintf "parent-host-%d" index }
                      Conversation =
                        PortableDirectConversation(
                            PortableProviderEndpoint(
                                (if index = 1 then secondDocument else CoolingFixture.contract),
                                List.item index handlers,
                                Realization.FixedDirectCall))
                        :> IPortableProviderConversation })
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let! parent =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member = List.item 0 members
                        Participants =
                          [ { Mapping =
                                { Occurrence = (List.item 0 members).Selection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member = List.item 1 members
                        Participants =
                          [ { Mapping =
                                { Occurrence = (List.item 1 members).Selection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorAuthority policy auditAuthorityId false } ] } ]
                    (runtimeRequest (plan (members |> List.map _.Selection.Occurrence)))
            return parent, handlers
        }

    let childToken kind =
        match kind with
        | ComponentChildActivationKind.Attached -> "attached"
        | ComponentChildActivationKind.Declined -> "declined"
        | ComponentChildActivationKind.ParentUnavailable -> "parent-unavailable"

    /// A child member's authority is its own request; reusing the parent's identity would give the
    /// two the same grant identity without CM5 having decided anything about the child.
    let childAuthority policy revoked =
        let childRelationship = RelationshipRequestId.create "relationship.child"
        let baseline = providerAuthority policy authorityId
        let request' =
            { baseline with
                Request = AdmissionRequestId.create "admission.child"
                Relationships =
                  [ { Request = childRelationship
                      ProposedActor = participant
                      Kind = ActorRelationshipKind.ComponentParticipant
                      Evidence = [ authorityEvidence ] } ]
                Authority =
                  [ { Request = AuthorityRequestId.create "authority.child-control"
                      Relationship = childRelationship
                      Capability = capability
                      Target = authorityTarget
                      Operation = operation
                      Scope = authorityScope
                      Unlimited = false } ] }
        if revoked then revokedRequest request' else request'

    let childActivationResult scenario =
        task {
            let! parent, parentHandlers = childParent (scenario = "cbi22-03-parent-not-released")
            let resolution, childSelection =
                if scenario = "cbi22-07-member-not-port-contained" then
                    looseChildPosition ()
                else
                    childPosition (
                        if scenario = "cbi22-08-port-lifecycle-overstated" then
                            PortLifecycleMode.ActivationOpen
                        else
                            PortLifecycleMode.RuntimeOpen
                    )
            let childHandlers = [ CoolingHandler() ]
            let document =
                if scenario = "cbi22-11-child-member-never-ready" then
                    { CoolingFixture.contract with
                        Provider = expectProvider "brontide.fake.substituted" }
                else
                    CoolingFixture.contract
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let childScope =
                if scenario = "cbi22-05-child-scope-is-the-parent-scope" then
                    parentScopeId
                else
                    childScopeId
            let planValue =
                planFor (GenerationId.create "gen.child") childScope [ childSelection.Occurrence ]
            let hostAssisted =
                scenario = "cbi22-02-host-assisted-attached"
                || scenario = "cbi22-10-host-assisted-order-conflict"
            let baseRequest = runtimeRequestFor planValue (GenerationId.create "gen.child-retained")
            let childRequest =
                { baseRequest with
                    ActiveScopes =
                        [ yield
                              { Scope = childScope
                                Generation = GenerationId.create "gen.child-retained"
                                Status = RuntimeScopeStatus.ActiveScope }
                          if childScope <> parentScopeId then
                              yield
                                  { Scope = parentScopeId
                                    Generation = GenerationId.create "gen.lifecycle"
                                    Status = RuntimeScopeStatus.ActiveScope } ]
                    Child =
                        Some
                            { ParentScope = parentScopeId
                              ParentGeneration =
                                if scenario = "cbi22-04-parent-generation-mismatch" then
                                    GenerationId.create "gen.other"
                                else
                                    GenerationId.create "gen.lifecycle"
                              Port =
                                if scenario = "cbi22-06-attachment-names-another-port" then
                                    PortId.create "port.other"
                                else
                                    childPortId
                              RuntimeOpen = true
                              Occupied = scenario = "cbi22-09-occupied-port-without-replacement"
                              ReplacementLifecycleDeclared = false
                              HostAssisted = hostAssisted
                              InternalReleaseSequence = (if hostAssisted then 1 else 0)
                              ExportReleaseSequence =
                                (if scenario = "cbi22-10-host-assisted-order-conflict" then 1 else 2)
                              OuterHostOwnsAdmission = false } }
            let! result =
                ComponentChildActivation.attach
                    resolution
                    parent
                    [ { Member =
                          { Selection = childSelection
                            Conversation =
                              PortableDirectConversation(
                                  PortableProviderEndpoint(
                                      document,
                                      List.item 0 childHandlers,
                                      Realization.FixedDirectCall))
                              :> IPortableProviderConversation }
                        Participants =
                          [ { Mapping =
                                { Occurrence = childSelection.Occurrence
                                  Participant = participant }
                              Request =
                                childAuthority policy (scenario = "cbi22-12-child-authority-denied") } ] } ]
                    childRequest
            return result, parent, parentHandlers, childHandlers
        }

    /// The positions the successor generation resolves, per scenario.
    let membershipPositions scenario =
        match scenario with
        | "cbi20-02-position-dropped"
        | "cbi20-06-dropped-member-cleanup-fails-after-cutover"
        | "cbi20-08-member-not-resolved" -> [ requirementId ]
        | "cbi20-03-position-added-and-dropped"
        | "cbi20-05-dropped-actor-reused-by-added-party"
        | "cbi20-10-surviving-occurrence-authority-changed"
        | "cbi20-14-release-fails-before-cutover" -> [ requirementId; tertiaryRequirementId ]
        | "cbi20-04-membership-unchanged" -> [ requirementId; secondaryRequirementId ]
        | "cbi20-09-successor-resolves-nothing" -> []
        | _ -> [ requirementId; secondaryRequirementId; tertiaryRequirementId ]

    let membershipParticipant scenario policy occurrence requirement =
        if requirement = secondaryRequirementId then
            { Mapping = { Occurrence = occurrence; Participant = supervisor }
              Request = supervisorAuthority policy auditAuthorityId false }
        elif requirement = tertiaryRequirementId then
            let request = observerRequest policy
            { Mapping = { Occurrence = occurrence; Participant = observer }
              Request =
                if scenario = "cbi20-11-added-member-authority-denied" then
                    revokedRequest request
                else
                    request }
        else
            let request = providerAuthority policy authorityId
            { Mapping = { Occurrence = occurrence; Participant = participant }
              Request =
                if scenario = "cbi20-10-surviving-occurrence-authority-changed" then
                    { request with
                        Authority =
                          [ { List.exactlyOne request.Authority with
                                Capability = CapabilityId.create "capability.other" } ] }
                else
                    request }

    /// The members the caller supplies, which the fixture deliberately lets disagree with the
    /// generation in two scenarios.
    let membershipMembers (successor: ResolutionOutcome) scenario =
        let supplied =
            match scenario with
            | "cbi20-07-resolved-position-not-supplied"
            | "cbi20-08-member-not-resolved" -> [ requirementId; secondaryRequirementId ]
            | _ -> membershipPositions scenario
        let providerSets =
            match successor with
            | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
            | _ -> []
        let policy =
            match scenario with
            | "cbi20-05-dropped-actor-reused-by-added-party"
            | "cbi20-12-surviving-actor-reused-by-added-party" ->
                groupPolicyFor providerLocalActor supervisorLocalActor supervisorLocalActor
            | _ -> groupPolicy providerLocalActor supervisorLocalActor
        supplied
        |> List.mapi (fun index requirement ->
            let position =
                providerSets |> List.tryFind (fun item -> item.Requirement = requirement)
            let catalogued =
                positionCatalog |> List.find (fun (item, _, _) -> item = requirement)
            // A member the generation does not resolve still has to be nameable, so it borrows the
            // occurrence the retained activation holds for that position.
            let occurrence, definition =
                match position with
                | Some value ->
                    let memberValue = List.exactlyOne value.Members
                    memberValue.Occurrence, memberValue.Definition
                | None ->
                    let _, definition, _ = catalogued
                    OccurrenceId.create (sprintf "occ.%s.1" (DefinitionId.value definition)), definition
            let selection =
                { Requirement = requirement
                  Definition = definition
                  Occurrence = occurrence
                  Component = CoolingFixture.component'
                  Provider = CoolingFixture.provider
                  HostEndpoint = sprintf "membership-host-%d" index
                  ProviderEndpoint = "cooling-provider"
                  RequiredContract = CoolingFixture.contract }
            // A provider the required contract does not match never reports Ready.
            let document =
                if
                    scenario = "cbi20-13-added-member-never-ready"
                    && requirement = tertiaryRequirementId
                then
                    { CoolingFixture.contract with
                        Provider = expectProvider "brontide.fake.substituted" }
                else
                    CoolingFixture.contract
            { Member =
                { Selection = selection
                  Conversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(document, CoolingHandler(), Realization.FixedDirectCall))
                    :> IPortableProviderConversation }
              Participants = [ membershipParticipant scenario policy occurrence requirement ] })

    let membershipRuntimeRequest (members: ComponentGroupParticipant list) scenario =
        let baseRequest =
            runtimeRequestFor
                (planFor
                    (GenerationId.create "gen.successor")
                    (RestartScopeId.create "restart.lifecycle")
                    (members |> List.map _.Member.Selection.Occurrence))
                (GenerationId.create "gen.lifecycle")
        if scenario = "cbi20-14-release-fails-before-cutover" then
            { baseRequest with
                Release =
                    { baseRequest.Release with
                        FailureMoment = ReleaseFailureMoment.BeforeCutover } }
        else
            baseRequest

    /// The activation being replaced: two released members, one of which every drop scenario drops.
    let membershipRetained failDroppedCleanup =
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
            let conversationFor index =
                let inner =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            List.item index handlers,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                // Only the member the successor drops refuses withdrawal, so the failure names it.
                if failDroppedCleanup && index = 1 then
                    FailingRetirementConversation inner :> IPortableProviderConversation
                else
                    inner
            let firstSelection =
                { selection (positionFor requirementId) with
                    HostEndpoint = "retained-host-primary" }
            let secondSelection =
                { selection (positionFor secondaryRequirementId) with
                    Requirement = secondaryRequirementId
                    HostEndpoint = "retained-host-secondary" }
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let! retained =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member =
                          { Selection = firstSelection
                            Conversation = conversationFor 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member =
                          { Selection = secondSelection
                            Conversation = conversationFor 1 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = secondSelection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorAuthority policy auditAuthorityId false } ] } ]
                    (runtimeRequest (plan [ firstSelection.Occurrence; secondSelection.Occurrence ]))
            return retained, handlers
        }

    let membershipResult scenario =
        task {
            let! retained, _ =
                membershipRetained (scenario = "cbi20-06-dropped-member-cleanup-fails-after-cutover")
            let successor =
                membershipPositions scenario
                |> requestForPositions
                |> FakeGenerationResolver.resolve
            let members = membershipMembers successor scenario
            let! result =
                ComponentGroupMembership.replace
                    successor
                    retained
                    members
                    (membershipRuntimeRequest members scenario)
                    (sprintf "membership replacement %s" scenario)
            return result, retained
        }

    let membershipToken kind =
        match kind with
        | ComponentGroupMembershipKind.Replaced -> "replaced"
        | ComponentGroupMembershipKind.CleanupFailed -> "cleanup-failed"
        | ComponentGroupMembershipKind.Declined -> "declined"
        | ComponentGroupMembershipKind.ActivationUnavailable -> "activation-unavailable"

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
    member _.``shared CBI21 vectors activate a strongly connected group without a protocol``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi21-strongly-connected-group-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI21 vector identity must be a string"
                    | value -> value
                let! result, planValue, handlers = stronglyConnectedResult scenario
                let expectedCode = vector.GetProperty("expectedCode")
                multiple (fun () ->
                    Assert.That(
                        ComponentGroupLifecycle.isActive result,
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    Assert.That(
                        result.Failure |> Option.map _.Code,
                        Is.EqualTo(
                            if expectedCode.ValueKind = JsonValueKind.Null then
                                None
                            else
                                Some(expectedCode.GetString())),
                        scenario)
                    Assert.That(
                        planValue.Groups.Length,
                        Is.EqualTo(vector.GetProperty("expectedGroups").GetInt32()),
                        scenario)
                    Assert.That(
                        planValue.Groups |> List.sumBy (fun group -> group.Members.Length),
                        Is.EqualTo(vector.GetProperty("expectedMembers").GetInt32()),
                        sprintf "%s: the plan carries the members the vector names." scenario)
                    Assert.That(
                        result.Members.Length,
                        Is.EqualTo(vector.GetProperty("expectedPrepared").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Members |> List.filter _.Member.IsReleased |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    // Grouping changes which members CM4 expects observations for; it changes no
                    // barrier.
                    Assert.That(
                        (result.Members |> List.forall _.Member.IsReleased)
                        || (result.Members |> List.forall (fun item -> not item.Member.IsReleased)),
                        Is.True,
                        sprintf
                            "%s: the release barrier is the activation's, whatever the grouping."
                            scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo 0,
                        sprintf "%s: activation exercises nothing of its own." scenario))
        }

    [<Test>]
    member _.``C1 a group is refused for its protocols and not for its members``() =
        task {
            let! cycle, _, _ = stronglyConnectedResult "cbi21-01-ordinary-cycle-activated"
            let! mixed, mixedPlan, _ = stronglyConnectedResult "cbi21-02-mixed-grouping-activated"
            let! unplanned, _, _ = stronglyConnectedResult "cbi21-04-member-not-planned"
            let! unselected, _, _ = stronglyConnectedResult "cbi21-05-member-not-selected"
            let! repeated, _, _ = stronglyConnectedResult "cbi21-06-member-not-distinct"
            multiple (fun () ->
                Assert.That(
                    ComponentGroupLifecycle.isActive cycle,
                    Is.True,
                    "A cyclic group that declares no protocol needs nothing this seam lacks.")
                Assert.That(ComponentGroupLifecycle.isActive mixed, Is.True)
                Assert.That(
                    mixedPlan.Groups
                    |> List.map (fun group -> string group.Members.Length)
                    |> List.sort
                    |> String.concat ",",
                    Is.EqualTo "1,2",
                    "One plan carrying a singleton group and a cyclic pair activates as one activation.")
                Assert.That(unplanned.Failure.Value.Code, Is.EqualTo "member-not-planned")
                Assert.That(unselected.Failure.Value.Code, Is.EqualTo "member-not-selected")
                Assert.That(repeated.Failure.Value.Code, Is.EqualTo "member-not-distinct")
                Assert.That(
                    [ unplanned; unselected; repeated ]
                    |> List.forall (fun item -> item.Members.IsEmpty && item.Runtime.IsNone),
                    Is.True,
                    "Every plan refusal happens before a member is prepared."))
        }

    [<Test>]
    member _.``C2 a declared bounded protocol is refused by name``() =
        task {
            let! refused, planValue, handlers = stronglyConnectedResult "cbi21-03-protocol-group-refused"
            multiple (fun () ->
                Assert.That(
                    refused.Failure.Value.Kind,
                    Is.EqualTo ComponentGroupActivationFailureKind.PlanUnsupported)
                Assert.That(
                    refused.Failure.Value.Code,
                    Is.EqualTo "relational-initialisation-unsupported")
                Assert.That(
                    refused.Failure.Value.Reason,
                    Does.Contain "Relational Initialisation",
                    "The refusal names the stage rather than the group's shape.")
                Assert.That(
                    (List.exactlyOne planValue.Groups).Protocols.Length,
                    Is.EqualTo 2,
                    "The plan really does declare bounded protocols.")
                Assert.That(refused.Members, Is.Empty)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C3 the refusal is the seam's and CM3 and CM4 both accept the plan``() =
        task {
            let! refused, planValue, _ = stronglyConnectedResult "cbi21-03-protocol-group-refused"
            let baseRequest = runtimeRequest planValue
            let supplied =
                { baseRequest with
                    StageOutcomes =
                        planValue.Groups
                        |> List.collect (fun group ->
                            group.Members
                            |> List.collect (fun groupMember ->
                                group.Stages
                                |> List.map (fun stage ->
                                    { Group = group.Group
                                      Member = groupMember.Occurrence
                                      Stage = stage.Stage
                                      Succeeded = true
                                      Detail = "supplied" })))
                    InteractionAttempts =
                        planValue.Groups
                        |> List.collect (fun group ->
                            group.Protocols
                            |> List.mapi (fun index protocol ->
                                { Interaction =
                                    RuntimeInteractionId.create (sprintf "interaction.%d" index)
                                  Group = group.Group
                                  From = protocol.From
                                  To = protocol.To
                                  Phase = RuntimeInteractionPhase.RelationalInitialisation
                                  Kind = RuntimeInteractionKind.Lifecycle
                                  Edge = protocol.Edge
                                  Operation = Some protocol.Operation
                                  Capability = Some(List.head protocol.Authority)
                                  InputShape = Some protocol.InputShape })) }
            let runtime = FakeActivationRuntime.activate supplied
            multiple (fun () ->
                Assert.That(
                    ComponentGroupLifecycle.isActive refused,
                    Is.False,
                    "The integration refuses it.")
                Assert.That(
                    (List.exactlyOne planValue.Groups).Stages
                    |> List.exists (fun stage ->
                        stage.Stage = ActivationStage.RelationalInitialisationStage),
                    Is.True,
                    "CM3 planned the stage.")
                Assert.That(
                    runtime.Kind,
                    Is.EqualTo ActivationRuntimeOutcomeKind.Active,
                    "And CM4 accepts the plan and its declared handshakes, so neither of them is the refusal."))
        }

    [<Test>]
    member _.``C4 the seam leaves no window for a relational stage``() =
        task {
            let resolution = requestForPositions [ requirementId ] |> FakeGenerationResolver.resolve
            let position =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) ->
                    generation.ProviderSets
                    |> List.exactlyOne
                    |> fun item -> List.exactlyOne item.Members
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let prepared =
                match ComponentBindingIntegration.prepare resolution (selection position) with
                | ComponentBindingIntegrationResult.Prepared memberValue -> memberValue
                | outcome -> failwithf "Expected a prepared member, got %A." outcome
            let readyBefore = prepared.IsReady
            let! interconnected = prepared.Interconnect(directCooling CoolingFixture.contract)
            multiple (fun () ->
                Assert.That(readyBefore, Is.False)
                Assert.That(Result.isOk interconnected, Is.True)
                Assert.That(
                    prepared.IsReady,
                    Is.True,
                    "Interconnection carries establishment and the readiness signal together.")
                Assert.That(
                    CompositionStage.token prepared.Stage,
                    Is.EqualTo "interconnected",
                    "So a member is Ready before anything else the seam offers can be called, and CM4 requires Relational Initialisation to precede Ready."))
        }

    [<Test>]
    member _.``C5 the seam has no lifecycle traffic verb``() =
        task {
            let resolution = requestForPositions [ requirementId ] |> FakeGenerationResolver.resolve
            let position =
                match resolution with
                | ResolutionOutcome.Resolved(_, generation) ->
                    generation.ProviderSets
                    |> List.exactlyOne
                    |> fun item -> List.exactlyOne item.Members
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let prepared =
                match ComponentBindingIntegration.prepare resolution (selection position) with
                | ComponentBindingIntegrationResult.Prepared memberValue -> memberValue
                | outcome -> failwithf "Expected a prepared member, got %A." outcome
            let handler = CoolingHandler()
            let! _ =
                prepared.Interconnect(
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation)
            let! attempted =
                prepared.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            multiple (fun () ->
                Assert.That(prepared.IsReady, Is.True)
                Assert.That(
                    (match attempted with
                     | Ok value -> value.Category
                     | Error _ -> None),
                    Is.EqualTo(Some ProtocolCategory.StateViolation),
                    "The one verb a composition can initiate is gated on Release, and the refusal is the portable layer's own.")
                Assert.That(
                    (match attempted with
                     | Ok value -> value.FrameDecision
                     | Error _ -> FrameDecision.None),
                    Is.EqualTo FrameDecision.None,
                    "So a declared handshake could not reach a provider even if one were named.")
                Assert.That(handler.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C6 a delivered group activates on CBI12 terms``() =
        task {
            let! cycle, planValue, handlers = stronglyConnectedResult "cbi21-01-ordinary-cycle-activated"
            multiple (fun () ->
                Assert.That(ComponentGroupLifecycle.isActive cycle, Is.True)
                Assert.That(
                    (List.exactlyOne planValue.Groups).Cyclic,
                    Is.True,
                    "One group, and CM3 calls it cyclic.")
                Assert.That(
                    (List.exactlyOne planValue.Groups).Stages
                    |> List.exists (fun stage ->
                        stage.Stage = ActivationStage.RelationalInitialisationStage),
                    Is.False,
                    "And it declares no relational stage, which is why it is deliverable.")
                Assert.That(cycle.Members |> List.forall _.Member.IsReleased, Is.True)
                Assert.That(cycle.Runtime.Value.Kind, Is.EqualTo ActivationRuntimeOutcomeKind.Active)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C7 a delivered group performs none of its internal edges``() =
        task {
            let! cycle, planValue, handlers = stronglyConnectedResult "cbi21-01-ordinary-cycle-activated"
            multiple (fun () ->
                Assert.That(
                    (List.exactlyOne planValue.Groups).InternalEdges.Length,
                    Is.EqualTo 2,
                    "The edges that made the group are declarations.")
                Assert.That(
                    cycle.Runtime.Value.Observation.BindingExercises,
                    Is.Empty,
                    "Activation produces no binding exercise of its own; that is CBI16's question.")
                Assert.That(cycle.Runtime.Value.Observation.Interactions, Is.Empty)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``shared CBI22 vectors attach a child to a released parent``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi22-child-port-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI22 vector identity must be a string"
                    | value -> value
                let! result, parent, parentHandlers, _ = childActivationResult scenario
                let childMembers =
                    result.Child
                    |> Option.bind _.Lifecycle
                    |> Option.map _.Members
                    |> Option.defaultValue []
                let childReleased = childMembers |> List.filter _.Member.IsReleased |> List.length
                let parentMembers =
                    parent.Lifecycle |> Option.map _.Members |> Option.defaultValue []
                let parentReleased = parentMembers |> List.filter _.Member.IsReleased |> List.length
                multiple (fun () ->
                    Assert.That(
                        childToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        childReleased,
                        Is.EqualTo(vector.GetProperty("expectedChildReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        parentReleased,
                        Is.EqualTo(vector.GetProperty("expectedParentReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Child
                        |> Option.map (fun value -> value.Admissions.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                        scenario)
                    // A child activation is a second activation, never a replacement of the first.
                    Assert.That(
                        vector.GetProperty("expectedParentReleased").GetInt32() = 0
                        || not (
                            parentMembers
                            |> List.exists (fun item ->
                                CompositionStage.token item.Member.Stage = "retired")
                        ),
                        Is.True,
                        sprintf
                            "%s: nothing in a child activation stands a released parent down."
                            scenario)
                    Assert.That(
                        childReleased = childMembers.Length || childReleased = 0,
                        Is.True,
                        sprintf "%s: the child's release barrier covers the child's members." scenario)
                    Assert.That(
                        parentHandlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo 0,
                        sprintf "%s: no child outcome exercises a parent provider." scenario))
        }

    [<Test>]
    member _.``C1 a child needs a released parent and an attachment read from it``() =
        task {
            let! unavailable, _, _, _ = childActivationResult "cbi22-03-parent-not-released"
            let! generation, parent, _, _ = childActivationResult "cbi22-04-parent-generation-mismatch"
            let! scopeResult, _, _, _ = childActivationResult "cbi22-05-child-scope-is-the-parent-scope"
            multiple (fun () ->
                Assert.That(
                    unavailable.Kind,
                    Is.EqualTo ComponentChildActivationKind.ParentUnavailable)
                Assert.That(generation.Code, Is.EqualTo "parent-generation-mismatch")
                Assert.That(
                    scopeResult.Code,
                    Is.EqualTo "child-scope-not-distinct",
                    "A child Port exists to give its Component a restart boundary.")
                Assert.That(
                    [ unavailable; generation; scopeResult ]
                    |> List.forall (fun item -> item.Child.IsNone),
                    Is.True,
                    "Every refusal before establishment creates no child member.")
                Assert.That(
                    parent.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C2 the Port is the generation's and not the caller's``() =
        task {
            let! foreign, _, _, _ = childActivationResult "cbi22-06-attachment-names-another-port"
            let! loose, _, _, _ = childActivationResult "cbi22-07-member-not-port-contained"
            let! overstated, _, _, _ = childActivationResult "cbi22-08-port-lifecycle-overstated"
            let! attached, _, _, _ = childActivationResult "cbi22-01-child-attached"
            multiple (fun () ->
                Assert.That(foreign.Code, Is.EqualTo "port-not-resolved")
                Assert.That(loose.Code, Is.EqualTo "member-not-port-contained")
                Assert.That(
                    overstated.Code,
                    Is.EqualTo "port-lifecycle-overstated",
                    "The envelope, not the caller, says what the Port permits.")
                Assert.That(
                    attached.Port |> Option.map PortId.value,
                    Is.EqualTo(Some(PortId.value childPortId)),
                    "An admitted attachment names the Port its members were resolved into."))
        }

    [<Test>]
    member _.``C2 members drawn from two Ports have no one Port to attach to``() =
        task {
            let! parent, _ = childParent false
            let resolution, selections = twoPortPositions ()
            let handlers = selections |> List.map (fun _ -> CoolingHandler())
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let members =
                selections
                |> List.mapi (fun index childSelection ->
                    { Member =
                        { Selection = childSelection
                          Conversation =
                            PortableDirectConversation(
                                PortableProviderEndpoint(
                                    CoolingFixture.contract,
                                    List.item index handlers,
                                    Realization.FixedDirectCall))
                            :> IPortableProviderConversation }
                      Participants =
                        [ { Mapping =
                              { Occurrence = childSelection.Occurrence
                                Participant = (if index = 0 then participant else supervisor) }
                            Request =
                              if index = 0 then
                                  providerAuthority policy authorityId
                              else
                                  supervisorAuthority policy auditAuthorityId false } ] })
            let planValue =
                planFor
                    (GenerationId.create "gen.child")
                    childScopeId
                    (selections |> List.map _.Occurrence)
            let baseRequest = runtimeRequestFor planValue (GenerationId.create "gen.child-retained")
            let! result =
                ComponentChildActivation.attach
                    resolution
                    parent
                    members
                    { baseRequest with
                        ActiveScopes =
                            [ { Scope = childScopeId
                                Generation = GenerationId.create "gen.child-retained"
                                Status = RuntimeScopeStatus.ActiveScope }
                              { Scope = parentScopeId
                                Generation = GenerationId.create "gen.lifecycle"
                                Status = RuntimeScopeStatus.ActiveScope } ]
                        Child =
                            Some
                                { ParentScope = parentScopeId
                                  ParentGeneration = GenerationId.create "gen.lifecycle"
                                  Port = childPortId
                                  RuntimeOpen = true
                                  Occupied = false
                                  ReplacementLifecycleDeclared = false
                                  HostAssisted = false
                                  InternalReleaseSequence = 0
                                  ExportReleaseSequence = 2
                                  OuterHostOwnsAdmission = false } }
            multiple (fun () ->
                Assert.That(
                    result.Code,
                    Is.EqualTo "port-not-resolved",
                    "One attachment names one Port, so members from two have no single Port to attach to.")
                Assert.That(result.Child, Is.EqualTo None)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C3 a Port-contained position outside a child activation is refused``() =
        task {
            let resolution, childSelection = childPosition PortLifecycleMode.RuntimeOpen
            let planValue = plan [ childSelection.Occurrence ]
            let! flattened =
                ComponentGroupLifecycle.activate
                    resolution
                    [ { Selection = childSelection
                        Conversation = directCooling CoolingFixture.contract } ]
                    (runtimeRequest planValue)
            let! singleton =
                ComponentBindingLifecycle.activate
                    resolution
                    childSelection
                    (runtimeRequest planValue)
                    (directCooling CoolingFixture.contract)
            multiple (fun () ->
                Assert.That(
                    flattened.Failure.Value.Code,
                    Is.EqualTo "member-port-contained",
                    "The containment is a statement the generation made about where the Component runs.")
                Assert.That(flattened.Members, Is.Empty)
                Assert.That(
                    singleton.Failure.Value.Code,
                    Is.EqualTo "member-port-contained",
                    "And the singleton path flattened it too.")
                Assert.That(singleton.Member, Is.EqualTo None))
        }

    [<Test>]
    member _.``C4 an occupied Port needs an explicit replacement lifecycle``() =
        task {
            let! occupied, parent, _, childHandlers =
                childActivationResult "cbi22-09-occupied-port-without-replacement"
            multiple (fun () ->
                Assert.That(occupied.Code, Is.EqualTo "replacement-lifecycle-required")
                Assert.That(
                    occupied.Child.Value.Lifecycle.Value.Runtime.Value.Kind,
                    Is.EqualTo ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired,
                    "The classification is CM4's, reported rather than reformed.")
                Assert.That(
                    childHandlers |> List.sumBy _.ProviderEffectCount,
                    Is.EqualTo 0,
                    "It reaches no provider.")
                Assert.That(
                    parent.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C5 a host-assisted export follows the child's internal Release``() =
        task {
            let! ordered, _, _, _ = childActivationResult "cbi22-02-host-assisted-attached"
            let! conflict, _, _, _ = childActivationResult "cbi22-10-host-assisted-order-conflict"
            let child =
                ordered.Child.Value.Lifecycle.Value.Runtime.Value.Observation.Child.Value
            multiple (fun () ->
                Assert.That(ComponentChildActivation.isAttached ordered, Is.True)
                Assert.That(child.HostAssisted, Is.True)
                Assert.That(
                    child.ExportReleaseSequence,
                    Is.GreaterThan child.InternalReleaseSequence,
                    "The exported boundary is released after the child's own Release.")
                Assert.That(conflict.Code, Is.EqualTo "host-assisted-order-conflict"))
        }

    [<Test>]
    member _.``C6 the parent is untouched in every outcome``() =
        task {
            let! attached, parent, _, _ = childActivationResult "cbi22-01-child-attached"
            let survivor = (List.item 0 parent.Lifecycle.Value.Members).Member
            let! attempted =
                survivor.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            let interaction =
                match attempted with
                | Ok value -> value
                | Error error -> failwithf "Expected the parent to still serve, got %A." error
            let observation = attached.Child.Value.Lifecycle.Value.Runtime.Value.Observation
            multiple (fun () ->
                Assert.That(ComponentChildActivation.isAttached attached, Is.True)
                Assert.That(
                    parent.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True)
                Assert.That(
                    interaction.FrameDecision,
                    Is.Not.EqualTo FrameDecision.None,
                    "The parent is still serving ordinary interaction.")
                let parentScopeGeneration =
                    observation.Scopes
                    |> List.find (fun item -> item.Scope = parentScopeId)
                    |> fun item -> item.Generation |> Option.map GenerationId.value
                Assert.That(
                    parentScopeGeneration,
                    Is.EqualTo(Some "gen.lifecycle"),
                    "And CM4 reports the parent scope carrying the generation it already had."))
        }

    [<Test>]
    member _.``C7 the child's barriers are its own``() =
        task {
            let! neverReady, parent, _, _ = childActivationResult "cbi22-11-child-member-never-ready"
            let! attached, attachedParent, _, _ = childActivationResult "cbi22-01-child-attached"
            multiple (fun () ->
                Assert.That(neverReady.Code, Is.EqualTo "child-establishment-refused")
                Assert.That(
                    neverReady.Child.Value.Lifecycle.Value.Members
                    |> List.filter _.Member.IsReleased
                    |> List.length,
                    Is.EqualTo 0)
                Assert.That(
                    parent.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length,
                    Is.EqualTo 2,
                    "A child that never comes up leaves the parent exactly as it was.")
                Assert.That(
                    attached.Child.Value.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True)
                Assert.That(
                    attachedParent.Lifecycle.Value.Members
                    |> List.filter _.Member.IsReleased
                    |> List.length,
                    Is.EqualTo 2,
                    "And so does one that does."))
        }

    [<Test>]
    member _.``C8 authority is the child's own``() =
        task {
            let! denied, _, _, childHandlers = childActivationResult "cbi22-12-child-authority-denied"
            let! attached, parent, _, _ = childActivationResult "cbi22-01-child-attached"
            let parentGrants = parent.Grants |> List.map (fun item -> CapabilityGrantId.value item.Grant)
            multiple (fun () ->
                Assert.That(denied.Code, Is.EqualTo "authority-not-admitted")
                Assert.That(
                    childHandlers |> List.sumBy _.ProviderEffectCount,
                    Is.EqualTo 0,
                    "A denied child admission contacts no child provider.")
                Assert.That(attached.Child.Value.Admissions.Length, Is.EqualTo 1)
                Assert.That(
                    attached.Child.Value.Grants
                    |> List.exists (fun item ->
                        List.contains (CapabilityGrantId.value item.Grant) parentGrants),
                    Is.False,
                    "The parent's grants admit nothing for a child member."))
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
    member _.``shared CBI15 vectors revise per member and check the activation``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi15-group-revision-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI15 vector identity must be a string"
                    | value -> value
                let resolution =
                    pairRequestFor [ "cooling.control" ] [ "cooling.audit" ]
                    |> FakeGenerationResolver.resolve
                let providerSets =
                    match resolution with
                    | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                    | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                let positionFor requirement =
                    providerSets
                    |> List.find (fun item -> item.Requirement = requirement)
                    |> fun item -> List.exactlyOne item.Members
                let handlers = [ CoolingHandler(); CoolingHandler() ]
                let conversationFor handler =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            handler,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation
                let firstMember =
                    { Selection =
                        { selection (positionFor requirementId) with
                            HostEndpoint = "revision-host-primary" }
                      Conversation = conversationFor (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "revision-host-secondary" }
                      Conversation = conversationFor (List.item 1 handlers) }
                let admitted =
                    [ providerAuthority (groupPolicy providerLocalActor supervisorLocalActor) authorityId
                      supervisorAuthority
                          (groupPolicy providerLocalActor supervisorLocalActor)
                          auditAuthorityId
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
                                  Request = List.item 0 admitted } ] }
                          { Member = secondMember
                            Participants =
                              [ { Mapping =
                                    { Occurrence = secondMember.Selection.Occurrence
                                      Participant = supervisor }
                                  Request = List.item 1 admitted } ] } ]
                        (runtimeRequest (plan occurrences))
                // The first member gains an observer; the second member is restated unchanged.
                let observer' = observerRequest (groupPolicy providerLocalActor supervisorLocalActor)
                let firstDependency: ComponentGrantDependency =
                    { Definition = firstMember.Selection.Definition
                      Entries =
                        [ { DeclaredAuthority = "cooling.control"
                            Capability = capability
                            Target = authorityTarget
                            Operation = operation
                            Scope = authorityScope } ] }
                let secondDependency: ComponentGrantDependency =
                    { Definition = secondMember.Selection.Definition
                      Entries =
                        [ { DeclaredAuthority = "cooling.audit"
                            Capability = auditCapability
                            Target = authorityTarget
                            Operation = auditOperation
                            Scope = authorityScope } ] }
                let firstRequests =
                    match scenario with
                    | "cbi15-04-nothing-changed" -> [ List.item 0 admitted ]
                    | "cbi15-05-identity-shared-across-members" ->
                        [ List.item 0 admitted
                          { observer' with
                              Authority =
                                [ { List.exactlyOne observer'.Authority with
                                      Request = auditAuthorityId } ] } ]
                    | "cbi15-06-local-actor-shared-across-members" ->
                        // The observer is mapped onto the Actor the second member's supervisor
                        // already holds.
                        [ List.item 0 admitted
                          observerRequest (
                              groupPolicyFor providerLocalActor supervisorLocalActor supervisorLocalActor) ]
                    | "cbi15-07-dependency-not-covered" -> [ observer' ]
                    | "cbi15-08-retained-identity-drift" ->
                        [ { List.item 0 admitted with
                              Authority =
                                [ { List.exactlyOne (List.item 0 admitted).Authority with
                                      Capability = CapabilityId.create "capability.other" } ] }
                          observer' ]
                    | _ -> [ List.item 0 admitted; observer' ]
                let secondRequests =
                    if scenario = "cbi15-02-unchanged-member-lapsed" then
                        [ revokedRequest (List.item 1 admitted) ]
                    else
                        [ List.item 1 admitted ]
                let revisions =
                    let first =
                        { Occurrence = firstMember.Selection.Occurrence
                          Selection = firstMember.Selection
                          Dependency = firstDependency
                          Requests = firstRequests }
                    let second =
                        { Occurrence = secondMember.Selection.Occurrence
                          Selection = secondMember.Selection
                          Dependency = secondDependency
                          Requests = secondRequests }
                    if scenario = "cbi15-03-member-set-changed" then
                        [ first ]
                    else
                        [ first; second ]
                let! result =
                    ComponentGroupRevision.revise
                        resolution
                        active
                        revisions
                        (sprintf "group revision %s" scenario)
                let lifecycle = active.Lifecycle.Value
                let released = lifecycle.Members |> List.filter _.Member.IsReleased |> List.length
                let inForceParticipants =
                    result.InForce
                    |> Option.map (fun value ->
                        value.Admissions |> List.sumBy (fun item -> item.Participants.Length))
                    |> Option.defaultValue 0
                multiple (fun () ->
                    Assert.That(
                        groupRevisionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CurrentAuthority.Length,
                        Is.EqualTo(vector.GetProperty("expectedEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        inForceParticipants,
                        Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    // A declined change is local; only a lapse retires, and then the whole
                    // activation.
                    let retired =
                        result.Kind = ComponentGroupRevisionKind.Withdrawn
                        || result.Kind = ComponentGroupRevisionKind.RetirementFailed
                    Assert.That(result.InForce.IsNone, Is.EqualTo retired, scenario)
                    Assert.That(
                        released = lifecycle.Members.Length || released = 0,
                        Is.True,
                        scenario))
        }

    [<Test>]
    member _.``shared CBI19 vectors replace the generation in one scope``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi19-scoped-replacement-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI19 vector identity must be a string"
                    | value -> value
                let! result, retained = replacementResult scenario
                let retainedMembers = retained.Lifecycle.Value.Members
                let successorMembers =
                    result.Successor
                    |> Option.bind _.Lifecycle
                    |> Option.map _.Members
                    |> Option.defaultValue []
                let successorReleased =
                    successorMembers |> List.filter _.Member.IsReleased |> List.length
                multiple (fun () ->
                    Assert.That(
                        replacementToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CutOver,
                        Is.EqualTo(vector.GetProperty("expectedCutover").GetBoolean()),
                        scenario)
                    Assert.That(
                        successorReleased,
                        Is.EqualTo(vector.GetProperty("expectedSuccessorReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        retainedMembers |> List.filter _.Member.IsReleased |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedRetainedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        retainedMembers
                        |> List.filter (fun item ->
                            CompositionStage.token item.Member.Stage = "retired")
                        |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedRetainedRetired").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Successor
                        |> Option.map (fun value -> value.Admissions.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                        scenario)
                    // Cutover is the boundary in both directions.
                    Assert.That(
                        not result.Retired.IsEmpty,
                        Is.EqualTo result.CutOver,
                        sprintf
                            "%s: retained members are retired exactly when the scope cut over."
                            scenario)
                    Assert.That(
                        successorReleased = successorMembers.Length || successorReleased = 0,
                        Is.True,
                        sprintf
                            "%s: the release barrier arms for the whole successor activation."
                            scenario)
                    Assert.That(
                        result.CutOver || (retainedMembers |> List.forall _.Member.IsReleased),
                        Is.True,
                        sprintf "%s: before cutover the retained activation is untouched." scenario))
        }

    [<Test>]
    member _.``C1 replacement needs a released activation and a successor for the same scope``() =
        task {
            let unavailable: ComponentGroupAuthorityResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let! refusedInput =
                ComponentGroupReplacement.replace
                    (pairRequest () |> FakeGenerationResolver.resolve)
                    unavailable
                    []
                    (runtimeRequest (plan []))
                    "replacement unavailable"
            let! scopeResult, _ = replacementResult "cbi19-02-scope-mismatch"
            let! sameGeneration, _ = replacementResult "cbi19-03-generation-not-successor"
            let! retainedMismatch, _ = replacementResult "cbi19-04-retained-generation-mismatch"
            multiple (fun () ->
                Assert.That(
                    refusedInput.Kind,
                    Is.EqualTo ComponentGroupReplacementKind.ActivationUnavailable)
                Assert.That(scopeResult.Code, Is.EqualTo "restart-scope-mismatch")
                Assert.That(sameGeneration.Code, Is.EqualTo "generation-not-successor")
                Assert.That(retainedMismatch.Code, Is.EqualTo "retained-generation-mismatch")
                Assert.That(
                    [ refusedInput; scopeResult; sameGeneration; retainedMismatch ]
                    |> List.forall (fun item ->
                        item.Successor.IsNone && not item.CutOver && item.Retired.IsEmpty),
                    Is.True,
                    "Every refusal before establishment creates no successor and cuts nothing over."))
        }

    [<Test>]
    member _.``C2 authority is re-established and follows the occurrence``() =
        task {
            let! changed, changedRetained =
                replacementResult "cbi19-05-surviving-occurrence-authority-changed"
            let! replaced, retained = replacementResult "cbi19-01-surviving-occurrences-replaced"
            multiple (fun () ->
                Assert.That(
                    changed.Code,
                    Is.EqualTo "authority-revalidation-mismatch",
                    "A surviving occurrence may not be re-admitted for different authority.")
                Assert.That(changed.Successor, Is.EqualTo None)
                Assert.That(
                    changedRetained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True)
                // Re-established, not inherited: the successor carries its own admissions from this
                // attempt, over the same durable occurrences.
                Assert.That(
                    String.Join(
                        ",",
                        replaced.Successor.Value.Admissions
                        |> List.map (fun item -> OccurrenceId.value item.Occurrence)),
                    Is.EqualTo(
                        String.Join(
                            ",",
                            retained.Admissions
                            |> List.map (fun item -> OccurrenceId.value item.Occurrence))))
                Assert.That(
                    replaced.Successor.Value.Admissions
                    |> List.sumBy (fun item -> item.Participants.Length),
                    Is.EqualTo 2))
        }

    [<Test>]
    member _.``C3 the successor stands up under CBI13 barriers``() =
        task {
            let! denied, retained = replacementResult "cbi19-06-successor-authority-denied"
            multiple (fun () ->
                Assert.That(denied.Code, Is.EqualTo "authority-not-admitted")
                Assert.That(
                    denied.Successor.Value.Lifecycle,
                    Is.EqualTo None,
                    "An admission refusal contacts no successor provider at all.")
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "And it leaves the retained activation released."))
        }

    [<Test>]
    member _.``C4 the release barrier re-arms for the whole successor activation``() =
        task {
            let! refused, _ = replacementResult "cbi19-07-successor-member-never-ready"
            let! replaced, _ = replacementResult "cbi19-01-surviving-occurrences-replaced"
            multiple (fun () ->
                Assert.That(
                    refused.Successor.Value.Lifecycle.Value.Members
                    |> List.filter _.Member.IsReleased
                    |> List.length,
                    Is.EqualTo 0,
                    "One member that never reports Ready releases none of them.")
                Assert.That(
                    replaced.Successor.Value.Lifecycle.Value.Members
                    |> List.forall _.Member.IsReleased,
                    Is.True)
                Assert.That(
                    replaced.Successor.Value.Lifecycle.Value.Members.Length,
                    Is.EqualTo 2))
        }

    [<Test>]
    member _.``C5 before cutover the retained activation is untouched``() =
        task {
            let! refused, retained = replacementResult "cbi19-08-release-fails-before-cutover"
            let survivor = (List.item 0 retained.Lifecycle.Value.Members).Member
            let! attempted =
                survivor.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            let interaction =
                match attempted with
                | Ok value -> value
                | Error error -> failwithf "Expected the retained member to still serve, got %A." error
            multiple (fun () ->
                Assert.That(refused.Code, Is.EqualTo "release-failed-before-cutover")
                Assert.That(refused.CutOver, Is.False)
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The retained activation was never stood down, so it does not need restoring.")
                Assert.That(
                    interaction.FrameDecision,
                    Is.Not.EqualTo FrameDecision.None,
                    "It is still serving ordinary interaction."))
        }

    [<Test>]
    member _.``C6 the retained members are retired after cutover and never before``() =
        task {
            let! replaced, retained = replacementResult "cbi19-01-surviving-occurrences-replaced"
            let! refused, untouched = replacementResult "cbi19-07-successor-member-never-ready"
            multiple (fun () ->
                Assert.That(replaced.CutOver, Is.True)
                Assert.That(
                    retained.Lifecycle.Value.Members
                    |> List.forall (fun item ->
                        CompositionStage.token item.Member.Stage = "retired"),
                    Is.True,
                    "Every retained member is retired once the scope cut over.")
                Assert.That(replaced.Retired.Length, Is.EqualTo 2)
                Assert.That(refused.CutOver, Is.False)
                Assert.That(
                    untouched.Lifecycle.Value.Members
                    |> List.exists (fun item ->
                        CompositionStage.token item.Member.Stage = "retired"),
                    Is.False,
                    "And none is retired when cutover did not happen.")
                Assert.That(refused.Retired, Is.Empty))
        }

    [<Test>]
    member _.``C7 a cleanup failure after cutover stays visible and does not undo it``() =
        task {
            let! result, _ = replacementResult "cbi19-09-retained-cleanup-fails-after-cutover"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentGroupReplacementKind.CleanupFailed)
                Assert.That(result.Code, Is.EqualTo "retained-retirement-failed")
                Assert.That(result.CutOver, Is.True)
                Assert.That(
                    result.Successor.Value.Lifecycle.Value.Members
                    |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The scope has already cut over, so the successor stays released.")
                Assert.That(result.Reason, Does.Contain "withdraw-refused"))
        }

    [<Test>]
    member _.``C8 a replacement produces an activation the other slices accept``() =
        task {
            let! result, _ = replacementResult "cbi19-01-surviving-occurrences-replaced"
            let successor = result.Successor.Value
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let requests =
                [ { Occurrence = (List.item 0 successor.Admissions).Occurrence
                    Requests = [ providerAuthority policy authorityId ] }
                  { Occurrence = (List.item 1 successor.Admissions).Occurrence
                    Requests = [ supervisorAuthority policy auditAuthorityId false ] } ]
            let! continued =
                ComponentGroupRevalidation.revalidate
                    successor
                    requests
                    "revalidate the successor activation"
            multiple (fun () ->
                Assert.That(ComponentGroupReplacement.isReplaced result, Is.True)
                Assert.That(
                    continued.Kind,
                    Is.EqualTo ComponentGroupRevalidationKind.Continued,
                    "CBI14 accepts the activation a replacement produced."))
        }

    [<Test>]
    member _.``C9 the replacer adds no grant and widens no scope``() =
        task {
            let! result, retained = replacementResult "cbi19-01-surviving-occurrences-replaced"
            let observation = result.Successor.Value.Lifecycle.Value.Runtime.Value.Observation
            let retainedObservation = retained.Lifecycle.Value.Runtime.Value.Observation
            multiple (fun () ->
                Assert.That(
                    RestartScopeId.value observation.RestartScope,
                    Is.EqualTo(RestartScopeId.value retainedObservation.RestartScope),
                    "The successor occupies the scope the retained activation held; nothing widens.")
                Assert.That(
                    GenerationId.value observation.RetainedGeneration,
                    Is.EqualTo(GenerationId.value retainedObservation.TargetGeneration),
                    "And it names the generation it replaced.")
                Assert.That(
                    result.Successor.Value.Grants.Length,
                    Is.EqualTo retained.Grants.Length,
                    "Replacement grants no authority of its own."))
        }

    [<Test>]
    member _.``C10 a replacement migrates no state and replaces no single member``() =
        task {
            let! result, retained = replacementResult "cbi19-01-surviving-occurrences-replaced"
            let successorMembers = result.Successor.Value.Lifecycle.Value.Members
            let retainedMembers = retained.Lifecycle.Value.Members
            multiple (fun () ->
                Assert.That(
                    retainedMembers.Length,
                    Is.EqualTo successorMembers.Length,
                    "The successor resolves the same positions; none is added or removed.")
                Assert.That(
                    retainedMembers
                    |> List.exists (fun retainedItem ->
                        successorMembers
                        |> List.exists (fun successorItem ->
                            obj.ReferenceEquals(retainedItem.Member, successorItem.Member))),
                    Is.False,
                    "No portable member is carried across; the successor's are its own.")
                Assert.That(
                    result.Retired.Length,
                    Is.EqualTo retainedMembers.Length,
                    "The whole retained generation goes, never one member of it."))
        }

    [<Test>]
    member _.``shared CBI18 vectors grow every member set or none``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi18-group-extension-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI18 vector identity must be a string"
                    | value -> value
                let! result, active, _ = groupExtensionResult scenario
                let lifecycle = active.Lifecycle.Value
                let released = lifecycle.Members |> List.filter _.Member.IsReleased |> List.length
                let inForceParticipants =
                    result.InForce
                    |> Option.map (fun value ->
                        value.Admissions |> List.sumBy (fun item -> item.Participants.Length))
                    |> Option.defaultValue 0
                multiple (fun () ->
                    Assert.That(
                        groupExtensionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CurrentAuthority.Length,
                        Is.EqualTo(vector.GetProperty("expectedEvaluated").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Grown.Length,
                        Is.EqualTo(vector.GetProperty("expectedGrownMembers").GetInt32()),
                        scenario)
                    Assert.That(
                        inForceParticipants,
                        Is.EqualTo(vector.GetProperty("expectedInForceParticipants").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Lapsed.Length,
                        Is.EqualTo(vector.GetProperty("expectedLapsed").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    // Only a lapse retires, and then the whole activation.
                    Assert.That(
                        released = lifecycle.Members.Length || released = 0,
                        Is.True,
                        scenario)
                    let retired =
                        result.Kind = ComponentGroupExtensionKind.Withdrawn
                        || result.Kind = ComponentGroupExtensionKind.RetirementFailed
                    Assert.That(result.InForce.IsNone, Is.EqualTo retired, scenario)
                    Assert.That(
                        not result.Grown.IsEmpty,
                        Is.EqualTo(ComponentGroupExtension.isExtended result),
                        sprintf
                            "%s: an applied extension grows at least one member, and a refused one grows none."
                            scenario))
        }

    [<Test>]
    member _.``C1 extension needs a released activation and the members it admitted``() =
        task {
            let unavailable: ComponentGroupAuthorityResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let! refusedInput = ComponentGroupExtension.extend unavailable [] "extension unavailable"
            let! wrongMembers, active, _ = groupExtensionResult "cbi18-08-member-set-changed"
            multiple (fun () ->
                Assert.That(
                    refusedInput.Kind,
                    Is.EqualTo ComponentGroupExtensionKind.ActivationUnavailable)
                Assert.That(refusedInput.CurrentAuthority, Is.Empty)
                Assert.That(refusedInput.InForce, Is.EqualTo None)
                Assert.That(wrongMembers.Kind, Is.EqualTo ComponentGroupExtensionKind.Declined)
                Assert.That(
                    wrongMembers.CurrentAuthority,
                    Is.Empty,
                    "A member set the activation did not admit evaluates nothing.")
                Assert.That(
                    active.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C2 every member retains everyone and the activation gains someone``() =
        task {
            let! removal, removalActive, _ = groupExtensionResult "cbi18-05-removal-declined"
            let! substitution, _, _ = groupExtensionResult "cbi18-06-substitution-declined"
            let! unchanged, _, _ = groupExtensionResult "cbi18-07-activation-unchanged"
            multiple (fun () ->
                Assert.That(removal.Code, Is.EqualTo "participant-not-retained")
                Assert.That(
                    substitution.Code,
                    Is.EqualTo "participant-not-retained",
                    "A substitute is a removal plus an addition, and the removal decides it.")
                Assert.That(unchanged.Code, Is.EqualTo "activation-unchanged")
                Assert.That(
                    [ removal; substitution; unchanged ]
                    |> List.forall (fun item ->
                        item.CurrentAuthority.IsEmpty && item.Grown.IsEmpty),
                    Is.True,
                    "None of the three evaluates anything or grows anyone.")
                Assert.That(
                    removalActive.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C3 no declaration is consulted for any member``() =
        task {
            // The absent parameter is the contract: a declaration parameter would not type-check.
            let signature:
                ComponentGroupAuthorityResult
                    -> ComponentGroupMemberRequests list
                    -> string
                    -> Task<ComponentGroupExtensionResult> =
                ComponentGroupExtension.extend
            let! result, _, prior = groupExtensionResult "cbi18-01-one-member-grown"
            // Coverage is monotone in the grants held, which is why growth needs no declaration.
            let tuples (grants: LocalCapabilityGrant list) =
                grants
                |> List.map (fun grant ->
                    sprintf
                        "%s|%s|%s|%s"
                        (CapabilityId.value grant.Capability)
                        (ActorId.value grant.Target)
                        (OperationId.value grant.Operation)
                        (CapabilityScopeId.value grant.Scope))
            let declared =
                (dependency consumer).Entries
                |> List.map (fun entry ->
                    sprintf
                        "%s|%s|%s|%s"
                        (CapabilityId.value entry.Capability)
                        (ActorId.value entry.Target)
                        (OperationId.value entry.Operation)
                        (CapabilityScopeId.value entry.Scope))
            let before = prior |> List.collect _.Grants |> tuples
            let after = result.InForce.Value.Grants |> tuples
            ignore signature
            multiple (fun () ->
                Assert.That(
                    declared
                    |> List.filter (fun tuple -> List.contains tuple before)
                    |> List.forall (fun tuple -> List.contains tuple after),
                    Is.True,
                    "Every tuple covered before the extension is still covered after it.")
                Assert.That(
                    before |> List.forall (fun tuple -> List.contains tuple after),
                    Is.True,
                    "Growth withdraws no grant at all."))
        }

    [<Test>]
    member _.``C4 a declined extension changes nothing anywhere``() =
        task {
            let! result, active, prior = groupExtensionResult "cbi18-11-addition-denied"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentGroupExtensionKind.Declined)
                Assert.That(result.InForce.IsSome, Is.True)
                Assert.That(
                    result.InForce.Value.Admissions
                    |> List.sumBy (fun item -> item.Participants.Length),
                    Is.EqualTo(prior |> List.sumBy (fun item -> item.Participants.Length)),
                    "The in-force activation is the one it was given, not the one that was intended.")
                Assert.That(result.Grown, Is.Empty)
                Assert.That(
                    active.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C5 a malformed request decides nothing and evaluated loss retires everything``() =
        task {
            let! drift, driftActive, _ = groupExtensionResult "cbi18-12-retained-identity-drift"
            let! lapse, lapseActive, _ = groupExtensionResult "cbi18-13-untouched-member-lapsed"
            multiple (fun () ->
                Assert.That(drift.Kind, Is.EqualTo ComponentGroupExtensionKind.Declined)
                Assert.That(
                    drift.CurrentAuthority,
                    Is.Empty,
                    "Nothing was evaluated, so nothing was learned.")
                Assert.That(
                    driftActive.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True)
                Assert.That(lapse.Kind, Is.EqualTo ComponentGroupExtensionKind.Withdrawn)
                Assert.That(
                    lapse.CurrentAuthority,
                    Is.Not.Empty,
                    "No result both retires and reports zero evaluations.")
                Assert.That(
                    lapseActive.Lifecycle.Value.Members
                    |> List.forall (fun item ->
                        CompositionStage.token item.Member.Stage = "retired"),
                    Is.True,
                    "The lapse was in the member that was not growing, and the whole activation retires."))
        }

    [<Test>]
    member _.``C6 retained authority is revalidated before it is extended``() =
        task {
            let! result, active, _ =
                groupExtensionResult "cbi18-15-lapse-outranks-a-denied-addition"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentGroupExtensionKind.Withdrawn,
                    "A lapse outranks any problem with an addition, so a call that would both retire and decline retires.")
                Assert.That(result.Code, Is.EqualTo "authority-not-renewed")
                Assert.That(
                    result.InForce,
                    Is.EqualTo None,
                    "No set is extended on top of authority that has itself lapsed.")
                Assert.That(
                    active.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length,
                    Is.EqualTo 0))
        }

    [<Test>]
    member _.``C7 an added participant is admitted on CBI13 terms``() =
        task {
            let! result, active, _ = groupExtensionResult "cbi18-11-addition-denied"
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo "authority-not-admitted")
                Assert.That(
                    result.CurrentAuthority.Length,
                    Is.EqualTo 3,
                    "The addition was evaluated, and refused on the evaluator's own terms.")
                Assert.That(
                    active.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "A refused addition declines the extension rather than retiring the activation."))
        }

    [<Test>]
    member _.``C8 the extended activation obeys the activation-wide rules``() =
        task {
            let! shared, _, _ = groupExtensionResult "cbi18-03-shared-party-added-to-second-member"
            let! secondActor, _, _ =
                groupExtensionResult "cbi18-04-shared-party-mapped-onto-a-second-actor"
            let! sharedActor, _, _ =
                groupExtensionResult "cbi18-10-local-actor-shared-across-members"
            let! identity, _, _ = groupExtensionResult "cbi18-09-identity-shared-across-members"
            let actorsHeldByParticipant =
                shared.InForce.Value.Admissions
                |> List.collect _.Participants
                |> List.filter (fun item -> item.Participant = participant)
                |> List.map (fun item ->
                    (List.exactlyOne item.Authority.Observation.Relationships).LocalActor)
                |> List.distinct
            multiple (fun () ->
                Assert.That(
                    ComponentGroupExtension.isExtended shared,
                    Is.True,
                    "A party already participating in another member may be added to a second, under the local Actor it already holds.")
                Assert.That(
                    actorsHeldByParticipant.Length,
                    Is.EqualTo 1,
                    "It arrives at exactly one receiving-domain Actor across the activation.")
                Assert.That(secondActor.Code, Is.EqualTo "participant-actor-not-single")
                Assert.That(sharedActor.Code, Is.EqualTo "local-actor-shared-across-members")
                Assert.That(identity.Code, Is.EqualTo "authority-identity-not-distinct"))
        }

    [<Test>]
    member _.``C9 an extension produces an activation the other slices accept``() =
        task {
            let! _, active, admitted, policy, _ = extensionActivation false
            let intended =
                [ { Occurrence = (List.item 0 active.Admissions).Occurrence
                    Requests = [ List.item 0 admitted; observerRequest policy ] }
                  { Occurrence = (List.item 1 active.Admissions).Occurrence
                    Requests = [ List.item 1 admitted; deputyRequest policy ] } ]
            let! result = ComponentGroupExtension.extend active intended "extend before revalidating"
            // CBI14 revalidates the extended activation from the same requests that produced it.
            let! continued =
                ComponentGroupRevalidation.revalidate
                    result.InForce.Value
                    intended
                    "revalidate the extended activation"
            multiple (fun () ->
                Assert.That(ComponentGroupExtension.isExtended result, Is.True)
                Assert.That(
                    continued.Kind,
                    Is.EqualTo ComponentGroupRevalidationKind.Continued,
                    "CBI14 accepts the activation an extension produced.")
                Assert.That(
                    continued.Members |> List.sumBy (fun item -> item.CurrentAuthority.Length),
                    Is.EqualTo 4))
        }

    [<Test>]
    member _.``C10 an extension exercises nothing and notifies no provider``() =
        task {
            let! _, active, admitted, policy, handlers = extensionActivation false
            let before = handlers |> List.sumBy _.ProviderEffectCount
            let! result =
                ComponentGroupExtension.extend
                    active
                    [ { Occurrence = (List.item 0 active.Admissions).Occurrence
                        Requests = [ List.item 0 admitted; observerRequest policy ] }
                      { Occurrence = (List.item 1 active.Admissions).Occurrence
                        Requests = [ List.item 1 admitted ] } ]
                    "extension reaches no provider"
            multiple (fun () ->
                Assert.That(ComponentGroupExtension.isExtended result, Is.True)
                Assert.That(
                    handlers |> List.sumBy _.ProviderEffectCount,
                    Is.EqualTo before,
                    "CBI18 exercises no granted Operation.")
                Assert.That(
                    active.Lifecycle.Value.Members
                    |> List.forall (fun item ->
                        CompositionStage.token item.Member.Stage = "released"),
                    Is.True,
                    "It tells no provider the set changed, so no member's portable stage moves."))
        }

    [<Test>]
    member _.``shared CBI17 vectors narrow every member or none``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi17-group-succession-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI17 vector identity must be a string"
                    | value -> value
                let! result, active, effectsBefore, effectsAfter = groupSuccessionResult scenario
                let lifecycle = active.Lifecycle.Value
                let released = lifecycle.Members |> List.filter _.Member.IsReleased |> List.length
                multiple (fun () ->
                    Assert.That(
                        groupSuccessionToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Members |> List.sumBy (fun item -> item.Dropped.Length),
                        Is.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Members |> List.sumBy (fun item -> item.Vetoed.Length),
                        Is.EqualTo(vector.GetProperty("expectedVetoed").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Narrowed.Length,
                        Is.EqualTo(vector.GetProperty("expectedNarrowedMembers").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Members
                        |> List.sumBy (fun item -> item.Declaration.Entries.Length),
                        Is.EqualTo(vector.GetProperty("expectedDeclaredInForce").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    // This slice has no retirement path and reaches no provider.
                    Assert.That(released, Is.EqualTo lifecycle.Members.Length, scenario)
                    Assert.That(
                        effectsAfter,
                        Is.EqualTo effectsBefore,
                        sprintf
                            "%s: succession performs nothing, so no member's provider is reached."
                            scenario)
                    // A veto anywhere refuses every member's narrowing, and vice versa.
                    Assert.That(
                        not result.Vetoing.IsEmpty,
                        Is.EqualTo(
                            result.Members |> List.exists (fun item -> not item.Vetoed.IsEmpty)),
                        scenario)
                    Assert.That(
                        not result.Narrowed.IsEmpty,
                        Is.EqualTo(ComponentGroupSuccession.isNarrowed result),
                        sprintf
                            "%s: an applied succession narrows at least one member, and a refused one narrows none."
                            scenario))
        }

    [<Test>]
    member _.``a successor that narrows one member leaves the other untouched``() =
        task {
            let! result, active, _, _ = groupSuccessionResult "cbi17-02-one-member-unchanged"
            multiple (fun () ->
                Assert.That(ComponentGroupSuccession.isNarrowed result, Is.True)
                Assert.That(
                    OccurrenceId.value (List.exactlyOne result.Narrowed),
                    Is.EqualTo(OccurrenceId.value (List.item 0 active.Admissions).Occurrence),
                    "A member the successor does not narrow is untouched rather than refusing the succession.")
                Assert.That((List.item 1 result.Members).Dropped, Is.Empty)
                Assert.That((List.item 1 result.Members).Declaration.Entries.Length, Is.EqualTo 2))
        }

    [<Test>]
    member _.``a member the successor does not resolve blocks every other member``() =
        task {
            let! result, _, _, _ = groupSuccessionResult "cbi17-07-member-position-absent"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentGroupSuccessionKind.Declined)
                Assert.That(result.Code, Is.EqualTo "successor-position-mismatch")
                Assert.That(
                    result.Members |> List.forall (fun item -> item.Dropped.IsEmpty),
                    Is.True,
                    "A generation that does not resolve one member's position narrows none of them."))
        }

    [<Test>]
    member _.``a veto in one member refuses the narrowing the other had earned``() =
        task {
            let! result, active, _, _ = groupSuccessionResult "cbi17-03-use-vetoed-in-other-member"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentGroupSuccessionKind.Declined)
                Assert.That(
                    OccurrenceId.value (List.exactlyOne result.Vetoing),
                    Is.EqualTo(OccurrenceId.value (List.item 1 active.Admissions).Occurrence),
                    "The member that vetoed is named; the one whose narrowing it refused is not.")
                Assert.That((List.item 0 result.Members).Vetoed, Is.Empty)
                Assert.That(
                    (List.item 0 result.Members).Dropped,
                    Is.Empty,
                    "One transaction: the member with no veto drops nothing either.")
                Assert.That(
                    List.exactlyOne (List.item 1 result.Members).Vetoed,
                    Is.EqualTo "cooling.observe"))
        }

    [<Test>]
    member _.``a narrowed activation lets CBI15 release the participant it kept``() =
        task {
            let! resolution, active, selections, declarations, attributions, observations, participants, _ =
                successionActivation ()
            let revision index (declaration: ComponentGrantDependency) : ComponentGroupMemberRevision =
                { Occurrence = (List.item index selections).Occurrence
                  Selection = List.item index selections
                  Dependency = declaration
                  Requests =
                    if index = 0 then
                        [ List.item 0 (List.item 0 participants) ]
                    else
                        List.item 1 participants }
            // Dropping the supervisor is refused while the declaration in force still needs its
            // grant.
            let! before =
                ComponentGroupRevision.revise
                    resolution
                    active
                    [ revision 0 (List.item 0 declarations); revision 1 (List.item 1 declarations) ]
                    "drop the supervisor before succession"
            let successor =
                pairRequestFor [ "cooling.control" ] [ "cooling.observe" ]
                |> FakeGenerationResolver.resolve
            let narrowed =
                ComponentGroupSuccession.succeed
                    resolution
                    successor
                    active
                    (List.mapi
                        (fun index selection ->
                            { Selection = selection
                              Declaration = List.item index declarations
                              SuccessorDeclaration =
                                narrowedDeclaration (List.item index declarations) index ""
                              Attribution = List.item index attributions
                              Observations = List.item index observations })
                        selections)
            let! after =
                ComponentGroupRevision.revise
                    successor
                    active
                    [ revision 0 (List.item 0 narrowed.Members).Declaration
                      revision 1 (List.item 1 narrowed.Members).Declaration ]
                    "drop the supervisor after succession"
            multiple (fun () ->
                Assert.That(before.Code, Is.EqualTo "dependency-not-covered")
                Assert.That(ComponentGroupSuccession.isNarrowed narrowed, Is.True)
                Assert.That(
                    List.exactlyOne (List.item 0 narrowed.Members).Dropped,
                    Is.EqualTo "cooling.audit")
                Assert.That(after.Kind, Is.EqualTo ComponentGroupRevisionKind.Revised)
                Assert.That(
                    (List.item 0 after.InForce.Value.Admissions).Participants.Length,
                    Is.EqualTo 1,
                    "Narrowing permits the revision; it does not perform it."))
        }

    [<Test>]
    member _.``shared CBI16 vectors verify every member against its own declaration``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi16-group-verification-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI16 vector identity must be a string"
                    | value -> value
                let! result, active, handlers = groupVerificationResult scenario
                let lifecycle = active.Lifecycle.Value
                let released = lifecycle.Members |> List.filter _.Member.IsReleased |> List.length
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
                multiple (fun () ->
                    Assert.That(
                        groupVerificationToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        (ComponentGroupVerification.exercises result).Length,
                        Is.EqualTo(vector.GetProperty("expectedExercises").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Violating.Length,
                        Is.EqualTo(vector.GetProperty("expectedViolating").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Members |> List.sumBy (fun item -> item.Unexercised.Length),
                        Is.EqualTo(vector.GetProperty("expectedUnexercised").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Members |> List.sumBy (fun item -> item.Uncovered.Length),
                        Is.EqualTo(vector.GetProperty("expectedUncovered").GetInt32()),
                        scenario)
                    Assert.That(actualRuntimeActive, Is.EqualTo expectedRuntimeActive, scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo(int64 (vector.GetProperty("expectedProviderEffects").GetInt32())),
                        scenario)
                    // The runtime accepts the one projection exactly when every member is
                    // consistent.
                    match actualRuntimeActive with
                    | Some runtimeActive ->
                        Assert.That(
                            runtimeActive,
                            Is.EqualTo(ComponentGroupVerification.isConsistent result),
                            scenario)
                    | None -> ()
                    // A structural refusal evaluates nothing; a violation retires the whole
                    // activation.
                    Assert.That(
                        released = lifecycle.Members.Length || released = 0,
                        Is.True,
                        scenario)
                    Assert.That(
                        result.Violating.IsEmpty || released = 0,
                        Is.True,
                        sprintf "%s: a violation in any member closes every member's gate." scenario)
                    Assert.That(
                        result.Members
                        |> List.forall (fun item ->
                            let named = List.contains item.Occurrence result.Violating
                            ComponentGroupVerification.isViolating item = named),
                        Is.True,
                        sprintf
                            "%s: only members with a failed attribution are named as violating."
                            scenario))
        }

    [<Test>]
    member _.``one member's undeclared use is condemned by the runtime for the whole activation``() =
        task {
            let! result, active, _ = groupVerificationResult "cbi16-05-one-member-undeclared"
            let lifecycle = active.Lifecycle.Value
            let survivor = (List.item 0 lifecycle.Members).Member
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
                Assert.That(result.Kind, Is.EqualTo ComponentGroupVerificationKind.UndeclaredUse)
                Assert.That(
                    result.Runtime.Value.Kind,
                    Is.EqualTo ActivationRuntimeOutcomeKind.BindingObservationConflict,
                    "One request carries every member's exercises, so CM4's own rule refuses all of them.")
                Assert.That(
                    OccurrenceId.value (List.exactlyOne result.Violating),
                    Is.EqualTo(OccurrenceId.value (List.item 1 active.Admissions).Occurrence),
                    "The member that stayed inside its declaration is retired without being named as the cause.")
                Assert.That(
                    ComponentGroupVerification.isViolating (List.item 0 result.Members),
                    Is.False)
                Assert.That(result.Replacements.Length, Is.EqualTo lifecycle.Members.Length)
                Assert.That(CompositionStage.token survivor.Stage, Is.EqualTo "retired")
                Assert.That(interaction.Category, Is.EqualTo(Some ProtocolCategory.StateViolation)))
        }

    [<Test>]
    member _.``the same operation is attributed separately in each member``() =
        task {
            let! result, _, _ = groupVerificationResult "cbi16-02-same-operation-in-both-members"
            let exercises = ComponentGroupVerification.exercises result
            multiple (fun () ->
                Assert.That(ComponentGroupVerification.isConsistent result, Is.True)
                Assert.That(
                    exercises
                    |> List.map (fun item -> BindingExerciseId.value item.Exercise)
                    |> List.distinct
                    |> List.length,
                    Is.EqualTo 2,
                    "One CM4 request refuses a repeated binding-exercise identity.")
                Assert.That(
                    exercises |> List.forall _.AuthorityAdmitted,
                    Is.True,
                    "Each member's admission is derived from its own declaration and its own grants."))
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

    /// CBI19 claims one entry per successor member and no position added or removed; it checked
    /// neither, so a caller could drop a position the successor generation still resolves.
    [<Test>]
    member _.``CBI19 refuses a membership the successor generation does not resolve``() =
        task {
            let! retained, _ = replacementRetained false
            let successor = pairRequest () |> FakeGenerationResolver.resolve
            let position =
                match successor with
                | ResolutionOutcome.Resolved(_, generation) ->
                    generation.ProviderSets
                    |> List.find (fun item -> item.Requirement = requirementId)
                    |> fun item -> List.exactlyOne item.Members
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            let primary = { selection position with HostEndpoint = "partial-host-primary" }
            let! result =
                ComponentGroupReplacement.replace
                    successor
                    retained
                    [ { Member =
                          { Selection = primary
                            Conversation = directCooling CoolingFixture.contract }
                        Participants =
                          [ { Mapping =
                                { Occurrence = primary.Occurrence
                                  Participant = participant }
                              Request =
                                providerAuthority
                                    (groupPolicy providerLocalActor supervisorLocalActor)
                                    authorityId } ] } ]
                    (runtimeRequestFor
                        (planFor
                            (GenerationId.create "gen.successor")
                            (RestartScopeId.create "restart.lifecycle")
                            [ primary.Occurrence ])
                        (GenerationId.create "gen.lifecycle"))
                    "partial membership"
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo "position-not-supplied")
                Assert.That(result.CutOver, Is.False)
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "A membership the generation does not resolve stands nothing down."))
        }

    /// An added or dropped position is CBI20's operation, and CBI19 declines it by name.
    [<Test>]
    member _.``CBI19 refuses a changed membership``() =
        task {
            let! retained, _ = membershipRetained false
            let scenario = "cbi20-03-position-added-and-dropped"
            let successor =
                membershipPositions scenario
                |> requestForPositions
                |> FakeGenerationResolver.resolve
            let members = membershipMembers successor scenario
            let! result =
                ComponentGroupReplacement.replace
                    successor
                    retained
                    members
                    (membershipRuntimeRequest members scenario)
                    "changed membership through CBI19"
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo "membership-changed")
                Assert.That(result.CutOver, Is.False)
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``shared CBI20 vectors replace a membership across one cutover``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi20-membership-replacement-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI20 vector identity must be a string"
                    | value -> value
                let! result, retained = membershipResult scenario
                let retainedMembers = retained.Lifecycle.Value.Members
                let successorMembers =
                    result.Successor
                    |> Option.bind _.Lifecycle
                    |> Option.map _.Members
                    |> Option.defaultValue []
                let successorReleased =
                    successorMembers |> List.filter _.Member.IsReleased |> List.length
                let sorted occurrences =
                    occurrences
                    |> List.sortWith (fun left right ->
                        String.CompareOrdinal(OccurrenceId.value left, OccurrenceId.value right))
                multiple (fun () ->
                    Assert.That(
                        membershipToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.CutOver,
                        Is.EqualTo(vector.GetProperty("expectedCutover").GetBoolean()),
                        scenario)
                    Assert.That(
                        successorReleased,
                        Is.EqualTo(vector.GetProperty("expectedSuccessorReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        retainedMembers |> List.filter _.Member.IsReleased |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedRetainedReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        retainedMembers
                        |> List.filter (fun item ->
                            CompositionStage.token item.Member.Stage = "retired")
                        |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedRetainedRetired").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Successor
                        |> Option.map (fun value -> value.Admissions.Length)
                        |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Added.Length,
                        Is.EqualTo(vector.GetProperty("expectedAdded").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Dropped.Length,
                        Is.EqualTo(vector.GetProperty("expectedDropped").GetInt32()),
                        scenario)
                    // C2 over every vector: the three sets partition the two memberships.
                    Assert.That(
                        result.Added |> List.exists (fun item -> List.contains item result.Dropped),
                        Is.False,
                        sprintf "%s: nothing is both added and dropped." scenario)
                    Assert.That(
                        result.Dropped @ result.Surviving |> sorted,
                        Is.EqualTo<OccurrenceId>(
                            if result.Dropped.IsEmpty && result.Surviving.IsEmpty then
                                []
                            else
                                retained.Admissions |> List.map _.Occurrence |> sorted),
                        sprintf
                            "%s: dropped and surviving are the retained activation's membership."
                            scenario)
                    // C4 and C7: an addition needs the cutover, and the boundary holds both ways.
                    Assert.That(
                        result.CutOver || successorReleased = 0,
                        Is.True,
                        sprintf "%s: no member is released without a cutover." scenario)
                    Assert.That(
                        not result.Retired.IsEmpty,
                        Is.EqualTo result.CutOver,
                        sprintf
                            "%s: retained members are retired exactly when the scope cut over."
                            scenario)
                    Assert.That(
                        result.CutOver || (retainedMembers |> List.forall _.Member.IsReleased),
                        Is.True,
                        sprintf "%s: before cutover the retained activation is untouched." scenario))
        }

    [<Test>]
    member _.``C1 the membership is read from the successor generation``() =
        task {
            let! absent, absentRetained = membershipResult "cbi20-07-resolved-position-not-supplied"
            let! foreignMember, _ = membershipResult "cbi20-08-member-not-resolved"
            let unavailable: ComponentGroupAuthorityResult =
                { Admissions = []
                  Grants = []
                  Lifecycle = None
                  Failure = None }
            let! refusedInput =
                ComponentGroupMembership.replace
                    (pairRequest () |> FakeGenerationResolver.resolve)
                    unavailable
                    []
                    (runtimeRequest (plan []))
                    "membership unavailable"
            multiple (fun () ->
                Assert.That(absent.Code, Is.EqualTo "position-not-supplied")
                Assert.That(foreignMember.Code, Is.EqualTo "member-not-resolved")
                Assert.That(
                    refusedInput.Kind,
                    Is.EqualTo ComponentGroupMembershipKind.ActivationUnavailable)
                Assert.That(
                    [ absent; foreignMember; refusedInput ]
                    |> List.forall (fun item ->
                        item.Successor.IsNone
                        && not item.CutOver
                        && item.Added.IsEmpty
                        && item.Dropped.IsEmpty
                        && item.Surviving.IsEmpty),
                    Is.True,
                    "A refusal of the membership itself computes no membership change.")
                Assert.That(
                    absentRetained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C2 the added and dropped sets are derived from the generation``() =
        task {
            let! both, retained = membershipResult "cbi20-03-position-added-and-dropped"
            let sorted occurrences =
                occurrences
                |> List.sortWith (fun left right ->
                    String.CompareOrdinal(OccurrenceId.value left, OccurrenceId.value right))
            multiple (fun () ->
                Assert.That(
                    both.Added @ both.Surviving |> sorted,
                    Is.EqualTo<OccurrenceId>(
                        both.Successor.Value.Admissions |> List.map _.Occurrence |> sorted),
                    "Added and surviving are exactly the successor's membership.")
                Assert.That(
                    both.Dropped @ both.Surviving |> sorted,
                    Is.EqualTo<OccurrenceId>(retained.Admissions |> List.map _.Occurrence |> sorted),
                    "Dropped and surviving are exactly the retained activation's.")
                Assert.That(
                    OccurrenceId.value (List.exactlyOne both.Added),
                    Does.Contain "tertiary")
                Assert.That(
                    OccurrenceId.value (List.exactlyOne both.Dropped),
                    Does.Contain "secondary"))
        }

    [<Test>]
    member _.``C3 a dropped position authority is not re-established``() =
        task {
            let! result, retained = membershipResult "cbi20-02-position-dropped"
            let dropped = List.exactlyOne result.Dropped
            let priorGrants =
                retained.Admissions
                |> List.find (fun item -> item.Occurrence = dropped)
                |> fun item -> item.Grants |> List.map _.Request
            let successorGrants = result.Successor.Value.Grants |> List.map _.Request
            multiple (fun () ->
                Assert.That(
                    result.Successor.Value.Admissions
                    |> List.exists (fun item -> item.Occurrence = dropped),
                    Is.False,
                    "The successor admits nothing against a dropped occurrence.")
                Assert.That(priorGrants, Is.Not.Empty, "The dropped occurrence did hold a grant.")
                Assert.That(
                    priorGrants |> List.exists (fun item -> List.contains item successorGrants),
                    Is.False,
                    "And no grant of its authority survives into the successor."))
        }

    [<Test>]
    member _.``C4 an added position joins only across a cutover``() =
        task {
            let! refused, _ = membershipResult "cbi20-13-added-member-never-ready"
            let! denied, _ = membershipResult "cbi20-11-added-member-authority-denied"
            let! added, _ = membershipResult "cbi20-01-position-added"
            multiple (fun () ->
                Assert.That(
                    refused.Successor.Value.Lifecycle.Value.Members
                    |> List.filter _.Member.IsReleased
                    |> List.length,
                    Is.Zero,
                    "An added member that never reports Ready releases none of them.")
                Assert.That(refused.CutOver, Is.False)
                Assert.That(
                    denied.Successor.Value.Lifecycle,
                    Is.EqualTo None,
                    "An added member whose authority is denied reaches no provider.")
                Assert.That(added.CutOver, Is.True)
                Assert.That(
                    added.Successor.Value.Lifecycle.Value.Members
                    |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The addition is released with the whole successor activation.")
                Assert.That(added.Successor.Value.Lifecycle.Value.Members.Length, Is.EqualTo 3))
        }

    [<Test>]
    member _.``C5 an emptied membership is a withdrawal not a replacement``() =
        task {
            let! result, retained = membershipResult "cbi20-09-successor-resolves-nothing"
            multiple (fun () ->
                Assert.That(result.Code, Is.EqualTo "membership-empty")
                Assert.That(result.CutOver, Is.False)
                Assert.That(result.Retired, Is.Empty)
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "Standing the activation down is CBI14's operation, so this one stands nothing down."))
        }

    [<Test>]
    member _.``C6 the successor stands up under the earlier barriers``() =
        task {
            let! changedAuthority, _ =
                membershipResult "cbi20-10-surviving-occurrence-authority-changed"
            let! conflated, retained =
                membershipResult "cbi20-12-surviving-actor-reused-by-added-party"
            let! reused, _ = membershipResult "cbi20-05-dropped-actor-reused-by-added-party"
            multiple (fun () ->
                Assert.That(
                    changedAuthority.Code,
                    Is.EqualTo "authority-revalidation-mismatch",
                    "A surviving occurrence may not be re-admitted for different authority.")
                Assert.That(
                    conflated.Code,
                    Is.EqualTo "local-actor-shared-across-members",
                    "An addition may not take a surviving participant's receiving-domain Actor.")
                Assert.That(
                    conflated.Successor.Value.Lifecycle,
                    Is.EqualTo None,
                    "And it contacts no successor provider.")
                Assert.That(
                    ComponentGroupMembership.isReplaced reused,
                    Is.True,
                    "But it may take the Actor a dropped participant held.")
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C7 cutover is the boundary and the retained membership goes as a whole``() =
        task {
            let! refused, serving = membershipResult "cbi20-14-release-fails-before-cutover"
            let! replaced, retired = membershipResult "cbi20-03-position-added-and-dropped"
            let survivor = (List.item 1 serving.Lifecycle.Value.Members).Member
            let! attempted =
                survivor.Invoke(
                    CoolingFixture.setEnabled,
                    CoolingFixture.commandV1,
                    CoolingFixture.authorizedCommand "primary" true,
                    PortableConstraint.Atom PortableTruth.Satisfied)
            let interaction =
                match attempted with
                | Ok value -> value
                | Error error -> failwithf "Expected the dropped member to still serve, got %A." error
            multiple (fun () ->
                Assert.That(refused.Code, Is.EqualTo "release-failed-before-cutover")
                Assert.That(
                    serving.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "A pre-cutover failure leaves the dropped member serving too.")
                Assert.That(
                    interaction.FrameDecision,
                    Is.Not.EqualTo FrameDecision.None,
                    "The member whose position the successor drops is still interacting.")
                Assert.That(
                    retired.Lifecycle.Value.Members
                    |> List.forall (fun item ->
                        CompositionStage.token item.Member.Stage = "retired"),
                    Is.True,
                    "After cutover the whole retained membership goes, dropped and surviving alike.")
                Assert.That(replaced.Retired.Length, Is.EqualTo 2))
        }

    [<Test>]
    member _.``C8 a membership replacement produces an activation the other slices accept``() =
        task {
            let! result, _ = membershipResult "cbi20-01-position-added"
            let successor = result.Successor.Value
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let requests =
                [ { Occurrence = (List.item 0 successor.Admissions).Occurrence
                    Requests = [ providerAuthority policy authorityId ] }
                  { Occurrence = (List.item 1 successor.Admissions).Occurrence
                    Requests = [ supervisorAuthority policy auditAuthorityId false ] }
                  { Occurrence = (List.item 2 successor.Admissions).Occurrence
                    Requests = [ observerRequest policy ] } ]
            let! continued =
                ComponentGroupRevalidation.revalidate
                    successor
                    requests
                    "revalidate the replaced membership"
            multiple (fun () ->
                Assert.That(
                    continued.Kind,
                    Is.EqualTo ComponentGroupRevalidationKind.Continued,
                    "CBI14 accepts the activation a membership replacement produced.")
                Assert.That(
                    continued.Members |> List.map _.Occurrence,
                    Is.EqualTo<OccurrenceId>(successor.Admissions |> List.map _.Occurrence),
                    "And it names exactly the successor's membership, including the addition."))
        }

    [<Test>]
    member _.``C9 the membership replacer adds no grant and widens no scope``() =
        task {
            let! result, retained = membershipResult "cbi20-03-position-added-and-dropped"
            let observation = result.Successor.Value.Lifecycle.Value.Runtime.Value.Observation
            let retainedObservation = retained.Lifecycle.Value.Runtime.Value.Observation
            let admitted =
                result.Successor.Value.Admissions |> List.collect _.Grants |> List.length
            multiple (fun () ->
                Assert.That(
                    observation.RestartScope,
                    Is.EqualTo retainedObservation.RestartScope,
                    "The successor occupies the scope the retained activation held; nothing widens.")
                Assert.That(
                    observation.RetainedGeneration,
                    Is.EqualTo retainedObservation.TargetGeneration)
                Assert.That(
                    result.Successor.Value.Grants.Length,
                    Is.EqualTo admitted,
                    "Every grant in force was admitted in this attempt and none besides."))
        }

    [<Test>]
    member _.``C10 a membership replacement migrates no state and moves no single member``() =
        task {
            let! result, retained = membershipResult "cbi20-02-position-dropped"
            let successorMembers = result.Successor.Value.Lifecycle.Value.Members
            multiple (fun () ->
                Assert.That(
                    successorMembers.Length,
                    Is.EqualTo 1,
                    "The successor holds the positions its generation resolves, and no others.")
                Assert.That(
                    retained.Lifecycle.Value.Members
                    |> List.exists (fun item ->
                        successorMembers
                        |> List.exists (fun other ->
                            obj.ReferenceEquals(other.Member, item.Member))),
                    Is.False,
                    "No portable member is carried across; the successor's are its own.")
                Assert.That(
                    result.Retired.Length,
                    Is.EqualTo retained.Lifecycle.Value.Members.Length,
                    "The whole retained generation goes, never one member of it."))
        }
