# BRONTIDE

## Architecture 0.7 Change Plan

**Status:** Executed change plan; retained as the edit record for the 0.7 Complete Draft
**Baseline:** Brontide-Architecture-0.6.md
**Purpose:** Record the decided changes, rules, deferred fixes, and open decisions against which
the Architecture 0.7 document edit was executed.

This plan is not a specification. Where it states candidate normative wording, that wording is a
drafting starting point and is subject to the editorial pass.

---

## 1. Decided changes

### C1. Composite Definition Constraint evaluation with unknown atoms

**Problem.** §10.1 defines fail-closed evaluation for atomic Constraints. 0.6 introduces
recursively composable `AllOf`, `AnyOf`, and `Not` (§18.1) without defining what happens when an
atom *inside* a composite expression is unrecognised. Naive per-atom fail-closed evaluation makes
`Not(unknown)` evaluate as `Not(false) = true` — a privilege-escalation path, precisely the class
of error the fail-closed rule exists to prevent.

**Decided rule.** One poisoning rule, two context-specific consequences:

> An unrecognised atomic Constraint anywhere within a composite expression makes the entire
> expression unevaluatable. Unevaluatable never resolves to a truth value — in particular,
> `Not`, `AnyOf`, and `AllOf` never convert an unrecognised atom into `false` and then reason
> from it.

- **Authority context** (expression carried by a Capability): an unevaluatable expression causes
  denial of the Execution, per §10.1. No partial credit for recognised branches.
- **Selection context** (Definition Constraints in composition, Store-role requirements,
  provider selection): an unevaluatable expression is unsatisfiable; the candidate is excluded.
  A resolver SHOULD record the exclusion and the unrecognised atom in its explanatory record.

**Edit sites.** §18.1 (Definition Constraints — add the rule and both consequences), §10.1
(cross-reference the composite case), §23 (extend the "unknown semantics fail safely" list),
§29.2 (add a conformance example exercising `Not(unknown)` and `AnyOf(unknown, matching)`).

### C2. Typed member identity uses a distinct separator

**Problem.** The 0.6 candidate grammar (§22.4) encodes member kind as ordinary dot segments:
`Brontide:Editor.Project.Store.Core`. This collides with legitimate concept paths — nothing forbids
an ordinary authored namespace `Editor.Project.Store` containing a concept `Core`, producing the
identical canonical string with a different referent. Canonical names are frozen, signed, and
referenced by Capabilities; a string must have exactly one meaning forever. The scheme also
contradicts §6.10 as worded: member-kind segments would carry semantics, while §6.10 promises
that no dot segment does.

**Decided direction.** Typed members receive their own separator so that dots remain fully
semantically opaque and the colon retains its single authorship meaning:

```
CanonicalName := [AuthorityPath ":"] ConceptPath ["#" MemberKind "." MemberName]
```

For example:

```
Brontide:Editor.Project#Store.Core
Brontide:Editor.Project#Parameter.HistoryDepth
```

`#` is the working candidate glyph, chosen for its member-within-definition (fragment)
connotation. The final glyph is ratified with the canonical-name grammar; the *decision* — a
distinct separator rather than reserved dot segments — is settled. Reserved kind words were
rejected because every future member kind would retroactively collide with names already
ratified under earlier rules, which is unacceptable in an append-only namespace.

**Edit sites.** §22.4 (grammar and all examples), §22.3 (interaction with prefix blocks), §6.10
(add one sentence: member separators, not dots, carry kind semantics), §18.2 (Store-role
identity examples), §33 (narrow the open question to glyph choice, escaping, and member-kind
catalogue).

### C3. Attribute-constrained bindings are resolved once, statically

**Problem.** 0.6 introduces bindings selected through Attribute constraints without stating when
those constraints are evaluated or what survives after binding — a time-of-check/time-of-use
ambiguity of the same shape §10.2 warns against for target designation.

**Decided rule.** Bindings are one-time and static; runtime reaction is a different mechanism
and explicitly not part of binding semantics. Candidate wording:

> Attribute-constrained bindings are resolved exactly once, at composition or activation
> resolution. The resolver evaluates Definition Constraints against Attribute values obtained at
> that moment and records their effective values and provenance in the resolved definition. A
> later Attribute change never invalidates, rebinds, or migrates an active binding. Reacting to
> change is not a binding semantic; it belongs to Routers and to future lifecycle policy.

It is the composition author's responsibility to constrain sufficiently stable Attributes; the
recorded resolution is the guarantee record.

**Edit sites.** §18.1 (state the rule where Attributes are introduced; remove or relocate the
"observation freshness and the policy or Router semantics that react to change" sentence so
dynamic reaction is owned solely by Router and lifecycle text), §33 (retire the rebinding
portion of the open question; keep Router policy semantics open).

