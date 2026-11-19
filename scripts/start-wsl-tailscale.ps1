# Sobe o Docker no Ubuntu-24.04 e o container nbpitang-wsl-tailscale.
# Nao mexe em proxy do Windows, PAC, NetExtender nem no app Tailscale.
# A tarefa ZLab-WslTailscale chama este script no logon.

$ErrorActionPreference = "Stop"
$Distro = "Ubuntu-24.04"
$Compose = "/mnt/c/GIT/ZAPFCOORP/router-ctrl/wsl/tailscale-windows/docker-compose.yml"

$dockerUp = $false
for ($i = 0; $i -lt 5; $i++) {
    wsl -d $Distro -u root -- systemctl start docker
    if ($LASTEXITCODE -eq 0) {
        $dockerUp = $true
        break
    }
    Start-Sleep -Seconds 3
}

if (-not $dockerUp) {
    Write-Host "Docker nao subiu no $Distro."
    exit 1
}

wsl -d $Distro -u root -- docker compose -f $Compose up -d
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Tailscale WSL no ar. SOCKS 127.0.0.1:1055, HTTP 127.0.0.1:1056."
