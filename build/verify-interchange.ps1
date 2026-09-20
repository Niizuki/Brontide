param(
    # Run the two verifications that check the GATES rather than the design -- the Channel 0.2 probe
    # corpus and the gate-coverage measure. They are 411 seconds of the 442 the PowerShell half of
    # this gate takes, so they are opt-in here and run on their own schedule in CI. AT7.
    [switch]$IncludeGateSelfChecks
)
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$referenceSolution = Join-Path $repositoryRoot 'Reference\Brontide.Reference.sln'
$minimalSolution = Join-Path $repositoryRoot 'Minimal\Brontide.Minimal.slnx'

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command"
    }
}


# Independent work runs TOGETHER, and work that writes what later work reads does not.
#
# Two groups below qualify. The read-only verifications each read the repository and report; none
# writes to it, none depends on another having run, and each is already a child process. The test
# runs each load one built assembly and write only under their own temporary directories; the
# provider executables they spawn are read by every one of them and written by none. Run one after
# another either group is the sum of its costs; run together it is the cost of the slowest, which is
# the Channel 0.2 properties gate for the first and the Studio cross-process suite for the second.
# The `dotnet` phases between them keep their order, because those DO write -- restore before build,
# build before test, and the provider executables exist only once their projects are built.
#
# TWO THINGS THIS CHANGES, STATED RATHER THAN ABSORBED. Output no longer interleaves with execution:
# each command's output is captured and printed whole, in the order this file lists them, so a
# reader sees the same sequence and not the same timing. And a failure no longer stops the rest --
# every command in a group runs and every failure is reported, where before the first one threw. That
# is a better report and it is a different one; a run that used to stop after the first failure now
# costs what a whole group costs.
function Invoke-CheckedTogether {
    param(
        # One entry per command: `Name` for the failure report, `FilePath` for the executable, and
        # `ArgumentList` for its arguments. The current environment is inherited, so a provider path
        # exported above a group reaches every command in it.
        [Parameter(Mandatory = $true)][hashtable[]]$Commands
    )

    $running = [System.Collections.Generic.List[object]]::new()
    $outputRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("brontide-gate-" + [guid]::NewGuid().ToString('n'))
    $null = New-Item -ItemType Directory -Path $outputRoot -Force
    try {
        $ordinal = 0
        foreach ($command in $Commands) {
            $outputPath = Join-Path $outputRoot ("$ordinal.out")
            # `Start-Process` joins the list with spaces and quotes nothing, so an argument carrying a
            # space -- a repository path, on a machine that has one -- is quoted here.
            $arguments = @(foreach ($argument in [string[]]$command.ArgumentList) {
                if ($argument -match '\s') { '"' + $argument + '"' } else { $argument }
            })
            $running.Add([pscustomobject]@{
                Name    = [string]$command.Name
                Output  = $outputPath
                Error   = "$outputPath.err"
                Process = Start-Process -FilePath ([string]$command.FilePath) -NoNewWindow -PassThru -RedirectStandardOutput $outputPath -RedirectStandardError "$outputPath.err" -ArgumentList $arguments
            })
            # Reading `.Handle` is what makes `.ExitCode` readable after the process ends. Without it
            # `Start-Process -PassThru` hands back an object whose ExitCode is `$null` forever, and
            # `$null -ne 0` is true -- so every verification reported as failed while every one of
            # them had passed. Found by running the block and disbelieving seven simultaneous
            # failures; it is the same shape as a channel nobody reads, one process boundary out.
            $null = $running[$running.Count - 1].Process.Handle
            $ordinal++
        }

        $failed = [System.Collections.Generic.List[string]]::new()
        foreach ($verification in $running) {
            $verification.Process.WaitForExit()
            foreach ($streamPath in @($verification.Output, $verification.Error)) {
                if (-not (Test-Path -LiteralPath $streamPath)) { continue }
                foreach ($line in (Get-Content -LiteralPath $streamPath)) { Write-Host $line }
            }
            if ($verification.Process.ExitCode -ne 0) {
                $failed.Add("$($verification.Name) exited $($verification.Process.ExitCode)")
            }
        }
        if ($failed.Count -gt 0) {
            throw "Commands failed: $($failed -join '; ')"
        }
    }
    finally { Remove-Item -LiteralPath $outputRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

function New-GateCommand {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    return @{
        Name         = Split-Path -Leaf $RelativePath
        FilePath     = 'powershell.exe'
        ArgumentList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $repositoryRoot $RelativePath))
    }
}

function New-TestCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Target,
        [Parameter(Mandatory = $true)][string]$Filter)

    return @{
        Name         = $Name
        FilePath     = 'dotnet'
        ArgumentList = @('test', $Target, '--no-build', '--filter', $Filter)
    }
}

