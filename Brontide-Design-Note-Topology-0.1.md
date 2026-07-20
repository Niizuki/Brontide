# BRONTIDE

## Design Note: Topology Environments and Gates

**Status:** Recorded design direction, version 0.1; not ratified

**Current architecture context:** [Brontide Architecture 0.8](./Brontide-Architecture-0.8.md),
especially §§7, 18.1, 19, 24, and 33.

**Scope:** Records the resolved Environment and Gate direction together with the competing reasoning
that produced it. Nothing in this note adds a Brontide Base term, ratifies the `Topology` extension,
makes an Environment universally identical to a Component, or defines a cross-domain security
protocol.

## Resolution

The architectural question is resolved at this level:

- An **Environment** is an ordinary observer-relative topology grouping. It may be physical,
  virtual, mixed, nested, overlapping, or reconstituted. It carries no security or isolation claim
  merely by existing.
- A **Protected Environment** is an Environment with a declared and enforced protection boundary.
  Within one **Protection Plane**, Protected Environments are laminar: two are disjoint or one fully
  contains the other; they never partially overlap. Environments in independent Protection Planes
  may intersect because they describe different protection dimensions.
- A Protection Plane is identified by its protection dimension and enforcement basis. Two Protected
  Environments claiming the same dimension under the same basis belong to one Plane; laminarity
  cannot be escaped by minting Planes.
- Every covered interaction crossing a Protected Environment boundary passes through a declared
  **Gate**. A Protected Environment with no active Gate is a valid state, not a named term: it has
  no declared external communication and does not interact externally until a Gate becomes active.
- A Protected Environment is architecturally opaque from outside. Its internal identities,
  structure, contracts, topology, and authority information are exposed only through an
  audience-specific **Environment View** provided by a Gate.
- A Gate is the relationship-specific virtual-Component projection of its Environment. Boundary
  Actors—not the Environment itself—hold, accept, derive, or delegate Capabilities.
- Every contract a Gate exports carries exactly one declared **export fidelity**: Direct,
  Deputised, Mediated, Adapted, or Synthetic. Reinterpretation is never presented as exposure, and
  a silent fidelity downgrade is nonconforming.
- Ordinary Environments do not gain an `open`, `transparent`, or `namespace` security class. They
  remain security-neutral. Namespace translation and structural disclosure are properties of a Gate
  and its Environment View.
- Environment, Protected Environment, Protection Plane, Gate, Topology Map, and Environment View
  belong to the future `Topology` direction, not Brontide Base.
- Environment identity is portable at Gate boundaries, but mutual understanding is established only
  by an explicit versioned Topology exchange and Outcome. Recognition, trust, and authority remain
  separate decisions; Profiles state when successful understanding is mandatory.
- A protection boundary's no-bypass claim is graded evidence, never a Boolean. Recognition of a
  peer's Environment reference records an attributable alias relation rather than merging
  occurrences, and continuity across reconstitution or reconnection is a declared lifecycle or
  receiver-owned admission decision, never an inference.

This resolution deliberately stops before Gate grouping, orchestration, concrete descriptor format,
or implementation topology. Those concerns may reuse Composition and Mediation, but they do not
change the model above.

## Question and retained reasoning

Topology began as placement and attachment information. It now appears to have a wider role:

- keeping independently represented parts associated without assuming one physical device;
- describing physical, virtual, mixed, nested, overlapping, and dynamically reconstituted spaces;
- exposing one complex system to another without requiring identical internal rules;
- presenting different views of one system to different peers; and
- providing trustworthy inputs to composition, routing, failure, residency, and security policy.

This raises a structural question: should an **Environment** be a virtual Component with its own
boundary, or should topology and composition remain orthogonal? A related proposal introduces a
**Gate** at an Environment boundary. A Gate may organize how the Environment is described and how
its internal Actors, Components, Operations, Events, Shapes, and authority relationships are
presented externally. One Environment may have several Gates, each presenting a different view.

The remainder of this note preserves the strongest useful form of each position and its costs. The
Resolution above governs where the hypotheses differ; retaining their reasoning prevents the chosen
hybrid from later collapsing into either rejected extreme without evidence.

## Existing constraints

Any resolution must preserve the following Architecture 0.8 invariants:

- Brontide Base has eight terms and must satisfy the Embedded Test. A statically authored leaf need
  not run a topology resolver or describe its internal construction.
