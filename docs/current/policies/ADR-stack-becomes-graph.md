# ADR — "Stack" becomes "Graph"

**Date:** 2026-07-26
**Status:** Accepted; execution deferred and opportunistic

## Context

This repository uses **Stack** for one project's implementation of Brontide: the Brontide Reference
Stack (C#) and the Brontide Minimal Stack (F#). The word appears roughly 600 times in live
documents, twice as a field name in `Brontide-Architecture-Status.json`, and in nine verifier
scripts.

Two things made the word worth revisiting.

The first is that [`Brontide: The Idea`](../overview/Brontide-Introduction.md) now names
**Constellation** for the set of Components a particular system is built from, and **Nucleus** for
the part of it that system declares central. Constellation was chosen partly *because* it carries no
ordering: a system's Components sit alongside one another rather than in strata. Keeping "Stack" for
the adjacent concept leaves a layered word next to a deliberately unlayered one.

The second is that the thing "Stack" names is not a stack. `Brontide.Reference.Core` has no project
dependency; extensions, vocabularies, and experiments depend only on Core; Studio is the composition
root. `Brontide.Minimal.Model` has no project dependency, `Brontide.Minimal.Kernel` depends only on
Model, and Host is the composition root. That is a directed acyclic graph, and this repository
already says so in its own tooling: `AGENTS.md` requires that project graphs stay acyclic, and
[`verify-project-graph.ps1`](../../../build/verify-project-graph.ps1) and
[`verify-assembly-graph.ps1`](../../../build/verify-assembly-graph.ps1) verify exactly that
structure under exactly that name.

## Decision

**Stack becomes Graph.** The Brontide Reference Stack becomes the Brontide Reference Graph, and the
Brontide Minimal Stack becomes the Brontide Minimal Graph.

The rename is not executed now. It is recorded now so that the direction is decided rather than
rediscovered, and so that the transition period below is explained rather than mysterious.

## Why Graph

- It is accurate. The referent is a dependency-directed acyclic graph, and the repository already
  verifies it as one.
- It is not an invention. "Graph" is promoted from vocabulary already in use for this exact
  structure rather than introduced from outside.
- It removes the layer-cake reading. A graph has direction without strata, which is the property
  that made "Stack" misleading next to Constellation.

## What this accepts

A graph is a structure, not an actor. Sentences in which the referent *does* something read less
naturally: an implementation passes a conformance matrix, hosts a provider, and claims a Profile,
where a graph is a shape. Prose should keep saying "the Reference Graph's evidence" and similar
rather than attributing agency to the graph itself where that reads badly.

"Graph" is also heavily overloaded outside this repository — object graph, scene graph, graph
database, GraphQL. Inside it, the word is currently clean and means only dependency structure.

## Alternatives considered

- **Implementation.** `AGENTS.md` already opens by calling the two "deliberately independent .NET 10
  implementations", and *reference implementation* is a term of art. Rejected because
  "implementation" is also this repository's most common ordinary noun — implementation target,
  plan, evidence, findings — so the proper noun would compete with itself on nearly every page.
- **Constellation for both.** Rejected because it would give one word a source and an assembly
  sense, which is the ambiguity this direction exists to remove. A Graph is produced by one project;
  a Constellation is assembled by a system builder and may draw on several Graphs and on parts
  Brontide never defined.
- **Keeping Stack.** Rejected on the two grounds in Context, but it is worth recording that the cost
  of the word was low: no live document uses "stack" in the loose "technology stack of a system"
  sense, so the layered image was never applied to a system.

## Execution

The rename buys clarity, not correctness, so it must not spend a fresh independent review window of
its own. It rides along instead:

1. **Unpinned documents and scripts** may be renamed in one dedicated change whenever convenient.
2. **SHA-256 pinned documents** — eleven contain the word, including Architecture 0.8 with 61
   occurrences — are renamed only when they are already being repinned for substantive reasons, so
   the terminology change reuses that authorized repinning and review window rather than requesting
   one.
3. **The `"stack"` field in `Brontide-Architecture-Status.json`** is a schema change consumed by nine
   verifiers. It moves with a `schemaVersion` bump, not on its own.
4. **`docs/archive/` is never rewritten.** It records completed work in the vocabulary that work
   used. Archived documents will continue to say Stack, correctly.

A mixed vocabulary is therefore expected for as long as this takes, with newer material saying Graph
and pinned or archived material saying Stack. That is the intended state, not drift, and this record
is what makes it legible.

## Relationship to Constellation

The two terms answer different questions and the Linux analogy is close enough to be useful:

- A **Graph** is an upstream project's body of software, coherent and versioned together — closer to
  what a single project ships than to what a user installs.
- A **Constellation** is closer to a distribution: assembled from sources, named, and shipped as a
  usable whole, with an opinion about what is essential recorded as its Nucleus.

One Constellation may draw on several Graphs. A Graph never contains a Constellation.
