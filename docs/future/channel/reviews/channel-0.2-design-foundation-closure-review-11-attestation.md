# Channel 0.2 design-foundation closure review 11 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-11-2026-08-15-57bb1d8`

Reviewed commit: `57bb1d85292e5a0cf948f98c146131107cff1634`

Date: 2026-08-15

Overall verdict: **`does-not-conform`** — one blocking finding (**AG1**) and four nonblocking
findings (**AG2**-**AG5**).

Every retained finding B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1-U8, V1-V3, W1-W6, X1-X7,
Y1-Y4, Z1-Z4, AA1-AA3, AB1-AB2, AC1-AC4, AD1-AD3, and AE1-AE5 is closed in the artifact it was raised
against. Of AF1-AF8, six are closed completely, and two are closed on the first surface their evidence
named and not on the second: **AF1** (blocking, **AG1**) and **AF2** (nonblocking, **AG4**).

**The blocking finding is AF1, still live in the second of the two artifacts AF1's own evidence
named.** Closure review 10 wrote AF1 against C4's mutation-vector passage *and* against the
completeness review's silence-probe row for "control delivered before the request it names", quoting
the row verbatim. C4's passage is corrected and corrected well. The silence-probe row is
**byte-identical at `c358464` and at this pin** and still says the mutation vector's expected
observation is the recipient's recorded refusal, "which is the witness `C4-P2` fails on". A vector
authored from that row takes `C4-P2` green on its own named mutation — reproduced by evaluator,
probe **P3**, row M1b. That is the U1 condition, in the artifact the previous review pointed at, in
the commit written to close it.

Closure review 10 predicted this class in terms that turned out to be exact: "A reviewer inheriting
this attestation should assume the same is true somewhere else and should test it the cheap way: take
each retained finding's *evidence* sentences, not its title, and re-derive each one against the
corrected artifact." Applied to AF1 itself, that test fails. Applied to AF2 it fails too (**AG4**).

## Isolation

Complete, with the dispatch provenance disclosed in its own section below.

```text
C:/b031  ->  57bb1d85292e5a0cf948f98c146131107cff1634  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | count     ->  888
git diff HEAD            ->  (empty)
```

The clone materialised completely — 888 tracked paths, clean status, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was
read from `C:/b031`; all four gates available to this review were run there. The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at
any point in this session.

The reviewer identity above differs from all ten retained reviewers, from every correction author, and
from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md` and
`docs/future/channel/reviews/README.md` were both read from the clone at the pin, and are the source
of this review's scope.

**Independence caveat, stated plainly, and it is weaker this cycle than last.** The dispatching brief
named no artifact defect and no area of suspicion. It did three things that narrowed this review, and
the third matters more than it did for review 10.

1. It told me to verify the pin myself rather than take it from the brief. I did (see **Pin**).
2. It restated the policy's requirement of an evaluator and of at least one genuine falsification
   attempt. Roughly half the effort here went to C4, C12, the neutral brief, the redesign plan's
   rulings, and the two entry-point indexes; C5, C6, C7, C9, and C11 were assessed by reading and
   cross-tracing rather than by probe.
3. It told me to read closure review 10's attestation for form. That attestation **names the artifact
   and section of AG1 in its own AF1 evidence block and quotes the sentence verbatim**, and its
   closing section states the general method that finds it.

**So AG1 is not a finding this reviewer's cold context located independently, and I will not claim it
was.** What is this review's own work on AG1 is establishing that the correction did not close it,
that it is still live rather than merely stale wording — by evaluator, over the required vector group,
showing the property green on its own mutation — and that no check in the design gate reads that row.
A blocking finding that a previous reviewer documented and a correction pass did not close is still a
blocking finding, but the "cold context did its own work" evidence that reviews 9 and 10 could offer
is not available here on this finding.

**AG2 and AG3 were reached independently** and are not in any retained record: AG2 by asking what the
AF8 fix *depends on* and reading the artifact the contract makes a claim about, AG3 by tracing the
2026-08-14 ruling from the contract's reference to it back to the plan that records it. **AG5** was
reached by applying AF2's method to the sibling entry point.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that, on its own account:

- **authored the correction commit under review**, `fix(channel): close AF1-AF8, the layer under the
  AE1 correction`, including every artifact edit and every verifier check in it;
- **also authored the two commits before it**, `fix(channel): close AE1-AE5 and AD2 under the AE1
  owner ruling` and `fix(channel): close AD1 and AD3, the retained-record descriptions`, and the
  [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md) retained in this
  directory; and
- **dispatched closure reviews 9 and 10**, whose findings those commits correct.

This is the same relationship the directory discloses for closure review 10 — the dispatcher is the
author of the very commit being judged — extended by one further commit and one further dispatch. It
is recorded because an undisclosed relationship between a dispatcher and a reviewer is the same class
of defect as an undisclosed reviewer-repairs-own-finding, which this directory already discloses twice.

What the dispatch did and did not carry. The brief conveyed none of the dispatching session's
findings, reasoning, or conclusions. It named no artifact defect, no area of suspicion, and nothing
about where it believed the work was weak or strong; my context contains nothing from that session
beyond the brief itself. It pointed me at `AGENTS.md` and this policy, told me to take my scope from
them rather than from the brief, and told me explicitly that I was reviewing work whose author had
arranged my review and that this was a reason to probe the corrections harder rather than defer to
them. It also stated that ten consecutive reviews returned `does-not-conform`, that this was context
rather than a target, and that a false negative carries real consequence in both directions.

**What this reviewer can say about the effect.** The brief narrowed *where* effort went, as recorded
in the caveat above. On the question of whether it narrowed *what* was concluded, this cycle gives a
more mixed answer than the last two:

- **AG1 sits inside the dispatching author's own change and was already written down by the reviewer
  that change exists to answer.** It is not a defect the author could plausibly have missed for lack
  of information — it is the second half of a two-part evidence block in the attestation the commit
  message says it read and retained. The commit message's own account of the AF1 fix — "The passage
  now states the complete record set both endpoints produce" — is singular, and the correction touched
  one passage where the finding named two.
- **AG2 contradicts an assertion the correction commit makes about its own completeness.** The commit
  scoped the membership operand to the session and the contract now asserts, in the same paragraph,
  that "the precedence relation W1 added carries the same qualifier for the same reason". It does not.
  That is a defect the dispatcher had the strongest possible opportunity to find, in a sentence it
  wrote.
- Against that, **AG1 is not independent evidence of a cold reviewer's value**, for the reason stated
  above. The next cycle should weigh the arrangement on AG2 and AG3, which are, and discount AG1 on
  this axis.

## Pin

The policy names the current target as the commit titled
`fix(channel): close AF1-AF8, the layer under the AE1 correction`, committed 2026-08-14, "or any later
commit whose design artifacts hash identically to it — and check that claim rather than assuming it,
because this clause has now gone stale twice".

I checked it against the repository rather than against the brief, and it holds in the stronger form:

```text
git log -1 --format=%s ca364df   ->  fix(channel): close AF1-AF8, the layer under the AE1 correction
git diff --stat ca364df 57bb1d8  ->  (empty)
git rev-parse ca364df^{tree}     ->  9418cb6d2e1e58749bee6142607edd597dbcbe9f
git rev-parse 57bb1d8^{tree}     ->  9418cb6d2e1e58749bee6142607edd597dbcbe9f
```

The whole tree is identical, so every design artifact hashes identically by construction. `57bb1d8` is
the merge of PR #121 bringing `ca364df` to `main`; `ca364df` carries exactly the named subject and is
the head of the correction sequence beginning at `fix(channel): make C4-P2 falsifiable`. The X6
correction — checking this sentence against the most recent commit that changed a design artifact
rather than against its own wording — holds at this pin, and the design gate passes. This is the
fourth cycle at which this clause has been checked and the second at which it was true of a later
commit rather than vacuously of the named one.

The commit date is 2026-08-14 in the policy and 2026-08-15 (+0200) in the commit's own author date.
Noted, not raised: it is a timezone artifact of a commit made shortly after midnight, and the pin is
established by subject and tree, both of which I verified.

## Blocking finding

### AG1 — AF1 is closed in C4 and not in the completeness review, so `C4-P2` is still green on its own named mutation for a vector authored from the artifact AF1's evidence named second

**Artifacts.** `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Required silence probes
and dispositions", the row `control delivered before the request it names` (line 147);
`build/verify-channel-0.2-design.ps1`, the AF1 check at the `$expectedObservations` block.

