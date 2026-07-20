# BRONTIDE

## Design Note: Composition and Components

**Status:** Work-in-progress design note, version 0.1
**Originally extracted from:** Brontide Architecture 0.6, §18.1

**Current architecture context:** [Brontide Architecture 0.8](./Brontide-Architecture-0.8.md),
§18.1; the current architecture document retains a summary section under the same number. Staged
acquisition and generation management are expanded in
[Component Management](./Brontide-Design-Note-Component-Management-0.1.md). The broader Environment,
Gate, protection, and relational-transparency direction is recorded in
[Topology Environments and Gates](./Brontide-Design-Note-Topology-0.1.md).
**Scope:** Records design directions. Nothing in this note enlarges Brontide Base,
ratifies the Component model or the Brontide Portable Binding, or defines conformance
requirements.

References of the form §N refer to the Brontide Architecture specification.

---

Brontide uses *component* when discussing composition and Reference/Minimal interchange, but the category
must not inherit the scale or packaging model of conventional application frameworks.

A **Component** is a scale-independent, bounded unit of composition that declares the Brontide
contracts it provides and requires. A Component boundary may enclose a function library, firmware
subsystem, process, device, service, cluster, data centre, organisational system, or another
recursively composed environment. Physical size, address-space placement, language, transport,
and deployment mechanism do not determine whether something is a Component.

Component is not a ninth Brontide Base term and is not an authority-bearing participant. An Actor is
a participant in authority relationships; a Component is a unit of composition. One Component may
realise several Actors, one Actor may be realised by several Components, and a Component may
contain other Components. Capabilities are held and presented by Actors. Loading, attaching, or
binding a Component grants no authority by itself.

## Ordinary construction and Composition conformance

Every system is composed in the ordinary sense: authored code, firmware, configuration, deployment,
or physical attachment places its participants together. `Composition` becomes an architectural
contract only when that arrangement is itself exposed for independent implementation and tooling.
Base constrains the resulting Actors, Capabilities, Operations, and occurrences, but does not require
every participant to describe how it was selected or connected.

This permits **passive integration**. A mouse, fixed firmware subsystem, library, device, or remote
endpoint may expose a Base or Profile boundary and be incorporated by a Host that understands
Composition. The leaf need not implement a resolver, Discovery Query, Component Manager, or even a
participant-visible Component descriptor. The composing Host owns the external descriptor or
adapter, binding decision, admission policy, and authority establishment; it may not invent semantic
or authority claims that the leaf boundary does not support.

The intended specification shape is therefore layered:

1. a first-party **Composition extension** should ratify Components, requirements, Parameters,
   Composition Regions and Ports, Provider Sets, direct and mediated bindings, Binding Plans,
   resolved generations, activation, scoped restart, incremental composition, and optional
   replacement;
2. Composition must support both static authored resolution and dynamic resolution without making a
   runtime resolver, loader, or manager mandatory;
3. a separate optional **Discovery extension** should supply authorised queries, candidates,
   evidence, sources, and runtime observations to resolvers, while granting no authority and making
   no binding decision; and
4. provisional Profiles should express deployment expectations: a **General-Purpose System Profile**
   requires inspectable Composition and Component management and is expected to include Discovery,
   while a **Static Embedded Profile** requires none of that machinery and may be passively composed
   by a larger Host.

Component Manager names one facility over these contracts, not the Composition extension itself. A
build tool may erase resolution into a firmware image; Host machinery may construct generations; one
or more Components may provide management Operations. The extension standardises the observable
structure and invariants, not one control process or user interface.

## Worked composition boundaries

The same physical system may be composed at several boundaries and times. The word `Component`
applies only where a Composition contract exposes that boundary; it is not an automatic name for
every source module, firmware block, physical device, or Base implementation.

| Boundary | Composition owner | Typical time | Portable Composition visible? |
| --- | --- | --- | --- |
| Mouse firmware internals | manufacturer and build tool | design, build, factory image, and boot | normally no |
| Host-assisted device internals | device-local Composition Host, assisted by the outer Host | boot, provisioning, runtime regional activation, and recovery | yes inside the device |
| Mouse integrated into a Host | Host attachment policy and resolver | attachment or scoped activation | yes, when the Host claims Composition |
| Managed general-purpose system | system builder, administrator, user, and Component Manager | image construction, installation, preflight, restart, and runtime scopes | yes |

### Factory-composed embedded mouse

An ordinary Brontide-capable mouse does not carry `Base` as a Component. Base is the semantic and
authority contract obeyed by its exposed Actors, Operations, Events, and Capabilities. Its firmware
may contain implementation units such as:

```
sensor sampling
button scanning and debouncing
pointer transformation
transport endpoint
power and configuration control
```

These units are not automatically Brontide Components. The manufacturer may link them into one
fixed image, generate static dispatch and authority tables, verify the image, and install it at the
factory. Boot materialises that authored structure; it does not need to rediscover or resolve it.
The mouse may replace the whole image during an authorised firmware update without ever claiming the
Composition or Discovery extensions.

At its external boundary, the same image may expose several Actors:

```
MousePointer
    emits Input.Pointer.Motion
    emits Input.Pointer.Button

MouseConfiguration
    exposes Input.Pointer.Sensitivity
    exposes Input.Pointer.PollingRate
```

It may therefore conform to Base and relevant Input, peripheral, Channel, or Flow contracts without
describing its internal firmware decomposition. A more capable programmable mouse may deliberately
implement Composition for replaceable gesture, lighting, radio, or automation Components, but that
is an additional claim rather than the definition of a mouse.

### Attachment-time composition by the Host

When the mouse is attached, a general-purpose Host performs a second composition at a different
boundary. The device may supply a protocol descriptor or authored metadata, but those are claims and
evidence, not authority. A Host-side adapter, local catalogue, manufacturer package, or the device
itself may provide the Component description used for compatibility. The Host's policy decides which
functions to admit and which evidence to trust.

The resolved Host composition may represent the whole endpoint as one `MouseEndpoint` Component
exposing `MousePointer` and `MouseConfiguration` Actors, or use separate functional Components when
their contracts, lifecycle, or authority differ. The physical placement does not decide that
boundary. Interconnection creates the Host-domain Actor endpoints and grants only the admitted
Capabilities. If several pointing devices feed one logical input Flow, the consumer binds to the
declared Aggregator while that Aggregator holds the backing device relationships.

The mouse remains passive with respect to this Host composition. It need not know the Host's
Provider Sets, preferred adapter, input Aggregator, Workspace, users, or restart generation. Its
responsibility ends at the contracts it actually exposes.

### Host-assisted composable device

A more capable device may expose its internal composition rather than shipping only one sealed
image. It still cannot begin from nothing: a factory-installed **bootstrap composition** provides a
Composition Host or signed-plan verifier, recovery path, local authority roots and admission policy,
an authorised Channel to an outer Host, and enough loader and activation machinery to construct the
next internal generation.

