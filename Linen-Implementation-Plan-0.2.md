# LINEN

## Implementation Plan 0.2

**Status:** Working plan
**Companion to:** Atlas Architecture 0.5
**Stack:** F# / .NET 10 (LTS)
**First proof:** two-way Cooling component interchange with Fabric
**Decisive proof:** a mixed Fabric/Linen image workspace with visible composition boundaries

---

## 1. Stance

Linen is the second, deliberately independent full-stack implementation of Atlas. Its purpose is
not to confirm that Fabric can be reimplemented from its own design. Its purpose is to discover
whether Atlas is sufficient for two different implementations to compose.

Fabric is a practical showcase: an object-oriented runtime, rich inspection, and an interactive
desktop environment. Linen should take a different route. It will be a lean, data-oriented,
functional implementation with an immutable kernel and a headless host. It will implement Atlas
from the specification, not by wrapping Fabric or translating Fabric's internal object model.

Linen remains on .NET for practical reasons: mature tooling, straightforward deployment, and a
low-friction path to running both implementations in the same development environment. **F# is the
implementation language for every Linen production project, host, test project, and Linen-owned
binding tool.** It keeps the shared platform while encouraging a structurally different design
based on algebraic data, explicit state transitions, and composition rather than reproducing
Fabric's C# API in another namespace.

Linen targets .NET 10 but does not add a repository `global.json` or pin an SDK version or feature
band. The repository states the target framework and language expectations in project and build
configuration while allowing normal installed-SDK resolution. A Fabric-owned endpoint may remain
C#, but no C# implementation belongs inside the Linen stack.

“Full stack” means an independent Base implementation, selected extensions and vocabularies, a
host, bindings, conformance tests, and isolated experiments for work-in-progress Atlas directions.
It does not require a second desktop UI. Linen's headless host and text inspector are features of
the experiment: interoperability must not depend on Fabric Studio.

The first proof is not that Linen passes the same tests as Fabric. It is that a Linen Cooling
component can run in a Fabric environment and a Fabric Cooling component can run in a Linen
environment, within the contracts both sides declare, without sharing private runtime types.

That narrow proof is followed by the Architecture 0.5 decisive demonstration: a collaborative
image workspace in which small transformation modules remain simple, system facilities are adopted
incrementally, providers are substituted visibly, and Fabric and Linen Components coexist. GPU
execution is not a Linen milestone; it remains a separate experimental sideline project and may
not be simulated by the CPU or vector paths.

## 2. Independence contract

Independence is a design constraint, not an aspiration. The initial Linen repository follows
these rules:

- Linen Core does not reference a Fabric assembly, source project, package, generated model, or
  runtime service.
- Fabric and Linen do not share an “Atlas CLR model” containing Actor, Capability, Shape,
  Operation, Execution, Event, Outcome, or Constraint types. Such a package would make the shared
  implementation the real specification.
- Linen is designed from Atlas Architecture 0.5. The Fabric plan may supply test scenarios and
  known risks, but not Linen's object model or call pipeline.
- Linen does not copy Fabric's central mutable `Domain`, Capability object graph, evaluator
  registry, provenance log, or Studio-driven APIs.
- Canonical Atlas names, Shape identities, Declared Fragment identities, and conformance fixtures
  may be shared as specification data. Executable semantics remain independently implemented.
- Interchange occurs through an explicit binding boundary. Any translation is attributable,
  version-aware, and testable; an adapter may not silently repair an incompatible contract.
- Linen does not reference or reproduce the CLR types from `Fabric.Experimental.Composition`.
  Dependency strengths, operational observations, boxed applications, and optimisation claims are
  re-derived from Atlas 0.5 and compared through external fixtures and behaviour.
- Linen-specific experiments use the `Linen:` namespace and do not acquire Atlas standing merely
  because Linen implements them.

The common .NET runtime is substrate, not architecture. A test that succeeds only because both
sides exchange the same CLR object, `System.Type`, exception, dependency-injection container, or
ambient service is not evidence of Atlas interoperability.

## 3. Solution shape

```text
Linen.slnx
├── src/
│   ├── Linen.Model                   — independent Atlas names, Shapes, values, and occurrences
│   ├── Linen.Kernel                  — pure authority evaluation and immutable state transitions
│   ├── Linen.Extensions.Events       — Event Distribution
│   ├── Linen.Extensions.Flow         — Flow and recovery experiments
│   ├── Linen.Experimental.Enrichment — independent targeted Enrichment experiments
│   ├── Linen.Experimental.Composition — Architecture 0.5 composition and explanation experiments
│   ├── Linen.Vocabularies.Cooling    — first shared semantic test vocabulary
│   ├── Linen.Vocabularies.Imaging    — image-workspace Shapes and semantic Operations
│   ├── Linen.Binding                 — explicit external component boundary and Shape projection
│   └── Linen.Host                    — headless host, scenarios, and text inspection
└── tests/
    ├── Linen.Conformance             — Atlas requirements as executable behaviour
    ├── Linen.Enrichment.Tests        — explicitly non-conformance experiments for Atlas §16.6
    ├── Linen.Composition.Tests       — explicitly non-conformance Atlas 0.5 composition evidence
    ├── Linen.Kernel.Tests            — implementation properties and model-based tests
    └── Linen.Interchange.Tests       — Fabric ↔ Linen component substitution
```

