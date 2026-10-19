# 03: Painel do servidor (blocos de métricas)

Status: pendente

Depende de: 02. Leia antes o `00-LEIA-ME.md`.

## Objetivo

Trocar o texto cru de `uptime`, `free -h` e `df -h` (cabeçalhos na mesma linha dos valores, totais espremidos no fim) por **seis blocos de métricas**, no estilo do Gerenciador de Tarefas do Windows 11 e de monitores como Beszel. Só o que importa, em linguagem simples. É o quadro amarelo do Hil.

## Referência visual

`docs/mockups/mockup-com-sessao.png`: a linha de seis blocos logo abaixo de "Servidor".

## Layout

Abaixo do `SectionHeader` "Servidor" (tarefa 02), uma linha de 6 `MetricTile` (tarefa 01) com gap 12, altura 118, largura igual dividida (`TableLayoutPanel` de 6 colunas a 16,66%). Regra fixa: com 1100 px ou mais de largura útil, 6 colunas numa linha; abaixo disso, 3 colunas x 2 linhas (nunca 4 ou 5). 24 px de espaço até a seção "Serviços".

| # | Bloco | Ícone | Título | Número grande (`ValueParts`) | Legenda | Canto direito | Rodapé | Tooltip |
|---|---|---|---|---|---|---|---|---|
| 1 | CPU | `Cpu` | "CPU" | `("7","%")` | "média dos 4 núcleos" | nenhum | `Sparkline` dos últimos 60 s (12 amostras de 5 s), escala 0 a 100 | "Carga média (1, 5 e 15 min): 0,10 · 0,05 · 0,04. É quantos processos, em média, querem CPU ao mesmo tempo. Com 4 núcleos, abaixo de 4 está tranquilo." |
| 2 | Memória | `Ram` | "Memória" | `("1,4","GB")` (em uso) | "de 15 GB · 14 GB livres" | "9%" | barra 6 px por severidade | "Em uso = total − disponível (o mesmo critério do Gerenciador de Tarefas)." |
| 3 | Disco do sistema | `HardDrive` | "Disco do sistema" | `("12","GB")` (usado) | "de 49 GB · 74% livre" | "26%" (usado) | barra 6 px | "Partição / (ubuntu-lv)" |
| 4 | Disco Docker | `HardDrive` | "Disco Docker" | `("91","MB")` | "de 147 GB · 99% livre" | "1%" | barra 6 px | "Partição /var/lib/docker (docker-lv)" |
| 5 | Dados | `HardDrive` | "Dados /data" | ausente: "Não instalado" (`IsEmpty = true`) | "HD de 500 GB ainda não montado" | nenhum | nenhum | "Quando /data for montado, este bloco mostra o uso igual aos outros discos." Presente: igual aos discos 3 e 4. |
| 6 | Ligado há | `Clock` | "Ligado há" | `("11","h"),("4","min")` | "desde ontem, 15:26" | nenhum | texto com glifo `People`: "2 usuários conectados" (1 → "1 usuário conectado", 0 → "nenhum usuário conectado") | nenhum |

Antes da primeira leitura, ou se uma leitura falhar, os números mostram "—" e as barras ficam vazias. A **carga média não aparece mais como linha**: ela só existe no tooltip da CPU.

## Formatos (classe `src/ZLab.Core/Services/DisplayFormat.cs`, cultura pt-BR)

- **Bytes** (`DisplayFormat.BytesParts(long bytes)` → `(string Number, string Unit)`, base 1024, rótulos "MB"/"GB"/"TB" como no Windows): menos de 1 GiB → MB inteiro (`91 MB`); de 1 a 10 GiB → GB com 1 casa (`1,4 GB`); 10 GiB ou mais → GB inteiro, arredondando com `MidpointRounding.AwayFromZero` (`12 GB`, `147 GB`); 1 TiB ou mais → TB com 1 casa. `DisplayFormat.Bytes(long)` junta os dois com espaço.
- **Percentual**: inteiro (`9%`, `74%`); se for entre 0 e 1, mostra `<1%`. Percentual da CPU: inteiro.
- **Tempo ligado** (`DisplayFormat.UptimeParts(TimeSpan)`): menos de 1 h → `[("N","min")]`; menos de 1 dia → `[("H","h"),("M","min")]`; 1 dia ou mais → `[("D","d"),("H","h")]`. `DisplayFormat.Uptime` junta: `11 h 4 min`, `2 d 4 h`, `35 min`.
- **"desde"** (`DisplayFormat.Since(DateTime boot, DateTime now)`): mesmo dia → `desde hoje, 15:26`; dia anterior → `desde ontem, 15:26`; antes disso → `desde 16/10, 15:26`. O horário do boot é `now_epoch − uptime_s`, convertido para o horário local do PC.

