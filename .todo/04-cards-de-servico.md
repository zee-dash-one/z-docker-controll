# 04: Cards de serviço com mini monitor

Status: concluída em 2026-10-19

Depende de: 03. Leia antes o `00-LEIA-ME.md`.

## Objetivo

Refazer os cards de serviço: cartões arredondados em grade, com pílula de status, endereço como link, um mini monitor de três linhas por container (estado, CPU, memória) e as ações como ícones, com as menos usadas num menu "⋯". Inspiração: cards do Portainer/Dockge e do Homepage, tabela de containers do Beszel.

## Referência visual

`docs/mockups/mockup-com-sessao.png`: seção "Serviços" (seis cards em 3 colunas).

## Layout da seção

- `SectionHeader` "Serviços" com legenda `6 serviços · 5 no ar · 1 parado` ("no ar" em `Ok`), e à direita "atualizado há N s" (`Caption`, `Text3`) + `ZButton` Ghost Compact "Atualizar" (glifo `Sync`), que força um ciclo imediato do `StatsPoller`.
- `ResponsiveGrid` com `MinItemWidth = 360`, `MaxColumns = 3`, `ItemHeight = 184`, `Gap = 12`.

## Anatomia do card (`src/ZLab/Ui/ServiceCard.cs`, herda `RoundedPanel`)

Padding 16 nas laterais, 14 em cima, 10 embaixo.

1. **Topo**: ícone 32 x 32 raio 8, fundo `AccentSoft`, glifo em `Accent` (`Network` para 9Router e OmniRoute, `Robot` para MausBot, `Shield` para os Tailscale; guarde o glifo no catálogo ou derive do tipo do serviço). À direita do ícone: nome (`BodyStrong`) e, embaixo, o endereço **sem `http://`/`https://`** em `Caption` `Accent`, com reticências se não couber; clique abre no navegador (`Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`), cursor de mão. No canto direito: `StatusPill`.
2. **Mini monitor** (12 px abaixo do topo), grade de 3 colunas: chave 64 px (`Caption`, `Text3`) | barra 4 px (`ThinProgressBar`, severidade) | valor 116 px alinhado à direita (`Caption`, `Text2`, números tabulares). Linhas com 8 px entre si:
   - **Estado**: texto na largura das colunas 2 e 3: `Ativo há 2 d 4 h · 0 reinícios` (1 → `1 reinício`). Tempo com `DisplayFormat.Uptime` a partir do `StartedAt`.
   - **CPU**: barra com o % da máquina inteira; valor `0,8%` (1 casa decimal).
   - **Memória**: barra com `usado / limite`; valor `85 MB de 15 GB` (`DisplayFormat.Bytes` nos dois).
3. **Parado / sem container**: no lugar do mini monitor, uma caixa de 68 px com borda tracejada `Stroke2`, raio 8, texto centralizado `Text3` com glifo `Stop`: `Container parado · sem consumo`. Se o container não existir: `Container não criado · use Criar no menu ⋯`.
4. **Barra de ações** (colada embaixo, linha de 1 px `Stroke` em cima, 8 px de respiro): `ZButton` `IconOnly` de 32 x 32 com tooltip:
   - à esquerda: **Parar** (glifo `Stop`) quando no ar, ou **Subir** (glifo `Play`, cor `Ok`) quando parado; **Logs** (glifo `Document`); no MausBot, o botão "Código de acesso" entra aqui na tarefa 05;
   - à direita: **Abrir** (glifo `OpenInNewWindow`, desabilitado se parado) e **⋯** (glifo `More`), que abre um `ContextMenuStrip` com **Criar**, **Atualizar** e **Uso**.
   - Cada ação chama exatamente o que os botões atuais (Subir, Parar, Criar, Atualizar, Logs, Uso, Abrir) já fazem. Não invente ação nova. Confirmação antes de Parar.
   - Pinte o `ContextMenuStrip` com o tema (um `ToolStripProfessionalRenderer` com `ProfessionalColorTable` própria usando `Surface`, `Surface3` e `Text`).

