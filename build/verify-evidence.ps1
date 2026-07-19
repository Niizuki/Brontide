$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'independent-review-common.ps1')
$masterPath = Join-Path $repositoryRoot 'conformance\requirements.json'
$matrixPaths = @(
    Join-Path $repositoryRoot 'Reference\conformance\architecture-0.5.json'
    Join-Path $repositoryRoot 'Minimal\conformance\architecture-0.5.json'
)
$allowedStatuses = @('implemented', 'tested', 'demonstrated', 'planned', 'not-applicable')
$allowedClassifications = @('implemented', 'missing', 'conflicting', 'non-runtime')
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

function Test-Evidence {
    param(
        [Parameter(Mandatory = $true)]$Evidence,
        [Parameter(Mandatory = $true)][string]$RequirementId,
        [Parameter(Mandatory = $true)][string]$Kind
    )

    foreach ($item in @($Evidence)) {
        if ([string]::IsNullOrWhiteSpace($item.path) -or [string]::IsNullOrWhiteSpace($item.anchor)) {
            $failures.Add("$RequirementId has incomplete $Kind evidence.")
            continue
        }

        $candidate = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $item.path))
        $rootWithSeparator = $repositoryRoot.TrimEnd('\') + '\'
        if (-not $candidate.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
            $failures.Add("$RequirementId $Kind evidence escapes the repository: '$($item.path)'.")
            continue
        }

        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            $failures.Add("$RequirementId $Kind evidence path is stale: '$($item.path)'.")
            continue
        }

        $content = Get-Content -Raw -LiteralPath $candidate -Encoding UTF8
        if ($content.IndexOf([string]$item.anchor, [StringComparison]::Ordinal) -lt 0) {
            $failures.Add("$RequirementId $Kind evidence anchor is stale in '$($item.path)': '$($item.anchor)'.")
        }
    }
}

$master = Read-JsonFile $masterPath
if ($null -eq $master) {
    throw 'The requirement vocabulary could not be read.'
}

if ($master.schemaVersion -ne 1 -or $master.architectureRevision -ne '0.5') {
    $failures.Add('The master requirement vocabulary must use schema 1 and Architecture 0.5.')
}

$requirements = @($master.requirements)
$masterIds = @($requirements | ForEach-Object { $_.id })
$duplicateMasterIds = $masterIds | Group-Object | Where-Object Count -gt 1
foreach ($duplicate in $duplicateMasterIds) {
    $failures.Add("Duplicate master requirement id '$($duplicate.Name)'.")
}

$knownNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($requirement in $requirements) {
    if ($requirement.id -notmatch '^BR-05-[A-Z]+-[0-9]{3}$') {
        $failures.Add("Requirement id '$($requirement.id)' is not stable Architecture 0.5 form.")
    }

    foreach ($name in @($requirement.id) + @($requirement.aliases)) {
        if (-not $knownNames.Add([string]$name)) {
            $failures.Add("Requirement id or alias '$name' is duplicated.")
        }
    }

    if ([string]::IsNullOrWhiteSpace($requirement.section) -or
        [string]::IsNullOrWhiteSpace($requirement.summary) -or
        @($requirement.appliesTo).Count -eq 0) {
        $failures.Add("Requirement '$($requirement.id)' is incomplete.")
    }
}

foreach ($matrixPath in $matrixPaths) {
    $matrix = Read-JsonFile $matrixPath
    if ($null -eq $matrix) {
        continue
    }

    $stack = [string]$matrix.stack
    if ($matrix.schemaVersion -ne 1 -or $stack -notin @('Reference', 'Minimal')) {
        $failures.Add("Matrix '$matrixPath' has an unsupported schema or stack name.")
        continue
    }

    $entries = @($matrix.requirements)
    $entryIds = @($entries | ForEach-Object { $_.requirementId })
    foreach ($duplicate in ($entryIds | Group-Object | Where-Object Count -gt 1)) {
        $failures.Add("$stack matrix duplicates requirement '$($duplicate.Name)'.")
    }

    $applicableIds = @(
        $requirements |
            Where-Object { @($_.appliesTo) -contains $stack } |
            ForEach-Object { $_.id }
    )

    foreach ($missing in ($applicableIds | Where-Object { $_ -notin $entryIds })) {
        $failures.Add("$stack matrix is missing applicable requirement '$missing'.")
    }

    foreach ($unknown in ($entryIds | Where-Object { $_ -notin $masterIds })) {
        $failures.Add("$stack matrix contains unknown or stale requirement '$unknown'.")
    }

    foreach ($entry in $entries) {
        $id = [string]$entry.requirementId
        if ($entry.architectureRevision -ne '0.5') {
            $failures.Add("$stack/$id has stale architecture revision '$($entry.architectureRevision)'.")
        }

        if ([string]::IsNullOrWhiteSpace($entry.component) -or
            [string]::IsNullOrWhiteSpace($entry.rationale)) {
            $failures.Add("$stack/$id must name a component and rationale.")
        }

        if ($entry.status -notin $allowedStatuses) {
            $failures.Add("$stack/$id has unsupported status '$($entry.status)'.")
        }

        $positive = @($entry.positiveEvidence)
        $negative = @($entry.negativeEvidence)
        Test-Evidence $positive $id 'positive'
        Test-Evidence $negative $id 'negative'

        if ($entry.status -in @('tested', 'demonstrated')) {
            $testEvidence = @($positive + $negative | Where-Object { $_.path -match '(^|/)tests/' })
            if ($testEvidence.Count -eq 0) {
                $failures.Add("$stack/$id claims '$($entry.status)' without executable test evidence.")
            }
        }

        if ($entry.status -eq 'not-applicable' -and ($positive.Count + $negative.Count) -gt 0) {
            $failures.Add("$stack/$id is not-applicable but still names implementation evidence.")
        }
    }
}

