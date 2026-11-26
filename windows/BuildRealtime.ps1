param([string]$Output="$env:TEMP\winintel-ime-realtime\winintel-ime.exe")
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot -Parent
$folder=Split-Path $Output -Parent
if(!(Test-Path $folder)){New-Item -ItemType Directory $folder -Force | Out-Null}
$framework="$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
$sdk="${env:ProgramFiles(x86)}\Windows Kits\10"
$metadata=Get-ChildItem "$sdk\UnionMetadata" -Directory | Where-Object {$_.Name -match '^\d+\.' -and (Test-Path "$($_.FullName)\Windows.winmd")} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$facade=(Get-ChildItem "$env:WINDIR\Microsoft.NET\assembly\GAC_MSIL\System.Runtime" -Filter System.Runtime.dll -Recurse | Select-Object -First 1).FullName
$installed="$env:LOCALAPPDATA\Programs\winintel-ime-windows-0.5.8"
$refs=@("/r:System.Windows.Forms.dll","/r:System.Drawing.dll","/r:System.Web.Extensions.dll","/r:System.Net.Http.dll","/r:System.Security.dll","/r:$framework\WPF\UIAutomationClient.dll","/r:$framework\WPF\UIAutomationTypes.dll","/r:$framework\WPF\WindowsBase.dll","/r:$framework\System.Runtime.WindowsRuntime.dll","/r:$($metadata.FullName)\Windows.winmd","/r:$facade")
$sources=@(Get-ChildItem "$PSScriptRoot\App\*.cs" | ForEach-Object FullName)+@("$root\desktop\VoiceSession.cs","$root\desktop\ChildJob.cs")
& "$framework\csc.exe" /nologo /target:winexe /define:WININTEL_REALTIME /main:Winintel.Windows.RealtimeProgram /platform:x64 /codepage:65001 "/win32icon:$installed\winintel.ico" "/out:$Output" $refs $sources
if($LASTEXITCODE){throw "Realtime application compile failed"}
Write-Output "Built realtime news application: $Output"
