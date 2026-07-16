# LINEN

## Implementation Plan 0.1

**Status:** Working plan
**Companion to:** Atlas Architecture 0.4
**Stack:** F# / .NET 10 (LTS)
**First proof:** two-way component interchange with Fabric

---

## 1. Stance

Linen is the second, deliberately independent full-stack implementation of Atlas. Its purpose is
not to confirm that Fabric can be reimplemented from its own design. Its purpose is to discover
whether Atlas is sufficient for two different implementations to compose.

Fabric is a practical showcase: an object-oriented runtime, rich inspection, and an interactive
desktop environment. Linen should take a different route. It will be a lean, data-oriented,
functional implementation with an immutable kernel and a headless host. It will implement Atlas
from the specification, not by wrapping Fabric or translating Fabric's internal object model.

Linen remains on .NET for practical reasons: one SDK, mature tooling, straightforward deployment,
and a low-friction path to running both implementations in the same development environment. F#
is the deliberate implementation-language choice. It keeps the shared platform while encouraging
a structurally different design based on algebraic data, explicit state transitions, and
composition rather than reproducing Fabric's C# API in another namespace.

“Full stack” means an independent Base implementation, selected extensions and vocabularies, a
host, bindings, and conformance tests. It does not require a second desktop UI. Linen's headless
host and text inspector are features of the experiment: interoperability must not depend on
Fabric Studio.

The first proof is not that Linen passes the same tests as Fabric. It is that a Linen component can
run in a Fabric environment and a Fabric component can run in a Linen environment, within
the contracts both sides declare, without sharing private runtime types.

## 2. Independence contract

Independence is a design constraint, not an aspiration. The initial Linen repository follows
these rules:

- Linen Core does not reference a Fabric assembly, source project, package, generated model, or
  runtime service.
- Fabric and Linen do not share an “Atlas CLR model” containing Actor, Capability, Shape,
  Operation, Execution, Event, Outcome, or Constraint types. Such a package would make the shared
  implementation the real specification.
- Linen is designed from Atlas Architecture 0.4. The Fabric plan may supply test scenarios and
  known risks, but not Linen's object model or call pipeline.
- Linen does not copy Fabric's central mutable `Domain`, Capability object graph, evaluator
  registry, provenance log, or Studio-driven APIs.
- Canonical Atlas names, Shape identities, Declared Fragment identities, and conformance fixtures
  may be shared as specification data. Executable semantics remain independently implemented.
- Interchange occurs through an explicit binding boundary. Any translation is attributable,
  version-aware, and testable; an adapter may not silently repair an incompatible contract.
- Linen-specific experiments use the `Linen:` namespace and do not acquire Atlas standing merely
  because Linen implements them.

The common .NET runtime is substrate, not architecture. A test that succeeds only because both
sides exchange the same CLR object, `System.Type`, exception, dependency-injection container, or
ambient service is not evidence of Atlas interoperability.

## 3. Solution shape

```text
Linen.sln
├── src/
│   ├── Linen.Model                   — independent Atlas names, Shapes, values, and occurrences
│   ├── Linen.Kernel                  — pure authority evaluation and immutable state transitions
│   ├── Linen.Extensions.Events       — Event Distribution
│   ├── Linen.Extensions.Flow         — Flow and recovery experiments
│   ├── Linen.Experimental.Enrichment — independent targeted Enrichment experiments
│   ├── Linen.Vocabularies.Cooling    — first shared semantic test vocabulary
│   ├── Linen.Binding                 — explicit external component boundary and Shape projection
│   └── Linen.Host                    — headless host, scenarios, and text inspection
└── tests/
    ├── Linen.Conformance             — Atlas requirements as executable behaviour
    ├── Linen.Enrichment.Tests        — explicitly non-conformance experiments for Atlas §16.6
    ├── Linen.Kernel.Tests            — implementation properties and model-based tests
    └── Linen.Interchange.Tests       — Fabric ↔ Linen component substitution
```

The dependency direction is strict:

```text
Model ← Kernel ← Extensions / Vocabularies / Experimental.Enrichment ← Host
  ↑          ↑                    ↑
  └──────── Binding ──────────────┘
```

`Linen.Model` contains data contracts but no authority evaluator, dispatch machinery, host
services, or Fabric compatibility code. `Linen.Kernel` depends only on Model. Extensions and
Vocabularies depend on Model and Kernel but not Host. Experimental.Enrichment follows the same
dependency direction, while Kernel never references it. Binding translates at the system edge;
it does not become an alternative Core model. Host is the composition root and is referenced by
nothing.

