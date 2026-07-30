namespace Brontide.Minimal.Experimental.ComponentManagement

open System
open System.Collections.Generic

type ActivationDependencyKind =
    | OrdinaryInteraction
    | RelationalInitialisation
    | DescriptorExpansion

type ActivationStage =
    | LocalInitialisation
    | Interconnection
    | RelationalInitialisationStage
    | Ready

type Cm3EffectObservation =
    { Prepared: bool
      EstablishmentStarted: bool
      ActorEstablished: bool
      AuthorityGranted: bool
      LifecycleOperationExecuted: bool
      MemberReportedReady: bool
      Released: bool
      OrdinaryInteractionAdmitted: bool
      ActiveGenerationMutated: bool
      RollbackAttempted: bool }

[<RequireQualifiedAccess>]
module Cm3EffectObservation =
    let none =
        { Prepared = false
          EstablishmentStarted = false
          ActorEstablished = false
          AuthorityGranted = false
          LifecycleOperationExecuted = false
          MemberReportedReady = false
          Released = false
          OrdinaryInteractionAdmitted = false
          ActiveGenerationMutated = false
          RollbackAttempted = false }

type ActivationGroupMember =
    { Occurrence: OccurrenceId
      Definition: DefinitionId
      Region: RegionId
      Provides: ProvidedContract list
      RequiredReadyInputs: LifecycleInputId list
      AvailableReadyInputs: LifecycleInputId list
      WaitsForReadyOf: OccurrenceId list }

type ActivationDependency =
    { Edge: ActivationEdgeId
      From: OccurrenceId
      To: OccurrenceId
      Kind: ActivationDependencyKind
      Contract: ContractId
      Version: VersionLiteral
      ObservedBeforeRelease: bool
      Protocol: LifecycleProtocolId option
      CrossingPort: PortId option
      AllowWiderRegionProposal: bool }

type LifecycleProtocolDeclaration =
    { Protocol: LifecycleProtocolId
      Edge: ActivationEdgeId
      From: OccurrenceId
      To: OccurrenceId
      Operation: LifecycleOperationId
      Authority: CapabilityId list
      InputShape: ShapeId
      OutputShape: ShapeId
      Ordering: string
      TimeoutMilliseconds: int
      RetryLimit: int
      Idempotent: bool
      Completion: string
      Failure: string
      Rollback: string }

type RegionCrossingDeclaration =
    { Edge: ActivationEdgeId
      FromRegion: RegionId
      ToRegion: RegionId
      Port: PortId
      ImportDeclared: bool
      ExportDeclared: bool }

type ActivationGroupRequest =
    { Request: ActivationGroupRequestId
      Generation: GenerationId
      RestartScope: RestartScopeId
      Members: ActivationGroupMember list
      Edges: ActivationDependency list
      Protocols: LifecycleProtocolDeclaration list
      RegionCrossings: RegionCrossingDeclaration list }

type ActivationStageObservation =
    { Stage: ActivationStage
      OrdinaryGateOpen: bool }

type ActivationGroupObservation =
    { Group: ActivationGroupId
      Cyclic: bool
      Members: ActivationGroupMember list
      InternalEdges: ActivationDependency list
      Protocols: LifecycleProtocolDeclaration list
      RegionCrossings: RegionCrossingDeclaration list
      Stages: ActivationStageObservation list
      ReleasePending: bool }

type InterGroupEdgeObservation =
    { Edge: ActivationEdgeId
      FromGroup: ActivationGroupId
      ToGroup: ActivationGroupId }

type ActivationGroupDecision =
    { Group: ActivationGroupId option
      Member: OccurrenceId option
      Edge: ActivationEdgeId option
      Kind: string
      Reason: string }

type ActivationGroupPlan =
    { Generation: GenerationId
      RestartScope: RestartScopeId
      Groups: ActivationGroupObservation list
      InterGroupEdges: InterGroupEdgeObservation list
      RegionCrossings: RegionCrossingDeclaration list
      Decisions: ActivationGroupDecision list
      Effects: Cm3EffectObservation }

