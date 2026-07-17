using System.Collections.Immutable;
using Fabric.Core;

namespace Fabric.Extensions.Flow;

public static class FlowContracts
{
    public static readonly OperationReference Open = OperationReference.Parse("Flow.Open");
    public static readonly OperationReference PublishItem = OperationReference.Parse("Flow.Item.Publish");
    public static readonly OperationReference RequestReplay = OperationReference.Parse("Flow.RequestReplay");
    public static readonly EventReference GapDetected = EventReference.Parse("Flow.GapDetected");

    public static readonly ShapeReference OpenRequest = ShapeReference.Parse("Flow.Open.Request", 1);
    public static readonly ShapeReference PublishRequest = ShapeReference.Parse("Flow.Item.Publish.Request", 1);
    public static readonly ShapeReference ReplayRequest = ShapeReference.Parse("Flow.Replay.Request", 1);
    public static readonly ShapeReference Handle = ShapeReference.Parse("Flow.Handle", 1);
    public static readonly ShapeReference ReplayResult = ShapeReference.Parse("Flow.Replay.Result", 1);
    public static readonly ShapeReference GapAssertion = ShapeReference.Parse("Flow.Gap", 1);
}

public enum FlowOrdering
{
    None,
    SourcePosition
}

public enum FlowDelivery
{
    AtMostOnce,
    AtLeastOnce
}

public sealed record FlowRecoveryContract(
    FlowOrdering Ordering,
    FlowDelivery Delivery,
    bool ReplaySupported,
    int RetentionItems,
    string GapPolicy,
    string ResumeBehaviour)
{
    public static FlowRecoveryContract Recoverable(int retentionItems = 256) => new(
        FlowOrdering.SourcePosition,
        FlowDelivery.AtLeastOnce,
        true,
        retentionItems,
        "detect and request replay",
        "resume after last observed source position");
}

public sealed record FlowItem(
    InteractionContext Interaction,
    Guid FlowId,
    long Position,
    long SourcePosition,
    ShapeValue Item,
    bool IsReplay = false);

public sealed record FlowGap(Guid FlowId, long FromPosition, long ToPosition);

public sealed class FlowCursor
{
    private readonly SortedSet<long> _missing = [];

    internal FlowCursor(Guid flowId)
    {
        FlowId = flowId;
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
    public Guid FlowId { get; }
    public long LastObservedPosition { get; internal set; }
    public IReadOnlyCollection<long> MissingPositions => _missing.ToArray();

    internal void MarkMissing(long position) => _missing.Add(position);
    internal void MarkRecovered(long position) => _missing.Remove(position);
}

public sealed record FlowReadResult(
    ImmutableArray<FlowItem> Items,
    ImmutableArray<FlowGap> Gaps,
    FlowCursor Cursor);

/// <summary>
/// One established, bounded Flow. Creation, Item publication, and replay are available only through
/// <see cref="FlowRuntime"/>, whose handlers run behind the Core Execution gate.
/// </summary>
public sealed class RecoverableFlow
{
    private readonly object _gate = new();
    private readonly ShapeRegistry _shapes;
    private readonly ShapeContract _itemShape;
    private readonly List<FlowItem> _retained = [];
    private long _nextPosition;

    internal RecoverableFlow(
        ShapeRegistry shapes,
        ShapeContract itemShape,
        ActorReference producer,
        ActorReference consumer,
        FlowRecoveryContract recovery)
    {
        _shapes = shapes ?? throw new ArgumentNullException(nameof(shapes));
        _itemShape = itemShape ?? throw new ArgumentNullException(nameof(itemShape));
        Producer = producer ?? throw new ArgumentNullException(nameof(producer));
        Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        Recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        if (recovery.RetentionItems < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(recovery), "Flow retention must be positive.");
        }

        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
    public ActorReference Producer { get; }
    public ActorReference Consumer { get; }
    public FlowRecoveryContract Recovery { get; }
    public IReadOnlyList<FlowItem> RetainedItems
    {
        get { lock (_gate) { return _retained.ToArray(); } }
    }

    public FlowCursor OpenCursor() => new(Id);

