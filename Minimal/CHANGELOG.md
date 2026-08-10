# Changelog

## Unreleased - Architecture 0.8 A08-D4 experimental runtime

### Added

- Minimal-owned typed liveness-lease identity, Genesis issuance, and exact ancestor evaluation at
  the Draft-0.8 presentation instant.
- An immutable Base execution-rate accounting transition keyed by carrying Capability plus exact
  expression/atom occurrence and time window, shared by sibling Delegations.
- Minimal-native execution of all six A08-D4 C1/C5 vectors and their phase properties.

### Changed

- Successful authorization returns a `World` with prepared occurrence usage committed; every denial
  returns the original accounting state.
- Vocabulary-defined accounting scopes remain identifiable but are reported declined and deny
  before an ordinary evaluator when their scope cannot be enforced.

## Unreleased - Architecture 0.8 A08-D3 breaking experimental runtime

### Changed

- `ConstraintRequirement` now carries its presented `ParameterShape`, so authority values can be
  checked against the declaration's exact Shape/version without projection.
- `Draft08ExecutionRequest` now carries `PresentedCommandShape`; the complete payload is validated
  before a later additive record is projected to the Operation's accepted version.
- Constraint definitions now own first-class declarations and reject semantic or value-Shape drift
  under an existing canonical name.

### Added

- `World.registerConstraintDeclaration` and deterministic implemented/declined recognition-set
  evidence, including standard Constraints.
- Minimal-native execution of all six A08-D3 C9/C8 vectors and their phase properties.

### Breaking change

- Add `ParameterShape` to Constraint requirements and `PresentedCommandShape` to Draft-0.8 requests;
  later additive Constraint values now deny instead of being inferred or projected.

## Unreleased - Architecture 0.8 A08-D2 breaking experimental runtime

### Changed

- Removed the separate `Capability.DelegationAllowed` field and Boolean arguments from
  `Genesis.capability` and `Genesis.capabilityWithExpressions`; Capabilities are delegable by
  default and restriction uses the standard delegation-depth Constraint.
- Every derived Capability now carries an implicit standard `Origin.Derived` ceiling requirement,
  evaluated natively against its carrying link and complete resolved chain.
- `World.stepDraft08` now accepts a typed `Draft08ExecutionRequest`; successful Events record the
  exercised origin while the ordinary `World.step` path remains unverified-origin.

### Added

- Minimal-native execution of all four A08-D2 C6/C2 vectors plus the phase-wide zero-effect and
  removed-surface property.
- Standard Minimal definitions for delegation depth, origin grants, and origin ceilings.

### Breaking change

- Remove the Boolean issuance argument and replace former `false` values with a depth-zero standard
  Constraint requirement. Wrap Draft-0.8 requests with an explicit typed requested origin.

## Unreleased - Architecture 0.8 A08-D1 experimental runtime

### Added

- Minimal-native structural strong-Kleene evaluation and explicit `World.stepDraft08`, with
  True-only authority, pre-effect denial, instantaneous authorization, and resolved full-chain checks.
- Draft-0.8 Definition selection assessments that retain normalized Unknown atom names for both
  eligible and rejected candidates.
- Minimal-native execution of all 11 A08-D1 C7/C3/C4 vectors.

### Notes

- Existing Architecture 0.7 evaluator, `World.step`, selection behavior, and poisoning tests are
  retained. Minimal remains designed for Architecture 0.7; this is experimental 0.8 evidence.

## Unreleased - Architecture 0.8 delivery audit

### Added

- A Minimal-owned Architecture 0.8 C1-C14 audit matrix that distinguishes reusable candidates,
  partial candidates, conflicts, missing behavior, handoff attestations, and architecture-only scope.
- A shared audit capability contract, completeness review, ordered six-slice runtime queue, and
  mechanical gate covering all 33 canonical vectors and both documentation-only changes.

### Notes

- No runtime vector is accepted and Minimal remains designed for Architecture 0.7. A08-D1 is a
  proposed next slice, not an implementation claim.

## Unreleased - Architecture 0.8 M6 handoff planning

### Added

- A shared C1-C14 requirements/risk ledger accounting for all 33 Architecture 0.8 vectors and both
  documentation-only coverage changes without pre-implementing the draft.
- Minimal implementation notes recording the World-resolved parent-reference representation and
  its current no-post-issuance-revocation semantics.
- A mechanical handoff gate integrated into the repository completion gate.

### Notes

- Minimal remains designed for Architecture 0.7; this is planning evidence only.

## Unreleased - Architecture 0.7 M5 cross-stack comparison

### Added

- A Minimal-native F# process endpoint for the common 15-vector M1-M4 data fixture, implemented
  only with Minimal public surfaces.
- Paired comparison evidence for canonical results, denials, provenance, restoration, diagnostics,
  Dataset identity, Router guarantees, fallback, and redaction.

### Notes

- No disagreements are allowed or observed; the evidence remains finite and experimental.

## Unreleased - Architecture 0.7 M4 persistent-information experiment

### Added

- `Brontide.Minimal.Experimental.PersistentInformation`, a native F# experimental component with
  opaque Corpus/Dataset/Store-role/Store/Router identities, immutable records, result-valued
  refusals, declaration-checked single-writer operations, and Router-owned endpoint guarantees.
- `Brontide.Minimal.PersistentInformation.Tests`, with C1-C8 evidence for Kernel denial before
  effects, Store-independent Dataset identity, concurrency refusal, fallback, guarantee leakage
  prevention, and topology redaction.

### Notes

- The endpoints are in-memory evidence, not durable-media storage, transactions, or a general Router.
- The retained 0.7 matrix remains pinned as `planned` for M4 until review is retargeted.

## Unreleased - CBI69 cadence run supervision

### Added

- `ProviderTrustCadenceRunSupervision`, a live operating-system exclusion over one cadence run. It
  holds a lock beside the journal for its lifetime; a second acquisition in any process answers
  `cadence-supervision-busy` and changes nothing.
- `SupervisedProviderTrustCadenceRecovery.advance`, which advances a cadence only while its supervision
  is live and otherwise answers `cadence-supervision-required` without running the cycle. The exclusion
  is held across the whole advance, including the cycle, because that is the window the fence cannot
  cover.
- `DurableProviderTrustCadenceJournal.RecordPath`, the holder's own resolved path, so a supervision can
  answer whether it covers the journal it is handed. A supervision paired with another path or another
  run refuses.
- Shared cross-stack vectors with C1-C7 tests, including a real second process proving the exclusion.

### Notes

- CBI68's fence detects a lost run at the holder's next write, and a cadence writes after its cycle has
  run. A competitor that opens mid-cycle and reconciles the in-flight attempt therefore takes the run
  while the effects are still happening: the cycle runs, the commit is refused, and the record keeps
  nothing of it. A named test runs that scenario and the same one under a lock.
- CBI68's residual limits describe two interleaving holders as fencing each other alternately. They do
  not: a refused transition leaves the refused holder's epoch unchanged, so the loser is out
  permanently and only reopening rejoins. Pinned by a named test.
- Supervision claims nothing. Acquiring reads and writes no part of the record, so a run may be
  supervised before it is established and CBI68's rule that ownership is claimed by writing is
  untouched.
- No durable record is added. CBI54 publishes an epoch beside its lock because CBI53 has none; this
  journal already carries one, and a second record of a fact the first holds is a thing that can
  disagree with it.
- Supervision is opt-in and coordinates cooperating hosts, which is CBI54's limit unchanged. A host
  that opens the journal without acquiring is caught by the fence at its next write rather than
  excluded. Nothing expires, nothing is renewed, and the lock file is left behind on release because
  deleting it would race a supervisor that has already opened it.

## Unreleased - CBI68 cadence run ownership

### Fixed

- Two holders of one cadence journal each kept their own copy of the state and each wrote the whole
  record back, so a holder whose copy was behind erased a cycle another had committed, with nothing
  reporting it; a holder superseded while its own copy was current wrote over the phase of a run it no
  longer owned. Pinned by a failing test before the fix.

### Added

- An owner epoch in the journal record. Every write advances it, and a transition by a holder the
  record has moved past is refused as `durable-cadence-owner-superseded` without writing.
- `DurableProviderTrustCadenceJournal.OwnerEpoch`, the epoch a holder last saw or wrote, and shared
  cross-stack vectors with C1-C7 tests.

### Notes

- Ownership is claimed by writing rather than by opening. Claiming at open is the first design that
  comes to mind and three existing CBI48 tests refuse it: one opens a journal from inside a running
  cycle purely to observe it, and two compare the durable bytes across a recovery.
- That correction also removes the migration rather than easing it. A record written before this slice
  carries no epoch, reads as zero, and is claimed at one by its first write, so no adoption rule is
  needed and no format marker moves.
