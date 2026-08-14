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

# Prose assertions must survive line wrapping: a normative sentence that reflows across a wrap is
# still the same sentence. Table-row assertions keep asserting against raw text, where the exact
# single-line form is the thing being pinned.
function Get-FlowedText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)

    return [regex]::Replace($Content, '\s+', ' ')
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
if ($flowedContract.IndexOf('is the mutation this property must go red on', [System.StringComparison]::Ordinal) -ge 0) {
    if ($flowedContract.IndexOf('stated over the refusal that reordering produces', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 claims `C4-control-precedes-request` is the mutation it must go red on, but it is not stated over the refusal that reordering produces. Quantified over the frames a recipient accepts, the property is green on that mutation: the reordered control is refused at `unseen` and the request is then latched, so the accepted sequence is empty and trivially an order-preserving subsequence.')
    }
    if ($flowedContract.IndexOf('for a cancellation control whose request the same endpoint had already committed', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 does not forbid the observation `C4-control-precedes-request` actually produces: a recipient `rejected-protocol` at `unseen` for a cancellation control whose request the sending endpoint had already committed. Without that conjunct the mutation leaves no witness the property can quantify over.')
    }
    if ($flowedContract.IndexOf('the same endpoint committed before the frame that made the interaction terminal', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 covers only the initiator-to-recipient direction. Reordering the recipient''s acknowledgement and terminal produces a late-traffic `state-violation` instead, and the conjunct must be restricted to frames one endpoint committed, or a legal late control after a peer terminal would falsely fail it.')
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

    # V2: the mutation must be executable. The neutral provider is authorised to inject faults and
    # loss; `C4-control-precedes-request` needs deterministic reordering, which no artifact permits.
    # A property whose named mutation cannot be run is unfalsifiable in practice, which is U1 again
    # one layer down at the evidence boundary.
    if ($flowedBrief.IndexOf('reordering injection', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('The neutral provider boundary authorises deterministic fault/loss injection only, so no endpoint in the evidence set can produce `C4-control-precedes-request`. C4-P2 would carry a named mutation that nothing is permitted to execute.')
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
    '`channel-0.2-design-foundation-closure-record.md`',
    '`build/verify-channel-0.2-design.ps1`',
    '`build/verify-interchange.ps1`'
)

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
$expectedReviewNames = @('README.md', 'channel-0.2-design-foundation-attestation.md', 'channel-0.2-design-foundation-closure-attestation.md', 'channel-0.2-design-foundation-final-closure-attestation.md', 'channel-0.2-design-foundation-definitive-closure-attestation.md', 'channel-0.2-design-foundation-totality-closure-attestation.md', 'channel-0.2-design-foundation-closure-re-review-attestation.md', 'channel-0.2-design-foundation-closure-review-7-attestation.md', 'channel-0.2-design-foundation-closure-review-8-attestation.md', 'channel-0.2-u1-correction-iteration-review.md')
$actualReviewNames = @($reviewMarkdown.Name | Sort-Object)
if (($actualReviewNames -join ',') -cne (($expectedReviewNames | Sort-Object) -join ',')) {
    $failures.Add('The Channel 0.2 design foundation must retain exactly the review README, all eight negative attestations, and the U1 correction iteration review before the next closure review.')
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
