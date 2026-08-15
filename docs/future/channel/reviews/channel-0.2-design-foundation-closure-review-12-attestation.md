# Channel 0.2 design-foundation closure review 12 attestation

Reviewer identity: `agent:claude-opus-5-channel-0.2-closure-review-12-2026-08-15-f451f55`

Reviewed commit: `f451f557ec51b9b878ddc0476c1cc7e0bd836679`

Date: 2026-08-15

Overall verdict: **`conforms-with-nonblocking-findings`** — **no blocking finding**, and six nonblocking
findings (**AH1**-**AH6**).

Every retained finding B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3, U1-U8, V1-V3, W1-W6, X1-X7,
Y1-Y4, Z1-Z4, AA1-AA3, AB1-AB2, AC1-AC4, AD1-AD3, AE1-AE5, AF1-AF8, and AG1-AG5 is closed in the
artifact it was raised against, with one exception recorded below as **AH2**: **AF5** is closed in the
capability contract and in the neutral brief and is **not** closed in the contract-completeness
review's per-capability property audit, whose `C4-P2` required-green cell still names four of the
seven legal members of the group.

**AG1 is genuinely closed, and I established that by evaluator rather than by reading.** The
completeness review's silence-probe row now names the recipient's subsequent admission, and a vector
authored from that row takes `C4-P2` **red** on its own named mutation (probe **P3**, row M1b), where
the same vector authored from the row as it stood at `57bb1d8` takes it green (row M1c). AG2, AG3,
AG4, and AG5 are each closed in the artifacts their evidence named.

