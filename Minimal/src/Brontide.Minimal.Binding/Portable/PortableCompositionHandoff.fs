namespace Brontide.Minimal.Binding.Portable

open System.Threading.Tasks

/// Identifies one binding scope in a composition.
///
/// The scope is the composition's identity for one requirement position. It survives withdrawal,
/// termination, and replacement; the plan identifier does not, because a replacement negotiates a
/// new plan rather than resuming the retired one. Construction is validated and private, so an
/// unchecked string cannot become a scope.
type BindingScopeId = private BindingScopeId of string

[<RequireQualifiedAccess>]
module BindingScopeId =

    [<Literal>]
    let MaxLength = 256

    let private permitted (character: char) =
        System.Char.IsAsciiLetterOrDigit character
        || character = '.'
        || character = '_'
        || character = '-'

    let tryCreate (value: string) : PortableResult<BindingScopeId> =
        if
            System.String.IsNullOrEmpty value
            || value.Length > MaxLength
            || not (Seq.forall permitted value)
        then
            malformed
                "binding-scope"
                $"'{value}' is not a binding-scope identifier: use 1..{MaxLength} characters from letters, digits, '.', '_', and '-'."
        else
            Ok(BindingScopeId value)

    let value (BindingScopeId value) = value

/// How a requirement's Provider Set is presented to its consumer.
[<RequireQualifiedAccess>]
type Exposure =
    | Distinct
    | Mediated

[<RequireQualifiedAccess>]
module Exposure =
    let token exposure =
        match exposure with
        | Exposure.Distinct -> "distinct"
        | Exposure.Mediated -> "mediated"

/// The declared membership bounds of one Provider Set.
///
/// Version 0.1 binds exactly one member. A wider bound is refused rather than narrowed, because a
/// Provider Set also needs membership, exposure, and failure semantics this seam does not own.
[<StructuralEquality; NoComparison>]
type ProviderCardinality = { Minimum: int; Maximum: int }

[<RequireQualifiedAccess>]
module ProviderCardinality =
    let oneToOne = { Minimum = 1; Maximum = 1 }
    let isOneToOne cardinality = cardinality = oneToOne
    let text cardinality = $"{cardinality.Minimum}..{cardinality.Maximum}"

/// What a resolution already decided about one requirement position.
///
/// The handoff consumes this; it never produces one. How the Component was discovered, acquired,
/// ranked, or selected happened before this record existed.
[<StructuralEquality; NoComparison>]
type ResolvedRequirement =
    { Scope: BindingScopeId
      Component: PortableComponentRef
      RequiredProvider: PortableProviderRef option
      Cardinality: ProviderCardinality
      Exposure: Exposure
      HostEndpoint: string }

[<RequireQualifiedAccess>]
module ResolvedRequirement =

    /// The ordinary case: one contract, one provider, presented directly.
    let oneToOneContract scope component' hostEndpoint =
        { Scope = scope
          Component = component'
          RequiredProvider = None
          Cardinality = ProviderCardinality.oneToOne
          Exposure = Exposure.Distinct
          HostEndpoint = hostEndpoint }

    /// The provider-specific case: the resolution names which provider must answer.
    let oneToOneProvider scope component' provider hostEndpoint =
        { oneToOneContract scope component' hostEndpoint with
            RequiredProvider = Some provider }

/// The provision the resolution selected, as claimed before any frame is exchanged.
[<StructuralEquality; NoComparison>]
type OfferedProvision =
    { Component: PortableComponentRef
      Provider: PortableProviderRef
      ProviderEndpoint: string }

/// What a retired binding tells a future replacement generation.
///
/// It is data about a binding that has ended and grants nothing: a replacement generation still
/// resolves, preflights, negotiates, and releases from the beginning.
[<StructuralEquality; NoComparison>]
type ReplacementRecord =
    { Scope: BindingScopeId
      RetiredPlan: PlanId
      Component: PortableComponentRef
      Provider: PortableProviderRef
      TerminalState: string
      Reason: string
      ReplacementPermitted: bool }

/// The named stages one composition member passes through.
///
/// The stage is a union carrying what the stage actually has, rather than a flag beside a nullable
/// binding: there is no established binding to reach before Interconnection and no released one to
/// interact with before Release, so an ordinary interaction outside the released case has nothing
/// to run against.
[<RequireQualifiedAccess>]
type CompositionStage =
    | LocalInitialisation
    | Interconnected of PortableBindingHost
    | Released of PortableBindingHost
    | Retired of PortableBindingHost * ReplacementRecord

