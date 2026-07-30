using System.Collections.ObjectModel;

namespace Brontide.Reference.Experimental.ComponentManagement;

public enum ActivationDependencyKind
{
    OrdinaryInteraction,
    RelationalInitialisation,
    DescriptorExpansion,
}

public enum ActivationStage
{
    LocalInitialisation,
    Interconnection,
    RelationalInitialisation,
    Ready,
}

public sealed record Cm3EffectObservation(
    bool Prepared,
    bool EstablishmentStarted,
    bool ActorEstablished,
    bool AuthorityGranted,
    bool LifecycleOperationExecuted,
    bool MemberReportedReady,
    bool Released,
    bool OrdinaryInteractionAdmitted,
    bool ActiveGenerationMutated,
    bool RollbackAttempted)
{
    public static Cm3EffectObservation None { get; } =
        new(false, false, false, false, false, false, false, false, false, false);
}

public sealed record ActivationGroupMember(
    OccurrenceId Occurrence,
    DefinitionId Definition,
    RegionId Region,
    IReadOnlyList<ProvidedContract> Provides,
    IReadOnlyList<LifecycleInputId> RequiredReadyInputs,
    IReadOnlyList<LifecycleInputId> AvailableReadyInputs,
    IReadOnlyList<OccurrenceId> WaitsForReadyOf);

public sealed record ActivationDependency(
    ActivationEdgeId Edge,
    OccurrenceId From,
    OccurrenceId To,
    ActivationDependencyKind Kind,
    ContractId Contract,
    VersionLiteral Version,
    bool ObservedBeforeRelease,
    LifecycleProtocolId? Protocol,
    PortId? CrossingPort,
    bool AllowWiderRegionProposal);

public sealed record LifecycleProtocolDeclaration(
    LifecycleProtocolId Protocol,
    ActivationEdgeId Edge,
    OccurrenceId From,
    OccurrenceId To,
    LifecycleOperationId Operation,
    IReadOnlyList<CapabilityId> Authority,
    ShapeId InputShape,
    ShapeId OutputShape,
    string Ordering,
    int TimeoutMilliseconds,
    int RetryLimit,
    bool Idempotent,
    string Completion,
    string Failure,
    string Rollback);

public sealed record RegionCrossingDeclaration(
    ActivationEdgeId Edge,
    RegionId FromRegion,
    RegionId ToRegion,
    PortId Port,
    bool ImportDeclared,
    bool ExportDeclared);

public sealed record ActivationGroupRequest(
    ActivationGroupRequestId Request,
    GenerationId Generation,
    RestartScopeId RestartScope,
    IReadOnlyList<ActivationGroupMember> Members,
    IReadOnlyList<ActivationDependency> Edges,
    IReadOnlyList<LifecycleProtocolDeclaration> Protocols,
    IReadOnlyList<RegionCrossingDeclaration> RegionCrossings);

public sealed record ActivationStageObservation(
    ActivationStage Stage,
    bool OrdinaryGateOpen);

public sealed record ActivationGroupObservation(
    ActivationGroupId Group,
    bool Cyclic,
    IReadOnlyList<ActivationGroupMember> Members,
    IReadOnlyList<ActivationDependency> InternalEdges,
    IReadOnlyList<LifecycleProtocolDeclaration> Protocols,
    IReadOnlyList<RegionCrossingDeclaration> RegionCrossings,
    IReadOnlyList<ActivationStageObservation> Stages,
    bool ReleasePending);

public sealed record InterGroupEdgeObservation(
    ActivationEdgeId Edge,
    ActivationGroupId FromGroup,
    ActivationGroupId ToGroup);

public sealed record ActivationGroupDecision(
    ActivationGroupId? Group,
    OccurrenceId? Member,
    ActivationEdgeId? Edge,
    string Kind,
    string Reason);

public sealed record ActivationGroupPlan(
    GenerationId Generation,
    RestartScopeId RestartScope,
    IReadOnlyList<ActivationGroupObservation> Groups,
    IReadOnlyList<InterGroupEdgeObservation> InterGroupEdges,
    IReadOnlyList<RegionCrossingDeclaration> RegionCrossings,
    IReadOnlyList<ActivationGroupDecision> Decisions,
    Cm3EffectObservation Effects);

