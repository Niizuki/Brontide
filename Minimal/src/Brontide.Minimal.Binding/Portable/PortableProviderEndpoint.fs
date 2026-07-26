namespace Brontide.Minimal.Binding.Portable

open System

/// What one provider Operation did.
///
/// A shaped failure is a case of the same union as a success because both are ordinary Outcomes:
/// the Operation reported structured details in its declared detail Shape, which is neither a
/// protocol nor a process failure and transports no exception.
[<StructuralEquality; NoComparison>]
type OperationEffect =
    | EffectSucceeded of result: PortableValue * providerEffectCount: int64
    | EffectFailed of details: PortableValue * providerEffectCount: int64

/// The provider's domain behind the binding.
///
/// This is where the provider's own admission happens for a cross-trust presentation: the handler
/// receives attributable context and exact addressing and decides for itself, reporting a refusal
/// as a shaped failed Outcome rather than as a protocol rejection.
type IPortableOperationHandler =
    abstract Invoke:
        operation: PortableOperationRef * input: PortableValue * resources: PortableResource list ->
            PortableResult<OperationEffect>

/// The provider's answer to one request, before any transport decision.
[<StructuralEquality; NoComparison>]
type ProviderOutcome =
    { Status: OutcomeStatus
      ValueShape: PortableShapeRef
      Value: PortableValue
      ProviderEffectCount: int64
      MappingObligations: string list
      Resources: ResourceObservation list
      CopyCount: int64 }

