$ServiceName    = "AghadonKioskApi"
$DisplayName    = "Aghadon Kiosk API"
$Description    = "Backend API service for Aghadon Kiosk application"
$ExePath        = "C:\KioskApp\Backend\KioskCenter.exe"
$WorkingDir     = "C:\KioskApp\Backend"
$SqlServiceName = "MSSQL`$SQLEXPRESS"

# ---- 1. پیدا کردن registry key نسخه SQL Express نصب شده ----
Write-Host "Configuring SQL Server Express TCP port..."
$sqlInstances = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server" -ErrorAction SilentlyContinue |
                Where-Object { $_.PSChildName -match "^MSSQL\d+\.SQLEXPRESS$" }

foreach ($inst in $sqlInstances) {
    $tcpIpAllPath = "$($inst.PSPath)\MSSQLServer\SuperSocketNetLib\Tcp\IPAll"
    if (Test-Path $tcpIpAllPath) {
        # حذف پورت داینامیک و تنظیم پورت ثابت 1433
        Set-ItemProperty -Path $tcpIpAllPath -Name "TcpDynamicPorts" -Value "" -ErrorAction SilentlyContinue
        Set-ItemProperty -Path $tcpIpAllPath -Name "TcpPort"         -Value "1433" -ErrorAction SilentlyContinue
        Write-Host "Set static TCP port 1433 for $($inst.PSChildName)"
    }

    # فعال‌سازی TCP/IP
    $tcpPath = "$($inst.PSPath)\MSSQLServer\SuperSocketNetLib\Tcp"
    if (Test-Path $tcpPath) {
        Set-ItemProperty -Path $tcpPath -Name "Enabled" -Value 1 -ErrorAction SilentlyContinue
    }
}

# ---- 2. ری‌استارت SQL Server برای اعمال تنظیمات ----
Write-Host "Restarting SQL Server Express..."
$sqlSvc = Get-Service -Name $SqlServiceName -ErrorAction SilentlyContinue
if ($sqlSvc) {
    Restart-Service -Name $SqlServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 8

    # صبر تا SQL Server روی پورت 1433 آماده شود
    Write-Host "Waiting for SQL Server on port 1433..."
    $maxWait = 90
    $waited  = 0
    while ($waited -lt $maxWait) {
        try {
            $conn = New-Object System.Data.SqlClient.SqlConnection(
                "Server=localhost,1433;Database=master;Integrated Security=True;Connect Timeout=3;TrustServerCertificate=True"
            )
            $conn.Open()
            $conn.Close()
            Write-Host "SQL Server ready on port 1433 (after $waited s)."
            break
        } catch {
            Start-Sleep -Seconds 3
            $waited += 3
        }
    }
    if ($waited -ge $maxWait) {
        Write-Host "WARNING: SQL Server may not be ready. Proceeding anyway..."
    }
} else {
    Write-Host "WARNING: SQL Server service '$SqlServiceName' not found."
}

# ---- 3. حذف سرویس قبلی ----
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# ---- 4. ایجاد سرویس ----
sc.exe create $ServiceName `
    binPath= "`"$ExePath`" --contentRoot `"$WorkingDir`"" `
    DisplayName= "$DisplayName" `
    start= auto `
    obj= "LocalSystem"

sc.exe config $ServiceName depend= $SqlServiceName
sc.exe description $ServiceName "$Description"
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000

# ---- 5. شروع سرویس ----
Start-Service -Name $ServiceName

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($svc -and $svc.Status -eq 'Running') {
    Write-Host "Service '$ServiceName' started successfully."
} else {
    Write-Host "WARNING: Service may not have started. Check Event Viewer."
}
