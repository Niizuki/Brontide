# Component-management shared fixtures

This tree carries the neutral, data-only fixtures for the experimental fake Component Manager
planned by
[Brontide Component Management Implementation Plan 0.1](../docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md).
It may contain data and documentation only — never a shared runtime library, semantic
implementation logic, or code either stack executes. Each stack parses these files into its own
native types and computes its own observations.

Nothing in this tree is an Architecture 0.8 conformance claim. Sources, artifacts, digests,
evidence, and trust verdicts are deterministic test data for a fake manager; they prove nothing
about real distribution, packaging, or security.

CM1's observable behaviour is defined by the
[discovery, acquisition, and evidence capability contract](./cm1-capability-contract.md). That
contract stops at immutable staging and keeps selection, resolution, preparation, activation, Actor
establishment, and authority outside the phase.
Its completed phase-boundary absence audit is the
[CM1 contract-completeness review](./cm1-contract-completeness-review.md).

CM2's observable behavior is defined by the
[recursive generational resolution capability contract](./cm2-capability-contract.md). CM2 closes
finite acyclic structure into an inspectable Proposed Stack and immutable generation while retaining
occupied bindings, alternatives, policy exclusions, Provider Sets, occurrence identity, Mediation,
Ports, topology decisions, Parameters, requested authority, and decision provenance. It produces no
preparation, activation, Actor, authority, or active-generation effects. Its completed phase-boundary
absence audit is the
[CM2 contract-completeness review](./cm2-contract-completeness-review.md).

CM3's observable behavior is defined by the
[cyclic activation-group capability contract](./cm3-capability-contract.md). CM3 partitions a
complete fake activation graph into deterministic maximal strongly connected groups, validates
contract/version compatibility, bounded lifecycle protocols, Ready reachability, and declared
Region crossings, and emits an effect-free closed-gate stage plan. It does not prepare or establish
Components, execute lifecycle Operations, report runtime Ready, Release ordinary interaction, or
mutate an active generation. Its completed phase-boundary absence audit is the
[CM3 contract-completeness review](./cm3-contract-completeness-review.md).

CM4's observable behavior is defined by the
[preparation, activation, scoped-restart, and rollback capability contract](./cm4-capability-contract.md).
CM4 consumes a successful CM3 plan and explicit fake Host observations, then models optional
effect-free preparation, named establishment stages, exact lifecycle and ordinary gates, one
logical Release, post-Release binding exercises, scoped replacement, child-Port attachment, and
explicit recovery or degradation. It does not discover, resolve, load arbitrary code, or decide
authority policy. Its completed phase-boundary absence audit is the
[CM4 contract-completeness review](./cm4-contract-completeness-review.md).

CM5's observable behavior is defined by the
[authority-admission capability contract](./cm5-capability-contract.md). CM5 independently
evaluates attributable fake evidence, requested Actor relationships, and exact narrow authority
tuples under receiving-domain policy, then records local Actor mappings, Capability grants,
revocation or expiry refusals, unlimited-authority denial, and attributable policy mistakes. It is
not cryptographic, federated, or production security evidence. Its completed phase-boundary absence
audit is the
[CM5 contract-completeness review](./cm5-contract-completeness-review.md).

CM6 adds a versioned JSON Lines endpoint to each provider and runs every complete authority
scenario through both native CM5 implementations in both process directions. Comparison covers the
complete canonical CM5 profile rather than only its outcome; provider identity remains visible
outside the parity profile. Its observable behavior and claim boundary are defined by the
[independent-comparison capability contract](./cm6-capability-contract.md), and its completed
phase-boundary absence audit is the
[CM6 contract-completeness review](./cm6-contract-completeness-review.md). Equal profiles prove
agreement on the eight deterministic fake scenarios only, not real Component interchange,
contract completeness, general substitutability, or security.

CBI1 begins integration with Portable Component Binding at the two composition roots. Reference
Studio and Minimal Host independently accept one completed native CM2 direct `1..1` position plus
an explicit typed identity mapping and prepare a PB7 composition member without selecting,
negotiating, or starting a provider. Wider, mediated, absent, indirect, mismatched, or invalidly
addressed positions fail before a portable member exists. Its behavior and limits are recorded in
the [CBI1 capability contract](./cbi1-capability-contract.md) and completed
[contract-completeness review](./cbi1-contract-completeness-review.md).

CBI2 coordinates that prepared member with a singleton, protocol-free CM4 activation plan.
Reference Studio and Minimal Host independently derive CM4 stage observations from the actual PB7
member, validate the pure CM4 lifecycle before provider contact, project portable establishment
refusal into CM4, and release the portable ordinary-interaction gate only after CM4 reaches Active.
Caller-supplied stage claims are ignored. Its behavior and limits are recorded in the
[CBI2 capability contract](./cbi2-capability-contract.md) and completed
[contract-completeness review](./cbi2-contract-completeness-review.md).

