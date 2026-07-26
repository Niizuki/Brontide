namespace Brontide.Minimal.Binding.Portable

/// One Channel envelope.
[<StructuralEquality; NoComparison>]
type Envelope =
    { ContractVersion: int
      Kind: EnvelopeKind
      Channel: ChannelId
      Request: ChannelRequestId option
      Execution: ChannelExecutionId option
      Body: CborItem }

[<RequireQualifiedAccess>]
module Envelope =

    let control kind channel body =
        { ContractVersion = ContractDocument.SupportedContractVersion
          Kind = kind
          Channel = channel
          Request = None
          Execution = None
          Body = body }

    let empty kind channel = control kind channel (CborMap [])

    let correlated kind channel request execution body =
        { ContractVersion = ContractDocument.SupportedContractVersion
          Kind = kind
          Channel = channel
          Request = request
          Execution = execution
          Body = body }

/// Strict envelope codec; every miss is a portable category rather than a parser failure.
[<RequireQualifiedAccess>]
module EnvelopeCodec =

    let private envelopeFields =
        [ "contractVersion"; "kind"; "channelId"; "requestId"; "executionId"; "body" ]

    let toItem (envelope: Envelope) =
        CborMap(
            [ "contractVersion", CborInteger(int64 envelope.ContractVersion)
              "kind", CborText(EnvelopeKind.token envelope.Kind)
              "channelId", CborText(ChannelId.value envelope.Channel)
              "body", envelope.Body ]
            @ (match envelope.Request with
               | Some request -> [ "requestId", CborText(ChannelRequestId.value request) ]
               | None -> [])
            @ (match envelope.Execution with
               | Some execution -> [ "executionId", CborText(ChannelExecutionId.value execution) ]
               | None -> [])
        )

    let encode envelope = PortableCbor.encode (toItem envelope)

    let ofItem (item: CborItem) : PortableResult<Envelope> =
        portable {
            let! entries = CborAccess.requireMap item "envelope"

            // Foreign runtime identity is refused before the field set is checked, so the rejection
            // names the rule that actually applies rather than reporting an unknown field.
            do!
                entries
                |> iterate (fun (key, value) ->
                    if key = "body" then
                        Ok()
                    elif PortableForbiddenContent.isForbidden key then
                        malformed "foreign-runtime-data" $"Control field '{key}' carries foreign runtime identity."
                    else
                        PortableForbiddenContent.requireCleanControl value)

            do! CborAccess.requireDeclaredFields entries "envelope" envelopeFields
            let! contractVersion = CborAccess.int32 entries "contractVersion"

            do!
                ensure (contractVersion = ContractDocument.SupportedContractVersion) (fun () ->
                    refuse
                        ProtocolCategory.UnsupportedVersion
                        "envelope-version"
                        $"Envelope contract version {contractVersion} is not recognized.")

            let! kindToken = CborAccess.text entries "kind"
            let! kind = EnvelopeKind.tryParse kindToken
            let! channel = CborAccess.text entries "channelId"
            let! requestText = CborAccess.optionalText entries "requestId"
            let! executionText = CborAccess.optionalText entries "executionId"

            do!
                ensure (not (EnvelopeKind.requiresRequestId kind) || requestText.IsSome) (fun () ->
                    malformed
                        "correlation-absent"
                        $"A '{EnvelopeKind.token kind}' envelope carries a request identity; it is never matched to an outstanding request by position.")

            let! body = CborAccess.field entries "body"

            return
                { ContractVersion = contractVersion
                  Kind = kind
                  Channel = ChannelId.received channel
                  Request = requestText |> Option.map ChannelRequestId.received
                  Execution = executionText |> Option.map ChannelExecutionId.received
                  Body = body }
        }

    let decode (frame: byte array) limits =
        PortableCbor.decode frame limits |> Result.bind ofItem

/// The body of an establish-accepted envelope.
type EstablishAcceptedBody =
    { Contract: ContractDocument
      CompactIdentifiers: CompactAssignment list }

