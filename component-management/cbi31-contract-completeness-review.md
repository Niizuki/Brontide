# CBI31 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI31 verified-local-artifact activation contract, separate from its
conformance tests.

## Findings and dispositions

1. **CBI30 had no owner for the executable it launched.** Disposition: closed for one local file.
   Each composition root now verifies a named file, applies an explicit policy, launches it without
   a shell, owns its streams and process tree, and supplies the existing portable conversation.
2. **A digest alone is not activation authority.** Disposition: pinned. A matching digest still
   requires lexical containment beneath the allowed root and an exact admitted argument vector.
   Acquisition and policy refusals start no process.
3. **"Process isolation" could be mistaken for a security sandbox.** Disposition: prevented in the
   contract and docs. `dedicated-process` reports a distinct child process with redirected streams;
   it does not report a restricted token, namespace, container, filesystem boundary, resource
   quota, or other operating-system protection mechanism.
4. **Verification and execution are not atomically bound on every supported operating system.**
   Disposition: bounded to a host-controlled local source. The owner verifies bytes and then starts
   the same canonical path, but CBI31 does not build a content-addressed immutable store or defeat a
   hostile time-of-check/time-of-use replacement. Untrusted sources require a later staging design.
5. **Lexical containment does not establish link integrity.** Disposition: bounded. The policy uses
   canonical relative-path containment rather than an unsafe string prefix, but the allowed-root
   owner remains responsible for reparse-point and symbolic-link integrity. This is another reason
   CBI31 is not a sandbox.
6. **CM1's `StagedArtifact` is evidence, not an executable package.** Disposition: kept separate.
   Its fixture content cannot honestly be reinterpreted as provider bytes. CBI31 introduces a local
   executable acquisition record at the composition root instead of widening the completed fake
   CM1 contract or pretending that the two records are interchangeable.
7. **Cleanup must remain observable after disposal.** Disposition: corrected during implementation.
   The first cleanup test found that `HasExited` queried an already-disposed runtime object. Both
   owners now retain the terminal fact, make disposal idempotent, and expose successful cleanup.
8. **Launch failure must remain contract data.** Disposition: covered. A verified, admitted file
   that the operating system cannot execute returns `provider-process-start-failed`; it does not
   leak the platform exception.
9. **Cross-stack substitution and the ordinary gate must not become optional.** Disposition: reused.
   Both roots consume the same CBI31 vectors, all CBI31 tests are `CrossProcess`, and the CBI30
   completion-gate reruns already execute that category with both provider paths set.

## Result

The CBI31 contract is complete for verified activation of one already-present, host-controlled local
provider executable. It explicitly owns acquisition evidence, launch policy, dedicated process
lifetime, portable retirement, and cleanup without claiming a package manager or security boundary.
The next physical-distribution slice should own content-addressed staging and removal of a declared
multi-file artifact set; only then can acquisition be safely connected to remote or untrusted
sources.