# Every verification in this group runs exactly once, here. An earlier revision of this file ran six
# of them a second time, one after another, immediately below this block -- the sequential calls the
# block replaced had been left in place -- so a run paid the parallel cost and then the serial cost
# for the same seven answers.
Invoke-CheckedTogether -Commands @(
    (New-GateCommand 'build\verify-sdk.ps1')
    (New-GateCommand 'build\verify-text.ps1')
    (New-GateCommand 'build\verify-doc-links.ps1')
    (New-GateCommand 'build\verify-channel-0.2-design.ps1')
    (New-GateCommand 'build\verify-channel-0.2-properties.ps1')
    (New-GateCommand 'build\verify-channel-0.2-facts.ps1')
    # The fourth Channel 0.2 gate checks the other three rather than the design, and it is here rather
    # than behind the switch below because it PARSES them instead of running them: a whole census
    # costs about a second, where the two below cost minutes by executing what they measure. BA.
    (New-GateCommand 'build\verify-channel-0.2-return-channels.ps1')
)
# The three Channel 0.2 gates above check the design package. The two that check THOSE GATES -- the
# probe corpus and the gate-coverage measure -- are behind the switch, because they cost 411 of the
# 442 seconds the PowerShell half of this gate takes, and they cost it by running the gates they
# measure. The reason, what it gives up, and what holds it are in the file below. AT7.
if ($IncludeGateSelfChecks) {
    Invoke-Checked {
        powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-gate-self-checks.ps1')
    }
}
else {
    Write-Host 'SKIPPED: build/verify-gate-self-checks.ps1 -- the Channel 0.2 probe corpus and gate-coverage measure. Re-run with -IncludeGateSelfChecks, run that file directly, or wait for the scheduled repository-gate run, which always includes them.'
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-evidence.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-adversarial-vectors.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-handoff.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-delivery-audit.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d1.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d2.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d3.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d4.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d5.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-d6.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.8-closure.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-vectors.ps1')
}
Invoke-Checked {
    # -NeutralOnly: both stacks' portable vectors and their cross-process realizations run below with
    # the rest of the stack suites, so the portable gate does not build and test them twice.
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-portable-binding.ps1') -NeutralOnly
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-independent-review.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-binding-measurements.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-project-graph.ps1')
}

Invoke-Checked { dotnet restore $referenceSolution }
Invoke-Checked { dotnet restore $minimalSolution }
Invoke-Checked { dotnet build $referenceSolution --no-restore }
Invoke-Checked { dotnet build $minimalSolution --no-restore }

# The implementation-neutral provider is outside both solutions on purpose, so the gate builds it
# explicitly. Without it the PB5 rows that pair each host with a provider depending on neither stack
# would silently skip rather than fail, which is the one outcome a completeness gate must not allow.
$neutralProviderProject = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\PortableBinding.NeutralProvider.csproj'
Invoke-Checked { dotnet build $neutralProviderProject }

Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.7-comparison.ps1') -NoBuild
}

# Every provider path is exported before any test runs. The ordinary solution runs exclude the
# process category by filter, so they read none of these; the filtered runs beside them read all
# three, and a path that is absent turns a filtered run's evidence into a skip, which is what the
# filtered runs exist to make impossible.
$env:BRONTIDE_MINIMAL_PROVIDER = Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Interchange.Provider\bin\Debug\net10.0\Brontide.Minimal.Interchange.Provider.exe'
$env:BRONTIDE_REFERENCE_PROVIDER = Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Interchange.Provider\bin\Debug\net10.0\Brontide.Reference.Interchange.Provider.exe'
$env:BRONTIDE_NEUTRAL_PROVIDER = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\bin\Debug\net10.0\PortableBinding.NeutralProvider.exe'

# The eight test runs are independent of one another and run together. Each loads one built assembly,
# writes only under a temporary directory of its own, and spawns provider processes that every run
# reads and none writes. The two cross-process suites of the composition roots are two thirds of what
# the whole gate used to cost, and they were run one after the other.
Invoke-CheckedTogether -Commands @(
    (New-TestCommand -Name 'Reference solution' -Target $referenceSolution -Filter 'Category!=CrossProcess')
    (New-TestCommand -Name 'Minimal solution' -Target $minimalSolution -Filter 'Category!=CrossProcess')
    # CBI30, CBI54, CBI55, and CBI57 are owned by the composition roots, not the portable suites. Their
    # process category runs with every provider path exported, so the ordinary solution runs cannot
    # turn this evidence into environment-dependent skips.
    (New-TestCommand -Name 'Reference Studio cross-process' -Target (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Studio.Tests\Brontide.Reference.Studio.Tests.csproj') -Filter 'Category=CrossProcess')
    (New-TestCommand -Name 'Minimal Host cross-process' -Target (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Host.Tests\Brontide.Minimal.Host.Tests.fsproj') -Filter 'Category=CrossProcess')
    # CM6 lives in the Component Management suites rather than the interchange suites because each
    # host computes its own native CM5 baseline before crossing to the other stack's provider process.
    # The ordinary solution runs prove the offline surface; these filtered runs make skips impossible
    # in the repository completion gate.
    (New-TestCommand -Name 'Reference Component Management cross-process' -Target (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.ComponentManagement.Tests\Brontide.Reference.ComponentManagement.Tests.csproj') -Filter 'Category=CrossProcess')
    (New-TestCommand -Name 'Minimal Component Management cross-process' -Target (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.ComponentManagement.Tests\Brontide.Minimal.ComponentManagement.Tests.fsproj') -Filter 'Category=CrossProcess')
    (New-TestCommand -Name 'Reference Interchange cross-process' -Target (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj') -Filter 'Category=CrossProcess')
    (New-TestCommand -Name 'Minimal Interchange cross-process' -Target (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj') -Filter 'Category=CrossProcess')
)

Invoke-Checked {
    dotnet run --project (Join-Path $repositoryRoot 'Reference\benchmarks\Brontide.Reference.Benchmarks\Brontide.Reference.Benchmarks.csproj') --no-build -- --iterations 100
}
Invoke-Checked {
    dotnet run --project (Join-Path $repositoryRoot 'Minimal\benchmarks\Brontide.Minimal.Benchmarks\Brontide.Minimal.Benchmarks.fsproj') --no-build -- --iterations 100
}

Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'Reference\build\verify-dependencies.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'Minimal\build\verify-boundaries.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-assembly-graph.ps1')
}
