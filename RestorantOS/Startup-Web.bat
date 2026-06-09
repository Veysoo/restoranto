@echo off
setlocal EnableDelayedExpansion
title RestaurantOS Web Startup

echo ========================================
echo   RestaurantOS Web - Docker Baslatma
echo ========================================
echo.

docker info >nul 2>&1
if errorlevel 1 (
    echo [HATA] Docker calismiyor. Docker Desktop'i baslatin.
    pause
    exit /b 1
)

echo [1/2] Container'lar baslatiliyor (SQL + Web/API)...
docker-compose up -d --build
if errorlevel 1 (
    echo [HATA] docker-compose basarisiz.
    pause
    exit /b 1
)

echo.
echo [2/2] Saglik kontrolu bekleniyor...
set /a retries=0
:waitloop
docker inspect --format="{{.State.Health.Status}}" restaurantos-sqlserver 2>nul | findstr /i "healthy" >nul
if not errorlevel 1 goto ready
set /a retries+=1
if !retries! geq 60 goto ready
timeout /t 2 /nobreak >nul
goto waitloop

:ready
echo.
echo ========================================
echo   RestaurantOS Web HAZIR
echo ========================================
echo.
echo   Bu bilgisayar:  http://localhost:8080
echo   Agdaki diger PC: http://SUNUCU-IP:8080
echo.
echo   Giris: admin / admin123
echo.
echo   WPF masaustu uygulamasi ayri calisir (Startup.bat)
echo ========================================
echo.
start http://localhost:8080
timeout /t 5 >nul
