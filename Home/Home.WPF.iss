; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Home.WPF"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Code A Software"
#define MyAppURL "https://github.com/andyld97/Home"
#define MyAppExeName "Home.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{F6242F3F-3A3B-49C1-8418-EFC6BF9F7E7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=bin\Release\net6.0-windows\publish\
OutputBaseFilename=Home.WPF.Setup.exe
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]

Source: "bin\Release\net6.0-windows\publish\cs\*"; DestDir: "{app}\cs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\de\*"; DestDir: "{app}\de"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\es\*"; DestDir: "{app}\es"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\fr\*"; DestDir: "{app}\fr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\it\*"; DestDir: "{app}\it"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\ja\*"; DestDir: "{app}\ja"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\ko\*"; DestDir: "{app}\ko"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\pl\*"; DestDir: "{app}\pl"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\pt-BR\*"; DestDir: "{app}\pt-BR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\ru\*"; DestDir: "{app}\ru"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\tr\*"; DestDir: "{app}\tr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\zh-Hans\*"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\zh-Hant\*"; DestDir: "{app}\zh-Hant"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net6.0-windows\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\ControlzEx.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Fluent.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\HarfBuzzSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.Communication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.Communication.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.Data.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.Data.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Home.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\LiveChartsCore.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\LiveChartsCore.SkiaSharpView.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\LiveChartsCore.SkiaSharpView.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Bcl.AsyncInterfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Win32.Registry.AccessControl.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Win32.SystemEvents.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Microsoft.Xaml.Behaviors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\Serialization.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\SkiaSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\SkiaSharp.HarfBuzz.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\SkiaSharp.Views.Desktop.Common.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\SkiaSharp.Views.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.CodeDom.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ComponentModel.Composition.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ComponentModel.Composition.Registration.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Configuration.ConfigurationManager.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Data.Odbc.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Data.OleDb.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Data.SqlClient.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Diagnostics.EventLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Diagnostics.PerformanceCounter.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.DirectoryServices.AccountManagement.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.DirectoryServices.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.DirectoryServices.Protocols.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Drawing.Common.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Formats.Asn1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.IO.Packaging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.IO.Ports.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Private.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Reflection.Context.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Runtime.Caching.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Security.Cryptography.Pkcs.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Security.Cryptography.ProtectedData.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Security.Cryptography.Xml.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Security.Permissions.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.Duplex.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.Http.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.NetTcp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.Primitives.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.Security.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceModel.Syndication.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.ServiceProcess.ServiceController.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Speech.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Text.Encoding.CodePages.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Text.Encodings.Web.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Text.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Threading.AccessControl.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Web.Services.Description.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net6.0-windows\publish\System.Windows.Extensions.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
