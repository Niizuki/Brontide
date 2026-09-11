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
    [int]$GeneratedSeed = 20260904,
    # How many of those vectors AZ3's dropped-field sweep also runs over, counted from the first and
    # never more than `GeneratedCount`. It has its own count because it costs eighteen further
    # `C4-P2` evaluations per vector where the main measure costs twenty-six across all properties,
    # and because the guard corpus re-runs this whole gate once per probe -- so a sweep pinned to
    # `GeneratedCount` would multiply the cost of the probe corpus rather than of one run.
    #
    # Forty is well past what the sweep needs: every dropping's outcome is fixed by whether the
    # vector carries one session or several, so forty vectors cover each declared case many times
    # over. `verify-gate-self-checks.ps1` raises it for the deep run, exactly as it does the other.
    [int]$SweptCount = 40,
    # How many (property, input) pairs BB1's read-provenance census walks, counted from the first and
    # never more than the corpus holds. Zero, the default, means all of them.
    #
    # It exists for the same reason `SweptCount` does and it is a COST dial, not a fidelity one. The
    # coverage measure runs this gate under a line trace where every executed statement costs about a
    # millisecond, and the census is thousands of evaluator calls -- so tracing the whole of it was ten
    # minutes of one probe. Coverage needs each construct REACHED once, which two pairs do.
    #
    # What a cap does NOT weaken: the `Read-Optional` exercise check below is fed by the declared
    # corpus loop rather than by the census -- census reads are suppressed from it deliberately -- so a
    # capped census still checks every declaration. What it does weaken is the census itself, and the
    # summary line says so rather than reading like a full run.
    [int]$CensusPairs = 0
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

    # One pass over the steps rather than a `Where-Object` pipeline per field. The filters are a
    # conjunction, so applying them in one loop admits exactly the steps five successive filters
    # admitted; what changes is the cost, and only the cost. It was five pipelines per call, whose
    # SETUP -- not their element count -- was about twenty milliseconds of every `C4-P2` evaluation,
    # and AZ3's sweep multiplies those evaluations by eighteen. The declared corpus, the nine operand
    # mutations and the sweep's own eight load-bearing droppings all constrain this function, and all
    # of them are what say the rewrite kept its meaning.
    $publishedKind = Get-Field $Reference 'kind'
    $publishedSession = Get-Field $Reference 'session'
    $publishedIdentity = Get-Field $Reference 'interactionIdentity'
    $publishedEndpoint = Get-Field $Reference 'committingEndpoint'
    $publishedOrdinal = Get-Field $Reference 'arrivalOrdinal'
    if ($null -ne $publishedKind) { $publishedKind = [string]$publishedKind }
    if ($null -ne $publishedSession) { $publishedSession = [string]$publishedSession }
    if ($null -ne $publishedIdentity) { $publishedIdentity = [string]$publishedIdentity }
    if ($null -ne $publishedEndpoint) { $publishedEndpoint = [string]$publishedEndpoint }
    if ($null -ne $publishedOrdinal) { $publishedOrdinal = [int]$publishedOrdinal }

    $candidates = [System.Collections.Generic.List[object]]::new()
    foreach ($step in $Steps) {
        if ($null -ne $publishedKind -and $step.Kind -ne $publishedKind) { continue }
        if ($null -ne $publishedSession -and $step.Session -ne $publishedSession) { continue }
        if ($null -ne $publishedIdentity -and $step.InteractionIdentity -ne $publishedIdentity) { continue }
        if ($null -ne $publishedEndpoint -and $step.CommittingEndpoint -ne $publishedEndpoint) { continue }
        if ($null -ne $publishedOrdinal -and ($null -eq $step.ArrivalOrdinal -or $step.ArrivalOrdinal -ne $publishedOrdinal)) { continue }
        [void]$candidates.Add($step)
    }

    return @($candidates)
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

    # BB1. Both reads go through the widening readers, which is what the comment above already said
    # this test does: a set publishing no session is not excluded by the session filter, and one
    # publishing no identities contributes none. Written raw they said it by accident.
    $sets = @($AdmittedSets)
    if ($null -ne $Session) {
        $sets = @($sets | Where-Object { [string](Get-Field $_ 'session') -eq [string]$Session })
    }

    # Flattened by an explicit loop rather than through the pipeline. `Get-List` returns `,@(...)`
    # so that an assignment gets a collection, and that wrapper is exactly what survives ONE
    # unrolling -- so a `ForEach-Object` emitting it yields the inner array as a single object, and
    # `[string]` on that renders every identity into one space-joined string that matches nothing.
    # It was caught here by AZ3's sweep going green where it declares red, which is the point of
    # having a measure whose inputs are not the ones the change was written against.
    $identities = [System.Collections.Generic.List[string]]::new()
    foreach ($set in $sets) {
        foreach ($setIdentity in (Get-List $set 'identities')) {
            if ($null -eq $setIdentity) { continue }
            [void]$identities.Add([string]$setIdentity)
        }
    }
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
    $observations = Read-Required $Vector 'observations' "vector '$VectorId'"

    $admitted = Get-List $observations 'recipientAdmittedIdentities'

    # Conjunct 1. No endpoint records a recipient `rejected-protocol` at `unseen` for a cancellation
    # control whose committing endpoint had already committed the request naming that identity and
    # whose recipient afterwards admits an interaction for that identity in the same session.
    foreach ($refusal in (Get-List $observations 'unseenRefusals')) {
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
    foreach ($latch in (Get-List $observations 'lateTrafficLatches')) {
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

# BB1. A `Read-Optional` reason two properties give for the same field is stated once, for the reason
# W1 states any fact once: the second copy is the one that goes stale while both gates stay green.
$refusalIsAbsence = 'an interaction that records no refusal was not refused, which is a fact the design states rather than a silence in the vector'
# BC1. This reason was always the sentence of an obligation and never of an optional -- "the violation
# being detected" is not a fact the design states -- and it moved to the reader that means it. C6-P1
# is the contract sentence it cites: every denial or unevaluatable presentation records the decision
# point, initiator attribution, and `known-none`.
$presentationIsOmission = 'authority presentation part C6-P1 requires every denial or unevaluatable presentation to record, so a presentation that omits it is the violation this clause detects'

# ---------------------------------------------------------------------------------------------
# The five sanctioned readers, and the rule that there are only five -- BB1, widened by BC1.
#
# Every field an evaluator reads off a vector record is read through one of these. A read written as
# `$record.field` is not one of them, and the read-provenance census at the bottom of this file fails
# on one, because a raw read is how a record the evaluator COULD NOT READ becomes indistinguishable
# from one that conforms. That census's own section says what it measured; the rule is stated here,
# beside the readers, because this is the file the rule binds.
#
# The five differ in what an ABSENT field means, and that difference is the whole of the taxonomy:
#
#   * `Get-List`, and `Get-Timeline`/`Get-Interactions`/`Get-Sessions` over it -- absent means EMPTY.
#     AU2's ruling, unchanged.
#   * `Read-Required` -- absent means the record cannot be evaluated, reported against the vector.
#   * `Read-Optional` -- absent is itself a fact the design states, and the call site says which.
#   * `Read-Obligation` -- absent is the VIOLATION the reading clause detects. BC1, and it is the one
#     `Read-Optional` was being used for where the contract says the field is always recorded.
#   * `Get-Field` -- absent WIDENS the candidate set rather than erroring. Closure review 16's P3
#     rule, and `C4-P2` is the only property whose operands are resolved that way.
#
# The two whose absence carries meaning are the two that can be WRONG, so each is checked against the
# declared verdict of the input that exercises it, and the two requirements are opposite: a
# `Read-Optional` needs an input the property is declared GREEN on to leave the field absent, and a
# `Read-Obligation` needs one it is declared RED on. Either declaration exercised only by the other
# polarity is a declaration the suite does not distinguish from a wrong one -- BB5's unit, which
# counted any absence at all, one level in.
# ---------------------------------------------------------------------------------------------

# AU2, and both halves are one defect: an obligation that fires on what a vector does not SAY reports
# the same red as one that fires on what a realization did wrong, so nothing distinguishes them.
#
# `@($null)` is a ONE-element array in PowerShell, not an empty one, so a collection the vector does
# not publish reads as a collection holding one null. `C11-P1` was red on every vector that publishes
# no `requiredFacets` -- with a blank where the facet name belongs in its own witness -- and two
# evaluators already carried a local `if ($null -eq $history) { continue }` for the same thing, which
# patches one reader and leaves every other read exposed. An unpublished collection is empty.
#
# BB1 moved the READ inside. `Get-List $interaction.terminalHistories` performed the read in the
# CALLER and handed this function a value, so the caller was the raw reader and this function only
# ever saw what had already been read -- a producer's channel rebuilt one level out. It takes the
# record and the field name now, exactly as `Read-Required` beside it does.
function Get-List {
    param($Record, [Parameter(Mandatory = $true)][string]$Field)

    # The unary comma is BA5's lesson, and it is load-bearing rather than stylistic: PowerShell
    # unrolls a returned collection, so `return @()` hands the caller `$null` and a one-element list
    # comes back as a scalar. BA5 was a count guard that skipped exactly that scalar and reported two
    # producers of seven while reading as a total. Every caller here takes a collection.
    if ($null -eq $Record) { return ,@() }
    $member = $Record.PSObject.Properties[$Field]
    if ($null -eq $member -or $null -eq $member.Value) { return ,@() }
    return ,@($member.Value)
}

# The three vector-level collections, each `Get-List` against the vector and named for what it holds.
function Get-Timeline { param($Vector) return (Get-List $Vector 'sessionTimeline') }
function Get-Interactions { param($Vector) return (Get-List $Vector 'interactions') }
function Get-Sessions { param($Vector) return (Get-List $Vector 'sessions') }

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

# BB1. The third reader, and the one that carries a reason.
#
# Not every absent field leaves a record unevaluable. An interaction that records no refusal has not
# been silent about its refusal; the design says an interaction need not have one, and `I4` reading
# `refusal` and finding nothing has learned a fact rather than lost one. Routing such a read through
# `Read-Required` would report every conforming interaction in the corpus, which is the false report
# AU2 was raised against, arriving from the other direction.
#
# So the distinction between the two is a JUDGEMENT, and this reader is where it is written down. The
# `-Because` is mandatory and is not decoration: the census below counts the distinct reasons and
# prints them with the measure, so an optional read is a declaration a reader can audit against the
# design rather than a way of spelling a raw read. That is the AZ1 lesson applied to this file's own
# new channel -- a channel nobody reads is one nobody can check.
#
# And the declaration is CHECKED rather than counted. A `Read-Optional` claims that some record the
# properties are run over does not publish the field; if every record publishes it, the claim is
# unfalsified and the read should be `Read-Required`. That is AU1's unit one more level out -- a
# declaration no declared input exercises is one nothing in the suite distinguishes from a wrong one
# -- and the check below is what makes writing the reason cost something.
#
# `$script:CensusPoisoning` is why the census cannot satisfy the check for free: the census makes
# every field absent by construction, so an absence observed under it proves nothing about the
# corpus and is not counted.
#
# BC1 is what that check could not ask. It counts an absence wherever one occurs, and an absence
# occurs on a declared MUTATION as readily as on a conforming input -- so a declaration exercised
# only by the very vector the reading property is declared red on satisfied it. `decisionPoint` and
# `initiatorAttribution` were two such, and C6-P1's own sentence says every denial RECORDS them: what
# their absence is, is the violation the clause detects, which is the opposite of what this reader
# declares. So the record is no longer a flag but the set of DECLARED VERDICTS the inputs that
# produced the absence carry, and the check below reads the polarity rather than the count.
$script:OptionalReads = @{}
$script:ObligationReads = @{}
$script:CensusPoisoning = $false

# BC1. The verdict the input now being evaluated is DECLARED to produce, or `$null` outside the
# declared-corpus dispatch -- which is the only one of this file's five whose inputs carry a stated
# expectation at all. A generated vector, an operand mutation and a dropped field have no declared
# verdict, and an absence observed under one of them is recorded as `undeclared` rather than guessed
# at. It is a scalar rather than a `$script:` collection deliberately: BA3's rule binds a collection
# that must be cleared and drained at every dispatch, and a value that means "no declared
# expectation" when unset does not accumulate.
$script:DeclaredExpectation = $null

# The polarity an absence observed right now carries. Both readers record the same value and differ
# only in which polarity makes their declaration true, so the value is computed once here and the
# six lines that store it are written out at each reader rather than shared.
#
# BC3 is why they are written out. The first draft passed the accumulator to one helper as a
# parameter, and the return-channel census -- a frozen instrument -- reported that
# `$script:OptionalReads` was declared and that nothing in the gate adds to it. It was right: the
# census recognises a producer by a write to the `$script:` name, and a collection reached through a
# parameter is BB3's own stated limit, "a write through an alias", arriving one pass after BB3 wrote
# it down. A helper that hides a declared channel from the instrument that checks the channel costs
# more than the duplication it saves.
function Get-AbsencePolarity {
    if ($script:DeclaredExpectation) { return [string]$script:DeclaredExpectation }
    return 'undeclared'
}
function Read-Optional {
    param($Record, [Parameter(Mandatory = $true)][string]$Field,
          [Parameter(Mandatory = $true)][string]$Because)

    $value = $null
    $member = if ($null -eq $Record) { $null } else { $Record.PSObject.Properties[$Field] }
    if ($null -ne $member) { $value = $member.Value }
    if (-not $script:CensusPoisoning) {
        $declaration = "'$Field': $Because"
        if (-not $script:OptionalReads.ContainsKey($declaration)) {
            $script:OptionalReads[$declaration] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }
        if ($null -eq $value) { [void]$script:OptionalReads[$declaration].Add((Get-AbsencePolarity)) }
    }
    return $value
}

# BC1. The fifth reader, and the one the four could not express.
#
# `C6-P1` reads the three parts of an authority presentation and its second clause is about a
# presentation that OMITS one of them. Reading a part through `Read-Required` reports the vector as
# silent on exactly the field the mutation removes -- AU2 pointed at its own detector -- so the two
# were written as `Read-Optional`, which says the absence is a fact the design STATES. It is not:
# C6-P1's own sentence is that every denial or unevaluatable presentation records the decision point,
# initiator attribution, and `known-none`, so an absent part is the violation being detected. A
# reader that declares the opposite of the contract it serves is a raw read with a reason attached,
# and the polarity check below is what makes that visible instead of plausible.
#
# What this reader means: the field's PRESENCE is an obligation the reading clause enforces, and its
# absence is that clause's own red rather than a fact about the vector. Its falsification requirement
# is therefore the MIRROR of `Read-Optional`'s -- some input the property is declared RED on must
# leave the field absent, or nothing in the suite demonstrates the clause catches the omission.
#
# WHAT IT DOES NOT FIX, STATED HERE BECAUSE IT IS THE HALF A READER WILL LOOK FOR. A vector that
# omits the field because it models a realization that omitted it, and a vector that omits it because
# its author did not write it down, are still the same bytes. This reader names that rather than
# closing it; closing it needs the vector to state the omission POSITIVELY, which changes what a
# conforming authority record must carry and is an owner question rather than an author's. It is
# recorded as this pass's open question.
function Read-Obligation {
    param($Record, [Parameter(Mandatory = $true)][string]$Field,
          [Parameter(Mandatory = $true)][string]$Because)

    $value = $null
    $member = if ($null -eq $Record) { $null } else { $Record.PSObject.Properties[$Field] }
    if ($null -ne $member) { $value = $member.Value }
    if (-not $script:CensusPoisoning) {
        $declaration = "'$Field': $Because"
        if (-not $script:ObligationReads.ContainsKey($declaration)) {
            $script:ObligationReads[$declaration] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }
        if ($null -eq $value) { [void]$script:ObligationReads[$declaration].Add((Get-AbsencePolarity)) }
    }
    return $value
}
# BB1. The fourth reader, for an operand that is a whole RECORD rather than a field of one.
#
# `S5` compares the profile record two establishment routes produce. The operand is the record, not
# any named field of it -- naming the fields here would make this file a second surface for the
# profile's shape, which is the duplication W1 exists to retire -- so the comparison is over a
# rendering of the whole subtree.
#
# That is what the census found, and the finding is real rather than a labelling problem. A leaf the
# vector does not publish renders as `null`, the two renderings differ, and `S5` reports "produces
# different normative profile records" -- a violation it cannot substantiate, which is AU2's half
# that fires on what the vector did not SAY. So the rendering is performed here, where the leaf reads
# happen inside a named reader, and a `null` in it is reported as an unreadable record instead of
# being allowed to become a verdict. A normative profile record carries no nulls; one in the
# rendering is a leaf the vector left out.
function Read-Rendering {
    param($Record, [Parameter(Mandatory = $true)][string]$Field, [Parameter(Mandatory = $true)][string]$Subject)

    $value = Read-Required $Record $Field $Subject
    if ($null -eq $value) { return $null }
    $rendering = ($value | ConvertTo-Json -Depth 12 -Compress)
    if ($rendering -match '(:|,|\[)null(,|\}|\])') {
        [void]$script:UnpublishedFields.Add("$Subject renders '$Field' with a null leaf, so the whole-record comparison cannot tell an unpublished leaf from a genuine difference")
    }
    return $rendering
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
# BA1. `-Inherited` carries forward what a DELEGATE reported through its own `Errors`. Three
# properties evaluate a clause by calling another property's evaluator -- `C4-P1` delegates two
# clauses to `I1` and `I5`, `C2-P1` two to `S1` and `S4`, `C8-P1` two to `I2` and `I3` -- and each
# then returns a record built here, which until this pass constructed a FRESH empty collection. So a
# delegate saying "I could not be evaluated over this record at all" was not merely unread by the
# composed property: it was destroyed before any consumer could see it, and the composed property
# reported green over a record nobody could read.
#
# That is AZ1 one level below the loop AZ1 was raised against, and worse in kind. A loop that does
# not drain a channel can be made to drain it; a producer that rebuilds the channel empty has thrown
# the contents away first. It was demonstrated rather than argued: made to report an evaluation error
# on every input, `I1` surfaced it on all four of its own declared inputs and on **none** of `C4-P1`'s
# six. The declared corpus runs up to thirty-four delegated evaluations and the generated population up
# to six hundred more -- upper bounds rather than counts, because a composed evaluator returns before
# reaching its second delegate when the first clause fires, which is why the injection reached four of
# those six and not six.
$script:ObligationsReached = [System.Collections.Generic.HashSet[int]]::new()
function New-Red {
    param([string]$Witness, [string]$Conjunct, [AllowEmptyCollection()][string[]]$Inherited = @())
    [void]$script:ObligationsReached.Add((Get-PSCallStack)[1].ScriptLineNumber)
    $record = [pscustomobject]@{ Verdict = 'red'; Conjunct = $Conjunct; Witness = $Witness; Errors = [System.Collections.Generic.List[string]]::new() }
    foreach ($inheritedError in $Inherited) {
        [void]$record.Errors.Add($inheritedError)
    }
    return $record
}
function New-Green {
    param([AllowEmptyCollection()][string[]]$Inherited = @())
    $record = [pscustomobject]@{ Verdict = 'green'; Conjunct = $null; Witness = $null; Errors = [System.Collections.Generic.List[string]]::new() }
    foreach ($inheritedError in $Inherited) {
        [void]$record.Errors.Add($inheritedError)
    }
    return $record
}

function Invoke-S1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string](Read-Required $sessionEvent 'step' $timelineSubject) -ne 'transition') { continue }
        if (-not (Read-Required $sessionEvent 'accepted' $timelineSubject)) { continue }
        $edge = "$(Read-Required $sessionEvent 'from' $timelineSubject)>$(Read-Required $sessionEvent 'to' $timelineSubject)"
        if ($legalSessionTransitions -notcontains $edge) {
            return New-Red "session $(Read-Required $sessionEvent 'session' $timelineSubject) accepted the transition $edge on event $(Read-Required $sessionEvent 'event' $timelineSubject), which the legal table does not contain"
        }
    }
    return New-Green
}

