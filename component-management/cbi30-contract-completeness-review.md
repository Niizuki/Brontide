# CBI30 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI30 process-boundary activation contract, separate from conformance
review.

## Findings and dispositions

1. **The process seam already composes with CBI2.** Disposition: established. The composition roots
   inject the existing negotiated process conversation into the existing activation path; no CM,
   Portable Binding, Channel, or provider protocol surface was added.
2. **Substitution has to cross implementation roots, not only processes.** Disposition: pinned. Each
   host activates against both the Reference and Minimal provider executables and reaches the same
   stable outcome using only the portable contract.
3. **Reference erased a declared process failure.** Disposition: corrected. Reference caught
   `PortableProcessFailureException` under the generic exception fallback and reported
   `portable-interconnection-failed`; Minimal already projected its `Interrupted` case as
   `portable-process-interrupted`. Reference now classifies the declared process failure explicitly,
   in both singleton and group activation paths.
4. **A process boundary is not a distribution system.** Disposition: stated as the primary bound.
   The fake Component Manager remains host-local and the test supplies an already-built executable.
   There is no discovery service, artifact acquisition, verification, staging, launch policy,
   sandbox policy, or operating-system lifecycle owner.
5. **The authority domain does not move with the provider process.** Disposition: stated. CBI30
   proves process isolation inside the existing portable authority rules; it does not authenticate a
   remote domain, transport a Capability, or define Identity/Distributed attestation.
6. **Retirement owns protocol cleanup, not forceful process death.** Disposition: pinned. Successful
   vectors send withdrawal and termination, close the member gate, and observe the provider exit.
   A forceful kill exists only to construct the pre-Interconnection interruption vector and as test
   cleanup if a failed assertion leaves a process alive.
7. **The completion gate must make process evidence non-optional.** Disposition: corrected. The
   ordinary solution runs may skip when provider paths are absent; the repository gate now reruns
   the two composition-root suites with `Category=CrossProcess` after setting both provider paths.
8. **Loss timing remains intentionally narrow.** Disposition: bounded. CBI30 covers loss before
   Interconnection completes. It does not decide interruption during ordinary interaction,
   withdrawal, partial Release, retry, reconnection, or state recovery.

## Result

The CBI30 contract is complete for one direct CBI2 activation over a real provider process and for
cross-stack substitution at that seam. It closes the first executable part of the real-distribution
area without claiming a distribution standard. The next distribution work must own artifacts,
launch/isolation policy, and operating-system lifecycle explicitly; extending CBI30 with more process
timings would not supply those missing capabilities.
