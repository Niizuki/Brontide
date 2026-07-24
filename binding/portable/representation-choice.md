# Portable Binding — chain-conjunction representation choice (D3 / Architecture 0.8 §11)

**Status:** PB0 prerequisite record. Non-pinned working artifact. Not ratified; not an Architecture
0.8 conformance claim; changes no architecture or implementation semantics and does not alter either
stack's Architecture 0.7 target.

## Why this record exists

Architecture 0.8 §11 requires that an implementation evaluate the conjunction of **all** Constraints
along a Capability's derivation chain at the authorisation boundary, and that a domain **record its
representation choice as an operational property**, because that choice is the domain's **revocation
ceiling**:

- **carried** — ancestor Constraints inline in the Capability representation. Cannot be revoked
  without a deliberately inserted indirection point; §31 revocation-via-indirection is the recorded
  candidate mechanism.
- **pre-evaluated** — flattened into a static delegation table. Revocable only by rebuilding the
  table.
- **resolved** — resolved through domain machinery. Revokes naturally at its resolver.

The Portable Binding freeze (plan PB1/PB3) MUST NOT adopt a portable chain-conjunction
representation that caps either stack's ceiling below what that stack records here. Recording both
choices is therefore a PB0 prerequisite (plan §5 PB0 exit; Next-Steps analysis §D3).

## Reference stack — carried

Determined from `Reference/src/Brontide.Reference.Core/Authority.cs`:

- `Capability` is an immutable object holding only the Constraints added at its own derivation step
  (`AddedConstraintExpressions`) plus a direct in-memory reference to its `Parent` Capability.
- The conjunction is assembled at the authorisation boundary by walking that parent chain —
  `DerivationChain()` → `EffectiveConstraintExpressions()` — and evaluating every ancestor Constraint
  together via `ConstraintExpressionEvaluator`.
- Ancestor Constraints therefore travel **inline** in the presented Capability's object graph. They
  are neither pre-evaluated into a static table nor resolved through a domain-side indirection.

**Choice: carried. Revocation ceiling: carried** — no natural revocation without a deliberately
inserted indirection point (§31 revocation-via-indirection is the candidate should §33 revocation
semantics require it). The separate `LivenessLease` mortality axis (§10.3) is a distinct liveness
scope, not the chain-conjunction representation.

## Minimal stack — resolved

Determined from `Minimal/src/Brontide.Minimal.Kernel/Kernel.fs` (with `Brontide.Minimal.Model`):

- `Capability.Parent` is an opaque `CapabilityReference`, not the parent object.
- The conjunction is assembled at the authorisation boundary by resolving each link through the
  `World` authority store: `capabilityChain` follows each `Parent` via
  `Map.tryFind parentReference world.Capabilities`, and `effectiveConstraints` gathers each link's
  expressions from `world.CapabilityConstraintExpressions`.
- Ancestor Constraints are therefore **resolved through domain machinery** (the `World`), consistent
  with Minimal keeping opaque issuer-controlled references. They are neither carried inline in the
  presented reference nor pre-evaluated into a static table.

**Choice: resolved. Revocation ceiling: resolved** — revokes naturally at its resolver (the `World`
capability store); future §33 revocation semantics can be expressed by making chain resolution refuse
a withdrawn link, without inserting a new indirection point.

## Consequence for the Portable Binding freeze

The two stacks made **different** choices:

| Stack | Representation | Revocation ceiling |
| --- | --- | --- |
| Reference | carried | via-indirection only (tightest) |
| Minimal | resolved | at the resolver |

Both satisfy §11's floor (both evaluate the full chain conjunction). But the portable
chain-conjunction representation frozen in PB1/PB3 must accommodate **both** and cap **neither**:

- It must tolerate a **carried** peer whose only revocation path is §31 revocation-via-indirection —
  this is the binding constraint, because carried is the tighter ceiling.
- It must not *require* resolver-based revocation semantics that a carried peer cannot provide.
- It must not silently downgrade Minimal's resolver-natural revocation, but it cannot *rely* on that
  revocability across a trust boundary.

Practical direction: the portable contract should treat cross-trust chain-conjunction revocability as
**carried-tier by default**, exposing any resolver / indirection point as a **declared** capability
rather than an assumed one.

## Pending pinned-ledger transcription (deferred)

The intended permanent home for this record is the "Architecture 0.8 preparation" section of each
stack's delivery ledger — `Reference/docs/architecture-0.7-delivery.md` and
`Minimal/docs/architecture-0.7-delivery.md` — which already flag that the stack "must record [its]
representation choice here before the Portable Binding freezes one."

Those ledgers are SHA-256-pinned in `Brontide-Architecture-Status.json`, which is itself pinned by
`conformance/reviews/review-request.json`, and the independent-review attestations
(`conformance/reviews/attestations/{reference,minimal}.json`) certify the ledgers as reviewed at
commit `2049554c8e7ee5c26e4fcae6a103997737aa90f2`. Transcribing this record into them therefore
requires the authorized repinning + fresh-review window — the same window the Pinned Documentation
Relocation is waiting on. Until that window is opened, **this non-pinned artifact is the operative D3
record**, and PB0/PB1 may reference it directly.
