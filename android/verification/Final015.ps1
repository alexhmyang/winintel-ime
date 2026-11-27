param([string]$Work="F:\Temp\winintel-ime-android-015",[string]$Output="C:\Users\86187\Documents\Trae\default")
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
$source=[IO.File]::ReadAllText("$PSScriptRoot/KeyboardUi015.ps1",[Text.Encoding]::UTF8)
$end=$source.IndexOf('& $adb shell am start')
if($end -lt 0){throw "UI helpers missing"}
. ([scriptblock]::Create($source.Substring(0,$end)))
$script:rows=@(Get-Content "$Output/winintel-ime-0.1.5-手机交互检查.md" -Encoding UTF8)
& $adb install -r "$Output/winintel-ime-android-0.1.5.apk"
if($LASTEXITCODE){throw "Final APK installation failed"}
& $adb install -r "F:/Temp/winintel-ime-android-015-check-final2/check.apk"
if($LASTEXITCODE){throw "Final test APK installation failed"}
$checks=& $adb shell am instrument -w com.winintel.ime.check/.CaretCheck
$checks | Set-Content "$Work/caret-check-final.txt" -Encoding UTF8
if(($checks -join "`n") -notmatch 'result=PASS'){throw "Final caret checks failed: $checks"}
& $adb shell am start -n com.winintel.ime.check/.KeyboardActivity | Out-Null
& $adb shell ime set com.winintel.ime/.ImeService | Out-Null
Tap "CHAT";Tap "布局";Tap "拼音全键盘";Tap "CORRECT";Tap "j";Tap "i"
for($i=0;$i -lt 10;$i++){Snapshot;try{Find "集" | Out-Null;break}catch{Tap "›"}}
Tap "集";Check ((Text) -eq "数萌算力集群") "实际应用选区纠正组成新词"
function TypeFast([string]$pinyin){
 Tap ($pinyin.Substring(0,1));Snapshot
 $commands=@()
 foreach($ch in $pinyin.Substring(1).ToCharArray()){
  $node=Find ([string]$ch);$b=[regex]::Matches($node.GetAttribute("bounds"),"\d+")
  $x=[int](([int]$b[0].Value+[int]$b[2].Value)/2);$y=[int](([int]$b[1].Value+[int]$b[3].Value)/2)
  $commands+="input tap $x $y; sleep 0.15"
 }
 & $adb shell ($commands -join "; ") | Out-Null
 Snapshot
 if($null -eq $script:doc.SelectSingleNode("//node[@text='$pinyin']")){
  Tap "CHAT";foreach($ch in $pinyin.ToCharArray()){Tap ([string]$ch)};Snapshot
 }
}
Tap "CHAT";TypeFast "shumengsuanlijiqun"
Check ($null -ne $script:doc.SelectSingleNode("//node[@text='数萌算力集群']")) "纠正后完整长词可查"
Tap "数萌算力集群"
& $adb shell am force-stop com.winintel.ime | Out-Null
& $adb shell ime set com.winintel.ime/.ImeService | Out-Null
Tap "CHAT";TypeFast "shumengsuanlijiqun"
Check ($null -ne $script:doc.SelectSingleNode("//node[@text='数萌算力集群']")) "输入法进程重启后保留纠正词"
& $adb shell screencap -p /sdcard/final015.png
$p=Start-Process $adb -ArgumentList @("pull","/sdcard/final015.png","$Output/winintel-ime-android-0.1.5-green.png") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Work/adb-out.log" -RedirectStandardError "$Work/adb-err.log"
if($p.ExitCode){throw "Final screenshot download failed"}
Tap "数萌算力集群";Check ((Text) -eq "数萌算力集群") "最终APK记词后连续上屏"
$script:rows
