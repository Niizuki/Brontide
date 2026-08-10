$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$canonicalPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$evidencePath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d3-evidence.json'
$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d3-behavioral-contract.md'
$migrationPath = Join-Path $repositoryRoot 'docs\future\architecture\architecture-0.8-d3-breaking-migration.md'
$reviewPath = Join-Path $repositoryRoot 'docs\future\architecture\architecture-0.8-d3-completeness-review.md'

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

Assert-Equal 'A08-D3' $evidence.phase 'A08-D3 phase mismatch.'
Assert-Equal '0.8' $evidence.architectureRevision 'A08-D3 architecture revision mismatch.'
Assert-Equal 'executed-experimental-breaking' $evidence.evidenceStatus 'A08-D3 evidence status mismatch.'
Assert-Equal 'Architecture 0.7' $evidence.stackTargetsRemain 'A08-D3 stack-target boundary mismatch.'

$capabilities = @($evidence.capabilities)
Assert-Equal 6 $capabilities.Count 'A08-D3 capability count mismatch.'
foreach ($number in 1..6) {
    $id = "D3-C$number"
    Assert-Equal 1 @($capabilities | Where-Object { $_ -eq $id }).Count "$id capability accounting mismatch."
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the behavioral contract."
}

$expectedIds = @($canonical.vectors | Where-Object { $_.change -in @('C8', 'C9') } | Select-Object -ExpandProperty id | Sort-Object)
$actualIds = @($evidence.vectors.id | Sort-Object)
Assert-Equal 6 $expectedIds.Count 'A08-D3 canonical vector count mismatch.'
Assert-Equal 6 $actualIds.Count 'A08-D3 evidence vector count mismatch.'
Assert-Equal 6 @($actualIds | Sort-Object -Unique).Count 'A08-D3 evidence vectors must be unique.'
Assert-True (($expectedIds -join "`n") -ceq ($actualIds -join "`n")) 'A08-D3 evidence IDs differ from the canonical C8/C9 subset.'

foreach ($vector in $evidence.vectors) {
    foreach ($capability in @($vector.capabilities)) {
        Assert-True ($capabilities -contains $capability) "$($vector.id) names unknown capability $capability."
    }
    Assert-Anchor $vector.reference "$($vector.id) Reference"
    Assert-Anchor $vector.minimal "$($vector.id) Minimal"
}
foreach ($capability in $capabilities) {
    Assert-True (@($evidence.vectors | Where-Object { $_.capabilities -contains $capability }).Count -gt 0) "$capability has no vector evidence."
}

$surfaceChecks = @(
    @{ Path = 'Reference\src\Brontide.Reference.Core\Authority.cs'; Anchors = @('ConstraintDeclaration', 'ConstraintRecognitionDecision', 'ParallelCanonicalName') },
    @{ Path = 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs'; Anchors = @('ConstraintRecognitionSet', 'ValidateAuthorityValue', 'new canonical name') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Model\Model.fs'; Anchors = @('type ConstraintDeclaration', 'ParameterShape: ShapeReference', 'PresentedCommandShape: ShapeReference') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs'; Anchors = @('let constraintRecognitionSet', 'let registerConstraintDeclaration', 'not eligible for additive Shape projection', 'projectPayloadValue') }
)
foreach ($check in $surfaceChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    foreach ($anchor in $check.Anchors) {
        Assert-True ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) "A08-D3 surface anchor is missing from $($check.Path): $anchor"
    }
}

$referenceDomain = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs')
Assert-True ($referenceDomain -match 'strictAuthorityValues\s*\?\s*Shapes\.ValidateAuthorityValue') 'Reference draft-0.8 execution does not enforce exact Constraint value Shapes.'
Assert-True ($referenceDomain -match 'strictAuthorityValues: useStrongKleene') 'Reference execution does not bind strict authority values to the Draft-0.8 evaluator selection.'
Assert-True ($referenceDomain -match 'useStrongKleene: true') 'Reference draft-0.8 execution does not select strict authority values.'

Assert-True (Test-Path -LiteralPath $migrationPath -PathType Leaf) 'A08-D3 breaking migration document is missing.'
$migration = Get-Content -Raw -LiteralPath $migrationPath
Assert-True ($migration -match 'ConstraintRequirement') 'Minimal breaking migration is incomplete.'
Assert-True ($migration -match 'GenesisContext\.Constraint') 'Reference breaking migration is incomplete.'
Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'A08-D3 completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'next separately authorized\s+slice is A08-D4') 'A08-D3 completeness review does not preserve the A08-D4 authorization boundary.'

Write-Output 'Architecture 0.8 A08-D3 verification passed: D3-C1 through D3-C6, six canonical vectors, two-plane migration, and two independent implementations accounted.'
