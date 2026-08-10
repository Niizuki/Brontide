# Architecture 0.8 A08-D4 behavioral contract

Status: experimental runtime delivery contract for the separately authorized A08-D4 slice.

This contract delivers C1 liveness-scoped ancestor evaluation and C5 occurrence-pooled quantified
accounting through the explicit Draft-0.8 authority paths. It does not change either stack's
Architecture 0.7 target or ordinary 0.7 execution entry point.

## Capabilities

### D4-C1 — every liveness-scoped ancestor is evaluated at presentation

An expired liveness scope carried by any Capability in the presented derivation chain denies before
the requested effect, including when the expired occurrence belongs to an ancestor rather than the
leaf.

Property: every Draft-0.8 authorization evaluates the complete root-to-leaf chain at the one trusted
presentation instant; no descendant can mask, renew, or replace an ancestor's dead scope.

Evidence: `BR-08-ADV-C1-001` in each native D4 conformance suite.

### D4-C2 — unavailable liveness evaluation fails closed without disclosure

A well-formed liveness-scoped declaration for which the target has no evaluator is Unknown and
denies before effect dispatch. Its diagnostic names the declaration and a stable category but does
not disclose the scope value.

Property: every unavailable liveness evaluation path is effect-free and value-redacted.

Evidence: `BR-08-ADV-C1-002` in each native D4 conformance suite.

### D4-C3 — a live, evaluatable ancestor permits otherwise valid execution

When every liveness-scoped occurrence in the chain is live at presentation and all other Constraints
are satisfied, Draft-0.8 authorization reaches the effect exactly once.

Property: liveness is a narrowing predicate only; evaluating it never grants authority or causes the
requested effect by itself.

Evidence: `BR-08-ADV-C1-003` in each native D4 conformance suite.

### D4-C4 — Base quantified budgets belong to their chain occurrence

The Base execution-count/rate Constraint uses `ChainOccurrencePooling` and positive whole-millisecond
half-open windows aligned to the authority time domain's epoch. Its accounting identity is the
carrying Capability plus the exact expression/atom occurrence and window, not the presented leaf or
holder. Sibling Delegations therefore draw from one ancestor budget.

Property: for every chain occurrence and window, successful authorizations across all descendants
never exceed the declared maximum.

Evidence: `BR-08-ADV-C5-001` in each native D4 conformance suite.

### D4-C5 — denied executions consume no quantified budget

Quantified bookkeeping is prepared during evaluation and committed only after the complete
authority expression and chain authorize. A denial by any unrelated Constraint leaves every
prepared accounting claim unchanged.

Property: every rejected Draft-0.8 execution leaves all occurrence-pooled usage counters identical
to their pre-presentation values.

Evidence: `BR-08-ADV-C5-002` in each native D4 conformance suite.

### D4-C6 — unenforceable declared accounting scopes deny

`ChainOccurrencePooling` is the only quantified accounting scope implemented by Base in this slice.
A vocabulary-defined scope remains identifiable but is declined unless a future vocabulary supplies
an explicit enforcement facility; presenting it is Unknown and denies before effect dispatch.

Property: no quantified declaration is reported as implemented or evaluated unless the target can
enforce its declared accounting scope.

Evidence: `BR-08-ADV-C5-003` in each native D4 conformance suite.

## Phase boundary

- D4 applies only to the experimental Draft-0.8 execution entry points.
- Capability ancestry remains carried in Reference and resolved through `World` in Minimal; no
  revocation or interchange claim is added.
- The status registry, hash-pinned Architecture 0.7 matrices, and both `Designed for` declarations
  remain unchanged.
- Provider authority issuance (A08-D5/C10) and Terminus (A08-D6/C12) require separate authorization.
