# Changelog

## Unreleased — CBI14 multi-member revalidation and withdrawal

### Added

- Reference Studio revalidation of every member's authority in a multi-member activation from fresh explicit
  CM5 requests, evaluated all-or-none across the activation.
- Whole-activation retirement when any member's authority lapses, with the lapsed members and the
  participants within them named so the cause stays distinguishable from the consequence.
- Shared withdrawal vectors pinning evaluated members, lapsed members, released members, and
  replacement records, plus a phase-boundary completeness review.

CBI14 answers what CBI13 left open: a CM4 activation has exactly one restart scope and no way to
retire one member while it runs, so members that came up together go down together. CBI8 through
CBI11 still govern one member.

## Unreleased — CBI13 multi-member authority

### Added

- Reference Studio admission of a participant set per member of a multi-member activation, evaluated for
  every member before any provider is contacted.
- Activation-wide identity distinctness for admission, relationship, and authority requests, and a
  receiving-domain Actor mapping required to be a function and injective across the activation.
- Shared group-authority vectors pinning admitted members, aggregate grants, released members, and
  provider effects, plus a phase-boundary completeness review.

### Changed

- The effect-free half of CBI6 admission is now a separate step, so a multi-member activation can
  admit every member's set before any of them is established. CBI6's own behaviour is unchanged.

CBI13 answers both questions the plan raised: authority is admitted per member, against the
occurrence rather than the attempt, and the authority barrier is earlier than the release barrier
rather than the same one. CBI7 through CBI11 still govern one member, so a multi-member activation
has no post-activation authority story yet.

## Unreleased — Decision 11: negotiation compares provider identity

### Changed

- **BREAKING.** Portable negotiation now compares the provider by exact reference equality and
  refuses a mismatch as `unsupported-contract` with local code `provider-mismatch`. A required
  contract document naming a provider is binding rather than expectational.
- **BREAKING.** The Binding Plan's `provider` and `selectedProvider` facts, and the C9
  `selectedProvider` observation, are read from the **offered** document, so they name the provider
  that answered rather than the one the host asked for. Negotiation refuses a mismatch, so the value
  is unchanged wherever a plan exists.

### Added

- Neutral vector `PB-83-PROVIDER-SUBSTITUTED`, executed in Reference, pinning the refusal.

The composition-seam check is retained for the case negotiation cannot see: a required contract
naming a provider the resolution did not select, reachable only when the requirement names no
provider. Its refusal code stays `provider-substituted`.

BREAKING CHANGE: an endpoint answering as a provider the host did not require is now refused at
negotiation instead of establishing. A host that relied on the permissive behaviour must either name
the provider the peer will answer as, or reach the peer through a resolution that does. Version 0.1
defines no way to say "any provider of this Component"; that would be an additive change.

## Unreleased — CBI12 multi-member activation

### Added

- Reference Studio activation of several independent members under one CM4 activation, each with its own
  resolved position, portable contract, and conversation.
- The release barrier at the activation rather than the member: no member's ordinary-interaction
  gate opens until every member is Ready and CM4 accepts the activation.
- Retirement of every established member when any member fails, so none is left holding an open
  channel, with the failing occurrence named as the cause.
- Shared group-activation vectors pinning failure kinds and codes, member, released, and retired
  counts, and the runtime verdict, plus a phase-boundary completeness review.

CBI12 refuses a cyclic group: a multi-member group is a strongly connected component, which is what
Relational Initialisation exists for. Authority still governs one member — CBI3 and CBI6 through
CBI11 are unchanged — so a multi-member activation has no multi-member authority story yet.

## Unreleased — CBI11 declaration succession

### Added

- Reference Studio narrowing of the declaration in force to a successor CM2 resolution of the same position,
  which must declare strictly fewer authorities with every retained one keeping its exact tuple.
- Observed use as a veto: authority the member has already exercised cannot be narrowed away, while
  disuse never permits a narrowing.
- Shared succession vectors pinning outcome kinds and codes, dropped and vetoed authorities, the
  size of the declaration still in force, and that the member stays released, plus a phase-boundary
  completeness review.

CBI11 has no retirement path and does not change the participant set; it changes what a later CBI9
revision will admit. It does not verify that the successor declaration is truthful — a Component
that narrows dishonestly and then exercises what it dropped is caught by CBI10 as undeclared use.

