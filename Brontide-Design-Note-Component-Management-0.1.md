# BRONTIDE

## Design Note: Component Management and Distribution

**Status:** Work-in-progress design note, version 0.1
**Current architecture context:** [Brontide Architecture 0.8](./Brontide-Architecture-0.8.md),
§18.1, §24, §28, and §33
**Related direction:** [Composition and Components](./Brontide-Design-Note-Composition-0.1.md)

**Scope:** Records a provisional management and distribution direction. Nothing in this note
enlarges Brontide Base, ratifies a Component package format, requires dynamic loading, or makes a
particular manager, repository, marketplace, trust service, or operating-system layout conformant.

References of the form §N refer to the Brontide Architecture specification.

---

Component management joins two concerns that must cooperate without being confused:

1. **logical management** selects Components, resolves composition generations, validates their
   contracts and authority requirements, and coordinates activation, retirement, and rollback;
2. **physical distribution** describes, obtains, verifies, stages, retains, and removes the
   artifacts from which Components can be realised.

A general-purpose system will normally need both. An embedded image may erase both into its build.
Neither belongs in Brontide Base.

The recorded specification direction places this facility beneath a provisional **General-Purpose
System Profile**, not inside Base and not inside every Component. That Profile should require an
inspectable Composition implementation and Component-management facility and is expected to include
the separate Discovery extension where users, devices, or automation can introduce candidates from
changing sources. The Composition extension itself remains implementable over a closed authored
candidate set without Discovery.

A provisional **Static Embedded Profile** requires no manager, Discovery protocol, loader, or
runtime resolver. Its fixed image is composed in the ordinary engineering sense, but need not expose
that construction as a portable contract. Such a leaf may still be passively incorporated by a
larger Host that claims Composition and Discovery; the Host's Profile obligations do not transfer to
the leaf.

## Terminology

A **Component Manager** is machinery that coordinates some or all of the lifecycle from discovery
through generation activation and retirement. The role may be realised by host machinery, a build
tool, one or more Components, or a private facility inside a boxed composition. Brontide does not
assume one global manager, one process, or one user interface.

A **Component Source** supplies Component descriptors, distribution artifacts, or evidence about
them. A source may be a local directory, removable medium, build output, peer, network repository,
organisation catalogue, or another protocol. Sources are arbitrarily extensible subject to local
authority and admission policy.

A manager may consult any number of Component Sources at the same time. A **source endpoint** is the
place from which a descriptor or artifact was obtained; a **publisher** is the authored authority,
person, or organisation responsible for it. They are not interchangeable. Several endpoints may
mirror one publisher's work, and one endpoint may host unrelated publishers. A preference for the
requesting Component's creator is therefore **publisher affinity**, not source affinity.

A **marketplace** or **storefront** is a human-facing discovery, evaluation, and acquisition
experience. It may aggregate several Component Sources or itself expose one. It is not the manager
and does not gain authority to activate what it advertises.

The phrase **Component Store** should be avoided in architectural text. `Store` already has a
specific persistent-information meaning in §18.2, while a commercial store is only one possible
front end or source. Product interfaces may still use familiar words such as store or marketplace
when their meaning is clear.

A future user interface should be free to make a local, organisational, or remote source look like
a familiar software store. The portable presentation projection should be source-neutral and may
include display name, publisher, description, imagery references, categories, available versions,
compatibility, evidence and policy status, installed or pending state, dependencies, and
alternatives. The initial fake local source should populate the same projection with deterministic
fixture data. This creates a realistic user experience and test seam without claiming a real
network service, commercial relationship, review system, or trust decision.

A **Component Package** is a provisional physical distribution unit. A package may contain one or
more Component realisations, descriptors, Shapes, vocabularies, resources, and verification
material; one Component may also require several packages. Package identity, Component identity,
Actor identity, and authored Brontide contract identity are distinct.

A contract role, a Component definition or realisation, an activated Component occurrence, and the
Actor endpoints it establishes are also distinct. Several definitions may provide the same role,
and one definition may have several simultaneously active occurrences.

