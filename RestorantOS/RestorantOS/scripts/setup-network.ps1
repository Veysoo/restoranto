# RestaurantOS - Ag Kurulum Scripti
# Bu script yonetici (admin) olarak calistirilir.
# Guvenlik duvari + portproxy ayarlarini yapar.

param([string]$LanIp = "0.0.0.0")

Write-Host "  Guvenlik duvari kurallari ayarlaniyor..."

# Eski kurallari temizle
netsh advfirewall firewall delete rule name="RestaurantOS-8080" | Out-Null
netsh advfirewall firewall delete rule name="RestaurantOS-HTTP"  | Out-Null

# Yeni kurallar - tum profiller (Domain, Private, Public)
netsh advfirewall firewall add rule name="RestaurantOS-8080" dir=in action=allow protocol=TCP localport=8080 profile=any | Out-Null
netsh advfirewall firewall add rule name="RestaurantOS-HTTP"  dir=in action=allow protocol=TCP localport=80  profile=any | Out-Null

Write-Host "  Guvenlik duvari: OK"

# Docker Desktop WSL2 port forwarding duzeltmesi
# WSL2 backend bazen dis ag erisimini engelliyor, portproxy cozuyor.
Write-Host "  Docker portproxy ayarlaniyor..."

netsh interface portproxy delete v4tov4 listenport=8080 listenaddress=0.0.0.0 2>&1 | Out-Null
netsh interface portproxy delete v4tov4 listenport=80   listenaddress=0.0.0.0 2>&1 | Out-Null

netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=8080 connectaddress=127.0.0.1
netsh interface portproxy add v4tov4 listenport=80   listenaddress=0.0.0.0 connectport=80   connectaddress=127.0.0.1

Write-Host "  Portproxy: OK"
Write-Host "  Ag kurulumu tamamlandi."
