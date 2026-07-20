# Brontide Architecture: Composition Without a Kernel

## Standards, Interoperable Boundaries, and Emergent Foundations

**Status:** Proposed architecture  
**Purpose:** Define the architectural identity of Brontide independently of any particular composer, runtime, operating system, or reference implementation.

---

## 1. Scope

Brontide is a standards and composition model for describing computational parts, determining valid relationships between their provisions and requirements, and preserving interoperability wherever a realized system retains boundaries.

Brontide does not prescribe a universal runtime core. A composer may lower a Brontide description into a realization in which components, bindings, and other Brontide abstractions are partly or entirely erased. Where separateness remains, the retained boundary is the operational expression of the Brontide contract.

This document defines that architectural position and its consequences. In particular, it specifies:

- the relationship between description, composition, realization, and operation;
- the conditions under which Brontide abstractions may be erased;
- the interoperability obligations of retained boundaries;
- the absence of a mandatory Brontide runtime or kernel;
- the meanings of **Substrate**, **Foundation**, and **root requirement**;
- the preservation of composability at foundational levels;
- the status of kernel-like responsibilities and optional runtime facilities; and
- composition conformance and boundary conformance.

This document does not define concrete capability families, a wire encoding, an application model, or a required implementation topology. Those belong in narrower specifications and profiles.

The capitalized terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** express normative requirements in this document. Uncapitalized uses are descriptive.

## 2. Architectural Thesis

Brontide has no single mandatory runtime object in which its identity resides.

Its identity is expressed in two places:

1. **Internally, by composition semantics:** provisions, requirements, constraints, and compatibility rules determine which bindings form a valid realization.
2. **Externally, by interoperable boundaries:** where independently realizable parts remain separated, their interactions preserve a machine-legible contract that other Brontide systems can understand and satisfy.

The internal model may be erased after composition. A retained boundary may not discard the semantics required for interoperability and still claim conformance for that boundary.

Consequently, Brontide is neither a conventional kernel nor merely a deployment orchestrator. It is a common language and compatibility model from which static firmware, applications, distributed systems, and live operating environments may all be realized.

## 3. Terms

### 3.1 Description

A **description** declares computational intent using Brontide concepts. It identifies provisions, requirements, interactions, constraints, parameters, and other information relevant to composition. A description does not, by itself, prescribe process boundaries, transport, placement, privilege level, or deployment topology.

### 3.2 Component

A **component** is a compositional unit that groups provisions, requirements, state, interactions, or implementation material. It does not inherently mean a process, service, container, library, device, or runtime object.

A component may cease to exist as a distinct entity in a realization. Component identity is retained only when a description, profile, boundary, or runtime facility requires it.

### 3.3 Provision and Requirement

A **provision** declares a capability, interaction, resource, or guarantee that can be supplied.

A **requirement** declares a capability, interaction, resource, guarantee, or condition that must be satisfied for a described part or system to be valid in a stated context.

The irreducible relationship in Brontide is not the continued existence of a component, but the valid satisfaction of a requirement by a compatible provision.

### 3.4 Provider and Binding

A **provider** is a concrete or further-composable means of realizing a provision. A provider may be software, generated code, hardware, a host facility, a remote service, another Brontide composition, or an adapter around an existing system.

A **binding** is the selected relationship by which a provision satisfies a requirement. A binding may be realized as a function call, a static memory relationship, an ABI, a message exchange, a hardware signal, a network protocol, or another suitable mechanism. It need not survive as a runtime object.

### 3.5 Composition and Realization

**Composition** is the process of selecting compatible provisions, satisfying requirements, applying constraints and parameters, and determining a valid system.

A **realization** is the executable, deployable, manufacturable, or otherwise operative result of composition. A realization may be a firmware image, a single binary, a set of processes, a device topology, a distributed environment, a generated configuration, or a combination of these.

### 3.6 Retained Boundary

A **retained boundary** is a boundary between parts that remains observable in a realization and across which a Brontide provision or requirement is exposed. The boundary may be internal to one machine or cross processes, machines, trust domains, devices, or organizations.

### 3.7 Profile

