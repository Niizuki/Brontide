# CBI52 capability contract — provider restart enforcement

Date: 2026-08-05

Status: implementation contract

## Boundary

CBI51 decides whether a stopped provider may be restarted. CBI52 enforces one ready decision by
re-evaluating CBI51, re-verifying and launching the exact retained staged artifact set, and creating
a fresh portable member for the same resolved occurrence. The logical CM4 generation and restart
scope do not change: this repairs a provider connection inside that generation rather than replaying
the original generation cutover.

The stopped activation is opaque and retains the verified distribution and activation recipe. The
caller supplies policy observations and a typed cause, not paths, evidence, manifests, selections,
or lifecycle machinery.

## Capabilities

### C1 — eligibility is rechecked before effects

One call evaluates CBI51 against the stopped activation and exact current-cycle proof. Any result
other than `provider-restart-ready` is returned without provider, artifact, or lifecycle effects.

Property: every provider launch is preceded in the same call by one ready CBI51 decision.

### C2 — reconstruction uses the opaque retained recipe

The activation retains its verified staged artifact snapshot, resolution, selection, occurrence,
and prior logical runtime observation. CBI52 accepts none of those from its caller and cannot pair a
stopped provider with another member's recipe.

Property: every completed restart names the same typed occurrence and staged content identity as
the stopped activation.

### C3 — retained artifacts are verified again before launch

The content-addressed store re-verifies the complete retained set and launch arguments before
starting a new dedicated provider process and taking a new removal lease. Missing, mutated, or
unlaunchable content returns the existing CBI31 code and starts no portable lifecycle.

Property: no completed restart executes bytes that fail the retained content identity.

### C4 — restart repairs one logical generation

After provider launch, CBI52 prepares a fresh portable member from the retained resolution and
selection, interconnects it to the new provider, observes Ready, and releases it. It carries forward
the prior active CM4 runtime observation; it does not replay CM4 Release, invent a successor
generation, or widen the restart scope.

Property: every completed restart is serving under the original occurrence and logical runtime.

### C5 — incomplete reconstruction fails closed

If preparation, interconnection, Ready, or portable Release fails, the new provider is terminated
and its lease released. The stopped activation remains stopped and the result exposes the lifecycle
failure code.

Property: every failed reconstruction leaves no newly serving provider.

### C6 — one stopped activation has one successful successor

Restart enforcement is single-flight. Concurrent or repeated calls on an in-progress activation
return `provider-restart-in-progress`; calls after success return `provider-restart-already-completed`.
A failed launch or lifecycle reconstruction releases the claim so a later policy-approved attempt
may retry.

Property: one stopped activation can yield at most one successfully serving successor.

### C7 — refusal and failed launch preserve retained content

Policy refusal, claim refusal, artifact refusal, and lifecycle failure do not remove the retained
staged set. They do not mutate publisher policy or relabel availability as trust withdrawal.

Property: every non-completed vector retains the original staged content identity.

### C8 — both roots execute one shared enforcement model

Reference C# and Minimal F# independently execute shared vectors for completion, policy refusal,
artifact mutation, and repeated success, reporting code, origin, old/new serving state, occurrence
identity, and staged-content retention.

Property: every shared vector produces the same portable observation in both roots.

## Contract-completeness review

The contract states policy recheck, recipe ownership, complete artifact verification, same-generation
repair, lifecycle rollback, lease behavior, repeated and concurrent invocation, and retained-content
ownership. It deliberately does not persist attempt history, recover an enforcement interrupted by
host process loss, coordinate multiple host processes, replace a CM4 generation, select a different
provider artifact, or clean exhausted content. Those remain durable supervision, cross-process
ownership, generation replacement, distribution, and maintenance boundaries.

## Deliberate limits

CBI52 is one in-process call. It is not a daemon, durable restart journal, cross-process lock,
provider upgrade mechanism, endpoint/key rotation scheme, privileged floor anchor, or production
sandbox.
