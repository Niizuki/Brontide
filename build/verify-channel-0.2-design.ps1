param(
    [switch]$NegativeProbe
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$channelPath = Join-Path $repositoryRoot 'docs\future\channel'
$failures = [System.Collections.Generic.List[string]]::new()

$artifactNames = @(
    'README.md',
    'Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md',
    'Brontide-Channel-0.2-Capability-Contract-0.1.md',
    'Brontide-Channel-0.2-Session-State-Machine-0.1.md',
    'Brontide-Channel-0.2-Interaction-State-Machine-0.1.md',
    'Brontide-Channel-0.2-State-Event-Coverage-0.1.md',
    'Brontide-Channel-0.2-Responsibility-Matrix-0.1.md',
    'Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md',
    'Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md',
    'Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md',
    'reviews\README.md'
)

function Read-RequiredText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $channelPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Required Channel 0.2 design artifact does not exist: '$RelativePath'.")
        return ''
    }
    return Get-Content -Raw -LiteralPath $path -Encoding UTF8
}

function Assert-ContainsAll {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content,
        [Parameter(Mandatory = $true)][string[]]$Expected
    )

    foreach ($item in $Expected) {
        if ($Content.IndexOf($item, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("$Label is missing '$item'.")
        }
    }
}

foreach ($artifactName in $artifactNames) {
    Read-RequiredText $artifactName | Out-Null
}

$plan = Read-RequiredText 'Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md'
$contract = Read-RequiredText 'Brontide-Channel-0.2-Capability-Contract-0.1.md'
$session = Read-RequiredText 'Brontide-Channel-0.2-Session-State-Machine-0.1.md'
$interaction = Read-RequiredText 'Brontide-Channel-0.2-Interaction-State-Machine-0.1.md'
$stateEventCoverage = Read-RequiredText 'Brontide-Channel-0.2-State-Event-Coverage-0.1.md'
$responsibility = Read-RequiredText 'Brontide-Channel-0.2-Responsibility-Matrix-0.1.md'
$completeness = Read-RequiredText 'Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md'
$migration = Read-RequiredText 'Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md'
$neutralBrief = Read-RequiredText 'Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md'
$reviewReadme = Read-RequiredText 'reviews\README.md'

if ($NegativeProbe) {
    $contract = $contract.Replace('**Property C12-P1.**', '**Property C12-P1-REMOVED-BY-NEGATIVE-PROBE.**')
}

$capabilityMatches = [regex]::Matches($contract, '(?m)^## C([0-9]+)\s+[^\r\n]+$')
$actualCapabilities = @($capabilityMatches | ForEach-Object { [int]$_.Groups[1].Value })
$expectedCapabilities = @(1..12)
if (($actualCapabilities -join ',') -cne ($expectedCapabilities -join ',')) {
    $failures.Add("Capability headings must be exactly C1-C12 in order; found '$($actualCapabilities -join ',')'.")
}

$expectedProperties = @(1..12 | ForEach-Object { "**Property C$_-P1.**" })
Assert-ContainsAll 'Channel 0.2 capability contract properties' $contract $expectedProperties
if ([regex]::Matches($contract, '(?m)^\*\*Named scenarios\.\*\*').Count -ne 12) {
    $failures.Add('Every C1-C12 item must contain one Named scenarios paragraph.')
}
if ([regex]::Matches($contract, '(?m)^\*\*Silence\.\*\*').Count -ne 12) {
    $failures.Add('Every C1-C12 item must contain one explicit Silence paragraph.')
}

