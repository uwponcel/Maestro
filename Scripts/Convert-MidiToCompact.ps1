<#
.SYNOPSIS
    Converts a MIDI file to Maestro compact song format.

.DESCRIPTION
    Parses a MIDI file and extracts the melody, converting it to the compact
    note format used by Maestro. Since GW2 instruments are monophonic (one note
    at a time), this script prioritizes the highest note when chords occur.

.PARAMETER MidiPath
    Path to the input MIDI file.

.PARAMETER OutputPath
    Path for the output JSON file. If not specified, outputs to console.

.PARAMETER Name
    Song name. Defaults to filename without extension.

.PARAMETER Artist
    Artist name. Defaults to "Unknown".

.PARAMETER Instrument
    Target instrument (Harp, Piano, Lute, Bass). Defaults to "Harp".

.PARAMETER Bpm
    Override BPM. If not specified, uses MIDI tempo.

.EXAMPLE
    .\Convert-MidiToCompact.ps1 -MidiPath "song.mid" -OutputPath "song.json" -Artist "Artist Name"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$MidiPath,

    [string]$OutputPath,
    [string]$Name,
    [string]$Artist = "Unknown",
    [string]$Instrument = "Harp",
    [int]$Bpm = 0,
    [int]$SegmentSize = 16,
    [switch]$SingleOctave
)

# MIDI note number to note name mapping
# Middle C (C4) = 60
$NoteNames = @('C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B')

# GW2 playable range (roughly 3 octaves centered around middle)
# We'll map to: C3-B3 (low/-), C4-B4 (middle/no modifier), C5-B5 (high/+)
$MiddleOctave = 4  # MIDI octave 4 = middle octave in GW2

function Read-VariableLengthQuantity {
    param([System.IO.BinaryReader]$Reader)

    $value = 0
    $byte = 0
    do {
        $byte = $Reader.ReadByte()
        $value = ($value -shl 7) -bor ($byte -band 0x7F)
    } while (($byte -band 0x80) -ne 0)

    return $value
}