function Invoke-S2 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $state = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $step = [string](Read-Required $sessionEvent 'step' $timelineSubject)
        if ($step -eq 'transition') {
            if (Read-Required $sessionEvent 'accepted' $timelineSubject) { $state[$sessionId] = [string](Read-Required $sessionEvent 'to' $timelineSubject) }
            continue
        }
        if ($step -ne 'dispatch') { continue }
        $current = if ($state.ContainsKey($sessionId)) { $state[$sessionId] } else { 'unestablished' }
        if ($current -ne 'established') {
            return New-Red "interaction $(Read-Required $sessionEvent 'identity' $timelineSubject) dispatched while its own session $sessionId was $current"
        }
    }
    return New-Green
}

function Invoke-S3 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Per session. A second session establishing and admitting after the first drains is legal, and
    # reading the drain across the vector is exactly the false red AL1 found.
    $drained = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $step = [string](Read-Required $sessionEvent 'step' $timelineSubject)
        if ($step -eq 'transition') {
            if ((Read-Required $sessionEvent 'accepted' $timelineSubject) -and
                [string](Read-Required $sessionEvent 'to' $timelineSubject) -eq 'draining') {
                if (-not $drained.ContainsKey($sessionId)) { $drained[$sessionId] = $true }
            }
            continue
        }
        if ($step -eq 'admit' -and $drained.ContainsKey($sessionId)) {
            return New-Red "session $sessionId admitted interaction $(Read-Required $sessionEvent 'identity' $timelineSubject) after its own first drain transition"
        }
    }
    return New-Green
}

function Invoke-S4 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $terminal = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string](Read-Required $sessionEvent 'step' $timelineSubject) -ne 'transition') { continue }
        if (-not (Read-Required $sessionEvent 'accepted' $timelineSubject)) { continue }
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $to = [string](Read-Required $sessionEvent 'to' $timelineSubject)
        if ($terminal.ContainsKey($sessionId)) {
            return New-Red "session $sessionId reached terminal state $($terminal[$sessionId]) and then transitioned to $to under the same session identity"
        }
        if ($terminalSessionStates -contains $to) { $terminal[$sessionId] = $to }
    }
    return New-Green
}

