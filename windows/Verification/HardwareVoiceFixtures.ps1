param([Parameter(Mandatory=$true)][string]$Output,[string]$FixtureFile=(Join-Path $PSScriptRoot 'hardware-fixtures.json'))
$ErrorActionPreference='Stop'
if(!(Test-Path $Output)){throw 'Output directory must exist'}
Add-Type -AssemblyName System.Speech
$speech=New-Object System.Speech.Synthesis.SpeechSynthesizer
$speech.SelectVoice('Microsoft Huihui Desktop')
$format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo 16000,([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen),([System.Speech.AudioFormat.AudioChannel]::Mono)
$fixtures=[IO.File]::ReadAllText($FixtureFile,[Text.Encoding]::UTF8) | ConvertFrom-Json
$texts=@($fixtures | ForEach-Object {$_.text})
$expected=@($fixtures | ForEach-Object {$_.expected})
for($i=0;$i -lt $texts.Length;$i++){
 $speech.SetOutputToWaveFile("$Output\tts-$i.wav",$format);$speech.Speak($texts[$i]);$speech.SetOutputToNull()
}
[IO.File]::WriteAllText("$Output\fixtures.json",(ConvertTo-Json $expected),[Text.Encoding]::UTF8)
$speech.Dispose()