$sessionStates = @('unestablished', 'establishing', 'established', 'draining', 'closed', 'faulted')
Assert-ContainsAll 'Channel 0.2 session state machine' $session ($sessionStates | ForEach-Object { "| ``$_`` |" })
Assert-ContainsAll 'Channel 0.2 interaction state machine' $interaction @(
    '| `candidate` |',
    '| `admitting` |',
    '| `refused-local` |',
    '| `dispatched` |',
    '| `cancel-pending` |',
    '| `outcome-succeeded` |',
    '| `outcome-failed` |',
    '| `outcome-cancelled` |',
    '| `peer-fault` |',
    '| `lost` |'
)
Assert-ContainsAll 'Channel 0.2 recipient authority-denial path' $interaction @(
    '| `refused-local` | yes | Local policy denied a structurally valid request before dispatch; no peer frame is emitted. |',
    '| `validating` | structural/profile/state/class/direction/Shape/authority-structure/bound/replay/concurrency check fails | `rejected-protocol` | no |',
    '| `validating` | structurally valid authority presentation is denied by local policy | `refused-local` | no |'
)
Assert-ContainsAll 'Channel 0.2 recipient cancellation-refusal path' $interaction @(
    '| `executing` | structurally valid cancellation control is denied by local cancellation authority | `cancel-refused` | possible/already occurred; emit nonterminal `refused` acknowledgement |'
)
Assert-ContainsAll 'Channel 0.2 complete cancellation terminal paths' $interaction @(
    '| `dispatched`, `cancel-pending`, `cancel-accepted`, or `cancel-refused` | valid correlated peer protocol fault | `peer-fault` |',
    '| `executing`, `cancel-requested`, or `cancel-refused` | structurally invalid, unrecognized, unsupported, or wrongly scoped cancellation control | `peer-fault` | possible/already occurred; emit one interaction-scoped protocol fault and ignore a later handler terminal |'
)
Assert-ContainsAll 'Channel 0.2 live replay and recipient provenance paths' $interaction @(
    '| `executing`, `cancel-requested`, or `cancel-refused` | repeated request with the same accepted identity | `peer-fault` | possible/already occurred; no redispatch, emit `replay-detected`, and ignore a later handler terminal |',
    '| `peer-fault` | yes | One interaction-scoped peer protocol fault was committed; handler effects may already be possible. |',
    '| `lost` | yes | Local session or transport loss prevented a valid terminal commit; no peer statement is claimed. |',
    '| initiator `peer-fault` / recipient `peer-fault` / recipient `rejected-protocol` | no | yes | receipt/commit also observed locally |',
    '| initiator or recipient `lost` | no | no | yes |'
)
Assert-ContainsAll 'Channel 0.2 duplicate drain result' $session @(
    '| `draining` | duplicate local or peer drain control | `faulted` | session-scoped `state-violation`; preserve the original drain snapshot and all interaction effect evidence |'
)
Assert-ContainsAll 'Channel 0.2 cancellation acknowledgement totality' $interaction @(
    '| `cancel-accepted` | no | Peer accepted the one cancellation request; the interaction still awaits a terminal fact. |',
    '| `cancel-refused` | no | Peer refused the one cancellation request; ordinary execution still awaits success or failure. |',
    '| `dispatched` | unsolicited cancellation acknowledgement | `peer-fault` | `unknown`; emit/record interaction-scoped `state-violation` |',
    '| `cancel-pending` | cancellation `accepted` acknowledgement | `cancel-accepted` | acknowledgement is nonterminal and proves no effect fact |',
    '| `cancel-pending` | cancellation `refused` acknowledgement | `cancel-refused` | acknowledgement is nonterminal and proves no effect fact |',
    '| `cancel-accepted` or `cancel-refused` | any further cancellation acknowledgement | `peer-fault` | preserve possible effects; emit/record interaction-scoped `state-violation` |'
)
Assert-ContainsAll 'Channel 0.2 local phase refusal provenance' $interaction @(
    '| `validating` | receiver-local external phase predicate is `false` or `unknown` | `refused-local` | no |'
)
Assert-ContainsAll 'Channel 0.2 duplicate terminal fault action' $interaction @(
    '`late-traffic-fault` latch',
    '`clear`',
    '`fault-committed`',
    '`fault-unavailable`',
    'first duplicate semantic terminal or late non-fault control',
    'one interaction-scoped `state-violation` peer fault'
)
Assert-ContainsAll 'Channel 0.2 state/event coverage' $stateEventCoverage @(
    '## Closed-world totality rule',
    '## Session coverage grid',
    '## Initiator interaction coverage grid',
    '## Recipient interaction coverage grid',
    '## Late-traffic latch',
    'Every recognized event/state pair has exactly one route'
)

