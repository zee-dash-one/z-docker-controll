# Z-Lab

App Windows (WinForms .NET 8) que monitora o z-server-01: seis serviços do Compose em `/opt/zlab`, sessão SSH com usuário e senha, Wake-on-LAN.

## Servidor

Este repositório e o [z-server-01](https://github.com/zee-dash-one/z-server-01) (`C:\GIT\ZAPFCOORP\z-server-01`) são um ecossistema. A máquina e o Compose ficam naquele repositório. O sudoers estreito está em `z-server-01/zlab/sudoers-zlab-monitor` e ainda precisa ser aplicado à mão (`zlab/aplicar-sudoers.sh`).

## Docs

- Produto: [docs/SPEC-ZLAB.md](docs/SPEC-ZLAB.md) (2.0)
- Desenvolvimento: [.docs/](.docs/) (`requirements`, `technologies`, `ui-ux`, `todo`, `tech-debt`, `specs/`)
- Plano: [.docs/plans/zlab-no-z-server-01.md](.docs/plans/zlab-no-z-server-01.md)
- Dúvidas e restos: [.docs/tech-debt.md](.docs/tech-debt.md)

## Build (WSL)

```bash
export PATH="/home/hill/.dotnet:$PATH"
export DOTNET_ROOT="/home/hill/.dotnet"
cd /mnt/c/GIT/ZAPFCOORP/router-ctrl
dotnet test ZLab.sln -c Release
dotnet publish src/ZLab/ZLab.csproj -c Release -r win-x64 --self-contained false -o publish/ZLab
```

## Interface

Redesign em andamento (`.todo/`). Tokens e controles em `src/ZLab/Ui/`. Mockups: `docs/mockups/`.

Já feito: tema/componentes (01) e casca da janela com sessão, estado vazio e energia no painel Servidor (02). Painel de métricas e cards novos vêm nas tarefas 03–04.

Galeria dos componentes (tema claro/escuro):

```powershell
dotnet run --project src/ZLab -- --galeria
```

## Run (Windows)

```powershell
.\publish\ZLab\ZLab.exe
```

Requer .NET 8 Desktop Runtime. O exe fala com `192.168.0.112:22`. Não usa `wsl docker`.

A limpeza do Docker no WSL (containers e volumes nomeados, portproxy, hosts) é `scripts/wipe-wsl-docker.ps1`. Ela pede a palavra `APAGAR` e não faz parte da subida do exe.

## Rede

A sessão e o Liga usam IPv4 (`192.168.0.112`, broadcast `192.168.0.255`). O pull de imagem acontece no servidor. O script `scripts/fix-wsl-docker-ipv4.sh` continua no repositório para o Docker do WSL, e não é o caminho do botão Atualizar.