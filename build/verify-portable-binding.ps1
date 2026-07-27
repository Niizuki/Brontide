# Validates the Portable Component Binding neutral contract artifacts under binding/portable/, and
# then each stack's native evidence against them.
#
# The neutral section loads neither stack and starts no provider. It does build the
# implementation-neutral provider, because PB5 checks that endpoint's independence from what it
# actually resolved, and that check belongs with the neutral layer rather than with either stack.
#
# The PB2 and PB3 sections below build the Reference and Minimal bindings and run their portable
# vectors, which since PB4 include the direct-versus-process parity matrix, the Channel 0.1 coverage
# accounting, and that whole matrix repeated across a real process boundary. PB5 adds the cross-stack
# rows: each stack's host against the other's provider, and both hosts against the neutral one.
#
# Pass -NeutralOnly when a caller already runs the stack suites itself, as the repository gate does.
#
# The golden CBOR encodings are re-derived here from their value descriptions rather than trusted, so
# a checked-in byte string that does not follow the deterministic rules fails the build.

param([switch]$NeutralOnly)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$portableRoot = Join-Path $repositoryRoot 'binding\portable'
$schemaRoot = Join-Path $portableRoot 'schemas'
$vectorRoot = Join-Path $portableRoot 'vectors'
$channelVectorPath = Join-Path $repositoryRoot 'conformance\channel-0.1-vectors.json'
$adversarialPath = Join-Path $repositoryRoot 'conformance\architecture-0.8-adversarial-vectors.json'
$failures = [System.Collections.Generic.List[string]]::new()

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        return Get-Content -Raw -LiteralPath $Path -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        $failures.Add("Invalid JSON in '$Path': $($_.Exception.Message)")
        return $null
    }
}

# ---------------------------------------------------------------------------
# Deterministic CBOR core encoder (RFC 8949 section 4.2.1), restricted to the
# subset schemas/payload-representation.json declares.
# ---------------------------------------------------------------------------

function Write-CborHead {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[byte]]$Buffer,
        [Parameter(Mandatory = $true)][int]$Major,
        [Parameter(Mandatory = $true)][UInt64]$Argument
    )

    $prefix = [byte]($Major -shl 5)
    if ($Argument -lt 24) {
        $Buffer.Add([byte]($prefix -bor [byte]$Argument))
    }
    elseif ($Argument -le 255) {
        $Buffer.Add([byte]($prefix -bor 24))
        $Buffer.Add([byte]$Argument)
    }
    elseif ($Argument -le 65535) {
        $Buffer.Add([byte]($prefix -bor 25))
        $Buffer.Add([byte](($Argument -shr 8) -band 0xFF))
        $Buffer.Add([byte]($Argument -band 0xFF))
    }
    elseif ($Argument -le 4294967295) {
        $Buffer.Add([byte]($prefix -bor 26))
        for ($shift = 24; $shift -ge 0; $shift -= 8) {
            $Buffer.Add([byte](($Argument -shr $shift) -band 0xFF))
        }
    }
    else {
        $Buffer.Add([byte]($prefix -bor 27))
        for ($shift = 56; $shift -ge 0; $shift -= 8) {
            $Buffer.Add([byte](($Argument -shr $shift) -band 0xFF))
        }
    }
}

function ConvertTo-CborInteger {
    param([Parameter(Mandatory = $true)][long]$Value)

    $buffer = [System.Collections.Generic.List[byte]]::new()
    if ($Value -ge 0) {
        Write-CborHead -Buffer $buffer -Major 0 -Argument ([UInt64]$Value)
    }
    else {
        # Major type 1 encodes -1-n, so n is the magnitude minus one.
        Write-CborHead -Buffer $buffer -Major 1 -Argument ([UInt64](-1 - $Value))
    }

    return ,$buffer.ToArray()
}

function ConvertTo-CborText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $buffer = [System.Collections.Generic.List[byte]]::new()
    Write-CborHead -Buffer $buffer -Major 3 -Argument ([UInt64]$bytes.Length)
    $buffer.AddRange($bytes)
    return ,$buffer.ToArray()
}

