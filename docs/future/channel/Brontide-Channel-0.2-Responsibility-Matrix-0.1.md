# Channel 0.2 responsibility matrix 0.1

Date: 2026-08-11

Status: proposed first-batch ownership contract; subject to fresh independent review.

## Rule

Every semantic fact has one owner. Another system may carry, enforce, project, or observe the fact
only through a named neutral artifact. Carrying a fact does not transfer ownership of its meaning.

Dependency arrows below point from consumer to semantic owner. Reference and Minimal remain parallel
native consumers of the neutral artifacts; neither appears as the other's dependency.

## Ownership matrix

| Concern | Semantic owner | Consumers / dependency direction | Neutral artifact crossing the boundary | Explicitly not owned by |
| --- | --- | --- | --- | --- |
| Channel contract version | Channel | realization/profile → Channel | Channel profile identity/version | transport, Portable Binding |
| Application/Component contract identity | owning profile / Portable Binding | Channel interaction admission → profile | exact canonical contract reference | Channel core |
| Endpoint roles and allowed directions | Channel profile | Channel session/interaction → profile | role and interaction-class declarations | process topology |
| Fixed/negotiated profile equivalence | Channel | profile realizations → Channel | immutable established-profile record | negotiation codec |
| Wire encoding and frame mechanics | realization profile | Channel → realization declaration | encoding id, framing id, finite bounds | Channel logical contract |
| Session establishment/drain/close/fault | Channel | profiles and hosts → Channel state machine | session control declarations/observations | Composition, Portable Binding |
| Interconnection | Component Management / Portable Binding | Channel class admission → explicit phase predicate | activation member/binding phase observation | Channel session |
| Relational Initialisation phase | Component Management / Composition | Portable Binding and Channel admission → CM3 declaration | exact lifecycle declaration and current phase | Channel session |
| Ready | Component Management / Composition | Portable Binding gate → activation observation | explicit member-ready fact | Channel establishment |
| Release / ordinary gate | Composition / Portable Binding | Channel ordinary-class admission → release fact | explicit released-member/binding fact | Channel session |
| Binding withdrawal and cleanup | Portable Binding / Composition | Channel drain/loss observations → binding coordinator | binding identity, retained effect/resource observations | Channel core |
| Interaction identity and terminality | Channel | profiles/extensions → Channel interaction machine | session-scoped interaction record | Execution/Occurrence identity |
| Bounded unary concurrency | Channel profile under Channel rules | host/transport → established profile | finite max-in-flight declaration | scheduler |
| Scheduling and fairness | host/runtime | Channel observes only | none in Channel core | Channel |
| Cancellation control and terminal meaning | Channel core; class-specific cancellability in profile | Operation adapter → Channel cancellation contract | cancellation feature, authority and terminal declarations | transport abort alone |
| Operation semantics and shaped Outcome | Operation contract | Channel → exact Operation/Shape declaration | canonical Operation, input/output/details Shapes | Channel protocol fault taxonomy |
| Relational interaction declaration | CM3 lifecycle protocol | Channel/Portable Binding → CM3 | edge, direction, members, Operation, Capability, Shape | Channel inference |
| Ordinary interaction eligibility | Portable Binding / Composition | Channel → explicit Release predicate | member/binding release observation | successful establishment |
| Payload compatibility/projection | Shape contract | Channel/profile → Architecture 0.8 Shape rules | canonical Shape/Fragment refs and position classification | authority evaluator |
| Payload/resource representation | Portable Binding or another profile | Channel carries declared positions → profile | representation/resource descriptor | Channel core |
| Resource ownership/lifetime/release/fallback | Portable Binding or Resource extension | Channel loss/terminal observations → resource owner | profile-specific resource observation and cleanup result | Channel session close |
| Intra-domain Capability evaluation | target authority domain | Channel dispatch → local authority result | recognized presentation plus attributable result | Channel compatibility |
| Cross-trust admission and local grants | receiving authority domain / Component Management | Channel carries attributable evidence → local admission | no-capability-transfer mode, exact designations, admission result | sending peer |
| Cross-domain identity and attestation | Identity / Distributed | Channel profile may require facet → extension | versioned evidence/admission facet | Channel core |
| Local pre-dispatch refusal | local host/authority boundary | Channel observation consumes | local refusal observation | peer wire protocol |
| Semantic failure | Operation contract / responding Actor | Channel carries exact Outcome | shaped failed Outcome | protocol fault |
| Peer protocol fault | Channel | local observer consumes peer assertion | scoped fault category plus bounded diagnostics | transport/process observer |
| Transport/process loss classification | local host/realization | Channel observation consumes | local loss category and detection point | peer |
| Effect certainty | Channel form; effect details owned by profile | observations/extensions → Channel certainty | known-none / known(details ref) / unknown(reason) | adapters guessing zero |
| Retry attempt policy | Distributed/host profile | Channel admits each attempt independently | new interaction id plus optional causal prior reference | reuse/replay of one id |
| Delivery, persistence, ordering | Distributed/Flow/Realtime profile | Channel profile may require facet → extension | exact extension facet/version | Channel core |
| Streaming and backpressure | Flow/profile | Channel profile may add interaction class/facet → Flow | stream identity subordinate to interaction, terminal bridge | unary core reinterpretation |
| Long-running activity | Lifecycle | Channel Outcome may identify/start activity under exact extension | activity reference and lifecycle facet | keeping interaction forever nonterminal |
| Timing constraints | Realtime/profile | Channel observes declared timing facts → Realtime | explicit timing facet and clock provenance | ambient Channel clock |
| Logs, metrics, traces, storage | local observability systems | consume Channel observations | non-normative local projection | Channel semantics |

