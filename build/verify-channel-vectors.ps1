$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$inventoryPath = Join-Path $repositoryRoot 'conformance\channel-0.1-vectors.json'
$ledgerPath = Join-Path $repositoryRoot 'docs\future\channel\architecture-0.8-channel-requirements-and-risk-ledger.md'
$adversarialPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        return Get-Content -Raw -LiteralPath $Path -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        $failures.Add("Invalid JSON in '$Path': $($_.Exception.Message)")
        return $null
    }
}

foreach ($requiredPath in @($inventoryPath, $ledgerPath, $adversarialPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        $failures.Add("Required Channel evidence path does not exist: '$requiredPath'.")
    }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$inventory = Read-JsonFile $inventoryPath
$adversarial = Read-JsonFile $adversarialPath
if ($null -eq $inventory -or $null -eq $adversarial) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

if ($inventory.schemaVersion -ne 1) {
    $failures.Add('Channel vector inventory must use schemaVersion 1.')
}
$contractPath = Join-Path $repositoryRoot ([string]$inventory.contract.path)
if ([string]::IsNullOrWhiteSpace([string]$inventory.contract.path) -or -not (Test-Path -LiteralPath $contractPath)) {
    $failures.Add("Channel contract path '$($inventory.contract.path)' does not exist.")
}

$allowedRequirements = @(1..11 | ForEach-Object { "CH-R$_" })
$allowedFrames = @('emit', 'accept', 'reject', 'none')
$allowedResults = @('negotiation', 'request', 'outcome-succeeded', 'outcome-failed', 'protocol-error', 'process-failure', 'denial')
$protocolCategories = @(
    'malformed-message',
    'unsupported-version',
    'unsupported-contract',
    'unsupported-kind',
    'unsupported-operation',
    'correlation-mismatch',
    'invalid-payload',
    'invalid-authority-presentation',
    'replay-detected',
    'limit-exceeded',
    'state-violation',
    'internal-protocol-failure'
)
$processCategories = @(
    'transport-unavailable',
    'transport-interrupted',
    'timeout',
    'peer-terminated',
    'peer-unavailable',
    'resource-exhausted',
    'unknown'
)
$failureDomains = @('local-endpoint', 'transport', 'remote-endpoint', 'remote-provider', 'unknown')
$adversarialIds = @($adversarial.vectors | ForEach-Object { [string]$_.id })
$vectors = @($inventory.vectors)
$seenIds = [System.Collections.Generic.HashSet[string]]::new()
$coveredRequirements = [System.Collections.Generic.HashSet[string]]::new()
$coveredProtocolCategories = [System.Collections.Generic.HashSet[string]]::new()
$coveredProcessCategories = [System.Collections.Generic.HashSet[string]]::new()
$coveredFailureDomains = [System.Collections.Generic.HashSet[string]]::new()

if ($vectors.Count -eq 0) {
    $failures.Add('Channel vector inventory contains no vectors.')
}
if (@($inventory.conventions.appliesTo) -join ',' -ne 'Reference,Minimal') {
    $failures.Add('Channel vectors must apply independently to Reference and Minimal.')
}

foreach ($vector in $vectors) {
    $id = [string]$vector.id
    if ($id -notmatch '^CH-\d{2}-[A-Z0-9-]+$') {
        $failures.Add("Vector id '$id' does not match CH-<nn>-<NAME>.")
    }
    elseif (-not $seenIds.Add($id)) {
        $failures.Add("Duplicate Channel vector id '$id'.")
    }

    foreach ($clause in @('given', 'when', 'then')) {
        $items = @($vector.$clause)
        if ($items.Count -eq 0 -or @($items | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0) {
            $failures.Add("Vector '$id' has an empty or blank '$clause' clause.")
        }
    }

    foreach ($requirement in @($vector.requirements)) {
        $value = [string]$requirement
        if ($allowedRequirements -notcontains $value) {
            $failures.Add("Vector '$id' names unknown requirement '$value'.")
        }
        else {
            [void]$coveredRequirements.Add($value)
        }
    }
    if (@($vector.requirements).Count -eq 0) {
        $failures.Add("Vector '$id' names no Channel requirement.")
    }

    if ($allowedFrames -notcontains [string]$vector.expected.frameDecision) {
        $failures.Add("Vector '$id' has invalid frameDecision '$($vector.expected.frameDecision)'.")
    }
    if ($allowedResults -notcontains [string]$vector.expected.resultClass) {
        $failures.Add("Vector '$id' has invalid resultClass '$($vector.expected.resultClass)'.")
    }

    foreach ($category in @($vector.expected.category) + @($vector.expected.allowedCategories)) {
        $value = [string]$category
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        if ($protocolCategories -notcontains $value) {
            $failures.Add("Vector '$id' names unknown protocol category '$value'.")
        }
        else {
            [void]$coveredProtocolCategories.Add($value)
        }
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$vector.expected.processCategory)) {
        $value = [string]$vector.expected.processCategory
        if ($processCategories -notcontains $value) {
            $failures.Add("Vector '$id' names unknown process category '$value'.")
        }
        else {
            [void]$coveredProcessCategories.Add($value)
        }
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$vector.expected.failureDomain)) {
        $value = [string]$vector.expected.failureDomain
        if ($failureDomains -notcontains $value) {
            $failures.Add("Vector '$id' names unknown failure domain '$value'.")
        }
        else {
            [void]$coveredFailureDomains.Add($value)
        }
    }

    foreach ($case in @($vector.cases)) {
        $value = [string]$case
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        if ($processCategories -contains $value) { [void]$coveredProcessCategories.Add($value) }
        if ($failureDomains -contains $value) { [void]$coveredFailureDomains.Add($value) }
        if ($processCategories -notcontains $value -and $failureDomains -notcontains $value) {
            $failures.Add("Vector '$id' has undeclared case '$value'.")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$vector.linkedArchitectureVector) -and
        $adversarialIds -notcontains [string]$vector.linkedArchitectureVector) {
        $failures.Add("Vector '$id' links missing architecture vector '$($vector.linkedArchitectureVector)'.")
    }
}

