param(
    [string]$Version = "",
    [string]$Notes = ""
)

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

# 0. Update version.json for GitHub Auto-Update before compiling so the embedded manifest matches
$versionFilePath = Join-Path $PSScriptRoot "version.json"
$currentVer = "1.1.0"
$currentNotes = "Pembaruan fitur dan peningkatan performa Glance."

if (Test-Path $versionFilePath) {
    try {
        $existingJson = Get-Content $versionFilePath -Raw | ConvertFrom-Json
        if ($existingJson.version) {
            $parts = $existingJson.version.Split('.')
            if ($parts.Length -eq 3 -and [string]::IsNullOrWhiteSpace($Version)) {
                $patch = [int]$parts[2] + 1
                $currentVer = "$($parts[0]).$($parts[1]).$patch"
            } else {
                $currentVer = $existingJson.version
            }
        }
        if ($existingJson.releaseNotes) {
            $currentNotes = $existingJson.releaseNotes
        }
    } catch { }
}

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $currentVer = $Version.TrimStart('v', 'V')
}
if (-not [string]::IsNullOrWhiteSpace($Notes)) {
    $currentNotes = $Notes
}

$buildNum = [int64](Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
$pubDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")

$manifestObj = [ordered]@{
    version      = $currentVer
    buildNumber  = $buildNum
    publishedAt  = $pubDate
    downloadUrl  = "https://raw.githubusercontent.com/Fyxis/Glance/main/Glance.exe"
    releaseNotes = $currentNotes
}
$manifestObj | ConvertTo-Json -Depth 4 | Set-Content -Path $versionFilePath -Encoding UTF8
Write-Host "0. Updated version.json -> v$currentVer (Build $buildNum)" -ForegroundColor Yellow

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

Write-Host "Done! Glance.exe (v$currentVer) and GlanceSetup.exe compiled successfully with .NET 8." -ForegroundColor Green