## Script remoto (`scripts/zlab-stats.sh`)

Crie exatamente este script (testado num Linux; LF, UTF-8 sem BOM). A tarefa 04 acrescenta a seção de containers no fim do mesmo arquivo, para que **uma única chamada SSH a cada 5 s** traga tudo.

```bash
#!/usr/bin/env bash
# zlab-stats.sh: leitura do z-server-01 para o Z-Lab. Só lê, não grava nada.
# Saída: uma linha "chave=valor" por métrica. Versão do formato em "v".
set -u
export LC_ALL=C
echo "v=1"
echo "now_epoch=$(date +%s)"
read -r up _ < /proc/uptime
echo "uptime_s=${up%.*}"
echo "users=$(who | wc -l)"
echo "cores=$(nproc)"
read -r _ u n s idle iow irq sirq steal _ < /proc/stat
echo "cpu_total=$((u + n + s + idle + iow + irq + sirq + steal))"
echo "cpu_idle=$((idle + iow))"
awk '/^MemTotal:/{print "mem_total_kb="$2} /^MemAvailable:/{print "mem_available_kb="$2}' /proc/meminfo
read -r l1 l5 l15 _ < /proc/loadavg
echo "load=$l1 $l5 $l15"
disk() { # $1 = chave, $2 = ponto de montagem
  if [ "$2" = "/" ] || mountpoint -q "$2" 2>/dev/null; then
    df -B1 --output=used,size "$2" 2>/dev/null | awk -v k="$1" 'NR==2{print "disk_"k"="$1" "$2}'
  else
    echo "disk_$1=absent"
  fi
}
disk root /
disk docker /var/lib/docker
disk data /data
```

Exemplo de saída (valores de exemplo):

```
v=1
now_epoch=1792387824
uptime_s=39840
users=2
cores=4
cpu_total=19971908
cpu_idle=19568684
mem_total_kb=16211000
mem_available_kb=14740000
load=0.10 0.05 0.04
disk_root=12884901888 52613349376
disk_docker=95420416 157840048128
disk_data=absent
```

Regras do formato: uma métrica por linha, `chave=valor`; discos como `usados_bytes total_bytes` ou `absent`; linhas desconhecidas são ignoradas (para o formato crescer sem quebrar); `section=...` marca o começo de outra seção (a da tarefa 04).

### Como executar

- Inclua o script como recurso embutido do `ZLab.Core` (`<EmbeddedResource Include="..\..\scripts\zlab-stats.sh" LogicalName="ZLab.Scripts.zlab-stats.sh" />`, ajuste o caminho relativo).
- Leia o recurso, troque `\r\n` por `\n` e envie pela sessão SSH para `bash -s` (stdin). Veja em `SshSession`/`RemoteCommands` como os comandos são executados hoje e acrescente um método `RunScriptAsync(string scriptText, CancellationToken)` se não houver equivalente. Nada é gravado no servidor.
- Acrescente em `.gitattributes`: `scripts/*.sh text eol=lf`.

## Parser e modelo