function Compare-CborKey {
    param(
        [Parameter(Mandatory = $true)][byte[]]$Left,
        [Parameter(Mandatory = $true)][byte[]]$Right
    )

    $shared = [Math]::Min($Left.Length, $Right.Length)
    for ($index = 0; $index -lt $shared; $index++) {
        if ($Left[$index] -ne $Right[$index]) {
            return $Left[$index] - $Right[$index]
        }
    }

    return $Left.Length - $Right.Length
}

function ConvertTo-CborMap {
    param([Parameter(Mandatory = $true)]$Entries)

    # $Entries is an array of [pscustomobject]@{ Key = <byte[]>; Value = <byte[]> }.
    $sorted = @($Entries)
    for ($outer = 1; $outer -lt $sorted.Count; $outer++) {
        $current = $sorted[$outer]
        $inner = $outer - 1
        while ($inner -ge 0 -and (Compare-CborKey -Left $sorted[$inner].Key -Right $current.Key) -gt 0) {
            $sorted[$inner + 1] = $sorted[$inner]
            $inner--
        }
        $sorted[$inner + 1] = $current
    }

    $buffer = [System.Collections.Generic.List[byte]]::new()
    Write-CborHead -Buffer $buffer -Major 5 -Argument ([UInt64]$sorted.Count)
    foreach ($entry in $sorted) {
        $buffer.AddRange($entry.Key)
        $buffer.AddRange($entry.Value)
    }

    return ,$buffer.ToArray()
}

function ConvertTo-CborValue {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Context
    )

    $kind = [string]$Value.kind
    switch ($kind) {
        'text' { return ConvertTo-CborText ([string]$Value.value) }
        'boolean' {
            $flag = [bool]$Value.value
            return ,[byte[]]@($(if ($flag) { 0xF5 } else { 0xF4 }))
        }
        'unit' { return ,[byte[]]@(0xF6) }
        'integer' { return ConvertTo-CborInteger ([long]$Value.value) }
        'bytes' {
            $hex = [string]$Value.hex
            if ($hex.Length % 2 -ne 0 -or $hex -notmatch '^[0-9a-f]*$') {
                $failures.Add("$Context has a malformed 'hex' byte string.")
                return ,[byte[]]@()
            }

            $raw = [System.Collections.Generic.List[byte]]::new()
            for ($index = 0; $index -lt $hex.Length; $index += 2) {
                $raw.Add([Convert]::ToByte($hex.Substring($index, 2), 16))
            }

            $buffer = [System.Collections.Generic.List[byte]]::new()
            Write-CborHead -Buffer $buffer -Major 2 -Argument ([UInt64]$raw.Count)
            $buffer.AddRange($raw)
            return ,$buffer.ToArray()
        }
        'decimal' {
            $exponent = [long]$Value.exponent
            $mantissa = [long]$Value.mantissa
            if ($mantissa -eq 0 -and $exponent -ne 0) {
                $failures.Add("$Context encodes a zero mantissa with a non-zero exponent; the canonical form is exponent 0.")
            }
            if ($mantissa -ne 0 -and ($mantissa % 10) -eq 0) {
                $failures.Add("$Context has an unnormalized mantissa: it is divisible by 10.")
            }
            if ($exponent -lt -28 -or $exponent -gt 28) {
                $failures.Add("$Context has a Decimal exponent outside the declared -28..28 range.")
            }

            $buffer = [System.Collections.Generic.List[byte]]::new()
            Write-CborHead -Buffer $buffer -Major 6 -Argument ([UInt64]4)
            Write-CborHead -Buffer $buffer -Major 4 -Argument ([UInt64]2)
            $buffer.AddRange((ConvertTo-CborInteger $exponent))
            $buffer.AddRange((ConvertTo-CborInteger $mantissa))
            return ,$buffer.ToArray()
        }
        'sequence' {
            $items = @($Value.items)
            $buffer = [System.Collections.Generic.List[byte]]::new()
            Write-CborHead -Buffer $buffer -Major 4 -Argument ([UInt64]$items.Count)
            foreach ($item in $items) {
                $buffer.AddRange((ConvertTo-CborValue -Value $item -Context $Context))
            }
            return ,$buffer.ToArray()
        }
        'choice' {
            $buffer = [System.Collections.Generic.List[byte]]::new()
            Write-CborHead -Buffer $buffer -Major 4 -Argument ([UInt64]2)
            $buffer.AddRange((ConvertTo-CborText ([string]$Value.case)))
            $buffer.AddRange((ConvertTo-CborValue -Value $Value.value -Context $Context))
            return ,$buffer.ToArray()
        }
        'record' {
            $fieldEntries = @()
            if ($null -ne $Value.fields) {
                foreach ($property in $Value.fields.PSObject.Properties) {
                    $fieldEntries += [pscustomobject]@{
                        Key   = ConvertTo-CborText $property.Name
                        Value = ConvertTo-CborValue -Value $property.Value -Context $Context
                    }
                }
            }

            $fragmentEntries = @()
            if ($null -ne $Value.fragments) {
                foreach ($fragment in $Value.fragments.PSObject.Properties) {
                    $innerEntries = @()
                    foreach ($field in $fragment.Value.PSObject.Properties) {
                        $innerEntries += [pscustomobject]@{
                            Key   = ConvertTo-CborText $field.Name
                            Value = ConvertTo-CborValue -Value $field.Value -Context $Context
                        }
                    }

                    $fragmentEntries += [pscustomobject]@{
                        Key   = ConvertTo-CborText $fragment.Name
                        Value = ConvertTo-CborMap $innerEntries
                    }
                }
            }

            $buffer = [System.Collections.Generic.List[byte]]::new()
            Write-CborHead -Buffer $buffer -Major 4 -Argument ([UInt64]2)
            $buffer.AddRange((ConvertTo-CborMap $fieldEntries))
            $buffer.AddRange((ConvertTo-CborMap $fragmentEntries))
            return ,$buffer.ToArray()
        }
        default {
            $failures.Add("$Context uses unknown value kind '$kind'.")
            return ,[byte[]]@()
        }
    }
}

