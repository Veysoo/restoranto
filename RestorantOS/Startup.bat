@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul 2>&1
title RestaurantOS - Kurulum ve Baslatma

echo.
echo  ╔══════════════════════════════════════════════════╗
echo  ║        RestaurantOS  -  Tam Kurulum              ║
echo  ║   Docker + Veritabani + Uygulama + Ag Erisimi   ║
echo  ╚══════════════════════════════════════════════════╝
echo.

:: Uygulama klasoru (bu .bat neredeyse orasi)
set "APP=%~dp0"
if "%APP:~-1%"=="\" set "APP=%APP:~0,-1%"

:: Log klasoru
if not exist "%APP%\logs" mkdir "%APP%\logs"
set "LOG=%APP%\logs\startup.log"
echo [%date% %time%] === Startup basladi === >> "%LOG%"

:: ============================================
:: ADIM 0: Yonetici yetkisi kontrol
:: ============================================
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo  [0/7] Yonetici yetkisi gerekiyor. Yetki isteniyor...
    echo.
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "Start-Process -FilePath '%~f0' -Verb RunAs -Wait"
    exit /b %errorlevel%
)
echo  [0/7] Yonetici yetkisi mevcut.  OK

:: ============================================
:: ADIM 1: Sanallaştirma kontrol
:: ============================================
echo  [1/7] Sanallaştirma kontrol ediliyor...

:: Docker PATH'e ekle
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:: Docker zaten kuruluysa WSL kontrolunu atla (Docker kendi halleder)
where docker >nul 2>&1 && goto :wsl_done
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe" goto :wsl_done
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" goto :wsl_done

:: Docker yok, WSL2/Sanallaştirma gerekli
echo         Docker kurulmamis, sanallaştirma kontrol ediliyor...
dism /online /get-featureinfo /featurename:VirtualMachinePlatform 2>nul | findstr /c:"State : Enabled" >nul 2>&1
if not errorlevel 1 goto :wsl_done

echo         Sanallaştirma etkinlestiriliyor...
dism /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart >nul 2>&1
echo [%date% %time%] Sanallaştirma etkinlestirildi >> "%LOG%"

dism /online /get-featureinfo /featurename:VirtualMachinePlatform 2>nul | findstr /c:"State : Enabled" >nul 2>&1
if errorlevel 1 (
    echo.
    echo  ╔══════════════════════════════════════════════════╗
    echo  ║  Sanallaştirma etkinlestirildi. BILGISAYAR        ║
    echo  ║  YENIDEN BASLATILMALI. Restart sonrasi bu .bat    ║
    echo  ║  dosyasini tekrar calistirin.                     ║
    echo  ╚══════════════════════════════════════════════════╝
    echo.
    set /p RESTART="  Simdi yeniden baslatilsin mi? (E/H): "
    if /i "!RESTART!"=="E" shutdown /r /t 10 /c "RestaurantOS icin yeniden baslatiliyor"
    pause
    exit /b 0
)

:wsl_done
echo         Sanallaştirma hazir.  OK

:: ============================================
:: ADIM 2: Docker kurulu mu kontrol et
:: ============================================
echo  [2/7] Docker kontrol ediliyor...

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
    echo [%date% %time%] Docker kurulum BASARISIZ >> "%LOG%"
    pause
    exit /b 1
)
:: PATH yeniden guncelle
set "PATH=%ProgramFiles%\Docker\Docker\resources\bin;%LocalAppData%\Programs\Docker\Docker\resources\bin;%PATH%"

:docker_found
echo         Docker kurulu.  OK

:: ============================================
:: ADIM 3: Docker Desktop baslatilsin
:: ============================================
echo  [3/7] Docker servisi kontrol ediliyor...

docker info >nul 2>&1
if not errorlevel 1 goto :docker_running

echo         Docker calısmiyor. Baslatiliyor...

set "DEXE="
if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"              set "DEXE=%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"     set "DEXE=%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe"

if not defined DEXE (
    echo  [HATA] Docker Desktop bulunamadi.
    echo [%date% %time%] Docker Desktop exe bulunamadi >> "%LOG%"
    pause
    exit /b 1
)

