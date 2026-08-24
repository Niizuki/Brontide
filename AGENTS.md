# Brontide — agent instructions

Brontide is an architecture specification with two deliberately independent .NET 10
implementations:

- `Reference/` is the C#/Avalonia implementation and interactive showcase.
- `Minimal/` is the F# implementation and headless counterpoint.

The implementations support, challenge, and eventually substitute for one another without
collapsing into the same codebase. `Brontide-Architecture-Status.json` selects the current and latest
ratified architecture, and `docs/README.md` maps the documentation authorities. This file carries
working rules and does not restate their current status.

## How to read this file

- **Every rule here was paid for by something breaking**, and this file is loaded into every session,
  so length is a real cost. Keep one clause of why here; the full argument belongs in the decision,
  plan, or policy that owns it.
- **Where prose and a guard disagree, the guard describes what is actually enforced.** Report and
  reconcile the discrepancy; neither working prose nor a stale guard can overrule the architecture or
  evidence authority that owns the claim.
- **Everything above `## This repository` is portable doctrine.** Repository-specific paths,
  commands, architecture boundaries, platform quirks, and deliberate exceptions belong below it.
- **These rules deliberately trade short-term speed for long-term cost.** Report concrete friction
  under *Report friction with these rules* rather than quietly skipping a rule.
- **Editing this file has its own rules.** The appendix binds edits to this file just as the rest of
  the file binds edits to the repository.

---

## 1. Working discipline

### Tests are part of the implementation, not a step after it

Write and run assertions with each piece of behaviour. A test written against finished code is easily
written *from* that code rather than from the intended contract, and the scenarios considered during
implementation are usually the ones lost by the end.

Three concrete triggers each require a test:

- **A comment claiming behaviour under a named scenario.** The thinking is already done; only the
  assertion is missing. Review comments mechanically by asking which claims are tested.
- **A load-bearing assertion** — a claim another decision rests on. This includes claims about a
  dependency's behaviour, where a small probe is more reliable than reasoning from expectation.
- **Each capability-contract item (`C1..Cn`).** Give each item a named test in the same change so the
  suite, rather than the author, answers whether the contract is satisfied.

Two boundaries keep this rule from freezing accidents:

- **A nameable trigger, or it is not a test.** State the input, state, or sequence that provokes the
  case. If none can be named, leave it as a hypothesis rather than specifying incidental behaviour.
- **See every new test fail once.** Write it before the path it covers, or deliberately break that
  path and observe the failure. A test that has only been green may assert nothing; rewrite an
  assertion whose observable surface cannot distinguish right from wrong.

Never make a suite green by weakening, skipping, or deleting a valid test.

### Pin a found defect before fixing it — and read the failure

Whenever a defect is identified, write the regression test first, run it, and confirm it fails for
the claimed reason. A defect is a hypothesis until the failure checks the diagnosis; a test that
passes before the fix says the fault is not where claimed. The same test then becomes the permanent
regression guard.

Where the defect is a class rather than one instance, pin the class: a convention or reflection guard
that fails when the next member is omitted is stronger than assertions naming only today's members.
Where a test genuinely cannot reach the defect — for example, a compile-time surface — state that and
pin the nearest observable behaviour instead of manufacturing a runtime path.

Close findings as they are discovered when the task authorizes fixes. Report-only is the exception:
the user requested review without changes, the correction requires their design decision, formal
review policy separates attestation from implementation, or the finding is out of scope. State which
findings were closed, which were left, and why.

### Pinning during concurrent discovery

The pin must run at discovery, but it must not land under another reader's feet. Concurrent reads and
writes on one tree create false findings as lines and quoted behaviour move.

- An agent that can run code writes and runs the pin in its own worktree or scratch when another
  reader is active, then reports the test source and observed failure.
- An agent that cannot write or run code reports a **labelled draft pin**: the owning fixture, named
  trigger, setup, and assertion. An unrun test is a second hypothesis and must be identified as such.
- The pin lands with the fix. Whoever lands it runs it against the real tree and owns the requirement
  to have seen it fail.

Give concurrent writers separate trees, or route findings that touch the same file to one writer. Do
not open a new reading pass until the prior writes have landed.

### Review in fresh passes

