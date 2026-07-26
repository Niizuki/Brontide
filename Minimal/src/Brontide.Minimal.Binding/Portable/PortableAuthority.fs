namespace Brontide.Minimal.Binding.Portable

/// A three-valued authority atom.
[<RequireQualifiedAccess>]
type PortableTruth =
    | Satisfied
    | Unsatisfied
    | Unknown

/// An authority expression over three-valued atoms.
///
/// Unknown never resolves to permitted. Only a Satisfied expression permits an effect, so an
/// unrecognized Constraint value version denies rather than widening authority by projection.
[<RequireQualifiedAccess>]
type PortableConstraint =
    | Atom of PortableTruth
    | AnyOf of PortableConstraint list
    | AllOf of PortableConstraint list
    | Not of PortableConstraint

[<RequireQualifiedAccess>]
module PortableConstraint =

    /// Evaluates under strong Kleene three-valued logic.
    let rec evaluate expression =
        match expression with
        | PortableConstraint.Atom truth -> truth
        | PortableConstraint.AnyOf operands ->
            let results = operands |> List.map evaluate

            if List.contains PortableTruth.Satisfied results then PortableTruth.Satisfied
            elif List.contains PortableTruth.Unknown results then PortableTruth.Unknown
            else PortableTruth.Unsatisfied
        | PortableConstraint.AllOf operands ->
            let results = operands |> List.map evaluate

            if List.contains PortableTruth.Unsatisfied results then PortableTruth.Unsatisfied
            elif List.contains PortableTruth.Unknown results then PortableTruth.Unknown
            else PortableTruth.Satisfied
        | PortableConstraint.Not operand ->
            match evaluate operand with
            | PortableTruth.Satisfied -> PortableTruth.Unsatisfied
            | PortableTruth.Unsatisfied -> PortableTruth.Satisfied
            | PortableTruth.Unknown -> PortableTruth.Unknown

/// The tokens by which this endpoint recognizes authority-bearing content in a payload position.
///
/// The neutral contract forbids a Capability, Constraint expression, or derivation chain from
/// crossing a trust boundary but does not enumerate their names, so the recognizer is declared here
/// rather than inferred. The names are local; the effect is not: any of them in the body of a
/// trust-crossing binding is an invalid authority presentation, never a merely undeclared field.
[<RequireQualifiedAccess>]
module PortableAuthorityVocabulary =

    let private tokens =
        set
            [ "capability"
              "capabilitychain"
              "constraint"
              "constraintexpression"
              "derivation"
              "derivationchain"
              "authoritytoken" ]

    let isAuthorityBearing (name: string) = Set.contains (name.ToLowerInvariant()) tokens

    let private refuse (name: string) =
        invalidAuthority
            "capability-in-body"
            $"Member '{name}' presents authority across a trust boundary that carries no Capability."

    /// Refuses authority-bearing content anywhere in a decoded body before the body is given a
    /// Shape, so the refusal names the authority rule rather than the Shape.
    let requireNoCapabilityContent (item: CborItem) : PortableResult<unit> =
        let rec walk item =
            match item with
            | CborMap entries ->
                entries
                |> iterate (fun (key, value) -> if isAuthorityBearing key then refuse key else walk value)
            | CborArray items -> items |> iterate walk
            | CborInteger _
            | CborText _
            | CborBytes _
            | CborBoolean _
            | CborNull
            | CborDecimal _ -> Ok()

        walk item

    /// The same scan over an in-memory value, which is what the fixed direct-call realization
    /// presents. Both realizations therefore refuse the same content.
    let requireNoCapabilityValue (value: PortableValue) : PortableResult<unit> =
        let rec walk value =
            match value with
            | PortableRecord(fields, fragments) ->
                portable {
                    do!
                        fields
                        |> Map.toList
                        |> iterate (fun (name, child) -> if isAuthorityBearing name then refuse name else walk child)

                    do!
                        fragments
                        |> Map.toList
                        |> iterate (fun (reference, fragmentFields) ->
                            let name = PortableFragmentRef.name reference

                            if isAuthorityBearing name then
                                refuse name
                            else
                                fragmentFields
                                |> Map.toList
                                |> iterate (fun (fieldName, child) ->
                                    if isAuthorityBearing fieldName then refuse fieldName else walk child))
                }
            | PortableSequence items -> items |> iterate walk
            | PortableChoice(alternative, child) ->
                if isAuthorityBearing alternative then refuse alternative else walk child
            | PortableUnit
            | PortableText _
            | PortableBoolean _
            | PortableInteger _
            | PortableDecimalValue _
            | PortableBytesValue _ -> Ok()

        walk value

/// The result of a local authority evaluation.
type PortableAdmission =
    { Decision: AuthorityDecision
      DecisionPoint: AuthorityDecisionPoint
      Reason: string }

    member this.MayProceed = this.Decision = AuthorityDecision.Permitted

/// Evaluates the local authority boundary for one request.
///
/// A denial or unknown condition decided here starts no provider and emits no Channel frame: local
/// denial is an observation, never an envelope kind.
[<RequireQualifiedAccess>]
module PortableAuthorityGate =

    let evaluate (declaration: AuthorityDeclaration) expression =
        let point =
            if declaration.TrustBoundaryCrossed then
                AuthorityDecisionPoint.HostLocal
            else
                AuthorityDecisionPoint.TargetBoundary

        match PortableConstraint.evaluate expression with
        | PortableTruth.Satisfied ->
            { Decision = AuthorityDecision.Permitted
              DecisionPoint = point
              Reason = "The authority expression evaluated to True." }
        | PortableTruth.Unsatisfied ->
            { Decision = AuthorityDecision.Denied
              DecisionPoint = point
              Reason = "The authority expression evaluated to False." }
        | PortableTruth.Unknown ->
            { Decision = AuthorityDecision.Unknown
              DecisionPoint = point
              Reason = "The authority expression evaluated to Unknown, which never resolves to permitted." }

    /// Validates the declared presentation before any binding is established.
    let requireValidPresentation (declaration: AuthorityDeclaration) : PortableResult<unit> =
        portable {
            do!
                ensure (declaration.ConstraintPolicy = ContractDocument.OnlyPermittedConstraintPolicy) (fun () ->
                    invalidAuthority "constraint-policy" "The only permitted constraint policy is fail-closed.")

            let crossTrust =
                declaration.PresentationMode = AuthorityMode.CrossTrustNoCapabilityTransfer

            do!
                ensure (crossTrust = declaration.TrustBoundaryCrossed) (fun () ->
                    invalidAuthority
                        "presentation-mode"
                        "The declared presentation mode and the trust-boundary declaration disagree.")

            do!
                ensure (not declaration.TrustBoundaryCrossed || declaration.NoCapabilityTransfer) (fun () ->
                    invalidAuthority
                        "no-capability-transfer-absent"
                        "A trust-crossing binding must declare no-capability-transfer rather than defaulting to a permissive mode.")
        }
