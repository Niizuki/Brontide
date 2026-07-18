# Cooling contract matrix

| Neutral contract item | Brontide Reference Stack mapping | Brontide Minimal Stack mapping | Classification | Information loss |
| --- | --- | --- | --- | --- |
| `interchange.tests.cooling.set-enabled` 1 | `true` maps to `Fan.SetSpeed(100)` and `false` maps to `Fan.Stop` through `BinaryCoolingComponent` | Maps to `Cooling.apply (SetCoolingEnabled enabled)` | Brontide Reference Stack semantic Adapter; Brontide Minimal Stack native mapping | None for the declared binary enabled/disabled contract; Brontide Reference Stack fan magnitude is intentionally outside the neutral result |
| Input Shape `interchange.tests.cooling.command` 1 | Open Brontide Reference Stack record Shape | Open Brontide Minimal Stack record Shape | Native mapping | None |
| Output Shape `interchange.tests.cooling.result` 1 | Brontide Reference Stack record result | Brontide Minimal Stack `CoolingState` projection | Native mapping | Brontide Minimal Stack target/measured temperatures are not part of the declared result and are projected away |
| Required `interchange.tests.cooling.host-context` 1 Fragment | Added by Brontide Reference Stack's local Enrichment composition | Added by Brontide Minimal Stack's local targeted Enrichment | Binding requirement | None; it carries attributable information only and never authority |
| Additional authored input Fragments | Brontide Reference Stack host validates known local attachments; provider treats them as unsupported | Brontide Minimal Stack host validates known local attachments; provider treats them as unsupported | Binding requirement | Canonical execution ignores them; transparent protocol forwarding preserves their exact tagged value |
| Host authority | Brontide Reference Stack `AuthorityDomain` evaluates Actor, Capability, target, Operation, Shape, and Constraints before the handler starts | Brontide Minimal Stack `World.step` evaluates Actor, presented Capability, target, Operation, Shape, and Constraints before the handler starts | Binding requirement | No Capability is serialized |
| Failed semantic result | Shaped `OperationEffect.Failure` | `ExecutionOutcome` with `Failed` status and shaped details at the binding seam | Native Outcome mapping | No exception crosses the boundary |
| Provider process exit | Failed Brontide Reference Stack-native Outcome plus experimental binding observation | Failed Brontide Minimal Stack-native Outcome plus experimental binding observation | Binding requirement | Provider-private diagnostics are not promoted to semantic details |

No Brontide ambiguity blocks this fixture. Two findings remain deliberately provisional: the portable
descriptor vocabulary and the universal form of binding observations are not ratified. Current
design is sourced from [Architecture 0.7](../Brontide-Architecture-0.7.md); this retained matrix is
not 0.7 conformance evidence.
