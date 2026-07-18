using Brontide.Reference.Core;

namespace Brontide.Reference.Conformance;

public sealed class ShapeConformanceTests
{
    [Test]
    [SpecSection("16.2")]
    [SpecSection("16.3")]
    [SpecSection("16.4")]
    [SpecSection("29.2")]
    public void Open_shape_projects_unknown_optional_authored_fragment_canonically()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var velocity = ShapeReference.Parse("Velocity", 1);
        var direction = ShapeReference.Parse("Bob:Direction", 1);
        var directional = FragmentReference.Parse("Bob:DirectionalVelocity", 1);
        registry.Register(ShapeDefinition.Scalar<string>(direction));
        registry.Register(ShapeDefinition.Record(velocity, FragmentPolicy.Open,
            RecordField.Required("speed", BuiltInShapes.Signed64)));
        registry.Register(DeclaredFragmentDefinition.Attached(directional, velocity,
            RecordField.Required("direction", direction)));

        var composed = ShapeValue.Record(
            velocity,
            [("speed", ShapeValue.Signed64(12))],
            [(directional, new Dictionary<string, ShapeValue>
            {
                ["direction"] = ShapeValue.Scalar(direction, "north")
            })]);

        var canonical = registry.Project(composed, ShapeContract.For(velocity));
        var required = registry.Project(composed, ShapeContract.For(velocity, directional));
        var missing = registry.Project(
            ShapeValue.Record(velocity, ("speed", ShapeValue.Signed64(12))),
            ShapeContract.For(velocity, directional));

        Assert.That(canonical.IsValid, Is.True, canonical.Message);
        Assert.That(canonical.IgnoredFragments, Does.Contain(directional));
        Assert.That(canonical.Value!.Fragments, Is.Empty);
        Assert.That(required.IsValid, Is.True, required.Message);
        Assert.That(required.UnderstoodFragments, Does.Contain(directional));
        Assert.That(missing.IsValid, Is.False);
    }

    [Test]
    [SpecSection("16.2")]
    public void Same_name_versions_are_additive_and_redefinition_is_rejected()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var v1 = ShapeReference.Parse("Example.Record", 1);
        var v2 = ShapeReference.Parse("Example.Record", 2);
        registry.Register(ShapeDefinition.Record(v1, FragmentPolicy.Open,
            RecordField.Required("value", BuiltInShapes.Text)));
        registry.Register(ShapeDefinition.Record(v2, FragmentPolicy.Open,
            RecordField.Required("value", BuiltInShapes.Text),
            RecordField.Optional("comment", BuiltInShapes.Text)));
        var laterValue = ShapeValue.Record(
            v2,
            ("value", ShapeValue.Text("stable")),
            ("comment", ShapeValue.Text("optional addition")));
        var earlierProjection = registry.Project(laterValue, ShapeContract.For(v1));

        Assert.That(() => registry.Register(
            ShapeDefinition.Record(ShapeReference.Parse("Example.Record", 3), FragmentPolicy.Open,
                RecordField.Required("value", BuiltInShapes.Signed64))),
            Throws.TypeOf<ShapeRegistrationException>());
        Assert.That(earlierProjection.IsValid, Is.True, earlierProjection.Message);
        Assert.That(((RecordShapeValue)earlierProjection.Value!).Fields.Keys, Is.EqualTo(new[] { "value" }));
    }

    [Test]
    [SpecSection("16.3")]
    [SpecSection("16.4")]
    public void Closed_shapes_reject_authored_attachment_and_transparent_forwarding_preserves_unknowns()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var closed = ShapeReference.Parse("Example.Closed", 1);
        registry.Register(ShapeDefinition.Record(closed, FragmentPolicy.Closed,
            RecordField.Required("value", BuiltInShapes.Text)));

        var fragment = FragmentReference.Parse("Other:Extra", 1);
        Assert.That(() => registry.Register(DeclaredFragmentDefinition.Attached(fragment, closed,
                RecordField.Optional("extra", BuiltInShapes.Text))),
            Throws.TypeOf<ShapeRegistrationException>());

        var open = ShapeReference.Parse("Example.Open", 1);
        registry.Register(ShapeDefinition.Record(open, FragmentPolicy.Open,
            RecordField.Required("value", BuiltInShapes.Text)));
        var unknown = ShapeValue.Record(
            open,
            [("value", ShapeValue.Text("canonical"))],
            [(fragment, new Dictionary<string, ShapeValue> { ["extra"] = ShapeValue.Text("preserve me") })]);

        var projection = registry.Project(unknown, ShapeContract.For(open));
        var forwarded = ShapeRegistry.PreserveForForwarding(unknown);

        Assert.That(projection.IsValid, Is.True, projection.Message);
        Assert.That(projection.IgnoredFragments, Does.Contain(fragment));
        Assert.That(projection.Value!.Fragments, Is.Empty);
        Assert.That(forwarded, Is.SameAs(unknown));
        Assert.That(forwarded.Fragments, Does.ContainKey(fragment));
    }

    [Test]
    [SpecSection("13.2")]
    [SpecSection("16.3")]
    public void Reusable_fragment_requires_explicit_host_inclusion_even_on_closed_shape()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        var interaction = FragmentReference.Parse("Interaction", 1);
        registry.Register(DeclaredFragmentDefinition.Reusable(interaction,
            RecordField.Required("actor", BuiltInShapes.Text)));
        var occurrence = ShapeReference.Parse("Example.Occurrence", 1);
        registry.Register(ShapeDefinition.RecordIncluding(
            occurrence,
            FragmentPolicy.Closed,
            [],
            [interaction]));

        var missing = registry.Project(
            ShapeValue.Record(occurrence),
            ShapeContract.For(occurrence));
        var complete = registry.Project(
            ShapeValue.Record(
                occurrence,
                [],
                [(interaction, new Dictionary<string, ShapeValue> { ["actor"] = ShapeValue.Text("A") })]),
            ShapeContract.For(occurrence));

        Assert.That(missing.IsValid, Is.False);
        Assert.That(complete.IsValid, Is.True, complete.Message);
    }
}