CBI3 gates that lifecycle with one receiving-domain CM5 admission. Each composition root requires
an explicit occurrence-to-Actor mapping, one `ComponentParticipant` relationship, and one exact
narrow authority request; denial stops before provider contact, while exactly one attributable
relationship and grant permit CBI2 to continue. The grant remains a local CM5 observation and no
Capability crosses the portable trust boundary. Its behavior and limits are recorded in the
[CBI3 capability contract](./cbi3-capability-contract.md) and completed
[contract-completeness review](./cbi3-contract-completeness-review.md).

CBI4 projects five equivalent native CBI3 executions into a canonical integrated profile in each
composition root. The profile covers the complete CM5 observation by digest, the CBI3 decision,
every CM4 effect and stable failure, portable lifecycle state, and every stable resolution and
Binding Plan fact except the locally generated `planId`. Shared expected digests force both stacks
to answer the same questions without sharing runtime code. Its bounded claim is recorded in the
[CBI4 capability contract](./cbi4-capability-contract.md) and completed
[contract-completeness review](./cbi4-contract-completeness-review.md).

CBI5 revalidates the exact CM5 relationship and grant that admitted an active CBI3 binding using a
fresh explicit request. Exact renewal keeps the member released; revocation, expiry, request
mismatch, or any non-identical local admission retires it before another ordinary Operation can
reach the provider. Cleanup failure remains visible while the local gate stays closed. Its bounded
behavior is recorded in the [CBI5 capability contract](./cbi5-capability-contract.md) and completed
[contract-completeness review](./cbi5-contract-completeness-review.md).

CBI6 widens the authority gate from one participant holding one grant to a set of participants each
holding one or more exact narrow grants, over the same singleton binding. Because a CM5 request
names exactly one participant, the cross-request rules belong to the composition root: identities
stay distinct across the whole set, two participants may not share one receiving-domain Actor, every
participant is evaluated, and a set that is not admitted exactly grants nothing and reaches no
provider. Its bounded behavior is recorded in the
[CBI6 capability contract](./cbi6-capability-contract.md) and completed
[contract-completeness review](./cbi6-contract-completeness-review.md).

CBI7 revalidates that whole set after activation and answers the question CBI6 left undecided: when
one participant of several loses authority, the shared member is retired rather than the set being
narrowed, because nothing in an admitted set says which participants the member's ordinary
interaction depends on. Continuation requires the identical participant set to renew identically;
membership change or identity drift retires the member before any request is evaluated, and
retirement closes the ordinary-interaction gate before peer cleanup. Its bounded behavior is
recorded in the [CBI7 capability contract](./cbi7-capability-contract.md) and completed
[contract-completeness review](./cbi7-contract-completeness-review.md).

CBI8 changes an admitted set while its member stays released, and only by growing it. Removal and
substitution in place are refused for the same reason CBI7 refuses narrowing, so participant
precedence never has to be decided; they go through CBI7 retirement and a fresh CBI6 admission. A
declined extension leaves the binding exactly as it was, while an evaluated lapse in a retained
participant retires it — a malformed request decides nothing, evaluated loss decides everything. Its
bounded behavior is recorded in the [CBI8 capability contract](./cbi8-capability-contract.md) and
completed [contract-completeness review](./cbi8-contract-completeness-review.md).

CBI9 supplies what CBI7 and CBI8 both stopped at — a statement of which grants the member's ordinary
interaction depends on — and then removes and substitutes participants of a live set. The declared
names come from CM2's record of the selected definition's requested authority, so the Component says
what it depends on and the caller only maps each name to the CM5 tuple that satisfies it. A revision
is admitted while every declared dependency stays covered, which lets a substitute with a different
holder satisfy a dependency the departing participant used to satisfy, and which means participant
precedence never has to be decided. An empty declaration licenses nothing. Its bounded behavior is
recorded in the [CBI9 capability contract](./cbi9-capability-contract.md) and completed
[contract-completeness review](./cbi9-contract-completeness-review.md).

CBI10 checks that declaration against what the member actually did. Each observed portable
interaction is projected into one CM4 binding exercise whose authority admission is derived from the
declaration and the grants in force, never claimed by the caller, so CM4's own rule — delivery
cannot succeed when the external authority check denied it — is what condemns interaction outside
the declaration. An interaction that emitted no frame exercised nothing; an interaction that cannot
be attributed to declared authority is undeclared use; and either violation retires the member.
Declared authority nothing exercised, and declared authority nothing covers, are reported rather
than condemned. Its bounded behavior is recorded in the
[CBI10 capability contract](./cbi10-capability-contract.md) and completed
[contract-completeness review](./cbi10-contract-completeness-review.md).