A pass sharing the authoring context mostly re-reads the author's intent. Use several focused passes,
including a fresh-context read of the whole diff. Where a separate review decides branch readiness,
run it in a new conversation or other context meeting the repository's independence policy.

Give each reviewer a claim to falsify, not an open-ended hunt. Run two kinds of brief in this order:

1. **What the work asserts.** Ask cold, without the author's suspicions, so the reviewer forms an
   independent view.
2. **What the reasoning asserts.** Provide the reasoning, suspected weakness, and rejected
   alternative, then ask the reviewer to break them.

A review climbs the pinning ladder as far as its environment allows: an executed failing test, a
labelled draft pin, or at minimum a concrete trigger. A returned finding is verified rather than
queued as a question. Red for the stated reason confirms it; green means the pin did not reach the
claimed mechanism, so try one second concrete pin before closing it as unconfirmed.

Independent reviewers agreeing is evidence; a lone reviewer is a lead. Note what no brief covered,
because a clean report over an unasked question proves nothing. After corrections, review the changed
lines again: they are now the least-reviewed part of the branch.

Stop when findings converge to preferences and nice-to-haves. Use a stated cycle ceiling only as a
backstop, report whether convergence or the ceiling stopped the loop, and never call a capped review
converged. Recommend another pass only when you can name what it would reach that the last pass could
not.

These practices govern ordinary review. Formal attestations must also meet the repository-local
identity, isolation, evidence-pin, and policy requirements below.

### Gate what the change can break

While working, run the narrowest gate the diff could plausibly fail and state that scope. A change to
no compiled file cannot break compiled behaviour, but it can break documentation, text, schema, or
evidence guards. Widen when shared build files, project graphs, public contracts, or widely consumed
components change, or when the affected reach is uncertain.

At finalization, run the full gate required for the change's declared scope; repository-wide
finalization uses the repository's complete gate. Tests are hermetic by default: ordinary runs do not
call production systems or require live credentials.

### Finalization

Judge readiness at finalization rather than during active work. When the user asks whether work is
ready, to open a pull request, or to merge:

- Run the gates CI or repository policy requires at full scope.
- Fix in-scope blocking and non-blocking defects, keep documentation current, and read the final diff.
- Judge the pull-request title and description against the whole branch. If either is stale or
  misleading, state what is wrong and offer a correction; never silently rewrite outward-facing text.

### Shortcuts and scope

- **Remove the cause by default.** A release in flight, a dependency outside the task, or a fix that
  would widen the diff beyond reviewable scope may justify a workaround; speed alone does not.
- **Name the cost of a shortcut.** Record the workaround, unmigrated call sites, uncovered case, or
  condition that would remove it so it cannot masquerade as finished work.
- **Give a workaround a retirement signal** where possible: an assertion that changes when the
  dependency defect or external condition is gone.
- **Maintainability does not authorize unrelated work.** Deliver the requested scope; report adjacent
  work separately so the control provided by review remains effective.

---

## 2. Design defaults

- **Prefer a strongly typed identifier over a primitive.** Use one type per identity space so mixing
  identities is a type error. Back it with the source primitive, keep that primitive at serialization,
  storage, parsing, and external-system seams, and skip a type only for a genuinely polymorphic or
  throwaway handle with a documented reason.
- **Define capability before surface.** Before public types, packages, or hosts, write a short
  behavioural contract (`C1..Cn`) stating observable capability, failure behaviour, and required
  evidence. Then build the smallest surface that satisfies it.
- **Preserve dependency direction and keep the core small.** Host concerns, UI, persistence,
  transport, and experimental composition stay outside the innermost semantic layer. Keep project
  graphs acyclic.
- **Fail closed at authority boundaries.** Unknown actors, capabilities, constraints, Shapes, records,
  providers, or operations produce a visible denial before effects. Never infer authority from
  delivery, possession, availability, or structural similarity.
- **Keep transitions deterministic where claimed.** Semantic logic receives time, providers,
  handlers, and external observations explicitly; avoid ambient clocks, hidden service lookup,
  mutable global state, and nondeterministic enumeration in decisions.
