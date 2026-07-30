# CM1 contract-completeness review

Date: 2026-07-30

Review type: phase-boundary absence audit, separate from conformance and independent attestation

Scope: the CM1 C1-C7 capability contract, neutral fixtures, Reference and Minimal public surfaces,
and both native test suites on PR 15

Result: complete; every finding below is corrected and no unresolved CM1 contract silence remains

This review asks what the contract did not say. It does not claim Architecture 0.8 conformance,
cross-stack interoperability, production distribution security, or independent review.

## C1 — portable discovery query

Finding: the public query carried target, lifecycle, requester, preference, occupied-binding,
Region, Port, Constraint, and topology context without saying whether CM1 filtered or ranked by it.
That let either stack silently implement part of CM2.

Disposition: the contract now states that CM1 carries those fields unchanged and assigns them no
filtering, ranking, compatibility, trust, selection, or authority semantics. Contract identity and
exact provided-contract version are the only CM1 filters. Native tests preserve a populated context
while obtaining the same candidate.

Finding: `AvailableEvidence` was derived from every claim about an artifact and attributed to the
source being queried. The neutral data never said that source supplied the claim.

Disposition: [`cm1-source-evidence.json`](./fixtures/cm1-source-evidence.json) now declares
source-to-evidence availability separately from CM0 evidence content. Both strict native loaders
reject unknown sources, unknown evidence, duplicates, and attribution to a source that does not
advertise a package carrying the subject artifact.

## C2 — deterministic attributable discovery

Finding: the property said “every enumeration permutation,” while each suite tested one reversed
source/advertisement order.

Disposition: both suites now exhaust all six permutations of the three source snapshots and every
per-source advertisement permutation in the retained fixture, comparing complete outcomes. Each
candidate is also checked against the attributed source/package advertisement.

## C3 — staged acquisition

Finding: acquisition computed the artifact digest, but the property test did not independently
recompute the digest from the returned staged content. Policy verdict mapping was observable but
unstated.

Disposition: both suites independently recompute SHA-256 over the staged UTF-8 content. The
contract now states the fake policy's one-item-at-a-time accepted/rejected mapping and keeps it
explicitly non-normative.

## C4 — source disappearance does not mutate staging

Finding: “immutable” did not say whether caller-owned fixture collections could remain aliased by a
Reference source snapshot.

Disposition: Reference snapshots nested fixture collections before use, and its test mutates the
caller's artifact collection after source construction before acquiring. Minimal's persistent lists
make the corresponding aliasing path unavailable. Both then remove the source, preserve the staged
value, refuse later acquisition, and omit the unavailable source from discovery.

## C5 — evidence remains attributable and contestable

Finding: issuer attribution existed, but supplying-source attribution had no neutral observation.
Two implementations could therefore agree on invented provenance.

Disposition: the CM1 source-evidence fixture is now the only source of availability attribution.
Both suites preserve the two contradictory Fabrikam reviews, verify one policy decision per evidence
identity, and match every staged source/evidence pair to the neutral declaration.

## C6 — storefront projection is source-neutral

Finding: the fixture had no local and remote source projecting the same package metadata, so type
identity made the property impossible to falsify.

Disposition: the remote Contoso mirror now carries the same Contoso storefront projection as the
local cache. Both suites compare every projection field after normalising only the attributable
source identity.

## C7 — staging grants no authority and causes no lifecycle effects

Finding: successful staging carried the all-false effect observation, but an acquisition refusal did
not expose it. “Every CM1 result” was therefore stronger than the public surface.

Disposition: Reference `AcquisitionResult` and Minimal `AcquisitionResult` now expose the all-false
effect observation for success and refusal. Both suites assert it for every failure category.

## Residual boundary

CM1 still does not decide candidate compatibility beyond exact provided-contract identity/version,
rank alternatives, interpret preferences or Constraints, aggregate evidence into trust, select or
resolve a Component, construct a Proposed Stack or generation, prepare or activate anything,
establish an Actor, or grant authority. Those are deliberate CM2-or-later boundaries, not silent
CM1 behavior.
