# Cooling contract matrix

| Neutral contract item | Fabric mapping | Linen mapping | Classification | Information loss |
| --- | --- | --- | --- | --- |
| `interchange.tests.cooling.set-enabled` 1 | `true` maps to `Fan.SetSpeed(100)` and `false` maps to `Fan.Stop` through `BinaryCoolingComponent` | Maps to `Cooling.apply (SetCoolingEnabled enabled)` | Fabric semantic Adapter; Linen native mapping | None for the declared binary enabled/disabled contract; Fabric fan magnitude is intentionally outside the neutral result |
| Input Shape `interchange.tests.cooling.command` 1 | Open Fabric record Shape | Open Linen record Shape | Native mapping | None |
| Output Shape `interchange.tests.cooling.result` 1 | Fabric record result | Linen `CoolingState` projection | Native mapping | Linen target/measured temperatures are not part of the declared result and are projected away |
| Required `interchange.tests.cooling.host-context` 1 Fragment | Added by Fabric's local Enrichment composition | Added by Linen's local targeted Enrichment | Binding requirement | None; it carries attributable information only and never authority |
| Additional authored input Fragments | Fabric host validates known local attachments; provider treats them as unsupported | Linen host validates known local attachments; provider treats them as unsupported | Binding requirement | Canonical execution ignores them; transparent protocol forwarding preserves their exact tagged value |
| Host authority | Fabric `AuthorityDomain` evaluates Actor, Capability, target, Operation, Shape, and Constraints before the handler starts | Linen `World.step` evaluates Actor, grant, Operation, Shape, and Constraints before the handler starts | Binding requirement | No Capability is serialized |
| Failed semantic result | Shaped `OperationEffect.Failure` | `ExecutionOutcome` with `Failed` status and shaped details at the binding seam | Native Outcome mapping | No exception crosses the boundary |
| Provider process exit | Failed Fabric-native Outcome plus experimental binding observation | Failed Linen-native Outcome plus experimental binding observation | Binding requirement | Provider-private diagnostics are not promoted to semantic details |

No Atlas ambiguity blocks this fixture. Two findings remain deliberately provisional: the portable
descriptor vocabulary and the universal form of binding observations are not ratified by
Architecture 0.5.
