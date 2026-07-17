# ATLAS

## Architecture 0.4

An Introduction to the Atlas Computational Model

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

**Status:** Early Draft
**Version:** 0.4 (see §35 for changes from Architecture 0.2 and §36 for changes from Architecture 0.3)

---

## Contents

1. Introduction
2. One Model Across Different Kinds of System
3. Why Atlas Base Is Small
4. The Core Idea
5. The Maximal Atlas Environment
6. Design Principles
7. Atlas Base
8. Authority Domains
9. Actor
10. Capability
11. Delegation
12. Genesis: The Origin of Authority
13. Operation, Execution, and Interaction
14. Event and Outcome
15. Origin: Provenance of Effect
16. Shape: Structure Across Implementations
17. A Minimal Atlas System
18. Growing Beyond the Base
19. Architectural Extensions: Flow and Event Distribution
20. Profiles
21. Domain Vocabularies
22. Names, Authorship, and Declaration Prefix Blocks
23. Versioning and Ratification
24. Devices and Atlas
25. Systems and Macro-Scale Operations
26. Admission: Interacting with Bounded Capacity
27. Atlas and Existing Systems
28. Threat Model
29. Conformance
30. Atlas, Fabric, and Linen
31. Related Work
32. Authors' Discussion: The Larger Direction
33. Open Questions
34. Summary
35. Changes from 0.2
36. Changes from 0.3

---

## 1. Introduction

Computing no longer fits neatly inside a computer.

A modern computational environment may contain programs, services, embedded controllers,
peripheral devices, remote machines, human users, and autonomous systems. These participants
differ radically in implementation, scale, latency, and functional scope, yet they increasingly need to
cooperate.

A person may ask a software system to perform work.
A software system may delegate part of that work to another service.
An autonomous system may request human intervention.
A peripheral may expose functionality to a workstation.
A workstation may use computational resources located in another room or another administrative
domain.
An entire organisational system may request a database migration, initiate an audit, approve a
deployment, or begin another operation whose meaning is much larger than any single process or
API call used to realise it.

Today, these interactions are represented through many largely separate architectural models.

Programs execute as processes.
Users authenticate through account systems.
Devices expose driver interfaces.
Services communicate through APIs.
Remote systems use network protocols.
Automated agents invent their own tool and delegation models.
Large business and operational actions are often represented only indirectly through endpoints,
messages, workflows, and conventions understood by the systems involved.

The boundaries between these systems are often historical rather than fundamental.

Atlas begins with a different question:

> *What is the smallest common computational model through which radically different participants
> can cooperate?*

Atlas proposes an actor-centric and capability-based architecture centred on one principle:

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

An Actor is not defined by how it is implemented.

A process may be an Actor.
A firmware subsystem may be an Actor.
A peripheral may expose one or more Actors.
A human may participate through an Actor.
An autonomous agent may be an Actor.
An organisational or composite system may participate through an Actor.

Several processes may collectively realise one Actor, while a single process may contain many
Actors.

Atlas does not claim that these participants are equivalent.
It proposes that they can share a common architectural model for authority, delegation,
Operations, and observable occurrences.

An **Operation** is a stable semantic contract saying, in effect, *cause this*. An **Execution**
is one concrete attempt to execute an Operation. An Execution may be rejected, accepted, fail, or
succeed; its existence does not assert that the requested effect began. An Event is an immutable,
attributable assertion, and an Outcome is a terminal Event for an identifiable Execution or
activity.

An Operation says *cause this*.
An Execution says *this Operation was attempted like this*.
An Event says *this happened*.
An Outcome says *this Execution or activity ended like this*.

Executions, Events, and Outcomes compose a common named and versioned structural fragment called
**Interaction**. Interaction supplies attribution, identity, correlation, causation, origin, and
optional temporal placement where present. It is not an occurrence, superclass, or additional
Base primitive, and composing it does not make the distinct occurrence semantics interchangeable.
Replaying an Event repeats an assertion; replaying an Execution may repeat an effect. An Event
carries no authority for an observer to react. An Execution is evaluated using an explicit
Capability.

Values crossing an Atlas boundary are described by Shapes. A Shape is a named, versioned
abstract structural contract that allows independently implemented participants to agree on
complete input and output structures, Event assertions, Outcome results, and Constraint values
without requiring the same language types, object model, or wire encoding. A Shape is never the
value itself. Every Operation declares separate input and output Shapes that evolve independently.

Likewise, Atlas does not prescribe the scale of an Operation.

An Operation may represent a register-level hardware action, a device configuration change, a
database migration, an audit request, or another semantically meaningful action performed by a
larger system. One Execution of that Operation may be realised trivially or may involve a
substantial graph of Actors, Delegations, Operations, and further Executions.

Longer-lived exchange is defined by the first-party `Flow` Architectural Extension. A Flow is an
ongoing relationship that orders and governs a sequence of Executions, Events, Outcomes, or
extension-defined Items. Atlas Base does not understand Flow; Flow is expressed entirely in Base
terms.

Authority is represented by Capabilities.
Authority may be restricted.
Where permitted, authority may be delegated.
Authority never appears from nowhere: it is created at known moments (Genesis) and only narrows
as it flows (Delegation).

This common model is intended to make interoperability possible across systems that would
otherwise require separate integration mechanisms.

Atlas is an open specification.

It is not a kernel and it is not an operating system. It defines a computational architecture
that may be implemented by firmware, runtimes, operating systems, distributed environments, or
future systems designed around Atlas directly.

The first implementation of Atlas is named **Fabric**. A second, independently implemented full
stack named **Linen** is planned to test whether Atlas components are genuinely composable and
interchangeable rather than merely compatible with Fabric's internal model.

Fabric and Linen exist to explore and validate Atlas.
Neither implementation is the definition of Atlas.

## 2. One Model Across Different Kinds of System

Atlas is intended to describe systems at radically different scales.

At one extreme, an Atlas implementation may be a small microcontroller.
It may contain several Actors represented by static structures and direct function dispatch.

At another extreme, an Atlas environment may span personal devices, servers, peripherals, remote
infrastructure, human participants, autonomous computational systems, and organisational
services.

These systems are not expected to use the same implementation machinery.
They are expected to preserve the same architectural relationships.

Consider five Atlas relationships and occurrences:

```
CoolingController
    executes Fan.SetSpeed

TemperatureSensor
    emits Sensor.Temperature.Changed

MousePointer
    emits Input.Pointer.Motion

BuildAgent
    derives a Capability permitting Deployment.Approve
    for an authorised human Actor

OperationsSystem
    executes Audit.Start
```

The first two may occur entirely within firmware.
The pointer interaction may cross a wireless connection between a peripheral and workstation.
The approval interaction may span several machines and involve a human participant.
The audit Operation may initiate work across many services, people, data stores, and systems.

Atlas attempts to describe all five without defining one as the normal case and the others as
special adaptations.

This is central to the Atlas model.

> *The implementation context and scale of an Actor or Operation may change. Their participation
> in the authority model does not.*

Atlas therefore aims to provide an architectural meeting point between systems that are currently
integrated through unrelated mechanisms.

## 3. Why Atlas Base Is Small

Atlas was motivated primarily by large and heterogeneous systems.

Distributed personal computing, cooperating devices, remote resources, humans and autonomous
systems participating in common workflows, semantically meaningful system-level operations, and
modular operating systems all contributed to the discussion that produced Atlas.

Atlas Base nevertheless begins with none of these as mandatory features.

This is deliberate.

A useful architecture should not require a workstation merely to describe authority between two
components.

A mouse should be able to implement Atlas.
A pair of headphones should be able to implement Atlas.
A microcontroller with no operating system, no network, no virtual memory, and very little memory
should be capable of participating in the same architectural model.

This requirement is called the **Embedded Test**.

Atlas Base should remain implementable meaningfully on a small microcontroller without requiring:

- virtual memory,
- dynamic memory allocation,
- networking,
- persistent storage,
- multiprocessing,
- pre-emptive scheduling,
- a filesystem,
- or a human-facing interface.

The Embedded Test is not intended to define the ideal Atlas system.
It is a test of architectural necessity.

Every concept in this document has been checked against it. Where a concept is stated as required
(MUST), a static compile-time structure satisfies it. Where a concept requires dynamic machinery,
it is stated as optional (MAY/SHOULD) or deferred to an extension.

A workstation may require identity, discovery, resource selection, distributed communication,
presentation, and execution placement.
A large Atlas environment may treat multiple devices and remote resources as one cooperating
computational environment.
An organisational Atlas environment may expose migration, audit, deployment, approval,
reconciliation, or recovery as explicit semantic Operations, with their authority represented by
Capabilities, rather than leaving both meaning and authority implicit in implementation-specific
APIs.

These facilities are not secondary to the purpose of Atlas. They are among the principal
systems Atlas is intended to enable.

They are excluded from Base only because the Base architecture should contain concepts that
remain fundamental at every scale.

Atlas grows through modular specifications layered on top of that common core.

## 4. The Core Idea

Consider a simple embedded controller.

It contains a temperature reader, a cooling controller, and an emergency handler.
The cooling controller may read the temperature and change the fan speed.
The emergency handler may stop the fan or trigger a safe state.

These parts may all execute within the same firmware image and the same cooperative event loop.
Traditional system boundaries such as processes and users may not exist.

Atlas describes the system through Actors and authority.

```
SensorReader
    may execute Temperature.Read
    may emit Sensor.Temperature.Changed

CoolingController
    may execute Temperature.Read
    may execute Fan.SetSpeed

EmergencyHandler
    may execute Fan.Stop
```

The implementation may represent these rules through static tables compiled into firmware.

Now consider a larger system.

```
CodeAgent
    may execute Repository.Read
    may execute Repository.Comment

BuildSystem
    derives a Capability permitting Repository.Read
    for CodeAgent

CodeAgent
    requests BusinessIntent.Clarify
    from an eligible Actor

HumanActor
    satisfies BusinessIntent.Clarify
```

The CodeAgent may be backed by a model invocation running on a remote machine.
The BuildSystem may be several processes.
The HumanActor may be realised through a workstation, phone, or other interaction endpoint.

Now consider a system-level Operation:

```
OperationsSystem
    holds Capability MigrationGrant
        permits Database.Migrate

OperationsSystem
    executes Database.Migrate
        using MigrationGrant
```

The Operation may internally require:

```
SchemaValidator
    executes Schema.Validate

BackupSystem
    executes Backup.Create

DeploymentController
    holds Capability MigrationExecutionGrant
        permits Migration.Execute

DeploymentController
    derives WorkerMigrationGrant
        from MigrationExecutionGrant
        adding permitted-operation: Migration.Execute
        for MigrationWorker

MigrationWorker
    executes Migration.Execute

AuditRecorder
    executes Audit.Record
```

Atlas does not require the initiating Actor to model these internal steps as part of the original
request.

`Database.Migrate` remains one semantically meaningful Operation. Its Execution at the target
boundary may recursively involve additional Actors, Capabilities, Delegations, Operations,
Executions, Events, and Outcomes.

This is the central distinction in Atlas:

> *Implementation machinery is local to the system. Actor, authority, contract, and occurrence
> boundaries are architectural.*

## 5. The Maximal Atlas Environment

The Embedded Test defines the minimum environment in which Atlas must remain meaningful.
It does not define the upper ambition of Atlas.

A maximal Atlas implementation may expose a computational environment containing many cooperating
systems:

```
Actors:
    Human
    BuildAgent
    CodeReviewAgent
    OperationsSystem
    AuditSystem
    MusicController
    MousePointer
    HeadsetController

Devices and systems:
    Workstation
    Laptop
    Phone
    Headphones
    Mouse
    Home server
    Remote compute
    Corporate infrastructure

Available functionality:
    computation
    presentation
    input
    storage
    sensors
    specialised accelerators
    organisational operations
```

An Actor may receive authority on one device and use it through functionality realised
elsewhere.

A human Actor may initiate work from a laptop.
The resulting Execution may occur on a workstation.
An automated Actor may inspect the result.
That Actor may request intervention from another Actor with authority to clarify business intent.
The responding Actor may happen to be human.

A mouse may expose standard Input Operations and Events directly.
A headset may expose standard Audio, Input, and Sensor Operations and Events without requiring the host
system to understand the manufacturer's internal software architecture.

A corporate system may expose:

```
Database.Migrate
Audit.Start
Audit.Cancel
Deployment.Begin
Deployment.Rollback
Accounting.ClosePeriod
```

These Operations may be requested by human or non-human Actors possessing the required
Capabilities.

The semantic identity of the Operation is visible to the Atlas environment.
Its authority requirements can therefore be explicit.
Its Delegation history can be inspected where the implementation preserves provenance.
Its Outcome can be represented according to the relevant specification.
Its implementation may involve hundreds of conventional API calls without reducing the
architectural Operation to those calls.

In such an environment, the boundary of a physical device is not assumed to be the natural
boundary of computation. Likewise, the boundary of a process or service API is not assumed to be
the natural boundary of semantic action.

Local and remote systems may differ substantially in latency, trust, availability, cost, or
bandwidth. Small and large Operations may differ substantially in duration, consequence,
reversibility, or implementation complexity.

Atlas does not require implementations to hide these differences.
Instead, higher-level Atlas specifications may describe them explicitly and allow systems to
reason about them.

The Composition direction (§18.1) treats topology, latency, cost, capacity, availability, and
related properties as explicit selection characteristics of Components and their bindings. `Local`
and `remote` are useful projections of those characteristics for a particular observer, not the
two architectural kinds of computation.

This larger environment is not a bolt-on use case for Atlas.
It is one of the principal motivations for defining a common Actor, authority, contract, and
occurrence model in the first place.

The purpose of a minimal Base is to allow this environment to be assembled from coherent
architectural parts rather than defining a workstation, network, enterprise workflow engine, or
distributed runtime as the universal foundation.

## 6. Design Principles

### 6.1 Actors are universal participants

An Actor is a participant in the Atlas authority model.

Actor is intentionally broader than conventional operating system concepts such as process or
user.

An Actor may be realised by:

- a firmware subsystem,
- a state machine,
- a conventional program,
- a service,
- a process,
- several cooperating processes,
- a device controller,
- a peripheral,
- a human interaction endpoint,
- an autonomous system,
- a composite or organisational system,
- or another computational participant.

A physical device is not necessarily identical to one Actor.

A mouse may expose separate Actors for pointer interaction and configuration.
A workstation may host thousands of Actors.
A human may participate through one persistent Actor or several context-specific Actors,
depending on specifications outside Atlas Base.
A corporate platform may present one Actor at a defined architectural boundary while internally
containing thousands of services and Executions.

Atlas does not require these participants to be treated as identical.
An implementation may expose relevant characteristics through extensions or policies.

The Base architectural relationship remains common:

> *An Actor participates by presenting explicitly available Capabilities.*

### 6.2 Authority is explicit

An Actor does not gain authority merely because it runs in a particular place.

Execution inside a process, user session, machine, trusted network, or organisational system is
not itself an Atlas grant of authority.

Authority is represented through Capabilities.
An Actor may present only Capabilities available to it.

This does not require every implementation to use sophisticated runtime checks.

A microcontroller may enforce authority through its static structure.
A general-purpose operating system may validate Capabilities dynamically.
A distributed system may use cryptographically verifiable authority.

All may implement the same Atlas semantics.

### 6.3 Authority is bounded

A Capability grants defined authority.
It does not grant general access to the system surrounding its target.

For example, authority to stop a motor does not imply authority to change its speed.

```
Motor.Stop
```

is not equivalent to:

```
Motor.Control
```

Likewise:

```
Audit.Start
```

does not necessarily imply:

```
Audit.Cancel
Audit.ModifyScope
Audit.DeleteRecord
```

The semantic size of an Operation does not make its authority broader.

Boundedness extends to the *presentation* of an Execution as well as its effect: authority to
execute an Operation does not include authority to present that Execution as coming from a
particular kind of source (see §15, Origin). Masquerade is amplification.

The architectural requirement:

> *Possession of authority must not imply authority outside its effective bounds.*

How bounds are expressed and evaluated is defined in §10 and §11.

### 6.4 Delegation is fundamental

Systems routinely act on behalf of other systems.

A user starts a program.
A service calls another service.
A controller authorises a subsystem.
An autonomous Actor asks another Actor to complete part of a task.
An operations platform authorises a migration worker.
An organisation authorises a system to begin an audit.

Most computing platforms represent these relationships through several unrelated mechanisms.
Atlas treats Delegation directly.

An Actor that possesses delegable authority may derive narrower authority for another Actor.

```
Actor A holds Capability MotorGrant:
    permitted-operations:
        Motor.SetSpeed
        Motor.Stop

Actor A derives Capability EmergencyGrant:
    from: MotorGrant
    for: Actor B
    adding:
        permitted-operation: Motor.Stop
```

Actor B may stop the motor.
Actor B does not gain `Motor.SetSpeed`.

The same rule applies at larger scale.

```
OperationsSystem holds Capability DatabaseGrant:
    permitted-operations:
        Database.Migrate
        Database.Backup.Create
        Database.Restore

OperationsSystem derives Capability CustomerMigrationGrant:
    from: DatabaseGrant
    for: MigrationController
    adding:
        permitted-operation: Database.Migrate
        target: CustomerDatabase
        maximum-version: 42
```

The MigrationController gains authority for the delegated migration.
It does not gain unrestricted database administration.

> *Delegation must not increase effective authority.*

Atlas considers this constraint fundamental enough to belong in the Base architecture. §11
defines the structural rule that guarantees it.

### 6.5 Actors are not runtime contexts

An Actor is not a synonym for a process, thread, service, task, or machine.

On an embedded device, several Actors may be represented by a single firmware image.
On a larger system, one Actor may be realised by several processes, services, or runtime contexts.
An Actor may persist while the machinery currently realising it is replaced.
A large Actor may represent a stable system boundary while its internal implementation changes
completely.

These behaviours are not required by Atlas Base.
The distinction exists so that Atlas does not inherit the execution model of its first
implementation.

The binding between an Actor and the runtime machinery realising it is owned by the implementation,
and it is a security boundary: when that machinery is replaced, existing
Capabilities held by that Actor must not silently transfer to an unrelated successor.

Atlas does not require that machinery to be dynamically replaceable. If an implementation retains
the same Actor reference while replacing the machinery realising it, the replacement MUST preserve
that Actor's identity, declared Atlas contracts, and authority relationships. If those properties
cannot be preserved, the implementation MUST expose a successor Actor or an explicit rebinding
rather than silently presenting replacement as continuity. State transfer, quiescence, rollback,
and the treatment of in-progress Executions are not supplied by Actor identity; a Component
claiming hot-swappability must declare those semantics separately (§18.1).

### 6.6 Operations are scale-agnostic

Atlas does not distinguish between "small" and "large" Operations at the Base architectural
level.

The following may all be Operations:

```
Register.Read
Fan.Stop
Input.Pointer.Motion
File.Convert
Database.Migrate
Audit.Start
Deployment.Rollback
```

Their duration, complexity, consequence, and implementation differ radically.

Their common architectural property is that each defines semantically meaningful activity which
an Actor may attempt through an authorised Execution.

An Execution may be realised through one primitive action.
It may also be realised through a large internal system involving many Actors, Operations, and
further Executions.
Atlas does not require the internal implementation graph to be exposed at every architectural
boundary.

A system may therefore expose:

```
Audit.Start
```

without requiring the requesting Actor to individually orchestrate:

```
Evidence.Collect
Reviewer.Assign
Scope.Validate
Record.Create
Notification.Send
```

The system implementing `Audit.Start` remains responsible for preserving the authority semantics
of all Atlas Executions within the implementation where Atlas applies.

This scale independence is intentional.

Atlas seeks to describe meaningful computational action rather than equating architectural
Operations with machine instructions, function calls, API requests, or individual messages.

### 6.7 Interoperability should follow semantics

Atlas aims to allow independently implemented systems to cooperate through common architectural
and domain semantics.

A mouse should not need a vendor-specific host application merely to expose standard pointer
configuration. A headset should not need a manufacturer's private software environment merely to
expose standard audio or sensor functionality.

Likewise, an automated Actor and a human Actor should not require completely unrelated Delegation
models merely because their implementations differ.

A system exposing `Database.Migrate` should be able to communicate more semantic information than
a generic opaque API endpoint named `/execute`.

Shared names without shared Shapes are likewise insufficient. Two components do not implement the
same `Fan.SetSpeed` contract if one accepts a scalar speed and the other requires an unrelated
record, even when both print the same Operation name in an inspector.

Atlas does not attempt to eliminate specialised functionality.
It provides a standard semantic space in which common functionality may be represented directly.
Non-standard functionality remains possible and visibly non-standard.

### 6.8 No implementation defines the architecture

Fabric is initially expected to run on existing operating systems, particularly Linux.

A Linux implementation may use processes, namespaces, sockets, file descriptors, cgroups, or
device files. These are valid implementation tools. They are not automatically Atlas concepts.

Likewise, an embedded implementation may use interrupts, static dispatch tables, and direct
function calls. A future operating system may implement Atlas through kernel-native Capabilities
and message endpoints.

