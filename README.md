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

## Rede: IPv4 only (obrigatório)

Neste lab **não usar IPv6** no WSL/Docker. Pulls do Docker Hub falham quando o daemon resolve CloudFront em AAAA (`cannot assign requested address` / `network is unreachable`).

Após setup ou se o **Atualizar** falhar por IPv6:

```bash
wsl -d Ubuntu-24.04 -u root -- bash /mnt/c/GIT/ZAPFCOORP/router-ctrl/scripts/fix-wsl-docker-ipv4.sh
```

Regra do projeto: [`.cursor/rules/no-ipv6.mdc`](.cursor/rules/no-ipv6.mdc).