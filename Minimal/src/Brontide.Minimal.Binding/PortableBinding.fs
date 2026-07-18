namespace Brontide.Minimal.Binding

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open Brontide.Minimal.Model

[<Struct; StructuralEquality; StructuralComparison>]
type BindingRequestId = private BindingRequestId of Guid

[<RequireQualifiedAccess>]
module BindingRequestId =
    let create () = BindingRequestId(Guid.NewGuid())
    let value (BindingRequestId value) = value
    let parse (value: string) = BindingRequestId(Guid.Parse value)
    let text id = value id |> _.ToString("D")

[<Struct; StructuralEquality; StructuralComparison>]
type BindingExecutionId = private BindingExecutionId of Guid

[<RequireQualifiedAccess>]
module BindingExecutionId =
    let create () = BindingExecutionId(Guid.NewGuid())
    let value (BindingExecutionId value) = value
    let parse (value: string) = BindingExecutionId(Guid.Parse value)
    let text id = value id |> _.ToString("D")

[<Struct; StructuralEquality; StructuralComparison>]
type BindingOccurrenceId = private BindingOccurrenceId of Guid

[<RequireQualifiedAccess>]
module BindingOccurrenceId =
    let create () = BindingOccurrenceId(Guid.NewGuid())
    let value (BindingOccurrenceId value) = value
    let parse (value: string) = BindingOccurrenceId(Guid.Parse value)
    let text id = value id |> _.ToString("D")

[<StructuralEquality; StructuralComparison>]
type PortableReference =
    { Name: string
      Version: int }

[<StructuralEquality; StructuralComparison>]
type PortableField =
    { Name: string
      Shape: PortableReference
      Required: bool }

[<StructuralEquality; StructuralComparison>]
type PortableShape =
    { Reference: PortableReference
      Kind: string
      FragmentPolicy: string
      Fields: PortableField list }

[<StructuralEquality; StructuralComparison>]
type PortableFragment =
    { Reference: PortableReference
      HostShape: PortableReference
      Fields: PortableField list }

[<StructuralEquality; StructuralComparison>]
type PortableAuthorityRequirement =
    { HostDecisionRequired: bool
      ConstraintPolicy: string }

[<StructuralEquality; StructuralComparison>]
type PortableOperation =
    { Reference: PortableReference
      InputShape: PortableReference
      OutputShape: PortableReference
      RequiredFragments: PortableReference list
      Authority: PortableAuthorityRequirement }

[<StructuralEquality; StructuralComparison>]
type PortableDependency =
    { Kind: string
      Reference: PortableReference
      Strength: string
      ProviderSpecific: bool }

[<StructuralEquality; StructuralComparison>]
type PortableBindingDeclaration =
    { Representations: string list
      CrossedBoundaries: string list
      Limitations: string list }

[<StructuralEquality; StructuralComparison>]
type PortableManifest =
    { ProtocolVersion: int
      Component: PortableReference
      Provider: PortableReference
      Operations: PortableOperation list
      Shapes: PortableShape list
      Fragments: PortableFragment list
      Dependencies: PortableDependency list
      Binding: PortableBindingDeclaration }

exception PortableBindingException of string

module private PortableJson =
    let forbidden =
        set [ "$type"; "typename"; "exception"; "stacktrace"; "innerexception"; "targetsite" ]

    let rec private validate (element: JsonElement) =
        match element.ValueKind with
        | JsonValueKind.Object ->
            let names = HashSet<string>(StringComparer.Ordinal)

            element.EnumerateObject()
            |> Seq.iter (fun property ->
                if not (names.Add property.Name) then
                    raise (PortableBindingException $"Duplicate protected field '{property.Name}' is not permitted.")

                if Set.contains (property.Name.ToLowerInvariant()) forbidden then
                    raise (
                        PortableBindingException
                            $"Private CLR type or exception metadata field '{property.Name}' is not permitted."
                    )

                validate property.Value)
        | JsonValueKind.Array -> element.EnumerateArray() |> Seq.iter validate
        | _ -> ()

    let parse (json: string) =
        try
            let document =
                JsonDocument.Parse(
                    json,
                    JsonDocumentOptions(AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 64)
                )

            try
                validate document.RootElement
                Ok document
            with error ->
                document.Dispose()
                raise error
        with
        | PortableBindingException message -> Error message
        | error -> Error $"The boundary JSON is invalid: {error.Message}"

    let required (name: string) (element: JsonElement) =
        let mutable property = Unchecked.defaultof<JsonElement>

        if element.TryGetProperty(name, &property) then
            property
        else
            raise (PortableBindingException $"The JSON value is missing '{name}'.")

    let requiredString description (element: JsonElement) =
        match element.GetString() |> Option.ofObj with
        | Some value -> value
        | None -> raise (PortableBindingException $"{description} cannot be null.")

    let writeText write =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = false, SkipValidation = false))
        write writer
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    let writeRaw (writer: Utf8JsonWriter) (json: string) =
        use document = JsonDocument.Parse json
        document.RootElement.WriteTo writer

