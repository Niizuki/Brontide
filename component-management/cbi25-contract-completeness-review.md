# CBI25 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI25 mediated-position contract, separate from conformance review.

## Findings and dispositions

1. **The seam's refusal was the answer, not the obstacle.** Disposition: the finding to carry forward,
   and the first time in this programme that a published refusal turned out to point at the
   solution. `PortableExposure.Mediated` is refused because *"an erased Mediation still carries
   provenance, deputy, and authority obligations"*. Read as a requirement rather than a wall, that
   sentence says what a correct translation must do: keep the obligations with a holder. CM2 supplies
   the holder — a policy-bearing Mediation must be realized as a dedicated Component — and a Component
   is what Portable Binding binds. Nothing is erased and nothing is relaxed.
2. **This is the opposite outcome from CBI21, and the difference is worth naming.** Disposition:
   recorded. Relational Initialisation is unreachable because the seam has no stage, no verb, and no
   window for it; mediation is reachable because the seam needs nothing new — the mediator is an
   ordinary provider. A refusal in the seam is therefore not evidence either way about whether a
   capability can be integrated; what decides it is whether the thing being refused has a
   representation the seam already has.
3. **A static-host Mediation cannot be bound, and that is not a gap to close later.** Disposition:
   refused, with the reason stated. The host *is* the mediator, so a binding to it would be a binding
   to nobody; the composition root binds the members directly and performs the mediation as its own
   work. CM2 already forbids a policy-bearing Mediation from being realized this way, so what is
   refused here owns nothing — and it is refused anyway, because its harmlessness is a property of
   today's declaration rather than of the path.
4. **A Mediation naming a Component while realized as a static host is still refused.** Disposition:
   the realization decides, not the presence of a name. This became a vector because the falsification
   pass found the realization check was not load-bearing without one: the fixture only named a
   Component when the realization was dedicated, so the null check alone caught every case.
5. **A distinct position may carry a Mediation declaration, and CM2 ignores it.** Disposition:
   refused, and this too became a vector for the same reason. `ValidateMediation` returns early for
   distinct exposure without inspecting the declaration, and the resolved position keeps it. Exposure
   and the declaration are two facts, a caller can disagree with either, and checking only one left
   the exposure check unfalsifiable.
6. **The mediator must be resolved as a position of its own.** Disposition: required and checked. CM2
   names the Mediation's Component as a `DefinitionId`, not as a member of the set it fronts, so
   nothing guarantees the generation resolves an occurrence for it. The caller supplies one and the
   translation verifies it against the generation.
7. **The mediated members are not portable members of this position.** Disposition: deliberate. What
   the mediator does with the set behind it is the mediator's own composition, and modelling it here
   would be inventing a structure neither CM2 nor the seam describes.
8. **The mediator's authority is not admitted, and the reason is a real gap.** Disposition: excluded
   and named. CBI3 admits against an occurrence; the mediator has one, so admission is mechanically
   possible — but whether that occurrence's grants may stand for the obligations CM2 says the
   *Mediation* owns is a question about what a deputy is, and answering it by simply admitting the
   mediator would decide it invisibly. That is the shape CBI20 and CBI22 both warn about.
9. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
   structural limitation. The vectors force the mediated-versus-distinct rule in both directions, the
   two realization refusals, the mapping rule, and the unchanged seam refusal; they cannot establish
   general mediation completeness.

## Result

The CBI25 contract is complete for translating a mediated position into a binding to its mediator, at
preflight. Finding 1 is the one to carry forward, and finding 2 is the one that generalises: a seam's
refusal says nothing about integrability until you ask whether the refused thing has a representation
the seam already holds. Findings 4 and 5 are both vectors the falsification pass demanded rather than
the design anticipated. Finding 8 is the deliberate stop, and the next question. No finding requires
widening this contract into admitting the mediator's authority, activating the mediated members, or
teaching the seam about mediation.