The dependency direction is strict:

```text
Model ← Kernel ← Extensions / Vocabularies / Experiments ← Host
  ↑          ↑                    ↑
  └──────── Binding ──────────────┘
```

`Linen.Model` contains data contracts but no authority evaluator, dispatch machinery, host
services, or Fabric compatibility code. `Linen.Kernel` depends only on Model. Extensions and
Vocabularies depend on Model and Kernel but not Host. Experimental.Enrichment and
Experimental.Composition follow the same dependency direction, while Model and Kernel never
reference either experiment. Binding translates at the system edge; it does not become an
alternative Core model. Host is the composition root and is referenced by nothing.

The dependency check also enforces language and SDK policy: Linen-owned projects are F# projects,
no Linen project references a Fabric assembly, and no `global.json` SDK pin is introduced.

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

**Experimental Composition (§6.12–§6.14, §18.1–§18.2, §32.2).** Linen independently models the
Architecture 0.5 composition guardrails outside Model and Kernel. The experiment may describe
Components, facilities, provider selection, replacement, boxed applications, and execution
observations, but those types are not Atlas Base and do not enter normative conformance.

A simple module declares its Operation, input and output Shapes, required Fragments, and authority
boundary without depending on identity, persistence, scheduling, networking, system metadata, or
acceleration. The surrounding experimental composition may add facilities under explicit
dependencies. It preserves at least four distinct dependency meanings:

- a required generic contract;
- a required stronger Profile;
- a preference for a system-provided implementation; and
- a requirement for a specifically authored provider.

An opaque boxed application may expose a narrow Atlas boundary or no Atlas Operation at all. Linen
must not reject it, invent visibility into its private interior, or treat private mechanisms as
defective merely because system tooling cannot inspect them.

Purity, determinism, replay safety, batchability, vectorisability, relocatability, and accelerator
compatibility are attributable implementation claims, never consequences of Operation identity or
placement. The selector refuses optimisation when required claims are absent. Linen's main plan
requires CPU execution and may include an honest vector experiment; GPU execution remains a
separate sideline and is neither required nor simulated.

Each experimental execution produces one structured explanation linking the submitted Execution,
applied Enrichments, selected provider, representation, crossed process or machine boundaries,
authority decision, batching, copies, retries, fallback, emitted occurrences, Outcome, timing, and
causality where available. The representation is a Linen experiment, not a ratified Atlas trace or
Binding Plan.

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

- its authored component and provider identities;
- the Profile, Extension, and Domain Vocabulary versions it requires;
- the Operations it provides or consumes;
- input and output Shape identities and versions;
- required Declared Fragments and open-Shape fragment policy;
- the Operations for which the host must be able to supply recognised Capabilities; and
- each dependency's strength: generic requirement, stronger Profile, system-provider preference,
  or authored-provider requirement;
- attributable selection characteristics relevant to the test, including placement and failure
  boundaries, without treating a provider's self-description as proof;
- any explicit optimisation claims used during selection; and
- any binding limitation that narrows its substitutability claim.

The manifest is specification data exchanged at the process boundary, not a shared CLR assembly.
Fabric and Linen parse it into their own private models. The initial encoding is a versioned test
fixture and must not be presented as the Atlas Portable Binding or silently grow implementation
semantics that Atlas has not specified.

Shared demonstration contracts use explicit authorship. A neutral test authority may author the
image-workspace Operation and Shapes, or Linen may deliberately implement a documented
Fabric-authored contract; either choice is recorded in the fixture. Linen never republishes a
Fabric-authored name as `Linen:`, and no non-ratified image name is left unqualified as though Atlas
had standardised it.

The proof harness performs contract negotiation before activation. It rejects incompatible Shape
lineages, missing required Fragments, unsupported Operations, and authority requirements that the
host cannot represent. It also rejects unsatisfied required dependencies and provider-specific
requirements while allowing an unmet preference to remain visibly optional. It does not infer
compatibility from CLR assignability, provider name, placement, or structural coincidence.