The outer Host may make its Discovery facility available through an explicit Capability. The device
submits requirements; the Host returns descriptors, artifacts, and attributable evidence; and the
device resolves or verifies a proposed internal generation. Discovery does not install the result.
In the ordinary host-assisted mode, device-local policy decides what to admit and which internal
Capabilities may be derived. A **Host-owned** mode is also possible, but the device must explicitly
designate the outer Host as composition authority rather than inferring that authority from the
connection.

After the internal generation completes Establishment and Release, it exports a stable Component or
Actor boundary. Only then does the outer Host compose that boundary into its own generation. The two
resolutions may be prepared together, but their authority domains, Ready conditions, failure and
rollback rules remain distinct. A mouse-shaped implementation of this profile is architecturally a
small managed computer; physical product category does not weaken the composition contract.

This direction is recorded as the provisional **Host-Assisted Composable Device Profile**. Its exact
Channel, distributed authority, plan-signature, artifact, recovery, and attestation protocols remain
open.

### Managed general-purpose system

A managed workstation, service node, or operational system normally composes at several times:

1. **Image or factory construction** establishes a minimal Host, authority roots, Component Manager,
   local Component Source, recovery path, and enough presentation or management machinery to start.
2. **Installation or administrative selection** adds applications and system facilities. While the
   current generation remains active, Discovery supplies candidates and the resolver builds an
   inspectable Proposed Stack containing providers, bindings, Mediation, authority requests, and
   restart scope.
3. **Preflight and scoped activation** prepare the resolved generation, then perform Local
   Initialisation, Interconnection, optional Relational Initialisation, Ready, and one Release at a
   system, service, session, Workspace, or device boundary.
4. **Runtime attachment** may bind a device or service into a declared runtime Provider Set, or may
   stage another generation when the change is structural. Attachment never grants authority merely
   because a compatible participant was discovered.

Typical system Components may provide presentation, Workspace organisation, input Aggregation,
display Distribution, persistence, identity, policy, audit, recovery, and Component management, as
well as ordinary applications. None is `Base`; each Component realises one or more Actors whose
interactions remain governed by Base. A minimal profile may pre-resolve all of them into a static
image, while the General-Purpose System Profile exposes the same structural decisions to users and
tools.

A Component with an Brontide-visible boundary declares, as applicable:

- the Profiles, Extensions, and Domain Vocabularies it provides and requires;
- the Operations it provides or consumes, including their input and output Shapes;
- required Declared Fragments and open-Shape fragment policy;
- the Corpora it understands, the roles it can perform concerning them, and the Store contracts
  required by those relationships (§18.2);
- the Actor relationships and authority requirements visible at its boundary;
- versioned Attribute sources and recursive Constraints relevant to placement or binding;
- Composition and Activation Parameters exposed by the definition; and
- its binding model, including requirement scope, Composition Regions and Ports, provider
  cardinality, exposure, topology membership and requirements, and, where applicable, Slots and
  Classes; Local Initialisation inputs and
  outputs; Interconnection requirements; optional Relational Initialisation
  Operations and lifecycle authority; Ready, Release, failure, and rollback behaviour; Preferred
  Providers and other discovery hints; and lifecycle limitations that narrow its composition or
  substitutability claims.

The contract describes observable Brontide semantics, not a private language type, object model,
package format, process protocol, or wire encoding. Two Components may realise the same contract
through entirely different internal machinery.

## Parameters in architectural definitions

A **Parameter** is a named, Shape-described input to an architectural definition. A parameterised
definition describes a bounded family of possible resolved definitions; it is incomplete until
every required Parameter has been bound or an explicit default or absence rule has resolved it.

Parameters are not an alternate name for ordinary application settings. A preference that merely
changes a Component's private behaviour normally belongs in a Dataset. A Parameter is appropriate
when its value shapes architectural resolution or fills a resource slot intentionally left open by
that resolution.

Architecture 0.8 distinguishes two binding stages:

- A **Composition Parameter** is bound while constructing a composition or generation. It MAY
  select or omit Components, requirements, Store roles, provider classes, or other declared
  architectural structure.
- An **Activation Parameter** is bound when an already composed definition is activated in a
  particular environment. It MAY fill a declared Store, device, endpoint, authority-domain,
  identity-scope, credential-reference, or similar resource slot. It MUST NOT introduce a new
  Component, Store role, Corpus Form, authority category, required contract, or other structure
  absent from the resolved composition.

`Activation` is preferred to `startup`: firmware, a service, a device binding, and an organisational
system need not have a conventional process startup. Changing a Composition Parameter creates a
new resolved composition. Rebinding an Activation Parameter creates a new activation of structure
that was already declared.

A Parameter declaration identifies at least its name, binding stage, Shape, binding requirement,
and any definition Constraints. A default is permitted only with explicit semantics. Context-
derived defaults are attributable inputs rather than hidden environment reads; the resolved
definition records their effective values and provenance.

For example:

```
parameters:
    - name: RetainDiagnostics
      binding: Composition
      shape: Boolean 1
      requirement: Optional
      default: false

    - name: CoreStore
      binding: Activation
      shape: Store.Reference 1
      requirement: Required
```

Parameter scope is lexical. A nested or composed definition does not capture a similarly named
Parameter implicitly; forwarding or deriving a value is explicit. Selecting a Component may expose
that Component's declared Parameters and requirements and thereby expand the pending composition.
That is recursive composition: every structural choice and nested Composition Parameter is resolved
to finite dependency closure before activation. It does not make an Activation Parameter
structural. Architecture 0.8 admits Shape-composed Parameter values but does not ratify unconstrained
parameter-generating parameters.

## Staged management and composition generations

A user may select a Component while the current composition remains active without making the
selection a runtime binding or hot swap. The structural effect belongs to a **pending composition**.
A resolver expands the selected Component's requirements and Parameters recursively, selects
compatible providers, constructs Binding Plans, and produces a complete **resolved generation**.
Only that generation is activated at cutover.

The conceptual lifecycle is:

```
discover or acquire → select → resolve → prepare
                                           ↓
Local Initialisation → Interconnection → Relational Initialisation? → Ready → Release as Active
```

The stages are semantic, not necessarily separate processes or persistent records. An embedded
image may perform them at build time. A general-purpose environment may acquire and resolve a new
generation while the old one is running, prepare code or data needed by the new generation, and
activate it after a restart. The complete structural tree and its verification record exist before
cutover; preparation is an optimisation and never evidence that activation succeeded.

Activation of a resolved generation has two observable phases: **Establishment** and **Release**.
Establishment contains named stages so requirements are portable and attributable:

