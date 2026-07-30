using System.Collections.ObjectModel;

namespace Brontide.Reference.Experimental.ComponentManagement;

public enum ProviderExposure
{
    Distinct,
    Mediated,
}

public enum MediationKind
{
    Selection,
    Distribution,
    Aggregation,
    Arbitration,
    DomainSpecific,
}

public enum MediationRealization
{
    StaticHost,
    DedicatedComponent,
}

public enum PortLifecycleMode
{
    Sealed,
    ActivationOpen,
    RuntimeOpen,
}

public enum CandidatePolicyDomain
{
    Trust,
    Origin,
    Platform,
    Authority,
    Resource,
    LocalPolicy,
}

public enum TopologyPolicyDisposition
{
    Accepted,
    Refined,
    Rejected,
}

public sealed record Cm2EffectObservation(
    bool SelectionMutated,
    bool Prepared,
    bool Activated,
    bool ActorEstablished,
    bool CapabilityGranted,
    bool ActiveGenerationMutated)
{
    public static Cm2EffectObservation None { get; } = new(false, false, false, false, false, false);
}

public sealed record SharingDeclaration(
    bool IsolationCompatible,
    bool LifecycleCompatible,
    bool AuthorityCompatible);

public sealed record CandidatePolicyObservation(
    CandidatePolicyDomain Domain,
    bool Accepted,
    string Reason);

public sealed record ResolutionCandidate(
    DefinitionId Definition,
    SourceId Source,
    PublisherId Publisher,
    PackageId Package,
    IReadOnlyList<ProvidedContract> Provides,
    bool Generic,
    SharingDeclaration Sharing,
    IReadOnlyList<CandidatePolicyObservation> Policy,
    IReadOnlyList<EvidenceId> Evidence,
    IReadOnlyList<string> Authority,
    string FailureDomain,
    TopologyNodeId? AttachmentNode);

public sealed record MediationDeclaration(
    MediationId Mediation,
    MediationKind Kind,
    MediationRealization Realization,
    DefinitionId? Component,
    bool OwnsMutableMembership,
    bool OwnsResidue,
    bool OwnsBackpressure,
    bool OwnsAuthority,
    bool OwnsRecovery,
    bool OwnsLifecycle);

public sealed record ResolutionRequirement(
    RequirementId Requirement,
    ContractId Contract,
    VersionLiteral Version,
    BindingScopeId Scope,
    Cardinality Cardinality,
    bool AllowSharing,
    ProviderExposure Exposure,
    MediationDeclaration? Mediation,
    IReadOnlyList<DefinitionConstraint> Constraints,
    RegionId? ContainingRegion,
    PortId? ContainingPort,
    bool RuntimeAttachment,
    IReadOnlyList<string> RequiredImports,
    IReadOnlyList<string> RequiredExports,
    string? RequiredFailurePolicy,
    string? RequiredRollbackBoundary,
    IReadOnlyList<string> RequestedAuthority,
    IReadOnlyList<TopologyRelation> TopologyRequirements);

public sealed record CompositionParameterDeclaration(
    ParameterId Parameter,
    IReadOnlyList<DefinitionId> AllowedDefinitions,
    bool Required);

public sealed record ActivationParameterDeclaration(
    ParameterId Parameter,
    bool Required,
    string? DefaultValue);

public sealed record ResolutionDefinition(
    DefinitionId Definition,
    PublisherId Publisher,
    IReadOnlyList<ProvidedContract> Provides,
    IReadOnlyList<ResolutionRequirement> Requirements,
    IReadOnlyList<CompositionParameterDeclaration> CompositionParameters,
    IReadOnlyList<ActivationParameterDeclaration> ActivationParameters,
    IReadOnlyList<string> RequestedAuthority);

public sealed record CompositionParameterSelection(
    DefinitionId Owner,
    ParameterId Parameter,
    DefinitionId SelectedDefinition);

public sealed record ActivationParameterValue(ParameterId Parameter, string Value);

public sealed record ProviderPreselection(RequirementId Requirement, DefinitionId Definition);

public sealed record PortEnvelope(
    RegionId Region,
    PortId Port,
    PortLifecycleMode Lifecycle,
    IReadOnlyList<ProvidedContract> Contracts,
    Cardinality Cardinality,
    IReadOnlyList<string> Imports,
    IReadOnlyList<string> Exports,
    IReadOnlyList<string> AuthorityCeiling,
    IReadOnlyList<TopologyRelation> TopologyRequirements,
    string FailurePolicy,
    string RollbackBoundary,
    bool AllowWiderGenerationProposal);

public sealed record TopologyPolicyInput(
    ClaimId Claim,
    ObserverId AssertedBy,
    TopologyRelation Relation,
    TopologyNodeId From,
    TopologyNodeId To,
    TopologyPolicyDisposition Disposition,
    TopologyRelation? RefinedRelation,
    string Reason);

