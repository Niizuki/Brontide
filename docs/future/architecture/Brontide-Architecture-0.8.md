# BRONTIDE

## Architecture 0.8

An Introduction to the Brontide Computational Model

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

**Status:** Complete Draft (document edit complete; implementation evidence pending; not ratified)
**Version:** 0.8 (see §35 for changes from Architecture 0.7; earlier diffs are retained in
Brontide-Architecture-Change-History.md)
**Notation:** NonStrict (§22.4). Normative identities expand to the same canonical form as Strict
notation; explanatory examples may use locally resolvable shorthand.

Implementations and focused experiments state locally which architecture revision they were devised
against. The Reference and Minimal stacks currently target Architecture 0.7; their 0.3
implementation notes record the 0.8 handoff without claiming to implement this draft. Experimental
work designed for 0.8 states that target in its own document. None changes this document's status by
itself.

---

## Contents

1. Introduction
2. One Model Across Different Kinds of System
3. Why Brontide Base Is Small
4. The Core Idea
5. The Maximal Brontide Environment
6. Design Principles
7. Brontide Base
8. Authority Domains
9. Actor
10. Capability
11. Delegation
12. Genesis and Terminus: The Origin and End of Authority
13. Operation, Execution, and Interaction
14. Event and Outcome
15. Origin: Provenance of Effect
16. Shape: Structure Across Implementations
17. A Minimal Brontide System
18. Growing Beyond the Base
19. Architectural Extensions and Recorded Actor Roles
20. Profiles
21. Domain Vocabularies
22. Names, Authorship, Declaration Prefix Blocks, and Notation Strictness
23. Versioning and Ratification
24. Devices, External Systems, and Trust Admission
25. Systems and Macro-Scale Operations
26. Admission: Interacting with Bounded Capacity
27. Brontide and Existing Systems
28. Threat Model
29. Conformance
30. Brontide, Brontide Reference Stack, and Brontide Minimal Stack
31. Related Work
32. Authors' Discussion: The Larger Direction
33. Open Questions
34. Summary
35. Changes from 0.7

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

Brontide begins with a different question:

> *What is the smallest common computational model through which radically different participants
> can cooperate?*

Brontide proposes an actor-centric and capability-based architecture centred on one principle:

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

Brontide does not claim that these participants are equivalent.
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
optional temporal placement where present. Composing it does not make the distinct occurrence
semantics interchangeable.
Replaying an Event repeats an assertion; replaying an Execution may repeat an effect. An Event
carries no authority for an observer to react. An Execution is evaluated using an explicit
Capability.

Values crossing an Brontide boundary are described by Shapes. A Shape is a named, versioned
abstract structural contract that allows independently implemented participants to agree on
complete input and output structures, Event assertions, Outcome results, and Constraint values
without requiring the same language types, object model, or wire encoding. A Shape is never the
value itself. Every Operation declares separate input and output Shapes that evolve independently.

Persistent information is addressed beyond Base through the provisional Corpus model (§18.2).
A Shape defines the structure of a value; a **Corpus** defines the semantic intent, organisation,
compatibility, and lifecycle of an independently addressable body of information composed from
Shapes where applicable. A **Dataset** is one concrete body conforming to a Corpus. One or more
Corpus-defined Store roles bind that Dataset to logical **Stores**, while Components declare the
roles through which they can operate on the Corpus. A Component may author a Corpus, but neither
the Corpus nor its Datasets thereby become part of or exclusively owned by that Component.

Likewise, Brontide does not prescribe the scale of an Operation.

An Operation may represent a register-level hardware action, a device configuration change, a
database migration, an audit request, or another semantically meaningful action performed by a
larger system. One Execution of that Operation may be realised trivially or may involve a
substantial graph of Actors, Delegations, Operations, and further Executions.

Longer-lived exchange is defined by the first-party `Flow` Architectural Extension. A Flow is an
ongoing relationship that orders and governs a sequence of Executions, Events, Outcomes, or
extension-defined Items. Brontide Base does not understand Flow; Flow is expressed entirely in Base
terms.

Authority is represented by Capabilities.
Authority may be restricted.
Where permitted, authority may be delegated.
Authority never appears from nowhere: it is created at known moments (Genesis) and only narrows
as it flows (Delegation).

This common model is intended to make interoperability possible across systems that would
otherwise require separate integration mechanisms.

Brontide is an open specification.

It is not a kernel and it is not an operating system. It defines a computational architecture
that may be implemented by firmware, runtimes, operating systems, distributed environments, or
future systems designed around Brontide directly.

The first implementation of Brontide is named **Brontide Reference Stack**. The second,
independently implemented stack is named **Brontide Minimal Stack**. Their deliberately narrow
interchange evidence tests whether Brontide components can be composable and interchangeable rather
than merely compatible with Brontide Reference Stack's internal model.

Brontide Reference Stack and Brontide Minimal Stack exist to explore and validate Brontide.
Neither implementation is the definition of Brontide.

## 2. One Model Across Different Kinds of System

Brontide is intended to describe systems at radically different scales.

At one extreme, an Brontide implementation may be a small microcontroller.
It may contain several Actors represented by static structures and direct function dispatch.

At another extreme, an Brontide environment may span personal devices, servers, peripherals, remote
infrastructure, human participants, autonomous computational systems, and organisational
services.

These systems are not expected to use the same implementation machinery.
They are expected to preserve the same architectural relationships.

Consider five Brontide relationships and occurrences:

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

Brontide attempts to describe all five without defining one as the normal case and the others as
special adaptations.

This is central to the Brontide model.

> *The implementation context and scale of an Actor or Operation may change. Their participation
> in the authority model does not.*

Brontide therefore aims to provide an architectural meeting point between systems that are currently
integrated through unrelated mechanisms.

## 3. Why Brontide Base Is Small

Brontide was motivated primarily by large and heterogeneous systems.

Distributed personal computing, cooperating devices, remote resources, humans and autonomous
systems participating in common workflows, semantically meaningful system-level operations, and
modular operating systems all contributed to the discussion that produced Brontide.

Brontide Base nevertheless begins with none of these as mandatory features.

This is deliberate.

A useful architecture should not require a workstation merely to describe authority between two
components.

A mouse should be able to implement Brontide.
A pair of headphones should be able to implement Brontide.
A microcontroller with no operating system, no network, no virtual memory, and very little memory
should be capable of participating in the same architectural model.

This requirement is called the **Embedded Test**.

Brontide Base should remain implementable meaningfully on a small microcontroller without requiring:

- virtual memory,
- dynamic memory allocation,
- networking,
- persistent storage,
- multiprocessing,
- pre-emptive scheduling,
- a filesystem,
- or a human-facing interface.

The Embedded Test is not intended to define the ideal Brontide system.
It is a test of architectural necessity.

Every concept in this document has been checked against it. Where a concept is stated as required
(MUST), a static compile-time structure satisfies it. Where a concept requires dynamic machinery,
it is stated as optional (MAY/SHOULD) or deferred to an extension.

A workstation may require identity, discovery, resource selection, distributed communication,
presentation, and execution placement.
A large Brontide environment may treat multiple devices and remote resources as one cooperating
computational environment.
An organisational Brontide environment may expose migration, audit, deployment, approval,
reconciliation, or recovery as explicit semantic Operations, with their authority represented by
Capabilities, rather than leaving both meaning and authority implicit in implementation-specific
APIs.

These facilities are not secondary to the purpose of Brontide. They are among the principal
systems Brontide is intended to enable.

They are excluded from Base only because the Base architecture should contain concepts that
remain fundamental at every scale.

Brontide grows through modular specifications layered on top of that common core.

## 4. The Core Idea

Consider a simple embedded controller.

It contains a temperature reader, a cooling controller, and an emergency handler.
The cooling controller may read the temperature and change the fan speed.
The emergency handler may stop the fan or trigger a safe state.

These parts may all execute within the same firmware image and the same cooperative event loop.
Traditional system boundaries such as processes and users may not exist.

Brontide describes the system through Actors and authority.

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

Brontide does not require the initiating Actor to model these internal steps as part of the original
request.

`Database.Migrate` remains one semantically meaningful Operation. Its Execution at the target
boundary may recursively involve additional Actors, Capabilities, Delegations, Operations,
Executions, Events, and Outcomes.

This is the central distinction in Brontide:

> *Implementation machinery is local to the system. Actor, authority, contract, and occurrence
> boundaries are architectural.*

## 5. The Maximal Brontide Environment

The Embedded Test defines the minimum environment in which Brontide must remain meaningful.
It does not define the upper ambition of Brontide.

A maximal Brontide implementation may expose a computational environment containing many cooperating
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

The semantic identity of the Operation is visible to the Brontide environment.
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

Brontide does not require implementations to hide these differences.
Instead, higher-level Brontide specifications may describe them explicitly and allow systems to
reason about them.

The Composition direction (§18.1) treats topology, latency, cost, capacity, availability, and
related properties as explicit selection characteristics of Components and their bindings. `Local`
and `remote` are useful projections of those characteristics for a particular observer, not the
two architectural kinds of computation.

This larger environment is not a bolt-on use case for Brontide.
It is one of the principal motivations for defining a common Actor, authority, contract, and
occurrence model in the first place.

The purpose of a minimal Base is to allow this environment to be assembled from coherent
architectural parts rather than defining a workstation, network, enterprise workflow engine, or
distributed runtime as the universal foundation.

## 6. Design Principles

### 6.1 Actors are universal participants

An Actor is a participant in the Brontide authority model.

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
depending on specifications outside Brontide Base.
A corporate platform may present one Actor at a defined architectural boundary while internally
containing thousands of services and Executions.

Brontide does not require these participants to be treated as identical.
An implementation may expose relevant characteristics through extensions or policies.

The Base architectural relationship remains common:

> *An Actor participates by presenting explicitly available Capabilities.*

### 6.2 Authority is explicit

An Actor does not gain authority merely because it runs in a particular place.

Execution inside a process, user session, machine, trusted network, or organisational system is
not itself an Brontide grant of authority.

Authority is represented through Capabilities.
An Actor may present only Capabilities available to it.

This does not require every implementation to use sophisticated runtime checks.

A microcontroller may enforce authority through its static structure.
A general-purpose operating system may validate Capabilities dynamically.
A distributed system may use cryptographically verifiable authority.

All may implement the same Brontide semantics.

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
Brontide treats Delegation directly.

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

Brontide considers this constraint fundamental enough to belong in the Base architecture. §11
defines the structural rule that guarantees it.

### 6.5 Actors are not runtime contexts

An Actor is not a synonym for a process, thread, service, task, or machine.

On an embedded device, several Actors may be represented by a single firmware image.
On a larger system, one Actor may be realised by several processes, services, or runtime contexts.
An Actor may persist while the machinery currently realising it is replaced.
A large Actor may represent a stable system boundary while its internal implementation changes
completely.

These behaviours are not required by Brontide Base.
The distinction exists so that Brontide does not inherit the execution model of its first
implementation.

The binding between an Actor and the runtime machinery realising it is owned by the implementation,
and it is a security boundary: when that machinery is replaced, existing
Capabilities held by that Actor must not silently transfer to an unrelated successor.

Brontide does not require that machinery to be dynamically replaceable. If an implementation retains
the same Actor reference while replacing the machinery realising it, the replacement MUST preserve
that Actor's identity, declared Brontide contracts, and authority relationships. If those properties
cannot be preserved, the implementation MUST expose a successor Actor or an explicit rebinding
rather than silently presenting replacement as continuity. State transfer, quiescence, rollback,
and the treatment of in-progress Executions are not supplied by Actor identity; a Component
claiming hot-swappability must declare those semantics separately (§18.1).

### 6.6 Operations are scale-agnostic

Brontide does not distinguish between "small" and "large" Operations at the Base architectural
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
Brontide does not require the internal implementation graph to be exposed at every architectural
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
of all Brontide Executions within the implementation where Brontide applies.

This scale independence is intentional.

Brontide seeks to describe meaningful computational action rather than equating architectural
Operations with machine instructions, function calls, API requests, or individual messages.

### 6.7 Interoperability should follow semantics

Brontide aims to allow independently implemented systems to cooperate through common architectural
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

Brontide does not attempt to eliminate specialised functionality.
It provides a standard semantic space in which common functionality may be represented directly.
Non-standard functionality remains possible and visibly non-standard.

### 6.8 No implementation defines the architecture

Brontide Reference Stack is initially expected to run on existing operating systems, particularly Linux.

A Linux implementation may use processes, namespaces, sockets, file descriptors, cgroups, or
device files. These are valid implementation tools. They are not automatically Brontide concepts.

Likewise, an embedded implementation may use interrupts, static dispatch tables, and direct
function calls. A future operating system may implement Brontide through kernel-native Capabilities
and message endpoints.

Brontide should describe all of these systems without pretending that one implementation is the
natural form of the others.

A concept belongs in Brontide because it is required by the Brontide model, not because Brontide Reference Stack happens
to need it.

Brontide Minimal Stack provides a second guard. It is intended as an independent full-stack implementation rather
than a thin alternative front end over Brontide.Reference. Where Brontide Reference Stack and Brontide Minimal Stack components implement the
same Brontide contracts, they should be interchangeable despite different internal structures. A
concept that exists only because both implementations copied the same accidental design has not
passed this test.

The openness presumption (§29.1) is the enforcement mechanism for this principle: because
unspecified behaviour is never guaranteed, no implementation's accidents — including Brontide Reference Stack's
or Brontide Minimal Stack's —
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

In the recorded Mediation direction (§18.1), the mediating-Actor pattern is an instance of
Arbitration.

### 6.10 Names are structurally legible and semantically opaque

Brontide names have parseable namespace and concept segments. Implementations may preserve, group,
route, display, and discover names using that structure.

No authority, compatibility, or implication relationship follows from lexical ancestry unless an
Brontide specification explicitly defines one. `Motor.Control` does not imply `Motor.Stop` because
of its spelling. A Domain Vocabulary MAY define a Capability template that permits both
Operations, but the relationship comes from the vocabulary, never from the dots.

Likewise, an authored namespace such as `Logitech.MX:` is structurally subordinate to the
`Logitech` namespace, but the syntax alone does not prove that Logitech authorised it. §22
defines canonical names, authored namespaces, verification boundaries, and declaration prefix
blocks. Where a typed member identity is exposed, its member kind is carried by a distinct
member separator (§22.4), never by dot segments.

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

### 6.12 Simple participation, sophisticated composition

Brontide should allow a Component to be simple even when the environment around it is sophisticated.
A small module may declare one Operation, its input and output Shapes, and the authority required at
its boundary without understanding persistence, identity, networking, scheduling, remote execution,
or specialised acceleration.

The surrounding composition may then, under explicit contracts:

- authenticate or otherwise establish participating Actors;
- record and distribute Events and Outcomes;
- persist state or execution history;
- apply declared Enrichments;
- select another compatible implementation;
- batch or relocate suitable work; and
- lower an eligible implementation to a vector, GPU, or other accelerator path.

These facilities do not make the original Component more complex. The composition becomes more
capable around a small declared boundary.

This is progressive sophistication, not automatic equivalence. A Component gains no property merely
because the environment would benefit from it. Purity, determinism, replay safety, batchability,
vectorisability, relocatability, and accelerator compatibility are separate attributable claims or
contracts. A system MUST NOT silently infer them from an ordinary Operation or hide the operational
consequences of using them.

### 6.13 System participation is rewarded, not required

A general-purpose Brontide environment may offer persistence, Event Distribution, identity,
authorisation support, Presentation, Workspace, Web, scheduling, compilation, acceleration,
observability, and other facilities as system-provided Components or Profiles. Their presence does
not make them mandatory for applications.

An application may use its own database, event mechanism, identity system, rendering engine,
scheduler, or other private machinery. It may expose only a narrow Brontide boundary, or no internal
Brontide composition at all, and remain a valid application in the surrounding environment.

> *System-native services provide additional interoperability, composition, and inspection. They
> do not confer basic legitimacy on an application.*

Tools and Profiles must therefore distinguish generic requirements, stronger optional facilities,
and provider-specific dependencies. An application that requires Event semantics, one that benefits
from durable replay, and one that specifically requires a Brontide Reference Stack persistence contract make three
different claims. Flattening them into one dependency would turn convenience into hidden lock-in.

### 6.14 Semantic portability preserves operational truth

An Operation may retain its semantic identity across in-process, cross-process, remote, batched, or
accelerated execution. This does not make those executions operationally identical.

When an implementation crosses a representation, authority-domain, transport, device, or failure
boundary, the selected binding and its observable characteristics must remain inspectable. Relevant
facts include placement, serialisation or representation mapping, retries, batching, copies,
admission, selected provider, and failure domain.

> *Semantic location independence must not become invisible distribution.*

Brontide may make substitution and relocation possible while still allowing a developer to answer
where work ran, what boundary it crossed, which guarantees applied, and why a provider was selected.

### 6.15 Persistent information is independent of Components

Conventional applications often treat their persistent information as an implementation detail
inside the application's installation or private directory. Brontide must permit that compatibility
model, but it should not make application ownership the architectural default.

Where information participates in the Corpus model (§18.2), the persistent body is independently
addressable. Components declare which Corpora they understand and what roles they can perform;
authority determines which concrete Datasets they may actually access. Authorship of a Corpus is
not ownership of every Dataset conforming to it, and uninstalling, replacing, deactivating, or
hot-swapping a Component does not by itself remove those Datasets.

> *Components operate on persistent information; persistent information is not inherently part of
> the Components that operate on it.*

This separation permits several Components to share one Corpus, one Component to use several
Corpora, provider replacement without obligatory export/import, and data lifecycle decisions that
remain distinct from software lifecycle decisions. Opaque and conventionally private information
remain valid where interoperability or system understanding is unavailable or inappropriate.

### 6.16 Compatibility and authority are separate evaluation regimes

Every Execution carries two structures across a boundary: the input value — the payload plane —
and the presented Capability with its Constraint values — the authority plane. The planes cross
the same boundary under opposite regimes, because their failure modes point in opposite
directions. Integration failure is an availability failure, so the payload plane wants
tolerance: additive versioning, canonical projection, accept-and-ignore (§16). Authority
failure is a safety failure, so the authority plane wants intolerance: fail closed, deny on
doubt (§10.1).

A value in payload position informs; it flows covariantly, unknown additional structure is
enrichment, and projection to a known version is compatibility (§16.4). A value in authority
position restricts; it flows contravariantly, unknown structure is where the restriction lives,
and projection is broadening (§10.1). Integration tolerance is applied only in covariant
positions. Every position in which a Shape-described value appears MUST be classifiable as
covariant or contravariant, and extensions MUST declare the classification for positions they
introduce.

The regimes also separate in time. Integration questions are answerable early — at composition
and binding resolution, which record their results once (§18.1). Authority questions are
answerable only late, at presentation (§13.5). A well-composed system therefore discovers
recognition mismatches when bindings resolve; encountering an unevaluatable Constraint at an
authorisation boundary at runtime indicates a composition defect that travelled, not normal
operation. The fail-closed rules remain the backstop, never the integration mechanism. A
Profile MAY operationalise this stance by mandating a recognition catalogue: a stated set of
Constraint types and value-Shape versions that conforming components recognise (§20, §23).

## 7. Brontide Base

Brontide Base defines the smallest common computational model recognised as Brontide.

Architecture 0.8 retains eight Base terms:

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

Brontide Base also recognises two scoping concepts that describe the responsibilities of
implementations rather than adding
participant-facing objects:

```
Authority Domain
Genesis
```

Operation is a semantic contract. Execution and Event are distinct occurrence forms, and Outcome
is a specialised Event. Execution and Event compose the standard Interaction fragment, defined
using Shape (§13.2). Flow is a first-party Architectural Extension expressed through Base terms
(§19.1).

These terms remain provisional. Brontide 0.x is intended to discover whether they are genuinely
fundamental and whether their current boundaries are correct.

Base membership is determined by architectural necessity, not frequency of use. A concept does
not enter Base merely because most workstations, applications, or large systems are expected to
implement it. `Resource`, `State`, `Transaction`, `Persistence`, `Presentation`, and
`Workspace` remain extension directions (§19), however common some of them may become. Every Base
term must remain necessary to the smallest meaningful Brontide system and must survive the Embedded
Test (§3).

Shape enters Base in 0.3 not because it is expected to be common, but because independent
implementations cannot preserve Operation, Event, Outcome, and Constraint contracts while
disagreeing about the structure of their values (§16).

Every working system is composed in the ordinary engineering sense: some choice or construction
places its participants together. That fact alone does not make **Composition** a Base contract.
Base constrains the resulting Actors, authority, Operations, and occurrences regardless of how they
were constructed; it does not require the construction process, component boundaries, candidate
set, or resolver to be visible to every participant. A fixed firmware image may arrive already
composed, and a host may passively integrate a mouse, device, library, or remote endpoint whose
exposed boundary implements Base without implementing Component descriptors or Discovery.

Composition becomes shared architecture when the arrangement itself must be portable and
inspectable: independently authored Components declare requirements, providers are selected,
bindings and Mediation are resolved, and lifecycle or replacement behaviour is exposed. That is a
strong and widely useful interoperability contract, but it remains separable from the smallest
meaningful Brontide interaction. The distinction parallels Transaction (§19): an implementation may
construct atomically without exposing Transaction semantics, and may construct compositionally
without exposing Composition semantics.

Conformance to Brontide always implies conformance to Brontide Base.
For this reason, implementations do not list Base as a separately supported feature.

A system claims:

```
Brontide 0.8
```

