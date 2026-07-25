# Brontide Next-Steps Working Analysis 0.1

**Status:** Temporary, deletion-gated working note (not pinned evidence, not a specification)
**Date:** 2026-07-24
**Scope:** Planning and risk analysis only. Changes no architecture or implementation semantics.
**Assumes:** Priority 0 (Pinned Documentation Relocation) is treated as complete.

This note collects four working analyses requested after the current-state evaluation: (A) a
line-ending cleanup runbook, (B) an ordered cross-stack breakdown of the Portable Component
Binding programme, (C) an audit of the open 0.7 M3/M4 (R3/R4) delivery gap, and (D) a map of the
0.7 → 0.8 conflict surface. It exists to be executed against and then deleted; nothing here is a
conformance or ratification claim.

---

## A. Line-ending cleanup (mechanical, do first)

### Finding

The working tree currently reports every text file as modified — a symmetric diff (equal
insertions and deletions) — with no real content change. Root cause: **committed blobs have mixed
line endings** while the working tree is uniformly CRLF and there is no `.gitattributes`.

- Documentation and conformance JSON are stored **LF** in the repo (e.g. `README.md`,
  `Brontide-Architecture-Status.json`, `conformance/*.json`, the architecture docs, review
  attestations). These show as "modified" because Visual Studio rewrote the working copies to CRLF.
- **All C#/F# source, and a handful of READMEs/fixtures, are stored CRLF** in the repo (e.g.
  `AGENTS.md`, everything under `Reference/src`, `Minimal/src`, most `interchange/` fixtures).
  These currently match the CRLF working tree and so do *not* show as modified.

`core.autocrlf` is unset. A stale `.git/index.lock` is present (Visual Studio holding it); clear it
before any git operation.

Why it matters: both stack completion gates and the relocation closure require the repository gate
to pass **from a clean worktree**. This phantom diff buries real changes and can silently rewrite
evidence-pinned documents if committed carelessly.

### The pinned-hash hazard

A naive `* text=auto` + `git add --renormalize .` normalizes everything to LF. Because the
**pinned documents are already LF**, they are *absent* from a renormalize diff — their content and
SHA-256 do not change. The renormalize commit touches only the **CRLF-in-repo set** (source and a
few fixtures/READMEs), none of which is SHA-256-pinned in the status registry. This is safe *if
verified*, not assumed.

The files to double-check against the evidence registry before committing (CRLF-in-repo, therefore
they *will* appear in the renormalize diff):

- `docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md`
- `docs/archive/foundation/Brontide-Reference-Stack-Implementation-Plan-0.2.md`
- `interchange/manifest-v2.json`, `interchange/contract-matrix.md`, and the
  `interchange/messages/*.json` / `interchange/values/*.json` fixtures

If any of those is pinned by an attestation or a `verify-*` script, defer *its* normalization to the
authorized repinning/fresh-review window; normalize the rest now.

### Runbook (run on Windows, in the repo, from a clean checkout)

Two committed files are already provided at the repo root by this analysis: `.gitattributes` and
`.editorconfig`.

```powershell
# 0. Close Visual Studio. Remove any stale index lock.
Remove-Item .git\index.lock -ErrorAction SilentlyContinue

# 1. Discard the uncommitted VS line-ending churn so you start clean.
#    (These are unwanted working-tree rewrites, not real edits.)
git checkout -- .

# 2. Stage the policy files.
git add .gitattributes .editorconfig

# 3. Renormalize the whole tree to the new policy.
git add --renormalize .

# 4. INSPECT before committing. Confirm only source / non-pinned files appear.
git diff --cached --stat
#    Cross-check the list against conformance/reviews/attestations/*.json and the
#    verify-evidence / verify-text gates. If a pinned doc appears, unstage it:
#    git restore --staged <path>   (and handle it in the repinning window)

# 5. Run the local gates before committing (Windows, .NET 10 SDK).
.\build\verify-text.ps1
.\build\verify-evidence.ps1
.\build\verify-interchange.ps1

# 6. Commit as a single, clearly-scoped normalization.
git commit -m "chore: add line-ending policy and normalize working tree to LF"
```

Expected end state: `git status` clean, future VS saves no longer churn endings, and the repository
gate runs against a genuinely clean worktree.

---

## B. Portable Component Binding — ordered cross-stack breakdown

The plan (`docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md`)
already defines phases PB0–PB8 and a capability contract C1–C10. This section turns that into an
execution ordering with explicit gates, the critical path, and — importantly — its dependency on
analysis D.

### Hard prerequisites (gates before PB1 code)

Three plan open-questions are real blockers, not paperwork. Treat them as PB0 exit criteria:

