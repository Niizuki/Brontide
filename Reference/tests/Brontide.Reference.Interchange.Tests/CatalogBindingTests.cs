using System.Collections.Immutable;
using System.Text.Json;
using Brontide.Reference.Experimental.Binding;

namespace Brontide.Reference.Interchange.Tests;

public sealed class CatalogBindingTests
{
    private static string Fixture(params string[] parts) =>
        Path.Combine([TestContext.CurrentContext.TestDirectory, "interchange", "catalog", .. parts]);

    [Test]
    public void Neutral_catalog_manifest_round_trips_with_two_operations_and_a_resource_boundary()
    {
        var manifest = CatalogManifestCodec.Decode(File.ReadAllText(Fixture("manifest-v1.json")));
        var roundTrip = CatalogManifestCodec.Decode(CatalogManifestCodec.Encode(manifest));

        Assert.Multiple(() =>
        {
            Assert.That(roundTrip.Operations, Has.Length.EqualTo(2));
            Assert.That(roundTrip.ResourceBoundary, Is.EqualTo("provider-scoped-resource-handle"));
            Assert.That(roundTrip.PayloadLimitBytes, Is.EqualTo(65_536));
        });
    }

    [Test]
    public async Task Catalog_endpoint_rejects_malformed_unknown_version_replay_and_oversized_vectors()
    {
        var valid = File.ReadAllText(Fixture("vectors", "valid-upsert.json")).TrimEnd();
        var lines = new[]
        {
            File.ReadAllText(Fixture("vectors", "malformed.json")).TrimEnd(),
            File.ReadAllText(Fixture("vectors", "unknown-field.json")).TrimEnd(),
            File.ReadAllText(Fixture("vectors", "unknown-variant.json")).TrimEnd(),
            File.ReadAllText(Fixture("vectors", "version-skew.json")).TrimEnd(),
            valid,
            File.ReadAllText(Fixture("vectors", "replay.json")).TrimEnd(),
            "{\"padding\":\"" + new string('x', CatalogContract.PayloadLimitBytes + 1) + "\"}",
            "{\"protocolVersion\":1,\"kind\":\"shutdown\",\"requestId\":\"91111111-1111-1111-1111-111111111111\"}"
        };
        using var input = new StringReader(string.Join(Environment.NewLine, lines));
        using var output = new StringWriter();
        var endpoint = new CatalogProviderEndpoint(invocation => CatalogProviderReply.StoredItems(invocation.Items.Length));

        var exitCode = await endpoint.RunAsync(input, output);
        var responses = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var codes = responses.Select(ReadCode).Where(code => code is not null).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(responses, Has.Length.EqualTo(lines.Length));
            Assert.That(codes, Does.Contain("invalid-message"));
            Assert.That(codes, Does.Contain("unknown-field"));
            Assert.That(codes, Does.Contain("unknown-operation"));
            Assert.That(codes, Does.Contain("unsupported-version"));
            Assert.That(codes, Does.Contain("replay"));
            Assert.That(codes, Does.Contain("payload-limit"));
        });
    }

    private static string? ReadCode(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}

[Category("CrossProcess")]
public sealed class ReferenceHostsMinimalCatalogTests
{
    private static ProviderLaunch MinimalCatalogProvider()
    {
        var path = Environment.GetEnvironmentVariable("BRONTIDE_MINIMAL_PROVIDER");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Assert.Ignore("BRONTIDE_MINIMAL_PROVIDER does not name a built Brontide.Minimal provider endpoint.");
        }

        return new ProviderLaunch(path!, "--catalog");
    }

    [Test]
    public async Task Catalog_nested_batch_two_operations_and_explicit_failure_cross_the_Minimal_process()
    {
        var client = new CatalogProcessClient(MinimalCatalogProvider(), TimeSpan.FromSeconds(10));
        var items = ImmutableArray.Create(
            new CatalogItem("alpha", "Alpha", ["nested", "repeated"]),
            new CatalogItem("beta", "Beta", ["second"]));

        var result = await client.RunScenarioAsync(new("catalog-sandbox", "shared"), items);

        Assert.Multiple(() =>
        {
            Assert.That(result.ProviderStarts, Is.EqualTo(1));
            Assert.That(result.Upsert.Succeeded, Is.True);
            Assert.That(result.Upsert.Stored, Is.EqualTo(2));
            Assert.That(result.Find.Succeeded, Is.True);
            Assert.That(result.Find.Items, Has.Length.EqualTo(2));
            Assert.That(result.Find.Items[0].Tags, Is.EqualTo(new[] { "nested", "repeated" }));
            Assert.That(result.Missing.Succeeded, Is.False);
            Assert.That(result.Missing.Code, Is.EqualTo("missing-items"));
            Assert.That(result.Missing.MissingIds, Is.EqualTo(new[] { "missing-item" }));
        });
    }

    [Test]
    public async Task Catalog_provider_refuses_a_resource_handle_outside_its_declared_scope()
    {
        var client = new CatalogProcessClient(MinimalCatalogProvider(), TimeSpan.FromSeconds(10));
        var result = await client.RunScenarioAsync(
            new("catalog-sandbox", "outside-scope"),
            [new CatalogItem("alpha", "Alpha", ["scope-test"])]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Upsert.Succeeded, Is.False);
            Assert.That(result.Upsert.Code, Is.EqualTo("resource-refused"));
            Assert.That(result.Find.Succeeded, Is.False);
            Assert.That(result.Find.Code, Is.EqualTo("resource-refused"));
        });
    }
}
