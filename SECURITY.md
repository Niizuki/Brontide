# Security policy

Do not report suspected authority bypasses, unsafe deserialization, credential exposure, denial of
service, or dependency vulnerabilities in a public issue. Use GitHub's
[private vulnerability reporting form](https://github.com/Niizuki/Brontide/security/advisories/new).
If private reporting is unavailable, contact the repository owner through the GitHub profile and
request a private channel without including exploit or credential details in the first message.

Include the affected commit, stack and component, threat boundary, minimal reproduction, observed
impact, and whether any real system or secret was involved. Do not test against production systems
or third-party data. Maintainers should acknowledge a private report within seven days, agree a
coordination plan, and publish remediation and credit only with the reporter's consent.

Architecture drafts and explicitly experimental bindings are not automatically production-safe.
Security fixes still preserve fail-closed authority semantics, independent stack boundaries, and
the evidence requirements in [`AGENTS.md`](./AGENTS.md).