## 4. Mapping Atlas onto a different .NET architecture

The intended public architecture is a state machine, not a graph of live service objects.
Conceptually:

```text
step : Environment -> World -> Execution -> Decision * World * Occurrence list
```

The exact F# API will be refined during M1, but the properties are load-bearing.

**World and authority domain (§8).** `World` is an immutable snapshot containing domain-local
tables for Actors, Capabilities, Operations, Shapes, Declared Fragments, and active relationships.
The kernel returns a new World rather than mutating a domain object. A small host-owned reference
to the current snapshot is an implementation concern outside the pure evaluator.

**Actor references (§9.1).** Linen uses opaque domain-local identifiers validated against World,
not CLR object identity. Construction is private to the kernel. Unambiguity, comparability,
stability, and unforgeability are tested as properties of issuance and lookup.

**Operation and Execution (§13).** An Operation is registry data: canonical name, input Shape,
independent output Shape, required Declared Fragments, and target semantics. An Execution is an
immutable occurrence containing one Operation reference, initiator, target, presented Capability,
and input value. “Execute an Operation” remains natural API prose; the value representing a
specific attempt is always an Execution.

**Capability recognition (§10).** Capabilities are immutable grant rows identified by opaque
references. The target recognises an authorised Operation together with the Operation's input and
output Shapes and required Declared Fragments. This closure is derived from the Operation registry,
not copied into every Capability. Unknown Operations, Shapes, Fragments, or Constraints deny. Extra
optional Shape fragments cannot expand the authorised Operation.

**Delegation (§11).** Delegation appends a child grant whose parent is an existing Capability and
whose only semantic additions are Constraints. There is no API that rewrites inherited authority.
The derivation graph is data in World, not object ownership or nested Capability instances.

**Constraints (§10.1).** Constraint evaluation is a pure function over the Execution, the complete
derivation chain, World, and explicit Environment inputs. Evaluators are supplied as a closed map
for one kernel step. Missing semantics produce `Denied UnknownConstraint`; they never fall through
to success. Stateful rate or liveness accounting is represented by the returned World.

**Shape and values (§16).** Shape identity never uses `System.Type`, reflection, or a CLR record
name. Linen defines an independent Shape algebra for unit, scalar, record, sequence, choice, and
opaque contracts, plus fragment policy and Declared Fragment references. Runtime values use a
separate data representation. A Shape is never confused with the value conforming to it.

Open Shape matching computes the canonical projection explicitly. Unknown optional authored
Fragments may be preserved without being interpreted; required Fragments must be recognised and
validated. Reusable Fragments such as `Interaction 1` are included explicitly by each host Shape
and do not make unrelated host Shapes compatible.

**Experimental Enrichment (§16.6).** Linen implements the first targeted-Enrichment experiment as
an independent pure composition function outside Model and Kernel. Given an explicit composition
scope, available shaped values, and a named Operation boundary, it either produces the required
absent Fragment or returns a visible missing-source, conflict, or derivation error. It must not
copy Fabric's resolver API or assume Fabric's call pipeline.

The experiment describes availability, not a global topology. Its state-machine host may realise
availability as an explicit Environment input or another immutable carrier, while preserving the
same external semantics as implementations using parameter threading, carrier structures,
contextual storage, attached fragments, or forwarding. Store access remains an authorised
Operation whose returned value may later feed Enrichment. Capabilities remain solely within the
authority model; Enrichment cannot create, delegate, transfer, bind, broaden, or implicitly supply
one. Ambient Enrichment and general propagation remain later experiments.

**Interaction composition (§13.2).** Interaction is represented as reusable data included by
Execution, Event, Outcome, and any Linen extension occurrence that opts in. It is not a union case
standing above every occurrence and does not carry semantic name, target, Capability, input,
assertion, result, or status. This keeps structural reuse separate from occurrence ontology.

**Event and Outcome (§14).** A successful kernel step returns emitted Events and Outcomes as data;
Event Distribution later decides delivery. A successful Outcome may carry a `result` value that
conforms to the Operation's output Shape. Rejection and failure carry separately shaped `details`.
Receiving an Event never schedules a reactive Execution unless an Actor deliberately presents a
Capability for it.

