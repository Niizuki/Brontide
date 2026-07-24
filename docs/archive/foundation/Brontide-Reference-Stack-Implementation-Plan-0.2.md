# BRONTIDE REFERENCE STACK

## Implementation Plan 0.2

**Status:** Execution-ready working plan
**Companion to:** Brontide Architecture 0.4
**Stack:** C# / .NET 10 (LTS) · Avalonia UI
**First showcase:** the virtual device board
**Execution model:** milestone-by-milestone delegation to Codex

**Current execution note:** Brontide Reference Stack 0.2 and its isolated Architecture 0.5 composition evidence are
recorded in `Reference/docs/milestone-evidence.md`. The next Brontide Reference Stack work is the Brontide Reference Stack-owned half of
`docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md`; this plan remains the historical source for the
completed Brontide Reference Stack milestone semantics.

---

## 1. Stance

Brontide Reference Stack is the first implementation of Brontide (Brontide §30). It is an application, not an operating
system: a hosted Brontide runtime plus a desktop showcase that makes the invisible — authority,
delegation, provenance — visible and manipulable. Its purpose is to produce evidence about the
Brontide model in both directions: where the model carries real weight, and where it creaks.

Two deliverables share one codebase:

- **Brontide Reference Stack runtime** — a faithful, reusable implementation of Brontide Base semantics as a .NET
  library. This is the part that must be *correct*.
- **Brontide Reference Stack Studio** — an Avalonia desktop application hosting showcase scenes on top of the
  runtime. This is the part that must be *persuasive*.

Non-goals for the foreseeable versions: cross-domain interaction, persistence, networking, real
device drivers, and revocation beyond mortality. Each of these is explicitly deferred by the
specification itself; Brontide Reference Stack should not run ahead of it. Brontide §6.8 cuts both ways — the spec
must not become Brontide Reference Stack-shaped, and Brontide Reference Stack must not pretend to be further along than the spec.

Brontide Reference Stack targets Architecture 0.4 as one complete baseline. Requirements stated normatively in 0.4
govern conformance. Work-in-progress material, especially Enrichment and value propagation
(§16.6), governs labelled experiments whose results feed the specification; passing such an
experiment is not represented as conformance to semantics that Architecture 0.4 has not ratified.

## 2. Solution shape

```
Brontide.Reference.sln
├── src/
│   ├── Brontide.Reference.Core                   — Brontide Base: domains, actor references, capabilities,
│   │                                   delegation, operations, executions, events, outcomes,
│   │                                   shapes, genesis, origin, provenance log
│   ├── Brontide.Reference.Extensions.Events      — Event Distribution (Brontide §19.2): mediator, subscriptions
│   ├── Brontide.Reference.Extensions.Flow        — Flow (Brontide §19.1): items, cursors, recovery contract
│   ├── Brontide.Reference.Experimental.Enrichment— targeted Enrichment experiments (Brontide §16.6)
│   ├── Brontide.Reference.Vocabularies.Cooling   — the Brontide §17 demo vocabulary (Temperature, Fan)
│   ├── Brontide.Reference.Vocabularies.Input     — Input.Pointer (the Brontide §21.1 template, once drafted)
│   └── Brontide.Reference.Studio                 — Avalonia app: scenes + inspectors
└── tests/
    ├── Brontide.Reference.Conformance            — the spec, executable (see §5 below)
    ├── Brontide.Reference.Enrichment.Tests       — explicitly non-conformance experiments for Brontide §16.6
    └── Brontide.Reference.Core.Tests             — implementation-level tests
```

The dependency rule mirrors the spec's layering: Core references nothing. Extensions,
Vocabularies, and Experimental.Enrichment reference Core only. Core never references the
experimental project. Studio references everything and is referenced by nothing. Studio's needs
must never leak into Core — Brontide Reference Stack must not define Brontide, and Studio must not define Brontide.Reference.

## 3. Mapping Brontide onto .NET

The load-bearing design decisions, each traceable to a spec section:

