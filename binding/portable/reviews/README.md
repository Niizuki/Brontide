# PB8 independent reviews

This directory records the fresh independent reviews required by PB8 step 5 of the
[Portable Component Binding implementation plan](../../../docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md).
It is separate from `conformance/reviews/`, whose request and attestations are pinned to the closed
implementation-correction programme and must not be repurposed.

## Review request

- **Authorized:** 2026-08-11 by `user:JakHoh`.
- **Pinned target:** `ab94ad742104f6939b4a378373f2d68b285c3751` (the merge of Decision 13).
- **Current architecture:** the revision selected by
  [`Brontide-Architecture-Status.json`](../../../Brontide-Architecture-Status.json), including its
  status and the registry's latest-ratified entry.
- **Local targets:** each stack's own `README.md`, status, and limitations at the pinned target.
- **Requirements:** C1-C10 as stated by the pinned
  [`contract-matrix.md`](../contract-matrix.md), including the declared 0.1 limits and the executed
  evidence table rather than an inferred broader capability.
- **Required records:** independent Reference, Minimal, and neutral-contract attestations.

Each automated reviewer has a distinct reviewer and session identity, starts in a fresh isolated
context, has no access to the implementation session's private reasoning, and inspects an isolated
snapshot of the pinned target. For every C1-C10 item the reviewer records exactly one of `conforms`,
`approved-disposition`, or `does-not-conform`, with rationale and concrete evidence. Each record also
states the commands actually run, their results, current-architecture assessment, limitations,
findings, overall verdict, `freshContext: true`, and `implementationContextAccess: none`.

The three scopes are deliberately separate:

- [`pb8-reference-attestation.md`](./pb8-reference-attestation.md) reviews the Reference realization;
- [`pb8-minimal-attestation.md`](./pb8-minimal-attestation.md) reviews the Minimal realization; and
- [`pb8-neutral-attestation.md`](./pb8-neutral-attestation.md) reviews the data-only contract,
  schemas, vectors, golden encodings, Channel mapping, and neutral-provider boundary.

PB8 review closure requires all three records to be complete and to contain no unresolved in-scope
finding. A negative completed review remains evidence and blocks closure until the defect is pinned,
corrected, and independently reviewed against a newly pinned target. Review completion establishes
experimental Portable Binding 0.1 evidence only; it does not ratify Channel, promote a public stable
version, or implement the versioned 0.2 work recorded by Decision 13.
