# BRONTIDE

## Design Note: Topology Environments and the Guardian Family

**Status:** Recorded design direction, version 0.1; not ratified

**Current architecture context:** [Brontide Architecture 0.8](../architecture/Brontide-Architecture-0.8.md),
especially §§7, 18.1, 19, 24, and 33.

**Scope:** Records the resolved Environment and Guardian-family direction together with the competing reasoning
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
- A **Guardian** is an Actor explicitly entrusted to protect or represent another participant,
  resource, or bounded interaction. Guardian is an Actor role, not a new authority primitive: it
  acts only through Capabilities it actually holds.
- A **Sentinel** is the bounded observational Guardian specialisation. Its primary function within a
  declared Sentinel Watch is third-party observation and reporting. The Watch grants neither
  visibility nor response authority; both remain governed by ordinary Capabilities and Executions.
- Every covered interaction crossing a Protected Environment boundary passes through a declared
  **Gatekeeper**. A Gatekeeper is the specialised Guardian designated by the protection contract as
  an allowed boundary participant. A Protected Environment with no active Gatekeeper is a valid
  state, not a named term: it has no declared external communication and does not interact
  externally until a Gatekeeper becomes active.
- A **Warden** is the operational-deputy Guardian specialisation designated for a declared
  privileged operation surface. For covered external requesters, the Warden is the sole operational
  entrance to that surface: the request terminates at the Warden, the requester's authority is
  evaluated there, and any interior effect is initiated separately under authority the Warden
  independently holds. The requester is neither admitted to the privileged interior nor elevated.
- A Protected Environment is architecturally opaque from outside. Its internal identities,
  structure, contracts, topology, and authority information are exposed only through an
  audience-specific **Environment View** provided by a Gatekeeper.
- Every Gatekeeper is a Guardian and therefore an Actor. It holds, accepts, derives, presents, and
  delegates Capabilities through the ordinary Actor rules; it is not a Component, Port, Environment
  projection, or parallel authority principal. Components or Host machinery may realise a Gatekeeper
  without changing that type. Not every Guardian is a Gatekeeper: the Guardian of human attention,
  for example, becomes one only when it is also designated at a Protected Environment boundary.
- Only Protected Environments have Gatekeepers. An Actor may describe, route into, or expose
  functionality from an ordinary Environment, but doing so does not make it a Gatekeeper because no
  protected crossing is being admitted.
- Every contract a Gatekeeper exports carries exactly one declared **export fidelity**: Direct,
  Deputised, Mediated, Adapted, or Synthetic. Reinterpretation is never presented as exposure, and
  a silent fidelity downgrade is nonconforming.
- Ordinary Environments do not gain an `open`, `transparent`, or `namespace` security class. They
  remain security-neutral. Namespace translation and structural disclosure are properties of a
  Gatekeeper and its Environment View.
- Guardian, Gatekeeper, Warden, and Sentinel form a recorded family of Actor roles; Sentinel Watch is
  the subordinate purpose and scope contract for Sentinel activity. Environment, Protected
  Environment, Protection Plane, Gatekeeper boundary semantics, Warden operational-deputy semantics,
  Topology Map, and Environment View belong to the future `Topology` direction, not Brontide Base.
- Protected Environment identity is portable at Gatekeeper boundaries, but mutual understanding is
  established only by an explicit versioned Topology exchange and Outcome. Recognition, trust, and
  authority remain separate decisions; Profiles state when successful understanding is mandatory.
- A protection boundary's no-bypass coverage is evidenced, never assumed from a Boolean flag.
  Recognition of a
  peer's Environment reference records an attributable alias relation rather than merging
  occurrences, and continuity across reconstitution or reconnection is a declared lifecycle or
  receiver-owned admission decision, never an inference.

This resolution deliberately stops before Gatekeeper or Warden grouping, orchestration, concrete
descriptor format, or implementation topology. Those concerns may reuse Composition and Mediation,
but they do not change the Actor and protection-boundary model above.

## Question and retained reasoning

Topology began as placement and attachment information. It now appears to have a wider role:

- keeping independently represented parts associated without assuming one physical device;
- describing physical, virtual, mixed, nested, overlapping, and dynamically reconstituted spaces;
- exposing one complex system to another without requiring identical internal rules;
- presenting different views of one system to different peers; and
- providing trustworthy inputs to composition, routing, failure, residency, and security policy.

This raises a structural question: should an **Environment** be a virtual Component with its own
boundary, or should topology and composition remain orthogonal? A related early proposal introduced
a boundary participant for any Environment. The resolution above narrows that idea: only a
Protected Environment has a **Gatekeeper**, and one Protected Environment may designate several
Gatekeepers for different protected relationships.

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
boundary holds, accepts, derives, or delegates a Capability. For a Protected Environment, an Actor
admitted as a covered boundary path is a Gatekeeper. An ordinary Environment may be represented by
Actors without acquiring a Gatekeeper; the Environment itself does not become an authority
principal unless it also has a separately defined Actor identity.

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

### The Guardian family

The **Guardian family** groups related Actor roles concerned with protection, representation, and
assurance. Family membership is a role designation, not a new participant category or authority
primitive. Every Guardian-family member is an Actor, and one Actor may hold several family roles.
The family currently records Guardian as the general role and Gatekeeper, Warden, and Sentinel as
independent specialisations.

#### Guardian

A **Guardian** is an Actor explicitly entrusted to protect or represent another participant,
resource, or bounded interaction. The human-attention guardian of Architecture 0.8 §26.1 is the
ordinary example: it arbitrates access to a bounded resource under authority it actually holds.
Calling an Actor a Guardian grants nothing, implies no ownership, and creates no authority outside
the Capability and Delegation model.

#### Gatekeeper

