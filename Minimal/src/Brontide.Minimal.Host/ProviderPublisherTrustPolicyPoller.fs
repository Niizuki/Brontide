namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

type ProviderPublisherTrustPolicyPollSchedule =
    private
    | ProviderPublisherTrustPolicyPollSchedule of int * TimeSpan * int * TimeSpan * TimeSpan

    member this.MaximumAttempts =
        let (ProviderPublisherTrustPolicyPollSchedule(value, _, _, _, _)) = this
        value

    member this.BaseDelay =
        let (ProviderPublisherTrustPolicyPollSchedule(_, value, _, _, _)) = this
        value

    member this.BackoffMultiplier =
        let (ProviderPublisherTrustPolicyPollSchedule(_, _, value, _, _)) = this
        value

    member this.MaximumDelay =
        let (ProviderPublisherTrustPolicyPollSchedule(_, _, _, value, _)) = this
        value

    member this.AttemptTimeout =
        let (ProviderPublisherTrustPolicyPollSchedule(_, _, _, _, value)) = this
        value

    /// The gap before the retry that follows this many consecutive failures. Progress resets the
    /// count, so the gap is not a function of the attempt index.
    member this.DelayForConsecutiveFailures(consecutiveFailures: int) =
        if consecutiveFailures <= 0 then TimeSpan.Zero
        else
            let cap = this.MaximumDelay.Ticks
            let multiplier = int64 this.BackoffMultiplier
            let mutable ticks = this.BaseDelay.Ticks
            let mutable index = 1
            // Clamping at every step keeps a long budget from overflowing before reaching the cap
            // that would have bounded the product anyway.
            while index < consecutiveFailures && ticks < cap do
                ticks <- if multiplier > cap / ticks then cap else ticks * multiplier
                index <- index + 1
            TimeSpan.FromTicks(min ticks cap)

[<RequireQualifiedAccess>]
module ProviderPublisherTrustPolicyPollSchedule =
    let create
        maximumAttempts
        (baseDelay: TimeSpan)
        backoffMultiplier
        (maximumDelay: TimeSpan)
        (attemptTimeout: TimeSpan) =
        if maximumAttempts < 1 || maximumAttempts > 64 then
            invalidArg (nameof maximumAttempts) "A poll budget must be between one and sixty-four attempts."
        if baseDelay <= TimeSpan.Zero then
            invalidArg (nameof baseDelay) "A base delay must be positive."
        if backoffMultiplier < 1 || backoffMultiplier > 16 then
            invalidArg (nameof backoffMultiplier) "A backoff multiplier must be between one and sixteen."
        if maximumDelay < baseDelay || maximumDelay > TimeSpan.FromHours 1.0 then
            invalidArg (nameof maximumDelay) "A maximum delay must be at least the base delay and at most one hour."
        // CBI39 refuses a longer attempt timeout, so a schedule carrying one could never be run.
        if attemptTimeout <= TimeSpan.Zero || attemptTimeout > TimeSpan.FromMinutes 1.0 then
            invalidArg (nameof attemptTimeout) "An attempt timeout must be positive and no greater than one minute."
        ProviderPublisherTrustPolicyPollSchedule(
            maximumAttempts, baseDelay, backoffMultiplier, maximumDelay, attemptTimeout)

/// Retains a recovery floor outside the checkpoint it describes. CBI38 detects rollback only against
/// state held independently of the file it guards, so custody of this value is the host's.
type ProviderPublisherTrustPolicyFloorSink =
    ProviderPublisherTrustPolicyRecoveryFloor -> CancellationToken -> Task

/// The cycle's only source of elapsed time: it answers with the instant the gap ended, so a cycle is
/// a function of what the host injects rather than of an ambient clock.
type ProviderPublisherTrustPolicyPollDelay =
    DateTimeOffset -> TimeSpan -> CancellationToken -> Task<DateTimeOffset>

type ProviderPublisherTrustPolicyPollResult =
    { Code: string
      LastAttemptCode: string option
      Attempts: int
      Delays: TimeSpan list
      AppliedSequences: int64 list
      RetainedSequences: int64 list
      Current: VerifiedProviderPublisherTrustPolicySnapshot option
      Floor: ProviderPublisherTrustPolicyRecoveryFloor }
    member this.IsCurrent = this.Code = "policy-poll-current"

