using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Binding;

namespace Brontide.Reference.Interchange.Tests;

public sealed class FixtureContractTests
{
    private static string Fixture(params string[] parts) =>
        Path.Combine([TestContext.CurrentContext.TestDirectory, "interchange", .. parts]);

    [Test]
    public void Neutral_manifest_round_trips_and_declares_the_complete_v2_contract()
    {
        var json = File.ReadAllText(Fixture("manifest-v2.json"));
        var decoded = ManifestCodec.Decode(json);
        var roundTripped = ManifestCodec.Decode(ManifestCodec.Encode(decoded));

        Assert.Multiple(() =>
        {
            Assert.That(roundTripped.ProtocolVersion, Is.EqualTo(2));
            Assert.That(roundTripped.Component.Name, Is.EqualTo("interchange.tests.cooling-component"));
            Assert.That(roundTripped.Operations.Single().RequiredFragments.Single().Name,
                Is.EqualTo("interchange.tests.cooling.host-context"));
            Assert.That(roundTripped.Dependencies.Any(item =>
                item.ProviderSpecific && item.Strength == "required"), Is.True);
            Assert.That(roundTripped.Binding.Limitations, Does.Contain("no-capability-transfer"));
        });
    }

    [Test]
    public void Golden_values_round_trip_and_fail_closed_for_equivalent_invalid_reasons()
    {
        var registry = ShapeRegistry.CreateWithBuiltIns();
        InterchangeCoolingContract.RegisterShapes(registry, includeOptionalLocalFragment: true);
        var valid = PortableShapeValueCodec.Decode(
            File.ReadAllText(Fixture("values", "valid-command.json")),
            InterchangeCoolingContract.CommandShape);
        var optional = PortableShapeValueCodec.Decode(
            File.ReadAllText(Fixture("values", "valid-command-with-optional-fragment.json")),
            InterchangeCoolingContract.CommandShape);
        var missing = PortableShapeValueCodec.Decode(
            File.ReadAllText(Fixture("values", "invalid-command-missing-fragment.json")),
            InterchangeCoolingContract.CommandShape);
        var wrongKind = PortableShapeValueCodec.Decode(
            File.ReadAllText(Fixture("values", "invalid-command-wrong-kind.json")),
            InterchangeCoolingContract.CommandShape);

        Assert.Multiple(() =>
        {
            Assert.That(registry.Project(
                valid,
                ShapeContract.For(InterchangeCoolingContract.CommandShape, InterchangeCoolingContract.HostContext)).IsValid,
                Is.True);
            Assert.That(registry.Project(
                optional,
                ShapeContract.For(InterchangeCoolingContract.CommandShape, InterchangeCoolingContract.HostContext)).IsValid,
                Is.True);
            Assert.That(registry.Project(
                missing,
                ShapeContract.For(InterchangeCoolingContract.CommandShape, InterchangeCoolingContract.HostContext)).Message,
                Does.Contain("required Fragment").IgnoreCase);
            Assert.That(registry.Project(
                wrongKind,
                ShapeContract.For(InterchangeCoolingContract.CommandShape, InterchangeCoolingContract.HostContext)).Message,
                Does.Contain("cannot project to Boolean"));
        });

        var roundTrip = PortableShapeValueCodec.Decode(
            PortableShapeValueCodec.Encode(optional),
            InterchangeCoolingContract.CommandShape);
        Assert.That(roundTrip.Fragments.Keys, Does.Contain(InterchangeCoolingContract.OptionalForwardingNote));
    }

    [Test]
    public void Boundary_rejects_private_type_metadata_duplicate_fields_and_exception_shaped_data()
    {
        var privateType = File.ReadAllText(Fixture("values", "invalid-private-type.json"));
        const string duplicate =
            "{\"kind\":\"record\",\"kind\":\"record\",\"fields\":{},\"fragments\":{}}";
        const string exception =
            "{\"kind\":\"record\",\"fields\":{\"exception\":{\"kind\":\"text\",\"value\":\"bad\"}},\"fragments\":{}}";

        Assert.Multiple(() =>
        {
            Assert.That(
                () => PortableShapeValueCodec.Decode(privateType, InterchangeCoolingContract.CommandShape),
                Throws.TypeOf<BoundaryProtocolException>().With.Message.Contains("type or exception metadata"));
            Assert.That(
                () => PortableShapeValueCodec.Decode(duplicate, InterchangeCoolingContract.CommandShape),
                Throws.TypeOf<BoundaryProtocolException>().With.Message.Contains("Duplicate"));
            Assert.That(
                () => PortableShapeValueCodec.Decode(exception, InterchangeCoolingContract.CommandShape),
                Throws.TypeOf<BoundaryProtocolException>().With.Message.Contains("type or exception metadata"));
        });
    }
}
