$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Equal($Expected, $Actual, [string]$Message) { if ($Expected -ne $Actual) { throw "$Message Expected '$Expected', found '$Actual'." } }
function Read-Json([string]$Path) { Assert-True (Test-Path -LiteralPath $Path -PathType Leaf) "Missing closure artifact: $Path"; Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json }
function Assert-Anchor([object]$Evidence, [string]$Label) {
    $path = Join-Path $repositoryRoot ([string]$Evidence.path)
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "$Label path is missing: $($Evidence.path)"
    $content = Get-Content -Raw -LiteralPath $path
    Assert-True ($content.IndexOf([string]$Evidence.anchor, [StringComparison]::Ordinal) -ge 0) "$Label anchor is missing: $($Evidence.anchor)"
}

$contractPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-closure-contract.md'
$reviewPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-closure-completeness-review.md'
$contract = Get-Content -Raw -LiteralPath $contractPath
foreach ($number in 1..5) {
    $id = "CL-C$number"
    Assert-True ($contract -match [regex]::Escape($id)) "$id is missing from the closure contract."
    Assert-True ($contract -match "(?s)$([regex]::Escape($id)).*?Property:") "$id has no phase-wide property."
}

$registry = Read-Json (Join-Path $repositoryRoot 'Brontide-Architecture-Status.json')
Assert-Equal '0.8' $registry.currentArchitecture.revision 'CL-C1 current architecture mismatch.'
Assert-True ($registry.currentArchitecture.status -match 'implementation evidence complete') 'CL-C1 current status does not record complete implementation evidence.'
Assert-True ($registry.currentArchitecture.status -match 'not ratified') 'CL-C5 current status lost the ratification boundary.'
Assert-Equal 'none' $registry.latestRatifiedArchitecture.status 'CL-C5 must not invent a ratified architecture.'
$architecturePath = Join-Path $repositoryRoot ([string]$registry.currentArchitecture.path)
Assert-True ($registry.currentArchitecture.path -eq 'docs/current/architecture/Brontide-Architecture-0.8.md') 'CL-C1 Architecture 0.8 is not classified as current.'
Assert-Equal $registry.currentArchitecture.sha256 (Get-FileHash -Algorithm SHA256 -LiteralPath $architecturePath).Hash 'CL-C1 architecture hash mismatch.'

