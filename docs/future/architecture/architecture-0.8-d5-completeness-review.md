# Architecture 0.8 A08-D5 completeness review

Reviewed: 2026-08-10

This review asks what the two canonical C10 vectors could otherwise leave silent. A08-D5 remains
experimental evidence and does not retarget either implementation.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| Authority source | Can an authorized creation grant authority from the requester's create Capability? | No. The API requires a distinct provider-held Capability targeting the creating provider; the issued Capability names that authority as its immediate parent. |
| Conservation | Can issuance choose new Operations or a different target? | No. Native Delegation inherits both from the provider parent and only appends an exact Dataset Constraint. |
| Root reachability | Is checking only the immediate parent enough? | No. Both tests resolve the complete root-to-issued chain and require a primordial first link. |
| Attribution | Is the earlier Dataset issuer field being relabelled as Delegation? | No. The issuance result contains an actual registered/resolved Capability parent relation; Dataset attribution remains separate. |
| Space conjunction | Can a child satisfy one Dataset-space ancestor while exceeding another? | No. Every Dataset-authority Constraint on the complete provider chain must admit the designation. |
| Invalid provider | Can a closure pass authority held by another Actor or targeting another service? | No. Reference's active execution context and Minimal's preflight both require holder and target to be the creating provider. |
| Failure ordering | Can an out-of-scope request create the Dataset before authority refusal? | No. Scope and Dataset structural validation precede registry insertion and Capability Delegation. Minimal additionally preflights before its pure handler transition; Reference performs both inside the authorized handler effect. |
| Dataset validation | Can a later duplicate/role failure leave an orphan Capability? | No. Existing Dataset validation is performed before Delegation; the insertion after Delegation is the already-validated deterministic mutation. |
| Constraint use | Is Dataset designation being added to Base? | No. Its declaration, encoding, and evaluator remain inside the experimental Persistent Information components. Core/Kernel only expose stack-native provider Delegation and chain resolution. |
| Representation | Must both stacks record Delegation identically? | No. Reference registers a carried Capability object with a parent; Minimal returns an immutable World containing parent and `IssuedBy`. Both preserve C11's recorded ceiling. |
| Ordinary 0.7 behavior | Did D5 silently retarget retained execution paths? | No. Reference uses `ExecuteDraft08Async`; Minimal's coordinator uses `World.stepDraft08`. Existing ordinary entry points and matrices are unchanged. |

No additional capability is required inside A08-D5's C10 boundary. The next separately authorized
slice is A08-D6: attributable Terminus and authority-disposition policy (C12).