### C4. Router logical-endpoint Attribute guarantee

**Problem.** A Store role may constrain Attributes such as `locality`; a tiering Router may have
both Local and Remote backing Stores. 0.6 does not say whose Attributes the Router answers with,
which is exactly where implementations would diverge.

**Decided sentence** (candidate wording, §18.2 Router subsection):

> The Attributes of a logical Store endpoint presented by a Router are the Router's own declared
> guarantees for that endpoint, not the Attributes of any current backing Store. A Router MUST
> NOT declare an endpoint guarantee that its rules cannot uphold across its declared backing
> Stores and fallback behaviour.

This composes with C3: the binding records the Router's endpoint guarantees at resolution, and
the Router is the sanctioned owner of dynamic reaction behind that stable declaration.

### C5. Dataset authority, identity, and concurrency

Three rectifications to §18.2, agreed in full:

**C5a. Capability designation of Datasets, and creation authority.** §18.2 must sketch how a
Capability targets a Dataset or Store role (target designation per §10.2), and must state inline
that Dataset creation is an instance of the open Genesis-versus-authorised-issuance question,
with an explicit cross-reference to §12 and the `Resource` entry in §33. Storage is where
dynamic resource creation bites first; the section should say so rather than leaving the reader
to assemble it from §33.

**C5b. Dataset identity grounded.** 0.6 asserts that a multi-role Dataset "remains one
meaningful body of information" without defining what makes it one. 0.7 must state what
constitutes Dataset identity — at minimum: identity is a property of the Dataset record itself,
independent of any single Store role's content; the Corpus declares which roles are
identity-bearing; and the failure or absence of a non-identity-bearing role's Store does not
fork or destroy the Dataset. Exact atomicity semantics across roles remain open (§33), but
identity must not depend on them.

**C5c. Concurrent access becomes a MUST-declare.** Two Contributors writing one Dataset is
currently addressed nowhere. Following the document's established pattern (§10.3 withdrawal
declarations, §6.9 conflict semantics), a Corpus MUST declare its concurrent-access semantics —
even if that declaration is "single-writer, enforcement by authority" or "undefined,
coordination external." Add to the Corpus contract in §18.2 and to the vocabulary
must-contain list in §21.1.

### C6. Extraction of work-in-progress material into companion design notes

§16.6 (Enrichment), §18.1 (Composition and Components), and §18.2 (Corpus, Dataset, Store,
Router) move to companion design-note documents, following the precedent of the Input.Pointer
vocabulary draft. The architecture document retains, for each extracted direction:

- a short normative-adjacent summary (the principle, the settled invariants, the Base
  non-impact statement), and
- an entry in a new **term status registry**: one table listing every term with status
  *Base / scoping / extension-direction / work-in-progress / rejected*, replacing the six
  scattered "X is not a ninth Base term" disclaimers.

Candidate companion documents:

```
Brontide-Design-Note-Enrichment-0.1.md
Brontide-Design-Note-Composition-0.1.md
Brontide-Design-Note-Persistent-Information-0.1.md
```

Historical changelogs (§35–§37) move to a companion history document; 0.7 retains only its own
changes-from-0.6 section and a pointer.

### C7. Full editorial pass

Since no agreed change requires redesign, 0.7 includes a complete pass over the remaining
document: deduplicate repeated disclaimers into the term status registry, check aphoristic
phrasing where it substitutes for precision in normative-adjacent text, verify all section
cross-references after extraction and renumbering, and confirm every fail-closed statement
cites §10.1 consistently.

**C7a. "Where authority machinery lives" passage.** 0.6 answers the first-time implementer's
question — where do grants actually live? — correctly but scattered across §8, §10.4, §27, and
§28. Add one short assembling passage (candidate placement: §8 or a §10.4 expansion) stating:

- The grant mechanism is the authority domain's trusted computing base and lives wherever the
  domain boundary lives; there is deliberately no single home (§6.8 applied to the most
  security-critical machinery).
- Its three separable responsibilities may live in different places within one system:
  **minting** (Genesis) lives in domain initialisation policy — a compiled authority table, a
  boot construction, an attachment policy; **representation and custody** (§10.4) is
  unprescribed — static entry, kernel object, unforgeable reference, cryptographic credential;
  **evaluation** (§10.1, §13.5) is decentralised to targets by design — Brontide has no central
  reference monitor as an architectural concept, and domain machinery acts on behalf of
  targets at their boundaries.
- The three characteristic homes: firmware structure (mechanism is the image, no runtime
  machinery), hosted or native runtime/kernel (custody in the runtime's or kernel's TCB, with
  the Capsicum seam caveat of §31 for hosted implementations), and cryptography at the
  cross-domain tier (custody is the math; §9.1's four Actor-reference properties are the
  portable contract the representation must preserve).
