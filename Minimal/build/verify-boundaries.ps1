$ErrorActionPreference = 'Stop'

$minimalRoot = Split-Path -Parent $PSScriptRoot
$projects = Get-ChildItem -Path $minimalRoot -Recurse -File -Filter '*.fsproj'
$failures = [System.Collections.Generic.List[string]]::new()

$expectedProjects = @(
    'Brontide.Minimal.Model',
    'Brontide.Minimal.Kernel',
    'Brontide.Minimal.Extensions.Events',
    'Brontide.Minimal.Extensions.Flow',
    'Brontide.Minimal.Experimental.Enrichment',
    'Brontide.Minimal.Experimental.Composition',
    'Brontide.Minimal.Experimental.ComponentManagement',
    'Brontide.Minimal.Vocabularies.Cooling',
    'Brontide.Minimal.Vocabularies.Imaging',
    'Brontide.Minimal.Binding',
    'Brontide.Minimal.Interchange.Provider',
    'Brontide.Minimal.Host',
    'Brontide.Minimal.Benchmarks',
    'Brontide.Minimal.Conformance',
    'Brontide.Minimal.Enrichment.Tests',
    'Brontide.Minimal.Composition.Tests',
    'Brontide.Minimal.ComponentManagement.Tests',
    'Brontide.Minimal.Kernel.Tests',
    'Brontide.Minimal.Interchange.Tests'
)

$actualProjects = $projects | ForEach-Object { $_.BaseName }

foreach ($expected in $expectedProjects) {
    if ($actualProjects -notcontains $expected) {
        $failures.Add("Missing expected F# project '$expected'.")
    }
}

if ($projects.Count -ne $expectedProjects.Count) {
    $failures.Add("Expected $($expectedProjects.Count) F# projects, but found $($projects.Count).")
}

if (Get-ChildItem -Path $minimalRoot -Recurse -File -Filter 'global.json') {
    $failures.Add('Brontide.Minimal must not contain a global.json SDK pin.')
}

foreach ($project in $projects) {
    $content = Get-Content -Raw -LiteralPath $project.FullName

    if ($content -match '(?i)Brontide.Reference[.\\/]') {
        $failures.Add("$($project.BaseName) references Brontide.Reference; interchange must remain external.")
    }

    if ($project.BaseName -ne 'Brontide.Minimal.Host' -and $content -match 'Brontide.Minimal[.]Host') {
        $failures.Add("$($project.BaseName) references the host composition root.")
    }
}

$modelProject = $projects | Where-Object BaseName -eq 'Brontide.Minimal.Model'
$modelContent = Get-Content -Raw -LiteralPath $modelProject.FullName
if ($modelContent -match '<ProjectReference') {
    $failures.Add('Brontide.Minimal.Model must not contain project references.')
}

$kernelProject = $projects | Where-Object BaseName -eq 'Brontide.Minimal.Kernel'
$kernelContent = Get-Content -Raw -LiteralPath $kernelProject.FullName
$kernelReferences = [regex]::Matches($kernelContent, '<ProjectReference')
if ($kernelReferences.Count -ne 1 -or $kernelContent -notmatch 'Brontide.Minimal[.]Model') {
    $failures.Add('Brontide.Minimal.Kernel must reference Brontide.Minimal.Model and no other project.')
}

if ($modelContent -match 'Experimental' -or $kernelContent -match 'Experimental') {
    $failures.Add('Experimental projects must not flow into Brontide.Minimal.Model or Brontide.Minimal.Kernel.')
}

$foreignAssemblies = Get-ChildItem -Path $minimalRoot -Recurse -File -Filter 'Brontide.Reference*.dll' |
    Where-Object { $_.FullName -match '[\\/]bin[\\/]' }

if ($foreignAssemblies) {
    $foreignAssemblies | ForEach-Object {
        $failures.Add("Brontide.Minimal output contains foreign Brontide.Reference assembly '$($_.FullName)'.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Brontide.Minimal boundary verification passed for $($projects.Count) F# projects."
