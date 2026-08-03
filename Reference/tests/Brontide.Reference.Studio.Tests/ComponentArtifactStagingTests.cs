using Brontide.Reference.Experimental.Binding.Portable;
using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi32Observation(
        string StageCode,
        bool Staged,
        bool Reused,
        string ActiveRemovalCode,
        bool Active,
        bool Released,
        bool Retired,
        bool ProviderExited,
        string RemovalCode,
        bool Residue);

    private static void Cbi32DeleteTree(string path)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception exception) when (
                attempt < 9 && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(25);
            }
        }
    }

    private static ProviderArtifactSet Cbi32Declaration(
        string provider,
        string sourceRoot,
        string mutation)
    {
        var providerPath = Cbi31ProviderPath(provider);
        var providerRoot = Path.GetDirectoryName(providerPath)!;
        Directory.CreateDirectory(sourceRoot);
        foreach (var source in Directory.EnumerateFiles(providerRoot).OrderBy(path => path, StringComparer.Ordinal))
        {
            File.Copy(source, Path.Combine(sourceRoot, Path.GetFileName(source)));
        }

        var files = Directory.EnumerateFiles(sourceRoot)
            .Select(path => new ProviderArtifactFile(Path.GetFileName(path), Cbi31Digest(path)))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToList();
        if (mutation == "missing-member")
        {
            files.Add(new ProviderArtifactFile("missing-member.dll", new string('0', 64)));
        }
        else if (mutation == "member-integrity")
        {
            files[0] = files[0] with { Sha256 = new string('0', 64) };
        }
        else if (mutation == "traversal")
        {
            files.Add(new ProviderArtifactFile("../escape.dll", new string('0', 64)));
        }

        var executable = Path.GetFileName(providerPath);
        var arguments = new[] { "--portable" };
        var identity = ProviderArtifactSetIdentity.Compute(files, executable, arguments);
        if (mutation == "identity")
        {
            identity = ProviderArtifactSetId.Create(new string('0', 64));
        }

        return new(identity, sourceRoot, files, executable, arguments);
    }

    private static async Task<JsonElement> Cbi32VectorAsync(string identity)
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi32-content-addressed-staging-vectors.json")));
        return fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("id").GetString() == identity)
            .Clone();
    }

    private static async Task<Cbi32Observation> Cbi32RunAsync(JsonElement vector)
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        var sourceRoot = Path.Combine(testRoot, "source");
        var storeRoot = Path.Combine(testRoot, "store");
        try
        {
            var declaration = Cbi32Declaration(
                vector.GetProperty("provider").GetString()!,
                sourceRoot,
                vector.GetProperty("mutation").GetString()!);
            var store = new ContentAddressedProviderStore(storeRoot);
            var staging = store.Stage(declaration);
            if (!staging.IsStaged)
            {
                var refusedRemoval = store.Remove(declaration.Identity);
                return new(
                    staging.Code,
                    false,
                    false,
                    "not-launched",
                    false,
                    false,
                    false,
                    true,
                    refusedRemoval.Code,
                    Directory.EnumerateFileSystemEntries(storeRoot).Any());
            }

            var restaged = store.Stage(declaration);
            Assert.That(restaged.IsStaged, Is.True);
            if (vector.GetProperty("removeSourceBeforeActivation").GetBoolean())
            {
                Cbi32DeleteTree(sourceRoot);
            }

            var activation = store.Activate(staging.Staged!, ["--portable"]);
            Assert.That(activation.IsLaunched, Is.True, activation.Failure?.Reason);
            await using var owner = activation.Owner!;
            var activeRemoval = store.Remove(declaration.Identity);
            var (resolution, selection, occurrence) = LifecycleInput();
            var result = await ComponentBindingLifecycle.ActivateAsync(
                resolution,
                selection,
                RuntimeRequest(Plan(occurrence)),
                owner.Conversation);
            var member = result.Member;
            var active = result.IsActive;
            var released = member?.IsReleased == true;
            var retired = false;
            if (active)
            {
                var retirement = await member!.RetireAsync("CBI32 staged activation completed.");
                retired = member.Stage == PortableCompositionStage.Retired && retirement.ReplacementPermitted;
            }

            if (member is not null)
            {
                await member.DisposeAsync();
            }

            var exited = await owner.WaitForExitAsync(TimeSpan.FromSeconds(5));
            await owner.DisposeAsync();
            var removal = store.Remove(declaration.Identity);
            return new(
                staging.Code,
                true,
                restaged.Staged!.Reused,
                activeRemoval.Code,
                active,
                released,
                retired,
                exited,
                removal.Code,
                Directory.EnumerateFileSystemEntries(storeRoot).Any());
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi32_vectors_stage_activate_and_remove_content_addressed_sets()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi32-content-addressed-staging-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var scenario = vector.GetProperty("id").GetString()!;
            var observation = await Cbi32RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(observation.StageCode, Is.EqualTo(vector.GetProperty("expectedStageCode").GetString()), scenario);
                Assert.That(observation.Staged, Is.EqualTo(vector.GetProperty("expectedStaged").GetBoolean()), scenario);
                Assert.That(observation.Active, Is.EqualTo(vector.GetProperty("expectedActivated").GetBoolean()), scenario);
                Assert.That(observation.RemovalCode, Is.EqualTo(vector.GetProperty("expectedRemovalCode").GetString()), scenario);
                Assert.That(observation.Residue, Is.False, scenario);
                Assert.That(observation.Active, Is.EqualTo(observation.Released), scenario);
                Assert.That(observation.Active, Is.EqualTo(observation.Retired), scenario);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi32_C1_manifest_is_canonical_and_complete()
    {
        using var fixture = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(
                    TestContext.CurrentContext.TestDirectory,
                    "component-management",
                    "fixtures",
                    "cbi32-content-addressed-staging-vectors.json")));
        var canonical = fixture.RootElement.GetProperty("canonicalManifest");
        var files = canonical.GetProperty("files").EnumerateArray()
            .Select(file => new ProviderArtifactFile(
                file.GetProperty("path").GetString()!,
                file.GetProperty("sha256").GetString()!));
        var computed = ProviderArtifactSetIdentity.Compute(
            files,
            canonical.GetProperty("executablePath").GetString()!,
            canonical.GetProperty("arguments").EnumerateArray().Select(value => value.GetString()!));
        var identity = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-05-identity-refused"));
        var traversal = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-06-traversal-refused"));
        Assert.Multiple(() =>
        {
            Assert.That(computed.Value, Is.EqualTo(canonical.GetProperty("expectedIdentity").GetString()));
            Assert.That(identity.StageCode, Is.EqualTo("artifact-set-invalid"));
            Assert.That(traversal.StageCode, Is.EqualTo("artifact-set-invalid"));
            Assert.That(identity.Staged || traversal.Staged, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi32_C2_staging_is_verified_and_transactional()
    {
        var missing = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-03-member-unavailable"));
        var changed = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-04-member-integrity-refused"));
        Assert.Multiple(() =>
        {
            Assert.That(missing.StageCode, Is.EqualTo("artifact-set-unavailable"));
            Assert.That(changed.StageCode, Is.EqualTo("artifact-set-integrity-failed"));
            Assert.That(missing.Residue || changed.Residue, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public void Cbi32_C3_content_identity_reuses_verified_state_and_detects_corruption()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        try
        {
            var declaration = Cbi32Declaration("reference", Path.Combine(testRoot, "source"), "none");
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var first = store.Stage(declaration);
            var second = store.Stage(declaration);
            Assert.That(second.Staged?.Reused, Is.True);
            var sourceBefore = declaration.Files.Select(file => (file.RelativePath, file.Sha256)).ToArray();
            var stagedPaths = Directory.EnumerateFiles(first.Staged!.RootPath, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(first.Staged.RootPath, path).Replace(Path.DirectorySeparatorChar, '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var sourceAfter = declaration.Files
                .Select(file => (file.RelativePath, Sha256: Cbi31Digest(Path.Combine(declaration.SourceRoot, file.RelativePath))))
                .ToArray();
            Assert.Multiple(() =>
            {
                Assert.That(stagedPaths, Is.EqualTo(declaration.Files.Select(file => file.RelativePath).OrderBy(path => path, StringComparer.Ordinal)));
                Assert.That(sourceAfter, Is.EqualTo(sourceBefore));
            });
            var stagedFile = Path.Combine(first.Staged!.RootPath, declaration.Files[0].RelativePath);
            File.SetAttributes(stagedFile, FileAttributes.Normal);
            File.WriteAllText(stagedFile, "corrupt");
            var corrupt = store.Stage(declaration);
            Assert.That(corrupt.Code, Is.EqualTo("staged-artifact-integrity-failed"));
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi32_C4_staging_is_inactive_and_composes_with_cbi31()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        try
        {
            var declaration = Cbi32Declaration("reference", Path.Combine(testRoot, "source"), "none");
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            Assert.That(store.Stage(declaration).IsStaged, Is.True);
            Assert.That(store.Remove(declaration.Identity).Code, Is.EqualTo("removed"));
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }

        var observation = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-01-reference-staged-activation"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.Staged, Is.True);
            Assert.That(observation.Active, Is.True);
            Assert.That(observation.Released, Is.True);
            Assert.That(observation.ProviderExited, Is.True);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi32_C5_removal_respects_active_leases_and_exact_ownership()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi32-{Guid.NewGuid():N}");
        try
        {
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var first = Cbi32Declaration("reference", Path.Combine(testRoot, "source-a"), "none");
            var secondBase = Cbi32Declaration("reference", Path.Combine(testRoot, "source-b"), "none");
            var second = secondBase with { Arguments = ["--portable", "--second"] };
            second = second with
            {
                Identity = ProviderArtifactSetIdentity.Compute(second.Files, second.ExecutablePath, second.Arguments),
            };
            Assert.That(store.Stage(first).IsStaged, Is.True);
            var stagedSecond = store.Stage(second);
            Assert.That(stagedSecond.IsStaged, Is.True);
            Assert.That(store.Remove(first.Identity).Code, Is.EqualTo("removed"));
            Assert.That(Directory.Exists(stagedSecond.Staged!.RootPath), Is.True);
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }

        var observation = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-02-minimal-staged-activation"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.ActiveRemovalCode, Is.EqualTo("artifact-set-in-use"));
            Assert.That(observation.RemovalCode, Is.EqualTo("removed"));
            Assert.That(observation.Residue, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi32_C6_both_roots_agree_on_portable_observations()
    {
        var reference = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-01-reference-staged-activation"));
        var minimal = await Cbi32RunAsync(await Cbi32VectorAsync("cbi32-02-minimal-staged-activation"));
        Assert.That(reference, Is.EqualTo(minimal));
    }
}