- The guard runs before each transition's phase preconditions, because those are judged from state a
  superseded holder already knows to be stale, and reporting one would name a protocol error the holder
  did not make. An unreadable record keeps the outcome CBI48 already defines.
- This is a fence, not a lock: it makes a written-past holder harmless and does not stop a second host
  from opening a run the first is driving.

## Unreleased - CBI67 durable stop attribution

### Added

- `DurableProviderStopAttributionStore`, a host-local record of why the host stopped each occurrence's
  provider, integrity-tagged as CBI42's floor store is and with the same limit.
- `ProviderStopAttribution`, an opaque issuer-controlled value with no public construction path.
- Writers on every path in the host that stops a provider — CBI50's availability enforcement, CBI46's
  trust sweep, and an explicit operator retirement — each recording after the effect is complete.
- Shared cross-stack vectors and C1-C8 tests.

### Changed

- `ProviderRestartPolicy.Evaluate` takes a `ProviderStopAttribution` in place of a
  `ProviderRestartCause`, and the value threads through CBI52, CBI53, and CBI55 unchanged in shape.
  Its refusals are unchanged; what changed is that the caller can no longer choose which applies.
  A caller that constructed a cause now obtains one from the store instead.

### Notes

- Two of the three wrong claims were already caught by something else, and only checking showed which:
  a withdrawn publisher fails CBI51's own authorization check whatever cause is claimed, and an
  unexpected exit is the restartable case anyway. Operator retirement is the one that is neither, and
  it is the whole of what the record buys.
- A record is written after the stop, never before, because a record is a statement about something
  that happened. The opposite order claims a stop that did not occur and would have CBI52 launch a
  second provider for an occurrence still serving.
- A stop the host did not perform cannot be attributed: an operator who kills a provider from outside
  the host leaves no record and an exited process, which is indistinguishable from an unexpected exit.

## Unreleased - CBI66 retry-aware cadence gaps

### Fixed

- `CompleteGap` validated the instant it was given and then recorded the schedule interval regardless,
  so a gap that was not the interval was recorded as one that was, leaving the recorded gaps
  disagreeing with the recorded cycle instants in the same journal. It records the gap that elapsed.
  Pinned by a failing test before the fix.

### Added

- The cadence shortens its next gap to CBI49's retry instant when that is sooner than the schedule
  interval, so a run lands on the availability deadline rather than at the first scheduled cycle after
  it. Shared cross-stack vectors and C1-C7 tests.

### Changed

- The journal accepts a positive gap no greater than the interval instead of requiring equality. This
  is a relaxation: a journal written before this slice has gaps all equal to its interval and stays
  valid, and no format marker moves.

### Notes

- The bound is one-sided on purpose. A retry instant may bring a cadence's next look forward and never
  push it back, because the interval is the host's own schedule.
- A cadence cannot detect an outage before it looks, so the first outage cycle still falls on the
  ordinary interval and an interval longer than grace can still pass the deadline before any outage is
  seen. A vector states that outcome rather than leaving it to be read as a guarantee.

## Unreleased - CBI65 durable availability baseline

### Added

- `ProviderTrustCadenceAvailabilityRecovery`, a native F# derivation of CBI64's availability baseline
  from the observations CBI48 already committed. It reads a snapshot rather than a journal, so it has
  nothing to write to.
- `ProviderServingTrustCycleCodes.establishes`, the vocabulary's answer to whether a cycle reporting a
  code established current policy. It is `None` for a code the vocabulary cannot answer, which a
  consumer must refuse rather than resolve.
- `ProviderAvailabilityTrustCycle.resume`, which starts a cycle from a derived baseline; `create` is
  now that function with no baseline. Shared cross-stack vectors cover the derivation and the
  classification of every cycle code, with C1-C8 tests.

### Notes

- No new durable record. The journal has held each cycle's instant and code since CBI48, and a second
  record of a fact the first already holds is a thing that can disagree with it.
- `provider-trust-cycle-stopped` is unanswerable — the cycle produces it both for a poll that was not
  current and for a current poll whose sweep failed — so it is refused, and the refusal outranks any
  establishing observation behind it. CBI48 cannot place one in front of a later cycle, which is
  probed rather than assumed.
- A terminal journal is a source rather than something to reject: CBI49 anchors the deadline in
  absolute time, so an old baseline is already expired. No freshness guard is added here, because a
  baseline later than the evaluating instant is already `offline-observation-invalid`.

## Unreleased - CBI64 cadence availability enforcement

### Added

- `ProviderAvailabilityTrustCycle`, a native F# cycle that applies CBI49's availability policy to a
  cadence cycle that could not establish current policy and CBI50's enforcement to whatever that
  decides. It closes over the instant of the most recent cycle whose poll was current and never lets
  an outage refresh it.
- `ProviderAvailabilityTrustCycle.enforcement`, binding one offline policy and the host's serving-set
  snapshot to that seam, and `ProviderTrustCycleAvailability`, the cycle's projection of a CBI50
  result.
- `provider-trust-cycle-offline`, a continuing cycle code for a cadence inside grace, added to the one
  vocabulary CBI48's journal validates against.
- Shared cross-stack vectors over a scripted policy endpoint and real serving providers, and C1-C8
  tests.

### Changed

- `ProviderServingTrustCycleResult` gains an `Availability` field. Adding a field to an F# record is a
  breaking change for any external construction site; the migration is `Availability = None`, which is
  every cycle composed before this slice.

### Notes

- CBI63 named this boundary as terminating providers when grace expires; CBI50 had done that since
  2026-08-05, and the real gap was that nothing polling repeatedly had ever called CBI49 or CBI50 at
  all. An outage previously ended the cadence with every provider still serving, which is neither
  answer the policy offers.
- Every non-current poll that made a poll reaches a decision, not only the grace-eligible outcomes,
  because routing one third would leave the other two unreachable from a cadence. Cancellation and a
  cycle its rotation stopped before the endpoint enforce nothing.
- The cycle code still names why current policy could not be established, so CBI61's
  `provider-trust-cycle-authority-behind` attribution survives a cycle that stopped every member.

## Unreleased - CBI63 governed interruption reconciliation

### Added

- A durable cursor recorded by the write that already marks a governed attempt in-flight, naming the
  authority generation, active authority, policy sequence, and policy identity the attempt was about
  to act on.
- A native F# governed reconciliation that derives the rotation and policy observations from that cursor and the
  registry, and accepts only a serving verdict from the host.
- A governed recovery advance that supplies the cursor, shared cross-stack vectors, and C1-C7 tests.

### Changed

- CBI49's ungoverned reconciliation refuses an in-flight journal that recorded a cursor, as
  `cadence-reconciliation-governed`. Its single verdict would speak for effects the host need not have
  inspected. Ungoverned runs are unaffected.
- `ProviderTrustCadenceJournalSnapshot` gains a `Cursor` field. The journal is its only producer, so
  no in-repo consumer constructs one; an external consumer that did would need to add
  `Cursor = None`. `BeginCycle` gains a cursor-taking overload and keeps its existing no-argument
  form.

### Notes

- The evidence is narrower than CBI49's rather than wider. Two of the three things a governed cycle
  can do record themselves durably, so there is no field a host could over-assert into; the sweep
  remains the one thing nothing here can check. A journal written before this slice has no cursor and
  is refused rather than derived against an invented baseline. A derived effect is reported and never
  vetoes retry, because CBI62 established that a retried governed cycle cannot double-apply.

## Unreleased - CBI62 durable governed cadence resumption

### Fixed

- CBI48's journal refused the two cycle codes CBI61 added, reporting `durable-cadence-result-invalid`
  and leaving the run in-flight, so a governed cadence that completed normally was recorded as an
  interruption that never happened. Cycle codes now live in one vocabulary that the cycles produce
  from and the journal validates against, and a named test walks the vocabulary rather than listing
  today's codes.

### Added

- `ProviderServingTrustCycleCodes`, the single native F# vocabulary of cycle codes with their
  continue/stop classification.
- Shared cross-stack resumption vectors covering commit, interruption, retry, and abandonment of a
  governed run, and C1-C6 tests including the two that pin the absence of a loop marker and the
  safety of a governed retry.

### Notes

- The journal records nothing about which of the two loops a resumed cycle had run. A marker written
  after the rotation returns is not atomic with its effect, and the rotation's effect is already
  durably recorded in the retained chain, so a marker could only be a less trustworthy copy. A
  retried governed cycle cannot double-apply either half: CBI57 refuses a replayed rotation by
  generation and CBI37 refuses a replayed update by sequence.

## Unreleased - CBI61 governed trust cadence

### Added

- A native F# governed cycle that runs one CBI60 rotation cycle before CBI47's poll and sweep, inside
  CBI47's unchanged cadence, with both observations retained whole.
