[CmdletBinding()]
param(
    # Rewrite every fenced region from the declaration instead of only checking it. This is how a
    # field is added to a fact: edit conformance/channel-0.2-facts.json, run with -Apply, review the
    # diff. Twenty sites change together or none does.
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

# Channel 0.2 owned facts.
#
# W1 of the verification foundation plan. One fact -- the five fields of a frame reference -- was
# published in twenty places across five artifacts and maintained by hand, and nine consecutive
# closure cycles carried one instance of the same failure: the fact changed, the edit reached some of
# its surfaces, and the check written to catch that could only see the surfaces its author already
# knew about. AI1, AJ1, AK1 and AL2 are one event four times.
#
# The plan framed the fix as a choice between duplication and citation, and citation costs standalone
# readability: a reader of the grid alone should still learn what the `unseen` cells record. This is
# the third option. The duplication survives for the reader and dies for the maintainer -- every
# publication site is a fenced region rendered from the declaration, so a reader sees the whole fact
# in place and no human hand-writes the second copy.
#
# What that buys over the registry it replaces, and it is more than the plan's acceptance asked for:
# the surface list is not in this file. A fence IS the registration, and it lives in the artifact. So
# there is no exact-count assertion to keep in step, and no way to add a surface without registering
# it -- which is the "a guard scoped to what it can already read certifies its own completeness"
# failure the design verifier's own comments kept naming.
#
# The sweep for an unfenced publication remains, demoted from primary mechanism to backstop: it is
# what catches a surface that states the fact in prose without a fence, which is AJ1's shape.

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$factsPath = Join-Path $repositoryRoot 'conformance\channel-0.2-facts.json'
$channelPath = Join-Path $repositoryRoot 'docs\future\channel'
$failures = [System.Collections.Generic.List[string]]::new()

if (-not (Test-Path -LiteralPath $factsPath)) {
    Write-Host "FAIL: the fact declaration does not exist: '$factsPath'."
    exit 1
}

try { $facts = Get-Content -Raw -LiteralPath $factsPath -Encoding UTF8 | ConvertFrom-Json }
catch { Write-Host "FAIL: invalid JSON in '$factsPath': $($_.Exception.Message)"; exit 1 }

function Get-FlowedText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content)
    return [regex]::Replace(($Content -replace '\*\*', ''), '\s+', ' ').Trim()
}

# The rendering is DERIVED from the field list and checked against the declared string rather than
# trusted. A field added to `fields` and forgotten in `rendering` is the six-site edit arriving inside
# the file that exists to end it.
$emphasised = @($facts.emphasised)
function Get-Rendering {
    param([Parameter(Mandatory = $true)]$Fact)
    $rendered = @()
    $fieldList = @($Fact.fields)
    for ($index = 0; $index -lt $fieldList.Count; $index++) {
        $field = [string]$fieldList[$index]
        $text = if ($emphasised -contains $field) { "**$field**" } else { $field }
        $prefix = if ($index -eq $fieldList.Count - 1) { 'and its ' } else { 'its ' }
        $rendered += "$prefix$text"
    }
    return (($rendered -join ', ') + ' ' + [string]$Fact.scope)
}

$declared = @{}
$fieldNameSets = @{}
foreach ($fact in $facts.facts) {
    $expected = Get-Rendering $fact
    if ([string]$fact.rendering -cne $expected) {
        $failures.Add("The declared rendering of '$($fact.id)' is not what its field list renders to. Declared: '$($fact.rendering)'. Derived: '$expected'. The field list is the fact and the rendering is derived from it, so a rendering edited by hand is the hand-maintained copy this file exists to abolish.")
    }
    $declared[[string]$fact.id] = [string]$fact.rendering
    $fieldNameSets[[string]$fact.id] = (@($fact.fields) -join '|')
}

