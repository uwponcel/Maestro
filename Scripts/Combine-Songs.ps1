# Combine all individual song JSON files into a single songs.json for ContentsManager
# Songs are organized in instrument subfolders: Songs/Harp/, Songs/Lute/, etc.
$songsPath = "C:\git\perso\Maestro\Songs"
$outputPath = "C:\git\perso\Maestro\ref\songs.json"

$songs = @()

# Get JSON files from instrument subfolders (not root, not AHK folder)
$instrumentFolders = @("Harp", "Lute", "Bass", "Piano")
foreach ($folder in $instrumentFolders) {
    $folderPath = Join-Path $songsPath $folder
    if (Test-Path $folderPath) {
        $files = Get-ChildItem "$folderPath\*.json"
        Write-Host "${folder}: $($files.Count) songs"
        foreach ($file in $files) {
            $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
            $songs += $content
        }
    }
}

$json = $songs | ConvertTo-Json -Depth 10
Set-Content -Path $outputPath -Value $json -Encoding UTF8

Write-Host ""
Write-Host "Combined $($songs.Count) songs into $outputPath"
