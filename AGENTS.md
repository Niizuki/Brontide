# Brontide agent instructions

Brontide is an architecture specification with two deliberately independent .NET 10 implementations:

- `Reference/` is the C#/Avalonia implementation and interactive showcase.
- `Minimal/` is the F# implementation and headless counterpoint.

The implementations should support, challenge, and eventually substitute for one another without
collapsing into the same codebase. Architecture decisions and implementation claims must remain
honest about which behaviour is normative, experimental, implemented, or deferred.

## Architecture and implementation targets

Use `Brontide-Architecture-Status.json` to locate the current architecture and latest ratified
architecture. Do not infer either from filenames or the highest version number.

Implementation targets are local and deliberately simpler. Each stack README or focused
implementation document states `Designed for: Brontide Architecture <version>`. Read that target,
the document's status and limitations, and then executable tests. Plans, notes, requirement
inventories, and matrices may provide detail, but none forms a mandatory routing hierarchy or
changes the locally stated target.

Use earlier architecture documents for the semantics of work designed against an older version and
for historical context; do not silently project later draft rules into an older implementation.

The implementation-correction programme is closed. Its permanent status, completion report, and
independent-review records preserve the closure evidence; do not recreate or treat the deleted
temporary plan as active authority. Keep provisional or non-ratified work in explicitly
experimental projects and do not present it as Brontide Base conformance.

## Ground rules

- **Define capability before surface.** Before creating public types, packages, or hosts for a new
  feature, write a short behavioural contract (`C1` through `Cn`) that states the observable
  capability, failure behavior, and evidence required. Preserve that capability when translating
  between stacks. If the source design is racy, leaky, or coupled to private machinery, redesign the
  realization instead of preserving the defect.
- **Keep Brontide Reference Stack and Brontide Minimal Stack independent.** Neither implementation may reference the other's projects,
  assemblies, private CLR types, dependency-injection container, or exceptions. Cross-stack work
  uses explicit external manifests, versioned data contracts, Shape projection, and process
  boundaries. Implement a concept natively on each side rather than adding an in-process
  compatibility layer.
- **Test relationships through neutral seams.** Family-level and cross-stack tests may connect
  independently implemented components through external manifests, versioned contracts, fixtures,
  process boundaries, or other neutral seams. Such tests prove interconnection; they do not permit
  one implementation to depend on the other's private runtime.
- **Preserve dependency direction.** `Brontide.Reference.Core` has no project dependency; Brontide Reference Stack extensions,
  vocabularies, and experiments depend only on Core; Studio is the composition root. `Brontide.Minimal.Model`
  has no project dependency; `Brontide.Minimal.Kernel` depends only on Model; extensions, vocabularies,
  experiments, and Binding stay outside Model/Kernel; Host is the composition root.
- **Base stays small.** Host services, UI concerns, persistence, transport, provider selection,
  acceleration, and experimental composition do not belong in Brontide Reference Stack Core or Brontide Minimal Stack Model/Kernel.
- **Prefer strongly typed identifiers.** Public surfaces take and return a distinct identifier type
  for each identity space rather than a bare string, number, or universally shaped identifier. An
  Actor id, Capability id, Shape id, Operation id, Execution id, Occurrence id, Activity id,
  Fragment id, external item id, collaboration id, and version id remain different types even when
  backed by the same primitive, so mixing them is a type error rather than a silent bug. Back each
  identifier with the primitive its source actually uses; do not invent a different representation
  for convenience. Keep the bare primitive at parsing, serialization, storage, or external-system
  seams and unwrap it only there. Skip a dedicated type only for a genuinely polymorphic or
  throwaway handle, and document why the exception is reasonable.
- **Fail closed at authority boundaries.** Unknown actors, capabilities, constraints, Shapes,
  providers, or operations must produce visible denial/error results before effects occur. Do not
  infer authority from delivery, possession, provider availability, or structural similarity.