- `provider-trust-cycle-authority-behind`, reported exactly when a poll refused with
  `policy-update-authority-mismatch` and the same cycle's rotation did not reach current, and
  `provider-trust-cycle-authority-unretained`, which stops before the policy endpoint.
- Shared cross-stack cadence vectors and C1-C7 tests, including the vector pair that differs only in
  what the rotation reported.

### Changed

- **Breaking.** `ProviderServingTrustCycleResult` gains a `Rotation: ProviderPolicyAuthorityCycleResult
  option` field and its `Poll` becomes `ProviderPublisherTrustPolicyPollResult option`, because a
  governed cycle can stop before the policy endpoint is contacted. Construction expressions must add
  `Rotation = None` and wrap an existing poll in `Some`; an ungoverned cadence leaves `Rotation`
  absent, which is every cadence composed before CBI61.

### Notes

- CBI61 adds no capability to CBI41, CBI46, CBI47, or CBI60 and reclassifies no refusal. CBI41's
  behaviour is unchanged: failing closed on an update it cannot verify is correct whether the cause
  is a stranger or a rotation the host has not learned, and only a cycle that ran both loops can tell
  those apart. Offline policy, durable resumption, and cross-process ownership remain separate.

## Unreleased - CBI60 durable policy-authority rotation cycle

### Added

- A native F# bounded, host-driven cycle of CBI58 rotation attempts with a validated schedule,
  jitter-free backoff over consecutive failures, and retry confined to transport failure, timeout, a
  stale window, and a superseded cursor.
- Durable custody of the CBI38 authority floor: an integrity-tagged host-local store bound to the
  authority pin that advances only by a handoff from a publication this host performed, and a custody
  function that opens the checkpoint under both that guard and CBI42's.
- Shared cross-stack cycle vectors and C1-C7 tests, including the truncation case only the authority
  floor detects and the one neither guard detects.

### Notes

- CBI60 adds no capability to CBI57, CBI58, or CBI59 and reclassifies no refusal. It is one call the
  host makes, not a daemon or a schedule that survives the process. An authority guard absent beneath
  an existing checkpoint is adopted at zero and reported as `policy-authority-floor-adopted`, because
  CBI42's establish-before-the-checkpoint ordering is not available to a guard introduced later.
  Privileged floor custody and cross-process ownership remain separate boundaries.

## Unreleased - CBI59 policy-authority rotation wire

### Added

- An independent native F# strict big-endian, strict-UTF-8 codec for the complete CBI58 cursor and
  zero or one CBI57 authority-rotation statement, with shared exact cross-stack golden encodings.
- A single-attempt HTTPS source that requires the exact endpoint and media type, bounds declared and
  streamed responses to 1 MiB, and propagates cancellation without retry.

### Notes

- CBI59 leaves the established CBI39/CBI40 wire unchanged. HTTP handler policy, scheduling, retry,
  pin rotation, and privileged floor custody remain separate boundaries.

## Unreleased - CBI58 policy-authority rotation distribution

### Added

- A native F# endpoint-authenticated single-attempt client for delivering zero or one CBI57
  authority-rotation statement against the exact durable policy and authority cursor.
- A canonical signed response manifest, injected source seam, bounded failure results, shared
  cross-stack vectors, and C1-C6 tests that reopen the resulting checkpoint.

### Notes

- CBI58 is separate from the unchanged CBI39/CBI40 policy-distribution records and wire. Portable
  framing, HTTPS transport, scheduling, pin rotation, and privileged floor custody remain separate.

## Unreleased - CBI57 policy-authority key rotation

### Added

- A native F# authority rotation inside the retained CBI38 chain: `ProviderPolicyAuthorityRotationStatement`,
  its canonical manifest, `Rotate` on both the live and durable registries, and
  `ActiveAuthorityIdentity`/`AuthorityGeneration`, with recovery replaying updates against the
  authority in force at each position.
- `ProviderPolicyAuthorityFloor` and a four-argument `DurableProviderPublisherTrustPolicyRegistry.Open`
  overload returning it beside the recovered registry.
- Independent C1-C8 tests and shared rotation vectors with the Reference stack.

### Changed

- The checkpoint record keeps the CBI38 shape until a rotation is retained and then advances to a
  tagged CBI57 record. Checkpoints written before this change open unchanged.
- The three-argument `Open` overload is unchanged and now delegates to the four-argument one.

### Notes

- CBI57 rotates the key that signs publisher-trust policy. The out-of-band pin never moves, and
  remediation of a compromised predecessor, rotation transport, and privileged floor custody remain
  separate work.

## Unreleased - CBI56 distribution-endpoint key rotation

### Added

- A native F# durable active CBI39 endpoint anchor with current-key-signed successor staging,
  successor confirmation, integrity checks, and external rollback-floor support.
- Independent C1-C8 tests and shared refusal/staging vectors with the Reference stack.

### Notes

- CBI56 rotates the CBI39 response-authentication key only. Policy-authority rotation, endpoint URI
  discovery, TLS policy, failover, and privileged floor custody remain separate work.

## Unreleased - CBI55 external restart-effect reconciliation

### Added

- A durable exact-attempt effect record and provider-held operating-system lease with an atomic,
  bounded process receipt.
- Successor-fenced reconciliation that proves no provider remains or terminates the exact matching
  orphan before selecting CBI53 retry, with shared C1-C8 and real child-process evidence.

### Notes

- CBI55 is cooperative host-local cleanup, not provider adoption, distributed ownership,
  exactly-once execution, hostile-process attestation, or proof about detached external effects.

## Unreleased - CBI54 cross-process provider restart ownership

### Added

- Host-local operating-system lock ownership for one durable CBI53 restart lineage, with distinct
  typed owner and lease identities and a monotone integrity-checked fencing epoch.
- Fail-closed inspection, stale-lease rejection, process-loss recovery, shared C1-C8 vectors, and
  real child-process exclusion evidence.

### Notes

- CBI54 coordinates cooperating processes on one host and shared filesystem. It does not provide a
  distributed lease or reconcile whether an interrupted external provider effect occurred.

## Unreleased - CBI51 provider restart policy

### Added

- One bounded, effect-free restart policy requiring an exact current-cycle policy identity and a
  fresh publisher authorization for the stopped activation's retained content.
- Typed stop-cause eligibility, deterministic attempt delay and exhaustion, fail-closed observation
  validation, and shared C1-C8 vectors executed independently by both stacks.

### Notes

- CBI51 decides eligibility but launches nothing. Provider and lifecycle reconstruction remains the
  next host enforcement boundary.

## Unreleased - CBI50 offline service enforcement

### Added

- One bounded host coordinator that evaluates CBI49 against the exact supplied serving snapshot and
  enforces every stop decision in deterministic typed-occurrence order.
- Complete per-member retirement and provider-stop observations, zero-effect preflight refusal, and
  shared C1-C8 vectors executed independently by both stacks.

### Notes

- Offline availability enforcement retains staged artifacts and never authorizes restart. Provider
  restart selection remains separate host work.

## Unreleased - CBI49 provider trust offline and reconciliation policy

### Added

- One explicit host availability policy with bounded offline grace and retry intervals, derived from
  the last cycle that established current policy and restricted to exhausted transport failure or
  timeout.
- Existing-service-only offline decisions that never authorize acquisition, launch, admission, or
  restart, plus exact retry deadlines that cannot slide across repeated failures.
- Reconciliation of a CBI48 interruption through exact run/index/instant evidence: confirmed
  no-effect selects retry, accounted effects select abandonment, and unknown evidence stays inert.
- Shared offline and reconciliation vectors with named C1-C8 tests in both roots.

### Notes

- CBI49 reports when existing service must stop but does not terminate it. It neither manufactures
  reconciliation evidence nor decides provider restart.

## Unreleased - CBI48 durable provider trust cadence resumption

### Added

- One integrity-checked, atomically replaced host-local journal for a distinctly identified bounded
  cadence run, with ordered cycle, gap, interruption, and retry observations.
- Record-before-effect recovery: committed cycles resume from their next clean boundary, while an
  interrupted cycle opens as indeterminate until the host explicitly chooses retry or abandonment.
- Shared six-vector recovery evidence and named C1-C8 tests in both roots.

### Notes

- The journal does not claim exactly-once execution, choose whether replay is safe, coordinate
  multiple owners, decide offline or provider restart policy, or provide adversary-resistant custody.

## Unreleased - CBI47 provider trust cadence

### Added

- One bounded host-owned cadence that immediately polls current publisher policy, snapshots and
  sweeps the current serving set, then repeats through an injected delay for at most 64 cycles.
- Explicit complete, stopped, and canceled observations, with successful trust withdrawal allowed
  to continue and shared six-vector evidence executed independently in both roots.

### Notes

- The cadence is deterministic in-process orchestration, not a daemon, durable retry queue,
  crash-resumption protocol, offline policy, or restart controller.

