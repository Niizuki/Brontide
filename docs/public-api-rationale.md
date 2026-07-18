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

No public compatibility promise extends from these experimental APIs. A breaking change must still
follow [`VERSIONING.md`](../VERSIONING.md), document affected provider/host consumers, and update the
fixture contract version or protocol version when wire interpretation changes.