- **Keep transitions deterministic where claimed.** Core/kernel logic receives time, providers,
  handlers, and external observations explicitly. Avoid ambient clocks, hidden service lookup,
  mutable global state, and nondeterministic enumeration in semantic decisions.
- **Centralize dependency versions.** Each build boundary has one authoritative dependency-version
  manifest; individual project descriptors name dependencies without repeating versions. Keep
  project graphs acyclic.
- **Warnings are errors.** Compiler and analyzer warnings fail the build repository-wide. Do not
  suppress a warning merely to make a gate pass; fix it or document why a narrowly scoped
  suppression is correct.
- **Do not add an SDK pin.** The repository intentionally has no `global.json`. Target .NET 10 and
  use the SDK selected by the environment. Brontide Minimal Stack's `MSBuildToolsPath` copy of the selected SDK's
  `FSharp.Core.dll` is a runtime-output workaround, not permission to pin a version or path.
- **Tests accompany behaviour.** Add or update the nearest native test suite for semantic changes.
  Keep normative conformance evidence separate from Enrichment, Composition, GPU, and other
  explicitly experimental evidence.
- **Automated attestations are valid independent review.** An automated reviewer counts as
  independent when it has a reviewer identity distinct from every implementation actor, runs in a
  fresh isolated context, has no access to the implementation session's private reasoning, and
  records a decision and rationale for every pinned requirement. Every attestation also reviews
  the current architecture selected by the status registry, including its status, and the
  implementation's locally stated target and limitations. A retained older matrix may establish what is
  implemented, but it never limits which architecture the review must consider. This rule remains
  in force unless the status registry or an explicit repository review policy changes it.
- **Every independently consumable component owns its verification stack.** New components ship
  unit tests with their first public behaviour. When code is extracted or moved, translate and move
  its existing test estate with it rather than leaving verification behind. Integration components
  additionally provide explicit live-probe/end-to-end coverage and, when useful, a non-interactive
  test console or example host under `tests/<Component>.TestConsole` with one verb per capability.
  Live checks must skip themselves when credentials are absent, remain outside ordinary CI, use
  dedicated sandbox resources, and fail with a non-zero exit code. The owning README documents the
  quick reference from day one; add a task-oriented `docs/integration-guide.md`, beginning with a
  short rules summary for coding agents, as the surface grows. Register every test console in this
  file so it is discoverable.
- **Treat public API changes as breaking-change decisions.** Describe the affected consumers and
  migration path explicitly. For an independently versioned component, update its `CHANGELOG.md`
  and bump only that component's version in the same change; do not bump untouched components. Mark
  a breaking commit or PR title with `!` and include a
  `BREAKING CHANGE: <what changed and how to migrate>` footer. This breaking-change marker is the
  one required title convention even though ordinary branch and PR naming is intentionally relaxed.
- **GPU execution is experimental and sideline-only.** CPU execution is the reference path. GPU
  work cannot count as completion of Base, Composition, Imaging, or mixed-stack milestones and must
  expose eligibility, lowering, buffers, copies, dispatch, failures, and fallback.
- **Preserve user work.** The worktree may already contain unrelated changes. Do not rewrite,
  discard, stage, or commit them. Avoid destructive git commands and keep edits scoped to the
  requested implementation.
- **Write comments for intent.** Code comments explain invariants, surprising tradeoffs, and why a
  design is safe. Do not narrate a port, duplicate commit history, or embed tracker references in
  source comments; keep provenance in changelogs, plans, and commit history.

## Implementation-specific conventions

### .NET-wide

- Use Central Package Management. Versions live only in the nearest owning
  `Directory.Packages.props`; `.csproj` and `.fsproj` files use
  `<PackageReference Include="..." />` without a `Version` attribute or child element.
- Set `TreatWarningsAsErrors` for every production, host, tool, and test project through the owning
  build props. Keep nullable analysis and relevant analyzers enabled; narrowly scoped suppressions
  require an explanatory comment or documented rationale.