## Unreleased — CBI10 observed-interaction verification

### Added

- Reference Studio verification of a CBI9 declaration against the portable interactions the member actually
  performed, projected into CM4 binding exercises.
- Derived, never claimed, authority admission on each projected exercise, so CM4's own rule that
  delivery cannot succeed when the external authority check denied it is what condemns interaction
  outside the declaration.
- Shared observed-interaction vectors pinning verdict kinds and codes, projected exercise counts,
  unexercised and uncovered declared authorities, the runtime's verdict, the member's stage, and the
  provider effects the interactions caused, plus a phase-boundary completeness review.

CBI10 supersedes CBI3's refusal of caller-authored binding-exercise authority by deriving that
authority instead of accepting it. It detects a declaration contradicted by use, never one
contradicted by disuse, and it neither authorizes a future interaction nor undoes a past one.

## Unreleased — CBI9 declared grant dependency and participant revision

### Added

- Reference Studio removal and substitution of participants in a live set, admitted while every
  declared dependency stays covered by the intended set.
- A dependency declaration whose names must equal the requested authority CM2 records for the
  CBI1-selected definition, with the caller supplying only the explicit typed mapping from each
  declared name to a CM5 Capability, target Actor, Operation, and scope.
- Shared revision vectors pinning outcome kinds, codes, evaluated counts, in-force set size and
  grant count, and whether the member is still released, plus a phase-boundary completeness review.

CBI9 closes the question CBI7 and CBI8 both deferred, and disposes of participant precedence:
coverage decides who may leave. It does not verify that a Component's declared authority is truthful
or complete, revoke a departing participant's authority elsewhere, or transfer state between a
departing and an arriving participant.

## Unreleased — CBI8 in-place participant extension

### Added

- Reference Studio growth of an admitted CBI6 participant set while its member stays released, with
  retained participants revalidated in the same all-or-none evaluation as the additions.
- Whole-set identity and receiving-domain Actor checks against participants that are already live,
  and a declined outcome that leaves the binding exactly as it was.
- Shared extension vectors pinning outcome kinds, codes, evaluated counts, the size of the set still
  in force, and whether the member is still released, plus a phase-boundary completeness review.

### Changed

- The cross-request identity check, admission shape check, exactness check, and member retirement
  are now shared between the CBI6, CBI7, and CBI8 coordinators within the stack instead of being
  restated per slice.

CBI8 only grows a set. Removal and substitution in place are declined and route through CBI7
retirement and a fresh CBI6 admission, which is also why participant precedence does not have to be
decided here.

## Unreleased — CBI7 participant-set withdrawal

### Added

- Reference Studio revalidation of every participant of an admitted CBI6 set from fresh explicit
  CM5 requests, keeping the shared member released only when the identical set renews identically.
- Fail-closed retirement for membership change, identity drift, and any participant that does not
  renew, with the unrenewed participants named in the result.
- Shared withdrawal vectors pinning outcome kinds, codes, evaluated counts, and unrenewed counts,
  plus a phase-boundary completeness review.

CBI7 answers the question CBI6 deferred: partial loss retires the shared member rather than
narrowing the set, because nothing in an admitted set says which participants its ordinary
interaction depends on. It does not replace a participant in place, order participants, or
propagate revocation to another domain.

## Unreleased — CBI6 participant-set admission

### Added

- Reference Studio admission of a set of participants over one singleton binding, each with its own
  CM5 request carrying one `ComponentParticipant` relationship and one or more exact narrow grants.
- Cross-request rules the evaluator cannot see: distinct admission, relationship, and authority
  request identities across the set, and distinct receiving-domain Actors per participant.
- Shared participant-admission vectors pinning failure kinds, codes, evaluation counts, and
  aggregate grant counts, plus a phase-boundary completeness review.

CBI6 admits a participant set. It does not revalidate or withdraw one, order participants, exercise
a granted Operation, or model participants joining or leaving an active binding.

## Unreleased — CBI5 authority withdrawal

### Added

- Reference Studio revalidation of the exact CM5 relationship and grant behind one active CBI3
  binding, using fresh explicit time, evidence, and policy.
- Fail-closed retirement for revoked, expired, mismatched, or non-identical authority, plus shared
  withdrawal vectors and a phase-boundary completeness review.

CBI5 governs subsequent ordinary interaction for one singleton binding. It does not cancel
in-flight execution or provide distributed revocation.

