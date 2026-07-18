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
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-binding-measurements.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-project-graph.ps1')
}

Invoke-Checked { dotnet restore $referenceSolution }
Invoke-Checked { dotnet restore $minimalSolution }
Invoke-Checked { dotnet build $referenceSolution --no-restore }
Invoke-Checked { dotnet build $minimalSolution --no-restore }
Invoke-Checked { dotnet test $referenceSolution --no-build }
Invoke-Checked { dotnet test $minimalSolution --no-build }

$env:BRONTIDE_MINIMAL_PROVIDER = Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Interchange.Provider\bin\Debug\net10.0\Brontide.Minimal.Interchange.Provider.exe'
$env:BRONTIDE_REFERENCE_PROVIDER = Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Interchange.Provider\bin\Debug\net10.0\Brontide.Reference.Interchange.Provider.exe'

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
