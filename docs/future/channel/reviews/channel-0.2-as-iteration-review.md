# Channel 0.2 seventh W1-W3 verification-foundation iteration review

Reviewer identity: `agent:codex-gpt-5.6-sol-channel-0.2-condition-4-seventh-pass-2026-08-31-7bf34a1`

Reviewed work: the verification-foundation work done under the closure-cycle hold — W1 (the owned
facts and their gates), W2 (the twenty-six executable properties), W3 (the status blocks and Channel
index rows), the retained guard corpus, the coverage instrument, and the AR corrections — at
`7bf34a1`, `Merge pull request #138 from Niizuki/codex/adopt-bulwark-agent-guidance`; raised and
corrected AS1-AS6

Date: 2026-08-31

**This is an iteration review, not an attestation.** It ran in the working repository and its actor
corrected what it found. Under [two kinds of review](./README.md#two-kinds-of-review), it **does not
close the first batch, does not authorize Batch 2**, does not produce the closure record, and does not
supply the conforming verdict the Closure section requires.

It is the **seventh** pass condition 4 of the
[verification foundation plan](../Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md#3-how-the-hold-ends)
names. **Condition 4 is not met by it**, because it found six defects and corrected them.

## Method

The pass began from the current `origin/main`, fetched immediately before the work and verified at
`0 0` against the branch head. It ran all **64 of 64** retained probes and then the coverage gate:
every conditional in the three covered design gates was evaluated by a passing run, with ten declared
exemptions.

It then treated that result as the floor the AR review says it is. The audit searched every remaining
numeric proximity bound in the design, properties, facts, guard, and coverage gates and classified it
by direction: a truncated positive assertion fails loudly, while a truncated negative assertion
silently reports the forbidden text absent. It also opened compound recognizers whose condition ran
but whose individual operands the line trace cannot distinguish.

AS1-AS5 were each added to `conformance/channel-0.2-guard-probes.json` first and observed failing in
the harness because the gate returned `pass`; only then was its gate corrected. Two suspected cases
were withdrawn after their probes passed immediately: the per-session profile distribution and the
dated AE1-ruling boundary already fail closed under existing checks.

## Findings

### AS1 — AJ6 still used a 600-character negative window — corrected

The facts gate rejects wording such as “the first three” beneath a rendered frame-reference field
list, because inserting a field renumbers the sentence while every fence remains correct. Its check
read only 600 characters after the fence. The probe placed the forbidden wording 1,125 characters
after the fence but in the same paragraph; the facts gate returned `pass`.

Corrected by bounding the assertion at the publication paragraph. The paragraph is the subject the
comment claims, so growth inside it cannot move forbidden wording outside the check.

### AS2 — the unfenced-publication sweep used a 200-character negative window — corrected

The abbreviated-publication backstop looked for a reference phrase followed within 200 characters by
four of its five field names. A single sentence may explain the record before enumerating those
fields, so the probe spread the four names beyond 200 characters in the same sentence and the facts
gate returned `pass`.

Corrected by checking the sentence that contains each reference phrase. An intermediate paragraph
scope was deliberately rejected after the clean package itself produced four false positives: prose
about a reference can name several fields across separate sentences, while a publication enumerates
them in one.

### AS3 — retained-review denials read only the preceding 160 characters — corrected

The design gate prevents a retained review from saying a finding family has no retained iteration
record when that record exists. It looked 160 characters before “no retained iteration review”. The
probe kept `W1-W6` and the denial in one sentence, inserted an explanatory clause between them, and
the design gate returned `pass`.

Corrected by reading the sentence containing the denial. That is the extent the guard's own comment
names.

### AS4 — the duplicate-measure sweep read a 320-character context — corrected

The design gate prevents maintained indexes from restating the verification plan's status-block line
measure. It looked 160 characters on either side of a numeric “lines” claim. The probe put “status
block” and `2,717 lines` in one sentence farther apart than that window, and the gate returned `pass`.

Corrected by reading the sentence containing the numeric claim. The measure owner remains the
verification plan; retained review evidence remains outside the sweep.

### AS5 — the per-session fact recognizer joined words across at most 24 characters — corrected

The design gate audits a property that reads a C12-declared per-session fact and requires the property
to name its session. The trigger was the fact's words joined by at most 24 characters. The probe made
`I5` read the same established finite bound without a session qualifier, separated `established` from
`finite bound` by an explanatory phrase, and the design gate returned `pass`.

Corrected by joining the fact's words within the punctuation-bounded clause instead of within a
character count. The clause is already the unit used to decide whether a session qualifier governs
the fact.

### AS6 — the widened fact recognizer rejected a correctly quantified property — corrected

The AS5 correction made the clean package fail on `C2-P1`. The property says “Every accepted session
transition” before reading the prior and terminal session state, but the qualifier recognizer did not
recognize that universal quantifier. This was read from the gate's failure rather than reasoned away.

Corrected by recognizing `every accepted session` as a session qualifier. The unmodified capability
contract is the permanent positive case; AS5 is the paired negative case.

## What this pass verified rather than believed

- The five new negative probes were red before their fixes and green afterward.
- The clean facts gate rejected the first paragraph-scoped AS2 correction, identifying four passages
  where separate sentences discuss a reference without publishing it; sentence scope made the clean
  package green while retaining the AS2 refusal.
- The clean design gate rejected the first AS5 correction on `C2-P1`; adding its existing universal
  session quantifier made the package green without weakening AS5.
- The two withdrawn probes failed the underlying gate immediately, so neither became a finding or a
  retained corpus entry.

## What remains outside the pass

The retained coverage instrument still measures the design, properties, and facts gates, not the
guard harness or the coverage gate itself. It also reports whether a compound condition ran, not
whether every operand influenced a verdict. The next pass inherits those two named surfaces rather
than another search for character-bounded negative assertions in the three covered gates.

The closure review remains on hold. The finding count by condition-4 pass is now three, six, three,
two, five, one, **six**; a pass with findings cannot satisfy condition 4 even when all findings are
corrected.

## Where this family is dispositioned

AS is a `verification` family. Its corrections reach the verification gates and their retained probe
corpus, not any first-batch design artifact. Under the 2026-08-20 routing ruling, its disposition lives
in the verification foundation plan; the review policy's provenance table declares it on both axes.
