# CM6 independent-comparison capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1 and §24, Complete Draft, not ratified

CM6 runs common external authority-admission scenarios through the native CM5 implementation in
each stack and compares complete normalized observations across a real process boundary. The
Reference and Minimal stacks independently parse the neutral scenario, construct their own typed
requests, execute their own evaluator, and project their own comparison profile.

CM6 proves agreement on the tested fake model only. It is not Component interchange, cryptographic
verification, cross-domain Capability transport, federation, or security evidence.

## C1 — the fixture is complete data, not shared behavior

Every scenario carries a complete CM5 request, evidence set, relationship and authority requests,
local policy, evaluation instant, and expected outcome category. It contains no patch language,
defaults, evaluator instructions, implementation type names, or algorithm.

Property: deleting either native implementation leaves no executable comparison behavior in the
shared fixture tree.

## C2 — each stack reconstructs its own typed request

Each endpoint strictly parses strings at the serialization seam into its own Actor, evidence,
relationship, Capability, target, Operation, scope, policy, rule, local-reference, and request
identity types. Neither endpoint references the other stack or consumes the other's CLR values.

Property: every identity crossing the boundary is a primitive in JSON and becomes a stack-native
strong type before semantic evaluation.

## C3 — comparison covers the complete CM5 observation

The comparison profile includes the outcome and failure, request, policy and evaluation instant,
every evidence, relationship, and authority decision, established Actor relationships, local
Capability grants, policy-mistake findings, and the deterministic decision log. Implementation
identity is reported outside the profile and is not a parity field.

Property: no CM5 effect or denial may be omitted from parity merely because both outcome categories
match.

## C4 — normalization is canonical and semantic

Profiles use one versioned JSON shape, fixed property order, invariant timestamps, lower-case
enumeration tokens, explicit nulls, and identity-sorted arrays. Native collection order,
language-specific union or enum representation, exception type, and serializer defaults are not
observable comparison semantics.

Property: semantically equal observations produce byte-identical profile JSON, while any differing
CM5 field changes the profile.

## C5 — the process seam is bounded and deterministic

The endpoint uses UTF-8 JSON Lines: one non-empty scenario object of at most 1,048,576 characters
per input line and exactly one response per line. Standard output carries protocol responses only.
The endpoint remains stateless between lines, flushes every response, and terminates successfully
at clean end-of-input.

Property: N valid input lines produce exactly N responses in the same order with no cross-request
state.

## C6 — protocol failure is separate from CM5 refusal

Malformed JSON, an unknown schema version, unknown or missing fields, and unknown enumeration tokens
produce a versioned `protocol-error` response and do not invoke CM5. A structurally invalid but
well-formed CM5 request produces the ordinary `invalid-request` CM5 profile instead.

Property: no protocol failure is serialized as `denied`, `invalid-request`, or another semantic
CM5 outcome.

## C7 — both process directions are evidence

A Reference host compares its native profiles with a Minimal provider process for every scenario,
and a Minimal host independently compares its native profiles with a Reference provider process.
Each test records the foreign implementation identity and verifies it differs from the host.

Property: CM6 cannot pass solely through either stack talking to itself.

## C8 — required scenario breadth

The common inventory includes an accepted narrow grant, a request with no local mapping, mixed
partial admission, malicious unlimited authority, revoked evidence, expired evidence, an
attributable mistaken local allow, and a structurally invalid request.

Property: every CM5 outcome category and every mandatory CM5 security-boundary case appears in at
least one process comparison.

## C9 — bounded claims remain explicit

Equal profiles establish only that the independent implementations agree for these deterministic
fake scenarios. The comparison does not prove the CM5 contract complete, the architecture
conformant, the policies wise, the evidence authentic, or the implementations generally
substitutable.

Property: every CM6 status and handoff statement preserves this limitation.

## C10 — complete deterministic explanation

Every response identifies the protocol schema, provider implementation, scenario, and exactly one
of a CM5 profile or protocol error. Fixtures and responses are immutable snapshots, and repeated
evaluation of the same line is byte-identical apart from the deliberately excluded implementation
identity.

Property: equal semantic input produces equal complete profiles across repetition, enumeration
permutation, and both process directions.

## Structured protocol results

CM6 returns exactly one of:

- `profile`, carrying one CM5 `admitted`, `partially-admitted`, `denied`, or `invalid-request`
  observation; or
- `protocol-error`, carrying `malformed-json`, `unsupported-schema`, `invalid-envelope`, or
  `unknown-token`.
