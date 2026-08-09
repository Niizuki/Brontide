# CBI67 capability contract — durable stop attribution

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI50 and CBI64 both name durable recording of a stop as a later boundary. Taking it found what the
absence costs: **CBI51 decides restart eligibility from a `ProviderRestartCause` the caller passes
in**, and two of its four values are refusals. A caller that says `OfflineAvailability` about a
provider an operator deliberately retired gets a restart the policy would have denied, and nothing
anywhere holds a fact that contradicts it.

Two of the three wrong claims are caught by something else, and only checking showed which. A
withdrawn publisher fails CBI51's own authorization check whatever cause is claimed, and an
unexpected exit is the restartable case anyway. **Operator retirement is the one that is neither**, so
it is the whole of what an attributable record buys — and saying that is more useful than implying
the record guards all four.

CBI67 records why the host stopped a provider and makes the cause CBI51 reads issuer-controlled: the
`ProviderRestartCause` parameter is replaced by an opaque attribution that only the store issues, so
there is no longer a public path by which a caller can state one.

This is not a clock, a supervisor, a cross-process owner, or a claim about a stop the host did not
perform.

## Capabilities

### C1 — a stop is recorded after it happens, never before

Each path in the host that stops a provider records the stop once the effect is complete: CBI50's
availability enforcement, CBI46's trust sweep, and an explicit operator retirement.

The ordering is CBI41's rule in its third instance. **A record is a statement about something that
happened, so it cannot precede the thing it describes.** A record written first and interrupted claims
a stop that did not occur, and CBI52 would then launch a second provider for an occurrence that is
still serving. Written after, an interruption leaves a stop with no record, which reads as an
unexpected exit — restartable, which is what an availability stop wanted, and refused anyway for a
withdrawn publisher by a check that does not depend on this record.

Property: no attribution names an occurrence whose provider is still running.

### C2 — the cause is issued, not asserted

`ProviderRestartPolicy.Evaluate` takes an opaque `ProviderStopAttribution` in place of a
`ProviderRestartCause`. The attribution has no public construction path, so the only way to obtain one
is to ask the store about an activation. CBI51's refusals are unchanged; what changed is that the
caller can no longer choose which one applies.

Property: no test and no host path constructs an attribution without the store.

### C3 — an attribution must match the activation it is about

A record is bound to the occurrence and the staged identity it was serving. A record the store holds
for that occurrence under a different staged identity describes a different deployment, and is refused
as `provider-restart-attribution-stale` rather than resolved either way. A host holding a record it
cannot match does not guess.

Property: every issued attribution names the activation's own occurrence and staged identity.

### C4 — no record means the host did not stop it

An occurrence with no record is attributed `UnexpectedExit`, because every stop the host performs
writes one. That is the honest reading of absence and it is also the safe one: it is the restartable
case, and the two causes that must not restart are either double-guarded (trust withdrawal) or, in the
case this slice exists for, recorded.

Property: absence yields exactly one cause and never a refusal of its own.

### C5 — a stop the host did not perform cannot be attributed

An operator who kills a provider from outside the host leaves no record and an exited process, which
is indistinguishable from an unexpected exit. The capability is bounded to retirements issued through
the host, and the contract says so rather than implying the record covers every retirement.

Property: the operator path is the only way an `OperatorRetirement` attribution comes into existence.

### C6 — the record is consumed by the restart it authorizes

A successful CBI52 reconstruction clears the record for that occurrence, so a stale attribution cannot
authorize a second restart of a provider that is already running again. A refused or failed
reconstruction leaves it, because nothing was restarted.

Property: after a successful reconstruction, the store holds no record for that occurrence.

### C7 — the store detects damage and does not claim more

The record is integrity-tagged exactly as CBI42's floor store is, with the same limit: it detects
corruption, not a writer who can rewrite the file and recompute the tag. A record that fails its tag
is refused rather than read.

Property: a corrupted record is never issued as an attribution.

### C8 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the attributed cause,
the refusal code where one applies, and CBI51's resulting decision.

Property: every shared vector yields an identical typed observation in both roots.

## Deliberate limits

The store is host-local and single-writer, as CBI48's journal and CBI42's floor store are. Cross-process
ownership of it is the same separate boundary those slices name.

Nothing here supervises: a stop is recorded by whatever performed it, and a host that stops a provider
by some path this slice does not know about is recorded by absence. The record says why the host
stopped a provider and never that a provider was healthy, ready, or should be restarted — CBI51 and
CBI52 keep every other condition they already impose.