1. **Local Initialisation.** Each Component is materialised and initialises private provisional
   state from declared inputs without same-group peer relationships. It exposes inert Actor and
   endpoint descriptions or fails with a declared Outcome.
2. **Interconnection.** The Host establishes Actor identities and endpoints, binds Activation
   Parameters and resources, constructs Binding Plans, evaluates requested local authority, and
   connects the complete group. The ordinary-interaction gate remains closed.
3. **Relational Initialisation** *(optional).* A Component may use only declared lifecycle
   initialisation Operations against declared connected peers under narrow initialisation authority.
   These Operations permit relationship-dependent setup without opening ordinary application
   traffic or changing the resolved composition. Their Shapes, concurrency or ordering, completion,
   retry, timeout, idempotence, failure, and rollback semantics are contract declarations.
4. **Ready.** The group is Ready only after every required Component reports its successful
   establishment Outcome and the Host validates the group.
5. **Release.** The Host opens the ordinary-interaction gate and the generation becomes Active.

The release barrier makes activation **logically simultaneous**. It does not require every
Component to execute a machine instruction at the same instant, nor does it promise an ordering
among interactions admitted after Release. A simple static implementation may erase the machinery
if it preserves the observable stages and the rule that no member participates as Active until the
whole required activation group is Ready.

Local Initialisation is isolated from same-group peer relationships, not necessarily from memory,
processes, security domains, the Host, or already-Active dependencies outside the restart scope.
Relational Initialisation opens only a lifecycle-operation gate; it does not open ordinary
Executions, Events, or Flows early. Trusted Host or binding machinery enforces both gates.

A Component contract therefore separates at least:

- Local Initialisation inputs, Activation Parameters, resources, restored state, inert outputs, and
  failure Outcomes;
- the Actor identities, endpoints, bindings, resources, and authority relationships established by
  Interconnection;
- permitted Relational Initialisation Operations, peers, Shapes, lifecycle authority, progress,
  completion, timeout, retry, idempotence, failure, and rollback semantics;
- its Ready condition and establishment Outcome; and
- behaviour on Release, quiescence, failed group activation, and retirement.

A **Component Manager** is the provisional name for machinery that coordinates some or all of this
lifecycle. It is not a Component Store. `Store` already names a persistent-information resource in
§18.2, and acquisition is only one part of management. A manager may consult any number of
Component Sources, including a local catalogue, removable media, a network repository, a peer, a
build output, or a human-facing marketplace. A marketplace or storefront is a source and user
experience, not the authority that decides what becomes active. Discovery, download, signature,
origin, or popularity claims remain evidence consumed by local policy (§24); none grants authority
or admits a Component by itself.

There is no requirement for one system-wide manager. The manager may be host machinery, one or
more Components, a build tool, or a private facility inside a boxed composition. Static
composition remains possible without it. A detailed distribution and management direction is
recorded in `Brontide-Design-Note-Component-Management-0.1.md`.

### Discovery, preferences, and occupied providers

A manager may consult several Component Sources concurrently. The repository or endpoint serving a
package is distinct from its publisher or authored authority: mirrors may serve one publisher, and
one repository may serve many. The preference described informally as `same source` is therefore
publisher affinity, not affinity to the endpoint that delivered the requesting Component.

A Component requirement identifies the canonical contract, version, Constraints, lifecycle role,
binding scope, provider cardinality, and exposure. It may declare **Preferred Providers**, but
preference remains a selection hint. It cannot grant authority, establish trust, weaken
compatibility, or replace an existing provider by itself. For a `1..1` requirement or occupied
Slot, a compatible current binding remains selected unless the user or an authorised replacement
policy chooses otherwise. Tooling records and highlights the preferred alternative, the Component
that requested it, and why it was not selected.

For each unfilled required position in a Provider Set, the default candidate tiers are:

1. an explicitly Preferred Provider;
2. an admissible compatible provider from the requester's publisher;
3. an admissible generic implementation centred on the canonical required contract; and
4. any other admissible compatible implementation.

Local trust, origin, platform, version, authority, resource, and other policy may exclude or demote
a candidate at every tier. Neither publisher affinity nor a `generic` declaration is proof. Within
a tier, deterministic local policy resolves ties and records the explanation.

A standardised Discovery Query carries the requirement, target, Constraints, requester and
publisher, preferences, and existing binding. Sources return attributable candidate records. The
manager recursively produces a **Proposed Stack** showing retained occupants, preselected candidates,
Provider Set cardinalities and assignments, activation occurrences, binding scopes, sharing and
exposure choices, mediation, sources, publishers, evidence, requested authority, alternatives,
conflicts, preference requesters, restart scope, and unresolved decisions. A future store-like UI
may present the same proposal with the best candidates preselected; automatic or one-action
installation does not erase the record.

### Provider multiplicity, binding scope, and exposure

A canonical contract or lifecycle role is not a system-wide singleton. Four levels must remain
distinct:

1. the contract and role state *what purpose is required*;
2. a Component definition and realisation state *what can provide it*;
3. an activated Component occurrence is one materialised participation in a generation; and
4. its Actor endpoints and provider bindings state *who can actually interact under which
   authority*.

Several definitions may provide one contract, and one definition may be activated several times.
A system-wide default database, for example, is a named or policy-selected binding in a scope, not
ownership of the database role everywhere. One application may bind that default, another may bind
a provider-specific implementation, and a third may bind several providers without conflict.

A **Provider Set** is the resolved, identity-preserving set of provider bindings satisfying one
requirement in one binding scope. The requirement declares a minimum and maximum cardinality; the
ordinary compatibility default is `1..1`, while `0..1`, `0..*`, `1..*`, and finite bounded ranges
are expressible. Every resolved membership remains finite. The resolver satisfies the lower bound
and does not fill optional capacity merely because more compatible candidates exist. Extra members
require an explicit selection or authorised policy. An existing activated occurrence may satisfy
several requirements only when its sharing, lifecycle, isolation, and authority contracts permit
that reuse; otherwise the resolver creates or selects separate occurrences.

Multiplicity does not itself say that members are interchangeable, merged, replicated, or visible
as one endpoint. A requirement therefore declares one of two exposure forms:

- **Distinct exposure** preserves the Provider Set as separately addressable members. The consumer
  can bind or route by member identity, Actor, Attribute, user, session, Workspace, tenant, Dataset,
  device, authority domain, or another declared key. No member becomes a fallback for another and
  no authority is unioned merely because they share a contract.
- **Mediated exposure** presents a declared Mediation endpoint over the Provider Set. Its Selection,
  Distribution, Aggregation, Arbitration, or domain-specific combination defines routing, affinity,
  ordering, delivery, failure, backpressure, and provenance. The mediation remains visible in the
  Binding Plan and does not erase the identities, origins, failure domains, or authority of its
  members.

