# CBI43 contract-completeness review

Date: 2026-08-03

Scope: absence review of the end-to-end distribution chain, separate from conformance tests. It asks
what the contract does *not* say, per capability.

## Findings and dispositions

1. **The chain could have reclassified its members' refusals.** Disposition: refused. Every failure
   keeps the code its slice produced and gains only an origin. A composition that renamed
   `publisher-key-revoked` into a chain-level code would make the programme's most useful diagnostic
   disappear at the exact point a host reads it — which is the defect CBI29 found in the child-Port
   wrapper and this slice is where it would recur.
2. **The trust gate turned out to be about attribution, not protection.** Disposition: found by
   deliberate defect, and recorded rather than removed. Deleting CBI43's own trust check does **not**
   open a source: the governed acquirer refuses a missing authorization on its own. What it destroys
   is the reason — "the policy revoked this publisher" becomes "trust was required". The step earns
   its place by preserving attribution, and the contract now says so instead of implying a second
   safety barrier that does not exist.
3. **A ladder could have been asserted per vector and never as a rule.** Disposition: prevented. The
   six observations are checked as a true-prefix over *every* vector, so a stage that ran without its
   predecessor is a failure even in a vector written for something else.
4. **Transport completion and admission refusal could have merged.** Disposition: preserved. A
   delivered set whose digest then fails is CBI32 refusing admission, not CBI33 failing transport,
   and the chain reports the two apart because CBI33's contract keeps them apart. The first draft
   guessed the transport code and reported the wrong origin; the shared vector caught it.
5. **A refusal could have left residue.** Disposition: checked in three places at once — no staged
   set, no running process, no advanced floor — over every refusing vector rather than the one the
   case was written for.
6. **The executable could have been the caller's rather than the publisher's.** Disposition: pinned.
   The launched path must resolve inside the content-addressed store, so the bytes that run are the
   ones the store reverified under the identity the publisher signed.
7. **The chain does not re-poll, retry, or recover.** Disposition: explicit. One poll, one
   acquisition, one launch, one activation. Scheduling is CBI41's, and nothing here reacts to a
   provider that dies after Release.
8. **Nothing verifies the policy is still current when the artifact is launched.** Disposition:
   stated. The authorization is checked against the snapshot in force at acquisition, and a
   revocation arriving between acquisition and launch is not seen. Closing that would need a
   revalidation seam this slice does not define.
9. **The vectors exercise one provider and one member.** Disposition: bounded deliberately. The
   chain's claim is that the stages compose in order, not that they compose under fan-out, child
   Ports, or multi-member activations, all of which have their own slices upstream of the seam.
10. **It remains a fake manager over a host-local store.** Disposition: unchanged from CBI30 onward.
    Nothing here is a package format, a discovery service, a deployment tool, or a security boundary.

## Result

The CBI43 contract is complete for one ordered pass through the distribution programme, from a polled
and floor-guarded trust policy to a released portable member over a launched provider process, with
every stage's refusal stopping the chain and keeping its origin.

Composing the slices found no defect in them, which is itself worth recording: the parts were built
against contracts that already fitted. What it found was in the composition's own first draft — a
guessed transport code and a trust step whose value was not what it looked like.

The next boundary remains the one CBI42 named and this slice does not touch: **custody in a domain
the checkpoint's writer cannot reach**. Revalidating policy between acquisition and launch, endpoint
and authority key rotation, and a real scheduling host remain separate work.
