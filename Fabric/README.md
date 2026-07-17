# Fabric 0.2

Fabric is the .NET 10 / Avalonia implementation and showcase of Atlas Architecture 0.4. The
solution implements the functional scope of milestones M0 through M9 from
`Fabric-Implementation-Plan-0.2.md`. Current gate evidence and the one historical limitation are
recorded in `docs/milestone-evidence.md`.

## Build and test

The SDK policy in `global.json` accepts stable .NET 10 feature bands from 10.0.100 onward, while
`Directory.Build.props` selects C# 14.
NuGet versions use Central Package Management in `Directory.Packages.props`; warnings are errors
solution-wide.

```powershell
dotnet restore .\Fabric.sln
dotnet build .\Fabric.sln --no-restore
dotnet test .\Fabric.sln --no-build
.\build\verify-dependencies.ps1
```

Tests use NUnit 4, the NUnit adapter and analyzers, plus NSubstitute for collaboration boundaries.
The Enrichment tests are marked `Experimental` and are deliberately separate from normative Atlas
conformance.

## Run Studio

```powershell
dotnet run --project .\src\Fabric.Studio\Fabric.Studio.csproj
```

Fabric Studio opens on the virtual-device board. Its actions expose:

- device attachment as a recorded Genesis occurrence;
- device-origin pointer input, denied malware injection, and authorised but unverified remote input;
- actor and capability graphs, derivation trees, and articulate denials;
- the §29.4 secure/weakened attack toggle;
- the headless Cooling scenario;
- capability-gated Event Distribution and derived-origin replay;
- capability-gated pointer Flow opening and Item publication, gap detection, and replay; and
- an `Audit.Start` macro Operation that creates and later terminally completes an activity.

## Dependency rule

`Fabric.Core` has no project dependency. Extensions, vocabularies, and the Enrichment experiment
reference only Core. Studio composes all projects and is referenced only by its test project. The
dependency verification script checks the mechanically enforceable part of this rule.