function Read-MidiFile {
    param([string]$Path)

    $stream = [System.IO.File]::OpenRead($Path)
    $reader = New-Object System.IO.BinaryReader($stream)

    try {
        # Read header chunk
        $headerChunk = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        if ($headerChunk -ne "MThd") {
            throw "Invalid MIDI file: missing MThd header"
        }

        # Header length (big-endian)
        $headerLength = [System.Net.IPAddress]::NetworkToHostOrder($reader.ReadInt32())

        # Format, track count, time division (big-endian 16-bit)
        $format = [System.Net.IPAddress]::NetworkToHostOrder($reader.ReadInt16())
        $trackCount = [System.Net.IPAddress]::NetworkToHostOrder($reader.ReadInt16())
        $timeDivision = [System.Net.IPAddress]::NetworkToHostOrder($reader.ReadInt16())

        Write-Host "MIDI Format: $format, Tracks: $trackCount, TimeDivision: $timeDivision"

        $allNotes = @()
        $tempo = 500000  # Default: 120 BPM (microseconds per quarter note)

        # Read track chunks
        for ($t = 0; $t -lt $trackCount; $t++) {
            $trackChunk = [System.Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
            if ($trackChunk -ne "MTrk") {
                Write-Warning "Expected MTrk, got $trackChunk"
                continue
            }

            $trackLength = [System.Net.IPAddress]::NetworkToHostOrder($reader.ReadInt32())
            $trackEnd = $stream.Position + $trackLength

            $absoluteTime = 0
            $runningStatus = 0
            $trackNotes = @()
            $activeNotes = @{}  # Track note-on times for duration calculation

            while ($stream.Position -lt $trackEnd) {
                $deltaTime = Read-VariableLengthQuantity -Reader $reader
                $absoluteTime += $deltaTime

                $statusByte = $reader.ReadByte()

                # Handle running status
                if (($statusByte -band 0x80) -eq 0) {
                    # This is a data byte, use running status
                    $stream.Position--
                    $statusByte = $runningStatus
                } else {
                    $runningStatus = $statusByte
                }

                $eventType = $statusByte -band 0xF0
                $channel = $statusByte -band 0x0F

                switch ($eventType) {
                    0x80 {  # Note Off
                        $note = $reader.ReadByte()
                        $velocity = $reader.ReadByte()

                        if ($activeNotes.ContainsKey($note)) {
                            $startTime = $activeNotes[$note]
                            $duration = $absoluteTime - $startTime
                            $trackNotes += [PSCustomObject]@{
                                Note = $note
                                StartTime = $startTime
                                Duration = $duration
                                Velocity = $velocity
                            }
                            $activeNotes.Remove($note)
                        }
                    }
                    0x90 {  # Note On
                        $note = $reader.ReadByte()
                        $velocity = $reader.ReadByte()

                        if ($velocity -eq 0) {
                            # Note On with velocity 0 = Note Off
                            if ($activeNotes.ContainsKey($note)) {
                                $startTime = $activeNotes[$note]
                                $duration = $absoluteTime - $startTime
                                $trackNotes += [PSCustomObject]@{
                                    Note = $note
                                    StartTime = $startTime
                                    Duration = $duration
                                    Velocity = 64
                                }
                                $activeNotes.Remove($note)
                            }
                        } else {
                            $activeNotes[$note] = $absoluteTime
                        }
                    }
                    0xA0 {  # Polyphonic Key Pressure
                        $reader.ReadBytes(2) | Out-Null
                    }
                    0xB0 {  # Control Change
                        $reader.ReadBytes(2) | Out-Null
                    }
                    0xC0 {  # Program Change
                        $reader.ReadByte() | Out-Null
                    }
                    0xD0 {  # Channel Pressure
                        $reader.ReadByte() | Out-Null
                    }
                    0xE0 {  # Pitch Bend
                        $reader.ReadBytes(2) | Out-Null
                    }
                    0xF0 {  # System messages
                        if ($statusByte -eq 0xFF) {
                            # Meta event
                            $metaType = $reader.ReadByte()
                            $metaLength = Read-VariableLengthQuantity -Reader $reader

                            if ($metaType -eq 0x51) {
                                # Tempo - always 3 bytes
                                $tempoBytes = $reader.ReadBytes(3)
                                $tempo = ([int]$tempoBytes[0] -shl 16) -bor ([int]$tempoBytes[1] -shl 8) -bor [int]$tempoBytes[2]
                                Write-Host "Tempo: $tempo microseconds per quarter note ($([Math]::Round(60000000 / $tempo)) BPM)"
                            } elseif ($metaLength -gt 0) {
                                $reader.ReadBytes($metaLength) | Out-Null
                            }
                        } elseif ($statusByte -eq 0xF0 -or $statusByte -eq 0xF7) {
                            # SysEx
                            $sysexLength = Read-VariableLengthQuantity -Reader $reader
                            $reader.ReadBytes($sysexLength) | Out-Null
                        }
                    }
                }
            }

            $allNotes += $trackNotes
        }

        return @{
            Notes = $allNotes
            TimeDivision = $timeDivision
            Tempo = $tempo
        }
    }
    finally {
        $reader.Close()
        $stream.Close()
    }
}

function Convert-MidiNoteToCompact {
    param(
        [int]$MidiNote,
        [int]$MiddleOctave = 4,
        [bool]$ForceSingleOctave = $false
    )

    $noteIndex = $MidiNote % 12
    $noteName = $NoteNames[$noteIndex]

    # Single octave mode - no modifiers, all notes in middle octave
    if ($ForceSingleOctave) {
        return $noteName
    }

    $octave = [Math]::Floor($MidiNote / 12) - 1  # MIDI octave

    # Determine octave modifier relative to middle
    $octaveDiff = $octave - $MiddleOctave

    # Clamp to GW2 range (-1 to +1)
    if ($octaveDiff -lt -1) { $octaveDiff = -1 }
    if ($octaveDiff -gt 1) { $octaveDiff = 1 }

    $modifier = ""
    if ($octaveDiff -eq -1) { $modifier = "-" }
    elseif ($octaveDiff -eq 1) { $modifier = "+" }

    # Handle high C (C from next octave) - use C^ notation
    if ($noteName -eq "C" -and $octaveDiff -eq 1) {
        return "C^"
    }

    return "$noteName$modifier"
}

function Get-ClosestDuration {
    param(
        [int]$Ticks,
        [int]$TicksPerQuarter
    )

    # Standard note durations (in quarter notes)
    $durations = @(
        @{ Value = 1; Quarters = 4.0 },    # Whole
        @{ Value = 2; Quarters = 2.0 },    # Half
        @{ Value = 4; Quarters = 1.0 },    # Quarter
        @{ Value = 8; Quarters = 0.5 },    # Eighth
        @{ Value = 16; Quarters = 0.25 },  # Sixteenth
        @{ Value = 32; Quarters = 0.125 }  # Thirty-second
    )

    $quarterNotes = $Ticks / $TicksPerQuarter

    $closest = $durations[2]  # Default to quarter
    $minDiff = [double]::MaxValue
    $isDotted = $false

    foreach ($dur in $durations) {
        # Regular duration
        $diff = [Math]::Abs($quarterNotes - $dur.Quarters)
        if ($diff -lt $minDiff) {
            $minDiff = $diff
            $closest = $dur
            $isDotted = $false
        }

        # Dotted duration (1.5x)
        $dottedQuarters = $dur.Quarters * 1.5
        $diff = [Math]::Abs($quarterNotes - $dottedQuarters)
        if ($diff -lt $minDiff) {
            $minDiff = $diff
            $closest = $dur
            $isDotted = $true
        }
    }

    $durationStr = $closest.Value.ToString()
    if ($isDotted) { $durationStr += "." }

    return $durationStr
}

function Get-OptimalOctaveForSegment {
    param(
        [array]$Notes,
        [int]$GlobalMiddle,
        [int]$CurrentOctave,
        [double]$StickinessBonus = 1.3
    )

    # GW2 octave options relative to global middle: -1, 0, +1
    $octaveOptions = @(
        @{ Octave = $GlobalMiddle - 1; Modifier = -1 },  # Low
        @{ Octave = $GlobalMiddle;     Modifier = 0  },  # Middle
        @{ Octave = $GlobalMiddle + 1; Modifier = 1  }   # High
    )

    $bestOctave = $GlobalMiddle
    $bestScore = -1

    foreach ($option in $octaveOptions) {
        $targetOctave = $option.Octave
        $score = 0

        foreach ($note in $Notes) {
            $noteOctave = [int][Math]::Floor($note.Note / 12) - 1
            $noteIndex = $note.Note % 12

            # Check if note fits in this octave's range
            # GW2 range per octave: C to B (12 semitones) + C^ (high C)
            if ($noteOctave -eq $targetOctave) {
                # Perfect fit - note is in this exact octave
                $score += 2
            }
            elseif ($noteOctave -eq $targetOctave + 1 -and $noteIndex -eq 0) {
                # High C (C^) - can be played without switching using NumPad8
                $score += 1.5
            }
            elseif ([Math]::Abs($noteOctave - $targetOctave) -eq 1) {
                # Adjacent octave - will need modifier but still playable
                $score += 0.5
            }
            # Notes 2+ octaves away get 0 points (will be clamped)
        }

        # Apply stickiness bonus to current octave to prevent ping-ponging
        if ($CurrentOctave -ne $null -and $targetOctave -eq $CurrentOctave) {
            $score *= $StickinessBonus
        }

        if ($score -gt $bestScore) {
            $bestScore = $score
            $bestOctave = $targetOctave
        }
    }

    return $bestOctave
}

function Convert-NoteForSegment {
    param(
        [int]$MidiNote,
        [int]$SegmentOctave,
        [int]$GlobalMiddle
    )

    $noteIndex = $MidiNote % 12
    $noteName = $NoteNames[$noteIndex]
    $noteOctave = [int][Math]::Floor($MidiNote / 12) - 1

    $noteDiff = $noteOctave - $SegmentOctave

    # C^ special case: C one octave above segment (free, no switch)
    if ($noteIndex -eq 0 -and $noteDiff -eq 1) {
        return "C^"
    }

    # Note is in segment octave - use segment modifier
    if ($noteDiff -eq 0) {
        $segmentDiff = $SegmentOctave - $GlobalMiddle
        $modifier = ""
        if ($segmentDiff -eq 1) { $modifier = "+" }
        elseif ($segmentDiff -eq -1) { $modifier = "-" }
        return "$noteName$modifier"
    }

    # Note is in ADJACENT octave - use its REAL modifier (allows switch)
    if ([Math]::Abs($noteDiff) -eq 1) {
        $realOctaveDiff = $noteOctave - $GlobalMiddle
        # Clamp to GW2 range (-1, 0, +1)
        if ($realOctaveDiff -lt -1) { $realOctaveDiff = -1 }
        if ($realOctaveDiff -gt 1) { $realOctaveDiff = 1 }

        $modifier = ""
        if ($realOctaveDiff -eq 1) { $modifier = "+" }
        elseif ($realOctaveDiff -eq -1) { $modifier = "-" }
        return "$noteName$modifier"
    }

    # Note is 2+ octaves away - clamp to segment (can't help it)
    $segmentDiff = $SegmentOctave - $GlobalMiddle
    $modifier = ""
    if ($segmentDiff -eq 1) { $modifier = "+" }
    elseif ($segmentDiff -eq -1) { $modifier = "-" }
    return "$noteName$modifier"
}

# Main conversion logic
Write-Host "Reading MIDI file: $MidiPath"
$midiData = Read-MidiFile -Path $MidiPath

if ($midiData.Notes.Count -eq 0) {
    Write-Error "No notes found in MIDI file"
    exit 1
}

Write-Host "Found $($midiData.Notes.Count) notes"

# Sort notes by start time
$sortedNotes = $midiData.Notes | Sort-Object StartTime

# Group notes that start at the same time (chords) and pick highest note (melody)
$groupedNotes = @()
$currentGroup = @()
$currentTime = -1

foreach ($note in $sortedNotes) {
    if ($note.StartTime -ne $currentTime) {
        if ($currentGroup.Count -gt 0) {
            # Pick the highest note from the chord (usually melody)
            $melodyNote = $currentGroup | Sort-Object Note -Descending | Select-Object -First 1
            $groupedNotes += $melodyNote
        }
        $currentGroup = @($note)
        $currentTime = $note.StartTime
    } else {
        $currentGroup += $note
    }
}
# Don't forget the last group
if ($currentGroup.Count -gt 0) {
    $melodyNote = $currentGroup | Sort-Object Note -Descending | Select-Object -First 1
    $groupedNotes += $melodyNote
}

Write-Host "Extracted $($groupedNotes.Count) melody notes"

# Analyze note distribution to find optimal middle octave
$octaveCounts = @{}
foreach ($note in $groupedNotes) {
    $octave = [int][Math]::Floor($note.Note / 12) - 1
    if (-not $octaveCounts.ContainsKey($octave)) {
        $octaveCounts[$octave] = 0
    }
    $octaveCounts[$octave]++
}

Write-Host "Note distribution by octave:"
$sortedOctaves = $octaveCounts.Keys | Sort-Object
foreach ($oct in $sortedOctaves) {
    Write-Host "  Octave $oct`: $($octaveCounts[$oct]) notes"
}

# Find the octave with most notes - that becomes our middle octave
$maxCount = 0
$optimalMiddle = 4
foreach ($oct in $octaveCounts.Keys) {
    if ($octaveCounts[$oct] -gt $maxCount) {
        $maxCount = $octaveCounts[$oct]
        $optimalMiddle = $oct
    }
}

# Count octave switches with this middle octave
$currentSwitches = 0
$prevOctaveDiff = $null
foreach ($note in $groupedNotes) {
    $octave = [int][Math]::Floor($note.Note / 12) - 1
    $diff = $octave - $optimalMiddle
    if ($diff -lt -1) { $diff = -1 }
    if ($diff -gt 1) { $diff = 1 }
    if ($prevOctaveDiff -ne $null -and $diff -ne $prevOctaveDiff) {
        $currentSwitches++
    }
    $prevOctaveDiff = $diff
}

Write-Host "Optimal middle octave: $optimalMiddle (MIDI octave numbering)"
Write-Host "Estimated octave switches (naive): $currentSwitches"

$GlobalMiddle = $optimalMiddle

# Calculate BPM
$calculatedBpm = [Math]::Round(60000000 / $midiData.Tempo)
if ($Bpm -eq 0) {
    $Bpm = $calculatedBpm
}
Write-Host "Using BPM: $Bpm"

# Convert to compact format using segment-based octave optimization
$compactLines = @()
$currentLine = @()
$notesPerLine = 8

if ($SingleOctave) {
    Write-Host "Single octave mode: All notes in middle octave (no switches)"
    # Simple single-octave conversion
    foreach ($note in $groupedNotes) {
        $noteName = $NoteNames[$note.Note % 12]
        $duration = Get-ClosestDuration -Ticks $note.Duration -TicksPerQuarter $midiData.TimeDivision
        $compactNote = "$noteName`:$duration"
        $currentLine += $compactNote

        if ($currentLine.Count -ge $notesPerLine) {
            $compactLines += ($currentLine -join " ")
            $currentLine = @()
        }
    }
} else {
    Write-Host "Segment-based optimization: SegmentSize=$SegmentSize notes"

    # Divide notes into segments and optimize octave for each segment
    $totalNotes = $groupedNotes.Count
    $segmentCount = [Math]::Ceiling($totalNotes / $SegmentSize)
    $currentSegmentOctave = $GlobalMiddle
    $actualSwitches = 0
    $prevSegmentOctave = $null

    for ($segIdx = 0; $segIdx -lt $segmentCount; $segIdx++) {
        $startIdx = $segIdx * $SegmentSize
        $endIdx = [Math]::Min($startIdx + $SegmentSize, $totalNotes)
        $segmentNotes = $groupedNotes[$startIdx..($endIdx - 1)]

        # Find optimal octave for this segment
        $segmentOctave = Get-OptimalOctaveForSegment -Notes $segmentNotes -GlobalMiddle $GlobalMiddle -CurrentOctave $currentSegmentOctave

        # Count switches
        if ($prevSegmentOctave -ne $null -and $segmentOctave -ne $prevSegmentOctave) {
            $actualSwitches++
        }
        $prevSegmentOctave = $segmentOctave
        $currentSegmentOctave = $segmentOctave

        # Convert all notes in this segment using consistent modifiers for segment octave
        foreach ($note in $segmentNotes) {
            $noteName = Convert-NoteForSegment -MidiNote $note.Note -SegmentOctave $segmentOctave -GlobalMiddle $GlobalMiddle
            $duration = Get-ClosestDuration -Ticks $note.Duration -TicksPerQuarter $midiData.TimeDivision
            $compactNote = "$noteName`:$duration"
            $currentLine += $compactNote

            if ($currentLine.Count -ge $notesPerLine) {
                $compactLines += ($currentLine -join " ")
                $currentLine = @()
            }
        }
    }

    Write-Host "Segment optimization: $segmentCount segments, $actualSwitches octave switches"
    Write-Host "Improvement: $currentSwitches -> $actualSwitches switches ($([Math]::Round((1 - $actualSwitches / [Math]::Max(1, $currentSwitches)) * 100))% reduction)"
}

# Add remaining notes
if ($currentLine.Count -gt 0) {
    $compactLines += ($currentLine -join " ")
}

Write-Host "Generated $($compactLines.Count) lines of compact notation"

# Create JSON output
if (-not $Name) {
    $Name = [System.IO.Path]::GetFileNameWithoutExtension($MidiPath)
}

$song = @{
    name = $Name
    artist = $Artist
    instrument = $Instrument
    bpm = $Bpm
    notes = $compactLines
}

$json = $song | ConvertTo-Json -Depth 10

# Default output to Songs/ConvertedMidis folder
if (-not $OutputPath) {
    $scriptDir = Split-Path -Parent $PSScriptRoot
    $outputDir = Join-Path $scriptDir "Songs\ConvertedMidis"

    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        Write-Host "Created output directory: $outputDir"
    }

    $safeName = $Name -replace '[\\/:*?"<>|]', '_'
    $OutputPath = Join-Path $outputDir "$safeName.json"
}

$json | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Host "Saved to: $OutputPath"
