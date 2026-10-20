# PAC local: so *.mandrill-court.ts.net vai ao proxy HTTP 127.0.0.1:1056.
# O resto fica DIRECT. Nao liga ProxyEnable (NetExtender / tunel dividido).
# O Opera GX le o proxy do Windows. O proxy HTTP resolve o nome dentro do WSL.
# PowerShell 5. Uso: powershell -File scripts\tailnet-pac.ps1
# -Configure so grava o Windows. -Listen so publica o script.
# Na entrada do usuario a tarefa ZLab-TailnetPac sobe de novo com -Listen.

param(
    [switch]$Listen,
    [switch]$Configure
)

$ErrorActionPreference = "Stop"
$Port = 18080
$PacUrl = "http://127.0.0.1:$Port/v2.pac"
$TaskName = "ZLab-TailnetPac"
$PacFile = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\wsl\tailscale-windows\tailnet.pac"))

if (-not (Test-Path $PacFile)) {
    Write-Host "Nao achei $PacFile"
    exit 1
}

function Update-WinInet {
    if (-not ("ZLab.WinInet" -as [type])) {
        Add-Type -Namespace ZLab -Name WinInet -MemberDefinition @"
[System.Runtime.InteropServices.DllImport("wininet.dll", SetLastError=true)]
public static extern bool InternetSetOption(System.IntPtr hInternet, int dwOption, System.IntPtr lpBuffer, int dwBufferLength);
"@
    }
    [void][ZLab.WinInet]::InternetSetOption([IntPtr]::Zero, 39, [IntPtr]::Zero, 0)
    [void][ZLab.WinInet]::InternetSetOption([IntPtr]::Zero, 37, [IntPtr]::Zero, 0)
}

function Set-ConnectionBlob {
    $urlBytes = [System.Text.Encoding]::ASCII.GetBytes($PacUrl)
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([int32]0x46)
    $bw.Write([int32]5)
    # 0x05 = direto + script. Sem 0x08, a deteccao automatica nao fura o script.
    $bw.Write([int32]0x05)
    $bw.Write([int32]0)
    $bw.Write([int32]0)
    $bw.Write([int32]$urlBytes.Length)
    $bw.Write($urlBytes)
    $bw.Write([byte]0)
    $bw.Flush()
    $blob = $ms.ToArray()
    $conn = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections"
    Set-ItemProperty -Path $conn -Name DefaultConnectionSettings -Value $blob -Type Binary
    Set-ItemProperty -Path $conn -Name SavedLegacySettings -Value $blob -Type Binary
}

function Set-PacSetting {
    $key = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings"
    Set-ItemProperty -Path $key -Name AutoConfigURL -Value $PacUrl
    Set-ItemProperty -Path $key -Name ProxyEnable -Value 0 -Type DWord
    Set-ConnectionBlob
    Update-WinInet
}

function Register-PacTask {
    $arg = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$PSCommandPath`" -Listen"
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arg
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Force | Out-Null
}

function Start-PacListener {
    $pac = [System.IO.File]::ReadAllText($PacFile)
    $listener = New-Object System.Net.HttpListener
    $listener.Prefixes.Add("http://127.0.0.1:$Port/")
    $listener.Start()
    Write-Host "PAC em $PacUrl"
    try {
        while ($listener.IsListening) {
            $ctx = $listener.GetContext()
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($pac)
            $ctx.Response.StatusCode = 200
            $ctx.Response.ContentType = "application/x-ns-proxy-autoconfig"
            $ctx.Response.ContentLength64 = $bytes.Length
            $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
            $ctx.Response.OutputStream.Close()
        }
    } finally {
        $listener.Stop()
    }
}

if ($Configure) {
    Set-PacSetting
    Register-PacTask
    Write-Host "AutoConfigURL = $PacUrl"
    Write-Host "ProxyEnable = 0"
    exit 0
}

if ($Listen) {
    Start-PacListener
    exit 0
}

Set-PacSetting
Register-PacTask
Write-Host "AutoConfigURL = $PacUrl"
Write-Host "ProxyEnable = 0"
Write-Host "Tarefa $TaskName na entrada de $env:USERNAME"
Start-PacListener
