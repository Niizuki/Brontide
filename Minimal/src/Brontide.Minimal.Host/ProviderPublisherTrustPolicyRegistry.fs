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

/// One CBI57 authority transition. It carries both signatures because the predecessor authorizes the
/// transition and the successor proves its own key exists; a statement is meaningless without both.
type ProviderPolicyAuthorityRotationStatement =
    { Generation: int64
      PolicySequence: int64
      PolicyIdentity: ProviderPublisherTrustPolicyId option
      PreviousAuthority: ProviderPublisherTrustPolicyAuthorityId
      NextAuthority: ProviderPublisherTrustPolicyAuthorityId
      Algorithm: string
      PreviousAuthorityPublicKeySpkiBase64: string
      NextAuthorityPublicKeySpkiBase64: string
      PreviousSignatureBase64: string
      NextSignatureBase64: string }

[<RequireQualifiedAccess>]
module ProviderPolicyAuthorityRotationManifest =
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

    let encode generation policySequence policyIdentity previousAuthority nextAuthority =
        if generation <= 0L || policySequence < 0L then
            invalidArg (nameof generation) "A valid policy authority transition is required."
        use output = new MemoryStream()
        appendString output "CBI57"
        appendInt64 output generation
        appendInt64 output policySequence
        appendInt32 output (if Option.isSome policyIdentity then 1 else 0)
        policyIdentity |> Option.iter (ProviderPublisherTrustPolicyId.value >> appendString output)
        appendString output (ProviderPublisherTrustPolicyAuthorityId.value previousAuthority)
        appendString output (ProviderPublisherTrustPolicyAuthorityId.value nextAuthority)
        output.ToArray()

type ProviderPolicyAuthorityRotationResult =
    { Code: string
      Generation: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId }
    member this.IsApplied = this.Code = "policy-authority-rotation-applied"

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
    let mutable activeAuthority = authorityIdentity
    let mutable authorityGeneration = 0L

    let identify (publicKey: byte array) =
        SHA256.HashData publicKey |> Convert.ToHexString |> ProviderPublisherTrustPolicyAuthorityId.create

    /// Imports P-256 key material and reports whether the signature holds. `None` distinguishes key
    /// material that is not exactly P-256 SPKI from a signature that simply does not verify.
    let tryVerify (publicKey: byte array) (manifest: byte array) (signatureBase64: string) =
        let signature = Convert.FromBase64String signatureBase64
        use verifier = ECDsa.Create()
        let mutable bytesRead = 0
        verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
        let curveOid = verifier.ExportParameters(false).Curve.Oid.Value |> Option.ofObj
        if bytesRead <> publicKey.Length || verifier.KeySize <> 256 || curveOid <> Some "1.2.840.10045.3.1.7" then None
        else
            Some(verifier.VerifyData(
                manifest, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))

    let validateRotation (statement: ProviderPolicyAuthorityRotationStatement) =
        if statement.Generation <> authorityGeneration + 1L then Some "policy-authority-generation-invalid"
        elif statement.PreviousAuthority <> activeAuthority then Some "policy-authority-predecessor-mismatch"
        elif statement.NextAuthority = statement.PreviousAuthority then Some "policy-authority-self-refused"
        elif statement.PolicySequence <> (current |> Option.map _.Sequence |> Option.defaultValue 0L)
            || statement.PolicyIdentity <> (current |> Option.map _.Policy.Identity) then
            Some "policy-authority-chain-mismatch"
        elif statement.Algorithm <> "ECDSA-P256-SHA256"
            || String.IsNullOrWhiteSpace statement.PreviousAuthorityPublicKeySpkiBase64
            || String.IsNullOrWhiteSpace statement.NextAuthorityPublicKeySpkiBase64
            || String.IsNullOrWhiteSpace statement.PreviousSignatureBase64
            || String.IsNullOrWhiteSpace statement.NextSignatureBase64 then
            Some "policy-authority-evidence-invalid"
        else
            try
                let previousKey = Convert.FromBase64String statement.PreviousAuthorityPublicKeySpkiBase64
                let nextKey = Convert.FromBase64String statement.NextAuthorityPublicKeySpkiBase64
                if identify previousKey <> statement.PreviousAuthority
                    || identify nextKey <> statement.NextAuthority then Some "policy-authority-key-mismatch"
                else
                    let manifest =
                        ProviderPolicyAuthorityRotationManifest.encode statement.Generation
                            statement.PolicySequence statement.PolicyIdentity
                            statement.PreviousAuthority statement.NextAuthority
                    match tryVerify previousKey manifest statement.PreviousSignatureBase64,
                          tryVerify nextKey manifest statement.NextSignatureBase64 with
                    | None, _ | _, None -> Some "policy-authority-evidence-invalid"
                    | Some false, _ -> Some "policy-authority-signature-invalid"
                    | Some true, Some false -> Some "policy-authority-successor-unproven"
                    | Some true, Some true -> None
            with
            | :? FormatException
            | :? CryptographicException
            | :? ArgumentException -> Some "policy-authority-evidence-invalid"

    let validate (update: ProviderPublisherTrustPolicyUpdate) =
        match ProviderPublisherTrustEvaluator.tryValidate update.Policy with
        | None -> Error "policy-update-policy-invalid"
        | Some policy when update.Sequence <= 0L -> Error "policy-update-policy-invalid"
        | Some _ when update.Algorithm <> "ECDSA-P256-SHA256" -> Error "policy-update-unsupported"
        | Some policy ->
            try
                let publicKey = Convert.FromBase64String update.AuthorityPublicKeySpkiBase64
                let signature = Convert.FromBase64String update.SignatureBase64
                if identify publicKey <> activeAuthority then Error "policy-update-authority-mismatch"
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

    /// The out-of-band pin. It never moves, which is what lets a chain recorded against it stay
    /// comparable across an authority rotation.
    member internal _.AuthorityIdentity = authorityIdentity

    /// The authority that may sign the next policy update, which a rotation moves.
    member _.ActiveAuthorityIdentity = lock syncRoot (fun () -> activeAuthority)
    member _.AuthorityGeneration = lock syncRoot (fun () -> authorityGeneration)

    member _.Rotate(statement: ProviderPolicyAuthorityRotationStatement) =
        if isNull (box statement) then nullArg (nameof statement)
        lock syncRoot (fun () ->
            match validateRotation statement with
            | Some code -> { Code = code; Generation = authorityGeneration; ActiveAuthority = activeAuthority }
            | None ->
                activeAuthority <- statement.NextAuthority
                authorityGeneration <- statement.Generation
                { Code = "policy-authority-rotation-applied"
                  Generation = authorityGeneration
                  ActiveAuthority = activeAuthority })

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
                    // The snapshot names the pin rather than the signing key: it is the trust root the
                    // policy was verified under, and every downstream comparison of it must survive a
                    // rotation.
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
