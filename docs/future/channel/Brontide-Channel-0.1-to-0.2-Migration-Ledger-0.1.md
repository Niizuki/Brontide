# Channel 0.1 to 0.2 migration ledger 0.1

Date: 2026-08-11

Status: proposed first-batch migration disposition; B4, N1/N3, F3, and D5 corrected after independent
review and subject to fresh independent totality closure review. Serialized spellings remain
unselected until the neutral contract batch.

Sources inventoried:

- [Draft Channel Contract 0.1](./Brontide-Draft-Channel-Contract-0.1.md);
- [`conformance/channel-0.1-vectors.json`](../../../conformance/channel-0.1-vectors.json);
- [`channel-envelope.json`](../../../binding/portable/schemas/channel-envelope.json);
- [`limits-and-lifecycle.json`](../../../binding/portable/schemas/limits-and-lifecycle.json); and
- [`binding-observation.json`](../../../binding/portable/schemas/binding-observation.json).

Disposition meanings are those in the redesign plan: **retained**, **replaced**, **moved**,
**removed**, and **legacy-only**. “Retained” means semantic identity, not necessarily serialized
spelling.

## Migration rules

1. Channel 0.1 evidence files, golden encodings, attestations, and pins remain unchanged.
2. Channel 0.2 uses a distinct contract/profile version and does not accept 0.1 frames implicitly.
3. A dual-version host uses separate decoders and state machines.
4. An adapter projects a 0.1 fact only where this ledger records identical meaning. Missing 0.2 facts
   remain unknown/unavailable.
5. Moved fields keep their meaning but acquire the named semantic owner. Carrying a field does not
   return ownership to Channel.

## Logical Shapes and fields

| Channel 0.1 item | Disposition | Channel 0.2 location and rationale |
| --- | --- | --- |
| `Brontide:Channel.Envelope` | **replaced** | A bounded frame carries an exact Channel version, session identity, and one declared message class. Session control, interaction, peer fault, and local observation no longer share one omnibus body union. |
| `Envelope.contract-version` | **retained** | Channel contract version in every transmitted Channel frame; establishment also fixes one immutable profile version. |
| `Envelope.kind` | **replaced** | Exact message class within `session-control`, `interaction`, or `peer-protocol-fault`; unknown classes have their own fault classification. |
| `Envelope.correlation` | **replaced** | Session identity is common; interaction identity is required only for interaction-scoped messages. Attribution context is not correlation. |
| `Envelope.body` | **replaced** | Class-specific bounded structure; no generic choice whose lifecycle and protocol meanings share one namespace. |
| `Brontide:Channel.Correlation` | **replaced** | `SessionIdentity` plus subordinate `InteractionIdentity`, each strongly distinct. |
| `Correlation.channel` | **retained** | Renamed semantically to session identity; still opaque and exact. |
| `Correlation.request` | **retained** | Renamed semantically to interaction identity; scopes all controls/terminal facts for one interaction. |
| `Correlation.execution` | **moved** | Optional attributable context owned by the initiating host/profile; never Channel correlation. |
| `Correlation.occurrence` | **moved** | Optional attributable context owned by the profile/Composition; never Channel correlation. |
| `Negotiation` | **replaced** | Exact establishment proposal and acceptance/refusal producing one immutable profile. Fixed validation produces the same record without wire mechanics. |
| negotiation contract identity | **retained** | Exact Channel profile and application contract references. |
| offered/selected version | **replaced** | Exact proposal plus accepted version; no implicit “selected compatible” downgrade. |
| realization feature declarations | **replaced** | Required/optional profile facets with exact identity/version and additive-optional rule. |
| `Request` | **replaced** | `InteractionRequest` with exact class, direction, Operation/input positions, external phase predicate evidence, and authority mode. |
| request Operation identity | **retained** | Exact canonical Operation reference owned by the profile/Operation contract. |
| request input payload | **retained** | Payload-plane Shape position with Architecture 0.8 projection. |
| request target/resource designation | **moved** | Exact designation carried by the interaction but owned by Portable Binding/resource profile. Addressing remains non-authority. |
| request authority presentation | **replaced** | Boundary-relative mode plus exact local authority decision; no cross-trust Capability form. |
| `Outcome` | **retained** | `InteractionOutcome`, always interaction-scoped and one of success, shaped failure, or supported cancellation. |
| Outcome `succeeded` / result | **retained** | Semantic success with shaped result. |
| Outcome `failed` / details | **retained** | Semantic failure with shaped details, distinct from peer fault/loss. |
| Outcome `cancelled` | **replaced** | The 0.1 Outcome terminal set is replaced by a profile-declared set that may include semantic cancellation; cancellation acknowledgement alone is not terminal. |
| `ProtocolError` | **replaced** | `PeerProtocolFault`, explicitly a bounded peer assertion with session or interaction scope. |
| protocol category | **replaced** | Revised peer-fault taxonomy below; unknown fault category becomes local `unrecognized-peer-fault` and does not trigger a reply loop. |
| realization/local code | **retained** | Bounded, non-normative diagnostic excluded from portable semantics. |
| diagnostic text | **retained** | Bounded, sanitized, non-normative; no exception/type/authority object. |
| detecting failure domain | **replaced** | Peer role/provenance is carried by the peer fault; local detection point belongs to local observation. No field claims global topology. |
| `Lifecycle` | **removed** | The generic logical body is split. Establish/drain/close are session controls. Relational initialization is an interaction class. Component Ready/Release remain external facts. |
| lifecycle action discriminator | **replaced** | Exact session-control class or profile interaction class. |