**Genesis (§12).** Initial World is built from explicit primordial declarations and records an
enumerable set of Genesis occurrences. There is no privileged general-purpose mint method on a long-lived
domain object. Later resource creation must pass through an authorised Operation or an explicitly
specified Genesis boundary.

**Time (§10.3, §13.3).** Time is an Environment input. Tests supply counters and named time-domain
values directly; Core does not depend on `DateTime`, a global clock, or .NET `TimeProvider`.
`emitted-at` remains an attributable Temporal Mark and is never used as the authority clock.

**No ambient dispatch.** The kernel decides authorisation and records the decision. The host maps
an accepted Operation to an effect handler explicitly and returns the resulting Outcome or Events
through another checked transition. A handler receives the minimum accepted input and context; it
does not receive World or a service locator from which it can obtain ambient authority. Any
follow-up work is returned as an explicit requested Execution and must carry a Capability made
available to that handler deliberately.

## 5. The interchange experiment

Interchange must test Atlas contracts without accidentally standardising Linen's or Fabric's
private API. The first binding is therefore deliberately narrow and explicitly non-normative.

Each exported component declares:

- the Profile, Extension, and Domain Vocabulary versions it requires;
- the Operations it provides or consumes;
- input and output Shape identities and versions;
- required Declared Fragments and open-Shape fragment policy;
- the Operations for which the host must be able to supply recognised Capabilities; and
- any binding limitation that narrows its substitutability claim.

The proof harness performs contract negotiation before activation. It rejects incompatible Shape
lineages, missing required Fragments, unsupported Operations, and authority requirements that the
host cannot represent. It does not infer compatibility from CLR assignability.

The first proof runs across a process boundary using a small test binding. This prevents shared
object identity, static state, dependency injection, and exception behaviour from smuggling in a
common implementation. The binding encoding is a test instrument, not an Atlas wire standard; its
only claim is that it preserves the declared semantic identities and values needed by the test.
An in-process binding may follow, but it must pass the same behavioural suite.

Process isolation is not treated as cross-domain federation. For this experiment, the hosting
stack owns one authority domain, evaluates the Execution, and invokes the foreign provider only
after acceptance. The binding maps host-owned references without granting the provider a second
authority namespace. That mapping is trusted host machinery and part of the test's declared trusted
computing base. Cross-domain Capability representation and attestation remain deferred with the
Atlas `Identity` and `Distributed` extensions.

Two directions are mandatory:

1. Fabric hosts a Linen Cooling component and drives its declared Operations with Fabric-created
   Capabilities and Shape values.
2. Linen hosts a Fabric Cooling component and drives the same contracts with Linen-created
   Capabilities and Shape values.

Both directions test success, authority rejection, authored-fragment projection, a missing
required Fragment, an unknown Constraint, an Outcome result, and provenance preservation. Adapter
code and any information loss are measured as part of the result. A large adapter is evidence
that the shared contract is incomplete, not a success to hide.

## 6. Milestones

Each milestone ends with an observable result and an Atlas conformance increment. No dates until
M1 establishes the implementation pace.

1. **M0 — Independent skeleton and executable contracts.** Create the solution with no Fabric
   references. Encode the Atlas §29 delegation example, worked attack, Operation/Execution
   distinction, and open-Shape fragment example as failing behavioural tests.
2. **M1 — Pure Base kernel.** Implement Actor issuance, Operation and Shape registries,
   target-recognised Capabilities, Delegation by added Constraints, pure authorisation, Genesis,
   and explicit time. Property tests prove attenuation, fail-closed evaluation, and immutable
   state transitions.
3. **M2 — Cooling, headless and native to Linen.** Run the Cooling scenario entirely through the
   F# API: sensor Event, controller Execution, derived emergency Capability, and terminal Outcome.
   The text inspector prints World transitions and denials without any Fabric component.
4. **M3 — Shape composition.** Implement additive versions, canonical projection, authored
   Fragments, reusable Fragment inclusion, transparent preservation, and result/details
   separation. Run the Velocity plus `Bob:DirectionalVelocity` conformance cases.
5. **M4 — Independent targeted Enrichment.** Implement the Atlas §16.6 pointer-temperature case
   as a pure composition boundary outside Model and Kernel. Demonstrate composition-local
   availability without a fixed call graph, visible missing-source and provider-conflict errors,
   explicit store acquisition, pure derivation, and rejection of Capability enrichment. Design
   from Architecture 0.4, then compare behaviours—not APIs—with Fabric's experiment.
