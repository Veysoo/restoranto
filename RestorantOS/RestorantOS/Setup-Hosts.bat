@echo off
:: Bu script YONETICI olarak calistirilmalidir
:: Cift tikla ya da: sag tik -> Yonetici olarak calistir
title RestaurantOS - Ag Kurulumu

net session >nul 2>&1
if errorlevel 1 (
    echo Bu script YONETICI olarak calistirilmalidir.
    echo Sag tik - Yonetici olarak calistir
    pause
    exit /b 1
)

set SITE=restaurantos.local

:: hosts dosyasina ekle
findstr /c:"%SITE%" %SystemRoot%\System32\drivers\etc\hosts >nul 2>&1
if errorlevel 1 (
    echo 127.0.0.1 %SITE%>> %SystemRoot%\System32\drivers\etc\hosts
    echo [OK] restaurantos.local hosts dosyasina eklendi.
    echo     Bu bilgisayardan http://restaurantos.local ile erisebilirsiniz.
) else (
    echo [OK] restaurantos.local zaten kayitli.
)

:: DNS flush
ipconfig /flushdns >nul 2>&1
echo [OK] DNS onbellegi temizlendi.

echo.
echo ========================================
echo   Kurulum Tamamlandi
echo ========================================
echo.
echo   Bu PC : http://restaurantos.local
echo.
echo   Mobil icin (her telefon icin bir kez):
echo   WiFi Ayarlari -> Gelismis -> DNS
echo   Ozel DNS: 192.168.1.103
echo   Sonra telefonda yayin: restaurantos.local
echo ========================================
pause
