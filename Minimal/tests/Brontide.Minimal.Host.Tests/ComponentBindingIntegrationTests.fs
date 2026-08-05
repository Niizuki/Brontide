namespace Brontide.Minimal.Host.Tests

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open NUnit.Framework
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Host

type private Cbi43Observation =
    { Code: string
      RefusedBy: string option
      PolicyApplied: bool
      Authorized: bool
      SourceOpened: bool
      Staged: bool
      Launched: bool
      Released: bool
      StoredFloor: int64
      StagedSetRemains: bool
      ProviderRunning: bool
      ExecutableInsideStore: bool }

type private Cbi44Observation =
    { Code: string
      RefusedBy: string option
      PolicyApplied: bool
      Authorized: bool
      SourceOpened: bool
      Staged: bool
      Revalidated: bool
      Launched: bool
      Released: bool
      LaunchPolicyChanged: bool option
      RegistrySequence: int64
      StoredFloor: int64
      StagedSetRemains: bool
      ProviderRunning: bool
      LaunchPolicyIsCurrent: bool
      LaunchAdmitsPublisher: bool
      StagedIsRequested: bool }

type private Cbi45Observation =
    { Code: string
      RefusedBy: string option
      Revalidated: bool
      Continued: bool
      PolicyChanged: bool
      MemberReleased: bool
      ProviderRunning: bool
      StagedSetRemains: bool
      ServingPolicyIsCurrent: bool
      DecisionMatchesStagedIdentity: bool }

type private Cbi46Observation =
    { Code: string
      RefusedBy: string
      Order: string list
      MemberCodes: string list
      Continued: int
      Withdrawn: int
      FirstServing: bool
      SecondServing: bool
      StagedSetRemains: bool }

type private Cbi30Observation =
    { Active: bool
      Code: string
      Realization: string option
      AnsweringProvider: string option
      Released: bool
      Retired: bool
      ProviderExited: bool }