A **Gatekeeper** is the preventative Guardian specialisation designated by a Protected
Environment's protection contract as an allowed boundary participant. Every covered crossing is
either blocked or occurs through a Gatekeeper. The designation records the Protected Environment
and Protection Plane, direction, covered crossing classes, intended peer or audience, and the
Environment View and contracts the Gatekeeper may expose. Every Gatekeeper is a Guardian; a Guardian
that protects something other than a Protected Environment boundary is not a Gatekeeper.

Guardian, Gatekeeper, Warden, and Sentinel are Actor roles, not second objects wrapped around Actors. A
Gatekeeper's Actor reference is its authority identity, and ordinary Capability, Delegation,
Operation, Event, and Outcome semantics apply without translation. Routing, projection, or boundary
placement alone does not create a Gatekeeper: an ordinary Environment has no Gatekeeper, and an
Actor becomes a Gatekeeper only through a Protected Environment's enforced protection contract.

A Gatekeeper may be realised as an Actor within a dedicated Component, by Host machinery, or by
static construction. A Component realisation must expose the Gatekeeper as an Actor rather than
treating the Component itself as authority-bearing. As with Mediation, erasing a trivial
implementation must not erase the declared Gatekeeper type or its protection-boundary designation.
A Gatekeeper that owns policy, state, queues, translation, authority handling, audit, recovery, or
an independent lifecycle should normally be realised by a dedicated Component.

Several Gatekeepers may protect one Protected Environment. An internal administration Gatekeeper
might disclose rich structure, an application Gatekeeper might expose selected services, and a
public Gatekeeper might expose one opaque façade. All participate at the same protected boundary
from different relationships; none is automatically the complete or globally privileged
description.

A Gatekeeper and a Composition Port remain different concepts. The Port owns composition semantics —
contracts, cardinality, imports and exports, resolution, and the lifecycle envelope of child
generations. The Gatekeeper is the Actor through which the protection contract admits a covered
crossing.
Where a Region boundary coincides with a Protected Environment boundary, attachment through a
runtime-open Port must terminate at or be performed through a declared Gatekeeper. The Port does not
become that Gatekeeper, and an ordinary Region acquires no Gatekeeper obligation merely by having
Ports.

#### Warden

A **Warden** is the operational-deputy Guardian specialisation designated for a declared privileged
operation surface. For covered external requesters, it is the sole operational entrance to that
surface. A requester invokes a Warden-exposed Operation rather than an interior Operation or an
interactive interior session. The requester's Capability is evaluated at the Warden request surface
and is never combined with, rewritten as, or promoted into the authority the Warden holds over the
interior.

If the request is accepted, the Warden deliberately initiates a separate interior Execution using
its own Capability. The outer request, the Warden's decision, the inner Execution, and any returned
Outcome remain causally correlated and attributable. This is visible deputyship under the invocation
principle, not Delegation to the requester and not an exception to conservation of authority. The
Warden's interior authority must pre-exist the request or be established independently through the
ordinary Genesis and Delegation rules; the request itself creates none.

Authorisation is conjunctive rather than additive:

1. the requester must be authorised to invoke the Warden's request Operation;
2. the Warden's declared policy must permit that exact request, target, input, and context; and
3. the Warden must hold and present a Capability authorising the separate interior Execution, which
   the interior target evaluates normally.

Failure of any condition denies before the interior effect begins. The requester's authority and the
Warden's authority are never unioned. Approval is bound to the represented request; it is not a
reusable credential, a Capability transfer, or a change to the requester's authority.

**Requester authority never elevates at the Warden seam.** A successful request changes which
effect the Warden chooses to cause, not which Capabilities the requester holds. Any later change to
the requester's authority requires an independent Genesis or Delegation occurrence and is not an
effect of Warden access.

A Warden exposes typed Operations with complete input and output Shapes. It does not accept an
arbitrary interior command, target, path, or designation merely because the requester supplied it.
Any requester-selectable designation must be an explicit part of the Warden contract, resolved under
the Warden's namespace and policy, and constrained before the Warden presents its own authority.
Defaulting to the Warden's ambient authority on requester-controlled designations is the confused-
deputy error that the role exists to make visible and prevent.

Gatekeeper and Warden answer different questions. A Gatekeeper decides whether a covered crossing
may occur; a Warden decides whether to perform a privileged effect for a requester that is not
authorised to perform it directly. One Actor may hold both roles when the Warden is itself the
designated boundary participant. A Gatekeeper may instead front a separate Warden, but that route
does not create a second operational entrance: the Gatekeeper admits the request crossing and the
Warden remains the only Actor authorised by the declared surface to turn it into the privileged
interior effect. A Sentinel may observe and report either Execution, but its findings grant no
authority to approve or perform the effect.

A Warden may be realised by a dedicated Component, Host machinery, static construction, or another
form that preserves its Actor identity, declared request surface, authority envelope, and
attribution. A Warden with policy, queues, audit, result filtering, recovery, or an independent
lifecycle should normally be realised by a dedicated Component. Before the Warden is active, after
it fails, or while its policy or audit preconditions are unavailable, the privileged surface fails
closed; no fallback path silently grants direct access.

#### Sentinel

A **Sentinel** is the observational Guardian specialisation whose primary function within a declared
**Sentinel Watch** is observation and reporting of third-party activity. It watches occurrences in
which it is not an operational participant except through observation, recording, reporting, or
adjacent alerting. The observed activity may include Capabilities not held or exercised by the
Sentinel, their presentation, evaluation, Delegation, revocation, or use, and the Operations,
Executions, Events, Outcomes, or Flows of other Actors.