and then lists any additional extensions, profiles, and domain vocabularies it supports.

### 7.1 Term status registry

The status of every named Brontide concept is recorded here once, replacing per-section status
disclaimers. The registry is an index; normative force remains with each concept's defining
section or design note.

| Term | Status |
| --- | --- |
| Actor, Capability, Shape, Delegation, Operation, Execution, Event, Outcome | Brontide Base (eight terms) |
| Constraint | Subordinate concept within Capability; Base-normative declaration discipline (§10.1); ninth-term question recorded (§33) |
| Authority Domain, Genesis, Terminus | Scoping concepts; implementation responsibilities, not participant-facing objects |
| Interaction | Standard reusable Declared Fragment (`Interaction 1`); not a Base term, occurrence, or superclass |
| Shape fragment, Declared Fragment | Subordinate concepts within Shape |
| Flow, Event Distribution | First-party Architectural Extension directions; placement defined (§19), conformance unratified |
| Channel, Resource, Composition, Discovery, Runtime, Topology, Authority Topology, Distributed, Identity, Persistence, Realtime, Presentation, Workspace, Intent, State, Transaction, Lifecycle, Time | Provisional extension names (§19); no accepted extensions implied |
| Component, Provider Set, Composition Region, Composition Port, Binding Plan, Portable Binding, Replacement Slot, Hot-swap Slot, Hot-swap Class, Parameter, Attribute, Definition Constraint | Work in progress; Composition design note (§18.1) |
| Mediation, Selection, Distribution, Aggregation, Arbitration, Router, Distributor, Aggregator, Arbiter | Recorded direction; Composition design note (§18.1) |
| Topology Node, Topology Relation | Work in progress; minimum composition-membership direction (§18.1, §19) |
| Guardian, Gatekeeper, Sentinel, Sentinel Watch | Recorded Actor-role family and subordinate watch contract: Guardian protects or represents; Gatekeeper prevents unadmitted protected-boundary crossings; Sentinel performs bounded third-party observation and reporting (§19.3, §26.1) |
| Topology Map, Environment, Protected Environment, Protection Plane, Gatekeeper export fidelity, Environment View | Recorded direction; Topology design note (§19) |
| Enrichment, value propagation | Work in progress; Enrichment design note (§16.6) |
| Corpus, Dataset, Store, Store role, Store Relationship (Mirror, Backup) | Work in progress; Persistent Information design note (§18.2) |
| Dot-relative declarations, Structured Data extension, Remote Service category, `@version` name suffixes | Rejected; design history retained where instructive |

## 8. Authority Domains

An **authority domain** is the scope within which one implementation is responsible for
preserving Brontide authority semantics.

A firmware image is an authority domain.
An operating system instance is an authority domain.
A distributed deployment sharing one trust root may be one authority domain.

Brontide Base defines authority semantics *within* a domain. Cooperation *between* domains — mutual
identification, attestation, and cryptographic representation of authority — is real, important,
and deferred to the `Identity` and `Distributed` extensions. This is deliberate: requiring global
identity in Base would fail the Embedded Test and would repeat the mistake of global naming
schemes, which collapse under their own registration bureaucracy. Identity in Brontide is
domain-relative, as names are in SPKI/SDSI and as capability references are in every capability
operating system.

Each authority domain's implementation is its own trusted computing base (see §28).

The grant machinery itself lives in that trusted computing base — and therefore wherever the
domain boundary lives. There is deliberately no single home; this is §6.8 applied to the most
security-critical machinery. Three responsibilities are separable and may live in different
places within one system. Minting (Genesis, §12) lives in domain initialisation policy: a
compiled authority table, boot-time construction, an attachment policy. Representation and
custody (§10.4) are unprescribed: a static entry, a kernel object, an unforgeable reference, a
cryptographic credential. Evaluation (§10.1, §13.5) is decentralised to targets by design —
Brontide has no central reference monitor as an architectural concept; domain machinery may act on
behalf of targets at their boundaries.

The characteristic homes follow the implementation depths of §27. In firmware, the mechanism is
the image structure itself, with no runtime machinery. In a hosted or native runtime, custody
lives in the runtime's or kernel's trusted computing base — with the Capsicum lesson of §31 for
hosted implementations: the seam with the ambient-authority host is where the model leaks. At
the cross-domain tier, custody dissolves into cryptography, and the four Actor-reference
properties of §9.1 are the portable contract the representation must preserve. In every home,
Capabilities do not travel between trust boundaries; authorisation happens at each boundary.

## 9. Actor

An **Actor** is a participant capable of presenting authority within an Brontide system.

An Actor may:

- hold Capabilities,
- present Capabilities,
- initiate Executions of Operations,
- emit Events,
- receive delegated authority,
- and delegate authority where permitted.

Actor is the common participant abstraction of Brontide.
Its definition deliberately does not depend on implementation nature or scale.

A process may be an Actor.
A peripheral may expose an Actor.
A human may participate as an Actor.
An autonomous system may be an Actor.
A firmware subsystem may be an Actor.
A complete software platform or composite system may expose an Actor at an architectural
boundary.

These statements do not imply that Brontide considers a human, process, mouse, and corporate
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

Brontide Base does not define the representation of Actor references, global Actor identity, Actor
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

Brontide Base does not define Actor persistence, discovery of Actors, or runtime placement.

Brontide Base requires only that Actors participating in an authority relationship can be
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
It represents authority recognised by the Brontide implementation.

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
- or another property defined by an Brontide extension or Domain Vocabulary.

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

Brontide Base does not prescribe a Constraint syntax. It prescribes the Constraint *algebra*:

> Constraints only ever narrow. Effective authority is evaluated when an Execution is presented,
> at the target's authorisation boundary, as the conjunction of all
> Constraints along the derivation chain (§11). A Constraint that the evaluating implementation
> cannot interpret MUST cause denial.

The fail-closed rule is not optional politeness. Without it, every future Constraint type is a
privilege-escalation window against older implementations; with it, older implementations
degrade to *stricter*, never to *wrong* — which also makes Constraint evolution version-safe
(§23).

Where Constraints compose into expressions (recursive Definition Constraints, §18.1), the
fail-closed rule extends structurally through strong three-valued (Kleene) evaluation. An
unrecognised or unevaluatable atom has the value Unknown, and Unknown never resolves at the
atom: `Not(Unknown)` is Unknown; `AllOf` is False if any member is False, True only if all
members are True, and otherwise Unknown; `AnyOf` is True if any member is True, False only if
all members are False, and otherwise Unknown. In authority context an Execution is authorised
only where the expression evaluates True; Unknown and False deny. In selection contexts the
same evaluation applies with a different consequence: a candidate is retained only where the
expression evaluates True, and the resolver SHOULD record every Unknown atom encountered
(§18.1). Evaluation is structural: implementations MUST NOT reason across repeated atoms —
`AnyOf(X, Not(X))` with X Unknown is Unknown, not a tautology. An expression evaluating True
is true under every interpretation of its Unknown atoms, so this rule is exactly as sound as
denying on any unknown, while permitting the authored fallback pattern
`AnyOf(NewConstraint, OldConstraint)` through which Constraint vocabularies migrate across
version skew.

Constraint types and their meanings are defined by Domain Vocabularies and extensions; the
declaration form is defined here. Every Constraint type is introduced by a declaration stating
its canonical name and version, the Shape of its value, its evaluation semantics as a narrowing
predicate over an Execution, its accounting scope where quantified, and any declaration duties
imposed by its vocabulary (§21.1). A domain MAY decline to implement a Constraint type; it MUST
NOT be unable to identify one. Declining is a decision with defined semantics — denial under
the fail-closed rule, attributable to a nameable declaration — while inability to identify a
well-formed declaration is nonconformance. Bookkeeping needed to evaluate rate, capacity, or
liveness MAY change state, but evaluating a Constraint must not itself grant authority or cause
the requested domain effect.

A Constraint quantified over a history of Executions — rate, capacity, count — is accounted at
its occurrence in the derivation chain. Every Capability derived from the Constraint-carrying
ancestor draws on that single budget; Delegation never instantiates a fresh budget. Bookkeeping
state changes only on successful authorisation; a denied Execution consumes nothing. Under this
rule the narrowing algebra extends to quantified authority: the effects available through all
derivations of a Capability together never exceed what that Capability itself permits. Every
quantified Constraint type MUST declare its accounting scope in its declaration. The Base
default, and the only scope Base itself defines, is this *chain-occurrence pooling*. A
vocabulary MAY define a different scope — per-holder, per-target, per-Flow — but MUST then
state explicitly that Delegation multiplies the aggregate budget under that scope, and grantors
SHOULD pair such Constraints with Delegation restrictions or explicit subtree caps. An
evaluator that cannot enforce a declared accounting scope denies under the fail-closed rule.

Every Constraint type that carries a value MUST declare the Shape of that value (§16). A target
that cannot establish Shape compatibility for a presented Constraint value cannot evaluate the
Constraint and therefore denies under the fail-closed rule. Shared Constraint names without
shared value structure do not constitute shared authority semantics.

Constraint evaluation is exempt from additive Shape projection (§16.4, §6.16). A presented
Constraint value carrying structure the evaluator does not recognise — including optional
constituents introduced by a later version of the value's Shape — makes that atom
unevaluatable, and standing alone an unevaluatable atom denies. The projection rule that a
consumer processes the canonical projection and ignores unknown structure applies to Operation,
Event, and Outcome values, never to Constraint values: projecting a value whose purpose is to
narrow authority discards narrowing, and discarded narrowing is broadening. The authority plane
therefore evolves by parallel names and authored fallback rather than additively in place:
changing a Constraint type's evaluation semantics or value Shape requires a new canonical name,
and migration across version skew is written explicitly as
`AnyOf(NewConstraint, OldConstraint)` (§23). A vocabulary that evolves a Constraint value Shape
thereby accepts that older evaluators deny the new form.

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

Liveness-scoped validity conjoins across a derivation chain like every other Constraint:
effective authority exists only while every liveness-scoped link in the chain is live at
authorisation time. Each liveness-scoped Constraint MUST identify the maintained relationship
that scopes it — the lease, session, attachment, or Flow, and its maintaining Actor —
precisely enough that the target's authorisation boundary can evaluate the link. A domain that
permits liveness-scoped Constraints on delegable Capabilities MUST provide targets the means to
evaluate the liveness of every link at the authorisation boundary; a target that cannot
evaluate a liveness-scoped link denies under the fail-closed rule (§10.1). In a domain without
such chain-liveness machinery, a grantor SHOULD attach wall-clock validity rather than liveness
scoping to delegable grants: a Constraint that is unevaluatable by construction at every
reachable target does not create mortal authority; it mints authority that is dead on arrival.

Recommended design stance for dynamic domains:

> *Authority defaults to mortal. Immortality is the explicit exception.*

Concretely: where a domain supports dynamic Delegation, a granting Actor SHOULD attach wall-clock
or liveness-scoped validity to new grants, and an unbounded grant SHOULD be an explicit,
attributable choice rather than an omission. Mortality is the cheap majority of revocation,
available now; full revocation semantics remain an open question (§33).

Withdrawal of authority and cancellation of accepted work are distinct. Withdrawal denies new
Executions and renewals; it does not retroactively undo committed effects, and it is the sole
Base mechanism governing an effect already begun — authorisation itself is evaluated once, at
presentation (§13.5). Every extension or
Domain Vocabulary that defines a continuing relationship or long-running activity MUST state:

- whether withdrawal terminates existing work,
- the maximum revocation horizon,
- any safe checkpoint or commit-point behaviour,
- the terminal Outcome produced,
- and whether compensation is available as a separate authorised Operation.

The common declaration is required even while the exact `Lifecycle` and `Flow` revocation
protocols remain open.

### 10.4 Representation

Brontide Base does not prescribe how Capabilities are represented.

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

Brontide guarantees this structurally rather than by comparison:

> A Delegation derives a Capability that is the delegator's Capability plus zero or more added
> Constraints. A Delegation MUST NOT express authority any other way.

The derived Capability designates a new holder and records its parent. Changing the designated
holder and adding the derivation link are the mechanics of Delegation; they do not rewrite the
Operation set, target, or other effective authority inherited from the parent.

A Capability is delegable unless a Constraint restricts further Delegation. Delegability is not
a separately granted right: a derived Capability's delegability never exceeds its parent's,
because Delegation-restricting Constraints conjoin along the chain like every other Constraint.
A prohibition on Delegation is an attribution boundary rather than an effect boundary — a
willing holder can always act as a deputy (§13.6) — and what the Constraint guarantees is that
every Execution under the authority remains initiated by the named holder, with sharing forced
into visible deputyship or visible Delegation. Every Delegation also implicitly conjoins the
origin-demotion Constraint of §15: a derived Capability asserts at most `Origin.Derived`.

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
An embedded Brontide implementation may define every Delegation relationship at build time — a
static delegation table represents pre-evaluated Constraints.
A distributed system may create and withdraw Delegations at runtime.
Both models are compatible with the architectural concept.

Delegations form a derivation graph: every derived Capability records what it was derived from,
where the implementation preserves provenance. This graph is the structure against which any
future revocation semantics will be defined (revoking a Delegation invalidates its derivation
subtree), which is why revocation can remain open (§33) without contaminating the Base model.

Preserving the derivation graph for audit is optional; establishing effective authority is not.
An implementation MUST be able to evaluate the conjunction of all Constraints along the
derivation chain at the authorisation boundary — by carrying ancestor Constraints in the
Capability representation, by pre-evaluating them into a static table, or by resolving them
through domain machinery. An implementation that evaluates only the presented Capability's own
added Constraints does not conform.

The means chosen is also the domain's revocation ceiling. A representation that inlines
ancestor Constraints cannot be revoked without an indirection point deliberately inserted into
its chain; a pre-evaluated static table is revocable only by rebuilding the table; a resolved
representation revokes naturally at its resolver. A domain MUST record its representation
choice as an operational property, so that future revocation semantics (§33) can state which
representations satisfy them. Revocation-via-indirection (§31) is the recorded candidate
mechanism for carried representations.

Patterns requiring *amplification* — two authorities combining into more than their sum — are
not expressible as Delegation, deliberately. They are modelled as an Actor exposing a new
Capability whose implementation internally uses its own authority (§25). Amplification becomes a
service boundary, not a delegation rule; the delegation calculus stays monotonic and auditable.
Issuance of authority over newly created resources follows the same move (§12).

## 12. Genesis and Terminus: The Origin and End of Authority

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
occurrence is exposed to Brontide participants, it is represented as an Event under §14. Event
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

Authority over a newly created addressable thing is *issued*, not minted. An Operation whose
effect creates a resource conveys authority over that resource by Delegation from the providing
Actor's own authority over its resource space, performed as part of the Operation's effect and
recorded like any other Delegation (§11). Issuance therefore introduces no new authority: the
provider's authority narrows, conservation holds, and every issued Capability's derivation
chain terminates, through the provider, in a primordial grant. Genesis remains reserved for the
domain's own policy moments. A domain MAY realise a creation service as domain machinery, in
which case issuance and Genesis coincide — by stated policy, attributably, never by accident.
Resource creation is therefore always a deputy pattern: the creating provider is a deputy over
its own resource space, and the invocation principle (§13.6) applies to issuance without new
machinery.

**Terminus** is the counterpart of Genesis: the policy occurrence at which an Actor ceases to
participate in the authority model. Terminus occurrences are implementation- or
extension-defined but MUST be enumerable and attributable to the domain's own policy, exactly
as Genesis occurrences are. For Terminus, a domain MUST define: the disposition of Capabilities
the Actor held — extinguished with the holder, since no surviving participant may present them
(§13.5); the disposition of Delegations the Actor granted — which outbound grants survive,
which are extinguished, and on what schedule; and the retirement of the Actor's references
consistently with §9.1, never reused while a surviving authority relationship mentions them.
Liveness-scoped grants die with their maintaining relationship (§10.3). An immortal grant that
survives its grantor MUST remain attributable to the granting Actor's recorded identity at
grant time, so provenance stays reachable (§29.3) even when the grantor is not. Where a
Terminus occurrence is exposed to participants, it is represented as an Event under §14, as
Genesis occurrences are. The embedded case is trivial by the same argument as Genesis: a static
domain has no Terminus occurrences, and its compiled authority table is the complete
disposition policy.

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
responsible Actor made it available at an Brontide boundary. For direct embedded dispatch, the
boundary may be a function call. For a distributed implementation, it may be entry into Brontide
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

Authorisation is instantaneous. Effective authority is evaluated once, when the Execution is
presented (§10.1), and governs only whether the requested effect may begin. Brontide Base
defines no re-evaluation of Constraints against an effect in progress; an effect already begun
is governed by the withdrawal and cancellation semantics of §10.3. Mid-effect re-evaluation,
checkpointed revalidation, and partial-revocation semantics are extension-defined and MUST be
explicitly declared by the extension or Domain Vocabulary that introduces them (§29.1).

An Execution thus carries two structures across the boundary: the input value on the payload
plane and the presented Capability with its Constraint values on the authority plane, evaluated
under opposite regimes (§6.16).

An Execution may be rejected before the effect begins, accepted and fail, or complete
successfully. Execution alone does not provide delivery, idempotency, ordering, cancellation,
rollback, completion, or a particular invocation mechanism. Replaying an Execution may repeat an
effect; an Operation contract itself is reusable rather than replayable.

A minimal embedded Execution is a direct call or static dispatch entry whose initiating Actor,
Capability, Operation, input Shape, and target are fixed or checked by program structure. No
message allocation, runtime identity, scheduler, or dynamic lookup is required. An Execution may
also be realised through inter-process communication, a network message, workflow, distributed
orchestration, hardware instruction sequence, or another mechanism.

Brontide standardises Operation, Execution, Interaction composition, Shape, and authority semantics,
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

Assertion is deliberate language. Brontide records who emitted the Event, its semantic name, and
available provenance. Brontide does not thereby guarantee that the assertion is true.

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

At minimum, Brontide preserves the distinction between:

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

Brontide Base does not define the lifecycle of `Audit-318` or a universal error taxonomy. A future
`Lifecycle` extension and specialised Domain Vocabularies define richer activity states and
Outcomes.

## 15. Origin: Provenance of Effect

Brontide provenance has two components. The Delegation chain answers *by what authority* an Execution
or assertion occurred. The **origin class** answers *what kind of cause* produced the occurrence
or effect — a physical transducer, a human act, an autonomous computation, derived or replayed
data.

The two are orthogonal: a remote-desktop tool may have impeccable authority and still must not
look like a mouse. A consent record is only as strong as the claim that a *human* produced the
approval. A replayed sensor reading with a valid chain is still not a live measurement.

The polarity of this mechanism is its most important property. A scheme in which suspicious
sources must label themselves fails immediately — attackers do not self-label. Brontide inverts the
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
> authority domain, or a Guardian (§19.3) acting as it, vouching directly — as it does at device
> attachment or through a trusted human-input path.

Without this rule, a device could delegate its device-ness to companion software and masquerade
would return one hop downstream, laundered through a valid chain. Origin is genesis attribution
(§12) made portable: droppable along a chain, never gainable. The fail-closed rule (§10.1)
covers implementations that do not recognise an asserted class.

Origin demotion is part of the Delegation algebra, not an exception to it: every Delegation
implicitly conjoins the Constraint `origin-assertion: at most Origin.Derived`, so a derived
Capability's effective authority remains exactly its parent's Capability plus added Constraints
(§11), with the demotion Constraint among them, and demotion is testable through ordinary
Constraint evaluation. Nor does demotion make `Origin.Derived` the common origin of a working
system: the default origin class of an occurrence is unverified, and `Origin.Derived` appears
only where an Actor holds a delegated assertion right and exercises it. The sources for which
origin matters — devices at attachment, trusted human-input paths — hold genesis-grade grants
and continue to assert their own classes. Origin stays informative because it is asserted at
the boundary where it was established and demoted everywhere it is merely relayed.

The class taxonomy — `Origin.Device`, `Origin.Human`, `Origin.Autonomous`, `Origin.Derived` —
and Guardian vouching rules belong to an `Origin` vocabulary, not Base. Domain Vocabularies
consume origin classes; they do not define local markings. At the cross-domain tier, origin
claims become signed assertions, compatible in spirit with content-credential systems (C2PA)
that build this mechanism as a bolt-on because no platform layer offers it.

Precedents confirming the demand: Windows has flagged injected input for two decades
(advisory and spoofable, because it is a flag rather than an authority); financial regulation
mandates algorithmic-order flagging; media provenance is being retrofitted cryptographically.
Three independent partial rebuilds of one missing primitive. Brontide provides it once, generally,
with enforcement instead of etiquette.

In the presence of autonomous Actors, "was a human actually in the loop" becomes a mechanically
checkable property of the provenance record.

## 16. Shape: Structure Across Implementations

A **Shape** is the complete abstract structural contract for one value used at an Brontide boundary.
It has one canonical named and versioned definition and may compose named and versioned Declared
Fragments. Shape describes admissible structure and types; it is never the value, object, memory,
or encoded bytes that conform to that contract.

Operation names alone are insufficient for interoperability. Two implementations may both
recognise `Fan.SetSpeed`, yet remain incompatible if one expects a signed integer and the other
expects an implementation-specific object with different fields. Likewise, a Capability
Constraint cannot be evaluated consistently if the participants disagree about the structure of
the value it constrains.

Shape is therefore part of Brontide Base (since Architecture 0.3).

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
Fragment references that are included, present, or required. Brontide does not require a new combined
name for every such composition.

