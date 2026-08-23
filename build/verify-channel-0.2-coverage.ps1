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
# the same reason, one level up again.
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
# THE LIMIT, STATED BECAUSE THE NEXT PASS SHOULD NOT TRUST THIS FILE FURTHER THAN IT GOES. This finds
# a condition that is never evaluated. It does not find a condition that is evaluated and cannot fail,
# and it does not find an assertion whose extent is too small -- which is **AQ5**, and which was found
# by reading the windows this instrument pointed at rather than by the instrument. Coverage is a floor
# and not a proof.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$exemptionsPath = Join-Path $repositoryRoot 'conformance\channel-0.2-coverage-exemptions.json'
$failures = [System.Collections.Generic.List[string]]::new()

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
if (-not $Report) {
    $gitAvailable = Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')
    if ($gitAvailable) {
        $dirty = & git -C $repositoryRoot status --porcelain 2>$null
        if ($LASTEXITCODE -eq 0 -and $dirty) {
            Write-Host "FAIL: the working tree has uncommitted changes, and coverage measured on one understates itself -- the review-target pin check skips while a design artifact is uncommitted, so it would read here as a check that never runs. Commit first, or use -Report to see what is uncovered without the verdict. Uncommitted: $(($dirty | ForEach-Object { $_.Trim() }) -join '; ')"
            exit 1
        }
    }
}

function Get-ExecutedLines {
    param([Parameter(Mandatory = $true)][string]$GatePath)

    # A CHILD PROCESS under `Set-PSDebug -Trace 1`, for the reason the probe harness runs gates as
    # children: a gate reports through `exit` and through the error stream, and an in-scope call under
    # this file's `Stop` preference turns a correctly failing gate into an error here. The trace goes
    # to a temporary file rather than through the pipeline so a gate's own output cannot be mistaken
    # for trace lines.
    $traceFile = [System.IO.Path]::GetTempFileName()
    try {
        $command = "Set-PSDebug -Trace 1; & '$GatePath'"
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $command 2>&1 |
                Out-File -FilePath $traceFile -Encoding utf8
            $gateExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $previousPreference }

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

$reportRows = [System.Collections.Generic.List[object]]::new()

foreach ($coveredGate in $coveredGates) {
    $gateName = [string]$coveredGate.gate
    $gatePath = Join-Path $repositoryRoot "build\$gateName"
    if (-not (Test-Path -LiteralPath $gatePath)) {
        $failures.Add("The coverage declaration names the gate '$gateName' and no such file exists in build/.")
        continue
    }

    $run = Get-ExecutedLines -GatePath $gatePath
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
}

if ($Report) {
    if ($reportRows.Count -lt 1) { Write-Host 'Every conditional in the covered gates is evaluated by a passing run.' }
    else { $reportRows | Sort-Object Gate, Line | Format-Table -AutoSize | Out-String | Write-Host }
    exit 0
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$exemptionCount = @($coveredGates | ForEach-Object { $_.exemptions } | Where-Object { $_ }).Count
Write-Host "Channel 0.2 gate coverage passed: every conditional in $($coveredGates.Count) gates is evaluated by a passing run, with $exemptionCount declared exemptions."
