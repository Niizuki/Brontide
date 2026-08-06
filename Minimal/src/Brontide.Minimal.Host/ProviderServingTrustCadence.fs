namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

type ProviderServingTrustCadenceSchedule =
    private
    | ProviderServingTrustCadenceSchedule of int * TimeSpan
    member this.MaximumCycles =
        let (ProviderServingTrustCadenceSchedule(value, _)) = this
        value
    member this.Interval =
        let (ProviderServingTrustCadenceSchedule(_, value)) = this
        value

[<RequireQualifiedAccess>]
module ProviderServingTrustCadenceSchedule =
    let create maximumCycles (interval: TimeSpan) =
        if maximumCycles < 1 || maximumCycles > 64 then
            invalidArg (nameof maximumCycles) "A cadence must contain between one and sixty-four cycles."
        if interval <= TimeSpan.Zero || interval > TimeSpan.FromHours 24.0 then
            invalidArg (nameof interval) "A cadence interval must be positive and no greater than twenty-four hours."
        ProviderServingTrustCadenceSchedule(maximumCycles, interval)

type ProviderPublisherTrustPolicyCycle =
    DateTimeOffset -> CancellationToken -> Task<ProviderPublisherTrustPolicyPollResult>

type ProviderServingTrustSweepCycle =
    CancellationToken -> Task<ProviderServingTrustSweepResult option>

[<RequireQualifiedAccess>]
module ProviderServingTrustCycleBinding =
    let policy
        (poller: ProviderPublisherTrustPolicyPoller)
        source
        floorSink
        delay
        : ProviderPublisherTrustPolicyCycle =
        fun now cancellationToken -> poller.PollAsync(source, floorSink, delay, now, cancellationToken)

    let sweep
        registry
        store
        (servingSet: CancellationToken -> Task<ProviderServingActivation list>)
        retirementReason
        : ProviderServingTrustSweepCycle =
        fun cancellationToken -> task {
            let! activations = servingSet cancellationToken
            if List.isEmpty activations then return None
            else
                let! result = ProviderServingTrustSweep.run registry store activations retirementReason
                return Some result
        }

/// One cycle's observations. `Rotation` is `None` for a cadence that does not govern authority
/// rotation, which is every cadence composed before CBI61; a governed cycle carries the CBI60 result
/// it ran before the poll, and `Poll` is `None` when that rotation stopped the cycle before the
/// policy endpoint was contacted.
type ProviderServingTrustCycleResult =
    { Code: string
      Poll: ProviderPublisherTrustPolicyPollResult option
      Sweep: ProviderServingTrustSweepResult option
      ServingCount: int
      Rotation: ProviderPolicyAuthorityCycleResult option }
    member this.CanContinue =
        this.Code = "provider-trust-cycle-current"
        || this.Code = "provider-trust-cycle-withdrawn"
    member this.IsCanceled = this.Code = "provider-trust-cycle-canceled"

type ProviderServingTrustCycle =
    DateTimeOffset -> CancellationToken -> Task<ProviderServingTrustCycleResult>

[<RequireQualifiedAccess>]
module ProviderServingTrustCycle =
    let create
        (policy: ProviderPublisherTrustPolicyCycle)
        (serving: ProviderServingTrustSweepCycle)
        : ProviderServingTrustCycle =
        fun now cancellationToken -> task {
            let! poll = policy now cancellationToken
            if poll.Code = "policy-poll-canceled" then
                return
                    { Code = "provider-trust-cycle-canceled"; Poll = Some poll
                      Sweep = None; ServingCount = 0; Rotation = None }
            elif not poll.IsCurrent then
                return
                    { Code = "provider-trust-cycle-stopped"; Poll = Some poll
                      Sweep = None; ServingCount = 0; Rotation = None }
            else
                let! sweep = serving cancellationToken
                match sweep with
                | None ->
                    return
                        { Code = "provider-trust-cycle-current"; Poll = Some poll
                          Sweep = None; ServingCount = 0; Rotation = None }
                | Some result ->
                    let code =
                        match result.Code with
                        | "serving-trust-sweep-current" -> "provider-trust-cycle-current"
                        | "serving-trust-sweep-withdrawn" -> "provider-trust-cycle-withdrawn"
                        | _ -> "provider-trust-cycle-stopped"
                    return
                        { Code = code; Poll = Some poll; Sweep = Some result
                          ServingCount = result.Members.Length; Rotation = None }
        }

type ProviderServingTrustCadenceDelay =
    DateTimeOffset -> TimeSpan -> CancellationToken -> Task<DateTimeOffset>

type ProviderServingTrustCadenceCycle =
    { Instant: DateTimeOffset
      Result: ProviderServingTrustCycleResult }

type ProviderServingTrustCadenceResult =
    { Code: string
      Cycles: ProviderServingTrustCadenceCycle list
      Gaps: TimeSpan list }

[<RequireQualifiedAccess>]
module ProviderServingTrustCadence =
    let run
        (schedule: ProviderServingTrustCadenceSchedule)
        (cycle: ProviderServingTrustCycle)
        (delay: ProviderServingTrustCadenceDelay)
        start
        (cancellationToken: CancellationToken)
        = task {
            if isNull (box cycle) then nullArg (nameof cycle)
            if isNull (box delay) then nullArg (nameof delay)

            let cycles = ResizeArray<ProviderServingTrustCadenceCycle>()
            let gaps = ResizeArray<TimeSpan>()
            let mutable instant = start
            let mutable code: string option = None

            while cycles.Count < schedule.MaximumCycles && Option.isNone code do
                if cancellationToken.IsCancellationRequested then
                    code <- Some "provider-trust-cadence-canceled"
                else
                    let! canRun = task {
                        if cycles.Count = 0 then return true
                        else
                            try
                                let! next = delay instant schedule.Interval cancellationToken
                                if cancellationToken.IsCancellationRequested then return false
                                else
                                    if next <= instant then
                                        invalidOp "A cadence delay must advance the cycle instant."
                                    instant <- next
                                    gaps.Add schedule.Interval
                                    return true
                            with :? OperationCanceledException -> return false
                    }
                    if not canRun then code <- Some "provider-trust-cadence-canceled"
                    else
                        let! result = cycle instant cancellationToken
                        cycles.Add { Instant = instant; Result = result }
                        if result.IsCanceled then code <- Some "provider-trust-cadence-canceled"
                        elif not result.CanContinue then code <- Some "provider-trust-cadence-stopped"

            return
                { Code = code |> Option.defaultValue "provider-trust-cadence-complete"
                  Cycles = List.ofSeq cycles; Gaps = List.ofSeq gaps }
        }