/// The semantic provider endpoint: lifecycle, negotiation, conformance, resource admission, and
/// dispatch, with no transport knowledge.
///
/// Both realizations drive this same endpoint, which is what makes their category-level
/// observations comparable: the direct call and the process seam differ in how a request arrives,
/// not in what the endpoint decides about it.
type PortableProviderEndpoint(offered: ContractDocument, handler: IPortableOperationHandler, realization: Realization) =

    let mutable lifecycle = Lifecycle.create false
    let mutable established: BindingPlan option = None

    /// Advances the lifecycle, or refuses the transition and leaves the previous state intact.
    let advance kind =
        Lifecycle.apply kind lifecycle
        |> Result.map (fun next -> lifecycle <- next)

    /// A refusal ends the binding, because the lifecycle's declared transition on a protocol error
    /// is to the failed state. Applying it here rather than in the transport is what keeps the
    /// direct realization's endpoint state equal to the process realization's.
    let failOnRefusal (result: PortableResult<'T>) =
        match result with
        | Error(Refused _) ->
            lifecycle <- Lifecycle.fail lifecycle
            result
        | Ok _
        | Error(Interrupted _) -> result

    let requirePlan () =
        match established with
        | Some plan -> Ok plan
        | None -> stateViolation "unestablished" "A request arrived before any contract was established."

    /// Attribution is never inferred from delivery, so a required Fragment must be present.
    let requireDeclaredFragments (value: PortableValue) (declaration: OperationDeclaration) =
        if List.isEmpty declaration.RequiredFragments then
            Ok()
        else
            let attached = PortableRecord.fragments value

            declaration.RequiredFragments
            |> iterate (fun fragment ->
                if Map.containsKey fragment attached then
                    Ok()
                else
                    invalidPayload
                        "required-fragment-absent"
                        $"Operation {PortableOperationRef.text declaration.Reference} requires Fragment {PortableFragmentRef.text fragment}.")

    member _.State = Lifecycle.state lifecycle

    member _.Plan = established

    member _.Realization = realization

    member _.Establish(required: ContractDocument, hostEndpoint: string) : PortableResult<EstablishAcceptedBody> =
        portable {
            do! advance EnvelopeKind.Establish

            let! plan =
                PortableNegotiation.negotiate
                    required
                    offered
                    realization
                    hostEndpoint
                    (PortableProviderRef.text offered.Provider)
                    "the provider endpoint offered the exact negotiated contract"

            lifecycle <- Lifecycle.declareReplayProtection (BindingPlan.replayProtectionDeclared plan) lifecycle
            do! advance EnvelopeKind.EstablishAccepted
            established <- Some plan

            return
                { Contract = offered
                  CompactIdentifiers = BindingPlan.compactAssignments plan }
        }
        |> failOnRefusal

    member _.SignalReady() = advance EnvelopeKind.Ready |> failOnRefusal

    member _.Withdraw() = advance EnvelopeKind.Withdraw |> failOnRefusal

    member _.Terminate() = advance EnvelopeKind.Terminate |> failOnRefusal

    member _.Fail() = lifecycle <- Lifecycle.fail lifecycle

    /// Resolves the Operation a request names, canonically or by a binding-scoped compact
    /// identifier. The transport needs the declaration before it can decode a schema-guided input.
    member _.ResolveOperation(designation: OperationDesignation) : PortableResult<OperationDeclaration> =
        portable {
            let! plan = requirePlan ()

            match designation with
            | OperationDesignation.Canonical reference -> return! BindingPlan.operation reference plan
            | OperationDesignation.Compact compact ->
                let assignment =
                    BindingPlan.compactAssignments plan
                    |> List.tryFind (fun candidate ->
                        candidate.Space = IdentitySpace.Operation
                        && CompactId.value candidate.Compact = compact)

                match assignment with
                | None ->
                    // A compact identifier this binding never assigned resolves to no canonical
                    // identity.
                    return!
                        unsupportedContract
                            "compact-identifier-unassigned"
                            $"Compact identifier {compact} was never assigned in this binding."
                | Some assignment ->
                    let reference =
                        BindingPlan.operations plan
                        |> List.tryFind (fun candidate -> PortableOperationRef.text candidate = assignment.Reference)

                    match reference with
                    | Some reference -> return! BindingPlan.operation reference plan
                    | None ->
                        return!
                            unsupportedContract
                                "compact-identifier-unassigned"
                                $"Compact identifier {compact} names no negotiated Operation."
        }

    member this.Request
        (
            request: ChannelRequestId,
            designation: OperationDesignation,
            inputShape: PortableShapeRef,
            input: PortableValue,
            resources: PortableResource list
        ) : PortableResult<ProviderOutcome> =
        portable {
            let! plan = requirePlan ()
            do! advance EnvelopeKind.Request
            let! next = Lifecycle.recordRequest request lifecycle
            lifecycle <- next
            let! declaration = this.ResolveOperation designation

            if BindingPlan.trustBoundaryCrossed plan then
                do! PortableAuthorityVocabulary.requireNoCapabilityValue input

            do!
                resources
                |> iterate (fun resource ->
                    let flavour = ResourceFlavor.token (PortableResource.flavor resource)

                    portable {
                        do!
                            ensure (List.contains flavour declaration.ResourceFlavors) (fun () ->
                                unsupportedContract
                                    "resource-flavor-unnegotiated"
                                    $"Operation {PortableOperationRef.text declaration.Reference} declares no resource flavor '{flavour}'.")

                        do!
                            ResourceCodec.admit
                                resource
                                (BindingPlan.resourceFlavors plan)
                                (BindingPlan.acceptedResourceHandles plan)
                                (BindingPlan.limits plan)
                    })

            let catalog = BindingPlan.catalog plan
            let! projected, obligations = PortableValueCodec.project catalog inputShape declaration.InputShape input
            do! PortableValueCodec.validate catalog declaration.InputShape declaration.RequiredFragments projected
            do! requireDeclaredFragments projected declaration

            let! effect =
                try
                    handler.Invoke(declaration.Reference, projected, resources)
                with _ ->
                    // The provider's own runtime failed. Only the portable category crosses: no
                    // exception, stack trace, or runtime type name is admitted into the observation
                    // or the frame.
                    refuse
                        ProtocolCategory.InternalProtocolFailure
                        "handler-failure"
                        "The endpoint cannot continue protocol processing."

            do! advance EnvelopeKind.Outcome

            let observations =
                resources |> List.map (fun resource -> PortableResource.observe resource realization)

            let status, valueShape, value, effectCount =
                match effect with
                | EffectSucceeded(result, count) -> OutcomeStatus.Succeeded, declaration.ResultShape, result, count
                | EffectFailed(details, count) -> OutcomeStatus.Failed, declaration.DetailShape, details, count

            return
                { Status = status
                  ValueShape = valueShape
                  Value = value
                  ProviderEffectCount = effectCount
                  MappingObligations = obligations
                  Resources = observations
                  CopyCount = observations |> List.sumBy (fun resource -> resource.Copies) }
        }
        |> failOnRefusal
