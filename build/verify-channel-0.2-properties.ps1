$ErrorActionPreference = 'Stop'

# Channel 0.2 executable capability-wide properties.
#
# This is W2 of the verification foundation plan. Twenty-six properties are stated in English across
# eleven artifacts, and until this file existed nothing executed one: the design verifier beside it is
# over two thousand lines of structure and string checking, which can say that a field list appears in
# every surface registered to carry it and cannot say whether the property those fields are operands of
# is true, can fail, or stays green on conforming behaviour. Every closure reviewer since the eighth
# wrote an evaluator from the published prose, used it to find something, and threw it away. This keeps
# one.
#
# Three rules govern what is written here, and each is a rule the design paid for.
#
#   * The artifacts are the authority. Every statement in `channel-0.2-properties.json` cites the
#     artifact that owns it, and the citation checks below fail when the two disagree. This file must
#     not become a twelfth surface publishing the same fact -- that is the failure W1 exists to retire,
#     and adding another copy of it here would be the AI1/AJ1/AK1/AL2 family arriving through the gate.
#   * An arrival ordinal is an identifier and never an ordering operand. It is read only inside
#     Resolve-FrameReference, to say which received frame a record names. `Test-Precedes` reads the
#     declared commit sequence and nothing else, so no code path can order by observed arrival.
#   * A reference that does not single out one declared step is resolved existentially, and the
#     property is red if any resolution makes it red. A vector author facing an ambiguous reference
#     still has to write one expected observation, so "the fields do not decide" means some author
#     reaches the wrong verdict. This is closure review 16's P3 rule, and it is what makes an operand
#     mutation observable: dropping a published field widens the candidate set rather than erroring.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$propertiesPath = Join-Path $repositoryRoot 'conformance\channel-0.2-properties.json'
$vectorsPath = Join-Path $repositoryRoot 'conformance\channel-0.2-property-vectors.json'
$channelPath = Join-Path $repositoryRoot 'docs\future\channel'
$contractPath = Join-Path $channelPath 'Brontide-Channel-0.2-Capability-Contract-0.1.md'
$briefPath = Join-Path $channelPath 'Brontide-Channel-0.2-Neutral-Contract-Brief-0.1.md'
$planPath = Join-Path $channelPath 'Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md'

$failures = [System.Collections.Generic.List[string]]::new()

foreach ($requiredPath in @($propertiesPath, $vectorsPath, $contractPath, $briefPath, $planPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        $failures.Add("Required path does not exist: '$requiredPath'.")
    }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

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

function Get-PlainText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)

    # Emphasis is stripped and whitespace is flowed before comparison, the same way the design
    # verifier compares a field list: the fields are the fact, the bolding and the line wrap are not.
    return [regex]::Replace(($Content -replace '\*\*', ''), '\s+', ' ')
}

function Get-Field {
    param($Object, [Parameter(Mandatory = $true)][string]$Path)

    $current = $Object
    foreach ($segment in $Path.Split('.')) {
        if ($null -eq $current) { return $null }
        $property = $current.PSObject.Properties[$segment]
        if ($null -eq $property) { return $null }
        $current = $property.Value
    }
    return $current
}

$properties = Read-JsonFile $propertiesPath
$vectorFile = Read-JsonFile $vectorsPath
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$contractPlain = Get-PlainText (Get-Content -Raw -LiteralPath $contractPath -Encoding UTF8)
$briefPlain = Get-PlainText (Get-Content -Raw -LiteralPath $briefPath -Encoding UTF8)
$planPlain = Get-PlainText (Get-Content -Raw -LiteralPath $planPath -Encoding UTF8)

if ($properties.schemaVersion -ne 1) { $failures.Add('channel-0.2-properties.json must use schemaVersion 1.') }
if ($vectorFile.schemaVersion -ne 1) { $failures.Add('channel-0.2-property-vectors.json must use schemaVersion 1.') }

$vectorsById = @{}
foreach ($vector in $vectorFile.vectors) {
    if ($vectorsById.ContainsKey($vector.id)) {
        $failures.Add("Vector id '$($vector.id)' is declared more than once. A property's expectation is keyed by vector id, so a duplicate id makes which vector was evaluated unanswerable.")
        continue
    }
    $vectorsById[$vector.id] = $vector
}

# ---------------------------------------------------------------------------------------------
# Vector structure.
#
# `declaredOrder` is the index of a step in the vector's declared ordered stimulus sequence, and it is
# the only ordering `Test-Precedes` reads. `commitIndex` is the step's position within its own
# endpoint's commit sequence for one session and one interaction identity, and it is checked against
# the declared order rather than trusted: a vector whose two orderings disagree would make every
# precedence verdict depend on which of them the evaluator happened to read, which is the class of
# defect a second surface for one fact always has.
# ---------------------------------------------------------------------------------------------

$vectorIndex = @{}
foreach ($vector in $vectorFile.vectors) {
    $stepsById = @{}
    $order = 0
    foreach ($step in $vector.declaredSteps) {
        foreach ($requiredStepField in @('id', 'kind', 'committingEndpoint', 'session', 'interactionIdentity', 'commitIndex')) {
            if ($null -eq (Get-Field $step $requiredStepField)) {
                $failures.Add("Vector '$($vector.id)' has a declared stimulus step missing '$requiredStepField'. Attribution is not bookkeeping: C4-P2's precedence relation is defined over one endpoint's own frames for one identity within one session, and without all three the operator has no operand.")
            }
        }
        if ($stepsById.ContainsKey([string]$step.id)) {
            $failures.Add("Vector '$($vector.id)' declares step id '$($step.id)' more than once.")
        }
        $stepsById[[string]$step.id] = [pscustomobject]@{
            Id = [string]$step.id
            Kind = [string]$step.kind
            CommittingEndpoint = [string]$step.committingEndpoint
            Session = [string]$step.session
            InteractionIdentity = [string]$step.interactionIdentity
            CommitIndex = [int]$step.commitIndex
            DeclaredOrder = $order
            ArrivalOrdinal = $null
            ReceivingEndpoint = $null
            Delivered = $false
        }
        $order++
    }

    foreach ($delivery in $vector.delivery) {
        $stepKey = [string]$delivery.step
        if (-not $stepsById.ContainsKey($stepKey)) {
            $failures.Add("Vector '$($vector.id)' declares delivery for step '$stepKey', which is not a declared stimulus step.")
            continue
        }
        $entry = $stepsById[$stepKey]
        if ([string]$delivery.disposition -eq 'delivered') {
            $entry.Delivered = $true
            $entry.ReceivingEndpoint = [string]$delivery.receivingEndpoint
            if ($null -eq $delivery.arrivalOrdinal) {
                $failures.Add("Vector '$($vector.id)' delivers step '$stepKey' without an arrival ordinal. The ordinal is what a frame reference names a received frame by, so a delivered step without one cannot be the operand of any record.")
            }
            else {
                $entry.ArrivalOrdinal = [int]$delivery.arrivalOrdinal
            }
        }
        elseif ([string]$delivery.disposition -ne 'lost') {
            $failures.Add("Vector '$($vector.id)' gives step '$stepKey' disposition '$($delivery.disposition)', which is outside the closed set delivered/lost.")
        }
    }

    foreach ($step in $vector.declaredSteps) {
        if (-not ($vector.delivery | Where-Object { [string]$_.step -eq [string]$step.id })) {
            $failures.Add("Vector '$($vector.id)' declares step '$($step.id)' and states no disposition for it. Loss is legal behaviour C4-P2 must stay green on, so whether a frame arrived is data the vector states rather than a default.")
        }
    }

    $steps = @($stepsById.Values | Sort-Object DeclaredOrder)
    foreach ($group in ($steps | Group-Object { "$($_.CommittingEndpoint)|$($_.Session)|$($_.InteractionIdentity)" })) {
        $expectedIndex = 1
        foreach ($groupStep in ($group.Group | Sort-Object DeclaredOrder)) {
            if ($groupStep.CommitIndex -ne $expectedIndex) {
                $failures.Add("Vector '$($vector.id)' step '$($groupStep.Id)' carries commitIndex $($groupStep.CommitIndex) and is $expectedIndex in the declared order for endpoint '$($groupStep.CommittingEndpoint)', session '$($groupStep.Session)', identity '$($groupStep.InteractionIdentity)'. The declared sequence and the commit indices are two statements of one fact and they disagree.")
            }
            $expectedIndex++
        }
    }

    $vectorIndex[[string]$vector.id] = $steps
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

# ---------------------------------------------------------------------------------------------
# The operators.
# ---------------------------------------------------------------------------------------------

function Resolve-FrameReference {
    # A frame reference resolves to every declared stimulus step matching the fields it PUBLISHES.
    # A field the reference does not carry narrows nothing -- that is the whole mechanism the operand
    # corrections AF8, AG2, AH1, AI1, AJ1, AK1, AK5 and AK6 each closed, and dropping a field here is
    # how the operand mutations reproduce those findings instead of asserting them.
    #
    # The arrival ordinal is matched for EQUALITY only, to say which received frame the record names.
    # It is never returned to a caller that orders by it: the only ordering in this file is
    # Test-Precedes, which reads DeclaredOrder.
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Steps,
        [Parameter(Mandatory = $true)]$Reference
    )

    $candidates = @($Steps)
    foreach ($pair in @(
            @{ Field = 'kind'; Property = 'Kind' },
            @{ Field = 'session'; Property = 'Session' },
            @{ Field = 'interactionIdentity'; Property = 'InteractionIdentity' },
            @{ Field = 'committingEndpoint'; Property = 'CommittingEndpoint' })) {
        $published = Get-Field $Reference $pair.Field
        if ($null -ne $published) {
            $candidates = @($candidates | Where-Object { $_.($pair.Property) -eq [string]$published })
        }
    }

    $publishedOrdinal = Get-Field $Reference 'arrivalOrdinal'
    if ($null -ne $publishedOrdinal) {
        $candidates = @($candidates | Where-Object { $null -ne $_.ArrivalOrdinal -and $_.ArrivalOrdinal -eq [int]$publishedOrdinal })
    }

    return $candidates
}

