# BRONTIDE

## Draft Channel Contract 0.1

**Status:** Draft semantic contract for Architecture 0.8 evidence; not ratified, not Brontide Base,
and not an implementation or wire-format claim.  
**Design source:** [Channel Design Note 0.1](./Brontide-Design-Note-Channel-0.1.md).  
**Decision ledger:**
[Architecture 0.8 Channel requirements and risk ledger](./architecture-0.8-channel-requirements-and-risk-ledger.md).  
**Vectors:** [Channel 0.1 conformance vectors](../../../conformance/channel-0.1-vectors.json).

This draft fixes the semantic questions required before the Portable Component Binding can choose
an encoding. Names below identify logical Shapes and fields; they do not reserve a serialized
spelling, numeric tag, media type, or package identifier. A realization maps its representation to
these meanings and declares that mapping.

## 1. Contract and envelope

A Channel realization declares one contract version, its compatibility model, supported message
kinds, correlation fields, authority-presentation mode, hardening dimensions, and delivery
limitations before an effect can begin. Every message is one complete frame and has one logical
`Brontide:Channel.Envelope` version 1 with:

| Position | Requirement |
| --- | --- |
| `contract-version` | Positive integer; exact recognition is required. An unknown value fails closed. |
| `kind` | Exactly one of `negotiation`, `request`, `outcome`, `protocol-error`, or `lifecycle`. |
| `correlation` | A `Brontide:Channel.Correlation` value as defined below. |
| `body` | A choice whose selected alternative exactly matches `kind`. Unknown alternatives fail closed. |

The body alternatives are logical version-1 Shapes:

- `Negotiation`: contract identity, offered/selected version, and declared realization features;
- `Request`: Operation identity, input payload, target/resource designation when present, and one
  boundary-relative authority presentation;
- `Outcome`: `succeeded` with `result`, or `failed` with structured `details`, never both;
- `ProtocolError`: one standard category, optional realization code, non-sensitive diagnostic
  text, and the detecting failure domain; and
- `Lifecycle`: a realization-declared session action. An unknown action fails closed.

Negotiation is optional. A statically fixed contract satisfies the same pre-invocation rule without
a negotiation message. Denial is not an envelope kind.

## 2. Correlation

`Brontide:Channel.Correlation` version 1 contains distinct opaque identifiers:

| Field | Presence | Meaning |
| --- | --- | --- |
| `channel` | Required on every frame | One logical Channel exchange/session. |
| `request` | Required on `request` and its `outcome`; required on request-bound `protocol-error` | One request/terminal-response pair. |
| `execution` | Optional | A finer Channel-scoped attempt identity. |
| `occurrence` | Optional | A finer Channel-scoped occurrence identity. |

Identifiers compare by exact octet sequence after decoding their declared portable scalar form. A
realization declares its maximum length and generation rules. Every carried identifier on an
Outcome must equal the corresponding request value. Extra, missing, or mismatched carried
identifiers produce `correlation-mismatch` and the claimed Outcome is not accepted.

These identifiers are Channel identities only. They MUST NOT be accepted as an Actor, Capability,
Execution, Occurrence, Activity, or authority-domain identity. A host may record a mapping to its
own identity, but never identity equivalence across the boundary.

## 3. Standard protocol-error categories

The `category` field is normative. A realization MAY additionally expose a local `code`, but must
map that code to exactly one category and must not require a peer to interpret it.

| Category | Meaning |
| --- | --- |
| `malformed-message` | The frame cannot be decoded as the declared envelope or violates required field structure. |
| `unsupported-version` | The contract or envelope version is not recognized. |
| `unsupported-contract` | The named contract or negotiated manifest is not supported. |
| `unsupported-kind` | The message/body/lifecycle discriminator is not supported in the current contract. |
| `unsupported-operation` | A request names an Operation outside the established contract. |
| `correlation-mismatch` | Required correlation is missing, extra where forbidden, or unequal. |
| `invalid-payload` | A payload-plane value fails its declared Shape or allowed projection. |
| `invalid-authority-presentation` | An authority/control position is absent, unrecognized, broadened, or otherwise fails closed. |
| `replay-detected` | A realization that declares replay protection rejects a repeated request identity. |
| `limit-exceeded` | A declared frame, payload, depth, field-count, or other parse bound is exceeded. |
| `state-violation` | A recognized message is not legal in the current Channel lifecycle state. |
| `internal-protocol-failure` | The endpoint cannot continue protocol processing without exposing a foreign exception or type. |

