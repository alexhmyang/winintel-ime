param(
  [string]$AssetRoot=$env:WININTEL_ASSETS,
  [string]$BuildRoot=(Join-Path $env:TEMP "winintel-ime-release-windows"),
  [string]$Output=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent),
  [string]$Nsis=$env:WININTEL_NSIS,
  [string]$VcRoot,
  [string]$SdkRoot="${env:ProgramFiles(x86)}\Windows Kits\10",
  [switch]$SkipInstaller
)
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot -Parent
$versionMatch=[regex]::Match((Get-Content "$PSScriptRoot\App\Version.cs" -Raw),'AssemblyFileVersion\("(\d+\.\d+\.\d+)\.\d+"\)')
if(!$versionMatch.Success){throw "Cannot read the Windows source version"}
$version=$versionMatch.Groups[1].Value
if(!$AssetRoot){
  $installed=Get-ChildItem "$env:LOCALAPPDATA\Programs" -Directory -Filter "winintel-ime-windows*" |
    Where-Object {Test-Path "$($_.FullName)\dictionary.bin"} | Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if($installed){$AssetRoot=$installed.FullName}
}
foreach($required in @("runtime\node.exe","node_modules","dictionary.bin","winintel.ico","models\voice","models\sensevoice","models\translation")){
  if(!$AssetRoot -or !(Test-Path (Join-Path $AssetRoot $required))){throw "Offline asset '$required' missing. Install the release package or specify -AssetRoot."}
}
if(!$VcRoot){
  $vswhere="${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
  if(!(Test-Path $vswhere)){throw "Install Visual Studio 2022 C++ build tools"}
  $installation=& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
  $vc=Get-ChildItem "$installation\VC\Tools\MSVC" -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
  if(!$vc){throw "MSVC compiler not found"};$VcRoot=$vc.FullName
}
$sdk=Get-ChildItem "$SdkRoot\Include" -Directory | Where-Object {Test-Path "$($_.FullName)\um\msctf.h"} |
  Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
if(!$sdk){throw "Windows SDK not found"}
$sdkVersion=$sdk.Name
foreach($directory in @($BuildRoot,$Output)){New-Item -ItemType Directory -Force $directory | Out-Null}
$payload=Join-Path $BuildRoot "payload"
if(Test-Path $payload){
  $payload=Join-Path $BuildRoot ("payload-"+[Guid]::NewGuid().ToString("N").Substring(0,8))
}
New-Item -ItemType Directory $payload | Out-Null
# Reuse installed open-source runtimes and model weights; always compile product code.
Get-ChildItem $AssetRoot | Where-Object {$_.Name -notin @("uninstall.exe","ProductDemo.exe","TranslationProbe.exe")} |
  Copy-Item -Destination $payload -Recurse -Force