Atlas should describe all of these systems without pretending that one implementation is the
natural form of the others.

A concept belongs in Atlas because it is required by the Atlas model, not because Fabric happens
to need it.

Linen provides a second guard. It is intended as an independent full-stack implementation rather
than a thin alternative front end over Fabric. Where Fabric and Linen components implement the
same Atlas contracts, they should be interchangeable despite different internal structures. A
concept that exists only because both implementations copied the same accidental design has not
passed this test.

The openness presumption (§29.1) is the enforcement mechanism for this principle: because
unspecified behaviour is never guaranteed, no implementation's accidents — including Fabric's
or Linen's —
can quietly become the de facto standard.

### 6.9 Authority is permission, not precedence

Two fully authorised Actors may act in conflict: a cooling controller sets fan speed while an
emergency handler stops the fan.

> *Possession of authority confers permission, not precedence. Ordering, priority, and conflict
> resolution between authorised Operations are defined by Domain Vocabularies or extensions,
> never inferred from the authority model.*

Two patterns cover most cases without Base machinery. A vocabulary may declare conflict semantics
for its Operations (a Fan vocabulary may state that `Fan.Stop` latches and supersedes
`Fan.SetSpeed` until released). A contested resource may be fronted by a mediating Actor that
holds the direct Capability and exposes arbitrated Operations.

Base only disclaims what it does not provide, so that no one assumes it does.

### 6.10 Names are structurally legible and semantically opaque

Atlas names have parseable namespace and concept segments. Implementations may preserve, group,
route, display, and discover names using that structure.

No authority, compatibility, or implication relationship follows from lexical ancestry unless an
Atlas specification explicitly defines one. `Motor.Control` does not imply `Motor.Stop` because
of its spelling. A Domain Vocabulary MAY define a Capability template that permits both
Operations, but the relationship comes from the vocabulary, never from the dots.

Likewise, an authored namespace such as `Logitech.MX:` is structurally subordinate to the
`Logitech` namespace, but the syntax alone does not prove that Logitech authorised it. §22
defines canonical names, authored namespaces, verification boundaries, and declaration prefix
blocks.

### 6.11 Interaction is composition, not ontology

Execution, Event, Outcome, and extension-defined occurrences may compose the Interaction fragment
defined in §13. Sharing that structure does not make their semantics interchangeable. Interaction
is not itself an occurrence and does not establish an inheritance hierarchy.

> *Composition with Interaction guarantees only its named structural and attribution semantics.
> It does not imply the semantics of Execution, Event, Outcome, or any extension occurrence.*

Every Base occurrence and every extension-defined occurrence MUST state:

- its definition,
- the semantics it adds,
- its required invariants,
- the behaviours it explicitly does not provide,
- and a minimal embedded realisation where the Embedded Test applies.

This is the **Interaction fence**. Common facilities such as identity, attribution, correlation,
causation, provenance, and optional temporal placement may be composed through Interaction.
Semantic name, target, value, authority, effect, truth, delivery, ordering, replay, persistence,
cancellation, and lifecycle remain with the composing occurrence or its specification.

## 7. Atlas Base

Atlas Base defines the smallest common computational model recognised as Atlas.

Version 0.3 identifies eight Base terms:

```
Actor
Capability
Shape
Delegation
Operation
Execution
Event
Outcome
```

Atlas Base also recognises two scoping concepts that describe the responsibilities of
implementations rather than adding
participant-facing objects:

```
Authority Domain
Genesis
```

Operation is a semantic contract. Execution and Event are distinct occurrence forms, and Outcome
is a specialised Event. Execution and Event compose the standard Interaction fragment, which is
defined using Shape rather than admitted as a ninth Base term. Flow is a first-party
Architectural Extension expressed through Base terms (§19.1).

These terms remain provisional. Atlas 0.x is intended to discover whether they are genuinely
fundamental and whether their current boundaries are correct.

Base membership is determined by architectural necessity, not frequency of use. A concept does
not enter Base merely because most workstations, applications, or large systems are expected to
implement it. `Resource`, `State`, `Transaction`, `Persistence`, `Presentation`, and
`Workspace` remain extension directions (§19), however common some of them may become. Every Base
term must remain necessary to the smallest meaningful Atlas system and must survive the Embedded
Test (§3).

Shape enters Base in 0.3 not because it is expected to be common, but because independent
implementations cannot preserve Operation, Event, Outcome, and Constraint contracts while
disagreeing about the structure of their values (§16).

Conformance to Atlas always implies conformance to Atlas Base.
For this reason, implementations do not list Base as a separately supported feature.

A system claims:

```
Atlas 0.3
```

and then lists any additional extensions, profiles, and domain vocabularies it supports.

## 8. Authority Domains

An **authority domain** is the scope within which one implementation is responsible for
preserving Atlas authority semantics.

A firmware image is an authority domain.
An operating system instance is an authority domain.
A distributed deployment sharing one trust root may be one authority domain.

Atlas Base defines authority semantics *within* a domain. Cooperation *between* domains — mutual
identification, attestation, and cryptographic representation of authority — is real, important,
and deferred to the `Identity` and `Distributed` extensions. This is deliberate: requiring global
identity in Base would fail the Embedded Test and would repeat the mistake of global naming
schemes, which collapse under their own registration bureaucracy. Identity in Atlas is
domain-relative, as names are in SPKI/SDSI and as capability references are in every capability
operating system.

Each authority domain's implementation is its own trusted computing base (see §28).

## 9. Actor

An **Actor** is a participant capable of presenting authority within an Atlas system.

An Actor may:

- hold Capabilities,
- present Capabilities,
- initiate Executions of Operations,
- emit Events,
- receive delegated authority,
- and delegate authority where permitted.

Actor is the common participant abstraction of Atlas.
Its definition deliberately does not depend on implementation nature or scale.

A process may be an Actor.
A peripheral may expose an Actor.
A human may participate as an Actor.
An autonomous system may be an Actor.
A firmware subsystem may be an Actor.
A complete software platform or composite system may expose an Actor at an architectural
boundary.

These statements do not imply that Atlas considers a human, process, mouse, and corporate
platform equivalent. They imply that each may participate in the same authority model.

### 9.1 Actor references

Every authority relationship presupposes that the implementation can answer two questions:
*which Actor initiated this Execution* (attribution), and *which Actor was this authority
granted to* (designation). If either answer can be forged or confused, the authority model
collapses regardless of how well Capabilities are represented.

An **Actor reference** is a designator for an Actor, provided by the implementation, with the
following properties within its authority domain:

1. **Unforgeable** — an Actor cannot fabricate a reference it was not given by the
   implementation.
2. **Unambiguous** — a reference designates at most one Actor at any given time.
3. **Stable** — a reference remains valid for at least the lifetime of the authority
   relationships that mention it.
4. **Comparable** — the implementation can decide whether two references designate the same
   Actor.

Atlas Base does not define the representation of Actor references, global Actor identity, Actor
discovery, or cross-domain Actor authentication.

All four properties are satisfiable by a compile-time index on a microcontroller where the static
program structure prevents Actors from fabricating or dispatching arbitrary indices. Integer
equality then supplies comparison at negligible runtime cost. An unrestricted integer in shared
writable memory is not unforgeable merely because it was assigned at compile time. A dynamic
implementation may use kernel-protected opaque references. A cross-domain implementation may use
cryptographic keys — at which point the four properties are exactly what the cryptographic
representation must preserve, which is what makes movement between representations coherent.

The comparability property forbids uncorrelatable aliasing within a domain: if one Actor could
hold two references the implementation cannot connect, any future revocation or audit keyed on
"the Actor" would silently fail for the alias.

Atlas Base does not define Actor persistence, discovery of Actors, or runtime placement.

Atlas Base requires only that Actors participating in an authority relationship can be
distinguished to the extent necessary to preserve that relationship — and the four properties
above are that extent, made precise.

## 10. Capability

A **Capability** is a target-recognised, explicit and presentable grant of authority.

A Capability authorises its holder to execute one or more defined Operations within an effective
scope. It references the canonical Operations recognised by the target. Each Operation declares
its input and output Shapes and required Declared Fragments, so that structural recognition is
part of the Capability contract transitively rather than repeated as a second list.

Recognition does not imply that the holder understands the Operation, that an Execution is
currently available, or that an attempted Execution will succeed. It means that the target can
identify and evaluate the referenced Operation and its structural contract. A purported grant
whose Operation or required Shapes are not recognised by the target cannot authorise an Execution
there.
Additional fragments on an open Shape never broaden the Capability: a target may act on stronger
fragment semantics only where the recognised Operation contract and presented authority cover
them.

A Capability is not merely a permission name.
It represents authority recognised by the Atlas implementation.

Names such as the following identify Operations, not Capability objects:

```
Temperature.Read
Fan.SetSpeed
Fan.Stop
Database.Migrate
Audit.Start
```

A Capability is a particular grant, for example:

```
Capability CoolingGrant:
    holder: CoolingController
    permitted-operations:
        Temperature.Read
        Fan.SetSpeed
        Fan.Stop
    target: CoolingSystem
```

The notation `CoolingController holds Fan.Stop` is permitted as explanatory shorthand for
"CoolingController holds a Capability authorising `Fan.Stop`". Normative examples use explicit
Capability identities whenever derivation or comparison matters.

### 10.1 Constraints

A Capability may carry Constraints. These may constrain:

- the permitted Operation,
- the target,
- a value or quantity,
- system state,
- rate or capacity (see §26),
- temporal validity (§10.3),
- origin assertion (§15),
- further Delegation,
- or another property defined by an Atlas extension or Domain Vocabulary.

For example:

```
Capability MigrationGrant:
    permitted-operation: Database.Migrate
    target: CustomerDatabase
    maximum-version: 42
```

or:

```
Capability AuditGrant:
    permitted-operation: Audit.Start
    organisation: Erste
    scope: FinancialControls
```

Atlas Base does not prescribe a Constraint syntax. It prescribes the Constraint *algebra*:

> Constraints only ever narrow. Effective authority is evaluated when an Execution is presented,
> at the target's authorisation boundary, as the conjunction of all
> Constraints along the derivation chain (§11). A Constraint that the evaluating implementation
> cannot interpret MUST cause denial.

The fail-closed rule is not optional politeness. Without it, every future Constraint type is a
privilege-escalation window against older implementations; with it, older implementations
degrade to *stricter*, never to *wrong* — which also makes Constraint evolution version-safe
(§23).

Constraint types and their meanings are defined by Domain Vocabularies and extensions. Base
defines only the composition rule and the fail-closed rule. A Constraint's authorisation meaning
MUST be a narrowing predicate over an Execution. Bookkeeping needed to evaluate rate, capacity, or
liveness MAY change state, but evaluating a Constraint must not itself grant authority or cause
the requested domain effect.

Every Constraint type that carries a value MUST declare the Shape of that value (§16). A target
that cannot establish Shape compatibility for a presented Constraint value cannot evaluate the
Constraint and therefore denies under the fail-closed rule. Shared Constraint names without
shared value structure do not constitute shared authority semantics.

### 10.2 Designation of targets

Where a Capability or Delegation designates a target by name (`target: CustomerDatabase`), the
name SHOULD be resolved at grant or delegation time, within the namespace of the *granting*
Actor's domain, and the resulting binding recorded in the Capability. Authorisation-time (late)
resolution is permitted only where the vocabulary explicitly declares it, and the resolving
namespace must be identified.

*Resolve early, bind, record.* A name that means one thing when authority is granted and another
when an Execution is authorised is a time-of-check/time-of-use vulnerability wearing a convenience
costume.

### 10.3 Temporal validity and mortality

Two temporal Constraint families are recognised:

- **Wall-clock validity** (`not-after`, `not-before`) — for domains with real time. Conjunction
  along a chain takes the intersection of windows. A domain without a clock denies wall-clock-
  bounded Capabilities under the fail-closed rule, and is simply granted non-temporal ones.
- **Liveness-scoped validity** — authority valid only while its grantor actively maintains it
  (lease renewal), or scoped to an enclosing session, attachment, or Flow where the `Flow`
  extension is present. Doing nothing kills the authority. This costs a tick counter and
  therefore passes the Embedded Test.

Recommended design stance for dynamic domains:

> *Authority defaults to mortal. Immortality is the explicit exception.*

Mortality is the cheap majority of revocation, available now; full revocation semantics remain
an open question (§33).

Withdrawal of authority and cancellation of accepted work are distinct. Withdrawal denies new
Executions and renewals; it does not retroactively undo committed effects. Every extension or
Domain Vocabulary that defines a continuing relationship or long-running activity MUST state:

- whether withdrawal terminates existing work,
- the maximum revocation horizon,
- any safe checkpoint or commit-point behaviour,
- the terminal Outcome produced,
- and whether compensation is available as a separate authorised Operation.

The common declaration is required even while the exact `Lifecycle` and `Flow` revocation
protocols remain open.

### 10.4 Representation

Atlas Base does not prescribe how Capabilities are represented.

An implementation might use:

- a static table entry,
- an unforgeable reference,
- a kernel object,
- a cryptographic credential,
- or another mechanism.

The representation may differ.
The effective authority must not.

## 11. Delegation

**Delegation** is the derivation of authority from one Actor to another.

An Actor may delegate only authority it is permitted to delegate.
The resulting authority must remain within the effective authority available to the delegating
Actor.

Atlas guarantees this structurally rather than by comparison:

> A Delegation derives a Capability that is the delegator's Capability plus zero or more added
> Constraints. A Delegation MUST NOT express authority any other way.

The derived Capability designates a new holder and records its parent. Changing the designated
holder and adding the derivation link are the mechanics of Delegation; they do not rewrite the
Operation set, target, or other effective authority inherited from the parent.

Narrowing therefore holds *by construction*. No implementation ever computes whether one
authority expression is a subset of another — an undecidable comparison in general, and the rock
on which independent implementations would otherwise disagree. The delegator narrows
syntactically (adds Constraints, no evaluation needed, works offline and in static tables); the
target's implementation evaluates semantically at authorisation time (§10.1).

For example:

```
CoolingController holds Capability CoolingGrant:
    permitted-operations:
        Fan.SetSpeed
        Fan.Stop

CoolingController derives Capability EmergencyGrant:
    from: CoolingGrant
    for: EmergencyHandler
    adding:
        permitted-operation: Fan.Stop
```

The EmergencyHandler may stop the fan.
It does not gain unrestricted fan control.

At a larger scale:

```
OperationsSystem holds Capability AuditGrant:
    permitted-operations:
        Audit.Start
        Audit.Suspend
        Audit.DeleteRecord

OperationsSystem derives Capability CoordinatorGrant:
    from: AuditGrant
    for: AuditCoordinator
    adding:
        permitted-operation: Audit.Start
        organisation: Erste
        scope: FinancialControls
```

The AuditCoordinator may initiate the authorised audit.
It does not gain unrestricted Audit administration.

Delegation may be dynamic. It may also be completely static.
An embedded Atlas implementation may define every Delegation relationship at build time — a
static delegation table represents pre-evaluated Constraints.
A distributed system may create and withdraw Delegations at runtime.
Both models are compatible with the architectural concept.

Delegations form a derivation graph: every derived Capability records what it was derived from,
where the implementation preserves provenance. This graph is the structure against which any
future revocation semantics will be defined (revoking a Delegation invalidates its derivation
subtree), which is why revocation can remain open (§33) without contaminating the Base model.

Patterns requiring *amplification* — two authorities combining into more than their sum — are
not expressible as Delegation, deliberately. They are modelled as an Actor exposing a new
Capability whose implementation internally uses its own authority (§25). Amplification becomes a
service boundary, not a delegation rule; the delegation calculus stays monotonic and auditable.

## 12. Genesis: The Origin of Authority

Delegation describes how authority flows. It does not describe how authority comes to exist.
Every derivation chain needs a root.

> Every Capability is either **primordial** — created by the authority domain itself — or
> **derived** through Delegation. Every derivation chain terminates in a primordial Capability.
> After domain initialisation, no authority comes into existence except by derivation, unless a
> **Genesis occurrence** happens. Genesis occurrences are implementation- or extension-defined,
> but MUST be enumerable and attributable to the domain's own policy.

This is conservation of authority: authority is never created mid-flight, only at named,
attributable moments.

"Genesis occurrence" names the policy occurrence at which authority is introduced. Where that
occurrence is exposed to Atlas participants, it is represented as an Event under §14. Event
Distribution is not required; an embedded domain may record Genesis only in its static primordial
table.

The embedded case satisfies it trivially — the compiled-in authority table *is* the primordial
set, and there are no Genesis occurrences. A dynamic operating system satisfies it by constructing
initial authority at boot and deriving everything else. Device attachment is the canonical
Genesis occurrence: the host's attachment policy mints the new device's Capabilities at the moment of
attachment (see §24). Domain federation, when defined by the Distributed extension, will be
another.

Genesis occurrences are also where origin classes are anchored (§15): a Genesis occurrence is precisely a
moment when the domain *observes* a fact worth vouching for.

## 13. Operation, Execution, and Interaction

An **Operation** is a named semantic contract for a requested effect. An **Execution** is one
concrete attempt to execute an Operation at a target. Operation defines what may be requested;
Execution records that it was attempted in a particular authority and attribution context.

Operation is not an occurrence, message, function, or implementation object. Execution is an
occurrence, but its existence does not assert delivery, authorisation, start, or success. A denied
request is still an Execution whose requested effect did not begin.

### 13.1 Operation

Every Operation declares:

- a canonical semantic name;
- the requested effect and its normative observable semantics;
- one complete input Shape;
- one separate, complete output Shape;
- and the target or provider semantics against which authority is evaluated.

The input Shape describes the complete set of values supplied to request the effect. Several
apparent function parameters are fields or other constituents of that one Shape, not several
Operation-level Shapes. The output Shape independently describes the complete successful return
value. `Unit 1` is used where either side has no value. Input and output Shape names, versions,
Declared Fragments, and evolution are independent; compatibility of one never implies
compatibility of the other.

Declaring an output Shape does not promise that an Execution completes, returns synchronously, or
delivers an Outcome. It defines only the abstract structure of a successful return value wherever
the completion mechanism exposes one. Failure and rejection details are not results and use
separately declared Shapes.

An Operation is defined by semantic meaning, not implementation size. `Fan.Stop` may be realised
almost directly by hardware. `Database.Migrate` may take several minutes and coordinate many
services. `Audit.Start` may create an activity involving systems and humans over a much longer
period. One Execution may internally initiate further Executions without changing the identity of
the outer Operation.

### 13.2 Interaction composition

**Interaction** is the standard reusable Declared Fragment through which occurrence attribution
and common contextual relationships are composed. It is named and versioned as `Interaction 1`.
It is not an occurrence, superclass, Interaction form, or ninth Base term.

Execution and Event include Interaction explicitly; Outcome composes it as a specialised Event
(§14.2). An extension-defined occurrence MAY include it. Inclusion is composition, not
inheritance: the including occurrence retains its own identity and semantics.

Interaction defines:

- the Actor responsible for initiating or emitting the occurrence;
- an optional occurrence identity or reference;
- optional correlation with a larger context;
- optional causation by another occurrence;
- optional origin information (§15); and
- an optional `emitted-at` Temporal Mark.

Semantic name, target, presented Capability, input value, assertion value, result, and form-specific
status do not belong to Interaction. They remain in Execution, Event, Outcome, or the extension
occurrence that owns their meaning.

Base does not require a universal object, header, payload encoding, runtime Shape descriptor,
dynamic metadata, or allocation. A direct call, static callback, shared-memory descriptor,
network frame, or another mechanism may establish the same Interaction composition by
construction. An identity is required only where another recognised relationship — such as
Outcome's `terminal-for` — refers to the occurrence, and it need be stable only for the lifetime
of that relationship.

Interaction is the first standard reusable Declared Fragment. A Shape explicitly including a
reusable fragment adopts its exact fields and invariants. This differs from an independently
authored fragment attached to an open Shape: explicit host inclusion is required, and structural
reuse creates no implicit compatibility between the host Shapes (§16.3).

### 13.3 Temporal Marks

An occurrence composing Interaction MAY carry `emitted-at`, meaning the time at which its
responsible Actor made it available at an Atlas boundary. For direct embedded dispatch, the
boundary may be a function call. For a distributed implementation, it may be entry into Atlas
communication machinery.

When present, `emitted-at` contains:

```
emitted-at:
    milliseconds: <signed integer>
    time-domain: <time-domain reference>
    uncertainty-milliseconds: <non-negative integer, optional>
```

All Base temporal values are signed integer milliseconds from an epoch defined by their time
domain. Floating-point values and formatted date strings are not Base temporal representations.
Zero is a valid value, never an unknown sentinel. A time domain defines the epoch, clock
progression, and comparison rules. Temporal Marks from different domains are comparable only
where a specification defines compatibility between those domains.

`emitted-at` is optional. Absence means that no emission time was supplied. Absence of
`uncertainty-milliseconds` means unknown or unreported uncertainty, not zero uncertainty. Richer
clock synchronisation, trust, drift, logical clocks, and sub-millisecond representation belong to
a future `Time` extension.

