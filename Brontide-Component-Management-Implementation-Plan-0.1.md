# Brontide Component Management Implementation Plan 0.1

Status: Partially implemented experimental work
Implementation state: CM0 is implemented and tested independently in both stacks; CM1–CM6 remain
planned.
Designed for: [Brontide Architecture 0.8](./Brontide-Architecture-0.8.md) §18.1, §19,
§20.1, §24, and §33, Complete Draft, not ratified
Design source:
[Component Management and Distribution](./docs/future/component-management/Brontide-Design-Note-Component-Management-0.1.md)
Related notes:
[Composition and Components](./Brontide-Design-Note-Composition-0.1.md),
[Topology Environments and the Guardian Family](./Brontide-Design-Note-Topology-0.1.md)

## 1. Purpose and evidence boundary

Build an entirely fake, deterministic Component Manager in each implementation. The harness exists
to discover architectural mistakes early and to support automated testing of composition
mechanisms. It is not a real marketplace, package manager, security product, dynamic code loader,
or Architecture 0.8 conformance claim. Architecture 0.8 §33 records this sequencing expectation
directly: a deterministic, entirely fake manager should exercise these seams and present a
realistic local storefront before any online marketplace or production loader is attempted.

This experiment has its own Architecture 0.8 target. It does not change either stack's Architecture
0.7 target, and its output remains experimental rather than evidence that the provisional Component
model is ratified.

The two stacks implement the behaviour natively:

- proposed Reference boundary:
  `Reference/src/Brontide.Reference.Experimental.ComponentManagement` with tests in
  `Reference/tests/Brontide.Reference.ComponentManagement.Tests`;
- proposed Minimal boundary:
  `Minimal/src/Brontide.Minimal.Experimental.ComponentManagement` with tests in
  `Minimal/tests/Brontide.Minimal.ComponentManagement.Tests`.

Neither production boundary may reference the other stack. Shared material is limited to external,
versioned data fixtures and expected observable outcomes.

## 2. Fake means controlled, not semantically weak

The first implementation deliberately has:

- no network access or online catalogue;
- no real package download, unpacking, installation, or arbitrary code execution;
- no production signature, identity, attestation, reputation, or origin verifier;
- no operating-system integration or machine-wide package database;
- no live hot swapping; and
- no claim that a fake trust decision proves real-world trust.

It still models each boundary explicitly. Test fixtures stand in for Component Sources, immutable
artifacts, evidence issuers, local policy, the resolver, generation storage, activation hosts, and
cutover failures. A fake verifier returns attributable test evidence and a fake policy records why
it accepted or rejected that evidence; a Boolean named `trusted` is insufficient.

## 3. Minimum model

Each stack owns native representations for:

1. a strongly typed source identity and a fake source that enumerates descriptors and retrieves
   artifacts by immutable identity while exposing source-neutral storefront presentation data;
2. distinct source-endpoint, publisher, package, Component definition, activated Component
   occurrence, Actor, contract, binding-scope, generation, restart-scope, Composition Region,
   Composition Port, Topology Node, and evidence identities;
3. a Component descriptor declaring provided and required contracts, versions, Composition and
   Activation Parameters, dependencies, requirement scope, Provider Set cardinality, sharing and
   exposure, Preferred Providers, requested authority relationships, activation-group membership,
   Local Initialisation, Interconnection, optional Relational Initialisation, Ready, Release, and
   failure behaviour;
4. an immutable fake artifact carrying deterministic content and digest observations;
5. attributable integrity, origin, signature, review, and policy-decision evidence;
6. a standardised fake Discovery Query and attributable candidate record from each source;
7. a pending selection set, occupied-binding inventory, Provider Set and activation-occurrence
   planner, deterministic candidate ranker, and recursive resolver;
