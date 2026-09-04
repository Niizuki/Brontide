[CmdletBinding()]
param(
    # Report every never-evaluated construct with its trace evidence instead of comparing against the
    # declared exemptions, for working on this file or on a gate it covers.
    [switch]$Report
)

$ErrorActionPreference = 'Stop'

# Channel 0.2 gate coverage.
#
# AR2 of the verification foundation plan's condition-4 work, and the instrument the AQ pass built,
# used once, and did not keep -- which is section 1.1 of that plan happening a second time. AO3 kept
# the probes because three passes had rebuilt them from prose; this keeps the coverage measure for
# the same reason, one level up again. AT4 added a second unit inside the conditions it already
# covered, and AT7 settled which gates are worth covering at all.
#
# WHAT IT MEASURES. A guard whose key has expired does not announce itself. Its comment still reads
# correctly, its code is still there, and both gates stay green -- that is AP1, and the AQ family is
# five more of it. But it has one property that is mechanically visible: **it never runs**. So each
# gate is executed under a line trace, the executed line numbers are collected, and every conditional
# in the script's syntax tree whose line never appears is reported.
#
# WHY THE UNIT IS A CONDITION AND NOT A STATEMENT. A passing gate is supposed to skip its failure
# bodies: `if (bad) { fail }` traces its condition and never enters the block, and a measure over
# statements reports every check in the file. A measure over conditions reports only the checks whose
# condition was never REACHED, which is exactly "this check did not run". The first draft of this
# file measured statements, reported 179 of them across the three gates, and would have been
# abandoned as noise within a cycle.
#
# WHY A `foreach` COUNTS AS ONE. A loop whose body never runs iterated zero times, so the collection
# its key selects is empty. That is AQ1 exactly: a column was inserted into a table, every row
# stopped matching, and the loop over closure-review families quietly had nothing in it for three
# cycles.
#
# THE SECOND UNIT, AND WHY IT IS AN OPERAND THAT WAS NEVER EVALUATED. AR1 was a property clause that
# no declared input reached, found by hand after this file said the enclosing condition ran. A
# condition can run while an operand inside it never does, because `-and` and `-or` short-circuit, and
# an operand no input reaches is a check that could be deleted outright with every gate green -- AR1's
# shape one level down, and the thing the AR review named as this file's limit. So each leaf operand
# of every `-and`/`-or`/`-xor` expression is traced too, and one is reported when the expression
# around it WAS evaluated and it was not.
#
# The qualifier is the whole measure. Reporting every operand that never *decided* an outcome reports
# 138 of 247 across these gates, nearly all of them null checks and length checks that are always true
# on well-formed input; that is the statement-level draft again. Reporting an operand the enclosing
# expression never reached is the same choice as conditions over statements, one level down, and it
# reports nine across the five gates AT4 measured, eight in the three covered here.
#
# THE LIMIT, STATED BECAUSE THE NEXT PASS SHOULD NOT TRUST THIS FILE FURTHER THAN IT GOES. This finds
# a condition that is never evaluated and an operand that is never evaluated. It does not find a
# condition or operand that is evaluated and cannot fail -- an operand that runs, always takes the
# same value, and could be deleted without changing any observed verdict is invisible here, and there
# are 124 of those. That class includes **AQ5**, an assertion whose extent is too small, which was
# found by reading the windows this instrument pointed at rather than by the instrument. Coverage is a
# floor and not a proof.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$exemptionsPath = Join-Path $repositoryRoot 'conformance\channel-0.2-coverage-exemptions.json'
$failures = [System.Collections.Generic.List[string]]::new()

# WHAT THIS FILE DOES NOT COVER, AND WHY -- AT7. It covers the three gates that check the design
# package, and not the guard harness or itself. Both were measured once, under AT4, and each turned
# out to be nearly covered already: the harness holds four constructs a passing run cannot reach and
# this file three, all of them failure paths or the `-Report` branch. Neither is covered on every
# commit, because covering either means RUNNING it here, and both are shaped so that running them is
# what costs -- the harness runs seventy-three probes, and tracing this file traces the syntax-tree
# walks below, where the predicate is a script block invoked once per node. Measured, all in verifying
# mode: covering both took this gate from 77 seconds to 652 and the repository gate past its
# thirty-minute ceiling; not covering them leaves it at 103.
#
# That is a real loss and it is stated rather than absorbed. AO3's argument for keeping the probe
# corpus was that an unmeasured guard rots quietly, and these two are now unmeasured guards. What
# holds them is that both are small, both were read and measured once here, and the corpus covers the
# harness from the other side, since every probe is a run of it. A pass that finds a rotted check in
# either should reopen this trade rather than treat it as settled.

