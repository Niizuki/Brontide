[CmdletBinding()]
param(
    # Run one probe by id instead of the whole corpus, for working on a single guard.
    [string]$Probe,
    # How many probes to run at once, each in its own copy of the repository.
    #
    # WHY ISOLATION AND NOT LOCKING. A probe edits a real file in the working tree and restores it, so
    # two probes running at once would each restore over the other's mutation and both would report a
    # verdict about a package neither wrote. That is why this corpus has always been serial. The
    # answer is a copy per worker rather than a lock, because the probes are otherwise independent and
    # a lock would serialise exactly the part that costs.
    #
    # WHAT IT COSTS. The repository is 28 MB with its git directory and without build output, and a
    # worker copy measures at 0.6 seconds. A dozen of those is seconds against a corpus measured in
    # minutes.
    #
    # WHAT IT DOES NOT CHANGE. Each worker runs this same file over a subset of ids, in a full copy
    # that has its own `.git`, so every probe still gets the dirty-tree refusal and the residual check
    # it gets today. One means today's behaviour in this tree with no copying, and is the default so
    # that nothing about a single run changes unless it is asked for.
    [int]$Parallel = 1,
    # Set by the parent when it fans out: a file holding one probe id per line. Not for a person to
    # pass, and a run over a subset is not a run of the corpus.
    #
    # A FILE rather than an argument list, and that is not a style choice. Twenty-five of these ids
    # contain a space -- `D2 W2`, `C7 hold` -- and `powershell -File script -ProbeIds a b c` neither
    # quotes them nor binds them as an array, so the first attempt handed each worker one mangled id
    # and every worker reported nothing. One path is one argument whatever the ids contain.
    [string]$ProbeIdFile
)

$ErrorActionPreference = 'Stop'

# Channel 0.2 guard probes.
#
# AO3 of the verification foundation plan's condition-4 work. The three gates beside this one check
# the design package; nothing checked THEM. Their guards are asserted to fire in prose -- the AM
# review lists its probes in sentences, the AN review re-derived those sentences into mutations, and
# this pass re-derived them a third time and could not set four of them up, because the text they
# anchored on had been corrected in the meantime and no one noticed the probes had rotted.
#
# That is section 1.1 of the plan exactly, one level up. There the finding was that every closure
# reviewer wrote a property evaluator, used it, and threw it away, so the most productive instrument
# the programme had was rebuilt from prose every cycle. The same was true of the probes, and the same
# answer applies: keep the instrument. `conformance/channel-0.2-guard-probes.json` is the corpus and
# this file runs it.
#
# What a probe is, and the boundary that keeps this honest. A probe makes ONE guard's own subject
# present in the package and asserts the verdict that guard must return. It is evidence about a
# guard and never a statement about the design: where a probe and an artifact disagree about what the
# design says, the artifact is right and the probe is the defect. A guard with no probe here is not
# thereby wrong -- it is unmeasured, which is the state this file exists to reduce.
#
# Every mutation is applied to the working tree and undone from bytes read before it, never with
# `git checkout`, and the tree is required to be clean for the paths a probe touches before it runs.
# Both rules are paid for: restoring with git discarded an hour of uncommitted corrections during the
# AN pass, on the setup-failure path where the probe never even ran.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$corpusPath = Join-Path $repositoryRoot 'conformance\channel-0.2-guard-probes.json'
$failures = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $corpusPath)) {
    Write-Host "FAIL: the guard-probe corpus does not exist: '$corpusPath'."
    exit 1
}

try { $corpus = Get-Content -Raw -LiteralPath $corpusPath -Encoding UTF8 | ConvertFrom-Json }
catch { Write-Host "FAIL: invalid JSON in '$corpusPath': $($_.Exception.Message)"; exit 1 }

$probes = @($corpus.probes)
if ($Probe) { $probes = @($probes | Where-Object { $_.id -eq $Probe }) }
if ($ProbeIdFile) {
    if (-not (Test-Path -LiteralPath $ProbeIdFile)) {
        Write-Host "FAIL: this worker was given the probe list '$ProbeIdFile' and no such file exists."
        exit 1
    }
    $assigned = @(Get-Content -LiteralPath $ProbeIdFile | Where-Object { $_.Trim().Length -gt 0 })
    $wanted = [System.Collections.Generic.HashSet[string]]::new([string[]]$assigned, [System.StringComparer]::Ordinal)
    $probes = @($probes | Where-Object { $wanted.Contains([string]$_.id) })
    if ($probes.Count -ne $wanted.Count) {
        Write-Host "FAIL: this worker was given $($wanted.Count) probe ids and the corpus holds $($probes.Count) of them. A worker that silently runs fewer probes than it was given reports a pass for the ones it never ran."
        exit 1
    }
}
if ($probes.Count -lt 1) {
    Write-Host "FAIL: no probe to run$(if ($Probe) { " with id '$Probe'" })."
    exit 1
}