- **Expected outcomes are values, not exceptions.** Represent not-found, refused, invalid, and other
  anticipated semantic results so callers must branch on them. A boundary whose ecosystem requires
  exceptions documents and translates that idiom rather than mixing both styles inside one workflow.
- **A guard that fails open is invisible until refusal is asserted.** Test both the accepted and
  refused case for a rule of the form “X is refused when Y.”
- **Stored state must affect behaviour.** Before relying on a lifecycle or status value as enforcement,
  identify and test the branch that acts on it; a value carried only into output or documentation is
  not a guard.
- **Recoverable substance should not be terminal in form.** If entering a state destroys nothing,
  provide an exit; irreversible form over reversible effects trains users to avoid the state.
- **Warnings are errors** repository-wide, tests included. Fix them or use a narrow suppression with
  its rationale at the suppression site.
- **Dependency versions are centrally managed.** Each build boundary has one authoritative manifest;
  project descriptors name dependencies without repeating versions.
- **Complete the declared capability through every affected layer, or label it partial.** A lower-tier
  implementation with an implied follow-up reads as done and becomes an accidental contract.
- **Fixtures and neutral data ship with the capability** when they are part of its evidence. Keep them
  deterministic and safe to re-run so a fresh clone can reproduce the claim.

---

## 3. Dependencies and third-party code

- **Read the dependency before writing around it.** Look at its public documentation, source, or
  shipped assembly. Searching only for a guessed type name does not establish absence.
- **Pin dependency behaviour that a workaround rests on.** When a rule compensates for a dependency
  defect, assert the defect so a corrected dependency retires the workaround.
- **A component proved in one hosting model is untested in another.** Threading and exception
  assumptions can be invisible in one host and fatal in another; probe the hosting model actually
  claimed.
- **Prefer the harness's own tool over a substitute.** A hand-rolled equivalent becomes a second,
  drifting standard. If the built-in cannot be used, state why.

---

## 4. Measurement and evidence

- **An in-flight reading is not evidence about a completed object.** Land a small case and read the end
  state when the claim concerns completion.
- **A broad scan does not identify its counterexamples.** Before calling a quantity invariant, sort by
  it, inspect both ends of the range, and state the mechanism expected to produce it.
- **Diagnosing a measurement error does not remove it from the figures.** Re-derive with the corrected
  method rather than explaining the error beside stale numbers.
- **Pin the revision for a rolling measurement.** Re-measure instead of carrying a number forward.
- **Verify against absolute paths.** Multiple checkouts can make the same relative path name different
  content. Ask version control for the committed state and assert a positive fact such as a hash,
  length, or known marker.
- **Verify tooling rather than reasoning from plausibility.** A schema cap, environment variable, or
  tool limit either applies or does not; probe it.
- **Encoding and line endings silently eat data.** Send explicitly encoded bytes where content crosses
  a process boundary, and verify what arrived rather than trusting console rendering.

---

## 5. Documentation and comments

- **Documentation is self-contained.** A document carries its reasoning and does not depend on private
  reasoning or another repository remaining available.
- **Documentation cleanup is default completion work.** When a document is created, superseded, moved,
  or changes status, classify it, repair references, and update the relevant index in the same change.
- **Placement follows ownership.** A document lives with the component that owns the decision even when
  it affects others; only genuinely repository-wide material lives at the top level.
- **Keep documentation classes distinct.** Separate implemented behaviour and operational policy,
  future or partial work with its remainder stated, deletion-gated working notes, and completed or
  superseded history.
- **Route independently consumable documents from the relevant index.** Guard missing and stale routes
  in both directions where feasible.
- **Guard checkable documentation claims.** If a plan or policy names a component, version, path,
  environment, or numbered item that implementers will follow literally, fail a verification step when
  it stops being true.
- **Plans separate open from resolved questions.** Open sections contain only undecided items. Move a
  decision to a dated resolved section rather than annotating it in place.
- **ADRs are one self-contained file per decision**, unnumbered, with `Date` and `Status` headers. A
  superseded ADR keeps its name, changes status, and links its successor.
- **Versioned components update their changelog with the code.** Use lifecycle headings whose state is
  objective and guard them against release state where the repository publishes releases.
- **Never let a pull-request description be the only decision record.** Put rationale, retractions,
  and attributions beside the thing they govern.