**This is the first conforming verdict in twelve cycles, and I want to be explicit about what it is
and is not.** It is a judgement that no finding I reached rises to blocking under the standard this
programme has actually applied — the standard review 10 stated when it rated AF5 nonblocking, and
review 11 applied when it rated AG2 and AG3 nonblocking and AG1 blocking. It is not a judgement that
the package is clean: six nonblocking findings remain, four of them are recurrences of classes this
programme has already named, and one of them (**AH2**) is the fifth instance of the exact
"closed in the first artifact, left in another" pattern the reviewed commit exists to sweep. Section
[What this verdict means](#what-this-verdict-means) states the reasoning for each escalation decision
I declined to make, so an owner who disagrees with one of them can see precisely which one.

## Isolation

Complete, with the dispatch provenance disclosed in its own section below.

```text
C:/b032  ->  f451f557ec51b9b878ddc0476c1cc7e0bd836679  (clean)
git status --porcelain   ->  (empty, 0 lines)
git ls-files | wc -l     ->  889
git diff HEAD            ->  (empty)
```

The clone materialised completely — 889 tracked paths, clean status, empty `git diff HEAD`, no
`Filename too long` failure, the clone target being a short path. Every artifact assessed here was
read from `C:/b032`; all four gates available to this review were run there. The author's working
repository `C:/Users/jakub/source/repos/Brontide` was not read, written to, or executed against at any
point in this session.

The reviewer identity above differs from all eleven retained reviewers, from every correction author,
and from every retained iteration-review actor. No author private reasoning was available. `AGENTS.md`
and `docs/future/channel/reviews/README.md` were both read from the clone at the pin, and are the
source of this review's scope. The `C4-P2` evaluator used in probe **P3** imports no repository code.

**Independence caveat, stated plainly.** The dispatching brief named no artifact defect and no area of
suspicion. Three things in it narrowed this review, and I record them so the next cycle can discount
accordingly.

1. It told me to verify the pin myself rather than take it from the brief. I did (see **Pin**), and it
   holds in the stronger tree-hash form.
2. It restated the policy's requirement of at least one genuine attempt to falsify a capability-wide
   property. Roughly half of the effort here went to C4, C12, the neutral brief, the completeness
   review's property audit, the redesign plan's rulings, and the three entry-point indexes. C5, C6,
   C7, C9, and C11 were assessed by reading and cross-tracing rather than by probe, with one
   falsification attempt each at `C8-P1` and `C4-P1`.
3. It told me to read closure review 11's attestation for form. That attestation is a detailed account
   of AG1-AG5, so my verification that the AG corrections landed is verification of findings I had
   been told about. **AH1, AH2, AH4, and AH6 are not in any retained record**; **AH3** is the AA2/AF2
   class applied to surfaces no retained record names; **AH5** is my answer to a question the
   completeness review's own residual risk 2 asks the reviewer to answer.

I did **not** read any retained attestation before forming my own reading of C4, the two machines, the
grid, and the brief. Review 11's attestation was read after the `C4-P2` evaluator had been written and
run, and reviews 8, 9, and 10 were consulted only for specific findings' evidence sections when
re-deriving them.

## Disclosed process deviation in this dispatch

This review was dispatched by a session that, on its own account:

- **authored the correction commit under review**, `fix(channel): close AG1-AG5 and sweep every
  finding's named artifacts`, including every artifact edit and every verifier check in it;
- **also authored the three commits before it** — the AF, AE, and AD corrections — and the
  [AD correction iteration review](./channel-0.2-ad-correction-iteration-review.md) retained in this
  directory; and
- **dispatched closure reviews 9, 10, and 11**, whose findings those commits correct.

This is the same relationship the directory discloses for closure reviews 10 and 11 — the dispatcher
is the author of the very commit being judged — extended by one further commit and one further
dispatch. It is recorded because an undisclosed relationship between a dispatcher and a reviewer is
the same class of defect as an undisclosed reviewer-repairs-own-finding, which this directory already
discloses twice.

**What the dispatch did and did not carry.** The brief conveyed none of the dispatching session's
findings, reasoning, or conclusions. It named no artifact defect, no area of suspicion, and nothing
about where it believed the work was weak or strong; my context contains nothing from that session
beyond the brief itself. It pointed me at `AGENTS.md` and this directory's policy, told me to take my
scope from them rather than from the brief, told me explicitly that I was reviewing work whose author
had arranged my review and that this was a reason to probe the corrections harder rather than defer to
them, and stated that eleven consecutive reviews returned `does-not-conform`, that this was context
rather than a target, and that **neither** manufacturing a finding to avoid a conforming verdict nor
suppressing one to produce it was acceptable.

**Did anything in the dispatch narrow where I looked?** Yes, as recorded in the caveat above: the
instruction to run an evaluator, and the instruction to read review 11 for form, concentrated effort
on C4/C12/the brief and on the AG findings. Nothing in the dispatch narrowed what I *concluded*.

**This cycle's evidence on whether the arrangement softened the review is weaker than reviews 9 and
10 offered, and stronger than review 11's.** Unlike reviews 9 and 10, I did not find a blocking defect
inside the dispatching author's own change. Unlike review 11, four of my six findings are ones no
retained record names, and two of them — **AH1** and **AH4** — contradict claims the correction commit
makes about its own completeness, in sentences that session wrote. **AH2** is a defect the commit's own
sweep was structurally unable to find, and saying so is a finding about the sweep rather than about
the author's diligence.

**A reader weighing this verdict should weigh the arrangement.** A conforming verdict is the outcome
the dispatching author had an interest in, and it is being returned by a reviewer that author
arranged. The strongest thing I can say against that concern is that I raised six findings, that four
are new, and that I have set out below the specific escalation decisions on which the verdict turns —
so the disagreement, if there is one, is locatable rather than diffuse.

## Pin

The policy's pin clause names the current target as the commit titled
`fix(channel): close AG1-AG5 and sweep every finding's named artifacts`, "or any later commit whose
design artifacts hash identically to it — and check that claim rather than assuming it, because this
clause has now gone stale twice" (U6, then X6).

I checked it against the repository rather than against the brief, and it holds in the stronger form:

```text
git log -1 --format=%s 6d0e43f   ->  fix(channel): close AG1-AG5 and sweep every finding's named artifacts
git rev-parse 6d0e43f^{tree}     ->  7d35c1f25ea951b92b73d6e2444d59c22582a21f
git rev-parse f451f55^{tree}     ->  7d35c1f25ea951b92b73d6e2444d59c22582a21f
git diff --stat 6d0e43f f451f55  ->  (empty)
```

The whole tree is identical, so every design artifact hashes identically by construction. `f451f55` is
the merge of PR #122 bringing `6d0e43f` to `main`; `6d0e43f` carries exactly the named subject and is
the head of the correction sequence beginning at `fix(channel): make C4-P2 falsifiable`. The X6
correction — checking this sentence against the most recent commit that changed a design artifact
rather than against its own wording — holds at this pin, and the design gate passes. This is the fifth
cycle at which the clause has been checked and the third at which it was true of a later commit rather
than vacuously of the named one.

The commit's own author date is 2026-08-15 (+0200); the policy dates the correction sequence
2026-08-14 for its predecessor. Noted, not raised; the pin is established by subject and tree, both of
which I verified.

## Blocking findings

**None.**

I record this as a positive result rather than an absence. The two blocking-finding classes this
programme has produced were both tested for directly and neither is present:

- **a property that cannot fail** (S1 → U1 → AC3 → AF1 → AG1). Probe **P3** runs `C4-P2` red on both
  named mutations, from the corrected C4 passage, from the corrected completeness silence-probe row,
  and from the complete record set both state machines determine.
- **a property that cannot stay green** (AE1, and AG2 as the same failure reached through an operand).
  Probe **P3** runs `C4-P2` green on all seven legal members of its required vector group under the
  operator set as published, provided a declared stimulus step can be attributed to a session — which
  is **AH1**, and which I rate nonblocking for the reason review 11 gave for AG2 and review 10 gave
  for AF8: no required vector triggers it.

## Nonblocking findings

### AH1 — the session qualifier AG2 added to the precedence relation has no operand: a declared stimulus step carries no session, and the vector format contradicts the three passages that reason about two-session vectors

**Artifacts.** `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Capability-wide property format"
(the closed operator set, and the AG2 paragraph immediately below it); the same document §"Vector
format" (the stimulus-step bullet and the "profile and initial session/interaction state" bullet);
the same document §"Observation and parity profile" (the admission bullet's closing sentence);
`Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the AF8 paragraph;
`build/verify-channel-0.2-design.ps1`, the AG2 check at `$precedenceOperator`.

**What the correction did.** The operator set now reads:

> precedence between two steps in one vector's declared ordered stimulus sequence, for one endpoint
> and one interaction identity **within one session**

and the paragraph below it explains the qualifier in AF8's terms: "a wholly conforming two-session
vector reusing one identity puts two unrelated endpoints' steps in one precedence relation and takes
`C4-P2` red". C4's assertion that the relation "carries the same qualifier for the same reason" is now
true of the operator, and the verifier pins it.

**What it depends on, and does not have.** Precedence is defined over the vector's *declared ordered
stimulus steps*, which are the vector author's own data. The vector format states what a step carries:

> ordered stimulus steps, each naming its **committing endpoint** and, where it carries one, its
> interaction identity.

No session. Nothing else in the brief gives a stimulus step a session: `session` appears in the
message schemas, in the identity-space list, in the parity profile, and in the operator paragraph, and
in the vector format only as "profile and initial session/interaction state". W5 was raised for exactly
this shape — "the precedence relation is defined over one endpoint's own frames, and the vector format
recorded 'ordered stimulus steps' with no committing endpoint, so the operator had no operand; steps
now name theirs." The AG2 correction added a second dimension to the relation and did not add it to
the operand.

The membership operand is not in the same position, and the contrast is what makes this a gap rather
than a symmetry: membership reads *observations*, and C10 already requires an observation to
distinguish "session and interaction identities", so AF8's session scope has somewhere to read from.
Precedence reads *declared steps*, and they have no session field.

**Probed, not reasoned.** Probe **P3**, column B: the evaluator implements precedence exactly as the
brief publishes it and attributes steps exactly as the vector format declares them. On the wholly
conforming two-session vector (session A delivering an acknowledgement then an Outcome in commit
order; session B carrying required-green member 7, a duplicate terminal from a nonconformant peer,
legitimately reusing the same interaction identity value) the property goes **red**. Under column A —
the same operator with steps carrying a session — it goes green. That is AG2's own result, reproduced
after AG2's correction, through the operand instead of the operator.

**The second half, which is the part a correction pass should read first.** Underneath both AF8 and
AG2 is a question no artifact answers: *may a Channel 0.2 vector carry more than one session?* Three
normative passages assume it may —

> a two-session vector could otherwise hold one identity refused at `unseen` in one session and
> admitted in another (C4)
>
> a wholly conforming two-session vector reusing one identity … takes `C4-P2` red (the brief)
>
> a vector carrying two sessions may hold the same identity value in both (the parity profile)

— while the vector format's own field list names "profile and initial session/**interaction** state"
in the singular, which reads as one session per vector. Both readings are internally coherent and each
makes the other's text wrong: if a vector may carry two sessions, precedence is not evaluable and AH1
is live; if it may not, then AF8's membership scope, AG2's precedence scope, and three paragraphs of
normative justification all defend against a vector no author can write, and two corrections are
inert. The design has now spent two findings on this exposure without stating which reading holds.

**Nonblocking, and for the same reason review 11 gave AG2 and review 10 gave AF8: I could not name a
required vector that triggers it.** `C4-P2`'s selector is "Across every C4 vector"; the reconnect and
new-session cases live in C2's group and the completeness review's silence table; no required
adversarial group names a two-session vector. Under `AGENTS.md`'s own standard — "a nameable trigger,
or it is not a test" — this is a gap in the operand's specification rather than a demonstrated defect.
Recorded at that weight, and recorded rather than passed over, because the design now asserts in five
places that this exposure is closed, and a false claim of closure invites the next reviewer not to
look — which is the argument review 11 itself made for recording AG2.

### AH2 — AF5's required-green correction is closed in the contract and the brief and left uncorrected in the completeness review's property audit, which still names four of the seven members

**Artifacts.** `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Per-capability property
audit", the `C4` row's **Required-green inputs** cell (line 176);
`Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4 "**Required green.**" (lines 293-303).

**The contract, corrected under AF5**, names seven and says why the count matters:

> Its required vector group has seven legal members and the set names all seven, because a member with
> no stated expectation is the condition AE1 arose from: conforming commit-order delivery in the
> initiator direction; conforming commit-order delivery in the recipient direction; a request **lost**
> …; a lost **acknowledgement** …; a cancellation control for an identity the peer never opened; a
> legal late control arriving after a peer's terminal; and a duplicate terminal from a nonconformant
> peer.
>
> The two conforming-delivery members were the sharpest omission when AF5 was raised: a property that
> goes red on plain conforming delivery is the worst failure available to it.

**The audit cell**, in the table AE3 created to make required-green sets visible, reads in full:

> `C4-P2`: a request lost while the control naming its identity is delivered; a control for an
> identity the peer never opened; a legal late control after a peer's terminal; a duplicate terminal
> from a nonconformant peer. The first is AE1 — a required member of the group, carrying no
> expectation, that the property was red on

Four members. Missing: both conforming-delivery members and the lost acknowledgement — members 1, 2,
and 4, which are precisely the three AF5 named, including the two the contract calls the sharpest
omission. `git show 6d0e43f -- …Contract-Completeness-Review-0.1.md` touches two hunks, the
silence-probe row and the disposition history; this cell is untouched, and it was untouched by the AF
correction as well.

**Why it matters where it sits.** The audit is the table this same document points Batch 2 at:
"**Batch 2 cannot author `capability-properties.json` until these are stated**, because the property
format now lists the required-green set as a normative field." The one capability whose set is stated
is stated twice, differently, and the incomplete statement is the one in the table an author of
`capability-properties.json` reaches for. For members 1, 2, and 4 that reproduces the condition C12's
own paragraph names: "the vector it failed on was already a required member of its own group with no
stated expectation at all."

**This is the fifth instance of the pattern the reviewed commit exists to sweep, and it is the
instance the sweep could not find.** The commit's account of its own method is: "A sweep over all
eleven retained attestations extracted, for every finding, the artifacts its own **evidence section
cites**." AF5's evidence section cites the contract, the brief in two sections, and the verifier's
`$requiredGreen` check — not the completeness review. So the sweep, executed exactly as described,
returns nothing here. The class is not "the second artifact a finding's evidence names"; it is "every
artifact that carries the fact", and mechanising the former is what leaves this one standing.

**Nonblocking, on the programme's own precedent and against my first instinct.** Review 10 rated AF5
nonblocking in terms that apply here unchanged: "probe **P3** ran all seven and the property is green
on all seven, so there is no live defect in `C4-P2` — only in the record of what must be green. That
distinction is exactly the one that separates this from AE1, and it is why this is not blocking." My
own probe **P3** confirms the property is green on all seven and red on both mutations. This is
strictly less severe than AF5 was, because the *normative* statement in C4 is now correct and only the
audit record is stale. Escalating it would apply a harsher standard to the second surface than the
programme applied to the first.

### AH3 — three narrative entry-point surfaces stop one family short, and one of them states that no independent review has seen the AF corrections

**Artifacts.** `docs/future/README.md` §"Priority 1 — Channel 0.2 redesign and migration", the
design-package paragraph (lines 31-70); `docs/future/channel/README.md` §"Channel 0.2 design
foundation" narrative (lines 6-24 and 30-44);
`Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md` **Status** block (lines 5-31).

The reviewed commit updated the counts, the Channel index's range sentence, all nine per-artifact
rows, and the future-work index's `| Channel |` row. It did not update the three narratives.

**`docs/future/README.md`** carries the eleventh review's existence nowhere in its Priority 1 prose,
and closes the Channel passage with a sentence that is now false:

> **No independent review has yet seen the AF corrections**, and no Channel 0.2 schema or
> implementation is authorized until the [review handoff] closes cleanly.

The eleventh independent closure review saw them, at `57bb1d8`, and raised AG1-AG5; its attestation is
retained in this directory and the same document's own `| Channel |` row names the AG family. `AG`
appears nowhere else in the file. This is AA2's defect — "the longest Channel narrative of any entry
point", stopping short — with a false assertion about the state of independent review on top of it.

**`docs/future/channel/README.md`** narrates "the ninth closure review raised **AE1**-**AE5** …; the
tenth raised **AF1**-**AF8** … Both are corrected", and stops. No eleventh review, no AG family, and
the paragraph below it still ends "and most recently a fix stated only in the artifact that reads the
fact rather than the ones that own it", which describes AC1. The range sentence four lines above does
say "S1 through **AG5**". That is AF2's defect in the same document, one paragraph over from where
AF2 was corrected.

**The redesign plan's Status block** ends "The tenth closure review then returned `does-not-conform`
with blocking **AF1** and nonblocking **AF2**-**AF8** … All eight are corrected." No eleventh review,
no AG family; `AG3` appears in the plan exactly once, four hundred lines below, inside the ruling note
AG3 asked for. **AB1 was raised for this precise surface** — "the redesign plan is the fourth entry
point and the one status block the T4 cycle-name check set never covered, and it had stopped at S3
while six passes ran" — and the Channel index row describing this artifact now declares "AB1 and AG3
corrected".

**Why no gate sees any of it.** The AA2 family check (`build/verify-channel-0.2-design.ps1`, line
1082) asks only that each family appear *somewhere* in `docs/future/README.md`; `AG1-AG5` in the
`| Channel |` row satisfies it. The AF2 check (line 1412) reads one sentence of the Channel index
narrative — the `runs from S1 through …` range — and nothing else in it. No check reads the plan's
status block for a family at all.

Nonblocking: no design fact is contradicted and no reader is sent to reconstruct evidence. Recorded
because this is the sixth cycle in which entry-point staleness has been raised (S3, AA1/AA2, AE4,
AF2, AG4/AG5, now this), because one of the three surfaces is affirmatively false rather than merely
incomplete, and because the surface AB1 owns has now gone stale a second time while an index row
declares AB1 closed.

### AH4 — the AG4 check's escape clause is not bound to the newest family, so five of nine rows will satisfy every future family's check without naming it

**Artifacts.** `build/verify-channel-0.2-design.ps1`, the AG4 block;
`Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"The pattern is the finding", final
sentence; `docs/future/channel/README.md`, the design-foundation artifact table.

**The claim.** The completeness review states the structural guarantee the correction bought:

> The per-artifact index rows now state their position against the newest family explicitly — naming
> it or declaring the artifact unchanged by it — **so a row cannot go stale by being left alone**

and the check's own comment says "Each row must now make an explicit claim about the latest family --
either it names it or it says the artifact is unchanged by **it**".

**The check.** The condition that raises a failure is

```powershell
if ($artifactRow -cnotmatch "\b$($latestDispositionFamily[0])[0-9]" -and $artifactRow -notmatch 'unchanged by')
```

The second clause matches the bare phrase `unchanged by` and is not bound to `$latestDispositionFamily`
in any way. A row that says "unchanged by AF and AG" therefore satisfies the check for family `AH`,
`AI`, and every family after, without making any claim about them. Of the nine rows the check
enumerates, **four** name the current family with a digit (plan `AG3`, contract `AG2`, completeness
`AG1`, brief `AG2`) and **five** pass only through the escape: session state machine ("unchanged by
the AE, AF, and AG passes"), interaction state machine ("unchanged by AF and AG"), state/event
coverage ("unchanged by AF and AG"), responsibility matrix ("unchanged by the AE, AF, and AG passes"),
migration ledger ("unchanged by AG"). None of the five contains `AG` followed by a digit, so all five
depend on the unbound clause, and all five will go stale silently at the next family — which is the
exact failure the row-level correction was written to prevent.

The sibling AG5 check on the future-work index's `| Channel |` row has no such escape and is sound.

Nonblocking: it is a weakness in a gate rather than a defect in the design, and the rows are accurate
today. Recorded because it is the fourth time a check written by a correction pass has been narrower
than the rule its own comment states (AC4, AD2, AF5's `$requiredGreen` window, AF1's contract-only
scope), and because a first-batch artifact asserts the guarantee as achieved. The commit message's
companion claim — "cross-artifact claims are pinned against the artifact they describe **so AG2's
class cannot be written again**" — is likewise stronger than the mechanism: one such claim is pinned,
not the class.

### AH5 — U7's direction-scope disposition predates AE3's converse rule, and under that rule `C4-P1` and `I5` may fail against a conforming realization while both required-green cells read `owed`

**Artifacts.** `Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md` §"Required silence probes
and dispositions", the `direction scope of the in-flight bound` row, and §"Residual review risks"
item 2, and the `C4` and `I5` rows of the property audit;
`Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4 "**Property C4-P1**" and §C12;
`Brontide-Channel-0.2-Interaction-State-Machine-0.1.md` §"Capability-wide properties", `I5`.

The completeness review's residual risk 2 asks the reviewer to "test whether recording the
disagreement is still the right disposition". This is my answer, and it is a finding rather than an
endorsement because the rule governing it changed after the disposition was made.

The recorded disagreement is that `C4-P1` bounds "the number of nonterminal interactions" and `I5`
bounds "concurrency" with no direction restriction, which reads session-wide, while "the only
mechanism the design provides — the interaction machine's atomic one-position reservation at admission
— is local and has no cross-endpoint coordination, so it can enforce only a per-direction count."
When U7 was dispositioned, C12 required only that a property be *able to fail*. AE3 added the converse:
"every property **must not fail against a conforming realization**: it carries a named set of legal
inputs it must leave green." Under that rule the disagreement is no longer an undeclared scope — it is
a named case in which two properties may go red on a realization that enforces exactly what the design
tells it to enforce, in a profile where both endpoints initiate. That is AE1's shape, and AE3 exists to
make it visible.

Two things keep this nonblocking. It is unreachable in the only named profile, where one endpoint
initiates both classes and the two readings coincide, so no vector can falsify either reading today —
the same "no nameable trigger" test that keeps AH1 and AG2 nonblocking. And it is disclosed: the row
records the disagreement rather than hiding it, and names the owner of the fix (`Channel profile` plus
the Batch 2 `established-profile` schema). What is *not* disclosed is the connection to AE3: both
required-green cells read `owed`, so nothing records that `C4-P1` and `I5` have a known
conforming-realization exposure, and `owed` reads as "not yet written" rather than "known to have a
red case". A correction pass filling in `C4-P1`'s required-green set from its vector group would
reproduce the omission unless the direction scope is settled first.

### AH6 — C4 and the brief both say the retention rule *requires* the later admission; it says the request is admitted on its own merits, and a reordering whose displaced request is refused leaves the first conjunct green

**Artifacts.** `Brontide-Channel-0.2-Capability-Contract-0.1.md` §C4, the retention passage and the
AE1 justification paragraph; `Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md` §"Observation and
parity profile", the admission bullet.

The AE1 clause is justified in C4 by:

> a reordering delivers the request afterwards and the recipient admits an interaction for that
> identity, **exactly as the retention passage below says it must**

and in the brief by:

> a reordering delivers it and the recipient admits an interaction for that identity, **exactly as
> C4's retention rule requires of any later request bearing it**

The retention passage does not say that. It says:

> A later request bearing that identity therefore arrives at `unseen` as any other first request does,
> **and is admitted on its own merits**; the earlier fault does not bar it, because a refusal the
> recipient did not retain cannot bar anything.

Its normative content is that the earlier refusal does not bar the request — the request is judged on
its merits, not that it is admitted. Both citing sentences convert "not barred" into "must be
admitted". That is AG2's class of defect — a claim about text the text does not carry — with the cited
text one screen below the citation rather than in another document.

**Probed, not reasoned.** Probe **P3**, row Q1: a genuine reordering in which the displaced request is
then refused on its own merits (bound exceeded, authority denied, or a false phase predicate) produces
the recipient's `rejected-protocol` at `unseen` and no admission, so the membership test is empty and
the first conjunct is **green** on a reordering the promise forbids. The named mutation vector still
goes red, because C4 specifies `C4-control-precedes-request`'s expected observations with a successful
admission and dispatch; the gap is in the conjunct's coverage of the promise, not in the vector.

Nonblocking. `C12`'s two rules are both satisfied — the property fails on its named mutation and stays
green on all seven legal members — and C4's Silence already concedes the operationalization is narrower
than the promise ("this is stated over the refusal that reordering produces"). Recorded because it
answers, in the negative, one of the four sharp questions the policy's handoff puts to this cycle
("does the refusal-based formulation admit a reordering that produces neither named fault"), and
because the two sentences that justify the AE1 clause overstate what the design guarantees.

## Capability verdicts

| Item | Verdict | Rationale |
| --- | --- | --- |
| C1 | conforms | One canonical established profile with byte/semantic equality between the fixed and negotiated paths after canonicalization; unknown Channel versions, required features, classes, authority modes, and incompatible application contracts refuse; no implicit downgrade, no in-place renegotiation. `C1-P1`'s disjunction is total over its vectors. The established-profile image carries the realization's per-interaction frame order declaration and refuses establishment when it is absent, and W2's point — establishment verifies the declaration is *present*, never *true* — remains stated at the provider boundary. Cross-checked against the session machine's fixed/negotiated equivalence section, which makes a field absent from the fixed path a contract defect rather than realization freedom. |
| C2 | conforms | Six states, `closed`/`faulted` terminal and non-transitioning, drain freezing the admitted set, D1's duplicate drain fatal with the first snapshot and every interaction's effect evidence preserved. `C2-P1` covers acceptance, rejection, and terminal monotonicity. The session totality rule explicitly does not override the named nonfatal peer-interaction-during-drain row, so no event/state pair offers a choice. Reconnect creates a new session identity and inherits no replay or in-flight state. |
| C3 | conforms | Class, direction, and external phase are three separate exact admission inputs; `false` and `unknown` are treated identically; D3's receiver-local refusal is frameless `refused-local` with `known-none`; T1's rule that a phase refusal is never `state-violation` is carried in the ledger's `state-violation` row. Channel evaluates the declared predicate without creating or advancing the phase. |
| C4 | conforms-with-nonblocking-findings | **AH1**, **AH2**, **AH5**, and **AH6** bear on C4; none is blocking. The property itself is sound: probe **P3** finds `C4-P2` green on all seven legal members of its required vector group and red on both named mutations, including with the complete record set both state machines determine for `C4-control-precedes-request`, and including the initiator's late-traffic latch settled against the recipient's genuine Outcome, which stays green for the right reason. **AG1 is closed**: a vector authored from the completeness review's silence-probe row now goes red where at `57bb1d8` it went green. `C4-P1`'s three clauses, the finite positive `max-in-flight`, replay as a nonterminal-window fault with T2's post-terminal split, the W4 retention rule with X5's recording-versus-retaining distinction, AC3's committing-endpoint subject, AF5's seven-member set in the contract, AF8's session-scoped membership operand, and both conjuncts' restriction to one endpoint's own frames — which probe **P3** confirms is load-bearing for members 6 and 7 — all hold. |
| C5 | conforms | Positional payload/authority classification, pre-dispatch parsing and bounds, no partial frame becoming a partial interaction, `known-none` on structural refusal. `C5-P1` binds dispatch to having passed every declared bound and positional rule. Environmental limits must be exposed and accepted at establishment, which is where the retained register's `CH-K6` hardening asymmetry is answered. Allocation failure is locally classified without transporting a runtime exception. |
| C6 | conforms | Authority is evaluated per interaction after structural admission and before dispatch; delivery, correlation, establishment, provider availability, and Shape compatibility are each explicitly disclaimed as grants; local denial emits no frame and records `known-none`; cross-trust carries attributable context and designations and no Capability, Constraint expression, or derivation chain. `C6-P1` requires exactly one `permitted` local decision to reach dispatch, and requires every denial or unevaluatable presentation to record decision point, initiator attribution, and `known-none`. |
| C7 | conforms | Traced clause by clause against Decision 13 as recorded in `binding/portable/open-decisions.md` §686: Option A retained for 0.1 and Option B selected for 0.2, with C and D rejected, recorded 2026-08-11. C7 carries Option B's exact CM3-declared edge, direction, initiating member, receiving member, Operation, Capability, and input Shape; the pre-Ready window; the composition root initiating on the Component's behalf; the refusal to introduce a Component-to-Component binding kind; and failure preventing Ready and Release while returning the actual observation to CM4 cleanup or rollback. `C7-P1` forbids the interaction producing Ready or Release by itself. Option B's wording says "a new envelope kind" and C7 uses the ordinary interaction form; that departure is explicit, reasoned in the completeness review, and recorded in the matrix's boundary ruling, so it preserves the semantic ruling rather than reinterpreting it. |
| C8 | conforms | One accepted terminal history; cancellation acknowledgement explicitly nonterminal; R1's held control bounded at exactly one; R2's statement that the two preconditions are local and unsynchronised; S2's third and fourth exits from `validating`; T3's `cancelled`-with-no-request-in-force routed as a class at both endpoints. C8's statement that recipient admission is not observable from `dispatched` is what makes AE1's loss vector legal and is correctly unchanged. A falsification attempt at `C8-P1` (probe **P6**) failed: no path records a cancellation control, drain, timeout, or protocol rejection as semantic success, and `outcome-cancelled` is a distinct terminal rather than success. |
| C9 | conforms | Four provenance forms with an exclusivity property; unknown peer-fault category faults the local session as `unrecognized-peer-fault` with no answering fault and no loop; loss categories and detection points observer-relative and claiming no global topology. `C9-P1` forbids any field permitting a local inference to be accepted as a peer statement or a protocol fault as an Outcome. PB8's blocking finding — both stacks fabricating a known zero effect count on process loss — is answered by C10's certainty form rather than restated as a Channel 0.1 defect. |
| C10 | conforms | AE2's `known-none` is present in both the machine row and both grid cells; AC2's refused-frame kind and detailed reason, Y1/Y2's latch and settling frame, and Z3's `not-applicable` are all present and owned by the matrix's `Local observation content and provenance` row. `C10-P1` forbids an unsupported `known-none` after a possible post-dispatch path. The observation record required by C10 is sufficient for the membership operand AF8 scoped to the session, since C10 requires an observation to distinguish "session and interaction identities" — which is the asymmetry that makes **AH1** a gap on the precedence operand alone. |
| C11 | conforms | Facets may add classes, payload forms, and stronger evidence and may not reinterpret identities, authority, the four provenance forms, or effect certainty; retry is a new interaction identity with optional causal attribution and never replay; the intra-interaction ordering fact is named as the one ordering fact core owns, which a facet may strengthen and may not weaken. `C11-P1` binds both halves. Cross-capability invariant 7 and the matrix's `Extension hooks` list agree with it. |
| C12 | conforms-with-nonblocking-findings | **AH1**, **AH2**, and **AH5** are all against surfaces C12's rules govern. AE3's converse rule is stated in C12 in the terms that make it a rule; the brief's property format carries the required-green set as a normative field; AF7's audit extension holds — I enumerated it independently at 12 capability rows plus 13 state-machine rows = 25 audited against 12 C-properties + `S1`-`S6` + `I1`-`I7` = 25 stated, with 31 `owed` cells (probe **P4**). The audit's honesty about `I1`-`I7` satisfying neither half of C12's rule is the right disposition and is disclosed residual work. What the converse rule newly catches this cycle is **AH5**, and what it does not yet reach is **AH1**, because the operator it points at cannot be evaluated on the vector class the same document reasons about. |

## Area verdicts

| Area | Verdict | Rationale |
| --- | --- | --- |
| Session state | conforms | Ten legal transition rows, a refused/illegal table, a totality rule that explicitly does not override named nonfatal rows, and `S1`-`S6` carried in the property audit under AF7. No external phase appears as a session state and each is listed as explicitly not one. Fixed/negotiated equivalence makes a field absent from the fixed path a contract defect. Drain is symmetric and occurs exactly once per endpoint history. |
| Interaction state | conforms | Twelve initiator and twelve recipient states with terminality marked; every transition row carries an effect-certainty or handler-effect column; `I1`-`I7` hold as statements and are audited. The `unseen` row is a detailed row (X3) routing to `unseen` rather than to the terminal state (Y3), carrying `known-none` (AE2), the refused frame's kind and the detailed reason (AC2), and no history, latch, or reservation (W4). The terminal-provenance table's last row gives the refusal a declared provenance. The settling-frame reference carries four fields and no session, which is part of **AH1**. |
| State/event totality | conforms | Independently enumerated (probe **P2**): 6×6 session, 6×6 initiator, 6×6 recipient published rows = **108 published-row cells**, zero empty; expanding the published groups against the machine's own state tables (12 initiator states of which 6 terminal, 12 recipient states of which 7 terminal) gives **180 underlying state/event pairs**. Agrees with the seventh, eighth, ninth, tenth, and eleventh reviews. The six-rule closed-world ordering is well-founded, rule 1 genuinely claims the `unseen` event so rule 2 cannot produce the terminal `peer-fault` W4 refuses, and the `not-applicable` latch is compared as a value rather than an absent field. |
| Responsibility | conforms | Enumerated mechanically (probe **P4**): **39 ownership rows, 22 declared identifiers, 22 distinct identifiers used, zero used-but-undeclared, zero declared-but-unused, zero rows with two owners or none.** `channel-core` appears in the document only in the prose recording that U2 abolished it, and in no owner cell and no status entry point. The `Intra-interaction frame order` row is owned by `channel` and its crossing artifact is the realization profile's declaration; the `Local observation content and provenance` row (AB2) names the latch with its `not-applicable` value, the settling frame with its arrival ordinal, and the kind and provenance of a refused frame that opens no interaction. |
| Completeness | conforms-with-nonblocking-findings | **AG1 is closed** in this document's silence-probe row, verified by evaluator rather than by reading. **AH2** is in this document's property audit and **AH5** is my answer to its own residual risk 2. The disposition history is otherwise accurate and now runs to the eleventh independent review; the residual risks are stated as challenges rather than resolutions; the AF7 audit extension is complete over all 25 properties and its `owed` cells are honest. |
| Migration coverage | conforms | All 24 predecessor vectors dispositioned CH-01 through CH-24 in order, verified against `conformance/channel-0.1-vectors.json`, which holds exactly 24. Twelve protocol categories, seven process categories, five failure domains, ten limits, ten features, and every observation field and resource subfield carry a disposition from the declared five-value vocabulary. AE5's retained requirements register is in the sources inventory and in the completion check (AF3), and its range `CH-R1`-`CH-R11` and `CH-K1`-`CH-K7` matches the register's own highest identifiers, which I computed from the register. `CH-R10` is dispositioned **replaced** with `CH-K5` **retained** as its one accepted instance. AF4's admission is in the new-evidence inventory. Z4's intra-interaction frame order and both mutations are listed. |
| Neutral brief | conforms-with-nonblocking-findings | **AH1** is against this document's operator set and vector format. Everything else holds: artifact boundaries, identity spaces, the three-version rule, the vector format with W5's committing endpoint, the closed operator set with W1's precedence relation and Z1's identification-only restriction on the arrival ordinal, the parity profile with V1's detailed reason, X1's settling frame, AE1's admission comparison and AF8's session scope on it, the required-green set as a normative format field, the golden policy, the reordering-injection provider boundary with W2's present-not-true point, and the Batch 2 entry gate. AG2's session qualifier is present in the operator; **AH1** is that its operand is not. |

## Owner rulings

The four first-batch rulings recorded 2026-08-11 are each represented consistently throughout the
first-batch design. Verified by tracing each ruling to every artifact that must carry it, not by
reading the plan's resolved-questions section alone.

| Ruling | Represented consistently | Trace |
| --- | --- | --- |
| Core concurrency and cancellation | yes | C4's finite positive `max-in-flight` and C8's optional cancellation with fixed meaning; the interaction machine's `Concurrent interactions` and `Cancellation` sections; matrix rows `Bounded unary concurrency` → `channel-profile`, `Cancellation control and terminal meaning` → `channel`, `Class-specific cancellability` → `channel-profile`; ledger `maxConcurrentRequests` → **replaced** as `max-in-flight`, `single invocation` → **replaced**, `cancellation unsupported` → **replaced**; the grid's cancellation columns; the matrix's `Concurrency and cancellation` boundary ruling. The completeness review's direction-scope row records the session-wide-versus-per-direction disagreement rather than hiding it and does not contradict the ruling; **AH5** is about the disposition's age relative to AE3, not about the ruling. |
| Session-state ownership | yes | C2 and the session machine's explicit "not Channel session states" list; matrix rows assigning Interconnection and Release to `portable-binding`, the Relational Initialisation phase to `composition`, and Ready to `component-management`, with the same sentence in the plan's ruling and in the matrix's boundary ruling; ledger `ready` → **moved** as state, message kind, and feature. No artifact lets a peer signal create a composition fact. |
| Relational initialization representation | yes | C3 and C7 as an interaction class under the ordinary machine; the interaction machine's `Relational initialization` section with the `interconnected && !ready` predicate; the matrix's boundary ruling of the same name; ledger `Lifecycle` → **removed** and split. Matches Decision 13's recorded Option B including its explicit rejection of C and D, its composition-root standing-in, and its refusal to introduce a Component-to-Component binding kind, with the envelope-kind departure disclosed and reasoned. |
| Extension invariants | yes | C11; cross-capability invariant 7; the matrix's `Extension hooks` list of the five things a facet cannot reinterpret; the brief's facet rules; ledger `retry unsupported` and `streaming unsupported` dispositions. C11's sentence that a facet may strengthen the intra-interaction ordering fact but not weaken it is the one place the S1 ruling touches this ruling, and the two are consistent. |

The plan's `## Open questions (owners needed)` section correctly reports no unresolved owner decision.
The R1 (2026-08-13), S1 (2026-08-13), and AE1 (2026-08-14) correction rulings are each recorded as
correction rulings that do **not** join the fixed set of four, in the plan and in the review policy.
**AG3 is closed**: the AE1 ruling now states the membership operand "within one session", carries the
"**Issued with a vector-scoped operand, narrowed to the session under AF8 on 2026-08-15**" note, and
records the original wording rather than overwriting it — the same treatment the 2026-08-13 S1 ruling
gives `channel-core`, which I verified carries the parenthetical "*(The identifier recorded in this
ruling was later normalised to `channel` under U2… the ruling text is retained as issued.)*".

