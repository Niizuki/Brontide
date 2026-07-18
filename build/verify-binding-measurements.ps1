$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$measurementPath = Join-Path $repositoryRoot 'interchange\binding-measurements.json'
$measurement = Get-Content -LiteralPath $measurementPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($measurement.schemaVersion -ne 1) {
    throw 'The binding measurement schema version is not supported.'
}

foreach ($stack in $measurement.stacks) {
    $manualTotal = 0
    foreach ($file in $stack.files) {
        $path = Join-Path $repositoryRoot $file.path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Binding measurement source is missing: $($file.path)"
        }

        $actual = @(Get-Content -LiteralPath $path -Encoding UTF8).Count
        if ($actual -ne $file.manualSourceLines) {
            throw "Binding measurement drift for $($file.path): recorded $($file.manualSourceLines), actual $actual."
        }

        $manualTotal += $actual
    }

    if ($manualTotal -ne $stack.manualSourceLines) {
        throw "Manual binding line total drift for $($stack.name): recorded $($stack.manualSourceLines), actual $manualTotal."
    }

    if ($stack.generatedSourceLines -ne 0) {
        throw "The $($stack.name) measurement claims generated source but names no generated source inventory."
    }

    if (($stack.manualSourceLines + $stack.generatedSourceLines) -ne $stack.totalSourceLines) {
        throw "The $($stack.name) binding source totals are inconsistent."
    }
}

Write-Host "Binding measurement verification passed for $($measurement.stacks.Count) independent stacks."