**Authority domain (§8).** A `Domain` instance owns all authority state and is the single gate
for every Execution. One process may host several domains — useful later for simulating
attachment, and eventually federation, without networking.

**Actor references (§9.1).** Opaque handles issued only by the domain. CLR object references are
unforgeable within a process, reference identity gives comparability, and the domain's issuance
discipline supplies unambiguity and stability. The four required properties fall out of the
platform nearly for free — the conformance suite states them anyway.

**Capability and Delegation (§10, §11).** An immutable record: holder, parent, added constraints.
The *only* public way to obtain a derived Capability is
`capability.Delegate(newHolder, params Constraint[] added)` — narrowing-by-construction becomes a
type-system fact. No API exists that could express amplification.

**Constraints (§10.1).** An abstract `Constraint` record plus evaluators registered per
constraint type. Evaluation when an Execution is presented is a conjunction over the derivation
chain; an unregistered constraint type is a dictionary miss, and a dictionary miss is a denial.
Fail-closed is the default control flow, not a guard clause someone must remember to write.

**Execution pipeline (§13.5, §13.6).** `domain.Execute(actor, operation, capability, input)`:
attribute → walk chain → evaluate constraints → dispatch effect → record. Every Execution, denial
included, appends to an in-memory provenance log; Studio is essentially a viewer over this log.
The invocation principle gets an explicit API: a deputy either forwards presented capabilities or
calls `AsOwnAuthority(reason)` — a deliberate act, recorded as such.

**Shape and values (§16).** Shape identity is a canonical name plus integer version — never
`System.Type`, reflection, or a CLR record name. Core carries a Shape registry: every Operation
registers one input Shape and one independent output Shape, and Event assertions, Outcome
results, failure `details`, and Constraint values declare theirs. The Capability recognition
closure (§10) is derived from the Operation registry, not duplicated per grant. Open-record
composition, authored Declared Fragments, and canonical projection follow §16.3–§16.4: unknown
optional fragments are ignored without claiming their semantics, and required Fragments cannot
be projected away.

**Experimental Enrichment (§16.6).** Enrichment is not part of `Brontide.Reference.Core`. The first experiment
resolves a pure, targeted Enrichment at a named Operation boundary from values already available
to an explicit composition. It supports copying, projection, and deterministic derivation;
missing sources and competing providers fail visibly. The resolver declares availability at the
boundary and does not require or invent a fixed call graph. Parameter threading, carrier
structures, contextual storage, attached fragments, and forwarding remain interchangeable
realisation strategies behind the same experiment.

Ambient Enrichment, Capability binding, and general value propagation are deferred until targeted
Enrichment produces evidence. A store read remains an ordinary authorised Operation; its returned
snapshot may then supply an Enrichment. The experiment cannot create, delegate, transfer, broaden,
or implicitly supply a Capability, and no Enrichment API appears on `Domain` or the Base execution
pipeline.

**Events and Outcomes (§14).** Immutable records; `Outcome` carries `TerminalFor`, an optional
`Result` conforming to the Operation's output Shape, and separately shaped `Details` for
rejection and failure diagnostics. Emission is an act by an Actor within the domain; distribution
is strictly the extension's business.

**Genesis (§12).** A domain is constructed with its primordial table; the constructor is the only
unguarded minting moment. Simulated attachment is a Genesis-occurrence API that mints capabilities
and records the occurrence — the §24 attachment model, literally.

**Origin (§15).** Origin-assertion grants are constraints on capabilities; unverified is the
default; `Origin.Derived` is the ceiling for anything delegated. The virtual-mouse scene exists
to demonstrate exactly this.

**Time (§10.3, §13.3).** .NET's `TimeProvider` maps directly onto time domains: tests inject a
fake clock, liveness leases tick against it, and a domain "without a clock" is simply constructed
without one — wall-clock-bounded capabilities then deny, fail-closed, which is itself testable.

**Later, not now.** Source generators compiling a fully static domain (the Embedded Test analog:
prove a Brontide Reference Stack domain can be all compile-time tables), and a NativeAOT headless "microdomain"
sample.