**What AF1's evidence named.** Closure review 10's AF1 opens with an **Artifacts** line naming two
documents, and its body quotes both. The first:

> `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the mutation-vector passage

The second, quoted there in full:

> The completeness review's silence-probe row says the same thing in its own words, in the row that
> exists for this exact scenario:
>
> > `C4-control-precedes-request` exists as a mutation vector whose expected observation is the
> > recipient's recorded `rejected-protocol` at `unseen`, **which is the witness `C4-P2` fails on**

and again in that attestation's Completeness area verdict: "**AF1's second instance** is in this
document's silence-probe row for 'control delivered before the request it names'".

**What the correction did.** C4's passage is corrected, and corrected well. It now reads "Their
expected observations are the complete set of records both endpoints produce under the vector, and
**not the refusal alone**", names the recipient's subsequent admission as part of that set in its own
bolded paragraph, and closes with "These recorded facts, and not the refusals alone, are the witnesses
`C4-P2` fails on." I assessed the choice not to enumerate the per-endpoint rows and consider it
correct rather than evasive: the two machines do determine them, and I walked the vector through both
to confirm it (probe **P3**, row M1c).

**What the correction did not do.** The silence-probe row is unchanged:

```text
git show c358464:...Contract-Completeness-Review-0.1.md | grep 'control delivered before'  ->  sha256 91b257e3...
git show ca364df:...Contract-Completeness-Review-0.1.md | grep 'control delivered before'  ->  sha256 91b257e3...
```

Byte-identical. The row still states the vector's expected observation in the singular, still names the
refusal alone, and still calls that refusal "the witness `C4-P2` fails on" — the exact phrase C4 now
contradicts four lines above its own property statement. The two artifacts now give incompatible
accounts of the same vector, which is the condition AF1 was raised for, moved from *inside* C4 to
*between* C4 and the completeness review.

**Probed, not reasoned.** Probe **P3** ran the published conjunct through an evaluator built from
C4's prose, the brief's closed operator set, the brief's vector format, and the parity profile's
compared fields. Three rows are the finding:

| Vector | Design expects | Evaluator |
| --- | --- | --- |
| `C4-control-precedes-request`, expected observations per the **corrected C4 passage** | red | **red** |
| `C4-control-precedes-request`, full record set per **both state machines** | red | **red** |
| `C4-control-precedes-request`, expected observation per the **completeness silence-probe row** | red | **green** |

The membership test finds an empty admitted set and the first conjunct is satisfied. The second
conjunct produces no witness on this vector either, so the property is green overall on its own named
mutation. This is U1 verbatim.

**Why this is blocking rather than editorial**, in the terms review 10 used for the same finding and
which apply unchanged:

1. The Batch 2 entry gate requires that "C1-C12, both state machines, and the closed state/event grid
   have no unresolved internal contradiction". The contradiction is now between C4 and the
   completeness review rather than within C4, and the completeness review is a first-batch artifact
   the gate's own artifact list carries and the design verifier requires to exist.
2. `capability-properties.json` and the C4 vector file are authored in Batch 2 from these documents.
   The silence-probe table is the per-scenario index a vector author reaches for when writing the
   vector for one named scenario; it is the *only* document in the package organised by scenario, and
   "control delivered before the request it names" is its row for this one. A vector authored from it
   expects one record and the property does not fail.
3. It is the same failure path S1 → U1 → AC3 → AE1 → AF1 have each taken: the promise is correct and
   the thing that must refute it cannot.

**No gate reads it.** The AF1 check added in the reviewed commit matches `Their expected observations`
against `$flowedContract` only, and asserts one required phrase and one forbidden one *in the
contract*. The string `control delivered before` does not appear anywhere in
`build/verify-channel-0.2-design.ps1`. The check written to close AF1 cannot see AF1's second
artifact, which is the same relationship AF5 recorded between the required-green check and the
required-green set.

**Not repaired here**, per the policy. Recorded for the correction pass only, and not as a
recommendation this review is entitled to make: the row's third column and its own "impossible under
C4 intra-interaction frame order" clause are unaffected; what is false is the middle clause's account
of the expected observation and its "which is the witness" apposition. Whether the row should restate
the record set, or point at C4's passage rather than paraphrase it, is a design choice — and the
second option is worth naming only because a row that paraphrases a normative passage is what went
stale here, twice, while the passage itself was corrected.

## Nonblocking findings

### AG2 — the contract asserts the precedence relation carries AF8's session qualifier; the brief's operator set does not, and a conforming two-session vector goes red without it

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the sentence ending "That is
AF8, and the precedence relation W1 added carries the same qualifier for the same reason";
`Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Capability-wide property format", the closed
operator set and the precedence paragraph below it; §"Observation and parity profile", the
settling-frame reference; `Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §"Late terminal and
control disposition".

**The claim.** The AF8 correction scoped the membership operand to the session, in the contract and in
the brief, and the contract explains why at length: interaction identity is unique only within a
session, so "a two-session vector could otherwise hold one identity refused at `unseen` in one session
and admitted in another, satisfy the test across them, and take the conjunct red on conforming
behaviour". It then asserts, in the same sentence, that the other operand is already safe:

> That is AF8, and the precedence relation W1 added **carries the same qualifier for the same reason.**

**The brief.** The precedence relation is defined in the brief's closed operator set and nowhere else:

> **precedence between two steps in one vector's declared ordered stimulus sequence, for one endpoint
> and one interaction identity**

No session qualifier. Neither does the paragraph that justifies it ("Restricting precedence to one
endpoint's own declared steps…"), nor the vector format's stimulus-step attribution (W5 gave steps a
committing endpoint and an interaction identity, not a session). The settling-frame reference that
binds a latch to a declared step carries four fields — kind, interaction identity, committing
endpoint, arrival ordinal — in the brief, in the interaction machine, and in the grid, and none of the
three carries a session. `git diff c358464 ca364df` over the brief is a single hunk, and it changes
only the membership operand. **The contract's assertion is false of the artifact it is about**, and it
is the kind of assertion AD1 was raised for: a reader auditing "is AF8 closed?" reads that sentence
and marks it closed without opening the brief.

**Probed, not reasoned.** Probe **P3** ran a wholly conforming two-session vector that reuses one
interaction identity value legitimately — C4 states identity is "not reused within that session" and
that "a new session has a new identity" — where session A delivers a cancellation acknowledgement and
then an Outcome in commit order, and session B contains required-green member 7, a duplicate terminal
from a nonconformant peer.

| Operator set | Vector | Result |
| --- | --- | --- |
| precedence **as the brief publishes it** (no session qualifier) | conforming two-session reuse | **red** |
| precedence **as the contract claims it is** (session-qualified) | conforming two-session reuse | green |

The settling frame `(outcome, X, recipient, ordinal 2)` matches session A's step as well as session
B's, and the terminal frame matches session B's as well as session A's, so the second conjunct binds
a session-A step before a session-B terminal and goes red on legal input. That is AE1's failure mode
— a property that cannot stay green — reached through the precedence operand instead of the membership
one, and it is the failure mode C12's new converse rule exists to make visible.

**Nonblocking, and I want to be precise about why**, for the same reason review 10 gave AF8. **I could
not name a required vector that triggers it.** The vector format declares "profile and initial
session/interaction state" in the singular, which reads as one session per vector; the reconnect cases
live in C2's probes and the completeness review's silence table rather than in a C4 group; and
`C4-P2`'s selector, "Across every C4 vector", does not reach them today. Under `AGENTS.md`'s own
standard — "a nameable trigger, or it is not a test" — this is a gap in the operand's specification
rather than a demonstrated defect.

It is recorded at that weight, and recorded rather than passed over, for three reasons. The gap is
identical to the one AF8 was raised for and rated at the same weight, so the design has now judged this
exact exposure worth correcting once. The contract states it is already corrected, which is worse than
silence: silence invites the next reviewer to look, and a false claim of closure invites it not to. And
if a second session ever enters a C4 vector — which the two-session case in the completeness review's
"reconnect after fault" probe makes a live direction — the trigger appears in the operand the design
believes it already fixed.

### AG3 — the redesign plan's dated AE1 owner ruling still states the operand scope AF8 corrected, with no retained-as-issued note

**Artifact.** `Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` §"Resolved questions",
**2026-08-14 — AE1 correction ruling**, Option A.

The ruling of record reads:

> The parity profile now compares that admission, and the conjunct tests membership of the identity in
> the set the recipient admits **within the vector**, which the closed operator set already permits.

`within the vector` is the scope AF8 was raised against and the reviewed commit corrected everywhere
else. The plan was touched by that commit — its status block gained a paragraph naming AF1-AF8 — and
this sentence, four hundred lines below, was not.

This matters more than an ordinary staleness because of where it sits. C4 does not merely agree with
the ruling; it *defers* to it: "it is the 2026-08-14 owner ruling recorded in the redesign plan". A
reader following that pointer to settle what the operand's scope is gets the superseded answer, stated
as the selected option of a dated owner ruling. Probe **P3** confirms the scope is not cosmetic: run
with the plan's wording, the conforming two-session refusal/admission vector goes **red** — the AF8
case exactly.

The repository already has the convention that fixes this, and applies it two rulings above. The
2026-08-13 S1 ruling names `channel-core` and carries a parenthetical: "*(The identifier recorded in
this ruling was later normalised to `channel` under U2… The owner is unchanged; the ruling text is
retained as issued.)*" The AE1 ruling carries no equivalent for AF8. Nonblocking because the contract
and the brief are the normative statements and both carry the session scope, and because a ruling is a
record of a decision rather than the operative text. Recorded because the plan is the fourth entry
point (**AB1**), the design verifier's AF8 check reads only the contract and the brief, and the AF8
regex would not match this wording in any case — it looks for "the same vector", and the plan says
"the vector".

### AG4 — AF2's third evidence sentence is uncorrected: the Channel index's seven artifact rows each stop short of the family the artifact they describe records

**Artifact.** `docs/future/channel/README.md`, the design-foundation artifact table.

Closure review 10's AF2 named three defects in this file. Two are corrected: the sequence range now
reads "S1 through **AF8**", and the AD2-as-open sentence is replaced by an accurate account naming
AE1-AE5 and AF1-AF8. The third, quoted from that attestation, is not:

> The artifact rows are stale by the same measure: the contract row stops at Z3, the
> interaction-machine row at Y3, the grid row at Z2, the matrix row at U2, the completeness row at
> "the V-Z iteration families", the ledger row at Z4, and the brief row at Z1.

All seven read at this pin exactly as review 10 described them. Against the artifacts they describe:
the contract's own status block records through AC3, the interaction machine through AC2, the grid
through AC2, the matrix through AC1, the completeness review's disposition history through AF8, the
ledger through AC2, and the brief through AC2. `git diff c358464 ca364df` over this file shows three
hunks, none of them in the table except the `9` → `10` attestation count.

Nonblocking: no reader is sent to reconstruct evidence and no design fact is contradicted. Recorded
because this is the fifth cycle in which this document has been raised (S3, AA1, AE4, AF2, now AG4);
because the AA1 structural check passes on it for the reason AE4 described — it asks only that each
family appear *somewhere* in the index; and because it is the third consecutive cycle in which a
correction pass closed the surfaces a finding's evidence named first and left the one it named last.
That pattern now accounts for AF2, AF3, AG1, and this finding.

### AG5 — the future-work index's Channel row enumerates the correction families and stops one family short

**Artifact.** `docs/future/README.md`, the `| Channel |` row in "Other planned areas".

> the 0.2 first-batch design package is complete with four resolved owner rulings and 10 retained
> independent reviews, has correction passes through U1-U8 and the author-side V1-V3, W1-W6, X1-X7,
> Y1-Y4, Z1-Z4, AA1-AA3, AB1-AB2, AC1-AC4, AD1-AD3, and **AE1-AE5** families, and awaits a fresh
> independent closure re-review

AF1-AF8 is absent from the enumeration, though the commit corrected AF1-AF8 and the document's own
Priority 1 prose above names them (lines 62-63). The counts in the same sentence are correct and were
updated. This is AA2's defect — "the longest Channel narrative of any entry point", stopping short —
in the row rather than in the prose, and it is AF2's shape in the sibling document.

Nonblocking, and mechanically it is the same class as AG4. The verifier's Channel-row check asserts
only `four resolved owner rulings` and `fresh independent closure re-review`, so the row's family
enumeration is unchecked; the AA2 family check passes because `AF` appears elsewhere in the document.
Recorded separately from AG4 because it is a different artifact with a different owner check, and a
correction pass working from AG4 alone would not open it.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | Fixed/negotiated equivalence is one canonical record with byte/semantic equality after canonicalization; unknown Channel versions, required features, classes, authority modes, and incompatible application contracts refuse; no implicit downgrade and no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors. The established-profile image carries the realization's per-interaction frame order declaration and refuses establishment when it is absent, and W2's point — establishment verifies the declaration is present, never true — remains stated where the mutation provider is defined. Unchanged by this commit and re-verified against the session machine's fixed/negotiated equivalence section. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain freezing the admitted set, D1's duplicate drain fatal with the first snapshot preserved and no interaction's certainty rewritten. `C2-P1` covers acceptance, rejection, and terminal monotonicity. The session totality rule explicitly does not override the named nonfatal peer-interaction-during-drain row. Reconnect creates a new session identity and inherits no replay window. |
| C3 | conforms | Class, direction, and external phase are three separate exact admission inputs; `false` and `unknown` are treated identically; D3's receiver-local refusal is frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger's `state-violation` row. Channel evaluates the declared predicate without creating or advancing the phase. |
| C4 | **does not conform** | **AG1**, with **AG2** and **AG3** also bearing on C4's property. The AE1 correction and the AF1 correction to C4's own mutation-vector passage are both sound and both work — probe **P3** finds the property green on all seven legal members of its required vector group and red on both named mutations, including with the complete record set both state machines determine for `C4-control-precedes-request`. What does not hold is that C4 is no longer the only artifact stating what that vector expects: the completeness review's silence-probe row still names the refusal alone and calls it the witness, and a vector authored from it takes the property green. Everything else in C4 was verified and holds: `C4-P1`'s three clauses, the finite positive `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, the W4 retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, AF5's seven-member required-green set, and both conjuncts' restriction to one endpoint's own frames, which probe **P3** confirms is load-bearing for members 6 and 7. |
| C5 | conforms | Positional payload/authority classification, pre-dispatch parsing and bounds, no partial frame becoming a partial interaction, `known-none` on structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits must be exposed and accepted at establishment, which is where `CH-K6`'s hardening asymmetry is answered. |
| C6 | conforms | Authority is evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility are each explicitly disclaimed as grants; a local denial emits no frame and records `known-none`; cross-trust carries attributable context and no Capability. `C6-P1` requires exactly one `permitted` decision to reach dispatch. |
| C7 | conforms | Matches Decision 13's recorded Option B clause for clause: the CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the `interconnected && !ready` window; the composition root initiating on the Component's behalf; no Component-to-Component binding kind; failure preventing Ready and Release and returning the actual observation to CM4. `C7-P1` forbids the interaction producing Ready or Release by itself. Decision 13's Option B wording says "a new envelope kind" and C7 uses the ordinary interaction form instead; that departure is explicit, reasoned in the completeness review, and recorded in the matrix's boundary ruling, so it preserves the semantic ruling rather than reinterpreting it. |
| C8 | conforms | One terminal history; cancellation acknowledgement explicitly nonterminal; R1's held control bounded at exactly one; R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating`; T3's `cancelled`-with-no-request-in-force routed as a class at both endpoints. C8's statement that recipient admission is not observable from `dispatched` is what makes AE1's loss vector legal, and it is correctly unchanged. |
| C9 | conforms | Four provenance forms with an exclusivity property; an unknown peer-fault category faults the local session as `unrecognized-peer-fault` with no answering fault; loss categories observer-relative. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement. PB8's finding that both stacks fabricated a known zero is answered in C10's certainty form rather than restated here. |
| C10 | conforms | AE2 remains closed in both artifacts and both grid cells; AC2's refused-frame kind and detailed reason, Y1/Y2's latch and settling frame, and Z3's `not-applicable` are all present and owned. `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. Probe **P5** attempted to falsify `C10-P1` on the AF1-corrected mutation vector — where the initiator is at `dispatched` and receives a peer fault whose detailed reason is `unopened-interaction-identity` — and failed; C10's own `known-none` rule forces the initiator to `unknown` because the initiator cannot prove dispatch did not occur, and the fault speaks to the control rather than to the request. |
| C11 | conforms | Facets may add classes, payload forms, and stronger evidence and may not reinterpret identities, authority, the four provenance forms, or certainty; retry is a new identity with optional causal attribution; the intra-interaction ordering fact is named as the one ordering fact core owns and a facet may strengthen but not weaken it. `C11-P1` binds both halves. |
| C12 | conforms-with-nonblocking-findings | **AG2** is against the operator set C12's property format governs. AE3's converse rule is stated in C12 in the terms that make it a rule, the brief's property format carries the required-green set as a normative field, and AF7's correction lands: the per-capability audit now covers all twenty-five properties the package states — twelve C-properties, `S1`-`S6`, `I1`-`I7` — which I enumerated independently (probe **P4**) and which agrees. The audit's honesty about `I1`-`I7` satisfying neither half of C12's rule is the right disposition and is disclosed residual work, not a new finding. What AG2 shows is that the rule's *converse* half is now violated in principle by the operator set the format points at, which is the first time the new rule has had something to catch. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Ten legal transition rows, a refused/illegal table, a totality rule that does not override named nonfatal rows, and `S1`-`S6` as capability-wide properties now carried in the audit (AF7). No external phase appears as a session state and each is listed as explicitly not one. Fixed/negotiated equivalence states that a field absent from the fixed path is a contract defect rather than realization freedom. |
| Interaction state | conforms | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; `I1`-`I7` hold and are now audited. The `unseen` row is a detailed row (X3) routing to `unseen` rather than to the terminal state (Y3), carries `known-none` (AE2), the refused frame kind and the detailed reason (AC2). The terminal-provenance table's last row gives the refusal a declared provenance. The settling-frame reference carries four fields and no session, which is part of **AG2**. |
| State/event totality | conforms | Independently enumerated (probe **P2**): 6×6 session, 6×6 initiator, 6×6 recipient published rows = **108 published-row cells**, zero empty; expanding the published groups against the machine's own state tables (12 initiator states, 12 recipient states) gives **180 underlying state/event pairs**. Agrees with the seventh, eighth, ninth, and tenth reviews. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event, and the `not-applicable` latch value is compared as a value. |
| Responsibility | conforms | Enumerated mechanically (probe **P4**): 39 ownership rows, every `Semantic owner` cell exactly one identifier, 22 declared identifiers, 22 distinct identifiers used, no identifier used before declaration, none declared and unused, and `channel-core` absent from the vocabulary and from all four status entry points. The two facts the S1/AB2 line added each have a row whose crossing artifact names what AC1 made comparable. The AE1 admission needs no new row: it is an ordinary admission decision inside the C10 record this matrix owns (probe **P6**). |
| Completeness | **does not conform** | **AG1** is in this document. The disposition history is otherwise accurate and now runs to the tenth independent review, recording AF1-AF8 correctly; the residual risks are stated as challenges rather than resolutions; the property audit's AF7 correction is complete over all 25 properties and its `owed` cells are honest. The one row that did not move is the one AF1's evidence named, and it is the row for the scenario the blocking finding is about. |
| Migration coverage | conforms | All 24 predecessor vectors dispositioned CH-01 through CH-24 in order (verified against `conformance/channel-0.1-vectors.json`, which holds exactly 24), twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. **AF3 is closed on both surfaces**: the completion check now claims the retained register, and the disposition range reads `CH-R1`-`CH-R11` and `CH-K1`-`CH-K7`, which I verified against the register itself (`CH-R` highest = 11, `CH-K` highest = 7). **AF4 is closed**: the new-evidence inventory names the admission. |
| Neutral brief | conforms-with-nonblocking-findings | **AG2** is against this document's operator set, and it is a gap in what the brief states rather than a contradiction of a superior artifact — the contract asserts a qualifier the brief does not carry, and under the hierarchy the brief is subordinate, so the contract's *claim* is the wrong half. Everything else holds: artifact boundaries, identity spaces, the three-version rule, the vector format with W5's committing endpoint, the closed operator set with W1's precedence relation and Z1's identification-only restriction on the arrival ordinal, the parity profile with V1's detailed reason, X1's settling frame, AE1's admission comparison and AF8's session scope on it, the required-green set as a normative format field, the golden policy, the reordering-injection provider boundary with W2's present-not-true point, and the Batch 2 entry gate. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; the grid's cancellation columns. The completeness review's direction-scope row records the session-wide-versus-per-direction disagreement (U7) rather than hiding it, and does not contradict the ruling. |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan's ruling and in the matrix's boundary ruling; ledger `ready` → **moved** as state, message kind, and feature. No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B including its explicit rejection of C and D and its composition-root standing-in, with the envelope-kind departure disclosed and reasoned. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's sentence that a facet may strengthen the intra-interaction ordering fact but not weaken it is the one place the S1 ruling touches this ruling, and the two are consistent. |

