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
- **Pin a found defect with a failing test before fixing it, and read the failure.** Whenever a
  defect is identified — a review finding, a failing gate, something noticed in passing — write the
  test first, run it, and confirm it fails *for the reason you claimed*. Only then fix. This is one
  half of the discipline; the other half, the same rule applied while implementing and before there
  is a defect to find, is the bullet below. Two things the order buys, and the first is what
  justifies it.
  - **It checks the diagnosis, not only the fix.** A reasoned-about defect is a hypothesis, and an
    executable assertion is the cheapest way to discover the reasoning was wrong: the failure
    message says whether the mechanism is the one you named. It also routinely finds the fault is
    *wider* than the report: PB6's resource observations turned out to claim both an acceptance and
    an integrity check that never happened, in the same fields the parity profile compares, so a fix
    aimed at whichever was noticed first would have left the other behind and looked complete. The
    opposite outcome is worth as much — a test that passes before the fix means the defect is not
    where you think, which is far better learned before editing semantic code than after.
  - **It is the regression guard, already written.** The test outlives the fix and is what stops the
    defect returning under a later refactor. A fix verified only by inspection leaves nothing behind.

  Where the defect is a *class* rather than one instance, pin the class: a guard that fails when the
  next member is added and left out beats three assertions naming today's three. Where a test
  genuinely cannot reach the defect — a compile-time surface, a declared category with no reachable
  path — say so and pin the nearest observable thing rather than manufacturing a path, as PB6 did
  for `peer-unavailable`.
- **Pin what you claim, as you write it: tests are part of the implementation, not a step after
  it.** The rule above catches a defect once someone has found it, and most of what it catches was
  already thought about while the code was written and simply never asserted. So the same discipline
  runs forward: as each piece of behaviour is written, its assertions are written *with* it and run.
  Not "implement, then cover" — that ordering is where the value leaks away, for two reasons. A test
  written against a finished implementation is written *from* it: it asserts what the code does
  rather than what it should do, which is how a suite ends up green over the wrong behaviour. And by
  the time the implementation is finished, the list of scenarios you considered along the way is
  gone — the retirement racing the gate, the sibling member exposing an Operation of the same name,
  the successor that resolves only part of an activation — and those are exactly the cases worth
  having. Three triggers, each a specific thing to write a test *for* rather than an exhortation to
  test more:
  - **A comment claiming behaviour under a named scenario is a test case.** If a remark says what
    happens when X, something should assert X; the thinking was already done and only the assertion
    is missing. This one is mechanically checkable at review time — read the comments and ask which
    are asserted.
  - **A load-bearing assertion is a test case** — a claim some *other* decision rests on, so that if
    it is false something else in the design is wrong rather than merely undocumented. This
    explicitly includes a claim about a dependency's behaviour, which is the class you cannot reason
    your way to certainty about and where a probe is cheap. Decision 11 is the worked example: the
    Binding Plan's provider fact was taken to name the provider that answered, six phases of work
    rested on it, and it actually named the one the host asked for — both stacks identically, and
    every fixture derived from one declaration, so nothing ever asked the question.
  - **Each capability-contract item (`C1` through `Cn`) gets a named test in the same change**,
    naming the item, alongside the property that item states. The contract is already the spec, so
    this makes "the contract is satisfied" something the suite answers rather than something the
    author believes.

  Two boundaries, because this rule fails worse than its absence does.
  - **A nameable trigger, or it is not a test.** Write it when you can state the concrete input,
    state, or sequence that provokes the case. If you cannot name one, it is a hypothetical: leave it
    as a comment or leave it out. Speculative tests over unspecified behaviour freeze whatever the
    code happens to do into a contract, and a later *correct* change then reads as a regression —
    which is the silence problem below arriving from the other side.
  - **You must have seen each test fail once.** Write it before the code path it covers, or break
    the implementation deliberately and watch it go red. A test that has only ever been green may be
    asserting nothing: CBI17's first draft asserted that the admissions and grants in force were
    unchanged across a succession, which no implementation could have failed, because both stacks
    hold them in immutable values. It was replaced by the provider effect count across the call,
    which a wrong implementation can move. A test that cannot fail is a finding against the test, the
    same way a property that cannot fail is a finding against the property.
