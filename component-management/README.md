# Component-management shared fixtures

This tree carries the neutral, data-only fixtures for the experimental fake Component Manager
planned by
[Brontide Component Management Implementation Plan 0.1](../docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md).
It may contain data and documentation only — never a shared runtime library, semantic
implementation logic, or code either stack executes. Each stack parses these files into its own
native types and computes its own observations.

Nothing in this tree is an Architecture 0.8 conformance claim. Sources, artifacts, digests,
evidence, and trust verdicts are deterministic test data for a fake manager; they prove nothing
about real distribution, packaging, or security.

CM1's observable behaviour is defined by the
[discovery, acquisition, and evidence capability contract](./cm1-capability-contract.md). That
contract stops at immutable staging and keeps selection, resolution, preparation, activation, Actor
establishment, and authority outside the phase.
Its completed phase-boundary absence audit is the
[CM1 contract-completeness review](./cm1-contract-completeness-review.md).

CM2's observable behavior is defined by the
[recursive generational resolution capability contract](./cm2-capability-contract.md). CM2 closes
finite acyclic structure into an inspectable Proposed Stack and immutable generation while retaining
occupied bindings, alternatives, policy exclusions, Provider Sets, occurrence identity, Mediation,
Ports, topology decisions, Parameters, requested authority, and decision provenance. It produces no
preparation, activation, Actor, authority, or active-generation effects. Its completed phase-boundary
absence audit is the
[CM2 contract-completeness review](./cm2-contract-completeness-review.md).

CM3's observable behavior is defined by the
[cyclic activation-group capability contract](./cm3-capability-contract.md). CM3 partitions a
complete fake activation graph into deterministic maximal strongly connected groups, validates
contract/version compatibility, bounded lifecycle protocols, Ready reachability, and declared
Region crossings, and emits an effect-free closed-gate stage plan. It does not prepare or establish
Components, execute lifecycle Operations, report runtime Ready, Release ordinary interaction, or
mutate an active generation. Its completed phase-boundary absence audit is the
[CM3 contract-completeness review](./cm3-contract-completeness-review.md).

CM4's observable behavior is defined by the
[preparation, activation, scoped-restart, and rollback capability contract](./cm4-capability-contract.md).
CM4 consumes a successful CM3 plan and explicit fake Host observations, then models optional
effect-free preparation, named establishment stages, exact lifecycle and ordinary gates, one
logical Release, post-Release binding exercises, scoped replacement, child-Port attachment, and
explicit recovery or degradation. It does not discover, resolve, load arbitrary code, or decide
authority policy. Its completed phase-boundary absence audit is the
[CM4 contract-completeness review](./cm4-contract-completeness-review.md).

CM5's observable behavior is defined by the
[authority-admission capability contract](./cm5-capability-contract.md). CM5 independently
evaluates attributable fake evidence, requested Actor relationships, and exact narrow authority
tuples under receiving-domain policy, then records local Actor mappings, Capability grants,
revocation or expiry refusals, unlimited-authority denial, and attributable policy mistakes. It is
not cryptographic, federated, or production security evidence. Its completed phase-boundary absence
audit is the
[CM5 contract-completeness review](./cm5-contract-completeness-review.md).

CM6 adds a versioned JSON Lines endpoint to each provider and runs every complete authority
scenario through both native CM5 implementations in both process directions. Comparison covers the
complete canonical CM5 profile rather than only its outcome; provider identity remains visible
outside the parity profile. Its observable behavior and claim boundary are defined by the
[independent-comparison capability contract](./cm6-capability-contract.md), and its completed
phase-boundary absence audit is the
[CM6 contract-completeness review](./cm6-contract-completeness-review.md). Equal profiles prove
agreement on the eight deterministic fake scenarios only, not real Component interchange,
contract completeness, general substitutability, or security.

CBI1 begins integration with Portable Component Binding at the two composition roots. Reference
Studio and Minimal Host independently accept one completed native CM2 direct `1..1` position plus
an explicit typed identity mapping and prepare a PB7 composition member without selecting,
negotiating, or starting a provider. Wider, mediated, absent, indirect, mismatched, or invalidly
addressed positions fail before a portable member exists. Its behavior and limits are recorded in
the [CBI1 capability contract](./cbi1-capability-contract.md) and completed
[contract-completeness review](./cbi1-contract-completeness-review.md).

