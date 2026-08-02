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

        return PrepareMember(member, providerSet.Scope.Value, selection);
    }

    /// <summary>
    /// Prepares one resolved member against one binding scope, once the position it belongs to has
    /// been checked.
    /// </summary>
    /// <remarks>
    /// The scope arrives as text because this is the parsing seam: CBI1 hands it the CM position's
    /// scope, while a wide position has one scope for several members and hands it the one its caller
    /// named for this member.
    /// </remarks>
    internal static ComponentBindingIntegrationResult PrepareMember(
        Cm.ProviderSetMember member,
        string scopeText,
        ComponentBindingSelection selection)
    {
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
            var scope = Portable.PortableBindingScopeId.Parse(scopeText);
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

    /// <summary>
    /// Checks that where CM2 placed these positions and what the request says about a child Port
    /// agree, before anything is prepared.
    /// </summary>
    /// <remarks>
    /// A Provider Set carries the Region and Port CM2 resolved it into, and until CBI22 nothing read
    /// either: a position resolved inside a child Port was flattened into an ordinary one and
    /// activated in whatever scope the caller's plan named, so CM4 never saw an attachment and the
    /// restart boundary the Port exists to give was silently dropped.
    /// </remarks>
    internal static (string Code, string Reason)? PortContainment(
        Cm.ResolutionOutcome resolution,
        IReadOnlyList<ComponentBindingSelection> selections,
        Cm.ChildActivationDeclaration? child)
    {
        if (resolution.Generation is not { } generation)
        {
            return null;
        }

        var contained = selections
            .Select(selection => (
                selection.Requirement,
                Port: generation.ProviderSets
                    .Where(item => item.Requirement == selection.Requirement)
                    .Select(item => item.ContainingPort)
                    .FirstOrDefault()))
            .OrderBy(item => item.Requirement.Value, StringComparer.Ordinal)
            .ToArray();

        if (child is null)
        {
            var inPort = contained.Where(item => item.Port is not null).ToArray();
            return inPort.Length == 0
                ? null
                : (
                    "member-port-contained",
                    $"CM2 resolved {string.Join(", ", inPort.Select(item => item.Requirement.Value))} inside Port '{inPort[0].Port}', which needs a child attachment rather than an ordinary activation.");
        }

        var loose = contained.Where(item => item.Port is null).ToArray();
        if (loose.Length > 0)
        {
            return (
                "member-not-port-contained",
                $"{string.Join(", ", loose.Select(item => item.Requirement.Value))} is not resolved inside any Port, so it has nothing to attach to Port '{child.Port}'.");
        }

        var foreign = contained.Where(item => item.Port != child.Port).ToArray();
        if (foreign.Length > 0)
        {
            return (
                "port-not-resolved",
                $"The attachment names Port '{child.Port}', but CM2 resolved {string.Join(", ", foreign.Select(item => item.Requirement.Value))} into '{foreign[0].Port}'.");
        }

        // The envelope, not the caller, says what the Port permits. CM2 refuses a sealed Port at
        // resolution, so the reachable disagreement is a caller claiming a runtime-open Port the
        // generation resolved as activation-open.
        var envelope = generation.Ports.FirstOrDefault(item => item.Port == child.Port);
        if (envelope is null)
        {
            return (
                "port-not-resolved",
                $"The completed generation carries no envelope for Port '{child.Port}'.");
        }

        return child.RuntimeOpen && envelope.Lifecycle != Cm.PortLifecycleMode.RuntimeOpen
            ? (
                "port-lifecycle-overstated",
                $"The attachment declares Port '{child.Port}' runtime-open; the resolved envelope declares it {envelope.Lifecycle}.")
            : null;
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

public enum ComponentMediatedTranslationKind
{
    Translated,
    Declined,
}

public sealed record ComponentMediatedSelection(
    Cm.RequirementId MediatedRequirement,
    ComponentBindingSelection Mediator);

public sealed record ComponentMediatedTranslationResult(
    ComponentMediatedTranslationKind Kind,
    Portable.PortableCompositionMember? Member,
    Cm.RequirementId? MediatedRequirement,
    Cm.MediationId? Mediation,
    string Code,
    string Reason)
{
    public bool IsTranslated => Kind == ComponentMediatedTranslationKind.Translated;
}

/// <summary>
/// Carries a CM2 position resolved with mediated exposure into portable preflight, by binding the
/// Component the Mediation is realized as.
/// </summary>
/// <remarks>
/// The portable seam refuses mediated exposure because "an erased Mediation still carries provenance,
/// deputy, and authority obligations", and that refusal is right and is not relaxed here: nothing
/// mediated is ever presented to it. CM2 requires a policy-bearing Mediation to be realized as a
/// dedicated Component, so the obligations have a holder, the holder is an ordinary Component, and
/// binding it erases nothing — the plan's provider fact names the mediator, which is who answers. A
/// static-host Mediation has no Component to reach and is refused.
/// </remarks>
public static class ComponentMediatedBinding
{
    public static ComponentMediatedTranslationResult Translate(
        Cm.ResolutionOutcome resolution,
        ComponentMediatedSelection selection)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);

        if (resolution.Generation is not { } generation)
        {
            return Decline(
                "resolution-not-complete",
                "CBI25 translates a position of a completed CM2 generation.");
        }

        var mediated = generation.ProviderSets
            .Where(item => item.Requirement == selection.MediatedRequirement)
            .ToArray();
        if (mediated.Length != 1)
        {
            return Decline(
                "mediated-position-not-resolved",
                $"The completed generation contains {mediated.Length} provider positions for requirement '{selection.MediatedRequirement}'.");
        }

        var position = mediated[0];
        if (position.Exposure != Cm.ProviderExposure.Mediated || position.Mediation is null)
        {
            return Decline(
                "position-not-mediated",
                $"Requirement '{selection.MediatedRequirement}' resolves a {position.Exposure} position, which CBI1 translates directly.");
        }

        var mediation = position.Mediation;
        if (mediation.Realization != Cm.MediationRealization.DedicatedComponent
            || mediation.Component is not { } mediator)
        {
            // A static-host Mediation is the composition root's own work over the members' direct
            // bindings; there is no Component for a binding to reach.
            return Decline(
                "mediation-not-a-component",
                $"Mediation '{mediation.Mediation}' is realized as {mediation.Realization} with no Component, so there is nothing for a binding to reach.");
        }

        // The erasure the seam warns about, arriving through the composition root instead: a mapping
        // that names one of the mediated members binds past the Mediation rather than to it.
        if (selection.Mediator.Definition != mediator)
        {
            return Decline(
                "mediator-not-declared",
                $"Mediation '{mediation.Mediation}' declares Component '{mediator}', and the mapping names '{selection.Mediator.Definition}'.");
        }

        var resolved = generation.ProviderSets
            .SelectMany(item => item.Members)
            .Any(item => item.Definition == mediator && item.Occurrence == selection.Mediator.Occurrence);
        if (!resolved)
        {
            return Decline(
                "mediator-not-resolved",
                $"The generation resolves no occurrence '{selection.Mediator.Occurrence}' for the mediator '{mediator}'.");
        }

        // From here it is an ordinary distinct position: the mediator's own. Nothing mediated is
        // presented to the seam, and the prepared member is indistinguishable from any other.
        var prepared = ComponentBindingIntegration.Prepare(resolution, selection.Mediator);
        return prepared.Member is { } member
            ? new(
                ComponentMediatedTranslationKind.Translated,
                member,
                selection.MediatedRequirement,
                mediation.Mediation,
                "mediator-bound",
                $"Mediation '{mediation.Mediation}' of '{selection.MediatedRequirement}' is bound through its Component '{mediator}'.")
            : Decline(prepared.Failure!.Code, prepared.Failure.Reason);
    }

    private static ComponentMediatedTranslationResult Decline(string code, string reason) =>
        new(ComponentMediatedTranslationKind.Declined, null, null, null, code, reason);
}

public enum ComponentProviderSetTranslationKind
{
    Translated,
    Unfilled,
    Declined,
}

/// <summary>One resolved member of a wide position, with the binding scope its binding will hold.</summary>
public sealed record ComponentProviderSetMemberSelection(
    Portable.PortableBindingScopeId Scope,
    ComponentBindingSelection Selection);

public sealed record ComponentProviderSetSelection(
    Cm.RequirementId Requirement,
    IReadOnlyList<ComponentProviderSetMemberSelection> Members);

public sealed record ComponentProviderSetMemberOutcome(
    Cm.OccurrenceId Occurrence,
    Portable.PortableCompositionMember Member);

public sealed record ComponentProviderSetTranslationResult(
    ComponentProviderSetTranslationKind Kind,
    IReadOnlyList<ComponentProviderSetMemberOutcome> Members,
    Cm.RequirementId? Requirement,
    Cm.BindingScopeId? PositionScope,
    Cm.Cardinality? Cardinality,
    int UnfilledOptionalPositions,
    string Code,
    string Reason)
{
    public bool IsTranslated => Kind == ComponentProviderSetTranslationKind.Translated;
}