A **profile** defines requirements and guarantees for a class of Brontide systems or operational contexts. A profile may require capabilities such as persistence, introspection, isolation, live reconfiguration, or a particular interoperability protocol. A profile SHOULD specify semantic obligations and required guarantees without unnecessarily fixing the providers that must satisfy them.

### 3.8 Operational Context and Root Requirements

An **operational context** is a defined stage or mode in which a system is expected to be operable. Examples include initial boot, recovery, normal interactive operation, offline sensing, and cluster control.

**Root requirements** are the requirements that a description or profile designates as necessary for operation in a particular operational context. Root requirements are contextual. The root requirements for boot need not be the root requirements for steady-state operation, recovery, or an isolated device mode.

## 4. Description, Composition, and Erasure

A Brontide description is not required to remain present in the system it produces. The composer is part of the Brontide toolchain, but it is not necessarily part of the realized system.

Conceptually:

```text
Description + available providers + parameters
                        |
                        v
                    Composer
                        |
                        v
                   Realization
```

The composer MAY:

- inline one component into another;
- replace interactions with direct calls;
- allocate state at fixed locations;
- merge components or split their implementation across artifacts;
- select host facilities and existing services as providers;
- generate dispatch tables, protocols, adapters, or static schedules;
- retain only metadata required by a selected profile; or
- erase all Brontide-specific runtime metadata when no retained obligation requires it.

Such lowering is conformant only if it preserves the behavior, compatibility conditions, constraints, and guarantees required by the valid composition.

Brontide therefore does not require:

- a resident composer;
- a component registry;
- a capability broker;
- a runtime graph;
- a universal invocation mechanism;
- dynamic discovery;
- component identity at runtime; or
- a Brontide daemon.

A profile MAY require any of these facilities for its own purposes. Their absence from Brontide Base is not a prohibition on their use.

### 4.1 Complete Erasure

A sealed device may be produced from a Brontide description as one firmware image with no external interconnect, no runtime metadata, and no separately observable components. In that case, its Brontide origin is a property of how the artifact was composed, not an operationally detectable feature of the artifact.

Such a device may be described as **Brontide-composed** or **Brontide-produced**. It SHOULD NOT be described as exposing a Brontide environment unless it actually preserves the boundaries or runtime facilities implied by that claim.

### 4.2 Partial Preservation

A realization may preserve only selected parts of the model. For example, it may erase its internal component graph while retaining one typed hardware or network boundary. It may preserve stable identities for observability without allowing rebinding. It may retain a live composition graph while using direct calls for local interactions.

Preservation is therefore an explicit property of a profile, composition, or boundary—not an all-or-nothing property of Brontide itself.

## 5. Interoperability at Retained Boundaries

Interconnect is the primary operational invariant of Brontide, but mere byte exchange is insufficient. A Brontide boundary represents a compatible relationship between a provision and a requirement.

A conformant retained boundary MUST preserve enough information or predetermined shared definition to establish:

- the semantic identity of the capability or interaction;
- the interaction form, such as request, event, stream, shared state, signal, or transaction;
- the Shapes and meanings of values crossing the boundary;
- applicable constraints, guarantees, limits, and failure behavior;
- the compatibility and versioning rules used to determine whether connection is valid; and
- any lifecycle, ordering, authority, or trust conditions required for correct use.

The contract MUST be machine-legible either directly at runtime or through an authoritative description available during composition. Runtime self-description is not mandatory.

Brontide does not require one wire format or transport. A semantic interaction may be realized through direct ABI calls, shared memory, a serial bus, messages, network protocols, hardware registers, or other mechanisms. Transport-specific specifications MAY define additional conformance requirements.

A boundary MUST NOT claim compatibility merely because endpoint names or data layouts happen to match. Compatibility follows from the declared semantic contract and the applicable rules.

### 5.1 Realization Independence

Unless a capability or profile explicitly requires otherwise, a boundary contract MUST NOT assume that the other side is:

- in the same process or on the same machine;
- remotely networked;
- dynamically discoverable;
- implemented in a particular language;
- a persistent runtime component; or
- internally composed using Brontide.

This permits an existing non-Brontide system to participate through a conformant adapter and permits a fully Brontide-composed system to erase its internal abstractions while retaining interoperable edges.