$narratives = @(
    Join-Path $repositoryRoot 'Reference\docs\milestone-evidence.md'
    Join-Path $repositoryRoot 'Minimal\docs\milestone-evidence.md'
)
foreach ($narrative in $narratives) {
    $content = Get-Content -Raw -LiteralPath $narrative -Encoding UTF8
    if ($content.IndexOf('conformance/architecture-0.5.json', [StringComparison]::Ordinal) -lt 0) {
        $failures.Add("Narrative evidence '$narrative' does not route claims through its checked matrix.")
    }
}

$currentRegistryPath = Join-Path $repositoryRoot 'Brontide-Architecture-Status.json'
$currentMasterPath = Join-Path $repositoryRoot 'conformance\architecture-0.7-requirements.json'
$currentMatrixPaths = @(
    Join-Path $repositoryRoot 'Reference\conformance\architecture-0.7.json'
    Join-Path $repositoryRoot 'Minimal\conformance\architecture-0.7.json'
)
$registry = Read-JsonFile $currentRegistryPath
$currentMaster = Read-JsonFile $currentMasterPath

if ($null -ne $registry -and $null -ne $currentMaster) {
    $expectedArchitecture = $registry.currentArchitecture
    $recordedArchitecture = $currentMaster.architecture
    if ($currentMaster.schemaVersion -ne 1 -or $recordedArchitecture.revision -ne '0.7') {
        $failures.Add('The current requirement vocabulary must use schema 1 and Architecture 0.7.')
    }

    foreach ($field in @('revision', 'status', 'path', 'sha256')) {
        if ([string]$recordedArchitecture.$field -cne [string]$expectedArchitecture.$field) {
            $failures.Add("The current requirement vocabulary architecture field '$field' does not match the status registry.")
        }
    }

    $architecturePath = Join-Path $repositoryRoot ([string]$recordedArchitecture.path)
    if (-not (Test-Path -LiteralPath $architecturePath -PathType Leaf)) {
        $failures.Add("The current architecture path is stale: '$($recordedArchitecture.path)'.")
    }
    elseif ((Get-CanonicalTextHash $architecturePath) -cne [string]$recordedArchitecture.sha256) {
        $failures.Add('The current architecture content hash does not match the requirement vocabulary.')
    }

    if ([string]::IsNullOrWhiteSpace($recordedArchitecture.reviewedCommit) -or
        [string]$recordedArchitecture.reviewedCommit -notmatch '^[0-9a-f]{40}$') {
        $failures.Add('The current requirement vocabulary must record the reviewed commit.')
    }

    $currentRequirements = @($currentMaster.requirements)
    $currentIds = @($currentRequirements | ForEach-Object { $_.id })
    foreach ($duplicate in ($currentIds | Group-Object | Where-Object Count -gt 1)) {
        $failures.Add("Current requirement vocabulary duplicates '$($duplicate.Name)'.")
    }

    foreach ($requirement in $currentRequirements) {
        if ($requirement.id -notmatch '^BR-07-[A-Z]+-[0-9]{3}$') {
            $failures.Add("Current requirement id '$($requirement.id)' is not stable Architecture 0.7 form.")
        }

        if ($requirement.change -notmatch '^C[1-8]$' -or
            [string]::IsNullOrWhiteSpace($requirement.section) -or
            [string]::IsNullOrWhiteSpace($requirement.summary) -or
            @($requirement.appliesTo).Count -eq 0) {
            $failures.Add("Current requirement '$($requirement.id)' is incomplete.")
        }

        foreach ($predecessor in @($requirement.predecessors)) {
            if ($predecessor -notin $masterIds) {
                $failures.Add("Current requirement '$($requirement.id)' names unknown predecessor '$predecessor'.")
            }
        }
    }

    foreach ($change in 1..8 | ForEach-Object { "C$_" }) {
        if (@($currentRequirements | Where-Object change -eq $change).Count -eq 0) {
            $failures.Add("The current requirement vocabulary does not cover Architecture 0.7 change '$change'.")
        }
    }

    foreach ($matrixPath in $currentMatrixPaths) {
        $matrix = Read-JsonFile $matrixPath
        if ($null -eq $matrix) {
            continue
        }

        $stack = [string]$matrix.stack
        if ($matrix.schemaVersion -ne 1 -or $stack -notin @('Reference', 'Minimal')) {
            $failures.Add("Current matrix '$matrixPath' has an unsupported schema or stack name.")
            continue
        }

        $implementationStatus = @($registry.implementations | Where-Object stack -eq $stack)
        if ($implementationStatus.Count -ne 1) {
            $failures.Add("The status registry must contain exactly one '$stack' implementation entry.")
        }
        else {
            $delivery = $implementationStatus[0].currentDelivery
            $expectedMatrixPath = [string]$delivery.matrix.path
            $actualMatrixPath = $matrixPath.Substring($repositoryRoot.Length).TrimStart('\').Replace('\', '/')
            if ($expectedMatrixPath -cne $actualMatrixPath) {
                $failures.Add("$stack current matrix path does not match the status registry.")
            }
            elseif ((Get-CanonicalTextHash $matrixPath) -cne [string]$delivery.matrix.sha256) {
                $failures.Add("$stack current matrix content hash does not match the status registry.")
            }

            if ([string]$delivery.requirements.path -cne 'conformance/architecture-0.7-requirements.json') {
                $failures.Add("$stack current requirement path does not match the permanent current inventory.")
            }
            elseif ((Get-CanonicalTextHash $currentMasterPath) -cne [string]$delivery.requirements.sha256) {
                $failures.Add("$stack current requirement inventory hash does not match the status registry.")
            }

            foreach ($artifact in @('plan', 'ledger')) {
                $artifactRecord = $delivery.$artifact
                $artifactPath = Join-Path $repositoryRoot ([string]$artifactRecord.path)
                if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
                    $failures.Add("$stack current-delivery $artifact path is stale: '$($artifactRecord.path)'.")
                }
                elseif ((Get-CanonicalTextHash $artifactPath) -cne [string]$artifactRecord.sha256) {
                    $failures.Add("$stack current-delivery $artifact content hash does not match the status registry.")
                }
            }
        }

        foreach ($field in @('revision', 'status', 'path', 'sha256', 'reviewedCommit')) {
            if ([string]$matrix.architecture.$field -cne [string]$recordedArchitecture.$field) {
                $failures.Add("$stack current matrix architecture field '$field' does not match the current requirement vocabulary.")
            }
        }

        $entries = @($matrix.requirements)
        $entryIds = @($entries | ForEach-Object { $_.requirementId })
        foreach ($duplicate in ($entryIds | Group-Object | Where-Object Count -gt 1)) {
            $failures.Add("$stack current matrix duplicates requirement '$($duplicate.Name)'.")
        }

        $applicableIds = @(
            $currentRequirements |
                Where-Object { @($_.appliesTo) -contains $stack } |
                ForEach-Object { $_.id }
        )
        foreach ($missing in ($applicableIds | Where-Object { $_ -notin $entryIds })) {
            $failures.Add("$stack current matrix is missing applicable requirement '$missing'.")
        }

        foreach ($unknown in ($entryIds | Where-Object { $_ -notin $currentIds })) {
            $failures.Add("$stack current matrix contains unknown or stale requirement '$unknown'.")
        }

        foreach ($entry in $entries) {
            $id = [string]$entry.requirementId
            $requirement = @($currentRequirements | Where-Object id -eq $id)
            if ($entry.architectureRevision -ne '0.7') {
                $failures.Add("$stack/$id has stale current architecture revision '$($entry.architectureRevision)'.")
            }

            if ($requirement.Count -eq 1 -and [string]$entry.section -cne [string]$requirement[0].section) {
                $failures.Add("$stack/$id has a stale or missing architecture section '$($entry.section)'.")
            }

            if ($entry.classification -notin $allowedClassifications) {
                $failures.Add("$stack/$id has unsupported action classification '$($entry.classification)'.")
            }

            if ([string]::IsNullOrWhiteSpace($entry.component) -or
                [string]::IsNullOrWhiteSpace($entry.rationale)) {
                $failures.Add("$stack/$id must name a current component or disposition and rationale.")
            }

            if ($entry.status -notin $allowedStatuses) {
                $failures.Add("$stack/$id has unsupported current status '$($entry.status)'.")
            }

            $positive = @($entry.positiveEvidence)
            $negative = @($entry.negativeEvidence)
            Test-Evidence $positive $id 'positive'
            Test-Evidence $negative $id 'negative'

            if ($entry.status -in @('tested', 'demonstrated')) {
                $testEvidence = @($positive + $negative | Where-Object { $_.path -match '(^|/)tests/' })
                if ($testEvidence.Count -eq 0) {
                    $failures.Add("$stack/$id claims '$($entry.status)' without executable current-architecture evidence.")
                }
            }

            if ($entry.status -eq 'planned' -and ($positive.Count + $negative.Count) -gt 0) {
                $failures.Add("$stack/$id is planned but already names accepted evidence.")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Requirement evidence verification passed: $($requirements.Count) retained and $($currentMaster.requirements.Count) current stable requirements across $($matrixPaths.Count + $currentMatrixPaths.Count) stack matrices."
