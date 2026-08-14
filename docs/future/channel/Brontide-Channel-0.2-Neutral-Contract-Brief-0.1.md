# Channel 0.2 neutral contract and vector brief 0.1

Date: 2026-08-11

Status: proposed first-batch artifact boundary; no neutral schemas or generated code exist yet, and
subject to a fresh independent closure re-review. Batch 2 opens only after that review conforms and
its closure record exists. U3, V1, and V2 corrected after independent review: the established-profile
image carries the realization's per-interaction frame order declaration, the required adversarial
groups include one owning intra-interaction frame order and its ordering mutation, the parity profile
compares the peer-fault detailed reason, and the neutral provider may inject deterministic
per-interaction reordering so a declared ordering mutation can actually be executed.

## Purpose

Batch 2 will create a data-only Channel 0.2 contract that can be validated and consumed without
loading Reference, Minimal, or a shared semantic runtime. This brief fixes the artifact boundaries,
identity representation, version rule, vector/property structure, observation profile, golden policy,
and negative-probe discipline before a schema is authored.

The brief is subordinate to the [C1-C12 capability contract](./Brontide-Channel-0.2-Capability-Contract-0.1.md)
both state machines, and the closed
[state/event coverage grid](./Brontide-Channel-0.2-State-Event-Coverage-0.1.md). If a convenient
schema shape contradicts them, the schema changes.

## Planned neutral layout

Batch 2 is expected to create the following new tree without modifying the 0.1 neutral artifacts:

```text
channel/0.2/
    README.md
    contract.json
    schemas/
        established-profile.json
        session-message.json
        interaction-message.json
        peer-protocol-fault.json
        local-observation.json
        authority-presentation.json
        external-phase-predicate.json
        extension-facet.json
    vectors/
        c1-establishment.json
        c2-session.json
        c3-class-and-phase.json
        c4-correlation-and-concurrency.json
        c5-shape-and-bounds.json
        c6-authority.json
        c7-relational-initialisation.json
        c8-terminal-and-cancellation.json
        c9-provenance.json
        c10-observation.json
        c11-extensions.json
        c12-portability.json
    properties/
        capability-properties.json
    migration/
        channel-0.1-vector-map.json
    golden/
        <semantic-image>.json
        <selected-encoding-image>.bin
```

The exact filenames may change in Batch 2, but each semantic boundary remains separate. In
particular, local observations do not share a schema choice with peer messages.

## Artifact rules

- JSON documents are declarations and expected observations only; no script or expression language
  decides Channel semantics.
- Schemas reject unknown control fields unless an explicitly additive extension container owns them.
- Documentation fields are separated from encodable contract data so a neutral peer never has to
  strip prose to obtain a valid contract.
- Every enum is closed at its authority/control position. Additive payload Shapes follow their own
  open/closed declaration.
- Every numeric bound is finite, positive where zero has no meaning, and consistency-checked.
- Each schema names its capability owner and responsibility-matrix owner.
- The neutral verifier may parse and validate declarations, enumerate tables, compare canonical
  images, and execute a deliberately simple reference state model used only as a test oracle if the
  independent review explicitly approves it. No production stack may import that executable model.

The preferred first implementation is a declarative transition/property verifier rather than a
shared endpoint, preserving the repository's rule that the stacks implement semantics independently.

## Identity representation

The neutral form uses a bounded byte string for each identity space, rendered as lower-case
base16 only in JSON source. Each schema position identifies one exact space:

- Channel contract identity;
- profile identity/version;
- application contract identity/version;
- endpoint role identity;
- session identity;
- interaction identity;
- interaction-class identity;
- extension-facet identity/version; and
- optional profile-owned attribution references.

Session and interaction identities have a 16-byte default test representation and a declared
maximum of 32 bytes. The neutral contract does not mandate generation randomness or global
uniqueness; it mandates exact octet comparison and uniqueness in the declared scope. A stack exposes
each space as a distinct native type. Execution, Occurrence, Actor, Capability, Operation, Shape,
binding, and Component identities retain their owning canonical/typed forms and are never accepted
in a Channel identity position.

## Version and establishment rule

Three versions remain distinct:

1. **Channel contract version** — selects the Channel semantics and message/state contract;
2. **profile version** — selects a concrete use such as Portable Binding 0.2; and
3. **application contract version** — selects the Component/Operation contract conducted through the
   profile.

A negotiated proposal declares exact supported Channel versions and one exact proposed profile plus
required/optional extension facets. Acceptance selects an exact mutually supported Channel version
and confirms the complete profile image. It does not select a “nearest” application contract.

A fixed realization supplies the exact same profile image locally and validates it with the same
rules. The vector suite compares the resulting canonical established-profile image byte for byte
after semantic canonicalization.

The established-profile image also carries the realization's **per-interaction frame order**
declaration. C4 promises intra-interaction frame order, the responsibility matrix makes that
declaration the artifact crossing the boundary from the realization, and C4's evidence requires a
profile to check it at establishment — so `established-profile.json` gives it a normative position and
a realization that does not declare it refuses establishment exactly as an unknown required facet
does. It is a realization fact, not an extension facet: a profile with no facets at all still has it,
because core promises the ordering rather than a `delivery-facet` supplying it.

Unknown required facets or any version mismatch refuse before interaction dispatch. Optional facets
may be absent only when their declaration states that absence changes no core identity, authority,
terminal, or uncertainty meaning.

## Message-schema separation

### Session message

Carries Channel version, session identity, and one of proposal, acceptance, establishment refusal,
drain, close, or scoped fatal peer fault. Application Outcomes are impossible in this schema.

### Interaction message

Carries Channel version, session identity, interaction identity, interaction class, direction, and
one of request, cancellation control, or semantic Outcome. Peer protocol faults use their own schema
and scope. Local loss never appears here.

### Peer protocol fault

Carries Channel version, session identity, optional interaction identity according to scope, one
closed category, optional bounded local code, and bounded sanitized diagnostics. An unknown fault
category fails local validation and generates no fault reply.

### Local observation

Carries no peer-authored body. It records local provenance, state, admission decisions, dispatch
boundary, terminal form, detection point, and effect certainty. Profile-owned details are nested
under a versioned profile observation, never flattened into Channel core.

## External phase and authority inputs

The neutral external-phase record contains named Boolean/three-valued facts and their owner, snapshot
identity, and observation provenance. Interaction classes declare a closed predicate over those
facts. Batch 2 should prefer a small declarative conjunction/equality form over an executable
expression language. `false` and `unknown` both refuse admission.

Authority declarations record only the boundary mode, required presentation position, and expected
decision/result form. Neutral fixtures may carry authored Capability/Constraint examples for
same-domain tests, but the Channel schema does not define Capability wire semantics. Cross-trust
fixtures contain attributable context/designations and explicit `no-capability-transfer`, never a
Capability representation.

## Vector format

Every vector contains:

- stable vector id and capability id;
- predecessor 0.1 vector ids where applicable;
- profile and initial session/interaction state;
- endpoint perspective and role;
- explicit external phase and authority inputs;
- ordered stimulus steps;
- expected accepted/refused transitions;
- expected frame decision and peer/local provenance;
- expected terminal history and effect certainty;
- expected sibling-interaction effects for concurrency vectors;
- required evidence modes: neutral validation, Reference native, Minimal native, process, neutral
  peer, and cross-stack; and
- property memberships.

Expected observations are complete data, not prose interpreted by adapters. A vector never says
“implementation-defined” for a normative field. Realization-specific facts are either declared
inputs or explicitly excluded from the parity profile with a reason.

## Vector groups

One file is owned by each C1-C12 capability. Cross-capability scenarios live with the capability
whose property they are intended to falsify and list the other requirements. Required adversarial
groups include:

- fixed/negotiated profile equivalence and downgrade refusal;
- every legal and representative illegal session transition;
- external phase false and unknown for each interaction class;
- bounded concurrency interleavings, replay, mismatch, and out-of-order terminal facts;
- intra-interaction frame order and its ordering mutation: conforming commit-order delivery in both
  directions, loss of either frame, a cancellation control for an identity the peer never opened —
  which is legal input from a nonconformant peer and must not fail `C4-P2` — and
  `C4-control-precedes-request` itself, which requires the reordering injection declared under the
  neutral provider boundary and is the only vector group whose expected observation is a property
  going red;
