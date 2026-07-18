namespace Brontide.Minimal.Extensions.Flow

open Brontide.Minimal.Model

type FlowStep =
    { Name: CanonicalName
      Operation: OperationReference
      DependsOn: Set<CanonicalName> }

type FlowDefinition =
    { Name: CanonicalName
      Steps: Map<CanonicalName, FlowStep> }

type StepState =
    | Waiting
    | Ready
    | Running of ExecutionReference
    | Completed of ShapeValue
    | Rejected of string

type FlowState = Map<CanonicalName, StepState>

[<RequireQualifiedAccess>]
module Flow =
    let tryCreate (name: CanonicalName) (steps: FlowStep seq) =
        let duplicates =
            steps
            |> Seq.groupBy _.Name
            |> Seq.tryFind (fun (_, group) -> Seq.length group > 1)

        let stepMap: Map<CanonicalName, FlowStep> =
            steps |> Seq.map (fun step -> step.Name, step) |> Map.ofSeq

        let missingDependency =
            stepMap
            |> Map.toSeq
            |> Seq.collect (fun (_, step) -> step.DependsOn)
            |> Seq.tryFind (fun dependency -> not (Map.containsKey dependency stepMap))

        let rec reaches
            (visited: Set<CanonicalName>)
            (current: CanonicalName)
            (target: CanonicalName)
            =
            if Set.contains current visited then
                false
            elif current = target then
                true
            else
                let next = stepMap[current].DependsOn
                next |> Seq.exists (fun dependency -> reaches (Set.add current visited) dependency target)

        let cyclic =
            stepMap
            |> Map.toSeq
            |> Seq.map fst
            |> Seq.tryFind (fun step ->
                stepMap[step].DependsOn
                |> Seq.exists (fun dependency -> reaches Set.empty dependency step))

        match duplicates, missingDependency, cyclic with
        | Some _, _, _ -> Error "Flow step names must be unique."
        | _, Some _, _ -> Error "A flow step depends on an unknown step."
        | _, _, Some _ -> Error "A flow cannot contain a dependency cycle."
        | _ -> Ok { Name = name; Steps = stepMap }

    let start (definition: FlowDefinition) : FlowState =
        definition.Steps
        |> Map.map (fun _ step -> if Set.isEmpty step.DependsOn then Ready else Waiting)

    let private dependenciesComplete
        (definition: FlowDefinition)
        (state: FlowState)
        (stepName: CanonicalName)
        =
        definition.Steps[stepName].DependsOn
        |> Seq.forall (fun dependency ->
            match state[dependency] with
            | Completed _ -> true
            | _ -> false)

    let refresh (definition: FlowDefinition) (state: FlowState) : FlowState =
        state
        |> Map.map (fun name current ->
            match current with
            | Waiting when dependenciesComplete definition state name -> Ready
            | other -> other)

    let markRunning
        (step: CanonicalName)
        (execution: ExecutionReference)
        (_definition: FlowDefinition)
        (state: FlowState)
        =
        match Map.tryFind step state with
        | Some Ready -> Ok(Map.add step (Running execution) state)
        | Some _ -> Error "Only a ready step can start."
        | None -> Error "The flow step is unknown."

    let complete
        (step: CanonicalName)
        (result: ShapeValue)
        (definition: FlowDefinition)
        (state: FlowState)
        =
        match Map.tryFind step state with
        | Some(Running _) -> Ok(Map.add step (Completed result) state |> refresh definition)
        | Some _ -> Error "Only a running step can complete."
        | None -> Error "The flow step is unknown."

    let reject (step: CanonicalName) (reason: string) (state: FlowState) =
        if Map.containsKey step state then
            Ok(Map.add step (Rejected reason) state)
        else
            Error "The flow step is unknown."

    let readySteps (state: FlowState) =
        state
        |> Map.toSeq
        |> Seq.choose (fun (name, stepState) -> if stepState = Ready then Some name else None)
        |> Seq.toList
