# CM4 preparation, activation, scoped restart, and rollback capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, Complete Draft, not ratified

CM4 consumes one complete successful CM3 activation-group plan plus explicit fake Host
observations. It deterministically models optional preparation, establishment, one logical Release,
post-Release binding exercise, scoped generation replacement, child-Port attachment, and failure
recovery. The Reference and Minimal stacks implement this contract independently.

CM4 is not a loader, process supervisor, authority policy, or production rollback system. It does
not discover, acquire, select, or resolve Components. It never infers authority from activation:
CM5 owns authority admission. CM4 records only whether a binding exercise's separately supplied
authority check admitted delivery.

## C1 — optional preparation is effect-free

Preparation is absent or names a distinct preparation identity and a finite ordered set of fake
steps. A successful preparation may record artifact validation, image construction, cache warming,
or state snapshot readiness. It establishes no Actor, endpoint, binding, resource authority,
lifecycle relationship, Ready member, Release, ordinary interaction, active generation, or
retirement. Preparation failure stops before establishment and leaves every active scope unchanged.

Property: every preparation-only and preparation-failure path reports no establishment, authority,
Ready, Release, delivery, active-generation mutation, retirement, or rollback effect.

## C2 — exact restart scope and immutable retained state

The request names the CM3 plan's exact restart scope and a complete immutable snapshot of active
scope states. Exactly one state matches the restart scope. Its retained generation must match the
request, the target generation identity must differ from the retained identity, and every unrelated
scope is outside the activation transaction. Duplicate scopes, a
missing target scope, a mismatched retained generation, or an attempt to widen the scope is refused
before establishment.

Property: every outcome preserves the generation and activity state of every unrelated scope.

## C3 — complete named establishment stages

Each planned group advances in CM3's dependency-first group order through Local Initialisation,
Interconnection, optional Relational Initialisation, and Ready. Every planned member has exactly one
explicit outcome for every stage its group declares. Missing, duplicate, foreign-member, or
out-of-plan stage observations are refused. The first failed observation in deterministic
group/stage/member order stops establishment; no later stage is reported as completed.

Property: completed establishment events are a prefix of the plan's group/stage order, while
members within one stage have no startup order claim.

## C4 — lifecycle and ordinary gates are enforced

Local Initialisation admits no same-group peer traffic. Interconnection establishes inert Actors,
endpoints, bindings, resources, and separately decided authority observations while both lifecycle
and ordinary traffic remain closed. Relational Initialisation admits only the exact declared
lifecycle Operation, capability, input Shape, edge, and peer from the group's bounded CM3
protocols. Ready admits no peer traffic. Ordinary interaction is admitted only after the logical
Release and only through an edge retained by the CM3 plan.

Property: every refused interaction produces no delivery, and no pre-Release path admits ordinary
interaction.

## C5 — Ready is an all-member barrier and Release is logical

A group reaches Ready only when every planned member reports a successful Ready outcome. The
generation releases only after every group is Ready. One typed Release observation opens every
ordinary gate for the new generation as one logical barrier; the output records no first-member
order and makes no clock-level simultaneity claim.

Property: one missing or failed required Ready outcome prevents Release for every member, and every
successful activation contains exactly one Release event.

## C6 — post-Release bindings retain identity and provenance

After Release the Host may exercise declared fake distinct or mediated bindings. Every exercise
names its own identity, binding, consumer and provider occurrences, source provenance, exposure,
routing decision, authority-check result, and delivery result. Distinct exposure has no mediation;
mediated exposure names one Mediation identity. A failed or denied delivery remains an observation
of the active generation and cannot be rewritten as success.

Property: every admitted delivery is post-Release, authority-admitted, and retains the exact member,
binding, source, routing, and failure observations supplied at the external seam.

## C7 — cutover, retirement, and rollback are explicit

A successful Release atomically makes the new generation active in the target scope and retires or
retains the old generation according to the declared policy. Failure before cutover discards the
provisional generation and leaves the retained generation active. Failure after cutover follows
the declared rollback availability: an available intact retained generation is restored; an
unavailable rollback is reported as degraded without fabricating an active generation; a corrupted
retained generation makes rollback fail visibly.

Property: no failure outcome claims both generations active, and no rollback-unavailable or
corruption outcome claims restoration succeeded.

## C8 — child-Port attachment preserves the active parent

Child activation names an active parent scope and generation, a runtime-open Port, and whether the
Port is empty. Initial attachment to an empty open Port is not hot replacement. A sealed Port is
refused. Replacing an occupied Port requires an explicit replacement lifecycle declaration. The
child uses its own restart scope, and the parent scope remains active and unchanged.

For a host-assisted device, the internal child generation must reach its Release before the
exported outer boundary is released. Outer-Host-owned admission must be explicitly declared; it is
never inferred from assistance.

Property: every successful child activation preserves the parent generation, and every
host-assisted export event follows the child's internal Release event.

## C9 — no hidden authority or scope expansion

Actor, endpoint, binding, and resource establishment are Host observations, not proof of authority.
CM4 accepts the supplied authority-check result for each post-Release exercise but cannot create or
expand a Capability grant. A wider restart or parent-generation change is outside CM4 and must
return to resolution.

Property: every outcome reports zero Capability grants and the requested restart scope is unchanged.

## C10 — complete deterministic explanation

Every outcome records the request, target and retained generations, restart scope, preparation,
typed Release declaration and retained-generation disposition, completed stages, interaction
decisions, Release/cutover point, binding exercises, scope states, retirement or rollback result,
child attachment, and a deterministic decision log. Inputs and outputs are immutable snapshots.
Refusal before establishment returns no partial activation.

Property: equal semantic input produces equal complete observations under every input permutation,
and every failure path preserves unrelated scopes and reports no unrecorded delivery.

## Structured outcomes

CM4 returns exactly one of:

- `active`;
- `rolled-back`;
- `preparation-failed`;
- `establishment-failed`;
- `release-failed-before-cutover`;
- `rollback-unavailable`;
- `retained-generation-corrupted`;
- `invalid-cm3-plan`;
- `restart-scope-conflict`;
- `stage-observation-conflict`;
- `interaction-refused`;
- `binding-observation-conflict`;
- `child-port-closed`;
- `replacement-lifecycle-required`;
- `host-assisted-order-conflict`.