## Retained findings

Every retained finding was verified in the artifact it was raised against rather than taken from a
disposition history or an index. Summary, with only the departures spelled out:

- **B1-B4, N1-N3, F1-F3, D1-D5, T1-T4, R1-R3, S1-S3** — closed. Recipient frameless `refused-local`;
  nonterminal cancellation-denial transition; one owner identifier per row; five-value disposition
  vocabulary; exact Ready ownership; peer fault from `cancel-pending`; `retained` disposition with
  treatment column; `replay-detected` live window; distinct recipient `peer-fault`/`lost`; `replaced`
  cancellation Outcome; duplicate drain fatal; distinct acknowledgement states; receiver-local phase to
  `refused-local`; the three-value latch; delivery-fallback moved to its facet; phase refusal never
  `state-violation`; T4's stable phrase present with no superseded cycle name in any status block;
  held control bounded at one; local unsynchronised preconditions; separate `unseen` and `validating`
  grid rows. S3's index staleness is closed as framed and recurs as **AH3**.
- **U1-U8** — **U1 closed**, at the property, at C4's vector passage, and now at the completeness
  review's account of the same vector, which is what AG1 asked for. U2-U8 closed: the owner vocabulary
  is closed with the ordering row owned by `channel`; the brief carries the establishment declaration
  and the adversarial group; the disposition history runs past the eleventh cycle; the audit registers
  `C4-P2` and both mutations; the pin clause is true at this pin and checked against the repository;
  the direction-scope row records the disagreement (see **AH5**); the initiator pre-dispatch Local
  loss cell names `lost`.
