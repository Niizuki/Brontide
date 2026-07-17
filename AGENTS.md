# Atlas agent instructions

Atlas is an architecture specification with two deliberately independent .NET 10 implementations:

- `Fabric/` is the C#/Avalonia implementation and interactive showcase.
- `Linen/` is the F# implementation and headless counterpoint.

The implementations should support, challenge, and eventually substitute for one another without
collapsing into the same codebase. Architecture decisions and implementation claims must remain
honest about which behaviour is normative, experimental, implemented, or deferred.

## Source of truth

Use this order when sources disagree:

1. `Atlas-Architecture-0.5.md` for current architectural semantics.
2. The relevant implementation plan and milestone-evidence documents.
3. Executable conformance tests and current code.
4. `Atlas-Architecture-0.4.md` only for historical context.

Architecture 0.5 does not ratify the experimental Component, discovery, execution-explanation, or
optimisation vocabularies. Keep such work in explicitly experimental projects and do not present it
as Atlas Base conformance.

## Ground rules

- **Keep Fabric and Linen independent.** Neither implementation may reference the other's projects,
  assemblies, private CLR types, dependency-injection container, or exceptions. Cross-stack work
  uses explicit external manifests, versioned data contracts, Shape projection, and process
  boundaries. Implement a concept natively on each side rather than adding an in-process
  compatibility layer.
- **Preserve dependency direction.** `Fabric.Core` has no project dependency; Fabric extensions,
  vocabularies, and experiments depend only on Core; Studio is the composition root. `Linen.Model`
  has no project dependency; `Linen.Kernel` depends only on Model; extensions, vocabularies,
  experiments, and Binding stay outside Model/Kernel; Host is the composition root.
- **Base stays small.** Host services, UI concerns, persistence, transport, provider selection,
  acceleration, and experimental composition do not belong in Fabric Core or Linen Model/Kernel.
- **Use strongly typed semantic references.** Actor, Capability, Shape, Operation, Execution,
  Occurrence, Activity, Fragment, and provider identities are distinct concepts even when their wire
  representations look alike. Keep bare strings, numbers, and GUIDs at parsing or external-system
  seams; do not let them erase distinctions in public or kernel-facing APIs.
- **Fail closed at authority boundaries.** Unknown actors, capabilities, constraints, Shapes,
  providers, or operations must produce visible denial/error results before effects occur. Do not
  infer authority from delivery, possession, provider availability, or structural similarity.
- **Keep transitions deterministic where claimed.** Core/kernel logic receives time, providers,
  handlers, and external observations explicitly. Avoid ambient clocks, hidden service lookup,
  mutable global state, and nondeterministic enumeration in semantic decisions.
- **Centralize package versions.** Each implementation owns a `Directory.Packages.props`; project
  files carry versionless `PackageReference` items. Keep project graphs acyclic.
- **Warnings are errors.** Maintain a clean build for both implementations. Do not suppress a
  warning merely to make a gate pass; fix it or document why a narrowly scoped suppression is
  correct.
- **Do not add an SDK pin.** The repository intentionally has no `global.json`. Target .NET 10 and
  use the SDK selected by the environment. Linen's `MSBuildToolsPath` copy of the selected SDK's
  `FSharp.Core.dll` is a runtime-output workaround, not permission to pin a version or path.
- **Tests accompany behaviour.** Add or update the nearest native test suite for semantic changes.
  Keep normative conformance evidence separate from Enrichment, Composition, GPU, and other
  explicitly experimental evidence.
- **GPU execution is experimental and sideline-only.** CPU execution is the reference path. GPU
  work cannot count as completion of Base, Composition, Imaging, or mixed-stack milestones and must
  expose eligibility, lowering, buffers, copies, dispatch, failures, and fallback.
- **Preserve user work.** The worktree may already contain unrelated changes. Do not rewrite,
  discard, stage, or commit them. Avoid destructive git commands and keep edits scoped to the
  requested implementation.

## Documentation

- Keep documentation self-contained; do not depend on reasoning that lives only in another repo or
  chat. External code may be mentioned for comparison, but Atlas decisions belong here.
- Update the affected implementation's `README.md`, `milestone-evidence.md`,
  `implementation-findings.md`, or `experimental-and-sideline-projects.md` when a change alters a
  claimed milestone, architectural boundary, known limitation, or experiment status.
- Record the difference between local/native evidence and actual cross-stack interoperability. A
  local fixture that simulates an external runtime is not Fabric ↔ Linen proof.
- Keep implementation-owned docs with their implementation. Put repository-wide architectural
  material at the root or in a future root `docs/` tree when no single implementation owns it.
- If ADRs are introduced, use one self-contained `ADR-<topic>.md` per decision with `Date` and
  `Status` headers. Do not number or silently rewrite superseded decisions.

## Build and test

Run commands from the repository root unless a section says otherwise.

Fabric:

```powershell
dotnet restore .\Fabric\Fabric.sln
dotnet build .\Fabric\Fabric.sln --no-restore
dotnet test .\Fabric\Fabric.sln --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Fabric\build\verify-dependencies.ps1
```

Linen:

```powershell
dotnet restore .\Linen\Linen.slnx
dotnet build .\Linen\Linen.slnx --no-restore
dotnet test .\Linen\Linen.slnx --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Linen\build\verify-boundaries.ps1
```

Scope verification to the changed implementation or project when the dependency boundary is clear.
Run the complete implementation suite when changing shared build files, solution files, Core,
Model, Kernel, public semantic contracts, or project references, and whenever the impact is
uncertain. Changes spanning Fabric and Linen require both suites and both dependency guards.

Tests should be hermetic by default. Do not call production systems or require live credentials in
ordinary test runs. Any future live probe must be explicit, credential-gated, safe for a dedicated
sandbox, and excluded from CI by default.

## Git, branches, and pull requests

- Do not create a branch, pull request, or task record unless the user asks for one or the active
  workflow requires it.
- There is no task/ticket naming scheme. Do not invent task identifiers, issue numbers, lane names,
  or mandatory prefixes.
- When a branch is useful, choose a short descriptive name. A plain name such as `linen-binding` or
  `docs-agent-guidance` is fine; follow an explicitly requested name when one is given.
- Commit subjects should be concise and describe the change. Conventional Commit form is welcome
  (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `build:`, `chore:`), but a scope is optional and
  no tracker reference is expected. Mark genuine breaking changes clearly in the subject/body.
- PR titles may be plain descriptive summaries or Conventional Commit titles. They do not require a
  task number, branch name, scope, or special prefix. Accuracy about the whole branch matters more
  than format.
- Judge PR title/description quality when the branch is being finalized for review or merge, not
  during active work. If stale or misleading, explain the issue and offer a correction; do not
  silently rewrite it.
- Before pushing or merging, verify the relevant suite and dependency guard, check the final diff,
  and report any deliberately deferred milestone work. Prefer fast-forward merges when history
  permits and never overwrite a concurrently advanced remote branch.