function Test-Precedes {
    # Precedence between two positions in the vector's declared ordered stimulus sequence -- data the
    # vector author wrote down. Never an observed time, an arrival order, or anything but the declared
    # sequence. The restriction to one endpoint, one identity, and one session is carried by the
    # operands: a reference that publishes those fields admits only steps that share them, and a
    # reference that has lost one admits steps that do not, which is exactly the false verdict the
    # corresponding correction was raised for.
    param([Parameter(Mandatory = $true)]$Earlier, [Parameter(Mandatory = $true)]$Later)

    return $Earlier.DeclaredOrder -lt $Later.DeclaredOrder
}

function Test-MemberOf {
    # The membership test the first conjunct reads: the refused identity against the set the recipient
    # admits WITHIN THE SAME SESSION (AF8). Where the record publishes no session the test is not
    # scoped and looks across the whole vector, which is the false red AK1 was raised for; where it
    # publishes no identity the test is existential over the scoped set.
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$AdmittedSets,
        $Session,
        $Identity
    )

    $sets = @($AdmittedSets)
    if ($null -ne $Session) {
        $sets = @($sets | Where-Object { [string]$_.session -eq [string]$Session })
    }

    $identities = @($sets | ForEach-Object { $_.identities } | Where-Object { $null -ne $_ } | ForEach-Object { [string]$_ })
    if ($null -eq $Identity) { return $identities.Count -gt 0 }
    return $identities -contains [string]$Identity
}

# ---------------------------------------------------------------------------------------------
# C4-P2, evaluated.
#
# The two conjuncts are the contract's, clause for clause. Each returns red on the first witness it
# finds and names it, because a red verdict whose witness is unnamed is a verdict a reader has to
# reproduce by hand -- which is the cost this file exists to remove.
# ---------------------------------------------------------------------------------------------