## Selected boundary rulings

### Session state versus activation phase

Channel owns only `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`.
Interconnection, Relational Initialisation, Ready, and Release are external phase facts. This prevents
the 0.1 defect in which readiness was simultaneously a wire event, a binding state, and an activation
claim.

Channel still enforces an interaction class's phase predicate. Enforcement consumes an explicit
fact; it does not make Channel the fact's owner.

### Relational initialization representation

Relational initialization is a state-gated interaction class using the ordinary Channel interaction
machine. It is not a dedicated envelope kind. This keeps Operation, Capability, Shape, correlation,
terminal Outcome, cancellation, and effect attribution in one mechanism while retaining a distinct
class and pre-Ready gate.

### Concurrency and cancellation

Channel core defines bounded concurrent unary interactions and the terminal meaning of optional
cancellation. Profiles select a finite concurrency bound and whether each class supports or requires
cancellation. Scheduling fairness and preemption remain runtime/Operation concerns.

Defining these semantics in core prevents a later extension from changing interaction identity or
terminality. It does not require the first Portable Binding 0.2 profile to select concurrency greater
than one or cancellation support before its vectors justify them.

### Extension hooks

An extension composes as an exact profile facet and may add interaction classes or stronger evidence.
It cannot reinterpret:

- session or interaction identity;
- authority and phase admission;
- semantic Outcome versus peer fault versus local loss;
- one accepted terminal history; or
- effect certainty.

If an extension needs to change one of those, it requires a new Channel contract version rather than
an optional field.

## Dependency constraints

1. Channel neutral artifacts depend on Architecture 0.8 semantics and no stack runtime.
2. Portable Binding 0.2 depends on Channel 0.2 and Shape/authority semantics; Channel does not depend
   on Portable Binding schemas.
3. Component/Composition adapters project explicit phase and declaration facts into a Channel profile;
   Channel does not resolve or activate Components.
4. Flow, Distributed, Realtime, Lifecycle, Identity, and Resource facets depend on Channel's declared
   extension boundary; Channel core imports none of their private machinery.
5. Concrete encodings and transports implement a declared Channel realization; their errors enter as
   local observations or peer protocol faults without becoming semantic Outcomes.
6. Reference and Minimal each implement the neutral contract independently and meet only through
   manifests, fixtures, encodings, and process boundaries.

## Boundary verification required

- A machine-readable ownership inventory must give every normative neutral field one owner.
- The neutral verifier must reject duplicate or missing owners.
- Dependency guards must prove neither stack imports the other and Channel neutral artifacts import
  no stack assembly.
- Native tests must show successful Channel establishment does not create Ready, Release, or an
  authority grant.
- CM3/CM4 integration must show Channel relational success is consumed before Ready rather than
  creating Ready itself.
- A transport-loss test must show the peer never receives or emits a fabricated process-failure
  statement.

## Open boundary work

None blocks the first-batch contract. Resource lifetime, streaming, durable delivery, cross-domain
identity, and long-running activity remain explicitly owned extension/profile work. Their detailed
contracts are not part of Channel 0.2 foundation.