The plan's `## Open questions (owners needed)` section correctly reports no unresolved owner decision.
The R1 (2026-08-13), S1 (2026-08-13), and AE1 (2026-08-14) correction rulings are each recorded as
correction rulings that do **not** join the fixed set of four, in the plan and in the review policy.
I assessed the AE1 ruling's two rejected options and consider both correctly reasoned. **AG3** is
against that ruling's text, not against its substance or its standing.

## Retained findings

Every retained finding was verified in the artifact it was raised against rather than taken from a
disposition history or an index. Where a correction is a phrase, the check was mechanised across all
twelve `docs/future/channel` documents plus `docs/future/README.md`, with whitespace flowed so line
wrapping cannot produce a false negative (probe **P4**). Summary:

- **B1-B4, N1-N3, F1-F3, D1-D5** — closed. Recipient frameless `refused-local`; nonterminal
  cancellation-denial transition; one owner identifier per row; five-value disposition vocabulary;
  exact Ready ownership; peer fault from `cancel-pending`; `retained` disposition with treatment
  column; `replay-detected` live window; distinct recipient `peer-fault`/`lost`; `replaced`
  cancellation Outcome; duplicate drain fatal; distinct acknowledgement states; receiver-local phase
  to `refused-local`; the three-value latch; delivery-fallback moved to its facet.
