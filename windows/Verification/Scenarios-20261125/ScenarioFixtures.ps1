param([string]$BenchmarkRoot="F:\Temp\winintel-ime-model-upgrade")
$ErrorActionPreference="Stop"
Add-Type -AssemblyName System.Speech
$root=$BenchmarkRoot
$folder="$root\scenario-audio"
New-Item -ItemType Directory -Force $folder | Out-Null
$samples=Get-Content "$root\scenarios.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$synth=New-Object System.Speech.Synthesis.SpeechSynthesizer
$format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo 16000,([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen),([System.Speech.AudioFormat.AudioChannel]::Mono)
$rows=@()
for($i=0;$i -lt $samples.Count;$i++){
  foreach($lang in @("zh","en")){
    $voice=if($lang -eq "zh"){"Microsoft Huihui Desktop"}else{"Microsoft Zira Desktop"}
    $synth.SelectVoice($voice)
    $synth.Rate=0
    $file="$folder\$i-$lang.wav"
    $synth.SetOutputToWaveFile($file,$format)
    $synth.Speak([string]$samples[$i].$lang)
    $synth.SetOutputToNull()
    $rows+=@{id="$i-$lang";group=$samples[$i].group;lang=$lang;text=$samples[$i].$lang;file=$file;condition="clean"}
  }
}
$synth.Dispose()
$rows | ConvertTo-Json -Depth 5 | Set-Content "$folder\fixtures.json" -Encoding UTF8
Write-Output "Generated $($rows.Count) offline synthetic speech samples"