/// <summary>
/// Carries a CM2 position whose cardinality is not <c>1..1</c> into portable preflight, as one
/// ordinary member per resolved member of the Provider Set.
/// </summary>
/// <remarks>
/// A Provider Set's members each have a representation the seam already holds — one provider
/// answering one contract — and the set does not: nothing in the seam says that these bindings answer
/// one requirement together. So the set stays here, where several members are already one activation,
/// and the seam is neither widened nor relaxed. What the fan-out needs and CM2 does not supply is a
/// binding scope per member: the portable scope names one binding and the seam tells a composition to
/// reject reuse, while a CM scope is a container holding one binding per member, distinguished by
/// BindingId. The caller therefore names each member's scope, as it already names every other portable
/// identity.
/// </remarks>
public static class ComponentProviderSetBinding
{
    public static ComponentProviderSetTranslationResult Translate(
        Cm.ResolutionOutcome resolution,
        ComponentProviderSetSelection selection)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);

        if (resolution.Generation is not { } generation)
        {
            return Decline(
                "resolution-not-complete",
                "CBI27 translates a position of a completed CM2 generation.");
        }

        var matches = generation.ProviderSets
            .Where(item => item.Requirement == selection.Requirement)
            .ToArray();
        if (matches.Length != 1)
        {
            return Decline(
                "wide-position-not-resolved",
                $"The completed generation contains {matches.Length} provider positions for requirement '{selection.Requirement}'.");
        }

        var position = matches[0];
        if (position.Cardinality.Minimum == 1 && position.Cardinality.Maximum == 1)
        {
            return Decline(
                "position-not-wide",
                $"Requirement '{selection.Requirement}' resolves a 1..1 position, which CBI1 translates directly.");
        }

        // Exposure and the declaration are two facts: CM2 records a Mediation on a distinct position
        // and ignores it, so checking only the first would leave the second unchecked.
        if (position.Exposure != Cm.ProviderExposure.Distinct || position.Mediation is not null)
        {
            return Decline(
                "position-mediated",
                position.Exposure != Cm.ProviderExposure.Distinct
                    ? $"Requirement '{selection.Requirement}' resolves a {position.Exposure} position, which CBI25 binds through the Component its Mediation is realized as."
                    : $"Requirement '{selection.Requirement}' is distinct but declares Mediation '{position.Mediation!.Mediation}', and CM2 records it without acting on it.");
        }

        var undirected = position.Members
            .Where(item => position.BindingPlans.Count(plan =>
                plan.Member == item.Occurrence && plan.Direct && plan.Mediation is null) != 1)
            .ToArray();
        if (position.BindingPlans.Count != position.Members.Count || undirected.Length > 0)
        {
            return Decline(
                "binding-not-direct",
                "The resolved position does not contain exactly one direct binding observation for each of its members.");
        }

        var foreign = selection.Members
            .Where(item => item.Selection.Requirement != selection.Requirement)
            .ToArray();
        if (foreign.Length > 0)
        {
            return Decline(
                "member-requirement-mismatch",
                $"A member mapping names requirement '{foreign[0].Selection.Requirement}', and this translation is of '{selection.Requirement}'.");
        }

        if (Membership(position, selection) is { } membership)
        {
            return Decline("membership-not-resolved", membership);
        }

        if (position.Members.Count == 0)
        {
            // Nothing to bind and nothing wrong. Reporting it as an empty translation would make it
            // indistinguishable from a position nobody translated.
            return new(
                ComponentProviderSetTranslationKind.Unfilled,
                Array.Empty<ComponentProviderSetMemberOutcome>(),
                selection.Requirement,
                position.Scope,
                position.Cardinality,
                position.OptionalPositionsUnfilled,
                "position-resolved-empty",
                $"Requirement '{selection.Requirement}' declares {position.Cardinality} and resolved no member, so it binds nothing.");
        }

        var scopes = selection.Members
            .GroupBy(item => item.Scope.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        if (scopes.Length > 0)
        {
            return Decline(
                "scope-not-distinct",
                $"Binding scope '{scopes[0].Key}' is named by {scopes[0].Count()} members, and a scope that two members hold is two bindings claiming one position.");
        }

        var prepared = new List<ComponentProviderSetMemberOutcome>();
        foreach (var entry in selection.Members
            .OrderBy(item => item.Selection.Occurrence.Value, StringComparer.Ordinal))
        {
            var member = position.Members.Single(item => item.Occurrence == entry.Selection.Occurrence);
            var preparation = ComponentBindingIntegration.PrepareMember(
                member,
                entry.Scope.Value,
                entry.Selection);
            if (preparation.Member is not { } portable)
            {
                // The seam refuses a wide cardinality rather than narrowing it to a first member, and
                // keeping the members that happened to work would be that narrowing performed here,
                // where the seam cannot see it.
                return Decline(preparation.Failure!.Code, preparation.Failure.Reason);
            }

            prepared.Add(new(entry.Selection.Occurrence, portable));
        }

        return new(
            ComponentProviderSetTranslationKind.Translated,
            prepared,
            selection.Requirement,
            position.Scope,
            position.Cardinality,
            position.OptionalPositionsUnfilled,
            "position-fanned-out",
            $"Requirement '{selection.Requirement}' declares {position.Cardinality} and resolved {prepared.Count} member(s), each bound in its own scope.");
    }

    /// <summary>Says how the supplied membership differs from the generation's, or nothing.</summary>
    private static string? Membership(
        Cm.ProviderSetObservation position,
        ComponentProviderSetSelection selection)
    {
        var repeated = selection.Members
            .GroupBy(item => item.Selection.Occurrence.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        if (repeated.Length > 0)
        {
            return $"Member {string.Join(", ", repeated)} is supplied more than once for requirement '{position.Requirement}'.";
        }

        var supplied = selection.Members
            .Select(item => item.Selection.Occurrence)
            .ToHashSet();
        var missing = position.Members
            .Where(item => !supplied.Contains(item.Occurrence))
            .Select(item => item.Occurrence.Value)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        if (missing.Length > 0)
        {
            return $"The generation resolves {string.Join(", ", missing)} for requirement '{position.Requirement}', and the translation was not given them.";
        }

        var resolved = position.Members.Select(item => item.Occurrence).ToHashSet();
        var unexpected = selection.Members
            .Select(item => item.Selection.Occurrence)
            .Where(item => !resolved.Contains(item))
            .Select(item => item.Value)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        return unexpected.Length == 0
            ? null
            : $"The generation resolves no member {string.Join(", ", unexpected)} for requirement '{position.Requirement}'.";
    }

    private static ComponentProviderSetTranslationResult Decline(string code, string reason) =>
        new(
            ComponentProviderSetTranslationKind.Declined,
            Array.Empty<ComponentProviderSetMemberOutcome>(),
            null,
            null,
            null,
            0,
            code,
            reason);
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

        if (ComponentBindingIntegration.PortContainment(resolution, [selection], request.Child) is { } containment)
        {
            return Refuse(
                ComponentBindingLifecycleFailureKind.PlanUnsupported,
                containment.Code,
                containment.Reason);
        }

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

    internal static string? SamePosition(
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

/// <summary>
/// One member of an activation: what to bind, what to bind it through, and — where the generation
/// cannot say — which binding scope the binding holds.
/// </summary>
/// <remarks>
/// The scope is absent for a member of a `1..1` position, where CM2's position scope names the one
/// binding it has, and present for a member of a wider one, where CM2 names the position and the
/// caller names each binding within it.
/// </remarks>
public sealed record ComponentGroupMember(
    ComponentBindingSelection Selection,
    Portable.IPortableProviderConversation Conversation,
    Portable.PortableBindingScopeId? Scope = null);

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
        if (UnsupportedPlan(runtimeRequest.Plan, ordered) is { } unsupported)
        {
            return Refuse(
                ComponentGroupActivationFailureKind.PlanUnsupported,
                unsupported.Code,
                unsupported.Reason);
        }

        if (ComponentBindingIntegration.PortContainment(
                resolution,
                ordered.Select(item => item.Selection).ToArray(),
                runtimeRequest.Child) is { } containment)
        {
            return Refuse(
                ComponentGroupActivationFailureKind.PlanUnsupported,
                containment.Code,
                containment.Reason);
        }

        var preparation = PrepareMembers(resolution, ordered);
        if (preparation.Failure is { } unprepared)
        {
            return Refuse(
                ComponentGroupActivationFailureKind.PreparationUnavailable,
                unprepared.Code,
                unprepared.Reason,
                unprepared.Member);
        }

        var prepared = preparation.Members;

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

    private sealed record PreparationRefusal(string Code, string Reason, Cm.OccurrenceId? Member);

    private sealed record MemberPreparation(
        IReadOnlyList<ComponentGroupMemberOutcome> Members,
        PreparationRefusal? Failure);

    /// <summary>
    /// Prepares every member of the activation, one position at a time.
    /// </summary>
    /// <remarks>
    /// A wide position goes through CBI27 as a whole rather than member by member, which is what makes
    /// the generation's membership the authority here: CBI12's plan checks compare the caller's member
    /// list with the caller's plan, so a position supplied half-complete satisfies both of them and
    /// only the resolution can say otherwise.
    /// </remarks>
    private static MemberPreparation PrepareMembers(
        Cm.ResolutionOutcome resolution,
        IReadOnlyList<ComponentGroupMember> ordered)
    {
        var prepared = new Dictionary<Cm.OccurrenceId, Portable.PortableCompositionMember>();
        foreach (var position in ordered
            .GroupBy(item => item.Selection.Requirement)
            .OrderBy(group => group.Key.Value, StringComparer.Ordinal))
        {
            var resolved = resolution.Generation?.ProviderSets
                .FirstOrDefault(item => item.Requirement == position.Key);
            var wide = resolved is not null &&
                (resolved.Cardinality.Minimum != 1 || resolved.Cardinality.Maximum != 1);
            if (!wide)
            {
                // The generation names the scope of a 1..1 position, so a caller naming one is
                // disagreeing with the resolution rather than completing it.
                if (position.FirstOrDefault(item => item.Scope is not null) is { } scoped)
                {
                    return Unprepared(
                        "member-scope-not-required",
                        $"Requirement '{position.Key}' resolves one binding, whose scope the generation names; member {scoped.Selection.Occurrence} also names '{scoped.Scope}'.",
                        scoped.Selection.Occurrence);
                }

                foreach (var member in position)
                {
                    var one = ComponentBindingIntegration.Prepare(resolution, member.Selection);
                    if (one.Member is not { } portable)
                    {
                        return Unprepared(
                            one.Failure!.Code,
                            one.Failure.Reason,
                            member.Selection.Occurrence);
                    }

                    prepared[member.Selection.Occurrence] = portable;
                }

                continue;
            }

            if (position.FirstOrDefault(item => item.Scope is null) is { } unscoped)
            {
                return Unprepared(
                    "member-scope-required",
                    $"Requirement '{position.Key}' resolves {resolved!.Cardinality} and CM2 gives the position one scope, so member {unscoped.Selection.Occurrence} needs a binding scope of its own.",
                    unscoped.Selection.Occurrence);
            }

            var translation = ComponentProviderSetBinding.Translate(
                resolution,
                new(
                    position.Key,
                    [.. position.Select(item => new ComponentProviderSetMemberSelection(
                        item.Scope!.Value,
                        item.Selection))]));
            if (!translation.IsTranslated)
            {
                return Unprepared(translation.Code, translation.Reason, null);
            }

            foreach (var outcome in translation.Members)
            {
                prepared[outcome.Occurrence] = outcome.Member;
            }
        }

        return new(
            [.. ordered.Select(item => new ComponentGroupMemberOutcome(
                item.Selection.Occurrence,
                prepared[item.Selection.Occurrence]))],
            null);
    }

    private static MemberPreparation Unprepared(string code, string reason, Cm.OccurrenceId? member) =>
        new(Array.Empty<ComponentGroupMemberOutcome>(), new(code, reason, member));

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

    private static (string Code, string Reason)? UnsupportedPlan(
        Cm.ActivationGroupPlan plan,
        IReadOnlyList<ComponentGroupMember> members) =>
        UnsupportedPlan(plan, members.Select(item => item.Selection.Occurrence).ToArray());

    /// <summary>
    /// Says why a CM3 plan cannot be activated across the portable seam, or nothing if it can.
    /// </summary>
    /// <remarks>
    /// The unit of refusal is a group's declared protocols, not its member count. CM3 groups by
    /// strongly connected component over every edge, so two Components with mutual ordinary
    /// interaction are one cyclic group that declares no protocol, no Relational Initialisation
    /// stage, and a stage plan CM4 activates — which is nothing this seam lacks. What it lacks is the
    /// stage itself: the Composition handoff declares Relational Initialisation out of scope, and a
    /// portable member is Ready the moment Interconnection returns, so there is no window before
    /// Ready in which a declared handshake could run.
    /// </remarks>
    internal static (string Code, string Reason)? UnsupportedPlan(
        Cm.ActivationGroupPlan plan,
        IReadOnlyList<Cm.OccurrenceId> occurrences)
    {
        if (ComponentParticipantAdmission.FirstDuplicate(
                occurrences.Select(item => item.Value)) is { } repeated)
        {
            return ("member-not-distinct", $"Occurrence '{repeated}' is selected more than once.");
        }

        var relational = plan.Groups.FirstOrDefault(group => group.Protocols.Count > 0);
        if (relational is not null)
        {
            return (
                "relational-initialisation-unsupported",
                $"Group '{relational.Group}' declares {relational.Protocols.Count} bounded lifecycle protocol(s), and Portable Binding declares Relational Initialisation outside the Composition handoff.");
        }

        var planned = plan.Groups
            .SelectMany(group => group.Members.Select(member => member.Occurrence))
            .ToHashSet();
        var unplanned = occurrences
            .Where(item => !planned.Contains(item))
            .OrderBy(item => item.Value, StringComparer.Ordinal)
            .ToArray();
        if (unplanned.Length > 0)
        {
            return (
                "member-not-planned",
                $"The CM3 plan carries no member for {string.Join(", ", unplanned.Select(item => item.Value))}.");
        }

        var selected = occurrences.ToHashSet();
        var unselected = planned
            .Where(item => !selected.Contains(item))
            .OrderBy(item => item.Value, StringComparer.Ordinal)
            .ToArray();
        return unselected.Length == 0
            ? null
            : (
                "member-not-selected",
                $"The CM3 plan carries {string.Join(", ", unselected.Select(item => item.Value))}, which this activation did not select.");
    }

    internal static IReadOnlyList<Cm.MemberStageOutcome> GroupStageOutcomes(
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
    internal static (string Code, string Reason)? ActorMapping(
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

public enum ComponentChildActivationKind
{
    Attached,
    Declined,
    ParentUnavailable,
}

public sealed record ComponentChildActivationResult(
    ComponentChildActivationKind Kind,
    ComponentGroupAuthorityResult? Child,
    Cm.PortId? Port,
    string Code,
    string Reason)
{
    public bool IsAttached => Kind == ComponentChildActivationKind.Attached;
}

/// <summary>
/// Activates a Component position CM2 resolved inside a child Port, in its own restart scope,
/// attached to the scope and generation a released parent activation made active.
/// </summary>
/// <remarks>
/// A child activation is a second activation rather than a replacement of the first: separate plan,
/// separate Release, separate restart scope, and a parent that CM4 requires to stay active and
/// unchanged throughout. What the attachment says about the Port is read from the resolved envelope
/// rather than from the caller, because the Port is where the generation placed the Component.
/// </remarks>
public static class ComponentChildActivation
{
    public static async ValueTask<ComponentChildActivationResult> AttachAsync(
        Cm.ResolutionOutcome resolution,
        ComponentGroupAuthorityResult parent,
        IReadOnlyList<ComponentGroupParticipant> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);

        if (!parent.IsActive)
        {
            return Decline(
                ComponentChildActivationKind.ParentUnavailable,
                "active-parent-unavailable",
                "CBI22 attaches a child to one released CBI13 activation.");
        }

        if (runtimeRequest.Child is not { } child)
        {
            return Decline(
                ComponentChildActivationKind.Declined,
                "child-attachment-missing",
                "CBI22 requires the CM4 request to declare the child attachment it is making.");
        }

        if (Attachment(parent, runtimeRequest, child) is { } invalid)
        {
            return Decline(ComponentChildActivationKind.Declined, invalid.Code, invalid.Reason);
        }

        // Where the generation put these positions is a structural question, so it is answered
        // before any authority is evaluated: a disagreement about the Port decides nothing about
        // whether the receiving domain would have admitted the child.
        if (ComponentBindingIntegration.PortContainment(
                resolution,
                members.Select(item => item.Member.Selection).ToArray(),
                child) is { } containment)
        {
            return Decline(ComponentChildActivationKind.Declined, containment.Code, containment.Reason);
        }

        var activation = await ComponentGroupAuthority.ActivateAsync(
            resolution,
            members,
            runtimeRequest,
            cancellationToken).ConfigureAwait(false);
        if (activation.IsActive)
        {
            return new(
                ComponentChildActivationKind.Attached,
                activation,
                child.Port,
                "child-attached",
                $"The child activation occupies scope '{runtimeRequest.Plan.RestartScope}' through Port '{child.Port}' of generation '{child.ParentGeneration}'.");
        }

        var failure = activation.Failure;
        return new(
            ComponentChildActivationKind.Declined,
            activation,
            child.Port,
            failure?.Kind == ComponentGroupAuthorityFailureKind.ActivationRefused
                ? ChildFailureCode(activation)
                : failure?.Code ?? "child-activation-refused",
            failure?.Reason ?? "The child activation did not release every member.");
    }

    /// <summary>
    /// Checks the attachment against what the parent activation actually made active, rather than
    /// against the caller's plan.
    /// </summary>
    private static (string Code, string Reason)? Attachment(
        ComponentGroupAuthorityResult parent,
        Cm.ActivationRuntimeRequest runtimeRequest,
        Cm.ChildActivationDeclaration child)
    {
        if (parent.Lifecycle?.Runtime?.Observation is not { } current)
        {
            return ("parent-generation-unknown", "The parent activation records no CM4 observation to attach to.");
        }

        if (child.ParentScope != current.RestartScope)
        {
            return (
                "parent-scope-mismatch",
                $"The parent activation occupies scope '{current.RestartScope}', not '{child.ParentScope}'.");
        }

        if (child.ParentGeneration != current.TargetGeneration)
        {
            return (
                "parent-generation-mismatch",
                $"Scope '{current.RestartScope}' holds generation '{current.TargetGeneration}', not '{child.ParentGeneration}'.");
        }

        return runtimeRequest.Plan.RestartScope == child.ParentScope
            ? (
                "child-scope-not-distinct",
                $"A child Port exists to give its Component a restart boundary, so its scope may not be the parent's '{child.ParentScope}'.")
            : null;
    }

    /// <summary>Names a child refusal as whichever layer classified it.</summary>
    /// <remarks>
    /// A containment disagreement is refused before CM4 runs at all, so its code has to be read from
    /// the plan refusal rather than from a runtime outcome that does not exist yet.
    /// </remarks>
    private static string ChildFailureCode(ComponentGroupAuthorityResult activation)
    {
        if (activation.Lifecycle?.Failure is
            { Kind: ComponentGroupActivationFailureKind.PlanUnsupported } refusal)
        {
            return refusal.Code;
        }

        return activation.Lifecycle?.Runtime?.Kind switch
        {
            Cm.ActivationRuntimeOutcomeKind.ChildPortClosed => "child-port-closed",
            Cm.ActivationRuntimeOutcomeKind.ReplacementLifecycleRequired => "replacement-lifecycle-required",
            Cm.ActivationRuntimeOutcomeKind.HostAssistedOrderConflict => "host-assisted-order-conflict",
            Cm.ActivationRuntimeOutcomeKind.RestartScopeConflict => "restart-scope-conflict",
            _ => "child-establishment-refused",
        };
    }

    private static ComponentChildActivationResult Decline(
        ComponentChildActivationKind kind,
        string code,
        string reason) =>
        new(kind, null, null, code, reason);
}

public enum ComponentAttachmentWithdrawalKind
{
    Withdrawn,
    CleanupFailed,
    Declined,
}

public sealed record ComponentAttachmentRetirement(
    Cm.RestartScopeId Scope,
    IReadOnlyList<Cm.OccurrenceId> Members,
    string? Cleanup);

public sealed record ComponentAttachmentWithdrawalResult(
    ComponentAttachmentWithdrawalKind Kind,
    IReadOnlyList<ComponentAttachmentRetirement> Retired,
    string Code,
    string Reason)
{
    public bool IsWithdrawn => Kind == ComponentAttachmentWithdrawalKind.Withdrawn;
}

/// <summary>
/// Stands down a set of attached activations, deepest first.
/// </summary>
/// <remarks>
/// CM4 requires a child's parent scope to be active when the child attaches and preserves it through
/// the activation, and that is the whole of the relationship it models: nothing records that a scope
/// has children, and nothing stands a child down when its parent goes. The ordering is therefore the
/// composition root's, and it can only order what it is given — a child the caller does not name is
/// invisible here, which the contract states rather than implies.
/// </remarks>
public static class ComponentAttachmentWithdrawal
{
    public static async ValueTask<ComponentAttachmentWithdrawalResult> WithdrawAsync(
        IReadOnlyList<ComponentGroupAuthorityResult> activations,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activations);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        var attachments = new List<(ComponentGroupAuthorityResult Activation, Cm.RestartScopeId Scope, Cm.RestartScopeId? Parent)>();
        foreach (var activation in activations)
        {
            if (!activation.IsActive || activation.Lifecycle?.Runtime?.Observation is not { } observation)
            {
                return Decline(
                    "activation-unavailable",
                    "CBI23 stands down released CBI13 activations, each carrying its own CM4 observation.");
            }

            attachments.Add((activation, observation.RestartScope, observation.Child?.ParentScope));
        }

        if (ComponentParticipantAdmission.FirstDuplicate(
                attachments.Select(item => item.Scope.Value)) is { } repeated)
        {
            return Decline(
                "scope-not-distinct",
                $"Two activations claim restart scope '{repeated}', so which one holds it is undecidable.");
        }

        var ordered = Depths(attachments);
        if (ordered is null)
        {
            return Decline(
                "attachment-cycle",
                "The attachment relation contains a cycle, so no deepest-first order exists.");
        }

        // Deepest first: an attachment occupies a Port of a generation, so it cannot outlive the
        // generation that offers the Port.
        var retired = new List<ComponentAttachmentRetirement>();
        var cleanup = new List<string>();
        foreach (var (activation, scope, _) in ordered)
        {
            var members = new List<Cm.OccurrenceId>();
            var failures = new List<string>();
            foreach (var outcome in activation.Lifecycle!.Members)
            {
                var (_, failure) = await ComponentParticipantRevalidation
                    .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                    .ConfigureAwait(false);
                members.Add(outcome.Occurrence);
                if (failure is not null)
                {
                    failures.Add($"{outcome.Occurrence}: {failure}");
                }
            }

            var detail = failures.Count == 0 ? null : string.Join("; ", failures);
            retired.Add(new(scope, members, detail));
            if (detail is not null)
            {
                cleanup.Add($"{scope}: {detail}");
            }
        }

        return cleanup.Count == 0
            ? new(
                ComponentAttachmentWithdrawalKind.Withdrawn,
                retired,
                "attachments-withdrawn",
                $"{retired.Count} attached scopes were retired, deepest first.")
            : new(
                // The cascade continues rather than stopping: restoring an already-retired level
                // would claim a state the runtime does not model.
                ComponentAttachmentWithdrawalKind.CleanupFailed,
                retired,
                "attachment-retirement-failed",
                string.Join("; ", cleanup));
    }

    /// <summary>
    /// Orders the set by how deep each activation sits within it, or reports that no order exists.
    /// </summary>
    /// <remarks>
    /// Depth counts only ancestors present in the supplied set, because those are the only ones this
    /// call can see. An activation whose parent is absent is a root here even when it is attached to
    /// something the caller left out.
    /// </remarks>
    private static List<(ComponentGroupAuthorityResult Activation, Cm.RestartScopeId Scope, Cm.RestartScopeId? Parent)>?
        Depths(
            List<(ComponentGroupAuthorityResult Activation, Cm.RestartScopeId Scope, Cm.RestartScopeId? Parent)> attachments)
    {
        var byScope = attachments.ToDictionary(item => item.Scope);
        var depths = new Dictionary<Cm.RestartScopeId, int>();
        foreach (var attachment in attachments)
        {
            var seen = new HashSet<Cm.RestartScopeId>();
            var depth = 0;
            var current = attachment;
            while (current.Parent is { } parent && byScope.TryGetValue(parent, out var next))
            {
                if (!seen.Add(current.Scope))
                {
                    return null;
                }

                depth++;
                current = next;
            }

            if (!seen.Add(current.Scope))
            {
                return null;
            }

            depths[attachment.Scope] = depth;
        }

        return
        [
            .. attachments
                .OrderByDescending(item => depths[item.Scope])
                .ThenBy(item => item.Scope.Value, StringComparer.Ordinal),
        ];
    }

    private static ComponentAttachmentWithdrawalResult Decline(string code, string reason) =>
        new(
            ComponentAttachmentWithdrawalKind.Declined,
            Array.Empty<ComponentAttachmentRetirement>(),
            code,
            reason);
}

public enum ComponentAttachedReplacementKind
{
    Replaced,
    CleanupFailed,
    Declined,
}

public sealed record ComponentAttachedReplacementResult(
    ComponentAttachedReplacementKind Kind,
    ComponentGroupReplacementResult? Replacement,
    IReadOnlyList<ComponentAttachmentRetirement> Cascaded,
    string Code,
    string Reason)
{
    public bool IsReplaced => Kind == ComponentAttachedReplacementKind.Replaced;
}

/// <summary>
/// Replaces the generation occupying one restart scope when child activations are attached to Ports
/// that generation offers.
/// </summary>
/// <remarks>
/// CM4 does nothing about them by design: its C2 property preserves the generation and activity state
/// of every unrelated scope, and a child scope is unrelated, so a cutover rewrites the target scope
/// and carries the child through untouched — leaving the attachment's recorded parent generation
/// pointing at one that is no longer active anywhere, with nothing that will ever look again. The
/// cascade therefore runs before the cutover rather than after it, which is the opposite order from
/// CBI19's retained members: those are inside the transaction and must keep serving until it
/// succeeds, while an attachment is outside it, in a scope CM4 will not touch either way.
/// </remarks>
public static class ComponentAttachedReplacement
{
    public static async ValueTask<ComponentAttachedReplacementResult> ReplaceAsync(
        Cm.ResolutionOutcome successor,
        ComponentGroupAuthorityResult retained,
        IReadOnlyList<ComponentGroupParticipant> members,
        IReadOnlyList<ComponentGroupAuthorityResult> attachments,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(retained);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!retained.IsActive || retained.Lifecycle?.Runtime?.Observation is not { } current)
        {
            return Decline(
                "active-authority-unavailable",
                "CBI24 replaces the generation one released CBI13 activation made active.");
        }

        // The replacement's own preconditions are checked before anything is stood down, so a
        // request that was never going to cut over does not cost the attachments their lives.
        if (ComponentGroupReplacement.Scope(retained, runtimeRequest) is { } invalid)
        {
            return Decline(invalid.Code, invalid.Reason);
        }

        // The supplied set is a forest beneath the retained generation, not a flat list of its
        // direct children: an attachment that must go takes everything beneath it, and CBI23 orders
        // the whole of it. So each one is either attached to the generation being replaced or to a
        // scope that is itself in the set.
        var supplied = new HashSet<Cm.RestartScopeId>();
        foreach (var attachment in attachments)
        {
            if (attachment.Lifecycle?.Runtime?.Observation.RestartScope is { } scope)
            {
                supplied.Add(scope);
            }
        }

        foreach (var attachment in attachments)
        {
            if (!attachment.IsActive
                || attachment.Lifecycle?.Runtime?.Observation.Child is not { } child
                || !((child.ParentGeneration == current.TargetGeneration
                        && child.ParentScope == current.RestartScope)
                    || supplied.Contains(child.ParentScope)))
            {
                return Decline(
                    "attachment-not-beneath-retained",
                    $"Every supplied activation must be released and attached either to generation '{current.TargetGeneration}' in scope '{current.RestartScope}' or to another scope in the set.");
            }
        }

        var cascade = attachments.Count == 0
            ? null
            : await ComponentAttachmentWithdrawal
                .WithdrawAsync(attachments, retirementReason, cancellationToken)
                .ConfigureAwait(false);
        if (cascade is { Kind: ComponentAttachmentWithdrawalKind.Declined })
        {
            return Decline(cascade.Code, cascade.Reason);
        }

        var cascaded = cascade?.Retired ?? Array.Empty<ComponentAttachmentRetirement>();
        if (cascade is { Kind: ComponentAttachmentWithdrawalKind.CleanupFailed })
        {
            // The attachments are down and one peer refused. Replacing on top of that would report a
            // cutover whose starting state nobody can describe.
            return new(
                ComponentAttachedReplacementKind.CleanupFailed,
                null,
                cascaded,
                cascade.Code,
                cascade.Reason);
        }

        var replacement = await ComponentGroupReplacement.ReplaceAsync(
            successor,
            retained,
            members,
            runtimeRequest,
            retirementReason,
            cancellationToken).ConfigureAwait(false);
        return replacement.Kind switch
        {
            ComponentGroupReplacementKind.Replaced => new(
                ComponentAttachedReplacementKind.Replaced,
                replacement,
                cascaded,
                "generation-replaced",
                $"{cascaded.Count} attached scopes were stood down, then the scope cut over to {runtimeRequest.Plan.Generation}."),
            ComponentGroupReplacementKind.CleanupFailed => new(
                ComponentAttachedReplacementKind.CleanupFailed,
                replacement,
                cascaded,
                replacement.Code,
                replacement.Reason),
            // The retained generation keeps serving, as CBI19 guarantees, but the attachments are
            // already gone and are not restored: standing one up again would be a fresh activation
            // against a generation this call did not establish.
            _ => new(
                ComponentAttachedReplacementKind.Declined,
                replacement,
                cascaded,
                replacement.Code,
                replacement.Reason),
        };
    }

    private static ComponentAttachedReplacementResult Decline(string code, string reason) =>
        new(
            ComponentAttachedReplacementKind.Declined,
            null,
            Array.Empty<ComponentAttachmentRetirement>(),
            code,
            reason);
}

public enum ComponentMediatorAuthorityKind
{
    Admitted,
    Declined,
}

public sealed record ComponentMediatorAuthorityResult(
    ComponentMediatorAuthorityKind Kind,
    IReadOnlyList<ComponentParticipantObservation> Admissions,
    IReadOnlyList<Cm.LocalCapabilityGrant> Grants,
    Cm.MediationId? Mediation,
    string Code,
    string Reason)
{
    public bool IsAdmitted => Kind == ComponentMediatorAuthorityKind.Admitted;
}

/// <summary>
/// Admits the authority of the mediator CBI25 binds, for what the mediator does itself.
/// </summary>
/// <remarks>
/// CM5 has no deputy. Its relationship kinds are AttachedDevice, ExternalPeer, and
/// ComponentParticipant, none of which means "acts on behalf of", and its grant names exactly one
/// Holder with no beneficiary beside it. So a Mediation declaring that it owns authority is refused
/// rather than approximated: admitting the mediator and letting its own grants stand for the members'
/// would decide what a deputy is, invisibly. The other ownership flags describe what the mediator
/// does with the set behind it, which is not a CM5 question at all.
/// </remarks>
public static class ComponentMediatorAuthority
{
    public static ComponentMediatorAuthorityResult Admit(
        Cm.ResolutionOutcome resolution,
        ComponentMediatedSelection selection,
        IReadOnlyList<ComponentParticipantRequest> participants,
        Cm.ActivationRuntimeRequest runtimeRequest)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(runtimeRequest);

        var translation = ComponentMediatedBinding.Translate(resolution, selection);
        if (!translation.IsTranslated)
        {
            return Decline(translation.Code, translation.Reason);
        }

        var mediation = resolution.Generation!.ProviderSets
            .Single(item => item.Requirement == selection.MediatedRequirement)
            .Mediation!;
        if (mediation.OwnsAuthority)
        {
            // CM5 can say who holds a grant and nothing about whom it is held for, so a Mediation
            // responsible for the authority of what it fronts has no representation here.
            return Decline(
                "mediation-owns-authority",
                $"Mediation '{mediation.Mediation}' declares that it owns authority, and CM5 has no relationship that means 'on behalf of' and no grant with a beneficiary.");
        }

        var admitted = ComponentParticipantAdmission.Admit(
            selection.Mediator,
            participants,
            runtimeRequest);
        return admitted.Failure is { } refusal
            ? new(
                ComponentMediatorAuthorityKind.Declined,
                admitted.Admissions,
                Array.Empty<Cm.LocalCapabilityGrant>(),
                null,
                refusal.Code,
                refusal.Reason)
            : new(
                ComponentMediatorAuthorityKind.Admitted,
                admitted.Admissions,
                admitted.Grants,
                mediation.Mediation,
                "mediator-admitted",
                $"Mediation '{mediation.Mediation}' is fronted by an admitted mediator holding {admitted.Grants.Count} grants of its own.");
    }

    private static ComponentMediatorAuthorityResult Decline(string code, string reason) =>
        new(
            ComponentMediatorAuthorityKind.Declined,
            Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.LocalCapabilityGrant>(),
            null,
            code,
            reason);
}

