namespace Brontide.Minimal.Kernel

open System
open System.Globalization
open System.Security.Cryptography
open System.Text
open Brontide.Minimal.Model

module private ReferenceIdentity =
    let private opaque ((scope, epoch, value): Guid * Guid * int64) =
        String.concat
            ":"
            [ scope.ToString("N")
              epoch.ToString("N")
              value.ToString(CultureInfo.InvariantCulture) ]

    let actor reference = reference |> ActorReference.identity |> opaque
    let capability reference = reference |> CapabilityReference.identity |> opaque
    let constraintReference reference = reference |> ConstraintReference.identity |> opaque
    let occurrence reference = reference |> OccurrenceReference.identity |> opaque

    let shapeReference (reference: ShapeReference) =
        $"{CanonicalName.value reference.Name}@{reference.Version.ToString(CultureInfo.InvariantCulture)}"

    let fragmentReference (reference: FragmentReference) =
        $"{CanonicalName.value reference.Name}@{reference.Version.ToString(CultureInfo.InvariantCulture)}"

    let rec shapeValueTokens value =
        match value with
        | UnitValue -> [ "unit" ]
        | BooleanValue value -> [ "boolean"; if value then "true" else "false" ]
        | IntegerValue value -> [ "integer"; value.ToString(CultureInfo.InvariantCulture) ]
        | DecimalValue value -> [ "decimal"; value.ToString(CultureInfo.InvariantCulture) ]
        | TextValue value -> [ "text"; value ]
        | BytesValue value -> [ "bytes"; Convert.ToHexString(value) ]
        | RecordValue(fields, fragments) ->
            [ yield "record"
              for KeyValue(name, fieldValue) in fields do
                  yield "field"
                  yield name
                  yield! shapeValueTokens fieldValue
              for KeyValue(reference, fragmentValue) in fragments do
                  yield "fragment"
                  yield fragmentReference reference
                  yield! shapeValueTokens fragmentValue ]
        | SequenceValue values ->
            [ yield "sequence"
              for item in values do
                  yield "item"
                  yield! shapeValueTokens item ]
        | ChoiceValue(caseName, choiceValue) ->
            [ yield "choice"
              yield caseName
              yield! shapeValueTokens choiceValue ]

    let constraintRequirementTokens requirement =
        [ yield constraintReference requirement.Constraint
          yield! shapeValueTokens requirement.Parameters ]

    let rec constraintExpressionTokens expression =
        match expression with
        | AtomicConstraint requirement ->
            [ yield "atomic"
              yield! constraintRequirementTokens requirement ]
        | AllOf expressions ->
            [ yield "all-of"
              for child in expressions do
                  yield "child"
                  yield! constraintExpressionTokens child ]
        | AnyOf expressions ->
            [ yield "any-of"
              for child in expressions do
                  yield "child"
                  yield! constraintExpressionTokens child ]
        | Not child ->
            [ yield "not"
              yield! constraintExpressionTokens child ]

    let capabilityAllocationTokens
        name
        holder
        target
        (operations: Set<OperationReference>)
        expressions
        parent
        issuedBy
        =
        [ yield CanonicalName.value name
          yield actor holder
          yield actor target
          for operation in operations do
              yield "operation"
              yield CanonicalName.value operation.Name
          for expression in expressions do
              yield "constraint-expression"
              yield! constraintExpressionTokens expression
          yield parent |> Option.map capability |> Option.defaultValue "root"
          yield issuedBy |> Option.map actor |> Option.defaultValue "primordial" ]

    let temporalMarkTokens mark =
        [ mark.Milliseconds.ToString(CultureInfo.InvariantCulture)
          mark.TimeDomain |> TimeDomainReference.name |> CanonicalName.value
          mark.UncertaintyMilliseconds
          |> Option.map (fun value -> value.ToString(CultureInfo.InvariantCulture))
          |> Option.defaultValue "none" ]

    let executionTokens (request: ExecutionRequest) recordedAt =
        [ yield actor request.Initiator
          yield actor request.Target
          yield capability request.PresentedCapability
          yield CanonicalName.value request.Operation.Name
          yield! shapeValueTokens request.Command
          yield request.Occurrence |> Option.map occurrence |> Option.defaultValue "none"
          for KeyValue(name, value) in request.Context do
              yield "context"
              yield name
              yield value
          yield! temporalMarkTokens recordedAt ]

    let derive (parent: Guid) (sequence: int64) kind parts =
        let components =
            [ yield parent.ToString("N")
              yield sequence.ToString(CultureInfo.InvariantCulture)
              yield kind
              yield! parts ]

        let encoded =
            components
            |> List.map (fun part -> $"{Encoding.UTF8.GetByteCount(part)}:{part}")
            |> String.concat "|"
            |> Encoding.UTF8.GetBytes

        encoded |> SHA256.HashData |> Array.take 16 |> Guid

type private AuthorityTransactionCoordinator() =
    let gate = obj ()
    let mutable activeGenesis: Guid option = None

    member _.AllowsMutation(transaction: Guid option) =
        lock gate (fun () ->
            match activeGenesis, transaction with
            | None, None -> true
            | Some active, Some branch -> active = branch
            | _ -> false)

    member _.RunGenesis(transaction: Guid, whenBusy: unit -> 'T, action: unit -> 'T) =
        lock gate (fun () ->
            match activeGenesis with
            | Some _ -> whenBusy ()
            | None ->
                activeGenesis <- Some transaction

                try
                    action ()
                finally
                    activeGenesis <- None)

    member _.RunRuntime(transaction: Guid option, whenBlocked: unit -> 'T, action: unit -> 'T) =
        lock gate (fun () ->
            if activeGenesis.IsSome || transaction.IsSome then
                whenBlocked ()
            else
                action ())

type private QuantifiedConstraintOccurrence =
    | QuantifiedConstraintOccurrence of CapabilityReference * expressionIndex: int * atomIndex: int * windowIndex: int64

type World =
    private
        { Scope: Guid
          AuthorityTransactions: AuthorityTransactionCoordinator
          ReferenceEpoch: Guid
          GenesisTransaction: Guid option
          NextReference: int64
          GenesisActive: bool
          AuthorityActor: ActorReference
          Actors: Map<ActorReference, Actor>
          RetiredActors: Set<ActorReference>
          Capabilities: Map<CapabilityReference, Capability>
          ExtinguishedCapabilities: Set<CapabilityReference>
          LivenessLeases: Map<LivenessLeaseReference, LivenessLease>
          QuantifiedUsage: Map<QuantifiedConstraintOccurrence, int64>
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
          TerminusOccurrences: TerminusOccurrence list
          TimeDomain: TimeDomainReference
          LastLogicalTime: int64 }

