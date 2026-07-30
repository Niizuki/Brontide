# Portable Binding — contract-completeness reviews

**Status:** Standing practice record. Non-pinned. Nothing here ratifies anything or changes either
stack's Architecture 0.7 implementation target.

Decision 10, recorded 2026-07-28, adopted two standing practices after every defect PB6 found turned
out to be present identically in both stacks. One is a property per capability, which lives with the
vectors. The other is this: **a contract-completeness review at each phase boundary**, asking what
the contract does *not* say, kept separate from conformance review — which by construction can only
check what was written down.

The reasoning is worth keeping in front of the reviewer. Two implementations written from one
contract by one reader **diverge where the contract is ambiguous and agree where it is silent**.
Independent implementation detects disagreement, so it covers ambiguity and is structurally blind to
silence. Comparing the stacks will therefore never surface anything in this file.

## How to read an entry

A review asks one question per capability the phase touched: *what would two independent implementers
have to decide for themselves here?* Every answer is one of:

- **Declared** — the contract now answers it, and the answer is data rather than prose;
- **Owned elsewhere** — another programme owns it, named explicitly; or
- **Accepted** — the silence stands for this version, with the reason.

A finding is not a defect. It is a place where agreement between the two stacks would have proved
nothing.

## PB7 — the Composition handoff (2026-07-30)

**Reviewed:** [`schemas/composition-handoff.json`](schemas/composition-handoff.json) and the eleven
vectors in [`vectors/composition-handoff.json`](vectors/composition-handoff.json), against C2 and C8.
**Reviewer:** the implementing session, which is the limit of this entry — see the note at the end.

| # | What the contract did not say | Disposition |
| --- | --- | --- |
| 1 | Whether two members in one composition may carry the same binding scope | **Owned elsewhere.** The scope is the composition's identity space; this seam binds one position and cannot see the others. Declared as `scope-uniqueness`, naming the composition's resolver as the place a reused scope is rejected. |
| 2 | Whether a member may be retired without ever being released | **Declared.** Yes — a group whose barrier never opens must be able to end the bindings that did establish. Both stacks already behaved this way; nothing said so, so the two could have diverged on the next reading. Declared as `retirement-before-release`. |
| 3 | Whether the lifecycle calls may be driven concurrently for one member | **Declared.** No. Version 0.1 declares one concurrent request for ordinary interaction and now makes the same assumption explicit for lifecycle calls: the stage transitions are checked, not atomic. Declared as `lifecycle-call-concurrency`. |
| 4 | What becomes of a released member whose binding later fails | **Declared.** The gate stays open, the failure is reported per interaction, and retirement records terminal state `failed` with replacement not permitted. Declared as `failure-after-release`. |
| 5 | Whether one offered provision may back two binding scopes at once | **Owned elsewhere.** Sharing an activated occurrence is a sharing, isolation, and authority question the Component Management programme owns. Declared as `provision-sharing` so neither stack invents an answer. |

Four of the five were things **both stacks already did the same way** — which is exactly the pattern
the practice exists to catch. Agreement between them was evidence that one reader had made one
choice twice, not that the contract had made it. Each is now data in `declaredSilences`, so a third
implementer reads the answer instead of inferring it, and a future change to any of them fails
review rather than passing quietly.

Two of the five were resolved by naming another owner rather than by answering. That is the honest
disposition when the question is real but outside the seam: `1..1` binding does not know what else
the composition holds, and a seam that guessed would be making the Component Management programme's
decisions invisibly — the same boundary PB7's four refusal vectors enforce.

**The limit of this entry.** This review was performed by the session that implemented PB7, which is
the weakest form the practice can take: the reader who wrote the contract is the least likely to
notice what it does not say. It found five silences anyway, which suggests the practice is worth
more when run by someone else. PB8's outstanding independent review is the natural place for a second
pass, and a reviewer who disagrees with a disposition here should say so there.
