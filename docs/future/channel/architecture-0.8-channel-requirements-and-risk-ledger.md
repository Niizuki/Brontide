# Architecture 0.8 Channel requirements and risk ledger

Status: non-runtime planning evidence for the Architecture 0.8 `Channel` direction. This is a
requirements and risk register, not a ratified Channel contract, an extension specification, or an
implementation claim. It adds no Brontide Base term.

Design source: [Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md).
Draft semantic contract: [Draft Channel Contract 0.1](./Brontide-Draft-Channel-Contract-0.1.md).
Shared vectors: [Channel 0.1 conformance vectors](../../../conformance/channel-0.1-vectors.json).
Evidence base: the retained Cooling and Catalog interchange proofs governed by the
[Reference/Minimal Interchange Implementation Plan 0.1](../../archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md).
Architecture context: [Brontide Architecture 0.8](../architecture/Brontide-Architecture-0.8.md) §6.16, §8,
§13.6, §16.4, §19, §24, §33, §35.1.

This ledger is the deliverable the Reference and Minimal 0.3 plan phases R6/M6 call for. It exists
so that the `Channel` extension's open questions are tracked before any wire format or realisation
freezes them (§6.8, §35.1). Completing an item here creates a decision or a piece of evidence for
review; it does not by itself ratify Channel or authorise implementation.

## 1. Scope boundary

Channel is the recorded first-cycle communication extension: the request and Outcome
representation, correlation, error propagation, and delivery semantics that §13.6 needs and Base
withholds. It precedes the Portable Component Binding, which becomes its first conforming
realisation, and is derived from the shared behaviour of two independently implemented interchange
protocols rather than drafted abstractly.

Out of scope for Channel, and therefore out of scope for this ledger: cross-domain mutual
identification, attestation, and any cryptographic cross-domain authority representation (owned by
`Identity`/`Distributed`, §8, §24, §33); delivery guarantees, ordering, and long-running Executions
(owned by `Distributed`, `Realtime`, `Flow`, `Lifecycle`); and the concrete Portable Binding
encoding (its own §18.1 work, realising this frame).

## 2. Requirements register

Each requirement is what the `Channel` extension must eventually settle. Disposition is one of
`open`, `decided-in-note` (the design note records the semantic answer; the portable form remains
open), `decided-in-draft` (the draft contract records a reviewable semantic answer without
ratification), `vectors-authored` (shared data-only vectors exist but stack harnesses remain
evidence-gated), `realisation-executed` (a conforming realisation executes the vectors in both
stacks; the semantics are evidenced but not ratified), or `evidence-gated` (answerable only against
a running conformance realisation).

| ID | Requirement | Source | Disposition |
| --- | --- | --- | --- |
| CH-R1 | Message envelope model: a versioned, kind-discriminated message in the categories negotiation, request, outcome, protocol-error, lifecycle. | note §"envelope model"; Cooling/Catalog `kind` | decided-in-draft; logical version-1 Envelope/body Shapes recorded, encoding open |
| CH-R2 | Error taxonomy: a standard category set, with realisation codes mapped onto it. Category, never code string, is normative. | note §"envelope model"; stacks' code divergence | decided-in-draft; twelve protocol categories and seven local process categories enumerated |
| CH-R3 | Correlation model: at least a request correlation identity, echoed on the Outcome and matched on receipt; carried identities never conflated with host-native Execution or Occurrence identity. | note §"correlation"; §8; Plan §3.3 | decided-in-draft; channel/request plus optional finer identities recorded as distinct opaque positions |
| CH-R4 | Compatibility model: version declared on every message; fail closed on an unrecognised version; compatibility settled before or independently of invocation (negotiated handshake or fixed contract). | note §"version and contract"; §6.16 | decided-in-note; handshake-vs-fixed left to realisation |
| CH-R5 | Authority presentation: boundary-relative — intra-domain Capability presentation for target evaluation, cross-trust-boundary attributable context only; **no Capability crosses a trust boundary**. | note §"authority presentation"; §6.16, §8, §24 | decided-in-note; intra-domain representation is the Portable Binding's subject |
| CH-R6 | Failure separation: denial (boundary decision, never a wire message), semantic failed Outcome (structured `details`), and protocol/process failure (category code + failure domain) remain three distinct meanings; no foreign exception or runtime type crosses. | note §"failure taxonomy"; Cooling forbidden-field scan | decided-in-draft; five relative failure domains recorded |
| CH-R7 | Two-plane classification: every Shape-described Channel position is declared covariant (payload, projects under §16.4) or contravariant (authority, fail-closed, never projected). | note §"relationship to the two planes"; §6.16 | decided-in-draft; every logical envelope position classified, including C8 Constraint exemption |
| CH-R8 | Transport and framing: framed, self-delimited messages over a duplex transport, one message per frame, diagnostic side band carrying no semantic result; a realisation declares framing and any frame-size bound. | note §"framed messages"; Cooling/Catalog stdio JSON-lines | decided-in-note; transport left open |
| CH-R9 | Declared hardening dimensions: replay window, payload bound, field strictness, parse bounds — each stated explicitly, including stating that none is provided. | note §"boundary hardening"; Catalog vectors | decided-in-note; declaration form open |
| CH-R10 | Non-promises: no delivery, ordering, or retry guarantee; interruption, retry, and fallback recorded as facts, success never fabricated. | note §"non-promises"; Plan §4 | decided-in-note |
| CH-R11 | Conformance vectors: a vector set expressing one Channel contract runnable against both stacks, so the C7/C8 constraint-evaluation rules and the failure taxonomy are checked identically. | §29.2 discipline; adversarial-vector precedent | realisation-executed; all 24 shared vectors have executed evidence in each stack independently, derived from the Portable Binding's neutral declarations rather than restated (see §4) |

