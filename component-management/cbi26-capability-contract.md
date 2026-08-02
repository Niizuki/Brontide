# CBI26 mediator authority admission capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §13.5, §18.1, and §26.1, Complete Draft, not ratified

CBI26 admits the authority of the mediator CBI25 binds. CBI25 stopped before CM5 because the question
it left is not about plumbing: a Mediation may declare that it **owns authority**, and whether that
can be admitted is a question about what a deputy is.

**CM5 has no deputy, and the answer follows from that.** Its relationship kinds are
`AttachedDevice`, `ExternalPeer`, and `ComponentParticipant`; none of them means *acts on behalf of*.
Its grant names exactly one `Holder`, a local Actor, with no beneficiary beside it. So a mediator can
be admitted for **its own** interaction, exactly as any participant is, and there is no way to express
a grant it exercises for a member behind it. A Mediation declaring `OwnsAuthority` is therefore
refused rather than approximated: admitting the mediator and letting its own grants stand for the
members' would decide what a deputy is, invisibly, in the place least likely to be read.

## C1 — the mediator is admitted as an ordinary participant, against its own occurrence

The input is a CBI25 translation and one participant request for the mediator. Admission is CBI3's,
unchanged: one `ComponentParticipant` relationship, exact narrow authority tuples dependent on it,
and the mediator's own occurrence. Nothing about the Component standing in front of a Provider Set
changes how it is admitted for what it does itself.

Property: an admitted mediator holds grants naming its own local Actor, and the admission is
indistinguishable from an ordinary participant's.

## C2 — a Mediation that owns authority is refused

`OwnsAuthority` says the Mediation is responsible for the authority of the interaction it fronts, and
CM5 cannot say that. Refusing it is the fail-closed reading: the alternative is a mediator holding
narrow grants for its own Operations while a reader believes those grants cover the members, which is
precisely the erasure CBI25's binding avoided at the seam arriving instead at the admission.

Property: no admission is produced for a Mediation declaring `OwnsAuthority`, and the refusal names
the declaration rather than the request.

## C3 — the other ownership flags are not authority, and are not refused here

A Mediation may own mutable membership, residue, backpressure, recovery, or lifecycle. None of those
is a CM5 question: they describe what the mediator does with the set behind it, which this seam does
not model and this slice does not touch. They are admitted without comment and remain out of scope,
which the contract states so that their silence is deliberate rather than accidental.

Property: an admission's outcome depends on `OwnsAuthority` alone among the ownership flags.

## C4 — the mediator's grants are its own

An admitted mediator's grants name its local Actor as holder and correspond one-for-one to the narrow
tuples it submitted. No grant of a mediated member's appears, because no member was admitted: the
mediated Provider Set is behind the mediator and outside this admission entirely.

Property: every grant in the result is held by the mediator's local Actor.

## C5 — nothing widens CM5, and CBI3 is unchanged

No relationship kind is added, no grant gains a beneficiary, and CBI3's supported shape is not
relaxed. CBI26 is a caller of CM5, not an extension of it.

Property: the admission a mediator receives is one CBI3 would produce for the same request against
the same occurrence.

## C6 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate paths, delegating the admission to their own CBI3 one.
CBI26 is additive: CBI1 through CBI25 are unchanged.

CBI26 proves fail-closed admission of a mediator's own authority. It does not admit authority on
behalf of a mediated member, model what the mediator does with the set behind it, activate the
mediated members, or provide production identity, policy, distribution, or security. Whether CM5
should gain a deputy relationship is recorded as an owner decision rather than answered here.

Property: deleting either path leaves native CM2, CM5, and Portable Binding behavior unchanged, and
every CBI26 status statement preserves these limits.
