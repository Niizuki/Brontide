# Brontide

New to the project? [Brontide: The Idea](./docs/current/overview/Brontide-Introduction.md) is the readable
introduction. The [documentation map](./docs/README.md) classifies the precise documents as current,
future, temporary, or archival:

- [`docs/current/`](./docs/current/README.md) — implemented behavior, current implementation
  targets, and operational policy;
- [`docs/future/`](./docs/future/README.md) — planned, draft, proposed, or otherwise unimplemented
  work;
- [`docs/temporary/`](./docs/temporary/README.md) — deletion-gated execution notes; and
- [`docs/archive/`](./docs/archive/README.md) — completed or superseded work.

Repository-wide design, plan, ledger, and correction documents now live under `docs/` and each
stack's `docs/future/`, classified by the documentation map. The completed
[Pinned Documentation Relocation Plan 0.1](./docs/archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md)
carried out that move under an authorized evidence-repinning window and repinned every dependent
evidence trail; the repository root now holds only standard project-control files, this `README.md`,
`AGENTS.md`, and `Brontide-Architecture-Status.json`.

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- [Brontide Reference Stack](./Reference/README.md), the C#/Avalonia implementation and interactive showcase;
- [Brontide Minimal Stack](./Minimal/README.md), the F# implementation and headless counterpoint.

[`Brontide-Architecture-Status.json`](./Brontide-Architecture-Status.json) identifies the current
architecture source and latest ratified architecture. Implementation targets are stated locally in
the document or stack README that owns the work; a central registry does not choose them. The
additional hashes and paths in the registry are retained for existing verification tooling, not as a
second implementation roadmap.

## Implementation targets

- [Brontide Reference Stack](./Reference/README.md) is designed for Architecture 0.7. Its README
  states what is implemented and which projects deliberately experiment against Architecture 0.8.
- [Brontide Minimal Stack](./Minimal/README.md) is designed for Architecture 0.7 under the same rule.
- A focused experiment or implementation note may target a different architecture revision by
  stating `Designed for: Brontide Architecture <version>` in that document.

A target records the architecture against which work was devised. It is not, by itself, a complete
conformance or ratification claim. Code, tests, and concise known-limitations prose remain the useful
evidence of what actually works.

The implementation correction programme is complete. Its work and validation are summarized in the
[completion report](./docs/archive/corrections/implementation-correction-completion-report.md), with permanent status in
the [implementation correction record](./docs/archive/corrections/implementation-correction-status.md) and
machine-checkable evidence in the [independent-review framework](./conformance/reviews/README.md).
The temporary plan was deleted only after two conforming reviews and explicit checked authorization.

