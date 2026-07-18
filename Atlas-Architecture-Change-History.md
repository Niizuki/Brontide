# ATLAS

## Architecture Change History (0.2 → 0.6)

This document retains the historical change records extracted from the Atlas Architecture
specification, preserved verbatim from Architecture 0.6, where they appeared as §35–§38. The
current architecture document records only its own diff.

Section references of the form §N refer to section numbers as they stood in the architecture
version that introduced the change.

---

## 35. Changes from 0.2

This section records the changes introduced since Architecture 0.2.

**Changes in 0.3:**

- **§7 and §13–§14 — Operation and Execution separated.** Operation now names the stable semantic
  contract, with one input Shape and one independent output Shape. Execution is the Base occurrence
  representing one concrete attempt to execute an Operation. Correct prose may say “execute an
  Operation”; the specific run, including any identity and metadata, is an Execution. Event remains
  the immutable assertion form, and Outcome terminates an identifiable Execution or activity.

- **§7, §13, and §16 — Interaction moved from ontology to composition.** Interaction is no longer
  a Base term, occurrence, superclass, or “form.” `Interaction 1` is the first standard reusable
  Declared Fragment. Execution and Event include it explicitly, Outcome composes it through Event,
  and extension occurrences may include it for attribution, optional identity, correlation,
  causation, origin, and temporal placement. The
  Interaction fence excludes semantic name, target, values, authority, result, effect, truth,
  delivery, ordering, replay, persistence, and lifecycle semantics.

- **§10 and §13 — Capability recognition made explicit.** A Capability is not only authority: it
  is a target-recognised grant authorising Operations and transitively binding to their input and
  output Shapes and required Declared Fragments. That closure is not duplicated in the Capability.
  Recognition does not promise holder understanding, availability, or success, and unknown or
  stronger fragments cannot broaden authority.

- **§6.8 and §30 — Linen introduced as an independent full-stack implementation.** Fabric remains
  the first practical showcase. Linen is defined as a lean, independently implemented stack whose
  purpose is to test composability and component interchange with Fabric. Shared conformance is
  necessary but not sufficient: components implementing the same declared contracts should
  interchange without depending on either implementation's private types or conventions.

- **§7 and §16 — Shape added to Atlas Base.** Architecture 0.3 identifies eight Base terms. Shape
  is a named, explicitly versioned abstract structural contract, never the value, object, memory,
  or bytes. It is Base because independent implementations cannot
  share those semantics while disagreeing about value structure. Shape does not prescribe a wire
  encoding, reflection system, object model, allocation strategy, or language type system, and a
  fixed compile-time representation satisfies the Embedded Test.

- **§16.1–§16.2 — Shape identity and additive versioning defined.** Shape references contain a
  canonical Shape name and positive integer version. Same-name versions form a monotonic,
  backward-compatible lineage: later versions may add optional structure but cannot remove,
  rename, retype, require, or reinterpret existing fields. Breaking change requires a new
  canonical Shape name. Coincidental structural similarity does not create compatibility.

- **§13.1 and §16 — Operation inputs, outputs, and Shape fragments defined.** Every Operation
  has one complete input Shape and one separate, independently evolving output Shape; `Unit 1`
  represents either side when it carries no value. A fragment is any subset or projection of a
  complete Shape, but only named, versioned, and specified Declared Fragments carry portable
  contract meaning. Declared Fragments may be authored attachments to an open host Shape or reusable
  structures explicitly included by unrelated Shapes. Inclusion does not make the host Shapes
  compatible.

- **§16.3–§16.4 — Open Shape composition and projection defined.** An open record Shape may carry
  several non-overlapping authored fragments. A component accepting the canonical Shape MUST
  accept a valid composed value, process the canonical projection, and ignore unsupported
  fragments without claiming their semantics. An authored fragment may enrich but never condition,
  constrain, or reinterpret canonical fields; stronger semantics require the fragment explicitly.
  Closed Shapes reject authored fragment attachment where exact structure is part of the contract.

- **§10, §13–§14, §21, §23, and §29 — Shape integrated across existing semantics.** Constraint
  values declare Shapes and fail closed when compatibility cannot be established. Executions carry
  input values conforming to the Operation's input Shape; successful Outcomes may carry `result`
  values conforming to the output Shape; diagnostics use separately shaped `details`. Domain
  Vocabularies declare accepted Shape and Declared Fragment versions. Ratification freezes Shape,
  fragment, and field meanings. Conformance now includes version, fragment composition,
  projection, unknown-fragment, and incompatible-redefinition tests.