## 6. No Mandatory Runtime Core or Kernel

Brontide defines no universal resident core and no mandatory kernel topology.

This does not remove the responsibilities conventionally associated with a kernel. Meaningful systems still require some combination of execution, scheduling, memory and resource management, device access, isolation, trust, interaction transport, lifecycle, time, and failure handling. Brontide permits these responsibilities to be supplied by separate providers, combined where appropriate, delegated to a host, statically generated, or made unnecessary by a particular realization.

Illustratively:

| Responsibility | Possible realizations |
| --- | --- |
| Execution and scheduling | Static loop, host processes, language runtime, dedicated scheduler, cluster executor |
| Memory and resources | Compile-time allocation, host virtual memory, allocator component, hardware-managed regions |
| Interaction | Direct calls, generated dispatch, shared memory, IPC, device bus, network router |
| Device access | Host drivers, user-space managers, firmware providers, direct hardware bindings |
| Isolation and authority | None, cooperative checks, process isolation, capability hardware, hypervisor, remote trust domain |
| Lifecycle and recovery | Fixed startup, bootstrap service, supervisor, replicated controller, external operator |

The names above are illustrative capability areas, not a required standard taxonomy.

Providers of foundational responsibilities MAY be co-located or privileged in a particular implementation. Brontide does not pretend that privilege, hardware protection, or roots of trust disappear. It requires that such properties be treated as realization facts and compatibility guarantees rather than inferred from membership in a universally privileged Brontide class.

The phrase **composition without a kernel** therefore means that Brontide does not define itself by a single mandatory nucleus. It does not mean that every realization lacks a conventional host kernel, privileged code, or a trusted base.

## 7. Substrate

The **Substrate** is the lower-level execution reality upon which a particular Brontide realization depends from a stated point of view.

A Substrate may include:

- hardware and firmware;
- Linux or another host operating system;
- a hypervisor;
- a WebAssembly host;
- a browser execution environment;
- a language runtime;
- an external device platform; or
- facilities produced by another Brontide composition.

Substrate is a relative term, not a fixed architectural layer or a class of permanently opaque machinery. What one composition treats as its Substrate may be represented as providers, requirements, and replaceable bindings in a wider or lower-level composition.

For example, a desktop composition may treat Linux process and memory facilities as its Substrate. A host-platform composition may instead describe alternative execution and memory providers and make Linux only one possible realization. A bare-metal composition may expose previously implicit firmware and hardware dependencies as explicit requirements.

A realization MAY leave all or part of its Substrate outside the Brontide model. This limits Brontide's ability to reason about or replace that part, but does not invalidate the rest of the composition. Where substitution matters, the relevant substrate facility SHOULD be surfaced through a provision, requirement, or adapter with honest guarantees.

Replacing a Substrate is never implied merely by naming it composable. Replacement is possible only where contracts, available providers, trust, bootstrap conditions, state transfer, and physical constraints permit it. The architecture MUST permit such replacement to be expressed; an individual realization need not support it dynamically or at all.

## 8. Foundation

The **Foundation** of a Brontide system is the effective set of selected providers and transitive dependencies necessary to satisfy the root requirements of a stated operational context.

For an operational context `O`:

```text
Root requirements R(O)
        |
        v
Selected bindings and providers
        |
        v
Transitive dependencies needed to keep R(O) satisfied
        =
Foundation F(O)
```

Foundation is an emergent property of a realized dependency graph. It is not:

- a mandatory package or binary;
- a universal list of components;
- a permanent architectural layer;
- a special provider class;
- necessarily Brontide-native;
- necessarily resident at runtime; or
- necessarily identical across operational contexts.

A static schedule, fixed memory layout, host kernel service, hardware device, generated binding, and ordinary Brontide component may all participate in a Foundation. Conversely, an apparently system-like component is not foundational if the relevant operational context does not depend on it.

### 8.1 Contextual and Temporal Character

Foundation MUST be discussed with its context made explicit. A system may have different Foundations during:

- secure initialization;
- normal operation;
- disconnected operation;
- recovery;
- upgrade or recomposition; and
- handoff from one environment to another.