- **V1-V3, W1-W6, X1-X7, Y1-Y4, Z1-Z4** — closed. Detailed reason compared; reordering injection
  declared and bounded to mutation vectors; precedence relation added and restricted to one endpoint's
  own declared steps; the reordering provider's declaration with the present-not-true point; second
  mutation added and placed in a required group; retention rule in C4, the machine, and the grid;
  committing-endpoint operand supplied; latch compared; settling frame recorded and compared;
  `not-applicable` owned; `unseen` transition row present; recording-versus-retaining distinction; pin
  clause checked structurally; iteration reviews retained; C10 and the schema carrying the latch and
  settling frame; the refusal leaving state at `unseen`; the arrival ordinal restricted to
  identification; the grid naming a provenance as a provenance; the ordering requirement in the
  new-evidence inventory. **W5 is closed as framed** and its class recurs on the session dimension as
  **AH1**.
- **AA1-AA3, AB1-AB2** — closed as framed. Both indexes carry every disposition family somewhere,
  both computed counts read 11, `channel-core` appears in no status entry point, and the matrix owns
  local observation content and provenance. The narrative surfaces are **AH3**, and the plan's status
  block is AB1's own surface stale a second time.
- **AC1-AC4** — closed. The arrival ordinal is in the brief, the interaction machine, the grid, and the
  matrix; the closed detailed-reason set carries `unopened-interaction-identity` and C10 requires the
  refused frame's kind; `C4-P2`'s subject is the committing endpoint in both conjuncts and is named
  explicitly rather than left to the nearest antecedent; the class check matches two-letter families.