- Actors participate in authority. Components declare composition contracts but do not hold
  Capabilities merely by being loaded, selected, or attached.
- Composition Regions own resolved and activated generations. They are lifecycle boundaries, not
  automatically physical, network, authority, or visibility boundaries.
- Topology is observer-relative, attributable, possibly incomplete, and sometimes disputed. A
  participant's description is evidence, not self-authenticating truth.
- Authority Domains remain distinct until a future Distributed and Identity protocol defines how
  cross-domain identities and evidence are admitted. Connection alone grants nothing.
- Visibility, reachability, trust, identity, and authority are different properties. None follows
  merely from membership in the same grouping.

The language “an Environment exposes a Capability” is therefore shorthand. Strictly, an Actor at a
boundary holds, accepts, derives, or delegates a Capability. An Environment may be represented by
such Actors; the Environment itself does not become an authority principal unless it also has a
separately defined Actor identity.

## Recorded vocabulary

The names in this section are recorded design terms, not ratified extension vocabulary.

### Topology Map

A **Topology Map** is an observer-scoped, versioned, and potentially incomplete graph of topology
identities and attributable relations. `Map` is preferred to `set`: the important information is not
only membership, but direction, relation type, evidence, time, and the observer from whose position
the graph is meaningful.

Two Maps may disagree without either being malformed. A local Host may know that several functions
arrived through one receiver while a device reports a finer hierarchy. Policy decides which claims
to accept; the Map preserves the disagreement and its provenance rather than manufacturing one
universal topology.

### Environment

An **Environment** is an identity-bearing grouping in a Topology Map. It has a describable
boundary and may be:

- physical, such as a device assembly, rack, room, or power domain;
- virtual, such as a process group, virtual machine, user session, Workspace, or simulated world;
- mixed, such as a service spanning machines and virtual runtimes;
- nested, such as a device Environment containing a firmware Environment;
- overlapping, such as the same Component occurrence belonging to a hardware Environment, user
  Environment, failure Environment, and organisational Environment; or
- reconstituted, such as a service restored elsewhere from retained state.

An Environment is anchored to the minimum topology floor that the Composition direction owns: its
direct members are Topology Nodes, and through the floor's attributable Relations it reaches the
Components, Actors, resources, Regions, and Ports associated with them. A Topology Map's relation
vocabulary begins with those floor Relations; richer graphs extend them without replacing
membership.

An Environment is an observer's identity for a grouping, not a claim that the grouping has one
physical enclosure, owner, Actor, authority domain, Component definition, or activation lifecycle.
Reconstitution normally creates a new Environment occurrence. Relations such as `SuccessorOf`,
`RestoredFrom`, or `Reconstitutes` may assert continuity without silently preserving Actor identity,
state, authority, trust, or failure history.

### Gate

A **Gate** is a declared boundary relationship anchored to one Environment and addressed
to a particular peer, audience, Authority Domain, or class of observers. It defines what crosses the
boundary and how that Environment is projected to that relationship.

A Gate may be realised by a dedicated Component and its Actors, by Host machinery, or by static
construction. As with Mediation, erasing a trivial implementation must not erase the declared
relationship. A Gate that owns policy, state, queues, translation, authority handling, audit,
recovery, or an independent lifecycle should normally be a dedicated Component.

Several Gates may belong to one Environment. An internal administration Gate might disclose rich
structure, an application Gate might expose selected services, and a public Gate might expose one
opaque façade. All describe the same Environment from different relationships; none is automatically
the complete or globally privileged description.

A Gate and a Composition Port are different declarations that may share one boundary occurrence.
The Port owns composition semantics — contracts, cardinality, imports and exports, resolution, and
the lifecycle envelope of child generations. The Gate owns protection and projection semantics.
Where a Region boundary coincides with a Protected Environment boundary, every covered crossing is
Gate business, and attachment through a runtime-open Port is a covered crossing: such a Port
coincides with a declared Gate, one boundary occurrence carrying both declarations. An ordinary
Region acquires no Gate obligation merely by having Ports.

### Protected Environment and Protection Plane

A **Protected Environment** adds a protection contract to an Environment. The contract identifies
its **Protection Plane**, protected membership and resources, enforcing Actors or Host machinery,
covered crossing classes, Gates, fail-closed behaviour, lifecycle, and evidence supporting the claim
that relevant paths cannot bypass the boundary.

