#define MyAppName "Aghadon Kiosk"
#define MyAppVersion "1.2"
#define AppDir "C:\KioskApp"

[Setup]
AppId={{A1B2C3D4-E5F6-4A5B-8C9D-0E1F2A3B4C5D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={#AppDir}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=AghadonKioskInstaller
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Types]
Name: "full";   Description: "نصب کامل"
Name: "custom"; Description: "انتخاب دستی"; Flags: iscustom

[Components]
Name: "app";        Description: "برنامه کیوسک (بک‌اند + فرانت‌اند)"; Types: full custom; Flags: fixed
Name: "iis";        Description: "نصب و پیکربندی IIS";                Types: full custom
Name: "sql";        Description: "SQL Server Express";                  Types: full custom
Name: "chrome";     Description: "Google Chrome";                       Types: full custom
Name: "dorsan";     Description: "DorsanDesk";                          Types: full custom
Name: "printer";    Description: "درایور پرینتر TS-200P";              Types: full custom
Name: "touchtest";  Description: "تست لمسی (Touch Calibration)";        Types: full custom

[Files]
; --- فرانت‌اند ---
Source: "front\*"; DestDir: "C:\inetpub\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: app

; --- بک‌اند ---
Source: "backend\*"; DestDir: "{#AppDir}\Backend"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: app

; --- اسکریپت‌های راه‌اندازی ---
Source: "scripts\kiosk.bat";            DestDir: "{#AppDir}";  Flags: ignoreversion;        Components: app
Source: "scripts\install-iis.ps1";      DestDir: "{tmp}";      Flags: deleteafterinstall;   Components: iis
Source: "scripts\install-service.ps1";  DestDir: "{tmp}";      Flags: deleteafterinstall;   Components: app
Source: "scripts\install-printer.ps1";  DestDir: "{tmp}";      Flags: deleteafterinstall;   Components: printer
Source: "scripts\uninstall-all.ps1";    DestDir: "{#AppDir}";  Flags: ignoreversion;        Components: app

; --- پیش‌نیازها ---
Source: "prereqs\chrome.msi";                        DestDir: "{tmp}"; Flags: deleteafterinstall; Components: chrome
Source: "prereqs\DorsanDesk.msi";                    DestDir: "{tmp}"; Flags: deleteafterinstall; Components: dorsan
Source: "prereqs\SQLEXPR_x64_ENU.exe";               DestDir: "{tmp}"; Flags: deleteafterinstall; Components: sql
Source: "prereqs\ts-200p-driver.exe";                DestDir: "{tmp}"; Flags: deleteafterinstall; Components: printer
Source: "prereqs\GDTouchTest_EN (3).exe";            DestDir: "{tmp}"; Flags: deleteafterinstall; Components: touchtest
Source: "prereqs\SeeTouchTest.1.39.2306.EN (1).exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Components: touchtest

[Code]
function GetSystemMetrics(nIndex: Integer): Integer;
external 'GetSystemMetrics@user32.dll stdcall';

procedure RunTouchTest();
var
  ScreenWidth: Integer;
  ResultCode: Integer;
begin
  ScreenWidth := GetSystemMetrics(0);
  Log('Detected Screen Width: ' + IntToStr(ScreenWidth));
  if ScreenWidth <= 1920 then
    Exec(ExpandConstant('{tmp}\GDTouchTest_EN (3).exe'), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode)
  else
    Exec(ExpandConstant('{tmp}\SeeTouchTest.1.39.2306.EN (1).exe'), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if IsComponentSelected('touchtest') then
      RunTouchTest();
  end;
end;

[Run]
; 1. نصب کروم
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\chrome.msi"" /qn /norestart"; StatusMsg: "Installing Chrome..."; Flags: waituntilterminated; Components: chrome

; 2. IIS
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\install-iis.ps1"""; StatusMsg: "Installing IIS..."; Flags: waituntilterminated runhidden; Components: iis

; 3. SQL Server Express
Filename: "{tmp}\SQLEXPR_x64_ENU.exe"; Parameters: "/QS /ACTION=Install /FEATURES=SQLEngine /INSTANCENAME=SQLEXPRESS /IAcceptSqlServerLicenseTerms /SECURITYMODE=SQL /SAPWD=""Aghadon@123"""; StatusMsg: "Installing SQL Server Express..."; Flags: waituntilterminated; Components: sql

; 4. DorsanDesk
Filename: "msiexec.exe"; Parameters: "/i ""{tmp}\DorsanDesk.msi"" /qn"; StatusMsg: "Installing DorsanDesk..."; Flags: waituntilterminated; Components: dorsan

; 5. درایور پرینتر
Filename: "{tmp}\ts-200p-driver.exe"; StatusMsg: "Installing printer driver..."; Flags: waituntilterminated; Components: printer
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\install-printer.ps1"""; StatusMsg: "Configuring printer..."; Flags: waituntilterminated runhidden; Components: printer

; 6. Windows Service بک‌اند
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\install-service.ps1"""; StatusMsg: "Installing Aghadon Kiosk API service..."; Flags: waituntilterminated runhidden; Components: app

; 7. اجرای کیوسک پس از نصب
Filename: "{#AppDir}\kiosk.bat"; Description: "Launch Aghadon Kiosk"; Flags: postinstall nowait; Components: app

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{#AppDir}\uninstall-all.ps1"""; Flags: waituntilterminated runhidden; RunOnceId: "UninstallAll"

[Icons]
Name: "{commonstartup}\AghadonAutoStart"; Filename: "{#AppDir}\kiosk.bat"; Components: app
Name: "{commondesktop}\Aghadon Kiosk";   Filename: "{#AppDir}\kiosk.bat"; Components: app