public sealed record ComponentGroupMemberRequests(
    Cm.OccurrenceId Occurrence,
    IReadOnlyList<Cm.AuthorityAdmissionRequest> Requests);

public enum ComponentGroupRevalidationKind
{
    Continued,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentGroupMemberRevalidation(
    Cm.OccurrenceId Occurrence,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.ActorId> Unrenewed);

public sealed record ComponentGroupRevalidationResult(
    ComponentGroupRevalidationKind Kind,
    IReadOnlyList<ComponentGroupMemberRevalidation> Members,
    IReadOnlyList<Cm.OccurrenceId> Lapsed,
    IReadOnlyList<Portable.PortableReplacementRecord> Replacements,
    string Code,
    string Reason)
{
    public bool IsActive => Kind == ComponentGroupRevalidationKind.Continued;
}

/// <summary>
/// Revalidates every member's authority and retires the whole activation when any of it lapses.
/// </summary>
/// <remarks>
/// A CM4 activation has one restart scope and every member is inside it, and CM4 models no way to
/// retire one member while its scope keeps running — that is a scoped replacement, a different
/// operation. The members came up together inside one scope, so they go down together. Their being
/// otherwise independent is about what they need from each other, not about what scope they share.
/// </remarks>
public static class ComponentGroupRevalidation
{
    public static async ValueTask<ComponentGroupRevalidationResult> RevalidateAsync(
        ComponentGroupAuthorityResult active,
        IReadOnlyList<ComponentGroupMemberRequests> requests,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle is not { } lifecycle)
        {
            return new(
                ComponentGroupRevalidationKind.ActivationUnavailable,
                Array.Empty<ComponentGroupMemberRevalidation>(),
                Array.Empty<Cm.OccurrenceId>(),
                Array.Empty<Portable.PortableReplacementRecord>(),
                "active-authority-unavailable",
                "CBI14 requires one released CBI13 activation with every member admitted.");
        }