Assert-ContainsAll 'Channel 0.2 responsibility matrix' $responsibility @(
    'Channel contract version',
    'Session establishment/drain/close/fault',
    'Relational Initialisation phase',
    'Ready',
    'Release / ordinary gate',
    'Cancellation control and terminal meaning',
    'Transport/process loss classification',
    'Effect certainty',
    'Streaming and backpressure',
    'Long-running activity'
)
$ownershipSection = ($responsibility -split '## Ownership matrix', 2)[1] -split '## Selected boundary rulings', 2 | Select-Object -First 1
$ownershipRows = @($ownershipSection -split "`r?`n" | Where-Object { $_ -match '^\| ' -and $_ -notmatch '^\| ---' })
foreach ($row in $ownershipRows) {
    $cells = @($row.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -ge 2 -and $cells[0] -ne 'Concern' -and $cells[1] -notmatch '^`[a-z0-9-]+`$') {
        $failures.Add("Channel 0.2 responsibility owner must be one exact owner identifier: '$($cells[0])' has '$($cells[1])'.")
    }
}

$migrationVectorMatches = [regex]::Matches($migration, '(?m)^\| CH-([0-9]{2}) ')
$migrationVectorNumbers = @($migrationVectorMatches | ForEach-Object { [int]$_.Groups[1].Value })
if (($migrationVectorNumbers -join ',') -cne ((1..24) -join ',')) {
    $failures.Add("Migration ledger must disposition CH-01 through CH-24 exactly once in order; found '$($migrationVectorNumbers -join ',')'.")
}