function Invoke-S5 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # For EACH session, over that session own declared profile. Two sessions carrying two different
    # declared profiles are conforming and this property says nothing about them, which is AL4.
    foreach ($session in (Get-Sessions $Vector)) {
        $record = Read-Required $session 'establishedProfileRecord' 'a session record'
        if ($null -eq $record) { continue }
        $recordSubject = 'a session established-profile record'
        $fixed = Read-Rendering $record 'fixed' $recordSubject
        $negotiated = Read-Rendering $record 'negotiated' $recordSubject
        if ($fixed -cne $negotiated) {
            return New-Red "session $(Read-Required $session 'id' 'a session record') produces different normative profile records from fixed and negotiated establishment of its own declared profile"
        }
    }
    return New-Green
}

function Invoke-S6 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $forbidden = @('ready', 'release', 'authority', 'application-outcome')
    foreach ($declaredEvent in (Get-List $Vector 'sessionEvents')) {
        if ($null -eq $declaredEvent) { continue }
        $eventSubject = 'a declared session event'
        foreach ($created in (Get-List $declaredEvent 'creates')) {
            if ($forbidden -contains [string]$created) {
                return New-Red "session event $(Read-Required $declaredEvent 'event' $eventSubject) in session $(Read-Required $declaredEvent 'session' $eventSubject) creates $created"
            }
        }
    }
    return New-Green
}

function Invoke-I1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Per session: one identity may legitimately be dispatched in each of two sessions.
    $seen = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string](Read-Required $sessionEvent 'step' $timelineSubject) -ne 'dispatch') { continue }
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $identity = [string](Read-Required $sessionEvent 'identity' $timelineSubject)
        $key = "$sessionId|$identity"
        if ($seen.ContainsKey($key)) {
            return New-Red "identity $identity crossed the dispatch boundary twice in session $sessionId"
        }
        $seen[$key] = $true
    }
    return New-Green
}

function Invoke-I2 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $histories = Get-List $interaction 'terminalHistories'
        if ($histories.Count -gt 1) {
            return New-Red "interaction $(Read-Required $interaction 'identity' $interactionSubject) in session $(Read-Required $interaction 'session' $interactionSubject) has $($histories.Count) terminal histories"
        }
    }
    return New-Green
}

function Invoke-I3 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $nonSemantic = @('cancellation-acknowledgement', 'drain', 'timeout', 'protocol-fault')
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        foreach ($history in (Get-List $interaction 'terminalHistories')) {
            if ($null -eq $history) { continue }
            $historySubject = 'an interaction terminal history'
            $form = [string](Read-Required $history 'form' $historySubject)
            if (($nonSemantic -contains $form) -and (Read-Required $history 'semanticSuccess' $historySubject)) {
                return New-Red "interaction $(Read-Required $interaction 'identity' $interactionSubject) records a $form terminal as a semantic success"
            }
        }
    }
    return New-Green
}

function Invoke-I4 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $refusal = Read-Optional $interaction 'refusal' $refusalIsAbsence
        if ($null -eq $refusal) { continue }
        $refusalSubject = 'an interaction refusal record'
        $stage = [string](Read-Required $refusal 'stage' $refusalSubject)
        $certainty = [string](Read-Required $refusal 'effectCertainty' $refusalSubject)
        # AT1: the two clauses are named, so a mutation cannot fire through the one it was not written
        # for. Until the operand measure reached it, nothing carried a pre-dispatch refusal into this
        # property's group at all and the first clause was deleteable with both gates green -- AR1's
        # finding on C5-P1, which declares conjuncts, on a property that did not.
        if ($stage -eq 'pre-dispatch' -and $certainty -ne 'known-none') {
            return New-Red "interaction $(Read-Required $interaction 'identity' $interactionSubject) records a pre-dispatch refusal with effect certainty $certainty" 'I4-clause-1'
        }
        if ($stage -eq 'post-dispatch' -and $certainty -ne 'unknown' -and
            -not (Read-Required $refusal 'explicitEvidence' $refusalSubject)) {
            return New-Red "interaction $(Read-Required $interaction 'identity' $interactionSubject) records a possible post-dispatch loss as $certainty with no explicit evidence narrowing it" 'I4-clause-2'
        }
    }
    return New-Green
}

function Invoke-I5 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Concurrency is counted per session against THAT session bound, which is AK7. Counted across the
    # vector, two sessions each holding one nonterminal interaction breach a bound neither did.
    $bounds = @{}
    $sessionSubject = 'a session record'
    foreach ($session in (Get-Sessions $Vector)) {
        $bounds[[string](Read-Required $session 'id' $sessionSubject)] = [int](Read-Required $session 'establishedBound' $sessionSubject)
    }
    $live = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $step = [string](Read-Required $sessionEvent 'step' $timelineSubject)
        if (-not $live.ContainsKey($sessionId)) { $live[$sessionId] = 0 }
        if ($step -eq 'admit') { $live[$sessionId]++ }
        elseif ($step -eq 'terminal' -and (Read-Required $sessionEvent 'accepted' $timelineSubject)) { $live[$sessionId] = [Math]::Max(0, $live[$sessionId] - (Get-List $sessionEvent 'closes').Count) }
        if ($bounds.ContainsKey($sessionId) -and $live[$sessionId] -gt $bounds[$sessionId]) {
            return New-Red "session $sessionId held $($live[$sessionId]) nonterminal interactions against its own established bound of $($bounds[$sessionId])"
        }
    }
    return New-Green
}

function Invoke-I6 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ([string](Read-Required $interaction 'class' $interactionSubject) -ne 'relational') { continue }
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        $subject = "interaction $identity in session $(Read-Required $interaction 'session' $interactionSubject)"
        $matches = [int](Read-Required $interaction 'declarationMatches' $subject)
        if ($matches -ne 1) {
            return New-Red "relational interaction $identity matches $matches declarations"
        }
        if (Read-Required $interaction 'createsReadyOrRelease' $subject) {
            return New-Red "relational interaction $identity creates Ready or Release"
        }
    }
    return New-Green
}

function Invoke-I7 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $changedBy = [string](Read-Optional $interaction 'terminalHistoryChangedBy' 'an interaction whose terminal history no sibling changed does not record a changer, and this property is about the interactions that do')
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        if ($changedBy -and $changedBy -ne $identity) {
            return New-Red "interaction $identity had its terminal history changed by sibling $changedBy"
        }
    }
    return New-Green
}

function Invoke-C4P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Three clauses, each session-scoped under AK7. The second and third are the same claims I1 and I5
    # make, so they are evaluated by those functions rather than restated here: two implementations of
    # one claim is the duplication W1 exists to retire, arriving in the gate instead of in the prose.
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string](Read-Required $sessionEvent 'step' $timelineSubject) -ne 'terminal') { continue }
        if (-not (Read-Required $sessionEvent 'accepted' $timelineSubject)) { continue }
        $closes = Get-List $sessionEvent 'closes'
        if ($closes.Count -ne 1) {
            return New-Red "an accepted terminal fact in session $(Read-Required $sessionEvent 'session' $timelineSubject) closes $($closes.Count) admitted interactions"
        }
    }
    # BA1. Everything the delegate hands back is read: its verdict, its witness, the conjunct its red
    # arrived through, and the `Errors` through which it says it could not be evaluated at all. The
    # last of those is the one that was being destroyed; the conjunct is inert until a delegate names
    # one, and is read now so that a delegate which starts naming them does not lose it silently.
    $dispatchResult = Invoke-I1 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($dispatchResult.Verdict -eq 'red') {
        return New-Red -Witness "$($dispatchResult.Witness)$(if ($dispatchResult.Conjunct) { " through $($dispatchResult.Conjunct)" }), which the second clause of C4-P1 forbids" -Inherited $dispatchResult.Errors
    }
    $boundResult = Invoke-I5 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($boundResult.Verdict -eq 'red') {
        return New-Red -Witness "$($boundResult.Witness)$(if ($boundResult.Conjunct) { " through $($boundResult.Conjunct)" }), which the third clause of C4-P1 forbids" -Inherited $boundResult.Errors
    }
    return New-Green -Inherited (@($dispatchResult.Errors) + @($boundResult.Errors))
}

# The session/identity keys of every dispatch the timeline records. Four capability properties index
# their interactions by it and each had written the loop out; BB1 read all four while routing their
# reads, and four copies of one index is the duplication W1 retires wherever it is found.
function Get-DispatchedKeys {
    param($Vector)

    $dispatched = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        if ([string](Read-Required $sessionEvent 'step' $timelineSubject) -ne 'dispatch') { continue }
        $dispatched["$(Read-Required $sessionEvent 'session' $timelineSubject)|$(Read-Required $sessionEvent 'identity' $timelineSubject)"] = $true
    }
    return $dispatched
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
    $sessionSubject = 'a session record'
    foreach ($session in (Get-Sessions $Vector)) {
        $exact = ([int](Read-Required $session 'establishedProfiles' $sessionSubject) -eq 1) -and
            (Read-Required $session 'profileFactsMatchExpected' $sessionSubject)
        if ($exact) { continue }
        if (Read-Required $session 'dispatchable' $sessionSubject) {
            return New-Red "session $(Read-Required $session 'id' $sessionSubject) has no established profile equal to the profile it expects, and interactions remain dispatchable"
        }
    }
    return New-Green
}

function Invoke-C2P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # BA1, as in C4-P1 above: the delegate's `Errors` are carried forward rather than rebuilt empty.
    $tableResult = Invoke-S1 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($tableResult.Verdict -eq 'red') {
        return New-Red -Witness "$($tableResult.Witness)$(if ($tableResult.Conjunct) { " through $($tableResult.Conjunct)" }), which the first clause of C2-P1 forbids" -Inherited $tableResult.Errors
    }
    $monotonic = Invoke-S4 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($monotonic.Verdict -eq 'red') {
        return New-Red -Witness "$($monotonic.Witness)$(if ($monotonic.Conjunct) { " through $($monotonic.Conjunct)" }), which the third clause of C2-P1 forbids" -Inherited $monotonic.Errors
    }
    # Both delegates ran green and either may still have reported that it could not be evaluated over
    # a record. That travels with every verdict this function can now return, including its own.
    $inheritedErrors = @($tableResult.Errors) + @($monotonic.Errors)
    # The middle clause: any other input leaves the prior state unchanged or enters faulted. An input
    # recorded as an accepted transition that the table does not contain is caught above; an admission
    # recorded as accepted outside established is caught here.
    $state = @{}
    $timelineSubject = 'a session-timeline event'
    foreach ($sessionEvent in (Get-Timeline $Vector)) {
        $sessionId = [string](Read-Required $sessionEvent 'session' $timelineSubject)
        $step = [string](Read-Required $sessionEvent 'step' $timelineSubject)
        if ($step -eq 'transition') {
            if (Read-Required $sessionEvent 'accepted' $timelineSubject) { $state[$sessionId] = [string](Read-Required $sessionEvent 'to' $timelineSubject) }
            continue
        }
        if ($step -ne 'admit') { continue }
        $current = if ($state.ContainsKey($sessionId)) { $state[$sessionId] } else { 'unestablished' }
        if ($current -ne 'established') {
            return New-Red -Witness "session $sessionId accepted a new interaction while it was $current, so an input that must leave the state unchanged or enter faulted admitted instead" -Inherited $inheritedErrors
        }
    }
    return New-Green -Inherited $inheritedErrors
}

