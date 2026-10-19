# Changelog

## 2026-10-19

### todo 07: Tailscale WSL → SOCKS5 em 127.0.0.1:1055

- `wsl/tailscale-windows/`: userspace, `tag:winwsl`, MagicDNS (`TS_ACCEPT_DNS` + `dns: 100.100.100.100`), publish só `127.0.0.1:1055`.
- Wipe WSL: correção CRLF em `wipe-wsl-docker-inner.sh` / normalização no `.ps1`.
- Prova: `curl.exe --socks5-hostname 127.0.0.1:1055` → zlab/9router-serve/mausbot-serve.

### todo 06: Saída recolhível e polimento

- `OutputPanel` + `OutputBuffer` (máx. 1000 linhas, badge de não lidas, Copiar/Limpar, Ctrl+L).
- Tema persistido em `%AppData%/ZLab/settings.json` (`Theme`); System segue o Windows.
- Bandeja: Abrir / Entrar / Encerrar sessão / Fechar Z-Lab; tooltip de sessão; X só esconde.
- DPI PerMonitorV2, `CompositedPanel`, F5, capturas em `docs/screenshots/`.

### todo 05: código de acesso MausBot

- Botão "Código de acesso" só no card do MausBot; `AccessCodePopover` com Copiar (código só em memória).
- `MausBotAccessCode`: lê `pairing code:` nos logs (`docker logs --timestamps --tail 2000`); se vazio, mint via `docker exec … pair` (mesmo formato). Nunca grava o valor.
- Formato: `pairing code:\s*(\S+)` (ex. `XXXX-XXXX-XXXX`). Saída: só `Código de acesso do MausBot exibido.`

### todo 04: cards de serviço

- `ServiceCard` em grade responsiva (3/2/1 colunas), pílula, endereço clicável, mini monitor CPU/memória.
- `zlab-stats.sh` + parser com `section=containers`; StatusWatcher alimentado pelo StatsPoller (uma SSH/5 s).

### rede: IP novo do z-server-01

- Alvo SSH/LAN: `192.168.2.37` (antes `192.168.0.112`). Broadcast WOL: `192.168.2.255`.
- Fonte de verdade: `C:\GIT\ZAPFCOORP\z-server-01\README.md`. Regra Cursor: `.cursor/rules/z-server-01-source.mdc`.

### todo 03: painel do servidor

- Script `scripts/zlab-stats.sh` (recurso embutido) + `StatsPoller` a cada 5 s com sessão.
- Seis `MetricTile`: CPU (sparkline), Memória, discos, Dados ausente, Ligado há.
- `DisplayFormat`, `StatsParser`, `CpuUsageCalculator`, `ServerTilesPresenter`; removido `HostReadings` da UI.

### todo 02: janela e sessão

- `HeaderBar` (marca, chip de sessão, Entrar / Encerrar sessão, tema, Fechar Z-Lab).
- `EmptyStatePanel` sem sessão; `DashboardPanel` com seções Servidor (energia) e Serviços (placeholder).
- `SessionUiRules` / `SessionState` no Core; Saída compacta (40 px).
- Removidos os botões soltos do topo e os GroupBoxes antigos de serviço.

### todo 01: tema e componentes base

- Namespace `ZLab.Ui`: tokens claro/escuro, ThemeManager, RoundedPanel, ThinProgressBar, Sparkline, MetricTile, StatusPill, ZButton, ResponsiveGrid.
- Galeria: `dotnet run --project src/ZLab -- --galeria`.
- `UiMath` no Core para severidade e colunas da grade (testes no WSL).
