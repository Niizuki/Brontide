$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$registryPath = Join-Path $repositoryRoot 'conformance\reviews\snapshots\implementation-correction-architecture-status.json'
$architecturePath = Join-Path $repositoryRoot 'docs\future\architecture\Brontide-Architecture-0.8.md'
$vectorPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$requirementsPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-delivery-audit-requirements.json'
$reportPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-delivery-audit-report.md'
$contractPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-delivery-audit-contract.md'
$reviewPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-delivery-audit-completeness-review.md'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', found '$Actual'." }
}

function Read-Json([string]$Path) {
    Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "Missing JSON artifact: $Path"
    return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
}

function Assert-Evidence([object]$Evidence, [string]$Stack, [string]$RequirementId) {
    foreach ($item in $Evidence) {
        Assert-True (-not [string]::IsNullOrWhiteSpace($item.path)) "$Stack $RequirementId has evidence without a path."
        Assert-True (-not [string]::IsNullOrWhiteSpace($item.anchor)) "$Stack $RequirementId has evidence without an anchor."
        $fullPath = Join-Path $repositoryRoot $item.path
        Assert-True (Test-Path -LiteralPath $fullPath -PathType Leaf) "$Stack $RequirementId evidence path does not exist: $($item.path)"
        $content = Get-Content -Raw -LiteralPath $fullPath
        Assert-True ($content.IndexOf([string]$item.anchor, [System.StringComparison]::Ordinal) -ge 0) "$Stack $RequirementId evidence anchor was not found: $($item.anchor)"
    }
}

function Test-Evidence([object]$Evidence) {
    foreach ($item in $Evidence) {
        if ([string]::IsNullOrWhiteSpace($item.path) -or [string]::IsNullOrWhiteSpace($item.anchor)) { return $false }
        $fullPath = Join-Path $repositoryRoot $item.path
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { return $false }
        $content = Get-Content -Raw -LiteralPath $fullPath
        if ($content.IndexOf([string]$item.anchor, [System.StringComparison]::Ordinal) -lt 0) { return $false }
    }
    return $true
}

$registry = Read-Json $registryPath
$vectors = Read-Json $vectorPath
$inventory = Read-Json $requirementsPath

Assert-Equal '0.8' $registry.currentArchitecture.revision 'DA1 registry revision mismatch.'
Assert-Equal $registry.currentArchitecture.revision $inventory.architecture.revision 'DA1 inventory revision mismatch.'
Assert-Equal $registry.currentArchitecture.status $inventory.architecture.status 'DA1 inventory status mismatch.'
Assert-Equal $registry.currentArchitecture.path $inventory.architecture.path 'DA1 inventory path mismatch.'
Assert-Equal $registry.currentArchitecture.sha256 $inventory.architecture.sha256 'DA1 inventory digest mismatch.'
$actualDigest = (Get-FileHash -Algorithm SHA256 -LiteralPath $architecturePath).Hash
Assert-Equal $registry.currentArchitecture.sha256 $actualDigest 'DA1 current architecture digest mismatch.'

$requirements = @($inventory.requirements)
Assert-Equal 14 $requirements.Count 'DA2 requirement count mismatch.'
$requirementIds = @($requirements.id)
Assert-Equal 14 @($requirementIds | Sort-Object -Unique).Count 'DA2 requirement IDs must be unique.'
$changes = @($requirements.change)
foreach ($number in 1..14) {
    Assert-Equal 1 @($changes | Where-Object { $_ -eq "C$number" }).Count "DA2 C$number accounting mismatch."
}

$canonicalVectorIds = @($vectors.vectors.id | Sort-Object)
$inventoryVectorIds = @($requirements.vectors | ForEach-Object { $_ } | Sort-Object)
Assert-Equal 33 $canonicalVectorIds.Count 'DA2 canonical vector count mismatch.'
Assert-Equal 33 $inventoryVectorIds.Count 'DA2 inventory vector count mismatch.'
Assert-Equal 33 @($inventoryVectorIds | Sort-Object -Unique).Count 'DA2 inventory vectors must be unique.'
Assert-True (($canonicalVectorIds -join "`n") -ceq ($inventoryVectorIds -join "`n")) 'DA2 inventory vector IDs differ from the canonical vector inventory.'
Assert-Equal 'coverage.C13' ($requirements | Where-Object change -eq 'C13').coverage 'DA2 C13 coverage mismatch.'
Assert-Equal 'coverage.C14' ($requirements | Where-Object change -eq 'C14').coverage 'DA2 C14 coverage mismatch.'

$allowedStatuses = @($inventory.audit.evidenceStatuses)
Assert-Equal 6 $allowedStatuses.Count 'DA4 status vocabulary count mismatch.'
foreach ($forbidden in @('accepted', 'implemented', 'tested')) {
    Assert-True ($allowedStatuses -notcontains $forbidden) "DA4 forbidden status '$forbidden' appears in the vocabulary."
}

$matrices = @(
    @{ Stack = 'Reference'; Path = Join-Path $repositoryRoot 'Reference\conformance\architecture-0.8-delivery-audit.json' },
    @{ Stack = 'Minimal'; Path = Join-Path $repositoryRoot 'Minimal\conformance\architecture-0.8-delivery-audit.json' }
)