    internal FlowItem PublishAuthorized(ActorReference producer, ShapeValue item, OriginClass origin)
    {
        if (!ReferenceEquals(producer, Producer))
        {
            throw new InvalidOperationException("Only the established Flow producer may publish an Item.");
        }

        var projected = _shapes.Project(item, _itemShape);
        if (!projected.IsValid)
        {
            throw new InvalidOperationException($"Flow Item Shape is invalid: {projected.Message}");
        }

        lock (_gate)
        {
            var position = ++_nextPosition;
            var flowItem = new FlowItem(
                new InteractionContext(Producer, OccurrenceReference.New(), Origin: origin),
                Id,
                position,
                position,
                projected.Value!);
            _retained.Add(flowItem);
            while (_retained.Count > Recovery.RetentionItems)
            {
                _retained.RemoveAt(0);
            }

            return flowItem;
        }
    }

    public FlowReadResult Read(FlowCursor cursor, IEnumerable<long>? deliberatelyDropped = null)
    {
        EnsureCursor(cursor);
        var dropped = (deliberatelyDropped ?? []).ToImmutableHashSet();
        ImmutableArray<FlowItem> candidates;
        lock (_gate)
        {
            candidates = _retained
                .Where(item => item.SourcePosition > cursor.LastObservedPosition)
                .OrderBy(item => item.SourcePosition)
                .ToImmutableArray();
        }

        var delivered = ImmutableArray.CreateBuilder<FlowItem>();
        foreach (var item in candidates)
        {
            if (dropped.Contains(item.SourcePosition))
            {
                cursor.MarkMissing(item.SourcePosition);
            }
            else
            {
                delivered.Add(item);
            }

            cursor.LastObservedPosition = item.SourcePosition;
        }

        return new FlowReadResult(delivered.ToImmutable(), CoalesceGaps(cursor.MissingPositions), cursor);
    }

    internal ImmutableArray<FlowGap> CurrentGaps(FlowCursor cursor)
    {
        EnsureCursor(cursor);
        return CoalesceGaps(cursor.MissingPositions);
    }

    internal FlowReadResult ReplayAuthorized(ActorReference consumer, FlowCursor cursor)
    {
        if (!ReferenceEquals(consumer, Consumer))
        {
            throw new InvalidOperationException("Only the established Flow consumer may request replay.");
        }

        EnsureCursor(cursor);
        if (!Recovery.ReplaySupported)
        {
            throw new InvalidOperationException("This Flow recovery contract does not offer replay.");
        }

        var requested = cursor.MissingPositions.ToImmutableHashSet();
        ImmutableArray<FlowItem> retained;
        lock (_gate)
        {
            retained = _retained
                .Where(item => requested.Contains(item.SourcePosition))
                .OrderBy(item => item.SourcePosition)
                .ToImmutableArray();
        }

        var replayed = retained.Select(item => item with
        {
            Interaction = item.Interaction with { Origin = OriginClass.Derived },
            IsReplay = true
        }).ToImmutableArray();
        foreach (var item in replayed)
        {
            cursor.MarkRecovered(item.SourcePosition);
        }

        return new FlowReadResult(replayed, CoalesceGaps(cursor.MissingPositions), cursor);
    }

    private ImmutableArray<FlowGap> CoalesceGaps(IEnumerable<long> missing)
    {
        var ordered = missing.Order().ToArray();
        if (ordered.Length == 0)
        {
            return [];
        }

        var gaps = ImmutableArray.CreateBuilder<FlowGap>();
        var start = ordered[0];
        var end = start;
        foreach (var position in ordered.Skip(1))
        {
            if (position == end + 1)
            {
                end = position;
                continue;
            }

            gaps.Add(new FlowGap(Id, start, end));
            start = end = position;
        }

        gaps.Add(new FlowGap(Id, start, end));
        return gaps.ToImmutable();
    }

    private void EnsureCursor(FlowCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(cursor);
        if (cursor.FlowId != Id)
        {
            throw new InvalidOperationException("Cursor belongs to another Flow.");
        }
    }
}

/// <summary>
/// Capability-gated Flow operations. Item publication is independently authorised, which is one
/// of the two continuing-authority realisations permitted by Atlas Architecture §19.1.1.
/// </summary>
public sealed class FlowRuntime
{
    private readonly object _gate = new();
    private readonly Dictionary<string, OpenRequest> _openRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PublicationRequest> _publicationRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ReplayRequest> _replayRequests = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RecoverableFlow> _opened = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FlowReadResult> _replayed = new(StringComparer.Ordinal);