## Format

Every fixture file is UTF-8 JSON with `schemaVersion` 1 and a discriminating `fixture` name.
Consumers fail closed: unknown schema versions, unknown top-level sections, duplicate identifiers
within one identity space, and unresolved references are rejected with a deterministic explanation.
The single deliberate exception is an artifact reference listed in
`expectations.missingArtifacts`, which models a package whose artifact cannot be retrieved.

Identifier values use lowercase ASCII letters, digits, `.`, and `-`. Each identity space (source,
publisher, package, definition, occurrence, actor, contract, binding scope, binding, artifact,
evidence, node, function, claim, observer, preference) is distinct: the same string in two spaces
is two unrelated identities, and native representations must keep them type-distinct.

### `cm0-catalog` sections

`contracts`, `publishers`, `sources`, `packages`, `advertisements`, `componentDefinitions`,
`bindingScopes`, `activatedOccurrences`, `occupiedBindings`, `preferences`, `artifacts`,
`evidence`, `storefront`, and `expectations`. Artifact digests are the real SHA-256 of the
`content` string's UTF-8 bytes, uppercase hex. The `storefront` entries are the source-neutral
presentation projection required by CM0: a future UI seam, not a UI.

### `cm1-source-evidence` sections

`availability` contains explicit source/evidence pairs. Each pair must name a source and evidence
item from `cm0-catalog`, and the source must advertise a package carrying the evidence subject's
artifact. Advertising a package does not implicitly supply every claim about it. The file is
separate so CM1 adds provenance without changing the retained CM0 catalog contract.

### `cm2-resolution-vectors` sections

`vectors` is the shared data-only inventory of CM2 capabilities and expected outcome categories.
Each native suite constructs equivalent stack-owned inputs and computes its own observations; the
fixture contains no resolver algorithm, ranking function, lifecycle host, or authority service.

### `cm3-activation-group-vectors` sections

`vectors` is the shared data-only inventory of CM3 group-analysis capabilities and expected
outcome categories. Each native suite constructs equivalent stack-owned activation graphs and
computes its own groups, stage plans, decisions, and structured failures; the fixture contains no
graph algorithm, activation host, lifecycle executor, or runtime effect.

### `cm4-activation-runtime-vectors` sections

`vectors` is the shared data-only inventory of CM4 fake-Host capabilities and expected outcome
categories. Each native suite constructs equivalent stack-owned runtime inputs and independently
computes stage, gate, Release, cutover, binding, scope, child-attachment, and rollback observations;
the fixture contains no activation algorithm, executable Component, authority policy, or rollback
mechanism.

### `cm5-authority-admission-vectors` sections

`vectors` is the shared data-only inventory of CM5 evidence, relationship, local-policy, and narrow
grant capabilities and expected outcome categories. Each native suite constructs its own typed
requests and independently computes every decision and effect; the fixture contains no verifier,
policy evaluator, Actor mapper, Capability implementation, or security mechanism.

### `cm6-authority-comparison-vectors` sections

`scenarios` contains complete versioned CM5 request, evidence, relationship, authority, policy,
evaluation-time, and expected-outcome data. Each stack strictly reconstructs its own typed model
and emits a canonical complete profile through a bounded JSON Lines endpoint. The fixture contains
no defaults, patch language, evaluator algorithm, native type names, or executable comparison
behavior.

### `cm0-mice-topology` sections

`contracts`, `observers`, `topologyNodes`, `functions`, `claims`, and `expectations`. Relations
are the minimum floor vocabulary: `PartOf`, `AttachedThrough`, `HostedBy`, `SamePhysicalAssembly`,
`SharesPowerDomain`, `SharesFailureDomain`. Claims are attributable assertions; the fixture labels
which are expected to be treated as contradictory or malicious so both stacks surface them without
accepting physical grouping, identity, trust, or authority on assertion alone.

### `expectations`

The `expectations` block is the shared expected-observation record: every entry must equal what a
loader computes from the data sections. A mismatch is a fixture defect or a loader defect and must
fail loading. Expectations never grant authority and carry no semantics beyond identification.
