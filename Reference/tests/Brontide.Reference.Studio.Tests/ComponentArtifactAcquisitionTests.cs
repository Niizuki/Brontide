using System.Text.Json;

namespace Brontide.Reference.Studio.Tests;

public sealed partial class ComponentBindingIntegrationTests
{
    private sealed record Cbi33Observation(
        string SourceIdentity,
        string TransportCode,
        string PublisherEvidenceCode,
        string AdmissionCode,
        bool Staged,
        bool Reused,
        bool Activated,
        string RemovalCode,
        bool Residue,
        int OpenCount,
        bool Bounded);

    private sealed class CountingReadStream(Stream inner, Action<long> count) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int readCount)
        {
            var read = inner.Read(buffer, offset, readCount);
            count(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = inner.Read(buffer);
            count(read);
            return read;
        }

        public override int ReadByte()
        {
            var value = inner.ReadByte();
            if (value >= 0)
            {
                count(1);
            }

            return value;
        }

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int writeCount) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class MemoryArtifactSource(
        ProviderArtifactSourceId identity,
        IReadOnlyDictionary<string, Func<Stream?>> members) : IProviderArtifactSource
    {
        public ProviderArtifactSourceId Identity { get; } = identity;

        public int OpenCount { get; private set; }

        public long BytesRead { get; private set; }

        public Stream? OpenRead(string relativePath)
        {
            OpenCount++;
            var stream = members.TryGetValue(relativePath, out var member) ? member() : null;
            return stream is null ? null : new CountingReadStream(stream, count => BytesRead += count);
        }
    }

    private sealed class FailingReadStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("fixture read failure");

        public override int Read(Span<byte> buffer) => throw new IOException("fixture read failure");
    }

    private static (ProviderArtifactAcquisitionRequest Request, MemoryArtifactSource Source) Cbi33Input(
        string provider,
        string mutation,
        string? extraRelativePath = null,
        byte[]? extraContent = null)
    {
        var providerPath = Cbi31ProviderPath(provider);
        var providerRoot = Path.GetDirectoryName(providerPath)!;
        var bytes = Directory.EnumerateFiles(providerRoot)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes, StringComparer.Ordinal);
        if (extraRelativePath is not null)
        {
            ArgumentNullException.ThrowIfNull(extraContent);
            bytes.Add(extraRelativePath, extraContent);
        }
        var files = bytes.Select(pair => new ProviderArtifactAcquisitionFile(
                pair.Key,
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pair.Value)),
                pair.Value.LongLength))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();
        if (mutation == "digest")
        {
            files[0] = files[0] with { Sha256 = new string('0', 64) };
        }

        var sourceIdentity = ProviderArtifactSourceId.Create("fixture://brontide/provider-output");
        var actualIdentity = mutation == "source-mismatch"
            ? ProviderArtifactSourceId.Create("fixture://brontide/other-output")
            : sourceIdentity;
        var first = files[0].RelativePath;
        var members = bytes.ToDictionary(
            pair => pair.Key,
            pair => (Func<Stream?>)(() =>
            {
                if (mutation == "missing" && pair.Key == first)
                {
                    return null;
                }

                if (mutation == "read-failure" && pair.Key == first)
                {
                    return new FailingReadStream(pair.Value);
                }

                var content = pair.Value;
                if (mutation == "short" && pair.Key == first)
                {
                    content = content[..^1];
                }
                else if (mutation == "long" && pair.Key == first)
                {
                    content = [.. content, 0x7F];
                }

                return new MemoryStream(content, writable: false);
            }),
            StringComparer.Ordinal);
        var total = files.Sum(file => file.Length);
        var limit = mutation == "budget" ? total - 1 : total;
        var artifactFiles = files.Select(file => new ProviderArtifactFile(file.RelativePath, file.Sha256));
        var request = new ProviderArtifactAcquisitionRequest(
            sourceIdentity,
            ProviderArtifactSetIdentity.Compute(artifactFiles, Path.GetFileName(providerPath), ["--portable"]),
            files,
            Path.GetFileName(providerPath),
            ["--portable"],
            limit);
        return (request, new MemoryArtifactSource(actualIdentity, members));
    }

    private static async Task<JsonElement> Cbi33VectorAsync(string id)
    {
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "component-management",
            "fixtures",
            "cbi33-attributable-acquisition-vectors.json")));
        return fixture.RootElement.GetProperty("vectors").EnumerateArray()
            .Single(vector => vector.GetProperty("id").GetString() == id)
            .Clone();
    }

    private static async Task<Cbi33Observation> Cbi33RunAsync(JsonElement vector)
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"brontide-cbi33-{Guid.NewGuid():N}");
        try
        {
            var (request, source) = Cbi33Input(
                vector.GetProperty("provider").GetString()!,
                vector.GetProperty("mutation").GetString()!);
            var store = new ContentAddressedProviderStore(Path.Combine(testRoot, "store"));
            var acquirer = new ProviderArtifactAcquirer(store, Path.Combine(testRoot, "transactions"));
            var result = acquirer.Acquire(request, source);
            var reused = false;
            var activated = false;
            var removalCode = "artifact-set-not-staged";
            if (result.IsStaged)
            {
                reused = acquirer.Acquire(request, source).Staged!.Reused;
                var activation = store.Activate(result.Staged!, ["--portable"]);
                activated = activation.IsLaunched;
                if (activation.Owner is not null)
                {
                    await activation.Owner.DisposeAsync();
                }

                removalCode = store.Remove(request.Identity).Code;
            }

            var transactionRoot = Path.Combine(testRoot, "transactions");
            return new(
                result.SourceIdentity.Value,
                result.TransportCode,
                result.PublisherEvidenceCode,
                result.AdmissionCode,
                result.IsStaged,
                reused,
                activated,
                removalCode,
                Directory.Exists(transactionRoot) && Directory.EnumerateFileSystemEntries(transactionRoot).Any(),
                source.OpenCount,
                source.BytesRead <= (result.IsStaged ? 2 : 1) * (request.MaxTotalBytes + request.Files.Count));
        }
        finally
        {
            Cbi32DeleteTree(testRoot);
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Shared_cbi33_vectors_acquire_attributable_bounded_artifacts()
    {
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "component-management", "fixtures",
            "cbi33-attributable-acquisition-vectors.json")));
        foreach (var vector in fixture.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var observation = await Cbi33RunAsync(vector);
            Assert.Multiple(() =>
            {
                Assert.That(observation.TransportCode, Is.EqualTo(vector.GetProperty("transportCode").GetString()));
                Assert.That(observation.AdmissionCode, Is.EqualTo(vector.GetProperty("admissionCode").GetString()));
                Assert.That(observation.Staged, Is.EqualTo(vector.GetProperty("staged").GetBoolean()));
                Assert.That(observation.PublisherEvidenceCode, Is.EqualTo("publisher-evidence-not-evaluated"));
                Assert.That(observation.Residue, Is.False);
                Assert.That(observation.Bounded, Is.True);
            });
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi33_C1_declaration_is_complete_and_bounded()
    {
        var observation = await Cbi33RunAsync(await Cbi33VectorAsync("cbi33-09-invalid-budget"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.TransportCode, Is.EqualTo("acquisition-invalid"));
            Assert.That(observation.OpenCount, Is.Zero);
            Assert.That(observation.Residue, Is.False);
            Assert.That(observation.Bounded, Is.True);
        });
    }

    [TestCase("cbi33-03-source-mismatch", "acquisition-source-mismatch", 0)]
    [TestCase("cbi33-04-member-unavailable", "acquisition-member-unavailable", 1)]
    [TestCase("cbi33-05-short-stream", "acquisition-length-mismatch", 1)]
    [TestCase("cbi33-06-overlong-stream", "acquisition-length-mismatch", 1)]
    [TestCase("cbi33-07-transport-failure", "acquisition-transport-failed", 1)]
    [Category("CrossProcess")]
    public async Task Cbi33_C2_acquisition_admits_exact_bounded_streams(
        string id,
        string transportCode,
        int openCount)
    {
        var observation = await Cbi33RunAsync(await Cbi33VectorAsync(id));
        Assert.Multiple(() =>
        {
            Assert.That(observation.Staged, Is.False);
            Assert.That(observation.TransportCode, Is.EqualTo(transportCode));
            Assert.That(observation.OpenCount, Is.EqualTo(openCount));
            Assert.That(observation.Residue, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi33_C3_source_attribution_is_not_publisher_evidence()
    {
        foreach (var id in new[] { "cbi33-01-reference-success", "cbi33-08-integrity-refused" })
        {
            var observation = await Cbi33RunAsync(await Cbi33VectorAsync(id));
            Assert.That(observation.SourceIdentity, Is.EqualTo("fixture://brontide/provider-output"));
            Assert.That(observation.PublisherEvidenceCode, Is.EqualTo("publisher-evidence-not-evaluated"));
        }
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi33_C4_transport_completion_is_not_local_admission()
    {
        var observation = await Cbi33RunAsync(await Cbi33VectorAsync("cbi33-08-integrity-refused"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.TransportCode, Is.EqualTo("transport-completed"));
            Assert.That(observation.AdmissionCode, Is.EqualTo("artifact-set-integrity-failed"));
            Assert.That(observation.Staged, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi33_C5_admitted_content_composes_with_cbi32_lifecycle()
    {
        var observation = await Cbi33RunAsync(await Cbi33VectorAsync("cbi33-01-reference-success"));
        Assert.Multiple(() =>
        {
            Assert.That(observation.Staged, Is.True);
            Assert.That(observation.Reused, Is.True);
            Assert.That(observation.Activated, Is.True);
            Assert.That(observation.RemovalCode, Is.EqualTo("removed"));
            Assert.That(observation.Residue, Is.False);
        });
    }

    [Test]
    [Category("CrossProcess")]
    public async Task Cbi33_C6_both_roots_agree_on_portable_observations()
    {
        var reference = await Cbi33RunAsync(await Cbi33VectorAsync("cbi33-01-reference-success"));
        var minimal = await Cbi33RunAsync(await Cbi33VectorAsync("cbi33-02-minimal-success"));
        Assert.That(reference.Staged, Is.True);
        Assert.That(minimal.Staged, Is.True);
        Assert.That(reference with { SourceIdentity = "same", OpenCount = 0 },
            Is.EqualTo(minimal with { SourceIdentity = "same", OpenCount = 0 }));
    }
}
