@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Kurulum ve Baslangic

echo.
echo  ╔══════════════════════════════════════════════╗
echo  ║   RestaurantOS  -  Kurulum ve Baslangic      ║
echo  ║   Sifir bilgisayarda her seyi otomatik yapar ║
echo  ╚══════════════════════════════════════════════╝
echo.

set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"

if not exist "%APP%\logs" mkdir "%APP%\logs"
set "LOG=%APP%\logs\kurulum.log"
echo [%date% %time%] Kurulum basladi > "%LOG%"

:: ══════════════════════════════════════════
:: ADIM 1 — Docker kurulum kontrolu
:: ══════════════════════════════════════════
echo  [1/6] Docker kontrol ediliyor...

set "DOCKER_OK=0"
where docker >nul 2>&1 && set DOCKER_OK=1
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          set DOCKER_OK=1
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" set DOCKER_OK=1

if "%DOCKER_OK%"=="0" (
    echo         Docker bulunamadi. Kuruluyor...
    echo         Bu islem 5-15 dakika surebilir, bekleyin.
    echo.
    call :fn_docker_kur
    if errorlevel 1 (
        echo.
        echo  ══════════════════════════════════════════════
        echo  [HATA] Docker kurulamadi.
        echo  Lutfen elle kurun: https://docs.docker.com/desktop/install/windows-install/
        echo  Kurduktan sonra bu dosyayi tekrar calistirin.
        echo  ══════════════════════════════════════════════
        pause
        exit /b 1
    )
    set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"
) else (
    echo         Docker kurulu.  OK
)
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:: ══════════════════════════════════════════
:: ADIM 2 — Docker servisi baslat
:: ══════════════════════════════════════════
echo  [2/6] Docker servisi kontrol ediliyor...

docker info >nul 2>&1
if errorlevel 1 (
    echo         Docker calısmiyor. Baslatiliyor...
    set "DEXE="
    if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          set "DEXE=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
    if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" set "DEXE=%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"

    if defined DEXE (
        start "" "!DEXE!"
        echo         Docker baslatildi. Hazir olana kadar bekleniyor (max 3 dk)...
        set /a dw=0
        :docker_bekle
        ping -n 5 127.0.0.1 >nul
        set /a dw+=1
        set /a sn=!dw!*5
        docker info >nul 2>&1 && goto :docker_hazir
        if !dw! lss 36 ( echo         !sn! sn... & goto :docker_bekle )
        echo  [HATA] Docker 3 dk'da hazir olmadi. Docker Desktop'i elle acin.
        pause & exit /b 1
    ) else (
        echo  [HATA] Docker Desktop exe bulunamadi.
        pause & exit /b 1
    )
)
:docker_hazir
echo         Docker servisi hazir.  OK

:: Docker'i Windows baslangicina ekle
set "DEXE="
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          set "DEXE=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" set "DEXE=%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"
if defined DEXE reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "Docker Desktop" /t REG_SZ /d "\"!DEXE!\"" /f >nul 2>&1

:: ══════════════════════════════════════════
:: ADIM 3 — Ag IP + .env
:: ══════════════════════════════════════════
echo  [3/6] Ag yapilandirmasi...

set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -notlike '172.25.*' -and $_.IPAddress -notlike '192.168.56.*' } | Select-Object -First 1 -ExpandProperty IPAddress"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

echo HOST_LAN_IP=%HOST_LAN_IP%  > "%APP%\.env"
echo EXTERNAL_PORT=8080         >> "%APP%\.env"

echo         LAN IP: %HOST_LAN_IP%  OK
echo [%date% %time%] LAN IP: %HOST_LAN_IP% >> "%LOG%"

:: ══════════════════════════════════════════
:: ADIM 4 — Guvenlik duvari + Portproxy
::          (UAC ile tek seferlik admin islemi)
:: ══════════════════════════════════════════
echo  [4/6] Guvenlik duvari ve portproxy ayarlaniyor...
echo         (Kucuk bir izin penceresi acilacak — Evet deyin)

