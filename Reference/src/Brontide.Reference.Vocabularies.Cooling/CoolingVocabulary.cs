using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Vocabularies.Cooling;

/// <summary>Brontide Architecture §17's minimal Cooling Domain Vocabulary.</summary>
public static class CoolingVocabulary
{
    public static readonly OperationReference TemperatureRead = OperationReference.Parse("Temperature.Read");
    public static readonly OperationReference FanSetSpeed = OperationReference.Parse("Fan.SetSpeed");
    public static readonly OperationReference FanStop = OperationReference.Parse("Fan.Stop");
    public static readonly EventReference TemperatureChanged = EventReference.Parse("Sensor.Temperature.Changed");

    public static readonly ShapeReference TemperatureShape = ShapeReference.Parse("Sensor.Temperature", 1);
    public static readonly ShapeReference FanSpeedShape = ShapeReference.Parse("Fan.Speed", 1);

    internal static void Register(
        AuthorityDomain.GenesisContext genesis,
        ActorReference coolingSystem,
        CoolingPlant plant)
    {
        genesis.Shape(ShapeDefinition.Scalar<long>(TemperatureShape));
        genesis.Shape(ShapeDefinition.Scalar<long>(FanSpeedShape));
        genesis.Event(
            TemperatureChanged,
            ShapeContract.For(TemperatureShape),
            "Immutable assertion that a sensor observed a changed temperature; receipt grants no authority.");
        genesis.Operation(
            TemperatureRead,
            coolingSystem,
            ShapeContract.Unit,
            ShapeContract.For(TemperatureShape),
            "Return the sensor's current signed temperature reading.",
            _ => OperationEffect.SucceededAsync(
                ShapeValue.Scalar(TemperatureShape, plant.Temperature),
                $"temperature is {plant.Temperature}"));
        genesis.Operation(
            FanSetSpeed,
            coolingSystem,
            ShapeContract.For(FanSpeedShape),
            ShapeContract.Unit,
            "Set fan speed to the requested bounded percentage.",
            context =>
            {
                var speed = context.Input.RequireScalar<long>();
                if (speed is < 0 or > 100)
                {
                    return OperationEffect.FailedAsync(
                        ShapeContract.For(BuiltInShapes.Details),
                        ShapeValue.Record(
                            BuiltInShapes.Details,
                            ("message", ShapeValue.Text("fan speed must be in the range 0..100"))),
                        "invalid fan speed");
                }

                plant.SetFanSpeed(speed);
                return OperationEffect.SucceededAsync(ShapeValue.Unit, $"fan speed set to {speed}");
            });
        genesis.Operation(
            FanStop,
            coolingSystem,
            ShapeContract.Unit,
            ShapeContract.Unit,
            "Latch the fan into its stopped state.",
            _ =>
            {
                plant.StopFan();
                return OperationEffect.SucceededAsync(ShapeValue.Unit, "fan stopped");
            });
    }
}

internal sealed class CoolingPlant(long temperature)
{
    public long Temperature { get; } = temperature;
    public long FanSpeed { get; private set; }
    public int EffectCount { get; private set; }

    public void SetFanSpeed(long speed)
    {
        FanSpeed = speed;
        EffectCount++;
    }

    public void StopFan()
    {
        FanSpeed = 0;
        EffectCount++;
    }
}

public sealed record CoolingScenarioResult(
    AuthorityDomain Domain,
    DomainEvent TemperatureEvent,
    ExecutionResult TemperatureRead,
    ExecutionResult AcceptedSetSpeed,
    ExecutionResult DeniedSetSpeed,
    ExecutionResult EmergencyStop,
    Capability EmergencyGrant,
    int ExecutionCountImmediatelyAfterEvent,
    int EffectsImmediatelyAfterEvent,
    long FinalFanSpeed,
    int TotalEffects,
    ImmutableArray<string> Transcript);

