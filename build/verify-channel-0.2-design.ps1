param(
    [switch]$NegativeProbe
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$channelPath = Join-Path $repositoryRoot 'docs\future\channel'
$failures = [System.Collections.Generic.List[string]]::new()

# AV2. Every git call in this file redirects stderr with `2>$null`, and in Windows PowerShell that
# redirection wraps each stderr line in an ErrorRecord -- which this file's `Stop` preference turns
# into a TERMINATING error at the call. So a git invocation that fails does not hand a non-zero
# `$LASTEXITCODE` to the check written to read it: it kills the gate where it stands, every check
# after that point is skipped, and the exit code is 1, which is indistinguishable from a clean
# finding.
#
# It was proven on the historical-measure read below. Naming an unreadable revision died inside
# `Get-BlobText`, so the guard that reports exactly that condition -- "this verifier cannot be read
# at it" -- could never fire, and the probe written for that guard had been green on the crash rather
# than on the guard. That is AO1's class, a guard no input can reach, one level below where AO1 found
# it, and it was invisible because a probe read only the exit code.
#
# Lowering the preference for the duration of the call is what the guard harness already does around
# the gate it runs, for the same reason and after the same defect. Routed through one helper rather
# than repeated at seven call sites, because the next git call added here would otherwise reintroduce
# it.
function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        return (& git -C $repositoryRoot @Arguments 2>$null)
    }
    finally { $ErrorActionPreference = $previousPreference }
}

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

# Prose assertions must survive line wrapping: a normative sentence that reflows across a wrap is
# still the same sentence. Table-row assertions keep asserting against raw text, where the exact
# single-line form is the thing being pinned.
function Get-FlowedText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)

    return [regex]::Replace($Content, '\s+', ' ')
}

# Negative prose assertions need the whole sentence they govern. A character window can outgrow its
# key silently: forbidden text beyond the count looks identical to text that is absent, which is AQ5
# and the AS3/AS4 instances below.
function Get-SentenceAt {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content,
        [Parameter(Mandatory = $true)][int]$Index
    )

    if ($Index -lt 0 -or $Index -ge $Content.Length) { return '' }

    $start = 0
    $end = $Content.Length
    foreach ($boundary in [regex]::Matches($Content, '[.!?](?=\s|\z)')) {
        if ($boundary.Index -lt $Index) {
            $start = $boundary.Index + $boundary.Length
            continue
        }
        $end = $boundary.Index + $boundary.Length
        break
    }
    return $Content.Substring($start, $end - $start).Trim()
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
$channelReadme = Read-RequiredText 'README.md'
$reviewReadme = Read-RequiredText 'reviews\README.md'

# W3. An artifact's status block states what the artifact is and what it awaits, and nothing else.
# Its correction history is owned by the disposition index, which is a review record rather than a
# design artifact. Nine status blocks had reached 265 lines between them and none of it said what the
# artifact means -- it said what had once been wrong with it, which is surface every cold reviewer
# then has to read. The narrative checks below therefore read the index rather than the blocks: the
# text was moved verbatim, so each check asks the same question of the one file that now carries the
# answer instead of asking it of nine.
$dispositionIndex = Read-RequiredText 'reviews\channel-0.2-disposition-index.md'
# The Channel index's rows sit one directory above the status blocks, so both write the same relative
# pointer; it is declared here because both the row check and the block check resolve through it.
$dispositionLinkPattern = 'reviews/channel-0.2-disposition-index.md#'
$dispositionSections = @{}
# AN1. The section is also indexed by the anchor a pointer would use, because "the link resolves to a
# section" was the one of W3's four questions nothing asked: the lookup below is keyed by the artifact
# the section links to, so a status block could carry any anchor at all -- one that names no heading,
# or one that names another artifact's section -- and this check found the section by name and passed.
# Renaming a heading here left all nine pointers dead with every gate green, probed.
$dispositionAnchors = @{}
$dispositionAnchorCounts = @{}
foreach ($dispositionSection in [regex]::Matches($dispositionIndex, '(?ms)^## .+?$(.+?)(?=^## |\z)')) {
    $sectionLink = [regex]::Match($dispositionSection.Groups[1].Value, '\[([^\]]+\.md)\]')
    if ($sectionLink.Success) { $dispositionSections[$sectionLink.Groups[1].Value] = $dispositionSection.Groups[1].Value }
}
# The anchor a Markdown renderer derives from each heading, duplicate suffixes included, so a pointer
# is answered with the reader's question rather than a weaker one. Every heading level, not just the
# per-artifact `##` sections: two of the Channel index's rows point at `###` headings under the
# sections-with-no-design-artifact heading, and a check that indexed only `##` would fail them for
# pointing at a heading that is there. `build/verify-doc-links.ps1` asks the same resolution question
# across the whole repository; what is asked here is that the pointer resolves to *this artifact's*
# section, which a link checker has no way to know.
foreach ($dispositionHeading in [regex]::Matches($dispositionIndex, '(?ms)^#{2,6} (.+?)$(.+?)(?=^#{2,6} |\z)')) {
    $sectionSlug = [regex]::Replace($dispositionHeading.Groups[1].Value.Trim(), '\[([^\]]*)\]\([^)]*\)', '$1')
    $sectionSlug = $sectionSlug -replace '[*_`~]', ''
    $sectionSlug = ([regex]::Replace($sectionSlug, '[^\w\- ]', '')).ToLowerInvariant().Trim() -replace ' ', '-'
    if (-not $sectionSlug) { continue }
    if ($dispositionAnchorCounts.ContainsKey($sectionSlug)) {
        $dispositionAnchorCounts[$sectionSlug] = $dispositionAnchorCounts[$sectionSlug] + 1
        $sectionSlug = "$sectionSlug-$($dispositionAnchorCounts[$sectionSlug])"
    }
    else { $dispositionAnchorCounts[$sectionSlug] = 0 }
    $headingLink = [regex]::Match($dispositionHeading.Groups[2].Value, '\[([^\]]+\.md)\]')
    $dispositionAnchors[$sectionSlug] = if ($headingLink.Success) { $headingLink.Groups[1].Value } else { '' }
}
function Get-DispositionSection {
    param([Parameter(Mandatory = $true)][string]$ArtifactName)
    if ($dispositionSections.ContainsKey($ArtifactName)) { return $dispositionSections[$ArtifactName] }
    return ''
}
# Every surface whose whole statement about disposition is a pointer -- the nine status blocks and the
# eleven Channel index rows -- is checked through the pointer it carries rather than by looking the
# artifact up. `$ArtifactName` empty means the surface is not about one artifact, so any section of
# the index is a legitimate destination and only resolution is checked.
function Assert-DispositionPointer {
    param(
        [Parameter(Mandatory = $true)][string]$Surface,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$ArtifactName
    )

    foreach ($pointer in [regex]::Matches($Text, [regex]::Escape($dispositionLinkPattern) + '([^)\s]+)\)')) {
        $pointerAnchor = $pointer.Groups[1].Value.ToLowerInvariant()
        if (-not $dispositionAnchors.ContainsKey($pointerAnchor)) {
            $failures.Add("$Surface points at '#$pointerAnchor' in the disposition index and no heading there carries that anchor. The pointer is the whole of what this surface says about disposition, so one that lands nowhere leaves the reader with less than the history it replaced. This is AN1.")
            continue
        }
        if ($ArtifactName -and $dispositionAnchors[$pointerAnchor] -cne $ArtifactName) {
            $failures.Add("$Surface points at '#$pointerAnchor' in the disposition index, and that section is about '$($dispositionAnchors[$pointerAnchor])'. A pointer that resolves to the wrong artifact's history reads as an answer and is not one. This is AN1.")
        }
    }
}


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
Assert-ContainsAll 'Channel 0.2 invalid cancelled terminal' $interaction @(
    '| `executing` or `cancel-refused` | handler reports cancellation completed with no cancellation request in force | `peer-fault` | possible; commit one interaction-scoped `internal-channel-failure` and record the discarded handler terminal |',
    '| `dispatched` or `cancel-refused` | correlated cancelled Outcome | `peer-fault` | `unknown`; cancelled contradicts a history with no cancellation request in force |'
)
Assert-ContainsAll 'Channel 0.2 state/event coverage' $stateEventCoverage @(
    '## Closed-world totality rule',
    '## Session coverage grid',
    '## Initiator interaction coverage grid',
    '## Recipient interaction coverage grid',
    '## Late-traffic latch',
    'Every recognized event/state pair has exactly one route'
)

# R1: C8 and the recipient grid must agree about a cancellation control that arrives while the
# recipient is still admitting. The structural checks above cannot reach this: both artifacts are
# well formed and every Cn-P1 property stays green whichever provenance the event is given. This
# check compares what the two say about one event, which is the class T1 and R1 both belong to.
$recipientGridSection = ($stateEventCoverage -split '## Recipient interaction coverage grid', 2)[1] -split '## Late-traffic latch', 2 | Select-Object -First 1
$recipientGridRows = @($recipientGridSection -split "`r?`n" | Where-Object { $_ -match '^\| ' -and $_ -notmatch '^\| ---' -and $_ -notmatch '^\| Recipient state group' })
$validatingRow = @($recipientGridRows | Where-Object { $_ -match '^\| `validating` \|' })
$unseenRow = @($recipientGridRows | Where-Object { $_ -match '^\| `unseen` \|' })

