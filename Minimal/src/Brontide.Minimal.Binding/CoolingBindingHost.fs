namespace Brontide.Minimal.Binding

open System
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel

type MinimalExperimentalBindingObservation =
    { Host: string
      SelectedProvider: string
      SelectionReason: string
      RejectedAlternatives: string list
      Operation: PortableReference
      InputShape: PortableReference
      OutputShape: PortableReference
      Representation: string
      CrossedBoundaries: string list
      Copies: int
      ReferencedResources: string list
      HostAuthorityDecision: string
      AuthorityDecisionPoint: string
      MappingObligations: string list
      AdapterObligations: string list
      RetryCount: int
      Interrupted: bool
      ProviderProcessFailure: bool
      Fallback: string
      FailureDomain: string
      TerminalOutcome: string
      StartedAt: DateTimeOffset
      FinishedAt: DateTimeOffset
      RequestId: BindingRequestId
      BindingExecutionId: BindingExecutionId
      BindingOccurrenceId: BindingOccurrenceId
      HostExecutionId: ExecutionReference
      Requester: ActorReference
      ProviderEffectCount: int64 option }

type MinimalBoundExecutionResult =
    { Step: StepResult
      Observation: MinimalExperimentalBindingObservation
      ForwardedInput: ShapeValue option
      ProviderDetails: ShapeValue option }

type private MinimalExchangeFacts =
    { Provider: string
      SelectionReason: string
      ForwardedInput: ShapeValue option
      ProviderDetails: ShapeValue option
      ProviderEffectCount: int64 option
      Interrupted: bool
      ProviderProcessFailure: bool
      FailureDomain: string }

