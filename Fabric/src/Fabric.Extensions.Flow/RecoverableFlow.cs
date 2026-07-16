using System.Collections.Immutable;
using Fabric.Core;

namespace Fabric.Extensions.Flow;

public static class FlowContracts
{
    public static readonly OperationReference Open = OperationReference.Parse("Flow.Open");
    public static readonly OperationReference Acknowledge = OperationReference.Parse("Flow.Acknowledge");
    public static readonly OperationReference RequestReplay = OperationReference.Parse("Flow.RequestReplay");
    public static readonly OperationReference Resume = OperationReference.Parse("Flow.Resume");
    public static readonly OperationReference Cancel = OperationReference.Parse("Flow.Cancel");
    public static readonly EventReference GapDetected = EventReference.Parse("Flow.GapDetected");
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

/// <summary>A bounded in-memory Flow experiment with opaque cursors and explicit replay.</summary>
public sealed class RecoverableFlow
{
    private readonly object _gate = new();
    private readonly ShapeRegistry _shapes;
    private readonly ShapeContract _itemShape;
    private readonly List<FlowItem> _retained = [];
    private long _nextPosition;

    public RecoverableFlow(
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

    public FlowCursor OpenCursor() => new(Id);

    public FlowItem Publish(ShapeValue item, OriginClass origin = OriginClass.Unverified)
    {
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

        var gaps = CoalesceGaps(cursor.MissingPositions);
        return new FlowReadResult(delivered.ToImmutable(), gaps, cursor);
    }

    public FlowReadResult Replay(FlowCursor cursor)
    {
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

        var remaining = CoalesceGaps(cursor.MissingPositions);
        return new FlowReadResult(replayed, remaining, cursor);
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

public sealed record PointerFlowScenarioResult(
    RecoverableFlow Flow,
    FlowReadResult InitialRead,
    FlowReadResult Replay,
    ImmutableArray<FlowItem> Published);

public static class PointerFlowScenario
{
    public static PointerFlowScenarioResult Run()
    {
        ActorReference producer = null!;
        ActorReference consumer = null!;
        var pointer = ShapeReference.Parse("Input.Pointer.Motion", 1);
        var domain = AuthorityDomain.Create("pointer-flow", genesis =>
        {
            producer = genesis.Actor("PointerProducer");
            consumer = genesis.Actor("PointerConsumer");
            genesis.Shape(ShapeDefinition.Record(
                pointer,
                FragmentPolicy.Open,
                RecordField.Required("x", BuiltInShapes.Signed64),
                RecordField.Required("y", BuiltInShapes.Signed64)));
        });
        var flow = new RecoverableFlow(
            domain.Shapes,
            ShapeContract.For(pointer),
            producer,
            consumer,
            FlowRecoveryContract.Recoverable());
        var published = ImmutableArray.Create(
            flow.Publish(Motion(pointer, 1, 1), OriginClass.Device),
            flow.Publish(Motion(pointer, 2, 2), OriginClass.Device),
            flow.Publish(Motion(pointer, 3, 3), OriginClass.Device));
        var cursor = flow.OpenCursor();
        var initial = flow.Read(cursor, [2]);
        var replay = flow.Replay(cursor);
        return new PointerFlowScenarioResult(flow, initial, replay, published);
    }

    private static ShapeValue Motion(ShapeReference pointer, long x, long y) => ShapeValue.Record(
        pointer,
        ("x", ShapeValue.Signed64(x)),
        ("y", ShapeValue.Signed64(y)));
}
