# .NET SDK support policy

Brontide targets `net10.0` and deliberately has no `global.json`. The selected SDK must be at least
10.0.100 and lower than 11.0.0. The machine-readable policy is
[`eng/sdk-policy.json`](../eng/sdk-policy.json). CI runs the exact first .NET 10 SDK (`10.0.100`)
and resolves the current .NET 10 channel (`10.0.x`) at preview quality, so the supported range is
exercised without pinning the moving lane to one patch. Prerelease SDKs inside that range are
accepted for local architecture development; release evidence must name the exact SDK reported by
the gate.

The Minimal stack currently includes one selected-SDK workaround: for F# projects only,
`Minimal/Directory.Build.props` copies `FSharp.Core.dll` from the active SDK's
`$(MSBuildToolsPath)\FSharp` directory into runtime output. The path is deliberately relative to the
already selected SDK, is conditional on that file existing, and never names a machine or SDK
version. Remove the item when every supported CI feature band copies an equivalent runtime asset
without it; verification must then prove Minimal applications and tests start on a clean machine.

Run the policy check from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build\verify-sdk.ps1
```
