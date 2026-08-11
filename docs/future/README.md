# Future work

Architecture 0.8 is now the Complete Draft implementation target for both stacks. The implemented
C1-C14 delivery inventory, all 33 runtime vectors, and the completed D1-D6 programme are indexed by
the current stack matrices and archived architecture evidence. Architecture 0.7 remains retained
historical compatibility evidence. Architecture 0.8 is not ratified; formal ratification and
standard-vocabulary freezing remain separate decisions.

The exact pre-implementation Architecture 0.8 document remains at
[`architecture/Brontide-Architecture-0.8.md`](./architecture/Brontide-Architecture-0.8.md) solely as
a hash-pinned snapshot for the closed implementation-correction review. The implemented current
copy is [under `docs/current`](../current/architecture/Brontide-Architecture-0.8.md).

This directory is the authoritative entry point for planned, draft, proposed, work-in-progress, or
otherwise unimplemented work. A document belongs here even when it is the “current architecture” if
the implementations have not delivered it.

## Priority 1 — Channel 0.2 redesign and migration

The [Channel 0.2 Redesign and Migration Plan](./channel/Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md)
is the next work. The architecture-owner ruling does not ratify Channel 0.1's provisional names:
version 0.1 remains executable experimental evidence, and an explicitly migrated 0.2 will reconsider
the capability boundary, session and interaction state machines, message and failure taxonomy,
observation provenance, and extension seams before schemas or public surfaces are added.

Its mandatory first batch is design work: a fresh C1-Cn capability contract, explicit session and
interaction state machines, a responsibility matrix across Channel and adjacent extensions, a
contract-completeness and silence review, a complete 0.1-to-0.2 migration ledger, a neutral-artifact
brief, and fresh independent design review. Neither stack implements Channel 0.2 until that package
agrees and its review has no blocking finding.

