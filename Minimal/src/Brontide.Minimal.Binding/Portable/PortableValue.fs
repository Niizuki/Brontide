namespace Brontide.Minimal.Binding.Portable

open System

/// A shaped value in the portable Shape floor.
///
/// The wire carries no kind discriminator: the negotiated Shape determines the type of every
/// position. The discriminated union exists in memory so that a mismatch between the declared Shape
/// and the presented value is a refusal rather than a silent coercion.
[<StructuralEquality; NoComparison>]
type PortableValue =
    | PortableUnit
    | PortableText of string
    | PortableBoolean of bool
    | PortableInteger of int64
    | PortableDecimalValue of exponent: int * mantissa: int64
    | PortableBytesValue of byte array
    | PortableSequence of PortableValue list
    | PortableChoice of alternative: string * value: PortableValue
    | PortableRecord of fields: Map<string, PortableValue> * fragments: Map<PortableFragmentRef, Map<string, PortableValue>>

[<RequireQualifiedAccess>]
module PortableRecord =

    let empty = PortableRecord(Map.empty, Map.empty)

    let ofFields fields =
        PortableRecord(Map.ofList fields, Map.empty)

    let withField name value record =
        match record with
        | PortableRecord(fields, fragments) -> PortableRecord(Map.add name value fields, fragments)
        | other -> other

    let withFragment reference fields record =
        match record with
        | PortableRecord(recordFields, fragments) ->
            PortableRecord(recordFields, Map.add reference (Map.ofList fields) fragments)
        | other -> other

    let tryField name record =
        match record with
        | PortableRecord(fields, _) -> Map.tryFind name fields
        | _ -> None

    let fragments record =
        match record with
        | PortableRecord(_, fragments) -> fragments
        | _ -> Map.empty

/// Field names that carry foreign runtime identity and may never appear in any position.
///
/// The outcome follows the position: a refused name in a control position is malformed-message; in
/// a declared payload position it is invalid-payload. Matching is case-insensitive so a renamed
/// casing cannot smuggle the same content across.
[<RequireQualifiedAccess>]
module PortableForbiddenContent =

    let private names =
        set [ "$type"; "typename"; "exception"; "stacktrace"; "innerexception"; "targetsite" ]

    let isForbidden (name: string) = Set.contains (name.ToLowerInvariant()) names

    let private scan (refuseName: string -> PortableResult<unit>) (item: CborItem) : PortableResult<unit> =
        let rec walk item =
            match item with
            | CborMap entries ->
                entries
                |> iterate (fun (key, value) ->
                    if isForbidden key then refuseName key else walk value)
            | CborArray items -> items |> iterate walk
            | _ -> Ok()

        walk item

    /// Walks a decoded control item and refuses foreign runtime identity as malformed.
    let requireCleanControl item =
        scan (fun key -> malformed "foreign-runtime-data" $"Control field '{key}' carries foreign runtime identity.") item

    /// Walks a decoded payload item and refuses foreign runtime identity as invalid payload.
    let requireCleanPayload item =
        scan
            (fun key -> invalidPayload "foreign-runtime-data" $"Payload field '{key}' carries foreign runtime identity.")
            item

    /// The same scan over an in-memory value, which is what the fixed direct-call realization
    /// presents. Both realizations therefore refuse the same content.
    let requireCleanValue (value: PortableValue) : PortableResult<unit> =
        let refuse (name: string) =
            invalidPayload "foreign-runtime-data" $"Payload member '{name}' carries foreign runtime identity."

        let rec walk value =
            match value with
            | PortableRecord(fields, fragments) ->
                portable {
                    do!
                        fields
                        |> Map.toList
                        |> iterate (fun (name, child) -> if isForbidden name then refuse name else walk child)

                    do!
                        fragments
                        |> Map.toList
                        |> iterate (fun (reference, fragmentFields) ->
                            if isForbidden (PortableFragmentRef.name reference) then
                                refuse (PortableFragmentRef.name reference)
                            else
                                fragmentFields
                                |> Map.toList
                                |> iterate (fun (name, child) -> if isForbidden name then refuse name else walk child))
                }
            | PortableSequence items -> items |> iterate walk
            | PortableChoice(alternative, child) ->
                if isForbidden alternative then refuse alternative else walk child
            | PortableUnit
            | PortableText _
            | PortableBoolean _
            | PortableInteger _
            | PortableDecimalValue _
            | PortableBytesValue _ -> Ok()

        walk value

[<RequireQualifiedAccess>]
module PortableHex =

    let encode (bytes: byte array) =
        Convert.ToHexString(bytes).ToLowerInvariant()

    let decode (value: string) = Convert.FromHexString value
