$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$masterPath = Join-Path $repositoryRoot 'conformance\requirements.json'
$matrixPaths = @(
    Join-Path $repositoryRoot 'Reference\conformance\architecture-0.5.json'
    Join-Path $repositoryRoot 'Minimal\conformance\architecture-0.5.json'
)
$allowedStatuses = @('implemented', 'tested', 'demonstrated', 'planned', 'not-applicable')
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

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Requirement evidence verification passed: $($requirements.Count) stable requirements across $($matrixPaths.Count) stack matrices."
