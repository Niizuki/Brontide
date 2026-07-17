# Fabric interchange integration guide

## Rules for coding agents

- Keep the binding in `Fabric.Experimental.Binding`; never move transport or provider selection into
  `Fabric.Core`.
- Never reference Linen projects or assemblies. Exchange only the versioned root fixtures and one
  JSON object per line.
- Evaluate Fabric Actor, Capability, target, Operation, Shape, and Constraints before starting the
  provider process. Never serialize a Capability.
- Treat protocol version 2, its manifest, and `ExperimentalBindingObservation` as experimental.

## Quick reference

`FabricCoolingBindingHost` launches a foreign provider named by `ProviderLaunch`. The host accepts
the neutral `interchange.tests.cooling.set-enabled` contract. Its input must contain the required
`interchange.tests.cooling.host-context` Fragment; `TargetedEnrichmentComposition` can construct it
from an already available host-local value.

`Fabric.Interchange.Provider` serves the same contract using `BinaryCoolingComponent`. It maps
enabled to native `Fan.SetSpeed(100)` and disabled to `Fan.Stop`. `--reject-protocol` and
`--crash-after-activation` are deterministic test modes.

Ordinary tests skip real Linen launch when `ATLAS_LINEN_PROVIDER` is absent. Use the root
`build/verify-interchange.ps1` command for the required two-way process evidence.