- **§7, §18, and §19 — Base membership separated from prevalence.** Base membership depends on
  necessity to the smallest meaningful Atlas system and the Embedded Test, not expected deployment
  frequency. Shape enters Base under that test; Transaction remains an extension despite likely
  broad use. A future convenience Profile may collect a stable general-purpose bundle, but 0.3
  neither defines nor reserves one, and such a Profile has no privileged status.

- **§19 — Generic extension directions refined.** `Workspace`, `State`, and `Transaction` are
  recorded as provisional generic directions. `Structured Data` is rejected as an Architectural
  Extension direction in favour of database-agnostic `State`, Base Shape, `Resource`, and provider
  vocabularies. Presentation and Workspace are application-facing facilities rather than private
  browser or shell machinery.

- **§20 — Profiles compose transitively.** A Profile may require another Profile and declares its
  direct dependencies through the plural `uses:` block. Conformance expands the acyclic dependency
  closure, so Profiles do not repeat inherited extension and vocabulary lists. Interactive
  Application, Web, and Database are recorded as deliberately incomplete work-in-progress Profile
  directions; the Database direction standardises the integration seam, not provider operations.

- **§22.3 — Declaration prefix blocks replace dot-relative notation.** Grouped declarations use
  explicit `within` prefix blocks; every line expands from the block's stated prefix alone, so
  inserting, removing, or reordering lines never changes another line's expansion. The
  dot-relative notation of earlier drafts is rejected and recorded as design history. Prefix
  blocks are document notation only: Capabilities, signatures, hashes, wire representations,
  discovery records, conformance tests, and ratification records use expanded canonical names.

- **§10–§35 — Terminology aligned.** Formal prose consistently uses Operation for the contract,
  Execution for a concrete attempt, Capability for the recognised grant, Constraint for formal
  attenuation, Declared Fragment for portable fragment contracts, and Genesis occurrence where the
  occurrence need not be an Event. `Runtime` replaces the provisional extension name `Execution`,
  avoiding collision with the Base term.

- **Sections from Shape onward renumbered.** The new Shape section is §16. The previous §§16–§34
  become §§17–§35, and all internal references and Contents entries move with them.

Relative to 0.2, Shape and Execution enter Base while Interaction becomes a standard Declared
Fragment rather than a Base term. Architecture 0.3 therefore contains eight Base terms and retains
the two scoping concepts, Authority Domain and Genesis.

## 36. Changes from 0.3

This section records the changes introduced since Architecture 0.3. Section 35 is retained as the
historical diff between Architectures 0.2 and 0.3.

**Changes in 0.4:**

- **§18.1 — Scale-independent Component terminology introduced as work in progress.** A Component
  is a bounded unit of composition declaring provided and required Atlas contracts. It may range
  from a library or device to a service, data centre, or recursively composed environment. A
  Component is distinct from an Actor and gains no authority merely by being loaded or bound.

- **§18.1 — Static binding, runtime binding, replacement, and hot swapping distinguished.** A
  Hot-swap Host Component exposes a Hot-swap Slot for a declared Hot-swap Class; a Hot-swappable
  Component declares conformance to that Class and its candidate-side obligations.
  Hot-swappability is a joint operational claim, not an intrinsic property inferred from shared
  Operations. The Slot and Class contracts must define Actor continuity, authority establishment,
  state handoff, in-progress work, interruption, failure, and rollback semantics. `Plugin` remains
  host-specific terminology rather than the scale-independent architectural category.

- **§18.1 and §33 — Atlas Portable Binding proposed as the default Component seam.** A scoped
  Binding Plan may be established statically or dynamically and records contract, authority,
  representation, resource ownership and lifetime, synchronisation, delivery, and replacement
  decisions. The candidate portable binary realisation uses compact framing and schema-guided CBOR
  for inline shaped values while supporting referenced shaped resources for pooled, shared,
  device-local, registered, or other specialised data paths. Native and direct bindings may lower
  the same plan without materialising a generic object or serialised Interaction on the hot path.
  Base defines Shape and authority semantics but contains no universal mapping engine;
  representation mapping belongs to binding machinery, while semantic adaptation must remain an
  explicit Adapter Component or attributable declared transformation.