type private Cbi32Observation =
    { StageCode: string
      Staged: bool
      Reused: bool
      ActiveRemovalCode: string
      Active: bool
      Released: bool
      Retired: bool
      ProviderExited: bool
      RemovalCode: string
      Residue: bool }

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

    let startCbi30Provider providerName =
        let variable =
            match providerName with
            | "reference" -> "BRONTIDE_REFERENCE_PROVIDER"
            | "minimal" -> "BRONTIDE_MINIMAL_PROVIDER"
            | value -> invalidArg (nameof providerName) (sprintf "Unknown provider '%s'." value)
        match Environment.GetEnvironmentVariable variable |> Option.ofObj with
        | Some path when File.Exists path ->
            let info = ProcessStartInfo(path, "--portable")
            info.RedirectStandardInput <- true
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.UseShellExecute <- false
            info.CreateNoWindow <- true
            match Process.Start info |> Option.ofObj with
            | Some providerProcess -> providerProcess
            | None -> failwith "The CBI30 provider process did not start."
        | _ ->
            Assert.Ignore(sprintf "%s does not name a built provider endpoint." variable)
            failwith "The cross-process test was ignored."

    let cbi30Conversation (providerProcess: Process) =
        PortableProcessConversation(
            PortableStreamDuplex(
                providerProcess.StandardOutput.BaseStream,
                providerProcess.StandardInput.BaseStream,
                PortableLimits.declared,
                false),
            PortableLimits.declared)
        :> IPortableProviderConversation

    let stopCbi30Provider (providerProcess: Process) =
        if not providerProcess.HasExited then
            providerProcess.Kill true
        providerProcess.WaitForExit()

    let cbi30Run providerName interruptBeforeInterconnection =
        task {
            use providerProcess = startCbi30Provider providerName
            let conversation = cbi30Conversation providerProcess
            try
                if interruptBeforeInterconnection then
                    providerProcess.Kill true
                    providerProcess.WaitForExit()

                let resolution, selected, occurrence = prepared ()
                let! result =
                    ComponentBindingLifecycle.activate
                        resolution
                        selected
                        (runtimeRequest (plan [ occurrence ]))
                        conversation
                let memberValue = result.Member
                let active =
                    result.Failure.IsNone
                    && result.Runtime
                       |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                    && memberValue |> Option.exists _.IsReleased
                let realization =
                    memberValue
                    |> Option.bind _.TryPlan
                    |> Option.map (BindingPlan.realization >> Realization.token)
                let answeringProvider =
                    memberValue
                    |> Option.bind _.AnsweringProvider
                    |> Option.map PortableProviderRef.text
                let released = memberValue |> Option.exists _.IsReleased
                let! retired =
                    task {
                        if active then
                            let! retirement = memberValue.Value.Retire "CBI30 process activation completed."
                            return
                                match retirement with
                                | Ok record ->
                                    CompositionStage.token memberValue.Value.Stage = "retired"
                                    && record.ReplacementPermitted
                                | Error _ -> false
                        else
                            return false
                    }
                memberValue |> Option.iter _.Close()
                let exited = providerProcess.HasExited || providerProcess.WaitForExit 5000
                return
                    { Active = active
                      Code = result.Failure |> Option.map _.Code |> Option.defaultValue "active"
                      Realization = realization
                      AnsweringProvider = answeringProvider
                      Released = released
                      Retired = retired
                      ProviderExited = exited }
            finally
                stopCbi30Provider providerProcess
        }

    let cbi31ProviderPath providerName =
        let variable =
            match providerName with
            | "reference" -> "BRONTIDE_REFERENCE_PROVIDER"
            | "minimal" -> "BRONTIDE_MINIMAL_PROVIDER"
            | value -> invalidArg (nameof providerName) (sprintf "Unknown provider '%s'." value)
        match Environment.GetEnvironmentVariable variable |> Option.ofObj with
        | Some path when File.Exists path -> Path.GetFullPath path
        | _ ->
            Assert.Ignore(sprintf "%s does not name a built provider endpoint." variable)
            failwith "The cross-process test was ignored."

    let cbi31Digest path =
        use stream = File.OpenRead path
        SHA256.HashData stream |> Convert.ToHexString

    let cbi31Vector identity =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cbi31-local-artifact-activation-vectors.json")
        use fixture = JsonDocument.Parse(File.ReadAllText path)
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("id").GetString() = identity)
        |> _.Clone()

    let cbi31Run (vector: JsonElement) =
        task {
            let requiredString (name: string) : string =
                match vector.GetProperty(name).GetString() |> Option.ofObj with
                | None -> failwithf "CBI31 property '%s' must be a string." name
                | Some value -> value
            let providerPath = cbi31ProviderPath (requiredString "provider")
            let requestedPath =
                if requiredString "path" = "missing" then
                    providerPath + ".missing"
                else
                    providerPath
            let digest =
                if requiredString "digest" = "incorrect" then
                    String('0', 64)
                else
                    cbi31Digest providerPath
            let strings (name: string) =
                vector.GetProperty(name).EnumerateArray()
                |> Seq.map (fun value ->
                    match value.GetString() |> Option.ofObj with
                    | None -> failwithf "CBI31 property '%s' must contain strings." name
                    | Some text -> text)
                |> Seq.toList
            let allowedRoot =
                match Path.GetDirectoryName providerPath with
                | null -> failwith "CBI31 provider path must have a parent directory."
                | value -> value
            let activation =
                LocalProviderArtifactActivator.acquireAndLaunch
                    { Identity = requiredString "id"
                      SourcePath = requestedPath
                      Sha256 = digest
                      Arguments = strings "arguments" }
                    { AllowedRoot = allowedRoot
                      AllowedArguments = strings "allowedArguments" }
            match activation with
            | LocalProviderActivation.Refused failure ->
                return failure.Code, false, None, false, false, false, true
            | LocalProviderActivation.Launched owner ->
                use owner = owner
                let resolution, selected, occurrence = prepared ()
                let! result =
                    ComponentBindingLifecycle.activate
                        resolution
                        selected
                        (runtimeRequest (plan [ occurrence ]))
                        owner.Conversation
                let memberValue = result.Member
                let active =
                    result.Failure.IsNone
                    && result.Runtime
                       |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                    && memberValue |> Option.exists _.IsReleased
                let released = memberValue |> Option.exists _.IsReleased
                let! retired =
                    task {
                        if active then
                            let! retirement = memberValue.Value.Retire "CBI31 artifact activation completed."
                            return
                                match retirement with
                                | Ok record ->
                                    CompositionStage.token memberValue.Value.Stage = "retired"
                                    && record.ReplacementPermitted
                                | Error _ -> false
                        else
                            return false
                    }
                memberValue |> Option.iter _.Close()
                let exited = owner.WaitForExit(TimeSpan.FromSeconds 5.0)
                return
                    result.Failure |> Option.map _.Code |> Option.defaultValue "active",
                    true,
                    Some owner.Isolation,
                    active,
                    released,
                    retired,
                    exited
        }

    let cbi32DeleteTree path =
        let rec remove attempt =
            if Directory.Exists path then
                try
                    Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    |> Seq.iter (fun file -> File.SetAttributes(file, FileAttributes.Normal))
                    Directory.Delete(path, true)
                with
                | :? IOException when attempt < 9 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
                | :? UnauthorizedAccessException when attempt < 9 ->
                    Threading.Thread.Sleep 25
                    remove (attempt + 1)
        remove 0

    let cbi32Declaration (provider: string) (sourceRoot: string) (mutation: string) : ProviderArtifactSet =
        let fileName (path: string) =
            match Path.GetFileName path |> Option.ofObj with
            | Some value -> value
            | None -> failwith "CBI32 source member must have a file name."
        let providerPath = cbi31ProviderPath provider
        let providerRoot =
            match Path.GetDirectoryName providerPath |> Option.ofObj with
            | Some value -> value
            | None -> failwith "CBI32 provider path must have a parent directory."
        Directory.CreateDirectory sourceRoot |> ignore
        Directory.EnumerateFiles providerRoot
        |> Seq.sort
        |> Seq.iter (fun source -> File.Copy(source, Path.Combine(sourceRoot, fileName source)))
        let mutable files =
            Directory.EnumerateFiles sourceRoot
            |> Seq.map (fun path ->
                { RelativePath = fileName path
                  Sha256 = cbi31Digest path })
            |> Seq.sortBy _.RelativePath
            |> Seq.toList
        match mutation with
        | "missing-member" ->
            files <- files @ [ { RelativePath = "missing-member.dll"; Sha256 = String('0', 64) } ]
        | "member-integrity" ->
            files <- { files.Head with Sha256 = String('0', 64) } :: files.Tail
        | "traversal" ->
            files <- files @ [ { RelativePath = "../escape.dll"; Sha256 = String('0', 64) } ]
        | _ -> ()
        let executable = fileName providerPath
        let arguments = [ "--portable" ]
        let mutable identity = ProviderArtifactSetIdentity.compute files executable arguments
        if mutation = "identity" then
            identity <- ProviderArtifactSetId.create (String('0', 64))
        { Identity = identity
          SourceRoot = sourceRoot
          Files = files
          ExecutablePath = executable
          Arguments = arguments }

    let cbi32Vector identity =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cbi32-content-addressed-staging-vectors.json")
        use fixture = JsonDocument.Parse(File.ReadAllText path)
        fixture.RootElement.GetProperty("vectors").EnumerateArray()
        |> Seq.find (fun vector -> vector.GetProperty("id").GetString() = identity)
        |> _.Clone()

    let cbi32Run (vector: JsonElement) =
        task {
            let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}")
            let sourceRoot = Path.Combine(testRoot, "source")
            let storeRoot = Path.Combine(testRoot, "store")
            try
                let requiredString (name: string) : string =
                    match vector.GetProperty(name).GetString() |> Option.ofObj with
                    | Some value -> value
                    | None -> failwithf "CBI32 property '%s' must be a string." name
                let declaration =
                    cbi32Declaration (requiredString "provider") sourceRoot (requiredString "mutation")
                let store = ContentAddressedProviderStore storeRoot
                match store.Stage declaration with
                | ProviderArtifactStagingResult.Refused failure ->
                    let removal = store.Remove declaration.Identity
                    return
                        { StageCode = failure.Code
                          Staged = false
                          Reused = false
                          ActiveRemovalCode = "not-launched"
                          Active = false
                          Released = false
                          Retired = false
                          ProviderExited = true
                          RemovalCode = removal.Code
                          Residue = Directory.EnumerateFileSystemEntries(storeRoot) |> Seq.isEmpty |> not }
                | ProviderArtifactStagingResult.Staged staged ->
                    let restaged =
                        match store.Stage declaration with
                        | ProviderArtifactStagingResult.Staged value -> value
                        | ProviderArtifactStagingResult.Refused failure ->
                            failwithf "CBI32 restaging failed: %s" failure.Code
                    if vector.GetProperty("removeSourceBeforeActivation").GetBoolean() then
                        cbi32DeleteTree sourceRoot
                    match store.Activate(staged, [ "--portable" ]) with
                    | StagedProviderActivation.Refused failure ->
                        return failwithf "CBI32 activation failed: %s" failure.Code
                    | StagedProviderActivation.Launched owner ->
                        let activeRemoval = store.Remove declaration.Identity
                        let resolution, selected, occurrence = prepared ()
                        let! result =
                            ComponentBindingLifecycle.activate
                                resolution
                                selected
                                (runtimeRequest (plan [ occurrence ]))
                                owner.Conversation
                        let memberValue = result.Member
                        let active =
                            result.Failure.IsNone
                            && result.Runtime
                               |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                            && memberValue |> Option.exists _.IsReleased
                        let released = memberValue |> Option.exists _.IsReleased
                        let! retired =
                            task {
                                if active then
                                    let! retirement = memberValue.Value.Retire "CBI32 staged activation completed."
                                    return
                                        match retirement with
                                        | Ok record ->
                                            CompositionStage.token memberValue.Value.Stage = "retired"
                                            && record.ReplacementPermitted
                                        | Error _ -> false
                                else
                                    return false
                            }
                        memberValue |> Option.iter _.Close()
                        let exited = owner.WaitForExit(TimeSpan.FromSeconds 5.0)
                        owner.Dispose()
                        let removal = store.Remove declaration.Identity
                        return
                            { StageCode = "staged"
                              Staged = true
                              Reused = restaged.Reused
                              ActiveRemovalCode = activeRemoval.Code
                              Active = active
                              Released = released
                              Retired = retired
                              ProviderExited = exited
                              RemovalCode = removal.Code
                              Residue = Directory.EnumerateFileSystemEntries(storeRoot) |> Seq.isEmpty |> not }
            finally
                cbi32DeleteTree testRoot
        }

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
                  Scope = None
                  Conversation = conversationFor (List.item 0 handlers) }
            let secondMember =
                { Selection =
                    { selection (positionFor secondaryRequirementId) with
                        Requirement = secondaryRequirementId
                        HostEndpoint = "verification-host-secondary" }
                  Scope = None
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
                            Scope = None
                            Conversation = conversationFor 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member =
                          { Selection = secondSelection
                            Scope = None
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
                            Scope = None
                            Conversation = conversationFor CoolingFixture.contract 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerRequest } ] }
                      { Member =
                          { Selection = secondSelection
                            Scope = None
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
                            Scope = None
                            Conversation = baseConversation (List.item 0 handlers) }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = List.item 0 admitted } ] }
                      { Member =
                          { Selection = secondSelection
                            Scope = None
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
                            Scope = None
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
                            Scope = None
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
    let mediatedRequirementId = RequirementId.create "req.cooling-mediated"
    let mediatedProviderId = DefinitionId.create "def.test.cooling-mediated"
    let mediatedContractId = ContractId.create "brontide.fake.cooling-mediated"
    let mediatorRequirementId = RequirementId.create "req.cooling-mediator"
    let mediatorDefinitionId = DefinitionId.create "def.test.cooling-mediator"
    let mediatorContractId = ContractId.create "brontide.fake.cooling-mediator"

    let positionCatalog =
        [ requirementId, provider, contractId
          secondaryRequirementId, secondaryProvider, secondaryContractId
          tertiaryRequirementId, tertiaryProvider, tertiaryContractId
          mediatorRequirementId, mediatorDefinitionId, mediatorContractId
          mediatedRequirementId, mediatedProviderId, mediatedContractId ]

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
                      Scope = None
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
                      Scope = None
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
                            Scope = None
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

    let grandchildPortId = PortId.create "port.grandchild"
    let grandchildScopeId = RestartScopeId.create "restart.grandchild"

    let withdrawalToken kind =
        match kind with
        | ComponentAttachmentWithdrawalKind.Withdrawn -> "withdrawn"
        | ComponentAttachmentWithdrawalKind.CleanupFailed -> "cleanup-failed"
        | ComponentAttachmentWithdrawalKind.Declined -> "declined"

    /// One position CM2 resolved inside the named Port, with the named lifecycle.
    let portPosition port lifecycle endpoint =
        let single = request (Cardinality.parse "1..1")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let contained =
            { List.exactlyOne consumerDefinition.Requirements with
                ContainingRegion = Some childRegion
                ContainingPort = Some port
                RuntimeAttachment = lifecycle = PortLifecycleMode.RuntimeOpen }
        let resolution =
            { single with
                Definitions =
                    { consumerDefinition with Requirements = [ contained ] }
                    :: (single.Definitions |> List.filter (fun item -> item.Definition <> consumer))
                Ports = [ portEnvelope port contractId lifecycle ] }
            |> FakeGenerationResolver.resolve
        let position =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) ->
                generation.ProviderSets |> List.exactlyOne |> fun item -> List.exactlyOne item.Members
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        resolution, { selection position with HostEndpoint = endpoint }

    /// One attachment beneath the given parent, with everything it needs derived from the spec.
    let attachLevel
        (parent: ComponentGroupAuthorityResult)
        parentScope
        parentGeneration
        (port, scope, generation, lifecycle, suffix, failCleanup, declaredParentGeneration)
        =
        task {
            let resolution, childSelection =
                portPosition port lifecycle (sprintf "%s-host" suffix)
            let handler = CoolingHandler()
            let inner =
                PortableDirectConversation(
                    PortableProviderEndpoint(
                        CoolingFixture.contract,
                        handler,
                        Realization.FixedDirectCall))
                :> IPortableProviderConversation
            let conversation =
                if failCleanup then
                    FailingRetirementConversation inner :> IPortableProviderConversation
                else
                    inner
            let childRelationship = RelationshipRequestId.create (sprintf "relationship.%s" suffix)
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let baseline = providerAuthority policy authorityId
            let admissionRequest =
                { baseline with
                    Request = AdmissionRequestId.create (sprintf "admission.%s" suffix)
                    Relationships =
                      [ { Request = childRelationship
                          ProposedActor = participant
                          Kind = ActorRelationshipKind.ComponentParticipant
                          Evidence = [ authorityEvidence ] } ]
                    Authority =
                      [ { Request = AuthorityRequestId.create (sprintf "authority.%s" suffix)
                          Relationship = childRelationship
                          Capability = capability
                          Target = authorityTarget
                          Operation = operation
                          Scope = authorityScope
                          Unlimited = false } ] }
            let planValue = planFor generation scope [ childSelection.Occurrence ]
            let retainedGeneration =
                GenerationId.create (sprintf "%s-retained" (GenerationId.value generation))
            let baseRequest = runtimeRequestFor planValue retainedGeneration
            let childRequest =
                { baseRequest with
                    ActiveScopes =
                        [ yield
                              { Scope = scope
                                Generation = retainedGeneration
                                Status = RuntimeScopeStatus.ActiveScope }
                          if scope <> parentScope then
                              yield
                                  { Scope = parentScope
                                    Generation = parentGeneration
                                    Status = RuntimeScopeStatus.ActiveScope } ]
                    Child =
                        Some
                            { ParentScope = parentScope
                              ParentGeneration =
                                defaultArg declaredParentGeneration parentGeneration
                              Port = port
                              RuntimeOpen = true
                              Occupied = false
                              ReplacementLifecycleDeclared = false
                              HostAssisted = false
                              InternalReleaseSequence = 0
                              ExportReleaseSequence = 2
                              OuterHostOwnsAdmission = false } }
            let! result =
                ComponentChildActivation.attach
                    resolution
                    parent
                    [ { Member =
                          { Selection = childSelection
                            Scope = None
                            Conversation = conversation }
                        Participants =
                          [ { Mapping =
                                { Occurrence = childSelection.Occurrence
                                  Participant = participant }
                              Request = admissionRequest } ] } ]
                    childRequest
            return result
        }

    let childSpec failCleanup =
        childPortId,
        childScopeId,
        GenerationId.create "gen.child",
        PortLifecycleMode.RuntimeOpen,
        "child",
        failCleanup,
        None

    /// A parent, a child beneath it, and a grandchild beneath that.
    let nestedTree scenario =
        task {
            let! root, _ = childParent false
            let! child = attachLevel root parentScopeId (GenerationId.create "gen.lifecycle") (childSpec false)
            let childActivation = child.Child.Value
            if scenario = "cbi23-05-attachment-beneath-a-retired-parent" then
                for outcome in childActivation.Lifecycle.Value.Members do
                    let! _ = outcome.Member.Retire "retired before the grandchild attaches"
                    ()
            let spec =
                grandchildPortId,
                (if scenario = "cbi23-03-grandchild-scope-is-its-parents" then
                     childScopeId
                 else
                     grandchildScopeId),
                GenerationId.create "gen.grandchild",
                (if scenario = "cbi23-02-grandchild-port-lifecycle-overstated" then
                     PortLifecycleMode.ActivationOpen
                 else
                     PortLifecycleMode.RuntimeOpen),
                "grandchild",
                false,
                (if scenario = "cbi23-04-grandchild-parent-generation-mismatch" then
                     Some(GenerationId.create "gen.other")
                 else
                     None)
            let! result =
                attachLevel childActivation childScopeId (GenerationId.create "gen.child") spec
            let levels =
                [ yield root
                  yield childActivation
                  match result.Child with
                  | Some grandchild when ComponentChildActivation.isAttached result -> yield grandchild
                  | _ -> () ]
            return result, levels
        }

    let withdrawalResult scenario =
        task {
            let! root, _ = childParent false
            let! child =
                attachLevel
                    root
                    parentScopeId
                    (GenerationId.create "gen.lifecycle")
                    (childSpec (scenario = "cbi23-08-cleanup-fails-in-the-child"))
            let childActivation = child.Child.Value
            let! grandchild =
                attachLevel
                    childActivation
                    childScopeId
                    (GenerationId.create "gen.child")
                    (grandchildPortId,
                     grandchildScopeId,
                     GenerationId.create "gen.grandchild",
                     PortLifecycleMode.RuntimeOpen,
                     "grandchild",
                     false,
                     None)
            let levels = [ root; childActivation; grandchild.Child.Value ]
            let given =
                match scenario with
                | "cbi23-07-cascade-from-the-middle" -> [ List.item 1 levels; List.item 2 levels ]
                | "cbi23-09-duplicate-scope" ->
                    [ List.item 0 levels; List.item 1 levels; List.item 2 levels; List.item 2 levels ]
                | _ -> levels
            let! result =
                ComponentAttachmentWithdrawal.withdraw
                    given
                    (sprintf "attachment withdrawal %s" scenario)
            return result, levels
        }

    let attachedToken kind =
        match kind with
        | ComponentAttachedReplacementKind.Replaced -> "replaced"
        | ComponentAttachedReplacementKind.CleanupFailed -> "cleanup-failed"
        | ComponentAttachedReplacementKind.Declined -> "declined"

    /// A successor generation resolving the same two positions the parent activation holds.
    let successorFor () =
        requestForPositions [ requirementId; secondaryRequirementId ] |> FakeGenerationResolver.resolve

    let successorMembers neverReady =
        let resolution = successorFor ()
        let providerSets =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        let policy = groupPolicy providerLocalActor supervisorLocalActor
        let substituted =
            { CoolingFixture.contract with
                Provider = expectProvider "brontide.fake.substituted" }
        [ requirementId; secondaryRequirementId ]
        |> List.mapi (fun index requirement ->
            let position =
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> List.exactlyOne item.Members
            { Member =
                { Selection =
                    { selection position with
                        Requirement = requirement
                        HostEndpoint = sprintf "successor-host-%d" index }
                  Scope = None
                  Conversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            (if neverReady && index = 1 then substituted else CoolingFixture.contract),
                            CoolingHandler(),
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation }
              Participants =
                [ { Mapping =
                      { Occurrence = position.Occurrence
                        Participant = (if index = 0 then participant else supervisor) }
                    Request =
                      if index = 0 then
                          providerAuthority policy authorityId
                      else
                          supervisorAuthority policy auditAuthorityId false } ] })

    let replacementRequest generation wrongScope =
        let resolution = successorFor ()
        let providerSets =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation') -> generation'.ProviderSets
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        let occurrences =
            [ requirementId; secondaryRequirementId ]
            |> List.map (fun requirement ->
                providerSets
                |> List.find (fun item -> item.Requirement = requirement)
                |> fun item -> (List.exactlyOne item.Members).Occurrence)
        let planValue =
            planFor
                generation
                (if wrongScope then RestartScopeId.create "restart.elsewhere" else parentScopeId)
                occurrences
        runtimeRequestFor planValue (GenerationId.create "gen.lifecycle")

    let attachedReplacementResult scenario =
        task {
            let! root, _ = childParent false
            let! child =
                attachLevel
                    root
                    parentScopeId
                    (GenerationId.create "gen.lifecycle")
                    (childPortId,
                     childScopeId,
                     GenerationId.create "gen.child",
                     PortLifecycleMode.RuntimeOpen,
                     "child",
                     scenario = "cbi24-07-cascade-cleanup-fails",
                     None)
            let attached = ResizeArray<ComponentGroupAuthorityResult>()
            attached.Add child.Child.Value
            if
                scenario = "cbi24-02-two-attachments-cascaded-deepest-first"
                || scenario = "cbi24-03-attachment-names-another-parent-generation"
            then
                let! grandchild =
                    attachLevel
                        child.Child.Value
                        childScopeId
                        (GenerationId.create "gen.child")
                        (grandchildPortId,
                         grandchildScopeId,
                         GenerationId.create "gen.grandchild",
                         PortLifecycleMode.RuntimeOpen,
                         "grandchild",
                         false,
                         None)
                attached.Add grandchild.Child.Value
            let supplied =
                match scenario with
                // The grandchild is attached to the child, not to the generation being replaced.
                | "cbi24-03-attachment-names-another-parent-generation" -> [ attached.[1] ]
                | "cbi24-04-supplied-activation-is-not-an-attachment" -> [ root ]
                | _ -> List.ofSeq attached
            let! result =
                ComponentAttachedReplacement.replace
                    (successorFor ())
                    root
                    (successorMembers (scenario = "cbi24-06-replacement-fails-after-the-cascade"))
                    supplied
                    (replacementRequest
                        (GenerationId.create "gen.successor")
                        (scenario = "cbi24-05-scope-mismatch-refused-before-the-cascade"))
                    (sprintf "attached replacement %s" scenario)
            return result, root, List.ofSeq attached
        }

    let mediatedToken kind =
        match kind with
        | ComponentMediatedTranslationKind.Translated -> "translated"
        | ComponentMediatedTranslationKind.Declined -> "declined"

    /// A generation with a mediated position and, separately, a position that resolves the Component
    /// its Mediation is realized as.
    let mediatedResolutionOwning' realization nameComponent declareMediated owns =
        let pair = requestForPositions [ mediatorRequirementId; mediatedRequirementId ]
        let consumerDefinition =
            pair.Definitions |> List.find (fun item -> item.Definition = consumer)
        let requirements =
            consumerDefinition.Requirements
            |> List.map (fun item ->
                if item.Requirement = mediatedRequirementId then
                    { item with
                        // CM2 records a Mediation on a distinct position and ignores it, so exposure
                        // and the declaration are two separate facts a caller can disagree with.
                        Exposure =
                            if declareMediated then
                                ProviderExposure.Mediated
                            else
                                ProviderExposure.Distinct
                        Mediation =
                            Some
                                { Mediation = MediationId.create "mediation.cooling"
                                  Kind = MediationKind.Selection
                                  Realization = realization
                                  Component = (if nameComponent then Some mediatorDefinitionId else None)
                                  OwnsMutableMembership = owns = "recovery"
                                  OwnsResidue = owns = "recovery"
                                  OwnsBackpressure = false
                                  OwnsAuthority = owns = "authority"
                                  OwnsRecovery = owns = "recovery"
                                  OwnsLifecycle = owns = "lifecycle" } }
                else
                    item)
        { pair with
            Definitions =
                { consumerDefinition with Requirements = requirements }
                :: (pair.Definitions |> List.filter (fun item -> item.Definition <> consumer)) }
        |> FakeGenerationResolver.resolve

    let mediatedResolution realization nameComponent declareMediated =
        mediatedResolutionOwning' realization nameComponent declareMediated "none"

    let mediatedResolutionOwning owns =
        mediatedResolutionOwning' MediationRealization.DedicatedComponent true true owns

    let mediatedGeneration () =
        mediatedResolution MediationRealization.DedicatedComponent true true

    let mediatorSelection () =
        let resolution = mediatedGeneration ()
        let position =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) ->
                generation.ProviderSets
                |> List.find (fun item -> item.Requirement = mediatorRequirementId)
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        { selection (List.exactlyOne position.Members) with
            Requirement = mediatorRequirementId
            HostEndpoint = "mediator-host" }

    let mediatedPositionOf (resolution: ResolutionOutcome) =
        match resolution with
        | ResolutionOutcome.Resolved(_, generation) ->
            generation.ProviderSets
            |> List.find (fun item -> item.Requirement = mediatedRequirementId)
        | outcome -> failwithf "Expected a resolved generation, got %A." outcome

    let mediatedTranslation scenario =
        let resolution =
            match scenario with
            | "cbi25-03-mediation-realized-by-the-host" ->
                mediatedResolution MediationRealization.StaticHost false true
            | "cbi25-04-dedicated-component-not-named" ->
                mediatedResolution MediationRealization.DedicatedComponent false true
            // A host-realized Mediation is the host's work whatever Component it names.
            | "cbi25-08-static-host-naming-a-component" ->
                mediatedResolution MediationRealization.StaticHost true true
            | "cbi25-09-distinct-position-declaring-a-mediation" ->
                mediatedResolution MediationRealization.DedicatedComponent true false
            | _ -> mediatedGeneration ()
        let mediated =
            match scenario with
            | "cbi25-02-position-is-not-mediated" -> mediatorRequirementId
            | "cbi25-07-mediated-requirement-not-resolved" -> RequirementId.create "req.absent"
            | _ -> mediatedRequirementId
        let position = mediatedPositionOf resolution
        let mediator =
            match scenario with
            | "cbi25-05-mapping-names-a-mediated-member" ->
                let memberValue = List.head position.Members
                { mediatorSelection () with
                    Definition = memberValue.Definition
                    Occurrence = memberValue.Occurrence }
            | "cbi25-06-mediator-occurrence-not-resolved" ->
                { mediatorSelection () with
                    Occurrence = OccurrenceId.create "occ.not-resolved" }
            | _ -> mediatorSelection ()
        ComponentMediatedBinding.translate
            resolution
            { MediatedRequirement = mediated; Mediator = mediator }

    let mediatorToken kind =
        match kind with
        | ComponentMediatorAuthorityKind.Admitted -> "admitted"
        | ComponentMediatorAuthorityKind.Declined -> "declined"

    let mediatorParticipant revoked =
        let policy = groupPolicy providerLocalActor supervisorLocalActor
        let baseline = providerAuthority policy authorityId
        { Mapping =
            { Occurrence = (mediatorSelection ()).Occurrence
              Participant = participant }
          Request = (if revoked then revokedRequest baseline else baseline) }

    let mediatorAuthority scenario =
        let resolution =
            match scenario with
            | "cbi26-02-mediation-owns-authority" -> mediatedResolutionOwning "authority"
            | "cbi26-03-mediation-owns-lifecycle" -> mediatedResolutionOwning "lifecycle"
            | "cbi26-04-mediation-owns-recovery-and-residue" -> mediatedResolutionOwning "recovery"
            | _ -> mediatedGeneration ()
        let mediator =
            if scenario = "cbi26-05-translation-refused" then
                { mediatorSelection () with
                    Definition = DefinitionId.create "def.test.not-the-mediator" }
            else
                mediatorSelection ()
        let participantRequest =
            match scenario with
            | "cbi26-06-authority-denied" -> mediatorParticipant true
            | "cbi26-07-request-names-another-occurrence" ->
                { mediatorParticipant false with
                    Mapping =
                        { Occurrence = OccurrenceId.create "occ.elsewhere"
                          Participant = participant } }
            | _ -> mediatorParticipant false
        ComponentMediatorAuthority.admit
            resolution
            { MediatedRequirement = mediatedRequirementId; Mediator = mediator }
            [ participantRequest ]
            (runtimeRequest (plan [ (mediatorSelection ()).Occurrence ]))

    /// A second provider of the primary contract, so one position can resolve two members.
    let standbyProvider = DefinitionId.create "def.test.cooling-standby"

    /// One position whose cardinality is not 1..1, drawing on as many candidate providers as it is
    /// given.
    ///
    /// CM2 fills a Provider Set to its declared minimum and then takes explicit preselections up to
    /// its maximum, so how many members a wide position resolves is a fact about the request rather
    /// than about the declared bound.
    let wideResolution cardinality candidates preselect exposure declareMediation =
        let single = request cardinality
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let providerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = provider)
        let candidate = List.exactlyOne single.Candidates
        let requirement =
            { List.exactlyOne consumerDefinition.Requirements with
                Exposure = exposure
                Mediation =
                    if exposure <> ProviderExposure.Distinct || declareMediation then
                        Some
                            { Mediation = MediationId.create "mediation.cooling-wide"
                              Kind = MediationKind.Selection
                              Realization = MediationRealization.StaticHost
                              Component = None
                              OwnsMutableMembership = false
                              OwnsResidue = false
                              OwnsBackpressure = false
                              OwnsAuthority = false
                              OwnsRecovery = false
                              OwnsLifecycle = false }
                    else
                        None }
        { single with
            Definitions =
                [ { consumerDefinition with Requirements = [ requirement ] }
                  providerDefinition
                  { providerDefinition with Definition = standbyProvider } ]
            Candidates =
                match candidates with
                | 0 -> []
                | 1 -> [ candidate ]
                | _ -> [ candidate; { candidate with Definition = standbyProvider } ]
            PreselectedProviders =
                if preselect then
                    [ { Requirement = requirementId; Definition = standbyProvider } ]
                else
                    [] }
        |> FakeGenerationResolver.resolve

    let widePosition (resolution: ResolutionOutcome) =
        match resolution with
        | ResolutionOutcome.Resolved(_, generation) ->
            generation.ProviderSets |> List.find (fun item -> item.Requirement = requirementId)
        | outcome -> failwithf "Expected a resolved generation, got %A." outcome

    let wideScope index =
        match
            Brontide.Minimal.Binding.Portable.BindingScopeId.tryCreate (sprintf "scope.cooling-%d" index)
        with
        | Ok scope -> scope
        | Error failure -> failwithf "Expected a portable binding scope, got %A." failure

    let namedScope name =
        match Brontide.Minimal.Binding.Portable.BindingScopeId.tryCreate name with
        | Ok scope -> scope
        | Error failure -> failwithf "Expected a portable binding scope, got %A." failure

    let wideSelection (resolution: ResolutionOutcome) =
        { Requirement = requirementId
          Members =
            (widePosition resolution).Members
            |> List.mapi (fun index memberValue ->
                { Scope = wideScope index
                  Selection =
                    { selection memberValue with
                        HostEndpoint = sprintf "wide-host-%d" index } }) }

    let wideToken kind =
        match kind with
        | ComponentProviderSetTranslationKind.Translated -> "translated"
        | ComponentProviderSetTranslationKind.Unfilled -> "unfilled"
        | ComponentProviderSetTranslationKind.Declined -> "declined"

    let wideCardinalityText (cardinality: Cardinality) =
        match cardinality.Maximum with
        | Some maximum -> sprintf "%d..%d" cardinality.Minimum maximum
        | None -> sprintf "%d..*" cardinality.Minimum

    let wideTranslation scenario =
        let resolution =
            match scenario with
            | "cbi27-02-optional-capacity-unfilled" ->
                wideResolution (Cardinality.parse "1..3") 1 false ProviderExposure.Distinct false
            | "cbi27-03-preselected-optional-member" ->
                wideResolution (Cardinality.parse "1..2") 2 true ProviderExposure.Distinct false
            | "cbi27-04-position-resolved-empty"
            | "cbi27-16-unfilled-position-supplied" ->
                wideResolution (Cardinality.parse "0..2") 0 false ProviderExposure.Distinct false
            | "cbi27-05-position-is-one-to-one" -> resolve (Cardinality.parse "1..1")
            | "cbi27-06-position-mediated" ->
                wideResolution (Cardinality.parse "2..2") 2 false ProviderExposure.Mediated false
            | "cbi27-07-distinct-position-declaring-a-mediation" ->
                wideResolution (Cardinality.parse "2..2") 2 false ProviderExposure.Distinct true
            | _ -> wideResolution (Cardinality.parse "2..2") 2 false ProviderExposure.Distinct false
        let supplied = wideSelection resolution
        let members = supplied.Members
        let entry index = List.item index members
        let selectionValue =
            match scenario with
            | "cbi27-08-member-not-supplied" -> { supplied with Members = [ entry 0 ] }
            | "cbi27-09-member-not-resolved" ->
                { supplied with
                    Members =
                        members
                        @ [ { Scope = namedScope "scope.cooling-extra"
                              Selection =
                                { (entry 0).Selection with
                                    Occurrence = OccurrenceId.create "occ.not-resolved" } } ] }
            | "cbi27-10-member-supplied-twice" ->
                { supplied with
                    Members = [ entry 0; { entry 0 with Scope = namedScope "scope.cooling-again" } ] }
            | "cbi27-11-members-share-a-binding-scope" ->
                { supplied with
                    Members = [ entry 0; { entry 1 with Scope = (entry 0).Scope } ] }
            | "cbi27-12-member-mapping-mismatched" ->
                { supplied with
                    Members =
                        [ entry 0
                          { entry 1 with Selection = { (entry 1).Selection with Definition = provider } } ] }
            | "cbi27-13-member-endpoint-invalid" ->
                { supplied with
                    Members =
                        [ entry 0
                          { entry 1 with
                              Selection = { (entry 1).Selection with HostEndpoint = "  " } } ] }
            | "cbi27-14-member-requirement-mismatched" ->
                { supplied with
                    Members =
                        [ entry 0
                          { entry 1 with
                              Selection =
                                { (entry 1).Selection with Requirement = secondaryRequirementId } } ] }
            | "cbi27-15-position-not-resolved" ->
                { supplied with Requirement = RequirementId.create "req.absent" }
            | "cbi27-16-unfilled-position-supplied" ->
                { supplied with
                    Members =
                        [ { Scope = wideScope 0
                            Selection =
                              selection
                                  { Definition = provider
                                    Occurrence = OccurrenceId.create "occ.not-resolved"
                                    Source = None
                                    Publisher = PublisherId.create "pub.test"
                                    Package = None
                                    Retained = false
                                    Evidence = []
                                    Authority = []
                                    FailureDomain = "failure.test"
                                    AttachmentNode = None } } ] }
            | _ -> supplied
        ComponentProviderSetBinding.translate resolution selectionValue

    /// Two ordinary `1..1` positions, which the pair fixture resolves in one CM binding scope.
    let fannedOutPair () =
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
            let entryFor index requirement endpoint =
                { Selection =
                    { selection (positionFor requirement) with
                        Requirement = requirement
                        HostEndpoint = endpoint }
                  Scope = None
                  Conversation =
                    PortableDirectConversation(
                        PortableProviderEndpoint(
                            CoolingFixture.contract,
                            List.item index handlers,
                            Realization.FixedDirectCall))
                    :> IPortableProviderConversation }
            let first = entryFor 0 requirementId "pair-host-primary"
            let second = entryFor 1 secondaryRequirementId "pair-host-secondary"
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let! result =
                ComponentGroupAuthority.activate
                    resolution
                    [ { Member = first
                        Participants =
                          [ { Mapping =
                                { Occurrence = first.Selection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member = second
                        Participants =
                          [ { Mapping =
                                { Occurrence = second.Selection.Occurrence
                                  Participant = supervisor }
                              Request = supervisorAuthority policy auditAuthorityId false } ] } ]
                    (runtimeRequest (plan [ first.Selection.Occurrence; second.Selection.Occurrence ]))
            return result, handlers
        }

    /// A wide `2..2` position, optionally beside an ordinary `1..1` one.
    let wideActivationResolution withOrdinary =
        let single = request (Cardinality.parse "2..2")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let providerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = provider)
        let candidate = List.exactlyOne single.Candidates
        let wide = List.exactlyOne consumerDefinition.Requirements
        let ordinary =
            { wide with
                Requirement = secondaryRequirementId
                Contract = secondaryContractId
                Cardinality = Cardinality.parse "1..1" }
        { single with
            Definitions =
                [ { consumerDefinition with
                      Requirements = (if withOrdinary then [ wide; ordinary ] else [ wide ]) }
                  providerDefinition
                  { providerDefinition with Definition = standbyProvider }
                  { providerDefinition with
                      Definition = secondaryProvider
                      Provides = [ { Contract = secondaryContractId; Version = version } ] } ]
            Candidates =
                [ candidate
                  { candidate with Definition = standbyProvider }
                  { candidate with
                      Definition = secondaryProvider
                      Provides = [ { Contract = secondaryContractId; Version = version } ] } ] }
        |> FakeGenerationResolver.resolve

    /// One party per member, because two members of one position are two occurrences and CM5 admits
    /// against an occurrence.
    let fannedOutParticipant index occurrence policy scenario =
        match index with
        | 0 ->
            { Mapping = { Occurrence = occurrence; Participant = participant }
              Request = providerAuthority policy authorityId }
        | 1 ->
            { Mapping = { Occurrence = occurrence; Participant = supervisor }
              Request =
                supervisorAuthority
                    policy
                    reportAuthorityId
                    (scenario = "cbi28-08-one-member-denied") }
        | _ ->
            { Mapping = { Occurrence = occurrence; Participant = observer }
              Request = observerRequest policy }

    let fannedOutActivation scenario =
        task {
            let withOrdinary =
                scenario = "cbi28-02-wide-position-beside-an-ordinary-one"
                || scenario = "cbi28-05-ordinary-member-with-a-scope"
            let resolution = wideActivationResolution withOrdinary
            let position = widePosition resolution
            let wideMembers =
                if scenario = "cbi28-03-member-missing-from-the-activation" then
                    position.Members |> List.truncate 1
                else
                    position.Members
            let handlers = [ CoolingHandler(); CoolingHandler(); CoolingHandler() ]
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let wideEntries =
                wideMembers
                |> List.mapi (fun index memberValue ->
                    // The second member's provider is substituted where the vector needs one member
                    // of the position never to reach Ready.
                    let document =
                        if scenario = "cbi28-07-one-member-never-ready" && index = 1 then
                            substituted
                        else
                            CoolingFixture.contract
                    let scope =
                        if scenario = "cbi28-06-members-share-a-scope" then
                            namedScope "scope.cooling-0"
                        else
                            wideScope index
                    { Selection =
                        { selection memberValue with
                            HostEndpoint = sprintf "fanned-host-%d" index }
                      Scope =
                        (if scenario = "cbi28-04-member-without-a-scope" && index = 1 then
                             None
                         else
                             Some scope)
                      Conversation =
                        PortableDirectConversation(
                            PortableProviderEndpoint(
                                document,
                                List.item index handlers,
                                Realization.FixedDirectCall))
                        :> IPortableProviderConversation })
            let ordinaryEntries =
                if not withOrdinary then
                    []
                else
                    let ordinary =
                        match resolution with
                        | ResolutionOutcome.Resolved(_, generation) ->
                            generation.ProviderSets
                            |> List.find (fun item -> item.Requirement = secondaryRequirementId)
                        | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                    [ { Selection =
                          { selection (List.head ordinary.Members) with
                              Requirement = secondaryRequirementId
                              HostEndpoint = "fanned-host-ordinary" }
                        Scope =
                          (if scenario = "cbi28-05-ordinary-member-with-a-scope" then
                               Some(namedScope "scope.cooling-ordinary")
                           else
                               None)
                        Conversation =
                          PortableDirectConversation(
                              PortableProviderEndpoint(
                                  CoolingFixture.contract,
                                  List.item 2 handlers,
                                  Realization.FixedDirectCall))
                          :> IPortableProviderConversation } ]
            let entries = wideEntries @ ordinaryEntries
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let participants =
                entries
                |> List.mapi (fun index entry ->
                    { Member = entry
                      Participants =
                        [ fannedOutParticipant index entry.Selection.Occurrence policy scenario ] })
            let! result =
                ComponentGroupAuthority.activate
                    resolution
                    participants
                    (runtimeRequest (plan (entries |> List.map _.Selection.Occurrence)))
            return result, handlers
        }

    let wideChildResolution () =
        let single = request (Cardinality.parse "2..2")
        let consumerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = consumer)
        let providerDefinition =
            single.Definitions |> List.find (fun item -> item.Definition = provider)
        let candidate = List.exactlyOne single.Candidates
        let contained =
            { List.exactlyOne consumerDefinition.Requirements with
                ContainingRegion = Some childRegion
                ContainingPort = Some childPortId
                RuntimeAttachment = true }
        { single with
            Definitions =
                [ { consumerDefinition with Requirements = [ contained ] }
                  providerDefinition
                  { providerDefinition with Definition = standbyProvider } ]
            Candidates = [ candidate; { candidate with Definition = standbyProvider } ]
            Ports =
                [ { portEnvelope childPortId contractId PortLifecycleMode.RuntimeOpen with
                      Cardinality = Cardinality.parse "2..2" } ] }
        |> FakeGenerationResolver.resolve

    let wideChildActivation scenario =
        task {
            let! parent, parentHandlers = childParent false
            let resolution = wideChildResolution ()
            let position = widePosition resolution
            let selected =
                if scenario = "cbi29-02-member-omitted" then
                    position.Members |> List.truncate 1
                else
                    position.Members
            let childHandlers = selected |> List.map (fun _ -> CoolingHandler())
            let substituted =
                { CoolingFixture.contract with
                    Provider = expectProvider "brontide.fake.substituted" }
            let entries =
                selected
                |> List.mapi (fun index memberValue ->
                    let document =
                        if scenario = "cbi29-05-member-never-ready" && index = 1 then
                            substituted
                        else
                            CoolingFixture.contract
                    { Selection =
                        { selection memberValue with
                            HostEndpoint = sprintf "wide-child-host-%d" index }
                      Scope =
                        if scenario = "cbi29-03-member-without-portable-scope" && index = 1 then
                            None
                        else
                            Some(
                                namedScope (
                                    if scenario = "cbi29-04-portable-scope-reused" then
                                        "scope.child-member-0"
                                    else
                                        sprintf "scope.child-member-%d" index))
                      Conversation =
                        PortableDirectConversation(
                            PortableProviderEndpoint(
                                document,
                                List.item index childHandlers,
                                Realization.FixedDirectCall))
                        :> IPortableProviderConversation })
            let policy = groupPolicy providerLocalActor supervisorLocalActor
            let participants =
                entries
                |> List.mapi (fun index entry ->
                    { Member = entry
                      Participants =
                        [ fannedOutParticipant
                              index
                              entry.Selection.Occurrence
                              policy
                              (if scenario = "cbi29-06-member-authority-denied" then
                                   "cbi28-08-one-member-denied"
                               else
                                   scenario) ] })
            let planValue =
                planFor
                    (GenerationId.create "gen.child-wide")
                    childScopeId
                    (entries |> List.map _.Selection.Occurrence)
            let baseRequest = runtimeRequestFor planValue (GenerationId.create "gen.child-retained")
            let requestValue =
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
                              Port =
                                if scenario = "cbi29-07-attachment-names-another-port" then
                                    PortId.create "port.other"
                                else
                                    childPortId
                              RuntimeOpen = true
                              Occupied = false
                              ReplacementLifecycleDeclared = false
                              HostAssisted = false
                              InternalReleaseSequence = 0
                              ExportReleaseSequence = 2
                              OuterHostOwnsAdmission = false } }
            let! result =
                ComponentChildActivation.attach resolution parent participants requestValue
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
                  Scope = None
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
                            Scope = None
                            Conversation = conversationFor 0 }
                        Participants =
                          [ { Mapping =
                                { Occurrence = firstSelection.Occurrence
                                  Participant = participant }
                              Request = providerAuthority policy authorityId } ] }
                      { Member =
                          { Selection = secondSelection
                            Scope = None
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
                          Scope = None
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
                        Scope = None
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
    member _.``shared CBI23 vectors nest a child beneath a child``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi23-nested-child-port-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI23 vector identity must be a string"
                    | value -> value
                let! result, levels = nestedTree scenario
                let released =
                    levels
                    |> List.sumBy (fun level ->
                        level.Lifecycle
                        |> Option.map (fun lifecycle ->
                            lifecycle.Members |> List.filter _.Member.IsReleased |> List.length)
                        |> Option.defaultValue 0)
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
                        levels.Length,
                        Is.EqualTo(vector.GetProperty("expectedDepth").GetInt32()),
                        scenario)
                    Assert.That(
                        released,
                        Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()),
                        scenario))
        }

    [<Test>]
    member _.``shared CBI23 withdrawals retire an attachment tree deepest first``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi23-nested-child-port-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("withdrawals").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI23 withdrawal identity must be a string"
                    | value -> value
                let! result, levels = withdrawalResult scenario
                let expectedScopes =
                    vector.GetProperty("expectedRetiredScopes").EnumerateArray()
                    |> Seq.map (fun item ->
                        match item.GetString() with
                        | null -> failwith "CBI23 retired scopes must be strings"
                        | value -> value)
                    |> List.ofSeq
                let releasedAfter =
                    levels
                    |> List.sumBy (fun level ->
                        level.Lifecycle
                        |> Option.map (fun lifecycle ->
                            lifecycle.Members |> List.filter _.Member.IsReleased |> List.length)
                        |> Option.defaultValue 0)
                multiple (fun () ->
                    Assert.That(
                        withdrawalToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Retired
                        |> List.map (fun item -> RestartScopeId.value item.Scope)
                        |> String.concat ", ",
                        Is.EqualTo(expectedScopes |> String.concat ", "),
                        sprintf "%s: the cascade order is deepest first." scenario)
                    Assert.That(
                        releasedAfter,
                        Is.EqualTo(vector.GetProperty("expectedReleasedAfter").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Kind <> ComponentAttachmentWithdrawalKind.Declined
                        || result.Retired.IsEmpty,
                        Is.True,
                        sprintf "%s: a declined withdrawal retires nothing." scenario))
        }

    [<Test>]
    member _.``C1 a child activation is an ordinary parent``() =
        task {
            let! attached, _ = nestedTree "cbi23-01-grandchild-attached"
            let! overstated, overstatedLevels = nestedTree "cbi23-02-grandchild-port-lifecycle-overstated"
            let! scopeResult, _ = nestedTree "cbi23-03-grandchild-scope-is-its-parents"
            let! generation, _ = nestedTree "cbi23-04-grandchild-parent-generation-mismatch"
            multiple (fun () ->
                Assert.That(ComponentChildActivation.isAttached attached, Is.True)
                Assert.That(
                    attached.Port |> Option.map PortId.value,
                    Is.EqualTo(Some(PortId.value grandchildPortId)),
                    "The grandchild names the Port its own position was resolved into.")
                Assert.That(
                    overstated.Code,
                    Is.EqualTo "port-lifecycle-overstated",
                    "CBI22's envelope rule applies at the second level unchanged.")
                Assert.That(scopeResult.Code, Is.EqualTo "child-scope-not-distinct")
                Assert.That(generation.Code, Is.EqualTo "parent-generation-mismatch")
                Assert.That(
                    (List.item 1 overstatedLevels).Lifecycle.Value.Members
                    |> List.forall _.Member.IsReleased,
                    Is.True,
                    "And a refusal at the second level leaves the first child released."))
        }

    [<Test>]
    member _.``C2 depth is not bounded by this slice``() =
        task {
            let! attached, levels = nestedTree "cbi23-01-grandchild-attached"
            let! great =
                attachLevel
                    (List.item 2 levels)
                    grandchildScopeId
                    (GenerationId.create "gen.grandchild")
                    (PortId.create "port.great-grandchild",
                     RestartScopeId.create "restart.great-grandchild",
                     GenerationId.create "gen.great-grandchild",
                     PortLifecycleMode.RuntimeOpen,
                     "great",
                     false,
                     None)
            multiple (fun () ->
                Assert.That(ComponentChildActivation.isAttached attached, Is.True)
                Assert.That(
                    ComponentChildActivation.isAttached great,
                    Is.True,
                    "A fourth level is admitted on exactly the terms the second was.")
                Assert.That(great.Code, Is.EqualTo "child-attached")
                Assert.That(
                    levels
                    |> List.forall (fun level ->
                        level.Lifecycle.Value.Members |> List.forall _.Member.IsReleased),
                    Is.True))
        }

    [<Test>]
    member _.``C3 the attachment relation is derived and checked``() =
        task {
            let! duplicate, levels = withdrawalResult "cbi23-09-duplicate-scope"
            let! cascade, _ = withdrawalResult "cbi23-06-cascade-deepest-first"
            multiple (fun () ->
                Assert.That(duplicate.Code, Is.EqualTo "scope-not-distinct")
                Assert.That(
                    duplicate.Retired,
                    Is.Empty,
                    "Every refusal of the relation itself retires nothing.")
                Assert.That(
                    levels
                    |> List.forall (fun level ->
                        level.Lifecycle.Value.Members |> List.forall _.Member.IsReleased),
                    Is.True)
                // The relation is read from each activation rather than declared: the middle level
                // knows its parent because CM4 recorded the attachment, not because the caller said so.
                Assert.That(
                    cascade.Retired
                    |> List.map (fun item -> RestartScopeId.value item.Scope)
                    |> String.concat ", ",
                    Is.EqualTo "restart.grandchild, restart.child, restart.lifecycle"))
        }

    [<Test>]
    member _.``C4 a child is retired before the parent whose Port it occupies``() =
        task {
            let! cascade, _ = withdrawalResult "cbi23-06-cascade-deepest-first"
            let order = cascade.Retired |> List.map (fun item -> RestartScopeId.value item.Scope)
            multiple (fun () ->
                Assert.That(ComponentAttachmentWithdrawal.isWithdrawn cascade, Is.True)
                Assert.That(
                    List.findIndex ((=) "restart.grandchild") order,
                    Is.LessThan(List.findIndex ((=) "restart.child") order),
                    "The grandchild goes before the child whose Port it occupies.")
                Assert.That(
                    List.findIndex ((=) "restart.child") order,
                    Is.LessThan(List.findIndex ((=) "restart.lifecycle") order),
                    "And the child before the parent whose Port it occupies."))
        }

    [<Test>]
    member _.``C5 the root can only order what it is given``() =
        task {
            let! partial, levels = withdrawalResult "cbi23-07-cascade-from-the-middle"
            multiple (fun () ->
                Assert.That(ComponentAttachmentWithdrawal.isWithdrawn partial, Is.True)
                Assert.That(
                    partial.Retired
                    |> List.map (fun item -> RestartScopeId.value item.Scope)
                    |> String.concat ", ",
                    Is.EqualTo "restart.grandchild, restart.child",
                    "The outcome names exactly the scopes it retired.")
                Assert.That(
                    (List.item 0 levels).Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "A parent the caller did not name is left running, which is visible by absence."))
        }

    [<Test>]
    member _.``C6 an attachment beneath a retired parent is refused``() =
        task {
            let! refused, levels = nestedTree "cbi23-05-attachment-beneath-a-retired-parent"
            multiple (fun () ->
                Assert.That(
                    refused.Kind,
                    Is.EqualTo ComponentChildActivationKind.ParentUnavailable)
                Assert.That(refused.Child, Is.EqualTo None)
                Assert.That(
                    (List.item 1 levels).Lifecycle.Value.Members
                    |> List.forall (fun item ->
                        CompositionStage.token item.Member.Stage = "retired"),
                    Is.True,
                    "Its parent is gone, and CBI22's own precondition is what refuses it."))
        }

    [<Test>]
    member _.``C7 a cleanup failure is named and restores nothing``() =
        task {
            let! result, levels = withdrawalResult "cbi23-08-cleanup-fails-in-the-child"
            multiple (fun () ->
                Assert.That(
                    result.Kind,
                    Is.EqualTo ComponentAttachmentWithdrawalKind.CleanupFailed)
                Assert.That(result.Reason, Does.Contain "withdraw-refused")
                Assert.That(
                    result.Retired
                    |> List.map (fun item -> RestartScopeId.value item.Scope)
                    |> String.concat ", ",
                    Is.EqualTo "restart.grandchild, restart.child, restart.lifecycle",
                    "The cascade continues past the failure rather than stopping.")
                Assert.That(
                    result.Retired |> List.filter (fun item -> item.Cleanup.IsSome) |> List.length,
                    Is.EqualTo 1,
                    "And the failure is reported against the scope it happened in.")
                Assert.That(
                    levels
                    |> List.exists (fun level ->
                        level.Lifecycle.Value.Members |> List.exists _.Member.IsReleased),
                    Is.False,
                    "Nothing is returned to released."))
        }

    [<Test>]
    member _.``C8 nesting adds no grant and leaves the earlier slices alone``() =
        task {
            let! _, levels = nestedTree "cbi23-01-grandchild-attached"
            let grants =
                levels
                |> List.collect (fun level ->
                    level.Grants |> List.map (fun item -> CapabilityGrantId.value item.Grant))
            multiple (fun () ->
                Assert.That(
                    (grants |> List.distinct).Length,
                    Is.EqualTo grants.Length,
                    "Each level's authority is its own request, so no grant identity is shared.")
                Assert.That(
                    (List.item 2 levels).Lifecycle.Value.Runtime.Value.Observation.Child
                    |> Option.map (fun item -> RestartScopeId.value item.ParentScope),
                    Is.EqualTo(Some(RestartScopeId.value childScopeId)),
                    "The grandchild's attachment names the child's scope, not the root's.")
                Assert.That(
                    (List.item 0 levels).Lifecycle.Value.Runtime.Value.Observation.Child,
                    Is.EqualTo None,
                    "And the root is not itself an attachment."))
        }

    [<Test>]
    member _.``shared CBI24 vectors stand attachments down before the cutover``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi24-attached-replacement-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI24 vector identity must be a string"
                    | value -> value
                let! result, _, attached = attachedReplacementResult scenario
                let successorReleased =
                    result.Replacement
                    |> Option.bind _.Successor
                    |> Option.bind _.Lifecycle
                    |> Option.map (fun lifecycle ->
                        lifecycle.Members |> List.filter _.Member.IsReleased |> List.length)
                    |> Option.defaultValue 0
                let attachmentsReleased =
                    attached
                    |> List.sumBy (fun item ->
                        item.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length)
                multiple (fun () ->
                    Assert.That(
                        attachedToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Cascaded.Length,
                        Is.EqualTo(vector.GetProperty("expectedCascaded").GetInt32()),
                        scenario)
                    Assert.That(
                        successorReleased,
                        Is.EqualTo(vector.GetProperty("expectedSuccessorReleased").GetInt32()),
                        scenario)
                    Assert.That(
                        attachmentsReleased,
                        Is.EqualTo(vector.GetProperty("expectedAttachmentsReleased").GetInt32()),
                        scenario)
                    // Nothing is established while an attachment is still up.
                    Assert.That(
                        successorReleased = 0 || attachmentsReleased = 0,
                        Is.True,
                        sprintf
                            "%s: no successor member is released while an attachment is."
                            scenario)
                    Assert.That(
                        result.Cascaded.IsEmpty || attachmentsReleased = 0,
                        Is.True,
                        sprintf "%s: a cascade that ran left nothing attached and released." scenario))
        }

    [<Test>]
    member _.``C1 the operation takes the generation and its attachments together``() =
        task {
            let! foreignGeneration, _, foreignAttached =
                attachedReplacementResult "cbi24-03-attachment-names-another-parent-generation"
            let! notAttached, retained, _ =
                attachedReplacementResult "cbi24-04-supplied-activation-is-not-an-attachment"
            let! scopeResult, _, _ =
                attachedReplacementResult "cbi24-05-scope-mismatch-refused-before-the-cascade"
            multiple (fun () ->
                Assert.That(foreignGeneration.Code, Is.EqualTo "attachment-not-beneath-retained")
                Assert.That(notAttached.Code, Is.EqualTo "attachment-not-beneath-retained")
                Assert.That(
                    scopeResult.Code,
                    Is.EqualTo "restart-scope-mismatch",
                    "A replacement that was never going to cut over does not cost the attachments their lives.")
                Assert.That(
                    [ foreignGeneration; notAttached; scopeResult ]
                    |> List.forall (fun item -> item.Cascaded.IsEmpty),
                    Is.True,
                    "Every refusal before the cascade retires nothing.")
                Assert.That(
                    foreignAttached
                    |> List.forall (fun item ->
                        item.Lifecycle.Value.Members |> List.forall _.Member.IsReleased),
                    Is.True)
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True))
        }

    [<Test>]
    member _.``C2 the attachments are stood down before the cutover``() =
        task {
            let! result, retained, attached =
                attachedReplacementResult "cbi24-02-two-attachments-cascaded-deepest-first"
            multiple (fun () ->
                Assert.That(ComponentAttachedReplacement.isReplaced result, Is.True)
                Assert.That(
                    result.Cascaded
                    |> List.map (fun item -> RestartScopeId.value item.Scope)
                    |> String.concat ", ",
                    Is.EqualTo "restart.grandchild, restart.child",
                    "The cascade is CBI23's, deepest first.")
                Assert.That(
                    attached
                    |> List.forall (fun item ->
                        item.Lifecycle.Value.Members
                        |> List.forall (fun member' ->
                            CompositionStage.token member'.Member.Stage = "retired")),
                    Is.True,
                    "Every attachment is down before the successor is established.")
                Assert.That(
                    result.Replacement.Value.Successor.Value.Lifecycle.Value.Members
                    |> List.forall _.Member.IsReleased,
                    Is.True)
                Assert.That(
                    retained.Lifecycle.Value.Members
                    |> List.forall (fun item -> CompositionStage.token item.Member.Stage = "retired"),
                    Is.True,
                    "And the retained members go after the cutover, as CBI19 retires them."))
        }

    [<Test>]
    member _.``C3 a failed replacement does not restore the attachments``() =
        task {
            let! result, retained, attached =
                attachedReplacementResult "cbi24-06-replacement-fails-after-the-cascade"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentAttachedReplacementKind.Declined)
                Assert.That(result.Code, Is.EqualTo "successor-establishment-refused")
                Assert.That(
                    result.Cascaded.Length,
                    Is.EqualTo 1,
                    "The outcome still names every scope the cascade retired.")
                Assert.That(
                    attached
                    |> List.forall (fun item ->
                        item.Lifecycle.Value.Members
                        |> List.forall (fun member' ->
                            CompositionStage.token member'.Member.Stage = "retired")),
                    Is.True,
                    "They are not restored.")
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The retained generation keeps serving, as CBI19 guarantees."))
        }

    [<Test>]
    member _.``C4 a child reattaches to the successor as an ordinary attachment``() =
        task {
            let! result, _, _ = attachedReplacementResult "cbi24-01-attachment-stood-down-then-replaced"
            let successor = result.Replacement.Value.Successor.Value
            let! beneathSuccessor =
                attachLevel
                    successor
                    parentScopeId
                    (GenerationId.create "gen.successor")
                    (childPortId,
                     childScopeId,
                     GenerationId.create "gen.child-again",
                     PortLifecycleMode.RuntimeOpen,
                     "reattached",
                     false,
                     None)
            let! beneathRetained =
                attachLevel
                    successor
                    parentScopeId
                    (GenerationId.create "gen.lifecycle")
                    (childPortId,
                     RestartScopeId.create "restart.child-stale",
                     GenerationId.create "gen.child-stale",
                     PortLifecycleMode.RuntimeOpen,
                     "stale",
                     false,
                     None)
            multiple (fun () ->
                Assert.That(
                    ComponentChildActivation.isAttached beneathSuccessor,
                    Is.True,
                    "Standing the child up again is CBI22's attach naming the successor.")
                Assert.That(
                    beneathRetained.Code,
                    Is.EqualTo "parent-generation-mismatch",
                    "And one naming the generation that was replaced is refused by CBI22's own check."))
        }

    [<Test>]
    member _.``C5 an attachment the caller omits is not detected``() =
        task {
            let! root, _ = childParent false
            let! child =
                attachLevel
                    root
                    parentScopeId
                    (GenerationId.create "gen.lifecycle")
                    (childPortId,
                     childScopeId,
                     GenerationId.create "gen.child",
                     PortLifecycleMode.RuntimeOpen,
                     "child",
                     false,
                     None)
            let! orphaning =
                ComponentGroupReplacement.replace
                    (successorFor ())
                    root
                    (successorMembers false)
                    (replacementRequest (GenerationId.create "gen.successor") false)
                    "replacement that was never told about the child"
            let childActivation = child.Child.Value
            multiple (fun () ->
                Assert.That(
                    ComponentGroupReplacement.isReplaced orphaning,
                    Is.True,
                    "CBI19 replaces the generation without being able to see what is attached beneath it.")
                Assert.That(
                    childActivation.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The child is still running, attached to a generation that is no longer active anywhere.")
                Assert.That(
                    childActivation.Lifecycle.Value.Runtime.Value.Observation.Child
                    |> Option.map (fun item -> GenerationId.value item.ParentGeneration),
                    Is.EqualTo(Some "gen.lifecycle"),
                    "Its recorded parent generation is the replaced one, and nothing will look again."))
        }

    [<Test>]
    member _.``C6 a cascade cleanup failure stops before the cutover``() =
        task {
            let! result, retained, _ = attachedReplacementResult "cbi24-07-cascade-cleanup-fails"
            multiple (fun () ->
                Assert.That(result.Kind, Is.EqualTo ComponentAttachedReplacementKind.CleanupFailed)
                Assert.That(result.Reason, Does.Contain "withdraw-refused")
                Assert.That(
                    result.Replacement,
                    Is.EqualTo None,
                    "Replacing on top of a cascade nobody can describe would report a cutover from an unknown state.")
                Assert.That(
                    retained.Lifecycle.Value.Members |> List.forall _.Member.IsReleased,
                    Is.True,
                    "The retained generation is untouched."))
        }

    [<Test>]
    member _.``shared CBI25 vectors bind the mediator rather than erasing the Mediation``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi25-mediated-position-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI25 vector identity must be a string"
                    | value -> value
                let result = mediatedTranslation scenario
                let expectedMediation = vector.GetProperty("expectedMediation")
                multiple (fun () ->
                    Assert.That(
                        mediatedToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Member.IsSome,
                        Is.EqualTo(vector.GetProperty("expectedPrepared").GetBoolean()),
                        scenario)
                    Assert.That(
                        result.Mediation |> Option.map MediationId.value,
                        Is.EqualTo(
                            if expectedMediation.ValueKind = JsonValueKind.Null then
                                None
                            else
                                Some(expectedMediation.GetString())),
                        scenario)
                    // Nothing mediated ever reaches the seam: what it is handed is distinct or nothing.
                    Assert.That(
                        result.Member.IsNone
                        || result.Member.Value.Requirement.Exposure = Exposure.Distinct,
                        Is.True,
                        sprintf "%s: the seam is never handed a mediated requirement." scenario))
        }

    [<Test>]
    member _.``C1 the position named must actually be mediated``() =
        let unmediated = mediatedTranslation "cbi25-02-position-is-not-mediated"
        let absent = mediatedTranslation "cbi25-07-mediated-requirement-not-resolved"
        let declared = mediatedTranslation "cbi25-09-distinct-position-declaring-a-mediation"
        multiple (fun () ->
            Assert.That(unmediated.Code, Is.EqualTo "position-not-mediated")
            Assert.That(absent.Code, Is.EqualTo "mediated-position-not-resolved")
            Assert.That(
                declared.Code,
                Is.EqualTo "position-not-mediated",
                "CM2 records a Mediation on a distinct position and ignores it; so does this.")
            Assert.That(
                [ unmediated; absent; declared ]
                |> List.forall (fun item -> item.Member.IsNone && item.Mediation.IsNone),
                Is.True,
                "Every refusal produces no portable member and no Binding Plan."))

    [<Test>]
    member _.``C2 only a Mediation realized as a Component can be bound``() =
        let staticHost = mediatedTranslation "cbi25-03-mediation-realized-by-the-host"
        let named = mediatedTranslation "cbi25-08-static-host-naming-a-component"
        let unnamed = mediatedTranslation "cbi25-04-dedicated-component-not-named"
        let bound = mediatedTranslation "cbi25-01-mediator-bound"
        multiple (fun () ->
            Assert.That(
                staticHost.Code,
                Is.EqualTo "mediation-not-a-component",
                "A static-host Mediation is the root's own work; there is nothing for a binding to reach.")
            Assert.That(
                named.Code,
                Is.EqualTo "mediation-not-a-component",
                "And it stays the host's work whatever Component it names.")
            Assert.That(unnamed.Code, Is.EqualTo "mediation-not-a-component")
            Assert.That(ComponentMediatedBinding.isTranslated bound, Is.True)
            Assert.That(
                bound.Mediation |> Option.map MediationId.value,
                Is.EqualTo(Some "mediation.cooling")))

    [<Test>]
    member _.``C3 the mapping must name the declared mediator``() =
        let memberNamed = mediatedTranslation "cbi25-05-mapping-names-a-mediated-member"
        let unresolved = mediatedTranslation "cbi25-06-mediator-occurrence-not-resolved"
        let position = mediatedPositionOf (mediatedGeneration ())
        multiple (fun () ->
            Assert.That(
                memberNamed.Code,
                Is.EqualTo "mediator-not-declared",
                "Naming a member binds past the Mediation rather than to it, which is the erasure the seam warns about.")
            Assert.That(unresolved.Code, Is.EqualTo "mediator-not-resolved")
            Assert.That(
                position.Members
                |> List.exists (fun item -> item.Definition = mediatorDefinitionId),
                Is.False,
                "The mediator is not itself a member of the set it fronts."))

    [<Test>]
    member _.``C4 what is produced is an ordinary distinct member``() =
        task {
            let bound = mediatedTranslation "cbi25-01-mediator-bound"
            let resolution = mediatedGeneration ()
            let mediator = mediatorSelection ()
            let! lifecycle =
                ComponentBindingLifecycle.activate
                    resolution
                    mediator
                    (runtimeRequest (plan [ mediator.Occurrence ]))
                    (directCooling CoolingFixture.contract)
            multiple (fun () ->
                Assert.That(bound.Member.Value.Requirement.Exposure, Is.EqualTo Exposure.Distinct)
                Assert.That(
                    lifecycle.Failure.IsNone
                    && lifecycle.Member |> Option.exists _.IsReleased,
                    Is.True,
                    "CBI2 activates it exactly as it activates any other member."))
        }

    [<Test>]
    member _.``C5 the mediated requirement is carried as provenance only``() =
        let mediatedResult = mediatedTranslation "cbi25-01-mediator-bound"
        let ordinary =
            ComponentBindingIntegration.prepare (mediatedGeneration ()) (mediatorSelection ())
        let ordinaryFacts =
            match ordinary with
            | ComponentBindingIntegrationResult.Prepared memberValue -> memberValue.ResolutionFacts
            | outcome -> failwithf "Expected a prepared member, got %A." outcome
        multiple (fun () ->
            Assert.That(
                mediatedResult.MediatedRequirement |> Option.map RequirementId.value,
                Is.EqualTo(Some(RequirementId.value mediatedRequirementId)))
            Assert.That(mediatedResult.Mediation.IsSome, Is.True)
            Assert.That(
                mediatedResult.Member.Value.ResolutionFacts = ordinaryFacts,
                Is.True,
                "The portable member is indistinguishable from one prepared for an ordinary position.")
            Assert.That(
                mediatedResult.Member.Value.ResolutionFacts |> Map.containsKey "mediation",
                Is.False,
                "Nothing of the Mediation reaches the portable layer."))

    [<Test>]
    member _.``C6 presenting the mediated requirement itself is still refused``() =
        let resolution = mediatedGeneration ()
        let position = mediatedPositionOf resolution
        let memberValue = List.head position.Members
        let direct =
            ComponentBindingIntegration.prepare
                resolution
                { mediatorSelection () with
                    Requirement = mediatedRequirementId
                    Definition = memberValue.Definition
                    Occurrence = memberValue.Occurrence }
        multiple (fun () ->
            Assert.That(
                (match direct with
                 | ComponentBindingIntegrationResult.Refused failure -> failure.Code
                 | _ -> "prepared"),
                Is.EqualTo "exposure-unsupported",
                "CBI25 adds a path that reaches the mediator; it removes no refusal."))

    [<Test>]
    member _.``shared CBI26 vectors admit a mediator for what it does itself``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi26-mediator-authority-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI26 vector identity must be a string"
                    | value -> value
                let result = mediatorAuthority scenario
                let heldByMediator =
                    not result.Grants.IsEmpty
                    && result.Grants |> List.forall (fun item -> item.Holder = providerLocalActor)
                multiple (fun () ->
                    Assert.That(
                        mediatorToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Grants.Length,
                        Is.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                        scenario)
                    Assert.That(
                        heldByMediator,
                        Is.EqualTo(vector.GetProperty("expectedHeldByMediator").GetBoolean()),
                        scenario)
                    // No member of the mediated set is ever admitted here.
                    Assert.That(
                        result.Admissions |> List.forall (fun item -> item.Participant = participant),
                        Is.True,
                        sprintf
                            "%s: the mediated Provider Set is behind the mediator and outside this admission."
                            scenario))
        }

    [<Test>]
    member _.``C1 the mediator is admitted as an ordinary participant``() =
        let admitted = mediatorAuthority "cbi26-01-mediator-admitted-for-itself"
        let submitted = (mediatorParticipant false).Request.Authority
        multiple (fun () ->
            Assert.That(ComponentMediatorAuthority.isAdmitted admitted, Is.True)
            Assert.That(
                (List.exactlyOne admitted.Admissions).Participant,
                Is.EqualTo participant,
                "The mediator is admitted against its own occurrence, as any participant is.")
            Assert.That(
                admitted.Grants
                |> List.map (fun item ->
                    sprintf "%s/%s" (CapabilityId.value item.Capability) (OperationId.value item.Operation))
                |> String.concat ", ",
                Is.EqualTo(
                    submitted
                    |> List.map (fun item ->
                        sprintf "%s/%s" (CapabilityId.value item.Capability) (OperationId.value item.Operation))
                    |> String.concat ", "),
                "One grant per narrow tuple submitted, which is exactly CBI3's correspondence.")
            Assert.That(
                admitted.Mediation |> Option.map MediationId.value,
                Is.EqualTo(Some "mediation.cooling")))

    [<Test>]
    member _.``C2 a Mediation that owns authority is refused``() =
        let owned = mediatorAuthority "cbi26-02-mediation-owns-authority"
        multiple (fun () ->
            Assert.That(owned.Code, Is.EqualTo "mediation-owns-authority")
            Assert.That(
                owned.Reason,
                Does.Contain "on behalf of",
                "The refusal names the missing relation rather than the request.")
            Assert.That(owned.Grants, Is.Empty))

    [<Test>]
    member _.``C3 the other ownership flags are not authority``() =
        let lifecycle = mediatorAuthority "cbi26-03-mediation-owns-lifecycle"
        let recovery = mediatorAuthority "cbi26-04-mediation-owns-recovery-and-residue"
        multiple (fun () ->
            Assert.That(
                ComponentMediatorAuthority.isAdmitted lifecycle,
                Is.True,
                "Owning a lifecycle says nothing about who may exercise a Capability.")
            Assert.That(ComponentMediatorAuthority.isAdmitted recovery, Is.True)
            Assert.That(
                [ lifecycle; recovery ] |> List.forall (fun item -> item.Grants.Length = 1),
                Is.True,
                "The outcome depends on OwnsAuthority alone among the ownership flags."))

    [<Test>]
    member _.``C4 the mediator's grants are its own``() =
        let admitted = mediatorAuthority "cbi26-01-mediator-admitted-for-itself"
        let position = mediatedPositionOf (mediatedGeneration ())
        multiple (fun () ->
            Assert.That(
                admitted.Grants |> List.forall (fun item -> item.Holder = providerLocalActor),
                Is.True,
                "Every grant is held by the mediator's local Actor.")
            Assert.That(admitted.Admissions.Length, Is.EqualTo 1)
            Assert.That(
                position.Members,
                Is.Not.Empty,
                "The mediated set has members, and none of them was admitted."))

    [<Test>]
    member _.``C5 nothing widens CM5 and CBI3 is unchanged``() =
        let foreign = mediatorAuthority "cbi26-07-request-names-another-occurrence"
        let denied = mediatorAuthority "cbi26-06-authority-denied"
        let refusedTranslation = mediatorAuthority "cbi26-05-translation-refused"
        multiple (fun () ->
            Assert.That(
                foreign.Code,
                Is.EqualTo "participant-mapping-invalid",
                "CBI3's own mapping rule still decides, unrelaxed.")
            Assert.That(denied.Code, Is.EqualTo "authority-not-admitted")
            Assert.That(
                refusedTranslation.Code,
                Is.EqualTo "mediator-not-declared",
                "A translation that CBI25 refuses never reaches CM5.")
            Assert.That(
                [ foreign; denied; refusedTranslation ]
                |> List.forall (fun item -> item.Grants.IsEmpty),
                Is.True))

    [<Test>]
    member _.``shared CBI27 vectors fan a wide position out``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi27-wider-provider-set-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI27 vector identity must be a string"
                    | value -> value
                let result = wideTranslation scenario
                let expectedCardinality = vector.GetProperty("expectedCardinality")
                let distinctScopes =
                    result.Members
                    |> List.map (fun item ->
                        Brontide.Minimal.Binding.Portable.BindingScopeId.value item.Member.Scope)
                    |> List.distinct
                    |> List.length
                multiple (fun () ->
                    Assert.That(
                        wideToken result.Kind,
                        Is.EqualTo(vector.GetProperty("expectedKind").GetString()),
                        scenario)
                    Assert.That(
                        result.Code,
                        Is.EqualTo(vector.GetProperty("expectedCode").GetString()),
                        scenario)
                    Assert.That(
                        result.Members.Length,
                        Is.EqualTo(vector.GetProperty("expectedMembers").GetInt32()),
                        scenario)
                    Assert.That(
                        result.UnfilledOptionalPositions,
                        Is.EqualTo(vector.GetProperty("expectedUnfilled").GetInt32()),
                        scenario)
                    Assert.That(
                        distinctScopes,
                        Is.EqualTo(vector.GetProperty("expectedDistinctScopes").GetInt32()),
                        sprintf
                            "%s: every member of a fanned-out position holds a scope of its own."
                            scenario)
                    Assert.That(
                        result.Cardinality |> Option.map wideCardinalityText,
                        Is.EqualTo(
                            if expectedCardinality.ValueKind = JsonValueKind.Null then
                                None
                            else
                                Some(expectedCardinality.GetString())),
                        scenario)
                    // The properties over every vector: a position that is not fanned out produces
                    // nothing at all, and a member that is produced is an ordinary one-to-one binding.
                    Assert.That(
                        ComponentProviderSetBinding.isTranslated result || result.Members.IsEmpty,
                        Is.True,
                        sprintf "%s: only a translated position produces members." scenario)
                    Assert.That(
                        result.Members
                        |> List.forall (fun item ->
                            item.Member.Requirement.Cardinality = ProviderCardinality.oneToOne
                            && item.Member.Requirement.Exposure = Exposure.Distinct
                            && CompositionStage.token item.Member.Stage = "local-initialisation"
                            && item.Member.TryPlan.IsNone),
                        Is.True,
                        sprintf "%s: nothing of the set reaches the seam." scenario))
        }

    [<Test>]
    member _.``C1 a one-to-one position is CBI1's and a mediated one is CBI25's``() =
        let narrow = wideTranslation "cbi27-05-position-is-one-to-one"
        let mediated = wideTranslation "cbi27-06-position-mediated"
        let declared = wideTranslation "cbi27-07-distinct-position-declaring-a-mediation"
        let absent = wideTranslation "cbi27-15-position-not-resolved"
        multiple (fun () ->
            Assert.That(narrow.Code, Is.EqualTo "position-not-wide")
            Assert.That(mediated.Code, Is.EqualTo "position-mediated")
            Assert.That(
                declared.Code,
                Is.EqualTo "position-mediated",
                "Exposure and the declaration are two facts, and CM2 records the second without acting on it.")
            Assert.That(absent.Code, Is.EqualTo "wide-position-not-resolved")
            Assert.That(
                [ narrow; mediated; declared; absent ]
                |> List.forall (fun item -> item.Members.IsEmpty),
                Is.True))

    [<Test>]
    member _.``C2 the membership is the generation's statement``() =
        let resolution =
            wideResolution (Cardinality.parse "2..2") 2 false ProviderExposure.Distinct false
        let fanned = wideTranslation "cbi27-01-two-members-fanned-out"
        let missing = wideTranslation "cbi27-08-member-not-supplied"
        let unresolved = wideTranslation "cbi27-09-member-not-resolved"
        let repeated = wideTranslation "cbi27-10-member-supplied-twice"
        let elsewhere = wideTranslation "cbi27-14-member-requirement-mismatched"
        let translated =
            fanned.Members |> List.map (fun item -> OccurrenceId.value item.Occurrence)
        let resolved =
            (widePosition resolution).Members
            |> List.map (fun item -> OccurrenceId.value item.Occurrence)
        multiple (fun () ->
            Assert.That(
                String.concat ", " translated,
                Is.EqualTo(String.concat ", " resolved),
                "An admitted translation names exactly the members the generation resolved.")
            Assert.That(
                [ missing; unresolved; repeated ]
                |> List.forall (fun item -> item.Code = "membership-not-resolved"),
                Is.True,
                "Omitting, adding, and repeating a member are all the caller disagreeing with the generation.")
            Assert.That(elsewhere.Code, Is.EqualTo "member-requirement-mismatch"))

    [<Test>]
    member _.``C3 each member carries its own binding scope``() =
        let fanned = wideTranslation "cbi27-01-two-members-fanned-out"
        let shared = wideTranslation "cbi27-11-members-share-a-binding-scope"
        let scopes = fanned.Members |> List.map (fun item -> item.Member.TryFact "bindingScope")
        multiple (fun () ->
            Assert.That(
                scopes |> List.distinct |> List.length,
                Is.EqualTo scopes.Length,
                "The portable scope names one binding, so two members of one set cannot share it.")
            Assert.That(
                fanned.PositionScope
                |> Option.map Brontide.Minimal.Experimental.ComponentManagement.BindingScopeId.value,
                Is.EqualTo(Some "scope.cooling"),
                "The CM position's scope is carried as provenance, and no member reports it.")
            Assert.That(scopes, Does.Not.Contain(Some "scope.cooling"))
            Assert.That(shared.Code, Is.EqualTo "scope-not-distinct")
            Assert.That(shared.Members, Is.Empty))

    /// The same collision arrives without a wide set, and this records it rather than fixing it.
    ///
    /// CBI1 unwraps the CM position's binding scope into the portable one, which is a bijection only
    /// while one CM scope holds one position. Two positions resolved in one scope - which is what a CM
    /// scope is for, since CM2 looks bindings up by scope and contract - therefore reach the seam as
    /// two members claiming one scope, the case its scope-uniqueness silence tells a composition to
    /// reject. Correcting it moves every member's bindingScope fact and so every CBI4 digest the
    /// shared fixture pins, which is Decision 16's question rather than this slice's.
    [<Test>]
    member _.``C3 two positions in one CM scope reach the seam as one scope``() =
        let resolution = pairRequest () |> FakeGenerationResolver.resolve
        let positions =
            match resolution with
            | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
            | outcome -> failwithf "Expected a resolved generation, got %A." outcome
        let scopes =
            positions
            |> List.map (fun position ->
                let memberValue = List.head position.Members
                match
                    ComponentBindingIntegration.prepare
                        resolution
                        { selection memberValue with Requirement = position.Requirement }
                with
                | ComponentBindingIntegrationResult.Prepared prepared ->
                    prepared.TryFact "bindingScope"
                | ComponentBindingIntegrationResult.Refused failure ->
                    failwithf "Expected a prepared member, got %A." failure)
        multiple (fun () ->
            Assert.That(scopes.Length, Is.EqualTo 2)
            Assert.That(
                scopes |> List.distinct |> List.length,
                Is.EqualTo 1,
                "Both positions were resolved in one CM binding scope, and both members report it."))

    [<Test>]
    member _.``C4 a refused member leaves no member at all``() =
        let mismatched = wideTranslation "cbi27-12-member-mapping-mismatched"
        let endpoint = wideTranslation "cbi27-13-member-endpoint-invalid"
        multiple (fun () ->
            Assert.That(mismatched.Code, Is.EqualTo "selection-mismatch")
            Assert.That(endpoint.Code, Is.EqualTo "endpoint-invalid")
            Assert.That(
                [ mismatched; endpoint ] |> List.forall (fun item -> item.Members.IsEmpty),
                Is.True,
                "The member that would have worked is not kept: that would be the narrowing the seam refuses, performed here."))

    [<Test>]
    member _.``C5 the set is not a portable fact``() =
        let fanned = wideTranslation "cbi27-01-two-members-fanned-out"
        let wide =
            { Scope = namedScope "scope.cooling-wide"
              Component = CoolingFixture.component'
              RequiredProvider = Some CoolingFixture.provider
              Cardinality = { Minimum = 1; Maximum = 2 }
              Exposure = Exposure.Distinct
              HostEndpoint = "wide-host" }
        let refusedCode =
            match
                PortableCompositionHandoff.prepare
                    wide
                    { Component = CoolingFixture.component'
                      Provider = CoolingFixture.provider
                      ProviderEndpoint = "cooling-provider" }
                    CoolingFixture.contract
            with
            | Error(PortableError.Refused fault) -> fault.LocalCode
            | other -> failwithf "Expected a refusal, got %A." other
        multiple (fun () ->
            Assert.That(
                fanned.Members
                |> List.forall (fun item -> item.Member.TryFact "cardinality" = Some "1..1"),
                Is.True,
                "Each member is one provider answering one contract, which is all the seam binds.")
            Assert.That(
                refusedCode,
                Is.EqualTo "cardinality-unsupported",
                "The seam's own refusal is untouched: nothing wide is ever presented to it."))

    [<Test>]
    member _.``C6 a position that resolved nothing binds nothing``() =
        let unfilled = wideTranslation "cbi27-04-position-resolved-empty"
        let supplied = wideTranslation "cbi27-16-unfilled-position-supplied"
        multiple (fun () ->
            Assert.That(unfilled.Kind, Is.EqualTo ComponentProviderSetTranslationKind.Unfilled)
            Assert.That(unfilled.Code, Is.EqualTo "position-resolved-empty")
            Assert.That(
                unfilled.Members,
                Is.Empty,
                "Nothing to bind and nothing wrong, reported as neither a translation nor a refusal.")
            Assert.That(
                supplied.Code,
                Is.EqualTo "membership-not-resolved",
                "A caller that supplies a member for a position that resolved none disagrees with the generation."))

    [<Test>]
    member _.``C7 what the set carries beyond its members is not carried``() =
        let spare = wideTranslation "cbi27-02-optional-capacity-unfilled"
        let filled = wideTranslation "cbi27-03-preselected-optional-member"
        multiple (fun () ->
            Assert.That(
                spare.Cardinality,
                Is.EqualTo(Some(Cardinality.parse "1..3")))
            Assert.That(
                spare.UnfilledOptionalPositions,
                Is.EqualTo 2,
                "Spare capacity is reported by the translation, because no member can report it.")
            Assert.That(
                (List.exactlyOne spare.Members).Member.ResolutionFacts
                |> Map.exists (fun _ value -> value = "1..3"),
                Is.False,
                "The position's declared bound is not a portable fact.")
            Assert.That(
                filled.UnfilledOptionalPositions,
                Is.EqualTo 0,
                "A preselected optional member fills the capacity at resolution, not here.")
            Assert.That(filled.Members.Length, Is.EqualTo 2))

    [<Test>]
    member _.``C8 CBI1 and CBI25 are unchanged``() =
        let narrow = resolve (Cardinality.parse "1..1")
        let direct = ComponentBindingIntegration.prepare narrow (selection (memberOf narrow))
        let wide = wideResolution (Cardinality.parse "2..2") 2 false ProviderExposure.Distinct false
        let throughCbi1 =
            match
                ComponentBindingIntegration.prepare
                    wide
                    (selection (List.head (widePosition wide).Members))
            with
            | ComponentBindingIntegrationResult.Refused failure -> failure.Code
            | other -> failwithf "Expected a refusal, got %A." other
        let directlyPrepared =
            match direct with
            | ComponentBindingIntegrationResult.Prepared _ -> true
            | _ -> false
        let mediated =
            ComponentMediatedBinding.translate
                (mediatedGeneration ())
                { MediatedRequirement = mediatedRequirementId; Mediator = mediatorSelection () }
        multiple (fun () ->
            Assert.That(directlyPrepared, Is.True)
            Assert.That(
                throughCbi1,
                Is.EqualTo "cardinality-unsupported",
                "CBI1 still accepts exactly 1..1; CBI27 adds a path rather than widening one.")
            Assert.That(ComponentMediatedBinding.isTranslated mediated, Is.True))

    [<Test>]
    member _.``shared CBI28 vectors activate a fanned-out position``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi28-fanned-out-activation-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI28 vector identity must be a string"
                    | value -> value
                let! result, handlers = fannedOutActivation scenario
                let expectedFailure = vector.GetProperty("expectedFailureKind")
                let expectedCode = vector.GetProperty("expectedCode")
                let members =
                    match result.Lifecycle with
                    | Some lifecycle -> lifecycle.Members
                    | None -> []
                let released = members |> List.filter _.Member.IsReleased |> List.length
                let retired =
                    members
                    |> List.filter (fun item -> CompositionStage.token item.Member.Stage = "retired")
                    |> List.length
                multiple (fun () ->
                    Assert.That(
                        ComponentGroupAuthority.isActive result,
                        Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()),
                        scenario)
                    Assert.That(
                        result.Failure |> Option.map (fun failure -> groupAuthorityToken failure.Kind),
                        Is.EqualTo(
                            if expectedFailure.ValueKind = JsonValueKind.Null then
                                None
                            else
                                Some(expectedFailure.GetString())),
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
                        result.Admissions.Length,
                        Is.EqualTo(vector.GetProperty("expectedMembersAdmitted").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Grants.Length,
                        Is.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                        scenario)
                    Assert.That(released, Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()), scenario)
                    Assert.That(retired, Is.EqualTo(vector.GetProperty("expectedRetired").GetInt32()), scenario)
                    Assert.That(
                        handlers |> List.sumBy _.ProviderEffectCount,
                        Is.EqualTo(vector.GetProperty("expectedProviderEffects").GetInt32()),
                        scenario)
                    // The property over every vector: the barrier is the activation's, so a strict
                    // subset of its members never ends up serving.
                    Assert.That(
                        released = 0 || released = members.Length,
                        Is.True,
                        sprintf "%s: ordinary interaction opens for every member or for none." scenario))
        }

    [<Test>]
    member _.``C1 a member of a wide position carries the scope its caller named``() =
        task {
            let! active, _ = fannedOutActivation "cbi28-01-wide-position-activated"
            let! unscoped, _ = fannedOutActivation "cbi28-04-member-without-a-scope"
            let! overScoped, _ = fannedOutActivation "cbi28-05-ordinary-member-with-a-scope"
            let scopes =
                active.Lifecycle.Value.Members
                |> List.map (fun item -> defaultArg (item.Member.TryFact "bindingScope") "absent")
                |> List.sort
                |> String.concat ", "
            multiple (fun () ->
                Assert.That(
                    scopes,
                    Is.EqualTo "scope.cooling-0, scope.cooling-1",
                    "Each member of the position holds the scope its caller named.")
                Assert.That(unscoped.Failure.Value.Code, Is.EqualTo "member-scope-required")
                Assert.That(
                    overScoped.Failure.Value.Code,
                    Is.EqualTo "member-scope-not-required",
                    "A 1..1 position's scope is the generation's, so naming one disagrees with the resolution."))
        }

    /// The check the activation could not make: both of its existing checks compare the caller's
    /// member list with the caller's plan, so a position supplied half-complete satisfies both.
    [<Test>]
    member _.``C2 a wide position joins the activation whole``() =
        task {
            let! partial, handlers = fannedOutActivation "cbi28-03-member-missing-from-the-activation"
            let position = widePosition (wideActivationResolution false)
            let supplied = (List.head position.Members).Occurrence
            let planValue = plan [ supplied ]
            let planned =
                planValue.Groups
                |> List.collect (fun group -> group.Members |> List.map _.Occurrence)
            multiple (fun () ->
                Assert.That(
                    planned |> List.map OccurrenceId.value |> String.concat ", ",
                    Is.EqualTo(OccurrenceId.value supplied),
                    "The caller's plan carries exactly the member the caller selected, which is all the plan check compares.")
                Assert.That(position.Members.Length, Is.EqualTo 2, "The generation resolved two.")
                Assert.That(
                    partial.Failure.Value.Code,
                    Is.EqualTo "membership-not-resolved",
                    "Only the resolution can say the position is short a member.")
                Assert.That(partial.Lifecycle.Value.Members, Is.Empty)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C3 the position's minimum is not a runtime concept``() =
        task {
            let! failed, _ = fannedOutActivation "cbi28-07-one-member-never-ready"
            let spareResolution =
                wideResolution (Cardinality.parse "1..2") 2 true ProviderExposure.Distinct false
            let spare = widePosition spareResolution
            let decisions =
                match spareResolution with
                | ResolutionOutcome.Resolved(proposed, _) ->
                    proposed.Decisions
                    |> List.filter (fun item -> item.Requirement = Some requirementId)
                    |> List.map _.Kind
                | outcome -> failwithf "Expected a resolved generation, got %A." outcome
            multiple (fun () ->
                Assert.That(
                    failed.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length,
                    Is.EqualTo 0,
                    "One member short of Ready retires the activation, siblings included.")
                Assert.That(
                    spare.Cardinality.Minimum,
                    Is.EqualTo 1,
                    "A 1..2 position is satisfied by one provider, and that changes nothing here.")
                Assert.That(
                    decisions |> List.contains "required-provider-selected",
                    Is.True,
                    "CM2 knows which member was optional...")
                Assert.That(decisions |> List.contains "optional-provider-preselected", Is.True)
                Assert.That(
                    spare.Members |> List.map _.Retained |> List.distinct |> List.length,
                    Is.EqualTo 1,
                    "...and the resolved members carry nothing that distinguishes them.")
                Assert.That(
                    typeof<ActivationGroupMember>.GetProperties()
                    |> Array.map _.Name
                    |> Array.contains "Optional",
                    Is.False,
                    "Nothing about an optional member reaches the plan CM4 activates."))
        }

    [<Test>]
    member _.``C4 authority stays per member of the position``() =
        task {
            let! active, _ = fannedOutActivation "cbi28-01-wide-position-activated"
            let! denied, handlers = fannedOutActivation "cbi28-08-one-member-denied"
            let admitted =
                active.Admissions
                |> List.map (fun item -> OccurrenceId.value item.Occurrence)
                |> List.sort
                |> String.concat ", "
            let resolved =
                (widePosition (wideActivationResolution false)).Members
                |> List.map (fun item -> OccurrenceId.value item.Occurrence)
                |> List.sort
                |> String.concat ", "
            multiple (fun () ->
                Assert.That(
                    admitted,
                    Is.EqualTo resolved,
                    "Two members of one position are two admissions, each against its own occurrence.")
                Assert.That(
                    active.Grants |> List.map _.Holder |> List.distinct |> List.length,
                    Is.EqualTo 2,
                    "Each member's own party holds its own grant.")
                Assert.That(
                    groupAuthorityToken denied.Failure.Value.Kind,
                    Is.EqualTo "member-authority-refused")
                Assert.That(
                    denied.Lifecycle,
                    Is.EqualTo None,
                    "The authority barrier is earlier: a refused member leaves no lifecycle at all.")
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``C5 a wide position activates beside an ordinary one``() =
        task {
            let! mixed, _ = fannedOutActivation "cbi28-02-wide-position-beside-an-ordinary-one"
            let scopes =
                mixed.Lifecycle.Value.Members
                |> List.map (fun item -> item.Member.TryFact "bindingScope")
            multiple (fun () ->
                Assert.That(ComponentGroupAuthority.isActive mixed, Is.True)
                Assert.That(mixed.Lifecycle.Value.Members.Length, Is.EqualTo 3)
                Assert.That(
                    scopes |> List.contains (Some "scope.cooling"),
                    Is.True,
                    "The ordinary position's member reports the scope the generation recorded.")
                Assert.That(
                    scopes
                    |> List.filter (fun scope -> scope <> Some "scope.cooling")
                    |> List.map (fun scope -> defaultArg scope "absent")
                    |> List.sort
                    |> String.concat ", ",
                    Is.EqualTo "scope.cooling-0, scope.cooling-1"))
        }

    [<Test>]
    member _.``C6 scope distinctness is checked within the position only``() =
        task {
            let! shared, _ = fannedOutActivation "cbi28-06-members-share-a-scope"
            let! pair, _ = fannedOutPair ()
            let pairScopes =
                pair.Lifecycle.Value.Members
                |> List.map (fun item -> item.Member.TryFact "bindingScope")
                |> List.distinct
            multiple (fun () ->
                Assert.That(shared.Failure.Value.Code, Is.EqualTo "scope-not-distinct")
                Assert.That(
                    ComponentGroupAuthority.isActive pair,
                    Is.True,
                    "Two ordinary positions in one CM scope are admitted, which is why the check is not activation-wide.")
                Assert.That(
                    pairScopes.Length,
                    Is.EqualTo 1,
                    "Both of their members report one portable scope, which is Decision 16."))
        }

    [<Test>]
    member _.``C7 earlier slices are unchanged for every input they accepted``() =
        task {
            let! pair, _ = fannedOutPair ()
            let narrow = resolve (Cardinality.parse "1..1")
            let direct = ComponentBindingIntegration.prepare narrow (selection (memberOf narrow))
            let translation = wideTranslation "cbi27-01-two-members-fanned-out"
            multiple (fun () ->
                Assert.That(
                    ComponentGroupAuthority.isActive pair,
                    Is.True,
                    "A CBI13 activation of two 1..1 positions is untouched.")
                Assert.That(
                    (match direct with
                     | ComponentBindingIntegrationResult.Prepared _ -> true
                     | _ -> false),
                    Is.True,
                    "CBI1 is untouched.")
                Assert.That(
                    ComponentProviderSetBinding.isTranslated translation,
                    Is.True,
                    "CBI27 is untouched."))
        }

    [<Test>]
    member _.``shared CBI29 vectors activate a fanned-out position inside a child Port``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi29-fanned-out-child-port-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI29 vector identity must be a string"
                    | value -> value
                let! result, parent, parentHandlers, _ = wideChildActivation scenario
                let members =
                    result.Child
                    |> Option.bind _.Lifecycle
                    |> Option.map _.Members
                    |> Option.defaultValue []
                let released = members |> List.filter _.Member.IsReleased |> List.length
                let retired =
                    members
                    |> List.filter (fun item -> CompositionStage.token item.Member.Stage = "retired")
                    |> List.length
                multiple (fun () ->
                    Assert.That(childToken result.Kind, Is.EqualTo(vector.GetProperty("expectedKind").GetString()), scenario)
                    Assert.That(result.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario)
                    Assert.That(members.Length, Is.EqualTo(vector.GetProperty("expectedChildMembers").GetInt32()), scenario)
                    Assert.That(
                        result.Child |> Option.map (fun value -> value.Admissions.Length) |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedAdmitted").GetInt32()),
                        scenario)
                    Assert.That(
                        result.Child |> Option.map (fun value -> value.Grants.Length) |> Option.defaultValue 0,
                        Is.EqualTo(vector.GetProperty("expectedGrants").GetInt32()),
                        scenario)
                    Assert.That(released, Is.EqualTo(vector.GetProperty("expectedReleased").GetInt32()), scenario)
                    Assert.That(retired, Is.EqualTo(vector.GetProperty("expectedRetired").GetInt32()), scenario)
                    Assert.That(
                        parent.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length,
                        Is.EqualTo(vector.GetProperty("expectedParentReleased").GetInt32()),
                        scenario)
                    Assert.That(released = 0 || released = members.Length, Is.True, scenario)
                    Assert.That(parentHandlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0, scenario))
        }

    [<Test>]
    member _.``CBI29 C1 every member is contained in the attached Port``() =
        task {
            let! attached, _, _, _ = wideChildActivation "cbi29-01-wide-child-attached"
            let! foreign, _, _, foreignHandlers =
                wideChildActivation "cbi29-07-attachment-names-another-port"
            multiple (fun () ->
                Assert.That(attached.Port |> Option.map PortId.value, Is.EqualTo(Some(PortId.value childPortId)))
                Assert.That(attached.Child.Value.Admissions.Length, Is.EqualTo 2)
                Assert.That(foreign.Code, Is.EqualTo "port-not-resolved")
                Assert.That(foreign.Child, Is.EqualTo None, "Containment is checked before authority is evaluated.")
                Assert.That(foreignHandlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``CBI29 C2 the whole position enters the child activation``() =
        task {
            let! partial, _, _, handlers = wideChildActivation "cbi29-02-member-omitted"
            multiple (fun () ->
                Assert.That(partial.Code, Is.EqualTo "membership-not-resolved")
                Assert.That(partial.Child.Value.Admissions.Length, Is.EqualTo 1)
                Assert.That(partial.Child.Value.Lifecycle.Value.Members, Is.Empty)
                Assert.That(handlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``CBI29 C3 member scopes are distinct from the child restart scope``() =
        task {
            let! attached, _, _, _ = wideChildActivation "cbi29-01-wide-child-attached"
            let! missing, _, _, _ = wideChildActivation "cbi29-03-member-without-portable-scope"
            let! shared, _, _, _ = wideChildActivation "cbi29-04-portable-scope-reused"
            let scopes =
                attached.Child.Value.Lifecycle.Value.Members
                |> List.map (fun item -> item.Member.TryFact "bindingScope")
            multiple (fun () ->
                Assert.That(
                    scopes |> List.choose id |> List.sort |> String.concat ", ",
                    Is.EqualTo "scope.child-member-0, scope.child-member-1")
                Assert.That(scopes |> List.contains (Some(RestartScopeId.value childScopeId)), Is.False)
                Assert.That(
                    attached.Child.Value.Lifecycle.Value.Runtime.Value.Observation.RestartScope,
                    Is.EqualTo childScopeId)
                Assert.That(missing.Code, Is.EqualTo "member-scope-required")
                Assert.That(shared.Code, Is.EqualTo "scope-not-distinct"))
        }

    [<Test>]
    member _.``CBI29 C4 authority and Release are child-wide barriers``() =
        task {
            let! denied, _, _, deniedHandlers = wideChildActivation "cbi29-06-member-authority-denied"
            let! notReady, _, _, notReadyHandlers = wideChildActivation "cbi29-05-member-never-ready"
            multiple (fun () ->
                Assert.That(denied.Code, Is.EqualTo "authority-not-admitted")
                Assert.That(denied.Child.Value.Lifecycle, Is.EqualTo None)
                Assert.That(deniedHandlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0)
                Assert.That(notReady.Code, Is.EqualTo "child-establishment-refused")
                Assert.That(
                    notReady.Child.Value.Lifecycle.Value.Members |> List.filter _.Member.IsReleased |> List.length,
                    Is.EqualTo 0)
                Assert.That(notReadyHandlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0))
        }

    [<Test>]
    member _.``CBI29 C5 the released parent is untouched``() =
        task {
            for scenario in
                [ "cbi29-01-wide-child-attached"
                  "cbi29-02-member-omitted"
                  "cbi29-05-member-never-ready"
                  "cbi29-06-member-authority-denied" ] do
                let! _, parent, parentHandlers, _ = wideChildActivation scenario
                multiple (fun () ->
                    Assert.That(ComponentGroupAuthority.isActive parent, Is.True, scenario)
                    Assert.That(parent.Lifecycle.Value.Members |> List.forall _.Member.IsReleased, Is.True, scenario)
                    Assert.That(parentHandlers |> List.sumBy _.ProviderEffectCount, Is.EqualTo 0, scenario))
        }

    [<Test>]
    member _.``CBI29 C6 existing child and wide paths remain unchanged``() =
        task {
            let! ordinaryChild, _, _, _ = childActivationResult "cbi22-01-child-attached"
            let! wideRoot, _ = fannedOutActivation "cbi28-01-wide-position-activated"
            let! wideChild, _, _, _ = wideChildActivation "cbi29-01-wide-child-attached"
            multiple (fun () ->
                Assert.That(ComponentChildActivation.isAttached ordinaryChild, Is.True)
                Assert.That(ComponentGroupAuthority.isActive wideRoot, Is.True)
                Assert.That(ComponentChildActivation.isAttached wideChild, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``shared CBI30 vectors activate through real provider processes``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi30-process-activation-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario =
                    match vector.GetProperty("id").GetString() with
                    | null -> failwith "CBI30 vector identity must be a string"
                    | value -> value
                let providerName =
                    match vector.GetProperty("provider").GetString() with
                    | null -> failwith "CBI30 provider identity must be a string"
                    | value -> value
                let! observation =
                    cbi30Run
                        providerName
                        (vector.GetProperty("interruptBeforeInterconnection").GetBoolean())
                let expectedRealization = vector.GetProperty("expectedRealization")
                multiple (fun () ->
                    Assert.That(observation.Active, Is.EqualTo(vector.GetProperty("expectedActive").GetBoolean()), scenario)
                    Assert.That(observation.Code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario)
                    Assert.That(
                        observation.Realization,
                        Is.EqualTo(
                            if expectedRealization.ValueKind = JsonValueKind.Null then
                                None
                            else
                                Some(expectedRealization.GetString())),
                        scenario)
                    Assert.That(observation.Released, Is.EqualTo(vector.GetProperty("expectedReleased").GetBoolean()), scenario)
                    Assert.That(observation.Retired, Is.EqualTo(vector.GetProperty("expectedRetired").GetBoolean()), scenario)
                    Assert.That(
                        observation.ProviderExited,
                        Is.EqualTo(vector.GetProperty("expectedProviderExited").GetBoolean()),
                        scenario)
                    Assert.That(observation.Active, Is.EqualTo observation.Released, scenario))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI30 C1 activation crosses a real process boundary``() =
        task {
            let! observation = cbi30Run "minimal" false
            multiple (fun () ->
                Assert.That(observation.Active, Is.True)
                Assert.That(observation.Released, Is.True)
                Assert.That(observation.Code, Is.EqualTo "active"))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI30 C2 either stack provider is substitutable at the process seam``() =
        task {
            let! reference = cbi30Run "reference" false
            let! minimal = cbi30Run "minimal" false
            multiple (fun () ->
                Assert.That(reference.Active, Is.EqualTo minimal.Active)
                Assert.That(reference.Code, Is.EqualTo minimal.Code)
                Assert.That(reference.Realization, Is.EqualTo minimal.Realization)
                Assert.That(reference.AnsweringProvider, Is.EqualTo minimal.AnsweringProvider)
                Assert.That(reference.Released, Is.EqualTo minimal.Released)
                Assert.That(reference.Retired, Is.EqualTo minimal.Retired)
                Assert.That(reference.ProviderExited, Is.True)
                Assert.That(minimal.ProviderExited, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI30 C3 the negotiated realization and answering provider are observable``() =
        task {
            let! observation = cbi30Run "reference" false
            multiple (fun () ->
                Assert.That(observation.Realization, Is.EqualTo(Some "negotiated-process"))
                Assert.That(
                    observation.AnsweringProvider,
                    Is.EqualTo(Some(PortableProviderRef.text CoolingFixture.provider))))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI30 C4 process loss is an explicit pre-Release refusal``() =
        task {
            let! observation = cbi30Run "minimal" true
            multiple (fun () ->
                Assert.That(observation.Code, Is.EqualTo "portable-process-interrupted")
                Assert.That(observation.Active, Is.False)
                Assert.That(observation.Released, Is.False)
                Assert.That(observation.Realization, Is.EqualTo None))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI30 C5 retirement closes the process lifecycle``() =
        task {
            let! observation = cbi30Run "minimal" false
            multiple (fun () ->
                Assert.That(observation.Retired, Is.True)
                Assert.That(observation.ProviderExited, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``shared CBI31 vectors verify policy and activate local artifacts``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi31-local-artifact-activation-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario = vector.GetProperty("id").GetString()
                let! code, launched, isolation, active, released, retired, exited = cbi31Run vector
                let expectedIsolation = vector.GetProperty("expectedIsolation")
                multiple (fun () ->
                    Assert.That(code, Is.EqualTo(vector.GetProperty("expectedCode").GetString()), scenario)
                    Assert.That(launched, Is.EqualTo(vector.GetProperty("expectedLaunched").GetBoolean()), scenario)
                    Assert.That(
                        isolation,
                        Is.EqualTo(
                            if expectedIsolation.ValueKind = JsonValueKind.Null then None
                            else Some(expectedIsolation.GetString())),
                        scenario)
                    Assert.That(active, Is.EqualTo launched, scenario)
                    Assert.That(released, Is.EqualTo launched, scenario)
                    Assert.That(retired, Is.EqualTo launched, scenario)
                    Assert.That(exited, Is.True, scenario))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C1 acquisition verifies an immutable local artifact``() =
        task {
            let! missing = cbi31Run (cbi31Vector "cbi31-03-missing-artifact")
            let! changed = cbi31Run (cbi31Vector "cbi31-04-integrity-refused")
            let missingCode, missingLaunched, _, _, _, _, _ = missing
            let changedCode, changedLaunched, _, _, _, _, _ = changed
            multiple (fun () ->
                Assert.That(missingCode, Is.EqualTo "artifact-unavailable")
                Assert.That(changedCode, Is.EqualTo "artifact-integrity-failed")
                Assert.That(missingLaunched || changedLaunched, Is.False))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C2 launch policy is explicit and precedes execution``() =
        task {
            let! arguments = cbi31Run (cbi31Vector "cbi31-05-arguments-refused")
            let argumentsCode, argumentsLaunched, _, _, _, _, _ = arguments
            let providerPath = cbi31ProviderPath "minimal"
            let providerRoot =
                match Path.GetDirectoryName providerPath |> Option.ofObj with
                | Some value -> value
                | None -> failwith "CBI31 provider path must have a parent directory."
            let outsideRoot =
                LocalProviderArtifactActivator.acquireAndLaunch
                    { Identity = "outside-root"
                      SourcePath = providerPath
                      Sha256 = cbi31Digest providerPath
                      Arguments = [ "--portable" ] }
                    { AllowedRoot = Path.Combine(providerRoot, "allowed")
                      AllowedArguments = [ "--portable" ] }
            multiple (fun () ->
                Assert.That(argumentsCode, Is.EqualTo "launch-policy-refused")
                Assert.That(argumentsLaunched, Is.False)
                match outsideRoot with
                | LocalProviderActivation.Refused failure ->
                    Assert.That(failure.Code, Is.EqualTo "launch-policy-refused")
                | LocalProviderActivation.Launched owner ->
                    owner.Dispose()
                    Assert.Fail("The outside-root artifact was launched."))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C3 launch isolation is observable and bounded``() =
        let providerPath = cbi31ProviderPath "minimal"
        let providerRoot =
            match Path.GetDirectoryName providerPath |> Option.ofObj with
            | Some value -> value
            | None -> failwith "CBI31 provider path must have a parent directory."
        let activation =
            LocalProviderArtifactActivator.acquireAndLaunch
                { Identity = "isolation"
                  SourcePath = providerPath
                  Sha256 = cbi31Digest providerPath
                  Arguments = [ "--portable" ] }
                { AllowedRoot = providerRoot
                  AllowedArguments = [ "--portable" ] }
        match activation with
        | LocalProviderActivation.Refused failure -> Assert.Fail(sprintf "Launch failed: %s" failure.Code)
        | LocalProviderActivation.Launched owner ->
            use owner = owner
            multiple (fun () ->
                Assert.That(owner.Isolation, Is.EqualTo "dedicated-process")
                Assert.That(owner.UsesShell, Is.False)
                Assert.That(owner.RedirectsStandardStreams, Is.True))
        let nonExecutable = Path.Combine(Path.GetTempPath(), $"brontide-cbi31-{Guid.NewGuid():N}.txt")
        try
            File.WriteAllText(nonExecutable, "not an executable")
            let refused =
                LocalProviderArtifactActivator.acquireAndLaunch
                    { Identity = "not-executable"
                      SourcePath = nonExecutable
                      Sha256 = cbi31Digest nonExecutable
                      Arguments = [ "--portable" ] }
                    { AllowedRoot = Path.GetTempPath()
                      AllowedArguments = [ "--portable" ] }
            match refused with
            | LocalProviderActivation.Refused failure ->
                Assert.That(failure.Code, Is.EqualTo "provider-process-start-failed")
            | LocalProviderActivation.Launched owner ->
                owner.Dispose()
                Assert.Fail("A non-executable artifact was launched.")
        finally
            File.Delete nonExecutable

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C4 owner composes with CBI30 and owns retirement cleanup``() =
        task {
            let! _, launched, _, active, released, retired, exited =
                cbi31Run (cbi31Vector "cbi31-02-minimal-artifact")
            multiple (fun () ->
                Assert.That(launched, Is.True)
                Assert.That(active, Is.True)
                Assert.That(released, Is.True)
                Assert.That(retired, Is.True)
                Assert.That(exited, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C5 both roots agree on portable observations``() =
        task {
            let! reference = cbi31Run (cbi31Vector "cbi31-01-reference-artifact")
            let! minimal = cbi31Run (cbi31Vector "cbi31-02-minimal-artifact")
            Assert.That(reference, Is.EqualTo minimal)
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI31 C4 owner cleanup terminates an unfinished process``() =
        let providerPath = cbi31ProviderPath "minimal"
        let allowedRoot =
            match Path.GetDirectoryName providerPath |> Option.ofObj with
            | Some value -> value
            | None -> failwith "CBI31 provider path must have a parent directory."
        let activation =
            LocalProviderArtifactActivator.acquireAndLaunch
                { Identity = "cleanup"
                  SourcePath = providerPath
                  Sha256 = cbi31Digest providerPath
                  Arguments = [ "--portable" ] }
                { AllowedRoot = allowedRoot
                  AllowedArguments = [ "--portable" ] }
        match activation with
        | LocalProviderActivation.Refused failure -> Assert.Fail(sprintf "Launch failed: %s" failure.Code)
        | LocalProviderActivation.Launched owner ->
            owner.Dispose()
            Assert.That(owner.HasExited, Is.True)

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``shared CBI32 vectors stage activate and remove content addressed sets``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi32-content-addressed-staging-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            for vector in fixture.RootElement.GetProperty("vectors").EnumerateArray() do
                let scenario = vector.GetProperty("id").GetString()
                let! observation = cbi32Run vector
                multiple (fun () ->
                    Assert.That(observation.StageCode, Is.EqualTo(vector.GetProperty("expectedStageCode").GetString()), scenario)
                    Assert.That(observation.Staged, Is.EqualTo(vector.GetProperty("expectedStaged").GetBoolean()), scenario)
                    Assert.That(observation.Active, Is.EqualTo(vector.GetProperty("expectedActivated").GetBoolean()), scenario)
                    Assert.That(observation.RemovalCode, Is.EqualTo(vector.GetProperty("expectedRemovalCode").GetString()), scenario)
                    Assert.That(observation.Residue, Is.False, scenario)
                    Assert.That(observation.Active, Is.EqualTo observation.Released, scenario)
                    Assert.That(observation.Active, Is.EqualTo observation.Retired, scenario))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C1 manifest is canonical and complete``() =
        task {
            let path =
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi32-content-addressed-staging-vectors.json")
            use fixture = JsonDocument.Parse(File.ReadAllText path)
            let canonical = fixture.RootElement.GetProperty("canonicalManifest")
            let requiredString (element: JsonElement) (name: string) : string =
                match element.GetProperty(name).GetString() |> Option.ofObj with
                | Some value -> value
                | None -> failwithf "CBI32 canonical property '%s' must be a string." name
            let files =
                canonical.GetProperty("files").EnumerateArray()
                |> Seq.map (fun file ->
                    { RelativePath = requiredString file "path"
                      Sha256 = requiredString file "sha256" })
                |> Seq.toList
            let arguments =
                canonical.GetProperty("arguments").EnumerateArray()
                |> Seq.map (fun value ->
                    match value.GetString() |> Option.ofObj with
                    | Some text -> text
                    | None -> failwith "CBI32 canonical arguments must be strings.")
                |> Seq.toList
            let computed =
                ProviderArtifactSetIdentity.compute
                    files
                    (requiredString canonical "executablePath")
                    arguments
            let! identity = cbi32Run (cbi32Vector "cbi32-05-identity-refused")
            let! traversal = cbi32Run (cbi32Vector "cbi32-06-traversal-refused")
            multiple (fun () ->
                Assert.That(
                    ProviderArtifactSetId.value computed,
                    Is.EqualTo(requiredString canonical "expectedIdentity"))
                Assert.That(identity.StageCode, Is.EqualTo "artifact-set-invalid")
                Assert.That(traversal.StageCode, Is.EqualTo "artifact-set-invalid")
                Assert.That(identity.Staged || traversal.Staged, Is.False))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C2 staging is verified and transactional``() =
        task {
            let! missing = cbi32Run (cbi32Vector "cbi32-03-member-unavailable")
            let! changed = cbi32Run (cbi32Vector "cbi32-04-member-integrity-refused")
            multiple (fun () ->
                Assert.That(missing.StageCode, Is.EqualTo "artifact-set-unavailable")
                Assert.That(changed.StageCode, Is.EqualTo "artifact-set-integrity-failed")
                Assert.That(missing.Residue || changed.Residue, Is.False))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C3 content identity reuses verified state and detects corruption``() =
        let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}")
        try
            let declaration = cbi32Declaration "minimal" (Path.Combine(testRoot, "source")) "none"
            let sourceBefore = declaration.Files |> List.map (fun file -> file.RelativePath, file.Sha256)
            let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
            let first =
                match store.Stage declaration with
                | ProviderArtifactStagingResult.Staged value -> value
                | ProviderArtifactStagingResult.Refused failure -> failwithf "Stage failed: %s" failure.Code
            let second =
                match store.Stage declaration with
                | ProviderArtifactStagingResult.Staged value -> value
                | ProviderArtifactStagingResult.Refused failure -> failwithf "Restage failed: %s" failure.Code
            let stagedPaths =
                Directory.EnumerateFiles(first.RootPath, "*", SearchOption.AllDirectories)
                |> Seq.map (fun path -> Path.GetRelativePath(first.RootPath, path))
                |> Seq.sort
                |> Seq.toList
            let sourceAfter =
                declaration.Files
                |> List.map (fun file -> file.RelativePath, cbi31Digest (Path.Combine(declaration.SourceRoot, file.RelativePath)))
            let stagedFile = Path.Combine(first.RootPath, declaration.Files.Head.RelativePath)
            File.SetAttributes(stagedFile, FileAttributes.Normal)
            File.WriteAllText(stagedFile, "corrupt")
            let corrupt = store.Stage declaration
            multiple (fun () ->
                Assert.That(second.Reused, Is.True)
                Assert.That((stagedPaths = (declaration.Files |> List.map _.RelativePath |> List.sort)), Is.True)
                Assert.That((sourceAfter = sourceBefore), Is.True)
                match corrupt with
                | ProviderArtifactStagingResult.Refused failure ->
                    Assert.That(failure.Code, Is.EqualTo "staged-artifact-integrity-failed")
                | ProviderArtifactStagingResult.Staged _ -> Assert.Fail("Corrupt staged content was reused."))
        finally
            cbi32DeleteTree testRoot

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C4 staging is inactive and composes with CBI31``() =
        task {
            let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}")
            try
                let declaration = cbi32Declaration "minimal" (Path.Combine(testRoot, "source")) "none"
                let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
                match store.Stage declaration with
                | ProviderArtifactStagingResult.Refused failure -> failwithf "Stage failed: %s" failure.Code
                | ProviderArtifactStagingResult.Staged _ -> ()
                Assert.That((store.Remove declaration.Identity).Code, Is.EqualTo "removed")
            finally
                cbi32DeleteTree testRoot
            let! observation = cbi32Run (cbi32Vector "cbi32-02-minimal-staged-activation")
            multiple (fun () ->
                Assert.That(observation.Staged, Is.True)
                Assert.That(observation.Active, Is.True)
                Assert.That(observation.Released, Is.True)
                Assert.That(observation.ProviderExited, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C5 removal respects active leases and exact ownership``() =
        task {
            let testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}")
            try
                let store = ContentAddressedProviderStore(Path.Combine(testRoot, "store"))
                let first = cbi32Declaration "minimal" (Path.Combine(testRoot, "source-a")) "none"
                let secondBase = cbi32Declaration "minimal" (Path.Combine(testRoot, "source-b")) "none"
                let secondArguments = [ "--portable"; "--second" ]
                let second =
                    { secondBase with
                        Identity =
                            ProviderArtifactSetIdentity.compute
                                secondBase.Files
                                secondBase.ExecutablePath
                                secondArguments
                        Arguments = secondArguments }
                match store.Stage first with
                | ProviderArtifactStagingResult.Refused failure -> failwithf "First stage failed: %s" failure.Code
                | ProviderArtifactStagingResult.Staged _ -> ()
                let stagedSecond =
                    match store.Stage second with
                    | ProviderArtifactStagingResult.Refused failure -> failwithf "Second stage failed: %s" failure.Code
                    | ProviderArtifactStagingResult.Staged value -> value
                Assert.That((store.Remove first.Identity).Code, Is.EqualTo "removed")
                Assert.That(Directory.Exists stagedSecond.RootPath, Is.True)
            finally
                cbi32DeleteTree testRoot
            let! observation = cbi32Run (cbi32Vector "cbi32-01-reference-staged-activation")
            multiple (fun () ->
                Assert.That(observation.ActiveRemovalCode, Is.EqualTo "artifact-set-in-use")
                Assert.That(observation.RemovalCode, Is.EqualTo "removed")
                Assert.That(observation.Residue, Is.False))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member _.``CBI32 C6 both roots agree on portable observations``() =
        task {
            let! reference = cbi32Run (cbi32Vector "cbi32-01-reference-staged-activation")
            let! minimal = cbi32Run (cbi32Vector "cbi32-02-minimal-staged-activation")
            Assert.That(reference, Is.EqualTo minimal)
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
                      Scope = None
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
                      Scope = None
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
                      Scope = None
                      Conversation =
                        conversationFor CoolingFixture.contract (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "authority-host-secondary" }
                      Scope = None
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
                      Scope = None
                      Conversation = conversationFor (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "revision-host-secondary" }
                      Scope = None
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
                      Scope = None
                      Conversation =
                        baseConversation CoolingFixture.contract (List.item 0 handlers) }
                let secondMember =
                    { Selection =
                        { selection (positionFor secondaryRequirementId) with
                            Requirement = secondaryRequirementId
                            HostEndpoint = "withdrawal-host-secondary" }
                      Scope = None
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
                    Scope = None
                    Conversation = conversationFor CoolingFixture.contract (List.item 0 handlers) }
                  { Selection =
                      { selection (positionFor secondaryRequirementId) with
                          Requirement = secondaryRequirementId
                          HostEndpoint = "group-host-secondary" }
                    Scope = None
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
                            Scope = None
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

    // CBI43 runs the distribution slices as one path. The harness lives beside the CBI30 helpers
    // because it needs the same prepared resolution, plan, and runtime request.
    member private _.Cbi43Run(mutation: string) =
        task {
            let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi43-{Guid.NewGuid():N}")
            Directory.CreateDirectory root |> ignore
            let mutable provider: StagedProviderProcess option = None
            try
                use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
                use publisher = ECDsa.Create ECCurve.NamedCurves.nistP256
                use endpointKey = ECDsa.Create ECCurve.NamedCurves.nistP256
                let digestOf (key: ECDsa) = key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                let authorityIdentity = digestOf authority |> ProviderPublisherTrustPolicyAuthorityId.create
                let endpointIdentity =
                    digestOf endpointKey |> ProviderPublisherTrustPolicyDistributionEndpointId.create

                let executable =
                    match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
                    | Some path when File.Exists path -> Path.GetFullPath path
                    | _ ->
                        Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
                        failwith "The cross-process test was ignored."
                let named (value: string | null) =
                    match value with null -> failwith "A provider path component was missing." | present -> present
                let providerRoot = Path.GetDirectoryName executable |> named
                let bytes =
                    Directory.EnumerateFiles providerRoot
                    |> Seq.sort
                    |> Seq.map (fun path -> Path.GetFileName path |> named, File.ReadAllBytes path)
                    |> Map.ofSeq
                let mutable files =
                    bytes
                    |> Map.toList
                    |> List.map (fun (path, content) ->
                        { RelativePath = path
                          Sha256 = SHA256.HashData content |> Convert.ToHexString
                          Length = int64 content.LongLength }: ProviderArtifactAcquisitionFile)
                if mutation = "delivered-digest-mismatch" then
                    files <- { files.Head with Sha256 = String('0', 64) } :: files.Tail
                let expectedSource = ProviderArtifactSourceId.create "fixture://brontide/provider-output"
                let artifactFiles =
                    files |> List.map (fun file ->
                        { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
                let executableName = Path.GetFileName executable |> named
                let request: ProviderArtifactAcquisitionRequest =
                    { ExpectedSource = expectedSource
                      Identity = ProviderArtifactSetIdentity.compute artifactFiles executableName [ "--portable" ]
                      Files = files
                      ExecutablePath = executableName
                      Arguments = [ "--portable" ]
                      MaxTotalBytes = files |> List.sumBy _.Length }
                let opens = ref 0
                let source =
                    { new IProviderArtifactSource with
                        member _.Identity = expectedSource
                        member _.OpenRead relativePath =
                            opens.Value <- opens.Value + 1
                            bytes
                            |> Map.tryFind relativePath
                            |> Option.map (fun content -> new MemoryStream(content, false) :> Stream) }

                let publisherKey = digestOf publisher |> ProviderPublisherKeyId.create
                let evidence: ProviderPublisherEvidence =
                    { PublisherKeyId = publisherKey
                      Algorithm = "ECDSA-P256-SHA256"
                      PublicKeySpkiBase64 = publisher.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        publisher.SignData(
                            ProviderArtifactPublisherManifest.encode request,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }
                let admitted =
                    if mutation = "publisher-unknown" then ProviderPublisherKeyId.create (String('7', 64))
                    else publisherKey
                let entries =
                    [ { PublisherKeyId = admitted
                        Disposition = if mutation = "publisher-revoked" then Revoked else Admitted } ]
                let policy = { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }
                let update =
                    { Sequence = 1L; PreviousPolicyIdentity = None; Policy = policy
                      Algorithm = "ECDSA-P256-SHA256"
                      AuthorityPublicKeySpkiBase64 = authority.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        authority.SignData(
                            ProviderPublisherTrustPolicyUpdateManifest.encode 1L None policy.Identity,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }

                // 1. Custody, then one poll that either delivers the policy or does not.
                let custodyCode, _, registry, floors =
                    ProviderPublisherTrustPolicyCustody.open'
                        (Path.Combine(root, "policy.checkpoint")) (Path.Combine(root, "policy.floor"))
                        authorityIdentity
                Assert.That(custodyCode, Is.EqualTo "policy-floor-opened")
                let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
                let served = ref 0
                let pollSource =
                    { new IProviderPublisherTrustPolicyDistributionSource with
                        member _.FetchAsync(distribution, _) =
                            let selected =
                                if mutation <> "policy-undelivered" && served.Value = 0 then Some update else None
                            served.Value <- served.Value + 1
                            let issued, expires = now.ToUnixTimeSeconds(), now.AddMinutes(1.0).ToUnixTimeSeconds()
                            let signature =
                                endpointKey.SignData(
                                    ProviderPublisherTrustPolicyDistributionManifest.encode distribution.Challenge
                                        distribution.CurrentSequence distribution.CurrentPolicyIdentity issued
                                        expires selected,
                                    HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                            Task.FromResult
                                { Challenge = distribution.Challenge
                                  CurrentSequence = distribution.CurrentSequence
                                  CurrentPolicyIdentity = distribution.CurrentPolicyIdentity
                                  IssuedAtUnixSeconds = issued; ExpiresAtUnixSeconds = expires
                                  Update = selected; Algorithm = "ECDSA-P256-SHA256"
                                  EndpointPublicKeySpkiBase64 =
                                    endpointKey.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                                  SignatureBase64 = Convert.ToBase64String signature } }
                let schedule =
                    ProviderPublisherTrustPolicyPollSchedule.create 4 (TimeSpan.FromSeconds 1.0) 2
                        (TimeSpan.FromSeconds 4.0) (TimeSpan.FromSeconds 1.0)
                let delay: ProviderPublisherTrustPolicyPollDelay =
                    fun instant duration _ -> Task.FromResult(instant + duration)
                let poller = ProviderPublisherTrustPolicyPoller(registry.Value, endpointIdentity, schedule)
                let! poll = poller.PollAsync(pollSource, floors.Value.Sink, delay, now, Threading.CancellationToken.None)
                Assert.That(poll.Code, Is.EqualTo "policy-poll-current")
                let policyApplied = registry.Value.Current.IsSome

                // 2-5. Evidence, trust, governed acquisition, staging, and launch.
                let storeRoot = Path.GetFullPath(Path.Combine(root, "store"))
                let store = ContentAddressedProviderStore storeRoot
                let chainRequest: ProviderDistributionChainRequest =
                    { Acquisition = request
                      Evidence = if mutation = "evidence-unsigned" then None else Some evidence
                      AllowedArguments =
                        if mutation = "launch-refused" then [ "--not-allowed" ] else [ "--portable" ] }
                let chain =
                    ProviderDistributionChain.run registry.Value store (Path.Combine(root, "transactions"))
                        chainRequest source
                provider <- chain.Provider
                let executableInsideStore =
                    chain.StagedExecutablePath
                    |> Option.exists (fun path ->
                        path.StartsWith(storeRoot + string Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && File.Exists path)

                let mutable released = false
                let mutable outcome = chain.Code
                let mutable refusedBy = Some chain.RefusedBy
                let mutable stoppedEarly = false
                match provider with
                | None -> ()
                | Some launched ->
                    // 6. CBI30 activation across the launched provider's own conversation.
                    if mutation = "provider-lost" then
                        launched.Dispose()
                        stoppedEarly <- true
                    let resolution, selected, occurrence = prepared ()
                    let! result =
                        ComponentBindingLifecycle.activate resolution selected
                            (runtimeRequest (plan [ occurrence ])) launched.Conversation
                    released <- result.Member |> Option.exists _.IsReleased
                    let active =
                        result.Failure.IsNone
                        && result.Runtime
                           |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                        && released
                    outcome <- result.Failure |> Option.map _.Code |> Option.defaultValue "active"
                    refusedBy <- if active then None else Some "cbi30"
                    if active then
                        let! _ = result.Member.Value.Retire "CBI43 chain completed."
                        ()

                // The question is whether the chain leaves a process behind once it has returned, so
                // the exit is observed after teardown rather than during it.
                let mutable running = false
                match provider with
                | Some launched ->
                    if not stoppedEarly then
                        running <- not (launched.WaitForExit(TimeSpan.FromSeconds 5.0))
                        launched.Dispose()
                    store.Remove chain.StagedIdentity.Value |> ignore
                    provider <- None
                | None -> ()

                return
                    { Code = outcome
                      RefusedBy = refusedBy
                      PolicyApplied = policyApplied
                      Authorized = chain.Authorized
                      SourceOpened = opens.Value > 0
                      Staged = chain.Staged
                      Launched = chain.IsLaunched
                      Released = released
                      StoredFloor = floors.Value.Stored.Sequence
                      StagedSetRemains =
                        Directory.Exists storeRoot
                        && (Directory.EnumerateDirectories storeRoot |> Seq.isEmpty |> not)
                      ProviderRunning = running
                      ExecutableInsideStore = executableInsideStore }
            finally
                provider |> Option.iter _.Dispose()
                try Directory.Delete(root, true) with _ -> ()
        }

    member private _.Cbi43Fixture() =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi43-distribution-chain-vectors.json")))

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``shared CBI43 vectors run the distribution chain end to end``() =
        task {
            use document = this.Cbi43Fixture()
            let optional (value: string | null) = match value with null -> None | present -> Some present
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                let label = vector.GetProperty("mutation").GetString() |> optional |> Option.defaultValue ""
                let! actual = this.Cbi43Run label
                multiple (fun () ->
                    Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label)
                    Assert.That(actual.RefusedBy,
                        Is.EqualTo(vector.GetProperty("refusedBy").GetString() |> optional), label)
                    Assert.That(actual.PolicyApplied,
                        Is.EqualTo(vector.GetProperty("policyApplied").GetBoolean()), label)
                    Assert.That(actual.Authorized,
                        Is.EqualTo(vector.GetProperty("authorized").GetBoolean()), label)
                    Assert.That(actual.SourceOpened,
                        Is.EqualTo(vector.GetProperty("sourceOpened").GetBoolean()), label)
                    Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()), label)
                    Assert.That(actual.Launched, Is.EqualTo(vector.GetProperty("launched").GetBoolean()), label)
                    Assert.That(actual.Released, Is.EqualTo(vector.GetProperty("released").GetBoolean()), label)
                    Assert.That(actual.StoredFloor,
                        Is.EqualTo(vector.GetProperty("storedFloor").GetInt64()), label)
                    Assert.That(actual.StagedSetRemains,
                        Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label)
                    Assert.That(actual.ProviderRunning,
                        Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label)

                    // Phase-wide properties, over every vector rather than per case.
                    let ladder =
                        [ actual.PolicyApplied; actual.Authorized; actual.SourceOpened
                          actual.Staged; actual.Launched; actual.Released ]
                    Assert.That(ladder |> List.skipWhile id |> List.exists id, Is.False,
                        $"{label}: the ladder must be a true-prefix")
                    Assert.That(actual.StagedSetRemains, Is.False, label)
                    Assert.That(actual.ProviderRunning, Is.False, label)
                    if not actual.SourceOpened then Assert.That(actual.Staged, Is.False, label))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI43 C1 the chain composes from polled policy to released member``() =
        task {
            let! actual = this.Cbi43Run "complete"
            multiple (fun () ->
                Assert.That(actual.Code, Is.EqualTo "active")
                Assert.That(actual.Released, Is.True)
                Assert.That(actual.PolicyApplied, Is.True)
                Assert.That(actual.Launched, Is.True)
                // The executable ran from inside the content-addressed store rather than from the
                // source the caller named, so activation used the bytes the publisher signed.
                Assert.That(actual.ExecutableInsideStore, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI43 C3 a policy that never applied opens no source``() =
        task {
            for mutation in [ "policy-undelivered"; "publisher-revoked"; "publisher-unknown"; "evidence-unsigned" ] do
                let! actual = this.Cbi43Run mutation
                multiple (fun () ->
                    Assert.That(actual.SourceOpened, Is.False, mutation)
                    Assert.That(actual.Staged, Is.False, mutation)
                    Assert.That(actual.Launched, Is.False, mutation))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI43 C4 a refusal leaves no staged set process or advanced floor``() =
        task {
            use document = this.Cbi43Fixture()
            let optional (value: string | null) = match value with null -> None | present -> Some present
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                if not (vector.GetProperty("released").GetBoolean()) then
                    let label = vector.GetProperty("mutation").GetString() |> optional |> Option.defaultValue ""
                    let! actual = this.Cbi43Run label
                    multiple (fun () ->
                        Assert.That(actual.StagedSetRemains, Is.False, label)
                        Assert.That(actual.ProviderRunning, Is.False, label)
                        Assert.That(actual.StoredFloor,
                            Is.EqualTo(if actual.PolicyApplied then 1L else 0L), label))
        }

    // CBI44 takes the launch decision against the policy in force rather than spending the one that
    // authorized acquisition. The window it closes exists only while one chain call is in flight, so
    // the fixture advances the registry from the artifact source - the same device CBI41 uses to
    // reach CBI39's superseded cursor. The write lands after the governed acquirer has already
    // checked supersession, which is what makes it the post-acquisition window rather than CBI36's.
    member private _.Cbi44Run(mutation: string) =
        task {
            let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi44-{Guid.NewGuid():N}")
            Directory.CreateDirectory root |> ignore
            let mutable provider: StagedProviderProcess option = None
            try
                use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
                use publisher = ECDsa.Create ECCurve.NamedCurves.nistP256
                use endpointKey = ECDsa.Create ECCurve.NamedCurves.nistP256
                let digestOf (key: ECDsa) = key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                let authorityIdentity = digestOf authority |> ProviderPublisherTrustPolicyAuthorityId.create
                let endpointIdentity =
                    digestOf endpointKey |> ProviderPublisherTrustPolicyDistributionEndpointId.create

                let executable =
                    match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
                    | Some path when File.Exists path -> Path.GetFullPath path
                    | _ ->
                        Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
                        failwith "The cross-process test was ignored."
                let named (value: string | null) =
                    match value with null -> failwith "A provider path component was missing." | present -> present
                let providerRoot = Path.GetDirectoryName executable |> named
                let bytes =
                    Directory.EnumerateFiles providerRoot
                    |> Seq.sort
                    |> Seq.map (fun path -> Path.GetFileName path |> named, File.ReadAllBytes path)
                    |> Map.ofSeq
                let files =
                    bytes
                    |> Map.toList
                    |> List.map (fun (path, content) ->
                        { RelativePath = path
                          Sha256 = SHA256.HashData content |> Convert.ToHexString
                          Length = int64 content.LongLength }: ProviderArtifactAcquisitionFile)
                let expectedSource = ProviderArtifactSourceId.create "fixture://brontide/provider-output"
                let executableName = Path.GetFileName executable |> named
                let artifacts =
                    files
                    |> List.map (fun file ->
                        { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
                let request: ProviderArtifactAcquisitionRequest =
                    { ExpectedSource = expectedSource
                      Identity = ProviderArtifactSetIdentity.compute artifacts executableName [ "--portable" ]
                      Files = files
                      ExecutablePath = executableName
                      Arguments = [ "--portable" ]
                      MaxTotalBytes = files |> List.sumBy _.Length }

                let publisherKey = digestOf publisher |> ProviderPublisherKeyId.create
                let evidence: ProviderPublisherEvidence =
                    { PublisherKeyId = publisherKey
                      Algorithm = "ECDSA-P256-SHA256"
                      PublicKeySpkiBase64 = publisher.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        publisher.SignData(
                            ProviderArtifactPublisherManifest.encode request,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }

                let policyOf entries : ProviderPublisherTrustPolicy =
                    { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }
                let otherPublisher = ProviderPublisherKeyId.create (String('B', 64))
                let initial =
                    policyOf
                        [ { PublisherKeyId = publisherKey
                            Disposition = if mutation = "revoked-before-acquisition" then Revoked else Admitted } ]
                let successor =
                    match mutation with
                    | "revoked-at-launch" ->
                        Some(policyOf [ { PublisherKeyId = publisherKey; Disposition = Revoked } ])
                    // The successor simply stops naming the publisher, which CBI35 keeps distinct
                    // from revoking it.
                    | "removed-at-launch" ->
                        Some(policyOf [ { PublisherKeyId = otherPublisher; Disposition = Admitted } ])
                    // A real update with nothing to do with this publisher. It moves the policy
                    // identity, so a chain comparing snapshots rather than decisions refuses here.
                    | "unrelated-revocation" ->
                        Some(policyOf
                                 [ { PublisherKeyId = publisherKey; Disposition = Admitted }
                                   { PublisherKeyId = otherPublisher; Disposition = Revoked } ])
                    | _ -> None
                let signUpdate sequence previous (policy: ProviderPublisherTrustPolicy) : ProviderPublisherTrustPolicyUpdate =
                    { Sequence = sequence
                      PreviousPolicyIdentity = previous
                      Policy = policy
                      Algorithm = "ECDSA-P256-SHA256"
                      AuthorityPublicKeySpkiBase64 = authority.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        authority.SignData(
                            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }

                // 1. Custody, then one poll that applies exactly the first policy.
                let custodyCode, _, registryOption, floors =
                    ProviderPublisherTrustPolicyCustody.open'
                        (Path.Combine(root, "policy.checkpoint")) (Path.Combine(root, "policy.floor"))
                        authorityIdentity
                Assert.That(custodyCode, Is.EqualTo "policy-floor-opened")
                let registry = registryOption.Value
                let now = DateTimeOffset.FromUnixTimeSeconds 1800000000L
                let update = signUpdate 1L None initial
                let served = ref 0
                let pollSource =
                    { new IProviderPublisherTrustPolicyDistributionSource with
                        member _.FetchAsync(distribution, _) =
                            let selected = if served.Value = 0 then Some update else None
                            served.Value <- served.Value + 1
                            let issued, expires = now.ToUnixTimeSeconds(), now.AddMinutes(1.0).ToUnixTimeSeconds()
                            let signature =
                                endpointKey.SignData(
                                    ProviderPublisherTrustPolicyDistributionManifest.encode distribution.Challenge
                                        distribution.CurrentSequence distribution.CurrentPolicyIdentity issued
                                        expires selected,
                                    HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                            Task.FromResult
                                { Challenge = distribution.Challenge
                                  CurrentSequence = distribution.CurrentSequence
                                  CurrentPolicyIdentity = distribution.CurrentPolicyIdentity
                                  IssuedAtUnixSeconds = issued; ExpiresAtUnixSeconds = expires
                                  Update = selected; Algorithm = "ECDSA-P256-SHA256"
                                  EndpointPublicKeySpkiBase64 =
                                    endpointKey.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                                  SignatureBase64 = Convert.ToBase64String signature } }
                let schedule =
                    ProviderPublisherTrustPolicyPollSchedule.create 4 (TimeSpan.FromSeconds 1.0) 2
                        (TimeSpan.FromSeconds 4.0) (TimeSpan.FromSeconds 1.0)
                let delay: ProviderPublisherTrustPolicyPollDelay =
                    fun instant duration _ -> Task.FromResult(instant + duration)
                let poller = ProviderPublisherTrustPolicyPoller(registry, endpointIdentity, schedule)
                let! poll = poller.PollAsync(pollSource, floors.Value.Sink, delay, now, Threading.CancellationToken.None)
                Assert.That(poll.Code, Is.EqualTo "policy-poll-current")

                // 2. The successor is applied from inside the acquisition rather than by a second
                // poll, so no floor is handed off and the stored floor stays behind the live
                // sequence - which is CBI41's lagging floor, not a defect in this chain.
                let opens = ref 0
                let advanced = ref false
                let source =
                    { new IProviderArtifactSource with
                        member _.Identity = expectedSource
                        member _.OpenRead relativePath =
                            if not advanced.Value then
                                advanced.Value <- true
                                successor
                                |> Option.iter (fun policy ->
                                    Assert.That(
                                        registry.Apply(signUpdate 2L (Some initial.Identity) policy).IsApplied,
                                        Is.True, "the fixture's own successor must apply"))
                            opens.Value <- opens.Value + 1
                            bytes
                            |> Map.tryFind relativePath
                            |> Option.map (fun content -> new MemoryStream(content, false) :> Stream) }

                let storeRoot = Path.GetFullPath(Path.Combine(root, "store"))
                let store = ContentAddressedProviderStore storeRoot
                let chainRequest: ProviderDistributionChainRequest =
                    { Acquisition = request
                      Evidence = Some evidence
                      AllowedArguments =
                        if mutation = "launch-refused" then [ "--not-allowed" ] else [ "--portable" ] }
                let chain =
                    ProviderDistributionChain.run registry store (Path.Combine(root, "transactions"))
                        chainRequest source
                provider <- chain.Provider

                let mutable released = false
                let mutable outcome = chain.Code
                let mutable refusedBy = Some chain.RefusedBy
                match provider with
                | None -> ()
                | Some launched ->
                    let resolution, selected, occurrence = prepared ()
                    let! result =
                        ComponentBindingLifecycle.activate resolution selected
                            (runtimeRequest (plan [ occurrence ])) launched.Conversation
                    released <- result.Member |> Option.exists _.IsReleased
                    let active =
                        result.Failure.IsNone
                        && result.Runtime
                           |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active)
                        && released
                    outcome <- result.Failure |> Option.map _.Code |> Option.defaultValue "active"
                    refusedBy <- if active then None else Some "cbi30"
                    if active then
                        let! _ = result.Member.Value.Retire "CBI44 chain completed."
                        ()

                let mutable running = false
                match provider with
                | Some launched ->
                    running <- not (launched.WaitForExit(TimeSpan.FromSeconds 5.0))
                    launched.Dispose()
                    store.Remove chain.StagedIdentity.Value |> ignore
                    provider <- None
                | None -> ()

                let final = registry.Current.Value
                return
                    { Code = outcome
                      RefusedBy = refusedBy
                      PolicyApplied = true
                      Authorized = chain.Authorized
                      SourceOpened = opens.Value > 0
                      Staged = chain.Staged
                      Revalidated = chain.Revalidated
                      Launched = chain.IsLaunched
                      Released = released
                      LaunchPolicyChanged =
                        chain.LaunchPolicyIdentity
                        |> Option.map (fun identity -> Some identity <> chain.AcquisitionPolicyIdentity)
                      RegistrySequence = final.Sequence
                      StoredFloor = floors.Value.Stored.Sequence
                      StagedSetRemains =
                        Directory.Exists storeRoot
                        && (Directory.EnumerateDirectories storeRoot |> Seq.isEmpty |> not)
                      ProviderRunning = running
                      LaunchPolicyIsCurrent =
                        chain.LaunchPolicyIdentity
                        |> Option.forall (fun identity -> identity = final.Policy.Identity)
                      LaunchAdmitsPublisher =
                        final.Policy.Entries
                        |> List.exists (fun entry ->
                            entry.PublisherKeyId = publisherKey && entry.Disposition = Admitted)
                      StagedIsRequested =
                        chain.StagedIdentity |> Option.forall (fun staged -> staged = request.Identity) }
            finally
                provider |> Option.iter _.Dispose()
                try Directory.Delete(root, true) with _ -> ()
        }

    member private _.Cbi44Fixture() =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi44-launch-revalidation-vectors.json")))

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``shared CBI44 vectors revalidate trust between acquisition and launch``() =
        task {
            use document = this.Cbi44Fixture()
            let optional (value: string | null) = match value with null -> None | present -> Some present
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                let label = vector.GetProperty("mutation").GetString() |> optional |> Option.defaultValue ""
                let! actual = this.Cbi44Run label
                let expectedChange =
                    let element = vector.GetProperty "launchPolicyChanged"
                    if element.ValueKind = JsonValueKind.Null then None else Some(element.GetBoolean())
                multiple (fun () ->
                    Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label)
                    Assert.That(actual.RefusedBy,
                        Is.EqualTo(vector.GetProperty("refusedBy").GetString() |> optional), label)
                    Assert.That(actual.PolicyApplied,
                        Is.EqualTo(vector.GetProperty("policyApplied").GetBoolean()), label)
                    Assert.That(actual.Authorized,
                        Is.EqualTo(vector.GetProperty("authorized").GetBoolean()), label)
                    Assert.That(actual.SourceOpened,
                        Is.EqualTo(vector.GetProperty("sourceOpened").GetBoolean()), label)
                    Assert.That(actual.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()), label)
                    Assert.That(actual.Revalidated,
                        Is.EqualTo(vector.GetProperty("revalidated").GetBoolean()), label)
                    Assert.That(actual.Launched, Is.EqualTo(vector.GetProperty("launched").GetBoolean()), label)
                    Assert.That(actual.Released, Is.EqualTo(vector.GetProperty("released").GetBoolean()), label)
                    Assert.That(actual.LaunchPolicyChanged, Is.EqualTo expectedChange, label)
                    Assert.That(actual.RegistrySequence,
                        Is.EqualTo(vector.GetProperty("registrySequence").GetInt64()), label)
                    Assert.That(actual.StoredFloor,
                        Is.EqualTo(vector.GetProperty("storedFloor").GetInt64()), label)
                    Assert.That(actual.StagedSetRemains,
                        Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label)
                    Assert.That(actual.ProviderRunning,
                        Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label)

                    // Phase-wide properties, over every vector rather than per case.
                    let ladder =
                        [ actual.PolicyApplied; actual.Authorized; actual.SourceOpened; actual.Staged
                          actual.Revalidated; actual.Launched; actual.Released ]
                    Assert.That(ladder |> List.skipWhile id |> List.exists id, Is.False,
                        $"{label}: the ladder must be a true-prefix")
                    if actual.Launched then Assert.That(actual.LaunchAdmitsPublisher, Is.True, label)
                    Assert.That(actual.LaunchPolicyIsCurrent, Is.True, label)
                    Assert.That(actual.StagedIsRequested, Is.True, label)
                    if not actual.Launched then
                        Assert.That(actual.StagedSetRemains, Is.False, label)
                        Assert.That(actual.ProviderRunning, Is.False, label))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI44 C1 the launch decision is taken not remembered``() =
        task {
            let! actual = this.Cbi44Run "complete"
            multiple (fun () ->
                Assert.That(actual.Revalidated, Is.True)
                Assert.That(actual.Released, Is.True)
                // Nothing moved, so the two decisions name the same policy - and both were taken.
                Assert.That(actual.LaunchPolicyChanged, Is.EqualTo(Some false))
                Assert.That(actual.LaunchPolicyIsCurrent, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI44 C2 a publisher the current policy no longer admits does not launch``() =
        task {
            for mutation in [ "revoked-at-launch"; "removed-at-launch" ] do
                let! actual = this.Cbi44Run mutation
                multiple (fun () ->
                    Assert.That(actual.Staged, Is.True, mutation)
                    Assert.That(actual.Revalidated, Is.False, mutation)
                    Assert.That(actual.Launched, Is.False, mutation)
                    Assert.That(actual.RefusedBy, Is.EqualTo(Some "cbi35"), mutation))

            // The same code and the same origin as an acquisition-time revocation. Only the ladder
            // separates them, which is what CBI43's C2 exists for.
            let! early = this.Cbi44Run "revoked-before-acquisition"
            let! late = this.Cbi44Run "revoked-at-launch"
            multiple (fun () ->
                Assert.That(late.Code, Is.EqualTo early.Code)
                Assert.That(late.RefusedBy, Is.EqualTo early.RefusedBy)
                Assert.That(early.Authorized, Is.False)
                Assert.That(late.Authorized, Is.True)
                Assert.That(early.SourceOpened, Is.False)
                Assert.That(late.SourceOpened, Is.True)
                Assert.That(early.Staged, Is.False)
                Assert.That(late.Staged, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI44 C3 a changed policy that still admits the publisher launches``() =
        task {
            let! actual = this.Cbi44Run "unrelated-revocation"
            multiple (fun () ->
                // The snapshot moved and the decision did not, so a chain comparing policy
                // identities would refuse this and a chain comparing decisions runs it.
                Assert.That(actual.LaunchPolicyChanged, Is.EqualTo(Some true))
                Assert.That(actual.RegistrySequence, Is.EqualTo 2L)
                Assert.That(actual.Revalidated, Is.True)
                Assert.That(actual.Released, Is.True))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI44 C4 a refused launch leaves no staged set process or advanced floor``() =
        task {
            use document = this.Cbi44Fixture()
            let optional (value: string | null) = match value with null -> None | present -> Some present
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                if not (vector.GetProperty("released").GetBoolean()) then
                    let label = vector.GetProperty("mutation").GetString() |> optional |> Option.defaultValue ""
                    let! actual = this.Cbi44Run label
                    multiple (fun () ->
                        Assert.That(actual.StagedSetRemains, Is.False, label)
                        Assert.That(actual.ProviderRunning, Is.False, label)
                        // One poll applied one update, so the floor is one however far the live
                        // registry ran.
                        Assert.That(actual.StoredFloor, Is.EqualTo 1L, label))
        }

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI44 C5 the ladder gains a stage and stays a true-prefix``() =
        task {
            // A refusal after the launch decision proves the new stage sits before launch rather
            // than standing in for it: revalidated is true and launched is false in one vector.
            let! actual = this.Cbi44Run "launch-refused"
            multiple (fun () ->
                Assert.That(actual.Revalidated, Is.True)
                Assert.That(actual.Launched, Is.False)
                Assert.That(actual.RefusedBy, Is.EqualTo(Some "cbi31")))
        }

    member private _.Cbi45Run(mutation: string, repeat: bool) =
        task {
            let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi45-{Guid.NewGuid():N}")
            Directory.CreateDirectory root |> ignore
            let mutable provider: StagedProviderProcess option = None
            try
                use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
                use publisher = ECDsa.Create ECCurve.NamedCurves.nistP256
                let digestOf (key: ECDsa) =
                    key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                let authorityIdentity = digestOf authority |> ProviderPublisherTrustPolicyAuthorityId.create
                let executable =
                    match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
                    | Some path when File.Exists path -> Path.GetFullPath path
                    | _ ->
                        Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
                        failwith "The cross-process test was ignored."
                let named (value: string | null) =
                    match value with null -> failwith "A provider path component was missing." | present -> present
                let providerRoot = Path.GetDirectoryName executable |> named
                let bytes =
                    Directory.EnumerateFiles providerRoot
                    |> Seq.sort
                    |> Seq.map (fun path -> Path.GetFileName path |> named, File.ReadAllBytes path)
                    |> Map.ofSeq
                let files =
                    bytes |> Map.toList |> List.map (fun (path, content) ->
                        { RelativePath = path
                          Sha256 = SHA256.HashData content |> Convert.ToHexString
                          Length = int64 content.LongLength }: ProviderArtifactAcquisitionFile)
                let expectedSource = ProviderArtifactSourceId.create "fixture://brontide/provider-output"
                let executableName = Path.GetFileName executable |> named
                let artifacts =
                    files |> List.map (fun file ->
                        { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
                let request: ProviderArtifactAcquisitionRequest =
                    { ExpectedSource = expectedSource
                      Identity = ProviderArtifactSetIdentity.compute artifacts executableName [ "--portable" ]
                      Files = files; ExecutablePath = executableName; Arguments = [ "--portable" ]
                      MaxTotalBytes = files |> List.sumBy _.Length }
                let publisherKey = digestOf publisher |> ProviderPublisherKeyId.create
                let evidence: ProviderPublisherEvidence =
                    { PublisherKeyId = publisherKey
                      Algorithm = "ECDSA-P256-SHA256"
                      PublicKeySpkiBase64 = publisher.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        publisher.SignData(
                            ProviderArtifactPublisherManifest.encode request,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }
                let policyOf entries: ProviderPublisherTrustPolicy =
                    { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }
                let otherPublisher = ProviderPublisherKeyId.create (String('B', 64))
                let initial =
                    policyOf
                        [ { PublisherKeyId = publisherKey; Disposition = Admitted }
                          { PublisherKeyId = otherPublisher; Disposition = Admitted } ]
                let successor =
                    match mutation with
                    | "publisher-revoked" ->
                        policyOf [ { PublisherKeyId = publisherKey; Disposition = Revoked } ]
                    | "publisher-removed" ->
                        policyOf [ { PublisherKeyId = otherPublisher; Disposition = Admitted } ]
                    | "unrelated-revocation" ->
                        policyOf
                            [ { PublisherKeyId = publisherKey; Disposition = Admitted }
                              { PublisherKeyId = otherPublisher; Disposition = Revoked } ]
                    | _ -> initial
                let signUpdate sequence previous (policy: ProviderPublisherTrustPolicy) =
                    { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy
                      Algorithm = "ECDSA-P256-SHA256"
                      AuthorityPublicKeySpkiBase64 =
                        authority.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        authority.SignData(
                            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }: ProviderPublisherTrustPolicyUpdate
                let openedCode, openedRegistry, _ =
                    DurableProviderPublisherTrustPolicyRegistry.Open(
                        Path.Combine(root, "policy.checkpoint"), authorityIdentity, None)
                Assert.That(openedCode, Is.EqualTo "policy-checkpoint-empty")
                let registry = openedRegistry.Value
                Assert.That(registry.Apply(signUpdate 1L None initial).IsApplied, Is.True)
                let source =
                    { new IProviderArtifactSource with
                        member _.Identity = expectedSource
                        member _.OpenRead relativePath =
                            bytes |> Map.tryFind relativePath
                            |> Option.map (fun content -> new MemoryStream(content, false) :> Stream) }
                let storeRoot = Path.GetFullPath(Path.Combine(root, "store"))
                let store = ContentAddressedProviderStore storeRoot
                let chain =
                    ProviderDistributionChain.run registry store (Path.Combine(root, "transactions"))
                        { Acquisition = request; Evidence = Some evidence; AllowedArguments = [ "--portable" ] }
                        source
                provider <- chain.Provider
                let launched = chain.Provider.Value
                let resolution, selected, occurrence = prepared ()
                let! activation =
                    ProviderServingTrustRevalidation.activate chain resolution selected
                        (runtimeRequest (plan [ occurrence ]))
                Assert.That(activation.IsServing, Is.True)
                if mutation <> "unchanged" then
                    Assert.That(
                        registry.Apply(signUpdate 2L (Some initial.Identity) successor).IsApplied,
                        Is.True)
                let! result =
                    ProviderServingTrustRevalidation.revalidate registry store activation
                        "publisher trust lapsed"
                if repeat then
                    let! repeated =
                        ProviderServingTrustRevalidation.revalidate registry store activation
                            "publisher trust still lapsed"
                    multiple (fun () ->
                        Assert.That(repeated.Code, Is.EqualTo "serving-activation-unavailable")
                        Assert.That(repeated.Revalidated, Is.False))
                let current = registry.Current.Value
                let observation =
                    { Code = result.Code
                      RefusedBy = if result.RefusedBy = "none" then None else Some result.RefusedBy
                      Revalidated = result.Revalidated; Continued = result.Continued
                      PolicyChanged = result.ServingPolicyIdentity <> chain.LaunchPolicyIdentity
                      MemberReleased = activation.MemberReleased
                      ProviderRunning = not launched.HasExited
                      StagedSetRemains =
                        Directory.Exists storeRoot
                        && (Directory.EnumerateDirectories storeRoot |> Seq.isEmpty |> not)
                      ServingPolicyIsCurrent =
                        result.ServingPolicyIdentity |> Option.forall ((=) current.Policy.Identity)
                      DecisionMatchesStagedIdentity =
                        match result.Authorization, chain.StagedIdentity with
                        | Some authorization, Some staged -> authorization.ContentIdentity = staged
                        | None, _ -> true
                        | _ -> false }
                if result.Continued then
                    do! activation.Retire "CBI45 test completed."
                if not launched.HasExited then launched.Dispose()
                store.Remove chain.StagedIdentity.Value |> ignore
                provider <- None
                return observation
            finally
                provider |> Option.iter _.Dispose()
                try Directory.Delete(root, true) with _ -> ()
        }

    member private _.Cbi45Fixture() =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi45-serving-revalidation-vectors.json")))

    [<Test>]
    [<Category("CrossProcess")>]
    member this.``CBI45 C6 both roots execute the shared serving vectors``() =
        task {
            use document = this.Cbi45Fixture()
            let optional (value: string | null) = match value with null -> None | present -> Some present
            for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
                let label = vector.GetProperty("mutation").GetString() |> optional |> Option.defaultValue ""
                let! actual = this.Cbi45Run(label, false)
                multiple (fun () ->
                    Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("code").GetString()), label)
                    Assert.That(actual.RefusedBy,
                        Is.EqualTo(vector.GetProperty("refusedBy").GetString() |> optional), label)
                    Assert.That(actual.Revalidated, Is.EqualTo(vector.GetProperty("revalidated").GetBoolean()), label)
                    Assert.That(actual.Continued, Is.EqualTo(vector.GetProperty("continued").GetBoolean()), label)
                    Assert.That(actual.PolicyChanged, Is.EqualTo(vector.GetProperty("policyChanged").GetBoolean()), label)
                    Assert.That(actual.MemberReleased, Is.EqualTo(vector.GetProperty("memberReleased").GetBoolean()), label)
                    Assert.That(actual.ProviderRunning, Is.EqualTo(vector.GetProperty("providerRunning").GetBoolean()), label)
                    Assert.That(actual.StagedSetRemains, Is.EqualTo(vector.GetProperty("stagedSetRemains").GetBoolean()), label)
                    Assert.That(actual.ServingPolicyIsCurrent, Is.True, label)
                    Assert.That(actual.DecisionMatchesStagedIdentity, Is.True, label))
        }

    [<Test; Category("CrossProcess")>]
    member this.``CBI45 C1 the serving decision is current``() = task {
        let! actual = this.Cbi45Run("unchanged", false)
        Assert.That(actual.Revalidated, Is.True) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI45 C2 lapsed trust stops service``() = task {
        for mutation in [ "publisher-revoked"; "publisher-removed" ] do
            let! actual = this.Cbi45Run(mutation, false)
            multiple (fun () ->
                Assert.That(actual.Continued, Is.False, mutation)
                Assert.That(actual.MemberReleased, Is.False, mutation)
                Assert.That(actual.ProviderRunning, Is.False, mutation)
                Assert.That(actual.StagedSetRemains, Is.False, mutation)) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI45 C3 an unrelated policy change preserves service``() = task {
        let! actual = this.Cbi45Run("unrelated-revocation", false)
        Assert.That(actual.Continued, Is.True) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI45 C4 retained verified evidence is evaluated``() = task {
        let! actual = this.Cbi45Run("unchanged", false)
        Assert.That(actual.DecisionMatchesStagedIdentity, Is.True) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI45 C5 a withdrawn activation cannot be revalidated twice``() = task {
        let! actual = this.Cbi45Run("publisher-revoked", true)
        Assert.That(actual.Continued, Is.False) }

    member private _.Cbi46Run(scenario: string) =
        task {
            let root = Path.Combine(Path.GetTempPath(), $"brontide-cbi46-{Guid.NewGuid():N}")
            Directory.CreateDirectory root |> ignore
            let providers = ResizeArray<StagedProviderProcess>()
            let activations = ResizeArray<ProviderServingActivation>()
            try
                use authority = ECDsa.Create ECCurve.NamedCurves.nistP256
                use firstPublisher = ECDsa.Create ECCurve.NamedCurves.nistP256
                use secondPublisher = ECDsa.Create ECCurve.NamedCurves.nistP256
                let digestOf (key: ECDsa) =
                    key.ExportSubjectPublicKeyInfo() |> SHA256.HashData |> Convert.ToHexString
                let authorityIdentity = digestOf authority |> ProviderPublisherTrustPolicyAuthorityId.create
                let executable =
                    match Environment.GetEnvironmentVariable "BRONTIDE_MINIMAL_PROVIDER" |> Option.ofObj with
                    | Some path when File.Exists path -> Path.GetFullPath path
                    | _ ->
                        Assert.Ignore "BRONTIDE_MINIMAL_PROVIDER does not name a built provider endpoint."
                        failwith "The cross-process test was ignored."
                let named (value: string | null) =
                    match value with null -> failwith "A provider path component was missing." | present -> present
                let providerRoot = Path.GetDirectoryName executable |> named
                let bytes =
                    Directory.EnumerateFiles providerRoot
                    |> Seq.sort
                    |> Seq.map (fun path -> Path.GetFileName path |> named, File.ReadAllBytes path)
                    |> Map.ofSeq
                let files =
                    bytes |> Map.toList |> List.map (fun (path, content) ->
                        { RelativePath = path
                          Sha256 = SHA256.HashData content |> Convert.ToHexString
                          Length = int64 content.LongLength }: ProviderArtifactAcquisitionFile)
                let expectedSource = ProviderArtifactSourceId.create "fixture://brontide/provider-output"
                let executableName = Path.GetFileName executable |> named
                let artifacts = files |> List.map (fun file ->
                    { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
                let request: ProviderArtifactAcquisitionRequest =
                    { ExpectedSource = expectedSource
                      Identity = ProviderArtifactSetIdentity.compute artifacts executableName [ "--portable" ]
                      Files = files; ExecutablePath = executableName; Arguments = [ "--portable" ]
                      MaxTotalBytes = files |> List.sumBy _.Length }
                let evidenceFor (publisher: ECDsa) =
                    { PublisherKeyId = digestOf publisher |> ProviderPublisherKeyId.create
                      Algorithm = "ECDSA-P256-SHA256"
                      PublicKeySpkiBase64 = publisher.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        publisher.SignData(
                            ProviderArtifactPublisherManifest.encode request,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }: ProviderPublisherEvidence
                let evidence = [ evidenceFor firstPublisher; evidenceFor secondPublisher ]
                let policyOf entries: ProviderPublisherTrustPolicy =
                    { Identity = ProviderPublisherTrustPolicyIdentity.compute entries; Entries = entries }
                let initial =
                    evidence |> List.map (fun item ->
                        { PublisherKeyId = item.PublisherKeyId; Disposition = Admitted }) |> policyOf
                let signUpdate sequence previous (policy: ProviderPublisherTrustPolicy) =
                    { Sequence = sequence; PreviousPolicyIdentity = previous; Policy = policy
                      Algorithm = "ECDSA-P256-SHA256"
                      AuthorityPublicKeySpkiBase64 = authority.ExportSubjectPublicKeyInfo() |> Convert.ToBase64String
                      SignatureBase64 =
                        authority.SignData(
                            ProviderPublisherTrustPolicyUpdateManifest.encode sequence previous policy.Identity,
                            HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                        |> Convert.ToBase64String }: ProviderPublisherTrustPolicyUpdate
                let openedCode, openedRegistry, _ =
                    DurableProviderPublisherTrustPolicyRegistry.Open(
                        Path.Combine(root, "policy.checkpoint"), authorityIdentity, None)
                Assert.That(openedCode, Is.EqualTo "policy-checkpoint-empty")
                let registry = openedRegistry.Value
                Assert.That(registry.Apply(signUpdate 1L None initial).IsApplied, Is.True)
                let source =
                    { new IProviderArtifactSource with
                        member _.Identity = expectedSource
                        member _.OpenRead relativePath =
                            bytes |> Map.tryFind relativePath
                            |> Option.map (fun content -> new MemoryStream(content, false) :> Stream) }
                let storeRoot = Path.GetFullPath(Path.Combine(root, "store"))
                let store = ContentAddressedProviderStore storeRoot
                let chains =
                    evidence |> List.mapi (fun index item ->
                        ProviderDistributionChain.run registry store (Path.Combine(root, $"transactions-{index}"))
                            { Acquisition = request; Evidence = Some item; AllowedArguments = [ "--portable" ] }
                            source)
                chains |> List.iter (fun chain ->
                    let providerValue = chain.Provider.Value
                    providers.Add providerValue)

                let pair = pairRequestFor [] []
                let resolution =
                    { pair with
                        Definitions = pair.Definitions |> List.map (fun definition ->
                            if definition.Definition = consumer then
                                { definition with
                                    Requirements = definition.Requirements |> List.map (fun requirement ->
                                        { requirement with Contract = contractId }) }
                            elif definition.Definition = secondaryProvider then
                                { definition with Provides = [ { Contract = contractId; Version = version } ] }
                            else definition)
                        Candidates = pair.Candidates |> List.map (fun candidate ->
                            if candidate.Definition = secondaryProvider then
                                { candidate with Provides = [ { Contract = contractId; Version = version } ] }
                            else candidate) }
                    |> FakeGenerationResolver.resolve
                let providerSets =
                    match resolution with
                    | ResolutionOutcome.Resolved(_, generation) -> generation.ProviderSets
                    | outcome -> failwithf "Expected a resolved generation, got %A." outcome
                let selections =
                    [ requirementId; secondaryRequirementId ] |> List.map (fun requirement ->
                        let position = providerSets |> List.find (fun item -> item.Requirement = requirement)
                                       |> fun item -> List.exactlyOne item.Members
                        { selection position with Requirement = requirement })
                for chain, selected in List.zip chains selections do
                    let! activation =
                        ProviderServingTrustRevalidation.activate chain resolution selected
                            (runtimeRequest (plan [ selected.Occurrence ]))
                    Assert.That(activation.IsServing, Is.True)
                    activations.Add activation

                if scenario = "first-withdrawn-second-current" || scenario = "all-withdrawn" then
                    let successor =
                        evidence |> List.mapi (fun index item ->
                            { PublisherKeyId = item.PublisherKeyId
                              Disposition = if index = 0 || scenario = "all-withdrawn" then Revoked else Admitted })
                        |> policyOf
                    Assert.That(registry.Apply(signUpdate 2L (Some initial.Identity) successor).IsApplied, Is.True)

                let input =
                    match scenario with
                    | "reverse-all-current" -> [ activations[1]; activations[0] ]
                    | "duplicate-occurrence" -> [ activations[0]; activations[0] ]
                    | _ -> List.ofSeq activations
                if scenario = "unavailable-member" then
                    do! activations[1].Retire "make unavailable before sweep"
                let! result = ProviderServingTrustSweep.run registry store input "publisher trust lapsed"
                return
                    { Code = result.Code; RefusedBy = result.RefusedBy
                      Order = result.Members |> List.map (fun item -> OccurrenceId.value item.Occurrence)
                      MemberCodes = result.Members |> List.map (fun item -> item.Result.Code)
                      Continued = result.ContinuedCount; Withdrawn = result.WithdrawnCount
                      FirstServing = activations[0].IsServing; SecondServing = activations[1].IsServing
                      StagedSetRemains =
                        Directory.Exists storeRoot
                        && (Directory.EnumerateDirectories storeRoot |> Seq.isEmpty |> not) }
            finally
                for activation in activations do
                    if activation.IsServing then
                        activation.Retire("CBI46 test completed").GetAwaiter().GetResult()
                for providerValue in providers do
                    if not providerValue.HasExited then providerValue.Dispose()
                try Directory.Delete(root, true) with _ -> ()
        }

    [<Test>]
    member _.``CBI46 C1 the serving set is bounded and valid before effects``() = task {
        let registry = Unchecked.defaultof<DurableProviderPublisherTrustPolicyRegistry>
        let store = Unchecked.defaultof<ContentAddressedProviderStore>
        let! result = ProviderServingTrustSweep.run registry store [] "publisher trust lapsed"
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "serving-trust-sweep-invalid")
            Assert.That(result.RefusedBy, Is.EqualTo "preflight")
            Assert.That(result.Members, Is.Empty)) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C2 typed occurrence identity determines order``() = task {
        let! actual = this.Cbi46Run "reverse-all-current"
        Assert.That(actual.Order, Is.EqualTo(box
            [ "occ.def.test.cooling-provider.1"; "occ.def.test.cooling-provider.2" ])) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C3 every admitted member receives one current decision``() = task {
        let! actual = this.Cbi46Run "reverse-all-current"
        multiple (fun () ->
            Assert.That(actual.MemberCodes, Is.EqualTo(box [ "publisher-trust-current"; "publisher-trust-current" ]))
            Assert.That(actual.Continued, Is.EqualTo 2)) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C4 trust withdrawal reaches every affected member``() = task {
        let! actual = this.Cbi46Run "all-withdrawn"
        multiple (fun () ->
            Assert.That(actual.Withdrawn, Is.EqualTo 2)
            Assert.That(actual.FirstServing, Is.False)
            Assert.That(actual.SecondServing, Is.False)
            Assert.That(actual.StagedSetRemains, Is.False)) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C5 one members outcome does not hide its sibling``() = task {
        let! actual = this.Cbi46Run "first-withdrawn-second-current"
        multiple (fun () ->
            Assert.That(actual.Code, Is.EqualTo "serving-trust-sweep-withdrawn")
            Assert.That(actual.Continued, Is.EqualTo 1)
            Assert.That(actual.Withdrawn, Is.EqualTo 1)
            Assert.That(actual.StagedSetRemains, Is.True,
                "a staged set shared with a continuing sibling must remain available")) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C6 preflight refusal has zero effect``() = task {
        for scenario in [ "duplicate-occurrence"; "unavailable-member" ] do
            let! actual = this.Cbi46Run scenario
            multiple (fun () ->
                Assert.That(actual.Code, Is.EqualTo("serving-trust-sweep-invalid"), scenario)
                Assert.That(actual.Order, Is.Empty, scenario)
                Assert.That(actual.FirstServing, Is.True, scenario)
                Assert.That(actual.StagedSetRemains, Is.True, scenario)) }

    [<Test; Category("CrossProcess")>]
    member this.``CBI46 C7 minimal executes the shared sweep vectors``() = task {
        use document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi46-serving-trust-sweep-vectors.json")))
        Assert.That(document.RootElement.GetProperty("maximumMembers").GetInt32(),
            Is.EqualTo ProviderServingTrustSweep.MaximumMembers)
        let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let name = vector.GetProperty("name") |> textValue
            let! actual = this.Cbi46Run name
            let expectedOrder =
                vector.GetProperty("expectedOrder").EnumerateArray() |> Seq.map textValue |> Seq.toList
            multiple (fun () ->
                Assert.That(actual.Code, Is.EqualTo(vector.GetProperty("expectedCode") |> textValue), name)
                Assert.That(actual.Order, Is.EqualTo(box expectedOrder), name)
                Assert.That(actual.Continued, Is.EqualTo(vector.GetProperty("continued").GetInt32()), name)
                Assert.That(actual.Withdrawn, Is.EqualTo(vector.GetProperty("withdrawn").GetInt32()), name)) }

    [<Test>]
    member _.``CBI47 C1 cadence is bounded and explicit``() =
        let schedule = ProviderServingTrustCadenceSchedule.create 2 (TimeSpan.FromSeconds 5.0)
        multiple (fun () ->
            Assert.That(schedule.MaximumCycles, Is.EqualTo 2)
            Assert.That(schedule.Interval, Is.EqualTo(TimeSpan.FromSeconds 5.0))
            Assert.Throws<ArgumentException>(Action(fun () ->
                ProviderServingTrustCadenceSchedule.create 0 (TimeSpan.FromSeconds 5.0) |> ignore))
            |> ignore)

    member private _.Cbi47Poll code : ProviderPublisherTrustPolicyPollResult =
        { Code = code
          LastAttemptCode = None
          Attempts = 0
          Delays = []
          AppliedSequences = []
          RetainedSequences = []
          Current = None
          Floor = Unchecked.defaultof<ProviderPublisherTrustPolicyRecoveryFloor> }

    member private this.Cbi47CycleResult code : ProviderServingTrustCycleResult =
        { Code = code
          Poll = this.Cbi47Poll "policy-poll-current"
          Sweep = None
          ServingCount = 0 }

    member private this.Cbi47Run(codes: string list, cancellation: string, ?maximumCycles: int) = task {
        use source = new CancellationTokenSource()
        if cancellation = "before-first" then source.Cancel()
        let pending = Collections.Generic.Queue<string>(codes)
        let cycle: ProviderServingTrustCycle =
            fun _ _ -> Task.FromResult(this.Cbi47CycleResult(pending.Dequeue()))
        let delay: ProviderServingTrustCadenceDelay =
            fun now duration cancellationToken -> task {
                if cancellation = "during-gap" then
                    source.Cancel()
                    cancellationToken.ThrowIfCancellationRequested()
                return now + duration
            }
        let schedule =
            ProviderServingTrustCadenceSchedule.create
                (defaultArg maximumCycles 2) (TimeSpan.FromSeconds 5.0)
        return! ProviderServingTrustCadence.run schedule cycle delay
            (DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero)) source.Token
    }

    [<Test>]
    member this.``CBI47 C2 the first cycle is immediate and later cycles use injected time``() = task {
        let! result = this.Cbi47Run(
            [ "provider-trust-cycle-current"; "provider-trust-cycle-current" ], "none")
        multiple (fun () ->
            Assert.That(result.Cycles |> List.map _.Instant, Is.EqualTo(box [
                DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero)
                DateTimeOffset(2026, 8, 5, 8, 0, 5, TimeSpan.Zero) ]))
            Assert.That(result.Gaps, Is.EqualTo(box [ TimeSpan.FromSeconds 5.0 ]))) }

    [<Test>]
    member this.``CBI47 C3 current policy precedes any serving sweep``() = task {
        let mutable servingCalls = 0
        let policy: ProviderPublisherTrustPolicyCycle =
            fun _ _ -> Task.FromResult(this.Cbi47Poll "policy-poll-refused")
        let serving: ProviderServingTrustSweepCycle =
            fun _ ->
                servingCalls <- servingCalls + 1
                Task.FromResult None
        let! result =
            (ProviderServingTrustCycle.create policy serving)
                DateTimeOffset.UnixEpoch CancellationToken.None
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "provider-trust-cycle-stopped")
            Assert.That(servingCalls, Is.Zero)) }

    [<Test>]
    member this.``CBI47 C4 the current serving set is swept once``() = task {
        let mutable servingCalls = 0
        let policy: ProviderPublisherTrustPolicyCycle =
            fun _ _ -> Task.FromResult(this.Cbi47Poll "policy-poll-current")
        let serving: ProviderServingTrustSweepCycle =
            fun _ ->
                servingCalls <- servingCalls + 1
                Task.FromResult None
        let! result =
            (ProviderServingTrustCycle.create policy serving)
                DateTimeOffset.UnixEpoch CancellationToken.None
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "provider-trust-cycle-current")
            Assert.That(result.ServingCount, Is.Zero)
            Assert.That(servingCalls, Is.EqualTo 1)) }

    [<Test>]
    member this.``CBI47 C5 successful withdrawal does not stop cadence``() = task {
        let! result = this.Cbi47Run(
            [ "provider-trust-cycle-withdrawn"; "provider-trust-cycle-current" ], "none")
        Assert.That(result.Code, Is.EqualTo "provider-trust-cadence-complete") }

    [<Test>]
    member this.``CBI47 C6 an invalid or incomplete sweep stops before another gap``() = task {
        for sweepCode in
            [ "serving-trust-sweep-invalid"
              "serving-trust-sweep-incomplete"
              "serving-trust-sweep-cleanup-incomplete" ] do
            let policy: ProviderPublisherTrustPolicyCycle =
                fun _ _ -> Task.FromResult(this.Cbi47Poll "policy-poll-current")
            let serving: ProviderServingTrustSweepCycle =
                fun _ -> Task.FromResult(Some {
                    Code = sweepCode; RefusedBy = "test"; Members = []
                    ContinuedCount = 0; WithdrawnCount = 0 })
            let! cycle =
                (ProviderServingTrustCycle.create policy serving)
                    DateTimeOffset.UnixEpoch CancellationToken.None
            Assert.That(cycle.Code, Is.EqualTo("provider-trust-cycle-stopped"), sweepCode)

        let! result = this.Cbi47Run(
            [ "provider-trust-cycle-current"; "provider-trust-cycle-stopped" ],
            "none", maximumCycles = 3)
        multiple (fun () ->
            Assert.That(result.Code, Is.EqualTo "provider-trust-cadence-stopped")
            Assert.That(result.Cycles, Has.Length.EqualTo 2)
            Assert.That(result.Gaps, Has.Length.EqualTo 1)) }

    [<Test>]
    member this.``CBI47 C7 cancellation has an exact boundary``() = task {
        let! before = this.Cbi47Run([ "provider-trust-cycle-current" ], "before-first")
        let! during = this.Cbi47Run(
            [ "provider-trust-cycle-current"; "provider-trust-cycle-current" ], "during-gap")
        multiple (fun () ->
            Assert.That(before.Code, Is.EqualTo "provider-trust-cadence-canceled")
            Assert.That(before.Cycles, Is.Empty)
            Assert.That(during.Code, Is.EqualTo "provider-trust-cadence-canceled")
            Assert.That(during.Cycles, Has.Length.EqualTo 1)
            Assert.That(during.Gaps, Is.Empty)) }

    [<Test>]
    member this.``CBI47 C8 minimal executes the shared cadence vectors``() = task {
        use document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi47-provider-trust-cadence-vectors.json")))
        let schedule = document.RootElement.GetProperty "schedule"
        let maximumCycles = schedule.GetProperty("maximumCycles").GetInt32()
        let interval = TimeSpan.FromSeconds(float (schedule.GetProperty("intervalSeconds").GetInt32()))
        Assert.That(
            (ProviderServingTrustCadenceSchedule.create maximumCycles interval).MaximumCycles,
            Is.EqualTo maximumCycles)
        let textValue (value: JsonElement) = value.GetString() |> Option.ofObj |> Option.defaultValue ""
        for vector in document.RootElement.GetProperty("vectors").EnumerateArray() do
            let name = vector.GetProperty("name") |> textValue
            let codes =
                vector.GetProperty("cycleCodes").EnumerateArray()
                |> Seq.map textValue |> Seq.toList
            let! result = this.Cbi47Run(codes, vector.GetProperty("cancel") |> textValue)
            let expectedGaps =
                vector.GetProperty("expectedGapsSeconds").EnumerateArray()
                |> Seq.map _.GetInt32() |> Seq.toList
            multiple (fun () ->
                Assert.That(result.Code,
                    Is.EqualTo(vector.GetProperty("expectedCode") |> textValue), name)
                Assert.That(result.Cycles,
                    Has.Length.EqualTo(vector.GetProperty("expectedCycles").GetInt32()), name)
                Assert.That(result.Cycles |> List.map _.Result.Code,
                    Is.EqualTo(box (
                        vector.GetProperty("expectedCycleCodes").EnumerateArray()
                        |> Seq.map textValue |> Seq.toList)), name)
                Assert.That(result.Cycles |> List.map _.Instant,
                    Is.EqualTo(box (
                        vector.GetProperty("expectedCycleInstants").EnumerateArray()
                        |> Seq.map _.GetDateTimeOffset() |> Seq.toList)), name)
                Assert.That(result.Gaps |> List.map (fun gap -> int gap.TotalSeconds),
                    Is.EqualTo(box expectedGaps), name)) }
