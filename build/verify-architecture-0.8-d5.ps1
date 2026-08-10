$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$canonicalPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$evidencePath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d5-evidence.json'
$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d5-behavioral-contract.md'
$reviewPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-d5-completeness-review.md'
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Equal($Expected, $Actual, [string]$Message) { if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', found '$Actual'." } }
function Assert-Anchor([object]$Evidence, [string]$Label) {
    $path = Join-Path $repositoryRoot $Evidence.path
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "$Label evidence path is missing: $($Evidence.path)"
    $content = Get-Content -Raw -LiteralPath $path
    Assert-True ($content.IndexOf([string]$Evidence.anchor, [System.StringComparison]::Ordinal) -ge 0) "$Label evidence anchor is missing: $($Evidence.anchor)"
}
$canonical = Get-Content -Raw -LiteralPath $canonicalPath | ConvertFrom-Json
$evidence = Get-Content -Raw -LiteralPath $evidencePath | ConvertFrom-Json
$contract = Get-Content -Raw -LiteralPath $contractPath
Assert-Equal 'A08-D5' $evidence.phase 'A08-D5 phase mismatch.'
Assert-Equal '0.8' $evidence.architectureRevision 'A08-D5 architecture revision mismatch.'
Assert-Equal 'executed-experimental' $evidence.evidenceStatus 'A08-D5 evidence status mismatch.'
Assert-Equal 'Architecture 0.7' $evidence.stackTargetsRemain 'A08-D5 stack-target boundary mismatch.'
$capabilities = @($evidence.capabilities)
Assert-Equal 3 $capabilities.Count 'A08-D5 capability count mismatch.'
foreach ($number in 1..3) {
    $id = "D5-C$number"
    Assert-Equal 1 @($capabilities | Where-Object { $_ -eq $id }).Count "$id capability accounting mismatch."
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the behavioral contract."
    Assert-True ($contract -match "(?s)$([regex]::Escape($id)).*?Property:") "$id has no phase-wide property."
}
$expectedIds = @($canonical.vectors | Where-Object { $_.change -eq 'C10' } | Select-Object -ExpandProperty id | Sort-Object)
$actualIds = @($evidence.vectors.id | Sort-Object)
Assert-Equal 2 $expectedIds.Count 'A08-D5 canonical vector count mismatch.'
Assert-Equal 2 $actualIds.Count 'A08-D5 evidence vector count mismatch.'
Assert-Equal 2 @($actualIds | Sort-Object -Unique).Count 'A08-D5 evidence vectors must be unique.'
Assert-True (($expectedIds -join "`n") -ceq ($actualIds -join "`n")) 'A08-D5 evidence IDs differ from the canonical C10 subset.'
foreach ($vector in $evidence.vectors) {
    foreach ($capability in @($vector.capabilities)) { Assert-True ($capabilities -contains $capability) "$($vector.id) names unknown capability $capability." }
    Assert-Anchor $vector.reference "$($vector.id) Reference"
    Assert-Anchor $vector.minimal "$($vector.id) Minimal"
}
$testPaths = @('Reference\tests\Brontide.Reference.Conformance\Architecture08D5ConformanceTests.cs', 'Minimal\tests\Brontide.Minimal.Conformance\Architecture08D5Tests.fs')
foreach ($testPath in $testPaths) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $testPath)
    foreach ($number in 1..3) { Assert-True ($content -match "D5_C$number") "D5-C$number has no named failing-first test in $testPath." }
}
$surfaceChecks = @(
    @{ Path = 'Reference\src\Brontide.Reference.Core\Interactions.cs'; Anchors = @('DelegateProviderAuthority') },
    @{ Path = 'Reference\src\Brontide.Reference.Experimental.PersistentInformation\PersistentInformation.cs'; Anchors = @('DatasetAuthorityConstraint', 'IssueWithAuthority') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs'; Anchors = @('capabilityDerivationChain') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Experimental.PersistentInformation\PersistentInformation.fs'; Anchors = @('module DatasetAuthority', 'IssueWithAuthority') }
)
foreach ($check in $surfaceChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    foreach ($anchor in $check.Anchors) { Assert-True ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) "A08-D5 surface anchor is missing from $($check.Path): $anchor" }
}
Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'A08-D5 completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'next separately authorized\s+slice is A08-D6') 'A08-D5 completeness review does not preserve the A08-D6 authorization boundary.'
Write-Output 'Architecture 0.8 A08-D5 verification passed: D5-C1 through D5-C3, both canonical C10 vectors, constrained provider derivation, and two independent implementations accounted.'
