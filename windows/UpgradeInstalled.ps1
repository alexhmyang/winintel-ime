param(
 [string]$Install=(Join-Path $env:LOCALAPPDATA "Programs\winintel-ime-windows-0.5.4"),
 [string]$Native=(Join-Path $env:TEMP "winintel-ime-054-native")
)
$ErrorActionPreference="Stop"
$class="{1B6F3BD2-51A0-4935-B2F9-31CAF0345E71}"
$existing=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,[Microsoft.Win32.RegistryView]::Registry64).OpenSubKey("SOFTWARE\Classes\CLSID\$class\InprocServer32")
if(!$existing){throw "Install the complete Windows package first; this script upgrades an existing registered profile"}
$existing.Dispose()
foreach($arch in @("x64","x86")){
 Copy-Item "$Native\$arch\winintel-tsf.dll" "$Install\$arch\winintel-tsf.dll" -Force
 $view=if($arch -eq "x64"){[Microsoft.Win32.RegistryView]::Registry64}else{[Microsoft.Win32.RegistryView]::Registry32}
 $base=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
 $key=$base.CreateSubKey("Software\Classes\CLSID\$class\InprocServer32")
 try{$key.SetValue("","$Install\$arch\winintel-tsf.dll");$key.SetValue("ThreadingModel","Apartment")}finally{$key.Dispose();$base.Dispose()}
}
Copy-Item "$PSScriptRoot\Bilingual-guide.md" "$Install\Bilingual-guide.md" -Force
Set-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "winintel-ime" -Value "`"$Install\winintel-ime.exe`""
$uninstall="HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows"
Set-ItemProperty $uninstall -Name DisplayVersion -Value "0.5.4"
Set-ItemProperty $uninstall -Name InstallLocation -Value $Install
Set-ItemProperty $uninstall -Name UninstallString -Value "`"$Install\uninstall.exe`""
$shell=New-Object -ComObject WScript.Shell
$shortcut=$shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "数萌 AI 输入法.lnk"))
$shortcut.TargetPath="$Install\winintel-ime.exe";$shortcut.Arguments="--show";$shortcut.WorkingDirectory=$Install;$shortcut.Save()
$menu=Join-Path ([Environment]::GetFolderPath("Programs")) "数萌 AI 输入法"
if(Test-Path $menu){$shortcut=$shell.CreateShortcut("$menu\数萌设置.lnk");$shortcut.TargetPath="$Install\winintel-ime.exe";$shortcut.Arguments="--show";$shortcut.WorkingDirectory=$Install;$shortcut.Save()}
Start-Process "$Install\winintel-ime.exe"
Write-Output "Upgraded the existing Windows profile to 0.5.4. Reopen applications that already loaded the previous native module."
