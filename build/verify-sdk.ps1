$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$policyPath = Join-Path $repositoryRoot 'eng\sdk-policy.json'
$policy = Get-Content -Raw -LiteralPath $policyPath -Encoding UTF8 | ConvertFrom-Json
$failures = [System.Collections.Generic.List[string]]::new()

$selectedText = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet --version failed.'
}

$coreText = $selectedText.Split('-', 2)[0]
try {
    $selected = [version]$coreText
    $minimum = [version]$policy.minimumInclusive
    $maximum = [version]$policy.maximumExclusive
}
catch {
    throw "The selected SDK or SDK policy is not a parseable version: $($_.Exception.Message)"
}

if ($selected -lt $minimum -or $selected -ge $maximum) {
    $failures.Add("Selected SDK $selectedText is outside [$minimum, $maximum).")
}

$isPrerelease = $selectedText.Contains('-')
if ($isPrerelease -and -not $policy.allowPrerelease) {
    $failures.Add("Selected SDK $selectedText is prerelease but the policy disallows prerelease SDKs.")
}

$globalJson = Get-ChildItem -Path $repositoryRoot -Recurse -File -Filter 'global.json' |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
if ($globalJson) {
    $failures.Add("The repository intentionally follows a supported range and must not add global.json: $($globalJson.FullName -join ', ').")
}

$propsPath = Join-Path $repositoryRoot 'Minimal\Directory.Build.props'
$props = Get-Content -Raw -LiteralPath $propsPath -Encoding UTF8
if ($props -notmatch [regex]::Escape('$(MSBuildToolsPath)\FSharp\FSharp.Core.dll') -or
    $props -notmatch 'BrontideEnableSdkFSharpCoreWorkaround') {
    $failures.Add('The Minimal FSharp.Core workaround is not selected-SDK-relative, bounded, and removable.')
}

$workflowPath = Join-Path $repositoryRoot '.github\workflows\ci.yml'
if (Test-Path -LiteralPath $workflowPath) {
    $workflow = Get-Content -Raw -LiteralPath $workflowPath -Encoding UTF8
    foreach ($featureBand in @($policy.ciFeatureBands)) {
        if ($workflow.IndexOf([string]$featureBand, [StringComparison]::Ordinal) -lt 0) {
            $failures.Add("CI does not exercise declared SDK feature band '$featureBand'.")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "SDK policy verification passed for selected SDK $selectedText in [$minimum, $maximum)."