        var prior = active.Admissions;
        var ordered = requests
            .OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select(item => item.Occurrence).SequenceEqual(prior.Select(item => item.Occurrence)))
        {
            return await RetireAllAsync(
                lifecycle,
                retirementReason,
                "member-set-changed",
                "The fresh requests do not name the same members the activation admitted.",
                Array.Empty<ComponentGroupMemberRevalidation>(),
                Array.Empty<Cm.OccurrenceId>(),
                cancellationToken).ConfigureAwait(false);
        }

        for (var index = 0; index < ordered.Length; index++)
        {
            var member = ordered[index];
            var admitted = prior[index].Participants;
            if (member.Requests.Count != admitted.Count ||
                member.Requests
                    .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
                    .Where((item, position) => !ComponentParticipantRevalidation.MatchesPrior(
                        admitted[position],
                        item))
                    .Any())
            {
                return await RetireAllAsync(
                    lifecycle,
                    retirementReason,
                    "authority-revalidation-mismatch",
                    $"A fresh request for member {member.Occurrence} does not identify the authority that admitted it.",
                    Array.Empty<ComponentGroupMemberRevalidation>(),
                    Array.Empty<Cm.OccurrenceId>(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var members = ordered
            .Select((member, index) =>
            {
                var admitted = prior[index].Participants;
                var current = member.Requests
                    .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
                    .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
                    .ToArray();
                var unrenewed = current
                    .Where((item, position) => !ComponentParticipantRevalidation.IsSameAdmission(
                        admitted[position].Authority,
                        item.Authority))
                    .Select(item => item.Participant)
                    .ToArray();
                return new ComponentGroupMemberRevalidation(member.Occurrence, current, unrenewed);
            })
            .ToArray();
        var lapsed = members
            .Where(item => item.Unrenewed.Count > 0)
            .Select(item => item.Occurrence)
            .ToArray();
        if (lapsed.Length > 0)
        {
            return await RetireAllAsync(
                lifecycle,
                retirementReason,
                "authority-not-renewed",
                $"The receiving domain no longer admits the identical authority for {string.Join(", ", lapsed.Select(item => item.Value))}.",
                members,
                lapsed,
                cancellationToken).ConfigureAwait(false);
        }

        return new(
            ComponentGroupRevalidationKind.Continued,
            members,
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Portable.PortableReplacementRecord>(),
            "authority-current",
            "Every member still holds the identical receiving-domain authority the activation admitted.");
    }

    private static async ValueTask<ComponentGroupRevalidationResult> RetireAllAsync(
        ComponentGroupActivationResult lifecycle,
        string retirementReason,
        string code,
        string reason,
        IReadOnlyList<ComponentGroupMemberRevalidation> members,
        IReadOnlyList<Cm.OccurrenceId> lapsed,
        CancellationToken cancellationToken)
    {
        var replacements = new List<Portable.PortableReplacementRecord>();
        var cleanup = new List<string>();
        foreach (var outcome in lifecycle.Members)
        {
            var (replacement, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            if (replacement is not null)
            {
                replacements.Add(replacement);
            }

            if (failure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {failure}");
            }
        }

        return cleanup.Count == 0
            ? new(ComponentGroupRevalidationKind.Withdrawn, members, lapsed, replacements, code, reason)
            : new(
                ComponentGroupRevalidationKind.RetirementFailed,
                members,
                lapsed,
                replacements,
                "authority-retirement-failed",
                string.Join("; ", cleanup));
    }
}

public sealed record ComponentGroupMemberRevision(
    Cm.OccurrenceId Occurrence,
    ComponentBindingSelection Selection,
    ComponentGrantDependency Dependency,
    IReadOnlyList<Cm.AuthorityAdmissionRequest> Requests);

public enum ComponentGroupRevisionKind
{
    Revised,
    Declined,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentGroupRevisionResult(
    ComponentGroupRevisionKind Kind,
    ComponentGroupAuthorityResult? InForce,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.OccurrenceId> Lapsed,
    string Code,
    string Reason)
{
    public bool IsRevised => Kind == ComponentGroupRevisionKind.Revised;
}

/// <summary>
/// Revises the participant sets of a multi-member activation under per-member declarations.
/// </summary>
/// <remarks>
/// A change is decided per member, because admission is about an occurrence, and checked against the
/// activation, because CBI13's identity and Actor-mapping rules are activation-wide. A declined
/// change is local and alters nothing; a lapse discovered while evaluating is CBI14's case and
/// retires the whole activation, which shares a restart scope.
/// </remarks>
public static class ComponentGroupRevision
{
    public static async ValueTask<ComponentGroupRevisionResult> ReviseAsync(
        Cm.ResolutionOutcome resolution,
        ComponentGroupAuthorityResult active,
        IReadOnlyList<ComponentGroupMemberRevision> members,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle is not { } lifecycle)
        {
            return new(
                ComponentGroupRevisionKind.ActivationUnavailable,
                null,
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.OccurrenceId>(),
                "active-authority-unavailable",
                "CBI15 requires one released CBI13 activation with every member admitted.");
        }

        var prior = active.Admissions;
        var ordered = members.OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal).ToArray();
        if (!ordered.Select(item => item.Occurrence).SequenceEqual(prior.Select(item => item.Occurrence)))
        {
            // Naming the wrong members is a malformed request, not evidence about authority, so the
            // activation is left exactly as it was. CBI14 retires in the same situation because a
            // revalidation asserts continuity it then cannot demonstrate.
            return Decline(active, "member-set-changed", "The revision does not name the members this activation admitted.");
        }

        if (ordered.Where((item, index) => !SetChanged(prior[index], item)).Count() == ordered.Length)
        {
            return Decline(
                active,
                "activation-unchanged",
                "No member's participant set differs; revalidating what is in force is CBI14.");
        }

        for (var index = 0; index < ordered.Length; index++)
        {
            var member = ordered[index];
            if (ComponentParticipantRevision.DeclarationShape(resolution, member.Selection, member.Dependency) is { } invalid)
            {
                return Decline(active, invalid.Code, invalid.Reason);
            }

            var uncovered = ComponentParticipantRevision.Uncovered(member.Dependency, prior[index].Grants);
            if (uncovered.Length > 0)
            {
                return Decline(
                    active,
                    "dependency-unsatisfied",
                    $"Member {member.Occurrence} does not cover declared authority {string.Join(", ", uncovered)}.");
            }
        }

        var intended = ordered.SelectMany(item => item.Requests).ToArray();
        if (ComponentParticipantAdmission.DistinctIdentities(intended) is { } collision)
        {
            return Decline(active, collision.Code, collision.Reason);
        }

        foreach (var member in ordered)
        {
            if (member.Requests.Count == 0 ||
                ComponentParticipantAdmission.FirstDuplicate(
                    member.Requests.Select(item => item.Participant.Value)) is not null)
            {
                return Decline(
                    active,
                    "participant-set-invalid",
                    $"Member {member.Occurrence} must keep at least one participant, each named once.");
            }
        }

        var priorByMember = prior.ToDictionary(item => item.Occurrence);
        foreach (var member in ordered)
        {
            var admitted = priorByMember[member.Occurrence].Participants
                .ToDictionary(item => item.Participant);
            if (member.Requests
                .Where(item => admitted.ContainsKey(item.Participant))
                .Any(item => !ComponentParticipantRevalidation.MatchesPrior(admitted[item.Participant], item)))
            {
                return Decline(
                    active,
                    "authority-revalidation-mismatch",
                    $"A retained request for member {member.Occurrence} does not identify the authority that admitted it.");
            }
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var evaluated = ordered
            .Select(member => (
                Member: member,
                Observations: member.Requests
                    .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
                    .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
                    .ToArray()))
            .ToArray();
        var current = evaluated.SelectMany(item => item.Observations).ToArray();

        var lapsed = evaluated
            .Where(item => item.Observations.Any(observation =>
                priorByMember[item.Member.Occurrence].Participants
                    .Any(admitted => admitted.Participant == observation.Participant &&
                        !ComponentParticipantRevalidation.IsSameAdmission(admitted.Authority, observation.Authority))))
            .Select(item => item.Member.Occurrence)
            .ToArray();
        if (lapsed.Length > 0)
        {
            // A lapse is CBI14's case, not this one: the activation shares a restart scope.
            return await RetireAsync(
                lifecycle,
                retirementReason,
                current,
                lapsed,
                $"The receiving domain no longer admits the identical authority for {string.Join(", ", lapsed.Select(item => item.Value))}.",
                cancellationToken).ConfigureAwait(false);
        }

        foreach (var (member, observations) in evaluated)
        {
            var admitted = priorByMember[member.Occurrence].Participants
                .Select(item => item.Participant)
                .ToHashSet();
            var refused = member.Requests
                .Where(item => !admitted.Contains(item.Participant))
                .Where((item, position) => !ComponentParticipantAdmission.IsExactAdmission(
                    observations.Single(observation => observation.Participant == item.Participant).Authority,
                    item))
                .Select(item => item.Participant.Value)
                .ToArray();
            if (refused.Length > 0)
            {
                return Decline(
                    active,
                    "authority-not-admitted",
                    $"CM5 did not admit the exact submitted authority for {string.Join(", ", refused)}.",
                    current);
            }
        }

        var revised = evaluated
            .Select(item => new ComponentGroupMemberAdmission(
                item.Member.Occurrence,
                item.Observations,
                item.Observations
                    .SelectMany(observation => observation.Authority.Observation.Grants)
                    .OrderBy(grant => grant.Grant.Value, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
        if (ComponentGroupAuthority.ActorMapping(revised) is { } inconsistent)
        {
            return Decline(active, inconsistent.Code, inconsistent.Reason, current);
        }

        foreach (var (member, _) in evaluated)
        {
            var grants = revised.Single(item => item.Occurrence == member.Occurrence).Grants;
            var uncovered = ComponentParticipantRevision.Uncovered(member.Dependency, grants);
            if (uncovered.Length > 0)
            {
                return Decline(
                    active,
                    "dependency-not-covered",
                    $"Member {member.Occurrence} would hold no grant satisfying declared authority {string.Join(", ", uncovered)}.",
                    current);
            }
        }

        return new(
            ComponentGroupRevisionKind.Revised,
            active with
            {
                Admissions = revised,
                Grants = revised
                    .SelectMany(item => item.Grants)
                    .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
                    .ToArray(),
            },
            current,
            Array.Empty<Cm.OccurrenceId>(),
            "activation-revised",
            $"{revised.Length} members now hold {revised.Sum(item => item.Participants.Count)} participants.");
    }

    private static bool SetChanged(
        ComponentGroupMemberAdmission prior,
        ComponentGroupMemberRevision intended) =>
        !intended.Requests
            .Select(item => item.Participant.Value)
            .OrderBy(item => item, StringComparer.Ordinal)
            .SequenceEqual(
                prior.Participants
                    .Select(item => item.Participant.Value)
                    .OrderBy(item => item, StringComparer.Ordinal),
                StringComparer.Ordinal);

    private static async ValueTask<ComponentGroupRevisionResult> RetireAsync(
        ComponentGroupActivationResult lifecycle,
        string retirementReason,
        IReadOnlyList<ComponentParticipantObservation> current,
        IReadOnlyList<Cm.OccurrenceId> lapsed,
        string reason,
        CancellationToken cancellationToken)
    {
        var cleanup = new List<string>();
        foreach (var outcome in lifecycle.Members)
        {
            var (_, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            if (failure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {failure}");
            }
        }

        return cleanup.Count == 0
            ? new(ComponentGroupRevisionKind.Withdrawn, null, current, lapsed, "authority-not-renewed", reason)
            : new(
                ComponentGroupRevisionKind.RetirementFailed,
                null,
                current,
                lapsed,
                "authority-retirement-failed",
                string.Join("; ", cleanup));
    }

    private static ComponentGroupRevisionResult Decline(
        ComponentGroupAuthorityResult active,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? current = null) =>
        new(
            ComponentGroupRevisionKind.Declined,
            active,
            current ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.OccurrenceId>(),
            code,
            reason);
}

public sealed record ComponentGroupMemberInteractions(
    ComponentBindingSelection Selection,
    ComponentGrantDependency Dependency,
    IReadOnlyList<ComponentOperationAuthorityMapping> Attribution,
    IReadOnlyList<ComponentObservedInteraction> Observations);

public enum ComponentGroupVerificationKind
{
    Consistent,
    UndeclaredUse,
    UngrantedUse,
    RetirementFailed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentGroupMemberVerification(
    Cm.OccurrenceId Occurrence,
    IReadOnlyList<Cm.BindingExerciseDeclaration> Exercises,
    IReadOnlyList<string> Unexercised,
    IReadOnlyList<string> Uncovered,
    bool UndeclaredUse,
    bool UngrantedUse)
{
    public bool IsViolating => UndeclaredUse || UngrantedUse;
}

public sealed record ComponentGroupVerificationResult(
    ComponentGroupVerificationKind Kind,
    Cm.ActivationRuntimeOutcome? Runtime,
    IReadOnlyList<ComponentGroupMemberVerification> Members,
    IReadOnlyList<Cm.OccurrenceId> Violating,
    IReadOnlyList<Portable.PortableReplacementRecord> Replacements,
    string Code,
    string Reason)
{
    public bool IsConsistent => Kind == ComponentGroupVerificationKind.Consistent;

    public IReadOnlyList<Cm.BindingExerciseDeclaration> Exercises =>
        Members.SelectMany(item => item.Exercises).ToArray();
}

/// <summary>
/// Verifies every member's declaration against what that member actually did, through one CM4
/// request carrying the whole activation's projected binding exercises.
/// </summary>
/// <remarks>
/// A CBI12 activation is one CM4 request, so one member's undeclared use condemns all of them: CM4
/// refuses the request on the first offending exercise rather than excusing the members that
/// behaved. The answer comes from the runtime's shape, as CBI12's release barrier did, and agrees
/// with CBI14's separate reason that the activation shares a restart scope. Attribution stays per
/// member, because the declaration is per member.
/// </remarks>
public static class ComponentGroupVerification
{
    public static async ValueTask<ComponentGroupVerificationResult> VerifyAsync(
        Cm.ResolutionOutcome resolution,
        ComponentGroupAuthorityResult active,
        IReadOnlyList<ComponentGroupMemberInteractions> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle is not { } lifecycle)
        {
            return Decline(
                ComponentGroupVerificationKind.ActivationUnavailable,
                "active-authority-unavailable",
                "CBI16 requires one released CBI13 activation with every member admitted.");
        }

        var prior = active.Admissions;
        var ordered = members
            .OrderBy(item => item.Selection.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select(item => item.Selection.Occurrence)
            .SequenceEqual(prior.Select(item => item.Occurrence)))
        {
            return Decline(
                ComponentGroupVerificationKind.Declined,
                "member-set-changed",
                "The verification does not name the members this activation admitted.");
        }

        foreach (var member in ordered)
        {
            if (ComponentParticipantRevision.DeclarationShape(
                    resolution,
                    member.Selection,
                    member.Dependency) is { } invalid)
            {
                return Decline(ComponentGroupVerificationKind.Declined, invalid.Code, invalid.Reason);
            }

            // Distinct within a member only: two Components may both expose an Operation of the
            // same name, and each attributes it against its own declaration.
            if (ComponentParticipantAdmission.FirstDuplicate(
                    member.Attribution.Select(item => item.Operation.ToString())) is { } repeated)
            {
                return Decline(
                    ComponentGroupVerificationKind.Declined,
                    "operation-mapping-not-distinct",
                    $"Member {member.Selection.Occurrence} attributes Operation '{repeated}' to more than one declared authority.");
            }
        }

        if (ComponentGroupLifecycle.UnsupportedPlan(
                runtimeRequest.Plan,
                prior.Select(item => item.Occurrence).ToArray()) is not null)
        {
            return Decline(
                ComponentGroupVerificationKind.Declined,
                "plan-unsupported",
                "CBI16 projects exercises onto the protocol-free activation groups CBI12 activated.");
        }

        var verified = ordered
            .Select((member, index) => Project(member, prior[index].Grants))
            .ToArray();

        // One request, one verdict: every member's exercises are judged together.
        var runtime = new Cm.FakeActivationRuntime().Activate(runtimeRequest with
        {
            StageOutcomes = ComponentGroupLifecycle.GroupStageOutcomes(runtimeRequest.Plan, null, null),
            BindingExercises = verified.SelectMany(item => item.Exercises).ToArray(),
        });

        var violating = verified
            .Where(item => item.IsViolating)
            .Select(item => item.Occurrence)
            .ToArray();
        if (violating.Length == 0)
        {
            return new(
                ComponentGroupVerificationKind.Consistent,
                runtime,
                verified,
                violating,
                Array.Empty<Portable.PortableReplacementRecord>(),
                "interaction-consistent",
                $"{verified.Sum(item => item.Exercises.Count)} delivered interaction(s) across {verified.Length} members stayed inside their declarations.");
        }

        var undeclared = verified.Any(item => item.UndeclaredUse);
        var replacements = new List<Portable.PortableReplacementRecord>();
        var cleanup = new List<string>();
        foreach (var outcome in lifecycle.Members)
        {
            var (replacement, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            if (replacement is not null)
            {
                replacements.Add(replacement);
            }

            if (failure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {failure}");
            }
        }

        return cleanup.Count == 0
            ? new(
                undeclared
                    ? ComponentGroupVerificationKind.UndeclaredUse
                    : ComponentGroupVerificationKind.UngrantedUse,
                runtime,
                verified,
                violating,
                replacements,
                undeclared ? "interaction-undeclared" : "interaction-ungranted",
                undeclared
                    ? $"A delivered interaction of {string.Join(", ", violating.Select(item => item.Value))} could not be attributed to any authority that member declared."
                    : $"A delivered interaction of {string.Join(", ", violating.Select(item => item.Value))} exercised declared authority no participant of that member holds a grant for.")
            : new(
                ComponentGroupVerificationKind.RetirementFailed,
                runtime,
                verified,
                violating,
                replacements,
                "authority-retirement-failed",
                string.Join("; ", cleanup));
    }

    /// <summary>
    /// Projects one member's observations, deriving each exercise's admission from that member's own
    /// declaration and its own grants.
    /// </summary>
    /// <remarks>
    /// Exercise identity carries the occurrence because CM4 refuses a request with a repeated
    /// binding-exercise identity, and the whole activation now shares one request.
    /// </remarks>
    private static ComponentGroupMemberVerification Project(
        ComponentGroupMemberInteractions member,
        IReadOnlyList<Cm.LocalCapabilityGrant> grants)
    {
        var occurrence = member.Selection.Occurrence;
        var declared = member.Dependency.Entries
            .Select(item => item.DeclaredAuthority)
            .ToHashSet(StringComparer.Ordinal);
        var uncoveredNames = ComponentParticipantRevision.Uncovered(member.Dependency, grants)
            .ToHashSet(StringComparer.Ordinal);
        var attributed = ComponentInteractionVerification.Attribute(
            member.Attribution,
            member.Observations);

        var exercises = attributed
            .Select((name, index) => new Cm.BindingExerciseDeclaration(
                Cm.BindingExerciseId.Create($"exercise.observed.{occurrence.Value}.{index + 1}"),
                Cm.BindingId.Create($"binding.{occurrence.Value}"),
                occurrence,
                occurrence,
                Cm.SourceId.Create("source.portable-observation"),
                Cm.BindingExposureKind.Distinct,
                null,
                Cm.RoutingDecisionId.Create($"routing.observed.{occurrence.Value}.{index + 1}"),
                name is not null && declared.Contains(name) && !uncoveredNames.Contains(name),
                Cm.BindingDeliveryResult.Delivered,
                null))
            .ToArray();

        var exercised = attributed
            .Where(name => name is not null && declared.Contains(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
        return new(
            occurrence,
            exercises,
            member.Dependency.Entries
                .Select(item => item.DeclaredAuthority)
                .Where(name => !exercised.Contains(name))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray(),
            uncoveredNames.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
            attributed.Any(name => name is null || !declared.Contains(name)),
            attributed.Any(name => name is not null && uncoveredNames.Contains(name)));
    }

    private static ComponentGroupVerificationResult Decline(
        ComponentGroupVerificationKind kind,
        string code,
        string reason) =>
        new(
            kind,
            null,
            Array.Empty<ComponentGroupMemberVerification>(),
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Portable.PortableReplacementRecord>(),
            code,
            reason);
}

public enum ComponentGroupReplacementKind
{
    Replaced,
    CleanupFailed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentGroupReplacementResult(
    ComponentGroupReplacementKind Kind,
    ComponentGroupAuthorityResult? Successor,
    bool CutOver,
    IReadOnlyList<Cm.OccurrenceId> Retired,
    string Code,
    string Reason)
{
    public bool IsReplaced => Kind == ComponentGroupReplacementKind.Replaced;
}

/// <summary>
/// Replaces the generation occupying one restart scope with a successor generation, and cuts the
/// scope over to it.
/// </summary>
/// <remarks>
/// CM4's scoped replacement swaps a whole generation atomically: one Release for the attempt, one
/// cutover for the scope, and no operation anywhere that retires one member while its scope keeps
/// running. Authority follows the occurrence rather than the attempt — CBI13's own justification,
/// finally exercised — and is re-established in this attempt rather than inherited. The retained
/// members are retired only after cutover, because a failure before it must leave them serving.
/// </remarks>
public static class ComponentGroupReplacement
{
    public static async ValueTask<ComponentGroupReplacementResult> ReplaceAsync(
        Cm.ResolutionOutcome successor,
        ComponentGroupAuthorityResult retained,
        IReadOnlyList<ComponentGroupParticipant> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(retained);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!retained.IsActive || retained.Lifecycle is not { } lifecycle)
        {
            return Decline(
                ComponentGroupReplacementKind.ActivationUnavailable,
                "active-authority-unavailable",
                "CBI19 requires one released CBI13 activation to replace.");
        }

        if (Scope(retained, runtimeRequest) is { } invalid)
        {
            return Decline(ComponentGroupReplacementKind.Declined, invalid.Code, invalid.Reason);
        }

        // Both limits this slice declares, checked rather than assumed: the members are the
        // generation's positions, and they are the ones the retained activation already holds.
        if (Membership(successor, members) is { } disagreement)
        {
            return Decline(
                ComponentGroupReplacementKind.Declined,
                disagreement.Code,
                disagreement.Reason);
        }

        if (Changed(retained, members) is { } changed)
        {
            return Decline(ComponentGroupReplacementKind.Declined, changed.Code, changed.Reason);
        }

        return await ReplaceCoreAsync(
            successor,
            retained,
            lifecycle,
            members,
            runtimeRequest,
            retirementReason,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces the generation without deciding whether the membership may differ, so a membership
    /// replacement reaches the same cutover rather than restating it.
    /// </summary>
    internal static async ValueTask<ComponentGroupReplacementResult> ReplaceCoreAsync(
        Cm.ResolutionOutcome successor,
        ComponentGroupAuthorityResult retained,
        ComponentGroupActivationResult lifecycle,
        IReadOnlyList<ComponentGroupParticipant> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken)
    {
        // A surviving occurrence keeps whatever it was admitted for: the replacement may not change
        // it quietly. Authority is still re-established, never inherited.
        var admitted = retained.Admissions.ToDictionary(item => item.Occurrence);
        foreach (var member in members
            .OrderBy(item => item.Member.Selection.Occurrence.Value, StringComparer.Ordinal))
        {
            if (!admitted.TryGetValue(member.Member.Selection.Occurrence, out var prior))
            {
                continue;
            }

            if (member.Participants.Count != prior.Participants.Count ||
                member.Participants
                    .OrderBy(item => item.Request.Participant.Value, StringComparer.Ordinal)
                    .Where((item, index) => !ComponentParticipantRevalidation.MatchesPrior(
                        prior.Participants[index],
                        item.Request))
                    .Any())
            {
                return Decline(
                    ComponentGroupReplacementKind.Declined,
                    "authority-revalidation-mismatch",
                    $"Occurrence {member.Member.Selection.Occurrence} survives this replacement, so it must be admitted with the authority that admitted it.");
            }
        }

        // The successor stands up under CBI13's barriers: every set admitted before any successor
        // provider is contacted, then one Release once every member is Ready.
        var activation = await ComponentGroupAuthority.ActivateAsync(
            successor,
            members,
            runtimeRequest,
            cancellationToken).ConfigureAwait(false);
        if (!activation.IsActive)
        {
            // Before cutover the retained activation was never stood down; it is still serving.
            var failure = activation.Failure;
            return new(
                ComponentGroupReplacementKind.Declined,
                activation,
                false,
                Array.Empty<Cm.OccurrenceId>(),
                failure?.Kind == ComponentGroupAuthorityFailureKind.ActivationRefused
                    ? SuccessorFailureCode(activation)
                    : failure?.Code ?? "successor-activation-refused",
                failure?.Reason ?? "The successor activation did not release every member.");
        }

        // Cutover happened. Only now may the retained members be stood down.
        var cleanup = new List<string>();
        var retiredMembers = new List<Cm.OccurrenceId>();
        foreach (var outcome in lifecycle.Members)
        {
            var (_, cleanupFailure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            retiredMembers.Add(outcome.Occurrence);
            if (cleanupFailure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {cleanupFailure}");
            }
        }

        return cleanup.Count == 0
            ? new(
                ComponentGroupReplacementKind.Replaced,
                activation,
                true,
                retiredMembers,
                "activation-replaced",
                $"The scope cut over to {runtimeRequest.Plan.Generation}; {retiredMembers.Count} retained members were retired.")
            : new(
                // The scope has already cut over, so the successor stays released and the cleanup
                // failure is named rather than swallowed.
                ComponentGroupReplacementKind.CleanupFailed,
                activation,
                true,
                retiredMembers,
                "retained-retirement-failed",
                string.Join("; ", cleanup));
    }

    /// <summary>Every position the completed successor generation resolves, in a stable order.</summary>
    internal static IReadOnlyList<(Cm.RequirementId Requirement, Cm.OccurrenceId Occurrence)> Positions(
        Cm.ResolutionOutcome successor) =>
        successor.Generation is not { } generation
            ? Array.Empty<(Cm.RequirementId, Cm.OccurrenceId)>()
            : generation.ProviderSets
                .SelectMany(set => set.Members.Select(member => (set.Requirement, member.Occurrence)))
                .OrderBy(item => item.Requirement.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Occurrence.Value, StringComparer.Ordinal)
                .ToArray();

    /// <summary>
    /// Checks that the supplied members are exactly the positions the successor generation resolves.
    /// </summary>
    /// <remarks>
    /// The membership is the generation's statement, not the caller's. Without this a caller could
    /// omit a position the successor still resolves and cut a scope over to a generation whose plan
    /// covers fewer members than CM2 resolved, with the omitted Component retired and no refusal
    /// anywhere.
    /// </remarks>
    internal static (string Code, string Reason)? Membership(
        Cm.ResolutionOutcome successor,
        IReadOnlyList<ComponentGroupParticipant> members)
    {
        if (successor.Generation is null)
        {
            return (
                "resolution-not-complete",
                "Replacing a generation requires a completed CM2 successor generation.");
        }

        var resolved = Positions(successor);
        var supplied = members
            .Select(item => (item.Member.Selection.Requirement, item.Member.Selection.Occurrence))
            .ToHashSet();
        foreach (var position in resolved)
        {
            if (!supplied.Contains(position))
            {
                return (
                    "position-not-supplied",
                    $"The successor generation resolves requirement '{position.Requirement}' at occurrence '{position.Occurrence}', which no supplied member names.");
            }
        }

        var known = resolved.ToHashSet();
        foreach (var member in members
            .OrderBy(item => item.Member.Selection.Occurrence.Value, StringComparer.Ordinal))
        {
            var selection = member.Member.Selection;
            if (!known.Contains((selection.Requirement, selection.Occurrence)))
            {
                return (
                    "member-not-resolved",
                    $"Member '{selection.Occurrence}' names requirement '{selection.Requirement}', which the successor generation does not resolve at that occurrence.");
            }
        }

        return null;
    }

    /// <summary>
    /// Checks the limit this slice declares: the successor resolves the positions the retained
    /// activation already holds.
    /// </summary>
    internal static (string Code, string Reason)? Changed(
        ComponentGroupAuthorityResult retained,
        IReadOnlyList<ComponentGroupParticipant> members)
    {
        var held = retained.Admissions.Select(item => item.Occurrence).ToHashSet();
        var intended = members.Select(item => item.Member.Selection.Occurrence).ToHashSet();
        return held.SetEquals(intended)
            ? null
            : (
                "membership-changed",
                "CBI19 replaces a generation resolving the same positions; adding or dropping one is a membership replacement.");
    }

    /// <summary>
    /// Checks that the request replaces the generation this activation made active, in the scope it
    /// occupies.
    /// </summary>
    internal static (string Code, string Reason)? Scope(
        ComponentGroupAuthorityResult retained,
        Cm.ActivationRuntimeRequest runtimeRequest)
    {
        // Read what the retained activation actually made active from CM4's own observation, not
        // from the caller's plan.
        if (retained.Lifecycle?.Runtime?.Observation is not { } current)
        {
            return ("retained-generation-unknown", "The retained activation records no CM4 observation to replace.");
        }

        if (runtimeRequest.RequestedRestartScope != current.RestartScope ||
            runtimeRequest.Plan.RestartScope != current.RestartScope)
        {
            return (
                "restart-scope-mismatch",
                $"CBI19 replaces the generation in scope '{current.RestartScope}'; widening or moving the scope is CM4's refusal, not a replacement.");
        }

        if (runtimeRequest.Plan.Generation == current.TargetGeneration)
        {
            return (
                "generation-not-successor",
                $"Generation '{current.TargetGeneration}' is the one already active in this scope, so it succeeds nothing.");
        }

        return runtimeRequest.RetainedGeneration == current.TargetGeneration
            ? null
            : (
                "retained-generation-mismatch",
                $"The scope holds generation '{current.TargetGeneration}', not '{runtimeRequest.RetainedGeneration}'.");
    }

    /// <summary>Names a pre-cutover Release failure as CM4 classified it.</summary>
    private static string SuccessorFailureCode(ComponentGroupAuthorityResult activation) =>
        activation.Lifecycle?.Runtime?.Kind == Cm.ActivationRuntimeOutcomeKind.ReleaseFailedBeforeCutover
            ? "release-failed-before-cutover"
            : "successor-establishment-refused";

    private static ComponentGroupReplacementResult Decline(
        ComponentGroupReplacementKind kind,
        string code,
        string reason) =>
        new(kind, null, false, Array.Empty<Cm.OccurrenceId>(), code, reason);
}

public enum ComponentGroupMembershipKind
{
    Replaced,
    CleanupFailed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentGroupMembershipResult(
    ComponentGroupMembershipKind Kind,
    ComponentGroupAuthorityResult? Successor,
    bool CutOver,
    IReadOnlyList<Cm.OccurrenceId> Added,
    IReadOnlyList<Cm.OccurrenceId> Dropped,
    IReadOnlyList<Cm.OccurrenceId> Surviving,
    IReadOnlyList<Cm.OccurrenceId> Retired,
    string Code,
    string Reason)
{
    public bool IsReplaced => Kind == ComponentGroupMembershipKind.Replaced;
}

/// <summary>
/// Replaces the generation occupying one restart scope with a successor generation that resolves a
/// different set of positions, adding and dropping members across the cutover.
/// </summary>
/// <remarks>
/// The lift needs no new authority rule, because CBI19 decided authority per occurrence: a surviving
/// occurrence is re-admitted with what admitted it, a new one is admitted afresh, and a dropped one
/// has nothing to follow it to. What it needs is the membership to be the successor generation's
/// statement rather than the caller's, which is where a silent drop would otherwise enter, and for
/// the change to be reported so a member retired because its position is gone is distinguishable
/// from one retired because its generation was replaced. An addition joins only across the cutover:
/// a CM2 generation is one immutable object and a CM4 attempt covers its whole plan, so neither can
/// represent a member arriving into a generation already serving.
/// </remarks>
public static class ComponentGroupMembership
{
    public static async ValueTask<ComponentGroupMembershipResult> ReplaceAsync(
        Cm.ResolutionOutcome successor,
        ComponentGroupAuthorityResult retained,
        IReadOnlyList<ComponentGroupParticipant> members,
        Cm.ActivationRuntimeRequest runtimeRequest,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(retained);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(runtimeRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!retained.IsActive || retained.Lifecycle is not { } lifecycle)
        {
            return Decline(
                ComponentGroupMembershipKind.ActivationUnavailable,
                "active-authority-unavailable",
                "CBI20 requires one released CBI13 activation to replace.");
        }

        if (ComponentGroupReplacement.Scope(retained, runtimeRequest) is { } invalid)
        {
            return Decline(ComponentGroupMembershipKind.Declined, invalid.Code, invalid.Reason);
        }

        if (ComponentGroupReplacement.Membership(successor, members) is { } disagreement)
        {
            return Decline(
                ComponentGroupMembershipKind.Declined,
                disagreement.Code,
                disagreement.Reason);
        }

        // Emptying an activation is CBI14's withdrawal: a scope cannot cut over to a generation with
        // no member to release, and the release barrier is a barrier over a membership.
        if (ComponentGroupReplacement.Positions(successor).Count == 0)
        {
            return Decline(
                ComponentGroupMembershipKind.Declined,
                "membership-empty",
                "The successor generation resolves no position; standing the activation down is a withdrawal, not a replacement.");
        }

        var held = retained.Admissions.Select(item => item.Occurrence).ToArray();
        var intended = members.Select(item => item.Member.Selection.Occurrence).ToArray();
        var replacement = await ComponentGroupReplacement.ReplaceCoreAsync(
            successor,
            retained,
            lifecycle,
            members,
            runtimeRequest,
            retirementReason,
            cancellationToken).ConfigureAwait(false);
        return new(
            Kind(replacement.Kind),
            replacement.Successor,
            replacement.CutOver,
            Ordered(intended.Where(item => !held.Contains(item))),
            Ordered(held.Where(item => !intended.Contains(item))),
            Ordered(held.Where(intended.Contains)),
            replacement.Retired,
            replacement.Code,
            replacement.Reason);
    }

    private static ComponentGroupMembershipKind Kind(ComponentGroupReplacementKind kind) => kind switch
    {
        ComponentGroupReplacementKind.Replaced => ComponentGroupMembershipKind.Replaced,
        ComponentGroupReplacementKind.CleanupFailed => ComponentGroupMembershipKind.CleanupFailed,
        ComponentGroupReplacementKind.ActivationUnavailable =>
            ComponentGroupMembershipKind.ActivationUnavailable,
        _ => ComponentGroupMembershipKind.Declined,
    };

    private static IReadOnlyList<Cm.OccurrenceId> Ordered(IEnumerable<Cm.OccurrenceId> occurrences) =>
        occurrences.OrderBy(item => item.Value, StringComparer.Ordinal).ToArray();

    private static ComponentGroupMembershipResult Decline(
        ComponentGroupMembershipKind kind,
        string code,
        string reason) =>
        new(
            kind,
            null,
            false,
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Cm.OccurrenceId>(),
            code,
            reason);
}

public enum ComponentGroupExtensionKind
{
    Extended,
    Declined,
    Withdrawn,
    RetirementFailed,
    ActivationUnavailable,
}

public sealed record ComponentGroupExtensionResult(
    ComponentGroupExtensionKind Kind,
    ComponentGroupAuthorityResult? InForce,
    IReadOnlyList<ComponentParticipantObservation> CurrentAuthority,
    IReadOnlyList<Cm.OccurrenceId> Grown,
    IReadOnlyList<Cm.OccurrenceId> Lapsed,
    string Code,
    string Reason)
{
    public bool IsExtended => Kind == ComponentGroupExtensionKind.Extended;
}

/// <summary>
/// Grows the participant sets of a multi-member activation while every member stays released.
/// </summary>
/// <remarks>
/// No resolution and no declaration are taken, and the absent parameters are the contract: growth
/// removes nobody, coverage is monotone in the grants held, so a member holding a declaration is
/// grown by the same rule as one holding none and the two may sit in one activation. What is checked
/// against the whole activation is CBI13's identity and Actor-mapping rules, which an addition is a
/// fresh opportunity to violate against members already live.
/// </remarks>
public static class ComponentGroupExtension
{
    public static async ValueTask<ComponentGroupExtensionResult> ExtendAsync(
        ComponentGroupAuthorityResult active,
        IReadOnlyList<ComponentGroupMemberRequests> members,
        string retirementReason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentException.ThrowIfNullOrWhiteSpace(retirementReason);

        if (!active.IsActive || active.Lifecycle is not { } lifecycle)
        {
            return new(
                ComponentGroupExtensionKind.ActivationUnavailable,
                null,
                Array.Empty<ComponentParticipantObservation>(),
                Array.Empty<Cm.OccurrenceId>(),
                Array.Empty<Cm.OccurrenceId>(),
                "active-authority-unavailable",
                "CBI18 requires one released CBI13 activation with every member admitted.");
        }

        var prior = active.Admissions;
        var ordered = members.OrderBy(item => item.Occurrence.Value, StringComparer.Ordinal).ToArray();
        if (!ordered.Select(item => item.Occurrence).SequenceEqual(prior.Select(item => item.Occurrence)))
        {
            return Decline(active, "member-set-changed", "The extension does not name the members this activation admitted.");
        }

        for (var index = 0; index < ordered.Length; index++)
        {
            if (Structure(prior[index], ordered[index]) is { } declined)
            {
                return Decline(active, declined.Code, declined.Reason);
            }
        }

        // A member that gains nobody restates its own set; an activation that gains nobody is a
        // revalidation and belongs to CBI14.
        if (!ordered.Where((item, index) => item.Requests.Count > prior[index].Participants.Count).Any())
        {
            return Decline(
                active,
                "activation-unchanged",
                "No member gains a participant; revalidating what is in force is CBI14.");
        }

        var intended = ordered.SelectMany(item => item.Requests).ToArray();
        if (ComponentParticipantAdmission.DistinctIdentities(intended) is { } collision)
        {
            return Decline(active, collision.Code, collision.Reason);
        }

        var priorByMember = prior.ToDictionary(item => item.Occurrence);
        foreach (var member in ordered)
        {
            var admitted = priorByMember[member.Occurrence].Participants.ToDictionary(item => item.Participant);
            if (member.Requests
                .Where(item => admitted.ContainsKey(item.Participant))
                .Any(item => !ComponentParticipantRevalidation.MatchesPrior(admitted[item.Participant], item)))
            {
                // Nothing was evaluated, so nothing was learned: a malformed request is not evidence
                // that the retained authority is gone.
                return Decline(
                    active,
                    "authority-revalidation-mismatch",
                    $"A retained request for member {member.Occurrence} does not identify the authority that admitted it.");
            }
        }

        var evaluator = new Cm.FakeAuthorityAdmissionEvaluator();
        var evaluated = ordered
            .Select(member => (
                Member: member,
                Observations: member.Requests
                    .OrderBy(item => item.Participant.Value, StringComparer.Ordinal)
                    .Select(item => new ComponentParticipantObservation(item.Participant, evaluator.Evaluate(item)))
                    .ToArray()))
            .ToArray();
        var current = evaluated.SelectMany(item => item.Observations).ToArray();

        var lapsed = evaluated
            .Where(item => item.Observations.Any(observation =>
                priorByMember[item.Member.Occurrence].Participants
                    .Any(admitted => admitted.Participant == observation.Participant &&
                        !ComponentParticipantRevalidation.IsSameAdmission(admitted.Authority, observation.Authority))))
            .Select(item => item.Member.Occurrence)
            .ToArray();
        if (lapsed.Length > 0)
        {
            // A lapse outranks any problem with an addition, and retires the whole activation:
            // the members share one restart scope, so they share a fate.
            return await RetireAsync(lifecycle, retirementReason, current, lapsed, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var (member, observations) in evaluated)
        {
            var admitted = priorByMember[member.Occurrence].Participants
                .Select(item => item.Participant)
                .ToHashSet();
            var refused = member.Requests
                .Where(item => !admitted.Contains(item.Participant))
                .Where(item => !ComponentParticipantAdmission.IsExactAdmission(
                    observations.Single(observation => observation.Participant == item.Participant).Authority,
                    item))
                .Select(item => item.Participant.Value)
                .ToArray();
            if (refused.Length > 0)
            {
                return Decline(
                    active,
                    "authority-not-admitted",
                    $"CM5 did not admit the exact submitted authority for {string.Join(", ", refused)}.",
                    current);
            }
        }

        var extended = evaluated
            .Select(item => new ComponentGroupMemberAdmission(
                item.Member.Occurrence,
                item.Observations,
                item.Observations
                    .SelectMany(observation => observation.Authority.Observation.Grants)
                    .OrderBy(grant => grant.Grant.Value, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        // The permitting direction matters here: a party already participating in another member may
        // be added to a second, and must arrive at the local Actor it already holds.
        if (ComponentGroupAuthority.ActorMapping(extended) is { } inconsistent)
        {
            return Decline(active, inconsistent.Code, inconsistent.Reason, current);
        }

        var grown = evaluated
            .Where(item => item.Observations.Length > priorByMember[item.Member.Occurrence].Participants.Count)
            .Select(item => item.Member.Occurrence)
            .ToArray();
        return new(
            ComponentGroupExtensionKind.Extended,
            active with
            {
                Admissions = extended,
                Grants = extended
                    .SelectMany(item => item.Grants)
                    .OrderBy(item => item.Grant.Value, StringComparer.Ordinal)
                    .ToArray(),
            },
            current,
            grown,
            Array.Empty<Cm.OccurrenceId>(),
            "participant-set-extended",
            $"{grown.Length} of {extended.Length} members grew; the activation now holds {extended.Sum(item => item.Participants.Count)} participants.");
    }

    /// <summary>Checks one member's intended set: retains everyone, repeats nobody, adds soundly.</summary>
    private static (string Code, string Reason)? Structure(
        ComponentGroupMemberAdmission prior,
        ComponentGroupMemberRequests intended)
    {
        if (ComponentParticipantAdmission.FirstDuplicate(
                intended.Requests.Select(item => item.Participant.Value)) is { } repeated)
        {
            return (
                "participant-not-distinct",
                $"Participant '{repeated}' appears in more than one request for member {intended.Occurrence}.");
        }

        var missing = prior.Participants
            .Where(existing => !intended.Requests.Any(item => item.Participant == existing.Participant))
            .Select(existing => existing.Participant.Value)
            .ToArray();
        if (missing.Length > 0)
        {
            return (
                "participant-not-retained",
                $"CBI18 only grows a set. Removing or substituting {string.Join(", ", missing)} in member {intended.Occurrence} requires CBI15 under a declaration, or CBI14 retirement and a fresh admission.");
        }

        var added = intended.Requests
            .Where(item => !prior.Participants.Any(existing => existing.Participant == item.Participant))
            .ToArray();
        return added.All(ComponentParticipantAdmission.SupportedShape)
            ? null
            : (
                "authority-shape-unsupported",
                "CBI18 supports one ComponentParticipant relationship per added participant and distinct narrow authority tuples dependent on it.");
    }

    private static async ValueTask<ComponentGroupExtensionResult> RetireAsync(
        ComponentGroupActivationResult lifecycle,
        string retirementReason,
        IReadOnlyList<ComponentParticipantObservation> current,
        IReadOnlyList<Cm.OccurrenceId> lapsed,
        CancellationToken cancellationToken)
    {
        var cleanup = new List<string>();
        foreach (var outcome in lifecycle.Members)
        {
            var (_, failure) = await ComponentParticipantRevalidation
                .TryRetireAsync(outcome.Member, retirementReason, cancellationToken)
                .ConfigureAwait(false);
            if (failure is not null)
            {
                cleanup.Add($"{outcome.Occurrence}: {failure}");
            }
        }

        return cleanup.Count == 0
            ? new(
                ComponentGroupExtensionKind.Withdrawn,
                null,
                current,
                Array.Empty<Cm.OccurrenceId>(),
                lapsed,
                "authority-not-renewed",
                $"The receiving domain no longer admits the identical authority for {string.Join(", ", lapsed.Select(item => item.Value))}.")
            : new(
                ComponentGroupExtensionKind.RetirementFailed,
                null,
                current,
                Array.Empty<Cm.OccurrenceId>(),
                lapsed,
                "authority-retirement-failed",
                string.Join("; ", cleanup));
    }

    private static ComponentGroupExtensionResult Decline(
        ComponentGroupAuthorityResult active,
        string code,
        string reason,
        IReadOnlyList<ComponentParticipantObservation>? current = null) =>
        new(
            ComponentGroupExtensionKind.Declined,
            active,
            current ?? Array.Empty<ComponentParticipantObservation>(),
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Cm.OccurrenceId>(),
            code,
            reason);
}

public sealed record ComponentGroupMemberSuccession(
    ComponentBindingSelection Selection,
    ComponentGrantDependency Declaration,
    ComponentGrantDependency SuccessorDeclaration,
    IReadOnlyList<ComponentOperationAuthorityMapping> Attribution,
    IReadOnlyList<ComponentObservedInteraction> Observations);

public enum ComponentGroupSuccessionKind
{
    Narrowed,
    Declined,
    ActivationUnavailable,
}

public sealed record ComponentGroupMemberDeclaration(
    Cm.OccurrenceId Occurrence,
    ComponentGrantDependency Declaration,
    IReadOnlyList<string> Dropped,
    IReadOnlyList<string> Vetoed);

public sealed record ComponentGroupSuccessionResult(
    ComponentGroupSuccessionKind Kind,
    IReadOnlyList<ComponentGroupMemberDeclaration> Members,
    IReadOnlyList<Cm.OccurrenceId> Narrowed,
    IReadOnlyList<Cm.OccurrenceId> Vetoing,
    string Code,
    string Reason)
{
    public bool IsNarrowed => Kind == ComponentGroupSuccessionKind.Narrowed;
}

/// <summary>
/// Narrows every member's declaration to one successor generation, unless any member's observed use
/// vetoes it.
/// </summary>
/// <remarks>
/// The permission is a generation, and a CM2 generation is one immutable object resolving every
/// position at once, so a succession is one transaction: applying the members it narrows while
/// refusing the rest would leave the activation holding declarations from two generations. A member
/// the successor does not narrow is untouched rather than refused, which is the case CBI11's single
/// rule could not distinguish. Nothing here retires a member or touches a participant set, which is
/// why it needs no cancellation token.
/// </remarks>
public static class ComponentGroupSuccession
{
    public static ComponentGroupSuccessionResult Succeed(
        Cm.ResolutionOutcome resolution,
        Cm.ResolutionOutcome successor,
        ComponentGroupAuthorityResult active,
        IReadOnlyList<ComponentGroupMemberSuccession> members)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(successor);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(members);

        if (!active.IsActive || active.Lifecycle is not { } lifecycle)
        {
            return new(
                ComponentGroupSuccessionKind.ActivationUnavailable,
                Array.Empty<ComponentGroupMemberDeclaration>(),
                Array.Empty<Cm.OccurrenceId>(),
                Array.Empty<Cm.OccurrenceId>(),
                "active-authority-unavailable",
                "CBI17 requires one released CBI13 activation with every member admitted.");
        }

        var ordered = members
            .OrderBy(item => item.Selection.Occurrence.Value, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select(item => item.Selection.Occurrence)
            .SequenceEqual(active.Admissions.Select(item => item.Occurrence)))
        {
            return new(
                ComponentGroupSuccessionKind.Declined,
                Array.Empty<ComponentGroupMemberDeclaration>(),
                Array.Empty<Cm.OccurrenceId>(),
                Array.Empty<Cm.OccurrenceId>(),
                "member-set-changed",
                "The succession does not name the members this activation admitted.");
        }

        var portable = lifecycle.Members.ToDictionary(item => item.Occurrence, item => item.Member);
        foreach (var member in ordered)
        {
            if (Structure(resolution, successor, member, portable[member.Selection.Occurrence]) is { } invalid)
            {
                return Decline(ordered, invalid.Code, invalid.Reason);
            }
        }

        // Restating what is in force succeeds nothing; a member that restates its own is untouched.
        // The subset check has already run, so a member whose names differ at all is one that narrows.
        if (!ordered.Any(member => !Names(member.SuccessorDeclaration).SetEquals(Names(member.Declaration))))
        {
            return Decline(
                ordered,
                "activation-unchanged",
                "No member's successor declares fewer authorities, so there is nothing to succeed.");
        }

        var evaluated = ordered.Select(Evaluate).ToArray();
        var vetoing = evaluated
            .Where(item => item.Vetoed.Count > 0)
            .Select(item => item.Occurrence)
            .ToArray();
        if (vetoing.Length > 0)
        {
            // One transaction, so a veto anywhere refuses every member's narrowing.
            return new(
                ComponentGroupSuccessionKind.Declined,
                ordered
                    .Select((member, index) => new ComponentGroupMemberDeclaration(
                        member.Selection.Occurrence,
                        member.Declaration,
                        Array.Empty<string>(),
                        evaluated[index].Vetoed))
                    .ToArray(),
                Array.Empty<Cm.OccurrenceId>(),
                vetoing,
                "declaration-use-vetoed",
                $"{string.Join(", ", vetoing.Select(item => item.Value))} has already exercised authority the successor would narrow away.");
        }

        var narrowed = evaluated
            .Where(item => item.Dropped.Count > 0)
            .Select(item => item.Occurrence)
            .ToArray();
        return new(
            ComponentGroupSuccessionKind.Narrowed,
            ordered
                .Select((member, index) => new ComponentGroupMemberDeclaration(
                    member.Selection.Occurrence,
                    member.SuccessorDeclaration,
                    evaluated[index].Dropped,
                    Array.Empty<string>()))
                .ToArray(),
            narrowed,
            Array.Empty<Cm.OccurrenceId>(),
            "declaration-narrowed",
            $"{narrowed.Length} of {ordered.Length} members narrowed, dropping {evaluated.Sum(item => item.Dropped.Count)} declared authorities.");
    }

    /// <summary>
    /// Checks one member's pair of declarations and its successor position, without asking what the
    /// member has exercised.
    /// </summary>
    private static (string Code, string Reason)? Structure(
        Cm.ResolutionOutcome resolution,
        Cm.ResolutionOutcome successor,
        ComponentGroupMemberSuccession member,
        Portable.PortableCompositionMember portable)
    {
        if (ComponentParticipantRevision.DeclarationShape(
                resolution,
                member.Selection,
                member.Declaration) is { } stale)
        {
            return stale;
        }

        if (ComponentParticipantRevision.DeclarationShape(
                successor,
                member.Selection,
                member.SuccessorDeclaration) is { } invalid)
        {
            return invalid;
        }

        // A generation that fails this for any member is not a successor of this activation.
        if (ComponentDeclarationSuccession.SamePosition(successor, member.Selection, portable) is { } mismatch)
        {
            return ("successor-position-mismatch", $"{member.Selection.Occurrence}: {mismatch}");
        }

        var names = Names(member.Declaration);
        if (!Names(member.SuccessorDeclaration).IsSubsetOf(names))
        {
            return (
                "declaration-not-narrower",
                $"Member {member.Selection.Occurrence} would gain declared authority; succession only removes it.");
        }

        var repointed = member.SuccessorDeclaration.Entries
            .Where(entry => member.Declaration.Entries.Any(current =>
                current.DeclaredAuthority == entry.DeclaredAuthority &&
                ComponentParticipantRevision.Tuple(current) != ComponentParticipantRevision.Tuple(entry)))
            .Select(entry => entry.DeclaredAuthority)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        if (repointed.Length > 0)
        {
            return (
                "declaration-tuple-changed",
                $"Succession removes dependencies; it does not re-point them. {member.Selection.Occurrence}: {string.Join(", ", repointed)} would change tuple.");
        }

        return ComponentParticipantAdmission.FirstDuplicate(
                member.Attribution.Select(item => item.Operation.ToString())) is { } repeated
            ? (
                "operation-mapping-not-distinct",
                $"Member {member.Selection.Occurrence} attributes Operation '{repeated}' to more than one declared authority.")
            : null;
    }

    /// <summary>
    /// Computes what one member would drop, and what its own observed use vetoes.
    /// </summary>
    /// <remarks>
    /// Exercised authority is per member, as CBI16 attributes it: one member's interaction cannot
    /// veto another member's narrowing.
    /// </remarks>
    private static (Cm.OccurrenceId Occurrence, IReadOnlyList<string> Dropped, IReadOnlyList<string> Vetoed)
        Evaluate(ComponentGroupMemberSuccession member)
    {
        var dropped = Names(member.Declaration)
            .Except(Names(member.SuccessorDeclaration), StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
        var exercised = ComponentInteractionVerification
            .Attribute(member.Attribution, member.Observations)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
        return (member.Selection.Occurrence, dropped, dropped.Where(exercised.Contains).ToArray());
    }

    private static HashSet<string> Names(ComponentGrantDependency declaration) =>
        declaration.Entries.Select(item => item.DeclaredAuthority).ToHashSet(StringComparer.Ordinal);

    private static ComponentGroupSuccessionResult Decline(
        IReadOnlyList<ComponentGroupMemberSuccession> members,
        string code,
        string reason) =>
        new(
            ComponentGroupSuccessionKind.Declined,
            members
                .Select(member => new ComponentGroupMemberDeclaration(
                    member.Selection.Occurrence,
                    member.Declaration,
                    Array.Empty<string>(),
                    Array.Empty<string>()))
                .ToArray(),
            Array.Empty<Cm.OccurrenceId>(),
            Array.Empty<Cm.OccurrenceId>(),
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
