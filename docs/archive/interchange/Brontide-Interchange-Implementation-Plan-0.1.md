# BRONTIDE

## Reference/Minimal Interchange Implementation Plan 0.1

**Status:** Implemented experimental programme; retained as the plan and evidence index
**Date:** 2026-07-17
**Designed for:** [Brontide Architecture 0.7](../../../Brontide-Architecture-0.7.md), Complete Draft
**First proof:** two-way Cooling component interchange across a process boundary
**Decisive later proof:** a mixed Reference/Minimal/independent-provider image workspace

**Implementation status:** Phases P0-P4 are retained as executable experimental evidence. The
descriptor, protocol, and binding observation remain unratified test instruments.

---

## 1. Purpose

Brontide Reference Stack and Brontide Minimal Stack now provide independent native evidence for Brontide. That is necessary, but it is
not yet the architectural proof described by Architecture 0.7 §30: a Component implemented by one
stack must be usable from the other through declared Brontide contracts without sharing either
implementation's private object model.

This plan governs the first cross-stack evidence programme. It turns the existing Brontide Minimal Stack-side
manifest and value-codec experiment into a deliberately narrow, process-isolated Cooling exchange,
then records what the exchange teaches Brontide, Brontide Reference Stack, Brontide Minimal Stack, and the proposed binding seam.

The success claim is intentionally limited:

> Brontide Reference Stack and Brontide Minimal Stack can each host a Cooling Component implemented by the other stack under one
> declared test contract, while authority stays with the host and the process binding preserves the
> Shapes, Outcomes, provenance, and operational facts required by the experiment.

The first proof does not claim that the test protocol is the Brontide Portable Binding, that
Components are hot-swappable, that Capabilities federate across authority domains, or that the
same mechanism has been demonstrated across machines.

## 2. Starting point

The repository begins this programme with both native gates green:

- Brontide Reference Stack implements its native Cooling scenario, Shape projection, authority checks, Outcomes,
  provenance, Enrichment experiment, Studio inspection, and Architecture 0.5 composition
  experiment.
- Brontide Minimal Stack implements its independent kernel, native Cooling transition, Shape values, experimental
  Enrichment and Composition, plus a versioned JSON manifest and tagged value codec.
- Brontide Reference Stack and Brontide Minimal Stack do not reference one another's projects or private CLR types.
- Brontide Minimal Stack's current interchange tests use an `external-runtime` fixture in-process. They prove a
  data seam, not actual Reference/Minimal interchange.
- The native Cooling vocabularies are not currently the same contract. Brontide Reference Stack exposes its own
  `Temperature.Read`, `Fan.SetSpeed`, and `Fan.Stop` scenario; Brontide Minimal Stack exposes
  `Brontide Minimal Stack.cooling.apply`. Structural resemblance is not interchangeability.

The following native gaps were prerequisites and are now retained as executable evidence:

- Brontide Minimal Stack M3 retains the Velocity and authored-Fragment conformance cases.
- Brontide Minimal Stack M4 retains the pointer-temperature Enrichment scenario, now classified
  against Architecture 0.7 §16.6 as non-normative design-direction evidence.
- The independent binding manifests describe Shapes per Operation, required Fragments, authority
  requirements, dependency strength, provider identity, and binding limitations.
- Both stacks own external experimental binding endpoints and native host adapters.

Brontide Minimal Stack M8 Macro Operation, cross-stack Flow recovery, a machine boundary, the mixed image workspace,
and GPU execution are later phases. They do not block the first Cooling proof.

## 3. Governing boundaries

### 3.1 Keep the implementations independent

- Brontide Reference Stack projects MUST NOT reference Brontide Minimal Stack projects or assemblies.
- Brontide Minimal Stack projects MUST NOT reference Brontide Reference Stack projects or assemblies.
- Neither side may exchange `System.Type`, exceptions, dependency-injection registrations, object
  references, private identifiers, or implementation-specific Shape carriers.
- Shared material is restricted to external, versioned data fixtures and process messages. Each
  stack parses those fixtures into its own native types.
- A test harness may launch both processes, but it must not become a shared semantic runtime.

### 3.2 Use a neutral authored test contract

The exchange MUST define one versioned Cooling contract owned by the interchange fixture. It MUST
use an explicitly authored, non-`Brontide:` prefix and MUST NOT rename a Brontide Reference Stack-authored contract as
Brontide Minimal Stack-owned or the reverse. The fixture records:

- Component and provider identities;
- Operations and their versions;
- independent input and output Shape identities and versions;
- canonical fields, required Declared Fragments, and open/closed Fragment policy;
- host authority requirements;
- dependency strengths and any provider-specific requirement;
- binding limitations and supported representation choices; and
- the vocabulary/Profile/Extension versions, if any, on which the claim depends.

