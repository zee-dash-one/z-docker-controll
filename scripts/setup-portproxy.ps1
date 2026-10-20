# Encaminha localhost do Windows para o ssh -L do WSL, se ainda nao chegar.
# O WSL2 com encaminhamento de localhost (padrao) ja publica 127.0.0.1.
# Nesse caso o portproxy nao e criado: os dois no mesmo endereco brigam.
# Nao mexe no NetExtender. PowerShell 5. Pede admin so quando for criar o portproxy.

$ErrorActionPreference = "Stop"
$ports = 20128, 20129, 8799

function Test-LocalPort($port) {
    curl.exe -sS -o NUL -w "%{http_code}" --max-time 5 "http://127.0.0.1:$port/"
}

$missing = @()
foreach ($port in $ports) {
    $code = Test-LocalPort $port
    Write-Host "127.0.0.1:$port -> $code"
    if ($code -notmatch '^[0-9]{3}$' -or $code -eq "000") {
        $missing += $port
    }
}

if ($missing.Count -eq 0) {
    Write-Host "localhost ja alcanca o tunel. portproxy nao foi alterado."
    exit 0
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    $arg = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process -FilePath "powershell.exe" -ArgumentList $arg -Verb RunAs -Wait
    exit $LASTEXITCODE
}

$wslIp = (wsl -d Ubuntu-24.04 -- bash -lc "ip -4 -o addr show eth0" | ForEach-Object { if ($_ -match 'inet ([0-9.]+)/') { $Matches[1] } } | Select-Object -First 1)
if (-not $wslIp) {
    Write-Host "Nao achei o IP eth0 do WSL."
    exit 1
}
Write-Host "WSL eth0 = $wslIp"

foreach ($port in $missing) {
    netsh interface portproxy delete v4tov4 listenport=$port listenaddress=127.0.0.1 | Out-Null
    netsh interface portproxy add v4tov4 listenport=$port listenaddress=127.0.0.1 connectport=$port connectaddress=$wslIp
    if ($LASTEXITCODE -ne 0) {
        Write-Host "portproxy da porta $port falhou."
        exit $LASTEXITCODE
    }
}

netsh interface portproxy show all