start "" "!DEXE!"
echo         Docker Desktop baslatildi. Hazir olana kadar bekleniyor (max 5 dk)...

set /a dw=0
:wait_docker_loop
ping -n 6 127.0.0.1 >nul
set /a dw+=1
docker info >nul 2>&1
if not errorlevel 1 goto :docker_running
set /a elapsed=!dw!*5
if !dw! lss 60 (
    echo         !elapsed! sn beklendi...
    goto :wait_docker_loop
)
echo  [HATA] Docker 5 dakikada hazir olmadi.
echo  Docker Desktop'i elle acip tekrar deneyin.
echo [%date% %time%] Docker timeout >> "%LOG%"
pause
exit /b 1

:docker_running
echo         Docker servisi hazir.  OK

:: ============================================
:: ADIM 4: Ag IP tespit + .env yazilacak
:: ============================================
echo  [4/7] Ag yapilandiriliyor...

:: IP tespiti: scripts\get-lan-ip.ps1 ile
set "HOST_LAN_IP="
for /f "usebackq delims=" %%i in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%APP%\scripts\get-lan-ip.ps1"`) do set HOST_LAN_IP=%%i
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

:: .env dosyasini yaz
echo HOST_LAN_IP=%HOST_LAN_IP%> "%APP%\.env"
echo EXTERNAL_PORT=8080>> "%APP%\.env"

echo         LAN IP: %HOST_LAN_IP%  OK
echo [%date% %time%] LAN IP: %HOST_LAN_IP% >> "%LOG%"

:: ============================================
:: ADIM 5: Guvenlik Duvari + Ag kurallari
:: ============================================
echo  [5/7] Guvenlik duvari ve ag kurallari ayarlaniyor...

:: Eski kurallari temizle
netsh advfirewall firewall delete rule name="RestaurantOS-8080" >nul 2>&1
netsh advfirewall firewall delete rule name="RestaurantOS-HTTP" >nul 2>&1

:: Yeni kurallar ekle (gelen TCP 8080)
netsh advfirewall firewall add rule name="RestaurantOS-8080" dir=in action=allow protocol=TCP localport=8080 profile=any >nul 2>&1
netsh advfirewall firewall add rule name="RestaurantOS-HTTP" dir=in action=allow protocol=TCP localport=80 profile=any >nul 2>&1

:: Eski portproxy kalintilari varsa temizle
netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 >nul 2>&1
netsh interface portproxy delete v4tov4 listenport=80 listenaddress=0.0.0.0 >nul 2>&1

echo         Guvenlik duvari ayarlandi.  OK
echo [%date% %time%] Firewall kurallari ayarlandi >> "%LOG%"

:: ============================================
:: ADIM 6: Container'lari build + baslat
:: ============================================
echo  [6/7] Container'lar baslatiliyor...
echo         (Ilk seferde build 3-8 dk surebilir, lutfen bekleyin)

:: docker compose mu docker-compose mu?
set "DC=docker compose"
docker compose version >nul 2>&1
if errorlevel 1 (
    where docker-compose >nul 2>&1
    if errorlevel 1 (
        echo  [HATA] docker compose komutu bulunamadi.
        echo [%date% %time%] docker compose bulunamadi >> "%LOG%"
        pause
        exit /b 1
    )
    set "DC=docker-compose"
)

%DC% -f "%APP%\docker-compose.yml" --env-file "%APP%\.env" up -d --build
if errorlevel 1 (
    echo.
    echo  [HATA] Container baslatma basarisiz. Loglar kontrol ediliyor...
    %DC% -f "%APP%\docker-compose.yml" logs --tail=30
    echo.
    echo  Olasi cozumler:
    echo    1. Docker Desktop'in tamamen acilmasini bekleyin
    echo    2. "docker system prune -a" ile temizlik yapin
    echo    3. Bu .bat dosyasini tekrar calistirin
    echo [%date% %time%] docker compose BASARISIZ >> "%LOG%"
    pause
    exit /b 1
)
echo         Container'lar calistirildi.  OK
echo [%date% %time%] Containerlar baslatildi >> "%LOG%"