Sentinel is a semantic role rather than merely a label. An Actor meeting that behavioural criterion
acts as a Sentinel for the Watch even if its implementation calls it a monitor, audit recorder, or
alerting service. An Actor does not become a Sentinel merely because it observes its own work,
receives information as an ordinary participant in an interaction, or incidentally holds an
observation Capability. The same Actor may be a Sentinel for one Watch and perform unrelated roles
elsewhere.
It belongs to the Guardian family because the Watch entrusts it to represent observed activity to
declared audiences, not because every Watch must have a security purpose.

`Primary` is assessed per Watch rather than by measuring how much of an Actor's implementation or
lifetime is devoted to observation. Watch inputs are observation deliveries or observation-query
results; Watch outputs are records, findings, summaries, or alerts. A Sentinel participates in those
observation and reporting interactions, but not in the covered occurrence as its requester,
provider, target, mediator, Gatekeeper, or authority user. Any such participation is a separate role
for that occurrence.

Subscribing to an Event stream or opening or receiving a Flow does not by itself create a Sentinel
Watch. An Actor may consume the same stream for application logic, presentation, transformation,
orchestration, control, or an interaction in which it participates. It acts as a Sentinel only when
a Watch makes that stream an input for third-party observation and reporting. A Watch may also use
static instrumentation, authorised queries, callbacks, or other observation contracts without
depending on Event Distribution or Flow.

Non-participation applies to the watched occurrence, not to operation of the Watch. A Sentinel may
request ordinary support Operations to persist findings, append audit records, load policies or
models, checkpoint cursors, correlate observations, enforce retention, report its own health, or
deliver alerts. Such Capabilities are held toward Actors exposing facilities that may be realised by
Components; the Component itself is not authority-bearing. These supporting Executions remain part
of operating or reporting the Watch and do not turn the Sentinel into a participant in the observed
activity. Mitigation and unrelated domain effects remain separate roles.

A Sentinel Watch must make the observation boundary explicit enough that a candidate occurrence can
be classified as in scope, out of scope, or unresolved from declared facts. It records at least:

- a purpose identity and parameters;
- subject references, a bounded set or query, or a membership rule and resolution scope;
- occurrence classes and an inclusion predicate;
- sources, observation Capabilities, authority-domain and topology boundaries, exclusions, coverage,
  and unknown-source treatment;
- time interval, lease, lifecycle, revision, and continuity;
- evaluator contract and version, including deterministic, probabilistic, heuristic, or model-based
  interpretation and the confidence or evidence it produces;
- findings, records, summaries, and alerts together with audiences, provenance, retention,
  disclosure, and redaction; and
- backpressure, observation loss, gaps, failure, and withdrawal.

Purpose is a normative boundary rather than a descriptive tag. It identifies the concern and the
participant, resource, or interaction represented by the Watch, and constrains legitimate
interpretation, retention, disclosure, and reporting. `Observe everything` states breadth, not
purpose, and is insufficient by itself. Since authorised delivery cannot guarantee how an observer
later behaves, the purpose makes misuse nonconforming and attributable rather than magically
preventing betrayal.

The Watch boundary is deterministic even where the evaluator is not. `Every Event`, `every
Capability evaluation`, or `every action of these Actors` is bounded only when the sources,
occurrence vocabulary, Actor membership, lifetime, exclusions, and gap behaviour define the
quantified universe. A model may interpret that universe for the declared purpose; it may not
silently expand the Watch or repurpose its observations.

`Every Event used to assess availability of Environment E`, `every Capability evaluation used to
audit least-privilege policy in Authority Domain D`, and `every Execution by Actors A through N used
to verify a consent rule` are broad but bounded examples. Each names a purpose, subjects, occurrence
class, sources, lifetime, exclusions, and gap semantics rather than claiming omniscience.

A Sentinel receives observations only through contracts and Capabilities available to it. Its
findings use ordinary Events, Outcomes, or declared evidence Shapes. A finding is an attributable
claim: it is not proof, authority, or an instruction that another Actor must obey. Reporting may
include recording, correlation, summarisation, and alerting designated audiences.

The Sentinel designation carries no implicit power to block, quarantine, revoke, reconfigure, or
otherwise mitigate. Such effects require separately held Capabilities and an ordinary Execution
under a non-Sentinel responsibility. If a Sentinel Actor also admits or denies a covered Protected
Environment crossing, that Actor is also a Gatekeeper for the crossing. An exterior Sentinel
receives no declared architectural view of a Protected Environment's interior except through a
permitted Gatekeeper View or contract. Side-channel observations remain outside that claim unless
the protection contract explicitly covers them; the Sentinel role never creates a protection bypass.

A Component, Host mechanism, or static construction may realise a Sentinel while preserving its
Actor identity and Watch. Sentinel is universal in eligible subject kind and purpose, not in
visibility: security, safety, health, integrity, availability, compliance, performance, audit, and
domain-specific observation may all qualify. A deliberately broad Watch may cover every declared
source in an authority domain, but its authority concentration, exclusions, unknowns, and evidence
remain explicit rather than becoming architectural omniscience.

### Protected Environment and Protection Plane

A **Protected Environment** adds a protection contract to an Environment. The contract identifies
its **Protection Plane**, protected membership and resources, enforcing Actors or Host machinery,
covered crossing classes, the Gatekeepers permitted to admit them, fail-closed behaviour,
declared privileged operation surfaces and their Wardens where present, lifecycle, and evidence
supporting the claim that relevant paths cannot bypass the boundary or privileged operation surface.

Protection Plane scopes non-overlap. Within one Plane, a Protected Environment is disjoint from or
fully contains every peer Protected Environment. Independent Planes may overlap—for example, a
hardware-isolation Plane and a tenant-authority Plane—because an occurrence may legitimately be
subject to both. A crossing that leaves boundaries in several Planes must satisfy the Gatekeeper
rules of each.

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
through a Gatekeeper.