- `src/ZLab.Core/Models/HostStats.cs`: `record HostStats(TimeSpan Uptime, DateTimeOffset ServerNow, int Users, int Cores, ulong CpuTotal, ulong CpuIdle, long MemTotalBytes, long MemAvailableBytes, double Load1, double Load5, double Load15, DiskUsage Root, DiskUsage Docker, DiskUsage Data)` e `record DiskUsage(bool Present, long UsedBytes, long TotalBytes)` com propriedades calculadas `UsedPercent` e `FreePercent`. `MemUsedBytes = MemTotalBytes − MemAvailableBytes`.
- `src/ZLab.Core/Services/StatsParser.cs`: `static StatsSnapshot Parse(string output)`. `StatsSnapshot` tem `HostStats? Host` (e, na tarefa 04, a lista de containers). Use `CultureInfo.InvariantCulture` para ler números. Linha sem `=` ou com valor inválido é ignorada; se faltar uma chave obrigatória, `Host` vem nulo.
- `src/ZLab.Core/Services/CpuUsageCalculator.cs`: guarda a amostra anterior e devolve `double? Percent(ulong total, ulong idle)` = `(Δtotal − Δidle) / Δtotal × 100`. Primeira amostra, `Δtotal ≤ 0` ou contador que voltou (servidor reiniciou) → `null` e recomeça.
- `src/ZLab.Core/Services/StatsPoller.cs`: a cada 5 s, **só com sessão aberta**, executa o script; se a execução anterior ainda não terminou, pula o ciclo; publica `event Action<StatsSnapshot>`. `Start()` no login e `Stop()` no Encerrar sessão (tarefa 02). Erros: um aviso na Saída na primeira falha seguida, não a cada 5 s.
- O `MainForm` só traduz `StatsSnapshot` para as propriedades dos blocos (de preferência numa classe pura `ServerTilesPresenter` que devolve os textos e percentuais de cada bloco, fácil de testar).
- Substitua o uso de `ServerMonitor`/`HostReadings` na UI por este fluxo. Se ficarem sem uso, remova-os junto com os testes deles e registre isso.

## Passos

1. Leia `ServerMonitor.cs`, `HostReadings.cs`, `RemoteCommands.cs`, `SshSession.cs` e o trecho do `MainForm` que mostra "Tempo no", "Carga", "Memória" e "Disco".
2. Crie o script, o recurso embutido, os modelos, o parser, a calculadora de CPU, o `DisplayFormat`, o `StatsPoller` e o presenter.
3. Teste o script no servidor só lendo: no PowerShell, `Get-Content -Raw .\scripts\zlab-stats.sh | ssh -i C:\Users\htzc\.ssh\id_ed25519_zserver melchior@192.168.0.112 'bash -s'`. Confira se `disk_docker` vem com números e `disk_data=absent`. Se `/var/lib/docker` aparecer como `absent`, registre e use `df` sem o teste de `mountpoint` só para essa chave.
4. Testes em `tests/` (dados de exemplo, nada real):
   - `StatsParser`: a saída de exemplo acima vira os valores certos; `disk_data=absent` → `Present = false`; linha lixo é ignorada; faltando `mem_total_kb` → `Host` nulo.
   - `CpuUsageCalculator`: primeira amostra `null`; duas amostras dão o percentual certo; contador menor que o anterior → `null`.
   - `DisplayFormat`: `95420416` → `91 MB`; `1503238554` → `1,4 GB`; `12884901888` → `12 GB`; `157840048128` → `147 GB`; uptime 39840 s → `11 h 4 min`; 187200 s → `2 d 4 h`; 2100 s → `35 min`; `Since` para hoje, ontem e antes.
   - `ServerTilesPresenter`: memória `9%` e legenda `de 15 GB · 14 GB livres` para o exemplo; `/data` ausente gera bloco vazio "Não instalado".
5. Build, testes, abra o app, entre, e compare a linha de blocos com o mockup nos dois temas.

## Aceite

- [ ] Nenhum texto cru de `uptime`, `free` ou `df` na tela.
- [ ] Seis blocos como no mockup: CPU com % e sparkline; Memória "X GB" + "de Y GB · Z GB livres" + % + barra; Disco do sistema e Disco Docker com usado, total, % livre e barra; Dados "Não instalado"; Ligado há com tempo legível e usuários conectados.
- [ ] Barras verdes abaixo de 70%, amarelas de 70% a 90%, vermelhas acima de 90%.
- [ ] Carga média só no tooltip da CPU, com a explicação simples.
- [ ] Uma chamada SSH a cada 5 s, só com sessão; ao encerrar a sessão, as chamadas param.
- [ ] Testes novos passando.

## Depois

A tarefa 04 acrescenta a seção de containers ao mesmo script e refaz os cards.

## Registro de execução

(preencher ao executar)

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, a "carga" que não dizia nada virou dica no mouse. Agora é bater o olho e entender.