:: admin PS1 TEMP'e yaz
set "PS_NET=%TEMP%\ros_network_%RANDOM%.ps1"

echo Write-Host '  Guvenlik duvari...' -NoNewline                                          >  "%PS_NET%"
echo netsh advfirewall firewall delete rule name='RestaurantOS-8080' 2>&1 ^| Out-Null      >> "%PS_NET%"
echo netsh advfirewall firewall delete rule name='RestaurantOS-HTTP'  2>&1 ^| Out-Null     >> "%PS_NET%"
echo netsh advfirewall firewall add rule name='RestaurantOS-8080' dir=in action=allow protocol=TCP localport=8080 profile=any 2>&1 ^| Out-Null >> "%PS_NET%"
echo netsh advfirewall firewall add rule name='RestaurantOS-HTTP'  dir=in action=allow protocol=TCP localport=80  profile=any 2>&1 ^| Out-Null >> "%PS_NET%"
echo Write-Host ' OK'                                                                       >> "%PS_NET%"
echo Write-Host '  Portproxy (Docker dis ag)...' -NoNewline                                >> "%PS_NET%"
echo netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 2>&1 ^| Out-Null >> "%PS_NET%"
echo netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=8080 connectaddress=127.0.0.1 >> "%PS_NET%"
echo Write-Host ' OK'                                                                       >> "%PS_NET%"
echo Write-Host '  hosts dosyasi...' -NoNewline                                            >> "%PS_NET%"
echo $f="$env:SystemRoot\System32\drivers\etc\hosts"                                       >> "%PS_NET%"
echo $ip='%HOST_LAN_IP%'                                                                   >> "%PS_NET%"
echo $e="$ip`t`trestaurantos.local"                                                        >> "%PS_NET%"
echo $lines=Get-Content $f                                                                  >> "%PS_NET%"
echo if($lines ^| Where-Object {$_ -match 'restaurantos\.local'}){                         >> "%PS_NET%"
echo   $lines=$lines ^| ForEach-Object{if($_ -match 'restaurantos\.local'){$e}else{$_}}    >> "%PS_NET%"
echo   Set-Content $f $lines -Encoding UTF8                                                 >> "%PS_NET%"
echo }else{ Add-Content $f "`n$e" -Encoding UTF8 }                                         >> "%PS_NET%"
echo ipconfig /flushdns ^| Out-Null                                                         >> "%PS_NET%"
echo Write-Host ' OK'                                                                       >> "%PS_NET%"
echo Start-Sleep 2                                                                          >> "%PS_NET%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%PS_NET%""' -Verb RunAs -Wait"
del "%PS_NET%" >nul 2>&1

echo  [4/6] Guvenlik duvari + portproxy + hosts.  OK

:: ══════════════════════════════════════════
:: ADIM 5 — Container'lari baslat
:: ══════════════════════════════════════════
echo  [5/6] Container'lar baslatiliyor...
echo         (Ilk seferde image build 3-5 dk surebilir)

docker network inspect sanal-network >nul 2>&1
if errorlevel 1 (
    echo         sanal-network olusturuluyor...
    docker network create sanal-network >nul 2>&1
)

docker-compose -f "%APP%\docker-compose.yml" --env-file "%APP%\.env" up -d --build
if errorlevel 1 (
    echo.
    echo  [HATA] Container baslatma basarisiz. Son loglar:
    docker-compose -f "%APP%\docker-compose.yml" logs --tail=30
    echo [%date% %time%] docker-compose HATA >> "%LOG%"
    pause
    exit /b 1
)
echo         Container'lar baslatildi.  OK
echo [%date% %time%] Container'lar OK >> "%LOG%"

:: ══════════════════════════════════════════
:: ADIM 6 — Oto-baslat gorevi kaydet
:: ══════════════════════════════════════════
echo  [6/6] PC yeniden baslatinca oto-ac ayarlaniyor...

