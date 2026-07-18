[CmdletBinding()]
param(
    [switch]$RequireComplete
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$requestPath = Join-Path $repositoryRoot 'conformance\reviews\review-request.json'
$failures = [System.Collections.Generic.List[string]]::new()
$pending = [System.Collections.Generic.List[string]]::new()
$passingReviews = @{}
$reviewers = @{}
$closureComplete = $false
. (Join-Path $PSScriptRoot 'independent-review-common.ps1')

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

function Get-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $candidate = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $RelativePath))
    $rootWithSeparator = $repositoryRoot.TrimEnd('\') + '\'
    if (-not $candidate.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        $failures.Add("Review path escapes the repository: '$RelativePath'.")
        return $null
    }

    return $candidate
}

function Test-Hash {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedHash,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $failures.Add("$Label is missing: '$Path'.")
        return $false
    }

    $actualHash = Get-CanonicalTextHash $Path
    if ($actualHash -ne $ExpectedHash) {
        $failures.Add("$Label hash changed: expected $ExpectedHash, found $actualHash.")
        return $false
    }

    return $true
}

function Test-CommitExists {
    param([Parameter(Mandatory = $true)][string]$Commit)

    if ($Commit -notmatch '^[0-9a-fA-F]{40}$') {
        return $false
    }

    & git -C $repositoryRoot cat-file -e "$Commit^{commit}" 2>$null
    return $LASTEXITCODE -eq 0
}

function Test-CommitIsAncestor {
    param(
        [Parameter(Mandatory = $true)][string]$Ancestor,
        [Parameter(Mandatory = $true)][string]$Descendant
    )

    & git -C $repositoryRoot merge-base --is-ancestor $Ancestor $Descendant 2>$null
    return $LASTEXITCODE -eq 0
}