CBI11 answers what CBI10 could not: nothing retires an unexercised declaration except the Component
saying so. A declaration narrows only to a successor resolution of the same position that declares
strictly fewer authorities, each retained one keeping its exact tuple, and observed use vetoes its
own removal. Narrowing permits rather than performs — a later CBI9 revision is what releases the
participant the narrowed declaration no longer needs — and this slice has no retirement path at all.
Its bounded behavior is recorded in the
[CBI11 capability contract](./cbi11-capability-contract.md) and completed
[contract-completeness review](./cbi11-contract-completeness-review.md).

CBI12 relaxes the one constant every earlier slice held fixed and activates several members
together. **The release barrier is the activation, not the member**: CM4 models one logical Release
for an attempt, so ordinary interaction opens for every member at once or for none, and a member
that reached Ready while another failed is retired rather than released. Cyclic groups are refused,
because a multi-member group is a strongly connected component and that is what Relational
Initialisation exists for. Its bounded behavior is recorded in the
[CBI12 capability contract](./cbi12-capability-contract.md) and completed
[contract-completeness review](./cbi12-contract-completeness-review.md).

CBI13 closes the gap CBI12 opened, where the lifecycle spanned several members while authority still
governed one. **Authority is admitted per member**, because CM5 admits against an occurrence and an
occurrence is durable where an activation attempt is not. **The authority barrier and the release
barrier are two barriers**, and the authority one is strictly earlier: every member's set is admitted
before any provider is contacted, and Release still waits for every member to reach Ready. Across the
activation the receiving-domain Actor mapping must be a function and injective, so one party may
participate in two members but two parties may not arrive at one local Actor. Its bounded behavior is
recorded in the [CBI13 capability contract](./cbi13-capability-contract.md) and completed
[contract-completeness review](./cbi13-contract-completeness-review.md).

CBI14 lifts revalidation and withdrawal to the activation and answers what CBI13 left open: **when
one member's authority lapses, the whole activation retires.** The answer comes from CM4, as CBI12's
release barrier did — an activation has exactly one restart scope, every member is inside it, and
CM4 models no way to retire one member while its scope keeps running, because that is a scoped
replacement. Members being otherwise independent is about what they need from each other, not about
what scope they share. The result names which members lapsed and which participants within them, so
the cause stays distinguishable from the consequence. Its bounded behavior is recorded in the
[CBI14 capability contract](./cbi14-capability-contract.md) and completed
[contract-completeness review](./cbi14-contract-completeness-review.md).

CBI15 lifts CBI9's revision to the activation and answers what CBI14 left: **a change is decided per
member and checked against the activation.** Admission is about an occurrence, so changing one
member's set decides nothing about another's authority; but CBI13's identity and Actor-mapping rules
are activation-wide, so the result is checked across every member. **A declined change is local; a
discovered lapse is global** — the same call can produce either, and a lapse in a member that was not
being revised retires the whole activation. A wrongly named member set is declined here rather than
retiring as it does in CBI14, because a revision asks for something rather than asserting continuity.
Its bounded behavior is recorded in the [CBI15 capability contract](./cbi15-capability-contract.md)
and completed [contract-completeness review](./cbi15-contract-completeness-review.md).

CBI16 lifts CBI10's verification to the activation and answers what that lift raises: **one member's
undeclared use condemns the whole activation.** The answer is the runtime's rather than a preference,
as CBI12's release barrier was — a CBI12 activation is one CM4 request, so every member's exercises
are judged together and CM4 refuses the request on the first offending exercise rather than excusing
the members that behaved. It agrees with CBI14's separate reason that the activation shares a restart
scope. **Attribution stays per member**, because the declaration is per member, so the same Operation
in two members is two independent attributions while a repeat inside one member is still refused, and
no member's grants admit another member's use. A structural refusal evaluates nothing and changes
nothing. Its bounded behavior is recorded in the
[CBI16 capability contract](./cbi16-capability-contract.md) and completed
[contract-completeness review](./cbi16-contract-completeness-review.md).

CBI17 lifts CBI11's succession to the activation and answers the two questions that lift raises.
**A succession is one transaction**: the permission is a generation, and a CM2 generation is one
immutable object resolving every position at once, so narrowing the members it describes while
refusing the rest would leave the activation holding declarations from two generations. **A member
the successor does not resolve blocks every other member**, for the same reason — a generation that
does not describe the whole activation is not a successor of it. Lifting also separates something
CBI11 stated as one rule: *nothing to succeed* stays an activation-level refusal, while *this member
is untouched* becomes an ordinary per-member outcome, because a successor that narrows one Component
and leaves another alone is the common case. A veto anywhere refuses everything, and nothing here
retires a member or reaches a provider. Its bounded behavior is recorded in the
[CBI17 capability contract](./cbi17-capability-contract.md) and completed
[contract-completeness review](./cbi17-contract-completeness-review.md).