6. **M5 — External binding and contract negotiation.** Define the non-normative test binding,
   component manifest, and process-isolated harness. Prove that private CLR types never cross the
   boundary.
7. **M6 — First two-way interchange.** Complete Fabric-hosts-Linen and Linen-hosts-Fabric Cooling
   tests, including denials, Shape fragment asymmetry, and one targeted-Enrichment case resolved by
   each host independently. Record every adapter obligation as feedback for Atlas, Fabric, or the
   binding.
8. **M7 — Event Distribution and Flow.** Implement independent mediator and Flow state machines.
   Exchange an Event stream in both directions, preserve the original emitter, detect a gap, and
   recover without treating delivery as authority.
9. **M8 — Macro Operation.** Implement a headless `Audit.Start` scenario whose successful Outcome
   returns an activity reference and whose activity later terminates. Exchange one provider across
   stacks to test that the model remains scale-agnostic.

## 7. Governing discipline

Linen is specification-driven, but it must not merely duplicate Fabric's tests line for line.
Every normative behavioural test cites its Atlas section. For each major Base rule, Linen should
prefer a property or state-machine test in addition to examples:

- Delegation never increases effective authority.
- Unknown Constraint semantics always deny.
- A denied Execution produces no requested domain effect.
- Operation identity is stable across many Executions.
- Input and output Shape evolution are independent.
- Canonical projection ignores unsupported optional Fragments without claiming their semantics.
- Required Fragments cannot be projected away.
- Interaction composition adds only its declared structure.
- Event receipt grants no reactive Capability.
- Replaying an Execution may repeat an effect; replaying an Event repeats only the assertion.

The §16.6 Enrichment cases are kept in a separate experimental suite. They cite the work-in-progress
section for traceability but are not counted as normative conformance. Fabric and Linen compare
observable availability, failure, and authority boundaries without sharing resolver code or
requiring the same realisation strategy.

When Fabric and Linen disagree, neither implementation is presumed correct. Reduce the difference
to the Atlas text and record one of four findings: Fabric defect, Linen defect, underspecified
binding, or Atlas ambiguity. That classification is Linen's main research output.

## 8. Risks

- **Accidental convergence.** Developers copy Fabric APIs because they are available. Repository
  and review rules prohibit Fabric references in Linen Core; architecture tests enforce the
  project boundary.
- **Shared-runtime false confidence.** CLR assignability or shared process state makes a test pass.
  The first interchange proof is process-isolated and rejects private type metadata.
- **Adapter laundering.** A clever adapter invents missing semantics or authority. Negotiation
  must fail visibly, and adapter transformations are attributable and counted.
- **Functional ceremony.** An immutable model can become difficult to host if every transition
  copies or exposes too much state. Persistent collections and a narrow host loop may optimise the
  representation without changing the pure semantic boundary.
- **F# becoming the experiment.** Language novelty must not consume the project. Use ordinary F#,
  a small dependency surface, and direct .NET hosting; avoid metaprogramming unless a conformance
  problem requires it.
- **Lean becoming partial.** Linen may omit Studio, but not Base semantics needed for interchange.
  “Headless” is a presentation choice, not permission to delegate missing behaviour to Fabric.
- **Specification movement.** Atlas 0.x will change. Each implementation records the exact Atlas
  version it targets; migration changes tests and model explicitly rather than adding compatibility
  aliases that obscure the result.
- **Experimental convergence.** Copying Fabric's Enrichment API would make agreement meaningless.
  Linen derives its composition function independently from Architecture 0.4 and compares only
  observable cases after both designs exist.

## 9. Immediate next actions

1. Initialise `Linen.sln` with the dependency boundaries above and an automated assertion that no
   Core project references Fabric.
2. Encode four failing Atlas 0.4 tests: Operation versus Execution, the worked authority attack,
   additive Shape projection, and required versus optional Declared Fragments.
3. Spike the immutable `World` and pure `step` boundary with one accepted and one denied
   `Fan.Stop` Execution.
4. Define the independent Shape and value algebras without reflection or shared CLR DTOs.
5. Write the minimal component manifest for Cooling, then compare it with what Fabric can expose;
   treat every missing field as a design finding before implementing the binding.
6. Write the M4 targeted-Enrichment examples from §16.6 independently, but keep them outside the
   conformance suite until the semantics are ratified.
