# Fabric 0.2

Fabric is the .NET 10 / Avalonia implementation and showcase of Atlas Architecture 0.5. The
solution retains the functional scope of milestones M0 through M9 from the Architecture 0.4
`Fabric-Implementation-Plan-0.2.md` and adds isolated experimental evidence for the Architecture
0.5 composition guardrails. Current gate evidence, experiment limits, sideline-project status, and
the one historical limitation are recorded in `docs/milestone-evidence.md`,
`docs/implementation-findings.md`, and `docs/experimental-and-sideline-projects.md`.

The current repository-wide programme is
[`Atlas-Interchange-Implementation-Plan-0.1.md`](../Atlas-Interchange-Implementation-Plan-0.1.md).
Fabric now owns an independently implemented experimental host adapter and provider endpoint for
the process-isolated Cooling proof. The retained tests execute a real Linen provider process; no
Fabric project references Linen assemblies or private types.

Architecture 0.5 does not change Atlas Base and does not ratify Component descriptors, system
service discovery, execution explanation, or optimisation-property vocabularies. Their Fabric
realisations therefore live in `Fabric.Experimental.Composition`, not `Fabric.Core` or normative
conformance.

## Build and test

The repository does not pin an SDK version or feature band. Use an installed .NET 10 SDK;
`Directory.Build.props` selects C# 14.
NuGet versions use Central Package Management in `Directory.Packages.props`; warnings are errors
solution-wide.

```powershell
dotnet restore .\Fabric.sln
dotnet build .\Fabric.sln --no-restore
dotnet test .\Fabric.sln --no-build
.\build\verify-dependencies.ps1
```

The ordinary solution test run executes fixture and boundary tests and skips the foreign-process
cases unless `ATLAS_LINEN_PROVIDER` names a built endpoint. Run the complete two-way clean gate,
including both real foreign processes, from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

See [`docs/integration-guide.md`](./docs/integration-guide.md) for the binding quick reference.

Tests use NUnit 4, the NUnit adapter and analyzers, plus NSubstitute for collaboration boundaries.
The Enrichment and Architecture 0.5 composition tests are marked `Experimental` and are deliberately
separate from normative Atlas conformance.

## Experimental and sideline projects

GPU execution is a planned experimental sideline project, separate from the completed
`System.Numerics` vector evidence. It must preserve the same semantic Operation while exposing GPU
eligibility, compilation, buffers, copies, dispatch, failures, and fallback. It is not required by
the current Fabric showcase and is not represented as completed work. See
`docs/experimental-and-sideline-projects.md`.

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
- capability-gated pointer Flow opening and Item publication, gap detection, and replay;
- an `Audit.Start` macro Operation that creates and later terminally completes an activity; and
- the Architecture 0.5 image workspace: a simple CPU composition, independently adopted system
  facilities, visible provider substitution, and the same semantic Operation selected onto a real
  `System.Numerics` vector path using explicit eligibility claims and operational observations.

## Dependency rule

`Fabric.Core` has no project dependency. Extensions, vocabularies, and experimental projects
reference only Core. Studio composes all projects and is referenced only by its test project. The
experimental provider endpoint composes vocabulary and binding projects without becoming Studio.
The dependency verifier also rejects Linen project references and foreign Linen assemblies in
Fabric outputs.
