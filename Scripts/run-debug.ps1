param(
    [switch]$LocalBlishHUD
)

# Kill existing Blish HUD first (so build can overwrite .bhm)
Stop-Process -Name "Blish HUD" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Build local Blish HUD if using local version
if ($LocalBlishHUD) {
    Write-Host "Building local Blish HUD..." -ForegroundColor Cyan
    Set-Location "C:\git\perso\Blish-HUD"
    $buildResult = & dotnet build "Blish HUD.sln" -c Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Blish HUD build failed!" -ForegroundColor Red
        exit 1
    }
}

# Build the module
Write-Host "Building Maestro..." -ForegroundColor Cyan
Set-Location "C:\git\perso\Maestro"
$buildResult = & dotnet build Maestro.csproj -c Debug -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Start Blish HUD with module
if ($LocalBlishHUD) {
    Write-Host "Starting local Blish HUD..." -ForegroundColor Green
    $blishPath = "C:\git\perso\Blish-HUD\Blish HUD\bin\x64\Debug\net472"
    Set-Location $blishPath
    Start-Process "Blish HUD.exe" -ArgumentList '--debug', '--module', 'C:\git\perso\Maestro\bin\x64\Debug\Maestro.bhm'
} else {
    Write-Host "Starting installed Blish HUD..." -ForegroundColor Green
    Set-Location "C:\Users\uwpon\OneDrive\Documents\BlishHUD"
    Start-Process "Blish HUD.exe" -ArgumentList '--debug', '--module', 'C:\git\perso\Maestro\bin\x64\Debug\Maestro.bhm'
}

# Return to project directory
Set-Location "C:\git\perso\Maestro\Scripts"
