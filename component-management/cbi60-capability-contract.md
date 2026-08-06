# CBI60 capability contract — durable policy-authority rotation cycle

Date: 2026-08-06

Status: implementation contract

## Boundary

CBI58 makes one authenticated single-attempt rotation synchronization and CBI59 gives it a wire.
Neither schedules. CBI60 supplies the host-owned cycle around them: a bounded sequence of CBI58
attempts with deterministic retry, and durable custody of the authority floor those attempts advance.

It is CBI41's shape applied to the other key, and it is deliberately not more than that. It is not a
daemon, a timer, a cross-process owner, an offline policy, or a privileged custody domain. It cannot
weaken CBI59's single-attempt adapter, CBI58's authentication, or CBI57's native decision: every
refusal keeps the code the slice that produced it emitted.

## Capabilities

### C1 — one bounded cycle of single attempts

A schedule fixes a maximum attempt count of 1 to 64, a positive base delay, a backoff multiplier of
1 to 16, a maximum delay no greater than one hour, and an attempt timeout no greater than one minute
because CBI58 refuses a longer one. The first attempt is immediate; every later attempt is preceded
by a gap the host waits through an injected delay, which is the cycle's only source of elapsed time.
A cycle that reaches its budget ends `policy-authority-cycle-exhausted`.

Property: every cycle makes at most `MaximumAttempts` CBI58 calls, and the gaps it records are
exactly one fewer than the attempts it made.

### C2 — retry only what a fresh attempt can change

A retry changes the challenge, the cursor read from the registry, and the network. Transport failure,
timeout, a stale validity window, and a superseded cursor are therefore retried. Every
endpoint-authentication outcome — endpoint mismatch, invalid signature, challenge mismatch, cursor
mismatch, an unbounded or malformed response — and every native CBI57 refusal ends the cycle at the
attempt that produced it, reported as `policy-authority-cycle-refused` with that attempt's own code.
Repeating a request the pinned endpoint key just failed to authenticate cannot change the answer.

Property: no cycle makes a second CBI58 call after a non-retryable attempt code.

### C3 — backoff is a function of consecutive failures and carries no jitter

The gap before a retry is the base delay multiplied by the backoff multiplier once per consecutive
failure beyond the first, clamped to the maximum delay. Progress resets the count, so an applied
rotation between two failures returns the next gap to the base delay. There is no jitter, because the
cycle asks for a duration and the host decides how to wait it.

Property: the same schedule and the same attempt-outcome sequence produce the same gap sequence in
both roots.

### C4 — the authority floor is handed off after publication and never before

CBI57 publishes a rotation into the retained chain before advancing the live authority, so the floor
describing it cannot precede it. Each applied rotation is recorded as an applied generation, then
offered to the floor sink, then recorded as a retained generation. A refused handoff stops the cycle
as `policy-authority-cycle-floor-unretained`, reporting an applied generation with no matching
retained one, because the rotation is durable and cannot be undone.

Property: in every cycle the retained generations are a prefix of the applied generations, and they
differ by at most one entry.

### C5 — the authority floor has durable custody bound to the pin

A host-local store retains the CBI38 authority floor outside the checkpoint it guards. It is
integrity-tagged, refuses a record naming a different authority pin, refuses a generation below the
one stored, refuses an equal generation naming a different active authority — a fork rather than an
advance — and reports an identical floor as unchanged. A floor at generation zero must name the pin
itself, because that is the only floor an unrotated checkpoint can satisfy. The floor advances only
by a handoff from a publication this host performed; the value CBI38's `Open` derives from a
recovered chain is never written back, for CBI42's reason.

Property: a stored generation never decreases across any sequence of retentions.

### C6 — absence of this guard is weaker than CBI42's, and the chain is why

CBI42 establishes the policy floor at zero *before* the checkpoint exists, which is what lets a later
absence mean the guard was removed. That ordering is not available to a guard introduced after the
checkpoints it must guard already exist, so an absent authority floor beneath an existing checkpoint
is adopted at zero rather than refused, and reported as `policy-authority-floor-adopted` so the host
sees that the guard did not exist rather than being told it was recovered. Adoption under-detects for
one rotation and self-heals at the next handoff, which is CBI41's lagging floor rather than its
leading one.

What the weaker guarantee costs is bounded by the chain rather than by this store. A truncation that
drops a rotation with later updates is already refused as an invalid chain, because those updates are
signed by the successor authority the truncation removed; a truncation that drops policy updates is
already refused by CBI42's floor. The case only this floor detects is a checkpoint truncated at a
trailing rotation, and the case neither detects is that truncation with this guard deleted.

Property: for every truncation of a retained chain, at least one of chain replay, the policy floor,
and the authority floor refuses the result whenever both guards are present.

### C7 — both roots execute the shared vectors

Reference C# and Minimal F# independently run the shared cycle vectors and observe the same cycle
code, last attempt code, attempt count, gap sequence, applied generations, retained generations, and
recovered durable generation.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

CBI60 owns one call the host makes. It does not decide when the host calls it, run in the background,
survive the process as a schedule, coordinate two processes, or hold custody in a privileged domain —
the integrity tag detects corruption, not an adversary who can write the file and recompute it, which
is CBI42's limit unchanged. It applies no offline grace and retires nothing: a rotation cycle that
cannot reach its endpoint leaves every serving member exactly as it was.