/// <summary>A deterministic, non-interactive M2 host exercising the public Execution path.</summary>
public static class CoolingScenario
{
    public static async ValueTask<CoolingScenarioResult> RunAsync(long temperature = 86)
    {
        ActorReference sensorReader = null!;
        ActorReference coolingController = null!;
        ActorReference safetySupervisor = null!;
        ActorReference emergencyHandler = null!;
        ActorReference coolingSystem = null!;
        Capability sensorGrant = null!;
        Capability coolingGrant = null!;
        Capability emergencyGrant = null!;
        var plant = new CoolingPlant(temperature);
        var transcript = ImmutableArray.CreateBuilder<string>();

        var domain = AuthorityDomain.Create("Cooling showcase", genesis =>
        {
            sensorReader = genesis.Actor("SensorReader");
            coolingController = genesis.Actor("CoolingController");
            safetySupervisor = genesis.Actor("SafetySupervisor");
            emergencyHandler = genesis.Actor("EmergencyHandler");
            coolingSystem = genesis.Actor("CoolingSystem");
            CoolingVocabulary.Register(genesis, coolingSystem, plant);

            sensorGrant = genesis.Grant(sensorReader, coolingSystem, [CoolingVocabulary.TemperatureRead]);
            coolingGrant = genesis.Grant(
                coolingController,
                coolingSystem,
                [CoolingVocabulary.TemperatureRead, CoolingVocabulary.FanSetSpeed, CoolingVocabulary.FanStop]);
            var safetyGrant = genesis.Grant(
                safetySupervisor,
                coolingSystem,
                [CoolingVocabulary.TemperatureRead, CoolingVocabulary.FanSetSpeed, CoolingVocabulary.FanStop]);
            emergencyGrant = safetyGrant.Delegate(
                emergencyHandler,
                new PermittedOperationsConstraint(CoolingVocabulary.FanStop));
        });

        var temperatureEvent = domain.EmitEvent(
            sensorReader,
            CoolingVocabulary.TemperatureChanged,
            ShapeValue.Scalar(CoolingVocabulary.TemperatureShape, temperature));
        var executionsAfterEvent = domain.Provenance.Count(entry => entry.Kind == ProvenanceKind.Execution);
        var effectsAfterEvent = plant.EffectCount;
        transcript.Add($"event {temperatureEvent.Kind}: {temperature} (no Execution initiated)");

        var read = await domain.ExecuteAsync(
            sensorReader,
            CoolingVocabulary.TemperatureRead,
            sensorGrant,
            ShapeValue.Unit);
        transcript.Add($"{CoolingVocabulary.TemperatureRead}: {read.Outcome.Status}");

        var setSpeed = await domain.ExecuteAsync(
            coolingController,
            CoolingVocabulary.FanSetSpeed,
            coolingGrant,
            ShapeValue.Scalar(CoolingVocabulary.FanSpeedShape, 70L));
        transcript.Add($"{CoolingVocabulary.FanSetSpeed} by CoolingController: {setSpeed.Outcome.Status}");

        var denied = await domain.ExecuteAsync(
            emergencyHandler,
            CoolingVocabulary.FanSetSpeed,
            emergencyGrant,
            ShapeValue.Scalar(CoolingVocabulary.FanSpeedShape, 100L));
        transcript.Add($"{CoolingVocabulary.FanSetSpeed} by EmergencyHandler: {denied.Outcome.Message}");

        var stopped = await domain.ExecuteAsync(
            emergencyHandler,
            CoolingVocabulary.FanStop,
            emergencyGrant,
            ShapeValue.Unit);
        transcript.Add($"{CoolingVocabulary.FanStop} by EmergencyHandler: {stopped.Outcome.Status}");

        return new CoolingScenarioResult(
            domain,
            temperatureEvent,
            read,
            setSpeed,
            denied,
            stopped,
            emergencyGrant,
            executionsAfterEvent,
            effectsAfterEvent,
            plant.FanSpeed,
            plant.EffectCount,
            transcript.ToImmutable());
    }
}

/// <summary>
/// A small public binary Cooling component used by external hosts. It maps the binary contract to
/// the native Fan.SetSpeed/Fan.Stop Operations without exposing the private plant representation.
/// </summary>
public sealed class BinaryCoolingComponent
{
    private readonly CoolingPlant _plant;

    private BinaryCoolingComponent(
        AuthorityDomain domain,
        ActorReference actor,
        Capability capability,
        CoolingPlant plant)
    {
        Domain = domain;
        Actor = actor;
        Capability = capability;
        _plant = plant;
    }

    public AuthorityDomain Domain { get; }
    public ActorReference Actor { get; }
    public Capability Capability { get; }
    public bool CoolingEnabled => _plant.FanSpeed > 0;
    public long Revision { get; private set; }
    public int EffectCount => _plant.EffectCount;

    public static BinaryCoolingComponent Create(long temperature = 20)
    {
        ActorReference actor = null!;
        ActorReference target = null!;
        Capability capability = null!;
        var plant = new CoolingPlant(temperature);
        var domain = AuthorityDomain.Create("Binary Cooling component", genesis =>
        {
            actor = genesis.Actor("BinaryCoolingController");
            target = genesis.Actor("BinaryCoolingPlant");
            CoolingVocabulary.Register(genesis, target, plant);
            capability = genesis.Grant(
                actor,
                target,
                [CoolingVocabulary.FanSetSpeed, CoolingVocabulary.FanStop]);
        });
        return new BinaryCoolingComponent(domain, actor, capability, plant);
    }

    public async ValueTask<ExecutionResult> SetEnabledAsync(bool enabled)
    {
        var result = await Domain.ExecuteAsync(
            Actor,
            enabled ? CoolingVocabulary.FanSetSpeed : CoolingVocabulary.FanStop,
            Capability,
            enabled
                ? ShapeValue.Scalar(CoolingVocabulary.FanSpeedShape, 100L)
                : ShapeValue.Unit).ConfigureAwait(false);
        if (result.Outcome.Status == OutcomeStatus.Succeeded)
        {
            Revision++;
        }

        return result;
    }
}