if (-not (Test-Path -LiteralPath $exemptionsPath)) {
    Write-Host "FAIL: the coverage exemption declaration does not exist: '$exemptionsPath'."
    exit 1
}

try { $exemptionFile = Get-Content -Raw -LiteralPath $exemptionsPath -Encoding UTF8 | ConvertFrom-Json }
catch { Write-Host "FAIL: invalid JSON in '$exemptionsPath': $($_.Exception.Message)"; exit 1 }

$coveredGates = @($exemptionFile.gates)
if ($coveredGates.Count -lt 1) {
    Write-Host 'FAIL: the coverage exemption declaration names no gate to cover.'
    exit 1
}

# A DIRTY TREE IS REFUSED, on the rule the probe corpus already applies to a path it edits, and for a
# sharper reason. Several checks in these gates are guarded on the repository's committed state -- the
# review-target pin compares design-artifact blob hashes and skips itself outright while a design
# artifact has uncommitted edits, which is correct, since comparing a pin against a tree nobody has
# read answers nothing. Measured on a dirty tree those checks read as never evaluated, and the measure
# reports a defect that is really the author's own edit in progress.
#
# That matters more than an inconvenience. A measure that cries wolf while someone is working is a
# measure that gets an exemption written for it, and an exemption is permanent where the dirty tree
# was not. So this refuses rather than misreports. `-Report` still works mid-edit, because it states
# what it found without asserting that the finding is a defect.
#
# AT6 SCOPED IT TO THE PATHS THE REASON NAMES. This refused any dirty path in the repository, and the
# reason above is about design artifacts alone. The difference was not academic: the guard harness
# mutates a file before running the gate a probe names, so every probe pointed at this file was
# answered by this refusal instead of by the rule it claims to test -- `AR2-a` had been green that way
# since AR2, and the three probes AT4 added inherited it before they were ever run. A guard that
# cannot be reached by its own probe is AO1's class, and this is the reverse: a probe that cannot
# reach its own guard.
#
# So the scope is the directory that holds the design artifacts and the review policy the pin reads.
# It is the directory rather than the eleven names, because a list of today's artifacts is AN2. An
# uncommitted conformance declaration or gate is left alone: measuring against one is what a probe is
# for, and the pin check does not read either.
#
# The check runs before the operand measure writes anything, which is why an instrumented copy in
# build/ never trips it -- including in the marked child, which starts while this process has not yet
# written one.
if (-not $Report) {
    $gitAvailable = Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')
    if ($gitAvailable) {
        $dirty = & git -C $repositoryRoot status --porcelain -- 'docs/future/channel' 2>$null
        if ($LASTEXITCODE -eq 0 -and $dirty) {
            Write-Host "FAIL: a Channel 0.2 design artifact has uncommitted changes, and coverage measured on one understates itself -- the review-target pin check skips while a design artifact is uncommitted, so it would read here as a check that never runs. Commit first, or use -Report to see what is uncovered without the verdict. Uncommitted: $(($dirty | ForEach-Object { $_.Trim() }) -join '; ')"
            exit 1
        }
    }
}

