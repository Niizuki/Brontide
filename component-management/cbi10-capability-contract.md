# CBI10 observed-interaction verification capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI9 trusts the Component's declaration of what its ordinary interaction depends on, and its
completeness review recorded the consequence: nothing checked that declaration against what the
member actually did. CBI10 checks it, against the only evidence the composition has — the portable
interactions the member performed after Release.

Each observed interaction is projected into one CM4 binding exercise whose authority admission is
**derived** from the declaration and the grants in force, never claimed by the caller. CM4 then
judges the projection with a rule it already had: delivery cannot succeed when the external
authority check denied it. Use the member never declared, and use no participant holds a grant for,
therefore surface as CM4's own conflict rather than as a private opinion of this slice.

## C1 — verification needs an admitted set and a declaration that matches the resolution

The input is a set in force with a released member, and a declaration whose names equal the
requested authority CM2 records for the CBI1-selected definition. Shape is checked as CBI9 checks
it; coverage is not a precondition here, because an uncovered dependency is one of the things
verification is meant to report.

Property: no verification proceeds on an unavailable set or on a declaration the generation does not
record.

## C2 — an interaction that emitted no frame exercised nothing

An observation counts as use only if the interaction put a frame on the wire. A locally denied
request — a closed gate, an unsatisfied constraint — reached no provider and exercised no authority.
Any emitted frame counts, including a rejected one, because the receiving domain cannot know what a
frame the provider already saw caused.

Property: an observation with no frame decision contributes no exercise, no attribution, and no
verdict.

## C3 — attribution is explicit, and the unattributable is undeclared use

Each Operation is attributed to a declared authority through an explicit typed mapping, one entry
per Operation. A delivered interaction whose Operation the mapping does not name, or names an
authority the declaration does not declare, is undeclared use. Omitting a mapping entry is
therefore not a way to hide an interaction: from the receiving domain's view an interaction that
cannot be attributed to declared authority is exactly an interaction outside the declaration.

Property: every delivered interaction is either attributed to a declared authority or counted as
undeclared use; none is silently ignored.

## C4 — admission in the projection is derived, never claimed

The `AuthorityAdmitted` fact of each projected exercise is computed from the declaration and the
grants in force: true when the attributed authority is declared and some participant holds a grant
with its exact Capability, target Actor, Operation, and scope. The caller supplies observations and
a mapping, never an admission. CBI3's rule that a caller may not author binding-exercise authority
is preserved, and this is what supersedes it.

Property: no caller-supplied value determines the admission fact of any projected exercise.

## C5 — CM4 judges the projection

The projected exercises are submitted to the native CM4 runtime with the same plan and derived stage
observations the activation used. CM4's verdict on the projection is reported as it stands.

Property: the runtime accepts the projection exactly when the verification is consistent.

## C6 — undeclared or ungranted use retires the member

Either violation closes the ordinary-interaction gate before withdrawal and termination, as CBI7
retirement does. Verification cannot undo an interaction that already happened; retiring is what it
can do about the next one. Undeclared use is named before ungranted use when both are present, so
the reported violation is deterministic.

Property: after every violation an ordinary interaction cannot reach the provider, even when the
peer cleanup subsequently fails.

## C7 — a consistent verification changes nothing and says what was not used

The member stays released, the set in force is unchanged, and the result reports the declared
authorities no delivered interaction exercised and the declared authorities no participant covers.
Neither list is a violation: a dependency may be real and simply unused so far, and an uncovered
dependency that nothing exercised has not yet been relied on.

Property: every consistent result leaves the member released and creates no grant, exercise
authority, or portable effect.

## C8 — verification neither grants nor gates

It cannot make a past interaction lawful, does not authorize a future one, and adds nothing to any
portable contract, Binding Plan, constraint, or payload. It reports what the observations imply
about the declaration.

Property: changing a verification's outcome can retire a member, but cannot change any portable
contract or Binding Plan fact.

## C9 — both composition roots implement independently

Reference Studio and Minimal Host own separate verifiers over their native CM4, CM5, and PB7 types.
CBI10 is additive: CBI6 through CBI9 are unchanged, and shared material is limited to this contract
and the data-only scenario inventory.

Property: deleting either CBI10 verifier leaves native CM4, CM5, CBI1-CBI9, and Portable Binding
behavior unchanged.

## C10 — evidence remains bounded

CBI10 detects a declaration contradicted **by use**: interaction outside it, or interaction that no
grant covers. It cannot detect the opposite error — a declared dependency that is genuinely
unnecessary — because absence of use is not evidence of absence of need, so an over-declared set
keeps participants CBI9 will not let go. It also verifies only the interactions it is given, over
one released singleton binding, and does not observe the provider's own behaviour, attribute
interaction to a specific participant, or provide production identity, policy, distribution, or
security.

Property: every CBI10 status statement preserves these limits.
