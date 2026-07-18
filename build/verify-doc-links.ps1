$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$documents = Get-ChildItem -Path $repositoryRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/](\.git|bin|obj)[\\/]' }
$linkPattern = [regex]'!?\[[^\]]*\]\((?<target>[^)]+)\)'
$checked = 0

foreach ($document in $documents) {
    $insideFence = $false
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $document.FullName -Encoding UTF8) {
        $lineNumber++
        $trimmed = $line.TrimStart()
        if ($trimmed.StartsWith('```') -or $trimmed.StartsWith('~~~')) {
            $insideFence = -not $insideFence
            continue
        }

        if ($insideFence) {
            continue
        }

        foreach ($match in $linkPattern.Matches($line)) {
            $target = $match.Groups['target'].Value.Trim()
            if ($target.StartsWith('<') -and $target.EndsWith('>')) {
                $target = $target.Substring(1, $target.Length - 2)
            }

            if ($target -match '^(https?://|mailto:|#)' -or [string]::IsNullOrWhiteSpace($target)) {
                continue
            }

            $pathPart = $target.Split('#', 2)[0]
            if ($pathPart.Contains(' "')) {
                $pathPart = $pathPart.Split(' "', 2)[0]
            }

            $pathPart = [System.Uri]::UnescapeDataString($pathPart)
            $candidate = [System.IO.Path]::GetFullPath((Join-Path $document.DirectoryName $pathPart))
            $checked++
            if (-not (Test-Path -LiteralPath $candidate)) {
                $relativeDocument = $document.FullName.Substring($repositoryRoot.Length).TrimStart('\')
                $failures.Add("$relativeDocument`:$lineNumber has a broken local link '$target'.")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Documentation link verification passed for $checked local links across $($documents.Count) documents."