type ProviderPublisherTrustPolicyPoller(
    registry: DurableProviderPublisherTrustPolicyRegistry,
    endpointIdentity: ProviderPublisherTrustPolicyDistributionEndpointId,
    schedule: ProviderPublisherTrustPolicyPollSchedule) =

    do
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box schedule) then nullArg (nameof schedule)

    // The client is built here rather than accepted, so a poller cannot report on one registry while
    // advancing another.
    let client = ProviderPublisherTrustPolicyDistributionClient(registry, endpointIdentity)

    /// A retry changes the challenge, the cursor read, and the network. Everything else is decided by
    /// a key the retry does not change, by the caller, or by the registry's own state.
    let retryable code =
        [ "policy-distribution-transport-failed"
          "policy-distribution-timeout"
          "policy-distribution-stale"
          "policy-distribution-superseded" ]
        |> List.contains code

    member _.PollAsync(
        source: IProviderPublisherTrustPolicyDistributionSource,
        floorSink: ProviderPublisherTrustPolicyFloorSink,
        delay: ProviderPublisherTrustPolicyPollDelay,
        now: DateTimeOffset,
        cancellationToken: CancellationToken) = task {
        if isNull (box source) then nullArg (nameof source)
        if isNull (box floorSink) then nullArg (nameof floorSink)
        if isNull (box delay) then nullArg (nameof delay)

        let delays = ResizeArray<TimeSpan>()
        let applied = ResizeArray<int64>()
        let retained = ResizeArray<int64>()
        let attempts = ref 0
        let consecutiveFailures = ref 0
        let lastAttemptCode: string option ref = ref None
        let instant = ref now
        let pending: ProviderPublisherTrustPolicyPollResult option ref = ref None

        let observe code current floor =
            { Code = code
              LastAttemptCode = lastAttemptCode.Value
              Attempts = attempts.Value
              Delays = List.ofSeq delays
              AppliedSequences = List.ofSeq applied
              RetainedSequences = List.ofSeq retained
              Current = current
              Floor = floor }
        let observeRegistry code = observe code registry.Current registry.Floor

        while pending.Value.IsNone do
            if cancellationToken.IsCancellationRequested then
                pending.Value <- Some(observeRegistry "policy-poll-canceled")
            elif attempts.Value >= schedule.MaximumAttempts then
                pending.Value <- Some(observeRegistry "policy-poll-exhausted")
            else
                let! gapped = task {
                    if attempts.Value = 0 then return true
                    else
                        let duration = schedule.DelayForConsecutiveFailures consecutiveFailures.Value
                        try
                            let! next = delay instant.Value duration cancellationToken
                            instant.Value <- next
                            // A gap is recorded only once it has been waited, so a cancelled gap is
                            // not one.
                            delays.Add duration
                            return true
                        with :? OperationCanceledException -> return false
                }
                if not gapped || cancellationToken.IsCancellationRequested then
                    pending.Value <- Some(observeRegistry "policy-poll-canceled")
                else
                    attempts.Value <- attempts.Value + 1
                    let! attempt =
                        client.SynchronizeAsync(source, instant.Value, schedule.AttemptTimeout, cancellationToken)
                    lastAttemptCode.Value <- Some attempt.Code
                    if attempt.Code = "policy-distribution-current" then
                        pending.Value <- Some(observe "policy-poll-current" attempt.Current attempt.Floor)
                    elif attempt.IsApplied then
                        applied.Add attempt.Floor.Sequence
                        consecutiveFailures.Value <- 0
                        // The checkpoint is already published and live here; the floor describes it
                        // and so cannot be offered any earlier.
                        let! handed = task {
                            try
                                do! floorSink attempt.Floor cancellationToken
                                return true
                            with error when not (error :? OutOfMemoryException) -> return false
                        }
                        if handed then retained.Add attempt.Floor.Sequence
                        else
                            pending.Value <-
                                Some(observe "policy-poll-floor-unretained" attempt.Current attempt.Floor)
                    elif attempt.Code = "policy-distribution-canceled" then
                        pending.Value <- Some(observe "policy-poll-canceled" attempt.Current attempt.Floor)
                    elif not (retryable attempt.Code) then
                        pending.Value <- Some(observe "policy-poll-refused" attempt.Current attempt.Floor)
                    else consecutiveFailures.Value <- consecutiveFailures.Value + 1

        return pending.Value |> Option.get
    }
