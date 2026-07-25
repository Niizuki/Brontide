# BRONTIDE

## Architecture 0.8 Change Plan

**Status:** Draft change plan; constraint-algebra hardening decided in full (C1–C8), the
two-plane consolidation decided (C9), and the authority-lifecycle findings decided (C10–C14);
the adversarial vector set is authored at
`conformance/architecture-0.8-adversarial-vectors.json`
**Baseline:** Brontide-Architecture-0.7.md
**Purpose:** Record the decided changes and open decisions arising from the 0.7 constraint-algebra
stress test. The evidence-revision goals recorded in §35.1 of Architecture 0.7 (Portable Component
Binding, Channel, Flow conformance) remain the principal 0.8 programme; this plan currently
records the algebra corrections that precede them and will grow as those items are planned.

This plan is not a specification. Where it states candidate normative wording, that wording is a
drafting starting point and is subject to the editorial pass.

Section references of the form §N refer to Architecture 0.7 section numbers.

Every change in this plan lands together with §29.2 conformance vectors. The adversarial vector
set is authored: the canonical inventory is
`conformance/architecture-0.8-adversarial-vectors.json`, with rationale in
`conformance/architecture-0.8-adversarial-vectors.md`. The pinned plan hash in that inventory is
refreshed when this plan freezes for the document edit.

---

## 1. Decided changes

### C1. Liveness-scoped validity conjunction across derivation chains (stress-test finding F3)

**Problem.** §10.3 defines wall-clock conjunction (intersection of windows) but leaves liveness
conjunction undefined. If Actor A holds a liveness-scoped Capability P and derives C for Actor B,
the chain conjunction of C appears to require the target to evaluate whether A's maintaining
relationship is still live — an observation obligation §10.3 never states. Under the fail-closed
rule a target that cannot evaluate an intermediate link must deny. Composed with the §10.3
mortal-by-default recommendation, the spec's own recommended defaults would quietly disable
Delegation (§6.4) in any domain without chain-liveness machinery: every delegated mortal grant
would be dead on arrival.

**Decided rule** (candidate wording, §10.3):

> Liveness-scoped validity conjoins across a derivation chain like every other Constraint:
> effective authority exists only while every liveness-scoped link in the chain is live at
> authorisation time. Each liveness-scoped Constraint MUST identify the maintained relationship
> that scopes it — the lease, session, attachment, or Flow, and its maintaining Actor —
> precisely enough that the target's authorisation boundary can evaluate the link. A domain
> that permits liveness-scoped Constraints on delegable Capabilities MUST provide targets the
> means to evaluate the liveness of every link at the authorisation boundary. A target that
> cannot evaluate a liveness-scoped link denies under the fail-closed rule (§10.1).

Accompanying design stance:

> In a domain without chain-liveness machinery, a grantor SHOULD attach wall-clock validity
> rather than liveness scoping to delegable grants. A Constraint that is unevaluatable by
> construction at every reachable target does not create mortal authority; it mints authority
> that is dead on arrival.

**Edit sites.** §10.3 (both passages), §33 (retire the liveness-conjunction ambiguity; keep full
revocation semantics open), §29.2 (vector: delegated Capability with an expired intermediate
lease is denied; delegated Capability with an unevaluatable intermediate liveness link is
denied).

### C2. Origin demotion expressed inside the Delegation algebra (stress-test finding F5)

**Problem.** §11 states a Delegation "MUST NOT express authority any other way" than the parent
Capability plus added Constraints. §15 states a delegated Capability "asserts at most
`Origin.Derived`" — an automatic narrowing that is not an added Constraint. Either the demotion
is modelled inside the algebra, or the algebra has an unstated special case every implementation
must independently know.

**Decided rule** (candidate wording, §15, cross-referenced from §11):

> Origin demotion is part of the Delegation algebra, not an exception to it: every Delegation
> implicitly conjoins the Constraint `origin-assertion: at most Origin.Derived`. A derived
> Capability's effective authority therefore remains exactly its parent's Capability plus
> added Constraints (§11), with the demotion Constraint among them, and origin demotion is
> testable through ordinary Constraint evaluation.

Accompanying explanatory note (addressing informativeness of `Origin.Derived`):

