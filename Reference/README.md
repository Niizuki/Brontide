# Brontide Reference Stack 0.2

Brontide Reference Stack is the independent .NET 10 / Avalonia implementation and showcase.

**Designed for:** [Brontide Architecture 0.7](../docs/current/architecture/Brontide-Architecture-0.7.md)

**Status:** Partial implementation with explicitly labelled experiments

This target states the architecture revision against which the stack was devised. The implemented
surface and known limitations are described here and exercised by the solution tests. Focused
experimental projects may state a later target locally; in particular, Component Management is
designed against Architecture 0.8 without changing the stack-wide target.

Architecture 0.7 R1-R2 now have Reference-native Complete Draft evidence for recursive three-state
Constraint expressions, fail-closed authority evaluation, experimental Composition selection, and
distinct typed-member canonical names with an open provisional member-kind token. R3's static
Attribute-constrained binding (`BR-07-BINDING-001`) is implemented and tested, but the matrix still
records it as `planned`: changing that status changes a hash the closed independent-review request
pins, which needs that review retargeted and freshly attested by a reviewer who is not an
implementation actor.
The retained [`conformance/architecture-0.7.json`](./conformance/architecture-0.7.json) matrix is
detailed test evidence, not the source of the implementation target and not a claim that the
remaining Architecture 0.7 work is implemented.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md).
Brontide Reference Stack now owns independently implemented experimental hosts and provider endpoints for the
process-isolated Cooling and Catalog/resource proofs. The retained tests execute a real Brontide Minimal Stack provider process; no
Brontide Reference Stack project references Brontide Minimal Stack assemblies or private types.

Architecture 0.7 does not ratify Component descriptors, system service discovery, execution
explanation, or optimisation-property vocabularies. Their Brontide Reference Stack
realisations therefore live in `Brontide.Reference.Experimental.Composition`, not `Brontide.Reference.Core` or normative
conformance. That project also carries Architecture 0.7 §18.1's static Attribute-constrained
binding: a binding resolves exactly once against the Attribute values read at that moment, records
the effective values and the per-candidate provenance that decided it, and holds no source — so a
later Attribute or candidate change cannot rebind it, and restoration consults nothing.