foreach ($requirement in $allowedRequirements) {
    if (-not $coveredRequirements.Contains($requirement)) {
        $failures.Add("No Channel vector covers '$requirement'.")
    }
}
foreach ($category in $protocolCategories) {
    if (-not $coveredProtocolCategories.Contains($category)) {
        $failures.Add("No Channel vector covers protocol category '$category'.")
    }
}
foreach ($category in $processCategories) {
    if (-not $coveredProcessCategories.Contains($category)) {
        $failures.Add("No Channel vector covers process category '$category'.")
    }
}
foreach ($domain in $failureDomains) {
    if (-not $coveredFailureDomains.Contains($domain)) {
        $failures.Add("No Channel vector covers failure domain '$domain'.")
    }
}

if (Test-Path -LiteralPath $contractPath) {
    $contractText = Get-Content -Raw -LiteralPath $contractPath -Encoding UTF8
    foreach ($term in $protocolCategories + $processCategories + $failureDomains) {
        if (-not $contractText.Contains($term)) {
            $failures.Add("Channel contract does not contain declared taxonomy term '$term'.")
        }
    }
}
$ledgerText = Get-Content -Raw -LiteralPath $ledgerPath -Encoding UTF8
foreach ($requirement in $allowedRequirements) {
    if (-not $ledgerText.Contains("| $requirement |")) {
        $failures.Add("Channel ledger does not contain '$requirement'.")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    Write-Error "Channel vector verification failed with $($failures.Count) issue(s)."
    exit 1
}

Write-Host "Channel vector verification passed: $($vectors.Count) vectors cover 11 requirements, $($protocolCategories.Count) protocol categories, $($processCategories.Count) process categories, and $($failureDomains.Count) failure domains."
exit 0
