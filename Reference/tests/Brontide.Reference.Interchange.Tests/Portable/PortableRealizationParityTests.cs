using System.Collections.Immutable;
using Brontide.Reference.Experimental.Binding.Portable;

namespace Brontide.Reference.Interchange.Tests.Portable;

/// <summary>
/// One request, stated once, so the two realizations execute the same vector rather than two
/// similar ones.
/// </summary>
/// <remarks>
/// The scenario is data, not a delegate. A delegate could quietly send a different request to each
/// realization and still report parity; a record cannot, because both runs read the same fields.
/// </remarks>
public sealed record PortableParityScenario(
    string Id,
    ImmutableArray<string> Vectors,
    ImmutableArray<string> ChannelVectors,
    PortableContractDocument Contract,
    Func<IPortableOperationHandler> Handler,
    ImmutableArray<string> ProviderArguments,
    PortableOperationReference Operation,
    PortableShapeReference InputShape,
    PortableValue Input,
    PortableConstraint Authority,
    ImmutableArray<PortableResource> Resources,
    PortableFrameDecision Frame,
    PortableResultClass Result,
    PortableProtocolCategory? Category)
{
    public override string ToString() => Id;
}

/// <summary>
/// The PB4 parity matrix: every scenario runs in the fixed direct-call realization and in the
/// negotiated process realization, and the two must report the same category-level profile.
/// </summary>
/// <remarks>
/// PB2 measured parity for a success, a denial, and a resource. That left the interesting half
/// unmeasured: the refusals, whose decision point genuinely moves between the realizations. The
/// matrix below covers each portable result class the host can reach — success, shaped failure,
/// denial, and protocol rejection — because a parity claim that skips rejections claims very little.
/// </remarks>
internal static class PortableParityMatrix
{
    private static readonly PortableConstraint Denied = PortableConstraint.AllOf(
        PortableConstraint.Atom(PortableTruth.Satisfied),
        PortableConstraint.Atom(PortableTruth.Unsatisfied));

    private static readonly PortableConstraint Unknown = PortableConstraint.AllOf(
        PortableConstraint.Atom(PortableTruth.Satisfied),
        PortableConstraint.Atom(PortableTruth.Unknown));

    private static readonly PortableConstraint AnyOfPermits = PortableConstraint.AnyOf(
        PortableConstraint.Atom(PortableTruth.Satisfied),
        PortableConstraint.Atom(PortableTruth.Unknown));

