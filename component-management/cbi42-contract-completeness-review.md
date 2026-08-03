# CBI42 contract-completeness review

Date: 2026-08-03

Scope: absence review of durable recovery-floor custody, separate from conformance tests. It asks
what the contract does *not* say, per capability.

## Findings and dispositions

1. **The floor could have been advanced by a recovery.** Disposition: refused, and this is the
   slice's finding. CBI38's `Open` returns a floor derived from the replayed checkpoint, and writing
   it back is the obvious thing to do — it would even close CBI41's crash-window lag. It is exactly
   wrong: **a checkpoint that can raise its own guard makes the guard follow whatever the checkpoint
   says.** A forged chain reaching further than the true one would be adopted as the floor and would
   then refuse every genuine successor, wedging the host at the forgery with no way back that does
   not involve deleting the guard. The floor advances only by a handoff from a publication this host
   performed.
2. **CBI41's "self-heals on the next recovery" is narrower than it reads.** Disposition: corrected
   here rather than left standing. The *in-memory* floor is reissued from the recovered checkpoint
   and is correct for the rest of that process; the *durable* floor stays one behind until the next
   handoff advances it. Both statements are true and the earlier one did not distinguish them.
3. **Absence of the store was ambiguous.** Disposition: removed rather than answered. A missing store
   could mean "nothing has happened yet" or "the guard was deleted", which need opposite treatment,
   and no examination of the store itself can tell them apart. Establishing at sequence zero before
   any checkpoint exists is what makes absence unambiguous afterwards, so **the guard is created
   before the thing it guards**. A checkpoint with no store is then refused.
4. **That refusal could have reintroduced the false alarm CBI41 argues against.** Disposition:
   checked. A crash between the first publication and the first handoff leaves a store at zero
   beneath a checkpoint at one, and zero never trips rollback detection, so the start opens. The
   dangerous window closes because establishment precedes publication rather than following it.
5. **The integrity tag could have been asserted without being reachable.** Disposition: found and
   fixed during this slice. The first three corruption vectors — a flipped version marker, a
   truncation, and a trailing byte — are all refused by structural parsing *before* the tag is
   consulted, so a store that never checked its tag passed every one of them. A deliberate defect
   proved it. `start-tampered-sequence` alters a byte the parser accepts, yielding a different but
   entirely well-formed sequence, and only the tag refuses it. This is the same shape as PB6's
   defects: a check nothing could fail.
6. **A same-sequence fork could have been read as an advance.** Disposition: refused as a regression.
   Two policies at one sequence are two chains, and adopting the second would leave the floor unable
   to recognise the chain it was retained from. Only an identical floor is idempotent.
7. **The tag suggests protection it does not provide.** Disposition: stated plainly rather than
   implied away. It detects corruption and truncation. An adversary who can write this file
   recomputes it, and no keyed construction fixes that while the key lives beside the file. **Real
   custody is a separate privilege domain, and this slice does not have one** — which is why CBI38
   declined to claim secure floor custody and why this one does too. What CBI42 adds is that the
   floor now exists, is monotone, and is consulted, none of which was true before.
8. **A refused handoff could have been swallowed by the sink.** Disposition: prevented. The sink
   raises on any non-retained code, so CBI41 reports an advanced-but-unretained floor rather than
   the cycle believing custody it does not have.
9. **The store's regression refusal is unreachable through the composition.** Disposition: stated,
   not manufactured. A store above its checkpoint is precisely what the start refuses, so no
   end-to-end path reaches a regressing handoff. It is pinned directly instead, against a store
   seeded above the registry it is handed — the same treatment PB6 gave `peer-unavailable`.
10. **Nothing coordinates two processes over one store.** Disposition: explicit absence, inherited
    from CBI38's single-writer bound. The in-process lock serialises retention within one host and
    claims nothing beyond it.
11. **Nothing bounds how a host obtains the checkpoint and floor paths, or keeps them apart.**
    Disposition: explicit. A host that puts the store inside the checkpoint's own protection domain
    gets no guarantee from it, and the contract says the two must be separately protected without
    being able to check it.
12. **Nothing reads the store except a start.** Disposition: deliberate. There is no periodic
    re-verification, no watch, and no detection of a floor replaced while the process runs; the
    window between two starts is unguarded and is named as such.

## Result

The CBI42 contract is complete for durable, monotone, integrity-checked custody of one host-local
recovery floor and the composition that consumes it. The loop CBI41 opened is closed: a floor is
retained after publication, survives the process, and is what the next start hands to CBI38.

The next boundary should be the one thing this slice repeatedly had to decline — **custody in a
domain the checkpoint's writer cannot reach**, whether a platform rollback anchor, a sealed key, or
an attested counter. Endpoint and authority key rotation remain separate security work, and a real
scheduling host remains separate operational work.