# ---------------------------------------------------------------------------------------------
# The fan-out. A parent with -Parallel greater than one runs no probe itself: it partitions the ids,
# gives each worker its own copy of the repository, and merges what they report.
# ---------------------------------------------------------------------------------------------
if ($Parallel -ne 1 -and -not $ProbeIdFile) {
    $workerCount = if ($Parallel -le 0) { [Environment]::ProcessorCount } else { $Parallel }
    if ($workerCount -gt $probes.Count) { $workerCount = $probes.Count }

    # Dealt round-robin over a cost-ordered list rather than cut into blocks. The corpus is not
    # uniform -- one probe on the coverage measure is minutes where a probe on the owned-fact gate is
    # under a second -- so a block partition leaves one worker holding every expensive probe and the
    # run takes as long as that worker. Ordering by the gate's own cost and dealing one at a time
    # spreads them.
    $gateCost = @{
        'verify-channel-0.2-coverage.ps1'        = 600
        'verify-channel-0.2-properties.ps1'      = 18
        'verify-channel-0.2-design.ps1'          = 4
        'verify-channel-0.2-return-channels.ps1' = 2
        'verify-doc-links.ps1'                   = 2
        'verify-channel-0.2-facts.ps1'           = 1
    }
    $ordered = @($probes | Sort-Object -Property @{ Expression = {
        $named = [string]$_.gate
        if ($gateCost.ContainsKey($named)) { $gateCost[$named] } else { 10 } } } -Descending)

    $buckets = @{}
    foreach ($index in 0..($workerCount - 1)) { $buckets[$index] = [System.Collections.Generic.List[string]]::new() }
    for ($index = 0; $index -lt $ordered.Count; $index++) {
        [void]$buckets[$index % $workerCount].Add([string]$ordered[$index].id)
    }

    # A worker is a COPY, and a copy is only self-contained when `.git` is a directory. In a git
    # worktree `.git` is a file pointing at the main repository's worktree metadata, so the copy's
    # git commands would answer about the original path instead of about the copy -- and two of the
    # things a probe relies on are git answers: the dirty-tree refusal a probe expects a gate to
    # make, and the residual check that catches a probe which failed to restore. Refused rather than
    # worked around, because a worker whose git answers are about somewhere else reports verdicts
    # nobody can act on.
    $gitPath = Join-Path $repositoryRoot '.git'
    if ((Test-Path -LiteralPath $gitPath) -and -not (Test-Path -LiteralPath $gitPath -PathType Container)) {
        Write-Host "FAIL: -Parallel needs a repository whose '.git' is a directory, and this one is a git worktree, where '.git' is a pointer file. A worker copy of it would answer git questions about the original tree. Run the corpus here without -Parallel, or run it in a clone."
        exit 1
    }

    $workerRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("brontide-probes-" + [guid]::NewGuid().ToString('n'))
    $started = [System.Collections.Generic.List[object]]::new()
    try {
        foreach ($index in 0..($workerCount - 1)) {
            if ($buckets[$index].Count -lt 1) { continue }
            $workerPath = Join-Path $workerRoot "w$index"
            $null = New-Item -ItemType Directory -Path $workerPath -Force
            # `robocopy` rather than `Copy-Item`, for the reason it is always chosen on Windows: it
            # is multi-threaded and it does not walk the tree in PowerShell. Exit codes below 8 are
            # success. Build output is excluded because no gate reads it and it is fifty times the
            # size of everything that matters.
            $null = robocopy $repositoryRoot $workerPath /MIR /NFL /NDL /NJH /NJS /NP /MT:8 /XD bin obj .vs node_modules
            if ($LASTEXITCODE -ge 8) { throw "could not copy the repository to '$workerPath': robocopy exit $LASTEXITCODE." }

            $outputPath = Join-Path $workerRoot "w$index.out"
            $listPath = Join-Path $workerRoot "w$index.ids"
            Set-Content -LiteralPath $listPath -Value $buckets[$index] -Encoding UTF8
            $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
                (Join-Path $workerPath 'build\verify-channel-0.2-guards.ps1'),
                '-ProbeIdFile', $listPath)
            $started.Add([pscustomobject]@{
                Index   = $index
                Ids     = @($buckets[$index])
                Output  = $outputPath
                Process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -NoNewWindow -PassThru -RedirectStandardOutput $outputPath -RedirectStandardError "$outputPath.err"
            })
            # `.Handle` is read so `.ExitCode` is readable after the worker ends; without it the
            # property stays `$null` and the silent-worker guard below can never fire.
            $null = $started[$started.Count - 1].Process.Handle
        }

        $passed = 0
        foreach ($worker in $started) {
            $failuresBefore = $failures.Count
            $worker.Process.WaitForExit()
            $reported = if (Test-Path -LiteralPath $worker.Output) { @(Get-Content -LiteralPath $worker.Output) } else { @() }
            foreach ($errorPath in @("$($worker.Output).err")) {
                if (Test-Path -LiteralPath $errorPath) { $reported += @(Get-Content -LiteralPath $errorPath) }
            }
            $sawVerdict = $false
            foreach ($line in $reported) {
                if ($line -match '^FAIL: (.*)$') { $failures.Add($Matches[1]) }
                elseif ($line -match 'probes returned the verdict their guard owes\.') {
                    $sawVerdict = $true
                    if ($line -match ': ([0-9]+) of ([0-9]+) probes') { $passed += [int]$Matches[1] }
                }
            }
            # A worker that reported neither a failure nor a verdict did not run. Silence from a
            # child is the one outcome a parent must never read as success -- it is AZ1's shape at
            # the process boundary -- and it is reported whatever the worker exited with, because the
            # first version asked for exit 0 as well and a worker that died reported nothing at all.
            if (-not $sawVerdict -and $failures.Count -eq $failuresBefore) {
                $failures.Add("Probe worker $($worker.Index) exited $($worker.Process.ExitCode) and reported neither a verdict nor a failure for the $($worker.Ids.Count) probes it was given: $($worker.Ids -join ', '). A worker that says nothing has not passed.")
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $workerRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    # The count claim below is about the CORPUS, and a parallel run is a run of the whole corpus, so
    # it is checked here too. Returning early past it is how a check stops applying while every gate
    # stays green, which is AP1 and the family this file exists to catch.
    $parallelWorkerCount = $started.Count
}

# A probe corpus is a second surface for the set of gates, so the gate a probe names has to exist.
foreach ($guardProbe in $probes) {
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot "build\$($guardProbe.gate)"))) {
        $failures.Add("Probe '$($guardProbe.id)' names the gate '$($guardProbe.gate)' and no such file exists in build/.")
    }
    if (@('fail', 'pass') -notcontains [string]$guardProbe.expect) {
        $failures.Add("Probe '$($guardProbe.id)' expects '$($guardProbe.expect)', which is neither 'fail' nor 'pass'.")
    }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

