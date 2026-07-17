$ErrorActionPreference = 'Stop'

$linenRoot = Split-Path -Parent $PSScriptRoot
$projects = Get-ChildItem -Path $linenRoot -Recurse -File -Filter '*.fsproj'
$failures = [System.Collections.Generic.List[string]]::new()

$expectedProjects = @(
    'Linen.Model',
    'Linen.Kernel',
    'Linen.Extensions.Events',
    'Linen.Extensions.Flow',
    'Linen.Experimental.Enrichment',
    'Linen.Experimental.Composition',
    'Linen.Vocabularies.Cooling',
    'Linen.Vocabularies.Imaging',
    'Linen.Binding',
    'Linen.Interchange.Provider',
    'Linen.Host',
    'Linen.Conformance',
    'Linen.Enrichment.Tests',
    'Linen.Composition.Tests',
    'Linen.Kernel.Tests',
    'Linen.Interchange.Tests'
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

if (Get-ChildItem -Path $linenRoot -Recurse -File -Filter 'global.json') {
    $failures.Add('Linen must not contain a global.json SDK pin.')
}

foreach ($project in $projects) {
    $content = Get-Content -Raw -LiteralPath $project.FullName

    if ($content -match '(?i)Fabric[.\\/]') {
        $failures.Add("$($project.BaseName) references Fabric; interchange must remain external.")
    }

    if ($project.BaseName -ne 'Linen.Host' -and $content -match 'Linen[.]Host') {
        $failures.Add("$($project.BaseName) references the host composition root.")
    }
}

$modelProject = $projects | Where-Object BaseName -eq 'Linen.Model'
$modelContent = Get-Content -Raw -LiteralPath $modelProject.FullName
if ($modelContent -match '<ProjectReference') {
    $failures.Add('Linen.Model must not contain project references.')
}

$kernelProject = $projects | Where-Object BaseName -eq 'Linen.Kernel'
$kernelContent = Get-Content -Raw -LiteralPath $kernelProject.FullName
$kernelReferences = [regex]::Matches($kernelContent, '<ProjectReference')
if ($kernelReferences.Count -ne 1 -or $kernelContent -notmatch 'Linen[.]Model') {
    $failures.Add('Linen.Kernel must reference Linen.Model and no other project.')
}

if ($modelContent -match 'Experimental' -or $kernelContent -match 'Experimental') {
    $failures.Add('Experimental projects must not flow into Linen.Model or Linen.Kernel.')
}

$foreignAssemblies = Get-ChildItem -Path $linenRoot -Recurse -File -Filter 'Fabric*.dll' |
    Where-Object { $_.FullName -match '[\\/]bin[\\/]' }

if ($foreignAssemblies) {
    $foreignAssemblies | ForEach-Object {
        $failures.Add("Linen output contains foreign Fabric assembly '$($_.FullName)'.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Linen boundary verification passed for $($projects.Count) F# projects."