- **Default to no comment.** Comment only for a non-obvious why: an invariant, hidden constraint,
  surprising behaviour, or a specific workaround.
- **Comments describe intent, never provenance.** Ports, tracker references, and commit history belong
  in changelogs, plans, and version control. A named external system or wrapped type may remain when it
  is part of the domain.
- **Every comment claim under a named scenario is a test case.**

---

## 6. Git, branches, and pull requests

- **Do new work on a branch and make it reviewable before trunk.** Commit to trunk only when the user
  requests it or the local policy permits a self-evident change; state which exception applied.
- **Preserve user work.** Do not rewrite, discard, stage, or commit unrelated changes. Avoid destructive
  version-control commands and keep edits scoped to the request.
- **Read existing history before naming commits or pull requests.** Follow local conventions, describe
  the whole change rather than its first commit, and do not invent tracker identifiers.
- **Before pushing or merging**, compare with the upstream, run the relevant gate, read the final diff
  including untracked files, and report deliberately deferred work. Never overwrite a concurrently
  advanced remote branch.

---

## 7. Working as an agent

- **Present a brief plan for a refactor or multi-file change.** A single obvious edit needs none.
- **Do not stop at a step you can complete.** Stop at a genuine user decision: an architectural choice,
  an irreversible or outward-facing action, or a scope ambiguity whose readings produce materially
  different work. Deliver everything independent of that answer first.
- **Do not pause for permission to continue mid-run.** Report one short line per stage and start the
  next. Treat new guidance as stop, steer, or wind-down; state what changed and land any bounded work
  before stopping.
- **Never overstate a result.** `Unverified` and `skipped` are not `true`; a reached cap is not
  convergence; a truncated search is not exhaustive. Report every limit that engaged.
- **Be safe to re-run.** Assume operations may run twice or be interrupted and resumed. Dedupe against
  all observed input, not only accepted input, and do not rely on invocation-local state for durable
  correctness.
- **Solve obstacles you can solve.** A red gate is work, not a stopping point, but do not rerun an
  identical failed step. Try at most two materially different corrections before reporting the blocker.
- **Declare side effects and choose the least surprising default.** Name outward-facing actions before
  taking them and expose control through a parameter or explicit user authorization.
- **Track agent tooling in the repository** when it governs shared work. Keep it repository-aware by
  reading the target's own instructions instead of carrying a copied summary that silently drifts.

### When multi-agent work is explicitly requested

- **Effort and budget are parameters, never invented.** Pass session values through unchanged. A floor
  may be explicit; scaling effort above the user's value is not allowed.
- **Every loop has a ceiling and a wind-down.** Reaching the ceiling means land bounded work and report
  what remains, not abandon the tree half-written.
- **Disclose expensive fan-out before it runs.** Report per-invocation fan-out and cumulative agent
  count separately, because only the latter describes total cost.

### Cross-repository cross-pollination

Sibling repositories carry their own instructions. When one is supplied or intentionally inspected,
compare the two and **propose** importing project-neutral improvements in either direction. Name the
instruction, direction, benefit, and any contradiction. Do not auto-apply foreign packages, services,
commands, naming schemes, or architecture assumptions; the user decides whether the proposal belongs
in the target repository.

### Report friction with these rules

Record friction only when one of five triggers fires: a rule could not be applied without guessing;
two rules pointed different ways; a rule cost real work with no visible benefit in the concrete case;
a rule caught a defect that can be named; or an approach worked, is absent here, and would generalize.
“This rule is good” is not evidence.

State a conclusion usable without knowledge of the change and name the concrete case. Keep secrets and
customer data out. Append to the repository's feedback channel without reading existing entries first;
independent repetition is the signal. The periodic sweep groups entries and records what was done so
the channel cannot become an undispositioned suggestion box.

---

## Appendix — rules for writing rules

- **Name what a rule prevents.** A rule with no failure behind it is a preference; mark it as one or
  remove it.
- **One clause of why, then the pointer.** Put the full argument in the decision or policy that owns it.
- **Point at kinds, not filenames, in portable doctrine.** Paths are local facts and belong below
  `## This repository`.
