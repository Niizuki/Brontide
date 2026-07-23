namespace Brontide.Minimal.Kernel

open System
open Brontide.Minimal.Model

type World =
    private
        { Scope: Guid
          NextReference: int64
          AuthorityActor: ActorReference
          Actors: Map<ActorReference, Actor>
          Capabilities: Map<CapabilityReference, Capability>
          CapabilityConstraintExpressions: Map<CapabilityReference, ConstraintExpression list>
          Shapes: Map<ShapeReference, ShapeDefinition>
          Fragments: Map<FragmentReference, FragmentDefinition>
          Constraints: Map<ConstraintReference, ConstraintDefinition>
          Operations: Map<OperationReference, OperationDefinition>
          EventDefinitions: Map<EventReference, EventDefinition>
          Executions: ExecutionAudit list
          Events: Event list
          Provenance: ProvenanceClaim list
          GenesisOccurrences: GenesisOccurrence list
          TimeDomain: TimeDomainReference
          LastLogicalTime: int64 }

type ConstraintContext =
    { Request: ExecutionRequest
      Operation: OperationDefinition
      LogicalTime: int64 }

type ConstraintEvaluator = ShapeValue -> ConstraintContext -> Result<unit, string>

type OperationFailure =
    { Reason: string
      DetailsShape: ShapeReference option
      Details: ShapeValue option }

[<RequireQualifiedAccess>]
module OperationFailure =
    let withoutDetails reason =
        { Reason = reason
          DetailsShape = None
          Details = None }

    let withDetails detailsShape details reason =
        { Reason = reason
          DetailsShape = Some detailsShape
          Details = Some details }

type OperationHandler =
    ExecutionRequest -> Result<ShapeValue * EventDraft list * (CanonicalName * string) list, OperationFailure>
type Environment =
    { TrustedTime: TemporalMark
      ConstraintEvaluators: Map<ConstraintReference, ConstraintEvaluator>
      Handlers: Map<OperationReference, OperationHandler> }
type StepResult =
    { World: World
      Outcome: ExecutionOutcome
      EmittedEvents: Event list
      Provenance: ProvenanceClaim list }

type GenesisContext internal (scope: Guid) =
    let mutable active = true

    member internal _.EnsureActive(worldScope: Guid) =
        if not active then
            invalidOp "A completed Genesis context cannot introduce authority."

        if scope <> worldScope then
            invalidOp "A Genesis context cannot introduce authority into another domain."

    member internal _.Complete() = active <- false

