# Versioning and compatibility policy

Brontide has four independent version spaces. They must not be substituted for one another.

`Brontide-Architecture-Status.json` is the only generic registry of the current architecture,
latest ratified architecture, and implemented baseline. Other documents link to the registry or
describe their own fixed historical version; they do not independently declare what is latest.

- An Architecture version names one specification document and its ratification status. A later
  draft does not upgrade an implementation claim.
- A vocabulary or Profile version is an additive conformance claim over immutable canonical
  semantics. It is not part of an Operation or Event's canonical identity. Shape and Declared
  Fragment versions are separate positive-integer structural lineages.
- A wire-protocol version identifies one external encoding and negotiation contract. The current
  interchange protocol is an experimental test instrument, so its version does not imply
  Architecture ratification or package compatibility.
- A package or independently distributed component version describes its public API and runtime
  compatibility. Until packages are published, project assemblies remain repository-versioned
  development artifacts and make no independent SemVer stability promise.

Public API removals, signature changes, newly opaque constructors, changed serialized forms, and
changed authority behavior are breaking decisions. The change must identify consumers, give a
migration path, update the independently versioned component's changelog and version when one
exists, and use a breaking `!` commit or PR title plus a `BREAKING CHANGE:` footer. Additive APIs
and fixes do not permit silent reinterpretation of an already ratified canonical name or wire
field.

Architecture compatibility is established by the applicable requirement matrix. Wire
compatibility is established by exact negotiation and fixture vectors. Package/API compatibility
is established by build and test evidence plus an API-baseline tool once packages are introduced.
Passing one kind of gate is never evidence for another.