foreach ($matrixEntry in $matrices) {
    $stack = $matrixEntry.Stack
    $matrix = Read-Json $matrixEntry.Path
    Assert-Equal $stack $matrix.stack 'DA3 stack mismatch.'
    Assert-Equal 'Architecture 0.7' $matrix.designedFor "DA4 $stack target mismatch."
    Assert-True ($matrix.acceptanceBoundary -match 'no row is accepted') "DA4 $stack acceptance boundary is missing."
    $rows = @($matrix.requirements)
    Assert-Equal 14 $rows.Count "DA3 $stack row count mismatch."
    Assert-Equal 14 @($rows.requirementId | Sort-Object -Unique).Count "DA3 $stack requirement IDs must be unique."
    Assert-True ((@($rows.requirementId | Sort-Object) -join "`n") -ceq (@($requirements.id | Sort-Object) -join "`n")) "DA3 $stack rows differ from the shared inventory."

    foreach ($row in $rows) {
        Assert-True ($allowedStatuses -contains $row.evidenceStatus) "DA4 $stack $($row.requirementId) has unknown status '$($row.evidenceStatus)'."
        Assert-True (@('accepted', 'implemented', 'tested') -notcontains $row.evidenceStatus) "DA4 $stack $($row.requirementId) promotes audit evidence."
        Assert-Evidence $row.candidateEvidence $stack $row.requirementId
        if ($row.requirementId -eq 'BR-08-DELIVERY-C6' -and -not (Test-Evidence $row.conflictingEvidence)) {
            Assert-True (@($row.postAuditReplacement).Count -gt 0) "DA5 $stack C6 removed its audited conflict but has no post-audit replacement evidence."
            Assert-Evidence $row.postAuditReplacement $stack "$($row.requirementId) post-audit replacement"
        } else {
            Assert-Evidence $row.conflictingEvidence $stack $row.requirementId
        }
        $change = ($requirements | Where-Object id -eq $row.requirementId).change
        $expectedDisposition = if ($change -eq 'C11') { 'attested' } elseif ($change -in @('C13', 'C14')) { 'documentation-only' } else { 'not-executed' }
        Assert-Equal $expectedDisposition $row.vectorDisposition "DA4 $stack $change vector disposition mismatch."
        if ($row.evidenceStatus -in @('candidate-reusable', 'candidate-partial', 'handoff-attested')) {
            Assert-True (@($row.candidateEvidence).Count -gt 0) "DA4 $stack $change requires candidate evidence."
        }
        if ($row.evidenceStatus -eq 'conflicting') {
            Assert-True (@($row.conflictingEvidence).Count -gt 0) "DA5 $stack $change requires conflicting evidence."
        }
    }

    Assert-Equal 'conflicting' ($rows | Where-Object requirementId -eq 'BR-08-DELIVERY-C6').evidenceStatus "DA5 $stack C6 status mismatch."
    Assert-Equal 'conflicting' ($rows | Where-Object requirementId -eq 'BR-08-DELIVERY-C7').evidenceStatus "DA5 $stack C7 status mismatch."
    Assert-Equal 'handoff-attested' ($rows | Where-Object requirementId -eq 'BR-08-DELIVERY-C11').evidenceStatus "DA5 $stack C11 status mismatch."
}

$c7 = $requirements | Where-Object change -eq 'C7'
Assert-Equal 3 @($c7.supersedesRequirements).Count 'DA5 C7 supersession count mismatch.'
foreach ($id in @('BR-07-CONSTRAINT-001', 'BR-07-CONSTRAINT-002', 'BR-07-CONSTRAINT-003')) {
    Assert-True ($c7.supersedesRequirements -contains $id) "DA5 missing C7 supersession: $id"
}

foreach ($path in @($contractPath, $reportPath, $reviewPath)) {
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "Missing delivery-audit document: $path"
}
$contract = Get-Content -Raw -LiteralPath $contractPath
foreach ($id in 1..6) { Assert-True ($contract -match "DA$id") "Delivery-audit contract is missing DA$id." }
$report = Get-Content -Raw -LiteralPath $reportPath
foreach ($slice in 1..6) { Assert-Equal 1 ([regex]::Matches($report, "(?m)^\| A08-D$slice \|").Count) "DA6 A08-D$slice slice accounting mismatch." }
$sliceRows = [regex]::Matches($report, '(?m)^\| A08-D[1-6] \| (?:C[0-9]+(?:, )?)+ \|') | ForEach-Object { $_.Value }
foreach ($change in @(1..10) + 12) {
    Assert-Equal 1 ([regex]::Matches(($sliceRows -join "`n"), "(?<![0-9])C$change(?![0-9])").Count) "DA6 C$change runtime slice accounting mismatch."
}
Assert-True ($report -match 'This queue is an audit output, not runtime authorization') 'DA6 runtime authorization boundary is missing.'
Assert-True ($report -match 'C13 and C14\s+remain outside the runtime queue') 'DA6 architecture-only exclusion is missing.'

Write-Output 'Architecture 0.8 delivery audit verification passed: DA1-DA6, C1-C14, 33 vectors, two independent matrices, and six bounded runtime slices accounted.'
