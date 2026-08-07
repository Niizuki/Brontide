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

/// Every code a trust cycle can return, with whether the cadence may continue after it. Producers use
/// these values rather than literals and CBI48's journal validates against this set, so a new code
/// cannot be produced without the journal knowing it — which is what CBI61's two additions did before
/// this was one vocabulary.
///
/// `establishes` carries the second question a consumer asks of a committed observation: did the cycle
/// reporting this code establish current policy? It is stated here rather than in the consumer for the
/// same reason `continues` is, one consumer over — a code a cycle can produce and a consumer cannot
/// classify is exactly the seam defect CBI62 found. It is `None` for a non-continuing code, because
/// such a code makes the run terminal in the write that commits it and so never precedes another
/// observation; `Stopped` is the one that could not be answered anyway, since a cycle produces it both
/// for a poll that was not current and for a current poll whose sweep failed.
[<RequireQualifiedAccess>]
module ProviderServingTrustCycleCodes =
    [<Literal>]
    let Current = "provider-trust-cycle-current"
    [<Literal>]
    let Withdrawn = "provider-trust-cycle-withdrawn"
    [<Literal>]
    let Stopped = "provider-trust-cycle-stopped"
    [<Literal>]
    let Canceled = "provider-trust-cycle-canceled"
    [<Literal>]
    let AuthorityBehind = "provider-trust-cycle-authority-behind"
    [<Literal>]
    let AuthorityUnretained = "provider-trust-cycle-authority-unretained"
    [<Literal>]
    let Offline = "provider-trust-cycle-offline"

    let private codes =
        Map.ofList
            [ Current, (true, Some true)
              Withdrawn, (true, Some true)
              Stopped, (false, None)
              Canceled, (false, None)
              AuthorityBehind, (false, None)
              AuthorityUnretained, (false, None)
              Offline, (true, Some false) ]

    let all = codes |> Map.toList |> List.map fst

    let isKnown code = codes |> Map.containsKey code

    let continues code = codes |> Map.tryFind code |> Option.map fst |> Option.defaultValue false

    /// Whether a cycle reporting this code established current policy, or `None` when the vocabulary
    /// does not answer it. A consumer that meets a `None` must refuse rather than choose a side.
    let establishes code = codes |> Map.tryFind code |> Option.bind snd

/// What one cycle's availability evaluation decided and what enforcing it did. The cycle reports the
/// CBI49 decision and CBI50's counts rather than CBI50's own result, which keeps this result free of
/// the enforcement component's types; per-member evidence stays where its ordering and
/// failure-isolation properties are pinned. `DecisionCode` is `None` when CBI50 refused the serving
/// snapshot before any policy was evaluated.
type ProviderTrustCycleAvailability =
    { EnforcementCode: string
      DecisionCode: string option
      Deadline: DateTimeOffset option
      RetryAt: DateTimeOffset option
      AdmittedCount: int
      StoppedCount: int }
    member this.PermitsContinuation =
        this.DecisionCode = Some "offline-existing-service"
        || this.DecisionCode = Some "offline-idle"

/// One cycle's observations. `Rotation` is `None` for a cadence that does not govern authority
/// rotation, which is every cadence composed before CBI61; a governed cycle carries the CBI60 result
/// it ran before the poll, and `Poll` is `None` when that rotation stopped the cycle before the
/// policy endpoint was contacted. `Availability` is `None` for a cadence that governs no availability
/// policy, which is every cadence composed before CBI64, and for a cycle that established current
/// policy or never reached the endpoint.
type ProviderServingTrustCycleResult =
    { Code: string
      Poll: ProviderPublisherTrustPolicyPollResult option
      Sweep: ProviderServingTrustSweepResult option
      ServingCount: int
      Rotation: ProviderPolicyAuthorityCycleResult option
      Availability: ProviderTrustCycleAvailability option }
    member this.CanContinue = ProviderServingTrustCycleCodes.continues this.Code
    member this.IsCanceled = this.Code = ProviderServingTrustCycleCodes.Canceled

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
                    { Code = ProviderServingTrustCycleCodes.Canceled; Poll = Some poll
                      Sweep = None; ServingCount = 0; Rotation = None; Availability = None }
            elif not poll.IsCurrent then
                return
                    { Code = ProviderServingTrustCycleCodes.Stopped; Poll = Some poll
                      Sweep = None; ServingCount = 0; Rotation = None; Availability = None }
            else
                let! sweep = serving cancellationToken
                match sweep with
                | None ->
                    return
                        { Code = ProviderServingTrustCycleCodes.Current; Poll = Some poll
                          Sweep = None; ServingCount = 0; Rotation = None; Availability = None }
                | Some result ->
                    let code =
                        match result.Code with
                        | "serving-trust-sweep-current" -> ProviderServingTrustCycleCodes.Current
                        | "serving-trust-sweep-withdrawn" -> ProviderServingTrustCycleCodes.Withdrawn
                        | _ -> ProviderServingTrustCycleCodes.Stopped
                    return
                        { Code = code; Poll = Some poll; Sweep = Some result
                          ServingCount = result.Members.Length; Rotation = None; Availability = None }
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
