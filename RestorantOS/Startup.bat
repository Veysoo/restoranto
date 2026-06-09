@echo off
setlocal EnableDelayedExpansion
title RestaurantOS Startup

echo ========================================
echo   RestaurantOS v1.0 - Baslatiliyor...
echo ========================================
echo.

docker info >nul 2>&1
if errorlevel 1 (
    echo [HATA] Docker calismiyor. Lutfen Docker Desktop'i baslatin.
    pause
    exit /b 1
)

echo [1/3] SQL Server container kontrol ediliyor...
docker inspect restaurantos-sqlserver >nul 2>&1
if not errorlevel 1 (
    docker inspect --format="{{.State.Running}}" restaurantos-sqlserver 2>nul | findstr /i "true" >nul
    if not errorlevel 1 (
        echo [OK] Container zaten calisiyor.
        goto wait_health
    )
    echo Mevcut container baslatiliyor...
    docker start restaurantos-sqlserver >nul 2>&1
    if not errorlevel 1 goto wait_health
)

echo Container olusturuluyor (port 14330)...
docker-compose up -d
if errorlevel 1 (
    echo.
    echo [HATA] docker-compose basarisiz.
    echo.
    echo Olası nedenler:
    echo   - Port 14330 baska bir uygulama tarafindan kullaniliyor
    echo   - Eski/bozuk container kaldi
    echo.
    echo Cozum deneyin:
    echo   docker rm -f restaurantos-sqlserver
    echo   docker-compose up -d
    echo.
    pause
    exit /b 1
)

:wait_health
echo [2/3] SQL Server saglik kontrolu bekleniyor...
set /a retries=0
:waitloop
docker inspect --format="{{.State.Health.Status}}" restaurantos-sqlserver 2>nul | findstr /i "healthy" >nul
if not errorlevel 1 goto healthy
docker inspect --format="{{.State.Running}}" restaurantos-sqlserver 2>nul | findstr /i "true" >nul
if errorlevel 1 (
    echo [HATA] Container calismiyor.
    pause
    exit /b 1
)
set /a retries+=1
if !retries! geq 90 (
    echo [UYARI] Saglik kontrolu zaman asimi. SQL Server hala basliyor olabilir.
    echo Uygulama baglanti icin 60 saniye daha bekleyecek...
    goto launch
)
timeout /t 2 /nobreak >nul
goto waitloop

:healthy
echo [OK] SQL Server hazir (localhost:14330).

:launch
echo [3/3] RestaurantOS baslatiliyor...
set "APP_EXE=%~dp0RestaurantOS.WPF\bin\Release\net8.0-windows\RestaurantOS.exe"
if not exist "%APP_EXE%" (
    set "APP_EXE=%~dp0RestaurantOS.WPF\bin\Debug\net8.0-windows\RestaurantOS.exe"
)
if not exist "%APP_EXE%" (
    echo [HATA] Uygulama bulunamadi. Once derleyin:
    echo   dotnet build RestaurantOS.WPF -c Release
    pause
    exit /b 1
)
start "" "%APP_EXE%"
echo.
echo RestaurantOS baslatildi.
echo Veritabani: localhost,14330
echo Web surumu icin: Startup-Web.bat  ^(http://localhost:8080^)
timeout /t 3 >nul
