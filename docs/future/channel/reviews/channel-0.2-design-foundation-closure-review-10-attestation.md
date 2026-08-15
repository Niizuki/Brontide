# Channel 0.2 design-foundation closure review 10 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-10-2026-08-14-c358464`

Reviewed commit: `c358464263a1131f91bc4e96b3dcc353d1fcd5b7`

Date: 2026-08-14

Overall verdict: **`does-not-conform`** — one blocking finding (**AF1**) and seven nonblocking
findings (**AF2**-**AF8**).

Every retained finding B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1-U8, V1-V3, W1-W6, X1-X7,
Y1-Y4, Z1-Z4, AA1-AA3, AB1-AB2, AC1-AC4, AD1-AD3, and AE1-AE5 is closed in the artifact it was raised
against, with two exceptions recorded below as **AF2** and **AF3**: the AE4 and AE5 corrections each
closed the first surface their finding's evidence named and left the second untouched, and both
second surfaces still read exactly as closure review 9 quoted them.

**AE1 itself is genuinely closed at the property, and re-opened at the vector.** `C4-P2`'s corrected
first conjunct is green on the lost-request vector and red on `C4-control-precedes-request` — I
reproduced both by evaluator, and reproduced review 9's finding by reverting the conjunct. But the
only two passages in the package that state what the mutation vector's expected observations *are*
still say they are the recorded refusal alone, and call that refusal "the witness `C4-P2` fails on".
The corrected conjunct also reads the recipient's subsequent admission. A vector authored from those
passages makes the property green on its own named mutation, which is U1 restored one artifact below
where AE1 was fixed.

## Isolation

Complete, with the dispatch provenance disclosed in its own section below because the policy requires
process deviations to be stated rather than left implicit.

```text
C:/b030  ->  c358464263a1131f91bc4e96b3dcc353d1fcd5b7  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | count     ->  887
files on disk (non-.git) ->  887
git diff HEAD            ->  (empty)
```

The clone materialised completely — 887 tracked paths, 887 files on disk, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was
read from `C:/b030`; all four gates available to this review were run there. The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at
any point in this session.