The version belongs to the Shape and is independent of the Brontide Architecture version, Profile
versions, and transport encoding. Capability instances are grants identified through their
holder, scope, and derivation; Brontide does not assign semantic versions to individual
Capability objects. Shapes are explicitly versioned because their structural contracts evolve.

At minimum, a Base Shape system must be capable of expressing:

- **unit** — no value;
- **scalar** — a value with a canonical scalar Shape, not merely an implementation-local type;
- **record** — named fields, each referring to another Shape;
- **sequence** — zero or more values of one declared Shape;
- **choice** — one value selected from named alternatives; and
- **opaque** — uninterpreted data whose declared Shape and integrity may still be preserved.

Shape references compose recursively. A record Shape may contain fields described by other Shapes;
those Shapes may themselves be records, sequences, choices, or further compositions. A contract
that names one root Shape therefore permits an arbitrarily composed set of Shape definitions
without repeatedly introducing a separate "multiple Shapes" case. Multiplicity of Shape
definitions belongs to Shape composition; multiplicity of conforming values belongs to a sequence
Shape or, in the Corpus model, to the declared Corpus Form (§18.2).

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

This additive lineage is the payload plane's evolution calculus (§6.16). The authority plane
evolves differently: Constraint value Shapes are exempt from projection, and Constraint types
evolve by parallel names with authored fallback (§10.1).

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
remains one Shape. Fragment is a subordinate concept within Shape (§7.1).

Any participant may form an unnamed fragment locally by projecting an arbitrary subset of a
Shape. Such a fragment is useful for implementation, inspection, or explanation, but has no
portable identity, version, authorship, conformance meaning, or right to be required by another
participant. Brontide assigns architectural significance only to **Declared Fragments**: Fragments
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
Capabilities and unknown Capability Constraints continue to fail closed (§10.1). For the same
reason, Constraint values are exempt from projection altogether: a Constraint value carrying
unrecognised structure is unevaluatable rather than projectable, because projecting away
narrowing structure would broaden authority (§10.1, §6.16). Projection is a payload-plane
compatibility rule, never an authority-plane one.

### 16.5 Representation and the Embedded Test

Brontide Base defines Shape semantics, not a schema language, reflection API, object model, memory
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

Independent implementations such as Brontide Reference Stack and Brontide Minimal Stack may use entirely different internal types.
Their components are interoperable only where translation between those types preserves the same
named input and output Shapes, versions, required Declared Fragments, fields, and projection
semantics. This is the cross-implementation pressure that makes Shape fundamental rather than
merely convenient.

### 16.6 Enrichment and value propagation (extracted design direction)

> **Work in progress.** The full design direction is recorded in
> `Brontide-Design-Note-Enrichment-0.1.md`. It is not a normative Brontide mechanism, adds no Base
> term, and defines no conformance requirements.

The settled boundaries, retained here because other sections rely on them:

- An Enrichment adds previously absent information constructed from information already
  available to the composition. It never replaces, removes, constrains, or reinterprets
  information already present.
- Obtaining a value is an Operation. Capability invocation, sensor reads, provider queries,
  and other effectful acquisition must remain visible as ordinary Brontide Operations and must
  not be hidden inside an Enrichment.
- Enrichment concerns information described by Shapes and Declared Fragments. It cannot
  create, issue, derive, delegate, transfer, bind, broaden, or combine Capabilities.
- Targeted Enrichment declares availability at a named consumer, Operation boundary, or
  bounded scope. Ambient Enrichment, if retained, requires an explicit scope and explicit
  consumer declaration; the unsafe case is undeclared global consumption, not global storage.
- Systems are not necessarily topological. Graphs describe particular views — an Execution
  trace, a deployment composition, a Flow — rather than the universal form of an Brontide
  system.

## 17. A Minimal Brontide System

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

This is an Brontide system.

A much larger Brontide system may use radically different machinery and expose Operations,
Executions, and Events of radically different scale while preserving the same Base semantics.

## 18. Growing Beyond the Base

Brontide Base is intentionally insufficient for most complete computing environments.
This is expected.

Brontide grows through three additional forms of specification:

```
Architectural Extensions
Profiles
Domain Vocabularies
```

They solve different problems.

The Base provides a common Actor, authority, Operation, Execution, Event, and Shape model.
Extensions allow larger systems to introduce additional architectural concepts without making
those concepts mandatory for every Brontide implementation.
Profiles define useful interoperability expectations.
Domain Vocabularies allow Actors implemented by independent systems to agree on the semantic
meaning of common Operations, Events, Outcomes, and Shapes.

Together, these mechanisms are intended to allow Brontide to scale from embedded systems to highly
connected computational and organisational environments without treating either extreme as an
exception.

In practice, Profiles — not Base — are expected to be the unit of interoperability that software
targets, as instruction-set profiles are in comparable ecosystems. Base is the shared semantic
core; a Profile is what a developer writes against.

Section 20.1 now records three deliberately incomplete profile directions: a **General-Purpose System
Profile** centred on Composition and managed extensibility, a **Static Embedded Profile** that
requires no Composition, Discovery, loader, or manager claim, and a **Host-Assisted Composable Device
Profile** for recursively composed devices that receive outer discovery assistance while retaining a
sealed bootstrap and ordinarily local admission. These are ordinary Profile directions, not
privileged conformance tiers: none enlarges Base or weakens the Embedded Test, and their names and
dependency sets remain provisional until implementation evidence establishes a stable bundle.

### 18.1 Composition and Components (extracted design direction)

> **Work in progress.** The full composition direction — Component contracts, Parameters,
> Attributes, Definition Constraints, selection characteristics, bindings, staged Component
> management, standardised discovery, dependency preferences, composition generations, activation
> stages and barriers, Regions, Ports, incremental composition, topology membership, Slots, Classes,
> hot swapping, the proposed Brontide Portable Binding,
> representation mapping, and the recorded Mediation direction — is in
> `Brontide-Design-Note-Composition-0.1.md`. None of it enlarges
> Brontide Base.

The settled definitions and invariants, retained here because other sections rely on them:

- A **Component** is a scale-independent, bounded unit of composition that declares the Brontide
  contracts it provides and requires. A Component is not an Actor and is not
  authority-bearing: loading, attaching, or binding a Component grants no authority by
  itself. One Component may realise several Actors; one Actor may be realised by several
  Components; a Component may contain other Components.
- A contract role, a Component definition or realisation, an activated Component occurrence, and
  its Actor endpoints are distinct. A role is not a system-wide singleton: several definitions may
  provide it, and one definition may have several active occurrences. A named system default is a
  binding choice within a scope, not ownership of that role for every consumer.
- A **Provider Set** is the resolved, identity-preserving set of bindings satisfying one requirement
  in one binding scope. Requirements declare minimum and maximum cardinality, with `1..1` as the
  ordinary compatibility default, plus sharing and **distinct** or **mediated exposure**. Distinct
  members remain separately addressable; mediated exposure applies declared Selection,
  Distribution, Aggregation, Arbitration, or domain-specific semantics without erasing member
  identity, provenance, failure domain, or authority. Static membership is generation-fixed;
  runtime membership requires an explicit attachment and detachment contract.
- A **Composition Region** is a recursively nested composition boundary with its own immutable
  resolved and activated generation. A **Composition Port** is a parent-declared boundary through
  which a child Region or Component may attach. The Port declares contracts, cardinality, imports
  and exports, authority ceiling, topology requirements, lifecycle and failure behaviour, rollback,
  and whether it is sealed, activation-open, or runtime-open. Where the Region boundary coincides
  with a Protected Environment boundary (§19), attachment through a Port is a covered crossing and
  must terminate at or be performed through a declared Gatekeeper. The Port owns composition semantics;
  the Gatekeeper is the Guardian through which the protection contract admits the crossing. The Port does
  not become the Gatekeeper.
- **Incremental (per-partes) composition** resolves and activates a complete child generation inside
  a declared Port while its parent Region may remain Active. This is structural composition, not an
  Activation Parameter and not arbitrary mutation of the parent. If the child exceeds the Port's
  envelope or participates in an undeclared cross-boundary cycle, the resolver rejects it or
  proposes a wider parent generation and restart. The mechanism is the same for internal optional
  subsystems, attached devices, downloaded features, and remote participants.
- Every attachment occurrence has a local **Topology Node**. Attributable **Topology Relations**
  associate Components, Actors, resources, Regions, and Ports with that node. Relations such as
  `PartOf`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`, `SharesPowerDomain`, and
  `SharesFailureDomain` remain distinct; topology membership is neither identity, trust, authority,
  nor proof of any other relation. This minimum membership belongs to portable Composition so that
  independently resolved parts do not lose their grouping; richer graph behaviour remains the
  direction of the future `Topology` extension (§19).
- A **Parameter** is a named, Shape-described input to an architectural definition.
  **Composition Parameters** are bound while constructing a composition and may select
  declared architectural structure; **Activation Parameters** are bound at activation, may
  fill declared resource slots, and must not introduce structure absent from the resolved
  composition.
- Component acquisition, selection, composition, and activation are distinct. A selection made
  while one generation is active may recursively expand and resolve the complete next generation,
  including its nested Composition Parameters, without altering the active generation. Activation
  Parameters may be obtained during preflight, but fill only slots already declared by that closed
  structure. Preparation before cutover may make restart swift without turning activation into
  hidden recursive composition.
- Generation activation has two observable phases, **Establishment** and **Release**. Establishment
  has named stages: **Local Initialisation** creates private provisional state and inert endpoint
  descriptions without same-group relationships; **Interconnection** establishes Actors, endpoints,
  resources, Binding Plans, and local authority while ordinary interaction remains gated; optional
  **Relational Initialisation** admits only declared lifecycle Operations against declared peers
  under narrow authority; and **Ready** records successful completion for every required member.
  Release then opens the ordinary-interaction gate and makes the group Active. It is logically
  simultaneous, not a promise that machine instructions execute at the same instant or in a
  specified first-component order.
- A **Component Manager** is provisional lifecycle machinery, not a global architectural service.
  It may consult any number of arbitrarily extensible Component Sources simultaneously. A source
  endpoint is distinct from the package's publisher or authored authority; several sources may
  mirror one publisher and one source may host many. A marketplace or storefront is only a source,
  aggregate, or user experience, never activation authority. Product UI may make local and remote
  sources look like familiar stores through one source-neutral presentation projection, while
  `Component Store` is avoided as an architectural term because Store has the persistent-
  information meaning of §18.2.
- A requirement may declare **Preferred Providers**. For a compatible occupied `1..1` binding or
  Slot, the occupant remains stable unless the user or authorised replacement policy chooses
  otherwise; tooling highlights the preferred alternative and its requester. For each unfilled
  required Provider Set position, the default candidate order is explicit preference,
  publisher-affine implementation, generic implementation of the canonical contract, then any
  other compatible implementation.
  Compatibility, trust, origin, platform, authority, and local policy may exclude or demote any
  candidate. Resolution presents an explainable Proposed Stack with the best candidates preselected,
  Provider Set cardinalities and assignments, activation occurrences, binding scopes, sharing,
  exposure and mediation, retained occupants, alternatives, conflicts, sources, publishers,
  evidence, requested authority, restart scope, and preference provenance. Discovery and preference
  grant no authority (§24).
- Logical Component requirement cycles are permitted when a resolver can close each strongly
  connected group finitely and deterministically. Cyclic post-release interaction does not imply
  startup order. A cyclic Relational Initialisation protocol is also permitted when its lifecycle
  Operations, authority, bounded progress, completion, failure, and rollback are declared. Ordinary
  application interaction before Release remains invalid. Explicit ordered activation groups and
  their partial-release and rollback behaviour are structural and resolved before activation.
- An **Attribute** is a value obtained through a specified Brontide Operation — identified by
  its source Operation, vocabulary version, result Shape, and result path, under ordinary
  Capability evaluation — never a free-floating label. Attribute-constrained bindings are
  resolved exactly once, at composition or activation resolution, recording effective values
  and provenance; a later Attribute change never rebinds — reaction belongs to Routers and
  future lifecycle policy.
- A **Definition Constraint** is a Shape-typed declarative predicate used for selection and
  validation, composing recursively through `AllOf`, `AnyOf`, and `Not`. It selects or
  validates without granting authority; carried by a Capability, it conjoins under the Base
  narrowing algebra (§10.1). Composite expressions evaluate in strong three-valued logic
  (§10.1): an unrecognised atom is Unknown, only a True expression authorises or retains a
  candidate, and resolvers record Unknown atoms in their explanatory records.
- `Local` and `remote` are observer-relative, intentionally lossy projections of richer
  selection characteristics. They must not imply latency, trust, authority-domain, cost,
  capacity, or failure guarantees their projection rule does not define, and Remote Service
  is not a separate Component category.
- Interchangeability is a compatibility relationship between Components; hot-swappability is
  a joint operational claim made by a Host's Slot, its accepted Hot-swap Class, and a
  conforming Component. Live replacement semantics are declared, never inferred from shared
  names.
- Generational replacement may require a declared restart of the containing composition without
  requiring live hot swapping. Where a Host owns an independently activatable boundary, restart
  may be scoped to a device, workspace, session, service group, process, or other composition. The
  scope, authority and state disposition, cutover, failure, and rollback semantics remain explicit;
  an implementation must not silently widen a promised scope.
- The proposed **Brontide Portable Binding** is a first-party default seam for Component
  interchange, not the implementation model of Brontide Base. A **Binding Plan** fixes
  contracts, authority presentation, representation, resource ownership, synchronisation,
  delivery, and lifecycle before the hot path. Representation mapping within one Shape
  contract belongs to binding machinery; semantic adaptation between contracts is an explicit
  Adapter Component.
- **Mediation** is the recorded direction for declared intermediation: a relationship whose
  species — Selection, Distribution, Aggregation, Arbitration — are distinguished by cardinality
  and characteristic obligation, realised by dedicated Components (Router, Distributor,
  Aggregator, Arbiter), host machinery, or static construction, and enforced by authority topology
  rather than discovery.
- Direct `1..1` and deliberately member-addressed distinct bindings require no Mediation. A logical
  endpoint that selects, falls back, load-balances, distributes, aggregates, arbitrates, masks
  membership, or owns topology-wide ordering, backpressure, failure, or recovery MUST declare
  Mediation. The declaration is mandatory even when its simple realisation is erased into static or
  Host machinery. A dedicated mediating Component is preferred when the relationship owns mutable
  membership, shared policy, residue, queues, authority, metering, recovery, or lifecycle.

#### Worked composition timing: embedded mouse and managed Host

`Base` is not a Component installed in a mouse. Base is the contract obeyed by the mouse's exposed
Actors, Operations, Events, and Capabilities. Internally, sensor sampling, button debouncing, pointer
transformation, transport, power, and configuration may be private firmware units linked into a
factory image. The manufacturer composes them in the ordinary sense at design or build time and may
materialise static dispatch and authority tables at boot. Unless those internal boundaries are
exposed for independent replacement or tooling, the mouse makes no Composition or Discovery claim.

The external boundary may nevertheless expose `MousePointer` and `MouseConfiguration` Actors. At
attachment, a General-Purpose Host performs a second composition: it evaluates device claims and
local adapter metadata, admits functions separately, creates Host-domain Actor endpoints, constructs
the Binding Plans, and grants only locally authorised Capabilities (§24.2). The Host may represent
the physical endpoint as one Component exposing several Actors or as several functional Components;
physical enclosure does not decide the Component boundary. It does establish a useful, attributable
topology observation: pointer, button, configuration, battery, and other admitted functions from one
attachment can be related to one local Topology Node rather than mixed with compatible functions of
another mouse. With several pointing devices, a declared Aggregator may present one logical input
Flow while preserving both nodes and every member's identity.

A more capable device may instead claim the provisional Host-Assisted Composable Device Profile. It
is best understood as a small external computer, even when packaged as a mouse. It first boots a
sealed, recoverable composition containing the minimum local Host, plan verification and admission
policy, Channel and loading machinery. An outer Host may then provide Discovery, candidate artifacts,
and evidence through authorised Operations. Device-local policy ordinarily decides what becomes the
device's internal child generation; an outer-Host-owned admission mode is separate and explicit. The
internal generation completes Establishment and Release before the outer system activates the
boundary exported by the device. This recursive order keeps an unfinished internal system from
appearing ready merely because its transport is attached.

A managed workstation, service node, or operational system composes at still more times. Image
construction establishes the minimal Host, authority roots, management and recovery path;
installation or user selection resolves a pending generation while the current one runs; preflight
prepares its complete Binding Plans and authority requests; a scoped activation establishes and
releases it; and later device, service, or internal feature attachment may resolve a child generation
through a declared runtime-open Composition Port or stage a wider parent generation. Components may
provide presentation, Workspace, input,
display, persistence, identity, policy, audit, recovery, management, and applications. None is Base;
each realises Actors whose interactions obey Base.

The full paired example, including factory, attachment, and managed-system timelines, is retained in
`Brontide-Design-Note-Composition-0.1.md`.

### 18.2 Corpus, Dataset, Store, and Router (extracted design direction)

> **Work in progress.** The full persistent-information direction — Corpus kinds and Forms,
> the information-integration ladder, Dataset lifecycle, Component-Corpus roles, Store roles
> and absence behaviour, Stores, Store Relationships, and Router — is in
> `Brontide-Design-Note-Persistent-Information-0.1.md`. None of it enlarges Brontide Base, ratifies
> a Storage vocabulary, requires a database, or makes Structured integration mandatory.

The model separates five questions:

```
Shape       What is the structure of one value?
Corpus      What does an independently addressable body of information mean?
Dataset     Which concrete body of information is this?
Store role  What placement and lifecycle purpose does one part of that Dataset have?
Store       Which concrete logical resource retains that part?
```

The settled invariants, retained here because other sections rely on them:

- A **Corpus** is an authored, versioned definition of a meaningful, independently
  addressable body of information; a **Dataset** is one concrete body conforming to one
  Corpus version. Corpus authorship is not Dataset ownership, and the relationships are
  many-to-many.
- Components declare Corpus roles as compatibility claims; Capabilities govern what Actors
  may actually do to Datasets and Stores. Neither declaration nor binding grants authority.
- Each Corpus-defined Store role of a Dataset binds to exactly one logical Store endpoint;
  several roles may share a Store; every optional role declares one explicit absence
  behaviour (`UseRole`, `Discard`, `Recompute`, or `DisableFeature`).
- A **Router** presents a Store-compatible contract while delegating to backing Stores under
  explicit rules. Its endpoint Attributes are its own declared guarantees, not those of any
  current backing Store. Within the recorded Mediation direction (§18.1), the Router is the
  storage instantiation of Selection. Mirror and Backup remain declarative Store
  Relationships.
- Dataset identity is a property of the Dataset record itself, independent of any single
  Store role's content; a Corpus declares which Store roles are identity-bearing. Capability
  designation of Datasets follows §10.2, and Dataset creation authority follows the issuance
  rule (§12): the managing provider derives the creator's Capability from its own authority.
- A Corpus MUST declare its concurrent-access semantics. The declaration may be modest —
  single-writer with enforcement left to authority, or external coordination required — but it
  may not be absent.
- Removing software and removing information are independent decisions: uninstalling a
  Component does not imply deletion of Durable Datasets on which it operated.

### 18.3 Optional system-native services and boxed compositions

A general-purpose Brontide operating environment may participate much more deeply in application
composition than a conventional operating system. Instead of supplying only processes, files,
sockets, windows, users, and devices, it may offer replaceable Components implementing common
Profiles, Extensions, and Domain Vocabularies.

Candidate system-native facilities include:

- Event Distribution, history, replay, and observability;
- Corpus, Dataset, Store, State, Persistence, Resource, Transaction, and database integration;
- Identity, authorisation support, credentials, and policy;
- Presentation, Workspace, input, and document or Web engines;
- scheduling, remote execution, admission, and resource selection; and
- compilation, batching, vector execution, and specialised accelerators.

"System-native" means that the environment can discover, select, govern, and compose the facility.
It does not mean one globally privileged implementation, a mandatory kernel service, or a facility
that every application must consume. Several providers may implement the same declared contract;
an application may constrain selection, provide its own implementation, or decline the facility.

The relationship is reciprocal. An application may consume system-provided persistence or identity
while contributing its own semantic Operations to the surrounding environment. An image editor may
use Event Distribution and expose `Image.Edit`; a browser composition may use system identity while
exposing document rendering; a simulation may use system scheduling while exposing model execution
to tools. The boundary between application and system becomes a declared matter of composition and
policy rather than a fixed caste boundary.

Brontide also permits a **boxed composition**: an application whose database, identity, event system,
renderer, scheduler, and internal modules remain private. The box may expose a narrow set of Brontide
Operations at its edge or participate only as a conventionally hosted program. Its private interior
need not be decomposed, inspectable, or replaceable for the application to be valid.

Native participation should therefore be incremental. Adopting system Events should not require
adopting system persistence; adopting Presentation should not require system identity; and using a
system-selected accelerator should not force the application's database into the same composition.
Tools may provide richer inspection and recovery where declared contracts make them possible, but
must not misrepresent opaque private mechanisms as non-conforming merely because they remain
private.

To preserve honest portability, dependency strength must be visible. A Component may require a
generic contract, require a stronger Profile such as durable Event replay, prefer a system-provided
implementation, or require a specific authored provider contract. Selection and packaging must not
collapse these claims into an undifferentiated "uses the system service" flag.

## 19. Architectural Extensions and Recorded Actor Roles

An **Architectural Extension** adds a generic computational concept to Brontide.

Possible future extensions include:

```
Channel
Resource
Composition
Discovery
Runtime
Topology
Authority Topology
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

