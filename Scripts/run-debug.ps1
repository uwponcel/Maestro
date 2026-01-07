# Kill existing Blish HUD first (so build can overwrite .bhm)
Stop-Process -Name "Blish HUD" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Build the module
Set-Location "C:\git\Maestro"
$buildResult = & dotnet build Maestro.csproj -c Debug -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Start Blish HUD with module
Set-Location "C:\Users\uwpon\OneDrive\Documents\BlishHUD"
Start-Process "Blish HUD.exe" -ArgumentList '--debug', '--module', 'C:\git\Maestro\bin\x64\Debug\Maestro.bhm'

# Return to project directory
Set-Location "C:\git\Maestro\Scripts"
