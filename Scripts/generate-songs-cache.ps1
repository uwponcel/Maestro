# Generate songs.json from individual song files
# This combines all songs from the Songs directory into a single JSON array
# that gets packaged into the .bhm for production use

$projectRoot = "C:\git\perso\Maestro"
$songsDir = Join-Path $projectRoot "Songs"
$outputFile = Join-Path $projectRoot "ref\songs.json"

Write-Host "Generating songs cache..." -ForegroundColor Cyan
Write-Host "Source: $songsDir"
Write-Host "Output: $outputFile"

# Find all .json files recursively
$songFiles = Get-ChildItem -Path $songsDir -Filter "*.json" -Recurse

if ($songFiles.Count -eq 0) {
    Write-Host "No song files found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($songFiles.Count) song files" -ForegroundColor Green

# Load and combine all songs
$songs = @()
foreach ($file in $songFiles) {
    try {
        $content = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json

        # Determine instrument from parent folder name
        $instrument = $file.Directory.Name
        if ($instrument -notin @("Piano", "Harp", "Lute", "Bass")) {
            $instrument = "Piano"  # Default
        }

        # Add instrument if not already set
        if (-not $content.instrument) {
            $content | Add-Member -NotePropertyName "instrument" -NotePropertyValue $instrument -Force
        }

        $songs += $content
        Write-Host "  + $($content.name) by $($content.artist) [$instrument]" -ForegroundColor Gray
    }
    catch {
        Write-Host "  ! Failed to load: $($file.Name) - $_" -ForegroundColor Yellow
    }
}

# Sort by name
$songs = $songs | Sort-Object -Property name

# Ensure ref directory exists
$refDir = Join-Path $projectRoot "ref"
if (-not (Test-Path $refDir)) {
    New-Item -ItemType Directory -Path $refDir | Out-Null
}

# Write combined JSON
$json = $songs | ConvertTo-Json -Depth 10
$json | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "`nGenerated songs.json with $($songs.Count) songs" -ForegroundColor Green
Write-Host "File size: $([math]::Round((Get-Item $outputFile).Length / 1KB, 2)) KB"