These names are provisional and do not imply accepted Brontide extensions.

`Authority Topology` names the analysis direction recorded in §28: computing reachable
authority over the Delegation graph plus declared deputy surfaces. `Discovery` includes the
open holder-introspection question — whether an Actor may enumerate the Capabilities it holds
(§33).

An extension may depend on another extension.
For example, a future `Distributed` extension might require communication semantics defined by
`Channel`. A future `Lifecycle` extension might describe long-running Executions and persistent
activities initiated by them.

**Channel direction.** `Channel` is the first extension of the evidence cycle: the request and
Outcome representation, correlation, error propagation, and delivery semantics the invocation
principle (§13.6) needs and Base withholds. Rather than being drafted abstractly it is extracted
from the retained Cooling and Catalog interchange proofs and recorded in
[Channel](../channel/Brontide-Design-Note-Channel-0.1.md). The recorded frame is framed one-message-per-frame
exchange over a duplex transport; a versioned, kind-discriminated envelope in the categories
negotiation, request, outcome, protocol-error, and lifecycle; request-to-Outcome correlation by
echoed identities kept distinct from host-native Execution identity; a boundary-relative authority
presentation under which no Capability crosses a trust boundary (§8, §24); and a strict separation
of denial, semantic failed Outcome, and protocol or process failure, with no foreign exception or
runtime type crossing. Delivery, ordering, and retry are promised by no one. Channel fixes
semantics, not a wire format, adds no Base term, and precedes the Portable Component Binding
(§18.1), which becomes its first conforming realisation.

**Composition and Discovery direction.** A future first-party `Composition` extension should ratify
the portable structure of composition: Component contracts and requirements, Parameters and
Constraints, Provider Sets, Composition Regions and Ports, direct and mediated bindings, minimum
topology membership, Binding Plans, resolved generations, Establishment and Release, scoped restart,
incremental composition, and optional replacement. It must support authored static resolution,
startup-time dynamic resolution, and bounded runtime child resolution. Claiming the extension means
that this structure is an observable interoperability contract; it does not require every Port to be
runtime-open or require a runtime resolver, loader, package manager, dynamic allocation, or online
source.

`Discovery` should remain a separate optional extension used by Composition resolvers when the
available participants or providers are not already fixed. It may define authorised queries,
candidate and evidence records, Component Sources, runtime participant discovery, and related
introspection. A resolver may instead consume a closed authored candidate set and implement
Composition without Discovery. Discovery results remain inert: they neither select a candidate,
establish a binding, nor grant authority.

**Topology direction.** Portable Composition needs a minimum membership floor even when the complete
topology is private or statically authored: each attachment occurrence has a local Topology Node, and
attributable Topology Relations keep its independently represented functions, Components, Actors,
resources, Regions, and Ports associated. A device may propose these relations, but the receiving
Host records their source and may refine or reject them. An Environment is a grouping over that
floor: its direct members are Topology Nodes, and a Topology Map's relation vocabulary begins with
the floor's Relations. The broader `Topology` extension should
standardise richer nested, physical, logical, hosting, power, connectivity, and failure-domain graphs
and their observations. Neither the minimum floor nor the extension grants authority, proves
identity, or permits `SamePhysicalAssembly`, `HostedBy`, and `SharesFailureDomain` to collapse into
one vague `same device` relation. User interfaces may derive such a label as a declared projection.

The broader direction is recorded in
[Topology Environments and the Guardian Family](../topology/Brontide-Design-Note-Topology-0.1.md). A **Topology Map** is an
observer-scoped, versioned graph. An ordinary **Environment** is an identity-bearing physical,
virtual, mixed, overlapping, or reconstituted grouping with no security implication. Ordinary
Environments have no Gatekeepers merely because an Actor describes, routes into, or exposes functionality
from them. A **Guardian** is an Actor explicitly entrusted to protect or represent a participant,
resource, or bounded interaction through authority it actually holds. A **Protected Environment**
adds an enforced boundary: within one **Protection Plane**, Protected Environments are disjoint or
nested, never partially overlapping; independent Planes may intersect.

Every covered crossing of a Protected Environment passes through a **Gatekeeper**, the specialised
Guardian designated by its protection contract as an allowed boundary participant. Every Gatekeeper is a
Guardian and therefore an Actor; it is not a Component, Port, projection, or parallel authority
principal. Not every Guardian is a Gatekeeper. A Protected Environment with no active Gatekeeper has no declared
external communication and does not interact externally until a Gatekeeper becomes active.

A Gatekeeper exposes relationship-specific contracts and an audience-specific **Environment View**.
Protected interiors are architecturally opaque except for what their Gatekeepers expose; transparency
remains a multidimensional Gatekeeper property and never grants authority. A **Sentinel** is the
bounded observational Guardian specialisation defined in §19.3: its primary function within a
Sentinel Watch is third-party observation and reporting, without acquiring admission or response
authority from that role.
Environment, the Guardian family, Topology Map, Environment View, and the protection terms belong to
recorded extension direction rather than Base. Their descriptor and protocol forms remain
unratified.

A boundary is not a licence to reinterpret. Every contract a Gatekeeper exports declares one **export
fidelity** — Direct, Deputised, Mediated, Adapted, or Synthetic — so reinterpretation is never
presented as exposure. A Direct Gatekeeper is also the interior provider Actor; Deputised, Mediated,
Adapted, and Synthetic Gatekeepers remain distinct Actors whose held Capabilities and provenance are
explicit. Mediated exports obey the ordinary Mediation rules without erasing member identity,
provenance, failure domain, or authority; Adapted exports name their Adapter realisation and never
reuse an interior contract's name with changed semantics; Synthetic exports are boundary-authored
contracts backed by enumerated standing Capabilities held by the Gatekeeper. Fidelity therefore correlates
with the authority a Gatekeeper concentrates, and high fidelity remains the ordinary cheap choice.

A Protection Plane is identified by its protection dimension and enforcement basis — same-basis
Protected Environments share one Plane, so laminarity cannot be escaped by minting Planes. No-bypass
coverage belongs to the Protected Environment's complete protection contract, not to an individual
Gatekeeper: it is declared, enumerated, or statically verified against the resolved generation and always
names its exclusions. Attestation is orthogonal assurance evidence rather than a higher coverage
level. Protection holds fail-closed throughout Establishment. A Component-realised Gatekeeper admits
nothing before its realising Actor is Ready and released; statically or Host-realised Gatekeepers follow
the equivalent declared readiness point. Establishment failure never opens an undeclared crossing.
Export continuity across replacement of a Gatekeeper's backing is declared by its lifecycle contract; a
scoped restart behind a Gatekeeper never silently becomes identity continuity.

Environment identity must nevertheless be portable at protected boundaries. A Gatekeeper may present its
Protected Environment reference, Topology contract version and Profile closure, boundary surface,
continuity claims, and attributable evidence. The peer explicitly reports whether it understood,
partially understood, rejected, or does not support that contract and, on acceptance, records an
attributable alias relation to its own local Environment reference; aliases relate occurrences and
never merge them. Connection, silence, or successful decoding never demonstrates
understanding. Understanding remains distinct from recognition, trust, and locally granted authority.
A Profile may require a successful exchange before richer interaction; a Base-only peer may instead
treat the Gatekeeper as an opaque Actor boundary. This guarantees shared semantics where requested without
adding Environment to Base or transferring a Host's Topology obligations to a passive leaf.
Continuity is sequenced the same way: within one domain, reconstitution continuity is declared by
the Gatekeeper's lifecycle contract under §9.1's reference rules, while cross-domain continuity rests on
receiver-owned pairing — the receiving domain mints its own durable local identity at first
admission — until attested federation exists.

A **Component Manager** is a facility built over these contracts, not another Base term and not a
mandatory realisation of the Composition extension. A build tool can resolve a static image; Host
machinery can resolve an activation generation; one or more Components can provide management
Operations. A future distribution specification may define portable Component descriptors,
packages, sources, evidence, and transactional staging without making a marketplace or machine-wide
package manager part of Base. Runtime binding and hot swapping may additionally compose with
`Lifecycle`, `State`, or `Transaction`, but must not acquire their guarantees merely by using the
word Component. A future `Persistence`, `Resource`, or dedicated specification may ratify the
Corpus, Dataset, Store, and Router direction in §18.2.

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
without implying a storage engine, query language, data model, or durability. It remains distinct
from a Corpus, which defines the semantic intent and lifecycle of independently addressable
information (§18.2). `Transaction` may
describe a declared commit and atomicity relationship over participating effects, but must state
its isolation, durability, failure, withdrawal, and compensation semantics rather than implying
universal rollback. `Structured Data` is not retained as an Architectural Extension direction;
the provisional Corpus model uses Base Shape and explicit Forms without imposing a universal
database model, while database-specific structure belongs in Profiles and Domain Vocabularies.
Observable-condition semantics remain in `State`, and created-resource semantics remain in
`Resource` and related extensions.

Shape and Transaction illustrate opposite results under the same Base criterion. Shape is Base
because independent implementations must agree on value structure to share Operation, Execution,
Event, and Constraint semantics, even when that agreement is realised entirely by static
construction (§16.5). Transaction remains an extension despite likely broad use because an
Execution may already be implemented atomically without exposing cross-effect transaction
semantics.

Likewise, an Execution may be implemented atomically without Brontide observing a Transaction.
The `Transaction` extension becomes relevant when commit, isolation, participation, or atomicity
semantics are exposed across multiple visible effects, Operations, Actors, or Resources. Including
the name in Base without those guarantees would be empty; requiring the guarantees universally
would burden systems that do not need them.

This distinction prevents Brontide itself from becoming a specification of every device and
industry.

This architecture identifies `Flow` and `Event Distribution` as first-party extension directions
(first recorded in 0.3) and defines their architectural placement. Their complete conformance contracts remain to be
ratified.

### 19.1 Flow

A **Flow** is an extension-defined, continuing relationship that carries a sequence of
Executions, Events, Outcomes, or typed Items under shared authority and delivery context.

Flow is not an Brontide Base term or Base occurrence form.

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

Brontide SHOULD standardise at-most-once and at-least-once delivery, stable Item identities,
deduplication support, and idempotency keys. It must not promise universal exactly-once effects;
those require cooperation from the receiving domain or transaction system.

#### 19.1.3 Programmer-facing use

Flow is an application-facing Brontide facility, not machinery reserved for Brontide Reference Stack or system
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

### 19.3 The Guardian family

The **Guardian family** is a recorded family of Actor roles concerned with protection,
representation, and assurance. It does not introduce a new participant kind, authority primitive,
or ownership relation. Every member is an Actor, and every action it performs remains governed by
ordinary Capabilities, Delegations, Operations, Events, and Outcomes. Designating an Actor as a
Guardian records responsibility; it grants no authority by itself.

The family has one general role and two specialisations:

- A **Guardian** is an Actor explicitly entrusted to protect or represent a participant, resource,
  or bounded interaction. A Guardian may arbitrate access, act for its subject, or coordinate a
  response only through authority it actually holds.
- A **Gatekeeper** is the preventative specialisation: a Guardian designated by a Protected
  Environment's protection contract as an allowed boundary participant. It admits or denies covered
  crossings. Only Protected Environments have Gatekeepers, and every Gatekeeper is a Guardian.
- A **Sentinel** is the observational specialisation: a Guardian whose primary function within a
  declared **Sentinel Watch** is to observe and report third-party activity. It watches occurrences
  in which it is not an operational participant except through observation, recording, reporting,
  or adjacent alerting.

Gatekeeper and Sentinel are independent specialisations. A Gatekeeper need not inspect activity
beyond what admission requires, and a Sentinel need not sit at a boundary. One Actor may hold both
roles, but for a particular occurrence the roles remain distinguishable. Observing an admission
decision made by another Gatekeeper is Sentinel behaviour; making the decision is Gatekeeper
behaviour.

Sentinel is a semantic role rather than merely a declared label. An Actor acts as a Sentinel for a
Watch when its primary responsibility in that relationship is observation and reporting of
occurrences belonging to other Actors or subjects. This remains true if an implementation calls it
a monitor, audit recorder, or alerting service. Conversely, an Actor does not become a Sentinel merely
because it observes its own work, receives information for an interaction in which it participates,
or holds an observation Capability incidental to another primary function.
It belongs to the Guardian family because the Watch entrusts it to represent observed activity to
declared audiences, not because every Watch must have a security purpose.

`Primary` is assessed per Watch, not by measuring the proportion of an Actor's code or lifetime. The
Watch's required inputs are observation deliveries or observation-query results, and its required
outputs are records, findings, summaries, or alerts. The Sentinel may participate in the observation
and reporting interactions that obtain and convey those records, but it is not the requester,
provider, target, mediator, Gatekeeper, or authority user whose covered activity is being observed.
If the same Actor holds one of those positions, that participation is another role for that
occurrence.

Subscribing to an Event stream or opening or receiving a Flow is neither necessary nor sufficient
to create a Sentinel Watch. An Actor may consume Events or Flows for business processing,
presentation, transformation, orchestration, control, or its own direct interaction without acting
as a Sentinel. The same subscription becomes Sentinel input only when a Watch assigns it the
purpose of third-party observation and reporting. Conversely, static instrumentation, authorised
queries, callbacks, or other observation contracts may realise a Watch without Event Distribution
or Flow.

The non-participation rule applies to the **watched occurrence**, not to operation of the Watch. A
Sentinel may use ordinary Capabilities to persist findings, append audit records, load rules or
models, checkpoint progress, correlate data, manage retention, report health, and deliver alerts.
Where a Component supplies such facilities, the Sentinel invokes Operations exposed by Actors
realised through that Component; the Component itself does not become authority-bearing. These
supporting Executions remain attributable and bounded by the Watch. They do not make the Sentinel a
participant in the activity it observes, but any mitigation or domain effect beyond operating and
reporting the Watch remains a separate responsibility.

A **Sentinel Watch** is the bounded contract under which the role applies. A portable Watch must
declare at least:

- a purpose identity and parameters stating why the observations are collected and interpreted;
- the watch universe: explicit subject references, a bounded set or query, or a membership rule and
  resolution scope;
- the occurrence classes and inclusion predicate, such as Events, Executions, Outcomes, or the
  presentation, evaluation, Delegation, revocation, or use of Capabilities;
- observation sources, required observation Capabilities, topology and authority-domain boundaries,
  exclusions, claimed coverage, and how missing or unknown sources are represented;
- start, end, lease, lifecycle, revision, and continuity rules;
- the evaluator contract and version, including whether interpretation is deterministic,
  probabilistic, heuristic, or model-based and how confidence and supporting evidence are reported;
- finding, record, summary, and alert outputs together with their audiences, disclosure, retention,
  redaction, and provenance rules; and
- backpressure, observation loss, gap reporting, failure, and withdrawal behaviour.

Purpose is a normative boundary, not a descriptive tag. It identifies the concern and participant,
resource, or interaction the Sentinel represents, and constrains legitimate interpretation,
retention, disclosure, and reporting. `Observe everything` states breadth, not purpose, and is not
sufficient by itself. Purpose grants no observation authority; the Watch must still name the
Capabilities and sources that make observation possible. Because Brontide cannot constrain all
re-propagation after authorised delivery (§28), the purpose boundary makes misuse nonconforming and
attributable rather than claiming to make betrayal impossible.

The Watch boundary is deterministic even when interpretation is not. Given the declared facts about
a candidate occurrence, an implementation must be able to classify it as in scope, out of scope, or
unresolved. A model may then interpret in-scope observations according to the Watch's declared
purpose and evaluator contract. It may not silently widen the universe, sources, use, retention, or
audience. `Every Event`, `every Capability evaluation`, or `every action of Actors A through N` is
valid only when the source set, occurrence vocabulary, actor membership, time interval, exclusions,
and gap semantics make the quantified universe explicit. `Every` means every occurrence in that
Watch universe, never every physically possible occurrence.

For example, `every Event used to assess availability of Environment E`, `every Capability
evaluation used to audit least-privilege policy in Authority Domain D`, and `every Execution by
Actors A through N used to verify a consent rule` can each define a broad but bounded Watch. The
purpose, subjects, occurrence class, sources, lifetime, exclusions, and gap semantics make each
claim narrower than omniscience while leaving its evaluator appropriate to the domain.

A finding is an attributable claim or ordinary Event or Outcome; it is not proof, a Capability, or
an instruction that another Actor must obey. Reporting may include recording, correlation,
summarisation, and alerting designated audiences. Those outputs still require ordinary authority but
remain Sentinel behaviour. Mitigation or control is separate.

The Sentinel designation carries no implicit response authority. Blocking, quarantining, revoking,
reconfiguring, or initiating another effect requires a separately held Capability and an ordinary
Execution under a non-Sentinel responsibility. When the same Actor controls a covered Protected
Environment crossing, it is acting as a Gatekeeper for that crossing, not gaining preventative
power from being a Sentinel. An exterior Sentinel receives no declared architectural view of a
Protected Environment's interior except through a permitted Gatekeeper View or contract.
Side-channel observations remain outside that claim unless the protection contract explicitly
covers them; surveillance never creates a bypass.

Sentinel is universal in subject applicability and semantic classification, not in visibility. Any
Actor whose primary function in a bounded Watch is third-party observation and reporting is acting
as a Sentinel, whether its purpose is security, safety, health, integrity, availability, compliance,
performance, audit, or another domain concern. A Watch may deliberately cover all declared sources
and occurrence classes in an authority domain, but that broad authority and concentration remain
visible costs of the particular design—the architecture neither forbids such a `god` Sentinel nor
pretends it can see beyond its declared sources and evidence.

