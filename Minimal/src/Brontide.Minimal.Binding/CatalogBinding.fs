namespace Brontide.Minimal.Binding

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json

[<Struct; StructuralEquality; StructuralComparison>]
type CatalogRequestId = private CatalogRequestId of Guid

[<RequireQualifiedAccess>]
module CatalogRequestId =
    let create () = CatalogRequestId(Guid.NewGuid())
    let parse (text: string) = CatalogRequestId(Guid.Parse text)
    let value (CatalogRequestId value) = value
    let text id = (value id).ToString("D")

[<Struct; StructuralEquality; StructuralComparison>]
type CatalogExecutionId = private CatalogExecutionId of Guid

[<RequireQualifiedAccess>]
module CatalogExecutionId =
    let create () = CatalogExecutionId(Guid.NewGuid())
    let parse (text: string) = CatalogExecutionId(Guid.Parse text)
    let value (CatalogExecutionId value) = value
    let text id = (value id).ToString("D")

[<StructuralEquality; StructuralComparison>]
type CatalogResourceReference =
    { Provider: string
      Id: string }

[<StructuralEquality; StructuralComparison>]
type CatalogItem =
    { Id: string
      Title: string
      Tags: string list }

[<StructuralEquality; StructuralComparison>]
type CatalogManifestOperation =
    { Name: string
      InputShape: string
      OutputShape: string }

[<StructuralEquality; StructuralComparison>]
type CatalogManifest =
    { ProtocolVersion: int
      Component: string
      ContractVersion: int
      ResourceBoundary: string
      PayloadLimitBytes: int
      Operations: CatalogManifestOperation list }

[<StructuralEquality; StructuralComparison>]
type CatalogInvocation =
    { Request: CatalogRequestId
      Execution: CatalogExecutionId
      Operation: string
      Resource: CatalogResourceReference
      Items: CatalogItem list
      ItemIds: string list }

[<StructuralEquality; StructuralComparison>]
type CatalogProviderReply =
    { Succeeded: bool
      Stored: int64
      Items: CatalogItem list
      Code: string option
      MissingIds: string list }

[<StructuralEquality; StructuralComparison>]
type CatalogOutcome =
    { Request: CatalogRequestId
      Execution: CatalogExecutionId
      Operation: string
      Succeeded: bool
      Stored: int64
      Items: CatalogItem list
      Code: string option
      MissingIds: string list }

[<StructuralEquality; StructuralComparison>]
type CatalogScenarioResult =
    { Upsert: CatalogOutcome
      Find: CatalogOutcome
      Missing: CatalogOutcome
      ProviderStarts: int }

exception CatalogProtocolException of code: string * message: string

[<RequireQualifiedAccess>]
module CatalogContract =
    [<Literal>]
    let protocolVersion = 1

    [<Literal>]
    let payloadLimitBytes = 65536

    [<Literal>]
    let upsertOperation = "interchange.tests.catalog.upsert-items"

    [<Literal>]
    let findOperation = "interchange.tests.catalog.find-items"

    let manifest =
        { ProtocolVersion = protocolVersion
          Component = "interchange.tests.catalog-component"
          ContractVersion = 1
          ResourceBoundary = "provider-scoped-resource-handle"
          PayloadLimitBytes = payloadLimitBytes
          Operations =
            [ { Name = upsertOperation
                InputShape = "interchange.tests.catalog.upsert-command@1"
                OutputShape = "interchange.tests.catalog.upsert-result@1" }
              { Name = findOperation
                InputShape = "interchange.tests.catalog.find-command@1"
                OutputShape = "interchange.tests.catalog.find-result@1" } ] }

[<RequireQualifiedAccess>]
module CatalogProviderReply =
    let stored count =
        { Succeeded = true
          Stored = count
          Items = []
          Code = None
          MissingIds = [] }

    let found items =
        { Succeeded = true
          Stored = 0L
          Items = items
          Code = None
          MissingIds = [] }

    let failure code missingIds =
        { Succeeded = false
          Stored = 0L
          Items = []
          Code = Some code
          MissingIds = missingIds }

