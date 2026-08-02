# CBI25 mediated-position translation capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §26.1, Complete Draft, not ratified

CBI25 carries a CM2 position resolved with **mediated exposure** into portable preflight. Every slice
since CBI1 has refused one, and the portable seam refuses one too — with a reason that turns out to
be the answer rather than an obstacle.

**The seam's refusal is right, and it is not relaxed here.** `PortableExposure.Mediated` exists and is
refused, because *"an erased Mediation still carries provenance, deputy, and authority obligations"*.
CBI25 never presents a mediated requirement to the seam and never asks for that refusal to be
softened. It presents a **distinct** requirement whose provider is the mediator.

**A policy-bearing Mediation is a Component, and a Component is what Portable Binding binds.** CM2
requires any Mediation owning mutable membership, residue, backpressure, authority, recovery, or
lifecycle to be realized as a `DedicatedComponent` with a named Component. So the obligations the seam
warns about have a holder, that holder is an ordinary Component, and binding it erases nothing: the
Binding Plan's provider fact names the mediator, which is who actually answers. What is *not*
expressible is a `StaticHost` Mediation, because there is no Component to bind — the host is the
mediator, and a binding to it would be a binding to nobody.

## C1 — the mediated position is identified, and it must actually be mediated

The input names the mediated requirement, the requirement whose position resolves the mediator, and
an ordinary CBI1 mapping for that mediator position. The mediated requirement must resolve exactly one
Provider Set whose exposure is mediated and which carries a Mediation declaration; a distinct position
offered here is refused rather than translated, because a caller that reaches for this path about an
unmediated position has misread its own resolution.

Property: every refusal produces no portable member and no Binding Plan.

## C2 — only a Mediation realized as a Component can be bound

A Mediation whose realization is `StaticHost` is refused, and so is one declaring
`DedicatedComponent` without naming the Component. Neither has anything a binding could reach: in the
first the mediator is the composition root itself, which binds the members directly and performs the
mediation as its own work rather than through this seam; in the second the declaration is incomplete.

CM2 already refuses a policy-bearing Mediation that is not a dedicated Component, so what reaches this
seam is either a Component or a Mediation that owns nothing — and the second is refused here rather
than quietly bound, because its being harmless is a property of today's declaration and not of the
path.

Property: every admitted translation names a Mediation whose realization is a dedicated Component.

## C3 — the mapping must name the declared mediator, not one of its members

The mediator selection's definition must equal the Mediation's declared Component, and its occurrence
must be one the generation resolves for that definition. A mapping naming a member of the mediated
Provider Set is refused, and that refusal is the whole point: it is the erasure the seam warns
against, arriving through the composition root instead of through the seam.

Property: no admitted translation produces a member whose provider is a member of the mediated
Provider Set.

## C4 — what is produced is an ordinary distinct member

The prepared member is CBI1's, over the mediator's own position: distinct exposure, one provider, one
binding scope. Every later slice therefore accepts it without knowing a Mediation was involved, which
is the point of binding the mediator rather than teaching the seam about mediation.

Property: the prepared member reports distinct exposure and the mediator as its provider, and CBI2
activates it exactly as it activates any other.

## C5 — the mediated requirement is carried as provenance, not as a portable fact

The result records the mediated requirement and the Mediation identity, because the composition root
knows something about this binding that the seam does not and should not: that the Component being
bound stands in front of a Provider Set. Nothing of that reaches the portable layer, no plan fact
changes, and no frame carries it.

Property: the portable member of a mediated translation is indistinguishable from one prepared for an
ordinary distinct position.

## C6 — presenting the mediated requirement itself is still refused

CBI1 continues to refuse a mediated position, unchanged, and so does the seam beneath it. CBI25 adds a
path that reaches the mediator; it removes no refusal.

Property: the mediated requirement offered to CBI1 is refused as `exposure-unsupported`, before and
after this slice.

## C7 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate translations over their native CM2 and PB7 types,
delegating the preparation to their own CBI1 path. CBI25 is additive: CBI1 through CBI24 are
unchanged.

CBI25 proves fail-closed translation of a mediated position into a binding to its mediator, at
preflight. It does not activate the mediated members, bind more than one provider, express mediated
exposure at the portable seam, model what the mediator does with the members behind it, or provide
production identity, policy, distribution, or security. **The mediator's authority is not admitted
here**: CBI3 admits against an occurrence, and whether the mediator's occurrence may stand for the
obligations CM2 says the Mediation owns is a question this slice does not answer.

Property: deleting either translation leaves native CM2, CM4, CM5, and Portable Binding behavior
unchanged, and every CBI25 status statement preserves these limits.
