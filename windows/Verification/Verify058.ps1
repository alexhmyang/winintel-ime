param([string]$Payload="$env:TEMP\winintel-ime-057-build\payload",[string]$Check="$env:TEMP\winintel-ime-058-check",[switch]$SkipNative)
$ErrorActionPreference="Stop"
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
New-Item -ItemType Directory -Force $Check,"$Check\testpayload\x64","$Check\objects" | Out-Null
& "$Payload\runtime\node.exe" "$root\standalone\english-dictionary.mjs" $Payload
if($LASTEXITCODE){throw "English dictionary failed"}
$installation=& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
$vc=(Get-ChildItem "$installation\VC\Tools\MSVC" -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10"
$ver=(Get-ChildItem "$sdk\Include" -Directory | Sort-Object Name -Descending | Select-Object -First 1).Name
$env:INCLUDE="$vc\include;$sdk\Include\$ver\ucrt;$sdk\Include\$ver\shared;$sdk\Include\$ver\um"
if(!$SkipNative){foreach($arch in @("x64","x86")){
  New-Item -ItemType Directory -Force "$Check\objects-$arch" | Out-Null
  $env:LIB="$vc\lib\$arch;$sdk\Lib\$ver\ucrt\$arch;$sdk\Lib\$ver\um\$arch"
  & "$vc\bin\Hostx64\$arch\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX "$root\windows\Native\Module.cpp" "$root\windows\Native\Keyboard.cpp" "$root\windows\Native\CandidateView.cpp" "/Fo$Check\objects-$arch\\" /link "/DEF:$root\windows\Native\module.def" "/OUT:$Payload\$arch\winintel-tsf.dll" "/IMPLIB:$Check\objects-$arch\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
  if($LASTEXITCODE){throw "Native $arch compile failed"}
}
$env:LIB="$vc\lib\x64;$sdk\Lib\$ver\ucrt\x64;$sdk\Lib\$ver\um\x64"
& "$vc\bin\Hostx64\x64\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX /DWININTEL_TEST_STORE "$root\windows\Native\Module.cpp" "$root\windows\Native\Keyboard.cpp" "$root\windows\Native\CandidateView.cpp" "/Fo$Check\objects\\" /link "/DEF:$root\windows\Native\module.def" "/OUT:$Check\testpayload\x64\winintel-tsf.dll" "/IMPLIB:$Check\objects\probe.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
if($LASTEXITCODE){throw "Test native compile failed"}
}
Copy-Item "$Payload\dictionary.bin","$Payload\character-readings.tsv","$Payload\english-words.tsv","$Payload\hot-chinese.tsv" "$Check\testpayload" -Force
Copy-Item "$Payload\hotwords.json" "$Check" -Force
$framework="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$winmd="$sdk\UnionMetadata\10.0.22621.0\Windows.winmd"
$facade=(Get-ChildItem "$env:WINDIR\Microsoft.NET\assembly\GAC_MSIL\System.Runtime" -Filter System.Runtime.dll -Recurse | Select-Object -First 1).FullName
$refs=@("/r:System.Windows.Forms.dll","/r:System.Drawing.dll","/r:System.Web.Extensions.dll","/r:System.Net.Http.dll","/r:System.Security.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationTypes.dll","/r:$framework\WPF\WindowsBase.dll","/r:$framework\System.Runtime.WindowsRuntime.dll","/r:$winmd","/r:$facade")
$sources=@(Get-ChildItem "$root\windows\App\*.cs" | ForEach-Object FullName)+@("$root\desktop\VoiceSession.cs","$root\desktop\ChildJob.cs")
& "$framework\csc.exe" /nologo /target:winexe /platform:x64 /codepage:65001 "/win32icon:$Payload\winintel.ico" "/out:$Payload\winintel-ime.exe" $refs $sources
if($LASTEXITCODE){throw "Application compile failed"}
& "$framework\csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 /r:System.Windows.Forms.dll "/out:$Check\EnglishProbe.exe" "$PSScriptRoot\EnglishProbe.cs"
if($LASTEXITCODE){throw "English probe compile failed"}
& "$Check\EnglishProbe.exe" "$Check\testpayload" "$Check\english-results.md"
Get-Content "$Check\english-results.md"
if($LASTEXITCODE){throw "English checks failed"}
& "$framework\csc.exe" /nologo /target:exe /main:ContentProbe /platform:x64 /codepage:65001 "/out:$Check\ContentProbe.exe" $refs $sources "$PSScriptRoot\ContentProbe.cs"
if($LASTEXITCODE){throw "Content probe compile failed"}
& "$Check\ContentProbe.exe" $Payload "$Check\content-results.md"
if($LASTEXITCODE){throw "Content checks failed"}
