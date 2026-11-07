[Setup]
AppId={{7C3F39D2-7E91-4D68-8F9A-51E9EFD62A21}
AppName=Swipe Keyboard
AppVersion=0.1.0
DefaultDirName={autopf}\Swipe Keyboard
DefaultGroupName=Swipe Keyboard
OutputDir=output
OutputBaseFilename=SwipeKeyboard-Setup
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayIcon={app}\SwipeKeyboard.exe

[Files]
Source: "..\publish\SwipeKeyboard.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Swipe Keyboard"; Filename: "{app}\SwipeKeyboard.exe"
Name: "{autodesktop}\Swipe Keyboard"; Filename: "{app}\SwipeKeyboard.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"
Name: "startup"; Description: "Запускать вместе с Windows"; GroupDescription: "Дополнительные задачи:"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SwipeKeyboard"; ValueData: """{app}\SwipeKeyboard.exe"""; Tasks: startup; Flags: uninsdeletevalue