An emitted time is an attributable clock claim by the responsible Actor. Capability temporal
validity (§10.3) MUST be evaluated using the target authority domain's trusted clock, never a
sender-supplied Temporal Mark. Forwarding or redelivery MUST NOT overwrite the original
`emitted-at`. A newly derived occurrence receives its own identity and emission time and may
identify the original through causation.

### 13.4 The Interaction fence

Interaction supplies structure and nothing more. In particular, composing Interaction does not
provide:

- authority to cause an effect;
- truth;
- delivery or a response;
- ordering;
- persistence;
- replay;
- cancellation;
- lifecycle; or
- flow control.

An implementation may preserve or route a structurally valid occurrence composing Interaction
where policy permits, even when it does not understand the occurrence's semantic form. It MUST
NOT infer Execution, Event, Outcome, or extension semantics merely from the common fragment. Each
occurrence specification defines what it adds and explicitly fences what it does not provide
(§6.11).

### 13.5 Execution

An **Execution** is an occurrence through which an Actor attempts one Operation by presenting a
Capability at the target's authority boundary.

Conceptually:

```
initiating Actor
    initiates Execution of Operation
    presenting Capability
to target or provider Actor
```

Execution composes Interaction and adds:

- the referenced Operation;
- the target or provider boundary;
- the presented Capability;
- an input value conforming to the Operation's input Shape; and
- authorisation evaluation at the target boundary.

Execution requires:

- that the initiating Actor is attributable;
- that the Capability designates that Actor;
- that the target recognises the Operation, its input and output Shapes, and their required
  Declared Fragments;
- that the complete input value conforms to the Operation's input Shape;
- that the target evaluates every Constraint it recognises and denies every Constraint it does
  not recognise; and
- that the requested effect begins only after successful authorisation.

An Execution may be rejected before the effect begins, accepted and fail, or complete
successfully. Execution alone does not provide delivery, idempotency, ordering, cancellation,
rollback, completion, or a particular invocation mechanism. Replaying an Execution may repeat an
effect; an Operation contract itself is reusable rather than replayable.

A minimal embedded Execution is a direct call or static dispatch entry whose initiating Actor,
Capability, Operation, input Shape, and target are fixed or checked by program structure. No
message allocation, runtime identity, scheduler, or dynamic lookup is required. An Execution may
also be realised through inter-process communication, a network message, workflow, distributed
orchestration, hardware instruction sequence, or another mechanism.

Atlas standardises Operation, Execution, Interaction composition, Shape, and authority semantics,
not a call mechanism.

### 13.6 The invocation principle

When an Actor initiates an Execution in response to another Actor's request, the effective
authority evaluated MUST be attributable to that request — either Capabilities presented with the
request, or authority the responding Actor explicitly and deliberately presents as its own,
recorded as such. Implementations MUST NOT default to evaluating the responding Actor's ambient
authority on behalf of requesters.

Where an implementation preserves Delegation provenance, an Execution initiated on behalf of a
requester SHOULD carry the requester's place in the chain, so that audit and policy can
distinguish the provider or deputy from the initiator.

A responding Actor that holds real authority and serves less-privileged requesters is a *deputy*;
a deputy that presents its own Capability based on requester-supplied designations is a confused
deputy. The full request-carrying mechanics belong to future communication extensions; the
principle belongs in Base so implementations do not default into the vulnerability while waiting
for those mechanisms. A worked example appears in §29.4.

## 14. Event and Outcome

An **Event** is an immutable, attributable assertion by an Actor that something happened.

Assertion is deliberate language. Atlas records who emitted the Event, its semantic name, and
available provenance. Atlas does not thereby guarantee that the assertion is true.

Event composes Interaction and adds:

- assertion semantics,
- a canonical semantic name and subject,
- a declared Shape for any assertion value,
- and immutability after emission.

Event requires:

- preservation of emitter attribution and provenance,
- that replay repeats the assertion rather than the asserted occurrence,
- and that forwarding does not silently replace the original emitter with an intermediary.

Event alone does not provide truth, delivery, observation, fan-out, ordering, persistence, replay
availability, or authority for an observer to react. In particular, receiving an Event does not
grant an Actor authority to perform an Operation. An Actor that reacts using its own authority
must do so deliberately and remain attributable as the initiator of the resulting Execution.

Constructing an Event within an Actor's own boundary is not authority over another Actor. Making
that Event observable across a protected boundary may require admission, publication, or
observation Capabilities defined by an extension or Domain Vocabulary. Any asserted origin class
requires the authority described in §15.

An Event MAY carry `occurred-at` in addition to the common `emitted-at`. `occurred-at` uses the
Temporal Mark representation of §13.3 and describes the emitter's claim about when the asserted
occurrence happened. The two values may differ.

Base defines Event ontology, not event-distribution infrastructure. A direct return, callback,
static dispatch, state transition, or another observable mechanism may realise an Event. General
publication, observation, fan-out, subscriptions, filtering, persistence, and replay belong to
the `Event Distribution` extension (§19.2).

### 14.1 Minimal Event realisation

A microcontroller may represent an Event through a static callback selected by a compile-time
Event kind. The Event's emitter is established by the static call path; its semantic name is a
table index; no queue, allocation, clock, Event bus, or observer discovery is required.

This satisfies the Embedded Test while preserving the difference between an Execution attempting
an Operation and an Event asserting an occurrence.

### 14.2 Outcome

An **Outcome** is an Event that terminates one identifiable Execution, activity, or extension-
defined continuing relationship.

Outcome adds:

- a mandatory `terminal-for` relationship,
- a terminal status,
- and an optional result value with a declared Shape.

For an Outcome terminating an Execution, a successful result value conforms to the executed
Operation's independently declared output Shape (§13.1). Rejection or failure diagnostics are
`details`, not a result, and use their own declared Shapes. This relationship does not make the
output Shape part of the input Shape and does not require synchronous return.

The terminated subject must be identifiable for at least as long as the Outcome relationship is
relevant. An Outcome is final relative to that subject. An Execution may complete successfully by
creating a longer-lived activity; the activity later terminates through its own Outcome.

Outcome does not by itself provide rollback, compensation, persistence, lifecycle history, or
delivery to an observer. A minimal embedded Outcome is a direct return or static status value that
distinguishes authorised success, authority rejection, and authorised failure.

At minimum, Atlas preserves the distinction between:

- successful authorised completion,
- rejection because sufficient authority was not present,
- and failure during an otherwise authorised Execution,

where an observable result exists at the relevant boundary. Extensions and Domain Vocabularies
may define additional terminal statuses such as cancellation, authority withdrawal, or
compensation.

For example:

```
Outcome:
    terminal-for: AuditStart-7
    status: succeeded
    result:
        created-activity: Audit-318

Event:
    kind: Audit.Progress
    activity: Audit-318
    completed: 60%

Outcome:
    terminal-for: Audit-318
    status: completed
```

Atlas Base does not define the lifecycle of `Audit-318` or a universal error taxonomy. A future
`Lifecycle` extension and specialised Domain Vocabularies define richer activity states and
Outcomes.

## 15. Origin: Provenance of Effect

Atlas provenance has two components. The Delegation chain answers *by what authority* an Execution
or assertion occurred. The **origin class** answers *what kind of cause* produced the occurrence
or effect — a physical transducer, a human act, an autonomous computation, derived or replayed
data.

The two are orthogonal: a remote-desktop tool may have impeccable authority and still must not
look like a mouse. A consent record is only as strong as the claim that a *human* produced the
approval. A replayed sensor reading with a valid chain is still not a live measurement.

The polarity of this mechanism is its most important property. A scheme in which suspicious
sources must label themselves fails immediately — attackers do not self-label. Atlas inverts the
burden:

> **Origin assertion is authority.** An Execution or Event emission MAY assert an origin
> class only if the authority under which it occurs grants that assertion. Absent a granted
> assertion, the occurrence carries only its Interaction attribution and available provenance; its
> origin class is **unverified** — the default.

Software that obtains injection authority without an origin grant produces effects marked
unverified-origin *automatically*: looking like a device is a privilege it was never granted.
The burden of proof sits on the trusted.

> **Origin grants do not survive Delegation.** A delegated Capability asserts at most
> `Origin.Derived`. Origin classes are re-established only by genesis-grade grants: the
> authority domain, or a guardian acting as it, vouching directly — as it does at device
> attachment or through a trusted human-input path.

Without this rule, a device could delegate its device-ness to companion software and masquerade
would return one hop downstream, laundered through a valid chain. Origin is genesis attribution
(§12) made portable: droppable along a chain, never gainable. The fail-closed rule (§10.1)
covers implementations that do not recognise an asserted class.

The class taxonomy — `Origin.Device`, `Origin.Human`, `Origin.Autonomous`, `Origin.Derived` —
and guardian vouching rules belong to an `Origin` vocabulary, not Base. Domain Vocabularies
consume origin classes; they do not define local markings. At the cross-domain tier, origin
claims become signed assertions, compatible in spirit with content-credential systems (C2PA)
that build this mechanism as a bolt-on because no platform layer offers it.

Precedents confirming the demand: Windows has flagged injected input for two decades
(advisory and spoofable, because it is a flag rather than an authority); financial regulation
mandates algorithmic-order flagging; media provenance is being retrofitted cryptographically.
Three independent partial rebuilds of one missing primitive. Atlas provides it once, generally,
with enforcement instead of etiquette.

In the presence of autonomous Actors, "was a human actually in the loop" becomes a mechanically
checkable property of the provenance record.

## 16. Shape: Structure Across Implementations

A **Shape** is the complete abstract structural contract for one value used at an Atlas boundary.
It has one canonical named and versioned definition and may compose named and versioned Declared
Fragments. Shape describes admissible structure and types; it is never the value, object, memory,
or encoded bytes that conform to that contract.

Operation names alone are insufficient for interoperability. Two implementations may both
recognise `Fan.SetSpeed`, yet remain incompatible if one expects a signed integer and the other
expects an implementation-specific object with different fields. Likewise, a Capability
Constraint cannot be evaluated consistently if the participants disagree about the structure of
the value it constrains.

Shape is therefore part of Atlas Base in Architecture 0.3.

Every Operation declares one input Shape and one independent output Shape. An Execution's input
value conforms to the Operation's input Shape; a successful Outcome's result value conforms to its
output Shape. Event assertion values, failure details, and Constraint values likewise conform to
their separately declared Shapes. Absence of a value is represented by the unit Shape or
established as unit by construction. This does not require a serialised schema reference. A direct
call may establish Shapes through a statically known contract; an embedded implementation may use
a compile-time table; a distributed representation may carry explicit Shape references.

Shape provides structure, not authority. Possessing, constructing, or understanding a value
conforming to a Shape grants no Capability and authorises no Execution. Shape also does not by
itself establish the domain meaning of a field. That meaning belongs to the Domain Vocabulary or authored
specification that defines the Shape and the Operation, Event, Constraint, or other contract using it.

### 16.1 Shape identity and structure

A Shape reference contains:

```
shape-name: <canonical name>
shape-version: <positive integer>
```

An open record Shape may additionally be composed with Declared Fragments (§16.3). The complete
composed Shape is identified by its canonical Shape reference together with the unordered set of
Fragment references that are included, present, or required. Atlas does not require a new combined
name for every such composition.

The version belongs to the Shape and is independent of the Atlas Architecture version, Profile
versions, and transport encoding. Capability instances are grants identified through their
holder, scope, and derivation; Architecture 0.3 does not assign semantic versions to individual
Capability objects. Shapes are explicitly versioned because their structural contracts evolve.

At minimum, a Base Shape system must be capable of expressing:

- **unit** — no value;
- **scalar** — a value with a canonical scalar Shape, not merely an implementation-local type;
- **record** — named fields, each referring to another Shape;
- **sequence** — zero or more values of one declared Shape;
- **choice** — one value selected from named alternatives; and
- **opaque** — uninterpreted data whose declared Shape and integrity may still be preserved.

The exact standard scalar catalogue remains provisional. A Shape specification MUST NOT assume
that a CLR `int`, a C `int`, a JSON number, and a machine register are equivalent merely because
an implementation uses the same informal word for them.

A record field declaration contains:

```
field-name: <canonical field name>
field-shape: <Shape reference>
presence: required | optional
```

An unqualified canonical field name is defined by the authority responsible for its enclosing
Shape and is scoped to that Shape lineage. A field contributed through a named fragment is scoped
additionally by that fragment's identity and author. Field identity is therefore the enclosing
Shape lineage, owning fragment, and canonical field name; two unrelated Shapes or fragments do
not make their respective `speed` fields interchangeable merely by spelling them alike.

Record field order is not semantically significant. Repetition is expressed through a sequence
Shape rather than by assigning special meaning to repeated field names. Absence of an optional
field means absence; an implementation MUST NOT invent an implicit default unless the defining
Shape or Domain Vocabulary specifies one normatively.

Two Shapes are not compatible merely because their fields happen to look alike. Canonical Shape
identity and declared fragment composition carry the contract. This prevents values such as
milliseconds, distances, identifiers, and monetary quantities from becoming interchangeable
merely because each happens to use an integer representation.

### 16.2 Shape versions

Versions of one canonical Shape form a monotonic, backward-compatible lineage.

A later version MAY add optional fields or other explicitly additive declarations. It MUST NOT:

- remove or rename an existing field;
- change an existing field's Shape;
- change an optional field to required;
- reinterpret an existing field;
- change a record between open and closed fragment policy; or
- otherwise make a value valid under an earlier version invalid or differently meaningful.

A breaking structural or semantic change requires a new canonical Shape name. A larger version
number is not permission to reinterpret an existing contract.

An implementation claiming support for version *N* of a Shape supports the complete contract of
every earlier version in that Shape's lineage. An implementation accepting an earlier version
MUST accept a valid value of a later same-name version through the earlier projection and ignore
optional additions it does not understand, because later versions are additive by definition.
It MUST NOT claim support for the later version merely because it can project away all of that
version's additions.

Shape versions describe the accepted value contract, not a particular serialisation. Multiple
wire encodings or in-memory representations may realise the same Shape version where they
preserve its observable semantics.

Declared Fragment versions form independent, monotonic lineages and obey the same additive rule.
Changing a Fragment never silently versions a Shape that includes or accepts it, and changing the
Shape never silently versions the Fragment. A reusable Fragment does not name one host; each host
explicitly includes the required Fragment version. An authored attached Fragment declares the
earliest compatible version of its host Shape and remains attachable to later same-name versions
because those versions are additive. A Fragment that requires newer canonical structure names
that newer host version. Breaking Fragment semantics require a new Fragment name.

### 16.3 Shape Fragments and composition

A **Shape fragment** is an arbitrary subset or projection of one complete Shape. Calling part of a
Shape a fragment does not make it a separate input, output, or value: the enclosing structure
remains one Shape. Fragment is a subordinate concept within Shape, not a ninth Atlas Base term.

Any participant may form an unnamed fragment locally by projecting an arbitrary subset of a
Shape. Such a fragment is useful for implementation, inspection, or explanation, but has no
portable identity, version, authorship, conformance meaning, or right to be required by another
participant. Atlas assigns architectural significance only to **Declared Fragments**: Fragments
that are named, versioned, authored, and normatively specified with exact membership and
invariants.

Every record Shape has a **canonical fragment** containing the fields specified by the authority
responsible for that Shape. The canonical fragment shares the Shape's canonical name and version;
it does not acquire a redundant Fragment identity. A record Shape also declares one of two
Fragment policies:

```
fragment-policy: open | closed
```

There are two ways a Declared Fragment participates in a Shape:

- **explicit inclusion** — the Shape's own specification includes a reusable Declared Fragment
  and adopts its exact fields and invariants; or
- **authored attachment** — another authority attaches its Declared Fragment to a Shape whose
  Fragment policy is open.

Explicit inclusion is permitted regardless of Fragment policy because the host Shape opts in.
One Declared Fragment may be explicitly included by several unrelated Shapes; this reuses exact
structure but creates no compatibility between those host Shapes. `Interaction 1` is the first
standard reusable Declared Fragment: Execution and Event include it to share attribution,
correlation, causation, origin, and temporal structure (§13.2).

Only the authority responsible for a Shape may add unqualified fields to later versions of that
Shape's canonical fragment. An open record additionally permits another authority to define a
Declared authored Fragment that contributes fields. Such a Fragment:

- has its own canonical authored name and positive integer version;
- names its host Shape lineage and earliest compatible version;
- owns an exact, non-overlapping set of fields under its author's namespace;
- may define requirements for its own fields but cannot remove, rename, replace, constrain, or
  reinterpret fields owned by the canonical or another fragment; and
- remains independently projectable from the complete composed Shape.

For example:

```
Shape Velocity 1:
    kind: record
    fragment-policy: open
    canonical-fragment:
        required speed: Integer.Signed64 1

Fragment Bob:DirectionalVelocity 1:
    for-shape: Velocity 1
    fields:
        required direction: Bob:Direction 1
```

The complete composed Shape to which a value may conform is:

```
Velocity 1 + Bob:DirectionalVelocity 1
```

This is one Shape composed from its canonical fragment and Bob's Declared Fragment, not two
input values. Alice may add an independent fragment without requiring Bob, Alice, or the
canonical author to mint a combined Shape name.

A participant that accepts the open Shape `Velocity 1` MUST accept the valid canonical projection
of that composed value, consume `speed`, and ignore fragments it does not understand. It MUST NOT
claim that it understood or honoured Bob's `direction`. If direction is required for the intended
effect, the relevant Operation input, Operation output, Event, Constraint, or Profile must require
`Velocity 1 + Bob:DirectionalVelocity 1` explicitly. A sender cannot attach Bob's fragment and
assume that a participant accepting only the canonical fragment will act upon it.

An authored Fragment may enrich the base contract; it cannot condition or reinterpret it. If
the meaning of an inherited field changes when an authored fragment is present, the composition
is not compatible and must use a new canonical Shape contract. Unknown-fragment tolerance would
be unsafe otherwise: a canonical consumer would appear to accept the value while performing a
different semantic operation from the one the sender intended.

A closed record rejects authored fragment attachment. Closed Shapes are appropriate where
accepting unrecognised structure would make validation, signing, hashing, or safety semantics
ambiguous.

### 16.4 Matching, projection, and unknown structure

An Operation, Event, Outcome details contract, or Constraint type declares the complete Shape it
accepts: a canonical Shape reference and any Declared Fragments it includes or requires. The
target evaluates Shape compatibility before interpreting or acting on the value.

At minimum:

- a missing required field causes rejection;
- an existing field with an incompatible Shape causes rejection;
- an unknown Shape with no recognised compatible lineage causes rejection where interpretation is
  required;
- every Declared Fragment required by the accepted Shape must be present at a compatible version
  and valid under that Fragment's contract;
- a value of an open Shape carrying additional well-formed fragments MUST be accepted as its
  recognised canonical projection;
- unrequired fragments MUST NOT affect validity of the canonical projection; an implementation
  ignores Fragments it does not understand and MAY process a Declared Fragment it explicitly
  supports without claiming that the canonical fragment required that behaviour;
- projection never implies support for the projected-away fragments;
- a closed Shape rejects authored fragments; and
- Shape compatibility never substitutes for Capability evaluation.

An Actor that merely routes an opaque value MAY preserve a Shape it does not understand where
policy permits. An intermediary claiming lossless forwarding MUST preserve unknown fragment
references, their fields, and canonical Shape identity. If it drops fragments or fields and
constructs a new value, that value is a projection or derivation rather than the original and must
be attributable as such where provenance applies.

Structural compatibility is deliberately directional. Every
`Velocity 1 + Bob:DirectionalVelocity 1` value has a valid `Velocity 1` projection; an arbitrary
`Velocity 1` value does not satisfy an expression requiring Bob's fragment.

Compatibility does not bypass admission or authority. A target may still reject an otherwise
compatible value under declared size, rate, capacity, or resource Constraints (§26), but an open
Shape implementation must not reject a valid canonical projection merely because it carries
well-formed authored Fragments the implementation does not understand. A Fragment cannot carry
an authority restriction that a canonical consumer is expected to ignore; authority remains in
Capabilities and unknown Capability Constraints continue to fail closed (§10.1).

### 16.5 Representation and the Embedded Test

Atlas Base defines Shape semantics, not a schema language, reflection API, object model, memory
layout, or universal wire encoding.

An implementation may realise Shapes through:

- compile-time function signatures and static tables;
- language-native generated types;
- compact numeric Shape and field identifiers;
- runtime descriptors;
- schema registries; or
- another representation preserving the same contracts.

A microcontroller may support a fixed set of Shapes compiled into its dispatch tables, reject
unsupported Shape versions, and use no dynamic allocation or reflection. Shape therefore passes
the Embedded Test: the structural agreement is mandatory, while dynamic schema machinery is not.

Independent implementations such as Fabric and Linen may use entirely different internal types.
Their components are interoperable only where translation between those types preserves the same
named input and output Shapes, versions, required Declared Fragments, fields, and projection
semantics. This is the cross-implementation pressure that makes Shape fundamental rather than
merely convenient.

### 16.6 Work in progress: Enrichment and value propagation