## Portable Binding 0.1 realization message kinds

| 0.1 kind | Disposition | Channel/Portable Binding 0.2 mapping |
| --- | --- | --- |
| `establish` | **replaced** | Channel establishment proposal. |
| `establish-accepted` | **replaced** | Channel establishment acceptance with exact immutable profile. |
| `ready` | **moved** | readiness report carried by Portable Binding and semantically owned by Component Management; never a Channel session transition and never inferred from acceptance. |
| `request` | **replaced** | Channel interaction request with declared class. |
| `outcome` | **retained** | Channel interaction Outcome with exact interaction identity. |
| `protocol-error` | **replaced** | Peer protocol fault with explicit scope and provenance. |
| `withdraw` | **replaced** | Binding withdrawal remains Portable Binding; it may initiate Channel drain. Channel core has no generic binding withdrawal message. |
| `terminate` | **replaced** | Orderly Channel close after drain; Component/process termination remains externally owned. |
| Cooling `denial` | **legacy-only** | Retained fixture evidence only. Local denial remains frameless in 0.2. |
| process failure as a would-be message | **removed** | Local observation only; no Channel frame. |

## Session and interaction states

| 0.1 state | Disposition | 0.2 state/owner |
| --- | --- | --- |
| `unestablished` | **retained** | Channel session `unestablished`. |
| `establishing` | **retained** | Channel session `establishing` for negotiated mechanics only. |
| `established` | **retained** | Channel session `established`; admits interactions subject to class/phase/authority. |
| `ready` | **moved** | Component Management external Ready fact; not Channel session state. |
| `active` | **replaced** | Per-interaction initiator `dispatched` and recipient `executing`; several may coexist. |
| `withdrawn` | **replaced** | Binding withdrawal externally owned; Channel session `draining` refuses new interactions while preserving in-flight histories. |
| `terminated` | **replaced** | Channel session `closed`; external Component/process termination remains separate. |
| `failed` | **replaced** | Channel session `faulted`; each nonterminal interaction separately becomes peer-fault or lost with effect evidence. |

New 0.2 session state: `draining`. New interaction states are specified in the interaction state
machine and have no 0.1 global-state equivalent.

## Protocol-fault category migration

The labels below are logical design names, not serialized spellings.

| 0.1 category | Disposition | 0.2 category/handling |
| --- | --- | --- |
| `malformed-message` | **retained** | Peer `malformed-message` when a bounded frame reached the peer but cannot satisfy the declared message structure; local decode failure remains separately observed. |
| `unsupported-version` | **replaced** | `unsupported-channel-version`; profile/application version mismatches are separated below. |
| `unsupported-contract` | **replaced** | `unsupported-profile` or `unsupported-application-contract`, so Channel capability and domain contract mismatches are not conflated. |
| `unsupported-kind` | **replaced** | `unsupported-message-class`. An unknown peer-fault category is not this fault; it faults locally without a reply loop. |
| `unsupported-operation` | **retained** | Interaction-scoped peer fault before handler dispatch. |
| `correlation-mismatch` | **replaced** | `invalid-interaction-correlation`, including missing, extra, wrong-session, reused, or mismatched identities as the detailed reason. Replay remains separately classified when reuse is known. |
| `invalid-payload` | **retained** | Interaction-scoped, before handler dispatch. |
| `invalid-authority-presentation` | **retained** | Interaction-scoped, before handler dispatch; no authority projection. |
| `replay-detected` | **retained** | Same session/interaction identity already accepted; no redispatch. |
| `limit-exceeded` | **retained** | Scope identifies session establishment, frame, interaction, or declared profile bound. |
| `state-violation` | **retained** | Scope identifies session versus interaction; external phase refusal may be local frameless or peer fault depending where detected. |
| `internal-protocol-failure` | **replaced** | `internal-channel-failure`, sanitized and scoped; never a runtime exception transport. |