The reviewer identity above differs from all nine retained reviewers, from every correction author,
and from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md`
and `docs/future/channel/reviews/README.md` were both read from the clone at the pin.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect, no area of
suspicion, and no finding. It did narrow *where* this review spent effort, in two ways it is fair to
record. First, it restated the policy's instruction to write a property evaluator and to run each
property over its *required vector group*; roughly half the effort here went to C4, C10, C12, the
neutral brief, and the state/event grid, and correspondingly less to C5, C6, C7, and C11, which were
assessed by reading and cross-tracing rather than by probe. Second, it told me to verify the pin
claim myself rather than take it from the brief, which I did (see **Pin**), and that is the only
instruction that pointed at a specific check.

**AF1 was not named or hinted at by the brief.** It was reached by asking what the AE1 fix *depends
on* — the fix depends on the mutation vector carrying the recipient's subsequent admission — and then
reading the two passages that say what that vector carries. **AF2** and **AF3** were reached by
re-deriving closure review 9's own AE4 and AE5 evidence sentences against the corrected artifacts,
which is the AD pass's method (audit the records against the artifacts) turned on the newest
corrections. **AF4**-**AF8** were reached independently.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that, on its own account:

- **authored the correction commit under review**, `fix(channel): close AE1-AE5 and AD2 under the AE1
  owner ruling`, including every artifact edit and every verifier check added in it;
- **authored the immediately preceding commit** `fix(channel): close AD1 and AD3, the retained-record
  descriptions`, and the [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md)
  retained in this directory; and
- **dispatched closure review 9**, whose findings the reviewed commit corrects.

That is a closer relationship than the one disclosed for closure review 9, which was dispatched by
the author of the *preceding* commit. Here the dispatcher is the author of the very commit being
judged. It is recorded because an undisclosed relationship between a dispatcher and a reviewer is the
same class of defect as an undisclosed reviewer-repairs-own-finding, which this directory already
discloses twice.

What the dispatch did and did not carry. The brief conveyed none of the dispatching session's
findings, reasoning, or conclusions. It named no artifact defect, no area of suspicion, and nothing
about where it believed the work was weak or strong; my context contains nothing from that session
beyond the brief itself. It pointed me at `AGENTS.md` and this policy and told me to take my scope
from them, and it told me explicitly to weigh the corrections harder rather than defer to them.

What this reviewer can say about the effect, in the terms review 9 used: the brief narrowed *where*
effort went, as recorded above. It did not narrow *what* was concluded. **AF1 is a defect in the
correction commit the dispatching session wrote, in the two passages that commit did not touch**, and
the commit message's own account — "the parity profile compares that admission, and the required-green
set names the lost-request vector" — is a complete list of the surfaces that were updated for AE1 and
does not include the mutation vector's expected observations. The finding that closes this cycle is
therefore in the author's own change, in a place the author's own summary shows was not considered.

I read no retained attestation before forming AF1 except closure review 9's, which the brief directed
me to read for form. I read it after establishing the pin and before reading the design package; it
is the source of my knowledge of AE1-AE5 as findings, and it does **not** contain AF1. Its "Not
repaired here" note proposes reading the subsequent admission, which is the correction I assessed; it
says nothing about the mutation vector's expected observations.

## Pin

The policy names the current target as the commit titled
`fix(channel): close AE1-AE5 and AD2 under the AE1 owner ruling`, committed 2026-08-14, "or any later
commit whose design artifacts hash identically to it — and check that claim rather than assuming it".

I checked it, and it holds in a stronger form than claimed:

```text
git diff --stat 78e2339 c358464   ->  (empty; the whole tree is identical)
```

Per-artifact blob hashes were compared individually as well, and all thirteen `docs/future/channel/*.md`
paths — including the eight design artifacts, the two retained 0.1 documents, the requirements ledger,
and the index — are byte-identical at both commits. `c358464` is the merge of PR #120 bringing
`78e2339` to `main`; `78e2339` carries exactly the named subject and is the head of the correction
sequence beginning at `fix(channel): make C4-P2 falsifiable`. The X6 correction — checking this
sentence against the most recent commit that changed a design artifact rather than against its own
wording — holds at this pin, and the design gate passes.

This is the third cycle at which this clause has been checked and the first at which it was true of a
*later* commit rather than vacuously true of the named one, which is the case U6 and X6 were about.

## Blocking finding

### AF1 — the mutation vector's expected observations do not contain the fact the corrected first conjunct reads, so `C4-P2` is green on `C4-control-precedes-request`

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the mutation-vector passage
(the two paragraphs beginning "`C4-control-precedes-request` and `C4-outcome-precedes-ack` are
mutation vectors" and "Their expected observations are exactly what the receiving endpoint records");
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Required silence probes and
dispositions", row "control delivered before the request it names".

**What the correction did.** The AE1 correction restates `C4-P2`'s first conjunct as:

> no endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation control whose
> committing endpoint had already committed the request naming that identity **and whose recipient
> afterwards admits an interaction for that identity**

and states the mechanism explicitly: "The conjunct reads that, through a membership test over the
identities the recipient admits in the same vector." The parity profile compares "the admission of an
identity previously refused at `unseen`". Both are correct and both work — see probe **P3**.

**What it depends on.** The conjunct is now a conjunction of two recorded facts. For the mutation to
go red, the vector `C4-control-precedes-request` must carry *both*: the recipient's refusal at
`unseen`, and the recipient's later admission of that identity. The contract's own account of what
that vector carries is unchanged from before the correction and states the opposite:

> Their expected observations are **exactly** what the receiving endpoint records: one
> `rejected-protocol` for a control naming an identity the recipient has never been asked to open,
> and one late-traffic `state-violation` whose latch records the displaced acknowledgement as the
> frame that settled it. **Those recorded refusals are the witnesses `C4-P2` fails on.** Each is
> **complete data** rather than an unspecified expectation, which is what `C12` requires of every
> vector.

The completeness review's silence-probe row says the same thing in its own words, in the row that
exists for this exact scenario:

> `C4-control-precedes-request` exists as a mutation vector whose expected observation is the
> recipient's recorded `rejected-protocol` at `unseen`, **which is the witness `C4-P2` fails on**

Neither passage was touched by the AE1 correction. Both are now false of the first conjunct: the
refusal alone is not the witness the property fails on, and it is not complete data for a vector
whose compared field set now includes the admission.

**Probed, not reasoned.** Probe **P3** ran the published conjunct through an evaluator built from the
contract's prose, the brief's closed operator set, the brief's vector format, and the parity
profile's compared fields. Two rows are the finding:

| Vector | Design expects | Evaluator |
| --- | --- | --- |
| `C4-control-precedes-request`, expected observations = refusal **and** admission | red | **red** |
| `C4-control-precedes-request`, expected observations exactly as C4's mutation passage states them | red | **green** |

The membership test finds an empty admitted set and the conjunct is satisfied. This is the U1
condition verbatim — a property green on its own named mutation — arrived at through the vector
rather than through the property.

**It is worse than one missing observation.** Walking the mutation vector through both machines, the
recipient records the refusal at `unseen`, then admits the later-delivered request, dispatches, and
commits an Outcome; the initiator, which is at `cancel-pending`, receives the recipient's
interaction-scoped peer fault and reaches terminal `peer-fault` (initiator row "`dispatched`,
`cancel-pending`, `cancel-accepted`, or `cancel-refused` | valid correlated peer protocol fault"),
and the recipient's genuine Outcome for that identity then arrives at a terminal interaction and
settles the late-traffic latch (initiator row "any terminal | first duplicate semantic terminal or
late non-fault control while latch is `clear`"). At least four observations, across both endpoints,
are produced by a vector the contract describes as producing "exactly ... one". So the passage is not
one clause behind the correction; it never described the vector.

**Why this is blocking rather than editorial.** Three reasons, and the third is the one that matters.

1. The contract is the top authority — the brief states it is subordinate to the contract, both
   machines, and the grid, and AC1 was found precisely by applying that hierarchy against a fix that
   lived only in the brief. Here the fix lives in the contract's property statement and the brief's
   parity profile, and the contract's *own* description of the mutation vector contradicts it. The
   hierarchy does not resolve this one; the contract disagrees with itself.
2. The Batch 2 entry gate requires that "C1-C12, both state machines, and the closed state/event grid
   have no unresolved internal contradiction". Two paragraphs of C4 give incompatible accounts of what
   `C4-P2`'s first conjunct fails on.
3. `capability-properties.json` and the C4 vector file are authored in Batch 2 from these documents.
   The passage that a vector author reads to learn what `C4-control-precedes-request` expects is the
   passage that states it, and it states an expectation under which the property does not fail. That
   is the same failure path S1 → U1 → AC3 → AE1 have each taken: the promise is correct and the thing
   that must refute it cannot.

**Not repaired here**, per the policy. Recorded for the correction pass only, and not as a
recommendation this review is entitled to make: the distinguishing fact is already in the design and
already stated one paragraph below the conjunct — C4's retention passage says a later request bearing
the refused identity "is admitted on its own merits". Whether the mutation passage should enumerate
the vector's full observation set, or state its expected observations by reference to the compared
field set rather than by a closed list, is a design choice. It is worth noting that the phrase doing
the damage is "**exactly** ... **complete data**": a passage that claims completeness over an
enumeration is what a later reader trusts instead of deriving the set, which is AD1's mechanism and
the reason closure review 9 ruled AD2 a defect.

## Nonblocking findings

### AF2 — the second half of AE4 is uncorrected, and the Channel index now states a closed finding as open

**Artifact.** `docs/future/channel/README.md` lines 8-18 and lines 24-25.

Closure review 9's AE4 named two defects in this file. The first — the Design reviews row omitting
`AA` and `AB` — is corrected at line 50. The second is quoted here from that attestation:

> Separately, line 25 tells the reader that the pending review is "of the correction sequence that
> runs from S1 through Z4", four families short of where the sequence actually ends.

Line 24-25 at this pin reads, unchanged:

> Every artifact below awaits the same cycle: one fresh independent closure re-review, now of the
> correction sequence that runs from S1 through Z4.

It is now five families short — AA, AB, AC, AD, and AE.

Two further statements in the same document are false at this pin. Line 12-13 says "AD1 and AD3 are
corrected in the retained reviews' own descriptions of themselves, **with AD2 left as an owner
call**"; AD2 was ruled a defect by closure review 9 and corrected in the reviewed commit. And the
opening paragraph, which enumerates every family from B1 through AD, never mentions AE1-AE5 at all.
The artifact rows are stale by the same measure: the contract row stops at Z3, the interaction-machine
row at Y3, the grid row at Z2, the matrix row at U2, the completeness row at "the V-Z iteration
families", the ledger row at Z4, and the brief row at Z1.

The AA1 structural check passes because it requires only that each disposition family appear
*somewhere* in the index, and `AE` appears — inside the sentence explaining AE4, at line 50. That is
precisely the mechanism AE4 described one cycle ago.

Nonblocking: no reader is sent to reconstruct evidence, and nothing here contradicts a design fact.
Recorded because this is the fourth cycle in which this document has been raised (S3, AA1, AE4, now
AF2), because the AD2 sentence is AA2's defect — "still naming S1 as the open blocking finding four
families after it closed" — reproduced with a different identifier, and because the half of AE4 that
was left is the half a reviewer reads first: the sentence telling it what sequence it is reviewing.

### AF3 — the second half of AE5 is uncorrected, and the new disposition understates the register it inventories

**Artifacts.** `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` §"Sources inventoried",
§"Retained requirements register disposition", and §"Ledger completion check";
`architecture-0.8-channel-requirements-and-risk-ledger.md` §3.

Closure review 9's AE5 named two surfaces:

> The migration ledger's "Sources inventoried" list ... does not name the requirements ledger; **its
> completion check enumerates Shapes, fields, message kinds, states, categories, domains, limits,
> features, observation fields, the 24 vectors, the goldens, and the consumers, and does not claim
> the requirements register.**

The first is corrected. The completion check at this pin is unchanged and still does not claim the
register — so the ledger's one machine-checkable coverage statement still omits the source the
correction just added to its inventory, and the sentence closure review 9 wrote is still true word
for word.

Separately, the new disposition section opens:

> `CH-R1` through `CH-R11` and **`CH-K1` through `CH-K5`** are carried in substance by the field,
> state, category, limit, and feature tables below

The retained ledger's §3 risk register runs **`CH-K1` through `CH-K7`**. `CH-K6` (hardening asymmetry
between peers) and `CH-K7` (denial mistaken for a transported result) are outside the claimed range.
Both are answered in substance — C5 requires every effective normative bound to be exposed and
accepted at establishment, and C6/C9 keep a local denial frameless with `known-none` — which is why
this is nonblocking. It is recorded because the section exists to replace an unenumerated coverage
claim with an accurate one, and it makes a narrower coverage claim than the register it inventories.

A third, cosmetic instance in the same edit: the sources list now reads "`binding-observation.json`;
and" followed by a further "and the [requirements ledger]" item, leaving two conjunctions in one list.

### AF4 — the new-evidence inventory enumerates the observation fields the ordering vectors compare and omits the one AE1 added

**Artifact.** `Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md` §"New evidence required by
redesign", final bullet.

Z4 put intra-interaction frame order into this inventory so that "Batch 2's inventory" would not be
"silent about the one group it exists to produce". The bullet ends by enumerating the compared
evidence explicitly:

> The observation fields those vectors compare — the late-traffic latch, including its
> `not-applicable` value, and the frame that settled it with its arrival ordinal — are likewise new
> in 0.2 and have no 0.1 observation field to migrate from.

The AE1 correction added a third compared field to that group: the parity profile now compares "the
admission of an identity previously refused at `unseen`", and it is what the first conjunct reads.
The bullet was not updated. This is Z4's class applied to the newest correction, in the artifact Z4
was raised against, and it is the same relationship X1's settling frame had to this list before Z4.

Nonblocking because the fact is an ordinary admission decision that C10 already enumerates, so no
implementer has to invent a field — see probe **P6**, which was a deliberate attempt to establish the
opposite. Recorded because Batch 2 builds its vector groups from this list.

### AF5 — `C4-P2`'s required-green set names four of the seven legal members of its own required vector group

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4 "**Required green.**";
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Vector groups" (the intra-interaction frame
order group) and §"Capability-wide property format"; `build/verify-channel-0.2-design.ps1`, the
`$requiredGreen` check.

The brief defines the field as "**the** named legal inputs from the property's own required vector
group that it must not fail on", and C12 as "a named set of legal inputs it must leave green, drawn
from its own required vector group". The group's legal members are seven:

1. conforming commit-order delivery, initiator direction;
2. conforming commit-order delivery, recipient direction;
3. loss of either frame — request lost;
4. loss of either frame — acknowledgement lost;
5. a cancellation control for an identity the peer never opened;
6. a legal late control arriving after a peer's terminal;
7. a duplicate terminal from a nonconformant peer.

The required-green set names 3, 5, 6, and 7. Members 1, 2, and 4 carry no stated expectation — which
is the exact condition AE1 arose from, as C12's own new paragraph says: "the vector it failed on was
already a required member of its own group **with no stated expectation at all**." The omission of 1
and 2 is the sharpest, because a property that goes red on plain conforming delivery is the worst
available failure and is the one case the set does not name.

The verifier's check for this field tests only that the string `lost` appears within 700 characters
of "**Required green.**", so it cannot see an incomplete set. That is a narrower check than the rule
it guards, which is AD2's shape.

Nonblocking, and deliberately so: probe **P3** ran all seven and the property is green on all seven,
so there is no live defect in `C4-P2` — only in the record of what must be green. That distinction is
exactly the one that separates this from AE1, and it is why this is not blocking.

### AF6 — the AD2 replacement derives its class from one wording and still cannot see the first iteration family

**Artifact.** `build/verify-channel-0.2-design.ps1`, the `$iterationAttributions` /
`$iterationFamilies` block and the comment above it.

The replacement is a real improvement and closes what closure review 9 ruled: it derives ids from the
policy instead of testing two literals, and it now fails when the next iteration pass skips its
record. I reproduced its derivation (probe **P4**): it matches 8 attributions and derives 16 ids —
`AA1, AA3, AB1, AB2, AC1, AC4, AD1, AD3, W1, W4, X1, X7, Y1, Y4, Z1, Z4` — against 44 finding ids the
policy bolds, correctly excluding the closure-review families U and AE.

Its comment states the class it implements:

> The class is derived rather than listed: **the policy's own next-work steps say which families an
> iteration pass found**, so only those carry the retained-record obligation.

The regex requires the literal shape `Correct <ids>, found by a[n] <word> iteration pass`. Two
iteration-attributed groups in the current policy do not carry it. Step 3c is "~~Correct W5 and
W6.~~ **Done.**" with no attribution clause, and `V1`-`V3` are attributed to an iteration pass only in
the policy's status paragraph and its retained-reviews roster ("the [U1 correction iteration review]
— ... raised V1 and V2, corrected both, and recorded V3 as an owner call"). `V` is therefore a whole
iteration family the derived class does not cover, and `W5`/`W6` are two more ids, today, in the
document the check reads. The `if ($iterationFamilies.Count -lt 1)` guard catches total silence — a
wholesale wording change — and cannot catch the partial silence that is already present.

Nonblocking: every family including V does have a retained record, verified independently in probe
**P4**, so there is no live gap. Recorded because AD2 was ruled a defect for a comment claiming a
class over code that tests a subset, and the replacement's comment claims a class over code that
tests a subset — smaller by an order of magnitude, and the same shape.

### AF7 — the soundness rule AE3 added is enforced over twelve properties and the design states twenty-five

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` C12 §"Failure and uncertainty";
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Per-capability property audit";
`Brontide-Channel-0.2-Session-State-Machine-0.1.md` §"Capability-wide properties";
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §"Capability-wide properties".

C12's new rule is written over "**every** property": each must be able to fail against a named
incorrect implementation, and each "carries a named set of legal inputs it must leave green". The
audit that enforces it has twelve rows, one per C-item. The design states thirteen further
capability-wide properties under that exact heading — `S1`-`S6` in the session machine and `I1`-`I7`
in the interaction machine. None carries a required-green set. The session machine at least commits
each of its six to "a named negative probe in the neutral verifier"; the interaction machine's
`I1`-`I7` section carries no evidence sentence at all, so those seven satisfy neither half of C12's
rule.

This is AE4's mechanism rather than AE1's: a rule enforced on the surfaces the audit happens to
enumerate, with two further surfaces carrying the same kind of object under the same name. It is
distinct from the eleven `owed` cells, which are disclosed residual work inside the audit; these
thirteen are outside it and are not counted as owed anywhere.

Nonblocking. The C-properties carry the normative weight, `I1`-`I7` largely restate them at the
interaction scope, and no property here is known to be unsound. Recorded because AE3 exists to make
the soundness obligation visible and checkable, and it is visible over less than half the properties
the package states.

### AF8 — the membership operand is scoped to the vector, and interaction identity is unique only within a session

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the paragraph beginning "The
subsequent admission in the first conjunct" ("a membership test over the identities the recipient
admits **in the same vector**"); `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Observation
and parity profile" ("membership of the identity in the set the recipient admits **within the same
vector**"); C4 §"Common terms" ("**Interaction identity** — ... It is not reused **within that
session**").

The new operand's scope is the vector. Interaction-identity uniqueness is guaranteed per session, and
C4 states that "a new session has a new identity and cannot resume or inherit the replay window of an
old session". A vector containing two sessions could therefore legitimately carry the same interaction
identity twice — a refusal at `unseen` in the second session and an admission of the same identity
value in the first — and the membership test, which reads the vector rather than the session, would
return true and take the conjunct red on conforming behaviour. That is AE1's failure mode reached
through the operand's scope rather than through the missing clause.

The precedence relation W1 added does not have this problem by accident rather than by design: it is
scoped "for one endpoint and one interaction identity", also without a session qualifier, but it
compares positions within one declared stimulus sequence.

Nonblocking, and I want to be precise about why. **I could not name a required vector that triggers
it.** The vector format declares "profile and initial session/interaction state" in the singular,
which reads as one session per vector, and the reconnect cases live in C2's and the completeness
review's probes rather than in a C4 group, so `C4-P2`'s selector ("Across every C4 vector") does not
reach them today. Under `AGENTS.md`'s own standard — "a nameable trigger, or it is not a test" — this
is a gap in the operand's specification rather than a demonstrated defect, and it is recorded at that
weight.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | Fixed/negotiated equivalence is one canonical record with byte/semantic equality after canonicalization; unknown versions, required features, classes, and authority modes refuse; no implicit downgrade and no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors. The established-profile image carries the realization's per-interaction frame order declaration and refuses establishment when it is absent, and W2's point — establishment verifies the declaration is present, never true — is stated where the mutation provider is defined. Unchanged by this commit and re-verified. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain freezing the admitted set, D1's duplicate drain fatal with the first snapshot preserved and no interaction's certainty rewritten. `C2-P1` covers acceptance, rejection, and terminal monotonicity. The session totality rule does not override the named nonfatal peer-interaction-during-drain row. |
| C3 | conforms | Class, direction, and external phase are three separate exact admission inputs; `false` and `unknown` are treated identically; D3's receiver-local refusal is frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger. Channel evaluates the declared predicate without creating or advancing the phase. |
| C4 | **does not conform** | **AF1**, with **AF5** and **AF8** also against C4. The AE1 correction to `C4-P2`'s first conjunct is sound and works — probe **P3** confirms the lost-request vector is green under it and red under the pre-AE1 conjunct, and both named mutations go red when their vectors carry the facts the conjuncts read. What does not hold is C4's own account of what `C4-control-precedes-request` expects, which still names the refusal alone and calls it the witness. Everything else in C4 was verified and holds: `C4-P1`'s three clauses, the bounded finite `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, the W4 retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, and `C4-P2`'s second conjunct, which probe **P3** found red on `C4-outcome-precedes-ack` and green on both cases the design names. |
| C5 | conforms | Positional payload/authority classification, pre-dispatch parsing and bounds, no partial frame becoming a partial interaction, `known-none` on structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits must be exposed and accepted at establishment, which is also where `CH-K6` is answered. |
| C6 | conforms | Authority is evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility are each explicitly disclaimed as grants; a local denial emits no frame and records `known-none`; cross-trust carries attributable context and no Capability. `C6-P1` requires exactly one `permitted` decision to reach dispatch. |
| C7 | conforms | Matches Decision 13's recorded Option B clause for clause: the CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the `interconnected && !ready` window; the composition root initiating on the Component's behalf; no Component-to-Component binding kind; failure preventing Ready and Release and returning the actual observation to CM4. `C7-P1` forbids the interaction producing Ready or Release by itself. |
| C8 | conforms | One terminal history; cancellation acknowledgement explicitly nonterminal; R1's held control bounded at exactly one; R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating` with the latch not firing and the interaction outside the drain snapshot; T3's `cancelled`-with-no-request-in-force routed as a class. C8's statement that recipient admission is not observable from `dispatched` is what makes AE1's loss vector legal, and it is correctly unchanged by the correction. |
| C9 | conforms | Four provenance forms with an exclusivity property; an unknown peer-fault category faults the local session as `unrecognized-peer-fault` with no answering fault; loss categories observer-relative. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement. PB8's finding that both stacks fabricated a known zero is answered in C10's certainty form rather than restated here, which is the correct disposition. |
| C10 | conforms | **AE2 is closed and closes cleanly.** Both `unseen` rows now state effect certainty `known-none`, in the interaction machine and in both grid cells, and I confirmed the value is the one C10's own rule supplies — the recipient proves dispatch did not occur for an identity it never accepted. The Y1/Y2/AC2 corrections hold: the latch and its settling frame are enumerated, the `not-applicable` value is owned, the recognized-frame-that-opens-no-interaction observation exists with its detailed reason and refused frame kind, and `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. Probe **P5** attempted to falsify `C10-P1` on the AE1 loss vector and failed; probe **P6** attempted to establish a Y1 recurrence for the admission fact and failed. |
| C11 | conforms | Facets may add classes, payload forms, and stronger evidence and may not reinterpret identities, authority, the four provenance forms, or certainty; retry is a new identity with optional causal attribution; the intra-interaction ordering fact is named as the one ordering fact core owns and a facet may strengthen but not weaken it. `C11-P1` binds both halves. |
| C12 | conforms-with-nonblocking-findings | **AF7**, and **AF5** against its worked example. The AE3 correction is right and is the most valuable thing in this commit: the converse rule is stated, in the terms that make it a rule rather than a remark ("a property that cannot fail and a property that cannot stay green are the same defect measured from opposite ends"), the brief's property format carries the required-green set as a normative field, and the audit carries the column. The eleven `owed` cells are named residual work and I agree with the reasoning that refuses to guess them. What the rule does not yet reach is the thirteen `S`/`I` properties, and the one set that was written is incomplete against its own group. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Twelve legal rows, a refused/illegal table, a totality rule that does not override named nonfatal rows, and `S1`-`S6` as capability-wide properties. No external phase appears as a session state and each is listed as explicitly not one. Fixed/negotiated equivalence states that a field absent from the fixed path is a contract defect rather than realization freedom. `S1`-`S6` carry no required-green set, which is **AF7** rather than a defect in this machine. |
| Interaction state | conforms | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; `I1`-`I7` hold. The `unseen` row is a detailed row (X3) routing to `unseen` rather than to the terminal state (Y3), so the `any terminal` rows do not reclaim it, and it now carries `known-none` (AE2). The terminal-provenance table's last row gives the refusal a declared provenance. |
| State/event totality | conforms | Independently enumerated (probe **P2**): 108 published-row cells across three grids and 180 underlying state/event pairs, agreeing with the seventh, eighth, and ninth reviews. No cell is empty and no cell offers a choice between two routes. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event, the `not-applicable` latch value is compared as a value, and the AE2 addition is inside a populated cell rather than a new route. |
| Responsibility | conforms | Every row names exactly one owner identifier from the declared closed vocabulary of twenty-two (U2); consumers and carriers are separate columns; the two facts the S1/AB2 line added — intra-interaction frame order, and local observation content and provenance — each have a row whose crossing artifact names what AC1 made comparable. Enumeration found no duplicate owner, no missing owner, and no identifier used before declaration (probe **P4**). The AE1 admission fact needs no new row: it is an ordinary admission decision already inside the C10 record this matrix owns (probe **P6**). |
| Completeness | conforms-with-nonblocking-findings | The disposition history runs to the ninth independent review and records AE1-AE5 and the AD2 ruling accurately. The residual risks are stated as challenges rather than resolutions. The audit's new required-green column is the AE3 correction landing, and its `owed` cells are honest. **AF1's second instance** is in this document's silence-probe row for "control delivered before the request it names", and **AF7** is against the audit's scope. |
| Migration coverage | conforms-with-nonblocking-findings | All 24 predecessor vectors, twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. AC2's `unopened-interaction-identity` is in the closed detailed-reason set, and Z4's new-evidence inventory carries intra-interaction frame order and both mutations. **AE5's first surface is closed and its second is not (AF3)**, and the new-evidence inventory omits the field AE1 added (**AF4**). |
| Neutral brief | conforms | Artifact boundaries, identity spaces, the three-version rule, the vector format with W5's committing endpoint, the closed operator set with W1's precedence relation and Z1's identification-only restriction on the arrival ordinal, the parity profile with V1's detailed reason, X1's settling frame, and AE1's admission comparison, the golden policy, the reordering-injection provider boundary, and the Batch 2 entry gate. The brief is correctly subordinate to the contract, both machines, and the grid. Its required adversarial group is where AE1's vector came from and it now states that vector's expectation; **AF5** is against the contract's instance of the field this brief defines, not against the definition. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; the grid's cancellation columns. The completeness review's direction-scope row records the session-wide-versus-per-direction disagreement (U7) rather than hiding it, and does not contradict the ruling. |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan's ruling and in the matrix's boundary ruling; ledger `ready` → **moved** as state, message kind, and feature. No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B, including its explicit rejection of C and D and its composition-root standing-in. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's sentence that a facet may strengthen the intra-interaction ordering fact but not weaken it is the one place the S1 ruling touches this ruling, and the two are consistent. |

The plan's `## Open questions (owners needed)` section correctly reports no unresolved owner decision.
The R1 (2026-08-13), S1 (2026-08-13), and now AE1 (2026-08-14) correction rulings are each recorded as
correction rulings that do **not** join the fixed set of four, in the plan and in the review policy.
The AE1 ruling records Option A as selected with Options B and C rejected on stated grounds, which is
the form the plan uses for Decision-13-style rulings; I assessed both rejections and consider them
correctly reasoned — Option B would have made a property read harness metadata, which is the operand
class W1 narrowed the operator set to exclude, and Option C would have retired a promise to fit a
property that could not express it.

## Retained findings

Every retained finding was verified in the artifact it was raised against rather than taken from a
disposition history or an index, with phrase-level checks mechanised across all thirteen
`docs/future/channel` documents plus both indexes and the review policy, whitespace flowed (probe
**P4**). Summary:

- **B1-B4, N1-N3, F1-F3, D1-D5** — closed. Recipient frameless `refused-local`; nonterminal
  cancellation-denial transition; one owner identifier per row; five-value disposition vocabulary;
  exact Ready ownership; peer fault from `cancel-pending`; `retained` disposition with treatment
  column; `replay-detected` live window; distinct recipient `peer-fault`/`lost`; `replaced`
  cancellation Outcome; duplicate drain fatal; distinct acknowledgement states; receiver-local phase
  to `refused-local`; the three-value latch; delivery-fallback moved to its facet.
- **T1-T4** — closed. Phase refusal is never `state-violation`; `replay-detected` bound to the
  nonterminal window; `cancelled` with no request in force routed as a class. T4's stable phrase
  "fresh independent closure re-review" is present in all nine design artifacts, both indexes, and the
  review policy — twelve documents — and no superseded cycle name survives anywhere.
- **R1-R3** — closed. Held control bounded at one, local unsynchronised preconditions, separate
  `unseen` and `validating` grid rows.
- **S1-S3** — S1 closed as to ownership; S3's index staleness closed and recurring, which is **AF2**.
- **U1** — **closed as to falsifiability and as to soundness at the property, and not closed at the
  mutation vector.** See **AF1**. U2-U8 closed: vocabulary closed with the ordering row owned by
  `channel`; the brief carries the establishment declaration and the adversarial group; the
  disposition history runs past the ninth cycle; the audit registers `C4-P2` and both mutations; the
  pin clause is true at this pin and checked against the repository; the direction-scope row records
  the disagreement; the initiator pre-dispatch Local loss cell names `lost`.
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared and bounded to mutation vectors; precedence relation added and restricted to one endpoint's
  own declared steps; the reordering provider's declaration stated with the present-not-true point;
  second mutation added and placed in a required group; retention rule stated in C4, the machine, and
  the grid; committing-endpoint operand supplied; latch compared; settling frame recorded and
  compared; `not-applicable` value owned; `unseen` transition row present; recording-versus-retaining
  distinction; pin clause checked structurally; iteration reviews retained; C10 and the schema
  carrying the latch and settling frame; the refusal leaving state at `unseen`; the arrival ordinal
  restricted to identification; the grid naming a provenance as a provenance; the ordering
  requirement in the new-evidence inventory.
- **AA1-AA3, AB1-AB2** — closed as framed. Both indexes carry every disposition family, the counts are
  computed from the reviews directory and both now read 9, `channel-core` appears in no status entry
  point, the redesign plan's status block runs to AE, and the matrix owns local observation content
  and provenance. **AF2** is a fresh instance in the AA1 artifact rather than a reopening of AA1.
- **AC1-AC4** — closed. The arrival ordinal is in all four owning artifacts; the closed
  detailed-reason set carries `unopened-interaction-identity` and C10 requires the refused frame's
  kind; `C4-P2`'s subject is named as the committing endpoint in both conjuncts, and I confirmed the
  clause is load-bearing by evaluating the alternative reading; the class check matches two-letter
  families.
- **AD1-AD3** — closed, AD2 by the ruled correction. The AC residual is corrected in place; each
  retained review's scope line and roster entry name every family it records. The AD2 replacement is
  a genuine class check with the residual narrowness recorded as **AF6**.
- **AE1** — closed at the property and at the parity profile, **not closed at the mutation vector**
  (**AF1**). **AE2** — closed, in both artifacts and both grid cells. **AE3** — closed as a rule,
  with the scope gap recorded as **AF7** and the incomplete instance as **AF5**. **AE4** — first
  surface closed, second surface not (**AF2**). **AE5** — first surface closed, second surface not
  (**AF3**).

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 857 local links across 302 documents |
| `build/verify-text.ps1` | pass — 881 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised
for this review. **Green gates are not evidence of conformance**, and this review found that again:
AF1 through AF8 all sit behind a fully green design gate, as every blocking finding in this programme
has. Two of the eight sit behind checks written in the reviewed commit itself — the required-green
check cannot see an incomplete set (AF5), and the AE1 conjunct check reads the property's statement
paragraph and correctly finds the clause there, which is exactly why it cannot see that the vector the
clause depends on does not carry the fact (AF1).

### P2 — independent enumeration of the state/event grid

Enumerated from the state tables rather than from the grid's own counts, then compared.

- Session: 6 states × 6 event families = **36** cells, all populated.
- Initiator: 6 published state groups × 6 columns = **36** published cells; expanding the groups
  (`candidate`/`admitting` = 2, `dispatched`, `cancel-pending`, `cancel-accepted`, `cancel-refused`,
  and 6 terminal states — `refused-local`, `outcome-succeeded`, `outcome-failed`,
  `outcome-cancelled`, `peer-fault`, `lost`) gives 12 states × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells; expanding (`unseen`,
  `validating`, `executing`, `cancel-requested`, `cancel-refused`, and 7 terminal states —
  `refused-local`, `rejected-protocol`, `outcome-succeeded`, `outcome-failed`, `outcome-cancelled`,
  `peer-fault`, `lost`) gives 12 × 6 = **72** underlying pairs.

**108 published-row cells, 180 underlying state/event pairs**, agreeing with the eighth and ninth
reviews. No cell is empty, no cell offers two routes, and the closed-world rule ordering is
well-founded. AE2's addition is a new assertion inside a populated cell, not a new route.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`)

The policy requires at least one genuine attempt to falsify a capability-wide property, and the
handoff asks for it by evaluator rather than by reading, run over the *required vector group* rather
than the cases the capability's narrative names. An evaluator was written from the published prose of
`C4-P2` (including AC3's committing-endpoint subject and the AE1 admission clause), the brief's closed
operator set, the brief's vector format, and the parity profile's compared fields. It imports no
repository code. Precedence is implemented exactly as declared — two positions in one vector's
declared ordered stimulus sequence, one endpoint, one interaction identity — the arrival ordinal is
used only for equality, and the admission clause is a membership test over the identities the
recipient admits in the vector.

The evaluator was run twice per vector: once with the corrected conjunct, once with the pre-AE1
conjunct, so that AE1 could be reproduced rather than assumed.

| Vector | Design expects | Corrected conjunct | Pre-AE1 conjunct |
| --- | --- | --- | --- |
| conforming commit-order delivery, initiator direction | green | green | green |
| conforming commit-order delivery, recipient direction | green | green | green |
| loss of either frame — request lost, control delivered | green | **green** | **red** |
| loss of either frame — acknowledgement lost | green | green | green |
| cancellation control for an identity the peer never opened | green | green | green |
| legal late control after a peer's terminal | green | green | green |
| duplicate terminal from a nonconformant peer | green | green | green |
| `C4-control-precedes-request`, vector carrying refusal **and** admission | red | red | red |
| **`C4-control-precedes-request`, vector carrying exactly what C4's mutation passage states** | **red** | **green** | red |
| `C4-outcome-precedes-ack` | red | red | red |

Three results matter. **AE1 is real and is closed**: the third row is red before the correction and
green after, reproducing closure review 9's finding and confirming the fix. **The property is sound
over all seven legal members of its group**, which is more than its required-green set names (AF5).
And **the ninth row is AF1**: with the mutation vector's expected observations as the contract states
them, the property is green on its own named mutation.

### P4 — direct verification of retained findings, ownership, and the AD2 replacement

- Each retained finding was checked in its own artifact. Where a correction is a phrase, the check was
  mechanised across all thirteen `docs/future/channel` documents plus `docs/future/README.md` and the
  review policy, with whitespace flowed so line wrapping cannot produce a false negative. Results:
  T4's stable phrase in 12 documents and no superseded cycle name anywhere; AC1's arrival ordinal in
  9; AC2's detailed reason in the 5 artifacts that must carry it; AC3's committing-endpoint subject in
  5; AE1's admission clause in 4; AE3's converse rule in C12 and its required-green field in 6; AE5's
  `CH-R10` in 4; `channel-core` only in the passages that abolish it and in the retained S1 ruling
  text, and in zero owner-value cells.
- Every `Semantic owner` cell in the matrix draws from the declared closed vocabulary of twenty-two;
  enumeration found no identifier used before declaration and no row with two owners or none.
- The AD2 replacement was reproduced outside the verifier: 8 attributions matched, **16 ids derived**
  (`AA1, AA3, AB1, AB2, AC1, AC4, AD1, AD3, W1, W4, X1, X7, Y1, Y4, Z1, Z4`) against 44 finding ids
  the policy bolds, with the closure-review families U and AE correctly excluded. Every derived id has
  a retained record. `V` is the one iteration family the derivation does not reach, which is **AF6**.

### P5 — attempt to falsify `C10-P1` on the AE1 loss vector (negative result)

The handoff asks a reviewer to assume the mirrored question has further answers and to run each
property over its required vector group. I ran the sharpest available case at `C10-P1`: the AE1 loss
vector, which now contains both a possible post-dispatch path (the initiator reaches `dispatched`,
where "provider effects may be possible") and a `known-none` observation (the recipient's `unseen`
refusal, the value AE2 just supplied). `C10-P1` quantifies over *vectors*, not observations — "no
vector with a possible post-dispatch path records `known-none` without explicit evidence that the
handler did not begin" — so the loose quantifier is a real candidate.

**It does not fail.** The explicit evidence exists and is stated: the interaction machine's `unseen`
row records "Handler effect possible? no", and C10's own rule permits `known-none` "where the
observer proves dispatch did not occur", which the recipient does for an identity it never accepted.
The two facts belong to different endpoints and each is sound on its own evidence, which is what the
machine's "the two endpoint histories need not end with the same local label" already contemplates.
Recorded as a negative result because a failed falsification attempt is evidence and an unrecorded one
is not.

### P6 — attempt to establish a Y1 recurrence for the AE1 admission fact (negative result)

Y1's defect was a property reading a fact no observation was required to hold, and AC1's was a fix
stated only in the subordinate artifact. AE1 adds a fact the property reads and the parity profile
compares, so I looked for the same gap in C10, the brief's local-observation schema, and the
responsibility matrix — none of which was modified by the reviewed commit.

**It does not recur.** C10 already requires every attempted interaction to yield an observation
"sufficient to distinguish ... session and interaction identities, direction, class, **admission and
authority decisions**"; the brief's local-observation section already records "local provenance,
state, **admission decisions**"; and the matrix's `Local observation content and provenance` row owns
the C10 record as a whole. The admitted-identity set is derivable from observations the design already
mandates, so no new field or owner row is owed — unlike the latch and settling frame, which were
genuinely absent when Y1 was raised. I also confirmed that the prose's "afterwards" and the stated
membership mechanism coincide, because a recipient whose per-identity state is `unseen` cannot
previously have admitted that identity. The residual is the operand's session scope, recorded as
**AF8**.

### P7 — upstream consistency and clone completeness

- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with `latestRatifiedArchitecture` null. Recomputed
  SHA-256 for the architecture document, the 0.8 requirements, both stack matrices, and both stack
  plans: **all six match the registry**.
- The architecture document's own header carries the same Complete Draft status and states that
  neither implementation evidence nor an experiment changes its ratification status. Both stacks'
  READMEs state `Designed for: Brontide Architecture 0.8, Complete Draft, not ratified`. Every Channel
  0.2 artifact's `Designed for` line agrees, and no artifact treats 0.8 as ratified or claims Channel
  0.2 implementation conformance.
- Decision 13's recorded ruling (Option A retained for 0.1, Option B selected for 0.2) matches C3, C7,
  and the plan's relational-initialization ruling, including the no-verb/no-window analysis and the
  refusal to introduce a Component-to-Component binding kind.
- `conformance/channel-0.1-vectors.json` contains exactly **24** vectors, matching the ledger's
  coverage claim and the requirements ledger's `CH-R11` disposition. The retained requirements
  ledger's §2 register runs `CH-R1`-`CH-R11` and its §3 risk register `CH-K1`-`CH-K7`, which is
  **AF3**.
- PB8's blocking finding in both stacks — process loss fabricating a known zero effect count — is
  answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect.
- 887 tracked paths, 887 files on disk, empty `git diff HEAD`, clean status, HEAD at
  `c358464263a1131f91bc4e96b3dcc353d1fcd5b7`. No design artifact was read from outside the clone.

## What this verdict means

The AE corrections are the best work in this sequence and most of them land completely. AE1's ruling
is right: it found the distinguishing fact already inside the design instead of inventing one, it
rejected the two options that would have made the property read harness metadata or retired a promise,
and probe P3 confirms the conjunct now decides reordering rather than loss on all seven legal members
of its group and both named mutations. AE3 is the more valuable half — it converts "audit for
falsifiability" into "audit for both ends", writes the rule down where C12 can enforce it, and refuses
to guess the eleven sets rather than closing the finding in appearance. AE2 closes cleanly. AD2's
replacement is a real class check.

What this cycle adds is that **the layer under the fix moved sideways again**. Nine passes found the
layer under the previous correction; the AC and AD families found it several families back and in the
records about the design; AE1 found it under the *question*. AF1 is under the *evidence*: the property
is now correct, the parity profile compares what it reads, the ruling is recorded — and the one
passage that tells Batch 2 what the mutation vector contains still describes the vector the property
had before the correction, and asserts completeness while doing it. Every artifact that changed for
AE1 is correct. The finding is in an artifact that did not change and needed to.

There is a second pattern in this commit worth naming for the next cycle, because it accounts for two
of the eight findings and is mechanical rather than conceptual. Closure review 9 wrote AE4 and AE5
each with two evidence sentences naming two surfaces. In both cases the correction closed the first
surface and left the second, and in both cases the reviewer's own sentence about the second surface is
still true word for word at this pin. A correction pass that works from a finding's headline rather
than from its evidence list will reproduce that, and the checks written for both findings pass because
each was scoped to the surface that was fixed.

A reviewer inheriting this attestation should assume the same is true somewhere else and should test
it the cheap way: take each retained finding's *evidence* sentences, not its title, and re-derive each
one against the corrected artifact. That is the AD method — audit the records against the artifacts —
pointed at the newest correction rather than at the oldest, and it produced two of this review's eight
findings in a few minutes. The other direction still open is AF1's: for each fact a property reads,
ask which artifact tells Batch 2 that the vector carries it, and read that artifact.

Batch 2 remains closed. No schema, public type, package, host, provider, or encoding is authorized.
`channel-0.2-design-foundation-closure-record.md` is not created by this review and must not be,
because this verdict does not conform. The design was not repaired here: this attestation is the only
file this reviewer wrote, nothing else in the clone was modified, and nothing was committed.

## Note on the design gate

The gate results in P1 are from before this attestation existed. Retaining it makes
`build/verify-channel-0.2-design.ps1` fail with the same three failures the ninth review recorded,
shifted by one: the expected-file set names exactly nine negative attestations and four iteration
reviews, and the two computed counts in the Channel index and the future-work index are pinned to
`9`. That is the verifier working as designed, not a defect this review introduced; step 5 of the
policy's exact-next-work section is where the correction pass updates the verifier and both indexes
together.

Two notes for that pass. First, the expected-file check and both counting checks did exactly what
`AGENTS.md` asks of a guard when the ninth attestation was added, and will do it again here. Second,
if the correction pass updates the Channel index's counting sentence at line 50 without reading lines
8-25 of the same file, **AF2** survives the commit that closes it, because no check reads those lines.
