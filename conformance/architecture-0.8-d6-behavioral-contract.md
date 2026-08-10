# Architecture 0.8 A08-D6 behavioral contract

Status: experimental runtime delivery contract for the separately authorized A08-D6 slice.

This contract delivers C12 Terminus for dynamic Actors through each stack's native authority-domain
representation. It does not change either stack's Architecture 0.7 target or ordinary 0.7 execution
entry point.

## Capabilities

### D6-C1 — Terminus is enumerable, attributable, and policy-declared

A dynamic authority domain exposes one immutable Terminus disposition policy. Retiring an active
Actor creates exactly one occurrence attributed to the active policy Actor and records the reason,
retired Actor, held authority disposition, direct outbound survival/extinction sets, survival
schedule, and stable-reference disposition.

Property: every successful retirement is represented by exactly one ordered Terminus occurrence;
unknown or already retired Actors and inactive policy Actors produce no occurrence or disposition
change.

Evidence: `BR-08-ADV-C12-003` in each native D6 conformance suite.

### D6-C2 — authority held by a retired Actor cannot authorize

Terminus immediately ends the retired Actor's participation. A Capability designating that Actor as
holder cannot authorize any later Execution, and the retired Actor cannot receive or originate a
new Delegation. The Actor and Capability records remain retained for attribution rather than being
reused or erased.

Property: every post-Terminus presentation whose designated holder is retired denies before the
Operation effect while preserving the stable Actor and Capability identities in authority records.

Evidence: `BR-08-ADV-C12-001` in each native D6 conformance suite.

### D6-C3 — immortal outbound grants survive with reachable grantor attribution

The declared D6 policy preserves direct outbound grants without liveness scope indefinitely. Their
descendants remain governed by the ordinary complete-chain conjunction. A surviving holder may
present or further narrow such a grant, and its parent/issuer chain continues to name the retired
grantor's retained identity.

Property: every surviving immortal outbound grant retains exactly the same target, Operations,
Constraints, parent, and grantor attribution across Terminus; retirement itself never broadens it.

Evidence: `BR-08-ADV-C12-002` in each native D6 conformance suite.

### D6-C4 — relationship-scoped outbound authority ends immediately

Every direct outbound grant whose effective chain contains a liveness lease maintained by the
retired Actor, and every descendant of that grant, is extinguished at Terminus. The maintaining
Actor's leases become dead and cannot be renewed.

Property: every relationship-scoped outbound presentation after Terminus denies before effect, and
no descendant can bypass that extinction by presenting an otherwise unchanged derived Capability.

Evidence: phase-wide liveness-disposition tests adjacent to `BR-08-ADV-C12-003` in each native D6
conformance suite.

## Declared D6 policy

- held Capabilities: presentation ends immediately with holder retirement;
- immortal direct outbound grants: survive indefinitely;
- liveness-scoped direct outbound grants and their descendants: extinguish immediately;
- Actor references: retained without reuse while any authority or provenance record mentions them;
- already authorized effects: follow the existing instantaneous-authorization boundary and are not
  retroactively re-evaluated by Terminus.

## Phase boundary

- D6 supplies the concrete policy required by C12; it does not standardize the open cross-domain
  Terminus disposition vocabulary, custodianship, or finite survival schedules.
- Reference carries parent objects and records Terminus in domain provenance; Minimal resolves
  parent references and returns a new immutable `World` with the occurrence and disposition sets.
- The status registry, hash-pinned Architecture 0.7 matrices, and both `Designed for` declarations
  remain unchanged.
- Stack-wide Architecture 0.8 retargeting and ratification are separate closure decisions.