- **T1-T4** — closed. Phase refusal is never `state-violation`; `replay-detected` bound to the
  nonterminal window; `cancelled` with no request in force routed as a class. T4's stable phrase is
  present in all twelve documents and no superseded cycle name survives in any status block.
- **R1-R3** — closed. Held control bounded at one, local unsynchronised preconditions, separate
  `unseen` and `validating` grid rows.
- **S1-S3** — S1 closed as to ownership. S3's index staleness closed and recurring, which is **AG4**
  and **AG5**.
- **U1** — **closed at the property, closed at C4's vector passage, and not closed at the completeness
  review's account of the same vector.** See **AG1**. U2-U8 closed: vocabulary closed with the ordering
  row owned by `channel`; the brief carries the establishment declaration and the adversarial group;
  the disposition history runs past the tenth cycle; the audit registers `C4-P2` and both mutations;
  the pin clause is true at this pin and checked against the repository; the direction-scope row
  records the disagreement; the initiator pre-dispatch Local loss cell names `lost`.
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared and bounded to mutation vectors; precedence relation added and restricted to one endpoint's
  own declared steps; the reordering provider's declaration stated with the present-not-true point;
  second mutation added and placed in a required group; retention rule stated in C4, the machine, and
  the grid; committing-endpoint operand supplied; latch compared; settling frame recorded and compared;
  `not-applicable` owned; `unseen` transition row present; recording-versus-retaining distinction; pin
  clause checked structurally; iteration reviews retained; C10 and the schema carrying the latch and
  settling frame; the refusal leaving state at `unseen`; the arrival ordinal restricted to
  identification; the grid naming a provenance as a provenance; the ordering requirement in the
  new-evidence inventory.
