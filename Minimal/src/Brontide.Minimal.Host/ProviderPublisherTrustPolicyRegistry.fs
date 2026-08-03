namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text

[<StructuralEquality; StructuralComparison>]
type ProviderPublisherTrustPolicyAuthorityId = private ProviderPublisherTrustPolicyAuthorityId of string

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyAuthorityId =
    let create value =
        let valid =
            not (String.IsNullOrEmpty value) && value.Length = 64
            && value |> Seq.forall (fun c -> (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
        if not valid then invalidArg (nameof value) "A policy authority identity must be an uppercase SHA-256 digest."
        ProviderPublisherTrustPolicyAuthorityId value
    let value (ProviderPublisherTrustPolicyAuthorityId value) = value

type ProviderPublisherTrustPolicyUpdate =
    { Sequence: int64
      PreviousPolicyIdentity: ProviderPublisherTrustPolicyId option
      Policy: ProviderPublisherTrustPolicy
      Algorithm: string
      AuthorityPublicKeySpkiBase64: string
      SignatureBase64: string }

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyUpdateManifest =
    let private appendInt32 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> sizeof<int>
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendInt64 (output: Stream) value =
        let bytes = Array.zeroCreate<byte> sizeof<int64>
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(), value)
        output.Write bytes
    let private appendString output (value: string) =
        let bytes = Encoding.UTF8.GetBytes value
        appendInt32 output bytes.Length
        output.Write bytes

    let encode sequence previous policyIdentity =
        if sequence <= 0L then invalidArg (nameof sequence) "A positive policy sequence is required."
        use output = new MemoryStream()
        appendString output "CBI37"
        appendInt64 output sequence
        appendInt32 output (if Option.isSome previous then 1 else 0)
        previous |> Option.iter (ProviderPublisherTrustPolicyId.value >> appendString output)
        appendString output (ProviderPublisherTrustPolicyId.value policyIdentity)
        output.ToArray()

    let digest sequence previous policyIdentity =
        encode sequence previous policyIdentity |> SHA256.HashData |> Convert.ToHexString

type VerifiedProviderPublisherTrustPolicySnapshot =
    private
    | VerifiedProviderPublisherTrustPolicySnapshot of
        ProviderPublisherTrustPolicyAuthorityId * int64 * ProviderPublisherTrustPolicy
    member this.AuthorityIdentity =
        let (VerifiedProviderPublisherTrustPolicySnapshot(authority, _, _)) = this
        authority
    member this.Sequence =
        let (VerifiedProviderPublisherTrustPolicySnapshot(_, sequence, _)) = this
        sequence
    member this.Policy =
        let (VerifiedProviderPublisherTrustPolicySnapshot(_, _, policy)) = this
        policy

type ProviderPublisherTrustPolicyUpdateResult =
    { Code: string
      Current: VerifiedProviderPublisherTrustPolicySnapshot option }
    member this.IsApplied = this.Code = "policy-update-applied"

type ProviderPublisherTrustPolicyRegistry(authorityIdentity: ProviderPublisherTrustPolicyAuthorityId) =
    let syncRoot = obj ()
    let mutable current: VerifiedProviderPublisherTrustPolicySnapshot option = None

    let validate (update: ProviderPublisherTrustPolicyUpdate) =
        match ProviderPublisherTrustEvaluator.tryValidate update.Policy with
        | None -> Error "policy-update-policy-invalid"
        | Some policy when update.Sequence <= 0L -> Error "policy-update-policy-invalid"
        | Some _ when update.Algorithm <> "ECDSA-P256-SHA256" -> Error "policy-update-unsupported"
        | Some policy ->
            try
                let publicKey = Convert.FromBase64String update.AuthorityPublicKeySpkiBase64
                let signature = Convert.FromBase64String update.SignatureBase64
                let suppliedAuthority =
                    SHA256.HashData publicKey |> Convert.ToHexString |> ProviderPublisherTrustPolicyAuthorityId.create
                if suppliedAuthority <> authorityIdentity then Error "policy-update-authority-mismatch"
                else
                    use verifier = ECDsa.Create()
                    let mutable bytesRead = 0
                    verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
                    let curveOid = verifier.ExportParameters(false).Curve.Oid.Value |> Option.ofObj
                    if bytesRead <> publicKey.Length || verifier.KeySize <> 256 || curveOid <> Some "1.2.840.10045.3.1.7" then
                        Error "policy-update-malformed"
                    elif verifier.VerifyData(
                        ProviderPublisherTrustPolicyUpdateManifest.encode
                            update.Sequence update.PreviousPolicyIdentity policy.Identity,
                        signature,
                        HashAlgorithmName.SHA256,
                        DSASignatureFormat.Rfc3279DerSequence) then Ok policy
                    else Error "policy-update-signature-invalid"
            with
            | :? FormatException
            | :? CryptographicException
            | :? ArgumentException -> Error "policy-update-malformed"

    member _.Current = lock syncRoot (fun () -> current)
    member internal _.AuthorityIdentity = authorityIdentity

    member _.Apply(update: ProviderPublisherTrustPolicyUpdate) =
        if isNull (box update) then nullArg (nameof update)
        lock syncRoot (fun () ->
            match validate update with
            | Error code -> { Code = code; Current = current }
            | Ok policy ->
                let chainCode =
                    match current with
                    | None when update.Sequence <> 1L -> Some "policy-update-sequence-invalid"
                    | None when Option.isSome update.PreviousPolicyIdentity -> Some "policy-update-predecessor-mismatch"
                    | Some snapshot when update.Sequence <> snapshot.Sequence + 1L -> Some "policy-update-sequence-invalid"
                    | Some snapshot when update.PreviousPolicyIdentity <> Some snapshot.Policy.Identity ->
                        Some "policy-update-predecessor-mismatch"
                    | _ -> None
                match chainCode with
                | Some code -> { Code = code; Current = current }
                | None ->
                    let snapshot = VerifiedProviderPublisherTrustPolicySnapshot(authorityIdentity, update.Sequence, policy)
                    current <- Some snapshot
                    { Code = "policy-update-applied"; Current = current })

    member internal _.WithCurrent action = lock syncRoot (fun () -> action current)

type GovernedProviderArtifactAcquirer(
    registry: ProviderPublisherTrustPolicyRegistry,
    acquirer: TrustedProviderArtifactAcquirer) =
    do
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box acquirer) then nullArg (nameof acquirer)

    member _.Acquire(
        request: ProviderArtifactAcquisitionRequest,
        source: IProviderArtifactSource,
        authorization: TrustedProviderPublisherAuthorization option) =
        if isNull (box request) then nullArg (nameof request)
        if isNull (box source) then nullArg (nameof source)
        registry.WithCurrent(fun current ->
            match current with
            | None ->
                { TrustCode = "publisher-trust-policy-unavailable"
                  EvidenceCode = "publisher-evidence-not-evaluated"
                  PolicyIdentity = None
                  PublisherKeyId = None
                  SourceIdentity = request.ExpectedSource
                  TransportCode = "transport-not-attempted"
                  AdmissionCode = "admission-not-attempted"
                  Staged = None }
            | Some snapshot when authorization |> Option.exists (fun value -> value.PolicyIdentity <> snapshot.Policy.Identity) ->
                let supplied = authorization.Value
                { TrustCode = "publisher-authorization-superseded"
                  EvidenceCode = "publisher-evidence-valid"
                  PolicyIdentity = Some supplied.PolicyIdentity
                  PublisherKeyId = Some supplied.PublisherKeyId
                  SourceIdentity = request.ExpectedSource
                  TransportCode = "transport-not-attempted"
                  AdmissionCode = "admission-not-attempted"
                  Staged = None }
            | Some _ -> acquirer.Acquire(request, source, authorization))
