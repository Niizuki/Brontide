namespace Brontide.Minimal.Binding.Portable

open System
open System.Text

/// A value in the deterministic CBOR core the portable representation permits.
///
/// The union is deliberately narrower than CBOR: no float, no indefinite length, no simple value
/// beyond true/false/null, and exactly one tag. A value outside the union cannot be constructed, so
/// the encoder cannot emit something the decoder would have to refuse.
type CborItem =
    | CborInteger of int64
    | CborText of string
    | CborBytes of byte array
    | CborBoolean of bool
    | CborNull
    /// The single allowlisted tag: an exact decimal fraction encoded as tag 4.
    | CborDecimal of exponent: int * mantissa: int64
    | CborArray of CborItem list
    | CborMap of (string * CborItem) list

[<RequireQualifiedAccess>]
module CborDecimal =
    [<Literal>]
    let MinExponent = -28

    [<Literal>]
    let MaxExponent = 28

    /// Normalizes so that one value has exactly one encoding: a mantissa divisible by ten is scaled
    /// up, and a zero mantissa always carries exponent zero.
    let normalize exponent mantissa : PortableResult<int * int64> =
        if mantissa = 0L then
            Ok(0, 0L)
        else
            let rec scale exponent mantissa =
                if mantissa % 10L = 0L && exponent < MaxExponent then
                    scale (exponent + 1) (mantissa / 10L)
                else
                    exponent, mantissa

            let scaledExponent, scaledMantissa = scale exponent mantissa

            if scaledExponent < MinExponent || scaledExponent > MaxExponent then
                malformed "decimal-exponent" "A Decimal exponent outside -28..28 is not representable in the portable core."
            else
                Ok(scaledExponent, scaledMantissa)