type ActivationGroupFailureKind =
    | ContradictoryIdentity
    | MissingMember
    | RecursiveDescriptorExpansion
    | ContractVersionConflict
    | LifecycleProtocolRequired
    | LifecycleProtocolIncomplete
    | UndeclaredLifecycleTraffic
    | OrdinaryPreReleaseTraffic
    | ReadyInputUnavailable
    | CircularReadyWait
    | RegionCrossingRequired
    | RegionCrossingConflict

type ActivationGroupFailure =
    { Kind: ActivationGroupFailureKind
      Group: ActivationGroupId option
      Member: OccurrenceId option
      Edge: ActivationEdgeId option
      Source: OccurrenceId option
      Target: OccurrenceId option
      Contract: ContractId option
      Version: VersionLiteral option
      Protocol: LifecycleProtocolId option
      Region: RegionId option
      Port: PortId option
      Reason: string }

type WiderActivationGroupProposal =
    { Generation: GenerationId
      RestartScope: RestartScopeId
      Edge: ActivationEdgeId
      FromRegion: RegionId
      ToRegion: RegionId
      Port: PortId
      Reason: string }

type ActivationGroupOutcome =
    | Planned of ActivationGroupPlan
    | WiderParentGenerationRequired of WiderActivationGroupProposal
    | ActivationGroupRefused of ActivationGroupFailure

type ActivationGroupOutcome with
    member _.Effects = Cm3EffectObservation.none

