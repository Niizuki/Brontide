using System.Collections.Immutable;
using System.Globalization;

namespace Brontide.Reference.Experimental.Binding.Portable;

/// <summary>Identifies one binding scope in a composition.</summary>
/// <remarks>
/// The scope is the composition's identity for one requirement position. It survives withdrawal,
/// termination, and replacement; the plan identifier does not, because a replacement negotiates a
/// new plan rather than resuming the retired one.
/// </remarks>
public readonly record struct PortableBindingScopeId
{
    public const int MaxLength = 256;

    private PortableBindingScopeId(string value) => Value = value;

    public string Value { get; }

    public static PortableBindingScopeId Parse(string? value) =>
        TryParse(value, out var scope)
            ? scope
            : throw PortableFaultException.Malformed(
                "binding-scope",
                $"'{value}' is not a binding-scope identifier: use 1..{MaxLength} characters from letters, digits, '.', '_', and '-'.");

    public static bool TryParse(string? value, out PortableBindingScopeId scope)
    {
        scope = default;
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            var permitted = character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or
                (>= '0' and <= '9') or '.' or '_' or '-';
            if (!permitted)
            {
                return false;
            }
        }

        scope = new PortableBindingScopeId(value);
        return true;
    }

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>How a requirement's Provider Set is presented to its consumer.</summary>
public enum PortableExposure
{
    Distinct,
    Mediated
}

/// <summary>The named stages one composition member passes through.</summary>
public enum PortableCompositionStage
{
    LocalInitialisation,
    Interconnected,
    Released,
    Retired
}

public static class PortableCompositionVocabulary
{
    public static string Token(this PortableExposure exposure) =>
        exposure == PortableExposure.Distinct ? "distinct" : "mediated";

    public static string Token(this PortableCompositionStage stage) => stage switch
    {
        PortableCompositionStage.LocalInitialisation => "local-initialisation",
        PortableCompositionStage.Interconnected => "interconnected",
        PortableCompositionStage.Released => "released",
        PortableCompositionStage.Retired => "retired",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}

/// <summary>The declared membership bounds of one Provider Set.</summary>
/// <remarks>
/// Version 0.1 supports exactly one member. A wider bound is refused rather than narrowed, because
/// a Provider Set also needs membership, exposure, and failure semantics this seam does not own.
/// </remarks>
public readonly record struct PortableProviderCardinality(int Minimum, int Maximum)
{
    public static PortableProviderCardinality OneToOne { get; } = new(1, 1);

    public bool IsOneToOne => Minimum == 1 && Maximum == 1;

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Minimum}..{Maximum}");
}

/// <summary>
/// What a resolution already decided about one requirement position.
/// </summary>
/// <remarks>
/// The handoff consumes this; it never produces one. How the Component was discovered, acquired,
/// ranked, or selected happened before this record existed.
/// </remarks>
public sealed record PortableResolvedRequirement(
    PortableBindingScopeId Scope,
    PortableComponentReference Component,
    PortableProviderReference? RequiredProvider,
    PortableProviderCardinality Cardinality,
    PortableExposure Exposure,
    string HostEndpoint)
{
    /// <summary>The ordinary case: one contract, one provider, presented directly.</summary>
    public static PortableResolvedRequirement OneToOneContract(
        PortableBindingScopeId scope,
        PortableComponentReference component,
        string hostEndpoint) =>
        new(scope, component, null, PortableProviderCardinality.OneToOne, PortableExposure.Distinct, hostEndpoint);

    /// <summary>The provider-specific case: the resolution names which provider must answer.</summary>
    public static PortableResolvedRequirement OneToOneProvider(
        PortableBindingScopeId scope,
        PortableComponentReference component,
        PortableProviderReference provider,
        string hostEndpoint) =>
        new(scope, component, provider, PortableProviderCardinality.OneToOne, PortableExposure.Distinct, hostEndpoint);
}

/// <summary>The provision the resolution selected, as claimed before any frame is exchanged.</summary>
public sealed record PortableOfferedProvision(
    PortableComponentReference Component,
    PortableProviderReference Provider,
    string ProviderEndpoint);

/// <summary>What a retired binding tells a future replacement generation.</summary>
/// <remarks>
/// It is data about a binding that has ended and grants nothing: a replacement generation still
/// resolves, preflights, negotiates, and releases from the beginning.
/// </remarks>
public sealed record PortableReplacementRecord(
    PortableBindingScopeId Scope,
    PortablePlanId RetiredPlanId,
    PortableComponentReference Component,
    PortableProviderReference Provider,
    string TerminalState,
    string Reason,
    bool ReplacementPermitted);