function ConvertTo-HexString {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $builder = [System.Text.StringBuilder]::new()
    foreach ($byte in $Bytes) {
        [void]$builder.Append($byte.ToString('x2'))
    }

    return $builder.ToString()
}

# ---------------------------------------------------------------------------
# Load artifacts
# ---------------------------------------------------------------------------

$requiredPaths = @(
    (Join-Path $schemaRoot 'references-and-shape-floor.json'),
    (Join-Path $schemaRoot 'component-contract.json'),
    (Join-Path $schemaRoot 'authority-presentation.json'),
    (Join-Path $schemaRoot 'payload-representation.json'),
    (Join-Path $schemaRoot 'limits-and-lifecycle.json'),
    (Join-Path $schemaRoot 'binding-plan.json'),
    (Join-Path $schemaRoot 'channel-envelope.json'),
    (Join-Path $schemaRoot 'binding-observation.json'),
    (Join-Path $vectorRoot 'fixture-contract.json'),
    (Join-Path $vectorRoot 'catalog-fixture-contract.json'),
    (Join-Path $vectorRoot 'golden-encodings.json'),
    (Join-Path $vectorRoot 'establishment-and-shapes.json'),
    (Join-Path $vectorRoot 'authority-and-resources.json'),
    (Join-Path $vectorRoot 'limits-lifecycle-and-channel.json'),
    (Join-Path $vectorRoot 'plan-observation-and-parity.json'),
    $channelVectorPath,
    $adversarialPath
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Required portable-binding artifact does not exist: '$path'.")
    }
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    Write-Error "Portable binding verification failed with $($failures.Count) issue(s)."
    exit 1
}

$channelInventory = Read-JsonFile $channelVectorPath
$adversarialInventory = Read-JsonFile $adversarialPath
$envelopeSchema = Read-JsonFile (Join-Path $schemaRoot 'channel-envelope.json')
$goldenInventory = Read-JsonFile (Join-Path $vectorRoot 'golden-encodings.json')