[<RequireQualifiedAccess>]
module World =
    let private builtInShape (reference: ShapeReference) (body: ShapeBody) : ShapeDefinition =
        { Reference = reference
          Description = "Brontide.Minimal Base shape"
          Body = body
          AcceptedFragments = Set.empty
          IsOpenToFragments = false }

    let create (scope: Guid) (timeDomain: TimeDomainReference) : World =
        let shapes =
            [ builtInShape BuiltIn.unitShape UnitShape
              builtInShape BuiltIn.booleanShape (ScalarShape Boolean)
              builtInShape BuiltIn.integerShape (ScalarShape Integer)
              builtInShape BuiltIn.decimalShape (ScalarShape Decimal)
              builtInShape BuiltIn.textShape (ScalarShape Text)
              builtInShape BuiltIn.bytesShape (ScalarShape Bytes) ]
            |> Seq.map (fun definition -> definition.Reference, definition)
            |> Map.ofSeq

        let authorityReference = ActorReference.issue scope 1L
        let authorityActor =
            { Reference = authorityReference
              Name = CanonicalName.create "Brontide.Minimal:AuthorityDomain" }

        { Scope = scope
          NextReference = 2L
          AuthorityActor = authorityReference
          Actors = Map.ofList [ authorityReference, authorityActor ]
          Capabilities = Map.empty
          CapabilityConstraintExpressions = Map.empty
          Shapes = shapes
          Fragments = Map.empty
          Constraints = Map.empty
          Operations = Map.empty
          EventDefinitions = Map.empty
          Executions = []
          Events = []
          Provenance = []
          GenesisOccurrences = []
          TimeDomain = timeDomain
          LastLogicalTime = Int64.MinValue }

    let scope (world: World) = world.Scope
    let actors (world: World) = world.Actors |> Map.toSeq |> Seq.map snd |> Seq.toList
    let shapes (world: World) = world.Shapes |> Map.toSeq |> Seq.map snd |> Seq.toList
    let operations (world: World) = world.Operations |> Map.toSeq |> Seq.map snd |> Seq.toList
    let capabilities (world: World) = world.Capabilities |> Map.toSeq |> Seq.map snd |> Seq.toList
    let executions (world: World) = world.Executions
    let events (world: World) = world.Events
    let provenance (world: World) = world.Provenance
    let genesisOccurrences (world: World) = world.GenesisOccurrences
    let timeDomain (world: World) = world.TimeDomain
    let lastLogicalTime (world: World) = world.LastLogicalTime
    let tryFindShape (reference: ShapeReference) (world: World) = Map.tryFind reference world.Shapes
    let tryFindOperation (reference: OperationReference) (world: World) = Map.tryFind reference world.Operations
    let tryFindCapability (reference: CapabilityReference) (world: World) =
        Map.tryFind reference world.Capabilities

    let genesis
        (policy: CanonicalName)
        (recordedAt: TemporalMark)
        (initialize: GenesisContext -> World -> 'T * World)
        (world: World)
        =
        if recordedAt.TimeDomain <> world.TimeDomain then
            Error "Genesis must use the authority domain's trusted time domain."
        elif recordedAt.Milliseconds < world.LastLogicalTime then
            Error "Genesis time cannot move backwards."
        else
            let context = GenesisContext(world.Scope)
            let actorsBefore = world.Actors |> Map.keys |> Set.ofSeq
            let capabilitiesBefore = world.Capabilities |> Map.keys |> Set.ofSeq

            try
                let value, initialized = initialize context world
                context.Complete()

                if initialized.Scope <> world.Scope then
                    invalidOp "Genesis returned a World from another authority domain."

                let introducedActors =
                    initialized.Actors
                    |> Map.keys
                    |> Set.ofSeq
                    |> fun issued -> Set.difference issued actorsBefore
                    |> Set.toList

                let introducedCapabilities =
                    initialized.Capabilities
                    |> Map.keys
                    |> Set.ofSeq
                    |> fun issued -> Set.difference issued capabilitiesBefore
                    |> Set.toList

                let occurrence = OccurrenceReference.issue initialized.Scope initialized.NextReference

                let genesisRecord =
                    { Occurrence = occurrence
                      Policy = policy
                      IntroducedActors = introducedActors
                      IntroducedCapabilities = introducedCapabilities
                      RecordedAt = recordedAt }

                Ok(
                    value,
                    { initialized with
                        NextReference = initialized.NextReference + 1L
                        GenesisOccurrences = initialized.GenesisOccurrences @ [ genesisRecord ]
                        LastLogicalTime = recordedAt.Milliseconds }
                )
            finally
                context.Complete()

    let registerShape (definition: ShapeDefinition) (world: World) =
        if definition.Reference.Version < 1 then
            Error "Shape versions start at one."
        elif Map.containsKey definition.Reference world.Shapes then
            Error "That shape version is already registered."
        else
            let previousVersions =
                world.Shapes
                |> Map.toSeq
                |> Seq.map fst
                |> Seq.filter (fun reference -> reference.Name = definition.Reference.Name)
                |> Seq.map _.Version
                |> Seq.toList

            let previousDefinition =
                world.Shapes
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.filter (fun candidate -> candidate.Reference.Name = definition.Reference.Name)
                |> Seq.sortByDescending _.Reference.Version
                |> Seq.tryHead

            let preservesLineage previous current =
                match previous.Body, current.Body with
                | UnitShape, UnitShape -> true
                | ScalarShape previousKind, ScalarShape currentKind -> previousKind = currentKind
                | SequenceShape previousElement, SequenceShape currentElement -> previousElement = currentElement
                | ChoiceShape previousCases, ChoiceShape currentCases -> previousCases = currentCases
                | OpaqueShape previousMediaType, OpaqueShape currentMediaType -> previousMediaType = currentMediaType
                | RecordShape previousFields, RecordShape currentFields ->
                    previous.IsOpenToFragments = current.IsOpenToFragments
                    && Set.isSubset previous.AcceptedFragments current.AcceptedFragments
                    && (previousFields
                        |> List.forall (fun field ->
                            currentFields
                            |> List.exists (fun candidate ->
                                candidate.Name = field.Name
                                && candidate.Shape = field.Shape
                                && candidate.Required = field.Required)))
                    && (currentFields
                        |> List.filter (fun field ->
                            previousFields |> List.exists (fun candidate -> candidate.Name = field.Name) |> not)
                        |> List.forall (fun field -> not field.Required))
                | _ -> false

            if previousVersions |> List.exists (fun version -> version >= definition.Reference.Version) then
                Error "Shape versions must be registered additively."
            elif previousDefinition |> Option.exists (fun previous -> not (preservesLineage previous definition)) then
                Error "A later Shape version must preserve its lineage and may add only optional structure."
            else
                Ok
                    { world with
                        Shapes = Map.add definition.Reference definition world.Shapes }

    let registerFragment (definition: FragmentDefinition) (world: World) =
        if Map.containsKey definition.Reference world.Fragments then
            Error "That fragment version is already registered."
        elif not (Map.containsKey definition.HostShape world.Shapes) then
            Error "The fragment host Shape is not registered."
        elif not (Map.containsKey definition.Shape world.Shapes) then
            Error "The fragment shape is not registered."
        else
            Ok
                { world with
                    Fragments = Map.add definition.Reference definition world.Fragments }

    let registerConstraint
        (name: CanonicalName)
        (parameterShape: ShapeReference)
        (description: string)
        (world: World)
        =
        if not (Map.containsKey parameterShape world.Shapes) then
            Error "The constraint parameter shape is not registered."
        else
            let reference = ConstraintReference.issue world.Scope world.NextReference

            let definition =
                { Reference = reference
                  Name = name
                  ParameterShape = parameterShape
                  Description = description }

            Ok(
                definition,
                { world with
                    NextReference = world.NextReference + 1L
                    Constraints = Map.add reference definition world.Constraints }
            )

    let registerOperation (definition: OperationDefinition) (world: World) =
        if Map.containsKey definition.Reference world.Operations then
            Error "That operation is already registered."
        elif not (Map.containsKey definition.Target world.Actors) then
            Error "The operation target is not an issued Actor in this domain."
        elif not (Map.containsKey definition.CommandShape world.Shapes) then
            Error "The command shape is not registered."
        elif not (Map.containsKey definition.ResultShape world.Shapes) then
            Error "The result shape is not registered."
        elif
            definition.Constraints
            |> List.exists (fun requirement -> not (Map.containsKey requirement.Constraint world.Constraints))
        then
            Error "An operation constraint is not registered."
        else
            Ok
                { world with
                    Operations = Map.add definition.Reference definition world.Operations }

    let registerEvent (definition: EventDefinition) (world: World) =
        if Map.containsKey definition.Reference world.EventDefinitions then
            Error "That Event is already registered."
        elif not (Map.containsKey definition.AssertionShape world.Shapes) then
            Error "The Event assertion Shape is not registered."
        else
            Ok
                { world with
                    EventDefinitions = Map.add definition.Reference definition world.EventDefinitions }

    let private validateScalar kind value =
        match kind, value with
        | Boolean, BooleanValue _
        | Integer, IntegerValue _
        | Decimal, DecimalValue _
        | Text, TextValue _
        | Bytes, BytesValue _ -> Ok()
        | _ -> Error "The scalar value has the wrong kind."

    let rec validateValue (reference: ShapeReference) (value: ShapeValue) (world: World) =
        match Map.tryFind reference world.Shapes with
        | None -> Error "The shape is unknown."
        | Some definition ->
            match definition.Body, value with
            | UnitShape, UnitValue -> Ok()
            | ScalarShape kind, scalar -> validateScalar kind scalar
            | SequenceShape element, SequenceValue values ->
                values
                |> List.map (fun item -> validateValue element item world)
                |> List.tryFind Result.isError
                |> Option.defaultValue (Ok())
            | ChoiceShape cases, ChoiceValue(caseName, caseValue) ->
                match Map.tryFind caseName cases with
                | None -> Error "The choice case is not declared."
                | Some caseShape -> validateValue caseShape caseValue world
            | OpaqueShape _, BytesValue _ -> Ok()
            | RecordShape declaredFields, RecordValue(fields, fragments) ->
                let declaredNames = declaredFields |> Seq.map _.Name |> Set.ofSeq

                let missingRequired =
                    declaredFields
                    |> List.tryFind (fun field -> field.Required && not (Map.containsKey field.Name fields))

                let unknownField =
                    fields
                    |> Map.toSeq
                    |> Seq.map fst
                    |> Seq.tryFind (fun name -> not (Set.contains name declaredNames))

                let invalidField =
                    declaredFields
                    |> List.choose (fun field ->
                        Map.tryFind field.Name fields
                        |> Option.map (fun fieldValue -> validateValue field.Shape fieldValue world))
                    |> List.tryFind Result.isError

                let invalidFragment =
                    fragments
                    |> Map.toList
                    |> List.tryPick (fun (fragmentReference, fragmentValue) ->
                        match Map.tryFind fragmentReference world.Fragments with
                        | None -> Some(Error "The fragment is not registered.")
                        | Some fragment ->
                            let explicitlyIncluded =
                                Set.contains fragmentReference definition.AcceptedFragments

                            let compatibleAuthoredAttachment =
                                definition.IsOpenToFragments
                                && fragment.HostShape.Name = definition.Reference.Name
                                && fragment.HostShape.Version <= definition.Reference.Version

                            if not explicitlyIncluded && not compatibleAuthoredAttachment then
                                Some(Error "The fragment is not declared for that host Shape lineage.")
                            else
                                match validateValue fragment.Shape fragmentValue world with
                                | Ok() -> None
                                | Error message -> Some(Error message))

                match missingRequired, unknownField, invalidField, invalidFragment with
                | Some _, _, _, _ -> Error "A required record field is missing."
                | _, Some _, _, _ -> Error "The record contains an undeclared field."
                | _, _, Some error, _ -> error
                | _, _, _, Some error -> error
                | _ -> Ok()
            | _ -> Error "The value does not match the declared shape."

    let private validateConstraintRequirements (requirements: ConstraintRequirement list) (world: World) =
        requirements
        |> List.tryPick (fun requirement ->
            match Map.tryFind requirement.Constraint world.Constraints with
            | None -> Some "A Capability refers to an unknown Constraint."
            | Some definition ->
                match validateValue definition.ParameterShape requirement.Parameters world with
                | Ok() -> None
                | Error message -> Some("A Capability Constraint value is invalid: " + message))
        |> Option.map Error
        |> Option.defaultValue (Ok())

    let rec private validateConstraintExpression (expression: ConstraintExpression) (world: World) =
        match expression with
        | AtomicConstraint requirement -> validateConstraintRequirements [ requirement ] world
        | AllOf []
        | AnyOf [] -> Error "A composite Constraint group must contain at least one operand."
        | AllOf operands
        | AnyOf operands ->
            operands
            |> List.tryPick (fun operand ->
                match validateConstraintExpression operand world with
                | Ok() -> None
                | Error message -> Some(Error message))
            |> Option.defaultValue (Ok())
        | Not operand -> validateConstraintExpression operand world

    let private validateConstraintExpressions expressions world =
        expressions
        |> List.tryPick (fun expression ->
            match validateConstraintExpression expression world with
            | Ok() -> None
            | Error message -> Some(Error message))
        |> Option.defaultValue (Ok())

    let internal issueGenesisActor (context: GenesisContext) (name: CanonicalName) (world: World) =
        context.EnsureActive world.Scope
        let reference = ActorReference.issue world.Scope world.NextReference
        let actor = { Reference = reference; Name = name }

        actor,
        { world with
            NextReference = world.NextReference + 1L
            Actors = Map.add reference actor world.Actors }

    let internal issuePrimordialCapabilityWithExpressions
        (context: GenesisContext)
        (name: CanonicalName)
        (holder: ActorReference)
        (target: ActorReference)
        (operations: Set<OperationReference>)
        (expressions: ConstraintExpression list)
        (delegationAllowed: bool)
        (world: World)
        =
        context.EnsureActive world.Scope

        let operationsRecognized =
            operations
            |> Seq.forall (fun operation ->
                match Map.tryFind operation world.Operations with
                | Some definition -> definition.Target = target
                | None -> false)

        if not (Map.containsKey holder world.Actors) then
            Error "The Capability holder is not an issued Actor in this domain."
        elif not (Map.containsKey target world.Actors) then
            Error "The Capability target is not an issued Actor in this domain."
        elif Set.isEmpty operations then
            Error "A Capability must authorize at least one Operation."
        elif not operationsRecognized then
            Error "A Capability Operation is unknown or belongs to another target."
        else
            match validateConstraintExpressions expressions world with
            | Error message -> Error message
            | Ok() ->
                let reference = CapabilityReference.issue world.Scope world.NextReference

                let atomicCompatibility =
                    expressions
                    |> List.choose (function
                        | AtomicConstraint requirement -> Some requirement
                        | _ -> None)

                let capability =
                    { Reference = reference
                      Name = name
                      Holder = holder
                      Target = target
                      Operations = operations
                      AddedConstraints = atomicCompatibility
                      Parent = None
                      IssuedBy = None
                      DelegationAllowed = delegationAllowed }

                Ok(
                    capability,
                    { world with
                        NextReference = world.NextReference + 1L
                        Capabilities = Map.add reference capability world.Capabilities
                        CapabilityConstraintExpressions =
                            Map.add reference expressions world.CapabilityConstraintExpressions }
                )

    let internal issuePrimordialCapability
        (context: GenesisContext)
        (name: CanonicalName)
        (holder: ActorReference)
        (target: ActorReference)
        (operations: Set<OperationReference>)
        (constraints: ConstraintRequirement list)
        (delegationAllowed: bool)
        (world: World)
        =
        issuePrimordialCapabilityWithExpressions
            context
            name
            holder
            target
            operations
            (constraints |> List.map AtomicConstraint)
            delegationAllowed
            world

    let delegateCapabilityWithExpressions
        (name: CanonicalName)
        (delegator: ActorReference)
        (newHolder: ActorReference)
        (parentReference: CapabilityReference)
        (addedExpressions: ConstraintExpression list)
        (world: World)
        =
        match Map.tryFind parentReference world.Capabilities with
        | None -> Error "The parent Capability is unknown."
        | Some parent when parent.Holder <> delegator ->
            Error "Only the Capability holder may delegate it."
        | Some parent when not parent.DelegationAllowed ->
            Error "The parent Capability does not permit further Delegation."
        | Some _ when not (Map.containsKey newHolder world.Actors) ->
            Error "The delegated Capability holder is unknown."
        | Some parent ->
            match validateConstraintExpressions addedExpressions world with
            | Error message -> Error message
            | Ok() ->
                let reference = CapabilityReference.issue world.Scope world.NextReference

                let atomicCompatibility =
                    addedExpressions
                    |> List.choose (function
                        | AtomicConstraint requirement -> Some requirement
                        | _ -> None)

                let capability =
                    { Reference = reference
                      Name = name
                      Holder = newHolder
                      Target = parent.Target
                      Operations = parent.Operations
                      AddedConstraints = atomicCompatibility
                      Parent = Some parent.Reference
                      IssuedBy = Some delegator
                      DelegationAllowed = parent.DelegationAllowed }

                Ok(
                    capability,
                    { world with
                        NextReference = world.NextReference + 1L
                        Capabilities = Map.add reference capability world.Capabilities
                        CapabilityConstraintExpressions =
                            Map.add reference addedExpressions world.CapabilityConstraintExpressions }
                )

    let delegateCapability
        (name: CanonicalName)
        (delegator: ActorReference)
        (newHolder: ActorReference)
        (parentReference: CapabilityReference)
        (addedConstraints: ConstraintRequirement list)
        (world: World)
        =
        delegateCapabilityWithExpressions
            name
            delegator
            newHolder
            parentReference
            (addedConstraints |> List.map AtomicConstraint)
            world

    let validateContract
        (target: ShapeReference)
        (requiredFragments: Set<FragmentReference>)
        (value: ShapeValue)
        (world: World)
        =
        match validateValue target value world, value with
        | Error message, _ -> Error message
        | Ok(), RecordValue(_, fragments) ->
            let missing =
                requiredFragments
                |> Seq.tryFind (fun reference -> not (Map.containsKey reference fragments))

            match missing with
            | Some reference ->
                Error
                    $"The required fragment {CanonicalName.value reference.Name}@{reference.Version} is missing."
            | None -> Ok()
        | Ok(), _ when Set.isEmpty requiredFragments -> Ok()
        | Ok(), _ -> Error "Only record Shapes can require authored fragments."

    let projectRecordWithFragments
        (target: ShapeReference)
        (requiredFragments: Set<FragmentReference>)
        (value: ShapeValue)
        (world: World)
        =
        match Map.tryFind target world.Shapes, value with
        | Some definition, RecordValue(fields, fragments) ->
            match definition.Body with
            | RecordShape declaredFields ->
                let projectedFields =
                    declaredFields
                    |> List.choose (fun field ->
                        Map.tryFind field.Name fields |> Option.map (fun value -> field.Name, value))
                    |> Map.ofList

                let projectedFragments =
                    fragments
                    |> Map.filter (fun reference _ ->
                        Set.contains reference definition.AcceptedFragments
                        || Set.contains reference requiredFragments)

                let projected = RecordValue(projectedFields, projectedFragments)

                validateContract target requiredFragments projected world
                |> Result.map (fun () -> projected)
            | _ -> Error "The projection target is not a record shape."
        | None, _ -> Error "The projection target is unknown."
        | _, _ -> Error "Only record values can be projected."

    let projectRecord (target: ShapeReference) (value: ShapeValue) (world: World) =
        projectRecordWithFragments target Set.empty value world

    let private allocateExecution (world: World) =
        ExecutionReference.issue world.Scope world.NextReference,
        { world with NextReference = world.NextReference + 1L }

    let private allocateOccurrence (world: World) =
        OccurrenceReference.issue world.Scope world.NextReference,
        { world with NextReference = world.NextReference + 1L }

    let private observableTime (environment: Environment) (world: World) =
        if
            environment.TrustedTime.TimeDomain = world.TimeDomain
            && environment.TrustedTime.Milliseconds >= world.LastLogicalTime
            && environment.TrustedTime.UncertaintyMilliseconds |> Option.forall (fun value -> value >= 0L)
        then
            environment.TrustedTime
        else
            { Milliseconds = if world.LastLogicalTime = Int64.MinValue then 0L else world.LastLogicalTime
              TimeDomain = world.TimeDomain
              UncertaintyMilliseconds = None }

    let private recordExecution
        (request: ExecutionRequest)
        (status: ExecutionStatus)
        (reason: string option)
        (recordedAt: TemporalMark)
        (execution: ExecutionReference)
        (world: World)
        =
        let audit =
            { Execution = execution
              Initiator = request.Initiator
              Target = request.Target
              PresentedCapability = request.PresentedCapability
              Operation = request.Operation
              Status = status
              Reason = reason
              Occurrence = request.Occurrence
              RecordedAt = recordedAt }

        { world with
            Executions = world.Executions @ [ audit ]
            LastLogicalTime = max world.LastLogicalTime recordedAt.Milliseconds }

    let private finishWithoutEffects
        (environment: Environment)
        (status: ExecutionStatus)
        (request: ExecutionRequest)
        (reason: string)
        (detailsShape: ShapeReference option)
        (details: ShapeValue option)
        (world: World)
        =
        let execution, nextWorld = allocateExecution world
        let occurrence, afterOccurrence = allocateOccurrence nextWorld
        let recordedAt = observableTime environment world
        let emitter =
            if Map.containsKey request.Target world.Actors then request.Target else world.AuthorityActor

        let outcomeEvent =
            { Reference = BuiltIn.executionOutcomeEvent
              Occurrence = occurrence
              Emitter = emitter
              CausedBy = execution
              Payload = details |> Option.defaultValue UnitValue
              EmittedAt = recordedAt
              OccurredAt = None }

        let recordedWorld =
            recordExecution request status (Some reason) recordedAt execution afterOccurrence

        let nextWorld = { recordedWorld with Events = recordedWorld.Events @ [ outcomeEvent ] }

        { World = nextWorld
          Outcome =
            { Event = outcomeEvent
              Execution = execution
              TerminalFor = execution
              Operation = request.Operation
              Status = status
              Result = None
              DetailsShape = detailsShape
              Details = details
              Reason = Some reason
              EmittedAt = recordedAt }
          EmittedEvents = [ outcomeEvent ]
          Provenance = [] }

    let private deny environment request reason world =
        finishWithoutEffects environment Denied request reason None None world

    let private fail environment request (failure: OperationFailure) world =
        match failure.DetailsShape, failure.Details with
        | None, None ->
            finishWithoutEffects environment Failed request failure.Reason None None world
        | Some shape, Some details ->
            match validateValue shape details world with
            | Ok() ->
                finishWithoutEffects
                    environment
                    Failed
                    request
                    failure.Reason
                    (Some shape)
                    (Some details)
                    world
            | Error message ->
                finishWithoutEffects
                    environment
                    Failed
                    request
                    ("The handler returned invalid failure details: " + message)
                    None
                    None
                    world
        | _ ->
            finishWithoutEffects
                environment
                Failed
                request
                "The handler must provide both a failure-details Shape and value."
                None
                None
                world

    let private capabilityChain (capability: Capability) (world: World) =
        let rec collect current accumulated =
            match current.Parent with
            | None -> current :: accumulated
            | Some parentReference ->
                match Map.tryFind parentReference world.Capabilities with
                | Some parent -> collect parent (current :: accumulated)
                | None -> invalidOp "A Capability derivation chain is internally incomplete."

        collect capability []

    let private validateEventDraft (draft: EventDraft) (world: World) =
        if not (Map.containsKey draft.Emitter world.Actors) then
            Error "An emitted Event names an unknown emitter."
        else
            match Map.tryFind draft.Reference world.EventDefinitions with
            | None -> Error "An emitted Event is not registered."
            | Some definition ->
                match validateValue definition.AssertionShape draft.Payload world with
                | Error message -> Error("An emitted Event assertion is invalid: " + message)
                | Ok() ->
                    match draft.OccurredAt |> Option.bind _.UncertaintyMilliseconds with
                    | Some uncertainty when uncertainty < 0L ->
                        Error "An Event Temporal Mark cannot have negative uncertainty."
                    | _ -> Ok()

    let step (environment: Environment) (world: World) (request: ExecutionRequest) =
        match Map.tryFind request.Operation world.Operations with
        | _ when environment.TrustedTime.TimeDomain <> world.TimeDomain ->
            deny environment request "The target has no trusted clock for the supplied time domain." world
        | _ when environment.TrustedTime.Milliseconds < world.LastLogicalTime ->
            deny environment request "Trusted logical time cannot move backwards." world
        | _ when environment.TrustedTime.UncertaintyMilliseconds |> Option.exists (fun value -> value < 0L) ->
            deny environment request "Trusted time uncertainty cannot be negative." world
        | None -> deny environment request "The requested Operation is unknown." world
        | Some operation when not (Map.containsKey request.Initiator world.Actors) ->
            deny environment request "The initiating Actor is unknown." world
        | Some operation when not (Map.containsKey request.Target world.Actors) ->
            deny environment request "The target Actor is unknown." world
        | Some operation when operation.Target <> request.Target ->
            deny environment request "The Operation is not recognized by the requested target." world
        | Some operation when not (Map.containsKey request.PresentedCapability world.Capabilities) ->
            deny environment request "The presented Capability was not issued by this authority domain." world
        | Some operation ->
            let capability = world.Capabilities[request.PresentedCapability]

            if capability.Holder <> request.Initiator then
                deny environment request "The presented Capability does not designate the initiating Actor." world
            elif capability.Target <> request.Target then
                deny environment request "The presented Capability does not designate the requested target." world
            elif not (Set.contains request.Operation capability.Operations) then
                deny environment request "The presented Capability does not authorize the requested Operation." world
            else
                match validateValue operation.CommandShape request.Command world with
                | Error message -> deny environment request message world
                | Ok() ->
                    let context =
                        { Request = request
                          Operation = operation
                          LogicalTime = environment.TrustedTime.Milliseconds }

                    let effectiveConstraints =
                        (operation.Constraints |> List.map AtomicConstraint)
                        @ (capabilityChain capability world
                           |> List.collect (fun chainCapability ->
                               world.CapabilityConstraintExpressions
                               |> Map.tryFind chainCapability.Reference
                               |> Option.defaultValue (
                                   chainCapability.AddedConstraints |> List.map AtomicConstraint
                               )))

                    let evaluateAtom requirement =
                        match Map.tryFind requirement.Constraint world.Constraints with
                        | None -> ConstraintAtomEvaluation.evaluatorFailed
                        | Some definition ->
                            match validateValue definition.ParameterShape requirement.Parameters world with
                            | Error _ -> ConstraintAtomEvaluation.invalidValue
                            | Ok() ->
                                match Map.tryFind requirement.Constraint environment.ConstraintEvaluators with
                                | None -> ConstraintAtomEvaluation.unsupported definition.Name
                                | Some evaluator ->
                                    try
                                        match evaluator requirement.Parameters context with
                                        | Ok() -> ConstraintAtomEvaluation.satisfied
                                        | Error message -> ConstraintAtomEvaluation.unsatisfied message
                                    with _ ->
                                        ConstraintAtomEvaluation.evaluatorFailed

                    let evaluations =
                        effectiveConstraints
                        |> List.map (ConstraintExpression.evaluate evaluateAtom)

                    let constraintFailure =
                        evaluations
                        |> List.tryFind (fun evaluation -> evaluation.Outcome = Indeterminate)
                        |> Option.orElseWith (fun () ->
                            evaluations
                            |> List.tryFind (fun evaluation -> evaluation.Outcome = Unsatisfied))
                        |> Option.map _.Reason

                    match constraintFailure with
                    | Some message -> deny environment request message world
                    | None ->
                        match Map.tryFind operation.Reference environment.Handlers with
                        | None -> deny environment request "The Operation has no pure handler." world
                        | Some handler ->
                            match handler request with
                            | Error failure -> fail environment request failure world
                            | Ok(result, eventDrafts, claimDrafts) ->
                                match validateValue operation.ResultShape result world with
                                | Error message ->
                                    fail
                                        environment
                                        request
                                        (OperationFailure.withoutDetails
                                            ("The handler returned an invalid result: " + message))
                                        world
                                | Ok() ->
                                    match
                                        eventDrafts
                                        |> List.tryPick (fun draft ->
                                            match validateEventDraft draft world with
                                            | Ok() -> None
                                            | Error message -> Some message)
                                    with
                                    | Some message ->
                                        fail environment request (OperationFailure.withoutDetails message) world
                                    | None ->
                                        let execution, afterExecution = allocateExecution world

                                        let events, afterEvents =
                                            eventDrafts
                                            |> List.fold
                                                (fun (events, state) draft ->
                                                    let occurrence =
                                                        OccurrenceReference.issue state.Scope state.NextReference

                                                    let event =
                                                        { Reference = draft.Reference
                                                          Occurrence = occurrence
                                                          Emitter = draft.Emitter
                                                          CausedBy = execution
                                                          Payload = draft.Payload
                                                          EmittedAt = environment.TrustedTime
                                                          OccurredAt = draft.OccurredAt }

                                                    event :: events,
                                                    { state with NextReference = state.NextReference + 1L })
                                                ([], afterExecution)

                                        let emittedEvents = List.rev events
                                        let outcomeOccurrence, afterOutcome = allocateOccurrence afterEvents

                                        let outcomeEvent =
                                            { Reference = BuiltIn.executionOutcomeEvent
                                              Occurrence = outcomeOccurrence
                                              Emitter = request.Target
                                              CausedBy = execution
                                              Payload = result
                                              EmittedAt = environment.TrustedTime
                                              OccurredAt = None }

                                        let claims =
                                            claimDrafts
                                            |> List.map (fun (predicate, objectValue) ->
                                                { Subject = string (ExecutionReference.value execution)
                                                  Predicate = predicate
                                                  Object = objectValue
                                                  CausedBy = execution })

                                        let recordedWorld =
                                            recordExecution
                                                request
                                                Succeeded
                                                None
                                                environment.TrustedTime
                                                execution
                                                afterOutcome

                                        let nextWorld =
                                            { recordedWorld with
                                                Events = recordedWorld.Events @ emittedEvents @ [ outcomeEvent ]
                                                Provenance = recordedWorld.Provenance @ claims }

                                        { World = nextWorld
                                          Outcome =
                                            { Event = outcomeEvent
                                              Execution = execution
                                              TerminalFor = execution
                                              Operation = operation.Reference
                                              Status = Succeeded
                                              Result = Some result
                                              DetailsShape = None
                                              Details = None
                                              Reason = None
                                              EmittedAt = environment.TrustedTime }
                                          EmittedEvents = emittedEvents @ [ outcomeEvent ]
                                          Provenance = claims }

[<RequireQualifiedAccess>]
module Genesis =
    let actor context name world = World.issueGenesisActor context name world

    let capability context name holder target operations constraints delegationAllowed world =
        World.issuePrimordialCapability
            context
            name
            holder
            target
            operations
            constraints
            delegationAllowed
            world

    let capabilityWithExpressions context name holder target operations expressions delegationAllowed world =
        World.issuePrimordialCapabilityWithExpressions
            context
            name
            holder
            target
            operations
            expressions
            delegationAllowed
            world
