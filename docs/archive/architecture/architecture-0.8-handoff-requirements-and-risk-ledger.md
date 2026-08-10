# Architecture 0.8 R6/M6 handoff requirements and risk ledger

Status: non-runtime planning evidence for the Complete Draft Architecture 0.8 handoff. This ledger
does not claim Architecture 0.8 implementation or ratification and authorizes no production source
change.

Sources:

- [Architecture 0.8](../../current/architecture/Brontide-Architecture-0.8.md), especially §§10–12, §16, §19, §33, and §35;
- the executed [Architecture 0.8 change plan](../../archive/architecture/Brontide-Architecture-0.8-Change-Plan.md);
- the canonical [Architecture 0.8 adversarial vectors](../../../conformance/architecture-0.8-adversarial-vectors.json);
- the first-stage [Channel requirements and risk ledger](../../future/channel/architecture-0.8-channel-requirements-and-risk-ledger.md);
- [Reference implementation notes](../../../Reference/docs/architecture-0.8-handoff-implementation-notes.md) and
  [Minimal implementation notes](../../../Minimal/docs/architecture-0.8-handoff-implementation-notes.md).

This is the full C1–C14 ledger required by Reference phase R6 and Minimal phase M6. The Channel
ledger remains the detailed register for the first evidence stage; it is incorporated here by
reference rather than duplicated.

## 1. Proof and implementation boundary

This handoff establishes an ordered future-work inventory. It proves that every decided 0.8 change,
every authored vector, both documentation-only coverage entries, the known 0.7 supersession, and
both stacks' representation ceilings have an explicit disposition. It does not prove that either
stack implements any 0.8 behavior.

Disposition vocabulary:

- `existing-floor-to-audit`: 0.7 or experimental behavior appears relevant, but only a future 0.8
  delivery audit may accept it;
- `conflicting-rework`: delivered 0.7 behavior contradicts the 0.8 rule and must remain valid 0.7
  evidence until a separately governed migration;
- `missing-runtime`: the 0.8 behavior has no accepted native implementation evidence;
- `handoff-attested`: this phase's documentation-only obligation is complete;
- `architecture-only`: the change records scope or an open decision and requires no runtime vector.

## 2. Decided evidence order

| Order | Programme | Handoff disposition |
| --- | --- | --- |
| 1 | Channel | Semantic register and 24 shared vectors are recorded in the Channel ledger; the non-ratified draft and executed experimental evidence remain separate from architecture conformance. |
| 2 | Portable Binding and Shape floor | The Portable Binding is the first experimental Channel realization and carries a declared Shape floor; its PB evidence is reusable input to a future 0.8 audit, not accepted 0.8 conformance. |
| 3 | Flow conformance | Still future work. Event Distribution, continuing relationships, and the revocation horizon terminate here; R6/M6 adds no Flow behavior. |

No later stage may be used to retroactively define an earlier one's semantics. In particular, a
portable encoding cannot silently choose the authority-chain representation for either stack.

## 3. C1–C14 requirements register

