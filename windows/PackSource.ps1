# SPDX-License-Identifier: GPL-3.0-or-later
param([string]$Stage="$env:TEMP\winintel-ime-build\meeting050\archive",[string]$Output=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot -Parent
if(Test-Path "$Stage\windows"){throw "Use an empty source staging directory"}
New-Item -ItemType Directory -Force $Stage | Out-Null
Copy-Item $PSScriptRoot "$Stage\windows" -Recurse -Force
Copy-Item "$root\data" "$Stage\data" -Recurse -Force
Copy-Item "$root\LICENSE" $Stage -Force
Copy-Item "$root\package.json","$root\package-lock.json" $Stage -Force
foreach($dir in @("standalone","desktop","models\voice","models\voice\test_wavs")){New-Item -ItemType Directory -Force "$Stage\$dir" | Out-Null}
foreach($file in @("Engine.h","EngineTest.cpp","dictionary.mjs","voice-host.mjs","voice-worker.mjs","voice-refiner.mjs","voice-numbers.mjs")){Copy-Item "$root\standalone\$file" "$Stage\standalone" -Force}
Copy-Item "$root\standalone\translation-*.mjs" "$Stage\standalone" -Force
foreach($file in @("VoiceSession.cs","ChildJob.cs")){Copy-Item "$root\desktop\$file" "$Stage\desktop" -Force}
Copy-Item "$root\models\voice\model-manifest.json","$root\models\voice\README.md" "$Stage\models\voice" -Force
Copy-Item "$root\models\voice\hotwords.txt" "$Stage\models\voice" -Force
Copy-Item "$root\models\sensevoice" "$Stage\models\sensevoice" -Recurse -Force
Copy-Item "$root\models\voice\test_wavs\0.wav","$root\models\voice\test_wavs\1.wav","$root\models\voice\test_wavs\2.wav" "$Stage\models\voice\test_wavs" -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$destination="$Output\winintel-ime-0.5.0-source.zip"
if(Test-Path $destination){throw "Source archive already exists: $destination"}
[IO.Compression.ZipFile]::CreateFromDirectory($Stage,$destination,[IO.Compression.CompressionLevel]::Optimal,$false)
Write-Host "Source archive: $destination"
