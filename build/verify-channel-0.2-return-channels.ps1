param(
    # Print the census instead of failing on it, for working on a consumer.
    [switch]$Report
)

$ErrorActionPreference = 'Stop'

# Channel 0.2 return-channel census.
#
# BA of the verification foundation plan's condition-4 work, and the unit is the one AZ1 named.
# AZ1 was not a defect in a check. It was a whole return channel -- `Errors`, through which a
# property says it could not be evaluated over a record at all -- that one of the loops consuming an
# evaluator never read. The verdict beside a non-empty `Errors` is `green`, so a generated population
# whose every `C4-P2` record was unevaluable reported `0 red` and passed.
#
# No instrument here could have found it. The probe corpus tests a guard someone already suspected;
# the coverage measure asks whether a construct RUNS, and a discarded return value runs perfectly;
# the generated run asks the design a question and reads the answer through the channel that was
# being dropped. So this file asks the question that finds the class, mechanically and over the whole
# of these gates: WHAT DOES EACH CONSUMER DO WITH EACH THING ITS PRODUCER HANDS BACK, AND WHICH OF
# THOSE HAS NO CONSUMER AT ALL.
#
# THE TWO UNITS.
#
#   1. Returned members. A producer is a function that hands back a record -- a hashtable or
#      `[pscustomobject]` literal, or the result of another producer, taken to a fixed point so a
#      function that merely forwards one is a producer of the same shape. A consumer is an assignment
#      whose right-hand side calls a producer, directly or through `& $variable` where the file
#      builds a dispatch table of `${function:...}` references. Every member the producer can return
#      must be read from the assigned variable somewhere, or be declared exempt for that consumer.
#
#   2. Script-scope accumulators. A return value is not the only channel: `$script:UnpublishedFields`
#      is how `Read-Required` says a vector does not publish a field an obligation read, and it is
#      cleared before a consumer's call and drained after it. A consumer that does neither cannot see
#      what it was told. Each accumulator declares whether it is `per-evaluation` -- cleared and
#      drained at every consumer -- or `cumulative`, read once over the whole run and never cleared.
#
# WHAT THIS MEASURE CANNOT SEE, stated because the coverage measure states the same about itself and
# for the same reason: it is a floor against the class, not a proof.
#
#   - A read is counted anywhere in the consumer's own SCOPE -- the function it sits in, or everything
#     outside every function for a top-level dispatch -- rather than on the path that needs it. So a
#     consumer that reads a member on one branch and drops it on another reads it, as far as this is
#     concerned. Measured: removing the delegate's `Errors` from `C4-P1`'s red path does not fire
#     here, because its green path still reads them. Separating those needs path analysis, which is a
#     different instrument.
#   - A record passed WHOLE to another function is not a read of its members. Consuming a channel
#     that way is legitimate and invisible here, which is why the corrections beside this file read
#     each member at the call site rather than forwarding the record.
#   - A producer that is only ever returned and never assigned has no consumer site here; its members
#     are censused at whatever site does assign the caller's result.
#
# What it IS total over is producers: discovery is from the syntax tree and nothing is listed by hand.
# Every exemption is anchored on the text it exempts and fails when that text moves, which is AP1's
# class and the reason the coverage gate's exemptions are anchored the same way.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$declarationPath = Join-Path $repositoryRoot 'conformance\channel-0.2-return-channels.json'
$failures = [System.Collections.Generic.List[string]]::new()
$reportRows = [System.Collections.Generic.List[object]]::new()

if (-not (Test-Path -LiteralPath $declarationPath)) {
    Write-Host "FAIL: the return-channel declaration does not exist: '$declarationPath'."
    exit 1
}

try { $declaration = Get-Content -Raw -LiteralPath $declarationPath -Encoding UTF8 | ConvertFrom-Json }
catch { Write-Host "FAIL: invalid JSON in '$declarationPath': $($_.Exception.Message)"; exit 1 }

