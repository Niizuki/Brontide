# CBI45 capability contract — serving trust revalidation

Date: 2026-08-04

Status: implementation contract

## Boundary

CBI44 takes a second publisher-trust decision immediately before launch, but a policy change after
Release is invisible to the serving member. CBI45 adds one explicit, host-driven revalidation call
over one CBI44-launched provider and its one released portable member. It evaluates the verified
publisher evidence retained by the chain against the registry's current policy.

This slice is one observation, not a scheduler. It does not poll, retry, watch files, rotate keys,
fan out over members, or define when a host should invoke it. It remains experimental host-local
evidence over the fake distribution chain and portable composition.

## Capabilities

### C1 — the serving decision is current

The call takes a new trust decision against the policy in force when it runs. It reports both the
launch policy identity and the serving policy identity, and never spends the launch authorization.

### C2 — lapsed trust stops service

If the current policy revokes the publisher, the result keeps `publisher-key-revoked` with `cbi35`
as its origin. If the current policy no longer names the publisher, it keeps
`publisher-key-unknown` with the same origin. In either case the released member is retired before
the provider process is terminated; the process lease is released and staged-set removal is
attempted. A cleanup failure is reported separately and never changes the trust refusal into trust.

### C3 — a changed policy that still admits the publisher preserves service

An unrelated policy change may change the policy identity while leaving the publisher admitted.
The member and provider continue serving. The decision is compared, not the snapshot.

### C4 — retained evidence, not a caller claim, is evaluated

The distribution result retains the exact verified evidence produced inside CBI44's chain. CBI45
accepts no replacement evidence or publisher identity from its caller. Its opaque serving activation
also binds the lifecycle to the launched provider conversation, so a caller cannot pair one chain's
publisher with another chain's member. The serving decision's content identity equals the launched
staged identity.

### C5 — an unavailable serving activation has no revalidation effect

Only the opaque activation issued while binding one launched, revalidated CBI44 provider to one
released Active portable lifecycle is accepted. An unavailable activation is refused as
`serving-activation-unavailable` before policy evaluation or cleanup. Repeating a withdrawal
therefore reports unavailable rather than manufacturing a second retirement.

### C6 — both roots agree

Reference C# and Minimal F# independently execute the shared vectors and report the decision code
and origin, whether revalidation ran, whether service continued, both policy identities, and member,
process, and staged-set residue.

## Phase-wide properties

- Every continued result names the registry's current policy and an admitting decision for the
  retained verified publisher evidence.
- Every trust refusal terminates the concrete provider and releases its lease. Successful graceful
  cleanup also leaves no released member or staged set; cleanup failure remains explicit.
- No unavailable result evaluates policy, retires a member, terminates a process, or removes bytes.
- Wherever a serving decision is taken, its content identity is the launched staged identity.
- A policy-identity change alone neither continues nor withdraws service; the current decision does.

## Deliberate limits

This call does not decide cadence, race semantics between concurrent policy writers, or what a host
does when portable retirement itself fails. The concrete provider is still terminated on a trust
refusal so ordinary interaction fails closed; the result reports retirement failure separately if
that cleanup cannot complete. Multi-member cutover and restart policy remain outside the slice.
