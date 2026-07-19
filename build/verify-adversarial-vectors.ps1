$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$inventoryPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$failures = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $inventoryPath)) {
    Write-Error "Adversarial vector inventory not found at '$inventoryPath'."
    exit 1
}

try {
    $inventory = Get-Content -Raw -LiteralPath $inventoryPath -Encoding UTF8 | ConvertFrom-Json
}
catch {
    Write-Error "Invalid JSON in '$inventoryPath': $($_.Exception.Message)"
    exit 1
}

# --- Change plan pin -------------------------------------------------------
if ($null -eq $inventory.changePlan -or [string]::IsNullOrWhiteSpace($inventory.changePlan.path)) {
    $failures.Add('changePlan block is missing or incomplete.')
}
else {
    $planPath = Join-Path $repositoryRoot $inventory.changePlan.path
    if (-not (Test-Path -LiteralPath $planPath)) {
        $failures.Add("Pinned change plan '$($inventory.changePlan.path)' does not exist.")
    }
    elseif (-not [string]::IsNullOrWhiteSpace($inventory.changePlan.sha256)) {
        $actual = (Get-FileHash -LiteralPath $planPath -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $inventory.changePlan.sha256.ToUpperInvariant()) {
            $failures.Add("Change plan hash mismatch: inventory pins $($inventory.changePlan.sha256) but '$($inventory.changePlan.path)' hashes to $actual. Refresh the pin deliberately.")
        }
    }
}

# --- Conventions -----------------------------------------------------------
$contexts = @($inventory.conventions.contexts)
$kinds = @($inventory.conventions.kinds)
$expectations = @($inventory.conventions.expectations)
foreach ($required in @($contexts, $kinds, $expectations)) {
    if ($required.Count -eq 0) {
        $failures.Add('conventions block must enumerate contexts, kinds, and expectations.')
    }
}
$allowedStacks = @('Reference', 'Minimal')

# --- Vectors ---------------------------------------------------------------
$vectors = @($inventory.vectors)
if ($vectors.Count -eq 0) {
    $failures.Add('Inventory contains no vectors.')
}

$seenIds = [System.Collections.Generic.HashSet[string]]::new()
$changesWithVectors = [System.Collections.Generic.HashSet[string]]::new()
$idPattern = '^BR-08-ADV-C(?<change>\d{1,2})-\d{3}$'

foreach ($vector in $vectors) {
    $id = [string]$vector.id
    if ([string]::IsNullOrWhiteSpace($id)) {
        $failures.Add('A vector is missing its id.')
        continue
    }
    if (-not $seenIds.Add($id)) {
        $failures.Add("Duplicate vector id '$id'.")
    }
    $match = [regex]::Match($id, $idPattern)
    if (-not $match.Success) {
        $failures.Add("Vector id '$id' does not match the BR-08-ADV-C<change>-<nnn> pattern.")
    }
    elseif ("C$($match.Groups['change'].Value)" -ne [string]$vector.change) {
        $failures.Add("Vector id '$id' does not agree with its declared change '$($vector.change)'.")
    }

    if ([string]::IsNullOrWhiteSpace([string]$vector.change)) {
        $failures.Add("Vector '$id' is missing its change reference.")
    }
    else {
        [void]$changesWithVectors.Add([string]$vector.change)
    }

    if ($contexts -notcontains [string]$vector.context) {
        $failures.Add("Vector '$id' has undeclared context '$($vector.context)'.")
    }
    if ($kinds -notcontains [string]$vector.kind) {
        $failures.Add("Vector '$id' has undeclared kind '$($vector.kind)'.")
    }
    if ($expectations -notcontains [string]$vector.expect) {
        $failures.Add("Vector '$id' has undeclared expectation '$($vector.expect)'.")
    }

    foreach ($clause in @('given', 'when', 'then')) {
        $items = @($vector.$clause)
        if ($items.Count -eq 0 -or ($items | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0) {
            $failures.Add("Vector '$id' has an empty or blank '$clause' clause.")
        }
    }

    $stacks = @($vector.appliesTo)
    if ($stacks.Count -eq 0) {
        $failures.Add("Vector '$id' applies to no stack.")
    }
    foreach ($stack in $stacks) {
        if ($allowedStacks -notcontains [string]$stack) {
            $failures.Add("Vector '$id' names unknown stack '$stack'.")
        }
    }
}

# --- Coverage completeness -------------------------------------------------
$coverageOnly = @()
if ($null -ne $inventory.coverage) {
    $coverageOnly = @($inventory.coverage.PSObject.Properties.Name)
}
foreach ($index in 1..14) {
    $change = "C$index"
    $hasVectors = $changesWithVectors.Contains($change)
    $hasCoverageNote = $coverageOnly -contains $change
    if (-not $hasVectors -and -not $hasCoverageNote) {
        $failures.Add("Change $change has neither vectors nor a coverage entry.")
    }
    if ($hasVectors -and $hasCoverageNote) {
        $failures.Add("Change $change has both vectors and a documentation-only coverage entry; pick one.")
    }
}

# --- Report ----------------------------------------------------------------
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "FAIL: $failure"
    }
    Write-Error "Adversarial vector verification failed with $($failures.Count) issue(s)."
    exit 1
}

Write-Host "Adversarial vector inventory verified: $($vectors.Count) vectors across $($changesWithVectors.Count) changes, $($coverageOnly.Count) documentation-only coverage entries, change plan pin intact."
exit 0
