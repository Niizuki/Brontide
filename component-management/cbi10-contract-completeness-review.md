# CBI10 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI10 observed-interaction verification contract, separate from
conformance review.

## Findings and dispositions

1. **Verification could have been this slice's own opinion.** Disposition: the admission fact of
   each projected exercise is derived from the declaration and the grants in force, and the verdict
   is corroborated by CM4's existing rule that delivery cannot succeed when the external authority
   check denied it. Both stacks assert the equivalence as a property over every vector — the runtime
   accepts the projection exactly when the verification is consistent — so a divergence between the
   two would fail rather than pass silently.
2. **A caller could have supplied the admission.** Disposition: refused by construction. The caller
   supplies observations and an attribution mapping; nothing it provides sets `AuthorityAdmitted`.
   CBI3's rule that a caller may not author binding-exercise authority is preserved, and this slice
   is what supersedes it rather than relaxing it.
3. **Omitting a mapping entry could have hidden an interaction.** Disposition: a delivered
   interaction the mapping does not name is undeclared use, exactly as one attributed to an
   undeclared authority is. An interaction that cannot be attributed to declared authority is, from
   the receiving domain's view, an interaction outside the declaration.
4. **"Observed" needed a boundary.** Disposition: an interaction counts as use only if it emitted a
   frame. A locally denied request reached no provider and exercised nothing. Any emitted frame
   counts, including a rejected one, because the receiving domain cannot know what a frame the
   provider already saw caused. The vectors pin a denied interaction as exercising nothing and
   producing no provider effect.
5. **Two harmless-looking outcomes could have been read as violations.** Disposition: neither a
   declared authority nothing exercised nor a declared authority no participant covers is a
   violation on its own; both are reported. A dependency may be real and simply unused so far, and
   an uncovered dependency that nothing exercised has not yet been relied on. Only the combination —
   uncovered *and* exercised — is a violation.
6. **Verification could be mistaken for a gate.** Disposition: it cannot undo an interaction that
   already happened and does not authorize a future one. Retirement is what it can do about the next
   interaction, and it is the only effect it has.
7. **The order of two simultaneous violations was undefined.** Disposition: undeclared use is named
   before ungranted use, so the reported violation is deterministic when both are present.
8. **The slice cannot detect the opposite error.** Disposition: recorded as the boundary worth
   carrying forward. Absence of use is not evidence of absence of need, so a declared dependency
   that nothing exercised cannot be shown unnecessary, and an over-declared set keeps participants
   CBI9 will not release. CBI10 closes CBI9's finding 6 in one direction only.
9. **Interaction is not attributed to a participant.** Disposition: the projection attributes use to
   a declared authority, not to whichever participant holds the grant covering it. Several
   participants may hold the same tuple, and nothing in an observed interaction says which one it
   relied on.
10. **The provider's own behaviour is not observed.** Disposition: the evidence is the host's record
    of its own interactions. A provider that acted without a request, or acted differently from what
    it reported, is outside what this composition can see.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the frame boundary, attribution, admission-derivation,
    runtime-agreement, retirement, and reporting answers; they cannot establish general
    interaction-verification completeness.

## Result

The CBI10 contract is complete for verifying one declaration against a given set of observed
interactions over one released singleton binding. Finding 8 states what it deliberately cannot do:
it detects a declaration contradicted by use, never one contradicted by disuse. No finding requires
widening it into cross-vocabulary Operation mapping beyond the explicit attribution it already
takes, multi-member or relational lifecycles, mediation, real distribution, or Architecture 0.8
conformance.