> **Work in progress.** This section records a design direction for investigation in Architecture
> 0.4. It is not yet a normative Atlas mechanism, does not add a ninth Base term, and does not yet
> define conformance requirements or final syntax.

The Shape model permits independently authored fragments to add structure without replacing,
removing, constraining, or reinterpreting existing structure. A related composition problem
remains: an already working module may later require an additional fragment, while the information
needed to supply that fragment already exists elsewhere in the containing system. Requiring the
author to rewrite every unaffected module between the source and the consumer would make local
extension unnecessarily expensive and would give intermediate modules semantic responsibility for
information they do not use.

The provisional term **Enrichment** is preferred over Modifier or Patch because the intended
invariant is addition only:

> *An Enrichment adds previously absent information. It never replaces, removes, constrains, or
> reinterprets information already present.*

An Enrichment would construct a declared Shape fragment from information already available to the
composition. It may copy, project, rename, reshape, combine, or deterministically derive values,
subject to the contracts of the source and resulting Shapes. It cannot conjure missing information.
In particular, Capability invocation should not be hidden inside Enrichment. Reading a sensor,
querying a provider, starting an Execution, or performing any other operation to obtain a value has
latency, failure, authority, and effect semantics and should remain visible as an ordinary Atlas
Operation. Its result may subsequently become an input to an Enrichment.

#### Targeted Enrichment

A **targeted Enrichment** would declare that a fragment is available at one named consumer,
Operation boundary, or explicitly bounded scope. It declares where the additional information may
be consumed; it need not describe a physical route through intermediate modules. For example, if
`pointer.move` newly requires `ThermalContext`, a containing composition might declare that
`ThermalContext.temperature` is derived from an already available
`DeviceTelemetry.temperature` at that Operation boundary. The declaration would be local to that
target and would not change the Shapes understood by unrelated modules.

The intended properties are:

- the target and added fragment are explicit;
- the source information and derivation are inspectable;
- only absent structure may be supplied;
- competing providers are an error unless the composition resolves them explicitly; and
- validation occurs before the changed composition is activated.

Targeting supplies a strong scope boundary and is therefore the safer initial form. It also makes
the provenance question answerable: a consumer can determine which declaration supplied the
fragment and from which available value it was derived.

#### Ambient Enrichment

An **ambient Enrichment** is a distinct possibility and must not arise merely because targeting was
omitted. It would declare a fragment available within a broader explicit scope to eligible
consumers that request it. Like targeted Enrichment, it need not prescribe a physical route through
intermediate modules. Ambient does not mean global, implicit, or universally observable. Such an
Enrichment would need to be marked `ambient` (or an equivalent final term), name its scope, preserve
provenance, and define conflict and shadowing rules.

Ambient Enrichment may be useful for information such as request correlation, locale, tracing
context, device context, or other values used by several independently authored modules. It may
also recreate ambient global state under another name, conceal dependencies, retain sensitive
information too broadly, and make behaviour depend on composition surroundings that are difficult
to inspect. Before ratification Atlas must decide at least:

- whether a consumer must explicitly declare every ambient fragment it consumes;
- whether nested scopes may shadow an ambient value, and how that remains visible;
- whether ambient information crosses Actor, authority-domain, Flow, or device boundaries;
- how least exposure and Capability Constraints apply to carried sensitive values;
- when an ambient value expires or leaves scope; and
- whether ambient Enrichment is truly distinct from a scoped provider or binding mechanism.

The current direction is that ambient availability, if retained at all, must remain explicit at the
composition level and explicit in the consuming module's requirements. Mere presence in an
environment must not create an undeclared semantic dependency.

#### Ambient availability and global stores

Ambient Enrichment does not prohibit a global store. A widely reachable store may be a legitimate
Atlas participant or may be described through future `Resource` and `State` extensions. It may
expose Operations for reading and changing stored values, with ordinary Capability, failure,
latency, consistency, and Outcome semantics.

Atlas should distinguish five relationships that conventional systems often collapse:

- **storage** — where information persists;
- **discoverability** — which participants can locate the provider;
- **authority** — which Actors may read or change the information;
- **availability** — the consumer, Operation boundary, or scope in which an obtained value can be
  supplied; and
- **consumption** — the module that explicitly declares and uses the value.

A store may be globally reachable while an Enrichment based on one of its values remains bounded
to a particular composition, Execution, Flow, Actor, or other explicit scope. Obtaining the value
from the store is an Operation and must not be concealed inside Enrichment. Once obtained, a value
or snapshot may become an input to targeted or ambient Enrichment. Implementations may cache,
preload, or otherwise materialise that value, but they must preserve the observable semantics
promised by its acquisition and scope.

The unsafe case is not global storage itself. It is undeclared global consumption: a module whose
meaning changes because it silently reads whatever happens to exist in a surrounding store or
context. Ambient Enrichment remains acceptable only if its scope is explicit and consumers declare
the fragments on which they depend.

#### Enrichment and Capabilities

The current direction restricts Enrichment to information described by Shapes and Declared
Fragments. Enrichment does not enrich authority. It cannot create, issue, derive, delegate,
transfer, bind, broaden, or combine Capabilities.

The distinction follows from their different rules. Information Enrichment is addition-only:
absent structure may be supplied without changing existing information. Authority originates
through Genesis or authorised issuance and thereafter narrows through Delegation. Making a value
available and making authority available are therefore not instances of the same operation.
Especially in an ambient scope, silently making a Capability available could empower Actors merely
because of their composition surroundings and would contradict explicit presentation of authority.

A Shape may contain information about a Capability or preserve an opaque representation associated
with one, but Shapes grant no authority (§16). Structural presence does not establish a holder,
valid Delegation, recognition by the target, or permission to present the Capability. Enrichment of
such information must not be interpreted as authority transfer.

There may nevertheless be a separate need for a composition to declare that an Actor may use an
existing or validly delegated Capability for a named Operation. A future **Capability binding**,
**authority binding**, or **authority provisioning** mechanism could describe that availability,
but it would belong to authority composition rather than Shape Enrichment. It would have to
preserve holder identity, target recognition, Delegation provenance, Constraints, mortality, and
explicit presentation at Execution. It could never mint or amplify authority merely because a
composition declares the binding.

#### Systems Are Not Necessarily Topological

Atlas does not assume that a system has one fixed call graph, pipeline, or global topology. A
system is better understood first as a collection of Actors or modules exposing Operations. A
module holding suitable authority may directly execute an Operation exposed by another, and the
choice may be dynamic, recursive, distributed, or dependent on runtime information.

Graphs remain useful and sometimes precise, but they describe particular views rather than the
universal form of an Atlas system. A graph may represent one concrete Execution trace, a deployment
composition, a Flow, a workflow, a dependency view, or a diagnostic representation. A declared
composition may establish stable edges for one bounded purpose, while the surrounding system still
has no single global topology. The graph of causal relationships produced by actual Executions is
not necessarily the same as a static module or deployment graph.

This observation changes the Enrichment model. Targeted Enrichment declares availability at a
named consumer, Operation boundary, or scope. Ambient Enrichment declares availability within a
broader explicit scope. Neither declaration necessarily identifies a chain of intermediate modules
or asserts that one physical route exists. Their semantics should state where information is
available and under what contract, authority, provenance, and lifetime rules; implementations may
realise that availability differently in different environments.

#### Value propagation

Where an explicit bounded route does exist, **value propagation** may preserve and transport a typed
Shape fragment across that route while intermediate modules neither consume nor semantically expose
it. Propagation is therefore one possible way to realise Enrichment availability in a topological
composition. It must not be treated as a universal property of Atlas systems or as proof that every
Enrichment has a route that Atlas can enumerate.

Conceptually, an interaction may have an operation-specific payload and separately carried
fragments. An intermediate module transforms its declared payload while the composition preserves
the carried fragment. Carrying a fragment does not add it to that module's input or output Shape,
does not make the module its consumer, and grants no authority. At the destination, a targeted
Enrichment may project or derive the carried information into the fragment required by the
consumer.

Any ratified propagation design should be explicit, typed, bounded, inspectable, and attributable.
It should name a source and destination or a precisely bounded scope; preserve provenance; stop when
its declared purpose ends; and reject ambiguous paths or conflicting values unless an explicit
merge or selection rule exists. Branches, joins, retries, concurrent Executions, Flows, and
cross-domain transport require particular care.

Parameter threading, carrier structures, contextual storage, attached fragments, and distributed
forwarding are all possible implementation strategies. The specification should not prescribe one
of them as the Atlas model, and implementations need not use the same strategy at every boundary.
An implementation may also materialise an Enrichment directly at its declared consumer or scope
without representing a path of intermediate carriers at all.

The remaining architectural question is:

> *Which Atlas relationship gives propagated information a bounded lifetime and an unambiguous
> route when modules may invoke one another directly?*

Possible answers include restricting propagation to explicitly declared compositions; carrying
values through an Execution's causal lineage; treating ambient Enrichment as a lexically scoped
composition binding; or defining a separate carrier construct in an Architectural Extension. It is
also possible that propagation should remain an optional composition technique rather than become
a general Atlas construct. No choice is made in Architecture 0.4. Fabric and Linen experiments
should test whether common availability semantics can be preserved across static embedded
dispatch, direct in-process calls, dynamically selected modules, and distributed interactions even
when their implementation strategies and available topological views differ.

## 17. A Minimal Atlas System

Consider a microcontroller controlling temperature and cooling.

It defines:

```
Actors:
    SensorReader
    CoolingController
    SafetySupervisor
```

It recognises:

```
Operations:
    Temperature.Read
    Fan.SetSpeed
    Fan.Stop

Events:
    Sensor.Temperature.Changed
```

It associates statically known Shapes with the values carried by Executions and Events:

```
Temperature.Read:
    input: Unit 1
    output: Sensor.Temperature 1

Fan.SetSpeed:
    input: Fan.Speed 1
    output: Unit 1

Fan.Stop:
    input: Unit 1
    output: Unit 1

Sensor.Temperature.Changed:
    value: Sensor.Temperature 1
```

Its authority relationships are:

```
Capability SensorGrant:
    holder: SensorReader
    permitted-operation: Temperature.Read

Capability CoolingGrant:
    holder: CoolingController
    permitted-operations:
        Temperature.Read
        Fan.SetSpeed
        Fan.Stop

Capability SafetyGrant:
    holder: SafetySupervisor
    permitted-operations:
        Temperature.Read
        Fan.SetSpeed
        Fan.Stop
```

The SafetySupervisor may derive an EmergencyGrant from SafetyGrant by adding the Constraint
`permitted-operation: Fan.Stop` and designating an EmergencyHandler as the new holder.

The complete implementation may use static structures.

The static authority table is the primordial Capability set (§12); there are no Genesis occurrences.
Actor references are compile-time indices whose permitted uses are fixed by program structure
(§9.1). Executions are direct calls and Events are callbacks with static semantic identifiers;
both compose Interaction by construction. Every
Delegation is a build-time entry — pre-evaluated Constraints (§11). Shapes are compile-time
signatures and table entries; no runtime descriptor or serialisation is present. No clock is
present, so `emitted-at` is omitted.

No process model is required.
No network is required.
No persistent identity is required.
No filesystem is required.
No dynamic memory allocation is required.

This is an Atlas system.

A much larger Atlas system may use radically different machinery and expose Operations,
Executions, and Events of radically different scale while preserving the same Base semantics.

## 18. Growing Beyond the Base

Atlas Base is intentionally insufficient for most complete computing environments.
This is expected.

Atlas grows through three additional forms of specification:

```
Architectural Extensions
Profiles
Domain Vocabularies
```

They solve different problems.

The Base provides a common Actor, authority, Operation, Execution, Event, and Shape model.
Extensions allow larger systems to introduce additional architectural concepts without making
those concepts mandatory for every Atlas implementation.
Profiles define useful interoperability expectations.
Domain Vocabularies allow Actors implemented by independent systems to agree on the semantic
meaning of common Operations, Events, Outcomes, and Shapes.

Together, these mechanisms are intended to allow Atlas to scale from embedded systems to highly
connected computational and organisational environments without treating either extreme as an
exception.

In practice, Profiles — not Base — are expected to be the unit of interoperability that software
targets, as instruction-set profiles are in comparable ecosystems. Base is the shared semantic
core; a Profile is what a developer writes against.

A future Profile may group a broad set of commonly expected extensions and vocabularies for
general-purpose environments. Such a convenience Profile would remain an ordinary Profile: it
would not enlarge Base, weaken the Embedded Test, or become a second privileged conformance tier.
Architecture 0.3 does not define, name, or reserve such a Profile. It should be introduced only
when repeated real Profile requirements reveal a stable common bundle.

### 18.1 Work in progress: Composition and Components

Atlas uses *component* when discussing composition and Fabric/Linen interchange, but the category
must not inherit the scale or packaging model of conventional application frameworks.

A **Component** is a scale-independent, bounded unit of composition that declares the Atlas
contracts it provides and requires. A Component boundary may enclose a function library, firmware
subsystem, process, device, service, cluster, data centre, organisational system, or another
recursively composed environment. Physical size, address-space placement, language, transport,
and deployment mechanism do not determine whether something is a Component.

Component is not a ninth Atlas Base term and is not an authority-bearing participant. An Actor is
a participant in authority relationships; a Component is a unit of composition. One Component may
realise several Actors, one Actor may be realised by several Components, and a Component may
contain other Components. Capabilities are held and presented by Actors. Loading, attaching, or
binding a Component grants no authority by itself.

A Component with an Atlas-visible boundary declares, as applicable:

- the Profiles, Extensions, and Domain Vocabularies it provides and requires;
- the Operations it provides or consumes, including their input and output Shapes;
- required Declared Fragments and open-Shape fragment policy;
- the Actor relationships and authority requirements visible at its boundary;
- selection characteristics and requirements relevant to placement or binding; and
- its binding model and, where applicable, Slots, Classes, and lifecycle limitations that narrow
  its composition or substitutability claims.

The contract describes observable Atlas semantics, not a private language type, object model,
package format, process protocol, or wire encoding. Two Components may realise the same contract
through entirely different internal machinery.

#### Selection characteristics and the local/remote projection

A Component contract says what semantic and authority relationships a Component supports.
**Selection characteristics** describe the operational circumstances under which a particular
Component or binding may be suitable. They allow selection among contract-compatible Components
at a resolution finer than `local` or `remote`.

Selection characteristics may include:

- topological placement and proximity relationships;
- authority-domain, isolation, and data-residency boundaries, including supporting attestations;
- measured or promised latency, jitter, bandwidth, and throughput;
- monetary cost, energy cost, or resource consumption;
- capacity, current load, admission requirements, and queue limits;
- availability, maintenance state, reliability, and correlated failure domain; and
- required hardware, accelerators, devices, or environmental conditions.

This list is not a ratified catalogue. Portable characteristics require canonical definitions and
Shapes, including units and interpretation, so that a latency, price, capacity, or distance is not
reduced to an ambiguous scalar (§16). Characteristics may be static constraints, time-bounded
claims, current observations, estimates, or policy-derived values; their form and freshness must
remain visible.

A Component descriptor may publish characteristics about its Component, but self-description is a
claim, not proof. An Actor realised by a Host may measure latency, a guardian Actor may report
current capacity, a deployment Actor may supply topological placement, and an Identity or
Distributed mechanism may attest a boundary. Each value must remain attributable to the Actor or
implementation mechanism responsible for asserting or observing it. Selection policy decides
which claims it accepts and may require independent verification.

Topology is relational and observer-dependent, not one universal graph (§16.6). The same Component
may be near one Actor and distant from another; a network service may remain inside one authority
domain, while an in-process Component may cross a separately protected domain. `Local` therefore
does not imply low latency, low cost, shared trust, or high availability, and `remote` does not
imply their opposites.

This does not make the `local`/`remote` distinction meaningless or undesirable. Implementations
MAY collapse the richer selection surface to those labels for user interfaces, summaries,
discovery views, diagnostics, and coarse policy defaults. A concise interface often should. Such a
label is an intentionally lossy, observer-relative projection: it MUST NOT silently imply latency,
trust, authority-domain, cost, capacity, availability, or failure guarantees that its projection
rule does not define. A Profile or implementation relying on the distinction for behaviour SHOULD
state how the labels are derived and from whose perspective. Architectural selection and
interoperability claims remain grounded in the underlying characteristics when the distinction
matters.

Atlas intentionally does not define **Remote Service** as a separate Component category. A service
reached through a network is a Component whose current binding has particular transport,
topological, authority-domain, latency, cost, and failure characteristics. A library, device,
service, cluster, and data centre may implement the same Component contract while exposing very
different selection characteristics.

This intentionally brings hot swapping, device replacement, service failover, and data-centre
cutover into one composition model. The concepts are not identical: remoteness alone does not make
a Component hot-swappable, and a hot-swappable Component need not be remote. They converge only
when a Hot-swap Host, Slot, Class, and conforming Component establish the replacement contract
defined below.

Hot-swap Class membership establishes semantic and lifecycle eligibility. A Host's policy may
further filter eligible Components by their current selection characteristics without changing
their Class conformance. Failure to meet a particular latency, cost, topology, capacity, or
availability policy makes a Component unsuitable for that binding at that time; it does not by
itself make the Component semantically non-conforming.

A **Component binding** associates a Component with a surrounding composition at its declared
contract boundary. Binding does not supply Capabilities implicitly and does not change the
canonical names or meanings of the bound contracts. The binding model distinguishes establishment
time from replacement guarantees:

- A **statically bound Component** has its participation fixed before the containing composition
  becomes active, whether by compilation, linking, firmware image construction, configuration, or
  deployment.
- A **runtime-bound Component** may be selected or attached while the containing composition is
  active. Runtime binding alone does not imply that the Component can later be detached or
  replaced.

#### Work in progress: the default portable binding

Composition requires a paved road as well as permission for specialised machinery. If every host
invents its ordinary seam independently, Components are composable only after bespoke integration;
if Atlas mandates one object model or call mechanism, ordinary composition pays for generality it
may not need. The proposed **Atlas Portable Binding** is therefore a first-party default binding
for general-purpose Component interchange, not the implementation model of Atlas Base.

An implementation or Component does not become non-conforming to Base merely because it does not
support the Portable Binding. A host or Component claiming support for a particular Portable
Binding version must preserve that version's complete observable contract. Authors remain free to
realise it through generated types, handwritten code, compiler lowering, static tables, reflection,
native interoperation, or another mechanism. The portable contract must never require a common
language object model or runtime library.

A **Binding Plan** is the scoped agreement established for one Component binding. It may be
constructed at compile time, link time, deployment, or runtime. It may exist as explicit data or be
compiled completely into direct calls and static storage. A Binding Plan establishes, as applicable:

- the negotiated Operations, Shapes, versions, required Declared Fragments, and canonical
  projection rules;
- the Actor endpoints and how Capabilities are presented, represented, or continued without
  becoming shaped ambient authority;
- whether each interaction is a call, asynchronous exchange, or Flow;
- the representation selected for each boundary value;
- memory domain, allocation, alignment, mutability, ownership, borrowing, lifetime, and release;
- readiness and completion synchronisation;
- framing, batching, buffering, backpressure, delivery, and admission limits; and
- failure, withdrawal, termination, and replacement-generation behaviour.

The proposed portable binary realisation uses a length-delimited framing layer and a restricted,
schema-guided CBOR representation for ordinary inline values. Contract establishment exchanges
canonical names, Shape and Fragment identities, versions, and authority requirements. The active
binding may then assign compact binding-scoped numeric identifiers to those canonical identities.
Such identifiers have no portable meaning outside their Binding Plan and never replace canonical
identity in manifests, signatures, persistence, or conformance claims.

Schema-guided CBOR is not a collection of self-describing JSON-like objects. Once an Operation and
its input Shape are established, each Item need not repeat the Operation name, Shape name, field
names, or full descriptor. Shape remains the type system; CBOR is one portable representation of a
value already governed by that Shape. The exact CBOR subset, scalar mappings, canonicalisation,
field and Fragment identifier rules, bounds, and treatment of unknown preserved data remain to be
defined and measured. JSON and BSON may be diagnostic, logging, persistence, or alternative
binding encodings, but neither defines the Portable Binding's semantics.

The same binding supports two default payload forms:

- an **inline shaped value**, owned by the receiving interaction and encoded through the portable
  representation; and
- a **referenced shaped resource**, which leaves bulk or device-specific data in an explicitly
  described buffer, memory domain, stream, device allocation, or other future `Resource`, while
  carrying the representation, access, ownership, lifetime, integrity, and synchronisation needed
  to use it safely.

The referenced form is part of the seam, not a bypass around it. A video frame, tensor, command
buffer, or bulk record may cross the portable binding as a compact typed reference while the data
remains in shared, device-local, registered, or otherwise suitable memory. A sender and receiver
that do not share a compatible representation may use an attributable adapter or the inline
fallback within declared size and resource limits. They must reject the binding or Item when the
fallback is unsupported or violates a required cost, latency, copy, memory-domain, or admission
constraint; a catastrophic implicit copy is not successful negotiation.

