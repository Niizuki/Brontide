namespace Brontide.Minimal.Composition.Tests

open NUnit.Framework
open Brontide.Minimal.Model
open Brontide.Minimal.Kernel
open Brontide.Minimal.Experimental.Composition

/// Architecture 0.7 §18.1 change C3, requirement BR-07-BINDING-001. Contract items are named by the
/// shared behavioural contract at `conformance/br-07-binding-001-contract.md`.
module private AttributeBindingHelpers =
    let name value = CanonicalName.create value
    let multiple action = Assert.Multiple(System.Action action)

    let binding = name "brontide-minimal.binding.cooling"
    let region = name "brontide-minimal.attribute.region"
    let tier = name "brontide-minimal.attribute.tier"
    let exotic = name "brontide-minimal.attribute.exotic"
    let alpha = name "brontide-minimal.provider.alpha"
    let bravo = name "brontide-minimal.provider.bravo"
    let charlie = name "brontide-minimal.provider.charlie"
    let readRegion: OperationReference = { Name = name "brontide-minimal.operation.read-region" }

    /// Registers one constraint definition per Attribute and returns the reference-to-Attribute
    /// mapping the resolver needs, because a ConstraintReference carries no name of its own.
    let registry () =
        let timeDomain = TimeDomainReference.create (name "Brontide.Minimal.Tests:BindingClock")
        let initial = World.create (System.Guid.NewGuid()) timeDomain
        let register world attribute =
            World.registerConstraint attribute BuiltIn.textShape "attribute selection atom" world
            |> Result.defaultWith failwith
        let regionDefinition, world = register initial region
        let tierDefinition, world = register world tier
        let exoticDefinition, _ = register world exotic
        let byReference =
            [ regionDefinition.Reference, region
              tierDefinition.Reference, tier
              exoticDefinition.Reference, exotic ]
            |> Map.ofList
        let attributeOf reference = Map.tryFind reference byReference
        let atomFor definition value =
            AtomicConstraint
                { Constraint = (definition: ConstraintDefinition).Reference
                  Parameters = TextValue value }
        attributeOf, atomFor regionDefinition, atomFor exoticDefinition

    let sourced attribute value : AttributeValue =
        { Attribute = attribute
          SourceOperation = readRegion
          VocabularyVersion = 1
          ResultShape = BuiltIn.textShape
          ResultPath = "/region"
          Value = TextValue value }

    let candidate provider regionValue : AttributeCandidate =
        { Provider = provider
          Attributes = [ sourced region regionValue; sourced tier "primary" ] }

open AttributeBindingHelpers