set "ASTART=%APP%\scripts\autostart.bat"
schtasks /delete /tn "RestaurantOS-AutoStart" /f >nul 2>&1
schtasks /create /tn "RestaurantOS-AutoStart" /tr "\"%ASTART%\"" /sc ONLOGON /delay 0001:30 /rl HIGHEST /f >nul 2>&1
if errorlevel 1 schtasks /create /tn "RestaurantOS-AutoStart" /tr "\"%ASTART%\"" /sc ONLOGON /delay 0001:30 /f >nul 2>&1

echo         Gorev Zamanlayici kaydi yapildi.  OK

:: ══════════════════════════════════════════
:: Uygulama hazir bekleniyor
:: ══════════════════════════════════════════
echo.
echo  Uygulama hazir bekleniyor...
set /a tries=0
:waitloop
set /a tries+=1
if !tries! geq 72 goto :hazir
docker inspect --format="{{.State.Health.Status}}" restaurantos-app 2>nul | findstr /i "healthy" >nul 2>&1
if not errorlevel 1 goto :hazir
ping -n 5 127.0.0.1 >nul
goto :waitloop

:hazir
echo.
echo  ╔══════════════════════════════════════════════╗
echo  ║         RestaurantOS HAZIR!                  ║
echo  ╚══════════════════════════════════════════════╝
echo.
echo    Bu PC         : http://localhost:8080
echo    Agdaki erisim : http://%HOST_LAN_IP%:8080
echo    Domain        : http://restaurantos.local
echo    Mobil         : QR kodu tarayin
echo.
echo    Kullanici     : admin
echo    Sifre         : Resto@Admin2024!
echo.
echo    PC yeniden baslatilsa bile OTOMATIK ACAR!
echo.
echo  ══════════════════════════════════════════════
echo.
echo [%date% %time%] Kurulum tamamlandi - %HOST_LAN_IP%:8080 >> "%LOG%"
start "" "http://localhost:8080"
pause
goto :EOF

:: ══════════════════════════════════════════
:: FONKSIYON: Docker kur
:: ══════════════════════════════════════════
:fn_docker_kur

:: Yontem 1: winget
where winget >nul 2>&1
if not errorlevel 1 (
    echo         winget ile Docker Desktop kuruluyor...
    winget install --id Docker.DockerDesktop -e --silent ^
        --accept-source-agreements --accept-package-agreements ^
        --disable-interactivity
    if not errorlevel 1 ( echo         Docker winget ile kuruldu. & exit /b 0 )
    echo         winget basarisiz, dogrudan indirme deneniyor...
)

:: Yontem 2: curl ile indir (Windows 10+ dahili, TLS sorunsuz)
set "INST=%TEMP%\DockerSetup.exe"
set "DURL=https://desktop.docker.com/win/main/amd64/DockerDesktopInstaller.exe"
echo         Docker Desktop indiriliyor (~600 MB)...
curl.exe -L --progress-bar --retry 3 -o "%INST%" "%DURL%" 2>nul

:: Yontem 3: curl basarisizsa PowerShell TLS12
if not exist "%INST%" (
    echo         PowerShell ile indiriliyor...
    powershell -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol='Tls12,Tls13'; Invoke-WebRequest -Uri '%DURL%' -OutFile '%INST%' -UseBasicParsing"
)

if not exist "%INST%" ( echo  [HATA] Indirme basarisiz. & exit /b 1 )

echo         Yukleniyor (admin izni istenebilir)...
"%INST%" install --quiet --accept-license
set IERR=%ERRORLEVEL%
del "%INST%" >nul 2>&1
if %IERR% neq 0 ( echo  [HATA] Kurulum basarisiz (kod: %IERR%). & exit /b 1 )
echo         Docker Desktop kuruldu.
exit /b 0
