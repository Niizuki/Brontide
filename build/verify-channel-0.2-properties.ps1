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

$evaluators = @{ 'C4-P2' = ${function:Invoke-C4P2} }

# ---------------------------------------------------------------------------------------------
# Citations. The design artifacts own every fact this file states, and these checks fail when the two
# disagree rather than letting the executable form drift into a twelfth surface of its own.
# ---------------------------------------------------------------------------------------------

$numberWords = @{ 'one' = 1; 'two' = 2; 'three' = 3; 'four' = 4; 'five' = 5; 'six' = 6; 'seven' = 7; 'eight' = 8; 'nine' = 9; 'ten' = 10; 'eleven' = 11; 'twelve' = 12; 'twenty-five' = 25; 'twenty-six' = 26; 'zero' = 0 }

if ($briefPlain.IndexOf('a required-green set: the named legal inputs from the property', [System.StringComparison]::Ordinal) -lt 0) {
    $failures.Add("The neutral brief's capability-wide property format no longer states the required-green set as a normative field. This file's expectations are written against that field, so its removal would leave every green expectation here unsourced. This is AE3's field.")
}

foreach ($property in $properties.properties) {
    if ($contractPlain.IndexOf("Property $($property.id).", [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("Property '$($property.id)' is declared executable here and the capability contract states no property by that id.")
    }

    foreach ($mutation in $property.namedMutations) {
        if ($contractPlain.IndexOf([string]$mutation.vector, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("Named mutation '$($mutation.vector)' for '$($property.id)' is not a scenario the capability contract names. A mutation this file invents is a mutation no artifact requires, and a property red on it proves nothing about the design.")
        }
    }

    foreach ($member in $property.requiredGreen) {
        if ($contractPlain.IndexOf([string]$member.member, [System.StringComparison]::Ordinal) -lt 0) {
            $failures.Add("Required-green member '$($member.member)' for '$($property.id)' does not appear in the capability contract's own required-green set. Either the contract's set changed and this file did not, or this file names a member the contract does not require -- and a required-green set that is not the artifact's set is a second surface for the fact rather than an execution of it.")
        }
    }

    # AK4's class, on this file's own count: the contract states how many legal members the group has,
    # and a set that names a different number is the defect rather than the count being decorative.
    $memberCountClaim = [regex]::Match($contractPlain, "Property $([regex]::Escape($property.id))\..{0,4000}?required vector group has ([a-z-]+) legal members")
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

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

Write-Host "Channel 0.2 property verification passed: $(@($properties.properties).Count) of 26 properties executable, $evaluationCount property evaluations over $(@($vectorFile.vectors).Count) declared inputs, $mutationCount operand mutations."