The first programme of real cross-stack evidence remains
[Reference/Minimal Interchange Implementation Plan 0.1](./docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented: two-way Cooling component interchange and a
materially different, resource-scoped Catalog interchange both cross real process boundaries. The
Catalog proof adds nested/repeated values, two Operations in one provider session, explicit failure,
resource refusal, replay detection, strict message variants, version skew, and a 65,536-byte line
limit. They test Brontide substitutability without sharing private CLR types or treating either
experimental binding protocol as ratified architecture. Run the retained gate with
`.\build\verify-interchange.ps1`.

The first extension of the Architecture 0.8 evidence cycle, `Channel`, is extracted from those two
interchange proofs and recorded in
[Channel Design Note 0.1](./docs/future/channel/Brontide-Design-Note-Channel-0.1.md): the request/Outcome
representation, correlation, error propagation, and delivery semantics the invocation principle
needs and Base withholds. It fixes semantics rather than a wire format, keeps Capabilities from
crossing a trust boundary, and precedes the Portable Component Binding, which becomes its first
conforming realisation. It remains a recorded direction outside Base, not a ratified extension. Its
open questions and risks are tracked in
[`Channel requirements and risk ledger`](./docs/future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md).

With the Priority 0 documentation relocation carried out, the next implementation goal is the
[Portable Component Binding Implementation Plan 0.1](./docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md).
It turns the retained Cooling and Catalog experiments into a reusable, independently implemented
Binding Plan and Channel realization with direct/process parity, neutral vectors, bounded resource
semantics, and both cross-stack directions. The work remains experimental and does not enlarge Base
or change either stack's Architecture 0.7 target.

The provisional generational lifecycle, multiple-source acquisition model, trust separation,
scoped restart, and dependency-cycle policy are recorded in
[Component Management and Distribution Design Note 0.1](./docs/future/component-management/Brontide-Design-Note-Component-Management-0.1.md).
The corresponding
[Component Management Implementation Plan 0.1](./docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md)
records the completed CM0-CM6 independent fake managers in both stacks, including full CM5-profile
comparison across real processes in both host directions. This remains bounded experimental
evidence and does not imply a real online marketplace, production package manager, general
substitutability, security, or Architecture 0.8 conformance.
The first CBI1 integration slice now lets each stack's composition root carry a completed native
CM2 direct `1..1` decision into Portable Binding PB7 preflight through an explicit typed mapping,
without reselecting the provider or inventing a Binding Plan.
CBI2 then coordinates that member with one singleton, protocol-free CM4 plan: CM4 is validated
before provider contact, lifecycle observations come from PB7 state rather than caller claims, and
the portable gate opens only after CM4 reaches Active. CM5 authority remains outside this slice.
CBI3 adds the first CM5 integration: one explicit occurrence-to-Actor mapping and one exact narrow
receiving-domain grant must be admitted before provider contact. That local grant gates activation
but never crosses the portable trust boundary or authorizes a portable Operation by name.
CBI4 now compares five equivalent integrated executions through independently produced canonical
profiles. Equality covers the complete CM5 profile digest, CBI3 decision, CM4 effects, portable
lifecycle, and stable plan facts; it remains data-only evidence, not integrated cross-process
execution or general substitutability.
CBI5 revalidates the exact relationship and grant behind one active CBI3 binding. Revocation,
expiry, or identity drift retires the portable member and closes ordinary interaction before peer
cleanup; exact renewal leaves it released. This is bounded post-activation evidence, not
distributed revocation or cancellation of an in-flight Operation.
CBI6 widens admission from one participant holding one grant to a set of participants each holding
several. A CM5 request names one participant, so the questions only a set can raise are answered by
the composition root and answered fail closed: identities stay distinct across the whole set, two
participants may not share one receiving-domain Actor, and a set that is not admitted exactly grants
nothing and reaches no provider.
CBI7 revalidates that set after activation. When one participant of several loses authority the
shared member is retired rather than the set narrowed: nothing in an admitted set says which
participants its ordinary interaction depends on, so continuing would decide that invisibly. The
result names which participants did not renew, and retirement closes the local gate before peer
cleanup.
CBI8 grows an admitted set in place while the member stays released, and refuses to shrink one: a
substitute holding the identical tuples is still a different grant, so removal and substitution go
through retirement and fresh admission. That is also why participant precedence never has to be
decided. A declined extension changes nothing; an evaluated lapse in a retained participant retires
the member.
CBI9 supplies what the two before it lacked — a statement, taken from the resolved Component
definition rather than the caller, of which grants the member's interaction depends on — and then
removes and substitutes participants while every declared dependency stays covered. Because the
declaration names Capability, target Actor, Operation, and scope rather than holders, a substitute
can satisfy what a departing participant used to satisfy, and participant precedence never has to be
decided.
CBI10 then checks that declaration against what the member actually did. Each observed interaction
becomes a CM4 binding exercise whose authority admission is derived rather than claimed, so the
runtime's own rule condemns interaction outside the declaration; an interaction that emitted no
frame exercised nothing, and undeclared or ungranted use retires the member. It detects a
declaration contradicted by use, never one contradicted by disuse.
CBI11 answers what that leaves open: nothing retires an unexercised declaration except the Component
saying so. A declaration narrows only to a successor resolution of the same position that declares
less, and observed use vetoes its own removal, so no elapsed time or quiet period narrows anything.
CBI12 finally relaxes the single member. Several members activate under one CM4 activation, and the
release barrier is the activation rather than the member: ordinary interaction opens for all of them
or for none, and a member that reached Ready while another failed is retired rather than left
holding a channel. Cyclic groups are refused, because that is what Relational Initialisation is for.
CBI13 gives that activation its authority. Each member's participant set is admitted separately,
against the occurrence rather than the attempt, and every set is admitted before any provider is
contacted — so the authority barrier turns out to be earlier than the release barrier rather than the
same one. One party may participate in two members, but two parties may not arrive at one
receiving-domain Actor.
CBI14 revalidates that authority afterwards, and answers what a lapse does to the rest: the whole
activation retires. CM4 gives an activation one restart scope and no way to retire one member while
it runs, so members that came up together go down together — their independence is about what they
need from each other, not about what scope they share.
CBI15 revises those sets: a change is decided per member and checked against the activation, and a
declined change is local while a discovered lapse is global.
CBI16 checks those declarations against what the members actually did, and answers what a violation
does to the rest: one member's undeclared use condemns the activation. The projection is one CM4
request, so CM4 refuses it on the first offending exercise rather than excusing the members that
behaved — the same place CBI12's release barrier came from.
CBI17 narrows those declarations to a successor generation, and it is one transaction for the same
kind of reason: a generation is one immutable object resolving every position at once, so a member
the successor does not resolve blocks the others. It also splits a rule CBI11 could only state as
one — restating what is in force still succeeds nothing, but a member the successor leaves alone is
simply untouched.
CBI18 finishes the lifting programme by growing those participant sets, and dissolves the question it
inherited instead of deciding it: growth needs no declaration from any member, because a declaration
governs who may leave and growth removes nobody. The case a single member could never pose is that a
party already participating in one member may join another — the mapping rule that usually refuses
things, permitting one.
CBI19 replaces the generation in a restart scope, and its first finding corrects the slices that
pointed at it: CM4 swaps a whole generation atomically and has no operation that retires one member
while its scope keeps running, so "retire the whole activation" was never a placeholder. Authority
turns out to follow the occurrence rather than the attempt — which is the reason CBI13 gave for
admitting against an occurrence in the first place, finally put to work.
CBI20 lets that successor resolve a different set of positions, and what it mostly found was a defect
in CBI19: it declared one entry per successor member and checked nothing, so a caller could quietly
drop a position the generation still resolved. The lift needed no new authority rule at all — a
dropped occurrence has nothing to follow it to — and the one question left, whether an addition can
join a running activation, is answered by the runtime rather than by preference: a generation is
immutable and an attempt covers its whole plan, so it joins across the cutover or not at all.
CBI21 reaches Relational Initialisation and finds CBI12 had refused two different things under one
sentence: CM3 groups by strongly connected component over every edge, so a cyclic group is not the
same as one needing a handshake, and the first activates today. What stays refused is refused by
Portable Binding's own published contract, which declares the stage out of scope, offers a composition
one traffic verb gated on Release, and reports Ready during Interconnection — so there is neither a
verb nor a window before the readiness CM4 requires a handshake to precede. The gap is located rather
than papered over, and what the seam would need is left as an owner decision.
CBI22 activates a Component CM2 resolved inside a child Port, in its own restart scope beneath a
released parent — and finds a fail-open the programme had asserted was closed: the Region and Port a
Provider Set carries were read by nothing, so such a Component was flattened into an ordinary one and
activated in whatever scope the caller named. Which Port an attachment names is the generation's
statement rather than the caller's, and the parent stays active and serving throughout, because a
child activation is a second activation rather than a replacement of the first.
CBI23 nests those attachments, and hits the first ordering question the runtime does not answer: CM4
records nothing about a parent's children and stands none of them down when the parent goes. The answer
comes from what an attachment is rather than from a model object — a Port belongs to a generation, so
its occupant cannot outlive it, and a withdrawal cascades deepest first. What the root cannot see, it
says: a child the caller does not name is invisible, and every outcome names exactly what it retired.
CBI24 replaces a generation that has children attached to its Ports, and finds that a replacement
orphans them silently — CM4 preserves every unrelated scope by design, and a child's scope is
unrelated, so the child keeps running against a generation that no longer exists. There is no
migration: a Port does not move, a child is stood down and stood up again. The cascade runs before the
cutover, the opposite of CBI19's retained members, because which side of the transaction a thing lives
on decides when it goes.

The broader topology direction is recorded in
[Topology Environments and the Guardian Family Design Note 0.1](./docs/future/topology/Brontide-Design-Note-Topology-0.1.md). Ordinary
Environments remain overlapping, security-neutral topology identities and have no Gatekeeper requirement.
A Guardian is an Actor entrusted to protect or represent a participant, resource, or bounded
interaction. Gatekeeper is its preventative Protected-Environment-boundary specialisation. Sentinel
is its bounded observational specialisation: the primary third-party observer and reporter within a
purpose-specific Sentinel Watch. The Watch makes subjects, occurrence classes, sources, coverage,
lifecycle, evaluator, outputs, and gaps explicit while granting no implicit response authority.
Protected Environments are disjoint or nested within one Protection Plane and opaque except through
Gatekeepers; one with no active Gatekeeper has no declared external communication. Every Gatekeeper export declares
its fidelity — Direct, Deputised, Mediated, Adapted, or Synthetic — so reinterpretation never
masquerades as exposure. These terms remain outside Base and are not a ratified extension.

Exact boundary assumptions are recorded in
[`docs/current/policies/public-boundaries.md`](./docs/current/policies/public-boundaries.md), and the reproducible manual/generated
source-cost inventory is [`interchange/binding-measurements.json`](./interchange/binding-measurements.json).
The completed correction programme is summarized in
[`docs/archive/corrections/implementation-correction-completion-report.md`](./docs/archive/corrections/implementation-correction-completion-report.md).
Its finding and deletion-gate evidence is retained in
[`docs/archive/corrections/implementation-correction-status.md`](./docs/archive/corrections/implementation-correction-status.md).
The retained conformance matrices and independent-review workflow remain available as detailed test
and correction evidence. They do not determine either stack's architecture target; the owning README
does that directly.
