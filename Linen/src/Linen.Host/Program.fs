module Linen.Host.Program

open System
open Linen.Model
open Linen.Kernel
open Linen.Binding
open Linen.Vocabularies.Cooling
open Linen.Vocabularies.Imaging
open Linen.Experimental.Composition

let private name value = CanonicalName.create value

let private runCooling () =
    let initial = Cooling.initial "primary" 20.0M 24.0M
    let operatorChange = Cooling.apply (SetTargetTemperature 26.0M) initial
    let sensorChange = Cooling.apply (RecordMeasurement 28.0M) operatorChange.After

    printfn
        "Cooling: target=%M measured=%M enabled=%b revision=%d"
        sensorChange.After.TargetTemperature
        sensorChange.After.MeasuredTemperature
        sensorChange.After.CoolingEnabled
        sensorChange.After.Revision

let private runComposition () =
    let capability = name "linen.imaging.execute"
    let cpu = Composition.cpuImageComponent (name "linen.composition.cpu") capability 10
    let graph = Composition.tryCreate [ cpu ] [] |> Result.defaultWith failwith

    let intent =
        { Capability = capability
          PreferredProviders = Set.singleton cpu.Name
          OpposedProviders = Set.empty }

    let selected, explanation =
        Composition.select intent graph
        |> Result.defaultWith (fun _ -> failwith "No image provider was selected.")

    let image = Image.tryCreate 2 1 Grayscale8 [| 0uy; 200uy |] |> Result.defaultWith failwith
    let result = Composition.executeImage Invert image selected |> Result.defaultWith failwith

    printfn
        "Composition: provider=%s pixels=%A reason=%s"
        (CanonicalName.value selected.Name)
        (Image.pixels result)
        explanation.Accepted.Head

let private printBoundary () =
    let world = World.create(Guid.Parse "b28127d6-9708-4bee-8a30-2a40a17f4bf9")
    let manifest = Manifest.ofLinen "linen-fsharp" (World.shapes world |> Seq.map _.Reference) Seq.empty
    printfn "Binding manifest: %s" (Manifest.toJson manifest)

[<EntryPoint>]
let main _ =
    printfn "Linen — independent F# Atlas runtime"
    runCooling ()
    runComposition ()
    printBoundary ()
    0
