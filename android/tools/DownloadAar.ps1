$ErrorActionPreference="Stop"
$target=Join-Path $PSScriptRoot "sherpa-onnx-2.0.3.aar"
$jobs=@()
$size=52411426
$chunk=[int][Math]::Ceiling($size/16)
for($i=0;$i -lt 16;$i++){
  $begin=$i*$chunk;$end=[Math]::Min($size-1,$begin+$chunk-1)
  $psi=New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName=(Get-Command gh).Source
  $psi.Arguments="api repos/k2-fsa/sherpa-onnx/releases/assets/778921597 -H `"Accept: application/octet-stream`" -H `"Range: bytes=$begin-$end`""
  $psi.UseShellExecute=$false;$psi.RedirectStandardOutput=$true
  $process=New-Object System.Diagnostics.Process;$process.StartInfo=$psi;$process.Start()|Out-Null
  $file=[IO.File]::Create("$target.part$i")
  $task=$process.StandardOutput.BaseStream.CopyToAsync($file)
  $jobs+=@{process=$process;file=$file;task=$task;length=$end-$begin+1;path="$target.part$i"}
}
foreach($job in $jobs){$job.task.GetAwaiter().GetResult();$job.file.Dispose();$job.process.WaitForExit();if($job.process.ExitCode -ne 0 -or (Get-Item $job.path).Length -ne $job.length){throw "gh part download failed: $($job.path)"}}
$file=[IO.File]::Create($target)
try{foreach($job in $jobs){$part=[IO.File]::OpenRead($job.path);try{$part.CopyTo($file)}finally{$part.Dispose()}}}finally{$file.Dispose()}
if((Get-FileHash $target).Hash.ToLowerInvariant() -ne "4c766e2e74c9d33d595338c21fe3b305c73ba45d47e96a235675c33142a2ec50"){throw "AAR checksum mismatch"}