:: ============================================
:: ADIM 7: Autostart + uygulama hazir bekleme
:: ============================================
echo  [7/7] Son ayarlar ve baslatma...

:: Docker Desktop otomatik baslat
if defined DEXE (
    reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "Docker Desktop" /t REG_SZ /d "\"!DEXE!\"" /f >nul 2>&1
)

:: Gorev Zamanlayici: PC acilinca otomatik baslat
set "ASTART=%APP%\scripts\autostart.bat"
if exist "!ASTART!" (
    schtasks /delete /tn "RestaurantOS-AutoStart" /f >nul 2>&1
    schtasks /create /tn "RestaurantOS-AutoStart" ^
        /tr "\"!ASTART!\"" ^
        /sc ONLOGON ^
        /delay 0001:30 ^
        /ru "%USERDOMAIN%\%USERNAME%" ^
        /rl HIGHEST ^
        /f >nul 2>&1
    if not errorlevel 1 (
        echo         Otomatik baslatma kaydi yapildi.  OK
    ) else (
        schtasks /create /tn "RestaurantOS-AutoStart" ^
            /tr "\"!ASTART!\"" ^
            /sc ONLOGON ^
            /delay 0001:30 ^
            /f >nul 2>&1
        echo         Otomatik baslatma kaydi yapildi.  OK
    )
)

:: Uygulama hazir mi bekleme
echo.
echo         Uygulama baslatiliyor, hazir olana kadar bekleniyor...
set /a tries=0
:waitloop
set /a tries+=1
if !tries! gtr 90 goto :timeout

:: Health check - curl veya powershell ile
curl.exe -s -o nul -w "%%{http_code}" http://localhost:8080/api/health 2>nul | findstr /b "200" >nul 2>&1
if not errorlevel 1 goto :app_ready

powershell -NoProfile -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:8080/api/health' -UseBasicParsing -TimeoutSec 3; if($r.StatusCode -eq 200){'OK'} } catch {}" 2>nul | findstr "OK" >nul 2>&1
if not errorlevel 1 goto :app_ready

set /a remaining=90-!tries!
if !remaining! gtr 0 (
    if !tries! lss 10 (
        echo         Veritabani olusturuluyor... ^(!remaining! kontrol kaldi^)
    ) else (
        echo         Bekleniyor... ^(!remaining! kontrol kaldi^)
    )
)
ping -n 4 127.0.0.1 >nul
goto :waitloop

:app_ready
echo.
echo  ╔══════════════════════════════════════════════════╗
echo  ║          RestaurantOS KURULUM TAMAMLANDI!        ║
echo  ╠══════════════════════════════════════════════════╣
echo  ║                                                  ║
echo  ║  Bu PC'den erisim:                               ║
echo  ║    http://localhost:8080                          ║
echo  ║                                                  ║
echo  ║  Agdaki diger cihazlardan erisim:                ║
echo  ║    http://%HOST_LAN_IP%:8080                     ║
echo  ║                                                  ║
echo  ║  Tablet/Telefon:                                 ║
echo  ║    Ayni WiFi'ye baglanin ve yukaridaki adresi    ║
echo  ║    tarayiciya yazin.                             ║
echo  ║                                                  ║
echo  ╠══════════════════════════════════════════════════╣
echo  ║  Kullanici: admin                                ║
echo  ║  Sifre    : Resto@Admin2024!                     ║
echo  ╠══════════════════════════════════════════════════╣
echo  ║  PC yeniden baslatildiginda OTOMATIK ACILIR!     ║
echo  ╚══════════════════════════════════════════════════╝
echo.

echo [%date% %time%] BASARILI - http://%HOST_LAN_IP%:8080 >> "%LOG%"

start "" "http://localhost:8080"
pause
goto :EOF

