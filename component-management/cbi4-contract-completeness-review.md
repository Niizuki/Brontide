# CBI4 contract-completeness review

Date: 2026-07-30

Scope: absence review of the CBI4 canonical integrated-profile comparison contract, separate from
conformance review.

## Findings and dispositions

1. **An Active/refused bit could conceal materially different authority observations.** Disposition:
   every evaluated CM5 result contributes the SHA-256 of its complete existing CM6 canonical
   profile.
2. **An Active result could conceal different lifecycle effects or portable agreements.**
   Disposition: the profile includes every CM4 effect, stable failure category and code, member
   stage, Ready and Released state, and every stable resolution and Binding Plan fact.
3. **Locally generated correlation identities could make equivalent executions unequal.**
   Disposition: only PB7 `planId` is excluded, explicitly; scenario identity remains in the profile
   and every other plan fact remains comparison-relevant.
4. **Missing sub-results could be confused with empty or successful observations.** Disposition:
   authority, lifecycle, runtime, member, and failure absence are explicit JSON nulls.
5. **Map enumeration and native enum formatting could produce accidental byte differences.**
   Disposition: facts are ordinally sorted and all comparison tokens are explicit lowercase
   vocabulary. The first shared vector found and corrected a Reference Binding Plan fact that used
   CLR enum casing for compact-identifier spaces.
6. **A digest could be mistaken for security or general equivalence evidence.** Disposition:
   SHA-256 is an equality shorthand for exact canonical UTF-8 bytes, not a security proof; the
   claim is limited to five deterministic vectors.
7. **In-process comparison could be reported as integrated cross-process execution.** Disposition:
   CBI4 is deliberately data-only composition-root evidence. CM6 owns the existing authority
   process seam, and no integrated Component process protocol is claimed.
8. **Two implementations can still agree where this contract is silent.** Disposition: retained as
   a structural limitation. The shared vectors force both stacks to answer the named questions, but
   cannot establish contract completeness or general substitutability.

## Result

The CBI4 contract is complete for the bounded serialized-comparison slice. No finding requires
widening it into grant withdrawal, multiple participants or grants, portable Operation authority,
multi-member lifecycle, replacement, mediation, real distribution, or Architecture 0.8
conformance.
