param([string]$Work="F:\Temp\winintel-ime-android-014",[string]$Output="C:\Users\86187\Documents\Trae\default")
$ErrorActionPreference="Stop"
$adb="F:\Temp\winintel-ime-android\tools\platform-tools\adb.exe"
$script:rows=@("| 手机编辑检查 | 结果 |","|---|---|")
function Snapshot {
 & $adb shell uiautomator dump --windows /sdcard/ui014.xml | Out-Null
 $p=Start-Process $adb -ArgumentList @("pull","/sdcard/ui014.xml","$Work\ui.xml") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Work\adb-out.log" -RedirectStandardError "$Work\adb-err.log"
 if($p.ExitCode){throw "Snapshot download"}
 [xml]$script:doc=[IO.File]::ReadAllText("$Work\ui.xml",[Text.Encoding]::UTF8)
}
function Find([string]$label){
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
function Check([bool]$ok,[string]$name){if(!$ok){throw "Failed: $name"};$script:rows+="| $name | 通过 |";$script:rows | Set-Content "$Output\winintel-ime-0.5.10-0.1.4-编辑检查.md" -Encoding UTF8}
function Capture([string]$name){
 & $adb shell screencap -p "/sdcard/$name.png"
 $p=Start-Process $adb -ArgumentList @("pull","/sdcard/$name.png","$Output\$name.png") -Wait -NoNewWindow -PassThru -RedirectStandardOutput "$Work\adb-out.log" -RedirectStandardError "$Work\adb-err.log"
 if($p.ExitCode){throw "Screenshot download"}
}
function Chinese {foreach($key in @("n","i","h","a","o")){Tap $key};Tap "你好"}
& $adb shell am force-stop com.winintel.ime | Out-Null
& $adb shell am start -n com.winintel.ime.check/.KeyboardActivity | Out-Null
& $adb shell ime enable com.winintel.ime/.ImeService | Out-Null
& $adb shell ime set com.winintel.ime/.ImeService | Out-Null
Tap "CHAT";Tap "布局";Tap "拼音全键盘"
Snapshot;$del=Find "⌫";$q=Find "q";$d=[regex]::Matches($del.GetAttribute("bounds"),"\d+");$a=[regex]::Matches($q.GetAttribute("bounds"),"\d+")
Check ($d[1].Value -eq $a[1].Value -and [int]$d[0].Value -gt [int]$a[0].Value) "全键盘删除键位于右上角"
Tap "MID";Chinese;Check ((Text) -eq "前你好后") "句中中文插入"
Tap "SELECT";Chinese;Check ((Text) -eq "前你好后") "中文替换选区"
Tap "SELECT";Tap "⌫";Check ((Text) -eq "前后") "删除选区"
Tap "EMOJI";Tap "⌫";Check ((Text) -eq "前后") "一次删除整个 Emoji"
Tap "布局";Tap "英文全键盘";Tap "MID";foreach($key in @("h","e","l")){Tap $key};Tap "hello"
Check ((Text) -eq "前hello后") "英文补全插入光标位置"
Tap "MID";foreach($key in @("h","e","l")){Tap $key};Tap "MID";foreach($key in @("w","o","r","l","d")){Tap $key};Tap "world"
Check ((Text) -eq "前world后") "外部移动光标清理旧英文组合区"
Tap "布局";Tap "拼音九宫格";Tap "MID";Tap "编辑";Tap "→";Tap "，"
Check ((Text) -eq "前后，") "精确右移后插入符号"
Tap "选择";Tap "←";Tap "。"
Check ((Text) -eq "前后。") "编辑面板扩展选区并替换"
Capture "winintel-ime-android-0.1.4-edit"
Tap "符号";Snapshot;$del=Find "⌫";$tab=Find "中文";$d=[regex]::Matches($del.GetAttribute("bounds"),"\d+");$a=[regex]::Matches($tab.GetAttribute("bounds"),"\d+")
Check ($d[1].Value -eq $a[1].Value) "符号页删除键位于右上角"
Capture "winintel-ime-android-0.1.4-symbols"
Tap "返回";Tap "123";Tap "1";Tap "2";Check ((Text) -eq "前后。12") "数字输入保留编辑位置"
Tap "返回";Tap "布局";Tap "拼音九宫格"
$script:rows
