using Cm = Brontide.Reference.Experimental.ComponentManagement;
using Portable = Brontide.Reference.Experimental.Binding.Portable;
using System.Text;

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
