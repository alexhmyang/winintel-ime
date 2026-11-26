param(
 [string]$Work="F:\Temp\winintel-ime-video054",
 [string]$Output=(Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent),
 [string]$FFmpeg="F:\Temp\winintel-ime-video\package\ffmpeg.exe"
)
$ErrorActionPreference="Stop"
if(!(Test-Path "$Work\complete.txt")){throw "Actual recording is incomplete"}
$scenes=Get-Content "$Work\scenes.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$total=[double]::Parse((Get-Content "$Work\duration.txt"),[Globalization.CultureInfo]::InvariantCulture)
Add-Type -AssemblyName System.Speech
$synth=New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.SelectVoice("Microsoft Huihui Desktop");$synth.Rate=1
function Wav-Length([string]$path){
 $reader=New-Object IO.BinaryReader([IO.File]::OpenRead($path))
 try{
  $reader.BaseStream.Position=12;$rate=0
  while($reader.BaseStream.Position+8 -le $reader.BaseStream.Length){
   $tag=[Text.Encoding]::ASCII.GetString($reader.ReadBytes(4));$size=$reader.ReadUInt32();$start=$reader.BaseStream.Position
   if($tag -eq "fmt "){$reader.BaseStream.Position=$start+8;$rate=$reader.ReadUInt32()}
   if($tag -eq "data"){return $size/[double]$rate}
   $reader.BaseStream.Position=$start+$size+($size%2)
  }
 }finally{$reader.Dispose()}
}
function Srt-Time([double]$sec){$t=[TimeSpan]::FromMilliseconds([Math]::Round($sec*1000));return "{0:00}:{1:00}:{2:00},{3:000}" -f [int][Math]::Floor($t.TotalHours),$t.Minutes,$t.Seconds,$t.Milliseconds}
function Ass-Time([double]$sec){$t=[TimeSpan]::FromMilliseconds([Math]::Round($sec*1000));return "{0}:{1:00}:{2:00}.{3:00}" -f [int][Math]::Floor($t.TotalHours),$t.Minutes,$t.Seconds,[int][Math]::Floor($t.Milliseconds/10)}
$ass=@"
[Script Info]
ScriptType: v4.00+
PlayResX: 1536
PlayResY: 864
WrapStyle: 0
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Default,Microsoft YaHei,27,&H00FFFFFF,&H00FFFFFF,&H700C1423,&H700C1423,0,0,0,0,100,100,0,0,3,10,0,2,65,65,22,1
Style: Advantage,Microsoft YaHei,22,&H00BCD245,&H00FFFFFF,&H000C1423,&H000C1423,-1,0,0,0,100,100,0,0,1,1,0,7,0,0,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"@
$ass+="`n";$srt="";$cue=0
$argsList=New-Object 'System.Collections.Generic.List[string]'
$filters=New-Object 'System.Collections.Generic.List[string]'
$mix=New-Object 'System.Collections.Generic.List[string]'
$argsList.Add("-y");$argsList.Add("-i");$argsList.Add("$Work\screen-raw.mp4")
$highlights=@("全离线  ·  无付费 API","本地判断  ·  中英候选","单按 Shift  ·  即时切换","实时译文  +  整句复核","双语字幕  ·  分段时间戳","数据留在本机","快速切换  ·  实时翻译")
for($i=0;$i -lt $scenes.Count;$i++){
 $wav="$Work\narration-$i.wav"
 $synth.SetOutputToWaveFile($wav);$synth.Speak($scenes[$i].narration.Replace("数萌 AI","数萌 A I").Replace("Shift","希夫特").Replace("hello","哈喽").Replace("github","Git Hub"));$synth.SetOutputToNull()
 $length=Wav-Length $wav;$start=[double]$scenes[$i].time+0.25
 $end=if($i+1 -lt $scenes.Count){[double]$scenes[$i+1].time-0.25}else{$total-0.4}
 $speed=[Math]::Max(1.0,$length/($end-$start));if($speed -gt 2){throw "Narration is too long"}
 $tempo=$speed.ToString("0.000",[Globalization.CultureInfo]::InvariantCulture);$delay=[int][Math]::Round($start*1000);$index=$i+1
 $argsList.Add("-i");$argsList.Add($wav);$filters.Add("[$index`:a]atempo=$tempo,aresample=48000,aformat=channel_layouts=stereo,adelay=$delay`|$delay[v$i]");$mix.Add("[v$i]")
 $pieces=[regex]::Matches($scenes[$i].narration,'[^。！？]+[。！？]?') | ForEach-Object {$_.Value};$weight=($pieces | ForEach-Object {$_.Length} | Measure-Object -Sum).Sum;$cursor=$start
 foreach($piece in $pieces){$next=$cursor+$length/$speed*$piece.Length/$weight;$cue++;$srt+="$cue`r`n$(Srt-Time $cursor) --> $(Srt-Time $next)`r`n$piece`r`n`r`n";$ass+="Dialogue: 0,$(Ass-Time $cursor),$(Ass-Time $next),Default,,0,0,0,,$piece`n";$cursor=$next}
 $ass+="Dialogue: 1,$(Ass-Time $start),$(Ass-Time $end),Advantage,,0,0,0,,{\pos(58,575)\fad(200,200)}$($highlights[$i])`n"
}
$synth.Dispose();$duration=$total.ToString("0.000",[Globalization.CultureInfo]::InvariantCulture);$music=$scenes.Count+1
$argsList.Add("-f");$argsList.Add("lavfi");$argsList.Add("-i");$argsList.Add("aevalsrc=0.008*(sin(2*PI*130.81*t)+0.5*sin(2*PI*196*t)+0.25*sin(2*PI*261.63*t))*(0.75+0.25*sin(0.7*t)):s=48000:d=$duration")
$fade=([Math]::Max(0,$total-4)).ToString("0.000",[Globalization.CultureInfo]::InvariantCulture)
$filters.Add("[$music`:a]aformat=channel_layouts=stereo,afade=t=in:d=2,afade=t=out:st=$fade`:d=4[pad]");$mix.Add("[pad]");$count=$mix.Count
$filters.Add("$($mix -join '')amix=inputs=$count`:duration=longest:dropout_transition=0,volume=$count,loudnorm=I=-16:TP=-1.5:LRA=9[audio]")
$assPath=("$Work/subtitles.ass").Replace("\","/").Replace(":","\:")
$endFade=([Math]::Max(0,$total-0.7)).ToString("0.000",[Globalization.CultureInfo]::InvariantCulture)
$filters.Add("[0:v]trim=duration=$duration,setpts=PTS-STARTPTS,ass='$assPath',scale=1920:1080:flags=lanczos,fade=t=in:d=0.4,fade=t=out:st=$endFade`:d=0.7[video]")
[IO.File]::WriteAllText("$Work\subtitles.ass",$ass,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText("$Output\数萌AI输入法-0.5.4-实时翻译与中英快切.srt",$srt,[Text.UTF8Encoding]::new($true))
[IO.File]::WriteAllText("$Work\edit-filter.txt",($filters -join ";"),[Text.UTF8Encoding]::new($false))
$argsList.Add("-filter_complex_script");$argsList.Add("$Work\edit-filter.txt");$argsList.Add("-map");$argsList.Add("[video]");$argsList.Add("-map");$argsList.Add("[audio]");$argsList.Add("-t");$argsList.Add($duration)
$argsList.Add("-c:v");$argsList.Add("libx264");$argsList.Add("-preset");$argsList.Add("medium");$argsList.Add("-crf");$argsList.Add("19");$argsList.Add("-pix_fmt");$argsList.Add("yuv420p");$argsList.Add("-c:a");$argsList.Add("aac");$argsList.Add("-b:a");$argsList.Add("192k");$argsList.Add("-ar");$argsList.Add("48000");$argsList.Add("-movflags");$argsList.Add("+faststart")
$argsList.Add("-metadata");$argsList.Add("title=数萌 AI 输入法 0.5.4 实时翻译与中英快切");$argsList.Add("-metadata");$argsList.Add("artist=数萌AI服务器 (winintel)");$argsList.Add("$Output\数萌AI输入法-0.5.4-实时翻译与中英快切.mp4")
$ErrorActionPreference="Continue";& $FFmpeg @argsList 2> "$Work\edit-log.txt";$ErrorActionPreference="Stop";if($LASTEXITCODE){throw "Final movie encoding failed"}
Write-Output "Narrated product demo finished: $duration seconds, $cue captions"
