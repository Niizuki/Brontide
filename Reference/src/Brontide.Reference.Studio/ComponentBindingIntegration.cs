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

    private static bool TrySupportedGroup(
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

    private static IReadOnlyList<Cm.MemberStageOutcome> StageOutcomes(
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

        // Ordering by participant makes evaluation, observation, and grant order independent of
        // the order the caller happened to build the set in.
        var ordered = participants
            .OrderBy(item => item.Mapping.Participant.Value, StringComparer.Ordinal)
            .ToArray();
        if (ValidateSet(ordered, selection) is { } invalid)
        {
            return Refuse(
                ComponentParticipantAdmissionFailureKind.ParticipantSetInvalid,
                invalid.Code,
                invalid.Reason);
        }

        if (runtimeRequest.BindingExercises.Count > 0 ||
            ordered.Any(item => !SupportedShape(item.Request)))
        {
            return Refuse(
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
            return Refuse(
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
            return Refuse(
                ComponentParticipantAdmissionFailureKind.LocalIdentityConflict,
                "local-actor-conflict",
                $"Two participants were mapped onto the receiving-domain Actor '{sharedHolder}', which would merge their grants into one holder.",
                admissions);
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
                ComponentParticipantAdmissionFailureKind.LifecycleRefused,
                lifecycle.Failure?.Code ?? "lifecycle-not-active",
                lifecycle.Failure?.Reason ?? "CBI2 did not return a released Active member.",
                admissions,
                lifecycle);
        }

        var grants = admissions
            .SelectMany(item => item.Authority.Observation.Grants)
            .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
            .ToArray();
        return new(admissions, grants, lifecycle, null);
    }

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