[<RequireQualifiedAccess>]
module EstablishAcceptedBody =

    let encode (body: EstablishAcceptedBody) =
        CborMap
            [ "contract", ContractCodec.encode body.Contract
              "compactIdentifiers",
              CborArray(
                  body.CompactIdentifiers
                  |> List.map (fun assignment ->
                      CborMap
                          [ "space", CborText(IdentitySpace.token assignment.Space)
                            "reference", CborText assignment.Reference
                            "compactId", CborInteger(int64 (CompactId.value assignment.Compact)) ])
              ) ]

    let decode item : PortableResult<EstablishAcceptedBody> =
        portable {
            let! entries = CborAccess.requireMap item "establish-accepted"
            do! CborAccess.requireDeclaredFields entries "establish-accepted" [ "contract"; "compactIdentifiers" ]
            let! contract = CborAccess.field entries "contract" |> Result.bind ContractCodec.decode

            let! assignments =
                CborAccess.arrayOf entries "compactIdentifiers" (fun element ->
                    portable {
                        let! assignment = CborAccess.requireMap element "compactAssignment"
                        do! CborAccess.requireDeclaredFields assignment "compactAssignment" [ "space"; "reference"; "compactId" ]
                        let! spaceToken = CborAccess.text assignment "space"
                        let! space = IdentitySpace.tryParse spaceToken
                        let! reference = CborAccess.text assignment "reference"
                        let! compactValue = CborAccess.int32 assignment "compactId"
                        let! compact = CompactId.tryCreate compactValue

                        return
                            { Space = space
                              Reference = reference
                              Compact = compact }
                    })

            return
                { Contract = contract
                  CompactIdentifiers = assignments }
        }

/// How a request names the Operation it invokes.
///
/// The two designations are exclusive by construction rather than by a pair of optional members
/// that could both be present or both absent on the wire.
[<RequireQualifiedAccess>]
type OperationDesignation =
    | Canonical of PortableOperationRef
    /// A compact identifier this binding assigned. One it never assigned resolves to no canonical
    /// identity, which is an unsupported contract rather than an unknown Operation.
    | Compact of int

/// The body of a request envelope.
type RequestBody =
    { Operation: OperationDesignation
      InputShape: PortableShapeRef
      Input: CborItem
      Resources: CborItem list }

[<RequireQualifiedAccess>]
module RequestBody =

    let private fields = [ "operation"; "compactOperation"; "inputShape"; "input"; "resources" ]

    let encode (body: RequestBody) =
        CborMap(
            [ "inputShape", CborAccess.encodeCanonical (PortableShapeRef.canonical body.InputShape)
              "input", body.Input
              "resources", CborArray body.Resources ]
            @ (match body.Operation with
               | OperationDesignation.Canonical reference ->
                   [ "operation", CborAccess.encodeCanonical (PortableOperationRef.canonical reference) ]
               | OperationDesignation.Compact compact -> [ "compactOperation", CborInteger(int64 compact) ])
        )

    let decode item : PortableResult<RequestBody> =
        portable {
            let! entries = CborAccess.requireMap item "request"
            do! CborAccess.requireDeclaredFields entries "request" fields

            do!
                entries
                |> iterate (fun (key, value) ->
                    if key = "input" then
                        Ok()
                    else
                        PortableForbiddenContent.requireCleanControl value)

            let! input = CborAccess.field entries "input"
            do! PortableForbiddenContent.requireCleanPayload input

            let hasOperation = CborAccess.contains entries "operation"
            let hasCompact = CborAccess.contains entries "compactOperation"

            do!
                ensure (hasOperation <> hasCompact) (fun () ->
                    malformed
                        "operation-designation"
                        "A request names its Operation either canonically or by one compact identifier.")

            let! designation =
                if hasOperation then
                    CborAccess.field entries "operation"
                    |> Result.bind (fun item -> CborAccess.readCanonical item "operation")
                    |> Result.map (PortableOperationRef.ofCanonical >> OperationDesignation.Canonical)
                else
                    CborAccess.int32 entries "compactOperation" |> Result.map OperationDesignation.Compact

            let! inputShape =
                CborAccess.field entries "inputShape"
                |> Result.bind (fun item -> CborAccess.readCanonical item "inputShape")
                |> Result.map PortableShapeRef.ofCanonical

            let! resources = CborAccess.array entries "resources"

            return
                { Operation = designation
                  InputShape = inputShape
                  Input = input
                  Resources = resources }
        }

