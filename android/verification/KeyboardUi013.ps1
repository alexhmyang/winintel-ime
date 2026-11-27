param([string]$Build="F:\Temp\winintel-ime-android-013-final",[string]$Output="C:\Users\86187\Documents\Trae\default")
$ErrorActionPreference="Stop"
$adb="F:\Temp\winintel-ime-android\tools\platform-tools\adb.exe"
$script:rows=@("| Android 模拟器界面检查 | 结果 |","|---|---|")
function Snapshot {
 & $adb shell uiautomator dump --windows /sdcard/ui013.xml | Out-Null
 $pull=Start-Process $adb -ArgumentList @("pull","/sdcard/ui013.xml","$Build\ui.xml") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Build\adb-output.log" -RedirectStandardError "$Build\adb-error.log"
 if($pull.ExitCode){throw "UI snapshot download failed"}
 [xml]$script:doc=[IO.File]::ReadAllText("$Build\ui.xml",[Text.Encoding]::UTF8)
}
function Find([string]$label){
 foreach($node in $script:doc.SelectNodes("//node")){if($node.GetAttribute("text") -eq $label){return $node}}
 throw "Control missing: $label"
}
function Tap([string]$label){
 Snapshot;$node=Find $label;$b=[regex]::Matches($node.GetAttribute("bounds"),"\d+");$x=([int]$b[0].Value+[int]$b[2].Value)/2;$y=([int]$b[1].Value+[int]$b[3].Value)/2
 & $adb shell input tap ([int]$x) ([int]$y) | Out-Null
}
function Text {
 Snapshot;foreach($node in $script:doc.SelectNodes("//node")){if($node.GetAttribute("class") -eq "android.widget.EditText"){return $node.GetAttribute("text")}}
 throw "No editor"
}
function Check([bool]$ok,[string]$name){if(!$ok){throw "Failed: $name"};$script:rows+="| $name | 通过 |"}
function Capture([string]$name){& $adb shell screencap -p "/sdcard/$name.png";$pull=Start-Process $adb -ArgumentList @("pull","/sdcard/$name.png","$Output\$name.png") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Build\adb-output.log" -RedirectStandardError "$Build\adb-error.log";if($pull.ExitCode){throw "Screenshot download failed"}}
& $adb shell am force-stop com.winintel.ime | Out-Null
& $adb shell am force-stop com.winintel.ime.check | Out-Null
& $adb shell am start -n com.winintel.ime.check/.KeyboardActivity | Out-Null
& $adb shell ime enable com.winintel.ime/.ImeService | Out-Null
& $adb shell ime set com.winintel.ime/.ImeService | Out-Null
Tap "布局";Tap "拼音九宫格"
foreach($key in @("6`nMNO","4`nGHI","4`nGHI","2`nABC","6`nMNO")){Tap $key}
Snapshot;Check (@($doc.SelectNodes("//node") | Where-Object {$_.GetAttribute("text") -eq "你好"}).Count -gt 0) "九宫格 64426 候选"
Capture "winintel-ime-android-0.1.3-nine"
Tap "你好";Check ((Text) -eq "你好") "九宫格选词上屏"
Tap "123";Tap "1";Tap "2";Tap "3";Check ((Text) -eq "你好123") "独立数字九宫格"
Capture "winintel-ime-android-0.1.3-numbers"
Tap "返回";Tap "符号";Tap "，";Tap "数学";Tap "≥";Check ((Text) -eq "你好123，≥") "中文与数学符号"
Capture "winintel-ime-android-0.1.3-symbols"
Tap "返回";Tap "布局";Tap "拼音全键盘"
foreach($key in @("n","i","h","a","o")){Tap $key};Tap "你好";Check ((Text) -eq "你好123，≥你好") "全键盘中文输入"
Capture "winintel-ime-android-0.1.3-qwerty"
Tap "布局";Tap "英文全键盘";foreach($key in @("h","e","l")){Tap $key};Tap "hello"
Check ((Text) -eq "你好123，≥你好hello") "英文补全保留原文"
Tap "发送";Snapshot;Check (@($doc.SelectNodes("//node") | Where-Object {$_.GetAttribute("text") -eq "ACTION_4"}).Count -gt 0) "聊天发送键调用宿主动作"
Tap "⌫";Check ((Text) -eq "你好123，≥你好hell") "删除已上屏英文"
Tap "NUMBER";Tap "1";Tap "2";Tap "0";Check ((Text) -eq "120") "数字输入框自动数字页"
Tap "PASSWORD";Tap "语音";Snapshot;Check (@($doc.SelectNodes("//node") | Where-Object {$_.GetAttribute("text") -eq "密码输入框不启用语音"}).Count -gt 0) "密码框禁用语音"
Tap "CHAT";Tap "布局";Tap "拼音九宫格"
$script:rows | Set-Content "$Output\winintel-ime-android-0.1.3-ui-results.md" -Encoding UTF8
$script:rows
