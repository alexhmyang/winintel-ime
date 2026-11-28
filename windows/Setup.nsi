Unicode true
!ifndef VERSION
!define VERSION "0.5.0"
!endif
!include "MUI2.nsh"
!include "x64.nsh"
Name "数萌 AI 输入法"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\winintel-ime-windows-${VERSION}"
RequestExecutionLevel admin
SetCompressor /SOLID zlib
!define MUI_ICON "${PAYLOAD}\winintel.ico"
!define MUI_UNICON "${PAYLOAD}\winintel.ico"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PAYLOAD}\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
Section "数萌 AI 输入法"
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "离线语音版本需要 64 位 Windows 10 或 Windows 11。"
    Abort
  ${EndIf}
  ; The existing controller owns a mutex; it must exit before starting this version.
  nsExec::ExecToStack '"$SYSDIR\taskkill.exe" /F /IM winintel-ime.exe'
  Pop $0
  Pop $1
  SetOutPath "$INSTDIR"
  File /r /x ProductDemo.exe /x TranslationProbe.exe "${PAYLOAD}\*.*"
  ExecWait '"$SYSDIR\regsvr32.exe" /s "$INSTDIR\x86\winintel-tsf.dll"' $0
  ${DisableX64FSRedirection}
  ExecWait '"$WINDIR\System32\regsvr32.exe" /s "$INSTDIR\x64\winintel-tsf.dll"' $1
  ${EnableX64FSRedirection}
  ${If} $0 != 0
  ${OrIf} $1 != 0
    MessageBox MB_ICONSTOP "输入法注册失败，请以管理员身份运行安装程序。"
    Abort
  ${EndIf}
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "winintel-ime" '"$INSTDIR\winintel-ime.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows" "DisplayName" "数萌 AI 输入法"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows" "Publisher" "winintel AI"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows" "UninstallString" '"$INSTDIR\uninstall.exe"'
  CreateDirectory "$SMPROGRAMS\数萌 AI 输入法"
  CreateShortcut "$SMPROGRAMS\数萌 AI 输入法\数萌设置.lnk" "$INSTDIR\winintel-ime.exe" "--show"
  CreateShortcut "$DESKTOP\数萌 AI 输入法.lnk" "$INSTDIR\winintel-ime.exe" "--show"
  Exec '"$INSTDIR\winintel-ime.exe"'
SectionEnd
Section "Uninstall"
  ExecWait '"$SYSDIR\regsvr32.exe" /u /s "$INSTDIR\x86\winintel-tsf.dll"'
  ${DisableX64FSRedirection}
  ExecWait '"$WINDIR\System32\regsvr32.exe" /u /s "$INSTDIR\x64\winintel-tsf.dll"'
  ${EnableX64FSRedirection}
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\winintel-ime-windows"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "winintel-ime"
  Delete "$DESKTOP\数萌 AI 输入法.lnk"
  RMDir /r "$SMPROGRAMS\数萌 AI 输入法"
  RMDir /r "$INSTDIR"
SectionEnd
