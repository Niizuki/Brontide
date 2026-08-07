# CBI65 capability contract — durable availability baseline

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI64 put CBI49's availability policy inside the cadence and held its baseline — the instant of the
most recent cycle whose poll established current policy — in memory, for the width of one run. A
cadence resumed after a crash therefore begins with none, and CBI49's answer to a missing baseline is
that the first outage stops service. CBI65 derives the baseline from what CBI48 already committed.

Nothing new is recorded. The journal has held each cycle's instant and code since CBI48, and the
classification the derivation needs is a fact about the cycle vocabulary rather than about a run, so
this slice adds a second classification to that vocabulary rather than a second durable record.

This is not a clock, a secure custodian, a cross-process owner, or a claim that a durable record
proves what a provider did.

## Capabilities

### C1 — the baseline is derived, and deriving it writes nothing

Recovery reads the journal's committed observations and computes an instant. It performs no
transition, records no marker, and leaves the durable bytes exactly as it found them.

This is the shape CBI63 established and the reason is the same one: a record written *about* a
derivation is a less trustworthy copy of the record the derivation read.

Property: the journal file is byte-identical before and after any derivation, including a refused
one.

### C2 — which codes move the baseline is a property of the vocabulary

`ProviderServingTrustCycleCodes` gains a second classification beside `Continues`: whether a cycle
reporting that code established current policy. `provider-trust-cycle-current` and
`provider-trust-cycle-withdrawn` did — both required a current poll before the sweep ran.
`provider-trust-cycle-offline` did not, which is CBI64's rule reconstructed from the durable record
rather than restated beside it.

The derivation walks the vocabulary, so a later slice cannot add a cycle code without answering this
question. That is CBI62's repair applied to a second classification of the same vocabulary: a code
that a cycle can produce and a consumer cannot classify is the defect CBI62 found, one consumer over.

Property: replaying a run's committed observations yields exactly the baseline the live cadence held
at the end of that run.

### C3 — the baseline is a fact about the host, not about the run

A terminal journal is as good a source as an interrupted one. CBI49 anchors the deadline in absolute
time, so a baseline from an old run is already expired and needs no rejection; refusing it would make
a host that shut down cleanly and restarted stop service at its first outage, which is stricter than
CBI49 requires and buys nothing. The run identity is not compared, because the only journal a host
reads is the one it wrote.

A baseline later than the evaluating instant is refused by CBI49's own `offline-observation-invalid`,
so this slice adds no freshness guard of its own and states that rather than implying a check that
does not exist.

Property: no derivation outcome depends on the run identity or on the journal's terminal code.

### C4 — a record with no establishing cycle yields no baseline

A journal whose committed observations contain no establishing code — including one with no committed
observations at all — reports `cadence-baseline-absent` rather than an invented instant. CBI49's
answer is then unchanged: the first outage stops service.

Property: an absent baseline is reported as absent and never as an instant.

### C5 — an interrupted attempt contributes nothing

An in-flight attempt has no committed observation and therefore no code, so it cannot establish
anything. What that attempt did remains CBI63's question, answered from the cursor it recorded, and
the two derivations do not overlap.

Property: a journal's derived baseline is unchanged by whether an attempt is in flight over it.

### C6 — an unclassifiable observation is refused rather than guessed

`provider-trust-cycle-stopped` is genuinely ambiguous: the cycle produces it both for a poll that was
not current and for a current poll whose sweep failed, so nothing in the record says which. CBI48
never puts one in front of a later cycle — a non-continuing code makes the run terminal in the same
write — but the derivation takes a snapshot, and a snapshot carrying one is a reachable input rather
than a manufactured path. It is refused as `cadence-baseline-observation-invalid`.

That CBI48 cannot produce such a record is a claim about a dependency, so it is probed rather than
asserted: a test drives the journal through every continuing code and shows the run terminates on the
first non-continuing one.

Property: every committed observation the derivation accepts carries a code the vocabulary classifies.

### C7 — a resumed cadence continues the outage it was in

Composed, the effect is that a crash during an outage does not restart grace. A cadence seeded fresh
renews the deadline on every restart, so a host crash-looping inside an outage would serve
indefinitely past a deadline that never arrives — the failure CBI64's C2 prevents within one run,
arriving across runs instead.

Property: for one journal, the deadline a resumed cadence reports equals the deadline the cadence
reported before it was interrupted.

### C8 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the derivation code,
the derived instant, and the vocabulary's classification of every code it holds.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

The derivation trusts the journal exactly as far as CBI48 does: its tag detects accidental damage,
not a writer that can replace the record and recompute it, so a baseline is as trustworthy as the
host's own store. Nothing here proves a provider was serving at the instant the record names, and
nothing reads a second host's journal.

A cadence still evaluates at its own interval, so CBI64's limit is unchanged: expiry is observed at
the first cycle at or after the deadline. Cross-process ownership of the serving set and privileged
custody remain separate.
