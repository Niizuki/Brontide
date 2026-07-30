namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Collections.Generic

type ProviderExposure =
    | Distinct
    | Mediated

type MediationKind =
    | Selection
    | Distribution
    | Aggregation
    | Arbitration
    | DomainSpecific

type MediationRealization =
    | StaticHost
    | DedicatedComponent

type PortLifecycleMode =
    | Sealed
    | ActivationOpen
    | RuntimeOpen

type CandidatePolicyDomain =
    | Trust
    | Origin
    | Platform
    | Authority
    | Resource
    | LocalPolicy

type TopologyPolicyDisposition =
    | Accepted
    | Refined
    | Rejected

type Cm2EffectObservation =
    { SelectionMutated: bool
      Prepared: bool
      Activated: bool
      ActorEstablished: bool
      CapabilityGranted: bool
      ActiveGenerationMutated: bool }

[<RequireQualifiedAccess>]
module Cm2EffectObservation =
    let none =
        { SelectionMutated = false
          Prepared = false
          Activated = false
          ActorEstablished = false
          CapabilityGranted = false
          ActiveGenerationMutated = false }

type SharingDeclaration =
    { IsolationCompatible: bool
      LifecycleCompatible: bool
      AuthorityCompatible: bool }

type CandidatePolicyObservation =
    { Domain: CandidatePolicyDomain
      Accepted: bool
      Reason: string }

type ResolutionCandidate =
    { Definition: DefinitionId
      Source: SourceId
      Publisher: PublisherId
      Package: PackageId
      Provides: ProvidedContract list
      Generic: bool
      Sharing: SharingDeclaration
      Policy: CandidatePolicyObservation list
      Evidence: EvidenceId list
      Authority: string list
      FailureDomain: string
      AttachmentNode: TopologyNodeId option }

type MediationDeclaration =
    { Mediation: MediationId
      Kind: MediationKind
      Realization: MediationRealization
      Component: DefinitionId option
      OwnsMutableMembership: bool
      OwnsResidue: bool
      OwnsBackpressure: bool
      OwnsAuthority: bool
      OwnsRecovery: bool
      OwnsLifecycle: bool }

type ResolutionRequirement =
    { Requirement: RequirementId
      Contract: ContractId
      Version: VersionLiteral
      Scope: BindingScopeId
      Cardinality: Cardinality
      AllowSharing: bool
      Exposure: ProviderExposure
      Mediation: MediationDeclaration option
      Constraints: DefinitionConstraint list
      ContainingRegion: RegionId option
      ContainingPort: PortId option
      RuntimeAttachment: bool
      RequiredImports: string list
      RequiredExports: string list
      RequiredFailurePolicy: string option
      RequiredRollbackBoundary: string option
      RequestedAuthority: string list
      TopologyRequirements: TopologyRelation list }

type CompositionParameterDeclaration =
    { Parameter: ParameterId
      AllowedDefinitions: DefinitionId list
      Required: bool }

type ActivationParameterDeclaration =
    { Parameter: ParameterId
      Required: bool
      DefaultValue: string option }

type ResolutionDefinition =
    { Definition: DefinitionId
      Publisher: PublisherId
      Provides: ProvidedContract list
      Requirements: ResolutionRequirement list
      CompositionParameters: CompositionParameterDeclaration list
      ActivationParameters: ActivationParameterDeclaration list
      RequestedAuthority: string list }

type CompositionParameterSelection =
    { Owner: DefinitionId
      Parameter: ParameterId
      SelectedDefinition: DefinitionId }

type ActivationParameterValue = { Parameter: ParameterId; Value: string }
type ProviderPreselection = { Requirement: RequirementId; Definition: DefinitionId }

type PortEnvelope =
    { Region: RegionId
      Port: PortId
      Lifecycle: PortLifecycleMode
      Contracts: ProvidedContract list
      Cardinality: Cardinality
      Imports: string list
      Exports: string list
      AuthorityCeiling: string list
      TopologyRequirements: TopologyRelation list
      FailurePolicy: string
      RollbackBoundary: string
      AllowWiderGenerationProposal: bool }

type TopologyPolicyInput =
    { Claim: ClaimId
      AssertedBy: ObserverId
      Relation: TopologyRelation
      From: TopologyNodeId
      To: TopologyNodeId
      Disposition: TopologyPolicyDisposition
      RefinedRelation: TopologyRelation option
      Reason: string }

type ResolutionRequest =
    { Request: ResolutionRequestId
      Generation: GenerationId
      ActiveGeneration: GenerationId option
      RestartScope: RestartScopeId
      Roots: DefinitionId list
      Definitions: ResolutionDefinition list
      Candidates: ResolutionCandidate list
      ExistingOccurrences: ActivatedOccurrenceEntry list
      OccupiedBindings: OccupiedBindingEntry list
      Preferences: PreferenceEntry list
      AuthorisedReplacements: BindingId list
      CompositionParameters: CompositionParameterSelection list
      ActivationParameters: ActivationParameterValue list
      PreselectedProviders: ProviderPreselection list
      Ports: PortEnvelope list
      TopologyClaims: TopologyPolicyInput list }

type ProviderSetMember =
    { Definition: DefinitionId
      Occurrence: OccurrenceId
      Source: SourceId option
      Publisher: PublisherId
      Package: PackageId option
      Retained: bool
      Evidence: EvidenceId list
      Authority: string list
      FailureDomain: string
      AttachmentNode: TopologyNodeId option }