## Unreleased - CBI46 serving trust sweep

### Added

- One explicit host-owned sweep over 1-64 opaque serving activations, ordered by typed occurrence
  identity with whole-set preflight and one CBI45 observation per admitted member.
- Aggregate current, withdrawn, cleanup-incomplete, and post-preflight-incomplete outcomes plus shared
  four-vector evidence in both roots.

### Changed

- Serving activations expose the occurrence identity bound during activation. Sweep cleanup retains a
  staged artifact while any swept activation using the same content identity continues.

## Unreleased - CBI45 serving trust revalidation

### Added

- One explicit current-policy decision for a provider and portable member already serving, bound in
  an opaque activation so a caller cannot pair one provider's publisher evidence with another member.
- Trust withdrawal preserves CBI35's refusal, retires the member, terminates the provider, releases
  its store lease, and reports graceful-retirement or removal failure separately.
- Shared four-vector evidence for unchanged policy, unrelated change, revocation, and removal.

### Changed

- **Breaking:** `ProviderDistributionChainResult` gains `PolicyAuthorityIdentity` and
  `VerifiedEvidence`. Callers constructing the F# record literally must supply both fields; callers
  that only read results are unaffected, and `ProviderDistributionChain.run` fills them itself.

## Unreleased - CBI44 launch-time trust revalidation

### Added

- The distribution chain takes a second trust decision before the store activates a staged set,
  evaluating the verified publisher evidence against the policy the registry holds at that moment,
  so a publisher revoked or dropped between acquisition and launch does not run.
- Shared vectors covering the complete run, both launch-time lapses, an unrelated policy update, an
  acquisition-time refusal with the same code, and a post-decision launch refusal, with the ladder
  extended to seven observations and still required to be a true-prefix.

### Changed

- **Breaking:** `ProviderDistributionChainResult` gains `Revalidated`, `AcquisitionPolicyIdentity`,
  and `LaunchPolicyIdentity`. F# record construction is positional and exhaustive, so any caller
  building the record literally must supply the three new fields; callers that only read the result
  are unaffected. `ProviderDistributionChain.run` fills them itself.

### Notes

- The decision is compared, not the snapshot: a policy that changed and still admits the publisher
  launches, because refusing on a moved policy identity would refuse every benign update.
- The refusal codes stay CBI35's. Only the ladder says whether a revocation was seen at acquisition
  or at launch.
- Unlike CBI43's acquisition trust step, this one is a barrier: removing it launches a revoked
  publisher's executable.
- Nothing revalidates after Release.

## Unreleased - CBI43 end-to-end distribution chain

### Added

- A composition that runs publisher evidence, host trust policy, governed acquisition,
  content-addressed staging, and provider launch as one path, preserving each slice's own refusal
  code and recording which slice produced it.
- Shared vectors covering one complete run and one refusal per stage, with the ladder required to be
  a true-prefix and residue checks for staged sets, live processes, and the retained floor.

### Notes

- The chain's trust step preserves attribution rather than adding a barrier: the governed acquirer
  already refuses a missing authorization, but without this step the reason is lost.
- Nothing revalidates the trust policy between acquisition and launch.

## Unreleased - CBI42 durable recovery-floor custody

### Added

- A durable host-local recovery-floor store with a canonical record, a SHA-256 integrity tag, atomic
  publication, and monotone idempotent retention, exposing a sink the CBI41 cycle consumes directly.
- A custody composition that establishes the store before any checkpoint exists, refuses a checkpoint
  whose store is absent or unreadable, and opens the durable registry under the stored floor.
- Shared vectors, a golden record image, and named C1-C7 encoding, establishment, refusal, ordering,
  retention, end-to-end, and cross-stack evidence.

### Notes

- The floor is advanced only by a handoff, never by a recovered checkpoint, so a chain cannot raise
  the guard that would refuse it.
- The integrity tag detects corruption and truncation. It is not a defence against an adversary who
  can write the store, and custody in a separate privilege domain remains future work.

## Unreleased - CBI41 host-owned policy poll scheduler

### Added

- A bounded poll cycle over CBI39 that advances until the endpoint reports the host current, retries
  only transport, timeout, stale-window, and superseded-cursor outcomes, and ends at the attempt that
  produced any endpoint-authentication or registry refusal.
- A deterministic capped exponential backoff computed from consecutive failures, so progress resets
  it, with the elapsed-time seam supplied as an injected function rather than read from an ambient
  clock.
- A recovery-floor sink offered each newly published floor after its checkpoint is durable, and an
  explicit advanced-but-unretained outcome when the sink refuses.
- Shared vectors, a shared schedule, and named C1-C7 cycle, backoff, termination, ordering,
  handoff, cancellation, and cross-stack evidence.

## Unreleased - CBI40 portable policy-distribution wire

### Added

- A strict versioned big-endian request/response codec preserving the complete CBI39 envelope and
  optional CBI37 update under exact UTF-8, count, size, and EOF rules.
- A concrete single-POST HTTPS source with exact endpoint, status, media type, no-content-encoding,
  cancellation, and independent declared/streamed 1 MiB bounds.
- Shared vectors, golden wire digests, and named C1-C6 codec, transport, composition, and cross-stack
  evidence.

## Unreleased - CBI39 authenticated policy distribution

### Added

- A single-attempt asynchronous distribution client with a host-pinned P-256 endpoint key, fresh
  cryptographic challenge, exact local cursor binding, and signed short-lived response envelope.
- Explicit response-size, entry-count, timeout, cancellation, and no-retry bounds before an optional
  update enters the durable CBI38 registry.
- Shared vectors and named C1-C6 authentication, replay, freshness, bounds, durability, and
  cross-stack evidence.

## Unreleased - CBI38 durable trust-policy checkpoint

### Added

- A bounded canonical checkpoint containing the complete signed CBI37 update chain, with atomic
  publication before live registry advancement and full verifier replay during recovery.
- An issuer-controlled recovery floor that detects missing, older, and same-sequence conflicting
  checkpoint state, plus recovered governed acquisition.
- Shared vectors and named C1-C6 corruption, provenance, crash-residue, rollback, write-failure, and
  cross-stack evidence.

## Unreleased - CBI37 authoritative trust-policy updates

### Added

- A host-pinned ECDSA P-256 policy authority, canonical signed update payload, and process-local
  registry accepting only a strict sequence/predecessor chain.
- A governed acquisition gate that rejects missing or superseded current-policy authorization before
  source access while preserving CBI36 behavior for the current snapshot.
- Shared vectors, golden payload digests, and named C1-C6 provenance, monotonicity, atomicity,
  supersession, and cross-stack evidence.

## Unreleased - CBI36 trust-gated acquisition

### Changed

- `TrustedProviderPublisherAuthorization` is now issued only by
  `ProviderPublisherTrustEvaluator`; callers must replace direct construction with a successful
  CBI35 evaluation.

### Added

- Trust-gated CBI33 acquisition that matches exact content and canonical payload before source
  access while preserving independent trust, transport, and admission observations.
- Shared vectors and named C1-C6 evidence for issuer control, validation order, exact matching,
  zero-access refusals, CBI33/CBI32 composition, and cross-stack agreement.

## Unreleased - CBI35 publisher trust policy

### Added

- Deterministic host trust evaluation of CBI34-verified publisher keys against canonical immutable
  policy snapshots, with explicit admitted, revoked, unknown, unverified, and invalid-policy results.
- Shared vectors and named C1-C6 evidence that keep trust authorization scoped and separate from
  artifact acquisition and admission.

## Unreleased - CBI34 publisher evidence verification

### Added

- Canonical Minimal Host publisher-manifest encoding and detached ECDSA P-256/SHA-256 evidence
  verification with strongly typed public-key identities and detached verified results.
- Shared vectors, a neutral golden payload digest, and named C1-C6 evidence separating signature
  validity from source attribution, host trust, transport, and CBI32 admission.

## Unreleased - CBI33 attributable provider acquisition

### Added

- A Minimal Host byte-bounded acquisition owner that reads a complete provider output from a
  strongly identified injected source and submits private completed bytes to CBI32 staging.
- Separate transport, publisher-evidence, and local-admission observations plus shared vectors and
  named C1-C6 evidence for limits, source mismatch, stream failures, integrity refusal, lifecycle
  composition, and cross-stack agreement.

## Unreleased - CBI32 content-addressed provider staging

### Added

- A Minimal Host content-addressed store for canonical multi-file provider manifests, with verified
  transactional publication, corruption-detecting reuse, CBI31 activation leases, and exact
  removal.
- Shared vectors, a neutral golden identity, and named C1-C6 evidence covering invalid manifests,
  partial-state cleanup, source independence, inactive staging, sibling preservation, and
  cross-stack observations.

## Unreleased - CBI31 verified local provider activation

### Added

