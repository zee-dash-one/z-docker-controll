# Z-Lab

App Windows (WinForms .NET 8) para operar o lab Docker no WSL2: itens configuráveis por JSON, bandeja + painel.

## Docs

- Produto: [docs/SPEC-ZLAB.md](docs/SPEC-ZLAB.md)
- Desenvolvimento: [.docs/](.docs/) (`requirements`, `technologies`, `ui-ux`, `todo`, `specs/`)
- Catálogo default: [config/services.default.json](config/services.default.json)

## Build (WSL)

```bash
export PATH="$HOME/.dotnet:$PATH"
cd /mnt/c/GIT/ZAPFCOORP/router-ctrl
dotnet test ZLab.sln -c Release
dotnet publish src/ZLab/ZLab.csproj -c Release -r win-x64 --self-contained false -o publish/ZLab
```

## Run (Windows)

```powershell
.\publish\ZLab\ZLab.exe
```

Requer .NET 8 Desktop Runtime. Docker via `docker.exe` ou `wsl docker`.
