# BRONTIDE

## Design Note: Channel

**Status:** Work-in-progress design note, version 0.1; not ratified, not Brontide Base.
**Current architecture context:** [Brontide Architecture 0.8](./Brontide-Architecture-0.8.md),
especially §6.16, §8, §13.6, §16.4, §19, §24, §33, and §35.1.
**Evidence base:** the retained Cooling and Catalog interchange proofs, each implemented
independently in the Reference (C#) and Minimal (F#) stacks and crossing a real process boundary,
governed by the
[Reference/Minimal Interchange Implementation Plan 0.1](./Brontide-Interchange-Implementation-Plan-0.1.md).
**Related direction:** [Composition and Components](./Brontide-Design-Note-Composition-0.1.md) (the
Portable Binding is Channel's first intended realisation).

## Purpose and boundary

`Channel` is the recorded direction for the first Architectural Extension of the Architecture 0.8
evidence cycle: the generic communication frame that the invocation principle (§13.6) needs but
that Brontide Base deliberately withholds. §13.6 requires that the authority evaluated for an
Execution initiated on another Actor's behalf be attributable to that request, and states that
"the full request-carrying mechanics belong to future communication extensions." Channel is those
mechanics: **how a request and its Outcome are represented and correlated across a boundary, how
failure propagates, and what delivery does and does not promise.**

This note does not ratify Channel, add a Brontide Base term, or fix a wire format. It extracts the
semantic frame that the two retained interchange protocols **already share**, because §35.1 decided
that Channel is derived from that evidence rather than drafted in the abstract, and precedes the
Portable Component Binding, which becomes its first conforming realisation. Channel remains a
provisional extension name (§19).

Channel stays domain-neutral in the sense of §19: it may describe communication, but it does not
define any particular domain's messages. As the architecture puts it, Channel "may describe
communication [but] should not define how headphones transmit audio."

## The evidence base

Two independently implemented protocols cross a real `stdin`/`stdout` process boundary, each built
natively in both stacks with no shared runtime or private types (the dependency guards enforce the
separation; the root `interchange/` tree is data-only):

- **Cooling** (`protocolVersion: 2`) is a single-invocation exchange with a full runtime
  manifest-negotiation handshake, a host-owned authority domain that evaluates before the provider
  process is contacted, and three binding-scoped correlation identities.
- **Catalog** (`protocolVersion: 1`) is a multi-operation, resource-scoped exchange with no runtime
  handshake and no host authority domain, adding replay detection, a payload byte-limit, strict
  field allow-listing, version-skew rejection, and an explicit failure result.

Crucially, the two stacks do **not** diverge on the wire for either protocol: `protocolVersion`,
`kind` values, field names, and value encoding are identical, and interoperation is proven in both
host directions. Where they diverge is in realisation choices and host-native vocabulary — and that
divergence is the sharpest evidence of all, because it marks precisely what Channel must **not**
fix. The shared behaviour is Channel; the divergences are realisation freedom.

## Invariants Channel must preserve

Channel is an extension; it may not weaken Base. The following remain in force and shape every
rule below:

- **Two evaluation regimes (§6.16).** Every Execution carries a payload plane and an authority
  plane across the same boundary under opposite variance. Channel is where that boundary is
  crossed. Payload positions are covariant and tolerant (additive versioning, projection to a known
  version, §16.4); authority positions are contravariant and fail closed. Channel MUST classify
  every Shape-described position it introduces as covariant or contravariant, and MUST NOT apply
  payload-style projection to an authority-plane value.
- **Capabilities do not cross trust boundaries (§8).** Authorisation happens at each boundary.
  Channel may represent the *presentation* of authority at a boundary (§6.16), but never
  *constitutes* authority; custody stays domain machinery.
- **The invocation principle (§13.6).** Authority is attributable to the request; a responding
  Actor must not silently substitute its own ambient authority for a requester's.
- **Admission belongs to the receiver (§24).** A crossing participant proposes; the receiving
  domain admits. Self-description carried on a Channel is input to policy, never a grant.

## The extracted semantic frame

The following is the behaviour both Cooling and Catalog exhibit, abstracted to the level at which
they agree.

### Framed messages over a duplex byte stream

Both protocols exchange exactly one complete, self-contained message per frame over a duplex byte
stream, with a diagnostic side band that never carries semantic results (in the evidence: one JSON
object per line over `stdin`/`stdout`, with `stderr` diagnostic-only). Channel abstracts this to
**framed, self-delimited messages over a duplex transport, one message per frame, with any
diagnostic side band carrying no semantic result.** Channel does not mandate JSON, line framing, or
a standard-stream transport; those are realisation choices. A realisation MUST state its framing and
any frame-size bound.

### The message envelope model

Every message in both protocols carries, at minimum, a protocol version, a **kind** discriminator,
and a correlation identity. The kinds fall into a small, stable set of categories that Channel
adopts as its envelope model:

- **negotiation** (optional): establish or confirm the contract before invocation — present in
  Cooling (`activate`/`activation`), absent in Catalog;
- **request**: initiate an Execution on the far side (`invoke` in both);
- **outcome**: the single terminal result of a request, carrying a success/failure status;
- **protocol-error**: a boundary-level rejection that is not a semantic Outcome, carrying a
  category code and a message;
- **lifecycle**: orderly session control (`shutdown`/`shutdown-ack` in both).

Channel defines these **categories** and their meaning, not their spellings. The evidence is
explicit on this point: even between the two stacks, the protocol-error *code strings* differ for
equivalent conditions (a wrong-contract `invoke` is `unsupported-contract` in one stack and the
generic `invalid-message` in the other; an off-path message is `unknown-variant` versus
`invalid-message`). Channel therefore standardises the error **taxonomy**, and a realisation maps
its own codes onto it.

### Version and contract compatibility precede invocation

Both protocols declare a single protocol version on every message and **fail closed on any version
they do not recognise, never guessing across versions** (Catalog rejects a skewed version outright;
Cooling rejects any non-matching version on every inbound message). Cooling additionally negotiates
a full manifest — the contract, operation, Shape, and Fragment sets and each operation's
input/output Shapes — by *exact* match before any invocation; Catalog validates a static manifest
out of band. The shared rule Channel extracts is the §6.16 shift-left stance made concrete:
**compatibility is established before or independently of invocation, and a participant that cannot
establish it fails closed with a named protocol-error rather than proceeding.** Whether compatibility
is negotiated at runtime or fixed by a static contract is a realisation choice; that it is settled
before an effect is not.

### Request and Outcome correlation

Both protocols carry one or more binding-scoped correlation identities on the request, echo them on
the Outcome, and **reject an Outcome that does not match on every carried identity.** Cooling
carries three (request, execution, occurrence); Catalog carries two (request, execution, no
occurrence). Channel therefore requires **at least a request correlation identity, echoed on the
Outcome and matched on receipt**, and permits a realisation to carry finer identities.

A separate invariant both stacks enforce is that these binding-scoped identities are **not** the
host's own execution identity: the evidence maps them explicitly to a distinct host-native
execution id and asserts the two never conflate. Channel adopts this: **a correlation identity is
scoped to the Channel exchange and is never presented as a cross-domain Execution or Occurrence
identity** (§8, and the Plan's rule that binding-scoped identifiers "are not presented as
cross-domain identities").

### Authority presentation is boundary-relative, and no Capability crosses

This is the load-bearing rule, and the two protocols bracket it usefully. In Cooling, a real host
authority domain evaluates the request **before** the provider process is contacted; a denial means
the far side never starts and produces no effect; the manifest declares a `no-capability-transfer`
limitation and fails closed if it is absent; the only thing that crosses is host-constructed,
attributable request context (a fragment naming the requester), never a Capability. In Catalog there
is no host authority domain at all, and the resource handle "conveys addressing only, never
authority" — an unauthorised handle yields a semantic `resource-refused` failure, not a grant.

Channel unifies these as a **boundary-relative authority presentation**:

- A Channel request carries an authority-presentation position whose meaning depends on the boundary
  it crosses. **Within one authority domain**, it may present the Capability representation for the
  target's own evaluation (the §6.16 "presentation of authority at a boundary"). **Across a trust
  boundary** — the process-isolated case the evidence actually exercises — **no Capability crosses**;
  what crosses is at most attributable request context and a target/resource *designation* that is
  addressing, not authority, and the receiving domain performs its own admission (§8, §24).
- Denial occurs at the receiving boundary **before** the far side produces an effect, and is not a
  Channel wire message (see the failure taxonomy). The far side's own semantic checks (Catalog's
  `resource-refused`) are ordinary Outcomes, not authority grants.

This keeps Channel on the correct side of the §6.16 boundary fence: it defines how authority is
*presented*, never how it is *constituted*.

### The three-way failure taxonomy, and no exception crosses

Both protocols keep three failure meanings strictly distinct, and Channel adopts the distinction as
normative:

1. **Denial** — the receiving domain refuses to authorise. It is host-native, produces no far-side
   effect, and does **not** cross as a Channel message. (The `interchange/messages/denial.json`
   envelope is illustrative only; neither stack emits or consumes it — evidence that denial is a
   boundary decision, not a transported result.)
2. **Semantic failed Outcome** — the far side ran and reported failure. It crosses as an `outcome`
   with a failed status and a **structured `details` payload** (a code plus message or structured
   data), mutually exclusive with the success `result`. This is an ordinary terminal Outcome (§14.2)
   and flows in the payload plane.
3. **Protocol or process failure** — a malformed, unsupported, or out-of-contract message yields a
   `protocol-error` with a category code; a dead or timed-out far side yields a process failure
   recorded against a **failure domain**, never a fabricated success.

Across all three, **no exception, stack trace, or host CLR type ever crosses** — the Cooling codecs
scan for and reject a forbidden field set and duplicate protected fields. Channel makes this a rule:
**a Channel transports semantic Outcomes and protocol categories, never a foreign runtime's
exception or type representation.** (The evidence also shows this hardening is uneven between the two
protocols — Catalog relies on field allow-listing rather than a forbidden-field scan — which is why
Channel states the *guarantee* and leaves the *mechanism* to the realisation.)

### Boundary-hardening dimensions are declared, not fixed

Catalog adds replay detection (a process-local seen-request set), a payload byte-limit
(65,536 bytes), strict property allow-listing (unknown fields rejected), unknown-operation
rejection, and bounded parse depth; Cooling deliberately omits replay detection and any byte-limit.
Both are conforming. Channel therefore treats these as **declared hardening dimensions** — replay
window, payload bound, field strictness, parse bounds — each of which a realisation states
explicitly, including stating that it provides none. Silence is not a hardening claim.

### Non-promises

Neither protocol promises delivery, ordering, exactly-once, or automatic retry; both are synchronous
single-exchange request/response, and both record retry, interruption, and fallback as **facts,
never fabricating success** (the crash vectors assert `RetryCount = 0`, `Fallback = "none"`, and an
explicit interruption flag). Each declares its own limitations (Cooling: single invocation, no
capability transfer, no referenced resources; Catalog: no persistence after process exit). Channel
inherits this posture: **Base Channel promises no delivery, ordering, or retry semantics; a
realisation declares whatever it provides, and unprovided guarantees are recorded as facts rather
than simulated.** Stronger delivery and long-running semantics are the business of later extensions
(`Distributed`, `Realtime`, `Flow`, `Lifecycle`).

## What Channel does not define

Channel does not fix the transport, the serialisation, the framing discipline, the parse bounds, the
correlation-identity count, the presence or form of a negotiation handshake, the presence of a host
authority domain, the spelling of protocol-error codes, or the host-native terminal-Outcome
vocabulary. Every one of these varies between the two conforming protocols or between the two stacks
implementing them (for example, the host-native denial status is `Rejected` in one stack and
`Denied` in the other, and a missing required Fragment is caught at different layers producing
different host-native statuses — none of which crosses the wire). These are exactly the realisation
freedoms Channel preserves by defining semantics rather than encodings.

## Minimum Channel contract direction

A future portable Channel declaration should carry at least the following, without this note fixing
its descriptor or wire form:

- the transport and framing, and any frame-size bound;
- the protocol version and the compatibility model (negotiated handshake or fixed contract), and the
  fail-closed behaviour on an unrecognised version;
- the message categories supported and the mapping from realisation codes to the Channel error
  taxonomy;
- the correlation identities carried and echoed, and their explicit separation from any host-native
  Execution or Occurrence identity;
- the authority-presentation position and its boundary classification — intra-domain Capability
  presentation versus cross-trust-boundary attributable context — and the invariant that no
  Capability crosses a trust boundary;
- the failure taxonomy: how denial, semantic failed Outcome, and protocol/process failure are
  distinguished, and the failure-domain vocabulary;
- the declared hardening dimensions (replay window, payload bound, field strictness, parse bounds);
- the declared delivery limitations and the facts recorded on interruption, retry, and fallback; and
- the guarantee that no foreign exception or runtime type representation crosses.

## Relationship to the Portable Binding, Distributed, and the two planes

The Portable Component Binding (§18.1) is intended to be **Channel's first conforming realisation**:
a Binding Plan fixes contracts, authority presentation, representation, and lifecycle before the hot
path, and the Channel frame is what carries its requests and Outcomes. Sequencing Channel first
(§35.1) exists precisely so that "what a request is, how it correlates to its Outcome, at which
boundary authority is presented, how errors propagate" are settled as semantics before any encoding
freezes them — the §6.8 "no implementation defines the architecture" discipline applied to the wire.

A future `Distributed` extension is expected to depend on Channel (§19) and to add what Channel
deliberately omits at a trust boundary: mutual identification, attestation, and a cryptographic
cross-domain authority representation (§8, §24, §33). Channel does not attempt any of these; it fixes
the intra-domain and process-isolated frame on which they can later build.

Against §6.16: Channel is the boundary at which the payload and authority planes cross. It carries
payload-plane values covariantly (an Outcome's `result` and `details` project under §16.4) and keeps
the authority plane contravariant and fail-closed (an unrecognised authority-position value is never
projected to a weaker form). Naming the two planes at the Channel seam is what lets the C7/C8
constraint-evaluation rules run identically against both stacks.

## Deferred detail and open protocol work

This note stops before an extension specification. Still open, and owned by the future `Channel`
extension work rather than this note: the concrete descriptor and message Shapes; the canonical
error taxonomy and its required categories; the correlation-identity model as a portable contract;
the exact authority-presentation representation for the intra-domain case (the Portable Binding's
subject); duplex and streaming request shapes beyond single request/response; cancellation and
interruption signalling; and the conformance-vector set that would let one Channel contract be run
against both stacks. Delivery guarantees, ordering, and long-running Executions remain with
`Distributed`, `Realtime`, `Flow`, and `Lifecycle`. Nothing here ratifies the interchange test
protocols as Channel; they are the evidence Channel is extracted from, not its specification.

## Recorded direction

- Channel is the recorded first-cycle communication extension: the request/Outcome representation,
  correlation, error propagation, and delivery semantics that §13.6 needs and Base withholds. It
  remains a provisional extension name outside Base and is not ratified.
- Channel is extracted from the shared behaviour of the Cooling and Catalog interchange proofs;
  their divergences (correlation-id count, handshake presence, host authority domain, replay and
  payload bounds, error-code spellings, host-native Outcome vocabulary) mark the realisation freedom
  Channel preserves.
- Channel defines framed one-message-per-frame exchange over a duplex transport with a
  non-semantic diagnostic side band, transport- and serialisation-agnostic.
- A message carries a protocol version, a kind in the categories negotiation / request / outcome /
  protocol-error / lifecycle, and at least one correlation identity; receivers fail closed on an
  unrecognised version and never guess.
- Compatibility is settled before or independently of invocation; correlation identities are echoed,
  matched, and never conflated with host-native Execution or Occurrence identity.
- Authority presentation is boundary-relative: intra-domain it may present a Capability
  representation for the target's evaluation; across a trust boundary no Capability crosses, only
  attributable context and addressing, and the receiver admits under its own policy. Channel
  presents authority; it never constitutes it.
- Denial, semantic failed Outcome, and protocol/process failure are three distinct meanings; denial
  is a boundary decision that never crosses as a message; no exception or foreign runtime type ever
  crosses.
- Delivery, ordering, and retry are promised by no one; a realisation declares its limitations and
  records interruption, retry, and fallback as facts rather than simulating success.
- The Portable Component Binding is Channel's first intended realisation; `Distributed` is expected
  to depend on Channel for cross-domain trust it does not itself provide.