Unknown category values are themselves `unsupported-kind`. Semantic domain errors such as
`resource-refused` belong in a failed Outcome's structured `details`; they are not protocol-error
categories. A receiver-side authority denial remains a local boundary decision and emits no frame.

## 4. Process failure and failure domains

A process/transport failure is an observation made when no valid semantic Outcome is available. It
uses one of these local observation categories: `transport-unavailable`, `transport-interrupted`,
`timeout`, `peer-terminated`, `peer-unavailable`, `resource-exhausted`, or `unknown`. `unknown` is
permitted only when the observer records why it cannot attribute a narrower category.

Every protocol or process failure is recorded relative to the observer with exactly one failure
domain:

| Failure domain | Boundary |
| --- | --- |
| `local-endpoint` | Local framing, codec, contract, correlation, or Channel state before/after transport. |
| `transport` | The declared duplex transport or frame transfer. |
| `remote-endpoint` | A protocol error explicitly reported by the peer Channel endpoint. |
| `remote-provider` | The peer endpoint attributes loss to its provider/process beyond protocol handling. |
| `unknown` | The available evidence cannot distinguish the above; the uncertainty is retained. |

The vocabulary is relative, not a claim about global topology. It never converts a semantic failed
Outcome into a process failure or a denial into a wire observation. No exception, stack trace,
runtime type name, or authority object crosses in any failure form.

## 5. Two-plane position classification

Every logical position is classified before a realization is accepted:

| Position | Plane / variance | Rule |
| --- | --- | --- |
| contract version, message kind, correlation, lifecycle action | Boundary control; contravariant/fail-closed | Exact recognition; never projected. |
| negotiation contract and feature declarations | Boundary control; contravariant/fail-closed | Unknown required features reject before invocation. |
| request Operation identity and target/resource designation | Authority/control; contravariant/fail-closed | Exact recognized designation; addressing never grants authority. |
| request input | Payload; covariant | Additive Shape projection under Architecture 0.8 §16.4 is permitted. |
| intra-domain Capability presentation | Authority; contravariant/fail-closed | Exact evaluatable representation; never payload-projected. |
| cross-trust attributable context | Authority/control; contravariant/fail-closed | Must declare `no-capability-transfer`; unknown structure rejects. |
| Outcome status and terminal correlation | Boundary control; contravariant/fail-closed | Exact status and matching correlation. |
| successful `result` and failed `details` | Payload; covariant | Independently shaped; additive projection is permitted. |
| protocol-error category and failure domain | Boundary control; contravariant/fail-closed | Exact recognized category/domain. |
| diagnostic message and realization code | Payload/diagnostic; covariant and non-normative | May be retained or redacted; never drives portable semantics. |

Constraint values in an authority presentation use the Architecture 0.8 C8 polarity exemption:
they are not payload-projected. An unrecognized newer value is `Unknown` for structural
three-valued evaluation. The C7 rules then decide the complete expression; `Unknown` standing alone
or in a non-decisive branch denies, while an authored decisive `True` fallback may authorize.

## 6. Authority and failure invariants

- Within one authority domain, a request may carry the domain's declared Capability presentation
  for target-side evaluation.
- Across a trust boundary, no Capability crosses. Attributable context and addressing may cross,
  and the receiving domain performs its own admission.
- A local denial begins no far-side effect and emits no Channel message.
- A failed semantic Outcome proves the far side produced an Outcome; its `details` are payload.
- A protocol-error proves only that an endpoint rejected protocol processing.
- A process failure proves no semantic Outcome. Interruption, retry count, and fallback are recorded
  as facts; success is never fabricated.
- Channel 0.1 promises no delivery, ordering, exactly-once execution, retry, cancellation,
  streaming, or long-running lifecycle semantics.

## 7. Conformance boundary

The data-only vector set names identical category-level observations for Reference and Minimal. A
stack adapter may translate local codes, identities, and terminal-status names, but expected
categories, frame/no-frame decisions, correlation rules, and two-plane behavior must remain equal.

The draft is ready for Portable Binding implementation evidence when:

1. both stacks execute every vector independently without shared semantic runtime logic;
2. the C7 strong-Kleene and C8 polarity-flip vectors run at the Channel authority boundary;
3. one fixed-contract and one negotiated realization map their local codes to this taxonomy;
4. direct-call and process-isolated realizations report the same semantic observations; and
5. the architecture review either ratifies these provisional Shape/category names or replaces them
   with an explicitly migrated revision.

Until those gates pass, this document resolves planning questions but makes no Channel conformance
or ratification claim.