- **Enforce rules where feasible and name the local guard.** Keep rule-to-mechanism mappings in the
  repository-local section so portable doctrine remains portable.
- **No guard may require wording to remain in this file.** Guard the owned decision or behaviour, not
  the presence of a table, heading, or phrase in session instructions.
- **No rolling counts or version-specific delivery status.** Put changing measurements in a tracked
  document or, preferably, an executable check.
- **Do not narrate a rule's history.** Version control owns what it used to say.
- **State exceptions with the rule.** Do not put carve-outs paragraphs away from an apparent absolute.
- **Delete on contact.** Remove stale, contradicted, or obsolete rules in the change that discovers
  them.
- **Edit prose by hand, never with scripted find-and-replace.** Wrapped lines, encoding, and line
  endings make mechanical substitutions look verified while leaving broken sentences. Re-read every
  changed paragraph as prose.

---

## This repository

Everything above is portable doctrine. Everything below resolves Brontide's authorities, paths,
independent implementations, evidence rules, commands, and deliberate exceptions.

### Architecture authority and implementation targets

- Use `Brontide-Architecture-Status.json` to locate the current and latest ratified architecture. Do
  not infer either from filenames or the highest version number.
- Each stack README or focused implementation document states `Designed for: Brontide Architecture
  <version>`. Read that target, the document's status and limitations, and executable tests. Plans,
  notes, inventories, and matrices may add evidence but do not silently change the local target.
- Use earlier architecture documents for work designed against an older version. Do not project later
  draft rules backward.
- The implementation-correction programme is closed. Its permanent status, completion report, and
  independent reviews preserve the evidence; do not recreate the deleted temporary plan or treat it
  as active authority.
- Before planned implementation work, inspect `docs/future/README.md` and take its highest-priority
  item unless the user directs otherwise.

### Independent implementations and dependency direction

- **Keep Reference and Minimal independent.** Neither implementation may reference the other's
  projects, assemblies, private CLR types, dependency-injection container, or exceptions. Implement a
  concept natively on each side. Preserve the capability when translating between stacks; redesign a
  racy, leaky, or privately coupled realization instead of preserving its defect.
- **Cross the stacks only through neutral seams.** Use external manifests, versioned data contracts,
  Shape projection, fixtures, or process boundaries. Family and cross-stack tests prove
  interconnection without creating an in-process compatibility layer.
- **Reference direction:** `Brontide.Reference.Core` has no project dependency. Reference extensions,
  vocabularies, and experiments depend only on Core; Studio is the composition root.
- **Minimal direction:** `Brontide.Minimal.Model` has no project dependency;
  `Brontide.Minimal.Kernel` depends only on Model; extensions, vocabularies, experiments, and Binding
  stay outside Model/Kernel; Host is the composition root.
- **Base stays small.** Host services, UI, persistence, transport, provider selection, acceleration,
  and experimental composition do not belong in Reference Core or Minimal Model/Kernel.
- Public identity spaces use distinct types. Actor, Capability, Shape, Operation, Execution,
  Occurrence, Activity, Fragment, external item, collaboration, and version identities do not share a
  universal identifier merely because their backing primitives match.
- Reference public identifiers normally use immutable `readonly record struct` values or the existing
  local strongly typed identifier abstraction, with construction validation kept beside the type.
- Minimal public identifiers normally use private single-case unions, opaque records, or struct records
  with controlled construction. Issuer-controlled references expose no public path around validation.
- Provisional or non-ratified work belongs in explicitly experimental projects and is never presented
  as Brontide Base conformance.

### Contracts, evidence, and review

- A capability contract carries at least one property that holds over all its vectors, not only
  per-vector outcomes. At each phase boundary, run a separate contract-completeness review asking what
  the contract does not say. A property that cannot fail is a review finding against the property.
  The standing rationale is Decision 10 in
  `binding/portable/open-decisions.md`.
- Keep normative conformance evidence separate from Enrichment, Composition, GPU, and other explicitly
  experimental evidence. Record native/local evidence separately from actual Reference ↔ Minimal
  interoperability; a local fixture simulating another runtime is not cross-stack proof.
- Automated attestation counts as independent review only when the reviewer identity differs from all
  implementation actors, runs in a fresh isolated context without the implementation session's private
  reasoning, and records a decision and rationale for every pinned requirement.
