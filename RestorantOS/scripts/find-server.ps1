# RestaurantOS - Agda sunucu bul (yonetici gerektirmez)
# Paralel tarama ile hizli sonuc verir.
$ErrorActionPreference = 'SilentlyContinue'

$myIp = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.PrefixOrigin -eq 'Dhcp' -and
        $_.IPAddress -notmatch '^(169\.254\.|172\.25\.|192\.168\.56\.)' -and
        ($_.IPAddress -match '^(192\.168\.|10\.)' -or $_.IPAddress -match '^172\.(1[6-9]|2[0-9]|3[0-1])\.')
    } |
    Select-Object -First 1 -ExpandProperty IPAddress

if (-not $myIp) {
    $myIp = Get-NetIPAddress -AddressFamily IPv4 |
        Where-Object {
            $_.IPAddress -ne '127.0.0.1' -and
            $_.IPAddress -notmatch '^169\.254\.' -and
            ($_.IPAddress -match '^(192\.168\.|10\.)' -or $_.IPAddress -match '^172\.')
        } |
        Select-Object -First 1 -ExpandProperty IPAddress
}

if (-not $myIp) { exit 1 }

$subnet = $myIp -replace '\.\d+$', ''
$port   = 8080

$priority = @(1,2,3,10,20,50,100,101,102,103,104,105,106,107,108,109,110)
$rest     = 4..9 + 11..19 + 21..49 + 51..99 + 111..254
$allHosts = ($priority + $rest) | Select-Object -Unique

$runspacePool = [RunspaceFactory]::CreateRunspacePool(1, 40)
$runspacePool.Open()

$scriptBlock = {
    param($ip, $port)
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $ar  = $tcp.BeginConnect($ip, $port, $null, $null)
        $ok  = $ar.AsyncWaitHandle.WaitOne(800, $false)
        if ($ok -and $tcp.Connected) {
            $tcp.Close()
            try {
                $wc = New-Object System.Net.WebClient
                $r  = $wc.DownloadString("http://${ip}:${port}/api/health")
                if ($r -match 'ok') { return "${ip}:${port}" }
            } catch {}
        } else {
            $tcp.Close()
        }
    } catch {}
    return $null
}

$jobs = @()
foreach ($h in $allHosts) {
    $ip = "$subnet.$h"
    $ps = [PowerShell]::Create().AddScript($scriptBlock).AddArgument($ip).AddArgument($port)
    $ps.RunspacePool = $runspacePool
    $jobs += @{ Pipe = $ps; Handle = $ps.BeginInvoke() }
}

$found = $null
$deadline = (Get-Date).AddSeconds(30)

while ($jobs.Count -gt 0 -and (Get-Date) -lt $deadline) {
    for ($i = $jobs.Count - 1; $i -ge 0; $i--) {
        if ($jobs[$i].Handle.IsCompleted) {
            $result = $jobs[$i].Pipe.EndInvoke($jobs[$i].Handle)
            $jobs[$i].Pipe.Dispose()
            if ($result -and $result[0]) {
                $found = $result[0]
                break
            }
            $jobs.RemoveAt($i)
        }
    }
    if ($found) { break }
    Start-Sleep -Milliseconds 100
}

foreach ($j in $jobs) { $j.Pipe.Dispose() }
$runspacePool.Close()
$runspacePool.Dispose()

if ($found) {
    Write-Output $found
    exit 0
}
exit 1
