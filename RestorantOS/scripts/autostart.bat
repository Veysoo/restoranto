@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul 2>&1
title RestaurantOS - Otomatik Baslatma

echo.
echo  ================================================
echo    RestaurantOS  -  Otomatik Baslatma
echo    (PC acilisinda otomatik calisan servis)
echo  ================================================
echo.

:: ============================================
:: Uygulama klasorunu bul (scripts\..\)
:: ============================================
pushd "%~dp0.."
set "APP_DIR=%CD%"
popd

echo  Uygulama klasoru: %APP_DIR%
echo.

:: Log klasoru
if not exist "%APP_DIR%\logs" mkdir "%APP_DIR%\logs"
set "LOG=%APP_DIR%\logs\autostart.log"
echo [%date% %time%] === Autostart basladi === >> "%LOG%"

:: Docker PATH'e ekle
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:: ============================================
:: 1. Docker kurulu mu?
:: ============================================
echo  [1/4] Docker kontrol ediliyor...

where docker >nul 2>&1
if not errorlevel 1 goto :docker_installed
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" goto :docker_installed
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" goto :docker_installed

echo         Docker bulunamadi!
echo         Lutfen Startup.bat dosyasini calistirin.
echo [%date% %time%] Docker bulunamadi >> "%LOG%"
pause
exit /b 1

:docker_installed
echo         Docker kurulu.  OK

:: ============================================
:: 2. Docker Desktop baslatilsin
:: ============================================
echo  [2/4] Docker servisi kontrol ediliyor...

docker info >nul 2>&1
if not errorlevel 1 goto :docker_running

echo         Docker calısmiyor. Baslatiliyor...

set "DEXE="
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          set "DEXE=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" set "DEXE=%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"

if defined DEXE (
    start "" "!DEXE!"
    echo         Docker Desktop baslatildi. Bekleniyor...
) else (
    echo         Docker Desktop exe bulunamadi.
    echo [%date% %time%] Docker exe bulunamadi >> "%LOG%"
    pause
    exit /b 1
)

set /a dw=0
:wait_docker
ping -n 6 127.0.0.1 >nul
set /a dw+=1
docker info >nul 2>&1
if not errorlevel 1 goto :docker_running
set /a elapsed=!dw!*5
if !dw! gtr 72 (
    echo  [HATA] Docker 6 dakikada hazir olmadi.
    echo [%date% %time%] Docker timeout >> "%LOG%"
    pause
    exit /b 1
)
echo         !elapsed! sn beklendi...
goto :wait_docker

:docker_running
echo         Docker servisi hazir.  OK
echo [%date% %time%] Docker hazir >> "%LOG%"

:: ============================================
:: 3. LAN IP al ve .env yaz
:: ============================================
echo  [3/4] Ag adresi aliniyor...

set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%APP_DIR%\scripts\get-lan-ip.ps1"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

echo HOST_LAN_IP=%HOST_LAN_IP%> "%APP_DIR%\.env"
echo EXTERNAL_PORT=8080>> "%APP_DIR%\.env"

echo         LAN IP: %HOST_LAN_IP%  OK
echo [%date% %time%] LAN IP: %HOST_LAN_IP% >> "%LOG%"

:: ============================================
:: 4. Container'lari baslat
:: ============================================
echo  [4/4] Container'lar baslatiliyor...

:: Guvenlik duvari kurallari (sessiz)
netsh advfirewall firewall show rule name="RestaurantOS-8080" >nul 2>&1
if errorlevel 1 (
    netsh advfirewall firewall add rule name="RestaurantOS-8080" dir=in action=allow protocol=TCP localport=8080 profile=any >nul 2>&1
    netsh advfirewall firewall add rule name="RestaurantOS-HTTP" dir=in action=allow protocol=TCP localport=80 profile=any >nul 2>&1
)

:: docker compose veya docker-compose
set "DC=docker compose"
docker compose version >nul 2>&1
if errorlevel 1 (
    where docker-compose >nul 2>&1
    if not errorlevel 1 (
        set "DC=docker-compose"
    ) else (
        echo  [HATA] docker compose bulunamadi.
        echo [%date% %time%] docker compose bulunamadi >> "%LOG%"
        pause
        exit /b 1
    )
)

%DC% -f "%APP_DIR%\docker-compose.yml" --env-file "%APP_DIR%\.env" up -d
set DC_ERR=%ERRORLEVEL%
echo [%date% %time%] docker compose exit: %DC_ERR% >> "%LOG%"

if %DC_ERR% neq 0 (
    echo.
    echo  [HATA] Container baslatma hatasi!
    echo [%date% %time%] Container baslatma BASARISIZ >> "%LOG%"
    pause
    exit /b 1
)

echo.
echo  ================================================
echo    RestaurantOS HAZIR
echo    http://%HOST_LAN_IP%:8080
echo  ================================================
echo.
echo [%date% %time%] BASARILI - %HOST_LAN_IP%:8080 >> "%LOG%"

timeout /t 5 /nobreak >nul
exit /b 0
