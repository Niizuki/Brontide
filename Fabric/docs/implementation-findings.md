# Fabric implementation findings

## Targeted Enrichment failure attribution

- **Atlas section:** 16.6
- **Minimal scenario:** `Input.Pointer.Move` requires `Experiment:ThermalContext`, but its active
  composition has no provider or lacks the declared telemetry source.
- **Observed result:** resolution fails deterministically before `AuthorityDomain.ExecuteAsync`;
  therefore no Base Execution exists in the Core provenance log. The experimental exception names
  the composition, boundary, provider, and missing or competing source.
- **Expected result:** Architecture 0.4 deliberately leaves open whether pre-Execution Enrichment
  failures need attributable Atlas occurrence semantics.
- **Classification:** experimental result
- **Current disposition:** retain visible experimental failure outside Core and normative
  conformance. The experiment uses direct materialisation by default and records the selected
  realisation strategy in `EnrichmentTrace`; no strategy is treated as semantic.