- **AA1-AA3, AB1-AB2** — closed as framed. Both indexes carry every disposition family, both computed
  counts read 10, `channel-core` appears in no status entry point, the redesign plan's status block
  runs to AF, and the matrix owns local observation content and provenance. **AG4** and **AG5** are
  fresh instances in the AA1 and AA2 artifacts rather than reopenings.
- **AC1-AC4** — closed. The arrival ordinal is in all four owning artifacts; the closed
  detailed-reason set carries `unopened-interaction-identity` and C10 requires the refused frame's
  kind; `C4-P2`'s subject is the committing endpoint in both conjuncts; the class check matches
  two-letter families.
- **AD1-AD3** — closed, AD2 by the ruled correction which AF6 then replaced with a declared table.
- **AE1-AE5** — closed. AE1 at the property, the parity profile, and now the contract's vector
  passage; AE2 in both artifacts and both grid cells; AE3 as a rule with the audit column; AE4 on
  both surfaces (the index narrative is now accurate about AA/AB and about AE/AF); AE5 on both
  surfaces (sources inventory and completion check).
- **AF1** — **not closed**; see **AG1**. **AF2** — closed on two of three surfaces; see **AG4**.
  **AF3** — closed on both surfaces, verified against the register's own highest ids. **AF4** —
  closed. **AF5** — closed; all seven legal members named and probe **P3** confirms the property is
  green on all seven. **AF6** — closed; the provenance table classifies 19 families, every family the
  policy bolds is classified, and every `iteration` family including `V` has a retained record (probe
  **P4**). **AF7** — closed; all 25 properties audited. **AF8** — closed at the membership operand in
  both normative artifacts, **not** at the precedence operand the same paragraph claims it reached
  (**AG2**), and **not** in the ruling of record (**AG3**).

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 859 local links across 303 documents |
| `build/verify-text.ps1` | pass — 882 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised
for this review. **Green gates are not evidence of conformance**, and this review found that again:
AG1 through AG5 all sit behind a fully green design gate. AG1 sits behind the check written in the
reviewed commit *specifically to close AF1* — that check reads `$flowedContract` and the string
`control delivered before` appears nowhere in the verifier, so the check cannot see AF1's second
artifact. AG2 sits behind the AF8 check, which reads the membership phrase in the contract and the
brief and never the operator set the same paragraph makes a claim about.

