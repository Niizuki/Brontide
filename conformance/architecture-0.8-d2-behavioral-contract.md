# Architecture 0.8 A08-D2 behavioral contract

Status: authorized experimental runtime slice; not Architecture 0.8 ratification.

This contract bounds A08-D2 to Complete-Draft changes C6 and C2. It supersedes the runtime's
separate `DelegationAllowed` representation, but does not retarget either stack from Architecture
0.7 or rewrite pinned 0.7 evidence.

## Capability contract

### D2-C1 — Delegation is default-on

An unadorned Capability can be derived for another Actor without a separately granted Boolean
right. Derivation changes the holder, records the parent, preserves the target and Operation set,
and adds only Constraint expressions. `BR-08-ADV-C6-001` is the canonical vector.

Property: every successfully represented derivation has exactly one parent and cannot acquire an
Operation, target, or Constraint omission relative to its complete ancestor chain.

### D2-C2 — Further Delegation is Constraint-narrowed

The standard delegation-depth Constraint is evaluated relative to the link that carries it. A
maximum additional depth of zero permits the carrying Capability itself but makes every descendant
invalid at presentation. Derivation remains a structural, effect-free operation and does not need
to evaluate the Constraint; the target denies a purported child or deeper descendant before an
Operation effect begins. `BR-08-ADV-C6-002` is the canonical vector.

Property: for every presented Capability, every delegation-depth Constraint in its full chain is
evaluated against the number of links below the Constraint's carrying Capability; any exceeded
ceiling denies with zero effects.

### D2-C3 — Delegation implicitly demotes origin

Every derived Capability automatically adds the ordinary Constraint `origin-assertion: at most
Origin.Derived`. A descendant that requests `Origin.Device`, `Origin.Human`, or
`Origin.Autonomous` is denied before effects even when a primordial ancestor carried that origin
grant. A delegated assertion right may assert `Origin.Derived`; an unexercised assertion remains
`Origin.Unverified`. `BR-08-ADV-C2-001` is the canonical vector.

Property: every non-primordial Capability carries an origin-ceiling Constraint at its own link, and
no complete derived chain authorizes an origin stronger than `Derived`.

### D2-C4 — Primordial origin remains vouched

A primordial Capability carrying a genesis-grade origin grant can still assert the granted source
class. Successful occurrences record that class. `BR-08-ADV-C2-002` is the canonical vector.

Property: origin demotion is introduced only by Delegation; issuing or presenting a primordial
Capability never adds it implicitly.

### D2-C5 — Fail-closed observability and migration

Delegation-depth and origin-ceiling failures are ordinary Constraint decisions in the same
structural strong-Kleene evaluation path introduced by A08-D1. Their denial is visible and occurs
before handlers or emitted occurrences. The removed Boolean surface has an explicit migration:
omit the former `true` value, and replace `false` with a delegation-depth Constraint of zero.

Property: every D2 denial produces zero handler effects and names the exceeded Constraint boundary;
no Boolean delegability field or issuance parameter remains in either public stack surface.

## Evidence boundary

Each stack executes all four canonical C6/C2 vectors natively and a named phase-wide property test.
The implementations must remain independent. A mechanical evidence map accounts for contract items,
vectors, migration evidence, tests, and runtime anchors. A phase-boundary completeness review is
required before delivery. A08-D3 and later slices remain unauthorized by this contract.
