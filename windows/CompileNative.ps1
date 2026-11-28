param([string]$Output=(Join-Path $env:TEMP "winintel-ime-054-native"))
$ErrorActionPreference="Stop"
$vc="C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.39.33519"
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10";$version="10.0.22621.0"
$env:INCLUDE="$vc\include;$sdk\Include\$version\ucrt;$sdk\Include\$version\shared;$sdk\Include\$version\um"
foreach($arch in @("x64","x86")){
 New-Item -ItemType Directory -Force "$Output\$arch","$Output\objects-$arch" | Out-Null
 $env:LIB="$vc\lib\$arch;$sdk\Lib\$version\ucrt\$arch;$sdk\Lib\$version\um\$arch"
 & "$vc\bin\Hostx64\$arch\cl.exe" /nologo /LD /MT /O2 /EHsc /std:c++17 /utf-8 /DNOMINMAX "$PSScriptRoot\Native\Module.cpp" "$PSScriptRoot\Native\Keyboard.cpp" "$PSScriptRoot\Native\CandidateView.cpp" "/Fo$Output\objects-$arch\\" /link "/DEF:$PSScriptRoot\Native\module.def" "/OUT:$Output\$arch\winintel-tsf.dll" "/IMPLIB:$Output\objects-$arch\winintel-tsf.lib" uuid.lib ole32.lib oleaut32.lib user32.lib gdi32.lib advapi32.lib
 if($LASTEXITCODE){throw "Native $arch build failed"}
}
