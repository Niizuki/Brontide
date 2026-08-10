# Brontide Reference Stack experimental and sideline projects

This registry separates exploratory work from Brontide Reference Stack milestones and normative Brontide conformance.
A sideline project may provide useful evidence without becoming a required part of the main
showcase or implying that Brontide has ratified its contracts.

Related Architecture 0.7 implementation notes are in
[`architecture-0.7-delivery.md`](./architecture-0.7-delivery.md). Each entry below names its own
design context and remains
experimental even when they are required to produce evidence for the Architecture 0.7 Complete
Draft.

| Project | Classification | Status | Evidence boundary |
| --- | --- | --- | --- |
| Architecture 0.7 Composition delta | Experimental architecture evidence | C1 selection tested in R1; C3 static Attribute-constrained binding implemented and tested in `AttributeBinding.cs` against its [behavioural contract](../../conformance/br-07-binding-001-contract.md), with the 0.7 matrix status still `planned` pending a registry repin and fresh independent review | Composite Constraint selection and static Attribute-constrained binding remain outside Base. The binding resolves once and records effective values and provenance; it holds no source, so a later Attribute or candidate change cannot rebind it, and restoration consults nothing. Accepted evidence may support the 0.7 draft without ratifying Component, Attribute, or Binding Plan vocabularies. |
| `Brontide.Reference.Experimental.PersistentInformation` | Experimental architecture evidence | R4 implemented and tested against [`BR-07-PERSISTENT-INFORMATION-001`](../../conformance/br-07-persistent-information-001-contract.md); matrix promotion awaits review retargeting | The Opaque Corpus/Dataset/Store-role/Router slice tests C4/C5 only. In-memory endpoints, a single-writer declaration checked at operations, and bounded Router fallback do not imply durable media, transactions, a complete persistence system, deep Router policy, or a ratified extension. |
| `Brontide.Reference.Experimental.Binding` (`Portable/`) | Experimental architecture evidence | PB2 implemented and tested, PB4 parity measured, PB5 paired across stacks, PB6 hardened, PB7 Composition handoff added, PB8 evidence and documentation recorded, Decision 11 ruled on and delivered; PB8's independent reviews outstanding | The Reference native realization of the [Portable Component Binding Implementation Plan 0.1](../../docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md), built against the data-only neutral contract under [`binding/portable/`](../../binding/portable/README.md). It implements deterministic-CBOR encoding, negotiation and a frozen Binding Plan, local authority with frameless denial, referenced resources, the lifecycle machine, and the C9 observation set, in a fixed direct-call and a negotiated process realization. Cooling and Catalog are fixtures over that layer, not definitions of it. PB4 measured the two realizations against each other over every portable result class a host can reach and closed the two divergences that found: an endpoint-decided refusal reported two failure domains, and an authority-bearing body was refused under two categories. Every Channel 0.1 vector now has executed evidence here. PB5 paired the implementations: this host now drives the Minimal provider and an [implementation-neutral provider](../../binding/neutral-provider/README.md) over the portable contract, reaching the same category-level observations it reaches talking to itself. The retained line-delimited Cooling and Catalog experiments in the same project are consequently no longer the only cross-stack evidence, though they are retained. PB6 has since property-tested the decoders inside deterministic bounds and proved that failure paths leak no provider effect, value, runtime type, resource, or false success. Doing so corrected three defects here: resource observations claimed an acceptance and an integrity check that never happened, the transport let foreign exceptions escape the binding, and two declared process categories had no path that could produce them. The C6 and C8 refusals are now decided by an endpoint across a real seam rather than by a codec or a lifecycle object called directly. All three defects appeared identically in the Minimal stack too, which is why they matter: independent implementation catches divergence between the two and cannot catch a blind spot they share. PB7 added the Composition handoff in `Portable/PortableCompositionHandoff.cs`: a resolved Component requirement and an offered provision produce a Binding Plan at activation preflight, preserving the binding scope and the selected provider identity, with the ordinary-interaction gate closed until the composition releases the member. A Provider Set, a mediated exposure, an unselected provider, and a provider substituted by the answering endpoint are refused rather than approximated, because each is a Component Management decision. PB7 also found that negotiation never compared provider identity, so the plan's provider fact named who this host asked for rather than who answered — identically in the Minimal stack, and raised as Decision 11. That was ruled on 2026-07-30: negotiation now refuses a provider mismatch as `unsupported-contract`, and the plan and its C9 observation read the fact from the offered document, so it names who answered. The composition-seam check is retained for the case negotiation cannot see — a required contract naming a provider the resolution did not select. Nothing here is an Architecture 0.8 conformance claim or part of Brontide Base. |
| `Brontide.Reference.Experimental.ComponentManagement` | Experimental architecture evidence (fake harness) | CM0-CM6 implemented and tested | Fake, deterministic Component Manager for the [Component Management Implementation Plan 0.1](../../docs/future/component-management/Brontide-Component-Management-Implementation-Plan-0.1.md). CM0-CM4 deliver native fixture loading, discovery, acquisition, resolution, activation-group planning, and fake runtime activation. CM5 adds attributable evidence evaluation, receiving-domain Actor mapping, exact narrow Capability admission, revocation and expiry refusal, unlimited-authority denial, and policy-mistake recording. CM6 adds complete canonical CM5-profile comparison against the Minimal implementation through bounded JSON Lines provider processes in both host directions. Agreement is limited to eight deterministic fake scenarios. It is not real Component interchange, a marketplace, package manager, loader, resolver policy product, production activation host, durable rollback system, security product, or Architecture 0.8 conformance evidence. |
| GPU execution | Experimental sideline | Planned | Execute the same semantic image Operation through an explicitly eligible GPU provider while exposing compilation, buffers, host/device copies, batching, dispatch, failure domain, and CPU fallback. It must not infer GPU compatibility from ordinary Operation conformance and must not be represented by the existing `System.Numerics` vector provider. |

