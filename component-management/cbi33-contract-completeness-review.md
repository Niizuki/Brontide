# CBI33 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI33 attributable acquisition contract, separate from its conformance
tests.

## Findings and dispositions

1. **A source name can be mistaken for publisher identity.** Disposition: prevented. Results retain
   the expected acquisition-source identity and always report `publisher-evidence-not-evaluated`.
   The source identifier never enters the content identity, CBI31 launch policy, or an authority
   decision.
2. **A nominal total limit does not bound individual streams.** Disposition: closed. Every member has
   a non-negative declared length, the checked sum must fit within the positive request limit, and
   copying consumes exactly that length plus one end-of-stream probe byte.
3. **Length arithmetic can overflow before comparison.** Disposition: closed. Both roots compare
   each length against the remaining limit before adding it; no overflowing sum is formed.
4. **Source enumeration order could become transport behavior.** Disposition: closed. The acquirer
   requests each distinct member exactly once in ordinal relative-path order regardless of caller
   collection order.
5. **Successful delivery can conceal a digest failure.** Disposition: prevented. Exact-length
   delivery reports `transport-completed`; CBI32 independently hashes the private bytes and reports
   `artifact-set-integrity-failed` as the separate admission outcome.
6. **Acquisition residue can outlive refusal.** Disposition: closed for owner-controlled filesystem
   operations. All normal, unavailable, length, read, and admission outcomes leave through a
   `finally` cleanup with bounded Windows handle retries. No transaction path is returned.
7. **A source stream can fail while opening, reading, or disposing.** Disposition: closed for the
   declared transport exception family. I/O, access, disposed-stream, and unsupported-operation
   failures report `acquisition-transport-failed`; the transaction is removed.
8. **Byte-bounded is not time-bounded.** Disposition: bounded explicitly. An injected synchronous
   stream can block inside `OpenRead` or `Read`. CBI33 limits consumed bytes, not elapsed time, and
   adds no cancellation, timeout, asynchronous backpressure, retry, or resume protocol.
9. **Content-addressed reuse might skip the attributed transport.** Disposition: deliberately not
   optimized. Each acquisition attempt reads the complete declared source before CBI32 re-verifies
   and reports reuse, so the result describes this attempt rather than a cache lookup mislabeled as
   transport.
10. **Acquired content must not retain the source lifetime.** Disposition: pinned. Activation takes
    only the returned CBI32 staged value. It neither retains the source object nor opens another
    source stream; exact removal succeeds after the activation owner ends.
11. **Transaction coordination remains process-local.** Disposition: bounded. One acquirer serializes
    its attempts and CBI32 owns publication, but separate acquirers have no durable lock, startup
    recovery, retention policy, or garbage collection.
12. **Attribution still proves neither publisher nor trust.** Disposition: the next boundary. Source
    identifiers are host declarations checked for equality, not authenticated claims. CBI33 has no
    signed manifest, key identity, verification algorithm, revocation observation, or trust policy.

## Result

The CBI33 contract is complete for exact byte-bounded transfer from one injected named source into
the CBI32 transaction, with transport, publisher evidence, and local admission kept visibly
separate. It does not claim a network transport or trusted distribution. The next physical
distribution slice should verify attributable publisher evidence over the canonical manifest while
keeping cryptographic validity separate from host trust and CBI32 admission.