CBI18 lifts the last single-member slice, CBI8's declaration-free extension, and dissolves the
question the item recorded rather than deciding it. **An activation may hold declarations for some
members and none for others, because growth cannot observe them**: a declaration says whether a
departing participant may go, growth removes nobody, and coverage is monotone in the grants held, so
a member holding one is grown by the same rule as a member holding none. The entry point takes no
resolution and no declaration, and **the absent parameter is the contract**. Growth is checked
against the whole activation, as CBI15's revision is; the case only a second member can pose is that
**a party already participating in another member may be added to a second**, and must then arrive at
the local Actor it already holds — CBI13's mapping rule in its permitting direction. A lapse in any
retained participant still retires everything, and a declined extension still changes nothing. Its
bounded behavior is recorded in the [CBI18 capability contract](./cbi18-capability-contract.md) and
completed [contract-completeness review](./cbi18-contract-completeness-review.md).

CBI19 replaces the generation occupying one restart scope with a successor generation, and the first
thing it establishes is that three earlier slices deferred to something CM4 does not have. **Scoped
replacement swaps a whole generation, not a member**: CM4 targets one scope holding one retained
generation, and a successful Release makes the successor active there atomically. Nothing in CM4
retires one member while its scope keeps running, so CBI14, CBI15, and CBI18's "retire the whole
activation" was never a placeholder awaiting this slice - it was already correct. **Authority follows
the occurrence, not the activation attempt**, which is CBI13's own justification finally exercised: a
surviving occurrence must be re-admitted with the authority that admitted it, a new occurrence is
admitted afresh, and nothing is inherited either way. The release barrier re-arms for the whole
successor activation, and the retained members are retired only **after** cutover, because a
pre-cutover failure must leave them serving. Its bounded behavior is recorded in the
[CBI19 capability contract](./cbi19-capability-contract.md) and completed
[contract-completeness review](./cbi19-contract-completeness-review.md).

CBI20 lets the successor resolve a **different set of positions**, adding and dropping members across
the cutover, and pointing it at CBI19 found a defect there first: CBI19 claims one entry per successor
member and no position added or removed, and checked neither, so a caller could omit a position the
generation still resolves and cut a scope over to a generation whose plan covered fewer members than
CM2 resolved. Its vectors could not catch it, because each one derives the member list, the participant
sets, and the plan from one declaration. **The membership is the successor generation's statement, not
the caller's**, and both stacks now refuse an under-supplied, over-supplied, or changed one. The lift
itself needs no new authority rule, because CBI19 decided authority per occurrence: **a dropped
occurrence has nothing to follow it to**, so its grant is simply not re-established, and **an added
position joins only across a cutover**, because a CM2 generation is one immutable object and a CM4
attempt covers its whole plan. An emptied membership is refused as CBI14's withdrawal reached through
the wrong door. Its bounded behavior is recorded in the
[CBI20 capability contract](./cbi20-capability-contract.md) and completed
[contract-completeness review](./cbi20-contract-completeness-review.md).

CBI21 reaches the last stage neither the integration nor the seam has exercised, and its first result
is that **CBI12 refused two different things under one justification**. CBI12 declines a multi-member
group because "a multi-member group is a strongly connected component, which is what Relational
Initialisation exists for" — but CM3 groups by strongly connected component over *every* edge, so two
Components with mutual ordinary interaction are one cyclic group declaring no protocol, no relational
stage, and a stage plan CM4 activates. That group needs nothing the seam lacks, and CBI21 delivers it.
**What stays refused is refused by Portable Binding's own published contract**: the PB7 Composition
handoff lists Relational Initialisation in its `outOfScope` array, offers a composition one traffic
verb gated on Release, and reports Ready *during* Interconnection — so there is no verb for a declared
handshake and no window before the readiness CM4 requires it to precede. A protocol-bearing plan is
therefore refused by name while CM3 and CM4 both accept it, which locates the gap rather than
papering over it, and what the seam would need is recorded as Decision 13 rather than approximated
here. Its bounded behavior is recorded in the
[CBI21 capability contract](./cbi21-capability-contract.md) and completed
[contract-completeness review](./cbi21-contract-completeness-review.md).

