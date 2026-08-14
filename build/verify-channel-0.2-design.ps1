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
if ($flowedContract.IndexOf('is the mutation this property must go red on', [System.StringComparison]::Ordinal) -ge 0) {
    if ($flowedContract.IndexOf('stated over the refusal that reordering produces', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 claims `C4-control-precedes-request` is the mutation it must go red on, but it is not stated over the refusal that reordering produces. Quantified over the frames a recipient accepts, the property is green on that mutation: the reordered control is refused at `unseen` and the request is then latched, so the accepted sequence is empty and trivially an order-preserving subsequence.')
    }
    if ($flowedContract.IndexOf('for a cancellation control whose committing endpoint had already committed the request', [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add('C4-P2 does not forbid the observation `C4-control-precedes-request` actually produces: a recipient `rejected-protocol` at `unseen` for a cancellation control whose committing endpoint had already committed the request naming that identity. Without that conjunct the mutation leaves no witness the property can quantify over.')
    }
    if ($flowedContract.IndexOf('before that endpoint''s own frame that made the interaction terminal', [System.StringComparison]::Ordinal) -lt 0) {
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
if ($flowedBrief.IndexOf('intra-interaction frame order and its ordering mutation', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add('The neutral brief lists no adversarial vector group owning intra-interaction frame order, so `C4-control-precedes-request` has no home among the required groups.')
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
$designArtifactPathspec = @(
    'docs/future/channel/Brontide-Channel-0.2-Capability-Contract-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-Session-State-Machine-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-Interaction-State-Machine-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-State-Event-Coverage-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-Responsibility-Matrix-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md',
    'docs/future/channel/Brontide-Channel-0.1-to-0.2-Migration-Ledger-0.1.md',
    'docs/future/channel/Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md'
)
if (Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')) {
    $pendingDesignEdits = & git -C $repositoryRoot status --porcelain -- $designArtifactPathspec 2>$null
    $latestDesignSubject = (& git -C $repositoryRoot log -1 --format=%s -- $designArtifactPathspec 2>$null)
    if ($LASTEXITCODE -eq 0 -and $latestDesignSubject -and -not $pendingDesignEdits) {
        $flowedReviewReadme = Get-FlowedText $reviewReadme
        if ($flowedReviewReadme.IndexOf('The current review target is the commit titled', [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add('The review policy names no current review target, so a fresh reviewer has nothing to pin its attestation to.')
        }
        elseif ($flowedReviewReadme.IndexOf("The current review target is the commit titled ``$latestDesignSubject``", [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("The review policy's current review target is not the most recent commit to change a design artifact, which is '$latestDesignSubject'. This is U6: the clause is written in the commit before the one that supersedes it, and the reviewer is sent at artifacts that have already moved.")
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
$expectedReviewNames = @('README.md', 'channel-0.2-design-foundation-attestation.md', 'channel-0.2-design-foundation-closure-attestation.md', 'channel-0.2-design-foundation-final-closure-attestation.md', 'channel-0.2-design-foundation-definitive-closure-attestation.md', 'channel-0.2-design-foundation-totality-closure-attestation.md', 'channel-0.2-design-foundation-closure-re-review-attestation.md', 'channel-0.2-design-foundation-closure-review-7-attestation.md', 'channel-0.2-design-foundation-closure-review-8-attestation.md', 'channel-0.2-design-foundation-closure-review-9-attestation.md', 'channel-0.2-u1-correction-iteration-review.md', 'channel-0.2-w-correction-iteration-review.md', 'channel-0.2-ac-correction-iteration-review.md', 'channel-0.2-ad-correction-iteration-review.md')
$actualReviewNames = @($reviewMarkdown.Name | Sort-Object)
if (($actualReviewNames -join ',') -cne (($expectedReviewNames | Sort-Object) -join ',')) {
    $failures.Add('The Channel 0.2 design foundation must retain exactly the review README, all nine negative attestations, and all four correction iteration reviews before the next closure review.')
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
if (-not $dispositionHistory) {
    $failures.Add('The completeness review carries no review-disposition history, which is where each cycle''s findings are recorded.')
}
else {
    $iterationReviewFiles = @($reviewMarkdown | Where-Object { $_.Name -match 'iteration-review\.md$' })
    foreach ($reviewFile in $iterationReviewFiles) {
        $iterationRaw = Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8
        # The family pattern is `[A-Z]{1,2}` rather than `[A-Z]`: the AA and AB families already
        # existed when this check was written for X7 and it could not see either of them, so a
        # two-letter family's findings could be raised in a retained iteration review and never
        # reach the disposition history. A check written over a class has to match the whole class.
        $findingMatches = @([regex]::Matches($iterationRaw, '(?m)^### ([A-Z]{1,2}[0-9]+) '))
        foreach ($findingMatch in $findingMatches) {
            $findingId = $findingMatch.Groups[1].Value
            if ($dispositionHistory.IndexOf($findingId, [System.StringComparison]::Ordinal) -lt 0) {
                $failures.Add("The completeness review's disposition history does not record '$findingId', which '$($reviewFile.Name)' raises. A finding whose only record is an iteration review has no disposition in the artifact the next reviewer reads.")
            }
        }
        # The loop above fails when a finding is missing from the history and is silent when it
        # parses no findings at all, so narrowing the pattern disables it without failing anything --
        # which is how it ran blind past the AA and AB families. A retained iteration review exists to
        # record findings, so parsing none from one is the defect, and this is the assertion that
        # makes the pattern itself falsifiable.
        if ($findingMatches.Count -lt 1) {
            $failures.Add("No finding heading could be parsed from '$($reviewFile.Name)'. Either the review records no finding, which is not what a retained iteration review is for, or the heading pattern no longer matches its finding ids and the disposition check above is passing by seeing nothing.")
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
        foreach ($required in @("$attestationCount negative attestations", "$iterationCount iteration reviews")) {
            if ($indexReviewRow[0].IndexOf($required, [System.StringComparison]::Ordinal) -lt 0) {
                $failures.Add("The Channel index's Design reviews row does not say '$required', which is what the reviews directory actually holds. A count in prose is a claim that goes stale the next time a review is retained.")
            }
        }
    }
    $dispositionFamilies = @([regex]::Matches($dispositionHistory, '\*\*([A-Z]{1,2})[0-9]+\*\*') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
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
        if ($plan -cnotmatch "\b$family[0-9]") {
            $failures.Add("The redesign plan names no finding in the '$family' family, although the completeness review's disposition history records one. The plan is the entry point the future-work index sends a reader to first, and it is the one status block the cycle-name check never covered.")
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
    # so the window before each occurrence is what carries the families being denied.
    #
    # This reads assertion and quotation alike: a later pass retracting such a claim must not restate
    # it verbatim beside the families it names, which is a constraint on how a retraction is worded
    # rather than a defect in it. Detecting the difference would mean parsing negation, and a check
    # that guesses at that would fail open on the assertions this exists to catch.
    $recordedFamilies = @($recordedFamilies | Sort-Object -Unique)
    foreach ($reviewFile in $iterationReviewFiles) {
        $flowedIteration = Get-FlowedText (Get-Content -Raw -LiteralPath $reviewFile.FullName -Encoding UTF8)
        foreach ($denial in @([regex]::Matches($flowedIteration, 'no retained iteration review'))) {
            $windowStart = [Math]::Max(0, $denial.Index - 160)
            $window = $flowedIteration.Substring($windowStart, $denial.Index - $windowStart)
            foreach ($family in $recordedFamilies) {
                if ($window -cmatch "\b$family\b") {
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
    $iterationAttributions = @([regex]::Matches($flowedReviewPolicy, 'Correct ([^~]{1,60}?),? found by a[n]? [a-z]+ iteration pass'))
    $iterationFamilies = @(
        foreach ($attribution in $iterationAttributions) {
            [regex]::Matches($attribution.Groups[1].Value, '[A-Z]{1,2}[0-9]+') | ForEach-Object { $_.Value }
        }
    ) | Sort-Object -Unique
    if ($iterationFamilies.Count -lt 1) {
        $failures.Add('No finding id could be parsed from the review policy''s iteration-pass attributions. Either the policy attributes nothing to an author-side pass, or the attribution wording changed and this check is passing by seeing nothing -- which is the failure mode AD2 was.')
    }
    foreach ($findingFamily in $iterationFamilies) {
        if (-not ($iterationRecords | Where-Object { $_.IndexOf($findingFamily, [System.StringComparison]::Ordinal) -ge 0 })) {
            $failures.Add("The review policy attributes '$findingFamily' to an author-side iteration pass, and no retained iteration review records it. The two-kinds-of-review section requires that pass to be retained as evidence; a commit message is not the evidence trail.")
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
    $requiredGreen = [regex]::Match($c4p2Text, '\*\*Required green\.\*\*(.{0,700})')
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
