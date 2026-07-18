# BRONTIDE

## Design Note: Persistent Information — Corpus, Dataset, Store, and Router

**Status:** Work-in-progress design note, version 0.1
**Extracted from:** Brontide Architecture 0.6, §18.2; the architecture document retains a
summary section under the same number.
**Scope:** Records design directions. Nothing in this note enlarges Brontide Base,
ratifies a Storage vocabulary or Persistence extension, requires a database, or makes
Structured integration mandatory.

References of the form §N refer to the Brontide Architecture specification.

---

Brontide needs a first-class account of persistent information without turning one filesystem,
database, object model, or application-ownership convention into the architecture.

The provisional model separates five questions:

```
Shape       What is the structure of one value?
Corpus      What does an independently addressable body of information mean?
Dataset     Which concrete body of information is this?
Store role  What placement and lifecycle purpose does one part of that Dataset have?
Store       Which concrete logical resource retains that part?
```

Components declare how they can operate on Corpora and what Store contracts they require.
Capabilities govern what Actors may actually do to Datasets and Stores. None of these terms grants
authority merely by being declared or bound.

> **Terminology status.** `Corpus` is the current preferred term and is stable enough for ongoing
> design work, but remains provisional before terminology freeze. Its useful meaning is a coherent
> body of information, not specifically a textual collection or a large dataset. `Dataset` is the
> current preferred term for a concrete Corpus instance; it need not be tabular, analytical, or
> machine-learning data. `State` remains the runtime or observable condition of an Actor,
> Component, activity, or Resource. Runtime state persisted as independently addressable
> information may become a Dataset, but Corpus is not a synonym for State.

## Information-integration ladder

Architecture 0.6 recognises a practical three-step integration ladder:

1. **App-Level information** remains privately managed by conventional software outside the
   Corpus model. Brontide may see only files, directories, sandbox storage, or external Operations.
   Generic backup or containment remains possible, but logical Dataset identity, migration,
   cross-Component reuse, and selective lifecycle management are unavailable unless the software
   supplies them. This compatibility level is discouraged for new general-purpose software where
   a Corpus is reasonable, but remains necessary for legacy, specialised, and deliberately private
   implementations.
2. An **Opaque Corpus** defines Brontide-visible Datasets whose internal content Brontide cannot
   interpret through Shapes. Brontide can still manage identity, Store bindings, authority,
   provenance, size, integrity, lifecycle, compatibility, backup, migration as a whole, and
   deletion. Opaque is the preferred minimum for new software that cannot reasonably expose its
   internal representation.
3. A **Structured Corpus** defines a logical Form and describes its addressable values using
   Shapes. It enables structural validation, partial access, semantic migration, selective backup
   and synchronisation, generic inspection, indexing, enrichment, conflict handling, and reuse by
   independently implemented Components where the governing contracts and authority permit them.

This ladder is intentionally asymmetric. App-Level information is not a Corpus, while Opaque and
Structured are Corpus kinds. The grouping is retained because it communicates the useful migration
path from private application storage to system-managed semantic information. The terms `App-Level`
and `Corpus`, and the exact placement of this asymmetry, remain explicitly open to later rename;
the model must not wait on a perfect label.

Structured does not mean important, and Opaque does not mean disposable. A proprietary game save
may be Opaque and Durable; a Structured search index may be Rebuildable; a thumbnail may be Opaque
and Cached. `Document` describes a possible user-facing semantic classification, not the universal
concrete-instance term or a Corpus Form.

## Corpus

A **Corpus** is a versioned architectural definition of a meaningful, independently addressable
body of information. It defines semantic intent, kind, logical Form where Structured, participating
Shapes, compatibility and migration expectations, lifecycle classes, and Store roles independently
of any particular Component, Dataset, or Store.

Every Corpus has a definitional owner expressed through the authorship rules of §22. For example,
the canonical Corpus name `Brontide:Mail.Accounts` states that `Brontide Minimal Stack` owns and versions that
definition. It does not state that Brontide Minimal Stack owns, stores, may access, or exclusively manages every
conforming Dataset. An individual, organisation, implementation, standards body, or another
recognised namespace authority may own a Corpus definition. Another author may define a distinct
derived or adapted Corpus but may not publish a successor under the original owner's Authority
Path without that authority.

Corpus versions follow the existing Brontide separation between name and version (§23):

```
corpus-name: Brontide:Mail.Accounts
corpus-version: 3
owner: Brontide Minimal Stack
```