$schemaFiles = Get-ChildItem -LiteralPath $schemaRoot -Filter '*.json' -File
$vectorFiles = Get-ChildItem -LiteralPath $vectorRoot -Filter '*.json' -File |
    Where-Object { $_.Name -notin @('fixture-contract.json', 'catalog-fixture-contract.json', 'golden-encodings.json') }

# ---------------------------------------------------------------------------
# Neutrality: the checked-in neutral layer stays data only
# ---------------------------------------------------------------------------

$nonDataFiles = Get-ChildItem -LiteralPath $portableRoot -Recurse -File |
    Where-Object { $_.Extension.ToLowerInvariant() -notin @('.json', '.md') }
foreach ($file in $nonDataFiles) {
    $failures.Add("The neutral layer must contain data and documentation only; found '$($file.FullName)'.")
}

foreach ($file in $schemaFiles) {
    $schema = Read-JsonFile $file.FullName
    if ($null -eq $schema) { continue }
    if ($schema.schemaVersion -ne 1) {
        $failures.Add("'$($file.Name)' must use schemaVersion 1.")
    }
    if ([string]::IsNullOrWhiteSpace([string]$schema.contract.id)) {
        $failures.Add("'$($file.Name)' does not declare a contract id.")
    }
    if ([string]$schema.contract.version -ne '0.1-draft') {
        $failures.Add("'$($file.Name)' must declare contract version '0.1-draft'.")
    }

    $planPath = Join-Path $repositoryRoot ([string]$schema.contract.plan)
    if (-not (Test-Path -LiteralPath $planPath)) {
        $failures.Add("'$($file.Name)' names a plan path that does not exist: '$($schema.contract.plan)'.")
    }
}

# ---------------------------------------------------------------------------
# The Channel taxonomy is reproduced exactly, never extended
# ---------------------------------------------------------------------------

$channelConventions = $channelInventory.conventions
$allowedFrames = @($channelConventions.frameDecision)
$allowedResults = @($channelConventions.resultClass)
$channelVectorIds = @($channelInventory.vectors | ForEach-Object { [string]$_.id })
$adversarialIds = @($adversarialInventory.vectors | ForEach-Object { [string]$_.id })

$protocolCategories = @($envelopeSchema.protocolErrorCategories | ForEach-Object { [string]$_.id })
$processCategories = @($envelopeSchema.processFailureCategories | ForEach-Object { [string]$_.id })
$failureDomains = @($envelopeSchema.failureDomains | ForEach-Object { [string]$_.id })

$authoritativeProtocolCategories = @(
    'malformed-message', 'unsupported-version', 'unsupported-contract', 'unsupported-kind',
    'unsupported-operation', 'correlation-mismatch', 'invalid-payload',
    'invalid-authority-presentation', 'replay-detected', 'limit-exceeded', 'state-violation',
    'internal-protocol-failure'
)
$authoritativeProcessCategories = @(
    'transport-unavailable', 'transport-interrupted', 'timeout', 'peer-terminated',
    'peer-unavailable', 'resource-exhausted', 'unknown'
)
$authoritativeFailureDomains = @('local-endpoint', 'transport', 'remote-endpoint', 'remote-provider', 'unknown')

if ((($protocolCategories | Sort-Object) -join ',') -ne (($authoritativeProtocolCategories | Sort-Object) -join ',')) {
    $failures.Add('channel-envelope.json protocol categories diverge from the Channel 0.1 taxonomy.')
}
if ((($processCategories | Sort-Object) -join ',') -ne (($authoritativeProcessCategories | Sort-Object) -join ',')) {
    $failures.Add('channel-envelope.json process categories diverge from the Channel 0.1 taxonomy.')
}
if ((($failureDomains | Sort-Object) -join ',') -ne (($authoritativeFailureDomains | Sort-Object) -join ',')) {
    $failures.Add('channel-envelope.json failure domains diverge from the Channel 0.1 taxonomy.')
}

# ---------------------------------------------------------------------------
# Vectors
# ---------------------------------------------------------------------------

$allowedCapabilities = @(1..10 | ForEach-Object { "C$_" })
$allowedClassifications = @('valid', 'additive-compatible', 'adversarial')