[<RequireQualifiedAccess>]
module CompositionStage =
    let token stage =
        match stage with
        | CompositionStage.LocalInitialisation -> "local-initialisation"
        | CompositionStage.Interconnected _ -> "interconnected"
        | CompositionStage.Released _ -> "released"
        | CompositionStage.Retired _ -> "retired"

    /// The established binding, when the stage has one. A retired stage keeps its binding so a
    /// refusal after retirement still has the plan it happened under to observe against.
    let tryBinding stage =
        match stage with
        | CompositionStage.Interconnected host
        | CompositionStage.Released host
        | CompositionStage.Retired(host, _) -> Some host
        | CompositionStage.LocalInitialisation -> None

/// Records which provider actually answered the establish frame.
///
/// It exists because the offered contract is otherwise consumed inside establishment and never
/// surfaces: the Binding Plan's provider fact is read from the required document, so it reports who
/// the host asked for rather than who answered.
type private AnsweringProviderWitness(inner: IPortableProviderConversation) =
    let mutable offered: ContractDocument option = None

    member _.Offered = offered

    interface IPortableProviderConversation with
        member _.Realization = inner.Realization

        member _.Establish(required, hostEndpoint, channel) =
            task {
                let! result = inner.Establish(required, hostEndpoint, channel)

                match result with
                | Ok document -> offered <- Some document
                | Error _ -> ()

                return result
            }

        member _.AwaitReady channel = inner.AwaitReady channel

        member _.Request(plan, channel, request, execution, designation, inputShape, input, resources) =
            inner.Request(plan, channel, request, execution, designation, inputShape, input, resources)

        member _.Withdraw channel = inner.Withdraw channel
        member _.Terminate channel = inner.Terminate channel
        member _.Close() = inner.Close()

