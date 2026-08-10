# Architecture 0.8 A08-D6 completeness review

Reviewed: 2026-08-10

This review asks what the three canonical C12 vectors could otherwise leave silent. A08-D6 remains
experimental evidence and does not retarget either implementation.

| Area | Potentially silent question | Disposition |
| --- | --- | --- |
| Record, not deletion | Can Terminus erase the Actor or Capability records it affects? | No. Both stacks retain stable Actor, Capability, provenance, and occurrence identities; retired references are never reused. |
| Held authority | Does a held Capability have to be deleted to stop authorizing? | No. Presentation fails because its designated holder is retired. Retaining it also preserves ancestry for immortal grants that survive the grantor. |
| Outbound boundary | Which grants are classified by the policy? | Direct grants issued by the retiring Actor to another holder are the policy boundary. Their descendants inherit the resulting survival or extinction through the complete chain. |
| Immortal survival | Can grantor retirement silently narrow or broaden a surviving grant? | No. Target, Operations, Constraints, parent, and grantor attribution remain unchanged; ordinary later Delegation may only narrow it. |
| Liveness scope | Can a descendant escape a relationship maintained by the retiring Actor? | No. A direct outbound grant whose effective chain contains that Actor's liveness lease, plus every descendant, is extinguished immediately and the lease is made dead. |
| Duplicate Terminus | Can repeated or concurrent retirement create multiple occurrences? | No. Reference serializes the active-Actor check and mutation under the domain gate; Minimal's immutable transition rejects an already-retired Actor. Both tests require one occurrence only. |
| Policy attribution | Can an unknown or retired policy Actor retire another Actor? | No. Both transitions require an active, known policy Actor distinct from the domain authority and target Actor. |
| Target retirement | Can an Execution still target a retired Actor? | No. Draft-0.8 execution rejects a retired initiator or target before effect dispatch. |
| New authority | Can a retired Actor issue or receive a later Delegation? | No. Registration and Delegation validate active issuer, holder, target, and parent state. A surviving holder may still narrow an immortal grant. |
| Time and ordering | Is the occurrence ordered and attributable in each native model? | Yes. Minimal validates monotonic trusted marks and prepends an immutable record; Reference records an ordered provenance interaction at the synchronized transition. |
| In-flight effects | Does Terminus retroactively revoke an effect already authorized? | No. D6 retains the existing instantaneous-authorization boundary; only later presentations observe retirement. |
| Open vocabulary | Does this slice standardize cross-domain schedules or custodianship? | No. It supplies one explicit domain policy: immortal direct grants survive indefinitely, liveness-scoped grants die immediately, and references are retained. Broader vocabulary remains open. |

No additional runtime capability remains inside A08-D6's C12 boundary. A08-D1 through A08-D6 now
complete the separately authorized Architecture 0.8 runtime queue. Stack-wide 0.8 retargeting,
status-registry changes, pinned-matrix review, and ratification remain a separately authorized
closure phase.