External observers receive no portable view of a Protected Environment's interior except through a
Gatekeeper. This is **architectural opacity**, not a claim of information-theoretic invisibility:
timing, resource use, physical effects, or other side channels remain outside the guarantee unless
the protection contract explicitly covers them.

Zero active Gatekeepers is a valid state, not a named term: such a Protected Environment has no
declared external communication and does not interact externally until a Gatekeeper becomes active.
The state deliberately carries no dedicated vocabulary — "no active Gatekeeper" already describes
it completely, and an earlier `Sealed Environment` name invited confusion with the Host-Assisted
profile's sealed bootstrap composition. With one or more active Gatekeepers, all covered
communication crosses a Gatekeeper; an undocumented bypass invalidates the corresponding protection
claim rather than becoming an implicit Gatekeeper.

A Protected Environment need not declare a Warden merely because it is protected. Where it does
declare a privileged operation surface, zero active Wardens means that covered external requesters
cannot cause those privileged effects. A direct administrative API, debug path, ambient Host call,
or emergency fallback that can cause a covered effect without the Warden invalidates the surface's
sole-entrance claim unless it is explicitly outside the declared coverage.

Protection is in force throughout the containing generation's Establishment, not only after
Release. When a Component realises a Gatekeeper, the Gatekeeper becomes active no earlier than its
realising Actor is Ready and the containing generation Releases it. A statically or Host-realised
Gatekeeper follows the equivalent readiness point declared by the protection contract. Before a
Gatekeeper is active it admits nothing, and the only covered crossings permitted during
Establishment are the declared lifecycle Operations of Relational Initialisation under their narrow
authority. Failure during Establishment fails closed and never opens an undeclared crossing.

## Hypothesis A: an Environment is a virtual Component

The strong hypothesis treats every Environment as a Component occurrence, possibly realised only as
a virtual or statically erased boundary. It declares provided and required contracts, may contain
Components or nested Environments, participates in composition generations, and realises boundary
Actors through its Gatekeepers.

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
  Capabilities, implementations may incorrectly treat containment or Gatekeeper selection as authority.
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

## Resolved synthesis: Gatekeepers are boundary Guardians

A hybrid preserves the Environment as the topology identity while using ordinary Actor semantics at
an enforced boundary. Gatekeepers may expose Component-authored contracts, but they are Actors
rather than virtual Components or projection objects.

Under the recorded direction:

1. An ordinary Environment exists without a Gatekeeper. A physical room, inferred failure cluster,
   or observer-created device grouping remains ordinary topology even when an Actor describes it.
2. A Protected Environment's protection contract designates one or more Gatekeepers. Each is an
   Actor whose exposed contracts, direction, authority, lifecycle, failure behaviour, and Environment
   View define one admitted boundary surface.
3. The surface is relational. Another Gatekeeper on the same Protected Environment may expose
   different contracts or a different View, while an audience with no admitted Gatekeeper has no
   declared crossing.
4. Composition may realise a Gatekeeper and bind its contracts through a Port, but the Environment
   does not thereby acquire one universal composition parent and the Port does not become the
   Gatekeeper.
5. Reconstituting internal Regions need not replace a Gatekeeper, but any continuity of Actor identity,
   authority, state, or in-progress work must be declared by the Gatekeeper's lifecycle contract.
6. A remote relationship has an admission boundary on both sides. One system's Gatekeeper can state
   what it offers; it cannot define the peer's local Actor identity, trust decision, or granted
   authority.

This retains the useful part of the virtual-Component hypothesis without creating a new authority
category: a Gatekeeper may expose Component-authored contracts, while the Gatekeeper itself remains
the Guardian that participates as an Actor at the protected boundary.

This synthesis is the decision. It does not require a dedicated runtime object when a trivial
Gatekeeper can be erased into static or Host machinery, and it does not standardise groups or
orchestration of Gatekeepers or Wardens.

## Minimum Gatekeeper contract direction

A future portable Gatekeeper declaration must preserve at least the following information, without
this note fixing its descriptor or runtime representation:

- the Gatekeeper's Actor identity and the Protected Environment, Protection Plane, and Topology Map
  whose boundary designates it;
- the intended peer, audience, scope, or admitting Authority Domain;
- directionality and the permitted ingress, egress, discovery, and observation paths;
- exported and imported contracts;
- the declared export fidelity of every exported contract;
- Component, Actor, Operation, Event, Shape, and topology identities disclosed through it;
- semantic adaptation, representation mapping, routing, aggregation, and other Mediation used at
  the boundary;
- requested authority relationships and the explicit Delegation or local admission points through
  which they may be established;
- the Environment View disclosed through the Gatekeeper;
- lifecycle, readiness, continuity, state, failure, backpressure, revocation, audit, and rollback;
- provenance, evidence, freshness, and known uncertainty; and
- its share of the Protected Environment's declared crossing coverage and no-bypass evidence.

A Gatekeeper must not silently rename incompatible semantics. Representation mapping within one
Shape contract may follow Binding machinery; semantic adaptation between contracts remains an
explicit Adapter. Selection, Distribution, Aggregation, Arbitration, or topology-wide policy
remains declared Mediation rather than disappearing into the word Gatekeeper.

The last field is security-critical. No individual Gatekeeper can establish that a Protected
Environment has no other crossing: completeness belongs to the protection contract across all
Gatekeepers, blocked paths, resources, and crossing classes. Its coverage level is therefore
evidence rather than a Boolean:

- **Declared** — asserted, with no supporting evidence;
- **Enumerated** — every covered crossing class and resource is listed and mapped to its enforcing
  machinery;
- **Statically verified** — checked mechanically against the resolved composition: every modelled
  binding, Channel, and resource of protected members terminates inside the Environment or at a
  declared Gatekeeper, which a resolver can establish when the generation resolves.

