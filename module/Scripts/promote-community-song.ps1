param(
    [Parameter(Mandatory = $true)][string]$SongId
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

# $step describes what is about to be attempted, and - for every step from the first write
# onward - what has already been safely committed if this exact step is the one that fails.
# There is no automatic rollback (see the catch block): each gh api call is its own atomic
# commit on $branch, so a mid-sequence failure can only ever leave two namespaces
# (community-pending/ and community/) out of sync with each other, never a half-written file.
$step = "starting"
try {
    $step = "reading community-pending manifest (nothing written yet)"
    Write-Host "Reading community-pending manifest..." -ForegroundColor Cyan
    $pendingManifestFile = Get-GhFileContent "community-pending/manifest.json"
    $pendingManifest = $pendingManifestFile.Content | ConvertFrom-Json
    $entry = $pendingManifest.songs | Where-Object { $_.id -eq $SongId }

    if (-not $entry) {
        Write-Host "Song $SongId not found in community-pending manifest." -ForegroundColor Red
        exit 1
    }

    Write-Host "Found '$($entry.name)' by $($entry.artist). Promoting..." -ForegroundColor Cyan

    # From here on, use $entry.id (the canonical, exact-case id from the manifest) for every
    # path and comparison - not the raw $SongId param, which matched case-insensitively above
    # and would silently 404 against GitHub's case-sensitive Contents API paths if its casing
    # differs from the manifest's.
    $canonicalId = $entry.id

    $step = "reading pending song file community-pending/songs/$canonicalId.json (nothing written yet)"
    $songFile = Get-GhFileContent "community-pending/songs/$canonicalId.json"

    $step = "reading community manifest (nothing written yet)"
    $approvedManifestFile = Get-GhFileContent "community/manifest.json"
    $approvedManifest = $approvedManifestFile.Content | ConvertFrom-Json
    $approvedManifest.songs = @($approvedManifest.songs) + $entry
    $approvedManifest.lastUpdated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")

    $step = "writing song file to community/songs/$canonicalId.json (nothing written yet - safe to just re-run)"
    Write-Host "Writing song file to community/..." -ForegroundColor Cyan
    Set-GhFileContent "community/songs/$canonicalId.json" $songFile.Content $null "Promote song: $($entry.name)"

    $step = "writing updated manifest to community/manifest.json (community/songs/$canonicalId.json was written, but is NOT yet listed in community/manifest.json, so the song will not appear in-game until the manifest is fixed; re-running this script will now fail at the previous step because that song file already exists, so finish this by hand: add the entry to community/manifest.json yourself, or delete community/songs/$canonicalId.json first and then re-run)"
    Write-Host "Updating community manifest..." -ForegroundColor Cyan
    Set-GhFileContent "community/manifest.json" ($approvedManifest | ConvertTo-Json -Depth 10) $approvedManifestFile.Sha "Update manifest: promote $($entry.name)"

    $pendingManifest.songs = @($pendingManifest.songs | Where-Object { $_.id -ne $canonicalId })
    $pendingManifest.lastUpdated = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss.fffZ")

    $step = "removing the entry from community-pending/manifest.json (the song is ALREADY LIVE: community/songs/$canonicalId.json and community/manifest.json were both written successfully; only community-pending/manifest.json still lists it as pending, and community-pending/songs/$canonicalId.json still exists too - fix community-pending/manifest.json by hand to drop the entry)"
    Write-Host "Updating community-pending manifest..." -ForegroundColor Cyan
    Set-GhFileContent "community-pending/manifest.json" ($pendingManifest | ConvertTo-Json -Depth 10) $pendingManifestFile.Sha "Update manifest: remove promoted $($entry.name)"

    $step = "deleting community-pending/songs/$canonicalId.json (promotion actually succeeded: community/ and both manifests are correct; only this now-orphaned pending song file is left behind, and it can be deleted by hand from the GitHub UI, or safely ignored)"
    Write-Host "Removing song file from community-pending/..." -ForegroundColor Cyan
    Remove-GhFileContent "community-pending/songs/$canonicalId.json" $songFile.Sha "Remove promoted song: $($entry.name)"

    Write-Host "Promoted '$($entry.name)' to community/." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "FAILED while: $step" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host "No automatic rollback was attempted. Inspect community/ and community-pending/ on branch '$branch' for song id $SongId before retrying or finishing by hand." -ForegroundColor Yellow
    exit 1
}
