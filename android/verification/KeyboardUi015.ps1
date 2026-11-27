param([string]$Work="F:\Temp\winintel-ime-android-015",[string]$Output="C:\Users\86187\Documents\Trae\default")
# SPDX-License-Identifier: GPL-3.0-or-later
# SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
$ErrorActionPreference="Stop"
$adb="F:\Temp\winintel-ime-android\tools\platform-tools\adb.exe"
$script:rows=@("| 手机交互检查 | 结果 |","|---|---|")
function Snapshot {
 & $adb shell uiautomator dump --windows /sdcard/ui015.xml | Out-Null
 $p=Start-Process $adb -ArgumentList @("pull","/sdcard/ui015.xml","$Work/ui015.xml") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Work/adb-out.log" -RedirectStandardError "$Work/adb-err.log"
 if($p.ExitCode){throw "Snapshot download failed"}
 [xml]$script:doc=[IO.File]::ReadAllText("$Work/ui015.xml",[Text.Encoding]::UTF8)
}
function Find([string]$label){
 $letter=$null
 foreach($node in $script:doc.SelectNodes("//node")){
  if($node.GetAttribute("class") -eq "android.widget.Button" -and $node.GetAttribute("text") -eq $label){
   if($label -match '^[a-z]$'){$letter=$node}else{return $node}
  }
 }
 if($null -ne $letter){return $letter}
 foreach($node in $script:doc.SelectNodes("//node")){if($node.GetAttribute("text") -eq $label){return $node}}
 throw "Missing: $label"
}
function Tap([string]$label){
 Snapshot;$node=Find $label;$b=[regex]::Matches($node.GetAttribute("bounds"),"\d+")
 & $adb shell input tap ([int](([int]$b[0].Value+[int]$b[2].Value)/2)) ([int](([int]$b[1].Value+[int]$b[3].Value)/2)) | Out-Null
}
function Text {
 Snapshot;foreach($node in $script:doc.SelectNodes("//node")){if($node.GetAttribute("class") -eq "android.widget.EditText"){return $node.GetAttribute("text")}}
 throw "Editor missing"
}
function Check([bool]$ok,[string]$name){if(!$ok){throw "Failed: $name"};$script:rows+="| $name | 通过 |";$script:rows | Set-Content "$Output/winintel-ime-0.1.5-手机交互检查.md" -Encoding UTF8}
function Chinese {foreach($key in @("n","i","h","a","o")){Tap $key};Tap "你好"}
& $adb shell am start -n com.winintel.ime.check/.KeyboardActivity | Out-Null
& $adb shell ime enable com.winintel.ime/.ImeService | Out-Null
& $adb shell ime set com.winintel.ime/.ImeService | Out-Null
Tap "CHAT";Tap "布局";Tap "拼音全键盘";Snapshot
Check ($null -eq $script:doc.SelectSingleNode("//node[@text='编辑' or @text='编辑草稿' or @text='发送']")) "移除编辑和发送按钮"
Tap "AUTO_RESTART";Chinese;Chinese;Chinese
Check ((Text) -eq "你好你好你好") "每次上屏重启输入连接，连续输入六字"
Tap "n";Tap "i";Tap "RESTART_NOW";Tap "h";Tap "a";Tap "o";Tap "你好"
Check ((Text) -eq "你好你好你好你好") "拼音输入中重启连接保持待选文字"
Tap "换行";Snapshot
Check ($null -eq $script:doc.SelectSingleNode("//node[starts-with(@text,'ACTION_')]")) "换行不触发聊天发送或完成动作"
Check ($null -ne $script:doc.SelectSingleNode("//node[@text='布局']")) "确认候选和换行后键盘保持激活"
Tap "AUTO_RESTART";Tap "MID";Chinese;Check ((Text) -eq "前你好后") "直接句中插入中文"
Tap "SELECT";Chinese;Check ((Text) -eq "前你好后") "直接替换应用选区"
Tap "EMOJI";Tap "⌫";Check ((Text) -eq "前后") "删除完整 Emoji"
Tap "CHAT";foreach($key in @("r","e","n","g","o","n","g","z","h","i","n","e","n","g")){Tap $key}
Snapshot;Check ($null -ne $script:doc.SelectSingleNode("//node[@text='人工智能']")) "四字词人工智能候选"
& $adb shell screencap -p /sdcard/green015.png
$p=Start-Process $adb -ArgumentList @("pull","/sdcard/green015.png","$Output/winintel-ime-android-0.1.5-green.png") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Work/adb-out.log" -RedirectStandardError "$Work/adb-err.log"
if($p.ExitCode){throw "Screenshot download failed"}
Tap "人工智能";Tap "布局";Tap "拼音九宫格"
Tap "CHAT";foreach($key in @("9`nWXYZ","4`nGHI","4`nGHI","6`nMNO","3`nDEF","6`nMNO","4`nGHI","8`nTUV","4`nGHI")){Tap $key}
Snapshot;Check ($null -ne $script:doc.SelectSingleNode("//node[@text='智能体']")) "九宫格精选新词智能体"
Tap "智能体"
$script:rows
