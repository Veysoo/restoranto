@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Otomatik Baslangic

echo.
echo  ================================================
echo    RestaurantOS  -  Otomatik Baslangic
echo    (Bu script sadece SUNUCU PC icin gereklidir)
echo    Baska PC'den baglanmak icin: Agdan-Baglan.bat
echo  ================================================
echo.

:: ============================================
:: Uygulama klasorunu bul (scripts\..\)
:: ============================================
pushd "%~dp0.."
set "APP_DIR=%CD%"
popd

echo  Uygulama klasoru : %APP_DIR%
echo.

:: Log klasoru
if not exist "%APP_DIR%\logs" mkdir "%APP_DIR%\logs"
set "LOG=%APP_DIR%\logs\autostart.log"
echo [%date% %time%] === Autostart basladi === >> "%LOG%"
echo [%date% %time%] APP_DIR: %APP_DIR% >> "%LOG%"

:: Docker PATH'e ekle (her ihtimale karsi)
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:: ============================================
:: 1. Docker kurulu mu?
:: ============================================
echo  [1/4] Docker kontrol ediliyor...

where docker >nul 2>&1
if not errorlevel 1 goto :docker_installed
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" goto :docker_installed
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" goto :docker_installed

echo         Docker bulunamadi. Kuruluyor...
echo [%date% %time%] Docker yok, kuruluyor >> "%LOG%"
call :fn_install_docker
if errorlevel 1 (
    echo.
    echo  [HATA] Docker kurulamadi.
    echo  Elle kurun: https://docs.docker.com/desktop/install/windows-install/
    echo [%date% %time%] Docker kurulum BASARISIZ >> "%LOG%"
    pause
    exit /b 1
)
:: PATH guncelle
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:docker_installed
echo  [1/4] Docker kurulu.  OK

:: ============================================
:: 2. Docker Desktop calisiyor mu? Baslat.
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
    echo         Docker Desktop baslatildi. Hazir olana kadar bekleniyor...
) else (
    echo         Docker Desktop exe bulunamadi, devam ediliyor...
)

set /a dw=0
:wait_docker
docker info >nul 2>&1
if not errorlevel 1 goto :docker_running
set /a dw+=1
set /a elapsed=!dw!*5
if !dw! gtr 72 (
    echo  [HATA] Docker 6 dakikada hazir olmadi.
    echo [%date% %time%] Docker timeout >> "%LOG%"
    pause
    exit /b 1
)
echo         !elapsed! sn beklendi...
ping -n 6 127.0.0.1 >nul
goto :wait_docker

:docker_running
echo  [2/4] Docker servisi hazir.  OK
echo [%date% %time%] Docker hazir >> "%LOG%"

:: ============================================
:: 3. LAN IP al ve .env yaz
:: ============================================
echo  [3/4] Ag adresi aliniyor...

set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -notlike '172.25.*' -and $_.IPAddress -notlike '192.168.56.*' } | Select-Object -First 1 -ExpandProperty IPAddress"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

echo HOST_LAN_IP=%HOST_LAN_IP%  > "%APP_DIR%\.env"
echo EXTERNAL_PORT=8080         >> "%APP_DIR%\.env"

echo  [3/4] LAN IP: %HOST_LAN_IP%  OK
echo [%date% %time%] LAN IP: %HOST_LAN_IP% >> "%LOG%"

:: ============================================
:: 4. Container'lari baslat
:: ============================================
echo  [4/4] Container'lar baslatiliyor...

:: sanal-network yoksa olustur
docker network inspect sanal-network >nul 2>&1
if errorlevel 1 (
    echo         sanal-network olusturuluyor...
    docker network create sanal-network >nul 2>&1
)

:: Docker WSL2 portproxy - dis ag erisimi icin (sessiz, hata olsa devam et)
netsh interface portproxy show v4tov4 | findstr "8080" >nul 2>&1
if errorlevel 1 (
    echo         Portproxy ayarlaniyor...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%APP_DIR%\scripts\setup-network.ps1""' -Verb RunAs -Wait" >nul 2>&1
)

docker-compose -f "%APP_DIR%\docker-compose.yml" --env-file "%APP_DIR%\.env" up -d
set DC_ERR=%ERRORLEVEL%
echo [%date% %time%] docker-compose exit: %DC_ERR% >> "%LOG%"

if %DC_ERR% neq 0 (
    echo.
    echo  [HATA] docker-compose hatasi! Log: %LOG%
    pause
    exit /b 1
)

echo.
echo  ================================================
echo    RestaurantOS HAZIR
echo    http://%HOST_LAN_IP%:8080
echo  ================================================
echo.
echo [%date% %time%] Basarili - %HOST_LAN_IP%:8080 >> "%LOG%"

timeout /t 8 /nobreak >nul
exit /b 0

:: ============================================
:: FONKSIYON: Docker Desktop Kur
:: ============================================
:fn_install_docker

:: Once PATH disinda da kurulu olabilir, kontrol et
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          exit /b 0
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" exit /b 0

echo         Docker Desktop bulunamadi. Kurulum baslatiliyor...

:: --- Yontem 1: winget (sessiz, onay otomatik) ---
where winget >nul 2>&1
if not errorlevel 1 (
    echo         winget deneniyor...
    winget install --id Docker.DockerDesktop -e --silent ^
        --accept-source-agreements --accept-package-agreements ^
        --disable-interactivity >nul 2>&1
    if not errorlevel 1 (
        echo         Docker Desktop winget ile kuruldu.
        exit /b 0
    )
    echo         winget basarisiz, curl ile indirme deneniyor...
)

:: --- Yontem 2: curl.exe ile indir (Windows 10+ dahili gelir, TLS sorunsuz) ---
set "INST=%TEMP%\DockerSetup.exe"
set "DURL=https://desktop.docker.com/win/main/amd64/DockerDesktopInstaller.exe"

echo         Docker Desktop indiriliyor (~600 MB, lutfen bekleyin)...
curl.exe -L --progress-bar --retry 3 --retry-delay 5 -o "%INST%" "%DURL%"

if not exist "%INST%" (
    :: curl yoksa PowerShell TLS12 ile dene
    echo         curl basarisiz, PowerShell ile deneniyor...
    powershell -NoProfile -Command ^
        "[Net.ServicePointManager]::SecurityProtocol='Tls12,Tls13';" ^
        "Invoke-WebRequest -Uri '%DURL%' -OutFile '%INST%' -UseBasicParsing"
)

if not exist "%INST%" (
    echo.
    echo  [HATA] Indirme basarisiz.
    echo  Lutfen su adresten elle kurun ve tekrar calistirin:
    echo  https://docs.docker.com/desktop/install/windows-install/
    exit /b 1
)

echo         Yukleniyor (Kullanici Hesabi Denetimi penceresi acilabilir)...
"%INST%" install --quiet --accept-license
set INST_ERR=%ERRORLEVEL%
del "%INST%" >nul 2>&1

if %INST_ERR% neq 0 (
    echo  [HATA] Docker kurulum basarisiz (kod: %INST_ERR%).
    echo  Lutfen manuel kurun: https://docs.docker.com/desktop/install/windows-install/
    exit /b 1
)

echo         Docker Desktop kuruldu. Devam ediliyor...
exit /b 0