`Brontide:Mail.Accounts@3` is permitted as explanatory shorthand only if a future notation
specification defines that spelling; `@3` is not part of the canonical name in Architecture 0.6.
Changing the semantic meaning of a ratified Corpus requires a new canonical name. The exact
additive, migratory, and compatibility rules for Corpus versions remain provisional because a
Corpus lifecycle may include reversible or irreversible data migration without reinterpreting its
unchanged semantic fields.

A Corpus MUST declare its concurrent-access semantics — what several simultaneously authorised
Actors may assume when operating on one Dataset. The declaration may be as modest as
single-writer with enforcement left to authority, or external coordination required; it may not
be absent. Following the pattern of §10.3 withdrawal declarations, stating the obligation is
required even while richer coordination, State, and Transaction semantics remain open.

A Component may introduce or author a Corpus, but authorship does not make the Corpus part of the
Component. The relationships are many-to-many:

- one Corpus may be understood by many Components;
- one Component may understand many Corpora;
- one Corpus may have many Datasets; and
- one Dataset may be accessed by many Actors through many Components under explicit authority.

## Shape and Corpus Form

Shape and Corpus are orthogonal architectural levels. A Shape defines the structure of a value. A
Corpus gives a persistent or independently addressable body of information its intent, identity,
organisation, lifecycle, and compatibility expectations. The same Shape may participate in many
Corpora and interactions; one Corpus may compose many recursively defined Shapes (§16.1).

An Opaque Corpus has the single Form **Opaque**. Opaque does not mean one blob: a conforming Dataset
may be a file, directory tree, encrypted vault, proprietary database, game save, or coordinated set
of objects. Brontide manages the Dataset as a whole without addressing its internal values.

A Structured Corpus chooses one of the following base Forms:

- **Record** — one structured root value described by a Shape. Nested fields and sequences do not
  make it several Records; Brontide treats the root as one logical unit.
- **Collection** — a set or sequence of independently identifiable elements described by one root
  element Shape. Ordering, element identity, duplication, and merge behaviour are explicit rather
  than inferred.
- **Map** — entries addressed through structured key Shapes with value Shapes. Key-based addressing
  is part of the Corpus contract, not merely a storage implementation detail.
- **Graph** — nodes and relationships described by Shapes. A tree is a constrained Graph; no graph
  database is implied.
- **Journal** — an ordered, normally append-oriented durable history whose entry identity, ordering,
  and mutation rules are semantically significant.
- **Stream** — a temporally ordered sequence whose ongoing production and consumption are central
  to its meaning. A Stream may be durable, retained, buffered, or transient; its distinction from a
  Journal is intent, not whether bytes happen to persist.

The candidate list is mostly closed: Architecture implementations MUST NOT invent pseudo-standard
Forms merely to advertise a storage engine. A later Brontide specification revision may add a Form
after showing that it cannot be represented honestly through these Forms plus Shapes and
Constraints. Table is normally a Collection of homogeneous Records, tree a constrained Graph, and
time series a Stream or ordered Collection. Document is semantic purpose and may use any Form.

A Structured Corpus names one root Shape for each declared value position. That Shape may compose
other Shapes recursively. Collection therefore means several values conforming to its element
Shape, not a special escape hatch for "multiple Shapes."

## Dataset and lifecycle

A **Dataset** is a concrete, independently identifiable body of information conforming to one
Corpus version. It records its Dataset identity, Corpus reference, Store-role bindings, custodian,
authority relationships, lifecycle metadata, and provenance as applicable.

A Capability governing Dataset access designates the Dataset, its Store roles, or its bound
Stores as targets under the ordinary designation rules of §10.2: resolved at grant time, bound,
and recorded. Datasets are created dynamically, so Dataset creation authority is an instance of
the open Genesis-versus-authorised-issuance question (§12, §33): an Originator's authority to
create a Dataset is attributable issuance by an already authorised Actor, not domain-level
Genesis, and the future `Resource` extension must keep that distinction explicit.

Dataset identity is a property of the Dataset record itself, independent of the content of any
single Store role. A Corpus declares which Store roles are identity-bearing. The failure or
absence of a Store bound to a role that is not identity-bearing does not fork, duplicate, or
destroy the Dataset; it makes that role's content unavailable under the role's declared absence
or failure behaviour. Exact atomicity and failure semantics across several roles remain open
(§33), but Dataset identity does not depend on them.

The definitional owner of a Corpus, custodian of a Dataset, provider of its Store, Actor authorised
to access it, and Component selected as its default manager are separate relationships. A user may
be custodian of a Dataset conforming to a vendor-authored Corpus. `DefaultManager` is a current
composition or policy choice, not intrinsic ownership and not a `PrimaryManager` role hidden in the
Corpus.

A Corpus may classify its whole Dataset or individual Store-role content as:

