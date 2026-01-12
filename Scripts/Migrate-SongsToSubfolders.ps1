# One-time migration script: Move song JSON files to instrument subfolders
$songsPath = "C:\git\perso\Maestro\Songs"

$files = Get-ChildItem "$songsPath\*.json"
Write-Host "Found $($files.Count) JSON files to migrate"

$moved = @{}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
    $instrument = $content.instrument

    if (-not $instrument) {
        Write-Host "  SKIP (no instrument): $($file.Name)" -ForegroundColor Yellow
        continue
    }

    $targetFolder = Join-Path $songsPath $instrument

    # Create folder if needed
    if (-not (Test-Path $targetFolder)) {
        New-Item -ItemType Directory -Path $targetFolder | Out-Null
        Write-Host "  Created folder: $instrument" -ForegroundColor Green
    }

    # Move file
    $targetPath = Join-Path $targetFolder $file.Name
    Move-Item $file.FullName $targetPath

    # Track count
    if (-not $moved[$instrument]) { $moved[$instrument] = 0 }
    $moved[$instrument]++

    Write-Host "  Moved: $($file.Name) -> $instrument/"
}

Write-Host ""
Write-Host "Migration complete:" -ForegroundColor Green
foreach ($inst in $moved.Keys | Sort-Object) {
    Write-Host "  ${inst}: $($moved[$inst]) songs"
}