# The class assertion, moved here from the registry. Every frame reference a property reads identifies
# one declared stimulus step and the same five fields are what it takes to do that; a reference
# introduced with four of them fails without anyone writing a new check, which is what AK1 and AK6
# were. The ordinal's SCOPE differs between the refused reference and the other two and that is the
# fact rather than a slip, so the names are compared and the scope is not.
if ($facts.classAssertion.sameFieldNames) {
    $distinctFieldSets = @($fieldNameSets.Values | Sort-Object -Unique)
    if ($distinctFieldSets.Count -ne 1) {
        $failures.Add("The declared facts do not carry the same field names: $($distinctFieldSets -join ' / '). $($facts.classAssertion.why)")
    }
}

# ---------------------------------------------------------------------------------------------
# The fences.
# ---------------------------------------------------------------------------------------------

$fencePattern = '(?s)<!-- fact:([a-z0-9-]+) -->(.*?)<!-- /fact -->'
$artifactFiles = @(Get-ChildItem -LiteralPath $channelPath -Filter '*.md' -File)
$publicationCounts = @{}
$perArtifactCounts = @{}
$publishingArtifacts = @{}
foreach ($factId in $declared.Keys) { $publicationCounts[$factId] = 0; $perArtifactCounts[$factId] = @{}; $publishingArtifacts[$factId] = [System.Collections.Generic.List[string]]::new() }

foreach ($artifactFile in $artifactFiles) {
    $text = Get-Content -Raw -LiteralPath $artifactFile.FullName -Encoding UTF8
    $rewritten = $false

    # An opening marker with no closing one, or a closing marker with no opening one, would leave a
    # publication half inside the mechanism and half outside it -- which is a surface the fence check
    # cannot see and the backstop sweep can, so it is caught either way; naming it here says which.
    $openCount = ([regex]::Matches($text, '<!-- fact:[a-z0-9-]+ -->')).Count
    $closeCount = ([regex]::Matches($text, '<!-- /fact -->')).Count
    $matchedCount = ([regex]::Matches($text, $fencePattern)).Count
    if ($openCount -ne $closeCount -or $matchedCount -ne $openCount) {
        $failures.Add("'$($artifactFile.Name)' has unbalanced fact fences: $openCount opening markers, $closeCount closing markers, $matchedCount matched pairs.")
        continue
    }

    foreach ($fence in [regex]::Matches($text, $fencePattern)) {
        $factId = $fence.Groups[1].Value
        if (-not $declared.ContainsKey($factId)) {
            $failures.Add("'$($artifactFile.Name)' fences a region as fact '$factId', which the declaration does not define. A fence is the registration of a publication surface, so a fence naming nothing registers nothing.")
            continue
        }
        $publicationCounts[$factId]++
        if (-not $perArtifactCounts[$factId].ContainsKey($artifactFile.Name)) { $perArtifactCounts[$factId][$artifactFile.Name] = 0 }
        $perArtifactCounts[$factId][$artifactFile.Name]++
        if (-not $publishingArtifacts[$factId].Contains($artifactFile.Name)) { $publishingArtifacts[$factId].Add($artifactFile.Name) }

        if ((Get-FlowedText $fence.Groups[2].Value) -cne (Get-FlowedText $declared[$factId])) {
            if ($Apply) { $rewritten = $true }
            else {
                $failures.Add("'$($artifactFile.Name)' publishes '$factId' as '$(Get-FlowedText $fence.Groups[2].Value)' and the declaration renders it '$(Get-FlowedText $declared[$factId])'. Run ``build/verify-channel-0.2-facts.ps1 -Apply`` rather than editing the artifact: a surface corrected by hand is one of twenty, which is how AI1, AJ1, AK1 and AL2 each reached some of their sites and not the rest.")
            }
        }
    }

    if ($Apply -and $rewritten) {
        $updated = [regex]::Replace($text, $fencePattern, {
            param($fence)
            $factId = $fence.Groups[1].Value
            if (-not $declared.ContainsKey($factId)) { return $fence.Value }
            $body = $fence.Groups[2].Value
            # Preserve the site's own line structure: the artifacts wrap at about a hundred columns
            # and a rewrite that reflowed every site onto one line would make each correction's diff
            # unreadable, which is a real cost in a package reviewed by reading.
            $lineCount = @($body -split "`r?`n").Count
            $rendering = $declared[$factId]
            if ($lineCount -le 1) { return "<!-- fact:$factId -->$rendering<!-- /fact -->" }
            $indentMatch = [regex]::Match($body, "`r?`n([ \t]*)")
            $indent = if ($indentMatch.Success) { $indentMatch.Groups[1].Value } else { '' }
            $words = @($rendering -split ' ')
            $perLine = [Math]::Ceiling($words.Count / $lineCount)
            $lines = @()
            for ($start = 0; $start -lt $words.Count; $start += $perLine) {
                $lines += ($words[$start..([Math]::Min($start + $perLine - 1, $words.Count - 1))] -join ' ')
            }
            return "<!-- fact:$factId -->$($lines -join ("`r`n" + $indent))<!-- /fact -->"
        })
        Set-Content -LiteralPath $artifactFile.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "rewrote fenced publications in $($artifactFile.Name)"
    }
}