public sealed record ResolutionRequest(
    ResolutionRequestId Request,
    GenerationId Generation,
    GenerationId? ActiveGeneration,
    RestartScopeId RestartScope,
    IReadOnlyList<DefinitionId> Roots,
    IReadOnlyList<ResolutionDefinition> Definitions,
    IReadOnlyList<ResolutionCandidate> Candidates,
    IReadOnlyList<ActivatedOccurrenceEntry> ExistingOccurrences,
    IReadOnlyList<OccupiedBindingEntry> OccupiedBindings,
    IReadOnlyList<PreferenceEntry> Preferences,
    IReadOnlyList<BindingId> AuthorisedReplacements,
    IReadOnlyList<CompositionParameterSelection> CompositionParameters,
    IReadOnlyList<ActivationParameterValue> ActivationParameters,
    IReadOnlyList<ProviderPreselection> PreselectedProviders,
    IReadOnlyList<PortEnvelope> Ports,
    IReadOnlyList<TopologyPolicyInput> TopologyClaims);

public sealed record ProviderSetMember(
    DefinitionId Definition,
    OccurrenceId Occurrence,
    SourceId? Source,
    PublisherId Publisher,
    PackageId? Package,
    bool Retained,
    IReadOnlyList<EvidenceId> Evidence,
    IReadOnlyList<string> Authority,
    string FailureDomain,
    TopologyNodeId? AttachmentNode);

public sealed record CandidateAlternative(
    DefinitionId Definition,
    SourceId Source,
    PublisherId Publisher,
    PackageId Package,
    int Rank,
    bool Admissible,
    IReadOnlyList<string> ExclusionReasons);

public sealed record BindingPlanObservation(
    RequirementId Requirement,
    OccurrenceId Member,
    bool Direct,
    MediationId? Mediation);

public sealed record ProviderSetObservation(
    RequirementId Requirement,
    DefinitionId Requester,
    ContractId Contract,
    VersionLiteral Version,
    BindingScopeId Scope,
    Cardinality Cardinality,
    ProviderExposure Exposure,
    IReadOnlyList<ProviderSetMember> Members,
    int OptionalPositionsUnfilled,
    MediationDeclaration? Mediation,
    IReadOnlyList<BindingPlanObservation> BindingPlans,
    RegionId? ContainingRegion,
    PortId? ContainingPort,
    IReadOnlyList<CandidateAlternative> Alternatives);

public sealed record PreferenceObservation(
    PreferenceId Preference,
    DefinitionId Requester,
    DefinitionId PreferredDefinition,
    RequirementId Requirement,
    bool Used,
    string Reason);

public sealed record CandidateExclusion(
    RequirementId Requirement,
    DefinitionId Definition,
    SourceId Source,
    CandidatePolicyDomain Domain,
    string Reason);

public sealed record ResolutionConflict(
    RequirementId Requirement,
    string Kind,
    string Reason);

public sealed record ResolutionDecision(
    RequirementId? Requirement,
    DefinitionId? Definition,
    string Kind,
    string Reason);

public sealed record EffectiveParameter(
    DefinitionId Definition,
    ParameterId Parameter,
    string Value,
    string Provenance);

public sealed record DefinitionAuthorityObservation(
    DefinitionId Definition,
    IReadOnlyList<string> RequestedAuthority);

public sealed record TopologyDecision(
    ClaimId Claim,
    ObserverId AssertedBy,
    TopologyRelation ClaimedRelation,
    TopologyRelation? EffectiveRelation,
    TopologyNodeId From,
    TopologyNodeId To,
    TopologyPolicyDisposition Disposition,
    string Reason);

public sealed record ProposedStack(
    GenerationId Generation,
    GenerationId? RetainedActiveGeneration,
    RestartScopeId RestartScope,
    IReadOnlyList<DefinitionId> Roots,
    IReadOnlyList<DefinitionId> Definitions,
    IReadOnlyList<ProviderSetObservation> ProviderSets,
    IReadOnlyList<PreferenceObservation> Preferences,
    IReadOnlyList<CandidateExclusion> Exclusions,
    IReadOnlyList<ResolutionConflict> Conflicts,
    IReadOnlyList<EffectiveParameter> Parameters,
    IReadOnlyList<ActivationParameterValue> UnusedActivationParameters,
    IReadOnlyList<PortEnvelope> Ports,
    IReadOnlyList<DefinitionAuthorityObservation> RequestedAuthority,
    IReadOnlyList<TopologyDecision> Topology,
    IReadOnlyList<ResolutionDecision> Decisions);

public sealed record ResolvedGeneration(
    GenerationId Generation,
    RestartScopeId RestartScope,
    IReadOnlyList<DefinitionId> Definitions,
    IReadOnlyList<ProviderSetObservation> ProviderSets,
    IReadOnlyList<EffectiveParameter> Parameters,
    IReadOnlyList<PortEnvelope> Ports,
    IReadOnlyList<DefinitionAuthorityObservation> RequestedAuthority,
    IReadOnlyList<TopologyDecision> Topology,
    Cm2EffectObservation Effects);

public enum ResolutionFailureKind
{
    MissingDefinition,
    MissingDependency,
    IncompatibleContract,
    UnsupportedConstraint,
    UnboundedRequiredCardinality,
    ContradictoryIdentity,
    CycleRequiresCm3,
    AmbiguousSelection,
    MediationRequired,
    MediationRequiresComponent,
    PortUnavailable,
    PortEnvelopeExceeded,
    ActivationParameterUnavailable,
}

public sealed record ResolutionFailure(
    ResolutionFailureKind Kind,
    DefinitionId? Definition,
    RequirementId? Requirement,
    RegionId? Region,
    PortId? Port,
    ParameterId? Parameter,
    string Reason);

