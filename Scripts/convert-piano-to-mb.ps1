param(
    [Parameter(Mandatory=$true)]
    [string]$InputPath,

    [Parameter(Mandatory=$true)]
    [string]$OutputPath
)

# F-key to Alt+Numpad mapping for sharps
$FKeyToAltNumpad = @{
    "F1" = "Numpad1"  # C#
    "F2" = "Numpad2"  # D#
    "F3" = "Numpad3"  # F#
    "F4" = "Numpad4"  # G#
    "F5" = "Numpad5"  # A#
}

function Convert-FKeysToAlt {
    param([string]$Line)

    $result = $Line
    foreach ($fkey in $FKeyToAltNumpad.Keys) {
        $numpad = $FKeyToAltNumpad[$fkey]

        # Replace F-key down with Alt down + Numpad down
        # {F3 down} -> {LAlt down}{Numpad3 down}
        $result = $result -replace "\{$fkey down\}", "{LAlt down}{$numpad down}"

        # Replace F-key up with Numpad up + Alt up
        # {F3 up} -> {Numpad3 up}{LAlt up}
        $result = $result -replace "\{$fkey up\}", "{$numpad up}{LAlt up}"
    }

    return $result
}

$lines = [System.IO.File]::ReadAllLines($InputPath)
$outputLines = @()

$i = 0
while ($i -lt $lines.Count) {
    $line = $lines[$i].Trim()

    # Pass through hotkey definition, empty lines, and non-command lines
    if ($line -match "^[a-zA-Z]::" -or $line -eq "" -or $line -eq "{" -or $line -eq "}" -or $line -match "^'::") {
        $outputLines += $lines[$i]
        $i++
        continue
    }

    # Check if this is a key-down line (contains "down")
    if ($line -match "SendInput.*down") {
        # Collect ALL consecutive key down lines (handles chords)
        $keyDownLines = @()
        while ($i -lt $lines.Count -and $lines[$i].Trim() -match "SendInput.*down") {
            $keyDownLines += Convert-FKeysToAlt $lines[$i]
            $i++
        }

        # Next should be short Sleep (press duration ≤150ms)
        if ($i -lt $lines.Count -and $lines[$i].Trim() -match "Sleep,\s*(\d+)" -and [int]$matches[1] -le 150) {
            $pressDuration = [int]$matches[1]  # Capture the press duration - it's part of total note time
            $i++

            # Collect ALL consecutive key up lines
            $keyUpLines = @()
            while ($i -lt $lines.Count -and $lines[$i].Trim() -match "SendInput.*up") {
                $keyUpLines += Convert-FKeysToAlt $lines[$i]
                $i++
            }

            # Next should be real duration Sleep
            if ($i -lt $lines.Count -and $lines[$i].Trim() -match "Sleep,\s*(\d+)") {
                $totalDuration = $pressDuration + [int]$matches[1]  # Press duration + gap duration = total note time
                $i++

                # Skip trailing duplicate key ups AND accumulate any additional sleeps
                while ($i -lt $lines.Count) {
                    $nextLine = $lines[$i].Trim()
                    if ($nextLine -match "^SendInput.*up\}$") {
                        # Skip duplicate key up
                        $i++
                    }
                    elseif ($nextLine -match "Sleep,\s*(\d+)") {
                        # Accumulate additional sleep duration
                        $totalDuration += [int]$matches[1]
                        $i++
                    }
                    else {
                        # Stop at anything else (next note, octave change, etc.)
                        break
                    }
                }

                # Output MB format: key downs -> total duration -> key ups
                $outputLines += $keyDownLines
                $outputLines += "Sleep, $totalDuration"
                $outputLines += $keyUpLines
                continue
            }
        }

        # Pattern didn't fully match - output collected key downs as-is
        $outputLines += $keyDownLines
        continue
    }

    # Handle octave changes (Numpad0, Numpad9) - pass through
    if ($line -match "SendInput\s*\{Numpad[09]\}") {
        $outputLines += $lines[$i]
        $i++
        continue
    }

    # Pass through any other lines
    $outputLines += $lines[$i]
    $i++
}

[System.IO.File]::WriteAllLines($OutputPath, $outputLines)
Write-Host "Converted: $InputPath -> $OutputPath"
Write-Host "Total lines: $($outputLines.Count)"
