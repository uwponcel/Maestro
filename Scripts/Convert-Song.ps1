<#
.SYNOPSIS
    Converts legacy Maestro song JSON to new compact format.

.DESCRIPTION
    Converts song files from the verbose KeyDown/KeyUp/Wait command format
    to the compact Note:Duration string format.

.PARAMETER InputPath
    Path to the legacy JSON song file.

.PARAMETER OutputPath
    Path for the converted song file.

.PARAMETER Bpm
    Target BPM for duration calculations. Default: 90

.EXAMPLE
    .\Convert-Song.ps1 -InputPath "Songs\Despacito - Luis Fonsi.json" -OutputPath "Songs\Despacito - Luis Fonsi - new.json" -Bpm 90
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [int]$Bpm = 90
)

# NumPad to note mapping
$NumPadToNote = @{
    'NumPad1' = 'C'
    'NumPad2' = 'D'
    'NumPad3' = 'E'
    'NumPad4' = 'F'
    'NumPad5' = 'G'
    'NumPad6' = 'A'
    'NumPad7' = 'B'
    'NumPad8' = 'C^'  # High C (C from next octave up, special key)
}

$SharpMapping = @{
    'NumPad1' = 'C#'
    'NumPad2' = 'D#'
    'NumPad3' = 'F#'
    'NumPad4' = 'G#'
    'NumPad5' = 'A#'
}

function Get-Duration {
    param([int]$Ms, [int]$TargetBpm)

    # ms = (60000 / BPM) * (4 / note_value)
    # note_value = 4 * 60000 / (ms * BPM)
    $noteValue = (4 * 60000) / ($Ms * $TargetBpm)

    # Standard durations
    $durations = @(1, 2, 4, 8, 16, 32)

    $closest = 4
    $minDiff = [double]::MaxValue

    foreach ($d in $durations) {
        $diff = [Math]::Abs($noteValue - $d)
        if ($diff -lt $minDiff) {
            $minDiff = $diff
            $closest = $d
        }
    }

    # Check for dotted notes (1.5x longer = divide value by 1.5)
    $dottedValue = $noteValue * 1.5
    foreach ($d in $durations) {
        $diff = [Math]::Abs($dottedValue - $d)
        if ($diff -lt $minDiff) {
            $minDiff = $diff
            $closest = "$d."
        }
    }

    return $closest.ToString()
}

# Read input file
$data = Get-Content -Path $InputPath -Raw | ConvertFrom-Json

# Initialize result
$result = @{
    name       = $data.name
    artist     = $data.artist
    instrument = $data.instrument
    bpm        = $Bpm
    notes      = @()
}

$currentOctave = 0  # 0 = default, 1 = up, -1 = down
$currentNotes = @()
$isAltDown = $false
$currentLine = @()

foreach ($cmd in $data.commands) {
    if ($cmd.type -eq 'KeyDown') {
        if ($cmd.key -eq 'LeftAlt') {
            $isAltDown = $true
        }
        elseif ($cmd.key -eq 'NumPad9') {
            $currentOctave++  # Go up one octave
        }
        elseif ($cmd.key -eq 'NumPad0') {
            $currentOctave--  # Go down one octave
        }
        elseif ($cmd.key -like 'NumPad*') {
            $note = $null

            if ($isAltDown -and $SharpMapping.ContainsKey($cmd.key)) {
                $note = $SharpMapping[$cmd.key]
            }
            elseif ($NumPadToNote.ContainsKey($cmd.key)) {
                $note = $NumPadToNote[$cmd.key]
            }

            if ($note) {
                # Add octave suffix
                if ($currentOctave -eq 1) {
                    $note += '+'
                }
                elseif ($currentOctave -eq -1) {
                    $note += '-'
                }
                $currentNotes += $note
            }
        }
    }
    elseif ($cmd.type -eq 'KeyUp') {
        if ($cmd.key -eq 'LeftAlt') {
            $isAltDown = $false
        }
        # Octave keys: keep state until next change
    }
    elseif ($cmd.type -eq 'Wait' -and $cmd.duration -gt 0) {
        $duration = Get-Duration -Ms $cmd.duration -TargetBpm $Bpm

        # Output all notes collected before this wait
        if ($currentNotes.Count -gt 0) {
            # If multiple notes, give early ones short duration, last gets main duration
            for ($i = 0; $i -lt $currentNotes.Count; $i++) {
                $note = $currentNotes[$i]
                if ($i -lt $currentNotes.Count - 1) {
                    # Early notes get 32nd note duration (very quick)
                    $currentLine += "${note}:32"
                } else {
                    # Last note gets the actual duration
                    $currentLine += "${note}:${duration}"
                }
            }
            $currentNotes = @()
        }
        else {
            # Rest
            $currentLine += "R:${duration}"
        }

        # Group into lines of ~8 notes for readability
        if ($currentLine.Count -ge 8) {
            $result.notes += ($currentLine -join ' ')
            $currentLine = @()
        }

        # Note: Octave state persists! Only reset when explicit octave key is pressed
    }
}

# Add remaining notes
if ($currentLine.Count -gt 0) {
    $result.notes += ($currentLine -join ' ')
}

# Convert to JSON with correct property order (metadata first, notes last)
$orderedResult = [ordered]@{
    name       = $result.name
    artist     = $result.artist
    instrument = $result.instrument
    bpm        = $result.bpm
    notes      = $result.notes
}
$json = $orderedResult | ConvertTo-Json -Depth 10
Set-Content -Path $OutputPath -Value $json -Encoding UTF8

Write-Host "Converted: $InputPath -> $OutputPath" -ForegroundColor Green
Write-Host "Original commands: $($data.commands.Count)"
Write-Host "New notes lines: $($result.notes.Count)"
$totalNotes = ($result.notes | ForEach-Object { ($_ -split ' ').Count } | Measure-Object -Sum).Sum
Write-Host "Total notes: $totalNotes"
