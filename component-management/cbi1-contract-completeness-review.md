# CBI1 contract-completeness review

Status: complete phase-boundary absence audit

Reviewed contract:
[CBI1 Component Management to Portable Binding capability contract](./cbi1-capability-contract.md)

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

## Closed findings

| Silence found | Required disposition |
| --- | --- |
| Structural similarity could be mistaken for identity correspondence. | C2 requires an explicit mapping naming the selected CM definition and occurrence plus distinct portable Component and provider references. |
| A duplicate or absent requirement could be selected by enumeration order. | The adapters require exactly one resolved Provider Set with the requested requirement identity. |
| A `1..1` declaration could still carry zero or several actual members. | C3 independently requires exactly one member and refuses every other observed membership. |
| A nominally direct position could carry no direct observation or an observation for another occurrence. | The adapters require exactly one binding-plan observation, marked direct, without Mediation, for the selected occurrence. |
| A mapping could silently substitute another occurrence of the same definition. | C2 compares both definition and occurrence identity before PB7 is called. |
| Empty or unbounded opaque endpoint text could be carried into the future plan. | Endpoint designations must be non-empty UTF-8 and fit the supplied portable contract's declared `maxTextBytes` bound. |
| Portable contract validation could be accidentally reimplemented by the integration. | C5 delegates portable Component, provider, declaration, and preflight checks to PB7 and shapes any PB7 refusal as an integration failure. |
| A failed resolution might still be probed for a convenient member. | C1 accepts only the completed `Resolved` case and returns before inspecting portable declarations otherwise. |
| Adding the seam to either underlying experimental library would collapse dependency direction. | C7 places native adapters only in Reference Studio and Minimal Host, their respective composition roots. |
| Successful preparation could be overstated as negotiation, readiness, Release, authority, or general interoperability. | C6 and C8 state that preparation is effect-free, has no Binding Plan yet, and proves only entry into PB7 preflight for the supported direct `1..1` slice. |

## Result

No unresolved CBI1 contract-silence finding remains. CM4 stage orchestration, CM5 authority
admission, process comparison, mediation, wider Provider Sets, real distribution, production
activation, and general substitutability remain outside this first integration slice.
