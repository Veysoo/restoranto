@echo off
:: Ilk kurulum - YONETICI olarak calistirin (sag tik -> Yonetici)
title RestaurantOS - Ag Kurulumu (bir kez)

net session >nul 2>&1
if errorlevel 1 (
    echo Bu script YONETICI olarak calistirilmalidir.
    pause
    exit /b 1
)

set SITE=restaurantos.local

for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do (
    set IP=%%a
    goto gotip
)
:gotip
set IP=%IP:~1%

echo SITE_HOSTNAME=%SITE%> "%~dp0.env"
echo SITE_HOST=%SITE%>> "%~dp0.env"
echo SITE_URL=http://%SITE%>> "%~dp0.env"
echo HOST_LAN_IP=%IP%>> "%~dp0.env"

powershell -Command "(Get-Content '%~dp0docker\dnsmasq.conf.template') -replace '@HOST_IP@','%IP%' | Set-Content '%~dp0docker\dnsmasq.conf'"

findstr /c:"%SITE%" %SystemRoot%\System32\drivers\etc\hosts >nul 2>&1
if errorlevel 1 (
    echo %IP% %SITE% restaurantos>> %SystemRoot%\System32\drivers\etc\hosts
    echo [OK] hosts dosyasi guncellendi.
)

netsh advfirewall firewall add rule name="RestaurantOS HTTP" dir=in action=allow protocol=TCP localport=80 >nul 2>&1
netsh advfirewall firewall add rule name="RestaurantOS DNS" dir=in action=allow protocol=UDP localport=53 >nul 2>&1
netsh advfirewall firewall add rule name="RestaurantOS DNS TCP" dir=in action=allow protocol=TCP localport=53 >nul 2>&1

echo.
echo ========================================
echo   Ag kurulumu tamamlandi
echo ========================================
echo.
echo   Site adresi: http://%SITE%
echo.
echo   TELEFONLAR icin (bir kez, WiFi ayarlarindan):
echo   Ozel DNS sunucusu: %IP%
echo   (Bu adim sadece yoneticinin bilmesi icin - personele soylemeyin)
echo.
echo   Personel tarayiciya yazar: restaurantos.local
echo   veya giris ekranindaki QR kodu tarar.
echo ========================================
pause
