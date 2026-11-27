param([string]$Payload="F:\Temp\winintel-ime-059-build\payload")
$ErrorActionPreference="Stop"
$installed="$env:LOCALAPPDATA\Programs\winintel-ime-windows-0.5.9"
$old="$env:LOCALAPPDATA\Programs\winintel-ime-windows-0.5.8"
$class="Software\Classes\CLSID\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}\InprocServer32"
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
 $key=$base.OpenSubKey($class);if(!$key){throw "An installed TSF profile is required"};$key.Dispose();$base.Dispose()
}
New-Item -ItemType Directory -Force $installed | Out-Null
Copy-Item "$Payload\*" $installed -Recurse -Force
Copy-Item "$old\uninstall.exe" "$installed\uninstall.exe"
$rows=@("| Current-user upgrade | Result |","|---|---|")
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
 $key=$base.CreateSubKey($class);$arch=if($view -eq [Microsoft.Win32.RegistryView]::Registry64){"x64"}else{"x86"}
 $key.SetValue("","$installed\$arch\winintel-tsf.dll");$key.SetValue("ThreadingModel","Apartment");$key.Dispose();$base.Dispose()
 $rows+="| $view effective COM registration | PASS |"
}
Set-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "winintel-ime" -Value "`"$installed\winintel-ime.exe`""
$uninstall="HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows"
Set-ItemProperty $uninstall -Name DisplayVersion -Value "0.5.9"
Set-ItemProperty $uninstall -Name InstallLocation -Value $installed
Set-ItemProperty $uninstall -Name UninstallString -Value "`"$installed\uninstall.exe`""
$shell=New-Object -ComObject WScript.Shell
foreach($path in @((Join-Path ([Environment]::GetFolderPath("Desktop")) "数萌 AI 输入法.lnk"),(Join-Path ([Environment]::GetFolderPath("Programs")) "数萌 AI 输入法\数萌设置.lnk"))){
 if(Test-Path $path){$shortcut=$shell.CreateShortcut($path);$shortcut.TargetPath="$installed\winintel-ime.exe";$shortcut.Arguments="--show";$shortcut.WorkingDirectory=$installed;$shortcut.Save()}
}
foreach($name in @("x64\winintel-tsf.dll","x86\winintel-tsf.dll","winintel-ime.exe","dictionary.bin","standalone\voice-punctuation.mjs","standalone\voice-worker.mjs","standalone\voice-refiner.mjs")){
 if((Get-FileHash "$installed\$name").Hash -ne (Get-FileHash "$Payload\$name").Hash){throw "Content mismatch: $name"}
 $rows+="| $name | PASS |"
}
Get-Process -Name "winintel-ime" -ErrorAction SilentlyContinue | Where-Object {$_.Path -like "$env:LOCALAPPDATA\Programs\winintel-ime-windows-*\winintel-ime.exe"} | Stop-Process -Force
Start-Process "$installed\winintel-ime.exe"
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo("$installed\winintel-ime.exe").FileVersion
if($version -ne "0.5.9.0"){throw "Version mismatch"}
$rows+="| Installed version | $version |"
$rows | Set-Content "C:\Users\86187\Documents\Trae\default\winintel-ime-current\windows\Verification\059-install-results.md" -Encoding UTF8
$rows