function Invoke-GateChild {
    param(
        [Parameter(Mandatory = $true)][string]$GatePath,
        [Parameter(Mandatory = $true)][string]$OutputFile,
        [switch]$Trace,
        [hashtable]$Environment = @{},
        [string[]]$Arguments = @()
    )

    # A CHILD PROCESS under `Set-PSDebug -Trace 1`, for the reason the probe harness runs gates as
    # children: a gate reports through `exit` and through the error stream, and an in-scope call under
    # this file's `Stop` preference turns a correctly failing gate into an error here. The trace goes
    # to a temporary file rather than through the pipeline so a gate's own output cannot be mistaken
    # for trace lines.
    $previousEnvironment = @{}
    foreach ($name in $Environment.Keys) {
        $previousEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name)
        [System.Environment]::SetEnvironmentVariable($name, $Environment[$name])
    }
    try {
        # AW1: a covered gate may need arguments to reach its own constructs. The properties gate
        # generates vectors on every run, and tracing a hundred of them costs more than the whole of
        # this measure -- so the declaration names a small count that reaches every construct in the
        # generated block without paying for a population this file is not measuring.
        $argumentText = if ($Arguments.Count -gt 0) { ' ' + ($Arguments -join ' ') } else { '' }
        $command = if ($Trace) { "Set-PSDebug -Trace 1; & '$GatePath'$argumentText" } else { "& '$GatePath'$argumentText" }
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $command 2>&1 |
                Out-File -FilePath $OutputFile -Encoding utf8
            return $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $previousPreference }
    }
    finally {
        foreach ($name in $previousEnvironment.Keys) {
            [System.Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name])
        }
    }
}

function Get-ExecutedLines {
    param(
        [Parameter(Mandatory = $true)][string]$GatePath,
        [hashtable]$Environment = @{},
        [string[]]$Arguments = @()
    )

    $traceFile = [System.IO.Path]::GetTempFileName()
    try {
        $gateExit = Invoke-GateChild -GatePath $GatePath -OutputFile $traceFile -Trace -Environment $Environment -Arguments $Arguments
        $executed = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($traceLine in Get-Content -LiteralPath $traceFile) {
            if ($traceLine -match '^DEBUG:\s+(\d+)\+') { [void]$executed.Add([int]$Matches[1]) }
        }
        return @{ Executed = $executed; ExitCode = $gateExit }
    }
    finally { Remove-Item -LiteralPath $traceFile -Force -ErrorAction SilentlyContinue }
}

function Test-InsideCatch {
    param([Parameter(Mandatory = $true)]$Node)

    # Structural exemption, not a declared one. Every statement in a `catch` is an error path, and a
    # gate that reaches one has already failed at something this measure is not about. Expressed as a
    # walk to the root rather than as a list of the catch blocks that exist today, because AN2 is a
    # list that held eight of nine.
    $parent = $Node.Parent
    while ($parent) {
        if ($parent -is [System.Management.Automation.Language.CatchClauseAst]) { return $true }
        $parent = $parent.Parent
    }
    return $false
}

function Test-FailureReportingLoop {
    param([Parameter(Mandatory = $true)]$Node)

    # The other structural exemption: `foreach ($f in $failures) { ... }` is how every gate here
    # prints what it found, and a green gate has found nothing. Keyed to the collection the loop walks
    # rather than to the line it sits on.
    if ($Node -isnot [System.Management.Automation.Language.ForEachStatementAst]) { return $false }
    return ($Node.Condition.Extent.Text -match '^\$(failures|errors)$')
}

$script:LogicalOperators = @(
    [System.Management.Automation.Language.TokenKind]::And,
    [System.Management.Automation.Language.TokenKind]::Or,
    [System.Management.Automation.Language.TokenKind]::Xor)

function Test-LogicalExpression {
    param($Node)
    return (($Node -is [System.Management.Automation.Language.BinaryExpressionAst]) -and ($Node.Operator -in $script:LogicalOperators))
}

function Split-LogicalOperand {
    param($Node, $Accumulator)

    # The leaves of one logical expression tree: `a -and b -and c` parses as `(a -and b) -and c`, and
    # the operands a reader sees are the three, not the two an unflattened walk would report.
    if (Test-LogicalExpression -Node $Node) {
        Split-LogicalOperand -Node $Node.Left -Accumulator $Accumulator
        Split-LogicalOperand -Node $Node.Right -Accumulator $Accumulator
        return
    }
    $Accumulator.Add($Node)
}

