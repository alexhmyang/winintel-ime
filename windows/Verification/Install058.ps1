param([string]$Installer=(Join-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) "winintel-ime-0.5.8-setup.exe"),[string]$Payload="$env:TEMP\winintel-ime-057-build\payload")
$ErrorActionPreference="Stop"
$hash=(Get-FileHash $Installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$Installer.sha256","$hash  $([IO.Path]::GetFileName($Installer))`r`n",[Text.Encoding]::ASCII)
$process=Start-Process $Installer -ArgumentList "/S" -PassThru -Wait
if($process.ExitCode){throw "Installation failed: $($process.ExitCode)"}
$installed="$env:LOCALAPPDATA\Programs\winintel-ime-windows-0.5.8"
$rows=@("| Installed component | Result |","|---|---|")
foreach($name in @("x64\winintel-tsf.dll","x86\winintel-tsf.dll","winintel-ime.exe","dictionary.bin","character-readings.tsv","english-words.tsv","hot-chinese.tsv","hotwords.json","service\lexicon-api.mjs")){
  if((Get-FileHash "$installed\$name").Hash -ne (Get-FileHash "$Payload\$name").Hash){throw "Installed content differs: $name"}
  $rows+="| $name | PASS |"
}
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
  $registry=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
  $key=$registry.OpenSubKey("Software\Classes\CLSID\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}\InprocServer32")
  $path=$key.GetValue("");$key.Dispose();$registry.Dispose()
  if($path -notlike "$installed\*"){throw "Registration points to wrong version: $path"}
  $rows+="| $view registration | PASS |"
}
$startup=(Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name winintel-ime)."winintel-ime"
if($startup -notlike "*winintel-ime-windows-0.5.8*"){throw "Startup points to wrong version"}
$rows+="| Startup registration | PASS |"
Get-Process -Name "winintel-ime" -ErrorAction SilentlyContinue | Where-Object {$_.Path -like "$env:LOCALAPPDATA\Programs\winintel-ime-windows-*\winintel-ime.exe"} | Stop-Process -Force
Start-Process "$installed\winintel-ime.exe" -ArgumentList "--news"
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo("$installed\winintel-ime.exe").FileVersion
if($version -ne "0.5.8.0"){throw "Wrong installed version: $version"}
$rows+="| Installed version | $version |"
$rows | Set-Content "$PSScriptRoot\058-install-results.md" -Encoding UTF8
$rows
Get-Item $Installer | Select-Object FullName,Length