## 4. Milestones

Each milestone ends with something demonstrable plus a conformance increment. No dates until M1
calibrates the pace.

1. **M0 — Skeleton and executable spec.** Solution scaffold with dependency rules enforced by
   project references. Encode Brontide §29.2 (the delegation Given/When/Then and the Shape
   composition cases) and §29.4 (the worked attack) as failing tests first. The worked attack
   becomes the repository's first regression test before any feature exists.
2. **M1 — Base kernel green.** Capabilities, delegation-by-construction, the Operation registry
   with declared input and output Shapes, the Execution pipeline, fail-closed constraints,
   genesis, and mortality via liveness leases. M0's tests pass; the provenance log exists.
3. **M2 — Cooling scene, headless.** The Brontide §17 system end-to-end: sensor emits, controller
   executes, emergency handler stops the fan through a derived grant. Console output only —
   proves the API is livable before any UI exists.
4. **M3 — Shape composition.** The §16 contract green: additive same-name versions, open-record
   composition with authored Declared Fragments, canonical projection with unknown-fragment
   tolerance, and result/details separation on Outcomes. The §29.2 Velocity plus
   `Bob:DirectionalVelocity` cases pass. Mirrors Brontide Minimal Stack's M3, so the interchange experiment later
   meets a like-for-like Shape surface.
5. **M4 — Targeted Enrichment experiment.** Implement the Brontide §16.6 pointer-temperature case
   outside Core. Resolve `ThermalContext` at the named `pointer.move` boundary from already
   available telemetry without rewriting unrelated modules or assuming a global call graph. Test
   composition-local availability, missing sources, conflicting providers, pure derivation,
   explicit store acquisition, and rejection of Capability enrichment. Record which realisation
   strategy was used without making it part of the semantic contract.
6. **M5 — Studio v0.** Avalonia shell: actor graph, capability chains as trees, live Execution
   log. One non-negotiable feature: every denial explains itself — "denied: constraint
   `environment: staging` unrecognised by target; fail-closed (§10.1)".
7. **M6 — Virtual device board with origin.** The chosen showcase. A virtual mouse attaches
   (Genesis occurrence, on stage) and pointer events carry `Origin.Device`; a "malware" actor
   attempts synthetic input and is denied; a "remote desktop" actor holding granted injection
   authority succeeds but is visibly *not* device-origin. The spec's best demo, made interactive.
8. **M7 — Event Distribution.** Mediator actor, capability-gated publish and observe, fan-out
   preserving original emitters, replay marked `Origin.Derived`.
9. **M8 — Flow.** The pointer stream as a Flow: cursors, a deliberately dropped item, gap
   detection, replay. Feeds evidence back into the spec's open Flow questions (§33).
10. **M9 — Macro operation scene.** An `Audit.Start` mock: a delegation chain across
   "organisational" actors, an Outcome creating a long-lived activity, and its later terminal
   Outcome. Demonstrates the uniform-participation claim of §21.2.

## 5. The governing discipline: spec-as-tests

Every conformance test cites the section it enforces (`[SpecSection("11")]`). When Brontide Reference Stack and the
spec disagree, the disagreement itself is the deliverable: either the code is wrong, or we have
found a spec bug — and both feed the next Brontide revision's change log. Architecture 0.4's current
change log is §36. This is Brontide Reference Stack's actual purpose per Brontide §30: evidence, not just software.

## 6. Showcase design principles

Make authority visible; make denial articulate; make the attack a scene. Studio should include a
"what if" toggle in sandboxed scenes: remove the staging constraint, replay §29.4, and watch the
attack succeed — nothing communicates a security model like watching it fail when weakened.

## 7. Risks

- **Studio scope creep.** Inspectors are seductive; timebox Studio work to what each milestone's
  scene actually needs.
- **Core purity erosion.** Studio convenience APIs migrating into Core; the dependency rule and
  review discipline guard this seam.
- **Constraint ergonomics in C#.** The algebra must feel natural to write or vocabulary authors
  will fight it. Spike during M1 and iterate on the API before M6 bakes it in.
