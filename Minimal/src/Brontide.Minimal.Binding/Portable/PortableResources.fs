namespace Brontide.Minimal.Binding.Portable

open System.Security.Cryptography

[<RequireQualifiedAccess>]
type ResourceFlavor =
    | CopiedImmutableBlob
    | AddressingOnlyHandle

/// A referenced shaped resource carried alongside an inline request payload.
///
/// The two flavors are separate cases rather than one record with optional members, so a handle has
/// nowhere to put octets: the forbidden implicit copy is unrepresentable in memory and refused on
/// the wire.
[<StructuralEquality; NoComparison>]
type PortableResource =
    /// The v0.1 floor: the receiver obtains an independent immutable copy verified by content hash.
    /// Because ownership is by copy there is no borrow interval, the sender retains its own copy,
    /// and no release or completion signal is defined.
    | CopiedBlob of name: string * content: byte array * integrity: string
    /// The Catalog-derived flavor: a handle conveys addressing and never authority, so possession is
    /// not an admission decision.
    | AddressingHandle of name: string * provider: string * id: string

[<RequireQualifiedAccess>]
module ResourceFlavor =
    [<Literal>]
    let CopiedImmutableBlobToken = "copied-immutable-blob"

    [<Literal>]
    let AddressingOnlyHandleToken = "addressing-only-handle"

    /// Borrow intervals and ownership transfer are 0.1 non-goals. Declaring one fails negotiation
    /// closed; there is no fallback and no silent downgrade to the copied flavor.
    let nonGoals = [ "borrowed-read-only-region"; "transferred-ownership" ]

    let token flavor =
        match flavor with
        | ResourceFlavor.CopiedImmutableBlob -> CopiedImmutableBlobToken
        | ResourceFlavor.AddressingOnlyHandle -> AddressingOnlyHandleToken

    let tryParse value : PortableResult<ResourceFlavor> =
        match value with
        | CopiedImmutableBlobToken -> Ok ResourceFlavor.CopiedImmutableBlob
        | AddressingOnlyHandleToken -> Ok ResourceFlavor.AddressingOnlyHandle
        | other ->
            unsupportedContract
                "unsupported-resource-flavor"
                $"Resource flavor '{other}' is not supported in version 0.1; there is no fallback."

    let ownership flavor =
        match flavor with
        | ResourceFlavor.CopiedImmutableBlob -> "copied"
        | ResourceFlavor.AddressingOnlyHandle -> "provider-retained"

/// What one interaction observed about one referenced resource.
type ResourceObservation =
    { Flavor: string
      Ownership: string
      Copies: int64
      IntegrityVerified: bool
      Accepted: bool }

[<RequireQualifiedAccess>]
module PortableResource =

    let hashOf (content: byte array) =
        PortableHex.encode (SHA256.HashData content)

    let blob name (content: byte array) =
        CopiedBlob(name, content, hashOf content)

    let flavor resource =
        match resource with
        | CopiedBlob _ -> ResourceFlavor.CopiedImmutableBlob
        | AddressingHandle _ -> ResourceFlavor.AddressingOnlyHandle

    let name resource =
        match resource with
        | CopiedBlob(name, _, _) -> name
        | AddressingHandle(name, _, _) -> name

    /// The text by which the Binding Plan's accept list names one handle.
    let handleText provider id = $"{provider}/{id}"

    /// The copy accounting one realization implies. It is a required observation, so a forbidden
    /// implicit copy is visible rather than inferred.
    let copiesFor flavor realization =
        match flavor, realization with
        | ResourceFlavor.CopiedImmutableBlob, Realization.NegotiatedProcess -> 1L
        | _ -> 0L

    let observe resource realization =
        let flavour = flavor resource

        { Flavor = ResourceFlavor.token flavour
          Ownership = ResourceFlavor.ownership flavour
          Copies = copiesFor flavour realization
          IntegrityVerified = flavour = ResourceFlavor.CopiedImmutableBlob
          Accepted = true }

