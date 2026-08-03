namespace Brontide.Minimal.Host

open System.IO

type ProviderDistributionChainRequest =
    { Acquisition: ProviderArtifactAcquisitionRequest
      Evidence: ProviderPublisherEvidence option
      AllowedArguments: string list }

type ProviderDistributionChainResult =
    { Code: string
      /// The slice that produced the refusal, so a composed failure keeps its origin.
      RefusedBy: string
      Authorized: bool
      Staged: bool
      Provider: StagedProviderProcess option
      StagedIdentity: ProviderArtifactSetId option
      /// The verified path the launched provider actually ran from, inside the store.
      StagedExecutablePath: string option }
    member this.IsLaunched = this.Provider.IsSome

/// Runs the distribution slices as one path: publisher evidence, host trust policy, governed
/// acquisition, content-addressed staging, and a launched provider process holding a removal lease.
/// It adds no capability of its own and reclassifies no refusal - each one keeps the code and the
/// origin of the slice that made it.
[<RequireQualifiedAccess>]
module ProviderDistributionChain =
    let private refused code refusedBy authorized staged =
        { Code = code; RefusedBy = refusedBy; Authorized = authorized; Staged = staged
          Provider = None; StagedIdentity = None; StagedExecutablePath = None }

    let run
        (registry: DurableProviderPublisherTrustPolicyRegistry)
        (store: ContentAddressedProviderStore)
        (transactionRoot: string)
        (request: ProviderDistributionChainRequest)
        (source: IProviderArtifactSource) =
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box store) then nullArg (nameof store)
        if isNull (box source) then nullArg (nameof source)

        // A policy that never arrived authorizes nothing, and the chain stops before any evidence is
        // weighed or any source is opened.
        match registry.Current with
        | None -> refused "publisher-trust-policy-unavailable" "cbi37" false false
        | Some current ->
            let evidence = ProviderArtifactPublisherEvidenceVerifier.verify request.Acquisition request.Evidence
            match evidence.Verified with
            | None -> refused evidence.Code "cbi34" false false
            | Some verified ->
                // This gate is about attribution rather than protection: the governed acquirer
                // refuses a missing authorization anyway, but it reports "trust required" where the
                // policy actually said the publisher key was revoked or unknown, and the chain owes
                // a host the real reason.
                let trust = ProviderPublisherTrustEvaluator.evaluate current.Policy (Some verified)
                match trust.Authorization with
                | None -> refused trust.Code "cbi35" false false
                | Some authorization ->
                    let acquirer =
                        TrustedProviderArtifactAcquirer(ProviderArtifactAcquirer(store, transactionRoot))
                    let acquired = registry.Govern(acquirer).Acquire(request.Acquisition, source, Some authorization)
                    match acquired.Staged with
                    | None ->
                        // CBI33 keeps transport completion and local admission apart, and the chain
                        // preserves that: delivery that completed and then failed its digest is
                        // CBI32 refusing admission, not the transport failing.
                        let delivered = acquired.TransportCode = "transport-completed"
                        refused
                            (if delivered then acquired.AdmissionCode else acquired.TransportCode)
                            (if delivered then "cbi32" else "cbi33")
                            true false
                    | Some staged ->
                        // Activation re-verifies the staged bytes under their content address, so the
                        // executable that runs is the one the publisher signed rather than a path the
                        // caller named.
                        match store.Activate(staged, request.AllowedArguments) with
                        | StagedProviderActivation.Refused failure ->
                            store.Remove staged.Identity |> ignore
                            refused failure.Code "cbi31" true true
                        | StagedProviderActivation.Launched provider ->
                            { Code = "provider-launched"; RefusedBy = "none"; Authorized = true
                              Staged = true; Provider = Some provider
                              StagedIdentity = Some staged.Identity
                              StagedExecutablePath =
                                Path.GetFullPath(Path.Combine(staged.RootPath, staged.ExecutablePath)) |> Some }