### P2 — independent enumeration of the state/event grid

Parsed from the grid's three tables and cross-checked against the interaction machine's own state
tables rather than against the grid's prose counts.

- Session: 6 rows × 6 event columns = **36** cells.
- Initiator: 6 published state groups × 6 columns = **36** published cells; the machine states 12
  initiator states (6 terminal), giving 12 × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells; the machine states 12 recipient
  states (7 terminal), giving 12 × 6 = **72** underlying pairs.

**108 published-row cells, 0 empty, 180 underlying state/event pairs** — agreeing with the seventh,
eighth, ninth, and tenth reviews. No cell offers a choice between two routes, and the closed-world
rule ordering is well-founded.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`)

The policy requires at least one genuine attempt to falsify a capability-wide property, by evaluator
rather than by reading, run over the *required vector group* rather than the cases the capability's
narrative names. An evaluator was written from the published prose of `C4-P2` (both conjuncts, AC3's
committing-endpoint subject, the AE1 admission clause and AF8's session scope), the brief's closed
operator set, the brief's vector format, the parity profile's compared fields, and the settling-frame
reference as the brief, the machine, and the grid state it. It imports no repository code. Precedence
is implemented exactly as the brief declares it — two positions in one vector's declared ordered
stimulus sequence, for one endpoint and one interaction identity — and the arrival ordinal is used only
for equality.

Each vector was run under three operand configurations, so that the design's claims about its own
operands could be tested rather than assumed.

| Vector | Design expects | As published | Precedence session-qualified (as C4 claims) | Membership vector-scoped (as the plan's ruling states) |
| --- | --- | --- | --- | --- |
| 1. conforming commit-order delivery, initiator direction | green | green | green | green |
| 2. conforming commit-order delivery, recipient direction | green | green | green | green |
| 3. loss of either frame — request lost, control delivered | green | green | green | green |
| 4. loss of either frame — acknowledgement lost | green | green | green | green |
| 5. cancellation control for an identity the peer never opened | green | green | green | green |
| 6. legal late control after a peer's terminal | green | green | green | green |
| 7. duplicate terminal from a nonconformant peer | green | green | green | green |
| M1. `C4-control-precedes-request`, expected obs per the **corrected C4 passage** | red | red | red | red |
| M1c. same vector, **full record set both state machines determine** | red | red (conjunct 1; conjunct 2 correctly green) | red | red |
| **M1b. same vector, expected obs per the completeness silence-probe row** | **red** | **green** | **green** | **green** |
| M2. `C4-outcome-precedes-ack` | red | red | red | red |
| **P. conforming two-session identity reuse + required-green member 7** | **green** | **red** | green | green |
| **P2. conforming two-session reuse, refusal in one session, admission in the other** | **green** | green | green | **red** |

Five results matter.

1. **The AE1 and AF1 property-level corrections are real and complete.** The property is green on all
   seven legal members of its required vector group — more than the four AF5 found named, and now all
   seven are named — and red on both named mutations.
2. **M1c is a falsification attempt that failed, and is worth recording.** Walking
   `C4-control-precedes-request` through both machines produces four records, not the two C4's
   narrative sentences describe: the recipient's refusal, its later admission/dispatch/Outcome, the
   initiator's terminal `peer-fault` from the recipient's correlated fault, and the initiator's
   late-traffic latch settled against the recipient's genuine Outcome. That fourth record makes the
   *second* conjunct evaluate on the first conjunct's mutation. It stays green, because the recipient
   committed the settling Outcome *after* the peer fault that made the interaction terminal. Had it
   gone red the property would fail for the wrong reason on its own vector, which is AE1's class. It
   does not.
3. **M1b is AG1.** With the expected observation the completeness review still states, the property is
   green on its own named mutation.
4. **P is AG2.** Under the operator set as the brief publishes it, wholly conforming two-session
   behaviour goes red. Under the session-qualified relation C4 claims the brief carries, it goes green.
5. **P2 is AG3.** Under the membership scope the plan's dated ruling still states, the AF8 case goes
   red on conforming behaviour — confirming the AF8 fix is load-bearing and that the ruling text
   records the superseded scope.

### P4 — direct verification of retained findings, ownership, provenance, and the audit

- Each retained finding was checked in its own artifact. Phrase checks were mechanised across all
  twelve `docs/future/channel` documents plus `docs/future/README.md`, whitespace flowed. Results:
  T4's stable phrase in 12 documents and no superseded cycle name in any status block; AC1's arrival
  ordinal in 9; AC2's detailed reason in the 5 artifacts that must carry it; AC3's committing-endpoint
  subject in 5; AE1's admission clause in 4; AE3's converse rule in C12 and its required-green field
  in 6; AE5's `CH-R10` in 4; AF1's "complete set of records both endpoints produce" in 1 — the
  contract, and **not** the completeness review, which is AG1; AF8's session scope in 2;
  `channel-core` in zero owner-value cells and zero status entry points.
- **Responsibility matrix, enumerated:** 39 ownership rows, 22 declared identifiers, 22 distinct
  identifiers used, zero rows with two owners or none, zero used-but-undeclared, zero
  declared-but-unused, `channel-core` absent.
- **AF6 provenance table, reproduced outside the verifier:** the table classifies 19 families; the
  policy bolds 12; every bolded family is classified; every family classified `iteration` — V, W, X,
  Y, Z, AA, AB, AC, AD — has at least one retained iteration review recording it, including `V`,
  which the derivation AF6 replaced could not reach. The declared-and-totality-checked form is a
  genuine improvement over the derived one and closes AD2 and AF6 together.
- **AF7 audit, enumerated:** 12 capability rows plus 13 state-machine rows = 25 audited, against 12
  C-properties + 6 `S` + 7 `I` = 25 stated. Complete. 31 `owed` cells, all disclosed.
- **AF3 register range, computed from the register:** `CH-R` highest = 11, `CH-K` highest = 7; the
  ledger's disposition claims both, and its completion check now names the register.

### P5 — attempt to falsify `C10-P1` on the AF1-corrected mutation vector (negative result)

The sharpest available case at `C10-P1` after the AF1 correction: `C4-control-precedes-request` now
explicitly contains a recipient that *does* dispatch for the refused identity. The initiator is at
`dispatched` — "provider effects may be possible", a possible post-dispatch path — and then receives
the recipient's correlated peer fault whose detailed reason is `unopened-interaction-identity`. The
initiator's machine row permits `known-none` "only when fault explicitly proves handler did not begin".
If that fault counts as such proof, the initiator records `known-none` on a vector where the handler
did begin, and `C10-P1` fails.

**It does not fail.** C10's own rule is the authority and it is written over the *observer*:
`known-none` only "where the observer proves dispatch did not occur or the declared handler did not
begin". The initiator cannot prove dispatch did not occur — it committed the request to the seam
itself — and the fault it received speaks to the *cancellation control*, an identity the recipient had
not opened when it refused that frame, not to the request the recipient had not yet received. The row's
conditional therefore resolves to `unknown`, which is correct in both the reordering case and the loss
case the initiator cannot distinguish from it. Recorded as a negative result because a failed
falsification attempt is evidence and an unrecorded one is not.

### P6 — attempt to establish a Y1/AB2 recurrence for the AE1 admission fact (negative result)

AE1 added a fact `C4-P2` reads and the parity profile compares, so I looked for the Y1 gap — a
compared field no observation is required to hold — and the AB2 gap — a load-bearing fact with no
owner row — in C10, the brief's local-observation schema, and the responsibility matrix.

**It does not recur.** C10 already requires every attempted interaction to yield an observation
sufficient to distinguish "session and interaction identities, direction, class, **admission and
authority decisions**"; the brief's local-observation section records "local provenance, state,
**admission decisions**"; and the matrix's `Local observation content and provenance` row owns "the
C10 local observation record" as a whole, with its enumerated inclusions introduced by "including"
rather than as a closed list. The admitted-identity set is therefore derivable from observations the
design already mandates, unlike the latch and settling frame, which were genuinely absent when Y1 was
raised. I reached the same conclusion review 10 reached by the same route and record it as
independently re-derived rather than inherited.

### P7 — upstream consistency and clone completeness

- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with `latestRatifiedArchitecture` null.
  **I recomputed SHA-256 for all eleven registry-pinned paths** — the architecture document, the 0.5
  implementation baseline requirements, the 0.8 requirements, both stacks' 0.5 matrices, both stacks'
  0.8 matrices, both stack READMEs, and both stack milestone-evidence ledgers — and **all eleven match
  the registry**.
- The architecture document's own header carries the same Complete Draft status and states that
  neither implementation evidence nor an experiment changes its ratification status. Both stacks'
  READMEs state `Designed for: Brontide Architecture 0.8, Complete Draft, not ratified`. The Channel
  0.2 contract states `Designed for: Brontide Architecture 0.8, Complete Draft` and the plan states
  `Designed against: Brontide Architecture 0.8, Complete Draft`; no artifact treats 0.8 as ratified or
  claims Channel 0.2 implementation conformance.
- Decision 13's recorded ruling (Option A retained for 0.1, Option B selected for 0.2, C and D
  rejected) matches C3, C7, and the plan's relational-initialization ruling, including the
  no-verb/no-window analysis, the composition-root standing-in, and the refusal to introduce a
  Component-to-Component binding kind.
- `conformance/channel-0.1-vectors.json` contains exactly **24** vectors, matching the ledger's
  coverage claim; the retained requirements ledger's registers run `CH-R1`-`CH-R11` and
  `CH-K1`-`CH-K7`, both now within the ledger's claimed range.
- PB8's blocking finding in both stacks — process loss fabricating a known zero effect count — is
  answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect.
- 888 tracked paths, empty `git diff HEAD`, clean status, HEAD at
  `57bb1d85292e5a0cf948f98c146131107cff1634`. No design artifact was read from outside the clone.

## What this verdict means

The AF corrections are largely good work and six of the eight land completely. AF5's seven-member
required-green set, AF6's declared and totality-checked provenance table, and AF7's extension of the
audit to all twenty-five properties are each better than the finding asked for: AF6 replaces a
derivation with a declaration and makes the obligation total, and AF7's honesty about `I1`-`I7`
satisfying neither half of C12's rule is the kind of disclosure that costs the author something. The
AF1 fix to C4 is right, including its reasoned decision not to enumerate per-endpoint rows, which I
tested by walking both machines rather than by taking its word.

What this cycle adds is that **the layer under the fix did not move this time — the fix simply did not
reach the whole of what the finding named.** AE4 and AE5 each named two surfaces and each was closed on
one; review 10 raised that as AF2 and AF3, named the pattern in its closing section, and told the next
reviewer to test for it by re-deriving each finding's *evidence sentences* rather than its title. That
test, applied to the reviewed commit, fails on AF1 and on AF2 — the same class, the third consecutive
cycle, and now including the blocking finding. The correction that closed AF1's first surface was
accompanied by a check that reads only that surface.

There is a second pattern this cycle names for the first time, and it is the one worth carrying
forward. AF8's correction was accompanied by a sentence asserting that the sibling operand was already
safe. It is not, and the assertion is what stands between a reader and finding out. This is AD1's
mechanism — trusting a claim about a document over the document — arriving inside a correction rather
than inside a retained record, and it is more dangerous there, because a retained record is read once
by the next reviewer while a contract paragraph is read by everyone who authors from it. **For each
claim a correction makes about an artifact it did not edit, open that artifact.** That single question
produced AG2 and AG3, which are this review's independently reached findings, and neither would have
been visible from the diff.

Batch 2 remains closed. No schema, public type, package, host, provider, or encoding is authorized.
`channel-0.2-design-foundation-closure-record.md` is not created by this review and must not be,
because this verdict does not conform. The design was not repaired here: this attestation is the only
file this reviewer wrote, nothing else in the clone was modified, and nothing was committed.

## Note on the design gate

The gate results in **P1** are from before this attestation existed. Retaining it makes
`build/verify-channel-0.2-design.ps1` fail with the same three failures the ninth and tenth reviews
recorded, shifted by one: the expected-file set names exactly ten negative attestations and four
iteration reviews, and the two computed counts in the Channel index and the future-work index are
pinned to `10`. That is the verifier working as designed, not a defect this review introduced; step 5
of the policy's exact-next-work section is where the correction pass updates the verifier and both
indexes together.

Three notes for that pass. First, if it updates the Channel index's counting sentence and the
design-foundation narrative without reading the **artifact table below them**, **AG4** survives the
commit that closes it, because no check reads those rows — which is the warning review 10 wrote about
this same file, one paragraph up from where it went unheeded. Second, the same is true of the
future-work index's `| Channel |` row (**AG5**): its verifier check asserts two phrases and never the
family enumeration. Third, the check that closes **AG1** must read the completeness review, not the
contract; a check scoped to the artifact that was already corrected is what let this finding survive
its own correction.