Brontide Reference Stack and Brontide Minimal Stack implement or map that contract independently. Mapping within the same declared
Shape contract is binding work. Translation between different semantic Operations or Shapes is an
explicit Adapter obligation and must not be hidden as host machinery.

### 3.3 Keep authority with the host

The first proof uses one host-owned authority domain per direction. The host evaluates its own
Actor, Capability, constraints, target, and Operation before invoking the foreign process. A denial
MUST prevent the foreign provider effect.

The provider receives only the accepted invocation data and the minimum attributable context needed
by the test. It does not receive a serialised Capability that it can reinterpret as authority. Any
binding-scoped execution or occurrence identifiers are mapped explicitly and are not presented as
cross-domain identities.

### 3.4 Treat the protocol as a test instrument

The initial transport is a versioned UTF-8 JSON-lines exchange over redirected standard input and
output, with exactly one complete message object per line. It is chosen for inspectability and
process isolation, not as a ratified wire format. Messages cover manifest negotiation, accepted
invocation, terminal Outcome, protocol failure, and orderly shutdown. Standard error is diagnostic
only and never carries semantic results.

Brontide Minimal Stack's existing `protocolVersion: 1` fixture remains readable as historical seam evidence. The
expanded cross-stack manifest and message envelopes use a new protocol version rather than assigning
new required meaning to version 1. Compatibility behaviour is explicit and covered by fixtures;
receivers never guess across protocol versions.

Every parser fails closed on unknown protocol versions, malformed values, duplicate protected
fields, unsupported required contracts, and invalid identifiers. Optional unknown data may be
preserved only where the declared Shape or message contract permits it.

## 4. Required execution observation

Each bound Execution produces one structured observation owned by the host. It records, without
inventing unknown values:

- host and selected provider;
- selection reason and rejected alternatives;
- Operation, input Shape, and output Shape;
- representation and crossed process boundaries;
- copies and referenced resources used by the binding;
- host authority decision and the point at which it occurred;
- mapping or Adapter obligations;
- retry, interruption, provider-process failure, and fallback facts;
- failure domain;
- terminal Outcome; and
- timing supplied by an explicit host clock.

The document and implementation MUST call this an experimental binding observation, not a ratified
Binding Plan or universal Brontide trace.

## 5. Delivery phases and gates

### Phase 0 — freeze the shared contract and close prerequisite evidence

1. Add the missing Brontide Minimal Stack M3 Velocity/authored-Fragment conformance cases.
2. Add the Brontide Minimal Stack M4 pointer-temperature Enrichment scenario outside Model and Kernel.
3. Write the neutral Cooling manifest fixture and representative valid/invalid value fixtures.
4. Produce a contract matrix comparing the fixture with the existing Brontide Reference Stack and Brontide Minimal Stack Cooling
   surfaces. Classify every difference as native mapping, semantic Adapter, binding requirement,
   or Brontide ambiguity.
5. Parse and validate the same fixtures independently on both sides.

**Gate P0:** both native suites and dependency guards pass; both stacks reject the same invalid
fixtures for semantically equivalent reasons; no shared runtime contract assembly exists.

### Phase 1 — complete the experimental binding surface

1. Define the next protocol version and extend its manifest to cover the declarations in §3.2.
2. Define versioned request, response, denial, protocol-error, and shutdown envelopes.
3. Complete tagged ShapeValue encoding for the Cooling inputs, results, details, and Fragments.
4. Define binding-scoped identifiers separately for requests, Executions, and occurrences.
5. Implement structured observations from §4, including a visible provider-process failure.
6. Add boundary checks that reject CLR type metadata, exception-shaped outcomes, and undeclared
   required contracts.

**Gate P1:** golden fixtures round-trip independently; incompatible manifests fail before
activation; malformed or unsupported messages cannot invoke an effect.

### Phase 2 — Brontide Reference Stack hosts Brontide Minimal Stack

1. Add a Brontide Minimal Stack-owned non-interactive provider endpoint that serves the fixture contract through
   Brontide Minimal Stack's public binding and Cooling surfaces.
2. Add a Brontide Reference Stack-owned experimental host adapter that launches that endpoint and translates only
   through process data.
3. Authorise requests in Brontide Reference Stack before sending accepted invocations to Brontide.Minimal.
4. Return Brontide Minimal Stack results as Brontide Reference Stack-native Outcomes and record provenance plus the complete binding
   observation.

**Gate P2:** the Brontide Reference Stack-hosted suite passes the acceptance matrix in §6 and proves that denied
Brontide Reference Stack requests do not reach the Brontide Minimal Stack effect.

### Phase 3 — Brontide Minimal Stack hosts Brontide Reference Stack