Protection Plane scopes non-overlap. Within one Plane, a Protected Environment is disjoint from or
fully contains every peer Protected Environment. Independent Planes may overlap—for example, a
hardware-isolation Plane and a tenant-authority Plane—because an occurrence may legitimately be
subject to both. A crossing that leaves boundaries in several Planes must satisfy the Gate rules of
each.

A Protection Plane is not a free label. Its declaration names the protection dimension it isolates
— memory and execution, network reachability, tenancy and authority, physical containment,
jurisdiction — and its enforcement basis: the mechanism class, the enforcing Actors or Host
machinery, and the trust root those enforcers answer to. Two Protected Environments claiming the
same dimension under the same enforcement basis belong to the same Plane. Declaring them into
separate Planes to escape laminarity is a conformance violation detectable from the declarations
themselves: the escape requires a false enforcement-basis statement, which turns gaming the
invariant into an attributable misrepresentation under §24 rather than a loophole. A Plane
reference presented to another system is an ordinary claim under trust admission; the receiver
maps or rejects it locally.

A Protected Environment may encompass ordinary Environments and nested Protected Environments.
Ordinary topology descriptions in other Maps or Planes may intersect its membership, but they do not
gain permission to disclose or cross the protected interior; any such external View still comes
through a Gate.

External observers receive no portable view of a Protected Environment's interior except through a
Gate. This is **architectural opacity**, not a claim of information-theoretic invisibility: timing,
resource use, physical effects, or other side channels remain outside the guarantee unless the
protection contract explicitly covers them.

Zero active Gates is a valid state, not a named term: such a Protected Environment has no declared
external communication and does not interact externally until a Gate becomes active. The state
deliberately carries no dedicated vocabulary — "no active Gate" already describes it completely,
and an earlier `Sealed Environment` name invited confusion with the Host-Assisted profile's sealed
bootstrap composition. With one or more active Gates, all covered communication crosses a Gate; an
undocumented bypass invalidates the corresponding protection claim rather than becoming an implicit
Gate.

Protection is in force throughout the containing generation's Establishment, not only after
Release. A Gate realised by Components reaches Ready and Releases with its generation like any
other Component; before its Release a Gate admits nothing, and the only covered crossings permitted
during Establishment are the declared lifecycle Operations of Relational Initialisation under their
narrow authority. Failure during Establishment fails closed and never opens an undeclared crossing.

## Hypothesis A: an Environment is a virtual Component

The strong hypothesis treats every Environment as a Component occurrence, possibly realised only as
a virtual or statically erased boundary. It declares provided and required contracts, may contain
Components or nested Environments, participates in composition generations, and realises boundary
Actors through its Gates.

### Argument for the hypothesis

This interpretation has substantial advantages:

- **Recursive organization.** The same containment and resolution machinery describes a library,
  application, device, virtual machine, remote platform, data centre, or organisation-facing system.
- **A stable external unit.** Internal Components may be replaced or reconstituted while the virtual
  Component preserves a declared boundary, subject to explicit identity and lifecycle rules.
- **Remote systems become ordinary composition participants.** A remote system may expose one
  Component-shaped boundary even when its internal implementation, policy, and technology are
  unrelated to the receiving system.
- **Existing machinery can be reused.** Requirements, contracts, Provider Sets, Composition Ports,
  Binding Plans, generations, establishment, release, replacement, and rollback can apply at the
  Environment boundary instead of gaining a parallel orchestration model.
- **Encapsulation becomes inspectable.** The difference between internal and external structure is a
  declared projection rather than an informal deployment convention.
- **Nested Environments are natural.** If an Environment is a Component, it can contain another
  Environment using the same recursive composition rules.
- **Tooling becomes coherent.** A Component Manager can present, resolve, activate, inspect, and
  replace complex system groupings through one model.

The hypothesis is especially attractive for systems that are already managed as units and have a
deliberate external contract. In those cases “virtual Component” may describe the engineering reality
better than treating the boundary as topology metadata only.

### Criticism of the hypothesis

The equivalence also creates serious pressure:

- **Topology is observer-relative; Component structure is authored.** A Host may synthesize an
  Environment around an attached device or remote endpoint even when no corresponding Component was
  authored. Turning that observation into a Component invents requirements, ownership, and lifecycle
  that may not exist.