A specialised binding may replace the physical realisation without replacing the Component
contract. A generated direct call, function table, fixed-size ring, shared-memory queue, native ABI,
device queue, or network transport may lower the same Binding Plan. Negotiation and canonical-name
resolution occur before the hot path. A conforming high-rate realisation need not allocate a generic
value, perform string lookup, serialise CBOR, or materialise a complete Interaction for every Item.
Conversely, an ordinary seam may use the same Binding Plan machinery and select a direct inline
call; the portable design is not intentionally a slow path.

Shared representations require stronger rules than copied values. A Binding Plan must identify an
immutable borrowing interval, exclusive ownership transfer, copy-on-submit rule, versioned
integrity rule, or another mechanism preventing mutation after validation. It must also define
release and completion so a producer cannot reuse storage while a consumer or device still holds
it. A buffer, device, or session handle is a Resource reference unless the authority domain
explicitly recognises it as a Capability representation; structural possession alone grants no
authority (§16).

#### Where mapping lives

Atlas Base defines canonical Shape identity, compatibility, projection, and authority semantics.
It does not contain or require a universal **mapping engine**. The mechanism that maps private
language values, numeric binding identifiers, CBOR values, native layouts, and resource references
to the already agreed Atlas contracts belongs to the Component binding realisation and normally
runs in host or generated binding machinery.

This representation mapping may be a compiler-generated function, a static table, a trusted host
subsystem, or a separately replaceable Component. Making it a Component is useful when it is
independently selected, shared, inspected, metered, remote, or hot-swapped; it is not required when
the same work can be erased into a direct call. In either form, a representation mapper may not
invent Shape compatibility, authority, provenance, or guarantees absent from the declared
contracts.

Representation mapping must be distinguished from **semantic adaptation**. Mapping a C# record,
an F# record, CBOR fields, and a Rust structure that all realise the same Shape preserves one
contract. Translating between differently named Shapes or Operations, changing units, filling
meaningful missing information, dropping required semantics, or interpreting an authored native
contract as a standard one changes the contract. Such work must be exposed as an explicit Adapter
Component or attributable declared transformation with its own provided and required contracts,
costs, failure behaviour, and ordinary Capability requirements. A hidden mapping engine must fail
binding rather than silently manufacture semantic interoperability.

A **Replacement Slot** is a declared Component binding at which one occupying Component may be
substituted for another under a defined lifecycle. Replacement may require quiescence or restart of
the containing composition unless a stronger guarantee is declared. A slot is a logical
composition boundary; it does not imply a physical connector, memory location, process boundary,
or loader.

A **Hot-swap Slot** is a Replacement Slot whose active binding may change without terminating the
Component that owns the slot or its containing composition. A **Hot-swap Host Component** is a
Component that exposes one or more Hot-swap Slots. The Host defines each Slot's boundary, declares
what may occupy it, and coordinates or delegates activation and cutover. A Component may be a Host
for Components inside it while itself occupying a Hot-swap Slot at a larger composition boundary.

A **Hot-swap Class** is the declared conformance class of Components eligible to occupy a
particular kind of Hot-swap Slot. It is defined by a required Component contract, dependency
closure, and replacement protocol. It is not a source-language class, inheritance relationship,
package family, or set inferred from similarly spelled names. One Hot-swap Class may have many
independent implementations, and one Component may implement more than one Hot-swap Class.

A **Hot-swappable Component** is a Component that declares conformance to a particular Hot-swap
Class and fulfils that Class's candidate-side replacement obligations. Hot-swappability is
therefore relative, not universal: a Component may be hot-swappable in a Slot accepting one Class
and ineligible for a Slot requiring another, even when both Slots expose some of the same
Operations.

Interchangeability and hot-swappability are distinct. **Interchangeability** is a compatibility
relationship between Components under declared contracts. **Hot-swappability** is an operational
claim made jointly by a Host's Slot, its accepted Hot-swap Class, and a Component conforming to that
Class. A Component can be interchangeable but not hot-swappable, and a Hot-swap Slot can support
hot swapping only among Components in its declared Class.

Each Hot-swap Slot MUST declare the Hot-swap Class it accepts. Each Hot-swappable Component MUST
declare the Class or Classes it implements. Matching identifiers alone are insufficient: the Host
MUST establish compatible contracts and dependency closure before activation. The Slot and Class
contracts together MUST identify the replacement boundary and define or explicitly disclaim:

- whether compatibility is established by construction or runtime negotiation;
- the identity of Actors before and after replacement (§6.5);
- how authority is established for successor Actors and withdrawn or bounded for displaced Actors,
  without silent Capability transfer;
- state transfer or deliberate state loss;
- the handling of admitted and in-progress Executions and Flows;
- interruption, quiescence, failure, and rollback behaviour; and
- the cutover point at which the old binding ceases and the new binding becomes active.

Canonical Atlas contract names do not encode binding time. A standard Shape remains standard when
loaded dynamically, and an authored Shape remains authored when compiled statically. When a
portable Component or Hot-swap Class identity is exposed, it follows the authored-name rules of
§22, such as `Bob:PointerDriver`; source-language symbols and package names have no Atlas standing
unless they are explicitly mapped to such an identity.

The term **plugin** remains valid implementation or product terminology for a host-managed,
runtime-bound Component. It is not the architectural category: a plugin need not be detachable or
hot-swappable, and a hot-swappable mouse, remote service, or data centre need not be a plugin.

For example, a workstation or receiver may be a Hot-swap Host Component exposing a device Slot. A
mouse conforming to the Slot's Hot-swap Class is a Hot-swappable Component while exposing several
Actors and standard Input contracts. At another scale, a traffic-management Component may host a
Slot whose eligible Components are entire data centres. Each data centre may itself be a composite
Host containing thousands of Components and Actors. Replacement is evaluated at the declared Slot,
Class, and contract; Atlas does not privilege the smaller scale.

This section records the composition direction and vocabulary for experimentation. The exact
Component descriptor, Slot and Class representation, Binding Plan, Portable Binding framing and
CBOR profile, resource-reference contract, binding occurrence model, negotiation mechanism, and
conformance contract remain to be established through Fabric/Linen interchange and systems at
different scales. Until then, Component and the proposed Portable Binding do not enlarge Atlas
Base or imply a universal runtime loader, mapper, or transport.

## 19. Architectural Extensions: Flow and Event Distribution

An **Architectural Extension** adds a generic computational concept to Atlas.

Possible future extensions include:

```
Channel
Resource
Composition
Discovery
Runtime
Topology
Distributed
Identity
Persistence
Realtime
Presentation
Workspace
Intent
State
Transaction
Lifecycle
Time
```

These names are provisional and do not imply accepted Atlas extensions.

An extension may depend on another extension.
For example, a future `Distributed` extension might require communication semantics defined by
`Channel`. A future `Lifecycle` extension might describe long-running Executions and persistent
activities initiated by them.

A future `Composition` extension may ratify the Component, binding, and replacement direction in
§18.1. Static composition must remain possible without a loader or dynamic allocation. Runtime
binding and hot swapping may compose with `Discovery`, `Lifecycle`, `State`, or `Transaction`, but
must not acquire their guarantees merely by using the word Component.

Extensions should remain domain-neutral.
`Channel` may describe communication. It should not define how headphones transmit audio.
`Lifecycle` may describe the state of ongoing activity. It should not define the stages of a
financial audit.

Several provisional directions illustrate this rule. `Workspace` may describe application-facing
organisation of views, navigation contexts, tabs or panes, history, bookmarks, and provider-
supplied hierarchies, while `Presentation` describes the surfaces through which those views are
made perceptible and interactive. Neither facility is reserved for a system shell or Web runtime;
ordinary applications participate through the same extensions.

`State` may describe an identifiable observable condition, revisions, and authorised transitions
without implying a storage engine, query language, data model, or durability. `Transaction` may
describe a declared commit and atomicity relationship over participating effects, but must state
its isolation, durability, failure, withdrawal, and compensation semantics rather than implying
universal rollback. `Structured Data` is not retained as an Architectural Extension direction;
database-specific structure belongs in Profiles and Domain Vocabularies, while genuinely reusable
state semantics belong in `State`, Base Shape, `Resource`, and related extensions.

Shape and Transaction illustrate opposite results under the same Base criterion. Shape is Base
because independent implementations must agree on value structure to share Operation, Execution,
Event, and Constraint semantics, even when that agreement is realised entirely by static
construction (§16.5). Transaction remains an extension despite likely broad use because an
Execution may already be implemented atomically without exposing cross-effect transaction
semantics.

Likewise, an Execution may be implemented atomically without Atlas observing a Transaction.
The `Transaction` extension becomes relevant when commit, isolation, participation, or atomicity
semantics are exposed across multiple visible effects, Operations, Actors, or Resources. Including
the name in Base without those guarantees would be empty; requiring the guarantees universally
would burden systems that do not need them.

This distinction prevents Atlas itself from becoming a specification of every device and
industry.

Architecture 0.3 identifies `Flow` and `Event Distribution` as first-party extension directions
and defines their architectural placement. Their complete conformance contracts remain to be
ratified.

### 19.1 Flow

A **Flow** is an extension-defined, continuing relationship that carries a sequence of
Executions, Events, Outcomes, or typed Items under shared authority and delivery context.

Flow is not an Atlas Base term or Base occurrence form.

> *Base does not understand Flow. Flow understands Base.*

The extension is expressed through Base Operations, Events, and Outcomes:

```
Flow.Open             Operation
Flow.Opened           Outcome
Flow.Item             extension-defined occurrence
Flow.Acknowledge      Operation
Flow.GapDetected      Event
Flow.RequestReplay    Operation
Flow.Resume           Operation
Flow.Cancel           Operation
Flow.Closed           Outcome
```

`Flow.Opened` terminates the opening Execution and identifies the created Flow. `Flow.Closed`
terminates the Flow relationship. The Flow itself is the extension-defined context that relates
the occurrences between those points.

Flow adds:

- a Flow identity,
- declared participants or participant roles,
- an Item Shape,
- continuing authorisation,
- positions or cursors where ordering or recovery is declared,
- a delivery and recovery contract,
- backpressure or admission behaviour,
- and termination semantics.

Flow does not provide reliability, replay, total ordering, persistence, lossless delivery,
recovery, or exactly-once effects unless an extension version or Profile explicitly declares the
relevant behaviour.

#### 19.1.1 Flow Items and common context

A Flow carries Executions, Events, Outcomes, or the extension-defined `Flow.Item` occurrence
carrying typed data. Each occurrence may compose Interaction for attribution and correlation. The
Flow context may add:

```
flow-id
position
source-position
item-shape
integrity metadata
```

Stable context — participants, semantic name, origin requirements, encoding, Item Shape, and
authority — may be negotiated once at establishment and inherited by Items. An implementation is
not required to allocate or serialise a complete Interaction component for every high-rate Item.
A video frame
may be a compact `Flow.Item` under shared context; a bulk record may be a typed `Flow.Item`; an
Event Flow carries complete Events with Flow positions attached by the extension.

A Flow carrying Executions MUST NOT become an authority tunnel. Either each Execution is
independently authorised, or the establishing Capability explicitly grants the continuing class
of Operations and their Constraints. Arbitrary effectful data cannot inherit authority merely
by travelling inside an authorised Flow.

#### 19.1.2 Recovery contract

A Flow declares the recovery properties on which its endpoints may rely. Candidate properties
include:

```
ordering
delivery
replay support
retention
acknowledgement mode
gap policy
resume behaviour
revocation horizon
```

Reliable Flows may expose opaque cursors, cumulative or selective acknowledgement, replay ranges,
retention until acknowledgement, and resumption after reconnection. Ephemeral Flows may declare
that obsolete Items are skipped rather than replayed. File transfer, bulk import, Event replay,
video, audio, and pointer input therefore use the same extension without pretending that their
recovery needs are identical.

Atlas SHOULD standardise at-most-once and at-least-once delivery, stable Item identities,
deduplication support, and idempotency keys. It must not promise universal exactly-once effects;
those require cooperation from the receiving domain or transaction system.

#### 19.1.3 Programmer-facing use

Flow is an application-facing Atlas facility, not machinery reserved for Fabric or system
services. Language bindings SHOULD expose typed Items, asynchronous iteration or an equivalent
stream abstraction, cancellation, cursors, acknowledgement, and declared recovery behaviour.
Programs and Domain Vocabularies may define Item Shapes and use the same Flow semantics as device,
media, Event Distribution, and system infrastructure.

An implementation may optimise a Flow internally, but it must not make standard recovery or
authority semantics available only to privileged platform components when its conformance claim
offers them to applications.

#### 19.1.4 Base-only participants

A Base-only Actor may preserve or route a Flow occurrence and its Interaction composition where
policy permits. It does not thereby understand Flow identity, positions, ordering,
acknowledgement, gaps, replay,
resumption, backpressure, or termination, and MUST NOT claim to be a Flow endpoint.

An Actor that does not support Flow rejects an Execution of `Flow.Open` as unsupported. It must
not silently accept and treat subsequent Items as unrelated Events. An explicit adapter MAY downgrade a Flow
to individual occurrences, but it must declare every lost guarantee.

A routing intermediary need not understand Flow semantics if it preserves the Interaction
composition and extension metadata exactly. The actual endpoints and any intermediary that
modifies Flow state or guarantees MUST support the extension.

A minimal embedded Flow may use compile-time producer and consumer roles, a fixed-size buffer or
direct handoff, and integer positions. Networking, dynamic subscription, persistence, and
allocation are not required by the extension's architectural structure.

### 19.2 Event Distribution

Event is Base ontology (§14). Publication, observation, subscriptions, fan-out, fan-in,
filtering, groups, persistence, and replay are infrastructure defined by the **Event Distribution**
extension.

Two authorities are ordinary Capabilities over standard Operations:

```
Capability PublicationGrant:
    permitted-operation: Event.Publish
    subject: <Event subject>

Capability ObservationGrant:
    permitted-operation: Event.Observe
    subject: <Event subject>
```

Executing `Event.Publish` publishes an Event. A successful Execution of `Event.Observe`
establishes a Flow of Events; its Outcome identifies the subscription and Flow. Capability
mortality (§10.3) and the Flow's
revocation contract determine termination.

Broadcast is mediated. Fan-out is performed by an Event mediator Actor, not by an ambient bus.
Each observer receives an independently authorised subscription and Flow with its own cursor,
backpressure, retention, and delivery contract. One slow observer does not implicitly block the
others.

Fan-in is likewise mediated. Events from multiple publishers may be merged into one observer
Flow, but every Event preserves:

- its original emitting Actor,
- its Event identity,
- its source position where present,
- its origin and provenance,
- and any mediator-assigned Flow position.

The mediator must not launder multiple sources into its own origin. Replayed Events assert at
most `Origin.Derived` (§15), so replay cannot impersonate a live source.

Subscriptions may constrain subjects, structured name prefixes, publishers, authority domains,
origin classes, rate, retention, or assertion Shapes. Prefix matching is a syntactic selection over
the structured names of §22; it does not create semantic implication between those names.

Groups are mediated subscription sets. Authority to join, observe, or publish to a group is
explicit and delegable. A received Event carries no authority for a subscriber to initiate a
resulting Execution (§14); a reactive Actor either presents authority deliberately as its own or
receives authority explicitly attributable to the initiating request.

Event names and subject taxonomies belong to Domain Vocabularies
(`Sensor.Temperature.Changed`, `Deployment.Completed`). Event Distribution defines how those
Base Events move. A Base-only implementation may still realise an Event through direct return,
callback, or static dispatch without implementing this extension.

A minimal Event Distribution implementation is a static mediator dispatch table with
compile-time publishers and observers. Fan-out is a fixed sequence of calls; no ambient bus,
dynamic discovery, queue, or network is required.

## 20. Profiles

A **Profile** is a named interoperability contract.

Profiles group extensions and Domain Vocabularies, may require other Profiles, and may impose
additional behavioural requirements.

A Profile declares its direct dependencies with a `uses:` block. The block may name Profiles,
Architectural Extensions, and Domain Vocabularies:

```
Interactive Peripheral Profile uses:
    Connected Endpoint Profile 0.1
    Event Distribution 0.1
    Input 0.1
```

The block form is equivalent to repeating `uses` for every entry. The plural block removes
repetition without changing meaning; entries are independent and may be reordered without changing
one another.

Profile dependencies are transitive. Conformance to `Interactive Peripheral Profile` in the
example therefore includes every requirement of `Connected Endpoint Profile 0.1`; the declaring
Profile does not repeat that Profile's extensions or vocabularies. The full requirement set is the
transitive dependency closure. Circular Profile dependencies are invalid.

A Profile is not a level in an inheritance hierarchy.
A device or system may conform to several Profiles simultaneously.

Possible examples include:

```
Connected Endpoint
Interactive Peripheral
Audio Endpoint
Compute Node
Personal Workstation
Operational System
Interactive Application
Web
Database
```

A future Connected Endpoint Profile might declare:

```
Connected Endpoint Profile uses:
    Channel
    Discovery
    Flow
```

A future Interactive Peripheral Profile could then use it and add:

```
Interactive Peripheral Profile uses:
    Connected Endpoint Profile
    Event Distribution
    Input

Behaviour:
    defined connection-loss semantics
    Capability withdrawal reporting
    standard configuration discovery
```

A future Operational System Profile might require defined behaviour for long-running Executions,
Event delivery, Flow recovery, audit provenance, or activity lifecycle reporting.

A headset might conform to both:

```
Interactive Peripheral
Audio Endpoint
```

A smart monitor might conform to:

```
Interactive Peripheral
Audio Endpoint
Compute Node
```

Profiles describe useful combinations of Atlas behaviour without forcing all Atlas
implementations into a device or system hierarchy.

A mouse and a corporate operations platform may both implement Atlas while making very different
interoperability claims.

A Profile names the exact minimum versions of its direct dependencies (§23). Its expanded
transitive dependency closure makes the conformance claim complete and reproducible without
requiring every Profile to restate requirements already carried by another Profile.

### 20.1 Work-in-progress Profile directions

The following Profile directions are intentionally incomplete. Recording them establishes the
architectural target and tests whether Atlas provides sufficient primitives; it does not ratify
their final dependency sets or Domain Vocabularies.

An **Interactive Application Profile** may compose the shared facilities used by ordinary
applications and system-provided content:

```
Interactive Application Profile uses:
    Presentation
    Workspace
```

`Presentation` and `Workspace` are application-facing facilities. A conventional application,
filesystem navigator, settings surface, and Web implementation may all expose views through the
same subsystem while retaining their own Actors, Operations, Events, and specialised behaviour.
Tabs, panes, history, bookmarks, and trees are therefore not intrinsically browser or filesystem
features. A bookmark or history entry identifies a context; it does not preserve or grant the
authority needed to reopen it.

A future **Web Profile** may use the Interactive Application Profile rather than repeating its
requirements:

```
Web Profile uses:
    Interactive Application Profile
    Resource
    Runtime
    Channel
    Identity
    Intent
    Flow
```

The exact list remains provisional. The architectural direction is that Web content uses the same
Presentation and Workspace facilities as ordinary applications while adding Web-specific
runtime, identity, navigation, and communication semantics.

A future **Database Profile** is likewise benched as a recognised direction. Its purpose is to
make database providers first-class Atlas participants without defining a universal query
language, relational model, or lowest-common-denominator database API. A provider exposes one or
more Actors, obtains and evaluates Capabilities normally, declares the Domain Vocabularies and
Shapes it supports, uses Flows where transfer is potentially unbounded, and declares any
State, Transaction, Persistence, consistency, or Event Distribution semantics on which clients
may rely.

Provider and application Operations remain legitimate authored vocabulary. PostgreSQL, a graph
database, an embedded store, and a business application may expose different Operations while
sharing Atlas authority, attribution, Execution, Interaction composition, Flow, Shape, and Outcome
semantics. Portable
database behaviour requires a separately ratified common vocabulary; it is not implied merely by
conformance to the Database Profile.

The acceptance test for this direction is architectural: Atlas primitives must be sufficient to
construct a useful Database Profile and provider integration without adding database-specific
terms to Base. Detailed database semantics are deliberately deferred.

## 21. Domain Vocabularies

A **Domain Vocabulary** defines standard Atlas-native semantic concepts for a particular field.

Atlas does not need to know what a mouse is.

An Input vocabulary may define:

```
Input.Pointer.Motion
Input.Pointer.Button
Input.Scroll.Resistance
```

An Audio vocabulary may define:

```
Audio.Playback
Audio.Capture
Audio.Volume
```

A Sensor vocabulary may define:

```
Sensor.Temperature.Observe
Sensor.Orientation.Observe
Sensor.Temperature.Changed
Sensor.Orientation.Changed
```

A database operations vocabulary may define:

```
Database.Migrate
Database.Backup.Create
Database.Restore
```

An audit vocabulary may define:

```
Audit.Start
Audit.Suspend
Audit.Resume
Audit.Complete
```

These concepts use the Atlas authority model.

Domain Vocabularies allow independent devices and implementations to agree on semantic meaning
without Atlas Base needing to standardise every type of hardware, software system, or
organisational activity.

They are a major part of Atlas interoperability.

