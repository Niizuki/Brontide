# Brontide Reference Stack 0.2

Brontide Reference Stack is the independent .NET 10 / Avalonia implementation and showcase.
Architecture identity, ratification status, the retained executable baseline, and Reference's
current delivery plan, ledger, and matrix are selected only through
[`../Brontide-Architecture-Status.json`](../Brontide-Architecture-Status.json). A delivery plan does
not upgrade an implementation claim until its evidence is accepted. Current gate evidence,
experiment limits, sideline-project status, and the
one historical limitation are recorded in `docs/milestone-evidence.md`,
`docs/implementation-findings.md`, and `docs/experimental-and-sideline-projects.md`.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../Brontide-Interchange-Implementation-Plan-0.1.md).
Brontide Reference Stack now owns independently implemented experimental hosts and provider endpoints for the
process-isolated Cooling and Catalog/resource proofs. The retained tests execute a real Brontide Minimal Stack provider process; no
Brontide Reference Stack project references Brontide Minimal Stack assemblies or private types.

The registry-selected current architecture does not ratify Component descriptors, system
service discovery, execution explanation, or optimisation-property vocabularies. Their Brontide Reference Stack
realisations therefore live in `Brontide.Reference.Experimental.Composition`, not `Brontide.Reference.Core` or normative
conformance.

## Build and test

The repository deliberately has no `global.json`; [`../docs/sdk-policy.md`](../docs/sdk-policy.md)
defines and continuously checks the supported .NET 10 SDK range and CI feature bands.
`Directory.Build.props` selects C# 14.
NuGet versions use Central Package Management in `Directory.Packages.props`; warnings are errors
solution-wide.

```powershell
dotnet restore .\Brontide.Reference.sln
dotnet build .\Brontide.Reference.sln --no-restore
dotnet test .\Brontide.Reference.sln --no-build
.\build\verify-dependencies.ps1
```

The ordinary solution test run executes fixture and boundary tests and skips the foreign-process
cases unless `BRONTIDE_MINIMAL_PROVIDER` names a built endpoint. Run the complete two-way clean gate,
including both real foreign processes, from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

See [`docs/integration-guide.md`](./docs/integration-guide.md) for the binding quick reference.
See [`../docs/public-boundaries.md`](../docs/public-boundaries.md) for payload, timeout, cleanup,
redaction, replay, and denial-of-service assumptions.

Tests use NUnit 4, the NUnit adapter and analyzers, plus NSubstitute for collaboration boundaries.
The Enrichment and Architecture 0.5 composition tests are marked `Experimental` and are deliberately
separate from normative Brontide conformance.

## Experimental and sideline projects

GPU execution is a planned experimental sideline project, separate from the completed
`System.Numerics` vector evidence. It must preserve the same semantic Operation while exposing GPU
eligibility, compilation, buffers, copies, dispatch, failures, and fallback. It is not required by
the current Brontide Reference Stack showcase and is not represented as completed work. See
`docs/experimental-and-sideline-projects.md`.

## Run Studio

```powershell
dotnet run --project .\src\Brontide.Reference.Studio\Brontide.Reference.Studio.csproj
```

Brontide Reference Stack Studio opens on the virtual-device board. Its actions expose:

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

`Brontide.Reference.Core` has no project dependency. Extensions, vocabularies, and experimental projects
reference only Core. Studio composes all projects and is referenced only by its test project. The
experimental provider endpoint composes vocabulary and binding projects without becoming Studio.
The dependency verifier also rejects Brontide Minimal Stack project references and foreign Brontide Minimal Stack assemblies in
Brontide Reference Stack outputs.
