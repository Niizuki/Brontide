# CBI44 capability contract — the launch takes its own trust decision

Date: 2026-08-04

Status: implementation contract

## Boundary

CBI43 carried one authorization from acquisition all the way to launch and recorded that nothing
verified the policy was still current when the artifact ran. CBI44 closes that window inside one
chain: before the store activates a staged set, the retained verified publisher evidence is evaluated
again against the policy the registry holds at that moment, and a publisher the current policy no
longer admits does not run.

The window is the one between acquisition returning a staged set and the executable being launched,
within a single chain call. Revalidation while a provider is already running, re-polling, endpoint or
authority key rotation, and a scheduling host are outside it. This is still a fake host-local manager
over a host-local store, and nothing here is a package format, a discovery service, or a deployment
tool.

It supersedes CBI43's statement that the chain adds no distribution capability: the launch decision is
a capability CBI43 did not have. It reclassifies nothing — the refusal a lapsed publisher produces is
still CBI35's, with CBI35 as its origin.

## Capabilities

### C1 — the launch decision is taken, not remembered

The chain evaluates the verified publisher evidence a second time, against the registry's current
policy, and the launch proceeds only on an authorization that evaluation issues. The result reports
the policy identity that authorized acquisition and the policy identity the launch decision was taken
against, so "two decisions" is an observation rather than a claim.

### C2 — a publisher the current policy no longer admits does not launch

A policy applied after acquisition that revokes the publisher key refuses with
`publisher-key-revoked`; one that no longer names the key refuses with `publisher-key-unknown`. Both
refuse before the store touches the artifact, so a trust refusal is never masked by an unrelated
integrity result.

Neither code is new, and neither says where it was decided: the same two codes are reachable at
acquisition. **The ladder is what distinguishes them**, which is CBI43's C2 doing the work it was
written for — a launch-time revocation and an acquisition-time one differ by four observations and
not by one character of the code.

### C3 — a policy that changed and still admits the publisher launches

The decision is compared, not the snapshot. An update that revokes some other publisher advances the
policy identity while leaving this publisher admitted, and the chain runs to Release with the two
reported policy identities different. Requiring the launch policy to be the acquisition policy would
refuse every benign update, which for a host that polls is most of them.

### C4 — a refused launch leaves nothing behind

No refused vector leaves a staged set in the store, a live provider process, a held removal lease, or
an advanced recovery floor. Removing the staged set is residue hygiene rather than a security act:
the bytes are content-addressed and re-acquirable, and their integrity was never what lapsed.

### C5 — the ladder gains a stage and stays a true-prefix

Policy applied, authorized, source opened, staged, revalidated, launched, and released are reported
for every vector and form a true-prefix. A stage runs only because every earlier stage succeeded.

### C6 — both roots run the same chain and agree

Reference C# and Minimal F# independently compose their own slices over the shared vectors and
independently report the ladder, the refusing code and its origin, both policy identities, the
registry's applied sequence, the retained floor, and the residue checks.

## Phase-wide properties

- The seven ladder observations of every vector form a true-prefix followed by false.
- Every vector that launched was admitted by the policy in force at launch, not merely by the one
  that authorized acquisition.
- Wherever a launch decision was taken, the policy identity it names is the registry's current one,
  and the content identity it names is the staged set's own.
- Every vector that did not launch leaves no staged set and no running process.
- The stored floor equals the number of updates the *poll* applied, and a policy applied mid-chain
  advances the registry's sequence without advancing it.

## Notes on what is deliberately not a refusal

**The launch decision's content identity cannot disagree with the staged set's.** The evidence is
over the acquisition request, the request's identity is what the store staged under, and CBI36
already refuses an authorization whose content identity does not match. A guard here would be a
declared category with no reachable path, which is the defect PB6 found three of, so the equality is
asserted as a property and given no refusal code.

**A policy cannot be absent at launch when it was present at acquisition.** The registry advances and
never clears, so the launch decision is taken against whatever the policy has become. The invariant
is pinned by the property above rather than by an unreachable `publisher-trust-policy-unavailable`
branch of its own.

**Nothing revalidates after Release.** A revocation arriving while the provider is serving is not
seen, and closing it needs a seam that observes a running member rather than a launching one.
