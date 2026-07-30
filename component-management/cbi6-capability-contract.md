# CBI6 multi-participant and multi-grant admission capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI6 widens CBI3's authority gate from one participant holding one grant to a set of participants
each holding one or more exact narrow grants, while the binding itself stays the CBI1-CBI2
singleton. A CM5 request carries exactly one participant, so a set of participants is a set of
requests: each is evaluated separately by the native evaluator, and the composition continues only
when every one of them is admitted exactly as submitted.

The slice exists because "one participant, one grant" hid three questions that only a set can ask:
whether identities stay distinct across separate requests, whether two remote participants may
land on the same receiving-domain Actor, and what a partially admitted set is worth. CBI6 answers
all three fail closed.

## C1 — the participant set is explicit, non-empty, and distinct

The caller supplies one or more participant entries, each pairing an occurrence-to-Actor mapping
with a complete CM5 request. Every mapping names the CBI1-selected occurrence, every mapping's
Actor equals its own request's participant, and no Actor appears twice. An empty set is not an
absence of authority requirements; it is a refusal.

Property: no set that is empty, repeats a participant, or names an occurrence other than the CBI1
selection can reach CM5 evaluation or provider contact.

## C2 — the singleton binding is unchanged

CBI6 gates one prepared CBI1 member activated by one protocol-free CM4 plan, exactly as CBI2 and
CBI3 do. Several participants share authority over that one member; they do not introduce several
members, occurrences, Provider Sets, or activation groups.

Property: no participant set, of any size, produces more than one portable member, occurrence, or
activation group.

## C3 — each request keeps the exact narrow shape, now with several grants

Every request contains exactly one relationship, of kind `ComponentParticipant`, proposed by that
request's participant, and one or more authority requests, each dependent on that relationship and
none unlimited. Within one request the Capability, target Actor, Operation, and scope tuples are
pairwise distinct, so no participant can ask for the same narrow authority twice under two
identities. The CM4 request still contains no caller-authored binding exercises.

Property: an additional relationship, an unlimited request, an authority request dependent on
another participant's relationship, a repeated tuple within one request, or caller-authored CM4
binding authority produces no CM5 evaluation and no provider establishment.

Two different participants may request the same tuple. Their grants differ in holder, which is the
fact that makes them different grants, and the receiving domain decides each one separately.

## C4 — identities stay distinct across the whole set

Admission request, relationship request, and authority request identities are pairwise distinct
across every request in the set, not only inside each one. CM5 validates each request alone and
cannot see the collision; the aggregate is where a shared authority request identity would produce
two grants that are indistinguishable by identity.

Property: any identity repeated across two requests refuses the set before evaluation, even though
each request is individually valid.

## C5 — every participant is evaluated, and every one must be admitted exactly

Each request reaches the native CM5 evaluator unchanged, in a deterministic order, with its own
explicit evaluation time, evidence, and policy. Evaluation is effect-free, so the whole set is
evaluated and every outcome is retained for attribution rather than stopping at the first refusal.
Activation requires that each outcome be `Admitted`, with exactly one established relationship
naming the submitted one and exactly one grant per submitted authority request, matching its
Capability, target Actor, Operation, scope, and holder.

Property: a result carries either no CM5 observation at all or exactly one per participant, never a
prefix of the set, and one non-exact outcome anywhere in the set prevents provider contact.

## C6 — distinct participants map to distinct receiving-domain Actors

The local Actor references established for two different participants must differ. Local policy
maps `(proposed Actor, relationship kind)` to a local Actor reference and never consults the rest
of the set, so a policy that mapped two remote participants onto one local Actor would silently
merge two parties' grants into one holder. CBI6 refuses that set after evaluation and before
provider contact.

Property: every activated set has as many distinct local Actor references as it has participants.

## C7 — a partially admitted set grants nothing

The result exposes aggregate grants only when the complete set was admitted exactly. Every refusal
— structural, shape, admission, or local identity — reports the retained CM5 observations for
attribution and an empty grant set, leaves no portable member, and reaches no provider.

Property: a result that is not active carries no aggregate grant, no Binding Plan, and no provider
effect.

## C8 — authority still never crosses the portable trust boundary

As in CBI3, the admitted relationships and grants are receiving-domain observations controlling
whether the composition may continue. No Actor reference, grant, evidence, policy, or decision from
any participant enters the portable contract, Binding Plan, constraint value, or operation payload,
and the number of participants is not visible to the provider.

Property: changing the participant set can change whether activation proceeds, but cannot change
any portable contract or Binding Plan fact.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate coordinators over their native CM5, CBI1, and CBI2
types. CBI6 is additive: CBI3's single-participant coordinator, and the CBI4 and CBI5 evidence
built on it, are unchanged.

Property: deleting either CBI6 coordinator leaves native CM5, CBI1-CBI5, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI6 proves fail-closed admission of a set of participants and grants gating one singleton,
protocol-free activation. CBI7 separately covers revalidation and withdrawal of an admitted set.
CBI6 itself does not exercise any granted Operation, map grants to CM4 binding exercises or portable
Operations, order participants by priority, model participants joining or leaving an active binding,
or provide production identity, policy, distribution, or security.

Property: every CBI6 status statement preserves these limits.
