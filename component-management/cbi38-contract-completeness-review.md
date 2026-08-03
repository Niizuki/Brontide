# CBI38 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI38 durable publisher-trust policy checkpoint, separate from
conformance tests.

## Findings and dispositions

1. **Persisting only the current unsigned policy could discard provenance.** Disposition: prevented.
   The checkpoint contains every complete signed CBI37 update and recovery replays the whole chain.
2. **Malformed or unbounded state could consume arbitrary recovery resources.** Disposition: bounded.
   The versioned canonical binary format limits file size, update count, entry count, and string
   length, uses strict UTF-8, and refuses truncation and trailing bytes.
3. **A crash could expose a partially written checkpoint.** Disposition: prevented for the declared
   filesystem boundary. Publication writes a private sibling, flushes it through the file handle,
   and replaces the destination; abandoned sibling files are inert. Directory-metadata persistence
   and atomic replacement semantics remain those of the host filesystem.
4. **Memory could advance when durable publication fails.** Disposition: prevented. A shadow registry
   validates the successor, the full successor chain is published, and only then does the live
   registry apply the update.
5. **Recovery could trust serialized verification results.** Disposition: prevented. It creates a new
   CBI37 registry under the expected authority pin and re-verifies every signature and chain link.
6. **An older valid checkpoint could be restored.** Disposition: detected when the host supplies its
   independently retained authority, sequence, and policy-identity floor. Missing, older, or
   same-sequence conflicting state is refused.
7. **Checkpoint and floor could both be replaced.** Disposition: bounded. Secure custody of the floor
   is a host/deployment responsibility; CBI38 is not a TPM counter or operating-system keystore.
8. **Concurrent processes could race checkpoint replacement.** Disposition: bounded. The registry is
   single-process and single-writer; cross-process locking or consensus is not claimed.
9. **Authority compromise or rotation is absent.** Disposition: bounded. The authority remains one
   immutable CBI37 pin with no threshold, successor, emergency recovery, or transparency log.
10. **Recovered revocation could terminate existing artifacts.** Disposition: intentionally absent.
    Recovery governs future acquisition; staged artifacts, leases, and running processes retain their
    existing lifecycle.

## Result

The CBI38 contract is complete for bounded single-writer checkpointing, crash recovery, and rollback
detection against a separately retained floor. Production remote policy distribution remains a new
boundary: transport authentication, freshness, bounded retry, and update delivery must be specified
without weakening the durable registry or treating its external floor as securely stored by itself.