$seenVectorIds = [System.Collections.Generic.HashSet[string]]::new()
$coveredCapabilities = [System.Collections.Generic.HashSet[string]]::new()
$coveredChannelVectors = [System.Collections.Generic.HashSet[string]]::new()
$coveredProtocolCategories = [System.Collections.Generic.HashSet[string]]::new()
$coveredProcessCategories = [System.Collections.Generic.HashSet[string]]::new()
$coveredFailureDomains = [System.Collections.Generic.HashSet[string]]::new()
$coveredClassifications = [System.Collections.Generic.HashSet[string]]::new()
$vectorCount = 0

foreach ($file in $vectorFiles) {
    $group = Read-JsonFile $file.FullName
    if ($null -eq $group) { continue }
    if ($group.schemaVersion -ne 1) {
        $failures.Add("'$($file.Name)' must use schemaVersion 1.")
    }

    foreach ($schemaReference in @($group.group.schemas)) {
        $schemaPath = Join-Path $portableRoot ([string]$schemaReference)
        if (-not (Test-Path -LiteralPath $schemaPath)) {
            $failures.Add("'$($file.Name)' references missing schema '$schemaReference'.")
        }
    }

    foreach ($vector in @($group.vectors)) {
        $vectorCount++
        $id = [string]$vector.id
        if ($id -notmatch '^PB-\d{2}-[A-Z0-9-]+$') {
            $failures.Add("Vector id '$id' does not match PB-<nn>-<NAME>.")
        }
        elseif (-not $seenVectorIds.Add($id)) {
            $failures.Add("Duplicate portable-binding vector id '$id'.")
        }

        foreach ($clause in @('given', 'when', 'then')) {
            $items = @($vector.$clause)
            if ($items.Count -eq 0 -or @($items | Where-Object { [string]::IsNullOrWhiteSpace([string]$_) }).Count -gt 0) {
                $failures.Add("Vector '$id' has an empty or blank '$clause' clause.")
            }
        }

        $capabilities = @($vector.capabilities)
        if ($capabilities.Count -eq 0) {
            $failures.Add("Vector '$id' names no capability.")
        }
        foreach ($capability in $capabilities) {
            $value = [string]$capability
            if ($allowedCapabilities -notcontains $value) {
                $failures.Add("Vector '$id' names unknown capability '$value'.")
            }
            else {
                [void]$coveredCapabilities.Add($value)
            }
        }

        $classification = [string]$vector.classification
        if ($allowedClassifications -notcontains $classification) {
            $failures.Add("Vector '$id' has invalid classification '$classification'.")
        }
        else {
            [void]$coveredClassifications.Add($classification)
        }

        foreach ($channelVector in @($vector.channelVectors)) {
            $value = [string]$channelVector
            if ([string]::IsNullOrWhiteSpace($value)) { continue }
            if ($channelVectorIds -notcontains $value) {
                $failures.Add("Vector '$id' references unknown Channel vector '$value'.")
            }
            else {
                [void]$coveredChannelVectors.Add($value)
            }
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$vector.linkedArchitectureVector) -and
            $adversarialIds -notcontains [string]$vector.linkedArchitectureVector) {
            $failures.Add("Vector '$id' links missing architecture vector '$($vector.linkedArchitectureVector)'.")
        }

        if ($allowedFrames -notcontains [string]$vector.expected.frameDecision) {
            $failures.Add("Vector '$id' has invalid frameDecision '$($vector.expected.frameDecision)'.")
        }
        if ($allowedResults -notcontains [string]$vector.expected.resultClass) {
            $failures.Add("Vector '$id' has invalid resultClass '$($vector.expected.resultClass)'.")
        }

        $category = [string]$vector.expected.category
        if (-not [string]::IsNullOrWhiteSpace($category)) {
            if ($protocolCategories -notcontains $category) {
                $failures.Add("Vector '$id' names unknown protocol category '$category'.")
            }
            else {
                [void]$coveredProtocolCategories.Add($category)
            }
        }

        $processCategory = [string]$vector.expected.processCategory
        if (-not [string]::IsNullOrWhiteSpace($processCategory)) {
            if ($processCategories -notcontains $processCategory) {
                $failures.Add("Vector '$id' names unknown process category '$processCategory'.")
            }
            else {
                [void]$coveredProcessCategories.Add($processCategory)
            }
        }

        $failureDomain = [string]$vector.expected.failureDomain
        if (-not [string]::IsNullOrWhiteSpace($failureDomain)) {
            if ($failureDomains -notcontains $failureDomain) {
                $failures.Add("Vector '$id' names unknown failure domain '$failureDomain'.")
            }
            else {
                [void]$coveredFailureDomains.Add($failureDomain)
            }
        }

        foreach ($case in @($vector.cases)) {
            $value = [string]$case
            if ([string]::IsNullOrWhiteSpace($value)) { continue }
            if ($processCategories -contains $value) { [void]$coveredProcessCategories.Add($value) }
            if ($failureDomains -contains $value) { [void]$coveredFailureDomains.Add($value) }
            if ($processCategories -notcontains $value -and $failureDomains -notcontains $value) {
                $failures.Add("Vector '$id' has undeclared case '$value'.")
            }
        }

        # An adversarial vector that reaches a provider must say so explicitly: every rejection and
        # denial states its effect count, so "no provider effect" is evidence rather than assumption.
        if ($classification -eq 'adversarial' -and
            [string]$vector.expected.resultClass -in @('protocol-error', 'denial', 'process-failure') -and
            $null -eq $vector.expected.effectCount -and
            [string]::IsNullOrWhiteSpace([string]$vector.expected.effectCountNotAsserted)) {
            $failures.Add("Adversarial vector '$id' neither states an expected effectCount nor explains why it is unobservable.")
        }
        if ($null -ne $vector.expected.effectCount -and
            -not [string]::IsNullOrWhiteSpace([string]$vector.expected.effectCountNotAsserted)) {
            $failures.Add("Vector '$id' both states an effectCount and claims it is unobservable.")
        }
        if ($null -ne $vector.expected.effectCount -and [int]$vector.expected.effectCount -lt 0) {
            $failures.Add("Vector '$id' has a negative effectCount.")
        }

        # A local denial is frameless by contract; a denial that emits a frame contradicts C3.
        if ([string]$vector.expected.resultClass -eq 'denial' -and
            [string]$vector.expected.frameDecision -ne 'none') {
            $failures.Add("Vector '$id' is a denial but does not use frameDecision 'none'.")
        }
    }
}

