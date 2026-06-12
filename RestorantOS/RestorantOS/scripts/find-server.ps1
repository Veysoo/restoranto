# RestaurantOS - Agda sunucu bul (yonetici gerektirmez)
$ErrorActionPreference = 'SilentlyContinue'

$myIp = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.PrefixOrigin -eq 'Dhcp' -and
        $_.IPAddress -match '^(192\.168\.|10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)' -and
        $_.IPAddress -notmatch '^(169\.254\.|172\.25\.)'
    } |
    Select-Object -First 1 -ExpandProperty IPAddress

if (-not $myIp) { exit 1 }

$subnet = $myIp -replace '\.\d+$', ''
$hosts = (1..15) + (100..115) + @(50, 2, 3, 20, 30, 40) + (16..99) + (116..254) | Select-Object -Unique

foreach ($h in $hosts) {
    $ip = "$subnet.$h"
    foreach ($port in @(8080, 80)) {
        try {
            $r = Invoke-WebRequest -Uri "http://${ip}:${port}/api/health" -TimeoutSec 1 -UseBasicParsing
            if ($r.Content -match '"status"\s*:\s*"ok"') {
                Write-Output $ip
                exit 0
            }
        } catch {}
    }
}
exit 1
