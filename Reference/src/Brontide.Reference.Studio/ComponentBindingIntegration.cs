using Cm = Brontide.Reference.Experimental.ComponentManagement;
using Portable = Brontide.Reference.Experimental.Binding.Portable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Brontide.Reference.Studio;

public enum ComponentBindingIntegrationFailureKind
{
    ResolutionNotComplete,
    RequirementNotResolved,
    CardinalityUnsupported,
    ExposureUnsupported,
    MembershipUnsupported,
    BindingNotDirect,
    SelectionMismatch,
    MappingInvalid,
    PortableHandoffRefused,
}

public sealed record ComponentBindingSelection(
    Cm.RequirementId Requirement,
    Cm.DefinitionId Definition,
    Cm.OccurrenceId Occurrence,
    Portable.PortableComponentReference Component,
    Portable.PortableProviderReference Provider,
    string HostEndpoint,
    string ProviderEndpoint,
    Portable.PortableContractDocument RequiredContract);

public sealed record ComponentBindingIntegrationFailure(
    ComponentBindingIntegrationFailureKind Kind,
    string Code,
    string Reason);

public sealed record ComponentBindingIntegrationResult(
    Portable.PortableCompositionMember? Member,
    ComponentBindingIntegrationFailure? Failure)
{
    public bool IsPrepared => Member is not null;
}

/// <summary>
/// Composition-root adapter from one completed CM2 provider position to PB7 preflight.
/// </summary>
/// <remarks>
/// The adapter deliberately lives in Studio: Component Management and Portable Binding remain
/// independent experiments, while the composition root is allowed to connect their public seams.
/// </remarks>
public static class ComponentBindingIntegration
{
    public static ComponentBindingIntegrationResult Prepare(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);

        if (resolution.Generation is not { } generation)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.ResolutionNotComplete,
                "resolution-not-complete",
                "Portable preflight requires a completed CM2 generation.");
        }

        var matches = generation.ProviderSets
            .Where(item => item.Requirement == selection.Requirement)
            .ToArray();
        if (matches.Length != 1)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.RequirementNotResolved,
                "requirement-not-resolved",
                $"The completed generation contains {matches.Length} provider positions for requirement '{selection.Requirement}'.");
        }

        var providerSet = matches[0];
        if (providerSet.Cardinality.Minimum != 1 || providerSet.Cardinality.Maximum != 1)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.CardinalityUnsupported,
                "cardinality-unsupported",
                $"CBI1 accepts only cardinality 1..1, not {providerSet.Cardinality}.");
        }

        if (providerSet.Exposure != Cm.ProviderExposure.Distinct || providerSet.Mediation is not null)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.ExposureUnsupported,
                "exposure-unsupported",
                "CBI1 accepts only distinct exposure without Mediation.");
        }

        if (providerSet.Members.Count != 1)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.MembershipUnsupported,
                "membership-unsupported",
                $"A direct 1..1 position must have exactly one member, not {providerSet.Members.Count}.");
        }

        var member = providerSet.Members[0];
        var direct = providerSet.BindingPlans
            .Where(item => item.Member == member.Occurrence && item.Direct && item.Mediation is null)
            .ToArray();
        if (providerSet.BindingPlans.Count != 1 || direct.Length != 1)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.BindingNotDirect,
                "binding-not-direct",
                "The resolved position does not contain exactly one direct binding observation for its member.");
        }

        if (member.Definition != selection.Definition || member.Occurrence != selection.Occurrence)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.SelectionMismatch,
                "selection-mismatch",
                "The explicit portable mapping does not name the definition and occurrence selected by CM2.");
        }

        var maximumTextBytes = selection.RequiredContract.Limits.MaxTextBytes;
        if (!ValidEndpoint(selection.HostEndpoint, maximumTextBytes) ||
            !ValidEndpoint(selection.ProviderEndpoint, maximumTextBytes))
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.MappingInvalid,
                "endpoint-invalid",
                $"Endpoint designations must be non-empty UTF-8 text within the portable contract's {maximumTextBytes}-byte text bound.");
        }

        try
        {
            var scope = Portable.PortableBindingScopeId.Parse(providerSet.Scope.Value);
            var requirement = Portable.PortableResolvedRequirement.OneToOneProvider(
                scope,
                selection.Component,
                selection.Provider,
                selection.HostEndpoint);
            var provision = new Portable.PortableOfferedProvision(
                selection.Component,
                selection.Provider,
                selection.ProviderEndpoint);
            var prepared = Portable.PortableCompositionHandoff.Prepare(
                requirement,
                provision,
                selection.RequiredContract);
            return new(prepared, null);
        }
        catch (Portable.PortableFaultException fault)
        {
            return Refuse(
                ComponentBindingIntegrationFailureKind.PortableHandoffRefused,
                fault.LocalCode,
                fault.Message);
        }
    }

    private static ComponentBindingIntegrationResult Refuse(
        ComponentBindingIntegrationFailureKind kind,
        string code,
        string reason) =>
        new(null, new(kind, code, reason));

    private static bool ValidEndpoint(string? value, int maximumTextBytes) =>
        !string.IsNullOrWhiteSpace(value) &&
        Encoding.UTF8.GetByteCount(value) <= maximumTextBytes;
}

public enum ComponentBindingLifecycleFailureKind
{
    PreparationUnavailable,
    PlanUnsupported,
    RuntimeRefusedBeforeStart,
    PortableInterconnectionRefused,
    PortableReleaseRefused,
}

public sealed record ComponentBindingLifecycleFailure(
    ComponentBindingLifecycleFailureKind Kind,
    string Code,
    string Reason);

public sealed record ComponentBindingLifecycleResult(
    Cm.ActivationRuntimeOutcome? Runtime,
    Portable.PortableCompositionMember? Member,
    ComponentBindingLifecycleFailure? Failure)
{
    public bool IsActive =>
        Runtime?.IsActive == true &&
        Member?.Stage == Portable.PortableCompositionStage.Released &&
        Failure is null;
}

/// <summary>Coordinates one CBI1 member with a singleton, protocol-free CM4 activation plan.</summary>
public static class ComponentBindingLifecycle
{
    public static async ValueTask<ComponentBindingLifecycleResult> ActivateAsync(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        Cm.ActivationRuntimeRequest request,
        Portable.IPortableProviderConversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(conversation);

        var preparation = ComponentBindingIntegration.Prepare(resolution, selection);
        if (preparation.Member is not { } member)
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.PreparationUnavailable,
                "preparation-unavailable",
                "CBI2 requires a successfully prepared CBI1 member.");
        }

        if (!TrySupportedGroup(request.Plan, selection.Occurrence, out var group))
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.PlanUnsupported,
                "plan-unsupported",
                "CBI2 supports exactly one protocol-free activation group containing only the selected occurrence.",
                member: member);
        }

        var successfulStages = StageOutcomes(group!, selection.Occurrence, failedStage: null);
        var successfulRequest = request with { StageOutcomes = successfulStages };
        var preflight = new Cm.FakeActivationRuntime().Activate(successfulRequest);
        if (!preflight.IsActive)
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart,
                "runtime-refused-before-start",
                $"CM4 refused the derived lifecycle before provider establishment: {preflight.Kind}.",
                preflight,
                member);
        }

        try
        {
            await member.InterconnectAsync(conversation, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedRequest = request with
            {
                StageOutcomes = StageOutcomes(group!, selection.Occurrence, Cm.ActivationStage.Interconnection),
            };
            var failedRuntime = new Cm.FakeActivationRuntime().Activate(failedRequest);
            return Refuse(
                ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused,
                exception is Portable.PortableFaultException fault ? fault.LocalCode : "portable-interconnection-failed",
                exception.Message,
                failedRuntime,
                member);
        }

        if (!member.IsReady)
        {
            var failedRequest = request with
            {
                StageOutcomes = StageOutcomes(group!, selection.Occurrence, Cm.ActivationStage.Ready),
            };
            return Refuse(
                ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused,
                "ready-missing",
                "Portable Interconnection completed without a Ready lifecycle state.",
                new Cm.FakeActivationRuntime().Activate(failedRequest),
                member);
        }

        var runtime = new Cm.FakeActivationRuntime().Activate(successfulRequest);
        if (!runtime.IsActive)
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart,
                "runtime-state-changed",
                $"CM4 no longer accepted the lifecycle after portable Ready: {runtime.Kind}.",
                runtime,
                member);
        }

        try
        {
            member.Release();
            return new(runtime, member, null);
        }
        catch (Portable.PortableFaultException fault)
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.PortableReleaseRefused,
                fault.LocalCode,
                fault.Message,
                runtime,
                member);
        }
    }

    internal static bool TrySupportedGroup(
        Cm.ActivationGroupPlan plan,
        Cm.OccurrenceId selectedOccurrence,
        out Cm.ActivationGroupObservation? group)
    {
        group = plan.Groups.Count == 1 ? plan.Groups[0] : null;
        return group is not null &&
            group.Members.Count == 1 &&
            group.Members[0].Occurrence == selectedOccurrence &&
            group.Protocols.Count == 0;
    }

    internal static IReadOnlyList<Cm.MemberStageOutcome> StageOutcomes(
        Cm.ActivationGroupObservation group,
        Cm.OccurrenceId member,
        Cm.ActivationStage? failedStage) =>
        group.Stages.Select(stage => new Cm.MemberStageOutcome(
            group.Group,
            member,
            stage.Stage,
            failedStage switch
            {
                null => true,
                Cm.ActivationStage.Interconnection => stage.Stage == Cm.ActivationStage.LocalInitialisation,
                Cm.ActivationStage.Ready => stage.Stage != Cm.ActivationStage.Ready,
                _ => false,
            },
            failedStage == stage.Stage ? "portable stage failed" : "derived from portable member")).ToArray();

    private static ComponentBindingLifecycleResult Refuse(
        ComponentBindingLifecycleFailureKind kind,
        string code,
        string reason,
        Cm.ActivationRuntimeOutcome? runtime = null,
        Portable.PortableCompositionMember? member = null) =>
        new(runtime, member, new(kind, code, reason));
}

public sealed record ComponentAuthorityMapping(
    Cm.OccurrenceId Occurrence,
    Cm.ActorId Participant);

public enum ComponentAuthorityIntegrationFailureKind
{
    MappingInvalid,
    AuthorityShapeUnsupported,
    AuthorityRefused,
    LifecycleRefused,
}

public sealed record ComponentAuthorityIntegrationFailure(
    ComponentAuthorityIntegrationFailureKind Kind,
    string Code,
    string Reason);

