param(
  [string]$App=(Join-Path $env:TEMP "winintel-ime-sogou-build\winintel-ime.exe"),
  [string]$Assets=(Join-Path $env:LOCALAPPDATA "Programs\winintel-ime-windows"),
  [string]$Work=(Join-Path $env:TEMP "winintel-ime-sogou-check")
)
$ErrorActionPreference="Stop"
$source=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
New-Item -ItemType Directory -Force "$Work\objects","$Work\testpayload\x64","$Work\screens" | Out-Null
Copy-Item "$Assets\dictionary.bin" "$Work\testpayload" -Force
Copy-Item $App "$Work\winintel-ime.exe" -Force
$vc="C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.39.33519"
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10"
$version="10.0.22621.0"
$env:INCLUDE="$vc\include;$sdk\Include\$version\ucrt;$sdk\Include\$version\shared;$sdk\Include\$version\um"
$env:LIB="$vc\lib\x64;$sdk\Lib\$version\ucrt\x64;$sdk\Lib\$version\um\x64"
& "$vc\bin\Hostx64\x64\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX /DWININTEL_TEST_STORE "$source\windows\Native\Module.cpp" "$source\windows\Native\Keyboard.cpp" "$source\windows\Native\CandidateView.cpp" "/Fo$Work\objects\\" /link "/DEF:$source\windows\Native\module.def" "/OUT:$Work\testpayload\x64\winintel-tsf.dll" "/IMPLIB:$Work\objects\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
if($LASTEXITCODE){throw "TSF verification build failed"}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /codepage:65001 "/out:$Work\UiProbe.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll "$PSScriptRoot\UiProbe.cs"
if($LASTEXITCODE){throw "Interaction verification build failed"}
$process=Start-Process "$Work\UiProbe.exe" -ArgumentList @("`"$Work\testpayload`"","`"$Work\screens`"") -Wait -PassThru
Get-Content "$Work\screens\ui-results.txt"
if($process.ExitCode){throw "Interaction verification failed"}
