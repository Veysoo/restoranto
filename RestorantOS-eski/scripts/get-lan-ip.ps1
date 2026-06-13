$ErrorActionPreference = 'SilentlyContinue'

$ip = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.IPAddress -ne '127.0.0.1' -and
        $_.IPAddress -notlike '169.254.*' -and
        $_.IPAddress -notlike '172.17.*' -and
        $_.IPAddress -notlike '172.18.*' -and
        $_.IPAddress -notlike '172.19.*' -and
        $_.IPAddress -notlike '172.2?.*' -and
        $_.IPAddress -notlike '192.168.56.*' -and
        ($_.PrefixOrigin -eq 'Dhcp' -or $_.PrefixOrigin -eq 'Manual') -and
        $_.InterfaceAlias -notmatch 'vEthernet|Loopback|VMware|VirtualBox|WSL'
    } |
    Sort-Object -Property { if($_.PrefixOrigin -eq 'Dhcp'){0}else{1} } |
    Select-Object -First 1 -ExpandProperty IPAddress

if ($ip) {
    Write-Output $ip
} else {
    Write-Output 'localhost'
}
