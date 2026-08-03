# CBI36 capability contract — trust-gated provider artifact acquisition

Date: 2026-08-03

Status: implementation contract

## Boundary

CBI36 requires one issuer-controlled CBI35 publisher authorization that exactly matches a canonical
CBI33 acquisition request before delegating any source access to CBI33. Trust, source transport, and
CBI32 admission remain separate observations throughout the composed result.

This is not a policy loader, signature verifier, authorization serializer, network protocol,
credential service, revocation monitor, durable transaction coordinator, malware scanner, or
sandbox. It consumes a same-process authorization issued by CBI35 for the host-selected policy
snapshot; it does not reconstruct or independently verify that policy.

## Capabilities

### C1 — trusted authorization is issuer-controlled

`TrustedProviderPublisherAuthorization` has no public construction path. Only a successful CBI35
evaluation can issue it. Callers migrate from direct construction to
`ProviderPublisherTrustEvaluator`; missing authorization is `publisher-trust-required` and cannot
reach the acquisition source.

### C2 — the complete acquisition request is validated before trust composition

The gate snapshots the Reference request collections and validates the same canonical request used
by CBI34 and CBI33. An invalid request is `acquisition-invalid`, reports trust not evaluated, opens
no source member, creates no acquisition transaction, and attempts no admission.

### C3 — authorization matches exact content and canonical publisher payload

The authorization content identity must equal the request identity and its payload digest must equal
the CBI34 canonical manifest digest. Mismatch is respectively
`publisher-authorization-content-mismatch` or `publisher-authorization-payload-mismatch`; neither
path delegates to CBI33. Policy and publisher-key identities remain attached to every supplied
authorization observation.

### C4 — trust succeeds before any acquisition source access

Only an exact authorization reports `publisher-trusted` and permits delegation. Every trust refusal
reports `transport-not-attempted` and `admission-not-attempted`, and calls neither source identity nor
member access. A later CBI33 source refusal remains a transport result rather than changing trust.

### C5 — successful trust composition preserves CBI33 and CBI32 outcomes

After trust succeeds, CBI36 delegates the unchanged snapshot to CBI33 and preserves its source,
transport, admission, staged, reuse, and integrity observations. Transport completion can still end
in local integrity refusal; successful staging retains the existing CBI32 lifecycle.

### C6 — both implementation roots agree on portable observations

Reference C# and Minimal F# independently consume the shared CBI36 vectors and report the same trust
code, policy identity, publisher-key identity, source identity, transport code, admission code,
staged state, source-access count, and residue observation.

## Phase-wide properties

- No value constructible outside CBI35 can satisfy the authorization parameter.
- Every invalid-request or trust-refusal path observes zero source accesses and no staged content.
- Every delegated acquisition carries the exact policy, publisher key, content, and payload that
  passed the gate.
- Trust success never rewrites a CBI33 transport or CBI32 admission outcome.
- No failure leaves private acquisition content or a partial content address.