8. an inspectable Proposed Stack containing retained occupants, preselected candidates, preference
   requesters, Provider Set cardinalities and assignments, binding scopes, occurrence sharing,
   distinct or mediated exposure, containing Regions and Ports, proposed Topology Relations,
   alternatives, exclusions, conflicts, sources, publishers, and decision provenance;
9. fake distinct-exposure bindings and Selection, Distribution, Aggregation, and Arbitration
   mediations with identity-preserving member and decision traces;
10. an immutable resolved-generation record containing effective Parameters, dependency closure,
    strongly connected groups, Provider Sets, activation occurrences, child Regions and Ports,
    topology membership, provider and mediation decisions, exclusions, and provenance;
11. an optional prepared-generation marker that grants no authority and performs no Component
   effects;
12. a fake activation host with an explicit restart boundary, separate lifecycle and ordinary-
    interaction gates, named establishment stages, per-member readiness, generation-wide Release,
    and old/new generation cutover; and
13. structured discovery, resolution, establishment, release, activation, rollback, and rejection
    Outcomes; and
14. deterministic child-generation and host-assisted-device fixtures, including a sealed bootstrap,
    an outer discovery provider, device-local admission policy, and an explicitly Host-owned variant.

A descriptor's claim and the manager's observation remain distinguishable. Loading or selecting a
fixture grants no Capability and does not establish its requested Actors or authority.

The harness models only the minimum topology-membership floor that the Composition direction owns:
local Topology Nodes and attributable Topology Relations. Topology Map, Environment, Protected
Environment, Protection Plane, Guardian, Gatekeeper, Sentinel, Sentinel Watch, and Environment View semantics belong to
the future `Topology` extension direction recorded in the
[Topology design note](./Brontide-Design-Note-Topology-0.1.md) and stay outside this plan.

## 4. Delivery sequence

Delivery status: CM0 is implemented in both stacks
(`Reference/src/Brontide.Reference.Experimental.ComponentManagement` with tests in
`Reference/tests/Brontide.Reference.ComponentManagement.Tests`, and
`Minimal/src/Brontide.Minimal.Experimental.ComponentManagement` with tests in
`Minimal/tests/Brontide.Minimal.ComponentManagement.Tests`), sharing the data-only fixtures under
the root `component-management/` tree. CM1-CM6 remain planned.

### CM0 — vocabulary and fixtures

Define the data-only fixture format and native identity types. Include multiple sources advertising
the same Component identity; one publisher mirrored by several sources; one source serving several
publishers; distinct package, Component definition, activated occurrence, Actor, and binding-scope
identities; several definitions providing one contract; several occurrences of one definition;
occupied bindings; scoped system defaults; explicit preferences; generic candidates; conflicting
versions; missing artifacts; and contradictory evidence. Fixtures contain no shared executable
semantic logic.

Include two independently attached fake mice, each with pointer, button, configuration, and battery
functions related to a distinct Topology Node. Include attributable, contradictory, and malicious
device topology claims so the implementations must preserve membership without accepting physical
grouping, identity, trust, or authority on assertion alone.

Define a source-neutral storefront projection with deterministic fixture display names, publisher,
descriptions, imagery references, categories, versions, compatibility, evidence status, lifecycle
state, dependency summary, and alternatives. It is a future UI seam, not a UI implementation.

### CM1 — discovery, acquisition, and evidence

Implement standardised Discovery Queries and deterministic staged acquisition from any number of
fake sources. Keep source endpoint and publisher identity distinct. Record which source supplied
each descriptor, artifact, and evidence item. A source may disappear after acquisition without
changing immutable staged content. Successful discovery or acquisition must not select, resolve,
prepare, or activate the Component.

### CM2 — recursive generational resolution

Resolve a pending selection into a complete immutable generation:

- expand Component requirements and Composition Parameters recursively;
- preserve each requirement's binding scope, minimum and maximum Provider Set cardinality, sharing
  rule, and distinct or mediated exposure;
- retain a compatible occupied `1..1` binding unless the user or authorised replacement policy
  says otherwise;
