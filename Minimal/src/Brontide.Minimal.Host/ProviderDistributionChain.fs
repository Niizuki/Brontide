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
      /// The launch decision was taken against the policy in force and admitted the publisher.
      Revalidated: bool
      /// The policy that authorized acquisition, absent when trust was never evaluated.
      AcquisitionPolicyIdentity: ProviderPublisherTrustPolicyId option
      /// The policy the launch decision was taken against, absent when the chain never reached it.
      /// It may differ from the acquisition policy without refusing anything: what must still hold
      /// is the decision, not the snapshot that produced it.
      LaunchPolicyIdentity: ProviderPublisherTrustPolicyId option
      /// The authority whose policy governed both trust decisions.
      PolicyAuthorityIdentity: ProviderPublisherTrustPolicyAuthorityId option
      /// The evidence verified inside the chain and retained for serving revalidation.
      VerifiedEvidence: VerifiedProviderPublisherEvidence option
      Provider: StagedProviderProcess option
      StagedIdentity: ProviderArtifactSetId option
      /// The verified path the launched provider actually ran from, inside the store.
      StagedExecutablePath: string option }
    member this.IsLaunched = this.Provider.IsSome

/// Runs the distribution slices as one path: publisher evidence, host trust policy, governed
/// acquisition, content-addressed staging, a launch decision against the policy in force, and a
/// launched provider process holding a removal lease. It reclassifies no refusal - each one keeps
/// the code and the origin of the slice that made it.
[<RequireQualifiedAccess>]
module ProviderDistributionChain =
    /// The three flags are consecutive ladder stages in order - authorized, staged, revalidated -
    /// so a call site reads as how far the chain got before it refused.
    let private refused code refusedBy authorized staged revalidated acquisitionPolicy launchPolicy =
        { Code = code; RefusedBy = refusedBy; Authorized = authorized; Staged = staged
          Revalidated = revalidated
          AcquisitionPolicyIdentity = acquisitionPolicy
          LaunchPolicyIdentity = launchPolicy
          PolicyAuthorityIdentity = None
          VerifiedEvidence = None
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
        | None -> refused "publisher-trust-policy-unavailable" "cbi37" false false false None None
        | Some current ->
            let evidence = ProviderArtifactPublisherEvidenceVerifier.verify request.Acquisition request.Evidence
            match evidence.Verified with
            | None -> refused evidence.Code "cbi34" false false false None None
            | Some verified ->
                // This gate is about attribution rather than protection: the governed acquirer
                // refuses a missing authorization anyway, but it reports "trust required" where the
                // policy actually said the publisher key was revoked or unknown, and the chain owes
                // a host the real reason.
                let trust = ProviderPublisherTrustEvaluator.evaluate current.Policy (Some verified)
                match trust.Authorization with
                | None -> refused trust.Code "cbi35" false false false None None
                | Some authorization ->
                    let authorizingPolicy = Some authorization.PolicyIdentity
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
                            true false false authorizingPolicy None
                    | Some staged ->
                        // The launch is a second effect and takes its own decision rather than
                        // spending the one that authorized acquisition. The registry advances and
                        // never clears, so a policy present before acquisition is present now; what
                        // may have changed is what it says. Comparing the two policy identities
                        // instead would refuse every update that left this publisher alone.
                        let atLaunch = registry.Current |> Option.defaultValue current
                        let relaunch = ProviderPublisherTrustEvaluator.evaluate atLaunch.Policy (Some verified)
                        match relaunch.Authorization with
                        | None ->
                            // Decided before the store touches the artifact, so a lapsed publisher is
                            // never reported as whatever the staged bytes happened to look like.
                            // Removing them is residue hygiene rather than a security act: they are
                            // content-addressed, and integrity is not what lapsed.
                            store.Remove staged.Identity |> ignore
                            refused relaunch.Code "cbi35" true true false authorizingPolicy
                                (Some atLaunch.Policy.Identity)
                        | Some launchAuthorization ->
                            let launchPolicy = Some launchAuthorization.PolicyIdentity
                            // Activation re-verifies the staged bytes under their content address, so
                            // the executable that runs is the one the publisher signed rather than a
                            // path the caller named.
                            match store.Activate(staged, request.AllowedArguments) with
                            | StagedProviderActivation.Refused failure ->
                                store.Remove staged.Identity |> ignore
                                refused failure.Code "cbi31" true true true authorizingPolicy launchPolicy
                            | StagedProviderActivation.Launched provider ->
                                { Code = "provider-launched"; RefusedBy = "none"; Authorized = true
                                  Staged = true; Revalidated = true
                                  AcquisitionPolicyIdentity = authorizingPolicy
                                  LaunchPolicyIdentity = launchPolicy
                                  PolicyAuthorityIdentity = Some registry.AuthorityIdentity
                                  VerifiedEvidence = Some verified
                                  Provider = Some provider
                                  StagedIdentity = Some staged.Identity
                                  StagedExecutablePath =
                                    Path.GetFullPath(Path.Combine(staged.RootPath, staged.ExecutablePath)) |> Some }