The first proof runs across a process boundary using a small test binding. This prevents shared
object identity, static state, dependency injection, and exception behaviour from smuggling in a
common implementation. The binding encoding is a test instrument, not an Atlas wire standard; its
only claim is that it preserves the declared semantic identities and values needed by the test.
An in-process binding may follow, but it must pass the same behavioural suite.

Every bound Execution records the selected provider, selection reason, representation, copies,
crossed boundaries, retries, failure domain, fallback, and terminal Outcome. Unknown or opaque
facts remain explicitly unknown; they are not filled with optimistic defaults. A search through
unrelated Fabric and Linen logs is supporting evidence, not the structured explanation itself.

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

The Cooling exchange is the first proof because it keeps the seam small enough to diagnose. The
later decisive proof uses an image workspace: a small local CPU composition first, independently
adopted execution history, searchable metadata, and workspace state second, visible provider
substitution third, and a mixed Fabric/Linen workflow last. At least one provider crosses a
process boundary; a later machine boundary may use the same host-owned authority-domain model and
must not be misrepresented as cross-domain Capability federation.

The Fabric side of interchange requires an explicit experimental endpoint because Fabric does not
yet expose a ratified binding package. That endpoint is owned by Fabric or the external harness,
not referenced by Linen Model or Kernel, and must translate through process data rather than share
`Fabric.Experimental.Composition` objects. Every obligation discovered there is recorded as a
Fabric gap, Linen gap, binding gap, or Atlas ambiguity.

## 6. Milestones

Each milestone ends with an observable result and an Atlas conformance increment. No dates until
M1 establishes the implementation pace.

1. **M0 — Independent F# skeleton and executable contracts.** Create the solution entirely from
   F# projects, with no Fabric references and no SDK pin. Add automated project-boundary,
   language, and dependency checks. Encode the Atlas §29 delegation example, worked attack,
   Operation/Execution distinction, and open-Shape fragment example as failing behavioural tests.
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
   from Architecture 0.5, then compare behaviours—not APIs—with Fabric's experiment.
6. **M5 — External binding and contract negotiation.** Define the non-normative test binding,
   versioned component manifest, Fabric-owned test endpoint, and process-isolated harness. Prove
   that private CLR types never cross the boundary, dependency strengths remain distinct, required
   contracts fail visibly, and optional preferences remain optional.
7. **M6 — First two-way interchange.** Complete Fabric-hosts-Linen and Linen-hosts-Fabric Cooling
   tests, including denials, Shape fragment asymmetry, and one targeted-Enrichment case resolved by
   each host independently. Record provider selection, representation, crossed boundaries, copies,
   failure domain, Outcome, and every adapter obligation as feedback for Atlas, Fabric, Linen, or
   the binding.
8. **M7 — Event Distribution and Flow.** Implement independent mediator and Flow state machines.
   Exchange an Event stream in both directions, preserve the original emitter, detect a gap, and
   recover without treating delivery as authority.
9. **M8 — Macro Operation.** Implement a headless `Audit.Start` scenario whose successful Outcome
   returns an activity reference and whose activity later terminates. Exchange one provider across
   stacks to test that the model remains scale-agnostic.
10. **M9 — Independent Architecture 0.5 composition experiment.** Implement
    `Linen.Experimental.Composition` and its separate test suite. Demonstrate a simple image
    transformation with no system-service dependencies, the four dependency strengths, independent
    facility adoption, a valid opaque boxed application, visible provider replacement, explicit
    optimisation eligibility, and structured execution explanations. CPU execution is required;
    vector work is optional; GPU execution is excluded as a sideline project.
11. **M10 — Decisive mixed-stack image workspace.** Compose Fabric, Linen, and one independently
    authored provider in a coherent image workflow. Substitute at least one provider in each host,
    preserve the same semantic image Operation, cross a process boundary and then a declared
    machine boundary where practical, and expose representation, authority, state handoff,
    interruption, retry, fallback, and provider selection. The proof must not share private CLR
    types, imply cross-domain federation, or count GPU work as complete.

## 7. Governing discipline

Linen is specification-driven, but it must not merely duplicate Fabric's tests line for line.
The source-of-truth order is:

1. `Atlas-Architecture-0.5.md` for architectural semantics;
2. this Linen plan for implementation boundaries and milestone gates;
3. versioned external manifest and value fixtures for interchange data; and
4. Fabric and Linen behaviour as experimental evidence, never as specification by itself.

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
- Shared Operation identity never implies purity, batchability, relocatability, or acceleration.
- Required generic, Profile, and authored-provider dependencies remain distinct from preferences.
- An opaque boxed application remains valid without exposing an internal Atlas composition.
- A bound or selected Execution never hides known provider, representation, copy, retry, fallback,
  placement, or failure-boundary facts.

