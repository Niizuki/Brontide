$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$canonicalPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$evidencePath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d1-evidence.json'
$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-d1-behavioral-contract.md'
$reviewPath = Join-Path $repositoryRoot 'docs\future\architecture\architecture-0.8-d1-completeness-review.md'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', found '$Actual'." }
}

function Assert-Anchor([object]$Evidence, [string]$VectorId, [string]$Stack) {
    $path = Join-Path $repositoryRoot $Evidence.path
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "$VectorId $Stack evidence path is missing: $($Evidence.path)"
    $content = Get-Content -Raw -LiteralPath $path
    Assert-True ($content.IndexOf([string]$Evidence.anchor, [System.StringComparison]::Ordinal) -ge 0) "$VectorId $Stack evidence anchor is missing: $($Evidence.anchor)"
}

$canonical = Get-Content -Raw -LiteralPath $canonicalPath | ConvertFrom-Json
$evidence = Get-Content -Raw -LiteralPath $evidencePath | ConvertFrom-Json
$contract = Get-Content -Raw -LiteralPath $contractPath

Assert-Equal 'A08-D1' $evidence.phase 'A08-D1 phase mismatch.'
Assert-Equal '0.8' $evidence.architectureRevision 'A08-D1 architecture revision mismatch.'
Assert-Equal 'executed-experimental' $evidence.evidenceStatus 'A08-D1 evidence status mismatch.'
Assert-Equal 'Architecture 0.7' $evidence.stackTargetsRemain 'A08-D1 stack-target boundary mismatch.'

$capabilities = @($evidence.capabilities)
Assert-Equal 5 $capabilities.Count 'A08-D1 capability count mismatch.'
foreach ($number in 1..5) {
    $id = "D1-C$number"
    Assert-Equal 1 @($capabilities | Where-Object { $_ -eq $id }).Count "$id capability accounting mismatch."
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the behavioral contract."
}

$expectedIds = @($canonical.vectors | Where-Object { $_.change -in @('C3', 'C4', 'C7') } | Select-Object -ExpandProperty id | Sort-Object)
$actualIds = @($evidence.vectors.id | Sort-Object)
Assert-Equal 11 $expectedIds.Count 'A08-D1 canonical vector count mismatch.'
Assert-Equal 11 $actualIds.Count 'A08-D1 evidence vector count mismatch.'
Assert-Equal 11 @($actualIds | Sort-Object -Unique).Count 'A08-D1 evidence vectors must be unique.'
Assert-True (($expectedIds -join "`n") -ceq ($actualIds -join "`n")) 'A08-D1 evidence IDs differ from the canonical C3/C4/C7 subset.'

foreach ($vector in $evidence.vectors) {
    Assert-True (@($vector.capabilities).Count -gt 0) "$($vector.id) has no capability attribution."
    foreach ($capability in @($vector.capabilities)) {
        Assert-True ($capabilities -contains $capability) "$($vector.id) names unknown capability $capability."
    }
    Assert-Anchor $vector.reference $vector.id 'Reference'
    Assert-Anchor $vector.minimal $vector.id 'Minimal'
}
foreach ($capability in $capabilities) {
    Assert-True (@($evidence.vectors | Where-Object { $_.capabilities -contains $capability }).Count -gt 0) "$capability has no vector evidence."
}

$entryPointChecks = @(
    @{ Path = 'Reference\src\Brontide.Reference.Core\Authority.cs'; Anchors = @('EvaluateStrongKleene', 'public static ConstraintExpressionEvaluation Evaluate(') },
    @{ Path = 'Reference\src\Brontide.Reference.Core\AuthorityDomain.cs'; Anchors = @('ExecuteDraft08Async', 'ExecuteAsync') },
    @{ Path = 'Reference\src\Brontide.Reference.Experimental.Composition\CompositionModel.cs'; Anchors = @('FilterDraft08', 'public static DefinitionConstraintSelectionResult<T> Filter<T>') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Model\Model.fs'; Anchors = @('evaluateStrongKleene', 'let rec evaluate') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Kernel\Kernel.fs'; Anchors = @('stepDraft08', 'let step ') },
    @{ Path = 'Minimal\src\Brontide.Minimal.Experimental.Composition\Composition.fs'; Anchors = @('filterDraft08', 'let filter') }
)
foreach ($check in $entryPointChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    foreach ($anchor in $check.Anchors) {
        Assert-True ($content.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) "A08-D1 entry-point anchor is missing from $($check.Path): $anchor"
    }
}

$legacyChecks = @(
    @{ Path = 'Reference\tests\Brontide.Reference.Core.Tests\ConstraintExpressionTests.cs'; Anchor = 'BR_07_CONSTRAINT_001' },
    @{ Path = 'Reference\tests\Brontide.Reference.Studio.Tests\Architecture07ConstraintSelectionTests.cs'; Anchor = 'BR_07_CONSTRAINT_003' },
    @{ Path = 'Minimal\tests\Brontide.Minimal.Kernel.Tests\KernelTests.fs'; Anchor = 'BR_07_CONSTRAINT_001' },
    @{ Path = 'Minimal\tests\Brontide.Minimal.Composition.Tests\CompositionTests.fs'; Anchor = 'BR_07_CONSTRAINT_003' }
)
foreach ($check in $legacyChecks) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $check.Path)
    Assert-True ($content.IndexOf($check.Anchor, [System.StringComparison]::Ordinal) -ge 0) "Retained 0.7 evidence anchor is missing: $($check.Anchor)"
}

Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'A08-D1 completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'next separately\s+authorized slice is A08-D2') 'A08-D1 completeness review does not preserve the A08-D2 authorization boundary.'

Write-Output 'Architecture 0.8 A08-D1 verification passed: D1-C1 through D1-C5, 11 canonical vectors, two independent implementations, and retained 0.7 entry points accounted.'
