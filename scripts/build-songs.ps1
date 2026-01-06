<#
.SYNOPSIS
    Bundles all JSON song files into a single MessagePack binary file.

.DESCRIPTION
    Compiles and runs BuildSongs.cs which reads all *.json files from the Songs
    folder and serializes them into a single songs.bin file using MessagePack.

.NOTES
    Run this script before building the module to update the embedded songs bundle.
#>

param(
    [string]$SongsPath = "$PSScriptRoot\..\Songs",
    [string]$OutputPath = "$PSScriptRoot\..\Data\songs.bin"
)

$ErrorActionPreference = "Stop"

$packagesPath = "$PSScriptRoot\..\packages"
$scriptDir = $PSScriptRoot

# Reference assemblies for compilation
$references = @(
    "$packagesPath\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll",
    "$packagesPath\MessagePack.2.5.187\lib\net472\MessagePack.dll",
    "$packagesPath\MessagePack.Annotations.2.5.187\lib\netstandard2.0\MessagePack.Annotations.dll",
    "$packagesPath\System.Buffers.4.5.1\lib\net461\System.Buffers.dll",
    "$packagesPath\System.Memory.4.5.5\lib\net461\System.Memory.dll",
    "$packagesPath\System.Runtime.CompilerServices.Unsafe.6.0.0\lib\net461\System.Runtime.CompilerServices.Unsafe.dll",
    "$packagesPath\System.Collections.Immutable.6.0.0\lib\net461\System.Collections.Immutable.dll"
)

$cscPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
}
$sourcePath = "$scriptDir\BuildSongs.cs"
$exePath = "$scriptDir\BuildSongs.exe"

# Build the references argument
$refArgs = ($references | ForEach-Object { "/r:`"$_`"" }) -join " "

Write-Host "Compiling BuildSongs.cs..."
$compileCmd = "& `"$cscPath`" /nologo /out:`"$exePath`" $refArgs `"$sourcePath`""
Invoke-Expression $compileCmd

if (-not (Test-Path $exePath)) {
    Write-Error "Compilation failed"
    exit 1
}

# Copy required DLLs for runtime
Write-Host "Copying runtime dependencies..."
foreach ($ref in $references) {
    if (Test-Path $ref) {
        Copy-Item $ref -Destination $scriptDir -ErrorAction SilentlyContinue
    }
}

Write-Host "Running BuildSongs.exe..."
Push-Location $scriptDir
try {
    & $exePath $SongsPath $OutputPath
}
finally {
    Pop-Location
}

# Clean up
Write-Host "Cleaning up..."
Remove-Item $exePath -ErrorAction SilentlyContinue
foreach ($ref in $references) {
    $dllName = Split-Path $ref -Leaf
    Remove-Item "$scriptDir\$dllName" -ErrorAction SilentlyContinue
}

Write-Host "`nBuild complete!"
