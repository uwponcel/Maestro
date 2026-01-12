param(
    [Parameter(Mandatory=$true)]
    [string]$AhkPath,

    [Parameter(Mandatory=$true)]
    [string]$JsonPath,

    [Parameter(Mandatory=$true)]
    [string]$SongName,

    [Parameter(Mandatory=$true)]
    [string]$Artist,

    [string]$Transcriber = "",

    [string]$Instrument = "Harp"
)

# Numpad to note mapping
$NumpadToNote = @{
    "Numpad1" = "C"
    "Numpad2" = "D"
    "Numpad3" = "E"
    "Numpad4" = "F"
    "Numpad5" = "G"
    "Numpad6" = "A"
    "Numpad7" = "B"
    "Numpad8" = "C^"
}

# Sharp note mapping (when Alt is held)
$NumpadToSharp = @{
    "Numpad1" = "C#"
    "Numpad2" = "D#"
    "Numpad3" = "F#"
    "Numpad4" = "G#"
    "Numpad5" = "A#"
}

$lines = [System.IO.File]::ReadAllLines($AhkPath)
$noteLines = @()
$currentChord = @()
$currentOctave = 0
$altHeld = $false

foreach ($line in $lines) {
    $trimmed = $line.Trim()

    # Skip function wrapper lines
    if ($trimmed -match "^PlaySong\(\)" -or $trimmed -eq "{" -or $trimmed -eq "}") {
        continue
    }

    # Alt key down - track state for sharps
    if ($trimmed -match "LAlt down") {
        $altHeld = $true
    }
    # Alt key up - clear state
    if ($trimmed -match "LAlt up") {
        $altHeld = $false
    }

    # Key down - add note to current chord (can have multiple keys on same line)
    $downMatches = [regex]::Matches($trimmed, "\{(Numpad[1-8])\s+down\}")
    foreach ($match in $downMatches) {
        $numpad = $match.Groups[1].Value

        # Check if this is a sharp (Alt held or Alt on same line)
        $isSharp = $altHeld -or ($trimmed -match "LAlt down")

        if ($isSharp -and $NumpadToSharp.ContainsKey($numpad)) {
            $note = $NumpadToSharp[$numpad]
        } else {
            $note = $NumpadToNote[$numpad]
        }

        # Apply octave modifier
        if ($currentOctave -gt 0) { $note += "+" }
        elseif ($currentOctave -lt 0) { $note += "-" }

        $currentChord += $note
    }

    # Octave up
    if ($trimmed -match "SendInput\s*\{Numpad9\}") {
        $currentOctave++
    }
    # Octave down
    if ($trimmed -match "SendInput\s*\{Numpad0\}") {
        $currentOctave--
    }
    # Sleep - flush current chord with this duration
    if ($trimmed -match "Sleep,\s*(\d+)") {
        $durationMs = $matches[1]
        if ($currentChord.Count -gt 0) {
            # All notes in chord get the same duration (the Sleep value)
            $chordNotation = ($currentChord | ForEach-Object { "${_}:$durationMs" }) -join " "
            $noteLines += $chordNotation
            $currentChord = @()
        }
    }
}

Write-Host "Parsed $($noteLines.Count) note groups from AHK"

# Build JSON object manually to avoid PowerShell array issues
$notesJson = ($noteLines | ForEach-Object { "`"$_`"" }) -join ",`n    "

$json = @"
{
  "name": "$SongName",
  "artist": "$Artist",
  "transcriber": "$Transcriber",
  "instrument": "$Instrument",
  "notes": [
    $notesJson
  ]
}
"@

[System.IO.File]::WriteAllText($JsonPath, $json)
Write-Host "Created: $JsonPath"
