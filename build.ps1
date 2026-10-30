$ErrorActionPreference = 'Stop'

# Locate dotnet executable
$dotnet = $null
if (Test-Path "$HOME\.dotnet\dotnet.exe") {
    $dotnet = "$HOME\.dotnet\dotnet.exe"
} elseif (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    $dotnet = "dotnet"
} elseif (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
    $dotnet = "C:\Program Files\dotnet\dotnet.exe"
} else {
    throw "dotnet SDK 8.0+ not found. Please install .NET 8 SDK."
}

Stop-Process -Name Glance, GlanceSetup, DynamicIsland, DynamicIslandSetup -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

$tempGlancePublish = Join-Path $PSScriptRoot "bin\publish-glance"
$tempSetupPublish = Join-Path $PSScriptRoot "bin\publish-setup"

Write-Host "1. Compiling & Publishing Native .NET 8 Glance.exe..." -ForegroundColor Cyan
& $dotnet publish "$PSScriptRoot\Glance.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $tempGlancePublish
Copy-Item "$tempGlancePublish\Glance.exe" "$PSScriptRoot\Glance.exe" -Force

Write-Host "2. Compiling & Publishing Standalone GlanceSetup.exe Installer..." -ForegroundColor Cyan
& $dotnet publish "$PSScriptRoot\GlanceSetup.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $tempSetupPublish
Copy-Item "$tempSetupPublish\GlanceSetup.exe" "$PSScriptRoot\GlanceSetup.exe" -Force

# Update installed copy if present
$installedGlanceDir = Join-Path $env:LOCALAPPDATA "Programs\Glance"
if (Test-Path $installedGlanceDir) {
    Copy-Item "$PSScriptRoot\Glance.exe" (Join-Path $installedGlanceDir "Glance.exe") -Force
}
$installedLegacyDir = Join-Path $env:LOCALAPPDATA "Programs\WindowsDynamicIsland"
if (Test-Path $installedLegacyDir) {
    Copy-Item "$PSScriptRoot\Glance.exe" (Join-Path $installedLegacyDir "DynamicIsland.exe") -Force
}

# Clean temporary folders
Remove-Item -Recurse -Force $tempGlancePublish, $tempSetupPublish -ErrorAction SilentlyContinue

Write-Host "Done! Glance.exe and GlanceSetup.exe compiled successfully with .NET 8." -ForegroundColor Green