The common Actor model defines who may participate.
Capabilities define what authority is available.
Operations define semantically meaningful action.
Executions record concrete attempts of those Operations.
Events define attributable assertions about occurrences.
Outcomes define terminal completion and optional result values.
Domain Vocabularies allow independently developed participants to agree on what those
contracts and occurrences mean.

### 21.1 What a vocabulary must contain

A Domain Vocabulary is not a list of names. To earn its interoperability claims, every vocabulary
MUST contain:

- **Scope** — what is covered and, explicitly, what is not.
- **Model** — the Actors and authority directions involved.
- **Contracts, occurrences, and Capabilities** — Operations, Events, Outcomes,
  extension-defined occurrences, and the authority directions relevant to them, with normative
  observable semantics.
- **Shapes** — the independent input and output Shapes of every Operation, including their
  required Declared Fragments; and the canonical names, versions, and required Fragments accepted
  by every Event assertion, Outcome result or details value, and Constraint parameter, or explicit
  references to the specifications that define them (§16).
- **Constraint types** — the Constraints it defines for use with the Base algebra (§10.1), including
  the Shape of every carried value.
- **Conflict semantics** — what happens when authorised Operations collide (§6.9), and any
  ordering assumptions made about Events or extension-defined sequences.
- **Fences** — the predictable traps: behaviours a reasonable client would wrongly assume are
  guaranteed, explicitly declared unspecified (§29.1).
- **Verification surface** — which semantics are covered by conformance tests and which are
  trusted (§29.3).

The Input.Pointer vocabulary draft accompanies this specification as the template.

### 21.2 Interoperability claims are graded by precision

Conformance is the only mechanism, but vocabularies differ in how much their specifications pin
down, and therefore in what conformance buys:

For device-class vocabularies (Input, Audio, Sensor), full specification of observable semantics
is achievable and expected: any conforming implementation is substitutable for any other.

For organisational vocabularies (Audit, Deployment), full substitutability is neither achievable
nor the goal. Their value is **uniform participation**: the Operation gains explicit authority
requirements, attenuable Delegation, inspectable provenance, standard Outcome distinctions, and
compatibility with every generic tool that understands those things — Capability inspectors,
delegation viewers, policy engines, audit trails — none of which need to know what an audit *is*.

Base semantics are always fully substitutable. Vocabulary semantics are substitutable to the
degree the vocabulary's precision earns — and the vocabulary's verification surface (§21.1)
makes that degree visible rather than assumed.

## 22. Names, Authorship, and Declaration Prefix Blocks

Atlas names are structurally legible and semantically opaque (§6.10).

A canonical name has one of two forms:

```
ConceptPath
AuthorityPath:ConceptPath
```

with the provisional grammar:

```
CanonicalName  := [AuthorityPath ":"] ConceptPath
AuthorityPath  := Segment ("." Segment)*
ConceptPath    := Segment ("." Segment)*
```

The exact allowed characters within `Segment`, escaping rules, and namespace registration
process remain open for ratification. A canonical name contains no relative notation.

### 22.1 Standard Atlas names

Standard Atlas concepts use unqualified Concept Paths. Current and anticipated examples include:

```
Actor
Capability
Shape
Operation
Execution
Interaction
Event
Velocity
Flow.Open
Input.Pointer.Motion
Audio.Playback
Database.Migrate
Audit.Start
```

Unqualified, independently referenced canonical names are reserved for concepts ratified by an
Atlas specification. Every portable semantic concept with an independently referenced identity
that is not so ratified MUST use an authored canonical name of the form
`AuthorityPath:ConceptPath`. Names scoped inside an already authored enclosing contract, such as
the unqualified fields of an authored Shape, inherit that enclosing authorship (§16.1). An
implementation MUST NOT expose implementation-specific or privately authored functionality under
an unqualified canonical name merely because that functionality is compiled in, bundled by
default, or supplied by a widely used implementation.

Dots expose stable structural segments for parsing, grouping, discovery, prefix-block
declaration, and explicit syntactic filters. They do not create authority or semantic
subsumption. Supporting
`Input.Pointer` does not by spelling alone imply support for every name beginning with those
segments.

### 22.2 Authored names

The colon `:` separates an **Authority Path** from its authored Concept Path:

```
Linux:CGroup
Vulkan:Device
USB:HID
Logitech.MX:Input.Scroll.SmartShift
Sony.Headphones:Audio.AcousticOptimizer
Fabric:PredictivePlacement
Erste:Audit
```

Everything before `:` identifies the claimed namespace authority or authoring chain. Everything
after it identifies the concept defined by that authority.

This qualification records claimed **authorship**, not **Origin** in the sense of §15. Origin
describes what kind of cause produced an occurrence; an Authority Path identifies who claims to
have defined a semantic contract. Binding time does not affect either rule:
`Bob:DirectionalVelocity` remains authored when compiled into firmware, while a dynamically
obtained `Velocity` remains a standard Shape.

Authority Paths are hierarchical. In:

```
Logitech.MX:Input.Scroll.SmartShift
```

`Logitech` is the high-level namespace authority and `MX` is a subordinate authoring namespace.
This structure allows an organisation, standards body, project, team, product family, or
individual to subdivide authorship without flattening the result into one opaque vendor string.

Syntax is a claim, not proof. Parsing `Logitech.MX` identifies the claimed relationship; it does
not establish that Logitech authorised the `MX` subnamespace. Registry bindings, signatures,
delegated namespace authority, or other verification mechanisms belong to governance and future
Identity or Distributed specifications. An unverified authored name remains attributable only to
the party that actually presented it.

Authored qualification applies to portable semantic definitions, including Operations, Shapes,
Declared Fragments, Constraint types, extension concepts, and portable Component or Hot-swap Class
identities. It does not turn every runtime object into a canonical semantic name. In particular, a
Capability is a particular target-recognised grant (§10), not merely a permission name: its
authority provenance comes from Genesis and Delegation. An authored Capability template or
Constraint type uses an authored name; an individual grant retains its own identity, holder,
target, scope, and derivation.

A qualified name is not normative Atlas functionality solely because an Atlas implementation
exposes it. An organisation may initially define `Erste:Audit`. If a sufficiently general audit
vocabulary is later ratified, the system may expose `Audit.Start` with appropriate Constraints
and metadata.

Atlas may reserve an authored namespace for proposals incubated by the Atlas project. Such names
remain non-standard until ratified into an unqualified Atlas Concept Path.

For example:

```
Logitech:Input.Scroll.Resistance
Razer:Input.Scroll.Resistance
```

might contribute to:

```
Input.Scroll.Resistance
```

Promotion into the standard Atlas vocabulary is **ratification**, with the consequences defined
in §23.

### 22.3 Declaration prefix blocks

Specifications frequently declare several neighbouring concepts. Atlas documents MAY group such
declarations under an explicit prefix block introduced by `within`:

```
within Logitech.MX:Input.Pointer:
    Motion
    Button
    Button.Pressure
```

expands to:

```
Logitech.MX:Input.Pointer.Motion
Logitech.MX:Input.Pointer.Button
Logitech.MX:Input.Pointer.Button.Pressure
```

Likewise:

```
within KitchenWG:Kitchen.Toaster:
    Temperature
    Power.Draw
    Completion.Estimate
```

expands to:

```
KitchenWG:Kitchen.Toaster.Temperature
KitchenWG:Kitchen.Toaster.Power.Draw
KitchenWG:Kitchen.Toaster.Completion.Estimate
```

The rules are:

- a block opens with `within` followed by a canonical prefix — an Authority Path, the leading
  segments of a Concept Path, or both;
- every declaration inside the block expands to the block's prefix plus its own segments;
- declarations inside a block are mutually independent: inserting, removing, or reordering
  lines never changes the expansion of any other line;
- blocks do not nest, and a block ends at the next `within`, the next canonical declaration,
  or the end of its enclosing structure;
- expansion depends only on the enclosing block's stated prefix, never on document position
  beyond that block.

Prefix blocks are document notation only. Capabilities, signatures, hashes, wire
representations, discovery records, conformance tests, and ratification records MUST use expanded
canonical names. Moving text within a document must never silently change an already issued
Capability or signed semantic identity.

> **Note — design history.** Earlier drafts used dot-relative declarations (`..Button`,
> `...Pressure`), in which each leading dot retained one segment of the previous fully expanded
> declaration. The notation was rejected. Expansion depended on the previous declaration and
> therefore on document position: inserting or reordering a line silently changed the meaning of
> every line below it, and the dot-counting inverted familiar path intuition (more dots retained
> *more* context). These are unacceptable failure modes in precisely the documents whose contents
> ratification freezes forever (§23). Prefix blocks keep the brevity while making every line's
> expansion independent of its neighbours and locally visible.

## 23. Versioning and Ratification

The versioning problem for an authority model is concrete: a Capability granted against one
version of a vocabulary may be presented to an implementation of a later version. Whether it
still means the same thing must not be a matter of luck.

Atlas eliminates the problem rather than managing it, with one discipline borrowed from the
systems that got evolution right:

> **Ratified names are immutable.** Once a concept — Operation, Execution, Interaction,
> Event, Capability, Shape, Declared Fragment, Shape field, Constraint type, Event subject, or Outcome
> distinction — is ratified
> into a vocabulary, extension, or Base, its
> normative semantics are frozen forever. A change in meaning is a new name. Vocabularies are
> append-only namespaces: versions add concepts and may deprecate them; they never remove or
> repurpose them.

Capabilities reference canonical names; ratified names mean one thing forever; therefore
Capabilities do not carry vocabulary versions merely to defend against semantic reinterpretation.
Implementations may still differ in which additive concepts they support. Profiles and discovery
handle that availability skew.

Shapes and Declared Fragments additionally carry their own explicit structural versions (§16.2).
Those versions are monotonic and additive: they identify which optional structure is available,
never a breaking reinterpretation of a canonical name. Authored fragments compose with an open
Shape without mutating its canonical fragment or requiring a new name for every combination
(§16.3).

The supporting rules:

- **Version numbers are claims, not name parts.** `Input.Pointer 1.2` is a conformance claim:
  all concepts ratified through 1.2, with their frozen semantics. Minor versions are purely
  additive, so claims are comparable numerically — a 1.3 implementation satisfies every 1.1
  client. There are no breaking versions of a vocabulary, by construction; a fundamentally
  different model is a new vocabulary with a new name, and both may coexist indefinitely.
- **Deprecation marks, never breaks.** A deprecated concept remains normatively binding for any
  implementation claiming a version that includes it. Deprecation is guidance for new designs,
  not permission to drop support.
- **Unknown semantics fail safely.** An implementation encountering an unknown Constraint type
  denies (§10.1); encountering an unknown origin class, treats it as unverified (§15);
  encountering an unknown Operation rejects an Execution that names it; encountering an unknown
  extension occurrence does not process it or claim support for its semantics;
  encountering an unrecognised Shape with no compatible recognised lineage rejects it where
  interpretation is required; and encountering an unrequired fragment on an open recognised
  Shape ignores it for canonical projection without claiming its semantics (§16.4).
  The fail-closed rules adopted for security ensure older implementations degrade to stricter or
  unavailable behaviour rather than silently reinterpret authority.
- **Profiles pin and compose.** A Profile names exact minimum versions of its direct dependencies.
  Required Profiles contribute their own pinned dependency closures transitively (§20), so Profile
  conformance claims remain complete and reproducible without restating every indirect requirement.
- **Base freezes hardest.** Atlas Base pre-1.0 may change freely — that is what 0.x means. At
  1.0, the core terms' semantics freeze under the same append-only rule as everything else.
- **Ratification is the freezing moment.** When a name is promoted from a non-standard namespace
  into the standard vocabulary (§22), its semantics freeze irreversibly. The governance process
  must therefore include semantic review *before* ratification; that irreversibility is the
  point, not a hazard.

The openness presumption (§29.1) covers the remainder: unspecified behaviour may change at any
version, which is precisely why relying on it is non-conforming.

## 24. Devices and Atlas

A device does not need to run a conventional operating system to implement Atlas.

A device may itself participate in Atlas through one or more Actors.

A mouse may expose separate Actors concerned with pointer input and configuration.

Conceptually:

```
MousePointer
    emits Input.Pointer.Motion
    emits Input.Pointer.Button

MouseConfiguration
    exposes Input.Pointer.Sensitivity
    exposes Input.Pointer.PollingRate
```

The physical mouse is not the architectural primitive.
The Actors through which it participates are.

Note the direction of authority: publishing pointer input into the host's input system requires
authority over that system, even though the delivered Input occurrence is an Event.
The host grants publication and admission Capabilities to the device's Actors at attachment — a
Genesis occurrence (§12) under the host's attachment policy, typically liveness-scoped (§10.3) so that
detachment kills the authority with nothing to revoke, and typically carrying the
`Origin.Device` assertion grant (§15) because attachment is the moment the host observes the
physical fact it is vouching for.

Attachment is not cross-domain interaction, even though the device has its own implementation.
The host's attachment machinery creates Actors within the host's own authority domain that
represent the attached device. The binding between those Actors and the physical device is the
Actor–Execution binding of §6.5 — owned by the host implementation and, per §28, part of its
trusted computing base. A device's internal Atlas domain, where one exists, remains distinct;
attachment does not join the two domains architecturally. This is also what entitles the host to
grant the `Origin.Device` assertion: the vouching is intra-domain, made by the host's own trusted
machinery about a physical fact it directly observed. Where a future `Distributed` extension
defines attested federation, attachment MAY be upgraded to a verified cross-domain relationship,
and the Genesis occurrence becomes informed by attestation rather than by host policy alone.

A Base-only embedded connection may deliver the Events through static dispatch. A connected
peripheral may use Event Distribution and a Flow of Input Events. The Event meaning remains the
same; only the delivery contract changes.

One consequence deserves emphasis: synthetic input injection by unauthorised software becomes an
unauthorised Execution, mechanically denied — and injection by *authorised* software
(accessibility tools, remote desktop) is visibly distinguishable from device input, because
looking like a device is an authority those tools are not granted. This property is not a
feature of the Input vocabulary; it falls out of the Base model.

A manufacturer may additionally expose:

```
Logitech.MX:Input.Scroll.SmartShift
```

A host that understands the standard Input vocabulary can immediately interact with standard
functionality. It does not require a Logitech-specific application merely to configure pointer
sensitivity or polling behaviour.

The same model may apply to headphones.

A headset might expose Actors providing:

```
Audio.Playback
Audio.Capture
Audio.Volume
Input.Media.PlayPause
Sensor.WearState.Changed
```

and additionally:

```
Sony.Headphones:Audio.AcousticOptimizer
```

An Atlas-compatible system already understands the standard concepts.
The manufacturer remains free to innovate outside the standard.
Atlas makes the boundary visible.

## 25. Systems and Macro-Scale Operations

Atlas Actors may represent system boundaries larger than a process or device.

A system may expose an Actor through which other Actors interact with its semantic
functionality.

For example:

```
DatabasePlatform
    exposes Database.Migrate
    exposes Database.Backup.Create

AuditPlatform
    exposes Audit.Start
    exposes Audit.Suspend

DeploymentPlatform
    exposes Deployment.Begin
    exposes Deployment.Rollback
```

The internal implementation of such a system may contain many processes, services, databases,
queues, users, and further Actors.

Atlas does not require those internals to be visible to every caller.
The exposed Actor represents the architectural boundary at which authority is exercised.

This allows a system to communicate more than transport-level intent.

A conventional API request may state:

```
POST /jobs
```

with an implementation-specific payload.

An Atlas Operation may state semantically:

```
Audit.Start
    organisation: Erste
    scope: FinancialControls
```

The exact metadata model is outside Atlas Base and remains to be specified.

The architectural difference is that the Operation's semantic identity and required authority are
first-class. Its Outcome may identify a created long-lived activity; that activity may emit Events
and later terminate through another Outcome. This supports richer policy, Delegation,
observability, discovery, recovery, and interoperability in higher-level Atlas specifications.

Atlas does not require every API call to become an Atlas Operation.
Atlas Operations should represent semantic actions at boundaries where explicit authority and
interoperable meaning are useful — a boundary is worth an Operation where uniform exposure is
valuable even if substitution is not (§21.2).

## 26. Admission: Interacting with Bounded Capacity

Every Actor has bounded capacity. A human has limited attention. An autonomous Actor has limited
compute. A device has a limited duty cycle. A service has a limited queue.

Requesting service from an Actor consumes that Actor's capacity.

> An Actor, or a guardian acting for it, MAY treat the right to request as authority: an
> **admission Capability**, granted, attenuated, and delegated like any other. Where admission
> is capability-guarded, an unauthorised request is denied by the standard authority machinery
> before it reaches the expensive capacity it targets.

Validation, transport, and rejection still consume bounded front-door resources; admission is not
a claim to eliminate physical or network-level denial-of-service. It prevents unauthorised
requesters from reaching more valuable attention, compute, device duty cycle, or service queues
and limits amplification after the authority boundary.

Spam and prompt-flooding of autonomous Actors can therefore become unauthorised Executions rather
than only application-specific categories; rate limits are ordinary Constraints (`max-rate`,
liveness-scoped leases); and admission composes with Delegation — a service granted admission to
an agent may delegate narrower admission to its own sub-workers.

### 26.1 The human seam

Human interaction endpoints are the strictest instance of admission, in two layers.

**Admission before presentation.** A request to interact with a human Actor carries its
Delegation chain like any other Execution. The human's guardian implementation (operating system,
device, agent shell) applies policy *before anything renders*: chains that do not terminate in a
trusted primordial root, or that arrive from unverified or non-whitelisted origins, are refused
or quarantined mechanically. The naive attack population never reaches the person. Requesting a
human's attention is itself an Operation, requiring a Capability granted by the human's guardian
— which makes phishing not a UX failure but an unauthorised Execution, denied by the same
machinery that denies any other Execution.

**Bound consent.** For requests that pass admission, "the human approved it" must mean
approved-as-shown, by a verified human act — not that some process holding the human's delegated
authority clicked a button. Following the pattern of §10.3, Base states the obligation rather
than the mechanism. The `Intent`/`Presentation` extensions MUST define:

- how an approval record identifies the presentation the human actually acted upon,
- how the guardian vouches the origin class (§15) of the response through its trusted input
  path,
- and how both bind to the resulting Delegation record.

One possible realisation hashes the presentation artifact into the Delegation record; the choice
of mechanism is not Base semantics.

The full presentation mechanics belong to those extensions. Base carries only the principle:
human participation flows through guarded, recordable interaction endpoints, and humans differ
from other bounded-capacity Actors in policy strictness, not in mechanism.

## 27. Atlas and Existing Systems

Atlas is deliberately agnostic about implementation depth.

A firmware system may implement the Atlas model directly:

```
Atlas
  ↓
Firmware
  ↓
Hardware
```

A hosted runtime may implement Atlas above an existing operating system:

```
Atlas software
  ↓
Atlas runtime
  ↓
Host adapter
  ↓
Linux
```

An operating system may implement Atlas through native services:

```
Atlas software
  ↓
Native Atlas services
  ↓
Kernel
```

A future operating system may use Atlas as its primary computational architecture.

These are implementation choices.

Atlas should allow movement between them without requiring Atlas-native software to adopt an
entirely new authority model.

This is particularly important for hosted implementations.

Linux may be used because it already solves difficult hardware and compatibility problems.
The Atlas architecture must not therefore become Linux-shaped.

Existing systems are substrates and integration targets.
They are not the ontology of Atlas.

## 28. Threat Model

**In scope.** Atlas's authority semantics are designed to withstand: malicious or compromised
Actors within an authority domain attempting to forge, amplify, or replay authority; confused
deputies exercising authority on behalf of unauthorised requesters; masquerade — presenting an
effect as originating from a source class it did not (§15); malicious peers in cross-domain
interaction presenting invalid or over-broad Delegation chains; malformed Shape values attempting
to exploit ambiguity between implementations; authored fragments attempting to reinterpret a
canonical Shape or smuggle authority through ignored structure; and reliance on unspecified
behaviour as an escalation path.

**Out of scope.** Atlas does not defend against compromise of the authority domain's own
implementation — each domain's implementation is its trusted computing base, and a domain that
lies about its enforcement lies about everything. Side channels, physical attacks, and
denial-of-service below the admission model (§26) are likewise outside the authority model,
though extensions and Profiles MAY address availability.

**Information flow.** Atlas constrains *access* at every architectural boundary; it does not
constrain *re-propagation* after delivery. An Actor authorised to observe data may thereafter
transmit what it observed. Exfiltration by an authorised observer is out of scope; unauthorised
observation is in scope. The positive claim Atlas does make is **legibility**: because
observation is capability-gated, every first hop of every data flow is an explicit,
attributable, auditable grant — delegations of observation are visible data-sharing decisions.
Atlas cannot prevent a betrayal of trust; it guarantees the trust was explicit and the betrayer
is identifiable.

**Cross-domain trust** extends exactly as far as verification of the other domain's attestation,
and no further.

## 29. Conformance

Atlas conformance is behavioural.

An implementation conforms to an Atlas specification when it preserves the observable semantics
required by that specification.

### 29.1 The openness presumption