module private CatalogJson =
    let raiseProtocol code message = raise (CatalogProtocolException(code, message))

    let parse (text: string) =
        try
            JsonDocument.Parse(
                text,
                JsonDocumentOptions(AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 32)
            )
        with :? JsonException as error ->
            raiseProtocol "invalid-message" $"Malformed catalog protocol JSON: {error.Message}"

    let required (name: string) (element: JsonElement) =
        let mutable value = Unchecked.defaultof<JsonElement>

        if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then
            value
        else
            raiseProtocol "invalid-message" $"The protocol value is missing '{name}'."

    let requiredString name element =
        let value = required name element

        match value.ValueKind, value.GetString() |> Option.ofObj with
        | JsonValueKind.String, Some text -> text
        | _ -> raiseProtocol "invalid-message" $"The {name} must be text."

    let requiredNonEmptyString name element =
        let value = requiredString name element

        if String.IsNullOrWhiteSpace value then
            raiseProtocol "invalid-message" $"The {name} must not be empty."

        value

    let requiredGuid name element =
        match Guid.TryParse(requiredString name element) with
        | true, value -> value
        | _ -> raiseProtocol "invalid-message" $"The {name} is not a UUID."

    let requireExactProperties expected (element: JsonElement) =
        if element.ValueKind <> JsonValueKind.Object then
            raiseProtocol "invalid-message" "A catalog protocol object was expected."

        let actual = element.EnumerateObject() |> Seq.map _.Name |> Seq.toList
        let actualSet = Set.ofList actual
        let expectedSet = Set.ofList expected

        if actual.Length <> expected.Length || actualSet <> expectedSet then
            match actual |> List.tryFind (fun name -> not (Set.contains name expectedSet)) with
            | Some name -> raiseProtocol "unknown-field" $"Unknown catalog protocol field '{name}'."
            | None -> raiseProtocol "invalid-message" "A catalog protocol object is missing a required field."

    let requireVersion root =
        let version = (required "protocolVersion" root).GetInt32()

        if version <> CatalogContract.protocolVersion then
            raiseProtocol "unsupported-version" $"Catalog protocol version {version} is not supported."

    let write (action: Utf8JsonWriter -> unit) =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        action writer
        writer.Flush()
        Encoding.UTF8.GetString(stream.ToArray())

    let writeItem (writer: Utf8JsonWriter) (item: CatalogItem) =
        writer.WriteStartObject()
        writer.WriteString("id", item.Id)
        writer.WriteString("title", item.Title)
        writer.WriteStartArray("tags")
        item.Tags |> List.iter writer.WriteStringValue
        writer.WriteEndArray()
        writer.WriteEndObject()

    let readItem (element: JsonElement) =
        requireExactProperties [ "id"; "title"; "tags" ] element
        let tagsElement = required "tags" element

        if tagsElement.ValueKind <> JsonValueKind.Array then
            raiseProtocol "invalid-message" "Catalog item tags must be an array."

        let tags =
            tagsElement.EnumerateArray()
            |> Seq.map (fun tag ->
                match tag.ValueKind, tag.GetString() |> Option.ofObj with
                | JsonValueKind.String, Some text when not (String.IsNullOrWhiteSpace text) -> text
                | _ -> raiseProtocol "invalid-message" "Catalog tags must be non-empty text.")
            |> Seq.toList

        { Id = requiredNonEmptyString "id" element
          Title = requiredNonEmptyString "title" element
          Tags = tags }