Assert-ContainsAll 'Channel 0.1 protocol-category migration' $migration @(
    '`malformed-message`',
    '`unsupported-version`',
    '`unsupported-contract`',
    '`unsupported-kind`',
    '`unsupported-operation`',
    '`correlation-mismatch`',
    '`invalid-payload`',
    '`invalid-authority-presentation`',
    '`replay-detected`',
    '`limit-exceeded`',
    '`state-violation`',
    '`internal-protocol-failure`'
)
Assert-ContainsAll 'Channel 0.1 local-loss migration' $migration @(
    '`transport-unavailable`',
    '`transport-interrupted`',
    '`timeout`',
    '`peer-terminated`',
    '`peer-unavailable`',
    '`resource-exhausted`',
    '`unknown`'
)
Assert-ContainsAll 'Channel 0.1 failure-domain migration' $migration @(
    '`local-endpoint`',
    '`transport`',
    '`remote-endpoint`',
    '`remote-provider`'
)
Assert-ContainsAll 'Channel 0.1 limit migration' $migration @(
    '`maxFrameBytes`',
    '`maxNestingDepth`',
    '`maxRecordFields`',
    '`maxFragmentsPerRecord`',
    '`maxSequenceItems`',
    '`maxTextBytes`',
    '`maxByteStringBytes`',
    '`maxResourceBytes`',
    '`ioTimeoutMilliseconds`',
    '`maxConcurrentRequests`'
)
Assert-ContainsAll 'Channel 0.1 observation migration' $migration @(
    '`selectedProvider`',
    '`selectionReason`',
    '`negotiatedOperations`',
    '`negotiatedContractVersion`',
    '`representation`',
    '`crossedBoundaries`',
    '`copyCount`',
    '`referencedResources`',
    '`authorityDecisionPoint`',
    '`authorityDecision`',
    '`mappingObligations`',
    '`retryCount`',
    '`interrupted`',
    '`failureDomain`',
    '`terminalStatus`',
    '`providerEffectCount`',
    '`localCode`',
    '`localMessage`'
)
Assert-ContainsAll 'Channel 0.1 delivery fallback migration' $migration @(
    '| delivery `fallback` | **moved** | Delivery/retry facet observation; exact `none` remains a valid attributable value and Channel core does not infer another attempt. |'
)
$vectorMigrationSection = ($migration -split '## Channel 0.1 vector migration', 2)[1] -split '## New evidence required by redesign', 2 | Select-Object -First 1
$allowedDispositions = @('retained', 'replaced', 'moved', 'removed', 'legacy-only')
$allDispositionRows = @($migration -split "`r?`n" | Where-Object { $_ -match '^\| .* \| \*\*[^*]+\*\* \|' })
foreach ($row in $allDispositionRows) {
    $cells = @($row.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    $disposition = $cells[1].Trim('*').ToLowerInvariant()
    if ($disposition -notin $allowedDispositions) {
        $failures.Add("Channel 0.1 migration item '$($cells[0])' uses undeclared disposition '$disposition'.")
    }
}
$vectorRows = @($vectorMigrationSection -split "`r?`n" | Where-Object { $_ -match '^\| CH-[0-9]{2} ' })
foreach ($row in $vectorRows) {
    $cells = @($row.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    $disposition = $cells[1].Trim('*').ToLowerInvariant()
    if ($disposition -notin $allowedDispositions) {
        $failures.Add("Channel 0.1 vector migration '$($cells[0])' uses undeclared disposition '$disposition'.")
    }
}
$featureMigrationSection = ($migration -split '## Feature migration', 2)[1] -split '## Observation-field migration', 2 | Select-Object -First 1
$featureRows = @($featureMigrationSection -split "`r?`n" | Where-Object { $_ -match '^\| [^`-]' -and $_ -notmatch '^\| ---' -and $_ -notmatch '^\| 0\.1 feature ' })
foreach ($row in $featureRows) {
    $cells = @($row.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    $disposition = $cells[1].Trim('*').ToLowerInvariant()
    if ($disposition -notin $allowedDispositions) {
        $failures.Add("Channel 0.1 feature migration '$($cells[0])' uses undeclared disposition '$disposition'.")
    }
}

if ([regex]::Matches($plan, '(?m)^\| .*maintainers \| Confirm the proposed ruling:').Count -ne 0) {
    $failures.Add('The active redesign plan must not retain proposed owner rulings after owner resolution.')
}
Assert-ContainsAll 'Channel 0.2 resolved owner rulings' $plan @(
    'Core concurrency and cancellation:**',
    'Session-state ownership:**',
    'Relational initialization representation:**',
    'Extension invariants:**'
)
Assert-ContainsAll 'Channel 0.2 exact Ready ownership' $plan @(
    'Portable Binding owns Interconnection, Release, withdrawal, and cleanup; Composition owns the Relational Initialisation phase; Component Management owns Ready.'
)
Assert-ContainsAll 'Channel 0.2 Ready migration owner' $migration @(
    'readiness report carried by Portable Binding and semantically owned by Component Management',
    'Component Management external Ready fact; not Channel session state',
    'Component Management fact, separate from Channel establishment'
)
Assert-ContainsAll 'Channel 0.2 completeness review' $completeness @(
    '## Findings closed in the first-batch contract',
    '## Required silence probes and dispositions',
    '## Per-capability property audit',
    '## Residual review risks'
)
Assert-ContainsAll 'Channel 0.2 neutral brief' $neutralBrief @(
    '## Identity representation',
    '## Version and establishment rule',
    '## Vector format',
    '## Capability-wide property format',
    '## Golden policy',
    '## Batch 2 entry gate'
)
Assert-ContainsAll 'Channel 0.2 review policy' $reviewReadme @(
    'four owner rulings resolved',
    'fresh independent totality closure review is pending',
    '## Required review scope',
    '## Required verdicts',
    '## Closure',
    '## Exact next work',
    '`5cf42c4d97083324ffb8d6bd68491a145b8e611a`',
    '`channel-0.2-design-foundation-totality-closure-attestation.md`',
    '`channel-0.2-design-foundation-closure-record.md`',
    '`build/verify-channel-0.2-design.ps1`',
    '`build/verify-interchange.ps1`'
)

$reviewDirectory = Join-Path $channelPath 'reviews'
$reviewMarkdown = @(Get-ChildItem -LiteralPath $reviewDirectory -Filter '*.md' -File)
$expectedReviewNames = @('README.md', 'channel-0.2-design-foundation-attestation.md', 'channel-0.2-design-foundation-closure-attestation.md', 'channel-0.2-design-foundation-final-closure-attestation.md', 'channel-0.2-design-foundation-definitive-closure-attestation.md')
$actualReviewNames = @($reviewMarkdown.Name | Sort-Object)
if (($actualReviewNames -join ',') -cne (($expectedReviewNames | Sort-Object) -join ',')) {
    $failures.Add('The Channel 0.2 D1-D5 correction pin must retain exactly the review README and all four negative attestations before totality closure re-review.')
}

if (Test-Path -LiteralPath (Join-Path $repositoryRoot 'channel\0.2')) {
    $failures.Add('Channel 0.2 neutral schemas exist before first-batch owner/review closure.')
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "FAIL: $failure"
    }
    exit 1
}

Write-Host 'Channel 0.2 design-foundation verification passed: 11 required artifacts, C1-C12 with properties/scenarios/silence, total session/interaction event coverage, 6 session states, all 24 predecessor vectors dispositioned, 4 owner rulings resolved, and independent review still pending.'
