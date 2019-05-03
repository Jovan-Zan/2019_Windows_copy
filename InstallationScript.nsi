;---Installation script

	!define APP_NAME "MultithreadWindowsCopy"

;Include Modern UI
	!include "MUI2.nsh"

;------------------------------

; The name of the installer
	Name "${APP_NAME} Installer"

; The output file
	OutFile "MultithreadWindowsCopyInstaller.exe"

; The default installation directory
	InstallDir 	"$PROGRAMFILES\${APP_NAME}"
	
; Registry key to check for directory (so if installed again, it will 
; overwrite the old one automatically)
	InstallDirRegKey HKLM "Software\${APP_NAME}" "InstallDir"

; Administrator privileges are needed to install to ProgramFiles directory
	RequestExecutionLevel admin

;------------------------------

; Prompt a warning when user tries to cancel installation.
	!define MUI_ABORTWARNING	
	
;------------------------------

; Pages user goes through when installing
	!insertmacro MUI_PAGE_DIRECTORY
	!insertmacro MUI_PAGE_INSTFILES  
	
;------------------------------

; Uninstall pages
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES

;------------------------------


Section ""

; Set output path to the installation directory.
	SetOutPath $INSTDIR

; Put files
	File "CopyApp.exe"
	File "CopyApp.exe.config"
	File "PasteApp.exe"
	File "PasteApp.exe.config"
	File "ClipboardApp.exe"
	File "ClipboardApp.exe.config"	
	
	
; Save installation folder to registry
	WriteRegStr HKLM "Software\${APP_NAME}" "InstallDir" $INSTDIR  

; Add Robo-Copy command
	WriteRegStr HKCR "Directory\shell\Robo-Copy\command" "" '"$INSTDIR\CopyApp.exe" "copy"'
	WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Explorer" "MultipleInvokePromptMinimum" 16
	; OVO MOOOOOZDA NE MORA 
	WriteRegStr HKCR "*\shell\Robo-Copy\command" "" '"$INSTDIR\CopyApp.exe" "copy"'
	
; Add Robo-Cut command
	WriteRegStr HKCR "Directory\shell\Robo-Cut\command" "" '"$INSTDIR\CopyApp.exe" "cut"'
	WriteRegStr HKCR "*\shell\Robo-Cut\command" "" '"$INSTDIR\CopyApp.exe" "cut"'
	
; Add Robo-Paste command
	WriteRegStr HKCR "Directory\Background\shell\Robo-Paste\command" "" '"$INSTDIR\PasteApp.exe" "%v"' 
		
; Write the uninstall keys for Windows
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "${APP_NAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoModify" 1
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "NoRepair" 1
	
; Write the uninstaller
	WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd


Section "Uninstall"
; Kill ClipboardApp.exe before uninstallation and save result in registry R0
	${nsProcess::KillProcess} "${APP_EXE}" $R0

; Delete registry keys and values
	DeleteRegKey HKLM "Software\${APP_NAME}"
	DeleteRegKey HKCR "Directory\shell\Robo-Copy"
	DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Explorer" "MultipleInvokePromptMinimum"
	DeleteRegKey HKCR "*\shell\Robo-Copy"
	DeleteRegKey HKCR "Directory\shell\Robo-Cut"
	DeleteRegKey HKCR "*\shell\Robo-Cut"
	DeleteRegKey HKCR "Directory\Background\shell\Robo-Paste"
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

; Delete files
	Delete "$INSTDIR\CopyApp.exe"
	Delete "$INSTDIR\CopyApp.exe.config"
	Delete "$INSTDIR\PasteApp.exe"
	Delete "$INSTDIR\PasteApp.exe.config"
	Delete "$INSTDIR\ClipboardApp.exe"
	Delete "$INSTDIR\ClipboardApp.exe.config"
	Delete "$INSTDIR\uninstall.exe"
	
; Delete directories used
	RMDir $INSTDIR
	
SectionEnd
