# Implementation findings

These findings describe retained implementation evidence, including historical Architecture 0.5
results. New architectural decisions come from
[Architecture 0.7](../../Brontide-Architecture-0.7.md); planned Minimal responses are classified in
[`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). A historical finding is not
silently upgraded to 0.7 evidence.

## F# is an architectural constraint, not a façade

Every Brontide Minimal Stack-owned production, host, binding, and test project is F#. Immutable records, maps,
sets, and discriminated unions make the pure transition boundary direct: the same `World`,
`Environment`, and `ExecutionRequest` produce the same observable result.

## Similar wire fields still need distinct semantic types

Actor, Capability, Execution, Occurrence, and Activity references deliberately share scope/value
wire fields. Explicit F# type annotations at issuance sites keep those references distinct instead
of allowing record-field inference to erase the semantic boundary.

## Binding must be data-first

The binding package uses a versioned manifest and a tagged JSON `ShapeValue` codec. It does not
reference Brontide Reference Stack, load another implementation's assemblies, exchange `System.Type`, or treat an
exception as a portable Outcome. This gives the later entanglement experiment a falsifiable seam.

The version-2 experiment has now crossed real process boundaries in both directions. Host authority
remains in `World.step`; request, Execution, and occurrence ids are distinct binding-scoped types;
and the provider receives no Capability. Exact manifest negotiation, required Fragment checks,
semantic failure details, optional Fragment forwarding, and provider-process failure all remain
visible without a shared runtime assembly.

## Canonical projection must not retain unsupported authored Fragments

The Velocity/DirectionalVelocity conformance case exposed that Brontide Minimal Stack's canonical record projection
was retaining every registered Fragment merely because the Shape was open. Openness permits valid
input; it does not claim understanding. `World.projectRecord` now removes unsupported Fragments,
while `projectRecordWithFragments` retains explicitly required Fragments and the portable binding
keeps the original complete value separately when it claims transparent forwarding.

## First interchange is evidence, not ratification

The same Cooling matrix passes with Brontide Reference Stack hosting Brontide Minimal Stack and Brontide Minimal Stack hosting Brontide.Reference. The neutral
binary contract maps natively to Brontide Minimal Stack `SetCoolingEnabled`; Brontide Reference Stack declares its binary-to-fan
Adapter separately. No information promised by the neutral contract is lost, but Brontide Minimal Stack's target
and measured temperature fields are deliberately outside the output projection. This supports the
Component/Shape/authority model while leaving the descriptor vocabulary, universal observation,
machine transport, Capability federation, Event/Flow recovery, bulk resources, and hot-swap open.

## Composition must preserve support and opposition

Provider preferences and opposition are distinct inputs. An opposed provider is rejected visibly;
it is not silently assigned a lower score. Dependency gravity is represented by the four Brontide 0.5
claims: required generic contract, required Profile, preferred system provider, and required
authored provider.

## SDK observation

The installed .NET 10 preview SDK compiles F# without an implicit NuGet `FSharp.Core` package but
does not copy the bundled runtime assembly to application/test outputs. Brontide Minimal Stack copies the selected
SDK's own `FSharp.Core.dll` using `MSBuildToolsPath`. No SDK version or location is pinned.

## Base authority correction changed the public trust boundary

Public record construction made issuer-controlled references forgeable and an ambient grant map
obscured which target and Capability an execution presented. References are now private single-case
unions, Genesis is callback-scoped, and `World.step` is the only public execution path. It validates
initiator, target, presented Capability, operation scope, the full constraint chain, Shape, and
trusted time before dispatch. Delegation can change only the holder and append constraints; parent,
issuer, target, and operation scope remain auditable. Rejections allocate a payload-redacted audit.

This is a breaking correction, not a new Architecture 0.7 feature. `CHANGELOG.md` records the
migration and the Architecture 0.5 evidence matrix anchors the negative and positive tests.

## Second interchange widens evidence without widening claims

The Catalog/resource proof is deliberately unlike Cooling: two Operations share ephemeral provider
state, inputs contain nested records and repeated tags, missing items produce shaped failures, and
the provider must refuse an out-of-scope resource handle before mutation. Strict parsers reject
malformed, unknown, version-skewed, replayed, and oversized lines independently in each stack.

The proof remains local-process experimental evidence. Its resource handle is addressing, not
authority; it does not demonstrate persistence, filesystem dereference, network isolation,
Capability federation, or a ratified portable binding.

## Composite Constraints cannot inherit Boolean short-circuit semantics

- **Brontide sections:** Architecture 0.7 §§10.1, 18.1, 23, and 29.2
- **Minimal scenario:** an `AllOf` or `AnyOf` contains an unsupported atom beside a sibling that
  would ordinarily decide a Boolean result.
- **Observed result:** M1 evaluates all siblings and returns indeterminate with a stable diagnostic;
  `World.step` denies before dispatch and experimental selection excludes the candidate.
- **Classification:** Architecture 0.7 Complete Draft evidence
- **Current disposition:** retain flat Constraint requirements as compatibility leaves, store the
  recursive expression chain inside the private World representation, redact protected values,
  and route exact evidence through the revision-specific matrix. This does not ratify Architecture
  0.7 or complete C2-C5.

## Typed member identity must not overload concept names

- **Brontide sections:** Architecture 0.7 §§6.10 and 22.4
- **Minimal scenario:** parse `Brontide:Editor.Project#Store.Core` into `CanonicalName` or encode the
  provisional member-kind catalogue as a closed union.
- **Observed result:** M2 keeps `CanonicalName` unchanged and adds distinct opaque
  `CanonicalMemberName`, `MemberKind`, and `MemberName` values with ordinal comparison and strict
  delimiter rejection.
- **Classification:** Architecture 0.7 Complete Draft evidence
- **Current disposition:** keep `MemberKind` open because the architecture explicitly leaves the
  catalogue and final glyph provisional. No existing codec carried this form, so the public change
  is additive and has no wire alias or migration. This result does not ratify the grammar or
  complete C3-C5.

## Authored Fragments require a host Shape lineage

- **Brontide sections:** Architecture 0.5 §§16.3-16.4
- **Minimal scenario:** register an authored Fragment for open HostA and attach it to unrelated open
  HostB.
- **Observed result:** the prior model omitted the host lineage and accepted the attachment solely
  because HostB was open.
- **Classification:** Brontide Minimal Stack defect found by independent review
- **Current disposition:** `FragmentDefinition` now requires `HostShape`; registration validates it,
  projection accepts the same compatible lineage or explicit inclusion, and unrelated open hosts
  reject. The required record field is a source-breaking trust-boundary correction documented in
  `CHANGELOG.md`.
