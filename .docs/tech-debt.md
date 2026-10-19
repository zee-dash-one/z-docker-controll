# Z-Lab — Débito e dúvidas

A suíte `dotnet test ZLab.sln -c Release` no SDK do WSL passou (109 testes), inclusive a igualdade entre `FormatSudoersDocument` e `z-server-01/zlab/sudoers-zlab-monitor`. O projeto WinForms compilou. O painel não foi clicado contra o servidor.

## Inconsistente com o contrato, de propósito ou por resto de código

1. **Classes V1 ainda entram no `ZLab.Core`.** `DockerCli`, `ContainerService`, backup, `LanExposeHelper`, `NineRouterTunnelHelper`, `TailscaleAuthKeyCheck`, `LocalHostsHelper` e o catálogo JSON não são compostos em `Program`. Os testes antigos ainda passam contra `tests/ZLab.Tests/Fixtures/services.v1.json`. O seed [`config/services.default.json`](../config/services.default.json) agora é só um aviso. Apagar essas classes fica para depois da limpeza do WSL.
2. **`logTailLines` não muda o comando remoto.** O argv de log está fixo em `--tail 80` para o sudoers casar. O campo em `settings.json` é gravado e ignorado na linha de comando.
3. **O painel mostra o texto cru** de `uptime`, `free -h` e `df -h`, não colunas separadas (total / usado / cache).
4. **Uso** usa `--format {{.CPUPerc}}/{{.MemUsage}}/{{.PIDs}}` em vez de `{{json .}}`, porque o espaço quebraria o sudoers e o shell.
5. **Estado** usa `ps -a --format json`. O contrato original dizia só `ps`.
6. **Backup** (clique e diário) saiu do exe e não tem substituto no servidor.
7. **Queda de SSH no meio do poll** marca erro ou “sem sessão”. Não dispara balão “caiu” quando o status vai para `Unknown`.

## Dúvidas ainda abertas

1. **`sshd` aceita senha?** O README do z-server-01 documenta o login humano pela chave. Se `PasswordAuthentication` estiver desligado, o diálogo falha. Não conferi o `sshd -T` na máquina.
2. **`known_hosts` ao vivo.** O teste cobre linha clara e host com hash `|1|`. Não abri o `known_hosts` deste PC para ver se a chave que a SSH.NET entrega (bytes crus ou blob wire) é a mesma da linha de `192.168.2.37`. Se não bater, o login recusa — é o comportamento pedido, mas pode ser um falso negativo.
3. **`/usr/sbin/reboot` no Ubuntu 26.04.** Se for link para `systemctl`, a linha do sudoers não vale até ser trocada. O script `aplicar-sudoers.sh` imprime o `readlink` e não edita o arquivo.
4. **Sudoers e grupo `docker` ainda não foram aplicados.** Enquanto `melchior-nopasswd` e o grupo existirem, a trava estreita não segura uma sessão interativa. O exe só emite a lista mesmo assim.
5. **Tarefa 08 da BIOS.** O Liga envia o datagrama e, se a porta 22 não abrir em 2 minutos, avisa. Não marca a tarefa como feita.
6. **`/data` não está montado** (tarefa 06). O painel deve mostrar `ausente`. Não criei o diretório.
7. **Limpeza do WSL não rodou.** O script `scripts/wipe-wsl-docker.ps1` apaga todos os containers e volumes nomeados do Docker em `Ubuntu-24.04` depois que alguém digita `APAGAR`. Imagens ficam. A pasta `C:\GIT\ZAPFCOORP\tailscale-9router` fica. Eu não executei o script.
8. **Pareamento do MausBot** (`docker exec`) continua fora do perfil. Não há botão.
9. **Senha inicial `INITIAL_PASSWORD` no compose do servidor** não foi mexida.
10. **Cópia da senha dentro da SSH.NET** vive até o `Dispose` do cliente. O `char[]` do Z-Lab é zerado ao encerrar a sessão; a biblioteca guarda a cópia dela.
11. **Health HTTP/TCP** continua fora, como na SPEC antiga (RF20).
12. **IPv6 no pull.** O Atualizar roda `docker compose pull` no servidor. Se o daemon de lá preferir AAAA, o stderr volta no painel. O script `fix-wsl-docker-ipv4.sh` não entra mais nesse caminho.
13. **Um Tailscale no WSL, depois da limpeza.** Compose pronto em [`wsl/tailscale-windows/`](../wsl/tailscale-windows/). Tarefa [`.todo/07-tailscale-wsl-windows.md`](../.todo/07-tailscale-wsl-windows.md) **bloqueada** enquanto o Docker do WSL ainda tiver `mausbot`/`9router`/`omniroute`. Depois do wipe + `TS_AUTHKEY` no `.env`, `compose up` e `curl.exe --socks5 127.0.0.1:1055`.

## Cobertura

| Área | Teste automático | Manual |
|------|------------------|--------|
| Argv fechado, sudoers, perfil tailnet | `RemoteCommandCatalogTests` | — |
| Pacote mágico | `WakeOnLanTests` | Liga de verdade, depende da BIOS |
| known_hosts | `KnownHostsTrustTests` | Login real |
| `ps` JSON | `ComposePsParserTests` | Compose v5 no servidor |
| Painel, bandeja, diálogos | — | Abrir o exe no Windows |
| Sudoers na máquina | arquivo no repo irmão | `visudo` pelo operador |