- Tests use NUnit. Credentialed or live fixtures use `[Explicit]` plus a clear category and perform
  their own missing-credential skip. They are never part of the default CI test run.
- Test-console/example projects are hosts, not assertion libraries: compose the public component as
  a real consumer would, keep commands non-interactive, print plain-text diagnostics, and return a
  non-zero exit code on failure.

### Brontide Reference Stack / C#

- Represent public identity spaces with dedicated value types, normally immutable `readonly record
  struct` values or an existing local strongly-typed-id abstraction. Keep construction validation
  close to the type and expose the backing primitive only at serialization and external gateways.
- Keep C# package references versionless and let `Reference/Directory.Packages.props` own all NuGet
  versions. `Reference/Directory.Build.props` owns the warning-as-error policy.

### Brontide Minimal Stack / F#

- Represent identity spaces with distinct immutable types, normally private single-case unions,
  opaque records, or struct records with controlled construction. Issuer-controlled references must
  not expose a public construction path that bypasses validation.
- Keep F# package references versionless and let `Minimal/Directory.Packages.props` own all NuGet
  versions. `Minimal/Directory.Build.props` owns the warning-as-error policy.

## Documentation

- Keep documentation self-contained; do not depend on reasoning that lives only in another repo or
  chat. External code may be mentioned for comparison, but Brontide decisions belong here.
- **Documentation cleanup is default completion work.** Whenever documentation is created, edited,
  superseded, or changes implementation status, classify and place it correctly, move safely
  unpinned material, repair all Markdown and plain-text path references, and update the relevant
  indexes in the same change. Do this without waiting for a separate cleanup request.
- Use [`docs/README.md`](docs/README.md) as the authoritative map and keep its four classifications
  distinct:
  - `docs/current/` contains implemented behavior, currently used implementation targets, and
    operational policy;
  - `docs/future/` contains planned, draft, proposed, work-in-progress, or otherwise unimplemented
    work;
  - `docs/temporary/` contains deletion-gated execution notes; and
  - `docs/archive/` contains completed or superseded work.
- A partially implemented plan remains under `future` and states both the implemented subset and
  what remains. When all planned work is complete, move the plan to `archive` and move lasting
  operational guidance or evidence to `current` or the owning implementation.
- Keep the repository root limited to standard project-control files, `README.md`, `AGENTS.md`, and
  `Brontide-Architecture-Status.json`. Repository-wide Markdown belongs under `docs/`;
  implementation-owned documentation belongs under `Reference/` or `Minimal/`.
- Direct or transitive evidence pins are the only root-placement exception. Do not move such a file
  during ordinary cleanup, create a redirect stub, or invalidate a closed evidence trail. Record the
  blocked move in the
  [`Priority 0 relocation plan`](docs/future/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md)
  and preserve the stable path until the user authorizes evidence repinning and fresh review.
- Before beginning any other planned implementation work, inspect [`docs/future/README.md`](docs/future/README.md).
  The Priority 0 pinned-document relocation precedes other future work when its required
  repinning/review window is authorized. If authorization is absent, report the dependency and
  preserve the pinned files rather than silently bypassing it.
- Archive Architecture 0.5 and earlier work under `docs/archive/foundation/`; archive later work by
  area rather than by date.
- Implementation plans end with `## Open questions (owners needed)` containing only unresolved
  decisions and named owners, followed by `## Resolved questions` containing dated rulings. When a
  question is decided, move it to the resolved section instead of annotating it in place.