- Every attestation also reviews the architecture selected by the status registry, including its
  status, plus the implementation's local target and limitations. An older retained matrix may show
  implementation evidence but cannot limit the architecture a current review must consider.
- GPU execution is experimental and sideline-only. CPU is the reference path. GPU evidence cannot
  complete Base, Composition, Imaging, or mixed-stack milestones and must expose eligibility,
  lowering, buffers, copies, dispatch, failures, and fallback.
- A public API change is a breaking-change decision. Describe affected consumers and migration. For an
  independently versioned component, update its `CHANGELOG.md` and bump only that component. Mark its
  commit or pull-request title with `!` and include a `BREAKING CHANGE:` migration footer.

### Build, package, and test conventions

- Target .NET 10 and use the SDK selected by the environment. Do not add `global.json`. Minimal's
  `MSBuildToolsPath` copy of the selected SDK's `FSharp.Core.dll` is a runtime-output workaround, not
  permission to pin an SDK version or path.
- `Reference/Directory.Packages.props` and `Minimal/Directory.Packages.props` own dependency versions.
  Project files use versionless `PackageReference` items.
- `Reference/Directory.Build.props` and `Minimal/Directory.Build.props` own warning-as-error policy for
  production, host, tool, and test projects. Keep nullable analysis and relevant analyzers enabled;
  narrow suppressions require rationale.
- Tests use NUnit. Credentialed or live fixtures use `[Explicit]`, a clear category, and their own
  missing-credential skip. They stay outside ordinary CI, use dedicated sandbox resources, and fail
  non-zero when invoked and unsuccessful.
- Tests accompany behaviour in the nearest native suite. Changes spanning both implementations carry
  native evidence in both; neutral vectors force each to answer the same question.
- Every independently consumable component owns unit tests with its first public behaviour. Move or
  translate its tests when extracting it. Integration components also provide explicit live-probe or
  end-to-end coverage and, where useful, a non-interactive console under `tests/<Component>.TestConsole`
  with one verb per capability. A test console is a real-consumer host, not an assertion library: it
  composes only public surfaces, prints plain-text diagnostics, and exits non-zero on failure. Register
  consoles here, document their quick reference, and add a task-oriented `docs/integration-guide.md`
  with a short rules summary for coding agents as the surface grows.

Registered integration test consoles:

- `Reference/tests/Brontide.Reference.Architecture07.TestConsole` and
  `Minimal/tests/Brontide.Minimal.Architecture07.TestConsole` are offline, non-interactive R5/M5
  observation endpoints. Run both through `build/verify-architecture-0.7-comparison.ps1`. Their sole
  operation evaluates every vector in `conformance/architecture-0.7-comparison-vectors.json` and writes
  canonical JSON to a caller-supplied path. They have no live verbs, credentials, configuration
  source, network access, or permitted external sandbox target.

### Documentation in this repository

- `docs/README.md` is the authoritative documentation map. Keep its four classifications distinct:
  `docs/current/` for implemented behaviour and operational policy; `docs/future/` for planned,
  draft, partial, or proposed work; `docs/temporary/` for deletion-gated execution notes; and
  `docs/archive/` for completed or superseded work.
- Documentation cleanup repairs both Markdown links and plain-text path references and updates every
  affected index in the same change.
- A partially implemented plan remains under `future` and states both the implemented subset and the
  remainder. When complete, move it to `archive` and first move lasting guidance or evidence to
  `current` or the owning implementation.
- Agent-feedback entries follow the same lifecycle. The convention and unswept months live under
  `docs/current/ai-feedback/`; a swept month and its disposition report move to
  `docs/archive/ai-feedback/`. Do not read existing entries before writing a triggered one.
- Keep the repository root to standard project-control files, `README.md`, `AGENTS.md`, and
  `Brontide-Architecture-Status.json`. Repository-wide Markdown belongs under `docs/`;
  implementation-owned documentation belongs under `Reference/` or `Minimal/`.
- Directly or transitively evidence-pinned documents stay at their classified stable paths during
  ordinary cleanup. Moving or rewriting one requires explicit user authorization to repin the
  evidence and obtain fresh independent review. Do not substitute a redirect stub. Preserve the path
  and report the blocked cleanup when that authority is absent.