- **Environments overlap.** One Component occurrence may simultaneously belong to several physical,
  virtual, user, failure, and administrative Environments. Treating every membership as composition
  containment either duplicates the Component, forces an arbitrary parent, or turns the composition
  tree into an unrestricted graph with unclear lifecycle ownership.
- **Not every grouping is activatable.** A room, power circuit, latency region, legal residency
  boundary, or inferred failure cluster can be important topology without being loadable, resolvable,
  replaceable, or capable of reaching Ready.
- **Identity becomes ambiguous.** Is the Environment's identity authored by the Component publisher,
  created by the local observer, retained across reconstruction, or supplied by a remote system? A
  single virtual-Component identity cannot answer every case safely.
- **Authority may be accidentally laundered.** If the Environment is described as exposing
  Capabilities, implementations may incorrectly treat containment or Gate selection as authority.
  Only Actors and explicit Delegation may establish that relationship.
- **A virtual Component can overstate enforcement.** Drawing a boundary does not prove that all
  crossings traverse it. Side channels, shared resources, physical effects, and undisclosed network
  paths may bypass the supposed Component boundary.
- **Base-only leaves become awkward.** A passive mouse or sensor can be placed in a local Map without
  claiming Composition. Requiring an Environment Component at its own boundary risks transferring
  Host obligations to the leaf.
- **Independent Maps may produce competing Components.** Two observers can legitimately group the
  same occurrences differently. Treating both groupings as the one compositional truth makes
  reconciliation unnecessarily structural.

The strongest criticism is therefore not that virtual Components are useless. It is that they fit
some Environments extremely well and others only by pretending that description is ownership.

## Hypothesis B: topology and composition remain orthogonal

The opposite hypothesis keeps an Environment entirely descriptive. It may group Components, Actors,
resources, other Environments, and non-computational items without becoming any of them. Components
continue to own authored structure and lifecycle; Environments record observer-relative topology.

### Argument for the hypothesis

- Physical, virtual, inferred, overlapping, and incomplete groupings remain representable without
  inventing executable behaviour.
- A Base-only participant can be mapped by its Host without learning Composition or Topology.
- Maps can disagree, evolve, or be reconstructed without implicitly replacing Component instances.
- Security analysis remains honest: a described boundary is not confused with an enforcement point.
- Component, Actor, Authority Domain, Composition Region, failure domain, and Environment identities
  remain orthogonal and can be related explicitly.

### Criticism of the hypothesis

- A complex Environment with a real external contract needs a second mechanism beside Components to
  describe requirements, bindings, lifecycle, and replacement.
- Remote systems risk becoming special cases or opaque endpoints attached through ad hoc adapters.
- Tools may need separate topology, composition, deployment, and boundary models even where they
  describe the same managed unit.
- Encapsulation is underspecified unless another concept defines what internal structure is visible
  and how the external façade is produced.
- Recursive organization becomes less uniform: an Environment may contain Components but cannot use
  Component composition semantics merely because containment exists.

Pure orthogonality avoids conflation but may discard too much useful common structure.

## Resolved synthesis: Gates create Component projections

A hybrid preserves the Environment as the topology identity while allowing a Gate to expose it
through a **Component projection** for one relationship.

Under the recorded direction:

1. An Environment may exist without any Component projection or Gate. A physical room, inferred
   failure cluster, or observer-created device grouping remains ordinary topology.
2. A Gate may declare that its Environment is presented as a Component boundary to a particular
   audience. That projection has provided and required contracts, boundary Actors, Binding Plans,
   lifecycle, authority handling, and failure semantics.
3. The projection is relational. Another Gate on the same Environment may present a different
   Component surface, a topology-only view, or no visibility at all.
4. A Gate projection may be resolved through a Composition Port and may itself contain or refer to
   nested projections. The Environment does not thereby acquire one universal composition parent.
5. Reconstituting internal Regions need not replace the Environment or its external projection, but
   any continuity of Actor identity, authority, state, or in-progress work must be declared by the
   Gate's lifecycle contract.
6. A remote relationship has an admission boundary on both sides. One system's Gate can state what
   it offers; it cannot define the peer's local Actor identity, trust decision, or granted authority.

This gives the virtual-Component hypothesis a precise home: the virtual Component is the Gate's
relationship-specific projection, not necessarily the Environment in every Map.