Attestation is an orthogonal assurance property, not a fourth and automatically stronger coverage
level. Once a future `Identity` or `Distributed` protocol defines it, attestation may support any
level by identifying the enforcing machinery and configuration; it does not replace enumeration or
static verification.

Crossing classes outside the composition model — shared memory, DMA, radio, physical effects —
must be enumerated with their enforcing machinery or explicitly excluded from the claim. Every
coverage level names its exclusions, and side channels remain outside every level unless the protection
contract covers them. Encountering an unmodelled crossing at runtime is a protection or composition
defect that
travelled, in the sense of the shift-left stance of Architecture 0.8 §6.16: resolution-time
checking is the mechanism, runtime fail-closed the backstop. Tooling must not imply more coverage or
assurance than the evidence supports. Where completeness cannot be established, the declared
Gatekeepers remain useful boundary participants but the Protected Environment is not a complete
security perimeter, and an undocumented bypass invalidates its protection claim.

## Gatekeeper export fidelity

A raised concern deserves a recorded answer: if every protected boundary presents a
Gatekeeper-authored surface, distributed composition could degenerate into a network of semantic
middleboxes in which nothing composes end to end. The resolution is that a boundary is not a
licence to reinterpret. What distributed composition needs is not structural transparency but
honesty about three things: the semantic contract identity is preserved or explicitly changed;
provenance reaches the provider that actually executes; and operational facts — failure domain,
placement, latency, capacity — survive as capability-derived Attributes. A Gatekeeper that preserves
those is a seam; a Gatekeeper that silently changes them is a middlebox.

Every contract a Gatekeeper exports therefore carries exactly one declared **export fidelity**:

- **Direct** — the Gatekeeper is also the interior provider Actor; the export is its internal contract.
- **Deputised** — the Gatekeeper forwards one-to-one to one interior provider under the same
  contract identity, with provenance preserved. Authority is presented per request under the
  invocation principle.
- **Mediated** — the Gatekeeper performs declared Selection, Distribution, Aggregation, or
  Arbitration over several internal providers. The ordinary Mediation rules apply unchanged:
  member identity, provenance, failure domain, and authority are never erased, even where members
  are not externally addressable.
- **Adapted** — the Gatekeeper exposes semantics that differ from any interior contract. The declaration
  names the Adapter Component or Host mechanism realising that behaviour, and the export never
  reuses an interior contract's canonical name with changed semantics; semantic change requires a
  new name, exactly as it does for Constraint types.
- **Synthetic** — the Gatekeeper exposes a boundary-authored contract realised by orchestration over the
  interior. The contract has its own identity and version, while the Gatekeeper holds the explicitly
  declared authority needed to realise it.

Silently presenting one class as another — a Synthetic façade wearing a Direct face — is
nonconforming. Erasing a trivial realisation remains permitted; erasing the declared class does
not, exactly as declared Mediation survives a statically erased realisation.

Fidelity has an authority consequence that keeps the default honest. Direct and Deputised exports
pass per-request authority through the boundary, so the Gatekeeper needs little standing authority of its
own. Adapted and Synthetic exports must be backed by standing Capabilities held by the Gatekeeper over the
interior, enumerated per export. The more a Gatekeeper reinterprets, the more authority it concentrates and
the larger a confused-deputy target it becomes — visible in its declaration, priced rather than
hidden. High fidelity is therefore the ordinary, cheap choice; reinterpretation is deliberate and
paid for in declared authority.

Fidelity is chosen per relationship like every other Gatekeeper property. Within one authority domain,
high-fidelity Gatekeepers are the expected form and distributed composition remains ordinary
composition: a resolver may bind through them as it binds through Composition Ports. Across
organisational boundaries, the disclosed fidelity is the sovereign's explicit choice; a Static
Embedded or Host-Assisted device legitimately exposes stable contracts while its interior stays
private. The discipline this section adds is only that the choice is declared — never inferred and
never misrepresented.

Replacement behind a Gatekeeper is a lifecycle declaration, not an inference. Whether an export survives
replacement of its backing — a scoped restart of the interior, a Provider Set membership change
behind a Mediated export, or a hot swap where that stronger contract exists — is declared by the
Gatekeeper's lifecycle contract. Continuity of export contract identity, Gatekeeper identity, session
state, and in-progress work are separate declarations, and silence promises none of them. A scoped
restart behind a Gatekeeper never silently becomes identity continuity.

## Minimum Warden contract direction

A future portable Warden declaration must preserve at least the following information, without this
note fixing its descriptor or runtime representation:

- the Warden's Actor identity, the Protected Environment, and the exact privileged operation surface
  for which it is the sole operational entrance;
- the covered requester identities, audiences, Authority Domains, and origin classes;
- the typed request Operations and their complete input and output Shapes;
- the exact mapping from each accepted request to interior Operations, targets, and Shapes, including
  every requester-selectable designation and the namespace in which it is resolved;
- the Warden policy evaluator, version, inputs, approval facts, unknown-input treatment, and
  fail-closed decision behaviour;
- the standing interior Capability envelope the Warden may present, its provenance, Constraints,
  representation, lifetime, withdrawal behaviour, and maximum revocation horizon;
- causal and correlation links among the request Execution, Warden decision, interior Execution,
  returned Outcome, and any compensation;
- output projection, redaction, aggregation, and other egress filtering applied before information
  returns to the requester;
- lifecycle, readiness, continuity, queueing, backpressure, replay, idempotency, failure, recovery,
  and rollback where those properties are promised;
- audit records, audiences, retention, and the behaviour required when policy or audit prerequisites
  are unavailable;
- the separately authorised management Operations that may change policy, request catalogues,
  mappings, authority envelopes, or trusted approvers; and
