$ErrorActionPreference = 'Stop'

# Channel 0.2 gate self-checks -- the two verifications that check the GATES rather than the design,
# behind an explicit switch rather than in every run of the repository gate.
#
# WHAT IS HERE AND WHY IT IS SEPARATE. `build/verify-interchange.ps1` runs twenty-four PowerShell
# verifications. These two are 411 seconds of the 442 they take together, measured:
#
#   verify-channel-0.2-guards.ps1     360.2s   the 73-probe corpus
#   verify-channel-0.2-coverage.ps1    50.5s   condition and operand coverage of three gates
#   the other twenty-two                31.0s
#
# The cost is not accidental and it is not a defect to optimise away: a probe corpus works by running
# a gate once per probe, and a coverage measure works by running a gate under a line trace. Both are
# expensive for the same reason they are useful -- they execute the thing they are measuring.
#
# AT7 is why they are here. Adding a second coverage unit and three probes took the repository gate
# from 13 minutes to 23-25 against a 30-minute ceiling, which is a gate that fails on a slow morning
# rather than on a defect. The first answer was to raise the ceiling; the owner's answer was to put
# the expensive half behind a switch, which is better, because a ceiling absorbs a cost and a switch
# names it.
#
# WHAT THIS GIVES UP, STATED HERE BECAUSE IT IS THE HALF THAT WILL HURT. AO3 kept the probe corpus on
# the argument that an unmeasured guard rots quietly, and four probes had already stopped applying
# before anyone noticed. Running the corpus on demand rather than on every commit reopens exactly
# that gap: a correction can now rot a probe and merge, and the next run to notice is the nightly one
# or the next person who asks. Three things hold it:
#
#   - the scheduled run in .github/workflows/ci.yml, which is the floor rather than the plan;
#   - `-IncludeGateSelfChecks` on the repository gate, for a branch that touches these files; and
#   - the rule that a pass working on the Channel 0.2 verification foundation runs this before it
#     reports, which the verification foundation plan states as a condition rather than a courtesy.
#
# A rotted probe found by the nightly run is a finding against whoever merged past it, not against
# this file.

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-Checked {
    param([Parameter(Mandatory = $true)][scriptblock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command"
    }
}

# The probe corpus. Each probe makes one guard's own subject present in the package and requires that
# guard to return the verdict it owes. It mutates the working tree and restores from bytes it read
# first, and it refuses to run over a path with uncommitted changes. AO3.
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-guards.ps1')
}

# And the question the probes cannot ask: not "does this guard fire on its own subject" but "did this
# check run at all", and below it "did this operand of it run at all". A guard whose key expired keeps
# its comment, keeps its code, and stops being reached -- which is AP1 and five of the AQ findings,
# and no probe could have caught any of them, because a probe tests a guard someone already suspected.
# AR2, widened by AT4. It refuses a tree with uncommitted design artifacts, for the reason in the file.
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-coverage.ps1')
}

# And the question neither of those asks: not "does this guard fire" nor "did this check run", but
# "is a property red on conforming behaviour nobody thought to write down". The properties gate
# evaluates a hundred generated conforming vectors on every commit, which is the continuous floor;
# this raises the count, because a rate is worth more at two thousand than at a hundred and the cost
# is superlinear enough that the deep run belongs here rather than in every commit. It raises AZ3s
# dropped-field sweep with it, for the same reason and on its own count: the sweep costs eighteen
# further C4-P2 evaluations per vector it covers, so pinning it to the generated count would multiply
# what the probe corpus next door costs rather than what one run costs.
Invoke-Checked {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repositoryRoot 'build\verify-channel-0.2-properties.ps1') -GeneratedCount 2000 -SweptCount 500
}

Write-Host 'Channel 0.2 gate self-checks passed: the guard corpus, the gate-coverage measure and the deep generated-vector run all agree with the gates they check.'
