; EtherDNS installer — built by ..\build.ps1 (Inno Setup 6)

#define AppName "EtherDNS"
#ifndef AppVersion
  #define AppVersion "2.1.0"
#endif
#define AppPublisher "DevUranium"
#define AppExe "EtherDNS.exe"

[Setup]
AppId={{A6E3B7C2-5D41-4F8E-9B2A-3C7D1E0F4A52}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/DevURANIUM
AppSupportURL=https://t.me/DevRouter
AppCopyright=Copyright © DevUranium. All rights reserved.
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=EtherDNS-Setup-{#AppVersion}
SetupIconFile=..\Resources\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
WizardSizePercent=110
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent shellexec
; In-app updates run the installer with /SILENT — relaunch EtherDNS when that finishes.
Filename: "{app}\{#AppExe}"; Flags: nowait shellexec skipifnotsilent
