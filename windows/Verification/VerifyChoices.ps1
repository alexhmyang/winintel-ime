param(
  [string]$Payload="$env:LOCALAPPDATA\Programs\winintel-ime-windows",
  [string]$WorkDir="$env:TEMP\winintel-choice-check-$([Guid]::NewGuid().ToString('N'))",
  [switch]$OnlyPaging
)
$ErrorActionPreference="Stop"
$taskRoot=$WorkDir
$source=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if(!(Test-Path "$Payload\dictionary.bin")){throw "Dictionary not found under Payload"}
New-Item -ItemType Directory -Path $taskRoot -Force | Out-Null
$vc="C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.39.33519"
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10"
$sdkVersion="10.0.22621.0"
$env:INCLUDE="$vc\include;$sdk\Include\$sdkVersion\ucrt;$sdk\Include\$sdkVersion\shared;$sdk\Include\$sdkVersion\um"
$env:LIB="$vc\lib\x64;$sdk\Lib\$sdkVersion\ucrt\x64;$sdk\Lib\$sdkVersion\um\x64"
$fixtureId=[Guid]::NewGuid().ToString("N")
$learningFixture="$taskRoot\learning-fixture-$fixtureId"
$pagingFixture="$taskRoot\paging-fixture-$fixtureId"
foreach($path in @($learningFixture,$pagingFixture)){New-Item -ItemType Directory -Path $path -Force | Out-Null}
New-Item -ItemType Directory -Path "$learningFixture\winintel-ime-native" -Force | Out-Null
$legacyWord=[string][char]0x65E7+[char]0x8BCD
[IO.File]::WriteAllText("$learningFixture\winintel-ime-native\choices.tsv","legacyfixture`t$legacyWord`t3`n",[Text.UTF8Encoding]::new($false))
& "$vc\bin\Hostx64\x64\cl.exe" /nologo /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX "$source\windows\Verification\LearningProbe.cpp" "/Fo$taskRoot\LearningProbe.obj" "/Fe$taskRoot\LearningProbe.exe" /link user32.lib
if($LASTEXITCODE){throw "Learning probe build failed"}
if(!$OnlyPaging){& "$taskRoot\LearningProbe.exe" "$Payload\dictionary.bin" $learningFixture | Tee-Object "$taskRoot\learning-result.txt"
if($LASTEXITCODE){throw "Learning verification failed"}
}
& "$taskRoot\LearningProbe.exe" "$Payload\dictionary.bin" $pagingFixture paging | Tee-Object "$taskRoot\candidate-count.txt"
if($LASTEXITCODE){throw "Paging fixture failed"}
New-Item -ItemType Directory -Path "$taskRoot\testpayload\x64","$taskRoot\testobjects" -Force | Out-Null
if(!(Test-Path "$taskRoot\testpayload\dictionary.bin")){Copy-Item "$Payload\dictionary.bin" "$taskRoot\testpayload\dictionary.bin"}
& "$vc\bin\Hostx64\x64\cl.exe" /nologo /LD /MT /EHsc /std:c++17 /utf-8 /DNOMINMAX /DWININTEL_TEST_STORE "$source\windows\Native\Module.cpp" "$source\windows\Native\Keyboard.cpp" "$source\windows\Native\CandidateView.cpp" "/Fo$taskRoot\testobjects\\" /link "/DEF:$source\windows\Native\module.def" "/OUT:$taskRoot\testpayload\x64\winintel-tsf.dll" "/IMPLIB:$taskRoot\testobjects\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
if($LASTEXITCODE){throw "Test TSF DLL build failed"}
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /codepage:65001 "/out:$taskRoot\PagingProbe.exe" /r:System.Windows.Forms.dll "$source\windows\Verification\PagingProbe.cs"
if($LASTEXITCODE){throw "Paging probe build failed"}
$probe=Start-Process "$taskRoot\PagingProbe.exe" -ArgumentList @("`"$taskRoot\testpayload`"","`"$pagingFixture`"") -PassThru -Wait
Copy-Item "$pagingFixture\paging-result.txt" "$taskRoot\paging-result.txt"
Get-Content "$taskRoot\paging-result.txt"
if($probe.ExitCode){throw "TSF paging verification failed"}