    public static ImmutableArray<PortableParityScenario> Scenarios { get; } =
    [
        Cooling(
            "success",
            ["PB-58-DIRECT-AND-PROCESS-PARITY-ON-SUCCESS"],
            ["CH-01-CORRELATION-ECHO"],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeSucceeded),

        Cooling(
            "additive-projection",
            ["PB-11-ADDITIVE-PROJECTION", "PB-57-OBSERVATION-RECORDS-MAPPING-OBLIGATIONS"],
            ["CH-07-PAYLOAD-COVARIANCE"],
            CoolingPortableFixture.Command("primary", enabled: true, requestedBy: "operator"),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeSucceeded,
            inputShape: CoolingPortableFixture.CommandV2),

        Cooling(
            "strong-kleene-anyof-permits",
            ["PB-20-STRONG-KLEENE-ANYOF-PERMITS"],
            ["CH-09-STRONG-KLEENE-FALLBACK"],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeSucceeded,
            authority: AnyOfPermits),

        Cooling(
            "strong-kleene-unknown-denies",
            ["PB-19-AUTHORITY-VALUE-NOT-PROJECTED", "PB-21-STRONG-KLEENE-ALLOF-DENIES"],
            ["CH-08-AUTHORITY-NO-PROJECTION", "CH-10-STRONG-KLEENE-UNKNOWN-DENIES"],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.None,
            PortableResultClass.Denial,
            authority: Unknown),

        Cooling(
            "local-denial",
            ["PB-18-LOCAL-DENIAL-EMITS-NO-FRAME", "PB-56-OBSERVATION-COMPLETE-ON-DENIAL", "PB-59-DIRECT-AND-PROCESS-PARITY-ON-DENIAL"],
            ["CH-12-DENIAL-IS-NOT-A-FRAME"],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.None,
            PortableResultClass.Denial,
            authority: Denied),

        Cooling(
            "shaped-failed-outcome",
            ["PB-47-SEMANTIC-FAILED-OUTCOME"],
            ["CH-13-SEMANTIC-FAILED-OUTCOME"],
            CoolingPortableFixture.Command("primary", enabled: true, failureMode: "requested-failure"),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeFailed),

        Cooling(
            "unsupported-operation",
            ["PB-46-UNSUPPORTED-OPERATION"],
            ["CH-19-UNSUPPORTED-OPERATION"],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            category: PortableProtocolCategory.UnsupportedOperation,
            operation: PortableOperationReference.Parse("interchange.tests.cooling.set-disabled", 1)),

        Cooling(
            "missing-required-fragment",
            ["PB-24-MISSING-REQUIRED-FRAGMENT"],
            ["CH-20-INVALID-PAYLOAD"],
            CoolingPortableFixture.Command("primary", enabled: true, requesterLabel: null),
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            category: PortableProtocolCategory.InvalidPayload),

        Cooling(
            "capability-in-body-across-trust",
            ["PB-22-CAPABILITY-IN-BODY-ACROSS-TRUST"],
            ["CH-11-NO-CAPABILITY-TRANSFER"],
            CoolingPortableFixture.Command("primary", enabled: true)
                .WithField("capability", new PortableTextValue("cooling.write")),
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            category: PortableProtocolCategory.InvalidAuthorityPresentation),

        Cooling(
            "copied-immutable-blob",
            ["PB-25-COPIED-IMMUTABLE-BLOB-ACCEPTED", "PB-60-COPY-ACCOUNTING-DIFFERS-WITHOUT-BREAKING-PARITY"],
            [],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeSucceeded,
            resources: [PortableTestHarness.Blob()]),

        Cooling(
            "resource-integrity-mismatch",
            ["PB-26-RESOURCE-INTEGRITY-MISMATCH"],
            [],
            CoolingPortableFixture.Command("primary", enabled: true),
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            category: PortableProtocolCategory.InvalidPayload,
            resources: [Tampered()]),

        Catalog(
            "addressing-only-handle",
            ["PB-27-ADDRESSING-HANDLE-ACCEPTED"],
            CatalogPortableFixture.Handle(),
            PortableFrameDecision.Accept,
            PortableResultClass.OutcomeSucceeded),

        Catalog(
            "resource-scope-refused",
            ["PB-28-RESOURCE-SCOPE-REFUSED"],
            CatalogPortableFixture.Handle("catalog-provider", "secondary"),
            PortableFrameDecision.Reject,
            PortableResultClass.ProtocolError,
            category: PortableProtocolCategory.InvalidPayload)
    ];

    /// <summary>A blob whose declared content hash does not describe its content.</summary>
    private static PortableCopiedBlobResource Tampered() =>
        new(
            "profile",
            System.Text.Encoding.UTF8.GetBytes("tampered"),
            PortableCopiedBlobResource.HashOf(System.Text.Encoding.UTF8.GetBytes("original")));

    private static PortableParityScenario Cooling(
        string id,
        ImmutableArray<string> vectors,
        ImmutableArray<string> channelVectors,
        PortableValue input,
        PortableFrameDecision frame,
        PortableResultClass result,
        PortableProtocolCategory? category = null,
        PortableOperationReference? operation = null,
        PortableShapeReference? inputShape = null,
        PortableConstraint? authority = null,
        ImmutableArray<PortableResource> resources = default) =>
        new(
            id,
            vectors,
            channelVectors,
            CoolingPortableFixture.Contract,
            () => new CoolingPortableHandler(CoolingPortableFixture.CreateNativeRegistry()),
            ["--portable"],
            operation ?? CoolingPortableFixture.SetEnabled,
            inputShape ?? CoolingPortableFixture.CommandV1,
            input,
            authority ?? PortableTestHarness.Permitted(),
            resources.IsDefault ? [] : resources,
            frame,
            result,
            category);

    private static PortableParityScenario Catalog(
        string id,
        ImmutableArray<string> vectors,
        PortableResource resource,
        PortableFrameDecision frame,
        PortableResultClass result,
        PortableProtocolCategory? category = null) =>
        new(
            id,
            vectors,
            [],
            CatalogPortableFixture.Contract,
            () => new CatalogPortableHandler(),
            ["--portable", "--catalog"],
            CatalogPortableFixture.Upsert,
            CatalogPortableFixture.UpsertCommand,
            CatalogPortableFixture.UpsertCommandValue(CatalogPortableFixture.ItemValue("a", "Alpha", "one")),
            PortableTestHarness.Permitted(),
            [resource],
            frame,
            result,
            category);
}