The [first-batch design package](./channel/README.md) now includes C1-C12, both state machines, a
closed state/event grid, the responsibility matrix, silence review, migration ledger, neutral-
artifact brief, four resolved owner rulings, and five retained independent review cycles. Every
finding through T1-T4 has a contract-first correction at the current pin. The exact next work is a
fresh independent closure re-review and closure record under the
[review handoff](./channel/reviews/README.md#exact-next-work). No Channel 0.2 schema or implementation
is authorized until that handoff closes cleanly.

## Completed predecessor — Portable Component Binding 0.1

[Portable Component Binding Implementation Plan 0.1](./binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
records the completed experimental predecessor. It turned retained Cooling and Catalog experiments
into a reusable Binding Plan and Channel 0.1 realization, mapped them to C1-C10 and all Channel 0.1
vectors, and created the data-only neutral contract under
[`binding/portable/`](../../binding/portable/README.md).

**PB0 through PB7 are complete.** The PB0 scaffold, C1-C10 baseline inventory, representation
choice, and both resolved owner decisions are recorded there; PB1 authored the neutral contract
itself, as eight data-only schemas, 63 vectors covering C1-C10 and all 24 Channel 0.1 vectors, and
deterministic golden CBOR encodings — the later additions are Decision 5's eight Catalog vectors and
PB7's ninth schema with its eleven, Decision 11's provider-mismatch vector, and PB8's lifecycle
correction, taking the total to 84; PB2 implemented that contract natively in the Reference
stack under [`Brontide.Reference.Experimental.Binding/Portable/`](../../Reference/src/Brontide.Reference.Experimental.Binding/Portable/);
and PB3 implemented it independently in the Minimal stack under
[`Brontide.Minimal.Binding/Portable/`](../../Minimal/src/Brontide.Minimal.Binding/Portable/), using
Minimal-owned algebraic types and explicit results rather than a translation of the Reference
surface. Cooling and Catalog are fixtures over the reusable layer rather than definitions of it.
[`build/verify-portable-binding.ps1`](../../build/verify-portable-binding.ps1) validates the neutral
layer without loading either stack and then runs both stacks' evidence against it.

**PB4 measured the two realizations against each other** across every portable result class a host
can reach, rather than only the success and denial PB2 and PB3 compared. Doing so found four
divergences — the same four in each stack independently — and closed them: an endpoint-decided
refusal reported two different failure domains, and an authority-bearing request body was rejected
under two different categories depending on which rule happened to fire first. Every Channel 0.1
vector now has executed evidence in each stack, derived from the neutral declarations rather than
restated, and the parity profiles are reproduced against a provider in its own process. The three
migration obligations PB1 recorded — deterministic map key ordering, schema-guided values, and
frameless denial — were discharged on both sides in PB2 and PB3; they are described in
[`binding/portable/README.md`](../../binding/portable/README.md).

**PB5 paired the implementations.** All six combinations of the cross-stack matrix pass: each stack's
host against the other's provider, each host against an
[implementation-neutral provider](../../binding/neutral-provider/README.md) that imports no Brontide
assembly, and both fixed direct calls. Pairing them found two things four phases of independent work
had not — that Catalog was never a shared contract, and that the neutral fixture declaration was not
encodable as published, because it carries documentation fields the contract rejects. Both are fixed
in the neutral data, and no neutral vector is deferred in either stack any more.

**PB6 hardened both stacks.** Decoders are property-tested inside deterministic bounds, failure paths
are proved to leak no provider effect, value, runtime type, resource, or false success, transport
failures classify totally into declared process categories, and the C6 and C8 refusals are now
decided by an endpoint across a real seam rather than by a codec or a lifecycle object called
directly.

Testing properties rather than cases found three defects, each present identically in both stacks.
Resource observations claimed an acceptance and an integrity check that never happened, in fields the
parity profile compares. The transport let foreign exceptions escape the binding. And two declared
process categories had no path that could produce them.

That they appeared in *both* stacks is the finding worth carrying forward. The programme's central
safeguard is independent implementation, and independent implementation is exactly what cannot catch
these: two stacks written from one contract by one reader diverge where the contract is ambiguous —
which is what PB4 and PB5 found — and agree wherever it is silent, which is where PB6's defects
lived. The plan's PB6 section records all three, the ordering rule that a malformed frame is refused
before its direction is weighed, and why `peer-unavailable` and premature resource reuse stay
unreachable in version 0.1 rather than being given manufactured paths.

**All eight owner decisions raised by PB4, PB5, and PB6 were recorded on 2026-07-28**, with their
option sets and rationale retained in
[`binding/portable/open-decisions.md`](../../binding/portable/open-decisions.md) and dated rulings in
the plan's [Resolved questions](./binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md#resolved-questions).
Four confirmed the provisional choice unchanged; four created work, all of it now done.

Decision 10 is the one to read first, because it is about the programme rather than the binding.
Given that every PB6 defect was present identically in both stacks, it asks what supplements
independent implementation, and answers with two standing practices: **every capability states at
least one property holding over all its vectors**, and **each phase boundary gets a
contract-completeness review** asking what the contract does *not* say. Both are now ground rules in
[`AGENTS.md`](../../AGENTS.md). The reasoning is that two implementations written from one contract
by one reader diverge where it is *ambiguous* and agree where it is *silent*, so independence detects
ambiguity and is structurally blind to silence.

Decision 5 also gave the Catalog fixture the vectors it had never had — PB-64 through PB-71, executed
in both stacks — closing the case where the neutral layer declared what Catalog is without stating
what it must do. Doing so found that the Catalog *handlers* had drifted apart across all three
implementations on partial-match and count semantics, which the fixture contract never declared.
PB-70 and PB-71 settle both, with the rules derived from the declared Shapes rather than adopted from
whichever implementation was read first, and Minimal brought to them.

Three questions were open after CBI21: whether to ratify the provisional Channel Shape and category
names or publish an explicitly migrated revision, and Decisions 12 and 13, raised by CBI20 and CBI21
on 2026-08-01. Decision 13 was recorded on 2026-08-11: Portable Binding 0.1 retains its fail-closed
refusal of every CM3 group declaring a bounded lifecycle protocol, and a versioned 0.2 will separate
establishment from readiness and add exact declared relational lifecycle traffic before Ready. The
Channel question was resolved on 2026-08-11 in favor of a full, explicitly migrated 0.2 redesign;
Decision 12 remains open and blocks nothing.

**PB7 added the Composition handoff**: the narrow seam by which a resolved Component requirement and
an offered provision produce a Binding Plan during activation preflight. It consumes a resolution and
never produces one, so four of its eleven vectors (PB-72 through PB-82) are refusals — a Provider
Set, a mediated exposure, a provider the resolution did not select, and a Component the three inputs
disagree about — because approximating any of them would make the Component Management programme's
decisions here, invisibly. A controlled experimental composition in each stack establishes and
releases bindings across both realizations, with the ordinary-interaction gate closed until every
required member is ready. The seam was declared in
[`binding/portable/schemas/composition-handoff.json`](../../binding/portable/schemas/composition-handoff.json)
before either stack implemented it, which is Decision 10's completeness practice applied at the front
of a phase rather than at its boundary.

Pointing a new consumer at the layer found what six phases of using it had not: **negotiation never
compares provider identity**, so the Binding Plan's provider fact reports who the host asked for
rather than who answered. Both stacks do this identically, and every fixture derives from one
declaration, so the two have always agreed. The handoff checks it provisionally; the contract
question is **Decision 11**, recorded on 2026-07-30: negotiation now compares provider identity and
refuses a mismatch, and the plan reports the provider that answered. It was another instance of the
gap Decision 10 names.

**PB8 is complete.** Its evidence and documentation work is done: the contract matrix now
carries an executed-evidence table naming which realizations have run each capability; the Channel
ledger records CH-R11 as executed by a conforming realisation rather than awaiting stack harnesses;
the public boundary document gains a portable-seam section; both stacks' changelogs record the added
experimental surface; and the source-cost inventory has been re-measured, separated into retained and
portable layers, and extended with the representation, framing, allocation, copy, and payload-bound
facts for both realizations, each stating how it is known. The complete repository gate is green.

PB8 Step 5 is complete. The retained independent-review sequence found and corrected neutral
contract contradictions, stale target and Channel evidence, missing capability-wide properties,
and two production paths that fabricated a zero provider-effect count when attribution was unknown.
Fresh closure reviewers for Reference, Minimal, and the neutral contract all conform at `5150d6d`;
the records live under [`binding/portable/reviews/`](../../binding/portable/reviews/README.md).

PB8 Step 6 closed on 2026-08-11 by owner ruling: Channel 0.1's provisional logical names are not
ratified as the lasting contract. The explicitly migrated successor is the full Channel 0.2 redesign
now listed as Priority 1. This closes Portable Binding 0.1 evidence without promoting it to a stable
public extension.

The former Priority 0 documentation relocation is complete; its archived plan is the
[Pinned Documentation Relocation Plan 0.1](../archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md).
No documentation prerequisite now precedes planned implementation work.

## Priority 2 — Component Management

[Component Management Implementation Plan 0.1](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md)
records the completed fake Component Management programme retained at its evidence path. CM0
through CM5 are complete independently in both stacks. CM1 adds standardised contract/version
discovery across zero or more fake sources, deterministic attributable candidates, immutable staged
acquisition, contested evidence with attributable fake-policy decisions, source disappearance, four
structured fail-closed acquisition categories, and an explicit zero-effect boundary. Its C1-C7
behaviour and phase-wide properties live in the data-only
[`CM1 capability contract`](../../component-management/cm1-capability-contract.md).
Its required phase-boundary
[`contract-completeness review`](../../component-management/cm1-contract-completeness-review.md)
is complete with every finding corrected. CM2 adds deterministic, effect-free recursive acyclic
closure into an inspectable Proposed Stack and immutable generation, including occupied-binding
stability, explainable ranking and exclusions, Provider Sets, occurrence sharing, Mediation, child
Port envelopes, topology decisions, and post-closure Activation Parameters. Its
[`C1-C10 capability contract`](../../component-management/cm2-capability-contract.md) and
[`contract-completeness review`](../../component-management/cm2-contract-completeness-review.md)
are complete.

CM3 adds deterministic maximal strongly connected activation groups, exact contract/version and
bounded lifecycle-protocol validation, Ready reachability and wait analysis, declared Region/Port
containment, dependency-first group ordering, and explicit closed-gate stages through Ready. Its
[`C1-C9 capability contract`](../../component-management/cm3-capability-contract.md) and
[`contract-completeness review`](../../component-management/cm3-contract-completeness-review.md)
are complete. CM3 is effect-free planning: it does not prepare or establish Components, execute
lifecycle Operations, accept runtime Ready reports, Release ordinary interaction, establish Actors
or authority, or mutate an active generation.

CM4 adds a deterministic fake Host over successful CM3 plans: optional effect-free preparation,
complete named establishment, exact lifecycle and ordinary gates, one logical Release,
post-Release binding evidence, scoped replacement, retained-generation disposition, child-Port and
host-assisted activation, and explicit rollback, rollback-impossibility, or corruption outcomes.
Its
[`C1-C10 capability contract`](../../component-management/cm4-capability-contract.md) and
[`contract-completeness review`](../../component-management/cm4-contract-completeness-review.md)
are complete. It is fake runtime evidence, not a production activation or rollback system.

CM5 adds an independent fake receiving-domain authority-admission evaluator in each stack. It keeps
participant requests, evidence decisions, local Actor mappings, local policy, and exact narrow
Capability grants separate; refuses revoked, expired, unverified, untrusted, subject-mismatched,
unknown, and unlimited requests; and records deliberately mistaken policy decisions as attributable
local trusted-computing-base choices. Its
[`C1-C10 capability contract`](../../component-management/cm5-capability-contract.md) and
[`contract-completeness review`](../../component-management/cm5-contract-completeness-review.md)
are complete. It is not cryptographic, federated, or production authority evidence.

CM6 completes the experimental programme with bounded JSON Lines endpoints and complete canonical
CM5 profile comparison across real processes in both host directions. Equal profiles establish
agreement on eight deterministic fake scenarios only. The completed implementation plan remains
at its stable `future` path because architecture delivery evidence links it transitively; moving it
requires explicit authorization to repin and independently review that evidence.

## Priority 3 — Component Management / Portable Binding integration

CBI1 is the first implemented integration slice. Reference Studio and Minimal Host independently
translate one completed native CM2 direct `1..1` provider position, using an explicit typed mapping,
into PB7 portable preflight. The
[`CBI1 capability contract`](../../component-management/cbi1-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi1-contract-completeness-review.md)
define and close the slice. It refuses unresolved, wider-cardinality, mediated, empty or multiple,
indirect, identity-mismatched, and invalidly addressed positions before a provider or Binding Plan
exists.

CBI2 is also implemented. Each composition root now coordinates that prepared member with one
singleton, protocol-free CM4 plan, derives lifecycle stages from PB7 rather than caller claims,
validates CM4 before provider contact, projects portable establishment refusal into CM4, and opens
the portable ordinary-interaction gate only after CM4 reaches Active. The
[`CBI2 capability contract`](../../component-management/cbi2-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi2-contract-completeness-review.md)
bound the evidence.

CBI3 is also implemented. Each composition root now requires an explicit selected-occurrence to
participant-Actor mapping, one `ComponentParticipant` relationship, and one exact narrow CM5
authority request. CM5 denial stops before provider contact; exactly one attributable relationship
and local grant permit CBI2 activation. The grant is not transported through Portable Binding and
does not authorize any portable Operation by name. The
[`CBI3 capability contract`](../../component-management/cbi3-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi3-contract-completeness-review.md)
bound the evidence.

CBI4 shared serialized comparison is also implemented. Each composition root independently
projects five native CBI3 executions into the same canonical profile covering the complete CM5
observation digest, CBI3 decision, CM4 effects and stable failures, portable member lifecycle, and
all stable resolution and Binding Plan facts except local `planId`. The shared fixture pins exact
profile digests and exposed one corrected Reference compact-identifier token-casing divergence.
The [`CBI4 capability contract`](../../component-management/cbi4-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi4-contract-completeness-review.md)
bound this to deterministic data comparison, not an integrated process seam.

CBI5 grant withdrawal is also implemented. Each composition root revalidates the exact native CM5
relationship and grant that admitted one active CBI3 binding using fresh explicit time, evidence,
and policy. Exact renewal keeps the portable member released; revocation, expiry, request mismatch,
or non-identical local admission retires it before further ordinary interaction. Retirement closes
the gate before peer cleanup, so cleanup failure remains visible without restoring activity. The
[`CBI5 capability contract`](../../component-management/cbi5-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi5-contract-completeness-review.md)
bound this to post-activation admission, not in-flight cancellation or distributed revocation.

CBI6 multi-participant and multi-grant admission is also implemented. Each composition root accepts
a set of participants, each with its own CM5 request carrying one `ComponentParticipant`
relationship and one or more exact narrow authority requests, and admits the set only when every
request is admitted exactly as submitted. Because a CM5 request names exactly one participant, the
questions a set raises belong to the composition root, and all three are answered fail closed:
admission, relationship, and authority request identities must stay distinct across the whole set;
two participants may not be mapped onto one receiving-domain Actor; and a set that is not admitted
exactly carries no aggregate grant, leaves no portable member, and reaches no provider. The shared
vectors pin how many participants each scenario evaluates, which forces both stacks to answer a
question the contract could otherwise have left silent. The
[`CBI6 capability contract`](../../component-management/cbi6-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi6-contract-completeness-review.md)
bound this to admission of a set, not revalidation or withdrawal of one.

CBI7 participant-set revalidation and withdrawal is also implemented, and it closes the question
CBI6 deferred. Each composition root revalidates every participant of an admitted set from a fresh
explicit CM5 request; the shared member stays released only when the identical set renews
identically. **When one participant of several loses authority the member is retired, not narrowed**,
because nothing in an admitted set says which participants its ordinary interaction depends on — the
set is unordered, none is marked required, and the member declares no dependency on particular
grants — so continuing would make that Component Management decision invisibly. Membership change or
identity drift retires the member before any request is evaluated; retirement closes the
ordinary-interaction gate before peer cleanup; and the result names exactly which participants did
not renew, so a still-admitted participant stays visible next to the retirement it did not cause.
The [`CBI7 capability contract`](../../component-management/cbi7-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi7-contract-completeness-review.md)
bound this to one participant set over one released singleton binding.

CBI8 delivers the addition half of in-place participant replacement, and refuses the other half on
purpose. Each composition root grows an admitted set while its member stays released: the intended
set must retain every current participant and add at least one, retained participants are
revalidated in the same all-or-none evaluation as the additions, and the whole-set identity and
receiving-domain Actor rules are checked against the participants already live. **Removal and
substitution in place are declined**, for the reason CBI7 refuses narrowing — a substitute holding
the identical Capability, target Actor, Operation, and scope is still a different grant, because the
holder is part of the grant — and they route through CBI7 retirement and a fresh CBI6 admission
instead. That also disposes of participant precedence: it would only be needed to decide which
participant may be dropped from a live set, so refusing every drop removes the question rather than
answering it badly. A declined extension leaves the binding exactly as it was; an evaluated lapse in
a retained participant retires it. The
[`CBI8 capability contract`](../../component-management/cbi8-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi8-contract-completeness-review.md)
bound this to growth of one set over one released singleton binding.

CBI9 supplies that prerequisite and then removes and substitutes participants of a live set. The
declaration is not the caller's opinion: its names must equal the requested authority CM2 already
records for the CBI1-selected definition, so the Component states what its interaction depends on
and the caller supplies only the explicit typed mapping from each declared name to the CM5
Capability, target Actor, Operation, and scope that satisfies it. A revision is admitted while every
declared dependency stays covered by some participant of the intended set. Because the declaration
names tuples rather than holders, **a substitute with a different receiving-domain holder can satisfy
a dependency the departing participant used to satisfy** — which is the point CBI8 could not reach,
and which revises CBI8's reasoning rather than contradicting it, since CBI8's growth-only rule
remains correct wherever no declaration exists. It also closes participant precedence for good:
coverage decides who may leave, so no participant is ranked above another. A declaration cannot be
introduced to bless a set that never covered it, and an empty declaration licenses nothing. The
[`CBI9 capability contract`](../../component-management/cbi9-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi9-contract-completeness-review.md)
bound this to one set under one declaration over one released singleton binding, and record the
boundary worth carrying forward: the declaration is trusted as the Component's own statement, and
nothing checks it against what the member's ordinary interaction actually does.

CBI10 verifies that declaration against what the member actually did, and closes CBI9's finding in
one direction. Each observed portable interaction is projected into one CM4 binding exercise whose
`AuthorityAdmitted` fact is **derived** from the declaration and the grants in force rather than
claimed by the caller, so **CM4's own rule — delivery cannot succeed when the external authority
check denied it — is what condemns interaction outside the declaration**, and both stacks assert the
equivalence as a property: the runtime accepts the projection exactly when the verification is
consistent. An interaction that emitted no frame exercised nothing, because it reached no provider;
an interaction that cannot be attributed to declared authority is undeclared use, so omitting a
mapping entry hides nothing; and undeclared or ungranted use retires the member, since verification
cannot undo an interaction that already happened. Declared authority nothing exercised, and declared
authority no participant covers, are reported rather than condemned. This also supersedes CBI3's
refusal of caller-authored binding-exercise authority by deriving that authority instead of
accepting it. The [`CBI10 capability contract`](../../component-management/cbi10-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi10-contract-completeness-review.md)
bound it to the interactions it is given over one released singleton binding.

CBI11 answers the question CBI10 left, and the answer is that **no evidence of disuse ever narrows a
declaration**. A declaration narrows only to a successor CM2 resolution of the same position — same
requirement, definition, occurrence, cardinality, exposure, and the binding scope the live member
itself records — that declares strictly fewer authorities, each retained one keeping its exact
tuple. The Component's own re-declaration is the permission; observation appears only as a veto,
since authority the member has already exercised cannot be narrowed away. Elapsed time, interaction
counts, and quiet periods narrow nothing, and the contract says so to keep a later implementer from
reaching for a threshold. Narrowing permits rather than performs: a later CBI9 revision releases the
participant the narrowed declaration no longer needs, and each stack proves the difference by running
the same revision before and after. CBI11 has no retirement path at all, which every vector checks.
A Component that narrows dishonestly and then exercises what it dropped is caught afterwards by
CBI10 as undeclared use, so succession cannot launder authority — it can only move the binding to a
declaration the Component will be held to. The
[`CBI11 capability contract`](../../component-management/cbi11-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi11-contract-completeness-review.md)
bound it to one declaration over one released singleton binding.

CBI12 relaxes the constant every earlier slice held fixed and activates several members together.
**The release barrier is the activation, not the member.** CM4 models one logical Release for an
activation attempt, so ordinary interaction opens for every member at once or for none — the answer
comes from the runtime's own shape rather than from a preference, and both stacks assert it as a
property over every vector. A member that reached Ready while another failed is retired, gate first,
so no member is left holding an open channel because a sibling failed; each stack proves it by
attempting an ordinary Operation on the survivor and requiring a state refusal. Cyclic groups are
refused: a multi-member group is a strongly connected component, which is what Relational
Initialisation exists for, and the vector uses a genuinely cyclic CM3 plan — two relational edges,
each with its own complete protocol — rather than a hand-made shape. The
[`CBI12 capability contract`](../../component-management/cbi12-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi12-contract-completeness-review.md)
bound it to independent, protocol-free members within one activation.

CBI13 closes that gap and answers both questions the item raised. **Authority is admitted per
member**, because CM5 admits against an occurrence and CBI3 ties admission to one: an occurrence is
durable where an activation attempt is not, so admitting against an attempt would force authority to
be re-decided on every restart. **The authority barrier and the release barrier are two barriers, and
the authority one is strictly earlier** — which corrects the guess this item recorded. Authority is a
precondition evaluated before any provider is contacted; Release is reached after every member
reports Ready. What they share is being all-or-none over the activation, and both stacks check the
separation directly: every authority refusal leaves no lifecycle at all. Across the activation the
receiving-domain Actor mapping must be a function and injective, so one party may participate in two
members and must map consistently, while two parties may not arrive at one local Actor — CBI6's
conflation rule one level out. The
[`CBI13 capability contract`](../../component-management/cbi13-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi13-contract-completeness-review.md)
bound it to per-member admission over one protocol-free multi-member activation.

CBI14 lifts the first of those slices — revalidation and withdrawal — to the activation, and answers
the question above. **When one member's authority lapses, the whole activation retires.** The answer
comes from CM4 rather than from preference, as CBI12's release barrier did: a CM4 activation has
exactly one restart scope, every member of a CBI12 activation is inside it, and CM4 models no way to
retire one member while its scope keeps running — that is a scoped replacement, an operation it
declares separately. The members came up together inside one scope and they go down together. That
CBI12's members are otherwise independent looked like an argument for retiring only the lapsed one,
and the review records why it is not: independence is about what members need from each other, not
about what scope they share. The result names which members lapsed and which participants within
them, so a member retired because a sibling lapsed is never reported as the cause. The
[`CBI14 capability contract`](../../component-management/cbi14-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi14-contract-completeness-review.md)
bound it to revalidation and withdrawal of one protocol-free multi-member activation.

CBI15 lifts revision and answers that question: **a change is decided per member and checked against
the activation.** Admission is about an occurrence, so changing one member's set decides nothing
about another member's authority; but CBI13's identity and Actor-mapping rules are activation-wide,
so the result is checked across every member. Splitting the question that way is what lets CBI13's
per-member admission and CBI14's per-activation retirement both hold at once instead of one
overriding the other. It also settles a second thing: **a declined change is local and a discovered
lapse is global.** The same call can produce either — a revision the activation will not admit
changes nothing, while a retained participant that no longer renews is CBI14's case and retires
everything, including when the lapse is in a member that was not being revised. A wrongly named
member set is declined here rather than retiring as it does in CBI14, because a revision asks for
something the activation will not do while a revalidation asserts continuity it then cannot
demonstrate. The
[`CBI15 capability contract`](../../component-management/cbi15-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi15-contract-completeness-review.md)
bound it to revision under per-member declarations.

CBI16 lifts CBI10's verification and answers the question that lift raised: **one member's undeclared
use condemns the whole activation.** The answer comes from the runtime rather than from a preference,
as CBI12's release barrier did — a CBI12 activation is one CM4 request, so every member's projected
exercises are judged together and CM4's rule that delivery cannot succeed when the external authority
check denied it refuses the request on the first offending exercise rather than excusing the members
that behaved. That CBI14's independent reason, one restart scope and one fate, reaches the same
answer is recorded rather than relied on: two arguments converging would have had to be weighed
against each other had they disagreed. Lifting it also poses two questions a single member could not.
**Attribution is per member**, so two Components that both expose an Operation of the same name are
two independent attributions — a shared vector attributes the same Operation reference in both
members and is admitted — while a repeat inside one member is still refused. And **no member's grants
admit another member's use**, because CBI13 admits authority per member. A structural refusal
evaluates nothing and changes nothing, which is CBI15's decline-versus-retire distinction under a
different input. The [`CBI16 capability contract`](../../component-management/cbi16-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi16-contract-completeness-review.md)
bound it to the interactions it is given over one protocol-free multi-member activation.

CBI17 lifts CBI11's succession and answers both questions that lift raised. **A succession is one
transaction over the activation**, because the permission is a generation and a CM2 generation is one
immutable object that resolves every position at once; narrowing the members it describes while
refusing the rest would leave the activation holding declarations drawn from two generations, which
is a state no generation records. **A member whose position the successor does not resolve blocks
every other member**, for the same reason: a generation that does not describe the whole activation
is not a successor of it, and the declaration is exactly what CBI16 holds a member to afterwards.

It also found something the item did not anticipate. CBI11 refuses an unchanged declaration because
a single-member succession that changes nothing has nothing to succeed — and that turns out to be
**two rules only a second member can separate**. *Nothing to succeed* stays an activation-level
refusal; *this member is untouched* becomes an ordinary per-member outcome, because a successor that
narrows one Component and leaves another alone is the common case rather than an error. A veto is
computed from each member's own observations, as CBI16 attributes them, and refuses the whole
transaction including narrowings that had no veto. This slice still has no retirement path at all,
which every vector checks, and it is synchronous in both stacks because it has no peer traffic to
perform. The [`CBI17 capability contract`](../../component-management/cbi17-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi17-contract-completeness-review.md)
bound it to one successor generation over one protocol-free multi-member activation.

CBI18 lifts the last single-member slice and dissolves the first question the item recorded rather
than deciding it. **An activation may hold declarations for some members and none for others, because
growth cannot observe them.** A declaration says whether a departing participant may go; growth
removes nobody, and coverage is monotone in the grants held, so a set that covered its declaration
still covers it afterwards. CBI18 therefore takes no resolution and no declaration at all, and **the
absent parameter is the contract** — the same device CBI17 used when it made succession synchronous.
The second question was a re-application rather than a discovery: growth is checked against the whole
activation exactly as CBI15's revision is, because CBI13's rules are activation-wide.

What neither question anticipated is the case only a second member can pose. **A party already
participating in one member may be added to another**, and must then arrive at the local Actor it
already holds — CBI13's function-and-injective mapping rule in its *permitting* direction, which a
single member could never exercise. Both directions have a vector. A lapse in any retained
participant still retires the whole activation, and a declined extension still changes nothing
anywhere. The [`CBI18 capability contract`](../../component-management/cbi18-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi18-contract-completeness-review.md)
bound it to declaration-free growth over one protocol-free multi-member activation, and record the
boundary a mixed activation makes visible: CBI16 derives an exercise's admission from a declaration,
so an undeclared member's ordinary interaction cannot be verified at all.

**The lifting programme is complete.** CBI8, CBI10, and CBI11 have all been lifted, by CBI18, CBI16,
and CBI17.

CBI19 implements scoped replacement, and its first finding **corrects the three slices that deferred
to it** rather than fulfilling them. CBI14, CBI15, and CBI18 each said that retiring one member while
its scope keeps running "is a scoped replacement, an operation CM4 declares separately". Reading CM4
to implement it shows scoped replacement targets a restart scope holding a *generation*, and its
Release makes the successor generation active **atomically for the whole scope**. Nothing in CM4
retires one member and leaves its siblings running. So those slices' answer — retire everything — was
not a placeholder awaiting this one; it was already correct, and the forward reference named a
capability the model does not have.

That disposes of the second question this item recorded, which presupposed a per-member replacement:
the release barrier re-arms for the **whole successor activation**, from CM4's shape as CBI12's
original barrier did. The first question has a real answer. **Authority follows the occurrence, not
the activation attempt** — CBI13 admits against an occurrence *because* an occurrence is durable
where an attempt is not, and a replacement is precisely the event that ends an attempt while
occurrences persist, so that justification is finally exercised rather than asserted. A surviving
occurrence must be re-admitted with the authority that admitted it, so a replacement cannot quietly
change what it may do; a new occurrence is admitted afresh; and **nothing is inherited either way**,
so a revocation landing between the two attempts is seen. The ordering a plausible implementation
gets wrong is also pinned: the retained members are retired **after** cutover and never before,
because CM4 requires a pre-cutover failure to leave the retained generation serving. The
[`CBI19 capability contract`](../../component-management/cbi19-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi19-contract-completeness-review.md)
bound it to replacing the generation in one restart scope over protocol-free members.

CBI20 lifts the last structural constant and lets the successor resolve a different set of positions,
and its first result is a **defect in CBI19 rather than a new capability**. CBI19 states that its input
is one entry per successor member and that it adds or removes no position, and it checked neither: a
caller could hand it a membership that is a strict subset of what the successor generation resolves,
with a CM3 plan built from that same subset, and get a cutover to a generation whose plan covered fewer
members than CM2 resolved — the omitted Component retired, and no refusal anywhere. CBI19's vectors are
structurally unable to catch it, because each one derives the member list, the participant sets, and the
plan from one declaration, so no vector ever asked whether they agreed. That is the shape Decision 10
describes, and it appeared identically in both stacks, as PB6's three defects did. **The membership is
the successor generation's statement, not the caller's**, and CBI19 now refuses an under-supplied,
over-supplied, or changed one, so its stated limit is checked rather than assumed.

The lift itself needed no new authority rule, and that is the second result. CBI19 decided authority
per occurrence, so **a dropped occurrence has nothing to follow it to** — its grant is not
re-established, no withdrawal is performed against the receiving domain, and the member is retired with
the rest of the retained generation — while an added occurrence is admitted afresh. The question the
item recorded reads as though it needed a rule and needed none; what it needed was for the departure to
be *visible*, which the derived added, dropped, and surviving sets supply. The second question has an
answer from the runtime rather than from preference: **an added position joins only across a cutover**,
because a CM2 generation is one immutable object resolving every position at once and a CM4 attempt
carries one plan covering every member, so neither can represent a member arriving into a generation
already serving — which is also the line between CBI18 and CBI20. An emptied membership is refused as
CBI14's withdrawal reached through the wrong door, and the case only a changed membership can pose is
that a receiving-domain Actor a **dropped** participant held may be taken by a different party in an
**added** member, while the same reuse against a *surviving* participant stays the conflation CBI13
refuses. That permitted reuse is the one answer a second, independent implementation of this slice
reached differently, and it is **open as Decision 12** in
[`binding/portable/open-decisions.md`](../../binding/portable/open-decisions.md): the merged rule
reads CBI13's mapping as a property of an activation that ends at cutover, while the other reading
refuses the reuse until the retained members retire, because both generations are established against
the same binding scope for the width of the cutover. No vector distinguishes them today. The
[`CBI20 capability contract`](../../component-management/cbi20-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi20-contract-completeness-review.md)
bound it to one successor generation resolving a different set of positions over protocol-free members.

CBI21 reaches Relational Initialisation, and its first result is that **CBI12 refused two different
things under one justification**. CBI12 declines a multi-member group because "a multi-member group is
a strongly connected component, which is what Relational Initialisation exists for". CM3 computes
strongly connected components over *every* edge, so two Components with mutual ordinary-interaction
edges are one cyclic group that declares no protocol, no relational stage, and a stage plan CM4
returns Active for. Being cyclic and needing a handshake are two properties, and only the second is a
reason this seam cannot host a group. CBI21 delivers the first and refuses the second by name.

**What stays refused is refused by Portable Binding's own published contract, and this slice locates
it rather than deciding it.** The PB7 Composition handoff lists Relational Initialisation in its
`outOfScope` array. Two things are missing and the second is the one a later implementer would miss:
the seam offers a composition exactly one traffic verb and gates it on Release, and a portable member
reports Ready *during* Interconnection, while CM4 requires the handshake to complete **before** Ready.
Adding a verb without splitting that step would leave the handshake with nowhere to run that still
precedes the readiness it must precede. The vectors show CM3 produced the plan and CM4 accepts it with
its own declared handshakes supplied, so the integration is the only refusal in the chain.

The three questions this item recorded — what a bounded protocol means for the release barrier,
whether lifecycle-traffic authority is CBI13's admission or a separate one, and what a handshake
failing midway leaves behind — are therefore **unreachable rather than undecided**, and answering them
here would settle a Portable Binding contract question inside a Component Management slice. What the
seam needs was recorded as **Decision 13** on 2026-08-11: 0.1 keeps the refusal unchanged, while a
versioned 0.2 splits readiness from establishment and adds exact declared-protocol traffic before
Ready. That follow-on begins only after PB8 reviews the stable 0.1 evidence. CBI12's plan refusal also
stops reporting one code for four conditions. The
[`CBI21 capability contract`](../../component-management/cbi21-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi21-contract-completeness-review.md)
bound it to protocol-free strongly connected groups over one activation.

CBI22 activates a Component position CM2 resolved inside a **child Port**, in its own restart scope,
attached to the scope and generation a released parent activation made active. Its first result is a
fail-open this index itself asserted was closed. A `ProviderSetObservation` carries the Region and
Port CM2 resolved a position into; **CBI1 read neither**, so such a position was flattened into an
ordinary one and activated in whatever restart scope the caller's plan named — no child declaration,
no parent generation, and the restart boundary the Port exists to give silently dropped. The sentence
here saying CBI1 refused it was written from the contract rather than the code, and it is the third
stated limit in four slices that turned out to describe how something was called rather than a rule
it applied — the first of them written by this programme about itself. Both activation paths now
refuse a Port-contained position, and the child path is the way through.

**What the attachment says about the Port is the generation's statement, not the caller's**, which is
CBI20's membership rule applied to containment: every member must be contained in one Port, the
attachment must name that Port, and the lifecycle facts come from the resolved envelope, so a caller
can neither attach a Component to a Port CM2 did not put it in nor claim a Port is runtime-open when
the generation resolved it as activation-open. CM2 refuses a sealed Port at resolution, so that is the
only reachable form of CM4's closed-Port refusal and the contract says so rather than manufacturing a
path. CM4 owns the remaining refusals — an occupied Port needs an explicit replacement lifecycle
declaration, and a host-assisted export must follow the child's internal Release — and CBI22 reports
those classifications rather than forming its own.

Both questions this item recorded have answers from the models. **A child's members are portable
members of the same composition root**, because the seam binds a host to a provider and has no notion
of nesting; and **the parent's release barrier means nothing for a child that is still coming up**,
because they are separate CM4 attempts with separate plans, Releases, and restart scopes. The parent
stays active, released, and serving in every outcome, which every vector checks. The
[`CBI22 capability contract`](../../component-management/cbi22-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi22-contract-completeness-review.md)
bound it to one child attached to one runtime-open Port of one released parent.

CBI23 nests those attachments and answers both questions the item recorded. Nesting itself needed no
code: a child activation is an ordinary CBI13 activation, so passing it as a parent already worked, and
CBI22's review said exactly that — the one stated limit in four slices that turned out to be accurate.
What the chain forces is an ordering question, and **CM4 models no relationship between a parent and a
child after attachment**: it requires the parent scope active when the child attaches and preserves it
through the activation, and nothing records that a scope has children or stands a child down when its
parent goes. Every earlier slice could take a runtime object's shape as the answer to an ordering
question — CBI12's release barrier, CBI17's generation, CBI19's restart scope. Here there is no shape to
take, and the answer had to come from what an attachment *is*.

**A child is retired before the parent whose Port it occupies**, because an attachment occupies a Port
of a generation and cannot outlive the generation offering it. A withdrawal therefore cascades
deepest-first, with the relation derived from each activation's own CM4 observation rather than declared.
CBI22's independence claim is not contradicted: that one was one-directional, and this is the other
direction, which only a chain makes askable. **Depth is not bounded**, because no model bounds it and an
invented number is what CBI11 refuses for elapsed time and interaction counts; a fourth level is
exercised to show the second was not special.

The hole is stated rather than implied away: **the root can only order what it is given**. A child the
caller omits is invisible, because deriving the whole forest would need a record of a scope's children
that neither CM2 nor CM4 keeps, so every outcome names exactly the scopes it retired and what was not
ordered is visible by absence. A cycle is reported rather than refused, since no sequence of attachments
can produce one — the guard exists so the ordering terminates, not to catch a caller. The
[`CBI23 capability contract`](../../component-management/cbi23-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi23-contract-completeness-review.md)
bound it to a forest the caller supplies.

CBI24 replaces a generation when child activations are attached to Ports it offers, and the item's own
name turned out to be optimistic. **There is no migration operation**: re-pointing an attachment at a
successor would need CM4 to hold the declaration as mutable state, and it holds it as an input to one
activation attempt. A Port does not migrate — a child is stood down and stood up again, and the
standing-up is the child's own attachment against a generation that must already exist.

The finding is what happens if nobody does that. **A replacement silently orphans every attachment
beneath the generation it replaces, and CM4 does it deliberately**: its C2 property preserves the
generation and activity state of every *unrelated* scope, and a child scope is unrelated, so a cutover
rewrites the target scope and carries the child through untouched. The child keeps running while the
parent generation its attachment recorded stops existing anywhere, and nothing looks again — the
attachment was validated once, at attach time.

So the cascade runs **before** the cutover, which is the opposite order from CBI19's retained members,
and the asymmetry is the reasoning worth keeping: **which side of the transaction a thing lives on
decides when it goes.** A retained member is inside it and CM4 requires a pre-cutover failure to leave
it serving; an attachment is outside it, in a scope CM4 will not touch either way, so leaving it up
would produce exactly the orphan. A failed replacement therefore leaves the parent serving and does
*not* restore the children, because restoring one would be a fresh activation this call never made —
what it owes instead is naming every scope the cascade retired. The supplied set is a forest rather
than a flat list of direct children, which the first draft got wrong and a two-level vector caught.

What the root cannot do is notice an attachment it was not given. CBI19 and CBI20 stay reachable with
children attached, and a named test proves the orphan rather than describing it. That hole is now
**Decision 14**, raised from both directions: CBI23 cannot discover unnamed children to order them,
and CBI24 cannot detect them to protect them. The
[`CBI24 capability contract`](../../component-management/cbi24-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi24-contract-completeness-review.md)
bound it to the attachments the caller supplies.

CBI25 carries a position resolved with **mediated exposure** into portable preflight, and its first
result is that **the seam's refusal was the answer rather than the obstacle**. `PortableExposure.Mediated`
is refused because *"an erased Mediation still carries provenance, deputy, and authority obligations"*.
Read as a requirement rather than a wall, that sentence says what a correct translation must do: keep
the obligations with a holder. CM2 supplies one — a policy-bearing Mediation must be realized as a
**dedicated Component** — and a Component is exactly what Portable Binding binds. So a mediated position
is translated by **binding the mediator**: the plan's provider fact names it because it is who answers,
nothing mediated is ever presented to the seam, and no refusal is relaxed.

**This is the opposite outcome from CBI21, and the difference generalises.** Relational Initialisation
is unreachable because the seam has no stage, no verb, and no window for it. Mediation is reachable
because the seam needs nothing new. A refusal in a published seam is therefore not evidence either way
about whether a capability can be integrated; what decides it is whether the refused thing has a
representation the seam already holds.

A **static-host** Mediation is refused whatever Component it names, because the host is the mediator and
a binding to it reaches nobody — the composition root binds the members directly and does the mediation
as its own work. Two of the slice's refusals exist because the falsification pass demanded them rather
than because the design anticipated them: a static host *naming* a Component, and a **distinct** position
that declares a Mediation, which CM2 records and ignores. Without either vector the corresponding check
could not fail.

The deliberate stop is authority. **The mediator's authority is not admitted here**: CBI3 admits against
an occurrence and the mediator has one, so admission is mechanically possible — but whether that
occurrence's grants may stand for the obligations CM2 says the *Mediation* owns is a question about what
a deputy is, and admitting the mediator would answer it invisibly. The
[`CBI25 capability contract`](../../component-management/cbi25-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi25-contract-completeness-review.md)
bound it to preflight.

CBI26 admits the authority of the mediator CBI25 binds, and the answer comes from a concept CM5 does
not have. **CM5 has no deputy.** Its relationship kinds are `AttachedDevice`, `ExternalPeer`, and
`ComponentParticipant`, none of which means *acts on behalf of*, and a `LocalCapabilityGrant` names
exactly one `Holder` with no beneficiary beside it. So a mediator is admitted for **what it does
itself** — CBI3's admission, unchanged, against the mediator's own occurrence — and **a Mediation
declaring that it owns authority is refused**. Admitting the mediator and letting its narrow grants
stand for the members' interaction would decide what a deputy is in the least visible place available,
which is the erasure CBI25 avoided at the seam arriving instead at the admission.

Only `OwnsAuthority` decides. The other five ownership flags — mutable membership, residue,
backpressure, recovery, lifecycle — describe what the mediator does with the set behind it, which is
not a question about who may exercise a Capability, and two vectors pin that an admission survives
them.

**This is CBI21's answer arriving in a different model, and the pattern is the thing to carry.** Twice
now, a capability CM2 or CM3 can declare has turned out to have no representation in the model that
would have to carry it: Relational Initialisation in the portable seam, deputy authority in CM5.
Neither was a plumbing problem, and in both cases naming the missing concept was worth more than
building something shaped like the capability. CBI25 sits between them as the counter-example — a
refusal that looked the same and was not, because the refused thing did have a representation. The
[`CBI26 capability contract`](../../component-management/cbi26-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi26-contract-completeness-review.md)
bound it to the mediator's own authority, and **Decision 15** records the disagreement between CM2 and
CM5 for their owners.

CBI27 carries a position whose cardinality is **not** `1..1` into portable preflight, and CBI25's test
answers the question this index recorded: a Provider Set's *members* each have a representation the
seam already holds — one provider answering one contract — while the *set* has none, since nothing in
the seam says these bindings answer one requirement together. So a wide position is **n ordinary
members, and the set stays at the composition root**, which already holds several members as one
activation. The seam is not widened and its cardinality refusal is untouched.

Its finding is what the fan-out needs and CM2 does not supply. **A CM binding scope and a portable one
are not the same identity.** The portable scope names one binding — it survives replacement, and the
seam's own `scope-uniqueness` silence says a composition reusing one has "two members claiming one
position, which its own resolver is the place to reject". A CM scope is a container: CM2 looks occupied
bindings up by scope *and contract*, tells them apart by `BindingId`, and refuses several in one scope
only when the position is `1..1`. CBI1 maps one onto the other, which holds under two conditions it
never states — the position is `1..1`, and the scope holds one position — and **the second is already
false**: the multi-member slices resolve two or three positions in one CM scope, so every member of
those activations reports the same portable scope. Both stacks do it identically and no vector asked,
because every fixture clones one requirement template. That is the shape Decision 10 describes, and the
fourth stated limit in this programme that turned out to describe how something was called. A named
test in each stack pins it; correcting it moves every member's `bindingScope` fact and so every CBI4
digest the shared fixture pins, which is a repin rather than a slice's work, so it is **Decision 16**.

The rest follows from the two contracts rather than from preference. The membership is the generation's
statement, as CBI20's is. A refused member leaves **no** member at all, because the seam refuses a wide
bound "rather than narrowed to a first member" and keeping the ones that worked would be that narrowing
performed where the seam cannot see it. A position that resolved nothing is reported as its own outcome
rather than as an empty success. Two things the set states are named rather than approximated: nothing
here owns whether a `1..3` set still satisfies its minimum after losing a member — the activation's
answer, retire everything, is stricter than the set requires — and unfilled optional capacity is
reported rather than fillable. The deliberate stop is that **a fanned-out set has no activation path
yet**, because the group path prepares each member through CBI1, which refuses a wide position. The
[`CBI27 capability contract`](../../component-management/cbi27-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi27-contract-completeness-review.md)
bound it to preflight.

CBI28 activates the members CBI27 fans out, and the lift needed less than the item expected.
**Nothing downstream needed teaching**: a wide position's members are distinct occurrences, and every
slice from CBI12 onward is per-occurrence — CM3 plans occurrences, CM4 stages them, CM5 admits against
them, CBI16 attributes interaction per member. What the activation was missing is the one thing CBI27
found CM2 does not supply, a binding scope per member, so an activation member now carries one where
the generation cannot name it, and the rest follows unchanged.

Its finding is a hole the lift would otherwise have opened. **A wide position can be supplied
half-complete and both of CBI12's checks pass**, because each compares the caller's member list with
the caller's CM3 plan — two things the caller supplies — so omitting one member of a three-member
position and planning from the two that remain satisfies both, and the position comes up short-bound
with no refusal anywhere. Routing a wide position through CBI27 as a whole makes the generation the
authority, and the refusal a caller gets is CBI27's own rather than a restatement of it.

**The position's declared minimum is not a runtime concept**, and that is the answer to the question a
wide set poses that a single member cannot. `Cardinality.Minimum` says a `1..3` position is satisfied
by one provider, which makes "keep serving with two of three" look reachable; it is not. CM2 uses the
number at resolution to decide how many members to select and then stops carrying it, the
required-versus-optional split survives only as a **Proposed Stack decision** — provenance about how
the generation was formed — rather than as a fact about a member, and neither CM3's plan nor CM4's
attempt has any notion of an optional member. A runtime that wanted to run a degraded set could not
tell which members it may lose, so one member short of Ready retires the whole activation, siblings
included. That is CBI27's C7 exercised rather than asserted, and it is a third instance of the pattern
CBI21 and CBI26 recorded: a capability one model can declare, with no representation in the model that
would have to carry it.

Scope distinctness is checked within the position and deliberately **not** across the activation,
because two ordinary positions resolved in one CM binding scope already reach the seam as two members
reporting one scope — Decision 16, still open, and now re-pinned by a property here. An activation
member gaining a binding scope is a breaking change to the Minimal record and a source-compatible
addition to the Reference one; the migration is to leave it absent, which is every member every earlier
slice activates. The [`CBI28 capability contract`](../../component-management/cbi28-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi28-contract-completeness-review.md)
bound it to the fake runtime.

CBI29 closes CBI28's explicitly bounded child-Port combination. A complete wide position now enters
CBI22's existing child path, where CBI27 still translates the generation's whole membership and CBI28
still applies one authority and Release barrier across it. Each portable member scope remains distinct
from the child's one restart scope, and every outcome leaves the released parent unchanged. No new
activation surface was needed. The exercise did expose a classification defect: the child wrapper
turned CBI28 preparation refusals into `child-establishment-refused`; both roots now preserve plan and
preparation codes while reserving that generic code for provider/runtime establishment failures. The
[`CBI29 capability contract`](../../component-management/cbi29-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi29-contract-completeness-review.md)
bound the result to one wide position in one runtime-open child Port.

CBI30 begins the **real-distribution area** with its narrowest executable boundary: CBI2 now runs
against the negotiated Portable Binding realization in a real provider process, and each host runs
against both stacks' provider executable. The result proves process isolation and cross-stack
substitution, preserves CM4 Release and portable retirement, and exposes process loss as
`portable-process-interrupted`. It deliberately does not call this production distribution: the
manager remains fake and host-local, and no artifact acquisition, verification, staging, launch or
sandbox policy, cross-domain identity, retry, or recovery exists. The
[`CBI30 capability contract`](../../component-management/cbi30-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi30-contract-completeness-review.md)
bound that distinction. Physical distribution and production integration are the next area, with no
integration slice queued ahead of them.

CBI31 closes the first local physical-distribution slice. Both composition roots verify the
SHA-256 digest of one already-present provider executable, require canonical allowed-root and exact
argument-vector policy, launch it as a dedicated no-shell child with redirected streams, and own
cleanup through CBI30 retirement or forced disposal. The
[`CBI31 capability contract`](../../component-management/cbi31-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi31-contract-completeness-review.md)
keep the claim narrow: the source is host-controlled, verification and execution are not an atomic
untrusted-source boundary, and no content-addressed staging, multi-file package, removal policy,
sandbox, or trust service exists. Content-addressed staging and removal for a declared multi-file
artifact set is the next physical-distribution boundary.

CBI32 implements that boundary for one host-local store owner. A canonical manifest identity covers
the complete relative-path/digest set plus executable and argument metadata; verified bytes are
published atomically from a private sibling transaction, identical content is reverified and reused,
activation holds a removal lease through CBI31, and exact removal preserves sibling identities and
source content. The [`CBI32 capability contract`](../../component-management/cbi32-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi32-contract-completeness-review.md)
bound the result to already-present host-controlled sources and a process-local lease table. A
bounded attributable acquisition stream into that transaction is next; remote transport success,
publisher evidence, signature verification, and local admission must remain distinct observations.

CBI33 implements that byte-bounded acquisition seam without claiming a network protocol. Each
request names an expected source, exact member lengths, and a checked total limit; each member is
opened once in canonical order into private transaction state. Results keep source attribution,
transport completion, publisher evidence, and CBI32 admission distinct, including delivery that
completes before digest admission fails. The
[`CBI33 capability contract`](../../component-management/cbi33-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi33-contract-completeness-review.md)
bound the synchronous injected source to bytes rather than time and explicitly leave publisher
authentication and trust absent. Verifiable publisher evidence over the canonical manifest is next;
cryptographic validity, host trust, and local admission must remain separate observations.

CBI34 implements that evidence boundary with detached ECDSA P-256/SHA-256 signatures. A golden
length-prefixed payload binds the CBI32 content identity, ordered member paths, digests and CBI33
lengths, executable, and arguments; source and byte budget remain host transport policy. The exact
SPKI digest identifies the signing key, while every result keeps host trust and artifact admission
unevaluated. The [`CBI34 capability contract`](../../component-management/cbi34-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi34-contract-completeness-review.md)
bound the proof to private-key possession: there is no publisher-name binding, freshness, trusted-key
registry, rotation, or revocation. Explicit host trust policy over verified publisher keys is next;
key admission and revocation must remain distinct from signature validity and CBI32 admission.

CBI35 implements that trust-decision boundary over one immutable canonical policy snapshot. Exact
publisher-key identities are admitted or revoked explicitly; unknown and unverified keys remain
distinct, and successful authorization is scoped to the policy, key, content identity, and payload
digest without opening a source. The [`CBI35 capability contract`](../../component-management/cbi35-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi35-contract-completeness-review.md)
bound the result to caller-supplied snapshots with no provenance, distribution, freshness, key
lineage, or retroactive cancellation. Trust-gated CBI33 acquisition is next: a matching authorization
must be required before any source is opened, without folding policy loading or networking into it.

CBI36 implements that trust-gated composition. Authorization is issuer-controlled, and the gate
validates one immutable acquisition request and matches its exact content identity and canonical
publisher payload before CBI33 may inspect the source. Trust success remains separate from later
source, transport, integrity, and admission outcomes. The
[`CBI36 capability contract`](../../component-management/cbi36-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi36-contract-completeness-review.md)
bound it to a same-process host-selected policy snapshot. Trust-policy provenance and monotonic
updates are next: newer revocations must have explicit authority and supersession semantics before
the stack claims production remote distribution.

CBI37 implements authoritative policy provenance and monotonic supersession in process. One exact
P-256 authority SPKI is pinned out of band; signed snapshots form a strict sequence/predecessor chain,
and governed acquisition rejects authorizations from older snapshots before source access. The
[`CBI37 capability contract`](../../component-management/cbi37-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi37-contract-completeness-review.md)
bound the state to one process and one immutable authority pin. Durable atomic checkpointing is next:
authority, sequence, and current policy identity must survive crashes and detect rollback before
production remote policy distribution is claimed.

CBI38 implements that durable checkpoint boundary. The complete signed CBI37 chain is encoded under
strict bounds, published before live advancement, and re-verified on recovery. An independently
retained authority, sequence, and policy-identity floor detects missing, older, or conflicting state.
The [`CBI38 capability contract`](../../component-management/cbi38-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi38-contract-completeness-review.md)
bound the implementation to one process and writer and do not claim secure floor custody. Authenticated,
fresh, bounded remote policy distribution is the next boundary; authority rotation and a platform
rollback anchor remain separate deployment/security work.

CBI39 implements one authenticated, fresh, bounded distribution attempt. A host-pinned P-256
endpoint key signs the cryptographic challenge, exact CBI38 cursor, short validity interval, and
complete optional CBI37 update digest; the client makes one cancellable source call and never retries.
The [`CBI39 capability contract`](../../component-management/cbi39-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi39-contract-completeness-review.md)
keep endpoint authentication separate from policy authority and durable publication. A portable wire
codec and concrete bounded network transport adapter are next; scheduling, retry/backoff, endpoint
rotation, secure clock, and platform rollback anchoring remain separate work.

CBI40 implements the portable wire and concrete transport seam. Both roots share exact golden binary
request/current/update images, reject malformed or extended state, and issue one HTTPS POST through
an injected `HttpClient`; exact final URI/status/media metadata, absent content encoding, cancellation,
and independent declared/streamed 1 MiB bounds precede CBI39 authentication. The
[`CBI40 capability contract`](../../component-management/cbi40-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi40-contract-completeness-review.md)
keep handler TLS/DNS/proxy policy host-owned. A polling scheduler with bounded retry/backoff and
durable success-floor handoff is next; endpoint rotation and platform anchors remain separate work.

CBI41 implements that scheduler as one bounded, host-driven poll cycle rather than a service. It
advances until the endpoint reports the host current, and **retries only what a fresh attempt can
change** — a new challenge, a freshly read cursor, and the network — so transport failure, timeout, a
stale window, and a superseded cursor are retried while every endpoint-authentication outcome and
every registry refusal ends the cycle at the attempt that produced it. Repeating a request the pinned
key just failed to authenticate cannot change the answer and would only send more traffic to an
unauthenticated peer. Backoff is a function of *consecutive failures* rather than the attempt index,
so **progress resets it**, and the gap sequence carries no jitter — which is what lets fourteen
shared vectors pin an exact gap sequence across two independent realizations, and which costs
nothing, because the cycle asks for a duration and the host decides how to wait it.

Its finding is an ordering one, and it runs opposite to CBI38's own rule. CBI38 publishes its
checkpoint *before* advancing live state; CBI41 hands off the recovery floor *after* publication and
never before. **A floor is a statement about what the host durably holds, so it cannot precede the
thing it describes**: a floor retained ahead of its checkpoint and interrupted by a crash claims a
state no checkpoint records, and recovery reads that as `policy-checkpoint-rollback-detected` — a
refusal to open a checkpoint nothing rolled back. A lagging floor under-detects for one update and
self-heals on the next recovery; a leading floor denies service and does not. The sink observes the
ordering rather than being told it, by reopening the checkpoint as the floor arrives. A refused
handoff therefore stops the cycle and reports the applied sequence with no matching retained one:
the update is durable and cannot be undone, and every later advance would move further past a floor
the host does not hold.

The slice also records a disagreement between two earlier ones rather than resolving it. CBI39
declares a superseded cursor, reachable only when something advances the registry mid-attempt, while
CBI38 bounds itself to one process and one writer — so the category exists in one slice and is
excluded by the next. The shared vector provokes it by writing from the fake source, deliberately
outside CBI38's bound, because a declared category with no reachable path is the defect PB6 found
three of. The [`CBI41 capability contract`](../../component-management/cbi41-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi41-contract-completeness-review.md)
keep scheduling, timers, offline policy, and floor custody outside the slice. Durable custody of the
handed-off floor, consumed by `Open` on the following start, is the next boundary; endpoint and
authority key rotation and a platform rollback anchor remain separate security work.

CBI42 supplies that custody and closes the loop: a floor is retained after publication, survives the
process, and is what the next start hands to CBI38. Its finding is what *not* to do with a recovered
checkpoint. CBI38's `Open` returns a floor derived from the chain it has just replayed, and writing
that back is the obvious move — it would even repair the crash-window lag CBI41 recorded. It is
exactly wrong. **A checkpoint that can raise its own guard makes the guard follow whatever the
checkpoint says**: a forged chain reaching further than the true one would be adopted as the floor
and would then refuse every genuine successor, wedging the host at the forgery with no way back that
does not involve deleting the guard. The floor advances only by a handoff from a publication this
host performed, which also **narrows CBI41's note that the lag "self-heals on the next recovery"** —
the in-memory floor does, for that process; the durable floor does not.

A second question had no answer until the shape changed. A missing store could mean "nothing has
happened yet" or "the guard was deleted", and nothing about the store distinguishes them. So the
store is **established at zero before the checkpoint it guards exists**, absence afterwards means
the guard was removed, and a checkpoint with no store is refused. That ordering also avoids
reintroducing the false alarm CBI41 argues against: a crash between the first publication and the
first handoff leaves a store at zero beneath a checkpoint at one, and zero never trips rollback
detection.

Writing the slice also caught one of its own checks doing nothing. Three corruption vectors — a
flipped version marker, a truncation, a trailing byte — are all refused by structural parsing
*before* the integrity tag is consulted, so a store that never checked its tag passed every one of
them, which a deliberate defect demonstrated. A fourth vector alters a byte the parser accepts,
yielding a well-formed but different sequence that only the tag can refuse. What the tag does not do
is stated rather than implied: it detects corruption, not an adversary who can write the file and
recompute it. The [`CBI42 capability contract`](../../component-management/cbi42-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi42-contract-completeness-review.md)
bound it to one host-local store under one writer. **Custody in a domain the checkpoint's writer
cannot reach** — a platform rollback anchor, a sealed key, or an attested counter — is the next
boundary, and is the one thing this slice repeatedly had to decline.

CBI43 does not attempt that boundary, because it is a deployment property rather than a software one,
and instead runs the distribution programme **as one path rather than as pairs**. A polled,
floor-guarded trust policy authorizes publisher evidence over an acquisition request; the governed
acquisition stages a content-addressed set; the store launches its executable as a provider process
under a removal lease; and CBI30 activates a portable member across that process to Release. It adds
no capability, and it reclassifies no refusal — every failure keeps the code its slice produced and
gains only an origin, because a composition that renamed them would delete the programme's most
useful diagnostic at the point a host reads it.

Its finding came from breaking its own trust gate. Removing CBI43's trust check **opens no source**:
the governed acquirer refuses a missing authorization on its own. What it destroys is the reason —
"the policy revoked this publisher" becomes "trust was required". **That step earns its place by
preserving attribution, not by adding a barrier**, and the contract now says so rather than implying
a second safety check that does not exist. The other correction was in the composition's own first
draft, which guessed a transport code and so reported a delivered-then-rejected set as a transport
failure instead of CBI32 refusing admission; the shared vector caught it.

What the composition did *not* find is worth recording too: no defect in the slices themselves. Six
phases of pairwise work had already fitted them to contracts that composed. The evidence is a
ladder — policy applied, authorized, source opened, staged, launched, released — required to be a
true-prefix in **every** vector, so a stage reached past its own refusal fails anywhere, together
with residue checks that no refusal leaves a staged set, a live process, or an advanced floor. The
[`CBI43 capability contract`](../../component-management/cbi43-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi43-contract-completeness-review.md)
bound it to one provider and one member, and record that nothing revalidates the policy between
acquisition and launch. Custody in a privileged domain remains the named next boundary.

CBI44 closes the window CBI43 named, and its finding is about the step CBI43 already had rather than
the one it adds. **The launch takes its own trust decision.** The verified publisher evidence is
evaluated again against the policy the registry holds when the executable is about to run, so a
publisher revoked or dropped between acquisition and launch does not run — and the refusal is
CBI35's, unchanged, because the ladder rather than the code is what says where it was decided.

**The decision is compared, not the snapshot**, and that is the choice a second vector had to force.
Refusing because the policy identity moved is what the word *revalidate* suggests, it passes five of
the six vectors, and it would refuse every benign update a polling host receives. Only
`unrelated-revocation` — an update that revokes some other publisher — separates the two designs, and
without it two independent implementations would have agreed on the wrong one, because the contract
would have been silent rather than ambiguous. That is Decision 10's shape, arriving before the defect
instead of after it.

Its other result **corrects CBI43's reading of itself by symmetry**. CBI43 recorded that its
acquisition trust step earns its place by preserving attribution and not by adding a barrier, since
the governed acquirer refuses a missing authorization anyway. The launch step looks identical at the
call site and is the opposite: deleting it launches a revoked publisher's executable and reports
`active`. Two steps that read the same and differ completely in what they are for, and the only way
to tell which is which is to remove each and look — which both stacks did.

Two checks a reader would expect are **absent on purpose**. A guard that the launch decision names the
artifact the store staged cannot fail, because the evidence, the request, and the staged identity all
derive from one object and CBI36 already refuses a mismatch, so it is a property rather than a refusal
code — PB6's three unreachable categories are why. And the ordering of the trust decision before the
store's reverification is stated rather than pinned, because no reachable input makes them disagree.
The [`CBI44 capability contract`](../../component-management/cbi44-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi44-contract-completeness-review.md)
bound it to one chain call, and name the next boundary: **revalidation for a member that is already
serving**, which is CBI5's shape applied to trust rather than to authority.

The fake Component Manager and the portable seam now meet across every structural case the two models
share and across one real process boundary.

CBI45 closes CBI44's final named window with one explicit serving revalidation. The chain now retains
the verified publisher evidence and policy authority that actually governed launch, and each root
binds its lifecycle to that chain in an opaque serving activation rather than accepting two caller-
paired results. A current policy that still admits the publisher preserves service even when its
identity changed; revocation or removal retires the member, terminates the concrete provider, releases
the store lease, and attempts staged-set removal while preserving CBI35's refusal and reporting any
cleanup failure separately. The
[`CBI45 capability contract`](../../component-management/cbi45-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi45-contract-completeness-review.md)
bound it to one host-driven call over one member. Host invocation policy and fan-out across a serving
set were its next boundary.

CBI46 now supplies that boundary as one caller-triggered sweep over 1-64 opaque serving activations.
Both roots preflight the whole set, sort by typed occurrence identity, invoke CBI45 for every member,
and report complete per-member observations plus an aggregate. The completeness pass found that
naive CBI45 composition could remove a staged identity still used by a continuing sibling; sweep-
owned cleanup now removes shared bytes only when no swept member using them continues. The
[`CBI46 capability contract`](../../component-management/cbi46-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi46-contract-completeness-review.md)
bound it to one explicit sequential call.

CBI47 supplies the first scheduling boundary as one bounded in-process cadence. Each cycle establishes
current policy through CBI41 before taking the serving-set snapshot and applying CBI46; the first cycle
is immediate and later cycles use injected time. Empty serving sets are successful no-ops, successful
withdrawal continues, and cancellation, non-current policy, or an invalid/incomplete sweep stops
visibly. The [`CBI47 capability contract`](../../component-management/cbi47-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi47-contract-completeness-review.md)
keep this distinct from a daemon or durable schedule. Durable retry/resumption and offline/restart
policy were its next host boundary.

CBI48 now provides durable resumption for one bounded, single-owner cadence run. Both roots atomically
persist a distinct run identity, schedule, prepared instant, ordered observations, and an in-flight
marker before each effectful cycle. Committed cycles resume without replay; an interrupted cycle is
indeterminate because policy publication, floor retention, withdrawal, and cleanup cannot be one
transaction with the journal. Retry or abandonment therefore requires an explicit reconciliation
decision. The [`CBI48 capability contract`](../../component-management/cbi48-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi48-contract-completeness-review.md)
bound that claim.

CBI49 now supplies the offline and reconciliation decision boundary. A host chooses a bounded grace
and retry interval; only an exhausted transport failure or timeout can preserve providers that are
already serving, the deadline remains anchored to the last cycle that established current policy,
and no offline outcome authorizes acquisition, launch, admission, or restart. An interrupted CBI48
cycle accepts only reconciliation evidence naming its exact run, index, and instant: confirmed
no-effect selects retry, accounted effects select abandonment, and unknown or mismatched evidence
leaves the journal inert. The
[`CBI49 capability contract`](../../component-management/cbi49-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi49-contract-completeness-review.md)
bound the decision as effect-free.

CBI50 now enforces that decision over one exact, bounded serving snapshot. Eligible grace is
effect-free; expiry, integrity refusal, missing baseline, and invalid offline observations retire and
terminate every admitted provider in typed occurrence order. Preflight refusal has zero effect, one
member outcome does not hide siblings, and availability enforcement deliberately retains staged
artifacts because it is not a publisher-trust withdrawal. The
[`CBI50 capability contract`](../../component-management/cbi50-capability-contract.md) includes the
phase's contract-completeness review.

CBI51 now supplies the provider restart policy. Restart eligibility requires a stopped activation,
an availability or unexpected-exit cause, an exact identity proving that a successful current cycle
observed the registry's present policy, and a fresh publisher authorization for the retained content.
The effect-free decision applies a bounded attempt count and deterministic retry delay; trust
withdrawal, operator retirement, stale current proof, malformed history, and exhausted attempts deny
restart. The [`CBI51 capability contract`](../../component-management/cbi51-capability-contract.md)
includes the phase's contract-completeness review.

CBI52 now enforces one ready restart decision. The stopped activation owns the retained recipe;
enforcement rechecks CBI51, re-verifies the complete staged artifact set, launches a new dedicated
provider, and prepares, interconnects, observes Ready, and releases a fresh portable member for the
same occurrence while carrying forward the prior logical runtime. One activation can yield at most
one successful successor, and refusal or failed reconstruction releases the claim and preserves the
retained content. The
[`CBI52 capability contract`](../../component-management/cbi52-capability-contract.md) includes the
phase's contract-completeness review.

CBI53 now makes the restart-attempt history durable. A host-local journal is bound to one occurrence,
retained staged identity, and bounded policy; committed failures drive the next CBI51 decision, an
in-flight record precedes CBI52 effects, and interrupted work remains indeterminate until explicit
retry or abandonment. The shared vectors pin the portable history model in both roots. The
[`CBI53 capability contract`](../../component-management/cbi53-capability-contract.md) includes the
phase's contract-completeness review.

CBI54 now supplies host-local cross-process restart ownership. A live operating-system file lock
excludes competing host processes, while an atomically published integrity-checked epoch fences
successor leases even when caller identities are reused. Process death releases exclusivity without
erasing the prior epoch or claiming that an interrupted CBI53 attempt had no provider effect. The
[`CBI54 capability contract`](../../component-management/cbi54-capability-contract.md) includes the
phase's contract-completeness review.

CBI55 now supplies external reconciliation of that interrupted provider effect. A durable record
binds the exact CBI53 attempt to its CBI54 fence before launch; the cooperating provider holds a
token-specific operating-system lease and publishes a bounded process receipt. A strictly later
owner may select retry only after proving the lease free, or after matching and terminating the exact
orphan and then proving it free. Missing, corrupt, mismatched, unavailable, or still-busy evidence
leaves the journal in-flight. The
[`CBI55 capability contract`](../../component-management/cbi55-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi55-contract-completeness-review.md)
bound the claim to one cooperative host and provider lifetime.

CBI56 now supplies cooperative distribution-endpoint key rotation. The current CBI39 endpoint signs
one exact generation-plus-one successor, the host durably stages it, and only a complete CBI39
synchronization authenticated by that successor activates it. Ordinary polling remains pinned to
the active key, and the anchor is integrity-checked and externally floor-aware. The
[`CBI56 capability contract`](../../component-management/cbi56-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi56-contract-completeness-review.md)
bound the claim to response-authentication key rotation on one host.

CBI57 rotates the other key — the CBI37 authority that signs publisher-trust policy — and its result
is that **the shape CBI56 just established is the wrong one here, for a reason that generalises**.
CBI56 keeps its successor in a separate anchor and proves possession by completing one live
synchronization under the staged key, because an endpoint key authenticates a response that is judged
once and leaves nothing behind. A policy-authority key signs the record CBI38 replays on *every*
start, so its rotation is a fact about history rather than about now: a successor recorded beside the
chain leaves recovery unable to verify the updates the predecessor signed, and trusting them
unverified is exactly what that replay exists to prevent. **The rotation is therefore a link in the
same retained chain the policy updates form**, and recovery re-verifies each update against the
authority in force at its own position.

The same difference removes a phase rather than adding one. **Possession of an authority key is
proven by a signature, which is all an authority key ever does**, so the successor's countersignature
over the same manifest is the proof, and CBI57 has no staged successor to announce, confirm, or
abandon — the absent phase is the contract, as it was for CBI17's synchronous succession, and a named
test asserts the absence. What the pin buys is that it never moves: CBI43's chain records the trust
root rather than the signing key, so CBI44's launch decision and CBI45's serving revalidation compare
an identity a rotation cannot disturb, and both roots run a serving member across a rotation *and* an
update signed by the successor to show it. Had the verified snapshot begun naming the signing key,
every rotation would have retired every serving member — CBI44's decision-versus-identity finding
arriving one level up.

Writing the slice also caught one of its own tests proving nothing: the first C7 draft compared the
current policy snapshot before and after a rotation, which no implementation could fail, because a
rotation does not touch it. It was replaced by the *next* snapshot still naming the pin, which a
wrong implementation moves. The retained record advances its format marker only when a rotation
exists, so a host that never rotates keeps the bytes CBI38 wrote. The
[`CBI57 capability contract`](../../component-management/cbi57-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi57-contract-completeness-review.md)
bound it to host-local cooperative rotation of the signing key and record what it does not reach: the
pin itself, remediation of a compromised predecessor — which can still sign an alternative successor
at its own generation, refused only by a retained floor — and how rotation statements reach a host.
Distributed ownership, detached-effect custody, privileged floor custody, and production isolation
remain separate work.

CBI58 supplies the rotation statement CBI57 intentionally left injected. It is a separate
single-attempt distribution path so the strict CBI39/CBI40 policy-update records and golden wire do
not change. The active distribution endpoint signs a fresh response binding the exact durable policy
cursor, active authority generation and identity, and the digest of zero or one complete CBI57
statement. The client rechecks that cursor after authentication and routes a statement only through
the durable registry, preserving every native CBI57 refusal. The
[`CBI58 capability contract`](../../component-management/cbi58-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi58-contract-completeness-review.md)
bound it to one injected attempt. CBI59 supplies a separate strict big-endian, strict-UTF-8 wire for
the complete CBI58 request and response plus a concrete single-attempt HTTPS source. It accepts only
the configured absolute HTTPS endpoint, exact unparameterized media type, status 200, and bodies no
larger than 1 MiB by both declared and streamed size; cancellation propagates and it never retries.
The [`CBI59 capability contract`](../../component-management/cbi59-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi59-contract-completeness-review.md)
preserve the CBI39/CBI40 wire and CBI57 decision boundary.

CBI60 supplies that host-owned durable scheduling and retry, as one bounded cycle of CBI58 attempts
with CBI41's shape applied to the other key. Backoff is a jitter-free function of *consecutive
failures* that an applied rotation resets, and retry is confined to what a fresh attempt can change,
which the slice had to re-derive rather than inherit: a rotation cycle also faces the native CBI57
refusals a policy cycle never sees, and they are not retryable because the endpoint would offer the
same statement again. Each applied rotation is handed to the floor only after CBI57 has published it,
so a refused handoff reports an applied generation with no matching retained one rather than undoing
a rotation that is already durable.

Its finding is about the guard rather than the cycle. **CBI42's central trick is an ordering that a
later guard cannot have.** CBI42 establishes its policy floor at zero *before* the checkpoint exists,
which is precisely what lets a later absence mean the guard was removed; the authority floor is
introduced after those checkpoints already exist, so absence is ambiguous between "this host never
rotated" and "the guard was deleted". It is therefore **adopted at zero and reported as adopted**
rather than refused or reported as a recovery — the host is told the guard did not exist, which is
the whole of the difference the slice can honestly claim.

What that adoption costs turned out to be **one case rather than a class, and the chain is why**. A
truncation dropping a rotation that has later updates is already refused as an invalid chain, because
those updates name the successor authority the truncation removed; a truncation dropping policy
updates is already refused by CBI42's floor. The only case this guard alone detects is a checkpoint
truncated at a *trailing* rotation, and each stack pins both directions with a named test rather than
asserting the coverage. The [`CBI60 capability contract`](../../component-management/cbi60-capability-contract.md)
and [`contract-completeness review`](../../component-management/cbi60-contract-completeness-review.md)
bound it to one host-driven call under one writer, with CBI42's custody limit unchanged: the
integrity tag detects corruption, not an adversary who can write the file and recompute it.

CBI61 runs that cycle alongside CBI47's, and the two turn out to be one cycle rather than two loops.
**The order is decidable from the registry rather than chosen**: a policy update is verified against
the authority in force, so an update signed by the authority a pending rotation installs is refused
until that rotation is retained. Rotating first applies it; polling first refuses it. A shared vector
distinguishes the two orders by whether a sequence was applied, which makes the ordering a test
rather than a comment. CBI47's cadence needed no change — its loop is generic over a cycle, so
governing it is a new cycle — and the one change to CBI47 is that a cycle result now carries the
rotation it ran, which makes the pairing structural instead of positional.

Its finding is what a later slice did to an earlier vector. **CBI41's `foreign-authority` vector
became half-right when CBI57 landed.** `policy-update-authority-mismatch` could only mean a stranger
before an authority could rotate, and the vector's name says so; afterwards the same observable also
describes a legitimate publisher a host has not caught up with. CBI41 is neither wrong nor changed —
failing closed on an update it cannot verify is correct either way — but nothing below the
composition can tell the two causes apart, because only a cycle that ran both loops knows whether a
rotation was attempted and what it reported. This is the first case in the programme where a later
slice made an existing refusal **ambiguous without making it incorrect**, which is a different shape
from the four stated limits that turned out to describe how something was called.

Attribution is therefore a conjunction of two recorded facts rather than a reading of the poll code:
`provider-trust-cycle-authority-behind` appears exactly when the poll refused with that code *and*
the same cycle's rotation did not reach current. A rule that looked only at the poll code would
relabel a stranger's update as a rotation lag, and the vector pair that differs **only** in the
rotation outcome is what forces the distinction — without it both readings pass. Which rotation
outcomes are fatal follows from what each changed: a refused or exhausted rotation changed nothing
and is recorded beside a cycle that may still report current, while a rotation published without its
guard stops before the policy endpoint, for CBI41's own reason about its own floor. The
[`CBI61 capability contract`](../../component-management/cbi61-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi61-contract-completeness-review.md)
bound it to one bounded host-driven call that retires nothing.

CBI62 puts that governed cycle under CBI48's durable journal, and its first result is **a defect
CBI61 introduced that neither slice's tests could see**. CBI61 added two cycle codes; CBI48 validates
a committed code against the four it knew. A governed cadence reporting
`provider-trust-cycle-authority-behind` was refused as `durable-cadence-result-invalid` and left
in-flight, so **a run that completed normally was recorded as an interruption that never happened**,
and the host's next start would demand a reconciliation decision about nothing. CBI61's suite never
composed with a journal and CBI48's never produced a governed code — the defect lived in the seam
between two slices rather than in either, which is a different blindness from the one Decision 10
describes. The repair is not the two missing strings: producers and the journal now draw from one
vocabulary, so a code cannot be returned by a cycle and refused by the journal, and the guard walks
the vocabulary rather than naming today's six.

**The item's premise was wrong, and saying why is the capability.** It expected the journal to record
which of the two loops a resumed cycle had run. A marker written after the rotation returns is not
atomic with the rotation's effect, so it opens a second indeterminate window instead of closing the
first; and the rotation's effect is *already* durably recorded, by the retained chain and the stored
floor, so a marker could only ever be a less trustworthy copy of a record that exists. The absent
field is the contract, as it was for CBI17's synchronous succession. Its test is two runs identical in
every journal-visible respect and differing only in whether the rotation reached its endpoint: the
journals must be byte-identical while the checkpoints differ, which a journal recording the loop would
fail and a harness whose arms did not really differ would also fail.

What makes a governed retry safe is then a claim about two dependencies rather than about this slice,
so it is probed: **CBI57 refuses a replayed rotation by generation and CBI37 refuses a replayed update
by sequence.** Both the honest path — the host's own cursor moved, so the endpoints report it current
— and the defensive path — a stale endpoint re-offers the identical statement and update — are
exercised, because only the second shows the refusal doing any work. The
[`CBI62 capability contract`](../../component-management/cbi62-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi62-contract-completeness-review.md)
bound it to one host-local journal under one writer, and record what the vocabulary guard does *not*
bind: CBI49's and CBI50's observation vocabularies remain separate lists a later slice could let drift
the same way.

CBI63 reconciles a governed interruption, and **the item asked for wider evidence when what it needed
was narrower**. "Name which of the two loops the host has verified" presumes the host should be
speaking about both. Two of the three things a governed cycle can do — the rotation and the policy
update — are recorded durably by the components that do them, so an assertion about either is a claim
the record already answers better. The evidence therefore carries exactly one verdict, the serving
one, and **there is no field a host could over-assert into**. That is the third consecutive slice
whose scheduling item had to be corrected rather than fulfilled, and the pattern is worth naming: an
item written from the shape of the previous slice tends to propose a symmetry the models do not have.

**The loop boundary turns out not to be the verifiability boundary.** CBI61 split a cycle into the
rotation loop and the CBI47 loop; the split that matters here runs through the middle of the second
one, because the poll's effect is durably recorded and the sweep's is not. Evidence organised by loop
would have had one field covering a derivable effect and an underivable one together, which is exactly
the over-assertion this slice removes.

What makes the derivation possible is **the same device CBI62 refused, sound here because of when it
is written**. CBI62 refused a marker written *after* the rotation returns, since such a write is not
atomic with the effect it describes. A cursor written *before* the cycle describes state that already
exists and rides in the write that already marks the attempt in-flight, so it opens no window — and a
named test asserts the transition sequence is unchanged, making "no extra write" checked rather than
claimed. A derived effect then **reports and never vetoes**, because CBI62 established that a retried
governed cycle cannot double-apply either half; without that result, refusing retry would have looked
like the cautious choice. An absent cursor is refused rather than derived against an invented zero,
which would read every effect as applied, and a cursor above the observed state is refused as the
rollback it is rather than as an absence of effect. The
[`CBI63 capability contract`](../../component-management/cbi63-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi63-contract-completeness-review.md)
bound it to what the local durable record states.

CBI64 puts CBI49's availability policy and CBI50's enforcement inside the cadence, and its first
result is that **the boundary CBI63 named was already closed and the real one was next to it**. CBI63
pointed at "a host that terminates providers when CBI49's grace expires"; CBI50 has done that since
2026-08-05, and the sentence was copied from CBI49's deliberate-limits section, which nobody revised
when the slice discharging it landed. That is the fifth stated limit in this programme describing how
something was called rather than a rule anything applied, and the second the programme wrote about
itself. The actual hole is one step over: **CBI49 and CBI50 existed and nothing that polls repeatedly
had ever called them.** CBI47's cycle mapped every non-current poll to `provider-trust-cycle-stopped`
and ended the run, so an outage left every provider serving with no deadline — neither of the two
answers CBI49 offers.

Its finding is a property only a repeated evaluator can exercise. CBI49 states that repeated
evaluation uses the original last-current instant and cannot extend the deadline; its own vectors
evaluate once each, so nothing had ever tested it, and a cadence is exactly the caller that can get it
wrong. **A cadence that took each cycle's own instant as the baseline would report existing service
forever**, and the failure is invisible in every single-cycle vector because the deadline simply never
arrives. A vector holds an outage open across five cycles to its deadline, and a deliberate defect was
watched turning it into an endless one.

Two decisions come from the contracts rather than from preference. **Every non-current poll reaches a
decision, not only the grace-eligible third** — routing only the eligible outcomes would leave CBI49's
other two answers unreachable from any cadence, which is the composition deciding availability where
nothing can see it. And **the cycle code still names why policy could not be established, with
availability recorded beside it**, so CBI61's `provider-trust-cycle-authority-behind` attribution
survives a cycle that stopped every member; that refusal is never grace-eligible, so collapsing the
two facts into one code would have cost it. Cancellation is put ahead of the evaluation for the
reason it is not an endpoint failure: CBI49 would classify a canceled poll as
`offline-service-stop-required`, so an ordinary shutdown request would otherwise become an
availability withdrawal. The
[`CBI64 capability contract`](../../component-management/cbi64-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi64-contract-completeness-review.md)
bound it to one bounded host-driven cadence.

CBI65 derives that baseline from what CBI48 already committed, and its first result is that **a
durable boundary needed no durable record**. The journal has held each cycle's instant and code since
CBI48, so the slice adds a classification to an existing vocabulary and no storage at all — the
opposite of the obvious reading, which is to retain the baseline beside the journal and is the design
CBI42 argues against for the policy floor: a second record of a fact the first already holds is a
thing that can disagree with it.

What the derivation needed was a question the vocabulary had never been asked. `Continues` says
whether a cadence may go on; nothing said whether a cycle *established current policy*. Answering that
inside the derivation would have reproduced CBI62's defect one consumer over — a code a cycle can
produce and a consumer cannot classify — so the answer sits beside `Continues`, and a shared fixture
section pins it for every code rather than for the ones today's vectors happen to exercise. One code
turns out to be genuinely unanswerable: **`provider-trust-cycle-stopped` is produced both for a poll
that was not current and for a current poll whose sweep failed**, so the record does not say which. It
is refused rather than guessed, and the refusal outranks any establishing cycle behind it, because a
baseline drawn from the observations before it would be confidently wrong about everything after.
CBI48 cannot place such a code in front of a later cycle — a non-continuing code makes the run
terminal in the same write — and that is a claim about a dependency, so it is probed rather than
asserted.

Two decisions come from CBI49 rather than from preference. **The baseline is a fact about the host,
not about the run**: a terminal journal is as good a source as an interrupted one, because CBI49
anchors the deadline in absolute time, so an old baseline is already expired and refusing it would
only make a host that shut down cleanly stop service at its first outage. And **no freshness guard is
added**, because a baseline later than the evaluating instant is already `offline-observation-invalid`
under CBI49. The composed effect is that a crash during an outage does not restart grace — and the
wrong answer is the plausible one, since seeding a resumed cadence with its own restart instant renews
the deadline on every restart, so a crash loop would serve indefinitely past a deadline that never
arrives. The
[`CBI65 capability contract`](../../component-management/cbi65-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi65-contract-completeness-review.md)
bound it to what one host's own journal states, and record that C1's central property is a
compile-time one: the derivation is handed a snapshot rather than a journal, so it has nothing to
write to.

CBI66 lets that policy's retry instant shorten a cadence gap, closing the limit CBI64 recorded, and
its first result is **a defect underneath the fact nobody had consumed**. CBI49 has issued a next
retry instant with every continuation it permits since it was written, capped at the deadline by its
own C1 property, and nothing ever read it. Reading CBI48 in order to read it found `CompleteGap`
validating the instant it is given and then recording the schedule interval regardless — inert while
every gap equalled the interval, and wrong the moment one did not, because the recorded gaps then
disagree with the recorded cycle instants in the same journal. It was pinned with a failing test
before it was fixed.

The cost of the limit was not cosmetic. A host asking for service to stop five minutes after the
endpoint goes away got the rest of its polling interval as well, because the cadence's next look
landed after the deadline it was meant to enforce. **The gap is now the earlier of the interval and
the retry instant, so a run lands on the deadline itself.**

**The bound is one-sided, and which side is the capability.** A retry instant may bring a cadence's
next look forward and may never push it back: the interval is the host's own schedule, and a policy
that only ever asks to be consulted *sooner* must not be able to slow it down. The vector that fails
when this is wrong is the one whose retry is longer than the interval, and without it a
gap-lengthening implementation passes everything else. What the slice will not claim is a deadline it
cannot meet: **a cadence cannot detect an outage before it looks**, so the first outage cycle still
falls on the ordinary interval, an interval longer than grace can pass the deadline before any outage
is seen at all, and a vector states that outcome rather than leaving C6 to read as a guarantee. The
durable change is a relaxation, so the migration is that there is none — a journal written before this
slice has gaps all equal to its interval, and C5 pins the direction, because a guard requiring gaps
strictly below the interval would invalidate every record already on disk and passes every other test.
The [`CBI66 capability contract`](../../component-management/cbi66-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi66-contract-completeness-review.md)
bound it to one bounded host-driven cadence and keep CBI41's poll backoff and CBI60's rotation backoff
where they are.

CBI67 takes the boundary CBI50 and CBI64 both name — durable recording of a stop — and its result is
that **the record is the means and not the capability**. Neither slice says what the absence costs, and
reading CBI51 shows it: `ProviderRestartPolicy.Evaluate` took a `ProviderRestartCause` **the caller
passed in**, and two of its four values are refusals, so the caller chose which refusal applied to it.

**Only one of the three wrong claims was unguarded, and checking is what showed which.** A withdrawn
publisher fails CBI51's own authorization check whatever cause is claimed; an unexpected exit is the
restartable case anyway. Operator retirement is neither — the publisher is still trusted, so every
other condition passes — so a caller saying `OfflineAvailability` about a provider someone
deliberately retired got a restart the policy would have denied. That one case is the whole of what
an attributable record buys, and saying so is more useful than implying it guards all four. The cause
is now issued rather than asserted: an opaque attribution with no public construction path, obtainable
only from the store.

**The ordering is CBI41's rule in its third instance.** A record is a statement about something that
happened, so it cannot precede the thing it describes. Written first and interrupted, it claims a stop
that did not occur and CBI52 launches a second provider for an occurrence still serving; written
after, an interruption leaves a stop with no record, which reads as an unexpected exit — restartable,
which is what an availability stop wanted, and refused anyway for a withdrawn publisher by a check
that does not depend on this record. **Absence therefore means the host did not stop it**, which is
the only reading every writer's behaviour supports, and an unexpected exit cannot be written down at
all, because absence is what it is.

What the slice will not claim is the retirement it cannot see: **an operator who kills a provider from
outside the host leaves no record and an exited process**, which is indistinguishable from an
unexpected exit. The
[`CBI67 capability contract`](../../component-management/cbi67-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi67-contract-completeness-review.md)
bound it to the stops the host performs, under one host-local single-writer store with CBI42's custody
limit unchanged.

CBI68 enforces the bound six slices have declared and none has checked, and its result is that
**crossing it destroyed data rather than producing an error**. CBI48 states that its journal is bound
to one process and one writer, and CBI62, CBI63, CBI64, CBI65 and CBI67 each repeat that cross-process
ownership remains separate; none says what happens when two holders share one journal. `Open` read the
record into memory and took no lock and no fence, so each holder wrote the whole record back and **a
holder whose copy was behind erased a cycle another had committed**, with nothing reporting it. A
failing test pinned it before the slice was designed: a reopened journal held zero cycles where one had
been committed.

The fix is CBI54's mechanism one component over — an epoch published in the record, so a holder the
record has moved past is refused instead of writing. What is worth carrying forward is that **the
obvious reading of ownership was wrong and existing tests are what said so**. Claiming the run at
`Open` is the first design anyone writes, and three CBI48 tests refuse it: its C3 opens a journal from
inside a running cycle purely to observe the in-flight phase and then expects the driving holder to
commit, and its C5 and C7 compare the durable bytes across a recovery and require them unchanged. A
slice that takes a run away from a host in order to look at it breaks the component it is protecting.
**Ownership is therefore claimed by writing**, and opening only observes.

That correction **removed the migration rather than easing it**: under claim-on-open a record written
before this slice needed an adoption rule, and under claim-on-write it needs none, because the holder
reads epoch 0 and its first write claims the run at 1. The guard also runs before each transition's
phase preconditions, since those are judged from state a superseded holder already knows to be stale —
a vector caught the alternative telling a fenced holder it had not started a cycle, naming a protocol
error it had not made instead of the run it had lost. An unreadable record keeps the outcome CBI48
already defines, so the slice adds one code and changes none. The
[`CBI68 capability contract`](../../component-management/cbi68-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi68-contract-completeness-review.md)
bound it to a fence rather than a lock: it makes a written-past holder harmless and does not stop a
second host from opening a run the first is driving, which is CBI54's live file lock and the remaining
supervision boundary.

CBI69 supplies that lock, and reading the boundary in order to close it found **two things the limit
did not say, both about what leaving it open costs**. A fence detects a lost run at the holder's next
write, and **a cadence writes after its cycle has run**, so a competitor that opens the journal
mid-cycle and reconciles the in-flight attempt takes the run *while the effects are still happening* —
the cycle runs, the commit is refused, and the record keeps nothing of it. And CBI68's residual limits
describe two interleaving holders as fencing each other alternately; **they do not**, because a refused
transition leaves the refused holder's epoch unchanged, so the loser is out permanently and only a
host that decides to reopen rejoins. Alternation is at least visible from both sides; a silent
permanent transfer is not. Both are pinned in each stack, and the same competitor scenario is run
twice — once unsupervised, where the cycle is lost, and once under a lock, where it never reaches the
record — which makes the difference the fixture's answer rather than a comment. CBI68 enforces nothing
incorrectly and its text stands as written; this is the sixth stated limit in this programme that
described how something was called.

Two decisions come from the components rather than from preference. **Supervision claims nothing**:
acquiring reads and writes no part of the record, so a run may be supervised before it is established
and CBI68's corrected rule that ownership is claimed by writing survives a lock arriving beside it.
And **no durable record is added**, which is the opposite of the obvious reading — CBI54 publishes an
epoch beside its lock because CBI53 has none, and copying that shape here would put a second
owner-record next to the one the journal already carries, the design CBI42 argues against for the
policy floor and CBI65 for the availability baseline. What the slice did need was the journal's own
resolved path, without which a supervision paired with a different journal would have been trusted.
The [`CBI69 capability contract`](../../component-management/cbi69-capability-contract.md) and
[`contract-completeness review`](../../component-management/cbi69-contract-completeness-review.md)
bound it to cooperating hosts over one shared filesystem: a host that opens the journal without
acquiring is caught by the fence at its next write rather than excluded, nothing expires or is
renewed, and a lock over a path an adversary can write is no stronger than CBI42's custody limit.

PB8's independent reviews and owner closure are complete. Decision 13 was recorded on 2026-08-11:
the current 0.1 implementation continues to refuse every CM3 group that declares a bounded lifecycle
protocol. The owner then selected an explicitly migrated, full Channel 0.2 redesign rather than a
minimum binding-only correction; its mandatory design batch is Priority 1 above. Decisions 12 and 14
through 16 await rulings and block no current work.

## Other planned areas

| Area | Planning source | Current implementation state |
| --- | --- | --- |
| Architecture 0.8 | [current implemented copy](../current/architecture/Brontide-Architecture-0.8.md) and [pinned pre-implementation snapshot](./architecture/Brontide-Architecture-0.8.md) | Complete Draft implementation evidence available; not ratified. |
| Channel | [`Channel 0.2 redesign package`](./channel/README.md), retained [`Channel 0.1 Design Note`](./channel/Brontide-Design-Note-Channel-0.1.md), [`Draft Channel Contract 0.1`](./channel/Brontide-Draft-Channel-Contract-0.1.md), and [requirements ledger](./channel/architecture-0.8-channel-requirements-and-risk-ledger.md) | Channel 0.1 has complete experimental realization evidence; the 0.2 first-batch design package is complete with four resolved owner rulings and five retained independent reviews, and awaits a fresh independent closure re-review before implementation. |
| Component Management | [design note](./component-management/Brontide-Design-Note-Component-Management-0.1.md) and [`implementation plan`](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md) | CM0-CM6 are implemented independently in both stacks; the complete fake programme is retained here because of transitive evidence pins. Real distribution and production integration remain future work. |
| Composition | [`Composition Design Note`](./composition/Brontide-Design-Note-Composition-0.1.md) and [Composition Without a Kernel](./architecture/Brontide-Architecture-Composition-Without-a-Kernel.md) | Experimental composition evidence exists; the proposed architecture is not ratified. |
| Enrichment | [`Enrichment Design Note`](./enrichment/Brontide-Design-Note-Enrichment-0.1.md) | Targeted experimental evidence exists; the wider design remains work in progress. |
| Persistent Information | [`Persistent Information Design Note`](./persistent-information/Brontide-Design-Note-Persistent-Information-0.1.md) | R4/M4 experimental Opaque Corpus, Dataset, Store-role, declared-concurrency, and Router-guarantee evidence exists independently in both stacks; durable media and the wider design remain planned. |
| Topology and Guardians | [`Topology Design Note`](./topology/Brontide-Design-Note-Topology-0.1.md) | Recorded design direction; not ratified. |
| Reference 0.3 plan | [`Reference implementation plan`](../../Reference/docs/future/Brontide-Reference-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |
| Minimal 0.3 plan | [`Minimal implementation plan`](../../Minimal/docs/future/Brontide-Minimal-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |

Planned documents must state what is already implemented separately from what remains. When a plan
is completed, move it to `docs/archive/<area>/` and move lasting operational guidance or evidence to
`docs/current/` or the owning implementation.
