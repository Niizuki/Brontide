# Brontide Minimal Stack

Brontide Minimal Stack is the independent F# implementation and headless counterpoint.

**Designed for:** [Brontide Architecture 0.7](../docs/current/architecture/Brontide-Architecture-0.7.md)

**Status:** Partial implementation with explicitly labelled experiments

This target states the architecture revision against which the stack was devised. The implemented
surface and known limitations are described here and exercised by the solution tests. Focused
experimental projects may state a later target locally; in particular, Component Management is
designed against Architecture 0.8 without changing the stack-wide target.

Minimal lives beside Brontide Reference Stack but does not reference Reference assemblies or reuse
Reference CLR types; the implementations support, challenge, and eventually substitute for one
another through an explicit external binding seam.

Architecture 0.7 M1-M2 now have Minimal-native Complete Draft evidence for recursive three-state
Constraint expressions, fail-closed target-side evaluation, experimental Composition selection,
and opaque typed-member canonical names with an open provisional member-kind token. M3's static
Attribute-constrained binding (`BR-07-BINDING-001`) is implemented and tested, but the matrix still
records it as `planned`: changing that status changes a hash the closed independent-review request
pins, which needs that review retargeted and freshly attested by a reviewer who is not an
implementation actor. The retained
[`conformance/architecture-0.7.json`](./conformance/architecture-0.7.json)
matrix is detailed test evidence, not the source of the implementation target and not a claim that
the remaining Architecture 0.7 work is implemented.

The current repository-wide programme is
[`Brontide-Interchange-Implementation-Plan-0.1.md`](../docs/archive/interchange/Brontide-Interchange-Implementation-Plan-0.1.md).
Its first two experimental proofs are implemented in both host directions. Cooling exercises the
native authority/Fragment/Outcome path; Catalog adds nested repeated values, two Operations,
explicit failure, provider-scoped resource refusal, replay detection, and a fixed payload limit.

The implementation currently provides:

- an immutable `World` and pure `World.step` authority kernel with opaque issued references,
  explicit target and presented Capability, narrowing delegation, recursive fail-closed Constraint
  expressions, trusted time, and redacted audits;
- canonical versioned Shapes, authored Fragments, explicit projection, Operations, Constraints,
  Capabilities, attenuation, Outcomes, Events, and provenance;
- native Cooling, Event Distribution, and Flow semantics;
- isolated Enrichment and implementation-baseline Composition experiments, the latter carrying
  Architecture 0.7 §18.1's static Attribute-constrained binding: one-time resolution recording
  effective values and provenance, holding no source, and restoring without reselection;
- deterministic CPU imaging, boxed application boundaries, provider opposition and selection
  explanations, and visible optimisation eligibility;
- a tagged JSON ShapeValue codec and versioned external manifest negotiation;
- independently implemented Cooling v2 and Catalog v1 process-binding experiments, host clients,
  provider endpoints, adversarial vectors, and structured operational observations;
- a native realization of the experimental [Portable Component Binding](../binding/portable/README.md)
  under `Brontide.Minimal.Binding/Portable/`: deterministic-CBOR encoding over a bounded
  length-delimited wire, negotiation and a frozen Binding Plan, frameless local denial, referenced
  resources, an explicit lifecycle, the C9 observation set, a fixed direct-call and a negotiated
  process realization, and the Composition handoff that turns a resolved requirement and an offered
  provision into a Binding Plan at activation preflight;
- the fake Component Management CM0-CM6 programme: strict neutral-fixture loading, deterministic
  attributable discovery, immutable staged acquisition, evidence-policy observations, and pure
  source-removal transitions, followed by Minimal-native effect-free recursive resolution into
  immutable Proposed Stack and generation values, and deterministic strongly-connected
  activation-group planning followed by a Minimal-native deterministic fake Host for optional
  preparation, named establishment, lifecycle and ordinary gates, Ready, logical Release, scoped
  replacement, child-Port attachment, post-Release binding evidence, and explicit rollback or
  degradation, followed by Minimal-native evidence evaluation, receiving-policy Actor mapping,
  exact narrow Capability admission, withdrawal on revoked or expired evidence, unlimited-authority
  refusal, and attributable policy mistakes, followed by complete scenario reconstruction into
  Minimal-native types and canonical full-profile comparison with a Reference provider across a
  bounded JSON Lines process seam; the reciprocal Reference-host test exercises this provider. It
  does not load arbitrary code, provide production
  isolation, durable rollback, cryptographic verification, federation, or production authority;
