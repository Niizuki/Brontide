using System.Collections.Immutable;
using Brontide.Reference.Core;

namespace Brontide.Reference.Extensions.Events;

public static class EventDistributionContracts
{
    public static readonly OperationReference Publish = OperationReference.Parse("Event.Publish");
    public static readonly OperationReference Observe = OperationReference.Parse("Event.Observe");
    public static readonly ShapeReference PublishRequest = ShapeReference.Parse("Event.Publish.Request", 1);
    public static readonly ShapeReference ObserveRequest = ShapeReference.Parse("Event.Observe.Request", 1);
    public static readonly ShapeReference Subscription = ShapeReference.Parse("Event.Subscription", 1);
}

public sealed class EventSubscription
{
    private readonly object _gate = new();
    private readonly Action<DomainEvent>? _observer;
    private readonly List<DomainEvent> _received = [];

    internal EventSubscription(Action<DomainEvent>? observer)
    {
        Id = Guid.NewGuid();
        _observer = observer;
    }

    public Guid Id { get; }
    public IReadOnlyList<DomainEvent> Received
    {
        get { lock (_gate) { return _received.ToArray(); } }
    }

    internal void Deliver(DomainEvent @event)
    {
        lock (_gate)
        {
            _received.Add(@event);
        }

        _observer?.Invoke(@event);
    }
}

/// <summary>
/// A capability-gated mediator. Events remain immutable and preserve their original emitter.
/// </summary>
public sealed class EventMediatorRuntime
{
    private readonly object _gate = new();
    private readonly Dictionary<string, DomainEvent> _stagedPublications = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Action<DomainEvent>?> _stagedObservers = new(StringComparer.Ordinal);
    private readonly List<EventSubscription> _subscriptions = [];

    public IReadOnlyList<EventSubscription> Subscriptions
    {
        get { lock (_gate) { return _subscriptions.ToArray(); } }
    }

    public void Register(AuthorityDomain.GenesisContext genesis, ActorReference mediator)
    {
        ArgumentNullException.ThrowIfNull(genesis);
        ArgumentNullException.ThrowIfNull(mediator);
        genesis.Shape(ShapeDefinition.Record(
            EventDistributionContracts.PublishRequest,
            FragmentPolicy.Closed,
            RecordField.Required("token", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            EventDistributionContracts.ObserveRequest,
            FragmentPolicy.Closed,
            RecordField.Required("token", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            EventDistributionContracts.Subscription,
            FragmentPolicy.Closed,
            RecordField.Required("id", BuiltInShapes.Text)));
        genesis.Operation(
            EventDistributionContracts.Publish,
            mediator,
            ShapeContract.For(EventDistributionContracts.PublishRequest),
            ShapeContract.Unit,
            "Publish one already-emitted immutable Event through a mediator.",
            context =>
            {
                var token = context.Input.RequireField("token").RequireScalar<string>();
                DomainEvent published;
                EventSubscription[] subscribers;
                lock (_gate)
                {
                    if (!_stagedPublications.Remove(token, out published!))
                    {
                        return OperationEffect.FailedAsync(
                            ShapeContract.For(BuiltInShapes.Details),
                            ShapeValue.Record(
                                BuiltInShapes.Details,
                                ("message", ShapeValue.Text("publication token is missing or already consumed"))),
                            "publication failed");
                    }

                    subscribers = _subscriptions.ToArray();
                }

                foreach (var subscriber in subscribers)
                {
                    subscriber.Deliver(published);
                }

                return OperationEffect.SucceededAsync(ShapeValue.Unit, $"published to {subscribers.Length} observers");
            });
        genesis.Operation(
            EventDistributionContracts.Observe,
            mediator,
            ShapeContract.For(EventDistributionContracts.ObserveRequest),
            ShapeContract.For(EventDistributionContracts.Subscription),
            "Establish one independently authorised Event observation subscription.",
            context =>
            {
                var token = context.Input.RequireField("token").RequireScalar<string>();
                EventSubscription subscription;
                lock (_gate)
                {
                    if (!_stagedObservers.Remove(token, out var observer))
                    {
                        return OperationEffect.FailedAsync(
                            ShapeContract.For(BuiltInShapes.Details),
                            ShapeValue.Record(
                                BuiltInShapes.Details,
                                ("message", ShapeValue.Text("observation token is missing or already consumed"))),
                            "subscription failed");
                    }

                    subscription = new EventSubscription(observer);
                    _subscriptions.Add(subscription);
                }

                return OperationEffect.SucceededAsync(
                    ShapeValue.Record(
                        EventDistributionContracts.Subscription,
                        ("id", ShapeValue.Text(subscription.Id.ToString("N")))),
                    "subscription established");
            });
    }

    public ShapeValue StagePublication(DomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _stagedPublications.Add(token, @event);
        }

        return ShapeValue.Record(
            EventDistributionContracts.PublishRequest,
            ("token", ShapeValue.Text(token)));
    }

    public ShapeValue StageObservation(Action<DomainEvent>? observer = null)
    {
        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _stagedObservers.Add(token, observer);
        }

        return ShapeValue.Record(
            EventDistributionContracts.ObserveRequest,
            ("token", ShapeValue.Text(token)));
    }

