$ErrorActionPreference = 'Stop'
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$fw = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpf = "$fw\WPF"
$winmd = "C:\Windows\System32\WinMetadata"

Stop-Process -Name Glance, GlanceSetup, DynamicIsland, DynamicIslandSetup -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

Write-Host "1. Compiling Native Glance.exe..." -ForegroundColor Cyan
& $csc /nologo /target:winexe /out:"$PSScriptRoot\Glance.exe" /win32icon:"$PSScriptRoot\src\app.ico" `
  /r:"$wpf\PresentationFramework.dll" `
  /r:"$wpf\PresentationCore.dll" `
  /r:"$wpf\WindowsBase.dll" `
  /r:"$fw\System.Xaml.dll" `
  /r:"$fw\System.Drawing.dll" `
  /r:"$fw\System.Windows.Forms.dll" `
  /r:"$fw\System.Web.Extensions.dll" `
  /r:"$fw\System.Management.dll" `
  /r:"$fw\System.Runtime.dll" `
  /r:"$winmd\Windows.Foundation.winmd" `
  /r:"$winmd\Windows.Media.winmd" `
  /r:"$winmd\Windows.Storage.winmd" `
  "$PSScriptRoot\src\GlanceApp.cs"

Write-Host "2. Compiling Standalone GlanceSetup.exe Installer..." -ForegroundColor Cyan
& $csc /nologo /target:winexe /out:"$PSScriptRoot\GlanceSetup.exe" `
  /win32icon:"$PSScriptRoot\src\app.ico" `
  /resource:"$PSScriptRoot\Glance.exe",Glance.exe `
  /r:"$wpf\PresentationFramework.dll" `
  /r:"$wpf\PresentationCore.dll" `
  /r:"$wpf\WindowsBase.dll" `
  /r:"$fw\System.Xaml.dll" `
  /r:"$fw\System.Drawing.dll" `
  /r:"$fw\System.Windows.Forms.dll" `
  "$PSScriptRoot\src\SetupInstaller.cs"

# Update installed copy if present
$installedGlanceDir = Join-Path $env:LOCALAPPDATA "Programs\Glance"
if (Test-Path $installedGlanceDir) {
    Copy-Item "$PSScriptRoot\Glance.exe" (Join-Path $installedGlanceDir "Glance.exe") -Force
}
$installedLegacyDir = Join-Path $env:LOCALAPPDATA "Programs\WindowsDynamicIsland"
if (Test-Path $installedLegacyDir) {
    Copy-Item "$PSScriptRoot\Glance.exe" (Join-Path $installedLegacyDir "DynamicIsland.exe") -Force
}

Write-Host "Done! Glance.exe and GlanceSetup.exe compiled successfully." -ForegroundColor Green
