module Linen.Interchange.Provider.Program

open System
open Linen.Binding
open Linen.Vocabularies.Cooling

[<EntryPoint>]
let main arguments =
    let crashAfterActivation = arguments |> Array.contains "--crash-after-activation"
    let rejectProtocol = arguments |> Array.contains "--reject-protocol"
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
        "linen-fsharp-provider"
        crashAfterActivation
        rejectProtocol
        invoke
