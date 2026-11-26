param(
  [string]$BuildRoot=(Join-Path $env:TEMP "winintel-ime-current-build"),
  [string]$AssetRoot=(Join-Path $env:LOCALAPPDATA "Programs\winintel-ime-windows"),
  [string]$Output=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [string]$Nsis="$env:TEMP\winintel-ime-build\nsis\Bin\makensis.exe",
  [switch]$SkipInstaller,
  [switch]$ReuseAssets
)
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot -Parent
$payload=Join-Path $BuildRoot "payload"
New-Item -ItemType Directory -Force $BuildRoot,$payload | Out-Null
# Reuse the verified offline models and workers; replace only the native client.
if(!$ReuseAssets){
  if(!(Test-Path "$AssetRoot\dictionary.bin")){throw "Offline assets were not found. Install winintel-ime 0.5.2 or specify -AssetRoot."}
  Copy-Item "$AssetRoot\*" $payload -Recurse -Force
  # Installed applications also contain uninstall.exe; it is not a distribution asset.
  if(Test-Path "$payload\uninstall.exe"){Remove-Item "$payload\uninstall.exe"}
}
$vc="C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.39.33519"
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10"
$version="10.0.22621.0"
$env:INCLUDE="$vc\include;$sdk\Include\$version\ucrt;$sdk\Include\$version\shared;$sdk\Include\$version\um"
foreach($arch in @("x64","x86")){
  New-Item -ItemType Directory -Force "$BuildRoot\$arch" | Out-Null
  $env:LIB="$vc\lib\$arch;$sdk\Lib\$version\ucrt\$arch;$sdk\Lib\$version\um\$arch"
  & "$vc\bin\Hostx64\$arch\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX "$PSScriptRoot\Native\Module.cpp" "$PSScriptRoot\Native\Keyboard.cpp" "$PSScriptRoot\Native\CandidateView.cpp" "/Fo$BuildRoot\$arch\\" /link "/DEF:$PSScriptRoot\Native\module.def" "/OUT:$payload\$arch\winintel-tsf.dll" "/IMPLIB:$BuildRoot\$arch\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
  if($LASTEXITCODE){throw "$arch native build failed"}
}
$sources=@(Get-ChildItem "$PSScriptRoot\App\*.cs" | ForEach-Object {$_.FullName})
$sources+="$root\desktop\VoiceSession.cs";$sources+="$root\desktop\ChildJob.cs"
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /codepage:65001 "/win32icon:$payload\winintel.ico" "/out:$payload\winintel-ime.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll $sources
if($LASTEXITCODE){throw "Tray build failed"}
$note="winintel AI IME 0.5.2: Material-inspired settings, compact non-activating candidates, adaptive caret positioning and a voice preview that hides when switching applications. Ranking, speech and translation stay local. Reopen applications after installation."
[IO.File]::WriteAllText("$payload\Local-ranking.txt",$note,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText("$BuildRoot\setup-wrapper.nsi",'!include /CHARSET=UTF8 "'+$PSScriptRoot+'\Setup.nsi"',[Text.Encoding]::ASCII)
if($SkipInstaller){return}
& $Nsis "/DVERSION=0.5.2" "/DPAYLOAD=$payload" "/DOUTPUT=$Output\winintel-ime-0.5.2-ui-setup.exe" "$BuildRoot\setup-wrapper.nsi"
if($LASTEXITCODE){throw "Installer build failed"}
$installer="$Output\winintel-ime-0.5.2-ui-setup.exe"
$hash=(Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$installer.sha256",$hash+"  "+[IO.Path]::GetFileName($installer)+"`n",[Text.Encoding]::ASCII)
