# Brontide Reference Stack 0.2

Brontide Reference Stack is the independent .NET 10 / Avalonia implementation and showcase.

**Designed for:** [Brontide Architecture 0.7](../docs/current/architecture/Brontide-Architecture-0.7.md)

**Status:** Partial implementation with explicitly labelled experiments

This target states the architecture revision against which the stack was devised. The implemented
surface and known limitations are described here and exercised by the solution tests. Focused
experimental projects may state a later target locally; in particular, Component Management is
designed against Architecture 0.8 without changing the stack-wide target.

Architecture 0.7 R1-R5 now have Reference-native Complete Draft implementation evidence for recursive three-state
Constraint expressions, fail-closed authority evaluation, experimental Composition selection, and
distinct typed-member canonical names with an open provisional member-kind token. R3's static
Attribute-constrained binding (`BR-07-BINDING-001`) is implemented and tested, but the matrix still
records it as `planned`: changing that status changes a hash the closed independent-review request
pins, which needs that review retargeted and freshly attested by a reviewer who is not an
implementation actor. R4 adds an experimental persistent-information component for Opaque Corpus,
Dataset identity and authorised issuance, explicit single-writer access, Store roles, and stable
Router endpoint guarantees. Its matrix promotion is governed by the same pinned-review boundary.
R5 adds a real-process comparison endpoint which consumes a shared data-only fixture and agrees
with both its independent oracle and the Minimal endpoint across 15 R1-R4 observations. This is
finite experimental comparison evidence, not a ratification or private-model compatibility claim.
R6 is complete as non-runtime handoff planning: the shared Architecture 0.8 ledger accounts for
C1-C14 and all 33 adversarial/evidence vectors, while Reference's separate implementation note
records its carried parent chain and current no-revocation ceiling. No 0.8 runtime claim follows.
The follow-on Architecture 0.8 delivery audit is also complete: Reference's independent matrix marks
C3/C4 as reusable candidates, C1/C2/C8-C10 as partial candidates, C6/C7 as conflicts, and the
remaining runtime gaps as missing. All canonical runtime vectors remain unaccepted; A08-D1 is only
the proposed next runtime slice.
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