/// <summary>
/// PB4: the fixed direct-call realization and the negotiated process realization report the same
/// category-level portable observations for every vector in the matrix.
/// </summary>
/// <remarks>
/// Only the portable observation set is normalized. Representation, framing, crossed boundaries,
/// copy accounting, correlation, timing, and the endpoint's own diagnostic code are excluded by the
/// neutral parity profile, and this suite asserts that they are excluded for the stated reasons
/// rather than because nothing ever differs.
/// </remarks>
public sealed class PortableRealizationParityTests
{
    public static IEnumerable<PortableParityScenario> Scenarios => PortableParityMatrix.Scenarios;

    [TestCaseSource(nameof(Scenarios))]
    public async Task Both_realizations_report_the_same_parity_profile(PortableParityScenario scenario)
    {
        var direct = await RunAsync(scenario, direct: true);
        var process = await RunAsync(scenario, direct: false);

        Assert.Multiple(() =>
        {
            Assert.That(direct.FrameDecision, Is.EqualTo(scenario.Frame), "The direct realization decided a different frame.");
            Assert.That(process.FrameDecision, Is.EqualTo(scenario.Frame), "The process realization decided a different frame.");
            Assert.That(direct.ResultClass, Is.EqualTo(scenario.Result));
            Assert.That(process.ResultClass, Is.EqualTo(scenario.Result));
            Assert.That(direct.Category, Is.EqualTo(scenario.Category));
            Assert.That(process.Category, Is.EqualTo(scenario.Category));
            Assert.That(
                process.ParityProfile(),
                Is.EqualTo(direct.ParityProfile()),
                Difference(direct, process));
        });
    }

    /// <summary>
    /// A rejection reports zero provider effects wherever it was decided, and the counted effect of
    /// a success is the same number in both realizations.
    /// </summary>
    [TestCaseSource(nameof(Scenarios))]
    public async Task Provider_effects_are_counted_the_same_way_in_both_realizations(PortableParityScenario scenario)
    {
        var direct = await RunAsync(scenario, direct: true);
        var process = await RunAsync(scenario, direct: false);
        var expected = scenario.Result == PortableResultClass.OutcomeSucceeded ? 1 : 0;

        Assert.Multiple(() =>
        {
            Assert.That(direct.Observation.ProviderEffectCount, Is.EqualTo(expected));
            Assert.That(process.Observation.ProviderEffectCount, Is.EqualTo(expected));
        });
    }

