param(
  [string]$ToolRoot=(Join-Path $env:LOCALAPPDATA "winintel-build\android-tools"),
  [string]$ModelRoot=(Join-Path $env:LOCALAPPDATA "winintel-build\android-models"),
  [string]$PublishedApk,
  [string]$SeedTools
)
$ErrorActionPreference="Stop"
foreach($directory in @($ToolRoot,"$ModelRoot\sensevoice")){New-Item -ItemType Directory -Force $directory | Out-Null}
function Download([string]$url,[string]$target){
  & curl.exe -L --fail --retry 2 -o $target $url
  if($LASTEXITCODE){throw "Download failed: $url"}
}
if($SeedTools){
  foreach($directory in @("build-tools","platform")){if(!(Test-Path "$ToolRoot\$directory")){Copy-Item "$SeedTools\$directory" $ToolRoot -Recurse}}
  foreach($file in @("sherpa-onnx-2.0.3.aar","kotlin-stdlib.jar")){if(!(Test-Path "$ToolRoot\$file")){Copy-Item "$SeedTools\$file" $ToolRoot}}
}
if(!(Test-Path "$ToolRoot\build-tools\android-15\aapt.exe")){
  Download "https://dl.google.com/android/repository/build-tools_r35_windows.zip" "$ToolRoot\build-tools.zip"
  Expand-Archive "$ToolRoot\build-tools.zip" "$ToolRoot\build-tools" -Force
}
if(!(Test-Path "$ToolRoot\platform\android-35\android.jar")){
  Download "https://dl.google.com/android/repository/platform-35_r02.zip" "$ToolRoot\platform.zip"
  Expand-Archive "$ToolRoot\platform.zip" "$ToolRoot\platform" -Force
}
if(!(Test-Path "$ToolRoot\kotlin-stdlib.jar")){Download "https://repo.maven.apache.org/maven2/org/jetbrains/kotlin/kotlin-stdlib/1.9.24/kotlin-stdlib-1.9.24.jar" "$ToolRoot\kotlin-stdlib.jar"}
if(!(Test-Path "$ToolRoot\sherpa-onnx-2.0.3.aar")){Download "https://github.com/k2-fsa/sherpa-onnx/releases/download/v2.0.3/sherpa-onnx-2.0.3.aar" "$ToolRoot\sherpa-onnx-2.0.3.aar"}
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(!(Test-Path "$ModelRoot\sensevoice\model.int8.onnx") -or !(Test-Path "$ModelRoot\sensevoice\tokens.txt")){
  if(!$PublishedApk -or !(Test-Path $PublishedApk)){throw "Supply -PublishedApk from the official Android release to extract model files"}
  $zip=[IO.Compression.ZipFile]::OpenRead($PublishedApk)
  try{
    foreach($name in @("model.int8.onnx","tokens.txt")){
      $entry=$zip.GetEntry("assets/sensevoice/$name")
      if(!$entry){throw "Model file missing in APK: $name"}
      [IO.Compression.ZipFileExtensions]::ExtractToFile($entry,"$ModelRoot\sensevoice\$name",$true)
    }
  }finally{$zip.Dispose()}
}
if((Get-FileHash "$ModelRoot\sensevoice\model.int8.onnx").Hash.ToLowerInvariant() -ne "c71f0ce00bec95b07744e116345e33d8cbbe08cef896382cf907bf4b51a2cd51"){throw "Model SHA256 mismatch"}
Write-Output "Android build tools and verified offline model are ready outside the repository."
