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

$lines = [System.IO.File]::ReadAllLines($AhkPath)
$noteLines = @()
$currentChord = @()
$currentOctave = 0

foreach ($line in $lines) {
    $trimmed = $line.Trim()

    # Skip function wrapper lines
    if ($trimmed -match "^PlaySong\(\)" -or $trimmed -eq "{" -or $trimmed -eq "}") {
        continue
    }

    # Key down - add note to current chord
    if ($trimmed -match "SendInput\s*\{(Numpad[1-8])\s+down\}") {
        $numpad = $matches[1]
        $note = $NumpadToNote[$numpad]

        # Apply octave modifier
        if ($currentOctave -gt 0) { $note += "+" }
        elseif ($currentOctave -lt 0) { $note += "-" }

        $currentChord += $note
    }
    # Octave up
    elseif ($trimmed -match "SendInput\s*\{Numpad9\}") {
        $currentOctave++
    }
    # Octave down
    elseif ($trimmed -match "SendInput\s*\{Numpad0\}") {
        $currentOctave--
    }
    # Sleep - flush current chord with this duration
    elseif ($trimmed -match "Sleep,\s*(\d+)") {
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