## Estados

| Estado do container (`docker inspect`) | Pílula | Corpo |
|---|---|---|
| `running` | Online, "No ar" | mini monitor |
| `exited`, `created`, `paused` | Stopped, "Parado" | caixa tracejada "Container parado · sem consumo" |
| `restarting`, `dead` | Error, "Erro" | mini monitor com Estado `Reiniciando…` ou `Com erro`, barras vazias |
| container não encontrado | Unknown, "—" | caixa "Container não criado · use Criar no menu ⋯" |
| antes da primeira leitura | Unknown, "—" | mini monitor com "—" |

## Seção de containers no script

Acrescente **no fim** de `scripts/zlab-stats.sh` (mesmo arquivo da tarefa 03, mesma chamada SSH):

```bash
# --- seção containers (tarefa 04) ---
echo "section=containers"
if command -v docker >/dev/null 2>&1; then
  ids=$(docker ps -aq 2>/dev/null)
  if [ -n "$ids" ]; then
    stats=$(docker stats --no-stream --format '{{.Name}}{{"\t"}}{{.CPUPerc}}{{"\t"}}{{.MemUsage}}' 2>/dev/null)
    # shellcheck disable=SC2086
    docker inspect --format '{{.Name}}{{"\t"}}{{.State.Status}}{{"\t"}}{{.State.StartedAt}}{{"\t"}}{{.RestartCount}}' $ids 2>/dev/null \
    | sed 's#^/##' \
    | while IFS=$'\t' read -r name state started restarts; do
        line=$(printf '%s\n' "$stats" | awk -F'\t' -v n="$name" '$1==n{print $2"\t"$3; exit}')
        if [ -n "$line" ]; then cpu=${line%%$'\t'*}; mem=${line#*$'\t'}; else cpu=-; mem=-; fi
        printf 'c=%s\t%s\t%s\t%s\t%s\t%s\n' "$name" "$state" "$started" "$restarts" "$cpu" "$mem"
      done
  fi
else
  echo "docker=absent"
fi
```

- Se o app hoje chama o Docker com prefixo (por exemplo `sudo -n docker`), veja em `DockerCli` e use o mesmo prefixo aqui, de forma consistente.
- Os `{{"\t"}}` nos `--format` geram TAB pelo próprio template do Docker, então o script não depende de TAB literal no arquivo.
- Saída de exemplo (valores de exemplo; o separador é TAB, não espaços):

```
section=containers
c=9router	running	2026-10-16T22:14:03.123456789Z	0	3.21%	85.2MiB / 15.5GiB
c=tailscale-mausbot	exited	2026-10-18T10:00:00Z	2	-	-
```

Campos de cada linha `c=`: nome, estado, início (ISO 8601 UTC com até 9 casas de fração), reinícios, CPU do `docker stats` (pode passar de 100%, porque é por núcleo), memória do `docker stats` (`usado / limite`), ou `-` quando o container não está rodando.

## Parser e regras

- `src/ZLab.Core/Models/ContainerStats.cs`: `record ContainerStats(string Name, string State, DateTimeOffset? StartedAt, int Restarts, double? CpuPercentOfHost, long? MemUsedBytes, long? MemLimitBytes)`.
- `StatsParser` (tarefa 03) passa a preencher `StatsSnapshot.Containers` (`IReadOnlyList<ContainerStats>`).
- **CPU por host** = CPU do Docker ÷ `cores` (vem da seção host; 4 no z-server-01). Ex.: `3.21%` com 4 núcleos → `0,8%`.
- Memória: aceite as unidades `B`, `KiB`, `MiB`, `GiB`, `TiB`, `kB`, `MB`, `GB` (base 1024 para `iB`, 1000 para as outras).
- Data de início: corte a fração para 7 dígitos antes de `DateTimeOffset.Parse` (o .NET não aceita 9). `0001-01-01T00:00:00Z` = sem data.
- Ligação card → container: use o campo que já liga `ServiceItem` ao container/serviço do compose (veja `ServiceItem`, `ComposePsParser`, `ServiceCatalogStore`). Não invente nomes de container no código.
- Se o `StatusWatcher` faz chamadas SSH próprias a cada ciclo para saber o estado, passe a alimentá-lo (ou substituí-lo) com o `StatsSnapshot`, para ficar **uma chamada SSH por ciclo**. Registre a decisão.
- Atualização a cada 5 s, só com sessão aberta (mesmo `StatsPoller`). Atualize só o que mudou nos cards (sem recriar controles a cada ciclo, para não piscar).