type CandidateAlternative =
    { Definition: DefinitionId
      Source: SourceId
      Publisher: PublisherId
      Package: PackageId
      Rank: int
      Admissible: bool
      ExclusionReasons: string list }

type BindingPlanObservation =
    { Requirement: RequirementId
      Member: OccurrenceId
      Direct: bool
      Mediation: MediationId option }

type ProviderSetObservation =
    { Requirement: RequirementId
      Requester: DefinitionId
      Contract: ContractId
      Version: VersionLiteral
      Scope: BindingScopeId
      Cardinality: Cardinality
      Exposure: ProviderExposure
      Members: ProviderSetMember list
      OptionalPositionsUnfilled: int
      Mediation: MediationDeclaration option
      BindingPlans: BindingPlanObservation list
      ContainingRegion: RegionId option
      ContainingPort: PortId option
      Alternatives: CandidateAlternative list }

type PreferenceObservation =
    { Preference: PreferenceId
      Requester: DefinitionId
      PreferredDefinition: DefinitionId
      Requirement: RequirementId
      Used: bool
      Reason: string }

type CandidateExclusion =
    { Requirement: RequirementId
      Definition: DefinitionId
      Source: SourceId
      Domain: CandidatePolicyDomain
      Reason: string }

type ResolutionConflict =
    { Requirement: RequirementId
      Kind: string
      Reason: string }

type ResolutionDecision =
    { Requirement: RequirementId option
      Definition: DefinitionId option
      Kind: string
      Reason: string }

type EffectiveParameter =
    { Definition: DefinitionId
      Parameter: ParameterId
      Value: string
      Provenance: string }

type DefinitionAuthorityObservation =
    { Definition: DefinitionId
      RequestedAuthority: string list }

type TopologyDecision =
    { Claim: ClaimId
      AssertedBy: ObserverId
      ClaimedRelation: TopologyRelation
      EffectiveRelation: TopologyRelation option
      From: TopologyNodeId
      To: TopologyNodeId
      Disposition: TopologyPolicyDisposition
      Reason: string }

type ProposedStack =
    { Generation: GenerationId
      RetainedActiveGeneration: GenerationId option
      RestartScope: RestartScopeId
      Roots: DefinitionId list
      Definitions: DefinitionId list
      ProviderSets: ProviderSetObservation list
      Preferences: PreferenceObservation list
      Exclusions: CandidateExclusion list
      Conflicts: ResolutionConflict list
      Parameters: EffectiveParameter list
      UnusedActivationParameters: ActivationParameterValue list
      Ports: PortEnvelope list
      RequestedAuthority: DefinitionAuthorityObservation list
      Topology: TopologyDecision list
      Decisions: ResolutionDecision list }

type ResolvedGeneration =
    { Generation: GenerationId
      RestartScope: RestartScopeId
      Definitions: DefinitionId list
      ProviderSets: ProviderSetObservation list
      Parameters: EffectiveParameter list
      Ports: PortEnvelope list
      RequestedAuthority: DefinitionAuthorityObservation list
      Topology: TopologyDecision list
      Effects: Cm2EffectObservation }

type ResolutionFailureKind =
    | MissingDefinition
    | MissingDependency
    | IncompatibleContract
    | UnsupportedConstraint
    | UnboundedRequiredCardinality
    | ContradictoryIdentity
    | CycleRequiresCm3
    | AmbiguousSelection
    | MediationRequired
    | MediationRequiresComponent
    | PortUnavailable
    | PortEnvelopeExceeded
    | ActivationParameterUnavailable

type ResolutionFailure =
    { Kind: ResolutionFailureKind
      Definition: DefinitionId option
      Requirement: RequirementId option
      Region: RegionId option
      Port: PortId option
      Parameter: ParameterId option
      Reason: string }

type WiderGenerationProposal =
    { Region: RegionId
      Port: PortId
      Requirement: RequirementId
      Reason: string }

type ResolutionOutcome =
    | Resolved of ProposedStack * ResolvedGeneration
    | WiderGenerationRequired of WiderGenerationProposal
    | Refused of ResolutionFailure

type ResolutionOutcome with
    member _.Effects = Cm2EffectObservation.none

