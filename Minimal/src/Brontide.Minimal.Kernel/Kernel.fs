namespace Brontide.Minimal.Kernel

open System
open Brontide.Minimal.Model

type World =
    private
        { Scope: Guid
          NextReference: int64
          Actors: Map<ActorReference, Actor>
          Capabilities: Map<CapabilityReference, Capability>
          Grants: Map<ActorReference, Set<CapabilityReference>>
          Shapes: Map<ShapeReference, ShapeDefinition>
          Fragments: Map<FragmentReference, FragmentDefinition>
          Constraints: Map<ConstraintReference, ConstraintDefinition>
          Operations: Map<OperationReference, OperationDefinition>
          Events: Event list
          Provenance: ProvenanceClaim list }

type ConstraintContext =
    { Request: ExecutionRequest
      Operation: OperationDefinition
      LogicalTime: int64 }

type ConstraintEvaluator = ShapeValue -> ConstraintContext -> Result<unit, string>
type OperationHandler = ExecutionRequest -> Result<ShapeValue * EventDraft list * (CanonicalName * string) list, string>

type Environment =
    { LogicalTime: int64
      ConstraintEvaluators: Map<ConstraintReference, ConstraintEvaluator>
      Handlers: Map<OperationReference, OperationHandler> }

type StepResult =
    { World: World
      Outcome: ExecutionOutcome
      EmittedEvents: Event list
      Provenance: ProvenanceClaim list }

