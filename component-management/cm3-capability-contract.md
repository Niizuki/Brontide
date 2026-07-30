# CM3 cyclic activation-group planning capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, Complete Draft, not ratified

CM3 consumes a complete, immutable fake activation graph whose members, dependency edges,
contracts, Regions, Port crossings, readiness inputs, and lifecycle protocols are already declared.
It partitions the graph into deterministic activation groups and returns either an inspectable
effect-free plan, a proposal to widen the parent generation, or one structured refusal.

CM3 does not prepare artifacts, materialise Components, establish Actors or endpoints, bind
resources, grant authority, execute lifecycle or ordinary Operations, report members Ready, Release
a group, mutate an active generation, or roll back. CM4 owns those effects. CM3 proves only that the
declared graph and lifecycle protocol admit a bounded activation-group plan.

## C1 — immutable, effect-free graph input

A request names one resolved generation, its members, all dependency edges, readiness declarations,
lifecycle protocols, Region crossings, and explicit fake policy observations. The planner snapshots
every caller-owned collection at `Plan` invocation.

Property: mutating caller-owned collections after planning cannot change the result, and every
outcome reports an all-false preparation, establishment, authority, lifecycle, Ready, Release,
ordinary-interaction, active-generation, and rollback effect observation.

## C2 — deterministic strongly connected groups

The planner includes every declared member exactly once and partitions dependency edges into
maximal strongly connected components. A singleton is cyclic only when it has a self-edge. Group
identity is derived from its ordinally first occurrence identity, members and internal edges are
ordered by typed identity, and the condensation graph is emitted in deterministic dependency-first
topological order. Duplicate member or edge identities, missing endpoints, or contradictory
occurrence-to-definition declarations are refused.

Property: every member belongs to exactly one maximal group, every inter-group edge appears in the
condensation graph, and permuting input members or edges cannot change the complete observation.

## C3 — finite closure versus recursive descriptor expansion

Ordinary post-Release dependency cycles are permitted and never manufacture a component startup
order. A cycle containing a descriptor-expansion edge is not finite closed structure and is refused
as `recursive-descriptor-expansion`. Descriptor-expansion edges outside cycles remain attributable
structural dependencies but do not create lifecycle traffic.

Property: adding or removing an ordinary internal edge may change SCC membership but never creates
member order; every cyclic descriptor-expansion path is refused before a plan is returned.

## C4 — exact contract and version compatibility

Each interaction or lifecycle edge names one required contract and exact version. Its target member
must declare that provision. A group containing a missing or version-conflicting provision is
refused with the responsible edge, source, target, contract, and requested version.

Property: every planned interaction edge has exactly one matching target provision, independent of
edge enumeration.

## C5 — declared bounded relational lifecycle protocols

An internal cyclic relational-initialisation edge is accepted only when it names a protocol
declaring a lifecycle Operation, narrow authority, input and output Shapes, concurrency or ordering,
a positive timeout, a finite retry bound, idempotence, completion, failure, and rollback behavior.
The protocol names the same source and target occurrences as its edge. Missing, duplicate,
misdirected, or incomplete protocols are refused. Ordinary interaction is never reclassified as
lifecycle traffic. Relational Initialisation is group-internal in CM3; a relational edge between
distinct activation groups is refused instead of disappearing into an inter-group dependency.

Property: every planned cyclic relational edge has exactly one complete bounded protocol and no
ordinary edge has one used on its behalf.

## C6 — readiness reachability and circular-wait rejection

Every member declares the local inputs required to reach Ready, the inputs already available without
same-group ordinary traffic, and any peers whose Ready state it waits for. Missing local inputs,
waits on unknown peers, and cycles in the Ready-wait graph are refused. A member may depend on peer
endpoints or declared relational protocol completion without waiting for that peer to become Ready.

Property: the Ready-wait graph of every planned group is acyclic and every required local input is
available before Release.

## C7 — closed gates and stage plan

Each group plan records Local Initialisation, Interconnection, optional Relational Initialisation,
and Ready in that order. Relational Initialisation is included exactly when the group has a declared
relational protocol. Ordinary interaction is closed in every CM3 stage. The plan records no
first-component order and ends at Ready with Release still pending.

Declared observations of ordinary pre-Release traffic or undeclared lifecycle traffic are refused
before any stage is planned.

Property: no CM3 success opens the ordinary gate, reports Active, or orders members within a stage.

## C8 — Region and Port cycle containment

An edge crossing Region identities names one Region-crossing declaration and Port. Both the import
and export must be declared for that edge, and the declaration must name the same source and target
Regions. A contained cross-Region cycle may form one activation group. Missing or contradictory
crossing evidence is refused; an otherwise valid undeclared crossing marked widenable returns
`wider-parent-generation-required` rather than silently widening the restart scope.

Property: every planned cross-Region edge is justified by exactly one matching Port crossing, and
no success changes the requested generation or restart scope.

## C9 — complete deterministic explanation

Success records generation and restart scope, every group, member, internal edge, inter-group edge,
cycle classification, lifecycle protocol, Region crossing, stage, closed-gate observation, and a
decision for every edge and member. Failure returns no partial group plan and names the responsible
group, member, edge, protocol, Region, or Port where applicable.

Property: equal semantic input produces equal complete observations under every input permutation,
and every success and failure carries the same all-false effect profile.

## Structured outcomes

CM3 returns exactly one of:

- `planned`;
- `wider-parent-generation-required`;
- `contradictory-identity`;
- `missing-member`;
- `recursive-descriptor-expansion`;
- `contract-version-conflict`;
- `lifecycle-protocol-required`;
- `lifecycle-protocol-incomplete`;
- `undeclared-lifecycle-traffic`;
- `ordinary-pre-release-traffic`;
- `ready-input-unavailable`;
- `circular-ready-wait`;
- `region-crossing-required`;
- `region-crossing-conflict`.