| ID | Change | Future delivery requirement | Current disposition | Canonical vector or coverage accounting |
| --- | --- | --- | --- | --- |
| A08-HO-C1 | Liveness-scoped validity across derivation chains | Evaluate every liveness-scoped ancestor link at presentation; unevaluatable links deny fail closed. | `missing-runtime`; existing atomic mortality evidence is only an audit input. | BR-08-ADV-C1-001, BR-08-ADV-C1-002, BR-08-ADV-C1-003 |
| A08-HO-C2 | Origin demotion inside Delegation algebra | Every Delegation implicitly conjoins the `Origin.Derived` ceiling as an ordinary Constraint. | `missing-runtime`; current origin restrictions require a future algebra audit. | BR-08-ADV-C2-001, BR-08-ADV-C2-002 |
| A08-HO-C3 | Instantaneous Base authorization | Evaluate once before an effect begins; Base performs no implicit mid-effect re-evaluation. | `existing-floor-to-audit`; both execution roots currently authorize before handlers, but this is not yet 0.8 evidence. | BR-08-ADV-C3-001, BR-08-ADV-C3-002 |
| A08-HO-C4 | Ancestor Constraint visibility | Establish the full derivation-chain conjunction at the target, never leaf-only evaluation. | `existing-floor-to-audit`; both native 0.7 representations traverse parent chains. | BR-08-ADV-C4-001 |
| A08-HO-C5 | Quantified Constraint accounting | Pool the Base default at the Constraint's chain occurrence; denied Executions consume nothing; unenforceable declared scope denies. | `missing-runtime`; no general quantified evaluator or accounting-scope declaration is accepted. | BR-08-ADV-C5-001, BR-08-ADV-C5-002, BR-08-ADV-C5-003 |
| A08-HO-C6 | Delegability default | Delegability is default-on and narrowed only by a conjoining Constraint, never a separately granted Boolean right. | `conflicting-rework`; both 0.7 stacks carry an explicit `DelegationAllowed` representation. | BR-08-ADV-C6-001, BR-08-ADV-C6-002 |
| A08-HO-C7 | Structural strong three-valued evaluation | Replace whole-expression poisoning with structural strong-Kleene evaluation in authority and selection contexts. | `conflicting-rework`; `BR-07-CONSTRAINT-001/-002/-003` remain valid 0.7 evidence until migrated. | BR-08-ADV-C7-001, BR-08-ADV-C7-002, BR-08-ADV-C7-003, BR-08-ADV-C7-004, BR-08-ADV-C7-005, BR-08-ADV-C7-006, BR-08-ADV-C7-007, BR-08-ADV-C7-008 |
| A08-HO-C8 | Constraint-value projection exemption | Never project authority-plane Constraint values across Shape versions; unrecognized required values deny. | `existing-floor-to-audit`; Portable Binding negative evidence is experimental input only. | BR-08-ADV-C8-001, BR-08-ADV-C8-002, BR-08-ADV-C8-003 |
| A08-HO-C9 | Two-plane calculus and first-class declarations | Declare Constraint identity, value Shape/version, evaluator domain, unknown behavior, accounting scope where relevant, and evolution policy. | `missing-runtime`; the draft/portable declarations do not establish stack-wide 0.8 recognition sets. | BR-08-ADV-C9-001, BR-08-ADV-C9-002, BR-08-ADV-C9-003 |
| A08-HO-C10 | Authority issuance by derivation | A resource provider issues authority by Delegation from its primordial resource-space authority; it cannot exceed that chain. | `missing-runtime`; R4/M4 Dataset issuance records Actor and Operation but issues no derived Capability. | BR-08-ADV-C10-001, BR-08-ADV-C10-002 |
| A08-HO-C11 | Representation choice is revocation ceiling | Record each domain's carried, pre-evaluated, or resolved chain representation and the exact ceiling it places on future revocation. | `handoff-attested`; the two implementation notes record distinct current choices and explicitly claim no revocation behavior. | BR-08-ADV-C11-001 |
| A08-HO-C12 | Terminus | Declare attributable Actor-retirement policy for held authority, outbound grants, survival schedules, and stable references. | `missing-runtime`; neither stack exposes a Terminus policy. | BR-08-ADV-C12-001, BR-08-ADV-C12-002, BR-08-ADV-C12-003 |
| A08-HO-C13 | First-hop legibility and Authority Topology | Limit Base legibility claims to explicit first hops and retain transitive reachability analysis as an extension direction. | `architecture-only`; no implementation claim follows. | coverage.C13 |
| A08-HO-C14 | Holder introspection decision | Keep held-authority introspection explicit as an open architecture decision, distinct from discovery of available Operations. | `architecture-only`; no implementation claim follows. | coverage.C14 |