- A Minimal Host local-artifact owner that verifies an executable SHA-256 digest, applies
  allowed-root and exact argument-vector policy, and launches the existing portable realization in
  a dedicated no-shell process.
- Shared vectors and named C1-C5 evidence for acquisition refusal, launch policy, isolation,
  CBI30 composition, cross-stack substitution, retirement, and forced cleanup.

## Unreleased - CBI30 process-boundary activation

### Added

- Minimal Host activation through the negotiated Portable Binding realization against both the
  Minimal and Reference provider executables over real operating-system process boundaries.
- Shared vectors, named C1-C5 properties, a phase-boundary completeness review, and mandatory
  cross-process execution in the repository completion gate.

## Unreleased - CBI29 fanned-out child-Port activation

### Added

- Minimal Host evidence that CBI22, CBI27, and CBI28 compose for a complete wide position in one
  child Port, with distinct member binding scopes and one child restart scope.
- Shared vectors, a phase-boundary completeness review, and named C1-C6 properties covering
  containment, whole-position membership, scope separation, child-wide barriers, and parent
  preservation.

### Fixed

- Child activation now preserves structural plan and preparation refusal codes instead of reporting
  them as a generic provider-establishment refusal.

## Unreleased - CBI28 fanned-out set activation

### Added

- Minimal Host activation of a wide position's members, each in the binding scope its caller named,
  beside ordinary `1..1` positions in one attempt under one release barrier.
- A refusal for a wide position supplied without every member the generation resolved for it, for a
  wide member with no binding scope, and for a `1..1` member that names one.
- Shared vectors, a phase-boundary completeness review, and a named test for every contract item.

CBI28's finding is that a wide position could be supplied half-complete and pass both of the existing
plan checks, because each compares the caller's member list with the caller's CM3 plan. Routing a wide
position through CBI27 as a whole makes the generation the authority. Its second result is that the
position's declared minimum is not a runtime concept: CM2 stops carrying it after resolution, the
required-versus-optional split survives only as a Proposed Stack decision, and neither CM3 nor CM4 has
an optional member — so one member short of Ready retires the whole activation.

### Changed

- **Breaking:** `ComponentGroupMember` carries a `Scope: BindingScopeId option`. Every construction
  site must supply it; `Scope = None` is correct for a member of a `1..1` position, which is every
  member the earlier slices activate. A member of a wider position must name its own scope, because
  CM2 gives the position one for all of them.

## Unreleased - CBI27 wider Provider Set translation

### Added

- Minimal Host translation of a CM2 position whose cardinality is not `1..1` into one ordinary
  portable member per resolved member, at preflight, with the caller naming each member's binding
  scope and the Provider Set staying at the composition root.
- Refusals for a `1..1` or mediated position, a membership that is not the generation's, two members
  sharing a binding scope, and any member whose preparation fails — which leaves no member at all,
  because the seam refuses a wide bound rather than narrowing it to a first member.
- A distinct outcome for an optional position that resolved no members, so "nothing was bound" is not
  reported as an empty success.
- Shared vectors, a phase-boundary completeness review, and a named test for every contract item.

CBI27's finding is that a CM binding scope and a portable one are not the same identity: the CM one is
a container holding one binding per member, distinguished by `BindingId`, while the portable one names
a single binding and the seam tells a composition to reject reuse. CBI1's mapping of one onto the other
holds only while a position is `1..1` and a scope holds one position, and the second condition is
already false wherever two positions are resolved in one CM scope. A named test pins it; correcting it
would move every member's `bindingScope` fact and so every pinned CBI4 digest, which is Decision 16.

### Changed

- `ComponentBindingIntegration` prepares a member through an internal per-member step shared with the
  wide path. CBI1's checks, order, and observable behaviour are unchanged.

## Unreleased - CBI26 mediator authority admission

### Added

- Minimal Host admission of the authority of the mediator CBI25 binds, for what the mediator does itself:
  CBI3's admission, unchanged, against the mediator's own occurrence.
- A refusal for a Mediation declaring that it owns authority, because CM5 has no relationship meaning
  "on behalf of" and no grant with a beneficiary.
- Shared vectors, a phase-boundary completeness review, and a named test for every contract item.

CBI26's finding is that CM5 has no deputy: its relationship kinds are AttachedDevice, ExternalPeer,
and ComponentParticipant, and a grant names exactly one Holder. A mediator is therefore admitted for
its own interaction and for nothing else, and only OwnsAuthority among CM2's six ownership flags
changes the outcome. Whether CM5 should gain a deputy is recorded as Decision 15.

## Unreleased - CBI25 mediated-position translation

### Added

- Minimal Host translation of a CM2 position resolved with mediated exposure into portable preflight, by
  binding the Component the Mediation is realized as.
- Refusals for a position that is not mediated, a Mediation realized as a static host with or without
  a named Component, a mapping naming a member of the mediated set instead of the mediator, and a
  mediator occurrence the generation does not resolve.
- Shared vectors, a phase-boundary completeness review, and a named test for every contract item.

CBI25's finding is that the portable seam's refusal of mediated exposure was the answer rather than
the obstacle. It refuses because "an erased Mediation still carries provenance, deputy, and authority
obligations"; CM2 requires a policy-bearing Mediation to be realized as a dedicated Component, so the
obligations have a holder and the holder is an ordinary provider. Nothing mediated is presented to the
seam and no refusal is relaxed. The mediator's authority is deliberately not admitted: whether its
occurrence's grants may stand for what the Mediation owns is a question about deputies that this slice
does not answer.

## Unreleased - CBI24 replacing a generation that offers occupied Ports

### Added

- Minimal Host replacement of a generation with child activations attached to its Ports: the attachments
  are stood down first, deepest-first as CBI23 orders them, and only then is the generation replaced.
- Refusals before anything is retired for an activation that is not attached beneath the retained
  generation, for one whose own parent the caller left out, and for a replacement whose scope was
  never going to cut over.
- Shared vectors, a phase-boundary completeness review, and a named test for every contract item,
  including one that proves the orphan a caller creates by not presenting its attachments.

CBI24's finding is that a replacement silently orphans every attachment beneath the generation it
replaces, and CM4 does it deliberately: its C2 property preserves every unrelated scope, and a child
scope is unrelated. There is also no migration operation - re-pointing an attachment would need CM4 to
hold the declaration as mutable state, and it holds it as an input to one attempt - so a Port does not
migrate; a child is stood down and stood up again.

## Unreleased - CBI23 nested child-Port activation

### Added

- Minimal Host nesting of child activations: a child may itself be the parent of another attachment, with
  CBI22's rules applied unchanged at each level and no bound on depth.
- Ordered withdrawal of an attachment forest, deepest first, with the relation derived from each
  activation's own CM4 observation rather than declared by the caller.
- A refusal for two activations claiming one restart scope, and a terminating report for a relation
  that cannot be ordered.
- Shared nesting and withdrawal vectors, a phase-boundary completeness review, and a named test for
  every contract item.

CBI23's finding is that CM4 models no relationship between a parent and a child after attachment: it
requires the parent scope active at attach time and preserves it, and nothing records that a scope has
children or stands a child down when its parent goes. The ordering is therefore the composition root's,
derived from what an attachment is - a Port of a generation, which its occupant cannot outlive - and it
can only order the activations it is given.

## Unreleased — CBI22 child-Port activation

### Added

- Minimal Host activation of a Component position CM2 resolved inside a child Port, in its own restart
  scope, attached to the scope and generation a released parent activation made active.
- Attachment facts read from the parent's own CM4 observation and from the resolved Port envelope
  rather than from the caller, with a distinct refusal for each disagreement.
- CM4's child classifications reported rather than reformed: an occupied Port without a replacement
  lifecycle, and a host-assisted export that does not follow the child's internal Release.
- Shared child-Port vectors, a phase-boundary completeness review, and a named test for every
  contract item.

### Fixed

- A position CM2 resolved inside a child Port was flattened into an ordinary one and activated in
  whatever restart scope the caller named, dropping the restart boundary the Port exists to give.
  Both the group and singleton activation paths now refuse it, and the child path is the way through.

## Unreleased — CBI21 strongly connected activation groups

### Added

- Minimal Host activation of a strongly connected group that declares no lifecycle protocol, and of a plan
  mixing such a group with singleton ones.
- A named refusal for a group declaring bounded lifecycle protocols, which Portable Binding's
  Composition handoff declares out of scope.
- Evidence locating that refusal: CM3 produces the plan, CM4 accepts it with its declared handshakes
  supplied, and only the portable seam declines it.
- Shared strongly-connected-group vectors, a phase-boundary completeness review, and a named test for
  every contract item.

### Changed

- The plan refusal reports which condition fired — a declared protocol, an unplanned member, an
  unselected member, or a repeated selection — where it previously reported one code for all four.
  CBI12's vectors pin the specific codes now.

