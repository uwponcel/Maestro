# Combine all individual song JSON files into a single songs.json for ContentsManager
$songsPath = "C:\git\perso\Maestro\Songs"
$outputPath = "C:\git\perso\Maestro\ref\songs.json"

$songs = @()

$files = Get-ChildItem "$songsPath\*.json"
Write-Host "Found $($files.Count) song files"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
    $songs += $content
    Write-Host "  Added: $($content.name)"
}

$json = $songs | ConvertTo-Json -Depth 10
Set-Content -Path $outputPath -Value $json -Encoding UTF8

Write-Host ""
Write-Host "Combined $($songs.Count) songs into $outputPath"
