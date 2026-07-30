# CBI9 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI9 declared grant dependency and participant revision contract,
separate from conformance review.

## Findings and dispositions

1. **A dependency declaration supplied by the caller would decide nothing.** Disposition: the
   declared names must equal the requested authority CM2 already records for the CBI1-selected
   definition, so the Component states what its interaction depends on. The caller supplies only the
   explicit typed mapping from each declared name to a CM5 tuple, and a mapping aimed at a tuple
   nobody holds is caught before any revision by the coverage check on the set in force.
2. **A declaration could have been introduced to bless a set that never covered it.** Disposition:
   refused. The set currently in force must satisfy the declaration before a revision is considered,
   which is also what makes a self-serving mapping visible rather than merely unused.
3. **An empty declaration could have licensed arbitrary shrinking.** Disposition: refused. A
   definition that requests no authority states nothing about what its interaction depends on, which
   is not the same as stating that it depends on nothing that was admitted. Growth stays available
   through CBI8 and retirement through CBI7.
4. **CBI8 refuses substitution and CBI9 permits it, which could read as a contradiction.**
   Disposition: it is not. CBI8's reason was the *absence* of any statement about dependence, and
   its growth-only rule remains correct in that absence. A declaration names tuples rather than
   holders, so a substitute satisfying the same declared dependency is enough. The two rules coexist
   by whether a declaration exists, and CBI8's contract now says so.
5. **A departing participant is never evaluated.** Disposition: deliberate. After the revision it
   holds nothing in this set, so its current admission state cannot affect the outcome; coverage is
   computed over the intended set only. A dropped participant that had itself lapsed is simply gone.
6. **Coverage cannot tell whether the declaration is true.** Disposition: recorded as a limit rather
   than closed. CBI9 verifies that the intended set satisfies what the Component declared; it does
   not verify that the declaration is truthful or complete, so a Component that under-declares can
   have grants removed that its interaction actually relies on. Naming that boundary is the point of
   this finding.
7. **A departing participant's authority is not revoked anywhere.** Disposition: the set in force no
   longer includes it, and CBI9 claims nothing further — no revocation of its local grant elsewhere,
   no notification, and no transfer of state to the arriving participant.
8. **The provider is never told the set changed.** Disposition: unchanged from CBI6 and CBI8. No CM5
   identity, grant, evidence, or decision crosses the portable boundary, and the participant count
   stays invisible to the provider.
9. **The malformed-request and evaluated-loss split could drift from CBI8's.** Disposition: restated
   identically here and implemented over the same shared helpers within each stack — a retained
   request that does not re-identify its authority declines with the binding untouched, and a
   retained participant whose fresh outcome differs retires the member.
10. **Participant precedence is still never decided.** Disposition: coverage decides which
    participants may leave, so no participant has to be ranked above another. The question that CBI7
    and CBI8 both deferred is now closed rather than inherited.
11. **Two implementations can still agree where this contract is silent.** Disposition: retained as
    a structural limitation. The vectors force the declaration-source, empty-declaration,
    unsatisfied-declaration, uncovered-dependency, substitution, evaluated-count, in-force-size, and
    released-state answers; they cannot establish general multi-party authority lifecycle
    completeness.

## Result

The CBI9 contract is complete for fail-closed revision of one participant set under one declaration
derived from one resolved definition, over one released singleton binding. Finding 6 is the boundary
worth carrying forward: the declaration is trusted as the Component's own statement, and nothing
here checks it against what the member's ordinary interaction actually does. No finding requires
widening the contract into CM4 binding-exercise projection, cross-vocabulary Operation mapping,
multi-member or relational lifecycles, mediation, real distribution, or Architecture 0.8
conformance.
