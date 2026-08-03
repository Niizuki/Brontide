namespace Brontide.Minimal.Host

open System
open System.Buffers.Binary
open System.IO
open System.Security.Cryptography
open System.Text

[<StructuralEquality; StructuralComparison>]
type ProviderPublisherKeyId = private ProviderPublisherKeyId of string

[<RequireQualifiedAccess>]
module ProviderPublisherKeyId =
    let create value =
        let valid =
            not (String.IsNullOrEmpty value)
            && value.Length = 64
            && value |> Seq.forall (fun character ->
                (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))
        if not valid then invalidArg (nameof value) "A publisher key identity must be an uppercase SHA-256 digest."
        ProviderPublisherKeyId value

    let value (ProviderPublisherKeyId value) = value

type ProviderPublisherEvidence =
    { PublisherKeyId: ProviderPublisherKeyId
      Algorithm: string
      PublicKeySpkiBase64: string
      SignatureBase64: string }

type VerifiedProviderPublisherEvidence =
    { ContentIdentity: ProviderArtifactSetId
      PublisherKeyId: ProviderPublisherKeyId
      PayloadSha256: string }

type ProviderPublisherEvidenceResult =
    { Code: string
      PayloadSha256: string
      PublisherKeyId: ProviderPublisherKeyId option
      Verified: VerifiedProviderPublisherEvidence option
      TrustCode: string
      AdmissionCode: string }
    member this.IsVerified = this.Verified.IsSome

[<RequireQualifiedAccess>]
module ProviderArtifactPublisherManifest =
    let private safeRelativePath (path: string) =
        not (String.IsNullOrWhiteSpace path)
        && not (Path.IsPathFullyQualified path)
        && not (path.Contains '\\')
        && (path.Split '/' |> Array.forall (fun segment -> segment.Length > 0 && segment <> "." && segment <> ".."))

    let private isDigest (value: string) =
        not (String.IsNullOrEmpty value)
        && value.Length = 64
        && value |> Seq.forall (fun character ->
            (character >= '0' && character <= '9') || (character >= 'A' && character <= 'F'))

    let internal tryValidate (request: ProviderArtifactAcquisitionRequest) =
        let structural =
            not (List.isEmpty request.Files)
            && not (String.IsNullOrWhiteSpace(ProviderArtifactSourceId.value request.ExpectedSource))
            && request.MaxTotalBytes > 0L
            && not (request.Arguments |> List.exists String.IsNullOrEmpty)
            && safeRelativePath request.ExecutablePath
            && (request.Files |> List.forall (fun file ->
                safeRelativePath file.RelativePath && isDigest file.Sha256 && file.Length >= 0L))
            && (request.Files |> List.map _.RelativePath |> List.distinct |> List.length) = request.Files.Length
            && (request.Files |> List.exists (fun file -> file.RelativePath = request.ExecutablePath))
        if not structural then false
        else
            let withinLimit, _ =
                request.Files
                |> List.fold (fun (valid, total) file ->
                    if not valid || file.Length > request.MaxTotalBytes - total then false, total
                    else true, total + file.Length) (true, 0L)
            let artifactFiles = request.Files |> List.map (fun file ->
                { RelativePath = file.RelativePath; Sha256 = file.Sha256 }: ProviderArtifactFile)
            withinLimit
            && ProviderArtifactSetIdentity.compute artifactFiles request.ExecutablePath request.Arguments = request.Identity

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

    let encode (request: ProviderArtifactAcquisitionRequest) =
        if not (tryValidate request) then invalidArg (nameof request) "The acquisition request is not a canonical publisher manifest."
        use output = new MemoryStream()
        appendString output "CBI34"
        appendString output (ProviderArtifactSetId.value request.Identity)
        appendInt32 output request.Files.Length
        for file in request.Files |> List.sortBy _.RelativePath do
            appendString output file.RelativePath
            appendString output file.Sha256
            appendInt64 output file.Length
        appendString output request.ExecutablePath
        appendInt32 output request.Arguments.Length
        request.Arguments |> List.iter (appendString output)
        output.ToArray()

    let digest request = encode request |> SHA256.HashData |> Convert.ToHexString

[<RequireQualifiedAccess>]
module ProviderArtifactPublisherEvidenceVerifier =
    let private refuse code payload keyId =
        { Code = code
          PayloadSha256 = payload
          PublisherKeyId = keyId
          Verified = None
          TrustCode = "publisher-trust-not-evaluated"
          AdmissionCode = "admission-not-attempted" }

    let verify (request: ProviderArtifactAcquisitionRequest) (evidence: ProviderPublisherEvidence option) =
        if not (ProviderArtifactPublisherManifest.tryValidate request) then
            refuse "publisher-evidence-request-invalid" String.Empty None
        else
            let payload = ProviderArtifactPublisherManifest.encode request
            let payloadDigest = SHA256.HashData payload |> Convert.ToHexString
            match evidence with
            | None -> refuse "publisher-evidence-not-provided" payloadDigest None
            | Some value when value.Algorithm <> "ECDSA-P256-SHA256" ->
                refuse "publisher-evidence-unsupported" payloadDigest (Some value.PublisherKeyId)
            | Some value ->
                try
                    let publicKey = Convert.FromBase64String value.PublicKeySpkiBase64
                    let signature = Convert.FromBase64String value.SignatureBase64
                    let computedKeyId = SHA256.HashData publicKey |> Convert.ToHexString |> ProviderPublisherKeyId.create
                    if computedKeyId <> value.PublisherKeyId then
                        refuse "publisher-evidence-malformed" payloadDigest (Some value.PublisherKeyId)
                    else
                        use verifier = ECDsa.Create()
                        let mutable bytesRead = 0
                        verifier.ImportSubjectPublicKeyInfo(publicKey.AsSpan(), &bytesRead)
                        let parameters = verifier.ExportParameters false
                        let curveOid = parameters.Curve.Oid.Value |> Option.ofObj
                        if bytesRead <> publicKey.Length || verifier.KeySize <> 256 || curveOid <> Some "1.2.840.10045.3.1.7" then
                            refuse "publisher-evidence-malformed" payloadDigest (Some value.PublisherKeyId)
                        elif not (verifier.VerifyData(
                            payload,
                            signature,
                            HashAlgorithmName.SHA256,
                            DSASignatureFormat.Rfc3279DerSequence)) then
                            refuse "publisher-evidence-invalid" payloadDigest (Some computedKeyId)
                        else
                            let verified =
                                { ContentIdentity = request.Identity
                                  PublisherKeyId = computedKeyId
                                  PayloadSha256 = payloadDigest }
                            { Code = "publisher-evidence-valid"
                              PayloadSha256 = payloadDigest
                              PublisherKeyId = Some computedKeyId
                              Verified = Some verified
                              TrustCode = "publisher-trust-not-evaluated"
                              AdmissionCode = "admission-not-attempted" }
                with
                | :? FormatException
                | :? CryptographicException
                | :? ArgumentException ->
                    refuse "publisher-evidence-malformed" payloadDigest (Some value.PublisherKeyId)
