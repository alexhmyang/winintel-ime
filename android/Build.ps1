param(
  [string]$JavaHome="C:\Program Files\Java\jdk-20",
  [string]$KeyStore,
  [string]$PasswordFile,
  [string]$Output
)
$ErrorActionPreference="Stop"
$root=$PSScriptRoot
$env:JAVA_HOME=$JavaHome
$env:PATH="$JavaHome\bin;$env:PATH"
$bt="$root\tools\build-tools\android-15"
$platform="$root\tools\platform\android-35\android.jar"
if(!$KeyStore -or !$PasswordFile){throw "Supply a private keystore and password file. Never distribute either."}
if(!$Output){$Output="$root\build\winintel-ime-android-0.1.0.apk"}
function CheckExit {if($LASTEXITCODE -ne 0){throw "Build tool failed: $LASTEXITCODE"}}
New-Item -ItemType Directory -Force "$root\build\classes","$root\build\dex","$root\build\generated","$root\tools\aar" | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if(!(Test-Path "$root\tools\aar\classes.jar")){
  [IO.Compression.ZipFile]::ExtractToDirectory("$root\tools\sherpa-onnx-2.0.3.aar","$root\tools\aar")
}
& "$bt\aapt.exe" package -f -m -J "$root\build\generated" -M "$root\AndroidManifest.xml" -S "$root\res" -A "$root\assets" -I $platform -0 onnx -0 bin -F "$root\build\resources.apk"
CheckExit
$sources=@(Get-ChildItem "$root\src","$root\build\generated" -Recurse -Filter *.java | ForEach-Object {$_.FullName})
$cp="$platform;$root\tools\aar\classes.jar;$root\tools\kotlin-stdlib.jar"
& "$JavaHome\bin\javac.exe" --release 8 -encoding UTF-8 -cp $cp -d "$root\build\classes" @sources
CheckExit
& "$JavaHome\bin\jar.exe" cf "$root\build\app.jar" -C "$root\build\classes" .
CheckExit
& "$bt\d8.bat" --min-api 26 --lib $platform --output "$root\build\dex" "$root\build\app.jar" "$root\tools\aar\classes.jar" "$root\tools\kotlin-stdlib.jar"
CheckExit
Copy-Item "$root\build\resources.apk" "$root\build\unsigned.apk" -Force
$zip=[IO.Compression.ZipFile]::Open("$root\build\unsigned.apk",[IO.Compression.ZipArchiveMode]::Update)
try{
  Get-ChildItem "$root\build\dex" -Filter *.dex | ForEach-Object {[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$_.FullName,$_.Name,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}
  foreach($abi in @("arm64-v8a","x86_64")){
    Get-ChildItem "$root\tools\aar\jni\$abi" -Filter *.so | ForEach-Object {[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$_.FullName,"lib/$abi/$($_.Name)",[IO.Compression.CompressionLevel]::NoCompression)|Out-Null}
  }
}finally{$zip.Dispose()}
& "$bt\zipalign.exe" -f -P 16 4 "$root\build\unsigned.apk" "$root\build\aligned.apk"
CheckExit
& "$bt\apksigner.bat" sign --ks $KeyStore --ks-key-alias winintel --ks-pass "file:$PasswordFile" --out $Output "$root\build\aligned.apk"
CheckExit
& "$bt\apksigner.bat" verify --verbose --print-certs $Output
CheckExit
& "$bt\zipalign.exe" -c -P 16 4 $Output
CheckExit
Get-FileHash $Output -Algorithm SHA256