- **AD1-AD3** — closed, AD2 by the ruled correction which AF6 replaced with the declared provenance
  table.
- **AE1-AE5** — closed. AE1 at the property, the parity profile, the contract's vector passage, and now
  the completeness review's silence-probe row; AE2 in both artifacts and both grid cells; AE3 as a rule
  in C12 with the format field and the audit column; AE4 and AE5 on both surfaces each.
- **AF1** — **closed on both surfaces**, verified by evaluator (**P3**, rows M1, M1b, M1c). **AF2** —
  closed on all three surfaces its evidence named; the Channel index's artifact rows now name AG or
  declare themselves unchanged by it, subject to **AH4**. **AF3** — closed on both surfaces, verified
  against the register's own highest identifiers. **AF4** — closed. **AF5** — **closed in the contract
  and the brief, not closed in the completeness review's property audit** (**AH2**). **AF6** — closed;
  the provenance table classifies 19 families, every family the policy bolds is classified, and every
  `iteration` family including `V` has a retained record. **AF7** — closed; all 25 properties audited.
  **AF8** — closed at the membership operand in both normative artifacts and in the ruling of record;
  the precedence operand carries the qualifier and lacks its operand (**AH1**).
- **AG1-AG5** — **all five closed** in the artifacts their evidence named. AG1 verified by evaluator;
  AG2's qualifier present in the operator set with the claim pinned by the verifier; AG3's ruling note
  present in the S1 ruling's form; AG4's nine artifact rows each making a claim about AG; AG5's
  `| Channel |` row naming AG1-AG5.