type ConstraintContext =
    { Request: ExecutionRequest
      Operation: OperationDefinition
      LogicalTime: int64
      RequestedOrigin: OriginClass
      ConstraintCapability: Capability }

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

type GenesisContext internal (scope: Guid, transaction: Guid, issuedAtMilliseconds: int64) =
    let allocationGate = obj ()
    let mutable active = true
    let mutable nextAllocation = 0L

    member internal _.EnsureActive(worldScope: Guid, worldTransaction: Guid option, genesisActive: bool) =
        lock allocationGate (fun () ->
            if not active then
                invalidOp "A completed Genesis context cannot introduce authority."

            if scope <> worldScope then
                invalidOp "A Genesis context cannot introduce authority into another domain."

            if worldTransaction <> Some transaction || not genesisActive then
                invalidOp "A Genesis context can introduce authority only into its transaction World.")

    member internal _.AllocateReferenceEpoch(
        worldScope: Guid,
        worldTransaction: Guid option,
        genesisActive: bool,
        kind,
        parts
    ) =
        lock allocationGate (fun () ->
            if not active then
                invalidOp "A completed Genesis context cannot introduce authority."

            if scope <> worldScope then
                invalidOp "A Genesis context cannot introduce authority into another domain."

            if worldTransaction <> Some transaction || not genesisActive then
                invalidOp "A Genesis context can introduce authority only into its transaction World."

            nextAllocation <- nextAllocation + 1L
            ReferenceIdentity.derive transaction nextAllocation kind parts)

    member internal _.Complete() =
        lock allocationGate (fun () -> active <- false)

    member internal _.IssuedAtMilliseconds = issuedAtMilliseconds