:timeout
echo.
echo  ╔══════════════════════════════════════════════════╗
echo  ║  [UYARI] Uygulama henuz hazir olmadi.            ║
echo  ╠══════════════════════════════════════════════════╣
echo  ║  Veritabani ilk kez olusturuluyorsa biraz daha   ║
echo  ║  bekleyin. Genellikle 1-2 dakika yeterlidir.     ║
echo  ║                                                  ║
echo  ║  Kontrol icin:                                   ║
echo  ║    docker logs restaurantos-app --tail 30         ║
echo  ║    docker logs restaurantos-sqlserver --tail 15   ║
echo  ║                                                  ║
echo  ║  Tekrar denemek icin bu .bat'i calistirin.       ║
echo  ╚══════════════════════════════════════════════════╝
echo.
echo [%date% %time%] TIMEOUT - uygulama hazir olmadi >> "%LOG%"

set /p OPENBROWSER="  Yine de tarayiciyi acayim mi? (E/H): "
if /i "!OPENBROWSER!"=="E" start "" "http://localhost:8080"
pause
goto :EOF

:: ============================================
:: FONKSIYON: Docker Desktop Kur
:: ============================================
:fn_install_docker

if exist "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"          exit /b 0
if exist "%LocalAppData%\Programs\Docker\Docker\Docker Desktop.exe" exit /b 0

echo         Docker Desktop bulunamadi. Kurulum baslatiliyor...

:: Internet kontrol
ping -n 1 -w 3000 8.8.8.8 >nul 2>&1
if errorlevel 1 (
    echo  [HATA] Internet baglantisi yok! Docker kurulamaz.
    echo  Lutfen internet baglantisini kontrol edin.
    exit /b 1
)

:: --- Yontem 1: winget ---
where winget >nul 2>&1
if not errorlevel 1 (
    echo         winget ile kuruluyor (sessiz mod)...
    winget install --id Docker.DockerDesktop -e --silent ^
        --accept-source-agreements --accept-package-agreements ^
        --disable-interactivity
    if not errorlevel 1 (
        echo         Docker Desktop winget ile kuruldu.
        exit /b 0
    )
    echo         winget basarisiz, dogrudan indirme deneniyor...
)

:: --- Yontem 2: curl.exe ile indir ---
set "INST=%TEMP%\DockerSetup.exe"
set "DURL=https://desktop.docker.com/win/main/amd64/DockerSetupInstaller.exe"

echo         Docker Desktop indiriliyor (~600 MB)...
echo         Lutfen bekleyin, bu biraz zaman alabilir...

curl.exe -L --progress-bar --retry 3 --retry-delay 5 -o "%INST%" "%DURL%"

:: curl basarisizsa PowerShell dene
if not exist "%INST%" (
    set "DURL=https://desktop.docker.com/win/main/amd64/DockerDesktopInstaller.exe"
    curl.exe -L --progress-bar --retry 3 --retry-delay 5 -o "%INST%" "!DURL!"
)

if not exist "%INST%" (
    echo         curl basarisiz, PowerShell ile deneniyor...
    powershell -NoProfile -Command ^
        "[Net.ServicePointManager]::SecurityProtocol='Tls12,Tls13';" ^
        "$ProgressPreference='SilentlyContinue';" ^
        "Invoke-WebRequest -Uri 'https://desktop.docker.com/win/main/amd64/DockerDesktopInstaller.exe' -OutFile '%INST%' -UseBasicParsing"
)

if not exist "%INST%" (
    echo.
    echo  [HATA] Docker indirme basarisiz.
    echo  Lutfen su adresten elle kurun:
    echo  https://docs.docker.com/desktop/install/windows-install/
    exit /b 1
)

echo         Docker Desktop yukleniyor...
echo         (UAC izin penceresi acilabilir - Evet deyin)
"%INST%" install --quiet --accept-license
set "INST_ERR=!ERRORLEVEL!"
del "%INST%" >nul 2>&1

if !INST_ERR! neq 0 (
    echo  [HATA] Docker kurulum basarisiz (kod: !INST_ERR!).
    echo  Lutfen manuel kurun: https://docs.docker.com/desktop/install/windows-install/
    exit /b 1
)

echo         Docker Desktop kuruldu.
exit /b 0
