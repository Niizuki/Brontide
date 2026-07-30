# CBI3 contract-completeness review

Status: complete phase-boundary absence audit

Reviewed contract:
[CBI3 authority-gated portable activation capability contract](./cbi3-capability-contract.md)

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

## Closed findings

| Silence found | Required disposition |
| --- | --- |
| Co-locating an occurrence and Actor in one call could be mistaken for identity correspondence. | C1 requires an explicit typed mapping and compares both sides with the validated CBI1 selection and CM5 participant before evaluation. |
| A broad CM5 request could admit one convenient grant while hiding denied or unrelated requests. | C2 permits exactly one `ComponentParticipant` relationship and one dependent, non-unlimited authority request; every larger shape fails before evaluation. |
| Caller-authored CM4 binding exercises could independently assert authority. | C2 requires the first CBI3 slice to carry no CM4 binding exercises; grant-to-exercise projection is explicitly future work. |
| Provider establishment could occur before local authority policy was known to admit the participant. | C3 runs the pure native CM5 evaluator first and creates no CBI1 member for denied, partially admitted, or invalid outcomes. |
| A nominally admitted outcome might contain no grant, several grants, or a grant for another request. | C3 and C4 require exactly one relationship and grant and compare every grant tuple field with the submitted request. |
| A CM5 grant could be mistaken for a Capability transported to the provider. | C5 keeps every CM5 identity and decision outside the portable contract, Binding Plan, constraint value, and payload; tests retain PB7's `noCapabilityTransfer` fact. |
| Matching CM5 and portable Operation names could be inferred from similar text. | C5 makes no cross-vocabulary Operation mapping. CBI3 gates activation on a local decision; it does not authorize a particular portable invocation. |
| A pure CM5 grant observation could be overstated as effective activity when portable establishment later fails. | C6 requires both admitted CM5 and released Active CBI2 results; lifecycle refusal remains visible alongside, but is not erased by, the earlier admission observation. |
| Evaluation time, evidence validity, or policy mistakes could be reimplemented inconsistently in the coordinator. | CBI3 delegates all CM5 semantics to the native evaluator and checks only the bounded integration shape and exact returned linkage. |
| Success could imply withdrawal, multi-party, relational, or general authority integration. | C8 enumerates those omissions and limits the claim to one local relationship and grant gating one singleton activation. |

## Result

No unresolved CBI3 contract-silence finding remains. This review designated shared serialized
comparison as the next integration item; CBI4 subsequently completed it. Grant withdrawal after
activation, cross-vocabulary
Operation-to-invocation mapping, multiple participants or grants, CM4 binding-exercise projection,
multi-member and relational activation, replacement, child Ports, mediation, wider Provider Sets,
real distribution, and general substitutability remain outside CBI3.