Thus several keyboards may remain distinct and bind to different user or session scopes, or an
Aggregator may expose their occurrences through one logical input Flow while preserving the source
device of each occurrence. Several displays may receive separate feeds through distinct bindings or
one feed through Distribution. Several database providers may serve different applications or
Datasets directly, sit behind Selection, or participate in a storage-specific replication or quorum
relationship. Multiplicity supplies the common composition structure; it does not invent the domain
semantics of merging screens or replicating data.

For statically bound Provider Sets, membership is fixed by the resolved generation. A runtime-bound
Provider Set may admit attachment and detachment only when its contract declares cardinality bounds,
admission, member identity, authority establishment and revocation, membership occurrences,
in-progress work, and failure behaviour. Discovery of another compatible provider never mutates an
active Provider Set by itself.

### Incremental (per-partes) composition

A large or long-running system need not replace one machine-wide generation whenever one part is
added. A **Composition Region** is a recursively nested composition boundary that owns an
independently resolved and activated generation. A **Composition Port** is a declaration in its
parent Region through which a child Region or Component occurrence may be attached.

A Port declares at least:

- the contracts, direction, role, cardinality, and direct or mediated exposure at the boundary;
- permitted imports from and exports to the parent and neighbouring Regions;
- the maximum requested-authority envelope and the policy point that evaluates it;
- required topology membership or relations;
- whether it is **sealed**, **activation-open**, or **runtime-open**; and
- establishment, Release, detachment, failure, state, rollback, and restart-scope behaviour.

The parent generation is still structurally closed: it contains the Port and its envelope. Filling a
runtime-open Port does not mutate that immutable record. The resolver creates a finite child
generation, recursively closes the selected Component's requirements inside the Port, establishes it
behind its own interaction gate, and atomically publishes its exported bindings at child Release.
The containing Region may remain Active throughout. The system therefore evolves **per partes**
while every participating generation remains complete at its own scope.

This is structural composition, not an Activation Parameter. The Port is declared beforehand, but
the child generation may introduce its own Components and Composition Parameters within the Port's
envelope. If resolution needs an undeclared parent contract, broader authority, incompatible
topology, or a cycle that cannot close within the Region, the manager must reject it or propose a
wider parent generation and restart scope; it must not silently puncture the boundary.

Runtime composition is independent of physical placement. An internal analytics module, local
library, newly attached device, remote service, user session, and device-internal firmware feature
may all occupy Regions under the same rules. Several Regions may be resolved concurrently when their
imports are independent. Cross-Region dependencies and activation ordering remain explicit; a cycle
requiring joint readiness either forms one activation group at a common scope or is rejected.

Attaching to an empty Port is not automatically hot swapping. Replacement of an Active child,
detachment, continuity of Actor identity, state transfer, and uninterrupted parent service require
the corresponding Slot, lifecycle, or mediation contracts. Discovery merely proposes a candidate;
local admission and Interconnection still establish authority.

### Composition topology and discrete membership

Composition needs a minimal topology floor below macro placement and network routing. A **Topology
Node** identifies an item or grouping in a stated observer's topology. A **Topology Relation** is an
attributable assertion between Nodes, Components, Actors, Resources, Regions, or Ports. Neither is an
Actor, a Capability, proof of physical truth, or a source of authority.

At minimum, a resolved composition records containment between Regions, Ports, Component
occurrences, and their exported Actor endpoints. An attachment occurrence also creates a distinct
local Topology Node under the admitting Host. Functions admitted from one composite mouse can be
related to that node, preventing an input resolver from accidentally pairing the optical sensor of
mouse A with the buttons of mouse B. Detachment terminates the attachment relation; reattachment is a
new node unless policy establishes persistent identity.

`PartOf`, `Contains`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`, `SharesPowerDomain`, and
`SharesFailureDomain` illustrate different relations; this list is not ratified vocabulary. They
must not be collapsed. One receiver may host several mice, one enclosure may contain several
authority or failure domains, and one virtual device may span several physical assemblies. A user
interface may project selected accepted relations as `same device`, but portable composition reasons
over the underlying relations and their evidence.

When a Host observes only a shared receiver and cannot establish finer membership, it records the
uncertainty rather than guessing. Separately admitted function occurrences may each be
`AttachedThrough` the receiver while remaining ungrouped with one another. Pairing a sensor with
buttons then requires an attributable description, independent observation, or explicit local choice.
Topology makes incomplete knowledge visible; it cannot reconstruct physical truth that no available
evidence distinguishes.

A device descriptor may propose a topology hierarchy. The Host records who asserted it and may
replace, refine, or reject it using attachment-path observations, administrator policy, attestation,
or other evidence. Locally synthesising nodes is a valid implementation; the portable requirement is
that membership identity, source, lifetime, and relevant relations remain visible. The broader
`Topology` extension may add spatial, network, latency, power, residency, and failure graphs, while
this membership floor remains required for Composition occurrences.

The richer direction is resolved in
[Topology Environments and Gates](./Brontide-Design-Note-Topology-0.1.md). Ordinary Environments
remain overlapping topology identities rather than Components; Gates create their relational
virtual-Component projections. Protected Environments are disjoint or nested within one Protection
Plane, are opaque except through Gates, and become Sealed when no Gate is active. These terms remain
outside Base and do not change Composition's minimum membership floor.

### Scoped restart and generational replacement

Activation may occur at the smallest declared boundary capable of independent replacement: a
device host, workspace, user session, service group, process, or complete system. Such a **scoped
restart** is still replacement by generation, not hot swapping: the affected composition becomes
inactive across its cutover. The scope, quiescence, failure boundary, retained state, authority
disposition, and rollback behaviour must be declared. A manager must not silently widen a promised
restart boundary when resolution or activation fails.

Changing a Component selection or Composition Parameter creates a new generation. Activation
Parameters may be obtained and validated during preflight—even while the previous generation is
active—but they bind only slots already present in the resolved generation. This ordering permits
a swift restart without letting environmental input introduce an unbounded second round of hidden
composition.

### Dependency cycles

Brontide does not require the logical Component requirement graph to be acyclic. Mutually
dependent contracts, event relationships, and recursively composed systems are legitimate. A
resolver must detect each strongly connected group, resolve it as one unit, and either establish a
finite, version-compatible dependency closure or reject the generation with an explanation. A
cycle must never cause unbounded descriptor expansion or make the result depend on traversal order.

Several graphs must not be collapsed into one vague `dependency` relation:

- an acquisition graph states which descriptors or artifacts must be available;
- a contract graph states which provided and required Component contracts must be satisfied;
- an establishment graph states which declared inputs, external resources, inert endpoints, and
  bindings are needed for each Component to report Ready;
- an interaction graph states which Active peers may interact after the release barrier; and
- a source or build graph is an implementation concern and may impose stronger acyclicity rules.

A cycle in the acquisition or contract graph is acceptable when its complete set can be resolved.
A cycle in ordinary interaction requirements is also acceptable and imposes no component-by-
component startup order: every member establishes behind the closed gate, the Host validates the
strongly connected group, and the group is released together.

A Relational Initialisation protocol may also be cyclic and may run peers concurrently, but its
declared completion and failure rules must make progress observable and bounded. It must not depend
on accidental traversal or call order. An undeclared message, a lifecycle Operation no peer may
invoke, or a circular sequence of `wait until the other is Ready` conditions prevents the group
from reaching Ready.

A Component that requires an ordinary application interaction with a same-group peer in order to
report Ready has placed the interaction in the wrong phase. It must express the exchange as a
declared Relational Initialisation Operation, defer it until Release, obtain the input from an
already-Active service outside the restart scope, or participate in explicitly ordered activation
groups. Ordered groups are structural composition, not an implicit consequence of graph traversal;
their cross-group readiness dependencies must themselves admit a deterministic order and declare
partial-release and rollback behaviour.

If a group cannot reach Ready while its interaction gate is closed, activation is impossible and
must fail before release. This is a phase error or an unsatisfied establishment requirement, not a
reason to prohibit cyclic Component composition.

This policy permits cycles; it does not promise that every cycle is meaningful. Implementations
must produce deterministic cycle diagnostics and may reject cycles whose declared lifecycle they
cannot honour. Brontide conformance of a Component graph does not require an implementation's
source projects or package build graph to become cyclic.

## Attributes and recursive definition Constraints

An **Attribute** is a value exposed through a specified Brontide Operation and used declaratively to
describe, select, constrain, or compare an architectural item. Attribute is a provisional general
composition term, not a ninth Base term and not an ambient metadata bag.

An Attribute reference identifies the exact Operation to execute, the vocabulary or Extension
version and required Declared Fragments under which that Operation is understood, the result Shape
and version, and the path within that result. Executing the source Operation normally requires
presentation and evaluation of a Capability. A declaration MUST NOT write merely
`Locality: Local`, because that omits who
defines locality, from whose perspective it is reported, how it is obtained, and what Shape gives
the value meaning.

For example:

```
attribute:
    source-operation: Example:Storage.Topology.Describe
    source-vocabulary-version: 2
    result-shape: Example:Storage.Topology.Description 2
    result-path: locality
    authority: Capability permitting Example:Storage.Topology.Describe

