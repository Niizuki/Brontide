# Linen

Linen is an independent F# implementation of the Atlas Architecture 0.5 model. It lives beside
Fabric, but does not reference Fabric assemblies or reuse Fabric CLR types. The two implementations
are meant to support, challenge, and eventually substitute for one another through the explicit
external binding seam.

The implementation currently provides:

- an immutable `World` and pure `World.step` authority kernel;
- canonical versioned Shapes, authored Fragments, explicit projection, Operations, Constraints,
  Capabilities, attenuation, Outcomes, Events, and provenance;
- native Cooling, Event Distribution, and Flow semantics;
- isolated Enrichment and Architecture 0.5 Composition experiments;
- deterministic CPU imaging, boxed application boundaries, provider opposition and selection
  explanations, and visible optimisation eligibility;
- a tagged JSON ShapeValue codec and versioned external manifest negotiation;
- a headless host and five F# test assemblies.

There is deliberately no `global.json`. Linen targets .NET 10 and uses the SDK selected by the
calling environment. The current preview SDK does not copy its bundled `FSharp.Core` runtime into
application outputs, so `Directory.Build.props` copies that runtime from `MSBuildToolsPath`; this is
not a version pin.

## Run

```powershell
dotnet build .\Linen.slnx -nologo
dotnet test .\Linen.slnx -nologo --no-build
dotnet run --project .\src\Linen.Host\Linen.Host.fsproj -nologo
.\build\verify-boundaries.ps1
```

See `docs/milestone-evidence.md` for the implemented boundary and the work intentionally deferred
until the Fabric/Linen entanglement experiment.
