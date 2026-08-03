namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text

[<StructuralEquality; StructuralComparison>]
type ProviderPublisherTrustPolicyId = private ProviderPublisherTrustPolicyId of string

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyId =
    let create value =
        let valid =
            not (String.IsNullOrEmpty value)
            && value.Length = 64
            && value |> Seq.forall (fun character ->
                (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))
        if not valid then invalidArg (nameof value) "A publisher trust policy identity must be an uppercase SHA-256 digest."
        ProviderPublisherTrustPolicyId value

    let value (ProviderPublisherTrustPolicyId value) = value

type ProviderPublisherTrustDisposition =
    | Admitted
    | Revoked

type ProviderPublisherTrustEntry =
    { PublisherKeyId: ProviderPublisherKeyId
      Disposition: ProviderPublisherTrustDisposition }

type ProviderPublisherTrustPolicy =
    { Identity: ProviderPublisherTrustPolicyId
      Entries: ProviderPublisherTrustEntry list }

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyIdentity =
    let private appendInt32 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> sizeof<int>
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        output.Write bytes

    let private appendString output (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        appendInt32 output bytes.Length
        output.Write bytes

    let compute entries =
        use output = new MemoryStream()
        appendString output "CBI35"
        appendInt32 output (List.length entries)
        entries
        |> List.sortBy (fun entry -> ProviderPublisherKeyId.value entry.PublisherKeyId)
        |> List.iter (fun entry ->
            appendString output (ProviderPublisherKeyId.value entry.PublisherKeyId)
            appendString output (match entry.Disposition with Admitted -> "admitted" | Revoked -> "revoked"))
        output.ToArray() |> SHA256.HashData |> Convert.ToHexString |> ProviderPublisherTrustPolicyId.create

type TrustedProviderPublisherAuthorization =
    { PolicyIdentity: ProviderPublisherTrustPolicyId
      PublisherKeyId: ProviderPublisherKeyId
      ContentIdentity: ProviderArtifactSetId
      PayloadSha256: string }

type ProviderPublisherTrustResult =
    { Code: string
      EvidenceCode: string
      PolicyIdentity: ProviderPublisherTrustPolicyId
      PublisherKeyId: ProviderPublisherKeyId option
      ContentIdentity: ProviderArtifactSetId option
      Authorization: TrustedProviderPublisherAuthorization option
      AdmissionCode: string }
    member this.IsTrusted = this.Authorization.IsSome

[<RequireQualifiedAccess>]
module ProviderPublisherTrustEvaluator =
    let private refused code evidenceCode (policy: ProviderPublisherTrustPolicy)
                        (keyId: ProviderPublisherKeyId option) (contentId: ProviderArtifactSetId option) =
        { Code = code
          EvidenceCode = evidenceCode
          PolicyIdentity = policy.Identity
          PublisherKeyId = keyId
          ContentIdentity = contentId
          Authorization = None
          AdmissionCode = "admission-not-attempted" }

    let evaluate (policy: ProviderPublisherTrustPolicy) (evidence: VerifiedProviderPublisherEvidence option) =
        let evidenceCode = if Option.isSome evidence then "publisher-evidence-valid" else "publisher-evidence-not-verified"
        let entries = policy.Entries |> List.ofSeq
        let structurallyValid =
            not (List.isEmpty entries)
            && (entries |> List.map _.PublisherKeyId |> List.distinct |> List.length) = entries.Length
            && ProviderPublisherTrustPolicyIdentity.compute entries = policy.Identity
        if not structurallyValid then
            refused "publisher-trust-policy-invalid" evidenceCode policy
                (evidence |> Option.map _.PublisherKeyId)
                (evidence |> Option.map _.ContentIdentity)
        else
            match evidence with
            | None -> refused "publisher-evidence-not-verified" evidenceCode policy None None
            | Some verified ->
                match entries |> List.tryFind (fun entry -> entry.PublisherKeyId = verified.PublisherKeyId) with
                | None ->
                    refused "publisher-key-unknown" evidenceCode policy
                        (Some verified.PublisherKeyId) (Some verified.ContentIdentity)
                | Some entry when entry.Disposition = Revoked ->
                    refused "publisher-key-revoked" evidenceCode policy
                        (Some verified.PublisherKeyId) (Some verified.ContentIdentity)
                | Some _ ->
                    let authorization: TrustedProviderPublisherAuthorization =
                        { PolicyIdentity = policy.Identity
                          PublisherKeyId = verified.PublisherKeyId
                          ContentIdentity = verified.ContentIdentity
                          PayloadSha256 = verified.PayloadSha256 }
                    { Code = "publisher-trusted"
                      EvidenceCode = evidenceCode
                      PolicyIdentity = policy.Identity
                      PublisherKeyId = Some verified.PublisherKeyId
                      ContentIdentity = Some verified.ContentIdentity
                      Authorization = Some authorization
                      AdmissionCode = "admission-not-attempted" }