## Passos

1. Leia `ContainerService.cs`, `DockerCli.cs`, `ComposePsParser.cs`, `StatusWatcher.cs`, `ServiceCatalogStore.cs`, `ServiceItem.cs` e o trecho do `MainForm` que monta os cards.
2. Estenda o script e teste no servidor só lendo: `Get-Content -Raw .\scripts\zlab-stats.sh | ssh -i C:\Users\htzc\.ssh\id_ed25519_zserver melchior@192.168.2.37 'bash -s'`.
3. Crie `ContainerStats`, estenda o `StatsParser`, crie `ServiceCard`, um presenter puro `ServiceCardPresenter` (de `ServiceItem` + `ContainerStats?` + `cores` para textos, pílula e percentuais) e troque os cards antigos pelos novos no `MainForm`.
4. Testes em `tests/` com dados de exemplo:
   - parser: as duas linhas de exemplo; `-` vira nulo; data com 9 casas; `0001-01-01` vira nulo;
   - memória: `85.2MiB / 15.5GiB`, `512KiB / 1GiB`, `1.2GB / 16GB`;
   - presenter: `3.21%` com 4 núcleos → `0,8%`; `running` → "No ar"; `exited` → "Parado" e caixa tracejada; container ausente → "—" e texto de Criar; `1` reinício no singular.
5. Build, testes, abra o app, entre e compare com o mockup nos dois temas. Redimensione a janela e veja a grade ir para 2 e 1 colunas.

## Aceite

- [ ] Cards arredondados em grade de 3 colunas (2 e 1 em janelas estreitas), iguais ao mockup.
- [ ] Pílula "No ar"/"Parado"/"Erro"/"—" correta; endereço clicável, sem `https://`.
- [ ] Mini monitor com Estado (tempo ativo e reinícios), CPU % da máquina e memória "MB de GB", com barras finas por severidade.
- [ ] Container parado mostra só "Container parado · sem consumo".
- [ ] Ações por ícone com tooltip; Criar, Atualizar e Uso no menu "⋯"; todas fazem o mesmo que antes.
- [ ] Uma chamada SSH a cada 5 s para host + todos os containers, só com sessão; cards não piscam.
- [ ] Testes novos passando.

## Depois

A tarefa 05 acrescenta o botão "Código de acesso" no card do MausBot.

## Registro de execução

- Data: 2026-10-19
- Script: `zlab-stats.sh` ganhou `section=containers` (docker inspect + stats, sem sudo; melchior está no grupo docker).
- Novos: `ContainerStats`, `DockerMemParser`, `ServiceCardPresenter`, `ServiceCard`, `ThemedContextMenu`.
- `StatsParser` preenche `Containers`; `StatsPoller.RequestImmediate` no botão Atualizar; `StatusWatcher.ApplyStatuses` alimentado pelo snapshot (loop SSH do watcher desligado — uma chamada a cada 5 s).
- `DashboardPanel`: grade 3 colunas + resumo "N no ar".
- SSH live no servidor: não revalidado neste passo (IP novo; host pode estar off). Unitários cobrem o formato.
- Testes: 152 ok.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, cada container agora conta em três linhas como está. Do jeito que você gostou da tabela, só que bonito.