public sealed record ComponentAuthorityIntegrationResult(
    Cm.AuthorityAdmissionOutcome? Authority,
    ComponentBindingLifecycleResult? Lifecycle,
    ComponentAuthorityIntegrationFailure? Failure)
{
    public bool IsActive =>
        Authority?.Kind == Cm.AuthorityAdmissionOutcomeKind.Admitted &&
        Authority.Observation.Grants.Count == 1 &&
        Lifecycle?.IsActive == true &&
        Failure is null;
}

/// <summary>Gates one CBI2 activation with one exact native CM5 admission.</summary>
public static class ComponentAuthorityIntegration
{
    public static async ValueTask<ComponentAuthorityIntegrationResult> ActivateAsync(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        ComponentAuthorityMapping mapping,
        Cm.ActivationRuntimeRequest runtimeRequest,
        Cm.AuthorityAdmissionRequest authorityRequest,
        Portable.IPortableProviderConversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentNullException.ThrowIfNull(authorityRequest);
        ArgumentNullException.ThrowIfNull(conversation);

        if (mapping.Occurrence != selection.Occurrence ||
            mapping.Participant != authorityRequest.Participant)
        {
            return Refuse(
                ComponentAuthorityIntegrationFailureKind.MappingInvalid,
                "authority-mapping-invalid",
                "CBI3 requires the explicit occurrence and participant mapping to match the CBI1 selection and CM5 request.");
        }

        if (!TrySupportedAuthorityShape(authorityRequest, runtimeRequest, out var relationship, out var authority))
        {
            return Refuse(
                ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported,
                "authority-shape-unsupported",
                "CBI3 supports one ComponentParticipant relationship, one dependent narrow authority request, and no caller-authored CM4 binding exercises.");
        }

        var admission = new Cm.FakeAuthorityAdmissionEvaluator().Evaluate(authorityRequest);
        if (!IsExactAdmission(admission, relationship!, authority!))
        {
            return Refuse(
                ComponentAuthorityIntegrationFailureKind.AuthorityRefused,
                "authority-not-admitted",
                $"CM5 did not admit exactly one attributable relationship and grant: {admission.Kind}.",
                admission);
        }

        var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            runtimeRequest,
            conversation,
            cancellationToken).ConfigureAwait(false);
        if (!lifecycle.IsActive)
        {
            return Refuse(
                ComponentAuthorityIntegrationFailureKind.LifecycleRefused,
                lifecycle.Failure?.Code ?? "lifecycle-not-active",
                lifecycle.Failure?.Reason ?? "CBI2 did not return a released Active member.",
                admission,
                lifecycle);
        }

        return new(admission, lifecycle, null);
    }

    private static bool TrySupportedAuthorityShape(
        Cm.AuthorityAdmissionRequest request,
        Cm.ActivationRuntimeRequest runtime,
        out Cm.ActorRelationshipRequest? relationship,
        out Cm.AuthorityRequest? authority)
    {
        relationship = request.Relationships.Count == 1 ? request.Relationships[0] : null;
        authority = request.Authority.Count == 1 ? request.Authority[0] : null;
        return relationship is not null &&
            authority is not null &&
            relationship.Kind == Cm.ActorRelationshipKind.ComponentParticipant &&
            relationship.ProposedActor == request.Participant &&
            authority.Relationship == relationship.Request &&
            !authority.Unlimited &&
            runtime.BindingExercises.Count == 0;
    }

    private static bool IsExactAdmission(
        Cm.AuthorityAdmissionOutcome outcome,
        Cm.ActorRelationshipRequest requestedRelationship,
        Cm.AuthorityRequest requestedAuthority)
    {
        if (outcome.Kind != Cm.AuthorityAdmissionOutcomeKind.Admitted ||
            outcome.Observation.Relationships.Count != 1 ||
            outcome.Observation.Grants.Count != 1)
        {
            return false;
        }

        var relationship = outcome.Observation.Relationships[0];
        var grant = outcome.Observation.Grants[0];
        return relationship.Request == requestedRelationship.Request &&
            relationship.ProposedActor == requestedRelationship.ProposedActor &&
            grant.Request == requestedAuthority.Request &&
            grant.Holder == relationship.LocalActor &&
            grant.Capability == requestedAuthority.Capability &&
            grant.Target == requestedAuthority.Target &&
            grant.Operation == requestedAuthority.Operation &&
            grant.Scope == requestedAuthority.Scope;
    }

    private static ComponentAuthorityIntegrationResult Refuse(
        ComponentAuthorityIntegrationFailureKind kind,
        string code,
        string reason,
        Cm.AuthorityAdmissionOutcome? authority = null,
        ComponentBindingLifecycleResult? lifecycle = null) =>
        new(authority, lifecycle, new(kind, code, reason));
}

public enum ComponentAuthorityRevalidationKind
{
    Continued,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentAuthorityRevalidationResult(
    ComponentAuthorityRevalidationKind Kind,
    Cm.AuthorityAdmissionOutcome? CurrentAuthority,
    Portable.PortableReplacementRecord? Replacement,
    string Code,
    string Reason)
{
    public bool IsActive => Kind == ComponentAuthorityRevalidationKind.Continued;
}

/// <summary>Revalidates the exact CM5 grant that gated CBI3 and retires PB7 when it is lost.</summary>
public static class ComponentAuthorityRevalidation
{
    public static async ValueTask<ComponentAuthorityRevalidationResult> RevalidateAsync(
        ComponentAuthorityIntegrationResult active,
        Cm.AuthorityAdmissionRequest request,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive ||
            active.Authority is not { } previous ||
            active.Lifecycle?.Member is not { } member ||
            previous.Observation.Relationships.Count != 1 ||
            previous.Observation.Grants.Count != 1)
        {
            return new(
                ComponentAuthorityRevalidationKind.ActivationUnavailable,
                null,
                null,
                "active-authority-unavailable",
                "CBI5 requires one released Active CBI3 result with one relationship and grant.");
        }

        Cm.AuthorityAdmissionOutcome? current = null;
        var code = "authority-revalidation-mismatch";
        if (MatchesPreviousRequest(previous, request))
        {
            current = new Cm.FakeAuthorityAdmissionEvaluator().Evaluate(request);
            if (IsSameAdmission(previous, current))
            {
                return new(
                    ComponentAuthorityRevalidationKind.Continued,
                    current,
                    null,
                    "authority-current",
                    "The exact receiving-domain relationship and grant remain admitted.");
            }

            code = "authority-not-renewed";
        }

        try
        {
            var replacement = await member.RetireAsync(retirementReason, cancellationToken)
                .ConfigureAwait(false);
            return new(
                ComponentAuthorityRevalidationKind.Withdrawn,
                current,
                replacement,
                code,
                "The prior authority is no longer current, so the portable member was retired.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            var detail = error is Portable.PortableFaultException fault
                ? $"{fault.LocalCode}: {fault.Message}"
                : error.Message;
            return new(
                ComponentAuthorityRevalidationKind.RetirementFailed,
                current,
                null,
                "authority-retirement-failed",
                detail);
        }
    }

    private static bool MatchesPreviousRequest(
        Cm.AuthorityAdmissionOutcome previous,
        Cm.AuthorityAdmissionRequest request)
    {
        var relationship = previous.Observation.Relationships[0];
        var grant = previous.Observation.Grants[0];
        return request.Request == previous.Observation.Request &&
            request.Policy.Policy == previous.Observation.Policy &&
            request.Participant == relationship.ProposedActor &&
            request.Relationships.Count == 1 &&
            request.Authority.Count == 1 &&
            request.Relationships[0].Request == relationship.Request &&
            request.Relationships[0].ProposedActor == relationship.ProposedActor &&
            request.Relationships[0].Kind == relationship.Kind &&
            request.Authority[0].Request == grant.Request &&
            request.Authority[0].Relationship == relationship.Request &&
            request.Authority[0].Capability == grant.Capability &&
            request.Authority[0].Target == grant.Target &&
            request.Authority[0].Operation == grant.Operation &&
            request.Authority[0].Scope == grant.Scope &&
            !request.Authority[0].Unlimited;
    }

    private static bool IsSameAdmission(
        Cm.AuthorityAdmissionOutcome previous,
        Cm.AuthorityAdmissionOutcome current)
    {
        if (current.Kind != Cm.AuthorityAdmissionOutcomeKind.Admitted ||
            current.Observation.Relationships.Count != 1 ||
            current.Observation.Grants.Count != 1)
        {
            return false;
        }

        var oldRelationship = previous.Observation.Relationships[0];
        var newRelationship = current.Observation.Relationships[0];
        var oldGrant = previous.Observation.Grants[0];
        var newGrant = current.Observation.Grants[0];
        return newRelationship.Request == oldRelationship.Request &&
            newRelationship.ProposedActor == oldRelationship.ProposedActor &&
            newRelationship.Kind == oldRelationship.Kind &&
            newRelationship.LocalActor == oldRelationship.LocalActor &&
            newRelationship.Policy == oldRelationship.Policy &&
            newRelationship.Rule == oldRelationship.Rule &&
            newGrant.Grant == oldGrant.Grant &&
            newGrant.Request == oldGrant.Request &&
            newGrant.Holder == oldGrant.Holder &&
            newGrant.Capability == oldGrant.Capability &&
            newGrant.Target == oldGrant.Target &&
            newGrant.Operation == oldGrant.Operation &&
            newGrant.Scope == oldGrant.Scope &&
            newGrant.Policy == oldGrant.Policy &&
            newGrant.Rule == oldGrant.Rule;
    }
}

public sealed record ComponentParticipantRequest(
    ComponentAuthorityMapping Mapping,
    Cm.AuthorityAdmissionRequest Request);

public enum ComponentParticipantAdmissionFailureKind
{
    ParticipantSetInvalid,
    AuthorityShapeUnsupported,
    AuthorityRefused,
    LocalIdentityConflict,
    LifecycleRefused,
}

public sealed record ComponentParticipantAdmissionFailure(
    ComponentParticipantAdmissionFailureKind Kind,
    string Code,
    string Reason);

public sealed record ComponentParticipantObservation(
    Cm.ActorId Participant,
    Cm.AuthorityAdmissionOutcome Authority);

/// <summary>The outcome of the effect-free admission step, before any provider is contacted.</summary>
internal sealed record ComponentParticipantAdmissionStep(
    IReadOnlyList<ComponentParticipantObservation> Admissions,
    IReadOnlyList<Cm.LocalCapabilityGrant> Grants,
    ComponentParticipantAdmissionFailure? Failure);

public sealed record ComponentParticipantAdmissionResult(
    IReadOnlyList<ComponentParticipantObservation> Admissions,
    IReadOnlyList<Cm.LocalCapabilityGrant> Grants,
    ComponentBindingLifecycleResult? Lifecycle,
    ComponentParticipantAdmissionFailure? Failure)
{
    public bool IsActive => Failure is null && Grants.Count > 0 && Lifecycle?.IsActive == true;
}

/// <summary>
/// Gates one CBI2 activation with a set of participants, each holding one or more exact narrow
/// CM5 grants.
/// </summary>
/// <remarks>
/// A CM5 request names exactly one participant, so a participant set is a set of requests. The
/// evaluator sees each one alone, which leaves the cross-request questions — repeated identities
/// and two participants sharing one receiving-domain Actor — to this coordinator.
/// </remarks>
public static class ComponentParticipantAdmission
{
    public static async ValueTask<ComponentParticipantAdmissionResult> ActivateAsync(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        IReadOnlyList<ComponentParticipantRequest> participants,
        Cm.ActivationRuntimeRequest runtimeRequest,
        Portable.IPortableProviderConversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentNullException.ThrowIfNull(conversation);

        var admitted = Admit(selection, participants, runtimeRequest);
        if (admitted.Failure is { } refusal)
        {
            return new(admitted.Admissions, Array.Empty<Cm.LocalCapabilityGrant>(), null, refusal);
        }

        var admissions = admitted.Admissions;
        var lifecycle = await ComponentBindingLifecycle.ActivateAsync(
            resolution,
            selection,
            runtimeRequest,
            conversation,
            cancellationToken).ConfigureAwait(false);
        if (!lifecycle.IsActive)
        {
            return Refuse(
                ComponentParticipantAdmissionFailureKind.LifecycleRefused,
                lifecycle.Failure?.Code ?? "lifecycle-not-active",
                lifecycle.Failure?.Reason ?? "CBI2 did not return a released Active member.",
                admissions,
                lifecycle);
        }

        return new(admissions, admitted.Grants, lifecycle, null);
    }

