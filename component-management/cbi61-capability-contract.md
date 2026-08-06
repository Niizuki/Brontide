# CBI61 capability contract — governed trust cadence

Date: 2026-08-06

Status: implementation contract

## Boundary

CBI47 runs a bounded host cadence of policy poll then serving sweep. CBI60 runs a bounded cycle of
authority-rotation attempts. A host that does both has two loops over one registry, and CBI61 makes
them one cycle inside CBI47's unchanged cadence.

This is composition. It adds no capability to CBI41, CBI46, CBI47, or CBI60, contacts no new peer,
and reclassifies no refusal: every failure keeps the code the slice that produced it emitted, and
gains only its place in the cycle. It is not a daemon, a durable schedule, a cross-process owner, an
offline policy, or a privileged custody domain.

## Capabilities

### C1 — the rotation cycle runs first, and the order is forced rather than preferred

Within a cycle the CBI60 rotation cycle runs before the CBI41 poll. The order comes from the
registry rather than from a preference: a policy update is verified against the authority in force,
so an update signed by the authority a rotation installs is refused until that rotation is retained.
Polling first would refuse an update the host is entitled to apply and would report a policy failure
whose cause is an unlearned rotation.

Property: in a cycle where a rotation to a successor authority and an update signed by that successor
are both available, the cycle reaches current and the update is applied. Reversing the order fails
this vector, which is what makes the claim a test rather than a comment.

### C2 — a rotation that changed nothing does not stop the cadence

`policy-authority-cycle-refused` and `policy-authority-cycle-exhausted` leave the active authority,
the retained chain, the stored floor, and every serving member exactly as they were. Nothing
downstream depends on a rotation having been attempted successfully, so the cycle proceeds to the
poll and the sweep, and the rotation observation is carried beside them.

Property: every cycle whose rotation neither cancelled nor left an unretained floor also carries a
poll observation, whatever the rotation reported.

### C3 — a rotation that happened without its guard stops before the poll

`policy-authority-cycle-floor-unretained` is the one rotation outcome that changed something the host
cannot account for: the chain advanced past a floor the host does not hold, and every later advance
moves further past it. The cycle reports `provider-trust-cycle-authority-unretained` and runs no
poll and no sweep. This is CBI41's own reason for stopping on its own floor handoff, applied to the
other floor.

Property: after an unretained floor there is no poll observation, no sweep, and no later cycle.

### C4 — neither loop's failure is reported as the other's

Every cycle that ran both retains both observations whole. A rotation failure never appears as a poll
code and a poll failure never appears as a rotation code. A cycle may report `current` while carrying
a refused rotation, because a rotation that changed nothing is a fact to record rather than a fault to
propagate.

Property: in every cycle, the rotation observation's code is one CBI60 emits and the poll
observation's code is one CBI41 emits, and neither field ever holds the other's vocabulary.

### C5 — an authority mismatch is attributed only when a rotation was attempted and did not complete

`policy-update-authority-mismatch` is CBI41's fail-closed refusal of an update signed by an authority
the registry does not hold. Before CBI57 that could only be a stranger, and CBI41's own vector calls
it `foreign-authority`. After CBI57 the same observable also describes a legitimate publisher the host
has not rotated to yet, and only the composition can tell them apart, because only it knows whether a
rotation was attempted in the same cycle and what it reported.

The cycle reports `provider-trust-cycle-authority-behind` exactly when the poll refused with
`policy-update-authority-mismatch` **and** this cycle's rotation did not reach current. That is a
mechanical conjunction of two recorded facts, not a judgement about the update. When the rotation did
reach current the same poll code is reported as an ordinary stop, because a host that is up to date
and still cannot verify an update is being sent one it should refuse.

Property: the attributed code appears exactly when both conditions hold, and the underlying poll and
rotation codes are unchanged in both cases.

### C6 — cancellation keeps CBI47's boundary

Cancellation observed by the rotation cycle cancels the cycle with no poll and no sweep. Cancellation
observed by the poll keeps CBI47's existing meaning. A cancelled cycle is the last recorded cycle and
rolls back no rotation, policy update, floor retention, withdrawal, or cleanup already observed.

Property: a cancelled cycle carries no sweep, and no cycle follows it.

### C7 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the cadence code, the
ordered cycle codes, each cycle's rotation and poll codes, whether the poll ran, and the durable
authority generation and policy sequence the registry reaches.

Property: every shared vector yields an identical typed observation in both roots.

## Phase-wide properties

- No cadence contains more cycles than the schedule budget, and CBI47's gap accounting is unchanged.
- A stopped or cancelled cycle is always the final recorded cycle.
- No sweep is reached in a cycle whose poll did not report current, which is CBI47's C3 unchanged.
- No cycle reaches a poll whose rotation cancelled or left an unretained floor.
- The registry's authority generation is non-decreasing across a cadence.

## Deliberate limits

CBI61 owns the order of two loops within one bounded call. It does not decide when the host makes
that call, resume it after a restart, coordinate two processes, apply an offline grace, or retire
anything: a cadence that cannot reach either endpoint leaves every serving member exactly as it was.
Privileged custody of either floor remains the named deployment boundary, with CBI42's limit
unchanged.
