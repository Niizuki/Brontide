namespace Brontide.Minimal.Experimental.Composition

open Brontide.Minimal.Model

/// An Attribute value together with the source that produced it, as Architecture 0.7 §18.1
/// requires: an Attribute is a value obtained through a specified Operation, never a free-floating
/// label.
type AttributeValue =
    { Attribute: CanonicalName
      SourceOperation: OperationReference
      VocabularyVersion: int
      ResultShape: ShapeReference
      ResultPath: string
      Value: ShapeValue }

/// A selection candidate and the Attribute values it reports at the moment it is read.
type AttributeCandidate =
    { Provider: CanonicalName
      Attributes: AttributeValue list }

type AttributeCandidateDisposition =
    | Selected
    | Unsatisfied
    | Unevaluatable

type AttributeCandidateOutcome =
    { Provider: CanonicalName
      Disposition: AttributeCandidateDisposition
      DiagnosticCategory: ConstraintDiagnosticCategory
      UnsupportedConstraints: CanonicalName list
      Reason: string }

/// The immutable record of one completed resolution.
///
/// It holds no reference to the candidate set or Attribute source it was resolved from, which is
/// what makes a later Attribute or candidate change unable to rebind it.
type AttributeBindingRecord =
    { Binding: CanonicalName
      SelectedProvider: CanonicalName
      EffectiveValues: AttributeValue list
      Provenance: AttributeCandidateOutcome list }

type AttributeBindingResolution =
    { Binding: AttributeBindingRecord option
      Provenance: AttributeCandidateOutcome list
      Reason: string }