- **Durable** — preservation is part of the intended lifecycle;
- **Rebuildable** — loss is acceptable if a declared Component can reconstruct it;
- **Cached** — replaceable performance data with explicit invalidation or expiry semantics; or
- **Temporary** — information whose lifecycle intentionally ends with a declared scope.

These are lifecycle characteristics, not Forms. Sensitivity, secrecy, document meaning, operational
importance, residency, retention, and custody remain separate dimensions expressed through Corpus
semantics, authority, or capability-derived Attributes.

Corpus version transitions declare compatibility, required migrations, reversibility, and which
Components can validate, migrate, or rebuild affected Datasets. Software rollback MUST NOT be
reported as complete when the prior Component cannot read the migrated Corpus version. A lifecycle
plan should distinguish reversible data migration, irreversible migration requiring a snapshot or
explicit acknowledgement, and Rebuildable content that may simply be discarded and regenerated.

Removing software and removing information are therefore independent decisions. Uninstalling a
Component may remove its code, private App-Level information, and selected Cached or Temporary
Datasets according to explicit policy; it does not imply deletion of Durable Datasets on which the
Component operated.

## Component-Corpus relationships

A **Component-Corpus Relationship** declares that a Component understands or can perform named
roles concerning a Corpus. It is a compatibility and behaviour claim, not a grant of authority over
any Dataset.

The candidate role vocabulary is:

- **Originator** — can create a new Dataset conforming to the Corpus;
- **Reader** — can interpret and read conforming information;
- **Contributor** — can add or modify content within declared limits;
- **Curator** — can organise, merge, or remove content;
- **Manager** — can perform broader declared Dataset lifecycle operations;
- **Migrator** — can transform between named Corpus versions;
- **Rebuilder** — can reconstruct Rebuildable Datasets or Store-role content;
- **Importer** — can construct a Dataset from a declared external representation;
- **Exporter** — can produce a declared external representation; and
- **Validator** — can assess conformance without necessarily managing the Dataset.

Profiles or Domain Vocabularies may narrow these roles or define additional domain-specific roles.
No role implies `Primary`; policy selects a current default handler where one is useful. A Component
may declare several roles for one Corpus and different roles for several Corpora.

## Corpus Store roles

A Corpus defines one or more logical **Store roles** describing the placement purposes that
Components expect to find. A Store role names which semantic part of the Corpus it contains,
whether binding is required, which Store Operations and Attribute Constraints must be satisfied,
and exactly what absence means.

A Dataset may bind several Corpus-defined Store roles. Each role binds to exactly one logical
Store, while several roles MAY bind to the same Store. The Dataset remains one meaningful body of
information; Store roles make intentionally different retention, security, latency, backup, or
placement policies visible rather than allowing a Component to scatter private files invisibly.

A required role without a valid binding prevents creation or activation of the Dataset. Every
optional role MUST define one explicit absence behaviour, such as:

- **UseRole** — place the declared content in another named role, commonly Core;
- **Discard** — generated content is intentionally voided rather than retained;
- **Recompute** — do not retain the content and reconstruct it when needed; or
- **DisableFeature** — the declared feature is unavailable without the role.

These behaviours are semantic. An implementation cannot silently discard information whose absent
role declares `UseRole`, or silently place diagnostic information in Core when the Corpus declares
`Discard`.

For example, in this document's NonStrict notation:

```
Corpus:
    name: Brontide:Editor.Project
    corpus-version: 3
    owner: Brontide Reference Stack
    kind: Structured

    form:
        Graph:
            node-shape: Brontide:Editor.ProjectNode 2
            edge-shape: Brontide:Editor.ProjectEdge 1

    stores:
        - name: Core
          requirement: Required
          content: [ProjectContent, Settings]

        - name: Metadata
          requirement: Optional
          content: [SearchIndex, Thumbnails]
          when-absent: UseRole Core

        - name: Diagnostics
          requirement: Optional
          content: [Logs, CrashReports]
          when-absent: Discard
```

`Core`, `Metadata`, and `Diagnostics` are locally resolvable Store-role names, not globally
unqualified Brontide concepts. Under Strict notation their candidate expanded identities are
`Brontide:Editor.Project#Store.Core`, `Brontide:Editor.Project#Store.Metadata`, and
`Brontide:Editor.Project#Store.Diagnostics`; the Corpus version remains a separate field. The exact
typed-member grammar is provisional (§22.4).

## Store

A **Store** is a concrete logical system resource responsible for retaining Dataset content bound
to a Store role. A Store may be local, remote, removable, encrypted, content-addressed, filesystem-
backed, database-backed, object-backed, or realised through another mechanism. Those descriptions
do not create Store subtypes.