function Invoke-C4P2 {
    param(
        [Parameter(Mandatory = $true)][string]$VectorId,
        [Parameter(Mandatory = $true)]$Vector,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Steps
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $observations = $Vector.observations
    $admitted = @()
    if ($null -ne $observations.recipientAdmittedIdentities) { $admitted = @($observations.recipientAdmittedIdentities) }

    # Conjunct 1. No endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation
    # control whose committing endpoint had already committed the request naming that identity and
    # whose recipient afterwards admits an interaction for that identity in the same session.
    foreach ($refusal in @($observations.unseenRefusals)) {
        if ($null -eq $refusal) { continue }
        $selectors = @(
            @{ Path = 'provenance'; Value = 'recipient' },
            @{ Path = 'frameDecision'; Value = 'rejected-protocol' },
            @{ Path = 'detailedReason'; Value = 'unopened-interaction-identity' },
            @{ Path = 'refusedFrame.kind'; Value = 'cancellation-control' })
        $selected = $true
        foreach ($selector in $selectors) {
            $actual = Get-Field $refusal $selector.Path
            # An absent selector field narrows nothing, for the same reason an absent operand does.
            if ($null -ne $actual -and [string]$actual -ne $selector.Value) { $selected = $false }
        }
        if (-not $selected) { continue }

        $reference = Get-Field $refusal 'refusedFrame'
        if ($null -eq $reference) {
            # The whole reference, not one of its fields. A record carrying none of it is what
            # the `unseen` refusal was before AK1 and AK5, and the conjunct then has no operand
            # at all -- which is unevaluable rather than green, and must not read as a pass.
            $errors.Add("Vector '$VectorId' records an ``unseen`` refusal with no refused-frame reference. That reference is the first conjunct's operand, so the property cannot be evaluated over this record.")
            continue
        }
        $refusedSteps = Resolve-FrameReference -Steps $Steps -Reference $reference
        if ($refusedSteps.Count -eq 0) {
            $errors.Add("Vector '$VectorId' records an ``unseen`` refusal whose refused-frame reference matches no declared stimulus step. The record is the operand of the conjunct's precedence half, so a reference that names nothing leaves the property unevaluable rather than green.")
            continue
        }

        $referenceSession = Get-Field $reference 'session'
        $referenceIdentity = Get-Field $reference 'interactionIdentity'
        $referenceEndpoint = Get-Field $reference 'committingEndpoint'

        $requests = @($Steps | Where-Object { $_.Kind -eq 'request' })
        if ($null -ne $referenceSession) { $requests = @($requests | Where-Object { $_.Session -eq [string]$referenceSession }) }
        if ($null -ne $referenceIdentity) { $requests = @($requests | Where-Object { $_.InteractionIdentity -eq [string]$referenceIdentity }) }
        if ($null -ne $referenceEndpoint) { $requests = @($requests | Where-Object { $_.CommittingEndpoint -eq [string]$referenceEndpoint }) }

        $admits = Test-MemberOf -AdmittedSets $admitted -Session $referenceSession -Identity $referenceIdentity

        foreach ($refusedStep in $refusedSteps) {
            foreach ($request in $requests) {
                if ((Test-Precedes -Earlier $request -Later $refusedStep) -and $admits) {
                    return [pscustomobject]@{
                        Verdict = 'red'
                        Conjunct = 'C4-P2-conjunct-1'
                        Witness = "the request '$($request.Id)' was committed before the refused control '$($refusedStep.Id)' by endpoint '$($request.CommittingEndpoint)', and that identity is in the admitted set the session field of that record scopes the membership test to"
                        Errors = $errors
                    }
                }
            }
        }
    }

    # Conjunct 2. None records a late-traffic `state-violation` latched against a frame whose
    # committing endpoint had committed it before that endpoint's own frame the interaction's terminal
    # history was accepted on.
    foreach ($latch in @($observations.lateTrafficLatches)) {
        if ($null -eq $latch) { continue }
        $category = Get-Field $latch 'category'
        $latchValue = Get-Field $latch 'latchValue'
        if ($null -ne $category -and [string]$category -ne 'state-violation') { continue }
        if ($null -ne $latchValue -and [string]$latchValue -ne 'fault-committed') { continue }

        $settling = Get-Field $latch 'settlingFrame'
        $terminal = Get-Field $latch 'terminalFrame'
        # A settled latch is on a terminal interaction, so both operands exist. A record missing
        # either is the state AK6 found the design in -- the conjunct had one operand identified
        # to five fields and the other to nothing -- and the property is then unevaluable rather
        # than green. Skipping such a record would report exactly the vacuous pass AK6 named.
        if ($null -eq $settling -or $null -eq $terminal) {
            $errors.Add("Vector '$VectorId' records a settled late-traffic ``state-violation`` whose latch omits the settling-frame or the terminal-frame reference. Both are operands of the conjunct's precedence relation, so the property cannot be evaluated over this record.")
            continue
        }

        # "that endpoint's own frame": the conjunct compares two frames of ONE endpoint, for one
        # identity, within one session. Where either reference has lost the field the comparison is
        # not narrowed by it, which is what makes AK6's operand mutation observable.
        $sameFrameScope = $true
        foreach ($pair in @(
                @{ Field = 'committingEndpoint' }, @{ Field = 'session' }, @{ Field = 'interactionIdentity' })) {
            $left = Get-Field $settling $pair.Field
            $right = Get-Field $terminal $pair.Field
            if ($null -ne $left -and $null -ne $right -and [string]$left -ne [string]$right) { $sameFrameScope = $false }
        }
        if (-not $sameFrameScope) { continue }

        $settlingSteps = Resolve-FrameReference -Steps $Steps -Reference $settling
        $terminalSteps = Resolve-FrameReference -Steps $Steps -Reference $terminal
        if ($settlingSteps.Count -eq 0 -or $terminalSteps.Count -eq 0) {
            $errors.Add("Vector '$VectorId' records a settled late-traffic latch whose settling-frame or terminal-frame reference matches no declared stimulus step. Both are operands of the conjunct's precedence relation, so a reference that names nothing leaves the property unevaluable rather than green.")
            continue
        }

        foreach ($settlingStep in $settlingSteps) {
            foreach ($terminalStep in $terminalSteps) {
                if (Test-Precedes -Earlier $settlingStep -Later $terminalStep) {
                    return [pscustomobject]@{
                        Verdict = 'red'
                        Conjunct = 'C4-P2-conjunct-2'
                        Witness = "the latch settled against '$($settlingStep.Id)', which its committing endpoint committed before '$($terminalStep.Id)', the frame that endpoint's terminal history was accepted on"
                        Errors = $errors
                    }
                }
            }
        }
    }

    return [pscustomobject]@{ Verdict = 'green'; Conjunct = $null; Witness = $null; Errors = $errors }
}

# ---------------------------------------------------------------------------------------------
# The session and interaction properties.
#
# S1-S6 and I1-I7 read a vector's ordered session timeline -- transitions, admissions, dispatches and
# accepted terminal facts, each naming its session -- plus per-interaction facts. Every one of them is
# SESSION-SCOPED, and that is the whole of AK7 and AL1: a property that reads one session's fact
# across the vector is green on a single-session vector and red on two conforming sessions. Each
# evaluator therefore groups by session before it counts anything, and the two-session vector is a
# required-green member of all fourteen.
# ---------------------------------------------------------------------------------------------

# The legal session transition table, from the session state machine. S1 is the property that reads
# it, so it is written once here and pinned against the artifact by the citation check further down.
$legalSessionTransitions = @(
    'unestablished>established', 'unestablished>establishing', 'unestablished>closed',
    'establishing>established', 'establishing>closed',
    'established>draining', 'draining>faulted', 'draining>closed',
    # The machine's two `any nonterminal` rows -- a fatal recognized Channel violation and a
    # transport/process loss -- expanded over the nonterminal states. They were missing until AO1,
    # and `draining>faulted` was here only because a concrete row states that one as well, so `S1`
    # and `C2-P1` were red on a session faulting from any of the three states below. The
    # cross-check further down is what keeps this list and the artifact in step, in both directions.
    'unestablished>faulted', 'establishing>faulted', 'established>faulted')
$terminalSessionStates = @('closed', 'faulted')

function Get-Timeline { param($Vector) if ($null -eq $Vector.sessionTimeline) { return @() } return @($Vector.sessionTimeline) }
function Get-Interactions { param($Vector) if ($null -eq $Vector.interactions) { return @() } return @($Vector.interactions) }
# AR1. `-Conjunct` names WHICH clause of a multi-clause property went red. It is not new structure
# invented here: the check at the bottom of this file already requires a mutation declared against a
# conjunct to fire through that conjunct, and the reason it gave -- "a conjunct whose mutation fires
# through the other conjunct is unfalsifiable in the suite however well the contract names it" -- was
# enforced only for `C4-P2`, the one property that declared conjuncts. `C5-P1` and `C6-P1` each state
# two clauses in one sentence, each had one named mutation, and each mutation fired through the first
# clause. Naming the clauses is the mechanical decomposition that lets the existing rule reach them;
# the statement itself stays the contract's, verbatim and unrestated.
function New-Red { param([string]$Witness, [string]$Conjunct) return [pscustomobject]@{ Verdict = 'red'; Conjunct = $Conjunct; Witness = $Witness; Errors = [System.Collections.Generic.List[string]]::new() } }
function New-Green { return [pscustomobject]@{ Verdict = 'green'; Conjunct = $null; Witness = $null; Errors = [System.Collections.Generic.List[string]]::new() } }

function Invoke-S1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -ne 'transition' -or -not $sessionEvent.accepted) { continue }
        $edge = "$($sessionEvent.from)>$($sessionEvent.to)"
        if ($legalSessionTransitions -notcontains $edge) {
            return New-Red "session $($sessionEvent.session) accepted the transition $edge on event $($sessionEvent.event), which the legal table does not contain"
        }
    }
    return New-Green
}

function Invoke-S2 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $state = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string]$sessionEvent.session
        if ([string]$sessionEvent.step -eq 'transition' -and $sessionEvent.accepted) { $state[$sessionId] = [string]$sessionEvent.to; continue }
        if ([string]$sessionEvent.step -ne 'dispatch') { continue }
        $current = if ($state.ContainsKey($sessionId)) { $state[$sessionId] } else { 'unestablished' }
        if ($current -ne 'established') {
            return New-Red "interaction $($sessionEvent.identity) dispatched while its own session $sessionId was $current"
        }
    }
    return New-Green
}

function Invoke-S3 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Per session. A second session establishing and admitting after the first drains is legal, and
    # reading the drain across the vector is exactly the false red AL1 found.
    $drained = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string]$sessionEvent.session
        if ([string]$sessionEvent.step -eq 'transition' -and $sessionEvent.accepted -and [string]$sessionEvent.to -eq 'draining') {
            if (-not $drained.ContainsKey($sessionId)) { $drained[$sessionId] = $true }
            continue
        }
        if ([string]$sessionEvent.step -eq 'admit' -and $drained.ContainsKey($sessionId)) {
            return New-Red "session $sessionId admitted interaction $($sessionEvent.identity) after its own first drain transition"
        }
    }
    return New-Green
}

function Invoke-S4 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $terminal = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -ne 'transition' -or -not $sessionEvent.accepted) { continue }
        $sessionId = [string]$sessionEvent.session
        if ($terminal.ContainsKey($sessionId)) {
            return New-Red "session $sessionId reached terminal state $($terminal[$sessionId]) and then transitioned to $($sessionEvent.to) under the same session identity"
        }
        if ($terminalSessionStates -contains [string]$sessionEvent.to) { $terminal[$sessionId] = [string]$sessionEvent.to }
    }
    return New-Green
}

function Invoke-S5 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # For EACH session, over that session own declared profile. Two sessions carrying two different
    # declared profiles are conforming and this property says nothing about them, which is AL4.
    foreach ($session in @($Vector.sessions)) {
        $record = $session.establishedProfileRecord
        if ($null -eq $record) { continue }
        $fixed = ($record.fixed | ConvertTo-Json -Depth 12 -Compress)
        $negotiated = ($record.negotiated | ConvertTo-Json -Depth 12 -Compress)
        if ($fixed -cne $negotiated) {
            return New-Red "session $($session.id) produces different normative profile records from fixed and negotiated establishment of its own declared profile"
        }
    }
    return New-Green
}