function Get-FileBytes {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [System.IO.File]::ReadAllBytes($Path)
}

function Read-Text {
    param([Parameter(Mandatory = $true)][string]$Path)
    # Read and write the exact bytes around the edit: these artifacts differ in byte-order mark and in
    # line ending, and a probe that normalised either would be measuring its own rewrite.
    $bytes = Get-FileBytes -Path $Path
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom) { $text = $text.Substring(1) }
    return @{ Text = $text; HasBom = $hasBom }
}

function Write-Text {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Text, [bool]$HasBom)
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($HasBom)))
}

function Restore-FileBytes {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][byte[]]$Bytes,
        [scriptblock]$WriteBytes = {
            param($TargetPath, $TargetBytes)
            [System.IO.File]::WriteAllBytes($TargetPath, $TargetBytes)
        },
        [scriptblock]$Delay = {
            param($Milliseconds)
            Start-Sleep -Milliseconds $Milliseconds
        }
    )

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            & $WriteBytes $Path $Bytes
            return
        }
        catch [System.IO.IOException] {
            if ($attempt -eq 5) { throw }
            & $Delay (50 * $attempt)
        }
    }
}

$restoreTest = [pscustomobject]@{ Attempts = 0; Bytes = $null }
$restoreTestBytes = [byte[]](0x42, 0x37)
Restore-FileBytes -Path '<restore-self-test>' -Bytes $restoreTestBytes -WriteBytes {
    param($TargetPath, $TargetBytes)
    $restoreTest.Attempts++
    if ($restoreTest.Attempts -lt 3) {
        throw [System.IO.IOException]::new('simulated sharing violation')
    }
    $restoreTest.Bytes = $TargetBytes
} -Delay { param($Milliseconds) }

