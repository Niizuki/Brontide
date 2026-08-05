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

type ProviderServingTrustCycleResult =
    { Code: string
      Poll: ProviderPublisherTrustPolicyPollResult
      Sweep: ProviderServingTrustSweepResult option
      ServingCount: int }
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
                    { Code = "provider-trust-cycle-canceled"; Poll = poll
                      Sweep = None; ServingCount = 0 }
            elif not poll.IsCurrent then
                return
                    { Code = "provider-trust-cycle-stopped"; Poll = poll
                      Sweep = None; ServingCount = 0 }
            else
                let! sweep = serving cancellationToken
                match sweep with
                | None ->
                    return
                        { Code = "provider-trust-cycle-current"; Poll = poll
                          Sweep = None; ServingCount = 0 }
                | Some result ->
                    let code =
                        match result.Code with
                        | "serving-trust-sweep-current" -> "provider-trust-cycle-current"
                        | "serving-trust-sweep-withdrawn" -> "provider-trust-cycle-withdrawn"
                        | _ -> "provider-trust-cycle-stopped"
                    return
                        { Code = code; Poll = poll; Sweep = Some result
                          ServingCount = result.Members.Length }
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
