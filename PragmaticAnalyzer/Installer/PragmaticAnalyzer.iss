#define AppName "Система поддержки принятия решения специалистом по ЗИ"
#define AppVersion "1.0"
#define AppPublisher "PragmaticAnalyzer"
#define SourceDir "Package"
#define OutputDir "..\bin\Installer"
#define OutputBaseName "SPPR-ZI-Setup-1.0"
#define AppExeName "PragmaticAnalyzer.exe"

[Setup]
AppId={{B93B89AE-6FC2-46E8-B824-464E61E7D331}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\SPPR-ZI
DefaultGroupName={#AppName}
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseName}
SetupIconFile=..\Images\icon.ico
Compression=zip/1
SolidCompression=no
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
DisableWelcomePage=no
DisableDirPage=no
DisableProgramGroupPage=yes
AlwaysShowDirOnReadyPage=yes
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "startmenuicon"; Description: "Создать ярлык в меню Пуск"; GroupDescription: "Ярлыки:"
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Ярлыки:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#ifexist "redist\vc_redist.x64.exe"
Source: "redist\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
#endif

[Dirs]
Name: "{app}\Config"
Name: "{app}\Database"
Name: "{app}\Models"
Name: "{app}\NativeLlama"
Name: "{app}\SemanticSearchEngine"
Name: "{app}\Translator"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: startmenuicon
Name: "{group}\Удалить {#AppName}"; Filename: "{uninstallexe}"; Tasks: startmenuicon
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
#ifexist "redist\vc_redist.x64.exe"
Filename: "{tmp}\vc_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Установка Microsoft Visual C++ Redistributable..."; Flags: waituntilterminated
#endif
Filename: "{app}\{#AppExeName}"; Description: "Запустить программу"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"