A **Provider Set** is the resolved, identity-preserving set of provider bindings satisfying one
requirement in one binding scope. A requirement declares minimum and maximum cardinality, with
`1..1` as the ordinary compatibility default. Binding scope prevents a convenient system default
from becoming a global monopoly over a contract: applications, users, sessions, Workspaces,
tenants, Datasets, devices, or authority domains may bind different providers concurrently. A
provider occurrence may be shared only when its isolation, lifecycle, and authority contracts allow
it.

Provider Sets have **distinct exposure** when members remain separately addressable. They have
**mediated exposure** when a declared Selection, Distribution, Aggregation, Arbitration, or domain-
specific Mediation presents a logical endpoint over them. Multiplicity alone never merges members,
makes them replicas, assigns fallback order, or unions their authority. Static membership is fixed
by the generation; runtime attachment and detachment require an explicit lifecycle contract and do
not follow merely from discovery.

Direct Binding Plans remain appropriate for a `1..1` requirement and for distinct, member-addressed
sets. A logical endpoint that selects, distributes, aggregates, arbitrates, masks membership, or owns
topology-wide failure policy requires declared Mediation. The manager records that boundary even
when a trivial implementation erases it into static or Host machinery. It should prefer a dedicated
mediating Component when the relationship owns policy, residue, queues, authority, recovery, or an
independent lifecycle.

A **Composition Region** is a recursively nested composition boundary with its own resolved and
activated generation. A **Composition Port** is a parent-declared attachment boundary through which
a child Region or Component may be composed without silently changing the parent generation. The
Port states the permitted contracts, cardinality, imports and exports, authority ceiling, topology
requirements, lifecycle, failure and rollback behaviour, and whether it is sealed, activation-open,
or runtime-open.

