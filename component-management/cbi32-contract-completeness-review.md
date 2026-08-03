# CBI32 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI32 content-addressed provider staging contract, separate from its
conformance tests.

## Findings and dispositions

1. **A file digest is not a complete multi-file identity.** Disposition: closed. The identity covers
   a version marker, ordered safe relative paths and member digests, executable designation, and
   parsed arguments. A neutral golden manifest prevents both roots from sharing one unnoticed
   canonicalization error.
2. **Copying verified source bytes directly into the final path exposes partial state.** Disposition:
   closed for handled failures. Every member is copied and rehashed in a private sibling directory;
   only a same-filesystem directory move publishes the final content address. Missing or changed
   members remove the transaction directory.
3. **Content-addressed reuse can preserve corruption.** Disposition: closed. Reuse verifies the
   exact declared path set and every digest. Missing, additional, inaccessible, or changed staged
   content is refused and is not silently repaired or replaced.
4. **Staging could accidentally become activation.** Disposition: prevented. `Stage` owns no process
   path or lease. A staged set can be removed immediately. Only explicit `Activate` enters CBI31 and
   therefore retains its launch policy, dedicated-process observation, CM4 Release, retirement, and
   cleanup.
5. **Source disappearance must not invalidate acquired content.** Disposition: pinned. A shared
   vector deletes the copied source tree before activation; the staged set remains independently
   launchable and removable.
6. **Removal can race activation or erase a sibling.** Disposition: closed inside one store owner.
   Stage, activation, lease acquisition, and removal share one lock; an active identity is refused,
   while removing a second identity preserves the first one's exact directory.
7. **Caller-owned mutable manifests can change during Reference validation.** Disposition: closed.
   Reference snapshots the file and argument collections on entry. Minimal lists are immutable by
   construction. Successful results in both roots carry detached manifest values.
8. **Windows build and scanner handles can be shorter-lived than a valid staging attempt.**
   Disposition: corrected. The first strengthened cross-root run produced one clean
   `artifact-set-stage-failed` from a transient file denial. Both roots now retry private copy and
   publish operations for a bounded 100 milliseconds; digest mismatch remains an immediate refusal.
9. **Read-only files are not an immutability or sandbox guarantee.** Disposition: bounded. The bit is
   an accidental-write guard inside a host-controlled store. A same-authority actor can change it;
   CBI32 detects that change on reuse or activation but does not prevent it.
10. **The lease table is process-local and crash recovery is absent.** Disposition: bounded. Removal
    safety applies to one `ContentAddressedProviderStore` owner. A process crash may leave a private
    transaction directory, and another process does not observe leases. Durable locking, startup
    recovery, retention, and garbage collection remain future work.
11. **Artifact removal is not information removal.** Disposition: pinned structurally. The store can
    address only its own validated digest directories and never receives a Dataset path or handle.
    CBI32 therefore cannot interpret Component removal as Dataset deletion.
12. **Local staging still has no acquisition transport or source attestation.** Disposition: the next
    boundary. CBI32 accepts an already-present host-controlled source tree. It does not stream bytes
    from a remote source, authenticate a publisher, verify signatures, or decide trust.

## Result

The CBI32 contract is complete for transactional staging, reuse, leased activation, and exact
removal of one declared multi-file provider set under one host-local store owner. It closes CBI31's
mutable-source and multi-file bounds without claiming durable package management. The next physical
distribution slice should define a bounded, attributable acquisition stream into the staging
transaction, keeping transport success separate from publisher evidence and local admission.