function Invoke-S6 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $forbidden = @('ready', 'release', 'authority', 'application-outcome')
    foreach ($declaredEvent in @($Vector.sessionEvents)) {
        if ($null -eq $declaredEvent) { continue }
        foreach ($created in @($declaredEvent.creates)) {
            if ($forbidden -contains [string]$created) {
                return New-Red "session event $($declaredEvent.event) in session $($declaredEvent.session) creates $created"
            }
        }
    }
    return New-Green
}

function Invoke-I1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Per session: one identity may legitimately be dispatched in each of two sessions.
    $seen = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -ne 'dispatch') { continue }
        $key = "$($sessionEvent.session)|$($sessionEvent.identity)"
        if ($seen.ContainsKey($key)) {
            return New-Red "identity $($sessionEvent.identity) crossed the dispatch boundary twice in session $($sessionEvent.session)"
        }
        $seen[$key] = $true
    }
    return New-Green
}

function Invoke-I2 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        $histories = @($interaction.terminalHistories)
        if ($histories.Count -gt 1) {
            return New-Red "interaction $($interaction.identity) in session $($interaction.session) has $($histories.Count) terminal histories"
        }
    }
    return New-Green
}

function Invoke-I3 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $nonSemantic = @('cancellation-acknowledgement', 'drain', 'timeout', 'protocol-fault')
    foreach ($interaction in (Get-Interactions $Vector)) {
        foreach ($history in @($interaction.terminalHistories)) {
            if ($null -eq $history) { continue }
            if (($nonSemantic -contains [string]$history.form) -and $history.semanticSuccess) {
                return New-Red "interaction $($interaction.identity) records a $($history.form) terminal as a semantic success"
            }
        }
    }
    return New-Green
}

function Invoke-I4 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        $refusal = $interaction.refusal
        if ($null -eq $refusal) { continue }
        $stage = [string]$refusal.stage
        $certainty = [string]$refusal.effectCertainty
        if ($stage -eq 'pre-dispatch' -and $certainty -ne 'known-none') {
            return New-Red "interaction $($interaction.identity) records a pre-dispatch refusal with effect certainty $certainty"
        }
        if ($stage -eq 'post-dispatch' -and $certainty -ne 'unknown' -and -not $refusal.explicitEvidence) {
            return New-Red "interaction $($interaction.identity) records a possible post-dispatch loss as $certainty with no explicit evidence narrowing it"
        }
    }
    return New-Green
}

function Invoke-I5 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Concurrency is counted per session against THAT session bound, which is AK7. Counted across the
    # vector, two sessions each holding one nonterminal interaction breach a bound neither did.
    $bounds = @{}
    foreach ($session in @($Vector.sessions)) { $bounds[[string]$session.id] = [int]$session.establishedBound }
    $live = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string]$sessionEvent.session
        if (-not $live.ContainsKey($sessionId)) { $live[$sessionId] = 0 }
        if ([string]$sessionEvent.step -eq 'admit') { $live[$sessionId]++ }
        elseif ([string]$sessionEvent.step -eq 'terminal' -and $sessionEvent.accepted) { $live[$sessionId] = [Math]::Max(0, $live[$sessionId] - @($sessionEvent.closes).Count) }
        if ($bounds.ContainsKey($sessionId) -and $live[$sessionId] -gt $bounds[$sessionId]) {
            return New-Red "session $sessionId held $($live[$sessionId]) nonterminal interactions against its own established bound of $($bounds[$sessionId])"
        }
    }
    return New-Green
}

function Invoke-I6 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ([string]$interaction.class -ne 'relational') { continue }
        if ([int]$interaction.declarationMatches -ne 1) {
            return New-Red "relational interaction $($interaction.identity) matches $($interaction.declarationMatches) declarations"
        }
        if ($interaction.createsReadyOrRelease) {
            return New-Red "relational interaction $($interaction.identity) creates Ready or Release"
        }
    }
    return New-Green
}

function Invoke-I7 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        $changedBy = [string]$interaction.terminalHistoryChangedBy
        if ($changedBy -and $changedBy -ne [string]$interaction.identity) {
            return New-Red "interaction $($interaction.identity) had its terminal history changed by sibling $changedBy"
        }
    }
    return New-Green
}

function Invoke-C4P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Three clauses, each session-scoped under AK7. The second and third are the same claims I1 and I5
    # make, so they are evaluated by those functions rather than restated here: two implementations of
    # one claim is the duplication W1 exists to retire, arriving in the gate instead of in the prose.
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -ne 'terminal' -or -not $sessionEvent.accepted) { continue }
        $closes = @($sessionEvent.closes)
        if ($closes.Count -ne 1) {
            return New-Red "an accepted terminal fact in session $($sessionEvent.session) closes $($closes.Count) admitted interactions"
        }
    }
    $dispatchResult = Invoke-I1 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($dispatchResult.Verdict -eq 'red') { return New-Red "$($dispatchResult.Witness), which the second clause of C4-P1 forbids" }
    $boundResult = Invoke-I5 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($boundResult.Verdict -eq 'red') { return New-Red "$($boundResult.Witness), which the third clause of C4-P1 forbids" }
    return New-Green
}

# ---------------------------------------------------------------------------------------------
# The per-capability properties C1-P1 through C12-P1.
#
# Two of these are the machines' properties stated at capability level, and they are evaluated by
# CALLING those rather than by restating them: C2-P1 is S1 and S4, C8-P1 is I2 and I3. Two
# implementations of one claim is the duplication W1 exists to retire, and it is no better inside a
# verifier than inside prose -- the second copy is what goes stale.
# ---------------------------------------------------------------------------------------------

$provenanceForms = @('local-pre-dispatch-refusal', 'semantic-outcome', 'peer-protocol-fault', 'local-loss-observation')

function Invoke-C1P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Per session, and the disjunction is the property: an exact profile, OR nothing dispatchable with
    # known-none. A realization that has neither is what the mutation produces.
    foreach ($session in @($Vector.sessions)) {
        $exact = ([int]$session.establishedProfiles -eq 1) -and $session.profileFactsMatchExpected
        if ($exact) { continue }
        if ($session.dispatchable) {
            return New-Red "session $($session.id) has no established profile equal to the profile it expects, and interactions remain dispatchable"
        }
    }
    return New-Green
}

function Invoke-C2P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $tableResult = Invoke-S1 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($tableResult.Verdict -eq 'red') { return New-Red "$($tableResult.Witness), which the first clause of C2-P1 forbids" }
    $monotonic = Invoke-S4 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($monotonic.Verdict -eq 'red') { return New-Red "$($monotonic.Witness), which the third clause of C2-P1 forbids" }
    # The middle clause: any other input leaves the prior state unchanged or enters faulted. An input
    # recorded as an accepted transition that the table does not contain is caught above; an admission
    # recorded as accepted outside established is caught here.
    $state = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string]$sessionEvent.session
        if ([string]$sessionEvent.step -eq 'transition' -and $sessionEvent.accepted) { $state[$sessionId] = [string]$sessionEvent.to; continue }
        if ([string]$sessionEvent.step -ne 'admit' -or -not $sessionEvent.acceptedTransition) { continue }
        $current = if ($state.ContainsKey($sessionId)) { $state[$sessionId] } else { 'unestablished' }
        if ($current -ne 'established') {
            return New-Red "session $sessionId accepted a new interaction while it was $current, so an input that must leave the state unchanged or enter faulted admitted instead"
        }
    }
    return New-Green
}

function Invoke-C3P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -eq 'dispatch') { $dispatched["$($sessionEvent.session)|$($sessionEvent.identity)"] = $true }
    }
    foreach ($interaction in (Get-Interactions $Vector)) {
        if (-not $dispatched.ContainsKey("$($interaction.session)|$($interaction.identity)")) { continue }
        if (-not $interaction.profileMatch) {
            return New-Red "interaction $($interaction.identity) dispatched without its class and direction matching the established profile of session $($interaction.session)"
        }
        # false and unknown both refuse admission: only an exact true satisfies the predicate.
        if ($interaction.phasePredicate -isnot [bool] -or -not $interaction.phasePredicate) {
            return New-Red "interaction $($interaction.identity) dispatched with external phase predicate $($interaction.phasePredicate), and only an exact true matches"
        }
    }
    return New-Green
}