/// <summary>
/// The narrow seam by which a resolved Component requirement and an offered provision produce a
/// Binding Plan during activation preflight.
/// </summary>
/// <remarks>
/// Everything the seam refuses is refused on purpose. Discovery, acquisition, provider selection
/// policy, resolved generations, mediation, and hot swap belong to the Component Management
/// programme; a seam that approximated any of them would make that programme's decisions here,
/// invisibly.
/// </remarks>
public static class PortableCompositionHandoff
{
    /// <summary>
    /// Local Initialisation and the frameless part of preflight: the member exists, holds its
    /// resolution, and has no peer relationship, no conversation, and no plan.
    /// </summary>
    /// <remarks>
    /// Every check here runs before a conversation exists, so a refusal emits no frame and starts no
    /// provider. That is structural rather than asserted: there is nothing here to emit through.
    /// </remarks>
    public static PortableCompositionMember Prepare(
        PortableResolvedRequirement requirement,
        PortableOfferedProvision provision,
        PortableContractDocument requiredContract)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(provision);
        ArgumentNullException.ThrowIfNull(requiredContract);

        if (provision.Component != requirement.Component)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "component-mismatch",
                $"The resolution requires {requirement.Component} but the offered provision provides {provision.Component}.");
        }

        // A host whose own declaration disagrees with its resolution is refused here rather than at
        // negotiation, where the diagnostic would name the peer for a local inconsistency.
        if (requiredContract.Component != requirement.Component)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "declaration-mismatch",
                $"The required contract document declares {requiredContract.Component}, which is not the resolved {requirement.Component}.");
        }

        if (requirement.RequiredProvider is { } required && provision.Provider != required)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "provider-not-selected",
                $"The resolution requires provider {required} but the offered provision is {provision.Provider}; selecting a compatible substitute is resolution work.");
        }

        if (!requirement.Cardinality.IsOneToOne)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "cardinality-unsupported",
                $"Cardinality {requirement.Cardinality} is outside version 0.1, which binds exactly one provider; it is refused rather than narrowed to a first member.");
        }

        if (requirement.Exposure != PortableExposure.Distinct)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "exposure-unsupported",
                $"Exposure '{requirement.Exposure.Token()}' is outside version 0.1; an erased Mediation would still carry provenance, deputy, and authority obligations.");
        }

        return new PortableCompositionMember(requirement, provision, requiredContract);
    }
}

/// <summary>
/// One Component occurrence's participation in a composition, from Local Initialisation to
/// retirement.
/// </summary>
/// <remarks>
/// The member owns the ordinary-interaction gate. Trusted host or binding machinery has to enforce
/// it: a member that answered an ordinary interaction before Release would be Active while its
/// group was not, which is exactly what the release barrier exists to prevent.
/// </remarks>
public sealed class PortableCompositionMember : IAsyncDisposable
{
    private readonly PortableContractDocument _requiredContract;
    private PortableBindingHost? _binding;
    private PortableProviderReference? _answeringProvider;

    internal PortableCompositionMember(
        PortableResolvedRequirement requirement,
        PortableOfferedProvision provision,
        PortableContractDocument requiredContract)
    {
        Requirement = requirement;
        Provision = provision;
        _requiredContract = requiredContract;
        ResolutionFacts = BuildResolutionFacts(requirement, provision);
    }

    public PortableResolvedRequirement Requirement { get; }

    public PortableOfferedProvision Provision { get; }

    public PortableBindingScopeId Scope => Requirement.Scope;

    public PortableCompositionStage Stage { get; private set; } = PortableCompositionStage.LocalInitialisation;

    /// <summary>What the resolution fixed, answerable from Local Initialisation onward.</summary>
    public ImmutableSortedDictionary<string, string> ResolutionFacts { get; }

    /// <summary>The frozen plan, or null while no contract has been negotiated.</summary>
    public PortableBindingPlan? Plan => _binding?.Plan;

    /// <summary>The provider that actually answered, or null before Interconnection.</summary>
    public PortableProviderReference? AnsweringProvider => _answeringProvider;

    public bool IsReady => _binding?.State == PortableLifecycleState.Ready;

    public bool IsReleased => Stage == PortableCompositionStage.Released;

    /// <summary>
    /// One fact by name: a resolution fact at any stage, a plan fact once Interconnection has fixed
    /// one.
    /// </summary>
    /// <remarks>
    /// A plan fact is unanswerable before Interconnection rather than answered with a placeholder.
    /// Answering one would claim an agreement that does not exist yet.
    /// </remarks>
    public string Fact(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (ResolutionFacts.TryGetValue(name, out var resolved))
        {
            return resolved;
        }

        return _binding is { } binding
            ? binding.Plan.Fact(name)
            : throw new InvalidOperationException(
                $"'{name}' is not a resolution fact, and no Binding Plan exists before Interconnection.");
    }

