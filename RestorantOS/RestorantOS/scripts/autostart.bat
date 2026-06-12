@echo off
::
:: RestaurantOS - Otomatik Baslangic
:: Windows Gorev Zamanlayici tarafindan kullanici oturumu acildiginda calisir.
:: Docker hazir olana kadar bekler, sonra container'lari ayaga kaldirir.
::
setlocal EnableDelayedExpansion

set "APP_DIR=%~dp0.."
:: ~dp0 scripts\ klasörünü verir, bir üste çık
for %%D in ("%APP_DIR%") do set "APP_DIR=%%~fD"

:: Log dosyasi
set "LOG=%APP_DIR%\logs\autostart.log"
if not exist "%APP_DIR%\logs" mkdir "%APP_DIR%\logs"
echo [%date% %time%] RestaurantOS autostart baslatildi >> "%LOG%"

:: ============================================
:: 1. Docker Desktop'in baslamasi icin bekle
:: ============================================
set /a w=0
:wait_docker
docker info >nul 2>&1
if not errorlevel 1 goto :docker_ok

set /a w+=1
:: 5 dakika bekle (60 x 5 sn = 300 sn)
if !w! gtr 60 (
    echo [%date% %time%] Docker 5 dakikada hazir olmadi, cikiliyor >> "%LOG%"
    exit /b 1
)
ping -n 6 127.0.0.1 >nul
goto :wait_docker

:docker_ok
echo [%date% %time%] Docker hazir >> "%LOG%"

:: ============================================
:: 2. LAN IP guncelle (.env yenile)
:: ============================================
set "PS_GET_IP=Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -notlike '172.25.*' -and $_.IPAddress -notlike '192.168.56.*' } | Select-Object -First 1 -ExpandProperty IPAddress"

set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "%PS_GET_IP%"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

echo HOST_LAN_IP=%HOST_LAN_IP%  > "%APP_DIR%\.env"
echo EXTERNAL_PORT=8080         >> "%APP_DIR%\.env"

echo [%date% %time%] LAN IP: %HOST_LAN_IP% >> "%LOG%"

:: ============================================
:: 3. Container'lari baslat (build olmadan, hizli)
:: ============================================
docker-compose -f "%APP_DIR%\docker-compose.yml" --env-file "%APP_DIR%\.env" up -d >> "%LOG%" 2>&1

if errorlevel 1 (
    echo [%date% %time%] docker-compose up basarisiz >> "%LOG%"
    exit /b 1
)

echo [%date% %time%] Container'lar baslatildi >> "%LOG%"
exit /b 0