Every attachment occurrence also has a local **Topology Node**. Attributable **Topology Relations**
associate Components, Actors, resources, Regions, and Ports with that node. Relations such as
`PartOf`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`, `SharesPowerDomain`, and
`SharesFailureDomain` remain distinct facts: no one relation grants authority or implies all the
others.

## Open acquisition, strict activation

Brontide should place few architectural restrictions on where a Component may be obtained. A user
or authorised automation may add sources, import a local package, receive an artifact from a peer,
or select an entry offered by a marketplace. Openness of acquisition is not openness of authority.

Every descriptor, signature, origin statement, attestation, review, reputation score, and
compatibility claim is attributable evidence. Under §24, the local authority domain decides which
evidence it accepts and what, if anything, follows from it. Acquiring or staging an artifact grants
it no Capability, establishes no Actor as trusted, and causes no code to run. A manager that treats
source presence or a successful download as activation authority has made an over-broad local
admission decision.

This separation permits both permissive and restrictive environments. One may allow unsigned local
experiments after an explicit user decision; another may require an organisational signature,
reproducible-build evidence, review quorum, and hardware attestation. The distribution mechanism
carries evidence and records the policy decision. It does not define one universal trust policy.

## Standardised discovery and dependency preference

A Component requirement names the canonical contract, versions, Constraints, lifecycle role,
binding scope, Provider Set cardinality, and distinct or mediated exposure it needs. It may
additionally declare one or more **Preferred Providers**. A preference is an authored selection
hint, not a new dependency strength: it cannot grant authority, establish trust, suppress an
incompatibility, force acquisition, or replace an occupied binding by itself. A requirement that
truly accepts only one authored provider states that stronger compatibility requirement explicitly
rather than disguising it as preference.

Discovery should use a portable semantic exchange even when its physical implementation is a local
table. A **Discovery Query** identifies the required contract and version range, target environment,
Definition Constraints, lifecycle role, requester and publisher, declared preferences, and any
existing binding, containing Region and Port, and topology requirements. Each source returns
attributable candidate records containing identities,
provided contracts, versions, target support, publisher, artifact locations, and available
verification evidence. A future wire protocol may carry these records; the semantic fields and
explanation obligations should not wait for that protocol.

For a `1..1` requirement or occupied Slot, a compatible existing binding is stable by default. The
manager keeps it unless the user or an authorised replacement policy chooses otherwise. If a newly
selected Component prefers another provider, the proposed stack highlights the alternative, the
Component that expressed the preference, and the reason, but does not silently replace the
occupant. A Provider Set may admit another member without displacement when its upper bound,
sharing rules, and exposure contract allow that. An incompatible occupant does not satisfy the
requirement and is reported as a conflict rather than treated as occupied success.

For each unsatisfied lower-bound position, candidates are considered in this order:

1. an explicitly Preferred Provider declared by the requesting Component;
2. an admissible compatible implementation from the same publisher as the requester;
3. an admissible **generic implementation** whose primary declared purpose is the canonical required
   contract rather than one requester's authored ecosystem; and
4. any other admissible compatible implementation.

The tiers are defaults, not a trust hierarchy. Local policy may exclude or demote a candidate for
integrity, origin, trust, platform, version, authority, resource, cost, or other declared reasons.
Publisher affinity may improve expected integration but does not prove safety or quality. A
`generic` claim likewise requires attributable contract evidence and creates no privileged
implementation class.

Within a tier the manager applies deterministic local policy and records the tie-break. If the
explicitly preferred package is absent, discovery continues through publisher-affine, generic, and
other candidates rather than failing prematurely. Sources may be queried incrementally for
efficiency, but the explanation records which sources were consulted and must not confuse the
repository serving a package with its publisher.

Resolution produces a **Proposed Stack** before acquisition or activation. It presents at least:

- the selected root Components and recursively required roles;
- each role's binding scope, cardinality, sharing requirement, and distinct or mediated exposure;
- occupied provider occurrences that will be retained and whether they are shared or private;
- the preselected best candidates for each unfilled required position and why they ranked first;
- the resulting Provider Set membership, activation occurrences, member assignments, and any
  mediator and its topology, including why the binding is direct or mediated and whether the
  mediation is dedicated or erased;
- the containing Composition Region and Port for every new occurrence, whether the Port permits
  runtime composition, and any boundary constraint that the candidate would exceed;
- proposed Topology Nodes and Relations, their evidence and attribution, and any local refinement or
  rejection of device-supplied claims;
- source endpoint, publisher, compatibility, version, trust and origin evidence, and requested
  authority for each candidate;
- Preferred Providers, the Components that requested them, and whether each preference was used,
  unavailable, inadmissible, or displaced by an existing binding;
- compatible alternatives and conflicts; and
- the resulting restart scope and unresolved user decisions.

A user interface may present this as a store-like installation review with the best candidates
already selected. Policy may permit a one-action or automatic path, but the same proposal and
explanation remain inspectable. Discovery decides what *could* satisfy the composition; local
admission and generation activation still decide what may become active.

## Generational lifecycle

The portable lifecycle is conceptual rather than a required storage schema:

```
discovered → acquired → selected → resolved → prepared
                                                   ↓