[<RequireQualifiedAccess>]
module PortableContract =
    [<Literal>]
    let protocolVersion = 2

    let private reference name version : PortableReference =
        { Name = name
          Version = version }

    let componentContract = reference "interchange.tests.cooling-component" 1
    let operation = reference "interchange.tests.cooling.set-enabled" 1
    let commandShape = reference "interchange.tests.cooling.command" 1
    let resultShape = reference "interchange.tests.cooling.result" 1
    let detailsShape = reference "interchange.tests.cooling.details" 1
    let hostContext = reference "interchange.tests.cooling.host-context" 1
    let optionalForwardingNote = reference "third-party.cooling.note" 1

    let private field name shape required : PortableField =
        { Name = name
          Shape = shape
          Required = required }

    let manifest providerName =
        { ProtocolVersion = protocolVersion
          Component = componentContract
          Provider = reference providerName 1
          Operations =
            [ { Reference = operation
                InputShape = commandShape
                OutputShape = resultShape
                RequiredFragments = [ hostContext ]
                Authority =
                  { HostDecisionRequired = true
                    ConstraintPolicy = "fail-closed" } } ]
          Shapes =
            [ { Reference = commandShape
                Kind = "record"
                FragmentPolicy = "open"
                Fields =
                  [ field "loop" (reference "Text" 1) true
                    field "enabled" (reference "Boolean" 1) true
                    field "failureMode" (reference "Text" 1) false ] }
              { Reference = resultShape
                Kind = "record"
                FragmentPolicy = "closed"
                Fields =
                  [ field "loop" (reference "Text" 1) true
                    field "coolingEnabled" (reference "Boolean" 1) true
                    field "revision" (reference "Integer.Signed64" 1) true
                    field "providerEffectCount" (reference "Integer.Signed64" 1) true ] }
              { Reference = detailsShape
                Kind = "record"
                FragmentPolicy = "closed"
                Fields =
                  [ field "code" (reference "Text" 1) true
                    field "message" (reference "Text" 1) true ] } ]
          Fragments =
            [ { Reference = hostContext
                HostShape = commandShape
                Fields = [ field "requesterLabel" (reference "Text" 1) true ] } ]
          Dependencies =
            [ { Kind = "profile"
                Reference = reference "interchange.tests.cooling-profile" 1
                Strength = "required"
                ProviderSpecific = false }
              { Kind = "binding"
                Reference = reference "interchange.tests.inline-tagged-json" 1
                Strength = "required"
                ProviderSpecific = true } ]
          Binding =
            { Representations = [ "inline-tagged-json" ]
              CrossedBoundaries = [ "process" ]
              Limitations = [ "single-invocation"; "no-capability-transfer"; "no-referenced-resources" ] } }

    let fragmentReference (reference: PortableReference) : FragmentReference =
        { Name = CanonicalName.create reference.Name
          Version = reference.Version }

    let shapeReference (reference: PortableReference) : ShapeReference =
        { Name = CanonicalName.create reference.Name
          Version = reference.Version }

    let operationReference (reference: PortableReference) : OperationReference =
        { Name = CanonicalName.create reference.Name }

    let command loop enabled failureMode requesterLabel forwardingNote =
        let canonicalFields =
            [ "loop", TextValue loop; "enabled", BooleanValue enabled ]
            @ (failureMode |> Option.map (fun value -> [ "failureMode", TextValue value ]) |> Option.defaultValue [])

        let fragments =
            [ requesterLabel
              |> Option.map (fun value ->
                  fragmentReference hostContext,
                  RecordValue(Map.ofList [ "requesterLabel", TextValue value ], Map.empty))
              forwardingNote
              |> Option.map (fun value ->
                  fragmentReference optionalForwardingNote,
                  RecordValue(Map.ofList [ "note", TextValue value ], Map.empty)) ]
            |> List.choose id
            |> Map.ofList

        RecordValue(Map.ofList canonicalFields, fragments)

    let result loop enabled revision effectCount =
        RecordValue(
            Map.ofList
                [ "loop", TextValue loop
                  "coolingEnabled", BooleanValue enabled
                  "revision", IntegerValue revision
                  "providerEffectCount", IntegerValue effectCount ],
            Map.empty
        )

    let details code message =
        RecordValue(
            Map.ofList [ "code", TextValue code; "message", TextValue message ],
            Map.empty
        )

    let validateCommand value =
        match value with
        | RecordValue(fields, fragments) ->
            match Map.tryFind "loop" fields, Map.tryFind "enabled" fields with
            | Some(TextValue _), Some(BooleanValue _) ->
                let requiredFragment = fragmentReference hostContext

                match Map.tryFind requiredFragment fragments with
                | Some(RecordValue(fragmentFields, _)) ->
                    match Map.tryFind "requesterLabel" fragmentFields with
                    | Some(TextValue _) -> Ok()
                    | _ -> Error "The host-context requesterLabel has the wrong kind."
                | _ -> Error $"The required fragment {hostContext.Name}@{hostContext.Version} is missing."
            | _, Some _ -> Error "The Cooling enabled field has the wrong kind."
            | _ -> Error "The Cooling command is missing a required canonical field."
        | _ -> Error "The Cooling command must be a record value."

    let readCommand value =
        validateCommand value
        |> Result.bind (fun () ->
            match value with
            | RecordValue(fields, _) ->
                match fields["loop"], fields["enabled"] with
                | TextValue loop, BooleanValue enabled ->
                    let failureMode =
                        match Map.tryFind "failureMode" fields with
                        | Some(TextValue value) -> Some value
                        | _ -> None

                    Ok(loop, enabled, failureMode)
                | _ -> Error "The Cooling command has incorrectly shaped fields."
            | _ -> Error "The Cooling command must be a record value.")

    let validateResult value =
        match value with
        | RecordValue(fields, fragments) when Map.isEmpty fragments ->
            match
                Map.tryFind "loop" fields,
                Map.tryFind "coolingEnabled" fields,
                Map.tryFind "revision" fields,
                Map.tryFind "providerEffectCount" fields
            with
            | Some(TextValue _), Some(BooleanValue _), Some(IntegerValue _), Some(IntegerValue _) -> Ok()
            | _ -> Error "The Cooling result has missing or incorrectly shaped fields."
        | _ -> Error "The Cooling result must be a closed record value."

    let validateDetails value =
        match value with
        | RecordValue(fields, fragments) when Map.isEmpty fragments ->
            match Map.tryFind "code" fields, Map.tryFind "message" fields with
            | Some(TextValue _), Some(TextValue _) -> Ok()
            | _ -> Error "The Cooling details have missing or incorrectly shaped fields."
        | _ -> Error "The Cooling details must be a closed record value."