Store identity is logical rather than physical. One filesystem mount or database server may expose
several Stores with independent authority, retention, or lifecycle policies. Conversely, a Store
may use implementation-internal physical redundancy without exposing every disk as an Brontide Store.
If independently governed placement, failure, authority, or retention must be visible to Brontide, it
is represented through distinct Stores, Store relationships, or a Router rather than hidden inside
one ambiguous Store identity.

A Store exposes the Operations through which it is used and described, including exact vocabulary
and Shape versions. Store-role requirements name required Operations and constrain Attributes
obtained through specified description Operations. `Local`, `Remote`, `Durable`, or `Fast` are not
ambient booleans attached to a Store.

The declaration does not request "Transactions" or "Snapshots" as an abstract Capability. In
Brontide, a Capability is a particular target-recognised grant, not the name of a feature (§10). A
Store role instead names the exact Operations, Profiles, Extensions, vocabulary versions, and
required Declared Fragments on which it relies; binding or activation then establishes actual
Capabilities permitting the relevant Actors to execute those Operations.

For example:

```
store-role: Brontide:Editor.Project#Store.Core

requires:
    vocabulary: Storage 2
    extensions: [Transaction 1]
    operations:
        - Storage.Read
        - Storage.Write
        - Storage.Snapshot.Create

requires-attributes:
    - source-operation: Storage.Topology.Describe
      source-vocabulary-version: 2
      result-shape: Storage.Topology.Description 2
      result-path: locality
      where: value in {Local, Nearby}

    - source-operation: Storage.Profile.Describe
      source-vocabulary-version: 1
      result-shape: Storage.Profile.Description 1
      result-path: expected-latency
      where: value < 50ms
```

The example Operations are candidate standard names, not ratified by Architecture 0.6. A portable
definition uses authored names until the relevant Storage vocabulary is ratified (§22).

A Store SHOULD expose directly inspectable descriptions of capacity, supported Corpus Forms,
durability, locality, authority boundary, availability expectations, encryption guarantees,
retention, and supported Operations where relevant. A simple Store does not require installation
of a Router merely to be manageable. Description remains an attributable claim or observation,
not automatic proof.

## Store relationships and Router

Simple static storage topology belongs in declarative **Store Relationships**. Architecture 0.6
recognises two candidate relationships:

- **Mirror** maintains another current copy under declared consistency and failure semantics.
- **Backup** retains recoverable information or generations under declared schedule, retention,
  integrity, and restoration semantics.

A system may therefore declare one Store as a mirror or backup of another without inserting a
Router. These words are not guarantees by themselves: the relationship contract states direction,
authority, consistency, recovery point, retention, failure visibility, and whether deletion
propagates.

A **Router** is a Component or provider that presents a Store-compatible contract while delegating
Dataset operations to one or more Stores according to explicit rules. It is not defined through a
class hierarchy as a subtype of Store; it satisfies the Store-facing contracts required by the
binding. From a consuming Component's perspective the bound logical endpoint is a Store. Under
appropriate authority, management tooling may inspect that the provider is a Router and examine its
topology.

Routers own policy-driven or condition-sensitive topology, including fallback during service
outage, hot/cold tiering, sharding, latency-aware choice, jurisdictional routing, conditional
replication, migration, and weighted placement. A Router may use dynamic Attributes internally and
expose stable guarantees for the logical Store it presents. The Router's rules, selected backing
Stores, copies, failure boundaries, and fallback behaviour must remain explainable; presenting a
Store contract is not permission to hide a catastrophic copy or weaken the Store-role Constraints.

The Attributes of the logical Store endpoint presented by a Router are the Router's own
declared guarantees for that endpoint, not the Attributes of any current backing Store. A
Router MUST NOT declare an endpoint guarantee that its rules cannot uphold across its declared
backing Stores and fallback behaviour. Within the recorded Mediation direction (Composition
design note), the Router is the storage instantiation of Selection.

The resulting relationship is:

```
Dataset
    binds each Corpus Store role to exactly one logical Store endpoint

logical Store endpoint
    is realised directly by a Store
    or presented by a Router delegating to Stores

Stores
    may additionally have explicit Mirror or Backup relationships
```

## Status of the model

Corpus, Dataset, Store, Store role, Router, Attribute, Definition Constraint, and Parameter are
work-in-progress composition terms in Architecture 0.6. They do not enlarge Brontide Base, ratify a
Storage vocabulary, require a database, or make Structured integration mandatory. Brontide Reference Stack and Brontide Minimal Stack
must test independent definitions, Opaque and Structured Datasets, multiple Store-role bindings,
Store replacement, simple Mirror and Backup relationships, and a Router before a normative
Composition or Persistence specification freezes their exact descriptors and conformance rules.
