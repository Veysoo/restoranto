@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Otomatik Baslatmayi Kapat

echo.
echo  ==================================================
echo    RestaurantOS  -  Otomatik Baslatmayi Kapat
echo  ==================================================
echo.

:: Yonetici yetkisi
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  Yonetici yetkisi gerekiyor...
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b %errorlevel%
)

echo  [1/3] Zamanlanmis gorev siliniyor...
schtasks /delete /tn "RestaurantOS-AutoStart" /f >nul 2>&1
if errorlevel 1 (
    echo         Gorev bulunamadi veya zaten silinmis.
) else (
    echo         RestaurantOS-AutoStart silindi.  OK
)

echo  [2/3] Windows baslangic kaydi kaldiriliyor...
reg delete "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "Docker Desktop" /f >nul 2>&1
if errorlevel 1 (
    echo         Docker Desktop baslangic kaydi yok.
) else (
    echo         Docker Desktop baslangic kaydi silindi.  OK
)

echo  [3/3] Calisan container'lar durduruluyor...
set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"
pushd "%APP%"
docker compose down >nul 2>&1
if errorlevel 1 docker-compose down >nul 2>&1
popd
echo         Container'lar durduruldu (Docker aciksa).

echo.
echo  ==================================================
echo    Tamamlandi!
echo.
echo    PC acildiginda RestaurantOS artik otomatik
echo    baslamayacak.
echo.
echo    Uygulamayi elle baslatmak icin:
echo      Startup.bat
echo  ==================================================
echo.
pause