- **§5, §18.1, and §32 — Remote services placed inside the Component model.** `Local` and `remote`
  are treated as observer-relative projections of richer selection characteristics, including
  topology, authority-domain boundaries, latency, cost, capacity, availability, and failure domain.
  A Component descriptor may publish such characteristics, but self-description remains an
  attributable claim rather than proof; hosting Actors, guardians, measurements, and attestation
  may provide independent values. Implementations may still use `local` and `remote` as useful
  lossy projections for user interfaces, summaries, and coarse policies, without treating them as
  sources of unstated guarantees. Hot swapping, device replacement, service failover, and
  data-centre cutover deliberately share one binding model without becoming identical concepts.

- **§22 — Authored qualification made explicit.** Unqualified canonical names are reserved for
  ratified Atlas concepts. Portable non-standard concepts use
  `AuthorityPath:ConceptPath` regardless of whether they are compiled statically or loaded
  dynamically. Authorship is distinguished from occurrence Origin, and Capability grant identity
  remains distinct from authored semantic definitions.

- **§16.6 — Enrichment recorded as a work-in-progress design direction.** Enrichment is proposed
  as an addition-only composition mechanism that supplies previously absent Shape fragments from
  information already available to the composition. It may copy, project, reshape, combine, or
  deterministically derive values, but may not replace existing information or hide Capability
  invocation and other operational acquisition.

- **§16.6 — Targeted and ambient Enrichment distinguished.** Targeted Enrichment declares an added
  fragment available at a named consumer, Operation boundary, or bounded scope. Ambient Enrichment
  declares availability within a broader explicit scope. Neither necessarily specifies a physical
  route through intermediate modules. Consumer requirements, shadowing, conflict, confidentiality,
  provenance, and expiry semantics remain deliberately open, and ambient availability is not
  accepted as implicit global context.

- **§16.6 — Global storage separated from ambient availability.** A globally reachable store is
  permitted as an Atlas participant or future Resource and State facility, but reading it remains
  an Operation with explicit authority, failure, latency, consistency, and Outcome semantics. The
  draft distinguishes storage, discoverability, authority, scoped availability, and declared
  consumption. An obtained value may feed an Enrichment without making store access implicit.

- **§16.6 — Enrichment restricted to information, not authority.** Enrichment concerns Shapes and
  Declared Fragments and cannot create, issue, derive, delegate, transfer, bind, broaden, or combine
  Capabilities. Capability representations carried as shaped information grant no authority. A
  possible Capability-binding or authority-provisioning mechanism is recorded as separate future
  authority-composition work subject to holder, recognition, Delegation, Constraint, mortality,
  and explicit-presentation rules.

- **§16.6 — Value propagation introduced for investigation.** Where an explicit bounded route
  exists, a composition may preserve and transport typed fragments across modules that neither
  consume nor semantically expose them. Propagation must not be treated as a universal property of
  Atlas systems. Parameter threading, carrier structures, contextual storage, attached fragments,
  and distributed forwarding remain possible implementation strategies. Branches, joins, Flows,
  dynamic calls, conflicts, and cross-domain transport remain unresolved.

- **§16.6 and §33 — Systems are not necessarily topological.** Atlas does not assume a fixed call
  graph, pipeline, or global topology. The system is primarily a collection of Actors or modules
  exposing Operations; graphs are particular Execution traces, deployment compositions, Flows,
  workflows, dependency views, or diagnostic representations. The draft does not yet select the
  Atlas relationship that should bound propagated information when direct dynamic invocation means
  no single static path exists.

- **§33 — Open questions expanded.** Enrichment, ambient scope, propagation, hidden dependencies,
  global-store boundaries, possible Capability binding, provenance, expiry, confidentiality,
  conflict resolution, and experimental validation across Fabric and Linen are now tracked
  explicitly.

Architecture 0.4 makes no change to the eight Atlas Base terms and does not ratify Enrichment,
value propagation, the Component model, or the proposed Atlas Portable Binding. The additions are
intentionally marked as work in progress so that static, dynamic, embedded, and distributed
composition models can be tested before normative semantics are chosen.

## 37. Changes from 0.4

This section records the changes introduced since Architecture 0.4. Sections 35 and 36 are retained
as the historical diffs from Architectures 0.2 and 0.3 respectively.

**Changes in 0.5:**

- **§6.12 — Simple participation and sophisticated composition made an explicit principle.** A small
  module may expose one Operation and its Shapes without understanding persistence, identity,
  networking, scheduling, or acceleration. A surrounding composition may add those facilities under
  declared contracts. Purity, determinism, replay safety, batchability, vectorisability,
  relocatability, and accelerator compatibility remain explicit claims rather than inferred
  properties.

