# Component-management shared fixtures

This tree carries the neutral, data-only fixtures for the experimental fake Component Manager
planned by
[Brontide Component Management Implementation Plan 0.1](../Brontide-Component-Management-Implementation-Plan-0.1.md).
It may contain data and documentation only — never a shared runtime library, semantic
implementation logic, or code either stack executes. Each stack parses these files into its own
native types and computes its own observations.

Nothing in this tree is an Architecture 0.8 conformance claim. Sources, artifacts, digests,
evidence, and trust verdicts are deterministic test data for a fake manager; they prove nothing
about real distribution, packaging, or security.

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