[<RequireQualifiedAccess>]
module CatalogManifestCodec =
    let private readOperation (element: JsonElement) =
        CatalogJson.requireExactProperties [ "name"; "inputShape"; "outputShape" ] element

        { Name = CatalogJson.requiredString "name" element
          InputShape = CatalogJson.requiredString "inputShape" element
          OutputShape = CatalogJson.requiredString "outputShape" element }

    let validate manifest =
        let operations = manifest.Operations |> List.map _.Name |> Set.ofList

        if
            manifest.ProtocolVersion <> CatalogContract.protocolVersion
            || manifest.ContractVersion <> 1
            || manifest.Component <> CatalogContract.manifest.Component
            || manifest.ResourceBoundary <> "provider-scoped-resource-handle"
            || manifest.PayloadLimitBytes <> CatalogContract.payloadLimitBytes
            || operations <> Set.ofList [ CatalogContract.upsertOperation; CatalogContract.findOperation ]
        then
            CatalogJson.raiseProtocol "incompatible-manifest" "The catalog manifest is incompatible with contract version 1."

    let decode json =
        use document = CatalogJson.parse json
        let root = document.RootElement

        CatalogJson.requireExactProperties
            [ "protocolVersion"
              "component"
              "contractVersion"
              "resourceBoundary"
              "payloadLimitBytes"
              "operations" ]
            root

        let operationsElement = CatalogJson.required "operations" root

        if operationsElement.ValueKind <> JsonValueKind.Array then
            CatalogJson.raiseProtocol "incompatible-manifest" "Manifest operations must be an array."

        let manifest =
            { ProtocolVersion = (CatalogJson.required "protocolVersion" root).GetInt32()
              Component = CatalogJson.requiredString "component" root
              ContractVersion = (CatalogJson.required "contractVersion" root).GetInt32()
              ResourceBoundary = CatalogJson.requiredString "resourceBoundary" root
              PayloadLimitBytes = (CatalogJson.required "payloadLimitBytes" root).GetInt32()
              Operations = operationsElement.EnumerateArray() |> Seq.map readOperation |> Seq.toList }

        validate manifest
        manifest

    let encode manifest =
        validate manifest

        CatalogJson.write (fun writer ->
            writer.WriteStartObject()
            writer.WriteNumber("protocolVersion", manifest.ProtocolVersion)
            writer.WriteString("component", manifest.Component)
            writer.WriteNumber("contractVersion", manifest.ContractVersion)
            writer.WriteString("resourceBoundary", manifest.ResourceBoundary)
            writer.WriteNumber("payloadLimitBytes", manifest.PayloadLimitBytes)
            writer.WriteStartArray("operations")

            manifest.Operations
            |> List.iter (fun operation ->
                writer.WriteStartObject()
                writer.WriteString("name", operation.Name)
                writer.WriteString("inputShape", operation.InputShape)
                writer.WriteString("outputShape", operation.OutputShape)
                writer.WriteEndObject())

            writer.WriteEndArray()
            writer.WriteEndObject())

