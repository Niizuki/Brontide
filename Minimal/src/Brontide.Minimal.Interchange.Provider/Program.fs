module Brontide.Minimal.Interchange.Provider.Program

open System
open System.Collections.Generic
open Brontide.Minimal.Binding
open Brontide.Minimal.Vocabularies.Cooling

[<EntryPoint>]
let main arguments =
    let crashAfterActivation = arguments |> Array.contains "--crash-after-activation"
    let rejectProtocol = arguments |> Array.contains "--reject-protocol"

    if arguments |> Array.contains "--catalog" then
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
