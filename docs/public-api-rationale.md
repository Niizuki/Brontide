# Public API rationale

Status: implementation-owned API review record. Public means independently consumable by the
stack's host, test console, or another assembly in the same stack; it does not imply normative
Brontide architecture.

The Base/Core public surface exists so hosts can carry typed identities, declare Shapes and
Operations, issue or delegate authority through the owning domain, submit an explicit execution,
and inspect redacted Outcomes/Events. Constructors that could mint issuer-controlled references or
bypass registration remain private/internal. Constraint evaluators, time providers, and handlers
are injected because deterministic and fail-closed decisions cannot depend on ambient services.

Experimental binding DTOs and codecs are public only because provider executables and foreign-host
test assemblies are separate consumers. `ProviderLaunch`, process clients, endpoints, manifests,
resource references, and portable values form that executable seam. Provider state and semantic
handlers remain in composition roots. Protocol parsing helpers, JSON writers, replay sets, and
resource-scope checks that need no external composition remain private/internal.

The Catalog public records use distinct request, execution, and resource-reference types so their
identity spaces cannot be mixed accidentally. `CatalogProtocolException` is public so a host can
classify an experimental protocol failure without receiving a provider-private exception; its
message is diagnostic and is never promoted automatically into a Base failure payload.

Architecture 0.7 typed-member identities are public value types because definition readers,
gateways, and tooling must parse and compare them without lossy string splitting. They are additive
to the existing concept-name API. `MemberKind` is intentionally an open validated token rather than
an enum or closed union because Architecture 0.7 leaves the member-kind catalogue and final glyph
provisional. No current binding protocol serializes the new type, so no wire version or legacy alias
is introduced.

Two correction APIs expose trust-boundary facts deliberately. Minimal `FragmentDefinition` now
requires `HostShape` so an authored attachment cannot be replayed onto an unrelated open Shape;
callers must provide the earliest compatible host Shape. Reference `ExecutionRecord.HasInput`
allows audit consumers to distinguish a complete authorized record from a rejected provenance copy
whose protected input was not retained. Direct `ExecutionResult` values remain complete for their
caller.

No public compatibility promise extends from these experimental APIs. A breaking change must still
follow [`VERSIONING.md`](../VERSIONING.md), document affected provider/host consumers, and update the
fixture contract version or protocol version when wire interpretation changes.
