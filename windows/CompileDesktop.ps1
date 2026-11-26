param(
  [string]$Assets=(Join-Path $env:LOCALAPPDATA "Programs\winintel-ime-windows"),
  [string]$Output=(Join-Path $env:TEMP "winintel-ime-sogou-build\winintel-ime.exe")
)
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot -Parent
if(!(Test-Path "$Assets\winintel.ico")){throw "Install the offline Windows package or specify -Assets."}
New-Item -ItemType Directory -Force (Split-Path $Output -Parent) | Out-Null
$sources=@(Get-ChildItem "$PSScriptRoot\App\*.cs" | ForEach-Object {$_.FullName})
$sources+="$root\desktop\VoiceSession.cs"
$sources+="$root\desktop\ChildJob.cs"
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /codepage:65001 "/win32icon:$Assets\winintel.ico" "/out:$Output" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll $sources
if($LASTEXITCODE){throw "Windows client build failed"}
Get-Item $Output | Select-Object FullName,Length