function Get-LogicalOperand {
    param([Parameter(Mandatory = $true)]$Ast)

    # A logical expression can sit inside another one's operand -- `-not ($a -and $b)` is a leaf of the
    # outer expression and a root of its own. The outer operand is what a logical operator directly
    # consumes, so it is the one kept, and anything contained in a kept operand is dropped.
    # The predicate is inlined rather than calling the helper beside it. `FindAll` runs it once per
    # node of a syntax tree that is tens of thousands of nodes for the design gate, and a PowerShell
    # function call per node took this measure from seconds to minutes -- which showed up as a
    # thirty-minute repository gate rather than as anything visible here.
    $logical = $script:LogicalOperators
    $roots = @($Ast.FindAll({
                param($candidate)
                ($candidate -is [System.Management.Automation.Language.BinaryExpressionAst]) -and
                ($candidate.Operator -in $logical) -and
                -not (($candidate.Parent -is [System.Management.Automation.Language.BinaryExpressionAst]) -and
                    ($candidate.Parent.Operator -in $logical))
            }.GetNewClosure(), $true))

    $operands = [System.Collections.Generic.List[object]]::new()
    foreach ($root in $roots) {
        $leaves = [System.Collections.Generic.List[object]]::new()
        Split-LogicalOperand -Node $root -Accumulator $leaves
        foreach ($leaf in $leaves) {
            $operands.Add([pscustomobject]@{ Ast = $leaf; RootStart = $root.Extent.StartOffset; RootLine = $root.Extent.StartLineNumber; RootText = $root.Extent.Text })
        }
    }

    $ordered = @($operands | Sort-Object { $_.Ast.Extent.StartOffset }, { - $_.Ast.Extent.EndOffset })
    $kept = [System.Collections.Generic.List[object]]::new()
    $coveredTo = -1
    foreach ($candidate in $ordered) {
        if ($candidate.Ast.Extent.StartOffset -lt $coveredTo) { continue }
        $kept.Add($candidate)
        $coveredTo = $candidate.Ast.Extent.EndOffset
    }
    return $kept
}

