# Convert all Legacy songs to Compact format
$songsPath = "C:\git\Maestro\Songs"
$converterPath = "C:\git\Maestro\Scripts\Convert-Song.ps1"

$files = Get-ChildItem "$songsPath\*.json"
$converted = 0
$skipped = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw | ConvertFrom-Json

    if ($content.commands) {
        Write-Host "Converting: $($file.Name)"
        & $converterPath -InputPath $file.FullName -OutputPath $file.FullName
        $converted++
    } else {
        Write-Host "Skipping (already Compact): $($file.Name)"
        $skipped++
    }
}

Write-Host ""
Write-Host "Done! Converted: $converted, Skipped: $skipped"
