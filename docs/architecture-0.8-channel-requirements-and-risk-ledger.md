# Architecture 0.8 Channel requirements and risk ledger

Status: non-runtime planning evidence for the Architecture 0.8 `Channel` direction. This is a
requirements and risk register, not a ratified Channel contract, an extension specification, or an
implementation claim. It adds no Brontide Base term.

Design source: [Channel Design Note 0.1](../Brontide-Design-Note-Channel-0.1.md).
Evidence base: the retained Cooling and Catalog interchange proofs governed by the
[Reference/Minimal Interchange Implementation Plan 0.1](../Brontide-Interchange-Implementation-Plan-0.1.md).
Architecture context: [Brontide Architecture 0.8](../Brontide-Architecture-0.8.md) §6.16, §8,
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
open), or `evidence-gated` (answerable only against a running conformance realisation).

| ID | Requirement | Source | Disposition |
| --- | --- | --- | --- |
| CH-R1 | Message envelope model: a versioned, kind-discriminated message in the categories negotiation, request, outcome, protocol-error, lifecycle. | note §"envelope model"; Cooling/Catalog `kind` | decided-in-note; canonical Shapes open |
| CH-R2 | Error taxonomy: a standard category set, with realisation codes mapped onto it. Category, never code string, is normative. | note §"envelope model"; stacks' code divergence | open (taxonomy enumeration) |
| CH-R3 | Correlation model: at least a request correlation identity, echoed on the Outcome and matched on receipt; carried identities never conflated with host-native Execution or Occurrence identity. | note §"correlation"; §8; Plan §3.3 | decided-in-note; portable form open |
| CH-R4 | Compatibility model: version declared on every message; fail closed on an unrecognised version; compatibility settled before or independently of invocation (negotiated handshake or fixed contract). | note §"version and contract"; §6.16 | decided-in-note; handshake-vs-fixed left to realisation |
| CH-R5 | Authority presentation: boundary-relative — intra-domain Capability presentation for target evaluation, cross-trust-boundary attributable context only; **no Capability crosses a trust boundary**. | note §"authority presentation"; §6.16, §8, §24 | decided-in-note; intra-domain representation is the Portable Binding's subject |
| CH-R6 | Failure separation: denial (boundary decision, never a wire message), semantic failed Outcome (structured `details`), and protocol/process failure (category code + failure domain) remain three distinct meanings; no foreign exception or runtime type crosses. | note §"failure taxonomy"; Cooling forbidden-field scan | decided-in-note; failure-domain vocabulary open |
| CH-R7 | Two-plane classification: every Shape-described Channel position is declared covariant (payload, projects under §16.4) or contravariant (authority, fail-closed, never projected). | note §"relationship to the two planes"; §6.16 | open (per-position classification) |
| CH-R8 | Transport and framing: framed, self-delimited messages over a duplex transport, one message per frame, diagnostic side band carrying no semantic result; a realisation declares framing and any frame-size bound. | note §"framed messages"; Cooling/Catalog stdio JSON-lines | decided-in-note; transport left open |
| CH-R9 | Declared hardening dimensions: replay window, payload bound, field strictness, parse bounds — each stated explicitly, including stating that none is provided. | note §"boundary hardening"; Catalog vectors | decided-in-note; declaration form open |
| CH-R10 | Non-promises: no delivery, ordering, or retry guarantee; interruption, retry, and fallback recorded as facts, success never fabricated. | note §"non-promises"; Plan §4 | decided-in-note |
| CH-R11 | Conformance vectors: a vector set expressing one Channel contract runnable against both stacks, so the C7/C8 constraint-evaluation rules and the failure taxonomy are checked identically. | §29.2 discipline; adversarial-vector precedent | open |

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
- **Open.** The canonical error taxonomy (CH-R2); the portable correlation and envelope Shapes
  (CH-R1, CH-R3); the per-position covariant/contravariant classification (CH-R7); the intra-domain
  authority-presentation representation, which is the Portable Binding's subject (CH-R5); the
  failure-domain vocabulary (CH-R6); and a cross-stack conformance-vector set (CH-R11).

The proofs are test instruments, not a specification: passing them ratifies neither Channel nor a
Portable Binding.

## 5. Recorded test scenarios (forward)

These are recorded targets for the eventual Channel realisation and its conformance work; none is
implemented here.

- **Cross-stack conformance vectors (CH-R11).** One authored Channel contract exercised against both
  stacks: request/Outcome correlation, each failure category, an unrecognised version, and an
  authority-position value that must not project (the C8 polarity flip) — asserting identical
  category-level results on both sides.
- **Channel-provider Component required by another Component.** A composition test in which one
  Component *provides* a Channel-conformant contract and a second Component *requires* it, resolved
  through the Composition Provider-Set machinery (§18.1) and exercised over the Channel frame. This
  is an excellent end-to-end vector because it jointly exercises the Channel frame, the Composition
  resolver's required-to-provided binding, and the §13.6 invocation principle across the resulting
  seam. It is a natural future integration between this direction and the Component-management
  harness (a required contract in the CM fixture model), and belongs to that experimental track, not
  to Base.
- **Portable Binding as first realisation.** The Portable Component Binding (§18.1) realising this
  frame against the §6.16 presentation contract, compared with direct-call and process-isolated
  paths so protocol cost is distinguished from implementation cost.

## 6. Sequencing and non-goals

Decided order (§35.1): **Channel** (this direction, derived from the retained interchange evidence)
→ **Portable Component Binding and the Shape floor** (Channel's first conforming realisation against
the §6.16 presentation contract) → **Flow conformance** (Event Distribution and the revocation
horizon terminate in it).

Non-goals, unchanged: `Identity` and `Distributed` cross-domain trust wait for proven intra-domain
interchange; `Presentation` and `Workspace` wait; revocation beyond mortality advances only as far as
Flow ratification forces, now bounded by the representation-ceiling rule (§11).

## 7. Hand-off boundary

This ledger is the R6/M6 planning artifact. It is complete for hand-off when every requirement above
carries a current disposition and every risk a current mitigation, and it is superseded when the
`Channel` extension direction is either specified or its items are dispositioned into that
specification. It never changes the architecture status, the implementation baseline, or any
ratification claim; those remain governed by the status registry.
