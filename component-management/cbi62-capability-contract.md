# CBI62 capability contract — durable governed cadence resumption

Date: 2026-08-07

Status: implementation contract

## Boundary

CBI48 gives a CBI47 cadence a durable journal and a recovery protocol. CBI61 made a cadence cycle
govern two loops instead of one. CBI62 puts them together, and the item that scheduled it expected
the journal to record which of the two loops a resumed cycle had already run. It does not, and the
absence is the contract.

This is composition and one defect repair. It adds no capability to CBI47, CBI48, or CBI61, contacts
no peer, and is not a daemon, a cross-process lock, an offline policy, or a privileged custodian.

## Capabilities

### C1 — every code a cycle can return is committable

CBI61 added two cycle codes and CBI48 validates the four it knew about, so a governed cycle reporting
`provider-trust-cycle-authority-behind` or `provider-trust-cycle-authority-unretained` was refused as
`durable-cadence-result-invalid` and left the journal in-flight — an interrupted run that was never
interrupted. The vocabulary now lives in one place that both the cycles and the journal draw from, so
a code cannot be produced without the journal knowing it.

Property: every member of the shared vocabulary is committable, and the classification covers the
vocabulary exactly. This is the guard the defect asks for: it fails when the next code is added and
left out, rather than naming today's six.

### C2 — the journal names the run's outcome and never renames the cycle's

Both new codes end the run, so both commit `durable-cadence-stopped`. The cause stays in the
committed observation, which keeps CBI43's rule — a composition that renamed a refusal would delete
the diagnostic at the point a host reads it — holding one level up.

Property: for every vector, the committed observation's code is exactly the code the cycle returned.

### C3 — the journal records nothing about which loop ran, and the absent field is the contract

Recording it is worse than not recording it, for two reasons. A marker written after the rotation
returns is not atomic with the rotation's effect, so it creates a second indeterminate window instead
of closing the first. And the rotation's effect is already durably self-describing: the retained chain
names the authority generation and the stored floor names the guard, so a recovering host reads the
truth rather than a claim about it. A journal marker could only ever be a less trustworthy copy of a
record that already exists.

Property: the journal bytes of an interrupted governed cycle are identical whether the interruption
fell before or after the rotation, while the durable checkpoint differs. A test that observes both is
what makes this an absence rather than an omission.

### C4 — retrying an interrupted governed cycle is safe because both halves refuse a replay

CBI48 retries an interrupted index by re-running the cycle, which now re-runs both loops. Neither can
double-apply. CBI57 requires a rotation's generation to be exactly one past the active one, so a
rotation that already applied is refused as `policy-authority-generation-invalid`; CBI37 requires an
update's sequence to be exactly one past the current one, so an update that already applied is
refused as `policy-update-sequence-invalid`. Both refusals are the ordinary fail-closed ones, reached
without any knowledge of the interruption.

Property: retrying a cycle whose rotation had already applied leaves the authority generation exactly
where the first attempt left it, and retrying one whose update had already applied leaves the policy
sequence where it was.

### C5 — an ungoverned cadence is unchanged

A cadence composed before CBI61 leaves the rotation observation absent, commits the four codes it
always did, and reaches the same terminal codes, phases, gaps, and counts. CBI48's C1-C8 remain in
force verbatim.

Property: every CBI48 vector produces the same observation it did before this slice.

### C6 — both roots agree

Reference C# and Minimal F# independently consume the shared vectors and report the terminal code,
phase, ordered cycle codes, next index, interruption and retry counts, and the durable authority
generation and policy sequence the registry reaches.

Property: every shared vector yields an identical typed observation in both roots.

## Phase-wide properties

- No journal contains more observations than its declared budget, and CBI48's ordering, gap, and
  atomicity rules are unchanged.
- A committed observation is never replayed, including across a governed retry.
- A refused commit leaves the exact prior durable bytes.
- The registry's authority generation and policy sequence are non-decreasing across any sequence of
  interruptions and retries.

## Deliberate limits

CBI62 does not decide whether an indeterminate governed cycle is safe to retry — that is CBI49's
reconciliation decision, and what this slice supplies is the fact that the retry cannot double-apply
either half. It does not reconcile the sweep's effects, which remain the ones CBI48 says no local
journal can commit atomically with its own cursor. Cross-process ownership and privileged custody of
either floor remain separate.