- Capabilities do not travel between trust boundaries; authorisation happens at each boundary.
  The Reference/Minimal interchange rule that no Capability crosses the process seam is this
  principle in practice, not an implementation accident.

### C8. Mediation recorded as a direction (not ratified)

**Motivation.** Router-shaped mediation already exists outside storage in 0.6: the Event
mediator (§19.2) delegates under explicit rules with provenance preservation; the
traffic-management example (§18.1) routes across data-centre-scale Components; §6.9's mediating
Actor and §26.1's guardian front contested and bounded resources; and C3 assigns dynamic
reaction to "Routers" — which, if Router remained Store-only, would leave dynamic reaction for
non-storage contracts without an architectural home. Selection machinery (Attributes,
Definition Constraints) already applies at composition resolution and at Slot replacement;
per-interaction mediation is the third cell of the same matrix.

**Recorded structure** (to the Composition design note):

**Mediation** is the genus: a declared relationship in a composition in which one party
presents a declared contract while interactions are delegated to backing providers under
explicit, inspectable rules. It follows the Brontide relationship-noun grammar (Delegation,
Interaction, Enrichment); no architectural "Mediator" participant category exists.

**Declared relationship, free realisation.** Mediation joins the established pattern of
Enrichment, the Binding Plan, and representation mapping: the declaration is architectural,
the realisation is not. A Mediation may be realised by a dedicated Component (the paved road —
reusable, replaceable, independently selected, metered, hot-swappable), by host or composition
machinery, or erased into static construction (an embedded static fan-out table is a Mediation
realised by construction). Invariants hold regardless of realisation:

- Mediation is declared, never invisible. The resolved composition records that a binding is
  mediated and under which rules; silent load-balancing behind a believed-direct binding
  violates §6.14.
- Endpoint guarantees are the Mediation's own declarations (C4 generalised).
- Provenance is preserved; origin is never laundered.
- Deputy discipline per §13.6 applies to whatever realises the Mediation.
- Affinity semantics are declared (per-interaction, per-Flow, sticky).
- A **residue obligation** is declared — what the mediation's decision function must remember.
- Mediation is scoped to contracts with declared substitutability (§21.2).
- **Interposition is enforced by authority topology, not discovery.** A Mediation is effective
  for exactly those requesters whose granted Capabilities designate the Mediation endpoint as
  target (§10.2); shared contract shape never couples, only the grant does. The mediating party
  holds the direct Capabilities to the backings (the §6.9 pattern; the membrane lineage of
  §31). A direct grant to a backing bypasses the Mediation — that is a visible, attributable
  policy decision in the delegation graph, not a discovery failure. Discovery remains inert:
  seeing a backing exists confers no reachability. Binding supplies no Capabilities implicitly
  (§18.1); the composition wires requesters to the Mediation endpoint through explicit grants
  by the authority that owns them.
- **Discovery tiering.** At consumer tier, discovery presents the Mediation endpoint as the
  provider; backing visibility is a management-authority concern. This generalises the existing
  §18.2 rule that management tooling may inspect that a provider is a Router and examine its
  topology.

**Trust surface.** A dedicated mediating Component can hold only the Capabilities to its
backings — a small, auditable, least-privilege deputy. Erased mediation is absorbed into the
host's trusted computing base (§28). Both are legitimate; the declaration states which, because
they read differently under security review.

**Species**, distinguished by cardinality and characteristic obligation:

- **Selection** — one requester, one of N substitutable providers. Residue obligation ranges
  from total (storage findability, participating in Dataset identity per C5b) to none
  (stateless compute). Instances: storage routing, compute/accelerator routing,
  identity-provider selection, eligible-Actor resolution.
- **Distribution** — one occurrence, all of N entitled receivers. Obligation is delivery and
  consistency. Instances: Event fan-out, Mirror, Backup. 0.6 already separates Selection from
  Distribution within storage (Router versus Mirror/Backup) without naming the split.
- **Arbitration** — N requesters converging on one bounded resource or provider. Obligation is
  ordering, precedence, and fairness state. Absorbs two existing 0.6 patterns: the §6.9
  mediating Actor fronting a contested resource, and the §26.1 guardian fronting human
  attention. Conflict semantics remain Domain Vocabulary property (§6.9); Arbitration composes
  with admission (§26).

**Component categories.** Consistent with the existing agent-noun tier for Component kinds
(Host, Adapter), a dedicated Component realising a Mediation takes a category name: a
**Router** realises Selection in any domain — the §18.2 storage Router becomes the first
Router rather than the definition; a **Distributor** realises Distribution; an **Arbiter**
realises Arbitration. Erased realisations carry no category noun.

**Declaration level.** 0.7 admits Mediation declarations at composition level only — a
property of a binding. Whether a Component may additionally require Mediation properties of
its environment ("binds only through a Selection with sticky affinity") remains open; it is
more expressive but in tension with §6.12's simple-participation principle.