- permit several definitions and several occurrences of one definition to coexist for one contract;
- satisfy required Provider Set positions deterministically without filling optional capacity merely
  because candidates exist;
- share an occurrence only when its declared isolation, lifecycle, and authority rules allow it;
- produce direct Binding Plans for `1..1` and deliberately member-addressed distinct bindings, but
  require declared Mediation whenever one logical endpoint selects, falls back, load-balances,
  distributes, aggregates, arbitrates, masks membership, or owns topology-wide ordering,
  backpressure, failure, or recovery policy;
- record whether Mediation is realised by a dedicated fake Component or erased into fake Host
  construction, without erasing the relationship or its trust consequence;
- resolve complete child generations within declared Composition Ports of active parent Regions;
- enforce each Port's contracts, cardinality, imports, exports, authority ceiling, topology
  requirements, lifecycle mode, failure policy, and rollback boundary; reject excess requirements or
  propose a wider parent generation instead of changing it implicitly;
- construct distinct local Topology Nodes for attachment occurrences and accept, refine, or reject
  attributable Topology Relations according to fake local policy;
- highlight unused Preferred Providers and the Components that requested them;
- for unfilled required Provider Set positions, rank an admissible explicit preference first, then
  publisher-affine, generic, and other compatible candidates;
- apply trust, origin, platform, version, authority, resource, and local policy at every tier;
- keep a repository endpoint distinct from publisher affinity;
- make provider, version, and tie-break choices deterministically;
- bind no Activation Parameter until structural closure exists;
- then bind or validate declared Activation Parameter slots from the fake environment;
- produce the Proposed Stack with preselected candidates, Provider Set membership, activation
  occurrences, scopes, sharing, exposure and mediation, alternatives, evidence, requested authority,
  conflicts, and decision explanation for every role;
- record every default, forwarded value, source claim, exclusion, and decision; and
- reject missing, ambiguous, incompatible, unbounded, or traversal-order-dependent closure.

Resolution occurs while a prior generation may remain active and has no effect on it.

### CM3 — cyclic groups and activation phases

Detect strongly connected dependency groups and resolve them as units. Accept finite compatible
contract and ordinary interaction cycles without inventing component startup order. Require each
member to complete Local Initialisation, Interconnection, any declared Relational Initialisation,
and Ready behind a closed ordinary-interaction gate. Accept a cyclic relational protocol only when
its lifecycle Operations and bounded completion rules are declared. Reject undeclared lifecycle or
ordinary pre-release traffic, circular Ready waits, version-conflict cycles, recursive descriptor
expansion, and a member that cannot reach Ready from its declared inputs, with a deterministic
explanation naming the group and edges.

Apply the same group analysis across Region boundaries. A cycle contained by declared Port imports
and exports may resolve as one bounded activation group; a cycle requiring undeclared parent access
is rejected or reported as requiring a wider parent generation.

### CM4 — preparation, activation barrier, scoped restart, and rollback

Model preparation as an optional, effect-free optimisation. Activate a resolved generation through
an explicit fake restart scope. Establishment advances through named Local Initialisation,
Interconnection, optional Relational Initialisation, and Ready stages. The Host rejects same-group
peer traffic during Local Initialisation, permits only declared lifecycle Operations during
Relational Initialisation, and keeps ordinary interaction gated until one logical Release. After
Release, interaction is admitted without a specified first-component order.
After Release, the fake Host exercises distinct and mediated bindings while recording member
identity, source provenance, routing or delivery decisions, failure, and authority checks.
Prove that an unrelated scope remains active, the cutover point is observable, the old generation
terminates according to policy, and a failed establishment or release either restores the retained
generation or reports that rollback was explicitly unavailable. Never silently widen the restart
scope.

Also activate a child generation through a runtime-open Port while its parent remains active. For a
host-assisted device, establish and Release the device's internal generation before releasing its
exported boundary into the outer generation. Distinguish this initial attachment to an empty Port
from replacement of an active child, which requires its own lifecycle contract.