- **Test the contract's silence, not only its cases.** Two implementations written from one contract
  by one reader diverge where that contract is *ambiguous* and agree wherever it is *silent*.
  Independent implementation therefore detects ambiguity and is structurally blind to silence: a
  defect the contract never spoke to appears identically on both sides, and every cross-stack
  comparison passes. It detects even that ambiguity only where a **shared vector forces both
  implementations to answer the same question** — the Catalog fixture's provider domain diverged
  across three implementations for four phases because no vector ever asked it anything. Two
  practices supplement this, and both are standing requirements rather than one-off responses.
  - **A property per capability.** Every behavioural contract (`C1` through `Cn`) states at least one
    property that must hold over *all* of its vectors — "what must be true of every failure path" —
    not only per-vector expectations. A property is a claim about every path, so it can fail where no
    single case was written. A property that cannot fail is a review finding against the property,
    not a reason to drop the practice.
  - **A contract-completeness review at each phase boundary.** A pass that asks what the contract
    does *not* say, per capability, kept separate from conformance review — which by construction can
    only check what was written down. It works from absence, which is a hard brief, and is the point.

  Recorded 2026-07-28 as Decision 10 in
  [`binding/portable/open-decisions.md`](binding/portable/open-decisions.md), after all three defects
  found by PB6 turned out to be present identically in both stacks, in fields the parity profile
  compares. Writing an implementation-neutral endpoint from the published artifacts before either
  stack implements a phase was considered and not adopted: it is the strongest safeguard and the most
  expensive, and the completeness review is expected to reach most of it at a fraction of the cost.
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
- **Report friction with these rules as evidence, not verdicts.** Every rule in this file was paid
  for by something breaking, which means a rule that is merely *ambiguous at the point of use*, or
  expensive relative to what it buys here, never gets revisited: it produces no defect, so nothing
  triggers a rewrite. An agent applying these rules across a session is the only party that sees that
  friction, and this is the only channel back.

  **"This rule is good" is worth nothing.** A model is biased toward agreeing with the repository it
  is working in, and it never sees the counterfactual — a defect a rule prevented is invisible by
  construction. So an entry describes a *situation*, never an opinion, and only when one of five
  triggers has fired: a rule you **could not apply without guessing** what it meant; two rules that
  **pointed different ways**; a rule that **cost real work for no benefit visible here**; a rule that
  **caught something you can name**; or an **approach that worked, is not in this file, and would
  generalise**. Nothing else — no end-of-task summaries, no "the instructions were clear".

  State the conclusion so that a reader who knows nothing about the change can use it, and name the
  concrete case as evidence, which is how the rest of these documents argue. Append to
  [`docs/current/ai-feedback/`](docs/current/ai-feedback/README.md) and **do not read the other
  entries first**: repetition across independent sessions is the signal the folder exists to
  produce, and an agent that had read an earlier entry would either skip its own or restate someone
  else's framing. That README carries the file convention, the sweep that records what was done
  about each entry, and where a swept month goes afterwards.

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
- Agent-feedback entries follow the same lifecycle rather than forming a fifth classification. The
  convention and the open months live under `docs/current/ai-feedback/`, because the convention is
  operational policy and an unswept month is live; a month whose report records what was done about
  each entry moves to `docs/archive/ai-feedback/` with the report beside it.
- Keep the repository root limited to standard project-control files, `README.md`, `AGENTS.md`, and
  `Brontide-Architecture-Status.json`. Repository-wide Markdown belongs under `docs/`;
  implementation-owned documentation belongs under `Reference/` or `Minimal/`.
- Documents carrying direct or transitive evidence pins now live at their classified `docs/` or
  `<stack>/docs/` paths, which are the stable evidence paths. Do not move or rewrite such a file
  during ordinary cleanup, create a redirect stub, or invalidate a closed evidence trail. Moving one
  requires explicit user authorization to repin the evidence and obtain fresh independent review, as
  the completed
  [`pinned documentation relocation`](docs/archive/documentation/Brontide-Pinned-Documentation-Relocation-Plan-0.1.md)
  did; report the blocked move and preserve the current path until that authorization exists.
- Before beginning any planned implementation work, inspect [`docs/future/README.md`](docs/future/README.md)
  for the current priority order and take the highest-priority item unless the user directs
  otherwise.
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

- **Do new work on a branch and open a pull request.** This is the default for implementation,
  documentation, and evidence work alike, so changes are reviewable before they reach `main`.
  Commit directly to `main` only when there is an explicit reason: the user asks for it, or the
  change is small and self-evident enough that review would add nothing. State which reason applied.
- Do not create a task record unless the user asks for one or the active workflow requires it.
- There is no task/ticket naming scheme. Do not invent task identifiers, issue numbers, lane names,
  or mandatory prefixes.
- Choose a short descriptive branch name. A plain name such as `Brontide Minimal Stack-binding` or
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
