param([string]$Output)
$ErrorActionPreference="Stop"
if(!$Output){throw "Supply an output ZIP path"}
$root=$PSScriptRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open($Output,[IO.Compression.ZipArchiveMode]::Create)
try{
 $files=@()
 foreach($folder in @("src","res","licenses","verification","assets","data","scripts")){
  $files+=Get-ChildItem "$root\$folder" -File -Recurse | Where-Object {$_.Name -ne "model.int8.onnx"}
 }
 $files+=Get-Item "$root\Build.ps1","$root\README.md","$root\LICENSE","$root\DictionaryCheck.java","$root\PackSource.ps1"
 $files+=Get-ChildItem "$root\tools" -File -Filter *.ps1
 foreach($file in $files){
  $relative=$file.FullName.Substring($root.Length+1).Replace('\','/')
  [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal)|Out-Null
 }
}finally{$zip.Dispose()}
Get-FileHash $Output -Algorithm SHA256