### CM5 — authority and admission seam

Expose requested Actor relationships and authority as requests for local policy, never self-grants.
Exercise accepted narrow grants, rejected requests, malicious claims of unlimited authority,
revoked or expired evidence, and policy mistakes that are recorded as local decisions. The harness
does not need production cryptography to prove that evidence, policy, and authority establishment
are distinct stages.

### CM6 — independent comparison

Run common external fixtures through both native implementations and compare resolved records and
Outcome categories across a process or serialized-data boundary. Equal fixture results establish
agreement on the tested fake model only; they are not real Component interchange or security
evidence.

## 5. Mandatory automated vectors

At minimum, both stacks test:

- zero, one, and several Component Sources, including duplicate and conflicting claims;
- one publisher mirrored across sources and one source hosting unrelated publishers;
- a fake local source and fake remote source projecting the same storefront presentation fields;
- offline/local import behaving as a source without privileged trust;
- source removal after immutable acquisition;
- selection leaving the active generation unchanged;
- nested Composition Parameters introducing further requirements;
- Activation Parameters filling declared slots but being unable to add Components or roles;
- deterministic resolution under permuted source and descriptor enumeration;
- a compatible occupied `1..1` binding being retained while a Preferred Provider is highlighted
  with its requester and reason;
- several compatible Component definitions providing one contract remaining simultaneously active
  in different scoped bindings;
- several activated occurrences of one Component definition remaining separately identifiable;
- one scoped `1..1` system-default database binding coexisting with a different application-specific
  provider and a multiple-member database Provider Set;
- a Provider Set admitting another provider without displacing a compatible member when its upper
  bound and policy allow it;
- an optional Provider Set position remaining empty rather than being filled merely because a
  compatible candidate exists;
- one provider occurrence being shared only when its sharing and authority contract permits it, and
  otherwise resolving to separate occurrences;
- distinct exposure preserving per-member identity, authority, provenance, and failure;
- a simple `1..1` topology and a deliberately member-addressed distinct set requiring no Mediation;
- a logical endpoint over several members being rejected when Selection, Distribution, Aggregation,
  or Arbitration is not declared;
- a statically erased Mediation remaining visible in the resolved generation and explanation;
- a policy-bearing Mediation with mutable membership, residue, backpressure, authority, recovery, or
  lifecycle being represented by a dedicated fake mediating Component;
- several keyboards binding separately to different user or session scopes;
- several keyboard sources feeding one Aggregation while every input occurrence preserves source
  identity and the declared ordering and backpressure policy;
- several displays receiving independent feeds through distinct bindings and one feed through
  explicit Distribution;
- Selection over several providers preserving declared affinity, and member failure following the
  declared fallback rule rather than an inferred one;
- mediated exposure granting no consumer the union of backing-member authority;
- static Provider Set membership remaining fixed for a generation, and runtime attachment or
  detachment being denied unless the descriptor declares its cardinality, admission, authority,
  occurrence, in-progress-work, and failure semantics;
- a parent Region remaining active while a complete child generation is resolved, established, and
  released through a declared runtime-open Port;
- runtime composition through an undeclared or sealed Port being rejected;
- a child exceeding a Port's contract, authority, topology, or cardinality envelope being rejected
  or producing an explicit wider-parent-generation proposal;
- internal optional subsystems and externally attached devices following the same regional
  composition rules;
- attaching to an empty Port not being treated as hot replacement, while replacement and detachment
  require declared quiescence, state, failure, and rollback behaviour;
- a compatible cycle across Regions resolving as a bounded activation group, and an undeclared
  cross-boundary cycle being widened explicitly or rejected;
- two attached mice receiving distinct Topology Nodes so no resolver combines the pointer sensor of
  one with the buttons of the other merely because their contracts are compatible;
