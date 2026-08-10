# Architecture 0.8 closure and retargeting contract

Status: authorized implementation-target closure; Complete Draft implementation evidence, not
architecture ratification.

## CL-C1 — the current implementation target is Architecture 0.8

Both stack READMEs name the registry-selected Architecture 0.8 document as their `Designed for`
target. The document is classified as current because its runtime changes are implemented, while
its status continues to say that it is not ratified.

Property: every implementation-target declaration resolves to the same revision, path, and status
as the central registry; no declaration describes Complete Draft evidence as ratification.

## CL-C2 — current delivery evidence accounts for the complete 0.8 change set

One shared current requirements inventory accounts for C1 through C14 and all 33 canonical runtime
vectors. Reference and Minimal each own an independent current-delivery matrix with executable
evidence for C1 through C10 and C12, the recorded representation ceiling for C11, and explicit
non-runtime dispositions for C13 and C14.

Property: every applicable requirement occurs exactly once in each matrix, every runtime vector is
accounted for exactly once, and every `tested` claim has a native test anchor in its owning stack.

## CL-C3 — retained 0.7 evidence stays historical and immutable

The Architecture 0.7 requirements, matrices, plans, ledgers, and their hashes remain unchanged.
They no longer serve as `currentDelivery`; the registry points to new 0.8 artifacts instead.

Property: retargeting changes no byte of the retained 0.7 evidence set and no 0.8 claim is inferred
by rewriting an older matrix.

## CL-C4 — closed reviews remain bound to their reviewed snapshot

The completed implementation-correction review continues to verify the registry snapshot pinned by
its request hash. It is historical evidence about its reviewed commit, not a perpetual assertion
that the live registry can never advance. New 0.8 closure review is handled at the new delivery
boundary and does not rewrite the old attestations.

Property: a closed attestation is validated against the exact pinned registry bytes and reviewed
commit; advancing the live registry neither mutates that snapshot nor misrepresents it as a review
of later evidence.

## CL-C5 — closure does not claim ratification

The registry changes the current architecture status from implementation-evidence-pending to
implementation-evidence-available while retaining `not ratified`. `latestRatifiedArchitecture`
remains `none`. Closure documentation names the remaining decisions separately.

Property: every current target and matrix may claim implemented or tested Complete Draft behavior,
but no artifact may set a ratified revision or use ratification as a synonym for implementation.

## Phase boundary

- No runtime behavior or public API changes in this phase.
- Architecture 0.7 remains readable as retained historical implementation evidence.
- Architecture 0.8 becomes the current implementation target after D1 through D6.
- Formal ratification, standard-vocabulary freezing, and extension ratification remain separate.