public enum ActivationGroupFailureKind
{
    ContradictoryIdentity,
    MissingMember,
    RecursiveDescriptorExpansion,
    ContractVersionConflict,
    LifecycleProtocolRequired,
    LifecycleProtocolIncomplete,
    UndeclaredLifecycleTraffic,
    OrdinaryPreReleaseTraffic,
    ReadyInputUnavailable,
    CircularReadyWait,
    RegionCrossingRequired,
    RegionCrossingConflict,
}

public sealed record ActivationGroupFailure(
    ActivationGroupFailureKind Kind,
    ActivationGroupId? Group,
    OccurrenceId? Member,
    ActivationEdgeId? Edge,
    OccurrenceId? Source,
    OccurrenceId? Target,
    ContractId? Contract,
    VersionLiteral? Version,
    LifecycleProtocolId? Protocol,
    RegionId? Region,
    PortId? Port,
    string Reason);

public sealed record WiderActivationGroupProposal(
    GenerationId Generation,
    RestartScopeId RestartScope,
    ActivationEdgeId Edge,
    RegionId FromRegion,
    RegionId ToRegion,
    PortId Port,
    string Reason);

public sealed record ActivationGroupOutcome
{
    private ActivationGroupOutcome(
        ActivationGroupPlan? plan,
        WiderActivationGroupProposal? wider,
        ActivationGroupFailure? failure)
    {
        Plan = plan;
        Wider = wider;
        Failure = failure;
    }

    public ActivationGroupPlan? Plan { get; }
    public WiderActivationGroupProposal? Wider { get; }
    public ActivationGroupFailure? Failure { get; }
    public Cm3EffectObservation Effects => Cm3EffectObservation.None;
    public bool IsPlanned => Plan is not null;

    internal static ActivationGroupOutcome Planned(ActivationGroupPlan plan) => new(plan, null, null);
    internal static ActivationGroupOutcome WiderRequired(WiderActivationGroupProposal wider) => new(null, wider, null);
    internal static ActivationGroupOutcome Refused(ActivationGroupFailure failure) => new(null, null, failure);
}

/// <summary>
/// Effect-free CM3 planner. It validates finite activation groups and their declared lifecycle
/// protocol, but does not execute any activation stage or open either lifecycle gate.
/// </summary>
public sealed class FakeActivationGroupPlanner
{
    public ActivationGroupOutcome Plan(ActivationGroupRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var request = Snapshot(input);

        var duplicateMember = Duplicate(request.Members, item => item.Occurrence);
        if (duplicateMember is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.ContradictoryIdentity,
                $"occurrence '{duplicateMember.Value}' has contradictory duplicate member declarations",
                member: duplicateMember.Value);
        }