[<RequireQualifiedAccess>]
module PortableManifestCodec =
    let private parseReference (element: JsonElement) : PortableReference =
        let reference =
            { Name = PortableJson.required "name" element |> PortableJson.requiredString "A reference name"
              Version = PortableJson.required "version" element |> _.GetInt32() }

        match CanonicalName.tryCreate reference.Name with
        | Error message -> raise (PortableBindingException message)
        | Ok _ when reference.Version < 1 -> raise (PortableBindingException "A reference version is invalid.")
        | Ok _ -> reference

    let private parseField (element: JsonElement) : PortableField =
        { Name = PortableJson.required "name" element |> PortableJson.requiredString "A field name"
          Shape = PortableJson.required "shape" element |> parseReference
          Required = PortableJson.required "required" element |> _.GetBoolean() }

    let private parseFields (element: JsonElement) =
        PortableJson.required "fields" element
        |> _.EnumerateArray()
        |> Seq.map parseField
        |> Seq.toList

    let tryFromJson json =
        match PortableJson.parse json with
        | Error message -> Error message
        | Ok document ->
            use document = document

            try
                let root = document.RootElement

                let operations =
                    PortableJson.required "operations" root
                    |> _.EnumerateArray()
                    |> Seq.map (fun element ->
                        let authority = PortableJson.required "authority" element

                        { Reference = PortableJson.required "reference" element |> parseReference
                          InputShape = PortableJson.required "inputShape" element |> parseReference
                          OutputShape = PortableJson.required "outputShape" element |> parseReference
                          RequiredFragments =
                            PortableJson.required "requiredFragments" element
                            |> _.EnumerateArray()
                            |> Seq.map parseReference
                            |> Seq.toList
                          Authority =
                            { HostDecisionRequired =
                                PortableJson.required "hostDecisionRequired" authority |> _.GetBoolean()
                              ConstraintPolicy =
                                PortableJson.required "constraintPolicy" authority
                                |> PortableJson.requiredString "A constraint policy" } })
                    |> Seq.toList

                let shapes =
                    PortableJson.required "shapes" root
                    |> _.EnumerateArray()
                    |> Seq.map (fun element ->
                        { Reference = PortableJson.required "reference" element |> parseReference
                          Kind = PortableJson.required "kind" element |> PortableJson.requiredString "A Shape kind"
                          FragmentPolicy =
                            PortableJson.required "fragmentPolicy" element
                            |> PortableJson.requiredString "A Fragment policy"
                          Fields = parseFields element })
                    |> Seq.toList

                let fragments =
                    PortableJson.required "fragments" root
                    |> _.EnumerateArray()
                    |> Seq.map (fun element ->
                        { Reference = PortableJson.required "reference" element |> parseReference
                          HostShape = PortableJson.required "hostShape" element |> parseReference
                          Fields = parseFields element })
                    |> Seq.toList

                let dependencies =
                    PortableJson.required "dependencies" root
                    |> _.EnumerateArray()
                    |> Seq.map (fun element ->
                        { Kind = PortableJson.required "kind" element |> PortableJson.requiredString "A dependency kind"
                          Reference = PortableJson.required "reference" element |> parseReference
                          Strength =
                            PortableJson.required "strength" element
                            |> PortableJson.requiredString "A dependency strength"
                          ProviderSpecific = PortableJson.required "providerSpecific" element |> _.GetBoolean() })
                    |> Seq.toList

                let binding = PortableJson.required "binding" root

                let stringList name (element: JsonElement) =
                    PortableJson.required name element
                    |> _.EnumerateArray()
                    |> Seq.map (PortableJson.requiredString name)
                    |> Seq.toList

                let manifest =
                    { ProtocolVersion = PortableJson.required "protocolVersion" root |> _.GetInt32()
                      Component = PortableJson.required "component" root |> parseReference
                      Provider = PortableJson.required "provider" root |> parseReference
                      Operations = operations
                      Shapes = shapes
                      Fragments = fragments
                      Dependencies = dependencies
                      Binding =
                        { Representations = stringList "representations" binding
                          CrossedBoundaries = stringList "crossedBoundaries" binding
                          Limitations = stringList "limitations" binding } }

                if manifest.ProtocolVersion <> PortableContract.protocolVersion then
                    Error $"Unsupported protocol version {manifest.ProtocolVersion}."
                elif
                    manifest.Operations
                    |> List.exists (fun operation ->
                        not operation.Authority.HostDecisionRequired
                        || operation.Authority.ConstraintPolicy <> "fail-closed")
                then
                    Error "The test contract requires a fail-closed host authority decision."
                elif not (List.contains "inline-tagged-json" manifest.Binding.Representations) then
                    Error "The manifest does not offer inline tagged JSON."
                elif not (List.contains "no-capability-transfer" manifest.Binding.Limitations) then
                    Error "The manifest does not prohibit Capability transfer."
                else
                    Ok manifest
            with
            | PortableBindingException message -> Error message
            | error -> Error $"The portable manifest is invalid: {error.Message}"

    let private writeReference (writer: Utf8JsonWriter) (reference: PortableReference) =
        writer.WriteStartObject()
        writer.WriteString("name", reference.Name)
        writer.WriteNumber("version", reference.Version)
        writer.WriteEndObject()

    let private writeReferenceProperty
        (name: string)
        (writer: Utf8JsonWriter)
        (reference: PortableReference)
        =
        writer.WritePropertyName name
        writeReference writer reference

    let private writeFields (writer: Utf8JsonWriter) (fields: PortableField list) =
        writer.WriteStartArray("fields")

        fields
        |> List.iter (fun field ->
            writer.WriteStartObject()
            writer.WriteString("name", field.Name)
            writeReferenceProperty "shape" writer field.Shape
            writer.WriteBoolean("required", field.Required)
            writer.WriteEndObject())

        writer.WriteEndArray()

    let toJson (manifest: PortableManifest) =
        PortableJson.writeText (fun writer ->
            writer.WriteStartObject()
            writer.WriteNumber("protocolVersion", manifest.ProtocolVersion)
            writeReferenceProperty "component" writer manifest.Component
            writeReferenceProperty "provider" writer manifest.Provider
            writer.WriteStartArray("operations")

            manifest.Operations
            |> List.iter (fun operation ->
                writer.WriteStartObject()
                writeReferenceProperty "reference" writer operation.Reference
                writeReferenceProperty "inputShape" writer operation.InputShape
                writeReferenceProperty "outputShape" writer operation.OutputShape
                writer.WriteStartArray("requiredFragments")
                operation.RequiredFragments |> List.iter (writeReference writer)
                writer.WriteEndArray()
                writer.WriteStartObject("authority")
                writer.WriteBoolean("hostDecisionRequired", operation.Authority.HostDecisionRequired)
                writer.WriteString("constraintPolicy", operation.Authority.ConstraintPolicy)
                writer.WriteEndObject()
                writer.WriteEndObject())

            writer.WriteEndArray()
            writer.WriteStartArray("shapes")

            manifest.Shapes
            |> List.iter (fun shape ->
                writer.WriteStartObject()
                writeReferenceProperty "reference" writer shape.Reference
                writer.WriteString("kind", shape.Kind)
                writer.WriteString("fragmentPolicy", shape.FragmentPolicy)
                writeFields writer shape.Fields
                writer.WriteEndObject())

            writer.WriteEndArray()
            writer.WriteStartArray("fragments")

            manifest.Fragments
            |> List.iter (fun fragment ->
                writer.WriteStartObject()
                writeReferenceProperty "reference" writer fragment.Reference
                writeReferenceProperty "hostShape" writer fragment.HostShape
                writeFields writer fragment.Fields
                writer.WriteEndObject())

            writer.WriteEndArray()
            writer.WriteStartArray("dependencies")

            manifest.Dependencies
            |> List.iter (fun dependency ->
                writer.WriteStartObject()
                writer.WriteString("kind", dependency.Kind)
                writeReferenceProperty "reference" writer dependency.Reference
                writer.WriteString("strength", dependency.Strength)
                writer.WriteBoolean("providerSpecific", dependency.ProviderSpecific)
                writer.WriteEndObject())

            writer.WriteEndArray()
            writer.WriteStartObject("binding")

            let writeStrings (name: string) (values: string list) =
                writer.WriteStartArray name
                values |> List.iter (fun value -> writer.WriteStringValue value)
                writer.WriteEndArray()

            writeStrings "representations" manifest.Binding.Representations
            writeStrings "crossedBoundaries" manifest.Binding.CrossedBoundaries
            writeStrings "limitations" manifest.Binding.Limitations
            writer.WriteEndObject()
            writer.WriteEndObject())

    let negotiateExact (required: PortableManifest) (offered: PortableManifest) =
        let missing
            (kind: string)
            (requiredItems: PortableReference list)
            (offeredItems: PortableReference list)
            =
            requiredItems
            |> List.tryFind (fun reference -> not (List.contains reference offeredItems))
            |> Option.map (fun reference -> $"Required {kind} {reference.Name}@{reference.Version} was not negotiated.")

        if required.ProtocolVersion <> offered.ProtocolVersion then
            Error "The boundary protocol versions are incompatible."
        elif required.Component <> offered.Component then
            Error $"Required component {required.Component.Name}@{required.Component.Version} was not offered."
        else
            let checks =
                [ missing
                      "Operation"
                      (required.Operations |> List.map _.Reference)
                      (offered.Operations |> List.map _.Reference)
                  missing "Shape" (required.Shapes |> List.map _.Reference) (offered.Shapes |> List.map _.Reference)
                  missing
                      "Fragment"
                      (required.Fragments |> List.map _.Reference)
                      (offered.Fragments |> List.map _.Reference) ]

            match checks |> List.choose id |> List.tryHead with
            | Some message -> Error message
            | None ->
                let incompatibleOperation =
                    required.Operations
                    |> List.tryPick (fun operation ->
                        offered.Operations
                        |> List.tryFind (fun candidate -> candidate.Reference = operation.Reference)
                        |> Option.bind (fun candidate ->
                            if
                                candidate.InputShape <> operation.InputShape
                                || candidate.OutputShape <> operation.OutputShape
                                || candidate.RequiredFragments <> operation.RequiredFragments
                            then
                                Some
                                    $"Operation {operation.Reference.Name}@{operation.Reference.Version} has incompatible Shape or Fragment declarations."
                            else
                                None))

                match incompatibleOperation with
                | Some message -> Error message
                | None ->
                    let missingDependency =
                        required.Dependencies
                        |> List.filter (fun dependency -> dependency.Strength = "required")
                        |> List.tryFind (fun dependency ->
                            offered.Dependencies
                            |> List.contains dependency
                            |> not)

                    match missingDependency with
                    | Some dependency ->
                        Error
                            $"Required dependency {dependency.Reference.Name}@{dependency.Reference.Version} was not offered."
                    | None -> Ok()

