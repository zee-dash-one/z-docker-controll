# Limpa o Docker do WSL Ubuntu-24.04: todos os containers e volumes nomeados.
# Pede admin para o portproxy e para o arquivo hosts.
# Nao remove imagens. Nao apaga C:\GIT\ZAPFCOORP\tailscale-9router.
# Nao executa nada no z-server-01. PowerShell 5: sem &&.
# Depois desta limpeza o plano e um unico container: Tailscale, para o Windows
# usar a tailnet sem o app (NetExtender). Este script nao cria esse container.

$ErrorActionPreference = "Stop"
Write-Host "Isto apaga TODOS os containers e TODOS os volumes nomeados do Docker em Ubuntu-24.04."
Write-Host "Imagens ficam. A pasta tailscale-9router no disco fica. O servidor 192.168.0.112 nao e alterado."
$typed = Read-Host "Digite APAGAR para continuar"
if ($typed -ne "APAGAR") {
    Write-Host "Cancelado."
    exit 1
}

$inner = Join-Path $PSScriptRoot "wipe-wsl-docker-inner.sh"
if (-not (Test-Path $inner)) {
    Write-Host "Nao achei $inner"
    exit 1
}

Get-Content -Raw -Path $inner | wsl -d Ubuntu-24.04 -- bash -s
if ($LASTEXITCODE -ne 0) {
    Write-Host "A limpeza dentro do WSL falhou (codigo $LASTEXITCODE)."
    exit $LASTEXITCODE
}

$proxy = @"
netsh interface portproxy delete v4tov4 listenport=20128 listenaddress=0.0.0.0
netsh interface portproxy delete v4tov4 listenport=20129 listenaddress=0.0.0.0
"@
$proxyFile = Join-Path $env:TEMP "zlab-clear-portproxy.cmd"
Set-Content -Path $proxyFile -Value $proxy -Encoding ASCII
Start-Process -FilePath $proxyFile -Verb RunAs -Wait

$hostsPath = Join-Path $env:SystemRoot "System32\drivers\etc\hosts"
$hostsCopy = Join-Path $env:TEMP "zlab-hosts-clean.txt"
$lines = Get-Content -Path $hostsPath
$kept = foreach ($line in $lines) {
    $trim = $line.Trim()
    if ($trim -match '(^|\s)9router-local($|\s)' -or $trim -match '(^|\s)omniroute-local($|\s)') {
        continue
    }
    $line
}
Set-Content -Path $hostsCopy -Value $kept -Encoding ASCII
$copyCmd = Join-Path $env:TEMP "zlab-copy-hosts.cmd"
Set-Content -Path $copyCmd -Value "copy /Y `"$hostsCopy`" `"$hostsPath`"" -Encoding ASCII
Start-Process -FilePath $copyCmd -Verb RunAs -Wait

Write-Host "Concluido. Confira o UAC: se voce cancelou, portproxy ou hosts podem ter ficado."