- Architecture 0.5 and earlier archives live under `docs/archive/foundation/`; archive later work by
  area rather than date.
- Implementation plans end with `## Open questions (owners needed)` containing only unresolved items
  and named owners, followed by `## Resolved questions` with dated rulings.
- Finalizing an architecture version includes a changelog passage titled `Direction for <next
  version>` that names the next version's priorities and explicit non-goals.
- Update the affected stack's `README.md`, `milestone-evidence.md`,
  `implementation-findings.md`, or `experimental-and-sideline-projects.md` when a change alters a
  milestone claim, boundary, limitation, or experiment status.

### Build and verification

Run commands from the repository root unless a component says otherwise. During work, scope compiled
verification to the changed stack or project when the dependency boundary is clear. Documentation-only
changes run the text and link guards plus the version-control whitespace check:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-text.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-doc-links.ps1
git diff --check
```

Reference stack:

```powershell
dotnet restore .\Reference\Brontide.Reference.sln
dotnet build .\Reference\Brontide.Reference.sln --no-restore
dotnet test .\Reference\Brontide.Reference.sln --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1
```

Minimal stack:

```powershell
dotnet restore .\Minimal\Brontide.Minimal.slnx
dotnet build .\Minimal\Brontide.Minimal.slnx --no-restore
dotnet test .\Minimal\Brontide.Minimal.slnx --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1
```

Run a complete stack suite for shared build or solution files, Core, Model, Kernel, public semantic
contracts, project references, or uncertain reach. Changes spanning both stacks run both suites and
both dependency guards.

Repository-wide finalization runs the complete gate after in-scope blockers and non-blockers are
fixed and current documentation is updated:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

Obtain fresh-context review afterward wherever active evidence or review policy requires it. Do not
turn ordinary work-in-progress iterations into repeated full gates or formal review requests.

### Git and GitHub

- New implementation, documentation, and evidence work normally uses a branch and pull request.
  Commit directly to `main` only when the user requests it or the change is small and self-evident
  enough that review adds nothing; state the reason.
- Do not create a task record unless the user asks or an active workflow requires one. Do not invent
  task identifiers, issue numbers, or lane names.
- User branches have no mandatory naming scheme. Codex-created branches use `codex/` plus a short
  descriptive name unless the user requests another name; never rename an active user branch merely
  to satisfy that default.
- Commit subjects are concise, lowercase-imperative, and have no trailing period. Conventional Commit
  form is welcome but optional except for breaking changes. Pull-request titles may be plain summaries
  or Conventional Commit titles and must describe the whole branch accurately.
- Before pushing or merging, verify the relevant suite and dependency guard, check the final diff,
  and report deliberately deferred milestone work. Prefer fast-forward merges where history permits.
  Never force-push, merge, rewrite a pull-request title or description, or otherwise mutate remote
  state without user authorization.

### Local mechanisms the doctrine refers to

| Doctrine rule | Brontide mechanism |
| --- | --- |
| Architecture and implementation routing | `Brontide-Architecture-Status.json`, stack `README.md` files, and `docs/README.md` |
| Warnings as errors and central dependencies | Each stack's `Directory.Build.props` and `Directory.Packages.props` |
| Dependency direction and stack independence | `Reference/build/verify-dependencies.ps1`, `Minimal/build/verify-boundaries.ps1`, and root graph guards |
| Text and documentation integrity | `build/verify-text.ps1` and `build/verify-doc-links.ps1` |
| Neutral and cross-stack evidence | Shared `conformance/` and `binding/` artifacts plus the comparison and portable-binding gates |
| Evidence pins and independent review | `build/verify-evidence.ps1` and `build/verify-independent-review.ps1` |
| Repository finalization | `build/verify-interchange.ps1` |
| Rule-friction feedback | `docs/current/ai-feedback/`, only when a portable trigger fires |

### What reads this file

No guard requires a phrase, table, or section to remain in this file; it is free to shrink when rules
become stale. As a root project-control document, `AGENTS.md` remains at the repository root and is
covered by the text and documentation-link guards.