[<RequireQualifiedAccess>]
module PortableValueCodec =
    let encode value = ValueCodec.encode value

    let decode json =
        match PortableJson.parse json with
        | Error message -> Error message
        | Ok document ->
            document.Dispose()
            ValueCodec.decode json

type ProviderLaunch =
    { FileName: string
      Arguments: string }

type PortableProviderEffect =
    { Succeeded: bool
      Value: ShapeValue
      ProviderEffectCount: int64 }

type PortableInvocationResult =
    { Succeeded: bool
      Value: ShapeValue
      ForwardedInput: ShapeValue
      Provider: string
      ProviderEffectCount: int64 }

type PortableBindingFailure =
    { Message: string
      FailureDomain: string
      Interrupted: bool
      ProviderProcessFailure: bool }

module private PortableProtocol =
    let start (kind: string) (request: BindingRequestId) (writer: Utf8JsonWriter) =
        writer.WriteStartObject()
        writer.WriteNumber("protocolVersion", PortableContract.protocolVersion)
        writer.WriteString("kind", kind)
        writer.WriteString("requestId", BindingRequestId.text request)

    let writeReferenceProperty
        (name: string)
        (writer: Utf8JsonWriter)
        (reference: PortableReference)
        =
        writer.WriteStartObject name
        writer.WriteString("name", reference.Name)
        writer.WriteNumber("version", reference.Version)
        writer.WriteEndObject()

    let activate (request: BindingRequestId) (manifest: PortableManifest) =
        PortableJson.writeText (fun writer ->
            start "activate" request writer
            writer.WritePropertyName "manifest"
            PortableJson.writeRaw writer (PortableManifestCodec.toJson manifest)
            writer.WriteEndObject())

    let invoke
        (request: BindingRequestId)
        (execution: BindingExecutionId)
        (occurrence: BindingOccurrenceId)
        (input: ShapeValue)
        =
        PortableJson.writeText (fun writer ->
            start "invoke" request writer
            writer.WriteString("executionId", BindingExecutionId.text execution)
            writer.WriteString("occurrenceId", BindingOccurrenceId.text occurrence)
            writeReferenceProperty "operation" writer PortableContract.operation
            writeReferenceProperty "inputShape" writer PortableContract.commandShape
            writeReferenceProperty "outputShape" writer PortableContract.resultShape
            writer.WritePropertyName "input"
            PortableJson.writeRaw writer (PortableValueCodec.encode input)
            writer.WriteEndObject())

    let shutdown (request: BindingRequestId) =
        PortableJson.writeText (fun writer ->
            start "shutdown" request writer
            writer.WriteEndObject())

    let activation (request: BindingRequestId) (manifest: PortableManifest) =
        PortableJson.writeText (fun writer ->
            start "activation" request writer
            writer.WriteBoolean("accepted", true)
            writer.WritePropertyName "manifest"
            PortableJson.writeRaw writer (PortableManifestCodec.toJson manifest)
            writer.WriteEndObject())

    let protocolError (request: BindingRequestId) (code: string) (message: string) =
        PortableJson.writeText (fun writer ->
            start "protocol-error" request writer
            writer.WriteString("code", code)
            writer.WriteString("message", message)
            writer.WriteEndObject())

    let shutdownAck (request: BindingRequestId) =
        PortableJson.writeText (fun writer ->
            start "shutdown-ack" request writer
            writer.WriteEndObject())

    let outcome
        (request: BindingRequestId)
        (execution: BindingExecutionId)
        (occurrence: BindingOccurrenceId)
        (effect: PortableProviderEffect)
        (forwardedInput: JsonElement)
        (provider: PortableReference)
        =
        PortableJson.writeText (fun writer ->
            start "outcome" request writer
            writer.WriteString("executionId", BindingExecutionId.text execution)
            writer.WriteString("occurrenceId", BindingOccurrenceId.text occurrence)
            writer.WriteString("status", if effect.Succeeded then "succeeded" else "failed")
            writeReferenceProperty "provider" writer provider
            writer.WriteNumber("providerEffectCount", effect.ProviderEffectCount)
            writer.WritePropertyName(if effect.Succeeded then "result" else "details")
            PortableJson.writeRaw writer (PortableValueCodec.encode effect.Value)
            writer.WritePropertyName "forwardedInput"
            forwardedInput.WriteTo writer
            writer.WriteEndObject())

    let requestId (root: JsonElement) =
        PortableJson.required "requestId" root
        |> PortableJson.requiredString "A request id"
        |> BindingRequestId.parse

    let executionId (root: JsonElement) =
        PortableJson.required "executionId" root
        |> PortableJson.requiredString "An Execution id"
        |> BindingExecutionId.parse

    let occurrenceId (root: JsonElement) =
        PortableJson.required "occurrenceId" root
        |> PortableJson.requiredString "An occurrence id"
        |> BindingOccurrenceId.parse

    let reference (name: string) (root: JsonElement) : PortableReference =
        let element = PortableJson.required name root

        { Name = PortableJson.required "name" element |> PortableJson.requiredString name
          Version = PortableJson.required "version" element |> _.GetInt32() }

