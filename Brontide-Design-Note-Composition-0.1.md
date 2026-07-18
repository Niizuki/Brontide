# BRONTIDE

## Design Note: Composition and Components

**Status:** Work-in-progress design note, version 0.1
**Originally extracted from:** Brontide Architecture 0.6, §18.1

**Current architecture context:** [Brontide Architecture 0.7](./Brontide-Architecture-0.7.md),
§18.1; the current architecture document retains a summary section under the same number. The
Mediation section is recorded as a non-ratified direction by the 0.7 change plan.
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

A Component with an Brontide-visible boundary declares, as applicable:

- the Profiles, Extensions, and Domain Vocabularies it provides and requires;
- the Operations it provides or consumes, including their input and output Shapes;
- required Declared Fragments and open-Shape fragment policy;
- the Corpora it understands, the roles it can perform concerning them, and the Store contracts
  required by those relationships (§18.2);
- the Actor relationships and authority requirements visible at its boundary;
- versioned Attribute sources and recursive Constraints relevant to placement or binding;
- Composition and Activation Parameters exposed by the definition; and
- its binding model and, where applicable, Slots, Classes, and lifecycle limitations that narrow
  its composition or substitutability claims.

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

Architecture 0.7 distinguishes two binding stages:

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
Parameter implicitly; forwarding or deriving a value is explicit. Whether one Parameter may
declare, expand, or parameterise another definition, and the exact treatment of comments or
annotations in a future definition language, remain open (§33). Architecture 0.7 admits
Shape-composed Parameter values but does not ratify parameter-generating parameters.

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
  deployment.
- A **runtime-bound Component** may be selected or attached while the containing composition is
  active. Runtime binding alone does not imply that the Component can later be detached or
  replaced.

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
constructed at compile time, link time, deployment, or runtime. It may exist as explicit data or be
compiled completely into direct calls and static storage. A Binding Plan establishes, as applicable:

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
the containing composition unless a stronger guarantee is declared. A slot is a logical
composition boundary; it does not imply a physical connector, memory location, process boundary,
or loader.

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
realises Distribution; an **Arbiter** realises Arbitration. Erased realisations carry no
category noun.

### Declaration level and status

Mediation declarations are admitted at composition level only — a property of a binding.
Whether a Component may additionally require Mediation properties of its environment ("binds
only through a Selection with sticky affinity") remains open; it is more expressive but in
tension with §6.12.

The storage Router remains the first instantiation, and ratification waits on Reference/Minimal
storage evidence. The Event mediator is evaluated against this contract when Event Distribution
is drafted — it combines Distribution with Selection-like subscription filtering, and the two
roles may deserve separation there as they already have in storage. An Actor legitimately
holding both a direct and a mediated grant experiences Arbitration as advisory, which is
correct under §6.9 — "everything flows through the Arbiter" is therefore a claim about grant
policy, verified in the delegation graph, not a structural guarantee.
