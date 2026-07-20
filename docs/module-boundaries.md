# Implementation module boundaries

Designed for: [Brontide Architecture 0.7](../Brontide-Architecture-0.7.md)

Status: implementation-owned dependency and review map, not new architectural vocabulary.

| Concern | Reference owner | Minimal owner | Public review rule |
| --- | --- | --- | --- |
| Naming and identity | `Brontide.Reference.Core/Names.cs` | `Brontide.Minimal.Model/Model.fs` identity section | Issuer-controlled references expose carry/compare access but no accepted public minting path. |
| Shapes and serialization-neutral values | `Core/Shapes.cs` | `Model.fs` Shape section | Values contain data, never Capabilities or private runtime objects. |
| Authority and constraints | `Core/Authority.cs`, `Constraints.cs` | `Kernel.fs` `Genesis`, `World`, and constraint evaluation sections | Capability issue/delegation and evaluator registration stay behind the owning authority boundary. |
| Execution, Outcome, Event | `Core/AuthorityDomain.cs`, `Interactions.cs` | `Kernel.fs` execution section plus Model execution records | Handler dispatch occurs only after the same fail-closed checks; audits redact rejected payloads. |
| Cooling binding | separate `Experimental.Binding` files | `PortableBinding.fs` plus `CoolingBindingHost.fs` | Experimental; no stack-to-stack assembly reference and no Capability transfer. |
| Catalog transport | `Experimental.Binding/CatalogBinding.cs` | `Binding/CatalogBinding.fs` | Separate file/module per stack, strict protocol, fixed payload limit, independent endpoint and client. |
| Process composition | each `Interchange.Provider` executable | each `Interchange.Provider` executable | Provider programs are composition roots and own semantic state; binding libraries do not select authority or credentials. |

The Catalog correction deliberately introduced a separate transport module instead of enlarging the
Cooling-specific protocol file. Future changes to the older 1,000-line Minimal portable binding
must extract a coherent manifest, value-codec, or process-transport module with its tests in the
same change; line movement alone is not a correction.