if ($restoreTest.Attempts -ne 3 -or
    [Convert]::ToBase64String($restoreTest.Bytes) -cne [Convert]::ToBase64String($restoreTestBytes)) {
    throw 'Guard harness restore retry self-test failed.'
}

$gitAvailable = Test-Path -LiteralPath (Join-Path $repositoryRoot '.git')
if (-not $parallelWorkerCount) { $parallelWorkerCount = 0; $passed = 0 }

foreach ($guardProbe in $(if ($parallelWorkerCount -gt 0) { @() } else { $probes })) {
    $paths = @($guardProbe.edits | ForEach-Object { [string]$_.path } | Sort-Object -Unique)
    $absolute = @{}
    $snapshots = @{}
    $setupError = $null

    foreach ($path in $paths) {
        $full = Join-Path $repositoryRoot ($path -replace '/', '\')
        if (-not (Test-Path -LiteralPath $full)) {
            $setupError = "the file '$path' does not exist"
            break
        }
        $absolute[$path] = $full
        $snapshots[$path] = Get-FileBytes -Path $full
    }

    # A dirty path is refused rather than probed: the restore below puts back what this file read, and
    # a probe that ran over someone's uncommitted edit would report on a tree nobody else has.
    if (-not $setupError -and $gitAvailable) {
        $dirty = & git -C $repositoryRoot status --porcelain -- $paths 2>$null
        if ($LASTEXITCODE -eq 0 -and $dirty) {
            $setupError = "the working tree has uncommitted changes to a path this probe edits: $(($dirty | ForEach-Object { $_.Trim() }) -join '; ')"
        }
    }

    if ($setupError) {
        $failures.Add("Probe '$($guardProbe.id)' could not run: $setupError.")
        continue
    }

    $exitCode = $null
    try {
        foreach ($edit in $guardProbe.edits) {
            $path = [string]$edit.path
            $file = Read-Text -Path $absolute[$path]
            $text = $file.Text
            if ($null -ne $edit.append) {
                $text = $text + [string]$edit.append
            }
            else {
                $find = [string]$edit.find
                $occurrences = 0
                $scan = 0
                while (($scan = $text.IndexOf($find, $scan, [System.StringComparison]::Ordinal)) -ge 0) {
                    $occurrences++
                    $scan += $find.Length
                }
                if ($occurrences -lt 1) {
                    # The rotted-probe case, and it is a failure rather than a skip. A probe whose
                    # anchor a correction moved is a probe that stopped measuring, and three passes
                    # have now discovered that by hand instead of being told.
                    throw "its anchor no longer occurs in '$path'. The guard may still be sound; the probe is stale and must be re-anchored or deleted with the guard it was written for."
                }
                if ($null -ne $edit.occurrence) {
                    $index = -1
                    for ($step = 0; $step -le [int]$edit.occurrence; $step++) {
                        $index = $text.IndexOf($find, $index + 1, [System.StringComparison]::Ordinal)
                        if ($index -lt 0) { throw "occurrence $($edit.occurrence) of its anchor does not exist in '$path'." }
                    }
                    $text = $text.Substring(0, $index) + [string]$edit.replace + $text.Substring($index + $find.Length)
                }
                elseif ($edit.all) {
                    $text = $text.Replace($find, [string]$edit.replace)
                }
                else {
                    if ($occurrences -ne 1) { throw "its anchor occurs $occurrences times in '$path' and the probe names no occurrence." }
                    $text = $text.Replace($find, [string]$edit.replace)
                }
            }
            Write-Text -Path $absolute[$path] -Text $text -HasBom $file.HasBom
        }

        # A CHILD PROCESS, not a dot-source or a call in this scope. A gate reports through
        # `Write-Error` and through `exit`, and under this file's `$ErrorActionPreference` the
        # first of those becomes a terminating error here -- so an in-scope call turned every
        # correctly-failing gate into "this probe could not be applied", which reads as a defect
        # in the probe and is the gate doing exactly what the probe asked of it.
        # `2>&1` on a child process puts each stderr line into the pipeline as an ErrorRecord, and
        # under this file's `Stop` preference the first one throws -- which turned a gate that
        # failed exactly as the probe asked into "this probe could not be applied". The preference
        # is lowered around the call and restored after it, so the gate's verdict is its exit code
        # and nothing else.
        #
        # AV1: the exit code is no longer the whole verdict, and this sentence used to end "and
        # nothing else" as a statement of design rather than of a limit. The output is captured too,
        # because a probe that reads only the exit code cannot tell its own guard firing from the
        # gate failing for any other reason. Measured: an unconditional failure added to the design
        # gate left 76 of 77 probes still reporting the verdict their guard owes, and the one that
        # noticed was the single probe on that gate expecting a pass.
        $previousPreference = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            # A probe may name the arguments its gate is run with. It is a COST control and never a
            # fidelity one: the arguments a probe passes must not be able to switch off the guard it
            # asserts, and the check below refuses a probe whose guard then fails to report. Measured,
            # the corpus was five full runs of the coverage measure and thirty-three runs of the
            # generated-vector loop that no probe on this list asserts anything about.
            $probeArguments = @([string[]]($guardProbe.gateArguments) | Where-Object { $_ })
            $gateOutput = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot "build\$($guardProbe.gate)") @probeArguments 2>&1
            $exitCode = $LASTEXITCODE
            # Not `Out-String`: it renders at the console width and truncates, which cut every
            # `Write-Error` message in half and would have made a message assertion unmatchable for
            # the one gate that reports that way. Each record is taken as its own string instead.
            # Whitespace is REMOVED rather than collapsed. A child process renders its output at its
            # own console width, and the break lands mid-word -- 'Session-Stat e-Machine' -- so no
            # amount of collapsing makes the message match. Comparing both sides without whitespace
            # is insensitive to where the host chose to wrap.
            $gateLines = @($gateOutput | ForEach-Object {
                if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { [string]$_ }
            })
            $gateText = [regex]::Replace((($gateLines -join ' ') -replace '﻿', ''), '\s+', '')
        }
        finally { $ErrorActionPreference = $previousPreference }
    }
    catch {
        $failures.Add("Probe '$($guardProbe.id)' could not be applied: $($_.Exception.Message)")
    }
    finally {
        foreach ($path in $paths) {
            Restore-FileBytes -Path $absolute[$path] -Bytes $snapshots[$path]
        }
    }

    if ($null -eq $exitCode) { continue }

    $observed = if ($exitCode -eq 0) { 'pass' } else { 'fail' }
    if ($observed -cne [string]$guardProbe.expect) {
        $failures.Add("Probe '$($guardProbe.id)' -- $($guardProbe.claim) -- expected '$($guardProbe.gate)' to $($guardProbe.expect) and it returned $observed. A guard that no longer answers its own subject is a guard that has stopped measuring.")
    }
    elseif ([string]$guardProbe.expect -ceq 'fail' -and -not $guardProbe.guardMessage) {
        # AV1. Mandatory rather than optional: a probe with no declared message is one that asserts
        # only the exit code, which is the state this finding is about, and an optional field would
        # leave the corpus half in it while reporting a whole number.
        $failures.Add("Probe '$($guardProbe.id)' expects '$($guardProbe.gate)' to fail and declares no ``guardMessage``. Without one it asserts that the gate failed and not that its own guard fired, so it stays green when the gate is broken by something else entirely.")
    }
    elseif ([string]$guardProbe.expect -ceq 'fail' -and
            $gateText.IndexOf([regex]::Replace([string]$guardProbe.guardMessage, '\s+', ''), [System.StringComparison]::Ordinal) -lt 0) {
        $failures.Add("Probe '$($guardProbe.id)' -- $($guardProbe.claim) -- made '$($guardProbe.gate)' fail and its own guard did not report: the output does not contain '$($guardProbe.guardMessage)'. Either the gate is now failing for a different reason, in which case this probe has stopped measuring what it names, or the guard's message moved and this probe must be re-anchored on it.")
    }
    else {
        $passed++
    }
}