[<RequireQualifiedAccess>]
module World =
    let private allocationEpoch kind parts (world: World) =
        ReferenceIdentity.derive world.ReferenceEpoch world.NextReference kind parts

    let private mutationAllowed (world: World) =
        world.AuthorityTransactions.AllowsMutation(world.GenesisTransaction)

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
            @ [ { Reference = BuiltIn.executionRateLimitShape
                  Description = "Brontide.Minimal Base execution-rate value"
                  Body =
                    RecordShape
                        [ { Name = "maximum-executions"; Shape = BuiltIn.integerShape; Required = true }
                          { Name = "window-milliseconds"; Shape = BuiltIn.integerShape; Required = true } ]
                  AcceptedFragments = Set.empty
                  IsOpenToFragments = false } ]
            |> Seq.map (fun definition -> definition.Reference, definition)
            |> Map.ofSeq

        let referenceEpoch = scope
        let authorityReference = ActorReference.issue scope referenceEpoch 1L
        let authorityActor =
            { Reference = authorityReference
              Name = CanonicalName.create "Brontide.Minimal:AuthorityDomain" }

        let standardConstraint value name shape description accountingScope =
            let reference = ConstraintReference.issue scope referenceEpoch value
            reference,
            { Reference = reference
              Declaration =
                { Name = name
                  Version = 1
                  ValueShape = shape
                  EvaluationSemantics = description
                  EvaluatorDomain = ConstraintEvaluatorDomain.TargetAuthority
                  UnknownBehavior = ConstraintUnknownBehavior.Deny
                  AccountingScope = accountingScope
                  EvolutionPolicy = ConstraintEvolutionPolicy.ParallelCanonicalName } }

        let constraints =
            [ standardConstraint -1L BuiltIn.delegationDepthConstraintName BuiltIn.integerShape "maximum additional derivation links" ConstraintAccountingScope.NotQuantified
              standardConstraint -2L BuiltIn.originGrantConstraintName BuiltIn.textShape "genesis-grade origin assertion" ConstraintAccountingScope.NotQuantified
              standardConstraint -3L BuiltIn.originCeilingConstraintName BuiltIn.textShape "maximum delegated origin assertion" ConstraintAccountingScope.NotQuantified
              standardConstraint -4L BuiltIn.livenessLeaseConstraintName BuiltIn.textShape "the declared liveness lease is active at presentation" ConstraintAccountingScope.NotQuantified
              standardConstraint -5L BuiltIn.executionRateLimitConstraintName BuiltIn.executionRateLimitShape "successful Executions do not exceed the occurrence-pooled maximum in the current window" ConstraintAccountingScope.ChainOccurrencePooling ]
            |> Map.ofList

        { Scope = scope
          AuthorityTransactions = AuthorityTransactionCoordinator()
          ReferenceEpoch = referenceEpoch
          GenesisTransaction = None
          NextReference = 2L
          GenesisActive = false
          AuthorityActor = authorityReference
          Actors = Map.ofList [ authorityReference, authorityActor ]
          RetiredActors = Set.empty
          Capabilities = Map.empty
          ExtinguishedCapabilities = Set.empty
          LivenessLeases = Map.empty
          QuantifiedUsage = Map.empty
          CapabilityConstraintExpressions = Map.empty
          Shapes = shapes
          Fragments = Map.empty
          Constraints = constraints
          Operations = Map.empty
          EventDefinitions = Map.empty
          Executions = []
          Events = []
          Provenance = []
          GenesisOccurrences = []
          TerminusOccurrences = []
          TimeDomain = timeDomain
          LastLogicalTime = Int64.MinValue }

    let scope (world: World) = world.Scope
    let actors (world: World) = world.Actors |> Map.toSeq |> Seq.map snd |> Seq.toList
    let retiredActors (world: World) = world.RetiredActors |> Set.toList
    let shapes (world: World) = world.Shapes |> Map.toSeq |> Seq.map snd |> Seq.toList
    let operations (world: World) = world.Operations |> Map.toSeq |> Seq.map snd |> Seq.toList
    let capabilities (world: World) = world.Capabilities |> Map.toSeq |> Seq.map snd |> Seq.toList
    let executions (world: World) = world.Executions
    let events (world: World) = world.Events
    let provenance (world: World) = world.Provenance
    let genesisOccurrences (world: World) = world.GenesisOccurrences
    let terminusOccurrences (world: World) = world.TerminusOccurrences
    let terminusPolicy (_: World) =
        { HeldCapabilityDisposition = HeldCapabilitiesExtinguishedImmediately
          OutboundGrantDisposition = ImmortalSurvivesIndefinitely
          LivenessScopedGrantDisposition = LivenessScopedGrantsExtinguishedImmediately
          ActorReferenceDisposition = RetainedWithoutReuse }
    let timeDomain (world: World) = world.TimeDomain
    let lastLogicalTime (world: World) = world.LastLogicalTime
    let tryFindShape (reference: ShapeReference) (world: World) = Map.tryFind reference world.Shapes
    let tryFindOperation (reference: OperationReference) (world: World) = Map.tryFind reference world.Operations

    let constraintRecognitionSet (environment: Environment) (world: World) =
        world.Constraints
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.map (fun definition ->
            let isStandard =
                definition.Name = BuiltIn.delegationDepthConstraintName
                || definition.Name = BuiltIn.originGrantConstraintName
                || definition.Name = BuiltIn.originCeilingConstraintName
                || definition.Name = BuiltIn.livenessLeaseConstraintName
                || definition.Name = BuiltIn.executionRateLimitConstraintName
            let enforceable =
                match definition.Declaration.AccountingScope with
                | ConstraintAccountingScope.NotQuantified ->
                    isStandard || Map.containsKey definition.Reference environment.ConstraintEvaluators
                | ConstraintAccountingScope.ChainOccurrencePooling ->
                    definition.Name = BuiltIn.executionRateLimitConstraintName
                | ConstraintAccountingScope.VocabularyDefined _ -> false
            { Declaration = definition.Declaration
              Decision =
                if enforceable then
                    ConstraintRecognitionDecision.Implemented
                else
                    ConstraintRecognitionDecision.Declined })
        |> Seq.sortBy _.Declaration.Name
        |> Seq.toList
    let tryFindCapability (reference: CapabilityReference) (world: World) =
        Map.tryFind reference world.Capabilities
    let tryFindLivenessLease (reference: LivenessLeaseReference) (world: World) =
        Map.tryFind reference world.LivenessLeases
    let capabilityConstraintExpressions (reference: CapabilityReference) (world: World) =
        Map.tryFind reference world.CapabilityConstraintExpressions
    let tryFindConstraint (reference: ConstraintReference) (world: World) =
        Map.tryFind reference world.Constraints
    let capabilityDerivationChain (reference: CapabilityReference) (world: World) =
        let rec collect visited current acc =
            if Set.contains current visited then
                Error "The Capability derivation chain contains a cycle."
            else
                match Map.tryFind current world.Capabilities with
                | None -> Error "The Capability derivation chain contains an unknown parent."
                | Some capability ->
                    match capability.Parent with
                    | None -> Ok(capability :: acc)
                    | Some parent -> collect (Set.add current visited) parent (capability :: acc)
        collect Set.empty reference []
    let tryFindConstraintByName (name: CanonicalName) (world: World) =
        world.Constraints
        |> Map.toSeq
        |> Seq.map snd
        |> Seq.tryFind (fun definition -> definition.Name = name)

    let genesis
        (policy: CanonicalName)
        (recordedAt: TemporalMark)
        (initialize: GenesisContext -> World -> 'T * World)
        (world: World)
        =
        if world.GenesisTransaction.IsSome then
            Error "An uncommitted Genesis branch cannot start another Genesis occurrence."
        elif recordedAt.TimeDomain <> world.TimeDomain then
            Error "Genesis must use the authority domain's trusted time domain."
        elif recordedAt.Milliseconds < world.LastLogicalTime then
            Error "Genesis time cannot move backwards."
        else
            let transactionEpoch = Guid.NewGuid()
            world.AuthorityTransactions.RunGenesis(
                transactionEpoch,
                (fun () -> Error "Genesis occurrences cannot be nested."),
                (fun () ->
                    let context = GenesisContext(world.Scope, transactionEpoch, recordedAt.Milliseconds)
                    let transactionWorld =
                        { world with
                            ReferenceEpoch = transactionEpoch
                            GenesisTransaction = Some transactionEpoch
                            GenesisActive = true }
                    let actorsBefore = world.Actors |> Map.keys |> Set.ofSeq
                    let capabilitiesBefore = world.Capabilities |> Map.keys |> Set.ofSeq

                    try
                        let value, initialized = initialize context transactionWorld
                        context.Complete()

                        if initialized.Scope <> world.Scope then
                            invalidOp "Genesis returned a World from another authority domain."

                        if initialized.GenesisTransaction <> Some transactionEpoch || not initialized.GenesisActive then
                            invalidOp "Genesis must return the transaction World supplied to its callback."

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

                        let occurrenceEpoch =
                            allocationEpoch
                                "genesis-occurrence"
                                [ CanonicalName.value policy
                                  yield! ReferenceIdentity.temporalMarkTokens recordedAt ]
                                initialized

                        let occurrence =
                            OccurrenceReference.issue initialized.Scope occurrenceEpoch initialized.NextReference

                        let genesisRecord =
                            { Occurrence = occurrence
                              Policy = policy
                              IntroducedActors = introducedActors
                              IntroducedCapabilities = introducedCapabilities
                              RecordedAt = recordedAt }

                        Ok(
                            value,
                            { initialized with
                                ReferenceEpoch = occurrenceEpoch
                                GenesisTransaction = None
                                NextReference = initialized.NextReference + 1L
                                GenesisActive = false
                                GenesisOccurrences = initialized.GenesisOccurrences @ [ genesisRecord ]
                                LastLogicalTime = recordedAt.Milliseconds }
                        )
                    finally
                        context.Complete())
            )

    let registerShape (definition: ShapeDefinition) (world: World) =
        if not (mutationAllowed world) then
            Error "World mutation is unavailable outside the active Genesis branch."
        elif definition.Reference.Version < 1 then
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
        if not (mutationAllowed world) then
            Error "World mutation is unavailable outside the active Genesis branch."
        elif Map.containsKey definition.Reference world.Fragments then
            Error "That fragment version is already registered."
        elif not (Map.containsKey definition.HostShape world.Shapes) then
            Error "The fragment host Shape is not registered."
        elif not (Map.containsKey definition.Shape world.Shapes) then
            Error "The fragment shape is not registered."
        else
            Ok
                { world with
                    Fragments = Map.add definition.Reference definition world.Fragments }

    let registerConstraintDeclaration (declaration: ConstraintDeclaration) (world: World) =
        if not (mutationAllowed world) then
            Error "World mutation is unavailable outside the active Genesis branch."
        elif declaration.Version <= 0 then
            Error "A Constraint declaration version must be positive."
        elif String.IsNullOrWhiteSpace declaration.EvaluationSemantics then
            Error "Constraint evaluation semantics are required."
        elif declaration.EvaluatorDomain <> ConstraintEvaluatorDomain.TargetAuthority
             || declaration.UnknownBehavior <> ConstraintUnknownBehavior.Deny
             || declaration.EvolutionPolicy <> ConstraintEvolutionPolicy.ParallelCanonicalName then
            Error "The Constraint does not use the Architecture 0.8 authority declaration regime."
        elif not (Map.containsKey declaration.ValueShape world.Shapes) then
            Error "The constraint parameter shape is not registered."
        elif world.Constraints |> Map.exists (fun _ existing -> existing.Name = declaration.Name) then
            let existing =
                world.Constraints
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun candidate -> candidate.Name = declaration.Name)
            if existing.Declaration = declaration then
                Error "That Constraint canonical name is already declared."
            else
                Error "A changed Constraint declaration requires a new canonical name."
        else
            let referenceEpoch =
                allocationEpoch
                    "constraint"
                    [ CanonicalName.value declaration.Name
                      string declaration.Version
                      ReferenceIdentity.shapeReference declaration.ValueShape
                      declaration.EvaluationSemantics
                      string declaration.EvaluatorDomain
                      string declaration.UnknownBehavior
                      string declaration.AccountingScope
                      string declaration.EvolutionPolicy ]
                    world

            let reference = ConstraintReference.issue world.Scope referenceEpoch world.NextReference

            let definition =
                { Reference = reference
                  Declaration = declaration }

            Ok(
                definition,
                { world with
                    ReferenceEpoch = referenceEpoch
                    NextReference = world.NextReference + 1L
                    Constraints = Map.add reference definition world.Constraints }
            )

    let registerConstraint
        (name: CanonicalName)
        (parameterShape: ShapeReference)
        (description: string)
        (world: World)
        =
        registerConstraintDeclaration
            { Name = name
              Version = 1
              ValueShape = parameterShape
              EvaluationSemantics = description
              EvaluatorDomain = ConstraintEvaluatorDomain.TargetAuthority
              UnknownBehavior = ConstraintUnknownBehavior.Deny
              AccountingScope = ConstraintAccountingScope.NotQuantified
              EvolutionPolicy = ConstraintEvolutionPolicy.ParallelCanonicalName }
            world

    let registerOperation (definition: OperationDefinition) (world: World) =
        if not (mutationAllowed world) then
            Error "World mutation is unavailable outside the active Genesis branch."
        elif Map.containsKey definition.Reference world.Operations then
            Error "That operation is already registered."
        elif not (Map.containsKey definition.Target world.Actors) then
            Error "The operation target is not an issued Actor in this domain."
        elif Set.contains definition.Target world.RetiredActors then
            Error "A retired Actor cannot become an Operation target."
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
        if not (mutationAllowed world) then
            Error "World mutation is unavailable outside the active Genesis branch."
        elif Map.containsKey definition.Reference world.EventDefinitions then
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

    let private validateAuthorityValue
        (declaredShape: ShapeReference)
        (presentedShape: ShapeReference)
        (value: ShapeValue)
        (world: World)
        =
        if presentedShape <> declaredShape then
            Error "Constraint values are not eligible for additive Shape projection."
        else
            match validateValue presentedShape value world with
            | Error message -> Error message
            | Ok() ->
                match world.Shapes[presentedShape].Body, value with
                | RecordShape _, RecordValue(_, fragments) ->
                    let accepted = world.Shapes[presentedShape].AcceptedFragments
                    if fragments |> Map.exists (fun reference _ -> not (Set.contains reference accepted)) then
                        Error "Constraint values cannot contain projected-away Fragments."
                    else
                        Ok()
                | _ -> Ok()

    let private validateConstraintRequirements (requirements: ConstraintRequirement list) (world: World) =
        requirements
        |> List.tryPick (fun requirement ->
            match Map.tryFind requirement.Constraint world.Constraints with
            | None -> Some "A Capability refers to an unknown Constraint."
            | Some definition ->
                if requirement.ParameterShape.Name <> definition.ParameterShape.Name then
                    Some "A Capability Constraint value uses a different Shape lineage from its declaration."
                else
                    match validateValue requirement.ParameterShape requirement.Parameters world with
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
        let referenceEpoch =
            context.AllocateReferenceEpoch(
                world.Scope,
                world.GenesisTransaction,
                world.GenesisActive,
                "actor",
                [ CanonicalName.value name ]
            )

        let reference = ActorReference.issue world.Scope referenceEpoch world.NextReference
        let actor = { Reference = reference; Name = name }

        actor,
        { world with
            ReferenceEpoch = referenceEpoch
            NextReference = world.NextReference + 1L
            Actors = Map.add reference actor world.Actors }

    let internal issueLivenessLease
        (context: GenesisContext)
        (grantor: ActorReference)
        (durationMilliseconds: int64)
        (world: World)
        =
        context.EnsureActive(world.Scope, world.GenesisTransaction, world.GenesisActive)
        if not (Map.containsKey grantor world.Actors) then
            Error "A liveness lease grantor must be an issued Actor."
        elif Set.contains grantor world.RetiredActors then
            Error "A retired Actor cannot maintain a liveness lease."
        elif durationMilliseconds <= 0L then
            Error "A liveness lease duration must be positive."
        elif context.IssuedAtMilliseconds > Int64.MaxValue - durationMilliseconds then
            Error "The liveness lease expiry is not representable."
        else
            let referenceEpoch =
                allocationEpoch
                    "liveness-lease"
                    [ string (ActorReference.value grantor)
                      string durationMilliseconds
                      string context.IssuedAtMilliseconds ]
                    world
            let reference = LivenessLeaseReference.issue world.Scope referenceEpoch world.NextReference
            let lease =
                { Reference = reference
                  Grantor = grantor
                  ExpiresAtMilliseconds = context.IssuedAtMilliseconds + durationMilliseconds
                  Dead = false }
            Ok(
                lease,
                { world with
                    ReferenceEpoch = referenceEpoch
                    NextReference = world.NextReference + 1L
                    LivenessLeases = Map.add reference lease world.LivenessLeases })

    let livenessLeaseConstraint (lease: LivenessLease) (world: World) =
        if not (Map.containsKey lease.Reference world.LivenessLeases) then
            Error "The liveness lease is not registered by this authority domain."
        else
            let definition =
                tryFindConstraintByName BuiltIn.livenessLeaseConstraintName world
                |> Option.defaultWith (fun () -> invalidOp "The Base liveness Constraint is missing.")
            Ok
                { Constraint = definition.Reference
                  ParameterShape = definition.ParameterShape
                  Parameters = TextValue(string (LivenessLeaseReference.value lease.Reference)) }

    let executionRateLimitConstraint maximumExecutions windowMilliseconds (world: World) =
        if maximumExecutions <= 0L then
            Error "The execution maximum must be positive."
        elif windowMilliseconds <= 0L then
            Error "The accounting window must be positive."
        else
            let definition =
                tryFindConstraintByName BuiltIn.executionRateLimitConstraintName world
                |> Option.defaultWith (fun () -> invalidOp "The Base execution-rate Constraint is missing.")
            Ok
                { Constraint = definition.Reference
                  ParameterShape = definition.ParameterShape
                  Parameters =
                    RecordValue(
                        Map.ofList
                            [ "maximum-executions", IntegerValue maximumExecutions
                              "window-milliseconds", IntegerValue windowMilliseconds ],
                        Map.empty) }

    let internal issuePrimordialCapabilityWithExpressions
        (context: GenesisContext)
        (name: CanonicalName)
        (holder: ActorReference)
        (target: ActorReference)
        (operations: Set<OperationReference>)
        (expressions: ConstraintExpression list)
        (world: World)
        =
        context.EnsureActive(world.Scope, world.GenesisTransaction, world.GenesisActive)

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
        elif Set.contains holder world.RetiredActors || Set.contains target world.RetiredActors then
            Error "A retired Actor cannot hold or target newly issued authority."
        elif Set.isEmpty operations then
            Error "A Capability must authorize at least one Operation."
        elif not operationsRecognized then
            Error "A Capability Operation is unknown or belongs to another target."
        else
            match validateConstraintExpressions expressions world with
            | Error message -> Error message
            | Ok() ->
                let allocationParts =
                    ReferenceIdentity.capabilityAllocationTokens
                        name
                        holder
                        target
                        operations
                        expressions
                        None
                        None

                let referenceEpoch =
                    context.AllocateReferenceEpoch(
                        world.Scope,
                        world.GenesisTransaction,
                        world.GenesisActive,
                        "capability",
                        allocationParts
                    )

                let reference = CapabilityReference.issue world.Scope referenceEpoch world.NextReference

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
                      IssuedBy = None }

                Ok(
                    capability,
                    { world with
                        ReferenceEpoch = referenceEpoch
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
        (world: World)
        =
        issuePrimordialCapabilityWithExpressions
            context
            name
            holder
            target
            operations
            (constraints |> List.map AtomicConstraint)
            world

    let delegateCapabilityWithExpressions
        (name: CanonicalName)
        (delegator: ActorReference)
        (newHolder: ActorReference)
        (parentReference: CapabilityReference)
        (addedExpressions: ConstraintExpression list)
        (world: World)
        =
        match mutationAllowed world, Map.tryFind parentReference world.Capabilities with
        | false, _ -> Error "World mutation is unavailable outside the active Genesis branch."
        | true, None -> Error "The parent Capability is unknown."
        | true, Some parent when parent.Holder <> delegator ->
            Error "Only the Capability holder may delegate it."
        | true, Some _ when Set.contains delegator world.RetiredActors ->
            Error "A retired Actor cannot delegate authority."
        | true, Some _ when Set.contains parentReference world.ExtinguishedCapabilities ->
            Error "An extinguished Capability cannot be delegated."
        | true, Some _ when not (Map.containsKey newHolder world.Actors) ->
            Error "The delegated Capability holder is unknown."
        | true, Some _ when Set.contains newHolder world.RetiredActors ->
            Error "A retired Actor cannot receive delegated authority."
        | true, Some parent when Set.contains parent.Target world.RetiredActors ->
            Error "Authority cannot be delegated to a retired target Actor."
        | true, Some parent ->
            let originCeiling =
                world.Constraints
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun definition -> definition.Name = BuiltIn.originCeilingConstraintName)
            let derivedExpressions =
                addedExpressions
                @ [ AtomicConstraint
                        { Constraint = originCeiling.Reference
                          ParameterShape = BuiltIn.textShape
                          Parameters = TextValue "Derived" } ]
            match validateConstraintExpressions derivedExpressions world with
            | Error message -> Error message
            | Ok() ->
                let referenceEpoch =
                    ReferenceIdentity.capabilityAllocationTokens
                        name
                        newHolder
                        parent.Target
                        parent.Operations
                        derivedExpressions
                        (Some parent.Reference)
                        (Some delegator)
                    |> fun parts -> allocationEpoch "capability" parts world

                let reference = CapabilityReference.issue world.Scope referenceEpoch world.NextReference

                let atomicCompatibility =
                    derivedExpressions
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
                      IssuedBy = Some delegator }

                Ok(
                    capability,
                    { world with
                        ReferenceEpoch = referenceEpoch
                        NextReference = world.NextReference + 1L
                        Capabilities = Map.add reference capability world.Capabilities
                        CapabilityConstraintExpressions =
                            Map.add reference derivedExpressions world.CapabilityConstraintExpressions }
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

    let private allocateExecution request recordedAt (world: World) =
        let referenceEpoch =
            ReferenceIdentity.executionTokens request recordedAt
            |> fun parts -> allocationEpoch "execution" parts world

        ExecutionReference.issue world.Scope referenceEpoch world.NextReference,
        { world with
            ReferenceEpoch = referenceEpoch
            NextReference = world.NextReference + 1L }

    let private allocateOccurrence kind (world: World) =
        let referenceEpoch = allocationEpoch kind [] world

        OccurrenceReference.issue world.Scope referenceEpoch world.NextReference,
        { world with
            ReferenceEpoch = referenceEpoch
            NextReference = world.NextReference + 1L }

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
        let recordedAt = observableTime environment world
        let execution, nextWorld = allocateExecution request recordedAt world
        let occurrence, afterOccurrence = allocateOccurrence "outcome-occurrence" nextWorld
        let emitter =
            if Map.containsKey request.Target world.Actors then request.Target else world.AuthorityActor

        let outcomeEvent =
            { Reference = BuiltIn.executionOutcomeEvent
              Occurrence = occurrence
              Emitter = emitter
              CausedBy = execution
              Payload = details |> Option.defaultValue UnitValue
              Origin = OriginClass.Unverified
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

    let private hasMaintainedLivenessScope actor (capability: Capability) (world: World) =
        capabilityChain capability world
        |> List.collect (fun item ->
            world.CapabilityConstraintExpressions
            |> Map.tryFind item.Reference
            |> Option.defaultValue (item.AddedConstraints |> List.map AtomicConstraint))
        |> List.collect ConstraintExpression.atoms
        |> List.exists (fun requirement ->
            match Map.tryFind requirement.Constraint world.Constraints, requirement.Parameters with
            | Some definition, TextValue token when definition.Name = BuiltIn.livenessLeaseConstraintName ->
                world.LivenessLeases
                |> Map.exists (fun _ lease ->
                    lease.Grantor = actor
                    && string (LivenessLeaseReference.value lease.Reference) = token)
            | _ -> false)

    let terminateActor
        (policyActor: ActorReference)
        (actor: ActorReference)
        (reason: string)
        (recordedAt: TemporalMark)
        (world: World)
        =
        world.AuthorityTransactions.RunRuntime(
            world.GenesisTransaction,
            (fun () -> Error "Terminus is unavailable inside or through an uncommitted Genesis occurrence."),
            (fun () ->
                if String.IsNullOrWhiteSpace reason then
                    Error "A Terminus occurrence requires an attributable reason."
                elif recordedAt.TimeDomain <> world.TimeDomain then
                    Error "Terminus must use the authority domain's trusted time domain."
                elif recordedAt.Milliseconds < world.LastLogicalTime then
                    Error "Terminus time cannot move backwards."
                elif recordedAt.UncertaintyMilliseconds |> Option.exists (fun value -> value < 0L) then
                    Error "Terminus time uncertainty cannot be negative."
                elif not (Map.containsKey policyActor world.Actors) || Set.contains policyActor world.RetiredActors then
                    Error "The Terminus policy Actor is unknown or retired."
                elif not (Map.containsKey actor world.Actors) || Set.contains actor world.RetiredActors then
                    Error "The Actor is unknown or already retired."
                elif actor = world.AuthorityActor then
                    Error "The authority-domain Actor cannot be retired by dynamic Terminus."
                else
                    let capabilities = world.Capabilities |> Map.toList |> List.map snd
                    let held =
                        capabilities
                        |> List.filter (fun capability -> capability.Holder = actor)
                        |> List.map _.Reference
                    let outbound =
                        capabilities
                        |> List.filter (fun capability ->
                            match capability.Parent with
                            | Some parent -> world.Capabilities[parent].Holder = actor
                            | None -> false)
                    let livenessScoped =
                        outbound
                        |> List.filter (fun capability -> hasMaintainedLivenessScope actor capability world)
                    let livenessScopedReferences = livenessScoped |> List.map _.Reference |> Set.ofList
                    let extinguished =
                        capabilities
                        |> List.filter (fun capability ->
                            capabilityChain capability world
                            |> List.exists (fun ancestor -> Set.contains ancestor.Reference livenessScopedReferences))
                        |> List.map _.Reference
                        |> Set.ofList
                    let surviving =
                        outbound
                        |> List.filter (fun capability -> not (Set.contains capability.Reference livenessScopedReferences))
                        |> List.map _.Reference
                    let leases =
                        world.LivenessLeases
                        |> Map.map (fun _ lease -> if lease.Grantor = actor then { lease with Dead = true } else lease)
                    let occurrence, allocated = allocateOccurrence "terminus-occurrence" world
                    let policy = terminusPolicy world
                    let record =
                        { Occurrence = occurrence
                          PolicyActor = policyActor
                          ActorRetired = actor
                          Reason = reason
                          Policy = policy
                          HeldCapabilitiesExtinguished = held
                          OutboundGrantsSurviving = surviving
                          OutboundGrantsExtinguished = livenessScoped |> List.map _.Reference
                          RecordedAt = recordedAt }
                    Ok(
                        record,
                        { allocated with
                            RetiredActors = Set.add actor allocated.RetiredActors
                            ExtinguishedCapabilities = Set.union extinguished allocated.ExtinguishedCapabilities
                            LivenessLeases = leases
                            TerminusOccurrences = allocated.TerminusOccurrences @ [ record ]
                            LastLogicalTime = recordedAt.Milliseconds })
            ))

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

    let private projectPayloadValue
        (presentedShape: ShapeReference)
        (acceptedShape: ShapeReference)
        (value: ShapeValue)
        (world: World)
        =
        match validateValue presentedShape value world with
        | Error message -> Error message
        | Ok() when presentedShape.Name <> acceptedShape.Name || presentedShape.Version < acceptedShape.Version ->
            Error "The presented payload Shape cannot project to the Operation input Shape."
        | Ok() when presentedShape = acceptedShape -> Ok value
        | Ok() ->
            match value with
            | RecordValue _ -> projectRecord acceptedShape value world
            | _ -> validateValue acceptedShape value world |> Result.map (fun () -> value)

    let private stepCore
        (evaluateExpression:
            (ConstraintRequirement -> ConstraintAtomEvaluation) ->
                ConstraintExpression ->
                ConstraintExpressionEvaluation)
        (environment: Environment)
        (world: World)
        (request: ExecutionRequest)
        (requestedOrigin: OriginClass)
        (presentedCommandShape: ShapeReference option)
        (strictAuthorityValues: bool)
        =
        match Map.tryFind request.Operation world.Operations with
        | _ when world.GenesisActive ->
            deny environment request "Runtime execution is unavailable inside an active Genesis occurrence." world
        | _ when environment.TrustedTime.TimeDomain <> world.TimeDomain ->
            deny environment request "The target has no trusted clock for the supplied time domain." world
        | _ when environment.TrustedTime.Milliseconds < world.LastLogicalTime ->
            deny environment request "Trusted logical time cannot move backwards." world
        | _ when environment.TrustedTime.UncertaintyMilliseconds |> Option.exists (fun value -> value < 0L) ->
            deny environment request "Trusted time uncertainty cannot be negative." world
        | None -> deny environment request "The requested Operation is unknown." world
        | Some operation when not (Map.containsKey request.Initiator world.Actors) ->
            deny environment request "The initiating Actor is unknown." world
        | Some operation when Set.contains request.Initiator world.RetiredActors ->
            deny environment request "The initiating Actor is retired." world
        | Some operation when not (Map.containsKey request.Target world.Actors) ->
            deny environment request "The target Actor is unknown." world
        | Some operation when Set.contains request.Target world.RetiredActors ->
            deny environment request "The target Actor is retired." world
        | Some operation when operation.Target <> request.Target ->
            deny environment request "The Operation is not recognized by the requested target." world
        | Some operation when not (Map.containsKey request.PresentedCapability world.Capabilities) ->
            deny environment request "The presented Capability was not issued by this authority domain." world
        | Some operation when Set.contains request.PresentedCapability world.ExtinguishedCapabilities ->
            deny environment request "The presented Capability was extinguished by Terminus." world
        | Some operation ->
            let capability = world.Capabilities[request.PresentedCapability]

            if capability.Holder <> request.Initiator then
                deny environment request "The presented Capability does not designate the initiating Actor." world
            elif capability.Target <> request.Target then
                deny environment request "The presented Capability does not designate the requested target." world
            elif not (Set.contains request.Operation capability.Operations) then
                deny environment request "The presented Capability does not authorize the requested Operation." world
            else
                let projectedCommand =
                    match presentedCommandShape with
                    | None ->
                        validateValue operation.CommandShape request.Command world
                        |> Result.map (fun () -> request.Command)
                    | Some presented -> projectPayloadValue presented operation.CommandShape request.Command world

                match projectedCommand with
                | Error message -> deny environment request message world
                | Ok command ->
                    let request = { request with Command = command }
                    let context =
                        { Request = request
                          Operation = operation
                          LogicalTime = environment.TrustedTime.Milliseconds
                          RequestedOrigin = requestedOrigin
                          ConstraintCapability = capability }

                    let effectiveConstraints =
                        (operation.Constraints
                         |> List.mapi (fun index requirement -> capability, -index - 1, AtomicConstraint requirement))
                        @ (capabilityChain capability world
                           |> List.collect (fun chainCapability ->
                               world.CapabilityConstraintExpressions
                               |> Map.tryFind chainCapability.Reference
                               |> Option.defaultValue (
                                   chainCapability.AddedConstraints |> List.map AtomicConstraint
                               )
                               |> List.mapi (fun index expression -> chainCapability, index, expression)))

                    let pendingAccounting = ResizeArray<QuantifiedConstraintOccurrence>()

                    let evaluateAtom constraintCapability expressionIndex atomIndex requirement =
                        match Map.tryFind requirement.Constraint world.Constraints with
                        | None -> ConstraintAtomEvaluation.evaluatorFailed
                        | Some definition ->
                            let normalizedRequirement =
                                if strictAuthorityValues then
                                    validateAuthorityValue
                                        definition.ParameterShape
                                        requirement.ParameterShape
                                        requirement.Parameters
                                        world
                                    |> Result.map (fun () -> requirement)
                                else
                                    projectPayloadValue
                                        requirement.ParameterShape
                                        definition.ParameterShape
                                        requirement.Parameters
                                        world
                                    |> Result.map (fun parameters ->
                                        { requirement with
                                            ParameterShape = definition.ParameterShape
                                            Parameters = parameters })

                            match normalizedRequirement with
                            | Error _ -> ConstraintAtomEvaluation.invalidValue
                            | Ok requirement ->
                                let atomContext = { context with ConstraintCapability = constraintCapability }
                                match definition.Declaration.AccountingScope with
                                | ConstraintAccountingScope.VocabularyDefined _ ->
                                    ConstraintAtomEvaluation.unsupported definition.Name
                                | ConstraintAccountingScope.ChainOccurrencePooling when
                                    definition.Name <> BuiltIn.executionRateLimitConstraintName ->
                                    ConstraintAtomEvaluation.unsupported definition.Name
                                | _ when definition.Name = BuiltIn.livenessLeaseConstraintName && strictAuthorityValues ->
                                    match requirement.Parameters with
                                    | TextValue token ->
                                        let lease =
                                            world.LivenessLeases
                                            |> Map.toSeq
                                            |> Seq.map snd
                                            |> Seq.tryFind (fun candidate ->
                                                string (LivenessLeaseReference.value candidate.Reference) = token)
                                        match lease with
                                        | None -> ConstraintAtomEvaluation.unsupported definition.Name
                                        | Some value when value.Dead || environment.TrustedTime.Milliseconds >= value.ExpiresAtMilliseconds ->
                                            ConstraintAtomEvaluation.unsatisfied "the liveness lease is expired"
                                        | Some _ -> ConstraintAtomEvaluation.satisfied
                                    | _ -> ConstraintAtomEvaluation.invalidValue
                                | _ when definition.Name = BuiltIn.executionRateLimitConstraintName && strictAuthorityValues ->
                                    match requirement.Parameters with
                                    | RecordValue(fields, _) ->
                                        match Map.tryFind "maximum-executions" fields, Map.tryFind "window-milliseconds" fields with
                                        | Some(IntegerValue maximum), Some(IntegerValue window) when maximum > 0L && window > 0L ->
                                            let quotient = environment.TrustedTime.Milliseconds / window
                                            let windowIndex =
                                                if environment.TrustedTime.Milliseconds % window < 0L then
                                                    quotient - 1L
                                                else
                                                    quotient
                                            let occurrence =
                                                QuantifiedConstraintOccurrence(
                                                    constraintCapability.Reference,
                                                    expressionIndex,
                                                    atomIndex,
                                                    windowIndex)
                                            let committed = world.QuantifiedUsage |> Map.tryFind occurrence |> Option.defaultValue 0L
                                            let prepared = pendingAccounting |> Seq.filter ((=) occurrence) |> Seq.length |> int64
                                            if committed + prepared >= maximum then
                                                ConstraintAtomEvaluation.unsatisfied "the chain-occurrence execution budget is exhausted"
                                            else
                                                pendingAccounting.Add occurrence
                                                ConstraintAtomEvaluation.satisfied
                                        | _ -> ConstraintAtomEvaluation.invalidValue
                                    | _ -> ConstraintAtomEvaluation.invalidValue
                                | _ when definition.Name = BuiltIn.delegationDepthConstraintName ->
                                    match requirement.Parameters with
                                    | IntegerValue maximum when maximum >= 0L ->
                                        let chain = capabilityChain capability world
                                        let sourceIndex = chain |> List.findIndex ((=) constraintCapability)
                                        let linksBelow = int64 (List.length chain - sourceIndex - 1)
                                        if linksBelow <= maximum then
                                            ConstraintAtomEvaluation.satisfied
                                        else
                                            ConstraintAtomEvaluation.unsatisfied
                                                $"delegation depth {linksBelow} exceeds the Constraint ceiling {maximum}"
                                    | _ -> ConstraintAtomEvaluation.invalidValue
                                | _ when definition.Name = BuiltIn.originGrantConstraintName ->
                                    match requirement.Parameters with
                                    | TextValue granted ->
                                        if requestedOrigin = OriginClass.Unverified then
                                            ConstraintAtomEvaluation.satisfied
                                        elif capability.Parent.IsNone && string requestedOrigin = granted then
                                            ConstraintAtomEvaluation.satisfied
                                        elif capability.Parent.IsSome && requestedOrigin = OriginClass.Derived then
                                            ConstraintAtomEvaluation.satisfied
                                        else
                                            ConstraintAtomEvaluation.unsatisfied
                                                $"origin {requestedOrigin} exceeds the Capability's origin grant"
                                    | _ -> ConstraintAtomEvaluation.invalidValue
                                | _ when definition.Name = BuiltIn.originCeilingConstraintName ->
                                    match requirement.Parameters with
                                    | TextValue "Derived" when
                                        requestedOrigin = OriginClass.Unverified
                                        || requestedOrigin = OriginClass.Derived ->
                                        ConstraintAtomEvaluation.satisfied
                                    | TextValue "Derived" ->
                                        ConstraintAtomEvaluation.unsatisfied
                                            $"origin {requestedOrigin} exceeds the Derived ceiling"
                                    | _ -> ConstraintAtomEvaluation.invalidValue
                                | _ ->
                                    match Map.tryFind requirement.Constraint environment.ConstraintEvaluators with
                                    | None -> ConstraintAtomEvaluation.unsupported definition.Name
                                    | Some evaluator ->
                                        try
                                            match evaluator requirement.Parameters atomContext with
                                            | Ok() -> ConstraintAtomEvaluation.satisfied
                                            | Error message -> ConstraintAtomEvaluation.unsatisfied message
                                        with _ ->
                                            ConstraintAtomEvaluation.evaluatorFailed

                    let evaluations =
                        effectiveConstraints
                        |> List.map (fun (constraintCapability, expressionIndex, expression) ->
                            let mutable atomIndex = 0
                            evaluateExpression
                                (fun requirement ->
                                    let current = atomIndex
                                    atomIndex <- atomIndex + 1
                                    evaluateAtom constraintCapability expressionIndex current requirement)
                                expression)

                    let constraintFailure =
                        evaluations
                        |> List.tryFind (fun evaluation -> evaluation.Outcome = Indeterminate)
                        |> Option.orElseWith (fun () ->
                            evaluations
                            |> List.tryFind (fun evaluation -> evaluation.Outcome = Unsatisfied))
                        |> Option.map _.Reason

                    let originGrantPresent =
                        effectiveConstraints
                        |> List.collect (fun (_, _, expression) -> ConstraintExpression.atoms expression)
                        |> List.exists (fun requirement ->
                            world.Constraints[requirement.Constraint].Name = BuiltIn.originGrantConstraintName)

                    match constraintFailure with
                    | Some message -> deny environment request message world
                    | None when requestedOrigin <> OriginClass.Unverified && not originGrantPresent ->
                        deny environment request $"origin {requestedOrigin} was asserted without an origin grant" world
                    | None ->
                        let accountingWorld =
                            pendingAccounting
                            |> Seq.fold (fun state occurrence ->
                                let consumed = state.QuantifiedUsage |> Map.tryFind occurrence |> Option.defaultValue 0L
                                { state with QuantifiedUsage = Map.add occurrence (consumed + 1L) state.QuantifiedUsage }) world
                        match Map.tryFind operation.Reference environment.Handlers with
                        | None -> deny environment request "The Operation has no pure handler." world
                        | Some handler ->
                            match handler request with
                            | Error failure -> fail environment request failure accountingWorld
                            | Ok(result, eventDrafts, claimDrafts) ->
                                match validateValue operation.ResultShape result accountingWorld with
                                | Error message ->
                                    fail
                                        environment
                                        request
                                        (OperationFailure.withoutDetails
                                            ("The handler returned an invalid result: " + message))
                                        accountingWorld
                                | Ok() ->
                                    match
                                        eventDrafts
                                        |> List.tryPick (fun draft ->
                                            match validateEventDraft draft accountingWorld with
                                            | Ok() -> None
                                            | Error message -> Some message)
                                    with
                                    | Some message ->
                                        fail environment request (OperationFailure.withoutDetails message) accountingWorld
                                    | None ->
                                        let execution, afterExecution =
                                            allocateExecution request environment.TrustedTime accountingWorld

                                        let events, afterEvents =
                                            eventDrafts
                                            |> List.fold
                                                (fun (events, state) draft ->
                                                    let occurrence, nextState =
                                                        allocateOccurrence "event-occurrence" state

                                                    let event =
                                                        { Reference = draft.Reference
                                                          Occurrence = occurrence
                                                          Emitter = draft.Emitter
                                                          CausedBy = execution
                                                          Payload = draft.Payload
                                                          Origin = requestedOrigin
                                                          EmittedAt = environment.TrustedTime
                                                          OccurredAt = draft.OccurredAt }

                                                    event :: events,
                                                    nextState)
                                                ([], afterExecution)

                                        let emittedEvents = List.rev events
                                        let outcomeOccurrence, afterOutcome =
                                            allocateOccurrence "outcome-occurrence" afterEvents

                                        let outcomeEvent =
                                            { Reference = BuiltIn.executionOutcomeEvent
                                              Occurrence = outcomeOccurrence
                                              Emitter = request.Target
                                              CausedBy = execution
                                              Payload = result
                                              Origin = requestedOrigin
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

    let private stepUsing
        evaluateExpression
        requestedOrigin
        presentedCommandShape
        strictAuthorityValues
        (environment: Environment)
        (world: World)
        (request: ExecutionRequest)
        =
        world.AuthorityTransactions.RunRuntime(
            world.GenesisTransaction,
            (fun () ->
                deny
                    environment
                    request
                    "Runtime execution is unavailable inside or through an uncommitted Genesis occurrence."
                    world),
            (fun () ->
                stepCore
                    evaluateExpression
                    environment
                    world
                    request
                    requestedOrigin
                    presentedCommandShape
                    strictAuthorityValues)
        )

    let step (environment: Environment) (world: World) (request: ExecutionRequest) =
        stepUsing ConstraintExpression.evaluate OriginClass.Unverified None false environment world request

    /// Executes through the explicit Complete-Draft Architecture 0.8 A08-D1 authority path.
    /// The ordinary step function retains Architecture 0.7 poisoning semantics.
    let stepDraft08 (environment: Environment) (world: World) (request: Draft08ExecutionRequest) =
        stepUsing
            ConstraintExpression.evaluateStrongKleene
            request.RequestedOrigin
            (Some request.PresentedCommandShape)
            true
            environment
            world
            request.Request

[<RequireQualifiedAccess>]
module Genesis =
    let actor context name world = World.issueGenesisActor context name world

    let livenessLease context grantor durationMilliseconds world =
        World.issueLivenessLease context grantor durationMilliseconds world

    let capability context name holder target operations constraints world =
        World.issuePrimordialCapability
            context
            name
            holder
            target
            operations
            constraints
            world

    let capabilityWithExpressions context name holder target operations expressions world =
        World.issuePrimordialCapabilityWithExpressions
            context
            name
            holder
            target
            operations
            expressions
            world