New local-only classification: `unrecognized-peer-fault`. It records that the incoming peer-fault
category cannot be interpreted, faults the local session, and sends no fault in response.

## Local loss category migration

| 0.1 process category | Disposition | 0.2 local observation |
| --- | --- | --- |
| `transport-unavailable` | **retained** | Local transport was unavailable for required transfer. Pre-dispatch versus post-dispatch is recorded separately. |
| `transport-interrupted` | **retained** | Local transfer began and did not complete. |
| `timeout` | **retained** | Local declared timer expired; timer owner/provenance is required. |
| `peer-terminated` | **replaced** | `peer-closed` when the connected peer end is observed closed. It proves neither provider identity nor global process cause. |
| `peer-unavailable` | **moved** | Launcher/Portable Binding observation before a Channel session exists. Channel given no connected peer has no session fact to report. |
| `resource-exhausted` | **retained** | Local bounded Channel/realization resource failure, with stage and no runtime exception crossing. |
| `unknown` | **retained** | Requires a reason the narrower local category/detection point is unavailable. |

## Failure domain migration

| 0.1 failure domain | Disposition | 0.2 provenance |
| --- | --- | --- |
| `local-endpoint` | **replaced** | Local observation with exact detection point: establishment validator, frame decoder, session machine, interaction admission, or terminal validator. |
| `transport` | **retained** | Local observation detection point `transport`; still observer-relative. |
| `remote-endpoint` | **replaced** | Valid peer protocol fault from the established peer role. It is a peer statement, not a guessed remote domain. |
| `remote-provider` | **moved** | Profile-owned peer/provider attribution where explicitly reported and admitted; Channel core records peer provenance and does not infer topology behind it. |
| `unknown` | **retained** | Unknown local detection/attribution with mandatory reason. |

## Limit migration

| 0.1 limit | Disposition | 0.2 owner/location |
| --- | --- | --- |
| `maxFrameBytes` | **retained** | Realization profile, accepted during C1 establishment and enforced before allocation/semantic use. |
| `maxNestingDepth` | **retained** | Representation/Shape decoder profile; finite and accepted at establishment. |
| `maxRecordFields` | **retained** | Representation/Shape decoder profile. |
| `maxFragmentsPerRecord` | **retained** | Shape representation profile. |
| `maxSequenceItems` | **retained** | Shape representation profile. |
| `maxTextBytes` | **retained** | Representation profile. |
| `maxByteStringBytes` | **retained** | Representation profile. |
| `maxResourceBytes` | **moved** | Portable Binding/resource profile; must fit the selected frame/representation limits but is not Channel core. |
| `ioTimeoutMilliseconds` | **moved** | Local transport/host profile with clock provenance; expiration becomes a local observation. |
| `maxConcurrentRequests` | **replaced** | Channel profile `max-in-flight`, finite positive and supported by core concurrency semantics. |

The 0.1 numeric values remain legacy profile evidence. Channel 0.2 does not adopt them universally;
the new profile declares finite values and the neutral vectors test their consistency.

## Feature migration

| 0.1 feature | Disposition | 0.2 treatment |
| --- | --- | --- |
| establishment | **retained** | C1 profile establishment. |
| readiness signal | **moved** | Component Management fact, separate from Channel establishment; Portable Binding may carry its observation. |
| single invocation | **replaced** | Finite declared `max-in-flight`; a profile may still choose 1. |
| clean withdrawal | **replaced** | Binding withdrawal externally; Channel drain controls new admission. |
| clean termination | **replaced** | Channel orderly close after drain. |
| retry unsupported | **replaced** | Channel core makes no retry promise; exact Distributed/host facet may create a new attempt with a new identity. |
| cancellation unsupported | **replaced** | Optional Channel core cancellation contract; profile declares support/requirement per class. |
| streaming unsupported | **retained** | Retained as a non-promise: Flow/profile facet only; unary core is not reinterpreted. |
| ordering guarantee unsupported | **retained** | Retained as a non-promise: no cross-interaction order; extension facet required. |
| exactly-once unsupported | **retained** | Retained as a non-promise: replay protection remains distinct from exactly-once effects. |