## 3. Risk register

| ID | Risk | Severity | Mitigation |
| --- | --- | --- | --- |
| CH-K1 | Premature wire-format freeze: an encoding ships and silently fixes the semantic answers (§6.8 accident). | High | Channel-first sequencing (§35.1): settle semantics in this cycle; the Portable Binding realises them second. This ledger tracks the semantics independently of any encoding. |
| CH-K2 | Error-taxonomy divergence: the two stacks already emit different protocol-error code strings for equivalent conditions, so independent realisations diverge silently. | High | CH-R2 standardises categories not spellings; CH-R11 vectors assert category equivalence across stacks. |
| CH-K3 | Authority-plane leakage: a realisation lets a Capability cross a trust boundary, or projects an authority-position value to a weaker version (broadening). | Critical | CH-R5 no-capability-transfer invariant; CH-R7 contravariance forbids projection of authority positions; the Cooling manifest already fails closed when `no-capability-transfer` is absent. |
| CH-K4 | Correlation-identity conflation: a Channel identity is treated as a cross-domain Execution/Occurrence identity, laundering provenance. | Medium | CH-R3 distinctness invariant; the retained proofs already assert the binding-scoped ids never equal the host Execution id. |
| CH-K5 | Over-scoping: Channel accretes delivery, ordering, streaming, or long-running semantics that belong to later extensions. | Medium | Explicit §1 non-goals; CH-R10 non-promises; those semantics remain with `Distributed`/`Realtime`/`Flow`/`Lifecycle`. |
| CH-K6 | Hardening asymmetry surprises interop: one side enforces replay/payload limits and another does not, so a benign peer is rejected or an abusive peer admitted. | Medium | CH-R9 requires each hardening dimension to be declared, including its absence, so a peer's expectations are explicit rather than discovered at runtime. |
| CH-K7 | Denial mistaken for a transported result: a realisation invents a denial wire message, implying the far side observed the request. | Low | CH-R6 keeps denial a boundary decision; the unused `interchange/messages/denial.json` envelope is retained as evidence that denial does not cross. |

## 4. Evidence status against the retained proofs

What the Cooling and Catalog proofs already establish for Channel, versus what remains open:

- **Established.** Framed one-message-per-frame exchange (CH-R8); a versioned, kind-discriminated
  envelope with correlation (CH-R1, CH-R3); fail-closed version handling and pre-invocation
  compatibility (CH-R4); host-side authority with no Capability crossing (CH-R5); the three-way
  failure separation and the no-exception-crosses guarantee (CH-R6); replay, payload-limit,
  strict-field, and version-skew handling as realisation choices (CH-R9); and the delivery
  non-promises with facts-not-fabrication (CH-R10). Each is demonstrated in both host directions
  across a real process boundary.
- **Decided in the non-ratified draft.** The category-level error taxonomy (CH-R2); logical
  correlation and envelope Shapes (CH-R1, CH-R3); relative failure domains (CH-R6); and the
  per-position covariant/contravariant classification including the C8 polarity flip (CH-R7).
- **Authored and now executed.** Twenty-four shared data-only vectors cover CH-R11, including C7/C8,
  category mapping, frame/no-frame failure separation, and failure attribution. Independent stack
  adapters now exist: the Portable Component Binding executes every one of them in each stack.
- **Open for the next realization.** The exact intra-domain authority-presentation representation
  is the Portable Binding's subject (CH-R5), as are concrete encoding, descriptor, and harness APIs.

The proofs are test instruments, not a specification: passing them ratifies neither Channel nor a
Portable Binding.

### What the Portable Binding realisation has since evidenced

The Portable Component Binding is Channel's first conforming realisation (§1). PB2 through PB7 have
executed it, and what that adds to the register above is recorded here rather than in the
dispositions, because evidence is not ratification and the ledger's semantic answers are unchanged.