# R3: the two states are not alike for cancellation control and may not share one verdict.
if ($validatingRow.Count -ne 1 -or $unseenRow.Count -ne 1) {
    $failures.Add('The recipient coverage grid must give `unseen` and `validating` separate rows, because a cancellation control correlates against a known identity in one and not the other.')
}
else {
    $validatingCells = @($validatingRow[0].Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    $unseenCells = @($unseenRow[0].Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($validatingCells.Count -ge 3) {
        $validatingCancelCell = $validatingCells[2]
        if ($validatingCancelCell.IndexOf('rejected-protocol', [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add('The recipient coverage grid must not route a cancellation control arriving during `validating` to `rejected-protocol`: the initiator cannot observe when the recipient reaches `executing`, so a conformant control would be faulted for losing an unobservable race.')
        }
        if ($validatingCancelCell.IndexOf('hold', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The recipient coverage grid must state that a cancellation control arriving during `validating` is held until admission resolves.')
        }
    }
    if ($unseenCells.Count -ge 3 -and $unseenCells[2].IndexOf('rejected-protocol', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The recipient coverage grid must keep a cancellation control for an unopened identity at `unseen` as `rejected-protocol`; holding state for an identity the recipient has never seen would let a peer allocate unbounded local state.')
    }
}

# The contract must own the rule rather than leaving the grid as its only normative statement.
Assert-ContainsAll 'Channel 0.2 cancellation admission race (C8)' (Get-FlowedText $contract) @(
    'A cancellation control that arrives while the recipient is still admitting the interaction is held, not faulted',
    'retains exactly one held control and applies it when admission resolves'
)

# The interaction machine must carry the two recipient rows the rule needs.
Assert-ContainsAll 'Channel 0.2 cancellation admission race (interaction machine)' $interaction @(
    '| `validating` | valid cancellation control for this admitted identity arrives | `validating` | no; hold exactly one control and apply it when admission resolves |',
    '| `validating` | all checks pass, dispatch boundary is crossed, and one held cancellation control applies | `cancel-requested` or `cancel-refused` | yes; dispatch precedes the held control, which is then evaluated under local cancellation authority |'
)

# R2: the two endpoint preconditions are two local states with no synchronising event between them.
Assert-ContainsAll 'Channel 0.2 cancellation precondition separation' (Get-FlowedText $interaction) @(
    'The two preconditions are local to their own endpoints and no event synchronises them'
)

# The completeness review must carry the recipient-side race in its silence inventory, next to the
# initiator-side one it already records.
Assert-ContainsAll 'Channel 0.2 completeness silence inventory' $completeness @(
    'cancel during recipient admission'
)

# S1: the R1 correction keeps `rejected-protocol` at `unseen` and relies on the coverage grid's
# assertion that a realization delivers one interaction's controls in commit order. The R1 check
# above compares whether four artifacts agree about one cell; it cannot reach S1, because every
# artifact is well formed and the disagreement is about who *owns* a fact rather than about what any
# of them says. This check asks the ownership question directly, and it is written to key off the
# grid's assertion rather than to assert any particular artifact exists: if the grid leans on
# intra-interaction delivery order, the contract must promise it, the capabilities that disclaim
# ordering must scope their disclaimers, and the matrix must name an owner and a crossing artifact a
# profile can actually check.
$flowedContract = Get-FlowedText $contract
$flowedResponsibility = Get-FlowedText $responsibility
$flowedCompleteness = Get-FlowedText $completeness

# The condition is the grid cell that *depends* on the ordering fact, not the sentence that asserts
# it. Keying off the assertion would let the check pass vacuously the moment someone deleted the
# sentence while leaving the `unseen` fault standing -- which is S1 restored with the evidence for it
# removed.
$unseenFaultsCancellation = $false
if ($unseenRow.Count -eq 1) {
    $unseenCancelCells = @($unseenRow[0].Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($unseenCancelCells.Count -ge 3) {
        $unseenFaultsCancellation = $unseenCancelCells[2].IndexOf('rejected-protocol', [System.StringComparison]::Ordinal) -ge 0
    }
}
if ($unseenFaultsCancellation) {
    if ($flowedContract.IndexOf('for one interaction identity, frames sent by one endpoint are delivered in the order that endpoint committed them', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The recipient grid faults a cancellation control at `unseen`, which is sound only if a conformant control cannot arrive there, but C4 does not promise the intra-interaction delivery order that makes it so. The fact the verdict depends on must be owned by the capability contract rather than asserted only in the coverage grid.')
    }
    if ($flowedContract.IndexOf('C4 promises neither fairness nor relative scheduling, transport ordering,', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add('C4 silence disclaims transport ordering without qualification while the grid relies on intra-interaction delivery order. One fact cannot be both promised and disclaimed; scope the silence to cross-interaction and cross-session ordering.')
    }
    if ($flowedContract.IndexOf('Channel core promises no retry, delivery, ordering, persistence, resumption, or exactly-once effect', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add('C11 disclaims ordering without qualification while the grid relies on intra-interaction delivery order. Scope the C11 non-promise so it does not contradict the promise C4 makes.')
    }
    if ($flowedResponsibility.IndexOf('Intra-interaction frame order', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The responsibility matrix has no row for intra-interaction frame order, so the fact the `unseen` verdict depends on has no owner. The matrix rule requires every semantic fact to have exactly one.')
    }
    if ($flowedResponsibility.IndexOf('per-interaction frame order', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The realization-profile crossing artifact declares no per-interaction frame order field, so a realization cannot state the obligation the grid places on it and a profile cannot verify it.')
    }

    # AB2: S1 was a fact a verdict depended on with no owner row. X5, Y1, and Y2 have now made the
    # local observation record load-bearing for `C4-P2` in exactly the same way -- it is what the
    # property reads -- and the matrix owns the peer fault, the loss classification, the effect
    # certainty and the observability system that *consumes* observations, while the observation
    # record itself has no row. The consumer is owned and the fact is not, which is S1 at the same
    # place in the same artifact.
    if ($flowedResponsibility.IndexOf('Local observation content', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The responsibility matrix has no row owning local observation content, although `C4-P2` now reads it and the matrix already owns the observability system that consumes it. A fact a property depends on with no owner row is the S1 defect, in the artifact S1 was raised against.')
    }
    if ($flowedContract.IndexOf('**Property C4-P2.**', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4 carries no property over the intra-interaction ordering promise. S1 survived seven review cycles because every Cn-P1 stayed green across it, so a new promise without a falsifiable property repeats exactly that failure.')
    }
    if ($flowedContract.IndexOf('`C4-control-precedes-request`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4 names no scenario in which a control is delivered before the request it names, which is the exact mutation Property C4-P2 must be able to fail on.')
    }
    if ($flowedCompleteness.IndexOf('intra-interaction frame order', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The completeness review silence inventory does not record the intra-interaction ordering promise, although its own residual risk 2 asks whether the design accidentally imports ordering promises.')
    }
}

# U1: the S1 correction gave the ordering fact an owner and attached `C4-P2` to it, but a promise is
# only as good as the property that can refute it. This check asks whether `C4-P2` can fail at all.
# It is keyed off the claim that *depends* on falsifiability -- C4 asserting that
# `C4-control-precedes-request` is the mutation the property must go red on -- rather than off the
# property's own wording, so deleting the claim cannot make the check pass while leaving an
# untestable promise standing.
#
# The defect it pins: `C4-P2` originally quantified over the frames a recipient *accepts*, and the
# design refuses every reordered frame rather than accepting it, so the accepted sequence is empty
# and trivially in order. The property was green on its own mutation. The fix quantifies over the
# refusal the reordering produces instead, which the fault routing manufactures rather than destroys.
# AP1: the key is the PROPERTY, not the sentence about it. This block was keyed to C4's assertion
# that `C4-control-precedes-request` is the mutation `C4-P2` must go red on, on the stated ground
# that deleting the claim could not silence the checks while leaving an untestable promise
# standing -- the promise went with the sentence. That was true when it was written and W2 ended
# it: the promise now lives in `conformance/channel-0.2-properties.json` and executes in the
# properties gate, so the sentence could be deleted, twenty-four checks here went silent, and
# both gates stayed green. Probed.
#
# The key is now C4-P2's own existence, which no one can delete quietly: the properties gate
# fails when the declaration names a property the stating artifact does not carry. The
# falsifiability sentence becomes the first thing checked rather than the thing that decides
# whether anything is checked -- an absent claim is loud instead of silencing, which is the
# direction AM1, AN1, AN2 and AO1 each ended in.
if ($flowedContract.IndexOf('Property C4-P2.', [System.StringComparison]::Ordinal) -ge 0) {
    if ($flowedContract.IndexOf('is the mutation this property must go red on', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4 no longer asserts which named mutation `C4-P2` must go red on. That assertion is what makes the property falsifiable in the design as well as in the gate, and it is what every check below is written against. This is U1''s own claim and AP1''s key.')
    }
    if ($flowedContract.IndexOf('stated over the refusal that reordering produces', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 claims `C4-control-precedes-request` is the mutation it must go red on, but it is not stated over the refusal that reordering produces. Quantified over the frames a recipient accepts, the property is green on that mutation: the reordered control is refused at `unseen` and the request is then latched, so the accepted sequence is empty and trivially an order-preserving subsequence.')
    }
    if ($flowedContract.IndexOf('for a cancellation control whose committing endpoint had already committed the request', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 does not forbid the observation `C4-control-precedes-request` actually produces: a recipient `rejected-protocol` at `unseen` for a cancellation control whose committing endpoint had already committed the request naming that identity. Without that conjunct the mutation leaves no witness the property can quantify over.')
    }
    # Asserted on the restriction rather than on the whole clause. AK6 renamed the second operand to
    # match the reference that publishes it -- "that endpoint's own frame the interaction's terminal
    # history was accepted on" -- and a check pinned to the old noun phrase would have failed for the
    # wrong reason on the pass that gave the operand a publisher. What is load-bearing here, and what
    # the message below is about, is that the operand is *that endpoint's own* frame.
    if ($flowedContract.IndexOf('before that endpoint''s own frame', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 covers only the initiator-to-recipient direction. Reordering the recipient''s acknowledgement and terminal produces a late-traffic `state-violation` instead, and the conjunct must be restricted to frames one endpoint committed, or a legal late control after a peer terminal would falsely fail it.')
    }

    # AC3: both conjuncts open with "no endpoint *records*" and then say "the same endpoint had
    # already committed". The nearest antecedent is the recording endpoint, and the recording
    # endpoint is never the committing one -- a recipient does not commit requests, and the
    # initiator does not commit the acknowledgement its own latch settles against. Read literally
    # each conjunct quantifies over an endpoint pair that cannot occur, which is a property that
    # cannot fail: U1's defect reintroduced by a pronoun. The reviewer this programme asks for
    # writes an evaluator from this prose, so the antecedent has to be in the prose.
    foreach ($ambiguous in @('whose request the same endpoint had already committed', 'a frame the same endpoint committed before')) {
        if ($flowedContract.IndexOf($ambiguous, [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add("C4-P2 says '$ambiguous'. Its nearest antecedent is the endpoint that *records* the refusal, which is never the endpoint that committed the frame, so the conjunct reads as one no vector can satisfy. The property must name the committing endpoint as the subject.")
        }
    }
    if ($flowedContract.IndexOf('the endpoint that committed the frame the refusal names, which is never the endpoint that records it', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 restricts both conjuncts to one endpoint''s own frames without saying which endpoint that is. The gloss has to be explicit, because the two candidate readings differ by whether the property can fail at all.')
    }
    if ($flowedContract.IndexOf('the vector is rejected as nonconforming evidence', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add('C4 gives `C4-control-precedes-request` an expected observation of being rejected as nonconforming evidence, which contradicts C4-P2 going red on it: a vector rejected before it executes is never evaluated by the property. The mutation needs one deterministic expected observation under C12-P1.')
    }
    $completenessAudit = ($completeness -split '## Per-capability property audit', 2)[1] -split '## Deliberate non-goals', 2 | Select-Object -First 1
    if ($completenessAudit -and (Get-FlowedText $completenessAudit).IndexOf('C4-P2', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The per-capability property audit does not register C4-P2 or the mutation that must fail it. That table is the register of property/mutation pairs, and its silence is why an unfalsifiable property survived the correction that introduced it.')
    }

    # V1: C4-P2's first conjunct turns on *which* refusal the recipient recorded. A property can only
    # quantify over facts the parity profile actually compares, and that list carries the peer-fault
    # category alone -- the detailed reason distinguishing a control for an unopened identity from the
    # other correlation faults lives in the migration ledger's prose and is normative nowhere.
    $flowedBrief = Get-FlowedText $neutralBrief
    if ($flowedBrief.IndexOf('peer-fault detailed reason', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The neutral brief compares only the peer-fault category, so C4-P2 cannot distinguish a `rejected-protocol` caused by a cancellation control at `unseen` from any other `invalid-interaction-correlation`. A property may only quantify over facts the parity profile makes normative.')
    }

    # AC2: V1 made the detailed reason normative "wherever its category declares a closed set of
    # them" and named the C4-P2 case as one detailed reason of `invalid-interaction-correlation`.
    # The only artifact that declares that set is the migration ledger, and its five values --
    # missing, extra, wrong-session, reused, mismatched -- contain nothing for an identity that was
    # never accepted. The set is closed and the value the conjunct reads is not in it, which is X1's
    # `state-violation` finding one category over: there the category declared no reason set, here it
    # declares one without the reason. V1 quoted those five values and did not ask whether they
    # covered the case.
    # Scoped to the category table rather than the artifact: mutation testing found the
    # phrase-anywhere form satisfied by the ledger's own status block, which is a claim about the
    # document rather than the closed set the document has to declare. U3 and X1 were weak the same
    # way and were scoped for the same reason.
    $migrationFaultCategories = Get-FlowedText ((($migration -split '## Protocol-fault category migration', 2)[1] -split '## Local loss category migration', 2 | Select-Object -First 1))
    if (-not $migrationFaultCategories -or $migrationFaultCategories.IndexOf('`unopened-interaction-identity`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The migration ledger declares the closed detailed-reason set for `invalid-interaction-correlation` as missing, extra, wrong-session, reused, or mismatched identities. A cancellation control naming an identity the recipient never accepted is none of them, and it is the reason C4-P2''s first conjunct quantifies over, so the compared field has no value for the refusal the property reads.')
    }
    $briefParityReasons = Get-FlowedText ((($neutralBrief -split '## Observation and parity profile', 2)[1] -split 'Excluded by default', 2 | Select-Object -First 1))
    if ($briefParityReasons -and $briefParityReasons.IndexOf('`unopened-interaction-identity`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The parity profile names the C4-P2 refusal as "one detailed reason of `invalid-interaction-correlation`" without naming which. A reason identified only by description is not a compared value; the closed set has to be reachable by name.')
    }
    # The conjunct quantifies over a *cancellation control*, and both `unseen` cells record the one
    # provenance and now the one detailed reason. Which frame was refused is therefore a fact the
    # property reads and C10 does not require, which is Y1's defect on the other conjunct.
    # Scoped to C10, which owns observation content, for the reason above: the contract's status
    # block names this correction and would satisfy a phrase-anywhere check with C10 reverted.
    # ASCII-only split anchors: this script has no byte-order mark, so Windows PowerShell 5.1 reads
    # it as ANSI and a literal em dash in a pattern would never match the UTF-8 artifacts.
    $contractC10 = Get-FlowedText ((($contract -split '(?m)^## C10 ', 2)[1] -split '(?m)^## C11 ', 2 | Select-Object -First 1))
    if (-not $contractC10 -or $contractC10.IndexOf('the kind of frame refused', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C10 requires the observation of a frame that opens no interaction to record the refusal and its provenance, and not which kind of frame was refused. C4-P2''s first conjunct quantifies over a cancellation control specifically, and the grid gives the cancellation-control and other-control cells at `unseen` the same provenance, so the property reads a distinction the observation is not required to carry.')
    }

    # V2: the mutation must be executable. The neutral provider is authorised to inject faults and
    # loss; `C4-control-precedes-request` needs deterministic reordering, which no artifact permits.
    # A property whose named mutation cannot be run is unfalsifiable in practice, which is U1 again
    # one layer down at the evidence boundary.
    if ($flowedBrief.IndexOf('reordering injection', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The neutral provider boundary authorises deterministic fault/loss injection only, so no endpoint in the evidence set can produce `C4-control-precedes-request`. C4-P2 would carry a named mutation that nothing is permitted to execute.')
    }

    # W1: the property must be writable in the form the brief requires of every property. C4-P2 turns
    # on "had already committed" and "committed before", which are precedence comparisons between two
    # positions in one endpoint's declared step sequence. The closed operator set offers equality,
    # membership, counts, transition edges, set uniqueness, implication and bounded for-all -- no
    # ordering relation at all. U1 was a property that could not fail and V2 a mutation that could not
    # run; this is the same family again, at the property language.
    $briefOperators = ($neutralBrief -split '## Capability-wide property format', 2)[1] -split '## Observation and parity profile', 2 | Select-Object -First 1
    if (-not $briefOperators -or (Get-FlowedText $briefOperators).IndexOf('precedence between two steps', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The closed property operator set has no ordering relation, so C4-P2''s "had already committed" and "committed before" cannot be expressed in the form the brief requires of every property. A property that cannot be written is not falsifiable however well it is worded.')
    }

    # W2: a mutation run has to establish before it can reorder, and C4 requires a realization to
    # declare per-interaction frame order at establishment. Nothing said what the injecting
    # realization declares, so the fixture was either internally inconsistent or unable to establish.
    if ($flowedBrief.IndexOf('declares per-interaction frame order and then violates it', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('Nothing states what the reordering realization declares at establishment. C4 requires the declaration, so an injecting provider either declares conformance and lies -- which must be said, because it is the whole reason the property is needed alongside the declaration -- or cannot establish and the mutation cannot run.')
    }

    # W3: C4-P2 has two conjuncts and one named mutation, and that mutation exercises the
    # initiator-to-recipient direction only. C12 requires every property to be failable against a
    # named incorrect implementation; half a property with no named mutation is half unfalsifiable.
    if ($flowedContract.IndexOf('`C4-outcome-precedes-ack`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2''s second conjunct covers the recipient-to-initiator direction and has no named mutation. `C4-control-precedes-request` reorders a request and a control, which the first conjunct catches; nothing named reorders an acknowledgement and its terminal.')
    }

    # W4: the mutation's expected observation depends on what the recipient keeps after refusing a
    # control at `unseen`. Only accepted identities enter the replay set, so an identity refused at
    # `unseen` was never accepted -- yet the late-traffic latch belongs to "every terminal
    # interaction", which would mean retaining state for identities a peer never opened. That is the
    # exact exposure the R1 ruling refused when it rejected holding at `unseen`.
    if ($flowedContract.IndexOf('retains no interaction history and no latch', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('Nothing says whether a recipient retains a terminal history for an identity refused at `unseen`. Retaining one lets a peer accrue unbounded state by naming identities it never opens, which is what the R1 ruling refused; not retaining one must be stated, because the late-traffic latch otherwise claims every terminal interaction.')
    }

    # The retention rule is exactly the kind of fact S1 was raised about: stated in one artifact and
    # contradicted by another. The interaction machine says every terminal interaction owns a latch
    # and the grid routes every terminal through it, so both must carry the exception or C4 disagrees
    # with them.
    $flowedInteraction = Get-FlowedText $interaction
    $flowedCoverage = Get-FlowedText $stateEventCoverage
    if ($flowedInteraction.IndexOf('An identity refused at `unseen` is not a terminal interaction', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The interaction machine gives every terminal interaction a late-traffic latch without excepting an identity refused at `unseen`, which C4 says retains nothing. One fact, two artifacts, two answers.')
    }
    if ($flowedCoverage.IndexOf('retains no history and no latch', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The recipient coverage grid routes an `unseen` cancellation control to `rejected-protocol` and routes every terminal through the late-traffic latch, without recording that this particular refusal retains nothing.')
    }
    if ($flowedCompleteness.IndexOf('`C4-outcome-precedes-ack`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The per-capability property audit registers only one of C4-P2''s two named mutations.')
    }

    # W5: the precedence operator is defined "for one endpoint", but the vector format records only
    # "ordered stimulus steps" with no committing endpoint. The operator's operand does not exist in
    # the vector schema, so the property still cannot be written -- W1 solved in the operator set and
    # unsolved in the data the operator reads.
    $briefVectorFormat = ($neutralBrief -split '## Vector format', 2)[1] -split '## Vector groups', 2 | Select-Object -First 1
    if (-not $briefVectorFormat -or (Get-FlowedText $briefVectorFormat).IndexOf('committing endpoint', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The vector format records ordered stimulus steps without saying which endpoint committed each one. C4-P2''s precedence relation is defined for one endpoint''s own frames, so without that attribution the operator has no operand and the property is unwritable for the same reason W1 named.')
    }

    # W6: the coverage grid requires every generated cell to assert the late-traffic latch, and the
    # normative parity profile does not compare it. C4-P2's second conjunct quantifies over a latched
    # `state-violation`, so the fact it reads is required as evidence in one artifact and absent from
    # the comparison set in another -- the V1 defect again, on the other conjunct.
    $briefParity = ($neutralBrief -split '## Observation and parity profile', 2)[1] -split 'Excluded by default', 2 | Select-Object -First 1
    if (-not $briefParity -or (Get-FlowedText $briefParity).IndexOf('late-traffic latch', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The state/event grid requires every generated cell to assert the late-traffic latch, but the normative parity profile never compares it. C4-P2''s second conjunct reads a latched `state-violation`, so the fact is demanded as evidence and excluded from comparison at the same time.')
    }

    # X1: W6 made the latch *value* comparable, and the second conjunct does not read the latch value.
    # It reads which frame the latch settled against, and which endpoint committed that frame. The
    # latch is a three-valued enum that names nothing, and the migration ledger declares no detailed
    # reason set for `state-violation`, so V1's detailed-reason clause -- conditional on the category
    # declaring one -- does not reach this conjunct either. The mutation the conjunct must fail on and
    # the two cases the contract says it must stay green on record the identical comparable
    # observation: terminal preserved, category `state-violation`, latch `fault-committed`.
    if ($flowedContract.IndexOf('`state-violation` latched against a frame', [System.StringComparison]::Ordinal) -ge 0) {
        if (-not $briefParity -or (Get-FlowedText $briefParity).IndexOf('frame that settled the latch', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The parity profile compares the late-traffic latch value but not the frame that settled it. C4-P2''s second conjunct forbids a latch settled against a frame the same endpoint committed before the terminal frame, while a legal late control after a peer''s terminal and a duplicate terminal from a nonconformant peer must both leave it green -- and all three record `state-violation` with `fault-committed`. The conjunct cannot separate the mutation it must fail on from the cases it must not.')
        }
        # Scoped to the section that has to carry the rule, not to the artifact: mutation testing found
        # the phrase-anywhere form satisfied by this artifact's own status block, which is a claim
        # about the document rather than the rule the document must state. U3's check was weak the
        # same way and was scoped for the same reason.
        $latchSection = ($interaction -split '## Late terminal and control disposition', 2)[1] -split '## Interaction event totality', 2 | Select-Object -First 1
        if (-not $latchSection -or (Get-FlowedText $latchSection).IndexOf('records the frame that settled it', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The interaction machine settles the late-traffic latch without recording which frame settled it, so the fact the parity profile is asked to compare is not produced by the machine that owns the latch.')
        }
    }

    # X2: W4 created a route that reaches no terminal interaction, and the grid still requires every
    # generated cell to assert a latch while W6 makes that value a normative comparison. A cell with a
    # required field and no value is the silence Decision 10 names: two independent implementations
    # pick `clear` and absent, and every cross-stack comparison passes.
    $coverageEvidence = ($stateEventCoverage -split '## Evidence required', 2)[1]
    if ($flowedCoverage.IndexOf('retains no history and no latch', [System.StringComparison]::Ordinal) -ge 0 -and
        $coverageEvidence -and (Get-FlowedText $coverageEvidence).IndexOf('late-traffic latch', [System.StringComparison]::Ordinal) -ge 0) {
        if ((Get-FlowedText $coverageEvidence).IndexOf('not-applicable', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The grid requires every generated cell to assert the late-traffic latch and W4 gave one cell no latch at all. The absent value must be an explicit `not-applicable`, or two implementations will differ between absent and `clear` with nothing to catch it.')
        }
    }

    # X3: the grid is not the detailed authority and says so. The recipient transition table -- which
    # is -- has exactly one row from `unseen`, for a request. A cancellation control at `unseen` has no
    # detailed row, so the machine's own totality rule routes it to an interaction-scoped
    # `state-violation` and terminal `peer-fault`, which is a terminal interaction, which owns a latch.
    # That is three contradictions of W4 and of the grid cell, about the exact event C4-P2's first
    # conjunct quantifies over.
    if ($unseenFaultsCancellation) {
        $recipientTransitions = ($interaction -split '## Recipient transitions', 2)[1] -split '## Admission order', 2 | Select-Object -First 1
        $unseenTransitionRows = @($recipientTransitions -split "`r?`n" | Where-Object { $_ -match '^\| `unseen` \|' -and $_.IndexOf('rejected-protocol', [System.StringComparison]::Ordinal) -ge 0 })
        if ($unseenTransitionRows.Count -lt 1) {
            $failures.Add('The recipient transition table has no row for a recognized peer event at `unseen`, so the interaction machine''s own totality rule sends it to `state-violation` and terminal `peer-fault` -- a terminal interaction, which owns a latch. The grid says `rejected-protocol` with no latch and no history, and the grid is not the detailed authority.')
        }
    }

    # X4: W3 added the second mutation so the recipient-to-initiator conjunct has something to fail
    # on. U3 added the vector group that makes the first mutation exist in Batch 2. Nothing added the
    # second to that group, so half of W3 does not reach the vector suite -- U3 one layer down.
    if ($flowedContract.IndexOf('`C4-outcome-precedes-ack`', [System.StringComparison]::Ordinal) -ge 0) {
        $briefVectorGroups = ($neutralBrief -split '## Vector groups', 2)[1] -split '## Capability-wide property format', 2 | Select-Object -First 1
        if (-not $briefVectorGroups -or (Get-FlowedText $briefVectorGroups).IndexOf('`C4-outcome-precedes-ack`', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The required adversarial vector groups name `C4-control-precedes-request` and not `C4-outcome-precedes-ack`, so nothing requires the second conjunct''s mutation to be written. A named mutation absent from the required groups is a mutation no suite has to contain.')
        }
    }

    # X5: W4 says the recipient "keeps nothing" for an identity refused at `unseen`, and C4-P2's first
    # conjunct quantifies over what an endpoint *records* there. Either the witness does not exist or
    # "keeps nothing" is false, and which one is meant is the difference between a bounded refusal and
    # an unfalsifiable property. The distinction that reconciles them -- evidence recorded once and
    # never consulted, versus per-identity state a later decision reads -- is stated nowhere, and the
    # machine's terminal-provenance table covers only terminal histories, which W4 says this is not.
    if ($flowedContract.IndexOf('retains no interaction history and no latch', [System.StringComparison]::Ordinal) -ge 0) {
        if ($flowedContract.IndexOf('never consulted by a later admission', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('C4 says the `unseen` refusal keeps nothing and C4-P2 quantifies over the refusal the recipient records there. One local observation is evidence and not the per-identity state the R1 ruling refused, but only because nothing consults it -- and no artifact says so, so the property''s witness is a record the contract has just abolished.')
        }
        if ($flowedInteraction.IndexOf('for an identity never accepted', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The interaction machine''s terminal-provenance table has no row for the `unseen` refusal, because W4 says it is not a terminal history. The table is where an observation''s provenance is fixed, so the one record C4-P2 reads has no declared provenance.')
        }
        if ($flowedCoverage.IndexOf('local observation is recorded', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The grid states what the `unseen` refusal retains and not what it records, so the cell the generated recipient model enumerates has no observation to assert.')
        }

        # Y2: C10 owns what an observation must be sufficient to distinguish, and its scope sentence
        # covers "every attempted establishment and interaction". A control naming an identity never
        # accepted is neither: W4 and X3 both insist no interaction exists there. So the one record X5
        # relies on is required by C4 and not by the capability that owns observation.
        if ($flowedContract.IndexOf('recognized frame that opens no interaction', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('C10 requires an observation for every attempted establishment and interaction, and the `unseen` refusal is neither -- no interaction exists there, which is the whole of W4. The record C4-P2 reads is therefore mandated by C4 alone and not by the capability that owns observation content.')
        }
    }

    # Y1: a property may only read facts the observation carries. C10 enumerates what every
    # observation must distinguish and the brief's local-observation schema enumerates what it holds;
    # neither names the late-traffic latch or the frame that settled it, which W6 and X1 have now made
    # normative comparisons. This is V1's defect at the schema boundary rather than the parity list:
    # a compared field that no observation is required to carry is compared between two absences.
    if ($briefParity -and (Get-FlowedText $briefParity).IndexOf('frame that settled the latch', [System.StringComparison]::Ordinal) -ge 0) {
        if ($flowedContract.IndexOf('late-traffic latch and the frame that settled it', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('C10 does not require an observation to distinguish the late-traffic latch or the frame that settled it, although the parity profile now compares both and C4-P2 reads them. C10 owns observation content; a fact compared but not owned is the S1 shape again.')
        }
        $briefObservationSchema = ($neutralBrief -split '### Local observation', 2)[1] -split '## External phase and authority inputs', 2 | Select-Object -First 1
        if (-not $briefObservationSchema -or (Get-FlowedText $briefObservationSchema).IndexOf('late-traffic latch', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The local-observation schema records provenance, state, admission decisions, dispatch boundary, terminal form, detection point, and effect certainty -- and not the latch or its settling frame. Batch 2 would author a schema with no position for the fields the parity profile compares.')
        }

        # Y4: kind, identity, and committing endpoint do not identify *which* frame settled the latch
        # when one endpoint commits two frames of the same kind for one interaction -- which is
        # precisely the duplicate-terminal case the property must leave green. Bound to the first
        # matching step it reads "committed before the terminal frame" and goes red on legal input.
        if ((Get-FlowedText $briefParity).IndexOf('arrival ordinal', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The settling-frame reference names kind, interaction identity, and committing endpoint, which do not distinguish two frames of the same kind from one endpoint. A duplicate terminal is exactly that, and it is a case C4-P2 must leave green; bound to the earlier of the two steps the property goes red on legal input. The reference needs the settling frame''s arrival ordinal within the interaction.')
        }

        # AC1: Y4 added the arrival ordinal to the neutral brief and to nothing else. The brief
        # declares itself subordinate to the contract, both state machines, and the grid -- "if a
        # convenient schema shape contradicts them, the schema changes" -- and the interaction
        # machine, which owns the latch, and the grid, which enumerates the cells that assert it,
        # both still name the three fields X1 gave them. The hierarchy therefore resolves the
        # conflict against Y4: Batch 2 reads the machine, writes three fields, and the duplicate
        # terminal the property must leave green stops being decidable, which is the whole of what
        # Y4 was raised about. Scoped to the sections that produce the fact rather than to the
        # artifacts, for the reason X1's own check was scoped that way.
        $machineLatchSection = ($interaction -split '## Late terminal and control disposition', 2)[1] -split '## Interaction event totality', 2 | Select-Object -First 1
        if (-not $machineLatchSection -or (Get-FlowedText $machineLatchSection).IndexOf('arrival ordinal', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The interaction machine records the settling frame as kind, interaction identity, and committing endpoint, while the parity profile compares those three and an arrival ordinal. The machine owns the latch and the brief is subordinate to it, so the contradiction resolves against the ordinal Y4 added -- and without it a duplicate terminal cannot be told from a reordering.')
        }
        $coverageLatchSection = ($stateEventCoverage -split '## Late-traffic latch', 2)[1] -split '## Evidence required', 2 | Select-Object -First 1
        if (-not $coverageLatchSection -or (Get-FlowedText $coverageLatchSection).IndexOf('arrival ordinal', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The grid''s late-traffic latch section names the settling frame''s three X1 fields and not the arrival ordinal, so the generated models enumerate cells that assert less than the parity profile compares.')
        }
        $observationOwnerRow = @($responsibility -split "`r?`n" | Where-Object { $_ -match '^\| Local observation content' })
        if ($observationOwnerRow.Count -eq 1 -and $observationOwnerRow[0].IndexOf('arrival ordinal', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The owner row AB2 added for local observation content lists the latch, its `not-applicable` value, and the settling frame, and not the settling frame''s arrival ordinal. The crossing artifact must carry every field the parity profile compares, or the owned fact and the compared fact are different facts.')
        }

        # Z1: W1 made the precedence relation deliberately narrow -- declared steps only, never an
        # observed time, arrival order, or cross-endpoint relation -- because Channel promises no
        # order across endpoints and owns no clock. Y4 then made an arrival ordinal a compared
        # normative field. It is there to identify which frame settled the latch, and nothing says so,
        # so the property language now has an observed-order operand of exactly the kind W1 excluded.
        if ($briefOperators -and (Get-FlowedText $briefOperators).IndexOf('never as an ordering operand', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The settling frame''s arrival ordinal is a compared normative field and the property format does not restrict how it may be used. W1 excluded observed arrival order from the operator set on purpose; an ordinal that may be an ordering operand hands it back, and a property could then assert an order Channel does not promise.')
        }

        # Z3: X2 introduced `not-applicable` and the parity profile compares it. Y1 gave C10 the latch
        # and the settling frame and stopped at "the terminal interaction's" latch, so the one value a
        # non-terminal route asserts is compared and unowned -- Y1's own defect, surviving in the
        # corner Y1 did not sweep.
        if ($flowedContract.IndexOf('`not-applicable`', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('C10 requires an observation to distinguish the terminal interaction''s latch, and the parity profile also compares the `not-applicable` value a route reaching no terminal interaction asserts. That value is compared and owned by nothing, which is what Y1 was raised about.')
        }
    }

    # Z4: the ledger's new-evidence inventory is where Batch 2 learns which 0.2 cases have no 0.1
    # predecessor, and intra-interaction frame order is the newest requirement in the batch and the
    # subject of every finding since S1. It is absent, so the one evidence group this whole sequence
    # exists to produce is missing from the inventory that lists what must be built.
    if ($flowedContract.IndexOf('`C4-outcome-precedes-ack`', [System.StringComparison]::Ordinal) -ge 0) {
        $ledgerNewEvidence = ($migration -split '## New evidence required by redesign', 2)[1] -split '## Golden encodings, parity profiles, and pins', 2 | Select-Object -First 1
        if (-not $ledgerNewEvidence -or (Get-FlowedText $ledgerNewEvidence).IndexOf('intra-interaction frame order', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The migration ledger lists the 0.2 cases with no 0.1 equivalent and does not list intra-interaction frame order or its two ordering mutations. The requirement every finding since S1 turns on is absent from the inventory of what Batch 2 must build, and it has no 0.1 predecessor to carry it in by another route.')
        }
    }

    # Y3: X3 routes the `unseen` refusal to `rejected-protocol`, and the recipient state table marks
    # `rejected-protocol` terminal. The machine's `any terminal` rows then claim the identity and apply
    # the late-traffic latch -- the state W4 refuses and the grid says the `any terminal` row does not
    # reach. Adding the row fixed the routing and left the destination contradicting the rule.
    if ($unseenFaultsCancellation) {
        # Z2: Y3 settled that the refusal leaves the recipient at `unseen` with `rejected-protocol` as
        # provenance. The grid's `unseen` cells still read as a next state, in the same column format
        # every other row uses for one -- one token, two meanings, two artifacts, which is S1's shape
        # and the reason the grid needed an owner in the first place.
        # Scoped to the cancellation-control cell rather than the row: mutation testing found the
        # row-wide form satisfied by the neighbouring `Other peer event` cell, so reverting the cell
        # under test left the check green.
        $unseenCancellationCell = ''
        if ($unseenRow.Count -eq 1) {
            $unseenCancellationCells = @($unseenRow[0].Trim('|').Split('|') | ForEach-Object { $_.Trim() })
            if ($unseenCancellationCells.Count -ge 3) { $unseenCancellationCell = $unseenCancellationCells[2] }
        }
        if ($unseenCancellationCell.IndexOf('unchanged', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The recipient grid''s `unseen` row names `rejected-protocol` in the cell format every other row uses for a next state, while the interaction machine says the state is unchanged and `rejected-protocol` is the provenance the refusal is recorded under. The grid must say which it means.')
        }
        if ($flowedInteraction.IndexOf('per-identity state remains `unseen`', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The `unseen` refusal is routed to `rejected-protocol`, which the recipient state table marks terminal, so the machine''s `any terminal` rows apply and give the identity a late-traffic latch. W4 refuses exactly that state and the grid says the `any terminal` row does not reach it. The machine must say what state the recipient is left in, and the only answer consistent with retaining nothing is `unseen`.')
        }
    }
}

# S2: a held control needs a disposition for the third exit from `validating`. Admission succeeding
# and admission refusing are the only two C8 enumerates; loss and drain are neither.
Assert-ContainsAll 'Channel 0.2 held control under loss or drain (C8)' $flowedContract @(
    'If the session or transport is lost, or drain refuses the interaction, while a control is held'
)
Assert-ContainsAll 'Channel 0.2 held control under loss (interaction machine)' $interaction @(
    '| `validating` | local session or transport loss, with or without a held cancellation control | `lost` | no; any held control is discarded with no answering frame and the late-traffic latch does not fire |',
    '| `validating` | drain refuses this still-admitting interaction, with or without a held cancellation control | `refused-local` | no; an interaction whose admission has not resolved is outside the drain snapshot, and any held control is discarded with no answering frame |'
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

# U2: B3 required one owner identifier per row and got it, but nothing kept the *vocabulary* closed.
# The S1 correction introduced `channel-core` for a fact whose contract family every other row calls
# `channel`, so one owner acquired two names and a Batch 2 ownership inventory keyed by identifier
# would read them as two owners. The matrix must therefore declare its identifiers and use only those.
$ownerGlossarySection = ($responsibility -split '## Owner identifiers', 2)[1]
if (-not $ownerGlossarySection) {
    $failures.Add('The responsibility matrix declares no owner-identifier vocabulary, so nothing stops a correction inventing a synonym for an existing owner. B3 fixed one owner per row; it did not fix one name per owner.')
}
else {
    $ownerGlossarySection = ($ownerGlossarySection -split '## Ownership matrix', 2) | Select-Object -First 1
    $declaredOwners = @([regex]::Matches($ownerGlossarySection, '(?m)^- `([a-z0-9-]+)`') | ForEach-Object { $_.Groups[1].Value })
    $usedOwners = @($ownershipRows | ForEach-Object { ($_.Trim('|').Split('|')[1]).Trim().Trim('`') } | Where-Object { $_ -match '^[a-z0-9-]+$' } | Sort-Object -Unique)
    foreach ($usedOwner in $usedOwners) {
        if ($declaredOwners -notcontains $usedOwner) {
            $failures.Add("Channel 0.2 responsibility owner '$usedOwner' is used in the ownership matrix but is not declared in the owner-identifier vocabulary.")
        }
    }
    if ($declaredOwners -contains 'channel' -and $declaredOwners -contains 'channel-core') {
        $failures.Add('The owner vocabulary declares both `channel` and `channel-core`. They name one contract family, and two identifiers for one owner is the duplicate the neutral ownership inventory must reject.')
    }
}

# U3: the neutral brief is where every Batch 2 boundary is fixed, and the S1 correction created an
# establishment-time obligation and a mutation vector that it never carried. V1 and V2 paid part of
# this; the establishment declaration and the vector group are the rest.
# Scoped to the establishment section rather than the whole brief: a phrase-anywhere check here is
# satisfied by the status block, which is a claim about the artifact rather than the rule the artifact
# has to state. Mutation-testing this check is what exposed that -- it stayed green with the
# establishment paragraph deleted.
$briefEstablishment = ($neutralBrief -split '## Version and establishment rule', 2)[1] -split '## Message-schema separation', 2 | Select-Object -First 1
if (-not $briefEstablishment -or (Get-FlowedText $briefEstablishment).IndexOf('per-interaction frame order', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The neutral brief''s establishment rule does not carry the per-interaction frame order declaration, although C4 requires a profile to check it at establishment and the responsibility matrix makes it the crossing artifact. The brief fixes the established-profile boundary, so an obligation absent from it does not reach Batch 2.')
}
# This check read the whole brief until W3 moved the status blocks out, and the only passage carrying
# its phrase was the status block's own account of what the V-Z corrections had done -- so a check on
# the required vector groups was being answered by a sentence about a correction to them. It is scoped
# to the vector-groups section and matches what that section says, which is stronger: BOTH mutations,
# one per conjunct, is the requirement, and the singular the status prose used was the weaker claim.
$briefVectorGroups = ($neutralBrief -split '## Vector groups', 2)[1] -split '## Capability-wide property format', 2 | Select-Object -First 1
if (-not $briefVectorGroups -or ((Get-FlowedText $briefVectorGroups) -replace '\*\*', '').IndexOf('intra-interaction frame order and both its ordering mutations', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The neutral brief''s required adversarial vector groups do not list intra-interaction frame order with both its ordering mutations, so a conjunct of `C4-P2` has no group required to falsify it.')
}

# U4: the completeness review narrates a disposition paragraph per review cycle, and stopped after the
# fifth while its own status block claimed R1-R3 and S1-S3 complete. A disposition history that
# silently stops is the staleness class S3 named.
$dispositionSection = ($completeness -split '## Review disposition', 2)[1]
foreach ($cyclePin in @('11ba93bddbd38f03df59b4afc5166d7c6991c865', '3892c23a8dd4c7f298e877ba73710ee0ddc97bc4', '3b27e3a85bf018bead6d226a13d075c7e6ed16fa')) {
    if ($dispositionSection -and $dispositionSection.IndexOf($cyclePin, [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("The completeness review's disposition history does not record the cycle reviewed at '$cyclePin', although its status block claims that cycle's findings are corrected.")
    }
}

# U7: the silence row added for the in-flight bound's direction scope attributes the atomic
# reservation to C4, which describes no reservation -- the interaction machine does. In a correction
# class whose whole subject is which artifact states which fact, the attribution has to be exact.
if ($flowedCompleteness.IndexOf('the atomic reservation C4 describes', [System.StringComparison]::Ordinal) -ge 0) {
    $failures.Add('The in-flight direction-scope row attributes the atomic in-flight reservation to C4. C4 states the bound; the interaction state machine states the reservation.')
}
if ($flowedCompleteness.IndexOf('state one count without saying whether it is per session or per initiating direction', [System.StringComparison]::Ordinal) -ge 0) {
    $failures.Add('The in-flight direction-scope row says C4-P1 and I5 do not say which scope they mean. Both count nonterminal interactions with no direction restriction, which reads session-wide, while the only mechanism the design provides is a local reservation that can enforce a per-direction count. The row should say that rather than call the scope undeclared.')
}

# U8: every Local loss cell in the initiator grid names the state it selects except the pre-dispatch
# one, which is the cell S2's reconciliation was about.
$initiatorGridSection = ($stateEventCoverage -split '## Initiator interaction coverage grid', 2)[1] -split '## Recipient interaction coverage grid', 2 | Select-Object -First 1
$preDispatchRow = @($initiatorGridSection -split "`r?`n" | Where-Object { $_ -match '^\| `candidate` / `admitting` \|' })
if ($preDispatchRow.Count -eq 1) {
    $preDispatchCells = @($preDispatchRow[0].Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($preDispatchCells.Count -ge 6 -and $preDispatchCells[5].IndexOf('`lost`', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The initiator grid''s pre-dispatch Local loss cell names no state, while every other Local loss cell names `lost`. The interaction machine selects `lost` in any nonterminal state including pre-dispatch ones, which is exactly what S2 reconciled.')
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
Assert-ContainsAll 'Channel 0.2 external phase refusal provenance' $migration @(
    '| `state-violation` | **retained** | Scope identifies session versus interaction. An external phase refusal is never this fault: a false or unknown predicate is a frameless local refusal at either endpoint under C3. |'
)
# T1: the ledger may not offer an adapter author a peer-fault reading of a refusal that C3 and the
# recipient machine require to be frameless at both endpoints.
if ($migration.IndexOf('external phase refusal may be local frameless or peer fault', [System.StringComparison]::Ordinal) -ge 0) {
    $failures.Add('The migration ledger must not permit a peer fault for an external phase refusal.')
}
Assert-ContainsAll 'Channel 0.2 replay window migration' $migration @(
    '| `replay-detected` | **retained** | A repeated accepted identity received while its original interaction is nonterminal; no redispatch. A repeat arriving after that interaction is terminal follows the late-traffic latch as `state-violation` instead. |'
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
Assert-ContainsAll 'Channel 0.2 review policy' (Get-FlowedText $reviewReadme) @(
    'four owner rulings resolved',
    'fresh independent closure re-review is pending',
    '## Required review scope',
    '## Required verdicts',
    '## Closure',
    '## Exact next work',
    '`3892c23a8dd4c7f298e877ba73710ee0ddc97bc4`',
    '`channel-0.2-design-foundation-closure-review-7-attestation.md`',
    '`channel-0.2-design-foundation-closure-review-8-attestation.md`',
    '`channel-0.2-design-foundation-closure-review-9-attestation.md`',
    '`channel-0.2-design-foundation-closure-record.md`',
    '`build/verify-channel-0.2-design.ps1`',
    '`build/verify-interchange.ps1`'
)

# X6: U6 was a pin clause naming a commit that later work had superseded, and the clause rewritten to
# close it went stale the same way one commit later: it names the U2-U8 correction as the current
# review target while the W1-W6 commit changed six design artifacts after it. Prose cannot check
# itself against history, so this check does, and it keys off the repository rather than off the
# clause's wording. It is skipped only while the design artifacts have uncommitted edits, because a
# pin cannot name a commit that does not exist yet; once the correction is committed the clause and
# the commit that carries it must agree.
#
# AN2: derived from `$artifactNames` rather than written out again. The second list held eight of the
# nine and the one it omitted was the redesign plan -- item 3 of the review policy's own required
# review scope, and where the four owner rulings and the closure standard live. A commit changing only
# that artifact left the pin green while the material a reviewer reads had moved, which is U6 exactly,
# inside the check written to end U6. Probed by committing a plan-only change: the gate passed.
#
# One list, so a tenth design artifact joins both questions at once. This is W1's rule applied to the
# verifier's own enumerations: a fact published twice and maintained by hand drifts, and which
# artifacts are the design is that kind of fact.
$designArtifactPathspec = @(
    $artifactNames |
        Where-Object { $_ -ne 'README.md' -and $_ -ne 'reviews\README.md' } |
        ForEach-Object { "docs/future/channel/$($_ -replace '\\', '/')" }
)
# AM5, and the check is now written over the rule the policy actually states rather than over the
# commit subject. The clause says: "Review that commit or any later commit whose design artifacts hash
# identically to it." This check demanded the SUBJECT of the most recent commit to touch one of those
# paths, which is a narrower thing, and the two disagree whenever a branch changes a design artifact
# and changes it back -- as the AM branch did, adding a paragraph to the completeness review in one
# commit and removing it in the next.
#
# That disagreement is not academic and it is not a tie. Path-limited `git log` simplifies history: on
# the pull-request MERGE commit the merge is TREESAME to main for those eight paths, so git follows
# main and reports main's last design commit, while on the linear branch it reports the branch's. Only
# one of the two could satisfy a subject match, and the one that matters is the merge -- because it is
# what `main` will report after the merge, so a pin that satisfied the branch would turn main red.
# CI found this; the local gate could not, because the local gate never sees the merge view.
#
# Comparing the design artifacts' blob hashes answers the policy's question directly and gives the
# same answer in both views: a pin is valid when the artifacts at the pinned commit are the artifacts
# a reviewer would read now.
if (Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')) {
    $pendingDesignEdits = Invoke-Git (@('status', '--porcelain', '--') + $designArtifactPathspec)
    $latestDesignSubject = Invoke-Git (@('log', '-1', '--format=%s', '--') + $designArtifactPathspec)
    if ($LASTEXITCODE -eq 0 -and $latestDesignSubject -and -not $pendingDesignEdits) {
        $flowedReviewReadme = Get-FlowedText $reviewReadme
        $pinnedSubjectMatch = [regex]::Match($flowedReviewReadme, 'The current review target is the commit titled `([^`]+)`')
        if (-not $pinnedSubjectMatch.Success) {
            $failures.Add('The review policy names no current review target, so a fresh reviewer has nothing to pin its attestation to.')
        }
        else {
            $pinnedSubject = $pinnedSubjectMatch.Groups[1].Value
            # The named commit has to exist. A subject naming nothing is a pin to nowhere, which is
            # worse than a stale one: the reviewer cannot even discover that it moved.
            # Matched against the SUBJECT LINE, not against the message: these commit bodies quote
            # other commits' subjects, and `--grep` would resolve a pin to whichever commit mentioned
            # it last.
            $pinnedCommit = @(Invoke-Git @('log', "--format=%H`t%s") |
                Where-Object { ($_ -split "`t", 2)[1] -ceq $pinnedSubject } |
                ForEach-Object { ($_ -split "`t", 2)[0] } |
                Select-Object -First 1)
            if (-not $pinnedCommit -or -not $pinnedCommit[0]) {
                $failures.Add("The review policy pins the review target to a commit titled '$pinnedSubject' and no commit in this history carries that subject. The most recent commit to change a design artifact is '$latestDesignSubject'.")
            }
            else {
                # Every design artifact, compared by blob hash at the pinned commit against the tree a
                # reviewer would read now. `git rev-parse <commit>:<path>` is the artifact's identity,
                # which is what the policy's sentence is about.
                $movedArtifacts = [System.Collections.Generic.List[string]]::new()
                foreach ($designArtifact in $designArtifactPathspec) {
                    $pinnedBlob = (Invoke-Git @('rev-parse', "$($pinnedCommit[0]):$designArtifact"))
                    $currentBlob = (Invoke-Git @('rev-parse', "HEAD:$designArtifact"))
                    if (-not $pinnedBlob -or -not $currentBlob -or $pinnedBlob -cne $currentBlob) {
                        $movedArtifacts.Add(($designArtifact -split '/')[-1])
                    }
                }
                if ($movedArtifacts.Count -gt 0) {
                    $failures.Add("The review policy pins the review target to '$pinnedSubject', and $($movedArtifacts.Count) design artifact(s) have moved since that commit: $($movedArtifacts -join ', '). The policy's own clause permits any commit whose design artifacts hash identically to the pinned one, and these do not. The most recent commit to change one is '$latestDesignSubject'. This is U6: the reviewer is sent at artifacts that have already moved.")
                }
            }
        }
    }
}

# T4: every first-batch artifact's own status block names the review cycle it is actually waiting
# for. The escalating adjectives of the earlier cycles ("final", "definitive", "totality") left three
# status blocks pointing at a review that had already happened, so the phrase is now one stable
# string. Only the status block is constrained; the completeness review's history of earlier cycles
# is retained evidence and names them deliberately.
function Get-StatusBlock {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)

    $firstSection = $Content.IndexOf("`n## ", [System.StringComparison]::Ordinal)
    if ($firstSection -lt 0) {
        return $Content
    }
    return $Content.Substring(0, $firstSection)
}

# Prose status claims wrap across lines at the author's discretion, so every phrase check below runs
# against whitespace-collapsed text. Without this a required phrase disappears the moment a sentence
# reflows, and a forbidden one hides in the same way, which is a check that fails for the wrong reason
# and passes for the wrong reason respectively.
$statusArtifacts = [ordered]@{
    'Channel 0.2 capability contract status'   = Get-FlowedText (Get-StatusBlock $contract)
    'Channel 0.2 session state machine status' = Get-FlowedText (Get-StatusBlock $session)
    'Channel 0.2 interaction machine status'   = Get-FlowedText (Get-StatusBlock $interaction)
    'Channel 0.2 state/event coverage status'  = Get-FlowedText (Get-StatusBlock $stateEventCoverage)
    'Channel 0.2 responsibility matrix status' = Get-FlowedText (Get-StatusBlock $responsibility)
    'Channel 0.2 completeness review status'   = Get-FlowedText (Get-StatusBlock $completeness)
    'Channel 0.2 migration ledger status'      = Get-FlowedText (Get-StatusBlock $migration)
    'Channel 0.2 neutral brief status'         = Get-FlowedText (Get-StatusBlock $neutralBrief)
    'Channel 0.2 index status'                 = Get-FlowedText $channelReadme
}
foreach ($statusArtifact in $statusArtifacts.GetEnumerator()) {
    Assert-ContainsAll $statusArtifact.Key $statusArtifact.Value @('fresh independent closure re-review')
    foreach ($supersededCycle in @('definitive closure review', 'final closure review', 'totality closure review')) {
        if ($statusArtifact.Value.IndexOf($supersededCycle, [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add("$($statusArtifact.Key) still names the superseded '$supersededCycle'.")
        }
    }
}

# Three entry-point documents state Channel's status for a reader who never opens the design package,
# and each went stale independently: the future-work index kept an "author pass" table row for four
# review cycles while its own Priority 1 prose was current. A phrase-anywhere check passes on the
# strength of one current sentence, so each document is pinned for the claims it must not make.
$statusIndexPaths = [ordered]@{
    'Repository README Channel status'  = 'README.md'
    'Documentation map Channel status'  = 'docs\README.md'
    'Future-work index Channel status'  = 'docs\future\README.md'
}
$staleChannelClaims = @(
    'author pass',
    'owner confirmations',
    'owner confirmation',
    'architecture rulings and fresh independent design review remain',
    'independent review pending',
    'independent review has not yet occurred'
)
foreach ($statusIndex in $statusIndexPaths.GetEnumerator()) {
    $statusIndexPath = Join-Path $repositoryRoot $statusIndex.Value
    if (-not (Test-Path -LiteralPath $statusIndexPath)) {
        $failures.Add("A Channel status index does not exist at '$($statusIndex.Value)'.")
        continue
    }

    $statusIndexText = Get-FlowedText (Get-Content -Raw -LiteralPath $statusIndexPath -Encoding UTF8)
    Assert-ContainsAll $statusIndex.Key $statusIndexText @('fresh independent closure re-review')
    foreach ($staleClaim in $staleChannelClaims) {
        if ($statusIndexText.IndexOf($staleClaim, [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add("$($statusIndex.Key) still claims '$staleClaim'.")
        }
    }
}

# The future-work index states Channel's status twice. The Other planned areas row is the one a
# reader reaches from another area's entry, so it is pinned for its own content as well.
$futureIndex = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'docs\future\README.md') -Encoding UTF8
$channelAreaRows = @($futureIndex -split "`r?`n" | Where-Object { $_ -match '^\| Channel \|' })
if ($channelAreaRows.Count -ne 1) {
    $failures.Add("The future-work index must carry exactly one Channel row in Other planned areas; found $($channelAreaRows.Count).")
}
else {
    Assert-ContainsAll 'Channel row in the future-work index' $channelAreaRows[0] @(
        'four resolved owner rulings',
        'fresh independent closure re-review'
    )
}

$reviewDirectory = Join-Path $channelPath 'reviews'
$reviewMarkdown = @(Get-ChildItem -LiteralPath $reviewDirectory -Filter '*.md' -File)
$expectedReviewNames = @('README.md', 'channel-0.2-design-foundation-attestation.md', 'channel-0.2-design-foundation-closure-attestation.md', 'channel-0.2-design-foundation-final-closure-attestation.md', 'channel-0.2-design-foundation-definitive-closure-attestation.md', 'channel-0.2-design-foundation-totality-closure-attestation.md', 'channel-0.2-design-foundation-closure-re-review-attestation.md', 'channel-0.2-design-foundation-closure-review-7-attestation.md', 'channel-0.2-design-foundation-closure-review-8-attestation.md', 'channel-0.2-design-foundation-closure-review-9-attestation.md', 'channel-0.2-design-foundation-closure-review-10-attestation.md', 'channel-0.2-design-foundation-closure-review-11-attestation.md', 'channel-0.2-design-foundation-closure-review-12-attestation.md', 'channel-0.2-design-foundation-closure-review-13-attestation.md', 'channel-0.2-design-foundation-closure-review-14-attestation.md', 'channel-0.2-design-foundation-closure-review-15-attestation.md', 'channel-0.2-design-foundation-closure-review-16-attestation.md', 'channel-0.2-u1-correction-iteration-review.md', 'channel-0.2-w-correction-iteration-review.md', 'channel-0.2-ac-correction-iteration-review.md', 'channel-0.2-ad-correction-iteration-review.md', 'channel-0.2-am-iteration-review.md', 'channel-0.2-an-iteration-review.md', 'channel-0.2-ao-iteration-review.md', 'channel-0.2-ap-iteration-review.md', 'channel-0.2-aq-iteration-review.md', 'channel-0.2-ar-iteration-review.md', 'channel-0.2-as-iteration-review.md', 'channel-0.2-at-iteration-review.md', 'channel-0.2-au-iteration-review.md', 'channel-0.2-av-iteration-review.md', 'channel-0.2-aw-iteration-review.md', 'channel-0.2-ax-iteration-review.md', 'channel-0.2-ay-iteration-review.md', 'channel-0.2-disposition-index.md')
$actualReviewNames = @($reviewMarkdown.Name | Sort-Object)
if (($actualReviewNames -join ',') -cne (($expectedReviewNames | Sort-Object) -join ',')) {
    $failures.Add('The Channel 0.2 design foundation must retain exactly the review README, all sixteen retained attestations, and all seventeen iteration reviews, plus the disposition index the status blocks point at, before the next closure review.')
}

# The closure-cycle hold. The review policy tells an agent not to dispatch a closure review while the
# hold stands, and an instruction in prose is exactly the kind of thing this programme has watched go
# unread: an agent that never opens the policy is the case the instruction cannot reach.
#
# What this check can and cannot do, stated rather than implied. It cannot see a dispatch -- that
# happens outside the repository, in someone else's clone -- so it catches the retention, which is the
# first moment the work becomes visible here and the moment before it is committed. A review dispatched
# and never retained costs a cold context and produces nothing this gate can observe. The instruction
# in step 4 remains the primary control and this is the backstop.
#
# The state is read from the verification foundation plan, which is the artifact that owns the owner
# decision, and the review policy carries a link to it rather than a second copy of it. That is W1 of
# that plan applied to the plan's own fact: one owning artifact, citations elsewhere. A second copy
# here would be the six-surface problem the plan exists to retire, one surface smaller.
$verificationPlan = Read-RequiredText 'Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md'
$holdDeclaration = [regex]::Match((Get-FlowedText $verificationPlan), 'Closure-cycle state: \*\*([a-z-]+)\*\* since ([0-9]{4}-[0-9]{2}-[0-9]{2}), at ([0-9]+) retained attestations')
if (-not $holdDeclaration.Success) {
    $failures.Add('The verification foundation plan declares no closure-cycle state. It is the artifact that owns the owner decision holding or resuming the cycle, and a hold that is stated only in prose is a hold no gate can enforce -- which is the condition this check was added to end.')
}
else {
    $holdState = $holdDeclaration.Groups[1].Value
    $holdCount = [int]$holdDeclaration.Groups[3].Value
    $holdAttestationCount = @($reviewMarkdown | Where-Object { $_.Name -match 'attestation\.md$' }).Count
    $reviewPolicyFlowed = Get-FlowedText $reviewReadme
    $dispatchMarker = 'On hold since ' + $holdDeclaration.Groups[2].Value + ' - do not dispatch.'
    # The dash between the date and the instruction is a dash of the author's choosing; this file
    # stays ASCII, so the separator is matched rather than spelled.
    $dispatchMarkerPresent = $reviewPolicyFlowed -match ('\*\*On hold since ' + $holdDeclaration.Groups[2].Value + '.{0,3}do not dispatch\.\*\*')
    if ($holdState -ne 'on-hold' -and $holdState -ne 'open') {
        $failures.Add("The verification foundation plan declares the closure-cycle state as '$holdState', which is outside the closed vocabulary ``on-hold``/``open``. A state outside a closed set cannot be acted on by this check or by a reader, which is the defect B4 was raised for in the migration ledger's categories.")
    }
    elseif ($holdState -eq 'on-hold') {
        if ($holdAttestationCount -ne $holdCount) {
            $failures.Add("The closure cycle is on hold at $holdCount retained attestations and the reviews directory holds $holdAttestationCount. A closure review was run and retained while the cycle was held. Lifting the hold is an owner decision recorded in the verification foundation plan against that plan's four stated conditions -- it is not this number being edited to match, and an attestation retained ahead of the decision cannot be un-run.")
        }
        if (-not $dispatchMarkerPresent) {
            $failures.Add("The closure cycle is on hold and the review policy's step 4 does not carry the '$dispatchMarker' marker. The agent that dispatches a review reads that step, not this plan, so the hold has to be stated where the dispatch decision is made as well as where it is owned.")
        }
    }
    elseif ($dispatchMarkerPresent) {
        $failures.Add("The verification foundation plan declares the closure cycle ``open`` while the review policy's step 4 still carries the do-not-dispatch marker. One of the two is stale, and the dispatching agent reads the one that says stop -- so a resumed cycle that leaves this marker standing is a hold nobody lifted.")
    }
}

# AJ7: the retained-attestations list is what a reader scans for the most recent record, and it ran
# 11, 13, 12 -- the thirteenth review's entry in the eleventh's place. Nothing is misstated in either
# entry, which is why it was worth checking rather than reading: AI8 established that a defect of
# exactly this weight, seen and dispositioned as "noted, not raised", is not something this
# programme's machinery can act on. The list's numbered entries must be the numbered attestations the
# directory holds, in ascending order, so an entry cannot be filed out of sequence or left out.
$retainedSection = ($reviewReadme -split '(?m)^## Retained attestations', 2)[1] -split '(?m)^## ', 2 | Select-Object -First 1
if (-not $retainedSection) {
    $failures.Add('The review policy has no Retained attestations section, which is the list a reader scans for the most recent retained record.')
}
else {
    $listedReviewNumbers = @([regex]::Matches($retainedSection, 'closure-review-([0-9]+)-attestation\.md') | ForEach-Object { [int]$_.Groups[1].Value } | Select-Object -Unique)
    $heldReviewNumbers = @($reviewMarkdown.Name |
        ForEach-Object { [regex]::Match($_, '^channel-0\.2-design-foundation-closure-review-([0-9]+)-attestation\.md$') } |
        Where-Object { $_.Success } |
        ForEach-Object { [int]$_.Groups[1].Value } |
        Sort-Object)
    if (($listedReviewNumbers -join ',') -cne ($heldReviewNumbers -join ',')) {
        $failures.Add("The review policy's retained-attestations list gives the numbered reviews in the order '$($listedReviewNumbers -join ',')' and the directory holds '$($heldReviewNumbers -join ',')'. The list is read top to bottom for the newest record, so an entry out of sequence or missing is read as the state of the programme. This is AJ7.")
    }
}

# An iteration review is author-side work and may never be mistaken for a closing judgement. The file
# naming carries that distinction, so the naming is checked rather than trusted: nothing may be named
# an attestation without being one, and an iteration review must say what it cannot do.
foreach ($reviewFile in $reviewMarkdown) {
    if ($reviewFile.Name -notmatch 'iteration-review\.md$') { continue }
    $iterationText = Get-FlowedText (Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8)
    foreach ($required in @('This is an iteration review, not an attestation', 'does not close the first batch, does not authorize Batch 2')) {
        if ($iterationText.IndexOf($required, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("'$($reviewFile.Name)' is an iteration review but does not state that it cannot close the batch. An author-side pass that reads as a verdict is how a programme talks itself into closure.")
        }
    }
}

# X7: the two-kinds-of-review section requires an iteration review to be retained as evidence. The
# W1-W6 passes left none: their record is a commit message and a step list, and the disposition
# history that carries V1 and V2 stops before them. The check is written over the general class --
# every finding a retained iteration review raises must appear in the disposition history, and a
# finding family the review policy attributes to an iteration pass must have a retained record --
# rather than over the six ids, so the next pass that skips its record fails here too.
$dispositionHistory = ($completeness -split '## Review disposition', 2)[1]
# The verification foundation plan is the second of the two homes a family's disposition can have. It
# is not a design artifact and no closure review assesses it, which is exactly why it is the right
# record for a family raised against the verification work rather than against the design.
$verificationPlanText = Get-Content -Raw -LiteralPath (Join-Path $channelPath 'Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md') -Encoding UTF8
if (-not $dispositionHistory) {
    $failures.Add('The completeness review carries no review-disposition history, which is where each cycle''s findings are recorded.')
}
else {
    $iterationReviewFiles = @($reviewMarkdown | Where-Object { $_.Name -match 'iteration-review\.md$' })
    # The provenance table is read here rather than below, because under the 2026-08-20 owner ruling
    # it is what ROUTES the obligation: a `design` family owes its disposition to the completeness
    # review's history, a `verification` family to the verification foundation plan. Neither is
    # exempt. Reading it later would mean the routing check ran without knowing the class.
    $provenanceTable = [regex]::Match($reviewReadme, '(?ms)^## Finding family provenance\r?\n(.+?)(?=^## |\z)').Groups[1].Value
    $familyProvenance = @{}
    # The second axis, under the 2026-08-20 owner ruling: what a family was raised AGAINST decides
    # which record owes its disposition. Both values are required of every row, so a family cannot be
    # added on one axis and left unclassified on the other -- which is AF6's lesson about a class
    # inferred from the members that happened to be visible.
    $familySubject = @{}
    foreach ($provenanceRow in [regex]::Matches($provenanceTable, '(?m)^\|\s*([A-Z]{1,2})\s*\|\s*(iteration|closure-review)\s*\|\s*([a-z]+)\s*\|')) {
        $familyProvenance[$provenanceRow.Groups[1].Value] = $provenanceRow.Groups[2].Value
        $familySubject[$provenanceRow.Groups[1].Value] = $provenanceRow.Groups[3].Value
    }
    foreach ($subjectRow in $familySubject.GetEnumerator()) {
        if ($subjectRow.Value -ne 'design' -and $subjectRow.Value -ne 'verification') {
            $failures.Add("The finding-family provenance table classifies '$($subjectRow.Key)' as raised against '$($subjectRow.Value)', which is outside the closed set ``design``/``verification``. A value outside a closed vocabulary is uncountable, which is what the migration ledger's B4 finding was.")
        }
    }
    foreach ($provenanceFamily in $familyProvenance.Keys) {
        if (-not $familySubject.ContainsKey($provenanceFamily)) {
            $failures.Add("The finding-family provenance table does not say what '$provenanceFamily' was raised against. That axis is what routes the family's disposition to the completeness review or to the verification foundation plan, and an unclassified family is owed by neither.")
        }
    }

    foreach ($reviewFile in $iterationReviewFiles) {
        $iterationRaw = Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8
        # The family pattern is `[A-Z]{1,2}` rather than `[A-Z]`: the AA and AB families already
        # existed when this check was written for X7 and it could not see either of them, so a
        # two-letter family's findings could be raised in a retained iteration review and never
        # reach the disposition history. A check written over a class has to match the whole class.
        $findingMatches = @([regex]::Matches($iterationRaw, '(?m)^### ([A-Z]{1,2}[0-9]+) '))
        foreach ($findingMatch in $findingMatches) {
            $findingId = $findingMatch.Groups[1].Value
            $findingFamily = [regex]::Match($findingId, '^([A-Z]{1,2})').Groups[1].Value
            # Which record owes this finding's disposition. The obligation itself is unchanged and
            # unconditional; only its home depends on what the family was raised against.
            $findingHome = if ($familySubject[$findingFamily] -eq 'verification') {
                @{ Text = $verificationPlanText; Name = 'the verification foundation plan'; Why = 'That plan owns the work this family was raised against, and is where a reviewer of it reads what was decided.' }
            }
            else {
                @{ Text = $dispositionHistory; Name = "the completeness review's disposition history"; Why = 'A finding whose only record is an iteration review has no disposition in the artifact the next reviewer reads.' }
            }
            if ($findingHome.Text.IndexOf($findingId, [System.StringComparison]::Ordinal) -lt 0) {
                $failures.Add("$($findingHome.Name) does not record '$findingId', which '$($reviewFile.Name)' raises. $($findingHome.Why)")
            }
        }
        # The loop above fails when a finding is missing from the history and is silent when it
        # parses no findings at all, so narrowing the pattern disables it without failing anything --
        # which is how it ran blind past the AA and AB families. A retained iteration review exists to
        # record findings, so parsing none from one is the defect, and this is the assertion that
        # makes the pattern itself falsifiable.
        # AW1. This asserted that a retained iteration review records at least one finding, which was
        # true of the ten that had run and stopped being true the moment the 2026-09-04 ruling made
        # "found nothing in the package" the outcome condition 4 asks for. The two-kinds-of-review
        # section still requires such a pass to be retained as evidence, so the guard forbade
        # recording the one result the programme is working toward -- AP1's class, a key that was
        # correct when written and expired when the work moved.
        #
        # The pattern stays falsifiable, which is what this check is for. A review that parses no
        # finding must SAY so in the declared form below, and a review that says so must carry no
        # finding heading at all -- so a heading pattern that has quietly stopped matching still
        # fails, because nobody writes that sentence into a review that found things.
        $noFindingsDeclaration = 'This pass records no finding against the package.'
        # Matched as a WHOLE LINE, not as a substring. The first form of this check fired on the
        # review that introduced it, because a review describing the declaration quotes it in a
        # sentence -- so the declaration is a line a review writes deliberately, and a mention of it
        # inside prose is a mention.
        $declaresNoFindings = [regex]::IsMatch($iterationRaw, '(?m)^' + [regex]::Escape($noFindingsDeclaration) + '?$')
        # The declaration is tested FIRST so that both operands are reached. Written the other way
        # round, `$findingMatches.Count -lt 1` is false for every review retained so far and
        # short-circuiting means nothing ever evaluates the declaration -- an operand that could be
        # deleted with every gate green, which is AT4's unit and would have been a finding here.
        if (-not $declaresNoFindings -and $findingMatches.Count -lt 1) {
            $failures.Add("No finding heading could be parsed from '$($reviewFile.Name)', and it does not state '$noFindingsDeclaration'. Either the heading pattern no longer matches its finding ids and the disposition check above is passing by seeing nothing, or the pass genuinely found nothing and must say so in that form.")
        }
        if ($findingMatches.Count -gt 0 -and $declaresNoFindings) {
            $failures.Add("'$($reviewFile.Name)' states '$noFindingsDeclaration' and also records $($findingMatches.Count) finding heading(s). One of the two is wrong, and a review that says both leaves the next reader to guess which.")
        }
    }

    # AA1: the Channel index summarises the programme for a reader who never opens the design
    # package, and it went stale across five correction passes at once -- naming U1-U8 and V1-V2 as
    # the whole of the corrected set, "the S1 correction" as the pending cycle, and seven retained
    # reviews when there were ten files. The status-phrase checks above cannot see any of it: every
    # required phrase was present and every forbidden one absent. These two are structural instead,
    # so the index cannot fall behind a pass without failing.
    $indexReviewRow = @($channelReadme -split "`r?`n" | Where-Object { $_ -match '^\| \[Design reviews\]' })
    $attestationCount = @($reviewMarkdown | Where-Object { $_.Name -match 'attestation\.md$' }).Count
    $iterationCount = $iterationReviewFiles.Count
    if ($indexReviewRow.Count -ne 1) {
        $failures.Add('The Channel index must carry exactly one Design reviews row.')
    }
    else {
        # "retained attestations" rather than "negative attestations": review 12 returned
        # `conforms-with-nonblocking-findings`, so the count is no longer a count of negatives and a
        # phrase saying otherwise is a false claim in the index that reports it.
        foreach ($required in @("$attestationCount retained attestations", "$iterationCount iteration reviews")) {
            if ($indexReviewRow[0].IndexOf($required, [System.StringComparison]::Ordinal) -lt 0) {
                $failures.Add("The Channel index's Design reviews row does not say '$required', which is what the reviews directory actually holds. A count in prose is a claim that goes stale the next time a review is retained.")
            }
        }
    }

    # AX1, and this is AA1's correction applied to the two surfaces AA1 did not reach. The Channel
    # index row above is recomputed and was correct after the eleventh pass; the plan's own condition-4
    # tally and the review policy's pass count are prose, and both went stale in the commit that
    # RECORDED that pass. That is the ninth time an entry point has gone stale in this programme and
    # the same split every time -- what a gate recomputes is right, what is left to prose is wrong.
    #
    # The population is keyed on what the review calls itself rather than on its filename: a
    # condition-4 pass is a retained iteration review titled a W1-W3 verification-foundation iteration
    # review. A filename key would be a lexical key over a naming convention, which is the shape AL1
    # and AT1 were each raised against.
    $conditionFourPasses = @($iterationReviewFiles | Where-Object {
        (Get-Content -LiteralPath $_.FullName -Encoding UTF8 -TotalCount 1) -match 'W1-W3 verification-foundation iteration review'
    }).Count
    # Written out here rather than read from `$numberWords`, which this file defines nine hundred
    # lines below: PowerShell runs top to bottom, and a check that reads a map declared after it
    # reads `$null` and throws where it meant to compare.
    $passCardinals = @('zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine',
        'ten', 'eleven', 'twelve', 'thirteen', 'fourteen', 'fifteen', 'sixteen', 'seventeen',
        'eighteen', 'nineteen', 'twenty')
    $ordinalWords = @('first', 'second', 'third', 'fourth', 'fifth', 'sixth', 'seventh', 'eighth',
        'ninth', 'tenth', 'eleventh', 'twelfth', 'thirteenth', 'fourteenth', 'fifteenth', 'sixteenth',
        'seventeenth', 'eighteenth', 'nineteenth', 'twentieth')
    $cardinalWord = if ($conditionFourPasses -lt $passCardinals.Count) { $passCardinals[$conditionFourPasses] } else { $null }
    $nextOrdinal = if ($conditionFourPasses -lt $ordinalWords.Count) { $ordinalWords[$conditionFourPasses] } else { $null }

    if (-not $cardinalWord) {
        $failures.Add("There are $conditionFourPasses retained condition-4 passes and this file has no number word for that count, so the tallies below cannot be checked. Teach it the word rather than dropping the check.")
    }
    else {
        $countClaims = @(
            @{ Text = Get-FlowedText $verificationPlanText
               Pattern = 'Condition 4 has run ([a-z-]+) times'
               Where = "the verification foundation plan's condition-4 tally" }
            @{ Text = Get-FlowedText $reviewReadme
               Pattern = '([A-Za-z-]+) such passes have run'
               Where = "the review policy's condition-4 pass count" }
        )
        foreach ($countClaim in $countClaims) {
            $claimMatch = [regex]::Match($countClaim.Text, $countClaim.Pattern)
            if (-not $claimMatch.Success) {
                $failures.Add("$($countClaim.Where) is no longer stated in the form this check recomputes. That sentence is what tells a reader how many times condition 4 has been attempted, and a count only prose carries is one that goes stale in the commit that records the next pass.")
                continue
            }
            $claimedWord = $claimMatch.Groups[1].Value.ToLowerInvariant()
            if ($claimedWord -cne $cardinalWord) {
                $failures.Add("$($countClaim.Where) says '$claimedWord' and $conditionFourPasses condition-4 passes are retained, which is '$cardinalWord'.")
            }
        }
    }

    # And the ordinal the next pass is named by, which is where the staleness actually shows: both
    # entry points still called the next pass the eleventh after the eleventh had run and been
    # retained beside the sentence.
    if ($nextOrdinal) {
        $ordinalClaims = @(
            @{ Text = Get-FlowedText $verificationPlanText
               Pattern = 'The next work is therefore an? ([a-z-]+) author-side pass'
               Where = "the verification foundation plan's next-work sentence" }
            @{ Text = Get-FlowedText $reviewReadme
               Pattern = 'An? ([a-z-]+) pass over the same scope is the live path'
               Where = "the review policy's live-path sentence" }
        )
        foreach ($ordinalClaim in $ordinalClaims) {
            $ordinalMatch = [regex]::Match($ordinalClaim.Text, $ordinalClaim.Pattern)
            if (-not $ordinalMatch.Success) {
                $failures.Add("$($ordinalClaim.Where) no longer names the next pass in the form this check recomputes. That sentence is what the next agent reads to know which pass it is running.")
            }
            elseif ($ordinalMatch.Groups[1].Value -cne $nextOrdinal) {
                $failures.Add("$($ordinalClaim.Where) calls the next pass the '$($ordinalMatch.Groups[1].Value)' and $conditionFourPasses have been retained, so the next one is the '$nextOrdinal'. A pass named after one that has already run sends the next agent to repeat it.")
            }
        }
    }
    # DESIGN families only, under the 2026-08-20 ruling. This set is the anchor for five freshness
    # checks -- the Channel index's stated correction range, the future-work index's Channel row, the
    # Design reviews row, every per-artifact section of the disposition index, and the status-block
    # pointer check -- and every one of them asks a question about the design artifacts. A family
    # raised against the verification work makes "the newest family" one that touched none of them,
    # and all five are then answered by sections saying "unchanged", which is a guard becoming a
    # formality. Its disposition is required all the same, in the plan that owns that work.
    $dispositionFamilies = @([regex]::Matches($dispositionHistory, '\*\*([A-Z]{1,2})[0-9]+\*\*') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $familySubject[$_] -ne 'verification' } | Sort-Object -Unique)
    foreach ($family in $dispositionFamilies) {
        if ($channelReadme -cnotmatch "\b$family[0-9]") {
            $failures.Add("The Channel index names no finding in the '$family' family, although the completeness review's disposition history records one. The index is where a reader who opens nothing else learns what has been corrected.")
        }
    }

    # AA2: the future-work index carries the longest Channel narrative of any entry point and the one
    # a reader reaches while choosing what to work on. It stopped at the seventh review: seven
    # retained cycles when there were eight, and S1 named as the open blocking finding four correction
    # passes after it closed. Counted and family-checked here for the same reason as the index above.
    $futureIndexText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'docs\future\README.md') -Encoding UTF8
    foreach ($family in $dispositionFamilies) {
        if ($futureIndexText -cnotmatch "\b$family[0-9]") {
            $failures.Add("The future-work index names no finding in the '$family' family, although the completeness review's disposition history records one. This is the entry point a reader reaches while choosing what to work on.")
        }
    }
    if ($futureIndexText.IndexOf("$attestationCount retained independent reviews", [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("The future-work index does not say '$attestationCount retained independent reviews', which is what the reviews directory holds. Its predecessor count was written as a word and went stale unnoticed for a full cycle.")
    }

    # AB1: the redesign plan is the fourth entry point -- the future-work index calls it "the next
    # work" -- and it was the one status block the T4 check set never covered, so it went stale
    # unnoticed through six correction passes while the checks watched the other nine.
    foreach ($family in $dispositionFamilies) {
        if ((Get-DispositionSection 'Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md') -cnotmatch "\b$family[0-9]") {
            $failures.Add("The disposition index's section for the redesign plan names no finding in the '$family' family, although the completeness review's disposition history records one. The plan is the entry point the future-work index sends a reader to first, and its disposition history is the one the cycle-name check never covered.")
        }
    }

    # AA3: U2 closed the owner vocabulary and abolished `channel-core` as a second name for `channel`.
    # The future-work index still attributes the ordering row to it, so the identifier the matrix may
    # not use survives in the document most readers meet first -- a closed vocabulary that is closed
    # in one artifact only is not closed.
    foreach ($statusIndexRelativePath in @('README.md', 'docs\README.md', 'docs\future\README.md', 'docs\future\channel\README.md')) {
        $statusIndexFullPath = Join-Path $repositoryRoot $statusIndexRelativePath
        if (-not (Test-Path -LiteralPath $statusIndexFullPath)) { continue }
        if ((Get-Content -Raw -LiteralPath $statusIndexFullPath -Encoding UTF8).IndexOf('channel-core', [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add("'$statusIndexRelativePath' names the owner identifier ``channel-core``, which U2 abolished when it closed the responsibility matrix's owner vocabulary. One owner has one identifier, or an ownership inventory keyed by identifier reads two owners.")
        }
    }

    # AD1/AD3: three documents gave three different accounts of what one retained iteration review
    # contains, and one of them was false. The W review records AA1-AA3 and AB1-AB2 as headed
    # findings with corrected dispositions; its own scope line stopped at AA, the policy's roster
    # entry for it stopped at Z, and the AC review's residual note said the AA and AB passes "still
    # have no retained iteration review" and referred the resulting gap to the owner. A retained
    # record whose own description understates it sends the next reviewer to reconstruct evidence
    # that already exists, which is the opposite of what retaining it is for.
    #
    # Checked as a class rather than over these three statements: every family a review records must
    # be named by that review's scope line and by its roster entry, and no review may deny a record
    # that exists. A pass that extends a retained review and forgets to say so fails here.
    # Bounded at the next heading: the disclosed-deviation sections below the roster link the same
    # files, and an unbounded section reads those links as roster entries.
    $iterationRoster = [regex]::Match($reviewReadme, '(?ms)^## Retained iteration reviews\r?\n(.+?)(?=^## |\z)').Groups[1].Value
    if (-not $iterationRoster) {
        $failures.Add('The review policy carries no retained-iteration-review roster. That section is where a reader learns which author-side passes left evidence, and the two-kinds-of-review section requires each to be retained.')
    }
    $recordedFamilies = @()
    foreach ($reviewFile in $iterationReviewFiles) {
        $iterationRaw = Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8
        $families = @([regex]::Matches($iterationRaw, '(?m)^### ([A-Z]{1,2})[0-9]+ ') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        $recordedFamilies += $families

        # The scope line is the review's own account of what it covers. Two conventions are in use
        # and both are legitimate: naming the corrections reviewed, or enumerating the families the
        # document records. Only a *partial* enumeration is the defect, so this fires when a scope
        # line names some of its own families and not the rest.
        $scopeMatch = [regex]::Match($iterationRaw, '(?ms)^Reviewed[^:\r\n]*:(.+?)\r?\n\r?\n')
        if (-not $scopeMatch.Success) {
            $failures.Add("'$($reviewFile.Name)' carries no 'Reviewed' scope line, so nothing states which corrections it examined.")
        }
        else {
            $scopeText = Get-FlowedText $scopeMatch.Groups[1].Value
            $enumerated = @($families | Where-Object { $scopeText -cmatch "\b$_[0-9]" })
            if ($enumerated.Count -gt 0) {
                foreach ($family in $families) {
                    if ($scopeText -cnotmatch "\b$family[0-9]") {
                        $failures.Add("'$($reviewFile.Name)' enumerates the families it records and omits '$family', which it also records. A partial enumeration is read as the whole of it, and the omitted family looks like a pass that left no evidence.")
                    }
                }
            }
        }

        if ($iterationRoster) {
            $rosterEntry = @($iterationRoster -split '(?m)^- ' | Where-Object { $_.IndexOf($reviewFile.Name, [System.StringComparison]::Ordinal) -ge 0 })
            if ($rosterEntry.Count -ne 1) {
                $failures.Add("The retained-iteration-review roster does not carry exactly one entry for '$($reviewFile.Name)'.")
            }
            else {
                $rosterText = Get-FlowedText $rosterEntry[0]
                foreach ($family in $families) {
                    if ($rosterText -cnotmatch "\b$family[0-9]") {
                        $failures.Add("The roster entry for '$($reviewFile.Name)' does not name the '$family' family, which that review records. The roster is what tells a fresh reviewer where a family's reasoning already lives.")
                    }
                }
            }
        }
    }

    # A retained review may not deny a record that exists. The claim's subject precedes the phrase,
    # so the sentence containing each occurrence carries the families being denied. AS3 replaced the
    # earlier character window after moving the family farther away in the same sentence took it green.
    #
    # This reads assertion and quotation alike: a later pass retracting such a claim must not restate
    # it verbatim beside the families it names, which is a constraint on how a retraction is worded
    # rather than a defect in it. Detecting the difference would mean parsing negation, and a check
    # that guesses at that would fail open on the assertions this exists to catch.
    $recordedFamilies = @($recordedFamilies | Sort-Object -Unique)
    foreach ($reviewFile in $iterationReviewFiles) {
        $flowedIteration = Get-FlowedText (Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8)
        foreach ($denial in @([regex]::Matches($flowedIteration, 'no retained iteration review'))) {
            $sentence = Get-SentenceAt -Content $flowedIteration -Index $denial.Index
            foreach ($family in $recordedFamilies) {
                if ($sentence -cmatch "\b$family[0-9]") {
                    $failures.Add("'$($reviewFile.Name)' states that the '$family' pass left no retained iteration review, and a retained review records that family's findings under its own headings. The next reviewer is sent to reconstruct evidence that already exists, or to re-decide a question that is already answered.")
                }
            }
        }
    }

    $iterationRecords = @($iterationReviewFiles | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName -Encoding UTF8 })
    $flowedReviewPolicy = Get-FlowedText $reviewReadme
    # AD2, ruled a defect by closure review 9. The comment above asserts this check is written over
    # the general class; it was a membership test of two literals, evaluating 2 of the 36 finding ids
    # the policy bolds, and could not fail for AA, AB, AC, or AD. The false comment was the worse
    # half -- it is what a later reader trusts instead of reading the loop, which is AD1's mechanism.
    #
    # The class is derived rather than listed: the policy's own next-work steps say which families an
    # iteration pass found, so only those carry the retained-record obligation. Families a closure
    # review raised are excluded by construction, because no iteration pass owns them.
    # AF6: the previous form derived this class from one sentence shape in the next-work steps, and
    # two iteration-attributed groups did not carry it -- V, a whole family, and W5/W6. That is the
    # defect AD2 was ruled for, an order of magnitude smaller: a comment claiming a class over code
    # that tests a subset. The class is now *declared* rather than inferred from prose, and the
    # declaration is required to be total, so a family cannot be added without being classified.
    if ($familyProvenance.Count -lt 1) {
        $failures.Add('The review policy declares no finding-family provenance table. That table is what makes the retained-record obligation checkable over the whole class rather than over whichever families a sentence shape happens to match.')
    }
    else {
        $boldedFamilies = @([regex]::Matches($flowedReviewPolicy, '\*\*([A-Z]{1,2})[0-9]+\*\*') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        foreach ($bolded in $boldedFamilies) {
            if (-not $familyProvenance.ContainsKey($bolded)) {
                $failures.Add("The finding-family provenance table does not classify '$bolded', which the review policy names. An unclassified family carries no retained-record obligation and is invisible to every check written over that class.")
            }
        }
        foreach ($classified in $familyProvenance.GetEnumerator()) {
            if ($classified.Value -ne 'iteration') { continue }
            if (-not ($iterationRecords | Where-Object { $_ -cmatch "\b$($classified.Key)[0-9]" })) {
                $failures.Add("The provenance table classifies '$($classified.Key)' as an author-side iteration family, and no retained iteration review records it. The two-kinds-of-review section requires that pass to be retained as evidence; a commit message is not the evidence trail.")
            }
        }
        # AK2: the class "families a retained iteration review records" is DECLARED in the table above
        # and was derived below from `### <family><n> ` headings inside those reviews. Those two
        # populations differ by exactly one family -- `W`, whose findings the W review records in a
        # table rather than under headings -- so the AE4 check could not ask the Channel index for the
        # family the retained record is named after, and the row has omitted it since AE4's own
        # correction. Deriving from the declaration is what AF6 already did for the check thirty lines
        # above; the headings are kept as well, so a family recorded but not declared still counts.
        $recordedFamilies = @($recordedFamilies + @($familyProvenance.GetEnumerator() | Where-Object { $_.Value -eq 'iteration' } | ForEach-Object { $_.Key }) | Sort-Object -Unique)
    }

    # The backstop on the second axis, and the reason it is not an exemption. A family is classified by
    # the author of the finding, and this programme's recurring defect is an author mis-scoping their
    # own work -- so the classification is checked against something outside itself: a `verification`
    # family may not be named by any design artifact. The package's own convention is that a
    # correction names the finding it closes, so a finding whose correction reached the design says so
    # in the design, and this fires. `design` needs no such check: naming it in the design artifacts is
    # what that class already requires.
    foreach ($subjectFamily in @($familySubject.GetEnumerator() | Where-Object { $_.Value -eq 'verification' })) {
        # Over the nine design artifacts, and neither index. The Channel index's Design reviews row is
        # REQUIRED to name every iteration family by the AE4 rule above, because that row states what
        # the reviews directory holds -- a fact about records rather than a disposition of a finding --
        # and the review policy is where the family is classified in the first place.
        foreach ($subjectArtifact in $artifactNames) {
            if ($subjectArtifact -eq 'README.md' -or $subjectArtifact -eq 'reviews\README.md') { continue }
            if ((Read-RequiredText $subjectArtifact) -cmatch "\b$($subjectFamily.Key)[0-9]") {
                $failures.Add("'$subjectArtifact' names a finding in the '$($subjectFamily.Key)' family, which the provenance table classifies as raised against the verification work rather than the design. Either the classification is wrong -- a finding whose correction reached a design artifact is a design family whatever its author called it -- or the design artifact is carrying a disposition that belongs in the verification foundation plan.")
            }
        }
    }

    # AE4: AD3's check covers a retained review's own scope line and its roster entry. The Channel
    # index is a third surface describing the same documents, it is the entry point AA1 was raised
    # against, and it named neither AA nor AB. The AA1 family check passes because it asks only that
    # a family appear somewhere in the index, and both appear higher up.
    if ($indexReviewRow.Count -eq 1) {
        foreach ($family in $recordedFamilies) {
            if ($indexReviewRow[0] -cnotmatch "\b$family[0-9]" -and $indexReviewRow[0] -cnotmatch "\b$family\b") {
                $failures.Add("The Channel index's Design reviews row does not name the '$family' family, which a retained iteration review records. This is the third surface describing those documents, and a description that understates them is what sent the AC pass to deny records that existed.")
            }
        }
    }
}

# AE1: closure review 9 found `C4-P2`'s first conjunct red on a conforming realization. When the
# transport loses the request, the initiator legally commits its one cancellation control -- C8 says
# recipient admission is not observable from `dispatched` -- and the control lands at `unseen`,
# producing exactly the refusal the conjunct forbade of an endpoint that had already committed the
# request. Loss of either frame is a *required* member of the property's adversarial group, and the
# loss vector and `C4-control-precedes-request` presented identical values in every field the property
# may read, so the design had no third option: either the property failed on legal behaviour or its
# own mutation was green, which is U1 by another route.
#
# The 2026-08-14 owner ruling resolves it by reading the fact that already separates them: a
# reordering delivers the request afterwards and the recipient admits an interaction for that
# identity, while a loss never does. These pin the ruling at each artifact that has to carry it.
$flowedContract = Get-FlowedText $contract
$c4p2Index = $flowedContract.IndexOf('**Property C4-P2.**', [System.StringComparison]::Ordinal)
if ($c4p2Index -lt 0) {
    $failures.Add('The capability contract states no `C4-P2`, which is the property intra-interaction frame order rests on.')
}
else {
    # Bounded at the start of C5 rather than by a character count: the AE1 correction added an
    # explanation between the conjuncts and the required-green set, and a fixed window stopped short
    # of the paragraph it was meant to read.
    $c4SectionEnd = $flowedContract.IndexOf('## C5 ', $c4p2Index, [System.StringComparison]::Ordinal)
    if ($c4SectionEnd -lt 0) { $c4SectionEnd = $flowedContract.Length }
    $c4p2Text = $flowedContract.Substring($c4p2Index, $c4SectionEnd - $c4p2Index)

    # The conjunct check reads the property's own statement paragraph, not the section: the paragraph
    # explaining the ruling names the same fact, so a section-wide search passes after the conjunct
    # itself has been gutted. The two checks in this block deliberately use different scopes, and
    # widening one to fix its own miss is what broke the other.
    $c4p2Statement = Get-FlowedText ([regex]::Match($contract, '(?ms)\*\*Property C4-P2\.\*\*(.*?)\r?\n\r?\n').Groups[1].Value)
    if ($c4p2Statement.IndexOf('admits an interaction for that identity', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('`C4-P2`''s first conjunct does not require that the recipient later admits an interaction for the refused identity. Without it the conjunct is satisfied by a conforming realization whose request was lost, and it cannot be told apart from `C4-control-precedes-request`, which presents identical values in every field the property may read. This is AE1.')
    }
    # Scoped to the required-green paragraph, not to the property's whole window. Mutation testing
    # found the window form passing on the surrounding explanation, which says "a lost request and a
    # reordered one" -- the phrase-anywhere weakness X1's check was rescoped for.
    # Bounded at the paragraph, not by a character count: a 700-character window reached into the
    # Evidence and Silence passages below, and AF5's missing members were reported present because a
    # neighbouring paragraph happened to use the same words.
    $requiredGreen = [regex]::Match($contract, '(?ms)\*\*Required green\.\*\*(.*?)\r?\n\r?\n')
    if (-not $requiredGreen.Success) {
        $failures.Add('`C4-P2` states no required-green set. C12 requires every property to name the legal inputs it must not fail on, and this is the property that was red on one of them.')
    }
    elseif ($requiredGreen.Groups[1].Value.IndexOf('lost', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('`C4-P2`''s required-green set does not name the lost-request case. "Loss may still drop a frame" is not expressible in the closed operator set -- there is no "was this lost" operand -- so the carve-out has to be a named green input an evaluator can run, not a sentence it cannot apply.')
    }
}

# The conjunct now reads a fact, so the parity profile has to compare it. A property that reads a
# fact the profile excludes is W6, and one that reads a fact no observation carries is Y1.
$flowedBrief = Get-FlowedText $neutralBrief
if ($flowedBrief.IndexOf('admission of an identity previously refused at `unseen`', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The normative parity profile does not compare the admission of an identity previously refused at `unseen`, which is the fact `C4-P2`''s corrected first conjunct reads. A conjunct reading a fact the profile does not compare is W6 restored.')
}

# AE3 is the structural half, and the reason ten passes audited falsifiability and none audited
# soundness: C12 requires a property to be able to fail and nothing required one to stay green, so
# the loss vector sat in the required group with no stated expectation at all.
if ((Get-FlowedText $contract).IndexOf('must not fail against a conforming realization', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('C12 requires every property to be able to fail and does not require it to hold on conforming input. Falsifiability and soundness are the same defect measured from opposite ends, and only one of them was ever written down as a rule. This is AE3.')
}
# Asserted against the format's bullet list rather than the brief at large, for the same reason: the
# paragraph explaining the field also names it, so a whole-document search passes on the explanation
# after the required field itself has been renamed away.
if ($neutralBrief -cnotmatch '(?m)^- a \*\*required-green set\*\*') {
    $failures.Add('The capability-wide property format does not list a required-green set as a field. Its other fields name one mutation that must fail and no input that must not, so a property that goes red on legal behaviour satisfies the format completely.')
}
$propertyAudit = ($completeness -split '## Per-capability property audit', 2)[1]
if ($propertyAudit -and (($propertyAudit -split '(?m)^## ')[0]).IndexOf('Required-green', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The per-capability property audit has no required-green column. It asks whether each property has a mutation that must fail and never what must leave it green, which is the audit AE1 passed through ten times.')
}

# AE2: X3 and Y3 made the `unseen` refusal a detailed row precisely so the machine's totality rule
# does not claim it -- and the totality rule is what supplied effect certainty. The grid requires
# every cell to assert one and C10-P1 requires each observation to be complete for its provenance.
foreach ($unseenArtifact in @(@{ Name = 'interaction state machine'; Text = $interaction }, @{ Name = 'state/event coverage grid'; Text = $stateEventCoverage })) {
    $unseenRows = @($unseenArtifact.Text -split "`r?`n" | Where-Object { $_.IndexOf('`unseen`', [System.StringComparison]::Ordinal) -ge 0 -and $_.IndexOf('unopened-interaction-identity', [System.StringComparison]::Ordinal) -ge 0 })
    foreach ($unseenRow in $unseenRows) {
        if ($unseenRow.IndexOf('known-none', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("The $($unseenArtifact.Name)'s `unseen` refusal row does not state effect certainty. The grid requires every cell to assert one and C10-P1 requires each observation to be complete for its provenance form; one implementer writes ``known-none`` and another ``not-applicable``, which is not in C10's closed set, and both are defensible. This is AE2.")
        }
    }
}

# AE5: the retained requirements ledger states it "must be dispositioned item by item in the
# successor's migration ledger". No `CH-R` id appeared anywhere in the 0.2 package, and CH-R10 is the
# non-promise the S1 ruling narrowed -- the entry every finding since S1 turns on. This is Z4's class
# one artifact further out.
if ($migration.IndexOf('CH-R10', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The migration ledger does not disposition `CH-R10`, the retained requirements ledger''s ordering non-promise. That ledger instructs item-by-item disposition in the successor, and CH-R10 is the register entry the 2026-08-13 S1 ruling changed. This is AE5.')
}

# AF1, blocking in closure review 10. The AE1 correction made `C4-P2`'s first conjunct read a second
# fact -- the recipient's subsequent admission -- and left untouched the two passages that say what
# the mutation vector's expected observations *are*. A vector authored from those passages carries the
# refusal alone, the membership test finds an empty admitted set, and the property is green on
# `C4-control-precedes-request`. That is U1 restored, four paragraphs above the property it was fixed
# in, and the two paragraphs of C4 contradict each other while both gates stayed green.
#
# AQ5, the half that matters most. The capture was 900 characters and the passage is longer than
# that, so the positive assertion below still passed -- its subject is early -- while the negative
# one policed only the passage's first 900 characters. Restoring AF1's own superseded wording at the
# far end of the same passage reproduces AF1 verbatim with the gate green; that probe is `AQ5-b`.
#
# This is the general shape and it is worth stating once, because it is what the next pass should
# hunt. A character-bounded window fails *safely* for an assertion that something must be present:
# truncation makes the check fail loudly. It fails *silently* for an assertion that something must
# be absent, because the forbidden text simply sits past the boundary. So a negative assertion must
# never be evaluated over a window a character count bounds. Both of the ones in this file were, and
# both are now bounded by the end of their own subject.
$expectedObservationsBoundary = 'These recorded facts, and not the refusals alone'
$expectedObservations = [regex]::Match($flowedContract, "Their expected observations(.+?)(?=$([regex]::Escape($expectedObservationsBoundary))|\z)")
if (-not $expectedObservations.Success) {
    $failures.Add('C4 states no expected observations for its ordering mutation vectors. C12 requires every vector to carry complete data rather than an unspecified expectation, and these are the vectors `C4-P2` must go red on.')
}
elseif ($flowedContract.IndexOf($expectedObservationsBoundary, [System.StringComparison]::Ordinal) -lt 0) {
    # The boundary is asserted rather than assumed. Without it the region above runs to the end of
    # the contract, which is the same defect one size larger: a negative assertion whose extent
    # nothing declares.
    $failures.Add("C4's expected-observation passage no longer ends at '$expectedObservationsBoundary', which is the sentence naming the witnesses ``C4-P2`` fails on and the declared end of the region the assertions below read. A region with no declared end is the AQ5 defect restored one size larger.")
}
else {
    if ($expectedObservations.Groups[1].Value -notmatch 'admi(ts|ssion)') {
        $failures.Add('C4''s expected-observation passage for the ordering mutation vectors does not include the recipient''s subsequent admission, which the corrected first conjunct reads. A vector authored from this passage leaves the membership test an empty set and the property green on its own named mutation. This is AF1, and it is U1 reached through the vector rather than through the property.')
    }
    # Two assertions, because a requirement-only check is satisfied while the superseded claim still
    # stands beside it, and the superseded claim is the one a vector author would follow.
    if ($expectedObservations.Groups[1].Value.IndexOf('exactly what the receiving endpoint records', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add('C4 still states that the ordering mutations'' expected observations are exactly what the receiving endpoint records. That is the AF1 wording: it names the refusal alone as complete data, and a vector authored from it takes `C4-P2` green on its own named mutation.')
    }
}

# AF5: the field is defined as "the named legal inputs from the property's own required vector group",
# and the group has seven legal members. The set named four. Conforming commit-order delivery is the
# sharpest omission -- a property red on plain conforming delivery is the worst available failure and
# was the one case unnamed. The previous check tested only that `lost` appeared, which is narrower
# than the rule it guards; that is AD2's shape in the check written to close AE1.
foreach ($requiredGreenMember in @(
    @{ Phrase = 'conforming commit-order delivery'; Why = 'conforming commit-order delivery in either direction' },
    @{ Phrase = 'acknowledgement'; Why = 'a lost acknowledgement, the second half of "loss of either frame"' },
    @{ Phrase = 'never opened'; Why = 'a control for an identity the peer never opened' },
    @{ Phrase = 'late control'; Why = 'a legal late control after a peer''s terminal' },
    @{ Phrase = 'duplicate terminal'; Why = 'a duplicate terminal from a nonconformant peer' })) {
    if ($requiredGreen.Success -and $requiredGreen.Groups[1].Value.IndexOf($requiredGreenMember.Phrase, [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("``C4-P2``'s required-green set does not name $($requiredGreenMember.Why). C12 defines the field as the legal inputs drawn from the property's own required vector group, and a member with no stated expectation is the exact condition AE1 arose from.")
    }
}

# AF8: the operand reads the vector and interaction identity is unique only per session, so a
# two-session vector could carry one identity value twice -- a refusal at `unseen` in one and an
# admission in the other -- and take the conjunct red on conforming behaviour. AE1's failure mode
# reached through the operand's scope instead of through the missing clause.
foreach ($membershipArtifact in @(@{ Name = 'capability contract'; Text = $flowedContract }, @{ Name = 'neutral brief'; Text = $flowedBrief })) {
    # The window stops at the scope phrase itself. A wider one reached the paragraph explaining why
    # the scope is the session, which says "session" and passed after the operand had been reverted.
    $membershipScope = [regex]::Match($membershipArtifact.Text, 'the recipient admits\s*\*{0,2}(?:in|within) the same (\w+)')
    if ($membershipScope.Success -and $membershipScope.Groups[1].Value -cne 'session') {
        $failures.Add("The $($membershipArtifact.Name) scopes ``C4-P2``'s membership operand to the vector and not to the session. Interaction identity is unique within a session, so a vector carrying two sessions may hold one identity value twice and satisfy the test across them, taking the conjunct red on conforming behaviour. This is AF8.")
    }
}

# AF7: C12's rule is written over "every property", and the audit enforcing it once had twelve rows
# while the two state machines stated thirteen more properties under the same heading.
#
# AP2 moved the enforcement rather than extending it here. This check listed four ids -- `S1`, `S6`,
# `I1`, `I7` -- to stand for the class its own comment says is every property, so a row that kept its
# text and lost its property id passed for the other twenty-two, probed. The set of properties is
# `conformance/channel-0.2-properties.json`'s, so the registration check lives in the gate that reads
# it and is written over the declared set: `build/verify-channel-0.2-properties.ps1`. Keeping a
# sample here beside a total check there would be the second enumeration AN2 was.

# AF2: AE4 corrected the Channel index's Design reviews row and left the narrative above it, which
# still named a correction sequence five families stale and called a corrected finding an open owner
# call. The review that raised it warned that a pass updating the counting sentence without reading
# the narrative would let the finding survive the commit that closes it, because no check read those
# lines. This one reads them: the design-foundation intro must name the latest disposition family.
$latestDispositionFamily = @($dispositionFamilies | Sort-Object { $_.Length }, { $_ } | Select-Object -Last 1)
$channelIntro = [regex]::Match($channelReadme, '(?ms)^## Channel 0\.2 design foundation\r?\n(.+?)(?=^\| Artifact)').Groups[1].Value
if (-not $channelIntro) {
    $failures.Add('The Channel index has no design-foundation introduction, which is the narrative a reader meets before the artifact table.')
}
elseif ($latestDispositionFamily) {
    # The claim this reads is the stated *range* of the pending review, not a mention anywhere in the
    # narrative: the passage that reports AF2 names the AF family while the range sentence beside it
    # can still say Z4, which is the staleness AF2 was.
    $sequenceRange = [regex]::Match((Get-FlowedText $channelIntro), 'runs from S1 through \*{0,2}([A-Z]{1,2})[0-9]')
    if (-not $sequenceRange.Success) {
        $failures.Add('The Channel index''s design-foundation narrative states no range for the correction sequence the pending review covers. That sentence is what tells a reader how far the programme has gone.')
    }
    elseif ($sequenceRange.Groups[1].Value -cne $latestDispositionFamily[0]) {
        $failures.Add("The Channel index says the pending review covers the sequence through the '$($sequenceRange.Groups[1].Value)' family, and the disposition history records '$($latestDispositionFamily[0])'. AE4 corrected the counting row below this passage and left this sentence; a count is not the only thing in an index that goes stale. This is AF2.")
    }
}

# AF3: the ledger's completion check enumerates what it claims to have inventoried and did not claim
# the requirements register that AE5 had just added to its sources, and the new disposition understated
# the register's own range. A coverage claim that does not cover is the AE5 class inside the AE5 fix.
$ledgerCompletion = [regex]::Match($migration, '(?ms)^## Ledger completion check\r?\n(.+?)(?=^## |\z)').Groups[1].Value
if ($ledgerCompletion -and (Get-FlowedText $ledgerCompletion) -notmatch 'requirements (?:and risk )?(?:ledger|register)') {
    $failures.Add('The migration ledger''s completion check does not claim the retained requirements register among the sources it inventories, although that register is now listed as a source and instructs item-by-item disposition. This is AF3.')
}
$registerPath = Join-Path $channelPath 'architecture-0.8-channel-requirements-and-risk-ledger.md'
if (Test-Path -LiteralPath $registerPath) {
    $registerText = Get-Content -Raw -LiteralPath $registerPath -Encoding UTF8
    foreach ($registerPrefix in @('CH-R', 'CH-K')) {
        $registerIds = @([regex]::Matches($registerText, "$registerPrefix(\d+)") | ForEach-Object { [int]$_.Groups[1].Value } | Sort-Object -Unique)
        if ($registerIds.Count -gt 0) {
            $highest = $registerIds[-1]
            if ($migration.IndexOf("$registerPrefix$highest", [System.StringComparison]::Ordinal) -lt 0) {
                $failures.Add("The migration ledger's register disposition does not reach '$registerPrefix$highest', which is the highest '$registerPrefix' the retained register states. A coverage claim computed from a smaller range than the source it inventories understates exactly what it exists to account for.")
            }
        }
    }
}

# AF4: Z4 put intra-interaction frame order into the new-evidence inventory and the bullet enumerates
# the observation fields those vectors compare. AE1 added a third and the bullet was not updated --
# Z4's class applied to the newest correction, in the artifact Z4 was raised against.
$newEvidence = [regex]::Match($migration, '(?ms)^## New evidence required by redesign\r?\n(.+?)(?=^## |\z)').Groups[1].Value
# Scoped to the enumeration clause, not the section: the sentence added below it explaining that the
# admission needs no new field also says "admission", and satisfied a section-wide search.
$comparedFields = [regex]::Match((Get-FlowedText $newEvidence), 'The observation fields those vectors compare(.{0,220})').Groups[1].Value
if ($newEvidence -and $comparedFields -notmatch 'admi(ts|ssion)') {
    $failures.Add('The migration ledger''s new-evidence inventory enumerates the observation fields the ordering vectors compare and does not name the admission the AE1 correction added. Batch 2 builds its vector groups from this list. This is AF4.')
}

# AG1: AF1 named two artifacts and quoted both. The correction closed C4 and stopped, and the check
# written for it searched the contract alone, so the completeness review's silence-probe row still
# said the expected observation is the recorded refusal -- the U1 condition surviving in the commit
# written to close it. This is the fourth instance of one shape: a correction closing the *first*
# artifact a finding's evidence names. The check therefore reads the second artifact directly.
$silenceProbeRow = @($completeness -split "`r?`n" | Where-Object { $_.IndexOf('control delivered before the request it names', [System.StringComparison]::Ordinal) -ge 0 })
if ($silenceProbeRow.Count -ne 1) {
    $failures.Add('The completeness review does not carry exactly one silence-probe row for a control delivered before the request it names. That row states the ordering mutation''s expected observation, and it is the second artifact AF1''s evidence named.')
}
elseif ([regex]::Match($silenceProbeRow[0], 'expected observation is(.{0,170})').Groups[1].Value -notmatch 'admi(ts|ssion)') {
    $failures.Add('The completeness review''s silence-probe row states the ordering mutation''s expected observation without the recipient''s subsequent admission, which `C4-P2`''s first conjunct reads. A vector authored from this row takes the property green on its own named mutation, which is AF1 surviving in the artifact its own evidence named second. This is AG1.')
}

# AG2 is a different and sharper class: a correction asserting something about an artifact it did not
# open. C4 claims the precedence relation carries AF8's session qualifier; the brief's operator set
# did not carry it, and a conforming two-session vector goes red without it. Cross-artifact claims are
# pinned here so a sentence about another document cannot be written without that document agreeing.
$precedenceOperator = [regex]::Match($flowedBrief, 'precedence between two steps in one(.{0,160})')
if (-not $precedenceOperator.Success) {
    $failures.Add('The neutral brief''s closed operator set states no precedence relation, which is the operator `C4-P2` needs and W1 added.')
}
elseif ($precedenceOperator.Groups[1].Value -notmatch 'session') {
    $failures.Add('The neutral brief''s precedence relation is scoped to one endpoint and one interaction identity with no session qualifier, while C4 asserts it carries AF8''s session qualifier. An interaction identity is unique only within a session, so a conforming two-session vector reusing one identity value goes red under the operator as published. This is AG2, and it is a claim one artifact makes about another it did not open.')
}

# AG3: the dated AE1 owner ruling still stated the operand scope AF8 corrected, and C4 defers to that
# ruling, so the contract and the ruling it cites disagreed. The S1 ruling carries a retained-as-issued
# note for exactly this situation; this requires the same treatment rather than a silent rewrite.
#
# AQ5's second instance, and the same shape as the AF1 window above. The capture was 2,600
# characters and the ruling runs to 6,923, so the AG3 assertion below policed the first 38% of the
# passage it names. The pre-AF8 operand scope restored among the rejected options -- inside this
# ruling, past the boundary -- reproduces AG3 with both gates green; that probe is `AQ5-c`.
#
# The ruling's extent is the plan's own: a dated ruling runs to the next dated ruling. That boundary
# is asserted below rather than assumed, for the reason the AF1 one is.
$flowedPlan = Get-FlowedText $plan
$ae1Ruling = [regex]::Match($flowedPlan, '2026-08-14.{0,6}AE1 correction ruling(.+?)(?=- \*\*20[0-9]{2}-[0-9]{2}-[0-9]{2}|\z)')
if (-not $ae1Ruling.Success) {
    $failures.Add('The redesign plan records no dated 2026-08-14 AE1 correction ruling, which is what C4 cites for the subsequent-admission clause.')
}
elseif ($ae1Ruling.Groups[1].Value.Length -ge ($flowedPlan.Length - $ae1Ruling.Index - 64)) {
    $failures.Add('The dated AE1 correction ruling runs to the end of the redesign plan, so no later dated ruling bounds it and the assertions below read whatever follows it. A dated ruling is bounded by the next dated ruling; without one the region has no declared end, which is the AQ5 defect.')
}
else {
    if ($ae1Ruling.Groups[1].Value -match 'admits within the vector|membership of the identity in the set the recipient admits within the vector') {
        $failures.Add('The dated AE1 owner ruling still states the membership operand is scoped within the vector, which AF8 corrected to the session. C4 defers to this ruling, so the contract and the ruling it cites disagree about the scope of the operand the conjunct reads. This is AG3.')
    }
    if ($ae1Ruling.Groups[1].Value.IndexOf('AF8', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The dated AE1 owner ruling carries no note that AF8 narrowed the operand scope it was issued with. A dated ruling is retained as issued and annotated, as the S1 ruling is for `channel-core`; a silent rewrite loses the fact that the scope was decided twice.')
    }
}

# AG4, AH4 and AJ5 are retired here, and this is the second half of W3.
#
# They policed the Channel index's eleven per-artifact rows for freshness: each row had to name the
# newest finding family or say the artifact was unchanged by it, because AE4 corrected the Design
# reviews row, AF2 corrected the narrative, and seven per-artifact rows still stopped at Z3/Y3/Z2/U2.
# AH4 then closed the escape clause bound to no family, and AJ5 closed the escape naming one finding
# of a family as though it spoke for the family. Three checks over one surface, each written from the
# shape of the finding before it, which is section 1.4 of the plan exactly.
#
# The rows now carry no disposition history at all -- 8,746 characters of it moved verbatim to the
# disposition index -- so there is nothing left in them to go stale. A row states what the artifact is
# for and points at the record. The freshness question is asked once, of the disposition index, by the
# W3 check further down, and the pointer is what this check requires.
if ($latestDispositionFamily) {
    $artifactRows = @($channelReadme -split "`r?`n" | Where-Object { $_ -match '^\| \[[^\]]+\]\(\./(?:Brontide-Channel-|reviews/)' })
    if ($artifactRows.Count -lt 1) {
        $failures.Add('The Channel index carries no per-artifact rows, which are what tell a reader the state of each design artifact.')
    }
    foreach ($artifactRow in $artifactRows) {
        $rowName = [regex]::Match($artifactRow, '^\| \[([^\]]+)\]').Groups[1].Value
        if ($artifactRow.IndexOf('reviews/channel-0.2-disposition-index.md', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("The Channel index row for '$rowName' does not point at the disposition index. A row that carries its own correction history is a second place that history has to be kept current, and keeping it current is what AE4, AF2, AG4, AH4 and AJ5 were each about.")
        }
        # AN1, on the second of the two surfaces whose whole statement about disposition is a pointer.
        # The row's own artifact is not asserted here: three of the eleven rows -- the reviews
        # directory, the verification foundation plan, and the Channel index's own extra rows -- point
        # at sections that are not per-artifact, so resolution is the question this surface can answer.
        Assert-DispositionPointer -Surface "The Channel index row for '$rowName'" -Text $artifactRow -ArtifactName ''
        # The bound is what retires the surface rather than relocating it: a row that keeps its
        # pointer and grows a clause beside it is the old row with a link added.
        $rowState = @($artifactRow -split '\|')
        # The Design reviews row is bounded higher, and the reason is stated rather than fudged: the
        # AE4 check above REQUIRES it to name every family a retained iteration review records, nine
        # of them, because a pass once denied records that existed. That enumeration is a fact about
        # what the directory holds rather than disposition history, so it is what the row is for.
        $rowBound = if ($rowName -eq 'Design reviews') { 300 } else { 220 }
        if ($rowState.Count -ge 4 -and $rowState[3].Trim().Length -gt $rowBound) {
            $failures.Add("The Channel index row for '$rowName' carries $($rowState[3].Trim().Length) characters of state and the bound is $rowBound. Disposition history belongs in the disposition index; these eleven cells averaged 795 characters of it before W3.")
        }
    }
}

# AM2 and AM3. Section 4 of the verification foundation plan carries five measures and one of them --
# properties executable in the gate -- is recomputed by the properties gate. Of the four left to prose,
# the two this pass could recompute were both wrong: the status-block total read 289 where no reading
# of the commit gives more than 283, and the index-row total read 1,208 where the commit that produced
# it gives 1,306. The two the gate already determines were right. That is the plan's own thesis
# arriving in the plan, so both are recomputed here, historical half included -- a measure section
# whose numbers are read rather than derived is a stale-number finding waiting for the cycle that
# checks it.
$measurePlanText = Get-Content -Raw -LiteralPath (Join-Path $channelPath 'Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md') -Encoding UTF8
# A historical blob is read as UTF-8 explicitly. Windows PowerShell decodes a native command's output
# with the console code page, which turns every em dash in these artifacts into three characters -- so
# the first form of this check measured 8,754 characters at a commit that holds 8,746, and would have
# failed a correct measure. It was caught only because the same code gave a different answer outside
# the verifier, which is how quiet a mis-decoding is.
function Get-BlobText {
    param([Parameter(Mandatory = $true)][string]$Revision, [Parameter(Mandatory = $true)][string]$RepositoryPath)
    $previousEncoding = [Console]::OutputEncoding
    try {
        [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
        return ((Invoke-Git @('show', "${Revision}:${RepositoryPath}")) -join "`n")
    }
    finally { [Console]::OutputEncoding = $previousEncoding }
}
function Measure-StatusBlockLines {
    param([Parameter(Mandatory = $true)][scriptblock]$ReadArtifact)
    $measured = 0
    foreach ($measuredArtifact in $artifactNames) {
        if ($measuredArtifact -eq 'README.md' -or $measuredArtifact -eq 'reviews\README.md') { continue }
        $measuredText = & $ReadArtifact $measuredArtifact
        if (-not $measuredText) { continue }
        $measuredMatch = [regex]::Match($measuredText, '(?ms)^((?:\*\*)?Status:.*?)(?=\r?\n\s*\r?\n|\z)')
        if (-not $measuredMatch.Success) { continue }
        $measured += @($measuredMatch.Groups[1].Value.Trim() -split "`r?`n" | Where-Object { $_.Trim() }).Count
    }
    return $measured
}
function Measure-IndexRowCharacters {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$IndexText)
    $measured = 0
    foreach ($measuredRow in @($IndexText -split "`r?`n" | Where-Object { $_ -match '^\| \[[^\]]+\]\(\./(?:Brontide-Channel-|reviews/)' })) {
        $measuredCells = @($measuredRow -split '\|')
        if ($measuredCells.Count -ge 4) { $measured += $measuredCells[3].Trim().Length }
    }
    return $measured
}
# The third measure, pinned for AM2's reason: this file's own length. It is the measure that says
# whether the design verifier is still absorbing the cost of a structural problem, and a number about
# THIS file that this file does not compute is the one most likely to be left behind by the commit
# that changes it -- which is what 289 and 1,208 both were.
$measureLineClaim = [regex]::Match($measurePlanText, 'design-verifier lines\*\* . \*\*([0-9,]+)\*\* now')
if (-not $measureLineClaim.Success) {
    $failures.Add("The verification foundation plan's section 4 no longer states the design-verifier line count in the form '**<n>** now'. It is one of the five measures that section exists to keep honest and the only one about this file.")
}
else {
    $measureLineActual = @(Get-Content -LiteralPath $PSCommandPath -Encoding UTF8).Count
    if ([int]($measureLineClaim.Groups[1].Value -replace ',', '') -ne $measureLineActual) {
        $failures.Add("The verification foundation plan says this verifier is $($measureLineClaim.Groups[1].Value) lines and it is $measureLineActual.")
    }
}
# AN3: the same measure's HISTORY, which is the half AM2 and AM3 corrected in the two measures beside
# this one and left standing here. It read "it fell at each step" over four deltas, and it rose at two
# of them; three of the four numbers -- 169 out, 32 out, 182 back -- are produced by no reading of the
# commits they describe, gross or net. So each step is now stated as a commit and a line count and
# every one of them is recomputed, which is the only form of this measure that cannot go stale
# silently. The claim that the file grew overall is checked too, because that is the uncomfortable
# half and the half a later edit would be tempted to drop.
if ($measureLineClaim.Success -and (Test-Path -LiteralPath (Join-Path $repositoryRoot '.git'))) {
    $measureStepBullet = [regex]::Match($measurePlanText, '(?ms)^- \*\*design-verifier lines\*\*.+?(?=^- \*\*|^## )')
    $measureSteps = @([regex]::Matches($measureStepBullet.Value, '`([0-9a-f]{7,40})` \*\*([0-9,]+)\*\*'))
    if ($measureSteps.Count -lt 2) {
        $failures.Add("The verification foundation plan's design-verifier-lines measure states fewer than two steps in the form '``<commit>`` **<n>**'. That form is what lets this check recompute the history; a history stated only in prose is what AN3 was, and what AM2 and AM3 were in the two measures above it.")
    }
    foreach ($measureStep in $measureSteps) {
        $stepRevision = $measureStep.Groups[1].Value
        $stepClaimed = [int]($measureStep.Groups[2].Value -replace ',', '')
        $stepText = Get-BlobText -Revision $stepRevision -RepositoryPath 'build/verify-channel-0.2-design.ps1'
        if ($LASTEXITCODE -ne 0 -or -not $stepText) {
            $failures.Add("The verification foundation plan's design-verifier-lines measure names the commit '$stepRevision' and this verifier cannot be read at it.")
            continue
        }
        $stepActual = @($stepText -split "`n").Count
        if ($stepActual -ne $stepClaimed) {
            $failures.Add("The verification foundation plan says this verifier was $($measureStep.Groups[2].Value) lines at '$stepRevision' and it was $stepActual. This is AN3's own class: a number about a commit that nothing recomputes.")
        }
    }
}
$measureClaims = @(
    @{ Name = 'status-block lines across the nine artifacts'
       Pattern = 'status-block lines across the nine artifacts\*\* . \*\*([0-9,]+)\*\* at `([0-9a-f]{7,40})` and \*\*([0-9,]+)\*\* now'
       Now = { Measure-StatusBlockLines -ReadArtifact { param($name) Read-RequiredText $name } }
       Then = { param($rev) Measure-StatusBlockLines -ReadArtifact { param($name) Get-BlobText -Revision $rev -RepositoryPath "docs/future/channel/$($name -replace '\\', '/')" } } }
    @{ Name = 'Channel index row characters'
       Pattern = 'Channel index row characters\*\* . \*\*([0-9,]+)\*\* at `([0-9a-f]{7,40})` and \*\*([0-9,]+)\*\* now'
       Now = { Measure-IndexRowCharacters -IndexText $channelReadme }
       Then = { param($rev) Measure-IndexRowCharacters -IndexText (Get-BlobText -Revision $rev -RepositoryPath 'docs/future/channel/README.md') } }
)
foreach ($measureClaim in $measureClaims) {
    $measureMatch = [regex]::Match($measurePlanText, $measureClaim.Pattern)
    if (-not $measureMatch.Success) {
        $failures.Add("The verification foundation plan's section 4 no longer states the '$($measureClaim.Name)' measure in the form '**<then>** at ``<commit>`` and **<now>** now'. That form is what lets this check recompute it; a measure stated only in prose is one nobody recomputes, which is how it came to say 289 and 1,208.")
        continue
    }
    $claimedThen = [int]($measureMatch.Groups[1].Value -replace ',', '')
    $measureRevision = $measureMatch.Groups[2].Value
    $claimedNow = [int]($measureMatch.Groups[3].Value -replace ',', '')
    $actualNow = & $measureClaim.Now
    if ($claimedNow -ne $actualNow) {
        $failures.Add("The verification foundation plan says the '$($measureClaim.Name)' measure is now $claimedNow and it is $actualNow.")
    }
    if (Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')) {
        $actualThen = & $measureClaim.Then $measureRevision
        if ($LASTEXITCODE -eq 0 -and $actualThen -gt 0 -and $claimedThen -ne $actualThen) {
            $failures.Add("The verification foundation plan says the '$($measureClaim.Name)' measure was $claimedThen at '$measureRevision' and it was $actualThen. The historical half is checked as well because it is the half that was wrong: nothing recomputes a number a reader cannot reach.")
        }
    }
}

# AN5. Recomputing the plan's copy of a measure does nothing about a SECOND copy elsewhere, and there
# was one: AM2 corrected "289 lines" in section 2b and section 4 and named those as the two surfaces,
# and the disposition index -- the file W3 created, and the one a status block sends its reader to --
# said 289 for three more commits. That is the family this programme has recorded ten times, committed
# by the pass whose method was to recompute every number.
#
# The fix is removal rather than a guard over a second copy, which is W1's own remedy and this plan's
# stated preference: the index cites the measure now. This check keeps it that way. It is scoped to
# the MAINTAINED files -- a retained attestation or iteration review is immutable evidence under the
# review policy and legitimately records the readings it rejected, including 289 itself, so it cannot
# be corrected and is not swept.
$measureOwner = 'Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md'
$measureSweepFiles = @(
    @{ Name = 'the disposition index'; Text = $dispositionIndex },
    @{ Name = 'the Channel index'; Text = $channelReadme },
    @{ Name = 'the review policy'; Text = $reviewReadme },
    @{ Name = 'the future-work index'; Text = $futureIndexText }
)
foreach ($measureSweepFile in $measureSweepFiles) {
    $flowedMeasureText = Get-FlowedText $measureSweepFile.Text
    foreach ($restated in [regex]::Matches($flowedMeasureText, '([0-9][0-9,]{1,6}) lines?\b')) {
        $restatedSentence = Get-SentenceAt -Content $flowedMeasureText -Index $restated.Index
        if ($restatedSentence -match '(?i)status(?: |-)block') {
            $failures.Add("$($measureSweepFile.Name) states a status-block line total of $($restated.Groups[1].Value). That measure is owned and recomputed in '$measureOwner'; a second copy is a number nothing recomputes, which is what said 289 for three commits after AM2 corrected the two surfaces it had found. Cite the measure instead of restating it. This is AN5.")
        }
    }
}

# AG5: the same staleness in the future-work index's Channel row, which is the one a reader meets
# while choosing what to work on.
if ($latestDispositionFamily) {
    $futureChannelRow = @($futureIndexText -split "`r?`n" | Where-Object { $_ -match '^\| Channel \|' })
    if ($futureChannelRow.Count -eq 1 -and $futureChannelRow[0] -cnotmatch "\b$($latestDispositionFamily[0])[0-9]") {
        $failures.Add("The future-work index's Channel row enumerates the correction families and does not reach '$($latestDispositionFamily[0])'. This is the row a reader consults while choosing what to work on. This is AG5.")
    }
}

# AH3: three narrative surfaces stopped one family short, and one asserted that no independent review
# had seen the AF corrections after review 11 had. The plan's status block is AB1's own surface, stale
# a second time. Each must reach the newest family the disposition history records.
if ($latestDispositionFamily) {
    $planStatus = Get-DispositionSection 'Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md'
    if ($planStatus -and (Get-FlowedText $planStatus) -cnotmatch "\b$($latestDispositionFamily[0])[0-9]") {
        $failures.Add("The disposition index's section for the redesign plan does not reach the '$($latestDispositionFamily[0])' family. This is AB1's own surface, stale twice before W3 moved it out of the plan; moving it did not make it exempt. This is AH3, now asked of the record that owns the history.")
    }
    # AQ4. The pattern is a prose sentence and was matched against the raw file, so the moment the
    # sentence reflowed -- it now wraps between "No independent" and "review has yet seen" -- the
    # match stopped happening and the check went silent. This file has carried `Get-FlowedText` for
    # exactly this since the first prose assertion; the check that most needed it was the one written
    # without it, and it is the guard against an *affirmative* false claim about review coverage
    # rather than a merely stale one.
    if ((Get-FlowedText $futureIndexText) -match 'No independent review has yet seen the ([A-Z]{1,2}) corrections') {
        $seenClaim = $Matches[1]
        if ($seenClaim -cne $latestDispositionFamily[0]) {
            $failures.Add("The future-work index states that no independent review has seen the '$seenClaim' corrections, and a later review has. An affirmative false claim about review coverage is worse than a stale one, because it tells a reader the newest work is unreviewed when it has been reviewed and corrected. This is AH3.")
        }
    }
}

# AJ2: the entry-point narratives have now gone stale for eight consecutive cycles, and every
# correction has been a token substitution -- a count, a family letter in one sentence -- because
# every check reads a token. The AA2 check asks only that a family appear *somewhere* in the file,
# which a table row satisfies; the AH3 check reads one sentence and the plan's status block; the AF2
# check reads the Channel index's range sentence. Nothing read a narrative, so the future-work index
# ran "ninth review -> AE, tenth review -> AF, all eight are corrected" and then jumped to a family
# three reviews later that its prose never introduces.
#
# The requirement is derived from the declared provenance table rather than from any list written
# here: every family that table attributes to a numbered independent closure review must be named in
# each narrative, and the review that raised it must be introduced there by its ordinal. A narrative
# that cannot say "the thirteenth review" has not been rewritten, whatever tokens it carries.
$numberedReviewOrdinals = @{ 7 = 'seventh'; 8 = 'eighth'; 9 = 'ninth'; 10 = 'tenth'; 11 = 'eleventh'; 12 = 'twelfth'; 13 = 'thirteenth'; 14 = 'fourteenth'; 15 = 'fifteenth'; 16 = 'sixteenth'; 17 = 'seventeenth'; 18 = 'eighteenth'; 19 = 'nineteenth'; 20 = 'twentieth' }
$channelNarrative = ($channelReadme -split '\| Artifact \| Purpose \| Current state \|', 2) | Select-Object -First 1
$futureChannelNarrative = ($futureIndexText -split '## Priority 1 . Channel 0.2 redesign and migration', 2)[1] -split '(?m)^## ', 2 | Select-Object -First 1
$planNarrative = Get-DispositionSection 'Brontide-Channel-0.2-Redesign-and-Migration-Plan-0.1.md'
$reviewNarratives = @(
    @{ Name = 'the Channel index narrative'; Text = $channelNarrative },
    @{ Name = "the future-work index's Priority 1 narrative"; Text = $futureChannelNarrative },
    @{ Name = "the disposition index's section for the redesign plan"; Text = $planNarrative }
)
# AQ1. The row pattern above read the third cell as the Record, and on 2026-08-20 the owner ruling
# that made each family declare what it was raised *against* inserted `Raised against` as the third
# column. Every row then yielded ` design ` where a record name had been, no row matched
# `closure review <n>`, every one took the `continue` below, and this check -- the whole of it, over
# three narratives and every closure-review family -- measured nothing for three iteration passes.
# That is AP1's class exactly, and this is the second key W-scale work has expired: correct when
# written, silently vacuous once the artifact it keys on moved.
#
# So the key is no longer the table's shape. The numbered attestations the reviews directory HOLDS
# are the set the narratives owe, they exist independently of any prose, and a family is looked up
# for each of them. A column inserted, a header renamed, or a row's wording changed now fails here
# instead of emptying the loop -- the AP1 correction pattern, an absent claim made loud rather than
# silencing.
$provenanceCells = @{}
foreach ($provenanceRow in [regex]::Matches($reviewReadme, '(?m)^\| ([A-Z]{1,2}) \|(.+)$')) {
    $provenanceCells[$provenanceRow.Groups[1].Value] = $provenanceRow.Groups[2].Value
}
$narrativeReviewNumbers = @($reviewMarkdown.Name |
    ForEach-Object { [regex]::Match($_, '^channel-0\.2-design-foundation-closure-review-([0-9]+)-attestation\.md$') } |
    Where-Object { $_.Success } |
    ForEach-Object { [int]$_.Groups[1].Value } |
    Sort-Object)
foreach ($reviewNumber in $narrativeReviewNumbers) {
    $provenanceFamily = @($provenanceCells.Keys | Where-Object { $provenanceCells[$_] -match "closure review $reviewNumber\b" })
    if ($provenanceFamily.Count -ne 1) {
        $failures.Add("The finding-family provenance table attributes closure review $reviewNumber to $($provenanceFamily.Count) families, and the reviews directory retains that review's attestation. Exactly one family row must name it, or the narratives owe a family this check cannot name. This is AQ1: the table gained a column on 2026-08-20 and the row pattern that read it went silent rather than loud.")
        continue
    }
    $provenanceFamily = $provenanceFamily[0]
    if (-not $numberedReviewOrdinals.ContainsKey($reviewNumber)) {
        $failures.Add("The reviews directory retains closure review $reviewNumber and this check has no ordinal word for that number. Extend the map rather than leaving the narratives unchecked.")
        continue
    }
    $reviewOrdinal = $numberedReviewOrdinals[$reviewNumber]
    foreach ($reviewNarrative in $reviewNarratives) {
        if (-not $reviewNarrative.Text) {
            $failures.Add("$($reviewNarrative.Name) could not be located, so the narrative check would pass over it by seeing nothing.")
            continue
        }
        if ($reviewNarrative.Text -cnotmatch "\b$provenanceFamily[0-9]") {
            $failures.Add("$($reviewNarrative.Name) names no finding in the '$provenanceFamily' family, which the provenance table attributes to closure review $reviewNumber. A family reachable only from a table row or a status sentence is a family the prose never introduced, and this is the narrative half of AI2 -- AJ2.")
        }
        # AX3. The key was the bare ordinal word anywhere in the narrative, which was enough while
        # ordinals in these documents only ever numbered closure reviews. They no longer do: the
        # condition-4 passes are numbered too, and the sentence naming the next one -- "a thirteenth
        # pass is the next work" -- satisfies `\bthirteenth\b` on its own. So the guard kept passing
        # while the narrative had dropped the review it is about, and the AQ1-a probe is what noticed.
        # It is the third time a bare-word key has been wider than its question, after AU3 and AV3.
        # Requiring the ordinal to sit immediately before the word `review` was tried and is wrong:
        # these narratives legitimately write "the eighth **U1**-**U8**" and "the eleventh raised",
        # naming the review by its findings. What actually distinguishes the two populations is the
        # other one's noun, so an ordinal that introduces a *pass* does not count as introducing a
        # review, and every existing phrasing still does.
        if ($reviewNarrative.Text -notmatch "(?i)\b$reviewOrdinal\b(?!\s+pass\b)") {
            $failures.Add("$($reviewNarrative.Name) never introduces the $reviewOrdinal independent closure review, whose findings it is required to carry. Substituting the newest family token into a sentence about an earlier review leaves the reader with a narrative that jumps from the tenth review to a family raised by the thirteenth. This is AJ2.")
        }
    }
}

# AH1: AG2 added a session qualifier to the precedence relation and did not add a session to the
# operand it reads. Precedence is defined over declared stimulus steps, and a step named its
# committing endpoint and interaction identity only -- which is W5's defect inside the correction
# written to close AG2. Underneath it sat a question no artifact answered: may a vector carry more
# than one session? Three normative passages assumed it may while the vector format read singular.
# Bounded to the naming list itself: a wider window reached the sentence explaining why the step
# carries a session, which says "session" and passed after the field had been removed.
$stimulusStep = [regex]::Match($flowedBrief, 'ordered stimulus steps, each naming(.{0,110})')
if (-not $stimulusStep.Success) {
    $failures.Add('The vector format does not state what a declared stimulus step names, which is the operand every ordering relation reads.')
}
elseif ($stimulusStep.Groups[1].Value -notmatch 'session') {
    $failures.Add('A declared stimulus step names its committing endpoint and interaction identity and no session, while the precedence relation is scoped "within one session". The operator has a qualifier its operand cannot supply, which is W5 restored inside the AG2 correction. This is AH1.')
}
if ($flowedBrief -notmatch 'more than one session') {
    $failures.Add('The neutral brief does not state whether a Channel 0.2 vector may carry more than one session. Three normative passages defend against a two-session vector and the vector format reads singular; on one reading precedence is not evaluable, on the other AF8 and AG2 defend against a vector no author can write. This is AH1''s second half.')
}

# AH2: the fifth instance of the pattern, and the one the AG sweep could not reach -- it enumerated
# artifacts each finding's *evidence* cites, and AF5's evidence never cited the completeness review.
# The audit is the artifact Batch 2 authors `capability-properties.json` from.
$auditC4Row = @($completeness -split "`r?`n" | Where-Object { $_ -match '^\| C4 \|' })
if ($auditC4Row.Count -ne 1) {
    $failures.Add('The per-capability property audit does not carry exactly one C4 row.')
}
else {
    # The required-green cell, not the row: the mutation column already contains "acknowledgement"
    # (it names `C4-outcome-precedes-ack`), so a row-wide search reports that member present while
    # the cell that must list it does not.
    $auditC4Cells = @($auditC4Row[0].Trim('|') -split '\|')
    $auditC4Green = $auditC4Cells[$auditC4Cells.Count - 1]
    foreach ($auditMember in @('conforming commit-order delivery', 'acknowledgement')) {
        if ($auditC4Green.IndexOf($auditMember, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("The property audit's C4 required-green cell does not name '$auditMember', which AF5 added to the set in the contract and the brief. The audit is what Batch 2 authors the property file from, so a cell naming four of seven members encodes four. This is AH2.")
        }
    }
}

# AH5: U7's direction-scope disposition was made when C12 required only that a property be able to
# fail. AE3 added the converse, and under it the recorded disagreement is a named case where two
# properties may go red on a conforming realization -- while both required-green cells read `owed`,
# which reads as "not yet written" rather than "known to have a red case".
$directionScopeRow = @($completeness -split "`r?`n" | Where-Object { $_ -match 'direction scope of the in-flight bound' })
# The rule is that the row names AE3, not that it mentions required-green somewhere: the disjunction
# was satisfied by the sentence about the `owed` cells after the AE3 connection itself was removed.
if ($directionScopeRow.Count -eq 1 -and $directionScopeRow[0] -notmatch 'AE3') {
    $failures.Add('The direction-scope disposition does not record its relationship to AE3''s converse rule. Under that rule the disagreement it discloses is a named conforming-realization exposure for `C4-P1` and `I5`, and nothing connects the two. This is AH5.')
}

# AH6: both sentences citing the retention rule convert "not barred" into "must be admitted". The
# rule says a later request is admitted *on its own merits*; a reordering whose displaced request is
# refused on its merits therefore leaves the first conjunct green. AG2's class with the cited text one
# screen below the citation.
foreach ($retentionCiter in @(@{ Name = 'capability contract'; Text = $flowedContract }, @{ Name = 'neutral brief'; Text = $flowedBrief })) {
    if ($retentionCiter.Text -match 'admits an interaction for that identity, exactly as (the retention passage below says it must|C4''s retention rule requires)') {
        $failures.Add("The $($retentionCiter.Name) says the retention rule requires the later admission. It says the request is admitted on its own merits and that the earlier refusal does not bar it, which is a different claim: a reordering whose displaced request is refused on its merits leaves `C4-P2`'s first conjunct green. This is AH6.")
    }
}

# The frame-reference registry that stood here is deleted. It hardcoded, per reference, the list of
# surfaces publishing it, an exact-count assertion over that list, and a package-wide sweep for an
# abbreviated publication -- roughly 125 lines whose whole purpose was to notice that the
# hand-maintained copies of one fact had drifted apart. They drifted anyway, once per cycle for nine
# cycles: AI1, AJ1, AK1 and AL2 are one event four times, and each check written to catch it could
# only see the surfaces its author already knew about.
#
# The fact is now owned by `conformance/channel-0.2-facts.json` and RENDERED into every artifact that
# publishes it, inside a fence the artifact carries. `build/verify-channel-0.2-facts.ps1` verifies
# every fenced region against the declaration and sweeps for an unfenced publication. So there is no
# surface list to keep in step -- a fence is the registration and it lives in the artifact -- and no
# exact count to maintain, because a surface cannot exist without registering itself. That is W1 of
# the verification foundation plan, and its acceptance was precisely that this registry could be
# deleted rather than extended.
#
# What stays here is what reads the fact rather than publishing it: AK4's count claim and the operand
# enumeration's registration rows, each taking the field list and the reference set from the
# declaration. The AL2 record-keyed sweep left with the `unseen` refusal record, which is a declared
# fact of its own now; the note where it stood says where it went.
$frameFacts = $null
$frameFactsPath = Join-Path $repositoryRoot 'conformance\channel-0.2-facts.json'
if (-not (Test-Path -LiteralPath $frameFactsPath)) {
    $failures.Add('The owned-fact declaration conformance/channel-0.2-facts.json does not exist. The frame references are rendered from it into every artifact that publishes them, so without it nothing here knows what the field list is.')
}
else {
    try { $frameFacts = Get-Content -Raw -LiteralPath $frameFactsPath -Encoding UTF8 | ConvertFrom-Json }
    catch { $failures.Add("The owned-fact declaration is not valid JSON: $($_.Exception.Message)") }
}
$framePlain = { param($Text) ((Get-FlowedText $Text) -replace '\*\*', '') }
$frameFencePattern = '(?s)<!-- fact:([a-z0-9-]+) -->(.*?)<!-- /fact -->'
$frameReferences = @()
$framePublicationArtifacts = @()
if ($frameFacts) {
    # The frame-reference class only. The declaration also owns the `unseen` refusal record, which
    # NESTS the refused-frame reference inside itself, and the two checks below are about references:
    # the enumeration registers each reference as an operand, and the count claim is about the
    # artifacts publishing one on its own. A record surface is neither, and treating it as one would
    # demand an enumeration row for a fact whose four components already have four rows.
    $frameReferences = @($frameFacts.facts | Where-Object { [string]$_.class -eq 'frame-reference' } | ForEach-Object {
        @{ Key = ([string]$_.id -replace '-frame-reference$', '')
           Id = [string]$_.id
           Label = [string]$_.title
           FieldList = & $framePlain ([string]$_.rendering) } })
    $frameReferenceIds = @($frameReferences | ForEach-Object { $_.Id })
    # Which artifacts publish a reference is DISCOVERED from the fences rather than declared here.
    $framePublishing = [System.Collections.Generic.List[string]]::new()
    foreach ($frameArtifactName in $artifactNames) {
        $frameArtifactRaw = Read-RequiredText $frameArtifactName
        $publishesReference = @([regex]::Matches($frameArtifactRaw, $frameFencePattern) | Where-Object { $frameReferenceIds -contains $_.Groups[1].Value }).Count -gt 0
        if ($publishesReference -and -not $framePublishing.Contains($frameArtifactName)) {
            $framePublishing.Add($frameArtifactName)
        }
    }
    $framePublicationArtifacts = @($framePublishing)
}

# The AL2 record-keyed sweep that stood here is deleted. It watched the recipient `unseen` refusal
# record -- a fact stated in full by three surfaces, in part by two more, and hand-maintained at every
# one of them -- by noticing a passage that named the record's detailed reason together with its
# provenance and its effect certainty without publishing the refused-frame reference. That is a check
# over a fact this file does not own, which is the arrangement W1 exists to end: the record is now
# declared in `conformance/channel-0.2-facts.json`, rendered into all five of its surfaces, and
# the sweep lives beside the declaration in `build/verify-channel-0.2-facts.ps1`, keyed to the
# trigger and co-terms the declaration states rather than to a field list written out here. Its
# neighbour exemption went with the move: run against this file's parent commit -- where THIS sweep
# was green -- the moved one fires on the interaction machine's `unseen` row and both grid cells,
# each of which rendered the reference this sweep asked for and hand-wrote the record's other three
# contents beside it.

# AK4: the ledger's status block said the inventory states the reference "in the same form as the five
# other artifacts that publish it". Four other artifacts publish it, in five other lists, because the
# brief publishes it twice. The count of artifacts publishing this reference is the exact quantity AJ1
# turned on, so a claim about it is checked against the registry rather than read.
$numberWords = @{ 'one' = 1; 'two' = 2; 'three' = 3; 'four' = 4; 'five' = 5; 'six' = 6; 'seven' = 7; 'eight' = 8; 'nine' = 9; 'ten' = 10; 'eleven' = 11; 'twelve' = 12; 'thirteen' = 13; 'fourteen' = 14; 'fifteen' = 15; 'sixteen' = 16; 'seventeen' = 17; 'eighteen' = 18; 'nineteen' = 19; 'twenty' = 20; 'twenty-one' = 21; 'twenty-two' = 22; 'twenty-three' = 23; 'twenty-four' = 24; 'twenty-five' = 25; 'twenty-six' = 26; 'twenty-seven' = 27; 'twenty-eight' = 28; 'twenty-nine' = 29; 'thirty' = 30 }
#
# AQ2, and both halves of the key were wrong once W3 ran. The claim AK4 was raised against lived in
# the ledger's status block; W3 moved every status block's history verbatim into the disposition
# index, which is a review record and not one of the nine design artifacts this loop swept -- so the
# sentence went on stating the count with nothing reading it. Asking "where else is this stated"
# then found the same count a second time, in the index's row for the ledger, worded "the four other
# **publishing artifacts**" -- which the phrase above would not have matched even in the right file.
# The sweep is therefore over every file that may carry the claim, and keyed to the claim rather
# than to the one sentence that first carried it.
$frameCountSurfaces = @($artifactNames | ForEach-Object { @{ Name = $_; Text = (Read-RequiredText $_) } })
$frameCountSurfaces += @{ Name = 'reviews\channel-0.2-disposition-index.md'; Text = $dispositionIndex }
foreach ($frameCountSurface in $frameCountSurfaces) {
    $frameCountText = & $framePlain $frameCountSurface.Text
    foreach ($frameCountClaim in [regex]::Matches($frameCountText, '(?i)\b([a-z]+(?:-[a-z]+)?) other (?:artifacts that publish|publishing artifacts|artifacts publishing)')) {
        $claimedWord = $frameCountClaim.Groups[1].Value.ToLowerInvariant()
        $expectedOthers = $framePublicationArtifacts.Count - 1
        if (-not $numberWords.ContainsKey($claimedWord) -or $numberWords[$claimedWord] -ne $expectedOthers) {
            $failures.Add("'$($frameCountSurface.Name)' says '$claimedWord other' artifacts publish a frame reference, and $($framePublicationArtifacts.Count) artifacts publish one, so $expectedOthers others do. The brief publishes each reference twice, so the count of publishing surfaces and the count of publishing artifacts are different numbers and a reader who takes this one literally looks for an artifact that does not exist. This is AK4, swept under AQ2 over the record W3 moved the claim into and over both wordings it is stated in.")
        }
    }
}

# W3, and the check AI4 and AH3 collapse into. An artifact's status block is what a reader opening
# that artifact alone is told, and AI4 was six of eight of them stale by one to four families. The
# answer then was to check nine blocks for freshness; the answer now is that they carry nothing that
# can go stale. A status block states what the artifact is and what it awaits, in five lines or
# fewer, and resolves to the disposition index -- so this one check replaces the freshness check on
# nine blocks and the plan's separate one, and the history it used to police is checked once, in the
# record that owns it.
#
# Three halves, and each is load-bearing. The length bound is what actually retires the surface: a
# block that keeps its pointer and grows a paragraph beneath it is the status quo with a link added,
# which is how every previous correction to these blocks went. The pointer must RESOLVE, because a
# link to a section that does not exist is worse than the history it replaced -- the reader is told
# the record is elsewhere and finds nothing. And the section it resolves to must reach the newest
# family, which is AI4's own question asked once instead of nine times.
#
# The middle half was the one this file did not implement: it asked whether the index has a section
# for the artifact, found it by the artifact's own name, and never looked at the anchor. `AN1` is that
# gap, and `Assert-DispositionPointer` above resolves the pointer that is actually written.
# AM1, found by the W1-W3 iteration pass. The bound above was read to the first BLANK LINE, and the
# history it exists to keep out sat one blank line beneath it: at `5894aba` the session machine's AL1
# paragraph -- "This status block previously recorded that the AK pass had audited `S1`-`S6`..." --
# was line 12 of the artifact, outside the reader and inside the surface. A paragraph of disposition
# history appended below a five-line block passed the gate; the probe was run before this correction
# and was green. So the region is now bounded by the artifact's first section HEADING, and everything
# in it that is not the status block must be declared front matter.
#
# A permit list rather than a pattern for history, and the direction matters: a guard that recognises
# disposition history by the words it uses cannot see the instance that does not use them, which is
# AL1 and AL2 exactly. An unrecognised paragraph here fails, so a new kind of front matter is declared
# once and a paragraph of narrative is a gate failure on the commit that writes it.
$frontMatterLabels = @(
    'Designed for:',
    'Designed against:',
    'Predecessor evidence:',
    'Companion artifacts:',
    'Contract owner:',
    'Contract owners:',
    'Normative companions:',
    'Reviewed artifacts:',
    'Sources inventoried:'
)
foreach ($statusArtifactName in $artifactNames) {
    if ($statusArtifactName -eq 'README.md' -or $statusArtifactName -eq 'reviews\README.md') { continue }
    $statusText = Read-RequiredText $statusArtifactName
    # The status REGION is everything from `Status:` to the first section heading, and the block is
    # its first paragraph. Bounding the region this way also removes the reason AH3 existed as a
    # second check over the redesign plan, whose title heading sits above its status line.
    $statusRegionMatch = [regex]::Match($statusText, '(?ms)^((?:\*\*)?Status:.*?)(?=^#|\z)')
    if (-not $statusRegionMatch.Success) {
        $failures.Add("'$statusArtifactName' has no status block. Every first-batch artifact states what it is and what it awaits.")
        continue
    }
    $regionParagraphs = @($statusRegionMatch.Groups[1].Value.Trim() -split "`r?`n\s*`r?`n" | Where-Object { $_.Trim() })
    $statusBody = $regionParagraphs[0].Trim()
    $statusLines = @($statusBody -split "`r?`n" | Where-Object { $_.Trim() })
    if ($statusLines.Count -gt 5) {
        $failures.Add("'$statusArtifactName' has a status block of $($statusLines.Count) lines and the bound is five. Disposition history belongs in the disposition index: every correction that adds a sentence here adds it to what the next cold reviewer has to read, which is the plan's section 1.3 and is what W3 retires.")
    }
    # Everything else before the first heading. The block can no longer be kept at five lines by
    # moving the sixth line into a paragraph of its own, which is what AM1 was.
    $previousLabelExpectsList = $false
    for ($regionIndex = 1; $regionIndex -lt $regionParagraphs.Count; $regionIndex++) {
        $regionParagraph = $regionParagraphs[$regionIndex].Trim()
        $regionLines = @($regionParagraph -split "`r?`n" | Where-Object { $_.Trim() })
        $regionFirstLine = ($regionLines[0] -replace '\*\*', '').Trim()
        if (@($frontMatterLabels | Where-Object { $regionFirstLine.StartsWith($_, [System.StringComparison]::Ordinal) }).Count -gt 0) {
            $previousLabelExpectsList = $regionParagraph.TrimEnd().EndsWith(':')
            continue
        }
        # A list under a label that ends in a colon is that label's own content, not a new paragraph.
        # A wrapped bullet continues on an indented line, so the test is that the paragraph opens with
        # a bullet and every later line is either a bullet or indented under one -- not that every
        # line is a bullet, which the ledger's own wrapped entries are not.
        $regionIsList = $regionLines[0].Trim() -match '^[-*] ' -and @($regionLines | Where-Object { $_.Trim() -notmatch '^[-*] ' -and $_ -notmatch '^\s' }).Count -eq 0
        if ($previousLabelExpectsList -and $regionIsList) {
            $previousLabelExpectsList = $false
            continue
        }
        $failures.Add("'$statusArtifactName' carries a paragraph between its status block and its first section heading that is not declared front matter: '$($regionFirstLine.Substring(0, [Math]::Min(80, $regionFirstLine.Length)))'. That is where this artifact's disposition history sat before W3 moved it, one blank line below a block the length bound was measuring. Either it is front matter, in which case its label joins the declared list, or it is history, in which case it belongs in the disposition index. This is AM1.")
    }
    if ($statusBody.IndexOf($dispositionLinkPattern, [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("'$statusArtifactName' has a status block that does not link to its section of the disposition index. A block that carries no history and no pointer to it leaves a reader who opens this artifact alone unable to find out what was corrected in it.")
        continue
    }
    $statusSection = Get-DispositionSection $statusArtifactName
    if (-not $statusSection) {
        $failures.Add("'$statusArtifactName' points at the disposition index and the index has no section for it. The pointer is the whole of what the status block now says about disposition, so a pointer that resolves to nothing is a worse answer than the history it replaced.")
        continue
    }
    # AN1: the pointer the block actually carries, resolved. The check above asks whether the index
    # has a section for this artifact, which is not the same question and is why an anchor naming no
    # heading -- or naming a different artifact's section -- passed it.
    Assert-DispositionPointer -Surface "'$statusArtifactName''s status block" -Text $statusBody -ArtifactName $statusArtifactName
    if ($latestDispositionFamily -and (Get-FlowedText $statusSection) -cnotmatch "\b$($latestDispositionFamily[0])[0-9]") {
        $failures.Add("The disposition index's section for '$statusArtifactName' does not reach the '$($latestDispositionFamily[0])' family, while the Channel index claims corrections in it. This is AI4, asked once of the record that owns the history instead of nine times of the artifacts.")
    }
}

# AI7: the same lists AH1 made per-session left `profile` and the established-profile digest singular.
# Asserted on the field itself rather than by a lookahead over the rest of the bullet: the sentence
# explaining the change says "per-session" and satisfied the lookahead after the field was reverted.
if ($flowedBrief -notmatch 'established profile digest \*\*of each session') {
    $failures.Add('The parity profile compares one exact established profile digest while a vector may now carry more than one session, each with its own established profile. This is AI7, and it is the AI1 class in the field list beside it.')
}

# AJ4, first half: AI4 was two findings and the check written for it caught one. The countable half --
# does the block reach the newest family -- is checked above. The other half is a status block whose
# self-description states a rule in the form it had before a correction changed it, which the family
# token cannot see: the brief's block still said stimulus steps name their committing endpoint "so
# that relation has an operand" after AH1 had added the session that relation needs, in the same block
# that announces AI1's session.
#
# Written over the fact rather than over that sentence: every passage in the package that states what
# a declared stimulus step names must name both operands, so a status block, a narrative, or a second
# normative list cannot describe the step in its pre-AH1 form. AG2's class is exactly this one artifact
# away, and it has now been raised three times.
foreach ($statusArtifactName in $artifactNames) {
    $stimulusText = (Get-FlowedText (Read-RequiredText $statusArtifactName)) -replace '\*\*', ''
    # The window ends at the clause rather than after N characters. Sized to a paragraph this check
    # passed its own mutation test: reverting the operand left the sentence explaining why the operand
    # is there, and that sentence says "session". Six checks in this file have now been weakened the
    # same way, so the boundary is the punctuation that ends the enumeration.
    foreach ($stimulusMatch in [regex]::Matches($stimulusText, "stimulus steps?,?(?: each)? (?:names?|naming)([^.;$([char]0x2014)]{0,160})")) {
        $stimulusWindow = $stimulusMatch.Groups[1].Value
        $missingOperands = @(@('committing endpoint', 'session') | Where-Object { $stimulusWindow.IndexOf($_, [System.StringComparison]::Ordinal) -lt 0 })
        if ($missingOperands.Count -gt 0) {
            $failures.Add("'$statusArtifactName' states what a declared stimulus step names and omits its $($missingOperands -join ' and '). `C4-P2`'s precedence relation is defined over one endpoint's own frames for one identity within one session, so a step that names fewer operands than the relation reads leaves the operator without one -- which is W5, then AH1, and a description of the step in its pre-AH1 form is AJ4.")
        }
    }
}

# AJ4, second half: the completeness review's own status block said its disposition history "runs to
# the eighth cycle" while the history ran to the thirteenth -- U4's defect restored as a
# self-description, in the artifact that is the package's record of what has been fixed. A block
# satisfies the family check above by appending a sentence naming the newest family and keeps the
# stale count in the sentence beside it, which is what happened. A claim about how far the history
# runs is therefore compared against how many review cycles there are.
if ($attestationCount) {
    $ordinalWords = @{ 'first' = 1; 'second' = 2; 'third' = 3; 'fourth' = 4; 'fifth' = 5; 'sixth' = 6; 'seventh' = 7; 'eighth' = 8; 'ninth' = 9; 'tenth' = 10; 'eleventh' = 11; 'twelfth' = 12; 'thirteenth' = 13; 'fourteenth' = 14; 'fifteenth' = 15; 'sixteenth' = 16; 'seventeenth' = 17; 'eighteenth' = 18; 'nineteenth' = 19; 'twentieth' = 20 }
    #
    # AQ3. The claim was read out of the nine design artifacts' status blocks, and W3 emptied those
    # blocks -- the history, this sentence with it, went verbatim to the disposition index. From that
    # commit no status block has carried the phrase and this check has read nothing, while the claim
    # itself is live in the record that now owns it. AJ4's subject did not go away; the place it is
    # written did.
    #
    # The surfaces are the blocks *and* the moved status text, and the boundary between the two
    # things the index holds is what makes this checkable. A `Status:` paragraph is the document
    # speaking about itself in the present, which is the whole of what AJ4 is about -- "a status
    # block that understates its own document". The disposition paragraphs beneath it are history,
    # and history recites old counts on purpose: the index says in one breath that a block once said
    # the history ran to the eighth cycle, and in the next that it runs to the sixteenth. Sweeping
    # the file whole reads the recital as a claim. Sweeping the `Status:` paragraphs reads the claim
    # and leaves the recital alone, without asking this check to tell a past tense from a present one.
    $cycleClaimSurfaces = @($artifactNames | ForEach-Object {
        @{ Name = $_; Text = [regex]::Match((Get-StatusBlock (Read-RequiredText $_)), '(?ms)^(?:\*\*)?Status:(.+)').Groups[1].Value } })
    foreach ($movedStatus in [regex]::Matches($dispositionIndex, '(?ms)^Status:(.+?)(?=\r?\n\r?\n|\z)')) {
        $cycleClaimSurfaces += @{ Name = "reviews\channel-0.2-disposition-index.md (moved status text)"; Text = $movedStatus.Groups[1].Value }
    }
    foreach ($cycleClaimSurface in $cycleClaimSurfaces) {
        if (-not $cycleClaimSurface.Text) { continue }
        foreach ($cycleClaim in [regex]::Matches((Get-FlowedText $cycleClaimSurface.Text), '(?i)runs to the ([a-z]+) cycle')) {
            $claimedWord = $cycleClaim.Groups[1].Value.ToLowerInvariant()
            if (-not $ordinalWords.ContainsKey($claimedWord)) {
                $failures.Add("'$($cycleClaimSurface.Name)' says the disposition history runs to the '$claimedWord' cycle, which is not an ordinal this check can compare against the number of retained review cycles. State the cycle as an ordinal word so the claim is checkable.")
            }
            elseif ($ordinalWords[$claimedWord] -lt $attestationCount) {
                $failures.Add("'$($cycleClaimSurface.Name)' says the disposition history runs to the '$claimedWord' cycle and there are $attestationCount retained review cycles. A record that understates its own history is what a reader consulting it alone is told, and appending the newest family leaves the sentence beside it saying what it always said. This is AJ4, asked under AQ3 of the record W3 moved the history into.")
            }
        }
    }
}

# AJ3: AI7's evidence named two entries in two lists and the correction reached the parity profile,
# which the check above reads. The vector format's own entry still read "profile, and the initial
# session/interaction state of each session the vector carries" -- the comma leaves `profile` outside
# the distribution the same sentence just made plural, so a two-session vector declares one profile
# for two sessions that establish independently.
#
# The class is "a field listed alongside a per-session distribution is inside it", not the one bullet:
# every place the brief distributes over the sessions a vector carries is read, and a field separated
# from the distribution by a comma fails. A lookahead for the words "each session" cannot see this --
# they are present in the defective form, which is why AI7's own check passed on half its finding.
foreach ($perSessionMatch in [regex]::Matches($flowedBrief, 'profile(.{0,120}?)(?:\*\*)?(?:of )?each session the vector carries')) {
    if ($perSessionMatch.Groups[1].Value.IndexOf(',', [System.StringComparison]::Ordinal) -ge 0) {
        $failures.Add("The neutral brief lists the profile as an item separate from a per-session distribution -- 'profile$($perSessionMatch.Groups[1].Value)each session the vector carries' -- so the distribution covers the fields after the comma and not the profile. A vector may carry more than one session and each establishes its own profile, so the entry states one profile for two sessions. This is AJ3.")
    }
}

# AI8: the pin clause dates the target commit, and the date has been wrong since the AG commit. The
# X6 check reads the subject only, which is why two cycles passed over it.
# Skipped while the design artifacts have uncommitted edits, for the same reason the X6 subject check
# is: a pin cannot name a commit that does not exist yet, and a correction pass that rewrites the
# clause ahead of its own commit would otherwise be told the date is wrong every time. The subject
# check carried this guard and the date check added beside it did not, which made the gate unusable
# mid-pass on any day the correction crossed midnight -- the exact condition AI8 was raised for.
# AM5 reaches this check too, and it would have gone wrong a day later rather than immediately: the
# date came from `git log -1` over the design paths, which is the answer that differs between the merge
# view and the branch view. It agreed today only because both commits fall on 2026-08-20. The date is
# now the date of the PINNED commit, which is the commit the clause is dating.
if ($pinnedCommit -and $pinnedCommit[0] -and -not $pendingDesignEdits) {
    $latestDesignDate = (Invoke-Git @('log', '-1', '--format=%ad', '--date=short', $pinnedCommit[0]))
    if ($latestDesignDate -and (Get-FlowedText $reviewReadme) -notmatch "committed $latestDesignDate") {
        $failures.Add("The review policy's pin clause does not date the review target '$latestDesignDate', which is when the commit it names was made. The X6 check compares the subject and never the date, so a wrong date survives every correction that rewrites the sentence. This is AI8.")
    }
}

# AI9: S3's evidence named the plan's section 7.8, which still reported seven retained negative
# attestations. A retained finding was therefore open while every index said all findings were closed.
if ($plan -match 'Seven independent negative attestations') {
    $failures.Add('The redesign plan still reports seven retained negative attestations. S3''s own evidence named this passage, so a retained finding has been open while every entry point claimed the programme''s findings were all closed. This is AI9.')
}

# The package's properties, counted from the artifacts that state them rather than from any sentence
# that reports the number. The capability id pattern is deliberately open at the right-hand end so the
# negative probe's renamed `C12-P1` still counts: the probe removes the *claim* that C12 has a
# property, not the paragraph, and a count that moved with the probe would make the probe fail twice.
$capabilityPropertyIds = @([regex]::Matches($contract, '(?m)^\*\*Property (C[0-9]+-P[0-9]+)') | ForEach-Object { $_.Groups[1].Value })
$sessionPropertyIds = @([regex]::Matches($session, '(?m)^- \*\*(S[0-9]+)\.\*\*') | ForEach-Object { $_.Groups[1].Value })
$interactionPropertyIds = @([regex]::Matches($interaction, '(?m)^- \*\*(I[0-9]+)\.\*\*') | ForEach-Object { $_.Groups[1].Value })
$statedProperties = $capabilityPropertyIds.Count + $sessionPropertyIds.Count + $interactionPropertyIds.Count

# AK3: three surfaces reported the package as stating twenty-five properties, and the property the
# count dropped is `C4-P2` -- the one fifteen cycles of this programme have been about. All three
# numbers were counts of *audit rows* presented as counts of properties, because the audit's `C4` row
# carries two properties in one cell. Counted here from the property statements themselves, so a
# sentence reporting the number cannot disagree with the artifacts that state them.
foreach ($countArtifact in $artifactNames) {
    $countText = Get-FlowedText (Read-RequiredText $countArtifact)
    # Only a spelled number is compared. "the properties the package states" states no count and is
    # not a claim; failing it would push the next pass to delete the sentence rather than to fix a
    # number, which is the opposite of what AJ4 established about self-description.
    foreach ($packageClaim in [regex]::Matches($countText, '(?i)\b([a-z]+(?:-[a-z]+)?)(?: properties)? the package states')) {
        $claimedWord = $packageClaim.Groups[1].Value.ToLowerInvariant()
        if (-not $numberWords.ContainsKey($claimedWord)) { continue }
        if ($numberWords[$claimedWord] -ne $statedProperties) {
            $failures.Add("'$countArtifact' says the package states '$claimedWord' properties and it states $statedProperties -- $($capabilityPropertyIds.Count) capability-wide, $($sessionPropertyIds.Count) session, $($interactionPropertyIds.Count) interaction. The per-capability audit has twelve capability rows and its `C4` row carries two properties, so a count of rows read as a count of properties drops `C4-P2`. This is AK3.")
        }
    }
    foreach ($coverageClaim in [regex]::Matches($countText, '(?i)covers the ([a-z]+(?:-[a-z]+)?) capability-wide')) {
        $claimedWord = $coverageClaim.Groups[1].Value.ToLowerInvariant()
        if (-not $numberWords.ContainsKey($claimedWord)) { continue }
        if ($numberWords[$claimedWord] -ne $capabilityPropertyIds.Count) {
            $failures.Add("'$countArtifact' says the per-capability audit covers '$claimedWord' capability-wide properties and the contract states $($capabilityPropertyIds.Count). This is AK3.")
        }
    }
}

# AK3's other half: "eleven capabilities owe the required-green set" counts the cells that read `owed`
# outright and misses that the `C4` cell ends "`C4-P1`: **owed**", so twelve capabilities owe at least
# one set. Counted from the audit rows.
$auditSection = [regex]::Match($completeness, '(?ms)^## Per-capability property audit\r?\n(.+?)(?=^### |^## |\z)').Groups[1].Value
$auditRows = @([regex]::Matches($auditSection, '(?m)^\| (C[0-9]+) \|.*$') | ForEach-Object { $_.Value })
if ($auditRows.Count -lt 12) {
    $failures.Add("The per-capability property audit parses to $($auditRows.Count) capability rows and the contract states $($capabilityPropertyIds.Count) capability-wide properties across twelve capabilities. A row count this check cannot read is a count claim nothing compares against.")
}
else {
    $owingCapabilities = @($auditRows | Where-Object { $_ -cmatch 'owed' }).Count
    foreach ($owedArtifact in $artifactNames) {
        $owedText = Get-FlowedText (Read-RequiredText $owedArtifact)
        foreach ($owedClaim in [regex]::Matches($owedText, '(?i)\b([a-z]+(?:-[a-z]+)?) capabilities owe')) {
            $claimedWord = $owedClaim.Groups[1].Value.ToLowerInvariant()
            if (-not $numberWords.ContainsKey($claimedWord) -or $numberWords[$claimedWord] -ne $owingCapabilities) {
                $failures.Add("'$owedArtifact' says '$claimedWord capabilities owe' a required-green set and $owingCapabilities of the audit's capability rows carry an `owed` cell. `C4`'s cell states `C4-P2`'s set and ends '`C4-P1`: **owed**', so counting the cells that read `owed` outright understates the residual work by one capability. This is AK3.")
            }
        }
    }
}

# AK7 and AK8. AH1 settled that a vector **may carry more than one session**, and three families since
# have been one operand of `C4-P2` catching up with that decision: AH1 gave the declared stimulus step
# its session, AI1 and AJ1 gave the settling-frame reference its session across six surfaces, and AK1
# is the same question on the refusal record. The audit that closed AK1 asked it of every fact `C4-P1`
# and `C4-P2` read and then of every property in the package, and found the decision had never reached
# the property *statements*: `C4-P1` forbade an identity being "dispatched twice" and bounded "the
# number of nonterminal interactions" with no session named, `C1-P1` required "exactly one profile"
# per vector, and `I5` bounded concurrency against "the established finite bound". Each is red on a
# conforming two-session vector, which is AE1's defect reached through a quantifier instead of a
# clause.
#
# The trigger set is DECLARED in C12 rather than listed here, for AF6's reason: a class inferred from
# whichever members were visible when the check was written is not the class. A per-session fact added
# to that declaration is covered without editing this loop, and a fact removed from it stops being
# checked visibly rather than silently.
# The recognizer, not the class: any of these phrasings names the session a clause means. Kept
# generous on purpose, because a property that names the session in an unlisted way is correct and
# failing it would train the next pass to reword rather than to scope. It is defined here rather than
# beside its first use because the AL1 check below reads it too, and a qualifier that exists only
# inside the branch where a declaration was found would leave that check unable to run in exactly the
# case where the declaration is missing.
$sessionQualifier = '(?i)(?:per session|per-session|session-scoped|every accepted session|(?:with)?in (?:one|each|that|its own|its|any|the same) session|(?:of|for|in) (?:each|its own|that|one|any) session|each session the vector carries|(?:that|its own) session''?s)'
$sessionScopeBlock = [regex]::Match($contract,'(?ms)\*\*Facts a vector may hold more than one of\.\*\*(.+?)(?=^\*\*|^## |\z)').Groups[1].Value
$sessionScopedFacts = @([regex]::Matches($sessionScopeBlock, '(?m)^- `([^`]+)` ') | ForEach-Object { $_.Groups[1].Value })
if ($sessionScopedFacts.Count -lt 1) {
    $failures.Add('C12 declares no list of facts a vector may hold more than one of. AH1 made multi-session vectors legal, and the rule that a property naming a per-session fact names the session it means is unenforceable over a class nothing declares -- which is AF6''s finding applied to a rule instead of to a family.')
}
else {
    $allProperties = @()
    foreach ($propertyMatch in [regex]::Matches($contract, '(?ms)^\*\*Property (C[0-9]+-P[0-9]+)[^*]*\*\*(.+?)(?=\r?\n\r?\n)')) {
        $allProperties += @{ Id = $propertyMatch.Groups[1].Value; Where = 'capability contract'; Text = $propertyMatch.Groups[2].Value }
    }
    foreach ($propertyMatch in [regex]::Matches($session, '(?m)^- \*\*(S[0-9]+)\.\*\* (.+)$')) {
        $allProperties += @{ Id = $propertyMatch.Groups[1].Value; Where = 'session state machine'; Text = $propertyMatch.Groups[2].Value }
    }
    foreach ($propertyMatch in [regex]::Matches($interaction, '(?ms)^- \*\*(I[0-9]+)\.\*\* (.+?)(?=\r?\n- \*\*|\r?\n\r?\n)')) {
        $allProperties += @{ Id = $propertyMatch.Groups[1].Value; Where = 'interaction state machine'; Text = $propertyMatch.Groups[2].Value }
    }
    if ($allProperties.Count -ne $statedProperties) {
        $failures.Add("The session-scope check parses $($allProperties.Count) property statements and the package states $statedProperties. A property statement this loop cannot read is a property it cannot audit, which is how the audit came to cover twelve of the package's properties while claiming every one.")
    }
    foreach ($property in $allProperties) {
        $propertyText = Get-FlowedText $property.Text
        foreach ($fact in $sessionScopedFacts) {
            # Words joined inside one clause rather than as the exact phrase: `C1-P1` writes the
            # established profile as "exactly one profile is established", and a check that matched
            # only the declared word order would have passed the property AK8 was raised against.
            # AS5 moved the same fact's words apart without crossing a clause boundary and the numeric
            # gap stopped recognising it, so punctuation owns the extent instead of a character count.
            $factWords = @($fact -split '\s+' | ForEach-Object { [regex]::Escape(($_ -replace 's$', '')) })
            $factPatterns = @(($factWords -join '[^,.;]*?'))
            if ($factWords.Count -eq 2) { $factPatterns += (($factWords[1], $factWords[0]) -join '[^,.;]*?') }
            # The qualifier has to GOVERN each occurrence, not merely appear in the property. `C4-P1`
            # reads three per-session facts in three clauses, and the first form of this check --
            # "a qualifier somewhere in the property" -- passed the pre-AK7 wording with the session
            # named only in the last clause, which is the defect with two thirds of it left standing.
            # Governance is read as English does: a qualifier governs an occurrence if it appears
            # anywhere from the start of the property through the end of the clause the occurrence
            # sits in, so a leading "within each session that vector carries" covers every clause
            # after it and a trailing one covers only its own.
            foreach ($factPattern in $factPatterns) {
                foreach ($factMatch in [regex]::Matches($propertyText, $factPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
                    $clauseEnd = $propertyText.IndexOfAny([char[]](',', ';', '.'), $factMatch.Index + $factMatch.Length)
                    if ($clauseEnd -lt 0) { $clauseEnd = $propertyText.Length - 1 }
                    $factWindow = $propertyText.Substring(0, $clauseEnd + 1)
                    if ($factWindow -notmatch $sessionQualifier) {
                        $failures.Add("Property '$($property.Id)' in the $($property.Where) reads the per-session fact '$fact' in a clause that names no session. A vector may carry more than one session under AH1 and these facts belong to one session each, so the clause counts or compares across sessions and goes red on a conforming multi-session vector -- AE1's defect reached through the property's quantifier instead of through a clause. This is AK7 for `C4-P1`, `C4-P2` and `I5`, and AK8 for the properties outside C4 that share it.")
                    }
                }
            }
        }
    }
}

# AL1. The AK7 loop above is a recognizer: it matches a DECLARED fact's own words inside a property's
# text. That is the right shape for a fact a property names, and it is blind by construction to a
# property that reads a per-session fact without naming it. `S3` -- "no new interaction is admitted
# after the first drain transition" -- reads one session's own state through the transition that
# changed it, contains none of the declared facts' words, and is red on the two-session vector AK7 was
# raised for: the first drain transition is not a fact of a vector, so a legal admission in a second
# session violates it literally.
#
# This check is structural instead. Every property the session state machine states is a statement
# about one session -- that is what the machine is, and the artifact's boundary section says so -- so
# each of them must name the session it means. The class is total over that artifact by construction
# rather than inferred from the members that were visible when it was written, which is AF6's
# distinction and the reason this is not another list of facts.
#
# What it does not cover is stated rather than implied: the interaction machine's `I1`-`I7` are
# statements about one interaction, which belongs to one session, and the same argument reaches them.
# They are left to the declared-fact recognizer above because `interaction identity` is a declared
# fact and their subject is that identity, so the two checks meet there. A later cycle that finds an
# `I` property reading a per-session fact it does not name has found a finding, not a gap in this
# comment.
$sessionMachineProperties = @([regex]::Matches($session, '(?m)^- \*\*(S[0-9]+)\.\*\* (.+)$'))
if ($sessionMachineProperties.Count -lt 1) {
    $failures.Add('The session state machine states no capability-wide properties this check can read. Its properties are the population AL1 is about, and a check that reads none of them reports nothing while auditing nothing -- which is the AI1 failure mode this file has now corrected twice.')
}
foreach ($sessionProperty in $sessionMachineProperties) {
    $sessionPropertyText = Get-FlowedText $sessionProperty.Groups[2].Value
    if ($sessionPropertyText -notmatch $sessionQualifier) {
        $failures.Add("Property '$($sessionProperty.Groups[1].Value)' in the session state machine names no session. Every property of that machine is a statement about one session's own state, a vector may carry more than one session under AH1, and a property that leaves the session unnamed is read across the vector: `S3` counted the first drain transition that way and went red on a vector conforming in both of its sessions. This is AL1, and it is AE1's defect reached through the quantifier -- the same class as AK7 and AK8, over the properties whose per-session fact is the machine's own subject rather than a fact they name.")
    }
}

# AL3. The declared list above is the AK7 recognizer's trigger set, and the AK pass derived it from
# the five properties that pass had found red. That is a class inferred from today's members, which is
# AF6 one level up, and the omission it left is the session's own state -- the fact `S3` reads.
#
# The list is therefore checked against another artifact rather than against itself: the neutral
# brief's vector format states what a vector distributes per session, and every fact it distributes
# has to be declared here. That is a derivation from the artifact that defines the vector, so a fact
# added to the vector format cannot stay outside the trigger set silently.
#
# It is still not a proof of totality, and claiming one here would be AD2's defect. A fact a property
# reads that the vector format does not enumerate is caught by reading, as AL1 was.
$vectorFormatSection = [regex]::Match($neutralBrief, '(?ms)^## Vector format\r?\n(.+?)(?=^## |\z)').Groups[1].Value
$vectorDistributionBullet = [regex]::Match((Get-FlowedText $vectorFormatSection), '- ([^;]{0,200}?) of \*\*each session the vector carries\*\*')
if (-not $vectorDistributionBullet.Success) {
    $failures.Add("The neutral brief's vector format states no per-session distribution this check can read, so C12's declared list of facts a vector may hold more than one of is checked against nothing. AH1 made multi-session vectors legal and the vector format is where what a vector holds per session is stated; a list checked only against itself is the derivation AL3 was raised against.")
}
else {
    $distributedFacts = @($vectorDistributionBullet.Groups[1].Value -replace '\*\*', '' -split '\s+and\s+' | ForEach-Object { ($_ -replace '^the\s+', '' -replace '^initial\s+', '').Trim().ToLowerInvariant() } | Where-Object { $_ })
    foreach ($distributedFact in $distributedFacts) {
        $isDeclared = $false
        foreach ($fact in $sessionScopedFacts) {
            $factWords = @($fact -split '\s+' | ForEach-Object { ($_ -replace 's$', '').ToLowerInvariant() })
            if (@($factWords | Where-Object { $distributedFact.IndexOf($_, [System.StringComparison]::Ordinal) -ge 0 }).Count -eq $factWords.Count) { $isDeclared = $true }
        }
        if (-not $isDeclared) {
            $failures.Add("The neutral brief's vector format distributes '$distributedFact' per session and C12 declares no matching fact in its list of facts a vector may hold more than one of. That list is the trigger set of the session-scope check above, so a fact the vector format holds per session and the declaration omits is a fact no property can be audited against -- which is how `S3` read one session's own state across the vector through fifteen review cycles and one complete property audit. This is AL3.")
        }
    }
}

# The durable half of the AK1 audit. Four families in a row -- W5, AH1, AI1/AJ1, and AK1 -- were one
# shape: an operator qualifier whose operand the record it reads does not publish. Each was found by
# sampling one operand, so the AK pass enumerated `C4-P1` and `C4-P2` completely and recorded the
# enumeration in the completeness review as a table the next cycle can check rather than rediscover.
#
# What this check can and cannot do is worth stating plainly. It cannot prove the table is total over
# the properties' operands -- that is a reading, and claiming otherwise here would be AD2's defect.
# What it does pin is that the table exists, that every declared class of operand appears in it, that
# no row is left recording an unpublished operand, and that every row names a surface that exists. A
# fifth frame reference or a fifth per-session fact therefore cannot be declared without the table
# growing to match.
$operandSection = [regex]::Match($completeness, '(?ms)^## `C4-P1` and `C4-P2` operand enumeration\r?\n(.+?)(?=^## |\z)').Groups[1].Value
if (-not $operandSection) {
    $failures.Add('The completeness review carries no `C4-P1`/`C4-P2` operand enumeration. Four consecutive families were one shape -- an operator qualifier whose operand is not published by the record it reads -- and each was found by sampling one operand; the enumeration is what lets the next cycle check the rest instead of sampling again.')
}
else {
    $operandRows = @([regex]::Matches($operandSection, '(?m)^\| (?!Operand \|)(?!-)(.+?) \| (.+?) \| (.+?) \| (.+?) \| (.+?) \|\s*$'))
    if ($operandRows.Count -lt 1) {
        $failures.Add('The `C4-P1`/`C4-P2` operand enumeration parses to no rows. A table this check cannot read is a table nothing compares against, which is the shape of every check in this file that certified its own scope.')
    }
    $operandSurfaceTokens = @('capability contract', 'C4', 'C10', 'C12', 'session state machine', 'interaction machine', 'state/event grid', 'responsibility matrix', 'neutral brief', 'migration ledger', 'redesign plan')
    foreach ($operandRow in $operandRows) {
        $operandName = $operandRow.Groups[1].Value.Trim()
        $operandSurfaces = $operandRow.Groups[4].Value
        $operandVerdict = ($operandRow.Groups[5].Value -replace '[*` ]', '').ToLowerInvariant()
        if ($operandVerdict -ne 'sufficient' -and $operandVerdict -ne 'insufficient') {
            $failures.Add("The operand-enumeration row '$operandName' records sufficiency as '$operandVerdict', which is outside the closed vocabulary `sufficient`/`insufficient`. A verdict outside a closed set is what the migration ledger's B4 finding was, and it makes the row uncountable.")
        }
        elseif ($operandVerdict -eq 'insufficient') {
            $failures.Add("The operand-enumeration row '$operandName' records an operand the design does not publish at the scope its clause claims. That is AK1's defect standing open and recorded rather than corrected: a property whose operand is unpublished is red or green on facts no observation carries.")
        }
        if (($operandSurfaceTokens | Where-Object { $operandSurfaces.IndexOf($_, [System.StringComparison]::Ordinal) -ge 0 }).Count -lt 1) {
            $failures.Add("The operand-enumeration row '$operandName' names no publishing surface this check can resolve to a design artifact. An operand whose surfaces are described rather than named is what AD1 acted on: a description consulted instead of the document.")
        }
    }
    # The registered marker inside a row's own operand column, not the key anywhere in the section.
    # The keys are bare words -- `settling`, `terminal`, `refused` -- and each occurs in several
    # neighbouring rows, so the section-wide form could not fail: deleting the terminal-frame row
    # outright left it green. That is the shape AI1's `Count -lt 3` guard had, in the check written to
    # end that shape, and it was found by mutation-testing rather than by reading.
    $operandNames = @($operandRows | ForEach-Object { $_.Groups[1].Value })
    foreach ($reference in $frameReferences) {
        $registeredMarker = "registered as ``$($reference.Key)``"
        if (-not @($operandNames | Where-Object { $_.IndexOf($registeredMarker, [System.StringComparison]::Ordinal) -ge 0 })) {
            $failures.Add("The `C4-P1`/`C4-P2` operand enumeration has no row registering the $($reference.Label) as '$($reference.Key)', which is a declared frame reference one of the two properties reads. A declared operand class missing from the enumeration is the sampling the enumeration exists to end.")
        }
    }
    foreach ($fact in $sessionScopedFacts) {
        if ($operandSection.IndexOf($fact, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("The `C4-P1`/`C4-P2` operand enumeration has no row naming the per-session fact '$fact', which C12 declares and `C4-P1` or `C4-P2` reads.")
        }
    }
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
