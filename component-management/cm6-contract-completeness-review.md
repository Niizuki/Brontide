# CM6 contract-completeness review

Status: complete phase-boundary absence audit

Reviewed contract: [CM6 independent-comparison capability contract](./cm6-capability-contract.md)

Designed for: Brontide Architecture 0.8 §18.1 and §24, Complete Draft, not ratified

This review asks what the CM6 contract failed to say before treating cross-process parity as
complete. It is separate from conformance testing: passing the written vectors cannot expose a
question that the contract never asks.

## Closed findings

| Silence found | Required disposition |
| --- | --- |
| Outcome-only comparison could hide different denial reasons or admitted effects. | C3 requires the complete CM5 observation, including every decision, effect, policy mistake, and decision-log entry. |
| Native serializers could agree semantically but differ on incidental ordering or formatting. | C4 defines one canonical semantic profile with fixed property order, invariant timestamps, explicit nulls, and identity-sorted arrays. |
| Provider identity would necessarily differ and could either break comparison or be silently erased. | C3 keeps implementation identity in the response envelope but outside the parity profile. |
| A stream protocol without a per-message version could drift after the fixture was loaded. | C5 and C6 require every scenario and response to carry schema version 1 and reject unsupported versions. |
| Duplicate scenario identities could make diagnostics and comparison attribution ambiguous. | Both fixture loaders reject duplicate scenario identifiers before execution. |
| A misspelled expected outcome could become an inert annotation. | Both fixture loaders and endpoints reject unknown expected-outcome tokens; native tests compare every declared outcome with the computed profile. |
| Unbounded lines could turn the comparison endpoint into an accidental resource sink. | C5 limits each input line to 1,048,576 characters, enforced before JSON parsing and covered by both native suites. |
| Structurally invalid CM5 data could be confused with malformed protocol data. | C6 distinguishes protocol errors from a well-formed CM5 `invalid-request` profile. |
| Stateful endpoints could make a valid scenario depend on earlier lines. | C5 requires one ordered response per line and no cross-request state; multi-line tests exercise the property in each stack. |
| One host direction could accidentally compare a stack with itself. | C7 requires both Reference-host-to-Minimal-provider and Minimal-host-to-Reference-provider tests and checks the foreign identity. |
| Ordinary native test runs may skip a missing foreign provider. | The repository interchange gate supplies both built provider paths and runs both CM6 `CrossProcess` tests explicitly. |
| Stable fixture order could conceal enumeration-order dependence. | C10 requires permutation stability; each suite reverses semantically unordered authority collections and compares the complete profile. |
| A shared helper could make apparent agreement non-independent. | C1 and C2 restrict shared material to data and serialized contracts; dependency guards prohibit cross-stack project or assembly references. |
| Equal outputs could be overstated as proof of contract completeness or real interchange. | C9 limits the claim to agreement on eight deterministic fake scenarios and records the blind spot of shared contract silence. |

## Deliberately outside CM6

CM6 does not establish real Component interchange, general substitutability, architecture
conformance, contract completeness, cryptographic evidence authenticity, trust-root management,
cross-domain Capability transport, federation, production policy safety, adversarial process
hardening, or production activation effects. A future real-integration plan must define those
capabilities rather than widening this harness's evidence claim.

## Result

No unresolved CM6 contract-silence finding remains. The contract now says what is compared, what
must be canonical, how processes fail, how both directions are proven, and exactly what equal
profiles do and do not establish.
