param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
if(!(Test-Path $Output)){throw 'Output directory must exist'}
Add-Type -AssemblyName System.Speech
$speech=New-Object System.Speech.Synthesis.SpeechSynthesizer
$speech.SelectVoice('Microsoft Huihui Desktop')
$format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo 16000,([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen),([System.Speech.AudioFormat.AudioChannel]::Mono)
$texts=@('数萌AI服务器','今天下午三点开会，请带上测试报告。','输入法指令，清空。','输入法指令，撤销。','输入法指令，删除上一句。','输入法指令，把明天改成今天。','请不要清空这些文字。')
for($i=0;$i -lt $texts.Length;$i++){
    $speech.SetOutputToWaveFile("$Output\tts-$i.wav",$format);$speech.Speak($texts[$i]);$speech.SetOutputToNull()
}
[IO.File]::WriteAllText("$Output\fixtures.json",(ConvertTo-Json $texts),[Text.Encoding]::UTF8)
$speech.Dispose()
