using System.Collections;
using System.Collections.Immutable;
using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Composition;
using NUnit.Framework;

namespace Brontide.Reference.Studio.Tests;

/// <summary>
/// Architecture 0.7 §18.1 change C3, requirement BR-07-BINDING-001. Contract items are named by the
/// shared behavioural contract at <c>conformance/br-07-binding-001-contract.md</c>.
/// </summary>
[TestFixture]
public sealed class AttributeBindingTests
{
    private static readonly CanonicalName Binding = CanonicalName.Parse("Brontide:Binding.Cooling");
    private static readonly CanonicalName Region = CanonicalName.Parse("Brontide:Attribute.Region");
    private static readonly CanonicalName Tier = CanonicalName.Parse("Brontide:Attribute.Tier");
    private static readonly CanonicalName Exotic = CanonicalName.Parse("Brontide:Attribute.Exotic");
    private static readonly CanonicalName ReadRegion = CanonicalName.Parse("Brontide:Operation.ReadRegion");
    private static readonly CanonicalName Alpha = CanonicalName.Parse("Brontide:Provider.Alpha");
    private static readonly CanonicalName Bravo = CanonicalName.Parse("Brontide:Provider.Bravo");
    private static readonly CanonicalName Charlie = CanonicalName.Parse("Brontide:Provider.Charlie");