Graduation into the main showcase would require repeatable GPU execution tests, structured
operational observations, honest fallback behavior, and evidence that the transformation module
does not need an application-level redesign.

Reference Studio is the composition root for the CBI1-CBI30 integration slices. It references the
independent Component Management and Portable Binding experiments and maps one completed native
CM2 direct `1..1` position into PB7 preflight, then coordinates one singleton, protocol-free CM4
plan from PB7 lifecycle evidence and releases the portable gate only after CM4 Active.
CBI3 additionally requires one explicit occurrence-to-Actor mapping and one exact local CM5
relationship and grant before provider contact. That grant is not transported through PB7 or
treated as authority for a portable Operation. CBI4 independently serializes five native outcomes
to shared canonical profile digests; this is data-only parity rather than an integrated process
seam. CBI5 revalidates that exact local relationship and grant after activation, retiring the PB7
member before further ordinary interaction when authority is not renewed. CBI6 admits a set of
participants holding one or more exact narrow grants each over that one member, owning the
cross-request identity and receiving-domain Actor rules a single CM5 request cannot see. CBI7
revalidates that set and retires the shared member when the identical set does not renew
identically, naming the unrenewed participants instead of narrowing the set. CBI8 grows that set in
place while the member stays released and declines every change that would shrink it. CBI9 removes
and substitutes participants under a dependency the resolved definition declares, admitting a
revision only while every declared dependency stays covered. CBI10 verifies that declaration against
observed portable interaction through CM4 binding exercises whose authority admission is derived
rather than claimed. CBI11 narrows a declaration to a successor resolution of the same position,
with observed use as a veto and no retirement path. CBI12 activates several independent members
under one CM4 activation, with the release barrier at the activation rather than at any member. CBI13
admits a participant set per member before any provider is contacted, so the authority barrier is
earlier than the release barrier. CBI14 revalidates every member and retires the whole activation
when any member’s authority lapses, because a CM4 activation has one restart scope. CBI15 revises
those sets per member and checks the result against the activation. CBI16 verifies every member's
declaration against that member's observed interaction through one CM4 request, so one member's
undeclared use condemns the activation. CBI17 narrows those declarations to one successor generation
as a single transaction, so a member the successor does not resolve blocks the others. CBI18 grows
those sets without consulting any declaration, because growth removes nobody and so cannot uncover a
declared dependency. CBI19 replaces the generation in the restart scope with a successor generation,
re-establishing authority per occurrence and retiring the retained members only after cutover. CBI20
lets that successor resolve a different set of positions, reading the membership from the generation
rather than the caller and joining an added position only across the cutover. CBI21 activates a
strongly connected group that declares no lifecycle protocol, and refuses one that does because the
portable seam declares Relational Initialisation out of scope. CBI22 attaches one child activation
to a runtime-open Port of a released parent, in its own restart scope, and CBI23 nests those
attachments and retires a supplied forest deepest first, and CBI24 stands those attachments down
before replacing the generation offering their Ports, CBI25 binds the Component a Mediation is
realized as, CBI26 admits that mediator's own authority, CBI27 carries a position wider than
`1..1` into preflight as one ordinary member per resolved member, CBI28 activates those members
under one release barrier, and CBI29 runs that complete wide position through one child Port while
leaving its released parent unchanged. CBI30 runs the direct activation through either stack's
provider executable over the negotiated portable process realization. This
does not merge the experimental projects,
perform a lifecycle handshake, detect an attachment it was not given, admit authority on behalf of a
mediated member, keep a Provider Set serving when one member is lost, or establish real Component
interchange.