function Invoke-C5P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -eq 'dispatch') { $dispatched["$($sessionEvent.session)|$($sessionEvent.identity)"] = $true }
    }
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ($dispatched.ContainsKey("$($interaction.session)|$($interaction.identity)")) {
            if (-not $interaction.boundsChecked) {
                return New-Red "interaction $($interaction.identity) dispatched without passing every declared bound" 'C5-P1-clause-1'
            }
            if (-not $interaction.positionalShapeChecked) {
                return New-Red "interaction $($interaction.identity) dispatched without passing every positional Shape rule" 'C5-P1-clause-1'
            }
        }
        $refusal = $interaction.refusal
        if ($null -eq $refusal -or [string]$refusal.stage -ne 'pre-dispatch') { continue }
        if ([string]$refusal.effectCertainty -ne 'known-none') {
            return New-Red "interaction $($interaction.identity) records a pre-dispatch structural refusal with effect certainty $($refusal.effectCertainty)" 'C5-P1-clause-2'
        }
    }
    return New-Green
}

function Invoke-C6P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -eq 'dispatch') { $dispatched["$($sessionEvent.session)|$($sessionEvent.identity)"] = $true }
    }
    foreach ($interaction in (Get-Interactions $Vector)) {
        $decision = [string]$interaction.authorityDecision
        if ($dispatched.ContainsKey("$($interaction.session)|$($interaction.identity)") -and $decision -ne 'permitted') {
            return New-Red "interaction $($interaction.identity) reached handler dispatch with local authority decision $decision" 'C6-P1-clause-1'
        }
        if ($decision -eq 'permitted') { continue }
        $record = $interaction.authorityRecord
        if ($null -eq $record -or -not $record.decisionPoint -or -not $record.initiatorAttribution -or [string]$record.effectCertainty -ne 'known-none') {
            return New-Red "interaction $($interaction.identity) records a $decision authority presentation without its decision point, initiator attribution, and known-none" 'C6-P1-clause-2'
        }
    }
    return New-Green
}

function Invoke-C7P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = @{}
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string]$sessionEvent.step -eq 'dispatch') { $dispatched["$($sessionEvent.session)|$($sessionEvent.identity)"] = $true }
    }
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ([string]$interaction.class -ne 'relational') { continue }
        if (-not $dispatched.ContainsKey("$($interaction.session)|$($interaction.identity)")) { continue }
        if ([int]$interaction.declarationMatches -ne 1) {
            return New-Red "dispatched relational interaction $($interaction.identity) matches $($interaction.declarationMatches) lifecycle declarations"
        }
        if (-not $interaction.inPreReadyWindow) {
            return New-Red "dispatched relational interaction $($interaction.identity) does not occur in the pre-Ready window"
        }
        if ($interaction.createsReadyOrRelease) {
            return New-Red "dispatched relational interaction $($interaction.identity) produces a Ready or Release fact by itself"
        }
    }
    return New-Green
}

function Invoke-C8P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $singleTerminal = Invoke-I2 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($singleTerminal.Verdict -eq 'red') { return New-Red "$($singleTerminal.Witness), which the first clause of C8-P1 forbids" }
    $notSuccess = Invoke-I3 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($notSuccess.Verdict -eq 'red') { return New-Red "$($notSuccess.Witness), which the second clause of C8-P1 forbids" }
    return New-Green
}

function Invoke-C9P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        $form = [string]$interaction.provenanceForm
        if (-not $form) { continue }
        if ($provenanceForms -notcontains $form) {
            return New-Red "interaction $($interaction.identity) selects provenance form $form, which is not one of the four"
        }
        # The second clause: no field permits a local inference to be accepted as a peer statement.
        # The vector states what the observation actually was where the two differ, and a recorded form
        # that is not the actual one is exactly that acceptance.
        $actual = [string]$interaction.provenanceFormActually
        if ($actual -and $actual -ne $form) {
            return New-Red "interaction $($interaction.identity) records provenance form $form for what was actually a $actual"
        }
    }
    return New-Green
}

function Invoke-C10P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ($interaction.PSObject.Properties['observationComplete'] -and -not $interaction.observationComplete) {
            return New-Red "interaction $($interaction.identity) records an observation that is not complete for its provenance form"
        }
        if (-not $interaction.possiblePostDispatchPath) { continue }
        $refusal = $interaction.refusal
        if ($null -ne $refusal -and [string]$refusal.effectCertainty -eq 'known-none' -and -not $refusal.explicitEvidence) {
            return New-Red "interaction $($interaction.identity) has a possible post-dispatch path and records known-none with no explicit evidence that the handler did not begin"
        }
        foreach ($history in @($interaction.terminalHistories)) {
            if ($null -eq $history) { continue }
            if ([string]$history.effectCertainty -eq 'known-none' -and -not $history.explicitEvidence) {
                return New-Red "interaction $($interaction.identity) has a possible post-dispatch path and records a known-none terminal history with no explicit evidence that the handler did not begin"
            }
        }
    }
    return New-Green
}

function Invoke-C11P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    foreach ($session in @($Vector.sessions)) {
        foreach ($required in @($session.requiredFacets)) {
            if (@($session.supportedFacets) -notcontains [string]$required) {
                return New-Red "session $($session.id) requires facet $required and its established profile does not support it"
            }
        }
        if ($session.facetChangesCore) {
            return New-Red "session $($session.id) has a facet that changes a core identity, authority, terminal-provenance, or uncertainty result"
        }
    }
    return New-Green
}

function Invoke-C12P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Only the first clause is per vector. The second is over the declaration set and is evaluated once
    # below; the third is a dependency fact no vector carries and is enforced by the repository guards.
    if ($Vector.PSObject.Properties['deterministicExpectedObservation'] -and -not $Vector.deterministicExpectedObservation) {
        return New-Red "vector $VectorId has no single deterministic expected portable observation"
    }
    return New-Green
}

$evaluators = @{
    'C4-P2' = ${function:Invoke-C4P2}; 'C4-P1' = ${function:Invoke-C4P1}
    'S1' = ${function:Invoke-S1}; 'S2' = ${function:Invoke-S2}; 'S3' = ${function:Invoke-S3}
    'S4' = ${function:Invoke-S4}; 'S5' = ${function:Invoke-S5}; 'S6' = ${function:Invoke-S6}
    'I1' = ${function:Invoke-I1}; 'I2' = ${function:Invoke-I2}; 'I3' = ${function:Invoke-I3}
    'I4' = ${function:Invoke-I4}; 'I5' = ${function:Invoke-I5}; 'I6' = ${function:Invoke-I6}
    'I7' = ${function:Invoke-I7}
    'C1-P1' = ${function:Invoke-C1P1}; 'C2-P1' = ${function:Invoke-C2P1}; 'C3-P1' = ${function:Invoke-C3P1}
    'C5-P1' = ${function:Invoke-C5P1}; 'C6-P1' = ${function:Invoke-C6P1}; 'C7-P1' = ${function:Invoke-C7P1}
    'C8-P1' = ${function:Invoke-C8P1}; 'C9-P1' = ${function:Invoke-C9P1}; 'C10-P1' = ${function:Invoke-C10P1}
    'C11-P1' = ${function:Invoke-C11P1}; 'C12-P1' = ${function:Invoke-C12P1}
}


# ---------------------------------------------------------------------------------------------
# Citations. The design artifacts own every fact this file states, and these checks fail when the two
# disagree rather than letting the executable form drift into a twelfth surface of its own.
# ---------------------------------------------------------------------------------------------

$numberWords = @{ 'zero' = 0; 'one' = 1; 'two' = 2; 'three' = 3; 'four' = 4; 'five' = 5; 'six' = 6; 'seven' = 7; 'eight' = 8; 'nine' = 9; 'ten' = 10; 'eleven' = 11; 'twelve' = 12; 'thirteen' = 13; 'fourteen' = 14; 'fifteen' = 15; 'sixteen' = 16; 'seventeen' = 17; 'eighteen' = 18; 'nineteen' = 19; 'twenty' = 20; 'twenty-five' = 25; 'twenty-six' = 26 }

