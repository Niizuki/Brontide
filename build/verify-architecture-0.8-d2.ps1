$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$canonicalPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$evidencePath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d2-evidence.json'
$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d2-behavioral-contract.md'
$migrationPath = Join-Path $repositoryRoot 'docs\current\architecture\architecture-0.8-d2-breaking-migration.md'
$reviewPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-d2-completeness-review.md'

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

Assert-Equal 'A08-D2' $evidence.phase 'A08-D2 phase mismatch.'
Assert-Equal '0.8' $evidence.architectureRevision 'A08-D2 architecture revision mismatch.'
Assert-Equal 'executed-experimental-breaking' $evidence.evidenceStatus 'A08-D2 evidence status mismatch.'
Assert-Equal 'Architecture 0.7' $evidence.stackTargetsRemain 'A08-D2 stack-target boundary mismatch.'

$capabilities = @($evidence.capabilities)
Assert-Equal 5 $capabilities.Count 'A08-D2 capability count mismatch.'
foreach ($number in 1..5) {
    $id = "D2-C$number"
    Assert-Equal 1 @($capabilities | Where-Object { $_ -eq $id }).Count "$id capability accounting mismatch."
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the behavioral contract."
}

$expectedIds = @($canonical.vectors | Where-Object { $_.change -in @('C2', 'C6') } | Select-Object -ExpandProperty id | Sort-Object)
$actualIds = @($evidence.vectors.id | Sort-Object)
Assert-Equal 4 $expectedIds.Count 'A08-D2 canonical vector count mismatch.'
Assert-Equal 4 $actualIds.Count 'A08-D2 evidence vector count mismatch.'
Assert-Equal 4 @($actualIds | Sort-Object -Unique).Count 'A08-D2 evidence vectors must be unique.'
Assert-True (($expectedIds -join "`n") -ceq ($actualIds -join "`n")) 'A08-D2 evidence IDs differ from the canonical C2/C6 subset.'

foreach ($vector in $evidence.vectors) {
    foreach ($capability in @($vector.capabilities)) {
        Assert-True ($capabilities -contains $capability) "$($vector.id) names unknown capability $capability."
    }
    Assert-Anchor $vector.reference "$($vector.id) Reference"
    Assert-Anchor $vector.minimal "$($vector.id) Minimal"
}
Assert-Anchor $evidence.phaseProperties.reference 'A08-D2 Reference phase property'
Assert-Anchor $evidence.phaseProperties.minimal 'A08-D2 Minimal phase property'
foreach ($capability in $capabilities) {
    Assert-True (@($evidence.vectors | Where-Object { $_.capabilities -contains $capability }).Count -gt 0) "$capability has no vector evidence."
}

$surfaceChecks = @(
    @{ Path = 'Reference\src\Brontide.Reference.Core\Authority.cs'; Anchors = @('DelegationDepthConstraint', 'OriginCeilingConstraint', 'Append<ConstraintExpression>(new OriginCeilingConstraint') },
    @{ Path = 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs'; Anchors = @('StandardConstraintNames.DelegationDepth', 'StandardConstraintNames.OriginCeiling', 'ConstraintCapability') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Model\Model.fs'; Anchors = @('type OriginClass', 'type Draft08ExecutionRequest', 'delegationDepthConstraintName', 'originCeilingConstraintName') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs'; Anchors = @('maximum additional derivation links', 'ConstraintCapability', 'RequestedOrigin', 'let stepDraft08') }
)
foreach ($check in $surfaceChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    foreach ($anchor in $check.Anchors) {
        Assert-True ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) "A08-D2 surface anchor is missing from $($check.Path): $anchor"
    }
}

$referenceAuthority = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Core\Authority.cs')
$referenceDomain = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs')
$minimalModel = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Model\Model.fs')
$minimalKernel = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs')
Assert-True ($referenceAuthority -notmatch 'DelegationAllowed') 'Reference still exposes the removed DelegationAllowed field.'
Assert-True ($referenceDomain -notmatch '\bdelegable\b') 'Reference still exposes a delegable issuance argument.'
Assert-True ($minimalModel -notmatch 'DelegationAllowed') 'Minimal still exposes the removed DelegationAllowed field.'
Assert-True ($minimalKernel -notmatch 'delegationAllowed') 'Minimal still exposes a delegationAllowed issuance argument.'

Assert-True (Test-Path -LiteralPath $migrationPath -PathType Leaf) 'A08-D2 breaking migration document is missing.'
$migration = Get-Content -Raw -LiteralPath $migrationPath
Assert-True ($migration -match 'Replace `delegable: false`') 'Reference breaking migration is incomplete.'
Assert-True ($migration -match 'Remove the Boolean argument') 'Minimal breaking migration is incomplete.'
Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'A08-D2 completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'next separately authorized\s+slice is A08-D3') 'A08-D2 completeness review does not preserve the A08-D3 authorization boundary.'

Write-Output 'Architecture 0.8 A08-D2 verification passed: D2-C1 through D2-C5, four canonical vectors, breaking migration, and two independent implementations accounted.'