    public DomainEvent Replay(EventSubscription subscription, DomainEvent original)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(original);
        lock (_gate)
        {
            if (!_subscriptions.Contains(subscription))
            {
                throw new InvalidOperationException("The subscription does not belong to this mediator.");
            }
        }

        var replay = original with
        {
            Interaction = original.Interaction with { Origin = OriginClass.Derived },
            IsReplay = true
        };
        subscription.Deliver(replay);
        return replay;
    }
}

public sealed record EventDistributionScenarioResult(
    ExecutionResult ObserveFirst,
    ExecutionResult ObserveSecond,
    ExecutionResult UnauthorizedPublish,
    ExecutionResult Publish,
    DomainEvent Original,
    DomainEvent Replay,
    ImmutableArray<EventSubscription> Subscriptions);

public static class EventDistributionScenario
{
    public static async ValueTask<EventDistributionScenarioResult> RunAsync()
    {
        var eventKind = EventReference.Parse("Example.Changed");
        ActorReference publisher = null!;
        ActorReference firstObserver = null!;
        ActorReference secondObserver = null!;
        ActorReference mediator = null!;
        ActorReference intruder = null!;
        Capability publishCapability = null!;
        Capability firstObserveCapability = null!;
        Capability secondObserveCapability = null!;
        var runtime = new EventMediatorRuntime();
        var domain = AuthorityDomain.Create("event-distribution", genesis =>
        {
            publisher = genesis.Actor("Publisher");
            firstObserver = genesis.Actor("FirstObserver");
            secondObserver = genesis.Actor("SecondObserver");
            mediator = genesis.Actor("EventMediator");
            intruder = genesis.Actor("Intruder");
            genesis.Event(eventKind, ShapeContract.For(BuiltInShapes.Text), "example assertion");
            runtime.Register(genesis, mediator);
            publishCapability = genesis.Grant(publisher, mediator, [EventDistributionContracts.Publish]);
            firstObserveCapability = genesis.Grant(firstObserver, mediator, [EventDistributionContracts.Observe]);
            secondObserveCapability = genesis.Grant(secondObserver, mediator, [EventDistributionContracts.Observe]);
        });

        var first = await domain.ExecuteAsync(
            firstObserver,
            EventDistributionContracts.Observe,
            firstObserveCapability,
            runtime.StageObservation());
        var second = await domain.ExecuteAsync(
            secondObserver,
            EventDistributionContracts.Observe,
            secondObserveCapability,
            runtime.StageObservation());
        var original = domain.EmitEvent(publisher, eventKind, ShapeValue.Text("changed"));
        var staged = runtime.StagePublication(original);
        var unauthorized = await domain.ExecuteAsync(
            intruder,
            EventDistributionContracts.Publish,
            publishCapability,
            staged);
        var publish = await domain.ExecuteAsync(
            publisher,
            EventDistributionContracts.Publish,
            publishCapability,
            staged);
        var replay = runtime.Replay(runtime.Subscriptions[0], original);

        return new EventDistributionScenarioResult(
            first,
            second,
            unauthorized,
            publish,
            original,
            replay,
            runtime.Subscriptions.ToImmutableArray());
    }
}