where:
    value in {Local, Nearby}
```

The Capability is a particular target-recognised grant under §10, not a free-floating interface
name. The declaration states which Operation must be callable and which result is constrained;
binding or activation still establishes the actual Actor and Capability relationships.

Attribute sources SHOULD describe sufficiently stable expectations for composition, such as an
advertised locality relationship, durability class, residency, expected-latency class, or supported
storage Form. An Operation may expose a dynamic observation, but one changed measurement does not
implicitly recompose, rebind, or migrate a system. Attribute-constrained bindings are resolved
exactly once, at composition or activation resolution. The resolver evaluates Definition
Constraints against Attribute values obtained at that moment and records their effective values
and provenance in the resolved definition. A later Attribute change never invalidates, rebinds,
or migrates an active binding; reacting to change is not a binding semantic and belongs to
Routers and future lifecycle policy. It is the composition author's responsibility to constrain
sufficiently stable Attributes; the recorded resolution is the guarantee record.

A **Definition Constraint** is a Shape-typed declarative predicate used for Parameter validation,
Attribute requirements, provider selection, Store binding, or another composition decision. It
reuses the word Constraint deliberately but does not alter the Base authority algebra (§10.1): when
such an expression is carried by a Capability, Delegation still conjoins it with the derivation
chain and it can only narrow effective authority. Outside a Capability it selects or validates; it
does not grant authority.

The candidate expression model is recursive:

```
ConstraintExpression :=
      AtomicConstraint
    | AllOf(ConstraintExpression...)
    | AnyOf(ConstraintExpression...)
    | Not(ConstraintExpression)
```

`AllOf` and `AnyOf` may contain atomic expressions, other `AllOf` groups, other `AnyOf` groups, and
`Not` expressions without a fixed architectural nesting limit. Empty logical groups are invalid.
The base candidate operators include equality and inequality, ordered comparison, range, set
membership, containment, subset, intersection, and logical composition. The referenced Shapes
determine which comparisons are meaningful; a string does not acquire numeric ordering merely
because a parser accepts `<`.

Evaluation with unrecognised atoms is defined once, structurally: an unrecognised atomic
Constraint anywhere within a composite expression makes the entire expression unevaluatable.
Unevaluatable never resolves to a truth value — `Not`, `AnyOf`, and `AllOf` never convert an
unrecognised atom into `false` and then reason from it; naive per-atom fail-closed evaluation
would otherwise make `Not(unknown)` evaluate as satisfied, a privilege-escalation path. In
authority context — the expression carried by a Capability — an unevaluatable expression causes
denial of the Execution under §10.1, with no partial credit for recognised branches. In
selection context — composition, Store-role requirements, provider selection — an unevaluatable
expression is unsatisfiable and the candidate is excluded; a resolver SHOULD record the
exclusion and the unrecognised atom in its explanatory record.

For example:

```
where:
    AllOf:
        - durability >= Durable
        - AnyOf:
            - AllOf:
                - locality == Local
                - expected-latency < 50ms
            - AllOf:
                - locality == Remote
                - AnyOf:
                    - residency-regions contains CZ
                    - administrative-domain == UserControlled
        - Not:
            exposure in {Public, Untrusted}
