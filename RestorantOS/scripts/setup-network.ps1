# RestaurantOS - Ag Kurulum Scripti
# Admin olarak calistirilir (UAC ile otomatik yukseltilir).
# Guvenlik duvari kurallarini yapar ve eski portproxy kalintisi varsa temizler.

Write-Host ""
Write-Host "  RestaurantOS - Ag Kurulumu" -ForegroundColor Cyan
Write-Host ""

# ---- 1. Eski portproxy kalintisini temizle ----
Write-Host "  [1/2] Eski portproxy kurallari temizleniyor..." -NoNewline

netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 2>&1 | Out-Null
netsh interface portproxy delete v4tov4 listenport=80   listenaddress=0.0.0.0 2>&1 | Out-Null
netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=127.0.0.1 2>&1 | Out-Null

Write-Host " OK" -ForegroundColor Green

# ---- 2. Guvenlik Duvari ----
Write-Host "  [2/2] Guvenlik duvari kurallari..." -NoNewline

netsh advfirewall firewall delete rule name="RestaurantOS-8080" 2>&1 | Out-Null
netsh advfirewall firewall delete rule name="RestaurantOS-HTTP"  2>&1 | Out-Null

netsh advfirewall firewall add rule `
    name="RestaurantOS-8080" dir=in action=allow `
    protocol=TCP localport=8080 profile=any 2>&1 | Out-Null

netsh advfirewall firewall add rule `
    name="RestaurantOS-HTTP" dir=in action=allow `
    protocol=TCP localport=80 profile=any 2>&1 | Out-Null

Write-Host " OK" -ForegroundColor Green

Write-Host ""
Write-Host "  Ag kurulumu tamamlandi." -ForegroundColor Green
Write-Host "  Agdaki diger cihazlar artik sunucuya baglanabilir." -ForegroundColor Green
Write-Host ""

Start-Sleep -Seconds 2
