using System.Text.Json;

namespace Brontide.Reference.Experimental.ComponentManagement;

/// <summary>
/// Strictly loads the CM1-only evidence-availability seam without changing the retained CM0
/// catalog schema. Provenance is data: a source cannot be credited with evidence merely because it
/// advertises the evidence subject's package.
/// </summary>
public static class Cm1FixtureLoader
{
    public static SourceEvidenceFixture LoadSourceEvidence(string json, CatalogFixture catalog)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(catalog);
        var failures = new List<string>();
        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            failures.Add($"source-evidence: invalid JSON: {exception.Message}");
        }

        if (document is null)
        {
            throw new FixtureFormatException(failures);
        }

        using (document)
        {
            var root = document.RootElement;
            CheckObject(root, "source-evidence", new[] { "schemaVersion", "fixture", "description", "availability" }, failures);
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new FixtureFormatException(failures);
            }

            if (!root.TryGetProperty("schemaVersion", out var schema)
                || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var schemaVersion)
                || schemaVersion != 1)
            {
                failures.Add("source-evidence: schemaVersion must be 1.");
            }

            if (!root.TryGetProperty("fixture", out var fixture)
                || fixture.ValueKind != JsonValueKind.String
                || fixture.GetString() != "cm1-source-evidence")
            {
                failures.Add("source-evidence: fixture must be 'cm1-source-evidence'.");
            }

            var description = ReadString(root, "description", "source-evidence", failures) ?? string.Empty;
            var availability = new List<SourceEvidenceAvailability>();
            if (!root.TryGetProperty("availability", out var entries) || entries.ValueKind != JsonValueKind.Array)
            {
                failures.Add("source-evidence: availability must be an array.");
            }
            else
            {
                var index = 0;
                foreach (var entry in entries.EnumerateArray())
                {
                    var context = $"source-evidence.availability[{index}]";
                    CheckObject(entry, context, new[] { "source", "evidence" }, failures);
                    var sourceText = ReadString(entry, "source", context, failures);
                    var evidenceText = ReadString(entry, "evidence", context, failures);
                    if (sourceText is not null && evidenceText is not null)
                    {
                        try
                        {
                            availability.Add(
                                new SourceEvidenceAvailability(
                                    SourceId.Create(sourceText),
                                    EvidenceId.Create(evidenceText)));
                        }
                        catch (ArgumentException exception)
                        {
                            failures.Add($"{context}: {exception.Message}");
                        }
                    }

                    index++;
                }
            }

            var duplicateKeys = availability
                .GroupBy(entry => $"{entry.Source.Value}|{entry.Evidence.Value}", StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(key => key, StringComparer.Ordinal);
            foreach (var duplicate in duplicateKeys)
            {
                failures.Add($"source-evidence: duplicate availability '{duplicate}'.");
            }

            var sources = catalog.Sources.Select(source => source.Source).ToHashSet();
            var evidenceById = catalog.Evidence.ToDictionary(evidence => evidence.Evidence);
            var packagesById = catalog.Packages.ToDictionary(package => package.Package);
            foreach (var entry in availability)
            {
                if (!sources.Contains(entry.Source))
                {
                    failures.Add($"source-evidence: unknown source '{entry.Source}'.");
                }

                if (!evidenceById.TryGetValue(entry.Evidence, out var evidence))
                {
                    failures.Add($"source-evidence: unknown evidence '{entry.Evidence}'.");
                    continue;
                }

                var sourceCarriesSubject = catalog.Advertisements.Any(advertisement =>
                    advertisement.Source == entry.Source
                    && packagesById.TryGetValue(advertisement.Package, out var package)
                    && package.Artifact == evidence.SubjectArtifact);
                if (!sourceCarriesSubject)
                {
                    failures.Add(
                        $"source-evidence: source '{entry.Source}' does not advertise a package carrying '{evidence.SubjectArtifact}' for '{entry.Evidence}'.");
                }
            }

            if (failures.Count > 0)
            {
                throw new FixtureFormatException(failures);
            }

            return new SourceEvidenceFixture(description, Array.AsReadOnly(availability.ToArray()));
        }
    }

    private static string? ReadString(
        JsonElement element,
        string property,
        string context,
        ICollection<string> failures)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            failures.Add($"{context}: '{property}' must be a string.");
            return null;
        }

        return value.GetString();
    }

    private static void CheckObject(
        JsonElement element,
        string context,
        IReadOnlyCollection<string> allowed,
        ICollection<string> failures)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            failures.Add($"{context}: expected an object.");
            return;
        }

        var names = element.EnumerateObject().Select(property => property.Name).ToArray();
        foreach (var unknown in names.Where(name => !allowed.Contains(name, StringComparer.Ordinal)))
        {
            failures.Add($"{context}: unknown property '{unknown}'.");
        }

        foreach (var missing in allowed.Where(name => !names.Contains(name, StringComparer.Ordinal)))
        {
            failures.Add($"{context}: missing property '{missing}'.");
        }
    }
}
