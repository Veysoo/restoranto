@echo off
setlocal EnableDelayedExpansion
title RestaurantOS

echo ========================================
echo   RestaurantOS - Baslatiliyor
echo ========================================
echo.

:: Docker calisiyor mu?
docker info >nul 2>&1
if errorlevel 1 (
    echo [HATA] Docker calismiyor. Docker Desktop'i baslatin.
    pause
    exit /b 1
)

:: LAN IP bul - PowerShell ile gercek ethernet/wifi IP'si
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notmatch '^(127\.|169\.254\.|172\.|192\.168\.5[0-9]\.)' } | Sort-Object { [Version]$_.IPAddress } -Descending | Select-Object -First 1 -ExpandProperty IPAddress"`) do (
    set HOST_LAN_IP=%%i
)
if not defined HOST_LAN_IP set HOST_LAN_IP=localhost

:: .env yaz
(
    echo HOST_LAN_IP=%HOST_LAN_IP%
    echo EXTERNAL_PORT=8080
) > "%~dp0.env"

:: Firewall kurallari
netsh advfirewall firewall add rule name="RestaurantOS-8080" dir=in action=allow protocol=TCP localport=8080 >nul 2>&1
netsh advfirewall firewall add rule name="RestaurantOS-HTTP"  dir=in action=allow protocol=TCP localport=80  >nul 2>&1

echo LAN IP : %HOST_LAN_IP%
echo.

:: Rebuild ve baslat
echo [1/4] Docker build ve baslat...
docker-compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" up -d --build
if errorlevel 1 (
    echo.
    echo [HATA] Docker baslatma basarisiz.
    docker-compose -f "%~dp0docker-compose.yml" logs --tail=20
    pause
    exit /b 1
)

:: sanal-network'e baglan + nginx reload
echo [2/4] Ag ve nginx ayarlaniyor...
docker network connect sanal-network restaurantos-app >nul 2>&1
docker exec sanalsirket-nginx nginx -s reload >nul 2>&1
echo     OK.

:: hosts dosyasina restaurantos.local ekle (yonetici gerekir)
echo [3/4] Hosts dosyasi kontrol ediliyor...
findstr /c:"restaurantos.local" "%SystemRoot%\System32\drivers\etc\hosts" >nul 2>&1
if not errorlevel 1 goto :hosts_ok
>> "%SystemRoot%\System32\drivers\etc\hosts" echo 127.0.0.1 restaurantos.local
if not errorlevel 1 (
    echo     OK: restaurantos.local eklendi.
    ipconfig /flushdns >nul 2>&1
) else (
    echo     NOT: Setup-Hosts.bat dosyasini yonetici olarak calistirin.
)
goto :hosts_done
:hosts_ok
echo     OK: restaurantos.local zaten kayitli.
:hosts_done

:: Hazir bekle (max 3 dk)
echo [4/4] Uygulama hazir bekleniyor...
set /a tries=0
:waitloop
set /a tries+=1
if !tries! geq 60 goto :ready
docker inspect --format="{{.State.Health.Status}}" restaurantos-app 2>nul | findstr /i "healthy" >nul 2>&1
if not errorlevel 1 goto :ready
ping -n 4 127.0.0.1 >nul
goto :waitloop

:ready
echo.
echo.
echo ========================================
echo   RestaurantOS HAZIR
echo ========================================
echo.
echo   Domain (bu PC)  : http://restaurantos.local
echo   Direkt erisim   : http://localhost:8080
echo   Agdaki cihazlar : http://%HOST_LAN_IP%:8080
echo   Mobil           : Giris ekranindaki QR kodu tarayin
echo.
setlocal DisableDelayedExpansion
echo   Kullanici       : admin
echo   Sifre           : Resto@Admin2024!
endlocal
echo ========================================
echo.
start http://localhost:8080
