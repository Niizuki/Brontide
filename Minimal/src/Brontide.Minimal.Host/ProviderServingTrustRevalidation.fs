namespace Brontide.Minimal.Host

open System
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement

type ProviderServingTrustRevalidationResult =
    { Code: string
      RefusedBy: string
      Revalidated: bool
      Continued: bool
      LaunchPolicyIdentity: ProviderPublisherTrustPolicyId option
      ServingPolicyIdentity: ProviderPublisherTrustPolicyId option
      Authorization: TrustedProviderPublisherAuthorization option
      RetirementCode: string }

type ProviderServingActivation =
    private
        { Chain: ProviderDistributionChainResult
          Lifecycle: ComponentBindingLifecycleResult option }
    member this.IsServing =
        this.Chain.Provider |> Option.exists (fun provider -> not provider.HasExited)
        && this.Lifecycle |> Option.exists (fun lifecycle ->
            lifecycle.Failure.IsNone
            && lifecycle.Member |> Option.exists _.IsReleased
            && lifecycle.Runtime |> Option.exists (fun runtime -> runtime.Kind = ActivationRuntimeOutcomeKind.Active))
    member this.MemberReleased =
        this.Lifecycle |> Option.bind _.Member |> Option.exists _.IsReleased
    member this.Retire reason =
        task {
            if String.IsNullOrWhiteSpace reason then invalidArg (nameof reason) "retirement reason is required"
            match this.Lifecycle |> Option.bind _.Member with
            | Some memberValue when memberValue.IsReleased ->
                let! _ = memberValue.Retire reason
                return ()
            | _ -> return ()
        }

/// Takes one current publisher-trust decision for an already released portable member. A lapsed
/// publisher is retired and its concrete provider is terminated; cadence remains a host concern.
[<RequireQualifiedAccess>]
module ProviderServingTrustRevalidation =
    let activate
        (chain: ProviderDistributionChainResult)
        resolution
        selection
        request
        =
        task {
            match chain.Provider with
            | Some provider when chain.Revalidated ->
                let! lifecycle =
                    ComponentBindingLifecycle.activate resolution selection request provider.Conversation
                return { Chain = chain; Lifecycle = Some lifecycle }
            | _ -> return { Chain = chain; Lifecycle = None }
        }

    let revalidate
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (activation: ProviderServingActivation)
        retirementReason
        =
        task {
            if isNull (box registry) then nullArg (nameof registry)
            if isNull (box store) then nullArg (nameof store)
            if String.IsNullOrWhiteSpace retirementReason then
                invalidArg (nameof retirementReason) "retirement reason is required"

            let chain = activation.Chain

            match
                chain.Provider,
                chain.VerifiedEvidence,
                chain.StagedIdentity,
                chain.PolicyAuthorityIdentity,
                activation.Lifecycle |> Option.bind _.Member
            with
            | Some provider, Some evidence, Some stagedIdentity, Some authority, Some memberValue
                when chain.Revalidated
                     && authority = registry.AuthorityIdentity
                     && not provider.HasExited
                     && memberValue.IsReleased
                     && activation.IsServing ->
                match registry.Current with
                | None ->
                    return
                        { Code = "publisher-trust-policy-unavailable"; RefusedBy = "cbi37"
                          Revalidated = false; Continued = false
                          LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                          ServingPolicyIdentity = None; Authorization = None
                          RetirementCode = "retirement-not-attempted" }
                | Some current ->
                    let trust = ProviderPublisherTrustEvaluator.evaluate current.Policy (Some evidence)
                    match trust.Authorization with
                    | Some authorization ->
                        return
                            { Code = "publisher-trust-current"; RefusedBy = "none"
                              Revalidated = true; Continued = true
                              LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                              ServingPolicyIdentity = Some current.Policy.Identity
                              Authorization = Some authorization
                              RetirementCode = "retirement-not-attempted" }
                    | None ->
                        let! retired = memberValue.Retire retirementReason
                        let mutable retirementCode =
                            match retired with Ok _ -> "retired" | Error _ -> "retirement-failed"
                        provider.Dispose()
                        let removal = store.Remove stagedIdentity
                        if not removal.Removed && removal.Code <> "artifact-set-not-staged" then
                            retirementCode <- $"{retirementCode};{removal.Code}"
                        return
                            { Code = trust.Code; RefusedBy = "cbi35"
                              Revalidated = true; Continued = false
                              LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                              ServingPolicyIdentity = Some current.Policy.Identity
                              Authorization = None; RetirementCode = retirementCode }
            | _ ->
                return
                    { Code = "serving-activation-unavailable"; RefusedBy = "none"
                      Revalidated = false; Continued = false
                      LaunchPolicyIdentity = chain.LaunchPolicyIdentity
                      ServingPolicyIdentity = None; Authorization = None
                      RetirementCode = "retirement-not-attempted" }
        }