module private CatalogProtocol =
    let private start (kind: string) (request: CatalogRequestId) (writer: Utf8JsonWriter) =
        writer.WriteStartObject()
        writer.WriteNumber("protocolVersion", CatalogContract.protocolVersion)
        writer.WriteString("kind", kind)
        writer.WriteString("requestId", CatalogRequestId.text request)

    let writeInvocation (invocation: CatalogInvocation) =
        CatalogJson.write (fun writer ->
            start "invoke" invocation.Request writer
            writer.WriteString("executionId", CatalogExecutionId.text invocation.Execution)
            writer.WriteString("operation", invocation.Operation)
            writer.WriteStartObject("resource")
            writer.WriteString("provider", invocation.Resource.Provider)
            writer.WriteString("id", invocation.Resource.Id)
            writer.WriteEndObject()
            writer.WriteStartObject("input")

            if invocation.Operation = CatalogContract.upsertOperation then
                writer.WriteStartArray("items")
                invocation.Items |> List.iter (CatalogJson.writeItem writer)
                writer.WriteEndArray()
            else
                writer.WriteStartArray("itemIds")
                invocation.ItemIds |> List.iter writer.WriteStringValue
                writer.WriteEndArray()

            writer.WriteEndObject()
            writer.WriteEndObject())

    let writeOutcome (invocation: CatalogInvocation) (reply: CatalogProviderReply) =
        CatalogJson.write (fun writer ->
            start "outcome" invocation.Request writer
            writer.WriteString("executionId", CatalogExecutionId.text invocation.Execution)
            writer.WriteString("operation", invocation.Operation)
            writer.WriteString("status", if reply.Succeeded then "succeeded" else "failed")

            if reply.Succeeded then
                writer.WriteStartObject("result")

                if invocation.Operation = CatalogContract.upsertOperation then
                    writer.WriteNumber("stored", reply.Stored)
                else
                    writer.WriteStartArray("items")
                    reply.Items |> List.iter (CatalogJson.writeItem writer)
                    writer.WriteEndArray()

                writer.WriteEndObject()
                writer.WriteNull("details")
            else
                writer.WriteNull("result")
                writer.WriteStartObject("details")
                writer.WriteString("code", reply.Code |> Option.defaultValue "provider-failure")
                writer.WriteStartArray("missingIds")
                reply.MissingIds |> List.iter writer.WriteStringValue
                writer.WriteEndArray()
                writer.WriteEndObject()

            writer.WriteEndObject())

    let writeProtocolError (request: CatalogRequestId) (code: string) (message: string) =
        CatalogJson.write (fun writer ->
            start "protocol-error" request writer
            writer.WriteString("code", code)
            writer.WriteString("message", message)
            writer.WriteEndObject())

    let writeShutdown request =
        CatalogJson.write (fun writer ->
            start "shutdown" request writer
            writer.WriteEndObject())

    let writeShutdownAck request =
        CatalogJson.write (fun writer ->
            start "shutdown-ack" request writer
            writer.WriteEndObject())

    let readInvocation (root: JsonElement) =
        CatalogJson.requireExactProperties
            [ "protocolVersion"; "kind"; "requestId"; "executionId"; "operation"; "resource"; "input" ]
            root

        CatalogJson.requireVersion root

        if CatalogJson.requiredString "kind" root <> "invoke" then
            CatalogJson.raiseProtocol "unknown-variant" "The catalog message kind is not invoke."

        let request = CatalogJson.requiredGuid "requestId" root |> _.ToString("D") |> CatalogRequestId.parse
        let execution = CatalogJson.requiredGuid "executionId" root |> _.ToString("D") |> CatalogExecutionId.parse
        let operation = CatalogJson.requiredString "operation" root
        let resourceElement = CatalogJson.required "resource" root
        CatalogJson.requireExactProperties [ "provider"; "id" ] resourceElement

        let resource =
            { Provider = CatalogJson.requiredNonEmptyString "provider" resourceElement
              Id = CatalogJson.requiredNonEmptyString "id" resourceElement }

        let input = CatalogJson.required "input" root

        if operation = CatalogContract.upsertOperation then
            CatalogJson.requireExactProperties [ "items" ] input
            let itemsElement = CatalogJson.required "items" input

            if itemsElement.ValueKind <> JsonValueKind.Array then
                CatalogJson.raiseProtocol "invalid-message" "Catalog items must be an array."

            let items = itemsElement.EnumerateArray() |> Seq.map CatalogJson.readItem |> Seq.toList

            if List.isEmpty items || (items |> List.map _.Id |> Set.ofList |> Set.count) <> items.Length then
                CatalogJson.raiseProtocol "invalid-message" "An upsert requires uniquely identified catalog items."

            { Request = request
              Execution = execution
              Operation = operation
              Resource = resource
              Items = items
              ItemIds = [] }
        elif operation = CatalogContract.findOperation then
            CatalogJson.requireExactProperties [ "itemIds" ] input
            let idsElement = CatalogJson.required "itemIds" input

            if idsElement.ValueKind <> JsonValueKind.Array then
                CatalogJson.raiseProtocol "invalid-message" "Catalog itemIds must be an array."

            let ids =
                idsElement.EnumerateArray()
                |> Seq.map (fun item ->
                    match item.ValueKind, item.GetString() |> Option.ofObj with
                    | JsonValueKind.String, Some text when not (String.IsNullOrWhiteSpace text) -> text
                    | _ -> CatalogJson.raiseProtocol "invalid-message" "Catalog itemIds must be non-empty text.")
                |> Seq.toList

            if List.isEmpty ids then
                CatalogJson.raiseProtocol "invalid-message" "A find requires at least one itemId."

            { Request = request
              Execution = execution
              Operation = operation
              Resource = resource
              Items = []
              ItemIds = ids }
        else
            CatalogJson.raiseProtocol "unknown-operation" $"Unknown catalog operation '{operation}'."

    let readOutcome (line: string) (expected: CatalogInvocation) =
        if Encoding.UTF8.GetByteCount line > CatalogContract.payloadLimitBytes then
            CatalogJson.raiseProtocol "payload-limit" "The provider response exceeds the 65536-byte payload limit."

        use document = CatalogJson.parse line
        let root = document.RootElement
        CatalogJson.requireVersion root
        let kind = CatalogJson.requiredString "kind" root

        if kind = "protocol-error" then
            CatalogJson.requireExactProperties [ "protocolVersion"; "kind"; "requestId"; "code"; "message" ] root
            CatalogJson.raiseProtocol (CatalogJson.requiredString "code" root) (CatalogJson.requiredString "message" root)

        CatalogJson.requireExactProperties
            [ "protocolVersion"; "kind"; "requestId"; "executionId"; "operation"; "status"; "result"; "details" ]
            root

        let request = CatalogJson.requiredGuid "requestId" root |> _.ToString("D") |> CatalogRequestId.parse
        let execution = CatalogJson.requiredGuid "executionId" root |> _.ToString("D") |> CatalogExecutionId.parse

        if
            kind <> "outcome"
            || request <> expected.Request
            || execution <> expected.Execution
            || CatalogJson.requiredString "operation" root <> expected.Operation
        then
            CatalogJson.raiseProtocol "invalid-message" "The catalog Outcome does not match its invocation."

        let status = CatalogJson.requiredString "status" root
        let result = CatalogJson.required "result" root
        let details = CatalogJson.required "details" root

        if status = "succeeded" then
            if details.ValueKind <> JsonValueKind.Null then
                CatalogJson.raiseProtocol "invalid-message" "A successful catalog Outcome cannot carry failure details."

            if expected.Operation = CatalogContract.upsertOperation then
                CatalogJson.requireExactProperties [ "stored" ] result

                { Request = request
                  Execution = execution
                  Operation = expected.Operation
                  Succeeded = true
                  Stored = (CatalogJson.required "stored" result).GetInt64()
                  Items = []
                  Code = None
                  MissingIds = [] }
            else
                CatalogJson.requireExactProperties [ "items" ] result
                let items = CatalogJson.required "items" result |> _.EnumerateArray() |> Seq.map CatalogJson.readItem |> Seq.toList

                { Request = request
                  Execution = execution
                  Operation = expected.Operation
                  Succeeded = true
                  Stored = 0L
                  Items = items
                  Code = None
                  MissingIds = [] }
        elif status = "failed" && result.ValueKind = JsonValueKind.Null then
            CatalogJson.requireExactProperties [ "code"; "missingIds" ] details
            let missing = CatalogJson.required "missingIds" details |> _.EnumerateArray() |> Seq.map _.GetString() |> Seq.choose Option.ofObj |> Seq.toList

            { Request = request
              Execution = execution
              Operation = expected.Operation
              Succeeded = false
              Stored = 0L
              Items = []
              Code = Some(CatalogJson.requiredString "code" details)
              MissingIds = missing }
        else
            CatalogJson.raiseProtocol "invalid-message" "The catalog Outcome status or payload is invalid."

