# ============================================================
#  Aghadon Kiosk - Full Uninstall Script
# ============================================================

# ---- 1. توقف و حذف Windows Service ----
Write-Host "Removing Aghadon Kiosk API service..."
$svc = Get-Service -Name "AghadonKioskApi" -ErrorAction SilentlyContinue
if ($svc) {
    if ($svc.Status -eq 'Running') {
        Stop-Service -Name "AghadonKioskApi" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
    }
    sc.exe delete "AghadonKioskApi" | Out-Null
    Write-Host "Service removed."
}

# ---- 2. حذف IIS Site و App Pool ----
Write-Host "Removing IIS configuration..."
Import-Module WebAdministration -ErrorAction SilentlyContinue
if (Get-Module WebAdministration) {
    if (Test-Path "IIS:\Sites\AghadonKiosk") {
        Remove-WebSite -Name "AghadonKiosk" -ErrorAction SilentlyContinue
        Write-Host "IIS site removed."
    }
    if (Test-Path "IIS:\AppPools\AghadonKiosk") {
        Remove-WebAppPool -Name "AghadonKiosk" -ErrorAction SilentlyContinue
        Write-Host "IIS app pool removed."
    }
    # پاک کردن فایل‌های فرانت از wwwroot
    $wwwroot = "C:\inetpub\wwwroot"
    if (Test-Path $wwwroot) {
        Get-ChildItem $wwwroot -Recurse -Force | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
        Write-Host "wwwroot cleaned."
    }
}

# ---- 3. حذف پوشه برنامه ----
Write-Host "Removing app directory C:\KioskApp..."
if (Test-Path "C:\KioskApp") {
    Remove-Item "C:\KioskApp" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "C:\KioskApp removed."
}

# ---- 4. حذف SQL Server Express ----
Write-Host "Uninstalling SQL Server Express..."
$sqlSetup = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
            ForEach-Object { Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue } |
            Where-Object { $_.DisplayName -match "SQL Server.*Express" -and $_.DisplayName -match "Database Engine" } |
            Select-Object -First 1

if ($sqlSetup -and $sqlSetup.UninstallString) {
    $uninstStr = $sqlSetup.UninstallString
    Write-Host "Found: $($sqlSetup.DisplayName)"
    # اجرای uninstall silent
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/x `"$($sqlSetup.PSChildName)`" /qn /norestart" -Wait -ErrorAction SilentlyContinue
} else {
    # روش دیگر: از طریق setup.exe
    $sqlExe = Get-ChildItem "C:\Program Files\Microsoft SQL Server" -Filter "setup.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sqlExe) {
        Start-Process $sqlExe.FullName -ArgumentList "/Action=Uninstall /INSTANCENAME=SQLEXPRESS /FEATURES=SQLEngine /Q" -Wait -ErrorAction SilentlyContinue
        Write-Host "SQL Server uninstall initiated."
    } else {
        Write-Host "WARNING: SQL Server Express uninstall entry not found. Remove manually if needed."
    }
}

# ---- 5. حذف DorsanDesk ----
Write-Host "Uninstalling DorsanDesk..."
$dorsan = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
          ForEach-Object { Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue } |
          Where-Object { $_.DisplayName -match "Dorsan" } |
          Select-Object -First 1
if ($dorsan -and $dorsan.UninstallString) {
    Start-Process -FilePath "msiexec.exe" -ArgumentList "/x `"$($dorsan.PSChildName)`" /qn" -Wait -ErrorAction SilentlyContinue
    Write-Host "DorsanDesk removed."
} else {
    Write-Host "DorsanDesk not found in registry."
}

# ---- 6. حذف درایور پرینتر TS-200P ----
Write-Host "Removing TS-200P printer driver..."
$printer = Get-Printer | Where-Object { $_.Name -match "TS-200|GRANDMI" } | Select-Object -First 1
if ($printer) {
    Remove-Printer -Name $printer.Name -ErrorAction SilentlyContinue
    Write-Host "Printer '$($printer.Name)' removed."
}
$driver = Get-PrinterDriver | Where-Object { $_.Name -match "TS-200|Thermal" } | Select-Object -First 1
if ($driver) {
    Remove-PrinterDriver -Name $driver.Name -ErrorAction SilentlyContinue
    Write-Host "Printer driver removed."
}

# ---- 7. حذف Startup shortcut ----
$startupLnk = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\AghadonAutoStart.lnk"
if (Test-Path $startupLnk) {
    Remove-Item $startupLnk -Force -ErrorAction SilentlyContinue
    Write-Host "Startup shortcut removed."
}

Write-Host ""
Write-Host "=== Aghadon Kiosk uninstall complete ==="