/// The deterministic CBOR core encoder and decoder (RFC 8949 section 4.2.1), restricted to the
/// subset the portable representation declares.
///
/// Map keys sort by the bytewise order of their complete encoding, not by ordinal string
/// comparison. The two orders disagree once key lengths cross an encoding-width boundary, which is
/// why the baseline JSON codec's comparer cannot be reused here.
[<RequireQualifiedAccess>]
module PortableCbor =

    let private strictUtf8 = UTF8Encoding(false, true)

    /// Orders two complete key encodings by the deterministic bytewise rule.
    let compareKeyEncodings (left: byte array) (leftStart: int) (leftLength: int) (right: byte array) (rightStart: int) (rightLength: int) =
        let shared = min leftLength rightLength

        let rec walk index =
            if index = shared then compare leftLength rightLength
            else
                let l = left.[leftStart + index]
                let r = right.[rightStart + index]
                if l = r then walk (index + 1)
                elif l < r then -1
                else 1

        walk 0

    // -- encoding ---------------------------------------------------------

    let private writeHead (buffer: ResizeArray<byte>) (major: int) (argument: uint64) =
        let prefix = byte (major <<< 5)

        let writeBigEndian width =
            for shift in [ (width - 1) * 8 .. -8 .. 0 ] do
                buffer.Add(byte ((argument >>> shift) &&& 0xFFUL))

        if argument < 24UL then
            buffer.Add(prefix ||| byte argument)
        elif argument <= uint64 Byte.MaxValue then
            buffer.Add(prefix ||| 24uy)
            buffer.Add(byte argument)
        elif argument <= uint64 UInt16.MaxValue then
            buffer.Add(prefix ||| 25uy)
            writeBigEndian 2
        elif argument <= uint64 UInt32.MaxValue then
            buffer.Add(prefix ||| 26uy)
            writeBigEndian 4
        else
            buffer.Add(prefix ||| 27uy)
            writeBigEndian 8

    let rec private write (buffer: ResizeArray<byte>) (item: CborItem) : PortableResult<unit> =
        match item with
        | CborInteger value when value >= 0L ->
            writeHead buffer 0 (uint64 value)
            Ok()
        | CborInteger value ->
            writeHead buffer 1 (uint64 (-1L - value))
            Ok()
        | CborBoolean value ->
            buffer.Add(if value then 0xF5uy else 0xF4uy)
            Ok()
        | CborNull ->
            buffer.Add 0xF6uy
            Ok()
        | CborText value ->
            let bytes = strictUtf8.GetBytes value
            writeHead buffer 3 (uint64 bytes.Length)
            buffer.AddRange bytes
            Ok()
        | CborBytes value ->
            writeHead buffer 2 (uint64 value.Length)
            buffer.AddRange value
            Ok()
        | CborDecimal(exponent, mantissa) ->
            CborDecimal.normalize exponent mantissa
            |> Result.bind (fun (normalizedExponent, normalizedMantissa) ->
                writeHead buffer 6 4UL
                writeHead buffer 4 2UL

                portable {
                    do! write buffer (CborInteger(int64 normalizedExponent))
                    do! write buffer (CborInteger normalizedMantissa)
                })
        | CborArray items ->
            writeHead buffer 4 (uint64 (List.length items))
            items |> iterate (write buffer)
        | CborMap entries -> writeMap buffer entries

    and private writeMap (buffer: ResizeArray<byte>) (entries: (string * CborItem) list) : PortableResult<unit> =
        let duplicate =
            entries
            |> List.countBy fst
            |> List.tryFind (fun (_, count) -> count > 1)

        match duplicate with
        | Some(key, _) -> malformed "duplicate-key" $"Map key '{key}' appears more than once."
        | None ->
            let encodeEntry (key, value) =
                portable {
                    let keyBuffer = ResizeArray<byte>()
                    do! write keyBuffer (CborText key)
                    let valueBuffer = ResizeArray<byte>()
                    do! write valueBuffer value
                    return keyBuffer.ToArray(), valueBuffer.ToArray()
                }

            entries
            |> traverse encodeEntry
            |> Result.map (fun encoded ->
                let sorted =
                    encoded
                    |> List.sortWith (fun (leftKey: byte array, _) (rightKey: byte array, _) ->
                        compareKeyEncodings leftKey 0 leftKey.Length rightKey 0 rightKey.Length)

                writeHead buffer 5 (uint64 (List.length sorted))

                for key, value in sorted do
                    buffer.AddRange key
                    buffer.AddRange value)

    let encode (item: CborItem) : PortableResult<byte array> =
        let buffer = ResizeArray<byte>()
        write buffer item |> Result.map (fun () -> buffer.ToArray())

    // -- decoding ---------------------------------------------------------

    let private take (body: byte array) (position: int) (count: int) : PortableResult<int> =
        if count < 0 || position + count > body.Length then
            malformed "truncated-item" "The frame body ends inside an item."
        else
            Ok(position + count)

    let private requireShortest (value: uint64) (minimum: uint64) =
        if value >= minimum then
            Ok value
        else
            malformed "non-shortest-argument" "An argument must use the shortest form that represents it exactly."

    let private readBigEndian (body: byte array) (position: int) (width: int) : PortableResult<uint64 * int> =
        take body position width
        |> Result.map (fun next ->
            let mutable value = 0UL

            for index in position .. next - 1 do
                value <- (value <<< 8) ||| uint64 body.[index]

            value, next)

    let private readArgument (body: byte array) (position: int) (additional: int) : PortableResult<uint64 * int> =
        if additional < 24 then
            Ok(uint64 additional, position)
        elif additional = 24 then
            take body position 1
            |> Result.bind (fun next ->
                requireShortest (uint64 body.[position]) 24UL
                |> Result.map (fun value -> value, next))
        elif additional = 25 then
            readBigEndian body position 2
            |> Result.bind (fun (value, next) ->
                requireShortest value (uint64 Byte.MaxValue + 1UL) |> Result.map (fun value -> value, next))
        elif additional = 26 then
            readBigEndian body position 4
            |> Result.bind (fun (value, next) ->
                requireShortest value (uint64 UInt16.MaxValue + 1UL) |> Result.map (fun value -> value, next))
        elif additional = 27 then
            readBigEndian body position 8
            |> Result.bind (fun (value, next) ->
                requireShortest value (uint64 UInt32.MaxValue + 1UL) |> Result.map (fun value -> value, next))
        elif additional = 31 then
            malformed "indefinite-length" "Indefinite-length items are excluded from the deterministic core."
        else
            malformed "reserved-argument" "The argument uses a reserved additional-information value."

    let private requireLength (argument: uint64) (bound: int) (what: string) : PortableResult<int> =
        if argument > uint64 bound then
            limitExceeded $"{what}-bound" $"A {what} of {argument} exceeds the declared bound of {bound}."
        else
            Ok(int argument)

    let rec private read (body: byte array) (position: int) (depth: int) (limits: PortableLimits) : PortableResult<CborItem * int> =
        if depth > limits.MaxNestingDepth then
            limitExceeded "nesting-depth" $"Decoding stopped at the declared nesting depth of {limits.MaxNestingDepth}."
        else
            take body position 1
            |> Result.bind (fun afterHead ->
                let head = body.[position]
                let major = int (head >>> 5)
                let additional = int (head &&& 0x1Fuy)

                if major = 7 then
                    match additional with
                    | 20 -> Ok(CborBoolean false, afterHead)
                    | 21 -> Ok(CborBoolean true, afterHead)
                    | 22 -> Ok(CborNull, afterHead)
                    | _ ->
                        malformed
                            "simple-value"
                            "Only the simple values 20, 21, and 22 belong to the deterministic core; floats are excluded."
                else
                    readArgument body afterHead additional
                    |> Result.bind (fun (argument, afterArgument) ->
                        match major with
                        | 0 ->
                            if argument > uint64 Int64.MaxValue then
                                malformed "integer-domain" "An unsigned integer outside the Integer.Signed64 domain is not portable."
                            else
                                Ok(CborInteger(int64 argument), afterArgument)
                        | 1 ->
                            if argument > uint64 Int64.MaxValue then
                                malformed "integer-domain" "A negative integer outside the Integer.Signed64 domain is not portable."
                            else
                                Ok(CborInteger(-1L - int64 argument), afterArgument)
                        | 2 ->
                            requireLength argument limits.MaxByteStringBytes "byte-string"
                            |> Result.bind (fun length ->
                                take body afterArgument length
                                |> Result.map (fun next -> CborBytes(Array.sub body afterArgument length), next))
                        | 3 ->
                            requireLength argument limits.MaxTextBytes "text-string"
                            |> Result.bind (fun length ->
                                take body afterArgument length
                                |> Result.bind (fun next ->
                                    try
                                        Ok(CborText(strictUtf8.GetString(body, afterArgument, length)), next)
                                    with :? DecoderFallbackException ->
                                        malformed "text-encoding" "A text string is not well-formed UTF-8."))
                        | 4 ->
                            requireLength argument limits.MaxSequenceItems "array"
                            |> Result.bind (fun count -> readArray body afterArgument depth limits count)
                        | 5 ->
                            requireLength argument limits.MaxRecordFields "map"
                            |> Result.bind (fun count -> readMap body afterArgument depth limits count)
                        | 6 -> readTag body afterArgument depth limits argument
                        | _ -> malformed "major-type" "The item uses a major type outside the portable core."))

    and private readArray body position depth limits count : PortableResult<CborItem * int> =
        let rec walk index accumulated cursor =
            if index = count then
                Ok(CborArray(List.rev accumulated), cursor)
            else
                match read body cursor (depth + 1) limits with
                | Ok(item, next) -> walk (index + 1) (item :: accumulated) next
                | Error error -> Error error

        walk 0 [] position

    and private readMap body position depth limits count : PortableResult<CborItem * int> =
        let rec walk index accumulated cursor previousKey =
            if index = count then
                Ok(CborMap(List.rev accumulated), cursor)
            else
                let keyStart = cursor

                match read body cursor (depth + 1) limits with
                | Error error -> Error error
                | Ok(keyItem, afterKey) ->
                    match keyItem with
                    | CborText key ->
                        let ordering =
                            match previousKey with
                            | Some(previousStart, previousLength) ->
                                compareKeyEncodings body previousStart previousLength body keyStart (afterKey - keyStart)
                            | None -> -1

                        if ordering = 0 then
                            malformed "duplicate-key" $"Map key '{key}' appears more than once."
                        elif ordering > 0 then
                            malformed "map-key-order" $"Map key '{key}' breaks ascending deterministic key order."
                        else
                            match read body afterKey (depth + 1) limits with
                            | Error error -> Error error
                            | Ok(value, afterValue) ->
                                walk (index + 1) ((key, value) :: accumulated) afterValue (Some(keyStart, afterKey - keyStart))
                    | _ -> malformed "map-key-kind" "Every portable map key is a text string."

        walk 0 [] position None

    and private readTag body position depth limits tag : PortableResult<CborItem * int> =
        if tag <> 4UL then
            malformed "tag-allowlist" $"Tag {tag} is outside the allowlist, which contains only tag 4."
        else
            read body position (depth + 1) limits
            |> Result.bind (fun (item, next) ->
                match item with
                | CborArray [ CborInteger exponent; CborInteger mantissa ] ->
                    if exponent < int64 CborDecimal.MinExponent || exponent > int64 CborDecimal.MaxExponent then
                        malformed "decimal-exponent" "A Decimal exponent must lie in -28..28."
                    elif mantissa = 0L && exponent <> 0L then
                        malformed "decimal-canonical" "A zero mantissa is canonically encoded with exponent 0."
                    elif mantissa <> 0L && mantissa % 10L = 0L then
                        malformed "decimal-canonical" "A Decimal mantissa is normalized so that it is not divisible by ten."
                    else
                        Ok(CborDecimal(int exponent, mantissa), next)
                | _ -> malformed "decimal-body" "Tag 4 carries an array of exactly two integers: [exponent, mantissa].")

    let decode (body: byte array) (limits: PortableLimits) : PortableResult<CborItem> =
        read body 0 1 limits
        |> Result.bind (fun (item, position) ->
            if position <> body.Length then
                malformed "trailing-bytes" "One frame carries exactly one top-level item; trailing bytes are not permitted."
            else
                Ok item)