Copy-Item "$root\standalone\*.mjs" "$payload\standalone" -Force
Copy-Item "$root\LICENSE" "$payload\LICENSE" -Force
Copy-Item "$PSScriptRoot\Bilingual-guide.md" "$payload\Bilingual-guide.md" -Force
Copy-Item "$PSScriptRoot\English-news-guide.md" "$payload\English-news-guide.md" -Force
New-Item -ItemType Directory -Force "$payload\service" | Out-Null
Copy-Item "$root\service\*" "$payload\service" -Recurse -Force
Copy-Item "$root\README.md","$root\README.en.md" "$payload" -Force
Copy-Item "$PSScriptRoot\Legal\*" "$payload\licenses" -Force
Copy-Item "$root\data\rime-ice\provenance.json" "$payload\licenses\dictionary-provenance.json" -Force
& "$payload\runtime\node.exe" "$root\standalone\dictionary.mjs" "$payload\dictionary.bin"
if($LASTEXITCODE){throw "Dictionary build failed"}
& "$payload\runtime\node.exe" "$root\standalone\english-dictionary.mjs" "$payload"
if($LASTEXITCODE){throw "English dictionary build failed"}
& "$payload\runtime\node.exe" "$root\standalone\industry-packs.mjs" "$payload"
if($LASTEXITCODE){throw "Industry dictionary build failed"}
Copy-Item "$root\docs" "$payload" -Recurse -Force
$env:INCLUDE="$VcRoot\include;$SdkRoot\Include\$sdkVersion\ucrt;$SdkRoot\Include\$sdkVersion\shared;$SdkRoot\Include\$sdkVersion\um"
foreach($arch in @("x64","x86")){
  New-Item -ItemType Directory -Force "$BuildRoot\objects-$arch","$payload\$arch" | Out-Null
  $env:LIB="$VcRoot\lib\$arch;$SdkRoot\Lib\$sdkVersion\ucrt\$arch;$SdkRoot\Lib\$sdkVersion\um\$arch"
  & "$VcRoot\bin\Hostx64\$arch\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX "$PSScriptRoot\Native\Module.cpp" "$PSScriptRoot\Native\Keyboard.cpp" "$PSScriptRoot\Native\CandidateView.cpp" "/Fo$BuildRoot\objects-$arch\\" /link "/DEF:$PSScriptRoot\Native\module.def" "/OUT:$payload\$arch\winintel-tsf.dll" "/IMPLIB:$BuildRoot\objects-$arch\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
  if($LASTEXITCODE){throw "Native $arch build failed"}
}
$sources=@(Get-ChildItem "$PSScriptRoot\App\*.cs" | ForEach-Object {$_.FullName})
$sources+="$root\desktop\VoiceSession.cs";$sources+="$root\desktop\ChildJob.cs"
$framework="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$wpf="$framework\WPF"
$winmd=Get-ChildItem "$SdkRoot\UnionMetadata" -Directory | Where-Object {$_.Name -match '^\d+\.' -and (Test-Path "$($_.FullName)\Windows.winmd")} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$facade=Get-ChildItem "$env:WINDIR\Microsoft.NET\assembly\GAC_MSIL\System.Runtime" -Filter System.Runtime.dll -Recurse | Select-Object -First 1
if(!$winmd -or !$facade){throw "Windows SDK WinRT metadata and .NET System.Runtime are required"}
$references=@("/r:System.Windows.Forms.dll","/r:System.Drawing.dll","/r:System.Web.Extensions.dll","/r:System.Net.Http.dll","/r:System.Security.dll","/r:$wpf\UIAutomationClient.dll","/r:$wpf\UIAutomationTypes.dll","/r:$wpf\WindowsBase.dll","/r:$framework\System.Runtime.WindowsRuntime.dll","/r:$($winmd.FullName)\Windows.winmd","/r:$($facade.FullName)")
& "$framework\csc.exe" /nologo /target:winexe /define:WININTEL_REALTIME /main:Winintel.Windows.RealtimeProgram /platform:x64 /codepage:65001 "/win32icon:$payload\winintel.ico" "/out:$payload\winintel-ime.exe" $references $sources
if($LASTEXITCODE){throw "Desktop application build failed"}
[IO.File]::WriteAllText("$BuildRoot\payload-path.txt",$payload,[Text.Encoding]::UTF8)
if($SkipInstaller){Write-Output "Compiled payload: $payload";return}
if(!$Nsis){
  $candidates=@("${env:ProgramFiles(x86)}\NSIS\makensis.exe","$env:ProgramFiles\NSIS\makensis.exe","$env:LOCALAPPDATA\winintel-build\nsis\makensis.exe")
  $found=Get-Command makensis.exe -ErrorAction SilentlyContinue
  if($found){$candidates=@($found.Source)+$candidates}
  $Nsis=$candidates | Where-Object {Test-Path $_} | Select-Object -First 1
}
if(!$Nsis -or !(Test-Path $Nsis)){throw "Install NSIS 3 or specify -Nsis"}
$installer=Join-Path $Output "winintel-ime-$version-setup.exe"
& $Nsis /V2 "/DVERSION=$version" "/DPAYLOAD=$payload" "/DOUTPUT=$installer" "$PSScriptRoot\Setup.nsi"
if($LASTEXITCODE){throw "Installer build failed"}
$hash=(Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$installer.sha256","$hash  $([IO.Path]::GetFileName($installer))`r`n",[Text.Encoding]::ASCII)
Get-Item $installer | Select-Object FullName,Length
