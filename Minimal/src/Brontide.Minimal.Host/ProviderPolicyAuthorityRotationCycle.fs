namespace Brontide.Minimal.Host

open System
open System.Threading
open System.Threading.Tasks

type ProviderPolicyAuthorityCycleSchedule =
    private
    | ProviderPolicyAuthorityCycleSchedule of int * TimeSpan * int * TimeSpan * TimeSpan

    member this.MaximumAttempts =
        let (ProviderPolicyAuthorityCycleSchedule(value, _, _, _, _)) = this
        value

    member this.BaseDelay =
        let (ProviderPolicyAuthorityCycleSchedule(_, value, _, _, _)) = this
        value

    member this.BackoffMultiplier =
        let (ProviderPolicyAuthorityCycleSchedule(_, _, value, _, _)) = this
        value

    member this.MaximumDelay =
        let (ProviderPolicyAuthorityCycleSchedule(_, _, _, value, _)) = this
        value

    member this.AttemptTimeout =
        let (ProviderPolicyAuthorityCycleSchedule(_, _, _, _, value)) = this
        value

    /// The gap before the retry that follows this many consecutive failures. An applied rotation is
    /// progress and resets the count, so the gap is not a function of the attempt index, and it
    /// carries no jitter, which is what lets a shared vector pin an exact gap sequence.
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
module ProviderPolicyAuthorityCycleSchedule =
    let create
        maximumAttempts
        (baseDelay: TimeSpan)
        backoffMultiplier
        (maximumDelay: TimeSpan)
        (attemptTimeout: TimeSpan) =
        if maximumAttempts < 1 || maximumAttempts > 64 then
            invalidArg (nameof maximumAttempts) "A cycle budget must be between one and sixty-four attempts."
        if baseDelay <= TimeSpan.Zero then
            invalidArg (nameof baseDelay) "A base delay must be positive."
        if backoffMultiplier < 1 || backoffMultiplier > 16 then
            invalidArg (nameof backoffMultiplier) "A backoff multiplier must be between one and sixteen."
        if maximumDelay < baseDelay || maximumDelay > TimeSpan.FromHours 1.0 then
            invalidArg (nameof maximumDelay) "A maximum delay must be at least the base delay and at most one hour."
        // CBI58 refuses a longer attempt timeout, so a schedule carrying one could never be run.
        if attemptTimeout <= TimeSpan.Zero || attemptTimeout > TimeSpan.FromMinutes 1.0 then
            invalidArg (nameof attemptTimeout) "An attempt timeout must be positive and no greater than one minute."
        ProviderPolicyAuthorityCycleSchedule(
            maximumAttempts, baseDelay, backoffMultiplier, maximumDelay, attemptTimeout)

/// Retains the authority floor outside the checkpoint it describes. CBI38 detects an authority
/// rollback only against state held independently of the file it guards, so custody is the host's.
type ProviderPolicyAuthorityFloorSink = ProviderPolicyAuthorityFloor -> CancellationToken -> Task

/// The cycle's only source of elapsed time: it answers with the instant the gap ended, so a cycle is
/// a function of what the host injects rather than of an ambient clock.
type ProviderPolicyAuthorityCycleDelay =
    DateTimeOffset -> TimeSpan -> CancellationToken -> Task<DateTimeOffset>

type ProviderPolicyAuthorityCycleResult =
    { Code: string
      LastAttemptCode: string option
      Attempts: int
      Delays: TimeSpan list
      AppliedGenerations: int64 list
      RetainedGenerations: int64 list
      Generation: int64
      ActiveAuthority: ProviderPublisherTrustPolicyAuthorityId
      Floor: ProviderPolicyAuthorityFloor }
    member this.IsCurrent = this.Code = "policy-authority-cycle-current"

/// One bounded, host-driven cycle of CBI58 rotation attempts. It is a call the host makes, not a
/// daemon: nothing here decides when the host makes it or keeps a schedule across the process.
type ProviderPolicyAuthorityRotationCycle(
    registry: DurableProviderPublisherTrustPolicyRegistry,
    endpointIdentity: ProviderPublisherTrustPolicyDistributionEndpointId,
    schedule: ProviderPolicyAuthorityCycleSchedule) =

    do
        if isNull (box registry) then nullArg (nameof registry)
        if isNull (box schedule) then nullArg (nameof schedule)

    // The client is built here rather than accepted, so a cycle cannot report on one registry while
    // advancing another.
    let client = ProviderPolicyAuthorityRotationDistributionClient(registry, endpointIdentity)

    /// A retry changes the challenge, the cursor read from the registry, and the network. Every
    /// endpoint-authentication outcome is decided by a key the retry does not change, and every
    /// native CBI57 refusal by a statement the endpoint would send again.
    let retryable code =
        [ "policy-authority-distribution-transport-failed"
          "policy-authority-distribution-timeout"
          "policy-authority-distribution-stale"
          "policy-authority-distribution-superseded" ]
        |> List.contains code

    member _.RunAsync(
        source: IProviderPolicyAuthorityRotationDistributionSource,
        floorSink: ProviderPolicyAuthorityFloorSink,
        delay: ProviderPolicyAuthorityCycleDelay,
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
        let pending: ProviderPolicyAuthorityCycleResult option ref = ref None

        let observe code generation activeAuthority floor =
            { Code = code
              LastAttemptCode = lastAttemptCode.Value
              Attempts = attempts.Value
              Delays = List.ofSeq delays
              AppliedGenerations = List.ofSeq applied
              RetainedGenerations = List.ofSeq retained
              Generation = generation
              ActiveAuthority = activeAuthority
              Floor = floor }
        let observeRegistry code =
            observe code registry.AuthorityGeneration registry.ActiveAuthorityIdentity registry.AuthorityFloor

        while pending.Value.IsNone do
            if cancellationToken.IsCancellationRequested then
                pending.Value <- Some(observeRegistry "policy-authority-cycle-canceled")
            elif attempts.Value >= schedule.MaximumAttempts then
                pending.Value <- Some(observeRegistry "policy-authority-cycle-exhausted")
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
                    pending.Value <- Some(observeRegistry "policy-authority-cycle-canceled")
                else
                    attempts.Value <- attempts.Value + 1
                    let! attempt =
                        client.SynchronizeAsync(source, instant.Value, schedule.AttemptTimeout, cancellationToken)
                    lastAttemptCode.Value <- Some attempt.Code
                    let observeAttempt code =
                        observe code attempt.Generation attempt.ActiveAuthority attempt.Floor
                    if attempt.Code = "policy-authority-distribution-current" then
                        pending.Value <- Some(observeAttempt "policy-authority-cycle-current")
                    elif attempt.IsApplied then
                        applied.Add attempt.Floor.Generation
                        consecutiveFailures.Value <- 0
                        // CBI57 publishes the rotation into the retained chain before advancing the
                        // live authority, so the floor describing it cannot be offered any earlier.
                        let! handed = task {
                            try
                                do! floorSink attempt.Floor cancellationToken
                                return true
                            with error when not (error :? OutOfMemoryException) -> return false
                        }
                        if handed then retained.Add attempt.Floor.Generation
                        else pending.Value <- Some(observeAttempt "policy-authority-cycle-floor-unretained")
                    elif attempt.Code = "policy-authority-distribution-canceled" then
                        pending.Value <- Some(observeAttempt "policy-authority-cycle-canceled")
                    elif not (retryable attempt.Code) then
                        pending.Value <- Some(observeAttempt "policy-authority-cycle-refused")
                    else consecutiveFailures.Value <- consecutiveFailures.Value + 1

        return pending.Value |> Option.get
    }
