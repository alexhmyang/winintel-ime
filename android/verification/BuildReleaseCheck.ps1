param(
  [string]$JavaHome=$env:JAVA_HOME,
  [string]$ToolRoot=(Join-Path $env:LOCALAPPDATA "winintel-build\android-tools"),
  [Parameter(Mandatory=$true)][string]$AppBuild,
  [Parameter(Mandatory=$true)][string]$KeyStore,
  [Parameter(Mandatory=$true)][string]$PasswordFile,
  [string]$Output=(Join-Path $env:TEMP "winintel-ime-release-android-check")
)
$ErrorActionPreference="Stop"
$env:JAVA_HOME=$JavaHome;$env:PATH="$JavaHome\bin;$env:PATH"
$bt="$ToolRoot\build-tools\android-15";$platform="$ToolRoot\platform\android-35\android.jar"
New-Item -ItemType Directory -Force "$Output\classes","$Output\dex" | Out-Null
function CheckExit([string]$stage){if($LASTEXITCODE){throw "$stage failed: $LASTEXITCODE"}}
& "$bt\aapt.exe" package -f -M "$PSScriptRoot\AndroidManifest.xml" -A "$PSScriptRoot\assets" -I $platform -F "$Output\resources.apk"
CheckExit "aapt"
$sources=@(Get-ChildItem "$PSScriptRoot\src" -Recurse -Filter *.java | ForEach-Object {$_.FullName})
& "$JavaHome\bin\javac.exe" --release 8 -encoding UTF-8 -cp "$platform;$AppBuild\app.jar;$AppBuild\aar\classes.jar;$ToolRoot\kotlin-stdlib.jar" -d "$Output\classes" @sources
CheckExit "javac"
& "$JavaHome\bin\jar.exe" cf "$Output\check.jar" -C "$Output\classes" .
CheckExit "jar"
& "$bt\d8.bat" --min-api 26 --lib $platform --classpath "$AppBuild\app.jar" --output "$Output\dex" "$Output\check.jar"
CheckExit "d8"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open("$Output\resources.apk",[IO.Compression.ZipArchiveMode]::Update)
try{[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,"$Output\dex\classes.dex","classes.dex") | Out-Null}finally{$zip.Dispose()}
& "$bt\zipalign.exe" -f -P 16 4 "$Output\resources.apk" "$Output\aligned.apk"
CheckExit "zipalign"
& "$bt\apksigner.bat" sign --ks $KeyStore --ks-key-alias winintel --ks-pass "file:$PasswordFile" --out "$Output\check.apk" "$Output\aligned.apk"
CheckExit "sign"
Write-Output "Instrumented check APK: $Output\check.apk"