> Any behaviour not explicitly specified by an Atlas specification is unspecified. Unspecified
> behaviour is presumed open to change between implementations and between versions of an
> implementation. Reliance on unspecified behaviour is non-conforming use.

Stated once, here, inherited by every extension, Profile, and Domain Vocabulary.

This clause exists because its absence has a known outcome: where a specification is silent, the
first popular implementation's behaviour becomes the de facto contract, accident by accident,
until the implementation *is* the specification and can never change. The presumption ensures
Atlas's de facto behaviour can never quietly become its de jure contract — including Fabric's
(§6.8).

The presumption alone is insufficient — ecosystems depend on de facto behaviour despite
universal disclaimers. Vocabularies therefore additionally SHOULD fence predictable traps: the
small set of behaviours a reasonable client will wrongly assume are guaranteed (§21.1).

### 29.2 The conformance shape

For example:

```
Given:
    Actor A holds Capability X permitting:
        Fan.SetSpeed
        Fan.Stop

And:
    X permits Delegation

When:
    A derives Capability Y for Actor B from X

And:
    Y adds permitted-operation: Fan.Stop

Then:
    B may execute Fan.Stop using Y

And:
    B may not execute Fan.SetSpeed using Y
```

Shape conformance includes additive cross-implementation compatibility:

```
Given:
    Velocity 1 is an open record Shape
    whose canonical fragment requires speed

And:
    Bob:DirectionalVelocity 1 is a Declared Fragment for Velocity 1
    and requires direction

When:
    a component accepting Velocity 1 receives a valid value shaped as
    Velocity 1 + Bob:DirectionalVelocity 1

Then:
    it accepts and processes the Velocity 1 projection

And:
    it ignores Bob:DirectionalVelocity 1 if it does not understand that fragment

And:
    it does not claim support for Bob:DirectionalVelocity 1

And:
    a component requiring Velocity 1 + Bob:DirectionalVelocity 1
    rejects a value carrying only Velocity 1

And:
    removing, redefining, or changing the Shape of speed is rejected as incompatible
```

One implementation may enforce this through static firmware structure.
Another may use kernel Capabilities.
Another may use cryptographically verifiable Delegation records.

The internal mechanism is not the conformance target.
The effective behaviour is.

The scale of an Operation does not change this requirement.
An implementation exposing `Audit.Start` must preserve the authority semantics attached to that
Operation just as an embedded implementation must preserve the authority semantics attached to
`Fan.Stop`.

### 29.3 Verifiable and verified

Two claims that must not be conflated:

> A specification is **verifiable** to the extent that its normative semantics are mechanically
> testable. An implementation is **verified** with respect to a specification when it has passed
> that specification's conformance suite, attested by a named party against a named suite
> version.

The boundary between verifiable and trusted falls *within* every occurrence, not between
occurrence forms:

- **Atlas mechanics** — Operation and Execution invariants, Interaction composition, Shape
  identity, version compatibility and projection, Capability recognition, authority evaluation,
  attenuation, Delegation validity, Event attribution and immutability, Outcome distinctions, and
  declared extension transitions — MUST be verifiable wherever their observable semantics apply.
- **Domain effect** — that the fan physically stops, that an audit meaningfully occurs — is
  verifiable in degrees. Each Domain Vocabulary MUST declare which of its domain semantics are
  covered by conformance tests and which are trusted (§21.1).

The two rules compose:

> The guaranteed surface of an Atlas specification is its normative text; the *attested* surface
> is what its conformance suite tests; everything else is unspecified and open.

The suite is evidence, not definition — the prose specification remains normative.

Where a domain effect is not mechanically verifiable, Atlas does not pretend trust away; it
makes trust attributable. The responsible party is the Actor exposing the Operation, and the
Delegation chain records who granted what through whom. The design rule: *mechanically verify
everything verifiable; for the remainder, ensure the trusted party is explicit, named, and
reachable through provenance.*

Attestation is flat — "verified against suite X version N, attested by P" — not graded
certification levels.

Atlas should be accompanied by conformance tests wherever normative behaviour can be tested
mechanically. Profile conformance requires satisfaction of the extensions, vocabularies, and
additional behavioural requirements defined by that Profile. Non-standard functionality must not
silently satisfy normative Atlas requirements unless a specification explicitly defines such an
integration.

### 29.4 A worked attack

A specification of an authority model should show an attack failing, not only delegation
working.

```
OperationsSystem holds Capability DeploymentGrant:
    permitted-operations:
        Deployment.Approve
        Deployment.Rollback

OperationsSystem derives Capability StagingApproval:
    from: DeploymentGrant
    for: BuildAgent
    adding:
        permitted-operation: Deployment.Approve
        environment: staging

BuildAgent exposes:
    CI.RequestApproval

PluginActor holds:
    CI.RequestApproval
```

A compromised `PluginActor` initiates an Execution of `CI.RequestApproval` naming deployment
`#1234` — which targets production.

If `BuildAgent` responds by executing its own `Deployment.Approve` against the resolved
deployment, the outcome depends on rules this specification makes normative:

1. The target's implementation evaluates the `environment: staging` Constraint when the Execution
   is presented
   (§10.1), finds production, and denies. The attack fails.
2. Had the target not understood the Constraint, it would deny under the fail-closed rule
   (§10.1) rather than approve. Without fail-closed evaluation, every Constraint-vocabulary
   evolution step is a privilege-escalation window.
3. Had `BuildAgent` "already checked" the environment at request time, the check would have
   validated the *request's claim*, not the resolved deployment — which is why evaluation is
   placed at the authority boundary, when the Execution is presented.
4. Under the invocation principle (§13.6), `BuildAgent` must not silently use broader
   ambient authority on requester-supplied designations. The request carries only
   `CI.RequestApproval`; `PluginActor` holds no deployment authority. If `BuildAgent` deliberately
   presents `StagingApproval` as its own policy decision, that choice is recorded and the staging
   Constraint still denies production. Provenance records both the initiator and the deputy.

## 30. Atlas, Fabric, and Linen

Fabric is the first implementation of Atlas.

Its purpose is to test whether the Atlas model remains useful when applied to larger and
heterogeneous systems.

Fabric is expected to explore areas such as:

- multiple cooperating devices,
- resource discovery and selection,
- remote execution,
- persistent identity,
- humans and autonomous Actors participating in common Delegation relationships,
- semantic device integration,
- macro-scale semantic Operations,
- and modular application environments.

These are not arbitrary features attached to Atlas after the fact.
The desire to describe such systems coherently is one of the reasons Atlas exists.
They are not part of Atlas Base because Atlas attempts to derive them from smaller architectural
concepts and explicit extensions.

Fabric may experiment with concepts before Atlas standardises them.
Fabric-specific functionality uses the `Fabric:` namespace.

For example:

```
Fabric:PredictivePlacement
```

Atlas must not adopt a concept solely because Fabric implements it.

Fabric is an experiment, implementation, and source of evidence.
It is not the specification.

**Linen** is planned as a second, deliberately independent full-stack implementation. Its primary
purpose is narrower: to test Atlas composability and substitutability. Linen implements its own
components rather than treating Fabric as the hidden platform, and favours a lean implementation
surface where Fabric favours a practical showcase.

The proof is component interchange, not merely two programs passing the same conformance suite.
Where both stacks implement the same Profile, Extension, and Domain Vocabulary contracts, a Linen
component should be usable within a Fabric environment and a Fabric component within a Linen
environment without either side depending on the other's private types or conventions.

Component has the scale-independent meaning described in §18.1. The first interchange proof may
use process-sized Components because they are practical test instruments; that does not restrict a
Component to a process, package, or application module. Interchange tests substitutability under
declared contracts. Runtime binding and hot swapping are stronger, separate claims and require the
additional replacement semantics defined there.

Shape is central to this test (§16). Shared Operation names and Capability semantics do not create
interoperability when the components disagree about the structures those Operations and
Constraints carry. Fabric and Linen may use different language types and encodings; their shared
input and output Shape identities, Declared Fragment identities, versions, and compatibility
rules are the architectural contract.

The interchange experiment should prototype the proposed Atlas Portable Binding rather than
allowing its first process protocol to become an accidental private convention. Fabric and Linen
should implement the binding independently, including Shape-guided inline values, binding-scoped
identifiers, authority presentation, and at least one referenced-resource or pooled-buffer path.
The experiment supplies evidence for the binding; agreement between the two implementations does
not ratify it by itself.

Interchangeability is always scoped to the declared contracts. A Fabric or Linen component that
requires additional Profiles, extensions, vocabularies, authored Operations, Shapes, or Declared
Fragments is not substitutable for a component lacking those requirements, and must expose
that difference explicitly. This is expected composition, not a failure of Atlas.

Linen-specific functionality uses the `Linen:` namespace. Atlas must not adopt a concept merely
because Fabric and Linen both happen to implement it.

Fabric and Linen are complementary experiments and sources of evidence.
Neither is the specification.

## 31. Related Work

Atlas stands in a lineage, deliberately. Most of its hard sub-problems have been solved at least
once, and the failures are as instructive as the successes.

**Capability operating systems.** KeyKOS, EROS, and Coyotos proved that capabilities-as-the-only-
authority is viable for a whole system, and contributed the revocation-via-indirection pattern.
seL4 demonstrated formally verified capability enforcement; its capability derivation tree is
the structure Atlas's Delegation graph abstractly is (§11), and the framing against which future
revocation semantics will be defined. Capsicum demonstrated the hybrid adoption path — capability
discipline coexisting with an ambient-authority host — which is the strategic position of Fabric
on Linux; its lesson is that the seam is where the model leaks.

**Distributed object capabilities.** The E language and CapTP (today OCapN) contain the deepest
treatment of capabilities across machine boundaries — unforgeable remote references, membranes —
and the definitive analysis of confused deputies, which §13.6 inherits. Macaroons proved
monotonic caveat-based attenuation in production and is the direct source of §11's structural
rule; its bearer-token weakness motivates proof-of-possession representations at the
cross-domain tier. Biscuit extends the same idea with offline public-key attenuation. UCAN is
Atlas's nearest relative in ambition — humans, services, and devices in one delegation model.
SPKI/SDSI is the direct intellectual ancestor: authorization rather than identity certificates,
and local names rather than global ones (§8); its failure to deploy teaches that being right is
insufficient without a coexistence path.

**Contrast cases.** Zanzibar-style relationship-based access control is the opposite
architecture — authority as a central database queried at check time rather than held, delegable
references — and the default assumption Atlas must explain itself against. OAuth 2 scopes are
the incumbent for cross-organisation delegation: coarse, non-attenuable, with delegation bolted
on; Atlas is in part "what scopes should have been." The WebAssembly component model shows the
industry independently converging on capability-secure boundaries at yet another scale.

**Vocabulary governance.** USB HID is the existence proof that device-class vocabularies can
achieve full substitutability with vendor extension space. Bluetooth profiles show the median
outcome (fragmentation); UPnP shows the failure mode (no conformance teeth — hence §29.3);
Matter shows the modern cost (certification requires institutions); schema.org shows vocabulary
sprawl when adding terms is free (hence ratification discipline, §23).

Atlas's distinctive synthesis is one authority model spanning `Fan.Stop` to
`Accounting.ClosePeriod`, with semantic Operations, Executions, and Events first-class at every
scale; the
Embedded Test as a standards-design constraint; and humans and autonomous systems as peers in one
delegation calculus. Individual mechanisms have precedents. Their combination and intended scope
are the architectural claim Fabric must test.

## 32. Authors' Discussion: The Larger Direction

Atlas Base is intentionally much smaller than the systems that motivated it.
This is a deliberate tension.

The authors consider interoperability between radically different computational participants to
be central to the larger Atlas direction.

A future Atlas environment may contain:

```
Humans
Applications
Services
Autonomous systems
Peripheral devices
Embedded controllers
Workstations
Servers
Remote infrastructure
Organisational systems
```

Atlas does not seek to erase the differences between them.
It seeks to give them a common language for participation, authority, Delegation, attributable
Events, and meaningful action.

The Actor abstraction is central to this goal.

A human asking an autonomous Actor to review code and an autonomous Actor asking a human to
clarify intent initiate structurally similar Executions. Both involve one Actor requesting
participation from another — and both, under §26, involve admission to a bounded-capacity
participant.

A process authorising a worker and an embedded controller authorising a safety subsystem
likewise involve bounded authority and Delegation. At larger scale, an operations system
authorising a migration or an organisation initiating an audit may follow the same architectural
rules.

The size of the action changes.
The authority model need not.

The authors are also particularly interested in computing environments where the boundary of a
physical device is no longer treated as the natural boundary of computation. A workstation,
phone, wearable, server, peripheral, and remote compute environment may participate in one
computational environment. Resources may be selected according to topology, trust, latency,
cost, and availability rather than a binary distinction between local and remote.

Section 18.1 applies this direction to Components explicitly. A remote service is not a separate
architectural species: it is a Component with a particular observable placement and operational
envelope. Conversely, a physically local Component may cross a stronger authority or failure
boundary than a remote Component. Selection therefore operates over attributable, scoped
characteristics rather than inferring behaviour from location labels.

Atlas Base does not define this environment. The anticipated Architectural Extensions are
intended, in substantial part, to make such environments possible.

Semantic interoperability is another major direction.

Applications, devices, and larger software systems currently bundle large amounts of meaning
inside implementation-specific interfaces. A mouse exposes configuration through a vendor
application. A headset exposes semantic device state through proprietary software. A corporate
system may expose an operation such as beginning an audit only through undocumented combinations
of API endpoints, payload conventions, workflow state, and organisational knowledge.

Standard Atlas-native Domain Vocabularies may allow systems to expose more of that semantic
structure directly.

Because occurrences can compose the standard Interaction fragment, an implementation may remain
structurally capable of attribution, routing, preservation, and discovery even when it does not
understand a domain concept. Installing or discovering a vocabulary, Profile, or adapter may add semantic
understanding without changing the underlying authority relationship. Understanding a concept
does not itself grant authority to execute its Operation.

A mouse exposing standard configuration Operations should not require a vendor-specific
configuration application for routine settings. A headset exposing standard Audio, Input, and
Sensor concepts should remain fully usable without installing a manufacturer's private software
environment. A system exposing `Audit.Start` should be able to make the action, its authority
boundary, and relevant metadata visible as semantic concepts rather than requiring every
participant to infer them from transport details.

These directions are not accidental uses of Atlas.
They are part of the reason for defining Atlas.

The purpose of keeping Base small is not to minimise the ambition of the architecture.
It is to avoid permanently embedding the shape of today's largest systems into the foundation of
tomorrow's.

## 33. Open Questions

Architecture 0.4 preserves the current Base, composition, Profile, and implementation directions;
§35 records the changes from 0.2 and §36 records the changes from 0.3. The following remain
genuinely open.

**Revocation beyond mortality.**
Liveness-scoped authority (§10.3) covers the common cases cheaply. Immediate revocation of
long-lived authority, the precise semantics of the revocation horizon, and the fate of in-flight
Operations and Flows when authority dies mid-execution remain unresolved. Atlas should
distinguish authority withdrawal from cancellation of an already accepted activity. Long-running
Operations likely need declared safe checkpoints, commit points, compensation Operations, and a
maximum revocation horizon. The Delegation derivation graph (§11) is the structure authority
withdrawal will prune; `Lifecycle` and `Flow` must define what existing work then does.

**Cross-domain interaction.**
Base authority semantics are defined within a domain (§8). Mutual identification, attestation,
the cryptographic representation of Capabilities and origin claims, and defence against a
hostile domain vouching falsely are the substance of the `Identity` and `Distributed`
extensions, and the largest body of unfinished work in the Atlas direction. Device attachment no
longer waits for this work: §24 handles attachment entirely within the host domain, with attested
federation as a later upgrade.

**Channel.**
The invocation principle (§13.6) requires that authority travel with requests; the mechanism —
request/response representation, error propagation across boundaries, delivery semantics —
awaits the `Channel` extension. Until it exists, the principle constrains implementations
without fully equipping them.

**Portable Component Binding and mapping.**
Section 18.1 proposes the Atlas Portable Binding and a scoped Binding Plan as the general-purpose
seam. Its exact framing, schema-guided CBOR subset, scalar and field mappings, numeric dictionary,
resource-reference representation, ownership and synchronisation rules, bounds, negotiation,
fallback behaviour, and conformance surface remain open. Fabric/Linen interchange must compare an
independently implemented portable path with direct-call, pooled-buffer, and process-isolated
realisations. The resulting evidence must distinguish protocol cost from implementation cost.

Base defines compatibility but contains no mapping engine. Experiments must determine the minimum
host mapping responsibilities and when a mapper should be exposed as a Component. They must keep
representation mapping within one Shape contract separate from semantic adaptation between
different Shapes or Operations; the latter must not disappear into trusted host machinery.

**Flow conformance.**
Section 19.1 defines Flow's architectural placement and candidate recovery contract, not a
ratified protocol. Cursor identity, ordering scopes, acknowledgement modes, replay retention,
gap repair, reconnection, multi-source sequencing, backpressure, and revocation interaction need
precise semantics. Flow must remain efficient for video and input while remaining recoverable for
files, bulk records, and durable Event streams.

**Event Distribution defaults.**
The mediator model, independently authorised subscriptions, and provenance preservation are
clear. Default delivery guarantees, group semantics, filter expressiveness, loop prevention,
durable replay, and failure behaviour for multi-source fan-in remain to be specified. A received
Event must never become an implicit grant for reactive authority.

**Interaction composition representation.**
Architecture 0.3 defines semantic fields, not a wire encoding. The minimum identity rules,
protected fragment data, and compact reuse of shared context need stress-testing.
`emitted-at` is signed integer milliseconds in a named time domain; the standard time-domain
registry and richer uncertainty semantics remain open.

**Shape catalogue and composition.**
Section 16 defines independent input and output Shapes, additive same-name evolution, arbitrary
fragments as projections, named and versioned Declared Fragments, open-record composition, and
canonical projection. The exact standard scalar catalogue, recursive Shapes, evolution of choices,
canonicalisation for signing and hashing, host-version requirements for fragments, cross-fragment
invariants, descriptor discovery, and representation negotiation remain to be specified. Fabric
and Linen must demonstrate that unknown Declared Fragments can be ignored for a canonical
projection without losing them in components claiming transparent forwarding.

**Enrichment, ambient scope, and propagation.**
Section 16.6 records a non-normative design direction for adding absent fragments from already
available information and carrying values without making intermediate modules semantic consumers.
Targeted Enrichment has a comparatively clear boundary. Global storage is not prohibited, but
storage, discoverability, authority, scoped availability, and declared consumption must remain
distinct. Ambient Enrichment, propagation across dynamic direct calls, conflict resolution,
provenance, expiry, confidentiality, and the carrier scope that reconciles a module heap with
observable execution graphs remain unresolved. Enrichment must not become hidden Capability
invocation, undeclared global consumption, or authority amplification. Whether composition needs a
separate Capability-binding or authority-provisioning mechanism also remains open.

**Is Actor sufficiently universal?**
Actor must be stress-tested against embedded systems, human participation, autonomous systems,
devices, composite systems, and conventional software without becoming so broad that it loses
architectural meaning.

**Genesis and resource creation.**
Genesis roots authority, but dynamic systems routinely create new resources and issue authority
over them as the result of authorised Operations. The future `Resource` extension must distinguish
domain-level Genesis from attributable issuance by an Actor already authorised to create or
administer a resource, without turning Genesis into an unconstrained minting escape hatch.

**Resource, State, and Transaction.**
The candidate `Resource` extension concerns identity, lifetime, and authority over created things;
`State` concerns observable condition, revisions, and authorised transitions; `Transaction`
concerns declared commit and atomicity relationships. Their exact boundaries, composition, and
minimal contracts remain open. None may assume a database, filesystem, persistence mechanism,
universal rollback, or one data model merely because those are important consumers.

**Presentation and Workspace.**
`Presentation` and `Workspace` are intended to be shared, application-facing facilities rather
than private machinery of a shell, filesystem navigator, or Web implementation. The boundary
between surfaces and interaction on one side, and views, navigation contexts, tabs, panes,
history, bookmarks, and provider-supplied hierarchies on the other, requires a dedicated
specification. Saved navigation must identify a context without silently preserving authority.

**Work-in-progress Profiles.**
Section 20.1 records Interactive Application, Web, and Database as Profile directions without
ratifying their dependency sets. The Database Profile is specifically an architectural
acceptance test: provider-specific calls must remain possible while Atlas supplies sufficient
generic authority, Base Shape, Flow, State, Resource, Transaction, and Outcome machinery to integrate
the provider as a first-class participant.

**Composition and hot swapping.**
Section 18.1 defines provisional scale-independent Component terminology and distinguishes static
binding, runtime binding, replacement, interchangeability, and hot-swappability. The Component
descriptor, recursive composition rules, Binding Plan representation, Hot-swap Slot and Class
representation, dependency negotiation, attachment and replacement occurrences, state handoff,
failure atomicity, rollback, interruption guarantees, and treatment of in-progress work remain
open. Fabric/Linen interchange, device replacement, service failover, and data-centre-scale
substitution should test whether one contract model survives all these cases without making a
package loader, mapping engine, or deployment orchestrator part of Atlas Base.