- **CH-R11 is executed rather than pending.** Each stack runs every Channel vector, and the
  accounting is derived rather than asserted: each stack reads
  [`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json) together
  with the neutral vectors' own `channelVectors` declarations, and counts a Channel vector as
  executed only when a portable vector the neutral layer says preserves it is executed by that
  stack. Removing a test, deferring a vector, or renaming a Channel vector fails the build.
- **CH-R2 categories survived contact with two implementations.** The taxonomy is reproduced exactly
  — no category added, none removed — and PB6 made process-category classification total, so a
  foreign runtime failure cannot escape as itself. Two categories that had been declared without a
  reachable path (`resource-exhausted`, `unknown`) now have behavioural evidence;
  `peer-unavailable` remains unreachable by design, because the binding layer never starts a peer.
- **CH-R6's three-way separation holds under adversarial pressure.** Denial is frameless, a semantic
  failure is a shaped Outcome, and a protocol rejection and a process loss stay distinct — proved
  across a real seam rather than through a codec called directly, and with failure paths shown to
  leak no provider effect, value, runtime type, resource, or false success.
- **CH-R8's framing question has one worked answer.** The portable wire is length-delimited with a
  4-byte big-endian prefix bounded at 65 536 bytes, and a retained line-delimited JSON message is
  refused on its length prefix alone. Channel still leaves transport open; this is one realisation's
  declaration, not a narrowing of the requirement.
- **CH-K2 (error-taxonomy divergence) is the risk the evidence most directly reduces.** The two
  stacks now report the same category for the same condition in both realisations and in both
  cross-stack directions, and an implementation-neutral endpoint that imports neither stack agrees.
- **CH-K3 and CH-K4 have executed negative evidence.** No Capability crosses a trust boundary — the
  host refuses to emit authority-bearing content before anything leaves it — and Channel correlation
  identities are asserted never to equal the host-native Execution identity.

One caution belongs with this: every defect the hardening phase found was present *identically* in
both stacks, so agreement between two independent implementations is evidence about the contract's
ambiguity and not about its silence. That is recorded as Decision 10 in
[`binding/portable/open-decisions.md`](../../../binding/portable/open-decisions.md) and is a
Channel-relevant caveat, not only a binding-programme one.

## 5. Recorded test scenarios (forward)

These are recorded targets for the eventual Channel realisation and its conformance work; none is
implemented here.

- **Cross-stack conformance vectors (CH-R11).** *Delivered.* The shared vector file records
  request/Outcome correlation, category mapping, unrecognised versions, frame/no-frame failure
  separation, C7 strong-Kleene outcomes, and the C8 authority-position value that must not project.
  Two independent stack adapters now assert identical category-level observations, in both host
  directions and against an endpoint that imports neither stack.
- **Channel-provider Component required by another Component.** *Partly delivered.* A resolved
  Component requirement and an offered provision now produce a Binding Plan at activation preflight,
  and a controlled composition in each stack establishes and releases the resulting binding over the
  Channel frame, with ordinary interaction gated until the provider is ready. What remains is the
  half this ledger names: the Composition **Provider-Set machinery** — cardinality beyond `1..1`,
  mediated exposure, and the resolver that produces the resolution. The handoff refuses each of them
  rather than approximating one, so the boundary is enforced rather than described. That remainder
  belongs to the Component-management experimental track, not to Base.
- **Portable Binding as first realisation.** *Delivered.* The Portable Component Binding (§18.1)
  realises this frame against the §6.16 presentation contract, with a fixed direct-call and a
  negotiated process realisation compared scenario by scenario, so protocol cost is distinguished
  from implementation cost. Structural cost is recorded in
  [`interchange/binding-measurements.json`](../../../interchange/binding-measurements.json);
  optimising the hot path remains an explicit 0.1 non-goal.

## 6. Sequencing and non-goals

Decided order (§35.1): **Channel** (this direction, derived from the retained interchange evidence)
→ **Portable Component Binding and the Shape floor** (Channel's first conforming realisation against
the §6.16 presentation contract) → **Flow conformance** (Event Distribution and the revocation
horizon terminate in it).

Non-goals, unchanged: `Identity` and `Distributed` cross-domain trust wait for proven intra-domain
interchange; `Presentation` and `Workspace` wait; revocation beyond mortality advances only as far as
Flow ratification forces, now bounded by the representation-ceiling rule (§11).

## 7. Hand-off boundary

This ledger is the R6/M6 planning artifact. Its semantic register is complete for hand-off: every
requirement carries a current disposition, every risk has a mitigation, and the unresolved work is
explicitly evidence-gated in the Portable Binding realization. It is superseded when the
`Channel` extension direction is either specified or its items are dispositioned into that
specification. It never changes the architecture status, the implementation baseline, or any
ratification claim; those remain governed by the status registry.