    [Test]
    public void BR_07_BINDING_001_C1_an_Attribute_is_a_sourced_value_never_a_label()
    {
        var resolution = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), [North(Bravo)]);
        var effective = resolution.Binding!.EffectiveValues.Single();

        Assert.Multiple(() =>
        {
            Assert.That(effective.SourceOperation, Is.EqualTo(ReadRegion));
            Assert.That(effective.VocabularyVersion, Is.EqualTo("1"));
            Assert.That(effective.ResultShape, Is.EqualTo(BuiltInShapes.Text));
            Assert.That(effective.ResultPath, Is.EqualTo("/region"));
            Assert.That(
                resolution.Provenance.Single().Reason,
                Does.Contain(ReadRegion.ToString()),
                "Provenance names the Operation that answered, not merely the value.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C2_resolution_happens_once_and_holds_no_source()
    {
        var holdsCollection = typeof(AttributeBindingRecord)
            .GetProperties()
            .Where(property => property.PropertyType != typeof(string))
            .Where(property => typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            .Select(property => property.PropertyType)
            .ToArray();
        var resolution = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), [North(Bravo), South(Alpha)]);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.IsResolved, Is.True);
            Assert.That(
                holdsCollection,
                Has.None.EqualTo(typeof(ImmutableArray<AttributeCandidate>))
                    .And.None.EqualTo(typeof(IEnumerable<AttributeCandidate>)),
                "A resolved binding records values, never the candidate set it was resolved from.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C3_the_binding_records_effective_values_and_why_it_selected()
    {
        var resolution = AttributeConstrainedBinding.Resolve(
            Binding,
            NorthConstraint(),
            [South(Alpha), North(Bravo), North(Charlie)]);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Binding!.SelectedProvider, Is.EqualTo(Bravo));
            Assert.That(
                resolution.Provenance.Select(item => item.Provider),
                Is.EqualTo(new[] { Alpha, Bravo }),
                "Candidates are accounted for in evaluation order, and evaluation stops at the selection.");
            Assert.That(resolution.Provenance[0].Disposition, Is.EqualTo(AttributeCandidateDisposition.Unsatisfied));
            Assert.That(
                resolution.Binding.EffectiveValues.Select(item => item.Attribute),
                Is.EqualTo(new[] { Region }),
                "Every Attribute the constraint referenced has a recorded effective value.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C4_a_later_Attribute_change_never_rebinds()
    {
        var candidates = new List<AttributeCandidate> { North(Bravo), South(Alpha) };
        var resolved = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), candidates).Binding!;

        // The change is material: Bravo now reports south, so a fresh resolution finds no candidate.
        candidates[0] = South(Bravo);
        var fresh = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), candidates);

        Assert.Multiple(() =>
        {
            Assert.That(fresh.IsResolved, Is.False, "The Attribute change would have changed the answer.");
            Assert.That(resolved.SelectedProvider, Is.EqualTo(Bravo), "And the existing binding did not move.");
            Assert.That(
                resolved.EffectiveValues.Single().Value.RequireScalar<string>(),
                Is.EqualTo("north"),
                "It still reports the value that decided it.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C5_a_later_candidate_change_never_rebinds_not_even_a_better_one()
    {
        var candidates = new List<AttributeCandidate> { North(Bravo) };
        var resolved = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), candidates).Binding!;

        // Alpha sorts before Bravo, so a fresh resolution would prefer it.
        candidates.Add(North(Alpha));
        var better = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), candidates);

        // Removing the selected candidate is equally inert.
        candidates.RemoveAll(candidate => candidate.Provider == Bravo);
        var removed = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), candidates);

        Assert.Multiple(() =>
        {
            Assert.That(better.Binding!.SelectedProvider, Is.EqualTo(Alpha), "A fresh resolution prefers the new candidate.");
            Assert.That(removed.Binding!.SelectedProvider, Is.EqualTo(Alpha), "And no longer sees the old one at all.");
            Assert.That(resolved.SelectedProvider, Is.EqualTo(Bravo), "The existing binding is untouched by both.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C6_an_unresolved_binding_fails_explicitly_and_is_never_pending()
    {
        var none = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), [South(Alpha), South(Bravo)]);
        var empty = AttributeConstrainedBinding.Resolve(Binding, NorthConstraint(), []);

        Assert.Multiple(() =>
        {
            Assert.That(none.IsResolved, Is.False);
            Assert.That(none.Binding, Is.Null, "There is no partially resolved binding to observe.");
            Assert.That(none.Provenance, Has.Length.EqualTo(2), "The failure explains every candidate it excluded.");
            Assert.That(none.Reason, Does.Contain("No candidate satisfies"));
            Assert.That(empty.IsResolved, Is.False);
            Assert.That(empty.Provenance, Is.Empty);
        });
    }

    [Test]
    public void BR_07_BINDING_001_C7_an_unevaluatable_constraint_excludes_only_its_own_candidate()
    {
        var constraint = new AllOfConstraintExpression(
            new AttributeConstraint(Region, ShapeValue.Text("north")),
            new AttributeConstraint(Exotic, ShapeValue.Text("yes")));
        // Alpha cannot answer the exotic atom at all; Charlie can.
        var charlie = new AttributeCandidate(
            Charlie,
            Sourced(Region, "north"),
            Sourced(Exotic, "yes"));
        var resolution = AttributeConstrainedBinding.Resolve(Binding, constraint, [North(Alpha), charlie]);

        Assert.Multiple(() =>
        {
            Assert.That(resolution.Provenance[0].Disposition, Is.EqualTo(AttributeCandidateDisposition.Unevaluatable));
            Assert.That(
                resolution.Provenance[0].UnsupportedConstraints,
                Is.EqualTo(new[] { Exotic }),
                "The exclusion names the constraint that could not be evaluated.");
            Assert.That(
                resolution.Binding!.SelectedProvider,
                Is.EqualTo(Charlie),
                "Poisoning excludes the candidate it was evaluated against, not its neighbours.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C8_selection_is_deterministic_including_under_ties()
    {
        var forward = AttributeConstrainedBinding.Resolve(
            Binding,
            NorthConstraint(),
            [North(Alpha), North(Bravo), North(Charlie)]);
        var reversed = AttributeConstrainedBinding.Resolve(
            Binding,
            NorthConstraint(),
            [North(Charlie), North(Bravo), North(Alpha)]);

        Assert.Multiple(() =>
        {
            Assert.That(forward.Binding!.SelectedProvider, Is.EqualTo(Alpha));
            Assert.That(
                reversed.Binding!.SelectedProvider,
                Is.EqualTo(forward.Binding.SelectedProvider),
                "Three equally satisfying candidates resolve the same whatever order the caller supplied.");
        });
    }

    [Test]
    public void BR_07_BINDING_001_C9_restoration_reproduces_the_resolution_without_reselecting()
    {
        var parameters = typeof(AttributeConstrainedBinding)
            .GetMethod(nameof(AttributeConstrainedBinding.Restore))!
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        var resolved = AttributeConstrainedBinding
            .Resolve(Binding, NorthConstraint(), [North(Bravo), South(Alpha)]).Binding!;
        var restored = AttributeConstrainedBinding.Restore(NorthConstraint(), resolved);

        // A record whose effective values do not satisfy the constraint is refused, not restored.
        var tampered = resolved with { EffectiveValues = [Sourced(Region, "south")] };
        var refused = AttributeConstrainedBinding.Restore(NorthConstraint(), tampered);

        Assert.Multiple(() =>
        {
            Assert.That(
                parameters,
                Has.None.EqualTo(typeof(IEnumerable<AttributeCandidate>))
                    .And.None.EqualTo(typeof(ImmutableArray<AttributeCandidate>)),
                "Restoration takes no candidate set, so it cannot silently reselect against one.");
            Assert.That(restored.IsResolved, Is.True);
            Assert.That(restored.Binding!.SelectedProvider, Is.EqualTo(Bravo));
            Assert.That(restored.Binding.EffectiveValues, Is.EqualTo(resolved.EffectiveValues));
            Assert.That(refused.IsResolved, Is.False);
        });
    }

    [Test]
    public void BR_07_BINDING_001_C10_selection_grants_no_authority()
    {
        var surface = new[]
        {
            typeof(AttributeBindingRecord),
            typeof(AttributeBindingResolution),
            typeof(AttributeCandidateOutcome),
        };
        // Both the declared name and the carrying type matter: an authority fact smuggled through a
        // general-purpose name type is still an authority fact.
        var authorityBearing = surface
            .SelectMany(type => type.GetProperties())
            .SelectMany(property => new[] { property.Name, property.PropertyType.Name })
            .Where(name => name.Contains("Capability", StringComparison.Ordinal)
                || name.Contains("Grant", StringComparison.Ordinal)
                || name.Contains("Authority", StringComparison.Ordinal))
            .ToArray();

        Assert.That(
            authorityBearing,
            Is.Empty,
            "§18.1: a Definition Constraint selects or validates without granting authority.");
    }

    private static ConstraintExpression NorthConstraint() =>
        new AttributeConstraint(Region, ShapeValue.Text("north"));

    private static AttributeCandidate North(CanonicalName provider) =>
        new(provider, Sourced(Region, "north"), Sourced(Tier, "primary"));

    private static AttributeCandidate South(CanonicalName provider) =>
        new(provider, Sourced(Region, "south"), Sourced(Tier, "primary"));

    private static AttributeValue Sourced(CanonicalName attribute, string value) =>
        new(attribute, ReadRegion, "1", BuiltInShapes.Text, "/region", ShapeValue.Text(value));
}