CBI22 activates a Component position CM2 resolved inside a **child Port**, in its own restart scope,
attached to the scope and generation a released parent activation made active. Its first result is a
fail-open the programme had asserted was closed: a Provider Set carries the Region and Port CM2
resolved it into, **CBI1 read neither**, and a Port-contained position was therefore flattened into an
ordinary one and activated in whatever restart scope the caller named - no attachment, no parent
generation, and the restart boundary the Port exists to give silently dropped. The future-work index
said such a position was refused; it was not, and nothing tested it. Both activation paths now refuse
it and the child path is the way through. **What the attachment says is the generation's, not the
caller's**: every member must be contained in one Port, the attachment must name that Port, and the
lifecycle comes from the resolved envelope, so a caller cannot attach a Component to a Port CM2 did
not put it in. CM4 owns the rest - an occupied Port needs an explicit replacement lifecycle, a
host-assisted export must follow the child's internal Release - and CBI22 reports those
classifications rather than reforming them. The parent stays active, released, and serving throughout,
because a child activation is a second activation rather than a replacement of the first. Its bounded
behavior is recorded in the [CBI22 capability contract](./cbi22-capability-contract.md) and completed
[contract-completeness review](./cbi22-contract-completeness-review.md).

CBI23 nests those attachments - a child may itself be the parent of another - and then answers what a
chain forces. Nesting was already reachable and CBI22 said so accurately, so what this slice adds is
the claim, the vectors, and the ordering. **CM4 models no relationship between a parent and a child
after attachment**: it requires the parent scope active when the child attaches and preserves it
through the activation, and nothing records that a scope has children or stands a child down when its
parent goes. Every earlier slice could take the runtime's shape as the answer to an ordering question;
here there is no shape to take. The answer comes from what an attachment *is* - a Port of a generation,
which its occupant cannot outlive - so **a child is retired before the parent whose Port it occupies**,
and a withdrawal cascades deepest first with the relation derived from each activation's own CM4
observation. **Depth is not bounded**, because no model bounds it and a number this programme invented
is what CBI11 refuses for elapsed time. What the contract states rather than implies is the hole: the
root can only order what it is given, so a child the caller omits is invisible, and every outcome names
exactly the scopes it retired. Its bounded behavior is recorded in the
[CBI23 capability contract](./cbi23-capability-contract.md) and completed
[contract-completeness review](./cbi23-contract-completeness-review.md).

CBI24 replaces a generation when child activations are attached to Ports it offers, and its finding is
that **a replacement silently orphans them** — CM4's C2 property preserves the generation and activity
state of every *unrelated* scope, and a child scope is unrelated, so a cutover rewrites the target
scope and carries the child through untouched while the parent generation its attachment recorded
stops existing. Nothing ever looks again. There is also **no migration operation**: re-pointing an
attachment would need CM4 to hold the declaration as mutable state, and it holds it as an input to one
attempt, so a Port does not migrate — a child is stood down and stood up again. The cascade therefore
runs **before** the cutover, which is the opposite order from CBI19's retained members, and the
asymmetry is the point: a retained member is inside the transaction and CM4 requires a pre-cutover
failure to leave it serving, while an attachment is outside it in a scope CM4 will not touch. A failed
replacement leaves the parent serving and does not restore the children, because restoring one would
be a fresh activation this call did not make. What the root cannot do is notice an attachment it was
not given, which is now Decision 14. Its bounded behavior is recorded in the
[CBI24 capability contract](./cbi24-capability-contract.md) and completed
[contract-completeness review](./cbi24-contract-completeness-review.md).

## Format

Every fixture file is UTF-8 JSON with `schemaVersion` 1 and a discriminating `fixture` name.
Consumers fail closed: unknown schema versions, unknown top-level sections, duplicate identifiers
within one identity space, and unresolved references are rejected with a deterministic explanation.
The single deliberate exception is an artifact reference listed in
`expectations.missingArtifacts`, which models a package whose artifact cannot be retrieved.

Identifier values use lowercase ASCII letters, digits, `.`, and `-`. Each identity space (source,
publisher, package, definition, occurrence, actor, contract, binding scope, binding, artifact,
evidence, node, function, claim, observer, preference) is distinct: the same string in two spaces
is two unrelated identities, and native representations must keep them type-distinct.

### `cm0-catalog` sections

`contracts`, `publishers`, `sources`, `packages`, `advertisements`, `componentDefinitions`,
`bindingScopes`, `activatedOccurrences`, `occupiedBindings`, `preferences`, `artifacts`,
`evidence`, `storefront`, and `expectations`. Artifact digests are the real SHA-256 of the
`content` string's UTF-8 bytes, uppercase hex. The `storefront` entries are the source-neutral
presentation projection required by CM0: a future UI seam, not a UI.

### `cm1-source-evidence` sections

`availability` contains explicit source/evidence pairs. Each pair must name a source and evidence
item from `cm0-catalog`, and the source must advertise a package carrying the evidence subject's
artifact. Advertising a package does not implicitly supply every claim about it. The file is
separate so CM1 adds provenance without changing the retained CM0 catalog contract.

### `cm2-resolution-vectors` sections

`vectors` is the shared data-only inventory of CM2 capabilities and expected outcome categories.
Each native suite constructs equivalent stack-owned inputs and computes its own observations; the
fixture contains no resolver algorithm, ranking function, lifecycle host, or authority service.

