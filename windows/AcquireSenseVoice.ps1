# SPDX-License-Identifier: GPL-3.0-or-later
param([Parameter(Mandatory=$true)][string]$AssetRoot)
$ErrorActionPreference='Stop'
if(!(Test-Path $AssetRoot)){throw 'AssetRoot must exist'}
$model=Join-Path $AssetRoot 'models\sensevoice'
New-Item -ItemType Directory -Force $model | Out-Null
$repo='csukuangfj/sherpa-onnx-sense-voice-zh-en-ja-ko-yue-2024-07-17'
$revision='2365baeacb507f821a0c8120fcee3d484dba7a07'
foreach($file in @('model.int8.onnx','tokens.txt')){
    $url="https://hf-mirror.com/$repo/resolve/$revision/$file"
    if($file -eq 'tokens.txt'){$url="https://hf-mirror.com/$repo/raw/$revision/$file"}
    & curl.exe --fail -L --max-time 600 $url -o "$model\$file"
    if($LASTEXITCODE){throw "Download failed: $file"}
}
$expected='C71F0CE00BEC95B07744E116345E33D8CBBE08CEF896382CF907BF4B51A2CD51'
if((Get-FileHash "$model\model.int8.onnx").Hash -ne $expected){throw 'Model checksum mismatch'}
if((Get-FileHash "$model\tokens.txt").Hash -ne 'F449EB28DC567533D7FA59BE34E2ABCA8784F771850C78A47FB731A31429A1DC'){throw 'Tokens checksum mismatch'}
Copy-Item "$PSScriptRoot\..\models\sensevoice\model-manifest.json","$PSScriptRoot\..\models\sensevoice\MODEL_LICENSE","$PSScriptRoot\..\models\sensevoice\README.md" $model -Force
New-Item -ItemType Directory -Force "$AssetRoot\licenses" | Out-Null
Copy-Item "$model\MODEL_LICENSE" "$AssetRoot\licenses\SenseVoice-MODEL_LICENSE" -Force
