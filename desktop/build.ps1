param(
    [string]$BuildDir = "$env:TEMP\winintel-ime-build",
    [string]$OutputDir = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [switch]$PayloadOnly
)
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $BuildDir)) { throw "Prepare build directory first: $BuildDir" }
if (-not (Test-Path $OutputDir)) { throw "Output directory does not exist" }
$Payload = Join-Path $BuildDir "payload"
New-Item -ItemType Directory -Path $Payload -Force | Out-Null
foreach ($Folder in @("src", "web", "rime", "data", "models", "node_modules", "desktop", "runtime", "vendor", "licenses")) {
    New-Item -ItemType Directory -Path (Join-Path $Payload $Folder) -Force | Out-Null
}
foreach ($Folder in @("src", "web", "rime", "data", "models", "node_modules")) {
    Copy-Item (Join-Path $Root "$Folder\*") (Join-Path $Payload $Folder) -Recurse -Force
}
foreach ($File in @("configure-ime.ps1", "configure-ime.mjs", "winintel_desktop.lua", "winintel_english.schema.yaml", "customer-guide.txt", "distribution-notices.txt")) {
    Copy-Item (Join-Path $PSScriptRoot $File) (Join-Path $Payload "desktop") -Force
}
Copy-Item (Join-Path $Root "LICENSE") $Payload -Force
Copy-Item (Join-Path $Root "package.json") $Payload -Force
Copy-Item (Join-Path $Root "package-lock.json") $Payload -Force
Copy-Item (Join-Path $Root "desktop\customer-guide.txt") (Join-Path $Payload "Install-guide.txt") -Force
Copy-Item (Get-Command node.exe).Source (Join-Path $Payload "runtime\node.exe") -Force
$Weasel = Join-Path $BuildDir "weasel-0.17.4.0-installer.exe"
if ((Get-FileHash $Weasel -Algorithm SHA256).Hash -ne "CF509534A8F5F8AF9C98ED7CBB8F135439F145A8CBE7E50EDE42BB5B5AB45C29") { throw "Weasel checksum mismatch" }
Copy-Item $Weasel (Join-Path $Payload "vendor") -Force
$Manifest = Get-Content (Join-Path $Root "models\voice\model-manifest.json") -Raw | ConvertFrom-Json
foreach ($File in $Manifest.files) {
    if ($File.sha256 -and (Get-FileHash (Join-Path $Payload "models\voice\$($File.path)") -Algorithm SHA256).Hash.ToLower() -ne $File.sha256) { throw "ASR checksum mismatch: $($File.path)" }
}
foreach ($File in (Get-ChildItem (Join-Path $BuildDir "licenses") -File)) { Copy-Item $File.FullName (Join-Path $Payload "licenses") -Force }
$Compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$Sources = @("Native.cs", "VoiceSession.cs", "Runtime.cs", "TrayApp.cs", "ChildJob.cs") | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $Compiler /nologo /target:winexe /platform:x64 /optimize+ ("/out:" + (Join-Path $Payload "winintel-ime.exe")) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll $Sources
if ($LASTEXITCODE -ne 0) { throw "Desktop compilation failed" }
if ($PayloadOnly) { Write-Output "Payload ready: $Payload"; exit 0 }
$Output = Join-Path $OutputDir "winintel-AI-Input-Setup-0.1.0.exe"
& (Join-Path $BuildDir "nsis\Bin\makensis.exe") /V2 ("/DPAYLOAD=" + $Payload) ("/DOUTPUT=" + $Output) ("/DSOURCE=" + (Join-Path $PSScriptRoot "installer.nsi")) (Join-Path $PSScriptRoot "compiler.nsi")
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed" }
$Hash = (Get-FileHash $Output -Algorithm SHA256).Hash.ToLower()
[IO.File]::WriteAllText("$Output.sha256", "$Hash  $(Split-Path -Leaf $Output)`n")
Write-Output "Installer ready: $Output"
