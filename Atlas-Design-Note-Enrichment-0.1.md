# ATLAS

## Design Note: Enrichment and Value Propagation

**Status:** Work-in-progress design note, version 0.1
**Extracted from:** Atlas Architecture 0.6, §16.6; the architecture document retains a
summary section under the same number.
**Scope:** Records a design direction. Nothing in this note is a normative Atlas
mechanism, adds an Atlas Base term, or defines conformance requirements.

References of the form §N refer to the Atlas Architecture specification.

---

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

## Targeted Enrichment

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

## Ambient Enrichment

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

## Ambient availability and global stores

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

## Enrichment and Capabilities

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

## Systems Are Not Necessarily Topological

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

## Value propagation

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
a general Atlas construct. No choice is made in Architecture 0.6. Fabric and Linen experiments
should test whether common availability semantics can be preserved across static embedded
dispatch, direct in-process calls, dynamically selected modules, and distributed interactions even
when their implementation strategies and available topological views differ.
