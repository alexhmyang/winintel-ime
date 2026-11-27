param([string]$Installer="C:\Users\86187\Documents\Trae\default\winintel-ime-0.5.9-setup.exe",[string]$Payload="F:\Temp\winintel-ime-059-build\payload")
$ErrorActionPreference="Stop"
$process=Start-Process $Installer -ArgumentList "/S" -PassThru -Wait
if($process.ExitCode){throw "Installation failed: $($process.ExitCode)"}
$installed="$env:LOCALAPPDATA\Programs\winintel-ime-windows-0.5.9"
$rows=@("| Installed component | Result |","|---|---|")
foreach($name in @("x64\winintel-tsf.dll","x86\winintel-tsf.dll","winintel-ime.exe","dictionary.bin","standalone\voice-worker.mjs","standalone\voice-refiner.mjs","standalone\voice-punctuation.mjs")){
 if((Get-FileHash "$installed\$name").Hash -ne (Get-FileHash "$Payload\$name").Hash){throw "Installed content differs: $name"}
 $rows+="| $name | PASS |"
}
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
 $key=$base.OpenSubKey("Software\Classes\CLSID\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}\InprocServer32")
 if(!$key){throw "Registration missing"}
 $path=$key.GetValue("");$key.Dispose();$base.Dispose()
 if($path -notlike "$installed\*"){throw "Registration points to wrong version: $path"}
 $user=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
 $override=$user.OpenSubKey("Software\Classes\CLSID\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}\InprocServer32",$true)
 if($override){$arch=if($view -eq [Microsoft.Win32.RegistryView]::Registry64){"x64"}else{"x86"};$override.SetValue("","$installed\$arch\winintel-tsf.dll");$override.Dispose()}
 $user.Dispose();$rows+="| $view registration | PASS |"
}
$startup=(Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name winintel-ime)."winintel-ime"
if($startup -notlike "*winintel-ime-windows-0.5.9*"){throw "Startup points to wrong version"}
Get-Process -Name "winintel-ime" -ErrorAction SilentlyContinue | Where-Object {$_.Path -like "$env:LOCALAPPDATA\Programs\winintel-ime-windows-*\winintel-ime.exe"} | Stop-Process -Force
Start-Process "$installed\winintel-ime.exe"
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo("$installed\winintel-ime.exe").FileVersion
if($version -ne "0.5.9.0"){throw "Incorrect installed version"}
$rows+="| Installed version | $version |"
$rows | Set-Content "$PSScriptRoot\059-install-results.md" -Encoding UTF8
$hash=(Get-FileHash $Installer -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$Installer.sha256","$hash  $([IO.Path]::GetFileName($Installer))`r`n",[Text.Encoding]::ASCII)
$rows