/// One Component occurrence's participation in a composition, from Local Initialisation to
/// retirement.
///
/// The member owns the ordinary-interaction gate. Trusted host or binding machinery has to enforce
/// it: a member that answered an ordinary interaction before Release would be Active while its
/// group was not, which is exactly what the release barrier exists to prevent.
type CompositionMember private (requirement: ResolvedRequirement, provision: OfferedProvision, required: ContractDocument) =

    let mutable stage = CompositionStage.LocalInitialisation
    let mutable answeringProvider: PortableProviderRef option = None

    let resolutionFacts =
        Map.ofList
            [ "bindingScope", BindingScopeId.value requirement.Scope
              "resolvedComponent", PortableComponentRef.text requirement.Component
              "requiredProvider",
              (match requirement.RequiredProvider with
               | Some provider -> PortableProviderRef.text provider
               | None -> "absent")
              "cardinality", ProviderCardinality.text requirement.Cardinality
              "exposure", Exposure.token requirement.Exposure
              "resolvedHostEndpoint", requirement.HostEndpoint
              "selectedProvision", PortableProviderRef.text provision.Provider
              "selectedProviderEndpoint", provision.ProviderEndpoint ]

    /// Ends a binding that was established but must not be released. Whether the peer can still be
    /// told is not interesting: the refusal being reported is.
    let abandon (host: PortableBindingHost) =
        task {
            let! _ = host.Terminate()
            host.Close()
        }

    /// The gate refusal, reported as a complete observation the way every other refusal is.
    ///
    /// The authority decision is 'unknown' because the gate refuses before the authority boundary is
    /// reached: no evaluation ran, and reporting 'permitted' would claim one that did not.
    let refusedByGate (host: PortableBindingHost) =
        let correlation =
            { Channel = host.Channel
              Request = ChannelRequestId.next ()
              HostNativeExecution = HostExecutionId.next () }

        { FrameDecision = FrameDecision.None
          ResultClass = ResultClass.ProtocolError
          Category = Some ProtocolCategory.StateViolation
          ProcessCategory = None
          Value = None
          Observation =
            ObservationBuilder.build
                host.Plan
                TerminalStatus.ProtocolError
                AuthorityDecision.Unknown
                AuthorityDecisionPoint.HostLocal
                correlation
                (Some FailureDomain.LocalEndpoint)
                0L
                0L
                []
                []
                false
                { EstablishedAtElapsedMilliseconds = 0L
                  RequestElapsedMilliseconds = 0L }
                (Some "gate-closed")
                (Some
                    $"Binding scope '{BindingScopeId.value requirement.Scope}' is not released, so no ordinary interaction is admitted.") }

    static member internal Create(requirement, provision, required) =
        CompositionMember(requirement, provision, required)

    member _.Requirement = requirement
    member _.Provision = provision
    member _.Scope = requirement.Scope
    member _.Stage = stage

    /// What the resolution fixed, answerable from Local Initialisation onward.
    member _.ResolutionFacts = resolutionFacts

    /// The frozen plan, or none while no contract has been negotiated.
    member _.TryPlan = CompositionStage.tryBinding stage |> Option.map _.Plan

    /// The provider that actually answered, or none before Interconnection.
    member _.AnsweringProvider = answeringProvider

    member _.IsReady =
        match CompositionStage.tryBinding stage with
        | Some host -> host.State = LifecycleState.Ready
        | None -> false

    member _.IsReleased =
        match stage with
        | CompositionStage.Released _ -> true
        | _ -> false

    /// One fact by name: a resolution fact at any stage, a plan fact once Interconnection has fixed
    /// one.
    ///
    /// A plan fact is unanswerable before Interconnection rather than answered with a placeholder.
    /// Answering one would claim an agreement that does not exist yet.
    member _.TryFact name =
        match Map.tryFind name resolutionFacts with
        | Some value -> Some value
        | None ->
            CompositionStage.tryBinding stage
            |> Option.bind (fun host -> BindingPlan.tryFact name host.Plan)

    /// Interconnection: negotiate the contract, freeze the plan, and wait for the readiness signal.
    /// The ordinary-interaction gate stays closed throughout.
    member _.Interconnect(conversation: IPortableProviderConversation) : Task<PortableResult<unit>> =
        task {
            match stage with
            | CompositionStage.LocalInitialisation ->
                let witness = AnsweringProviderWitness(conversation)
                let! established = PortableBindingHost.Establish(required, witness, requirement.HostEndpoint)

                match established with
                | Error error -> return Error error
                | Ok host ->
                    // Negotiation refuses an endpoint answering as a provider the host did not
                    // require, so what is left here is the case it cannot see: a required contract
                    // naming a provider this resolution did not select. Reachable only when the
                    // requirement named no provider, since preflight otherwise settles it before
                    // contact.
                    match witness.Offered with
                    | Some offered when offered.Provider = provision.Provider ->
                        stage <- CompositionStage.Interconnected host
                        answeringProvider <- Some offered.Provider
                        return Ok()
                    | Some offered ->
                        do! abandon host

                        return
                            unsupportedContract
                                "provider-substituted"
                                $"The resolution selected provider {PortableProviderRef.text provision.Provider} but the endpoint answered as {PortableProviderRef.text offered.Provider}; the binding is abandoned rather than rebound."
                    | None ->
                        // Establishment cannot succeed without the offered contract passing through
                        // the witness, so this states an invariant of this endpoint rather than
                        // anything a peer can cause: it is an internal protocol failure rather than a
                        // contract refusal.
                        do! abandon host

                        return
                            refuse
                                ProtocolCategory.InternalProtocolFailure
                                "witness-missing"
                                "Establishment reported success without an offered contract, so no provider identity could be checked."
            | other ->
                return
                    stateViolation
                        "illegal-stage"
                        $"'interconnect' is illegal for binding scope '{BindingScopeId.value requirement.Scope}' in stage '{CompositionStage.token other}'."
        }

    /// Release: the composition opens the ordinary-interaction gate.
    ///
    /// Release requires the readiness signal, stated once rather than as a separate stage rule and a
    /// separate readiness rule that could drift apart. A member still in Local Initialisation has no
    /// binding and therefore no signal, which is how a release that skipped Interconnection is
    /// caught: releasing one would admit ordinary interaction against a provider that never reported
    /// its establishment Outcome.
    member this.Release() : PortableResult<unit> =
        match stage with
        | CompositionStage.Interconnected host when this.IsReady ->
            stage <- CompositionStage.Released host
            Ok()
        | CompositionStage.Released _
        | CompositionStage.Retired _ ->
            stateViolation
                "illegal-stage"
                $"'release' is illegal for binding scope '{BindingScopeId.value requirement.Scope}' in stage '{CompositionStage.token stage}'."
        | other ->
            stateViolation
                "release-before-ready"
                $"Binding scope '{BindingScopeId.value requirement.Scope}' has no readiness signal in stage '{CompositionStage.token other}', so its ordinary-interaction gate stays closed."

    /// One ordinary interaction, admitted only after Release.
    member _.Invoke
        (
            operation: PortableOperationRef,
            inputShape: PortableShapeRef,
            input: PortableValue,
            authority: PortableConstraint,
            ?resources: PortableResource list
        ) : Task<PortableResult<InteractionResult>> =
        task {
            match stage with
            | CompositionStage.Released host ->
                let! result = host.Invoke(operation, inputShape, input, authority, ?resources = resources)
                return Ok result

            // The gate is closed in every other stage, and retirement closes it rather than opening a
            // third behaviour. Both refusals report the plan the interaction would have run under.
            | CompositionStage.Interconnected host
            | CompositionStage.Retired(host, _) -> return Ok(refusedByGate host)
            | CompositionStage.LocalInitialisation ->
                // There is no established binding here, so there is no plan to observe against.
                return
                    stateViolation
                        "gate-closed"
                        $"Binding scope '{BindingScopeId.value requirement.Scope}' has no established binding, so no ordinary interaction can start."
        }

    /// Withdrawal and termination, producing the record a replacement generation is told.
    member _.Retire(reason: string) : Task<PortableResult<ReplacementRecord>> =
        task {
            match stage, answeringProvider with
            | (CompositionStage.Interconnected host | CompositionStage.Released host), Some provider ->
                // Authority and composition withdrawal must close the local ordinary-interaction
                // gate before peer cleanup. A peer refusal cannot be allowed to restore access.
                let closedRecord =
                    { Scope = requirement.Scope
                      RetiredPlan = BindingPlan.planId host.Plan
                      Component = BindingPlan.component' host.Plan
                      Provider = provider
                      TerminalState = "failed"
                      Reason = reason
                      ReplacementPermitted = false }

                stage <- CompositionStage.Retired(host, closedRecord)
                let! withdrawn = host.Withdraw()
                let! terminated = host.Terminate()

                match withdrawn, terminated with
                | Error error, _
                | _, Error error -> return Error error
                | Ok(), Ok() ->
                    let record =
                        { Scope = requirement.Scope
                          RetiredPlan = BindingPlan.planId host.Plan
                          Component = BindingPlan.component' host.Plan
                          Provider = provider
                          TerminalState = LifecycleState.token host.State
                          Reason = reason
                          // A failed binding leaves this seam no account of the provider's state, so
                          // replacement is permitted only after a clean end.
                          ReplacementPermitted = host.State = LifecycleState.Terminated }

                    stage <- CompositionStage.Retired(host, record)
                    return Ok record
            | _ ->
                return
                    stateViolation
                        "illegal-stage"
                        $"'retire' is illegal for binding scope '{BindingScopeId.value requirement.Scope}' in stage '{CompositionStage.token stage}'."
        }

    member _.Close() =
        CompositionStage.tryBinding stage |> Option.iter _.Close()