- no-bypass evidence enumerating every covered external path capable of causing the privileged
  effects.

The authorisation rule remains three ordinary decisions rather than a new authority calculus:

```
request Execution authorised
AND Warden policy permits the exact mapping
AND interior Execution authorised
```

The decisions must remain distinguishable in evidence and failure reporting. A denied request, a
policy rejection, an interior target's authority denial, and a semantic failure after the interior
effect begins must not collapse into one undifferentiated failure; the Warden contract states how
each is represented and reported.

The Warden's standing authority should be the minimum enumerable envelope required by its request
catalogue. A generic shell, arbitrary path, unrestricted target selector, or opaque `run as Warden`
Operation is not a shortcut around that requirement: if such an Operation is deliberately exposed,
its broad authority and designation constraints must be explicit, and its confused-deputy risk is a
property of the architecture rather than an implementation detail.

Approval is request-bound by default. If an approval artifact can be reused by the requester as
authority at another target, it is no longer merely a Warden decision: it is a Capability or
Delegation and must obey those rules explicitly. A policy record, signed message, possession of a
response, or successful earlier request grants no authority by itself.

The sole entrance is a semantic surface, not necessarily one process. Replication or failover may
preserve one Warden Actor and lifecycle contract where that continuity is declared. Independent
Warden Actors are independent operational entrances and must be represented as such; they must not
be hidden behind one name to preserve a false sole-entrance claim. When the active Warden cannot be
established, the covered surface fails closed rather than falling back to direct privileged access.

Changing a Warden is itself privileged work. Policy, operation mappings, trusted approvers, and the
standing authority envelope may be established by domain bootstrap, deployment authority, or a
separately authorised management Operation, potentially exposed through another Warden. None may be
changed merely because the data plane can address the Warden or because the Warden could use its own
ambient authority to modify itself.

## Environment identification and mutual understanding

An Environment needs a portable way to identify itself at an external boundary. This requirement
does not make Environment a Base term. An Environment is not an Actor and cannot speak or prove its
own identity; a Gatekeeper presents its Protected Environment's reference and the contract under which
that reference should be interpreted. An ordinary Environment may be described by any authorised
Actor without turning that Actor into a Gatekeeper.

Five occurrences must remain distinct:

- **Identification:** the Gatekeeper states that it represents a particular Environment reference.
- **Understanding:** the peer confirms that it implements compatible Environment semantics and
  understands the presented Topology contract version and Profile closure.
- **Recognition:** the peer creates or associates a local Environment reference with the presented
  reference under explicit identity rules.
- **Trust:** the peer evaluates provenance, identity, attestation, and other evidence under local
  policy.
- **Authority:** the peer grants or derives Capabilities for the Gatekeeper through ordinary local
  admission and Delegation.

None implies the next. Successful parsing is not understanding; understanding is not recognition;
recognition is not trust; and trust is not authority.

A minimum Environment-boundary presentation should contain:

- the presented Environment reference and the Actor making the presentation;
- the Topology contract version and any required Profile identifiers;
- the Gatekeeper identity, direction, and disclosed Environment View;
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
reconstitution is domain policy and needs no cryptography: the Gatekeeper's lifecycle contract declares
which of Actor identity, authority, state, and in-progress work survive, under the reference rules
of Architecture 0.8 §9.1, and everything undeclared does not survive. Across domains, continuity
rests on receiver-owned pairing: at first admission the receiving domain mints its own durable
local identity for the peer, so continuity across reconnection is the receiver's recognition
decision rather than the peer's claim. Presented continuity claims and their evidence are ordinary
admission inputs under §24; attested federation may later strengthen them, but no new protocol is
required before the intra-domain and pairing forms are useful.

A Base-only peer may continue to treat the Gatekeeper as an opaque Actor boundary without understanding
Environment. A Profile that requires mutual Environment understanding may instead fail closed or
restrict interaction until a compatible Outcome exists. General-purpose, Host-assisted, distributed,
or other environment-aware Profiles may depend on `Topology`; the Static Embedded Profile need not.
A Host may construct and negotiate an Environment representation for a passive Base-only leaf
without transferring its Topology obligations to that leaf.

The conclusion is therefore:

> Environment identity is portable and explicitly negotiable at Gatekeeper boundaries, while Environment
> remains outside Base. A Profile that requires the outside system to understand the Environment
> includes `Topology` and requires an explicit successful understanding Outcome.

## Transparency is relational and multidimensional

`Transparent` and `opaque` are not global properties of an ordinary Environment. The same
Environment may be transparent through one Gatekeeper and opaque through another, and visibility has
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

A structurally opaque Gatekeeper may expose broad operational functionality through a stable façade. A
structurally transparent diagnostic Gatekeeper may reveal internal membership while granting no authority
to invoke anything. Visibility never implies reachability, and reachability never implies authority.

A Gatekeeper's **Environment View** is the projection describing these dimensions for one
relationship. Product interfaces may summarize a View as opaque, selective, or transparent, but the
portable record should retain the individual dimensions and any filtered, summarized, pseudonymous,
or conditionally disclosed identities. Most practical Gatekeepers will be selective rather than wholly
transparent or opaque.

A Protected Environment adds one stronger invariant: its exterior receives no View of the interior
except through a Gatekeeper. A Gatekeeper may still disclose a transparent administrative View to an authorised
audience, but that disclosure does not make the Protected Environment globally transparent.

## Information carried by an Environment and Map

The normative kernel should be slim while allowing expansive typed Attributes.

An Environment occurrence should minimally carry:

- a strongly typed local occurrence identity and optional presentation name;
- a declared or observed kind without treating the label as proof;
- the observer and source of each assertion;
- creation, validity, revision, and termination information;
- direct membership and typed Topology Relations;
- when protected, its known Gatekeepers and Wardens, boundary-crossing relations, privileged
  operation surfaces, Protection Plane, covered resources and crossings, enforcing boundary,
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

