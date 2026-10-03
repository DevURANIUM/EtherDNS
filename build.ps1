# Builds EtherDNS and packages it into a Windows installer.
#   .\build.ps1                 -> publish + dist\EtherDNS-Setup-<version>.exe
#   .\build.ps1 -SkipInstaller  -> publish only (output in .\publish)
param([switch]$SkipInstaller)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$version = '2.1.0'
$publish = Join-Path $root 'publish'

Write-Host "==> Publishing EtherDNS $version (self-contained, win-x64)" -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish (Join-Path $root 'EtherDNS.csproj') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -p:DebugType=none -p:SatelliteResourceLanguages=en -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

if ($SkipInstaller) { Write-Host "Done: $publish" -ForegroundColor Green; return }

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw "Inno Setup 6 was not found. Install it with:  winget install JRSoftware.InnoSetup"
}

$installerDir = Join-Path $root 'installer'
if (-not (Test-Path (Join-Path $installerDir 'wizard-large.bmp'))) {
    Write-Host '==> Generating installer artwork' -ForegroundColor Cyan
    & (Join-Path $installerDir 'make-wizard-images.ps1')
}

Write-Host '==> Building installer' -ForegroundColor Cyan
& $iscc "/DAppVersion=$version" (Join-Path $installerDir 'EtherDNS.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compile failed' }

Write-Host "Done: $(Join-Path $root "dist\EtherDNS-Setup-$version.exe")" -ForegroundColor Green
