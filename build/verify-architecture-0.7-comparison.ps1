param([switch]$NoBuild)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fixturePath = Join-Path $repositoryRoot 'conformance\architecture-0.7-comparison-vectors.json'
$referenceProject = Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Architecture07.TestConsole\Brontide.Reference.Architecture07.TestConsole.csproj'
$minimalProject = Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Architecture07.TestConsole\Brontide.Minimal.Architecture07.TestConsole.fsproj'

function Assert-Comparison {
    param([bool]$Condition, [string]$Capability, [string]$Message)
    if (-not $Condition) { throw "${Capability}: $Message" }
}

$fixture = Get-Content -Raw -LiteralPath $fixturePath -Encoding UTF8 | ConvertFrom-Json
$vectors = @($fixture.vectors)

Assert-Comparison ($fixture.contract -eq 'BR-07-CROSS-STACK-COMPARISON-001' -and $fixture.version -eq 1) 'C1 data-only questions' 'unexpected contract or version'
Assert-Comparison ($vectors.Count -gt 0 -and @($vectors.id | Select-Object -Unique).Count -eq $vectors.Count) 'C1 data-only questions' 'vector ids must be present and unique'
$phases = @($vectors.phase | Select-Object -Unique)
Assert-Comparison (@($phases | Where-Object { $_ -notin @('R1-M1', 'R2-M2', 'R3-M3', 'R4-M4') }).Count -eq 0) 'C1 data-only questions' 'a vector is outside R1/M1 through R4/M4'
$serializedFixture = Get-Content -Raw -LiteralPath $fixturePath -Encoding UTF8
Assert-Comparison ($serializedFixture -notmatch 'Brontide\.Reference|Brontide\.Minimal|\.csproj|\.fsproj|System\.') 'C1 data-only questions' 'fixture leaks an implementation artifact'

Assert-Comparison (Test-Path -LiteralPath $referenceProject) 'C2 independent process observations' "missing Reference endpoint: $referenceProject"
Assert-Comparison (Test-Path -LiteralPath $minimalProject) 'C2 independent process observations' "missing Minimal endpoint: $minimalProject"

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("brontide-a07-comparison-" + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
try {
    $referenceOutput = Join-Path $temporaryRoot 'reference.json'
    $minimalOutput = Join-Path $temporaryRoot 'minimal.json'
    $referenceArguments = @('run', '--project', $referenceProject)
    $minimalArguments = @('run', '--project', $minimalProject)
    if ($NoBuild) {
        $referenceArguments += '--no-build'
        $minimalArguments += '--no-build'
    }
    $referenceArguments += @('--', $fixturePath, $referenceOutput)
    $minimalArguments += @('--', $fixturePath, $minimalOutput)
    & dotnet @referenceArguments
    if ($LASTEXITCODE -ne 0) { throw "C2 independent process observations: Reference endpoint exited $LASTEXITCODE" }
    & dotnet @minimalArguments
    if ($LASTEXITCODE -ne 0) { throw "C2 independent process observations: Minimal endpoint exited $LASTEXITCODE" }

    $reference = Get-Content -Raw -LiteralPath $referenceOutput -Encoding UTF8 | ConvertFrom-Json
    $minimal = Get-Content -Raw -LiteralPath $minimalOutput -Encoding UTF8 | ConvertFrom-Json
    function Test-ResultSet {
        param([string]$Name, [object[]]$Results)
        $name = $Name
        $results = $Results
        Assert-Comparison ($results.Count -eq $vectors.Count) 'C2 independent process observations' "$name returned $($results.Count) observations for $($vectors.Count) vectors"
        Assert-Comparison (@($results.id | Select-Object -Unique).Count -eq $results.Count) 'C2 independent process observations' "$name returned duplicate vector ids"
        Assert-Comparison (@(Compare-Object @($vectors.id | Sort-Object) @($results.id | Sort-Object)).Count -eq 0) 'C2 independent process observations' "$name returned a missing or unsolicited vector id"
        foreach ($result in $results) {
            Assert-Comparison ($result.PSObject.Properties.Name -notcontains 'stack') 'C3 complete observable comparison' "$name result $($result.id) leaks its stack name"
            if ($result.status -eq 'denied') {
                Assert-Comparison (-not [string]::IsNullOrWhiteSpace($result.diagnostic) -and $result.diagnostic -ne 'none') 'C3 complete observable comparison' "$name denial $($result.id) lacks a diagnostic category"
                Assert-Comparison ($result.PSObject.Properties.Name -notcontains 'effects' -or $result.effects -eq 0) 'C3 complete observable comparison' "$name denial $($result.id) reports effects"
            }
        }
    }
    Test-ResultSet -Name 'Reference' -Results $reference
    Test-ResultSet -Name 'Minimal' -Results $minimal

    function Normalize([object]$Value) { return ($Value | ConvertTo-Json -Depth 30 -Compress) }
    foreach ($vector in $vectors) {
        $expected = [ordered]@{ id = $vector.id }
        foreach ($property in $vector.expected.PSObject.Properties) { $expected[$property.Name] = $property.Value }
        $referenceResult = $reference | Where-Object id -eq $vector.id
        $minimalResult = $minimal | Where-Object id -eq $vector.id
        Assert-Comparison ((Normalize $referenceResult) -eq (Normalize ([pscustomobject]$expected))) 'C4 expected and paired agreement' "Reference disagrees with expected observation for $($vector.id)"
        Assert-Comparison ((Normalize $minimalResult) -eq (Normalize ([pscustomobject]$expected))) 'C4 expected and paired agreement' "Minimal disagrees with expected observation for $($vector.id)"
        Assert-Comparison ((Normalize $referenceResult) -eq (Normalize $minimalResult)) 'C4 expected and paired agreement' "the stacks disagree for $($vector.id)"
    }

    Assert-Comparison (@($fixture.allowedDisagreements).Count -eq 0) 'C5 disagreement accountability' 'this delivery must not allow a disagreement'
    Assert-Comparison ($vectors.Count -eq 15) 'C6 bounded proof' 'the published finite proof boundary changed without updating the contract evidence'
    Write-Host "Architecture 0.7 comparison passed: $($vectors.Count) vectors, two independent processes, no disagreements."
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) { Remove-Item -LiteralPath $temporaryRoot -Recurse -Force }
}
