# Technologies: Z-Lab 2.0

**Authority:** [`docs/SPEC-ZLAB.md`](../docs/SPEC-ZLAB.md)  
**Product:** `ZLab.exe` — WinForms tray that monitors z-server-01 over SSH.

## 1. Stack

| Decision | Choice |
|----------|--------|
| Runtime | .NET 8, `net8.0-windows` for the exe, `net8.0` for `ZLab.Core` |
| UI | WinForms (`MainForm`, `LoginForm`, `NotifyIcon`, `MessageBox`) |
| Language | C#, nullable enabled |
| SSH | SSH.NET 2026.0.0 inside the process. One `SshClient` per session. Password auth only |
| Wake | `UdpClient` broadcast, same 102-byte packet as the server script |
| Config | `%AppData%\ZLab\settings.json` for poll interval and log tail. Services are code (`RemoteCommands`) |
| Tests | xUnit on command strings, magic packet, `known_hosts`, Compose `ps` JSON |
| Publish | `dotnet publish src/ZLab/ZLab.csproj -c Release -r win-x64` from the WSL SDK when the Windows SDK is absent |

Not in the process: PowerShell 5 runspace (it does not load in .NET 8), `ssh.exe`, Docker CLI on WSL, portproxy, a Compose library, a DI framework.

IPv4: the session dials `192.168.2.37` and Wake uses `192.168.2.255`. Image pull runs on the server. The script `scripts/fix-wsl-docker-ipv4.sh` is not the Atualizar failure path anymore.

## 2. Layout

```text
src/ZLab.Core/Services/RemoteCommands.cs    closed argv
src/ZLab.Core/Services/SshSession.cs        password session, known_hosts
src/ZLab.Core/Services/ServerMonitor.cs     poll + verbs
src/ZLab.Core/Services/WakeOnLan.cs
src/ZLab/Program.cs                         composition
src/ZLab/TrayApp.cs
src/ZLab/MainForm.cs
src/ZLab/LoginForm.cs
config/monitor.settings.json                seed shape for poll/log
scripts/wipe-wsl-docker.ps1                 operator-run, not the exe
```

The sibling repo `z-server-01` holds `zlab/sudoers-zlab-monitor` and `zlab/aplicar-sudoers.sh`. The exe does not install that file.

## 3. Security

- Password buffer cleared on session end. SSH.NET keeps its own copy until the client is disposed.
- Commands are built only from the six ids. `sudo -n` fails closed when the server allowlist does not match.
- `known_hosts` is read from `%USERPROFILE%\.ssh\known_hosts`. No trust-on-first-use.
- `TS_AUTHKEY` is not read or written.
- Until the operator removes `/etc/sudoers.d/melchior-nopasswd` and the `docker` group, the server still allows more than this list. The exe still sends only the list.

## 4. V1 code still compiled

`DockerCli`, `ContainerService`, backup, `LanExposeHelper`, `NineRouterTunnelHelper`, and `TailscaleAuthKeyCheck` remain in `ZLab.Core` and are covered by older tests. `Program` does not construct them. Removing them is recorded in [tech-debt.md](tech-debt.md).
