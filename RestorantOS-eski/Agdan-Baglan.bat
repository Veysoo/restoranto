@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Agdan Baglan

echo.
echo  =================================================
echo    RestaurantOS  -  Agda Sunucu Bul
echo    Yonetici izni GEREKMEZ
echo  =================================================
echo.

:: Uygulama klasoru
set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"

echo  Agdaki cihazlar taraniyor...
echo  (Bu islem 10-30 saniye surebilir, lutfen bekleyin)
echo.

set "ADDR="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%APP%\scripts\find-server.ps1"`) do (
    set "ADDR=%%I"
)

if not defined ADDR (
    echo  -----------------------------------------------
    echo  [!] Agda RestaurantOS sunucusu BULUNAMADI.
    echo.
    echo  Kontrol listesi:
    echo    1. Sunucu PC de Startup.bat acik olmali
    echo    2. Bu bilgisayar sunucu ile AYNI WiFi da olmali
    echo    3. Guvenlik duvari acik olmali
    echo  -----------------------------------------------
    echo.
    set /p ADDR="Elle girmek ister misiniz? (orn: 192.168.1.103:8080) : "
    if "!ADDR!"=="" (
        echo Iptal edildi.
        pause
        exit /b 1
    )
)

:: Sadece IP girilmisse :8080 ekle
echo !ADDR! | find ":" >nul 2>&1
if errorlevel 1 set "ADDR=!ADDR!:8080"

set "URL=http://!ADDR!"

echo.
echo  ================================================
echo   [OK]  Sunucu Bulundu : !URL!
echo  ================================================
echo.

:: Masaustune .url kisayolu olustur (yonetici gerektirmez)
set "LNK=%USERPROFILE%\Desktop\RestaurantOS.url"
echo [InternetShortcut]    > "!LNK!"
echo URL=!URL!/login       >> "!LNK!"

echo  Masaustune "RestaurantOS" kisayolu olusturuldu.
echo  Bir sonraki sefer o kisayola cift tiklamak yeterli.
echo.

start "" "!URL!/login"
echo  Tarayici acildi!
echo.
pause
