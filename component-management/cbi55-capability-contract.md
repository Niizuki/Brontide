# CBI55 capability contract — external provider restart-effect reconciliation

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI53 deliberately leaves an interrupted restart attempt indeterminate, and CBI54 lets a successor
host process become its sole owner without claiming that the prior process produced no provider
effect. CBI55 gives a cooperating provider one external, host-local effect lease. Before CBI52 may
launch, a durable record binds that lease to the exact CBI53 attempt and CBI54 fencing epoch. The
provider holds an operating-system lock for its lifetime and publishes a bounded receipt naming its
process identity.

A successor owner may retry only after it proves that the lease is free, or terminates the exact
live process named by a valid receipt and then proves the lease is free. It cannot reconstruct or
adopt the lost in-memory portable lifecycle, so a found orphan is cleaned up rather than treated as
a completed restart.

This is cooperative one-host reconciliation. It is not distributed process discovery, hostile
process attestation, exactly-once execution, lifecycle adoption, or proof about effects outside the
provider process and its portable connection.

## Capabilities

### C1 — one durable effect record names one exact attempt

Preparation requires the CBI53 run, occurrence, staged-content identity, in-flight attempt index and
instant, prior CBI54 fencing epoch, and a distinct effect token. An existing record for another
lineage or for a non-successor attempt/fence is refused without replacement.

Property: every accepted effect record belongs to exactly one restart attempt and fencing epoch.

### C2 — the effect record precedes the provider effect

The CBI55 coordinator publishes the integrity-checked effect record before CBI53 marks the attempt
in-flight and before CBI52 launches. The provider receives only the record's lease path, receipt
path, token, and staged identity through its process environment; these facts do not weaken the
content-addressed argument policy.

Property: no CBI55 provider launch occurs without a complete durable record for that attempt.

### C3 — the provider exposes one externally observable lifetime

At startup the cooperating provider takes the effect lease, writes an atomic bounded receipt naming
the token, staged identity, process id, and process start instant, and holds the lease until exit.
Failure to take the lease or publish the receipt stops that provider before it serves requests.

Property: every provider that can serve under CBI55 has a matching live lease and receipt.

### C4 — absence of a live lease permits retry

For an exact in-flight journal and effect record, a successor owner probes the lease. If it can take
and release the lease, no CBI55 provider process remains; stale or absent receipt bytes do not turn
that absence into a success claim. Reconciliation selects CBI53 `Retry` and changes no external
effect.

Property: retry is selected only after the external provider lease is observed free.

### C5 — an exact orphan is terminated before retry

If the lease is busy, reconciliation requires an integrity-valid receipt with the exact token and
staged identity, a live process with the exact process id and start instant, and the expected
provider executable name. Only that process may be terminated. The lease must become free within a
bounded wait before CBI53 `Retry` is selected.

Property: every process termination is justified by one exact receipt and followed by a free lease.

### C6 — uncertainty remains indeterminate and effect-free

A missing or corrupt durable record, mismatched attempt or lineage, busy lease with a missing,
corrupt, or mismatched receipt, unavailable process observation, failed termination, or lease that
stays busy reports a stable deferred/refusal code. It preserves the exact CBI53 journal bytes and
does not terminate a process that cannot be matched.

Property: every uncertain path leaves the restart attempt in-flight and performs no journal transition.

### C7 — current ownership fences reconciliation

Reconciliation requires a current live CBI54 lease for the journal lineage. A successor fence may
reconcile a record from an earlier epoch; the same or a later record epoch is refused because it
cannot be residue from a prior owner. Released or stale ownership changes neither journal nor
external process state.

Property: every accepted external reconciliation is performed by a strictly later current fence.

### C8 — both roots execute one shared reconciliation model

Reference C# and Minimal F# independently execute shared vectors for a free lease, a missing record,
an attempt mismatch, and a record not fenced behind the current owner. Native named evidence in each
root additionally covers a busy lease with a missing receipt and uses a real child process for the
exact-orphan path. The observations include code, process termination, lease availability, journal
phase, and retry count.

Property: every shared vector produces the same portable observation in both roots.

## Deliberate limits

CBI55 turns one externally observable provider lifetime into evidence that a retry is safe after
cleanup. It never changes an interrupted attempt directly to completed, and it never interprets an
absent receipt alone as proof that no effect remains.
