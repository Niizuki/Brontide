# CBI7 participant-set revalidation and withdrawal capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI7 is to CBI6 what CBI5 is to CBI3: it revalidates, after activation, the authority that permitted
one. The caller supplies a fresh CM5 request for every participant of an admitted CBI6 set, each
with its own explicit evaluation instant, evidence, and policy. The shared portable member stays
released only when the identical set renews identically. Otherwise the composition retires the
member, closing its ordinary-interaction gate before peer withdrawal and termination.

The question CBI6 deliberately left undecided was what a shared member should do when one
participant of several loses authority. CBI7 answers it: the member is retired. See C6 for why
narrowing the set instead is refused rather than implemented.

## C1 — only an admitted CBI6 set can be revalidated

The input is a complete successful CBI6 result: every participant admitted exactly, aggregate
grants present, and one released Active member. A refused, partial, or already-retired result is not
silently treated as live authority.

Property: every unavailable input produces no CM5 evaluation, no retirement attempt, and no new
lifecycle effect.

## C2 — the participant set must be identical

The fresh requests name exactly the participants the admitted set named — no additions, no removals,
no substitutions, and no participant twice. Membership is compared before anything is evaluated,
because a changed set is not a renewal of the old one regardless of what its requests would say.

Property: any added, removed, substituted, or repeated participant retires the member without
evaluating a single request.

## C3 — each participant must re-identify the same authority, grant for grant

For every participant, the fresh request preserves the admission-request and policy identities, the
participant, the `ComponentParticipant` relationship request, and one authority request per prior
grant with identical local authority request identity, Capability, target Actor, Operation, and
scope. Evaluation time, evidence state, validity interval, trusted issuers, and policy rules may
change. Dropping a grant, adding one, or renaming any tuple field is not a renewal of that
participant's authority.

Property: identity or tuple drift in any request prevents every evaluation and can never keep the
member released.

## C4 — time, evidence, and policy stay explicit CM5 inputs, evaluated for all or none

Each fresh request reaches the native evaluator unchanged, in a deterministic order. CBI7 uses no
ambient clock and does not reinterpret revocation, expiry, evidence, or policy. As in CBI6,
evaluation is effect-free, so either every participant is evaluated and every current outcome is
retained, or none is.

Property: a result carries either no CM5 observation at all or exactly one per participant, never a
prefix of the set.

## C5 — continuation requires every participant to renew exactly

The member remains released only when every fresh outcome is `Admitted` and reproduces that
participant's established relationship and complete grant list exactly, including receiving-domain
Actor mapping, policy, and admitting rules. Similar, wider, substitute, partial, or newly mapped
authority is not continuity, and neither is one participant's renewal covering another's loss.

Property: every continued result reproduces, for every participant, one relationship and a grant
list equal to the ones CBI6 admitted.

## C6 — partial loss retires the shared member, and narrowing is refused

When any participant fails to renew, CBI7 retires the one member the whole set gated. It does not
drop that participant and keep the member released for the rest.

Narrowing would require knowing whether the lost participant was load-bearing for this binding, and
nothing in CBI6 says so: the set is unordered, no participant is marked required, and the member
exposes no way to declare which grants its ordinary interaction depends on. Choosing to continue
would therefore make a Component Management decision invisibly, which is the same reason PB7 refuses
to approximate a resolution. A caller that genuinely wants a smaller set can admit one through CBI6.

Property: a set where at least one participant does not renew leaves no released member, no reduced
participant set, and no reduced grant list.

## C7 — retirement closes the gate before peer cleanup, and cleanup failure stays visible

Retirement closes the ordinary-interaction gate before sending withdrawal or termination lifecycle
traffic. A clean retirement returns the replacement record, which grants nothing. Provider
withdrawal or termination failure produces a structured retirement failure that cannot restore the
prior authority, reopen the member, or fabricate a successful replacement, because the peer state is
unknown.

Property: after every non-continued result an ordinary interaction cannot reach the provider, even
when the peer cleanup subsequently fails.

## C8 — the result names which participants did not renew

A withdrawal reports the participants whose fresh admission was not identical, so the local decision
stays attributable rather than collapsing into one boolean. A structural refusal names none, because
nothing was evaluated.

Property: the reported participants are exactly those evaluated and found not identical, and are
empty whenever no request was evaluated.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate revalidators over their native CM5 and PB7 types.
CBI7 is additive: CBI5's single-participant revalidation and the CBI3, CBI4, and CBI6 evidence are
unchanged. Shared material is limited to this contract and the data-only scenario inventory.

Property: deleting either CBI7 revalidator leaves native CM5, CBI1-CBI6, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI7 proves fail-closed revalidation and withdrawal of one participant set over one released
singleton binding. CBI8 separately covers adding a participant in place. CBI7 itself does not
authorize a portable invocation, withdraw an already running execution, remove or replace a
participant in place, preserve state across retirement, order participants by priority, propagate
revocation to any other domain, or provide production identity, policy, distribution, or security.

Property: every CBI7 status statement preserves these limits.
