$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$HostRoot = $null
foreach ($View in @([Microsoft.Win32.RegistryView]::Registry32, [Microsoft.Win32.RegistryView]::Registry64)) {
    $Base = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, $View)
    $Key = $Base.OpenSubKey("Software\Rime\Weasel")
    if ($Key) { $HostRoot = $Key.GetValue("WeaselRoot"); $Key.Dispose() }
    $Base.Dispose()
    if ($HostRoot -and (Test-Path (Join-Path $HostRoot "WeaselDeployer.exe"))) { break }
}
if (-not $HostRoot -or -not (Test-Path (Join-Path $HostRoot "WeaselDeployer.exe"))) {
    $Installer = Join-Path $Root "vendor\weasel-0.17.4.0-installer.exe"
    $Expected = "CF509534A8F5F8AF9C98ED7CBB8F135439F145A8CBE7E50EDE42BB5B5AB45C29"
    if ((Get-FileHash $Installer -Algorithm SHA256).Hash -ne $Expected) { throw "System input host checksum mismatch" }
    # Windows TSF registration needs administrator rights. Run the official host installer once.
    $Process = Start-Process -FilePath $Installer -ArgumentList "/S" -Verb RunAs -Wait -PassThru
    if ($Process.ExitCode -notin @(0, 3010)) { throw "System input host installation failed" }
    $Base = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry32)
    $Key = $Base.OpenSubKey("Software\Rime\Weasel")
    if ($Key) { $HostRoot = $Key.GetValue("WeaselRoot"); $Key.Dispose() }
    $Base.Dispose()
}
if (-not $HostRoot) { throw "Cannot locate Windows input host" }
$UserDir = Join-Path $env:APPDATA "Rime"
$Key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey("Software\Rime\Weasel")
if ($Key) { $CustomDir = $Key.GetValue("RimeUserDir"); if ($CustomDir) { $UserDir = [Environment]::ExpandEnvironmentVariables($CustomDir) }; $Key.Dispose() }
& (Join-Path $Root "runtime\node.exe") (Join-Path $PSScriptRoot "configure-ime.mjs") --user-dir $UserDir
if ($LASTEXITCODE -ne 0) { throw "Input schema configuration failed" }
$Deployment = Start-Process -FilePath (Join-Path $HostRoot "WeaselDeployer.exe") -ArgumentList "/deploy" -Wait -PassThru
if ($Deployment.ExitCode -ne 0) { throw "Rime schema deployment failed" }
Write-Output "winintel input schema deployed"
