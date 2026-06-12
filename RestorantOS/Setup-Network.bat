@echo off
:: Ilk kurulum - YONETICI olarak calistirin (sag tik -> Yonetici)
title RestaurantOS - Ag Kurulumu (bir kez)

net session >nul 2>&1
if errorlevel 1 (
    echo Bu script YONETICI olarak calistirilmalidir.
    pause
    exit /b 1
)

set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"

:: LAN IP tespit et
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
    set IP=%%a
    goto gotip
)
:gotip
set IP=%IP:~1%

:: .env yaz
echo HOST_LAN_IP=%IP%> "%APP%\.env"
echo EXTERNAL_PORT=8080>> "%APP%\.env"

:: Eski portproxy varsa temizle
echo Eski portproxy kurallari temizleniyor...
netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 >nul 2>&1
netsh interface portproxy delete v4tov4 listenport=80   listenaddress=0.0.0.0 >nul 2>&1

:: Guvenlik duvari kurallari
echo Guvenlik duvari kurallari ayarlaniyor...
netsh advfirewall firewall delete rule name="RestaurantOS-8080" >nul 2>&1
netsh advfirewall firewall delete rule name="RestaurantOS-HTTP" >nul 2>&1
netsh advfirewall firewall delete rule name="RestaurantOS HTTP" >nul 2>&1
netsh advfirewall firewall delete rule name="RestaurantOS DNS"  >nul 2>&1
netsh advfirewall firewall delete rule name="RestaurantOS DNS TCP" >nul 2>&1

netsh advfirewall firewall add rule name="RestaurantOS-8080" dir=in action=allow protocol=TCP localport=8080 profile=any >nul 2>&1
netsh advfirewall firewall add rule name="RestaurantOS-HTTP" dir=in action=allow protocol=TCP localport=80 profile=any >nul 2>&1

echo.
echo ========================================
echo   Ag kurulumu tamamlandi
echo ========================================
echo.
echo   LAN IP   : %IP%
echo   Erisim   : http://%IP%:8080
echo.
echo   Diger PC lerde Agdan-Baglan.bat calistirilmali.
echo   Sunucu otomatik bulunacaktir.
echo ========================================
pause
