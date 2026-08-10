$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$canonicalPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$evidencePath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d4-evidence.json'
$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d4-behavioral-contract.md'
$reviewPath = Join-Path $repositoryRoot 'docs\future\architecture\architecture-0.8-d4-completeness-review.md'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', found '$Actual'." }
}

function Assert-Anchor([object]$Evidence, [string]$Label) {
    $path = Join-Path $repositoryRoot $Evidence.path
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "$Label evidence path is missing: $($Evidence.path)"
    $content = Get-Content -Raw -LiteralPath $path
    Assert-True ($content.IndexOf([string]$Evidence.anchor, [System.StringComparison]::Ordinal) -ge 0) "$Label evidence anchor is missing: $($Evidence.anchor)"
}

$canonical = Get-Content -Raw -LiteralPath $canonicalPath | ConvertFrom-Json
$evidence = Get-Content -Raw -LiteralPath $evidencePath | ConvertFrom-Json
$contract = Get-Content -Raw -LiteralPath $contractPath

Assert-Equal 'A08-D4' $evidence.phase 'A08-D4 phase mismatch.'
Assert-Equal '0.8' $evidence.architectureRevision 'A08-D4 architecture revision mismatch.'
Assert-Equal 'executed-experimental' $evidence.evidenceStatus 'A08-D4 evidence status mismatch.'
Assert-Equal 'Architecture 0.7' $evidence.stackTargetsRemain 'A08-D4 stack-target boundary mismatch.'

$capabilities = @($evidence.capabilities)
Assert-Equal 6 $capabilities.Count 'A08-D4 capability count mismatch.'
foreach ($number in 1..6) {
    $id = "D4-C$number"
    Assert-Equal 1 @($capabilities | Where-Object { $_ -eq $id }).Count "$id capability accounting mismatch."
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the behavioral contract."
    Assert-True ($contract -match "(?s)$([regex]::Escape($id)).*?Property:") "$id has no phase-wide property."
}

$expectedIds = @($canonical.vectors | Where-Object { $_.change -in @('C1', 'C5') } | Select-Object -ExpandProperty id | Sort-Object)
$actualIds = @($evidence.vectors.id | Sort-Object)
Assert-Equal 6 $expectedIds.Count 'A08-D4 canonical vector count mismatch.'
Assert-Equal 6 $actualIds.Count 'A08-D4 evidence vector count mismatch.'
Assert-Equal 6 @($actualIds | Sort-Object -Unique).Count 'A08-D4 evidence vectors must be unique.'
Assert-True (($expectedIds -join "`n") -ceq ($actualIds -join "`n")) 'A08-D4 evidence IDs differ from the canonical C1/C5 subset.'

foreach ($vector in $evidence.vectors) {
    foreach ($capability in @($vector.capabilities)) {
        Assert-True ($capabilities -contains $capability) "$($vector.id) names unknown capability $capability."
    }
    Assert-Anchor $vector.reference "$($vector.id) Reference"
    Assert-Anchor $vector.minimal "$($vector.id) Minimal"
}

$surfaceChecks = @(
    @{ Path = 'Reference\src\Brontide.Reference.Core\Authority.cs'; Anchors = @('ExecutionRateLimitConstraint', 'ChainOccurrencePooling', 'LivenessLeaseConstraint') },
    @{ Path = 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs'; Anchors = @('QuantifiedConstraintOccurrence', 'pendingAccounting', '_accountingGate') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Model\Model.fs'; Anchors = @('type LivenessLeaseReference', 'executionRateLimitConstraintName', 'type LivenessLease') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs'; Anchors = @('let livenessLeaseConstraint', 'let executionRateLimitConstraint', 'QuantifiedConstraintOccurrence', 'pendingAccounting') }
)
foreach ($check in $surfaceChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    foreach ($anchor in $check.Anchors) {
        Assert-True ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) "A08-D4 surface anchor is missing from $($check.Path): $anchor"
    }
}

Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'A08-D4 completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'next separately authorized\s+slice is A08-D5') 'A08-D4 completeness review does not preserve the A08-D5 authorization boundary.'

Write-Output 'Architecture 0.8 A08-D4 verification passed: D4-C1 through D4-C6, six canonical vectors, liveness, pooled accounting, and two independent implementations accounted.'