```

A resolver SHOULD preserve which branches and atomic predicates matched, which Attribute providers
supplied the values, and which values came from defaults. This explanatory record is part of Brontide's
larger legibility direction, not permission to expose confidential Attribute values without
authority.

## Selection characteristics through Attributes and the local/remote projection

A Component contract says what semantic and authority relationships a Component supports.
**Selection characteristics** describe the operational circumstances under which a particular
Component or binding may be suitable. They allow selection among contract-compatible Components
at a resolution finer than `local` or `remote`.

Selection characteristics may include:

- topological placement and proximity relationships;
- authority-domain, isolation, and data-residency boundaries, including supporting attestations;
- measured or promised latency, jitter, bandwidth, and throughput;
- monetary cost, energy cost, or resource consumption;
- capacity, current load, admission requirements, and queue limits;
- availability, maintenance state, reliability, and correlated failure domain; and
- required hardware, accelerators, devices, or environmental conditions.

This list is not a ratified catalogue. A portable selection characteristic is expressed through a
versioned Attribute source and Shape, including units, reference perspective, and interpretation,
so that a locality, latency, price, capacity, or distance is not reduced to an ambiguous scalar
(§16). Definitions normally constrain stable declarations or expected profiles. Time-bounded
claims, current observations, estimates, and policy-derived values remain possible, but their form,
source, and freshness stay visible and their change does not silently trigger rebinding.

A Component descriptor may declare the Attribute-source Operations through which characteristics
about its Component are obtained, but self-description is a claim, not proof. An Actor realised by
a Host may measure latency, a guardian Actor may report current capacity, a deployment Actor may
supply topological placement, and an Identity or Distributed mechanism may attest a boundary. Each
value remains attributable to the Actor and Operation responsible for asserting or observing it.
Selection policy decides which claims it accepts and may require independent verification.

Topology is relational and observer-dependent, not one universal graph (§16.6). The same Component
may be near one Actor and distant from another; a network service may remain inside one authority
domain, while an in-process Component may cross a separately protected domain. `Local` therefore
does not imply low latency, low cost, shared trust, or high availability, and `remote` does not
imply their opposites.

This does not make the `local`/`remote` distinction meaningless or undesirable. Implementations
MAY collapse the richer selection surface to those labels for user interfaces, summaries,
discovery views, diagnostics, and coarse policy defaults. A concise interface often should. Such a
label is an intentionally lossy, observer-relative projection: it MUST NOT silently imply latency,
trust, authority-domain, cost, capacity, availability, or failure guarantees that its projection
rule does not define. A Profile or implementation relying on the distinction for behaviour SHOULD
state how the labels are derived and from whose perspective. Architectural selection and
interoperability claims remain grounded in the underlying characteristics when the distinction
matters.

Brontide intentionally does not define **Remote Service** as a separate Component category. A service
reached through a network is a Component whose current binding has particular transport,
topological, authority-domain, latency, cost, and failure characteristics. A library, device,
service, cluster, and data centre may implement the same Component contract while exposing very
different selection characteristics.

This intentionally brings hot swapping, device replacement, service failover, and data-centre
cutover into one composition model. The concepts are not identical: remoteness alone does not make
a Component hot-swappable, and a hot-swappable Component need not be remote. They converge only
when a Hot-swap Host, Slot, Class, and conforming Component establish the replacement contract
defined below.

Hot-swap Class membership establishes semantic and lifecycle eligibility. A Host's policy may
further filter eligible Components by their current selection characteristics without changing
their Class conformance. Failure to meet a particular latency, cost, topology, capacity, or
availability policy makes a Component unsuitable for that binding at that time; it does not by
itself make the Component semantically non-conforming. A later Attribute change does not replace an
active binding unless a declared lifecycle policy or Router reacts to it.

A **Component binding** associates a Component with a surrounding composition at its declared
contract boundary. Binding does not supply Capabilities implicitly and does not change the
canonical names or meanings of the bound contracts. The binding model distinguishes establishment
time from replacement guarantees:

- A **statically bound Component** has its participation fixed before the containing composition
  becomes active, whether by compilation, linking, firmware image construction, configuration, or
  deployment or pre-activation resolution at startup.
- A **runtime-bound Component** may be selected or attached while the containing composition is
  active through a declared runtime-open Composition Port or equivalent resolved boundary. Runtime
  binding alone does not imply that the Component can later be detached or replaced.

## Work in progress: the default portable binding

Composition requires a paved road as well as permission for specialised machinery. If every host
invents its ordinary seam independently, Components are composable only after bespoke integration;
if Brontide mandates one object model or call mechanism, ordinary composition pays for generality it
may not need. The proposed **Brontide Portable Binding** is therefore a first-party default binding
for general-purpose Component interchange, not the implementation model of Brontide Base.

An implementation or Component does not become non-conforming to Base merely because it does not
support the Portable Binding. A host or Component claiming support for a particular Portable
Binding version must preserve that version's complete observable contract. Authors remain free to
realise it through generated types, handwritten code, compiler lowering, static tables, reflection,
native interoperation, or another mechanism. The portable contract must never require a common
language object model or runtime library.

A **Binding Plan** is the scoped agreement established for one Component binding. It may be
constructed at compile time, link time, deployment, activation preflight, or runtime. It may exist
as explicit data or be compiled completely into direct calls and static storage. A Binding Plan
establishes, as applicable:

- the negotiated Operations, Shapes, versions, required Declared Fragments, and canonical
  projection rules;
- the Actor endpoints and how Capabilities are presented, represented, or continued without
  becoming shaped ambient authority;
- whether each interaction is a call, asynchronous exchange, or Flow;
- the representation selected for each boundary value;
- memory domain, allocation, alignment, mutability, ownership, borrowing, lifetime, and release;
- readiness and completion synchronisation;
- framing, batching, buffering, backpressure, delivery, and admission limits; and
- failure, withdrawal, termination, and replacement-generation behaviour.

The proposed portable binary realisation uses a length-delimited framing layer and a restricted,
schema-guided CBOR representation for ordinary inline values. Contract establishment exchanges
canonical names, Shape and Fragment identities, versions, and authority requirements. The active
binding may then assign compact binding-scoped numeric identifiers to those canonical identities.
Such identifiers have no portable meaning outside their Binding Plan and never replace canonical
identity in manifests, signatures, persistence, or conformance claims.

Schema-guided CBOR is not a collection of self-describing JSON-like objects. Once an Operation and
its input Shape are established, each Item need not repeat the Operation name, Shape name, field
names, or full descriptor. Shape remains the type system; CBOR is one portable representation of a
value already governed by that Shape. The exact CBOR subset, scalar mappings, canonicalisation,
field and Fragment identifier rules, bounds, and treatment of unknown preserved data remain to be
defined and measured. JSON and BSON may be diagnostic, logging, persistence, or alternative
binding encodings, but neither defines the Portable Binding's semantics.

The same binding supports two default payload forms:

- an **inline shaped value**, owned by the receiving interaction and encoded through the portable
  representation; and
- a **referenced shaped resource**, which leaves bulk or device-specific data in an explicitly
  described buffer, memory domain, stream, device allocation, or other future `Resource`, while
  carrying the representation, access, ownership, lifetime, integrity, and synchronisation needed
  to use it safely.

The referenced form is part of the seam, not a bypass around it. A video frame, tensor, command
buffer, or bulk record may cross the portable binding as a compact typed reference while the data
remains in shared, device-local, registered, or otherwise suitable memory. A sender and receiver
that do not share a compatible representation may use an attributable adapter or the inline
fallback within declared size and resource limits. They must reject the binding or Item when the
fallback is unsupported or violates a required cost, latency, copy, memory-domain, or admission
constraint; a catastrophic implicit copy is not successful negotiation.

A specialised binding may replace the physical realisation without replacing the Component
contract. A generated direct call, function table, fixed-size ring, shared-memory queue, native ABI,
device queue, or network transport may lower the same Binding Plan. Negotiation and canonical-name
resolution occur before the hot path. A conforming high-rate realisation need not allocate a generic
value, perform string lookup, serialise CBOR, or materialise a complete Interaction for every Item.
Conversely, an ordinary seam may use the same Binding Plan machinery and select a direct inline
call; the portable design is not intentionally a slow path.

Shared representations require stronger rules than copied values. A Binding Plan must identify an
immutable borrowing interval, exclusive ownership transfer, copy-on-submit rule, versioned
integrity rule, or another mechanism preventing mutation after validation. It must also define
release and completion so a producer cannot reuse storage while a consumer or device still holds
it. A buffer, device, or session handle is a Resource reference unless the authority domain
explicitly recognises it as a Capability representation; structural possession alone grants no
authority (§16).

## Where mapping lives

Brontide Base defines canonical Shape identity, compatibility, projection, and authority semantics.
It does not contain or require a universal **mapping engine**. The mechanism that maps private
language values, numeric binding identifiers, CBOR values, native layouts, and resource references
to the already agreed Brontide contracts belongs to the Component binding realisation and normally
runs in host or generated binding machinery.

This representation mapping may be a compiler-generated function, a static table, a trusted host
subsystem, or a separately replaceable Component. Making it a Component is useful when it is
independently selected, shared, inspected, metered, remote, or hot-swapped; it is not required when
the same work can be erased into a direct call. In either form, a representation mapper may not
invent Shape compatibility, authority, provenance, or guarantees absent from the declared
contracts.

Representation mapping must be distinguished from **semantic adaptation**. Mapping a C# record,
an F# record, CBOR fields, and a Rust structure that all realise the same Shape preserves one
contract. Translating between differently named Shapes or Operations, changing units, filling
meaningful missing information, dropping required semantics, or interpreting an authored native
contract as a standard one changes the contract. Such work must be exposed as an explicit Adapter
Component or attributable declared transformation with its own provided and required contracts,
costs, failure behaviour, and ordinary Capability requirements. A hidden mapping engine must fail
binding rather than silently manufacture semantic interoperability.

A **Replacement Slot** is a declared Component binding at which one occupying Component may be
substituted for another under a defined lifecycle. Replacement may require quiescence or restart of
the containing composition unless a stronger guarantee is declared. A restart may be scoped to
that containing composition when it has a declared independent lifecycle; replacing a Component
does not inherently require rebooting a whole device or environment. A slot is a logical composition
boundary; it does not imply a physical connector, memory location, process boundary, or loader.

A **Hot-swap Slot** is a Replacement Slot whose active binding may change without terminating the
Component that owns the slot or its containing composition. A **Hot-swap Host Component** is a
Component that exposes one or more Hot-swap Slots. The Host defines each Slot's boundary, declares
what may occupy it, and coordinates or delegates activation and cutover. A Component may be a Host
for Components inside it while itself occupying a Hot-swap Slot at a larger composition boundary.

A **Hot-swap Class** is the declared conformance class of Components eligible to occupy a
particular kind of Hot-swap Slot. It is defined by a required Component contract, dependency
closure, and replacement protocol. It is not a source-language class, inheritance relationship,
package family, or set inferred from similarly spelled names. One Hot-swap Class may have many
independent implementations, and one Component may implement more than one Hot-swap Class.

A **Hot-swappable Component** is a Component that declares conformance to a particular Hot-swap
Class and fulfils that Class's candidate-side replacement obligations. Hot-swappability is
therefore relative, not universal: a Component may be hot-swappable in a Slot accepting one Class
and ineligible for a Slot requiring another, even when both Slots expose some of the same
Operations.

Interchangeability and hot-swappability are distinct. **Interchangeability** is a compatibility
relationship between Components under declared contracts. **Hot-swappability** is an operational
claim made jointly by a Host's Slot, its accepted Hot-swap Class, and a Component conforming to that
Class. A Component can be interchangeable but not hot-swappable, and a Hot-swap Slot can support
hot swapping only among Components in its declared Class.

Each Hot-swap Slot MUST declare the Hot-swap Class it accepts. Each Hot-swappable Component MUST
declare the Class or Classes it implements. Matching identifiers alone are insufficient: the Host
MUST establish compatible contracts and dependency closure before activation. The Slot and Class
contracts together MUST identify the replacement boundary and define or explicitly disclaim:

- whether compatibility is established by construction or runtime negotiation;
- the identity of Actors before and after replacement (§6.5);
- how authority is established for successor Actors and withdrawn or bounded for displaced Actors,
  without silent Capability transfer;
- state transfer or deliberate state loss;
- the handling of admitted and in-progress Executions and Flows;
- interruption, quiescence, failure, and rollback behaviour; and
- the cutover point at which the old binding ceases and the new binding becomes active.

Canonical Brontide contract names do not encode binding time. A standard Shape remains standard when
loaded dynamically, and an authored Shape remains authored when compiled statically. When a
portable Component or Hot-swap Class identity is exposed, it follows the authored-name rules of
§22, such as `Bob:PointerDriver`; source-language symbols and package names have no Brontide standing
unless they are explicitly mapped to such an identity.

The term **plugin** remains valid implementation or product terminology for a host-managed,
runtime-bound Component. It is not the architectural category: a plugin need not be detachable or
hot-swappable, and a hot-swappable mouse, remote service, or data centre need not be a plugin.

For example, a workstation or receiver may be a Hot-swap Host Component exposing a device Slot. A
mouse conforming to the Slot's Hot-swap Class is a Hot-swappable Component while exposing several
Actors and standard Input contracts. At another scale, a traffic-management Component may host a
Slot whose eligible Components are entire data centres. Each data centre may itself be a composite
Host containing thousands of Components and Actors. Replacement is evaluated at the declared Slot,
Class, and contract; Brontide does not privilege the smaller scale.

This section records the composition direction and vocabulary for experimentation. The exact
Component descriptor, Slot and Class representation, Binding Plan, Portable Binding framing and
CBOR profile, resource-reference contract, binding occurrence model, negotiation mechanism, and
conformance contract remain to be established through Reference/Minimal interchange and systems at
different scales. Until then, Component and the proposed Portable Binding do not enlarge Brontide
Base or imply a universal runtime loader, mapper, or transport.

## Mediation (recorded direction)

Router-shaped mediation appears repeatedly in Architecture 0.7: the Event mediator delegates
fan-out and fan-in under explicit rules with provenance preservation (§19.2); the
traffic-management example hosts data-centre-scale Components (§18.1); the mediating Actor of
§6.9 and the guardian of §26.1 front contested and bounded resources; and selection machinery
(Attributes, Definition Constraints) already operates at composition resolution and at Slot
replacement. Per-interaction mediation is the remaining cell of that matrix. This section
records the common structure. It is a direction, not a ratified mechanism.

**Mediation** is a declared relationship in a composition in which one party presents a
declared contract while interactions are delegated to backing providers under explicit,
inspectable rules. The name follows the Brontide relationship-noun grammar (Delegation,
Interaction, Enrichment); no architectural "Mediator" participant category exists.

### Declared relationship, free realisation

Mediation joins the established pattern of Enrichment, the Binding Plan, and representation
mapping: the declaration is architectural, the realisation is not. A Mediation may be realised
by a dedicated Component — the paved road: reusable, replaceable, independently selected,
metered, hot-swappable — by host or composition machinery, or erased into static construction.
An embedded static fan-out table is a Mediation realised by construction. The following
invariants hold regardless of realisation:

- Mediation is declared, never invisible. The resolved composition records that a binding is
  mediated and under which rules; silent load-balancing behind a believed-direct binding
  violates §6.14.
- The endpoint guarantees of a Mediation are its own declarations, not those of any current
  backing provider, and it must not declare a guarantee its rules cannot uphold across its
  declared backings and fallback behaviour.
- Provenance is preserved; origin is never laundered.
- Deputy discipline (§13.6) applies to whatever realises the Mediation.
- Affinity semantics are declared: per-interaction, per-Flow, or sticky.
- A residue obligation is declared — what the mediation's decision function must remember.
- Mediation is scoped to contracts with declared substitutability (§21.2).
- A Mediation over a Provider Set preserves each backing member's identity, provenance, failure
  domain, and authority. It cannot manufacture the union of member Capabilities or silently turn
  independent providers into replicas.
- Interposition is enforced by authority topology, not discovery. A Mediation is effective for
  exactly those requesters whose granted Capabilities designate the Mediation endpoint as
  target (§10.2); shared contract shape never couples, only the grant does. The mediating
  party holds the direct Capabilities to the backings — the §6.9 pattern, and the membrane
  lineage of §31. A direct grant to a backing bypasses the Mediation; that is a visible,
  attributable policy decision in the delegation graph, not a discovery failure. Discovery
  remains inert: seeing that a backing exists confers no reachability, and binding supplies no
  Capabilities implicitly (§18.1).
- Discovery is tiered. At consumer tier, discovery presents the Mediation endpoint as the
  provider; backing visibility is a management-authority concern, generalising the rule that
  management tooling may inspect that a provider is a Router and examine its topology (§18.2).

### Direct topology and the mediation threshold

The threshold is semantic responsibility, not a count of boxes. A `1..1` binding may be direct. A
Provider Set with distinct exposure may also use several direct Binding Plans when the consumer's
declared contract intentionally addresses each member and no composition machinery selects, merges,
duplicates, arbitrates, or hides them. Multiplicity alone does not require a mediator.

Mediation is mandatory in the resolved composition when one logical binding introduces a topology
decision on behalf of its consumer: Selection, fallback, load balancing, Distribution, Aggregation,
Arbitration, affinity, ordering, backpressure, membership masking, or topology-owned failure and
recovery. A Component cannot make that relationship direct merely by hiding the decision in private
code. If it presents the logical endpoint while delegating interactions to backings, it is realising
the declared Mediation and inherits its provenance, deputy, explanation, and authority obligations.

The mandatory element is the architectural Mediation boundary, not necessarily a separately loaded
Component. A small immutable mapping may be erased into host or static construction. A dedicated
Router, Distributor, Aggregator, or Arbiter SHOULD be used when the relationship owns mutable
membership, shared policy, persistent affinity or other residue, queues or backpressure, independent
authority, metering, recovery, or lifecycle. Erasure remains valid only when the host preserves the
same visible declaration and accepts the corresponding trusted-computing-base responsibility.

For example, a display or rendering Component need not know whether its output reaches one screen,
several mirrored screens, or a remote surface. Direct binding handles one addressed display;
Distribution handles fan-out. Presentation or Workspace policy may still decide layout, cloning,
focus, user association, and which logical surfaces exist. The eventual boundary between that UX
orchestration and topology mediation remains open; it must not be silently assigned to the renderer
or treated as generic Distribution semantics.

### Trust surface

A dedicated mediating Component can hold only the Capabilities to its backings — a small,
auditable, least-privilege deputy. Erased mediation is absorbed into the host's trusted
computing base (§28). Both are legitimate; the declaration states which, because they read
differently under security review.

### Species

The species are distinguished by cardinality and characteristic obligation:

- **Selection** — one requester, one of N substitutable providers. The residue obligation
  ranges from total (storage findability, where routing participates in Dataset identity) to
  none (stateless compute). Instances: storage routing, compute and accelerator routing,
  identity-provider selection, eligible-Actor resolution.
- **Distribution** — one occurrence, all of N entitled receivers. The obligation is delivery
  and consistency. Instances: Event fan-out, Mirror, Backup. The storage model already
  separates Selection from Distribution — Router versus Mirror/Backup — without naming the
  split.
- **Aggregation** — N providers or sources presented as one logical provider or occurrence stream.
  The obligation is source identity, ordering, fairness, backpressure, and member-arrival,
  departure, and failure semantics. Instances: several input devices feeding one input Flow,
  multi-source telemetry, and Event fan-in. Aggregation combines supply; it is distinct from
  Arbitration, which governs competing demand for one bounded target.
- **Arbitration** — N requesters converging on one bounded resource or provider. The
  obligation is ordering, precedence, and fairness state. Arbitration absorbs two existing
  patterns: the §6.9 mediating Actor fronting a contested resource, and the §26.1 guardian
  fronting human attention. Conflict semantics remain Domain Vocabulary property (§6.9);
  Arbitration composes with admission (§26).

The data/operations distinction does not produce two mediation mechanisms — a Store Router
already routes Operations, because a Store is defined through the Operations it exposes.
Data-ness appears as the residue-obligation dimension, not as a separate architecture.

### Component categories

Consistent with the existing agent-noun tier for Component kinds (Host, Adapter), a dedicated
Component realising a Mediation takes a category name: a **Router** realises Selection in any
domain — the storage Router is the first Router rather than the definition; a **Distributor**
realises Distribution; an **Aggregator** realises Aggregation; an **Arbiter** realises Arbitration.
Erased realisations carry no category noun.

### Declaration level and status

Mediation declarations are admitted at composition level only — a property of a binding.
Whether a Component may additionally require Mediation properties of its environment ("binds
only through a Selection with sticky affinity") remains open; it is more expressive but in
tension with §6.12.

The storage Router remains the first instantiation, and ratification waits on Reference/Minimal
storage evidence. The Event mediator is evaluated against this contract when Event Distribution
is drafted — it combines Distribution, Aggregation, and Selection-like subscription filtering, and
those roles may deserve separation there as they already have in storage. An Actor legitimately
holding both a direct and a mediated grant experiences Arbitration as advisory, which is
correct under §6.9 — "everything flows through the Arbiter" is therefore a claim about grant
policy, verified in the delegation graph, not a structural guarantee.