[<RequireQualifiedAccess>]
module FakeGenerationResolver =
    type private Accumulator =
        { Included: HashSet<DefinitionId>
          Pending: Queue<DefinitionId>
          Edges: HashSet<DefinitionId * DefinitionId>
          ProcessedRequirements: HashSet<RequirementId>
          ProviderSets: ResizeArray<ProviderSetObservation>
          Preferences: ResizeArray<PreferenceObservation>
          Exclusions: ResizeArray<CandidateExclusion>
          Conflicts: ResizeArray<ResolutionConflict>
          Decisions: ResizeArray<ResolutionDecision>
          Counters: Dictionary<DefinitionId, int>
          Shared: Dictionary<DefinitionId * BindingScopeId, OccurrenceId> }

    let private refusal kind definition requirement reason region port parameter =
        Refused
            { Kind = kind
              Definition = definition
              Requirement = requirement
              Region = region
              Port = port
              Parameter = parameter
              Reason = reason }

    let private simpleRefusal kind definition requirement reason =
        refusal kind definition requirement reason None None None

    let private policyBearing mediation =
        mediation.OwnsMutableMembership
        || mediation.OwnsResidue
        || mediation.OwnsBackpressure
        || mediation.OwnsAuthority
        || mediation.OwnsRecovery
        || mediation.OwnsLifecycle

    let private rank
        (request: ResolutionRequest)
        (requester: ResolutionDefinition)
        (requirement: ResolutionRequirement)
        (candidate: ResolutionCandidate)
        =
        if
            request.Preferences
            |> List.exists (fun preference ->
                preference.DeclaredBy = requester.Definition
                && preference.Contract = requirement.Contract
                && preference.PreferredDefinition = candidate.Definition)
        then 0
        elif candidate.Publisher = requester.Publisher then 1
        elif candidate.Generic then 2
        else 3

    let private memberFor
        (accumulator: Accumulator)
        (requirement: ResolutionRequirement)
        (candidate: ResolutionCandidate)
        =
        let canShare =
            requirement.AllowSharing
            && candidate.Sharing.IsolationCompatible
            && candidate.Sharing.LifecycleCompatible
            && candidate.Sharing.AuthorityCompatible
        let key = candidate.Definition, requirement.Scope
        let occurrence =
            match canShare, accumulator.Shared.TryGetValue key with
            | true, (true, existing) -> existing
            | _ ->
                let current =
                    match accumulator.Counters.TryGetValue candidate.Definition with
                    | true, value -> value
                    | false, _ -> 0
                let next = current + 1
                accumulator.Counters[candidate.Definition] <- next
                let created =
                    OccurrenceId.create (sprintf "occ.%s.%d" (DefinitionId.value candidate.Definition) next)
                if canShare then
                    accumulator.Shared[key] <- created
                created
        { Definition = candidate.Definition
          Occurrence = occurrence
          Source = Some candidate.Source
          Publisher = candidate.Publisher
          Package = Some candidate.Package
          Retained = false
          Evidence = candidate.Evidence |> List.sortBy EvidenceId.value
          Authority = candidate.Authority |> List.sortWith (fun left right -> String.CompareOrdinal(left, right))
          FailureDomain = candidate.FailureDomain
          AttachmentNode =
            candidate.AttachmentNode
            |> Option.map (fun node ->
                TopologyNodeId.create
                    (sprintf "%s.%s" (TopologyNodeId.value node) (OccurrenceId.value occurrence))) }

    let private validatePort
        (request: ResolutionRequest)
        (definition: DefinitionId)
        (requirement: ResolutionRequirement)
        =
        match requirement.ContainingRegion, requirement.ContainingPort with
        | None, None -> None
        | Some region, Some port ->
            let matchingPorts =
                request.Ports
                |> List.filter (fun item -> item.Region = region && item.Port = port)
            match matchingPorts with
            | _ :: _ :: _ ->
                Some(
                    refusal
                        ContradictoryIdentity
                        (Some definition)
                        (Some requirement.Requirement)
                        (sprintf "Port '%s' has contradictory duplicate envelopes" (PortId.value port))
                        (Some region)
                        (Some port)
                        None)
            | [ envelope ]
                when envelope.Lifecycle = Sealed
                     || (requirement.RuntimeAttachment && envelope.Lifecycle <> RuntimeOpen) ->
                Some(refusal PortUnavailable (Some definition) (Some requirement.Requirement) (sprintf "Port '%s' is unavailable for the requested lifecycle." (PortId.value port)) (Some region) (Some port) None)
            | [ envelope ] ->
                let compatible =
                    envelope.Contracts
                    |> List.exists (fun provided ->
                        provided.Contract = requirement.Contract && provided.Version = requirement.Version)
                let importsAllowed =
                    requirement.RequiredImports
                    |> List.forall (fun requiredImport -> List.contains requiredImport envelope.Imports)
                let exportsAllowed =
                    requirement.RequiredExports
                    |> List.forall (fun requiredExport -> List.contains requiredExport envelope.Exports)
                let failurePolicyAllowed =
                    requirement.RequiredFailurePolicy
                    |> Option.forall (fun required -> required = envelope.FailurePolicy)
                let rollbackBoundaryAllowed =
                    requirement.RequiredRollbackBoundary
                    |> Option.forall (fun required -> required = envelope.RollbackBoundary)
                let authorityAllowed =
                    requirement.RequestedAuthority
                    |> List.forall (fun authority -> List.contains authority envelope.AuthorityCeiling)
                let topologyAllowed =
                    requirement.TopologyRequirements
                    |> List.forall (fun relation -> List.contains relation envelope.TopologyRequirements)
                let cardinalityAllowed =
                    requirement.Cardinality.Minimum >= envelope.Cardinality.Minimum
                    && (match envelope.Cardinality.Maximum, requirement.Cardinality.Maximum with
                        | None, _ -> true
                        | Some _, None -> false
                        | Some ceiling, Some maximum -> maximum <= ceiling)
                if
                    compatible
                    && importsAllowed
                    && exportsAllowed
                    && failurePolicyAllowed
                    && rollbackBoundaryAllowed
                    && authorityAllowed
                    && topologyAllowed
                    && cardinalityAllowed
                then
                    None
                elif envelope.AllowWiderGenerationProposal then
                    Some(
                        WiderGenerationRequired
                            { Region = region
                              Port = port
                              Requirement = requirement.Requirement
                              Reason = "child requirement exceeds the declared Port envelope" })
                else
                    Some(refusal PortEnvelopeExceeded (Some definition) (Some requirement.Requirement) "child requirement exceeds the declared Port envelope" (Some region) (Some port) None)
            | [] ->
                Some(refusal PortUnavailable (Some definition) (Some requirement.Requirement) (sprintf "Port '%s' is unavailable." (PortId.value port)) (Some region) (Some port) None)
        | region, port ->
            Some(refusal PortUnavailable (Some definition) (Some requirement.Requirement) "a child requirement must name both Region and Port" region port None)

    let private validateMediation
        (definition: DefinitionId)
        (requirement: ResolutionRequirement)
        (memberCount: int)
        =
        match requirement.Exposure, requirement.Mediation with
        | Distinct, _ -> None
        | Mediated, None ->
            Some(simpleRefusal MediationRequired (Some definition) (Some requirement.Requirement) "mediated exposure requires a declared Mediation")
        | Mediated, Some mediation
            when policyBearing mediation
                 && (mediation.Realization <> DedicatedComponent || Option.isNone mediation.Component) ->
            Some(simpleRefusal MediationRequiresComponent (Some definition) (Some requirement.Requirement) "policy-bearing Mediation requires a dedicated fake Component")
        | Mediated, Some _ when memberCount < 1 ->
            Some(simpleRefusal MissingDependency (Some definition) (Some requirement.Requirement) "Mediation has no backing member")
        | _ -> None

    let private findCycle (included: HashSet<DefinitionId>) (edges: HashSet<DefinitionId * DefinitionId>) =
        let adjacency =
            edges
            |> Seq.groupBy fst
            |> Seq.map (fun (key, values) -> key, values |> Seq.map snd |> Seq.distinct |> Seq.sortBy DefinitionId.value |> Seq.toList)
            |> Map.ofSeq
        let visited = HashSet<DefinitionId>()
        let visiting = HashSet<DefinitionId>()
        let rec visit current =
            if visiting.Contains current then Some current
            elif not (visited.Add current) then None
            else
                visiting.Add current |> ignore
                let result =
                    Map.tryFind current adjacency
                    |> Option.defaultValue []
                    |> List.tryPick visit
                visiting.Remove current |> ignore
                result
        included |> Seq.sortBy DefinitionId.value |> Seq.tryPick visit

    let resolve (input: ResolutionRequest) : ResolutionOutcome =
        // F# lists and records form the resolver's immutable request snapshot.
        let request =
            { input with
                Roots = List.ofSeq input.Roots
                Definitions = List.ofSeq input.Definitions
                Candidates = List.ofSeq input.Candidates
                ExistingOccurrences = List.ofSeq input.ExistingOccurrences
                OccupiedBindings = List.ofSeq input.OccupiedBindings
                Preferences = List.ofSeq input.Preferences
                AuthorisedReplacements = List.ofSeq input.AuthorisedReplacements
                CompositionParameters = List.ofSeq input.CompositionParameters
                ActivationParameters = List.ofSeq input.ActivationParameters
                PreselectedProviders = List.ofSeq input.PreselectedProviders
                Ports = List.ofSeq input.Ports
                TopologyClaims = List.ofSeq input.TopologyClaims }

        let duplicateDefinition =
            request.Definitions
            |> List.groupBy (fun definition -> definition.Definition)
            |> List.tryFind (fun (_, declarations) -> List.length declarations > 1)
        let duplicateCandidate =
            request.Candidates
            |> List.groupBy (fun candidate -> candidate.Definition, candidate.Source)
            |> List.tryFind (fun (_, observations) -> List.length observations > 1)

        match duplicateDefinition, duplicateCandidate with
        | Some (identity, _), _ ->
            simpleRefusal ContradictoryIdentity (Some identity) None (sprintf "definition '%s' has contradictory duplicate declarations" (DefinitionId.value identity))
        | None, Some ((definitionId, source), _) ->
            simpleRefusal ContradictoryIdentity (Some definitionId) None (sprintf "candidate '%s' from '%s' has contradictory duplicate observations" (DefinitionId.value definitionId) (SourceId.value source))
        | None, None ->
            let definitions = request.Definitions |> List.map (fun definition -> definition.Definition, definition) |> Map.ofList
            let pending = Queue<DefinitionId>(request.Roots |> List.sortBy DefinitionId.value)
            let accumulator =
                { Included = HashSet<DefinitionId>()
                  Pending = pending
                  Edges = HashSet<DefinitionId * DefinitionId>()
                  ProcessedRequirements = HashSet<RequirementId>()
                  ProviderSets = ResizeArray<ProviderSetObservation>()
                  Preferences = ResizeArray<PreferenceObservation>()
                  Exclusions = ResizeArray<CandidateExclusion>()
                  Conflicts = ResizeArray<ResolutionConflict>()
                  Decisions = ResizeArray<ResolutionDecision>()
                  Counters = Dictionary<DefinitionId, int>()
                  Shared = Dictionary<DefinitionId * BindingScopeId, OccurrenceId>() }

            let mutable terminal: ResolutionOutcome option = None

            while accumulator.Pending.Count > 0 && Option.isNone terminal do
                let definitionId = accumulator.Pending.Dequeue()
                match Map.tryFind definitionId definitions with
                | None ->
                    terminal <-
                        Some(simpleRefusal MissingDefinition (Some definitionId) None (sprintf "definition '%s' is not declared" (DefinitionId.value definitionId)))
                | Some definition when accumulator.Included.Add definitionId ->
                    for parameter in definition.CompositionParameters |> List.sortBy (fun item -> ParameterId.value item.Parameter) do
                        if Option.isNone terminal then
                            let choices =
                                request.CompositionParameters
                                |> List.filter (fun choice -> choice.Owner = definitionId && choice.Parameter = parameter.Parameter)
                            match choices with
                            | [] when parameter.Required ->
                                terminal <-
                                    Some(simpleRefusal MissingDefinition (Some definitionId) None (sprintf "required Composition Parameter '%s' has no selection" (ParameterId.value parameter.Parameter)))
                            | [] -> ()
                            | [ choice ] when List.contains choice.SelectedDefinition parameter.AllowedDefinitions ->
                                accumulator.Edges.Add(definitionId, choice.SelectedDefinition) |> ignore
                                accumulator.Pending.Enqueue choice.SelectedDefinition
                                accumulator.Decisions.Add
                                    { Requirement = None
                                      Definition = Some choice.SelectedDefinition
                                      Kind = "composition-parameter"
                                      Reason = sprintf "%s selected %s" (ParameterId.value parameter.Parameter) (DefinitionId.value choice.SelectedDefinition) }
                            | [ choice ] ->
                                terminal <-
                                    Some(simpleRefusal IncompatibleContract (Some definitionId) None (sprintf "Composition Parameter '%s' cannot select '%s'" (ParameterId.value parameter.Parameter) (DefinitionId.value choice.SelectedDefinition)))
                            | _ ->
                                terminal <-
                                    Some(simpleRefusal AmbiguousSelection (Some definitionId) None (sprintf "Composition Parameter '%s' has several selections" (ParameterId.value parameter.Parameter)))

                    for requirement in definition.Requirements |> List.sortBy (fun item -> RequirementId.value item.Requirement) do
                        if Option.isNone terminal then
                            if not (accumulator.ProcessedRequirements.Add requirement.Requirement) then
                                terminal <-
                                    Some(simpleRefusal ContradictoryIdentity (Some definitionId) (Some requirement.Requirement) (sprintf "requirement '%s' is declared more than once" (RequirementId.value requirement.Requirement)))
                            else
                                let unsupported =
                                    requirement.Constraints
                                    |> List.tryFind (fun constraintValue ->
                                        not (List.contains constraintValue.Name [ "platform"; "trust"; "origin"; "authority"; "resource"; "local-policy" ]))
                                match unsupported with
                                | Some constraintValue ->
                                    terminal <-
                                        Some(simpleRefusal UnsupportedConstraint (Some definitionId) (Some requirement.Requirement) (sprintf "Constraint '%s' has no CM2 evaluator" constraintValue.Name))
                                | None when requirement.Cardinality.Minimum > 0 && Option.isNone requirement.Cardinality.Maximum ->
                                    terminal <-
                                        Some(simpleRefusal UnboundedRequiredCardinality (Some definitionId) (Some requirement.Requirement) (sprintf "required Provider Set '%s' has no finite maximum" (RequirementId.value requirement.Requirement)))
                                | None ->
                                    match validatePort request definitionId requirement with
                                    | Some outcome -> terminal <- Some outcome
                                    | None ->
                                        let members = ResizeArray<ProviderSetMember>()
                                        let occupied =
                                            request.OccupiedBindings
                                            |> List.filter (fun binding -> binding.Scope = requirement.Scope && binding.Contract = requirement.Contract)
                                            |> List.sortBy (fun binding -> BindingId.value binding.Binding)
                                        if List.length occupied > 1 && requirement.Cardinality = Cardinality.parse "1..1" then
                                            terminal <-
                                                Some(simpleRefusal AmbiguousSelection (Some definitionId) (Some requirement.Requirement) "several occupied bindings claim one 1..1 role")
                                        elif List.length occupied = 1
                                             && requirement.Cardinality = Cardinality.parse "1..1"
                                             && not (List.contains occupied.Head.Binding request.AuthorisedReplacements) then
                                            let binding = occupied.Head
                                            match Map.tryFind binding.OccupantDefinition definitions with
                                            | Some occupiedDefinition
                                                when occupiedDefinition.Provides
                                                     |> List.exists (fun provided ->
                                                         provided.Contract = requirement.Contract
                                                         && provided.Version = requirement.Version) ->
                                                let matchingOccurrences =
                                                    request.ExistingOccurrences
                                                    |> List.filter (fun occurrence ->
                                                        occurrence.Occurrence = binding.OccupantOccurrence
                                                        && occurrence.Definition = binding.OccupantDefinition)
                                                if List.length matchingOccurrences <> 1 then
                                                    terminal <-
                                                        Some(
                                                            simpleRefusal
                                                                ContradictoryIdentity
                                                                (Some binding.OccupantDefinition)
                                                                (Some requirement.Requirement)
                                                                (sprintf
                                                                    "occupied binding '%s' has no matching retained occurrence or has contradictory duplicates"
                                                                    (BindingId.value binding.Binding)))
                                                else
                                                    members.Add
                                                        { Definition = binding.OccupantDefinition
                                                          Occurrence = binding.OccupantOccurrence
                                                          Source = None
                                                          Publisher = occupiedDefinition.Publisher
                                                          Package = None
                                                          Retained = true
                                                          Evidence = []
                                                          Authority = occupiedDefinition.RequestedAuthority
                                                          FailureDomain = "retained"
                                                          AttachmentNode = None }
                                                    accumulator.Pending.Enqueue binding.OccupantDefinition
                                                    accumulator.Edges.Add(definitionId, binding.OccupantDefinition) |> ignore
                                                    accumulator.Decisions.Add
                                                        { Requirement = Some requirement.Requirement
                                                          Definition = Some binding.OccupantDefinition
                                                          Kind = "retained-occupant"
                                                          Reason = sprintf "retained %s" (BindingId.value binding.Binding) }
                                            | _ ->
                                                accumulator.Conflicts.Add
                                                    { Requirement = requirement.Requirement
                                                      Kind = "incompatible-occupant"
                                                      Reason = sprintf "occupied binding '%s' is incompatible" (BindingId.value binding.Binding) }

                                        let allCompatibleCandidates =
                                            request.Candidates
                                            |> List.filter (fun candidate ->
                                                candidate.Provides
                                                |> List.exists (fun provided ->
                                                    provided.Contract = requirement.Contract
                                                    && provided.Version = requirement.Version))

                                        for candidate in allCompatibleCandidates do
                                            for rejected in candidate.Policy |> List.filter (fun observation -> not observation.Accepted) do
                                                accumulator.Exclusions.Add
                                                    { Requirement = requirement.Requirement
                                                      Definition = candidate.Definition
                                                      Source = candidate.Source
                                                      Domain = rejected.Domain
                                                      Reason = rejected.Reason }
                                                accumulator.Decisions.Add
                                                    { Requirement = Some requirement.Requirement
                                                      Definition = Some candidate.Definition
                                                      Kind = "candidate-excluded"
                                                      Reason =
                                                        sprintf
                                                            "%s %A: %s"
                                                            (SourceId.value candidate.Source)
                                                            rejected.Domain
                                                            rejected.Reason }

                                        // Source mirrors remain alternatives for one definition
                                        // position. A rejected earlier mirror must not hide an
                                        // accepted observation from another source.
                                        let compatibleCandidates =
                                            allCompatibleCandidates
                                            |> List.filter (fun candidate ->
                                                candidate.Policy
                                                |> List.forall (fun observation -> observation.Accepted))
                                            |> List.groupBy (fun candidate -> candidate.Definition)
                                            |> List.map (fun (_, mirrors) ->
                                                mirrors
                                                |> List.sortBy (fun candidate ->
                                                    rank request definition requirement candidate,
                                                    PublisherId.value candidate.Publisher,
                                                    PackageId.value candidate.Package,
                                                    SourceId.value candidate.Source)
                                                |> List.head)

                                        let admissible =
                                            compatibleCandidates
                                            |> List.sortBy (fun candidate ->
                                                rank request definition requirement candidate,
                                                DefinitionId.value candidate.Definition,
                                                PublisherId.value candidate.Publisher,
                                                PackageId.value candidate.Package,
                                                SourceId.value candidate.Source)

                                        for candidate in admissible do
                                            if members.Count < requirement.Cardinality.Minimum
                                               && not (members |> Seq.exists (fun memberValue -> memberValue.Definition = candidate.Definition)) then
                                                members.Add(memberFor accumulator requirement candidate)
                                                accumulator.Decisions.Add
                                                    { Requirement = Some requirement.Requirement
                                                      Definition = Some candidate.Definition
                                                      Kind = "required-provider-selected"
                                                      Reason = sprintf "rank %d selected %s" (rank request definition requirement candidate) (SourceId.value candidate.Source) }

                                        if members.Count < requirement.Cardinality.Minimum then
                                            let hasWrongVersion =
                                                request.Candidates
                                                |> List.exists (fun candidate ->
                                                    candidate.Provides
                                                    |> List.exists (fun provided -> provided.Contract = requirement.Contract))
                                            terminal <-
                                                Some(
                                                    simpleRefusal
                                                        (if hasWrongVersion then IncompatibleContract else MissingDependency)
                                                        (Some definitionId)
                                                        (Some requirement.Requirement)
                                                        (sprintf "Provider Set '%s' needs %d members but resolved %d" (RequirementId.value requirement.Requirement) requirement.Cardinality.Minimum members.Count))
                                        else
                                            for preselected in
                                                request.PreselectedProviders
                                                |> List.filter (fun item -> item.Requirement = requirement.Requirement)
                                                |> List.sortBy (fun item -> DefinitionId.value item.Definition) do
                                                if Option.isNone terminal
                                                   && not (members |> Seq.exists (fun memberValue -> memberValue.Definition = preselected.Definition)) then
                                                    match admissible |> List.tryFind (fun candidate -> candidate.Definition = preselected.Definition) with
                                                    | None ->
                                                        terminal <-
                                                            Some(simpleRefusal IncompatibleContract (Some preselected.Definition) (Some requirement.Requirement) (sprintf "preselected provider '%s' is unavailable or inadmissible" (DefinitionId.value preselected.Definition)))
                                                    | Some candidate ->
                                                        let maximum = Option.defaultValue Int32.MaxValue requirement.Cardinality.Maximum
                                                        if members.Count >= maximum then
                                                            terminal <-
                                                                Some(simpleRefusal PortEnvelopeExceeded (Some preselected.Definition) (Some requirement.Requirement) "preselection exceeds Provider Set maximum")
                                                        else
                                                            members.Add(memberFor accumulator requirement candidate)
                                                            accumulator.Decisions.Add
                                                                { Requirement = Some requirement.Requirement
                                                                  Definition = Some candidate.Definition
                                                                  Kind = "optional-provider-preselected"
                                                                  Reason = sprintf "explicit preselection used optional capacity from %s" (SourceId.value candidate.Source) }

                                            if Option.isNone terminal then
                                                match validateMediation definitionId requirement members.Count with
                                                | Some outcome -> terminal <- Some outcome
                                                | None ->
                                                    for memberValue in members do
                                                        accumulator.Pending.Enqueue memberValue.Definition
                                                        accumulator.Edges.Add(definitionId, memberValue.Definition) |> ignore
                                                    let orderedMembers =
                                                        members |> Seq.sortBy (fun memberValue -> OccurrenceId.value memberValue.Occurrence) |> Seq.toList
                                                    let plans =
                                                        orderedMembers
                                                        |> List.map (fun memberValue ->
                                                            { Requirement = requirement.Requirement
                                                              Member = memberValue.Occurrence
                                                              Direct = requirement.Exposure = Distinct
                                                              Mediation = requirement.Mediation |> Option.map (fun mediation -> mediation.Mediation) })
                                                    let maximum = Option.defaultValue members.Count requirement.Cardinality.Maximum
                                                    let alternatives =
                                                        allCompatibleCandidates
                                                        |> List.sortBy (fun candidate ->
                                                            rank request definition requirement candidate,
                                                            DefinitionId.value candidate.Definition,
                                                            PublisherId.value candidate.Publisher,
                                                            PackageId.value candidate.Package,
                                                            SourceId.value candidate.Source)
                                                        |> List.map (fun candidate ->
                                                            { Definition = candidate.Definition
                                                              Source = candidate.Source
                                                              Publisher = candidate.Publisher
                                                              Package = candidate.Package
                                                              Rank = rank request definition requirement candidate
                                                              Admissible = candidate.Policy |> List.forall (fun item -> item.Accepted)
                                                              ExclusionReasons =
                                                                candidate.Policy
                                                                |> List.filter (fun item -> not item.Accepted)
                                                                |> List.map (fun item -> sprintf "%A: %s" item.Domain item.Reason)
                                                                |> List.sortWith (fun left right -> String.CompareOrdinal(left, right)) })
                                                    accumulator.ProviderSets.Add
                                                        { Requirement = requirement.Requirement
                                                          Requester = definitionId
                                                          Contract = requirement.Contract
                                                          Version = requirement.Version
                                                          Scope = requirement.Scope
                                                          Cardinality = requirement.Cardinality
                                                          Exposure = requirement.Exposure
                                                          Members = orderedMembers
                                                          OptionalPositionsUnfilled = max 0 (maximum - members.Count)
                                                          Mediation = requirement.Mediation
                                                          BindingPlans = plans
                                                          ContainingRegion = requirement.ContainingRegion
                                                          ContainingPort = requirement.ContainingPort
                                                          Alternatives = alternatives }
                                                    for preference in
                                                        request.Preferences
                                                        |> List.filter (fun preference ->
                                                            preference.DeclaredBy = definitionId
                                                            && preference.Contract = requirement.Contract)
                                                        |> List.sortBy (fun preference -> PreferenceId.value preference.Preference) do
                                                        let used =
                                                            orderedMembers
                                                            |> List.exists (fun memberValue -> memberValue.Definition = preference.PreferredDefinition)
                                                        let retained = orderedMembers |> List.exists (fun memberValue -> memberValue.Retained)
                                                        accumulator.Preferences.Add
                                                            { Preference = preference.Preference
                                                              Requester = definitionId
                                                              PreferredDefinition = preference.PreferredDefinition
                                                              Requirement = requirement.Requirement
                                                              Used = used
                                                              Reason =
                                                                if used then "preferred-provider-selected"
                                                                elif retained then "compatible-occupant-retained"
                                                                else "preferred-provider-unavailable-or-excluded" }
                | Some _ -> ()

            match terminal with
            | Some outcome -> outcome
            | None ->
                match findCycle accumulator.Included accumulator.Edges with
                | Some cycle ->
                    simpleRefusal CycleRequiresCm3 (Some cycle) None (sprintf "dependency or composition cycle through '%s' requires CM3 group analysis" (DefinitionId.value cycle))
                | None ->
                    let parameters = ResizeArray<EffectiveParameter>()
                    let mutable parameterFailure: ResolutionOutcome option = None
                    for definitionId in accumulator.Included |> Seq.sortBy DefinitionId.value do
                        let definition = Map.find definitionId definitions
                        for slot in definition.ActivationParameters |> List.sortBy (fun item -> ParameterId.value item.Parameter) do
                            if Option.isNone parameterFailure then
                                let values = request.ActivationParameters |> List.filter (fun value -> value.Parameter = slot.Parameter)
                                match values, slot.DefaultValue, slot.Required with
                                | [ value ], _, _ ->
                                    parameters.Add
                                        { Definition = definitionId
                                          Parameter = slot.Parameter
                                          Value = value.Value
                                          Provenance = "environment" }
                                | [], Some defaultValue, _ ->
                                    parameters.Add
                                        { Definition = definitionId
                                          Parameter = slot.Parameter
                                          Value = defaultValue
                                          Provenance = "default" }
                                | [], None, true ->
                                    parameterFailure <-
                                        Some(refusal ActivationParameterUnavailable (Some definitionId) None (sprintf "Activation Parameter '%s' is unavailable" (ParameterId.value slot.Parameter)) None None (Some slot.Parameter))
                                | [], None, false -> ()
                                | _ ->
                                    parameterFailure <-
                                        Some(refusal AmbiguousSelection (Some definitionId) None (sprintf "Activation Parameter '%s' has several values" (ParameterId.value slot.Parameter)) None None (Some slot.Parameter))

                    match parameterFailure with
                    | Some outcome -> outcome
                    | None ->
                        let topology =
                            request.TopologyClaims
                            |> List.sortBy (fun claim -> ClaimId.value claim.Claim)
                            |> List.map (fun claim ->
                                { Claim = claim.Claim
                                  AssertedBy = claim.AssertedBy
                                  ClaimedRelation = claim.Relation
                                  EffectiveRelation =
                                    match claim.Disposition with
                                    | Accepted -> Some claim.Relation
                                    | Refined -> claim.RefinedRelation
                                    | Rejected -> None
                                  From = claim.From
                                  To = claim.To
                                  Disposition = claim.Disposition
                                  Reason = claim.Reason })
                        match topology |> List.tryFind (fun decision -> decision.Disposition = Refined && Option.isNone decision.EffectiveRelation) with
                        | Some _ ->
                            simpleRefusal ContradictoryIdentity None None "a refined topology claim must name its effective relation"
                        | None ->
                            let attachmentOccurrences =
                                accumulator.ProviderSets
                                |> Seq.collect (fun set -> set.Members)
                                |> Seq.choose (fun memberValue ->
                                    memberValue.AttachmentNode
                                    |> Option.map (fun node -> memberValue.Occurrence, node))
                                |> Seq.groupBy fst
                                |> Seq.map (fun (occurrence, values) ->
                                    occurrence,
                                    values |> Seq.map snd |> Seq.distinct |> Seq.toList)
                                |> Seq.toList
                            let inconsistentOccurrence =
                                attachmentOccurrences
                                |> List.exists (fun (_, nodes) -> List.length nodes <> 1)
                            let nodes =
                                attachmentOccurrences
                                |> List.choose (fun (_, nodeValues) -> List.tryHead nodeValues)
                            if inconsistentOccurrence || List.length (List.distinct nodes) <> List.length nodes then
                                simpleRefusal ContradictoryIdentity None None "attachment occurrences must have distinct local Topology Nodes"
                            else
                                let orderedDefinitions = accumulator.Included |> Seq.sortBy DefinitionId.value |> Seq.toList
                                let orderedSets = accumulator.ProviderSets |> Seq.sortBy (fun set -> RequirementId.value set.Requirement) |> Seq.toList
                                let orderedParameters =
                                    parameters
                                    |> Seq.sortBy (fun parameter -> DefinitionId.value parameter.Definition, ParameterId.value parameter.Parameter)
                                    |> Seq.toList
                                let usedParameters = orderedParameters |> List.map (fun parameter -> parameter.Parameter) |> Set.ofList
                                let unusedParameters =
                                    request.ActivationParameters
                                    |> List.filter (fun value -> not (Set.contains value.Parameter usedParameters))
                                    |> List.sortBy (fun value -> ParameterId.value value.Parameter)
                                let ports =
                                    request.Ports
                                    |> List.sortBy (fun port -> RegionId.value port.Region, PortId.value port.Port)
                                let authority =
                                    orderedDefinitions
                                    |> List.map (fun definitionId ->
                                        { Definition = definitionId
                                          RequestedAuthority =
                                            (Map.find definitionId definitions).RequestedAuthority
                                            |> List.sortWith (fun left right -> String.CompareOrdinal(left, right)) })
                                let proposed =
                                    { Generation = request.Generation
                                      RetainedActiveGeneration = request.ActiveGeneration
                                      RestartScope = request.RestartScope
                                      Roots = request.Roots |> List.sortBy DefinitionId.value
                                      Definitions = orderedDefinitions
                                      ProviderSets = orderedSets
                                      Preferences = accumulator.Preferences |> Seq.sortBy (fun item -> PreferenceId.value item.Preference) |> Seq.toList
                                      Exclusions =
                                        accumulator.Exclusions
                                        |> Seq.sortBy (fun item ->
                                            RequirementId.value item.Requirement,
                                            DefinitionId.value item.Definition,
                                            SourceId.value item.Source,
                                            item.Domain)
                                        |> Seq.toList
                                      Conflicts = accumulator.Conflicts |> Seq.sortBy (fun item -> RequirementId.value item.Requirement) |> Seq.toList
                                      Parameters = orderedParameters
                                      UnusedActivationParameters = unusedParameters
                                      Ports = ports
                                      RequestedAuthority = authority
                                      Topology = topology
                                      Decisions =
                                        accumulator.Decisions
                                        |> Seq.sortBy (fun item ->
                                            item.Requirement |> Option.map RequirementId.value |> Option.defaultValue "",
                                            item.Definition |> Option.map DefinitionId.value |> Option.defaultValue "",
                                            item.Kind)
                                        |> Seq.toList }
                                let generation =
                                    { Generation = request.Generation
                                      RestartScope = request.RestartScope
                                      Definitions = orderedDefinitions
                                      ProviderSets = orderedSets
                                      Parameters = orderedParameters
                                      Ports = ports
                                      RequestedAuthority = authority
                                      Topology = topology
                                      Effects = Cm2EffectObservation.none }
                                Resolved(proposed, generation)
