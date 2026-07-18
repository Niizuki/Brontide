module Brontide.Minimal.Benchmarks.Program

open System
open System.Diagnostics
open System.IO
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Binding

let private get = function
    | Ok value -> value
    | Error message -> failwith message

let private name value = CanonicalName.create value
let private timeDomain = TimeDomainReference.create (name "Benchmarks:LogicalTime")
let private mark milliseconds =
    { Milliseconds = milliseconds
      TimeDomain = timeDomain
      UncertaintyMilliseconds = None }

let private report name count (elapsed: TimeSpan) =
    printfn
        "%s: count=%d; elapsedMs=%.3f; meanMicroseconds=%.3f"
        name
        count
        elapsed.TotalMilliseconds
        (elapsed.TotalMicroseconds / float count)

let private readIterations (arguments: string array) fallback =
    match arguments |> Array.tryFindIndex ((=) "--iterations") with
    | Some index when index + 1 < arguments.Length ->
        match Int32.TryParse arguments[index + 1] with
        | true, value when value > 0 -> value
        | _ -> fallback
    | _ -> fallback

[<EntryPoint>]
let main arguments =
    let iterations = readIterations arguments 1000
    let operation: OperationReference = { Name = name "Benchmarks:Constraint.Checked" }
    let initial = World.create (Guid.NewGuid()) timeDomain

    let constraintDefinition, withConstraint =
        World.registerConstraint
            (name "Benchmarks:Allow")
            BuiltIn.textShape
            "Benchmark allow constraint."
            initial
        |> get

    let (holder, target, capability), ready =
        World.genesis
            (name "Benchmarks:Bootstrap")
            (mark 0L)
            (fun genesis world ->
                let holder, world = Genesis.actor genesis (name "Benchmarks:Holder") world
                let target, world = Genesis.actor genesis (name "Benchmarks:Target") world

                let definition: OperationDefinition =
                    { Reference = operation
                      Description = "Benchmark one checked execution."
                      Target = target.Reference
                      CommandShape = BuiltIn.unitShape
                      ResultShape = BuiltIn.unitShape
                      Constraints = [] }

                let world = World.registerOperation definition world |> get
                let requirement =
                    { Constraint = constraintDefinition.Reference
                      Parameters = TextValue "allow" }

                let capability, world =
                    Genesis.capability
                        genesis
                        (name "Benchmarks:Capability")
                        holder.Reference
                        target.Reference
                        (Set.singleton operation)
                        [ requirement ]
                        false
                        world
                    |> get

                ((holder, target, capability), world))
            withConstraint
        |> get

    let evaluator _ _ = Ok()
    let handler _ = Ok(UnitValue, [], [])
    let mutable world = ready

    for index in 1..25 do
        let environment =
            { TrustedTime = mark (int64 index)
              ConstraintEvaluators = Map.ofList [ constraintDefinition.Reference, evaluator ]
              Handlers = Map.ofList [ operation, handler ] }

        let request =
            { Initiator = holder.Reference
              Target = target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = UnitValue
              Occurrence = None
              Context = Map.empty }

        world <- (World.step environment world request).World

    let constraintElapsed = Stopwatch.StartNew()
    for index in 1..iterations do
        let environment =
            { TrustedTime = mark (int64 (index + 25))
              ConstraintEvaluators = Map.ofList [ constraintDefinition.Reference, evaluator ]
              Handlers = Map.ofList [ operation, handler ] }

        let request =
            { Initiator = holder.Reference
              Target = target.Reference
              PresentedCapability = capability.Reference
              Operation = operation
              Command = UnitValue
              Occurrence = None
              Context = Map.empty }

        let result = World.step environment world request
        if result.Outcome.Status <> Succeeded then
            failwith "The constraint benchmark did not succeed."
        world <- result.World

    constraintElapsed.Stop()
    report "constraint-evaluation-and-execution" iterations constraintElapsed.Elapsed

    let serializationElapsed = Stopwatch.StartNew()
    for _ in 1..iterations do
        CatalogContract.manifest
        |> CatalogManifestCodec.encode
        |> CatalogManifestCodec.decode
        |> ignore
    serializationElapsed.Stop()
    report "catalog-manifest-serialize-roundtrip" iterations serializationElapsed.Elapsed

    let historyElapsed = Stopwatch.StartNew()
    let mutable historyChecksum = 0L
    for _ in 1..iterations do
        historyChecksum <-
            historyChecksum
            + int64 (World.executions world |> List.sumBy (fun execution -> if execution.Status = Succeeded then 1 else 0))
    historyElapsed.Stop()
    if historyChecksum = 0L then
        failwith "The execution history benchmark observed no records."
    report "execution-history-snapshot-and-scan" iterations historyElapsed.Elapsed

    match Environment.GetEnvironmentVariable "BRONTIDE_REFERENCE_PROVIDER" |> Option.ofObj with
    | Some provider when File.Exists provider ->
        let processIterations = 3
        let launch = { FileName = provider; Arguments = "--catalog" }
        let resource = { Provider = "catalog-sandbox"; Id = "shared" }
        let items: CatalogItem list =
            [ { Id = "bench"; Title = "Benchmark"; Tags = [ "nested"; "repeatable" ] } ]
        let processElapsed = Stopwatch.StartNew()

        for _ in 1..processIterations do
            let result = CatalogProcessClient.runScenario launch (TimeSpan.FromSeconds 10.0) resource items
            if not result.Upsert.Succeeded || not result.Find.Succeeded || result.Missing.Succeeded then
                failwith "The cross-process benchmark scenario failed."

        processElapsed.Stop()
        report "foreign-catalog-process-scenario" processIterations processElapsed.Elapsed
    | _ ->
        printfn "foreign-catalog-process-scenario: skipped (BRONTIDE_REFERENCE_PROVIDER is absent)"

    0
