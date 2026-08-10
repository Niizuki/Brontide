# Minimal Architecture 0.7 comparison endpoint

This offline console consumes the shared Architecture 0.7 R5/M5 data-only fixture, invokes the
Minimal stack's public M1–M4 experimental surfaces, and writes canonical JSON observations. It does
not reference or imitate the Reference stack.

Run the owning comparison gate from the repository root:

```powershell
.\build\verify-architecture-0.7-comparison.ps1
```

Direct use takes exactly two arguments: the fixture path and an output path. There are no live
verbs, credentials, network calls, or external targets.
