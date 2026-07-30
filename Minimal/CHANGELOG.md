# Changelog

## Unreleased — CBI7 participant-set withdrawal

### Added

- Minimal Host revalidation of every participant of an admitted CBI6 set from fresh explicit CM5
  requests, keeping the shared member released only when the identical set renews identically.
- Algebraic withdrawal results that name the unrenewed participants, and fail-closed retirement for
  membership change, identity drift, and any participant that does not renew.
- Shared withdrawal vectors pinning outcome kinds, codes, evaluated counts, and unrenewed counts,
  plus a phase-boundary completeness review.

CBI7 answers the question CBI6 deferred: partial loss retires the shared member rather than
narrowing the set, because nothing in an admitted set says which participants its ordinary
interaction depends on. It does not replace a participant in place, order participants, or
propagate revocation to another domain.

## Unreleased — CBI6 participant-set admission

### Added

- Minimal Host admission of a set of participants over one singleton binding, each with its own CM5
  request carrying one `ComponentParticipant` relationship and one or more exact narrow grants.
- Algebraic participant-set results and the cross-request rules the evaluator cannot see: distinct
  admission, relationship, and authority request identities across the set, and distinct
  receiving-domain Actors per participant.
- Shared participant-admission vectors pinning failure kinds, codes, evaluation counts, and
  aggregate grant counts, plus a phase-boundary completeness review.

CBI6 admits a participant set. It does not revalidate or withdraw one, order participants, exercise
a granted Operation, or model participants joining or leaving an active binding.

## Unreleased — CBI5 authority withdrawal

### Added

- Minimal Host revalidation of the exact CM5 relationship and grant behind one active CBI3 binding,
  using fresh explicit time, evidence, and policy.
- Algebraic withdrawal results, shared vectors, and a phase-boundary completeness review.

### Fixed

- PB7 retirement now closes the local member gate before peer withdrawal and termination, so a
  cleanup failure is visible without leaving ordinary interaction released.

CBI5 governs subsequent ordinary interaction for one singleton binding. It does not cancel
in-flight execution or provide distributed revocation.

## Unreleased — CBI4 integrated profile comparison

### Added

- An independent Minimal Host canonical profile for five CBI3 integration outcomes, covering
  complete CM5 parity, CM4 effects and failures, portable lifecycle, and stable plan facts.
- Shared exact profile digests plus the CBI4 capability contract and completeness review.

CBI4 is data-only comparison evidence, not integrated cross-process execution or general
substitutability.

## Unreleased — CBI3 authority-gated portable activation

### Added

- A Minimal Host coordinator that requires one explicit occurrence-to-Actor mapping and one exact
  CM5 `ComponentParticipant` relationship and narrow grant before CBI2 activation.
- Algebraic fail-closed shape, mapping, admission, and lifecycle outcomes that stop denial before
  provider contact and preserve later portable failure.
- Native authority-integration tests plus the CBI3 capability contract and completeness review.

CBI3 does not transport a Capability through Portable Binding or map a CM5 Operation to a portable
invocation. Withdrawal, multiple participants or grants, CM4 binding projection, relational or
multi-member activation, and general interoperability remain outside this slice.

## Unreleased — CBI2 portable lifecycle orchestration

### Added

- A Minimal Host coordinator for one CBI1 member and one singleton, protocol-free CM4 plan.
- CM4 preflight before provider contact, PB7-derived stage evidence, portable-refusal projection,
  and portable Release only after CM4 Active.
- Native lifecycle tests plus the CBI2 capability contract and contract-completeness review.

CBI2 grants no authority and does not support relational or multi-member activation, replacement,
child Ports, mediation, wider Provider Sets, or general interoperability.

## Unreleased — CBI1 Component Management / Portable Binding integration

### Added

- A Minimal Host composition-root adapter from one completed direct `1..1` CM2 provider position
  to PB7 preflight, using explicit CM definition/occurrence and portable Component/provider
  identities.
- Algebraic fail-closed outcomes for unresolved, wider, mediated, empty or multiple, indirect,
  identity-mismatched, invalidly addressed, and portable-preflight-refused positions.
- Native integration tests plus the CBI1 capability contract and contract-completeness review.

CBI1 prepares no provider, fixes no Binding Plan, grants no authority, and makes no real
interchange or Architecture 0.8 conformance claim.

## Unreleased — Component Management CM4 experimental evidence

### Added

- A Minimal-native deterministic fake activation Host over successful CM3 plans, with optional
  effect-free preparation, complete member-stage evidence, lifecycle and ordinary gate enforcement,
  one logical Release, and explicit cutover events.
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

- A Minimal-native, effect-free activation-group planner that partitions complete activation
  graphs into maximal strongly connected groups and orders the condensation graph dependency-first
  without inventing member startup order.
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

- A Minimal-native algebraic resolver that closes finite acyclic selections into immutable Proposed
  Stack and resolved-generation values with structured refusal and wider-parent outcomes.
- Occupied-binding stability, deterministic preference and affinity ranking, policy exclusions,
  lower-bound Provider Sets, explicit optional preselection, occurrence sharing, visible Mediation,
  child Port envelopes, topology decisions, and post-closure Activation Parameters.
- The neutral CM2 vector inventory, complete permutation properties, and the completed CM2
  contract-completeness review.

CM2 remains fake Architecture 0.8 experimental evidence. It does not prepare, activate, establish an
Actor, grant authority, mutate an active generation, accept cyclic groups, or claim conformance.