The same section treats topology, authority-domain boundaries, latency, cost, capacity,
availability, and related values as attributable selection characteristics rather than reducing
placement to `local` or `remote`. Their portable Shapes and units, provenance, attestation,
freshness, measurement semantics, aggregation, privacy, and policy language remain open. A
Component descriptor's self-claim must not silently become a verified placement or service-level
guarantee.

**Fabric and Linen interchange.**
Passing the same conformance suite is necessary but not sufficient evidence of component
interchangeability. The component boundary, binding metadata, Shape negotiation, host services,
and minimum adapter responsibilities must be discovered by exchanging real Fabric and Linen
components without allowing either implementation's private object model to become an Atlas
contract.

**Conformance inside composite Actors.**
When a platform exposes one Actor at its boundary (§25) while containing thousands of internal
services, what does conformance require of the interior? "Where Atlas applies" needs a
definition with edges.

**Authored namespace verification.**
Authority Paths are structurally hierarchical (§22), but syntax is not proof. Registry,
signature, delegated namespace authority, collision handling, display-name safety, and recovery
from compromised authoring keys require a governance and Identity design.

**Where does an Architectural Extension end and a Domain Vocabulary begin?**
Some concepts are clearly architectural; others clearly domain-specific. The boundary should be
defined carefully enough to prevent Atlas from absorbing every semantic standard built on top of
it.

## 34. Summary

Atlas is an open specification for actor-centric, capability-based computing and
interoperability.

Its core principle is:

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

An Actor is defined by participation in the Atlas authority model rather than by implementation
form or scale. A process, firmware subsystem, peripheral, human, autonomous system, or composite
organisational system may participate through Actors. Atlas does not claim these participants are
equivalent; it provides a common architectural model through which they may cooperate.

Operation is a stable named contract for a requested effect. It declares one input Shape and one
independent output Shape. Execution is one concrete attempt to execute an Operation by presenting
a Capability at the target authority boundary. It carries the input value and occurrence-specific
context; it may be rejected before effect begins, accepted and fail, or complete successfully.
Replaying an Execution may repeat an effect.

Capability is a target-recognised explicit grant. It authorises named Operations and therefore
binds transitively to those Operations' Shapes and required Declared Fragments. Recognition means
the target can interpret and evaluate that authority; it does not promise holder understanding,
current availability, or success. Unrecognised semantics and Constraints fail closed, and extra
Shape structure can never broaden authority.

Interaction is a named and versioned reusable Declared Fragment, not an occurrence, superclass, or
Base term. Execution and Event include it, Outcome composes it through Event, and extension
occurrences may include it to compose actor attribution, optional occurrence identity,
correlation, causation, origin, and temporal placement.
Interaction provides no semantic name, target, value, authority, result, effect, truth, delivery,
ordering, replay, persistence, or lifecycle semantics.

Shape is the named, versioned structural contract for values carried at Atlas boundaries. Every
Operation has one complete input Shape and one separate, independently evolving output Shape.
Shape is always an abstract contract, never the value, object, memory, or bytes. An Execution
carries an input value conforming to its Operation's input Shape. A successful Outcome may carry a
`result` value conforming to the output Shape; rejection and failure diagnostics are `details`
with their own Shape. Shape identity is semantic rather than implementation-local. Versions evolve
additively, and breaking change requires a new canonical name.

A Shape fragment is any projection of a Shape. Portable fragment contracts are named, versioned,
and specified as Declared Fragments. An open Shape may accept compatible authored fragments, and
any Shape may explicitly include reusable Declared Fragments such as `Interaction 1`. Explicitly
including the same reusable fragment does not make otherwise unrelated Shapes compatible. A
component accepting an open canonical Shape processes its canonical projection and may ignore
unknown optional fragments without claiming their semantics; a component requiring a fragment
rejects its absence. Shapes and fragments grant no authority.

Event is an immutable, attributable assertion that something happened. Replaying an Event repeats
the assertion, and receiving one grants no authority to react. Outcome is a specialised Event that
terminates one identifiable Execution, activity, or extension-defined occurrence and may carry a
result. Optional `emitted-at` is an attributable Temporal Mark, not authority or global causality.

Authority is represented through Capabilities, within authority domains whose implementations
are responsible for preserving Atlas semantics. Authority originates at genesis — primordial
grants and enumerable, attributable Genesis occurrences — and thereafter only narrows: Delegation
derives authority by adding Constraints, never by rewriting, so that delegation cannot amplify
by construction. Constraints are evaluated when an Execution is presented and fail closed.
Authority defaults to mortal where domains are dynamic.

Operations are scale-agnostic: a register write and an audit initiation share one authority
model, and a large Operation keeps its semantic identity at the boundary where it is executed
regardless of the machinery beneath. When one Actor acts on another's request, a Capability
attributable to the request — or a Capability the responder deliberately presents as its own —
must be evaluated and recorded, never silently replaced by ambient authority.

Flow is a first-party Architectural Extension, not a Base form. A Flow is a continuing
relationship carrying a sequence of Executions, Events, Outcomes, or typed Items under shared
authority and delivery context.
It defines positions, acknowledgement, backpressure, recovery, replay, and termination only as
declared by its contract. Occurrences may compose Interaction for common attribution and context.
Base-only Actors may preserve or route structurally understood occurrences but do not thereby
understand Flow or qualify as endpoints.

Event is Base ontology; Event Distribution is infrastructure. The extension defines publishing,
observation, subscriptions, mediated fan-out and fan-in, filtering, groups, persistence, and
replay. Subscriptions use Flows while preserving the original emitter, origin, and provenance of
every Event.

Provenance answers two questions: the Delegation chain records *by what authority*; origin
classes record *what kind of cause* — asserted only under granted authority, unverified by
default, never surviving Delegation. Interacting with any bounded-capacity participant — human
attention or agent compute — may itself require an admission Capability.

Atlas Base is intentionally small and must satisfy the Embedded Test: a microcontroller without
an operating system, networking, virtual memory, or dynamic allocation implements every Base
requirement with static structure. Base has eight terms: Actor, Capability, Shape, Delegation,
Operation, Execution, Event, and Outcome. Shape satisfies the Embedded Test through compile-time
signatures and static tables while remaining necessary for agreement between independent
implementations. Frequently deployed concepts such as Transaction remain extensions when they are
not necessary to that minimum. A future convenience Profile may collect common extensions for
general-purpose systems without acquiring privileged status or changing Base.

Atlas grows through Architectural Extensions, Profiles, and Domain Vocabularies. Profiles declare
direct dependencies in `uses:` blocks and may use other Profiles; conformance expands through the
transitive dependency closure without repeating indirect requirements. Standard names use
unqualified Concept Paths, which are reserved for ratified Atlas concepts. In authored names, `:`
separates a hierarchical Authority Path from its Concept Path, as in
`Logitech.MX:Input.Scroll.SmartShift`. Names are structurally legible and semantically opaque.
Declaration prefix blocks reduce repetition in documents but expand to canonical names before use.
Ratification freezes a canonical name's semantics forever; additive availability remains governed
by Profiles and discovery.

Beyond Base, a Component is a scale-independent unit of composition declaring the Atlas contracts
it provides and requires. Components and Actors are distinct: Components define composition
boundaries; Actors participate in authority. Bindings may be static or runtime-established. A
Hot-swap Host Component exposes a Hot-swap Slot for a declared Hot-swap Class; a Hot-swappable
Component conforms to that Class and its replacement obligations. Their joint contract must state
compatibility, Actor identity, authority, state, in-progress work, interruption, and rollback
semantics rather than treating live replacement as an automatic consequence of shared names.
Remote services are not a separate Component kind: `local` and `remote` are projections of richer,
attributable selection characteristics such as topology, authority domain, latency, cost, capacity,
availability, and failure domain. These characteristics guide selection among compatible
Components without changing their semantic contract identities. Implementations may still present
the useful `local`/`remote` shorthand in user interfaces and summaries, provided it remains a
declared, lossy projection rather than a source of unstated guarantees.

The proposed Atlas Portable Binding supplies a default general-purpose seam without defining one
implementation model. A scoped Binding Plan fixes contracts, authority, representation, memory and
resource handling, synchronisation, delivery, and lifecycle before the hot path. Its portable
binary direction uses compact framing and schema-guided CBOR for ordinary inline values, while
referenced shaped resources permit pooled, shared, device-local, registered, or otherwise
specialised data paths within the same seam. Static and native bindings may compile the plan into
direct calls or specialised machinery. Mapping between private representations of one Shape belongs
to binding or host machinery; semantic adaptation between different contracts remains an explicit
Component-level relationship rather than a Base mapping service.

Unspecified behaviour is open by presumption; the guaranteed surface is the normative text; the
attested surface is what conformance suites test.

Atlas does not define an operating system.
It defines a common computational architecture upon which firmware, runtimes, devices,
distributed environments, organisational systems, and operating systems may build.

Fabric is the first practical implementation and showcase of Atlas. Linen is the planned second,
independent full-stack implementation and composability test. Interchange of components between
their stacks is intended to reveal where an apparent Atlas contract is actually a private
implementation convention.

Fabric is not Atlas.
Linen is not Atlas.

## 35. Changes from 0.2

This section records the changes introduced since Architecture 0.2.

**Changes in 0.3:**

- **§7 and §13–§14 — Operation and Execution separated.** Operation now names the stable semantic
  contract, with one input Shape and one independent output Shape. Execution is the Base occurrence
  representing one concrete attempt to execute an Operation. Correct prose may say “execute an
  Operation”; the specific run, including any identity and metadata, is an Execution. Event remains
  the immutable assertion form, and Outcome terminates an identifiable Execution or activity.

- **§7, §13, and §16 — Interaction moved from ontology to composition.** Interaction is no longer
  a Base term, occurrence, superclass, or “form.” `Interaction 1` is the first standard reusable
  Declared Fragment. Execution and Event include it explicitly, Outcome composes it through Event,
  and extension occurrences may include it for attribution, optional identity, correlation,
  causation, origin, and temporal placement. The
  Interaction fence excludes semantic name, target, values, authority, result, effect, truth,
  delivery, ordering, replay, persistence, and lifecycle semantics.

- **§10 and §13 — Capability recognition made explicit.** A Capability is not only authority: it
  is a target-recognised grant authorising Operations and transitively binding to their input and
  output Shapes and required Declared Fragments. That closure is not duplicated in the Capability.
  Recognition does not promise holder understanding, availability, or success, and unknown or
  stronger fragments cannot broaden authority.

- **§6.8 and §30 — Linen introduced as an independent full-stack implementation.** Fabric remains
  the first practical showcase. Linen is defined as a lean, independently implemented stack whose
  purpose is to test composability and component interchange with Fabric. Shared conformance is
  necessary but not sufficient: components implementing the same declared contracts should
  interchange without depending on either implementation's private types or conventions.

- **§7 and §16 — Shape added to Atlas Base.** Architecture 0.3 identifies eight Base terms. Shape
  is a named, explicitly versioned abstract structural contract, never the value, object, memory,
  or bytes. It is Base because independent implementations cannot
  share those semantics while disagreeing about value structure. Shape does not prescribe a wire
  encoding, reflection system, object model, allocation strategy, or language type system, and a
  fixed compile-time representation satisfies the Embedded Test.

- **§16.1–§16.2 — Shape identity and additive versioning defined.** Shape references contain a
  canonical Shape name and positive integer version. Same-name versions form a monotonic,
  backward-compatible lineage: later versions may add optional structure but cannot remove,
  rename, retype, require, or reinterpret existing fields. Breaking change requires a new
  canonical Shape name. Coincidental structural similarity does not create compatibility.

- **§13.1 and §16 — Operation inputs, outputs, and Shape fragments defined.** Every Operation
  has one complete input Shape and one separate, independently evolving output Shape; `Unit 1`
  represents either side when it carries no value. A fragment is any subset or projection of a
  complete Shape, but only named, versioned, and specified Declared Fragments carry portable
  contract meaning. Declared Fragments may be authored attachments to an open host Shape or reusable
  structures explicitly included by unrelated Shapes. Inclusion does not make the host Shapes
  compatible.

- **§16.3–§16.4 — Open Shape composition and projection defined.** An open record Shape may carry
  several non-overlapping authored fragments. A component accepting the canonical Shape MUST
  accept a valid composed value, process the canonical projection, and ignore unsupported
  fragments without claiming their semantics. An authored fragment may enrich but never condition,
  constrain, or reinterpret canonical fields; stronger semantics require the fragment explicitly.
  Closed Shapes reject authored fragment attachment where exact structure is part of the contract.

- **§10, §13–§14, §21, §23, and §29 — Shape integrated across existing semantics.** Constraint
  values declare Shapes and fail closed when compatibility cannot be established. Executions carry
  input values conforming to the Operation's input Shape; successful Outcomes may carry `result`
  values conforming to the output Shape; diagnostics use separately shaped `details`. Domain
  Vocabularies declare accepted Shape and Declared Fragment versions. Ratification freezes Shape,
  fragment, and field meanings. Conformance now includes version, fragment composition,
  projection, unknown-fragment, and incompatible-redefinition tests.

- **§7, §18, and §19 — Base membership separated from prevalence.** Base membership depends on
  necessity to the smallest meaningful Atlas system and the Embedded Test, not expected deployment
  frequency. Shape enters Base under that test; Transaction remains an extension despite likely
  broad use. A future convenience Profile may collect a stable general-purpose bundle, but 0.3
  neither defines nor reserves one, and such a Profile has no privileged status.

- **§19 — Generic extension directions refined.** `Workspace`, `State`, and `Transaction` are
  recorded as provisional generic directions. `Structured Data` is rejected as an Architectural
  Extension direction in favour of database-agnostic `State`, Base Shape, `Resource`, and provider
  vocabularies. Presentation and Workspace are application-facing facilities rather than private
  browser or shell machinery.

- **§20 — Profiles compose transitively.** A Profile may require another Profile and declares its
  direct dependencies through the plural `uses:` block. Conformance expands the acyclic dependency
  closure, so Profiles do not repeat inherited extension and vocabulary lists. Interactive
  Application, Web, and Database are recorded as deliberately incomplete work-in-progress Profile
  directions; the Database direction standardises the integration seam, not provider operations.

- **§22.3 — Declaration prefix blocks replace dot-relative notation.** Grouped declarations use
  explicit `within` prefix blocks; every line expands from the block's stated prefix alone, so
  inserting, removing, or reordering lines never changes another line's expansion. The
  dot-relative notation of earlier drafts is rejected and recorded as design history. Prefix
  blocks are document notation only: Capabilities, signatures, hashes, wire representations,
  discovery records, conformance tests, and ratification records use expanded canonical names.

- **§10–§35 — Terminology aligned.** Formal prose consistently uses Operation for the contract,
  Execution for a concrete attempt, Capability for the recognised grant, Constraint for formal
  attenuation, Declared Fragment for portable fragment contracts, and Genesis occurrence where the
  occurrence need not be an Event. `Runtime` replaces the provisional extension name `Execution`,
  avoiding collision with the Base term.

- **Sections from Shape onward renumbered.** The new Shape section is §16. The previous §§16–§34
  become §§17–§35, and all internal references and Contents entries move with them.

Relative to 0.2, Shape and Execution enter Base while Interaction becomes a standard Declared
Fragment rather than a Base term. Architecture 0.3 therefore contains eight Base terms and retains
the two scoping concepts, Authority Domain and Genesis.

## 36. Changes from 0.3

This section records the changes introduced since Architecture 0.3. Section 35 is retained as the
historical diff between Architectures 0.2 and 0.3.

**Changes in 0.4:**

- **§18.1 — Scale-independent Component terminology introduced as work in progress.** A Component
  is a bounded unit of composition declaring provided and required Atlas contracts. It may range
  from a library or device to a service, data centre, or recursively composed environment. A
  Component is distinct from an Actor and gains no authority merely by being loaded or bound.

- **§18.1 — Static binding, runtime binding, replacement, and hot swapping distinguished.** A
  Hot-swap Host Component exposes a Hot-swap Slot for a declared Hot-swap Class; a Hot-swappable
  Component declares conformance to that Class and its candidate-side obligations.
  Hot-swappability is a joint operational claim, not an intrinsic property inferred from shared
  Operations. The Slot and Class contracts must define Actor continuity, authority establishment,
  state handoff, in-progress work, interruption, failure, and rollback semantics. `Plugin` remains
  host-specific terminology rather than the scale-independent architectural category.

- **§18.1 and §33 — Atlas Portable Binding proposed as the default Component seam.** A scoped
  Binding Plan may be established statically or dynamically and records contract, authority,
  representation, resource ownership and lifetime, synchronisation, delivery, and replacement
  decisions. The candidate portable binary realisation uses compact framing and schema-guided CBOR
  for inline shaped values while supporting referenced shaped resources for pooled, shared,
  device-local, registered, or other specialised data paths. Native and direct bindings may lower
  the same plan without materialising a generic object or serialised Interaction on the hot path.
  Base defines Shape and authority semantics but contains no universal mapping engine;
  representation mapping belongs to binding machinery, while semantic adaptation must remain an
  explicit Adapter Component or attributable declared transformation.

- **§5, §18.1, and §32 — Remote services placed inside the Component model.** `Local` and `remote`
  are treated as observer-relative projections of richer selection characteristics, including
  topology, authority-domain boundaries, latency, cost, capacity, availability, and failure domain.
  A Component descriptor may publish such characteristics, but self-description remains an
  attributable claim rather than proof; hosting Actors, guardians, measurements, and attestation
  may provide independent values. Implementations may still use `local` and `remote` as useful
  lossy projections for user interfaces, summaries, and coarse policies, without treating them as
  sources of unstated guarantees. Hot swapping, device replacement, service failover, and
  data-centre cutover deliberately share one binding model without becoming identical concepts.

- **§22 — Authored qualification made explicit.** Unqualified canonical names are reserved for
  ratified Atlas concepts. Portable non-standard concepts use
  `AuthorityPath:ConceptPath` regardless of whether they are compiled statically or loaded
  dynamically. Authorship is distinguished from occurrence Origin, and Capability grant identity
  remains distinct from authored semantic definitions.

- **§16.6 — Enrichment recorded as a work-in-progress design direction.** Enrichment is proposed
  as an addition-only composition mechanism that supplies previously absent Shape fragments from
  information already available to the composition. It may copy, project, reshape, combine, or
  deterministically derive values, but may not replace existing information or hide Capability
  invocation and other operational acquisition.

- **§16.6 — Targeted and ambient Enrichment distinguished.** Targeted Enrichment declares an added
  fragment available at a named consumer, Operation boundary, or bounded scope. Ambient Enrichment
  declares availability within a broader explicit scope. Neither necessarily specifies a physical
  route through intermediate modules. Consumer requirements, shadowing, conflict, confidentiality,
  provenance, and expiry semantics remain deliberately open, and ambient availability is not
  accepted as implicit global context.

- **§16.6 — Global storage separated from ambient availability.** A globally reachable store is
  permitted as an Atlas participant or future Resource and State facility, but reading it remains
  an Operation with explicit authority, failure, latency, consistency, and Outcome semantics. The
  draft distinguishes storage, discoverability, authority, scoped availability, and declared
  consumption. An obtained value may feed an Enrichment without making store access implicit.

- **§16.6 — Enrichment restricted to information, not authority.** Enrichment concerns Shapes and
  Declared Fragments and cannot create, issue, derive, delegate, transfer, bind, broaden, or combine
  Capabilities. Capability representations carried as shaped information grant no authority. A
  possible Capability-binding or authority-provisioning mechanism is recorded as separate future
  authority-composition work subject to holder, recognition, Delegation, Constraint, mortality,
  and explicit-presentation rules.

- **§16.6 — Value propagation introduced for investigation.** Where an explicit bounded route
  exists, a composition may preserve and transport typed fragments across modules that neither
  consume nor semantically expose them. Propagation must not be treated as a universal property of
  Atlas systems. Parameter threading, carrier structures, contextual storage, attached fragments,
  and distributed forwarding remain possible implementation strategies. Branches, joins, Flows,
  dynamic calls, conflicts, and cross-domain transport remain unresolved.

- **§16.6 and §33 — Systems are not necessarily topological.** Atlas does not assume a fixed call
  graph, pipeline, or global topology. The system is primarily a collection of Actors or modules
  exposing Operations; graphs are particular Execution traces, deployment compositions, Flows,
  workflows, dependency views, or diagnostic representations. The draft does not yet select the
  Atlas relationship that should bound propagated information when direct dynamic invocation means
  no single static path exists.

- **§33 — Open questions expanded.** Enrichment, ambient scope, propagation, hidden dependencies,
  global-store boundaries, possible Capability binding, provenance, expiry, confidentiality,
  conflict resolution, and experimental validation across Fabric and Linen are now tracked
  explicitly.

Architecture 0.4 makes no change to the eight Atlas Base terms and does not ratify Enrichment,
value propagation, the Component model, or the proposed Atlas Portable Binding. The additions are
intentionally marked as work in progress so that static, dynamic, embedded, and distributed
composition models can be tested before normative semantics are chosen.
