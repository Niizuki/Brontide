# CBI2 contract-completeness review

Status: complete phase-boundary absence audit

Reviewed contract:
[CBI2 portable lifecycle orchestration capability contract](./cbi2-capability-contract.md)

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

## Closed findings

| Silence found | Required disposition |
| --- | --- |
| Caller-provided CM4 stage outcomes could be mistaken for evidence from PB7. | C3 and C6 require the coordinator to replace the complete collection with observations derived from the prepared portable member. |
| A provider could be contacted before unrelated CM4 inputs were known to be valid. | C2 runs the pure CM4 lifecycle first and requires every preflight refusal to leave the member without a Binding Plan. |
| A singleton portable member could be attached to a different or larger CM3 plan. | C1 requires one group containing only the exactly selected occurrence and no lifecycle protocol. |
| Portable refusal could disappear behind a generic coordinator error. | C4 projects failed Interconnection and Ready observations through CM4 and retains the resulting `EstablishmentFailed` prefix. |
| Ready could be inferred merely because Interconnection returned. | C3 checks the portable member's actual Ready state before the successful CM4 evaluation. |
| CM4 Active and PB7 Release could be observed in the wrong order. | C5 permits portable Release only after an Active CM4 observation and returns success only after both transitions succeed. |
| A second evaluation of the pure CM4 request could silently differ. | The same request with the same coordinator-derived observations is evaluated before and after portable establishment; any changed result fails closed before portable Release. |
| Cancellation could be converted into an establishment refusal. | Reference explicitly rethrows cancellation; Minimal's portable result surface retains its declared interruption category. |
| Lifecycle success could be overstated as authority admission. | C6 leaves CM5 outside the coordinator, and the tests assert that CM4's `CapabilityGranted` effect remains false. |
| The narrow first lifecycle slice could imply relational, multi-member, replacement, child-Port, mediation, or wider-cardinality support. | C8 enumerates these omissions and the coordinator rejects non-singleton or protocol-bearing plans before provider establishment. |

## Result

No unresolved CBI2 contract-silence finding remains. CM5 authority admission is the next
implementable integration item. Multi-member release barriers, Relational Initialisation,
replacement and retirement, child Ports, serialized comparison, mediation, wider Provider Sets,
real distribution, and general substitutability remain outside CBI2.