type MinimalCoolingBindingHost(
    launch: ProviderLaunch,
    clock: TimeProvider,
    ?requireUnknownConstraint: bool,
    ?manifestTransform: PortableManifest -> PortableManifest
) =
    let gate = obj ()
    let requireUnknownConstraint = defaultArg requireUnknownConstraint false
    let transform = defaultArg manifestTransform id
    let requiredManifest = PortableContract.manifest "brontide-minimal-host-requirement" |> transform
    let scope = Guid.NewGuid()
    let name value = CanonicalName.create value
    let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Interchange:HostClock")
    let shape reference = PortableContract.shapeReference reference
    let fragment reference = PortableContract.fragmentReference reference
    let operation = PortableContract.operationReference PortableContract.operation
    let commandShape = shape PortableContract.commandShape
    let resultShape = shape PortableContract.resultShape
    let detailsShape = shape PortableContract.detailsShape
    let hostContext = fragment PortableContract.hostContext
    let optionalNote = fragment PortableContract.optionalForwardingNote

    let shapeDefinition reference fields isOpen accepted : ShapeDefinition =
        { Reference = reference
          Description = "Cooling interchange test Shape"
          Body = RecordShape fields
          AcceptedFragments = accepted
          IsOpenToFragments = isOpen }

    let field fieldName fieldShape required : RecordField =
        { Name = fieldName
          Shape = fieldShape
          Required = required }

    let get = function
        | Ok value -> value
        | Error message -> failwith message

    let mutable initial = World.create scope timeDomain

    let hostContextShape: ShapeReference =
        { Name = name "interchange.tests.cooling.host-context.fields"
          Version = 1 }

    let optionalNoteShape: ShapeReference =
        { Name = name "third-party.cooling.note.fields"
          Version = 1 }

    do
        initial <-
            World.registerShape
                (shapeDefinition
                    commandShape
                    [ field "loop" BuiltIn.textShape true
                      field "enabled" BuiltIn.booleanShape true
                      field "failureMode" BuiltIn.textShape false ]
                    true
                    (Set.ofList [ hostContext; optionalNote ]))
                initial
            |> get

        initial <-
            World.registerShape
                (shapeDefinition
                    resultShape
                    [ field "loop" BuiltIn.textShape true
                      field "coolingEnabled" BuiltIn.booleanShape true
                      field "revision" BuiltIn.integerShape true
                      field "providerEffectCount" BuiltIn.integerShape true ]
                    false
                    Set.empty)
                initial
            |> get

        initial <-
            World.registerShape
                (shapeDefinition
                    detailsShape
                    [ field "code" BuiltIn.textShape true
                      field "message" BuiltIn.textShape true ]
                    false
                    Set.empty)
                initial
            |> get

        initial <-
            World.registerShape
                (shapeDefinition
                    hostContextShape
                    [ field "requesterLabel" BuiltIn.textShape true ]
                    false
                    Set.empty)
                initial
            |> get

        initial <-
            World.registerShape
                (shapeDefinition
                    optionalNoteShape
                    [ field "note" BuiltIn.textShape true ]
                    false
                    Set.empty)
                initial
            |> get

        initial <-
            World.registerFragment
                { Reference = hostContext
                  Description = "Host-local attributable context"
                  HostShape = commandShape
                  Shape = hostContextShape }
                initial
            |> get

        initial <-
            World.registerFragment
                { Reference = optionalNote
                  Description = "Optional authored data unknown to the semantic provider"
                  HostShape = commandShape
                  Shape = optionalNoteShape }
                initial
            |> get

    let mutable constraintRequirement: ConstraintRequirement list = []

    do
        if requireUnknownConstraint then
            let definition, withConstraint =
                World.registerConstraint
                    (name "interchange.tests.unknown-constraint")
                    BuiltIn.textShape
                    "Deliberately has no host evaluator."
                    initial
                |> get

            initial <- withConstraint
            constraintRequirement <-
                [ { Constraint = definition.Reference
                    Parameters = TextValue "must fail closed" } ]

    let (authorizedActor, deniedActor, targetActor, capability), ready =
        World.genesis
            (name "Brontide.Minimal.Interchange:HostBootstrapPolicy")
            { Milliseconds = clock.GetUtcNow().ToUnixTimeMilliseconds()
              TimeDomain = timeDomain
              UncertaintyMilliseconds = None }
            (fun genesis world ->
                let authorizedActor, world =
                    Genesis.actor genesis (name "Brontide.Minimal.Interchange:AuthorizedRequester") world

                let deniedActor, world =
                    Genesis.actor genesis (name "Brontide.Minimal.Interchange:DeniedRequester") world

                let targetActor, world =
                    Genesis.actor genesis (name "Brontide.Minimal.Interchange:CoolingTarget") world

                let operationDefinition: OperationDefinition =
                    { Reference = operation
                      Description = "Neutral binary Cooling through a foreign process provider"
                      Target = targetActor.Reference
                      CommandShape = commandShape
                      ResultShape = resultShape
                      Constraints = constraintRequirement }

                let world = World.registerOperation operationDefinition world |> get

                let capability, world =
                    Genesis.capability
                        genesis
                        (name "Brontide.Minimal.Interchange:CoolingCapability")
                        authorizedActor.Reference
                        targetActor.Reference
                        (Set.singleton operation)
                        []
                        false
                        world
                    |> get

                ((authorizedActor, deniedActor, targetActor, capability), world))
            initial
        |> get

    let mutable world = ready
    let mutable providerStarts = 0
    let mutable currentIds: (BindingRequestId * BindingExecutionId * BindingOccurrenceId) option = None
    let mutable exchange: MinimalExchangeFacts option = None

    let detailsMessage value =
        match value with
        | RecordValue(fields, _) ->
            match Map.tryFind "message" fields with
            | Some(TextValue message) -> message
            | _ -> "The foreign provider returned failed details."
        | _ -> "The foreign provider returned failed details."

    let handler (request: ExecutionRequest) =
        match World.validateContract commandShape (Set.singleton hostContext) request.Command world with
        | Error message ->
            exchange <-
                Some
                    { Provider = "not-activated"
                      SelectionReason = "required Fragment validation stopped binding before provider selection"
                      ForwardedInput = None
                      ProviderDetails = None
                      ProviderEffectCount = None
                      Interrupted = false
                      ProviderProcessFailure = false
                      FailureDomain = "host-shape" }

            Error(OperationFailure.withoutDetails message)
        | Ok() ->
            match currentIds with
            | None -> Error(OperationFailure.withoutDetails "No Brontide.Minimal binding invocation is active.")
            | Some(requestId, executionId, occurrenceId) ->
                providerStarts <- providerStarts + 1

                match
                    ProcessBindingClient.invoke
                        launch
                        (TimeSpan.FromSeconds 10.0)
                        requiredManifest
                        requestId
                        executionId
                        occurrenceId
                        request.Command
                with
                | Ok result ->
                    exchange <-
                        Some
                            { Provider = result.Provider
                              SelectionReason =
                                "exact component, Operation, Shape, Fragment, dependency, and protocol versions negotiated"
                              ForwardedInput = Some result.ForwardedInput
                              ProviderDetails = if result.Succeeded then None else Some result.Value
                              ProviderEffectCount = Some result.ProviderEffectCount
                              Interrupted = false
                              ProviderProcessFailure = false
                              FailureDomain = "none" }

                    if result.Succeeded then
                        Ok(
                            result.Value,
                            [],
                            [ name "brontide-minimal.interchange.provider", result.Provider ]
                        )
                    else
                        Error(
                            OperationFailure.withDetails
                                detailsShape
                                result.Value
                                (detailsMessage result.Value)
                        )
                | Error failure ->
                    exchange <-
                        Some
                            { Provider = if failure.FailureDomain = "binding-negotiation" then "not-activated" else "unknown"
                              SelectionReason = failure.Message
                              ForwardedInput = None
                              ProviderDetails = None
                              ProviderEffectCount = None
                              Interrupted = failure.Interrupted
                              ProviderProcessFailure = failure.ProviderProcessFailure
                              FailureDomain = failure.FailureDomain }

                    Error(OperationFailure.withoutDetails failure.Message)

    member _.AuthorizedActor = authorizedActor
    member _.DeniedActor = deniedActor
    member _.Operation = operation
    member _.CommandShape = commandShape
    member _.HostContext = hostContext
    member _.OptionalForwardingNote = optionalNote
    member _.ProviderStarts = providerStarts
    member _.World = world

    member _.Execute(actor: ActorReference, command: ShapeValue) =
        lock gate (fun () ->
            let started = clock.GetUtcNow()
            let requestId = BindingRequestId.create ()
            let bindingExecutionId = BindingExecutionId.create ()
            let bindingOccurrenceId = BindingOccurrenceId.create ()
            currentIds <- Some(requestId, bindingExecutionId, bindingOccurrenceId)
            exchange <- None

            try
                let environment: Environment =
                    { TrustedTime =
                        { Milliseconds = started.ToUnixTimeMilliseconds()
                          TimeDomain = timeDomain
                          UncertaintyMilliseconds = None }
                      ConstraintEvaluators = Map.empty
                      Handlers = Map.ofList [ operation, handler ] }

                let request: ExecutionRequest =
                    { Initiator = actor
                      Target = targetActor.Reference
                      PresentedCapability = capability.Reference
                      Operation = operation
                      Command = command
                      Occurrence = None
                      Context = Map.empty }

                let step = World.step environment world request
                world <- step.World
                let finished = clock.GetUtcNow()
                let facts = exchange
                let authority = if step.Outcome.Status = Denied then "denied" else "allowed"

                let observation =
                    { Host = "brontide-minimal"
                      SelectedProvider = facts |> Option.map _.Provider |> Option.defaultValue "not-activated"
                      SelectionReason =
                        facts
                        |> Option.map _.SelectionReason
                        |> Option.defaultValue "host authority stopped binding before provider selection"
                      RejectedAlternatives = []
                      Operation = PortableContract.operation
                      InputShape = PortableContract.commandShape
                      OutputShape = PortableContract.resultShape
                      Representation = "inline-tagged-json"
                      CrossedBoundaries = if facts.IsSome then [ "process" ] else []
                      Copies = if facts.IsSome then 2 else 0
                      ReferencedResources = []
                      HostAuthorityDecision = authority
                      AuthorityDecisionPoint = "Brontide.Minimal World.step before foreign process activation"
                      MappingObligations = [ "tagged ShapeValue to Brontide.Minimal-native ShapeValue" ]
                      AdapterObligations = []
                      RetryCount = 0
                      Interrupted = facts |> Option.map _.Interrupted |> Option.defaultValue false
                      ProviderProcessFailure =
                        facts |> Option.map _.ProviderProcessFailure |> Option.defaultValue false
                      Fallback = "none"
                      FailureDomain =
                        facts
                        |> Option.map _.FailureDomain
                        |> Option.defaultValue (if authority = "denied" then "host-authority" else "none")
                      TerminalOutcome = string step.Outcome.Status
                      StartedAt = started
                      FinishedAt = finished
                      RequestId = requestId
                      BindingExecutionId = bindingExecutionId
                      BindingOccurrenceId = bindingOccurrenceId
                      HostExecutionId = step.Outcome.Execution
                      Requester = actor
                      ProviderEffectCount = facts |> Option.bind _.ProviderEffectCount }

                { Step = step
                  Observation = observation
                  ForwardedInput = facts |> Option.bind _.ForwardedInput
                  ProviderDetails = facts |> Option.bind _.ProviderDetails }
            finally
                currentIds <- None
                exchange <- None)
