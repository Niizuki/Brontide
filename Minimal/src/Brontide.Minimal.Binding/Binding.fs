namespace Brontide.Minimal.Binding

open System
open System.IO
open System.Text
open System.Text.Json
open Brontide.Minimal.Model

[<StructuralEquality; StructuralComparison>]
type WireReference =
    { Name: string
      Version: int }

type BoundaryManifest =
    { ProtocolVersion: int
      Implementation: string
      Shapes: Set<WireReference>
      Operations: Set<WireReference> }

type NegotiatedBoundary =
    { ProtocolVersion: int
      LocalImplementation: string
      RemoteImplementation: string
      Shapes: Set<WireReference>
      Operations: Set<WireReference> }

module private Json =
    let writeText (write: Utf8JsonWriter -> unit) =
        use stream = new MemoryStream()

        use writer =
            new Utf8JsonWriter(
                stream,
                JsonWriterOptions(Indented = false, SkipValidation = false)
            )

        write writer
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    let tryProperty (name: string) (element: JsonElement) =
        let mutable property = Unchecked.defaultof<JsonElement>

        if element.TryGetProperty(name, &property) then
            Ok property
        else
            Error $"The JSON value is missing '{name}'."

    let requiredString (description: string) (element: JsonElement) =
        match element.GetString() |> Option.ofObj with
        | Some value -> Ok value
        | None -> Error $"{description} cannot be null."

    let collect results =
        results
        |> Seq.fold
            (fun state result ->
                match state, result with
                | Ok values, Ok value -> Ok(value :: values)
                | Error message, _ -> Error message
                | _, Error message -> Error message)
            (Ok [])
        |> Result.map List.rev

[<RequireQualifiedAccess>]
module Manifest =
    let private wireReference (name: CanonicalName) version : WireReference =
        { Name = CanonicalName.value name
          Version = version }

    let ofMinimal
        (implementation: string)
        (shapes: ShapeReference seq)
        (operations: OperationReference seq)
        : BoundaryManifest =
        { ProtocolVersion = 1
          Implementation = implementation
          Shapes = shapes |> Seq.map (fun shape -> wireReference shape.Name shape.Version) |> Set.ofSeq
          Operations = operations |> Seq.map (fun operation -> wireReference operation.Name operation.Version) |> Set.ofSeq }

    let negotiate
        (requiredShapes: Set<WireReference>)
        (requiredOperations: Set<WireReference>)
        (local: BoundaryManifest)
        (remote: BoundaryManifest)
        =
        if local.ProtocolVersion <> remote.ProtocolVersion then
            Error "The boundary protocol versions are incompatible."
        else
            let commonShapes = Set.intersect local.Shapes remote.Shapes
            let commonOperations = Set.intersect local.Operations remote.Operations
            let missingShape = requiredShapes |> Seq.tryFind (fun shape -> not (Set.contains shape commonShapes))
            let missingOperation = requiredOperations |> Seq.tryFind (fun operation -> not (Set.contains operation commonOperations))

            match missingShape, missingOperation with
            | Some shape, _ -> Error $"Required shape {shape.Name}@{shape.Version} was not negotiated."
            | _, Some operation -> Error $"Required operation {operation.Name}@{operation.Version} was not negotiated."
            | _ ->
                Ok
                    { ProtocolVersion = local.ProtocolVersion
                      LocalImplementation = local.Implementation
                      RemoteImplementation = remote.Implementation
                      Shapes = commonShapes
                      Operations = commonOperations }

    let toJson (manifest: BoundaryManifest) =
        let writeReferences (propertyName: string) (references: Set<WireReference>) (writer: Utf8JsonWriter) =
            writer.WriteStartArray propertyName

            references
            |> Seq.iter (fun reference ->
                writer.WriteStartObject()
                writer.WriteString("name", reference.Name)
                writer.WriteNumber("version", reference.Version)
                writer.WriteEndObject())

            writer.WriteEndArray()

        Json.writeText (fun writer ->
            writer.WriteStartObject()
            writer.WriteNumber("protocolVersion", manifest.ProtocolVersion)
            writer.WriteString("implementation", manifest.Implementation)
            writeReferences "shapes" manifest.Shapes writer
            writeReferences "operations" manifest.Operations writer
            writer.WriteEndObject())

    let tryFromJson (json: string) =
        let parseReference (element: JsonElement) =
            match
                Json.tryProperty "name" element |> Result.bind (Json.requiredString "A manifest reference name"),
                Json.tryProperty "version" element
            with
            | Ok name, Ok version ->
                let reference: WireReference =
                    { Name = name
                      Version = version.GetInt32() }

                if String.IsNullOrWhiteSpace reference.Name || reference.Version < 1 then
                    Error "A manifest reference is invalid."
                else
                    Ok reference
            | Error message, _
            | _, Error message -> Error message

        let parseReferences propertyName root =
            Json.tryProperty propertyName root
            |> Result.bind (fun property ->
                if property.ValueKind <> JsonValueKind.Array then
                    Error $"Manifest property '{propertyName}' must be an array."
                else
                    property.EnumerateArray()
                    |> Seq.map parseReference
                    |> Json.collect
                    |> Result.map Set.ofList)

        try
            use document = JsonDocument.Parse json
            let root = document.RootElement

            match
                Json.tryProperty "protocolVersion" root,
                Json.tryProperty "implementation" root
                |> Result.bind (Json.requiredString "A manifest implementation"),
                parseReferences "shapes" root,
                parseReferences "operations" root
            with
            | Ok protocol, Ok implementation, Ok shapes, Ok operations when protocol.GetInt32() > 0 ->
                Ok
                    { ProtocolVersion = protocol.GetInt32()
                      Implementation = implementation
                      Shapes = shapes
                      Operations = operations }
            | Ok _, Ok _, Ok _, Ok _ -> Error "The boundary protocol version is invalid."
            | Error message, _, _, _
            | _, Error message, _, _
            | _, _, Error message, _
            | _, _, _, Error message -> Error message
        with error ->
            Error $"The boundary manifest is invalid JSON: {error.Message}"

