# CBI67 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI67 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C8.

## Findings closed in the contract

- **The named boundary was "record the stop"; what it was worth only appeared on reading CBI51.**
  Both CBI50 and CBI64 name durable recording of a stop as later work, and neither says why it
  matters. `ProviderRestartPolicy.Evaluate` took a `ProviderRestartCause` the caller passed in, and
  two of its four values are refusals — so the caller chose which refusal applied to it. Recording the
  stop is the means; making the cause issuer-controlled is the capability, and the contract leads with
  that rather than with the store.
- **Only one of the three wrong claims was unguarded, and checking is what showed it.** A withdrawn
  publisher fails CBI51's own authorization check whatever cause is claimed, and an unexpected exit is
  the restartable case anyway. Operator retirement is neither: the publisher is still trusted, so
  every other condition passes. Saying the record buys exactly that is more useful than implying it
  guards all four, and it is the same test CBI43 and CBI44 applied to their trust steps — remove each
  and look.
- **The ordering is CBI41's rule in its third instance.** A record is a statement about something that
  happened, so it cannot precede the thing it describes. Written first and interrupted, it claims a
  stop that did not occur and CBI52 launches a second provider for an occurrence still serving.
  Written after, an interruption leaves a stop with no record — restartable, which is what an
  availability stop wanted, and refused anyway for a withdrawn publisher by a check that does not
  depend on this record. The failure modes are asymmetric and the contract says which way.
- **Absence had to mean exactly one thing.** It could have been "the host did not stop it" or "a write
  was lost", and no fact in the store distinguishes them. It is read as an unexpected exit, which is
  the only reading every writer's behaviour supports, and C4 pins that it yields a cause rather than a
  refusal of its own — a refusal there would make a first-ever restart impossible.
- **A record under a different staged identity is refused rather than resolved.** Treating it as
  absent would silently restart something an operator retired under the deployment before; treating it
  as current would attribute this deployment's stop to a previous one's cause. A host holding a record
  it cannot match does not guess, and the vector that catches this is the only one where the two
  readings differ.
- **An unexpected exit cannot be written down.** The store refuses to record it, because absence is
  what it is; a record naming it would be a record of the host not having stopped anything. That also
  keeps the operator path as the only way the one cause this slice exists to attribute comes into
  existence.
- **The record is consumed, not left behind.** A successful reconstruction clears it, so a stale
  attribution cannot authorize a second restart of a provider that is already running again; a refused
  or failed reconstruction leaves it, because nothing was restarted.

## What the phase deliberately does not decide

Whether a stopped provider should come back remains CBI51's and CBI52's, under every condition they
already impose. This slice changes where one of their inputs comes from and nothing else about them —
their refusals, budgets, delays, and authorization checks are untouched.

## Residual limits

**A stop the host did not perform cannot be attributed.** An operator who kills a provider from
outside the host leaves no record and an exited process, which is indistinguishable from an unexpected
exit. The capability is bounded to retirements issued through the host, and C5 names the operator path
as the only origin of that attribution rather than implying the record covers every retirement.

The store is host-local and single-writer, as CBI48's journal and CBI42's floor store are, and its
integrity tag detects corruption rather than an adversary who can write the file and recompute it.
Cross-process ownership of it is the same separate boundary those slices name.

The shared vectors exercise the store rather than a provider, because what the store answers is
decided by the record it holds. That CBI51 acts on the issued cause is pinned by the existing restart
scenarios, which run real providers and which fail when the cause is ignored — checked by removing the
refusal and watching four of them go red.