CBI21's first finding corrects CBI12 rather than extending it: CM3 groups by strongly connected
component over every edge, so a cyclic group is not the same thing as a group needing Relational
Initialisation, and CBI12 refused the first for a property only the second has. What the seam would
need to host the stage is recorded as Decision 13 rather than approximated.

## Unreleased — CBI20 membership replacement

### Added

- Minimal Host replacement of the generation occupying one restart scope with a successor generation
  that resolves a different set of positions, adding and dropping members across the cutover and
  reporting the added, dropped, and surviving occurrences.
- Refusal of an emptied membership, which is CBI14's withdrawal rather than a replacement.
- Shared membership vectors pinning the derived membership sets and the cutover-only rule for an
  addition, plus a phase-boundary completeness review and a named test for every contract item.

### Fixed

- CBI19 accepted a membership the successor generation does not resolve. It declares one entry per
  successor member and no position added or removed, and checked neither, so a caller supplying a
  strict subset — with a CM3 plan built from that same subset — cut the scope over to a generation
  whose plan covered fewer members than CM2 resolved, retiring the omitted Component with no refusal
  anywhere. It now refuses an under-supplied, over-supplied, or changed membership by name.

### Changed (breaking)

- `ComponentGroupReplacement.replace` refuses inputs it previously accepted: a membership that is not
  exactly the positions the successor generation resolves (`position-not-supplied`,
  `member-not-resolved`) and one that differs from the retained activation's (`membership-changed`).
  A caller replacing a generation that resolves the same positions supplies the generation's full
  membership; a caller adding or dropping a position calls `ComponentGroupMembership.replace`
  instead, which takes the same arguments and additionally reports the added, dropped, and surviving
  occurrences.

The lift needed no new authority rule, because CBI19 decided authority per occurrence: a dropped
occurrence has nothing to follow it to, so its grant is not re-established and no withdrawal is
performed against the receiving domain, while an added occurrence is admitted afresh. An added
position joins only across a cutover, because a CM2 generation is one immutable object and a CM4
attempt covers its whole plan.

## Unreleased — BR-07-BINDING-001 static Attribute-constrained binding

### Added

- `Brontide.Minimal.Experimental.Composition` resolution of an Attribute-constrained binding
  exactly once, recording the effective values that decided it and a per-candidate account of the
  evaluation.
- Explicit failure when no candidate satisfies the declared constraints, candidate exclusion when an
  atom is unevaluatable, deterministic selection under ties, and restoration that consults no source.
- A named test for every item of the shared behavioural contract, each observed failing before being
  accepted.

Architecture 0.7 §18.1 change C3. The 0.7 matrix still records the requirement as `planned`: moving
it to `tested` changes a hash the closed independent-review request pins, which requires retargeting
that review and obtaining fresh attestations from a reviewer who is not an implementation actor. The
implementation and its evidence are complete and awaiting that.
The tempting implementation is a live query that re-answers on every read; the resolved record
therefore captures values rather than sources, and the evidence shows a change that would have
selected differently leaving the binding unmoved. Not a Brontide Base conformance claim.

## Unreleased — CBI19 scoped activation replacement

### Added

- Minimal Host replacement of the generation occupying one restart scope with a successor generation,
  standing the successor up under CBI13's barriers and cutting the scope over to it.
- Re-establishment of authority per occurrence rather than inheritance: a surviving occurrence must
  be re-admitted with the authority that admitted it, a new one is admitted afresh.
- Retirement of the retained members only after cutover, with a post-cutover cleanup failure named
  rather than swallowed.
- Shared replacement vectors pinning the cutover boundary in both directions, plus a phase-boundary
  completeness review and a named test for every contract item.

CBI19's first finding corrects three earlier slices rather than fulfilling them: CM4's scoped
replacement swaps a whole generation atomically, and nothing in CM4 retires one member while its
scope keeps running, so CBI14, CBI15, and CBI18's "retire the whole activation" was already correct
rather than a placeholder. Authority follows the occurrence, which is CBI13's own justification
finally exercised, and the release barrier re-arms for the whole successor activation.

## Unreleased — CBI18 multi-member participant extension

### Added

- Minimal Host declaration-free growth of the participant sets of a multi-member activation, applied
  while every member stays released and refusing removal and substitution in place.
- Activation-wide identity and receiving-domain Actor checks over the extended result, including the
  permitting direction: a party already participating in one member may be added to another under the
  local Actor it already holds.
- Shared extension vectors pinning evaluated participants, members grown, the in-force activation
  size, lapsed members, and released members, plus a phase-boundary completeness review and a named
  test for every contract item.

CBI18 lifts the last single-member slice and dissolves the question it recorded: an activation may
hold declarations for some members and none for others, because growth cannot observe them — a
declaration governs departure, growth removes nobody, and coverage is monotone in the grants held.
The entry point takes no resolution and no declaration, and the absent parameter is the contract. A
lapse in any retained participant still retires the whole activation. The lifting programme is
complete.

## Unreleased — CBI17 multi-member declaration succession

### Added

- Minimal Host narrowing of every member's declaration to one successor generation, applied as one
  transaction over the activation and refused entirely when any member's observed use vetoes it.
- Per-member position, subset, tuple-stability, and attribution checks against that one successor,
  with a member the successor does not narrow treated as untouched rather than refusing.
- Shared succession vectors pinning dropped and vetoed authorities, narrowed members, the
  declarations in force afterwards, and released members, plus a phase-boundary completeness review.

CBI17 answers both questions lifting CBI11 raised: a succession is one transaction, because a CM2
generation is one immutable object resolving every position at once, and a member the successor does
not resolve blocks every other member. It also separates two rules CBI11 stated as one — *nothing to
succeed* stays an activation-level refusal while *this member is untouched* becomes an ordinary
per-member outcome. Nothing here retires a member or reaches a provider, and the operation is
synchronous for that reason. CBI8 is the last single-member slice.

## Unreleased — CBI16 multi-member observed-interaction verification

### Added

- Minimal Host verification of every member's declaration against that member's observed portable
  interaction, through one CM4 request carrying the whole activation's projected binding exercises.
- Per-member attribution and per-member derivation of each exercise's authority admission, with
  exercise identity carried by the occurrence so one request cannot repeat it.
- Shared verification vectors pinning projected exercises, violating members, unexercised and
  uncovered declared authorities, the runtime verdict, released members, and provider effects, plus
  a phase-boundary completeness review.

CBI16 answers what lifting CBI10 raises: one member's undeclared use condemns the whole activation,
because a CBI12 activation is one CM4 request and CM4 refuses it on the first offending exercise
rather than excusing the members that behaved. Attribution stays per member, so the same Operation
in two members is two independent attributions. A structural refusal evaluates nothing and changes
nothing. CBI8 and CBI11 still govern one member.

## Unreleased — CBI15 multi-member participant revision

### Added

- Minimal Host revision of the participant sets of a multi-member activation under per-member
  declarations, decided per member and checked against the activation.
- Activation-wide identity and receiving-domain Actor checks over the revised result, and per-member
  coverage of each member's own declaration.
- Shared revision vectors pinning evaluated participants, the in-force activation size, and released
  members, plus a phase-boundary completeness review.

CBI15 answers what CBI14 left open, and separates two outcomes of one call: a declined change is
local and alters nothing, while a lapse discovered while evaluating retires the whole activation —
including when it is in a member that was not being revised. A wrongly named member set is declined
here rather than retiring as it does in CBI14. CBI8, CBI10, and CBI11 still govern one member.

## Unreleased — CBI14 multi-member revalidation and withdrawal

### Added

- Minimal Host revalidation of every member's authority in a multi-member activation from fresh explicit
  CM5 requests, evaluated all-or-none across the activation.
- Whole-activation retirement when any member's authority lapses, with the lapsed members and the
  participants within them named so the cause stays distinguishable from the consequence.
- Shared withdrawal vectors pinning evaluated members, lapsed members, released members, and
  replacement records, plus a phase-boundary completeness review.

CBI14 answers what CBI13 left open: a CM4 activation has exactly one restart scope and no way to
retire one member while it runs, so members that came up together go down together. CBI8 through
CBI11 still govern one member.

## Unreleased — CBI13 multi-member authority

### Added

- Minimal Host admission of a participant set per member of a multi-member activation, evaluated for
  every member before any provider is contacted.
- Activation-wide identity distinctness for admission, relationship, and authority requests, and a
  receiving-domain Actor mapping required to be a function and injective across the activation.
- Shared group-authority vectors pinning admitted members, aggregate grants, released members, and
  provider effects, plus a phase-boundary completeness review.

### Changed

- The effect-free half of CBI6 admission is now a separate step, so a multi-member activation can
  admit every member's set before any of them is established. CBI6's own behaviour is unchanged.