[<RequireQualifiedAccess>]
module ProcessBindingClient =
    let private failure
        (message: string)
        (domain: string)
        (interrupted: bool)
        (processFailure: bool)
        : Result<PortableInvocationResult, PortableBindingFailure>
        =
        Error
            { Message = message
              FailureDomain = domain
              Interrupted = interrupted
              ProviderProcessFailure = processFailure }

    let invoke
        (launch: ProviderLaunch)
        (timeout: TimeSpan)
        (requiredManifest: PortableManifest)
        (request: BindingRequestId)
        (execution: BindingExecutionId)
        (occurrence: BindingOccurrenceId)
        (input: ShapeValue)
        =
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- launch.FileName
        startInfo.Arguments <- launch.Arguments
        startInfo.RedirectStandardInput <- true
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false
        startInfo.CreateNoWindow <- true
        use providerProcess = new Process(StartInfo = startInfo)
        let mutable started = false

        let send (line: string) =
            providerProcess.StandardInput.WriteLine line
            providerProcess.StandardInput.Flush()

        let read (stage: string) =
            let pending = providerProcess.StandardOutput.ReadLineAsync()

            if not (pending.Wait timeout) then
                raise (TimeoutException $"The provider process timed out during {stage}.")

            match pending.Result |> Option.ofObj with
            | Some line -> line
            | None ->
                let diagnostics = providerProcess.StandardError.ReadToEnd()

                raise (
                    IOException(
                        if String.IsNullOrWhiteSpace diagnostics then
                            $"The provider process ended during {stage}."
                        else
                            $"The provider process ended during {stage}; diagnostics: {diagnostics.Trim()}"
                    )
                )

        let run () =
            if not (providerProcess.Start()) then
                failure "The provider process did not start." "provider-process:start" true true
            else
                started <- true
                send (PortableProtocol.activate request requiredManifest)
                let activationLine = read "activation"

                match PortableJson.parse activationLine with
                | Error message -> failure message "binding-protocol" true false
                | Ok activationDocument ->
                    use activationDocument = activationDocument
                    let root = activationDocument.RootElement
                    let kind = PortableJson.required "kind" root |> PortableJson.requiredString "A message kind"

                    if kind = "protocol-error" then
                        let message =
                            PortableJson.required "message" root
                            |> PortableJson.requiredString "A protocol error message"

                        failure message "binding-negotiation" false false
                    elif kind <> "activation" then
                        failure "The provider did not return activation." "binding-protocol" true false
                    else
                        let offeredJson = PortableJson.required "manifest" root |> _.GetRawText()

                        match PortableManifestCodec.tryFromJson offeredJson with
                        | Error message -> failure message "binding-negotiation" false false
                        | Ok offered ->
                            match
                                PortableManifestCodec.negotiateExact requiredManifest offered,
                                PortableManifestCodec.negotiateExact offered requiredManifest
                            with
                            | Error message, _
                            | _, Error message -> failure message "binding-negotiation" false false
                            | Ok(), Ok() ->
                                send (PortableProtocol.invoke request execution occurrence input)
                                let outcomeLine = read "outcome"

                                match PortableJson.parse outcomeLine with
                                | Error message -> failure message "binding-protocol" true false
                                | Ok outcomeDocument ->
                                    use outcomeDocument = outcomeDocument
                                    let outcomeRoot = outcomeDocument.RootElement
                                    let outcomeKind =
                                        PortableJson.required "kind" outcomeRoot
                                        |> PortableJson.requiredString "A message kind"

                                    if outcomeKind = "protocol-error" then
                                        let message =
                                            PortableJson.required "message" outcomeRoot
                                            |> PortableJson.requiredString "A protocol error message"

                                        failure message "binding-protocol" true false
                                    elif
                                        outcomeKind <> "outcome"
                                        || PortableProtocol.requestId outcomeRoot <> request
                                        || PortableProtocol.executionId outcomeRoot <> execution
                                        || PortableProtocol.occurrenceId outcomeRoot <> occurrence
                                    then
                                        failure
                                            "The provider Outcome identifiers do not match the invocation."
                                            "binding-protocol"
                                            true
                                            false
                                    else
                                        let status =
                                            PortableJson.required "status" outcomeRoot
                                            |> PortableJson.requiredString "An Outcome status"

                                        let succeeded = status = "succeeded"

                                        if not succeeded && status <> "failed" then
                                            failure "The provider returned an unknown Outcome status." "binding-protocol" true false
                                        else
                                            let valueProperty = if succeeded then "result" else "details"

                                            match
                                                PortableJson.required valueProperty outcomeRoot
                                                |> _.GetRawText()
                                                |> PortableValueCodec.decode,
                                                PortableJson.required "forwardedInput" outcomeRoot
                                                |> _.GetRawText()
                                                |> PortableValueCodec.decode
                                            with
                                            | Ok value, Ok forwarded ->
                                                let valid =
                                                    if succeeded then
                                                        PortableContract.validateResult value
                                                    else
                                                        PortableContract.validateDetails value

                                                match valid with
                                                | Error message -> failure message "binding-protocol" true false
                                                | Ok() ->
                                                    let provider = PortableProtocol.reference "provider" outcomeRoot
                                                    let effectCount =
                                                        PortableJson.required "providerEffectCount" outcomeRoot
                                                        |> _.GetInt64()

                                                    send (PortableProtocol.shutdown request)
                                                    ignore (read "shutdown acknowledgement")

                                                    Ok
                                                        { Succeeded = succeeded
                                                          Value = value
                                                          ForwardedInput = forwarded
                                                          Provider = provider.Name
                                                          ProviderEffectCount = effectCount }
                                            | Error message, _
                                            | _, Error message -> failure message "binding-protocol" true false
        let result =
            try
                run ()
            with
            | :? TimeoutException as error -> failure error.Message "provider-process:timeout" true true
            | :? IOException as error -> failure error.Message "provider-process:exchange" true true
            | PortableBindingException message -> failure message "binding-protocol" true false
            | error -> failure $"The provider process failed: {error.Message}" "provider-process:exchange" true true

        if started && not providerProcess.HasExited then
            providerProcess.Kill(true)
            providerProcess.WaitForExit()

        result