## Unreleased — Component Management CM1 experimental evidence

### Added

- A Minimal-native discovery pipeline over pure fake-source states, with standard contract/version
  queries, deterministic source/package/definition ordering, source-endpoint and publisher
  attribution, duplicate claims, advertised package versions, and the source-neutral storefront
  projection.
- Immutable staged artifacts carrying source-attributed contested evidence and fake-policy
  decisions; acquisition returns a `Staged`/`Refused` union with four exhaustive refusal cases.
  Source removal is a pure transition and every CM1 result reports no selection, resolution,
  preparation, activation, Actor establishment, or Capability grant.
- A separate neutral source/evidence-availability fixture, exhaustive enumeration-permutation
  properties, a falsifiable local/remote storefront comparison, and the completed CM1
  contract-completeness review.

This remains a fake Architecture 0.8 experiment outside Brontide Minimal Stack Base. It is not a
marketplace, package manager, loader, security product, conformance claim, or component-version
change.

## Unreleased — Portable Component Binding 0.1 experimental evidence

### Added

- `Brontide.Minimal.Binding.Portable`: the Minimal realization of the Portable Component Binding
  contract under [`binding/portable/`](../binding/portable/README.md), implemented natively rather
  than as a translation of the Reference surface. Every refusal is an explicit `PortableResult`
  value carrying its portable category, so a denial that never leaves the endpoint is a returned
  value rather than a raised failure; the Shape body is an algebraic union; the lifecycle is an
  immutable record whose illegal transition leaves the previous state intact; and the two resource
  flavors are separate union cases, so a forbidden implicit copy is unrepresentable in memory as
  well as refused on the wire. `PortableModelAdapter` is the Minimal-owned adapter between the
  stack's `ShapeValue` model and the neutral positions.
- `PortableCompositionHandoff` and `CompositionMember`: the seam by which a resolved Component
  requirement and an offered provision produce a Binding Plan during activation preflight. The stage
  is a union that carries the established binding, so a member outside the released case has no host
  to interact through. Provider Sets, mediated exposure, an unselected provider, and a provider
  substituted by the answering endpoint are refused rather than approximated.

The retained line-delimited Cooling and Catalog experiments in the same project are unchanged and
remain diagnostic and legacy. This surface is experimental architecture evidence: it is not part of
Brontide Minimal Stack Base, not an Architecture 0.8 conformance claim, not ratified, and not a
component-version change.

## Unreleased — Architecture 0.7 Complete Draft evidence

### Added

- Opaque `CanonicalMemberName`, `MemberKind`, and `MemberName` values for the provisional
  Architecture 0.7 typed-member grammar. Existing `CanonicalName` and binding wire forms are
  unchanged; member kinds remain open validated tokens while the catalogue is provisional.
- Recursive atomic, `AllOf`, `AnyOf`, and `Not` Constraint expressions with explicit satisfied,
  unsatisfied, and indeterminate results. Existing flat Capability and Operation requirements
  remain source-compatible atomic leaves; callers opt in through
  `Genesis.capabilityWithExpressions` and `World.delegateCapabilityWithExpressions`.
- Fail-closed target-side composite evaluation and experimental Composition candidate filtering.

These additions are current-draft evidence, not ratification and not a component-version change.

## Unreleased — Architecture 0.5 implementation correction

### Breaking

- `FragmentDefinition` now requires `HostShape`, the earliest compatible Shape for an authored
  Fragment. Update record construction to supply that host; unrelated open Shapes no longer accept
  the attachment unless they explicitly include the Fragment.
- Issuer-controlled Actor, Capability, Constraint, Execution, Occurrence, and Activity references
  no longer expose public record construction. Carry references returned by `Genesis`, `World`, or
  execution APIs instead of constructing scope/value records.
- Opaque generated references now include an internal deterministic allocation lineage. Treat a
  returned reference as one indivisible identity rather than correlating authority by its
  diagnostic scope/value pair; failed or discarded persistent branches cannot collide with an
  accepted branch, while replaying the same explicit transition still produces the same result.
- `World.create` now requires an explicit `TimeDomainReference`; execution receives a trusted
  `TemporalMark` from the host.
- `ExecutionRequest` now requires `Initiator`, `Target`, and `PresentedCapability`. Migrate callers
  from ambient grant/step helpers to `World.step environment world request`.
- `OperationDefinition` now declares its target Actor. Capability issuance records holder, target,
  operation scope, constraints, parent, issuer, and delegation permission; use
  `World.delegateCapability` to narrow authority.
- Operation handlers return `OperationFailure` rather than text. Use
  `OperationFailure.withoutDetails` or `OperationFailure.withDetails` so failure details have an
  independently validated Shape.
- Operation and Event identity is name-only. Remove semantic version arguments; Shape and Fragment
  references remain versioned.

These projects are repository components rather than independently published packages, so this
change has no package-version field to bump. Any future package extraction must choose its initial
version and treat the corrected API as the baseline.

### Added

- Attributed terminal Outcome Events, redacted execution audits, Genesis occurrence records, and
  authority-qualified canonical names.
- Genesis transactions use a shared authority-domain coordinator across every persistent `World`
  alias. Context issuance is bound to the exact transaction branch; pre-transaction aliases cannot
  dispatch, mutate, or nest while Genesis is active, and escaped uncommitted branches remain inert.
- Independent Catalog/resource process binding, strict adversarial vectors, replay and payload
  controls, and reproducible binding source-cost measurements.