- **Experimental leakage.** Enrichment convenience entering Core would turn Brontide Reference Stack's experiment
  into an accidental Brontide requirement. The project boundary, separate test suite, and explicit
  non-conformance label guard this seam.
- **Conformance drift.** Tests quietly testing the implementation instead of the spec. The
  section-citation attribute plus a periodic audit against the normative text keeps them honest.

## 8. Execution sequence and milestone gates

Codex or another implementer begins by inspecting the repository and finding the earliest
incomplete milestone. Completion is established by source, tests, and demonstrable behaviour, not
by directory names or prior claims. If no Brontide Reference Stack implementation exists, use the sequence below.

### 8.1 Repository preparation

1. Read `docs/archive/foundation/Brontide-Architecture-0.4.md` completely, then this plan, before designing public types.
2. Inspect repository instructions, existing files, version-control status, and local changes.
   Preserve unrelated work and adapt to an existing scaffold rather than replacing it.
3. Create only the M0 projects initially:

   ```text
   src/Brontide.Reference.Core
   src/Brontide.Reference.Vocabularies.Cooling
   tests/Brontide.Reference.Conformance
   tests/Brontide.Reference.Core.Tests
   ```

4. Enforce the dependency direction through project references. `Brontide.Reference.Core` has no project-local
   dependency. Tests and the Cooling vocabulary may reference Core; Core may not reference them.
5. Record the exact .NET SDK and test framework selected. Avoid additional dependencies unless
   they remove substantial accidental work without defining Brontide semantics on Brontide Reference Stack's behalf.

**M0 exit gate:**

- the solution restores and builds;
- dependency boundaries are mechanically checkable;
- the §29.4 worked attack and initial §29.2 authority and Shape cases exist as executable tests;
- tests cite their Brontide section through a small `SpecSection` mechanism; and
- expected failures were observed before their implementations were added, so the tests are known
  to detect missing behaviour.

### 8.2 Base kernel implementation order

Implement M1 in small vertical increments rather than defining the entire object model up front:

1. Canonical names and opaque references for Actor, Operation, Capability, Shape, Fragment, and
   Execution identities.
2. Shape references and the minimum value representation needed by the first authority tests.
3. Actor issuance and explicit primordial declarations for a single Authority Domain.
4. Operation registration with independent input and output Shapes.
5. Capability issuance at Genesis, target recognition, and Delegation by adding Constraints only.
6. Fail-closed Constraint evaluation over the complete derivation chain.
7. The checked Execution path: attribute, recognise, validate Shape, evaluate authority, dispatch,
   record, and return Events or an Outcome.
8. Interaction composition and an append-only in-memory provenance log.
9. Mortality through an injected time domain, including a domain constructed without a clock.

The public API must make amplification difficult or impossible to express. Do not add a generic
Capability constructor, a mutable authority record, an API that replaces inherited Constraints, or
a handler service locator carrying ambient authority.

**M1 exit gate:**

- valid authority permits the named Operation and invalid authority denies it;
- a Capability for another Operation denies;
- unknown Constraint semantics deny;
- Delegation cannot remove or rewrite inherited authority;
- denial occurs before the requested effect and records an intelligible reason;
- accepted and denied Executions are attributable in the provenance log;
- the §29.4 attack is denied for the reason required by the specification; and
- all M1 conformance and Core tests pass in a clean solution build.

### 8.3 First vertical slice

For M2, implement the smallest Cooling vocabulary needed for `Temperature.Read`, `Fan.SetSpeed`,
`Fan.Stop`, and `Sensor.Temperature.Changed`. Run `SensorReader`, `CoolingController`, and
`EmergencyHandler` headlessly through the same public Execution path used by tests. Demonstrate one
accepted effect, one denied attempt, a derived grant, an Event, and a terminal Outcome. Do not add
Studio to make this demonstration.

**M2 exit gate:**

- the Cooling scenario runs from a non-interactive host or test;
- no effect bypasses Capability and Shape evaluation;
- the accepted and denied paths are visible in textual output or test evidence;
- Event receipt alone initiates no Execution; and
- the complete solution remains green.