    /// <summary>
    /// Interconnection: negotiate the contract, freeze the plan, and wait for the readiness signal.
    /// The ordinary-interaction gate stays closed throughout.
    /// </summary>
    public async ValueTask InterconnectAsync(
        IPortableProviderConversation conversation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        RequireStage(PortableCompositionStage.LocalInitialisation, "interconnect");

        var witness = new AnsweringProviderWitness(conversation);
        var host = await PortableBindingHost
            .EstablishAsync(_requiredContract, witness, Requirement.HostEndpoint, cancellationToken)
            .ConfigureAwait(false);

        // Establishment cannot succeed without the offered contract passing through the witness, so
        // this states an invariant of this endpoint rather than anything a peer can cause: it is an
        // internal protocol failure rather than a contract refusal.
        if (witness.Offered is not { } offered)
        {
            await AbandonAsync(host, cancellationToken).ConfigureAwait(false);
            throw new PortableFaultException(
                PortableProtocolCategory.InternalProtocolFailure,
                "witness-missing",
                "Establishment reported success without an offered contract, so no provider identity could be checked.");
        }

        // Negotiation refuses an endpoint answering as a provider the host did not require, so what
        // is left here is the case it cannot see: a required contract naming a provider this
        // resolution did not select. Reachable only when the requirement named no provider, since
        // preflight otherwise settles it before contact.
        var answered = offered.Provider;
        if (answered != Provision.Provider)
        {
            await AbandonAsync(host, cancellationToken).ConfigureAwait(false);
            throw new PortableFaultException(
                PortableProtocolCategory.UnsupportedContract,
                "provider-substituted",
                $"The resolution selected provider {Provision.Provider} but the endpoint answered as {answered}; the binding is abandoned rather than rebound.");
        }

        _binding = host;
        _answeringProvider = answered;
        Stage = PortableCompositionStage.Interconnected;
    }

