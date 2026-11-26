param(
  [string]$JavaHome=$env:JAVA_HOME,
  [string]$ToolRoot=(Join-Path $env:LOCALAPPDATA "winintel-build\android-tools"),
  [string]$ModelRoot=(Join-Path $env:LOCALAPPDATA "winintel-build\android-models"),
  [string]$BuildRoot=(Join-Path $env:TEMP "winintel-ime-release-android"),
  [string]$KeyStore=$env:WININTEL_KEYSTORE,
  [string]$PasswordFile=$env:WININTEL_KEY_PASSWORD_FILE,
  [string]$Output=(Split-Path (Split-Path $PSScriptRoot -Parent) -Parent)
)
$ErrorActionPreference="Stop"
$root=$PSScriptRoot
if(!$JavaHome){
  $javac=Get-Command javac.exe -ErrorAction SilentlyContinue
  if($javac){$JavaHome=Split-Path (Split-Path $javac.Source -Parent) -Parent}
}
if(!$JavaHome -or !(Test-Path "$JavaHome\bin\javac.exe")){throw "Specify -JavaHome pointing to a JDK"}
foreach($item in @($KeyStore,$PasswordFile)){if(!$item -or !(Test-Path $item)){throw "Supply -KeyStore and -PasswordFile from outside the repository"}}
$repo=Split-Path $root -Parent
foreach($item in @($KeyStore,$PasswordFile)){
  if([IO.Path]::GetFullPath($item).StartsWith([IO.Path]::GetFullPath($repo)+"\",[StringComparison]::OrdinalIgnoreCase)){throw "Keep signing credentials outside the repository"}
}
$env:JAVA_HOME=$JavaHome;$env:PATH="$JavaHome\bin;$env:PATH"
$bt="$ToolRoot\build-tools\android-15";$platform="$ToolRoot\platform\android-35\android.jar"
foreach($item in @("$bt\aapt.exe","$bt\d8.bat","$bt\apksigner.bat",$platform,"$ToolRoot\sherpa-onnx-2.0.3.aar","$ToolRoot\kotlin-stdlib.jar","$ModelRoot\sensevoice\model.int8.onnx","$ModelRoot\sensevoice\tokens.txt")){
  if(!(Test-Path $item)){throw "Missing $item. Run tools\PrepareRelease.ps1 first."}
}
$manifest=[xml](Get-Content "$root\AndroidManifest.xml" -Raw -Encoding UTF8)
$version=$manifest.manifest.GetAttribute("versionName","http://schemas.android.com/apk/res/android")
if(!(Test-Path $Output)){New-Item -ItemType Directory -Force $Output | Out-Null}
if(Test-Path $BuildRoot){$BuildRoot=Join-Path $BuildRoot ([Guid]::NewGuid().ToString("N").Substring(0,8))}
New-Item -ItemType Directory -Force "$BuildRoot\classes","$BuildRoot\dex","$BuildRoot\generated","$BuildRoot\aar","$BuildRoot\assets\sensevoice" | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory("$ToolRoot\sherpa-onnx-2.0.3.aar","$BuildRoot\aar")
Copy-Item "$root\assets\*" "$BuildRoot\assets" -Recurse -Force
Copy-Item "$ModelRoot\sensevoice\model.int8.onnx","$ModelRoot\sensevoice\tokens.txt" "$BuildRoot\assets\sensevoice" -Force
function CheckExit([string]$stage){if($LASTEXITCODE){throw "$stage failed: $LASTEXITCODE"}}
# Generated dictionary stays in the build directory.
$node=Get-Command node.exe -ErrorAction SilentlyContinue
if(!$node){throw "Install Node.js 22+ to generate the dictionary"}
& $node.Source "$repo\standalone\dictionary.mjs" "$BuildRoot\assets\dictionary.bin"
CheckExit "Dictionary"
& $node.Source "$repo\standalone\english-dictionary.mjs" "$BuildRoot\assets"
CheckExit "English dictionary"
& "$bt\aapt.exe" package -f -m -J "$BuildRoot\generated" -M "$root\AndroidManifest.xml" -S "$root\res" -A "$BuildRoot\assets" -I $platform -0 onnx -0 bin -F "$BuildRoot\resources.apk"
CheckExit "aapt"
$sources=@(Get-ChildItem "$root\src","$BuildRoot\generated" -Recurse -Filter *.java | ForEach-Object {$_.FullName})
$cp="$platform;$BuildRoot\aar\classes.jar;$ToolRoot\kotlin-stdlib.jar"
& "$JavaHome\bin\javac.exe" --release 8 -encoding UTF-8 -cp $cp -d "$BuildRoot\classes" @sources
CheckExit "javac"
& "$JavaHome\bin\jar.exe" cf "$BuildRoot\app.jar" -C "$BuildRoot\classes" .
CheckExit "jar"
& "$bt\d8.bat" --min-api 26 --lib $platform --output "$BuildRoot\dex" "$BuildRoot\app.jar" "$BuildRoot\aar\classes.jar" "$ToolRoot\kotlin-stdlib.jar"
CheckExit "d8"
Copy-Item "$BuildRoot\resources.apk" "$BuildRoot\unsigned.apk"
$zip=[IO.Compression.ZipFile]::Open("$BuildRoot\unsigned.apk",[IO.Compression.ZipArchiveMode]::Update)
try{
  Get-ChildItem "$BuildRoot\dex" -Filter *.dex | ForEach-Object {[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$_.FullName,$_.Name,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}
  foreach($abi in @("arm64-v8a","x86_64")){
    Get-ChildItem "$BuildRoot\aar\jni\$abi" -Filter *.so | ForEach-Object {[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$_.FullName,"lib/$abi/$($_.Name)",[IO.Compression.CompressionLevel]::NoCompression)|Out-Null}
  }
}finally{$zip.Dispose()}
& "$bt\zipalign.exe" -f -P 16 4 "$BuildRoot\unsigned.apk" "$BuildRoot\aligned.apk"
CheckExit "zipalign"
$apk=Join-Path $Output "winintel-ime-android-$version.apk"
& "$bt\apksigner.bat" sign --ks $KeyStore --ks-key-alias winintel --ks-pass "file:$PasswordFile" --out $apk "$BuildRoot\aligned.apk"
CheckExit "APK signing"
& "$bt\apksigner.bat" verify --verbose --print-certs $apk
CheckExit "Signature verification"
& "$bt\zipalign.exe" -c -P 16 4 $apk
CheckExit "Alignment verification"
[IO.File]::WriteAllText("$BuildRoot\..\android-build-path.txt",$BuildRoot,[Text.Encoding]::UTF8)
$hash=(Get-FileHash $apk -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$apk.sha256","$hash  $([IO.Path]::GetFileName($apk))`r`n",[Text.Encoding]::ASCII)
Get-Item $apk | Select-Object FullName,Length
