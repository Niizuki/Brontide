$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$projectFiles = Get-ChildItem -Path (Join-Path $repositoryRoot 'Reference'), (Join-Path $repositoryRoot 'Minimal') -Recurse -File |
    Where-Object { $_.Extension -in @('.csproj', '.fsproj') -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$nodes = @{}

foreach ($projectFile in $projectFiles) {
    [xml]$xml = Get-Content -Raw -LiteralPath $projectFile.FullName -Encoding UTF8
    $references = [System.Collections.Generic.List[string]]::new()

    foreach ($reference in @($xml.Project.ItemGroup.ProjectReference)) {
        if ($null -eq $reference) {
            continue
        }

        $resolved = [System.IO.Path]::GetFullPath((Join-Path $projectFile.DirectoryName ([string]$reference.Include)))
        $references.Add($resolved)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            $failures.Add("$($projectFile.FullName) has stale ProjectReference '$($reference.Include)'.")
        }
    }

    foreach ($package in @($xml.Project.ItemGroup.PackageReference)) {
        if ($null -ne $package -and ($package.Version -or $package.SelectSingleNode('Version'))) {
            $failures.Add("$($projectFile.FullName) repeats package version for '$($package.Include)'; use Central Package Management.")
        }
    }

    $stack = if ($projectFile.FullName -match '[\\/]Reference[\\/]') { 'Reference' } else { 'Minimal' }
    $production = $projectFile.FullName -match '[\\/]src[\\/]'
    $nodes[$projectFile.FullName] = [pscustomobject]@{
        Name = $projectFile.BaseName
        Path = $projectFile.FullName
        Stack = $stack
        Production = $production
        References = @($references)
    }
}

foreach ($node in $nodes.Values) {
    foreach ($referencePath in $node.References) {
        if (-not $nodes.ContainsKey($referencePath)) {
            continue
        }

        $dependency = $nodes[$referencePath]
        if ($dependency.Stack -ne $node.Stack) {
            $failures.Add("$($node.Name) has a cross-stack ProjectReference to $($dependency.Name).")
        }
    }

    if (-not $node.Production) {
        continue
    }

    $dependencyNames = @($node.References | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) })
    if ($node.Name -eq 'Brontide.Reference.Core' -and $dependencyNames.Count -ne 0) {
        $failures.Add('Brontide.Reference.Core must have no ProjectReferences.')
    }

    if ($node.Name -like 'Brontide.Reference.Extensions.*' -or
        $node.Name -like 'Brontide.Reference.Vocabularies.*' -or
        $node.Name -like 'Brontide.Reference.Experimental.*') {
        foreach ($dependency in $dependencyNames | Where-Object { $_ -ne 'Brontide.Reference.Core' }) {
            $failures.Add("$($node.Name) may depend only on Brontide.Reference.Core, not '$dependency'.")
        }
    }

    if ($node.Name -eq 'Brontide.Minimal.Model' -and $dependencyNames.Count -ne 0) {
        $failures.Add('Brontide.Minimal.Model must have no ProjectReferences.')
    }

    if ($node.Name -eq 'Brontide.Minimal.Kernel' -and
        (($dependencyNames.Count -ne 1) -or $dependencyNames[0] -ne 'Brontide.Minimal.Model')) {
        $failures.Add('Brontide.Minimal.Kernel must depend only on Brontide.Minimal.Model.')
    }

    if ($node.Name -ne 'Brontide.Minimal.Host' -and $dependencyNames -contains 'Brontide.Minimal.Host') {
        $failures.Add("$($node.Name) references the Minimal composition root.")
    }
}

$visiting = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

function Visit-Project {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ($visited.Contains($Path)) {
        return
    }

    if (-not $visiting.Add($Path)) {
        $failures.Add("Project-reference cycle detected at '$Path'.")
        return
    }

    foreach ($reference in $nodes[$Path].References) {
        if ($nodes.ContainsKey($reference)) {
            Visit-Project $reference
        }
    }

    $visiting.Remove($Path) | Out-Null
    $visited.Add($Path) | Out-Null
}

foreach ($path in $nodes.Keys) {
    Visit-Project $path
}

$solutionFiles = @(
    Join-Path $repositoryRoot 'Reference\Brontide.Reference.sln'
    Join-Path $repositoryRoot 'Minimal\Brontide.Minimal.slnx'
)
foreach ($solutionFile in $solutionFiles) {
    $solution = Get-Content -Raw -LiteralPath $solutionFile -Encoding UTF8
    $stackRoot = Split-Path -Parent $solutionFile
    foreach ($node in $nodes.Values | Where-Object { $_.Path.StartsWith($stackRoot, [StringComparison]::OrdinalIgnoreCase) }) {
        if ($solution.IndexOf([System.IO.Path]::GetFileName($node.Path), [StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $failures.Add("$($node.Name) is not registered in '$solutionFile'.")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Project graph verification passed for $($nodes.Count) projects with no cycles or boundary violations."