    /// <summary>
    /// The excluded fields are excluded because they genuinely differ, not because they happen to
    /// agree. A copied blob is one copy across the seam and none in a direct call.
    /// </summary>
    [Test]
    public async Task The_excluded_fields_differ_exactly_as_their_stated_reasons_permit()
    {
        var scenario = Scenario("copied-immutable-blob");
        var direct = await RunAsync(scenario, direct: true);
        var process = await RunAsync(scenario, direct: false);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Observation.CopyCount, Is.Zero);
            Assert.That(process.Observation.CopyCount, Is.EqualTo(1));
            Assert.That(direct.Observation.CrossedBoundaries, Does.Not.Contain("process"));
            Assert.That(process.Observation.CrossedBoundaries, Does.Contain("process"));
            Assert.That(
                process.Observation.CorrelationMapping.RequestId,
                Is.Not.EqualTo(direct.Observation.CorrelationMapping.RequestId),
                "Correlation identities are per-run, which is why parity excludes them.");
            Assert.That(process.ParityProfile(), Is.EqualTo(direct.ParityProfile()));
        });
    }

    /// <summary>
    /// A refusal's local diagnostic code stays non-normative: it may differ between realizations
    /// while the portable category does not.
    /// </summary>
    [Test]
    public async Task A_refusal_carries_the_same_portable_category_whatever_its_local_code_says()
    {
        var scenario = Scenario("resource-scope-refused");
        var direct = await RunAsync(scenario, direct: true);
        var process = await RunAsync(scenario, direct: false);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(process.Category, Is.EqualTo(PortableProtocolCategory.InvalidPayload));
            Assert.That(direct.Observation.LocalCode, Is.Not.Null);
            Assert.That(process.Observation.LocalCode, Is.Not.Null);
        });
    }

    /// <summary>
    /// The matrix is measured against the neutral layer rather than against itself: a scenario that
    /// names a vector the neutral artifacts no longer declare is a stale claim.
    /// </summary>
    [Test]
    public void Every_scenario_names_vectors_the_neutral_layer_declares()
    {
        var declared = PortableTestHarness.NeutralVectorIds();
        var channel = PortableTestHarness.ChannelVectorIds();

        Assert.Multiple(() =>
        {
            foreach (var scenario in PortableParityMatrix.Scenarios)
            {
                Assert.That(scenario.Vectors, Is.Not.Empty, $"Scenario '{scenario.Id}' names no vector.");
                Assert.That(
                    scenario.Vectors.Where(vector => !declared.Contains(vector)),
                    Is.Empty,
                    $"Scenario '{scenario.Id}' names an undeclared portable vector.");
                Assert.That(
                    scenario.ChannelVectors.Where(vector => !channel.Contains(vector)),
                    Is.Empty,
                    $"Scenario '{scenario.Id}' names an undeclared Channel vector.");
            }

            Assert.That(
                PortableParityMatrix.Scenarios.Select(scenario => scenario.Id),
                Is.Unique);
        });
    }

    /// <summary>
    /// The portable process realization is length-delimited and bounded, and the retained
    /// line-delimited JSON protocol cannot pass for it.
    /// </summary>
    /// <remarks>
    /// The two experiments still share a repository, so "the portable wire" has to be something the
    /// portable reader can tell apart from the legacy one rather than a claim in a document. A JSON
    /// line's first four bytes are read as a length prefix, and every such prefix is far beyond the
    /// declared bound, so the legacy protocol is refused on the prefix alone.
    /// </remarks>
    [Test]
    public async Task A_line_delimited_JSON_message_is_not_a_portable_frame()
    {
        await using var host = await PortableTestHarness.DirectHostAsync();
        var line = System.Text.Encoding.UTF8.GetBytes(
            "{\"kind\":\"invoke\",\"requestId\":\"r1\",\"operation\":\"interchange.tests.cooling.set-enabled\"}\n");
        using var stream = new MemoryStream(line);

        var fault = Assert.ThrowsAsync<PortableFaultException>(async () =>
            await PortableFraming.ReadFrameAsync(stream, PortableLimits.Declared, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(fault!.Category, Is.EqualTo(PortableProtocolCategory.LimitExceeded));
            Assert.That(host.Plan.Fact("framing"), Is.EqualTo("direct-call"));
            Assert.That(PortableLimits.Declared.MaxFrameBytes, Is.EqualTo(65536), "The portable frame bound is finite.");
        });
    }

    /// <summary>Every portable result class the host can reach is measured for parity.</summary>
    [Test]
    public void The_matrix_covers_every_result_class_a_host_can_reach()
    {
        var covered = PortableParityMatrix.Scenarios
            .Select(scenario => scenario.Result)
            .ToImmutableHashSet();

        Assert.That(
            covered,
            Is.EquivalentTo(new[]
            {
                PortableResultClass.OutcomeSucceeded,
                PortableResultClass.OutcomeFailed,
                PortableResultClass.Denial,
                PortableResultClass.ProtocolError
            }));
    }

    internal static PortableParityScenario Scenario(string id) =>
        PortableParityMatrix.Scenarios.Single(scenario => scenario.Id == id);

    internal static async Task<PortableInteractionResult> RunAsync(PortableParityScenario scenario, bool direct)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (direct)
        {
            await using var host = await PortableTestHarness.DirectHostAsync(scenario.Contract, scenario.Handler());
            return await InvokeAsync(host, scenario);
        }

        var (processHost, seam) = await PortableTestHarness.ProcessHostAsync(scenario.Contract, scenario.Handler());
        await using (seam)
        await using (processHost)
        {
            return await InvokeAsync(processHost, scenario);
        }
    }

    private static ValueTask<PortableInteractionResult> InvokeAsync(
        PortableBindingHost host,
        PortableParityScenario scenario) =>
        host.InvokeAsync(
            scenario.Operation,
            scenario.InputShape,
            scenario.Input,
            scenario.Authority,
            scenario.Resources);

    /// <summary>Names the fields that differ, so a parity failure reports what diverged.</summary>
    private static string Difference(PortableInteractionResult direct, PortableInteractionResult process)
    {
        var left = direct.ParityProfile();
        var right = process.ParityProfile();
        var differing = left
            .Where(entry => !right.TryGetValue(entry.Key, out var other) || other != entry.Value)
            .Select(entry => $"{entry.Key}: direct='{entry.Value}' process='{right.GetValueOrDefault(entry.Key, "<absent>")}'");
        return "The realizations disagree on " + string.Join("; ", differing);
    }
}
