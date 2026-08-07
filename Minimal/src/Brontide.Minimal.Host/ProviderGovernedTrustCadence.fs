namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

type ProviderPolicyAuthorityRotationCycleBinding =
    DateTimeOffset -> CancellationToken -> Task<ProviderPolicyAuthorityCycleResult>

[<RequireQualifiedAccess>]
module ProviderGovernedTrustCycle =
    /// Before CBI57 an update the registry could not verify could only come from a stranger, which is
    /// what CBI41's own vector calls it. Afterwards the same observable also describes a legitimate
    /// publisher this host has not rotated to yet, and only a cycle that ran both loops knows which
    /// it is. The test is a conjunction of two recorded facts rather than a judgement about the
    /// update: a host that is up to date and still cannot verify one is being sent an update it
    /// should refuse.
    let private authorityBehind
        (rotation: ProviderPolicyAuthorityCycleResult)
        (result: ProviderServingTrustCycleResult) =
        result.Code = ProviderServingTrustCycleCodes.Stopped
        && (result.Poll |> Option.bind _.LastAttemptCode) = Some "policy-update-authority-mismatch"
        && not rotation.IsCurrent

    /// Binds one CBI60 cycle to the source, floor sink, and delay owned by its host.
    let rotationBinding
        (cycle: ProviderPolicyAuthorityRotationCycle)
        source
        floorSink
        delay
        : ProviderPolicyAuthorityRotationCycleBinding =
        fun now cancellationToken -> cycle.RunAsync(source, floorSink, delay, now, cancellationToken)

    /// Runs one CBI60 rotation cycle before CBI47's poll and sweep, inside CBI47's unchanged cadence.
    /// The order is the registry's rather than a preference: a policy update is verified against the
    /// authority in force, so an update signed by the authority a rotation installs is refused until
    /// that rotation is retained.
    let create
        (rotation: ProviderPolicyAuthorityRotationCycleBinding)
        (inner: ProviderServingTrustCycle)
        : ProviderServingTrustCycle =
        fun now cancellationToken -> task {
            let! rotated = rotation now cancellationToken
            if rotated.Code = "policy-authority-cycle-canceled" then
                return
                    { Code = ProviderServingTrustCycleCodes.Canceled; Poll = None
                      Sweep = None; ServingCount = 0; Rotation = Some rotated; Availability = None }
            // The one rotation outcome that changed something the host cannot account for: the chain
            // advanced past a floor it does not hold, and every later advance moves further past it.
            elif rotated.Code = "policy-authority-cycle-floor-unretained" then
                return
                    { Code = ProviderServingTrustCycleCodes.AuthorityUnretained; Poll = None
                      Sweep = None; ServingCount = 0; Rotation = Some rotated; Availability = None }
            else
                let! result = inner now cancellationToken
                let code =
                    if authorityBehind rotated result then ProviderServingTrustCycleCodes.AuthorityBehind
                    else result.Code
                return { result with Code = code; Rotation = Some rotated }
        }
