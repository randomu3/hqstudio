; HQ Studio Desktop Installer Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define MyAppName "HQ Studio"
#define MyAppVersion "1.8.0"
#define MyAppPublisher "HQ Studio"
#define MyAppURL "https://hqstudio.ru"
#define MyAppExeName "HQStudio.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=HQStudio-Setup-{#MyAppVersion}
SetupIconFile=..\HQStudio.Desktop\Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Красивый интерфейс
WizardImageFile=compiler:WizModernImage.bmp
WizardSmallImageFile=compiler:WizModernSmallImage.bmp

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Основной исполняемый файл (single-file publish)
Source: "..\HQStudio.Desktop\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Конфигурация (если есть)
Source: "..\HQStudio.Desktop\appsettings.example.json"; DestDir: "{app}"; DestName: "appsettings.json"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Проверка наличия .NET Runtime (опционально, т.к. self-contained)
function IsDotNetInstalled(): Boolean;
begin
  Result := True; // Self-contained не требует .NET
end;

// Удаление старой версии перед установкой
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  // Закрываем приложение если запущено
  if CheckForMutexes('HQStudioMutex') then
  begin
    MsgBox('HQ Studio запущен. Пожалуйста, закройте приложение перед установкой.', mbError, MB_OK);
    Result := 'HQ Studio должен быть закрыт';
  end;
end;
