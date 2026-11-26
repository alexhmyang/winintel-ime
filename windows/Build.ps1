param(
  [string]$BuildRoot=(Join-Path $env:TEMP "winintel-ime-current-build"),
  [string]$AssetRoot=(Join-Path $env:LOCALAPPDATA "Programs\winintel-ime-windows"),
  [string]$Output=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [string]$Nsis="$env:TEMP\winintel-ime-build\nsis\Bin\makensis.exe",
  [switch]$SkipInstaller,
  [switch]$ReuseAssets
)
& "$PSScriptRoot\BuildLocalOnly.ps1" @PSBoundParameters