[<RequireQualifiedAccess>]
module CatalogProviderEndpoint =
    let runWith (input: TextReader) (output: TextWriter) (invoke: CatalogInvocation -> CatalogProviderReply) =
        let seen = HashSet<CatalogRequestId>()
        let mutable running = true

        let write (line: string) =
            output.WriteLine line
            output.Flush()

        while running do
            match input.ReadLine() |> Option.ofObj with
            | None -> running <- false
            | Some line ->
                let mutable request = CatalogRequestId.parse(Guid.Empty.ToString("D"))

                try
                    if Encoding.UTF8.GetByteCount line > CatalogContract.payloadLimitBytes then
                        CatalogJson.raiseProtocol "payload-limit" "The protocol line exceeds the 65536-byte payload limit."

                    use document = CatalogJson.parse line
                    let root = document.RootElement
                    let kind = CatalogJson.requiredString "kind" root

                    if kind = "shutdown" then
                        CatalogJson.requireExactProperties [ "protocolVersion"; "kind"; "requestId" ] root
                        CatalogJson.requireVersion root
                        request <- CatalogJson.requiredGuid "requestId" root |> _.ToString("D") |> CatalogRequestId.parse
                        write (CatalogProtocol.writeShutdownAck request)
                        running <- false
                    else
                        let invocation = CatalogProtocol.readInvocation root
                        request <- invocation.Request

                        if not (seen.Add request) then
                            write (CatalogProtocol.writeProtocolError request "replay" "The requestId has already been used.")
                        else
                            invoke invocation |> CatalogProtocol.writeOutcome invocation |> write
                with
                | CatalogProtocolException(code, message) -> write (CatalogProtocol.writeProtocolError request code message)
                | _ ->
                    write (
                        CatalogProtocol.writeProtocolError
                            request
                            "provider-internal-failure"
                            "The provider could not process the message."
                    )

        0

    let run invoke = runWith Console.In Console.Out invoke

