#requires -Version 5.1
param(
    [string]$Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$iscc = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'

if (-not (Test-Path $iscc)) {
    $candidates = @(
        'C:\Program Files\Inno Setup 6\ISCC.exe',
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
    )
    $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $iscc) {
    throw 'Inno Setup 6 tidak dijumpai. Pasang dengan: winget install JRSoftware.InnoSetup'
}

$publish = Join-Path $root 'publish'
if (-not (Test-Path (Join-Path $publish 'SolatWaktuWidget.exe'))) {
    & (Join-Path $PSScriptRoot 'publish.ps1')
}

& $iscc "/DMyAppVersion=$Version" (Join-Path $PSScriptRoot 'SolatWaktuWidget.iss')

if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup gagal dengan kod $LASTEXITCODE."
}

Get-ChildItem (Join-Path $root 'dist') | Select-Object Name, Length
