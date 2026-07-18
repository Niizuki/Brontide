$ErrorActionPreference = 'Stop'

$projects = Get-ChildItem -Path "$PSScriptRoot\..\src" -Recurse -Filter *.csproj
$references = foreach ($project in $projects) {
    [xml]$xml = Get-Content -LiteralPath $project.FullName
    foreach ($reference in $xml.Project.ItemGroup.ProjectReference) {
        [pscustomobject]@{
            Project = $project.BaseName
            Reference = [System.IO.Path]::GetFileNameWithoutExtension($reference.Include)
        }
    }
}

$violations = $references | Where-Object {
    ($_.Project -eq 'Brontide.Reference.Core') -or
    ($_.Project -like 'Brontide.Reference.Extensions.*' -and $_.Reference -ne 'Brontide.Reference.Core') -or
    ($_.Project -like 'Brontide.Reference.Vocabularies.*' -and $_.Reference -ne 'Brontide.Reference.Core') -or
    ($_.Project -like 'Brontide.Reference.Experimental.*' -and $_.Reference -ne 'Brontide.Reference.Core')
}

if ($violations) {
    $violations | Format-Table | Out-String | Write-Error
}

$foreignReferences = $projects | Where-Object {
    (Get-Content -Raw -LiteralPath $_.FullName) -match '(?i)Brontide.Minimal[.\\/]'
}

if ($foreignReferences) {
    $foreignReferences.FullName | Out-String | Write-Error
}

$foreignAssemblies = Get-ChildItem -Path "$PSScriptRoot\.." -Recurse -File -Filter 'Brontide.Minimal*.dll' |
    Where-Object { $_.FullName -match '[\\/]bin[\\/]' }

if ($foreignAssemblies) {
    $foreignAssemblies.FullName | Out-String | Write-Error
}

Write-Host "Brontide.Reference project dependency direction is valid ($($references.Count) project references checked)."