# The restore is checked rather than assumed: this file writes to the working tree, and a probe that
# left a mutation behind would hand the next command a package nobody wrote.
if ($gitAvailable -and $parallelWorkerCount -eq 0) {
    $allPaths = @($probes | ForEach-Object { $_.edits } | ForEach-Object { [string]$_.path } | Sort-Object -Unique)
    $residual = & git -C $repositoryRoot status --porcelain -- $allPaths 2>$null
    if ($LASTEXITCODE -eq 0 -and $residual) {
        $failures.Add("A probe mutation was left in the working tree: $(($residual | ForEach-Object { $_.Trim() }) -join '; '). Every probe restores from bytes read before it applied, so this is a defect in this file rather than in a gate.")
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

# The plan states this count as a measure, so the file that determines it checks the claim -- AO2's
# remedy applied to the measure AO3 added. Skipped when a single probe was requested, since the
# corpus was not run whole.
if (-not $Probe -and -not $ProbeIdFile) {
    $planPath = Join-Path $repositoryRoot 'docs\future\channel\Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md'
    if (Test-Path -LiteralPath $planPath) {
        $planText = [regex]::Replace(((Get-Content -Raw -LiteralPath $planPath -Encoding UTF8) -replace '\*\*', ''), '\s+', ' ')
        $measureMatch = [regex]::Match($planText, 'guard probes executable . currently ([0-9,]*[0-9]) of ([0-9,]*[0-9])')
        if (-not $measureMatch.Success) {
            $failures.Add("The verification foundation plan's section 4 no longer states the guard-probe measure in the form 'currently <n> of <m>'. That measure is the claim the guards fire, which three passes asserted in prose while four probes had stopped applying.")
        }
        elseif ([int]($measureMatch.Groups[2].Value -replace ',', '') -ne $probes.Count) {
            $failures.Add("The verification foundation plan says the corpus holds $($measureMatch.Groups[2].Value) probes and it holds $($probes.Count).")
        }
        elseif ([int]($measureMatch.Groups[1].Value -replace ',', '') -ne $passed) {
            $failures.Add("The verification foundation plan says $($measureMatch.Groups[1].Value) probes return the verdict their guard owes and $passed do.")
        }
    }

    # AU3, and it is AM2's lesson arriving where AM2's own correction did not reach: the measure this
    # gate computes was right and every statement of the same number left to prose was wrong. The
    # corpus held 73 while the plan said 72 in two places, its own section 2k said "from 69 to 73",
    # and the review policy said 69 -- one fact, four surfaces, three values, and the one surface a
    # gate recomputed was the one that was correct. The question that finds this is AN's, "where else
    # is this stated", and it is asked here rather than answered once more by hand.
    #
    # AV3, and it is the rule this sweep imposes rather than a narrowing of it. The key is any
    # `<number> probes` in these four documents, which is broader than the question "does this
    # document state the size of the corpus": it fires on a sentence that counts probes for some other
    # reason, and it did, on prose describing an experiment over the corpus and on prose proposing to
    # write probes for a population of guards. Narrowing the key to a declared phrasing is what AN1
    # and AN2 were each raised for, so the breadth is kept and the rule is stated instead: **in these
    # four documents, `<number> probes` means the size of this corpus.** A count of probes written for
    # any other purpose is phrased another way, and the two sentences that were not have been.
    $countSurfaces = @(
        'docs\future\channel\Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md',
        'docs\future\channel\reviews\README.md',
        'docs\future\README.md',
        'docs\future\channel\README.md'
    )
    foreach ($countSurface in $countSurfaces) {
        $surfacePath = Join-Path $repositoryRoot $countSurface
        if (-not (Test-Path -LiteralPath $surfacePath)) {
            $failures.Add("The probe-count sweep names '$countSurface' and no such file exists. A sweep over a path that is not there is a check that passes by looking at nothing.")
            continue
        }
        $surfaceText = [regex]::Replace(((Get-Content -Raw -LiteralPath $surfacePath -Encoding UTF8) -replace '\*\*', ''), '\s+', ' ')
        foreach ($stated in [regex]::Matches($surfaceText, '([0-9][0-9,]*) probes')) {
            if ([int]($stated.Groups[1].Value -replace ',', '') -ne $probes.Count) {
                $failures.Add("'$countSurface' says the corpus holds $($stated.Groups[1].Value) probes and it holds $($probes.Count). A count of this corpus is a fact this file owns, and every statement of it that a gate does not recompute has gone stale at least once.")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

if ($parallelWorkerCount -gt 0 -and $passed -ne $probes.Count) {
    Write-Host "FAIL: the corpus holds $($probes.Count) probes and its workers accounted for $passed. A probe that no worker reported on is one nobody ran."
    exit 1
}
$parallelNote = if ($parallelWorkerCount -gt 0) { ", over $parallelWorkerCount parallel workers" } else { '' }
Write-Host "Channel 0.2 guard verification passed: $passed of $($probes.Count) probes returned the verdict their guard owes$parallelNote."