/// The narrow seam by which a resolved Component requirement and an offered provision produce a
/// Binding Plan during activation preflight.
///
/// Everything the seam refuses is refused on purpose. Discovery, acquisition, provider selection
/// policy, resolved generations, mediation, and hot swap belong to the Component Management
/// programme; a seam that approximated any of them would make that programme's decisions here,
/// invisibly.
[<RequireQualifiedAccess>]
module PortableCompositionHandoff =

    /// Local Initialisation and the frameless part of preflight: the member exists, holds its
    /// resolution, and has no peer relationship, no conversation, and no plan.
    ///
    /// Every check here runs before a conversation exists, so a refusal emits no frame and starts no
    /// provider. That is structural rather than asserted: there is nothing here to emit through.
    let prepare
        (requirement: ResolvedRequirement)
        (provision: OfferedProvision)
        (required: ContractDocument)
        : PortableResult<CompositionMember> =
        portable {
            do!
                ensure (provision.Component = requirement.Component) (fun () ->
                    unsupportedContract
                        "component-mismatch"
                        $"The resolution requires {PortableComponentRef.text requirement.Component} but the offered provision provides {PortableComponentRef.text provision.Component}.")

            // A host whose own declaration disagrees with its resolution is refused here rather than
            // at negotiation, where the diagnostic would name the peer for a local inconsistency.
            do!
                ensure (required.Component = requirement.Component) (fun () ->
                    unsupportedContract
                        "declaration-mismatch"
                        $"The required contract document declares {PortableComponentRef.text required.Component}, which is not the resolved {PortableComponentRef.text requirement.Component}.")

            do!
                ensure
                    (match requirement.RequiredProvider with
                     | Some selected -> provision.Provider = selected
                     | None -> true)
                    (fun () ->
                        unsupportedContract
                            "provider-not-selected"
                            $"The resolution requires a provider the offered provision {PortableProviderRef.text provision.Provider} is not; selecting a compatible substitute is resolution work.")

            do!
                ensure (ProviderCardinality.isOneToOne requirement.Cardinality) (fun () ->
                    unsupportedContract
                        "cardinality-unsupported"
                        $"Cardinality {ProviderCardinality.text requirement.Cardinality} is outside version 0.1, which binds exactly one provider; it is refused rather than narrowed to a first member.")

            do!
                ensure (requirement.Exposure = Exposure.Distinct) (fun () ->
                    unsupportedContract
                        "exposure-unsupported"
                        $"Exposure '{Exposure.token requirement.Exposure}' is outside version 0.1; an erased Mediation would still carry provenance, deputy, and authority obligations.")

            return CompositionMember.Create(requirement, provision, required)
        }
