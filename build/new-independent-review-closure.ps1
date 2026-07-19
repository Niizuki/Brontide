[CmdletBinding()]
param(
    [string]$OutputPath,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$requestPath = Join-Path $repositoryRoot 'conformance\reviews\review-request.json'
$request = Get-Content -Raw -LiteralPath $requestPath -Encoding UTF8 | ConvertFrom-Json
. (Join-Path $PSScriptRoot 'independent-review-common.ps1')
$statusRegistryPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $request.architectureStatusRegistry.path))
$architectureStatus = Get-Content -Raw -LiteralPath $statusRegistryPath -Encoding UTF8 | ConvertFrom-Json

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify-independent-review.ps1')
if ($LASTEXITCODE -ne 0) {
    throw 'Existing independent-review records are invalid; correct them before generating closure.'
}

$attestations = @()
foreach ($stackRequest in @($request.stacks)) {
    $attestationPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $stackRequest.attestationPath))
    if (-not (Test-Path -LiteralPath $attestationPath -PathType Leaf)) {
        throw "$($stackRequest.stack) attestation is missing: '$($stackRequest.attestationPath)'."
    }

    $attestation = Get-Content -Raw -LiteralPath $attestationPath -Encoding UTF8 | ConvertFrom-Json
    if ($attestation.overallVerdict -ne 'conforms') {
        throw "$($stackRequest.stack) attestation does not have a conforming overall verdict."
    }

    $attestations += [ordered]@{
        stack = [string]$stackRequest.stack
        path = [string]$stackRequest.attestationPath
        sha256 = Get-CanonicalTextHash $attestationPath
    }
}

$closure = [ordered]@{
    schemaVersion = 3
    architectureStatusRegistryPath = [string]$request.architectureStatusRegistry.path
    currentArchitectureRevision = [string]$architectureStatus.currentArchitecture.revision
    implementationBaselineRevision = [string]$architectureStatus.implementationBaseline.revision
    reviewTargetCommit = [string]$request.reviewTargetCommit
    findingClosures = @(
        foreach ($finding in @($request.correctionFindings)) {
            [ordered]@{
                findingId = [string]$finding.findingId
                closingCommit = [string]$finding.closingCommit
                evidence = [ordered]@{
                    path = [string]$finding.evidence.path
                    anchor = [string]$finding.evidence.anchor
                }
            }
        }
    )
    reviewAttestations = $attestations
    authorization = [ordered]@{
        authorized = $false
        authorizedBy = ''
        authorizedAt = ''
        statement = ''
    }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $destination = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $request.closurePath))
}
elseif ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $destination = [System.IO.Path]::GetFullPath($OutputPath)
}
else {
    $destination = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputPath))
}

if ((Test-Path -LiteralPath $destination) -and -not $Force) {
    throw "Closure record '$destination' already exists. Use -Force only when intentionally replacing it."
}

$destinationDirectory = Split-Path -Parent $destination
if (-not (Test-Path -LiteralPath $destinationDirectory)) {
    New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
}

$json = $closure | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($destination, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

Write-Host "Created pending independent-review closure record: $destination"
Write-Host 'Check the pinned hashes and commits, then fill the authorization fields.'
