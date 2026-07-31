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
