# Future work

This directory is the authoritative entry point for planned, draft, proposed, work-in-progress, or
otherwise unimplemented work. A document belongs here even when it is the “current architecture” if
the implementations have not delivered it.

## Priority 1 — Portable Component Binding

[Portable Component Binding Implementation Plan 0.1](./binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
is the next implementation goal. It turns retained Cooling and Catalog experiments into a reusable
Binding Plan and Channel realization. Its first stage, PB0, inventories the existing Cooling and
Catalog behaviour, maps it to the C1-C10 capability contract and the Channel 0.1 vectors, and
creates the data-only neutral contract under [`binding/portable/`](../../binding/portable/README.md).

**PB0 through PB7 are complete.** The PB0 scaffold, C1-C10 baseline inventory, representation
choice, and both resolved owner decisions are recorded there; PB1 authored the neutral contract
itself, as eight data-only schemas, 63 vectors covering C1-C10 and all 24 Channel 0.1 vectors, and
deterministic golden CBOR encodings — the later additions are Decision 5's eight Catalog vectors and
PB7's ninth schema with its eleven, taking the total to 82; PB2 implemented that contract natively in the Reference
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

Three questions remain open, and none is one of the eight: whether to ratify the provisional Channel
Shape and category names or publish an explicitly migrated revision, Decision 12, raised by CBI20 on
2026-08-01, and Decision 13, raised by CBI21 the same day — the only one of the three that blocks
implementation work, since it holds every CM3 group declaring a bounded lifecycle protocol.

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

**PB8 is partly complete.** Its evidence and documentation work is done: the contract matrix now
carries an executed-evidence table naming which realizations have run each capability; the Channel
ledger records CH-R11 as executed by a conforming realisation rather than awaiting stack harnesses;
the public boundary document gains a portable-seam section; both stacks' changelogs record the added
experimental surface; and the source-cost inventory has been re-measured, separated into retained and
portable layers, and extended with the representation, framing, allocation, copy, and payload-bound
facts for both realizations, each stating how it is known. The complete repository gate is green.

Two PB8 steps remain, and neither is the implementer's to close: **fresh independent reviews** of
Reference, Minimal, and the neutral contract, which require a reviewer identity distinct from every
implementation actor in a fresh context; and **question closure**, of which Decision 11 was recorded
on 2026-07-30, leaving the Channel naming question. Neither was closed by an implementer writing a
provisional choice down as a decision.

The former Priority 0 documentation relocation is complete; its archived plan is the
[Pinned Documentation Relocation Plan 0.1](../archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md).
No documentation prerequisite now precedes planned implementation work.

## Priority 2 — Component Management

[Component Management Implementation Plan 0.1](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md)
is the next implementable programme while Portable Binding awaits reviewer and owner actions. CM0
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
seam would need is **Decision 13**, raised 2026-08-01 and open: leave the stage out of scope, or split
readiness from establishment and add a declared-protocol verb, which is a version boundary's work
rather than a slice's. CBI12's plan refusal also stops reporting one code for four conditions. The
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

**Admitting a mediator's authority is the next implementable item.** CBI25 binds the mediator and stops
before CM5, and CM2's Mediation declaration says exactly what makes the question hard: a Mediation may
own authority, recovery, lifecycle, residue, backpressure, or mutable membership, and CM5 admits narrow
tuples against an occurrence. It has to decide whether a mediator is admitted for its own interaction
only, or as a deputy for the members behind it — and CBI3's rule that a grant names a holder suggests
the first, which would mean a Mediation owning authority cannot be represented at all. Wider Provider
Sets and real distribution remain future work behind it. PB8's independent reviews remain a separate
governance prerequisite rather than implementation work; Decision 11 was ruled on and delivered on
2026-07-30, and Decisions 12, 13, and 14 await rulings.

## Other planned areas

| Area | Planning source | Current implementation state |
| --- | --- | --- |
| Architecture 0.8 | [`Brontide-Architecture-0.8.md`](./architecture/Brontide-Architecture-0.8.md) | Complete draft; implementation evidence pending; not ratified. |
| Channel | [`Channel Design Note`](./channel/Brontide-Design-Note-Channel-0.1.md), [`Draft Channel Contract`](./channel/Brontide-Draft-Channel-Contract-0.1.md), and [requirements ledger](./channel/architecture-0.8-channel-requirements-and-risk-ledger.md) | Cooling/Catalog evidence exists; reusable Channel realization remains planned. |
| Component Management | [design note](./component-management/Brontide-Design-Note-Component-Management-0.1.md) and [`implementation plan`](./component-management/Brontide-Component-Management-Implementation-Plan-0.1.md) | CM0-CM6 are implemented independently in both stacks; the complete fake programme is retained here because of transitive evidence pins. Real distribution and production integration remain future work. |
| Composition | [`Composition Design Note`](./composition/Brontide-Design-Note-Composition-0.1.md) and [Composition Without a Kernel](./architecture/Brontide-Architecture-Composition-Without-a-Kernel.md) | Experimental composition evidence exists; the proposed architecture is not ratified. |
| Enrichment | [`Enrichment Design Note`](./enrichment/Brontide-Design-Note-Enrichment-0.1.md) | Targeted experimental evidence exists; the wider design remains work in progress. |
| Persistent Information | [`Persistent Information Design Note`](./persistent-information/Brontide-Design-Note-Persistent-Information-0.1.md) | Design direction only. |
| Topology and Guardians | [`Topology Design Note`](./topology/Brontide-Design-Note-Topology-0.1.md) | Recorded design direction; not ratified. |
| Reference 0.3 plan | [`Reference implementation plan`](../../Reference/docs/future/Brontide-Reference-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |
| Minimal 0.3 plan | [`Minimal implementation plan`](../../Minimal/docs/future/Brontide-Minimal-Stack-Implementation-Plan-0.3.md) | Planned work with retained delivery evidence. |

Planned documents must state what is already implemented separately from what remains. When a plan
is completed, move it to `docs/archive/<area>/` and move lasting operational guidance or evidence to
`docs/current/` or the owning implementation.