foreach ($capability in $allowedCapabilities) {
    if (-not $coveredCapabilities.Contains($capability)) {
        $failures.Add("No portable-binding vector covers capability '$capability'.")
    }
}
foreach ($channelVector in $channelVectorIds) {
    if (-not $coveredChannelVectors.Contains($channelVector)) {
        $failures.Add("No portable-binding vector preserves Channel vector '$channelVector'.")
    }
}
foreach ($category in $protocolCategories) {
    if (-not $coveredProtocolCategories.Contains($category)) {
        $failures.Add("No portable-binding vector covers protocol category '$category'.")
    }
}
foreach ($category in $processCategories) {
    if (-not $coveredProcessCategories.Contains($category)) {
        $failures.Add("No portable-binding vector covers process category '$category'.")
    }
}
foreach ($domain in $failureDomains) {
    if (-not $coveredFailureDomains.Contains($domain)) {
        $failures.Add("No portable-binding vector covers failure domain '$domain'.")
    }
}
foreach ($classification in $allowedClassifications) {
    if (-not $coveredClassifications.Contains($classification)) {
        $failures.Add("No portable-binding vector uses classification '$classification'.")
    }
}

# ---------------------------------------------------------------------------
# Golden encodings are re-derived, not trusted
# ---------------------------------------------------------------------------

