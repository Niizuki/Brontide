# Agent feedback on the repository rules

This folder is the channel by which an agent working under [`AGENTS.md`](../../../AGENTS.md)
reports **friction with those rules**, so that a rule which is ambiguous, contradictory, or
expensive for what it buys can be revisited before it costs something.

It exists because of an asymmetry. Every rule in `AGENTS.md` was paid for by a defect, so a rule
that *causes* a defect gets rewritten. A rule that merely wastes an hour, or that two readers apply
differently, produces no defect at all — nothing triggers a rewrite, and the cost is paid again
every session. The party that sees that cost is the one applying the rules dozens of times a
session, and this is where it says so.

## What belongs here

An entry describes a **situation**, never an opinion, and only when one of five triggers fired:

1. a rule you **could not apply without guessing** what it meant;
2. two rules that **pointed different ways**;
3. a rule that **cost real work for no benefit visible here**;
4. a rule that **caught something you can name**; or
5. an **approach that worked, is not in `AGENTS.md`, and would generalise**.

Nothing else. In particular, **"this rule is good" is worth nothing** and must not be written. A
model is biased toward agreeing with the repository it is working in, and it never sees the
counterfactual: a defect a rule prevented is invisible by construction, so an agent's approval of a
rule carries no information about whether the rule earns its cost. Trigger 4 is the exception that
proves the shape — it is admissible precisely because it names the thing caught, which is evidence
rather than approval.

Two content rules, both inherited from how the rest of the documentation argues:

- **The conclusion must be usable by a reader who knows nothing about the change.** A future
  maintainer reading a month's entries has no memory of the branch that produced them.
- **Name the concrete case as evidence.** An abstract complaint cannot be acted on, and this
  repository's documents already argue from worked examples rather than assertions.

## What does not belong here

Defects in code or documentation, which go to the ordinary suites and gates; design decisions, which
go to the owning plan's resolved questions; and anything a
[contract-completeness review](../../../component-management/cm1-contract-completeness-review.md)
should carry, which is a statement about a contract's silence rather than about these rules.

## Files

Append to `docs/current/ai-feedback/<YYYY-MM>/<YYYY-Www>.md`, using the ISO week. **A week files
under the month its last day falls in**, so a week is never swept while it is still open: 2026-W31
runs 27 July to 2 August and therefore files under `2026-08/`, entries dated in July included.
Create the file and the month folder when the first entry of that week is written.

**Do not read the other entries before writing yours.** This is deliberate and is the one procedural
rule here worth stating twice. Repetition across independent sessions is the signal this folder
exists to produce — three agents tripping over the same sentence in three different weeks is a much
stronger case than one agent's argument — and an agent that had read an earlier entry would either
suppress its own as a duplicate or unconsciously restate someone else's framing, destroying exactly
that signal.

An entry looks like this:

```markdown
## <short title>

Date: <YYYY-MM-DD>
Trigger: <one of the five, named>

<The situation: what was being done, what the rule said, and what happened.>

<The conclusion, stated so a reader who does not know this change can use it.>
```

## The sweep

At the end of a month, its entries are counted into `<YYYY-MM>/<YYYY-MM>-report.md`, which groups
them by trigger and records, for each, **what was done about it**: a rule reworded, a rule removed, a
practice adopted, or a decision to change nothing and why. Recording the outcome is what keeps this
folder from becoming a suggestion box nobody empties — an entry with no disposition is a defect in
the sweep, not in the entry.

Once a month is swept, its folder moves to `docs/archive/ai-feedback/<YYYY-MM>/` with its report,
following the ordinary documentation lifecycle: `current` holds the convention and the open months,
`archive` holds the closed ones. Any rule change the sweep decided on is made in `AGENTS.md` in its
own change, citing the entries that motivated it.