public sealed record WiderGenerationProposal(
    RegionId Region,
    PortId Port,
    RequirementId Requirement,
    string Reason);

public sealed record ResolutionOutcome
{
    private ResolutionOutcome(
        ProposedStack? proposed,
        ResolvedGeneration? generation,
        WiderGenerationProposal? widerGeneration,
        ResolutionFailure? failure)
    {
        Proposed = proposed;
        Generation = generation;
        WiderGeneration = widerGeneration;
        Failure = failure;
    }

    public ProposedStack? Proposed { get; }
    public ResolvedGeneration? Generation { get; }
    public WiderGenerationProposal? WiderGeneration { get; }
    public ResolutionFailure? Failure { get; }
    public Cm2EffectObservation Effects => Cm2EffectObservation.None;
    public bool IsResolved => Generation is not null;

    internal static ResolutionOutcome Resolved(ProposedStack proposed, ResolvedGeneration generation) =>
        new(proposed, generation, null, null);

    internal static ResolutionOutcome Wider(WiderGenerationProposal proposal) =>
        new(null, null, proposal, null);

    internal static ResolutionOutcome Refused(ResolutionFailure failure) =>
        new(null, null, null, failure);
}

/// <summary>
/// Deterministic fake CM2 resolver. All policy, environment, topology, and source observations are
/// explicit request data; the resolver has no clock, service lookup, active host, or authority API.
/// </summary>
public sealed class FakeGenerationResolver
{
    public ResolutionOutcome Resolve(ResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = Snapshot(request);
        var duplicateDefinition = snapshot.Definitions
            .GroupBy(item => item.Definition)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDefinition is not null)
        {
            return Refuse(
                ResolutionFailureKind.ContradictoryIdentity,
                duplicateDefinition.Key,
                null,
                $"definition '{duplicateDefinition.Key}' has contradictory duplicate declarations");
        }