## Probes performed

### P1 — gates, in the isolated clone

| Gate | Result |
| --- | --- |
| `build/verify-channel-0.2-design.ps1` | pass — "11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending" |
| `build/verify-channel-0.2-design.ps1 -NegativeProbe` | fails with exactly one failure — "Channel 0.2 capability contract properties is missing '**Property C12-P1.**'" — which is the in-memory removal and nothing else |
| `build/verify-doc-links.ps1` | pass — 861 local links across 304 documents |
| `build/verify-text.ps1` | pass — 883 UTF-8 files |

`build/verify-interchange.ps1` was not run; it was outside the set the dispatching brief authorised
for this review. **Green gates are not evidence of conformance**, and this review found that again:
all six findings sit behind a fully green design gate. **AH1** sits behind the AG2 check, which
verifies that the word `session` appears within 160 characters of the precedence operator and never
that a stimulus step can carry one. **AH2** sits behind the `$requiredGreen` check, which AF5 already
recorded reads a 700-character window of the contract and nothing else. **AH3** sits behind the AA2
family check, which asks only that each family appear somewhere in `docs/future/README.md`, and behind
the AF2 check, which reads one sentence of the Channel index narrative. **AH4** is a defect in a check.

### P2 — independent enumeration of the state/event grid

Parsed from the grid's three tables and cross-checked against the interaction machine's own state
tables rather than against the grid's prose counts.

