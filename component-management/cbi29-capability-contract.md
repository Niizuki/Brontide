# CBI29 fanned-out child-Port activation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI29 exercises the combination CBI28's completeness review left bounded: a wide Provider Set that
CM2 resolved inside one child Port. It adds no third activation path. CBI22 owns attachment and Port
containment, CBI27 translates the position as one portable member per provider, and CBI28 makes those
members one activation. This slice establishes whether those rules compose without relaxing any of
them.

## C1 — every member is contained in the one attached Port

Every member of the wide position comes from the generation's one Provider Set observation and
therefore carries the same containing Region and Port. The child attachment must name that Port.
Naming another Port or supplying a member outside the position is refused before authority or provider
effects.

Property: every admitted child member belongs to the position CM2 resolved into the attached Port.

## C2 — the whole position enters the child activation

The child path delegates the position to CBI27 as a whole. Omitting one resolved member is
`membership-not-resolved`; the caller cannot make the omission self-consistent by building a smaller
CM3 plan. No child member is established after that structural refusal.

Property: an attached wide child contains exactly the occurrences the generation resolved for its
position.

## C3 — portable member scopes and the child restart scope remain distinct facts

Each fanned-out member carries its own portable Binding scope, as CBI27 requires. The activation has
one CM restart scope, distinct from its parent's, as CBI22 requires. Reusing a portable scope within
the position is `scope-not-distinct`; it does not change which restart scope the child occupies.

Property: an admitted wide child has one restart scope and one distinct portable scope per member.

## C4 — authority and Release remain child-wide barriers

Authority is admitted independently per child occurrence, but a denial admits no partial child. Once
admitted, one member failing before Ready retires every child member and opens ordinary interaction for
none. The position's cardinality minimum does not become a runtime degraded-service rule merely
because the position is inside a Port.

Property: a wide child releases all of its members or none, and its grants are the union of the
independently admitted members' grants.

## C5 — the released parent is untouched

A wide child is still CBI22's second activation: separate plan, Release, restart scope, authority,
and providers. Success, structural refusal, authority denial, and establishment failure leave every
parent member released and produce no parent-provider effect.

Property: every CBI29 outcome preserves the parent's active generation and released members.

## C6 — both roots prove composition without new runtime semantics

Reference Studio and Minimal Host construct the path independently from their native CBI22, CBI27,
and CBI28 implementations. No Portable Binding, CM2, CM3, CM4, or CM5 surface changes. The evidence is
bounded to fake runtime activation of a distinct wide position in one runtime-open child Port; it does
not fill optional capacity, run degraded, span Ports, or resolve Decisions 12 through 16.

Property: removing the CBI29 evidence changes no behavior accepted by CBI1 through CBI28.
