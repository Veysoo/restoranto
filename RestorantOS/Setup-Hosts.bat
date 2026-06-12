@echo off
setlocal EnableDelayedExpansion
title RestaurantOS - Hosts Kurulum

echo.
echo  ================================================
echo    RestaurantOS  -  Hosts ve Ag Kurulumu
echo    Her bilgisayarda bir kez calistirilir.
echo    Sonrasinda: http://restaurantos.local
echo  ================================================
echo.

:: ============================================
:: 1. Sunucu IP'sini bul
::    Once bu PC'de mi calisiyor kontrol et,
::    degilse agda tara.
:: ============================================
echo  [1/3] Sunucu IP'si tespit ediliyor...

set "SERVER_IP="

:: Bu PC'de mi calisiyor?
curl -s --max-time 2 http://localhost:8080/api/health 2>nul | findstr "ok" >nul 2>&1
if not errorlevel 1 (
    :: Sunucu bu PC'de - gercek LAN IP'yi al
    for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.PrefixOrigin -eq 'Dhcp' -and $_.IPAddress -notlike '169.254.*' -and $_.IPAddress -notlike '172.25.*' -and $_.IPAddress -notlike '192.168.56.*' } | Select-Object -First 1 -ExpandProperty IPAddress"`) do set SERVER_IP=%%i
    if not defined SERVER_IP set SERVER_IP=127.0.0.1
    echo         Sunucu BU bilgisayarda. IP: !SERVER_IP!
    goto :ip_found
)

:: Agda tara
echo         Sunucu bu PC'de degil, agda aranıyor...
set "PS1=%TEMP%\ros_find_%RANDOM%.ps1"

echo $ErrorActionPreference = 'SilentlyContinue'                                            >  "%PS1%"
echo $nics = Get-NetIPAddress -AddressFamily IPv4                                           >> "%PS1%"
echo $myIp = $nics ^| Where-Object { $_.PrefixOrigin -eq 'Dhcp' } ^|                      >> "%PS1%"
echo     Where-Object { $_.IPAddress -like '192.168.*' -or $_.IPAddress -like '10.*' } ^|  >> "%PS1%"
echo     Where-Object { $_.IPAddress -notlike '192.168.56.*' -and $_.IPAddress -notlike '172.25.*' } ^| >> "%PS1%"
echo     Select-Object -First 1 -ExpandProperty IPAddress                                   >> "%PS1%"
echo if (-not $myIp) { exit 1 }                                                             >> "%PS1%"
echo $sub = $myIp -replace '\.\d+$', ''                                                     >> "%PS1%"
echo $priority = @(1,2,3,10,20,50,100,101,102,103,104,105,106,107,108,109,110)              >> "%PS1%"
echo $rest = 4..9 + 11..19 + 21..49 + 51..99 + 111..254                                    >> "%PS1%"
echo $all = ($priority + $rest) ^| Select-Object -Unique                                    >> "%PS1%"
echo $found = $null                                                                          >> "%PS1%"
echo foreach ($h in $all) {                                                                  >> "%PS1%"
echo     try {                                                                               >> "%PS1%"
echo         $r = Invoke-WebRequest "http://$sub.${h}:8080/api/health" -TimeoutSec 1 -UseBasicParsing >> "%PS1%"
echo         if ($r.StatusCode -eq 200) { $found = "$sub.$h"; break }                      >> "%PS1%"
echo     } catch {}                                                                          >> "%PS1%"
echo }                                                                                       >> "%PS1%"
echo if ($found) { Write-Host $found } else { exit 1 }                                     >> "%PS1%"

for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%PS1%"`) do set SERVER_IP=%%I
del "%PS1%" >nul 2>&1

if not defined SERVER_IP (
    echo.
    echo  [!] Agda sunucu bulunamadi.
    echo      Startup.bat calistirildiginden emin olun.
    echo.
    set /p SERVER_IP="Sunucu IP girin (orn: 192.168.1.103): "
    if "!SERVER_IP!"=="" ( pause & exit /b 1 )
)

:ip_found
echo  [1/3] Sunucu IP: %SERVER_IP%  OK

:: ============================================
:: 2. Hosts + DNS flush (UAC ile)
:: ============================================
echo  [2/3] hosts dosyasi guncelleniyor...
echo         (Izin penceresi acilacak - Evet deyin)

:: PS1 TEMP'e yaz
set "PS_H=%TEMP%\ros_sethosts_%RANDOM%.ps1"
echo $f = "$env:SystemRoot\System32\drivers\etc\hosts"                                     >  "%PS_H%"
echo $ip = '%SERVER_IP%'                                                                    >> "%PS_H%"
echo $entry = "$ip`t`trestaurantos.local"                                                   >> "%PS_H%"
echo $lines = Get-Content $f                                                                >> "%PS_H%"
echo if ($lines ^| Where-Object { $_ -match 'restaurantos\.local' }) {                     >> "%PS_H%"
echo     $lines = $lines ^| ForEach-Object {                                                >> "%PS_H%"
echo         if ($_ -match 'restaurantos\.local') { $entry } else { $_ }                   >> "%PS_H%"
echo     }                                                                                   >> "%PS_H%"
echo     Set-Content $f $lines -Encoding UTF8                                               >> "%PS_H%"
echo     Write-Host '  Guncellendi.' -ForegroundColor Green                                 >> "%PS_H%"
echo } else {                                                                                >> "%PS_H%"
echo     Add-Content $f "`n$entry" -Encoding UTF8                                           >> "%PS_H%"
echo     Write-Host '  Eklendi.' -ForegroundColor Green                                     >> "%PS_H%"
echo }                                                                                       >> "%PS_H%"
echo ipconfig /flushdns ^| Out-Null                                                         >> "%PS_H%"
echo Write-Host '  DNS cache temizlendi.' -ForegroundColor Green                            >> "%PS_H%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%PS_H%""' -Verb RunAs -Wait"
del "%PS_H%" >nul 2>&1
echo  [2/3] Hosts guncellendi.  OK

:: ============================================
:: 3. Firewall (sadece sunucu bu PC'yse)
:: ============================================
echo  [3/3] Guvenlik duvari kontrol ediliyor...

curl -s --max-time 1 http://localhost:8080/api/health 2>nul | findstr "ok" >nul 2>&1
if not errorlevel 1 (
    netsh advfirewall firewall show rule name="RestaurantOS-8080" >nul 2>&1
    if errorlevel 1 (
        echo         Guvenlik duvari kurali eksik, ekleniyor...
        powershell -NoProfile -ExecutionPolicy Bypass -Command ^
            "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%~dp0scripts\setup-network.ps1""' -Verb RunAs -Wait" >nul 2>&1
    ) else (
        echo         Guvenlik duvari kurali zaten aktif.
    )
) else (
    echo         Bu PC sunucu degil, guvenlik duvari atlanıyor.
)
echo  [3/3] OK

echo.
echo  ================================================
echo    KURULUM TAMAMLANDI
echo  ================================================
echo.
echo    Bu bilgisayardan erisim:
echo    http://restaurantos.local
echo    http://%SERVER_IP%:8080
echo.
pause