A bootstrap provider may be foundational during startup and absent afterward. A dynamic router may be foundational in one realization and replaced by generated direct bindings in another. A remote identity authority may be foundational while connected but unnecessary in an explicitly supported offline mode.

Changes to the Foundation MAY occur at build time, composition time, boot, handoff, failover, or runtime. Brontide Base guarantees none of these modes of change. A profile that requires live change MUST define the necessary continuity, authority, failure, and state-transfer semantics.

### 8.2 Foundation and Substrate Are Different Views

| | Substrate | Foundation |
| --- | --- | --- |
| Meaning | Lower-level execution reality assumed from a stated viewpoint | Providers and dependencies currently necessary to satisfy contextual root requirements |
| Derived from | Realization boundary and implementation viewpoint | Realized requirement/provision graph and operational context |
| Necessarily Brontide-described | No | No, although its role is identifiable through the composition model |
| Fixed | No | No |
| Replaceable | Where surfaced contracts and reality permit | By recomposition or supported rebinding where alternatives satisfy the same obligations |
| May include the other | A substrate facility may be foundational | A foundation provider may rely on or expose substrate facilities |

Neither term creates a protected architectural territory. They describe relationships within a particular analysis.

## 9. Composability at Foundational Levels

Brontide's composition rules apply at foundational levels as far as a description chooses to expose them. The standard MUST NOT reserve foundational responsibilities to one canonical runtime, vendor, host system, or implementation technique when their required semantics can be expressed as provisions, requirements, constraints, and guarantees.

To preserve this property:

1. **Profiles SHOULD specify obligations before implementations.** A profile should require isolation strength, scheduling behavior, persistence guarantees, device semantics, or interoperability protocols rather than naming one provider without necessity.
2. **Foundation providers remain providers.** They may have extraordinary trust or privilege, but they participate through declared contracts and may have alternative implementations.
3. **Dependencies remain explicit.** A provider of a foundational capability may declare its own requirements. Its position does not terminate composition by definition.
4. **Trust and privilege are guarantees and constraints.** They MUST NOT be silently inferred from labels such as `system`, `core`, or `kernel`.
5. **Replacement modes remain distinct.** Selection among alternatives at initial composition is a Base concern. Boot-time selection, failover, hot replacement, and live migration require additional capabilities and MUST NOT be implied by static composability.
6. **Bootstrap assumptions are admitted honestly.** Every concrete realization eventually reaches hardware, pre-existing software, externally supplied authority, or fixed generated behavior. A composition may treat this as a seed or Substrate; a wider composition may choose to model it.

This rule does not require implementers to reinvent mature facilities. It preserves the right and architectural ability to do so.

## 10. Reuse, Replacement, and Reinvention

Brontide expects implementations to reuse existing wheels where they are suitable. A Linux process, established database, device protocol, browser engine, cryptographic library, or managed cloud service may be an excellent provider.

Reuse MUST NOT make a particular wheel part of Brontide's universal identity unless interoperability genuinely requires the standardized artifact or protocol. The distinction is:

- a **composition** may deliberately select or fix one provider;
- a **profile** may require a concrete protocol or guarantee when necessary for interoperability;
- the **Brontide model** must still permit an independently implemented provider to satisfy a requirement wherever the contract admits one.

An implementation is not required to offer every alternative, nor to support replacing a selected provider in a running system. It MUST NOT falsely claim substitutability where hidden assumptions make replacement invalid.

This yields the governing principle:

> Reuse is expected; replacement is expressible; reinvention is permitted; compatibility decides.

## 11. Optional Runtime and Platform Facilities

Facilities that make Brontide into a live operating environment are ordinary capability families or profile requirements, not axioms of Brontide Base.

Possible facilities include:

### 11.1 Bootstrap

Discovery, verification, loading, initial binding, activation, recovery selection, and handoff. Bootstrap may be external, firmware-resident, generated, distributed, or absent in a statically complete realization.

### 11.2 Platform and Firmware Services

BIOS-like or firmware-like services may initialize hardware, enumerate devices, expose platform information, establish trust, and hand control to later providers. Brontide does not mandate a BIOS architecture or require these responsibilities to remain available after handoff.