if ($briefPlain.IndexOf('a required-green set: the named legal inputs from the property', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add("The neutral brief's capability-wide property format no longer states the required-green set as a normative field. This file's expectations are written against that field, so its removal would leave every green expectation here unsourced. This is AE3's field.")
}

# A property is stated by ONE artifact and its mutation and required-green set are recorded by the
# completeness review's per-capability audit. The citation therefore resolves against the artifact the
# declaration names rather than always against the capability contract: C4-P1 and C4-P2 are the
# contract's, S1-S6 the session machine's, I1-I7 the interaction machine's. A check that looked only
# at the contract would have forced this file to restate the machines' properties to satisfy itself,
# which is the second-surface failure W1 exists to retire.
$artifactTextCache = @{}
function Get-ArtifactPlain {
    param([Parameter(Mandatory = $true)][string]$RepoRelativePath)
    if (-not $artifactTextCache.ContainsKey($RepoRelativePath)) {
        $artifactPath = Join-Path $repositoryRoot $RepoRelativePath
        if (-not (Test-Path -LiteralPath $artifactPath)) { $artifactTextCache[$RepoRelativePath] = '' }
        else { $artifactTextCache[$RepoRelativePath] = Get-PlainText (Get-Content -Raw -LiteralPath $artifactPath -Encoding UTF8) }
    }
    return $artifactTextCache[$RepoRelativePath]
}
$auditPlain = Get-PlainText (Get-Content -Raw -LiteralPath (Join-Path $channelPath 'Brontide-Channel-0.2-Contract-Completeness-Review-0.1.md') -Encoding UTF8)

foreach ($property in $properties.properties) {
    $statingArtifact = Get-ArtifactPlain ([string]$property.statedIn)
    if (-not $statingArtifact) {
        $failures.Add("Property '$($property.id)' names '$($property.statedIn)' as the artifact that states it and that file could not be read.")
    }
    elseif ($statingArtifact.IndexOf("$($property.id).", [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("Property '$($property.id)' is declared executable here and '$($property.statedIn)' states no property by that id.")
    }

    # The mutation and the required-green set are recorded by the completeness review's audit, which
    # is the artifact Batch 2 authors property files from. The contract also names C4's two scenarios.
    foreach ($mutation in $property.namedMutations) {
        if ($auditPlain.IndexOf([string]$mutation.vector, [System.StringComparison]::Ordinal) -lt 0 -and $contractPlain.IndexOf([string]$mutation.vector, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("Named mutation '$($mutation.vector)' for '$($property.id)' is named by no artifact. A mutation this file invents is a mutation no artifact requires, and a property red on it proves nothing about the design.")
        }
    }

    foreach ($member in $property.requiredGreen) {
        if ($auditPlain.IndexOf([string]$member.member, [System.StringComparison]::Ordinal) -lt 0 -and $contractPlain.IndexOf([string]$member.member, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("Required-green member '$($member.member)' for '$($property.id)' appears in no artifact's required-green set. Either the artifact's set changed and this file did not, or this file names a member no artifact requires -- and a required-green set that is not the artifact's set is a second surface for the fact rather than an execution of it.")
        }
    }


    # AK4's class, on this file's own count: the contract states how many legal members the group has,
    # and a set that names a different number is the defect rather than the count being decorative.
    #
    # AQ5. The key was a 4,000-character proximity window from the property's own marker, and C4's
    # passage has since grown: the marker and the count now sit 5,246 characters apart, so the match
    # stopped happening and the check stopped running. Nothing announced it, because a window that
    # no longer reaches its subject looks exactly like a subject that is not there. A character count
    # is a key the artifact can outgrow, and this is the third guard in this pass to have expired
    # without saying so.
    #
    # The region is bounded by the contract's own structure instead -- the next property marker or
    # the next capability heading -- so it grows with the passage it is about.
    $propertyRegion = [regex]::Match($contractPlain, "Property $([regex]::Escape($property.id))\.(.+?)(?=Property C[0-9]+-P[0-9]+\.|## C[0-9]+ |\z)")
    $memberCountClaim = [regex]::Match($propertyRegion.Groups[1].Value, 'required vector group has ([a-z-]+) legal members')
    if ($memberCountClaim.Success) {
        $claimedWord = $memberCountClaim.Groups[1].Value.ToLowerInvariant()
        if (-not $numberWords.ContainsKey($claimedWord)) {
            $failures.Add("The capability contract states '$($property.id)' has '$claimedWord' legal members in its required vector group, which is not a number word this check can read.")
        }
        elseif ($numberWords[$claimedWord] -ne @($property.requiredGreen).Count) {
            $failures.Add("The capability contract states '$($property.id)' has $($numberWords[$claimedWord]) legal members in its required vector group and this file declares $(@($property.requiredGreen).Count) required-green members.")
        }
    }
}

# AP2: every declared property must be REGISTERED in the completeness review's audit, by a row that
# carries its id. The audit is the artifact Batch 2 authors property files from and the register
# of property/mutation pairs, and until now nothing required a row per property: the design
# verifier's AF7 check sampled four ids -- `S1`, `S6`, `I1`, `I7` -- while its own comment said the
# rule is written over every property and criticised enforcement 'over the surfaces one audit
# happens to enumerate'. A row that kept its text and lost its property id passed both gates,
# probed, for the other twenty-two.
#
# Enforced here rather than there because the set of properties is this file's, and enforced over
# the declared set rather than a list, so a property added to the package is registered or fails.
# Two row shapes, because the audit has two tables: the S and I properties key a row by their own
# id, and a C-property is named inside its capability's row.
$auditTableRows = @([regex]::Matches($auditPlain, '\| ([A-Za-z0-9()-]+(?:-P[0-9]+)?) \|([^|]*)\|([^|]*)\|([^|]*)\|'))
foreach ($property in $properties.properties) {
    $propertyId = [string]$property.id
    $capabilityId = [string]$property.capability
    $registered = @($auditTableRows | Where-Object {
        $rowKey = $_.Groups[1].Value.Trim()
        ($rowKey -ceq $propertyId) -or ($rowKey -ceq $capabilityId -and $_.Value.IndexOf($propertyId, [System.StringComparison]::Ordinal) -ge 0)
    })
    if ($registered.Count -lt 1) {
        $failures.Add("The completeness review's per-capability property audit registers no row for '$propertyId'. That audit is the register of property/mutation pairs and the artifact Batch 2 authors property files from, so a property missing from it is a property the design has stopped claiming to have audited. This is AP2.")
    }
}

# C12-P1's second clause, evaluated once rather than per vector because it is a claim about the
# DECLARATION SET and not about any input: every C1-C12 group has at least one capability-wide
# property. The declaration says this clause is checked here, so it is checked here; a property that
# claims a clause is evaluated elsewhere and is not is worse than one that admits the clause is owed.
$declaredCapabilities = @($properties.properties | ForEach-Object { [string]$_.capability } | Sort-Object -Unique)
foreach ($capabilityNumber in 1..12) {
    $capabilityId = "C$capabilityNumber"
    if ($declaredCapabilities -notcontains $capabilityId) {
        $failures.Add("Capability group '$capabilityId' declares no capability-wide property, which C12-P1's second clause requires of every C1-C12 group. A group with no property is a group nothing can fail.")
    }
}

# C12-P1's third clause -- that neither stack nor the neutral peer imports the other's semantic
# runtime -- is not a fact any vector carries and is not evaluated here. It is enforced by
# build/verify-project-graph.ps1, Reference/build/verify-dependencies.ps1 and
# Minimal/build/verify-boundaries.ps1, all of which run in the repository gate beside this file. The
# delegation is recorded in the property declaration rather than left for a reader to discover.

# S1's legal transition table is stated by the session state machine and copied into this file,
# which is a second surface for one fact -- the failure W1 exists to retire, arriving in the gate.
# It is not left as a copy: every edge declared above must appear as a row of that artifact's own
# transition table, and the artifact must declare no accepted edge this file does not carry. A row
# added there and forgotten here would make S1 red on conforming behaviour, and a row deleted there
# and left here would make S1 unable to fail on it.
#
# AO1: that is what the comment promised and the row reader could not deliver. The table's last
# two rows say `any nonterminal` in the From cell -- a fatal recognized Channel violation and a
# transport/process loss both fault from wherever the session is -- and the reader required a
# backticked lowercase state there, so it saw eight rows out of ten and reported the two lists
# identical. `S1` and `C2-P1` were therefore red on a session that faulted from `established`,
# which every column of the coverage grid's `established` row routes to `faulted`. That is AE1's
# defect -- a property that cannot stay green on conforming behaviour -- reached through the guard
# written to prevent it.
#
# So the From cell is PARSED rather than matched, over the states the machine itself declares, and
# a cell this parser does not recognise is a failure rather than a row it drops quietly. The
# direction is AM1's permit list: a guard that silently drops what it cannot read certifies its own
# completeness, which is the shape this programme has now recorded eleven times.
$sessionMachinePath = Join-Path $channelPath 'Brontide-Channel-0.2-Session-State-Machine-0.1.md'
$sessionMachineText = Get-Content -Raw -LiteralPath $sessionMachinePath -Encoding UTF8
$sessionStateRows = @([regex]::Matches($sessionMachineText, '(?m)^\| `([a-z]+)` \| (yes|no) \| '))
$declaredSessionStates = @($sessionStateRows | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
# Which states are terminal is the artifact's own column, not a list here. Expanding `any
# nonterminal` over a hardcoded copy of that fact would be AN2's second enumeration arriving inside
# the fix for AO1; the copy this file does keep -- `$terminalSessionStates`, which S2 and S6 read --
# is checked against the column rather than trusted.
$terminalDeclared = @($sessionStateRows | Where-Object { $_.Groups[2].Value -eq 'yes' } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
if ((@($terminalDeclared | Sort-Object) -join ',') -cne (@($terminalSessionStates | Sort-Object) -join ',')) {
    $failures.Add("The session state machine marks '$($terminalDeclared -join "', '")' terminal and this file treats '$($terminalSessionStates -join "', '")' as terminal. Every property that asks whether a session has ended reads this list, and an ``any nonterminal`` transition row expands over its complement.")
}
$transitionSection = [regex]::Match($sessionMachineText, '(?ms)^## Legal transition table\r?\n(.+?)(?=^## |\z)').Groups[1].Value
$artifactEdgeList = [System.Collections.Generic.List[string]]::new()
foreach ($transitionRow in [regex]::Matches($transitionSection, '(?m)^\| ([^|]+) \| [^|]+ \| `([a-z]+)` \|')) {
    $fromCell = $transitionRow.Groups[1].Value.Trim()
    $toState = $transitionRow.Groups[2].Value
    $fromStates = @()
    if ($fromCell -match '^`([a-z]+)`$') { $fromStates = @($Matches[1]) }
    elseif ($fromCell -eq 'any nonterminal') { $fromStates = @($declaredSessionStates | Where-Object { $terminalDeclared -notcontains $_ }) }
    else {
        $failures.Add("The session state machine's legal transition table has a From cell this check cannot read: '$fromCell'. A row it cannot read is a row it drops, and dropping the two ``any nonterminal`` rows is what made S1 and C2-P1 red on a conforming session fault -- AO1. Either the cell names a state, or it names a class this parser is taught.")
        continue
    }
    foreach ($fromState in $fromStates) { $artifactEdgeList.Add("$fromState>$toState") }
}
$artifactEdges = @($artifactEdgeList | Sort-Object -Unique)
if ($declaredSessionStates.Count -eq 0) {
    $failures.Add('The session state machine publishes no state rows this check can read, so an `any nonterminal` transition row would expand over an empty set and the comparison below would pass by seeing nothing.')
}
if ($artifactEdges.Count -eq 0) {
    $failures.Add('The session state machine publishes no legal transition rows this check can read, so S1 would be evaluated against a table nothing pins. S1 is the property that reads that table.')
}
else {
    foreach ($declaredEdge in $legalSessionTransitions) {
        if ($artifactEdges -notcontains $declaredEdge) {
            $failures.Add("This file declares the session transition $declaredEdge legal and the session state machine's transition table has no such row. S1 is evaluated against this list, so an edge here that the artifact does not have is a property that stays green on an illegal transition.")
        }
    }
    foreach ($artifactEdge in $artifactEdges) {
        if ($legalSessionTransitions -notcontains $artifactEdge) {
            $failures.Add("The session state machine declares the transition $artifactEdge legal and this file does not carry it. S1 would go red on a conforming realization taking that edge, which is the false red AL1 and AK7 were each raised for.")
        }
    }
}

# The plan's section 4 measures properties executable in the gate. It is the number this file
# determines, so it is checked here rather than left to be edited by whoever remembers.
$executableClaim = [regex]::Match($planPlain, 'properties executable in the gate[^;]*?currently ([a-z-]+) of twenty-six')
if (-not $executableClaim.Success) {
    $failures.Add('The verification foundation plan no longer states how many properties are executable in the gate. That count is one of the five measures section 4 exists to keep honest, and this file is what determines it.')
}
else {
    $claimedWord = $executableClaim.Groups[1].Value.ToLowerInvariant()
    $actualExecutable = @($properties.properties).Count
    if (-not $numberWords.ContainsKey($claimedWord) -or $numberWords[$claimedWord] -ne $actualExecutable) {
        $failures.Add("The verification foundation plan says '$claimedWord of twenty-six' properties are executable in the gate, and $actualExecutable execute here.")
    }
}

# ---------------------------------------------------------------------------------------------
# The run.
# ---------------------------------------------------------------------------------------------

function Copy-Vector {
    param([Parameter(Mandatory = $true)]$Vector)
    return ($Vector | ConvertTo-Json -Depth 32 | ConvertFrom-Json)
}

$collectionPaths = @{ 'unseen-refusals' = 'unseenRefusals'; 'late-traffic-latches' = 'lateTrafficLatches' }

function Remove-PublishedField {
    # An operand mutation reverts one published field of one record class, exactly as closure review
    # 16's P3 did by hand. It returns how many records it reached: a mutation that reaches none would
    # report the unmutated verdict and read as "no fire", which is a false negative rather than a
    # finding.
    param([Parameter(Mandatory = $true)]$Vector, [Parameter(Mandatory = $true)][string]$DropPath)

    $segments = $DropPath.Split('.')
    $collection = $segments[0]
    if (-not $collectionPaths.ContainsKey($collection)) { return -1 }
    $records = @($Vector.observations.($collectionPaths[$collection]))
    $tail = @($segments[1..($segments.Length - 1)])
    $removed = 0
    foreach ($record in $records) {
        if ($null -eq $record) { continue }
        $parent = $record
        for ($index = 0; $index -lt $tail.Count - 1; $index++) {
            if ($null -eq $parent) { break }
            $property = $parent.PSObject.Properties[$tail[$index]]
            if ($null -eq $property) { $parent = $null; break }
            $parent = $property.Value
        }
        if ($null -eq $parent) { continue }
        if ($null -ne $parent.PSObject.Properties[$tail[-1]]) {
            $parent.PSObject.Properties.Remove($tail[-1])
            $removed++
        }
    }
    return $removed
}

$evaluationCount = 0
$mutationCount = 0
# The fifteen properties condition 2 of the hold names, counted separately because section 2a of
# the plan states their run as its own sentence. Taken from the hold's own list rather than from a
# bare number, so a property added to that condition joins this measure without anyone remembering.
$conditionTwoProperties = @('C4-P1', 'C4-P2') + @(1..6 | ForEach-Object { "S$_" }) + @(1..7 | ForEach-Object { "I$_" })
$conditionTwoEvaluations = 0
$conditionTwoVectors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($property in $properties.properties) {
    $propertyId = [string]$property.id
    if (-not $evaluators.ContainsKey($propertyId)) {
        $failures.Add("Property '$propertyId' is declared in channel-0.2-properties.json and this gate has no evaluator for it. A property declared executable and not executed is the state this file exists to end.")
        continue
    }
    $evaluator = $evaluators[$propertyId]

    $expectations = @{}
    foreach ($member in $property.requiredGreen) { $expectations[[string]$member.vector] = @{ Verdict = 'green'; Conjunct = $null; Role = 'required-green' } }
    foreach ($member in $property.additionalGreen) { $expectations[[string]$member.vector] = @{ Verdict = 'green'; Conjunct = $null; Role = 'additional-green' } }
    foreach ($mutation in $property.namedMutations) { $expectations[[string]$mutation.vector] = @{ Verdict = [string]$mutation.expected; Conjunct = [string]$mutation.conjunct; Role = 'named-mutation' } }

    # No input is evaluated that the property does not claim, and no input the property claims is
    # missing. A vector file and a property file are two statements about which inputs matter, and the
    # nine cycles behind this plan are what two statements of one fact cost.
    foreach ($vector in $vectorFile.vectors) {
        if (@($vector.propertyMemberships) -contains $propertyId -and -not $expectations.ContainsKey([string]$vector.id)) {
            $failures.Add("Vector '$($vector.id)' declares membership of '$propertyId' and the property declares no expectation for it. An input in a property's group with no stated expectation is the condition AE1 arose from.")
        }
    }

    $redCount = 0
    $greenCount = 0
    foreach ($vectorId in ($expectations.Keys | Sort-Object)) {
        if (-not $vectorsById.ContainsKey($vectorId)) {
            $failures.Add("Property '$propertyId' names input '$vectorId' and no such vector is declared.")
            continue
        }
        $vector = $vectorsById[$vectorId]
        $expected = $expectations[$vectorId]
        $result = & $evaluator -VectorId $vectorId -Vector $vector -Steps $vectorIndex[$vectorId]
        $evaluationCount++
        if ($conditionTwoProperties -contains $propertyId) {
            $conditionTwoEvaluations++
            [void]$conditionTwoVectors.Add($vectorId)
        }
        foreach ($evaluationError in $result.Errors) { $failures.Add($evaluationError) }

        if ($result.Verdict -eq 'red') { $redCount++ } else { $greenCount++ }

        if ($result.Verdict -ne $expected.Verdict) {
            $detail = ''
            if ($result.Verdict -eq 'red') { $detail = " Witness: $($result.Witness)." }
            $failures.Add("Property '$propertyId' is $($result.Verdict) on '$vectorId' ($($expected.Role)) and must be $($expected.Verdict).$detail")
        }
        elseif ($expected.Verdict -eq 'red' -and $expected.Conjunct -and $result.Conjunct -ne $expected.Conjunct) {
            $failures.Add("Property '$propertyId' is red on '$vectorId' through '$($result.Conjunct)' and the mutation is declared against '$($expected.Conjunct)'. One mutation per conjunct is the requirement: a conjunct whose mutation fires through the other conjunct is unfalsifiable in the suite however well the contract names it.")
        }

        # The vector states its own expectation too, and the two are compared rather than one being
        # read and the other trusted.
        $vectorExpectation = Get-Field $vector "expected.$propertyId"
        if ($null -eq $vectorExpectation) {
            $failures.Add("Vector '$vectorId' states no expectation for '$propertyId'.")
        }
        elseif ([string]$vectorExpectation -ne $expected.Verdict) {
            $failures.Add("Vector '$vectorId' expects '$propertyId' $vectorExpectation and the property declares $($expected.Verdict) for it.")
        }
    }

    if ($redCount -eq 0) {
        $failures.Add("Property '$propertyId' is green on every declared input. A property that cannot be made to fail is a review finding against the property, not evidence for the design.")
    }
    if ($greenCount -eq 0) {
        $failures.Add("Property '$propertyId' is red on every declared input, so nothing here shows it stays green on conforming behaviour. That is AE3's converse and the half ten review cycles did not audit.")
    }

    foreach ($operandMutation in $property.operandMutations) {
        $vectorId = [string]$operandMutation.vector
        if (-not $vectorsById.ContainsKey($vectorId)) {
            $failures.Add("Operand mutation '$($operandMutation.id)' names vector '$vectorId', which is not declared.")
            continue
        }
        $mutated = Copy-Vector $vectorsById[$vectorId]
        $reached = 0
        $unknownClass = $false
        foreach ($dropPath in $operandMutation.drop) {
            $removedHere = Remove-PublishedField -Vector $mutated -DropPath ([string]$dropPath)
            if ($removedHere -lt 0) {
                $unknownClass = $true
                $failures.Add("Operand mutation '$($operandMutation.id)' names a record class outside the closed set this gate knows how to revert: '$dropPath'.")
            }
            else { $reached += $removedHere }
        }
        if ($unknownClass) { continue }
        if ($reached -le 0) {
            $failures.Add("Operand mutation '$($operandMutation.id)' reverted no published field on '$vectorId'. A mutation that reaches nothing reports the unmutated verdict, so its result is a false negative rather than evidence the field is redundant.")
            continue
        }

        $mutatedSteps = $vectorIndex[$vectorId]
        $mutatedResult = & $evaluator -VectorId $vectorId -Vector $mutated -Steps $mutatedSteps
        $mutationCount++
        if ($mutatedResult.Verdict -ne [string]$operandMutation.mutated) {
            $detail = ''
            if ($mutatedResult.Verdict -eq 'red') { $detail = " Witness: $($mutatedResult.Witness)." }
            $failures.Add("Operand mutation '$($operandMutation.id)' leaves '$propertyId' $($mutatedResult.Verdict) on '$vectorId' and is declared to leave it $($operandMutation.mutated).$detail")
        }

        $publishedResult = & $evaluator -VectorId $vectorId -Vector $vectorsById[$vectorId] -Steps $mutatedSteps
        if ($publishedResult.Verdict -ne [string]$operandMutation.published) {
            $failures.Add("Operand mutation '$($operandMutation.id)' records the published verdict on '$vectorId' as $($operandMutation.published) and the published form evaluates $($publishedResult.Verdict).")
        }
    }
}

# AO2. Section 2a of the plan states what this gate runs, in two sentences and four numbers, and
# every one of them was prose. Adding one vector under AO1 moved all four, and nothing would have
# said so -- which is AN3, AN4 and AN5's shape, in the section describing this file. They are
# recomputed here for the reason the plan's section 4 measures are recomputed next door: a number
# about a run belongs to the thing that runs.
$countClaims = @(
    @{ Name = 'the fifteen properties condition 2 names'
       Pattern = 'run in the gate on every commit: ([0-9,]+) evaluations over ([0-9,]+) declared inputs'
       Evaluations = $conditionTwoEvaluations; Inputs = $conditionTwoVectors.Count }
    @{ Name = 'all twenty-six properties'
       Pattern = 'The gate runs ([0-9,]+) evaluations over ([0-9,]+) declared inputs'
       Evaluations = $evaluationCount; Inputs = @($vectorFile.vectors).Count }
)
foreach ($countClaim in $countClaims) {
    $countMatch = [regex]::Match($planPlain, $countClaim.Pattern)
    if (-not $countMatch.Success) {
        $failures.Add("The verification foundation plan's section 2a no longer states what this gate runs for '$($countClaim.Name)' in the form this check recomputes. A count of a run that only prose carries is a stale number waiting for the next input, which is what adding one vector did to all four of them.")
        continue
    }
    if ([int]($countMatch.Groups[1].Value -replace ',', '') -ne $countClaim.Evaluations) {
        $failures.Add("The verification foundation plan says this gate runs $($countMatch.Groups[1].Value) evaluations for '$($countClaim.Name)' and it runs $($countClaim.Evaluations).")
    }
    if ([int]($countMatch.Groups[2].Value -replace ',', '') -ne $countClaim.Inputs) {
        $failures.Add("The verification foundation plan says '$($countClaim.Name)' runs over $($countMatch.Groups[2].Value) declared inputs and it runs over $($countClaim.Inputs).")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

Write-Host "Channel 0.2 property verification passed: $(@($properties.properties).Count) of 26 properties executable, $evaluationCount property evaluations over $(@($vectorFile.vectors).Count) declared inputs, $mutationCount operand mutations."