1. **Wire representation for the process realization.** Which restricted CBOR subset, scalar tags,
   canonicalization, identifier widths, and maximum bounds? Blocks PB1 fixtures and PB4
   process-parity. Owner: Portable Binding contract maintainers.
2. **Referenced-shaped-resource v0.1 floor.** Smallest viable form: copied immutable blob, borrowed
   read-only region, transferred ownership, or a deliberately smaller subset. Blocks PB1 resource
   schema and PB6. Owner: contract maintainers + both stack owners.
3. **Revocation-ceiling / representation choice (from analysis D, C11).** Both stacks MUST record
   their chain-conjunction representation (carried / pre-evaluated / resolved) **before** PB freezes
   a portable representation. This is the sharp cross-cut: PB1 and PB3 must not bake in a
   representation that caps future revocation semantics. See section D.

### Phase ordering, owners, and dependencies

| Phase | Deliverable | Owner | Depends on | Exit gate |
| --- | --- | --- | --- | --- |
| PB0 | Baseline inventory of Cooling/Catalog fields, map each to C1–C10 + Channel vectors, mark reusable/fixture/contradictory/missing; create `binding/portable/` matrix; **resolve the 3 prerequisites above** | Both | Analysis D (C11 ceiling) | Every C-item + Channel vector has owner, evidence path, expected observation; encoding questions resolved, not implicit |
| PB1 | Data-only versioned neutral contracts: references + Shape floor, provisions/requirements, negotiated Operations/Shapes/Fragments, authority-presentation + no-capability-transfer, inline + referenced-resource decls, limits/lifecycle, immutable Binding Plan facts, Channel envelopes/correlation/errors, C9 observations. Valid + additive + adversarial fixtures. | Contract maintainers | PB0 | Artifacts self-contained, deterministic, validated without loading either stack |
| PB2 | Reference native impl: refactor `Experimental.Binding` behind a fixture-neutral contract; strict decode, plan compilation, native Shape projection, authority admission, direct dispatch, process framing, resource-scope, lifecycle, C9. Cooling/Catalog become adapters. Core stays free of binding/transport. | Reference | PB1 | Reference passes all neutral vectors; direct + local-process realizations report equal semantic observations |
| PB3 | Minimal native impl in `Brontide.Minimal.Binding` using ADTs + explicit results; **not** a mechanical port of the Reference surface. Model/Kernel stay free of transport. Includes strong three-valued authority + polarity-flip at the Channel boundary. | Minimal | PB1 (parallel to PB2) | Minimal passes all neutral vectors; direct + process parity |
| PB4 | Direct vs negotiated-process parity in each stack over a real duplex boundary; length-delimited bounded framing for the portable wire (retained line-delimited JSON stays diagnostic only). | Both | PB2, PB3 | Every Channel 0.1 vector runs independently in both stacks; direct/process parity holds |
| PB5 | Cross-stack + independent-provider matrix: Ref→Min, Min→Ref (process), Ref/Min → neutral fixture (process), Ref→Ref, Min→Min (direct). Cooling for authority/projection/failure; Catalog for multi-op/nested/resource/replay/bounds. | Both | PB4 | Both directions + neutral provider pass with no shared executable semantics or private types |
| PB6 | Resource + lifecycle + hardening adversarial coverage: ownership/borrow, premature reuse, release, scope escape, integrity mismatch, fallback; establishment-fail-before-activation, withdrawal, peer loss, timeout, interruption, duplicate terminal, exhaustion; bounded fuzz of decoders. | Both | PB5 | C6 + C8 have positive + negative evidence in both stacks and across the seam |
| PB7 | Composition handoff: narrow adapter producing a Binding Plan at activation preflight, preserving scope + provider identity. **No** discovery/acquisition/selection/generations/mediation/hot-swap. | Both | PB6 | One controlled composition per stack establishes + releases a portable binding without moving it into Base/Core/Model/Kernel |
| PB8 | Evidence + docs + review closure: update READMEs, milestone evidence, public boundaries, changelogs; update Channel ledger + contract matrix; re-measure source/runtime cost; run `build/verify-portable-binding.ps1` then full gate from clean worktree; fresh independent reviews of Ref, Min, and the neutral contract. | Both | PB7 | C1–C10 evidence passing + discoverable; gate green; reviews have no unresolved in-scope findings |

### Critical path and parallelism

`PB0 → PB1 → {PB2 ∥ PB3} → PB4 → PB5 → PB6 → PB7 → PB8`. PB2 and PB3 are the parallelizable block
(one per stack). Everything else is serial. New root gate `build/verify-portable-binding.ps1` is
introduced in PB0/PB1 layout and wired into the repository gate.

### Watch-items

- **Independence discipline is the whole point.** PB3's warning against an OO compatibility facade
  "merely to make tests look alike" is the failure mode to police in review — the Minimal impl must
  be idiomatic F#, not a transliteration.
