param([string]$Payload="F:\Temp\winintel-ime-0510\payload",[string]$Version="0.5.10",[string]$PreviousVersion="0.5.9",[string]$Report="F:\Temp\winintel-ime-0510\install-results.md")
$ErrorActionPreference="Stop"
$installed="$env:LOCALAPPDATA\Programs\winintel-ime-windows-$Version"
$previous="$env:LOCALAPPDATA\Programs\winintel-ime-windows-$PreviousVersion"
$class="Software\Classes\CLSID\{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}\InprocServer32"
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
 $key=$base.OpenSubKey($class);if(!$key){throw "An installed TSF profile is required"};$key.Dispose();$base.Dispose()
}
if(!(Test-Path "$previous\uninstall.exe")){throw "Previous installation missing"}
Get-Process -Name "winintel-ime" -ErrorAction SilentlyContinue | Where-Object {$_.Path -like "$env:LOCALAPPDATA\Programs\winintel-ime-windows-*\winintel-ime.exe"} | ForEach-Object {$_.Kill();$_.WaitForExit(5000)|Out-Null}
New-Item -ItemType Directory -Force $installed | Out-Null
Copy-Item "$Payload\*" $installed -Recurse -Force
Copy-Item "$previous\uninstall.exe" "$installed\uninstall.exe" -Force
$rows=@("| Desktop upgrade verification | Result |","|---|---|")
foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
 $key=$base.CreateSubKey($class);$arch=if($view -eq [Microsoft.Win32.RegistryView]::Registry64){"x64"}else{"x86"}
 $key.SetValue("","$installed\$arch\winintel-tsf.dll");$key.SetValue("ThreadingModel","Apartment");$key.Dispose();$base.Dispose()
 $rows+="| $view COM registration | PASS |"
}
Set-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "winintel-ime" -Value "`"$installed\winintel-ime.exe`""
$uninstall="HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows"
Set-ItemProperty $uninstall -Name DisplayVersion -Value $Version
Set-ItemProperty $uninstall -Name InstallLocation -Value $installed
Set-ItemProperty $uninstall -Name UninstallString -Value "`"$installed\uninstall.exe`""
$shell=New-Object -ComObject WScript.Shell
foreach($path in @((Join-Path ([Environment]::GetFolderPath("Desktop")) "数萌 AI 输入法.lnk"),(Join-Path ([Environment]::GetFolderPath("Programs")) "数萌 AI 输入法\数萌设置.lnk"))){
 if(Test-Path $path){$shortcut=$shell.CreateShortcut($path);$shortcut.TargetPath="$installed\winintel-ime.exe";$shortcut.Arguments="--show";$shortcut.WorkingDirectory=$installed;$shortcut.Save()}
}
foreach($name in @("winintel-ime.exe","x64\winintel-tsf.dll","x86\winintel-tsf.dll","standalone\voice-punctuation.mjs")){
 if((Get-FileHash "$installed\$name").Hash -ne (Get-FileHash "$Payload\$name").Hash){throw "Content mismatch: $name"}
 $rows+="| $name | PASS |"
}
if([Diagnostics.FileVersionInfo]::GetVersionInfo("$installed\winintel-ime.exe").FileVersion -ne "$Version.0"){throw "Version mismatch"}
Start-Process "$installed\winintel-ime.exe"
$rows+="| Installed version | $Version |"
$rows | Set-Content $Report -Encoding UTF8
$rows