### `cm3-activation-group-vectors` sections

`vectors` is the shared data-only inventory of CM3 group-analysis capabilities and expected
outcome categories. Each native suite constructs equivalent stack-owned activation graphs and
computes its own groups, stage plans, decisions, and structured failures; the fixture contains no
graph algorithm, activation host, lifecycle executor, or runtime effect.

### `cm4-activation-runtime-vectors` sections

`vectors` is the shared data-only inventory of CM4 fake-Host capabilities and expected outcome
categories. Each native suite constructs equivalent stack-owned runtime inputs and independently
computes stage, gate, Release, cutover, binding, scope, child-attachment, and rollback observations;
the fixture contains no activation algorithm, executable Component, authority policy, or rollback
mechanism.

### `cm5-authority-admission-vectors` sections

`vectors` is the shared data-only inventory of CM5 evidence, relationship, local-policy, and narrow
grant capabilities and expected outcome categories. Each native suite constructs its own typed
requests and independently computes every decision and effect; the fixture contains no verifier,
policy evaluator, Actor mapper, Capability implementation, or security mechanism.

### `cm6-authority-comparison-vectors` sections

`scenarios` contains complete versioned CM5 request, evidence, relationship, authority, policy,
evaluation-time, and expected-outcome data. Each stack strictly reconstructs its own typed model
and emits a canonical complete profile through a bounded JSON Lines endpoint. The fixture contains
no defaults, patch language, evaluator algorithm, native type names, or executable comparison
behavior.

### `cbi4-integrated-comparison-vectors` sections

`vectors` names five integration scenarios with expected Active state, stable CBI3 failure kind,
and SHA-256 of the exact canonical integrated profile. Each composition root constructs and
executes its own native CBI3 scenario. The fixture contains no serializer, lifecycle coordinator,
authority evaluator, portable runtime, or process protocol.

### `cbi5-authority-withdrawal-vectors` sections

`vectors` names exact renewal, revoked evidence, expired evidence, request mismatch, and retirement
cleanup failure with stable outcome codes. Each composition root reconstructs and evaluates its own
native CM5 request and PB7 member. The fixture contains no evaluator, clock, cleanup mechanism,
portable authority, or runtime implementation.

### `cbi6-participant-admission-vectors` sections

`vectors` names one admitted two-participant set and eight refusals — a denied second participant,
a repeated participant, a shared authority request identity, a repeated grant tuple, an unlimited
grant, an empty set, a shared local Actor, and a foreign occurrence — with stable failure kinds and
codes. Each vector also pins how many participants were evaluated and how many aggregate grants the
result carries, so both composition roots must answer the evaluation-count and partial-set questions
rather than agreeing by silence. The fixture contains no evaluator, policy, identity rule, lifecycle
coordinator, or portable runtime.

### `cbi7-participant-withdrawal-vectors` sections

`vectors` names exact set renewal, one participant revoked, every participant expired, tuple drift,
a dropped grant, a removed participant, an added participant, and retirement cleanup failure, with
stable outcome kinds and codes. Each vector also pins how many participants were evaluated and how
many did not renew, so the all-or-none evaluation rule and the attribution of a partial loss are
forced answers rather than silences. The fixture contains no evaluator, clock, cleanup mechanism,
portable authority, or runtime implementation.

### `cbi8-participant-extension-vectors` sections

`vectors` names one admitted extension and ten refusals — removal, substitution, an unchanged set,
an added identity collision, an added unlimited grant, retained identity drift, a denied addition, a
shared local Actor, a retained participant revoked, and retirement cleanup failure — with stable
outcome kinds and codes. Each vector also pins how many participants were evaluated, how large the
set still in force is, and whether the member is still released, so "nothing changed" is a checked
answer rather than an assumption. The fixture contains no evaluator, policy, identity rule, cleanup
mechanism, or portable runtime.

### `cbi9-dependency-revision-vectors` sections

`vectors` names two admitted revisions — dropping a participant nothing depends on, and substituting
the holder of a declared dependency — and eleven refusals covering an uncovered dependency, an
unchanged or empty set, a declaration that misnames, empties, or fails to match the set in force,
retained identity drift, a denied addition, a shared local Actor, a retained participant revoked,
and retirement cleanup failure. Each vector pins the outcome kind, code, evaluated count, in-force
set size and grant count, and whether the member is still released. The fixture contains no
evaluator, resolver, policy, coverage rule, or portable runtime.

### `cbi10-observed-interaction-vectors` sections