function Invoke-C3P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = Get-DispatchedKeys $Vector
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        $sessionId = [string](Read-Required $interaction 'session' $interactionSubject)
        if (-not $dispatched.ContainsKey("$sessionId|$identity")) { continue }
        $subject = "interaction $identity in session $sessionId"
        if (-not (Read-Required $interaction 'profileMatch' $subject)) {
            return New-Red "interaction $identity dispatched without its class and direction matching the established profile of session $sessionId"
        }
        # false and unknown both refuse admission: only an exact true satisfies the predicate. Absent
        # is neither: a vector that does not publish the predicate has not stated an unknown one.
        $predicate = Read-Required $interaction 'phasePredicate' $subject
        if ($predicate -isnot [bool] -or -not $predicate) {
            return New-Red "interaction $identity dispatched with external phase predicate $predicate, and only an exact true matches"
        }
    }
    return New-Green
}

function Invoke-C5P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = Get-DispatchedKeys $Vector
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        $sessionId = [string](Read-Required $interaction 'session' $interactionSubject)
        if ($dispatched.ContainsKey("$sessionId|$identity")) {
            $subject = "interaction $identity in session $sessionId"
            if (-not (Read-Required $interaction 'boundsChecked' $subject)) {
                return New-Red "interaction $identity dispatched without passing every declared bound" 'C5-P1-clause-1'
            }
            if (-not (Read-Required $interaction 'positionalShapeChecked' $subject)) {
                return New-Red "interaction $identity dispatched without passing every positional Shape rule" 'C5-P1-clause-1'
            }
        }
        $refusal = Read-Optional $interaction 'refusal' $refusalIsAbsence
        if ($null -eq $refusal) { continue }
        $refusalSubject = 'an interaction refusal record'
        if ([string](Read-Required $refusal 'stage' $refusalSubject) -ne 'pre-dispatch') { continue }
        $certainty = [string](Read-Required $refusal 'effectCertainty' $refusalSubject)
        if ($certainty -ne 'known-none') {
            return New-Red "interaction $identity records a pre-dispatch structural refusal with effect certainty $certainty" 'C5-P1-clause-2'
        }
    }
    return New-Green
}

function Invoke-C6P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = Get-DispatchedKeys $Vector
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        $sessionId = [string](Read-Required $interaction 'session' $interactionSubject)
        $subject = "interaction $identity in session $sessionId"
        $decision = [string](Read-Required $interaction 'authorityDecision' $subject)
        if ($dispatched.ContainsKey("$sessionId|$identity") -and $decision -ne 'permitted') {
            return New-Red "interaction $identity reached handler dispatch with local authority decision $decision" 'C6-P1-clause-1'
        }
        if ($decision -eq 'permitted') { continue }
        # The second clause is about a presentation that OMITS one of the three, so each part is read
        # through the reader whose absence IS that omission. Reading them as required would report the
        # vector as silent on exactly the fields the mutation removes, which is AU2 pointed at its own
        # detector; reading them as optional declared the absence a fact the design states, which is
        # what this property's own sentence denies. BC1.
        $record = Read-Required $interaction 'authorityRecord' $subject
        if ($null -eq $record -or
            -not (Read-Obligation $record 'decisionPoint' $presentationIsOmission) -or
            -not (Read-Obligation $record 'initiatorAttribution' $presentationIsOmission) -or
            [string](Read-Required $record 'effectCertainty' 'an interaction authority record') -ne 'known-none') {
            return New-Red "interaction $identity records a $decision authority presentation without its decision point, initiator attribution, and known-none" 'C6-P1-clause-2'
        }
    }
    return New-Green
}

function Invoke-C7P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $dispatched = Get-DispatchedKeys $Vector
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        if ([string](Read-Required $interaction 'class' $interactionSubject) -ne 'relational') { continue }
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        $sessionId = [string](Read-Required $interaction 'session' $interactionSubject)
        if (-not $dispatched.ContainsKey("$sessionId|$identity")) { continue }
        $subject = "interaction $identity in session $sessionId"
        $matches = [int](Read-Required $interaction 'declarationMatches' $subject)
        if ($matches -ne 1) {
            return New-Red "dispatched relational interaction $identity matches $matches lifecycle declarations"
        }
        if (-not (Read-Required $interaction 'inPreReadyWindow' $subject)) {
            return New-Red "dispatched relational interaction $identity does not occur in the pre-Ready window"
        }
        if (Read-Required $interaction 'createsReadyOrRelease' $subject) {
            return New-Red "dispatched relational interaction $identity produces a Ready or Release fact by itself"
        }
    }
    return New-Green
}

function Invoke-C8P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # BA1, as in C4-P1 and C2-P1 above.
    $singleTerminal = Invoke-I2 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($singleTerminal.Verdict -eq 'red') {
        return New-Red -Witness "$($singleTerminal.Witness)$(if ($singleTerminal.Conjunct) { " through $($singleTerminal.Conjunct)" }), which the first clause of C8-P1 forbids" -Inherited $singleTerminal.Errors
    }
    $notSuccess = Invoke-I3 -VectorId $VectorId -Vector $Vector -Steps $Steps
    if ($notSuccess.Verdict -eq 'red') {
        return New-Red -Witness "$($notSuccess.Witness)$(if ($notSuccess.Conjunct) { " through $($notSuccess.Conjunct)" }), which the second clause of C8-P1 forbids" -Inherited $notSuccess.Errors
    }
    return New-Green -Inherited (@($singleTerminal.Errors) + @($notSuccess.Errors))
}

function Invoke-C9P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $form = [string](Read-Required $interaction 'provenanceForm' $interactionSubject)
        if (-not $form) { continue }
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        if ($provenanceForms -notcontains $form) {
            return New-Red "interaction $identity selects provenance form $form, which is not one of the four"
        }
        # The second clause: no field permits a local inference to be accepted as a peer statement.
        # The vector states what the observation actually was where the two differ, and a recorded form
        # that is not the actual one is exactly that acceptance.
        $actual = [string](Read-Optional $interaction 'provenanceFormActually' 'the vector states what an observation ACTUALLY was only where that differs from the recorded form, so an absent field is agreement and not silence')
        if ($actual -and $actual -ne $form) {
            return New-Red "interaction $identity records provenance form $form for what was actually a $actual"
        }
    }
    return New-Green
}

function Invoke-C10P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $interactionSubject = 'an interaction record'
    foreach ($interaction in (Get-Interactions $Vector)) {
        $identity = [string](Read-Required $interaction 'identity' $interactionSubject)
        if (-not (Read-Required $interaction 'observationComplete' $interactionSubject)) {
            return New-Red "interaction $identity records an observation that is not complete for its provenance form"
        }
        if (-not (Read-Required $interaction 'possiblePostDispatchPath' $interactionSubject)) { continue }
        $refusal = Read-Optional $interaction 'refusal' $refusalIsAbsence
        if ($null -ne $refusal -and
            [string](Read-Required $refusal 'effectCertainty' 'an interaction refusal record') -eq 'known-none' -and
            -not (Read-Required $refusal 'explicitEvidence' 'an interaction refusal record')) {
            return New-Red "interaction $identity has a possible post-dispatch path and records known-none with no explicit evidence that the handler did not begin"
        }
        foreach ($history in (Get-List $interaction 'terminalHistories')) {
            if ($null -eq $history) { continue }
            if ([string](Read-Required $history 'effectCertainty' 'an interaction terminal history') -eq 'known-none' -and
                -not (Read-Required $history 'explicitEvidence' 'an interaction terminal history')) {
                return New-Red "interaction $identity has a possible post-dispatch path and records a known-none terminal history with no explicit evidence that the handler did not begin"
            }
        }
    }
    return New-Green
}

function Invoke-C11P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    $sessionSubject = 'a session record'
    foreach ($session in (Get-Sessions $Vector)) {
        foreach ($required in (Get-List $session 'requiredFacets')) {
            if ((Get-List $session 'supportedFacets') -notcontains [string]$required) {
                return New-Red "session $(Read-Required $session 'id' $sessionSubject) requires facet $required and its established profile does not support it"
            }
        }
        if (Read-Required $session 'facetChangesCore' $sessionSubject) {
            return New-Red "session $(Read-Required $session 'id' $sessionSubject) has a facet that changes a core identity, authority, terminal-provenance, or uncertainty result"
        }
    }
    return New-Green
}

function Invoke-C12P1 {
    param([string]$VectorId, $Vector, [object[]]$Steps)
    # Only the first clause is per vector. The second is over the declaration set and is evaluated once
    # below; the third is a dependency fact no vector carries and is enforced by the repository guards.
    if (-not (Read-Required $Vector 'deterministicExpectedObservation' "vector '$VectorId'")) {
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
    foreach ($fromState in $fromStates) {
        $artifactEdgeList.Add("$fromState>$toState")
    }
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
    #
    # `Undo` is optional and does not change what this returns. A caller that supplies a list gets one
    # entry per field actually removed, and `Restore-PublishedFields` puts them back -- which is how
    # AZ3's sweep drops eighteen fields per generated vector without deep-copying the vector eighteen
    # times. The alternative measured at nineteen milliseconds a copy, which is most of an hour across
    # the deep run and enough to change what the guard corpus costs to run at all.
    param(
        [Parameter(Mandatory = $true)]$Vector,
        [Parameter(Mandatory = $true)][string]$DropPath,
        [System.Collections.Generic.List[object]]$Undo)

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
            if ($null -ne $Undo) {
                [void]$Undo.Add([pscustomobject]@{ Parent = $parent; Name = $tail[-1]; Value = $parent.PSObject.Properties[$tail[-1]].Value })
            }
            $parent.PSObject.Properties.Remove($tail[-1])
            $removed++
        }
    }
    return $removed
}