[<RequireQualifiedAccess>]
module CatalogProcessClient =
    let runScenario
        (launch: ProviderLaunch)
        (timeout: TimeSpan)
        (resource: CatalogResourceReference)
        (items: CatalogItem list)
        =
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- launch.FileName
        startInfo.Arguments <- launch.Arguments
        startInfo.RedirectStandardInput <- true
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false
        startInfo.CreateNoWindow <- true
        use provider = new Process(StartInfo = startInfo)

        if not (provider.Start()) then
            raise (IOException "The catalog provider process did not start.")

        let send (line: string) =
            provider.StandardInput.WriteLine line
            provider.StandardInput.Flush()

        let read (stage: string) =
            let pending = provider.StandardOutput.ReadLineAsync()

            if not (pending.Wait timeout) then
                raise (TimeoutException $"The catalog provider timed out during {stage}.")

            match pending.Result |> Option.ofObj with
            | Some line -> line
            | None ->
                let diagnostics = provider.StandardError.ReadToEnd()
                raise (IOException $"The catalog provider ended during {stage}; diagnostics: {diagnostics.Trim()}")

        let exchange (invocation: CatalogInvocation) =
            CatalogProtocol.writeInvocation invocation |> send
            CatalogProtocol.readOutcome (read invocation.Operation) invocation

        try
            let upsertInvocation =
                { Request = CatalogRequestId.create ()
                  Execution = CatalogExecutionId.create ()
                  Operation = CatalogContract.upsertOperation
                  Resource = resource
                  Items = items
                  ItemIds = [] }

            let upsert = exchange upsertInvocation

            let findInvocation =
                { Request = CatalogRequestId.create ()
                  Execution = CatalogExecutionId.create ()
                  Operation = CatalogContract.findOperation
                  Resource = resource
                  Items = []
                  ItemIds = items |> List.map _.Id }

            let found = exchange findInvocation

            let missingInvocation =
                { Request = CatalogRequestId.create ()
                  Execution = CatalogExecutionId.create ()
                  Operation = CatalogContract.findOperation
                  Resource = resource
                  Items = []
                  ItemIds = [ "missing-item" ] }

            let missing = exchange missingInvocation
            let shutdown = CatalogRequestId.create ()
            CatalogProtocol.writeShutdown shutdown |> send
            use shutdownDocument = CatalogJson.parse (read "catalog shutdown")
            let shutdownRoot = shutdownDocument.RootElement
            CatalogJson.requireExactProperties [ "protocolVersion"; "kind"; "requestId" ] shutdownRoot
            CatalogJson.requireVersion shutdownRoot

            if
                CatalogJson.requiredString "kind" shutdownRoot <> "shutdown-ack"
                || (CatalogJson.requiredGuid "requestId" shutdownRoot |> _.ToString("D") |> CatalogRequestId.parse) <> shutdown
            then
                CatalogJson.raiseProtocol "invalid-message" "The provider did not acknowledge shutdown."

            { Upsert = upsert
              Find = found
              Missing = missing
              ProviderStarts = 1 }
        finally
            if not provider.HasExited then
                provider.Kill true
                provider.WaitForExit()
