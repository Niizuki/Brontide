namespace Brontide.Minimal.Experimental.Composition

open Brontide.Minimal.Model
open Brontide.Minimal.Vocabularies.Imaging

type DependencyStrength =
    | RequiredGenericContract
    | RequiredProfile
    | PreferredSystemProvider
    | RequiredAuthoredProvider

type ExecutionProperty =
    | Pure
    | Deterministic
    | NoIo
    | BoundedMemory

type OptimisationEligibility =
    | Eligible
    | Ineligible of missing: Set<ExecutionProperty>

type Dependency =
    { Capability: CanonicalName
      Strength: DependencyStrength }

type Component =
    { Name: CanonicalName
      Provides: Set<CanonicalName>
      Dependencies: Dependency list
      Priority: int
      ExecuteImage: (ImageOperation -> Image -> Result<Image, string>) option }

type Relationship =
    | Supports
    | Opposes

type ComponentRelationship =
    { From: CanonicalName
      Toward: CanonicalName
      Relationship: Relationship
      Reason: string }

type CompositionGraph =
    { Components: Map<CanonicalName, Component>
      Relationships: ComponentRelationship list }

type SelectionIntent =
    { Capability: CanonicalName
      PreferredProviders: Set<CanonicalName>
      OpposedProviders: Set<CanonicalName> }

type SelectionExplanation =
    { Provider: CanonicalName option
      Accepted: string list
      Rejected: (CanonicalName * string) list }

type DefinitionConstraintCandidate<'T> =
    { Name: CanonicalName
      Value: 'T
      Constraint: ConstraintExpression }

type DefinitionConstraintRejection =
    { Candidate: CanonicalName
      DiagnosticCategory: ConstraintDiagnosticCategory
      UnsupportedConstraints: CanonicalName list
      Reason: string }

type DefinitionConstraintSelectionResult<'T> =
    { Eligible: DefinitionConstraintCandidate<'T> list
      Rejected: DefinitionConstraintRejection list }

[<RequireQualifiedAccess>]
module DefinitionConstraintSelection =
    let filter
        (evaluateAtom: ConstraintRequirement -> ConstraintAtomEvaluation)
        (candidates: DefinitionConstraintCandidate<'T> list)
        : DefinitionConstraintSelectionResult<'T> =
        let assessments =
            candidates
            |> List.map (fun candidate ->
                candidate,
                ConstraintExpression.evaluate evaluateAtom candidate.Constraint)

        { Eligible =
            assessments
            |> List.choose (fun (candidate, evaluation) ->
                if evaluation.Outcome = Satisfied then Some candidate else None)
          Rejected =
            assessments
            |> List.choose (fun (candidate, evaluation) ->
                if evaluation.Outcome = Satisfied then
                    None
                else
                    Some
                        { Candidate = candidate.Name
                          DiagnosticCategory = evaluation.DiagnosticCategory
                          UnsupportedConstraints = evaluation.UnsupportedConstraints
                          Reason = evaluation.Reason }) }

type BoxedValue =
    private
        { Shape: ShapeReference
          Value: ShapeValue
          Boundary: CanonicalName
          Claims: (CanonicalName * string) list }

[<RequireQualifiedAccess>]
module BoxedValue =
    let create shape value boundary claims =
        { Shape = shape
          Value = value
          Boundary = boundary
          Claims = claims }

    let shape boxed = boxed.Shape
    let boundary boxed = boxed.Boundary
    let claims boxed = boxed.Claims

    let tryUnbox expectedShape boxed =
        if boxed.Shape = expectedShape then
            Ok boxed.Value
        else
            Error "The boxed value does not have the requested shape."

[<RequireQualifiedAccess>]
module Composition =
    let tryCreate (components: Component seq) (relationships: ComponentRelationship seq) =
        let duplicates =
            components
            |> Seq.groupBy _.Name
            |> Seq.tryFind (fun (_, group) -> Seq.length group > 1)

        let componentMap = components |> Seq.map (fun candidate -> candidate.Name, candidate) |> Map.ofSeq

        let dangling =
            relationships
            |> Seq.tryFind (fun relation ->
                not (Map.containsKey relation.From componentMap)
                || not (Map.containsKey relation.Toward componentMap))

        match duplicates, dangling with
        | Some _, _ -> Error "Component names must be unique."
        | _, Some _ -> Error "A component relationship has a missing endpoint."
        | _ ->
            Ok
                { Components = componentMap
                  Relationships = List.ofSeq relationships }

    let validate graph =
        let provided =
            graph.Components
            |> Map.toSeq
            |> Seq.collect (fun (_, candidate) -> candidate.Provides)
            |> Set.ofSeq

        graph.Components
        |> Map.toSeq
        |> Seq.collect (fun (_, candidate) ->
            candidate.Dependencies
            |> Seq.choose (fun dependency ->
                if
                    dependency.Strength <> PreferredSystemProvider
                    && not (Set.contains dependency.Capability provided)
                then
                    Some $"{CanonicalName.value candidate.Name} requires unavailable capability {CanonicalName.value dependency.Capability}."
                else
                    None))
        |> Seq.toList

    let select intent graph =
        let candidates =
            graph.Components
            |> Map.toSeq
            |> Seq.map snd
            |> Seq.filter (fun candidate -> Set.contains intent.Capability candidate.Provides)
            |> Seq.toList

        let scored =
            candidates
            |> List.map (fun candidate ->
                let preferredBonus = if Set.contains candidate.Name intent.PreferredProviders then 1000 else 0

                let supportBonus =
                    graph.Relationships
                    |> List.sumBy (fun relationship ->
                        if relationship.Toward = candidate.Name && relationship.Relationship = Supports then 10 else 0)

                candidate, candidate.Priority + preferredBonus + supportBonus)
            |> List.filter (fun (candidate, _) -> not (Set.contains candidate.Name intent.OpposedProviders))
            |> List.sortBy (fun (candidate, score) -> -score, CanonicalName.value candidate.Name)

        let rejected =
            candidates
            |> List.choose (fun candidate ->
                if Set.contains candidate.Name intent.OpposedProviders then
                    Some(candidate.Name, "The selection intent explicitly opposes this provider.")
                else
                    None)

        match scored with
        | [] ->
            Error
                { Provider = None
                  Accepted = []
                  Rejected = rejected }
        | (selected, score) :: _ ->
            Ok(
                selected,
                { Provider = Some selected.Name
                  Accepted =
                    [ $"Provides {CanonicalName.value intent.Capability}."
                      $"Selected with deterministic score {score}." ]
                  Rejected = rejected }
            )

    let executeImage operation image candidate =
        match candidate.ExecuteImage with
        | None -> Error "The selected component has no image executor."
        | Some execute -> execute operation image

    let optimisationEligibility required declared =
        let missing = Set.difference required declared
        if Set.isEmpty missing then Eligible else Ineligible missing

    let cpuImageComponent name capability priority =
        { Name = name
          Provides = Set.singleton capability
          Dependencies = []
          Priority = priority
          ExecuteImage = Some(fun operation image -> Ok(Image.apply operation image)) }
