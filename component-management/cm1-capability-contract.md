# CM1 discovery, acquisition, and evidence capability contract

Status: Experimental capability contract for CM1

Designed for: Brontide Architecture 0.8 Complete Draft, not ratified

Implementations: independent Reference and Minimal experimental component-management projects

CM1 stops at immutable staging. It does not select a candidate, resolve a generation, prepare or
activate a Component, establish an Actor, or grant authority. The shared fixture is data and expected
observations only; each stack owns its discovery, acquisition, evidence-policy, and failure logic.

## Capabilities

### C1 — portable discovery query

A query names one canonical contract and exact contract version. Any number of fake sources may be
consulted, including zero. Unavailable sources are omitted from `ConsultedSources` and return no
candidates. Every result records the query, source endpoint, publisher, package, Component
definition, provided contract and version, artifact identity, explicitly source-available evidence
identities, and optional source-neutral storefront projection.

Target environment, lifecycle role, requester and requester publisher, Definition Constraints,
Preferred Providers, an occupied binding, containing Region and Port, and topology requirements are
carried unchanged for CM2 explanation and resolution. CM1 assigns them no filtering, ranking,
selection, compatibility, trust, or authority semantics. Contract and exact provided-contract
version are CM1's only candidate filters.

Evidence: native tests cover zero, one, and several sources, duplicate claims, and a source serving
unrelated publishers.

Property: every candidate answers the requested contract and version and is advertised by its
attributed source.

### C2 — deterministic attributable discovery

Source enumeration and per-source advertisement enumeration do not affect the result. Candidates
are ordered by source endpoint, package, and Component definition identity. Mirrored advertisements
remain distinct attributable candidates; source endpoint and publisher are never substituted for
one another.

Evidence: native tests exhaust every permutation of the three source snapshots and every
per-source advertisement permutation in the retained fixture, comparing complete outcomes.

Property: equal source snapshots and queries produce equal ordered results under every enumeration
permutation.

### C3 — staged acquisition

Acquisition is allowed only for a package advertised by the chosen source. Success copies the
descriptor, immutable artifact content, recorded digest, source-attributed evidence, and policy
decisions into a staged record. Missing artifacts, unavailable sources, unadvertised packages, and
digest mismatches fail closed with a structured reason and no staged value.

Evidence: native tests cover success and every declared refusal.

Property: a successful staged artifact's recomputed SHA-256 equals its recorded digest.

The fake policy decides each declared evidence item independently: an `accepted` claim produces an
accepted fake-policy decision and a `rejected` claim produces a rejected decision, with the policy,
issuer, source, and reason retained. This mapping is test machinery, not a universal trust policy.

### C4 — source disappearance does not mutate staging

After acquisition, removing or disabling the source cannot change already staged content,
attribution, evidence, decisions, or storefront data. A later acquisition through that source is
refused.

Evidence: native tests acquire, remove the source, compare the staged snapshot byte-for-byte, and
then observe a refusal.

Property: staged records depend only on the acquisition-time source snapshot.

### C5 — evidence remains attributable and contestable

Each acquired evidence item retains its evidence identity, issuer, kind, verdict, detail, subject
artifact, and supplying source. Source availability is declared separately in the neutral
`cm1-source-evidence` fixture; advertising a package does not imply supplying every claim about its
artifact. Fake local policy records an attributable decision and reason for each item independently.
Contradictory evidence is preserved; it is never collapsed into a `trusted` Boolean.

Evidence: native tests acquire the deliberately contested Fabrikam artifact and observe both
opposed review items and two policy decisions.

Property: staged evidence and decisions have a one-to-one identity-preserving correspondence.

### C6 — storefront projection is source-neutral

Local and remote sources expose the same storefront record shape. Discovery carries the projection
when the source advertises one and does not invent it when none exists.

Evidence: native tests compare the same Contoso projection from a local source and a remote mirror
through the same public discovery surface.

Property: source kind does not change the projection fields or their meaning.

### C7 — staging grants no authority and causes no lifecycle effects

Discovery and acquisition observations explicitly report zero selection, resolution, preparation,
activation, Actor-establishment, and Capability-grant effects. No API in CM1 accepts an activation
host or authority service.

Evidence: native tests assert the zero-effect observation for discovery, successful acquisition,
and every acquisition refusal.

Property: every CM1 result has the all-false lifecycle and authority effect profile.

## Failure categories

The CM1 fake source reports exactly one of:

- `source-unavailable`;
- `package-not-advertised`;
- `artifact-unavailable`;
- `artifact-integrity-failed`.

Failures are values in both stacks. They carry source and package attribution, expose the same
all-false effect observation as success, and never contain a partial staged artifact.
