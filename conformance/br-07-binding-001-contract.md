# BR-07-BINDING-001 — static Attribute-constrained binding capability contract

Status: experimental behavioral contract

Designed for: [Brontide Architecture 0.7](../docs/current/architecture/Brontide-Architecture-0.7.md)
§18.1, Complete Draft, and change C3 of the
[0.7 change plan](../docs/archive/architecture/Brontide-Architecture-0.7-Change-Plan.md)

This contract states the observable behaviour behind requirement `BR-07-BINDING-001` in
[`architecture-0.7-requirements.json`](./architecture-0.7-requirements.json), so both stacks answer
the same questions from one reading. It is shared text only: each stack implements it natively in its
own experimental Composition project, and neither may reference the other.

Architecture 0.7 §18.1 makes two commitments that this contract turns into checkable behaviour. An
Attribute is **a value obtained through a specified Brontide Operation** — identified by its source
Operation, vocabulary version, result Shape, and result path — never a free-floating label. And an
Attribute-constrained binding is **resolved exactly once**, recording effective values and
provenance, because "a later Attribute change never rebinds — reaction belongs to Routers and future
lifecycle policy".

The second is the one worth stating carefully, because the tempting implementation is a live query
that re-answers on every read. Such an implementation satisfies every single-shot test and violates
the architecture. The evidence therefore has to show not merely that a binding *did not* change, but
that a change occurred which **would have selected differently** and the binding still did not move.

## C1 — an Attribute is a sourced value, never a label

Every Attribute value carries the Operation it came from, that vocabulary's version, the result Shape,
and the result path within it, alongside the value. Two values of the same name from different
sources are different Attributes, and provenance records which source answered.

Property: no resolution consults a value that does not name its source Operation, vocabulary version,
result Shape, and result path.

## C2 — resolution happens exactly once, over the values read at that moment

Creating a binding evaluates the declared Definition Constraint against each candidate's Attribute
values as they read at that instant, and selects one candidate. The resolved binding is a record of
that evaluation, not a handle to the sources it consulted.

Property: a resolved binding holds no reference to the candidate set or Attribute source it was
resolved from.

## C3 — the binding records the effective values and why it selected

The resolved binding names the selected candidate, the exact Attribute values the constraint actually
read from it, and a per-candidate account of the evaluation in the order it was performed: which
candidates were considered, which were excluded, and under which diagnostic category.

Property: every resolution — successful or not — reports one provenance entry per candidate
considered, and a successful one reports an effective value for every Attribute its constraint
referenced.

## C4 — a later Attribute change never rebinds

Changing an Attribute value the binding relied on leaves the binding naming the same candidate with
the same effective values. Dynamic Attributes are permitted; automatic rebinding is not.

Property: after a change that makes a fresh resolution select a different candidate, the existing
binding still names the original candidate and reports the original effective values.

## C5 — a later candidate change never rebinds, not even a better one

Adding a candidate that would have been selected had it been present, or removing the candidate that
was selected, does not invalidate, rebind, or migrate the binding. Reaction to either belongs to
Routers and future lifecycle policy, not to the binding.

Property: neither adding a preferable candidate nor removing the selected one changes any fact the
binding records.

## C6 — an unresolved binding fails explicitly and is never created pending

No candidate satisfying the constraint is an explicit, explained failure. There is no partially
resolved, deferred, or live-query binding state to observe.

Property: every resolution yields either a binding with a selected candidate or a failure carrying
its per-candidate explanation; no third state exists.

## C7 — an unevaluatable constraint excludes the candidate it was evaluated against

An unrecognised atom anywhere within a composite expression makes the whole expression unevaluatable
for that candidate, which in selection context is candidate exclusion — §10.1 and §18.1, and the
poisoning rule `BR-07-CONSTRAINT-001` already delivered. Exclusion is recorded with the unsupported
constraint named, and does not exclude any other candidate.

Property: an unevaluatable candidate is excluded with its unsupported constraints named, and a
candidate the same expression evaluates cleanly is still selectable.

## C8 — selection is deterministic, including under ties

Candidates are evaluated in one stated total order, and the first satisfying candidate is selected.
Equal candidates therefore resolve identically on every run and in both stacks.

Property: the same candidate set and Attribute values select the same candidate, whatever order the
caller supplied them in.

## C9 — restoration reproduces the resolution without reselecting

A recorded binding restores to the same selected candidate and the same effective values without
consulting any candidate or Attribute source. Restoration cannot observe a source, so it cannot
silently reselect against one.

Property: restoration takes no candidate set and no Attribute source, and a restored binding equals
the one recorded even when the sources it was resolved from now say something different.

## C10 — both stacks implement independently, and evidence is bounded

Reference and Minimal each implement this natively in their own experimental Composition project.

The evidence proves one-time resolution, provenance, non-rebinding, explicit failure, and restoration
for statically declared candidates and Attribute values. It is Architecture 0.7 Complete Draft
evidence and not a Brontide Base conformance claim. It does not define the portable Attribute
description Operations, the freshness, confidentiality, or attestation model, what policy reacts to a
changed Attribute, how oscillation or unavailable Attribute providers are handled, or any Router
behaviour — all of which §18.1 leaves open.

Property: every status statement about this requirement preserves these limits.