[<RequireQualifiedAccess>]
module ValueCodec =
    let private fragmentText (reference: FragmentReference) =
        $"{CanonicalName.value reference.Name}@{reference.Version}"

    let private tryFragmentReference (text: string) =
        let separator = text.LastIndexOf '@'

        if separator < 1 then
            Error "A fragment reference must end with @version."
        else
            match
                CanonicalName.tryCreate (text.Substring(0, separator)),
                Int32.TryParse(text.Substring(separator + 1))
            with
            | Ok name, (true, version) when version > 0 ->
                Ok
                    ({ Name = name
                       Version = version }: FragmentReference)
            | Error message, _ -> Error message
            | _ -> Error "A fragment reference has an invalid version."

    let rec private writeValue (writer: Utf8JsonWriter) value =
        writer.WriteStartObject()

        match value with
        | UnitValue -> writer.WriteString("kind", "unit")
        | BooleanValue value ->
            writer.WriteString("kind", "boolean")
            writer.WriteBoolean("value", value)
        | IntegerValue value ->
            writer.WriteString("kind", "integer")
            writer.WriteNumber("value", value)
        | DecimalValue value ->
            writer.WriteString("kind", "decimal")
            writer.WriteNumber("value", value)
        | TextValue value ->
            writer.WriteString("kind", "text")
            writer.WriteString("value", value)
        | BytesValue value ->
            writer.WriteString("kind", "bytes")
            writer.WriteString("value", Convert.ToBase64String value)
        | ChoiceValue(caseName, value) ->
            writer.WriteString("kind", "choice")
            writer.WriteString("case", caseName)
            writer.WritePropertyName "value"
            writeValue writer value
        | SequenceValue values ->
            writer.WriteString("kind", "sequence")
            writer.WriteStartArray "items"
            values |> List.iter (writeValue writer)
            writer.WriteEndArray()
        | RecordValue(fields, fragments) ->
            writer.WriteString("kind", "record")
            writer.WriteStartObject "fields"
            fields
            |> Map.iter (fun name value ->
                writer.WritePropertyName name
                writeValue writer value)
            writer.WriteEndObject()
            writer.WriteStartObject "fragments"
            fragments
            |> Map.iter (fun reference value ->
                writer.WritePropertyName(fragmentText reference)
                writeValue writer value)
            writer.WriteEndObject()

        writer.WriteEndObject()

    let encode value = Json.writeText (fun writer -> writeValue writer value)

    let rec private decodeElement (element: JsonElement) : Result<ShapeValue, string> =
        let decodeValue () = Json.tryProperty "value" element |> Result.bind decodeElement

        match Json.tryProperty "kind" element with
        | Error message -> Error message
        | Ok kindProperty ->
            match kindProperty.GetString() with
            | "unit" -> Ok UnitValue
            | "boolean" -> Json.tryProperty "value" element |> Result.map (fun value -> BooleanValue(value.GetBoolean()))
            | "integer" -> Json.tryProperty "value" element |> Result.map (fun value -> IntegerValue(value.GetInt64()))
            | "decimal" -> Json.tryProperty "value" element |> Result.map (fun value -> DecimalValue(value.GetDecimal()))
            | "text" ->
                Json.tryProperty "value" element
                |> Result.bind (Json.requiredString "A text ShapeValue")
                |> Result.map TextValue
            | "bytes" ->
                Json.tryProperty "value" element
                |> Result.bind (fun value ->
                    try
                        Ok(BytesValue(value.GetBytesFromBase64()))
                    with error ->
                        Error $"The byte value is invalid: {error.Message}")
            | "choice" ->
                match
                    Json.tryProperty "case" element |> Result.bind (Json.requiredString "A choice case"),
                    decodeValue ()
                with
                | Ok caseName, Ok value -> Ok(ChoiceValue(caseName, value))
                | Error message, _
                | _, Error message -> Error message
            | "sequence" ->
                Json.tryProperty "items" element
                |> Result.bind (fun items ->
                    if items.ValueKind <> JsonValueKind.Array then
                        Error "Sequence items must be an array."
                    else
                        items.EnumerateArray()
                        |> Seq.map decodeElement
                        |> Json.collect
                        |> Result.map SequenceValue)
            | "record" ->
                let decodeFields (fields: JsonElement) =
                    fields.EnumerateObject()
                    |> Seq.map (fun property -> decodeElement property.Value |> Result.map (fun value -> property.Name, value))
                    |> Json.collect
                    |> Result.map Map.ofList

                let decodeFragments (fragments: JsonElement) =
                    fragments.EnumerateObject()
                    |> Seq.map (fun property ->
                        match tryFragmentReference property.Name, decodeElement property.Value with
                        | Ok reference, Ok value -> Ok(reference, value)
                        | Error message, _
                        | _, Error message -> Error message)
                    |> Json.collect
                    |> Result.map Map.ofList

                match Json.tryProperty "fields" element, Json.tryProperty "fragments" element with
                | Ok fields, Ok fragments when
                    fields.ValueKind = JsonValueKind.Object
                    && fragments.ValueKind = JsonValueKind.Object ->
                    match decodeFields fields, decodeFragments fragments with
                    | Ok decodedFields, Ok decodedFragments -> Ok(RecordValue(decodedFields, decodedFragments))
                    | Error message, _
                    | _, Error message -> Error message
                | Ok _, Ok _ -> Error "Record fields and fragments must be objects."
                | Error message, _
                | _, Error message -> Error message
            | kind -> Error $"Unknown ShapeValue kind '{kind}'."

    let decode (json: string) =
        try
            use document = JsonDocument.Parse json
            decodeElement document.RootElement
        with error ->
            Error $"The encoded ShapeValue is invalid JSON: {error.Message}"