- Minimal Host owns the CBI1-CBI2 composition-root integration: a completed direct `1..1` CM2 provider
  position and explicit identity mapping can enter PB7 preflight, while wider, mediated, missing,
  indirect, or mismatched positions fail before a provider starts; one singleton, protocol-free
  CM4 plan can then derive stages from PB7 and release the portable gate only after CM4 Active;
  CBI3 additionally requires one explicit occurrence-to-Actor mapping and one exact native CM5
  relationship and grant before provider contact, without transporting that grant through PB7;
  CBI4 independently serializes five native outcomes into shared canonical profile digests without
  adding an integrated process protocol; CBI5 revalidates the exact admitted relationship and grant,
  preserving Release only for exact renewal and otherwise retiring the member before further
  ordinary interaction; CBI6 admits a set of participants holding one or more exact narrow grants
  each, refusing repeated identities, a shared receiving-domain Actor, and any partially admitted
  set before a provider is reached; CBI7 revalidates that set and retires the shared member, naming
  which participants did not renew, rather than narrowing the set when one of them loses authority;
  CBI8 grows that set in place while the member stays released and declines every shrinking change
  without disturbing the binding; CBI9 removes and substitutes participants under a dependency the
  resolved definition declares, admitting a revision only while every declared dependency stays
  covered; CBI10 verifies that declaration against observed portable interaction through derived CM4
  binding exercises, retiring the member on undeclared or ungranted use; CBI11 narrows a declaration
  only to a successor resolution of the same position that declares less, with observed use vetoing
  its own removal and no retirement path at all; CBI12 activates several independent members under
  one CM4 activation, with the release barrier at the activation and a failed member retiring the
  ones that succeeded; CBI13 admits a participant set per member before any provider is contacted,
  with identities distinct across the whole activation and the receiving-domain Actor mapping a
  function and injective across it; CBI14 revalidates every member and retires the whole activation
  when any member's authority lapses, naming the lapsed members and the participants within them;
  CBI15 revises those sets per member under per-member declarations, checking the result against the
  whole activation and declining rather than retiring when the request itself is wrong; CBI16
  verifies every member's declaration against what that member did, through one CM4 request whose
  single verdict makes one member's undeclared use condemn the whole activation; CBI17 narrows those
  declarations to one successor generation as a single transaction, where a member the successor does
  not resolve blocks the others and a member it leaves alone is untouched rather than refusing; CBI18
  grows those sets without consulting any declaration, admitting a party already live in one member
  into another under the local Actor it already holds; CBI19 replaces the generation in the restart
  scope with a successor, re-establishing authority per occurrence and retiring the retained members
  only after cutover; CBI20 adds and removes positions across that replacement, refusing a drop the
  successor generation does not make and checking the receiving-domain Actor mapping over both
  activations at once;
- a headless host and seven F# test assemblies, including the host-owned CBI1-CBI20 integration
  suite.

There is deliberately no `global.json`. Brontide Minimal Stack targets .NET 10; the supported range
and CI feature bands are checked by [`sdk-policy.md`](../docs/current/policies/sdk-policy.md). The selected
preview SDK does not copy its bundled `FSharp.Core` runtime into application outputs, so
`Directory.Build.props` applies an explicit, bounded `MSBuildToolsPath` copy workaround. It is not a
version pin and has a documented removal gate.

## Run

```powershell
dotnet build .\Brontide.Minimal.slnx -nologo
dotnet test .\Brontide.Minimal.slnx -nologo --no-build
dotnet run --project .\src\Brontide.Minimal.Host\Brontide.Minimal.Host.fsproj -nologo
.\build\verify-boundaries.ps1
```

The ordinary solution test run executes fixture and boundary tests and skips the foreign-process
cases unless `BRONTIDE_REFERENCE_PROVIDER` names a built endpoint. Run the complete two-way clean gate,
including both real foreign processes, from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

See [`docs/integration-guide.md`](./docs/integration-guide.md) for the binding quick reference.
See [`../docs/current/policies/public-boundaries.md`](../docs/current/policies/public-boundaries.md) for payload, timeout, cleanup,
redaction, replay, and denial-of-service assumptions.

See `docs/milestone-evidence.md` for the implemented first boundary and the Event/Flow, Macro
Operation, mixed-image-workspace, machine, and authority-federation work intentionally deferred.