`Brontide.Reference.Experimental.PersistentInformation` carries R4/C4-C5 outside Core: typed
Corpus, Dataset, Store-role, Store, and Router identities; Dataset records independent of Store
content; issuance through an existing Capability-authorised Operation; and Router guarantees
validated across every declared backing and fallback. Its endpoints are deterministic in-memory
evidence, not a durable database or general storage layer.

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
CBI20 lets that successor resolve a different set of positions, adding and dropping members across the
cutover. The membership is read from the successor generation rather than taken from the caller — the
rule CBI19 declared and never checked — and the added, dropped, and surviving occurrences are derived
from it. A dropped occurrence's authority is not re-established because there is nothing to admit it
against, and an added position joins only across the cutover.
CBI21 activates a strongly connected group that declares no lifecycle protocol. CM3 groups by
strongly connected component over every edge, so being cyclic is not the same as needing a handshake,
and CBI12 refused the first for a property only the second has. A group that does declare one is
refused by name: Portable Binding's Composition handoff declares Relational Initialisation out of
scope, offers a composition one traffic verb gated on Release, and reports Ready during
Interconnection, so there is no window before the readiness CM4 requires a handshake to precede.
CBI22 activates a Component CM2 resolved inside a child Port, in its own restart scope beneath a
released parent. Until it, the Region and Port a Provider Set carries were read by nothing, so
such a position was flattened into an ordinary one; both activation paths now refuse it. Which
Port an attachment names, and what it may claim about that Port's lifecycle, come from the
resolved envelope rather than the caller, and the parent stays active and serving throughout.
CBI23 nests those attachments and orders their withdrawal. CM4 models no relationship between a
parent and a child after attachment, so the ordering is the composition root's: an attachment
occupies a Port of a generation and cannot outlive it, which makes the cascade deepest-first. Depth
is unbounded, and the root can only order the activations it is given.
CBI24 replaces a generation that has attachments beneath it. A replacement orphans them silently,
because CM4 preserves every unrelated scope and a child's is unrelated, so the cascade runs before
the cutover — the opposite of CBI19's retained members, since an attachment is outside the
transaction rather than inside it. A Port does not migrate; a child is stood down and stood up again.
CBI25 carries a mediated position into preflight by binding the Component its Mediation is realized
as. The seam refuses an erased Mediation because the obligations would lose their holder, and CM2
gives them one, so nothing is erased and no refusal is relaxed. A static-host Mediation is refused
whatever Component it names.
CBI26 admits that mediator's authority for what it does itself, and refuses a Mediation that
declares it owns authority: CM5 has no relationship meaning "acts on behalf of" and its grant names
one holder with no beneficiary, so deputy authority has no representation to approximate.
CBI27 carries a position wider than `1..1` into preflight as one ordinary member per resolved member,
because a Provider Set's members each have a representation the seam holds and the set does not. Doing
so shows that a CM binding scope holds many bindings while a portable one names a single binding, so
CBI1's mapping of one onto the other holds only while a position is `1..1` and a scope holds one
position.
CBI28 activates those members. Nothing downstream needed teaching, because every slice from CBI12
onward is per-occurrence; a wide position supplied half-complete would have passed both plan checks,
and the generation is now the authority on its membership. A Provider Set's declared minimum is not a
runtime concept, so one member short of Ready retires the whole activation.
CBI29 activates that complete wide position inside one child Port through the existing CBI22 path.
The member binding scopes remain distinct from the child restart scope, the barriers remain
child-wide, and every outcome leaves the released parent unchanged.
CBI30 runs the direct CBI2 activation through the negotiated portable realization in a real provider
process. Both provider implementations substitute behind the contract, process loss is explicit,
and retirement terminates the provider. CBI31 admits a verified local executable under an allowed
root and exact launch policy. CBI32 stages its complete declared file set transactionally under a
canonical content address, detects corrupt reuse, leases activation through CBI31, and removes only
the exact inactive set. CBI33 reads that set from a named injected source under exact member lengths
and a total-byte limit while keeping transport, publisher evidence, and local admission separate.
CBI34 verifies detached ECDSA P-256 evidence over the canonical acquisition manifest while leaving
publisher trust and admission explicitly undecided. CBI35 evaluates that verified key against a
canonical host policy with explicit admission and revocation while still leaving artifact admission
unattempted. CBI36 requires that exact authorization before CBI33 source access and preserves later
transport and admission outcomes. CBI37 verifies host-pinned signed policy updates, enforces a
monotonic predecessor chain, and supersedes old authorizations for future acquisition. Network
protocols and distribution remain future work. CBI38 atomically checkpoints and recovers that full
signed policy chain, detects rollback against an independently retained recovery floor, and keeps
failed publication from advancing live state. Secure floor custody, durable cross-process
coordination, and production isolation remain future work. CBI39 adds one authenticated, fresh,
challenge- and cursor-bound asynchronous distribution attempt with explicit size, timeout,
cancellation, and no-retry bounds. CBI40 supplies its portable strict binary codec and one exact
bounded HTTPS POST adapter. CBI41 drives that attempt from a bounded host-owned poll cycle with
deterministic capped backoff, retry confined to the outcomes a fresh attempt can change, and a
recovery-floor handoff ordered after checkpoint publication. CBI42 gives that floor durable custody:
a monotone, integrity-checked host-local store, established before the checkpoint it guards, read
back at the next start, and never advanced by a recovered checkpoint. Custody in a domain the
checkpoint's writer cannot reach, endpoint rotation, a real scheduling host, and platform security
anchors remain future work. CBI43 runs the whole chain as one path — polled policy, publisher
evidence, governed acquisition, content-addressed staging, provider launch, and CBI30 activation —
keeping each stage's own refusal code and origin. CBI44 makes the launch take its own trust decision
against the policy in force, so a publisher revoked between acquisition and launch does not run,
while a policy that changed and still admits it does. CBI45 retains the verified launch evidence,
binds the provider conversation to an opaque serving activation, and takes one explicit current-policy
decision after Release; lapsed trust retires the member and terminates the concrete provider. CBI46
adds one explicit host-owned sweep over 1-64 serving activations, with
whole-set preflight, deterministic occurrence order, complete sibling observations, and shared-set
cleanup that keeps bytes while any swept sibling continues. CBI47 composes CBI41 and CBI46 into one
bounded, injected-time host cadence: policy is current before the serving set is snapped, successful
withdrawal continues, and invalid, incomplete, canceled, or non-current work stops visibly. CBI48
adds a host-local durable run journal: committed cycles resume without replay, each effectful cycle is
recorded in-flight before invocation, and a crash during that cycle requires explicit retry or
abandonment rather than being mistaken for either success or no effect. CBI49 adds an explicit host
availability policy: only exhausted transport failure or timeout can preserve existing service
within a bounded, non-sliding grace interval, no offline outcome permits a provider start, and an
interrupted journal changes only under exact matching reconciliation evidence. CBI50 binds that
decision to one exact 0-64-member serving snapshot: permitted grace is effect-free, while every stop
decision retires and terminates the complete set in typed occurrence order without deleting staged
artifacts or authorizing restart. CBI51 makes restart eligibility explicit and effect-free: only
availability or unexpected-exit stops can proceed, and only under an exact current-cycle policy
identity, a fresh authorization for the retained content, and a bounded retry budget. CBI52 enforces
one ready decision from the stopped activation's opaque retained recipe: it re-verifies the complete
staged set, launches a new provider, reconstructs a fresh portable member under the same occurrence
and logical runtime, and admits at most one successful successor while preserving retained content
on refusal. CBI53 makes the bounded restart history durable: one journal is tied to the occurrence
and retained staged identity, records an attempt in-flight before CBI52 effects, resumes committed
history without replay, and requires explicit retry or abandonment after an interrupted attempt.
CBI54 adds host-local cross-process ownership in front of that journal: an operating-system lock
excludes competing processes, an integrity-checked durable epoch fences every successor owner, and
only the current live lease may enter CBI53 recovery. Process loss releases exclusivity without
turning the retained record into proof that an interrupted provider effect completed. CBI55 makes
that interrupted provider lifetime externally reconcilable: a durable record precedes launch, the
provider holds a token-specific operating-system lease and writes an exact process receipt, and a
strictly later owner selects retry only after proving the lease free or terminating the exact orphan
and then proving it free. Missing, corrupt, or mismatched evidence leaves the attempt in-flight.
CBI56 adds a separate durable CBI39 endpoint-key anchor: the active endpoint may sign one exact
successor, but that successor becomes active only after authenticating a complete CBI39
synchronization. Staging never widens ordinary polling, and externally retained floors detect
rollback of the active generation. CBI57 rotates the other key, the authority that signs policy
itself, and does it inside the retained CBI38 chain rather than beside it: a transition is one
durable link carrying the predecessor's authorization and the successor's countersignature, recovery
re-verifies every retained update against the authority in force at its position, and the
out-of-band pin never moves, so a rotation retires no serving member. CBI58 supplies those rotation
statements through a separate single-attempt source authenticated by the active CBI39 endpoint. Its
signed response binds the exact policy and authority cursor, freshness, and the complete CBI57
statement; only the durable registry decides whether the delivered transition applies.
CBI59 gives that separate source a canonical bounded binary wire and an exact single-attempt HTTPS
adapter. Both declared and streamed bodies are capped at 1 MiB, response metadata and the effective
URI are exact, cancellation propagates, and the adapter never retries or changes CBI57 authority.
CBI60 wraps those single attempts in one bounded, host-driven cycle and gives the authority floor
durable custody. Backoff is a jitter-free function of consecutive failures that an applied rotation
resets, retry is confined to what a fresh attempt can change, and each applied rotation is handed to
an integrity-tagged store only after CBI57 has published it. A guard absent beneath an existing
checkpoint is adopted at zero and reported as such, because CBI42's establish-before-the-checkpoint
ordering is not available to a guard introduced afterwards.
CBI61 makes those two loops one cycle inside CBI47's unchanged cadence. The rotation runs first
because a policy update is verified against the authority in force, so an update signed by the
authority a pending rotation installs is refused until that rotation is retained. A rotation that
changed nothing is recorded and the poll still runs; a rotation published without its guard stops
before the policy endpoint. An update the registry cannot verify is attributed to an incomplete
rotation only when one was attempted and did not complete, and is otherwise the stranger CBI41
already refuses.
CBI62 puts that governed cycle under CBI48's durable journal. Cycle codes now live in one vocabulary
the cycles produce from and the journal validates against, which repairs a seam defect: CBI48 refused
CBI61's two additions and left a normally completed run recorded as an interruption. The journal
records nothing about which of the two loops a resumed cycle had run, because a marker written after
the rotation returns is not atomic with its effect while the retained chain already records it, and a
retried cycle cannot double-apply either half.
CBI63 reconciles a governed interruption. The write that marks an attempt in-flight also records the
durable cursor it was about to act on — the same device CBI62 refused, sound here only because it
precedes the effect rather than following it — so the rotation and policy observations are derived
rather than asserted. The evidence is therefore narrower than CBI49's, carrying only the serving
verdict nothing can check, and CBI49's own path refuses a journal that recorded a cursor.
CBI64 puts CBI49's availability policy and CBI50's enforcement inside the cadence, which nothing that
polls repeatedly had ever reached: an outage used to end the run with every provider still serving.
The baseline is the instant of the most recent cycle whose poll was current and an outage never
refreshes it, so the deadline arrives; every non-current poll reaches a decision rather than only the
grace-eligible ones; and the cycle code still names why current policy could not be established, so
CBI61's attribution survives a cycle that stopped every member.
CBI65 derives that baseline from what CBI48 already committed, so a crash inside an outage does not
restart grace. It needs no new durable record — the journal has held each cycle's instant and code
since CBI48 — and the question it asks is answered by the cycle vocabulary rather than by the
derivation, so a later code cannot be added without deciding it. `provider-trust-cycle-stopped` is
unanswerable, because a cycle produces it both for a poll that was not current and for a current poll
whose sweep failed, and it is refused rather than guessed.
CBI66 lets CBI49's retry instant shorten a cadence gap, so a run lands on the availability deadline
rather than at the first scheduled cycle after it, and fixes a journal that recorded the schedule
interval as every gap regardless of what elapsed. The bound is one-sided: a retry instant may bring a
look forward and never push it back, because the interval is the host's own schedule.
CBI67 records why the host stopped each provider and makes the cause CBI51 reads issuer-controlled: it
took a caller-supplied `ProviderRestartCause`, two of whose four values are refusals, so the caller
chose which applied. A withdrawn publisher and an unexpected exit were guarded anyway; an operator
retirement was not, and that is what the record buys. A stop is recorded after it happens, absence
means the host did not stop it, and a retirement issued outside the host cannot be attributed.
CBI68 gives the cadence journal an owner epoch, closing a gap six slices declared and none checked: two
holders each wrote their whole copy back, so one whose copy was behind erased a committed cycle with
nothing reporting it. Ownership is claimed by writing rather than by opening, because opening is how a
host inspects a run — which three existing CBI48 tests require.
CBI69 pairs that fence with a live operating-system lock, so a second host is refused before it reaches
the record rather than after. Closing the boundary showed what it cost: a cadence writes after its
cycle runs, so a competitor that reconciled the in-flight attempt took the run while the cycle was
still executing and the record kept nothing of it — and a fenced holder never rejoins, so the transfer
was permanent rather than the alternation CBI68's limits describe. Supervision claims nothing, adds no
durable record because the journal already carries the epoch, and coordinates cooperating hosts only.

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