- Finalizing an architecture document version includes a plan for the next version. Before a
  version is declared complete, its changelog section must carry a "Direction for <next version>"
  passage naming what that version chases, in priority order, and its explicit non-goals (the
  latest architecture document's changelog shows the pattern).
- Update the affected implementation's `README.md`, `milestone-evidence.md`,
  `implementation-findings.md`, or `experimental-and-sideline-projects.md` when a change alters a
  claimed milestone, architectural boundary, known limitation, or experiment status.
- Record the difference between local/native evidence and actual cross-stack interoperability. A
  local fixture that simulates an external runtime is not Brontide Reference Stack ↔ Brontide Minimal Stack proof.
- Keep implementation-owned docs with their implementation. Put repository-wide architectural
  material in the root `docs/` tree when no single implementation owns it.
- If ADRs are introduced, use one self-contained `ADR-<topic>.md` per decision with `Date` and
  `Status` headers. Do not number or silently rewrite superseded decisions.
- When guidance from another repository is supplied for possible adoption, treat it as design input,
  not as authority. Import only project-neutral practices that fit Brontide, translate examples and
  paths, and omit foreign packages, services, CI conventions, and naming schemes.

## Build and test

Run commands from the repository root unless a section says otherwise.

Brontide:

```powershell
dotnet restore .\Reference\Brontide.Reference.sln
dotnet build .\Reference\Brontide.Reference.sln --no-restore
dotnet test .\Reference\Brontide.Reference.sln --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1
```

Brontide:

```powershell
dotnet restore .\Minimal\Brontide.Minimal.slnx
dotnet build .\Minimal\Brontide.Minimal.slnx --no-restore
dotnet test .\Minimal\Brontide.Minimal.slnx --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1
```

Scope verification to the changed implementation or project when the dependency boundary is clear.
Run the complete implementation suite when changing shared build files, solution files, Core,
Model, Kernel, public semantic contracts, or project references, and whenever the impact is
uncertain. Changes spanning Brontide Reference Stack and Brontide Minimal Stack require both suites and both dependency guards.

When finalizing repository-wide work for review or delivery, run the complete repository gate
(`.\build\verify-interchange.ps1`), fix both blocking and non-blocking findings that are in scope,
update current documentation, and then obtain a fresh-context review where the active evidence or
review policy requires one. Apply this finalization discipline at the end of the work; do not turn
ordinary work-in-progress iterations into repeated full-gate or review requests.

Tests should be hermetic by default. Do not call production systems or require live credentials in
ordinary test runs. Any future live probe must be explicit, credential-gated, safe for a dedicated
sandbox, and excluded from CI by default.

## Registered integration test consoles

No credentialed integration test consoles are currently registered. When one is added, list its
project path, offline check command, supported live verbs, configuration source, and permitted
sandbox target here in the same change.

## Git, branches, and pull requests

- Do not create a branch, pull request, or task record unless the user asks for one or the active
  workflow requires it.
- There is no task/ticket naming scheme. Do not invent task identifiers, issue numbers, lane names,
  or mandatory prefixes.
- When a branch is useful, choose a short descriptive name. A plain name such as `Brontide Minimal Stack-binding` or
  `docs-agent-guidance` is fine; follow an explicitly requested name when one is given.
- Commit subjects should be concise, lowercase-imperative, have no trailing period, and describe the change. Conventional Commit form is welcome
  (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `build:`, `chore:`), but a scope is optional and
  no tracker reference is expected. A breaking change is the exception: use
  `type(optional-scope)!: summary` and the required `BREAKING CHANGE:` migration footer.
- PR titles may be plain descriptive summaries or Conventional Commit titles. They do not require a
  task number, branch name, scope, or special prefix. Accuracy about the whole branch matters more
  than format. A PR containing a breaking public API change must use the Conventional Commit `!`
  marker described above.
- Judge PR title/description quality when the branch is being finalized for review or merge, not
  during active work. If stale or misleading, explain the issue and offer a correction; do not
  silently rewrite it.
- Before pushing or merging, verify the relevant suite and dependency guard, check the final diff,
  and report any deliberately deferred milestone work. Prefer fast-forward merges when history
  permits and never overwrite a concurrently advanced remote branch.
