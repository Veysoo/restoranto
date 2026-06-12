@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Agdan Baglan

echo.
echo  =================================================
echo    RestaurantOS  -  Agda Sunucu Bul
echo    Yonetici izni GEREKMEZ
echo  =================================================
echo.

:: PS1 scriptini gecici dosyaya yaz (ayri scripts klasoru gerekmez)
:: >> ile her satiri ayri yaziyoruz (CMD grup blogu sorununu onler).
:: -like kullaniyoruz: regex yerine wildcard, hic kacis karakteri gerekmez.
set "PS1=%TEMP%\ros_find_%RANDOM%.ps1"

echo $ErrorActionPreference = 'SilentlyContinue'                                            >  "%PS1%"
echo $nics = Get-NetIPAddress -AddressFamily IPv4                                           >> "%PS1%"
echo $myIp = $nics ^|                                                                       >> "%PS1%"
echo     Where-Object { $_.PrefixOrigin -eq 'Dhcp' } ^|                                    >> "%PS1%"
echo     Where-Object { $_.IPAddress -like '192.168.*' -or $_.IPAddress -like '10.*' } ^|  >> "%PS1%"
echo     Where-Object { $_.IPAddress -notlike '192.168.56.*' } ^|                          >> "%PS1%"
echo     Where-Object { $_.IPAddress -notlike '172.25.*' } ^|                              >> "%PS1%"
echo     Select-Object -First 1 -ExpandProperty IPAddress                                   >> "%PS1%"
echo if (-not $myIp) {                                                                      >> "%PS1%"
echo     $myIp = $nics ^|                                                                   >> "%PS1%"
echo         Where-Object { $_.IPAddress -like '192.168.*' -or $_.IPAddress -like '10.*' } ^| >> "%PS1%"
echo         Where-Object { $_.IPAddress -notlike '192.168.56.*' -and $_.IPAddress -notlike '172.*' } ^| >> "%PS1%"
echo         Select-Object -First 1 -ExpandProperty IPAddress                               >> "%PS1%"
echo }                                                                                       >> "%PS1%"
echo if (-not $myIp) { Write-Host 'IP_BULUNAMADI'; exit 1 }                                >> "%PS1%"
echo $sub = $myIp -replace '\.\d+$', ''                                                     >> "%PS1%"
echo $priority = @(1,2,3,10,20,50,100,101,102,103,104,105,106,107,108,109,110)              >> "%PS1%"
echo $rest = 4..9 + 11..19 + 21..49 + 51..99 + 111..254                                    >> "%PS1%"
echo $all = ($priority + $rest) ^| Select-Object -Unique                                    >> "%PS1%"
echo $found = $null                                                                          >> "%PS1%"
echo foreach ($h in $all) {                                                                  >> "%PS1%"
echo     foreach ($port in @(8080,80)) {                                                    >> "%PS1%"
echo         try {                                                                           >> "%PS1%"
echo             $uri = "http://$sub.${h}:${port}/api/health"                              >> "%PS1%"
echo             $r = Invoke-WebRequest -Uri $uri -TimeoutSec 1 -UseBasicParsing           >> "%PS1%"
echo             if ($r.StatusCode -eq 200) { $found = "$sub.${h}:${port}"; break }        >> "%PS1%"
echo         } catch {}                                                                     >> "%PS1%"
echo     }                                                                                   >> "%PS1%"
echo     if ($found) { break }                                                               >> "%PS1%"
echo }                                                                                       >> "%PS1%"
echo if ($found) { Write-Host $found } else { exit 1 }                                     >> "%PS1%"

echo  Agdaki cihazlar taranıyor...
echo  (Bu islem 15-40 saniye surebilir, lutfen bekleyin)
echo.

set "ADDR="
for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%"`) do (
    set "ADDR=%%I"
)
del "%PS1%" >nul 2>&1

if not defined ADDR (
    echo  -----------------------------------------------
    echo  [!] Agda RestaurantOS sunucusu BULUNAMADI.
    echo.
    echo  Kontrol listesi:
    echo    1. Sunucu PC de Startup.bat acik olmali
    echo    2. Bu bilgisayar sunucu ile AYNI WiFi da olmali
    echo    3. Guvenlik duvari Startup.bat tarafindan
    echo       yapilandirilmis olmali
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