function Restore-PublishedFields {
    # Puts back exactly what one `Remove-PublishedField` call took out. Property ORDER is not
    # preserved -- a restored field is appended -- and nothing in this file reads a record's property
    # order, only its property names through `Get-Field`.
    param([Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Undo)

    for ($index = $Undo.Count - 1; $index -ge 0; $index--) {
        $entry = $Undo[$index]
        # `Add-Member` rather than this costs about a millisecond a call, and the sweep makes tens of
        # thousands of them on the deep run.
        $entry.Parent.PSObject.Properties.Add([System.Management.Automation.PSNoteProperty]::new($entry.Name, $entry.Value))
    }
    $Undo.Clear()
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
    foreach ($member in $property.requiredGreen) {
        $expectations[[string]$member.vector] = @{ Verdict = 'green'; Conjunct = $null; Role = 'required-green' }
    }
    foreach ($member in $property.additionalGreen) {
        $expectations[[string]$member.vector] = @{ Verdict = 'green'; Conjunct = $null; Role = 'additional-green' }
    }
    foreach ($mutation in $property.namedMutations) {
        $expectations[[string]$mutation.vector] = @{ Verdict = [string]$mutation.expected; Conjunct = [string]$mutation.conjunct; Role = 'named-mutation' }
    }

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
        # BC1. The declared verdict is in scope for exactly this evaluation, so an absence the two
        # meaning-carrying readers observe is attributed to an input whose expectation is stated. It
        # is cleared in a `finally` rather than after the call: an evaluator that throws would
        # otherwise leave the next dispatch attributing its absences to this vector's verdict.
        $script:DeclaredExpectation = [string]$expected.Verdict
        try { $result = & $evaluator -VectorId $vectorId -Vector $vector -Steps $vectorIndex[$vectorId] }
        finally { $script:DeclaredExpectation = $null }
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
        foreach ($evaluationError in $result.Errors) {
            $failures.Add($evaluationError)
        }

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
        # BA3. The one input class this file builds by REMOVING fields is the one whose silence was
        # never checked. AU2's rule is that a field the obligation read and the vector does not
        # publish makes the verdict evidence of neither conformance nor violation, and the declared
        # and generated loops both clear this before the call and drain it after; the operand harness
        # did neither, so a mutation could report a verdict produced by silence and be believed.
        $script:UnpublishedFields.Clear()
        $mutatedResult = & $evaluator -VectorId $vectorId -Vector $mutated -Steps $mutatedSteps
        $mutationCount++
        foreach ($unpublished in ($script:UnpublishedFields | Sort-Object -Unique)) {
            $failures.Add("Operand mutation '$($operandMutation.id)' leaves '$propertyId' reading a field '$vectorId' then does not publish: $unpublished. The mutation's verdict is then produced by the vector's silence rather than by the operand it names, so it pins nothing either way.")
        }
        # BA2, and it is AZ1 at the harness AZ1's own correction did not reach. This block read the
        # verdict and nothing else. A drop that leaves the record unevaluable -- a whole reference
        # rather than one of its fields -- comes back `green` beside a non-empty `Errors`, because a
        # record the property could not read produces no witness, so a mutation declared green passed
        # by never having been evaluated. Demonstrated: dropping `unseen-refusals.refusedFrame` on
        # `C4-two-sessions-one-identity` and declaring it green was accepted, and the gate reported
        # ten operand mutations.
        foreach ($evaluationError in $mutatedResult.Errors) {
            $failures.Add("Operand mutation '$($operandMutation.id)' leaves '$propertyId' unable to be evaluated on '$vectorId': $evaluationError A mutation whose record the property cannot read reports the constructor's own verdict rather than a judgement, so it is neither a fire nor evidence that the field is redundant.")
        }
        if ($mutatedResult.Verdict -ne [string]$operandMutation.mutated) {
            $detail = ''
            if ($mutatedResult.Verdict -eq 'red') { $detail = " Witness: $($mutatedResult.Witness)." }
            $failures.Add("Operand mutation '$($operandMutation.id)' leaves '$propertyId' $($mutatedResult.Verdict) on '$vectorId' and is declared to leave it $($operandMutation.mutated).$detail")
        }
        # BA2's second half. The declared-input loop requires a red to arrive through the conjunct its
        # mutation is declared against, and AZ3's sweep requires the same of a dropped field, both for
        # the reason each states: a red arriving through the other conjunct witnesses something other
        # than the operand it names. This harness is the third place a declared red is checked and was
        # the only one not asking.
        elseif ([string]$operandMutation.mutated -ceq 'red') {
            if (-not $operandMutation.conjunct) {
                $failures.Add("Operand mutation '$($operandMutation.id)' is declared to leave '$propertyId' red and names no conjunct. Without one it asserts that something went red and not that the operand it reverts is what the property read, which is the state the declared-input loop and the dropped-field sweep are both written against.")
            }
            elseif ([string]$mutatedResult.Conjunct -cne [string]$operandMutation.conjunct) {
                $failures.Add("Operand mutation '$($operandMutation.id)' leaves '$propertyId' red on '$vectorId' through '$($mutatedResult.Conjunct)' and is declared against '$($operandMutation.conjunct)'. A red arriving through the other conjunct witnesses something other than the operand this mutation reverts.")
            }
        }

        $script:UnpublishedFields.Clear()
        $publishedResult = & $evaluator -VectorId $vectorId -Vector $vectorsById[$vectorId] -Steps $mutatedSteps
        foreach ($unpublished in ($script:UnpublishedFields | Sort-Object -Unique)) {
            $failures.Add("Operand mutation '$($operandMutation.id)' compares against a published form in which '$propertyId' reads a field '$vectorId' does not publish: $unpublished. The baseline the mutation is measured against is then produced by silence too.")
        }
        foreach ($evaluationError in $publishedResult.Errors) {
            $failures.Add("Operand mutation '$($operandMutation.id)' cannot evaluate '$propertyId' over the PUBLISHED form of '$vectorId': $evaluationError The baseline a mutation is measured against must itself be a judgement.")
        }
        if ($publishedResult.Verdict -ne [string]$operandMutation.published) {
            $detail = ''
            if ($publishedResult.Verdict -eq 'red') { $detail = " Witness: $($publishedResult.Witness), through '$($publishedResult.Conjunct)'." }
            $failures.Add("Operand mutation '$($operandMutation.id)' records the published verdict on '$vectorId' as $($operandMutation.published) and the published form evaluates $($publishedResult.Verdict).$detail")
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
# BB1: the read-provenance census. Every field an evaluator reads off a vector goes through one of
# the four sanctioned readers, and this is what says so.
#
# WHAT IT IS FOR. `Errors` is how an evaluator says it could not be evaluated over a record. AZ1
# found the generated loop discarding that channel, BA1 found three composed evaluators rebuilding it
# empty, and both corrections made the channel REACH its consumers. Neither asked the question one
# level in, which the fifteenth pass named and left: nothing requires an evaluator to FILL it. One
# evaluator of twenty-six populated it at all, so for the other twenty-five "I could not read this
# record" and "this realization conforms" were the same verdict, and no instrument here could tell
# them apart.
#
# HOW IT MEASURES. Each field of each vector is replaced, one at a time, by a property whose getter
# records that it was read and returns nothing -- the record still has the field, and the field has
# no value. The evaluator is then run. A getter that never fired says the evaluator does not read
# that field, and the site is not the census's business. A getter that fired says the evaluator read
# a field it could not read, and the census asks one question about it: WHICH READER performed the
# read, taken off the call stack rather than inferred from the outcome.
#
# WHY THE READER AND NOT THE OUTCOME. The outcome is the demonstration and the reader is the rule.
# Classifying by outcome would pass a raw read whose absence happens not to move this vector's
# verdict, which is the same mistake as measuring a guard by whether today's corpus makes it fire --
# AP1's class, and the reason the coverage measure counts conditions rather than failures. So the
# census fails on a raw read and reports what that raw read DID as the evidence a reader can act on:
# `silent` where the verdict stayed and nothing was reported, which is the vacuous pass this whole
# section exists to end, and `moved` where the verdict changed with nothing reported, which is AU2 --
# a property reporting a violation it cannot substantiate.
#
# WHY IT RUNS HERE, BETWEEN AU1'S CHECK AND THE GENERATED LOOP. A poisoned evaluation can take a
# property red and so REACH an obligation, and AU1's check asks which obligation no declared input
# reached. Running above it would let an obligation pinned by nothing but a poisoned record read as
# pinned, which is the generated loop's own reason one block down. The order is the whole of that
# separation and this block must stay between them.
#
# THREE LIMITS, STATED WHERE THEY APPLY RATHER THAN CLAIMED AWAY.
#
#   * It measures the reads a declared GREEN-EXPECTED input provokes. A read on a path no such input
#     reaches is invisible here, exactly as an unreached guard is invisible to the probe corpus, and
#     the `BB1-c` probe is what demonstrates that rather than asserting it. Green-expected because on
#     a declared mutation the property is supposed to be red, and a poisoned field that leaves it red
#     says nothing about whether the record could be read.
#   * `declaredSteps` is outside the poisoned set. The harness builds the step index from it once,
#     before any evaluator runs, so no evaluator reads those fields and poisoning them would measure
#     the harness rather than the properties. AZ3's dropped-field sweep is what covers that surface.
#   * A `Read-Optional` is a JUDGEMENT that an absent field is a fact the design states, and a
#     `Read-Obligation` the judgement that the absence is a violation. This census checks that the
#     judgement was DECLARED; the checks beside it require the POLARITY of the input that exercises
#     each to match what the judgement claims, which is BC1 and is what turned two of these into
#     obligations. What none of them can do is read the artifact: a declaration whose reason cites a
#     contract sentence that does not say what it claims is exercised, correctly polarised, and still
#     wrong. Each reason is written at its call site, which is where a reader audits it, and BC1's
#     own record names that as the next question rather than an answered one.
# ---------------------------------------------------------------------------------------------

# The readers that performed the read now in progress. It is a closure-captured LOCAL and not a
# `$script:` accumulator, and that is the point rather than a detail: BA3's rule is that a
# per-evaluation `$script:` collection must be cleared and drained at every top-level dispatch in
# this file, and this one is meaningful at exactly one of the five. A collection that is empty and
# ignored at four dispatches would have to be declared and checked at all five to say so. Captured
# here, it does not exist for the other four to get wrong.
$censusSiteReaders = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

# BB7, and the reason this walk is written out inside the getter instead of calling a function that
# holds it. `GetNewClosure` snapshots the VARIABLES of the enclosing scope into a new module scope,
# and a function this script defines is not resolvable from that scope when the script is invoked
# through the call operator rather than through `-File`. The getter then throws `CommandNotFound`,
# and a `PSScriptProperty` getter that throws yields `$null` to its reader **without surfacing the
# error** -- so every read came back unattributed, `$censusSiteReaders` stayed empty, and the census
# reported a clean 0 raw reads over a package it had entirely stopped reading.
#
# That is BA6's class in the instrument built one pass after BA6, and total rather than partial. The
# coverage measure is what found it: it runs each gate through the call operator in a child process,
# reported eleven constructs of this census as never executed, and the eleven were every construct
# downstream of a read being recorded. **The exemptions that would have silenced it were drafted
# before the question "why did this not run" was asked**, which is the mistake this note exists to
# stop the next reader repeating -- a coverage report is a finding until it is explained.
#
# `Get-PSCallStack` is a cmdlet and resolves from the closure's scope; a script function does not.
# The `$censusObserved` check below is the second half of the correction, and it is the half that
# would have caught this without the coverage measure.
$censusPoison = {
    foreach ($censusFrame in (Get-PSCallStack)) {
        if ($censusFrame.FunctionName -match '^(Read-Required|Read-Optional|Read-Obligation|Read-Rendering|Get-List|Get-Timeline|Get-Interactions|Get-Sessions|Get-Field)$') {
            [void]$censusSiteReaders.Add($censusFrame.FunctionName)
            break
        }
        if ($censusFrame.FunctionName -like 'Invoke-*') {
            [void]$censusSiteReaders.Add("$($censusFrame.FunctionName):$($censusFrame.ScriptLineNumber)")
            break
        }
    }
    return $null
}.GetNewClosure()

# The fields of one record, in the order they are poisoned. `declaredSteps` is excluded at the top
# level for the reason above; the depth bound is a guard against a self-referencing record rather than
# a judgement about the data, since the deepest path any vector carries is four.
function Get-CensusFields {
    param($Node, [int]$Depth)

    if ($null -eq $Node -or $Depth -gt 8) { return ,@() }
    if ($Node -is [string] -or $Node -is [bool] -or $Node -is [int] -or $Node -is [long] -or $Node -is [double]) { return ,@() }
    if ($Node -is [System.Collections.IEnumerable]) { return ,@() }
    return ,@($Node.PSObject.Properties | Where-Object { $_ -is [System.Management.Automation.PSNoteProperty] })
}

$censusSiteCount = 0
$censusRawCount = 0
$censusReadCount = 0
# One finding per (property, input, field, reading site). A vector carries many records of one shape
# and each is its own site, so the same raw read is found once per record; the finding names the
# reading line and the field, and a reader cannot act on the copies differently.
$censusReported = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($property in $properties.properties) {
    $propertyId = [string]$property.id
    if (-not $evaluators.ContainsKey($propertyId)) { continue }
    $evaluator = $evaluators[$propertyId]
    $censusPairsWalked = 0

    # Green-expected inputs only. On a declared mutation the property is SUPPOSED to be red, and a
    # poisoned field that leaves it red says nothing about whether the record could be read.
    $censusInputs = [System.Collections.Generic.List[string]]::new()
    foreach ($member in @($property.requiredGreen) + @($property.additionalGreen)) {
        if ($null -ne $member) { [void]$censusInputs.Add([string]$member.vector) }
    }

    foreach ($vectorId in ($censusInputs | Sort-Object -Unique)) {
        if (-not $vectorsById.ContainsKey($vectorId)) { continue }
        if ($CensusPairs -gt 0 -and $censusPairsWalked -ge $CensusPairs) { break }
        $censusPairsWalked++
        $censusVector = $vectorsById[$vectorId]
        $censusSteps = $vectorIndex[$vectorId]
        $script:UnpublishedFields.Clear()
        $censusBaselineResult = & $evaluator -VectorId $vectorId -Vector $censusVector -Steps $censusSteps
        # BA2's rule at its own dispatch: a baseline the property could not evaluate is not a baseline,
        # and every member of the record is read here for the reason AZ1 and BA1 were raised.
        foreach ($evaluationError in $censusBaselineResult.Errors) {
            $failures.Add("The read-provenance census cannot evaluate '$propertyId' over the unpoisoned form of '$vectorId': $evaluationError Every poisoned run below is compared against this one.")
        }
        if ([string]$censusBaselineResult.Verdict -eq 'red') {
            $failures.Add("The read-provenance census takes '$propertyId' red on '$vectorId' before poisoning anything, through '$($censusBaselineResult.Conjunct)': $($censusBaselineResult.Witness) It measures green-expected inputs, so a red baseline means the input's declared role and its verdict disagree.")
        }
        $censusBaseline = [string]$censusBaselineResult.Verdict
        $censusVectorReads = 0

        # Depth-first, and it PRUNES: a field beneath a container the evaluator did not read cannot
        # itself have been read, because the container is the only path to it. So a container whose
        # poisoning went unread takes its whole subtree out of the walk. That is not an optimisation
        # bolted on afterwards -- it is what makes this measure affordable in a gate the probe corpus
        # re-runs once per probe. Flat, the walk poisoned 10,862 fields to find 945 reads; pruned it
        # poisons 3,594 and finds the same 945, because the ones it skips are exactly the ones no
        # evaluator could reach, and this gate at `-GeneratedCount 0` goes from 10.6 seconds to 3.2.
        # The two forms were run against each other before the flat one was removed -- with and
        # without an injected raw read, where both report the same forty raw reads.
        $censusPending = [System.Collections.Generic.Stack[object]]::new()
        foreach ($topLevel in (Get-CensusFields -Node $censusVector -Depth 0)) {
            if ($topLevel.Name -eq 'declaredSteps') { continue }
            $censusPending.Push([pscustomobject]@{ Parent = $censusVector; Name = $topLevel.Name; Depth = 1 })
        }

        while ($censusPending.Count -gt 0) {
            $censusSite = $censusPending.Pop()
            $censusParent = $censusSite.Parent
            $censusName = $censusSite.Name
            $censusOriginal = $censusParent.PSObject.Properties[$censusName]
            if ($null -eq $censusOriginal -or $censusOriginal -isnot [System.Management.Automation.PSNoteProperty]) { continue }
            $censusValue = $censusOriginal.Value
            $censusSiteCount++

            # In place and restored in a `finally`, for AZ3's reason next door: copying the vector per
            # site costs nineteen milliseconds and there are thousands of sites.
            $censusSiteReaders.Clear()
            $script:UnpublishedFields.Clear()
            $censusParent.PSObject.Properties.Remove($censusName)
            $censusParent.PSObject.Properties.Add([System.Management.Automation.PSScriptProperty]::new($censusName, $censusPoison))
            $script:CensusPoisoning = $true
            try { $censusResult = & $evaluator -VectorId $vectorId -Vector $censusVector -Steps $censusSteps }
            finally {
                $script:CensusPoisoning = $false
                $censusParent.PSObject.Properties.Remove($censusName)
                $censusParent.PSObject.Properties.Add([System.Management.Automation.PSNoteProperty]::new($censusName, $censusValue))
            }
            if ($censusSiteReaders.Count -eq 0) { continue }
            $censusReadCount++
            $censusVectorReads++

            # Read, so what it holds is reachable and is walked. A collection is walked per element,
            # since each record of one shape is its own site.
            foreach ($censusChildHolder in @(if ($censusValue -is [System.Collections.IEnumerable] -and $censusValue -isnot [string]) { $censusValue } else { $censusValue })) {
                foreach ($censusChild in (Get-CensusFields -Node $censusChildHolder -Depth $censusSite.Depth)) {
                    $censusPending.Push([pscustomobject]@{ Parent = $censusChildHolder; Name = $censusChild.Name; Depth = $censusSite.Depth + 1 })
                }
            }

            foreach ($censusReader in ($censusSiteReaders | Sort-Object)) {
                if ($censusReader -notmatch '^Invoke-|^unattributed$') { continue }
                $censusRawCount++
                if (-not $censusReported.Add("$propertyId|$vectorId|$censusName|$censusReader")) { continue }
                # Every member of the poisoned record is read, and each says something a reader of the
                # finding needs: the verdict says which of the two failure classes this is, the errors
                # and the unpublished fields say whether some OTHER reader on the same record caught
                # what this one missed, and the conjunct and witness are BA4's diagnosis -- the
                # counterexample a reader can act on without re-deriving which clause produced it.
                $censusWitness = if ($censusResult.Witness) { " Witness: $($censusResult.Witness)$(if ($censusResult.Conjunct) { ", through '$($censusResult.Conjunct)'" })." } else { '' }
                $censusObserved = if ([string]$censusResult.Verdict -ne $censusBaseline) { "moved the verdict to $([string]$censusResult.Verdict).$censusWitness" }
                    elseif (@($censusResult.Errors).Count -gt 0 -or $script:UnpublishedFields.Count -gt 0) { 'was reported, but by another reader on the same record.' }
                    else { 'left the property green and reported nothing.' }
                $failures.Add("Property '$propertyId' reads '$censusName' off a record of '$vectorId' at $censusReader without a sanctioned reader, and making that field unreadable $censusObserved A raw read cannot say it could not read the record, so the property's green over an unreadable record is indistinguishable from its green over a conforming one. Read it through Read-Required, Read-Optional with the reason absence is a fact, Read-Obligation where the absence is the violation, or Get-List.")
            }
        }

        # BB7's second half, and the one that does not depend on another instrument noticing. Every
        # property reads SOMETHING off every input it is declared green on -- the least any of them
        # reads is one field of the vector -- so a walk that poisoned this vector's fields and
        # observed no read at all did not measure this property: it measured its own machinery
        # failing. Reported per property and input rather than as one total, because a census blinded
        # for one property and working for the rest is the partial case BA6 was, and a total would
        # hide it.
        if ($censusVectorReads -le 0) {
            $failures.Add("The read-provenance census poisoned every field of '$vectorId' and observed '$propertyId' reading none of them. Every property reads at least one field of every input it is declared green on, so this is the census failing to observe rather than the property failing to read -- a green from it here would be a clean report over a package it had stopped reading.")
        }
    }
}
# BB5, and BC1 beneath it. The first branch is BB5 unchanged in meaning: a declaration nothing
# exercises is a `Read-Required` written the long way. The second is what BB5 could not see -- an
# absence observed ONLY where the reading property is declared red is the absence of a conforming
# record, so it says nothing about a fact the design states and everything about a violation.
foreach ($optionalDeclaration in ($script:OptionalReads.Keys | Sort-Object)) {
    $optionalPolarities = $script:OptionalReads[$optionalDeclaration]
    if ($optionalPolarities.Count -eq 0) {
        $failures.Add("A Read-Optional declares that an absent $optionalDeclaration -- and no declared input leaves that field absent, so the declaration is unfalsified and the read is a Read-Required written the long way. Either make it required, or add the input whose silence the reason describes.")
        continue
    }
    if (-not $optionalPolarities.Contains('green')) {
        $failures.Add("A Read-Optional declares that an absent $optionalDeclaration -- and every input that leaves it absent is one the reading property is declared $(($optionalPolarities | Sort-Object) -join '/') on, never green. An absence a conforming input never produces is not a fact the design states about a conforming record; it is the violation the clause detects, which is Read-Obligation. Either read it through that, or add the conforming input whose silence the reason describes.")
    }
}
# The mirror, and it is the whole reason the fifth reader is checkable rather than merely honest. A
# declared obligation no red input exercises is a clause nothing shows catching the omission -- AR1's
# unfalsifiable clause, arriving through the reader instead of through the mutation table.
foreach ($obligationDeclaration in ($script:ObligationReads.Keys | Sort-Object)) {
    $obligationPolarities = $script:ObligationReads[$obligationDeclaration]
    if (-not $obligationPolarities.Contains('red')) {
        # The observed polarities are rendered without a conditional, and an empty list is the case
        # where no input leaves the field absent at all. A branch here would be reachable only on a
        # failing run, which the coverage measure reports as a never-executed construct and which the
        # exemption for it would then have to assert away -- BB7's lesson about drafting the exemption
        # before asking why the construct did not run.
        $failures.Add("A Read-Obligation declares that an absent $obligationDeclaration -- and the inputs that leave it absent are declared '$(($obligationPolarities | Sort-Object) -join '/')', never red; an empty list there means no input leaves it absent at all. Nothing then demonstrates the clause catches the omission, which is the unfalsifiable clause AR1 was raised against reached through the reader. Add the mutation whose omission the reason describes, or read the field through Read-Required.")
    }
}
$censusSanctioned = $censusReadCount - $censusRawCount
$censusScope = if ($CensusPairs -gt 0) { " -- CAPPED at $CensusPairs pairs per property by -CensusPairs, so this is not a census of the corpus" } else { '' }
# This line runs whether or not the checks above added a failure, so it states what was MEASURED and
# not what a passing run would imply about it. "Exercised by a conforming input" is the verdict of the
# check, not a property of the count, and printing it here would have the measure assert on a failing
# run exactly what that run had just contradicted.
Write-Host "Channel 0.2 read-provenance census: $censusReadCount of $censusSiteCount poisoned fields were read by an evaluator, $censusSanctioned of those through a sanctioned reader and $censusRawCount raw, over $($script:OptionalReads.Count) Read-Optional and $($script:ObligationReads.Count) Read-Obligation declarations, each checked against the declared verdict of the inputs whose silence exercises it.$censusScope"

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

    # ---------------------------------------------------------------------------------------------
    # AZ3. The dropped-field sweep: the nine retained operand mutations, as a rate.
    #
    # Every operand correction this programme made -- AF8, AG2, AH1, AI1, AJ1, AK1, AK5, AK6 -- is
    # about a frame reference that had LOST A FIELD. A reference resolves to every declared step
    # matching the fields it publishes, so dropping one WIDENS the candidate set, and that widening is
    # what the nine retained operand mutations assert by hand on hand-written vectors today.
    #
    # This drops each field from each of `C4-P2`'s three references on every generated vector and
    # states what must happen. Three outcomes are possible and all three occur:
    #
    #   * `red`         -- the field is LOAD-BEARING: without it a conforming realization is misjudged.
    #   * `green`       -- the field moves no verdict on this population. Recorded as a limit, NOT as
    #                      evidence the field is redundant: the declared corpus keeps its own mutations
    #                      for exactly the shapes a generator does not reach.
    #   * `unevaluable` -- the record carries no operand at all, so the property cannot be evaluated.
    #                      Only dropping a WHOLE reference reaches this. The plan's brief for this pass
    #                      expected a dropped FIELD to be able to leave a reference "resolving to no
    #                      step"; it cannot, because dropping a filter only ever widens a candidate set
    #                      and never empties one. That is a correction to the brief, not a finding.
    #
    # A `red` must fire through the NAMED conjunct, for the reason the declared harness gives: a
    # mutation that fires through the other conjunct is unfalsifiable however well it is named.
    $referenceDrops = @(
        # Whole references. `C4-P2` reports these as errors rather than verdicts, which is AK6's
        # machinery, and until AZ1 this block discarded them.
        @{ Drop = 'unseen-refusals.refusedFrame'; Verdict = 'unevaluable'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.settlingFrame'; Verdict = 'unevaluable'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.terminalFrame'; Verdict = 'unevaluable'; Conjunct = $null; Discriminates = 'every' },

        # AK1, on both conjuncts. One session's frames and another's collide on every field a
        # reference publishes except the session, because the arrival ordinal is counted per identity
        # and restarts in each session -- so the session is the only thing separating them, and
        # dropping it is AF8's two-session failure reproduced rather than read. It discriminates only
        # where there are two sessions to conflate, which is why the scope is stated.
        @{ Drop = 'unseen-refusals.refusedFrame.session'; Verdict = 'red'; Conjunct = 'C4-P2-conjunct-1'; Discriminates = 'multi-session' },
        @{ Drop = 'late-traffic-latches.settlingFrame.session'; Verdict = 'red'; Conjunct = 'C4-P2-conjunct-2'; Discriminates = 'multi-session' },
        @{ Drop = 'late-traffic-latches.terminalFrame.session'; Verdict = 'red'; Conjunct = 'C4-P2-conjunct-2'; Discriminates = 'multi-session' },

        # AK5, and Y4's argument on this operand: one endpoint commits two controls naming one identity
        # in one session, the request arrives between them, and the record has to say which was
        # refused. Without the ordinal the reference binds to the later control too, and the property
        # goes red on delivery that matched commit order.
        @{ Drop = 'unseen-refusals.refusedFrame.arrivalOrdinal'; Verdict = 'red'; Conjunct = 'C4-P2-conjunct-1'; Discriminates = 'every' },

        # The identity operand of the membership test and of the request set both.
        @{ Drop = 'unseen-refusals.refusedFrame.interactionIdentity'; Verdict = 'red'; Conjunct = 'C4-P2-conjunct-1'; Discriminates = 'every' },

        # Recorded and not raised, exactly as closure review 16 recorded their declared counterparts:
        # these move no verdict on this population. That is a limit of the population and not a case
        # for removing the field -- adding an operand is strictly more precise than inferring one.
        @{ Drop = 'unseen-refusals.refusedFrame.kind'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'unseen-refusals.refusedFrame.committingEndpoint'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.settlingFrame.kind'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.settlingFrame.interactionIdentity'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.settlingFrame.committingEndpoint'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.settlingFrame.arrivalOrdinal'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.terminalFrame.kind'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.terminalFrame.interactionIdentity'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.terminalFrame.committingEndpoint'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' },
        @{ Drop = 'late-traffic-latches.terminalFrame.arrivalOrdinal'; Verdict = 'green'; Conjunct = $null; Discriminates = 'every' })
    $dropTally = @{}
    foreach ($referenceDrop in $referenceDrops) {
        $dropTally[[string]$referenceDrop.Drop] = @{ Discriminating = 0; Inert = 0 }
    }

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

            # C4's frames for this session, and the three observation records that read them. All are
            # built to be **conforming**, which for `C4-P2` means the two situations its conjuncts
            # forbid do not occur while the records they quantify over do exist. Each is also built to
            # be conforming for a DIFFERENT reason, because a conjunct that is green for one reason on
            # every record in the population is tested by one record however many there are:
            #
            #   * Conjunct 1 forbids an `unseen` refusal of a cancellation control whose request the
            #     same endpoint had already committed AND whose identity the recipient afterwards
            #     admits. The first refusal is green because its identity was opened in NO session at
            #     all (session 1) or in a DIFFERENT one (every later session), so the conjunct's
            #     session-scoped request set is empty. The second is green because the control it names
            #     was committed BEFORE the request that opens its identity, so the precedence half is
            #     false although the membership half is true. Both are the legitimate `unseen` case the
            #     design keeps `rejected-protocol` for, and each is green on a record it examined.
            #   * Conjunct 2 forbids a late-traffic latch settled against a frame committed BEFORE the
            #     endpoint's own terminal frame. Here the settling frame is committed after it, which
            #     is what late traffic is, so the comparison runs and finds nothing.
            #
            # The refusal's `detailedReason`, `provenance` and `frameDecision` are the selectors the
            # conjunct narrows on; a record missing them would be skipped and prove nothing.
            # AZ2. The arrival ordinal is "its arrival ordinal FOR THAT INTERACTION IDENTITY", and the
            # declared corpus counts it per receiving endpoint, per session, per identity: in
            # `C4-two-sessions-one-identity` one identity's frames in two sessions are BOTH ordinal 1,
            # and that collision is the only reason the session operand is load-bearing there.
            #
            # This generator used a single counter running across the whole vector, so every generated
            # ordinal was globally unique and a reference publishing one was fully determined by it
            # alone. Every other field it published was then redundant BY CONSTRUCTION, and dropping
            # any one of them could not move a verdict -- which is why fourteen of the fifteen field
            # droppings were green before this pass and why AK1 and AK5 could not reproduce here at any
            # vector count. The generator was publishing a uniqueness the design does not give.
            $ordinalCounters = @{}
            $stepOrdinals = @{}

            # Three interaction identities per session, because one identity cannot carry the shapes
            # the retained operand mutations are about:
            #   * `f1` is REUSED across sessions, so the latch's two references collide on ordinal
            #     between sessions and their session field is what separates them -- AK1 on conjunct 2.
            #   * `g<k>` is opened in this session and named by the NEXT session's `unseen` refusal,
            #     which is the two-session identity reuse AF8's sentence was written for: conforming at
            #     both endpoints, refusal in one session and admission in the other -- AK1 on conjunct 1.
            #   * `h<k>` is opened by a request that arrives BETWEEN two controls naming it, so the
            #     record has to say which control it refused -- AK5, and Y4's argument on that operand.
            $sharedIdentity = 'f1'
            $carriedIdentity = "g$sessionOrdinal"
            $twoControlIdentity = "h$sessionOrdinal"
            # Session 1 has no predecessor to have opened anything, so its refusal names an identity no
            # session opens at all. That is the other legitimate `unseen` case and both belong here.
            $unopenedIdentity = if ($sessionOrdinal -eq 1) { 'u1' } else { "g$($sessionOrdinal - 1)" }
            $frameSpecs = @(
                @{ Id = "$sessionId-control-early"; Kind = 'cancellation-control'; Endpoint = 'initiator'; Identity = $twoControlIdentity; Commit = 1 },
                @{ Id = "$sessionId-request"; Kind = 'request'; Endpoint = 'initiator'; Identity = $sharedIdentity; Commit = 1 },
                @{ Id = "$sessionId-request-carried"; Kind = 'request'; Endpoint = 'initiator'; Identity = $carriedIdentity; Commit = 1 },
                @{ Id = "$sessionId-request-two-control"; Kind = 'request'; Endpoint = 'initiator'; Identity = $twoControlIdentity; Commit = 1 },
                @{ Id = "$sessionId-control-unopened"; Kind = 'cancellation-control'; Endpoint = 'initiator'; Identity = $unopenedIdentity; Commit = 1 },
                @{ Id = "$sessionId-control-late"; Kind = 'cancellation-control'; Endpoint = 'initiator'; Identity = $twoControlIdentity; Commit = 1 },
                @{ Id = "$sessionId-terminal"; Kind = 'outcome'; Endpoint = 'recipient'; Identity = $sharedIdentity; Commit = 1 },
                @{ Id = "$sessionId-late"; Kind = 'cancellation-control'; Endpoint = 'recipient'; Identity = $sharedIdentity; Commit = 2 })
            foreach ($frameSpec in $frameSpecs) {
                $receiving = $(if ($frameSpec.Endpoint -eq 'initiator') { 'recipient' } else { 'initiator' })
                $ordinalKey = "$receiving|$sessionId|$($frameSpec.Identity)"
                if (-not $ordinalCounters.ContainsKey($ordinalKey)) { $ordinalCounters[$ordinalKey] = 0 }
                $ordinalCounters[$ordinalKey]++
                $stepOrdinals[$frameSpec.Id] = $ordinalCounters[$ordinalKey]
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
                    receivingEndpoint = $receiving
                    arrivalOrdinal = $ordinalCounters[$ordinalKey]
                })
            }
            # The recipient admits the identity it was asked to open, and never the unopened one --
            # which is the operand AF8 scoped to the session and AK1 was raised for.
            $admittedSets.Add([pscustomobject]@{ session = $sessionId; identities = @($sharedIdentity, $carriedIdentity, $twoControlIdentity) })
            # Refusal one: the control names an identity THIS session never opened. For session 1 no
            # session opened it; for every later session the previous one did, and admits it. Both are
            # conforming -- an identity is opened within a session, so one opened elsewhere is unopened
            # here -- and the second is the shape that makes the session operand load-bearing.
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
                    arrivalOrdinal = $stepOrdinals["$sessionId-control-unopened"]
                }
            })
            # Refusal two: the earlier of two controls naming one identity in one session from one
            # endpoint, refused before the request that opens it arrived. Conforming for the same
            # reason -- nothing had opened the identity yet -- and green only because the ordinal says
            # WHICH control was refused. Drop that ordinal and the reference binds to the later control
            # as well, which the request does precede.
            $unseenRefusals.Add([pscustomobject]@{
                provenance = 'recipient'
                frameDecision = 'rejected-protocol'
                detailedReason = 'unopened-interaction-identity'
                effectCertainty = 'known-none'
                refusedFrame = [pscustomobject]@{
                    kind = 'cancellation-control'
                    session = $sessionId
                    interactionIdentity = $twoControlIdentity
                    committingEndpoint = 'initiator'
                    arrivalOrdinal = $stepOrdinals["$sessionId-control-early"]
                }
            })
            $lateTrafficLatches.Add([pscustomobject]@{
                category = 'state-violation'
                latchValue = 'fault-committed'
                settlingFrame = [pscustomobject]@{
                    kind = 'cancellation-control'
                    session = $sessionId
                    interactionIdentity = $sharedIdentity
                    committingEndpoint = 'recipient'
                    arrivalOrdinal = $stepOrdinals["$sessionId-late"]
                }
                terminalFrame = [pscustomobject]@{
                    kind = 'outcome'
                    session = $sessionId
                    interactionIdentity = $sharedIdentity
                    committingEndpoint = 'recipient'
                    arrivalOrdinal = $stepOrdinals["$sessionId-terminal"]
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
        # The two shapes AZ3's sweep needs, keyed on what makes each one discriminating rather than
        # on a record existing. Without the first, dropping the session operand conflates nothing;
        # without the second, dropping the arrival ordinal binds to the same single control.
        'a refusal naming an identity another session opened'      = 'carried-identity-refusal'
        'a refusal of the earlier of two controls naming one identity' = 'two-control-refusal'
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
        foreach ($shapeRefusal in @($generatedVector.observations.unseenRefusals)) {
            if ($null -eq $shapeRefusal -or $null -eq $shapeRefusal.refusedFrame) { continue }
            $shapeIdentity = [string]$shapeRefusal.refusedFrame.interactionIdentity
            $shapeSession = [string]$shapeRefusal.refusedFrame.session
            # Carried: some OTHER session admits the identity this one refused as unopened. That is
            # the two-session reuse AF8's sentence is about, and it is what the session operand
            # separates.
            foreach ($shapeAdmitted in @($generatedVector.observations.recipientAdmittedIdentities)) {
                if ([string]$shapeAdmitted.session -ne $shapeSession -and @($shapeAdmitted.identities) -contains $shapeIdentity) {
                    $shapesSeen['carried-identity-refusal'] = $true
                }
            }
            # Two controls: the refused reference names one of at least two declared controls sharing
            # its session, identity and committing endpoint, so the arrival ordinal is the only thing
            # saying which of them was refused.
            $shapeControls = @($generatedVector.declaredSteps | Where-Object {
                [string]$_.kind -eq 'cancellation-control' -and [string]$_.session -eq $shapeSession -and
                [string]$_.interactionIdentity -eq $shapeIdentity -and
                [string]$_.committingEndpoint -eq [string]$shapeRefusal.refusedFrame.committingEndpoint })
            if ($shapeControls.Count -gt 1) { $shapesSeen['two-control-refusal'] = $true }
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
            # AZ1. An evaluator reports two different things and this block read only one of them.
            # `Errors` is how a property says it could not be evaluated over a record -- a reference
            # carrying no operand, or one resolving to no declared step -- and the verdict that comes
            # back beside a non-empty `Errors` is `green`, because a record the property could not
            # read produced no witness. The declared loop has always drained it; this one discarded
            # it, so a generated population whose every `C4-P2` record was unevaluable reported `0
            # red` and passed. It was proven that way: naming an identity no step carries made all
            # 25 vectors' first-conjunct records unresolvable, and the run was clean.
            #
            # That is the vacuous pass this whole instrument exists to detect, one level below where
            # the twelfth pass found it, and it silently held open the exact hole the thirteenth
            # pass's review reported as closed.
            foreach ($evaluationError in $generatedResult.Errors) {
                if ($generatedRed.Count -lt 10) {
                    $generatedRed.Add("'$propertyId' could not be evaluated over generated conforming vector '$generatedId': $evaluationError")
                }
            }
            # BA4. The conjunct was the one thing an evaluator hands back that this block did not
            # read. It declares no expected conjunct here -- every red on a conforming vector is a
            # failure whichever clause produces it -- so this is diagnosis rather than a check, and
            # it is read because a reader of a rate needs to know which clause the counterexample
            # came from without re-running the seed by hand.
            if ($generatedResult.Verdict -eq 'red' -and $generatedRed.Count -lt 10) {
                $generatedConjunct = if ($generatedResult.Conjunct) { " through '$($generatedResult.Conjunct)'" } else { '' }
                $generatedRed.Add("'$propertyId' is red$generatedConjunct on generated conforming vector '$generatedId': $($generatedResult.Witness)")
            }
        }

        # The sweep removes one published field from the vector's own observation records, evaluates
        # `C4-P2`, and puts the field back. In place and restored rather than over a copy: copying
        # cost nineteen milliseconds a drop, thirty-six thousand times on the deep run, and the guard
        # corpus re-runs this gate once per probe. The restore is in a `finally` so a throwing
        # evaluator cannot leave the next drop reading a record with two fields missing.
        #
        # That the restore is faithful is not asserted separately -- it is what the eight load-bearing
        # droppings below establish, since a record that had lost a field permanently would take the
        # NEXT drop's declared verdict somewhere it is not declared, and the table reports that.
        $dropSessionCount = @($generatedVector.sessions).Count
        foreach ($referenceDrop in $(if ($generatedOrdinal -le $SweptCount) { $referenceDrops } else { @() })) {
            $dropPath = [string]$referenceDrop.Drop
            $dropUndo = [System.Collections.Generic.List[object]]::new()
            $dropReached = Remove-PublishedField -Vector $generatedVector -DropPath $dropPath -Undo $dropUndo
            if ($dropReached -le 0) {
                # A drop that reaches no record reports the undropped verdict, so a green from it is a
                # false negative rather than evidence the field is inert. Same guard, same reason, as
                # the declared operand harness above.
                Restore-PublishedFields -Undo $dropUndo
                if ($generatedRed.Count -lt 10) {
                    $generatedRed.Add("the dropped-field sweep reverted no published field for '$dropPath' on '$generatedId'")
                }
                continue
            }
            # BA3, as at the operand harness above: this is the sweep's own field-removing input and
            # its silence was equally unchecked. `Invoke-C4P2` reads its record through `Get-Field`
            # rather than `Read-Required`, so nothing reaches this today; the rule is over the
            # dispatch rather than over which evaluator happens to sit behind it.
            $script:UnpublishedFields.Clear()
            try { $dropResult = Invoke-C4P2 -VectorId $generatedId -Vector $generatedVector -Steps $generatedSteps }
            finally { Restore-PublishedFields -Undo $dropUndo }
            foreach ($unpublished in ($script:UnpublishedFields | Sort-Object -Unique)) {
                if ($generatedRed.Count -lt 10) {
                    $generatedRed.Add("dropping '$dropPath' leaves 'C4-P2' reading a field '$generatedId' does not publish: $unpublished")
                }
            }
            $dropObserved = if ($dropResult.Errors.Count -gt 0) { 'unevaluable' }
                elseif ($dropResult.Verdict -eq 'red') { 'red' }
                else { 'green' }
            # A drop declared to discriminate only between sessions is REQUIRED to be inert on a
            # single-session vector, not merely allowed to be. A field that moved a verdict where the
            # scope says it cannot would mean the scope, not the field, is what is wrong.
            $dropDiscriminates = ([string]$referenceDrop.Discriminates -eq 'every' -or $dropSessionCount -gt 1)
            $dropExpected = if ($dropDiscriminates) { [string]$referenceDrop.Verdict } else { 'green' }
            if ($dropDiscriminates) { $dropTally[$dropPath].Discriminating++ } else { $dropTally[$dropPath].Inert++ }
            if ($dropObserved -ne $dropExpected) {
                if ($generatedRed.Count -lt 10) {
                    $dropWitness = if ($dropObserved -eq 'red') { " Witness: $($dropResult.Witness)." }
                        elseif ($dropObserved -eq 'unevaluable') { " $($dropResult.Errors[0])" } else { '' }
                    $generatedRed.Add("dropping '$dropPath' leaves 'C4-P2' $dropObserved on '$generatedId', which carries $dropSessionCount session(s), and the sweep declares $dropExpected there.$dropWitness")
                }
            }
            elseif ($dropObserved -eq 'red' -and [string]$dropResult.Conjunct -ne [string]$referenceDrop.Conjunct -and $generatedRed.Count -lt 10) {
                $generatedRed.Add("dropping '$dropPath' takes 'C4-P2' red on '$generatedId' through '$($dropResult.Conjunct)' and the sweep declares it fires through '$($referenceDrop.Conjunct)'. A drop whose red arrives through the other conjunct witnesses something other than the operand it names.")
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
    # The rate AZ3 reports, stated per outcome class rather than as one number: "no drop misbehaved"
    # says nothing about how many of them could have. A drop with no discriminating vector behind it
    # is the vacuity this instrument was built to end, so it is a failure and not a footnote.
    $dropDiscriminating = 0
    $dropInert = 0
    $dropSwept = [Math]::Min($SweptCount, $GeneratedCount)
    foreach ($referenceDrop in $referenceDrops) {
        $dropPath = [string]$referenceDrop.Drop
        $dropDiscriminating += $dropTally[$dropPath].Discriminating
        $dropInert += $dropTally[$dropPath].Inert
        if ($dropTally[$dropPath].Discriminating -le 0) {
            $failures.Add("No generated vector let the sweep evaluate '$dropPath' where it is declared to discriminate, over the first $dropSwept of $GeneratedCount at seed $GeneratedSeed. A dropped field with no vector behind it reports its declared verdict by never having been tried.")
        }
    }
    $dropLoadBearing = @($referenceDrops | Where-Object { [string]$_.Verdict -ne 'green' }).Count
    Write-Host "Channel 0.2 generated-vector evaluation: $generatedEvaluations evaluations over $GeneratedCount generated conforming vectors at seed $GeneratedSeed, $($generatedRed.Count) red."
    Write-Host "Channel 0.2 dropped-field sweep: $($referenceDrops.Count) frame-reference droppings over the first $dropSwept of those vectors, $dropDiscriminating evaluated where declared to discriminate and $dropInert where declared inert, $dropLoadBearing of them load-bearing."
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

Write-Host "Channel 0.2 property verification passed: $(@($properties.properties).Count) of 26 properties executable, $evaluationCount property evaluations over $(@($vectorFile.vectors).Count) declared inputs, $mutationCount operand mutations."