- payload projection versus authority non-projection and each declared bound class;
- local denial, cross-trust forbidden authority, and deputy attribution;
- relational exact/mismatched edge, direction, member, Operation, Capability, Shape, and phase;
- cancellation races and duplicate terminal facts;
- every session/initiator/recipient state-event cell, including duplicate drain, acknowledgement
  multiplicity, receiver-local phase refusal, and the late-traffic latch;
- all peer-fault categories, local-loss categories, and unknown peer-fault no-loop behavior;
- known-none/known/unknown effect certainty counter-cases;
- required/optional extension facets and retry-as-new-attempt; and
- dependency, identity-space, determinism, and parity guards.

## Capability-wide property format

`capability-properties.json` declares at least one property per C-item with:

- property id (`C<n>-P<n>`);
- universal vector selector;
- fields/state facts quantified;
- invariant expressed through a closed declarative operator set;
- one named negative probe mutation;
- expected failing vector/property report; and
- evidence modes required to execute it.

The operator set may compare equality, membership, counts, transition edges, set uniqueness,
implication, and bounded “for all selected steps/vectors.” It may not call stack code or embed a
general scripting language.

Every property is run once against an intentionally mutated declaration/model and the failure output
is retained in the Batch 2 implementation record. A property that cannot be made to fail is a review
finding.

## Observation and parity profile

Core normative comparison includes:

- exact established profile digest;
- session state transition/result;
- session and interaction identity spaces (shape/scope, not opaque values across runs);
- interaction class/direction and phase decision;
- Shape and authority decisions;
- dispatch boundary crossed or not;
- terminal provenance and peer-fault/local-loss category where present;
- the peer-fault detailed reason wherever its category declares a closed set of them, so that two
  refusals sharing one category remain distinguishable — `C4-P2` quantifies over a recipient
  `rejected-protocol` caused by a cancellation control naming an unopened identity, which is one
  detailed reason of `invalid-interaction-correlation` and not the category as a whole;
- effect certainty and unknown reason class; and
- extension/profile-owned normative details selected by that profile's parity declaration.

Excluded by default:

- opaque generated identity values;
- encoding/framing choice when comparing direct versus process;
- local process topology and boundary count;
- representation-specific copy/allocation counts unless the profile makes them normative;
- timing, stack-local code, and diagnostic text.

An exclusion is valid only with a field-specific reason and property bounding the permitted
difference.

## Golden policy

Two kinds of golden are separate:

1. **semantic canonical image** — canonical JSON/data projection of the profile/message independent
   of a wire encoding; and
2. **selected encoding image** — bytes for one explicitly named encoding profile.

Batch 2 authors semantic goldens first. Encoding goldens are added only after an encoding decision
and cannot redefine semantics. Canonical map ordering, integer width, byte/text distinction, unknown
field policy, and normalization are declared by the encoding profile. Every golden is re-derived by
the neutral verifier and decoded independently by both stacks.

Channel 0.1 goldens remain under `binding/portable/golden/` and are never overwritten or renumbered
as 0.2 images.

## Neutral provider boundary

The implementation-neutral endpoint is built only after schemas/vectors exist. It:

- imports no Reference or Minimal assembly;
- implements the profile and both state machines independently from data artifacts;
- supports deterministic fault/loss injection named by vectors, and deterministic per-interaction
  reordering injection for the mutation vectors that require it. Reordering injection exists only to
  execute a declared mutation such as `C4-control-precedes-request`: it is never a legal delivery
  mode, no conforming realization may offer it, and a vector that does not name it receives commit
  order. Without it `C4-P2` would carry a named mutation nothing is permitted to produce;
- never supplies semantic expectations to the stack adapters at runtime; and
- exposes a process endpoint plus a fixed/direct pure test adapter where meaningful.

Its dependency graph and resolved libraries are verified as part of C12.

## Batch 2 entry gate

Schema authoring may begin only when:

- C1-C12, both state machines, and the closed state/event grid have no unresolved internal
  contradiction or uncovered recognized event;
- every responsibility-matrix concern has one owner;
- every 0.1 item/vector has a migration disposition;
- the completeness review has no unowned finding;
- the independent design review records no blocking finding; and
- the reviewed commit is pinned in a Channel 0.2 design-review closure record.
