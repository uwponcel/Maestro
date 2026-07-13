# Lists all songs currently awaiting approval in community-pending/ on bhud-static/Aex.Maestro.
# Read-only - makes no changes. Approve one with:
#   .\promote-community-song.ps1 -SongId <id>
$ErrorActionPreference = "Stop"

$repo = "uwponcel/Maestro"
$branch = "bhud-static/Aex.Maestro"

function Get-GhFileContent($path) {
    $raw = gh api "repos/$repo/contents/$path`?ref=$branch"
    if ($LASTEXITCODE -ne 0) {
        throw "gh api GET '$path' failed (exit $LASTEXITCODE)"
    }
    $result = $raw | ConvertFrom-Json
    $bytes = [Convert]::FromBase64String($result.content.Replace("`n", ""))
    return [System.Text.Encoding]::UTF8.GetString($bytes)
}

Write-Host "Fetching community-pending manifest..." -ForegroundColor Cyan
$manifest = Get-GhFileContent "community-pending/manifest.json" | ConvertFrom-Json

if (-not $manifest.songs -or $manifest.songs.Count -eq 0) {
    Write-Host "No pending submissions." -ForegroundColor Green
    exit 0
}

Write-Host "`n$($manifest.songs.Count) pending submission(s):`n" -ForegroundColor Yellow

foreach ($song in $manifest.songs) {
    Write-Host "-----------------------------------" -ForegroundColor DarkGray
    Write-Host "Name:        $($song.name)"
    Write-Host "Artist:      $($song.artist)"
    Write-Host "Transcriber: $($song.transcriber)"
    Write-Host "Instrument:  $($song.instrument)"
    Write-Host "Duration:    $([math]::Round($song.durationMs / 1000, 1))s"
    Write-Host "Submitted:   $($song.createdAt)"
    Write-Host "Id:          $($song.id)"
    Write-Host "Approve with:" -ForegroundColor Cyan
    Write-Host "  .\promote-community-song.ps1 -SongId $($song.id)" -ForegroundColor White
}
Write-Host "-----------------------------------" -ForegroundColor DarkGray