    public void Register(AuthorityDomain.GenesisContext genesis, ActorReference mediator)
    {
        ArgumentNullException.ThrowIfNull(genesis);
        ArgumentNullException.ThrowIfNull(mediator);
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.OpenRequest,
            FragmentPolicy.Closed,
            RecordField.Required("token", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.PublishRequest,
            FragmentPolicy.Closed,
            RecordField.Required("token", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.ReplayRequest,
            FragmentPolicy.Closed,
            RecordField.Required("token", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.Handle,
            FragmentPolicy.Closed,
            RecordField.Required("id", BuiltInShapes.Text)));
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.ReplayResult,
            FragmentPolicy.Closed,
            RecordField.Required("replayed", BuiltInShapes.Signed64),
            RecordField.Required("remaining-gaps", BuiltInShapes.Signed64)));
        genesis.Shape(ShapeDefinition.Record(
            FlowContracts.GapAssertion,
            FragmentPolicy.Closed,
            RecordField.Required("flow-id", BuiltInShapes.Text),
            RecordField.Required("from", BuiltInShapes.Signed64),
            RecordField.Required("to", BuiltInShapes.Signed64)));
        genesis.Event(
            FlowContracts.GapDetected,
            ShapeContract.For(FlowContracts.GapAssertion),
            "A consumer detected a missing range in one established Flow.");

        genesis.Operation(
            FlowContracts.Open,
            mediator,
            ShapeContract.For(FlowContracts.OpenRequest),
            ShapeContract.For(FlowContracts.Handle),
            "Establish a typed Flow between declared participants.",
            context =>
            {
                var token = Token(context.Input);
                OpenRequest request;
                lock (_gate)
                {
                    if (!_openRequests.Remove(token, out request!))
                    {
                        return Failure("Flow open token is missing or already consumed.");
                    }
                }

                if (!ReferenceEquals(context.Execution.Initiator, request.Producer))
                {
                    return Failure("The opening Actor is not the staged Flow producer.");
                }

                var flow = new RecoverableFlow(
                    request.Shapes,
                    request.ItemShape,
                    request.Producer,
                    request.Consumer,
                    request.Recovery);
                lock (_gate)
                {
                    _opened.Add(token, flow);
                }

                return OperationEffect.SucceededAsync(
                    ShapeValue.Record(FlowContracts.Handle, ("id", ShapeValue.Text(flow.Id.ToString("N")))),
                    "Flow established");
            });

        genesis.Operation(
            FlowContracts.PublishItem,
            mediator,
            ShapeContract.For(FlowContracts.PublishRequest),
            ShapeContract.Unit,
            "Publish one typed Item through an independently authorised Flow Item Execution.",
            context =>
            {
                var token = Token(context.Input);
                PublicationRequest request;
                lock (_gate)
                {
                    if (!_publicationRequests.Remove(token, out request!))
                    {
                        return Failure("Flow publication token is missing or already consumed.");
                    }
                }

                try
                {
                    _ = request.Flow.PublishAuthorized(
                        context.Execution.Initiator,
                        request.Item,
                        context.Execution.Interaction.Origin);
                    return OperationEffect.SucceededAsync(ShapeValue.Unit, "Flow Item published");
                }
                catch (Exception exception)
                {
                    return Failure(exception.Message);
                }
            });