This synthesis is the decision. It does not require a dedicated runtime object when a trivial Gate
can be erased into static or Host machinery, and it does not standardise groups or orchestration of
Gates.

## Minimum Gate contract direction

A future portable Gate declaration must preserve at least the following information, without this
note fixing its descriptor or runtime representation:

- the Environment and Topology Map to which the Gate is anchored;
- the intended peer, audience, scope, or admitting Authority Domain;
- directionality and the permitted ingress, egress, discovery, and observation paths;
- exported and imported contracts and the boundary Actors that realise them;
- the declared export fidelity of every exported contract;
- Component, Actor, Operation, Event, Shape, and topology identities disclosed or projected;
- semantic adaptation, representation mapping, routing, aggregation, and other Mediation used at
  the boundary;
- requested authority relationships and the explicit Delegation or local admission points through
  which they may be established;
- the Environment View disclosed through the Gate;
- lifecycle, readiness, continuity, state, failure, backpressure, revocation, audit, and rollback;
- provenance, evidence, freshness, and known uncertainty; and
- the completeness claim, if any, that all relevant crossings pass through this Gate, and its
  evidence grade.

A Gate must not silently rename incompatible semantics. Representation mapping within one Shape
contract may follow Binding machinery; semantic adaptation between contracts remains an explicit
Adapter. Selection, Distribution, Aggregation, Arbitration, or topology-wide policy remains declared
Mediation rather than disappearing into the word Gate.

The last field is security-critical. A Gate can enforce a boundary only if the implementation can
justify that relevant crossings cannot bypass it. The claim is therefore graded evidence rather
than a Boolean:

- **Declared** — asserted, with no supporting evidence;
- **Enumerated** — every covered crossing class and resource is listed and mapped to its enforcing
  machinery;
- **Statically verified** — checked mechanically against the resolved composition: every modelled
  binding, Channel, and resource of protected members terminates inside the Environment or at a
  declared Gate, which a resolver can establish when the generation resolves;
- **Attested** — the enforcing machinery's identity and configuration carry attestation evidence,
  once a future `Identity` or `Distributed` protocol defines it.

Crossing classes outside the composition model — shared memory, DMA, radio, physical effects —
must be enumerated with their enforcing machinery or explicitly excluded from the claim. Every
grade names its exclusions, and side channels remain outside every grade unless the protection
contract covers them. Encountering an unmodelled crossing at runtime is a composition defect that
travelled, in the sense of the shift-left stance of Architecture 0.8 §6.16: resolution-time
checking is the mechanism, runtime fail-closed the backstop. Tooling must not present a lower
grade with a higher grade's confidence. Where completeness cannot be established, the Gate is a
useful projection and mediation point but not a complete security perimeter, and an undocumented
bypass invalidates the claim at whatever grade it was made.

## Gate export fidelity

A raised concern deserves a recorded answer: if every boundary presents a Gate-authored
projection, distributed composition could degenerate into a network of semantic middleboxes in
which nothing composes end to end. The resolution is that a projection is not a licence to
reinterpret. What distributed composition needs from a boundary is not structural transparency but
honesty about three things: the semantic contract identity is preserved or explicitly changed;
provenance reaches the provider that actually executes; and operational facts — failure domain,
placement, latency, capacity — survive as capability-derived Attributes. A Gate that preserves
those is a seam; a Gate that silently changes them is a middlebox.

Every contract a Gate exports therefore carries exactly one declared **export fidelity**:

- **Direct** — the boundary Actor is the internal Actor; the export is the internal contract.
- **Deputised** — a boundary Actor forwards one-to-one to one internal provider under the same
  contract identity, with provenance preserved. Authority is presented per request under the
  invocation principle.
- **Mediated** — the export stands for a declared Selection, Distribution, Aggregation, or
  Arbitration over several internal providers. The ordinary Mediation rules apply unchanged:
  member identity, provenance, failure domain, and authority are never erased, even where members
  are not externally addressable.
- **Adapted** — the export's semantics differ from any internal contract. The declaration names
  the realising Adapter Component, and the export never reuses an internal contract's canonical
  name with changed semantics; semantic change requires a new name, exactly as it does for
  Constraint types.
- **Synthetic** — a contract authored at the boundary and realised by orchestration over the
  interior. A Synthetic export is a Component in its own right, with its own identity, version,
  and declared requested authority.