- **§6.13 and §18.2 — Optional system-native services and boxed applications defined.** A
  general-purpose Atlas environment may offer Event, persistence, State, Identity, Presentation,
  Workspace, Web, scheduling, observability, compilation, and accelerator Components. Applications
  may adopt them incrementally, supply private alternatives, expose only a narrow Atlas boundary,
  or remain conventionally hosted opaque boxes. System participation provides interoperability and
  inspection; it is not a condition of application legitimacy.

- **§18.2, §27, and §32.1 — Application/system entanglement described as reciprocal composition.**
  Applications may consume system Components while contributing Operations back to the environment.
  Database, identity, browser, Web, presentation, scheduling, and related facilities are framed as
  replaceable contracts and providers rather than mandatory monoliths. The system/application
  boundary becomes a matter of composition, policy, authority, and opacity while preserving the
  fully boxed alternative.

- **§6.14 and §32.2 — Developer-trust guardrails added.** Semantic portability must not hide
  distribution, representation, placement, retry, copy, or failure facts. Optionality must not
  become nominal interoperability or provider lock-in through tooling pressure. Dependency strength,
  semantic adapters, execution explanation, operational boundaries, and accelerator eligibility
  must remain visible.

- **§30.1 — A decisive Fabric/Linen demonstration framed.** The proposed collaborative
  image-processing workspace begins as a small local CPU composition, adopts system services
  incrementally, substitutes providers visibly, interchanges Fabric and Linen Components, and moves
  an explicitly eligible transformation to a GPU or other accelerator without changing its semantic
  Operation. The final mixed-stack workflow is intended to prove that Atlas is neither Fabric nor
  Linen.

- **§33 — Open questions expanded.** System-service discovery and dependency strength,
  boxed-application tooling, structured execution explanation, and portable optimisation properties
  are now tracked explicitly for Fabric/Linen experimentation.

- **§34 — Summary updated.** The summary now records optional native services, boxed applications,
  progressive sophistication, accelerator eligibility, operational legibility, and the staged
  demonstration target.

Architecture 0.5 makes no change to the eight Atlas Base terms. It does not ratify a system-service
Profile, execution-explanation model, optimisation-property vocabulary, Enrichment, value
propagation, Component model, or Atlas Portable Binding. The new material constrains the larger
direction and identifies experiments from which later normative specifications may be derived.

## 38. Changes from 0.5

This section records the changes introduced since Architecture 0.5. Sections 35, 36, and 37 are
retained as the historical diffs from Architectures 0.2, 0.3, and 0.4 respectively.

**Changes in 0.6:**

- **§1, §6.15, and §34 — Persistent information made independent of Components.** The architecture
  now states that Components operate on persistent information without inherently containing or
  owning it. Corpus authorship, Dataset custody, Store provision, authority, and default management
  are separate relationships. Component removal or replacement does not itself delete Datasets.

- **§16.1 — Recursive Shape composition clarified.** One root Shape may compose arbitrarily many
  Shape definitions through its fields and constituents. Multiplicity of Shape definitions belongs
  to Shape composition; multiplicity of values belongs to sequence Shapes or Corpus Forms.

- **§18.1 — Parameters introduced as work-in-progress composition terms.** Shape-described
  Parameters are bound at Composition or Activation. Composition Parameters may shape resolved
  architecture; Activation Parameters fill declared resource slots but may not introduce new
  structure. Effective values, defaults, context sources, scope, and provenance remain explicit,
  and ordinary application configuration remains distinct.

- **§18.1 — Attributes grounded in exact Atlas Operations.** An Attribute is no longer a free-
  floating label. Its source identifies the Operation, vocabulary claim, result Shape and version,
  result path, and ordinary Capability requirement through which the value is obtained. Definitions
  normally constrain stable advertised characteristics; dynamic observations do not silently
  trigger recomposition or rebinding.

- **§18.1 — Recursive Definition Constraints introduced.** Shape-typed atomic comparisons support
  equality, ordering, range, membership, containment, subset, and intersection as applicable.
  `AllOf`, `AnyOf`, and `Not` recursively contain other Constraint expressions without a fixed
  architectural nesting limit. The model selects and validates outside authority while preserving
  the Base narrowing algebra when carried by a Capability.

