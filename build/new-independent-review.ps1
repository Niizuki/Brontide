[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Reference', 'Minimal')]
    [string]$Stack,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReviewerId,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReviewerName,

    [ValidateSet('Human', 'Automated')]
    [string]$ReviewerKind = 'Human',

    [string]$AutomationSystem,

    [string]$AutomationSessionId,

    [string]$OutputPath,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$requestPath = Join-Path $repositoryRoot 'conformance\reviews\review-request.json'
. (Join-Path $PSScriptRoot 'independent-review-common.ps1')

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    Get-Content -Raw -LiteralPath $Path -Encoding UTF8 | ConvertFrom-Json
}

function Get-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $RelativePath))
}

$request = Read-JsonFile $requestPath
$reviewerKindValue = $ReviewerKind.ToLowerInvariant()
if ($ReviewerKind -eq 'Automated') {
    if ([string]::IsNullOrWhiteSpace($AutomationSystem) -or
        [string]::IsNullOrWhiteSpace($AutomationSessionId)) {
        throw 'Automated review packets require -AutomationSystem and -AutomationSessionId.'
    }
    if ($request.independencePolicy.automatedAttestation.allowed -ne $true) {
        throw 'The pinned current-architecture policy does not permit automated attestation.'
    }
}
$stackRequest = @($request.stacks | Where-Object { $_.stack -eq $Stack })
if ($stackRequest.Count -ne 1) {
    throw "The review request must contain exactly one '$Stack' stack entry."
}
$stackRequest = $stackRequest[0]

$statusRegistryPath = Get-RepositoryPath ([string]$request.architectureStatusRegistry.path)
if ((Get-CanonicalTextHash $statusRegistryPath) -ne $request.architectureStatusRegistry.sha256) {
    throw 'The architecture status registry no longer matches the pinned review request.'
}
$architectureStatus = Read-JsonFile $statusRegistryPath
if ($architectureStatus.schemaVersion -ne 1) {
    throw 'The architecture status registry must use schema 1.'
}
$implementationStatus = @($architectureStatus.implementations | Where-Object { $_.stack -eq $Stack })
if ($implementationStatus.Count -ne 1) {
    throw "The architecture status registry must contain exactly one '$Stack' implementation entry."
}
$implementationStatus = $implementationStatus[0]

$currentArchitecturePath = Get-RepositoryPath ([string]$architectureStatus.currentArchitecture.path)
$requirementsPath = Get-RepositoryPath ([string]$architectureStatus.implementationBaseline.requirements.path)
$matrixPath = Get-RepositoryPath ([string]$implementationStatus.implementationMatrix.path)
$currentPlanPath = Get-RepositoryPath ([string]$implementationStatus.currentDelivery.plan.path)
$currentLedgerPath = Get-RepositoryPath ([string]$implementationStatus.currentDelivery.ledger.path)
$currentArchitectureHash = Get-CanonicalTextHash $currentArchitecturePath
$requirementsHash = Get-CanonicalTextHash $requirementsPath
$matrixHash = Get-CanonicalTextHash $matrixPath
if ($currentArchitectureHash -ne $architectureStatus.currentArchitecture.sha256) {
    throw 'The current architecture source no longer matches the pinned review request.'
}
if ($requirementsHash -ne $architectureStatus.implementationBaseline.requirements.sha256) {
    throw 'The requirement vocabulary no longer matches the pinned review request.'
}
if ($matrixHash -ne $implementationStatus.implementationMatrix.sha256) {
    throw "The $Stack evidence matrix no longer matches the pinned review request."
}
if ((Get-CanonicalTextHash $currentPlanPath) -ne $implementationStatus.currentDelivery.plan.sha256) {
    throw "The $Stack current-architecture implementation plan no longer matches the pinned review request."
}
if ((Get-CanonicalTextHash $currentLedgerPath) -ne $implementationStatus.currentDelivery.ledger.sha256) {
    throw "The $Stack current-architecture delivery ledger no longer matches the pinned review request."
}