1. Add the corresponding Brontide Reference Stack-owned non-interactive provider endpoint.
2. Add a Brontide Minimal Stack-owned host adapter using Brontide Minimal Stack's immutable authority decision path.
3. Authorise requests in Brontide Minimal Stack before sending accepted invocations to Brontide.Reference.
4. Return Brontide Reference Stack results as Brontide Minimal Stack-native Outcomes and record the same observation fields.

**Gate P3:** the Brontide Minimal Stack-hosted suite passes the same acceptance matrix and proves that denied Brontide Minimal Stack
requests do not reach the Brontide Reference Stack effect.

### Phase 4 — consolidate evidence

1. Run both implementation suites and both dependency guards.
2. Audit project references, output assemblies, process messages, and fixtures for private-type or
   shared-runtime leakage.
3. Measure Adapter code and information loss in both directions.
4. Record each discovered obligation as a Brontide Reference Stack gap, Brontide Minimal Stack gap, binding gap, or Brontide ambiguity.
5. Update both milestone-evidence documents and both implementation-findings documents.
6. Record which findings inform the Architecture 0.8 Portable Binding, Channel, and Flow evidence
   programme. Passing the experiment alone does not ratify its protocol or descriptor format.

**Gate P4:** both directions are repeatable from a clean build, all acceptance cases are retained
as executable tests, the operational observations are inspectable, and the evidence documents make
no stronger claim than the experiment supports.

## 6. Acceptance matrix

Both host directions MUST cover the same behavioural cases:

| Case | Required observation |
| --- | --- |
| Compatible activation | Exact contract versions negotiate before provider activation. |
| Protocol mismatch | Activation fails visibly; no provider effect occurs. |
| Missing Operation or Shape | Negotiation fails closed and names the missing reference. |
| Missing required Fragment | Invocation is rejected before the semantic effect. |
| Authored-Fragment asymmetry | Canonical projection succeeds where permitted; transparent forwarding preserves unknown allowed Fragments. |
| Successful Cooling execution | The foreign provider changes its native state and returns the declared result Shape. |
| Host authority denial | The host records denial and the foreign provider is not invoked. |
| Unknown Constraint | The host fails closed and the foreign provider is not invoked. |
| Failed semantic execution | A declared failed Outcome crosses the boundary without transporting an exception. |
| Provenance | The host can attribute requester, host decision, selected foreign provider, Execution, and returned Outcome without conflating their identifiers. |
| Provider-process failure | Failure domain, interruption, retry decision, and fallback are explicit; success is never fabricated. |
| Host-local Enrichment | Each host can resolve the declared test Fragment independently without treating delivery as authority. |
| Private-type audit | No shared private CLR type, assembly reference, static state, or service container crosses the seam. |

## 7. Verification and evidence ownership

The normal Brontide Reference Stack and Brontide Minimal Stack suites remain hermetic. Cross-process tests use locally built endpoints,
require no credentials, and make no network calls. Test processes are bounded by explicit timeouts
and are terminated on failure.

New independently consumable endpoint or binding components own native unit tests from their first
public behaviour. If non-interactive test consoles are introduced, they are documented and
registered in `AGENTS.md` in the same implementation change as required by the repository rules.

Brontide Reference Stack-owned evidence belongs under `Reference/tests` and `Reference/docs`. Brontide Minimal Stack-owned evidence belongs
under `Minimal/tests` and `Minimal/docs`. Neutral JSON fixtures and their schema or format notes may live
in a root `interchange/` tree because neither implementation owns them. That tree MUST contain data
and documentation only, not a shared Brontide runtime library.

The repeatable final gate is:

```powershell
dotnet restore .\Reference\Brontide.Reference.sln
dotnet build .\Reference\Brontide.Reference.sln --no-restore
dotnet test .\Reference\Brontide.Reference.sln --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Reference\build\verify-dependencies.ps1

dotnet restore .\Minimal\Brontide.Minimal.slnx
dotnet build .\Minimal\Brontide.Minimal.slnx --no-restore
dotnet test .\Minimal\Brontide.Minimal.slnx --no-build
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Minimal\build\verify-boundaries.ps1
```

The complete cross-process command is included here and in both owning READMEs:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

## 8. Work deliberately deferred

The first interchange proof does not include:

- cross-stack Event Distribution or Flow gap recovery;
- Brontide Minimal Stack's Macro Operation or a cross-stack Activity provider;
- live replacement, state handoff during in-progress work, or hot-swap claims;
- a machine boundary or cross-authority-domain Capability federation;
- the decisive mixed-stack image workspace or a third-party provider;
- pooled-buffer or referenced-resource image transport; or
- GPU compilation, dispatch, or fallback.

The intended sequence after P4 is cross-stack Event/Flow evidence, Macro Operation exchange, and
then the mixed-stack image workspace from Architecture 0.7 §30.1. Referenced-resource and
pooled-buffer paths belong with that larger data experiment. GPU execution remains an independent
sideline and cannot substitute for any interchange gate.