        var duplicateEdge = Duplicate(request.Edges, item => item.Edge);
        if (duplicateEdge is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.ContradictoryIdentity,
                $"edge '{duplicateEdge.Value}' has contradictory duplicate declarations",
                edge: duplicateEdge.Value);
        }

        var duplicateProtocol = Duplicate(request.Protocols, item => item.Protocol);
        if (duplicateProtocol is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.ContradictoryIdentity,
                $"protocol '{duplicateProtocol.Value}' has contradictory duplicate declarations",
                protocol: duplicateProtocol.Value);
        }

        var duplicateProtocolEdge = Duplicate(request.Protocols, item => item.Edge);
        if (duplicateProtocolEdge is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.ContradictoryIdentity,
                $"edge '{duplicateProtocolEdge.Value}' has contradictory duplicate lifecycle protocols",
                edge: duplicateProtocolEdge.Value);
        }

        var duplicateCrossing = Duplicate(request.RegionCrossings, item => item.Edge);
        if (duplicateCrossing is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.ContradictoryIdentity,
                $"edge '{duplicateCrossing.Value}' has contradictory duplicate Region crossings",
                edge: duplicateCrossing.Value);
        }

        var members = request.Members.ToDictionary(item => item.Occurrence);
        foreach (var edge in request.Edges.OrderBy(item => item.Edge.Value, StringComparer.Ordinal))
        {
            if (!members.TryGetValue(edge.From, out var source))
            {
                return Refuse(
                    ActivationGroupFailureKind.MissingMember,
                    $"edge '{edge.Edge}' names missing source occurrence '{edge.From}'",
                    member: edge.From,
                    edge: edge.Edge);
            }

            if (!members.TryGetValue(edge.To, out var target))
            {
                return Refuse(
                    ActivationGroupFailureKind.MissingMember,
                    $"edge '{edge.Edge}' names missing target occurrence '{edge.To}'",
                    member: edge.To,
                    edge: edge.Edge);
            }

            if (edge.Kind is not ActivationDependencyKind.DescriptorExpansion
                && target.Provides.Count(item =>
                    item.Contract == edge.Contract && item.Version == edge.Version) != 1)
            {
                return Refuse(
                    ActivationGroupFailureKind.ContractVersionConflict,
                    $"edge '{edge.Edge}' requires '{edge.Contract}' version '{edge.Version}' that '{edge.To}' does not provide",
                    member: edge.To,
                    edge: edge.Edge,
                    source: edge.From,
                    target: edge.To,
                    contract: edge.Contract,
                    version: edge.Version);
            }

            if (edge.Kind == ActivationDependencyKind.OrdinaryInteraction && edge.ObservedBeforeRelease)
            {
                return Refuse(
                    ActivationGroupFailureKind.OrdinaryPreReleaseTraffic,
                    $"ordinary edge '{edge.Edge}' was observed before Release",
                    edge: edge.Edge);
            }

            if (edge.Kind != ActivationDependencyKind.RelationalInitialisation && edge.Protocol is not null)
            {
                return Refuse(
                    ActivationGroupFailureKind.UndeclaredLifecycleTraffic,
                    $"non-lifecycle edge '{edge.Edge}' names lifecycle protocol '{edge.Protocol}'",
                    edge: edge.Edge,
                    protocol: edge.Protocol);
            }

            var crossing = ValidateCrossing(request, edge, source, target);
            if (crossing is not null)
            {
                return crossing;
            }
        }

        var unreferencedProtocol = request.Protocols
            .Where(protocol => !request.Edges.Any(edge =>
                edge.Kind == ActivationDependencyKind.RelationalInitialisation
                && (edge.Protocol == protocol.Protocol || edge.Edge == protocol.Edge)))
            .OrderBy(item => item.Protocol.Value, StringComparer.Ordinal)
            .FirstOrDefault();
        if (unreferencedProtocol is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.UndeclaredLifecycleTraffic,
                $"lifecycle protocol '{unreferencedProtocol.Protocol}' is not declared by a relational edge",
                edge: unreferencedProtocol.Edge,
                protocol: unreferencedProtocol.Protocol);
        }

        foreach (var member in request.Members.OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal))
        {
            var missingInput = member.RequiredReadyInputs
                .Except(member.AvailableReadyInputs)
                .OrderBy(item => item.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (missingInput != default)
            {
                return Refuse(
                    ActivationGroupFailureKind.ReadyInputUnavailable,
                    $"member '{member.Occurrence}' cannot reach Ready because input '{missingInput}' is unavailable",
                    member: member.Occurrence);
            }

            var unknownWait = member.WaitsForReadyOf
                .Where(item => !members.ContainsKey(item))
                .OrderBy(item => item.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            if (unknownWait != default)
            {
                return Refuse(
                    ActivationGroupFailureKind.MissingMember,
                    $"member '{member.Occurrence}' waits for missing occurrence '{unknownWait}'",
                    member: unknownWait);
            }
        }

        var readyCycle = FindCycle(
            request.Members.Select(item => item.Occurrence),
            request.Members.SelectMany(item => item.WaitsForReadyOf.Select(wait => (item.Occurrence, wait))));
        if (readyCycle is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.CircularReadyWait,
                $"Ready wait cycle passes through '{readyCycle.Value}'",
                member: readyCycle.Value);
        }

        var components = StronglyConnectedComponents(request.Members, request.Edges);
        var groupForMember = new Dictionary<OccurrenceId, ActivationGroupId>();
        var groups = new List<ActivationGroupObservation>();
        var decisions = new List<ActivationGroupDecision>();

        foreach (var component in components)
        {
            var orderedMembers = component
                .Select(item => members[item])
                .OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal)
                .ToArray();
            var groupId = ActivationGroupId.Create($"group.{orderedMembers[0].Occurrence.Value}");
            foreach (var member in orderedMembers)
            {
                groupForMember[member.Occurrence] = groupId;
                decisions.Add(new(groupId, member.Occurrence, null, "member-grouped", $"member belongs to {groupId}"));
            }

            var memberIds = component.ToHashSet();
            var internalEdges = request.Edges
                .Where(item => memberIds.Contains(item.From) && memberIds.Contains(item.To))
                .OrderBy(item => item.Edge.Value, StringComparer.Ordinal)
                .ToArray();
            var cyclic = orderedMembers.Length > 1 || internalEdges.Any(item => item.From == item.To);
            var expansion = cyclic
                ? internalEdges.FirstOrDefault(item => item.Kind == ActivationDependencyKind.DescriptorExpansion)
                : null;
            if (expansion is not null)
            {
                return Refuse(
                    ActivationGroupFailureKind.RecursiveDescriptorExpansion,
                    $"cyclic group '{groupId}' contains descriptor-expansion edge '{expansion.Edge}'",
                    groupId,
                    edge: expansion.Edge);
            }

            var groupProtocols = new List<LifecycleProtocolDeclaration>();
            foreach (var edge in internalEdges.Where(item => item.Kind == ActivationDependencyKind.RelationalInitialisation))
            {
                var validation = ValidateProtocol(request, groupId, edge);
                if (validation.Outcome is not null)
                {
                    return validation.Outcome;
                }

                groupProtocols.Add(validation.Protocol!);
                decisions.Add(new(groupId, null, edge.Edge, "relational-protocol", $"bounded protocol {validation.Protocol!.Protocol} accepted"));
            }

            var crossings = request.RegionCrossings
                .Where(item => internalEdges.Any(edge => edge.Edge == item.Edge))
                .OrderBy(item => item.Edge.Value, StringComparer.Ordinal)
                .ToArray();
            var stages = new List<ActivationStageObservation>
            {
                new(ActivationStage.LocalInitialisation, false),
                new(ActivationStage.Interconnection, false),
            };
            if (groupProtocols.Count > 0)
            {
                stages.Add(new(ActivationStage.RelationalInitialisation, false));
            }

            stages.Add(new(ActivationStage.Ready, false));
            groups.Add(
                new(
                    groupId,
                    cyclic,
                    ReadOnly(orderedMembers.Select(ReadOnlyMember)),
                    ReadOnly(internalEdges),
                    ReadOnly(groupProtocols.OrderBy(item => item.Protocol.Value, StringComparer.Ordinal)),
                    ReadOnly(crossings),
                    ReadOnly(stages),
                    true));

            foreach (var edge in internalEdges)
            {
                decisions.Add(new(groupId, null, edge.Edge, "internal-edge", cyclic ? "closed inside one activation group" : "self-contained dependency"));
            }
        }

        var crossGroupLifecycle = request.Edges
            .Where(item =>
                item.Kind == ActivationDependencyKind.RelationalInitialisation
                && groupForMember[item.From] != groupForMember[item.To])
            .OrderBy(item => item.Edge.Value, StringComparer.Ordinal)
            .FirstOrDefault();
        if (crossGroupLifecycle is not null)
        {
            return Refuse(
                ActivationGroupFailureKind.UndeclaredLifecycleTraffic,
                $"relational edge '{crossGroupLifecycle.Edge}' crosses activation-group boundaries",
                edge: crossGroupLifecycle.Edge,
                protocol: crossGroupLifecycle.Protocol);
        }

        var interGroupEdges = request.Edges
            .Where(item => groupForMember[item.From] != groupForMember[item.To])
            .Select(item => new InterGroupEdgeObservation(item.Edge, groupForMember[item.From], groupForMember[item.To]))
            .OrderBy(item => item.Edge.Value, StringComparer.Ordinal)
            .ToArray();
        var orderedGroups = DependencyFirst(groups, interGroupEdges);
        foreach (var edge in interGroupEdges)
        {
            decisions.Add(new(edge.FromGroup, null, edge.Edge, "inter-group-edge", $"{edge.FromGroup} depends on {edge.ToGroup}"));
        }

        var plan = new ActivationGroupPlan(
            request.Generation,
            request.RestartScope,
            ReadOnly(orderedGroups),
            ReadOnly(interGroupEdges),
            ReadOnly(request.RegionCrossings.OrderBy(item => item.Edge.Value, StringComparer.Ordinal)),
            ReadOnly(decisions
                .OrderBy(item => item.Group?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Member?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Edge?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Kind, StringComparer.Ordinal)),
            Cm3EffectObservation.None);
        return ActivationGroupOutcome.Planned(plan);
    }

    private static ActivationGroupOutcome? ValidateCrossing(
        ActivationGroupRequest request,
        ActivationDependency edge,
        ActivationGroupMember source,
        ActivationGroupMember target)
    {
        var declarations = request.RegionCrossings.Where(item => item.Edge == edge.Edge).ToArray();
        if (source.Region == target.Region)
        {
            return declarations.Length == 0
                ? null
                : Refuse(
                    ActivationGroupFailureKind.RegionCrossingConflict,
                    $"same-Region edge '{edge.Edge}' carries a Region-crossing declaration",
                    edge: edge.Edge,
                    region: source.Region,
                    port: edge.CrossingPort);
        }

        if (declarations.Length == 0)
        {
            if (edge.AllowWiderRegionProposal && edge.CrossingPort is PortId port)
            {
                return ActivationGroupOutcome.WiderRequired(
                    new(
                        request.Generation,
                        request.RestartScope,
                        edge.Edge,
                        source.Region,
                        target.Region,
                        port,
                        "cross-Region dependency requires a wider parent generation"));
            }

            return Refuse(
                ActivationGroupFailureKind.RegionCrossingRequired,
                $"cross-Region edge '{edge.Edge}' has no declared Port crossing",
                edge: edge.Edge,
                region: target.Region,
                port: edge.CrossingPort);
        }

        var crossing = declarations[0];
        if (edge.CrossingPort is null
            || crossing.Port != edge.CrossingPort
            || crossing.FromRegion != source.Region
            || crossing.ToRegion != target.Region
            || !crossing.ImportDeclared
            || !crossing.ExportDeclared)
        {
            return Refuse(
                ActivationGroupFailureKind.RegionCrossingConflict,
                $"Region crossing for edge '{edge.Edge}' does not match its Port, Regions, import, and export declarations",
                edge: edge.Edge,
                region: target.Region,
                port: crossing.Port);
        }

        return null;
    }

    private static (LifecycleProtocolDeclaration? Protocol, ActivationGroupOutcome? Outcome) ValidateProtocol(
        ActivationGroupRequest request,
        ActivationGroupId group,
        ActivationDependency edge)
    {
        if (edge.Protocol is null)
        {
            return (
                null,
                Refuse(
                    ActivationGroupFailureKind.LifecycleProtocolRequired,
                    $"relational edge '{edge.Edge}' has no lifecycle protocol",
                    group,
                    edge: edge.Edge));
        }

        var matches = request.Protocols.Where(item => item.Protocol == edge.Protocol).ToArray();
        if (matches.Length != 1)
        {
            return (
                null,
                Refuse(
                    ActivationGroupFailureKind.LifecycleProtocolRequired,
                    $"relational edge '{edge.Edge}' has no unique lifecycle protocol '{edge.Protocol}'",
                    group,
                    edge: edge.Edge,
                    protocol: edge.Protocol));
        }

        var protocol = matches[0];
        var complete = protocol.Edge == edge.Edge
            && protocol.From == edge.From
            && protocol.To == edge.To
            && !string.IsNullOrEmpty(protocol.Operation.Value)
            && protocol.Authority.Count > 0
            && !string.IsNullOrWhiteSpace(protocol.Ordering)
            && protocol.TimeoutMilliseconds > 0
            && protocol.RetryLimit >= 0
            && !string.IsNullOrWhiteSpace(protocol.Completion)
            && !string.IsNullOrWhiteSpace(protocol.Failure)
            && !string.IsNullOrWhiteSpace(protocol.Rollback);
        return complete
            ? (ReadOnlyProtocol(protocol), null)
            : (
                null,
                Refuse(
                    ActivationGroupFailureKind.LifecycleProtocolIncomplete,
                    $"lifecycle protocol '{protocol.Protocol}' is incomplete or misdirected",
                    group,
                    edge: edge.Edge,
                    protocol: protocol.Protocol));
    }

    private static IReadOnlyList<ActivationGroupObservation> DependencyFirst(
        IReadOnlyList<ActivationGroupObservation> groups,
        IReadOnlyList<InterGroupEdgeObservation> edges)
    {
        var remaining = groups.ToDictionary(item => item.Group);
        var result = new List<ActivationGroupObservation>();
        while (remaining.Count > 0)
        {
            var ready = remaining.Values
                .Where(group => !edges.Any(edge =>
                    edge.FromGroup == group.Group && remaining.ContainsKey(edge.ToGroup)))
                .OrderBy(item => item.Group.Value, StringComparer.Ordinal)
                .ToArray();
            foreach (var group in ready)
            {
                result.Add(group);
                remaining.Remove(group.Group);
            }
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<OccurrenceId>> StronglyConnectedComponents(
        IReadOnlyList<ActivationGroupMember> members,
        IReadOnlyList<ActivationDependency> edges)
    {
        var adjacency = members.ToDictionary(
            item => item.Occurrence,
            item => edges
                .Where(edge => edge.From == item.Occurrence)
                .Select(edge => edge.To)
                .Distinct()
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
        var reverse = members.ToDictionary(
            item => item.Occurrence,
            item => edges
                .Where(edge => edge.To == item.Occurrence)
                .Select(edge => edge.From)
                .Distinct()
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
        var visited = new HashSet<OccurrenceId>();
        var order = new List<OccurrenceId>();
        foreach (var member in members.OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal))
        {
            Visit(member.Occurrence);
        }

        visited.Clear();
        var result = new List<IReadOnlyList<OccurrenceId>>();
        foreach (var member in order.AsEnumerable().Reverse())
        {
            if (visited.Contains(member))
            {
                continue;
            }

            var component = new List<OccurrenceId>();
            Collect(member, component);
            result.Add(component.OrderBy(item => item.Value, StringComparer.Ordinal).ToArray());
        }

        return result;

        void Visit(OccurrenceId current)
        {
            if (!visited.Add(current))
            {
                return;
            }

            foreach (var next in adjacency[current])
            {
                Visit(next);
            }

            order.Add(current);
        }

        void Collect(OccurrenceId current, ICollection<OccurrenceId> component)
        {
            if (!visited.Add(current))
            {
                return;
            }

            component.Add(current);
            foreach (var next in reverse[current])
            {
                Collect(next, component);
            }
        }
    }

    private static OccurrenceId? FindCycle(
        IEnumerable<OccurrenceId> members,
        IEnumerable<(OccurrenceId From, OccurrenceId To)> edges)
    {
        var adjacency = edges
            .GroupBy(item => item.From)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.To).Distinct().OrderBy(item => item.Value, StringComparer.Ordinal).ToArray());
        var visited = new HashSet<OccurrenceId>();
        var visiting = new HashSet<OccurrenceId>();
        foreach (var member in members.OrderBy(item => item.Value, StringComparer.Ordinal))
        {
            var cycle = Visit(member);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;

        OccurrenceId? Visit(OccurrenceId current)
        {
            if (visiting.Contains(current))
            {
                return current;
            }

            if (!visited.Add(current))
            {
                return null;
            }

            visiting.Add(current);
            if (adjacency.TryGetValue(current, out var next))
            {
                foreach (var target in next)
                {
                    var cycle = Visit(target);
                    if (cycle is not null)
                    {
                        return cycle;
                    }
                }
            }

            visiting.Remove(current);
            return null;
        }
    }

    private static TKey? Duplicate<T, TKey>(IEnumerable<T> values, Func<T, TKey> key)
        where TKey : struct =>
        values.GroupBy(key).Where(group => group.Count() > 1).Select(group => (TKey?)group.Key).FirstOrDefault();

    private static ActivationGroupMember ReadOnlyMember(ActivationGroupMember member) =>
        member with
        {
            Provides = ReadOnly(member.Provides),
            RequiredReadyInputs = ReadOnly(member.RequiredReadyInputs),
            AvailableReadyInputs = ReadOnly(member.AvailableReadyInputs),
            WaitsForReadyOf = ReadOnly(member.WaitsForReadyOf),
        };

    private static LifecycleProtocolDeclaration ReadOnlyProtocol(LifecycleProtocolDeclaration protocol) =>
        protocol with { Authority = ReadOnly(protocol.Authority) };

    private static ReadOnlyCollection<T> ReadOnly<T>(IEnumerable<T> values) =>
        Array.AsReadOnly(values.ToArray());

    private static ActivationGroupRequest Snapshot(ActivationGroupRequest request) =>
        request with
        {
            Members = request.Members.Select(ReadOnlyMember).ToArray(),
            Edges = request.Edges.ToArray(),
            Protocols = request.Protocols.Select(ReadOnlyProtocol).ToArray(),
            RegionCrossings = request.RegionCrossings.ToArray(),
        };

    private static ActivationGroupOutcome Refuse(
        ActivationGroupFailureKind kind,
        string reason,
        ActivationGroupId? group = null,
        OccurrenceId? member = null,
        ActivationEdgeId? edge = null,
        OccurrenceId? source = null,
        OccurrenceId? target = null,
        ContractId? contract = null,
        VersionLiteral? version = null,
        LifecycleProtocolId? protocol = null,
        RegionId? region = null,
        PortId? port = null) =>
        ActivationGroupOutcome.Refused(
            new(kind, group, member, edge, source, target, contract, version, protocol, region, port, reason));
}