- **Neutral layer must stay data-only.** No generated C#/F# in `binding/portable/`; generation runs
  per-stack from checked-in neutral source.
- **Do not upgrade ratification language** anywhere in PB8; this stays experimental 0.8 evidence.

---

## C. The open 0.7 gap — M3/M4 (R3/R4)

Both stacks have delivered M1/R1 (composite constraint poisoning, `BR_07_CONSTRAINT_001/002/003`)
and M2/R2 (typed member canonical names). The remaining runtime-bearing 0.7 changes are C3, C4, C5
— all still classified `missing` and none built. This is the honest gap in the current target.

### C3 — static Attribute-constrained binding (§18.1) → M3 / R3

Lands in `Experimental.Composition` in each stack (no new project). Required behavior:

- binding creation selects **once** under the declared constraints;
- the binding holds an immutable resolved value **plus selection provenance**;
- later candidate/attribute changes do **not** rebind;
- unresolved creation **fails explicitly** rather than becoming a live query;
- restoration preserves the prior resolution and does not silently reselect.

Vectors (native, failing-first, before reading the other stack): mutation, removal, a *better* later
candidate, ties, restoration, unsupported constraint. Minimal writes these in
`Brontide.Minimal.Composition.Tests`; Reference in its Studio-owned experimental Composition suite.

### C4 + C5 — Router endpoints and Dataset authority/identity/concurrency → M4 / R4

This is the larger lift and needs a **new experimental project that does not yet exist** in either
stack:

- `Minimal/src/Brontide.Minimal.Experimental.PersistentInformation` (+ `...PersistentInformation.Tests`)
- `Reference/src/Brontide.Reference.Experimental.PersistentInformation` (+ tests)

Neither directory is present today, so M4/R4 also carries scaffolding cost: create the projects, add
them to the solution/build graph, and extend the dependency-boundary verifier
(`verify-project-graph.ps1` / `verify-assembly-graph.ps1` / Minimal `verify-boundaries.ps1`) so the
new experimental project cannot leak into Base/Core/Model/Kernel.

**C4 — Router logical-endpoint guarantees (§18.2).** Router-owned guarantee tests, including behavior
when the backing Store changes, and explicit refusal of any guarantee the Router cannot uphold.
Endpoint guarantees belong to the Router and must **not** be inferred from a backing Store — the
negative test (Store guarantees leaking through a Router) is required evidence.

**C5 — Dataset authority, identity, concurrency (§12, §18.2, §21.1).** Required behavior:

1. Capability-governed Dataset designation/creation, with the **Genesis-vs-issuance** choice explicit
   in the evidence.
2. Dataset identity carried by the Dataset record **independently of any one Store**, with the Corpus
   declaring which Store roles are identity-bearing.
3. Identity-bearing Opaque Corpus, Dataset, and Store role references.
4. Explicitly declared concurrent-access behavior; **missing declarations are rejected**.
5. A minimal read/write/append path proving identity, authority, persistence, and declared
   concurrency observations.
6. Negative evidence: wrong Capability, wrong target, unsupported concurrency, Store-guarantee leakage.

Minimality is judged by surface area, not by dropping required semantics. Other persistent-information
roles stay deferred.

### Sequencing note (interacts with B and D)

M3/R3 is small and self-contained — good to land first. M4/R4 is the persistent-information slice and
is heavier. Neither blocks Portable Binding (B), which targets 0.8 §16/§18.1 and reuses the
Cooling/Catalog estate — so B and C can proceed in parallel with different focus. **However**, do not
invest in re-hardening M1's poisoning rule: under 0.8 C7 it becomes `conflicting` rework (see D).
Finish M3/M4 as clean 0.7 Complete-Draft evidence; don't gold-plate M1.

---

## D. The 0.7 → 0.8 conflict surface

Architecture 0.8 is a Complete Draft executing change plan C1–C14 with an authored adversarial vector
set (`conformance/architecture-0.8-adversarial-vectors.json`). Two decided changes already touch
delivered 0.7 evidence, and one of them directly gates the Portable Binding work in B.

### D1 — C7: three-valued (Kleene) evaluation supersedes expression poisoning

**What changes.** The 0.7 poisoning rule (deny whenever any atom in a composite is unrecognised) is
replaced by strong three-valued Kleene logic. An unrecognised/unevaluatable atom is **Unknown**:

- `Not(Unknown) = Unknown`
- `AllOf` is False if any member is False, True only if all True, else Unknown
- `AnyOf` is True if any member is True, False only if all False, else Unknown
- Authority: authorise only on **True**; Unknown and False deny.
- Selection: retain only on True, and the resolver SHOULD record every Unknown atom.
- **Structural evaluation** — implementations MUST NOT reason across repeated atoms:
  `AnyOf(X, Not(X))` with X Unknown is **Unknown**, not a tautology.

