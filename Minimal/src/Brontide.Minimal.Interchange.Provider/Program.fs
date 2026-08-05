module Brontide.Minimal.Interchange.Provider.Program

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text.Json
open Brontide.Minimal.Binding
open Brontide.Minimal.Binding.Portable
open Brontide.Minimal.Experimental.ComponentManagement
open Brontide.Minimal.Vocabularies.Cooling

/// Runs the reusable Portable Component Binding over a real duplex process boundary. The verbs
/// below it remain the retained line-delimited experiments, which stay the cross-stack baseline
/// until PB5 pairs the two portable implementations.
let private runPortable (arguments: string array) =
    let catalog = arguments |> Array.contains "--catalog"

    let endpoint =
        PortableProviderEndpoint(
            (if catalog then CatalogFixture.contract else CoolingFixture.contract),
            (if catalog then CatalogHandler() :> IPortableOperationHandler else CoolingHandler()),
            Realization.NegotiatedProcess
        )

    let duplex: IPortableDuplex =
        PortableStreamDuplex(
            Console.OpenStandardInput(),
            Console.OpenStandardOutput(),
            PortableLimits.declared,
            true
        )

    (PortableProviderProcessLoop.run duplex endpoint PortableLimits.declared).Wait()
    duplex.Close()
    0

let private runComponentManagement () =
    FakeAuthorityComparison.run
        Console.In
        Console.Out
        "minimal-fsharp"
        Threading.CancellationToken.None
    |> fun work -> work.GetAwaiter().GetResult()
    0

let private acquireRestartEffectLease () =
    let environment name = Environment.GetEnvironmentVariable name |> Option.ofObj
    match environment "BRONTIDE_RESTART_EFFECT_LEASE",
          environment "BRONTIDE_RESTART_EFFECT_RECEIPT",
          environment "BRONTIDE_RESTART_EFFECT_TOKEN",
          environment "BRONTIDE_RESTART_EFFECT_STAGED_IDENTITY" with
    | None, None, None, None -> Ok None
    | Some leasePath, Some receiptPath, Some token, Some stagedIdentity
        when not (String.IsNullOrWhiteSpace leasePath)
             && not (String.IsNullOrWhiteSpace receiptPath)
             && not (String.IsNullOrWhiteSpace token)
             && token.Length <= 128 && token = token.Trim()
             && stagedIdentity.Length = 64 ->
        try
            let lease = new FileStream(Path.GetFullPath leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)
            try
                use currentProcess = Process.GetCurrentProcess()
                use output = new MemoryStream()
                use writer = new Utf8JsonWriter(output)
                writer.WriteStartObject()
                writer.WriteString("format", "CBI55")
                writer.WriteString("token", token)
                writer.WriteString("stagedIdentity", stagedIdentity)
                writer.WriteNumber("processId", Environment.ProcessId)
                writer.WriteNumber("processStartUtcTicks", currentProcess.StartTime.ToUniversalTime().Ticks)
                let executableName =
                    Environment.ProcessPath
                    |> Option.ofObj
                    |> Option.bind (Path.GetFileName >> Option.ofObj)
                    |> Option.defaultValue ""
                if String.IsNullOrWhiteSpace executableName then invalidOp "The provider executable name is unavailable."
                writer.WriteString("executableName", executableName)
                writer.WriteEndObject()
                writer.Flush()
                let record = output.ToArray()
                let temporary = Path.GetFullPath(receiptPath) + ".tmp"
                use receipt = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)
                receipt.Write(record, 0, record.Length)
                let tag = SHA256.HashData record
                receipt.Write(tag, 0, tag.Length)
                receipt.Flush true
                receipt.Dispose()
                File.Move(temporary, Path.GetFullPath receiptPath, true)
                Ok(Some lease)
            with error ->
                lease.Dispose()
                raise error
        with :? IOException | :? UnauthorizedAccessException | :? InvalidOperationException | :? NotSupportedException -> Error 76
    | _ -> Error 76

let private run arguments =
    let crashAfterActivation = arguments |> Array.contains "--crash-after-activation"
    let rejectProtocol = arguments |> Array.contains "--reject-protocol"
    let ownershipProbePrefix = "--probe-exclusive-file="
    let ownershipHoldPrefix = "--hold-exclusive-file="

    match arguments |> Array.tryFind (fun argument ->
        argument.StartsWith(ownershipHoldPrefix, StringComparison.Ordinal)
        || argument.StartsWith(ownershipProbePrefix, StringComparison.Ordinal)) with
    | Some argument when argument.StartsWith(ownershipHoldPrefix, StringComparison.Ordinal) ->
        use _ = new FileStream(
            argument[ownershipHoldPrefix.Length..], FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.Read)
        Console.Out.WriteLine "held"
        Console.Out.Flush()
        Console.In.ReadLine() |> ignore
        0
    | Some argument ->
        try
            use _ = new FileStream(
                argument[ownershipProbePrefix.Length..], FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.Read)
            0
        with :? IOException -> 74
    | None when arguments |> Array.contains "--component-management" ->
        runComponentManagement ()
    | None when arguments |> Array.contains "--portable" ->
        let prefix = "--portable-fail-after-first="
        match arguments |> Array.tryFind (fun argument -> argument.StartsWith(prefix, StringComparison.Ordinal)) with
        | Some argument ->
            try
                use _ = new FileStream(argument[prefix.Length..], FileMode.CreateNew, FileAccess.Write, FileShare.None)
                runPortable arguments
            with :? IOException -> 73
        | None -> runPortable arguments
    | None when arguments |> Array.contains "--catalog" ->
        let catalog = Dictionary<string, CatalogItem>(StringComparer.Ordinal)

        let invoke (invocation: CatalogInvocation) =
            if
                invocation.Resource.Provider <> "catalog-sandbox"
                || invocation.Resource.Id <> "shared"
            then
                CatalogProviderReply.failure "resource-refused" []
            elif invocation.Operation = CatalogContract.upsertOperation then
                invocation.Items |> List.iter (fun item -> catalog[item.Id] <- item)
                CatalogProviderReply.stored invocation.Items.Length
            else
                let missing = invocation.ItemIds |> List.filter (catalog.ContainsKey >> not)

                if List.isEmpty missing then
                    invocation.ItemIds |> List.map (fun id -> catalog[id]) |> CatalogProviderReply.found
                else
                    CatalogProviderReply.failure "missing-items" missing

        CatalogProviderEndpoint.run invoke
    | None ->
        let mutable state = Cooling.initial "primary" 20.0M 20.0M

        let invoke (loop, enabled, failureMode) =
            if failureMode = Some "semantic" then
                { Succeeded = false
                  Value =
                    PortableContract.details
                        "requested-failure"
                        "The test contract requested a semantic failure."
                  ProviderEffectCount = state.Revision }
            else
                let transition = Cooling.apply (SetCoolingEnabled enabled) { state with Loop = loop }
                state <- transition.After

                { Succeeded = true
                  Value =
                    PortableContract.result
                        state.Loop
                        state.CoolingEnabled
                        state.Revision
                        state.Revision
                  ProviderEffectCount = state.Revision }

        PortableProviderEndpoint.run
            "brontide-minimal-fsharp-provider"
            crashAfterActivation
            rejectProtocol
            invoke

[<EntryPoint>]
let main arguments =
    match acquireRestartEffectLease () with
    | Error code -> code
    | Ok None -> run arguments
    | Ok(Some lease) ->
        use _ = lease
        run arguments