function Test-RepositoryEvidence {
    param(
        [Parameter(Mandatory = $true)][AllowNull()]$Evidence,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ($null -eq $Evidence -or
        [string]::IsNullOrWhiteSpace([string]$Evidence.path) -or
        [string]::IsNullOrWhiteSpace([string]$Evidence.anchor)) {
        $failures.Add("$Label must name a repository path and anchor.")
        return $false
    }

    $path = Get-RepositoryPath ([string]$Evidence.path)
    if ($null -eq $path -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $failures.Add("$Label path is missing: '$($Evidence.path)'.")
        return $false
    }

    $content = Get-Content -Raw -LiteralPath $path -Encoding UTF8
    if ($content.IndexOf([string]$Evidence.anchor, [StringComparison]::Ordinal) -lt 0) {
        $failures.Add("$Label anchor is stale in '$($Evidence.path)': '$($Evidence.anchor)'.")
        return $false
    }

    return $true
}

function Test-EvidenceSnapshot {
    param(
        [Parameter(Mandatory = $true)][AllowNull()][AllowEmptyCollection()]$Expected,
        [Parameter(Mandatory = $true)][AllowNull()][AllowEmptyCollection()]$Actual,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $expectedItems = @($Expected)
    $actualItems = @($Actual)
    if ($expectedItems.Count -ne $actualItems.Count) {
        $failures.Add("$Label evidence count changed from $($expectedItems.Count) to $($actualItems.Count).")
        return $false
    }

    $matches = $true
    for ($index = 0; $index -lt $expectedItems.Count; $index++) {
        if ($expectedItems[$index].path -ne $actualItems[$index].path -or
            $expectedItems[$index].anchor -ne $actualItems[$index].anchor) {
            $failures.Add("$Label evidence item $index does not match the pinned matrix.")
            $matches = $false
        }
    }

    return $matches
}

function Test-RequiredDate {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $parsed = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse($Value, [ref]$parsed)) {
        $failures.Add("$Label must be an ISO-compatible date and time.")
        return $false
    }

    return $true
}

if (-not (Test-Path -LiteralPath $requestPath -PathType Leaf)) {
    Write-Error "Independent-review request is missing: '$requestPath'."
    exit 1
}

$request = Read-JsonFile $requestPath
if ($null -eq $request) {
    exit 1
}

if ($request.schemaVersion -ne 1 -or $request.architectureRevision -ne '0.5') {
    $failures.Add('The independent-review request must use schema 1 and Architecture 0.5.')
}

$reviewTargetCommit = [string]$request.reviewTargetCommit
if (-not (Test-CommitExists $reviewTargetCommit)) {
    $failures.Add("Review target '$reviewTargetCommit' is not a locally available full commit id.")
}
elseif (-not (Test-CommitIsAncestor $reviewTargetCommit 'HEAD')) {
    $failures.Add("Review target '$reviewTargetCommit' is not an ancestor of HEAD.")
}

$requirementsPath = Get-RepositoryPath ([string]$request.requirements.path)
if ($null -ne $requirementsPath) {
    Test-Hash $requirementsPath ([string]$request.requirements.sha256) 'Pinned requirement vocabulary' | Out-Null
}
$master = if ($null -ne $requirementsPath -and (Test-Path -LiteralPath $requirementsPath)) {
    Read-JsonFile $requirementsPath
} else {
    $null
}

$stackRequests = @($request.stacks)
$stackNames = @($stackRequests | ForEach-Object { [string]$_.stack })
if ($stackRequests.Count -ne 2 -or
    @($stackNames | Sort-Object -Unique).Count -ne 2 -or
    'Reference' -notin $stackNames -or
    'Minimal' -notin $stackNames) {
    $failures.Add('The review request must contain exactly one Reference and one Minimal stack entry.')
}

$excludedReviewerIds = @($request.independencePolicy.excludedReviewerIds)
$requiredIndependenceStatement = [string]$request.independencePolicy.requiredStatement
$requiredAttestationStatement = [string]$request.independencePolicy.attestationStatement
if ([string]::IsNullOrWhiteSpace($requiredIndependenceStatement) -or
    [string]::IsNullOrWhiteSpace($requiredAttestationStatement)) {
    $failures.Add('The review request must pin independence and attestation statements.')
}

foreach ($stackRequest in $stackRequests) {
    $stack = [string]$stackRequest.stack
    $matrixPath = Get-RepositoryPath ([string]$stackRequest.matrixPath)
    if ($null -eq $matrixPath) {
        continue
    }

    $matrixHashValid = Test-Hash $matrixPath ([string]$stackRequest.matrixSha256) "$stack pinned matrix"
    $matrix = if ($matrixHashValid) { Read-JsonFile $matrixPath } else { $null }
    if ($null -eq $matrix -or $null -eq $master) {
        continue
    }

    if ($matrix.stack -ne $stack) {
        $failures.Add("$stack review request points at a '$($matrix.stack)' matrix.")
        continue
    }

    $attestationPath = Get-RepositoryPath ([string]$stackRequest.attestationPath)
    if ($null -eq $attestationPath) {
        continue
    }
    if (-not (Test-Path -LiteralPath $attestationPath -PathType Leaf)) {
        $pending.Add("$stack independent-review attestation is pending at '$($stackRequest.attestationPath)'.")
        continue
    }

    $failureCountBeforeAttestation = $failures.Count
    $attestation = Read-JsonFile $attestationPath
    if ($null -eq $attestation) {
        continue
    }

    if ($attestation.schemaVersion -ne 1 -or
        $attestation.architectureRevision -ne $request.architectureRevision -or
        $attestation.stack -ne $stack -or
        $attestation.reviewTargetCommit -ne $reviewTargetCommit) {
        $failures.Add("$stack attestation identity does not match the pinned review request.")
    }

    $reviewerId = [string]$attestation.reviewer.id
    $reviewerName = [string]$attestation.reviewer.name
    if ([string]::IsNullOrWhiteSpace($reviewerId) -or [string]::IsNullOrWhiteSpace($reviewerName)) {
        $failures.Add("$stack attestation must identify its reviewer.")
    }
    if ($reviewerId -in $excludedReviewerIds) {
        $failures.Add("$stack reviewer '$reviewerId' is excluded by the independence policy.")
    }
    if ($attestation.reviewer.independent -ne $true -or
        $attestation.reviewer.independenceStatement -ne $requiredIndependenceStatement) {
        $failures.Add("$stack reviewer must affirm the exact pinned independence statement.")
    }
    if (@($attestation.reviewer.conflicts).Count -ne 0) {
        $failures.Add("$stack independent review declares a conflict; use a conflict-free reviewer.")
    }
    Test-RequiredDate ([string]$attestation.reviewedAt) "$stack reviewedAt" | Out-Null

    if ($attestation.gate.commit -ne $reviewTargetCommit -or
        $attestation.gate.command -ne '.\build\verify-interchange.ps1' -or
        $attestation.gate.result -ne 'passed' -or
        [string]::IsNullOrWhiteSpace([string]$attestation.gate.environment)) {
        $failures.Add("$stack attestation must record a passing full gate at the pinned commit and name its environment.")
    }

    $masterById = @{}
    foreach ($requirement in @($master.requirements | Where-Object { @($_.appliesTo) -contains $stack })) {
        $masterById[[string]$requirement.id] = $requirement
    }
    $matrixById = @{}
    foreach ($entry in @($matrix.requirements)) {
        $matrixById[[string]$entry.requirementId] = $entry
    }

    $reviewEntries = @($attestation.requirements)
    $reviewIds = @($reviewEntries | ForEach-Object { [string]$_.requirementId })
    foreach ($duplicate in @($reviewIds | Group-Object | Where-Object Count -gt 1)) {
        $failures.Add("$stack attestation duplicates requirement '$($duplicate.Name)'.")
    }
    foreach ($missingId in @($masterById.Keys | Where-Object { $_ -notin $reviewIds })) {
        $failures.Add("$stack attestation is missing requirement '$missingId'.")
    }
    foreach ($unknownId in @($reviewIds | Where-Object { -not $masterById.ContainsKey($_) })) {
        $failures.Add("$stack attestation contains unknown requirement '$unknownId'.")
    }

    $hasNonConformance = $false
    foreach ($reviewEntry in $reviewEntries) {
        $id = [string]$reviewEntry.requirementId
        if (-not $masterById.ContainsKey($id) -or -not $matrixById.ContainsKey($id)) {
            continue
        }

        $requirement = $masterById[$id]
        $matrixEntry = $matrixById[$id]
        if ($reviewEntry.architectureSection -ne $requirement.section -or
            $reviewEntry.summary -ne $requirement.summary -or
            $reviewEntry.component -ne $matrixEntry.component -or
            $reviewEntry.claimedStatus -ne $matrixEntry.status -or
            $reviewEntry.matrixRationale -ne $matrixEntry.rationale) {
            $failures.Add("$stack/$id generated requirement snapshot does not match the pinned vocabulary and matrix.")
        }

        Test-EvidenceSnapshot @($matrixEntry.positiveEvidence) @($reviewEntry.positiveEvidence) "$stack/$id positive" | Out-Null
        Test-EvidenceSnapshot @($matrixEntry.negativeEvidence) @($reviewEntry.negativeEvidence) "$stack/$id negative" | Out-Null

        if ($reviewEntry.review.architectureReviewed -ne $true -or
            $reviewEntry.review.positiveEvidenceReviewed -ne $true -or
            $reviewEntry.review.negativeEvidenceReviewed -ne $true) {
            $failures.Add("$stack/$id must affirm architecture, positive-evidence, and negative-evidence review.")
        }
        if ([string]::IsNullOrWhiteSpace([string]$reviewEntry.review.rationale) -or
            ([string]$reviewEntry.review.rationale).Length -lt 20) {
            $failures.Add("$stack/$id needs a substantive review rationale of at least 20 characters.")
        }

        $verdict = [string]$reviewEntry.review.verdict
        if ($verdict -notin @('conforms', 'approved-disposition', 'does-not-conform')) {
            $failures.Add("$stack/$id has unsupported or incomplete verdict '$verdict'.")
        }
        elseif ($verdict -eq 'approved-disposition') {
            Test-RepositoryEvidence $reviewEntry.review.dispositionEvidence "$stack/$id approved disposition" | Out-Null
        }
        elseif ($verdict -eq 'does-not-conform') {
            $hasNonConformance = $true
        }
    }

    $expectedOverall = if ($hasNonConformance) { 'does-not-conform' } else { 'conforms' }
    if ($attestation.overallVerdict -ne $expectedOverall) {
        $failures.Add("$stack overall verdict must be '$expectedOverall' for its requirement decisions.")
    }
    if ($attestation.attestation.attestedBy -ne $reviewerId -or
        $attestation.attestation.statement -ne $requiredAttestationStatement) {
        $failures.Add("$stack attestation must be affirmed by its reviewer with the exact pinned statement.")
    }

    if ($failures.Count -eq $failureCountBeforeAttestation) {
        $reviewers[$stack] = $reviewerId
        if ($attestation.overallVerdict -eq 'conforms') {
            $passingReviews[$stack] = $attestationPath
        }
        else {
            $pending.Add("$stack review reports non-conformance and blocks correction closure.")
        }
    }
}

if ($request.independencePolicy.requireDistinctStackReviewers -eq $true -and
    $reviewers.ContainsKey('Reference') -and
    $reviewers.ContainsKey('Minimal') -and
    $reviewers['Reference'] -eq $reviewers['Minimal']) {
    $failures.Add('The review request requires distinct reviewers for Reference and Minimal.')
}

$closurePath = Get-RepositoryPath ([string]$request.closurePath)
if ($null -ne $closurePath -and (Test-Path -LiteralPath $closurePath -PathType Leaf)) {
    $failureCountBeforeClosure = $failures.Count
    $closure = Read-JsonFile $closurePath
    if ($null -ne $closure) {
        if ($closure.schemaVersion -ne 1 -or
            $closure.architectureRevision -ne $request.architectureRevision -or
            $closure.reviewTargetCommit -ne $reviewTargetCommit) {
            $failures.Add('Closure record identity does not match the pinned review request.')
        }

        $expectedFindings = @($request.correctionFindings)
        $actualFindings = @($closure.findingClosures)
        if ($actualFindings.Count -ne $expectedFindings.Count) {
            $failures.Add('Closure record does not contain every pinned correction finding.')
        }
        foreach ($expectedFinding in $expectedFindings) {
            $actual = @($actualFindings | Where-Object { $_.findingId -eq $expectedFinding.findingId })
            if ($actual.Count -ne 1) {
                $failures.Add("Closure record must contain exactly one finding '$($expectedFinding.findingId)'.")
                continue
            }
            $actual = $actual[0]
            if ($actual.closingCommit -ne $expectedFinding.closingCommit -or
                $actual.evidence.path -ne $expectedFinding.evidence.path -or
                $actual.evidence.anchor -ne $expectedFinding.evidence.anchor) {
                $failures.Add("Closure finding '$($expectedFinding.findingId)' does not match the pinned request.")
            }
            if (-not (Test-CommitExists ([string]$actual.closingCommit)) -or
                -not (Test-CommitIsAncestor ([string]$actual.closingCommit) $reviewTargetCommit)) {
                $failures.Add("Closure finding '$($expectedFinding.findingId)' names a commit outside the reviewed history.")
            }
            Test-RepositoryEvidence $actual.evidence "Closure finding '$($expectedFinding.findingId)'" | Out-Null
        }

        $actualReviewRecords = @($closure.reviewAttestations)
        if ($actualReviewRecords.Count -ne $stackRequests.Count) {
            $failures.Add('Closure record must pin both stack attestations.')
        }
        foreach ($stackRequest in $stackRequests) {
            $stack = [string]$stackRequest.stack
            $actual = @($actualReviewRecords | Where-Object { $_.stack -eq $stack })
            if ($actual.Count -ne 1 -or $actual[0].path -ne $stackRequest.attestationPath) {
                $failures.Add("Closure record must pin the expected $Stack attestation path.")
                continue
            }
            $attestationPath = Get-RepositoryPath ([string]$stackRequest.attestationPath)
            if ($null -ne $attestationPath -and (Test-Path -LiteralPath $attestationPath -PathType Leaf)) {
                Test-Hash $attestationPath ([string]$actual[0].sha256) "$stack closure attestation" | Out-Null
            }
            else {
                $failures.Add("Closure record pins missing $stack attestation '$($stackRequest.attestationPath)'.")
            }
        }

        if ($closure.authorization.authorized -eq $true) {
            if ([string]::IsNullOrWhiteSpace([string]$closure.authorization.authorizedBy)) {
                $failures.Add('Closure authorization must identify the authorizer.')
            }
            Test-RequiredDate ([string]$closure.authorization.authorizedAt) 'Closure authorizedAt' | Out-Null
            $requiredAuthorization = "I authorize deletion of $($request.temporaryPlanPath)."
            if ($closure.authorization.statement -ne $requiredAuthorization) {
                $failures.Add("Closure authorization must use the exact statement: '$requiredAuthorization'")
            }
        }
        else {
            $pending.Add('Independent-review closure exists but deletion is not authorized.')
        }

        if ($failures.Count -eq $failureCountBeforeClosure -and
            $passingReviews.Count -eq $stackRequests.Count -and
            $closure.authorization.authorized -eq $true) {
            $closureComplete = $true
        }
    }
}
else {
    $pending.Add("Independent-review closure record is pending at '$($request.closurePath)'.")
}

$temporaryPlanPath = Get-RepositoryPath ([string]$request.temporaryPlanPath)
$temporaryPlanExists = $null -ne $temporaryPlanPath -and (Test-Path -LiteralPath $temporaryPlanPath -PathType Leaf)
if (-not $temporaryPlanExists) {
    $RequireComplete = $true
}

if ($RequireComplete -and -not $closureComplete) {
    $failures.Add('Independent review is not complete: both conforming attestations and an authorized closure record are required.')
}

foreach ($message in $pending) {
    Write-Host "PENDING: $message" -ForegroundColor Yellow
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "ERROR: $failure" -ForegroundColor Red
    }
    exit 1
}

if ($closureComplete) {
    if ($temporaryPlanExists) {
        Write-Host 'Independent review verification passed; the temporary correction plan is authorized for deletion.'
    }
    else {
        Write-Host 'Independent review verification passed; temporary-plan deletion authorization is preserved.'
    }
}
else {
    Write-Host "Independent-review framework verification passed with $($pending.Count) pending item(s); the temporary correction plan remains required."
}
