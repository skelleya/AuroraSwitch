#define AppName "AuroraSwitch KVM Suite"
#define AppPublisher "KVM Software Switch Team"
#define AppVersion GetVersionNumbersString("staging\Dashboard\KvmSwitch.Dashboard.exe")
#define DashboardExe "KvmSwitch.Dashboard.exe"
#define ServiceExe "KvmSwitch.HostService.exe"
#define ServiceName "AuroraSwitchHostService"
#define AppIdGuid "{{B0EC7664-98E5-4F18-9D41-2AC31FA6BDA1}}"

[Setup]
AppId={#AppIdGuid}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://example.com/kvm-switch
AppSupportURL=https://example.com/kvm-switch/support
DefaultDirName={autopf}\AuroraSwitch
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
Compression=lzma2/max
SolidCompression=yes
OutputBaseFilename=AuroraSwitchSetup
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\Dashboard\{#DashboardExe}
CloseApplications=force
RestartApplications=no
; Icon files - run create-icon.ps1 to generate icon.ico, then uncomment below
SetupIconFile=icon.ico
; WizardImageFile=wizard-large.bmp
; WizardSmallImageFile=wizard-small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.UpdateOption=Update existing installation (keep current settings)
english.ReinstallOption=Remove existing files and perform a clean install
english.NetDownloadPrompt=Install Microsoft .NET 8 Desktop Runtime (required)

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "staging\Dashboard\*"; DestDir: "{app}\Dashboard"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "staging\HostService\*"; DestDir: "{app}\HostService"; Flags: ignoreversion recursesubdirs createallsubdirs
; Place the .NET desktop runtime installer under installer\redist prior to building
Source: "redist\dotnet-runtime-8.0.2-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not IsDotNetInstalled

[Icons]
Name: "{autoprograms}\{#AppName}\AuroraSwitch Dashboard"; Filename: "{app}\Dashboard\{#DashboardExe}"; IconFilename: "{app}\Dashboard\icon.ico"
Name: "{autodesktop}\AuroraSwitch Dashboard"; Filename: "{app}\Dashboard\{#DashboardExe}"; IconFilename: "{app}\Dashboard\icon.ico"; Tasks: desktopicon

[Run]
Filename: "{cmd}"; Parameters: "/c sc stop ""{#ServiceName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Stopping existing AuroraSwitch Host Service..."
Filename: "{cmd}"; Parameters: "/c sc delete ""{#ServiceName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Removing previous service registration..."
Filename: "{cmd}"; Parameters: "/c sc create ""{#ServiceName}"" binPath= ""{app}\HostService\{#ServiceExe}"" start= auto DisplayName= ""AuroraSwitch Host Service"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering AuroraSwitch Host Service..."
Filename: "{cmd}"; Parameters: "/c sc start ""{#ServiceName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Starting AuroraSwitch Host Service..."
Filename: "{app}\Dashboard\{#DashboardExe}"; Description: "Launch AuroraSwitch Dashboard"; Flags: nowait postinstall; WorkingDir: "{app}\Dashboard"
Filename: "{tmp}\dotnet-runtime-8.0.2-win-x64.exe"; Parameters: "/install /quiet /norestart"; \
    StatusMsg: "{cm:NetDownloadPrompt}"; Check: not IsDotNetInstalled; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{cmd}"; Parameters: "/c sc stop ""{#ServiceName}"" 2>nul || exit 0"; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/c sc delete ""{#ServiceName}"" 2>nul || exit 0"; Flags: runhidden waituntilterminated

[Code]
var
  InstallModePage: TWizardPage;
  UpdateRadio: TRadioButton;
  ReinstallRadio: TRadioButton;

function GetMajorVersion(const S: string): Integer;
var
  Delim: Integer;
begin
  Delim := Pos('.', S);
  if Delim > 0 then
    Result := StrToIntDef(Copy(S, 1, Delim - 1), 0)
  else
    Result := StrToIntDef(S, 0);
end;

function IsDotNetInstalled(): Boolean;
var
  version: string;
begin
  if RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', version) then
    Result := GetMajorVersion(version) >= 8
  else
    Result := False;
end;

function AlreadyInstalled(): Boolean;
var
  uninstallKey: string;
begin
  uninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + '{#AppIdGuid}';
  Result :=
    RegKeyExists(HKLM64, uninstallKey) or
    RegKeyExists(HKLM, uninstallKey) or
    RegKeyExists(HKCU, uninstallKey);
end;

procedure InitializeWizard;
begin
  if AlreadyInstalled() then
  begin
    InstallModePage := CreateCustomPage(wpSelectDir, 'Existing Installation Detected',
      'Choose how you would like to proceed with AuroraSwitch.');

    UpdateRadio := TNewRadioButton.Create(WizardForm);
    UpdateRadio.Parent := InstallModePage.Surface;
    UpdateRadio.Caption := ExpandConstant('{cm:UpdateOption}');
    UpdateRadio.Checked := True;

    ReinstallRadio := TNewRadioButton.Create(WizardForm);
    ReinstallRadio.Parent := InstallModePage.Surface;
    ReinstallRadio.Caption := ExpandConstant('{cm:ReinstallOption}');
    ReinstallRadio.Top := UpdateRadio.Top + UpdateRadio.Height + ScaleY(8);
  end;
end;

function NeedCleanInstall: Boolean;
begin
  Result := Assigned(ReinstallRadio) and ReinstallRadio.Checked;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and NeedCleanInstall then
  begin
    if DirExists(ExpandConstant('{app}')) then
    begin
      Log('Performing clean install - removing existing files.');
      DelTree(ExpandConstant('{app}'), True, True, True);
    end;
  end;
end;

procedure DeinitializeSetup;
begin
  if NeedCleanInstall then
  begin
    Log('Clean install completed.');
  end;
end;