Every runtime disposition is deliberately conservative. Existing behavior becomes 0.8 evidence only
when a future requirements inventory, per-stack matrices, native tests, and independent review say
so; this ledger cannot promote it.

## 4. Required supersession record

Architecture 0.8 C7 supersedes, rather than extends, Architecture 0.7 composite poisoning.
`BR-07-CONSTRAINT-001`, `BR-07-CONSTRAINT-002`, and `BR-07-CONSTRAINT-003` therefore become
`conflicting-rework` inputs to a future 0.8 delivery audit. They remain correct evidence for each
stack's stated Architecture 0.7 target. This phase changes neither implementation and neither pinned
0.7 matrix.

C6 is a second known representation conflict: both current stacks store delegability as an explicit
Boolean property, while 0.8 makes it default-on and Constraint-narrowed. It is recorded now so a
future C6 migration cannot be mistaken for simple evidence promotion.

## 5. Risk register

| ID | Risk | Severity | Mitigation / next evidence |
| --- | --- | --- | --- |
| A08-HO-K1 | A future audit relabels existing tests as 0.8 conformance without forcing the authored vectors. | Critical | Every runtime row remains non-accepted; the mechanical handoff gate accounts for all 33 vector ids, and future matrices must cite executed native evidence. |
| A08-HO-K2 | C7 poisoning evidence is silently carried forward, making gradual vocabulary evolution fail. | Critical | Explicit supersession above; all eight C7 vectors are mandatory migration inputs. |
| A08-HO-K3 | Leaf-only authorization drops an ancestor narrowing after a representation change. | Critical | C4 vector plus per-stack C11 notes; future tests must break the grandparent link deliberately and observe denial. |
| A08-HO-K4 | Representation is chosen for interchange convenience and later over-claims revocation. | High | Capabilities never cross the Portable Binding boundary; each stack records its own current ceiling and claims no revocation semantics. |
| A08-HO-K5 | Quantified Constraints multiply authority across sibling Delegations. | Critical | C5 remains missing until pooled occurrence accounting and denial silence execute natively. |
| A08-HO-K6 | Dataset creation is mistaken for authority issuance because R4/M4 records an issuer. | High | C10 row distinguishes an attributable Dataset record from a Capability derived from provider authority. |
| A08-HO-K7 | Experimental Component Management is promoted into the architecture ledger. | High | §6 exclusions below route it to its fake-manager plan and preserve its non-conformance label. |
| A08-HO-K8 | The Channel/PB implementation order is mistaken for semantic authority. | Medium | The decided order and proof boundary distinguish authored semantics, first realization, and future conformance. |
| A08-HO-K9 | Terminus or holder introspection is invented ad hoc by one stack. | High | C12 remains missing and C14 architecture-only until a separately reviewed policy exists. |

## 6. Explicit exclusions

The recorded Architecture 0.8 Composition, Component Management, minimum-topology, and
trust-admission directions (§18.1, §19, §20.1, §24, §33) remain outside this ledger's conformance
scope. Existing CM0–CM6 evidence belongs to the fake-manager programme and remains experimental.
Mediation, including Aggregation beside Selection, Distribution, and Arbitration, remains a
recorded direction rather than a ratified participant or implementation requirement.

Identity, Distributed, Presentation, Workspace, production distribution, cryptographic trust,
general revocation, and hot replacement are also not implemented by this handoff.

## 7. Handoff acceptance

R6/M6 handoff is complete when:

- the mechanical gate confirms one accounting occurrence for all 33 vector ids and C13/C14 coverage;
- both stack notes satisfy the C11 evidence row and state their no-revocation boundary;
- the sequence, supersession, risks, and exclusions above remain explicit;
- pinned Architecture 0.7 plans, matrices, and delivery ledgers remain byte-for-byte unchanged; and
- repository documentation/evidence gates pass.

The separate [completeness review](./architecture-0.8-handoff-contract-completeness-review.md)
audits what this ledger intentionally leaves unsaid.
