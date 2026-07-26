param(
    [Parameter(Mandatory = $true)][string]$SongId,
    [string]$Reason
)
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
    return @{ Content = [System.Text.Encoding]::UTF8.GetString($bytes); Sha = $result.sha }
}

# Returns $null when the file does not exist, instead of throwing, so the caller can tell
# "not there yet" apart from a real API failure. Any non-404 failure still throws.
function Get-GhFileContentIfExists($path) {
    $raw = gh api "repos/$repo/contents/$path`?ref=$branch" 2>$null
    if ($LASTEXITCODE -ne 0) {
        $LASTEXITCODE = 0
        return $null
    }
    $result = $raw | ConvertFrom-Json
    $bytes = [Convert]::FromBase64String($result.content.Replace("`n", ""))
    return @{ Content = [System.Text.Encoding]::UTF8.GetString($bytes); Sha = $result.sha }
}

function Set-GhFileContent($path, $content, $sha, $message) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $b64 = [Convert]::ToBase64String($bytes)
    $body = [ordered]@{ message = $message; content = $b64; branch = $branch }
    if ($sha) { $body["sha"] = $sha }
    ($body | ConvertTo-Json) | gh api "repos/$repo/contents/$path" --method PUT --input - | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "gh api PUT '$path' failed (exit $LASTEXITCODE)"
    }
}

function Remove-GhFileContent($path, $sha, $message) {
    gh api "repos/$repo/contents/$path" --method DELETE -f "message=$message" -f "sha=$sha" -f "branch=$branch" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "gh api DELETE '$path' failed (exit $LASTEXITCODE)"
    }
}

# Rejecting archives the submission to community-rejected/ rather than deleting it, so a
# mistaken reject is always recoverable. There is deliberately no community-rejected/manifest.json:
# the module never reads that namespace, it exists only as an audit trail.
#
# $step describes what is about to be attempted, and - for every step from the first write
# onward - what has already been safely committed if this exact step is the one that fails.
# There is no automatic rollback (see the catch block): each gh api call is its own atomic
# commit on $branch. The archive copy is written BEFORE the pending copy is deleted, so no
# single failed step can lose the payload; the worst case leaves the song in both namespaces.
$step = "starting"
try {
    $suffix = if ($Reason) { " ($Reason)" } else { "" }

    $step = "reading community-pending manifest (nothing written yet)"
    Write-Host "Reading community-pending manifest..." -ForegroundColor Cyan
    $pendingManifestFile = Get-GhFileContent "community-pending/manifest.json"
    $pendingManifest = $pendingManifestFile.Content | ConvertFrom-Json
    $entry = $pendingManifest.songs | Where-Object { $_.id -eq $SongId }

    if (-not $entry) {
        Write-Host "Song $SongId not found in community-pending manifest." -ForegroundColor Red
        exit 1
    }

    Write-Host "Found '$($entry.name)' by $($entry.artist). Rejecting$suffix..." -ForegroundColor Cyan

    # From here on, use $entry.id (the canonical, exact-case id from the manifest) for every
    # path and comparison - not the raw $SongId param, which matched case-insensitively above
    # and would silently 404 against GitHub's case-sensitive Contents API paths if its casing
    # differs from the manifest's.
    $canonicalId = $entry.id

    $step = "reading pending song file community-pending/songs/$canonicalId.json (nothing written yet)"
    $songFile = Get-GhFileContent "community-pending/songs/$canonicalId.json"

    $step = "checking for an existing archive at community-rejected/songs/$canonicalId.json (nothing written yet)"
    $existingArchive = Get-GhFileContentIfExists "community-rejected/songs/$canonicalId.json"

    if ($existingArchive -and $existingArchive.Content -ne $songFile.Content) {
        throw "community-rejected/songs/$canonicalId.json already exists with DIFFERENT content. Refusing to overwrite an unrelated archive - inspect it by hand and move or delete it before retrying."
    }

    if ($existingArchive) {
        # A previous run archived the song but died before finishing; the copy is byte-identical,
        # so skip the write and pick up where that run left off.
        Write-Host "Already archived (byte-identical), resuming from there..." -ForegroundColor Yellow
    }
    else {
        $step = "writing song file to community-rejected/songs/$canonicalId.json (nothing written yet - safe to just re-run)"
        Write-Host "Archiving song file to community-rejected/..." -ForegroundColor Cyan
        Set-GhFileContent "community-rejected/songs/$canonicalId.json" $songFile.Content $null "Archive rejected song: $($entry.name)$suffix"
    }

    $pendingManifest.songs = @($pendingManifest.songs | Where-Object { $_.id -ne $canonicalId })
    $pendingManifest.lastUpdated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")

    $step = "removing the entry from community-pending/manifest.json (community-rejected/songs/$canonicalId.json was archived successfully, but the song is STILL LISTED as pending and community-pending/songs/$canonicalId.json still exists, so it will keep showing up in list-pending-submissions.ps1; re-running this script is safe and will finish the job)"
    Write-Host "Updating community-pending manifest..." -ForegroundColor Cyan
    Set-GhFileContent "community-pending/manifest.json" ($pendingManifest | ConvertTo-Json -Depth 10) $pendingManifestFile.Sha "Update manifest: reject $($entry.name)$suffix"

    $step = "deleting community-pending/songs/$canonicalId.json (rejection actually succeeded: the song is archived and no longer listed as pending; only this now-orphaned pending song file is left behind, and it can be deleted by hand from the GitHub UI, or safely ignored)"
    Write-Host "Removing song file from community-pending/..." -ForegroundColor Cyan
    Remove-GhFileContent "community-pending/songs/$canonicalId.json" $songFile.Sha "Remove rejected song: $($entry.name)$suffix"

    Write-Host "Rejected '$($entry.name)'. Archived to community-rejected/songs/$canonicalId.json." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "FAILED while: $step" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "No automatic rollback was attempted. Inspect community-pending/ and community-rejected/ on branch '$branch' for song id $SongId before retrying or finishing by hand." -ForegroundColor Yellow
    exit 1
}