The Host creates a local ordinary Environment for the attachment and relates admitted functions to
it. The mouse remains Base-only or Static Embedded, and no Gatekeeper or Environment Component is required.
If Host policy places the attachment behind a Protected Environment boundary, the admitted function
Actor may be designated as a Direct Gatekeeper or a separate Gatekeeper may front it.

### Host-assisted smart mouse

The device has an internal composed Environment. A local management Gatekeeper can expose selected
internal Regions for repair or configuration, while an external input Gatekeeper presents only stable
pointer, button, battery, and configuration contracts. Internal transparency and Host-facing opacity
coexist. If the device claims a Protected Environment, every covered external interaction traverses
one of those Gatekeepers. If firmware replacement or protected calibration is available only through
typed management requests, the Actor performing those effects is also the Warden for that privileged
surface: the caller receives no firmware or device-management Capability. The device and Host still
make separate authority decisions.

### Remote organisational system

The remote system may be internally governed by different identity, policy, lifecycle, and
composition rules. A public Gatekeeper exposes one protected boundary surface; an administrative Gatekeeper
exposes another. The receiving system admits each Gatekeeper and derives local Capabilities according to
its own policy. Administrative Operations backed by authority unavailable to remote administrators
terminate at a Warden, which evaluates the request and initiates a separately authorised interior
Execution. The remote Environment cannot self-grant authority by claiming to be trusted, and an
administrator does not gain the Warden's authority by using its surface.

### Reconstituted service

A failed service is recreated on different machines. The new Environment occurrence may
`Reconstitute` the former one. A Gatekeeper may preserve a public contract and name, but preservation of
Actor identity, Capability validity, state, admitted work, and failure history requires explicit
lifecycle and authority rules. Topology lineage alone preserves none of them.

### Overlapping spaces

One database Component belongs simultaneously to a process Environment, a tenant Environment, a
physical-host Environment, a residency Environment, and a shared failure Environment. Those
memberships can inform different policies without creating five Component parents or five database
occurrences. Gatekeepers select the view relevant to each relationship. When any of these is protected,
laminar membership is required within its Protection Plane; independent Planes may intersect.

## Retained objection and resolution: Warden versus Gatekeeper and deputy

The objection is that Warden appears redundant: Brontide already names the generic deputy pattern,
and an Adapted or Synthetic Gatekeeper may expose an Operation backed by its own standing authority.
The overlap is real, but the concepts answer different architectural questions.

`Deputy` classifies authority behaviour under the invocation principle: any Actor deliberately
using its own authority in response to another Actor's request is a deputy. `Gatekeeper` records an
admitted Protected Environment crossing, while export fidelity records how faithfully a boundary
contract relates to interior contracts. `Warden` records a protective responsibility: one Guardian
is the declared sole operational entrance to a privileged surface and must constrain, correlate,
perform, filter, and account for effects initiated for less-privileged requesters.

A deputy is not necessarily a Guardian, a Gatekeeper may pass per-request authority without acting
as a deputy, and an Adapted or Synthetic Gatekeeper is not automatically a Warden unless the
protection contract assigns it the sole-entrance responsibility. Conversely, a Warden behind a
separate Gatekeeper remains a Warden even though another Actor admits the crossing. When one Actor
meets both definitions it must declare both roles; using one label to erase the other would hide
either crossing coverage or a standing-authority confused-deputy surface.

The Warden name is therefore retained because Authority Topology needs to identify bounded deputy
surfaces through which an Actor can cause effects beyond its directly held authority. The role adds
no Capability and changes no Delegation rule. It makes the operational entrance, standing authority,
request mapping, and no-bypass claim explicit enough to analyse and test.

## Retained objection and resolution: Sentinel as a universal observer

The objection was that Sentinel would be unnecessarily narrow if reserved for security or
assurance. Guardian already protects or represents in the general case; observation performed on
behalf of another participant may itself be protective or representative even when no suspicious
activity is involved. Under the broader reading, **Sentinel** names an Actor whose primary function
within a Watch is third-party observation and reporting, whether it watches an Environment,
Component, Actor, resource, interaction, health condition, performance signal, or domain-specific
activity.

That broader role has real advantages:

- it gives observation a consistent agent-oriented name alongside Actor and Guardian;
- it provides one place to declare observer identity, subject, provenance, freshness, retention,
  and audience across different kinds of subject;
- it avoids making security monitoring architecturally special when safety, health, integrity,
  availability, compliance, and ordinary representation may need the same observation mechanics;
- and it lets Sentinel mean an observer acting for a declared purpose rather than a product-specific
  telemetry collector or security appliance.

The counterargument was that any Actor may already observe through an appropriate Capability. The
resolution is that observation authority alone does not make a Sentinel. The distinguishing
semantics are primary third-party purpose and non-participation in the observed activity. An Actor
observing its own work or consuming data for an interaction in which it participates is an observer,
but not a Sentinel for that occurrence. Reporting, recording, correlation, and alerting remain
within the Watch; mitigation remains separate.

Three meanings of *universal* must therefore remain separate:

- **Universal applicability** means a Sentinel may watch any kind of explicitly declared subject.
  The current direction accepts this meaning.
- **Universal semantic classification** means every Actor whose primary function in a bounded Watch
  is third-party observation and reporting acts as a Sentinel, irrespective of purpose. The current
  direction accepts this meaning.
- **Universal visibility or presence** means a Sentinel can observe without subject-specific
  authority or must exist in every system. The current direction rejects both implications.