> Demotion does not make `Origin.Derived` the common origin of a working system. The default
> origin class of an occurrence is *unverified* (§15); `Origin.Derived` appears only where an
> Actor holds a delegated assertion right and exercises it. The sources for which origin
> matters — devices at attachment, trusted human-input paths — hold genesis-grade grants and
> continue to assert their own classes. Origin stays informative because it is asserted at the
> boundary where it was established and demoted everywhere it is merely relayed.

**Edit sites.** §15 (rule and note), §11 (one cross-reference sentence), §29.2 (optional
vector: a delegated Capability asserting `Origin.Device` is denied the assertion).

### C3. Authorisation is instantaneous; no mid-effect re-evaluation in Base (stress-test finding F6)

**Problem.** §10.1 evaluates effective authority "when an Execution is presented." §10.3's
declaration duties cover extensions defining continuing relationships, but a plain Base Operation
whose Execution merely takes a long time is not a continuing relationship. The semantics of
temporal or state Constraints partway through a long-running effect therefore fall to the
openness presumption (§29.1) — which is divergence at the most safety-relevant boundary.

**Decided rule** (candidate wording, §13.5, cross-referenced from §10.1 and §10.3):

> Authorisation is instantaneous. Effective authority is evaluated once, when the Execution is
> presented (§10.1), and governs only whether the requested effect may begin. Brontide Base
> defines no re-evaluation of Constraints against an effect in progress; an effect already
> begun is governed by the withdrawal and cancellation semantics of §10.3. Mid-effect
> re-evaluation, checkpointed revalidation, and partial-revocation semantics are
> extension-defined and MUST be explicitly declared by the extension or Domain Vocabulary that
> introduces them (§29.1).

**Edit sites.** §13.5 (add after the Execution requirements list), §10.3 (cross-reference),
§33 (note that mid-effect semantics are fenced, not open), §29.2 (vector: a Capability expiring
after presentation does not interrupt an authorised effect in Base semantics).

### C4. Chain conjunction requires ancestor Constraint visibility (stress-test finding F7)

**Problem.** §10.1 requires conjunction "along the derivation chain," while §11 discusses the
derivation graph "where the implementation preserves provenance." Audit provenance and
authorisation-time conjunction are different obligations, but the optional phrasing invites an
implementation to evaluate only the presented Capability's own added Constraints — silently
dropping every ancestor's narrowing.

**Decided rule** (candidate wording, §11):

> Preserving the derivation graph for audit is optional; establishing effective authority is
> not. An implementation MUST be able to evaluate the conjunction of all Constraints along the
> derivation chain at the authorisation boundary — by carrying ancestor Constraints in the
> Capability representation, by pre-evaluating them into a static table, or by resolving them
> through domain machinery. An implementation that evaluates only the presented Capability's
> own added Constraints does not conform.

**Edit sites.** §11 (rule), §29.2 (vector: a grandparent Constraint denies an Execution
presented with the grandchild Capability, in a representation that does not inline ancestor
Constraints).

### C5. Accounting scope of quantified Constraints (stress-test finding F1)

**Problem.** §10.1 defines a Constraint's authorisation meaning as "a narrowing predicate over
an Execution." Rate, capacity, and count Constraints (§10.1, §26) are predicates over
*histories* of Executions, and the narrowing-by-construction guarantee of §11 does not extend to
them automatically. The unstated question is accounting scope: when `rate ≤ 10/min` is inherited
through derivation, whether child Capabilities share the ancestor's budget or instantiate fresh
counters. Under the fresh-counter reading, delegating to N holders multiplies effective rate
authority by N — amplification through a calculus whose defining claim is that amplification is
inexpressible. Secondary ambiguities: whether a denied Execution consumes quota, and whether
stateful evaluation order along the chain is observable.

**Decided rule** (candidate wording, §10.1):

> A Constraint quantified over a history of Executions — rate, capacity, count — is accounted
> at its occurrence in the derivation chain. Every Capability derived from the
> Constraint-carrying ancestor draws on that single budget; Delegation never instantiates a
> fresh budget. Bookkeeping state changes only on successful authorisation; a denied Execution
> consumes nothing. Under this rule the narrowing algebra extends to quantified authority: the
> effects available through all derivations of a Capability together never exceed what that
> Capability itself permits.

**Decided extension — declarable accounting scope.** Pooled accounting must be expressible, not
merely implied, because pooling is precisely where implementations will trip. Candidate wording:

> Every quantified Constraint type MUST declare its accounting scope in its vocabulary
> definition. The Base default, and the only scope Base itself defines, is *chain-occurrence
> pooling*: one budget at the Constraint's occurrence, shared by the entire derivation
> subtree. A vocabulary MAY define a different scope — per-holder, per-target, per-Flow —
> but MUST then state explicitly that Delegation multiplies the aggregate budget under that
> scope, and grantors SHOULD pair such Constraints with Delegation restrictions or explicit
> subtree caps. An evaluator that cannot enforce a declared accounting scope denies under the
> fail-closed rule (§10.1).

Per-holder scopes have legitimate uses ("each holder may keep at most three concurrent
sessions"), and a root grantor that declares such a scope has chosen its multiplication rule
explicitly — conservation of authority is preserved because the multiplication is authored,
attributable, and visible, never accidental.

**Edit sites.** §10.1, §26 (admission interplay), §21.1 (vocabulary declaration duty), §33
(retire the ambiguity), §29.2 (vectors: two sibling derivations of a rate-constrained
Capability share one budget; a denied Execution consumes nothing; a declared per-holder scope
unenforceable at the evaluator denies).

### C6. Delegability default (stress-test finding F4)

**Problem.** §10.1 lists "further Delegation" as a constrainable property, implying
delegable-until-constrained. The §29.2 example states "X permits Delegation" as a Given,
implying an affirmative property. Two conforming implementations can disagree about an
unadorned Capability.

**Decided rule** (candidate wording, §10.1 or §11):

> A Capability is delegable unless a Constraint restricts further Delegation. Delegability is
> not a separately granted right: a derived Capability's delegability never exceeds its
> parent's, because Delegation-restricting Constraints conjoin along the chain like every
> other Constraint.

Rationale recorded for ratification discussion: (1) opt-in delegability would reintroduce
semantic comparison — granting `delegable` at derivation requires checking the parent held it,
which is exactly the evaluation the §11 syntactic-narrowing algebra exists to avoid; default-on
plus narrow-only requires no check anywhere. (2) A Delegation prohibition is not an effect
boundary: a willing holder can always proxy, exercising its authority on a requester's behalf
as a deputy (§13.6). What the Constraint really guarantees is attribution — every Execution
under the authority remains initiated by the named holder, and sharing is forced into visible
deputyship or visible Delegation rather than silent authority transfer. That is a real and
useful narrowing, correctly expressed as a Constraint, and consistent with Delegation as
fundamental (§6.4) and legibility (§28).

**Edit sites.** §10.1 or §11 (one normative sentence), §29.2 (reword the Given to "X carries
no Constraint restricting Delegation").

### C7. Three-valued evaluation replaces expression poisoning (stress-test finding F8)

**Problem.** The 0.7 poisoning rule (§10.1, §29.2; 0.7 change plan C1) denies whenever an
unrecognised atom appears anywhere in a composite expression, even where the expression's truth
is independent of that atom — `AnyOf(satisfied-known, unknown)` denies although authorising is
sound. This forecloses the principal legitimate use of `AnyOf` across version skew: vocabulary
migration of the form `AnyOf(NewConstraint, OldConstraint)`, where older targets recognise only
the old atom. Poisoning makes gradual Constraint evolution impossible without a flag day.

**Decided rule** (candidate wording, §10.1 and the Composition design note, superseding the
poisoning wording):

> Composite Constraint expressions are evaluated in strong three-valued (Kleene) logic. An
> unrecognised or unevaluatable atom has the value Unknown. Unknown never resolves at the
> atom: `Not(Unknown)` is Unknown; `AllOf` is False if any member is False, True only if all
> members are True, otherwise Unknown; `AnyOf` is True if any member is True, False only if
> all members are False, otherwise Unknown. In authority context an Execution is authorised
> only where the expression evaluates True; Unknown and False deny. In selection context a
> candidate is retained only where the expression evaluates True, and the resolver SHOULD
> record every Unknown atom encountered. Evaluation is structural: implementations MUST NOT
> reason across repeated atoms — `AnyOf(X, Not(X))` with X Unknown is Unknown, not a
> tautology.

This is sound where poisoning was sound — an expression evaluating Kleene-True is true under
every interpretation of its Unknown atoms — and strictly more available. The structural clause
is the guard rail: no supervaluation, no satisfiability reasoning, so independent
implementations compute identical results by construction.

**Decided vectors** (§29.2; `U` denotes an unrecognised atom's value, Unknown):

```
Not(U)                            = U     → deny
AnyOf(True, U)                    = True  → authorise
AllOf(True, U)                    = U     → deny
AnyOf(U, False)                   = U     → deny
AllOf(False, U)                   = False → deny
AnyOf(X, Not(X)), X unrecognised  = U     → deny  (no tautology reasoning)
```

The last vector is the trap the structural clause exists for: `AnyOf(X, Not(X))` is a
classical tautology, and a supervaluating implementation would authorise where a Kleene
implementation denies — silent divergence between conforming implementations. The vector
pins the non-clever answer.

**Recorded behavioural changes.** The §29.2 `Not(unknown)` example still denies
(`Not(Unknown)` is Unknown). The §29.2 selection example inverts: `AnyOf` of an unrecognised
atom and a matching atom now *retains* the candidate and records the Unknown atom. The 0.7
poisoning rule is superseded, and its rationale (never convert unknown to `false` and reason
from it) is preserved verbatim inside the Kleene rule.

**Edit sites.** §10.1, Composition design note (Definition Constraints), §29.2 (revise both
composite examples; add the vector table above), §23 (unknown semantics fail safely — wording
survives), §35 of the 0.8 document (record the supersession explicitly).

### C8. Constraint values are exempt from additive projection; strictness demands relocated by layer (stress-test finding F2)

**Problem.** Two individually correct rules collide. The fail-closed rule (§10.1) fires when a
target "cannot establish Shape compatibility" for a Constraint value. Additive Shape versioning
(§16.2, §16.4) guarantees that an older consumer *can* establish compatibility with a newer
value by processing the canonical projection and ignoring unknown structure. A Constraint whose
narrowing semantics ride in later-version optional structure is therefore silently evaluated as
its older, weaker projection: fail-closed never fires, the discarded structure was the
restriction, and projection becomes broadening. §16.3 binds fragment *authors* not to smuggle
constraining semantics, but the evaluator cannot verify that discipline from the value alone.

**Counter-proposal considered and withdrawn.** Strict evaluation as the default, with a per-Constraint
`strict = false` marking permitting older evaluators to project; whole systems (for example
corporate deployments) may declare advisory markings unacceptable and demand always-strict
components; separately, newly ratified Base Constraint types should default to required, with
implementation-specific Constraint types requiring integration-level defaults or an explicitly
acknowledged coupling.

**Analysis recorded against per-atom `strict = false` in authority context.**

- *Consent is necessary but not sufficient.* Only the narrowing party could ever be permitted
  to mark leniency — holder-marked leniency is self-service broadening. But even
  grantor-consented leniency fails the next test.
- *The adversary picks the evaluator.* An authority guarantee is only as strong as its weakest
  reachable evaluator; an adversary routes the Execution to the target where the atom is
  unknown. Under `strict = false` the enforcement floor at that target is *nothing* — the
  narrowing simply vanishes. An ignorable narrowing is therefore not a weaker guarantee but
  the absence of one, presented as one.
- *The sound form of leniency already exists.* Under C7, `AnyOf(NewConstraint, OldConstraint)`
  states an authored fallback: where New is unevaluatable, Old still binds. The enforcement
  floor is explicit, chosen by the grantor, and every authorised Execution satisfies a branch
  the grantor actually wrote. `strict = false` says "else enforce nothing"; the `AnyOf`
  fallback says "else enforce this instead." Graded strictness belongs inside the expression,
  not in an escape hatch beside it.

**Where the counter-proposal is adopted, relocated to the correct layer.**

- *System-wide strictness demands are Profile semantics.* A Profile MAY mandate a recognition
  catalogue: conformance to the Profile requires recognising a stated set of Constraint types
  and value-Shape versions. This is the corporate always-strict deployment, expressed from the
  recognition side rather than the leniency side.
- *A Base-level "MUST implement new Constraint types" is rejected.* It would fail the Embedded
  Test (§3); fail-closed already ensures that non-recognition degrades to stricter, never to
  wrong. Recognition duties are Profile and vocabulary business, not Base mandates.
- *Implementation-specific Constraint types are declared coupling.* Constraints do not carry
  integration defaults — Parameters do (§18.1). The correct instrument is composition-level
  visibility: a composition whose contracts carry authored (`Author:`) Constraint types MUST
  declare that coupling explicitly, or exclude the candidate — echoing the §30 rule that a
  component requiring additional vocabularies is not substitutable and must expose the
  difference.

**Decided rule** (three rules, candidate wording):

> 1. Constraint evaluation is exempt from additive Shape projection (§16.4), unconditionally.
>    A presented Constraint value carrying structure the evaluator does not recognise —
>    including optional constituents introduced by a later version of the value's Shape —
>    makes that atom unevaluatable (Unknown under C7). Projection applies to Operation,
>    Event, and Outcome values, never to Constraint values: projecting a value whose purpose
>    is to narrow authority discards narrowing, and discarded narrowing is broadening.
> 2. Graded strictness across version skew is expressed only as explicit fallback branches —
>    `AnyOf(NewConstraint, OldConstraint)` — evaluated under C7. Vocabulary migration
>    guidance to this effect is added to §21.
> 3. A Profile MAY mandate a recognition catalogue of Constraint types and value-Shape
>    versions; a composition whose contracts carry authored Constraint types MUST declare
>    that coupling explicitly.

**Edit sites.** §10.1, §16.4 (state the exemption at the projection rule),
§20 (Profile recognition catalogues), §21.1 (vocabulary migration guidance), §30 (coupling
declaration cross-reference), §33 (record the per-atom `strict = false` alternative as
rejected, with the adversary-picks-the-evaluator rationale), §29.2 (vectors: a v2 Constraint
value with an added optional constituent is Unknown at a v1 evaluator and denies alone;
`AnyOf(v2-atom, v1-atom)` authorises at the v1 evaluator where the v1 atom is satisfied).

### C9. The two-plane principle, first-class Constraint declarations, and the split evolution calculus

**Problem.** The constraint-algebra findings (C5, C7, C8) are case-by-case symptoms of one
structural condition: integration and authority share machinery without sharing a stated
regime. Since 0.3, Capabilities bind transitively to Shapes, so every authority decision embeds
recognition decisions — yet the two concerns have opposite failure preferences. Integration
failure is an availability failure, so the integration regime wants tolerance: additive
versioning, accept-and-ignore, graceful degradation. Authority failure is a safety failure, so
the authority regime wants intolerance: fail closed, deny on doubt. No single evaluation rule
serves both; each collision to date produced either a hole (F2) or an availability loss (F8).
Meanwhile Constraint types remain second-class — loosely vocabulary-defined, with values
described by ordinary Shapes and evolution inherited from the payload rules — and §7.1's term
status registry does not record Constraint at all.

**Decided principle** (candidate wording, new §6.16 — *Compatibility and authority are
separate evaluation regimes*):

> Every Execution carries two structures across a boundary: the input value — the payload
> plane — and the presented Capability with its Constraint values — the authority plane. The
> planes cross the same boundary under opposite regimes. A value in payload position informs;
> it flows covariantly, unknown additional structure is enrichment, and projection to a known
> version is compatibility (§16.4). A value in authority position restricts; it flows
> contravariantly, unknown structure is where the restriction lives, and projection is
> broadening (§10.1). Integration tolerance is applied only in covariant positions. Every
> position in which a Shape-described value appears MUST be classifiable as covariant or
> contravariant, and extensions MUST declare the classification for positions they introduce.

Accompanying design stance (shift-left):

> Integration questions are answerable early — at composition and binding resolution, which
> record their results once. Authority questions are answerable only late, at presentation.
> A well-composed system therefore discovers recognition mismatches when bindings resolve;
> encountering Unknown at an authorisation boundary at runtime indicates a composition defect
> that travelled, not normal operation. The fail-closed rules remain the backstop, never the
> integration mechanism. Profile recognition catalogues (C8) are this stance operationalised.

**Decided construct — Constraint declarations are Base-normative.** Symmetric with the
Operation declaration discipline of §13.1 (candidate wording, §10.1):

> Every Constraint type is introduced by a declaration stating: its canonical name and
> version; the Shape of its value; its evaluation semantics as a narrowing predicate over an
> Execution (§10.1); its accounting scope where quantified (C5); and any declaration duties
> imposed by its vocabulary. The declaration form is defined by Brontide Base; the catalogue
> of Constraint types belongs to vocabularies, extensions, and Profiles. A domain MAY decline
> to implement a Constraint type; it MUST NOT be unable to identify one. Declining is a
> decision with defined semantics — denial under the fail-closed rule, attributable to a
> nameable declaration. Inability to identify a well-formed declaration is nonconformance.

Rationale recorded (decision by the architecture author): Constraints must be recognisable
among all systems with strict structure and unambiguity. Non-implementation is a posture an
implementation chooses and can state — a declarable recognition set — not an omission the
architecture failed to anticipate. This passes the Embedded Test as the test is defined (§3):
the declaration form is satisfiable by compile-time structure, a static domain pre-evaluates
its constraint relationships at build time, and a domain implementing no dynamic evaluation
still conforms by identifying and denying unrecognised presentations.

**Registry placement.** Candidate stance: §7.1 records Constraint as a subordinate concept
within Capability carrying Base-normative declaration discipline — the status Declared
Fragment holds within Shape — preserving the eight-term claim. The alternative, Constraint as
a ninth Base term, is recorded as an open ratification question rather than decided
mid-cycle. Either way the current registry omission of Constraint is corrected.

**Decided rule — the split evolution calculus.** Each plane evolves under its own calculus,
derived from its variance (candidate wording, §16.2 and §10.1):

> The payload plane evolves additively in place: a later Shape version adds optional
> structure and older consumers project (§16.2). The authority plane evolves by parallel
> names and authored fallback: changing a Constraint type's evaluation semantics or value
> Shape requires a new canonical name, and migration across version skew is written
> explicitly as `AnyOf(NewConstraint, OldConstraint)` under three-valued evaluation (C7).
> Additive-in-place evolution of a Constraint value Shape is not compatibility (C8).

**Boundary fence.** Shape may describe the *presentation* of authority at a boundary — the
Channel and Portable Binding work of this cycle defines exactly that representation — but
Shape never *constitutes* authority. Capability custody remains domain machinery (§8, §10.4);
Capabilities do not travel between trust boundaries, authorisation happens at each boundary
(§8); and capability representations carried as shaped information grant no authority
(Enrichment design note). Description and constitution remain severed; unforgeability lives
in custody, never in structure.

**Edit sites.** New §6.16 (principle and shift-left stance); §10.1 (Constraint declaration
requirements, split evolution calculus for the authority plane); §7.1 (registry entry for
Constraint; ninth-term question recorded in §33); §16.2/§16.4 (payload-plane calculus named
as such; cross-reference the contravariant exemption); §13.5 (name the two planes where
Execution lists them); §21.1 (vocabulary declaration duties); §29.2 (vectors: a well-formed
but unrecognised Constraint declaration denies and is identifiable by name; a Constraint
type semantic change under the same name is nonconforming; a static domain with a declared
recognition set conforms).

### C10. Issuance: creation-time authority is derivation, not Genesis (authority finding A1)

**Problem.** Conservation of authority (§12) admits only primordial and derived Capabilities,
yet the most ordinary act in computing — executing `File.Create` and receiving authority over
the new file — is neither obviously primordial nor obviously derived. The 0.7 changelog names
this the Genesis-versus-issuance question and parks it in §33 as a Corpus detail; it is not a
Corpus detail but a gap in the conservation law, because every Operation that creates an
addressable thing needs the answer.

**Decided rule** (candidate wording, §12; the KeyKOS/EROS factory pattern expressed through
§11's deputy move):

> Authority over a newly created addressable thing is *issued*, not minted. An Operation whose
> effect creates a resource conveys authority over that resource by Delegation from the
> providing Actor's own authority over its resource space, performed as part of the Operation's
> effect and recorded like any other Delegation. Issuance therefore introduces no new
> authority: the provider's authority narrows, conservation holds, and every issued
> Capability's derivation chain terminates, through the provider, in a primordial grant.
> Genesis remains reserved for the domain's own policy moments. A domain MAY realise a
> creation service as domain machinery, in which case issuance and Genesis coincide — by
> stated policy, attributably, never by accident.

Consequence recorded: resource creation is always a deputy pattern — the creating provider is
a deputy over its own resource space — so the invocation principle (§13.6) and its recording
duties apply to issuance without new machinery.

**Edit sites.** §12 (rule), §11 (cross-reference), §18.2 / Persistent Information design note
(Dataset creation resolved by reference to §12), §33 (retire the question), §29.2 (vector: an
issued Capability's chain terminates in the provider's primordial grant; a provider cannot
issue authority exceeding its own resource-space authority).

### C11. Representation choice is the revocation ceiling (authority finding A2)

**Problem.** Full revocation semantics remain deliberately open (§33), scheduled to advance
only as far as Flow ratification forces (§35.1 of 0.7). But C4 names three means of
establishing chain conjunction — carried, pre-evaluated, resolved — and they have radically
different revocability. Nothing warns implementers that this representation choice, made now
for interchange convenience, is the ceiling any future revocation semantics can reach.

**Decided rule** (candidate wording, §11, beside the C4 rule):

> The means by which an implementation establishes chain conjunction is also its revocation
> ceiling. A representation that inlines ancestor Constraints cannot be revoked without an
> indirection point deliberately inserted into its chain; a pre-evaluated static table is
> revocable only by rebuilding the table; a resolved representation revokes naturally at its
> resolver. A domain MUST record its representation choice as an operational property, so
> that future revocation semantics can state which representations satisfy them.
> Revocation-via-indirection (§31) is the recorded candidate mechanism for carried
> representations.

**Edit sites.** §11 (rule), §33 (revocation question annotated with the ceiling taxonomy),
Reference and Minimal implementation notes (record each stack's representation choice and ceiling
before the Portable Binding freezes one).

### C12. Terminus: the counterpart of Genesis (authority finding A3)

**Problem.** Genesis names the moment authority enters the domain; nothing names the moment an
Actor leaves it. The disposition of a retired Actor's held Capabilities, its outbound grants,
and its references is unstated. The §9.1 stability property and mortality (§10.3) shrink the
blast radius but do not answer it: an immortal grant surviving its grantor is currently
authority granted by no one reachable, breaking the §29.3 rule that the trusted party remains
explicit, named, and reachable through provenance.

**Decided concept** (candidate wording, new passage beside §12; scoping concept, recorded in
§7.1 alongside Authority Domain and Genesis):

> **Terminus** is the counterpart of Genesis: the policy occurrence at which an Actor ceases
> to participate in the authority model. Terminus occurrences are implementation- or
> extension-defined but MUST be enumerable and attributable to the domain's own policy,
> exactly as Genesis occurrences are. For Terminus, a domain MUST define: the disposition of
> Capabilities the Actor held — extinguished with the holder, since no surviving participant
> may present them (§13.5); the disposition of Delegations the Actor granted — which outbound
> grants survive, which are extinguished, and on what schedule; and the retirement of the
> Actor's references consistently with §9.1, never reused while a surviving authority
> relationship mentions them. Liveness-scoped grants die with their maintaining relationship
> (§10.3). An immortal grant that survives its grantor MUST remain attributable to the
> granting Actor's recorded identity at grant time, so provenance stays reachable even when
> the grantor is not. Where a Terminus occurrence is exposed to participants, it is
> represented as an Event under §14, as Genesis occurrences are.

The embedded case is trivial by the same argument as Genesis: a static domain has no Terminus
occurrences, and its compiled authority table is the complete disposition policy.

**Edit sites.** §12 (new passage or subsection), §7.1 (registry: Terminus as a scoping
concept), §9.1 (cross-reference), §14 (Terminus Events noted beside Genesis Events), §33
(retire the retirement ambiguity; the survival-schedule vocabulary remains open), §29.2
(vector: a Capability held by a terminated Actor cannot authorise; an immortal surviving grant
remains attributable to its recorded grantor).

### C13. Legibility is scoped to first hops; Authority Topology recorded (authority finding A4)

**Problem.** §28 claims every first hop of every data flow is an explicit, attributable grant
— true, and valuable. But effective *reachable* authority includes what deputies will do on
request: `CI.RequestApproval` reaches deployment authority through `BuildAgent` (§29.4). The
Delegation graph structurally under-approximates reachable authority, §13.6's recording duty
covers exercise only after the fact, and nothing supports asking in advance what an Actor can
ultimately cause. Claiming more legibility than the model delivers is the one dishonesty the
threat model cannot afford.

**Decided rule** (candidate wording, §28):

> Brontide's legibility claim is scoped to first hops: every grant and every first hop of
> every data flow is explicit and attributable. Transitive reachable authority — what an
> Actor can ultimately cause through deputies exposing Operations backed by broader authority
> (§11, §25) — is not computable from the Delegation graph alone, and Brontide does not claim
> it. Analysis of reachable authority is the `Authority Topology` extension direction (§19).

**Edit sites.** §28 (scope statement), §19 (add `Authority Topology` to the provisional
extension names), §7.1 (registry row), §33 (record the analysis question: take-grant-style
reachability over the Delegation graph plus declared deputy surfaces).

### C14. Holder introspection recorded as a decision, not an omission (authority finding A5)

**Problem.** Whether an Actor can enumerate the Capabilities it holds is undefined. Every
practical capability system has grown this facility; Brontide currently has no word for it,
and silence here is an accidental openness (§29.1) rather than a choice.

**Decided disposition** (§33): record the question explicitly — whether holder introspection
is `Discovery` extension business or a Base-adjacent right of every Actor — with the note
that introspection of *held* authority is distinct from discovery of *available* Operations,
and that embedded domains satisfy any future answer statically.

**Edit sites.** §33 (question recorded), §19 (one clause under `Discovery`).

---

## 2. Sequencing of the evidence revision (decided)

§35.1 of Architecture 0.7 ordered the evidence goals: (1) Portable Component Binding and the
Shape floor, (2) Channel "alongside the experiment", (3) Flow conformance. This plan reorders
the first two: **Channel precedes the Portable Binding**, and the binding is authored as the
first conforming realisation of Channel. Flow remains third.

**The argument.**

- *A binding encodes messages; a message is meaningless without the channel's semantic frame.*
  What a request is, how it correlates to its Outcome, at which boundary authority is presented,
  how errors propagate, and what delivery does and does not promise are Channel semantics.
  Freezing an encoding freezes answers to all of these implicitly — schema-guided CBOR framing
  of "a request" is a channel model wearing a serialisation format. "Alongside" in practice
  means the binding's implementation schedule answers semantic questions first and the Channel
  text ratifies what shipped, which is §6.8's accident in slow motion. 0.7 already concedes the
  mechanism ("prevents one implementation's ad hoc answer from becoming the architecture");
  sequencing is the only lever that actually prevents it.
- *The evidence to derive Channel from already exists.* The Cooling and Catalog interchange
  proofs are two independently implemented ad hoc protocols crossing real process boundaries,
  with failure, refusal, replay, version-skew, and strict-variant vectors already tested.
  Channel should be extracted from that evidence — the repo's own evidence-first ethos —
  rather than drafted in the abstract. This also answers the standard objection to
  spec-before-implementation: the implementations exist; what is missing is the common
  semantics they must both already share to have interoperated at all.
- *The rework asymmetry is one-sided.* If the binding freezes first and Channel later
  contradicts it, the frozen wire format, both stacks' transport modules, and the
  binding-measurements evidence all rework. If Channel lands first and the binding realises
  it, Channel rework risk is bounded to abstract semantics with no frozen encodings behind
  them. Sequencing buys insurance at the cost of a few weeks' reordering.
- *The authority plane needs the seam defined.* C9 places authority presentation at the
  boundary in Shape-described form; C8 and C7 define how presented Constraint expressions
  evaluate under version skew. All three describe things that happen *at the Channel seam*.
  The cross-implementation vectors (BR-08-ADV-C7-*, C8-*) need a stable representation of a
  presented Constraint expression to run against both stacks; Channel is where that
  representation is declared.

**Decided order.** Channel (derived from the retained interchange evidence, exercising §13.6's
missing mechanics) → Portable Component Binding and the Shape floor (as Channel's first
conforming realisation, against the C9 presentation contract) → Flow conformance (unchanged;
Event Distribution and the revocation horizon still terminate in it). Explicit non-goals are
unchanged from §35.1 of 0.7.

The adversarial conformance vectors for the changes above are authored in
`conformance/architecture-0.8-adversarial-vectors.json`; mapping them into the `BR-08-*`
requirements inventory happens with the document edit.