[<RequireQualifiedAccess>]
type OutcomeStatus =
    | Succeeded
    | Failed

/// The body of an outcome envelope.
type OutcomeBody =
    { Status: OutcomeStatus
      ValueShape: PortableShapeRef
      Value: CborItem
      ProviderEffectCount: int64 }

[<RequireQualifiedAccess>]
module OutcomeBody =

    let private fields = [ "status"; "valueShape"; "value"; "providerEffectCount" ]

    let encode (body: OutcomeBody) =
        CborMap
            [ "status",
              CborText(
                  match body.Status with
                  | OutcomeStatus.Succeeded -> "succeeded"
                  | OutcomeStatus.Failed -> "failed"
              )
              "valueShape", CborAccess.encodeCanonical (PortableShapeRef.canonical body.ValueShape)
              "value", body.Value
              "providerEffectCount", CborInteger body.ProviderEffectCount ]

    let decode item : PortableResult<OutcomeBody> =
        portable {
            let! entries = CborAccess.requireMap item "outcome"
            do! CborAccess.requireDeclaredFields entries "outcome" fields

            do!
                entries
                |> iterate (fun (key, value) ->
                    if key = "value" then
                        Ok()
                    else
                        PortableForbiddenContent.requireCleanControl value)

            let! value = CborAccess.field entries "value"
            do! PortableForbiddenContent.requireCleanPayload value
            let! statusToken = CborAccess.text entries "status"

            let! status =
                match statusToken with
                | "succeeded" -> Ok OutcomeStatus.Succeeded
                | "failed" -> Ok OutcomeStatus.Failed
                | other -> malformed "outcome-status" $"'{other}' is not a terminal Outcome status."

            let! valueShape =
                CborAccess.field entries "valueShape"
                |> Result.bind (fun item -> CborAccess.readCanonical item "valueShape")
                |> Result.map PortableShapeRef.ofCanonical

            let! providerEffectCount = CborAccess.integer entries "providerEffectCount"

            return
                { Status = status
                  ValueShape = valueShape
                  Value = value
                  ProviderEffectCount = providerEffectCount }
        }

/// The body of a protocol-error envelope.
///
/// The portable category drives semantics. The local code travels beside it as non-normative data,
/// so two realizations may use different local strings for the same category.
type ProtocolErrorBody =
    { Category: ProtocolCategory
      LocalCode: string
      FailureDomain: FailureDomain }

[<RequireQualifiedAccess>]
module ProtocolErrorBody =

    let encode (body: ProtocolErrorBody) =
        CborMap
            [ "category", CborText(ProtocolCategory.token body.Category)
              "localCode", CborText body.LocalCode
              "failureDomain", CborText(FailureDomain.token body.FailureDomain) ]

    let decode item : PortableResult<ProtocolErrorBody> =
        portable {
            let! entries = CborAccess.requireMap item "protocol-error"
            do! CborAccess.requireDeclaredFields entries "protocol-error" [ "category"; "localCode"; "failureDomain" ]
            do! PortableForbiddenContent.requireCleanControl (CborMap entries)
            let! categoryToken = CborAccess.text entries "category"

            let! category =
                match ProtocolCategory.tryParse categoryToken with
                | Some category -> Ok category
                | None -> malformed "protocol-category" $"'{categoryToken}' is outside the Channel taxonomy."

            let! localCode = CborAccess.text entries "localCode"
            let! domainToken = CborAccess.text entries "failureDomain"

            let! domain =
                match FailureDomain.tryParse domainToken with
                | Some domain -> Ok domain
                | None -> malformed "failure-domain" $"'{domainToken}' is outside the Channel taxonomy."

            return
                { Category = category
                  LocalCode = localCode
                  FailureDomain = domain }
        }
