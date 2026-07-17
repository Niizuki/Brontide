# Linen interchange integration guide

## Rules for coding agents

- Keep binding and host machinery outside `Linen.Model` and `Linen.Kernel`; Kernel remains a pure
  authority/transition dependency.
- Never reference Fabric projects or assemblies. Exchange only the versioned root fixtures and one
  JSON object per line.
- Run `World.step` authority and Constraint checks before provider activation. Never serialize a
  Capability or reinterpret delivery as authority.
- Treat protocol version 2, its manifest, and `LinenExperimentalBindingObservation` as experimental.

## Quick reference

`LinenCoolingBindingHost` launches a foreign provider named by `ProviderLaunch`. The neutral input
requires `interchange.tests.cooling.host-context`; `TargetedEnrichment.resolve` can add that
Fragment from an already available host value before `World.step`.

`Linen.Interchange.Provider` serves the same contract through native
`Cooling.apply (SetCoolingEnabled enabled)`. `--reject-protocol` and
`--crash-after-activation` are deterministic test modes.

Ordinary tests skip real Fabric launch when `ATLAS_FABRIC_PROVIDER` is absent. Use the root
`build/verify-interchange.ps1` command for the required two-way process evidence.