`vectors` names three consistent verifications — a declared interaction, no interaction at all, and
one denied before any frame — and six refusals covering undeclared authority, an unmapped Operation,
ungranted authority, retirement cleanup failure, a declaration mismatch, and an ambiguous
attribution. Each vector pins the verdict kind and code, the number of projected exercises, the
unexercised and uncovered declared authorities, whether the CM4 runtime accepted the projection,
whether the member is still released, and how many provider effects the interactions actually
caused. The fixture contains no evaluator, runtime, projection rule, or portable implementation.

### `cbi11-declaration-succession-vectors` sections

`vectors` names one applied narrowing and eight refusals — observed use vetoing its own removal, an
unchanged or wider successor, a re-pointed tuple, a successor position that is not the live one, a
successor declaring nothing, a successor mapping that does not match its generation, and an
ambiguous attribution. Each vector pins the outcome kind and code, the dropped and vetoed
authorities, the size of the declaration still in force, and that the member is still released, so
"this slice never retires" is a checked answer. The fixture contains no resolver, declaration rule,
observation, or portable runtime.

### `cbi12-group-activation-vectors` sections

`vectors` names one two-member activation and five refusals — a second member whose provider is
substituted, a preparation that cannot resolve, a plan carrying an unselected member, a genuinely
cyclic group with Relational Initialisation protocols, and a CM4 refusal before establishment. Each
vector pins the failure kind and code, the number of members, how many are released, how many are
retired, and the runtime's verdict, so the release barrier is a checked answer in both directions.
The fixture contains no planner, runtime, portable implementation, or cleanup mechanism.

### `cbi13-group-authority-vectors` sections

`vectors` names two admitted activations — two members with their own parties, and one party
participating in both — and five refusals covering a denied member, an authority identity shared
across members, one participant mapped onto two local Actors, two participants mapped onto one, and
an activation refused after every member was admitted. Each vector pins how many members were
admitted, how many aggregate grants exist, how many members were released, and how many provider
effects occurred, so the ordering of the two barriers is a checked answer. The fixture contains no
evaluator, policy, planner, or portable runtime.

### `cbi14-group-withdrawal-vectors` sections

`vectors` names exact renewal of a whole activation and five withdrawals — one member lapsed, both
lapsed, a changed member set, participant identity drift, and retirement cleanup failure. Each pins
how many members were evaluated, how many lapsed, how many stayed released, and how many replacement
records exist, so shared fate and the cause-versus-consequence distinction are both checked answers.
The fixture contains no evaluator, clock, cleanup mechanism, or portable runtime.

### `cbi15-group-revision-vectors` sections

`vectors` names one applied revision and seven refusals — a lapse in the member that was not being
revised, a wrongly named member set, a revision that changes nothing, an authority identity shared
across members, a local Actor shared across members, an uncovered declaration, and retained identity
drift. Each pins how many participants were evaluated, how many the in-force activation holds, and
how many members stayed released, so decline-versus-retire and shared fate are both checked answers.
The fixture contains no evaluator, resolver, declaration rule, or portable runtime.

### `cbi16-group-verification-vectors` sections

`vectors` names four consistent verifications — one member interacting while its sibling stays
quiet, both members attributing the same Operation to their own declared authority, no interaction
at all, and one denied before any frame — and seven refusals covering one member's undeclared use,
one member's ungranted use, both at once, retirement cleanup failure, a member set the activation did
not admit, an Operation repeated inside one member, and a declaration the generation does not record.
Each pins the projected exercises, the violating members, the unexercised and uncovered declared
authorities, the runtime's verdict, how many members stayed released, and how many provider effects
the interactions caused, so per-member attribution and whole-activation condemnation are both checked
answers. The fixture contains no evaluator, runtime, projection rule, or portable implementation.

### `cbi17-group-succession-vectors` sections

`vectors` names two applied successions — both members narrowing, and one narrowing while the other
restates its declaration unchanged — and eight refusals covering a veto raised in the member that was
not narrowed first, an activation that narrows nothing, a member that would widen, a re-pointed
tuple, a member the successor does not resolve, a successor declaring nothing for one member, a
member set the activation did not admit, and an Operation repeated inside one member. Each pins the
dropped and vetoed authorities, how many members narrowed, the declared authorities in force
afterwards, and how many members stayed released, so the one-transaction scope and the
unchanged-versus-refused distinction are both checked answers. The fixture contains no resolver,
declaration rule, observation, or portable runtime.

### `cbi18-group-extension-vectors` sections

`vectors` names three applied extensions — one member growing while the other restates its set, both
growing, and a party already live in one member being added to a second — and eleven refusals
covering that same party mapped onto a second local Actor, removal, substitution, an activation that
gains nobody, a member set the activation did not admit, an authority identity shared across members,
a local Actor shared across members, a denied addition, retained identity drift, a lapse in the member
that was not growing, and retirement cleanup failure. Each pins how many participants were evaluated,
how many members grew, how many the in-force activation holds, how many members lapsed, and how many
stayed released, so growth-only, the shared-party answer in both directions, and decline-versus-retire
are all checked answers. The fixture contains no evaluator, policy, identity rule, or portable
runtime.