    /// <summary>
    /// The effect-free half: everything decided before a provider is contacted.
    /// </summary>
    /// <remarks>
    /// Separated so an activation of several members can admit every member's set before any of them
    /// is established, which is what lets a refusal cost nothing to undo.
    /// </remarks>
    internal static ComponentParticipantAdmissionStep Admit(
        ComponentBindingSelection selection,
        IReadOnlyList<ComponentParticipantRequest> participants,
        Cm.ActivationRuntimeRequest runtimeRequest)
    {
        // Ordering by participant makes evaluation, observation, and grant order independent of
        // the order the caller happened to build the set in.
        var ordered = participants
            .OrderBy(item => item.Mapping.Participant.Value, StringComparer.Ordinal)
            .ToArray();
        if (ValidateSet(ordered, selection) is { } invalid)
        {
            return Step(
                ComponentParticipantAdmissionFailureKind.ParticipantSetInvalid,
                invalid.Code,
                invalid.Reason);
        }

        if (runtimeRequest.BindingExercises.Count > 0 ||
            ordered.Any(item => !SupportedShape(item.Request)))
        {
            return Step(
                ComponentParticipantAdmissionFailureKind.AuthorityShapeUnsupported,
                "authority-shape-unsupported",
                "CBI6 supports one ComponentParticipant relationship per participant, distinct narrow authority tuples dependent on it, and no caller-authored CM4 binding exercises.");
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var admissions = ordered
            .Select(item => new ComponentParticipantObservation(
                item.Mapping.Participant,
                evaluator.Evaluate(item.Request)))
            .ToArray();
        var refused = ordered
            .Where((item, index) => !IsExactAdmission(admissions[index].Authority, item.Request))
            .Select(item => item.Mapping.Participant.Value)
            .ToArray();
        if (refused.Length > 0)
        {
            return Step(
                ComponentParticipantAdmissionFailureKind.AuthorityRefused,
                "authority-not-admitted",
                $"CM5 did not admit the exact submitted authority for {string.Join(", ", refused)}.",
                admissions);
        }

        var holders = admissions
            .Select(item => item.Authority.Observation.Relationships[0].LocalActor.Value)
            .ToArray();
        if (FirstDuplicate(holders) is { } sharedHolder)
        {
            return Step(
                ComponentParticipantAdmissionFailureKind.LocalIdentityConflict,
                "local-actor-conflict",
                $"Two participants were mapped onto the receiving-domain Actor '{sharedHolder}', which would merge their grants into one holder.",
                admissions);
        }

        return new(
            admissions,
            admissions
                .SelectMany(item => item.Authority.Observation.Grants)
                .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
                .ToArray(),
            null);
    }

    private static ComponentParticipantAdmissionStep Step(
        ComponentParticipantAdmissionFailureKind kind,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? admissions = null) =>
        new(
            admissions ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.LocalCapabilityGrant>(),
            new(kind, code, reason));

    private static (string Code, string Reason)? ValidateSet(
        IReadOnlyList<ComponentParticipantRequest> participants,
        ComponentBindingSelection selection)
    {
        if (participants.Count == 0)
        {
            return ("participant-set-empty", "CBI6 requires at least one participant admission request.");
        }

        foreach (var participant in participants)
        {
            if (participant.Mapping.Occurrence != selection.Occurrence ||
                participant.Mapping.Participant != participant.Request.Participant)
            {
                return (
                    "participant-mapping-invalid",
                    "Every participant mapping must name the CBI1-selected occurrence and its own CM5 request participant.");
            }
        }

        if (FirstDuplicate(participants.Select(item => item.Mapping.Participant.Value)) is { } participantActor)
        {
            return (
                "participant-not-distinct",
                $"Participant '{participantActor}' appears in more than one admission request.");
        }

        return DistinctIdentities(participants.Select(item => item.Request).ToArray());
    }

    /// <summary>
    /// Checks the identity rules that only span requests, which no single CM5 evaluation can see.
    /// </summary>
    internal static (string Code, string Reason)? DistinctIdentities(
        IReadOnlyList<Cm.AuthorityAdmissionRequest> requests)
    {
        if (FirstDuplicate(requests.Select(item => item.Request.Value)) is { } admission)
        {
            return (
                "admission-identity-not-distinct",
                $"Admission request identity '{admission}' is used by more than one participant.");
        }

        var relationships = requests
            .SelectMany(item => item.Relationships)
            .Select(item => item.Request.Value);
        if (FirstDuplicate(relationships) is { } relationship)
        {
            return (
                "relationship-identity-not-distinct",
                $"Relationship request identity '{relationship}' is used by more than one participant.");
        }

        var authority = requests
            .SelectMany(item => item.Authority)
            .Select(item => item.Request.Value);
        if (FirstDuplicate(authority) is { } authorityRequest)
        {
            return (
                "authority-identity-not-distinct",
                $"Authority request identity '{authorityRequest}' is used by more than one participant, so its grants would share an identity.");
        }

        return null;
    }

    internal static bool SupportedShape(Cm.AuthorityAdmissionRequest request)
    {
        if (request.Relationships.Count != 1 || request.Authority.Count == 0)
        {
            return false;
        }

        var relationship = request.Relationships[0];
        if (relationship.Kind != Cm.ActorRelationshipKind.ComponentParticipant ||
            relationship.ProposedActor != request.Participant)
        {
            return false;
        }

        if (request.Authority.Any(item => item.Relationship != relationship.Request || item.Unlimited))
        {
            return false;
        }

        return FirstDuplicate(request.Authority.Select(item =>
            $"{item.Capability.Value}|{item.Target.Value}|{item.Operation.Value}|{item.Scope.Value}")) is null;
    }

    internal static bool IsExactAdmission(
        Cm.AuthorityAdmissionOutcome outcome,
        Cm.AuthorityAdmissionRequest request)
    {
        if (outcome.Kind != Cm.AuthorityAdmissionOutcomeKind.Admitted ||
            outcome.Observation.Relationships.Count != 1 ||
            outcome.Observation.Grants.Count != request.Authority.Count)
        {
            return false;
        }

        var established = outcome.Observation.Relationships[0];
        var submitted = request.Relationships[0];
        if (established.Request != submitted.Request ||
            established.ProposedActor != submitted.ProposedActor ||
            established.Kind != submitted.Kind)
        {
            return false;
        }

        // One grant per submitted request, matched on the complete tuple, so equal counts and a
        // single match each make the correspondence a bijection rather than a coincidence.
        return request.Authority.All(authority =>
            outcome.Observation.Grants.Count(grant =>
                grant.Request == authority.Request &&
                grant.Holder == established.LocalActor &&
                grant.Capability == authority.Capability &&
                grant.Target == authority.Target &&
                grant.Operation == authority.Operation &&
                grant.Scope == authority.Scope) == 1);
    }

    internal static string? FirstDuplicate(IEnumerable<string> values) =>
        values.GroupBy(item => item, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(item => item, StringComparer.Ordinal)
            .FirstOrDefault();

    private static ComponentParticipantAdmissionResult Refuse(
        ComponentParticipantAdmissionFailureKind kind,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? admissions = null,
        ComponentBindingLifecycleResult? lifecycle = null) =>
        new(
            admissions ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.LocalCapabilityGrant>(),
            lifecycle,
            new(kind, code, reason));
}

public enum ComponentParticipantRevalidationKind
{
    Continued,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentParticipantRevalidationResult(
    ComponentParticipantRevalidationKind Kind,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.ActorId> Unrenewed,
    Portable.PortableReplacementRecord? Replacement,
    string Code,
    string Reason)
{
    public bool IsActive => Kind == ComponentParticipantRevalidationKind.Continued;
}

/// <summary>
/// Revalidates the complete CBI6 participant set behind one released member and retires it when the
/// set does not renew identically.
/// </summary>
/// <remarks>
/// Retiring on partial loss rather than dropping the participant that lost authority is deliberate:
/// nothing in the admitted set says which participants the member's ordinary interaction depends
/// on, so continuing would decide that invisibly.
/// </remarks>
public static class ComponentParticipantRevalidation
{
    public static async ValueTask<ComponentParticipantRevalidationResult> RevalidateAsync(
        ComponentParticipantAdmissionResult active,
        IReadOnlyList<Cm.AuthorityAdmissionRequest> requests,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle?.Member is not { } member)
        {
            return new(
                ComponentParticipantRevalidationKind.ActivationUnavailable,
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.ActorId>(),
                null,
                "active-authority-unavailable",
                "CBI7 requires one released Active CBI6 result with a completely admitted participant set.");
        }

        var prior = active.Admissions;
        var ordered = requests
            .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select(item => item.Participant)
            .SequenceEqual(prior.Select(item => item.Participant)))
        {
            return await RetireAsync(
                member,
                retirementReason,
                "participant-set-changed",
                "The fresh requests do not name the same participants the admitted set named.",
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.ActorId>(),
                cancellationToken).ConfigureAwait(false);
        }

        if (ordered.Where((item, index) => !MatchesPrior(prior[index], item)).Any())
        {
            return await RetireAsync(
                member,
                retirementReason,
                "authority-revalidation-mismatch",
                "A fresh request does not identify the same relationship and grants that admitted this member.",
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.ActorId>(),
                cancellationToken).ConfigureAwait(false);
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var current = ordered
            .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
            .ToArray();
        var unrenewed = current
            .Where((item, index) => !IsSameAdmission(prior[index].Authority, item.Authority))
            .Select(item => item.Participant)
            .ToArray();
        if (unrenewed.Length > 0)
        {
            return await RetireAsync(
                member,
                retirementReason,
                "authority-not-renewed",
                $"The receiving domain no longer admits the identical authority for {string.Join(", ", unrenewed.Select(item => item.Value))}.",
                current,
                unrenewed,
                cancellationToken).ConfigureAwait(false);
        }

        return new(
            ComponentParticipantRevalidationKind.Continued,
            current,
            Array.Empty<Cm.ActorId>(),
            null,
            "authority-current",
            "Every participant still holds the identical receiving-domain relationship and grants.");
    }

    private static async ValueTask<ComponentParticipantRevalidationResult> RetireAsync(
        Portable.PortableCompositionMember member,
        string retirementReason,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation> current,
        IReadOnlyList<Cm.ActorId> unrenewed,
        CancellationToken cancellationToken)
    {
        var (replacement, failure) = await TryRetireAsync(member, retirementReason, cancellationToken)
            .ConfigureAwait(false);
        return failure is null
            ? new(
                ComponentParticipantRevalidationKind.Withdrawn,
                current,
                unrenewed,
                replacement,
                code,
                reason)
            : new(
                ComponentParticipantRevalidationKind.RetirementFailed,
                current,
                unrenewed,
                null,
                "authority-retirement-failed",
                failure);
    }

    /// <summary>
    /// Retires the member and classifies the peer outcome, without deciding what the caller's
    /// result looks like.
    /// </summary>
    internal static async ValueTask<(Portable.PortableReplacementRecord? Replacement, string? Failure)>
        TryRetireAsync(
            Portable.PortableCompositionMember member,
            string retirementReason,
            CancellationToken cancellationToken)
    {
        try
        {
            return (await member.RetireAsync(retirementReason, cancellationToken).ConfigureAwait(false), null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            return (
                null,
                error is Portable.PortableFaultException fault
                    ? $"{fault.LocalCode}: {fault.Message}"
                    : error.Message);
        }
    }

    internal static bool MatchesPrior(
        ComponentParticipantObservation prior,
        Cm.AuthorityAdmissionRequest request)
    {
        var observation = prior.Authority.Observation;
        if (observation.Relationships.Count != 1 || request.Relationships.Count != 1)
        {
            return false;
        }

        var relationship = observation.Relationships[0];
        var submitted = request.Relationships[0];
        return request.Request == observation.Request &&
            request.Policy.Policy == observation.Policy &&
            request.Participant == prior.Participant &&
            submitted.Request == relationship.Request &&
            submitted.ProposedActor == relationship.ProposedActor &&
            submitted.Kind == relationship.Kind &&
            request.Authority.Count == observation.Grants.Count &&
            observation.Grants.All(grant => request.Authority.Count(authority =>
                authority.Request == grant.Request &&
                authority.Relationship == relationship.Request &&
                authority.Capability == grant.Capability &&
                authority.Target == grant.Target &&
                authority.Operation == grant.Operation &&
                authority.Scope == grant.Scope &&
                !authority.Unlimited) == 1);
    }

    internal static bool IsSameAdmission(
        Cm.AuthorityAdmissionOutcome prior,
        Cm.AuthorityAdmissionOutcome current)
    {
        if (current.Kind != Cm.AuthorityAdmissionOutcomeKind.Admitted ||
            current.Observation.Relationships.Count != 1)
        {
            return false;
        }

        return current.Observation.Relationships[0] == prior.Observation.Relationships[0] &&
            current.Observation.Grants.SequenceEqual(prior.Observation.Grants);
    }
}

public enum ComponentParticipantExtensionKind
{
    Extended,
    Declined,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentParticipantExtensionResult(
    ComponentParticipantExtensionKind Kind,
    ComponentParticipantAdmissionResult? InForce,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.ActorId> Unrenewed,
    Portable.PortableReplacementRecord? Replacement,
    string Code,
    string Reason)
{
    public bool IsExtended => Kind == ComponentParticipantExtensionKind.Extended;
}

/// <summary>
/// Adds participants to an admitted CBI6 set while its member stays released.
/// </summary>
/// <remarks>
/// Only growth is admitted. Removing or substituting a participant would withdraw authority the
/// member may rely on, and nothing in the set says whether it does; a substitute holding the same
/// tuple is a different grant because the holder is part of the grant. A declined extension is not
/// a failure of the binding and leaves it exactly as it was.
/// </remarks>
public static class ComponentParticipantExtension
{
    public static async ValueTask<ComponentParticipantExtensionResult> ExtendAsync(
        ComponentParticipantAdmissionResult active,
        IReadOnlyList<Cm.AuthorityAdmissionRequest> requests,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle?.Member is not { } member)
        {
            return new(
                ComponentParticipantExtensionKind.ActivationUnavailable,
                null,
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.ActorId>(),
                null,
                "active-authority-unavailable",
                "CBI8 requires one released Active CBI6 result with a completely admitted participant set.");
        }

        var prior = active.Admissions;
        var ordered = requests
            .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
            .ToArray();
        if (Structure(prior, ordered) is { } declined)
        {
            return Decline(active, declined.Code, declined.Reason);
        }

        var retained = ordered
            .Where(item => prior.Any(existing => existing.Participant == item.Participant))
            .ToArray();
        if (retained.Where((item, index) => !ComponentParticipantRevalidation.MatchesPrior(prior[index], item)).Any())
        {
            // Nothing was evaluated, so nothing was learned: this is a malformed request, not
            // evidence that the retained authority is gone.
            return Decline(
                active,
                "authority-revalidation-mismatch",
                "A retained request does not identify the same relationship and grants that admitted this member.");
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var current = ordered
            .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
            .ToArray();
        var currentRetained = current
            .Where(item => prior.Any(existing => existing.Participant == item.Participant))
            .ToArray();
        var unrenewed = currentRetained
            .Where((item, index) => !ComponentParticipantRevalidation.IsSameAdmission(
                prior[index].Authority,
                item.Authority))
            .Select(item => item.Participant)
            .ToArray();
        if (unrenewed.Length > 0)
        {
            // A lapse outranks any problem with the addition: the member's existing authority is
            // gone, whatever the caller was trying to add.
            var (replacement, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            return new(
                failure is null
                    ? ComponentParticipantExtensionKind.Withdrawn
                    : ComponentParticipantExtensionKind.RetirementFailed,
                null,
                current,
                unrenewed,
                replacement,
                failure is null ? "authority-not-renewed" : "authority-retirement-failed",
                failure ?? $"The receiving domain no longer admits the identical authority for {string.Join(", ", unrenewed.Select(item => item.Value))}.");
        }

        var refusedAdditions = ordered
            .Where((item, index) =>
                !prior.Any(existing => existing.Participant == item.Participant) &&
                !ComponentParticipantAdmission.IsExactAdmission(current[index].Authority, item))
            .Select(item => item.Participant.Value)
            .ToArray();
        if (refusedAdditions.Length > 0)
        {
            return Decline(
                active,
                "authority-not-admitted",
                $"CM5 did not admit the exact submitted authority for {string.Join(", ", refusedAdditions)}.",
                current);
        }

        var holders = current
            .Select(item => item.Authority.Observation.Relationships[0].LocalActor.Value)
            .ToArray();
        if (ComponentParticipantAdmission.FirstDuplicate(holders) is { } sharedHolder)
        {
            return Decline(
                active,
                "local-actor-conflict",
                $"The extended set would map two participants onto the receiving-domain Actor '{sharedHolder}'.",
                current);
        }

        var grants = current
            .SelectMany(item => item.Authority.Observation.Grants)
            .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
            .ToArray();
        return new(
            ComponentParticipantExtensionKind.Extended,
            new(current, grants, active.Lifecycle, null),
            current,
            Array.Empty<Cm.ActorId>(),
            null,
            "participant-set-extended",
            $"The participant set now holds {current.Length} participants and {grants.Length} grants.");
    }

    private static (string Code, string Reason)? Structure(
        IReadOnlyList<ComponentParticipantObservation> prior,
        IReadOnlyList<Cm.AuthorityAdmissionRequest> intended)
    {
        if (ComponentParticipantAdmission.FirstDuplicate(
                intended.Select(item => item.Participant.Value)) is { } repeated)
        {
            return (
                "participant-not-distinct",
                $"Participant '{repeated}' appears in more than one request.");
        }

        var missing = prior
            .Where(existing => !intended.Any(item => item.Participant == existing.Participant))
            .Select(existing => existing.Participant.Value)
            .ToArray();
        if (missing.Length > 0)
        {
            return (
                "participant-not-retained",
                $"CBI8 only grows a set. Removing or substituting {string.Join(", ", missing)} requires CBI7 retirement and a fresh CBI6 admission.");
        }

        if (intended.Count == prior.Count)
        {
            return (
                "participant-set-unchanged",
                "The intended set adds no participant; revalidating the current set is CBI7.");
        }

        if (ComponentParticipantAdmission.DistinctIdentities(intended) is { } collision)
        {
            return collision;
        }

        var added = intended
            .Where(item => !prior.Any(existing => existing.Participant == item.Participant))
            .ToArray();
        return added.All(ComponentParticipantAdmission.SupportedShape)
            ? null
            : (
                "authority-shape-unsupported",
                "CBI8 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.");
    }

    private static ComponentParticipantExtensionResult Decline(
        ComponentParticipantAdmissionResult active,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? current = null) =>
        new(
            ComponentParticipantExtensionKind.Declined,
            active,
            current ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.ActorId>(),
            null,
            code,
            reason);
}

public sealed record ComponentGrantDependencyEntry(
    string DeclaredAuthority,
    Cm.CapabilityId Capability,
    Cm.ActorId Target,
    Cm.OperationId Operation,
    Cm.CapabilityScopeId Scope);

public sealed record ComponentGrantDependency(
    Cm.DefinitionId Definition,
    IReadOnlyList<ComponentGrantDependencyEntry> Entries);

public enum ComponentParticipantRevisionKind
{
    Revised,
    Declined,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentParticipantRevisionResult(
    ComponentParticipantRevisionKind Kind,
    ComponentParticipantAdmissionResult? InForce,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.ActorId> Unrenewed,
    Portable.PortableReplacementRecord? Replacement,
    string Code,
    string Reason)
{
    public bool IsRevised => Kind == ComponentParticipantRevisionKind.Revised;
}

/// <summary>
/// Removes and substitutes participants of a live set, under a dependency the resolved Component
/// definition declared.
/// </summary>
/// <remarks>
/// The declaration is what CBI7 and CBI8 lacked. Its names come from CM2's record of the selected
/// definition's requested authority, so the Component states what its interaction depends on and
/// the caller only maps each name to the CM5 tuple that satisfies it. Because the declaration names
/// tuples rather than holders, a substitute that satisfies the same dependency is enough.
/// </remarks>
public static class ComponentParticipantRevision
{
    public static async ValueTask<ComponentParticipantRevisionResult> ReviseAsync(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        ComponentParticipantAdmissionResult active,
        ComponentGrantDependency dependency,
        IReadOnlyList<Cm.AuthorityAdmissionRequest> requests,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle?.Member is not { } member)
        {
            return new(
                ComponentParticipantRevisionKind.ActivationUnavailable,
                null,
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.ActorId>(),
                null,
                "active-authority-unavailable",
                "CBI9 requires one released Active CBI6 result with a completely admitted participant set.");
        }

        if (Declaration(resolution, selection, dependency, active) is { } undeclared)
        {
            return Decline(active, undeclared.Code, undeclared.Reason);
        }

        var prior = active.Admissions;
        var priorByActor = prior.ToDictionary(item => item.Participant);
        var ordered = requests
            .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
            .ToArray();
        if (Structure(prior, ordered) is { } declined)
        {
            return Decline(active, declined.Code, declined.Reason);
        }

        var retained = ordered.Where(item => priorByActor.ContainsKey(item.Participant)).ToArray();
        if (retained.Any(item => !ComponentParticipantRevalidation.MatchesPrior(
                priorByActor[item.Participant],
                item)))
        {
            return Decline(
                active,
                "authority-revalidation-mismatch",
                "A retained request does not identify the same relationship and grants that admitted this member.");
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var current = ordered
            .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
            .ToArray();
        var unrenewed = current
            .Where(item => priorByActor.TryGetValue(item.Participant, out var existing) &&
                !ComponentParticipantRevalidation.IsSameAdmission(existing.Authority, item.Authority))
            .Select(item => item.Participant)
            .ToArray();
        if (unrenewed.Length > 0)
        {
            var (replacement, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            return new(
                failure is null
                    ? ComponentParticipantRevisionKind.Withdrawn
                    : ComponentParticipantRevisionKind.RetirementFailed,
                null,
                current,
                unrenewed,
                replacement,
                failure is null ? "authority-not-renewed" : "authority-retirement-failed",
                failure ?? $"The receiving domain no longer admits the identical authority for {string.Join(", ", unrenewed.Select(item => item.Value))}.");
        }

        var refusedAdditions = ordered
            .Where((item, index) =>
                !priorByActor.ContainsKey(item.Participant) &&
                !ComponentParticipantAdmission.IsExactAdmission(current[index].Authority, item))
            .Select(item => item.Participant.Value)
            .ToArray();
        if (refusedAdditions.Length > 0)
        {
            return Decline(
                active,
                "authority-not-admitted",
                $"CM5 did not admit the exact submitted authority for {string.Join(", ", refusedAdditions)}.",
                current);
        }

        var holders = current
            .Select(item => item.Authority.Observation.Relationships[0].LocalActor.Value)
            .ToArray();
        if (ComponentParticipantAdmission.FirstDuplicate(holders) is { } sharedHolder)
        {
            return Decline(
                active,
                "local-actor-conflict",
                $"The revised set would map two participants onto the receiving-domain Actor '{sharedHolder}'.",
                current);
        }

        var grants = current
            .SelectMany(item => item.Authority.Observation.Grants)
            .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
            .ToArray();
        var uncovered = Uncovered(dependency, grants);
        if (uncovered.Length > 0)
        {
            return Decline(
                active,
                "dependency-not-covered",
                $"The intended set holds no grant satisfying declared authority {string.Join(", ", uncovered)}.",
                current);
        }

        return new(
            ComponentParticipantRevisionKind.Revised,
            new(current, grants, active.Lifecycle, null),
            current,
            Array.Empty<Cm.ActorId>(),
            null,
            "participant-set-revised",
            $"The participant set now holds {current.Length} participants and {grants.Length} grants, still covering every declared dependency.");
    }

    /// <summary>
    /// Checks that the declaration is the one the generation records, without asking whether the
    /// set in force covers it.
    /// </summary>
    internal static (string Code, string Reason)? DeclarationShape(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        ComponentGrantDependency dependency)
    {
        var declared = resolution.Generation?.RequestedAuthority
            .FirstOrDefault(item => item.Definition == selection.Definition);
        if (dependency.Definition != selection.Definition || declared is null)
        {
            return (
                "dependency-declaration-mismatch",
                "The declaration must name the CBI1-selected definition recorded by the completed generation.");
        }

        if (declared.RequestedAuthority.Count == 0)
        {
            return (
                "dependency-declaration-empty",
                "The selected definition requests no authority, which states nothing about what its interaction depends on; use CBI8 to grow the set or CBI7 to retire it.");
        }

        var names = dependency.Entries
            .Select(item => item.DeclaredAuthority)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var expected = declared.RequestedAuthority
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        return names.SequenceEqual(expected, StringComparer.Ordinal) &&
            ComponentParticipantAdmission.FirstDuplicate(dependency.Entries.Select(Tuple)) is null
            ? null
            : (
                "dependency-declaration-mismatch",
                "The declaration must map exactly the authority the selected definition requests, once each, to distinct tuples.");
    }

    private static (string Code, string Reason)? Declaration(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        ComponentGrantDependency dependency,
        ComponentParticipantAdmissionResult active)
    {
        if (DeclarationShape(resolution, selection, dependency) is { } invalid)
        {
            return invalid;
        }

        var uncovered = Uncovered(dependency, active.Grants);
        return uncovered.Length > 0
            ? (
                "dependency-unsatisfied",
                $"The set in force holds no grant satisfying declared authority {string.Join(", ", uncovered)}, so it never covered this declaration.")
            : null;
    }

    private static (string Code, string Reason)? Structure(
        IReadOnlyList<ComponentParticipantObservation> prior,
        IReadOnlyList<Cm.AuthorityAdmissionRequest> intended)
    {
        if (intended.Count == 0)
        {
            return (
                "participant-set-empty",
                "A revision must leave at least one participant; an empty set is not an admitted set.");
        }

        if (ComponentParticipantAdmission.FirstDuplicate(
                intended.Select(item => item.Participant.Value)) is { } repeated)
        {
            return (
                "participant-not-distinct",
                $"Participant '{repeated}' appears in more than one request.");
        }

        if (intended.Count == prior.Count &&
            intended.All(item => prior.Any(existing => existing.Participant == item.Participant)))
        {
            return (
                "participant-set-unchanged",
                "The intended set is the current one; revalidating it is CBI7.");
        }

        if (ComponentParticipantAdmission.DistinctIdentities(intended) is { } collision)
        {
            return collision;
        }

        var added = intended
            .Where(item => !prior.Any(existing => existing.Participant == item.Participant))
            .ToArray();
        return added.All(ComponentParticipantAdmission.SupportedShape)
            ? null
            : (
                "authority-shape-unsupported",
                "CBI9 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.");
    }

    internal static string[] Uncovered(
        ComponentGrantDependency dependency,
        IReadOnlyList<Cm.LocalCapabilityGrant> grants)
    {
        var held = grants.Select(Tuple).ToHashSet(StringComparer.Ordinal);
        return dependency.Entries
            .Where(entry => !held.Contains(Tuple(entry)))
            .Select(entry => entry.DeclaredAuthority)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    internal static string Tuple(ComponentGrantDependencyEntry entry) =>
        $"{entry.Capability.Value}|{entry.Target.Value}|{entry.Operation.Value}|{entry.Scope.Value}";

    internal static string Tuple(Cm.LocalCapabilityGrant grant) =>
        $"{grant.Capability.Value}|{grant.Target.Value}|{grant.Operation.Value}|{grant.Scope.Value}";

    private static ComponentParticipantRevisionResult Decline(
        ComponentParticipantAdmissionResult active,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? current = null) =>
        new(
            ComponentParticipantRevisionKind.Declined,
            active,
            current ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.ActorId>(),
            null,
            code,
            reason);
}

public sealed record ComponentObservedInteraction(
    Portable.PortableOperationReference Operation,
    Portable.PortableInteractionResult Result);

public sealed record ComponentOperationAuthorityMapping(
    Portable.PortableOperationReference Operation,
    string DeclaredAuthority);

public enum ComponentInteractionVerdictKind
{
    Consistent,
    UndeclaredUse,
    UngrantedUse,
    RetirementFailed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentInteractionVerdict(
    ComponentInteractionVerdictKind Kind,
    Cm.ActivationRuntimeOutcome? Runtime,
    IReadOnlyList<Cm.BindingExerciseDeclaration> Exercises,
    IReadOnlyList<string> Unexercised,
    IReadOnlyList<string> Uncovered,
    Portable.PortableReplacementRecord? Replacement,
    string Code,
    string Reason)
{
    public bool IsConsistent => Kind == ComponentInteractionVerdictKind.Consistent;
}

/// <summary>
/// Verifies a CBI9 declaration against what the member actually did, through CM4 binding exercises
/// projected from observed portable interactions.
/// </summary>
/// <remarks>
/// The admission fact of each projected exercise is derived from the declaration and the grants in
/// force, so CM4's own rule — delivery cannot succeed when the external authority check denied it —
/// is what condemns use outside the declaration. The caller supplies observations and an attribution
/// mapping, never an admission.
/// </remarks>
public static class ComponentInteractionVerification
{
    public static async ValueTask<ComponentInteractionVerdict> VerifyAsync(
        Cm.ResolutionOutcome resolution,
        ComponentBindingSelection selection,
        ComponentParticipantAdmissionResult active,
        ComponentGrantDependency dependency,
        IReadOnlyList<ComponentOperationAuthorityMapping> attribution,
        IReadOnlyList<ComponentObservedInteraction> observations,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(attribution);
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle?.Member is not { } member)
        {
            return Verdict(
                ComponentInteractionVerdictKind.ActivationUnavailable,
                "active-authority-unavailable",
                "CBI10 requires one released Active CBI6 result with a completely admitted participant set.");
        }

        if (ComponentParticipantRevision.DeclarationShape(resolution, selection, dependency) is { } invalid)
        {
            return Verdict(ComponentInteractionVerdictKind.Declined, invalid.Code, invalid.Reason);
        }

        if (ComponentParticipantAdmission.FirstDuplicate(
                attribution.Select(item => item.Operation.ToString())) is { } repeated)
        {
            return Verdict(
                ComponentInteractionVerdictKind.Declined,
                "operation-mapping-not-distinct",
                $"Operation '{repeated}' is attributed to more than one declared authority.");
        }

        if (!ComponentBindingLifecycle.TrySupportedGroup(runtimeRequest.Plan, selection.Occurrence, out var group))
        {
            return Verdict(
                ComponentInteractionVerdictKind.Declined,
                "plan-unsupported",
                "CBI10 projects exercises onto the one protocol-free activation group CBI2 activated.");
        }

        var declared = dependency.Entries.ToDictionary(item => item.DeclaredAuthority, StringComparer.Ordinal);
        var uncoveredNames = ComponentParticipantRevision.Uncovered(dependency, active.Grants)
            .ToHashSet(StringComparer.Ordinal);
        var attributed = Attribute(attribution, observations);

        var exercises = attributed
            .Select((name, index) => new Cm.BindingExerciseDeclaration(
                Cm.BindingExerciseId.Create($"exercise.observed-{index + 1}"),
                Cm.BindingId.Create($"binding.{selection.Occurrence.Value}"),
                selection.Occurrence,
                selection.Occurrence,
                Cm.SourceId.Create("source.portable-observation"),
                Cm.BindingExposureKind.Distinct,
                null,
                Cm.RoutingDecisionId.Create($"routing.observed-{index + 1}"),
                name is not null && declared.ContainsKey(name) && !uncoveredNames.Contains(name),
                Cm.BindingDeliveryResult.Delivered,
                null))
            .ToArray();
        var runtime = new Cm.FakeActivationRuntime().Activate(runtimeRequest with
        {
            StageOutcomes = ComponentBindingLifecycle.StageOutcomes(group!, selection.Occurrence, null),
            BindingExercises = exercises,
        });

        var exercised = attributed
            .Where(name => name is not null && declared.ContainsKey(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
        var unexercised = dependency.Entries
            .Select(item => item.DeclaredAuthority)
            .Where(name => !exercised.Contains(name))
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var uncovered = uncoveredNames.OrderBy(item => item, StringComparer.Ordinal).ToArray();

        var undeclared = attributed.Any(name => name is null || !declared.ContainsKey(name));
        var ungranted = attributed.Any(name => name is not null && uncoveredNames.Contains(name));
        if (undeclared || ungranted)
        {
            var (replacement, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            return new(
                failure is null
                    ? undeclared
                        ? ComponentInteractionVerdictKind.UndeclaredUse
                        : ComponentInteractionVerdictKind.UngrantedUse
                    : ComponentInteractionVerdictKind.RetirementFailed,
                runtime,
                exercises,
                unexercised,
                uncovered,
                replacement,
                failure is null
                    ? undeclared ? "interaction-undeclared" : "interaction-ungranted"
                    : "authority-retirement-failed",
                failure ?? (undeclared
                    ? "A delivered interaction could not be attributed to any authority the Component declared."
                    : "A delivered interaction exercised declared authority no participant holds a grant for."));
        }

        return new(
            ComponentInteractionVerdictKind.Consistent,
            runtime,
            exercises,
            unexercised,
            uncovered,
            null,
            "interaction-consistent",
            $"{exercises.Length} delivered interaction(s) stayed inside the declaration.");
    }

    /// <summary>
    /// Attributes every delivered interaction to a declared authority, or to none.
    /// </summary>
    /// <remarks>
    /// No frame, no exercise: a locally denied request reached no provider. Any emitted frame
    /// counts, because the receiving domain cannot know what a frame the provider already saw
    /// caused.
    /// </remarks>
    internal static string?[] Attribute(
        IReadOnlyList<ComponentOperationAuthorityMapping> attribution,
        IReadOnlyList<ComponentObservedInteraction> observations) =>
        observations
            .Where(item => item.Result.FrameDecision != Portable.PortableFrameDecision.None)
            .Select(item => attribution
                .FirstOrDefault(entry => entry.Operation == item.Operation)?.DeclaredAuthority)
            .ToArray();

    private static ComponentInteractionVerdict Verdict(
        ComponentInteractionVerdictKind kind,
        string code,
        string reason) =>
        new(
            kind,
            null,
            Array.Empty<Cm.BindingExerciseDeclaration>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            code,
            reason);
}

public enum ComponentDeclarationSuccessionKind
{
    Narrowed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentDeclarationSuccessionResult(
    ComponentDeclarationSuccessionKind Kind,
    ComponentGrantDependency? Declaration,
    IReadOnlyList<string> Dropped,
    IReadOnlyList<string> Vetoed,
    string Code,
    string Reason)
{
    public bool IsNarrowed => Kind == ComponentDeclarationSuccessionKind.Narrowed;
}

/// <summary>
/// Narrows the declaration in force to a successor resolution of the same position, unless observed
/// use vetoes it.
/// </summary>
/// <remarks>
/// Absence of use never justifies removing a dependency, so the permission comes from the
/// Component's own re-declaration and observation appears only as a veto. Nothing here retires a
/// member or changes the participant set; narrowing only changes what a later CBI9 revision will
/// admit.
/// </remarks>
public static class ComponentDeclarationSuccession
{
    public static ComponentDeclarationSuccessionResult Succeed(
        Cm.ResolutionOutcome resolution,
        Cm.ResolutionOutcome successor,
        ComponentBindingSelection selection,
        ComponentParticipantAdmissionResult active,
        ComponentGrantDependency declaration,
        ComponentGrantDependency successorDeclaration,
        IReadOnlyList<ComponentOperationAuthorityMapping> attribution,
        IReadOnlyList<ComponentObservedInteraction> observations)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(successorDeclaration);
        ArgumentNullException.ThrowIfNull(attribution);
        ArgumentNullException.ThrowIfNull(observations);

        if (!active.IsActive || active.Lifecycle?.Member is not { } member)
        {
            return new(
                ComponentDeclarationSuccessionKind.ActivationUnavailable,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "active-authority-unavailable",
                "CBI11 requires one released Active CBI6 result with a completely admitted participant set.");
        }

        if (ComponentParticipantRevision.DeclarationShape(resolution, selection, declaration) is { } stale)
        {
            return Decline(declaration, stale.Code, stale.Reason);
        }

        if (ComponentParticipantRevision.DeclarationShape(successor, selection, successorDeclaration) is { } invalid)
        {
            return Decline(declaration, invalid.Code, invalid.Reason);
        }

        if (SamePosition(successor, selection, member) is { } mismatch)
        {
            return Decline(declaration, "successor-position-mismatch", mismatch);
        }

        var names = declaration.Entries.Select(item => item.DeclaredAuthority).ToHashSet(StringComparer.Ordinal);
        var successorNames = successorDeclaration.Entries
            .Select(item => item.DeclaredAuthority)
            .ToHashSet(StringComparer.Ordinal);
        if (!successorNames.IsProperSubsetOf(names))
        {
            return Decline(
                declaration,
                "declaration-not-narrower",
                "Succession only narrows: the successor must declare strictly fewer authorities, all of them already declared.");
        }

        var repointed = successorDeclaration.Entries
            .Where(entry => declaration.Entries.Any(current =>
                current.DeclaredAuthority == entry.DeclaredAuthority &&
                ComponentParticipantRevision.Tuple(current) != ComponentParticipantRevision.Tuple(entry)))
            .Select(entry => entry.DeclaredAuthority)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        if (repointed.Length > 0)
        {
            return Decline(
                declaration,
                "declaration-tuple-changed",
                $"Succession removes dependencies; it does not re-point them. {string.Join(", ", repointed)} would change tuple.");
        }

        if (ComponentParticipantAdmission.FirstDuplicate(
                attribution.Select(item => item.Operation.ToString())) is { } repeated)
        {
            return Decline(
                declaration,
                "operation-mapping-not-distinct",
                $"Operation '{repeated}' is attributed to more than one declared authority.");
        }

        var dropped = names.Except(successorNames, StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var exercised = ComponentInteractionVerification.Attribute(attribution, observations)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
        var vetoed = dropped.Where(exercised.Contains).ToArray();
        if (vetoed.Length > 0)
        {
            return new(
                ComponentDeclarationSuccessionKind.Declined,
                declaration,
                Array.Empty<string>(),
                vetoed,
                "declaration-use-vetoed",
                $"The member has already exercised {string.Join(", ", vetoed)}, so the successor cannot narrow it away.");
        }

        return new(
            ComponentDeclarationSuccessionKind.Narrowed,
            successorDeclaration,
            dropped,
            Array.Empty<string>(),
            "declaration-narrowed",
            $"The declaration in force no longer includes {string.Join(", ", dropped)}.");
    }

    private static string? SamePosition(
        Cm.ResolutionOutcome successor,
        ComponentBindingSelection selection,
        Portable.PortableCompositionMember member)
    {
        if (successor.Generation is not { } generation)
        {
            return "The successor resolution did not complete a generation.";
        }

        var matches = generation.ProviderSets
            .Where(item => item.Requirement == selection.Requirement)
            .ToArray();
        if (matches.Length != 1)
        {
            return $"The successor generation contains {matches.Length} provider positions for requirement '{selection.Requirement}'.";
        }

        var providerSet = matches[0];
        if (providerSet.Cardinality.Minimum != 1 ||
            providerSet.Cardinality.Maximum != 1 ||
            providerSet.Exposure != Cm.ProviderExposure.Distinct ||
            providerSet.Mediation is not null ||
            providerSet.Members.Count != 1)
        {
            return "The successor position is not the direct 1..1 distinct position this member was bound under.";
        }

        var successorMember = providerSet.Members[0];
        return successorMember.Definition == selection.Definition &&
            successorMember.Occurrence == selection.Occurrence &&
            providerSet.Scope.Value == member.Fact("bindingScope")
            ? null
            : "The successor resolves a different definition, occurrence, or binding scope than the live member.";
    }

    private static ComponentDeclarationSuccessionResult Decline(
        ComponentGrantDependency declaration,
        string code,
        string reason) =>
        new(
            ComponentDeclarationSuccessionKind.Declined,
            declaration,
            Array.Empty<string>(),
            Array.Empty<string>(),
            code,
            reason);
}

public sealed record ComponentGroupMember(
    ComponentBindingSelection Selection,
    Portable.IPortableProviderConversation Conversation);

public enum ComponentGroupActivationFailureKind
{
    PlanUnsupported,
    PreparationUnavailable,
    RuntimeRefusedBeforeStart,
    MemberEstablishmentRefused,
    MemberReleaseRefused,
}

public sealed record ComponentGroupActivationFailure(
    ComponentGroupActivationFailureKind Kind,
    string Code,
    string Reason,
    Cm.OccurrenceId? Member);

public sealed record ComponentGroupMemberOutcome(
    Cm.OccurrenceId Occurrence,
    Portable.PortableCompositionMember Member);

public sealed record ComponentGroupActivationResult(
    Cm.ActivationRuntimeOutcome? Runtime,
    IReadOnlyList<ComponentGroupMemberOutcome> Members,
    ComponentGroupActivationFailure? Failure)
{
    public bool IsActive =>
        Failure is null &&
        Runtime?.IsActive == true &&
        Members.Count > 0 &&
        Members.All(item => item.Member.IsReleased);
}

/// <summary>
/// Activates several independent members under one CM4 activation, with the release barrier at the
/// activation rather than at any one member.
/// </summary>
/// <remarks>
/// CM4 models one logical Release for an activation attempt, so ordinary interaction opens for every
/// member at once or for none; the answer comes from the runtime's shape rather than from a choice
/// made here. Cyclic groups are refused: a multi-member group is a strongly connected component,
/// which is what Relational Initialisation exists for.
/// </remarks>
public static class ComponentGroupLifecycle
{
    public static async ValueTask<ComponentGroupActivationResult> ActivateAsync(
        Cm.ResolutionOutcome resolution,
        IReadOnlyList<ComponentGroupMember> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);

        var ordered = members
            .OrderBy(item => item.Selection.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        if (!SupportedPlan(runtimeRequest.Plan, ordered))
        {
            return Refuse(
                ComponentGroupActivationFailureKind.PlanUnsupported,
                "plan-unsupported",
                "CBI12 activates one protocol-free single-member group per selected occurrence, and no others.");
        }

        var prepared = new List<ComponentGroupMemberOutcome>();
        foreach (var member in ordered)
        {
            var preparation = ComponentBindingIntegration.Prepare(resolution, member.Selection);
            if (preparation.Member is not { } portable)
            {
                return Refuse(
                    ComponentGroupActivationFailureKind.PreparationUnavailable,
                    preparation.Failure!.Code,
                    preparation.Failure.Reason,
                    member.Selection.Occurrence);
            }

            prepared.Add(new(member.Selection.Occurrence, portable));
        }

        var successful = runtimeRequest with
        {
            StageOutcomes = GroupStageOutcomes(runtimeRequest.Plan, null, null),
        };
        var preflight = new Cm.FakeActivationRuntime().Activate(successful);
        if (!preflight.IsActive)
        {
            return new(
                preflight,
                prepared,
                new(
                    ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart,
                    "runtime-refused-before-start",
                    $"CM4 refused the derived activation before provider establishment: {preflight.Kind}.",
                    null));
        }

        for (var index = 0; index < ordered.Length; index++)
        {
            var member = ordered[index];
            var portable = prepared[index].Member;
            try
            {
                await portable.InterconnectAsync(member.Conversation, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return await FailAsync(
                    runtimeRequest,
                    prepared,
                    member.Selection.Occurrence,
                    Cm.ActivationStage.Interconnection,
                    exception is Portable.PortableFaultException fault
                        ? fault.LocalCode
                        : "portable-interconnection-failed",
                    exception.Message,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!portable.IsReady)
            {
                return await FailAsync(
                    runtimeRequest,
                    prepared,
                    member.Selection.Occurrence,
                    Cm.ActivationStage.Ready,
                    "ready-missing",
                    "Portable Interconnection completed without a Ready lifecycle state.",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var runtime = new Cm.FakeActivationRuntime().Activate(successful);
        if (!runtime.IsActive)
        {
            return new(
                runtime,
                prepared,
                new(
                    ComponentGroupActivationFailureKind.RuntimeRefusedBeforeStart,
                    "runtime-state-changed",
                    $"CM4 no longer accepted the activation after every member reported Ready: {runtime.Kind}.",
                    null));
        }

        // The barrier: every member reached Ready and CM4 accepted the activation, so ordinary
        // interaction opens for all of them together.
        foreach (var outcome in prepared)
        {
            try
            {
                outcome.Member.Release();
            }
            catch (Portable.PortableFaultException fault)
            {
                return new(
                    runtime,
                    prepared,
                    new(
                        ComponentGroupActivationFailureKind.MemberReleaseRefused,
                        fault.LocalCode,
                        fault.Message,
                        outcome.Occurrence));
            }
        }

        return new(runtime, prepared, null);
    }

    private static async ValueTask<ComponentGroupActivationResult> FailAsync(
        Cm.ActivationRuntimeRequest runtimeRequest,
        IReadOnlyList<ComponentGroupMemberOutcome> prepared,
        Cm.OccurrenceId failed,
        Cm.ActivationStage stage,
        string code,
        string reason,
        CancellationToken cancellationToken)
    {
        var cleanup = new List<string>();
        foreach (var outcome in prepared)
        {
            if (outcome.Member.Stage == Portable.PortableCompositionStage.LocalInitialisation ||
                outcome.Member.Stage == Portable.PortableCompositionStage.Retired)
            {
                continue;
            }

            var (_, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, $"activation failed at {failed}", cancellationToken)
                .ConfigureAwait(false);
            if (failure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {failure}");
            }
        }

        var runtime = new Cm.FakeActivationRuntime().Activate(runtimeRequest with
        {
            StageOutcomes = GroupStageOutcomes(runtimeRequest.Plan, failed, stage),
        });
        return new(
            runtime,
            prepared,
            new(
                ComponentGroupActivationFailureKind.MemberEstablishmentRefused,
                code,
                cleanup.Count == 0 ? reason : $"{reason} Cleanup also failed for {string.Join("; ", cleanup)}.",
                failed));
    }

    private static bool SupportedPlan(
        Cm.ActivationGroupPlan plan,
        IReadOnlyList<ComponentGroupMember> members)
    {
        var selected = members.Select(item => item.Selection.Occurrence).ToHashSet();
        return selected.Count == members.Count &&
            plan.Groups.Count == members.Count &&
            plan.Groups.All(group =>
                group.Members.Count == 1 &&
                group.Protocols.Count == 0 &&
                selected.Contains(group.Members[0].Occurrence));
    }

    private static IReadOnlyList<Cm.MemberStageOutcome> GroupStageOutcomes(
        Cm.ActivationGroupPlan plan,
        Cm.OccurrenceId? failedMember,
        Cm.ActivationStage? failedStage) =>
        plan.Groups
            .SelectMany(group => group.Members.SelectMany(member =>
                ComponentBindingLifecycle.StageOutcomes(
                    group,
                    member.Occurrence,
                    member.Occurrence == failedMember ? failedStage : null)))
            .ToArray();

    private static ComponentGroupActivationResult Refuse(
        ComponentGroupActivationFailureKind kind,
        string code,
        string reason,
        Cm.OccurrenceId? member = null) =>
        new(null, Array.Empty<ComponentGroupMemberOutcome>(), new(kind, code, reason, member));
}

public sealed record ComponentGroupParticipant(
    ComponentGroupMember Member,
    IReadOnlyList<ComponentParticipantRequest> Participants);

public enum ComponentGroupAuthorityFailureKind
{
    IdentityNotDistinct,
    MemberAuthorityRefused,
    ActorMappingInconsistent,
    ActivationRefused,
}

public sealed record ComponentGroupAuthorityFailure(
    ComponentGroupAuthorityFailureKind Kind,
    string Code,
    string Reason,
    Cm.OccurrenceId? Member);

public sealed record ComponentGroupMemberAdmission(
    Cm.OccurrenceId Occurrence,
    IReadOnlyList<ComponentParticipantObservation> Participants,
    IReadOnlyList<Cm.LocalCapabilityGrant> Grants);

public sealed record ComponentGroupAuthorityResult(
    IReadOnlyList<ComponentGroupMemberAdmission> Admissions,
    IReadOnlyList<Cm.LocalCapabilityGrant> Grants,
    ComponentGroupActivationResult? Lifecycle,
    ComponentGroupAuthorityFailure? Failure)
{
    public bool IsActive => Failure is null && Lifecycle?.IsActive == true;
}

/// <summary>
/// Admits a participant set per member, then activates the members together.
/// </summary>
/// <remarks>
/// Authority is admitted against an occurrence rather than an activation attempt, because an
/// occurrence is durable and an attempt is not. The authority barrier is therefore earlier than the
/// release barrier rather than the same one: every set is admitted before any provider is contacted,
/// and Release still waits for every member to reach Ready.
/// </remarks>
public static class ComponentGroupAuthority
{
    public static async ValueTask<ComponentGroupAuthorityResult> ActivateAsync(
        Cm.ResolutionOutcome resolution,
        IReadOnlyList<ComponentGroupParticipant> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);

        var ordered = members
            .OrderBy(item => item.Member.Selection.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        var requests = ordered.SelectMany(item => item.Participants.Select(entry => entry.Request)).ToArray();
        if (ComponentParticipantAdmission.DistinctIdentities(requests) is { } collision)
        {
            return Refuse(
                ComponentGroupAuthorityFailureKind.IdentityNotDistinct,
                collision.Code,
                collision.Reason);
        }

        // Every set is admitted before any member is prepared: CM5 evaluation is effect-free, so a
        // refusal here costs nothing to undo.
        var admissions = new List<ComponentGroupMemberAdmission>();
        foreach (var member in ordered)
        {
            var step = ComponentParticipantAdmission.Admit(
                member.Member.Selection,
                member.Participants,
                runtimeRequest);
            if (step.Failure is { } refusal)
            {
                return new(
                    admissions,
                    Array.Empty<Cm.LocalCapabilityGrant>(),
                    null,
                    new(
                        ComponentGroupAuthorityFailureKind.MemberAuthorityRefused,
                        refusal.Code,
                        refusal.Reason,
                        member.Member.Selection.Occurrence));
            }

            admissions.Add(new(member.Member.Selection.Occurrence, step.Admissions, step.Grants));
        }

        if (ActorMapping(admissions) is { } inconsistent)
        {
            return new(
                admissions,
                Array.Empty<Cm.LocalCapabilityGrant>(),
                null,
                new(
                    ComponentGroupAuthorityFailureKind.ActorMappingInconsistent,
                    inconsistent.Code,
                    inconsistent.Reason,
                    null));
        }

        var grants = admissions
            .SelectMany(item => item.Grants)
            .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
            .ToArray();
        var lifecycle = await ComponentGroupLifecycle.ActivateAsync(
            resolution,
            ordered.Select(item => item.Member).ToArray(),
            runtimeRequest,
            cancellationToken).ConfigureAwait(false);
        return lifecycle.IsActive
            ? new(admissions, grants, lifecycle, null)
            : new(
                admissions,
                grants,
                lifecycle,
                new(
                    ComponentGroupAuthorityFailureKind.ActivationRefused,
                    lifecycle.Failure?.Code ?? "activation-not-active",
                    lifecycle.Failure?.Reason ?? "CBI12 did not release every member.",
                    lifecycle.Failure?.Member));
    }

    /// <summary>
    /// Across the activation, one participant holds one local Actor and one local Actor is held by
    /// one participant.
    /// </summary>
    /// <remarks>
    /// The same party participating in two members is legitimate and must map consistently; two
    /// parties arriving at one local Actor is the conflation CBI6 refuses within a set, and it is no
    /// less a conflation across members.
    /// </remarks>
    private static (string Code, string Reason)? ActorMapping(
        IReadOnlyList<ComponentGroupMemberAdmission> admissions)
    {
        var byParticipant = new Dictionary<Cm.ActorId, Cm.LocalActorReferenceId>();
        var byLocalActor = new Dictionary<Cm.LocalActorReferenceId, Cm.ActorId>();
        foreach (var observation in admissions
            .SelectMany(item => item.Participants)
            .OrderBy(item => item.Participant.Value, StringComparer.Ordinal))
        {
            var local = observation.Authority.Observation.Relationships[0].LocalActor;
            if (byParticipant.TryGetValue(observation.Participant, out var existing) && existing != local)
            {
                return (
                    "participant-actor-not-single",
                    $"Participant '{observation.Participant}' is mapped onto both '{existing}' and '{local}' in one activation.");
            }

            if (byLocalActor.TryGetValue(local, out var holder) && holder != observation.Participant)
            {
                return (
                    "local-actor-shared-across-members",
                    $"Participants '{holder}' and '{observation.Participant}' are both mapped onto the receiving-domain Actor '{local}'.");
            }

            byParticipant[observation.Participant] = local;
            byLocalActor[local] = observation.Participant;
        }

        return null;
    }

    private static ComponentGroupAuthorityResult Refuse(
        ComponentGroupAuthorityFailureKind kind,
        string code,
        string reason) =>
        new(
            Array.Empty<ComponentGroupMemberAdmission>(),
            Array.Empty<Cm.LocalCapabilityGrant>(),
            null,
            new(kind, code, reason, null));
}

/// <summary>
/// Projects one native CBI3 result into the canonical, data-only CBI4 comparison profile.
/// </summary>
public static class ComponentAuthorityComparison
{
    public static string Profile(string scenario, ComponentAuthorityIntegrationResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentNullException.ThrowIfNull(result);

        var root = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["scenario"] = scenario,
            ["active"] = result.IsActive,
            ["integrationFailure"] = IntegrationFailure(result.Failure),
            ["authority"] = Authority(result.Authority),
            ["lifecycle"] = Lifecycle(result.Lifecycle),
        };
        return root.ToJsonString();
    }

    public static string Digest(string profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(profile)));
    }

    private static JsonNode? IntegrationFailure(ComponentAuthorityIntegrationFailure? failure) =>
        failure is null
            ? null
            : new JsonObject
            {
                ["kind"] = failure.Kind switch
                {
                    ComponentAuthorityIntegrationFailureKind.MappingInvalid => "mapping-invalid",
                    ComponentAuthorityIntegrationFailureKind.AuthorityShapeUnsupported => "authority-shape-unsupported",
                    ComponentAuthorityIntegrationFailureKind.AuthorityRefused => "authority-refused",
                    ComponentAuthorityIntegrationFailureKind.LifecycleRefused => "lifecycle-refused",
                    _ => throw new ArgumentOutOfRangeException(nameof(failure)),
                },
                ["code"] = failure.Code,
            };

    private static JsonNode? Authority(Cm.AuthorityAdmissionOutcome? authority) =>
        authority is null
            ? null
            : new JsonObject
            {
                ["outcome"] = AuthorityOutcomeToken(authority.Kind),
                ["profileSha256"] = Digest(Cm.FakeAuthorityComparisonEndpoint.CanonicalProfile(authority)),
            };

    private static JsonNode? Lifecycle(ComponentBindingLifecycleResult? lifecycle)
    {
        if (lifecycle is null)
        {
            return null;
        }

        return new JsonObject
        {
            ["runtime"] = Runtime(lifecycle.Runtime),
            ["member"] = Member(lifecycle.Member),
            ["failure"] = LifecycleFailure(lifecycle.Failure),
        };
    }

    private static JsonNode? Runtime(Cm.ActivationRuntimeOutcome? runtime)
    {
        if (runtime is null)
        {
            return null;
        }

        var effects = runtime.Observation.Effects;
        return new JsonObject
        {
            ["kind"] = RuntimeOutcomeToken(runtime.Kind),
            ["failureKind"] = runtime.Failure is null ? null : RuntimeOutcomeToken(runtime.Failure.Kind),
            ["effects"] = new JsonObject
            {
                ["prepared"] = effects.Prepared,
                ["establishmentStarted"] = effects.EstablishmentStarted,
                ["actorEndpointEstablished"] = effects.ActorEndpointEstablished,
                ["lifecycleOperationExecuted"] = effects.LifecycleOperationExecuted,
                ["memberReportedReady"] = effects.MemberReportedReady,
                ["released"] = effects.Released,
                ["ordinaryInteractionAdmitted"] = effects.OrdinaryInteractionAdmitted,
                ["activeGenerationMutated"] = effects.ActiveGenerationMutated,
                ["retainedGenerationRetired"] = effects.RetainedGenerationRetired,
                ["rollbackAttempted"] = effects.RollbackAttempted,
                ["capabilityGranted"] = effects.CapabilityGranted,
            },
        };
    }

    private static JsonNode? Member(Portable.PortableCompositionMember? member)
    {
        if (member is null)
        {
            return null;
        }

        var facts = new SortedDictionary<string, string>(member.ResolutionFacts, StringComparer.Ordinal);
        if (member.Plan is { } plan)
        {
            foreach (var fact in plan.Facts.Where(item => item.Key != "planId"))
            {
                facts[fact.Key] = fact.Value;
            }
        }

        var factObject = new JsonObject();
        foreach (var fact in facts)
        {
            factObject[fact.Key] = fact.Value;
        }

        return new JsonObject
        {
            ["stage"] = Portable.PortableCompositionVocabulary.Token(member.Stage),
            ["ready"] = member.IsReady,
            ["released"] = member.IsReleased,
            ["facts"] = factObject,
        };
    }

    private static JsonNode? LifecycleFailure(ComponentBindingLifecycleFailure? failure) =>
        failure is null
            ? null
            : new JsonObject
            {
                ["kind"] = failure.Kind switch
                {
                    ComponentBindingLifecycleFailureKind.PreparationUnavailable => "preparation-unavailable",
                    ComponentBindingLifecycleFailureKind.PlanUnsupported => "plan-unsupported",
                    ComponentBindingLifecycleFailureKind.RuntimeRefusedBeforeStart => "runtime-refused-before-start",
                    ComponentBindingLifecycleFailureKind.PortableInterconnectionRefused => "portable-interconnection-refused",
                    ComponentBindingLifecycleFailureKind.PortableReleaseRefused => "portable-release-refused",
                    _ => throw new ArgumentOutOfRangeException(nameof(failure)),
                },
                ["code"] = failure.Code,
            };

    private static string AuthorityOutcomeToken(Cm.AuthorityAdmissionOutcomeKind kind) => kind switch
    {
        Cm.AuthorityAdmissionOutcomeKind.Admitted => "admitted",
        Cm.AuthorityAdmissionOutcomeKind.PartiallyAdmitted => "partially-admitted",
        Cm.AuthorityAdmissionOutcomeKind.Denied => "denied",
        Cm.AuthorityAdmissionOutcomeKind.InvalidRequest => "invalid-request",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string RuntimeOutcomeToken(Cm.ActivationRuntimeOutcomeKind kind) => kind switch
    {
        Cm.ActivationRuntimeOutcomeKind.Active => "active",
        Cm.ActivationRuntimeOutcomeKind.RolledBack => "rolled-back",
        Cm.ActivationRuntimeOutcomeKind.PreparationFailed => "preparation-failed",
        Cm.ActivationRuntimeOutcomeKind.EstablishmentFailed => "establishment-failed",
        Cm.ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover => "release-failed-before-cutover",
        Cm.ActivationRuntimeOutcomeKind.RollbackUnavailable => "rollback-unavailable",
        Cm.ActivationRuntimeOutcomeKind.RetainedGenerationCorrupted => "retained-generation-corrupted",
        Cm.ActivationRuntimeOutcomeKind.InvalidCm3Plan => "invalid-cm3-plan",
        Cm.ActivationRuntimeOutcomeKind.RestartScopeConflict => "restart-scope-conflict",
        Cm.ActivationRuntimeOutcomeKind.StageObservationConflict => "stage-observation-conflict",
        Cm.ActivationRuntimeOutcomeKind.InteractionRefused => "interaction-refused",
        Cm.ActivationRuntimeOutcomeKind.BindingObservationConflict => "binding-observation-conflict",
        Cm.ActivationRuntimeOutcomeKind.ChildPortClosed => "child-port-closed",
        Cm.ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired => "replacement-lifecycle-required",
        Cm.ActivationRuntimeOutcomeKind.HostAssistedOrderConflict => "host-assisted-order-conflict",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
