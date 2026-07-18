# Brontide Minimal Stack

Brontide Minimal Stack is the independent F# implementation and headless counterpoint. New
architectural work targets
[`Brontide-Architecture-0.7.md`](../Brontide-Architecture-0.7.md), a complete draft whose
implementation evidence and ratification are still pending. The current executable evidence
baseline remains Architecture 0.5. Minimal lives beside Brontide Reference Stack but does not
reference Reference assemblies or reuse Reference CLR types; the implementations support,
challenge, and eventually substitute for one another through an explicit external binding seam.

Minimal delivery of the complete Architecture 0.7 change set is routed by
[`docs/architecture-0.7-delivery.md`](./docs/architecture-0.7-delivery.md) and planned in
[`Brontide-Minimal-Stack-Implementation-Plan-0.3.md`](../Brontide-Minimal-Stack-Implementation-Plan-0.3.md).
Nothing in that plan upgrades the current Architecture 0.5 implementation claim until the planned
evidence is accepted.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented in both host directions. Cooling exercises the
native authority/Fragment/Outcome path; Catalog adds nested repeated values, two Operations,
explicit failure, provider-scoped resource refusal, replay detection, and a fixed payload limit.

The implementation currently provides:

- an immutable `World` and pure `World.step` authority kernel with opaque issued references,
  explicit target and presented Capability, narrowing delegation, trusted time, and redacted audits;
- canonical versioned Shapes, authored Fragments, explicit projection, Operations, Constraints,
  Capabilities, attenuation, Outcomes, Events, and provenance;
- native Cooling, Event Distribution, and Flow semantics;
- isolated Enrichment and Architecture 0.5 Composition experiments;
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