# Which artifacts publish each fact, and how many times, checked in both directions. Fencing makes
# the registered sites impossible to desynchronise but cannot notice a site DELETED outright: a fence
# registers a surface that exists, so removing one removes its own registration. This is the exact
# count from the registry, kept for AI1's reason -- a lower bound is what let that check certify its
# own scope -- and moved beside the fact, where the person changing the fact sees it.
foreach ($fact in $facts.facts) {
    $factId = [string]$fact.id
    if ($publicationCounts[$factId] -eq 0) {
        $failures.Add("No artifact publishes '$factId'. A declared fact that nothing renders is a fact this file owns and no reader ever sees.")
    }
    $expectedBy = @{}
    foreach ($declaredArtifact in $fact.publishedBy.PSObject.Properties) { $expectedBy[$declaredArtifact.Name] = [int]$declaredArtifact.Value }
    foreach ($expectedArtifact in ($expectedBy.Keys | Sort-Object)) {
        $actual = 0
        if ($perArtifactCounts.ContainsKey($factId) -and $perArtifactCounts[$factId].ContainsKey($expectedArtifact)) { $actual = $perArtifactCounts[$factId][$expectedArtifact] }
        if ($actual -ne $expectedBy[$expectedArtifact]) {
            $failures.Add("'$expectedArtifact' publishes '$factId' $actual times and the declaration says $($expectedBy[$expectedArtifact]). A publication that disappears takes its own fence with it, so the count is what notices; a publication that appears without being declared is a surface nothing renders.")
        }
    }
    if ($perArtifactCounts.ContainsKey($factId)) {
        foreach ($actualArtifact in ($perArtifactCounts[$factId].Keys | Sort-Object)) {
            if (-not $expectedBy.ContainsKey($actualArtifact)) {
                $failures.Add("'$actualArtifact' publishes '$factId' and the declaration does not list it as a publishing artifact. Add it with its count, or remove the fence.")
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------
# The backstop.
#
# Fencing makes the twenty registered sites impossible to desynchronise. It cannot, on its own, catch
# a TWENTY-FIRST surface that states the fact in prose and carries no fence -- which is exactly AJ1's
# shape, an artifact publishing the reference in an abbreviated form that no check written over the
# known surfaces could reach. So the sweep survives, with the fenced regions replaced by a sentinel so
# that a reference phrase followed by its own fenced publication reads as published rather than as
# abbreviated.
# ---------------------------------------------------------------------------------------------

$referencePhrases = '(?:refused-frame reference|refused-frame position|frame that settled it|frame that settled the latch|settling-frame position|settling-frame reference|terminal-frame reference|terminal-frame position)'
# Scoped to the LIVE documents: the design artifacts, the review policy, and the disposition index.
# The retained attestations are deliberately outside it. An attestation records what a reference
# looked like before a correction -- the pre-AK1 record naming a provenance, a reason and a frame
# kind is quoted in four of them -- and those records are retained unmodified by policy, so a sweep
# that reached them would demand an edit the policy forbids. The disposition index IS in scope, and
# that is deliberate for the reason the AL2 sweep gave: a disposition history is where an abbreviated
# form of a record is most likely to be restated, so prose about a reference describes it rather than
# listing its fields.
$reviewsPath = Join-Path $channelPath 'reviews'
$sweepFiles = @($artifactFiles)
foreach ($liveReviewFile in @('README.md', 'channel-0.2-disposition-index.md')) {
    $liveReviewPath = Join-Path $reviewsPath $liveReviewFile
    if (Test-Path -LiteralPath $liveReviewPath) { $sweepFiles += @(Get-Item -LiteralPath $liveReviewPath) }
}

foreach ($sweepFile in $sweepFiles) {
    $sweepText = Get-Content -Raw -LiteralPath $sweepFile.FullName -Encoding UTF8
    $sentinelled = [regex]::Replace($sweepText, $fencePattern, ' <<fenced-publication>> ')
    $flowed = Get-FlowedText $sentinelled

    # A full field list outside every fence. Nothing else in the package should carry these five
    # fields in this order: that string is the fact, and the fact is rendered.
    foreach ($factId in $declared.Keys) {
        $renderedPlain = Get-FlowedText $declared[$factId]
        if ($flowed.IndexOf($renderedPlain, [System.StringComparison]::Ordinal) -ge 0) {
            $failures.Add("'$($sweepFile.Name)' states the whole field list of '$factId' outside a fact fence. Every publication of an owned fact is rendered from the declaration, so an unfenced one is a surface nothing keeps in step -- which is the twenty-first-surface case AJ1 was.")
        }
    }

    # The abbreviated form: a reference phrase followed within 200 characters by four or more of the
    # five field names, with no fenced publication in that window. Four rather than five, because five
    # is the answer and a check that requires the answer can only confirm surfaces that are already
    # right. Prose ABOUT a reference names one or two of its fields and is not reached.
    $fieldNames = @($facts.facts[0].fields | ForEach-Object { [string]$_ })
    foreach ($phraseMatch in [regex]::Matches($flowed, "$referencePhrases(?=(.{0,200}))")) {
        $window = $phraseMatch.Groups[1].Value
        if ($window.IndexOf('<<fenced-publication>>', [System.StringComparison]::Ordinal) -ge 0) { continue }
        $named = @($fieldNames | Where-Object { $window.IndexOf($_, [System.StringComparison]::Ordinal) -ge 0 })
        if ($named.Count -ge 4) {
            $failures.Add("'$($sweepFile.Name)' has a passage that reads as a publication of a frame reference -- the reference followed by $($named.Count) of its five field names: $($named -join ', ') -- with no fact fence. Either it is a surface stating the reference in an abbreviated form, which is AJ1's shape, or it is a new surface that has to be fenced.")
        }
    }

    # AJ6: the justification under a field list must name the fields it is about rather than counting
    # them from the front. The AI1 insertion left the interaction machine arguing that "the first
    # three" -- by then kind, session, and interaction identity -- do not identify the frame, which is
    # a claim about a set that no longer contains the committing endpoint it is about. A list that is
    # counted from the front cannot have a field added to it without breaking the sentence beneath it,
    # and adding a field is now one edit that rewrites twenty sites at once.
    foreach ($fenceMatch in [regex]::Matches($sweepText, $fencePattern)) {
        $after = $sweepText.Substring($fenceMatch.Index + $fenceMatch.Length, [Math]::Min(600, $sweepText.Length - $fenceMatch.Index - $fenceMatch.Length))
        if ((Get-FlowedText $after) -match '(?i)\bthe (?:first|other|last|remaining) (?:two|three|four|five)\b') {
            $failures.Add("'$($sweepFile.Name)' identifies part of a frame reference's field list by position rather than by name, within 600 characters of a fenced publication. Inserting a field renumbers every such sentence. This is AJ6.")
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    exit 1
}

$siteTotal = ($publicationCounts.Values | Measure-Object -Sum).Sum
Write-Host "Channel 0.2 owned-fact verification passed: $(@($facts.facts).Count) declared facts rendered into $siteTotal fenced publications across $(@($artifactFiles).Count) artifacts, with no unfenced publication in $(@($sweepFiles).Count) files."
