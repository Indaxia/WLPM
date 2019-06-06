; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{971F467E-1815-46E0-83BE-DB1FC661097D}
AppName=Warcraft 3 Lua Package Manager
AppVersion=0.6-beta
AppPublisher=Indaxia
AppPublisherURL=https://github.com/Indaxia/WLPM
AppSupportURL=https://github.com/Indaxia/WLPM/issues
DefaultDirName={pf}\WLPM
DefaultGroupName=WLPM
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputBaseFilename=Install WLPM for Windows 10 x64
Compression=lzma
SolidCompression=yes
ChangesEnvironment=yes
RestartIfNeededByRun=False
ShowLanguageDialog=no
AppReadmeFile=..\README.md
ArchitecturesInstallIn64BitMode=x64
InfoAfterFile=..\README.md
UninstallDisplayName=Warcraft 3 Lua Package Manager (WLPM)
AppCopyright=ScorpioT1000 � 2019
VersionInfoProductVersion=0.6
SetupIconFile=..\Resources\install.ico
AlwaysShowGroupOnReadyPage=True
AlwaysShowDirOnReadyPage=True
UninstallDisplayIcon={uninstallexe}
DisableWelcomePage=False

[Files]
Source: "..\bin\Release\netcoreapp2.2\win10-x64\publish\*.dll"; DestDir: "{app}" 
Source: "..\bin\Release\netcoreapp2.2\win10-x64\publish\*.json"; DestDir: "{app}"   
Source: "..\bin\Release\netcoreapp2.2\win10-x64\publish\*.pdb"; DestDir: "{app}" 
Source: "..\bin\Release\netcoreapp2.2\win10-x64\publish\wlpm.exe"; DestDir: "{app}"

[Icons]
Name: "{group}\{cm:UninstallProgram,WLPM}"; Filename: "{uninstallexe}"
Name: "{group}\Documentation"; Filename: "https://github.com/Indaxia/WLPM/blob/master/README.md"
Name: "{group}\WLPM"; Filename: "{app}\wlpm.exe"; IconFilename: "{uninstallexe}"; Parameters: "--noexit"

[Registry]
Root: HKLM; \
  Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
  ValueType: expandsz; \
  ValueName: "Path"; \
  ValueData: "{olddata};{app}"; \
  Check: NeedsAddPath('{app}') 

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  { look for the path with leading and trailing semicolon }
  { Pos() returns 0 if not found }
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;

const
  EnvironmentKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';

procedure RemovePath(Path: string);
var
  Paths: string;
  P: Integer;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths) then
  begin
    Log('PATH not found');
  end
    else
  begin
    Log(Format('PATH is [%s]', [Paths]));

    P := Pos(';' + Uppercase(Path) + ';', ';' + Uppercase(Paths) + ';');
    if P = 0 then
    begin
      Log(Format('Path [%s] not found in PATH', [Path]));
    end
      else
    begin
      if P > 1 then P := P - 1;
      Delete(Paths, P, Length(Path) + 1);
      Log(Format('Path [%s] removed from PATH => [%s]', [Path, Paths]));

      if RegWriteStringValue(HKEY_LOCAL_MACHINE, EnvironmentKey, 'Path', Paths) then
      begin
        Log('PATH written');
      end
        else
      begin
        Log('Error writing PATH');
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    RemovePath(ExpandConstant('{app}'));
  end;
end;