## Observation-field migration

| 0.1 observation field | Disposition | 0.2 owner/location |
| --- | --- | --- |
| `selectedProvider` | **moved** | Portable Binding profile observation. |
| `selectionReason` | **moved** | Composition/Portable Binding observation. |
| `negotiatedOperations` | **replaced** | Established profile's exact application contract and interaction-class declarations. |
| `negotiatedContractVersion` | **replaced** | Separate Channel version, profile version, and application contract version. |
| `representation` | **moved** | Realization/Portable Binding profile observation. |
| `crossedBoundaries` | **moved** | Local realization observation; not a peer or global-topology claim. |
| `copyCount` | **moved** | Representation/Portable Binding observation. |
| `referencedResources` | **moved** | Portable Binding/resource observation. |
| `authorityDecisionPoint` | **retained** | Local exact decision point and authority-domain mode. |
| `authorityDecision` | **retained** | Local permitted/denied/unknown result; only permitted dispatches. |
| `mappingObligations` | **moved** | Shape/profile adapter observation. |
| `retryCount` | **moved** | Distributed/host attempt policy. Channel records optional causal prior identity, not a retry count it did not own. |
| delivery `fallback` | **moved** | Delivery/retry facet observation; exact `none` remains a valid attributable value and Channel core does not infer another attempt. |
| `interrupted` | **replaced** | Local loss category/detection point and interaction terminal provenance. |
| `failureDomain` | **replaced** | Peer-statement provenance or local detection point, never one ambiguous domain field. |
| `terminalStatus` | **replaced** | Discriminated provenance: local refusal, semantic Outcome, peer protocol fault, or local loss. |
| `correlationMapping.channelId` | **retained** | Session identity. |
| `correlationMapping.requestId` | **retained** | Interaction identity. |
| `correlationMapping.hostNativeExecution` | **moved** | Optional local attribution mapping; excluded from Channel identity. |
| `providerEffectCount` | **moved** | Portable Binding/domain effect details. Channel owns `known-none` / `known(details-ref)` / `unknown(reason)`. |
| `timing.establishedAtElapsedMilliseconds` | **moved** | Non-normative local timing with clock provenance. |
| `timing.requestElapsedMilliseconds` | **moved** | Non-normative local timing with clock provenance. |
| `localCode` | **retained** | Bounded non-normative diagnostic. |
| `localMessage` | **retained** | Bounded sanitized non-normative diagnostic. |

### Resource observation subfields

`flavor`, `ownership`, `copies`, `integrityVerified`, and `accepted` are all **moved** to the Portable
Binding/resource profile. Channel observes the interaction terminal/loss boundary but does not claim
resource cleanup or integrity by implication.

## Channel 0.1 vector migration