/// Codec and admission rules for referenced resources.
[<RequireQualifiedAccess>]
module ResourceCodec =

    let private declaredFields =
        [ "flavor"; "name"; "content"; "integrity"; "provider"; "id"; "release" ]

    let encode resource =
        match resource with
        | CopiedBlob(name, content, integrity) ->
            CborMap
                [ "flavor", CborText ResourceFlavor.CopiedImmutableBlobToken
                  "name", CborText name
                  "content", CborBytes content
                  "integrity", CborText integrity ]
        | AddressingHandle(name, provider, id) ->
            CborMap
                [ "flavor", CborText ResourceFlavor.AddressingOnlyHandleToken
                  "name", CborText name
                  "provider", CborText provider
                  "id", CborText id ]

    /// Applies the admission rules a realization cannot skip: the flavor is negotiated, the bound
    /// holds, the content hash verifies, and a handle sits inside the declared accept list.
    ///
    /// The fixed direct-call realization never decodes a frame, so it calls this directly and
    /// refuses exactly what the process realization refuses.
    let admit resource (negotiatedFlavors: string list) (acceptedHandles: string list) limits : PortableResult<unit> =
        portable {
            let flavour = PortableResource.flavor resource

            do!
                ensure (List.contains (ResourceFlavor.token flavour) negotiatedFlavors) (fun () ->
                    unsupportedContract
                        "resource-flavor-unnegotiated"
                        $"The Binding Plan did not negotiate resource flavor '{ResourceFlavor.token flavour}'.")

            match resource with
            | CopiedBlob(_, content, integrity) ->
                do!
                    ensure (content.Length <= limits.MaxResourceBytes) (fun () ->
                        limitExceeded
                            "resource-bound"
                            $"A resource of {content.Length} bytes exceeds the declared bound of {limits.MaxResourceBytes}.")

                let wellFormed =
                    integrity.Length = 64
                    && integrity
                       |> Seq.forall (fun character ->
                           (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'))

                do!
                    ensure wellFormed (fun () ->
                        invalidPayload
                            "resource-integrity-form"
                            "A content hash is 64 lowercase hexadecimal characters of SHA-256.")

                do!
                    ensure (PortableResource.hashOf content = integrity) (fun () ->
                        invalidPayload
                            "resource-integrity"
                            "The received resource octets do not hash to the declared content hash.")
            | AddressingHandle(_, provider, id) ->
                // A handle carries no authority, so refusing one is a payload decision rather than
                // an authority decision.
                do!
                    ensure (List.contains (PortableResource.handleText provider id) acceptedHandles) (fun () ->
                        invalidPayload
                            "resource-refused"
                            $"Handle '{PortableResource.handleText provider id}' is outside the accept list the Binding Plan declares.")
        }

    /// Decodes and admits one declared resource against the frozen plan facts.
    ///
    /// Every refusal here happens before any provider effect, and none of them is an authority
    /// decision.
    let decode item negotiatedFlavors acceptedHandles limits : PortableResult<PortableResource> =
        portable {
            let! entries = CborAccess.requireMap item "resource"
            do! CborAccess.requireDeclaredFields entries "resource" declaredFields
            let! flavorToken = CborAccess.text entries "flavor"
            let! flavour = ResourceFlavor.tryParse flavorToken
            let! name = CborAccess.text entries "name"

            do!
                ensure (not (CborAccess.contains entries "release")) (fun () ->
                    stateViolation
                        "release-signal-undefined"
                        $"Flavor '{flavorToken}' defines no release signal, so a frame carrying one is illegal.")

            let! resource =
                match flavour with
                | ResourceFlavor.CopiedImmutableBlob ->
                    portable {
                        do!
                            ensure
                                (not (CborAccess.contains entries "provider" || CborAccess.contains entries "id"))
                                (fun () ->
                                    invalidPayload
                                        "resource-members"
                                        "A copied-immutable-blob resource carries content and integrity, not a handle.")

                        let! content = CborAccess.field entries "content"

                        let! octets =
                            match content with
                            | CborBytes octets -> Ok octets
                            | _ -> invalidPayload "resource-content" "Resource content must be a byte string."

                        let! integrity = CborAccess.text entries "integrity"
                        return CopiedBlob(name, octets, integrity)
                    }
                | ResourceFlavor.AddressingOnlyHandle ->
                    portable {
                        // The addressing-only handle permits no copy at all, so producing octets for
                        // it is a forbidden implicit copy rather than an accepted convenience.
                        do!
                            ensure (not (CborAccess.contains entries "content")) (fun () ->
                                invalidPayload
                                    "forbidden-implicit-copy"
                                    "The addressing-only-handle flavor permits no copy, so resource octets may not accompany it.")

                        let! provider = CborAccess.text entries "provider"
                        let! id = CborAccess.text entries "id"
                        return AddressingHandle(name, provider, id)
                    }

            do! admit resource negotiatedFlavors acceptedHandles limits
            return resource
        }