        genesis.Operation(
            FlowContracts.RequestReplay,
            mediator,
            ShapeContract.For(FlowContracts.ReplayRequest),
            ShapeContract.For(FlowContracts.ReplayResult),
            "Request replay of the missing positions tracked by one Flow cursor.",
            context =>
            {
                var token = Token(context.Input);
                ReplayRequest request;
                lock (_gate)
                {
                    if (!_replayRequests.Remove(token, out request!))
                    {
                        return Failure("Flow replay token is missing or already consumed.");
                    }
                }

                try
                {
                    foreach (var gap in request.Flow.CurrentGaps(request.Cursor))
                    {
                        _ = context.EmitEvent(
                            FlowContracts.GapDetected,
                            ShapeValue.Record(
                                FlowContracts.GapAssertion,
                                ("flow-id", ShapeValue.Text(gap.FlowId.ToString("N"))),
                                ("from", ShapeValue.Signed64(gap.FromPosition)),
                                ("to", ShapeValue.Signed64(gap.ToPosition))));
                    }

                    var replay = request.Flow.ReplayAuthorized(context.Execution.Initiator, request.Cursor);
                    lock (_gate)
                    {
                        _replayed.Add(token, replay);
                    }

                    return OperationEffect.SucceededAsync(
                        ShapeValue.Record(
                            FlowContracts.ReplayResult,
                            ("replayed", ShapeValue.Signed64(replay.Items.Length)),
                            ("remaining-gaps", ShapeValue.Signed64(replay.Gaps.Length))),
                        "Flow replay completed");
                }
                catch (Exception exception)
                {
                    return Failure(exception.Message);
                }
            });
    }

    public ShapeValue StageOpen(
        AuthorityDomain domain,
        ActorReference producer,
        ActorReference consumer,
        ShapeContract itemShape,
        FlowRecoveryContract recovery)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(producer);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(itemShape);
        ArgumentNullException.ThrowIfNull(recovery);
        var actors = domain.Actors;
        if (!actors.Any(actor => ReferenceEquals(actor, producer)) ||
            !actors.Any(actor => ReferenceEquals(actor, consumer)))
        {
            throw new InvalidOperationException("Flow participants must belong to the supplied authority domain.");
        }

        if (!domain.Shapes.Recognizes(itemShape))
        {
            throw new InvalidOperationException("The Flow Item Shape must be recognised by the supplied authority domain.");
        }

        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _openRequests.Add(token, new OpenRequest(domain.Shapes, producer, consumer, itemShape, recovery));
        }

        return Request(FlowContracts.OpenRequest, token);
    }

    public ShapeValue StagePublication(RecoverableFlow flow, ShapeValue item)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(item);
        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _publicationRequests.Add(token, new PublicationRequest(flow, item));
        }

        return Request(FlowContracts.PublishRequest, token);
    }

    public ShapeValue StageReplay(RecoverableFlow flow, FlowCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(flow);
        ArgumentNullException.ThrowIfNull(cursor);
        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            _replayRequests.Add(token, new ReplayRequest(flow, cursor));
        }

        return Request(FlowContracts.ReplayRequest, token);
    }

    public RecoverableFlow RequireOpenedFlow(ExecutionResult opening)
    {
        ArgumentNullException.ThrowIfNull(opening);
        if (opening.Outcome.Status != OutcomeStatus.Succeeded)
        {
            throw new InvalidOperationException("The Flow opening Execution did not succeed.");
        }

        var token = Token(opening.Execution.Input);
        lock (_gate)
        {
            return _opened.Remove(token, out var flow)
                ? flow
                : throw new InvalidOperationException("The opened Flow has already been claimed.");
        }
    }

    public FlowReadResult RequireReplayResult(ExecutionResult replay)
    {
        ArgumentNullException.ThrowIfNull(replay);
        if (replay.Outcome.Status != OutcomeStatus.Succeeded)
        {
            throw new InvalidOperationException("The Flow replay Execution did not succeed.");
        }

        var token = Token(replay.Execution.Input);
        lock (_gate)
        {
            return _replayed.Remove(token, out var result)
                ? result
                : throw new InvalidOperationException("The replay result has already been claimed.");
        }
    }

    private static string Token(ShapeValue request) => request.RequireField("token").RequireScalar<string>();

    private static ShapeValue Request(ShapeReference shape, string token) =>
        ShapeValue.Record(shape, ("token", ShapeValue.Text(token)));

    private static ValueTask<OperationEffect> Failure(string message) => OperationEffect.FailedAsync(
        ShapeContract.For(BuiltInShapes.Details),
        ShapeValue.Record(BuiltInShapes.Details, ("message", ShapeValue.Text(message))),
        message);

    private sealed record OpenRequest(
        ShapeRegistry Shapes,
        ActorReference Producer,
        ActorReference Consumer,
        ShapeContract ItemShape,
        FlowRecoveryContract Recovery);

    private sealed record PublicationRequest(RecoverableFlow Flow, ShapeValue Item);
    private sealed record ReplayRequest(RecoverableFlow Flow, FlowCursor Cursor);
}

