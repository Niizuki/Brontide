[CmdletBinding()]
param(
    # How many generated conforming vectors to evaluate every property over. This runs on every
    # commit rather than behind a switch: a hundred of them cost seven tenths of a second, and a
    # measure that runs only weekly protects the design only weekly. `verify-gate-self-checks.ps1`
    # raises the count for the deep run.
    #
    # Zero is allowed and skips generation, for bisecting a failure onto the declared corpus alone.
    [int]$GeneratedCount = 100,
    # The generator is seeded, so a reported counterexample is reproducible by re-running with the
    # seed and count it was found under. A rate nobody can reproduce is an anecdote.
    [int]$GeneratedSeed = 20260904
)

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

    # AX2. `dispatched` on an interaction record is a second surface for a fact the timeline already
    # states, and **no property reads it** -- every one of them derives dispatch from the timeline's
    # `dispatch` steps. Forty-nine declared interaction records carry the field, so a vector could say
    # an interaction was dispatched while its timeline never dispatches it, read to a human as one
    # thing and evaluate as another, with nothing to notice. That is the W1 class -- one fact, two
    # surfaces, maintained by hand -- on a field small enough that nobody looked at it, and it was
    # found by mutating a generated vector to disagree with itself and watching every property stay
    # green. The field is kept, because it is what a reader of the record sees, and reconciled here.
    # Accessed directly rather than through Get-Interactions/Get-Timeline: those are defined further
    # down this file, and a call to a function declared after the caller finds nothing at run time.
    foreach ($interaction in @(if ($null -eq $vector.interactions) { @() } else { $vector.interactions })) {
        $declaredDispatch = $interaction.PSObject.Properties['dispatched']
        if ($null -eq $declaredDispatch) { continue }
        $timelineDispatches = @(@(if ($null -eq $vector.sessionTimeline) { @() } else { $vector.sessionTimeline }) | Where-Object {
            [string]$_.step -eq 'dispatch' -and
            [string]$_.session -eq [string]$interaction.session -and
            [string]$_.identity -eq [string]$interaction.identity
        }).Count -gt 0
        if ([bool]$declaredDispatch.Value -ne $timelineDispatches) {
            $failures.Add("Vector '$($vector.id)' records interaction '$($interaction.identity)' in session '$($interaction.session)' as dispatched=$([bool]$declaredDispatch.Value) and its timeline says otherwise. Every property derives dispatch from the timeline, so the record's own field is read by nobody and can disagree with the fact it restates.")
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

# AU2, and both halves are one defect: an obligation that fires on what a vector does not SAY reports
# the same red as one that fires on what a realization did wrong, so nothing distinguishes them.
#
# `@($null)` is a ONE-element array in PowerShell, not an empty one, so a collection the vector does
# not publish reads as a collection holding one null. `C11-P1` was red on every vector that publishes
# no `requiredFacets` -- with a blank where the facet name belongs in its own witness -- and two
# evaluators already carried a local `if ($null -eq $history) { continue }` for the same thing, which
# patches one reader and leaves every other read exposed. An unpublished collection is empty.
function Get-List { param($Value) if ($null -eq $Value) { return @() } return @($Value) }

# A scalar an obligation reads has no such default. A vector that does not say whether the realization
# checked its declared bounds has not shown conformance and has not shown a violation either, and
# taking the property red on it is the AE1 shape waiting for the next required-green member: five
# properties were red on a conforming timeline whose interactions published no detail fields. Absence
# is an error against the vector, raised through the result's own error list, and never a verdict.
$script:UnpublishedFields = [System.Collections.Generic.List[string]]::new()
function Read-Required {
    param($Record, [Parameter(Mandatory = $true)][string]$Field, [Parameter(Mandatory = $true)][string]$Subject)

    $member = if ($null -eq $Record) { $null } else { $Record.PSObject.Properties[$Field] }
    if ($null -eq $member -or $null -eq $member.Value) {
        [void]$script:UnpublishedFields.Add("$Subject publishes no '$Field'")
        return $null
    }
    return $member.Value
}
# AR1. `-Conjunct` names WHICH clause of a multi-clause property went red. It is not new structure
# invented here: the check at the bottom of this file already requires a mutation declared against a
# conjunct to fire through that conjunct, and the reason it gave -- "a conjunct whose mutation fires
# through the other conjunct is unfalsifiable in the suite however well the contract names it" -- was
# enforced only for `C4-P2`, the one property that declared conjuncts. `C5-P1` and `C6-P1` each state
# two clauses in one sentence, each had one named mutation, and each mutation fired through the first
# clause. Naming the clauses is the mechanical decomposition that lets the existing rule reach them;
# the statement itself stays the contract's, verbatim and unrestated.
# AU1. Every call of this constructor is one obligation the evaluators enforce, and the check at the
# bottom of this file requires a declared input to reach each one. The unit is the constructor rather
# than the clause because a clause is what the contract calls a thing and an obligation is what the
# evaluator does: `C5-P1-clause-1` names one clause and returns two separate verdicts, and AR1's
# correction -- which keys on properties that declare a conjunct -- pinned the first and left the
# second deletable. Recording the call site here is what makes the class total over the file.
$script:ObligationsReached = [System.Collections.Generic.HashSet[int]]::new()
function New-Red {
    param([string]$Witness, [string]$Conjunct)
    [void]$script:ObligationsReached.Add((Get-PSCallStack)[1].ScriptLineNumber)
    return [pscustomobject]@{ Verdict = 'red'; Conjunct = $Conjunct; Witness = $Witness; Errors = [System.Collections.Generic.List[string]]::new() }
}
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
    foreach ($declaredEvent in (Get-List $Vector.sessionEvents)) {
        if ($null -eq $declaredEvent) { continue }
        foreach ($created in (Get-List $declaredEvent.creates)) {
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
        $histories = Get-List $interaction.terminalHistories
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
        foreach ($history in (Get-List $interaction.terminalHistories)) {
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
        # AT1: the two clauses are named, so a mutation cannot fire through the one it was not written
        # for. Until the operand measure reached it, nothing carried a pre-dispatch refusal into this
        # property's group at all and the first clause was deleteable with both gates green -- AR1's
        # finding on C5-P1, which declares conjuncts, on a property that did not.
        if ($stage -eq 'pre-dispatch' -and $certainty -ne 'known-none') {
            return New-Red "interaction $($interaction.identity) records a pre-dispatch refusal with effect certainty $certainty" 'I4-clause-1'
        }
        if ($stage -eq 'post-dispatch' -and $certainty -ne 'unknown' -and -not $refusal.explicitEvidence) {
            return New-Red "interaction $($interaction.identity) records a possible post-dispatch loss as $certainty with no explicit evidence narrowing it" 'I4-clause-2'
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
        elseif ([string]$sessionEvent.step -eq 'terminal' -and $sessionEvent.accepted) { $live[$sessionId] = [Math]::Max(0, $live[$sessionId] - (Get-List $sessionEvent.closes).Count) }
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
        $subject = "interaction $($interaction.identity) in session $($interaction.session)"
        if ([int](Read-Required $interaction 'declarationMatches' $subject) -ne 1) {
            return New-Red "relational interaction $($interaction.identity) matches $($interaction.declarationMatches) declarations"
        }
        if (Read-Required $interaction 'createsReadyOrRelease' $subject) {
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
        $closes = Get-List $sessionEvent.closes
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
        $subject = "interaction $($interaction.identity) in session $($interaction.session)"
        if (-not (Read-Required $interaction 'profileMatch' $subject)) {
            return New-Red "interaction $($interaction.identity) dispatched without its class and direction matching the established profile of session $($interaction.session)"
        }
        # false and unknown both refuse admission: only an exact true satisfies the predicate. Absent
        # is neither: a vector that does not publish the predicate has not stated an unknown one.
        $null = Read-Required $interaction 'phasePredicate' $subject
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
            $subject = "interaction $($interaction.identity) in session $($interaction.session)"
            if (-not (Read-Required $interaction 'boundsChecked' $subject)) {
                return New-Red "interaction $($interaction.identity) dispatched without passing every declared bound" 'C5-P1-clause-1'
            }
            if (-not (Read-Required $interaction 'positionalShapeChecked' $subject)) {
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
        $decision = [string](Read-Required $interaction 'authorityDecision' "interaction $($interaction.identity) in session $($interaction.session)")
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
        $subject = "interaction $($interaction.identity) in session $($interaction.session)"
        if ([int](Read-Required $interaction 'declarationMatches' $subject) -ne 1) {
            return New-Red "dispatched relational interaction $($interaction.identity) matches $($interaction.declarationMatches) lifecycle declarations"
        }
        if (-not (Read-Required $interaction 'inPreReadyWindow' $subject)) {
            return New-Red "dispatched relational interaction $($interaction.identity) does not occur in the pre-Ready window"
        }
        if (Read-Required $interaction 'createsReadyOrRelease' $subject) {
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
        foreach ($history in (Get-List $interaction.terminalHistories)) {
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
        foreach ($required in (Get-List $session.requiredFacets)) {
            if ((Get-List $session.supportedFacets) -notcontains [string]$required) {
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
        $script:UnpublishedFields.Clear()
        $result = & $evaluator -VectorId $vectorId -Vector $vector -Steps $vectorIndex[$vectorId]
        # AU2. A field the obligation read and this vector does not publish is reported against the
        # vector, before the verdict is compared: an obligation red because the input is silent proves
        # nothing about a realization, and a required-green member that is silent is the AE1 shape.
        foreach ($unpublished in ($script:UnpublishedFields | Sort-Object -Unique)) {
            $failures.Add("Property '$propertyId' reads a field vector '$vectorId' does not publish: $unpublished. An obligation cannot tell a realization that violates it from an input that does not state the fact, so a red here is not evidence and a green is not either.")
        }
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

# ---------------------------------------------------------------------------------------------
# AU1: every obligation is reached by a declared input.
#
# AR1 found a property clause that no input reached, and closed the class with a check over
# properties that DECLARE a conjunct. AT1-AT3 found three more that check could not see, and closed
# that class with a coverage measure over operands an expression never evaluated. Both instruments
# are blind to the same thing: an obligation whose condition IS evaluated, on every input, and never
# once takes the value that makes it fire. Eleven were, across nine properties -- including both
# clauses of `C2-P1`, whose one named mutation fires through the middle clause -- and each could be
# deleted outright with this gate, the design gate and the coverage gate all green.
#
# The unit is the `New-Red` call site, and that choice is the whole of the measure. The AT pass left
# the open problem as "separating a defensive null check from a second semantic obligation hiding
# beside it", after a deletion test over operands reported 124 of 247 and would have been abandoned
# as noise. The answer is not to separate them by analysis but to measure a unit that contains only
# semantic obligations: this constructor is the one place a property states a verdict, so a check
# over its call sites reports obligations and nothing else. It reports eleven.
#
# It is structural rather than lexical, which is AL1's and AT1's lesson: an obligation is a
# `New-Red` whatever the contract calls its clauses, so the class is total over this file by
# construction, and a twelfth obligation added tomorrow joins it without anyone registering it.
$obligationSites = [System.Collections.Generic.List[int]]::new()
$selfAst = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$null, [ref]$null)
foreach ($call in $selfAst.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.CommandAst] -and $node.GetCommandName() -eq 'New-Red'
    }, $true)) {
    $obligationSites.Add($call.Extent.StartLineNumber)
}
if ($obligationSites.Count -lt 1) {
    $failures.Add('No New-Red obligation site could be found in this file. Either the evaluators state no verdict, which is not what this gate is for, or the syntax-tree query no longer matches the constructor and this check is passing by seeing nothing.')
}
# The measure keys on the line, because that is what the call stack reports. Two obligations sharing a
# line are therefore one site to it, and reaching either would mark both -- a hole of exactly the kind
# this check exists to close, so it is refused rather than left to be discovered.
foreach ($shared in ($obligationSites | Group-Object | Where-Object { $_.Count -gt 1 })) {
    $failures.Add("Line $($shared.Name) of this file states $($shared.Count) obligations. This check identifies an obligation by the line its verdict is constructed on, so two on one line are indistinguishable to it and reaching either would report both as pinned. Put each on its own line.")
}
foreach ($site in ($obligationSites | Sort-Object -Unique)) {
    if ($script:ObligationsReached.Contains($site)) { continue }
    $sourceLine = (Get-Content -LiteralPath $PSCommandPath -Encoding UTF8)[$site - 1].Trim()
    $failures.Add("No declared input makes the obligation at line ${site} of this file fire: $sourceLine  That obligation can be deleted outright with every gate green, so nothing in the suite distinguishes an implementation that honours it from one that does not. Give the property a named mutation that fires through it.")
}

# ---------------------------------------------------------------------------------------------
# Generated conforming vectors -- the eleventh condition-4 pass, by owner ruling of 2026-09-04.
#
# WHY THIS RUNS ON EVERY COMMIT. A hundred vectors cost seven tenths of a second against this gate's
# one second, and a measure that runs weekly protects the design weekly. The deep run raises the count
# under `verify-gate-self-checks.ps1`; the cost is superlinear, so the count is a dial rather than a
# thing to maximise -- 100 costs 0.7s, 500 costs 5.7s and 2,000 costs 47s.
#
# WHY THIS EXISTS. Every property above is checked against HAND-AUTHORED vectors with hand-chosen
# mutations, so the design is tested only in the cases someone thought to write. Ten passes have now
# audited the verification machinery and none has examined the design's own claims; the last reading
# of those was closure review 16. This generates vectors from the design's declared rules instead,
# and a property that goes red on one is red on conforming behaviour -- AE1's class, which this
# programme has already paid for twice.
#
# WHY IT RUNS AFTER THE OBLIGATION CHECK ABOVE, AND NOT BEFORE. That check requires a DECLARED input
# to reach each obligation. Generated vectors reach obligations too, and if they ran first an
# obligation pinned by nothing but a random vector would read as pinned. The order is the whole of
# that separation, so this block must stay below it.
#
# WHY IT IS A RATE AND NOT A LIST. This is the first instrument here whose output is a number that
# strengthens with more input: "no property was red over N generated vectors" says more at 10,000
# than at 100, and a list of hand-picked inputs cannot say it at all. The generator is seeded so a
# counterexample is reproducible from the seed and count it was found under.
#
# WHAT CONFORMANCE MEANS HERE, AND THE LIMIT THAT COMES WITH IT. Each vector is built to satisfy the
# design's stated rules by construction: transitions are drawn only from the legal table this file
# already cross-checks against the session state machine, interactions dispatch only from
# `established`, admission stops at the session's first drain, concurrency stays inside the
# established bound, and every per-interaction fact is set to the conforming value. So a red is
# either a property that is wrong or a generator that is wrong, and the two are told apart by
# reading the witness against the artifact -- the artifact is the authority, exactly as it is for a
# probe. A generator asserting its own idea of the design would be a twelfth surface publishing it,
# which is the failure W1 exists to retire.
if ($GeneratedCount -gt 0) {
    $random = [System.Random]::new($GeneratedSeed)

    function New-ConformingVector {
        param([Parameter(Mandatory = $true)][string]$Id, [Parameter(Mandatory = $true)][System.Random]$Random)

        $sessions = [System.Collections.Generic.List[object]]::new()
        $timeline = [System.Collections.Generic.List[object]]::new()
        $interactions = [System.Collections.Generic.List[object]]::new()
        $sessionEvents = [System.Collections.Generic.List[object]]::new()
        # C4's frame-level view, which is a different record from the session timeline above: declared
        # stimulus steps are what `Test-Precedes` orders and what a frame reference resolves against,
        # and the observation records are what `C4-P2`'s two conjuncts quantify over. Without them
        # both conjuncts iterate an empty collection and return green having asserted nothing, which
        # is where the twelfth pass left the instrument.
        $declaredSteps = [System.Collections.Generic.List[object]]::new()
        $delivery = [System.Collections.Generic.List[object]]::new()
        $unseenRefusals = [System.Collections.Generic.List[object]]::new()
        $lateTrafficLatches = [System.Collections.Generic.List[object]]::new()
        $admittedSets = [System.Collections.Generic.List[object]]::new()
        $arrivalOrdinal = 0

        foreach ($sessionOrdinal in 1..($Random.Next(1, 4))) {
            $sessionId = "s$sessionOrdinal"
            $bound = $Random.Next(1, 4)
            # The profile record is one value used twice: S5 compares fixed against negotiated
            # establishment of the session's own declared profile, and they are equal on a conforming
            # realization.
            $profileRecord = [pscustomobject]@{
                fixed = [pscustomobject]@{ version = '0.2'; facets = @('core'); limits = [pscustomobject]@{ maxInFlight = $bound } }
                negotiated = [pscustomobject]@{ version = '0.2'; facets = @('core'); limits = [pscustomobject]@{ maxInFlight = $bound } }
            }
            $sessions.Add([pscustomobject]@{
                id = $sessionId
                establishedProfile = "neutral-fixed-$sessionOrdinal"
                initialSessionState = 'unestablished'
                initialInteractionState = 'idle'
                establishedBound = $bound
                establishedProfileRecord = $profileRecord
                establishedProfiles = 1
                profileFactsMatchExpected = $true
                dispatchable = $true
                requiredFacets = @('core')
                supportedFacets = @('core')
                facetChangesCore = $false
            })

            # Establishment takes one of the two legal routes to `established`. Both are edges the
            # legal table carries, and which one a realization takes is not a property's business.
            if ($Random.Next(0, 2) -eq 0) {
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'unestablished'; to = 'established'; event = 'validate-fixed-profile'; accepted = $true })
            }
            else {
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'unestablished'; to = 'establishing'; event = 'offer-profile'; accepted = $true })
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'establishing'; to = 'established'; event = 'accept-profile'; accepted = $true })
            }

            # Admitted interactions, in waves that run up to but never past the session's own
            # established bound -- I5 and C4-P1's third clause are what that is about. The wave is the
            # point: admitting each interaction and closing it before admitting the next leaves the
            # live count at one, which satisfies every bound trivially and tests neither property. A
            # wave of exactly `bound` reaches the boundary from the legal side, which is where a
            # comparison written with the wrong operator shows itself.
            #
            # `1..0` counts DOWN in PowerShell and yields 1,0, so a range is not how a possibly-empty
            # sequence is written. A session carrying no interaction at all is a legal input, and this
            # is what lets the generator produce one.
            $interactionCount = $Random.Next(0, ($bound * 2) + 2)
            $waveLive = 0
            $waveIdentities = [System.Collections.Generic.List[object]]::new()
            for ($interactionOrdinal = 1; $interactionOrdinal -le $interactionCount; $interactionOrdinal++) {
                $identity = "i$interactionOrdinal"
                $isRelational = ($Random.Next(0, 2) -eq 0)
                # One in four admitted interactions is REFUSED before dispatch instead of dispatched.
                # That is a legal realization and it is the half of the design the conforming-only
                # generator could not reach: with no refusal anywhere in the population, `I4`'s first
                # clause, `C5-P1`'s second and `C6-P1`'s second are evaluated by nothing, because each
                # of them gates on a refusal or on a decision that is not `permitted`.
                $isRefused = ($Random.Next(0, 4) -eq 0)
                $terminalForm = if ($isRefused) { 'protocol-fault' }
                    elseif ($Random.Next(0, 2) -eq 0) { 'outcome' }
                    else { 'cancellation-acknowledgement' }
                # I3 and C8-P1's second clause: only an application outcome is a semantic success.
                $semanticSuccess = ($terminalForm -eq 'outcome')
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'admit'; identity = $identity })
                $waveLive++
                $waveIdentities.Add([pscustomobject]@{ Identity = $identity; Form = $terminalForm })
                if (-not $isRefused) {
                    $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'dispatch'; identity = $identity })
                }
                # The wave closes when it is full or when the last interaction has been admitted, and
                # each terminal names the one identity it closes and the form that identity's own
                # record carries. Emitting a form here that the interaction record does not hold would
                # make the vector incoherent, and an incoherent vector produces a finding about the
                # generator wearing the shape of a finding about the design.
                if ($waveLive -ge $bound -or $interactionOrdinal -eq $interactionCount) {
                    foreach ($waveMember in $waveIdentities) {
                        $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'terminal'; identity = $waveMember.Identity; form = $waveMember.Form; semanticSuccess = ($waveMember.Form -eq 'outcome'); closes = $waveMember.Identity; accepted = $true })
                    }
                    $waveIdentities.Clear()
                    $waveLive = 0
                }
                $interactions.Add([pscustomobject]@{
                    session = $sessionId
                    identity = $identity
                    class = $(if ($isRelational) { 'relational' } else { 'operational' })
                    declarationMatches = 1
                    createsReadyOrRelease = $false
                    dispatched = (-not $isRefused)
                    # A pre-dispatch structural refusal records `known-none`, which is `I4`'s first
                    # clause and `C5-P1`'s second saying the same thing about the same record.
                    refusal = $(if ($isRefused) {
                        [pscustomobject]@{ stage = 'pre-dispatch'; effectCertainty = 'known-none'; explicitEvidence = $true }
                    } else { $null })
                    terminalHistories = @([pscustomobject]@{ form = $terminalForm; semanticSuccess = $semanticSuccess; effectCertainty = $(if ($isRefused) { 'known-none' } else { 'known' }); explicitEvidence = $true })
                    direction = 'initiator-to-recipient'
                    phasePredicate = $true
                    profileMatch = $true
                    boundsChecked = $true
                    positionalShapeChecked = $true
                    # A refused interaction carries the denial and everything the denial owes:
                    # `C6-P1`'s second clause requires a decision point, an initiator attribution and
                    # `known-none` of every presentation that is not `permitted`, and with every
                    # interaction permitted that clause had no input either.
                    authorityDecision = $(if ($isRefused) { 'denied' } else { 'permitted' })
                    authorityRecord = [pscustomobject]@{ decisionPoint = 'pre-dispatch'; initiatorAttribution = "initiator-$sessionOrdinal"; effectCertainty = 'known-none' }
                    inPreReadyWindow = $true
                    provenanceForm = $(if ($isRefused) { 'local-pre-dispatch-refusal' } elseif ($semanticSuccess) { 'semantic-outcome' } else { 'local-loss-observation' })
                    observationComplete = $true
                    # There is no post-dispatch path when the refusal precedes dispatch, which is why
                    # `C10-P1` does not require explicit evidence narrowing one here.
                    possiblePostDispatchPath = (-not $isRefused)
                    deterministicExpectedObservation = $true
                })
            }

            # C4's frames for this session, and the two observation records that read them. Both are
            # built to be **conforming**, which for `C4-P2` means the two situations its conjuncts
            # forbid do not occur while the records they quantify over do exist:
            #
            #   * Conjunct 1 forbids an `unseen` refusal of a cancellation control whose request the
            #     same endpoint had already committed AND whose identity the recipient afterwards
            #     admits. The control here names an identity **no request ever opened**, which is the
            #     legitimate `unseen` case the design keeps `rejected-protocol` for, so the conjunct's
            #     request set is empty and it is green on a record it actually examined.
            #   * Conjunct 2 forbids a late-traffic latch settled against a frame committed BEFORE the
            #     endpoint's own terminal frame. Here the settling frame is committed after it, which
            #     is what late traffic is, so the comparison runs and finds nothing.
            #
            # The refusal's `detailedReason`, `provenance` and `frameDecision` are the selectors the
            # conjunct narrows on; a record missing them would be skipped and prove nothing.
            $unopenedIdentity = "u$sessionOrdinal"
            $requestStep = "$sessionId-request"
            $controlStep = "$sessionId-control-unopened"
            $terminalStep = "$sessionId-terminal"
            $lateStep = "$sessionId-late"
            $frameSpecs = @(
                @{ Id = $requestStep; Kind = 'request'; Endpoint = 'initiator'; Identity = 'f1'; Commit = 1 },
                @{ Id = $controlStep; Kind = 'cancellation-control'; Endpoint = 'initiator'; Identity = $unopenedIdentity; Commit = 1 },
                @{ Id = $terminalStep; Kind = 'outcome'; Endpoint = 'recipient'; Identity = 'f1'; Commit = 1 },
                @{ Id = $lateStep; Kind = 'cancellation-control'; Endpoint = 'recipient'; Identity = 'f1'; Commit = 2 })
            foreach ($frameSpec in $frameSpecs) {
                $arrivalOrdinal++
                $declaredSteps.Add([pscustomobject]@{
                    id = $frameSpec.Id
                    kind = $frameSpec.Kind
                    committingEndpoint = $frameSpec.Endpoint
                    session = $sessionId
                    interactionIdentity = $frameSpec.Identity
                    commitIndex = $frameSpec.Commit
                })
                $delivery.Add([pscustomobject]@{
                    step = $frameSpec.Id
                    disposition = 'delivered'
                    receivingEndpoint = $(if ($frameSpec.Endpoint -eq 'initiator') { 'recipient' } else { 'initiator' })
                    arrivalOrdinal = $arrivalOrdinal
                })
            }
            # The recipient admits the identity it was asked to open, and never the unopened one --
            # which is the operand AF8 scoped to the session and AK1 was raised for.
            $admittedSets.Add([pscustomobject]@{ session = $sessionId; identities = @('f1') })
            $unseenRefusals.Add([pscustomobject]@{
                provenance = 'recipient'
                frameDecision = 'rejected-protocol'
                detailedReason = 'unopened-interaction-identity'
                effectCertainty = 'known-none'
                refusedFrame = [pscustomobject]@{
                    kind = 'cancellation-control'
                    session = $sessionId
                    interactionIdentity = $unopenedIdentity
                    committingEndpoint = 'initiator'
                    arrivalOrdinal = ($arrivalOrdinal - 2)
                }
            })
            $lateTrafficLatches.Add([pscustomobject]@{
                category = 'state-violation'
                latchValue = 'fault-committed'
                settlingFrame = [pscustomobject]@{
                    kind = 'cancellation-control'
                    session = $sessionId
                    interactionIdentity = 'f1'
                    committingEndpoint = 'recipient'
                    arrivalOrdinal = $arrivalOrdinal
                }
                terminalFrame = [pscustomobject]@{
                    kind = 'outcome'
                    session = $sessionId
                    interactionIdentity = 'f1'
                    committingEndpoint = 'recipient'
                    arrivalOrdinal = ($arrivalOrdinal - 1)
                }
            })

            # The session ends terminal, by drain or by a recognized fault from a nonterminal state.
            # Both are legal edges; S4 is what forbids anything after one.
            if ($Random.Next(0, 4) -eq 0) {
                # From `established`, and not from a randomly drawn nonterminal state: the session is
                # in `established` by this point, and a transition out of a state it is not in is a
                # fact the timeline does not support. The machine's `any nonterminal` fault rows are
                # wider than that, and their width is exercised by the establishment route above
                # rather than pretended at here.
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'established'; to = 'faulted'; event = 'recognized-violation'; accepted = $true })
                $sessionEvents.Add([pscustomobject]@{ session = $sessionId; event = 'recognized-violation'; creates = @() })
            }
            else {
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'established'; to = 'draining'; event = 'begin-drain'; accepted = $true })
                $timeline.Add([pscustomobject]@{ session = $sessionId; step = 'transition'; from = 'draining'; to = 'closed'; event = 'close'; accepted = $true })
                $sessionEvents.Add([pscustomobject]@{ session = $sessionId; event = 'begin-drain'; creates = @() })
                $sessionEvents.Add([pscustomobject]@{ session = $sessionId; event = 'close'; creates = @() })
            }
        }

        return [pscustomobject]@{
            id = $Id
            capability = 'generated'
            propertyMemberships = @()
            role = 'generated-conforming'
            summary = "Generated conforming vector $Id."
            sessions = @($sessions)
            sessionTimeline = @($timeline)
            interactions = @($interactions)
            sessionEvents = @($sessionEvents)
            declaredSteps = @($declaredSteps)
            delivery = @($delivery)
            observations = [pscustomobject]@{
                recipientAdmittedIdentities = @($admittedSets)
                unseenRefusals = @($unseenRefusals)
                lateTrafficLatches = @($lateTrafficLatches)
            }
            deterministicExpectedObservation = $true
        }
    }

    # The step index `C4-P2` is evaluated against, built the way the declared corpus's is at load:
    # `DeclaredOrder` is the position in the declared sequence and the only thing `Test-Precedes`
    # reads, and `ArrivalOrdinal` is an identifier a reference matches for equality and never an
    # ordering operand. Building it here rather than reusing the loader is the one duplication this
    # block carries, and it is why the shape assertions below check the index rather than trusting it.
    function New-GeneratedStepIndex {
        param([Parameter(Mandatory = $true)]$Vector)

        $order = 0
        $byId = @{}
        foreach ($step in @($Vector.declaredSteps)) {
            $byId[[string]$step.id] = [pscustomobject]@{
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
        foreach ($disposition in @($Vector.delivery)) {
            $entry = $byId[[string]$disposition.step]
            if ($null -eq $entry) { continue }
            if ([string]$disposition.disposition -eq 'delivered') {
                $entry.Delivered = $true
                $entry.ReceivingEndpoint = [string]$disposition.receivingEndpoint
                $entry.ArrivalOrdinal = [int]$disposition.arrivalOrdinal
            }
        }
        return @($byId.Values | Sort-Object DeclaredOrder)
    }

    # The generator's own required shapes, and the reason they are asserted here rather than measured
    # by the coverage gate next door. That gate runs each covered gate under a line trace, and tracing
    # even twenty-five generated vectors costs several times the whole of that measure -- so the
    # generated block is declared exempt there and covered here instead, by something stronger than a
    # line trace: a line trace says a branch was taken, and these say the population actually contains
    # the shapes the properties are supposed to be exercised over.
    #
    # A generator that quietly stopped producing one of these would keep reporting a large number of
    # green evaluations over a population that had lost its variety, which is the failure this whole
    # instrument would otherwise be prone to -- a rate is only worth what its inputs cover.
    $shapesSeen = @{}
    $requiredShapes = @{
        'a session that reaches a terminal state by faulting'      = 'faulted'
        'a session that establishes through `establishing`'        = 'establishing'
        'a session that drains and closes'                         = 'closed'
        'a session carrying no interaction at all'                 = 'empty-session'
        'a vector carrying more than one session'                  = 'multi-session'
        'a wave that fills the session''s established bound'       = 'bound-filled'
        'an interaction refused before dispatch'                   = 'pre-dispatch-refusal'
        'an `unseen` refusal record C4-P2''s first conjunct selects' = 'unseen-refusal'
        'a settled late-traffic latch its second conjunct reads'   = 'late-traffic-latch'
    }

    $generatedEvaluations = 0
    $generatedRed = [System.Collections.Generic.List[string]]::new()
    foreach ($generatedOrdinal in 1..$GeneratedCount) {
        $generatedId = "generated-$generatedOrdinal"
        $generatedVector = New-ConformingVector -Id $generatedId -Random $random
        $generatedSteps = New-GeneratedStepIndex -Vector $generatedVector

        $generatedTimeline = @($generatedVector.sessionTimeline)
        foreach ($shapeTo in @($generatedTimeline | Where-Object { [string]$_.step -eq 'transition' } | ForEach-Object { [string]$_.to })) {
            $shapesSeen[$shapeTo] = $true
        }
        if (@($generatedVector.sessions).Count -gt 1) { $shapesSeen['multi-session'] = $true }
        foreach ($shapeSession in @($generatedVector.sessions)) {
            $sessionAdmits = @($generatedTimeline | Where-Object { [string]$_.step -eq 'admit' -and [string]$_.session -eq [string]$shapeSession.id }).Count
            if ($sessionAdmits -eq 0) { $shapesSeen['empty-session'] = $true }
            # The wave filled the bound when the session admitted at least that many, which is the
            # boundary I5 and C4-P1's third clause are evaluated at from the legal side.
            if ($sessionAdmits -ge [int]$shapeSession.establishedBound) { $shapesSeen['bound-filled'] = $true }
        }
        foreach ($shapeInteraction in @(if ($null -eq $generatedVector.interactions) { @() } else { $generatedVector.interactions })) {
            if ($null -ne $shapeInteraction.refusal -and [string]$shapeInteraction.refusal.stage -eq 'pre-dispatch') { $shapesSeen['pre-dispatch-refusal'] = $true }
        }
        # Keyed on the selector values the conjunct narrows on, not merely on a record existing:
        # a refusal the conjunct skips proves as little as no refusal at all.
        foreach ($shapeRefusal in @($generatedVector.observations.unseenRefusals)) {
            if ($null -eq $shapeRefusal) { continue }
            if ([string]$shapeRefusal.provenance -eq 'recipient' -and
                [string]$shapeRefusal.frameDecision -eq 'rejected-protocol' -and
                [string]$shapeRefusal.detailedReason -eq 'unopened-interaction-identity' -and
                [string]$shapeRefusal.refusedFrame.kind -eq 'cancellation-control') { $shapesSeen['unseen-refusal'] = $true }
        }
        foreach ($shapeLatch in @($generatedVector.observations.lateTrafficLatches)) {
            if ($null -eq $shapeLatch) { continue }
            if ([string]$shapeLatch.category -eq 'state-violation' -and [string]$shapeLatch.latchValue -eq 'fault-committed' -and
                $null -ne $shapeLatch.settlingFrame -and $null -ne $shapeLatch.terminalFrame) { $shapesSeen['late-traffic-latch'] = $true }
        }

        foreach ($propertyId in ($evaluators.Keys | Sort-Object)) {
            $script:UnpublishedFields.Clear()
            $generatedResult = & $evaluators[$propertyId] -VectorId $generatedId -Vector $generatedVector -Steps $generatedSteps
            $generatedEvaluations++
            # Guarded rather than piped unconditionally: `Sort-Object` on an empty list, run once per
            # property per vector, is most of the cost of the whole measure at any useful count.
            if ($script:UnpublishedFields.Count -gt 0) {
                foreach ($unpublished in ($script:UnpublishedFields | Sort-Object -Unique)) {
                    if ($generatedRed.Count -lt 10) {
                        $generatedRed.Add("'$propertyId' reads a field the generator does not publish on '$generatedId': $unpublished")
                    }
                }
            }
            if ($generatedResult.Verdict -eq 'red' -and $generatedRed.Count -lt 10) {
                $generatedRed.Add("'$propertyId' is red on generated conforming vector '$generatedId': $($generatedResult.Witness)")
            }
        }
    }
    foreach ($requiredShape in ($requiredShapes.Keys | Sort-Object)) {
        if (-not $shapesSeen.ContainsKey($requiredShapes[$requiredShape])) {
            $failures.Add("No generated vector carried $requiredShape over $GeneratedCount at seed $GeneratedSeed. A rate is worth what its inputs cover, and a generator that has quietly stopped producing one of the shapes the properties are meant to be exercised over reports the same large green number over a narrower population.")
        }
    }
    foreach ($generatedFinding in $generatedRed) {
        $failures.Add("$generatedFinding. Reproduce with -GeneratedSeed $GeneratedSeed -GeneratedCount $GeneratedCount. Either the property is red on conforming behaviour, which is AE1's class, or the generator builds a vector the design does not permit -- read the witness against the artifact, which is the authority here exactly as it is for a probe.")
    }
    Write-Host "Channel 0.2 generated-vector evaluation: $generatedEvaluations evaluations over $GeneratedCount generated conforming vectors at seed $GeneratedSeed, $($generatedRed.Count) red."
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

Write-Host "Channel 0.2 property verification passed: $(@($properties.properties).Count) of 26 properties executable, $evaluationCount property evaluations over $(@($vectorFile.vectors).Count) declared inputs, $mutationCount operand mutations."
