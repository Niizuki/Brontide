$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$depsFiles = Get-ChildItem -Path (Join-Path $repositoryRoot 'Reference\src'), (Join-Path $repositoryRoot 'Minimal\src') -Recurse -File -Filter '*.deps.json' |
    Where-Object { $_.FullName -match '[\\/]bin[\\/]' }

if ($depsFiles.Count -eq 0) {
    throw 'No built production .deps.json files were found; run assembly verification after build.'
}

foreach ($depsFile in $depsFiles) {
    $projectName = $depsFile.Name.Substring(0, $depsFile.Name.Length - '.deps.json'.Length)
    $deps = Get-Content -Raw -LiteralPath $depsFile.FullName -Encoding UTF8 | ConvertFrom-Json
    $target = @($deps.targets.PSObject.Properties)[0].Value
    $rootLibrary = @($target.PSObject.Properties | Where-Object { $_.Name.StartsWith($projectName + '/', [StringComparison]::Ordinal) })[0]
    if ($null -eq $rootLibrary) {
        $failures.Add("$($depsFile.FullName) has no root library entry for $projectName.")
        continue
    }

    $resolved = @($rootLibrary.Value.dependencies.PSObject.Properties | ForEach-Object { $_.Name })
    $brontideDependencies = @($resolved | Where-Object { $_ -like 'Brontide.Reference.*' -or $_ -like 'Brontide.Minimal.*' })

    if ($projectName -like 'Brontide.Reference.*' -and ($brontideDependencies | Where-Object { $_ -like 'Brontide.Minimal.*' })) {
        $failures.Add("$projectName resolved a Minimal assembly dependency.")
    }

    if ($projectName -like 'Brontide.Minimal.*' -and ($brontideDependencies | Where-Object { $_ -like 'Brontide.Reference.*' })) {
        $failures.Add("$projectName resolved a Reference assembly dependency.")
    }

    if ($projectName -eq 'Brontide.Reference.Core' -and $brontideDependencies.Count -ne 0) {
        $failures.Add("Brontide.Reference.Core resolved unexpected Brontide dependencies: $($brontideDependencies -join ', ').")
    }

    if ($projectName -eq 'Brontide.Minimal.Model' -and $brontideDependencies.Count -ne 0) {
        $failures.Add("Brontide.Minimal.Model resolved unexpected Brontide dependencies: $($brontideDependencies -join ', ').")
    }

    if ($projectName -eq 'Brontide.Minimal.Kernel' -and
        (@($brontideDependencies).Count -ne 1 -or $brontideDependencies[0] -ne 'Brontide.Minimal.Model')) {
        $failures.Add("Brontide.Minimal.Kernel resolved dependencies other than Model: $($brontideDependencies -join ', ').")
    }

    if ($projectName -ne 'Brontide.Minimal.Host' -and $brontideDependencies -contains 'Brontide.Minimal.Host') {
        $failures.Add("$projectName resolved the Minimal composition-root assembly.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Resolved assembly graph verification passed for $($depsFiles.Count) production dependency manifests."