[<RequireQualifiedAccess>]
module World =
    let private builtInShape (reference: ShapeReference) (body: ShapeBody) : ShapeDefinition =
        { Reference = reference
          Description = "Brontide.Minimal Base shape"
          Body = body
          AcceptedFragments = Set.empty
          IsOpenToFragments = false }

    let create (scope: Guid) : World =
        let shapes =
            [ builtInShape BuiltIn.unitShape UnitShape
              builtInShape BuiltIn.booleanShape (ScalarShape Boolean)
              builtInShape BuiltIn.integerShape (ScalarShape Integer)
              builtInShape BuiltIn.decimalShape (ScalarShape Decimal)
              builtInShape BuiltIn.textShape (ScalarShape Text)
              builtInShape BuiltIn.bytesShape (ScalarShape Bytes) ]
            |> Seq.map (fun definition -> definition.Reference, definition)
            |> Map.ofSeq

        { Scope = scope
          NextReference = 1L
          Actors = Map.empty
          Capabilities = Map.empty
          Grants = Map.empty
          Shapes = shapes
          Fragments = Map.empty
          Constraints = Map.empty
          Operations = Map.empty
          Events = []
          Provenance = [] }

    let scope (world: World) = world.Scope
    let actors (world: World) = world.Actors |> Map.toSeq |> Seq.map snd |> Seq.toList
    let shapes (world: World) = world.Shapes |> Map.toSeq |> Seq.map snd |> Seq.toList
    let operations (world: World) = world.Operations |> Map.toSeq |> Seq.map snd |> Seq.toList
    let events (world: World) = world.Events
    let provenance (world: World) = world.Provenance

    let tryFindShape (reference: ShapeReference) (world: World) = Map.tryFind reference world.Shapes
    let tryFindOperation (reference: OperationReference) (world: World) = Map.tryFind reference world.Operations

    let issueActor (name: CanonicalName) (world: World) =
        let reference: ActorReference =
            { Scope = world.Scope
              Value = world.NextReference }

        let actor = { Reference = reference; Name = name }

        actor,
        { world with
            NextReference = world.NextReference + 1L
            Actors = Map.add reference actor world.Actors }

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

            if
                previousVersions
                |> List.exists (fun version -> version >= definition.Reference.Version)
            then
                Error "Shape versions must be registered additively."
            else
                Ok
                    { world with
                        Shapes = Map.add definition.Reference definition world.Shapes }

    let registerFragment (definition: FragmentDefinition) (world: World) =
        if Map.containsKey definition.Reference world.Fragments then
            Error "That fragment version is already registered."
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
            let reference: ConstraintReference =
                { Scope = world.Scope
                  Value = world.NextReference }

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
            Error "That operation version is already registered."
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

    let createCapability
        (name: CanonicalName)
        (operations: Set<OperationReference>)
        (parent: CapabilityReference option)
        (world: World)
        =
        let missingOperation =
            operations
            |> Seq.tryFind (fun operation -> not (Map.containsKey operation world.Operations))

        match missingOperation, parent with
        | Some _, _ -> Error "A capability refers to an unknown operation."
        | None, Some parentReference when not (Map.containsKey parentReference world.Capabilities) ->
            Error "The parent capability is unknown."
        | None, Some parentReference ->
            let parentCapability = world.Capabilities[parentReference]

            if not (Set.isSubset operations parentCapability.Operations) then
                Error "A delegated capability cannot broaden its parent."
            else
                let reference: CapabilityReference =
                    { Scope = world.Scope
                      Value = world.NextReference }

                let capability =
                    { Reference = reference
                      Name = name
                      Operations = operations
                      Parent = parent }

                Ok(
                    capability,
                    { world with
                        NextReference = world.NextReference + 1L
                        Capabilities = Map.add reference capability world.Capabilities }
                )
        | None, None ->
            let reference: CapabilityReference =
                { Scope = world.Scope
                  Value = world.NextReference }

            let capability =
                { Reference = reference
                  Name = name
                  Operations = operations
                  Parent = None }

            Ok(
                capability,
                { world with
                    NextReference = world.NextReference + 1L
                    Capabilities = Map.add reference capability world.Capabilities }
            )

    let grant (actor: ActorReference) (capability: CapabilityReference) (world: World) =
        if not (Map.containsKey actor world.Actors) then
            Error "The actor is unknown."
        elif not (Map.containsKey capability world.Capabilities) then
            Error "The capability is unknown."
        else
            let grants = Map.tryFind actor world.Grants |> Option.defaultValue Set.empty

            Ok
                { world with
                    Grants = Map.add actor (Set.add capability grants) world.Grants }

    let isAuthorized (actor: ActorReference) (operation: OperationReference) (world: World) =
        Map.tryFind actor world.Grants
        |> Option.defaultValue Set.empty
        |> Seq.choose (fun reference -> Map.tryFind reference world.Capabilities)
        |> Seq.exists (fun capability -> Set.contains operation capability.Operations)

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
                        let accepted =
                            definition.IsOpenToFragments
                            || Set.contains fragmentReference definition.AcceptedFragments

                        if not accepted then
                            Some(Error "The record does not accept that fragment.")
                        else
                            match Map.tryFind fragmentReference world.Fragments with
                            | None -> Some(Error "The fragment is not registered.")
                            | Some fragment ->
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
        ({ Scope = world.Scope
           Value = world.NextReference }: ExecutionReference),
        { world with NextReference = world.NextReference + 1L }

    let private deny (request: ExecutionRequest) (reason: string) (world: World) =
        let execution, nextWorld = allocateExecution world

        { World = nextWorld
          Outcome =
            { Execution = execution
              Operation = request.Operation
              Status = Denied
              Result = None
              Reason = Some reason }
          EmittedEvents = []
          Provenance = [] }

    let step (environment: Environment) (world: World) (request: ExecutionRequest) =
        match Map.tryFind request.Operation world.Operations with
        | None -> deny request "The requested operation is unknown." world
        | Some operation when not (Map.containsKey request.Actor world.Actors) ->
            deny request "The requesting actor is unknown." world
        | Some _ when not (isAuthorized request.Actor request.Operation world) ->
            deny request "The actor has no capability for this operation." world
        | Some operation ->
            match validateValue operation.CommandShape request.Command world with
            | Error message -> deny request message world
            | Ok() ->
                let context =
                    { Request = request
                      Operation = operation
                      LogicalTime = environment.LogicalTime }

                let constraintFailure =
                    operation.Constraints
                    |> List.tryPick (fun requirement ->
                        match Map.tryFind requirement.Constraint environment.ConstraintEvaluators with
                        | None -> Some "A required constraint has no evaluator."
                        | Some evaluator ->
                            match evaluator requirement.Parameters context with
                            | Ok() -> None
                            | Error message -> Some message)

                match constraintFailure with
                | Some message -> deny request message world
                | None ->
                    match Map.tryFind operation.Reference environment.Handlers with
                    | None -> deny request "The operation has no pure handler." world
                    | Some handler ->
                        match handler request with
                        | Error message ->
                            let execution, nextWorld = allocateExecution world

                            { World = nextWorld
                              Outcome =
                                { Execution = execution
                                  Operation = operation.Reference
                                  Status = Failed
                                  Result = None
                                  Reason = Some message }
                              EmittedEvents = []
                              Provenance = [] }
                        | Ok(result, eventDrafts, claimDrafts) ->
                            match validateValue operation.ResultShape result world with
                            | Error message -> deny request ("The handler returned an invalid result: " + message) world
                            | Ok() ->
                                let execution, afterExecution = allocateExecution world

                                let events, afterEvents =
                                    eventDrafts
                                    |> List.fold
                                        (fun (events, state) draft ->
                                            let occurrence: OccurrenceReference =
                                                { Scope = state.Scope
                                                  Value = state.NextReference }

                                            let event =
                                                { Reference = draft.Reference
                                                  Occurrence = occurrence
                                                  CausedBy = execution
                                                  Payload = draft.Payload }

                                            event :: events,
                                            { state with NextReference = state.NextReference + 1L })
                                        ([], afterExecution)

                                let emittedEvents = List.rev events

                                let claims =
                                    claimDrafts
                                    |> List.map (fun (predicate, objectValue) ->
                                        { Subject = string execution.Value
                                          Predicate = predicate
                                          Object = objectValue
                                          CausedBy = execution })

                                let nextWorld =
                                    { afterEvents with
                                        Events = afterEvents.Events @ emittedEvents
                                        Provenance = afterEvents.Provenance @ claims }

                                { World = nextWorld
                                  Outcome =
                                    { Execution = execution
                                      Operation = operation.Reference
                                      Status = Succeeded
                                      Result = Some result
                                      Reason = None }
                                  EmittedEvents = emittedEvents
                                  Provenance = claims }