/// Resolves an Attribute-constrained binding exactly once and records what decided it.
///
/// Architecture 0.7 §18.1: resolution evaluates Definition Constraints against Attribute values
/// obtained at that moment and records effective values and provenance; a later Attribute change
/// never invalidates, rebinds, or migrates an active binding. The tempting implementation is a live
/// query that re-answers on every read, which passes every single-shot test and violates that rule,
/// so the resolved record deliberately captures values rather than sources.
///
/// The atom is Model's own `ConstraintRequirement`, so the poisoning rule delivered by
/// BR-07-CONSTRAINT-001 decides exclusion here rather than being restated. The caller supplies the
/// mapping from a constraint reference to the Attribute it requires, because a `ConstraintReference`
/// is an issuer-controlled opaque handle and carries no name of its own.
[<RequireQualifiedAccess>]
module AttributeConstrainedBinding =
    let isResolved (resolution: AttributeBindingResolution) = resolution.Binding.IsSome

    /// Evaluates one atom against one candidate's reported values.
    ///
    /// A candidate that cannot answer the atom at all, or answers with a value this evaluator
    /// cannot compare, is Indeterminate rather than Unsatisfied: under the poisoning rule that
    /// makes the whole expression unevaluatable for that candidate, which in selection context is
    /// candidate exclusion.
    let private evaluateAtom
        (attributeOf: ConstraintReference -> CanonicalName option)
        (candidate: AttributeCandidate)
        (requirement: ConstraintRequirement)
        =
        match attributeOf requirement.Constraint with
        | None -> ConstraintAtomEvaluation.invalidValue
        | Some attribute ->
            match
                candidate.Attributes
                |> List.tryFind (fun reported -> reported.Attribute = attribute)
            with
            | None -> ConstraintAtomEvaluation.unsupported attribute
            | Some reported ->
                if reported.Value = requirement.Parameters then
                    ConstraintAtomEvaluation.satisfied
                else
                    ConstraintAtomEvaluation.unsatisfied
                        (sprintf
                            "Attribute '%s' reported by '%s' does not match the declared value."
                            (CanonicalName.value attribute)
                            (CanonicalName.value candidate.Provider))

    let private effectiveValues
        (attributeOf: ConstraintReference -> CanonicalName option)
        (constraintExpression: ConstraintExpression)
        (candidate: AttributeCandidate)
        =
        let referenced =
            ConstraintExpression.atoms constraintExpression
            |> List.choose (fun requirement -> attributeOf requirement.Constraint)
            |> Set.ofList
        candidate.Attributes
        |> List.filter (fun reported -> Set.contains reported.Attribute referenced)
        |> List.sortBy (fun reported -> CanonicalName.value reported.Attribute)

    let resolve
        (attributeOf: ConstraintReference -> CanonicalName option)
        (binding: CanonicalName)
        (constraintExpression: ConstraintExpression)
        (candidates: AttributeCandidate list)
        : AttributeBindingResolution =
        // One stated total order, so ties resolve identically on every run and in both stacks.
        let ordered =
            candidates |> List.sortBy (fun candidate -> CanonicalName.value candidate.Provider)

        let rec walk considered remaining =
            match remaining with
            | [] ->
                let provenance = List.rev considered
                { Binding = None
                  Provenance = provenance
                  Reason =
                    if provenance.IsEmpty then
                        sprintf
                            "Binding '%s' has no candidates to resolve against."
                            (CanonicalName.value binding)
                    else
                        sprintf
                            "No candidate satisfies the declared constraints for binding '%s'; %d were considered and excluded."
                            (CanonicalName.value binding)
                            provenance.Length }
            | (candidate: AttributeCandidate) :: rest ->
                let evaluation =
                    ConstraintExpression.evaluate
                        (evaluateAtom attributeOf candidate)
                        constraintExpression
                let disposition =
                    match evaluation.Outcome with
                    | Satisfied -> Selected
                    | ConstraintEvaluationOutcome.Unsatisfied -> Unsatisfied
                    | Indeterminate -> Unevaluatable
                let outcome =
                    { Provider = candidate.Provider
                      Disposition = disposition
                      DiagnosticCategory = evaluation.DiagnosticCategory
                      UnsupportedConstraints = evaluation.UnsupportedConstraints
                      Reason = evaluation.Reason }
                if disposition <> Selected then
                    walk (outcome :: considered) rest
                else
                    // Selected: capture the values the constraint actually read, then stop. Nothing
                    // beyond this point consults the candidate set again.
                    let provenance = List.rev (outcome :: considered)
                    { Binding =
                        Some
                            { Binding = binding
                              SelectedProvider = candidate.Provider
                              EffectiveValues =
                                effectiveValues attributeOf constraintExpression candidate
                              Provenance = provenance }
                      Provenance = provenance
                      Reason =
                        sprintf
                            "Binding '%s' resolved to '%s' against %d considered candidate(s)."
                            (CanonicalName.value binding)
                            (CanonicalName.value candidate.Provider)
                            provenance.Length }

        walk [] ordered

    /// Restores a recorded binding from its own recorded evidence, consulting no source.
    ///
    /// The absent parameter is the contract: restoration takes no candidate list and no Attribute
    /// source, so it cannot silently reselect against one. The recorded effective values must still
    /// satisfy the constraint, which is what makes an incomplete record refusable rather than
    /// restorable.
    let restore
        (attributeOf: ConstraintReference -> CanonicalName option)
        (constraintExpression: ConstraintExpression)
        (record: AttributeBindingRecord)
        : AttributeBindingResolution =
        let recorded =
            { Provider = record.SelectedProvider
              Attributes = record.EffectiveValues }
        let evaluation =
            ConstraintExpression.evaluate (evaluateAtom attributeOf recorded) constraintExpression
        if evaluation.Outcome <> Satisfied then
            { Binding = None
              Provenance = record.Provenance
              Reason =
                sprintf
                    "The recorded effective values for '%s' no longer satisfy the declared constraints: %s"
                    (CanonicalName.value record.SelectedProvider)
                    evaluation.Reason }
        else
            { Binding = Some record
              Provenance = record.Provenance
              Reason =
                sprintf
                    "Binding '%s' restored to '%s' without reselection."
                    (CanonicalName.value record.Binding)
                    (CanonicalName.value record.SelectedProvider) }