$goldenCount = 0
$seenGoldenIds = [System.Collections.Generic.HashSet[string]]::new()
foreach ($entry in @($goldenInventory.encodings)) {
    $goldenCount++
    $id = [string]$entry.id
    if (-not $seenGoldenIds.Add($id)) {
        $failures.Add("Duplicate golden encoding id '$id'.")
    }

    $expected = ([string]$entry.cbor).ToLowerInvariant()
    if ($expected -notmatch '^[0-9a-f]+$') {
        $failures.Add("Golden encoding '$id' has a non-hexadecimal 'cbor' value.")
        continue
    }

    $actual = ConvertTo-HexString (ConvertTo-CborValue -Value $entry.value -Context "Golden encoding '$id'")
    if ($actual -ne $expected) {
        $failures.Add("Golden encoding '$id' does not match its value description. Expected '$actual', found '$expected'.")
    }
}

if ($goldenCount -eq 0) {
    $failures.Add('golden-encodings.json declares no encodings.')
}

$framed = $goldenInventory.framedExample
if ($null -ne $framed) {
    $source = @($goldenInventory.encodings | Where-Object { [string]$_.id -eq [string]$framed.of })
    if ($source.Count -ne 1) {
        $failures.Add("The framed example names unknown golden encoding '$($framed.of)'.")
    }
    else {
        $body = ([string]$source[0].cbor).ToLowerInvariant()
        $bodyBytes = $body.Length / 2
        if ([int]$framed.bodyBytes -ne $bodyBytes) {
            $failures.Add("The framed example declares $($framed.bodyBytes) body bytes but its source encodes $bodyBytes.")
        }

        $prefix = ('{0:x8}' -f [int]$bodyBytes)
        $expectedFrame = $prefix + $body
        if (([string]$framed.framed).ToLowerInvariant() -ne $expectedFrame) {
            $failures.Add("The framed example does not carry a correct 4-byte big-endian length prefix. Expected '$expectedFrame'.")
        }
    }
}

$seenRejectedIds = [System.Collections.Generic.HashSet[string]]::new()
foreach ($rejected in @($goldenInventory.rejectedEncodings)) {
    $id = [string]$rejected.id
    if (-not $seenRejectedIds.Add($id)) {
        $failures.Add("Duplicate rejected encoding id '$id'.")
    }
    if (([string]$rejected.cbor).ToLowerInvariant() -notmatch '^[0-9a-f]+$') {
        $failures.Add("Rejected encoding '$id' has a non-hexadecimal 'cbor' value.")
    }
    if ([string]::IsNullOrWhiteSpace([string]$rejected.reason)) {
        $failures.Add("Rejected encoding '$id' states no reason.")
    }
    $category = [string]$rejected.expected.category
    if ($protocolCategories -notcontains $category) {
        $failures.Add("Rejected encoding '$id' names unknown protocol category '$category'.")
    }

    # A rejected encoding that happens to equal a golden one would be self-contradictory.
    if (@($goldenInventory.encodings | Where-Object { ([string]$_.cbor).ToLowerInvariant() -eq ([string]$rejected.cbor).ToLowerInvariant() }).Count -gt 0) {
        $failures.Add("Rejected encoding '$id' is byte-identical to an accepted golden encoding.")
    }
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    Write-Error "Portable binding verification failed with $($failures.Count) issue(s)."
    exit 1
}

Write-Host "Portable binding verification passed: $($schemaFiles.Count) neutral schemas, $vectorCount vectors covering $($allowedCapabilities.Count) capabilities and $($channelVectorIds.Count) Channel vectors, and $goldenCount re-derived golden encodings."

# ---------------------------------------------------------------------------
# PB2: the Reference stack's native evidence against the neutral contract
# ---------------------------------------------------------------------------

function Invoke-Checked {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command"
    }
}

$neutralProviderProject = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\PortableBinding.NeutralProvider.csproj'
Invoke-Checked { dotnet build $neutralProviderProject }

# ---------------------------------------------------------------------------
# PB5: the implementation-neutral provider imports neither stack
# ---------------------------------------------------------------------------
#
# The repository's project-graph and assembly-graph guards scope themselves to Reference\ and
# Minimal\, so this endpoint is outside both by construction. Its independence is therefore checked
# here, against what it actually resolved rather than against what its project file says.

$neutralDeps = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\bin\Debug\net10.0\PortableBinding.NeutralProvider.deps.json'
if (-not (Test-Path -LiteralPath $neutralDeps)) {
    Write-Error "The implementation-neutral provider was not built: '$neutralDeps' does not exist."
    exit 1
}

