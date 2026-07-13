# ONE-TIME BOOTSTRAP ONLY - already run successfully; bhud-static/Aex.Maestro is live.
# Do NOT re-run this. It builds an orphan branch from scratch and would either get rejected
# by a non-fast-forward push (safe, just noisy) or, if "fixed" by force-pushing past that,
# destroy every song promoted onto the branch since the initial seed (community/ now evolves
# only via Scripts/promote-community-song.ps1, not by reseeding from a local folder - the
# original "community/ : copy from local Community/" step is gone for exactly this reason,
# on top of Community/ no longer existing locally at all since the packaging cleanup task).
# The guard below refuses to run if the branch already exists remotely, as a safety net.
#
# Mints a stable "id" field into any Songs/**/*.json that doesn't already have one, writing it
# back into the source file - this part would be harmless to redo, but the branch-creation
# below is not, hence the guard applies to the whole script.
$ErrorActionPreference = "Stop"

$projectRoot = "C:\git\perso\Maestro\module"
$branchName = "bhud-static/Aex.Maestro"
$scratchClone = Join-Path $env:TEMP "maestro-static-seed-clone"
$now = Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Invoke-Git {
    param([string[]]$GitArgs)
    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed (exit $LASTEXITCODE)"
    }
}

function Write-Utf8NoBom {
    param([string]$Path, [string]$Content)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

$existingRef = git ls-remote --heads "git@github.com-personal:uwponcel/Maestro.git" $branchName
if ($existingRef) {
    Write-Host "REFUSING TO RUN: '$branchName' already exists on origin." -ForegroundColor Red
    Write-Host "This script is one-time bootstrap only - see the header comment for why re-running it is unsafe." -ForegroundColor Red
    Write-Host "To promote a community song, use Scripts\promote-community-song.ps1 instead." -ForegroundColor Yellow
    exit 1
}

if (Test-Path $scratchClone) { Remove-Item -Recurse -Force $scratchClone }

Write-Host "Cloning repo into scratch directory..." -ForegroundColor Cyan
Invoke-Git @("clone", "git@github.com-personal:uwponcel/Maestro.git", $scratchClone)
Set-Location $scratchClone
Invoke-Git @("checkout", "--orphan", $branchName)
Invoke-Git @("rm", "-rf", ".")

New-Item -ItemType Directory -Path "builtin\songs" -Force | Out-Null
New-Item -ItemType Directory -Path "community\songs" -Force | Out-Null
New-Item -ItemType Directory -Path "community-pending\songs" -Force | Out-Null

# --- builtin/ : mint stable ids where missing, build manifest + per-song files ---
Write-Host "Building builtin/ namespace..." -ForegroundColor Cyan
$builtinEntries = @()
$songFiles = Get-ChildItem -Path (Join-Path $projectRoot "Songs") -Filter "*.json" -Recurse

foreach ($file in $songFiles) {
    $content = Get-Content -Path $file.FullName -Raw | ConvertFrom-Json

    if (-not $content.id) {
        $newId = [guid]::NewGuid().ToString()
        $content | Add-Member -NotePropertyName "id" -NotePropertyValue $newId -Force
        Write-Utf8NoBom -Path $file.FullName -Content ($content | ConvertTo-Json -Depth 10)
        Write-Host "  minted id for $($content.name): $newId" -ForegroundColor Gray
    }

    # Prefer the instrument already recorded in the song file; fall back to the containing
    # folder name only if the file predates that field. Folder names are for human filing
    # only (e.g. "Drums" vs the DrumSet enum) and aren't guaranteed to match Maestro's
    # InstrumentType values, and SongSerializer parses this string with a case-sensitive
    # Enum.TryParse, so a mismatch here silently reloads the song as the wrong instrument.
    $instrument = if ($content.instrument) { $content.instrument } else { $file.Directory.Name }
    $durationMs = ($content.notes | ForEach-Object {
        if ($_ -match ':(\d+)$') { [int]$matches[1] } else { 0 }
    } | Measure-Object -Sum).Sum

    $builtinEntries += [ordered]@{
        id          = $content.id
        name        = $content.name
        artist      = $content.artist
        transcriber = $content.transcriber
        instrument  = $instrument
        durationMs  = $durationMs
        createdAt   = $now
    }

    $songJson = [ordered]@{
        name            = $content.name
        artist          = $content.artist
        transcriber     = $content.transcriber
        instrument      = $instrument
        notes           = $content.notes
        skipOctaveReset = if ($content.skipOctaveReset) { $content.skipOctaveReset } else { $false }
    } | ConvertTo-Json -Depth 10

    Write-Utf8NoBom -Path (Join-Path $scratchClone "builtin\songs\$($content.id).json") -Content $songJson
}

$builtinManifestJson = @{ version = 1; lastUpdated = $now; songs = $builtinEntries } | ConvertTo-Json -Depth 10
Write-Utf8NoBom -Path (Join-Path $scratchClone "builtin\manifest.json") -Content $builtinManifestJson

Write-Host "  $($builtinEntries.Count) built-in song(s) seeded" -ForegroundColor Green

# --- community/ : empty at bootstrap - the live namespace is populated by promoting
# submissions (Scripts\promote-community-song.ps1), never by reseeding this script ---
Write-Host "Building community/ namespace (empty)..." -ForegroundColor Cyan
$communityManifestJson = @{ version = 1; lastUpdated = $now; songs = @() } | ConvertTo-Json -Depth 10
Write-Utf8NoBom -Path (Join-Path $scratchClone "community\manifest.json") -Content $communityManifestJson

# --- community-pending/ : empty seed ---
Write-Host "Building community-pending/ namespace (empty)..." -ForegroundColor Cyan
$pendingManifestJson = @{ version = 1; lastUpdated = $now; songs = @() } | ConvertTo-Json -Depth 10
Write-Utf8NoBom -Path (Join-Path $scratchClone "community-pending\manifest.json") -Content $pendingManifestJson

Invoke-Git @("add", "-A")
Invoke-Git @("commit", "-m", "Seed bhud-static/Aex.Maestro: builtin, community, community-pending")

Write-Host "Pushing $branchName..." -ForegroundColor Cyan
$pushFailed = $false
try {
    Invoke-Git @("push", "origin", $branchName)
}
catch {
    $pushFailed = $true
    Write-Host "`nPUSH FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "The commit is safe in the scratch clone - nothing was deleted." -ForegroundColor Yellow
    Write-Host "Scratch clone left at: $scratchClone" -ForegroundColor Yellow
    Write-Host "Inspect the error above, then retry manually with:" -ForegroundColor Yellow
    Write-Host "  cd `"$scratchClone`"; git push origin $branchName" -ForegroundColor Yellow
}

Set-Location $projectRoot

if ($pushFailed) {
    Write-Host "`nStopped: push did not succeed. Scratch clone preserved for retry (see above)." -ForegroundColor Red
    exit 1
}

Remove-Item -Recurse -Force $scratchClone

Write-Host "`nDone. Pushed $branchName - check https://bhm.blishhud.com/Aex.Maestro/ after the webhook deploys." -ForegroundColor Green
