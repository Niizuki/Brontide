# CBI62 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI62 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C6.

## Findings closed in the contract

- **CBI61 broke CBI48 and neither slice's tests could see it.** CBI61 added two cycle codes; CBI48
  validates a committed code against the four it knew. A governed cadence reporting
  `provider-trust-cycle-authority-behind` was refused as `durable-cadence-result-invalid` and left
  in-flight, so a run that completed normally was recorded as an interruption that never happened —
  and the host's next start would demand a reconciliation decision about nothing. CBI61's suite never
  composed with a journal and CBI48's never produced a governed code, which is the shape of a defect
  that lives in the seam between two slices rather than in either.
- **A list of today's codes is not the guard the defect asks for.** Adding the two missing strings to
  the journal would repair this instance and leave the next one. The producers and the journal now
  draw from one vocabulary, so a code cannot be returned by a cycle and refused by the journal, and a
  named test walks the vocabulary rather than naming six codes. A stray code is still refused, so the
  repair did not become permissive.
- **The item's premise was wrong, and stating why is the capability.** It expected the journal to
  record which of the two loops a resumed cycle had run. A marker written after the rotation returns
  is not atomic with the rotation's effect, so it opens a second indeterminate window instead of
  closing the first; and the rotation's effect is already durably recorded in the retained chain and
  the stored floor, so a marker could only be a less trustworthy copy of a record that exists. The
  absent field is the contract, as it was for CBI17's synchronous succession and CBI18's absent
  declaration.
- **The absence needed a test that can fail, not a sentence.** Two runs identical in every
  journal-visible respect and differing only in whether the rotation reached its endpoint must
  produce byte-identical journals while their checkpoints differ. A journal that recorded the loop
  would fail it, and a harness whose two arms did not actually differ would fail the checkpoint half —
  which is what makes the equality meaningful rather than vacuous.
- **Retry safety is a claim about two dependencies, so it is probed rather than reasoned.** A retried
  governed cycle re-runs both loops, and neither can double-apply: CBI57 requires a rotation's
  generation to be exactly one past the active one and CBI37 requires an update's sequence to be
  exactly one past the current one. Both the honest path — the host's own cursor moved, so the
  endpoints answer that it is current — and the defensive path — a stale endpoint re-offers the
  identical statement and update — are exercised, because only the second shows the refusal doing any
  work.
- **The run's outcome must not become the cycle's.** Both new codes commit `durable-cadence-stopped`,
  and the cause stays in the committed observation. This is CBI43's rule about not renaming a refusal
  applied one level up, and every vector checks it.

## What the phase deliberately does not decide

Whether an indeterminate governed cycle *should* be retried is CBI49's reconciliation decision. What
this slice supplies is the fact that decision needs: the retry cannot double-apply either half. The
sweep's effects remain the ones CBI48 says no local journal can commit atomically with its own cursor,
and nothing here changes that.

## Residual limits

The vocabulary guard binds the journal to the cycle codes; it does not bind CBI49's or CBI50's
observation vocabularies, which remain separate lists a later slice could let drift the same way.
Cross-process ownership of a governed run and privileged custody of either floor remain separate. The
next bounded implementation boundary is reconciling a governed interruption through CBI49, where the
evidence must name which of the two loops the host has verified rather than asserting both.