The data/operations distinction does not produce two mediation mechanisms — a Store Router
already routes Operations (§18.2 defines Stores through their Operations). Data-ness appears
as the residue-obligation dimension, not as a separate architecture.

**Status.** Direction recorded in the Composition design note; the storage Router remains the
first instantiation and the Reference/Minimal storage evidence gate is unchanged. The Event
mediator is evaluated against the Mediation contract when Event Distribution is drafted — it
combines Distribution with Selection-like subscription filtering, and the two roles may
deserve separation there as they already have in storage.

**Edit sites.** Composition design note (new section); C3 candidate wording gains "Routers and
other declared Mediations"; §6.9 gains one sentence naming its mediating-Actor pattern as
Arbitration with a design-note reference; §26.1 gains one sentence naming the guardian as an
Arbiter over human attention; §18.2 gains one sentence placing Router as the storage
instantiation of Selection; §33 gains the Mediation open questions (species completeness,
component-required declarations, Event-mediator decomposition).

---

## 2. Deferred — marked for a later revision, not 0.7

**Deep Router semantics.** Topology visibility under authority, which guarantees describe the
logical endpoint versus its current backing, migration/outage/fallback explanation, and
confidential-policy boundaries. C4's guarantee sentence is the 0.7 stake in the ground; the full
semantics wait for Reference/Minimal storage evidence. Tracked in §33.

---

## 3. Pacing and implementation documents

Persistence expansion beyond the C4/C5 rectifications is held until Brontide Reference Stack and Brontide Minimal Stack produce
first Corpus evidence. The existing interchange sequence (Event/Flow gate, then Macro Operation
exchange, then the mixed image workspace) remains the pacing mechanism.

Following this plan's approval and the completed 0.7 document edit, the two follow-on
implementation plans are:

```
Reference/Brontide-Reference-Stack-Implementation-Plan-0.3.md
                                    — targets Architecture 0.7; adds first Corpus/Dataset/
                                      Store-role experimental evidence (Opaque first), C1
                                      composite-constraint conformance tests, and C3 static
                                      binding-resolution records.
Brontide-Minimal-Stack-Implementation-Plan-0.3.md    — independent counterpart; must implement C1 evaluation
                                      and the C2 member grammar from the specification text
                                      alone, as the cross-implementation check that the rules
                                      are stated precisely enough.
```

Their scope is defined in those documents against the completed 0.7 text. Known implementation
corrections remain separate in
`Brontide-Temporary-Implementation-Correction-Plan-0.1.md`.

---

## 4. Open decisions carried into the 0.7 edit

- Final member-separator glyph and escaping rules (C2; decision on *mechanism* is settled).
- Member-kind catalogue: closed set for 0.7 (`Store`, `Parameter` confirmed; others only as
  needed).
- Whether the selection-context exclusion record (C1) is normative SHOULD or MUST.
- Companion-document naming and whether design notes carry their own version lineages or track
  the architecture version.

---

## 5. Execution status

- **Executed:** C6 (extraction — three design notes, change-history document,
  `Brontide-Architecture-0.7.md` draft with summary stubs, §7.1 term status registry, and a new
  §35 changelog; section numbering unchanged); C8 (Mediation section written into the
  Composition design note; §6.9, §18.2, and §26.1 pop-out sentences applied); C4 (Router
  endpoint-guarantee wording applied in the Persistent Information design note and the §18.2
  summary); C1 (poisoning rule in §10.1, §18.1 summary, §23, §29.2 conformance example, and
  the Composition design note); C2 (`#` member separator in §6.10, §22.4, §33, §34, and the
  Persistent Information design note examples); C3 (static-once binding resolution in the
  §18.1 summary and the Composition design note); C5 (Dataset designation, Genesis
  cross-reference, identity-bearing Store roles, and the concurrent-access MUST-declare in
  the §18.2 summary, §21.1, §33, and the Persistent Information design note); C7a
  (authority-machinery passage in §8); C7 (full editorial pass: disclaimer deduplication into
  the §7.1 registry, §10.3 mortality stance concretised, stale version self-references made
  timeless, cross-references verified, fail-closed §10.1 citations confirmed).
- **Document-edit pending:** none.
- **Follow-on:** the Reference and Minimal 0.3 implementation plans are authored. Implementation
  evidence and Architecture 0.7 ratification remain pending and are outside this edit plan.

## 6. Source of decisions

This plan consolidates the 0.6 review discussion: composite-constraint soundness gap, typed
member collision, binding TOCTOU resolution (static-once, author responsibility), Router
endpoint honesty, Dataset authority/identity/concurrency gaps, document-weight extraction, and
implementation pacing. Where this plan and that discussion differ, this plan governs.
