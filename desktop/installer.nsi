Unicode true
!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "Sections.nsh"
!ifndef PAYLOAD
!error "Specify /DPAYLOAD=directory"
!endif
!ifndef OUTPUT
!define OUTPUT "winintel-AI-Input-Setup-0.1.0.exe"
!endif
Name "数萌AI输入法"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\winintel-ime"
InstallDirRegKey HKCU "Software\winintel\IME" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
VIProductVersion "0.1.0.0"
VIAddVersionKey /LANG=2052 "ProductName" "数萌AI输入法"
VIAddVersionKey /LANG=2052 "CompanyName" "winintel AI"
VIAddVersionKey /LANG=2052 "FileDescription" "数萌AI输入法安装程序"
VIAddVersionKey /LANG=2052 "FileVersion" "0.1.0"
VIAddVersionKey /LANG=2052 "LegalCopyright" "2026 winintel. GPL-3.0-or-later"
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PAYLOAD}\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\winintel-ime.exe"
!define MUI_FINISHPAGE_RUN_TEXT "启动数萌AI输入法"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
Var Options
Var Result
Section "数萌托盘、本地词库和语音模型（必需）" Core
  SectionIn RO
  IfFileExists "$INSTDIR\winintel-ime.exe" 0 +2
    ExecWait '"$INSTDIR\winintel-ime.exe" /quit'
  SetOutPath "$INSTDIR"
  File /r /x "desktop-self-test.txt" "${PAYLOAD}\*.*"
  WriteRegStr HKCU "Software\winintel\IME" "InstallDir" "$INSTDIR"
  CreateDirectory "$SMPROGRAMS\数萌AI输入法"
  CreateShortcut "$SMPROGRAMS\数萌AI输入法\数萌AI输入法.lnk" "$INSTDIR\winintel-ime.exe"
  CreateShortcut "$SMPROGRAMS\数萌AI输入法\设置.lnk" "$INSTDIR\winintel-ime.exe" "/settings"
  CreateShortcut "$SMPROGRAMS\数萌AI输入法\卸载.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\数萌AI输入法.lnk" "$INSTDIR\winintel-ime.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "DisplayName" "数萌AI输入法"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "DisplayVersion" "0.1.0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "Publisher" "winintel AI"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "DisplayIcon" "$INSTDIR\winintel-ime.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime" "NoRepair" 1
SectionEnd
Section "注册 Windows 系统输入法（首次安装需要管理员授权）" SystemIME
  DetailPrint "配置系统输入法和数萌方案，首次安装请允许 Windows 授权提示…"
  ExecWait '"$INSTDIR\winintel-ime.exe" /configure' $Result
  StrCmp $Result 0 done
    DetailPrint "系统输入法配置未完成，可从托盘菜单重新配置。"
    IfSilent done
    MessageBox MB_OK|MB_ICONEXCLAMATION "系统输入法配置未完成。请从数萌托盘菜单选择“配置系统输入法”重试。"
  done:
SectionEnd
Section /o "登录 Windows 时启动" AutoStart
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "winintel AI Input" '"$INSTDIR\winintel-ime.exe"'
SectionEnd
Function .onInit
  ${GetParameters} $Options
  ClearErrors
  ${GetOptions} $Options "/SKIPIME" $Result
  IfErrors skipImeDone
    !insertmacro UnselectSection ${SystemIME}
  skipImeDone:
FunctionEnd
Section "Uninstall"
  ExecWait '"$INSTDIR\winintel-ime.exe" /quit'
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "winintel AI Input"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime"
  DeleteRegKey HKCU "Software\winintel\IME"
  Delete "$DESKTOP\数萌AI输入法.lnk"
  RMDir /r "$SMPROGRAMS\数萌AI输入法"
  RMDir /r "$INSTDIR"
  ; Keep learned data, schema backups and the independently installed Rime host.
SectionEnd
