# Implementation findings

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
