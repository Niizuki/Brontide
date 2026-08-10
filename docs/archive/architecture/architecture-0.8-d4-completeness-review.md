# Architecture 0.8 A08-D4 completeness review

Reviewed: 2026-08-10

This review asks what the six canonical C1/C5 vectors could otherwise leave silent. A08-D4 remains
experimental evidence and does not retarget either implementation.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| Ancestor depth | Does the liveness check apply only to the leaf or immediate parent? | No. Both implementations enumerate the complete root-to-leaf chain already pinned by D1; D4 places the lease on the root and presents a child. |
| Presentation instant | Can each ancestor observe a different time? | No. Reference captures one trusted instant before traversal. Minimal supplies one immutable `TrustedTime` to the complete transition. |
| Evaluator loss | Can a well-formed but unavailable liveness evaluator be treated as satisfied? | No. C1-002 observes the named `UnsupportedConstraint`/Unknown path and zero effects without exposing the scope value. |
| Liveness effect | Does checking or renewing liveness itself authorize or dispatch the Operation? | No. Evaluation only returns a narrowing result; C1-003 observes one handler call after the whole chain succeeds. |
| Window origin | What does “same window” mean across independent clocks? | The contract now fixes positive whole-millisecond, half-open buckets aligned to the authority time domain's epoch. Reference uses Unix-epoch milliseconds; Minimal uses its explicit logical time-domain epoch. |
| Occurrence identity | Can two equal rate values in distinct chain locations accidentally share a budget? | No. Accounting includes carrying Capability identity plus expression and atom position. Only descendants of that exact occurrence pool together. |
| Sibling multiplication | Can holder or leaf identity instantiate a fresh budget? | No. Neither appears in the accounting key; C5-001 exercises two siblings and permits only the ancestor maximum in aggregate. |
| Concurrent authorization | Can two Reference executions race between checking and committing the final unit? | No. Reference serializes the accounting check and commit under a dedicated gate. Minimal is an immutable authority transition: the caller must advance one returned `World`; stale forks are not one authority-domain transition sequence. |
| Composite expressions | Is usage committed before the complete expression/chain result is known? | No. Satisfied quantified atoms prepare claims; all claims are discarded on any later False or Unknown and committed together only after complete authorization. |
| Effect failure | Does “successful authorization” mean “successful Operation effect”? | No. Once the complete authority gate succeeds, accounting commits before dispatch. A handler-level failure still consumed an authorized attempt; a pre-dispatch denial did not. |
| Unsupported scopes | Can an ordinary evaluator make a vocabulary-defined scope appear enforceable? | No. Recognition reports it Declined and presentation returns Unknown before invoking that evaluator. |
| Ordinary 0.7 behavior | Did D4 silently retarget the retained execution entry points? | No. The new standard Constraints are authored only by consumers choosing them, and D4 conformance runs through `ExecuteDraft08Async`/`World.stepDraft08`; the ordinary 0.7 comparison remains a required gate. |

No additional capability is required inside A08-D4's C1/C5 boundary. The next separately authorized
slice is A08-D5: provider resource-Capability issuance by derivation (C10).
