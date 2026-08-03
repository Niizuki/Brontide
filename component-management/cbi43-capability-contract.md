# CBI43 capability contract — the distribution chain end to end

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI43 runs the distribution programme as one path rather than as pairs. A floor-guarded, durably
checkpointed trust policy arrives by CBI41 polling; that policy authorizes CBI34 publisher evidence
over a CBI33 acquisition request; the governed acquisition stages a content-addressed artifact set;
the store launches its executable as a provider process under a removal lease; and CBI30 activates a
portable member across that process to Release.

It adds no new distribution capability. Every refusal it reports belongs to the slice that made it,
and CBI43 neither reclassifies nor supplements them. It is not a package format, a discovery service,
a deployment tool, a supervision loop, or a security boundary, and the fake Component Manager remains
host-local throughout.

## Capabilities

### C1 — the chain composes from polled policy to released member

One path carries a poll that applies a CBI37 policy, evidence verification, trust evaluation,
governed acquisition, content-addressed staging, a launched provider process, and a CBI30 activation
that reaches Active and Release. The launched provider answers the portable contract across a real
operating-system process boundary.

### C2 — the ladder is monotone, and each vector pins where it stopped

Policy applied, authorized, source opened, staged, launched, and released are reported for every
vector, and they form a true-prefix: once one is false every later one is false. A stage runs only
because every earlier stage succeeded, so "how far did it get" is a checked observation rather than
an inference from a single code.

### C3 — an unavailable or refusing policy opens no source

A poll that applies nothing leaves the governed acquirer without a current policy and the chain
refuses before evidence is weighed. A policy that revokes or does not know the publisher key, and an
authorization whose content or payload does not match the request, all refuse before the source is
opened. Source reads are counted, and in each of these the count is zero.

### C4 — a refusal leaves nothing behind

No refused vector leaves a staged set in the store, a live provider process, a held removal lease, or
an advanced recovery floor. Transport that completes and then fails local admission stages nothing,
which is CBI33's separation observed from the far end of the chain.

### C5 — the floor advances once for the applied policy and survives the chain

The poll retains exactly the floors its applied updates produced, and the chain that follows neither
advances nor regresses them. A restart after the chain opens under the retained floor.

### C6 — the provider launched is the artifact acquired

The executable the chain activates is the one the store staged and reverified under its content
address, not a path the caller supplied. The launched process's executable resolves inside the store
root and matches the manifest digest the publisher signed.

### C7 — both roots run the same chain and agree

Reference C# and Minimal F# independently compose their own slices over the shared vectors and
independently report the ladder, the refusing code, the retained floor, and the residue checks.

## Phase-wide properties

- The six ladder observations of every vector form a true-prefix followed by false.
- Every vector that did not stage leaves the store with no set, and every vector that staged and then
  refused leaves none either.
- No vector leaves a provider process running after the chain returns.
- The stored floor after a vector equals the number of updates its poll applied, and never decreases.
- A vector that opened no source read zero bytes from it.
