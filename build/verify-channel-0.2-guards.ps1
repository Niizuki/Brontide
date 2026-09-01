[CmdletBinding()]
param(
    # Run one probe by id instead of the whole corpus, for working on a single guard.
    [string]$Probe
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
if ($probes.Count -lt 1) {
    Write-Host "FAIL: no probe to run$(if ($Probe) { " with id '$Probe'" })."
    exit 1
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
$passed = 0

foreach ($guardProbe in $probes) {
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
        # The gate runs marked as nested. One probe here runs the coverage gate, which covers this
        # file, which runs this file's probes -- so an unmarked run would start a whole measurement
        # from inside one. A nested coverage run still covers the design gates, which is where that
        # probe's mutation is, so marking it costs the probe nothing.
        $previousPreference = $ErrorActionPreference
        $previousNestedMarker = [System.Environment]::GetEnvironmentVariable('BRONTIDE_CHANNEL_02_COVERAGE_NESTED')
        try {
            $ErrorActionPreference = 'Continue'
            [System.Environment]::SetEnvironmentVariable('BRONTIDE_CHANNEL_02_COVERAGE_NESTED', '1')
            $null = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot "build\$($guardProbe.gate)") 2>&1
            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
            [System.Environment]::SetEnvironmentVariable('BRONTIDE_CHANNEL_02_COVERAGE_NESTED', $previousNestedMarker)
        }
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
    else {
        $passed++
    }
}

# The restore is checked rather than assumed: this file writes to the working tree, and a probe that
# left a mutation behind would hand the next command a package nobody wrote.
if ($gitAvailable) {
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
if (-not $Probe) {
    $planPath = Join-Path $repositoryRoot 'docs\future\channel\Brontide-Channel-0.2-Verification-Foundation-Plan-0.1.md'
    if (Test-Path -LiteralPath $planPath) {
        $planText = [regex]::Replace(((Get-Content -Raw -LiteralPath $planPath -Encoding UTF8) -replace '\*\*', ''), '\s+', ' ')
        $measureMatch = [regex]::Match($planText, 'guard probes executable . currently ([0-9,]+) of ([0-9,]+)')
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
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

Write-Host "Channel 0.2 guard verification passed: $passed of $($probes.Count) probes returned the verdict their guard owes."