The present judgement therefore adopts the broader role without granting broader visibility. A
truly wide Sentinel is possible in the same sense that a `god` Component is possible: its Watch may
name all declared sources and occurrence classes in a domain, but the resulting authority,
information concentration, failure risk, exclusions, and unknowns remain visible properties of that
design. Brontide neither prohibits it nor treats it as universal in fact. Future evaluation concerns
the portable Watch Shapes and evidence needed to make these boundaries mechanically decidable while
leaving interpretation appropriate to the model and domain.

## Deliberately deferred detail

The direction does not need a deeper architecture taxonomy before implementation evidence exists.
In particular, it does not define Gatekeeper or Warden groups, their orchestration, one universal
descriptor, a wire protocol, or an implementation object graph. Those are extension-design and
implementation concerns provided they preserve the recorded boundaries.

A future `Topology` specification will still need concrete Shapes, identities, Map query and Dataset
representations, evidence formats, lifecycle records, and conformance tests. It will also need the
portable declaration of Gatekeeper type and export fidelity, the Plane dimension and enforcement-basis
vocabulary with same-basis detection, coverage and assurance evidence formats and the resolution-time
no-bypass check, alias-relation and receiver-owned pairing records, the intra-domain continuity
declaration, Warden request-surface and policy-evaluator Shapes, request-to-effect correlation,
standing-authority envelope and egress-filter declarations, privileged-surface no-bypass evidence,
and Sentinel watch scopes, finding Shapes, and response-separation contracts.
Distributed admission, cross-domain identity, attestation, side-channel claims, and persistence of
exported Actor identity remain owned by their respective specifications. These are protocol
obligations, not reasons to reopen whether Environment is a Component or where protection and
transparency reside.

## Recorded direction

Topology is a first-class architectural direction rather than a collection of placement Attributes:

- Environment, Protected Environment, Protection Plane, Topology Map, Gatekeeper, Warden, and
  Environment View remain outside Brontide Base;
- Guardian is the general recorded protective Actor role; Gatekeeper, Warden, and Sentinel are
  independent specialisations, and every family member acts only through authority it actually
  holds;
- Gatekeeper is the preventative specialisation at a Protected Environment boundary; Warden is the
  operational-deputy specialisation and sole operational entrance for its declared privileged
  surface; Sentinel is the primary third-party observer and reporter within a deterministic,
  purpose-bounded Watch and has no implicit response authority;
- a Warden authorises a typed outer request, applies its declared policy, and initiates a separately
  authorised interior Execution under its own independently established Capability; requester and
  Warden authority are never unioned, and the request neither admits nor elevates the requester;
- Gatekeeper and Warden remain distinct roles even when one Actor holds both: the former admits a
  covered crossing, while the latter performs a privileged effect as a visible deputy;
- Sentinel Watch deterministically bounds purpose, subjects, occurrence classes, sources, coverage,
  lifecycle, evaluator, outputs, and gaps while permitting declared domain- or model-specific
  interpretation;
- Event or Flow subscription alone does not create a Watch; a Sentinel may nevertheless use those
  mechanisms for observation and may use ordinary support Capabilities to persist, correlate,
  checkpoint, and report its findings;
- ordinary Environment identity is orthogonal to Component, Actor, Composition Region, and Authority
  Domain and may overlap other ordinary Environments;
- ordinary Environments have no Gatekeepers; describing, routing into, or exposing one does not
  create a protection boundary;
- Protected Environments are laminar within one Protection Plane and are opaque except through
  Gatekeepers;
- a Protected Environment with zero active Gatekeepers has no declared external communication and does
  not interact externally;
- a declared privileged operation surface with zero active Wardens permits no covered external
  privileged effect and has no direct-access fallback;
- every Gatekeeper is the specialised Guardian designated by a Protected Environment's protection
  contract as an allowed boundary participant;
- every Gatekeeper export declares one fidelity class — Direct, Deputised, Mediated, Adapted, or
  Synthetic — and reinterpretation is never presented as exposure;
- Adapted and Synthetic exports enumerate the standing authority that backs them;
- a Protection Plane is identified by its protection dimension and enforcement basis, and
  same-basis Protected Environments share one Plane;
- no-bypass coverage claims belong to the Protected Environment, are declared, enumerated, or
  statically verified, cover both boundary crossings and declared privileged operation surfaces,
  and name their exclusions; attestation is independent assurance evidence;
- peer recognition records attributable alias relations between Environment references and never
  merges occurrences;
- intra-domain reconstitution continuity is declared by Gatekeeper lifecycle contracts, and
  cross-domain continuity rests on receiver-owned pairing;
- attachment through a runtime-open Port across a Protected Environment boundary is a covered
  crossing and must terminate at or be performed through a declared Gatekeeper; the Port is not the
  Gatekeeper;
- an Environment groups Topology Nodes from the Composition-owned minimum floor, whose Relations
  begin every Map's relation vocabulary;
- protection holds fail-closed throughout Establishment, and a Gatekeeper admits nothing before the
  readiness point declared by the protection contract, while a Warden causes no covered privileged
  effect before its own readiness and policy prerequisites hold;
- export continuity across backing replacement is declared by Gatekeeper lifecycle contracts, never
  inferred from Gatekeeper or export identity;
- Gatekeepers present portable Protected Environment identities, and peers confirm compatible
  understanding through explicit versioned Topology Outcomes rather than inference;
- visibility remains Gatekeeper- and audience-relative, multidimensional, and independent of
  authority; and
- explicit Actors, Capabilities, Delegations, Mediation, lifecycle, and evidence support every claim
  that a Gatekeeper actively enforces rather than merely describes a boundary and every claim that a
  Warden is the sole operational entrance to a privileged surface.

The competing hypotheses remain above as decision rationale. The Environment/Gatekeeper split
and the Warden operational-deputy role are no longer open architecture questions in this design
direction.
