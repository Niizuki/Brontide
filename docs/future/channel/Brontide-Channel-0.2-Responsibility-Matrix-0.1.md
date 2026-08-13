# Channel 0.2 responsibility matrix 0.1

Date: 2026-08-11

Status: proposed first-batch ownership contract; B3 and cross-artifact N1 corrected after independent
review and confirmed unchanged by the fourth, fifth, sixth, and seventh reviews. Its
`Delivery, persistence, ordering` row is the evidence for blocking finding S1, which is a defect in
what other artifacts assert rather than in this matrix. It is subject to a fresh independent closure
re-review.

## Rule

Every semantic fact has one owner. Another system may carry, enforce, project, or observe the fact
only through a named neutral artifact. Carrying a fact does not transfer ownership of its meaning.

Dependency arrows below point from consumer to semantic owner. Reference and Minimal remain parallel
native consumers of the neutral artifacts; neither appears as the other's dependency.

The `Semantic owner` column uses one exact owner identifier per row. An identifier names the
contract family that defines the fact; a concrete profile selects the one owner instance where the
family is parameterized. Consumers and carriers remain separate columns and never become co-owners.

## Ownership matrix

| Concern | Semantic owner | Consumers / dependency direction | Neutral artifact crossing the boundary | Explicitly not owned by |
| --- | --- | --- | --- | --- |
| Channel contract version | `channel` | realization/profile → Channel | Channel profile identity/version | transport, Portable Binding |
| Application/Component contract identity | `application-profile` | Channel interaction admission → profile | exact canonical contract reference | Channel core |
| Endpoint roles and allowed directions | `channel-profile` | Channel session/interaction → profile | role and interaction-class declarations | process topology |
| Fixed/negotiated profile equivalence | `channel` | profile realizations → Channel | immutable established-profile record | negotiation codec |
| Wire encoding and frame mechanics | `realization-profile` | Channel → realization declaration | encoding id, framing id, finite bounds | Channel logical contract |
| Session establishment/drain/close/fault | `channel` | profiles and hosts → Channel state machine | session control declarations/observations | Composition, Portable Binding |
| Interconnection | `portable-binding` | Channel class admission → explicit phase predicate | activation member/binding phase observation | Channel session, Component Management |
| Relational Initialisation phase | `composition` | Portable Binding and Channel admission → composition phase | exact lifecycle declaration and current phase | Channel session, Component Management |
| Ready | `component-management` | Portable Binding gate → activation observation | explicit member-ready fact | Channel establishment, Composition |
| Release / ordinary gate | `portable-binding` | Channel ordinary-class admission → release fact | explicit released-member/binding fact | Channel session, Composition |
| Binding withdrawal and cleanup | `portable-binding` | Channel drain/loss observations → binding coordinator | binding identity, retained effect/resource observations | Channel core, Composition |
| Interaction identity and terminality | `channel` | profiles/extensions → Channel interaction machine | session-scoped interaction record | Execution/Occurrence identity |
| Bounded unary concurrency | `channel-profile` | host/transport → established profile | finite max-in-flight declaration | scheduler |
| Scheduling and fairness | `host-runtime` | Channel observes only | none in Channel core | Channel |
| Cancellation control and terminal meaning | `channel` | Operation adapter → Channel cancellation contract | cancellation authority and acknowledgement/terminal declarations | transport abort alone |
| Class-specific cancellability | `channel-profile` | Channel establishment/admission → profile | unsupported/optional/required class declaration | Operation adapter |
| Operation semantics and shaped Outcome | `operation-contract` | Channel → exact Operation/Shape declaration | canonical Operation, input/output/details Shapes | Channel protocol fault taxonomy |
| Relational interaction declaration | `cm3-lifecycle-contract` | Channel/Portable Binding → CM3 | edge, direction, members, Operation, Capability, Shape | Channel inference |
| Ordinary interaction eligibility | `portable-binding` | Channel → explicit Release predicate | member/binding release observation | successful establishment, Composition |
| Payload compatibility/projection | `shape-contract` | Channel/profile → Architecture 0.8 Shape rules | canonical Shape/Fragment refs and position classification | authority evaluator |
| Payload/resource representation | `resource-profile` | Channel carries declared positions → profile | representation/resource descriptor | Channel core |
| Resource ownership/lifetime/release/fallback | `resource-profile` | Channel loss/terminal observations → resource owner | profile-specific resource observation and cleanup result | Channel session close |
| Intra-domain Capability evaluation | `authority-domain` | Channel dispatch → local authority result | recognized presentation plus attributable result | Channel compatibility |
| Cross-trust admission and local grants | `authority-domain` | Channel carries attributable evidence → local admission | no-capability-transfer mode, exact designations, admission result | sending peer, Component Management |
| Cross-domain identity and attestation | `identity-facet` | Channel profile may require facet → extension | versioned evidence/admission facet | Channel core, Distributed transport |
| Local pre-dispatch refusal | `local-authority-boundary` | Channel observation consumes | local refusal observation | peer wire protocol |
| Semantic failure | `operation-contract` | Channel carries exact Outcome from responding Actor | shaped failed Outcome | protocol fault |
| Peer protocol fault | `channel` | local observer consumes peer assertion | scoped fault category plus bounded diagnostics | transport/process observer |
| Transport/process loss classification | `local-realization` | Channel observation consumes | local loss category and detection point | peer |
| Effect certainty | `channel` | observations/extensions → Channel certainty | known-none / known(details ref) / unknown(reason) | adapters guessing zero |
| Profile-owned effect details | `application-profile` | Channel observation references profile evidence | exact profile details reference | Channel certainty form |
| Retry attempt policy | `retry-profile` | Channel admits each attempt independently | new interaction id plus optional causal prior reference | reuse/replay of one id |
| Delivery, persistence, ordering | `delivery-facet` | Channel profile may require facet → extension | exact extension facet/version | Channel core |
| Streaming and backpressure | `flow-facet` | Channel profile may add interaction class/facet → Flow | stream identity subordinate to interaction, terminal bridge | unary core reinterpretation |
| Long-running activity | `lifecycle` | Channel Outcome may identify/start activity under exact extension | activity reference and lifecycle facet | keeping interaction forever nonterminal |
| Timing constraints | `realtime-facet` | Channel observes declared timing facts → Realtime | explicit timing facet and clock provenance | ambient Channel clock |
| Logs, metrics, traces, storage | `observability-system` | consume Channel observations | non-normative local projection | Channel semantics |

## Selected boundary rulings

### Session state versus activation phase

Channel owns only `unestablished`, `establishing`, `established`, `draining`, `closed`, and `faulted`.
Interconnection, Relational Initialisation, Ready, and Release are external phase facts. This prevents
the 0.1 defect in which readiness was simultaneously a wire event, a binding state, and an activation
claim.

Their exact owners are not collective: Portable Binding owns Interconnection, Release, binding
withdrawal, and binding cleanup; Composition owns the Relational Initialisation phase; Component
Management owns Ready. Each crossing artifact in the matrix carries one of those facts without
sharing its semantic ownership.

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
