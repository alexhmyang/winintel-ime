# SPDX-License-Identifier: GPL-3.0-or-later
param([string]$AssetRoot="$env:TEMP\winintel-ime-assets")
$ErrorActionPreference="Stop"
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Force $AssetRoot | Out-Null
foreach($dir in @("runtime","models\voice","licenses")){New-Item -ItemType Directory -Force (Join-Path $AssetRoot $dir) | Out-Null}
$nodeVersion="v24.14.0"
$archive="node-$nodeVersion-win-x64.zip"
Invoke-WebRequest "https://nodejs.org/dist/$nodeVersion/$archive" -OutFile "$AssetRoot\$archive" -UseBasicParsing
$checks=(Invoke-WebRequest "https://nodejs.org/dist/$nodeVersion/SHASUMS256.txt" -UseBasicParsing).Content
$expected=($checks -split "`n" | Where-Object {$_ -match [regex]::Escape($archive)+"$"}) -split "\s+"
if(!$expected -or (Get-FileHash "$AssetRoot\$archive" -Algorithm SHA256).Hash -ne $expected[0]){throw "Node checksum mismatch"}
Expand-Archive "$AssetRoot\$archive" $AssetRoot -Force
$nodeHome="$AssetRoot\node-$nodeVersion-win-x64"
Copy-Item "$nodeHome\node.exe" "$AssetRoot\runtime\node.exe" -Force
Copy-Item "$nodeHome\LICENSE" "$AssetRoot\licenses\Node-LICENSE.txt" -Force
$manifest=Get-Content "$PSScriptRoot\..\models\voice\model-manifest.json" -Raw | ConvertFrom-Json
foreach($file in $manifest.files){
  $url="$($manifest.source)/$($manifest.repository)/resolve/$($manifest.revision)/$($file.path)"
  $destination="$AssetRoot\models\voice\$($file.path)"
  Invoke-WebRequest $url -OutFile $destination -UseBasicParsing
  if((Get-Item $destination).Length -ne $file.size){throw "Model size mismatch: $($file.path)"}
  if($file.sha256 -and (Get-FileHash $destination -Algorithm SHA256).Hash -ne $file.sha256){throw "Model checksum mismatch: $($file.path)"}
}
Copy-Item "$PSScriptRoot\..\models\voice\model-manifest.json" "$AssetRoot\models\voice" -Force
Copy-Item "$PSScriptRoot\..\models\voice\test_wavs" "$AssetRoot\models\voice" -Recurse -Force
[IO.File]::WriteAllText("$AssetRoot\package.json",'{"private":true,"dependencies":{"sherpa-onnx-node":"2.0.3","ws":"8.23.0"}}',[Text.Encoding]::ASCII)
& "$nodeHome\node.exe" "$nodeHome\node_modules\npm\bin\npm-cli.js" install --prefix $AssetRoot --omit=dev --ignore-scripts
if($LASTEXITCODE){throw "Dependency install failed"}
Copy-Item "$PSScriptRoot\Legal\*" "$AssetRoot\licenses" -Force
Write-Host "Assets ready. Run windows\Build.ps1 -AssetRoot `"$AssetRoot`""
