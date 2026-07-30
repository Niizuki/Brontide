# CBI1 Component Management to Portable Binding capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI1 is the first composition-root integration between the completed fake Component Management
programme and Portable Component Binding PB7. It turns one provider position already resolved by
CM2 into one prepared portable composition member. It does not resolve, rank, negotiate, activate,
release, or grant authority by itself.

## C1 — resolution remains authoritative

The integration accepts only a completed native CM2 `ResolvedGeneration`. It selects nothing and
never falls back to a compatible provider when the resolved member is absent or mismatched.

Property: changing or removing the resolved member can only change the result to a refusal; it
cannot cause another provider to be selected.

## C2 — identity correspondence is explicit

The composition root receives an explicit native mapping that names the CM requirement,
definition, and occurrence together with the portable Component, provider, contract, and endpoint
identities. Equal string spellings across identity spaces imply nothing. Endpoint designations must
be non-empty UTF-8 text within the supplied portable contract's declared text bound.

Property: a mapping that names any different CM identity is refused before portable preflight.

## C3 — the first slice is exactly direct one-to-one

The resolved Provider Set must declare cardinality `1..1`, distinct exposure, no Mediation, exactly
one member, and exactly one direct binding-plan observation for that member.

Property: wider, optional-empty, multiple-member, mediated, or indirect positions produce no
portable member.

## C4 — scope survives the seam

The CM binding-scope identity is unwrapped only at the composition-root boundary, parsed into the
portable binding-scope type, and preserved by the prepared member.

Property: every successful member reports the same scope text the resolved Provider Set carried.

## C5 — portable negotiation remains authoritative

After structural CM checks, the integration calls the existing PB7 handoff with the explicit
portable requirement, provision, and required contract. It does not duplicate contract
negotiation, provider identity checks, or portable validation.

Property: every PB7 preflight refusal remains a visible integration refusal and leaves no member.

## C6 — refusal precedes provider effects

Preparation is local and effect-free. A refusal returns a structured code and reason, creates no
portable member, starts no provider, fixes no Binding Plan, establishes no Actor or Capability, and
does not mutate the CM generation.

Property: every CBI1 failure path has zero CM effects and no portable member or Binding Plan.

## C7 — both stacks integrate independently

Reference Studio and Minimal Host each implement the composition-root mapping in native code. The
Component Management and Binding projects remain independent and neither stack references the
other.

Property: deleting either composition-root adapter does not remove or alter either underlying
component implementation.

## C8 — evidence remains bounded

CBI1 proves only that a completed fake CM2 direct `1..1` decision can enter PB7 preflight without
reselecting or conflating identities. CM4 stage orchestration, CM5 authority admission, process
comparison, real distribution, production activation, and general substitutability remain future
integration work.

Property: every status statement preserves this boundary.