### 8.4 Shape composition

For M3, expand Shape only far enough to satisfy Architecture 0.4 §16 and its conformance cases:
unit, scalar, record, sequence, choice, opaque values, additive same-name versions, open and closed
fragment policy, reusable and authored Declared Fragments, canonical projection, transparent
preservation, and independent input/output evolution. Do not use CLR assignability as Shape
compatibility or reflection as canonical Shape identity.

**M3 exit gate:**

- `Velocity 1 + Bob:DirectionalVelocity 1` projects canonically to `Velocity 1`;
- an unknown optional authored Fragment is tolerated without claimed understanding;
- a required Fragment cannot be projected away;
- closed Shapes reject authored attachment;
- transparent forwarding preserves unknown Fragments;
- Outcome `result` and failure `details` use their separately declared Shapes; and
- Brontide Reference Stack and the tests agree because of Brontide contracts, not shared CLR types.

### 8.5 Targeted Enrichment experiment

Only after M3, add `Brontide.Reference.Experimental.Enrichment` and `Brontide.Reference.Enrichment.Tests`. The experimental
layer wraps Core and constructs a complete Operation input before invoking the Core Execution gate;
`Brontide.Reference.Core` remains unaware of Enrichment. This ordering is itself an experimental hypothesis,
not a new Brontide rule.

Implement only targeted Enrichment at a named Operation boundary. Begin with the
pointer-temperature case and support explicit available sources, copying, projection, and pure
deterministic derivation. Missing sources and competing providers fail visibly. Do not implement
ambient Enrichment, Capability binding, hidden Capability invocation, a global call graph, or
general propagation.

**M4 exit gate:**

- one composition supplies `ThermalContext` at `pointer.move` and another does not;
- the consumer declares the Fragment it requires;
- missing and conflicting providers fail deterministically;
- the same availability semantics work without an enumerated physical route;
- an explicit store-read result may supply Enrichment, while Enrichment itself performs no read;
- no Capability can be created, delegated, transferred, broadened, or supplied by Enrichment;
- all Enrichment code and tests remain outside Core and normative conformance; and
- findings record whether pre-Execution Enrichment failure needs future attributable semantics.

Studio work begins only after M4. M5 and later milestones then follow §4, one demonstrable scene and
one conformance or experimental evidence increment at a time.

## 9. Codex delegation protocol

This section is part of the plan so a new Codex task can continue the work without relying on the
conversation that produced it.

### 9.1 Sources of truth

Use the following precedence:

1. `docs/archive/foundation/Brontide-Architecture-0.4.md` for Brontide semantics;
2. this plan for Brontide Reference Stack sequencing and implementation boundaries;
3. section-cited conformance tests as executable interpretations of the Architecture; and
4. existing Brontide Reference Stack code as an implementation that may be corrected.

Tests and code do not silently override the Architecture. If normative text, a test, and a proposed
implementation cannot be reconciled, reduce the disagreement to the smallest example and report it
as an Brontide ambiguity, plan gap, test defect, or Brontide Reference Stack defect.

Architecture §16.6 is deliberately work in progress. Its cases belong to the experimental suite,
not normative conformance. Experimental success is evidence for Brontide; it is not permission to
move Enrichment into Base or Core.

### 9.2 Working rules for Codex

When delegated Brontide Reference Stack synthesis, Codex should:

- inspect repository instructions and the existing implementation before editing;
- determine the earliest incomplete milestone from the exit gates above;
- implement the smallest coherent increment that advances that milestone;
- write or strengthen the relevant section-cited test before or with behaviour changes;
- preserve user changes and avoid unrelated rewrites;
- keep public contracts minimal until a scenario or conformance rule requires them;
- use one checked public Execution path for tests, samples, and later Studio actions;
- keep Brontide Reference Stack-specific convenience and all §16.6 experiments outside `Brontide.Reference.Core`;
- build and test after each coherent increment and run the full available suite before handoff;
- continue through safe, in-scope implementation work until the requested milestone gate is met;
  and