- Session: 6 states × 6 event columns = **36** cells.
- Initiator: 6 published state groups × 6 columns = **36** published cells; the machine states 12
  initiator states (`candidate`, `admitting`, `refused-local`, `dispatched`, `cancel-pending`,
  `cancel-accepted`, `cancel-refused`, three Outcome terminals, `peer-fault`, `lost`; 6 terminal),
  giving 12 × 6 = **72** underlying pairs.
- Recipient: 6 published groups × 6 columns = **36** published cells; the machine states 12 recipient
  states (7 terminal), giving 12 × 6 = **72** underlying pairs.

**108 published-row cells, 0 empty, 180 underlying state/event pairs** — agreeing with the seventh,
eighth, ninth, tenth, and eleventh reviews. No cell offers a choice between two routes, and the
closed-world rule ordering is well-founded.

### P3 — falsification and soundness of a capability-wide property (`C4-P2`)

The policy requires at least one genuine attempt to falsify a capability-wide property, by evaluator
rather than by reading, run over the *required vector group* rather than the cases the capability's
narrative names. An evaluator was written from the published prose of `C4-P2` (both conjuncts, AC3's
committing-endpoint subject, the AE1 admission clause, AF8's session scope), the brief's closed
operator set, the brief's vector format, the parity profile's compared fields, and the settling-frame
reference as the brief, the interaction machine, and the grid state it. It imports no repository code.
Precedence is implemented exactly as the brief declares it; the arrival ordinal is used for equality
only, never as an ordering operand, and is counted "within that interaction" over all frames as the
brief, the machine, and the grid all say.

Each vector was run under three operand configurations, so that the design's claims about its own
operands could be tested rather than assumed.

| Vector | Design expects | A: precedence session-qualified, steps carry a session | B: precedence session-qualified, steps as the **vector format** declares | C: membership vector-scoped (the pre-AF8 wording) |
| --- | --- | --- | --- | --- |
| 1. conforming commit-order delivery, initiator direction | green | green | green | green |
| 2. conforming commit-order delivery, recipient direction | green | green | green | green |
| 3. request lost, control delivered | green | green | green | green |
| 4. acknowledgement lost | green | green | green | green |
| 5. cancellation control for an identity the peer never opened | green | green | green | green |
| 6. legal late control after a peer's terminal | green | green | green | green |
| 7. duplicate terminal from a nonconformant peer | green | green | green | green |
| M1. `C4-control-precedes-request`, expected obs per the **corrected C4 passage** | red | red | red | red |
| **M1b. same vector, expected obs per the completeness silence-probe row AS IT NOW READS** | **red** | **red** | **red** | **red** |
| M1c. same vector, expected obs per that row **as it stood at `57bb1d8`** | red | *green* | *green* | *green* |
| M2. `C4-outcome-precedes-ack` | red | red | red | red |
| **P. wholly conforming two-session identity reuse + required-green member 7** | **green** | green | **red** | green |
| P2. conforming two-session reuse, refusal in one session, admission in the other | green | green | green | *red* |

Six results matter.

1. **`C4-P2` is sound in both directions.** Green on all seven legal members of its required vector
   group, red on both named mutations. The AE1, AF1, and AF5 corrections all hold.
2. **AG1 is closed, and M1c is the proof it was real.** With the silence-probe row as it now reads the
   property is red on its own named mutation; with the row as it stood at the previous pin it is green.
   That is the U1 condition appearing and disappearing on one sentence of one artifact.
3. **Restricting both conjuncts to one endpoint's own frames is load-bearing**, confirmed rather than
   assumed: member 6 (a legal late control from the peer after the peer's terminal) is green only
   because the settling frame and the terminal frame have different committing endpoints, and member 7
   (a duplicate terminal) is green only because the arrival ordinal binds the settling frame to the
   *later* of the two matching declared steps.
4. **Column B is AH1.** Under precedence as published and steps attributed as the vector format
   declares them, wholly conforming two-session behaviour goes red.
5. **Column C is AG3's substance, and confirms the fix was load-bearing.** Under the membership scope
   the plan's dated ruling stated before this commit, the AF8 case goes red on conforming behaviour.
   The plan now carries the corrected scope, so no artifact describes column C.
6. **Q1 is AH6.** A reordering whose displaced request is then refused on its own merits produces the
   refusal and no admission, and the first conjunct is green on a reordering the promise forbids.

### P4 — mechanical verification of ownership, the property audit, and retained phrases

- **Responsibility matrix, enumerated from the source:** 39 ownership rows, 22 declared identifiers,
  22 distinct identifiers used, zero used-but-undeclared, zero declared-but-unused, zero rows with two
  owners or none. `channel-core` in no owner cell.
- **Property audit, enumerated:** 12 capability rows + 13 state-machine rows = 25 audited, against 12
  C-properties + 6 `S` + 7 `I` = 25 stated. Complete. 31 `owed` cells.
- **Registry pins, all eleven recomputed** — the Architecture 0.8 document, the 0.5 implementation
  baseline requirements, the 0.8 requirements, both stacks' 0.5 matrices, both stacks' 0.8 matrices,
  both stack READMEs, and both stack milestone-evidence ledgers — **all eleven match
  `Brontide-Architecture-Status.json`**.
- **Register ranges computed from the register itself:** `CH-R` highest = 11, `CH-K` highest = 7,
  matching the ledger's claimed range and its completion check.
- **`conformance/channel-0.1-vectors.json` holds exactly 24 vectors**, `CH-01-CORRELATION-ECHO`
  through `CH-24`, matching the ledger's coverage claim row for row.

### P5 — attempt to falsify `C4-P1` and `I5` against a conforming realization (positive result: AH5)

Applying AE3's converse rule to a property other than `C4-P2`, which is where the rule has been tested
so far. `C4-P1` bounds "the number of nonterminal interactions" and `I5` bounds "concurrency", both
without a direction restriction; the only enforcement mechanism the design provides is the interaction
machine's atomic one-position reservation at admission, which is local. In a profile where both
endpoints initiate, a realization that enforces exactly that mechanism holds up to twice the declared
bound session-wide, and both properties go red on conforming behaviour. It is unreachable in the only
named profile and the completeness review records the disagreement, so this is **AH5** at nonblocking
weight rather than a blocker — but the required-green cells for both properties read `owed`, so
nothing records that these two have a known conforming-realization exposure.

### P6 — attempt to falsify `C8-P1` (negative result)

`C8-P1` asserts that no cancellation control, drain, timeout, or protocol rejection is recorded as
semantic success. The sharpest available case is the recipient's `cancel-requested` → handler reports
cancellation completed → `outcome-cancelled` path, where a cancellation control does lead to a
semantic Outcome. **It does not fail**: `outcome-cancelled` is a distinct terminal from
`outcome-succeeded` in both state tables and in the ledger's Outcome dispositions, and C8's
`cancelled`-with-no-request-in-force rule (T3) routes the contradictory case to
`internal-channel-failure` at the recipient and `peer-fault` at the initiator rather than to any
success. Drain at `validating` reaches `refused-local`, timeout reaches `lost`, and protocol rejection
reaches `rejected-protocol` or `peer-fault`. Recorded because a failed falsification attempt is
evidence and an unrecorded one is not.

### P7 — attempt to establish that the terminal-frame operand needs its own disambiguator (negative result)

Y4 gave the settling frame an arrival ordinal because kind, identity, and committing endpoint do not
separate two frames of the same kind from one endpoint. `C4-P2`'s second conjunct compares that frame
against "that endpoint's own frame that made the interaction terminal", and **no artifact gives the
terminal frame any disambiguator at all** — the parity profile compares an ordinal for the settling
frame and none for the terminal one. I expected Y4's defect on the other operand.

**It does not recur.** Binding the terminal frame ambiguously to every matching declared step leaves
member 7 green, member 6 green, and M2 red — the same verdicts as the pinned binding. The reason is
structural rather than lucky: the only required case with two same-kind frames from one endpoint is
the duplicate terminal, and there the settling frame is bound by its ordinal to the *later* step, so
the precedence test `settling < terminal` cannot be satisfied whichever step the terminal frame binds
to. Recorded as a negative result.

### P8 — upstream consistency and clone completeness

- `Brontide-Architecture-Status.json` selects Architecture 0.8 at
  `docs/current/architecture/Brontide-Architecture-0.8.md`, status "Complete Draft (document and
  implementation evidence complete; not ratified)", with `latestRatifiedArchitecture` null and the
  rationale "No Brontide architecture document currently has Ratified status." The document's own
  header carries the same Complete Draft status.
- Both stacks state `**Designed for:** Brontide Architecture 0.8, Complete Draft, not ratified`. The
  Channel 0.2 contract states `Designed for: Brontide Architecture 0.8, Complete Draft` and the plan
  `Designed against: Brontide Architecture 0.8, Complete Draft`. No artifact treats 0.8 as ratified or
  claims Channel 0.2 implementation conformance, and every first-batch status block carries T4's
  stable phrase.
- Decision 13's recorded ruling (Option A retained for 0.1, Option B selected for 0.2, C and D
  rejected, recorded 2026-08-11 by `user:JakHoh`) matches C3, C7, and the plan's
  relational-initialization ruling, including the composition-root standing-in and the refusal to
  introduce a Component-to-Component binding kind.
