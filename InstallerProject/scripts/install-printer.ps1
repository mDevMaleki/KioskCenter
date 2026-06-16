# نام پرینتر دلخواه شما
$PrinterName = "Kiosk-Printer-TS200P"
# نام دقیق درایور (این نام باید با نام درایور در فایل inf یکی باشد، معمولا TS-200P است)
$DriverName = "TS-200P"

try {
    # پیدا کردن اولین پورت USB فعال
    $usbPort = Get-PrinterPort | Where-Object {$_.Name -like "USB*"} | Select-Object -First 1 -ExpandProperty Name
    
    if (-not $usbPort) {
        $usbPort = "USB001" # پیش‌فرض اگر پورتی نبود
    }

    # اضافه کردن پرینتر
    Add-Printer -Name $PrinterName -DriverName $DriverName -PortName $usbPort
    Write-Host "Printer $PrinterName installed on $usbPort"
}
catch {
    Write-Error "Failed to install printer: $_"
}