### 11.3 Connectors and Adapters

Connectors may preserve Brontide contracts across transports, trust domains, physical buses, legacy systems, or other standards. An adapter may make a system boundary-conformant without making the adapted system internally Brontide-composed.

### 11.4 Introspection and Live Composition

A runtime environment may retain component identities, the realized graph, capability metadata, binding state, and provenance. It may provide inspection, resolution, activation, deactivation, rebinding, relocation, or recomposition.

These operations require explicit authority, consistency, lifecycle, state, and failure semantics. The presence of a graph does not by itself make mutation safe.

### 11.5 Supervision and Routing

Lifecycle supervision, interaction routing, discovery, policy enforcement, and recovery may be supplied by one or several providers. A composer may instead lower some or all of these functions into static behavior.

No facility in this section is mandatory unless required by an applicable profile or composition.

## 12. Brontide Operating Environments

A **Brontide operating environment** is a profile or realized system that deliberately preserves and operates selected Brontide semantics at runtime.

Such an environment might require:

- discoverable component or capability identity;
- stable, inspectable bindings;
- lifecycle management;
- mediated authority and policy;
- runtime capability resolution;
- replaceable or relocatable providers;
- state coordination;
- presentation surfaces; or
- human and agent operation through the same typed interactions.

These are higher-level guarantees, not retroactive requirements on every Brontide-composed artifact. A static firmware image can be Brontide-composed without being a Brontide operating environment. A Linux-based desktop can be a Brontide operating environment while relying on Linux as part of its Substrate and Foundation.

An operating environment may develop a kernel-like concentration of privilege in a particular implementation. That topology is a realization choice, not a definition of Brontide. Reference implementations SHOULD decompose such responsibilities into meaningful providers where practical and SHOULD keep their contracts available for alternative realization.

## 13. Conformance

Brontide distinguishes at least two independent forms of conformance.

### 13.1 Composition Conformance

A composition process or its result is **composition-conformant** when:

- its provisions and requirements are interpreted according to the applicable Brontide specifications;
- every required binding is valid under the applicable compatibility rules;
- declared constraints, parameters, and profile obligations are satisfied;
- lowering preserves required observable behavior and guarantees; and
- the conformance claim identifies the relevant Brontide version and profile, where applicable.

Composition conformance may be established through tool behavior, validation records, provenance, tests, certification, or other evidence defined by a profile. It may be impossible to infer from the final artifact alone.

### 13.2 Boundary Conformance

A retained boundary is **boundary-conformant** when:

- it exposes or is governed by an authoritative Brontide contract;
- its capability and interaction semantics match that contract;
- exchanged values satisfy the required Shapes and meanings;
- it follows the applicable compatibility, versioning, lifecycle, and failure rules; and
- its actual guarantees are no weaker than those it declares.

Boundary conformance is scoped to named boundaries. It does not imply that the system behind those boundaries was internally composed using Brontide.

### 13.3 Valid Combinations

The following combinations are valid and useful:

| System | Composition conformance | Boundary conformance |
| --- | --- | --- |
| Sealed compiled device with no retained Brontide boundary | Yes | Not applicable |
| Legacy service exposed through a Brontide adapter | Not necessarily | Yes, for the adapter's declared boundaries |
| Compiled device with an interoperable Brontide edge | Yes | Yes |
| Live Brontide operating environment | Yes | Yes for its declared boundaries |

Conformance claims SHOULD be specific. “Brontide-compatible” without identifying whether the claim concerns a description, composition, profile, provider, or boundary is insufficient for rigorous interoperability.

## 14. Security, Trust, and Failure

The absence of a mandatory kernel does not imply the absence of a trusted foundation, privileged providers, protection boundaries, or single points of failure.

A description or profile concerned with security or resilience MUST express the guarantees it requires, including where relevant:

- the authority that selects or changes bindings;
- the mechanism enforcing isolation;
- roots of identity and trust;
- failure containment and recovery expectations;
- integrity and authenticity of descriptions, providers, and composition plans;
- state continuity during replacement or handoff; and
- the consequences of losing a foundational provider or substrate facility.