function Get-EvaluatedOperand {
    param(
        [Parameter(Mandatory = $true)][string]$GatePath,
        [Parameter(Mandatory = $true)]$Operands,
        [Parameter(Mandatory = $true)][string]$GateText)

    # The operand trace needs the operand's value at the moment the expression evaluated it, and a
    # line trace cannot supply that: both operands of an `-and` sit on one line, and the short-circuit
    # this measure is about leaves no line of its own. So each operand is wrapped in a recording call
    # and the gate is run from an instrumented copy.
    #
    # Two properties of the copy are load-bearing and neither is decoration. It keeps the gate's line
    # count exactly, because the design verifier measures its own length against the number the
    # verification plan states and an instrumented copy that grew would fail that check rather than
    # measure it. And it lives beside the gate, because every gate resolves the repository root from
    # `$PSScriptRoot`; a copy in the system temp directory would verify a repository that is not this
    # one.
    #
    # The operand is wrapped rather than rewritten: `-and` and `-or` coerce each operand to `[bool]`
    # anyway, so `(record (bool X))` is the value the operator would have used, evaluated where the
    # gate evaluates it. That matters for `-match`, which sets `$Matches` in the scope that runs it;
    # an argument expression runs in the caller's scope and a script block would not.
    #
    # It records an operand the FIRST time it is evaluated and never again. The measure asks whether
    # an operand was reached at all, so the second record answers a question nobody asked -- and the
    # first draft wrote one line per evaluation, which is 13,871 file opens for one clean run of these
    # gates and made the instrumented run cost more than everything else in this file put together.
    $recorder = "function brOperand { param(`$i,`$v) if (`$env:BRONTIDE_CHANNEL_02_OPERAND_LOG) { if (`$null -eq `$script:brOperandSeen) { `$script:brOperandSeen = @{} } ; if (-not `$script:brOperandSeen.ContainsKey(`$i)) { `$script:brOperandSeen[`$i] = `$true ; try { [System.IO.File]::AppendAllText(`$env:BRONTIDE_CHANNEL_02_OPERAND_LOG, `$i + [char]10) } catch { } } } ; `$v }"

    $text = $GateText
    $identified = [System.Collections.Generic.List[object]]::new()
    $ordinal = 0
    foreach ($operand in $Operands) {
        $ordinal++
        $identified.Add([pscustomobject]@{
                Id        = "operand-$ordinal"
                Start     = $operand.Ast.Extent.StartOffset
                End       = $operand.Ast.Extent.EndOffset
                Line      = $operand.Ast.Extent.StartLineNumber
                Text      = $operand.Ast.Extent.Text
                RootStart = $operand.RootStart
                RootLine  = $operand.RootLine
                RootText  = $operand.RootText
                InCatch   = (Test-InsideCatch -Node $operand.Ast)
            })
    }

    foreach ($entry in ($identified | Sort-Object Start -Descending)) {
        $text = $text.Substring(0, $entry.Start) + "(brOperand '$($entry.Id)' ([bool]($($entry.Text))))" + $text.Substring($entry.End)
    }

    $anchor = "`$ErrorActionPreference = 'Stop'"
    $anchorIndex = $text.IndexOf($anchor, [System.StringComparison]::Ordinal)
    if ($anchorIndex -lt 0) { return @{ Error = "the gate states no '$anchor' line, which is where the operand recorder is placed without moving a line." } }
    $lineEnd = $text.IndexOf("`n", $anchorIndex, [System.StringComparison]::Ordinal)
    if ($lineEnd -lt 0) { return @{ Error = 'the gate ends at its error preference, so there is no blank line to place the operand recorder on.' } }
    # Written over the blank line that follows, so the copy has the same number of lines as the gate.
    $blankEnd = $text.IndexOf("`n", $lineEnd + 1, [System.StringComparison]::Ordinal)
    if ($blankEnd -lt 0 -or $text.Substring($lineEnd + 1, $blankEnd - $lineEnd - 1).Trim().Length -gt 0) {
        return @{ Error = 'the line after the gate error preference is not blank, and the operand recorder is placed there so the copy keeps the line count the design verifier measures.' }
    }
    $text = $text.Substring(0, $lineEnd + 1) + $recorder + $text.Substring($blankEnd)

    if (([regex]::Matches($text, "`n")).Count -ne ([regex]::Matches($GateText, "`n")).Count) {
        return @{ Error = 'instrumenting the gate moved its line count, and a gate that measures its own length would then fail this measure rather than answer it.' }
    }

    $copyPath = Join-Path (Split-Path -Parent $GatePath) ("operand-probe-" + [guid]::NewGuid().ToString('n') + ".ps1")
    $logPath = [System.IO.Path]::GetTempFileName()
    $outputPath = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($copyPath, $text, (New-Object System.Text.UTF8Encoding($false)))
        Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
        $exitCode = Invoke-GateChild -GatePath $copyPath -OutputFile $outputPath -Environment @{ 'BRONTIDE_CHANNEL_02_OPERAND_LOG' = $logPath }

        $evaluated = [System.Collections.Generic.HashSet[string]]::new()
        if (Test-Path -LiteralPath $logPath) {
            foreach ($logLine in Get-Content -LiteralPath $logPath) {
                $recorded = $logLine.Trim()
                if (-not $recorded) { continue }
                [void]$evaluated.Add($recorded)
            }
        }
        return @{ Evaluated = $evaluated; ExitCode = $exitCode; Operands = $identified }
    }
    finally {
        Remove-Item -LiteralPath $copyPath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $outputPath -Force -ErrorAction SilentlyContinue
    }
}

$reportRows = [System.Collections.Generic.List[object]]::new()
$operandGateCount = 0