- a shared receiver with insufficient finer evidence retaining separate, ungrouped function
  occurrences rather than inventing mouse membership;
- `SamePhysicalAssembly`, `HostedBy`, `SharesPowerDomain`, `SharesFailureDomain`, identity, and
  authority remaining separate observations rather than collapsing into one `same device` fact;
- malicious or unsupported topology claims being rejected while locally observed attachment
  membership remains usable;
- detachment and reattachment creating a new local Topology Node unless persistent identity is
  separately established;
- an explicit admissible preference winning an unfilled required Provider Set position;
- an unavailable explicit preference falling through to publisher-affine, generic, then other
  compatible candidates;
- publisher affinity following publisher identity rather than the serving source endpoint;
- trust or compatibility policy excluding a higher-ranked candidate with an explanation;
- a Proposed Stack containing preselected candidates, retained occupants, alternatives, conflicts,
  evidence, requested authority, restart scope, and preference explanations;
- missing dependency, incompatible contract, version conflict, unsupported Constraint, and
  unavailable Activation Parameter;
- accepted finite dependency and post-release interaction cycles;
- Local Initialisation denying same-group peer interaction;
- Interconnection establishing inert endpoints while ordinary interaction remains denied;
- Relational Initialisation admitting only declared lifecycle Operations under narrow authority;
- a cyclic relational handshake reaching its bounded completion condition;
- an undeclared lifecycle or ordinary interaction attempt being denied before Release;
- cyclic members all reaching Ready and then interacting after one Release;
- failure of any required member to reach Ready preventing release of every group member;
- explicitly ordered activation groups resolving in their declared order, with cyclic group-order
  dependencies rejected;
- preparation producing no active Actor or authority;
- a host-assisted device booting only its sealed recovery and composition substrate before using an
  outer Host's Discovery results;
- a discovered candidate remaining inert until device-local admission accepts it, and rejection
  leaving the bootstrap generation intact;
- an outer Host controlling admission only in an explicitly declared Host-owned device mode;
- a host-assisted internal generation reaching Ready and Release before its exported outer boundary
  is activated;
- activation at device, workspace or session, service-group, and whole-system fake scopes;
- an unrelated scope surviving a scoped restart;
- no observable assumption about which Component runs first after logical Release;
- activation failure during each named stage, Release, and after the declared cutover point;
- rollback available, rollback impossible by declaration, and retained-generation corruption;
- signature, origin, review, and attestation claims rejected independently;
- a Component requesting unlimited authority and receiving none unless local policy grants it; and
- Component removal leaving Durable Dataset fixtures untouched.

Property-based tests should generate descriptor ordering, compatible cyclic graphs, incompatible
cycles, and failure points. Resolver output and diagnostics must be deterministic for the same
inputs.

## 6. Completion gate

This plan is complete only when:

- both independent experimental projects and their NUnit suites exist;
- the required vectors pass hermetically with no network or credential requirement;
- dependency guards prove that neither stack references the other;
- shared fixtures contain data and expected observations, not implementation logic;
- READMEs and experimental-project inventories identify the work as fake and non-conformant;
- comparison evidence states exactly which observations agree and what remains untested; and
- neither stack's locally stated architecture target nor any ratification claim changes as a result.

A later plan may replace fake sources and evidence with real distribution integrations. That work
must add live-probe boundaries, production trust analysis, artifact isolation, and operating-system
lifecycle concerns rather than silently upgrading the claims of this harness.

## Open questions (owners needed)

No unresolved owner decisions are currently recorded. New unresolved decisions belong here rather
than in delivery prose.

## Resolved questions

- **2026-07-23 — Evidence boundary:** the managers remain deterministic experimental harnesses, not
  production package managers, marketplaces, loaders, or Architecture 0.8 conformance evidence.
- **2026-07-23 — Delivery state:** CM0 fixtures and strict loaders are implemented in both stacks;
  CM1–CM6 remain future work.