Two providers may expose the same broad capability while offering materially different enforcement or failure guarantees. Compatibility MUST account for those differences when the requirements depend on them.

## 15. Illustrative Realizations

### 15.1 Sealed Microdevice

A sensor and display are composed into one firmware image. Interactions become direct calls, memory is statically allocated, and component identity is erased. The device has no external Brontide boundary.

It is composition-conformant if produced under the Brontide rules, but Brontide is not operationally observable after production.

### 15.2 Interconnected Device

The same device exposes a typed temperature provision over a serial bus. Its internal graph remains erased, but the serial edge preserves the capability semantics, Shape, units, update behavior, and compatibility rules.

The device may be both composition-conformant and boundary-conformant.

### 15.3 Linux-Based Operating Environment

Linux supplies process isolation, virtual memory, drivers, and other host facilities. Brontide providers supply live capability resolution, identity, lifecycle, state, and surfaces. Some host facilities are treated as Substrate; those on which normal operation depends also participate in the Foundation for that context.

Another composition could expose alternative execution or device providers. Linux is an implementation choice, not a universal Brontide requirement.

### 15.4 Bare-Metal Reinvention

A project implements scheduling, memory, device access, and interaction providers directly on hardware. It may reuse standard hardware interfaces or create new ones. Its providers are compatible when they satisfy the same required semantics and guarantees, not because they reproduce the internal structure of a reference implementation.

### 15.5 Live Recomposition

An environment retains a composition graph and provides authorized rebinding. During a controlled handoff, one interaction router is replaced by another while continuity constraints remain satisfied. The Foundation changes with the realized dependency graph.

This is a capability of that environment, not a behavior guaranteed by Brontide Base.

## 16. Non-Goals

This architecture does not require:

- every system to retain Brontide metadata;
- every internal interaction to use a standard wire protocol;
- every component to remain separately identifiable;
- all providers to be dynamically replaceable;
- every Substrate to be modeled or replaceable in the current composition;
- foundational providers to be unprivileged;
- replacement to be safe without explicit lifecycle and state semantics;
- existing operating systems or mature infrastructure to be reimplemented; or
- one reference implementation to define the only valid topology.

## 17. Architectural Tests

Future Brontide specifications and reference implementations SHOULD be tested against the following questions:

1. Can the described semantics be lowered into a static realization without a resident Brontide runtime when the selected profile permits it?
2. If a boundary remains, is its semantic contract sufficient for an independently realized participant to determine compatibility?
3. Does a proposed Base feature require one implementation topology when a capability or profile could express the requirement instead?
4. Is a claimed Foundation derived from explicit root requirements and an operational context, or has it become a fixed layer by convention?
5. Is a claimed Substrate genuinely outside the current composition viewpoint, and could a wider composition expose it through providers and requirements?
6. Are trust, privilege, isolation, and failure properties declared rather than inferred from system-like names?
7. Does the model distinguish static selection from boot-time, failover, or live replacement?
8. Can an existing implementation be reused through a provider or adapter?
9. Can an alternative implementation satisfy the same contract without reproducing the reference implementation's internals?
10. Is each conformance claim scoped to the relevant description, composition, provider, profile, or boundary?

## 18. Summary

Brontide is a common language and compatibility model for composing computational systems. Its descriptions establish valid relationships between requirements and provisions. A composer may lower those relationships into a realization that retains all, some, or none of Brontide's source-level structure.

Where a boundary remains, Brontide persists as an interoperable semantic contract. Where no boundary or live model remains, Brontide may survive only as composition provenance.

There is no mandatory Brontide runtime and no universal kernel. Kernel-like responsibilities are supplied by providers, host facilities, hardware, generated behavior, or combinations of them. The **Substrate** names the lower-level execution reality assumed from a stated viewpoint. The **Foundation** is the contextual, emergent set of providers and dependencies required to satisfy root requirements in a stated operational context. Neither is a fixed layer, and neither ends composition by definition.

Brontide implementations may rely on existing wheels, but Brontide's architecture does not make those wheels irreplaceable. Reuse is expected; replacement is expressible; reinvention is permitted; compatibility decides.