The [retained reasoning](../topology/Brontide-Design-Note-Topology-0.1.md#retained-objection-and-resolution-sentinel-as-a-universal-observer)
records why generic observation authority alone is insufficient to define the role and why bounded
third-party purpose supplies the missing semantics. Concrete Watch Shapes, query forms, coverage
evidence, evaluator contracts, and conformance vectors remain future `Topology`, observability, or
assurance specification work.

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

Profiles describe useful combinations of Brontide behaviour without forcing all Brontide
implementations into a device or system hierarchy.

A mouse and a corporate operations platform may both implement Brontide while making very different
interoperability claims.

A Profile names the exact minimum versions of its direct dependencies (§23). Its expanded
transitive dependency closure makes the conformance claim complete and reproducible without
requiring every Profile to restate requirements already carried by another Profile.

A Profile MAY additionally mandate a recognition catalogue: a stated set of Constraint types
and value-Shape versions that conforming implementations recognise (§6.16, §10.1). Recognition
duties are Profile business, not Base mandates — a Base-level implementation obligation for
every future Constraint type would fail the Embedded Test (§3), while fail-closed evaluation
already ensures that non-recognition degrades to stricter, never to wrong.

### 20.1 Work-in-progress Profile directions

The following Profile directions are intentionally incomplete. Recording them establishes the
architectural target and tests whether Brontide provides sufficient primitives; it does not ratify
their final dependency sets or Domain Vocabularies.

A **General-Purpose System Profile** should make composition itself a supported user- and tool-facing
contract rather than an invisible deployment detail. Its rough direction is:

```
General-Purpose System Profile uses:
    Composition
    Discovery

Behaviour:
    an inspectable Component-management facility
    static and dynamic resolution into declared generations and Binding Plans
    local admission and authority establishment remain separate from discovery
    Components may be selected from extensible local or remote sources
```

`Discovery` is included in this candidate dependency set because user-added sources, passive device
integration, and changing environments are expected general-purpose behaviours. Evidence may later
split a fixed-catalogue General-Purpose profile from a stronger open-discovery profile. Composition
itself remains the required centre; Component management may be realised by Host machinery, tools,
or Components rather than one mandatory global service.

A **Static Embedded Profile** should demonstrate the opposite valid boundary:

```
Static Embedded Profile:
    permits a completely authored and pre-resolved structure
    requires no Composition or Discovery conformance claim
    requires no Component Manager, loader, dynamic allocation, or source protocol
    permits passive integration into a larger composing Host
```

The profile does not deny that the firmware was composed; it states that the construction is not an
exposed portable contract of that participant. A leaf conforming only to Base or the Static Embedded
Profile may therefore participate inside a General-Purpose System whose Host claims Composition and
Discovery. Extension conformance is asymmetric across that boundary rather than inherited by every
contained device or Component.

A **Host-Assisted Composable Device Profile** should cover devices whose internal structure is
portable and changeable but whose own discovery catalogue, storage, or user interface is deliberately
small. Its rough direction is:

```
Host-Assisted Composable Device Profile uses:
    Composition
    Discovery
    Channel
    Topology

Behaviour:
    boots a sealed, recoverable bootstrap composition
    accepts authorised discovery results, artifacts, and evidence from an outer Host
    applies device-local admission and authority policy unless Host-owned mode is explicit
    resolves internal child generations through declared Composition Ports
    reaches internal Ready and Release before activating the exported outer boundary
    preserves topology membership across internal and outer composition
    presents its Environment through a Gatekeeper with an explicit understanding Outcome
```

This is not the ordinary mouse profile. A conventional mouse is normally simpler and more reliable
as factory-composed firmware under the Static Embedded Profile. A sufficiently capable “smart
mouse” may use the Host-Assisted profile, but architecturally it is then a small computer in the form
of a peripheral. The mechanism is scale-independent and is not restricted to external devices: an
internal controller or subsystem may receive the same assistance through authorised Channels and a
runtime-open Port. Exact discovery transport, artifact delivery, device identity, attestation,
recovery, and federation rules remain open protocol work; self-description never makes the outer
Host or device trust the other.

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
make database providers first-class Brontide participants without defining a universal query
language, relational model, or lowest-common-denominator database API. A provider exposes one or
more Actors, obtains and evaluates Capabilities normally, declares the Domain Vocabularies and
Shapes it supports, declares which Corpus Forms and Store Operations it implements, uses Flows
where transfer is potentially unbounded, and declares any State, Transaction, Persistence,
consistency, or Event Distribution semantics on which clients may rely. A database provider may
realise a Store without making Store synonymous with database.

Provider and application Operations remain legitimate authored vocabulary. PostgreSQL, a graph
database, an embedded store, and a business application may expose different Operations while
sharing Brontide authority, attribution, Execution, Interaction composition, Flow, Shape, and Outcome
semantics. Portable
database behaviour requires a separately ratified common vocabulary; it is not implied merely by
conformance to the Database Profile.

The acceptance test for this direction is architectural: Brontide primitives must be sufficient to
construct a useful Database Profile and provider integration without adding database-specific
terms to Base. Detailed database semantics are deliberately deferred.

## 21. Domain Vocabularies

A **Domain Vocabulary** defines standard Brontide-native semantic concepts for a particular field.

Brontide does not need to know what a mouse is.

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

These concepts use the Brontide authority model.

Domain Vocabularies allow independent devices and implementations to agree on semantic meaning
without Brontide Base needing to standardise every type of hardware, software system, or
organisational activity.

They are a major part of Brontide interoperability.

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
- **Corpora and storage semantics, where applicable** — each Corpus name, owner, version, kind,
  Form, participating Shapes, lifecycle and migration rules, concurrent-access
  semantics, Store roles, absence behaviours, and Component-Corpus roles; plus Store Operations and Attribute sources, or explicit references to
  the specifications that define them (§18.2).
- **Constraint types** — the Constraints it defines for use with the Base algebra (§10.1), each
  with the full declaration: canonical name and version, the Shape of every carried value,
  evaluation semantics, and accounting scope where quantified (§10.1); any Definition Constraint
  operators or Attribute paths it standardises for composition (§18.1); and migration guidance —
  a semantic change is a new canonical name, with version-skew fallback written explicitly as
  `AnyOf(NewConstraint, OldConstraint)`.
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

## 22. Names, Authorship, Declaration Prefix Blocks, and Notation Strictness

Brontide names are structurally legible and semantically opaque (§6.10).

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

### 22.1 Standard Brontide names

Standard Brontide concepts use unqualified Concept Paths. Current and anticipated examples include:

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
Brontide specification. Every portable semantic concept with an independently referenced identity
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
Brontide:PredictivePlacement
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
Declared Fragments, Constraint types, Corpora, Store roles, Attribute sources, Parameters,
extension concepts, and portable Component or Hot-swap Class identities. It does not turn every
runtime object into a canonical semantic name. In particular, a Capability is a particular target-
recognised grant (§10), not merely a permission name: its authority provenance comes from Genesis
and Delegation. An authored Capability template or Constraint type uses an authored name; an
individual grant retains its own identity, holder, target, scope, and derivation. A Dataset and
Store likewise have concrete resource identities; they do not become canonical semantic
definitions merely because their Corpus or Store-role contracts have authored names.

A qualified name is not normative Brontide functionality solely because an Brontide implementation
exposes it. An organisation may initially define `Erste:Audit`. If a sufficiently general audit
vocabulary is later ratified, the system may expose `Audit.Start` with appropriate Constraints
and metadata.

Brontide may reserve an authored namespace for proposals incubated by the Brontide project. Such names
remain non-standard until ratified into an unqualified Brontide Concept Path.

For example:

```
Logitech:Input.Scroll.Resistance
Razer:Input.Scroll.Resistance
```

might contribute to:

```
Input.Scroll.Resistance
```

Promotion into the standard Brontide vocabulary is **ratification**, with the consequences defined
in §23.

### 22.3 Declaration prefix blocks

Specifications frequently declare several neighbouring concepts. Brontide documents MAY group such
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

### 22.4 Strict and NonStrict notation

An Brontide document or machine-readable definition SHOULD declare its **notation strictness**:

```
notation: Strict | NonStrict
```

Strictness changes notation and permitted inference only. It MUST NOT change the resolved
architectural model, normative force, authority, compatibility, or lifecycle semantics. A
NonStrict document may use fully explicit Strict notation whenever clarity benefits.

A **Strict** document:

- uses expanded canonical names wherever an architectural identity is referenced;
- carries versions in their defined version fields rather than treating them as name text;
- identifies member kinds and owning definition paths where the canonical grammar requires them;
- makes Parameter binding stages, defaults, and Attribute-source Operations explicit; and
- does not rely on prefix-block, enclosing-definition, previous-line, or similarly contextual
  shorthand in its canonical declaration set.

Strict notation is appropriate for package manifests, signed definitions, conformance fixtures,
interchange contracts, generated bindings, policy, and canonical ratification records. Human prose
may quote or explain Strict definitions without every sentence becoming machine syntax.

A **NonStrict** document may use deterministic, locally visible abbreviations such as §22.3
`within` blocks, a Store role declared as `name: Core` inside one Corpus, a locally scoped Parameter
name, or a uniquely resolvable canonical prefix. It MUST NOT use ambiguous shorthand, and every
abbreviation must have one normalisation into Strict form. NonStrict permits shorthand; it never
requires shorthand.

The candidate canonical identity for a typed member uses a distinct member separator rather
than overloading dot segments or inventing another authorship separator:

```
CanonicalName := [AuthorityPath ":"] ConceptPath ["#" MemberKind "." MemberName]
```

For example:

```
Brontide:Editor.Project
Brontide:Editor.Project#Store.Core
Brontide:Editor.Project#Parameter.HistoryDepth
```

The Corpus version is carried separately:

```
corpus-name: Brontide:Editor.Project
corpus-version: 3
store-role-name: Brontide:Editor.Project#Store.Core
```

This form preserves the one colon meaning already defined by §22 — `Brontide Reference Stack` is the Authority
Path; everything after the colon is the Concept Path — while keeping every dot segment
semantically opaque (§6.10). Encoding member kind as ordinary dot segments was rejected: an
ordinary authored namespace could legitimately produce the identical canonical string with a
different referent, and canonical names that are frozen, signed, and referenced by Capabilities
must have exactly one meaning forever. Reserving member-kind words was likewise rejected,
because every future member kind would retroactively collide with names already ratified —
unacceptable in an append-only namespace. The member separator also prevents a Store role and a
Parameter with the same local spelling from colliding. `#` is the working candidate glyph; the
final glyph, escaping rules, the member-kind catalogue, and whether all member categories
require typed segments remain provisional with the canonical-name grammar itself.

In this document's declared NonStrict notation:

```
Corpus:
    name: Brontide:Editor.Project
    corpus-version: 3
    stores:
        - name: Core
```

normalises to the Strict identities above. A normaliser SHOULD be able to emit that expansion and
record any inferred owner, scope, version source, default, or Parameter binding. A document without
a strictness declaration MUST NOT be assumed Strict; legacy prose is treated as NonStrict unless a
governing specification says otherwise.

## 23. Versioning and Ratification

The versioning problem for an authority model is concrete: a Capability granted against one
version of a vocabulary may be presented to an implementation of a later version. Whether it
still means the same thing must not be a matter of luck.

Brontide eliminates the problem rather than managing it, with one discipline borrowed from the
systems that got evolution right:

> **Ratified names are immutable.** Once a concept — Operation, Execution, Interaction,
> Event, Capability, Shape, Declared Fragment, Shape field, Constraint type, Corpus, Store role,
> Attribute source, Parameter, Event subject, or Outcome distinction — is ratified
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

Corpus definitions likewise carry an explicit version separate from the canonical Corpus name
(§18.2). Their exact compatibility and migration rules remain provisional;
versioning may describe a lifecycle transition but may never silently reinterpret a ratified
canonical name. Store-role and Parameter identities remain members of their owning definition
rather than gaining hidden meaning from document position (§22.4).

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
  denies (§10.1); encountering an unrecognised atom within a composite Constraint expression
  evaluates in three-valued logic and authorises only where the expression is True regardless
  of that atom (§10.1); encountering an unknown
  origin class, treats it as unverified (§15);
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
- **Base freezes hardest.** Brontide Base pre-1.0 may change freely — that is what 0.x means. At
  1.0, the core terms' semantics freeze under the same append-only rule as everything else.
- **Ratification is the freezing moment.** When a name is promoted from a non-standard namespace
  into the standard vocabulary (§22), its semantics freeze irreversibly. The governance process
  must therefore include semantic review *before* ratification; that irreversibility is the
  point, not a hazard.

The openness presumption (§29.1) covers the remainder: unspecified behaviour may change at any
version, which is precisely why relying on it is non-conforming.

## 24. Devices, External Systems, and Trust Admission

Brontide does not determine whether a device, Component, or remote system is honest. That
judgement ultimately belongs to the composition or to the receiving authority domain. Brontide
does determine which side has the power to make it: an external participant may propose identity,
functionality, evidence, and requirements; only the receiving domain can admit it and grant or
recognise authority there.

### 24.1 Claims are proposals, not grants

> **Self-description is not authority. A claim may inform admission; it cannot perform
> admission.**

A device or external system may claim an Actor identity, device class, manufacturer, supported
Operations, Profile conformance, origin class, attestation, or Capability. Every such statement is
input to the receiving domain's admission machinery. Under §10, a purported Capability authorises
only when the target recognises the grant, its Operations, and its structural contract. Under §12,
an external participant cannot cause Genesis in the receiving domain merely by requesting it.

Before the receiving domain makes an admission decision, the participant has no Brontide authority
inside that domain. It may produce electrical signals, bytes, packets, or protocol messages at the
substrate, but those are observations available to already-authorised boundary machinery, not
self-authorising Executions.

A domain may deliberately offer a narrow, low-trust role on the basis of an unverified description.
That remains the domain's policy decision; it does not verify the description. A policy may instead
use a physical attachment point, administrator approval, an existing pairing, a cryptographic key,
remote attestation, supply-chain evidence, or several signals together. Evidence has only the
meaning the domain's trust policy assigns to it. In particular, attestation may establish an
identity or measured state under a chosen trust root; it does not establish benevolence and does
not itself grant authority.

Admission may create local Actor references, establish compatible bindings, and issue local
Capabilities. These are separate decisions. Recognising a Shape, Operation, vocabulary, Profile, or
Component contract establishes meaning or compatibility, never authority. A Component declaring
that it requires a Capability requests a composition condition; the declaration does not satisfy
it. Origin likewise remains unverified unless the domain grants an origin assertion under §15.

A claim of unlimited authority therefore has no effect by itself. Authority is relative to a
target and to Operations that target recognises, and the presented grant must be recognised by
that target. A participant may hold broad authority inside its own domain; that fact creates no
authority in another domain.

### 24.2 Device attachment

A device does not need to run a conventional operating system to implement Brontide. It may
participate through one or more Actors, but a descriptor such as "I am a mouse" is not proof of a
trusted identity. The host may represent each admitted function as a separate Actor and grant only
the authority required for that function. A device that also advertises keyboard, storage, network,
or vendor-specific functions receives no authority for those functions unless the host admits them
separately.

A host policy may intentionally admit any attached interface that claims a compatible pointer
protocol into a minimal pointer role. That is a useful low-trust compatibility policy, not a
verified claim that the device is a particular model, came from a particular manufacturer, is
benign, or reflects human intent.

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

The physical attachment is nevertheless a foundational topology observation. The Host assigns each
attachment occurrence its own local Topology Node and relates admitted pointer, button,
configuration, battery, lighting, and other functions to it. Two attached mice therefore produce two
nodes: compatible sensor and button roles are not free to cross-pair merely because discovery found
them at the same time. Detachment and reattachment normally produce a new node unless a separate
persistent-identity mechanism establishes continuity.

The connection path may justify a local `AttachedThrough` or `PartOf` observation. A device
descriptor may additionally claim `SamePhysicalAssembly`, `HostedBy`, `SharesPowerDomain`, or
`SharesFailureDomain`, but those claims remain attributable and subject to Host policy. A wireless
receiver containing several devices illustrates why they are separate: one transport endpoint need
not mean one physical assembly, identity, user context, failure domain, or authority domain. The
Host may construct a synthetic local node when the device exposes no topology description and may
refine or reject a proposed grouping. “Same device” is consequently a user-facing projection over
accepted relations, never a primitive equality and never an authority grant.

When only the shared receiver is observable and finer membership cannot be established, the Host
preserves that uncertainty. It may relate separately admitted function occurrences to the receiver
through `AttachedThrough`, but it must not invent which sensor and buttons form one physical mouse.
Those functions remain ungrouped or require an explicit local choice until attributable description,
independent observation, or other accepted evidence supports a finer relation. Topology represents
what is known and claimed; it cannot recover physical truth that no observer can distinguish.

The worked composition example in §18.1 separates the mouse's private factory-composed firmware from
the Host's attachment-time Composition. A device descriptor may help the Host construct the latter;
it does not make the device's private firmware modules into Components or make `Base` an installed
Component.

Note the direction of authority: publishing pointer input into the host's input system requires
authority over that system, even though the delivered Input occurrence is an Event. Where the
attachment policy admits the pointer function, the host grants publication and admission
Capabilities to the corresponding device Actor at attachment — a Genesis occurrence (§12) under
the host's policy, typically liveness-scoped (§10.3) so that detachment kills the authority with
nothing to revoke.

The grant may carry `Origin.Device` assertion authority (§15) because attachment is the moment the
host observes a physical device. That origin class vouches only for the physical kind of cause. It
does not assert a device class, manufacturer, firmware identity, harmlessness, or human action.
Sensitive interaction may therefore distinguish `Origin.Device` from a separately guarded
`Origin.Human` path.

Attachment is not cross-domain interaction, even though the device has its own implementation.
The host's attachment machinery creates Actors within the host's own authority domain that
represent the attached device. The binding between those Actors and the physical device is the
Actor–Execution binding of §6.5 — owned by the host implementation and, per §28, part of its
trusted computing base. A device's internal Brontide domain, where one exists, remains distinct;
attachment does not join the two domains architecturally. Where a future `Distributed` extension
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

Once the function has been admitted, a host that understands the standard Input vocabulary can
interact with its standard semantics. Vocabulary recognition removes the need for a
manufacturer-specific application for routine configuration; it does not establish manufacturer
identity or trust.

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

A Brontide-compatible system may already understand the standard concepts.
The manufacturer remains free to innovate outside the standard.
Brontide makes the semantic boundary visible without turning the manufacturer's claim into trust.

### 24.3 External systems and authority domains

A remote system remains a distinct authority domain. Its Actor identities, primordial grants,
Capability representations, origin claims, and internal policy are meaningful inside that domain;
they do not become authority in the receiving domain by being transmitted. As §8 requires,
Capabilities do not travel between trust boundaries and authorisation happens at each boundary.

A receiving system may expose a gateway or boundary Actor whose local Capabilities bound the
effects it can cause. A future attested federation may map verified external evidence into local
Actor references and authority under explicit policy. In both cases the receiving domain owns the
mapping. The remote participant cannot select its own local identity, import its authority graph,
or decide which of its claims the receiver trusts.

Brontide Base deliberately does not define mutual identification, attestation, or a cryptographic
cross-domain Capability representation. Those belong to the provisional `Identity` and
`Distributed` extensions (§8, §33). Until a stronger Profile or extension defines such a
relationship, an unrecognised external claim authorises nothing. A future protocol must state its
trust anchors, freshness and replay rules, claim-to-local-authority mapping, origin treatment,
failure behaviour, and withdrawal semantics; successful verification remains evidence consumed by
policy, not authority chosen by the peer.

### 24.4 The limit of the guarantee

A composition author or an authority domain's policy may select a compromised Component, trust a
malicious key, or grant an admitted participant every Operation the domain knows. No architecture
can prevent its own trusted decision-maker from deciding badly. The grant machinery, composition
root, attachment policy, boundary adapters, and evidence verifiers are part of the authority
domain's trusted computing base (§8, §28).

Brontide's guarantee is narrower and essential: the decision is local, explicit, and attributable;
the external participant cannot make it true by self-assertion; subsequent Delegation only narrows
it; and every target evaluates the authority it recognises at its own boundary. If an attachment
policy blindly promotes a claimed device class into broad authority, the resulting exposure is an
over-broad Genesis decision, not authority created by the attacker. If the attacker compromises the
host machinery that makes or enforces that decision, the authority domain itself is compromised and
§28's out-of-scope boundary applies.

## 25. Systems and Macro-Scale Operations

Brontide Actors may represent system boundaries larger than a process or device.

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

Brontide does not require those internals to be visible to every caller.
The exposed Actor represents the architectural boundary at which authority is exercised.

This allows a system to communicate more than transport-level intent.

A conventional API request may state:

```
POST /jobs
```

with an implementation-specific payload.

An Brontide Operation may state semantically:

```
Audit.Start
    organisation: Erste
    scope: FinancialControls
```

The exact metadata model is outside Brontide Base and remains to be specified.

The architectural difference is that the Operation's semantic identity and required authority are
first-class. Its Outcome may identify a created long-lived activity; that activity may emit Events
and later terminate through another Outcome. This supports richer policy, Delegation,
observability, discovery, recovery, and interoperability in higher-level Brontide specifications.

Brontide does not require every API call to become an Brontide Operation.
Brontide Operations should represent semantic actions at boundaries where explicit authority and
interoperable meaning are useful — a boundary is worth an Operation where uniform exposure is
valuable even if substitution is not (§21.2).

## 26. Admission: Interacting with Bounded Capacity

Every Actor has bounded capacity. A human has limited attention. An autonomous Actor has limited
compute. A device has a limited duty cycle. A service has a limited queue.

Requesting service from an Actor consumes that Actor's capacity.

> An Actor, or a **Guardian** (§19.3) acting for it, MAY treat the right to request as authority: an
> **admission Capability**, granted, attenuated, and delegated like any other. Where admission
> is capability-guarded, an unauthorised request is denied by the standard authority machinery
> before it reaches the expensive capacity it targets.

Validation, transport, and rejection still consume bounded front-door resources; admission is not
a claim to eliminate physical or network-level denial-of-service. It prevents unauthorised
requesters from reaching more valuable attention, compute, device duty cycle, or service queues
and limits amplification after the authority boundary.

Spam and prompt-flooding of autonomous Actors can therefore become unauthorised Executions rather
than only application-specific categories; rate limits are ordinary Constraints (`max-rate`,
liveness-scoped leases) and follow the quantified accounting rule (§10.1) — delegated admission
draws on the ancestor's budget unless a different accounting scope is explicitly declared; and
admission composes with Delegation — a service granted admission to
an agent may delegate narrower admission to its own sub-workers.

### 26.1 The human seam

Human interaction endpoints are the strictest instance of admission, in two layers.

**Admission before presentation.** A request to interact with a human Actor carries its
Delegation chain like any other Execution. The human's Guardian (§19.3), realised by an operating
system, device, or agent shell, applies policy *before anything renders*: chains that do not terminate in a
trusted primordial root, or that arrive from unverified or non-whitelisted origins, are refused
or quarantined mechanically. The naive attack population never reaches the person. Requesting a
human's attention is itself an Operation, requiring a Capability granted by the human's Guardian
— which makes phishing not a UX failure but an unauthorised Execution, denied by the same
machinery that denies any other Execution.

**Bound consent.** For requests that pass admission, "the human approved it" must mean
approved-as-shown, by a verified human act — not that some process holding the human's delegated
authority clicked a button. Following the pattern of §10.3, Base states the obligation rather
than the mechanism. The `Intent`/`Presentation` extensions MUST define:

- how an approval record identifies the presentation the human actually acted upon,
- how the Guardian vouches the origin class (§15) of the response through its trusted input
  path,
- and how both bind to the resulting Delegation record.

One possible realisation hashes the presentation artifact into the Delegation record; the choice
of mechanism is not Base semantics.

The full presentation mechanics belong to those extensions. Base carries only the principle:
human participation flows through guarded, recordable interaction endpoints, and humans differ
from other bounded-capacity Actors in policy strictness, not in mechanism. In the recorded
Mediation direction (§18.1), the Guardian is an Arbiter over human attention. It is a Gatekeeper only
when the human-facing interaction is also a covered crossing of a Protected Environment.

## 27. Brontide and Existing Systems

Brontide is deliberately agnostic about implementation depth.

A firmware system may implement the Brontide model directly:

```
Brontide
  ↓
Firmware
  ↓
Hardware
```

A hosted runtime may implement Brontide above an existing operating system:

```
Brontide software
  ↓
Brontide runtime
  ↓
Host adapter
  ↓
Linux
```

An operating system may implement Brontide through native services:

```
Brontide software
  ↓
Native Brontide services
  ↓
Kernel
```

A future operating system may use Brontide as its primary computational architecture.

These are implementation choices.

Brontide should allow movement between them without requiring Brontide-native software to adopt an
entirely new authority model.

The same freedom applies at the application boundary. One application may be deeply composed with
system-native Event, Corpus, Store, State, Identity, Presentation, Web, scheduling, and accelerator
Components.
Another may remain a single opaque process with private authentication, storage, and rendering.
Both are legitimate hosted applications. Brontide conformance applies only to the boundaries and
contracts they actually claim.

Conversely, a system-native window need not imply a monolithic system-owned application. A browser
surface, for example, may compose a Web or document engine, execution engine, renderer, security
policy, credential provider, history, and Workspace presentation from separately selected
Components. Replacement of any part remains subject to the explicit compatibility and lifecycle
semantics of §18.1; composability does not make hot swapping automatic.

This is particularly important for hosted implementations.

Linux may be used because it already solves difficult hardware and compatibility problems.
The Brontide architecture must not therefore become Linux-shaped.

Existing systems are substrates and integration targets.
They are not the ontology of Brontide.

## 28. Threat Model

**In scope.** Brontide's authority semantics are designed to withstand: malicious or compromised
Actors within an authority domain attempting to forge, amplify, or replay authority; confused
deputies exercising authority on behalf of unauthorised requesters; self-asserted identity, device
class, conformance, origin, or authority being presented as though it were a target-recognised
grant (§24); masquerade — presenting an effect as originating from a source class it did not
(§15); malicious peers in cross-domain interaction presenting invalid or over-broad Delegation
chains; malformed Shape values attempting to exploit ambiguity between implementations; authored
fragments attempting to reinterpret a canonical Shape or smuggle authority through ignored
structure; and reliance on unspecified behaviour as an escalation path.

**Out of scope.** Brontide does not defend against compromise of the authority domain's own
implementation — each domain's implementation is its trusted computing base, and a domain that
lies about its enforcement lies about everything. Nor can it make an authorised composition root
or admission policy choose wisely: deliberately or negligently selecting a compromised Component,
trusting malicious evidence, or granting excessive authority is a trusted-domain decision (§24.4).
Brontide requires that decision to remain explicit and attributable; it cannot make the decision
correct. Side channels, physical attacks, and denial-of-service below the admission model (§26) are
likewise outside the authority model, though extensions and Profiles MAY address availability.

**Information flow.** Brontide constrains *access* at every architectural boundary; it does not
constrain *re-propagation* after delivery. An Actor authorised to observe data may thereafter
transmit what it observed. Exfiltration by an authorised observer is out of scope; unauthorised
observation is in scope. The positive claim Brontide does make is **legibility**: because
observation is capability-gated, every first hop of every data flow is an explicit,
attributable, auditable grant — delegations of observation are visible data-sharing decisions.
Brontide cannot prevent a betrayal of trust; it guarantees the trust was explicit and the betrayer
is identifiable.

**Legibility is scoped to first hops.** Every grant and every first hop of every data flow is
explicit and attributable. Transitive reachable authority — what an Actor can ultimately cause
through deputies exposing Operations backed by broader authority (§11, §25) — is not computable
from the Delegation graph alone, and Brontide does not claim it. Analysis of reachable authority
is the `Authority Topology` extension direction (§19, §33).

**Cross-domain evidence** extends exactly as far as verification of the other domain's
attestation, and no further. Trust and any mapping from that evidence to local authority remain
the receiving domain's policy. Attestation neither grants authority nor proves benevolence
(§24.3).

## 29. Conformance

Brontide conformance is behavioural.

An implementation conforms to an Brontide specification when it preserves the observable semantics
required by that specification.

### 29.1 The openness presumption

> Any behaviour not explicitly specified by an Brontide specification is unspecified. Unspecified
> behaviour is presumed open to change between implementations and between versions of an
> implementation. Reliance on unspecified behaviour is non-conforming use.

Stated once, here, inherited by every extension, Profile, and Domain Vocabulary.

This clause exists because its absence has a known outcome: where a specification is silent, the
first popular implementation's behaviour becomes the de facto contract, accident by accident,
until the implementation *is* the specification and can never change. The presumption ensures
Brontide's de facto behaviour can never quietly become its de jure contract — including Brontide Reference Stack's
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
    X carries no Constraint restricting Delegation

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

Composite Constraint conformance follows three-valued evaluation (§10.1, §18.1):

```
Given:
    Actor B holds Capability Y permitting Fan.Stop

And:
    Y carries the Constraint Not(Example:Exposure in {Public})

When:
    an implementation that does not recognise Example:Exposure evaluates an Execution
    presenting Y

Then:
    Not(Unknown) is Unknown and the Execution is denied

And:
    AnyOf of a satisfied recognised atom and an unrecognised atom is True and authorises

And:
    AllOf of a satisfied recognised atom and an unrecognised atom is Unknown and denies

And:
    AnyOf(X, Not(X)) with X unrecognised is Unknown and denies; implementations do not
    reason across repeated atoms

And:
    a resolver evaluating AnyOf of an unrecognised atom and a matching atom for provider
    selection retains the candidate and records the unrecognised atom
```

Constraint values are evaluated strictly, never by projection (§10.1, §16.4):

```
Given:
    Constraint type Example:GeoFence declares value Shape Example:GeoFence.Area 1

And:
    Example:GeoFence.Area 2 adds an optional exclusion-zones constituent

And:
    a delegator narrows a Capability with an Example:GeoFence.Area 2 value carrying
    exclusion-zones

When:
    an evaluator recognising only Example:GeoFence.Area 1 evaluates an Execution
    presenting that Capability

Then:
    the atom is unevaluatable and, standing alone, the Execution is denied

And:
    the evaluator does not evaluate the Example:GeoFence.Area 1 projection of the value

And:
    a Capability instead carrying AnyOf of the version 2 atom and a satisfied version 1
    atom authorises through the authored fallback branch
```

Chain conjunction and quantified accounting are behavioural requirements (§10.1, §11):

```
Given:
    Root grants Capability G with Constraint K to Actor A

And:
    A derives C1 for B, and B derives C2 for D, adding no Constraints

When:
    D presents C2 for an Execution violating K

Then:
    the Execution is denied even where the representation does not inline ancestor
    Constraints
```

```
Given:
    Capability P carries rate <= N per window and two sibling derivations exist

When:
    the first sibling performs N authorised Executions within one window

Then:
    the second sibling's Execution within that window is denied, because both draw on
    the single budget at the Constraint's occurrence in P

And:
    denied Executions consume nothing
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

- **Brontide mechanics** — Operation and Execution invariants, Interaction composition, Shape
  identity, version compatibility and projection, Capability recognition, authority evaluation,
  attenuation, Delegation validity, Event attribution and immutability, Outcome distinctions, and
  declared extension transitions — MUST be verifiable wherever their observable semantics apply.
- **Domain effect** — that the fan physically stops, that an audit meaningfully occurs — is
  verifiable in degrees. Each Domain Vocabulary MUST declare which of its domain semantics are
  covered by conformance tests and which are trusted (§21.1).

The two rules compose:

> The guaranteed surface of an Brontide specification is its normative text; the *attested* surface
> is what its conformance suite tests; everything else is unspecified and open.

The suite is evidence, not definition — the prose specification remains normative.

Where a domain effect is not mechanically verifiable, Brontide does not pretend trust away; it
makes trust attributable. The responsible party is the Actor exposing the Operation, and the
Delegation chain records who granted what through whom. The design rule: *mechanically verify
everything verifiable; for the remainder, ensure the trusted party is explicit, named, and
reachable through provenance.*

Attestation is flat — "verified against suite X version N, attested by P" — not graded
certification levels.

Brontide should be accompanied by conformance tests wherever normative behaviour can be tested
mechanically. Profile conformance requires satisfaction of the extensions, vocabularies, and
additional behavioural requirements defined by that Profile. Non-standard functionality must not
silently satisfy normative Brontide requirements unless a specification explicitly defines such an
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

## 30. Brontide, Brontide Reference Stack, and Brontide Minimal Stack

Brontide Reference Stack is the first implementation of Brontide.

Its purpose is to test whether the Brontide model remains useful when applied to larger and
heterogeneous systems.

Brontide Reference Stack is expected to explore areas such as:

- multiple cooperating devices,
- resource discovery and selection,
- remote execution,
- persistent identity,
- humans and autonomous Actors participating in common Delegation relationships,
- semantic device integration,
- macro-scale semantic Operations,
- and modular application environments.

These are not arbitrary features attached to Brontide after the fact.
The desire to describe such systems coherently is one of the reasons Brontide exists.
They are not part of Brontide Base because Brontide attempts to derive them from smaller architectural
concepts and explicit extensions.

Brontide Reference Stack may experiment with concepts before Brontide standardises them.
Brontide Reference Stack-specific functionality uses the `Brontide:` namespace.

For example:

```
Brontide:PredictivePlacement
```

Brontide must not adopt a concept solely because Brontide Reference Stack implements it.

Brontide Reference Stack is an experiment, implementation, and source of evidence.
It is not the specification.

**Brontide Minimal Stack** is the second, deliberately independent implementation. Its primary
purpose is narrower: to test Brontide composability and substitutability. Brontide Minimal Stack
implements its own components rather than treating Brontide Reference Stack as the hidden platform,
and favours a lean implementation surface where Brontide Reference Stack favours a practical
showcase.

The proof is component interchange, not merely two programs passing the same conformance suite.
Where both stacks implement the same Profile, Extension, and Domain Vocabulary contracts, a Brontide Minimal Stack
component should be usable within a Brontide Reference Stack environment and a Brontide Reference Stack component within a Brontide Minimal Stack
environment without either side depending on the other's private types or conventions.

Component has the scale-independent meaning described in §18.1. The first interchange proof may
use process-sized Components because they are practical test instruments; that does not restrict a
Component to a process, package, or application module. Interchange tests substitutability under
declared contracts. Runtime binding and hot swapping are stronger, separate claims and require the
additional replacement semantics defined there.

Shape is central to this test (§16). Shared Operation names and Capability semantics do not create
interoperability when the components disagree about the structures those Operations and
Constraints carry. Brontide Reference Stack and Brontide Minimal Stack may use different language types and encodings; their shared
input and output Shape identities, Declared Fragment identities, versions, and compatibility
rules are the architectural contract.

The interchange experiment should prototype the proposed Brontide Portable Binding rather than
allowing its first process protocol to become an accidental private convention. Brontide Reference Stack and Brontide Minimal Stack
should implement the binding independently, including Shape-guided inline values, binding-scoped
identifiers, authority presentation, and at least one referenced-resource or pooled-buffer path.
The experiment supplies evidence for the binding; agreement between the two implementations does
not ratify it by itself.

Interchangeability is always scoped to the declared contracts. A Brontide Reference Stack or Brontide Minimal Stack component that
requires additional Profiles, extensions, vocabularies, authored Operations, Shapes, or Declared
Fragments is not substitutable for a component lacking those requirements, and must expose
that difference explicitly. This is expected composition, not a failure of Brontide.

Brontide Minimal Stack-specific functionality uses the `Brontide:` namespace. Brontide must not adopt a concept merely
because Brontide Reference Stack and Brontide Minimal Stack both happen to implement it.

Brontide Reference Stack and Brontide Minimal Stack are complementary experiments and sources of evidence.
Neither is the specification.

### 30.1 A decisive demonstration

The most useful Brontide demonstration should not merely show that a small application can be built.
It should make simplicity, incremental system participation, implementation substitution, and
optimisation visible in one coherent workflow.

A collaborative image-processing workspace is a suitable test because it begins as an ordinary
application while exercising typed values, bulk resources, persistent history, presentation,
remote work, and accelerator-friendly transformations.

**Stage one: a small local composition.**
The first version contains independently understandable Components for an image source, resize or
filter Operation, metadata extraction, thumbnail presentation, and history presentation. It runs
locally on a CPU with no database, network, shared identity provider, or distributed Event service.
The source should remain small enough that a new developer can understand and modify one module
without learning the complete environment.

**Stage two: incremental system participation.**
Without rewriting the transformation Components, the composition adopts system-provided Event
persistence, execution history, undo or replay, searchable metadata, permission-aware access, and
shared Workspace state. Each adoption is a separate declared dependency and may be removed or
replaced independently. The demonstration must show which added facility supplies each behaviour.

**Stage three: explicit substitution.**
While preserving the logical workspace, the environment replaces selected Components: an image
decoder, a persistence provider, an identity provider, the Web or presentation engine, and a
metadata implementation. At least one Brontide Reference Stack Component should be replaced by a Brontide Minimal Stack Component and
at least one stage should move to another machine or authority domain. A visible binding and
Execution view should show the selected provider, crossed boundaries, representation choices,
authority, state handoff, and any interruption or retry. Continuity must come from declared
replacement semantics, not a staged illusion.

**Stage four: optimisation without application redesign.**
The same composition then processes a workload large enough to make placement material. A pure,
deterministic, batchable transformation may be selected for compilation or lowering to a GPU or
other accelerator provider. The application continues to request the same semantic Operation; the
Binding Plan and implementation expose batching, buffers, copies, compilation, dispatch, failure,
and fallback. The demonstration must not imply that arbitrary code is accelerator-compatible or
that the CPU and accelerator paths have identical operational characteristics.

**Final proof: one mixed system.**
The resulting workflow should contain Brontide Reference Stack, Brontide Minimal Stack, and third-party Components in one composition,
for example a Brontide Reference Stack scheduler, Brontide Minimal Stack Event store, Brontide Reference Stack accelerator provider, Brontide Minimal Stack identity
provider, third-party thumbnail Component, and separately selected presentation engine. One
complete workflow across them supplies stronger evidence than two isolated reference stacks.

The demonstration succeeds only if it proves:

- a simple module remains simple;
- system facilities can be adopted incrementally;
- substitutions and operational boundaries are inspectable;
- optimisation follows explicit eligibility rather than magic; and
- the application remains coherent across independently implemented stacks.

Its final claim is therefore not that Brontide Reference Stack is modular. It is that Brontide is not Brontide Reference Stack, and Brontide Minimal Stack
is not Brontide either.

## 31. Related Work

Brontide stands in a lineage, deliberately. Most of its hard sub-problems have been solved at least
once, and the failures are as instructive as the successes.

**Capability operating systems.** KeyKOS, EROS, and Coyotos proved that capabilities-as-the-only-
authority is viable for a whole system, and contributed the revocation-via-indirection pattern.
seL4 demonstrated formally verified capability enforcement; its capability derivation tree is
the structure Brontide's Delegation graph abstractly is (§11), and the framing against which future
revocation semantics will be defined. Capsicum demonstrated the hybrid adoption path — capability
discipline coexisting with an ambient-authority host — which is the strategic position of Brontide Reference Stack
on Linux; its lesson is that the seam is where the model leaks.

**Distributed object capabilities.** The E language and CapTP (today OCapN) contain the deepest
treatment of capabilities across machine boundaries — unforgeable remote references, membranes —
and the definitive analysis of confused deputies, which §13.6 inherits. Macaroons proved
monotonic caveat-based attenuation in production and is the direct source of §11's structural
rule; its bearer-token weakness motivates proof-of-possession representations at the
cross-domain tier. Biscuit extends the same idea with offline public-key attenuation. UCAN is
Brontide's nearest relative in ambition — humans, services, and devices in one delegation model.
SPKI/SDSI is the direct intellectual ancestor: authorization rather than identity certificates,
and local names rather than global ones (§8); its failure to deploy teaches that being right is
insufficient without a coexistence path.

**Contrast cases.** Zanzibar-style relationship-based access control is the opposite
architecture — authority as a central database queried at check time rather than held, delegable
references — and the default assumption Brontide must explain itself against. OAuth 2 scopes are
the incumbent for cross-organisation delegation: coarse, non-attenuable, with delegation bolted
on; Brontide is in part "what scopes should have been." The WebAssembly component model shows the
industry independently converging on capability-secure boundaries at yet another scale.

**Vocabulary governance.** USB HID is the existence proof that device-class vocabularies can
achieve full substitutability with vendor extension space. Bluetooth profiles show the median
outcome (fragmentation); UPnP shows the failure mode (no conformance teeth — hence §29.3);
Matter shows the modern cost (certification requires institutions); schema.org shows vocabulary
sprawl when adding terms is free (hence ratification discipline, §23).

Brontide's distinctive synthesis is one authority model spanning `Fan.Stop` to
`Accounting.ClosePeriod`, with semantic Operations, Executions, and Events first-class at every
scale; the
Embedded Test as a standards-design constraint; and humans and autonomous systems as peers in one
delegation calculus. Individual mechanisms have precedents. Their combination and intended scope
are the architectural claim Brontide Reference Stack must test.

## 32. Authors' Discussion: The Larger Direction

Brontide Base is intentionally much smaller than the systems that motivated it.
This is a deliberate tension.

The authors consider interoperability between radically different computational participants to
be central to the larger Brontide direction.

A future Brontide environment may contain:

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

Brontide does not seek to erase the differences between them.
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

Brontide Base does not define this environment. The anticipated Architectural Extensions are
intended, in substantial part, to make such environments possible.

Semantic interoperability is another major direction.

Applications, devices, and larger software systems currently bundle large amounts of meaning
inside implementation-specific interfaces. A mouse exposes configuration through a vendor
application. A headset exposes semantic device state through proprietary software. A corporate
system may expose an operation such as beginning an audit only through undocumented combinations
of API endpoints, payload conventions, workflow state, and organisational knowledge.

Standard Brontide-native Domain Vocabularies may allow systems to expose more of that semantic
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

These directions are not accidental uses of Brontide.
They are part of the reason for defining Brontide.

### 32.1 Applications and systems as reciprocal compositions

Traditional operating systems provide coarse resources while applications repeatedly build their
own event buses, databases, identity systems, plugin mechanisms, schedulers, browser runtimes,
observability, and distributed coordination. Brontide creates the possibility of exposing these as
ordinary, replaceable system capabilities without freezing one implementation into the operating
system architecture.

The environment would not say *this is the database* or *this is the browser*. It would expose
available Corpus, Store, State, Persistence, Transaction, Web, Presentation, Workspace, Identity,
or other contracts together with capability-derived Attributes. One composition may select an in-memory Store,
another a local embedded provider, another a remote distributed provider, and another an
application-owned implementation that exposes only selected Operations back to the environment.

A browser may become a composition rather than a sovereign application boundary: document and
network processing, script execution, rendering, security policy, profile and credential provision,
history, and presentation can be separate Components. A documentation viewer, IDE preview, game
launcher, and conventional browser may share an engine without sharing presentation or policy. A
single presentation may select different compatible engines for different workloads, subject to
the explicit substitution rules of §18.1.

Identity and authorisation support may be composed similarly. An application may request that an
Actor be established or authorised for a particular boundary while constraining acceptable
providers: local identity, enterprise identity, a passkey provider, anonymous session,
application-owned login, or delegated remote identity. The Capability evaluated for the resulting
Execution remains explicit; using an identity provider must not become ambient authority.

The reciprocity matters. Applications do not merely consume the environment. They contribute
Operations, Events, Shapes, and Components back into it. The boundary between "system" and
"application" becomes a choice of composition, policy, authority, and opacity rather than a fixed
architectural caste. Section 18.3 preserves the equally important opposite choice: an application
may remain a box.

### 32.2 Developer trust is an architectural requirement

Brontide asks developers to accept composition across more boundaries than conventional application
platforms. That trust must be earned through observable constraints.

**Do not recreate transparent distributed objects.**
Shared Operation semantics may allow selection across placement boundaries, but latency, partial
failure, serialisation, retries, and failure domains remain real. Tooling and traces must expose
them. "It might be remote" is not an adequate failure model.

**Do not let optionality become nominal interoperability.**
If every provider assigns different meanings to identity, failure, cancellation, time, streams, or
transactions, the ecosystem becomes a collection of bespoke adapters. Shared Shapes, Profiles,
Domain Vocabularies, graded interoperability claims, and explicit semantic Adapter Components are
the guardrails. Structural coincidence is not compatibility.

**Do not build a god-platform by architectural pressure.**
A service is not meaningfully optional if only the system Event service can be debugged, only the
system identity provider is recognised by tooling, or only the system store participates in
recovery. System participation may unlock richer common behaviour, but private mechanisms must
remain legitimate, their boundaries must remain honestly opaque, and provider-specific advantages
must be declared as such. Migration into and out of system-native services should be testable where
portable contracts are claimed.

**Make executions explainable.**
Composition increases the number of plausible failure sites: invalid Shape, Enrichment, provider
selection, authority rejection, remote placement, retry, accelerator lowering, Event replay,
persistence, or stale presentation. An Brontide environment should provide a structured explanation
of what happened, including the submitted Execution, applied Enrichments, selected implementation,
Binding Plan, crossed boundaries, emitted occurrences, Outcome, timing, and causality. Logs may
support this explanation; a search through unrelated logs is not the explanation model.

**Make dependency gravity explicit.**
Useful platforms create gravity. A Component that requires generic Event semantics, benefits from
durable Event Distribution, or depends on a provider-specific recovery guarantee must state those
different dependency strengths. Developers should be able to identify which interoperability they
gain and which portability they give up.

**Do not promise universal acceleration.**
Ordinary code may allocate, mutate, throw, recurse, perform I/O, call arbitrary libraries, or depend
on nondeterminism. Brontide should accelerate implementations that declare and satisfy the relevant
execution properties, not market every Operation as automatically GPU-compatible. Profiling,
eligibility, compilation failure, fallback, and representation costs must remain visible.

**Let the model unfold progressively.**
The first useful experience should be: create a module, expose an Operation, bind another module,
execute it, and inspect what happened. Remote execution, persistence, security, replay, and
acceleration should become discoverable layers rather than vocabulary required before the first
result.

The purpose of keeping Base small is not to minimise the ambition of the architecture.
It is to avoid permanently embedding the shape of today's largest systems into the foundation of
tomorrow's.

## 33. Open Questions

Architecture 0.8 preserves the current Base, composition, Profile, and implementation directions;
§35 records the changes from Architecture 0.7, and Brontide-Architecture-Change-History.md retains
the historical diffs from Architectures 0.2 through 0.6. The following remain genuinely open.

**Revocation beyond mortality.**
Liveness-scoped authority (§10.3) covers the common cases cheaply. Immediate revocation of
long-lived authority, the precise semantics of the revocation horizon, and the fate of in-flight
Operations and Flows when authority dies mid-execution remain unresolved. Brontide should
distinguish authority withdrawal from cancellation of an already accepted activity. Long-running
Operations likely need declared safe checkpoints, commit points, compensation Operations, and a
maximum revocation horizon. The Delegation derivation graph (§11) is the structure authority
withdrawal will prune; `Lifecycle` and `Flow` must define what existing work then does. The
chain-conjunction representation choice is each domain's revocation ceiling (§11): revocation
semantics must state which representations can satisfy them, with revocation-via-indirection
(§31) the candidate for carried representations.

**Cross-domain interaction.**
Base authority semantics are defined within a domain (§8). Section 24 establishes the floor:
self-description is not authority, admission belongs to the receiving domain, and verified evidence
can only inform that domain's local mapping. Mutual identification, attestation, the cryptographic
representation of Capabilities and origin claims, and defence against a hostile domain vouching
falsely are the substance of the `Identity` and `Distributed` extensions, and the largest body of
unfinished work in the Brontide direction. Device attachment does not wait for this work: §24.2
handles attachment entirely within the host domain, with attested federation as a later upgrade.

**Channel.**
The invocation principle (§13.6) requires that authority travel with requests; the mechanism —
request/response representation, error propagation across boundaries, delivery semantics —
awaits the `Channel` extension. Until it exists, the principle constrains implementations
without fully equipping them. The evidence sequencing is decided: Channel is derived from the
retained Cooling and Catalog interchange evidence and precedes the Portable Component Binding,
which becomes its first conforming realisation. The shared frame extracted from that evidence is
now recorded in [the Channel design note](../channel/Brontide-Design-Note-Channel-0.1.md); it remains a
direction, not a ratified extension, and the canonical error taxonomy, correlation model, and
authority-presentation representation remain open there.

**Portable Component Binding and mapping.**
Section 18.1 proposes the Brontide Portable Binding and a scoped Binding Plan as the general-purpose
seam. Its exact framing, schema-guided CBOR subset, scalar and field mappings, numeric dictionary,
resource-reference representation, ownership and synchronisation rules, bounds, negotiation,
fallback behaviour, and conformance surface remain open. Reference/Minimal interchange must compare an
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
The architecture defines semantic fields, not a wire encoding. The minimum identity rules,
protected fragment data, and compact reuse of shared context need stress-testing.
`emitted-at` is signed integer milliseconds in a named time domain; the standard time-domain
registry and richer uncertainty semantics remain open.

**Shape catalogue and composition.**
Section 16 defines independent input and output Shapes, additive same-name evolution, arbitrary
fragments as projections, named and versioned Declared Fragments, open-record composition, and
canonical projection. The exact standard scalar catalogue, recursive Shapes, evolution of choices,
canonicalisation for signing and hashing, host-version requirements for fragments, cross-fragment
invariants, descriptor discovery, and representation negotiation remain to be specified. Brontide Reference Stack
and Brontide Minimal Stack must demonstrate that unknown Declared Fragments can be ignored for a canonical
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

**Resource identity and lifetime after issuance.**
The issuance rule (§12) resolves how authority over created resources comes to exist: by
Delegation from the providing Actor's authority, never by mid-flight minting. What remains open
for the future `Resource` extension is the created thing itself — identity, lifetime,
designation stability, and reclamation — and the vocabulary through which providers declare
their resource spaces.

**Constraint as a ninth Base term.**
Architecture 0.8 records Constraint in the term registry as a subordinate concept within
Capability carrying Base-normative declaration discipline (§7.1, §10.1). Whether ratification
should promote Constraint to a ninth Base term is deliberately left open; the declaration
discipline is normative either way. The rejected alternative is also recorded: per-atom
`strict = false` leniency markings were considered and rejected because an authority guarantee
is only as strong as its weakest reachable evaluator — an adversary routes the Execution to the
evaluator where the atom is unknown, making an ignorable narrowing the absence of a guarantee
presented as one. Graded strictness lives in authored `AnyOf` fallbacks (§10.1) and Profile
recognition catalogues (§20).

**Terminus disposition vocabulary.**
Terminus is defined (§12); the standard vocabulary for survival schedules — which outbound
grants persist, for how long, and under whose custodianship — and the interaction of Terminus
with `Lifecycle` and in-flight Flows remain open.

**Reachable authority analysis.**
Legibility is scoped to first hops (§28). The `Authority Topology` direction (§19) must define
take-grant-style reachability over the Delegation graph plus declared deputy surfaces, without
requiring omniscient visibility into opaque Components.

**Holder introspection.**
Whether an Actor may enumerate the Capabilities it holds — and whether that facility is
`Discovery` extension business or a Base-adjacent right — is recorded as a decision to be made,
not an omission. Introspection of held authority is distinct from discovery of available
Operations, and embedded domains satisfy any future answer statically.

**Mid-effect authority semantics.**
Authorisation is instantaneous (§13.5); Base defines no re-evaluation against an effect in
progress. Which extensions define checkpointed revalidation, and how such re-evaluation
interacts with withdrawal, compensation, and Flow recovery, remains open — fenced, not
accidental (§29.1).

**Resource, State, and Transaction.**
The candidate `Resource` extension concerns identity, lifetime, and authority over created things;
`State` concerns observable condition, revisions, and authorised transitions; `Transaction`
concerns declared commit and atomicity relationships. Their exact boundaries, composition, and
minimal contracts remain open. None may assume a database, filesystem, persistence mechanism,
universal rollback, or one data model merely because those are important consumers. Their
relationship to Dataset identity, Store retention, Corpus migration, and transactions spanning
several Corpus Store roles must be made explicit without collapsing the terms.

**Corpus, Dataset, and integration terminology.**
Section 18.2 provisionally adopts Corpus for the semantic definition and Dataset for a concrete
body of information. Corpus is intentionally near-stable but remains open to a clearly superior
non-composite term before terminology freeze; Dataset must be tested against small configuration
records as well as large collections. The App-Level → Opaque Corpus → Structured Corpus ladder is
useful but categorically asymmetric because App-Level information is not a Corpus. The final
specification must either retain that caveat or find terminology that removes the ambiguity without
pretending private application information is system-integrated.

Corpus-version compatibility, irreversible migration, partial restoration, semantic identity
across import/export, identity-bearing Store roles, Dataset splitting, Dataset custody, and
selection of default managers require Reference/Minimal evidence. The mostly closed Form list — Opaque, Record, Collection, Map, Graph,
Journal, and Stream — must be stress-tested against documents, media projects, encrypted vaults,
game saves, time series, directory-like structures, event-sourced systems, and live telemetry.

**Store roles, Stores, Store Relationships, and Routers.**
The base direction binds each Corpus Store role of a Dataset to exactly one logical Store, permits
several roles to share a Store, and makes independently governed placement visible as distinct
Stores. The exact atomicity and failure semantics when one Dataset uses several roles remain open.
The absence behaviours `UseRole`, `Discard`, `Recompute`, and `DisableFeature` need conformance
tests, especially across migration and rollback.

Mirror and Backup are candidate static Store Relationships; their consistency, recovery point,
retention, deletion propagation, authority, and failure reporting are not yet ratified. Router owns
policy-driven fallback, tiering, sharding, jurisdictional selection, and similar decisions while
presenting a Store-compatible contract. Experiments must establish how much Router topology is
visible, which guarantees describe the logical endpoint rather than its current backing Store, and
how tools explain migrations, copies, outages, and fallback without exposing confidential policy.

**Attributes and recursive Definition Constraints.**
Section 18.1 replaces free-floating selection labels with values obtained through exact Brontide
Operations, Shapes, and ordinary Capability presentation. The portable description Operations,
Shape paths, comparison vocabulary, units, reference perspective, freshness, provenance, and
attestation model remain open. `AllOf`, `AnyOf`, and `Not` are recursively composable over atomic
Constraints; Brontide Reference Stack and Brontide Minimal Stack must prove deterministic evaluation and explanation of nested branch
matches. The relationship between this candidate expression language and the Base Capability
Constraint algebra must remain explicit so provider selection never grants authority and a future
operator cannot widen delegated authority accidentally.

Dynamic Attributes are permitted but deliberately do not trigger automatic rebinding. The final
Composition and Router specifications must define when an observation becomes a declared profile,
what policy reacts to changes, and how oscillation, stale values, confidentiality, and unavailable
Attribute providers are handled.

**Parameters and definition language.**
Composition Parameters may shape resolved architecture; Activation Parameters may fill declared
resource slots but may not introduce new architectural structure. The exact boundary for
credentials, Actor identities, Store selection, optional features, and provider choice needs
testing. Defaults and context-derived values must remain reproducible and attributable.

Parameter scope, explicit forwarding, derived values, parameterised Corpora, parameters containing
other parameterised definitions, cycle detection, partial application, and whether comments or
annotations carry any machine semantics remain open. Architecture 0.8 does not admit a hidden
ambient configuration system under the Parameter name.

**Strict notation and canonical member identity.**
Section 22.4 distinguishes Strict canonical declarations from NonStrict deterministic shorthand
without changing semantics. The candidate typed member identity
`Authority:Definition#MemberKind.MemberName`, the member separator glyph, the member-kind
catalogue, normalisation record, escaping, and interaction with declaration prefix blocks
require ratification.
Tooling must prove that NonStrict documents expand uniquely and that signed or machine-actionable
artifacts never depend on contextual inference.

**Presentation and Workspace.**
`Presentation` and `Workspace` are intended to be shared, application-facing facilities rather
than private machinery of a shell, filesystem navigator, or Web implementation. The boundary
between surfaces and interaction on one side, and views, navigation contexts, tabs, panes,
history, bookmarks, and provider-supplied hierarchies on the other, requires a dedicated
specification. Saved navigation must identify a context without silently preserving authority.

**Work-in-progress Profiles.**
Section 20.1 records Interactive Application, Web, and Database as Profile directions without
ratifying their dependency sets. The Database Profile is specifically an architectural
acceptance test: provider-specific calls must remain possible while Brontide supplies sufficient
generic authority, Base Shape, Flow, State, Resource, Transaction, and Outcome machinery to integrate
the provider as a first-class participant.

**Optional system services and boxed applications.**
Section 18.3 permits deep application/system composition and an opaque boxed application as equally
valid choices. The portable discovery, preference, dependency-strength, migration, and fallback
contracts for system-provided Event, Corpus, Store, State, Identity, Presentation, Web, scheduling,
and accelerator facilities remain open. Tooling must show richer support for participating Components without
quietly treating private mechanisms as defective or making one provider mandatory in practice.

**Execution explanation and debugging.**
Interaction, Origin, Enrichment, Binding Plans, Events, and Outcomes provide pieces of a structured
explanation model, but Brontide does not yet define the minimum trace that answers what happened. The
identity and retention of traces, confidentiality of Shape values, representation of provider
selection and crossed boundaries, causal linkage across domains, sampling, redaction, and behaviour
for opaque Components require a dedicated specification. Debuggability must not require every
private application mechanism to become Brontide-visible.

**Optimisation and accelerator eligibility.**
Purity, determinism, replay safety, batchability, vectorisability, relocatability, and accelerator
compatibility are candidate execution properties, not consequences of Operation conformance. Their
portable definitions, attestation, compiler responsibility, fallback rules, numerical equivalence,
resource and copy accounting, profiling semantics, and interaction with authority and failure remain
open. Brontide Reference Stack and Brontide Minimal Stack should demonstrate the same semantic transformation through CPU and GPU paths
without hiding their operational differences or requiring an application redesign.

**Composition and hot swapping.**
Section 18.1 defines provisional scale-independent Component terminology and distinguishes static
binding, startup-time generational resolution, runtime binding, replacement, scoped restart,
interchangeability, and hot-swappability. The Component descriptor, recursive composition rules,
Provider Set representation, binding-scope and cardinality declarations, occurrence sharing,
distinct and mediated exposure, Composition Region and Port descriptors, incremental child-
generation resolution, Port-envelope validation and widening, Binding Plan representation, Hot-swap Slot and Class
representation, dependency negotiation, attachment and replacement occurrences, state handoff,
failure atomicity, rollback, interruption guarantees, and treatment of in-progress work remain
open. Cyclic contract and post-release interaction dependencies are not forbidden and do not define
startup order. The portable Local
Initialisation, Interconnection, Relational Initialisation, Ready, and Release declarations;
lifecycle-only and ordinary-interaction gates; strongly connected-group resolution; explicitly
ordered activation groups; and deterministic diagnostics remain to be proven.

The threshold between direct binding and declared Mediation is semantic rather than numeric. Its
portable descriptor expression and validation remain open. In particular, the boundary
between Presentation or Workspace orchestration—layout, cloning, focus, user association, and
logical-surface policy—and display Distribution remains open. A renderer should not silently absorb
that topology policy, while generic Distribution should not define user-experience semantics.

Composition's promotion path is now recorded without ratification. The minimal static/dynamic
`Composition` extension contract, its optional dependency boundary with `Discovery`, the exact
Component-management requirements of a General-Purpose System Profile, and the conformance evidence
for Static Embedded and Host-Assisted Composable Device Profiles remain open. The Host-Assisted
profile additionally needs portable bootstrap, recovery, discovery delivery, device-local versus
Host-owned admission, nested Release, and outer-boundary activation protocols. In particular, a
profile requirement imposed on a Host
must not accidentally become a requirement that every passively integrated leaf implement a
resolver or Discovery protocol.

Composition's minimum topology floor also requires exact Node and Relation identity, vocabulary,
observation and claim attribution, persistence across detach and reattach, device-supplied evidence,
Host refinement, and Region/Port serialization rules. That minimum membership floor remains owned by
Composition; Topology Map, Environment, protection, Gatekeeper, Sentinel, and Environment View semantics belong to
the future `Topology` extension. Their exact Shapes, evidence formats, and protocols remain open.
The recorded Guardian-family discipline leaves open the portable declaration and validation forms
of Gatekeeper type and export fidelity, the Plane dimension and enforcement-basis vocabulary with
same-basis detection, the coverage and assurance evidence formats and the resolution-time no-bypass
check, alias-relation and receiver-owned pairing records, the intra-domain continuity declaration,
and Sentinel Watch Shapes, subject queries, source-coverage evidence, evaluator contracts, finding
Shapes, and response-separation contracts. Sentinel's semantic boundary is recorded: it is the
primary third-party observer and reporter for a bounded Watch, universal in eligible subject kind
but never implicitly universal in visibility. The remaining question is how portable declarations
make that boundary mechanically decidable while preserving domain-specific and model-based
interpretation.
Implementations must not collapse physical assembly, hosting, transport, power, failure, identity,
trust, or authority into one `same device` flag while those rules are unsettled.

The Component Manager direction further leaves its portable descriptor and package formats,
source-registration and Discovery Query contracts, candidate and storefront projections, Preferred
Provider declarations, publisher identity, occupied-binding and Provider Set behaviour,
generic-provider criteria, ranking and explanation policy, integrity and provenance evidence,
trust-policy seam, generation record, transaction boundaries, retention, removal, and garbage
collection open. A deterministic, entirely fake manager should exercise these seams and present a
realistic local storefront before
any online marketplace or production loader is attempted. Reference/Minimal interchange, device
replacement, service failover, and data-centre-scale substitution should test whether one contract
model survives all these cases without making a package manager, mapping engine, or deployment
orchestrator part of Brontide Base.

The same section treats topology, authority-domain boundaries, latency, cost, capacity,
availability, and related values as capability-derived Attributes rather than reducing placement to
`local` or `remote`. Their source Operations, portable Shapes and units, provenance, attestation,
freshness, measurement semantics, aggregation, privacy, and policy language remain open. A
Component descriptor's self-claim must not silently become a verified placement or service-level
guarantee.

**Brontide Reference Stack and Brontide Minimal Stack interchange.**
Passing the same conformance suite is necessary but not sufficient evidence of component
interchangeability. The component boundary, binding metadata, Shape negotiation, host services,
and minimum adapter responsibilities must be discovered by exchanging real Brontide Reference Stack and Brontide Minimal Stack
components without allowing either implementation's private object model to become an Brontide
contract.

**Conformance inside composite Actors.**
When a platform exposes one Actor at its boundary (§25) while containing thousands of internal
services, what does conformance require of the interior? "Where Brontide applies" needs a
definition with edges.

**Authored namespace verification.**
Authority Paths are structurally hierarchical (§22), but syntax is not proof. Registry,
signature, delegated namespace authority, collision handling, display-name safety, and recovery
from compromised authoring keys require a governance and Identity design.

**Where does an Architectural Extension end and a Domain Vocabulary begin?**
Some concepts are clearly architectural; others clearly domain-specific. The boundary should be
defined carefully enough to prevent Brontide from absorbing every semantic standard built on top of
it.

## 34. Summary

Brontide is an open specification for actor-centric, capability-based computing and
interoperability.

Its core principle is:

> *Actors execute Operations by presenting explicit and bounded Capabilities.*

An Actor is defined by participation in the Brontide authority model rather than by implementation
form or scale. A process, firmware subsystem, peripheral, human, autonomous system, or composite
organisational system may participate through Actors. Brontide does not claim these participants are
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

Shape is the named, versioned structural contract for values carried at Brontide boundaries. Every
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
are responsible for preserving Brontide semantics. Authority originates at genesis — primordial
grants and enumerable, attributable Genesis occurrences — is issued over newly created
resources by Delegation from their providers, and ends at Terminus, the enumerable and
attributable counterpart of Genesis. Thereafter authority only narrows: Delegation derives
authority by adding Constraints, never by rewriting, so that delegation cannot amplify by
construction. Constraints are evaluated once, when an Execution is presented, and fail closed;
composite expressions evaluate in three-valued logic; quantified Constraints draw on one pooled
budget at their chain occurrence; and Constraint values are never evaluated by projection,
because the payload and authority planes cross boundaries under opposite regimes (§6.16).
Authority defaults to mortal where domains are dynamic.

Self-description never creates authority. Device descriptors, remote identities, conformance
claims, attestation, and purported Capabilities are inputs to the receiving authority domain's
admission policy. They authorise only through a target-recognised local grant or mapping; the
participant cannot choose the mapping for itself. A composition root or admission policy may still
trust the wrong Component or evidence — an unavoidable trusted-computing-base decision — but an
external claim cannot silently become authority (§24).

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

Brontide Base is intentionally small and must satisfy the Embedded Test: a microcontroller without
an operating system, networking, virtual memory, or dynamic allocation implements every Base
requirement with static structure. Base has eight terms: Actor, Capability, Shape, Delegation,
Operation, Execution, Event, and Outcome. Shape satisfies the Embedded Test through compile-time
signatures and static tables while remaining necessary for agreement between independent
implementations. Frequently deployed concepts such as Transaction remain extensions when they are
not necessary to that minimum. A future convenience Profile may collect common extensions for
general-purpose systems without acquiring privileged status or changing Base.

Every such system is still composed in the ordinary sense. What remains outside Base is the
requirement to expose that construction as a portable contract. The recorded `Composition`
extension direction covers Components, requirements, Provider Sets, Composition Regions and Ports,
bindings, Mediation, generations, and lifecycle in static as well as dynamic forms; `Discovery`
remains an optional source of candidates for its resolver. The General-Purpose System Profile
direction requires managed, inspectable Composition and provisionally includes Discovery, while the
Static Embedded Profile direction requires neither and permits passive integration into a larger
composing Host. The Host-Assisted Composable Device Profile occupies the deliberate middle: a sealed
device bootstrap may use outer discovery assistance while retaining local admission and activating
complete internal child generations before exposing them to the outer system.

Brontide grows through Architectural Extensions, Profiles, and Domain Vocabularies. Profiles declare
direct dependencies in `uses:` blocks and may use other Profiles; conformance expands through the
transitive dependency closure without repeating indirect requirements. Standard names use
unqualified Concept Paths, which are reserved for ratified Brontide concepts. In authored names, `:`
separates a hierarchical Authority Path from its Concept Path, as in
`Logitech.MX:Input.Scroll.SmartShift`. Names are structurally legible and semantically opaque.
Declaration prefix blocks reduce repetition in documents but expand to canonical names before use.
Ratification freezes a canonical name's semantics forever; additive availability remains governed
by Profiles and discovery.

Brontide documents declare Strict or NonStrict notation. Strict definitions use expanded canonical
names and explicit version and binding fields; NonStrict documents may use deterministic shorthand
that normalises to the same model. Authorship retains the single `AuthorityPath:ConceptPath`
separator, typed members use a distinct member separator (§22.4), and versions remain separate
claims rather than becoming part of a canonical name. Architecture 0.8 itself uses NonStrict explanatory notation.

Beyond Base, a Component is a scale-independent unit of composition declaring the Brontide contracts
it provides and requires. Components and Actors are distinct: Components define composition
boundaries; Actors participate in authority. Bindings may be static or runtime-established. A
Hot-swap Host Component exposes a Hot-swap Slot for a declared Hot-swap Class; a Hot-swappable
Component conforms to that Class and its replacement obligations. Their joint contract must state
compatibility, Actor identity, authority, state, in-progress work, interruption, and rollback
semantics rather than treating live replacement as an automatic consequence of shared names.
Remote services are not a separate Component kind: `local` and `remote` are projections of richer,
capability-derived Attributes such as topology, authority domain, latency, cost, capacity,
availability, and failure domain. These Attributes guide selection among compatible
Components without changing their semantic contract identities. Implementations may still present
the useful `local`/`remote` shorthand in user interfaces and summaries, provided it remains a
declared, lossy projection rather than a source of unstated guarantees.

Component management follows a staged, generational direction. Arbitrary Component Sources may
make descriptors, artifacts, and evidence available, but acquisition and self-description grant no
authority. An authorised selection shapes a pending composition; the resolver recursively closes
its structure and Parameters and may prepare it while the current generation runs; activation then
cuts over at a declared restart boundary. Activation Parameters may be acquired during preflight
but cannot introduce structure absent from the resolved generation. Establishment proceeds through
Local Initialisation, Interconnection, optional lifecycle-only Relational Initialisation, and Ready;
one logical Release then makes the group Active. Logical dependency, relational-initialisation, and
post-release interaction cycles are allowed when their declared protocols close finitely and
deterministically; ordinary application traffic remains gated until Release.

Incremental, or per-partes, composition applies the same generation protocol recursively. A parent
Region declares a Composition Port and its contracts, imports, exports, cardinality, authority
ceiling, topology, lifecycle, failure, and rollback envelope. A complete child generation may then
be resolved and released through a runtime-open Port while the parent remains active. Internal and
external attachment use the same rule; a child that exceeds the envelope requires rejection or an
explicitly wider parent generation. Attaching to an empty Port does not by itself promise live
replacement of an active child.

Composition also preserves a minimum topology membership model. Each attachment occurrence has a
local Topology Node, and attributable relations keep its Components, Actors, resources, Regions, and
Ports associated without equating physical assembly, hosting, connectivity, power, failure, identity,
trust, or authority. Thus two mice cannot have their compatible subfunctions accidentally combined,
while a declared Aggregation may still join their input Flows without erasing either membership.

The broader Topology direction distinguishes ordinary overlapping Environments from Protected
Environments. Ordinary Environments have no Gatekeeper requirement. The Guardian family records
protective Actor roles without granting authority: Guardian is the general role, Gatekeeper is its
preventative Protected-Environment-boundary specialisation, and Sentinel is its bounded third-party
observation-and-reporting specialisation. A Sentinel Watch makes purpose, subjects, occurrence
classes, sources, coverage, lifecycle, evaluator, outputs, and gaps explicit while leaving
domain-specific interpretation open. Protected Environments are disjoint or
nested within one Protection Plane, expose their opaque interiors only through Gatekeepers, and have no
declared external communication while no Gatekeeper is active. Each Gatekeeper may expose different contracts
and an Environment View without making the Environment itself universally identical to a Component,
Actor, or Authority Domain. Gatekeepers present portable Protected Environment references, while explicit
versioned Topology Outcomes establish whether a peer understands them; understanding does not
establish recognition, trust, or authority. Every Gatekeeper export declares one fidelity class — Direct,
Deputised, Mediated, Adapted, or Synthetic — so a boundary never presents reinterpretation as
exposure, and the standing authority behind Adapted and Synthetic exports is enumerated rather than
hidden.

A contract role is not a system-wide singleton. Several Component definitions may provide one role,
one definition may have several activated occurrences, and differently scoped consumers may bind
different providers concurrently. Each requirement resolves an identity-preserving Provider Set
with declared cardinality, sharing, scope, and distinct or mediated exposure. Distinct exposure
keeps members independently addressable; mediation may select, distribute, aggregate, arbitrate, or
apply domain-specific rules without laundering member identity or authority. Several keyboards may
therefore remain per-user inputs or feed one declared Aggregation; several displays may receive
separate feeds or explicit Distribution; and several database providers may serve separate
applications or Datasets, sit behind Selection, or participate in declared storage relationships.

Simple topology remains simple: direct `1..1` bindings and intentionally member-addressed distinct
sets need no mediation machinery. Once one logical endpoint owns Selection, Distribution,
Aggregation, Arbitration, membership masking, or topology-wide policy, Mediation is mandatory in the
resolved model. Its realisation may be erased when trivial, while relationships with independent
state, policy, authority, recovery, or lifecycle should use a dedicated mediating Component. Display
rendering therefore need not absorb multi-screen topology; the future Presentation and Workspace
directions still need to define how UX layout policy composes with Distribution.

A manager may aggregate multiple Component Sources. Serving endpoint and publisher identity remain
distinct, although a future UI may present local and remote sources as familiar stores. Compatible
occupied `1..1` bindings remain stable by default. For each required open Provider Set position,
Preferred Providers, publisher-affine candidates, generic implementations, and other compatible
candidates form the default ranking tiers under local admission policy. The inspectable Proposed
Stack shows membership, scopes, activation occurrences, sharing, exposure, mediation, preselected
candidates, and alternatives before acquisition or activation. Scoped restart is the ordinary
fluent replacement path, while hot swapping remains a stronger optional contract.

The proposed Brontide Portable Binding supplies a default general-purpose seam without defining one
implementation model. A scoped Binding Plan fixes contracts, authority, representation, memory and
resource handling, synchronisation, delivery, and lifecycle before the hot path. Its portable
binary direction uses compact framing and schema-guided CBOR for ordinary inline values, while
referenced shaped resources permit pooled, shared, device-local, registered, or otherwise
specialised data paths within the same seam. Static and native bindings may compile the plan into
direct calls or specialised machinery. Mapping between private representations of one Shape belongs
to binding or host machinery; semantic adaptation between different contracts remains an explicit
Component-level relationship rather than a Base mapping service.

Composition may expose Shape-described Parameters. Composition Parameters shape the resolved
architecture; Activation Parameters fill resource slots already declared by that architecture and
cannot introduce new structure. Attribute requirements are not ambient labels: they identify the
Operation, vocabulary claim, result Shape, and result path that provide a value under ordinary
Capability evaluation. Definition Constraints compare those values or validate Parameters through
Shape-appropriate atomic relations and recursively composable `AllOf`, `AnyOf`, and `Not` groups.
They select and validate without granting authority; composite expressions evaluate in
three-valued logic, retaining a candidate only where the expression is True; and their
effective values, matching branches, and Unknown atoms should remain explainable.

Persistent information is provisionally modelled through Corpus, Dataset, Store roles, Stores, and
Routers. A Corpus is an authored, versioned semantic definition independent of the Components that
operate on it. A Dataset is one concrete body conforming to a Corpus. App-Level information remains
outside the model for compatibility; an Opaque Corpus gives Brontide lifecycle understanding without
content interpretation; a Structured Corpus uses Record, Collection, Map, Graph, Journal, or Stream
Form with recursively composed Shapes. Component-Corpus roles describe compatibility, not
authority. Removing a Component does not imply removing its Datasets.

Each Corpus defines required or optional Store roles and explicit absence behaviour. Every role of
a Dataset binds to one logical Store endpoint; several roles may share one Store, and a Router may
present the Store contract while applying explicit policy across backing Stores. Simple Mirror and
Backup relationships remain declarative Store topology, while conditional fallback, tiering,
sharding, and related dynamic decisions belong to Routers. Store requirements name exact Operations
and constrain capability-derived Attributes rather than attaching undefined `local`, `fast`, or
`durable` labels.

A general-purpose environment may offer optional system-native Components for Events, persistence,
Corpus and Store management, State, identity, Presentation, Workspace, Web, scheduling,
observability, compilation, and acceleration. Applications may adopt these facilities incrementally
and may contribute their own Operations back to the environment. They may also remain boxed, with
private authentication, database, events, rendering, and internal composition. Brontide conformance
applies to the contracts they claim, not to an obligation to expose their interior.

Simple participation and sophisticated execution are compatible when stronger properties remain
explicit. A small module may expose an ordinary Operation while the surrounding composition adds
history, security, selection, batching, remote placement, or accelerator execution. Purity,
determinism, replay safety, batchability, relocatability, and GPU compatibility must be declared and
tested; semantic portability does not erase latency, copies, representation, authority, or failure
boundaries.

Developer trust depends on operational legibility. A conforming environment should make provider
selection, Enrichment, Binding Plans, crossed boundaries, retries, emitted occurrences, Outcomes,
timing, and causality explainable without claiming visibility into opaque private Components. System
services should reward participation with interoperability and inspection rather than make private
mechanisms illegitimate through tooling pressure.

Unspecified behaviour is open by presumption; the guaranteed surface is the normative text; the
attested surface is what conformance suites test.

Brontide does not define an operating system.
It defines a common computational architecture upon which firmware, runtimes, devices,
distributed environments, organisational systems, and operating systems may build.

Brontide Reference Stack is the first practical implementation and showcase of Brontide. Brontide
Minimal Stack is the second, independent implementation and composability test. Interchange of
components between their stacks is intended to reveal where an apparent Brontide contract is
actually a private implementation convention.

The decisive application demonstration is a staged image-processing workspace that begins as a
small local CPU composition, adopts system-native facilities incrementally, visibly substitutes
Brontide Reference Stack and Brontide Minimal Stack Components, and moves an explicitly eligible transformation to an accelerator
without changing its semantic Operation. Its purpose is evidence, not spectacle.

Brontide Reference Stack is not Brontide.
Brontide Minimal Stack is not Brontide.

## 35. Changes from 0.7

This section records the changes introduced since Architecture 0.7. The historical diffs from
Architectures 0.2 through 0.6 are retained in `Brontide-Architecture-Change-History.md`.

The 0.8 document edit executes `Brontide-Architecture-0.8-Change-Plan.md` (C1–C14), the product
of an adversarial stress test of the Constraint algebra and the authority lifecycle. The
adversarial conformance vectors accompanying every behavioural change are inventoried in
`conformance/architecture-0.8-adversarial-vectors.json`. Changes applied:

- **§6.16 — Compatibility and authority separated into explicit evaluation regimes (C9).**
  Every Execution carries a payload plane and an authority plane across the same boundary under
  opposite variance: integration tolerance applies only in covariant positions, and every
  position for a Shape-described value must be classifiable. Integration failures surface at
  composition resolution; runtime fail-closed evaluation is the backstop, never the mechanism.
- **§10.1 — Three-valued Constraint evaluation replaces expression poisoning (C7).** Composite
  expressions evaluate in strong Kleene logic; Unknown never resolves at the atom; only True
  authorises or retains a candidate; evaluation is structural with no reasoning across repeated
  atoms. The 0.7 poisoning rule is superseded; its rationale survives inside the new rule, and
  the `AnyOf(New, Old)` migration pattern becomes expressible. The §29.2 selection example
  inverts accordingly.
- **§10.1 — Quantified Constraints gain accounting scope (C5).** Rate, capacity, and count
  Constraints are accounted at their occurrence in the derivation chain: one pooled budget for
  the whole derivation subtree, no fresh budgets at Delegation, no consumption on denial.
  Declared alternative scopes must state their multiplication consequences and fail closed
  where unenforceable.
- **§10.1 — Constraint declarations become Base-normative (C9).** Every Constraint type
  declares canonical name and version, value Shape, evaluation semantics, and accounting scope.
  A domain may decline to implement a Constraint type but must be able to identify one:
  declining is a decision with defined semantics; inability is nonconformance.
- **§10.1, §16.4 — Constraint values exempt from additive projection (C8).** Unrecognised
  structure in a Constraint value makes the atom unevaluatable rather than projectable; the
  authority plane evolves by parallel names and authored fallback, never additively in place.
- **§10.3 — Liveness-scoped validity conjoins across chains (C1).** Every liveness-scoped link
  must be identifiable and evaluatable at the authorisation boundary; domains permitting
  liveness scoping on delegable Capabilities must provide chain-liveness evaluation, and
  grantors without it should prefer wall-clock validity.
- **§13.5 — Authorisation is instantaneous (C3).** Effective authority is evaluated once, at
  presentation; effects in progress are governed by withdrawal semantics (§10.3); mid-effect
  re-evaluation is extension-defined and must be declared.
- **§11 — Delegability default settled (C6).** A Capability is delegable unless a Constraint
  restricts further Delegation; the restriction is an attribution boundary, not an effect
  boundary.
- **§11 — Chain conjunction requires ancestor visibility; representation is the revocation
  ceiling (C4, C11).** Implementations must evaluate the full chain conjunction regardless of
  representation; the representation choice (carried, pre-evaluated, resolved) bounds future
  revocation and must be recorded as an operational property.
- **§12 — Issuance and Terminus complete the authority lifecycle (C10, C12).** Authority over
  created resources is issued by Delegation from the provider's authority, preserving
  conservation; Terminus names the enumerable, attributable policy occurrence at which an Actor
  ceases to participate, with mandatory disposition semantics for held Capabilities, outbound
  grants, and references. The section is retitled accordingly.
- **§15 — Origin demotion expressed inside the Delegation algebra (C2).** Every Delegation
  implicitly conjoins the origin-demotion Constraint, keeping §11's structural rule exact and
  demotion testable, with an explanatory note on why `Origin.Derived` does not become the
  common origin of a working system.
- **§28 — Legibility scoped to first hops (C13).** Transitive reachable authority is explicitly
  not claimed; `Authority Topology` is recorded as the analysis direction (§19).
- **§19, §33 — Holder introspection recorded as a decision, not an omission (C14).**
- **§7.1 — Registry corrected.** Constraint recorded as a subordinate concept within
  Capability; Terminus recorded beside Authority Domain and Genesis; Authority Topology added
  to the provisional extension names.
- **§18.1 and §33 — Staged Component management recorded.** Component acquisition, selection,
  recursive resolution, optional preparation, activation, and retirement are separated through
  immutable composition generations. The ordinary user-facing path may stage a selection while the
  current generation runs and activate it at a declared scoped restart; hot swapping remains a
  stronger optional contract. Establishment now names Local Initialisation, Interconnection,
  optional Relational Initialisation, and Ready before one logical Release makes the group Active.
  Cyclic interaction creates no implicit startup order, while cyclic relational setup must declare
  its lifecycle protocol and completion. Component Manager names the broader lifecycle facility;
  multiple marketplaces and repositories are unprivileged Component Sources; endpoint and publisher
  identity remain distinct; and product UI may project local and remote sources as familiar stores.
  Preferred Providers are hints, occupied compatible `1..1` bindings remain stable by default, and
  unfilled Provider Set positions rank explicit preference, publisher-affine, generic, then other
  compatible candidates under local policy. Multiple definitions and activated occurrences for one
  role are first-class through scoped, cardinality-declared Provider Sets with distinct or mediated
  exposure. Aggregation now records the N-source-to-one-logical-provider Mediation species alongside
  Selection, Distribution, and Arbitration. Direct topology remains direct, but any logical
  selection, fan-out, fan-in, arbitration, membership masking, or topology-wide policy must declare
  Mediation; simple realisations may be erased while policy-bearing relationships favour dedicated
  Components. The Presentation/Workspace-to-display-Distribution orchestration boundary is recorded
  as open. The resulting Proposed Stack remains inspectable before activation.
- **§7, §18–§20, and §33 — Composition separated from ordinary construction.** Every system is
  acknowledged to be composed, while Base requires no participant-visible resolver or Component
  model. A first-party Composition extension direction supports static and dynamic resolution;
  Discovery remains separately optional; Component Manager remains a facility built over them; and
  provisional General-Purpose System, Static Embedded, and Host-Assisted Composable Device Profile
  directions record three useful conformance boundaries. A composing Host may passively integrate a Base-only leaf without
  transferring its own extension obligations to that leaf.
- **§7, §18–§20, §24.2, and §33 — Incremental composition and discrete topology recorded.**
  Composition Regions and parent-declared Composition Ports make recursively resolved child
  generations first-class at build, activation, and runtime without permitting arbitrary mutation of
  an active parent. Each attachment receives a local Topology Node and attributable relations so
  independently represented functions retain device membership without conflating physical assembly,
  hosting, power, failure, identity, trust, or authority. The provisional Host-Assisted Composable
  Device Profile records a sealed device bootstrap, outer discovery assistance, ordinarily local
  admission, nested Release, and external-boundary activation; the ordinary mouse remains the simpler
  factory-composed case.
- **§7 and §19 — Guardian-family and Environment direction recorded outside Base.** A dedicated
  topology note retains the competing reasoning and resolves it: ordinary Environments remain
  overlapping, security-neutral topology identities and have no Gatekeepers. Guardian names an Actor
  entrusted to protect or represent a participant, resource, or bounded interaction; Gatekeeper is its
  preventative Protected-Environment-boundary specialisation, while Sentinel is the primary
  third-party observer and reporter within a deterministic, purpose-bounded Sentinel Watch. Watch
  interpretation may be rule-based or model-based, but subjects, occurrence classes, sources,
  coverage, lifecycle, outputs, and gaps remain explicit. Event or Flow subscription alone does not
  create a Watch, while persistence, rule loading, checkpointing, and alert delivery are legitimate
  Watch-supporting Executions. Sentinel findings grant no response authority.
  Protected Environments are laminar within a
  Protection Plane, opaque except through Gatekeepers, and without declared external communication while
  no Gatekeeper is active. Transparency remains multidimensional and Gatekeeper-relative; Environment identity
  is portable through Gatekeepers, but mutual understanding requires an explicit versioned Topology
  Outcome and an appropriate Profile. No new Base term or ratified extension results.
- **§7, §19, and §33 — Gatekeeper export fidelity and protection discipline recorded.** Every Gatekeeper export
  declares one fidelity class — Direct, Deputised, Mediated, Adapted, or Synthetic — so
  reinterpretation is never presented as exposure: Mediated exports keep the Mediation rules,
  Adapted exports name their Adapter realisation and never reuse a name with changed semantics, and
  Synthetic exports are boundary-authored contracts backed by standing Capabilities held by the
  Gatekeeper, keeping fidelity correlated with the authority it concentrates. A Protection Plane is
  identified by its protection dimension and enforcement basis, closing the
  laminarity-by-Plane-minting escape. No-bypass coverage belongs to the Protected Environment and is
  declared, enumerated, or statically verified; attestation is separate assurance evidence. Peer
  recognition records attributable alias relations rather than merged occurrences, and continuity
  is sequenced through Gatekeeper lifecycle contracts and receiver-owned pairing. Raised by the
  distributed-composition review of the boundary model; none of it adds a Base term.
- **§18.1 and §19 — Composition–Topology seam closed.** Attachment through a Composition Port
  across a Protected Environment boundary is a covered crossing and must terminate at or be
  performed through a declared Gatekeeper; the Port does not become the Gatekeeper. An ordinary Region acquires
  no Gatekeeper obligation merely by having Ports. An Environment is a grouping over the
  Composition-owned topology floor, and a Map's relation vocabulary begins with the floor's
  Relations. Protection holds fail-closed throughout Establishment, with a Gatekeeper admitting nothing
  before its declared readiness point. Export continuity across backing replacement is a Gatekeeper
  lifecycle declaration, never an inference from Gatekeeper or export identity.
- **§18.1 and §24.2 — Composition timing illustrated.** A factory-composed mouse keeps its private
  firmware structure outside Composition while exposing Base-governed Actors; attachment composes
  that boundary into a Host under local admission and authority; and a managed general-purpose
  system composes across image construction, selection, preflight, scoped activation, and runtime
  attachment. Base is explicitly a conformance contract rather than an installed Component.
- **§19 and §33 — Channel direction recorded from interchange evidence.** The first-cycle
  communication extension is extracted from the shared behaviour of the retained Cooling and Catalog
  interchange proofs and recorded in a dedicated design note: framed one-message-per-frame exchange;
  a versioned, kind-discriminated envelope (negotiation, request, outcome, protocol-error,
  lifecycle); echoed correlation identities kept distinct from host-native Execution identity; a
  boundary-relative authority presentation under which no Capability crosses a trust boundary; and a
  strict separation of denial, semantic failed Outcome, and protocol or process failure with no
  foreign exception crossing. The stacks' divergences (correlation-id count, handshake presence,
  host authority domain, replay and payload bounds, error-code spellings) mark the realisation
  freedom Channel preserves. No Base term is added, and the Portable Component Binding is recorded as
  Channel's first intended realisation.
