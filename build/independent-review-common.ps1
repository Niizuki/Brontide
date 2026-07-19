function Get-CanonicalTextHash {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = [System.IO.File]::ReadAllText($Path)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return -join @($sha256.ComputeHash($bytes) | ForEach-Object { $_.ToString('X2') })
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-LatestArchitectureIdentity {
    param([Parameter(Mandatory = $true)][string]$RepositoryRoot)

    $candidates = @(
        foreach ($file in Get-ChildItem -LiteralPath $RepositoryRoot -File -Filter 'Brontide-Architecture-*.md') {
            if ($file.BaseName -match '^Brontide-Architecture-(?<revision>[0-9]+\.[0-9]+)$') {
                [pscustomobject]@{
                    Path = $file.FullName
                    Name = $file.Name
                    Revision = $Matches.revision
                }
            }
        }
    )

    @(
        $candidates |
            Sort-Object -Property @{ Expression = { [version]$_.Revision }; Descending = $true } |
            Select-Object -First 1
    )
}