$requirements = Read-Json (Join-Path $repositoryRoot 'conformance\architecture-0.8-requirements.json')
Assert-Equal '0.8' $requirements.architecture.revision 'CL-C2 requirements revision mismatch.'
foreach ($field in @('revision', 'status', 'path', 'sha256')) { Assert-Equal $registry.currentArchitecture.$field $requirements.architecture.$field "CL-C2 requirements architecture $field mismatch." }
$rows = @($requirements.requirements)
Assert-Equal 14 $rows.Count 'CL-C2 requirement count mismatch.'
foreach ($number in 1..14) { Assert-Equal 1 @($rows | Where-Object change -eq "C$number").Count "CL-C2 C$number accounting mismatch." }
$canonical = Read-Json (Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json')
$expectedVectors = @($canonical.vectors.id | Sort-Object)
$actualVectors = @($rows.vectors | ForEach-Object { $_ } | Sort-Object)
Assert-Equal 33 $actualVectors.Count 'CL-C2 runtime-vector count mismatch.'
Assert-True (($expectedVectors -join "`n") -ceq ($actualVectors -join "`n")) 'CL-C2 runtime vectors differ from the canonical inventory.'

foreach ($stack in @('Reference', 'Minimal')) {
    $implementation = @($registry.implementations | Where-Object stack -eq $stack)
    Assert-Equal 1 $implementation.Count "CL-C1 $stack registry entry mismatch."
    $implementation = $implementation[0]
    Assert-Equal '0.8' $implementation.designedFor "CL-C1 $stack designedFor mismatch."
    Assert-Equal 'conformance/architecture-0.8-requirements.json' $implementation.currentDelivery.requirements.path "CL-C2 $stack requirements path mismatch."
    $matrixRelative = "$stack/conformance/architecture-0.8.json"
    Assert-Equal $matrixRelative $implementation.currentDelivery.matrix.path "CL-C2 $stack matrix path mismatch."
    $matrixPath = Join-Path $repositoryRoot $matrixRelative
    $matrix = Read-Json $matrixPath
    Assert-Equal $stack $matrix.stack "CL-C2 $stack matrix identity mismatch."
    Assert-Equal 'Architecture 0.8' $matrix.designedFor "CL-C1 $stack matrix target mismatch."
    $matrixRows = @($matrix.requirements)
    Assert-Equal 14 $matrixRows.Count "CL-C2 $stack matrix requirement count mismatch."
    foreach ($requirement in $rows) {
        $entry = @($matrixRows | Where-Object requirementId -eq $requirement.id)
        Assert-Equal 1 $entry.Count "CL-C2 $stack $($requirement.id) accounting mismatch."
        foreach ($evidence in @($entry[0].positiveEvidence) + @($entry[0].negativeEvidence)) { Assert-Anchor $evidence "$stack $($requirement.id)" }
        if ($entry[0].status -eq 'tested') {
            Assert-True (@(@($entry[0].positiveEvidence) + @($entry[0].negativeEvidence) | Where-Object path -match "^$stack/tests/").Count -gt 0) "CL-C2 $stack $($requirement.id) has no native test evidence."
        }
    }
}

$reviewRequest = Read-Json (Join-Path $repositoryRoot 'conformance\reviews\review-request.json')
$pinnedRegistryPath = Join-Path $repositoryRoot 'conformance\reviews\snapshots\implementation-correction-architecture-status.json'
Assert-True (Test-Path -LiteralPath $pinnedRegistryPath -PathType Leaf) 'CL-C4 pinned review registry snapshot is missing.'
Assert-Equal $reviewRequest.architectureStatusRegistry.sha256 (Get-FileHash -Algorithm SHA256 -LiteralPath $pinnedRegistryPath).Hash 'CL-C4 pinned review registry snapshot hash mismatch.'

foreach ($readme in @('Reference\README.md', 'Minimal\README.md')) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot $readme)
    Assert-True ($content -match 'Designed for:\*\*.*Architecture 0\.8') "CL-C1 $readme is not retargeted."
    Assert-True ($content -match 'not ratified') "CL-C5 $readme loses the ratification boundary."
}

$retained = @(
    @{ Path = 'conformance\architecture-0.7-requirements.json'; Hash = 'F8C2B9F3D0B3DA280EF5CF8E62A096DB693F1BF70B66252BB90EF0EE9B810DF9' },
    @{ Path = 'Reference\conformance\architecture-0.7.json'; Hash = 'C0FCA1AE7583AF83E4A7F552B754D8CB9E6C6D49F600DD9032596B9437AE0A1C' },
    @{ Path = 'Minimal\conformance\architecture-0.7.json'; Hash = 'DAF46C01D3392CA1B0C1694462EA0F0DA9CAB9B0706577C877CC1EF1C80C706F' }
)
foreach ($item in $retained) {
    $path = Join-Path $repositoryRoot $item.Path
    Assert-True (Test-Path -LiteralPath $path -PathType Leaf) "CL-C3 retained 0.7 evidence is missing: $($item.Path)"
    Assert-Equal $item.Hash (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash "CL-C3 retained 0.7 evidence changed: $($item.Path)"
}
Assert-True (Test-Path -LiteralPath $reviewPath -PathType Leaf) 'Closure completeness review is missing.'
$review = Get-Content -Raw -LiteralPath $reviewPath
Assert-True ($review -match 'Formal ratification, standard-vocabulary freezing') 'Closure review loses the ratification boundary.'
Write-Output 'Architecture 0.8 closure verification passed: CL-C1 through CL-C5, 14 requirements, 33 vectors, two retargeted stacks, retained 0.7 evidence, and no ratification claim.'
