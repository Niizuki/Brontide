# Portable Component Binding — implementation-neutral provider (`binding/neutral-provider/`)

**Status:** PB5 experimental evidence; planned work; not ratified; not part of Brontide Base.
**Plan:** [Portable Component Binding Implementation Plan 0.1](../../docs/future/binding/Brontide-Portable-Component-Binding-Implementation-Plan-0.1.md)
**Contract:** the data-only declarations under [`binding/portable/`](../portable/README.md)

This is a provider endpoint for the Portable Component Binding that **depends on neither stack**. It
exists to discharge C10: that the contract is implementable from its published form, without
importing the Reference or Minimal private model.

## Why it is evidence rather than a third implementation

Three properties do the work, and each is checked rather than asserted:

1. **It imports no Brontide assembly.** The repository's project-graph and assembly-graph guards
   scope themselves to `Reference/` and `Minimal/`, so this endpoint is outside both by
   construction. [`build/verify-portable-binding.ps1`](../../build/verify-portable-binding.ps1)
   therefore reads its resolved `.deps.json` and fails if any `Brontide.*` library appears. It
   resolves two libraries: itself and the base class library's CBOR codec.
2. **It does not restate the contract in source.** The contract document it answers with is
   transcoded at run time from the checked-in neutral declaration. Serving a contract you compiled
   in from a hand-written copy proves that you agree with yourself; serving the published file
   proves the file is sufficient.
3. **It does not use either stack's codec.** The wire is read and written by `System.Formats.Cbor`
   from the base class library. That the two stacks' hand-written deterministic-CBOR cores and an
   off-the-shelf decoder all agree is what makes "the representation is standard CBOR" a fact about
   the representation rather than about the two codecs.

It is deliberately the smallest endpoint that answers the contract honestly. It refuses what the
contract says must be refused, with the portable category the contract names, and it fabricates no
success. It is not a third Brontide implementation, owns no architecture claim, and nothing depends
on it but the tests.

## Running it

The verbs match both stacks' interchange providers, so a host drives it with no host-side change:

```bash
dotnet run --project binding/neutral-provider/PortableBinding.NeutralProvider -- --portable
```

`--portable` serves the Cooling contract; `--portable --catalog` serves the Catalog one. Both hosts
exercise it through `BRONTIDE_NEUTRAL_PROVIDER`, under the `NeutralProvider` test category.

## What building it found

Writing the first consumer that reads the neutral declaration *as published* surfaced something four
phases of hand-written implementations had not. The fixture files carry documentation alongside the
contract — `additiveOver` on a Shape version, `role` on the encoding-edge Shapes — and
[`schemas/component-contract.json`](../portable/schemas/component-contract.json) declares exactly
which fields a contract document has, with `unknownFieldPolicy: reject`. A faithful transcode of the
file was therefore a malformed contract, and both stacks rejected it.

The stacks had never noticed because neither reads the file: each hand-wrote its contract from it and
dropped the annotations by eye. The fixtures now declare their own `annotationFields`, so the
distinction is data rather than a convention someone has to know, and a future annotation has to
declare itself instead of silently becoming a malformed contract.

## Boundary

Both stacks now state Architecture 0.8 as their local implementation target. This provider asserts
no architecture conformance of its own. It is its own build boundary with its own `Directory.Build.props` and
`Directory.Packages.props`, because being buildable without either stack's build files is part of
what it demonstrates.