- **§18.1 — Component selection characteristics revised.** Topology, locality, latency, cost,
  capacity, availability, residency, and related selection values are now expressed through
  attributable capability-derived Attributes and recursive Constraints. `Local` and `remote`
  remain useful observer-relative projections without unstated guarantees.

- **§18.2 — Corpus and Dataset introduced provisionally.** A Corpus is an authored, versioned
  definition of the intent, organisation, compatibility, lifecycle, and storage roles of an
  independently addressable body of information. A Dataset is one concrete body conforming to a
  Corpus. The names are adopted for current design work while their remaining terminology concerns
  are recorded explicitly.

- **§18.2 — Information-integration ladder defined with an explicit asymmetry note.** App-Level
  information remains outside the Corpus model for compatibility; Opaque Corpora provide Atlas-
  managed identity and lifecycle without content interpretation; Structured Corpora expose Form and
  Shape semantics. App-Level is acknowledged as a practical comparison tier rather than a Corpus
  kind.

- **§18.2 — Corpus Forms established as a mostly closed candidate set.** Opaque, Record,
  Collection, Map, Graph, Journal, and Stream are defined by logical intent rather than physical
  storage. Document, table, tree, and time series are represented as semantic classifications or
  constrained uses of those Forms rather than new base Forms.

- **§18.2 — Dataset lifecycle and Component-Corpus roles defined.** Durable, Rebuildable, Cached,
  and Temporary lifecycle characteristics are separated from Form. Originator, Reader,
  Contributor, Curator, Manager, Migrator, Rebuilder, Importer, Exporter, and Validator describe
  Component compatibility without granting Dataset authority or reintroducing a primary owner.
  Corpus migration and software rollback must account for Dataset compatibility.

- **§18.2 — Corpus Store roles and explicit absence semantics introduced.** A Corpus may declare
  several required or optional logical Store roles to partition Dataset content by purpose. Each
  role binds to one logical Store, several roles may share a Store, and every optional role declares
  `UseRole`, `Discard`, `Recompute`, or `DisableFeature` behaviour rather than relying on implicit
  fallback.

- **§18.2 — Store, Store Relationships, and Router separated.** A Store is a concrete logical
  retention resource whose Operations and Attributes remain inspectable independently of physical
  mounts or devices. Mirror and Backup are limited candidate static relationships between Stores.
  A Router presents a Store-compatible contract while owning policy-driven fallback, tiering,
  sharding, jurisdictional routing, migration, or other conditional delegation across Stores.

- **§18.3, §19, §20.1, §21, §27, and §32.1 — Existing composition directions integrated.** The
  optional-system-services section moves from §18.2 to §18.3 without losing its boxed-application
  alternative. Extension, Database Profile, Domain Vocabulary, operating-system, and reciprocal
  application composition text now distinguishes Corpus and Store from runtime State, Resource,
  Transaction, Persistence, and database-provider machinery.

- **§22.2 and §22.4 — Strict and NonStrict notation introduced.** Strict declarations use expanded
  canonical identities and explicit version and binding fields; NonStrict documents permit only
  deterministic shorthand that normalises to the same model. Candidate typed member paths extend
  the existing `AuthorityPath:ConceptPath` grammar, while versions remain separate claims rather
  than `@version` name suffixes. Architecture 0.6 declares itself NonStrict.

- **§23 — Versioning coverage extended.** Corpus, Store-role, Attribute-source, and Parameter
  definitions are brought under authored qualification and immutable ratified-name rules while
  leaving Corpus migration and compatibility rules provisional.

- **§33 — Open questions expanded.** Corpus terminology and Forms, Dataset migration and custody,
  multi-role Store failure semantics, Mirror and Backup guarantees, Router transparency,
  Attribute sourcing, recursive Constraint evaluation, dynamic observations, Parameter scope and
  composition, comments and annotations, and Strict canonical member identity are now tracked
  explicitly for Fabric/Linen experimentation.

- **§34 — Summary updated.** The summary now records Parameters, capability-derived Attributes,
  recursive Definition Constraints, the Corpus/Dataset/Store/Router model, the information-
  integration ladder, and notation strictness.

Architecture 0.6 makes no change to the eight Atlas Base terms. It does not ratify Corpus,
Dataset, Store, Store role, Store Relationship, Router, Attribute, Definition Constraint,
Parameter, a Storage vocabulary, a Persistence extension, the Component model, or the Atlas
Portable Binding. These additions are explicit work-in-progress composition and persistent-
information directions whose exact descriptors and conformance rules require independent Fabric
and Linen evidence before ratification.
