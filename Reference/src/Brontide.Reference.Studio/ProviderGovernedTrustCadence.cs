namespace Brontide.Reference.Studio;

public interface IProviderPolicyAuthorityRotationCycle
{
    Task<ProviderPolicyAuthorityCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Binds one CBI60 cycle to the source, floor sink, and delay owned by its host.</summary>
public sealed class ProviderPolicyAuthorityRotationCycleBinding(
    ProviderPolicyAuthorityRotationCycle cycle,
    IProviderPolicyAuthorityRotationDistributionSource source,
    IProviderPolicyAuthorityFloorSink floorSink,
    IProviderPolicyAuthorityCycleDelay delay) : IProviderPolicyAuthorityRotationCycle
{
    public Task<ProviderPolicyAuthorityCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        cycle.RunAsync(source, floorSink, delay, now, cancellationToken);
}

/// <summary>
/// Runs one CBI60 rotation cycle before CBI47's poll and sweep, inside CBI47's unchanged cadence.
/// The order is the registry's rather than a preference: a policy update is verified against the
/// authority in force, so an update signed by the authority a rotation installs is refused until that
/// rotation is retained.
/// </summary>
public sealed class ProviderGovernedTrustCycle(
    IProviderPolicyAuthorityRotationCycle rotation,
    IProviderServingTrustCycle inner) : IProviderServingTrustCycle
{
    public async Task<ProviderServingTrustCycleResult> RunAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rotated = await rotation.RunAsync(now, cancellationToken).ConfigureAwait(false);

        if (rotated.Code == "policy-authority-cycle-canceled")
            return new("provider-trust-cycle-canceled", null, null, 0, rotated);

        // The one rotation outcome that changed something the host cannot account for: the chain
        // advanced past a floor it does not hold, and every later advance moves further past it.
        if (rotated.Code == "policy-authority-cycle-floor-unretained")
            return new("provider-trust-cycle-authority-unretained", null, null, 0, rotated);

        var result = await inner.RunAsync(now, cancellationToken).ConfigureAwait(false);
        var code = IsAuthorityBehind(rotated, result)
            ? "provider-trust-cycle-authority-behind"
            : result.Code;
        return result with { Code = code, Rotation = rotated };
    }

    /// <summary>
    /// Before CBI57 an update the registry could not verify could only come from a stranger, which is
    /// what CBI41's own vector calls it. Afterwards the same observable also describes a legitimate
    /// publisher this host has not rotated to yet, and only a cycle that ran both loops knows which
    /// it is. The test is a conjunction of two recorded facts rather than a judgement about the
    /// update: a host that is up to date and still cannot verify one is being sent an update it
    /// should refuse.
    /// </summary>
    private static bool IsAuthorityBehind(
        ProviderPolicyAuthorityCycleResult rotation,
        ProviderServingTrustCycleResult result) =>
        result.Code == "provider-trust-cycle-stopped"
        && result.Poll?.LastAttemptCode == "policy-update-authority-mismatch"
        && !rotation.IsCurrent;
}
