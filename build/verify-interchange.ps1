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


# The read-only verifications run TOGETHER, and the ones that build or test do not.
#
# Every file below reads the repository and reports; none of them writes to it, none depends on
# another having run, and each is already a child process. Run one after another they are the sum of
# their costs; run together they are the cost of the slowest, which is the Channel 0.2 properties
# gate. The `dotnet` phases underneath keep their order, because those DO write -- restore before
# build, build before test, and the provider executables exist only once their projects are built.
#
# TWO THINGS THIS CHANGES, STATED RATHER THAN ABSORBED. Output no longer interleaves with execution:
# each verification's output is captured and printed whole, in the order this file lists them, so a
# reader sees the same sequence and not the same timing. And a failure no longer stops the rest --
# every verification runs and every failure is reported, where before the first one threw. That is a
# better report and it is a different one; a run that used to stop after the first failure now costs
# what a whole run costs.
function Invoke-CheckedTogether {
    param([Parameter(Mandatory = $true)][string[]]$GatePaths)

    $running = [System.Collections.Generic.List[object]]::new()
    $outputRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("brontide-gate-" + [guid]::NewGuid().ToString('n'))
    $null = New-Item -ItemType Directory -Path $outputRoot -Force
    try {
        $ordinal = 0
        foreach ($gatePath in $GatePaths) {
            $outputPath = Join-Path $outputRoot ("$ordinal.out")
            $running.Add([pscustomobject]@{
                Path    = $gatePath
                Output  = $outputPath
                Error   = "$outputPath.err"
                Process = Start-Process -FilePath 'powershell.exe' -NoNewWindow -PassThru -RedirectStandardOutput $outputPath -RedirectStandardError "$outputPath.err" -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $gatePath)
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
                $failed.Add("$(Split-Path -Leaf $verification.Path) exited $($verification.Process.ExitCode)")
            }
        }
        if ($failed.Count -gt 0) {
            throw "Verifications failed: $($failed -join '; ')"
        }
    }
    finally { Remove-Item -LiteralPath $outputRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

Invoke-CheckedTogether -GatePaths @(
    (Join-Path $repositoryRoot 'build\verify-sdk.ps1')
    (Join-Path $repositoryRoot 'build\verify-text.ps1')
    (Join-Path $repositoryRoot 'build\verify-doc-links.ps1')
    (Join-Path $repositoryRoot 'build\verify-channel-0.2-design.ps1')
    (Join-Path $repositoryRoot 'build\verify-channel-0.2-properties.ps1')
    (Join-Path $repositoryRoot 'build\verify-channel-0.2-facts.ps1')
    (Join-Path $repositoryRoot 'build\verify-channel-0.2-return-channels.ps1')
)
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-text.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-doc-links.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-design.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-properties.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-facts.ps1')
}
# The fourth gate checks the other three rather than the design, and it is here rather than behind the
# switch below because it PARSES them instead of running them: a whole census costs about a second,
# where the two below cost minutes by executing what they measure. BA.
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-return-channels.ps1')
}
# The three gates above check the design package. The two that check THOSE GATES -- the probe corpus
# and the gate-coverage measure -- are behind the switch, because they cost 411 of the 442 seconds the
# PowerShell half of this gate takes, and they cost it by running the gates they measure. The reason,
# what it gives up, and what holds it are in the file below. AT7.
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
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-architecture-0.7-comparison.ps1') -NoBuild
}
Invoke-Checked { dotnet test $referenceSolution --no-build --filter 'Category!=CrossProcess' }
Invoke-Checked { dotnet test $minimalSolution --no-build --filter 'Category!=CrossProcess' }

$env:BRONTIDE_MINIMAL_PROVIDER = Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Interchange.Provider\bin\Debug\net10.0\Brontide.Minimal.Interchange.Provider.exe'
$env:BRONTIDE_REFERENCE_PROVIDER = Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Interchange.Provider\bin\Debug\net10.0\Brontide.Reference.Interchange.Provider.exe'

# CBI30, CBI54, CBI55, and CBI57 are owned by the composition roots, not the portable suites. Re-run their process
# category after both provider paths exist so the ordinary solution runs above cannot turn this
# evidence into environment-dependent skips.
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Studio.Tests\Brontide.Reference.Studio.Tests.csproj') --no-build --filter 'Category=CrossProcess'
}
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Host.Tests\Brontide.Minimal.Host.Tests.fsproj') --no-build --filter 'Category=CrossProcess'
}

# CM6 lives in the Component Management suites rather than the interchange suites because each
# host computes its own native CM5 baseline before crossing to the other stack's provider process.
# The ordinary solution runs above prove the offline surface; these filtered runs make skips
# impossible in the repository completion gate.
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.ComponentManagement.Tests\Brontide.Reference.ComponentManagement.Tests.csproj') --no-build --filter 'Category=CrossProcess'
}
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.ComponentManagement.Tests\Brontide.Minimal.ComponentManagement.Tests.fsproj') --no-build --filter 'Category=CrossProcess'
}

# The implementation-neutral provider is outside both solutions on purpose, so the gate builds it
# explicitly. Without it the PB5 rows that pair each host with a provider depending on neither stack
# would silently skip rather than fail, which is the one outcome a completeness gate must not allow.
$neutralProviderProject = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\PortableBinding.NeutralProvider.csproj'
Invoke-Checked { dotnet build $neutralProviderProject }
$env:BRONTIDE_NEUTRAL_PROVIDER = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\bin\Debug\net10.0\PortableBinding.NeutralProvider.exe'

Invoke-Checked {
    dotnet run --project (Join-Path $repositoryRoot 'Reference\benchmarks\Brontide.Reference.Benchmarks\Brontide.Reference.Benchmarks.csproj') --no-build -- --iterations 100
}
Invoke-Checked {
    dotnet run --project (Join-Path $repositoryRoot 'Minimal\benchmarks\Brontide.Minimal.Benchmarks\Brontide.Minimal.Benchmarks.fsproj') --no-build -- --iterations 100
}

Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj') --no-build --filter 'Category=CrossProcess'
}
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj') --no-build --filter 'Category=CrossProcess'
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
