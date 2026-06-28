@echo off

taskkill /IM chrome.exe /F >nul 2>&1

REM Backend as Windows Service - just wait for it to be ready
timeout /t 5 /nobreak > nul

set CHROME_PATH=C:\Program Files\Google\Chrome\Application\chrome.exe
IF EXIST "%CHROME_PATH%" (
    start "" "%CHROME_PATH%" --kiosk http://localhost
    exit /b
)

set CHROME_PATH=C:\Program Files (x86)\Google\Chrome\Application\chrome.exe
IF EXIST "%CHROME_PATH%" (
    start "" "%CHROME_PATH%" --kiosk http://localhost
    exit /b
)

echo Chrome not found.
pause