- record important findings in `docs/implementation-findings.md`, creating it when the first
  finding exists.

Each finding should contain the Brontide section, minimal scenario, observed result, expected result,
classification, and current disposition. Valid classifications are `Brontide Reference Stack defect`, `test defect`,
`plan gap`, `Brontide ambiguity`, and `experimental result`.

Codex should not:

- edit Brontide Architecture to make an implementation pass unless the user separately requests a
  specification change;
- begin Brontide Minimal Stack, Studio, networking, persistence, distributed authority, or a later milestone merely
  because its scaffold is convenient;
- infer authority from process location, dependency injection, ambient context, or Shape presence;
- use a shared CLR model as though it were the Brontide contract;
- conceal a failing rule behind an adapter or permissive fallback;
- commit, push, publish packages, or change remote state unless the user explicitly requests it;
  or
- replace an architectural decision with a framework default without recording the consequence.

Codex may make reversible internal design choices when the Architecture and this plan leave room.
It should pause for the user when a choice changes Brontide-visible semantics, crosses the Base versus
experimental boundary, requires a materially broader scope, or would make future Reference/Minimal
interchange depend on a Brontide Reference Stack-private convention.

### 9.3 Verification and handoff

At every handoff, Codex reports:

- the milestone and exit-gate items completed;
- the observable behaviour added;
- the principal files changed;
- build and test results, including any tests not run and why;
- new implementation findings or Architecture questions; and
- the next incomplete gate.

A milestone is not complete merely because its types exist. Its exit gate must pass through public
behaviour and evidence. A failed build, unexplained skipped test, hidden authority path, or
experimental dependency from Core keeps the milestone incomplete.

### 9.4 Ready-to-use delegation instruction

The following instruction may be given to Codex from the Brontide Reference Stack repository:

```text
Synthesize Brontide Reference Stack according to `docs/archive/foundation/Brontide-Architecture-0.4.md` and
`docs/archive/foundation/Brontide-Reference-Stack-Implementation-Plan-0.2.md`.

Read both documents completely, inspect all repository instructions and existing work, and
determine the earliest incomplete milestone from the plan's exit gates. Continue from that point;
do not recreate completed work. Implement the smallest coherent increments needed to complete the
current milestone, using section-cited tests as executable evidence. Build and test after each
increment and run the full available suite before handoff.

Preserve Brontide-visible semantics over framework convenience. Keep one checked Execution path,
make authority fail closed, preserve unrelated user changes, and record material discrepancies in
docs/implementation-findings.md. Do not edit the Brontide Architecture, start Brontide Minimal Stack, or advance to
Studio or later milestones before the current gate is complete.

Treat Brontide §16.6 as experimental: keep Enrichment outside Brontide.Reference.Core and normative conformance;
do not implement ambient Enrichment, Capability enrichment, hidden acquisition, a required global
topology, or general propagation unless the plan is explicitly revised. Do not commit, push, or
publish unless separately asked.

Work autonomously through safe in-scope decisions. Stop and request direction only when an
unresolved choice would change Brontide-visible semantics, cross the Base/experimental boundary,
depend on a Brontide Reference Stack-private convention for interchange, require broader authority, or contradict
the governing documents. At handoff, report the completed exit-gate evidence, changed files,
build/test results, findings, and the next incomplete gate.
```

## 10. Changes from Implementation Plan 0.1

Implementation Plan 0.2 preserves the Architecture 0.4 target, project boundaries, milestones,
and showcase direction introduced in 0.1, and adds an execution-ready path for delegated work.

- M0–M4 now have explicit implementation sequences and observable exit gates.
- The first headless Cooling slice and the targeted-Enrichment experiment have concrete acceptance
  cases.
- Normative conformance and §16.6 experimental evidence are separated operationally.
- Sources of truth, discrepancy classification, verification, and handoff requirements are
  explicit.
- A reusable Codex delegation instruction is included so implementation can resume from repository
  evidence without requiring the originating conversation.