[<RequireQualifiedAccess>]
module FakeActivationGroupPlanner =
    let private refusal kind reason group memberValue edge protocol region port =
        ActivationGroupRefused
            { Kind = kind
              Group = group
              Member = memberValue
              Edge = edge
              Source = None
              Target = None
              Contract = None
              Version = None
              Protocol = protocol
              Region = region
              Port = port
              Reason = reason }

    let private simpleRefusal kind reason =
        refusal kind reason None None None None None None

    let private contractRefusal (edge: ActivationDependency) =
        ActivationGroupRefused
            { Kind = ContractVersionConflict
              Group = None
              Member = Some edge.To
              Edge = Some edge.Edge
              Source = Some edge.From
              Target = Some edge.To
              Contract = Some edge.Contract
              Version = Some edge.Version
              Protocol = None
              Region = None
              Port = None
              Reason =
                sprintf
                    "edge '%s' requires '%s' version '%s' that '%s' does not provide"
                    (ActivationEdgeId.value edge.Edge)
                    (ContractId.value edge.Contract)
                    (VersionLiteral.value edge.Version)
                    (OccurrenceId.value edge.To) }

    let private duplicate key values =
        values
        |> List.groupBy key
        |> List.tryPick (fun (identity, declarations) ->
            if List.length declarations > 1 then Some identity else None)

    let private findCycle members edges =
        let adjacency =
            edges
            |> List.groupBy fst
            |> List.map (fun (key, values) ->
                key,
                values
                |> List.map snd
                |> List.distinct
                |> List.sortBy OccurrenceId.value)
            |> Map.ofList
        let visited = HashSet<OccurrenceId>()
        let visiting = HashSet<OccurrenceId>()
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
        members |> List.sortBy OccurrenceId.value |> List.tryPick visit

    let private stronglyConnectedComponents
        (members: ActivationGroupMember list)
        (edges: ActivationDependency list)
        =
        let adjacency =
            members
            |> List.map (fun memberValue ->
                memberValue.Occurrence,
                edges
                |> List.filter (fun edge -> edge.From = memberValue.Occurrence)
                |> List.map (fun edge -> edge.To)
                |> List.distinct
                |> List.sortBy OccurrenceId.value)
            |> Map.ofList
        let reverse =
            members
            |> List.map (fun memberValue ->
                memberValue.Occurrence,
                edges
                |> List.filter (fun edge -> edge.To = memberValue.Occurrence)
                |> List.map (fun edge -> edge.From)
                |> List.distinct
                |> List.sortBy OccurrenceId.value)
            |> Map.ofList
        let visited = HashSet<OccurrenceId>()
        let order = ResizeArray<OccurrenceId>()
        let rec visit current =
            if visited.Add current then
                Map.find current adjacency |> List.iter visit
                order.Add current
        members
        |> List.sortBy (fun item -> OccurrenceId.value item.Occurrence)
        |> List.iter (fun item -> visit item.Occurrence)

        visited.Clear()
        let rec collect current accumulated =
            if visited.Add current then
                Map.find current reverse
                |> List.fold (fun state next -> collect next state) (current :: accumulated)
            else accumulated
        order
        |> Seq.rev
        |> Seq.fold (fun components current ->
            if visited.Contains current then components
            else
                (collect current [] |> List.sortBy OccurrenceId.value) :: components) []
        |> List.rev

    let private validateCrossing
        (request: ActivationGroupRequest)
        (edge: ActivationDependency)
        (source: ActivationGroupMember)
        (target: ActivationGroupMember)
        =
        let declarations =
            request.RegionCrossings |> List.filter (fun item -> item.Edge = edge.Edge)
        if source.Region = target.Region then
            if List.isEmpty declarations then None
            else
                Some(
                    refusal
                        RegionCrossingConflict
                        (sprintf "same-Region edge '%s' carries a Region-crossing declaration" (ActivationEdgeId.value edge.Edge))
                        None
                        None
                        (Some edge.Edge)
                        None
                        (Some source.Region)
                        edge.CrossingPort)
        else
            match declarations with
            | [] ->
                match edge.AllowWiderRegionProposal, edge.CrossingPort with
                | true, Some port ->
                    Some(
                        WiderParentGenerationRequired
                            { Generation = request.Generation
                              RestartScope = request.RestartScope
                              Edge = edge.Edge
                              FromRegion = source.Region
                              ToRegion = target.Region
                              Port = port
                              Reason = "cross-Region dependency requires a wider parent generation" })
                | _ ->
                    Some(
                        refusal
                            RegionCrossingRequired
                            (sprintf "cross-Region edge '%s' has no declared Port crossing" (ActivationEdgeId.value edge.Edge))
                            None
                            None
                            (Some edge.Edge)
                            None
                            (Some target.Region)
                            edge.CrossingPort)
            | [ crossing ]
                when edge.CrossingPort = Some crossing.Port
                     && crossing.FromRegion = source.Region
                     && crossing.ToRegion = target.Region
                     && crossing.ImportDeclared
                     && crossing.ExportDeclared ->
                None
            | [ crossing ] ->
                Some(
                    refusal
                        RegionCrossingConflict
                        (sprintf "Region crossing for edge '%s' does not match its Port, Regions, import, and export declarations" (ActivationEdgeId.value edge.Edge))
                        None
                        None
                        (Some edge.Edge)
                        None
                        (Some target.Region)
                        (Some crossing.Port))
            | _ ->
                Some(
                    refusal
                        ContradictoryIdentity
                        (sprintf "edge '%s' has contradictory duplicate Region crossings" (ActivationEdgeId.value edge.Edge))
                        None
                        None
                        (Some edge.Edge)
                        None
                        (Some target.Region)
                        edge.CrossingPort)

    let private validateProtocol
        (request: ActivationGroupRequest)
        (groupId: ActivationGroupId)
        (edge: ActivationDependency)
        : Result<LifecycleProtocolDeclaration, ActivationGroupOutcome> =
        match edge.Protocol with
        | None ->
            Error(
                refusal
                    LifecycleProtocolRequired
                    (sprintf "relational edge '%s' has no lifecycle protocol" (ActivationEdgeId.value edge.Edge))
                    (Some groupId)
                    None
                    (Some edge.Edge)
                    None
                    None
                    None)
        | Some protocolId ->
            let matches =
                request.Protocols
                |> List.filter (fun (item: LifecycleProtocolDeclaration) -> item.Protocol = protocolId)
            match matches with
            | [ protocol ] ->
                let complete =
                    protocol.Edge = edge.Edge
                    && protocol.From = edge.From
                    && protocol.To = edge.To
                    && not (String.IsNullOrEmpty(LifecycleOperationId.value protocol.Operation))
                    && not (List.isEmpty protocol.Authority)
                    && not (String.IsNullOrWhiteSpace protocol.Ordering)
                    && protocol.TimeoutMilliseconds > 0
                    && protocol.RetryLimit >= 0
                    && not (String.IsNullOrWhiteSpace protocol.Completion)
                    && not (String.IsNullOrWhiteSpace protocol.Failure)
                    && not (String.IsNullOrWhiteSpace protocol.Rollback)
                if complete then Ok protocol
                else
                    Error(
                        refusal
                            LifecycleProtocolIncomplete
                            (sprintf "lifecycle protocol '%s' is incomplete or misdirected" (LifecycleProtocolId.value protocol.Protocol))
                            (Some groupId)
                            None
                            (Some edge.Edge)
                            (Some protocol.Protocol)
                            None
                            None)
            | _ ->
                Error(
                    refusal
                        LifecycleProtocolRequired
                        (sprintf "relational edge '%s' has no unique lifecycle protocol '%s'" (ActivationEdgeId.value edge.Edge) (LifecycleProtocolId.value protocolId))
                        (Some groupId)
                        None
                        (Some edge.Edge)
                        (Some protocolId)
                        None
                        None)

    let private dependencyFirst
        (groups: ActivationGroupObservation list)
        (edges: InterGroupEdgeObservation list)
        =
        let mutable remaining =
            groups
            |> List.map (fun (group: ActivationGroupObservation) -> group.Group, group)
            |> Map.ofList
        let ordered = ResizeArray<ActivationGroupObservation>()
        while not (Map.isEmpty remaining) do
            let ready =
                remaining
                |> Map.toList
                |> List.map snd
                |> List.filter (fun (group: ActivationGroupObservation) ->
                    not (
                        edges
                        |> List.exists (fun (edge: InterGroupEdgeObservation) ->
                            edge.FromGroup = group.Group
                            && Map.containsKey edge.ToGroup remaining)))
                |> List.sortBy (fun (group: ActivationGroupObservation) -> ActivationGroupId.value group.Group)
            for group in ready do
                ordered.Add group
                remaining <- Map.remove group.Group remaining
        ordered |> Seq.toList

    let plan (input: ActivationGroupRequest) : ActivationGroupOutcome =
        // F# records and lists form the planner's detached immutable snapshot.
        let request =
            { input with
                Members =
                    input.Members
                    |> List.map (fun (item: ActivationGroupMember) ->
                        { item with
                            Provides = List.ofSeq item.Provides
                            RequiredReadyInputs = List.ofSeq item.RequiredReadyInputs
                            AvailableReadyInputs = List.ofSeq item.AvailableReadyInputs
                            WaitsForReadyOf = List.ofSeq item.WaitsForReadyOf })
                Edges = List.ofSeq input.Edges
                Protocols =
                    input.Protocols
                    |> List.map (fun (item: LifecycleProtocolDeclaration) ->
                        { item with Authority = List.ofSeq item.Authority })
                RegionCrossings = List.ofSeq input.RegionCrossings }

        let mutable terminal: ActivationGroupOutcome option = None
        match duplicate (fun (item: ActivationGroupMember) -> item.Occurrence) request.Members with
        | Some identity ->
            terminal <-
                Some(
                    refusal
                        ContradictoryIdentity
                        (sprintf "occurrence '%s' has contradictory duplicate member declarations" (OccurrenceId.value identity))
                        None
                        (Some identity)
                        None
                        None
                        None
                        None)
        | None -> ()
        if Option.isNone terminal then
            match duplicate (fun (item: ActivationDependency) -> item.Edge) request.Edges with
            | Some identity ->
                terminal <-
                    Some(
                        refusal
                            ContradictoryIdentity
                            (sprintf "edge '%s' has contradictory duplicate declarations" (ActivationEdgeId.value identity))
                            None
                            None
                            (Some identity)
                            None
                            None
                            None)
            | None -> ()
        if Option.isNone terminal then
            match duplicate (fun (item: LifecycleProtocolDeclaration) -> item.Protocol) request.Protocols with
            | Some identity ->
                terminal <-
                    Some(
                        refusal
                            ContradictoryIdentity
                            (sprintf "protocol '%s' has contradictory duplicate declarations" (LifecycleProtocolId.value identity))
                            None
                            None
                            None
                            (Some identity)
                            None
                            None)
            | None -> ()
        if Option.isNone terminal then
            match duplicate (fun (item: LifecycleProtocolDeclaration) -> item.Edge) request.Protocols with
            | Some identity ->
                terminal <-
                    Some(
                        refusal
                            ContradictoryIdentity
                            (sprintf "edge '%s' has contradictory duplicate lifecycle protocols" (ActivationEdgeId.value identity))
                            None
                            None
                            (Some identity)
                            None
                            None
                            None)
            | None -> ()
        if Option.isNone terminal then
            match duplicate (fun (item: RegionCrossingDeclaration) -> item.Edge) request.RegionCrossings with
            | Some identity ->
                terminal <-
                    Some(
                        refusal
                            ContradictoryIdentity
                            (sprintf "edge '%s' has contradictory duplicate Region crossings" (ActivationEdgeId.value identity))
                            None
                            None
                            (Some identity)
                            None
                            None
                            None)
            | None -> ()

        let members =
            request.Members
            |> List.map (fun item -> item.Occurrence, item)
            |> Map.ofList

        for edge in request.Edges |> List.sortBy (fun item -> ActivationEdgeId.value item.Edge) do
            if Option.isNone terminal then
                match Map.tryFind edge.From members, Map.tryFind edge.To members with
                | None, _ ->
                    terminal <-
                        Some(
                            refusal
                                MissingMember
                                (sprintf "edge '%s' names missing source occurrence '%s'" (ActivationEdgeId.value edge.Edge) (OccurrenceId.value edge.From))
                                None
                                (Some edge.From)
                                (Some edge.Edge)
                                None
                                None
                                None)
                | _, None ->
                    terminal <-
                        Some(
                            refusal
                                MissingMember
                                (sprintf "edge '%s' names missing target occurrence '%s'" (ActivationEdgeId.value edge.Edge) (OccurrenceId.value edge.To))
                                None
                                (Some edge.To)
                                (Some edge.Edge)
                                None
                                None
                                None)
                | Some source, Some target ->
                    let matchingProvisionCount =
                        target.Provides
                        |> List.filter (fun provided ->
                            provided.Contract = edge.Contract
                            && provided.Version = edge.Version)
                        |> List.length
                    let contractMatches =
                        edge.Kind = DescriptorExpansion
                        || matchingProvisionCount = 1
                    if not contractMatches then
                        terminal <-
                            Some(contractRefusal edge)
                    elif edge.Kind = OrdinaryInteraction && edge.ObservedBeforeRelease then
                        terminal <-
                            Some(
                                refusal
                                    OrdinaryPreReleaseTraffic
                                    (sprintf "ordinary edge '%s' was observed before Release" (ActivationEdgeId.value edge.Edge))
                                    None
                                    None
                                    (Some edge.Edge)
                                    None
                                    None
                                    None)
                    elif edge.Kind <> RelationalInitialisation && Option.isSome edge.Protocol then
                        terminal <-
                            Some(
                                refusal
                                    UndeclaredLifecycleTraffic
                                    (sprintf "non-lifecycle edge '%s' names a lifecycle protocol" (ActivationEdgeId.value edge.Edge))
                                    None
                                    None
                                    (Some edge.Edge)
                                    edge.Protocol
                                    None
                                    None)
                    else
                        terminal <- validateCrossing request edge source target

        if Option.isNone terminal then
            let unreferenced =
                request.Protocols
                |> List.tryFind (fun protocol ->
                    request.Edges
                    |> List.exists (fun edge ->
                        edge.Kind = RelationalInitialisation
                        && (edge.Protocol = Some protocol.Protocol || edge.Edge = protocol.Edge))
                    |> not)
            match unreferenced with
            | Some protocol ->
                terminal <-
                    Some(
                        refusal
                            UndeclaredLifecycleTraffic
                            (sprintf "lifecycle protocol '%s' is not declared by a relational edge" (LifecycleProtocolId.value protocol.Protocol))
                            None
                            None
                            (Some protocol.Edge)
                            (Some protocol.Protocol)
                            None
                            None)
            | None -> ()

        for memberValue in request.Members |> List.sortBy (fun item -> OccurrenceId.value item.Occurrence) do
            if Option.isNone terminal then
                let missingInput =
                    memberValue.RequiredReadyInputs
                    |> List.filter (fun inputValue ->
                        not (List.contains inputValue memberValue.AvailableReadyInputs))
                    |> List.sortBy LifecycleInputId.value
                    |> List.tryHead
                match missingInput with
                | Some inputValue ->
                    terminal <-
                        Some(
                            refusal
                                ReadyInputUnavailable
                                (sprintf "member '%s' cannot reach Ready because input '%s' is unavailable" (OccurrenceId.value memberValue.Occurrence) (LifecycleInputId.value inputValue))
                                None
                                (Some memberValue.Occurrence)
                                None
                                None
                                None
                                None)
                | None ->
                    match
                        memberValue.WaitsForReadyOf
                        |> List.filter (fun wait -> not (Map.containsKey wait members))
                        |> List.sortBy OccurrenceId.value
                        |> List.tryHead
                    with
                    | Some unknown ->
                        terminal <-
                            Some(
                                refusal
                                    MissingMember
                                    (sprintf "member waits for missing occurrence '%s'" (OccurrenceId.value unknown))
                                    None
                                    (Some unknown)
                                    None
                                    None
                                    None
                                    None)
                    | None -> ()

        if Option.isNone terminal then
            let waits =
                request.Members
                |> List.collect (fun memberValue ->
                    memberValue.WaitsForReadyOf
                    |> List.map (fun wait -> memberValue.Occurrence, wait))
            match findCycle (request.Members |> List.map (fun item -> item.Occurrence)) waits with
            | Some memberValue ->
                terminal <-
                    Some(
                        refusal
                            CircularReadyWait
                            (sprintf "Ready wait cycle passes through '%s'" (OccurrenceId.value memberValue))
                            None
                            (Some memberValue)
                            None
                            None
                            None
                            None)
            | None -> ()

        match terminal with
        | Some outcome -> outcome
        | None ->
            let components = stronglyConnectedComponents request.Members request.Edges
            let groupForMember = Dictionary<OccurrenceId, ActivationGroupId>()
            let groups = ResizeArray<ActivationGroupObservation>()
            let decisions = ResizeArray<ActivationGroupDecision>()

            for componentMembers in components do
                if Option.isNone terminal then
                    let orderedMembers =
                        componentMembers
                        |> List.map (fun identity -> Map.find identity members)
                        |> List.sortBy (fun item -> OccurrenceId.value item.Occurrence)
                    let groupId =
                        ActivationGroupId.create
                            (sprintf "group.%s" (OccurrenceId.value orderedMembers.Head.Occurrence))
                    for memberValue in orderedMembers do
                        groupForMember[memberValue.Occurrence] <- groupId
                        decisions.Add
                            { Group = Some groupId
                              Member = Some memberValue.Occurrence
                              Edge = None
                              Kind = "member-grouped"
                              Reason = sprintf "member belongs to %s" (ActivationGroupId.value groupId) }
                    let memberIds = componentMembers |> Set.ofList
                    let internalEdges =
                        request.Edges
                        |> List.filter (fun edge ->
                            Set.contains edge.From memberIds
                            && Set.contains edge.To memberIds)
                        |> List.sortBy (fun edge -> ActivationEdgeId.value edge.Edge)
                    let cyclic =
                        List.length orderedMembers > 1
                        || (internalEdges |> List.exists (fun edge -> edge.From = edge.To))
                    match
                        if cyclic then
                            internalEdges
                            |> List.tryFind (fun edge -> edge.Kind = DescriptorExpansion)
                        else None
                    with
                    | Some expansion ->
                        terminal <-
                            Some(
                                refusal
                                    RecursiveDescriptorExpansion
                                    (sprintf "cyclic group '%s' contains descriptor-expansion edge '%s'" (ActivationGroupId.value groupId) (ActivationEdgeId.value expansion.Edge))
                                    (Some groupId)
                                    None
                                    (Some expansion.Edge)
                                    None
                                    None
                                    None)
                    | None ->
                        let protocols = ResizeArray<LifecycleProtocolDeclaration>()
                        for edge in internalEdges |> List.filter (fun item -> item.Kind = RelationalInitialisation) do
                            if Option.isNone terminal then
                                match validateProtocol request groupId edge with
                                | Ok protocol ->
                                    protocols.Add protocol
                                    decisions.Add
                                        { Group = Some groupId
                                          Member = None
                                          Edge = Some edge.Edge
                                          Kind = "relational-protocol"
                                          Reason = sprintf "bounded protocol %s accepted" (LifecycleProtocolId.value protocol.Protocol) }
                                | Error outcome -> terminal <- Some outcome
                        if Option.isNone terminal then
                            let crossings =
                                request.RegionCrossings
                                |> List.filter (fun crossing ->
                                    internalEdges
                                    |> List.exists (fun edge -> edge.Edge = crossing.Edge))
                                |> List.sortBy (fun item -> ActivationEdgeId.value item.Edge)
                            let stages =
                                [ { Stage = LocalInitialisation; OrdinaryGateOpen = false }
                                  { Stage = Interconnection; OrdinaryGateOpen = false } ]
                                @ (if protocols.Count > 0 then
                                       [ { Stage = RelationalInitialisationStage; OrdinaryGateOpen = false } ]
                                   else [])
                                @ [ { Stage = Ready; OrdinaryGateOpen = false } ]
                            groups.Add
                                { Group = groupId
                                  Cyclic = cyclic
                                  Members = orderedMembers
                                  InternalEdges = internalEdges
                                  Protocols =
                                    protocols
                                    |> Seq.sortBy (fun item -> LifecycleProtocolId.value item.Protocol)
                                    |> Seq.toList
                                  RegionCrossings = crossings
                                  Stages = stages
                                  ReleasePending = true }
                            for edge in internalEdges do
                                decisions.Add
                                    { Group = Some groupId
                                      Member = None
                                      Edge = Some edge.Edge
                                      Kind = "internal-edge"
                                      Reason =
                                        if cyclic then "closed inside one activation group"
                                        else "self-contained dependency" }

            match terminal with
            | Some outcome -> outcome
            | None ->
                let crossGroupLifecycle =
                    request.Edges
                    |> List.filter (fun edge ->
                        edge.Kind = RelationalInitialisation
                        && groupForMember[edge.From] <> groupForMember[edge.To])
                    |> List.sortBy (fun edge -> ActivationEdgeId.value edge.Edge)
                    |> List.tryHead
                match crossGroupLifecycle with
                | Some edge ->
                    refusal
                        UndeclaredLifecycleTraffic
                        (sprintf "relational edge '%s' crosses activation-group boundaries" (ActivationEdgeId.value edge.Edge))
                        None
                        None
                        (Some edge.Edge)
                        edge.Protocol
                        None
                        None
                | None ->
                    let interGroupEdges =
                        request.Edges
                        |> List.filter (fun edge ->
                            groupForMember[edge.From] <> groupForMember[edge.To])
                        |> List.map (fun edge ->
                            { Edge = edge.Edge
                              FromGroup = groupForMember[edge.From]
                              ToGroup = groupForMember[edge.To] })
                        |> List.sortBy (fun edge -> ActivationEdgeId.value edge.Edge)
                    for edge in interGroupEdges do
                        decisions.Add
                            { Group = Some edge.FromGroup
                              Member = None
                              Edge = Some edge.Edge
                              Kind = "inter-group-edge"
                              Reason =
                                sprintf
                                    "%s depends on %s"
                                    (ActivationGroupId.value edge.FromGroup)
                                    (ActivationGroupId.value edge.ToGroup) }
                    Planned
                        { Generation = request.Generation
                          RestartScope = request.RestartScope
                          Groups = dependencyFirst (groups |> Seq.toList) interGroupEdges
                          InterGroupEdges = interGroupEdges
                          RegionCrossings =
                            request.RegionCrossings
                            |> List.sortBy (fun crossing -> ActivationEdgeId.value crossing.Edge)
                          Decisions =
                            decisions
                            |> Seq.sortBy (fun item ->
                                item.Group |> Option.map ActivationGroupId.value |> Option.defaultValue "",
                                item.Member |> Option.map OccurrenceId.value |> Option.defaultValue "",
                                item.Edge |> Option.map ActivationEdgeId.value |> Option.defaultValue "",
                                item.Kind)
                            |> Seq.toList
                          Effects = Cm3EffectObservation.none }
