# Architecture 0.8 A08-D2 completeness review

Reviewed: 2026-08-10

This review asks what the A08-D2 contract and its four canonical vectors could otherwise leave
silent. A08-D2 is breaking experimental Complete-Draft evidence; it does not ratify Architecture
0.8 or retarget either stack.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| Carrying link | Does a depth-zero Constraint invalidate the Capability that carries it? | No. Both C6-002 tests first execute the carrying root successfully, then prove that its child and grandchild deny. Depth is measured below the carrying link. |
| Offline derivation | Must Delegation evaluate a target-side Constraint before it can represent a descendant? | No. Both stacks can represent the purported child and deeper descendant; presentation performs the semantic check and denies before effects. This preserves static-table and offline derivation. |
| Default-on widening | Can omission of the old Boolean broaden Operations or change targets? | No. C6-001 asserts the exact inherited target and Operation set plus the parent link. Derivation still only appends Constraints. |
| Multiple restriction links | Can a descendant escape an earlier restriction by adding another Constraint? | No. Each stack evaluates every expression at every chain link, relative to the link carrying it. An exceeded ancestor result remains conjoined with all later results. |
| Origin without assertion | Does implicit demotion relabel every derived occurrence as `Derived`? | No. Each C2-001 test executes the child without an assertion and records `Unverified`; `Derived` appears only when exercised under an inherited origin grant. |
| Repeated Delegation | Is the origin ceiling only attached at the first hop? | No. C2-001 inspects both child and grandchild native representations and finds one implicit `Derived` ceiling on each link. |
| Origin laundering | Can a genesis-grade `Device` grant override the implicit descendant ceiling? | No. The root grant and derived ceiling are separate conjoined ordinary Constraints; the descendant `Device` assertion denies and reaches no effect. |
| Primordial regression | Does adding origin algebra demote a direct genesis-grade assertion? | No. C2-002 records `Device` on a successful primordial outcome and confirms no implicit ceiling was added at issuance. |
| Unknown origin | Can an unrecognized textual origin enter through Minimal's standard Constraint value? | No. Requested origin is a closed native union; malformed standard values evaluate indeterminate and fail closed. Reference uses its closed enum and shaped standard Constraint. |
| Boolean compatibility shim | Could callers continue expressing a separate delegation right through an obsolete overload or field? | No. The field and issuance arguments are removed in both stacks, the phase property checks the public representation, and the migration document replaces `false` with a depth Constraint. |
| Existing evaluator split | Did D2 collapse A08-D1's explicit Draft-0.8 strong-Kleene boundary? | No. Ordinary `step`/`ExecuteAsync` and Draft-0.8 entry points remain distinct. D2 standard atoms participate in whichever structural evaluator the caller selected. |
| Architecture status | Does a breaking public migration change the status registry, stack target, or pinned matrices? | No. Those artifacts remain unchanged; the changelogs and migration note label this executed evidence experimental and breaking. |

No additional capability is required inside A08-D2's C6/C2 boundary. The next separately authorized
slice is A08-D3: Constraint declarations, recognition-set evidence, and projection exemption.