## Unreleased — CBI4 integrated profile comparison

### Added

- An independent Reference Studio canonical profile for five CBI3 integration outcomes, covering
  complete CM5 parity, CM4 effects and failures, portable lifecycle, and stable plan facts.
- Shared exact profile digests plus the CBI4 capability contract and completeness review.

### Fixed

- Portable Binding Plan compact-identifier facts now use the lowercase portable identity-space
  tokens instead of CLR enum casing, matching the Minimal realization and wire vocabulary.

CBI4 is data-only comparison evidence, not integrated cross-process execution or general
substitutability.

## Unreleased — CBI3 authority-gated portable activation

### Added

- A Reference Studio coordinator that requires one explicit occurrence-to-Actor mapping and one
  exact CM5 `ComponentParticipant` relationship and narrow grant before CBI2 activation.
- Fail-closed shape, mapping, admission, and lifecycle outcomes that stop denial before provider
  contact and preserve later portable failure.
- Native authority-integration tests plus the CBI3 capability contract and completeness review.

CBI3 does not transport a Capability through Portable Binding or map a CM5 Operation to a portable
invocation. Withdrawal, multiple participants or grants, CM4 binding projection, relational or
multi-member activation, and general interoperability remain outside this slice.

## Unreleased — CBI2 portable lifecycle orchestration

### Added

- A Reference Studio coordinator for one CBI1 member and one singleton, protocol-free CM4 plan.
- CM4 preflight before provider contact, PB7-derived stage evidence, portable-refusal projection,
  and portable Release only after CM4 Active.
- Native lifecycle tests plus the CBI2 capability contract and contract-completeness review.

CBI2 grants no authority and does not support relational or multi-member activation, replacement,
child Ports, mediation, wider Provider Sets, or general interoperability.

## Unreleased — CBI1 Component Management / Portable Binding integration

### Added

- A Reference Studio composition-root adapter from one completed direct `1..1` CM2 provider
  position to PB7 preflight, using explicit CM definition/occurrence and portable
  Component/provider identities.
- Structured fail-closed outcomes for unresolved, wider, mediated, empty or multiple, indirect,
  identity-mismatched, invalidly addressed, and portable-preflight-refused positions.
- Native integration tests plus the CBI1 capability contract and contract-completeness review.

CBI1 prepares no provider, fixes no Binding Plan, grants no authority, and makes no real
interchange or Architecture 0.8 conformance claim.

## Unreleased — Component Management CM4 experimental evidence

### Added

- A deterministic fake activation Host over successful CM3 plans, with optional effect-free
  preparation, complete member-stage evidence, lifecycle and ordinary gate enforcement, one logical
  Release, and explicit cutover events.
- Exact scoped replacement with unrelated-scope preservation, retained-generation disposition,
  pre- and post-cutover failure, rollback restoration, rollback-unavailable degradation, and
  retained-generation corruption.
- Post-Release distinct and mediated binding observations with typed identity, provenance, routing,
  authority-check, delivery, and failure evidence, plus runtime-open child-Port and host-assisted
  activation ordering.
- The neutral CM4 vector inventory, phase-wide permutation and failure-silence properties, and the
  completed CM4 contract-completeness review.

CM4 remains a fake Architecture 0.8 experiment. It is not a package loader, production activation
host, process-isolation boundary, durable rollback system, or authority policy; CM5 owns authority
and admission.

## Unreleased — Component Management CM3 experimental evidence

### Added

- A deterministic, effect-free activation-group planner that partitions complete activation graphs
  into maximal strongly connected groups and orders the condensation graph dependency-first without
  inventing member startup order.
- Exact contract/version checks, finite lifecycle-protocol validation, Ready reachability and wait
  analysis, Region/Port containment, structured wider-parent and refusal outcomes, and explicit
  closed-gate Local Initialisation, Interconnection, Relational Initialisation, and Ready stages.
- The neutral CM3 vector inventory, phase-wide permutation and failure-silence properties, and the
  completed CM3 contract-completeness review.

CM3 remains fake Architecture 0.8 experimental evidence. Planning performs no preparation,
establishment, lifecycle execution, Ready reporting, Release, Actor or authority establishment, or
active-generation mutation; those runtime transitions begin in CM4.

## Unreleased — Component Management CM2 experimental evidence

### Added