# Everything a function hands back. An explicit `return` and the trailing expression of the body both
# count, because PowerShell returns both and a producer written either way is the same producer to its
# caller -- and so does the record a function BUILDS INTO A VARIABLE and then returns, resolved one
# level through the assignments to that variable inside the function.
#
# BA6, and it is this file's subject arriving in the commit that introduced it. Written to recognise a
# producer only by a record literal in the return position, this went blind the moment BA1's own
# correction gave `New-Red` and `New-Green` a body that builds the record and returns the variable:
# the properties gate went from twenty-nine producers to two, the six composed evaluators stopped
# being producers, and the census reported a clean package it was no longer looking at. That is AP1's
# class -- a key correct when written and expired when the work moved -- inside one commit, and it is
# the argument for resolving the form rather than matching it. The one-level depth is the limit this
# measure states rather than the depth it happens to need; a producer that reaches its record through
# two variables is not censused, and the probe corpus is where that would be noticed.
function Get-HandedBack {
    param([Parameter(Mandatory = $true)]$FunctionAst)

    $direct = [System.Collections.Generic.List[object]]::new()
    foreach ($returned in $FunctionAst.FindAll({ param($node) $node -is [System.Management.Automation.Language.ReturnStatementAst] }, $true)) {
        if ($null -ne $returned.Pipeline) { $direct.Add($returned.Pipeline) }
    }
    $trailing = @($FunctionAst.Body.EndBlock.Statements)
    if ($trailing.Count -ge 1) { $direct.Add($trailing[-1]) }

    $resolved = [System.Collections.Generic.List[object]]::new()
    foreach ($expression in $direct) {
        $resolved.Add($expression)
        $bare = [regex]::Match($expression.Extent.Text, '^\s*\$(\w+)\s*$')
        if (-not $bare.Success) { continue }
        foreach ($assignment in $FunctionAst.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
            if ($assignment.Left -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
            if ($assignment.Left.VariablePath.UserPath -cne $bare.Groups[1].Value) { continue }
            $resolved.Add($assignment.Right)
        }
    }
    return , $resolved
}

# Every shape a function hands back as a record literal.
function Get-RecordShape {
    param([Parameter(Mandatory = $true)]$FunctionAst)

    $shapes = [System.Collections.Generic.List[object]]::new()
    foreach ($candidate in (Get-HandedBack -FunctionAst $FunctionAst)) {
        if ($candidate.Extent.Text -notmatch '^\s*(\[pscustomobject\]\s*)?@\{') { continue }
        $tables = @($candidate.FindAll({ param($node) $node -is [System.Management.Automation.Language.HashtableAst] }, $true))
        if ($tables.Count -lt 1) { continue }
        $shapes.Add(@($tables[0].KeyValuePairs | ForEach-Object { $_.Item1.Extent.Text.Trim("'`"") }))
    }
    # Comma-wrapped so a single shape reaches the caller as a one-element list rather than unrolling
    # into the shape itself. Written the obvious way, this reported two of seven producers while
    # looking total, which is BA5 and this file's own instance of its subject.
    return , $shapes
}

# The same expressions as text, for the fixed point below.
function Get-ReturnedText {
    param([Parameter(Mandatory = $true)]$FunctionAst)

    $texts = [System.Collections.Generic.List[string]]::new()
    foreach ($expression in (Get-HandedBack -FunctionAst $FunctionAst)) {
        $texts.Add($expression.Extent.Text)
    }
    return , $texts
}

function Test-NamesIdentifier {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text, [Parameter(Mandatory = $true)][string]$Identifier)
    return $Text -match ('(^|[^\w-])' + [regex]::Escape($Identifier) + '($|[^\w-])')
}

$censusGates = @($declaration.gates)
if ($censusGates.Count -lt 1) {
    Write-Host 'FAIL: the return-channel declaration names no gate. A census over nothing passes by looking at nothing.'
    exit 1
}

$producerTotal = 0
$consumerTotal = 0

foreach ($censusGate in $censusGates) {
    $gateName = [string]$censusGate.gate
    $gatePath = Join-Path $repositoryRoot "build\$gateName"
    if (-not (Test-Path -LiteralPath $gatePath)) {
        $failures.Add("The return-channel declaration names the gate '$gateName' and no such file exists in build/.")
        continue
    }

    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($gatePath, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors -and $parseErrors.Count -gt 0) {
        $failures.Add("The gate '$gateName' does not parse, so its return channels cannot be censused: $($parseErrors[0].Message)")
        continue
    }

    $functions = @($ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true))
    $members = @{}
    foreach ($function in $functions) {
        $shapes = Get-RecordShape -FunctionAst $function
        if ($shapes.Count -lt 1) { continue }
        $union = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($shape in $shapes) {
            foreach ($member in $shape) {
                [void]$union.Add([string]$member)
            }
        }
        $members[$function.Name] = $union
    }

    # A function that hands back a producer's result is a producer of the same shape, and one that
    # hands back ITS result is one too -- taken to a fixed point rather than one level deep. The six
    # composed evaluators BA1 was raised against are exactly this: each returns `New-Red`, which is a
    # producer, so each is one, and each is then a consumer of the delegate it calls.
    $growing = $true
    while ($growing) {
        $growing = $false
        foreach ($function in $functions) {
            foreach ($returnedText in (Get-ReturnedText -FunctionAst $function)) {
                foreach ($producerName in @($members.Keys)) {
                    if (-not (Test-NamesIdentifier -Text $returnedText -Identifier $producerName)) { continue }
                    if (-not $members.ContainsKey($function.Name)) {
                        $members[$function.Name] = [System.Collections.Generic.HashSet[string]]::new()
                        $growing = $true
                    }
                    foreach ($member in $members[$producerName]) {
                        if ($members[$function.Name].Add($member)) { $growing = $true }
                    }
                }
            }
        }
    }

    # A set rather than a map. The first draft of this file stored `$false` against each exempt name
    # and then `$true`, and read neither -- only `Contains`. A stored value with no consumer, in the
    # file whose whole subject is stored values with no consumer, found by reading this file against
    # its own question rather than by running it.
    $exemptProducers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($producerExemption in @($censusGate.producerExemptions)) {
        [void]$exemptProducers.Add([string]$producerExemption.producer)
    }
    foreach ($exemptName in $exemptProducers) {
        if (-not $members.ContainsKey($exemptName)) {
            $failures.Add("'$gateName' declares a producer exemption for '$exemptName', which this file does not census as a producer. The exemption has stopped applying and must be re-anchored or deleted with the code it was written for.")
        }
    }
    $producerTotal += @($members.Keys | Where-Object { -not $exemptProducers.Contains($_) }).Count

    # `& $variable` reaches whatever a dispatch table holds, and the table is built from
    # `${function:...}` references. The consumer is censused against the union of those, because the
    # dispatch is by property id at run time and every one of them is reachable there.
    $dispatchable = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($variable in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)) {
        if ($variable.VariablePath.UserPath -match '^function:(.+)$') { [void]$dispatchable.Add($Matches[1]) }
    }

    $consumers = [System.Collections.Generic.List[object]]::new()
    foreach ($assignment in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
        if ($assignment.Left -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
        $rightText = $assignment.Right.Extent.Text
        # The dispatch table itself is not a consumer of what it holds.
        if ($rightText -match '^\s*(\[pscustomobject\]\s*)?@\{') { continue }
        $named = @($members.Keys | Where-Object { (Test-NamesIdentifier -Text $rightText -Identifier $_) -and -not $exemptProducers.Contains($_) })
        $targets = $named
        if ($named.Count -lt 1) {
            if ($rightText -notmatch '&\s*\$\w+') { continue }
            $targets = @($dispatchable | Where-Object { $members.ContainsKey($_) -and -not $exemptProducers.Contains($_) })
        }
        if ($targets.Count -lt 1) { continue }
        # Whether this consumer is itself inside a producer, which is what bounds unit 2 below.
        $enclosing = $assignment.Parent
        while ($enclosing -and $enclosing -isnot [System.Management.Automation.Language.FunctionDefinitionAst]) { $enclosing = $enclosing.Parent }
        $consumers.Add([pscustomobject]@{
                Ast       = $assignment
                Line      = $assignment.Extent.StartLineNumber
                Variable  = $assignment.Left.VariablePath.UserPath
                Right     = $rightText
                Targets   = $targets
                Enclosing = $enclosing
                Nested    = ($null -ne $enclosing)
            })
    }
    $consumerTotal += $consumers.Count

    $declaredMemberExemptions = @($censusGate.memberExemptions)
    $matchedMemberExemptions = @{}

    foreach ($consumer in $consumers) {
        # Reads are counted in the consumer's own scope -- the function it sits in, or everything
        # outside every function for a top-level dispatch -- rather than across the whole file, so a
        # same-named variable in an unrelated function cannot answer for this one.
        $readMembers = [System.Collections.Generic.HashSet[string]]::new()
        $scope = if ($consumer.Enclosing) { $consumer.Enclosing } else { $ast }
        foreach ($memberRead in $scope.FindAll({ param($node) $node -is [System.Management.Automation.Language.MemberExpressionAst] }, $true)) {
            if ($memberRead.Expression -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
            if ($memberRead.Expression.VariablePath.UserPath -cne $consumer.Variable) { continue }
            if (-not $consumer.Enclosing) {
                $inFunction = $memberRead.Parent
                while ($inFunction -and $inFunction -isnot [System.Management.Automation.Language.FunctionDefinitionAst]) { $inFunction = $inFunction.Parent }
                if ($inFunction) { continue }
            }
            [void]$readMembers.Add($memberRead.Member.Extent.Text)
        }

        $offered = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($target in $consumer.Targets) {
            foreach ($member in $members[$target]) {
                [void]$offered.Add($member)
            }
        }

        foreach ($member in ($offered | Sort-Object)) {
            if ($readMembers.Contains($member)) { continue }
            $exemption = @($declaredMemberExemptions | Where-Object {
                    ([string]$_.consumer -ceq $consumer.Right) -and ([string]$_.member -ceq $member)
                })
            if ($exemption.Count -ge 1) {
                $matchedMemberExemptions["$($consumer.Right)|$member"] = $true
                continue
            }
            [void]$reportRows.Add([pscustomobject]@{ Gate = $gateName; Kind = 'member'; Line = $consumer.Line; Text = "`$$($consumer.Variable).$member" })
            if (-not $Report) {
                $failures.Add("'$gateName' line $($consumer.Line): '`$$($consumer.Variable)' is handed a '$member' by its producer and nothing reads it. A return channel with no consumer carries whatever it carries while the run reports the members beside it, which is AZ1: the verdict beside a non-empty ``Errors`` is green, and a population that could not be evaluated at all passed. If the member genuinely has nothing to say at this consumer, declare it in conformance/channel-0.2-return-channels.json with the reason. The consumer is: $($consumer.Right)")
            }
        }
    }

    foreach ($memberExemption in $declaredMemberExemptions) {
        $key = "$([string]$memberExemption.consumer)|$([string]$memberExemption.member)"
        if ($matchedMemberExemptions.ContainsKey($key)) { continue }
        $stillPresent = @($consumers | Where-Object { $_.Right -ceq [string]$memberExemption.consumer }).Count -gt 0
        if ($stillPresent) {
            $failures.Add("'$gateName' declares a member exemption for '$([string]$memberExemption.member)' at a consumer that DOES read it now: $([string]$memberExemption.consumer). The exemption claims the member has nothing to say there and something read it, so the reason recorded with it is no longer true. Delete the exemption.")
        }
        else {
            $failures.Add("'$gateName' declares a member exemption whose consumer no longer occurs in the gate: $([string]$memberExemption.consumer). The measure may still be sound; the exemption is stale and must be re-anchored or deleted with the code it was written for.")
        }
    }

    # ---- Unit 2: script-scope accumulators -------------------------------------------------------

    # BB3. Two write shapes, not one. This unit recognised an accumulator by `.Add(...)` alone, and a
    # hashtable accumulated by `$script:X[$key] = $value` is the same channel written the other way --
    # undeclared, and so unchecked for a consumer, with the gate green. It was found by writing one:
    # BB1's `$script:OptionalReads` records which declared-optional field some input left absent, and
    # this unit reported instead that the declaration for it applied to nothing.
    #
    # That is BA6's class inside the instrument BA6 was raised in, which is the argument against
    # recognising a thing by the syntax someone happened to write, made a second time. The limit that
    # remains is stated rather than closed: a write through an alias, or through a member other than
    # `Add` on a collection type that has one, is still invisible here.
    $accumulators = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($invocation in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.InvokeMemberExpressionAst] }, $true)) {
        if ([string]$invocation.Member.Extent.Text -cne 'Add') { continue }
        if ($invocation.Expression -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
        if ($invocation.Expression.VariablePath.UserPath -notmatch '^script:(.+)$') { continue }
        [void]$accumulators.Add($Matches[1])
    }
    foreach ($assignment in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
        $target = $assignment.Left
        if ($target -is [System.Management.Automation.Language.ConvertExpressionAst]) { $target = $target.Child }
        if ($target -isnot [System.Management.Automation.Language.IndexExpressionAst]) { continue }
        if ($target.Target -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
        if ($target.Target.VariablePath.UserPath -notmatch '^script:(.+)$') { continue }
        [void]$accumulators.Add($Matches[1])
    }

    $declaredAccumulators = @{}
    foreach ($declaredAccumulator in @($censusGate.accumulators)) {
        $declaredAccumulators[[string]$declaredAccumulator.name] = [string]$declaredAccumulator.kind
    }
    foreach ($declaredName in @($declaredAccumulators.Keys)) {
        if (-not $accumulators.Contains($declaredName)) {
            $failures.Add("'$gateName' declares the accumulator '`$script:$declaredName' and nothing in the gate adds to it. The declaration has stopped applying and must be re-anchored or deleted with the code it was written for.")
        }
        if (@('per-evaluation', 'cumulative') -notcontains $declaredAccumulators[$declaredName]) {
            $failures.Add("'$gateName' declares the accumulator '`$script:$declaredName' as '$($declaredAccumulators[$declaredName])', which is neither 'per-evaluation' nor 'cumulative'.")
        }
    }

    foreach ($accumulator in ($accumulators | Sort-Object)) {
        if (-not $declaredAccumulators.ContainsKey($accumulator)) {
            $failures.Add("'$gateName' writes to '`$script:$accumulator' and conformance/channel-0.2-return-channels.json does not declare it. An accumulator is a return channel whose consumer is the code around the call rather than the call's own result, and one nobody declared is one nobody checked a consumer for.")
            continue
        }
        if ($declaredAccumulators[$accumulator] -cne 'per-evaluation') {
            # Cumulative: read once over the whole run, so a `Clear()` anywhere would silently
            # discard part of what it accumulated.
            foreach ($invocation in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.InvokeMemberExpressionAst] }, $true)) {
                if ([string]$invocation.Member.Extent.Text -cne 'Clear') { continue }
                if ($invocation.Expression -isnot [System.Management.Automation.Language.VariableExpressionAst]) { continue }
                if ($invocation.Expression.VariablePath.UserPath -cne "script:$accumulator") { continue }
                $failures.Add("'$gateName' line $($invocation.Extent.StartLineNumber): '`$script:$accumulator' is declared cumulative and is cleared here. A cumulative accumulator is read once over the whole run, so a clear discards part of what it was told and the read then reports the remainder as the whole.")
            }
            continue
        }

        foreach ($consumer in $consumers) {
            # Only where one evaluation begins. A per-evaluation accumulator is scoped to one
            # evaluation, and a consumer inside a producer is INSIDE that evaluation: clearing there
            # would discard what the enclosing evaluator had already been told, which is the opposite
            # of the defect this unit is about. The boundary is therefore the top-level dispatch.
            if ($consumer.Nested) { continue }
            $block = $consumer.Ast.Parent
            while ($block -and $block -isnot [System.Management.Automation.Language.StatementBlockAst]) { $block = $block.Parent }
            # A `try`, `catch` or `finally` body is not the boundary of an evaluation; it is error
            # handling wrapped around one. The sweep's dispatch sits in a `try` whose `finally`
            # restores the dropped field, so stopping at that block would put the clear before it and
            # the drain after it outside the region and report a consumer that does both as doing
            # neither. Walked past for the same reason the coverage measure exempts a `catch`
            # structurally rather than by listing the ones that exist today.
            while ($block -and $block.Parent -and
                (($block.Parent -is [System.Management.Automation.Language.TryStatementAst]) -or
                 ($block.Parent -is [System.Management.Automation.Language.CatchClauseAst]))) {
                $block = $block.Parent.Parent
                while ($block -and $block -isnot [System.Management.Automation.Language.StatementBlockAst]) { $block = $block.Parent }
            }
            if (-not $block) { continue }
            $clearedBefore = $false
            $readAfter = $false
            foreach ($use in $block.FindAll({ param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)) {
                if ($use.VariablePath.UserPath -cne "script:$accumulator") { continue }
                $isClear = ($use.Parent -is [System.Management.Automation.Language.InvokeMemberExpressionAst]) -and
                    ([string]$use.Parent.Member.Extent.Text -ceq 'Clear')
                if ($use.Extent.StartOffset -lt $consumer.Ast.Extent.StartOffset) {
                    if ($isClear) { $clearedBefore = $true }
                    continue
                }
                if ($use.Extent.StartOffset -ge $consumer.Ast.Extent.EndOffset -and -not $isClear) { $readAfter = $true }
            }
            if ($clearedBefore -and $readAfter) { continue }
            $missing = if (-not $clearedBefore -and -not $readAfter) { 'neither cleared before it nor read after it' }
                elseif (-not $clearedBefore) { 'not cleared before it' }
                else { 'not read after it' }
            [void]$reportRows.Add([pscustomobject]@{ Gate = $gateName; Kind = 'accumulator'; Line = $consumer.Line; Text = "`$script:$accumulator $missing" })
            if (-not $Report) {
                $failures.Add("'$gateName' line $($consumer.Line): '`$script:$accumulator' is declared per-evaluation and is $missing at this consumer. It is how an obligation says the input does not publish a field it read, so a consumer that does not clear it attributes the previous call's report to this one, and one that does not read it cannot tell a verdict about a realization from a verdict about a silent input -- which is AU2. The consumer is: $($consumer.Right)")
            }
        }
    }
}

if ($Report) {
    if ($reportRows.Count -lt 1) { Write-Host 'Every member a producer returns has a consumer, and every per-evaluation accumulator is cleared and drained at each of them.' }
    else { $reportRows | Sort-Object Gate, Line | Format-Table -AutoSize | Out-String | Write-Host }
    exit 0
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$memberExemptionCount = @($censusGates | ForEach-Object { $_.memberExemptions } | Where-Object { $_ }).Count
$producerExemptionCount = @($censusGates | ForEach-Object { $_.producerExemptions } | Where-Object { $_ }).Count
Write-Host "Channel 0.2 return-channel census passed: $producerTotal producers and $consumerTotal consumers across $($censusGates.Count) gates, with $producerExemptionCount declared producer exemptions and $memberExemptionCount declared member exemptions."
