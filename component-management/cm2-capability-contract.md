# CM2 recursive generational resolution capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §19, §20.1, §24, and §33,
Complete Draft, not ratified

CM2 consumes an immutable pending selection plus explicit fake environment and policy observations.
It produces either an inspectable Proposed Stack and immutable resolved generation, a proposal to
widen the parent generation, or one structured refusal. It does not prepare, activate, release,
establish an Actor, grant authority, mutate an active generation, or perform Component effects.

CM2 closes finite acyclic structure. A dependency or composition cycle is detected deterministically
and reported as `cycle-requires-cm3`; CM3 owns strongly connected group acceptance and activation
semantics.

## C1 — immutable effect-free resolution input

A request names the proposed generation, retained active generation if any, selected root
definitions, candidate observations, occupied bindings, preferences, Composition Parameter
selections, Activation Parameter values, Port envelopes, topology claims, and local policy
decisions. The resolver snapshots every collection at `Resolve` invocation before evaluation.

Property: mutating caller-owned collections after resolver construction cannot change an outcome,
and every outcome reports the all-false CM2 effect observation.

## C2 — finite recursive structural closure

Selected definitions and declared Composition Parameter choices expand before their requirements.
Each newly included definition contributes its own requirements recursively. Only declared choices
may introduce structure. Repeated acyclic paths converge on one definition entry; a cycle returns
`cycle-requires-cm3`; a missing definition, missing required provider, incompatible exact contract
version, unsupported Constraint, unbounded required cardinality, or contradictory duplicate identity
returns an attributable refusal.

Activation Parameter values are not inspected until structural closure succeeds and cannot add a
definition, requirement, Provider Set member, Region, Port, or topology relation.

Property: every successful closure is finite and contains exactly the transitive structural closure
of the roots, declared Composition Parameter choices, and selected providers.

## C3 — occupied-binding stability and preference visibility

A compatible occupied `1..1` binding is retained unless the request carries an authorised
replacement decision. An explicit preference never displaces it by itself. The Proposed Stack
records the unused preference, requester, preferred definition, and `compatible-occupant-retained`
reason.

An incompatible occupant is a conflict and does not satisfy the lower bound.

Property: changing candidate enumeration or adding an admissible preference cannot replace a
compatible occupied `1..1` binding without an authorised replacement decision.

## C4 — Provider Set cardinality and deterministic ranking

For each unfilled required position, admissible exact-version candidates rank by:

1. a preference declared by the requesting definition;
2. publisher affinity with the requester;
3. a declared generic implementation; and
4. any other compatible implementation.

Within a tier, definition, publisher, package, and source identities are ordinal tie-breaks.
Repository source identity never creates publisher affinity. Trust, origin, platform, authority,
resource, and local-policy observations may exclude a candidate at every tier and each exclusion is
recorded. Mirrored advertisements of one package remain attributable alternatives but cannot fill
two Provider Set positions.

The resolver fills the lower bound only. Optional capacity remains empty unless the request
explicitly preselects an additional admissible member. It never exceeds the maximum.

Property: every Provider Set satisfies its lower and upper bounds, contains no duplicate definition
introduced only by source mirroring, and is invariant under candidate enumeration.

## C5 — occurrence identity, sharing, and scope

Every selected provider has a strongly typed activation-occurrence identity. Existing retained
occurrences preserve their identity. A proposed occurrence may be shared only when the requirement
allows sharing and the provider independently declares compatible isolation, lifecycle, and
authority sharing for the same binding scope. Otherwise separate deterministic occurrence identities
are produced. Several definitions and several occurrences of one definition may coexist.

Property: two roles share an occurrence if and only if all four sharing conditions and the binding
scope agree.

## C6 — direct, distinct, and mediated exposure

`1..1` and deliberately member-addressed distinct Provider Sets produce direct Binding Plans.
A logical endpoint over several members requires declared Selection, Distribution, Aggregation,
Arbitration, or domain-specific Mediation. Its member identity, source, publisher, authority,
provenance, and failure domain remain visible.

Static or Host-erased Mediation remains an explicit resolved relationship. Mediation owning mutable
membership, residue, queues, topology-wide ordering, backpressure, authority, recovery, or lifecycle
policy must name a dedicated fake mediating Component. Mediated exposure never grants the consumer
the union of backing-member authority.

Property: every non-distinct multi-member logical endpoint has exactly one declared Mediation, and
no output omits its backing members or realization.

## C7 — child Regions, Ports, and wider-generation proposals

A child requirement names its containing Region and Port. The Port must exist, be activation-open or
runtime-open as requested, admit the exact contract, remain within cardinality, imports, exports,
authority ceiling, topology requirements, lifecycle mode, failure policy, and rollback boundary.
The resolver never changes a parent envelope implicitly. A declared widenable excess produces a
`wider-parent-generation-required` proposal; otherwise it is refused. Parent and retained active
generations remain unchanged.

Property: every resolved child member is inside one declared Port envelope, and no successful child
generation exceeds any envelope dimension.

## C8 — attributable topology policy

Every attachment occurrence receives a distinct local Topology Node. Attributable relation claims
are independently accepted, refined to a named relation, or rejected by fake local policy.
`PartOf`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`, `SharesPowerDomain`, and
`SharesFailureDomain` remain distinct observations. None establishes identity, trust, or authority.

Property: topology policy never merges nodes, invents an unobserved relation, or changes selected
provider or authority observations.

## C9 — post-closure Activation Parameters

After structural closure, each declared Activation Parameter slot is filled from the fake
environment or from its declared default. The generation records the effective value and provenance.
Missing required values return `activation-parameter-unavailable`. Extra environment values remain
unused observations and cannot introduce structure.

Property: changing only Activation Parameter values can change only effective parameter records or
an Activation Parameter refusal.

## C10 — complete explanation and immutable generation

Success returns a Proposed Stack and immutable generation naming roots, recursively included
definitions, retained occupants, Provider Sets, occurrences, binding scopes, direct plans,
Mediation, Regions and Ports, effective Parameters, topology decisions, alternatives, exclusions,
conflicts, preferences, sources, publishers, evidence identities, requested authority, restart
scope, and a deterministic decision record for every role.

The Proposed Stack and generation are ordered by their typed identities. Resolution while an older
generation is active has no effect on that generation.

Property: equal semantic input produces equal complete observations under every
enumeration permutation, and every output has the all-false selection, preparation, activation,
Actor-establishment, authority-grant, and active-generation-mutation profile.

## Structured outcomes

CM2 returns exactly one of:

- `resolved`;
- `wider-parent-generation-required`;
- `missing-definition`;
- `missing-dependency`;
- `incompatible-contract`;
- `unsupported-constraint`;
- `unbounded-required-cardinality`;
- `contradictory-identity`;
- `cycle-requires-cm3`;
- `ambiguous-selection`;
- `mediation-required`;
- `mediation-requires-component`;
- `port-unavailable`;
- `port-envelope-exceeded`;
- `activation-parameter-unavailable`.

Refusals name the responsible definition, requirement, candidate, Region, Port, or Parameter where
applicable, contain no partial generation, and carry the same all-false effect observation as
success.