`Brontide.Reference.Experimental.ComponentManagement` now implements the fake Component Management
CM0-CM6 programme: strict neutral-fixture loading, deterministic attributable discovery, immutable
staged acquisition, evidence-policy observations, source disappearance, effect-free recursive
resolution into an inspectable Proposed Stack and immutable generation, and deterministic
strongly-connected activation-group planning followed by a deterministic fake Host for optional
preparation, named establishment, lifecycle and ordinary gates, Ready, logical Release, scoped
replacement, child-Port attachment, post-Release binding evidence, and explicit rollback or
degradation, followed by Reference-native evidence evaluation, receiving-policy Actor mapping,
exact narrow Capability admission, withdrawal on revoked or expired evidence, unlimited-authority
refusal, and attributable policy mistakes. CM6 reconstructs complete scenarios into Reference-native
types and compares canonical full CM5 profiles with a Minimal provider across a bounded JSON Lines
process seam; the reciprocal Minimal-host test exercises this provider. It does not load arbitrary code, provide production
isolation, durable rollback, cryptographic verification, federation, or production authority.
Reference Studio now also owns the CBI1 composition-root adapter: a completed direct `1..1` CM2
provider position and explicit identity mapping can enter PB7 preflight, while wider, mediated,
missing, indirect, or mismatched positions fail before a provider starts.
Its CBI2 coordinator aligns that member with one singleton, protocol-free CM4 lifecycle, deriving
stage evidence from PB7 and releasing the portable gate only after CM4 reaches Active. It does not
perform CM5 authority admission or support multi-member or relational activation.
CBI3 adds one explicit occurrence-to-Actor mapping and one exact native CM5 relationship and grant
as a precondition for that lifecycle. Denial stops before provider contact; the admitted grant
remains receiving-domain evidence and is never inserted into a portable contract or payload.
CBI4 independently serializes five native integrated outcomes into a canonical profile pinned by
the shared fixture, covering complete CM5 parity, CM4 effects, portable lifecycle, and stable
resolution and Binding Plan facts. It adds no integrated process protocol.
CBI5 revalidates the exact native relationship and grant behind one active CBI3 binding from fresh
explicit CM5 inputs. Exact renewal preserves Release; revocation, expiry, request mismatch, or a
different local admission retires the member before further ordinary interaction, and cleanup
failure cannot reopen the local gate.
CBI6 admits a set of participants, each holding one or more exact narrow grants, over that same
singleton binding. Repeated identities across requests, two participants mapped onto one
receiving-domain Actor, an unlimited or repeated authority tuple, and any participant the evaluator
does not admit exactly all refuse the set before a provider is reached.
CBI7 revalidates that set from fresh explicit CM5 requests. The identical set renewing identically
keeps the member released; a changed membership, identity drift, or any participant that does not
renew retires it before further ordinary interaction, and the result names which participants did
not renew rather than narrowing the set.
CBI8 adds participants to that set in place while the member stays released. Removal, substitution,
an unchanged set, a collision with a participant already live, and an addition CM5 does not admit
are all declined without disturbing the binding; only an evaluated lapse in a retained participant
retires it.
CBI9 removes and substitutes participants of that set under a dependency declared by the resolved
definition and mapped explicitly to CM5 tuples. A revision is admitted while every declared
dependency stays covered, so a substitute may hold what a departing participant held; an uncovered
dependency, an unsatisfied or empty declaration, and a declaration that does not match the
generation's record are all declined.
CBI10 verifies that declaration against observed portable interaction by projecting each one into a
CM4 binding exercise whose authority admission is derived from the declaration and the grants in
force. CM4 refuses the projection exactly when the verification does, and undeclared or ungranted
use retires the member.
CBI11 narrows a declaration only to a successor resolution of the same position that declares
strictly fewer authorities, with each retained one keeping its exact tuple and observed use vetoing
its own removal. It never retires a member; a later CBI9 revision releases what the narrower
declaration no longer needs.
CBI12 activates several independent members under one CM4 activation. No member is released until
every member is Ready and CM4 accepts the activation; a failed member retires the ones that
succeeded; and a cyclic group carrying Relational Initialisation is refused rather than activated
without it.
CBI13 admits a participant set per member before any provider is contacted, keeps admission,
relationship, and authority identities distinct across the whole activation, and requires the
receiving-domain Actor mapping to be a function and injective across it.
CBI14 revalidates every member's authority from fresh explicit CM5 requests and retires the whole
activation when any member's lapses, naming which members lapsed and which participants within them
so a member retired because a sibling lapsed is not reported as the cause.
CBI15 revises those participant sets under per-member declarations. A change is decided per member
and checked against the activation; a declined revision leaves everything as it was, while a lapse
found while evaluating retires the activation even when it is in a member that was not being
revised.
CBI16 verifies every member's declaration against what that member actually did. The activation's
projected binding exercises go to CM4 as one request, so one member's undeclared or ungranted use
condemns all of them; attribution stays per member, so the same Operation in two members is two
independent attributions.
CBI17 narrows those declarations to one successor generation, as one transaction: a member the
successor does not resolve blocks every other member, a veto anywhere refuses everything, and a
member the successor leaves alone is untouched rather than refusing. Nothing here retires a member or
reaches a provider.
CBI18 grows those participant sets without consulting any declaration, because growth removes nobody
and so cannot uncover a declared dependency.
CBI19 replaces the generation in the restart scope with a successor generation. CM4 swaps a whole
generation atomically, so nothing retires one member while its scope keeps running; authority follows
the occurrence and is re-established rather than inherited; and the retained members are retired only
after cutover, because a failure before it must leave them serving. A party already participating in one member may be added
to another, under the local Actor it already holds; removal and substitution stay CBI15's, and a
lapse in any retained participant retires the whole activation.
CBI20 adds and removes positions across that replacement. A member joins or leaves only by a cutover,
because which positions exist is a property of the generation; a drop the successor generation does
not make is refused, so removal stays the composition's decision rather than the caller's; and
CBI13's receiving-domain Actor rules are checked over the retained and successor activations
together, because both are established at once until the cutover completes.

## Build and test

The repository deliberately has no `global.json`; [`sdk-policy.md`](../docs/current/policies/sdk-policy.md)
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
See [`../docs/current/policies/public-boundaries.md`](../docs/current/policies/public-boundaries.md) for payload, timeout, cleanup,
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
`Brontide.Reference.Experimental.Binding/Portable/` — the Reference realization of the experimental
[Portable Component Binding](../binding/portable/README.md), including the Composition handoff that
produces a Binding Plan at activation preflight — obeys the same rule: it depends only on Core, and
the composition that drives it lives in the test estate rather than in Core.
The dependency verifier also rejects Brontide Minimal Stack project references and foreign Brontide Minimal Stack assemblies in
Brontide Reference Stack outputs.
