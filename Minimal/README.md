# Brontide Minimal Stack

Brontide Minimal Stack is an independent F# implementation of the Brontide Architecture 0.5 model. It lives beside
Brontide Reference Stack, but does not reference Brontide Reference Stack assemblies or reuse Brontide Reference Stack CLR types. The two implementations
are meant to support, challenge, and eventually substitute for one another through the explicit
external binding seam.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../Brontide-Interchange-Implementation-Plan-0.1.md).
Its first proof is implemented: Brontide Reference Stack-hosts-Brontide Minimal Stack and Brontide Minimal Stack-hosts-Brontide Reference Stack Cooling both execute
across real process boundaries under the same neutral contract and acceptance matrix.

The implementation currently provides:

- an immutable `World` and pure `World.step` authority kernel;
- canonical versioned Shapes, authored Fragments, explicit projection, Operations, Constraints,
  Capabilities, attenuation, Outcomes, Events, and provenance;
- native Cooling, Event Distribution, and Flow semantics;
- isolated Enrichment and Architecture 0.5 Composition experiments;
- deterministic CPU imaging, boxed application boundaries, provider opposition and selection
  explanations, and visible optimisation eligibility;
- a tagged JSON ShapeValue codec and versioned external manifest negotiation;
- an independently implemented version-2 portable-binding experiment, host adapter, and provider
  endpoint with structured operational observations;
- a headless host and five F# test assemblies.

There is deliberately no `global.json`. Brontide Minimal Stack targets .NET 10 and uses the SDK selected by the
calling environment. The current preview SDK does not copy its bundled `FSharp.Core` runtime into
application outputs, so `Directory.Build.props` copies that runtime from `MSBuildToolsPath`; this is
not a version pin.

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

See `docs/milestone-evidence.md` for the implemented first boundary and the Event/Flow, Macro
Operation, mixed-image-workspace, machine, and authority-federation work intentionally deferred.
