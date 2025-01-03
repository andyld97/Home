; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Home.Service.Windows"
#define MyAppVersion "1.2.9"
#define MyAppPublisher "Code A Software"
#define MyAppURL "https://github.com/andyld97/Home"
#define MyAppExeName "Home.Service.Windows.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C1895F29-AB90-43BE-AB73-50832505135A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
VersionInfoVersion={#MyAppVersion}
;PrivilegesRequired=admin
OutputDir=bin\release\net9.0-windows\publish\
OutputBaseFilename=Home.Service.Windows.Setup
SetupIconFile=icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
;Name: "StartMenuEntry" ; Description: "Start my app when Windows starts" ; GroupDescription: "Windows Startup"; MinVersion: 4,4;
;Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\release\net9.0-windows\publish\cs\*"; DestDir: "{app}\cs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\de\*"; DestDir: "{app}\de"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\es\*"; DestDir: "{app}\es"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\fr\*"; DestDir: "{app}\fr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\it\*"; DestDir: "{app}\it"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\ja\*"; DestDir: "{app}\ja"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\ko\*"; DestDir: "{app}\ko"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\pl\*"; DestDir: "{app}\pl"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\pt-BR\*"; DestDir: "{app}\pt-BR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\ru\*"; DestDir: "{app}\ru"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\tr\*"; DestDir: "{app}\tr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\zh-Hans\*"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\zh-Hant\*"; DestDir: "{app}\zh-Hant"; Flags: ignoreversion recursesubdirs createallsubdirs
; OLD NOTIFICATION (MUST BE COMPILED IN RELEASE BEFORE!)
Source: "..\..\Helper Applications\Notification\bin\Release\*"; DestDir: "{app}\Notification"; Flags: ignoreversion recursesubdirs createallsubdirs
; NEW NOTIFICATION MUST BE COMPILED/PUBLISHED IN RELEASE BEFORE!
Source: "..\..\Helper Applications\HomeNotification\bin\Release\net9.0-windows10.0.17763.0\publish\*"; DestDIr: "{app}\Toast"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\Helper Applications\ClientUpdate\bin\Release\net9.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\release\net9.0-windows\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\App.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Communication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Communication.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Data.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Data.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Measure.Windows.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Measure.Windows.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Home.Service.Windows.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Microsoft.Bcl.AsyncInterfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\Serialization.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ComponentModel.Composition.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ComponentModel.Composition.Registration.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Data.Odbc.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Data.OleDb.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Data.SqlClient.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.IO.Ports.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Private.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Reflection.Context.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Runtime.Caching.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.Duplex.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.Http.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.NetTcp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.Primitives.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.Security.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceModel.Syndication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.ServiceProcess.ServiceController.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Speech.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\System.Web.Services.Description.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\NumericUpDownLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\release\net9.0-windows\publish\ByteUnit.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
;Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
;Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
;Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks:;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: runascurrentuser nowait postinstall skipifsilent