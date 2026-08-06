# CBI63 contract-completeness review

Date: 2026-08-07

Status: complete

This review asks what the CBI63 contract could otherwise leave unsaid. It is separate from checking
whether the two implementations conform to C1-C7.

## Findings closed in the contract

- **The item asked for wider evidence and what it needed was narrower.** "Name which of the two loops
  the host has verified" presumes the host should be speaking about both. Two of the three things a
  governed cycle can do — the rotation and the policy update — are recorded durably by the components
  that do them, so an assertion about either is a claim the record already answers better. The
  evidence therefore carries exactly one verdict, the serving one, and there is no field a host could
  over-assert into. This is the third consecutive slice whose scheduling item had to be corrected
  rather than fulfilled, which is worth carrying forward: an item written from the shape of the
  previous slice tends to propose symmetry the models do not have.
- **The loop boundary is not the verifiability boundary, and the contract says which one it uses.**
  CBI61 split a cycle into the rotation loop and the CBI47 loop. The split that matters here runs
  through the middle of the second one: the poll's effect is durably recorded and the sweep's is not.
  Had the evidence been organised by loop it would have had one field covering a derivable effect and
  an underivable one together, which is exactly the over-assertion this slice removes.
- **The same device CBI62 refused is sound here because of when it is written.** CBI62 refused a
  marker written after the rotation returns, because such a write is not atomic with the effect it
  describes. A cursor written before the cycle describes state that already exists and rides in the
  write that already marks the attempt in-flight. The contract states the distinction as *when*
  rather than as a preference, and a named test asserts the transition sequence is unchanged so the
  "no extra write" claim is checked rather than asserted.
- **A derived effect reports and does not veto, and the reason is a previous slice's result.** CBI62
  established that a retried governed cycle cannot double-apply either half. Without that, a derived
  rotation would look like grounds to refuse retry, and refusing would have been the cautious-seeming
  choice. The contract records the dependency so a later reader does not reintroduce the veto.
- **An absent cursor is a real state, not a legacy detail.** A journal written before this slice has
  no baseline. Deriving against an invented zero would produce confident nonsense — every effect
  would read as applied — so it is refused and routed to CBI49's path, which is correct for the
  ungoverned run that journal actually describes. A named vector pins that nothing is derived.
- **A cursor above the observed state is a rollback, not an absence of effect.** Comparing only for
  "advanced" would silently read a regressed registry as "nothing happened", which is the direction
  that loses the alarm. It is refused instead.
- **Refusing the ungoverned path needs the ungoverned path to still work.** The C2 test checks both
  directions in one run, because a refusal that came from the path having broken would pass a
  one-sided test.

## What the phase deliberately does not decide

Whether an interrupted governed cycle *should* be retried remains the host's, informed by the serving
verdict it alone can supply. Nothing here inspects providers or proves a sweep's retirements and
cleanup happened.

## Residual limits

A derived rotation whose floor was not retained is reported rather than repaired; refusing retry
until custody is repaired would be a policy this slice has no grounds to invent, and CBI61 already
stops a cadence on that outcome when it is observed live. The derivation covers what the local
durable record states and nothing beyond it. Cross-process ownership and privileged custody of either
floor remain separate. The next bounded implementation boundary is a host that terminates providers
when CBI49's grace expires, which CBI49 names as its own deliberate limit and which no slice has yet
taken.
