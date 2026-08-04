# CBI44 contract-completeness review

Date: 2026-08-04

Scope: absence review of the launch-time trust decision, separate from conformance tests. It asks
what the contract does *not* say, per capability.

## Findings and dispositions

1. **The obvious guard here is unreachable, and writing it would have been the defect.** A launch
   decision naturally wants to check that the artifact it authorizes is the one the store staged.
   It cannot fail: the evidence is over the acquisition request, the store staged under that
   request's identity, and CBI36 already refuses an authorization whose content identity does not
   match. Disposition: **no refusal code**, because a declared category with no reachable path is
   what PB6 found three of. The equality is asserted as a property over every vector that reached a
   decision, which is the nearest observable thing rather than a manufactured path.

2. **The snapshot-comparison design passes five of the six vectors.** "Revalidate" suggests
   comparing the policy the launch sees with the one that authorized acquisition, and every vector
   except `unrelated-revocation` is green under that reading — including both revocation vectors,
   for the wrong reason. Disposition: **the vector is the contract.** It is the only one that forces
   the question, and both stacks were checked against it by deliberately building the wrong design
   and watching C3 go red. Without it two independent implementations would have agreed on a chain
   that refuses every benign policy update, which is Decision 10's shape exactly: the contract would
   have been silent, and silence is where independence is blind.

3. **This step is a barrier and CBI43's is not, and only breaking each shows which.** CBI43 recorded
   that deleting its trust gate opens no source, because the governed acquirer refuses a missing
   authorization anyway — the step earns its place by attribution. The two steps look alike in the
   code. Disposition: **checked by deliberate defect, both stacks.** Deleting this one launches a
   revoked publisher's executable and reports `active`. Symmetry of appearance is not evidence of
   symmetry of function, and the only way to know which is which is to remove each and look.

4. **The launch decision could have been ordered after the store's reverification.** Disposition:
   trust is decided first, so a lapsed publisher is never reported as whatever the staged bytes
   happened to look like. **No vector separates the two**, because the store has just staged the set
   and its reverification cannot fail there, so the ordering is stated rather than pinned — recorded
   here instead of given a manufactured path.

5. **Removing the staged set could have been read as a security act.** Disposition: it is not, and
   the contract says so. The bytes are content-addressed and re-acquirable, their integrity is not
   what lapsed, and nothing else holds a lease on them; removal is CBI43's residue rule applied to
   one more stage. A later slice that wants a warm cache is free to keep them without reopening a
   safety question.

6. **The window is provoked from the artifact source, not by a concurrent writer.** A single chain
   call has no other seam, and this is CBI41's device for reaching CBI39's superseded cursor. The
   write lands after the governed acquirer has already checked supersession, which is what makes it
   the post-acquisition window rather than CBI36's. Disposition: explicit. It does **not** establish
   behaviour under a genuinely concurrent revocation — which CBI38's one-process, one-writer bound
   excludes anyway, and which is the same disagreement between two slices that CBI41 recorded rather
   than resolved.

7. **Three vectors leave the stored floor behind the live registry, and that is the rule rather than
   a residue failure.** A policy applied mid-chain publishes a checkpoint and hands off no floor,
   because CBI42 advances the floor only from a publication this host performed through a poll.
   Disposition: pinned as `registrySequence: 2` against `storedFloor: 1` so a reader cannot mistake
   the lag for a defect, and so a later implementer cannot 'fix' it by letting any writer raise the
   guard — which is exactly what CBI42 refuses.

8. **The publisher signature is not verified a second time.** Disposition: stated. CBI34 declares no
   freshness, the key identity is a digest of the key, and the chain holds its own verification
   rather than a caller's claim, so re-checking would assert a fact already held. What changes
   between acquisition and launch is the policy, and that is what is re-read.

9. **The launch decision is reported and not retained.** Both policy identities are on the result, so
   a host can record which policy a running provider was launched under, but the chain stores
   nothing and the portable member carries nothing. Disposition: deliberate — durable provenance for
   a running member is a different slice's work.

10. **Nothing revalidates after Release.** A revocation arriving while the provider is serving is not
    seen. Disposition: outside the window by construction. Closing it needs a seam that observes a
    running member rather than a launching one, which is CBI5's shape applied to trust rather than
    to authority.

11. **One provider, one member, one poll, one chain call.** Disposition: bounded deliberately, as
    CBI43 is. Fan-out, child Ports, and multi-member activations have their own slices upstream of
    the seam and none of them reaches this decision differently.

## Result

The CBI44 contract is complete for the window between an acquisition returning a staged set and its
executable being launched, within one chain call.

The slice's own first draft was correct, which is unusual here and worth attributing rather than
claiming: CBI43 had already named the gap precisely, so there was little left to get wrong about
*what* to build. What the falsification pass changed was the contract, not the code — two of the
checks a reader would expect are absent on purpose, the content-identity guard because it cannot
fail and the ordering claim because no input separates it, and both are now stated as absences
instead of being quietly present or quietly missing.

The finding worth carrying is finding 3. The distribution programme now has two trust steps that
read identically at the call site and differ completely in what they are for, and the difference was
established by deleting each and observing what still happened. A reviewer reading either one in
isolation would have called it a safety check, and for one of them that is wrong.

The next boundary is the one finding 10 names: trust revalidation for a member that is already
serving. Custody in a domain the checkpoint's writer cannot reach, endpoint and authority key
rotation, and a real scheduling host remain separate work, unchanged by this slice.
