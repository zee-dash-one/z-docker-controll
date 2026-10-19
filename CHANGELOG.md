# Changelog

## 2026-10-19

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
