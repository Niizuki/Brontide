namespace Brontide.Minimal.ComponentManagement.Tests

open System
open System.IO
open System.Text.Json
open NUnit.Framework
open Brontide.Minimal.Experimental.ComponentManagement

[<TestFixture>]
type ResolutionTests() =
    let telemetry = ContractId.create "brontide.fake.telemetry-sink"
    let version = VersionLiteral.create "2.0"
    let systemScope = BindingScopeId.create "scope.system"
    let app = DefinitionId.create "def.test.app"
    let northwind = DefinitionId.create "def.northwind.telemetry"
    let generic = DefinitionId.create "def.contoso.generic-telemetry"
    let multiple action = Assert.Multiple(Action action)

    let requirement
        id
        cardinality
        allowSharing
        exposure
        region
        port
        runtime
        authority
        topology
        =
        { Requirement = RequirementId.create id
          Contract = telemetry
          Version = version
          Scope = systemScope
          Cardinality = cardinality
          AllowSharing = allowSharing
          Exposure = exposure
          Mediation = None
          Constraints = []
          ContainingRegion = region
          ContainingPort = port
          RuntimeAttachment = runtime
          RequiredImports = []
          RequiredExports = []
          RequiredFailurePolicy = None
          RequiredRollbackBoundary = None
          RequestedAuthority = authority
          TopologyRequirements = topology }

    let ordinaryRequirement id cardinality =
        requirement id cardinality false Distinct None None false [] []

    let definition identity requirements composition activation provides publisher =
        { Definition = identity
          Publisher = publisher
          Provides = provides
          Requirements = requirements
          CompositionParameters = composition
          ActivationParameters = activation
          RequestedAuthority = [] }

    let simpleDefinition identity requirements =
        definition identity requirements [] [] [] (PublisherId.create "pub.contoso")

    let candidate identity publisher genericValue =
        { Definition = identity
          Source = SourceId.create (sprintf "src.%s" (DefinitionId.value identity))
          Publisher = publisher
          Package = PackageId.create (sprintf "pkg.%s" (DefinitionId.value identity))
          Provides = [ { Contract = telemetry; Version = version } ]
          Generic = genericValue
          Sharing =
            { IsolationCompatible = true
              LifecycleCompatible = true
              AuthorityCompatible = true }
          Policy =
            [ { Domain = Trust; Accepted = true; Reason = "accepted by fake trust policy" }
              { Domain = Platform; Accepted = true; Reason = "fake platform matches" } ]
          Evidence = [ EvidenceId.create (sprintf "ev.%s" (DefinitionId.value identity)) ]
          Authority = [ "authority.read" ]
          FailureDomain = sprintf "failure.%s" (DefinitionId.value identity)
          AttachmentNode = Some(TopologyNodeId.create (sprintf "node.%s" (DefinitionId.value identity))) }

    let emptyRequest definitions =
        { Request = ResolutionRequestId.create "resolution.test"
          Generation = GenerationId.create "gen.proposed"
          ActiveGeneration = None
          RestartScope = RestartScopeId.create "restart.test"
          Roots = [ app ]
          Definitions = definitions
          Candidates = []
          ExistingOccurrences = []
          OccupiedBindings = []
          Preferences = []
          AuthorisedReplacements = []
          CompositionParameters = []
          ActivationParameters = []
          PreselectedProviders = []
          Ports = []
          TopologyClaims = [] }

    let baseRequest requirementValue =
        let appDefinition = simpleDefinition app [ requirementValue ]
        let northwindDefinition =
            definition
                northwind
                []
                []
                []
                [ { Contract = telemetry; Version = version } ]
                (PublisherId.create "pub.northwind")
        let genericDefinition =
            definition
                generic
                []
                []
                []
                [ { Contract = telemetry; Version = version } ]
                (PublisherId.create "pub.contoso")
        let excludedId = DefinitionId.create "def.test.excluded"
        let excludedDefinition =
            definition
                excludedId
                []
                []
                []
                [ { Contract = telemetry; Version = version } ]
                (PublisherId.create "pub.test")
        { emptyRequest [ appDefinition; northwindDefinition; genericDefinition; excludedDefinition ] with
            ActiveGeneration = Some(GenerationId.create "gen.active")
            Candidates =
                [ candidate northwind (PublisherId.create "pub.northwind") false
                  candidate generic (PublisherId.create "pub.contoso") true
                  { candidate excludedId (PublisherId.create "pub.test") false with
                      Policy = [ { Domain = Trust; Accepted = false; Reason = "fake trust policy excludes candidate" } ] } ]
            ExistingOccurrences =
                [ { Occurrence = OccurrenceId.create "occ.telemetry-retained"
                    Definition = northwind
                    Actors = [ ActorId.create "actor.telemetry-retained" ] } ]
            OccupiedBindings =
                [ { Binding = BindingId.create "bind.telemetry"
                    Scope = systemScope
                    Contract = telemetry
                    OccupantDefinition = northwind
                    OccupantOccurrence = OccurrenceId.create "occ.telemetry-retained" } ]
            Preferences =
                [ { Preference = PreferenceId.create "pref.generic"
                    DeclaredBy = app
                    Contract = telemetry
                    PreferredDefinition = generic } ] }

    let replaceRequirement replacement (request: ResolutionRequest) =
        { request with
            Definitions =
                request.Definitions
                |> List.map (fun item ->
                    if item.Definition = app then { item with Requirements = [ replacement ] }
                    else item) }

    let rec permutations values =
        match values with
        | [] -> [ [] ]
        | _ ->
            values
            |> List.mapi (fun index head ->
                let tail = values |> List.indexed |> List.choose (fun (candidateIndex, value) -> if candidateIndex = index then None else Some value)
                permutations tail |> List.map (fun suffix -> head :: suffix))
            |> List.concat

    [<Test>]
    member _.``Neutral vector inventory is complete and data only``() =
        let path =
            Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "component-management",
                "fixtures",
                "cm2-resolution-vectors.json")
        use document = JsonDocument.Parse(File.ReadAllText path)
        let root = document.RootElement
        let ids =
            root.GetProperty("vectors").EnumerateArray()
            |> Seq.map (fun vector -> vector.GetProperty("id").GetString())
            |> Seq.toList
        let expected = [ 1..15 ] |> List.map (sprintf "cm2-%02d")
        multiple (fun () ->
            Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1))
            Assert.That(root.GetProperty("fixture").GetString(), Is.EqualTo("cm2-resolution-vectors"))
            Assert.That((ids = expected), Is.True)
            Assert.That(root.GetRawText().Contains("algorithm", StringComparison.Ordinal), Is.False))

    [<Test>]
    member _.``Compatible occupied binding is retained and preference remains visible``() =
        let request = baseRequest (ordinaryRequirement "req.telemetry" (Cardinality.parse "1..1"))
        match FakeGenerationResolver.resolve request with
        | Resolved(proposed, generation) ->
            let set = List.exactlyOne generation.ProviderSets
            let memberValue = List.exactlyOne set.Members
            multiple (fun () ->
                Assert.That(memberValue.Retained, Is.True)
                Assert.That(memberValue.Occurrence, Is.EqualTo(OccurrenceId.create "occ.telemetry-retained"))
                Assert.That((List.exactlyOne proposed.Preferences).Used, Is.False)
                Assert.That((List.exactlyOne proposed.Preferences).Reason, Is.EqualTo("compatible-occupant-retained"))
                Assert.That(generation.Effects, Is.EqualTo(Cm2EffectObservation.none))
                Assert.That(proposed.RetainedActiveGeneration, Is.EqualTo(Some(GenerationId.create "gen.active"))))
        | outcome -> Assert.Fail(sprintf "Expected resolution, got %A" outcome)

    [<Test>]
    member _.``Authorised replacement applies preference and records exclusions``() =
        let request =
            { baseRequest (ordinaryRequirement "req.telemetry" (Cardinality.parse "1..1")) with
                AuthorisedReplacements = [ BindingId.create "bind.telemetry" ] }
        match FakeGenerationResolver.resolve request with
        | Resolved(proposed, generation) ->
            multiple (fun () ->
                let selected = (generation.ProviderSets |> List.exactlyOne).Members |> List.exactlyOne
                Assert.That(selected.Definition = generic, Is.True)
                Assert.That((List.exactlyOne proposed.Preferences).Used, Is.True)
                Assert.That((List.exactlyOne proposed.Exclusions).Domain, Is.EqualTo(Trust)))
        | outcome -> Assert.Fail(sprintf "Expected resolution, got %A" outcome)

    [<Test>]
    member _.``Optional Provider Set capacity stays empty unless preselected``() =
        let requirementValue = ordinaryRequirement "req.providers" (Cardinality.parse "1..3")
        let request =
            { baseRequest requirementValue with
                OccupiedBindings = []
                Preferences = [] }
        let ordinary = FakeGenerationResolver.resolve request
        let preselected =
            FakeGenerationResolver.resolve
                { request with
                    PreselectedProviders = [ { Requirement = requirementValue.Requirement; Definition = northwind } ] }
        match ordinary, preselected with
        | Resolved(_, first), Resolved(_, second) ->
            multiple (fun () ->
                Assert.That((List.exactlyOne first.ProviderSets).Members, Has.Length.EqualTo(1))
                Assert.That((List.exactlyOne first.ProviderSets).OptionalPositionsUnfilled, Is.EqualTo(2))
                Assert.That((List.exactlyOne second.ProviderSets).Members, Has.Length.EqualTo(2))
                Assert.That((List.exactlyOne second.ProviderSets).OptionalPositionsUnfilled, Is.EqualTo(1)))
        | outcomes -> Assert.Fail(sprintf "Expected two resolutions, got %A" outcomes)

    [<Test>]
    member _.``Recursive composition closes before Activation Parameters``() =
        let child = DefinitionId.create "def.test.child"
        let leaf = DefinitionId.create "def.test.leaf"
        let compositionParameter = ParameterId.create "param.child"
        let activationParameter = ParameterId.create "param.path"
        let root =
            definition
                app
                []
                [ { Parameter = compositionParameter; AllowedDefinitions = [ child ]; Required = true } ]
                [ { Parameter = activationParameter; Required = true; DefaultValue = None } ]
                []
                (PublisherId.create "pub.contoso")
        let childDefinition = simpleDefinition child [ ordinaryRequirement "req.child-leaf" (Cardinality.parse "1..1") ]
        let leafDefinition =
            definition
                leaf
                []
                []
                []
                [ { Contract = telemetry; Version = version } ]
                (PublisherId.create "pub.test")
        let request =
            { emptyRequest [ root; childDefinition; leafDefinition ] with
                CompositionParameters =
                    [ { Owner = app
                        Parameter = compositionParameter
                        SelectedDefinition = child } ]
                ActivationParameters =
                    [ { Parameter = activationParameter; Value = "fake-resource" }
                      { Parameter = ParameterId.create "param.unused"; Value = "ignored" } ]
                Candidates = [ candidate leaf (PublisherId.create "pub.test") false ] }
        match FakeGenerationResolver.resolve request with
        | Resolved(proposed, generation) ->
            multiple (fun () ->
                Assert.That(generation.Definitions = ([ app; child; leaf ] |> List.sortBy DefinitionId.value), Is.True)
                Assert.That((List.exactlyOne generation.Parameters).Value, Is.EqualTo("fake-resource"))
                Assert.That((List.exactlyOne proposed.UnusedActivationParameters).Parameter, Is.EqualTo(ParameterId.create "param.unused")))
        | outcome -> Assert.Fail(sprintf "Expected resolution, got %A" outcome)

        match FakeGenerationResolver.resolve { request with Candidates = [] } with
        | ResolutionOutcome.Refused failure ->
            multiple (fun () ->
                Assert.That(failure.Kind, Is.EqualTo(MissingDependency))
                Assert.That(Option.isNone failure.Parameter, Is.True))
        | outcome -> Assert.Fail(sprintf "Expected structural refusal, got %A" outcome)

    [<Test>]
    member _.``Sharing requires all declarations and equal scope``() =
        let first = requirement "req.first" (Cardinality.parse "1..1") true Distinct None None false [] []
        let second = requirement "req.second" (Cardinality.parse "1..1") true Distinct None None false [] []
        let root = simpleDefinition app [ first; second ]
        let provider =
            definition
                generic
                []
                []
                []
                [ { Contract = telemetry; Version = version } ]
                (PublisherId.create "pub.contoso")
        let request =
            { emptyRequest [ root; provider ] with
                Candidates = [ candidate generic (PublisherId.create "pub.contoso") true ] }
        let separateCandidate =
            { candidate generic (PublisherId.create "pub.contoso") true with
                Sharing =
                    { IsolationCompatible = true
                      LifecycleCompatible = false
                      AuthorityCompatible = true } }
        match
            FakeGenerationResolver.resolve request,
            FakeGenerationResolver.resolve { request with Candidates = [ separateCandidate ] }
        with
        | Resolved(_, shared), Resolved(_, separate) ->
            let distinctOccurrences generation =
                generation.ProviderSets
                |> List.collect (fun set -> set.Members)
                |> List.map (fun item -> item.Occurrence)
                |> List.distinct
                |> List.length
            multiple (fun () ->
                Assert.That(distinctOccurrences shared, Is.EqualTo(1))
                Assert.That(distinctOccurrences separate, Is.EqualTo(2))
                let distinctNodes =
                    separate.ProviderSets
                    |> List.collect (fun set -> set.Members)
                    |> List.choose (fun memberValue -> memberValue.AttachmentNode)
                    |> List.distinct
                    |> List.length
                Assert.That(distinctNodes, Is.EqualTo(2)))
        | outcomes -> Assert.Fail(sprintf "Expected two resolutions, got %A" outcomes)

    [<Test>]
    member _.``Mirrored sources remain alternatives but fill one definition position``() =
        let requirementValue = ordinaryRequirement "req.mirror" (Cardinality.parse "1..1")
        let root = simpleDefinition app [ requirementValue ]
        let provider =
            definition generic [] [] [] [ { Contract = telemetry; Version = version } ] (PublisherId.create "pub.contoso")
        let first = candidate generic (PublisherId.create "pub.contoso") true
        let rejected =
            { first with
                Policy =
                    [ { Domain = LocalPolicy
                        Accepted = false
                        Reason = "primary source unavailable" } ] }
        let mirror = { first with Source = SourceId.create "src.mirror" }
        let request =
            { emptyRequest [ root; provider ] with
                Candidates = [ rejected; mirror ] }
        match FakeGenerationResolver.resolve request with
        | Resolved(proposed, generation) ->
            let set = List.exactlyOne generation.ProviderSets
            multiple (fun () ->
                Assert.That(set.Members, Has.Length.EqualTo(1))
                Assert.That((List.exactlyOne set.Members).Source, Is.EqualTo(Some(SourceId.create "src.mirror")))
                Assert.That(set.Alternatives, Has.Length.EqualTo(2))
                Assert.That((List.exactlyOne proposed.Exclusions).Source, Is.Not.EqualTo(SourceId.create "src.mirror"))
                let sources = set.Alternatives |> List.map (fun alternative -> alternative.Source)
                Assert.That((sources = (sources |> List.sortBy SourceId.value)), Is.True))
        | outcome -> Assert.Fail(sprintf "Expected mirrored resolution, got %A" outcome)

    [<Test>]
    member _.``Occupied binding without matching occurrence fails closed``() =
        let request =
            { baseRequest (ordinaryRequirement "req.telemetry" (Cardinality.parse "1..1")) with
                ExistingOccurrences = [] }
        match FakeGenerationResolver.resolve request with
        | ResolutionOutcome.Refused failure ->
            multiple (fun () ->
                Assert.That(failure.Kind, Is.EqualTo(ContradictoryIdentity))
                Assert.That(failure.Reason, Does.Contain("no matching retained occurrence")))
        | outcome -> Assert.Fail(sprintf "Expected retained occurrence refusal, got %A" outcome)
        let duplicateRequest = baseRequest (ordinaryRequirement "req.telemetry" (Cardinality.parse "1..1"))
        match
            FakeGenerationResolver.resolve
                { duplicateRequest with
                    ExistingOccurrences =
                        duplicateRequest.ExistingOccurrences @ duplicateRequest.ExistingOccurrences }
        with
        | ResolutionOutcome.Refused failure ->
            Assert.That(failure.Kind, Is.EqualTo(ContradictoryIdentity))
        | outcome -> Assert.Fail(sprintf "Expected duplicate retained occurrence refusal, got %A" outcome)

    [<Test>]
    member _.``Mediated endpoint requires declaration and dedicated policy Component``() =
        let second = DefinitionId.create "def.test.second"
        let requirementValue = requirement "req.logical" (Cardinality.parse "2..2") false Mediated None None false [] []
        let root = simpleDefinition app [ requirementValue ]
        let provider identity publisher =
            definition identity [] [] [] [ { Contract = telemetry; Version = version } ] publisher
        let request =
            { emptyRequest [ root; provider generic (PublisherId.create "pub.contoso"); provider second (PublisherId.create "pub.other") ] with
                Candidates =
                    [ candidate generic (PublisherId.create "pub.contoso") true
                      candidate second (PublisherId.create "pub.other") false ] }
        match FakeGenerationResolver.resolve request with
        | ResolutionOutcome.Refused failure -> Assert.That(failure.Kind, Is.EqualTo(MediationRequired))
        | outcome -> Assert.Fail(sprintf "Expected mediation refusal, got %A" outcome)

        let mediator = DefinitionId.create "def.test.aggregator"
        let hostMediation =
            { Mediation = MediationId.create "med.logical"
              Kind = Aggregation
              Realization = StaticHost
              Component = None
              OwnsMutableMembership = false
              OwnsResidue = false
              OwnsBackpressure = true
              OwnsAuthority = false
              OwnsRecovery = false
              OwnsLifecycle = false }
        match FakeGenerationResolver.resolve (request |> replaceRequirement { requirementValue with Mediation = Some hostMediation }) with
        | ResolutionOutcome.Refused failure -> Assert.That(failure.Kind, Is.EqualTo(MediationRequiresComponent))
        | outcome -> Assert.Fail(sprintf "Expected dedicated Component refusal, got %A" outcome)

        let dedicated =
            { hostMediation with
                Realization = DedicatedComponent
                Component = Some mediator }
        let withMediator =
            { request with
                Definitions = request.Definitions @ [ simpleDefinition mediator [] ] }
            |> replaceRequirement { requirementValue with Mediation = Some dedicated }
        match FakeGenerationResolver.resolve withMediator with
        | Resolved(_, generation) ->
            let set = List.exactlyOne generation.ProviderSets
            multiple (fun () ->
                Assert.That(set.Mediation, Is.EqualTo(Some dedicated))
                Assert.That(set.BindingPlans |> List.forall (fun plan -> not plan.Direct), Is.True)
                Assert.That(set.Members, Has.Length.EqualTo(2)))
        | outcome -> Assert.Fail(sprintf "Expected mediated resolution, got %A" outcome)

    [<Test>]
    member _.``Port envelope refuses or requests explicit wider generation``() =
        let region = RegionId.create "region.parent"
        let port = PortId.create "port.child"
        let requirementValue =
            requirement
                "req.child"
                (Cardinality.parse "1..1")
                false
                Distinct
                (Some region)
                (Some port)
                true
                [ "authority.read" ]
                [ AttachedThrough ]
            |> fun value ->
                { value with
                    RequiredImports = [ "import.clock" ]
                    RequiredExports = [ "export.telemetry" ]
                    RequiredFailurePolicy = Some "contain"
                    RequiredRollbackBoundary = Some "child" }
        let envelope =
            { Region = region
              Port = port
              Lifecycle = RuntimeOpen
              Contracts = [ { Contract = telemetry; Version = version } ]
              Cardinality = Cardinality.parse "0..1"
              Imports = [ "import.clock" ]
              Exports = [ "export.telemetry" ]
              AuthorityCeiling = [ "authority.read" ]
              TopologyRequirements = [ AttachedThrough ]
              FailurePolicy = "contain"
              RollbackBoundary = "child"
              AllowWiderGenerationProposal = false }
        let root = simpleDefinition app [ requirementValue ]
        let provider =
            definition generic [] [] [] [ { Contract = telemetry; Version = version } ] (PublisherId.create "pub.contoso")
        let request =
            { emptyRequest [ root; provider ] with
                Candidates = [ candidate generic (PublisherId.create "pub.contoso") true ]
                Ports = [ envelope ] }
        match FakeGenerationResolver.resolve request with
        | Resolved(_, generation) ->
            multiple (fun () ->
                Assert.That(List.exactlyOne generation.Ports, Is.EqualTo(envelope))
                Assert.That((List.exactlyOne generation.ProviderSets).ContainingRegion, Is.EqualTo(Some region))
                Assert.That((List.exactlyOne generation.ProviderSets).ContainingPort, Is.EqualTo(Some port)))
        | outcome -> Assert.Fail(sprintf "Expected child resolution, got %A" outcome)
        match FakeGenerationResolver.resolve { request with Ports = [ envelope; envelope ] } with
        | ResolutionOutcome.Refused failure ->
            Assert.That(failure.Kind, Is.EqualTo(ContradictoryIdentity))
        | outcome -> Assert.Fail(sprintf "Expected duplicate Port refusal, got %A" outcome)

        let envelopeExcesses =
            [ { requirementValue with RequiredImports = [ "import.network" ] }
              { requirementValue with RequiredExports = [ "export.control" ] }
              { requirementValue with RequiredFailurePolicy = Some "propagate" }
              { requirementValue with RequiredRollbackBoundary = Some "parent" } ]
        for excessRequirement in envelopeExcesses do
            match FakeGenerationResolver.resolve (request |> replaceRequirement excessRequirement) with
            | ResolutionOutcome.Refused failure ->
                Assert.That(failure.Kind, Is.EqualTo(PortEnvelopeExceeded))
            | outcome -> Assert.Fail(sprintf "Expected Port envelope refusal, got %A" outcome)

        let excess = request |> replaceRequirement { requirementValue with RequestedAuthority = [ "authority.write" ] }
        match FakeGenerationResolver.resolve excess with
        | ResolutionOutcome.Refused failure -> Assert.That(failure.Kind, Is.EqualTo(PortEnvelopeExceeded))
        | outcome -> Assert.Fail(sprintf "Expected Port refusal, got %A" outcome)
        match FakeGenerationResolver.resolve { excess with Ports = [ { envelope with AllowWiderGenerationProposal = true } ] } with
        | WiderGenerationRequired proposal ->
            multiple (fun () ->
                Assert.That(proposal.Region, Is.EqualTo(region))
                Assert.That(proposal.Port, Is.EqualTo(port)))
        | outcome -> Assert.Fail(sprintf "Expected wider generation proposal, got %A" outcome)

    [<Test>]
    member _.``Topology policy preserves attribution and distinct relations``() =
        let host = TopologyNodeId.create "node.host"
        let attachment = TopologyNodeId.create "node.mouse"
        let claims =
            [ { Claim = ClaimId.create "claim.accept"
                AssertedBy = ObserverId.create "observer.local"
                Relation = AttachedThrough
                From = attachment
                To = host
                Disposition = TopologyPolicyDisposition.Accepted
                RefinedRelation = None
                Reason = "local attachment observation" }
              { Claim = ClaimId.create "claim.refine"
                AssertedBy = ObserverId.create "observer.device"
                Relation = SamePhysicalAssembly
                From = attachment
                To = host
                Disposition = Refined
                RefinedRelation = Some HostedBy
                Reason = "only hosting was locally observed" }
              { Claim = ClaimId.create "claim.reject"
                AssertedBy = ObserverId.create "observer.device"
                Relation = SharesPowerDomain
                From = attachment
                To = host
                Disposition = TopologyPolicyDisposition.Rejected
                RefinedRelation = None
                Reason = "unsupported claim" } ]
        let request = { emptyRequest [ simpleDefinition app [] ] with TopologyClaims = claims }
        match FakeGenerationResolver.resolve request with
        | Resolved(_, generation) ->
            multiple (fun () ->
                let dispositions = generation.Topology |> List.map (fun decision -> decision.Disposition)
                let expected =
                    [ TopologyPolicyDisposition.Accepted
                      TopologyPolicyDisposition.Refined
                      TopologyPolicyDisposition.Rejected ]
                Assert.That((dispositions = expected), Is.True)
                Assert.That(generation.Topology[1].EffectiveRelation, Is.EqualTo(Some HostedBy))
                Assert.That(Option.isNone generation.Topology[2].EffectiveRelation, Is.True))
        | outcome -> Assert.Fail(sprintf "Expected topology resolution, got %A" outcome)

    [<Test>]
    member _.``Resolver is deterministic under definition and candidate permutations``() =
        let second = DefinitionId.create "def.test.second"
        let requirementValue = ordinaryRequirement "req.providers" (Cardinality.parse "2..2")
        let root = simpleDefinition app [ requirementValue ]
        let provider identity publisher =
            definition identity [] [] [] [ { Contract = telemetry; Version = version } ] publisher
        let request =
            { emptyRequest [ root; provider generic (PublisherId.create "pub.contoso"); provider second (PublisherId.create "pub.other") ] with
                Candidates =
                    [ candidate generic (PublisherId.create "pub.contoso") true
                      candidate second (PublisherId.create "pub.other") false ] }
        let baseline = FakeGenerationResolver.resolve request
        for definitions in permutations request.Definitions do
            for candidates in permutations request.Candidates do
                let outcome =
                    FakeGenerationResolver.resolve
                        { request with
                            Definitions = definitions
                            Candidates = candidates }
                Assert.That(outcome, Is.EqualTo(baseline))

    [<Test>]
    member _.``Declared failures are structured and effect free``() =
        for kind in
            [ UnsupportedConstraint
              UnboundedRequiredCardinality
              ActivationParameterUnavailable
              CycleRequiresCm3 ] do
            let request =
                match kind with
                | UnsupportedConstraint ->
                    let invalid =
                        { ordinaryRequirement "req.failure" (Cardinality.parse "1..1") with
                            Constraints = [ { Name = "unknown"; Value = "x" } ] }
                    emptyRequest [ simpleDefinition app [ invalid ] ]
                | UnboundedRequiredCardinality ->
                    emptyRequest [ simpleDefinition app [ ordinaryRequirement "req.failure" (Cardinality.parse "1..*") ] ]
                | ActivationParameterUnavailable ->
                    emptyRequest
                        [ definition
                            app
                            []
                            []
                            [ { Parameter = ParameterId.create "param.missing"; Required = true; DefaultValue = None } ]
                            []
                            (PublisherId.create "pub.contoso") ]
                | CycleRequiresCm3 ->
                    let second = DefinitionId.create "def.test.second"
                    let first = simpleDefinition app [ ordinaryRequirement "req.first-second" (Cardinality.parse "1..1") ]
                    let other = simpleDefinition second [ ordinaryRequirement "req.second-first" (Cardinality.parse "1..1") ]
                    { emptyRequest [ first; other ] with
                        Candidates =
                            [ candidate app (PublisherId.create "pub.contoso") false
                              candidate second (PublisherId.create "pub.other") false ] }
                | _ -> failwithf "Unhandled test kind %A" kind
            match FakeGenerationResolver.resolve request with
            | ResolutionOutcome.Refused failure ->
                multiple (fun () ->
                    Assert.That(failure.Kind, Is.EqualTo(kind))
                    Assert.That((ResolutionOutcome.Refused failure).Effects, Is.EqualTo(Cm2EffectObservation.none)))
            | outcome -> Assert.Fail(sprintf "Expected refusal, got %A" outcome)
