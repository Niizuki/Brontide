# Implementation correction completion report

Status: completed and independently reviewed on 2026-07-23.

This report is the permanent narrative record of the correction programme formerly controlled by
`Brontide-Temporary-Implementation-Correction-Plan-0.1.md`. The machine-checkable evidence remains
in the conformance matrices, final reviewer attestations, and closure record. This report explains
what changed and why; it does not change either stack's stated architecture target or claim that
Architecture 0.8 is ratified.

## Outcome

The recommended work was completed across governance, draft design evidence, both implementation
stacks, tests, and review controls:

- the central architecture-status registry and review request now pin exact, canonical evidence;
- provisional typed member names are implemented without changing existing wire contracts;
- the Channel draft has an explicit semantic contract, requirements/risk ledger, and adversarial
  vector coverage;
- Reference and Minimal close the independently reproduced authority, rollback, identity,
  provenance, liveness, and persistent-state gaps;
- the complete two-stack gate passes without build warnings; and
- fresh independent reviewers found both stacks conforming to all 16 retained requirements at the
  same immutable target.

## Work delivered

### Architecture and evidence control

The architecture status registry remains the single route to the current Architecture 0.8 Complete
Draft and the retained Architecture 0.5 implementation baseline. Canonical hashes bind the stable
requirement vocabulary, stack matrices, implementation plans, and delivery ledgers. The review
workflow now records reviewer identity, context separation, exact target commits, per-requirement
decisions, the complete gate, and deletion authorization.

### Typed member names and Channel semantics

Commit `869231d` added provisional typed member-name value types to both stacks while deliberately
retaining `CanonicalName` and current interchange behavior. Commit `1582fdf` completed the Channel
draft semantic contract. The Channel work defines correlation, request/Outcome representation,
failure propagation, delivery categories, process boundaries, and capability-handling rules without
prematurely selecting a wire format or claiming ratification.

### Reference corrections

Reference now treats dynamic Genesis as an atomic authority transaction:

- failed callbacks roll back actors, capabilities, declarations, Shapes, and newly issued leases;
- retained actors and capabilities from a failed callback cannot execute or delegate;
- rejected provenance remains observable without retaining the protected submitted input;
- observed lease death is terminal across trusted-clock regression;
- failed renewal of a pre-existing lease restores expiry, trusted-time, death, and invalidation
  state;
- a newly issued lease removed by rollback is actively invalidated and cannot renew; and
- every `GenesisContext` activity check and mutation uses the same domain gate, preventing a
  concurrent issuer from resuming after rollback and deactivation.

The final two concurrency/lifetime corrections are commits `9a00428` and `e292ce3`. Permanent tests
include trusted-clock escaped-lease renewal, pre-existing lease restoration, escaped authority,
runtime/nested-Genesis rejection, and concurrent Actor/Capability/lease issuance.

### Minimal corrections

Minimal now provides the retained Base authority model and closes persistent-world edge cases:

- authored Fragments require a compatible host Shape lineage;
- failed Genesis never recycles opaque Actor or Capability identities;
- discarded and accepted branches within one callback allocate distinct authority identities;
- deterministic `World.step` replay remains derived from semantic transition inputs;
- every immutable `World` alias shares one Genesis transaction coordinator;
- original aliases cannot mutate registries, dispatch runtime work, or start nested Genesis during
  the transaction; and
- escaped uncommitted transaction branches remain permanently inert after success or failure.

The principal persistent-state corrections are commits `2384500`, `1ec3b88`, and `bd767de`.

## Independent-review discoveries

Independent review was intentionally repeated whenever a deeper invariant was reproduced. The
programme therefore corrected the observed behavior rather than accepting a green build as semantic
proof:

| Reproduced gap | Resolution |
| --- | --- |
| Reference authority survived failed dynamic Genesis | Registry rollback and retained-reference rejection (`f0941ca`) |
| Minimal failed branch recycled Actor identity | Non-reusable opaque allocation lineage (`2384500`) |
| Minimal same-callback branches collided; Reference pre-existing lease renewal was not restored | Persistent branch identity and lease snapshot/restore (`1ec3b88`) |
| A retained pre-transaction Minimal `World` alias bypassed Genesis isolation | Shared coordinator across all aliases (`bd767de`) |
| A rolled-back Reference lease object could still renew | Active lease invalidation (`9a00428`) |
| Concurrent Reference context calls could mutate after deactivation | Atomic activity validation and mutation (`e292ce3`) |

## Final verification

The immutable review target is `2049554c8e7ee5c26e4fcae6a103997737aa90f2`. From a clean detached
worktree, the following command completed with exit code 0:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

That gate covered text and link integrity, evidence and vector inventories, review controls,
warning-free builds, all ordinary Reference and Minimal tests, benchmarks, both directions of the
cross-process interchange suites, project dependency direction, Minimal boundary rules, and the
resolved assembly graph.

The final fresh automated reviews used separate identities and no implementation-session reasoning:

- Reference: 16/16 requirements conform; attestation SHA-256
  `809F8793756F3F78F0F4BD48C2E3CD3D78AA2D56AD913CC34550BE9B0EF6F9D7`.
- Minimal: 16/16 requirements conform; attestation SHA-256
  `F35BE48EA9D363BD42DB82DF065BE647D8131E433099C8FEE256E2F5A541CB87`.

Both reviewers recorded `current-deltas-recorded`: Architecture 0.8 remains a Complete Draft whose
implementation evidence is incomplete and which is not ratified. The Reference and Minimal READMEs
continue to own their local Architecture 0.7 targets and limitations.

## Permanent records and next work

The closure evidence is retained in:

- [`implementation-correction-status.md`](./implementation-correction-status.md);
- [`../conformance/reviews/review-request.json`](../conformance/reviews/review-request.json);
- [`../conformance/reviews/attestations/reference.json`](../conformance/reviews/attestations/reference.json);
- [`../conformance/reviews/attestations/minimal.json`](../conformance/reviews/attestations/minimal.json); and
- [`../conformance/reviews/closure.json`](../conformance/reviews/closure.json).

With the correction programme closed, the next architecture-delivery work should return to the
recorded Architecture 0.8 sequence: use the Channel semantics as the foundation for Portable
Component Binding, then advance Flow and the remaining current-draft deltas. Those are new delivery
milestones, not unfinished correction findings.