Silently presenting one class as another — a Synthetic façade wearing a Direct face — is
nonconforming. Erasing a trivial realisation remains permitted; erasing the declared class does
not, exactly as declared Mediation survives a statically erased realisation.

Fidelity has an authority consequence that keeps the default honest. Direct and Deputised exports
pass per-request authority through the boundary, so the Gate holds little on its own account.
Adapted and Synthetic exports must be backed by standing authority the Gate itself holds over the
interior, and that standing authority is enumerated per export. The more a Gate reinterprets, the
more authority it concentrates and the larger a confused-deputy target it becomes — visible in its
declaration, priced rather than hidden. High fidelity is therefore the ordinary, cheap choice;
reinterpretation is deliberate and paid for in declared authority.

Fidelity is chosen per relationship like every other Gate property. Within one authority domain,
high-fidelity Gates are the expected form and distributed composition remains ordinary
composition: a resolver may bind through them as it binds through Composition Ports. Across
organisational boundaries, the disclosed fidelity is the sovereign's explicit choice; a Static
Embedded or Host-Assisted device legitimately exposes stable contracts while its interior stays
private. The discipline this section adds is only that the choice is declared — never inferred and
never misrepresented.

Replacement behind a Gate is a lifecycle declaration, not an inference. Whether an export survives
replacement of its backing — a scoped restart of the interior, a Provider Set membership change
behind a Mediated export, or a hot swap where that stronger contract exists — is declared by the
Gate's lifecycle contract. Continuity of export contract identity, boundary Actor identity, session
state, and in-progress work are separate declarations, and silence promises none of them. A scoped
restart behind a Gate never silently becomes identity continuity.

## Environment identification and mutual understanding

An Environment needs a portable way to identify itself at an external boundary. This requirement
does not make Environment a Base term. An Environment is not an Actor and cannot speak or prove its
own identity; a Gate's boundary Actor presents the Environment reference and the contract under which
that reference should be interpreted.

Five occurrences must remain distinct:

- **Identification:** the Gate states that it represents a particular Environment reference.
- **Understanding:** the peer confirms that it implements compatible Environment semantics and
  understands the presented Topology contract version and Profile closure.
- **Recognition:** the peer creates or associates a local Environment reference with the presented
  reference under explicit identity rules.
- **Trust:** the peer evaluates provenance, identity, attestation, and other evidence under local
  policy.
- **Authority:** the peer grants or derives Capabilities for boundary Actors through ordinary local
  admission and Delegation.

None implies the next. Successful parsing is not understanding; understanding is not recognition;
recognition is not trust; and trust is not authority.

A minimum Environment-boundary presentation should contain:

- the presented Environment reference and the Actor making the presentation;
- the Topology contract version and any required Profile identifiers;
- the Gate identity, direction, and disclosed Environment View;
- the identity and continuity claims associated with the reference;
- available provenance, attestation, and other evidence references; and
- the requested fallback when the peer does not understand the contract.

The peer returns an explicit Outcome such as **Understood**, **Partially Understood**,
**Unsupported**, **Rejected**, or **Accepted with Local Environment Reference**. Exact Outcome names
and Shapes remain extension work, but silence, connection, successful decoding, or continued traffic
must never be interpreted as understanding. An acceptance Outcome records the effective version,
Profile closure, local reference, and any deliberately unsupported fields.

The Outcome provides an attributable protocol commitment, not proof that a faulty or malicious peer
implemented its claim correctly. Conformance evidence, attestation, challenge Operations, and later
observable behaviour may strengthen or contradict it. This is the strongest certainty an open system
can obtain without assuming the peer is truthful.

An acceptance Outcome creates an attributable **alias relation** in the receiver's Topology Map
between the presented reference and the local reference, carrying provenance, the effective
contract version, and freshness. Alias relations relate occurrences; they never merge them. Two
aliases binding one presented reference to different local occurrences are a conflict to surface
to policy, not an inconsistency to resolve silently. Each observer's Map remains its own system of
record, and traversing alias relations in queries is itself policy-controlled. Reconciliation
stronger than pairwise aliasing — shared identity across many observers — waits for the `Identity`
and `Distributed` work, like every other cross-domain identity question.