public sealed record PointerFlowScenarioResult(
    AuthorityDomain Domain,
    RecoverableFlow Flow,
    ExecutionResult Open,
    ExecutionResult SpoofedPublication,
    ImmutableArray<ExecutionResult> Publications,
    FlowReadResult InitialRead,
    ExecutionResult ReplayExecution,
    FlowReadResult Replay,
    ImmutableArray<FlowItem> Published);

public static class PointerFlowScenario
{
    public static async ValueTask<PointerFlowScenarioResult> RunAsync()
    {
        ActorReference producer = null!;
        ActorReference consumer = null!;
        ActorReference mediator = null!;
        Capability openCapability = null!;
        Capability publishCapability = null!;
        Capability spoofingCapability = null!;
        Capability replayCapability = null!;
        var pointer = ShapeReference.Parse("Input.Pointer.Motion", 1);
        var runtime = new FlowRuntime();
        var domain = AuthorityDomain.Create("pointer-flow", genesis =>
        {
            producer = genesis.Actor("PointerProducer");
            consumer = genesis.Actor("PointerConsumer");
            mediator = genesis.Actor("FlowMediator");
            genesis.Shape(ShapeDefinition.Record(
                pointer,
                FragmentPolicy.Open,
                RecordField.Required("x", BuiltInShapes.Signed64),
                RecordField.Required("y", BuiltInShapes.Signed64)));
            runtime.Register(genesis, mediator);
            openCapability = genesis.Grant(producer, mediator, [FlowContracts.Open]);
            publishCapability = genesis.Grant(
                producer,
                mediator,
                [FlowContracts.PublishItem],
                [new OriginGrantConstraint(OriginClass.Device)]);
            spoofingCapability = genesis.Grant(producer, mediator, [FlowContracts.PublishItem]);
            replayCapability = genesis.Grant(consumer, mediator, [FlowContracts.RequestReplay]);
        });

        var open = await domain.ExecuteAsync(
            producer,
            FlowContracts.Open,
            openCapability,
            runtime.StageOpen(domain, producer, consumer, ShapeContract.For(pointer), FlowRecoveryContract.Recoverable()));
        var flow = runtime.RequireOpenedFlow(open);
        var spoofed = await domain.ExecuteAsync(
            producer,
            FlowContracts.PublishItem,
            spoofingCapability,
            ShapeValue.Record(
                FlowContracts.PublishRequest,
                ("token", ShapeValue.Text(Guid.NewGuid().ToString("N")))),
            OriginClass.Device);

        var publications = ImmutableArray.CreateBuilder<ExecutionResult>();
        foreach (var item in new[] { Motion(pointer, 1, 1), Motion(pointer, 2, 2), Motion(pointer, 3, 3) })
        {
            publications.Add(await domain.ExecuteAsync(
                producer,
                FlowContracts.PublishItem,
                publishCapability,
                runtime.StagePublication(flow, item),
                OriginClass.Device));
        }

        var published = flow.RetainedItems.ToImmutableArray();
        var cursor = flow.OpenCursor();
        var initial = flow.Read(cursor, [2]);
        var replayExecution = await domain.ExecuteAsync(
            consumer,
            FlowContracts.RequestReplay,
            replayCapability,
            runtime.StageReplay(flow, cursor));
        var replay = runtime.RequireReplayResult(replayExecution);
        return new PointerFlowScenarioResult(
            domain,
            flow,
            open,
            spoofed,
            publications.ToImmutable(),
            initial,
            replayExecution,
            replay,
            published);
    }

    private static ShapeValue Motion(ShapeReference pointer, long x, long y) => ShapeValue.Record(
        pointer,
        ("x", ShapeValue.Signed64(x)),
        ("y", ShapeValue.Signed64(y)));
}
