# CBI26 contract-completeness review

Date: 2026-08-02

Scope: absence review of the CBI26 mediator-authority contract, separate from conformance review.

## Findings and dispositions

1. **CM5 has no deputy, and that is the whole answer.** Disposition: the finding to carry forward.
   Its relationship kinds are `AttachedDevice`, `ExternalPeer`, and `ComponentParticipant`, none of
   which means *acts on behalf of*; its grant names exactly one `Holder` and no beneficiary. A
   mediator can therefore be admitted for what it does itself and for nothing else. CBI25 predicted
   this outcome from CBI3's rule that a grant names a holder, and reading CM5 confirmed it from the
   model rather than from the inference.
2. **A Mediation that owns authority is refused rather than approximated.** Disposition: fail closed.
   The tempting alternative is to admit the mediator and let its own narrow grants stand for the
   members' interaction, which reads as working and decides what a deputy is in the least visible
   place available. Refusing costs the caller a capability CM2 can declare and CM5 cannot represent,
   which is the honest state of the two models rather than a gap in this slice.
3. **Only `OwnsAuthority` decides the outcome.** Disposition: deliberate, and stated because the
   other five flags look like they should matter. Mutable membership, residue, backpressure,
   recovery, and lifecycle describe what the mediator does with the set behind it; none is a claim
   about who may exercise a Capability. Two vectors pin that an admission survives them.
4. **This is CBI21's answer arriving in a different model.** Disposition: recorded. Relational
   Initialisation is unreachable because the portable seam lacks the concept; a mediator's deputy
   authority is unreachable because CM5 lacks it. Neither is a plumbing problem, and in both cases
   the slice's job was to establish that precisely rather than to build something that looks like the
   capability.
5. **The mediated members are not admitted, and nothing here notices whether they need to be.**
   Disposition: bounded. CBI25 leaves them behind the mediator and CBI26 does not reach them, so a
   Mediation whose members require their own admissions gets none from this path. That is a
   consequence of binding the mediator rather than the set, and it is stated rather than implied.
6. **Whether CM5 should gain a deputy relationship is not this slice's to decide.** Disposition:
   raised as Decision 15. CM2 can declare a Mediation that owns authority, so the two models
   disagree about what is expressible, and closing that is a Component Management decision about
   whether a grant may name a beneficiary.
7. **Two implementations can still agree where this contract is silent.** Disposition: retained as a
   structural limitation. The vectors force the ownership rule in both directions, the unrelaxed
   CBI3 refusals, and the holder of every grant; they cannot establish general mediator-authority
   completeness.

## Result

The CBI26 contract is complete for admitting a mediator's own authority. Finding 1 is the one to carry
forward, and finding 4 is the pattern: twice now, a capability CM2 or CM3 can declare has turned out
to have no representation in the model that would have to carry it, and in both cases naming the
missing concept was worth more than approximating the capability. Finding 6 is the open question. No
finding requires widening this contract into admitting authority for the mediated members.
