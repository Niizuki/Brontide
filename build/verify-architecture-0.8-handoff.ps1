$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$vectorPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$ledgerPath = Join-Path $repositoryRoot 'docs\archive\architecture\architecture-0.8-handoff-requirements-and-risk-ledger.md'
$referenceNotePath = Join-Path $repositoryRoot 'Reference\docs\architecture-0.8-handoff-implementation-notes.md'
$minimalNotePath = Join-Path $repositoryRoot 'Minimal\docs\architecture-0.8-handoff-implementation-notes.md'

function Assert-Handoff {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "Architecture 0.8 handoff: $Message" }
}

$inventory = Get-Content -Raw -LiteralPath $vectorPath -Encoding UTF8 | ConvertFrom-Json
$ledger = Get-Content -Raw -LiteralPath $ledgerPath -Encoding UTF8
$referenceNote = Get-Content -Raw -LiteralPath $referenceNotePath -Encoding UTF8
$minimalNote = Get-Content -Raw -LiteralPath $minimalNotePath -Encoding UTF8
$vectors = @($inventory.vectors)

Assert-Handoff ($vectors.Count -eq 33) "expected 33 adversarial/evidence vectors, found $($vectors.Count)"
Assert-Handoff (@($vectors.id | Select-Object -Unique).Count -eq $vectors.Count) 'vector ids are missing or duplicated'

foreach ($vector in $vectors) {
    $count = ([regex]::Matches($ledger, [regex]::Escape($vector.id))).Count
    Assert-Handoff ($count -eq 1) "vector '$($vector.id)' must occur exactly once in the handoff ledger; found $count"
}

foreach ($number in 1..14) {
    $change = "C$number"
    $rows = ([regex]::Matches($ledger, "(?m)^\| A08-HO-$change \|")).Count
    Assert-Handoff ($rows -eq 1) "change $change must have exactly one requirements-register row"
}

Assert-Handoff ($inventory.coverage.C13 -and $inventory.coverage.C14) 'canonical documentation-only coverage must name C13 and C14'
Assert-Handoff (([regex]::Matches($ledger, [regex]::Escape('coverage.C13'))).Count -eq 1) 'coverage.C13 must occur exactly once in the ledger'
Assert-Handoff (([regex]::Matches($ledger, [regex]::Escape('coverage.C14'))).Count -eq 1) 'coverage.C14 must occur exactly once in the ledger'
Assert-Handoff ($ledger -match 'Channel[\s\S]+Portable Binding and Shape floor[\s\S]+Flow conformance') 'the decided evidence order is incomplete'
Assert-Handoff ($ledger -match 'BR-07-CONSTRAINT-001' -and $ledger -match 'conflicting-rework') 'the Architecture 0.7 poisoning supersession is missing'
Assert-Handoff ($ledger -match 'Component Management' -and $ledger -match 'Mediation' -and $ledger -match 'outside this ledger') 'the non-normative scope exclusions are incomplete'

Assert-Handoff ($referenceNote -match 'BR-08-ADV-C11-001' -and $referenceNote -match 'carried-parent-chain') 'Reference C11 representation choice is missing'
Assert-Handoff ($referenceNote -match 'no post-issuance[\s\S]+revocation' -and $referenceNote -match 'Architecture 0.7') 'Reference revocation ceiling or target boundary is missing'
Assert-Handoff ($minimalNote -match 'BR-08-ADV-C11-001' -and $minimalNote -match 'resolved-parent-reference') 'Minimal C11 representation choice is missing'
Assert-Handoff ($minimalNote -match 'no current post-issuance[\s\S]+revocation' -and $minimalNote -match 'Architecture 0.7') 'Minimal revocation ceiling or target boundary is missing'

Write-Host 'Architecture 0.8 handoff verification passed: C1-C14, 33 vectors, 2 documentation-only coverage entries, and both stack representation ceilings accounted.'
