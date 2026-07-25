using Brontide.Reference.Core;
using Brontide.Reference.Experimental.Enrichment;

namespace Brontide.Reference.Enrichment.Tests;

[Category("Experimental")]
public sealed class TargetedEnrichmentTests
{
    [Test]
    public async Task Availability_is_composition_local_and_missing_sources_fail_before_execution()
    {
        var system = new PointerTemperatureSystem();
        var observer = Substitute.For<IEnrichmentObserver>();
        var provider = system.CopyProvider();
        var withProvider = new TargetedEnrichmentComposition(
            "with telemetry",
            system.Domain.Shapes,
            [provider],
            observer: observer);
        var withoutProvider = new TargetedEnrichmentComposition(
            "without telemetry",
            system.Domain.Shapes,
            []);
        var telemetry = system.Telemetry(31);

        var accepted = await withProvider.ExecuteAsync(
            system.Domain,
            system.PointerActor,
            system.PointerMove,
            system.PointerCapability,
            system.PointerInput(),
            system.ThermalContext,
            [AvailableValue.Direct("telemetry", telemetry, "composition-local telemetry snapshot")]);
        var executionCount = system.ExecutionCount;

        Assert.That(accepted.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(system.LastTemperature, Is.EqualTo(31));
        Assert.That(observer.ReceivedCalls().Count(), Is.EqualTo(1));
        Assert.That(() => withoutProvider.Resolve(
                system.PointerMove,
                system.ThermalContext,
                system.PointerInput(),
                [AvailableValue.Direct("telemetry", telemetry, "available elsewhere")]),
            Throws.TypeOf<EnrichmentResolutionException>());
        Assert.That(system.ExecutionCount, Is.EqualTo(executionCount),
            "pre-Execution Enrichment failure must not enter the Core Execution log");
        Assert.That(() => withProvider.Resolve(
                system.PointerMove,
                system.ThermalContext,
                system.PointerInput(),
                []),
            Throws.TypeOf<EnrichmentResolutionException>()
                .With.Message.Contains("missing source"));
    }

    [Test]
    public void Competing_providers_are_rejected_when_the_composition_is_activated()
    {
        var system = new PointerTemperatureSystem();
        var first = system.CopyProvider("Experiment:First");
        var second = system.CopyProvider("Experiment:Second");

        Assert.That(() => new TargetedEnrichmentComposition(
                "ambiguous",
                system.Domain.Shapes,
                [first, second]),
            Throws.TypeOf<EnrichmentConfigurationException>()
                .With.Message.Contains("Competing providers"));
    }

    [Test]
    public void Copy_projection_and_pure_derivation_are_deterministic_and_route_free()
    {
        var system = new PointerTemperatureSystem();
        var projection = EnrichmentProvider.Project(
            CanonicalName.Parse("Experiment:Projection"),
            system.PointerMove,
            system.ThermalContext,
            new EnrichmentSourceRequirement("telemetry", ShapeContract.For(system.TelemetryShape)),
            "temperature",
            source => source.RequireField("temperature"));
        var derived = EnrichmentProvider.DeriveDeterministically(
            CanonicalName.Parse("Experiment:Derivation"),
            system.PointerMove,
            system.ThermalContext,
            [new EnrichmentSourceRequirement("fahrenheit", ShapeContract.For(system.TemperatureShape))],
            inputs =>
            {
                var fahrenheit = inputs.Require("fahrenheit").RequireScalar<long>();
                return new Dictionary<string, ShapeValue>
                {
                    ["temperature"] = ShapeValue.Scalar(system.TemperatureShape, (fahrenheit - 32) * 5 / 9)
                };
            });
        var projectionComposition = new TargetedEnrichmentComposition(
            "projection", system.Domain.Shapes, [projection]);
        var derivationComposition = new TargetedEnrichmentComposition(
            "derivation", system.Domain.Shapes, [derived],
            EnrichmentRealizationStrategy.ParameterThreading);

        var projected = projectionComposition.Resolve(
            system.PointerMove,
            system.ThermalContext,
            system.PointerInput(),
            [AvailableValue.Direct("telemetry", system.Telemetry(20), "snapshot")]);
        var first = derivationComposition.Resolve(
            system.PointerMove,
            system.ThermalContext,
            system.PointerInput(),
            [AvailableValue.Direct("fahrenheit", ShapeValue.Scalar(system.TemperatureShape, 68L), "argument")]);
        var second = derivationComposition.Resolve(
            system.PointerMove,
            system.ThermalContext,
            system.PointerInput(),
            [AvailableValue.Direct("fahrenheit", ShapeValue.Scalar(system.TemperatureShape, 68L), "argument")]);

        Assert.That(projected.Input.Fragments[system.ThermalContext]["temperature"].RequireScalar<long>(),
            Is.EqualTo(20));
        Assert.That(first.Input.Fragments[system.ThermalContext]["temperature"].RequireScalar<long>(),
            Is.EqualTo(20));
        Assert.That(second.Input.Fragments[system.ThermalContext]["temperature"].RequireScalar<long>(),
            Is.EqualTo(20));
        Assert.That(first.Trace.Strategy, Is.EqualTo(EnrichmentRealizationStrategy.ParameterThreading));
        Assert.That(typeof(EnrichmentProvider).GetProperties().Any(property =>
            property.Name.Contains("Route", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
    public async Task Explicit_store_read_result_can_supply_enrichment_but_resolution_performs_no_read()
    {
        var system = new PointerTemperatureSystem();
        var composition = new TargetedEnrichmentComposition(
            "store snapshot",
            system.Domain.Shapes,
            [system.CopyProvider()]);

        var storeResult = await system.ReadStoreAsync();
        var readsBeforeResolution = system.StoreReads;
        var resolved = composition.Resolve(
            system.PointerMove,
            system.ThermalContext,
            system.PointerInput(),
            [AvailableValue.FromExecution("telemetry", storeResult)]);

        Assert.That(storeResult.Outcome.Status, Is.EqualTo(OutcomeStatus.Succeeded));
        Assert.That(system.StoreReads, Is.EqualTo(readsBeforeResolution));
        Assert.That(resolved.Trace.Sources["telemetry"], Does.StartWith("result of"));
        Assert.That(system.Domain.Provenance.Any(entry =>
            entry.Execution?.Operation == system.TelemetryRead), Is.True);
    }

    [Test]
    public async Task Shaped_capability_information_never_becomes_authority()
    {
        var system = new PointerTemperatureSystem();
        var composition = new TargetedEnrichmentComposition(
            "authority separation",
            system.Domain.Shapes,
            [system.CopyProvider()]);

        Assert.That(
            () => AvailableValue.Direct(
                "authority",
                ShapeValue.Scalar(BuiltInShapes.Text, system.PointerCapability),
                "attempted Capability transfer"),
            Throws.ArgumentException,
            "a Capability must not be representable as shaped scalar information");

        var result = composition.Resolve(
            system.PointerMove,
            system.ThermalContext,
            system.PointerInput(),
            [AvailableValue.Direct("telemetry", system.Telemetry(15),
                $"opaque note mentioning {system.PointerCapability.Id:N}")]);

        var denied = await system.Domain.ExecuteAsync(
            system.Attacker,
            system.PointerMove,
            system.PointerCapability,
            result.Input);

        Assert.That(denied.Outcome.Status, Is.EqualTo(OutcomeStatus.Rejected));
        Assert.That(typeof(AvailableValue).GetProperties().Any(property =>
            property.PropertyType == typeof(Capability)), Is.False);
        Assert.That(typeof(EnrichmentResolution).GetProperties().Any(property =>
            property.PropertyType == typeof(Capability)), Is.False);
    }

    private sealed class PointerTemperatureSystem
    {
        private int _moves;

        public PointerTemperatureSystem()
        {
            PointerMove = OperationReference.Parse("Input.Pointer.Move");
            TelemetryRead = OperationReference.Parse("DeviceTelemetry.Read");
            PointerShape = ShapeReference.Parse("Input.Pointer.Motion", 1);
            TemperatureShape = ShapeReference.Parse("Sensor.Temperature", 1);
            TelemetryShape = ShapeReference.Parse("DeviceTelemetry", 1);
            ThermalContext = FragmentReference.Parse("Experiment:ThermalContext", 1);

            ActorReference pointerTarget = null!;
            ActorReference store = null!;
            Domain = AuthorityDomain.Create("pointer-temperature", genesis =>
            {
                PointerActor = genesis.Actor("PointerActor");
                Attacker = genesis.Actor("Attacker");
                pointerTarget = genesis.Actor("PointerTarget");
                StoreActor = genesis.Actor("TelemetryConsumer");
                store = genesis.Actor("TelemetryStore");
                genesis.Shape(ShapeDefinition.Scalar<long>(TemperatureShape));
                genesis.Shape(ShapeDefinition.Record(TelemetryShape, FragmentPolicy.Closed,
                    RecordField.Required("temperature", TemperatureShape)));
                genesis.Shape(ShapeDefinition.Record(PointerShape, FragmentPolicy.Open,
                    RecordField.Required("x", BuiltInShapes.Signed64),
                    RecordField.Required("y", BuiltInShapes.Signed64)));
                genesis.Shape(DeclaredFragmentDefinition.Attached(
                    ThermalContext,
                    PointerShape,
                    RecordField.Required("temperature", TemperatureShape)));
                genesis.Operation(
                    PointerMove,
                    pointerTarget,
                    ShapeContract.For(PointerShape, ThermalContext),
                    ShapeContract.Unit,
                    "move pointer with declared thermal context",
                    context =>
                    {
                        LastTemperature = context.Input.Fragments[ThermalContext]["temperature"]
                            .RequireScalar<long>();
                        _moves++;
                        return OperationEffect.SucceededAsync(ShapeValue.Unit);
                    });
                genesis.Operation(
                    TelemetryRead,
                    store,
                    ShapeContract.Unit,
                    ShapeContract.For(TelemetryShape),
                    "explicitly acquire telemetry snapshot",
                    _ =>
                    {
                        StoreReads++;
                        return OperationEffect.SucceededAsync(Telemetry(24));
                    });
                PointerCapability = genesis.Grant(PointerActor, pointerTarget, [PointerMove]);
                StoreCapability = genesis.Grant(StoreActor, store, [TelemetryRead]);
            });
        }

        public AuthorityDomain Domain { get; }
        public ActorReference PointerActor { get; private set; } = null!;
        public ActorReference Attacker { get; private set; } = null!;
        public ActorReference StoreActor { get; private set; } = null!;
        public Capability PointerCapability { get; private set; } = null!;
        public Capability StoreCapability { get; private set; } = null!;
        public OperationReference PointerMove { get; }
        public OperationReference TelemetryRead { get; }
        public ShapeReference PointerShape { get; }
        public ShapeReference TemperatureShape { get; }
        public ShapeReference TelemetryShape { get; }
        public FragmentReference ThermalContext { get; }
        public long LastTemperature { get; private set; }
        public int StoreReads { get; private set; }
        public int ExecutionCount => Domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Execution);

        public ShapeValue PointerInput() => ShapeValue.Record(
            PointerShape,
            ("x", ShapeValue.Signed64(10)),
            ("y", ShapeValue.Signed64(20)));

        public ShapeValue Telemetry(long temperature) => ShapeValue.Record(
            TelemetryShape,
            ("temperature", ShapeValue.Scalar(TemperatureShape, temperature)));

        public EnrichmentProvider CopyProvider(string name = "Experiment:ThermalFromTelemetry") =>
            EnrichmentProvider.CopyField(
                CanonicalName.Parse(name),
                PointerMove,
                ThermalContext,
                new EnrichmentSourceRequirement("telemetry", ShapeContract.For(TelemetryShape)),
                "temperature",
                "temperature");

        public ValueTask<ExecutionResult> ReadStoreAsync() => Domain.ExecuteAsync(
            StoreActor,
            TelemetryRead,
            StoreCapability,
            ShapeValue.Unit);
    }
}
