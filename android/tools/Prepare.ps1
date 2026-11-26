param([string]$PublishedApk)
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Download([string]$url,[string]$target){& curl.exe -L --fail -o $target $url;if($LASTEXITCODE -ne 0){throw "Download failed"}}
if(!(Test-Path "$PSScriptRoot\build-tools\android-15\aapt.exe")){
 Download "https://dl.google.com/android/repository/build-tools_r35_windows.zip" "$PSScriptRoot\build-tools.zip"
 Expand-Archive "$PSScriptRoot\build-tools.zip" "$PSScriptRoot\build-tools" -Force
}
if(!(Test-Path "$PSScriptRoot\platform\android-35\android.jar")){
 Download "https://dl.google.com/android/repository/platform-35_r02.zip" "$PSScriptRoot\platform.zip"
 Expand-Archive "$PSScriptRoot\platform.zip" "$PSScriptRoot\platform" -Force
}
if(!(Test-Path "$PSScriptRoot\kotlin-stdlib.jar")){Download "https://repo.maven.apache.org/maven2/org/jetbrains/kotlin/kotlin-stdlib/1.9.24/kotlin-stdlib-1.9.24.jar" "$PSScriptRoot\kotlin-stdlib.jar"}
if(!(Test-Path "$PSScriptRoot\sherpa-onnx-2.0.3.aar")){& "$PSScriptRoot\DownloadAar.ps1"}
if(!(Test-Path "$root\assets\sensevoice\model.int8.onnx")){
 if(!$PublishedApk){throw "Supply -PublishedApk pointing to the companion release APK to extract model weights."}
 $zip=[IO.Compression.ZipFile]::OpenRead($PublishedApk)
 try{$entry=$zip.GetEntry("assets/sensevoice/model.int8.onnx");[IO.Compression.ZipFileExtensions]::ExtractToFile($entry,"$root\assets\sensevoice\model.int8.onnx",$true)}finally{$zip.Dispose()}
}
if((Get-FileHash "$root\assets\sensevoice\model.int8.onnx").Hash.ToLowerInvariant() -ne "c71f0ce00bec95b07744e116345e33d8cbbe08cef896382cf907bf4b51a2cd51"){throw "Model digest mismatch"}
Write-Output "Dependencies ready. Run Build.ps1 with your private signing key."
