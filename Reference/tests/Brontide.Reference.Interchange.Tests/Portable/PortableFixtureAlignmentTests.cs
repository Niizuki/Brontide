using System.Collections.Immutable;
using System.Text.Json;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// This stack's fixtures declare the same contract the neutral layer declares.
/// </summary>
/// <remarks>
/// PB1 declared only the Cooling fixture, so each stack authored its own Catalog fixture and the two
/// drifted: the Operation names and one <c>providerSpecific</c> flag disagreed. Negotiation matches
/// both exactly, so the drift made the two stacks unable to establish a Catalog binding at all — and
/// it stayed invisible while each stack ran Catalog only against itself. This suite compares the
/// negotiation surface against the checked-in declaration, so the next drift fails here rather than
/// in the cross-stack matrix, where the cause is much less obvious.
/// </remarks>
public sealed class PortableFixtureAlignmentTests
{
    public static IEnumerable<TestCaseData> Fixtures =>
    [
        new TestCaseData("fixture-contract.json", CoolingPortableFixture.Contract).SetName("cooling"),
        new TestCaseData("catalog-fixture-contract.json", CatalogPortableFixture.Contract).SetName("catalog")
    ];

    [TestCaseSource(nameof(Fixtures))]
    public void The_negotiation_surface_matches_the_neutral_declaration(
        string artifact,
        PortableContractDocument contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        using var document = PortableTestHarness.ReadNeutral("vectors", artifact);
        var root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(contract.ContractVersion, Is.EqualTo(root.GetProperty("contractVersion").GetInt32()));
            Assert.That(contract.Component.ToString(), Is.EqualTo(Reference(root.GetProperty("component"))));
            Assert.That(contract.Provider.ToString(), Is.EqualTo(Reference(root.GetProperty("provider"))));

            Assert.That(
                contract.Provisions.Select(provision =>
                    $"{provision.Kind.Token()} {provision.Reference} providerSpecific={Flag(provision.ProviderSpecific)}"),
                Is.EquivalentTo(root.GetProperty("provisions").EnumerateArray().Select(provision =>
                    $"{provision.GetProperty("kind").GetString()} {Reference(provision.GetProperty("reference"))}" +
                    $" providerSpecific={Flag(provision.GetProperty("providerSpecific").GetBoolean())}")),
                "Provisions differ from the neutral declaration.");

            Assert.That(
                contract.Requirements.Select(requirement =>
                    $"{requirement.Kind.Token()} {requirement.Reference} {requirement.Strength.Token()}" +
                    $" providerSpecific={Flag(requirement.ProviderSpecific)}"),
                Is.EquivalentTo(root.GetProperty("requirements").EnumerateArray().Select(requirement =>
                    $"{requirement.GetProperty("kind").GetString()} {Reference(requirement.GetProperty("reference"))}" +
                    $" {requirement.GetProperty("strength").GetString()}" +
                    $" providerSpecific={Flag(requirement.GetProperty("providerSpecific").GetBoolean())}")),
                "Requirements differ from the neutral declaration.");

            Assert.That(
                contract.Operations.Select(operation =>
                    $"{operation.Reference} in={operation.InputShape} out={operation.ResultShape}" +
                    $" detail={operation.DetailShape}" +
                    $" fragments=[{string.Join(",", operation.RequiredFragments)}]" +
                    $" flavors=[{string.Join(",", operation.ResourceFlavors)}]"),
                Is.EquivalentTo(root.GetProperty("operations").EnumerateArray().Select(operation =>
                    $"{Reference(operation.GetProperty("reference"))}" +
                    $" in={Reference(operation.GetProperty("inputShape"))}" +
                    $" out={Reference(operation.GetProperty("resultShape"))}" +
                    $" detail={Reference(operation.GetProperty("detailShape"))}" +
                    $" fragments=[{string.Join(",", operation.GetProperty("requiredFragments").EnumerateArray().Select(Reference))}]" +
                    $" flavors=[{string.Join(",", operation.GetProperty("resourceFlavors").EnumerateArray().Select(flavor => flavor.GetString()))}]")),
                "Operation declarations differ from the neutral declaration.");

            var representation = root.GetProperty("representation");
            Assert.That(contract.Representation.Representation, Is.EqualTo(representation.GetProperty("representation").GetString()));
            Assert.That(contract.Representation.Framing, Is.EqualTo(representation.GetProperty("framing").GetString()));
            Assert.That(
                contract.Representation.ResourceFlavors,
                Is.EquivalentTo(representation.GetProperty("resourceFlavors").EnumerateArray().Select(flavor => flavor.GetString())));
            Assert.That(
                contract.Representation.AcceptedResourceHandles,
                Is.EquivalentTo(representation.GetProperty("acceptedResourceHandles").EnumerateArray().Select(handle => handle.GetString())));

            // Every Shape the neutral declaration names is declared here. This stack may declare more
            // — the Cooling fixture adds the encoding-edge Shapes the golden encodings need — but it
            // may not omit one the contract's Operations depend on.
            Assert.That(
                contract.Shapes.Select(shape => shape.Reference.ToString()).ToImmutableHashSet(),
                Is.SupersetOf(root.GetProperty("shapes").EnumerateArray().Select(shape => Reference(shape.GetProperty("reference")))));
        });
    }

    private static string Reference(JsonElement element) =>
        $"{element.GetProperty("name").GetString()}@{element.GetProperty("version").GetInt32()}";

    private static string Flag(bool value) => value ? "true" : "false";
}
