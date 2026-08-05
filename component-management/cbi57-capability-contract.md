# CBI57 capability contract — policy-authority key rotation

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI37 pins exactly one policy authority out of band and verifies every publisher-trust policy update
against it; CBI38 retains the complete signed chain and re-verifies it against that same pin on every
start. CBI57 lets the authority that signs policy move to a successor without moving the out-of-band
pin and without asking the host to trust the retained chain it can no longer verify.

The pin stays immutable and stays the anchor of every recovery. A rotation is one link in the same
retained chain the policy updates form: it names the generation, the chain point it takes effect at,
its predecessor, and its successor, and it carries both the predecessor's signature and the
successor's countersignature over the same canonical CBI57 manifest. Recovery replays the chain in
stored order from the pin, so every retained update is re-verified against the authority in force at
its own position.

**This is where CBI57 differs from CBI56, and the difference decides the design.** CBI56 keeps its
endpoint successor in a separate anchor and proves possession by completing one live CBI39
synchronization under the staged key, because an endpoint key authenticates a response that is
evaluated once and leaves nothing durable behind. A policy-authority key signs the record CBI38
replays on every start, so its rotation is a fact about *history* rather than about *now*: a successor
recorded beside the chain would leave recovery unable to verify the updates the predecessor signed,
and trusting them unverified is what CBI38's replay exists to prevent. For the same reason CBI57
needs no staging phase — possession of an authority key is proven by a signature, which is exactly
what an authority key does, so the countersignature the statement already carries is the proof, and
there is nothing a later network attempt could add.

This is host-local cooperative authority rotation. It is not CBI56 endpoint rotation, certificate
chains or PKI, authority naming, transparency logging, quorum or threshold signing, key custody,
distribution of rotation statements, or remediation of a compromised predecessor key.

## Capabilities

### C1 — the pin is immutable and the active authority is derived from it

Opening a checkpoint still matches the stored authority against the out-of-band pin exactly. The
active authority and its non-negative monotone generation are computed by replaying the retained
chain from the pin; neither is supplied by a caller, and a rotation never rewrites the stored pin.

Property: for every usable registry the active authority is the last successor reachable from the pin
by a completely verified chain, and generation zero means the pin itself.

### C2 — only the active authority can authorize its exact successor, and only with the successor's countersignature

A rotation statement names generation plus one, the exact active predecessor, one distinct successor,
the chain point it applies at, `ECDSA-P256-SHA256`, both SPKI encodings, and two RFC 3279 DER
signatures over one canonical CBI57 manifest. Each SPKI digest must equal the identity it claims and
import as exact P-256 key material. The predecessor signature authorizes the transition; the
successor signature over the same bytes proves the successor's private key exists and names the
transition it is accepting.

Property: no statement missing either a valid predecessor signature or a valid successor
countersignature over the same manifest advances the generation.

### C3 — a rotation is one atomic durable link, and there is no staged state

A rotation is refused without changing a byte when its generation is not exactly the successor of the
active generation, its predecessor is not the active authority, its successor equals its predecessor,
its declared chain point is not the registry's current sequence and policy identity, or its evidence
is malformed. An accepted rotation is published to the checkpoint before the live authority advances,
as CBI38 publishes an update. No announcement, stage, or unconfirmed successor exists at any point.

Property: every rotation reported as applied is durable and live, and every refusal leaves both the
checkpoint bytes and the active authority unchanged.

### C4 — retirement is immediate and is not retroactive

After a rotation the retired predecessor can sign nothing further: a later update carrying its key is
refused as `policy-update-authority-mismatch`. The updates it signed before the rotation stay valid
and are re-verified as its work on every recovery. A successor cannot re-sign, reissue, or narrow
what a predecessor signed, and a chain in which an update precedes the rotation that authorized its
signer does not open.

Property: replay verifies each retained update against the authority in force at its own position,
and admits a chain exactly when every link verifies there.

### C5 — the retained record stays bounded, strictly decoded, and readable across the change

The checkpoint record advances its format marker only when a rotation exists, so a rotation-free
chain is written in the CBI38 record shape and a checkpoint written before this slice opens
unchanged. A chain containing a rotation is written as a bounded, tagged link sequence. Decoding is
total: a truncated, extended, over-long, unknown-tag, or reordered record is refused as
`policy-checkpoint-corrupt` or `policy-checkpoint-invalid-chain`, and damage to a retained rotation's
evidence is refused rather than replayed.

Property: no stored record produces a usable registry unless every link decodes strictly and
re-verifies.

### C6 — the authority generation has its own externally retained floor

`Open` accepts an optional floor naming a durable generation and active authority. A stored chain
whose generation is below it, or equal to it under a different active authority, is refused as
`policy-authority-rollback-detected` before the registry is usable. The floor is issued from the
durable state and is retained by the host, not by the record it guards; the policy recovery floor
CBI38 issues is unchanged and keeps its own custody.

Property: opening never returns an active authority older than, or conflicting with, the supplied
floor.

### C7 — rotation is not a trust event

A rotation changes no policy, no publisher disposition, and no current sequence, and it retires no
serving member. CBI43's chain records the pinned trust root rather than the signing key, so CBI44's
launch decision and CBI45's serving revalidation compare an identity a rotation does not move, and
they compare the publisher's admission decision rather than the identity of the snapshot that carried
it, as CBI44 established.

Property: across every applied rotation the current policy snapshot, every publisher disposition, and
the pinned identity every downstream slice compares are identical before and after.

### C8 — both roots execute one shared rotation model

Reference C# and Minimal F# independently execute shared vectors covering a valid rotation, a
non-positive and a skipped generation, predecessor mismatch, self-rotation, a wrong chain point, an
unsupported algorithm, a mismatched predecessor key, an invalid predecessor signature, and an
unproven successor. Each root additionally covers recovery of a rotated chain, retirement of the
predecessor, rotation before any policy exists, damage to a retained rotation, the floor, and the
unchanged CBI38 record shape while signing native P-256 evidence.

Property: every shared vector produces the same portable code in both roots.

## Deliberate limits

CBI57 rotates the key that signs publisher-trust policy. It does not rotate or replace the
out-of-band pin, which remains the only identity a host is told to trust and the only one whose
compromise this slice cannot address: a compromised predecessor at generation *g* can still sign a
different successor at *g+1*, and only a retained floor at that generation refuses the alternative,
so remediation of a compromised authority remains a deployment concern rather than a rotation. How
rotation statements reach the host is out of scope; they are supplied to the registry, not fetched.
Custody of the authority floor in a domain the checkpoint's writer cannot reach remains the boundary
CBI42 named and declined.