    /// <summary>Release: the composition opens the ordinary-interaction gate.</summary>
    /// <remarks>
    /// Release requires the readiness signal, stated once rather than as a separate stage rule and a
    /// separate readiness rule that could drift apart. A member still in Local Initialisation has no
    /// binding and therefore no signal, which is how a release that skipped Interconnection is
    /// caught: releasing one would admit ordinary interaction against a provider that never reported
    /// its establishment Outcome.
    /// </remarks>
    public void Release()
    {
        if (Stage is PortableCompositionStage.Released or PortableCompositionStage.Retired)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "illegal-stage",
                $"'release' is illegal for binding scope '{Scope}' in stage '{Stage.Token()}'.");
        }

        if (!IsReady)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "release-before-ready",
                $"Binding scope '{Scope}' has no readiness signal in stage '{Stage.Token()}', so its ordinary-interaction gate stays closed.");
        }

        Stage = PortableCompositionStage.Released;
    }

    /// <summary>One ordinary interaction, admitted only after Release.</summary>
    public ValueTask<PortableInteractionResult> InvokeAsync(
        PortableOperationReference operation,
        PortableShapeReference inputShape,
        PortableValue input,
        PortableConstraint authority,
        IReadOnlyList<PortableResource>? resources = null,
        CancellationToken cancellationToken = default)
    {
        if (_binding is not { } binding)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "gate-closed",
                $"Binding scope '{Scope}' has no established binding, so no ordinary interaction can start.");
        }

        return IsReleased
            ? binding.InvokeAsync(
                operation,
                inputShape,
                input,
                authority,
                resources,
                cancellationToken: cancellationToken)
            : ValueTask.FromResult(RefusedByGate(binding));
    }

    /// <summary>
    /// Withdrawal and termination, producing the record a replacement generation is told.
    /// </summary>
    public async ValueTask<PortableReplacementRecord> RetireAsync(
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        if (_binding is not { } binding)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "retire-before-interconnection",
                $"Binding scope '{Scope}' has no established binding to retire.");
        }

        if (Stage == PortableCompositionStage.Retired)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "already-retired",
                $"Binding scope '{Scope}' was already retired.");
        }

        // The gate closes first: withdrawal is lifecycle traffic, and no ordinary interaction may
        // overtake it.
        Stage = PortableCompositionStage.Retired;
        await binding.WithdrawAsync(cancellationToken).ConfigureAwait(false);
        await binding.TerminateAsync(cancellationToken).ConfigureAwait(false);

        var terminal = binding.State;
        return new PortableReplacementRecord(
            Scope,
            binding.Plan.PlanId,
            binding.Plan.Component,
            _answeringProvider!.Value,
            terminal.Token(),
            reason,
            // A failed binding leaves this seam no account of the provider's state, so replacement
            // is permitted only after a clean end.
            terminal == PortableLifecycleState.Terminated);
    }

    public async ValueTask DisposeAsync()
    {
        if (_binding is { } binding)
        {
            await binding.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The gate refusal, reported as a complete observation the way every other refusal is.
    /// </summary>
    /// <remarks>
    /// The authority decision is 'unknown' because the gate refuses before the authority boundary is
    /// reached: no evaluation ran, and reporting 'permitted' would claim one that did not.
    /// </remarks>
    private PortableInteractionResult RefusedByGate(PortableBindingHost binding)
    {
        var correlation = new PortableCorrelationMapping(
            binding.ChannelId,
            PortableChannelRequestId.New(),
            PortableHostExecutionId.New());
        var observation = new PortableObservationBuilder(binding.Plan).Build(
            PortableTerminalStatus.ProtocolError,
            PortableAuthorityDecision.Unknown,
            PortableAuthorityDecisionPoint.HostLocal,
            correlation,
            PortableFailureDomain.LocalEndpoint,
            0,
            0,
            [],
            [],
            false,
            new PortableTiming(0, 0),
            "gate-closed",
            $"Binding scope '{Scope}' is not released, so no ordinary interaction is admitted.");
        return new PortableInteractionResult(
            PortableFrameDecision.None,
            PortableResultClass.ProtocolError,
            PortableProtocolCategory.StateViolation,
            null,
            null,
            observation);
    }

    private void RequireStage(PortableCompositionStage expected, string action)
    {
        if (Stage != expected)
        {
            throw new PortableFaultException(
                PortableProtocolCategory.StateViolation,
                "illegal-stage",
                $"'{action}' is illegal for binding scope '{Scope}' in stage '{Stage.Token()}'.");
        }
    }

    /// <summary>Ends a binding that was established but must not be released.</summary>
    private static async ValueTask AbandonAsync(PortableBindingHost host, CancellationToken cancellationToken)
    {
        try
        {
            await host.TerminateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PortableFaultException)
        {
            // The refusal being reported is the interesting one; a peer that cannot be told the
            // binding ended does not change it.
        }
        catch (PortableProcessFailureException)
        {
        }

        await host.DisposeAsync().ConfigureAwait(false);
    }

    private static ImmutableSortedDictionary<string, string> BuildResolutionFacts(
        PortableResolvedRequirement requirement,
        PortableOfferedProvision provision)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        builder["bindingScope"] = requirement.Scope.Value;
        builder["resolvedComponent"] = requirement.Component.ToString();
        builder["requiredProvider"] = requirement.RequiredProvider?.ToString() ?? "absent";
        builder["cardinality"] = requirement.Cardinality.ToString();
        builder["exposure"] = requirement.Exposure.Token();
        builder["resolvedHostEndpoint"] = requirement.HostEndpoint;
        builder["selectedProvision"] = provision.Provider.ToString();
        builder["selectedProviderEndpoint"] = provision.ProviderEndpoint;
        return builder.ToImmutable();
    }

    /// <summary>
    /// Records which provider actually answered the establish frame.
    /// </summary>
    /// <remarks>
    /// It exists because the offered contract is otherwise consumed inside establishment and never
    /// surfaces: the Binding Plan's provider fact is read from the required document, so it reports
    /// who the host asked for rather than who answered.
    /// </remarks>
    private sealed class AnsweringProviderWitness(IPortableProviderConversation inner) : IPortableProviderConversation
    {
        public PortableContractDocument? Offered { get; private set; }

        public PortableRealization Realization => inner.Realization;

        public async ValueTask<PortableContractDocument> EstablishAsync(
            PortableContractDocument required,
            string hostEndpoint,
            PortableChannelId channel,
            CancellationToken cancellationToken)
        {
            Offered = await inner.EstablishAsync(required, hostEndpoint, channel, cancellationToken).ConfigureAwait(false);
            return Offered;
        }

        public ValueTask AwaitReadyAsync(PortableChannelId channel, CancellationToken cancellationToken) =>
            inner.AwaitReadyAsync(channel, cancellationToken);

        public ValueTask<PortableOutcomeReceipt> RequestAsync(
            PortableBindingPlan plan,
            PortableChannelId channel,
            PortableChannelRequestId request,
            PortableChannelExecutionId? execution,
            PortableOperationReference? operation,
            int? compactOperation,
            PortableShapeReference inputShape,
            PortableValue input,
            IReadOnlyList<PortableResource> resources,
            CancellationToken cancellationToken) =>
            inner.RequestAsync(
                plan,
                channel,
                request,
                execution,
                operation,
                compactOperation,
                inputShape,
                input,
                resources,
                cancellationToken);

        public ValueTask WithdrawAsync(PortableChannelId channel, CancellationToken cancellationToken) =>
            inner.WithdrawAsync(channel, cancellationToken);

        public ValueTask TerminateAsync(PortableChannelId channel, CancellationToken cancellationToken) =>
            inner.TerminateAsync(channel, cancellationToken);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