- PB8's blocking finding in both stacks — process loss fabricating a known zero effect count — is
  answered by C10's certainty form and `C10-P1` rather than restated as a Channel 0.1 defect, and the
  ledger moves `providerEffectCount` to the Portable Binding/domain owner.
- `channel/0.2` does not exist: no neutral schema, vector, property, or golden has been authored.
- 889 tracked paths, empty `git diff HEAD`, clean status, HEAD at
  `f451f557ec51b9b878ddc0476c1cc7e0bd836679`. No design artifact was read from outside the clone.

## What this verdict means

**The AG corrections all land.** AG1 is closed on the surface its predecessor named and I verified it
by evaluator rather than by reading; AG2's qualifier is in the operator set with the cross-artifact
claim pinned; AG3 gives the dated ruling the retained-as-issued treatment the S1 ruling already had;
AG4 and AG5 close the two index surfaces. That is the first cycle in this sequence in which every
finding of the previous review closed in every artifact its evidence named.

**The verdict turns on six escalation decisions I declined to make, and they are the part an owner
should audit.** In each case I applied the standard the programme has actually used rather than a
harsher or a softer one:

- **AH2** is the closest call. It is a contradiction between C4 and the completeness review about a
  field C12 makes normative, which is structurally what AG1 was. I did not escalate it because review
  10 rated AF5 — the same defect in the same field, then live in the *normative* artifact — nonblocking
  on the explicit ground that "there is no live defect in `C4-P2` — only in the record of what must be
  green", and because my own probe confirms the property is green on all seven members regardless. The
  situation today is strictly better than the one review 10 called nonblocking. **If an owner thinks
  AF5 was under-rated, then AH2 is blocking and this verdict is wrong** — but the disagreement is with
  review 10's standard, not with this reading of the artifacts.
- **AH1** and **AH5** are both properties that may go red on conforming input in a vector class no
  required group contains. Review 11 rated AG2 nonblocking on exactly that ground and review 10 rated
  AF8 the same way, and `AGENTS.md` supplies the standard directly: "a nameable trigger, or it is not a
  test."
- **AH3**, **AH4**, and **AH6** are index staleness, a weak gate, and a coverage note with an
  inaccurate cross-reference. None contradicts a design fact or leaves a property unable to refute.

**What I would tell the next pass to read first is AH1's second half.** AF8 and AG2 are two findings
against two operands of one property, both defending against a two-session vector, and the design has
never said whether such a vector exists. Settling that one question either retires both corrections as
unnecessary or turns AH1 into a required operand change; leaving it open is what produced two findings
where one answer was needed.

**The second thing worth carrying forward is what AH2 says about the sweep.** The reviewed commit's
method — extract, per finding, the artifacts its own evidence section cites — is a real improvement and
it found AG1 and AG4. It cannot find AH2, because AF5's evidence never cited the artifact where the
defect survives. The generalisation is that the class is "every artifact that carries the fact", and
the cheapest test for it is the one this review used: for each corrected fact, grep the whole
`docs/future/channel` tree for every other statement of that fact, not for the artifacts a previous
reviewer happened to name.

**On the consequence.** This verdict, if accepted, satisfies the Closure section's requirement that
every blocking finding be corrected and that a fresh closure attestation conform at the corrected
commit. It authorizes the remaining closure steps — retaining and committing this attestation
unchanged, calculating its SHA-256, creating `channel-0.2-design-foundation-closure-record.md`, and
updating the policy, the two indexes, the plan, and the design verifier so they accept exactly the
conforming attestation and closure record. It does **not** ratify Channel 0.2, does not claim
implementation conformance, and does not dispose of the six findings above, which remain open work
whether or not Batch 2 opens. The named residual work also remains: 31 `owed` required-green cells
across the property audit, of which `I1`-`I7` satisfy neither half of C12's rule, and the completeness
review's own statement that **Batch 2 cannot author `capability-properties.json` until those are
stated** is unchanged by this verdict.

**I did not create `channel-0.2-design-foundation-closure-record.md`**; that is a separate step and not
this reviewer's. The design was not repaired here: this attestation is the only file this reviewer
wrote, nothing else in the clone was modified, and nothing was committed.

## Note on the design gate

The gate results in **P1** are from before this attestation existed. Retaining it will make
`build/verify-channel-0.2-design.ps1` fail with the same class of failures the ninth, tenth, and
eleventh reviews recorded: the expected-file set names exactly eleven negative attestations, and the
two computed counts in the Channel index and the future-work index are pinned to `11`. That is the
verifier working as designed. Because this verdict conforms, the correction pass also has to change
what those checks assert — the file set gains a *conforming* attestation and a closure record, and the
"independent review still pending" summary line and the `awaits a fresh independent closure
re-review` phrases in twelve status blocks become false and must be replaced together, not
individually.

Three notes for that pass. First, **AH2**'s fix belongs in the completeness review's audit cell and the
check that guards it must read that cell, not the contract's `**Required green.**` window — a check
scoped to the artifact that was already corrected is what let AF1 survive its own correction, twice.
Second, **AH4**: binding the `unchanged by` escape to the family name is a two-token change and without
it the row-level guarantee the same commit asserts does not exist. Third, **AH3** spans three
documents and no gate reads any of the three narratives; if the pass updates the counts and the status
blocks without reading the prose above them, the same finding survives the commit that closes it, for
the sixth time.