- **§24 and §28 — External claims separated from trust admission.** Self-description, device
  class, conformance, attestation, origin, and purported authority are inputs to a receiving
  domain's policy, never grants chosen by the participant. Device attachment and remote-system
  admission are distinguished, over-broad composition and Genesis decisions are identified as
  trusted-computing-base failures, and the unfinished Identity/Distributed protocol work remains
  explicit. This clarifies existing target-recognition and Genesis invariants without adding a
  Base term or ratifying an attestation mechanism.

Architecture 0.8 makes no change to the eight Brontide Base terms; whether Constraint becomes a
ninth is recorded as an open ratification question (§33).

### 35.1 Direction for 0.9

Architecture 0.8 is the hardening draft. The evidence goals of the cycle remain those recorded
in Architecture 0.7, with the sequencing decided in the 0.8 change plan: `Channel` first,
derived from the retained Cooling and Catalog interchange evidence and equipping the invocation
principle (§13.6); the Portable Component Binding and the Shape floor second, as Channel's
first conforming realisation against the §6.16 presentation contract; Flow conformance third,
unchanged, with Event Distribution and the revocation horizon still terminating in it. Explicit
non-goals are unchanged: `Identity` and `Distributed` wait for proven intra-domain interchange,
`Presentation` and `Workspace` wait, and revocation beyond mortality advances only as far as
Flow ratification forces it — now with the representation-ceiling rule (§11) ensuring neither
stack builds a representation that future revocation semantics cannot reach.