**Impact on delivered evidence.** `BR-07-CONSTRAINT-001/-002/-003` (M1/R1) become `conflicting`
rework in a future 0.8 delivery audit. They remain **valid 0.7 Complete-Draft evidence** and are not
relabelled — but do not build further on the poisoning semantics.

**Behavioral inversions to expect (the review traps):**

| Expression (X unrecognised) | 0.7 poisoning | 0.8 Kleene | Note |
| --- | --- | --- | --- |
| `Not(U)` | deny | deny (`Not(Unknown)=Unknown`) | unchanged outcome |
| `AnyOf(True, U)` | deny (poisoned) | **authorise** | the migration case poisoning foreclosed |
| `AllOf(True, U)` | deny | deny (Unknown) | unchanged outcome |
| `AnyOf(U, False)` | deny | deny (Unknown) | unchanged outcome |
| `AllOf(False, U)` | deny | deny (False) | short-circuit False |
| `AnyOf(X, Not(X))` | deny | deny (Unknown — **no tautology**) | the structural guard rail |
| selection `AnyOf(match, U)` | candidate **excluded** | candidate **retained** + record U | direct inversion |

The `AnyOf(X, Not(X))` vector is the one that separates a correct Kleene implementation from a
"clever" supervaluating one — the latter would authorise where the former denies, a silent divergence
between conforming stacks. Both stacks' 0.8 evaluators must pin the non-clever answer.
Cross-implementation vectors are tagged `BR-08-ADV-C7-*` (and `C8-*`).

### D2 — C8: Constraint values exempt from additive projection

Adjacent to C7 and worth flagging now because it changes how the evaluator treats versioned Constraint
values: additive Shape projection (which lets an old consumer ignore unknown newer structure) is
**disallowed for Constraint values** — a Constraint carrying unrecognised later-version structure is
**Unknown**, not silently projected to its weaker form. Graded strictness across version skew is
expressed only as explicit `AnyOf(NewConstraint, OldConstraint)` fallback, evaluated under C7. A
per-atom `strict = false` escape hatch was considered and **rejected** ("the adversary picks the
evaluator"). Implication: when B builds the portable Shape floor and payload projection, Constraint
positions must be excluded from additive projection — projection applies to Operation/Event/Outcome
values only.

### D3 — C11 (§11): representation choice is the revocation ceiling  ← gates B

C4 of the 0.8 plan names three ways to establish chain conjunction, each with different revocability:

- **carried** (inline ancestor Constraints) — cannot be revoked without a deliberately inserted
  indirection point (`Revocation-via-indirection`, §31);
- **pre-evaluated** static table — revocable only by rebuilding the table;
- **resolved** representation — revokes naturally at its resolver.

The decided rule: a domain **MUST record its representation choice as an operational property**, and
this choice is the ceiling any future revocation semantics can reach. The edit sites explicitly
include *"Reference and Minimal implementation notes (record each stack's representation choice and
ceiling **before the Portable Binding freezes one**)."*

**This is the cross-cut with B.** PB1/PB3 must not freeze a portable chain-conjunction representation
until R6/M6 have recorded each stack's choice and its revocation ceiling. Doing PB first would cap
revocation semantics by accident. **Action:** add the representation-choice record to
`Reference/docs/architecture-0.7-delivery.md` and `Minimal/docs/architecture-0.7-delivery.md` (the
"Architecture 0.8 preparation" sections already flag this) as an explicit PB0 prerequisite.

### D4 — context items (not immediate blockers)

- **C9 — two-plane consolidation + first-class Constraint declarations.** The framing that unifies
  C5/C7/C8; C9 places authority presentation at the boundary in Shape-described form, which is the
  contract PB's cross-trust `no-capability-transfer` seam (B/C3, C4) must match.
- **C12 — Terminus** (counterpart of Genesis): disposition of a retired Actor's held Capabilities
  (extinguished), outbound grants (domain-defined survival schedule), and reference retirement. Design
  direction; affects any future lifecycle work, not PB 0.1.

### Bottom line on ordering

1. Record each stack's chain-conjunction **representation choice + revocation ceiling** (D3) — cheap,
   and it unblocks PB cleanly.
2. Then proceed with Portable Binding (B), keeping Constraint values out of additive projection (D2)
   and building the authority seam to the C9 presentation contract.
3. Finish M3/M4 (C) as clean 0.7 evidence in parallel; **do not** re-harden M1 — it is C7 rework.
4. The 0.8 evidence programme's decided order stands: **Channel → Portable Binding + Shape floor →
   Flow conformance.**

---

*Delete this note once A is committed and B/C/D are folded into the owning plans and stack ledgers.*
