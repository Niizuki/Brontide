# Implementation findings

## F# is an architectural constraint, not a façade

Every Linen-owned production, host, binding, and test project is F#. Immutable records, maps,
sets, and discriminated unions make the pure transition boundary direct: the same `World`,
`Environment`, and `ExecutionRequest` produce the same observable result.

## Similar wire fields still need distinct semantic types

Actor, Capability, Execution, Occurrence, and Activity references deliberately share scope/value
wire fields. Explicit F# type annotations at issuance sites keep those references distinct instead
of allowing record-field inference to erase the semantic boundary.

## Binding must be data-first

The binding package uses a versioned manifest and a tagged JSON `ShapeValue` codec. It does not
reference Fabric, load another implementation's assemblies, exchange `System.Type`, or treat an
exception as a portable Outcome. This gives the later entanglement experiment a falsifiable seam.

## Composition must preserve support and opposition

Provider preferences and opposition are distinct inputs. An opposed provider is rejected visibly;
it is not silently assigned a lower score. Dependency gravity is represented by the four Atlas 0.5
claims: required generic contract, required Profile, preferred system provider, and required
authored provider.

## SDK observation

The installed .NET 10 preview SDK compiles F# without an implicit NuGet `FSharp.Core` package but
does not copy the bundled runtime assembly to application/test outputs. Linen copies the selected
SDK's own `FSharp.Core.dll` using `MSBuildToolsPath`. No SDK version or location is pinned.