### `cbi19-scoped-replacement-vectors` sections

`vectors` names two cutovers - a clean replacement and one whose retained cleanup fails afterwards -
and seven refusals covering a moved restart scope, a generation that succeeds nothing, a retained
generation the scope does not hold, a surviving occurrence re-admitted for different authority, a
denied successor admission, a successor member that never reports Ready, and a Release that fails
before cutover. Each pins whether the scope cut over, how many successor members are released, how
many retained members are still released and how many are retired, and how many members the successor
admitted, so the cutover boundary is a checked answer in both directions. The fixture contains no
runtime, evaluator, planner, or portable implementation.

### `cbi20-membership-replacement-vectors` sections

`vectors` names six cutovers — a position added, one dropped, both at once, an unchanged membership, an
added party taking the receiving-domain Actor a dropped one held, and a dropped member whose cleanup
fails afterwards — and eight refusals covering a resolved position the caller did not supply, a member
the generation does not resolve, a successor resolving nothing, a surviving occurrence re-admitted for
different authority, a denied addition, an added party taking a *surviving* participant's local Actor,
an added member that never reports Ready, and a Release that fails before cutover. Each pins whether
the scope cut over, how many successor members are released, how many retained members are still
released and how many are retired, how many members the successor admitted, and how many positions were
added and dropped, so the derived membership sets and the cutover-only rule for an addition are both
checked answers. The cutover vectors keep CBI19's own outcome codes, because CBI20 delegates the cutover
rather than restating it. The fixture contains no runtime, evaluator, resolver, or portable
implementation.

### `cbi21-strongly-connected-group-vectors` sections

`vectors` names two activations - a cyclic pair that declares no protocol, and one plan carrying a
singleton group beside a cyclic pair - and four refusals covering a group declaring bounded lifecycle
protocols, a selected occurrence the plan does not carry, a planned member the activation did not
select, and a repeated selection. Each pins whether the activation reached Active, the refusal code,
how many groups and members the plan carries, and how many members were prepared and released, so
"which condition refused this plan" is a checked answer rather than one code standing for four. The
fixture contains no planner, runtime, portable implementation, or lifecycle protocol.

### `cbi22-child-port-vectors` sections

`vectors` names two attachments - an ordinary one and a host-assisted one - and ten refusals covering
a parent that never released, a parent generation the scope does not hold, a child scope equal to the
parent's, an attachment naming another Port, a member resolved into no Port, a Port lifecycle the
caller overstates, an occupied Port without a replacement lifecycle, a host-assisted export that does
not follow the internal Release, a child member that never reports Ready, and a denied child
admission. Each pins the outcome kind and code, how many child and parent members are released, and
how many members the child admitted, so "the parent is untouched" is a checked answer in every
outcome. The fixture contains no runtime, planner, resolver, or portable implementation.

### `cbi23-nested-child-port-vectors` sections

`vectors` names one grandchild attachment and four refusals at the second level - an overstated Port
lifecycle, a scope equal to its parent's, a parent generation the scope does not hold, and an
attachment beneath a parent that has been retired - each pinning how deep the tree got and how many
members are released. `withdrawals` names two ordered cascades, one from the root and one from the
middle, a cascade whose middle level fails cleanup, and a set naming one scope twice; each pins the
exact retirement order, so deepest-first is a checked answer rather than an assumption. The fixture
contains no runtime, planner, resolver, or portable implementation.

### `cbi24-attached-replacement-vectors` sections

`vectors` names two replacements that stand attachments down first - one attachment and a two-level
forest - and five refusals covering an activation attached to something else, one that is not an
attachment at all, a replacement whose scope was never going to cut over, a replacement that fails
after the cascade has run, and a cascade whose cleanup fails. Each pins how many scopes the cascade
retired, how many successor members are released, and how many attachment members are still released,
so "nothing is established while an attachment is up" is a checked answer. The fixture contains no
runtime, planner, resolver, or portable implementation.

### `cm0-mice-topology` sections

`contracts`, `observers`, `topologyNodes`, `functions`, `claims`, and `expectations`. Relations
are the minimum floor vocabulary: `PartOf`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`,
`SharesPowerDomain`, `SharesFailureDomain`. Claims are attributable assertions; the fixture labels
which are expected to be treated as contradictory or malicious so both stacks surface them without
accepting physical grouping, identity, trust, or authority on assertion alone.

### `expectations`

The `expectations` block is the shared expected-observation record: every entry must equal what a
loader computes from the data sections. A mismatch is a fixture defect or a loader defect and must
fail loading. Expectations never grant authority and carry no semantics beyond identification.
