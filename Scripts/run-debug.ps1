# Kill existing Blish HUD first (so build can overwrite .bhm)
Stop-Process -Name "Blish HUD" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Build the module
Set-Location "C:\git\perso\Maestro"
$buildResult = & dotnet build Maestro.csproj -c Debug -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Start Blish HUD with module
Set-Location "C:\Users\uwpon\OneDrive\Documents\BlishHUD"
$process = Start-Process "Blish HUD.exe" -ArgumentList '--debug', '--module', 'C:\git\perso\Maestro\bin\x64\Debug\Maestro.bhm' -PassThru

# Wait a bit for module to load and check for errors
Start-Sleep -Seconds 3

$logDir = "$env:USERPROFILE\OneDrive\Documents\Guild Wars 2\addons\blishhud\logs"
$latestLog = Get-ChildItem $logDir -Filter "blishhud.*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestLog) {
    $errors = Select-String -Path $latestLog.FullName -Pattern "ERROR|FATAL|Exception" -Context 0,5
    if ($errors) {
        Write-Host "`n=== ERRORS FOUND IN LOG ===" -ForegroundColor Red
        $errors | ForEach-Object {
            Write-Host $_.Line -ForegroundColor Yellow
            $_.Context.PostContext | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
        Write-Host "==========================`n" -ForegroundColor Red
        Write-Host "Full log: $($latestLog.FullName)" -ForegroundColor Cyan
    } else {
        Write-Host "No errors detected in log." -ForegroundColor Green
    }
}

# Return to project directory
Set-Location "C:\git\perso\Maestro\Scripts"
