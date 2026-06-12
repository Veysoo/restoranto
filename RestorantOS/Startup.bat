@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Akilli Baslangic

echo.
echo  ================================================
echo    RestaurantOS  -  Akilli Baslangic
echo    Docker otomatik kur + PC restartinda oto-ac
echo  ================================================
echo.

:: Uygulama klasoru (bu .bat nerede olursa olsun dogru calisir)
set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"

:: ============================================
:: ADIM 1: Docker kurulu mu kontrol et
:: ============================================
echo  [1/6]  Docker kontrol ediliyor...

where docker >nul 2>&1
if not errorlevel 1 goto :docker_found
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" goto :docker_found
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" goto :docker_found

echo         Docker bulunamadi. Otomatik kuruluyor...
echo.
call :fn_install_docker
if errorlevel 1 (
    echo.
    echo  [HATA] Docker kurulumu tamamlanamadi.
    echo  Elle kurun: https://docs.docker.com/desktop/install/windows-install/
    pause
    exit /b 1
)
:: PATH guncelle
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:docker_found
echo         Docker kurulu.  OK

:: ============================================
:: ADIM 2: Docker Desktop calisiyor mu?
:: ============================================
echo  [2/6]  Docker servisi kontrol ediliyor...

docker info >nul 2>&1
if not errorlevel 1 goto :docker_running

echo         Docker calısmiyor. Baslatiliyor...

:: Docker Desktop konumunu bul
set "DEXE="
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"              set "DEXE=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"     set "DEXE=%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"

if not defined DEXE (
    echo  [HATA] Docker Desktop bulunamadi. Adim 1 basarisiz olmali.
    pause
    exit /b 1
)

start "" "!DEXE!"
echo         Basladi. Hazir olana kadar bekleniyor (max 3 dk)...

set /a dw=0
:wait_docker_loop
ping -n 5 127.0.0.1 >nul
set /a dw+=1
docker info >nul 2>&1
if not errorlevel 1 goto :docker_running
set /a elapsed=!dw!*5
if !dw! lss 36 (
    echo         !elapsed! sn...
    goto :wait_docker_loop
)
echo  [HATA] Docker 3 dakikada hazir olmadi.
echo  Docker Desktop'i elle acip tekrar deneyin.
pause
exit /b 1

:docker_running
echo         Docker servisi hazir.  OK

:: ============================================
:: ADIM 3: Ag IP + .env
:: ============================================
echo  [3/6]  Ag yapılandırması...

set "PS_IP=Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -notlike '172.25.*' -and $_.IPAddress -notlike '192.168.56.*' } | Select-Object -First 1 -ExpandProperty IPAddress"
set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "%PS_IP%"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

echo HOST_LAN_IP=%HOST_LAN_IP%  > "%APP%\.env"
echo EXTERNAL_PORT=8080         >> "%APP%\.env"

:: Guvenlik duvari ayarlari (UAC ile yonetici izni ister)
echo         Guvenlik duvari ayarlaniyor...
echo         (Kucuk bir izin penceresi acilacak - Evet deyin)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%APP%\scripts\setup-network.ps1""' -Verb RunAs -Wait"
echo         Ag kurulumu tamamlandi.  OK

echo         LAN IP: %HOST_LAN_IP%  OK

:: ============================================
:: ADIM 4: Container'lari baslat / guncelle
:: ============================================
echo  [4/6]  Container'lar baslatiliyor (ilk seferde build 3-5 dk surebilir)...

:: sanal-network yoksa olustur (docker-compose external network hatasi onlenir)
docker network inspect sanal-network >nul 2>&1
if errorlevel 1 (
    echo         sanal-network olusturuluyor...
    docker network create sanal-network >nul 2>&1
)

docker-compose -f "%APP%\docker-compose.yml" --env-file "%APP%\.env" up -d --build
if errorlevel 1 (
    echo.
    echo  [HATA] Container baslatma basarisiz. Son loglar:
    docker-compose -f "%APP%\docker-compose.yml" logs --tail=25
    pause
    exit /b 1
)
echo         Container'lar calistirildi.  OK

:: Nginx/network (isteğe bagli, hata verirse devam et)
docker network connect sanal-network restaurantos-app >nul 2>&1
docker exec sanalsirket-nginx nginx -s reload >nul 2>&1

