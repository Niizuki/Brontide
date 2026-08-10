# Architecture 0.8 delivery-audit capability contract

Status: audit-only implementation evidence for the Complete Draft Architecture 0.8. This contract
authorizes inventory, classification, and mechanical verification. It does not authorize runtime
migration or change either stack's locally stated Architecture 0.7 target.

## Capability contract

| ID | Observable audit capability | Property over every audited row | Evidence |
| --- | --- | --- | --- |
| DA1 | Bind the audit to the exact current Architecture 0.8 document selected by the status registry. | No matrix can refer to a different revision, status, path, or digest. | The verifier recomputes the architecture digest and compares the registry and inventory. |
| DA2 | Account for every C1-C14 change, all 33 authored vectors, and both documentation-only coverage entries exactly once. | Adding, removing, or duplicating any change, vector, or coverage item fails the gate. | Shared requirements inventory plus canonical vector comparison. |
| DA3 | Preserve independent Reference and Minimal findings. | Each stack owns exactly one row per shared requirement; statuses and evidence may differ, and evidence paths must remain attributable. | Separate stack matrices with path and anchor validation. |
| DA4 | Prevent audit evidence from becoming an implementation claim. | No row may use `accepted`, `implemented`, `tested`, or an equivalent promotion status; every authored runtime vector remains `not-executed` unless the only obligation is the C11 attestation. | Closed status vocabulary and matrix disposition checks. |
| DA5 | Preserve known conflicts and representation ceilings. | C6 and C7 remain `conflicting`; C7 names all three superseded 0.7 requirements; C11 records the distinct stack representations and remains only `handoff-attested`. | Shared supersession metadata and per-stack C6/C7/C11 checks. |
| DA6 | Produce an ordered, bounded runtime queue without authorizing it. | Every runtime-requiring change in C1-C10 and C12 appears in exactly one proposed slice, C11 constrains all slices, dependencies are explicit, and C13/C14 remain outside runtime delivery. | Delivery-audit report plus mechanical slice accounting. |

## Failure behavior

The verifier fails closed when JSON is malformed, a path or anchor cannot be resolved, an unknown
status appears, a canonical vector is omitted or duplicated, a stack row promotes candidate
evidence, or the report loses the runtime-authorization boundary. A successful audit means the
inventory is internally complete; it does not mean Architecture 0.8 behavior executes.

## Evidence boundary

Candidate evidence is a pointer for a future implementation phase to test, break, retain, or
replace. `candidate-reusable` means the current behavior appears semantically aligned;
`candidate-partial` means only part of the requirement is present. Neither is conformance.
`conflicting` is still correct Architecture 0.7 evidence until a governed migration lands.