| 0.1 vector | Disposition | 0.2 evidence target |
| --- | --- | --- |
| CH-01 correlation echo | **replaced** | C4 exact session/interaction correlation plus distinct local Execution attribution. |
| CH-02 correlation mismatch | **replaced** | C4 invalid terminal correlation after dispatch, effect certainty unknown unless narrowed. |
| CH-03 unsupported version | **replaced** | C1 separates Channel/profile/application versions and refuses downgrade pre-effect. |
| CH-04 unsupported contract | **replaced** | C1 unsupported profile and unsupported application contract vectors. |
| CH-05 unknown kind | **replaced** | C9 unsupported message class; unknown peer-fault category gets separate no-loop vector. |
| CH-06 malformed message | **replaced** | C5 bounded structural refusal plus local-versus-peer provenance. |
| CH-07 payload covariance | **retained** | C5 Architecture 0.8 payload projection. |
| CH-08 authority no projection | **retained** | C5/C6 authority/control position refuses without projection. |
| CH-09 strong-Kleene fallback | **retained** | C6 exact local authority evaluation. |
| CH-10 strong-Kleene unknown denies | **retained** | C6 frameless local denial and known-none. |
| CH-11 no Capability transfer | **retained** | C6 cross-trust mode; forbidden bytes never leave sender. |
| CH-12 denial is not a frame | **retained** | C6/C9 local refusal provenance. |
| CH-13 semantic failed Outcome | **retained** | C8/C9 shaped failure distinct from peer fault/loss. |
| CH-14 protocol code mapping | **retained** | C9 normative peer-fault category, non-normative local code. |
| CH-15 replay declared | **replaced** | C4 session-scoped accepted identity; no redispatch under concurrency. |
| CH-16 declared limit | **replaced** | C5 profile-negotiated finite bounds and no partial interaction. |
| CH-17 peer terminated | **replaced** | C9 local peer-closed/loss observation and C10 unknown effects after dispatch. |
| CH-18 foreign runtime data forbidden | **retained** | C5/C9 sanitizer and positional classification. |
| CH-19 unsupported Operation | **retained** | C3/C5 peer/local pre-dispatch refusal. |
| CH-20 invalid payload | **retained** | C5 pre-dispatch known-none. |
| CH-21 state violation | **replaced** | C2 session illegal transition and C3/C8 interaction/phase illegal transition vectors. |
| CH-22 internal protocol failure | **replaced** | C9 internal-channel-failure without runtime leakage. |
| CH-23 process failure categories | **replaced** | C9 local loss categories only; peer-unavailable moves to launcher profile. |
| CH-24 failure-domain relativity | **replaced** | C9 peer-statement provenance versus local detection point; no global topology claim. |

No 0.1 vector is deleted. A retained vector is re-expressed under the 0.2 neutral shape. A replaced
vector may map to one or several successor vectors; each keeps the original vector identifier as
provenance metadata rather than reusing it as a 0.2 identity.

## New evidence required by redesign

The 0.1 set has no direct equivalents for these required 0.2 cases:

- fixed/negotiated full-profile equivalence;
- drain with concurrent in-flight interactions;
- out-of-order concurrent Outcomes;
- cancellation accepted/refused versus terminal Outcome;
- unknown peer-fault category without a response loop;
- relational interaction exact declaration, direction, authority, and pre-Ready phase;
- ordinary interaction before Release refusal;
- session fault mapping each in-flight interaction separately;
- extension facet unable to redefine authority or terminality; and
- effect certainty separated from profile-owned effect details.

## Golden encodings, parity profiles, and pins

- All six Portable Binding 0.1 golden encodings are **legacy-only** and remain re-derived by the 0.1
  gate. Channel 0.2 authors new goldens only after its neutral schemas exist.
- The 0.1 parity profile remains evidence for 0.1. The 0.2 profile compares session, interaction
  class, admission, authority, terminal provenance, and effect certainty; representation-specific
  copies, timing, and diagnostics remain explicitly excluded.
- PB8 attestations and their reviewed commits remain untouched. They establish predecessor
  conformance, not successor correctness.
- Any future move of a directly or transitively pinned predecessor document requires explicit
  repinning authorization and fresh review.

## Consumer migration obligations

| Consumer | Required migration |
| --- | --- |
| Reference Portable Binding | Add a separate 0.2 adapter/state implementation; keep 0.1 decoder and tests isolated. |
| Minimal Portable Binding | Implement the neutral 0.2 contract natively; do not translate Reference types. |
| Neutral provider | Add an independent 0.2 endpoint with no stack dependency; keep selectable 0.1 endpoint while retained evidence needs it. |
| CM3/CM4 integration | Supply exact external phase and lifecycle declarations; consume relational terminal evidence before recording Ready. |
| Cooling/Catalog fixtures | Remain 0.1 legacy fixtures unless separately migrated; no automatic version projection. |
| Cross-stack harness | Select exact profile/version per run and compare only the matching version's portable observation. |
| Documentation/evidence gates | Report 0.1 retained evidence and 0.2 design/implementation status separately. |

## Ledger completion check

This ledger covers every logical 0.1 envelope/Correlation/body Shape and field, every Portable
Binding 0.1 message kind and lifecycle state, all twelve protocol categories, seven process
categories, five failure domains, ten declared limits, ten lifecycle feature declarations, every
normative/non-normative observation field and resource subfield, all 24 Channel vectors, all six
goldens as a group, and the known consumers. The independent design review must challenge both the
coverage claim and every semantic disposition.
