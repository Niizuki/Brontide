# CBI38 capability contract — durable publisher-trust policy checkpoint

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI38 durably records the complete verified CBI37 update chain in one host-owned checkpoint, recovers
the current policy after restart by replaying every signed update, and detects rollback relative to
an issuer-controlled recovery floor retained by the host outside the checkpoint file.

This is not a database, distributed consensus protocol, TPM counter, operating-system keystore,
backup service, network policy fetcher, multi-process writer, authority rotation scheme, or defense
against an attacker able to replace both the checkpoint and the independently retained floor.

## Capabilities

### C1 — one bounded canonical checkpoint contains the complete signed chain

The checkpoint has a versioned binary encoding, pinned authority identity, bounded update count and
size, and every complete CBI37 update field including policy entries and signature evidence. Paths
and bare strings are confined to the file boundary. Oversized, truncated, trailing, or malformed
state is `policy-checkpoint-corrupt`.

### C2 — recovery re-verifies provenance and the complete monotonic chain

Recovery constructs a new CBI37 registry with the expected authority pin and replays every stored
update through its native verifier. Invalid authority, signature, policy identity, sequence,
predecessor, replay, gap, or fork refuses recovery and exposes no registry. An absent checkpoint is
valid only when no positive recovery floor is required.

### C3 — publication is atomic and crash residue is inert

Each accepted update is first validated against a shadow replay, then the complete successor chain
is flushed to a private sibling temporary file and atomically replaces the checkpoint. Only after
publication succeeds does the live registry advance. An abandoned temporary file is ignored and
removed on open; no partial checkpoint becomes current.

### C4 — an external recovery floor detects checkpoint rollback

Successful application and recovery return an issuer-controlled floor naming authority, sequence,
and policy identity. Recovery below that sequence, or at the same sequence with another identity,
is `policy-checkpoint-rollback-detected`. A missing checkpoint with a positive floor is also rollback.

### C5 — checkpoint failure preserves live state and governed acquisition

Write, flush, or replacement failure is `policy-checkpoint-write-failed`; the live current snapshot
and floor remain unchanged. A recovered or successfully advanced durable registry can create the
existing CBI37 governed acquisition gate without exposing a bypass to its mutable inner registry.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume shared CBI38 vectors and report the same recovery
or update code, sequence, policy identity, rollback observation, residue observation, and recovered
governed-acquisition result.

## Phase-wide properties

- No unverified or non-monotonic stored update can recover current policy state.
- Every failed publication leaves both durable current state and live current state unchanged.
- Every successful state transition returns a floor for exactly the state made durable.
- Recovery never accepts state older than or conflicting with its supplied floor.
- Governed acquisition after recovery uses only the recovered current policy snapshot.
