# Recomputes the recorded binding source-cost inventory and checks the facts recorded beside it.
#
# Schema 2 (PB8) adds two things the earlier inventory had no way to state: every file declares
# which layer it belongs to — the retained line-delimited experiments or the reusable Portable
# Component Binding — and each stack records a per-layer total. Both are recomputed here, so a file
# added to the portable layer without being measured, or a layer total that drifts from its files,
# fails the build rather than quietly making the comparison wrong.
#
# The inventory also carries the portable realization facts (representation, framing, allocation,
# copy accounting, payload bounds). Each of those states how it is known, and this gate checks that
# every one of them does: an unattributed fact is the failure mode worth catching, because a fact
# nobody can trace is indistinguishable from an assertion nobody checked.

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$measurementPath = Join-Path $repositoryRoot 'interchange\binding-measurements.json'
$measurement = Get-Content -LiteralPath $measurementPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($measurement.schemaVersion -ne 2) {
    throw 'The binding measurement schema version is not supported.'
}

$declaredLayers = @($measurement.layers.PSObject.Properties | ForEach-Object { $_.Name })
if ($declaredLayers.Count -eq 0) {
    throw 'The binding measurement declares no layers.'
}

foreach ($stack in $measurement.stacks) {
    $manualTotal = 0
    $layerTotals = @{}
    foreach ($layer in $declaredLayers) { $layerTotals[$layer] = 0 }

    foreach ($file in $stack.files) {
        $path = Join-Path $repositoryRoot $file.path
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Binding measurement source is missing: $($file.path)"
        }

        $layer = [string]$file.layer
        if ($declaredLayers -notcontains $layer) {
            throw "Binding measurement file '$($file.path)' names undeclared layer '$layer'."
        }

        $actual = @(Get-Content -LiteralPath $path -Encoding UTF8).Count
        if ($actual -ne $file.manualSourceLines) {
            throw "Binding measurement drift for $($file.path): recorded $($file.manualSourceLines), actual $actual."
        }

        $manualTotal += $actual
        $layerTotals[$layer] += $actual
    }

    foreach ($layer in $declaredLayers) {
        $recorded = $stack.layerSourceLines.$layer
        if ($null -eq $recorded) {
            throw "The $($stack.name) measurement records no total for layer '$layer'."
        }

        if ([int]$recorded -ne $layerTotals[$layer]) {
            throw "Layer line total drift for $($stack.name)/$($layer): recorded $recorded, actual $($layerTotals[$layer])."
        }
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

# Every source file under a stack's portable layer is measured. Adding one without measuring it
# would leave the comparison quietly incomplete, which is exactly what a source-cost inventory
# exists to prevent.
$portableDirectories = @(
    @{ Stack = 'Reference'; Path = 'Reference\src\Brontide.Reference.Experimental.Binding\Portable'; Extension = '.cs' },
    @{ Stack = 'Minimal'; Path = 'Minimal\src\Brontide.Minimal.Binding\Portable'; Extension = '.fs' }
)

$measuredPaths = @($measurement.stacks | ForEach-Object { $_.files } | ForEach-Object { ([string]$_.path).Replace('/', '\') })
foreach ($directory in $portableDirectories) {
    $absolute = Join-Path $repositoryRoot $directory.Path
    if (-not (Test-Path -LiteralPath $absolute -PathType Container)) {
        throw "The $($directory.Stack) portable binding directory does not exist: $($directory.Path)"
    }

    foreach ($file in Get-ChildItem -LiteralPath $absolute -File -Filter "*$($directory.Extension)") {
        $relative = Join-Path $directory.Path $file.Name
        if ($measuredPaths -notcontains $relative) {
            throw "Unmeasured portable binding source: $relative. Record it in interchange/binding-measurements.json."
        }
    }
}

# The recorded realization facts carry their provenance, and the vocabulary is closed.
$allowedProvenance = @('declared', 'asserted', 'measured')
$facts = $measurement.portableRealizationFacts
if ($null -eq $facts) {
    throw 'The binding measurement records no portable realization facts.'
}

$expectedRealizations = @('fixed-direct-call', 'negotiated-process')
$recordedRealizations = @($facts.realizations | ForEach-Object { [string]$_.id })
foreach ($realization in $expectedRealizations) {
    if ($recordedRealizations -notcontains $realization) {
        throw "The portable realization facts record nothing for '$realization'."
    }
}

function Test-FactProvenance {
    param(
        [Parameter(Mandatory = $true)]$Fact,
        [Parameter(Mandatory = $true)][string]$Context
    )

    if ($null -eq $Fact.how -or $allowedProvenance -notcontains [string]$Fact.how) {
        throw "$Context does not state how it is known ('declared', 'asserted', or 'measured')."
    }

    if ([string]::IsNullOrWhiteSpace([string]$Fact.source)) {
        throw "$Context states how it is known but names no source."
    }
}

foreach ($realization in $facts.realizations) {
    foreach ($property in $realization.PSObject.Properties) {
        if ($property.Name -eq 'id') { continue }
        Test-FactProvenance -Fact $property.Value -Context "Realization fact '$($realization.id).$($property.Name)'"
    }
}

Test-FactProvenance -Fact $facts.declaredLimits -Context 'The declared limit set'
Test-FactProvenance -Fact $facts.parityBoundary -Context 'The parity boundary'

Write-Host "Binding measurement verification passed for $($measurement.stacks.Count) independent stacks across $($declaredLayers.Count) layers, with $($recordedRealizations.Count) realizations' facts attributed."