[<RequireQualifiedAccess>]
module PortableProviderEndpoint =
    let run
        (providerName: string)
        (crashAfterActivation: bool)
        (rejectProtocol: bool)
        (invoke: (string * bool * string option) -> PortableProviderEffect)
        =
        let manifest = PortableContract.manifest providerName
        let provider = manifest.Provider
        let mutable activated = false
        let mutable running = true
        let mutable exitCode = 0

        let write (line: string) =
            Console.Out.WriteLine line
            Console.Out.Flush()

        while running do
            match Console.In.ReadLine() |> Option.ofObj with
            | None -> running <- false
            | Some line ->
                let mutable request = BindingRequestId.parse(Guid.Empty.ToString("D"))

                try
                    match PortableJson.parse line with
                    | Error message -> write (PortableProtocol.protocolError request "invalid-message" message)
                    | Ok document ->
                        use document = document
                        let root = document.RootElement
                        request <- PortableProtocol.requestId root
                        let version = PortableJson.required "protocolVersion" root |> _.GetInt32()
                        let kind = PortableJson.required "kind" root |> PortableJson.requiredString "A message kind"

                        if version <> PortableContract.protocolVersion then
                            write (
                                PortableProtocol.protocolError
                                    request
                                    "unsupported-protocol"
                                    $"Protocol version {version} is not supported."
                            )
                        else
                            match kind with
                            | "activate" when rejectProtocol ->
                                write (
                                    PortableProtocol.protocolError
                                        request
                                        "unsupported-protocol"
                                        "The provider deliberately rejected protocol version 2."
                                )
                            | "activate" ->
                                let offeredJson = PortableJson.required "manifest" root |> _.GetRawText()

                                match PortableManifestCodec.tryFromJson offeredJson with
                                | Error message ->
                                    write (PortableProtocol.protocolError request "incompatible-manifest" message)
                                | Ok offered ->
                                    match
                                        PortableManifestCodec.negotiateExact manifest offered,
                                        PortableManifestCodec.negotiateExact offered manifest
                                    with
                                    | Error message, _
                                    | _, Error message ->
                                        write (PortableProtocol.protocolError request "incompatible-manifest" message)
                                    | Ok(), Ok() ->
                                        activated <- true
                                        write (PortableProtocol.activation request manifest)

                                        if crashAfterActivation then
                                            exitCode <- 23
                                            running <- false
                            | "invoke" when not activated ->
                                write (
                                    PortableProtocol.protocolError
                                        request
                                        "not-activated"
                                        "The provider has not negotiated a contract."
                                )
                            | "invoke" ->
                                let execution = PortableProtocol.executionId root
                                let occurrence = PortableProtocol.occurrenceId root

                                if
                                    PortableProtocol.reference "operation" root <> PortableContract.operation
                                    || PortableProtocol.reference "inputShape" root <> PortableContract.commandShape
                                    || PortableProtocol.reference "outputShape" root <> PortableContract.resultShape
                                then
                                    write (
                                        PortableProtocol.protocolError
                                            request
                                            "unsupported-contract"
                                            "The invocation names an unsupported Operation or Shape."
                                    )
                                else
                                    let inputElement = PortableJson.required "input" root

                                    match PortableValueCodec.decode (inputElement.GetRawText()) with
                                    | Error message -> write (PortableProtocol.protocolError request "invalid-value" message)
                                    | Ok value ->
                                        match PortableContract.readCommand value with
                                        | Error message ->
                                            write (PortableProtocol.protocolError request "invalid-value" message)
                                        | Ok command ->
                                            let effect: PortableProviderEffect = invoke command
                                            write (
                                                PortableProtocol.outcome
                                                    request
                                                    execution
                                                    occurrence
                                                    effect
                                                    inputElement
                                                    provider
                                            )
                            | "shutdown" ->
                                write (PortableProtocol.shutdownAck request)
                                running <- false
                            | _ ->
                                write (
                                    PortableProtocol.protocolError
                                        request
                                        "unknown-message"
                                        "The protocol message kind is unknown."
                                )
                with
                | PortableBindingException message ->
                    write (PortableProtocol.protocolError request "invalid-message" message)
                | _ ->
                    write (
                        PortableProtocol.protocolError
                            request
                            "provider-internal-failure"
                            "The provider could not process the message."
                    )

        exitCode