CBI13 answers both questions the plan raised: authority is admitted per member, against the
occurrence rather than the attempt, and the authority barrier is earlier than the release barrier
rather than the same one. CBI7 through CBI11 still govern one member, so a multi-member activation
has no post-activation authority story yet.

## Unreleased — Decision 11: negotiation compares provider identity

### Changed

- **BREAKING.** Portable negotiation now compares the provider by exact reference equality and
  refuses a mismatch as `unsupported-contract` with local code `provider-mismatch`. A required
  contract document naming a provider is binding rather than expectational.
- **BREAKING.** The Binding Plan's `provider` and `selectedProvider` facts, and the C9
  `selectedProvider` observation, are read from the **offered** document, so they name the provider
  that answered rather than the one the host asked for. Negotiation refuses a mismatch, so the value
  is unchanged wherever a plan exists.

### Added

- Neutral vector `PB-83-PROVIDER-SUBSTITUTED`, executed in Minimal, pinning the refusal.

The composition-seam check is retained for the case negotiation cannot see: a required contract
naming a provider the resolution did not select, reachable only when the requirement names no
provider. Its refusal code stays `provider-substituted`.

BREAKING CHANGE: an endpoint answering as a provider the host did not require is now refused at
negotiation instead of establishing. A host that relied on the permissive behaviour must either name
the provider the peer will answer as, or reach the peer through a resolution that does. Version 0.1
defines no way to say "any provider of this Component"; that would be an additive change.

## Unreleased — CBI12 multi-member activation

### Added

- Minimal Host activation of several independent members under one CM4 activation, each with its own
  resolved position, portable contract, and conversation.
- The release barrier at the activation rather than the member: no member's ordinary-interaction
  gate opens until every member is Ready and CM4 accepts the activation.
- Retirement of every established member when any member fails, so none is left holding an open
  channel, with the failing occurrence named as the cause.
- Shared group-activation vectors pinning failure kinds and codes, member, released, and retired
  counts, and the runtime verdict, plus a phase-boundary completeness review.

CBI12 refuses a cyclic group: a multi-member group is a strongly connected component, which is what
Relational Initialisation exists for. Authority still governs one member — CBI3 and CBI6 through
CBI11 are unchanged — so a multi-member activation has no multi-member authority story yet.

## Unreleased — CBI11 declaration succession

### Added

- Minimal Host narrowing of the declaration in force to a successor CM2 resolution of the same position,
  which must declare strictly fewer authorities with every retained one keeping its exact tuple.
- Observed use as a veto: authority the member has already exercised cannot be narrowed away, while
  disuse never permits a narrowing.
- Shared succession vectors pinning outcome kinds and codes, dropped and vetoed authorities, the
  size of the declaration still in force, and that the member stays released, plus a phase-boundary
  completeness review.

CBI11 has no retirement path and does not change the participant set; it changes what a later CBI9
revision will admit. It does not verify that the successor declaration is truthful — a Component
that narrows dishonestly and then exercises what it dropped is caught by CBI10 as undeclared use.

## Unreleased — CBI10 observed-interaction verification

### Added

- Minimal Host verification of a CBI9 declaration against the portable interactions the member actually
  performed, projected into CM4 binding exercises.
- Derived, never claimed, authority admission on each projected exercise, so CM4's own rule that
  delivery cannot succeed when the external authority check denied it is what condemns interaction
  outside the declaration.
- Shared observed-interaction vectors pinning verdict kinds and codes, projected exercise counts,
  unexercised and uncovered declared authorities, the runtime's verdict, the member's stage, and the
  provider effects the interactions caused, plus a phase-boundary completeness review.

CBI10 supersedes CBI3's refusal of caller-authored binding-exercise authority by deriving that
authority instead of accepting it. It detects a declaration contradicted by use, never one
contradicted by disuse, and it neither authorizes a future interaction nor undoes a past one.

## Unreleased — CBI9 declared grant dependency and participant revision

### Added

- Minimal Host removal and substitution of participants in a live set, admitted while every declared
  dependency stays covered by the intended set.
- An algebraic dependency declaration whose names must equal the requested authority CM2 records for
  the CBI1-selected definition, with the caller supplying only the explicit typed mapping from each
  declared name to a CM5 Capability, target Actor, Operation, and scope.
- Shared revision vectors pinning outcome kinds, codes, evaluated counts, in-force set size and
  grant count, and whether the member is still released, plus a phase-boundary completeness review.

CBI9 closes the question CBI7 and CBI8 both deferred, and disposes of participant precedence:
coverage decides who may leave. It does not verify that a Component's declared authority is truthful
or complete, revoke a departing participant's authority elsewhere, or transfer state between a
departing and an arriving participant.

## Unreleased — CBI8 in-place participant extension

### Added

- Minimal Host growth of an admitted CBI6 participant set while its member stays released, with
  retained participants revalidated in the same all-or-none evaluation as the additions.
- Algebraic extension results carrying the set still in force, whole-set identity and
  receiving-domain Actor checks against participants that are already live, and a declined outcome
  that leaves the binding exactly as it was.
- Shared extension vectors pinning outcome kinds, codes, evaluated counts, the size of the set still
  in force, and whether the member is still released, plus a phase-boundary completeness review.

### Changed

- The cross-request identity check, admission shape check, exactness check, and member retirement
  are now shared between the CBI6, CBI7, and CBI8 modules within the stack instead of being restated
  per slice.

CBI8 only grows a set. Removal and substitution in place are declined and route through CBI7
retirement and a fresh CBI6 admission, which is also why participant precedence does not have to be
decided here.

## Unreleased — CBI7 participant-set withdrawal

### Added

- Minimal Host revalidation of every participant of an admitted CBI6 set from fresh explicit CM5
  requests, keeping the shared member released only when the identical set renews identically.
- Algebraic withdrawal results that name the unrenewed participants, and fail-closed retirement for
  membership change, identity drift, and any participant that does not renew.
- Shared withdrawal vectors pinning outcome kinds, codes, evaluated counts, and unrenewed counts,
  plus a phase-boundary completeness review.

CBI7 answers the question CBI6 deferred: partial loss retires the shared member rather than
narrowing the set, because nothing in an admitted set says which participants its ordinary
interaction depends on. It does not replace a participant in place, order participants, or
propagate revocation to another domain.

## Unreleased — CBI6 participant-set admission

### Added

- Minimal Host admission of a set of participants over one singleton binding, each with its own CM5
  request carrying one `ComponentParticipant` relationship and one or more exact narrow grants.
- Algebraic participant-set results and the cross-request rules the evaluator cannot see: distinct
  admission, relationship, and authority request identities across the set, and distinct
  receiving-domain Actors per participant.
- Shared participant-admission vectors pinning failure kinds, codes, evaluation counts, and
  aggregate grant counts, plus a phase-boundary completeness review.

CBI6 admits a participant set. It does not revalidate or withdraw one, order participants, exercise
a granted Operation, or model participants joining or leaving an active binding.

## Unreleased — CBI5 authority withdrawal

### Added

- Minimal Host revalidation of the exact CM5 relationship and grant behind one active CBI3 binding,
  using fresh explicit time, evidence, and policy.
- Algebraic withdrawal results, shared vectors, and a phase-boundary completeness review.

### Fixed

- PB7 retirement now closes the local member gate before peer withdrawal and termination, so a
  cleanup failure is visible without leaving ordinary interaction released.

CBI5 governs subsequent ordinary interaction for one singleton binding. It does not cancel
in-flight execution or provide distributed revocation.

## Unreleased — CBI4 integrated profile comparison

### Added

- An independent Minimal Host canonical profile for five CBI3 integration outcomes, covering
  complete CM5 parity, CM4 effects and failures, portable lifecycle, and stable plan facts.
- Shared exact profile digests plus the CBI4 capability contract and completeness review.

CBI4 is data-only comparison evidence, not integrated cross-process execution or general
substitutability.

## Unreleased — CBI3 authority-gated portable activation

### Added

- A Minimal Host coordinator that requires one explicit occurrence-to-Actor mapping and one exact
  CM5 `ComponentParticipant` relationship and narrow grant before CBI2 activation.
- Algebraic fail-closed shape, mapping, admission, and lifecycle outcomes that stop denial before
  provider contact and preserve later portable failure.
- Native authority-integration tests plus the CBI3 capability contract and completeness review.

CBI3 does not transport a Capability through Portable Binding or map a CM5 Operation to a portable
invocation. Withdrawal, multiple participants or grants, CM4 binding projection, relational or
multi-member activation, and general interoperability remain outside this slice.

## Unreleased — CBI2 portable lifecycle orchestration

### Added

- A Minimal Host coordinator for one CBI1 member and one singleton, protocol-free CM4 plan.
- CM4 preflight before provider contact, PB7-derived stage evidence, portable-refusal projection,
  and portable Release only after CM4 Active.