- A deterministic, effect-free recursive resolver that closes finite acyclic selections into an
  inspectable Proposed Stack and immutable generation.
- Occupied-binding stability, preference/publisher/generic/other ranking, policy exclusions,
  lower-bound Provider Sets, explicit optional preselection, occurrence sharing, direct and mediated
  Binding Plans, child Port envelopes, topology decisions, post-closure Activation Parameters, and
  structured refusals.
- The neutral CM2 vector inventory, complete permutation properties, and the completed CM2
  contract-completeness review.

CM2 remains fake Architecture 0.8 experimental evidence. It does not prepare, activate, establish an
Actor, grant authority, mutate an active generation, accept cyclic groups, or claim conformance.

## Unreleased — Component Management CM1 experimental evidence

### Added

- Standard contract/version discovery across zero or more controlled fake sources, with complete
  source-endpoint and publisher attribution, deterministic ordering, duplicate claims, advertised
  package-version observations, and the source-neutral storefront projection.
- Immutable staged acquisition with attributable evidence and fake-policy decisions, four
  structured fail-closed refusal categories, source disappearance, and an explicit observation that
  CM1 performs no selection, resolution, preparation, activation, Actor establishment, or Capability
  grant.
- A separate neutral source/evidence-availability fixture, exhaustive enumeration-permutation
  properties, a falsifiable local/remote storefront comparison, and the completed CM1
  contract-completeness review.

This remains a fake Architecture 0.8 experiment outside Brontide Base. It is not a marketplace,
package manager, loader, security product, conformance claim, or component-version change.

## Unreleased — Portable Component Binding 0.1 experimental evidence

### Added

- `Brontide.Reference.Experimental.Binding.Portable`: the Reference realization of the Portable
  Component Binding contract under [`binding/portable/`](../binding/portable/README.md). It adds a
  deterministic-CBOR core and length-delimited framing, portable references and the Shape floor,
  contract negotiation and a frozen, inspectable Binding Plan, local authority under strong Kleene
  evaluation with frameless denial, referenced resources, an explicit lifecycle with declared
  limits, the Channel envelopes, the C9 observation set, and a fixed direct-call alongside a
  negotiated process realization. `PortableCoreAdapter` is the Reference-owned adapter between the
  stack's `ShapeValue` model and the neutral positions.
- `PortableCompositionHandoff` and `PortableCompositionMember`: the seam by which a resolved
  Component requirement and an offered provision produce a Binding Plan during activation preflight,
  with the ordinary-interaction gate closed until a composition releases the member. Provider Sets,
  mediated exposure, an unselected provider, and a provider substituted by the answering endpoint are
  refused rather than approximated.

The retained line-delimited Cooling and Catalog experiments in the same project are unchanged and
remain diagnostic and legacy. This surface is experimental architecture evidence: it is not part of
Brontide Base, not an Architecture 0.8 conformance claim, not ratified, and not a component-version
change. These repository projects are not independently versioned packages, so no version is bumped.

## Unreleased — Architecture 0.7 Complete Draft evidence

### Added

- `CanonicalMemberName`, `MemberKind`, and `MemberName` value types for the provisional typed-member
  grammar. Existing `CanonicalName` parsing and all current wire contracts remain unchanged;
  `MemberKind` stays open while the architecture's catalogue and final glyph are provisional.

This addition is current-draft evidence, not ratification and not a component-version change.

## Unreleased — Architecture 0.5 implementation correction

### Changed

- Failed dynamic Genesis callbacks now roll back their actors, capabilities, newly issued leases,
  pre-existing mutable lease state, declarations, and Shape registrations before rethrowing.
  Lease reads and renewal are coordinated with the domain transaction, rolled-back leases are
  actively invalidated, other escaped references are rejected after rollback, and runtime effects
  or nested Genesis occurrences cannot run reentrantly inside the transaction. Genesis-context
  activity validation and mutation are atomic, so a concurrent issuer cannot resume after rollback.
- Rejected provenance retains execution metadata but does not retain the submitted protected input.
  Direct `ExecutionResult` records remain complete; audit consumers may inspect `HasInput`.
- A liveness lease remains terminally dead after trusted time observes expiry, even if the supplied
  clock later moves backward.

These repository projects are not independently versioned packages. The provenance behavior is a
security correction to an experimental public surface; future package extraction must choose its
initial compatibility baseline explicitly.
