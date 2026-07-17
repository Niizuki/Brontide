$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$fabricSolution = Join-Path $repositoryRoot 'Fabric\Fabric.sln'
$linenSolution = Join-Path $repositoryRoot 'Linen\Linen.slnx'

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

Invoke-Checked { dotnet restore $fabricSolution }
Invoke-Checked { dotnet restore $linenSolution }
Invoke-Checked { dotnet build $fabricSolution --no-restore }
Invoke-Checked { dotnet build $linenSolution --no-restore }
Invoke-Checked { dotnet test $fabricSolution --no-build }
Invoke-Checked { dotnet test $linenSolution --no-build }

$env:ATLAS_LINEN_PROVIDER = Join-Path $repositoryRoot 'Linen\src\Linen.Interchange.Provider\bin\Debug\net10.0\Linen.Interchange.Provider.exe'
$env:ATLAS_FABRIC_PROVIDER = Join-Path $repositoryRoot 'Fabric\src\Fabric.Interchange.Provider\bin\Debug\net10.0\Fabric.Interchange.Provider.exe'

Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Fabric\tests\Fabric.Interchange.Tests\Fabric.Interchange.Tests.csproj') --no-build --filter 'Category=CrossProcess'
}
Invoke-Checked {
    dotnet test (Join-Path $repositoryRoot 'Linen\tests\Linen.Interchange.Tests\Linen.Interchange.Tests.fsproj') --no-build --filter 'Category=CrossProcess'
}

Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'Fabric\build\verify-dependencies.ps1')
}
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'Linen\build\verify-boundaries.ps1')
}
