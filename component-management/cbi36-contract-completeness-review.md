# CBI36 contract-completeness review

Date: 2026-08-03

Scope: absence review of the CBI36 trust-gated acquisition contract, separate from conformance tests.

## Findings and dispositions

1. **A caller could forge the CBI35 authorization record.** Disposition: corrected and pinned. Both
   public constructors are now inaccessible; callers obtain the value from the CBI35 evaluator.
2. **A malformed request could be hashed or delegated.** Disposition: prevented. Canonical request
   validation precedes trust matching and returns `acquisition-invalid` without source access.
3. **Mutable Reference collections could change between matching and acquisition.** Disposition:
   closed. Reference snapshots files and arguments once before validation, digesting, and delegation;
   Minimal inputs are immutable lists.
4. **Matching only the CBI32 content identity would omit CBI33 lengths.** Disposition: prevented. The
   authorization must also match the CBI34 payload digest covering paths, digests, lengths,
   executable, and arguments.
5. **A refused trust decision could still query the source.** Disposition: prevented and observed.
   Missing and mismatched authorization paths read neither source identity nor any member.
6. **Trust success could hide later transport or integrity failure.** Disposition: prevented. Trust,
   transport, and admission codes remain independent; successful delivery may still fail CBI32.
7. **Private construction is not a defense against unrestricted reflection.** Disposition: bounded.
   CBI36 is a trusted same-process composition boundary, not a hostile-code sandbox or CLR security
   boundary. Authorization is neither serializable nor accepted across a process boundary.
8. **A policy can change after authorization issuance.** Disposition: bounded. The authorization
   names its exact policy snapshot, but CBI36 performs no freshness or current-policy lookup and does
   not retroactively cancel staged or active content.
9. **The gate does not authenticate a source or bound wall-clock time.** Disposition: intentional.
   It preserves CBI33 source attribution and byte limits; networking, credentials, retries, and time
   budgets remain outside this synchronous injected-source boundary.

## Result

The CBI36 contract is complete for same-process trust-gated CBI33 acquisition with exact content and
payload matching before source access. The next security boundary should establish provenance and
monotonic update authority for the host-selected trust-policy snapshot, including how newer
revocations supersede outstanding authorizations, before claiming production remote distribution.
