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

Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-sdk.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-text.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-doc-links.ps1')
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
