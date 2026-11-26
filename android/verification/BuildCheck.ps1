param([string]$JavaHome="C:\Program Files\Java\jdk-20",[string]$KeyStore,[string]$PasswordFile)
$ErrorActionPreference="Stop"
$root=Split-Path $PSScriptRoot
$env:JAVA_HOME=$JavaHome;$env:PATH="$JavaHome\bin;$env:PATH"
$bt="$root\tools\build-tools\android-15";$platform="$root\tools\platform\android-35\android.jar"
New-Item -ItemType Directory -Force "$root\build\check-classes","$root\build\check-dex" | Out-Null
& "$bt\aapt.exe" package -f -M "$PSScriptRoot\AndroidManifest.xml" -A "$PSScriptRoot\assets" -I $platform -F "$root\build\check.apk"
if($LASTEXITCODE -ne 0){throw "aapt failed"}
$sources=@(Get-ChildItem "$PSScriptRoot\src" -Recurse -Filter *.java | ForEach-Object {$_.FullName})
& "$JavaHome\bin\javac.exe" --release 8 -encoding UTF-8 -cp "$platform;$root\build\app.jar;$root\tools\aar\classes.jar;$root\tools\kotlin-stdlib.jar" -d "$root\build\check-classes" @sources
if($LASTEXITCODE -ne 0){throw "javac failed"}
& "$JavaHome\bin\jar.exe" cf "$root\build\check.jar" -C "$root\build\check-classes" .
& "$bt\d8.bat" --min-api 26 --lib $platform --classpath "$root\build\app.jar" --output "$root\build\check-dex" "$root\build\check.jar"
if($LASTEXITCODE -ne 0){throw "d8 failed"}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open("$root\build\check.apk",[IO.Compression.ZipArchiveMode]::Update)
try{[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,"$root\build\check-dex\classes.dex","classes.dex")|Out-Null}finally{$zip.Dispose()}
& "$bt\apksigner.bat" sign --ks $KeyStore --ks-key-alias winintel --ks-pass "file:$PasswordFile" "$root\build\check.apk"
if($LASTEXITCODE -ne 0){throw "sign failed"}
