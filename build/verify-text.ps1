$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$textExtensions = @(
    '.cs', '.csproj', '.fs', '.fsproj', '.json', '.md', '.props', '.ps1', '.sln', '.slnx',
    '.targets', '.txt', '.yml', '.yaml'
)
$failures = [System.Collections.Generic.List[string]]::new()
$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$mojibakeMarkers = @(
    [string][char]0xFFFD,
    [string][char]0x00C3,
    [string][char]0x00C2,
    ([string][char]0x00E2 + [string][char]0x20AC)
)

$files = Get-ChildItem -Path $repositoryRoot -Recurse -File | Where-Object {
    $_.FullName -notmatch '[\\/](\.git|bin|obj)[\\/]' -and
    $textExtensions -contains $_.Extension.ToLowerInvariant()
}

foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    try {
        $text = $strictUtf8.GetString($bytes)
    }
    catch {
        $failures.Add("'$($file.FullName)' is not strict UTF-8: $($_.Exception.Message)")
        continue
    }

    foreach ($marker in $mojibakeMarkers) {
        if ($text.IndexOf([string]$marker, [StringComparison]::Ordinal) -ge 0) {
            $failures.Add("'$($file.FullName)' contains likely mojibake marker '$marker'.")
            break
        }
    }

    for ($index = 0; $index -lt $text.Length; $index++) {
        $code = [int]$text[$index]
        if ($code -lt 32 -and $code -notin @(9, 10, 13)) {
            $failures.Add("'$($file.FullName)' contains control character U+$($code.ToString('X4')).")
            break
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Text integrity verification passed for $($files.Count) UTF-8 files."