Continuity is sequenced the same way. Within one authority domain, continuity across
reconstitution is domain policy and needs no cryptography: the Gate's lifecycle contract declares
which of Actor identity, authority, state, and in-progress work survive, under the reference rules
of Architecture 0.8 §9.1, and everything undeclared does not survive. Across domains, continuity
rests on receiver-owned pairing: at first admission the receiving domain mints its own durable
local identity for the peer, so continuity across reconnection is the receiver's recognition
decision rather than the peer's claim. Presented continuity claims and their evidence are ordinary
admission inputs under §24; attested federation may later strengthen them, but no new protocol is
required before the intra-domain and pairing forms are useful.

A Base-only peer may continue to treat the Gate as an opaque Actor boundary without understanding
Environment. A Profile that requires mutual Environment understanding may instead fail closed or
restrict interaction until a compatible Outcome exists. General-purpose, Host-assisted, distributed,
or other environment-aware Profiles may depend on `Topology`; the Static Embedded Profile need not.
A Host may construct and negotiate an Environment representation for a passive Base-only leaf
without transferring its Topology obligations to that leaf.

The conclusion is therefore:

> Environment identity is portable and explicitly negotiable at Gate boundaries, while Environment
> remains outside Base. A Profile that requires the outside system to understand the Environment
> includes `Topology` and requires an explicit successful understanding Outcome.

## Transparency is relational and multidimensional

`Transparent` and `opaque` are not global properties of an ordinary Environment. The same
Environment may be transparent through one Gate and opaque through another, and visibility has
several independent dimensions:

- member and containment discovery;
- Actor and Component identity disclosure;
- contract, Operation, Event, and Shape visibility;
- topology and failure-domain visibility;
- operational reachability;
- Event, Flow, state, health, and telemetry visibility;
- provenance and evidence disclosure;
- authority and Delegation introspection; and
- administrative or composition control.

A structurally opaque Gate may expose broad operational functionality through a stable façade. A
structurally transparent diagnostic Gate may reveal internal membership while granting no authority
to invoke anything. Visibility never implies reachability, and reachability never implies authority.

A Gate's **Environment View** is the projection describing these dimensions for one
relationship. Product interfaces may summarize a View as opaque, selective, or transparent, but the
portable record should retain the individual dimensions and any filtered, summarized, pseudonymous,
or conditionally disclosed identities. Most practical Gates will be selective rather than wholly
transparent or opaque.

A Protected Environment adds one stronger invariant: its exterior receives no View of the interior
except through a Gate. A Gate may still disclose a transparent administrative View to an authorised
audience, but that transparency is a Gate projection and does not make the Protected Environment
globally transparent.

## Information carried by an Environment and Map

The normative kernel should be slim while allowing expansive typed Attributes.

An Environment occurrence should minimally carry:

- a strongly typed local occurrence identity and optional presentation name;
- a declared or observed kind without treating the label as proof;
- the observer and source of each assertion;
- creation, validity, revision, and termination information;
- direct membership and typed Topology Relations;
- known Gates and boundary-crossing relations;
- when protected, its Protection Plane, covered resources and crossings, enforcing boundary,
  opacity, fail-closed behaviour, and no-bypass evidence;
- evidence, confidence or verification state, and unresolved conflicts;
- explicit incompleteness; and
- lineage or reconstruction relations to other occurrences.

A Topology Map should minimally carry its own identity, observer, scope, revision, observation time,
freshness rules, and policy for unknown or conflicting information.

Latency, coordinates, jurisdiction, capacity, cost, power, connectivity, residency, health,
availability, failure correlation, ownership claims, and similar information should be versioned,
Shape-described Attributes or relations. This permits rich Environments without freezing an
unbounded property bag into the core.

Security information may appear as attributable evidence and relations: governing Authority Domain,
attested isolation, security-zone claims, enforcement Components, policy version, or disclosure
classification. It must not be a universal `trusted` flag, an implicit Capability, or the union of
member authority. Reading or modifying a sensitive Map is itself an Operation controlled by
Capabilities.

## Thought experiments

### Ordinary mouse

The Host creates a local Environment for the attachment and relates admitted functions to it. The
mouse remains Base-only or Static Embedded. A trivial Gate projection may be erased into Host
machinery. No Environment Component is required inside the mouse.

### Host-assisted smart mouse

