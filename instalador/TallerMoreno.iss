; Instalador de Taller Moreno (Inno Setup 6).
;
; Primero se publica el programa en la carpeta "publicar":
;   dotnet publish src/TallerMoreno.Desktop -c Release -r win-x64 --self-contained -o publicar
; Luego, desde la raíz del repositorio:
;   iscc /DVersion=1.1.0 instalador/TallerMoreno.iss
; El instalador queda en instalador/salida/.
;
; Los datos (base de datos, fotos y errores.log) quedan en %LOCALAPPDATA%\TallerMoreno,
; así que desinstalar o actualizar el programa no los borra.

#ifndef Version
  #define Version "1.0.0"
#endif

[Setup]
AppId={{6B0C2E8A-4F7D-4C1B-9E3A-5D2F8A7C1E40}
AppName=Taller Moreno
AppVersion={#Version}
AppVerName=Taller Moreno {#Version}
AppPublisher=Santiago Caicedo
DefaultDirName={autopf}\Taller Moreno
DefaultGroupName=Taller Moreno
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=salida
OutputBaseFilename=TallerMoreno-Instalador-{#Version}
SetupIconFile=..\src\TallerMoreno.Desktop\Assets\icono.ico
UninstallDisplayIcon={app}\TallerMoreno.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "..\publicar\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.json,*.pdb"

[Icons]
Name: "{group}\Taller Moreno"; Filename: "{app}\TallerMoreno.exe"
Name: "{autodesktop}\Taller Moreno"; Filename: "{app}\TallerMoreno.exe"; Tasks: escritorio

[Run]
Filename: "{app}\TallerMoreno.exe"; Description: "Abrir Taller Moreno"; Flags: nowait postinstall skipifsilent
