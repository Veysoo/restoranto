# RestaurantOS - Ag Kurulum Scripti
# Admin olarak calistirilir (UAC ile otomatik yukseltilir).
# Guvenlik duvari + Docker WSL2 portproxy ayarlarini yapar.

Write-Host ""
Write-Host "  RestaurantOS - Ag Kurulumu" -ForegroundColor Cyan
Write-Host ""

# ---- Guvenlik Duvari ----
Write-Host "  [1/2] Guvenlik duvari kurallari..." -NoNewline

netsh advfirewall firewall delete rule name="RestaurantOS-8080" 2>&1 | Out-Null
netsh advfirewall firewall delete rule name="RestaurantOS-HTTP"  2>&1 | Out-Null

netsh advfirewall firewall add rule `
    name="RestaurantOS-8080" dir=in action=allow `
    protocol=TCP localport=8080 profile=any 2>&1 | Out-Null

netsh advfirewall firewall add rule `
    name="RestaurantOS-HTTP" dir=in action=allow `
    protocol=TCP localport=80 profile=any 2>&1 | Out-Null

Write-Host " OK" -ForegroundColor Green

# ---- Docker WSL2 PortProxy ----
# Docker Desktop WSL2 backend portlari dis aga acmayabiliyor.
# portproxy bunu Windows kernel seviyesinde zorla cozer.
Write-Host "  [2/2] Docker portproxy (dis ag erisimi)..." -NoNewline

netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 2>&1 | Out-Null

$result = netsh interface portproxy add v4tov4 `
    listenport=8080 listenaddress=0.0.0.0 `
    connectport=8080 connectaddress=127.0.0.1 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " UYARI: $result" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Ag kurulumu tamamlandi." -ForegroundColor Green
Write-Host "  Agdaki diger cihazlar artik sunucuya baglanabilir." -ForegroundColor Green
Write-Host ""

Start-Sleep -Seconds 3
