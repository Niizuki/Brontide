# CM4 contract-completeness review

Date: 2026-07-30

Review type: phase-boundary absence audit, separate from conformance and independent attestation

Scope: the CM4 C1-C10 capability contract, neutral vector inventory, Reference and Minimal public
surfaces, and both native test suites

Result: complete; every finding below is corrected and no unresolved CM4 contract silence remains

This review asks what the contract did not say. It does not claim Architecture 0.8 conformance,
cross-stack interoperability, production activation safety, real process isolation, durable
rollback, or authority-policy correctness.

## C1 — preparation

Finding: a `Prepared` state could be mistaken for a partially established generation, and an
implementation could retain caller-owned preparation-step storage in its observation.

Disposition: preparation has a separate typed identity and finite step inventory, its failure stops
before establishment, and every runtime effect remains false. Reference snapshots the step list
into a read-only collection; Minimal retains it as a persistent list.

## C2 — scope and generation identity

Finding: exact restart-scope matching did not rule out "activating" the already retained generation
and falsely reporting a cutover. Duplicate scope identities could also make the retained state
ambiguous.

Disposition: the target generation must differ from the retained generation, the target scope
appears exactly once and active with that retained identity, and every unrelated scope is preserved
in every outcome.

## C3 — stage completeness

Finding: per-member success booleans alone did not say whether missing, duplicate, foreign, or
out-of-plan stage observations were permitted, nor whether successful members created an accidental
within-stage order.

Disposition: the complete member/group/stage product is required exactly once. The Host processes
groups and stages in CM3 order but emits one all-member completion event per stage. A failure names
the first member in deterministic identity order without claiming that successful peers started in
that order.

## C4 — gate and edge admission

Finding: saying ordinary traffic was post-Release did not require it to follow a dependency edge
retained by CM3. A caller could therefore invent an undeclared peer interaction after Release while
still passing the timing rule.

Disposition: ordinary attempts must match one internal or condensation edge and the named member
groups. Lifecycle attempts must match the exact group-internal protocol edge, peers, Operation,
Capability, and input Shape. All other attempts are refused without delivery.

## C5 — Ready and Release evidence

Finding: a string event description would preserve the word `Release` but lose the typed Release
identity, while multiple per-group releases could accidentally satisfy a generation-wide barrier.

Disposition: every member has one successful Ready observation before one generation-level typed
Release declaration is emitted. The observation retains that declaration directly and records no
member release order.

## C6 — binding observations

Finding: an authority denial paired with a successful delivery, a mediated exercise without a
Mediation identity, or a failure without attributable detail could create internally contradictory
post-Release evidence.

Disposition: these combinations fail closed. Accepted observations retain typed exercise, binding,
member, source, Mediation, and routing identities plus authority and delivery outcomes. Denial and
failure are never rewritten as delivery.

## C7 — retirement and rollback

Finding: an all-false retirement effect did not distinguish retaining an old generation for
rollback from simply omitting its disposition. Post-cutover failure also needed an explicit single
scope state so both generations could not appear active.

Disposition: the output retains the declared old-generation disposition. Success exposes only the
new generation as active in the target scope; pre-cutover failure and successful rollback expose
only the retained generation; rollback-unavailable and retained-corruption outcomes expose one
degraded target scope and never claim restoration.

## C8 — child and host-assisted activation

Finding: a child Port identity did not prove that the parent was a distinct active scope, and a
host-assisted export flag did not establish observable ordering after the internal Release.

Disposition: child activation requires a distinct active parent scope and matching generation,
preserves that state, refuses sealed Ports, and requires replacement lifecycle declarations only
for occupied Ports. Host-assisted export has a declared sequence constraint and an output event
strictly after the internal Release.

## C9 — authority and widening

Finding: Interconnection establishes Actor and endpoint observations, which could be misread as a
Capability grant, and a second requested scope could silently widen the transaction.

Disposition: every effects record contains `CapabilityGranted = false`; binding authority is a
supplied check observation only. The requested scope must equal CM3's scope, and wider work returns
to resolution rather than entering the runtime.

## C10 — snapshots and complete explanation

Finding: CM4 accepted a public CM3 plan value, so Reference could deep-copy CM4's lists while still
retaining mutable nested plan collections. Release identity and retirement disposition were also
missing from the top-level observation.

Disposition: Reference deep-snapshots the complete nested CM3 plan and every CM4 collection;
Minimal uses persistent values. Both observations retain the typed Release and disposition,
deterministic events and decisions, binding exercises, child declaration, scope results, and effect
profile.

## Residual boundary

CM4 does not decide whether requested authority should be granted, validate real credentials,
establish production operating-system isolation, execute arbitrary package code, persist durable
rollback state, or prove atomic distributed cutover. Requested Actor relationships, local admission,
Capability grants, denials, revocation, expiry, and attributable policy mistakes remain CM5 work.