The §16.6 Enrichment cases are kept in a separate experimental suite. They cite the work-in-progress
section for traceability but are not counted as normative conformance. Fabric and Linen compare
observable availability, failure, and authority boundaries without sharing resolver code or
requiring the same realisation strategy.

The Architecture 0.5 Composition cases are also kept in a separate experimental suite. They test
dependency strength, boxed applications, provider selection, explicit optimisation claims, and
structured explanations, but do not count Component descriptors, system-service discovery,
Binding Plans, execution explanations, or optimisation-property vocabularies as ratified Atlas
conformance. Fabric and Linen compare external fixtures and observations only after each has an
independent model.

When Fabric and Linen disagree, neither implementation is presumed correct. Reduce the difference
to the Atlas text and record one of four findings: Fabric defect, Linen defect, underspecified
binding, or Atlas ambiguity. That classification is Linen's main research output.

Retained evidence lives under `Linen/docs`: `milestone-evidence.md` records repeatable gates,
`implementation-findings.md` records reduced disagreements and experimental results, and
`experimental-and-sideline-projects.md` keeps GPU and other non-milestone work out of the critical
path. Documentation never claims failing-first history, cross-stack execution, or machine-boundary
evidence that the repository cannot reproduce.

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
  Linen derives its Enrichment and Composition functions independently from Architecture 0.5 and
  compares only observable cases after both designs exist.
- **Premature binding standardisation.** A convenient JSON, CBOR, or CLR representation becomes an
  accidental Atlas wire contract. Test encodings are versioned, explicitly non-normative, and
  replaceable; semantic adapters remain visible Components or attributable transformations.
- **Operational opacity.** Shared Operation names make process, machine, representation, retry, or
  failure boundaries disappear. Structured explanations are milestone gates, not optional logging.
- **System-service gravity.** Linen tooling quietly treats its own history, identity, metadata, or
  workspace provider as the only legitimate implementation. Generic requirements, stronger
  Profiles, preferences, and authored-provider dependencies remain distinguishable and testable.
- **SDK and language drift.** A later scaffold pins an SDK or introduces C# Linen projects for
  convenience. Automated repository checks reject `global.json`, non-F# Linen projects, and Fabric
  references outside the external process harness.
- **GPU scope creep.** GPU work delays independent Base and interchange evidence or is simulated by
  a vector loop. GPU execution remains an experimental sideline and is not a Linen milestone.

## 9. Immediate next actions

1. Create `Linen/Linen.slnx` and the M0 F# projects only. Do not add `global.json`; target .NET 10
   through project configuration and verify normal installed-SDK resolution.
2. Add an automated repository check that every Linen-owned project is `.fsproj`, Model and Kernel
   have the required dependency direction, experiments do not flow into them, and no Linen project
   references Fabric.
3. Encode four failing Atlas 0.5 tests: Operation versus Execution, the worked authority attack,
   additive Shape projection, and required versus optional Declared Fragments.
4. Spike the immutable `World` and pure `step` boundary with one accepted and one denied
   `Fan.Stop` Execution, recording both decisions as immutable data.
5. Define the independent Shape and value algebras without reflection, shared CLR DTOs, or Fabric
   source inspection beyond externally observable contracts.
6. Draft the versioned Cooling manifest fixture with authored identities, Operations, Shapes,
   Fragments, authority requirements, dependency strengths, and explicit binding limitations.
   Compare it with what Fabric can expose and classify every missing field before implementing the
   process binding.
7. Define the minimum structured binding observation needed to explain provider selection,
   representation, boundaries, copies, retries, failure, fallback, Outcome, and timing without
   claiming a ratified Atlas explanation model.
8. Write the M4 targeted-Enrichment examples and the initial M9 composition guardrail examples
   independently, keeping both outside normative conformance until their semantics are ratified.

## 10. Changes from Implementation Plan 0.1

Implementation Plan 0.2 moves Linen from Atlas Architecture 0.4 to 0.5 without changing the Base
kernel direction. It makes F# mandatory for all Linen-owned code and tests, removes SDK pinning from
the intended scaffold, and preserves the immutable `World` plus pure `step` architecture.

The revision adds an isolated `Linen.Experimental.Composition` project and suite for Architecture
0.5's non-ratified Component, optional system-service, boxed-application, dependency-strength,
optimisation-claim, and structured-explanation directions. None enters Model, Kernel, or normative
conformance.

Cooling remains the first two-way Fabric/Linen proof. The mixed image workspace becomes the later
decisive proof, with independently adopted facilities, visible provider replacement, process and
declared machine boundaries, and operationally honest explanations. GPU execution is explicitly
classified as an experimental sideline rather than a Linen milestone.