$neutralGraph = Get-Content -Raw -LiteralPath $neutralDeps -Encoding UTF8 | ConvertFrom-Json
$neutralLibraries = @($neutralGraph.libraries.PSObject.Properties | ForEach-Object { $_.Name })
$borrowed = @($neutralLibraries | Where-Object { $_ -like 'Brontide.*' })
if ($borrowed.Count -gt 0) {
    Write-Error "The implementation-neutral provider resolved Brontide assemblies: $($borrowed -join ', ')."
    exit 1
}

Write-Host "Implementation-neutral provider independence verified: $($neutralLibraries.Count) resolved libraries, none from either stack."


if ($NeutralOnly) {
    Write-Host 'Skipping the native portable evidence for both stacks: -NeutralOnly was requested.'
    exit 0
}

$portableTests = Join-Path $repositoryRoot 'Reference\tests\Brontide.Reference.Interchange.Tests\Brontide.Reference.Interchange.Tests.csproj'
$referenceProviderProject = Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Interchange.Provider\Brontide.Reference.Interchange.Provider.csproj'
$minimalProviderProject = Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Interchange.Provider\Brontide.Minimal.Interchange.Provider.fsproj'

# PB5 pairs the implementations, so every endpoint the matrix names is built before either stack's
# suite runs, and both stacks see all three provider paths. The neutral provider was already built
# above, because its independence is checked even when only the neutral layer is verified.
Invoke-Checked { dotnet build $referenceProviderProject }
Invoke-Checked { dotnet build $minimalProviderProject }

$env:BRONTIDE_REFERENCE_PROVIDER = Join-Path $repositoryRoot 'Reference\src\Brontide.Reference.Interchange.Provider\bin\Debug\net10.0\Brontide.Reference.Interchange.Provider.exe'
$env:BRONTIDE_MINIMAL_PROVIDER = Join-Path $repositoryRoot 'Minimal\src\Brontide.Minimal.Interchange.Provider\bin\Debug\net10.0\Brontide.Minimal.Interchange.Provider.exe'
$env:BRONTIDE_NEUTRAL_PROVIDER = Join-Path $repositoryRoot 'binding\neutral-provider\PortableBinding.NeutralProvider\bin\Debug\net10.0\PortableBinding.NeutralProvider.exe'

Invoke-Checked { dotnet build $portableTests }
Invoke-Checked { dotnet test $portableTests --no-build --filter 'FullyQualifiedName~Brontide.Reference.Interchange.Tests.Portable' }

# The cross-process suite runs the same portable contract over a real duplex process boundary,
# including every PB4 parity scenario; the cross-stack and neutral-provider suites carry the same
# matrix against the Minimal endpoint and the implementation-neutral one.
Invoke-Checked {
    dotnet test $portableTests --no-build --filter 'Category=CrossProcess&FullyQualifiedName~Portable'
}

Write-Host 'Reference native portable-binding evidence passed, including the cross-process, cross-stack, and neutral-provider realizations.'

# ---------------------------------------------------------------------------
# PB3: the Minimal stack's native evidence against the same neutral contract
# ---------------------------------------------------------------------------

$minimalPortableTests = Join-Path $repositoryRoot 'Minimal\tests\Brontide.Minimal.Interchange.Tests\Brontide.Minimal.Interchange.Tests.fsproj'

Invoke-Checked { dotnet build $minimalPortableTests }
Invoke-Checked { dotnet test $minimalPortableTests --no-build --filter 'FullyQualifiedName~Brontide.Minimal.Interchange.Tests.Portable' }

# The Minimal cross-process suite serves the portable contract over a real duplex process boundary
# through its own --portable verb, including every PB4 parity scenario; the cross-stack and
# neutral-provider suites carry the same matrix against the Reference endpoint and the
# implementation-neutral one.
Invoke-Checked {
    dotnet test $minimalPortableTests --no-build --filter 'Category=CrossProcess&FullyQualifiedName~Portable'
}

Write-Host 'Minimal native portable-binding evidence passed, including the cross-process, cross-stack, and neutral-provider realizations.'
exit 0
