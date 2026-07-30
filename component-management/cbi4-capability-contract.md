# CBI4 canonical integrated-profile comparison capability contract

Status: experimental behavioral contract

Designed for: Brontide Architecture 0.8 §18.1, §20.1, and §24, Complete Draft, not ratified

CBI4 projects native CBI3 results from Reference Studio and Minimal Host into one canonical,
implementation-neutral JSON profile and compares each with shared data-only expected profiles. It
uses no shared runtime library and adds no process protocol. CM6 continues to own complete CM5
process comparison; CBI4 composes CM6's complete canonical authority profile with the
integration-specific lifecycle and portable observations CBI3 actually relies on.

## C1 — the shared vectors force the same integration questions

The neutral fixture names the scenario, expected active/refused result, and expected canonical
profile digest for admitted activation, authority denial, unsupported authority shape, mapping
refusal, and portable lifecycle refusal. Each stack constructs and executes the scenario with its
own native CBI3 types.

Property: deleting either native implementation leaves no executable comparison behavior in the
shared fixture tree.

## C2 — the complete CM5 observation remains parity-relevant

When CBI3 evaluated CM5, the profile carries the SHA-256 of the complete canonical CM6 authority
profile, including every evidence, relationship, authority, grant, policy-mistake, failure, and
decision-log field. A missing authority evaluation is explicit null.

Property: changing any canonical CM5 observation field changes the CBI4 profile digest.

## C3 — the CBI3 decision is explicit

The profile records active/refused, and either null or the exact integration failure kind and code.
Failure reason prose is excluded because the stable machine code already identifies the boundary
and wording is not comparison semantics.

Property: no refused CBI3 result can serialize as active, and no failure kind or code can disappear
from a refused profile.

## C4 — lifecycle evidence covers the authority-to-release boundary

When CBI2 ran, the profile records the CM4 runtime outcome and failure category, every CM4 effect
flag, the CBI2 failure kind and code, and the portable member's stage, Ready and Released states.
Absent runtime, member, or lifecycle results are explicit null.

Property: every difference that could change CBI3's Active decision changes the profile.

## C5 — stable portable facts are canonical

The profile records every CBI1 resolution fact and every fixed PB7 Binding Plan fact in ordinal
name order. The locally generated `planId` is excluded because it is a correlation identity rather
than portable semantics; every other plan fact remains parity-relevant.

Property: adding, removing, or changing any stable resolution or Binding Plan fact changes the
profile digest.

## C6 — canonical JSON is byte-stable

Profiles use schema version 1, fixed property order, lower-case tokens, explicit nulls, UTF-8,
ordinal fact ordering, and compact JSON. SHA-256 is computed over those exact UTF-8 bytes and
rendered as lower-case hexadecimal.

Property: repeated projection of one immutable result is byte-identical and has the same digest.

## C7 — both stacks remain independent

Reference Studio and Minimal Host implement the projection separately over their own native
results. The only shared inputs are the data-only vector fixture and the existing portable and CM6
contracts.

Property: neither stack references the other's assembly, serializer, runtime type, or private
implementation.

## C8 — evidence remains bounded

Equal CBI4 digests prove agreement only on the shared CBI3 vectors and selected canonical profile.
They do not prove cross-process integrated execution, general substitutability, contract
completeness, authority correctness, real distribution, or Architecture 0.8 conformance.

Property: every CBI4 status statement preserves this limitation.