Local Initialisation → Interconnection → Relational Initialisation? → Ready ⇢ Active
```

- **Discovered** means that attributable metadata is known. Its claims may be incomplete or false.
- **Acquired** means that immutable artifact content is locally available in a staging area. It is
  inactive.
- **Selected** means that an authorised choice requests the Component for a pending composition.
  Selection changes the next composition, not the active one.
- **Resolved** means that recursive structural closure, contracts, versions, Parameters,
  constraints, Provider Set membership and exposure, provider choices, authority requirements, and
  Binding Plans, Composition Regions and Ports, and topology membership have been checked and
  recorded as a generation.
- **Prepared** is an optional optimisation state in which code, caches, indexes, resources, or
  snapshots needed for activation have been made ready. Preparation does not authorise effects and
  is not proof that activation will succeed.
- **Local Initialisation** means that Components are initialising private provisional state and
  exposing inert endpoint descriptions without same-group relationships.
- **Interconnection** means that the Host is establishing Actor identities, endpoints, resources,
  Binding Plans, and requested local authority while ordinary interaction remains gated.
- **Relational Initialisation**, when declared, means that Components may use only authorised
  lifecycle initialisation Operations against their connected peers; ordinary interaction remains
  gated.
- **Ready** means that every required Component in the activation group has satisfied its declared
  establishment conditions and the Host has validated the complete group. It remains inactive
  until release.
- **Active** means that the generation has crossed its declared cutover and its Actors and bindings
  participate under locally established authority.

Implementations may combine or omit materialised states while preserving their distinctions. A
compile-time image may never persist `discovered` or `prepared`; a desktop manager may expose each
state to the user and retain several generations for inspection and rollback.

## Regional and host-assisted composition

Incremental, or per-partes, composition resolves and activates a complete child generation inside a
Port of an already active parent Region. The parent remains immutable because the possibility and
envelope of that attachment were part of its generation; runtime arrival fills the declared
structural boundary rather than mutating arbitrary parent structure. The same mechanism applies to
an internal optional subsystem, a newly attached device, a downloaded feature, or a remote system.

If the child needs contracts, authority, topology, cardinality, or cyclic participation beyond the
Port envelope, the manager rejects it or proposes a wider parent generation and restart. Attaching
to an empty Port is not itself hot replacement: detaching or replacing an active child additionally
requires declared quiescence, state, failure, and rollback semantics.

A host-assisted composable device carries a sealed bootstrap composition containing at least a
Composition Host and plan verifier, local admission roots and policy, recovery, Channel support, and
the loading and activation mechanism. An outer Host may supply Discovery, candidates, artifacts,
and evidence through authorised operations, but device-local authority ordinarily decides what is
admitted and activated. A mode in which the outer Host owns that decision must be explicit. The
device's internal child generation reaches Ready and crosses its Release before the outer system
activates the boundary that the device exports.

## Two-phase generation activation

Activation is a generation-level protocol rather than a component-by-component startup order. It
has two observable phases, **Establishment** and **Release**. Establishment has named, ordered stages
so Component authors can declare exactly when each requirement applies:

1. **Local Initialisation.** The Host materialises every Component in the activation group. Each may
   initialise private provisional state from declared Activation Parameters, local resources, and
   restored state. It has no ordinary same-group peer interaction. It exposes the inert Actor and
   endpoint descriptions required for the next stage or fails with a declared Outcome.
2. **Interconnection.** The Host establishes Actor identities and inert endpoints, constructs or
   restores Binding Plans, binds resources, evaluates requested local authority, and connects the
   complete group. Ordinary Executions, Events, and Flows remain gated; structural discovery and
   provider selection are already closed.
3. **Relational Initialisation** *(optional).* Connected Components may perform only the lifecycle
   initialisation Operations declared by their contracts, against declared peers and under narrow
   initialisation authority. This stage supports handshakes, negotiated state, schema or protocol
   agreement, and other setup that genuinely requires relationships. It cannot add Components,
   expand authority, or admit ordinary application interaction. Its Operations, Shapes, ordering or
   concurrency, completion condition, retry and timeout rules, idempotence, failure, and rollback
   behaviour are part of the Component contract.
4. **Ready.** Each required Component reports its establishment Outcome. The activation group is
   Ready only when every required member is Ready and the Host has validated the group.
5. **Release.** The Host opens the ordinary-interaction gate. The generation becomes Active and
   normal peer interaction may begin.

The release is one logical barrier, not a promise of clock-level simultaneity. Components must not
rely on which member receives processor time first after it. A Host may use queues, admission gates,
generated direct-call guards, process barriers, or another mechanism, but it must prevent any member
from observing a partially released peer group where the contract promises one activation group.
A distributed or otherwise non-atomic realisation must declare its cutover interval, partial-release
failure semantics, and recovery rather than claiming instantaneous activation.

`Local` in Local Initialisation describes lifecycle isolation from same-group peer relationships,
not necessarily memory, process, security, or physical isolation. Relational Initialisation opens a
separate lifecycle-only gate; it does not open the ordinary-interaction gate early. Both gates must
be enforced by trusted Host or binding machinery rather than voluntary component behaviour.

A portable Component descriptor should expose phase-specific declarations:

- Local Initialisation inputs, outputs, Activation Parameters, resources, state restoration, and
  failure Outcomes;
- inert Actors and endpoints exposed to Interconnection;
- peer contracts, Binding Plans, resources, and authority relationships Interconnection must
  establish;
- permitted Relational Initialisation Operations and peers, their narrow authority, Shapes,
  concurrency or ordering, completion, timeout, retry, idempotence, failure, and rollback semantics;
- the Ready condition and establishment Outcome; and
- Release, quiescence, state disposition, failed activation, and retirement behaviour.

Preparation remains outside this protocol: a prepared artifact or image has no established Actor,
binding, or authority merely because work was done in advance. Local Initialisation begins only when
the Host attempts activation at the declared restart boundary.

## Preflight and cutover

A manager may build the next generation while the current one is active. Before cutover, it should
be able to establish at least:

- finite dependency closure and deterministic provider selection;
- satisfied Provider Set cardinalities, unambiguous binding scopes, activation occurrence and
  sharing decisions, and declared distinct or mediated exposure;
- every logical topology decision represented by declared Mediation rather than hidden behind a
  direct binding, with the dedicated or erased realisation and trust consequence recorded;
- every child generation contained by a declared Composition Port, with its imports, exports,
  authority ceiling, lifecycle mode, and topology evidence within the Port envelope; a need to widen
  that envelope is reported as a parent-generation change rather than accepted implicitly;
- compatible Component contracts, Shapes, versions, and required Fragments;
- all Composition Parameters and the complete structure they create;
- declared Activation Parameter slots, with values pre-obtained where policy permits;
- Attribute and Definition Constraint decisions with provenance;
- package integrity and available trust, origin, signature, and review evidence;
- requested Actor relationships and authority requirements, without treating requests as grants;
- Binding Plans, resource requirements, restart scope, state disposition, and rollback plan; and
- declared absence or failure behaviour for unavailable optional facilities.

This makes activation an establishment-and-release protocol over known structure rather than an
opportunity for hidden recursive discovery. It also permits expensive work to occur before the
restart. The architecture does not require one preparation technique: an implementation might
validate manifests, compile bindings, create an image, warm a cache, reserve storage, or prepare a
process tree. None may establish an Active Actor or perform the new generation's externally visible
effects merely through preparation.

A failed resolution leaves the active generation untouched. A failed activation follows the
generation's declared failure atomicity. Failure before release normally discards the provisional
new group while the previous retained generation remains or is reactivated. Failure during or after
release follows declared partial-cutover and rollback semantics; irreversible migrations require a
stronger, visible policy. Rollback cannot be promised where state or external effects make it
impossible.

## Scoped restart

Restart is a property of a declared composition boundary, not necessarily of a whole machine. A
Component Host, device environment, workspace, user session, service group, process, or complete
system may own an independently activatable generation. A Composition Region may likewise activate,
restart, or roll back a child generation through its Port while the surrounding parent remains
active, when the declared lifecycle and boundary contracts permit that isolation.

A scoped restart declaration identifies:

- the boundary that becomes inactive and the Components and Actors it contains;
- which surrounding bindings remain valid across the restart;
- quiescence and treatment of admitted or in-progress work;
- persistence, migration, or deliberate loss of state;
- Terminus and re-establishment rules for Actors and their authority;
- the activation group, interaction gate, Ready conditions, and release occurrence;
- failure propagation before release, partial-release behaviour, cutover atomicity, and rollback;
  and
- whether the implementation may request a wider restart instead of silently taking one.

Generational replacement is deliberately the ordinary path for user-selected Components. Hot
swapping remains an optional stronger contract for Slots that need uninterrupted containing
compositions. A manager need not implement hot swapping to provide a fluent select-now,
restart-when-ready experience.

## Recursive dependencies and cycles

Selecting a Component may reveal further Component requirements and Composition Parameters. The
resolver expands them until it reaches a finite closure. Activation Parameters are then bound into
slots already declared by that closure. Both operations may happen during one startup or preflight
transaction; their semantic order, not their wall-clock time, distinguishes them.

Logical Component dependencies are not required to form a directed acyclic graph. A resolver must
detect strongly connected groups and resolve each as a unit. It accepts a group only when versions,
contracts, choices, and Parameters have one finite, deterministic result. Unbounded generative
expansion, traversal-order-dependent selection, or incompatible version constraints reject the
generation.

A contract or ordinary interaction cycle does not define an activation order and normally needs no
special bootstrap protocol. Every member performs Local Initialisation, the Host Interconnects the
strongly connected group, any declared Relational Initialisation protocol runs, and the group is
released only after all members report Ready.

A relational protocol may itself be cyclic: peers may exchange handshakes or negotiate concurrently.
Its contract must nevertheless define bounded progress and observable completion rather than rely on
an accidental call order. A member waiting for an undeclared message, a lifecycle Operation that no
peer is permitted to invoke, or a circular sequence of `wait until the other is Ready` conditions
prevents the group from reaching Ready and fails activation.

A Component that attempts an ordinary application interaction in order to become Ready still
violates the phase contract. The author must express the exchange as a declared Relational
Initialisation Operation, defer it until Release, obtain the input from an already-Active Component
outside the restart scope, or declare multiple ordered activation groups. Ordered groups become
explicit composition structure; the manager validates a deterministic group order and their
partial-release and rollback rules.

Failure of any required member to reach Ready prevents release of the entire group. This makes
cycles in post-release interaction unremarkable while ensuring that a hidden eager-start cycle
cannot deadlock activation. A build system's source or project dependency graph remains a separate
implementation concern and may be required to stay acyclic.

## Direction for a distribution standard

A future distribution specification should standardise portable evidence and transaction
boundaries before standardising a filesystem layout. Candidate requirements include:

- a versioned descriptor with distinct package, Component, contract, and publisher identities;
- immutable artifact identity, normally through a cryptographic content digest;
- explicit target environments, provided and required contracts, dependencies, Parameters,
  resources, and requested authority relationships;
- requirement binding scopes, Provider Set cardinalities, sharing rules, exposure and mediation,
  and runtime membership contracts where applicable;
- Composition Region and Port descriptors, including imports, exports, authority ceilings,
  topology requirements, lifecycle modes, and widening rules;
- attributable Topology Node and Relation records that preserve discrete attachment membership
  without treating physical grouping as identity, trust, or authority;
- Local Initialisation, Interconnection, optional Relational Initialisation, Ready, Release, and
  failure declarations;
- canonical dependency requirements, Preferred Providers, publisher identity, and enough discovery
  metadata to construct an explainable Proposed Stack;
- a source-neutral storefront projection suitable for deterministic local fixtures and future
  remote catalogues;
- signatures, attestations, provenance, reviews, and transparency evidence represented as
  attributable claims rather than collapsed into one `trusted` flag;
- deterministic resolution records, lockable generations, and side-by-side versions;
- acquisition and unpacking that cause no ambient-authority installation script to execute;
- effectful installation or migration hooks, where unavoidable, modelled as explicit Operations
  under narrow Capabilities with recorded Outcomes;
- transactional staging, activation, retention, rollback, removal, and garbage collection; and
- information-lifecycle rules preserving §18.2's distinction between removing a Component and
  deleting a Dataset.

Linux package managers are useful operational precedents, but Brontide should not copy the
assumption that installation primarily means writing files into one machine-wide hierarchy. A
package may realise an in-process library, device firmware, a process, a remote service binding, or
a recursively composed environment. The standard should describe identity, evidence, closure, and
lifecycle while permitting native layouts and optimised realisations.

## Implementation evidence direction

The first implementation should be entirely fake and deterministic. It needs no online service,
real package execution, or production cryptography. Fake sources, descriptors, artifacts, evidence,
policy decisions, generations, activation hosts, and failures are sufficient to exercise the hard
mechanisms and produce automated tests.

That harness should prove multiple-source discovery, recursive structural resolution, Parameter
staging, publisher-aware provider ranking, occupied-provider stability, multiple definitions and
active occurrences per role, scoped Provider Sets, cardinality, sharing, distinct and mediated
exposure, Preferred Provider explanations, Proposed Stack presentation, cyclic interaction groups,
Local Initialisation, Interconnection, lifecycle-gated Relational Initialisation, readiness failure,
logical Release, preflight rejection, requested-authority review, immutable generation records,
incremental child generations, Composition Port enforcement, host-assisted device admission,
discrete topology membership, scoped restart, failed cutover, and rollback. The independent-stack plan is
[Component Management Implementation Plan 0.1](./Brontide-Component-Management-Implementation-Plan-0.1.md).
