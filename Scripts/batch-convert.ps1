$artistMap = @{
    "Backstreet Boys - As Long As You Love Me" = @{ Artist = "Backstreet Boys"; Song = "As Long As You Love Me" }
    "Backstreet Boys - I Want It That Way" = @{ Artist = "Backstreet Boys"; Song = "I Want It That Way" }
    "blue-bird-piano-naruto-op3-ikimono-gakari" = @{ Artist = "Naruto"; Song = "Blue Bird" }
    "earth-song-michael_V5_12" = @{ Artist = "Michael Jackson"; Song = "Earth Song" }
    "Evanescence_-_My_Immortal" = @{ Artist = "Evanescence"; Song = "My Immortal" }
    "Flashdance-(What-A-Feeling)(primeira nota)_V5_12" = @{ Artist = "Irene Cara"; Song = "What A Feeling" }
    "here-comes-a-thought-steven-universe_mtx" = @{ Artist = "Steven Universe"; Song = "Here Comes A Thought" }
    "hypnotize-system-of-a-down" = @{ Artist = "System of a Down"; Song = "Hypnotize" }
    "Just Give Me A Reason" = @{ Artist = "Pink"; Song = "Just Give Me A Reason" }
    "Michael Jackson - Earth song" = @{ Artist = "Michael Jackson"; Song = "Earth Song" }
    "My Heart Will Go On - Titanic" = @{ Artist = "Celine Dion"; Song = "My Heart Will Go On" }
    "numb_V5_12" = @{ Artist = "Linkin Park"; Song = "Numb" }
    "One Piece - Bink's Sake.mid" = @{ Artist = "One Piece"; Song = "Bink's Sake" }
    "perfect-pentatonix" = @{ Artist = "Pentatonix"; Song = "Perfect" }
    "river-flows-in-you_V5_12" = @{ Artist = "Yiruma"; Song = "River Flows In You" }
    "Sadness and Sorrow Naruto" = @{ Artist = "Naruto"; Song = "Sadness and Sorrow" }
    "Still-Loving-You1_V5_12" = @{ Artist = "Scorpions"; Song = "Still Loving You" }
    "The Beatles - Hey Jude" = @{ Artist = "The Beatles"; Song = "Hey Jude" }
    "total-eclipse-of-the-heart" = @{ Artist = "Bonnie Tyler"; Song = "Total Eclipse Of The Heart" }
    "What's The Use Of Feeling Blue1_V5_12" = @{ Artist = "Steven Universe"; Song = "What's The Use Of Feeling Blue" }
}

$ahkFolder = "C:\git\perso\Maestro\Songs\AHK"
$pianoFolder = "C:\git\perso\Maestro\Songs\Piano"
$convertMB = "C:\git\perso\Maestro\Scripts\convert-piano-to-mb.ps1"
$convertJson = "C:\git\perso\Maestro\Scripts\convert-ahk-to-compact.ps1"

Get-ChildItem "$ahkFolder\*.ahk" | ForEach-Object {
    $baseName = $_.BaseName
    Write-Host "Converting: $baseName"

    $info = $artistMap[$baseName]
    if ($null -eq $info) {
        Write-Host "  WARNING: No mapping for $baseName - skipping"
        return
    }

    $tempFile = [System.IO.Path]::GetTempFileName()
    $outputFile = Join-Path $pianoFolder "$($info.Song).json"

    # Step 1: Convert to MB format
    & $convertMB -InputPath $_.FullName -OutputPath $tempFile

    # Step 2: Convert to JSON
    & $convertJson -AhkPath $tempFile -JsonPath $outputFile -Transcriber "PianoThomas" -Artist $info.Artist -SongName $info.Song -Instrument "Piano"

    Remove-Item $tempFile
    Write-Host "  -> $outputFile"
}

Write-Host "`nDone! Converted $((Get-ChildItem "$ahkFolder\*.ahk").Count) files."
