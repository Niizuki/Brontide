$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()
$documents = Get-ChildItem -Path $repositoryRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '[\\/](\.git|bin|obj)[\\/]' }
$linkPattern = [regex]'!?\[[^\]]*\]\((?<target>[^)]+)\)'
$checked = 0
$fragmentsChecked = 0

# A link's fragment is part of the link. Until AN1 this file split it off and threw it away, so a
# pointer into a heading that does not exist was indistinguishable from one that resolves -- and the
# Channel 0.2 status blocks now carry their whole disposition history as exactly such a pointer, nine
# of them into one index. Renaming a heading there left all nine dead with every gate green.
#
# The anchor set is derived from each document's headings the way a Markdown renderer derives it,
# including the `-1`, `-2` suffixes a repeated heading takes, so the check answers the reader's
# question -- does this pointer land somewhere -- rather than a weaker one about the file existing.
function Get-HeadingAnchors {
    param([Parameter(Mandatory = $true)][string]$Path)

    $anchors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $seen = @{}
    $insideCodeFence = $false
    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        $trimmedLine = $line.TrimStart()
        if ($trimmedLine.StartsWith('```') -or $trimmedLine.StartsWith('~~~')) {
            $insideCodeFence = -not $insideCodeFence
            continue
        }
        if ($insideCodeFence -or -not $trimmedLine.StartsWith('#')) {
            continue
        }

        $heading = $trimmedLine.TrimStart('#').Trim()
        if (-not $heading) { continue }
        # Link text survives, link target does not; emphasis and code markers are not part of the
        # slug; everything outside word characters, spaces and hyphens is dropped; spaces become
        # hyphens.
        $slug = [regex]::Replace($heading, '\[([^\]]*)\]\([^)]*\)', '$1')
        $slug = $slug -replace '[*_`~]', ''
        $slug = ([regex]::Replace($slug, '[^\w\- ]', '')).ToLowerInvariant().Trim() -replace ' ', '-'
        if (-not $slug) { continue }
        if ($seen.ContainsKey($slug)) {
            $seen[$slug] = $seen[$slug] + 1
            [void]$anchors.Add("$slug-$($seen[$slug])")
        }
        else {
            $seen[$slug] = 0
            [void]$anchors.Add($slug)
        }
    }
    return $anchors
}

$anchorCache = @{}
function Get-DocumentAnchors {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not $anchorCache.ContainsKey($Path)) {
        $anchorCache[$Path] = Get-HeadingAnchors -Path $Path
    }
    return $anchorCache[$Path]
}

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

            if ($target -match '^(https?://|mailto:)' -or [string]::IsNullOrWhiteSpace($target)) {
                continue
            }

            $relativeDocument = $document.FullName.Substring($repositoryRoot.Length).TrimStart('\')
            $pathPart = $target.Split('#', 2)[0]
            $fragment = if ($target.Contains('#')) { $target.Split('#', 2)[1] } else { '' }
            if ($pathPart.Contains(' "')) {
                $pathPart = $pathPart.Split(' "', 2)[0]
            }
            if ($fragment.Contains(' "')) {
                $fragment = $fragment.Split(' "', 2)[0]
            }

            # A fragment-only target points into the document that carries it.
            if (-not $pathPart) {
                $candidate = $document.FullName
            }
            else {
                $pathPart = [System.Uri]::UnescapeDataString($pathPart)
                $candidate = [System.IO.Path]::GetFullPath((Join-Path $document.DirectoryName $pathPart))
                $checked++
                if (-not (Test-Path -LiteralPath $candidate)) {
                    $failures.Add("$relativeDocument`:$lineNumber has a broken local link '$target'.")
                    continue
                }
            }

            if (-not $fragment) { continue }
            if ([System.IO.Path]::GetExtension($candidate) -ne '.md') { continue }
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }

            $fragmentsChecked++
            $fragment = [System.Uri]::UnescapeDataString($fragment).ToLowerInvariant()
            if (-not (Get-DocumentAnchors -Path $candidate).Contains($fragment)) {
                $failures.Add("$relativeDocument`:$lineNumber has a link '$target' whose fragment matches no heading in the document it points at. A pointer that resolves to nothing tells the reader the material is elsewhere and leaves them with nothing, which is worse than saying nothing at all.")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Documentation link verification passed for $checked local links and $fragmentsChecked heading fragments across $($documents.Count) documents."
