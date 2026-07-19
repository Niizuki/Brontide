# Architecture 0.7 Mediation requirements and risk ledger

Status: non-runtime disposition for Architecture 0.7 C8. This is implementation planning evidence,
not a ratified Mediation contract or conformance claim.

Mediation is a declared relationship at a composition binding. Neither stack introduces a universal
`Mediator` participant, implicit interposition, or authority obtained through discovery. A dedicated
Component, host machinery, or static construction may realise the relationship, but the observable
obligations remain attributable to that realisation.

| Species | Required observation | Primary risk | Required implementation disposition |
| --- | --- | --- | --- |
| Selection | One requester uses one of several substitutable providers; affinity and decision residue are declared. | A backing change silently weakens endpoint guarantees or loses findability. | Record the selected provider and explanation; preserve the logical endpoint's own guarantees; enforce interposition through Capability target topology. |
| Distribution | One occurrence reaches the entitled receivers under a declared delivery and consistency contract. | Fan-out launders emitter/origin, leaks to an unauthorised receiver, or implies delivery guarantees not supplied. | Preserve original attribution and provenance; authorise receivers independently; state delivery, failure, and residue behavior. |
| Arbitration | Several requesters converge on one bounded provider under declared ordering, precedence, and fairness rules. | Ambient priority, confused-deputy use, starvation, or an advisory arbiter presented as mandatory. | Keep conflict semantics in the owning vocabulary; apply admission and deputy discipline; make bypass grants visible in the authority graph. |

All species must keep provider selection, crossed authority or failure boundaries, affinity, and the
decision function's retained residue inspectable. Confidential Attribute values and routing policy
must be redacted unless management authority permits them. A direct Capability to a backing visibly
bypasses the relationship; discovery and structural compatibility never grant that reachability.

For the Reference stack, any dedicated implementation remains in an `Experimental` project and
must not move mediation into Core. For the Minimal stack, the model remains outside Model and
Kernel unless a later ratified Base rule requires otherwise. The first executable instantiation is
the planned experimental storage Router; Event distribution and existing image-provider selection
remain examples only and do not establish Architecture 0.7 Mediation conformance.
