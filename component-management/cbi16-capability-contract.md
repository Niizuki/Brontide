# CBI16 multi-member observed-interaction verification capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI16 lifts CBI10's verification — projecting observed portable interaction into CM4 binding
exercises whose authority admission is derived rather than claimed — to a multi-member activation,
and answers the question that lift raises.

**One member's undeclared use condemns the whole activation.** A CBI12 activation is one CM4
request, so every member's interactions are projected into one list of binding exercises and CM4
returns one verdict over all of them: its rule that delivery cannot succeed when the external
authority check denied it refuses the request on the first offending exercise, not the offending
member. The answer therefore comes from the runtime's own shape, as CBI12's release barrier did, and
it agrees with CBI14's independent reason — the activation shares a restart scope, so it shares a
fate.

**Attribution is per member.** Each member has its own declaration and its own Operation-to-authority
mapping, so the same Operation may be attributed differently in two members without ambiguity. Only
within one member must an Operation name one declared authority.

## C1 — verification needs a released activation and a declaration per member

The input is a released CBI13 activation and one entry per member it admitted, each naming that
member's own selection, its declaration, its attribution mapping, and its observations. Each
declaration's names equal the requested authority CM2 records for that member's CBI1-selected
definition. Coverage is not a precondition, because an uncovered dependency is one of the things
verification reports.

Property: no verification proceeds on an unavailable activation, on a member set the activation did
not admit, or on a declaration the generation does not record for that member.

## C2 — the frame boundary is unchanged, and applies per member

An observation counts as use only if the interaction put a frame on the wire, exactly as CBI10
decides it. A member that interacted only locally exercised nothing, whatever its siblings did.

Property: an observation with no frame decision contributes no exercise, no attribution, and no
violation, in any member.

## C3 — attribution is per member, and the unattributable is undeclared use

Each member's mapping attributes that member's Operations to that member's declared authorities, one
entry per Operation within the member. The same Operation in two members is two independent
attributions, because the members are different Components with their own declarations. A delivered
interaction whose Operation its own member's mapping does not name, or names an authority its own
member does not declare, is undeclared use.

Property: a mapping that repeats an Operation within one member is refused, and one that repeats an
Operation across members is not.

## C4 — the projection is one CM4 request, so exercise identity is activation-wide

Every member's exercises are submitted together with the derived stage observations of the whole
activation. Exercise and routing identities are distinct across the activation, and each exercise
names its own member's occurrence, so CM4 can attribute it.

Property: no projection contains a repeated exercise identity or an exercise naming an occurrence
outside the plan.

## C5 — admission in the projection is derived per member, never claimed

Each exercise's `AuthorityAdmitted` fact is computed from its own member's declaration and its own
member's grants in force: true when the attributed authority is declared by that member and some
participant of that member holds a grant with its exact Capability, target Actor, Operation, and
scope. No member's grants admit another member's use, because CBI13 admits authority per member.

Property: no caller-supplied value determines the admission fact of any projected exercise, and no
member's exercise is admitted by a grant held for another member.

## C6 — CM4 judges the whole projection

The one request carries every member's exercises, and CM4's verdict on it is reported as it stands.

Property: the runtime accepts the projection exactly when every member's verification is consistent.

## C7 — a violation in any member retires the whole activation

Undeclared or ungranted use by one member closes every member's ordinary-interaction gate before
withdrawal and termination. Undeclared use is named before ungranted use, as CBI10 orders them, so
the reported violation is deterministic when both are present in one activation.

Property: after every violation an ordinary interaction cannot reach any member's provider, and after
every retirement either every member is released or none is.

## C8 — the result names which members violated and which did not

A member retired because a sibling used undeclared authority is never reported as the cause, as
CBI14 separates cause from consequence. Each member's unexercised and uncovered declared authorities
are reported alongside, and neither is a violation.

Property: no result both retires the activation and names zero violating members, and no member is
named as violating without a delivered exercise that failed to attribute.

## C9 — a structural refusal decides nothing

A member set the activation did not admit, a repeated Operation within a member, a declaration the
generation does not record, or a plan the activation did not use is declined with every member still
released and nothing evaluated. Only observed use condemns, which is CBI15's decline-versus-retire
distinction under a different input.

Property: every declined result leaves every member released, projects no exercise, and submits
nothing to the runtime.

## C10 — both composition roots implement independently, and evidence remains bounded

Reference Studio and Minimal Host own separate activation verifiers over their native CM4, CM5, and
PB7 types. CBI16 is additive: CBI10's single-member verification is unchanged.

CBI16 detects a declaration contradicted by use across an activation. It inherits CBI10's boundary —
absence of use is not evidence of absence of need, so it cannot detect an over-declared set — and
adds nothing about attributing an interaction to a participant, observing the provider's own
behaviour, Relational Initialisation, scoped replacement, member addition or removal, mediation, real
distribution, or production identity, policy, or security.

Property: deleting either CBI16 verifier leaves native CM4, CM5, CBI1-CBI15, and Portable Binding
behavior unchanged, and every CBI16 status statement preserves these limits.