:: ============================================
:: ADIM 5: Docker Desktop + autostart kayıt
:: ============================================
echo  [5/6]  PC restart sonrası oto-baslat ayarlanıyor...

:: 5a. Docker Desktop Windows baslangicina ekle
if defined DEXE (
    reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "Docker Desktop" /t REG_SZ /d "\"!DEXE!\"" /f >nul 2>&1
)

:: 5b. Gorev Zamanlayiciya RestaurantOS autostart ekle
set "ASTART=%APP%\scripts\autostart.bat"
schtasks /delete /tn "RestaurantOS-AutoStart" /f >nul 2>&1
schtasks /create /tn "RestaurantOS-AutoStart" ^
    /tr "\"%ASTART%\"" ^
    /sc ONLOGON ^
    /delay 0001:30 ^
    /ru "%USERDOMAIN%\%USERNAME%" ^
    /rl HIGHEST ^
    /f >nul 2>&1
if not errorlevel 1 (
    echo         Gorev Zamanlayici kaydi yapildi.  OK
) else (
    :: Admin olmadan HIGHEST calismaz, normal seviyede dene
    schtasks /create /tn "RestaurantOS-AutoStart" ^
        /tr "\"%ASTART%\"" ^
        /sc ONLOGON ^
        /delay 0001:30 ^
        /f >nul 2>&1
    echo         Gorev Zamanlayici kaydi yapildi ^(normal seviye^).  OK
)

:: hosts
findstr /c:"restaurantos.local" "%SystemRoot%\System32\drivers\etc\hosts" >nul 2>&1
if errorlevel 1 (
    >> "%SystemRoot%\System32\drivers\etc\hosts" echo 127.0.0.1 restaurantos.local
    ipconfig /flushdns >nul 2>&1
)

:: ============================================
:: ADIM 6: Uygulama hazir bekleniyor
:: ============================================
echo  [6/6]  Uygulama baslatiliyor...
set /a tries=0
:waitloop
set /a tries+=1
if !tries! geq 72 goto :show_info
docker inspect --format="{{.State.Health.Status}}" restaurantos-app 2>nul | findstr /i "healthy" >nul 2>&1
if not errorlevel 1 goto :show_info
ping -n 5 127.0.0.1 >nul
goto :waitloop

:show_info
echo.
echo  ================================================
echo    RestaurantOS HAZIR!
echo  ================================================
echo.
echo    Bu PC         : http://localhost:8080
echo    Ag erisimi    : http://%HOST_LAN_IP%:8080
echo    Domain        : http://restaurantos.local
echo    Mobil         : QR kodu tarayin
echo.
echo    Kullanici     : admin
echo    Sifre         : Resto@Admin2024!
echo.
echo    PC YENIDEN BASLATILDIGINDA OTOMATIK ACAR!
echo    (Gorev: RestaurantOS-AutoStart)
echo  ================================================
echo.
start "" "http://localhost:8080"
pause
goto :EOF

:: ============================================
:: FONKSIYON: Docker Desktop Kur
:: ============================================
:fn_install_docker

:: PATH disinda kurulu olabilir, once exe konumlarini kontrol et
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          exit /b 0
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" exit /b 0

echo         Docker Desktop bulunamadi. Kurulum baslatiliyor...

:: --- Yontem 1: winget (sessiz, tam otomatik) ---
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

:: --- Yontem 2: curl.exe ile indir (Windows 10+ dahili, TLS sorunsuz) ---
set "INST=%TEMP%\DockerSetup.exe"
set "DURL=https://desktop.docker.com/win/main/amd64/DockerDesktopInstaller.exe"

echo         Docker Desktop indiriliyor (~600 MB, lutfen bekleyin...^)
curl.exe -L --progress-bar --retry 3 --retry-delay 5 -o "%INST%" "%DURL%"

if not exist "%INST%" (
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

echo         Yukleniyor ^(Kullanici Hesabi Denetimi penceresi acilabilir^)...
"%INST%" install --quiet --accept-license
set ERR=%ERRORLEVEL%
del "%INST%" >nul 2>&1

if %ERR% neq 0 (
    echo  [HATA] Kurulum basarisiz ^(kod: %ERR%^).
    echo  Lutfen manuel kurun: https://docs.docker.com/desktop/install/windows-install/
    exit /b 1
)

echo         Docker Desktop kuruldu.
exit /b 0