The device has an internal composed Environment. A local management Gate can expose selected
internal Regions for repair or configuration, while an external input Gate presents only stable
pointer, button, battery, and configuration contracts. Internal transparency and Host-facing opacity
coexist. If the device claims a Protected Environment, every covered external interaction traverses
one of those Gates. The device and Host still make separate authority decisions.

### Remote organisational system

The remote system may be internally governed by different identity, policy, lifecycle, and
composition rules. A public Gate presents one Component projection; an administrative Gate presents
another. The receiving system admits each boundary Actor and derives local Capabilities according to
its own policy. The remote Environment cannot self-grant authority by claiming to be trusted.

### Reconstituted service

A failed service is recreated on different machines. The new Environment occurrence may
`Reconstitute` the former one. A Gate may preserve a public contract and name, but preservation of
Actor identity, Capability validity, state, admitted work, and failure history requires explicit
lifecycle and authority rules. Topology lineage alone preserves none of them.

### Overlapping spaces

One database Component belongs simultaneously to a process Environment, a tenant Environment, a
physical-host Environment, a residency Environment, and a shared failure Environment. Those
memberships can inform different policies without creating five Component parents or five database
occurrences. Gates select the view relevant to each relationship. When any of these is protected,
laminar membership is required within its Protection Plane; independent Planes may intersect.

## Deliberately deferred detail

The direction does not need a deeper architecture taxonomy before implementation evidence exists.
In particular, it does not define Gate groups, Gate orchestration, one universal descriptor, a wire
protocol, or an implementation object graph. Those are extension-design and implementation concerns
provided they preserve the recorded boundaries.

A future `Topology` specification will still need concrete Shapes, identities, Map query and Dataset
representations, evidence formats, lifecycle records, and conformance tests. It will also need the
portable descriptor form of export fidelity and its validation, the Plane dimension and
enforcement-basis vocabulary with same-basis detection, completeness-grade evidence formats and the
resolution-time no-bypass check, alias-relation and receiver-owned pairing records, and the
intra-domain continuity declaration. Distributed admission,
cross-domain identity, attestation, side-channel claims, and persistence of exported Actor identity
remain owned by their respective specifications. These are protocol obligations, not reasons to
reopen whether Environment is a Component or where protection and transparency reside.

## Recorded direction

Topology is a first-class architectural direction rather than a collection of placement Attributes:

- Environment, Protected Environment, Protection Plane, Topology Map, Gate, and Environment View
  remain outside Brontide Base;
- ordinary Environment identity is orthogonal to Component, Actor, Composition Region, and Authority
  Domain and may overlap other ordinary Environments;
- Protected Environments are laminar within one Protection Plane and are opaque except through Gates;
- a Protected Environment with zero active Gates has no declared external communication and does
  not interact externally;
- a Gate exposes its Environment through a relational virtual-Component projection;
- every Gate export declares one fidelity class — Direct, Deputised, Mediated, Adapted, or
  Synthetic — and reinterpretation is never presented as exposure;
- Adapted and Synthetic exports enumerate the standing authority that backs them;
- a Protection Plane is identified by its protection dimension and enforcement basis, and
  same-basis Protected Environments share one Plane;
- no-bypass completeness claims are graded — declared, enumerated, statically verified, or
  attested — and name their exclusions;
- peer recognition records attributable alias relations between Environment references and never
  merges occurrences;
- intra-domain reconstitution continuity is declared by Gate lifecycle contracts, and cross-domain
  continuity rests on receiver-owned pairing;
- attachment through a runtime-open Port across a Protected Environment boundary is a covered
  crossing and coincides with a declared Gate, one boundary occurrence carrying both declarations;
- an Environment groups Topology Nodes from the Composition-owned minimum floor, whose Relations
  begin every Map's relation vocabulary;
- protection holds fail-closed throughout Establishment, and a Gate admits nothing before its
  Release;
- export continuity across backing replacement is declared by Gate lifecycle contracts, never
  inferred from Gate or export identity;
- Gate boundary Actors present portable Environment identities, and peers confirm compatible
  understanding through explicit versioned Topology Outcomes rather than inference;
- visibility remains Gate- and audience-relative, multidimensional, and independent of authority;
  and
- explicit Actors, Capabilities, Delegations, Mediation, lifecycle, and evidence support every claim
  that a Gate actively enforces rather than merely describes a boundary.

The competing hypotheses remain above as decision rationale. The Environment/Gate split itself is no
longer an open architecture question in this design direction.
