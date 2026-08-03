# CBI42 capability contract — durable recovery-floor custody

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI42 gives the CBI38 recovery floor a durable host-local store, the CBI41 sink that advances it, and
the composition that reads it back at the next start and hands it to `Open`. It closes the loop CBI41
only hands off.

This is not secure custody. The store is an ordinary file whose integrity tag detects corruption and
truncation, not an adversary who can write it; a checksum the attacker recomputes defends nothing.
Real custody is a separate privilege domain, a secure element, or a platform rollback anchor, and
none of those is here. Nor is this cross-process coordination, a lock manager, key rotation, a
retention schedule, or a replacement for CBI38 publication or CBI37 authority.

## Encoding

The store holds one record followed by a 32-byte SHA-256 tag over exactly the bytes preceding it.
The record is, in order: the length-prefixed UTF-8 string `CBI42`; the length-prefixed authority
identity; the sequence as a big-endian 64-bit integer; a big-endian 32-bit presence marker; and, when
the marker is 1, the length-prefixed policy identity. Every string is prefixed by its byte length as
a big-endian 32-bit integer. Both realizations produce identical bytes, and a shared golden SHA-256
pins the image.

## Capabilities

### C1 — the floor is durable, atomic, and integrity-checked

A retained floor is written to a private temporary sibling, flushed, and moved into place, so a crash
leaves either the previous floor or the new one and never a torn record. A stored image whose tag
does not match, that is truncated, that carries trailing bytes, or whose markers or identities are
invalid is refused rather than interpreted.

### C2 — the store is established before the checkpoint it guards exists

A start holding neither store nor checkpoint establishes the store at sequence zero. **A start
holding a checkpoint and no store is refused.** Absence would otherwise mean either "nothing has
happened yet" or "the guard was removed", and those need opposite answers; establishing at zero up
front is what makes them distinguishable. It also avoids the false alarm CBI41 argues against, since
a crash between the first publication and the first handoff leaves a store at zero beneath a
checkpoint at one, which opens.

### C3 — the stored floor is what `Open` consumes

Every start reads the store and supplies that floor to CBI38, so rollback detection is in force from
the first start that has anything to detect. A refused store refuses the start and yields no
registry: once a floor is owed, nothing opens without one.

### C4 — the floor advances only by a handoff, never by a recovery

The floor CBI38 returns from a recovered checkpoint is a report, not an instruction, and is never
written to the store. **A checkpoint cannot raise the floor that guards it.** If it could, a forged
chain reaching further than the true one would be adopted as the new floor and would then refuse
every genuine successor, wedging the host at the forgery permanently. This narrows CBI41's note that
the crash-window lag "self-heals on the next recovery": the in-memory floor does, for that process,
and the durable floor does not.

### C5 — retention is monotonic and idempotent

A floor below the stored sequence, or at the stored sequence under a different policy identity, is
refused as a regression and leaves the stored bytes unchanged. The identical floor is accepted as
unchanged, so a handoff repeated after a partial failure is not itself a failure. A floor naming a
different authority is refused. As a CBI41 sink, any refusal is reported to the cycle as an
unretained floor rather than swallowed.

### C6 — the composition closes CBI41's loop end to end

A poll cycle applies updates through the real store as its sink; the process is torn down; a fresh
start reads the store and opens the checkpoint at the sequence the cycle reached. A checkpoint rolled
back beneath the stored floor is refused at that start rather than served.

### C7 — both roots agree on the shared vectors

Reference C# and Minimal F# independently consume the shared vectors and the golden image, and
independently compute the outcome code, the checkpoint code, whether a registry opened, the stored
sequence before and after, and whether the stored bytes changed.

## Phase-wide properties

- No refusal, at any layer, leaves the stored bytes changed.
- The stored sequence never decreases across any vector.
- No vector ends with a stored sequence the store was not explicitly asked to retain, so no path
  derives the floor from a checkpoint.
- A start that refuses yields no registry and no store handle.
- Every start that opens supplies CBI38 a floor whose authority is the one the caller pinned.