/// Strict accessors over a decoded control item. Every miss is a portable category rather than a
/// parser failure, so no foreign runtime failure escapes the decode boundary.
[<RequireQualifiedAccess>]
module CborAccess =

    let requireMap (item: CborItem) (what: string) : PortableResult<(string * CborItem) list> =
        match item with
        | CborMap entries -> Ok entries
        | _ -> malformed $"{what}-kind" $"'{what}' must be a map."

    let requireArray (item: CborItem) (what: string) : PortableResult<CborItem list> =
        match item with
        | CborArray items -> Ok items
        | _ -> malformed $"{what}-kind" $"'{what}' must be an array."

    let tryField (entries: (string * CborItem) list) (name: string) =
        entries |> List.tryPick (fun (key, value) -> if key = name then Some value else None)

    let contains entries name = (tryField entries name).IsSome

    let field entries name : PortableResult<CborItem> =
        match tryField entries name with
        | Some value -> Ok value
        | None -> malformed $"{name}-absent" $"The required field '{name}' is absent."

    let text entries name : PortableResult<string> =
        field entries name
        |> Result.bind (fun item ->
            match item with
            | CborText value -> Ok value
            | _ -> malformed $"{name}-kind" $"'{name}' must be a text string.")

    let integer entries name : PortableResult<int64> =
        field entries name
        |> Result.bind (fun item ->
            match item with
            | CborInteger value -> Ok value
            | _ -> malformed $"{name}-kind" $"'{name}' must be an integer.")

    let int32 entries name : PortableResult<int> =
        integer entries name
        |> Result.bind (fun value ->
            if value >= int64 Int32.MinValue && value <= int64 Int32.MaxValue then
                Ok(int value)
            else
                malformed $"{name}-range" $"'{name}' is outside the 32-bit range.")

    let boolean entries name : PortableResult<bool> =
        field entries name
        |> Result.bind (fun item ->
            match item with
            | CborBoolean value -> Ok value
            | _ -> malformed $"{name}-kind" $"'{name}' must be a boolean.")

    let map entries name : PortableResult<(string * CborItem) list> =
        field entries name |> Result.bind (fun item -> requireMap item name)

    let array entries name : PortableResult<CborItem list> =
        field entries name |> Result.bind (fun item -> requireArray item name)

    let arrayOf entries name (read: CborItem -> PortableResult<'T>) : PortableResult<'T list> =
        array entries name |> Result.bind (traverse read)

    let optionalText entries name : PortableResult<string option> =
        match tryField entries name with
        | None -> Ok None
        | Some(CborText value) -> Ok(Some value)
        | Some _ -> malformed $"{name}-kind" $"'{name}' must be a text string."

    let requireText (item: CborItem) (what: string) : PortableResult<string> =
        match item with
        | CborText value -> Ok value
        | _ -> malformed $"{what}-kind" $"'{what}' must be a text string."

    /// Refuses any field the caller did not declare, per the reject-unknown-field policy.
    let requireDeclaredFields (entries: (string * CborItem) list) (what: string) (declared: string list) =
        entries
        |> iterate (fun (key, _) ->
            if List.contains key declared then
                Ok()
            else
                malformed $"{what}-unknown-field" $"'{what}' declares no field named '{key}'.")

    let encodeCanonical (canonical: PortableCanonical) =
        CborMap
            [ "name", CborText(PortableCanonical.name canonical)
              "version", CborInteger(int64 (PortableCanonical.version canonical)) ]

    let readCanonical (item: CborItem) (what: string) : PortableResult<PortableCanonical> =
        portable {
            let! entries = requireMap item what
            do! requireDeclaredFields entries what [ "name"; "version" ]
            let! name = text entries "name"
            let! version = int32 entries "version"
            return! PortableCanonical.tryCreate name version
        }