[<TestFixture>]
type AttributeBindingTests() =
    [<Test>]
    member _.``BR_07_BINDING_001 C1 an Attribute is a sourced value never a label``() =
        let attributeOf, north, _ = registry ()
        let resolution =
            AttributeConstrainedBinding.resolve attributeOf binding (north "north") [ candidate bravo "north" ]
        let effective = List.exactlyOne resolution.Binding.Value.EffectiveValues
        multiple (fun () ->
            Assert.That(effective.SourceOperation, Is.EqualTo readRegion)
            Assert.That(effective.VocabularyVersion, Is.EqualTo 1)
            Assert.That(effective.ResultShape, Is.EqualTo BuiltIn.textShape)
            Assert.That(effective.ResultPath, Is.EqualTo "/region"))

    [<Test>]
    member _.``BR_07_BINDING_001 C2 resolution happens once and holds no source``() =
        let attributeOf, north, _ = registry ()
        let fields = typeof<AttributeBindingRecord>.GetProperties() |> Array.map _.PropertyType.Name
        let resolution =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate bravo "north"; candidate alpha "south" ]
        multiple (fun () ->
            Assert.That(AttributeConstrainedBinding.isResolved resolution, Is.True)
            Assert.That(
                fields |> Array.exists (fun field -> field.Contains "AttributeCandidate"),
                Is.False,
                "A resolved binding records values, never the candidate set it was resolved from."))

    [<Test>]
    member _.``BR_07_BINDING_001 C3 the binding records effective values and why it selected``() =
        let attributeOf, north, _ = registry ()
        let resolution =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate alpha "south"; candidate bravo "north"; candidate charlie "north" ]
        multiple (fun () ->
            Assert.That(resolution.Binding.Value.SelectedProvider, Is.EqualTo bravo)
            Assert.That(
                String.concat "," (resolution.Provenance |> List.map (fun item -> CanonicalName.value item.Provider)),
                Is.EqualTo(String.concat "," [ CanonicalName.value alpha; CanonicalName.value bravo ]),
                "Candidates are accounted for in evaluation order, and evaluation stops at the selection.")
            Assert.That((List.head resolution.Provenance).Disposition, Is.EqualTo Unsatisfied)
            Assert.That(
                String.concat
                    ","
                    (resolution.Binding.Value.EffectiveValues
                     |> List.map (fun item -> CanonicalName.value item.Attribute)),
                Is.EqualTo(CanonicalName.value region),
                "Every Attribute the constraint referenced has a recorded effective value."))

    [<Test>]
    member _.``BR_07_BINDING_001 C4 a later Attribute change never rebinds``() =
        let attributeOf, north, _ = registry ()
        let before = [ candidate bravo "north"; candidate alpha "south" ]
        let resolved =
            (AttributeConstrainedBinding.resolve attributeOf binding (north "north") before).Binding.Value
        // The change is material: bravo now reports south, so a fresh resolution finds no candidate.
        let after = [ candidate bravo "south"; candidate alpha "south" ]
        let fresh = AttributeConstrainedBinding.resolve attributeOf binding (north "north") after
        multiple (fun () ->
            Assert.That(
                AttributeConstrainedBinding.isResolved fresh,
                Is.False,
                "The Attribute change would have changed the answer.")
            Assert.That(resolved.SelectedProvider, Is.EqualTo bravo, "And the existing binding did not move.")
            Assert.That(
                (List.exactlyOne resolved.EffectiveValues).Value,
                Is.EqualTo(TextValue "north"),
                "It still reports the value that decided it."))

    [<Test>]
    member _.``BR_07_BINDING_001 C5 a later candidate change never rebinds not even a better one``() =
        let attributeOf, north, _ = registry ()
        let resolved =
            (AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate bravo "north" ])
                .Binding.Value
        // alpha sorts before bravo, so a fresh resolution would prefer it.
        let better =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate bravo "north"; candidate alpha "north" ]
        // Removing the selected candidate is equally inert.
        let removed =
            AttributeConstrainedBinding.resolve attributeOf binding (north "north") [ candidate alpha "north" ]
        multiple (fun () ->
            Assert.That(
                better.Binding.Value.SelectedProvider,
                Is.EqualTo alpha,
                "A fresh resolution prefers the new candidate.")
            Assert.That(removed.Binding.Value.SelectedProvider, Is.EqualTo alpha)
            Assert.That(
                resolved.SelectedProvider,
                Is.EqualTo bravo,
                "The existing binding is untouched by both."))

    [<Test>]
    member _.``BR_07_BINDING_001 C6 an unresolved binding fails explicitly and is never pending``() =
        let attributeOf, north, _ = registry ()
        let none =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate alpha "south"; candidate bravo "south" ]
        let empty = AttributeConstrainedBinding.resolve attributeOf binding (north "north") []
        multiple (fun () ->
            Assert.That(AttributeConstrainedBinding.isResolved none, Is.False)
            Assert.That(none.Binding, Is.EqualTo None, "There is no partially resolved binding to observe.")
            Assert.That(none.Provenance.Length, Is.EqualTo 2, "The failure explains every candidate it excluded.")
            Assert.That(none.Reason, Does.Contain "No candidate satisfies")
            Assert.That(AttributeConstrainedBinding.isResolved empty, Is.False)
            Assert.That(empty.Provenance, Is.Empty))

    [<Test>]
    member _.``BR_07_BINDING_001 C7 an unevaluatable constraint excludes only its own candidate``() =
        let attributeOf, north, exoticAtom = registry ()
        let constraintExpression = AllOf [ north "north"; exoticAtom "yes" ]
        // alpha cannot answer the exotic atom at all; charlie can.
        let charlieCandidate =
            { Provider = charlie
              Attributes = [ sourced region "north"; sourced exotic "yes" ] }
        let resolution =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                constraintExpression
                [ candidate alpha "north"; charlieCandidate ]
        multiple (fun () ->
            Assert.That((List.head resolution.Provenance).Disposition, Is.EqualTo Unevaluatable)
            Assert.That(
                String.concat
                    ","
                    ((List.head resolution.Provenance).UnsupportedConstraints
                     |> List.map CanonicalName.value),
                Is.EqualTo(CanonicalName.value exotic),
                "The exclusion names the constraint that could not be evaluated.")
            Assert.That(
                resolution.Binding.Value.SelectedProvider,
                Is.EqualTo charlie,
                "Poisoning excludes the candidate it was evaluated against, not its neighbours."))

    [<Test>]
    member _.``BR_07_BINDING_001 C8 selection is deterministic including under ties``() =
        let attributeOf, north, _ = registry ()
        let forward =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate alpha "north"; candidate bravo "north"; candidate charlie "north" ]
        let reversed =
            AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate charlie "north"; candidate bravo "north"; candidate alpha "north" ]
        multiple (fun () ->
            Assert.That(forward.Binding.Value.SelectedProvider, Is.EqualTo alpha)
            Assert.That(
                reversed.Binding.Value.SelectedProvider,
                Is.EqualTo forward.Binding.Value.SelectedProvider,
                "Three equally satisfying candidates resolve the same whatever order the caller supplied."))

    [<Test>]
    member _.``BR_07_BINDING_001 C9 restoration reproduces the resolution without reselecting``() =
        let attributeOf, north, _ = registry ()
        let resolved =
            (AttributeConstrainedBinding.resolve
                attributeOf
                binding
                (north "north")
                [ candidate bravo "north"; candidate alpha "south" ])
                .Binding.Value
        let restored = AttributeConstrainedBinding.restore attributeOf (north "north") resolved
        // A record whose effective values do not satisfy the constraint is refused, not restored.
        let tampered =
            { resolved with EffectiveValues = [ sourced region "south" ] }
        let refused = AttributeConstrainedBinding.restore attributeOf (north "north") tampered
        multiple (fun () ->
            Assert.That(AttributeConstrainedBinding.isResolved restored, Is.True)
            Assert.That(restored.Binding.Value.SelectedProvider, Is.EqualTo bravo)
            Assert.That(restored.Binding.Value.EffectiveValues = resolved.EffectiveValues, Is.True)
            Assert.That(AttributeConstrainedBinding.isResolved refused, Is.False))

    [<Test>]
    member _.``BR_07_BINDING_001 C10 selection grants no authority``() =
        // Both the declared name and the carrying type matter: an authority fact smuggled through a
        // general-purpose name type is still an authority fact.
        let surface =
            [ typeof<AttributeBindingRecord>
              typeof<AttributeBindingResolution>
              typeof<AttributeCandidateOutcome> ]
        let authorityBearing =
            surface
            |> List.collect (fun item -> item.GetProperties() |> List.ofArray)
            |> List.collect (fun property -> [ property.Name; property.PropertyType.Name ])
            |> List.filter (fun value ->
                value.Contains "Capability" || value.Contains "Grant" || value.Contains "Authority")
        Assert.That(
            authorityBearing,
            Is.Empty,
            "§18.1: a Definition Constraint selects or validates without granting authority.")
