namespace Brontide.Minimal.Host

open System

type TrustedProviderArtifactAcquisitionResult =
    { TrustCode: string
      EvidenceCode: string
      PolicyIdentity: ProviderPublisherTrustPolicyId option
      PublisherKeyId: ProviderPublisherKeyId option
      SourceIdentity: ProviderArtifactSourceId
      TransportCode: string
      AdmissionCode: string
      Staged: StagedProviderArtifactSet option }
    member this.IsStaged = this.Staged.IsSome

type TrustedProviderArtifactAcquirer(acquirer: ProviderArtifactAcquirer) =
    let refused trustCode evidenceCode source policy key transport =
        { TrustCode = trustCode
          EvidenceCode = evidenceCode
          PolicyIdentity = policy
          PublisherKeyId = key
          SourceIdentity = source
          TransportCode = transport
          AdmissionCode = "admission-not-attempted"
          Staged = None }

    do if isNull (box acquirer) then nullArg (nameof acquirer)

    member _.Acquire(
        request: ProviderArtifactAcquisitionRequest,
        source: IProviderArtifactSource,
        authorization: TrustedProviderPublisherAuthorization option) =
        if isNull (box source) then nullArg (nameof source)
        if not (ProviderArtifactPublisherManifest.tryValidate request) then
            refused "publisher-trust-not-evaluated" "publisher-evidence-not-evaluated"
                request.ExpectedSource None None "acquisition-invalid"
        else
            match authorization with
            | None ->
                refused "publisher-trust-required" "publisher-evidence-not-evaluated"
                    request.ExpectedSource None None "transport-not-attempted"
            | Some trusted when trusted.ContentIdentity <> request.Identity ->
                refused "publisher-authorization-content-mismatch" "publisher-evidence-valid"
                    request.ExpectedSource (Some trusted.PolicyIdentity) (Some trusted.PublisherKeyId)
                    "transport-not-attempted"
            | Some trusted when trusted.PayloadSha256 <> ProviderArtifactPublisherManifest.digest request ->
                refused "publisher-authorization-payload-mismatch" "publisher-evidence-valid"
                    request.ExpectedSource (Some trusted.PolicyIdentity) (Some trusted.PublisherKeyId)
                    "transport-not-attempted"
            | Some trusted ->
                let acquisition = acquirer.Acquire(request, source)
                { TrustCode = "publisher-trusted"
                  EvidenceCode = "publisher-evidence-valid"
                  PolicyIdentity = Some trusted.PolicyIdentity
                  PublisherKeyId = Some trusted.PublisherKeyId
                  SourceIdentity = acquisition.SourceIdentity
                  TransportCode = acquisition.TransportCode
                  AdmissionCode = acquisition.AdmissionCode
                  Staged = acquisition.Staged }