        var duplicateCandidate = snapshot.Candidates
            .GroupBy(item => (item.Definition, item.Source))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCandidate is not null)
        {
            return Refuse(
                ResolutionFailureKind.ContradictoryIdentity,
                duplicateCandidate.Key.Definition,
                null,
                $"candidate '{duplicateCandidate.Key.Definition}' from '{duplicateCandidate.Key.Source}' has contradictory duplicate observations");
        }

        var definitions = snapshot.Definitions.ToDictionary(item => item.Definition);
        var included = new HashSet<DefinitionId>();
        var pending = new Queue<DefinitionId>(snapshot.Roots.OrderBy(item => item.Value, StringComparer.Ordinal));
        var edges = new HashSet<(DefinitionId From, DefinitionId To)>();
        var providerSets = new List<ProviderSetObservation>();
        var exclusions = new List<CandidateExclusion>();
        var conflicts = new List<ResolutionConflict>();
        var decisions = new List<ResolutionDecision>();
        var preferenceObservations = new List<PreferenceObservation>();
        var occurrenceCounters = new Dictionary<DefinitionId, int>();
        var sharedOccurrences = new Dictionary<(DefinitionId, BindingScopeId), OccurrenceId>();
        var processedRequirements = new HashSet<RequirementId>();

        while (pending.Count > 0)
        {
            var definitionId = pending.Dequeue();
            if (!definitions.TryGetValue(definitionId, out var definition))
            {
                return Refuse(
                    ResolutionFailureKind.MissingDefinition,
                    definitionId,
                    null,
                    $"definition '{definitionId}' is not declared");
            }

            if (!included.Add(definitionId))
            {
                continue;
            }

            foreach (var parameter in definition.CompositionParameters.OrderBy(item => item.Parameter.Value, StringComparer.Ordinal))
            {
                var matches = snapshot.CompositionParameters
                    .Where(item => item.Owner == definitionId && item.Parameter == parameter.Parameter)
                    .ToArray();
                if (matches.Length > 1)
                {
                    return Refuse(
                        ResolutionFailureKind.AmbiguousSelection,
                        definitionId,
                        null,
                        $"Composition Parameter '{parameter.Parameter}' has several selections");
                }

                if (matches.Length == 0)
                {
                    if (parameter.Required)
                    {
                        return Refuse(
                            ResolutionFailureKind.MissingDefinition,
                            definitionId,
                            null,
                            $"required Composition Parameter '{parameter.Parameter}' has no selection");
                    }

                    continue;
                }

                var selected = matches[0].SelectedDefinition;
                if (!parameter.AllowedDefinitions.Contains(selected))
                {
                    return Refuse(
                        ResolutionFailureKind.IncompatibleContract,
                        definitionId,
                        null,
                        $"Composition Parameter '{parameter.Parameter}' cannot select '{selected}'");
                }

                edges.Add((definitionId, selected));
                pending.Enqueue(selected);
                decisions.Add(new(null, selected, "composition-parameter", $"{parameter.Parameter} selected {selected}"));
            }

            foreach (var requirement in definition.Requirements.OrderBy(item => item.Requirement.Value, StringComparer.Ordinal))
            {
                if (!processedRequirements.Add(requirement.Requirement))
                {
                    return Refuse(
                        ResolutionFailureKind.ContradictoryIdentity,
                        definitionId,
                        requirement.Requirement,
                        $"requirement '{requirement.Requirement}' is declared more than once");
                }

                var unsupported = requirement.Constraints.FirstOrDefault(item =>
                    item.Name is not ("platform" or "trust" or "origin" or "authority" or "resource" or "local-policy"));
                if (unsupported is not null)
                {
                    return Refuse(
                        ResolutionFailureKind.UnsupportedConstraint,
                        definitionId,
                        requirement.Requirement,
                        $"Constraint '{unsupported.Name}' has no CM2 evaluator");
                }

                if (requirement.Cardinality.Maximum is null && requirement.Cardinality.Minimum > 0)
                {
                    return Refuse(
                        ResolutionFailureKind.UnboundedRequiredCardinality,
                        definitionId,
                        requirement.Requirement,
                        $"required Provider Set '{requirement.Requirement}' has no finite maximum");
                }

                var portResult = ValidatePort(snapshot, definitionId, requirement);
                if (portResult is not null)
                {
                    return portResult;
                }

                var members = new List<ProviderSetMember>();
                var matchingOccupied = snapshot.OccupiedBindings
                    .Where(item => item.Scope == requirement.Scope && item.Contract == requirement.Contract)
                    .OrderBy(item => item.Binding.Value, StringComparer.Ordinal)
                    .ToArray();
                if (matchingOccupied.Length > 1 && requirement.Cardinality == Cardinality.Parse("1..1"))
                {
                    return Refuse(
                        ResolutionFailureKind.AmbiguousSelection,
                        definitionId,
                        requirement.Requirement,
                        "several occupied bindings claim one 1..1 role");
                }

                if (matchingOccupied.Length == 1
                    && requirement.Cardinality == Cardinality.Parse("1..1")
                    && !snapshot.AuthorisedReplacements.Contains(matchingOccupied[0].Binding))
                {
                    var occupied = matchingOccupied[0];
                    var occupiedDefinition = definitions.GetValueOrDefault(occupied.OccupantDefinition);
                    var compatible = occupiedDefinition?.Provides.Any(item =>
                        item.Contract == requirement.Contract && item.Version == requirement.Version) == true;
                    if (compatible)
                    {
                        var existingOccurrences = snapshot.ExistingOccurrences.Where(item =>
                            item.Occurrence == occupied.OccupantOccurrence
                            && item.Definition == occupied.OccupantDefinition).ToArray();
                        if (existingOccurrences.Length != 1)
                        {
                            return Refuse(
                                ResolutionFailureKind.ContradictoryIdentity,
                                occupied.OccupantDefinition,
                                requirement.Requirement,
                                $"occupied binding '{occupied.Binding}' has no matching retained occurrence or has contradictory duplicates");
                        }

                        members.Add(
                            new(
                                occupied.OccupantDefinition,
                                occupied.OccupantOccurrence,
                                null,
                                occupiedDefinition!.Publisher,
                                null,
                                true,
                                Array.Empty<EvidenceId>(),
                                occupiedDefinition.RequestedAuthority.ToArray(),
                                "retained",
                                null));
                        pending.Enqueue(occupied.OccupantDefinition);
                        edges.Add((definitionId, occupied.OccupantDefinition));
                        decisions.Add(new(requirement.Requirement, occupied.OccupantDefinition, "retained-occupant", $"retained {occupied.Binding}"));
                    }
                    else
                    {
                        conflicts.Add(new(requirement.Requirement, "incompatible-occupant", $"occupied binding '{occupied.Binding}' is incompatible"));
                    }
                }

                var allCompatibleCandidates = snapshot.Candidates
                    .Where(candidate => candidate.Provides.Any(provided =>
                        provided.Contract == requirement.Contract && provided.Version == requirement.Version))
                    .ToArray();
                foreach (var candidate in allCompatibleCandidates)
                {
                    foreach (var rejected in candidate.Policy.Where(item => !item.Accepted))
                    {
                        exclusions.Add(
                            new(
                                requirement.Requirement,
                                candidate.Definition,
                                candidate.Source,
                                rejected.Domain,
                                rejected.Reason));
                        decisions.Add(
                            new(
                                requirement.Requirement,
                                candidate.Definition,
                                "candidate-excluded",
                                $"{candidate.Source} {rejected.Domain}: {rejected.Reason}"));
                    }
                }

                // Source mirrors are alternatives for one definition position. Choose the best
                // admissible observation rather than letting an earlier rejected mirror hide an
                // accepted one, while retaining every mirror in the explanation below.
                var compatibleCandidates = allCompatibleCandidates
                    .Where(candidate => candidate.Policy.All(item => item.Accepted))
                    .GroupBy(candidate => candidate.Definition)
                    .Select(group => group
                        .OrderBy(candidate => Rank(snapshot, definition, requirement, candidate))
                        .ThenBy(candidate => candidate.Publisher.Value, StringComparer.Ordinal)
                        .ThenBy(candidate => candidate.Package.Value, StringComparer.Ordinal)
                        .ThenBy(candidate => candidate.Source.Value, StringComparer.Ordinal)
                        .First())
                    .ToArray();

                var admissible = compatibleCandidates
                    .OrderBy(candidate => Rank(snapshot, definition, requirement, candidate))
                    .ThenBy(candidate => candidate.Definition.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Publisher.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Package.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Source.Value, StringComparer.Ordinal)
                    .ToList();

                var targetCount = requirement.Cardinality.Minimum;
                foreach (var candidate in admissible)
                {
                    if (members.Count >= targetCount)
                    {
                        break;
                    }

                    if (members.Any(item => item.Definition == candidate.Definition))
                    {
                        continue;
                    }

                    members.Add(CreateMember(candidate, requirement, occurrenceCounters, sharedOccurrences));
                    decisions.Add(
                        new(
                            requirement.Requirement,
                            candidate.Definition,
                            "required-provider-selected",
                            $"rank {Rank(snapshot, definition, requirement, candidate)} selected {candidate.Source}"));
                }

                if (members.Count < targetCount)
                {
                    var hasWrongVersion = snapshot.Candidates.Any(candidate =>
                        candidate.Provides.Any(provided => provided.Contract == requirement.Contract));
                    return Refuse(
                        hasWrongVersion ? ResolutionFailureKind.IncompatibleContract : ResolutionFailureKind.MissingDependency,
                        definitionId,
                        requirement.Requirement,
                        $"Provider Set '{requirement.Requirement}' needs {targetCount} members but resolved {members.Count}");
                }

                foreach (var preselected in snapshot.PreselectedProviders
                    .Where(item => item.Requirement == requirement.Requirement)
                    .OrderBy(item => item.Definition.Value, StringComparer.Ordinal))
                {
                    if (members.Any(item => item.Definition == preselected.Definition))
                    {
                        continue;
                    }

                    var candidate = admissible.FirstOrDefault(item => item.Definition == preselected.Definition);
                    if (candidate is null)
                    {
                        return Refuse(
                            ResolutionFailureKind.IncompatibleContract,
                            preselected.Definition,
                            requirement.Requirement,
                            $"preselected provider '{preselected.Definition}' is unavailable or inadmissible");
                    }

                    var maximum = requirement.Cardinality.Maximum ?? int.MaxValue;
                    if (members.Count >= maximum)
                    {
                        return Refuse(
                            ResolutionFailureKind.PortEnvelopeExceeded,
                            preselected.Definition,
                            requirement.Requirement,
                            "preselection exceeds Provider Set maximum");
                    }

                    members.Add(CreateMember(candidate, requirement, occurrenceCounters, sharedOccurrences));
                    decisions.Add(
                        new(
                            requirement.Requirement,
                            candidate.Definition,
                            "optional-provider-preselected",
                            $"explicit preselection used optional capacity from {candidate.Source}"));
                }

                var mediationFailure = ValidateMediation(definitionId, requirement, members.Count);
                if (mediationFailure is not null)
                {
                    return mediationFailure;
                }

                foreach (var member in members)
                {
                    pending.Enqueue(member.Definition);
                    edges.Add((definitionId, member.Definition));
                }

                var bindingPlans = members
                    .OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal)
                    .Select(item => new BindingPlanObservation(
                        requirement.Requirement,
                        item.Occurrence,
                        requirement.Exposure == ProviderExposure.Distinct,
                        requirement.Mediation?.Mediation))
                    .ToArray();
                var alternatives = allCompatibleCandidates
                    .OrderBy(candidate => Rank(snapshot, definition, requirement, candidate))
                    .ThenBy(candidate => candidate.Definition.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Publisher.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Package.Value, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Source.Value, StringComparer.Ordinal)
                    .Select(candidate => new CandidateAlternative(
                        candidate.Definition,
                        candidate.Source,
                        candidate.Publisher,
                        candidate.Package,
                        Rank(snapshot, definition, requirement, candidate),
                        candidate.Policy.All(item => item.Accepted),
                        Array.AsReadOnly(candidate.Policy
                            .Where(item => !item.Accepted)
                            .Select(item => $"{item.Domain}: {item.Reason}")
                            .OrderBy(item => item, StringComparer.Ordinal)
                            .ToArray())))
                    .ToArray();
                var maximumCount = requirement.Cardinality.Maximum ?? members.Count;
                providerSets.Add(
                    new(
                        requirement.Requirement,
                        definitionId,
                        requirement.Contract,
                        requirement.Version,
                        requirement.Scope,
                        requirement.Cardinality,
                        requirement.Exposure,
                        Array.AsReadOnly(members.OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal).ToArray()),
                        Math.Max(0, maximumCount - members.Count),
                        requirement.Mediation,
                        Array.AsReadOnly(bindingPlans),
                        requirement.ContainingRegion,
                        requirement.ContainingPort,
                        Array.AsReadOnly(alternatives)));

                foreach (var preference in snapshot.Preferences
                    .Where(item => item.DeclaredBy == definitionId && item.Contract == requirement.Contract)
                    .OrderBy(item => item.Preference.Value, StringComparer.Ordinal))
                {
                    var used = members.Any(item => item.Definition == preference.PreferredDefinition);
                    var retained = members.Any(item => item.Retained);
                    preferenceObservations.Add(
                        new(
                            preference.Preference,
                            definitionId,
                            preference.PreferredDefinition,
                            requirement.Requirement,
                            used,
                            used
                                ? "preferred-provider-selected"
                                : retained
                                    ? "compatible-occupant-retained"
                                    : "preferred-provider-unavailable-or-excluded"));
                }
            }
        }

        var cycle = FindCycle(included, edges);
        if (cycle is not null)
        {
            return Refuse(
                ResolutionFailureKind.CycleRequiresCm3,
                cycle.Value,
                null,
                $"dependency or composition cycle through '{cycle.Value}' requires CM3 group analysis");
        }

        var parameters = new List<EffectiveParameter>();
        foreach (var definitionId in included.OrderBy(item => item.Value, StringComparer.Ordinal))
        {
            var definition = definitions[definitionId];
            foreach (var slot in definition.ActivationParameters.OrderBy(item => item.Parameter.Value, StringComparer.Ordinal))
            {
                var values = snapshot.ActivationParameters.Where(item => item.Parameter == slot.Parameter).ToArray();
                if (values.Length > 1)
                {
                    return Refuse(
                        ResolutionFailureKind.AmbiguousSelection,
                        definitionId,
                        null,
                        $"Activation Parameter '{slot.Parameter}' has several values",
                        parameter: slot.Parameter);
                }

                if (values.Length == 1)
                {
                    parameters.Add(new(definitionId, slot.Parameter, values[0].Value, "environment"));
                }
                else if (slot.DefaultValue is not null)
                {
                    parameters.Add(new(definitionId, slot.Parameter, slot.DefaultValue, "default"));
                }
                else if (slot.Required)
                {
                    return Refuse(
                        ResolutionFailureKind.ActivationParameterUnavailable,
                        definitionId,
                        null,
                        $"Activation Parameter '{slot.Parameter}' is unavailable",
                        parameter: slot.Parameter);
                }
            }
        }

        var topology = snapshot.TopologyClaims
            .OrderBy(item => item.Claim.Value, StringComparer.Ordinal)
            .Select(item => new TopologyDecision(
                item.Claim,
                item.AssertedBy,
                item.Relation,
                item.Disposition switch
                {
                    TopologyPolicyDisposition.Accepted => item.Relation,
                    TopologyPolicyDisposition.Refined => item.RefinedRelation,
                    _ => null,
                },
                item.From,
                item.To,
                item.Disposition,
                item.Reason))
            .ToArray();
        if (topology.Any(item => item.Disposition == TopologyPolicyDisposition.Refined && item.EffectiveRelation is null))
        {
            return Refuse(
                ResolutionFailureKind.ContradictoryIdentity,
                null,
                null,
                "a refined topology claim must name its effective relation");
        }

        var attachmentOccurrences = providerSets
            .SelectMany(set => set.Members)
            .Where(member => member.AttachmentNode is not null)
            .GroupBy(member => member.Occurrence)
            .Select(group => (
                Occurrence: group.Key,
                Nodes: group.Select(member => member.AttachmentNode!.Value).Distinct().ToArray()))
            .ToArray();
        if (attachmentOccurrences.Any(item => item.Nodes.Length != 1)
            || attachmentOccurrences.Select(item => item.Nodes[0]).Distinct().Count() != attachmentOccurrences.Length)
        {
            return Refuse(
                ResolutionFailureKind.ContradictoryIdentity,
                null,
                null,
                "attachment occurrences must have distinct local Topology Nodes");
        }

        var orderedDefinitions = included.OrderBy(item => item.Value, StringComparer.Ordinal).ToArray();
        var orderedSets = providerSets.OrderBy(item => item.Requirement.Value, StringComparer.Ordinal).ToArray();
        var orderedParameters = parameters
            .OrderBy(item => item.Definition.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Parameter.Value, StringComparer.Ordinal)
            .ToArray();
        var usedParameters = orderedParameters.Select(item => item.Parameter).ToHashSet();
        var unusedParameters = snapshot.ActivationParameters
            .Where(item => !usedParameters.Contains(item.Parameter))
            .OrderBy(item => item.Parameter.Value, StringComparer.Ordinal)
            .ToArray();
        var authority = orderedDefinitions
            .Select(item => new DefinitionAuthorityObservation(
                item,
                Array.AsReadOnly(definitions[item].RequestedAuthority.OrderBy(value => value, StringComparer.Ordinal).ToArray())))
            .ToArray();
        var ports = snapshot.Ports
            .OrderBy(item => item.Region.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Port.Value, StringComparer.Ordinal)
            .Select(port => port with
            {
                Contracts = Array.AsReadOnly(port.Contracts.ToArray()),
                Imports = Array.AsReadOnly(port.Imports.ToArray()),
                Exports = Array.AsReadOnly(port.Exports.ToArray()),
                AuthorityCeiling = Array.AsReadOnly(port.AuthorityCeiling.ToArray()),
                TopologyRequirements = Array.AsReadOnly(port.TopologyRequirements.ToArray()),
            })
            .ToArray();
        var proposed = new ProposedStack(
            snapshot.Generation,
            snapshot.ActiveGeneration,
            snapshot.RestartScope,
            Array.AsReadOnly(snapshot.Roots.OrderBy(item => item.Value, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(orderedDefinitions),
            Array.AsReadOnly(orderedSets),
            Array.AsReadOnly(preferenceObservations.OrderBy(item => item.Preference.Value, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(exclusions
                .OrderBy(item => item.Requirement.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Definition.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Source.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Domain)
                .ToArray()),
            Array.AsReadOnly(conflicts.OrderBy(item => item.Requirement.Value, StringComparer.Ordinal).ToArray()),
            Array.AsReadOnly(orderedParameters),
            Array.AsReadOnly(unusedParameters),
            Array.AsReadOnly(ports),
            Array.AsReadOnly(authority),
            Array.AsReadOnly(topology),
            Array.AsReadOnly(decisions
                .OrderBy(item => item.Requirement?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Definition?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Kind, StringComparer.Ordinal)
                .ToArray()));
        var generation = new ResolvedGeneration(
            snapshot.Generation,
            snapshot.RestartScope,
            proposed.Definitions,
            proposed.ProviderSets,
            proposed.Parameters,
            proposed.Ports,
            proposed.RequestedAuthority,
            proposed.Topology,
            Cm2EffectObservation.None);
        return ResolutionOutcome.Resolved(proposed, generation);
    }

    private static ResolutionOutcome? ValidatePort(
        ResolutionRequest request,
        DefinitionId definition,
        ResolutionRequirement requirement)
    {
        if (requirement.ContainingRegion is null && requirement.ContainingPort is null)
        {
            return null;
        }

        if (requirement.ContainingRegion is null || requirement.ContainingPort is null)
        {
            return Refuse(
                ResolutionFailureKind.PortUnavailable,
                definition,
                requirement.Requirement,
                "a child requirement must name both Region and Port",
                requirement.ContainingRegion,
                requirement.ContainingPort);
        }

        var matchingPorts = request.Ports.Where(item =>
            item.Region == requirement.ContainingRegion && item.Port == requirement.ContainingPort).ToArray();
        if (matchingPorts.Length > 1)
        {
            return Refuse(
                ResolutionFailureKind.ContradictoryIdentity,
                definition,
                requirement.Requirement,
                $"Port '{requirement.ContainingPort}' has contradictory duplicate envelopes",
                requirement.ContainingRegion,
                requirement.ContainingPort);
        }

        var port = matchingPorts.SingleOrDefault();
        if (port is null
            || port.Lifecycle == PortLifecycleMode.Sealed
            || (requirement.RuntimeAttachment && port.Lifecycle != PortLifecycleMode.RuntimeOpen))
        {
            return Refuse(
                ResolutionFailureKind.PortUnavailable,
                definition,
                requirement.Requirement,
                $"Port '{requirement.ContainingPort}' is unavailable for the requested lifecycle",
                requirement.ContainingRegion,
                requirement.ContainingPort);
        }

        var compatible = port.Contracts.Any(item =>
            item.Contract == requirement.Contract && item.Version == requirement.Version);
        var importsAllowed = requirement.RequiredImports.All(port.Imports.Contains);
        var exportsAllowed = requirement.RequiredExports.All(port.Exports.Contains);
        var failurePolicyAllowed = requirement.RequiredFailurePolicy is null
            || requirement.RequiredFailurePolicy == port.FailurePolicy;
        var rollbackBoundaryAllowed = requirement.RequiredRollbackBoundary is null
            || requirement.RequiredRollbackBoundary == port.RollbackBoundary;
        var authorityAllowed = requirement.RequestedAuthority.All(port.AuthorityCeiling.Contains);
        var topologyAllowed = requirement.TopologyRequirements.All(port.TopologyRequirements.Contains);
        var cardinalityAllowed = requirement.Cardinality.Minimum >= port.Cardinality.Minimum
            && (port.Cardinality.Maximum is null
                || requirement.Cardinality.Maximum is int maximum && maximum <= port.Cardinality.Maximum);
        if (compatible
            && importsAllowed
            && exportsAllowed
            && failurePolicyAllowed
            && rollbackBoundaryAllowed
            && authorityAllowed
            && topologyAllowed
            && cardinalityAllowed)
        {
            return null;
        }

        if (port.AllowWiderGenerationProposal)
        {
            return ResolutionOutcome.Wider(
                new(
                    port.Region,
                    port.Port,
                    requirement.Requirement,
                    "child requirement exceeds the declared Port envelope"));
        }

        return Refuse(
            ResolutionFailureKind.PortEnvelopeExceeded,
            definition,
            requirement.Requirement,
            "child requirement exceeds the declared Port envelope",
            port.Region,
            port.Port);
    }

    private static ResolutionOutcome? ValidateMediation(
        DefinitionId definition,
        ResolutionRequirement requirement,
        int memberCount)
    {
        if (requirement.Exposure == ProviderExposure.Distinct)
        {
            return null;
        }

        if (requirement.Mediation is null)
        {
            return Refuse(
                ResolutionFailureKind.MediationRequired,
                definition,
                requirement.Requirement,
                "mediated exposure requires a declared Mediation");
        }

        var mediation = requirement.Mediation;
        var policyBearing = mediation.OwnsMutableMembership
            || mediation.OwnsResidue
            || mediation.OwnsBackpressure
            || mediation.OwnsAuthority
            || mediation.OwnsRecovery
            || mediation.OwnsLifecycle;
        if (policyBearing
            && (mediation.Realization != MediationRealization.DedicatedComponent || mediation.Component is null))
        {
            return Refuse(
                ResolutionFailureKind.MediationRequiresComponent,
                definition,
                requirement.Requirement,
                "policy-bearing Mediation requires a dedicated fake Component");
        }

        if (memberCount < 1)
        {
            return Refuse(
                ResolutionFailureKind.MissingDependency,
                definition,
                requirement.Requirement,
                "Mediation has no backing member");
        }

        return null;
    }

    private static int Rank(
        ResolutionRequest request,
        ResolutionDefinition requester,
        ResolutionRequirement requirement,
        ResolutionCandidate candidate)
    {
        if (request.Preferences.Any(item =>
            item.DeclaredBy == requester.Definition
            && item.Contract == requirement.Contract
            && item.PreferredDefinition == candidate.Definition))
        {
            return 0;
        }

        if (candidate.Publisher == requester.Publisher)
        {
            return 1;
        }

        return candidate.Generic ? 2 : 3;
    }

    private static ProviderSetMember CreateMember(
        ResolutionCandidate candidate,
        ResolutionRequirement requirement,
        IDictionary<DefinitionId, int> counters,
        IDictionary<(DefinitionId, BindingScopeId), OccurrenceId> shared)
    {
        var canShare = requirement.AllowSharing
            && candidate.Sharing.IsolationCompatible
            && candidate.Sharing.LifecycleCompatible
            && candidate.Sharing.AuthorityCompatible;
        var key = (candidate.Definition, requirement.Scope);
        if (canShare && shared.TryGetValue(key, out var existing))
        {
            return NewMember(existing);
        }

        counters.TryGetValue(candidate.Definition, out var current);
        var next = current + 1;
        counters[candidate.Definition] = next;
        var occurrence = OccurrenceId.Create($"occ.{candidate.Definition.Value}.{next}");
        if (canShare)
        {
            shared[key] = occurrence;
        }

        return NewMember(occurrence);

        ProviderSetMember NewMember(OccurrenceId occurrence) =>
            new(
                candidate.Definition,
                occurrence,
                candidate.Source,
                candidate.Publisher,
                candidate.Package,
                false,
                Array.AsReadOnly(candidate.Evidence.OrderBy(item => item.Value, StringComparer.Ordinal).ToArray()),
                Array.AsReadOnly(candidate.Authority.OrderBy(item => item, StringComparer.Ordinal).ToArray()),
                candidate.FailureDomain,
                candidate.AttachmentNode is null
                    ? null
                    : TopologyNodeId.Create($"{candidate.AttachmentNode.Value.Value}.{occurrence.Value}"));
    }

    private static DefinitionId? FindCycle(
        IEnumerable<DefinitionId> definitions,
        IEnumerable<(DefinitionId From, DefinitionId To)> edges)
    {
        var adjacency = edges
            .GroupBy(edge => edge.From)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.To).Distinct().OrderBy(item => item.Value, StringComparer.Ordinal).ToArray());
        var visited = new HashSet<DefinitionId>();
        var visiting = new HashSet<DefinitionId>();

        foreach (var definition in definitions.OrderBy(item => item.Value, StringComparer.Ordinal))
        {
            var cycle = Visit(definition);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;

        DefinitionId? Visit(DefinitionId current)
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
                foreach (var candidate in next)
                {
                    var cycle = Visit(candidate);
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

    private static ResolutionRequest Snapshot(ResolutionRequest request) =>
        request with
        {
            Roots = request.Roots.ToArray(),
            Definitions = request.Definitions.Select(definition => definition with
            {
                Provides = definition.Provides.ToArray(),
                Requirements = definition.Requirements.Select(requirement => requirement with
                {
                    Constraints = requirement.Constraints.ToArray(),
                    RequiredImports = requirement.RequiredImports.ToArray(),
                    RequiredExports = requirement.RequiredExports.ToArray(),
                    RequestedAuthority = requirement.RequestedAuthority.ToArray(),
                    TopologyRequirements = requirement.TopologyRequirements.ToArray(),
                }).ToArray(),
                CompositionParameters = definition.CompositionParameters.Select(parameter => parameter with
                {
                    AllowedDefinitions = parameter.AllowedDefinitions.ToArray(),
                }).ToArray(),
                ActivationParameters = definition.ActivationParameters.ToArray(),
                RequestedAuthority = definition.RequestedAuthority.ToArray(),
            }).ToArray(),
            Candidates = request.Candidates.Select(candidate => candidate with
            {
                Provides = candidate.Provides.ToArray(),
                Policy = candidate.Policy.ToArray(),
                Evidence = candidate.Evidence.ToArray(),
                Authority = candidate.Authority.ToArray(),
            }).ToArray(),
            ExistingOccurrences = request.ExistingOccurrences.Select(item => item with { Actors = item.Actors.ToArray() }).ToArray(),
            OccupiedBindings = request.OccupiedBindings.ToArray(),
            Preferences = request.Preferences.ToArray(),
            AuthorisedReplacements = request.AuthorisedReplacements.ToArray(),
            CompositionParameters = request.CompositionParameters.ToArray(),
            ActivationParameters = request.ActivationParameters.ToArray(),
            PreselectedProviders = request.PreselectedProviders.ToArray(),
            Ports = request.Ports.Select(port => port with
            {
                Contracts = port.Contracts.ToArray(),
                Imports = port.Imports.ToArray(),
                Exports = port.Exports.ToArray(),
                AuthorityCeiling = port.AuthorityCeiling.ToArray(),
                TopologyRequirements = port.TopologyRequirements.ToArray(),
            }).ToArray(),
            TopologyClaims = request.TopologyClaims.ToArray(),
        };

    private static ResolutionOutcome Refuse(
        ResolutionFailureKind kind,
        DefinitionId? definition,
        RequirementId? requirement,
        string reason,
        RegionId? region = null,
        PortId? port = null,
        ParameterId? parameter = null) =>
        ResolutionOutcome.Refused(new(kind, definition, requirement, region, port, parameter, reason));
}
