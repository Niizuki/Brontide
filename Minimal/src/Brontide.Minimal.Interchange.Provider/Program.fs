module Brontide.Minimal.Interchange.Provider.Program

open System
open System.Collections.Generic
open System.IO
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

[<EntryPoint>]
let main arguments =
    let crashAfterActivation = arguments |> Array.contains "--crash-after-activation"
    let rejectProtocol = arguments |> Array.contains "--reject-protocol"

    if arguments |> Array.contains "--component-management" then
        runComponentManagement ()
    elif arguments |> Array.contains "--portable" then
        let prefix = "--portable-fail-after-first="
        match arguments |> Array.tryFind (fun argument -> argument.StartsWith(prefix, StringComparison.Ordinal)) with
        | Some argument ->
            try
                use _ = new FileStream(argument[prefix.Length..], FileMode.CreateNew, FileAccess.Write, FileShare.None)
                runPortable arguments
            with :? IOException -> 73
        | None -> runPortable arguments
    elif arguments |> Array.contains "--catalog" then
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
    else
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
