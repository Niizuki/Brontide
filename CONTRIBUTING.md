# Contributing to Brontide

Brontide keeps two implementations independent so disagreements remain useful architecture
evidence. Before changing behavior, read [`AGENTS.md`](./AGENTS.md), the active architecture
document for design, the implemented-baseline plan for implementation claims, and the temporary
correction plan while it exists.

Open an issue or discussion with the maintainers before making a substantial contribution,
especially for public APIs, licensing, authority semantics, wire contracts, or cross-stack work.
The repository currently grants no open-source license, so do not assume that submitting code
changes the rights stated in [`LICENSE`](./LICENSE). Maintainers must agree contribution terms
before accepting outside work.

Keep changes in their owning stack, add the nearest NUnit evidence, preserve warnings-as-errors,
and do not introduce a project or assembly reference between stacks. Run the complete repository
gate for shared contracts or cross-stack work:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-interchange.ps1
```

Describe the Architecture requirement IDs affected, the implementation status achieved, any
experimental boundary, the exact tests run, and deliberately deferred work. Public breaking
changes follow [`VERSIONING.md`](./VERSIONING.md).
