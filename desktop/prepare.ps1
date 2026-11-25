param([string]$BuildDir = "$env:TEMP\winintel-ime-build")
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $BuildDir)) { New-Item -ItemType Directory $BuildDir | Out-Null }
function DownloadChecked($Url, $Path, $Hash) {
    if (-not (Test-Path $Path) -or (Get-FileHash $Path -Algorithm SHA256).Hash -ne $Hash) { Invoke-WebRequest $Url -OutFile $Path }
    if ((Get-FileHash $Path -Algorithm SHA256).Hash -ne $Hash) { throw "Checksum mismatch: $Path" }
}
$Weasel = Join-Path $BuildDir "weasel-0.17.4.0-installer.exe"
if (-not (Test-Path $Weasel)) {
    & gh release download 0.17.4 --repo rime/weasel --pattern "weasel-0.17.4.0-installer.exe" --dir $BuildDir
    if ($LASTEXITCODE -ne 0) { throw "Weasel download failed" }
}
if ((Get-FileHash $Weasel -Algorithm SHA256).Hash -ne "CF509534A8F5F8AF9C98ED7CBB8F135439F145A8CBE7E50EDE42BB5B5AB45C29") { throw "Weasel checksum mismatch" }
$NSIS = Join-Path $BuildDir "nsis-3.0.4.1.7z"
if (-not (Test-Path $NSIS)) {
    & gh release download nsis-3.0.4.1 --repo electron-userland/electron-builder-binaries --pattern nsis-3.0.4.1.7z --dir $BuildDir
    if ($LASTEXITCODE -ne 0) { throw "NSIS download failed" }
}
if ((Get-FileHash $NSIS -Algorithm SHA256).Hash -ne "9877DF902530F96357D13A7A31AE2B9DF67F48B11FFC9A1700A7C961574EC5FA") { throw "NSIS checksum mismatch" }
DownloadChecked "https://www.7-zip.org/a/7zr.exe" (Join-Path $BuildDir "7zr.exe") "256FECA8E274E5DA655E2A284FABAFD9F554365EB164862089DACD4E8276D282"
& (Join-Path $BuildDir "7zr.exe") x $NSIS ("-o" + (Join-Path $BuildDir "nsis")) -y
if ($LASTEXITCODE -ne 0) { throw "NSIS extraction failed" }
$LicenseDir = Join-Path $BuildDir "licenses"
New-Item -ItemType Directory -Path $LicenseDir -Force | Out-Null
$NodeVersion = & node --version
$Items = @(
    @{Repo="nodejs/node";Path="LICENSE";Ref=$NodeVersion;Name="Node-LICENSE.txt"},
    @{Repo="k2-fsa/sherpa-onnx";Path="LICENSE";Ref="v1.12.34";Name="Sherpa-LICENSE.txt"},
    @{Repo="csukuangfj/onnxruntime";Path="LICENSE";Ref="8372c86e7f15ed0d8d47e1e0afc115c30db39252";Name="ONNX-Runtime-LICENSE.txt"},
    @{Repo="csukuangfj/onnxruntime";Path="ThirdPartyNotices.txt";Ref="8372c86e7f15ed0d8d47e1e0afc115c30db39252";Name="ONNX-Runtime-ThirdParty.txt"},
    @{Repo="rime/weasel";Path="LICENSE.txt";Ref="0.17.4";Name="Weasel-LICENSE.txt"}
)
foreach ($Item in $Items) {
    $Response = & gh api "repos/$($Item.Repo)/contents/$($Item.Path)?ref=$($Item.Ref)"
    if ($LASTEXITCODE -ne 0) { throw "License download failed" }
    $Meta = $Response | ConvertFrom-Json
    [IO.File]::WriteAllText((Join-Path $LicenseDir $Item.Name), [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Meta.content)))
}
Copy-Item (Join-Path $LicenseDir "Sherpa-LICENSE.txt") (Join-Path $LicenseDir "Apache-2.0.txt") -Force
& node (Join-Path $PSScriptRoot "download-model.mjs")
if ($LASTEXITCODE -ne 0) { throw "Model download failed" }
Write-Output "Build assets prepared"