& git -C $repositoryRoot cat-file -e "$($request.reviewTargetCommit)^{commit}" 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "Review target commit '$($request.reviewTargetCommit)' is not available locally."
}

$master = Read-JsonFile $requirementsPath
$matrix = Read-JsonFile $matrixPath
$matrixById = @{}
foreach ($entry in @($matrix.requirements)) {
    $matrixById[[string]$entry.requirementId] = $entry
}

$packetRequirements = @(
    foreach ($requirement in @($master.requirements | Where-Object { @($_.appliesTo) -contains $Stack })) {
        $entry = $matrixById[[string]$requirement.id]
        if ($null -eq $entry) {
            throw "$Stack matrix is missing '$($requirement.id)'. Run verify-evidence.ps1 first."
        }

        [ordered]@{
            requirementId = [string]$requirement.id
            architectureSection = [string]$requirement.section
            summary = [string]$requirement.summary
            component = [string]$entry.component
            claimedStatus = [string]$entry.status
            matrixRationale = [string]$entry.rationale
            positiveEvidence = @($entry.positiveEvidence)
            negativeEvidence = @($entry.negativeEvidence)
            review = [ordered]@{
                architectureReviewed = $false
                positiveEvidenceReviewed = $false
                negativeEvidenceReviewed = $false
                testsRun = @()
                verdict = 'unreviewed'
                rationale = ''
                dispositionEvidence = $null
            }
        }
    }
)

$packet = [ordered]@{
    schemaVersion = 3
    architectureStatusRegistryPath = [string]$request.architectureStatusRegistry.path
    currentArchitectureRevision = [string]$architectureStatus.currentArchitecture.revision
    implementationBaselineRevision = [string]$architectureStatus.implementationBaseline.revision
    stack = $Stack
    reviewTargetCommit = [string]$request.reviewTargetCommit
    reviewer = [ordered]@{
        id = $ReviewerId
        name = $ReviewerName
        kind = $reviewerKindValue
        independent = $false
        independenceStatement = ''
        conflicts = @()
        automation = if ($ReviewerKind -eq 'Automated') {
            [ordered]@{
                system = $AutomationSystem
                sessionId = $AutomationSessionId
                freshContext = $false
                implementationContextAccess = 'unreviewed'
            }
        } else {
            $null
        }
    }
    currentArchitectureReview = [ordered]@{
        architecturePath = [string]$architectureStatus.currentArchitecture.path
        architectureStatus = [string]$architectureStatus.currentArchitecture.status
        implementationPlanPath = [string]$implementationStatus.currentDelivery.plan.path
        deliveryLedgerPath = [string]$implementationStatus.currentDelivery.ledger.path
        architectureReviewed = $false
        implementationPlanReviewed = $false
        deliveryLedgerReviewed = $false
        statusBoundaryAcknowledged = $false
        assessment = 'unreviewed'
        rationale = ''
    }
    reviewedAt = ''
    gate = [ordered]@{
        commit = [string]$request.reviewTargetCommit
        command = '.\build\verify-interchange.ps1'
        result = 'unreviewed'
        environment = ''
        notes = ''
    }
    requirements = $packetRequirements
    overallVerdict = 'unreviewed'
    attestation = [ordered]@{
        attestedBy = ''
        statement = ''
        externalRecord = ''
    }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $destination = Get-RepositoryPath ([string]$stackRequest.attestationPath)
}
elseif ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $destination = [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    $destination = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
}

if ((Test-Path -LiteralPath $destination) -and -not $Force) {
    throw "Review packet '$destination' already exists. Use -Force only when intentionally replacing it."
}

$destinationDirectory = Split-Path -Parent $destination
if (-not (Test-Path -LiteralPath $destinationDirectory)) {
    New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
}

$json = $packet | ConvertTo-Json -Depth 12
[System.IO.File]::WriteAllText($destination, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

Write-Host "Created $Stack independent-review packet: $destination"
Write-Host "Review the architecture selected by $($request.architectureStatusRegistry.path) and pinned commit $($request.reviewTargetCommit); do not change generated evidence snapshots."
