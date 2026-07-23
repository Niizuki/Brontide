# Brontide Minimal Stack

Brontide Minimal Stack is the independent F# implementation and headless counterpoint.

**Designed for:** [Brontide Architecture 0.7](../Brontide-Architecture-0.7.md)

**Status:** Partial implementation with explicitly labelled experiments

This target states the architecture revision against which the stack was devised. The implemented
surface and known limitations are described here and exercised by the solution tests. Focused
experimental projects may state a later target locally; in particular, Component Management is
designed against Architecture 0.8 without changing the stack-wide target.

Minimal lives beside Brontide Reference Stack but does not reference Reference assemblies or reuse
Reference CLR types; the implementations support, challenge, and eventually substitute for one
another through an explicit external binding seam.

Architecture 0.7 M1-M2 now have Minimal-native Complete Draft evidence for recursive three-state
Constraint expressions, fail-closed target-side evaluation, experimental Composition selection,
and opaque typed-member canonical names with an open provisional member-kind token. The retained
[`conformance/architecture-0.7.json`](./conformance/architecture-0.7.json)
matrix is detailed test evidence, not the source of the implementation target and not a claim that
the remaining Architecture 0.7 work is implemented.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented in both host directions. Cooling exercises the
native authority/Fragment/Outcome path; Catalog adds nested repeated values, two Operations,
explicit failure, provider-scoped resource refusal, replay detection, and a fixed payload limit.

The implementation currently provides:

- an immutable `World` and pure `World.step` authority kernel with opaque issued references,
  explicit target and presented Capability, narrowing delegation, recursive fail-closed Constraint
  expressions, trusted time, and redacted audits;
- canonical versioned Shapes, authored Fragments, explicit projection, Operations, Constraints,
  Capabilities, attenuation, Outcomes, Events, and provenance;
- native Cooling, Event Distribution, and Flow semantics;
- isolated Enrichment and implementation-baseline Composition experiments;
- deterministic CPU imaging, boxed application boundaries, provider opposition and selection
  explanations, and visible optimisation eligibility;
- a tagged JSON ShapeValue codec and versioned external manifest negotiation;
- independently implemented Cooling v2 and Catalog v1 process-binding experiments, host clients,
  provider endpoints, adversarial vectors, and structured operational observations;
- a headless host and five F# test assemblies.

There is deliberately no `global.json`. Brontide Minimal Stack targets .NET 10; the supported range
and CI feature bands are checked by [`../docs/sdk-policy.md`](../docs/sdk-policy.md). The selected
preview SDK does not copy its bundled `FSharp.Core` runtime into application outputs, so
`Directory.Build.props` applies an explicit, bounded `MSBuildToolsPath` copy workaround. It is not a
version pin and has a documented removal gate.

## Run

```powershell
dotnet build .\Brontide.Minimal.slnx -nologo
dotnet test .\Brontide.Minimal.slnx -nologo --no-build
dotnet run --project .\src\Brontide.Minimal.Host\Brontide.Minimal.Host.fsproj -nologo
.\build\verify-boundaries.ps1
```

The ordinary solution test run executes fixture and boundary tests and skips the foreign-process
cases unless `BRONTIDE_REFERENCE_PROVIDER` names a built endpoint. Run the complete two-way clean gate,
including both real foreign processes, from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

See [`docs/integration-guide.md`](./docs/integration-guide.md) for the binding quick reference.
See [`../docs/public-boundaries.md`](../docs/public-boundaries.md) for payload, timeout, cleanup,
redaction, replay, and denial-of-service assumptions.

See `docs/milestone-evidence.md` for the implemented first boundary and the Event/Flow, Macro
Operation, mixed-image-workspace, machine, and authority-federation work intentionally deferred.
