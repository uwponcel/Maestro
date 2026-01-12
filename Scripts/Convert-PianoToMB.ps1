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
        # Convert F-keys to Alt+Numpad
        $keyDownLine = Convert-FKeysToAlt $lines[$i]

        # Look ahead for the pattern: Sleep 25 -> key up -> Sleep [real duration]
        if ($i + 3 -lt $lines.Count) {
            $sleep1 = $lines[$i + 1].Trim()
            $keyUpLine = $lines[$i + 2].Trim()
            $sleep2 = $lines[$i + 3].Trim()

            # Check if this matches Thomas's pattern
            if ($sleep1 -match "Sleep,\s*25" -and $keyUpLine -match "SendInput.*up" -and $sleep2 -match "Sleep,\s*(\d+)") {
                $realDuration = $matches[1]

                # Convert F-keys in key-up line
                $keyUpLine = Convert-FKeysToAlt $lines[$i + 2]

                # Output MB format: key down -> real duration -> key up (no trailing sleep)
                $outputLines += $keyDownLine
                $outputLines += "Sleep, $realDuration"
                $outputLines += $keyUpLine

                $i += 4  # Skip all 4 lines we processed
                continue
            }
        }

        # If pattern doesn't match, output as-is
        $outputLines += $keyDownLine
        $i++
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