foreach ($coveredGate in $coveredGates) {
    $gateName = [string]$coveredGate.gate
    $gatePath = Join-Path $repositoryRoot "build\$gateName"
    if (-not (Test-Path -LiteralPath $gatePath)) {
        $failures.Add("The coverage declaration names the gate '$gateName' and no such file exists in build/.")
        continue
    }

    $gateArguments = @($coveredGate.arguments | Where-Object { $_ })
    $run = Get-ExecutedLines -GatePath $gatePath -Arguments $gateArguments
    if ($run.ExitCode -ne 0) {
        # Coverage of a failing gate measures nothing: the run stopped early, so every construct after
        # the failure reads as never evaluated. The gate is fixed first and this file is run after.
        $failures.Add("The gate '$gateName' exits $($run.ExitCode), so its coverage cannot be measured -- a gate that stops early leaves every check after the stop looking dead. Fix the gate, then run this.")
        continue
    }

    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($gatePath, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors -and $parseErrors.Count -gt 0) {
        $failures.Add("The gate '$gateName' does not parse, so its coverage cannot be measured: $($parseErrors[0].Message)")
        continue
    }
    $gateLines = Get-Content -LiteralPath $gatePath

    $neverEvaluated = [System.Collections.Generic.List[object]]::new()

    foreach ($node in $ast.FindAll({ param($candidate) $candidate -is [System.Management.Automation.Language.IfStatementAst] }, $true)) {
        $line = $node.Extent.StartLineNumber
        if ($run.Executed.Contains($line)) { continue }
        if (Test-InsideCatch -Node $node) { continue }
        [void]$neverEvaluated.Add(@{ Kind = 'if'; Line = $line; Text = $gateLines[$line - 1].Trim() })
    }

    foreach ($node in $ast.FindAll({ param($candidate) $candidate -is [System.Management.Automation.Language.ForEachStatementAst] }, $true)) {
        $bodyStatements = @($node.Body.Statements)
        if ($bodyStatements.Count -lt 1) { continue }
        if ($run.Executed.Contains($bodyStatements[0].Extent.StartLineNumber)) { continue }
        if (Test-InsideCatch -Node $node) { continue }
        if (Test-FailureReportingLoop -Node $node) { continue }
        $line = $node.Extent.StartLineNumber
        [void]$neverEvaluated.Add(@{ Kind = 'foreach'; Line = $line; Text = $gateLines[$line - 1].Trim() })
    }

    $declared = @($coveredGate.exemptions)
    $matched = @{}

    foreach ($construct in $neverEvaluated) {
        $exemption = @($declared | Where-Object { [string]$_.anchor -ceq $construct.Text })
        if ($exemption.Count -ge 1) {
            $matched[[string]$exemption[0].anchor] = $true
            continue
        }
        [void]$reportRows.Add([pscustomobject]@{ Gate = $gateName; Kind = $construct.Kind; Line = $construct.Line; Text = $construct.Text })
        if (-not $Report) {
            $failures.Add("'$gateName' line $($construct.Line): this $($construct.Kind) is never evaluated by a passing run, so the check it guards did not run. Either an input that reaches it is missing, or its key stopped selecting anything when the work moved -- which is AP1's class and five of the six AQ findings. If it is correctly unreachable, declare it in conformance/channel-0.2-coverage-exemptions.json with the reason. The construct is: $($construct.Text)")
        }
    }

    # An exemption that no longer matches anything is deleted rather than left standing. The probe
    # corpus made a rotted anchor a hard failure for exactly this reason: an entry that stops applying
    # is an entry that has stopped saying anything, and it reads as coverage that exists.
    foreach ($exemption in $declared) {
        $anchor = [string]$exemption.anchor
        if ($matched.ContainsKey($anchor)) { continue }
        $stillPresent = @($gateLines | Where-Object { $_.Trim() -ceq $anchor }).Count -gt 0
        if ($stillPresent) {
            $failures.Add("'$gateName' declares a coverage exemption for a construct that IS evaluated now: '$anchor'. The exemption claims the construct cannot be reached by a passing run and it was reached, so the reason recorded with it is no longer true. Delete the exemption.")
        }
        else {
            $failures.Add("'$gateName' declares a coverage exemption whose construct no longer occurs in the gate: '$anchor'. The measure may still be sound; the exemption is stale and must be re-anchored or deleted with the code it was written for.")
        }
    }

    if (-not $coveredGate.measureOperands) { continue }
    $operandGateCount++

    $gateText = [System.IO.File]::ReadAllText($gatePath)
    $operands = Get-LogicalOperand -Ast ([System.Management.Automation.Language.Parser]::ParseInput($gateText, [ref]$tokens, [ref]$parseErrors))
    $operandRun = Get-EvaluatedOperand -GatePath $gatePath -Operands $operands -GateText $gateText
    if ($operandRun.Error) {
        $failures.Add("The operand coverage of '$gateName' could not be measured: $($operandRun.Error)")
        continue
    }
    if ($operandRun.ExitCode -ne 0) {
        $failures.Add("Instrumented for operand coverage, the gate '$gateName' exits $($operandRun.ExitCode) where it passes uninstrumented. The instrumentation wraps each operand in a call that returns the operand's own boolean, so a verdict that changes under it is a defect in this measure rather than in that gate.")
        continue
    }

    $declaredOperands = @($coveredGate.operandExemptions)
    $matchedOperands = @{}
    $evaluatedRoots = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($operandEntry in $operandRun.Operands) {
        if ($operandRun.Evaluated.Contains($operandEntry.Id)) { [void]$evaluatedRoots.Add($operandEntry.RootStart) }
    }

    foreach ($operandEntry in $operandRun.Operands) {
        if ($operandRun.Evaluated.Contains($operandEntry.Id)) { continue }
        if ($operandEntry.InCatch) { continue }
        # Structural exemption, and the one that makes this unit worth having. An operand whose whole
        # expression was never evaluated is not an operand finding: the statement around it did not
        # run, which is the condition measure's subject and is already reported or exempted there.
        # Reporting it here would report the facts gate's `-Apply` path five more times under a
        # second name.
        if (-not $evaluatedRoots.Contains($operandEntry.RootStart)) { continue }

        $operandExemption = @($declaredOperands | Where-Object {
                ([string]$_.operand -ceq $operandEntry.Text) -and ([string]$_.within -ceq $operandEntry.RootText)
            })
        if ($operandExemption.Count -ge 1) {
            $matchedOperands["$([string]$operandExemption[0].within)|$([string]$operandExemption[0].operand)"] = $true
            continue
        }

        [void]$reportRows.Add([pscustomobject]@{ Gate = $gateName; Kind = 'operand'; Line = $operandEntry.Line; Text = $operandEntry.Text })
        if (-not $Report) {
            $failures.Add("'$gateName' line $($operandEntry.Line): this operand is never evaluated by a passing run although the expression around it is, so short-circuiting reached it in no input and it could be deleted with every gate green. That is AR1's shape one level down. If the missing input is the defect it belongs in the vector corpus rather than here; if the operand is correctly unreachable, declare it in conformance/channel-0.2-coverage-exemptions.json with the reason. The operand is: $($operandEntry.Text) -- within: $($operandEntry.RootText)")
        }
    }

    foreach ($operandExemption in $declaredOperands) {
        $key = "$([string]$operandExemption.within)|$([string]$operandExemption.operand)"
        if ($matchedOperands.ContainsKey($key)) { continue }
        $stillPresent = $gateText.IndexOf([string]$operandExemption.within, [System.StringComparison]::Ordinal) -ge 0
        if ($stillPresent) {
            $failures.Add("'$gateName' declares an operand exemption for an operand that IS evaluated now: '$([string]$operandExemption.operand)'. The exemption claims no input reaches it and one did, so the reason recorded with it is no longer true. Delete the exemption.")
        }
        else {
            $failures.Add("'$gateName' declares an operand exemption whose expression no longer occurs in the gate: '$([string]$operandExemption.within)'. The measure may still be sound; the exemption is stale and must be re-anchored or deleted with the code it was written for.")
        }
    }
}

if ($Report) {
    if ($reportRows.Count -lt 1) { Write-Host 'Every conditional and operand in the covered gates is evaluated by a passing run.' }
    else { $reportRows | Sort-Object Gate, Line | Format-Table -AutoSize | Out-String | Write-Host }
    exit 0
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$exemptionCount = @($coveredGates | ForEach-Object { $_.exemptions } | Where-Object { $_ }).Count
$operandExemptionCount = @($coveredGates | ForEach-Object { $_.operandExemptions } | Where-Object { $_ }).Count
Write-Host "Channel 0.2 gate coverage passed: every conditional in $($coveredGates.Count) gates and every operand in $operandGateCount of them is evaluated by a passing run, with $exemptionCount declared condition exemptions and $operandExemptionCount declared operand exemptions."