- Native lifecycle tests plus the CBI2 capability contract and contract-completeness review.

CBI2 grants no authority and does not support relational or multi-member activation, replacement,
child Ports, mediation, wider Provider Sets, or general interoperability.

## Unreleased — CBI1 Component Management / Portable Binding integration

### Added

- A Minimal Host composition-root adapter from one completed direct `1..1` CM2 provider position
  to PB7 preflight, using explicit CM definition/occurrence and portable Component/provider
  identities.
- Algebraic fail-closed outcomes for unresolved, wider, mediated, empty or multiple, indirect,
  identity-mismatched, invalidly addressed, and portable-preflight-refused positions.
- Native integration tests plus the CBI1 capability contract and contract-completeness review.

CBI1 prepares no provider, fixes no Binding Plan, grants no authority, and makes no real
interchange or Architecture 0.8 conformance claim.

## Unreleased — Component Management CM4 experimental evidence

### Added

- A Minimal-native deterministic fake activation Host over successful CM3 plans, with optional
  effect-free preparation, complete member-stage evidence, lifecycle and ordinary gate enforcement,
  one logical Release, and explicit cutover events.
- Exact scoped replacement with unrelated-scope preservation, retained-generation disposition,
  pre- and post-cutover failure, rollback restoration, rollback-unavailable degradation, and
  retained-generation corruption.
- Post-Release distinct and mediated binding observations with typed identity, provenance, routing,
  authority-check, delivery, and failure evidence, plus runtime-open child-Port and host-assisted
  activation ordering.
- The neutral CM4 vector inventory, phase-wide permutation and failure-silence properties, and the
  completed CM4 contract-completeness review.

CM4 remains a fake Architecture 0.8 experiment. It is not a package loader, production activation
host, process-isolation boundary, durable rollback system, or authority policy; CM5 owns authority
and admission.

## Unreleased — Component Management CM3 experimental evidence

### Added

- A Minimal-native, effect-free activation-group planner that partitions complete activation
  graphs into maximal strongly connected groups and orders the condensation graph dependency-first
  without inventing member startup order.
- Exact contract/version checks, finite lifecycle-protocol validation, Ready reachability and wait
  analysis, Region/Port containment, structured wider-parent and refusal outcomes, and explicit
  closed-gate Local Initialisation, Interconnection, Relational Initialisation, and Ready stages.
- The neutral CM3 vector inventory, phase-wide permutation and failure-silence properties, and the
  completed CM3 contract-completeness review.

CM3 remains fake Architecture 0.8 experimental evidence. Planning performs no preparation,
establishment, lifecycle execution, Ready reporting, Release, Actor or authority establishment, or
active-generation mutation; those runtime transitions begin in CM4.

## Unreleased — Component Management CM2 experimental evidence

### Added

- A Minimal-native algebraic resolver that closes finite acyclic selections into immutable Proposed
  Stack and resolved-generation values with structured refusal and wider-parent outcomes.
- Occupied-binding stability, deterministic preference and affinity ranking, policy exclusions,
  lower-bound Provider Sets, explicit optional preselection, occurrence sharing, visible Mediation,
  child Port envelopes, topology decisions, and post-closure Activation Parameters.
- The neutral CM2 vector inventory, complete permutation properties, and the completed CM2
  contract-completeness review.

CM2 remains fake Architecture 0.8 experimental evidence. It does not prepare, activate, establish an
Actor, grant authority, mutate an active generation, accept cyclic groups, or claim conformance.

## Unreleased — Component Management CM1 experimental evidence

### Added

- A Minimal-native discovery pipeline over pure fake-source states, with standard contract/version
  queries, deterministic source/package/definition ordering, source-endpoint and publisher
  attribution, duplicate claims, advertised package versions, and the source-neutral storefront
  projection.
- Immutable staged artifacts carrying source-attributed contested evidence and fake-policy
  decisions; acquisition returns a `Staged`/`Refused` union with four exhaustive refusal cases.
  Source removal is a pure transition and every CM1 result reports no selection, resolution,
  preparation, activation, Actor establishment, or Capability grant.
- A separate neutral source/evidence-availability fixture, exhaustive enumeration-permutation
  properties, a falsifiable local/remote storefront comparison, and the completed CM1
  contract-completeness review.

This remains a fake Architecture 0.8 experiment outside Brontide Minimal Stack Base. It is not a
marketplace, package manager, loader, security product, conformance claim, or component-version
change.

## Unreleased — Portable Component Binding 0.1 experimental evidence

### Added

- `Brontide.Minimal.Binding.Portable`: the Minimal realization of the Portable Component Binding
  contract under [`binding/portable/`](../binding/portable/README.md), implemented natively rather
  than as a translation of the Reference surface. Every refusal is an explicit `PortableResult`
  value carrying its portable category, so a denial that never leaves the endpoint is a returned
  value rather than a raised failure; the Shape body is an algebraic union; the lifecycle is an
  immutable record whose illegal transition leaves the previous state intact; and the two resource
  flavors are separate union cases, so a forbidden implicit copy is unrepresentable in memory as
  well as refused on the wire. `PortableModelAdapter` is the Minimal-owned adapter between the
  stack's `ShapeValue` model and the neutral positions.
- `PortableCompositionHandoff` and `CompositionMember`: the seam by which a resolved Component
  requirement and an offered provision produce a Binding Plan during activation preflight. The stage
  is a union that carries the established binding, so a member outside the released case has no host
  to interact through. Provider Sets, mediated exposure, an unselected provider, and a provider
  substituted by the answering endpoint are refused rather than approximated.

The retained line-delimited Cooling and Catalog experiments in the same project are unchanged and
remain diagnostic and legacy. This surface is experimental architecture evidence: it is not part of
Brontide Minimal Stack Base, not an Architecture 0.8 conformance claim, not ratified, and not a
component-version change.

## Unreleased — Architecture 0.7 Complete Draft evidence

### Added

- Opaque `CanonicalMemberName`, `MemberKind`, and `MemberName` values for the provisional
  Architecture 0.7 typed-member grammar. Existing `CanonicalName` and binding wire forms are
  unchanged; member kinds remain open validated tokens while the catalogue is provisional.
- Recursive atomic, `AllOf`, `AnyOf`, and `Not` Constraint expressions with explicit satisfied,
  unsatisfied, and indeterminate results. Existing flat Capability and Operation requirements
  remain source-compatible atomic leaves; callers opt in through
  `Genesis.capabilityWithExpressions` and `World.delegateCapabilityWithExpressions`.
- Fail-closed target-side composite evaluation and experimental Composition candidate filtering.

These additions are current-draft evidence, not ratification and not a component-version change.

## Unreleased — Architecture 0.5 implementation correction

### Breaking

- `FragmentDefinition` now requires `HostShape`, the earliest compatible Shape for an authored
  Fragment. Update record construction to supply that host; unrelated open Shapes no longer accept
  the attachment unless they explicitly include the Fragment.
- Issuer-controlled Actor, Capability, Constraint, Execution, Occurrence, and Activity references
  no longer expose public record construction. Carry references returned by `Genesis`, `World`, or
  execution APIs instead of constructing scope/value records.
- Opaque generated references now include an internal deterministic allocation lineage. Treat a
  returned reference as one indivisible identity rather than correlating authority by its
  diagnostic scope/value pair; failed or discarded persistent branches cannot collide with an
  accepted branch, while replaying the same explicit transition still produces the same result.
- `World.create` now requires an explicit `TimeDomainReference`; execution receives a trusted
  `TemporalMark` from the host.
- `ExecutionRequest` now requires `Initiator`, `Target`, and `PresentedCapability`. Migrate callers
  from ambient grant/step helpers to `World.step environment world request`.
- `OperationDefinition` now declares its target Actor. Capability issuance records holder, target,
  operation scope, constraints, parent, issuer, and delegation permission; use
  `World.delegateCapability` to narrow authority.
- Operation handlers return `OperationFailure` rather than text. Use
  `OperationFailure.withoutDetails` or `OperationFailure.withDetails` so failure details have an
  independently validated Shape.
- Operation and Event identity is name-only. Remove semantic version arguments; Shape and Fragment
  references remain versioned.

These projects are repository components rather than independently published packages, so this
change has no package-version field to bump. Any future package extraction must choose its initial
version and treat the corrected API as the baseline.

### Added

- Attributed terminal Outcome Events, redacted execution audits, Genesis occurrence records, and
  authority-qualified canonical names.
- Genesis transactions use a shared authority-domain coordinator across every persistent `World`
  alias. Context issuance is bound to the exact transaction branch; pre-transaction aliases cannot
  dispatch, mutate, or nest while Genesis is active, and escaped uncommitted branches remain inert.
- Independent Catalog/resource process binding, strict adversarial vectors, replay and payload
  controls, and reproducible binding source-cost measurements.
