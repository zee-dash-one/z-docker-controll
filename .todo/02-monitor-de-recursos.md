# 02 — Monitor de recursos legível

**Status:** pendente. Depende da 01 (painel só aparece com sessão).

## Objetivo
Trocar o bloco de texto cru (quadro amarelo do print) por uma tabela limpa, estilo Gerenciador de Tarefas: poucos dados, cada um numa linha, com números já interpretados.

## Layout esperado (uma tabela de 2 ou 3 colunas: Recurso | Valor | Barra de uso)
| Recurso | Valor exibido | Barra |
|---|---|---|
| Ligado há | 1 h 44 min (a partir de `/proc/uptime`) | — |
| Usuários conectados | 2 | — |
| CPU | 3 % em uso (4 núcleos) | sim |
| Memória | 1,4 GB de 15 GB usados (9 %) | sim |
| Disco do sistema (/) | 12 GB de 49 GB usados — 74 % livre | sim |
| Disco Docker (/var/lib/docker) | 0,1 GB de 147 GB usados — 99 % livre | sim |
| Disco de dados (/data) | "não instalado" quando ausente | sim |

- **Carga (load average) sai da tela principal.** No lugar, mostrar **CPU em %**. Cálculo: ler `/proc/stat` duas vezes com ~1 s de intervalo e calcular (1 − Δidle/Δtotal) × 100. Se quiser manter a carga, deixar só como tooltip na linha CPU com explicação simples: "média de tarefas esperando CPU; com 4 núcleos, abaixo de 4 está tranquilo".
- Memória: usar `MemTotal` e `MemAvailable` de `/proc/meminfo` (usado = total − disponível). Ignorar shared/buff/cache/swap na tela.
- Discos: usar `df -B1 --output=target,size,used,avail /` (e os outros pontos) e formatar em GB com 1 casa. Não exibir cabeçalhos do `df` nem nomes `/dev/mapper/...`.
- Cores da barra: verde < 70 %, amarelo 70–90 %, vermelho > 90 %.

## Passos
1. Ler `src/ZLab.Core/Services/ServerMonitor.cs`, `HostReadings.cs`, `RemoteCommands.cs` e onde o MainForm desenha esse bloco.
2. Fazer o servidor retornar dados **parseáveis**: um único comando SSH que imprime chave=valor (ex.: `uptime_s=`, `users=`, `cpu_idle1=`/`cpu_total1=`/… , `mem_total_kb=`, `mem_avail_kb=`, `disk_root_size=`, …). Preferir um script versionado em `scripts/` e chamado pelo app, para evitar escaping no PowerShell/C#.
3. Criar/ajustar um modelo tipado (ex.: `HostReadings` com propriedades numéricas) e um parser com testes unitários em `tests/` usando saídas de exemplo.
4. Trocar o controle de texto por `TableLayoutPanel` (ou `ListView` em modo Details) com `ProgressBar` por linha.
5. Atualizar a cada 5 s só enquanto houver sessão.

## Aceite
- Tabela com as 7 linhas acima, sem texto cru do `free`/`df`.
- Valores batem com `free -h` e `df -h` rodados no servidor.
- `/data` mostra "não instalado" até a tarefa do HD de 500 GB ser feita.
- Testes do parser passam; `dotnet build` sem erros.

## Depois
README e changelog.

## Revisão UI/UX

Quem opera é o Hil. A tabela só existe com sessão SSH: a revisão da 01 já deixou as leituras com `Visible = false` sem sessão. Com sessão, ela troca o bloco cru por números que o Hil lê sem passar pela listagem de memória nem pela listagem de disco. Os valores mudam a cada 5 s, e só enquanto a sessão existe.

### O que o Hil vê

Com sessão, na faixa em que hoje ficam as seis linhas, entre os grupos Sessão e Servidor e os seis cards. Sem GroupBox em volta. Cabeçalho `Recurso` | `Valor` | `Barra`. Sete linhas, nesta ordem, uma linha cada, Segoe UI 9, como o resto do painel:

| Recurso | Valor | Barra |
|---|---|---|
| Ligado há | `1 h 44 min` | — |
| Usuários conectados | `2` | — |
| CPU | `3 % em uso (4 núcleos)` | verde, curta |
| Memória | `1,4 GB de 15 GB usados (9 %)` | verde, curta |
| Disco do sistema (/) | `12 GB de 49 GB usados — 74 % livre` | verde, curta (a parte usada) |
| Disco Docker (/var/lib/docker) | `0,1 GB de 147 GB usados — 99 % livre` | verde, quase vazia |
| Disco de dados (/data) | `não instalado` | — |

Passar o mouse na linha CPU mostra a carga. No exemplo: `Carga (agora, 5 min, 15 min): 0,15, 0,10, 0,05. Média de tarefas esperando CPU; com 4 núcleos, abaixo de 4 está tranquilo.`

Sem sessão a tabela inteira some, cabeçalho incluído. Não fica `não instalado`, nem barra vazia, nem número antigo. O vão abaixo dos dois grupos é o da revisão da 01. A janela não muda de tamanho.

### O que conflita com o bloco de texto de hoje

O `MainForm` desenha seis rótulos e seis caixas só leitura (`_uptime`, `_load`, `_memory`, `_diskRoot`, `_diskDocker`, `_diskData`), cada caixa com 840 px. Não há colunas, cabeçalho nem barra.

- `Tempo no ar` recebe a linha inteira: relógio, tempo, usuários e carga no mesmo campo.
- `Carga` está na tela principal, com a linha crua das médias e o resto que vem junto.
- Não há linha `Usuários conectados`. Esse número, quando aparece, vem misturado no tempo no ar.
- `Memória` recebe a listagem completa: cabeçalho, memória, swap, shared e buff/cache.
- `Disco /`, `Disco Docker` e `Disco /data` recebem cabeçalho e o nome do dispositivo (`/dev/mapper/...`).
- Com `/data` ausente a caixa diz `ausente`. Outra falha escreve `falhou` ou o texto do erro dentro da caixa.
- As leituras andam no ritmo do watcher, 8 s (`pollSeconds` em `config/monitor.settings.json`), no mesmo ciclo dos cards.
- `.docs/ui-ux.md` ainda descreve a faixa como "Tempo no ar / Carga / Memória / Disco / / Docker / /data".

### Decisão

Ordem fixa, de cima para baixo: Ligado há; Usuários conectados; CPU; Memória; Disco do sistema (/); Disco Docker (/var/lib/docker); Disco de dados (/data). O cabeçalho não é botão: clique não classifica e não troca coluna de lugar. Nome, valor e barra cabem numa linha só. As barras compartilham a mesma largura de faixa e o mesmo começo, para 3 % e 90 % se compararem no olho. A faixa de fundo é cinza claro. O preenchimento leva a cor. A fonte monoespaçada continua só na Saída.

**Ligado há.** Minutos sempre na frase, segundos de fora. Abaixo de 1 h: `44 min`. Abaixo de 1 min: `0 min`. De 1 h até o fim do primeiro dia: `1 h 44 min` (e `2 h 0 min` quando os minutos fecham). De 1 d em diante: `2 d 1 h 44 min`, com a hora mesmo em zero (`1 d 0 h 44 min`). Sem barra.

**Usuários conectados.** Só o inteiro (`2`, `1`, `0`). É a contagem de logins no z-server-01. Sem barra.

**CPU.** `{n} % em uso (4 núcleos)`. Porcentagem inteira, com espaço antes do `%`. No z-server-01 o parêntese lê `4 núcleos`. A barra usa essa mesma porcentagem. Enquanto a primeira porcentagem não chegou, o valor é `—` e a linha ainda não tem barra. `—` não é uso zero.

A carga sai da tela principal. Fica só o tooltip da linha CPU (rótulo, valor e barra). Com as três médias, duas casas e vírgula: `Carga (agora, 5 min, 15 min): 0,15, 0,10, 0,05. Média de tarefas esperando CPU; com 4 núcleos, abaixo de 4 está tranquilo.` Sem as três médias, o tooltip é a frase: `Média de tarefas esperando CPU; com 4 núcleos, abaixo de 4 está tranquilo.` Nenhum outro controle mostra a carga.

**Memória.** `{usado} de {total} usados ({n} %)`. Unidade sempre `GB`, vírgula decimal, uma casa; a casa some quando é zero (`1,4 GB`, `15 GB`, `0,1 GB`). A palavra `usados` fica em toda leitura, inclusive em `0` e em `1`. A porcentagem é inteira e é a parte usada: total menos o que ainda está disponível. A barra repete essa porcentagem. A linha não mostra shared, buff/cache nem swap.

**Discos com tamanho.** `{usado} de {total} usados — {livre} % livre`, no mesmo GB da memória. O travessão é `—`. A porcentagem escrita é a parte livre. A barra e a cor usam a parte usada, também inteira. Livre e usada não precisam somar 100. No sistema, `12 GB de 49 GB usados — 74 % livre` leva barra curta e verde (12 de 49 está abaixo de 70). No Docker, `0,1 GB de 147 GB usados — 99 % livre` leva barra quase vazia e verde. O 99 da frase é espaço livre; não pinta a barra de vermelho.

**Disco de dados ausente.** O valor é exatamente `não instalado`, sem aspas e sem ponto. Sem barra. A palavra `ausente` sai desta linha.

**Quando a barra existe.** Ligado há e Usuários conectados: nunca. CPU, Memória, Disco do sistema (/) e Disco Docker (/var/lib/docker): assim que a porcentagem usada está no valor. Disco de dados (/data): só quando o valor é a frase de disco com tamanho. `não instalado` e `—` ficam com a célula da barra vazia.

**Cores.** A cor lê a porcentagem inteira da barra (parte usada):

- verde: 0 a 69
- amarelo: 70 a 90, os dois inclusive
- vermelho: 91 a 100

**Falha depois de um valor bom.** A linha guarda o último valor e a última barra. A célula não recebe texto de erro. Na primeira leitura, o que ainda não chegou permanece `—`, sem barra.

**5 s.** Com sessão, os números trocam a cada 5 segundos. Sem sessão a tabela está oculta e essa troca não roda. A tabela segue visível e legível enquanto um comando dos cards ou do grupo Servidor está em andamento.

A janela permanece no tamanho da revisão da 01. A tabela ocupa a faixa das seis linhas de hoje; as linhas ficam um pouco mais próximas para caber o cabeçalho e a sétima linha, sem segunda linha de texto e sem empurrar os cards.

## Notas do executor

- A barra pronta do WinForms, com o tema do Windows, permanece verde e ignora a cor pedida. Pintar o preenchimento (verde, amarelo, vermelho) na faixa da linha.
- Os 5 s são desta tabela. Cards e bandeja continuam em `pollSeconds` (8). O tooltip da bandeja segue os seis serviços.
- A célula não recebe a listagem de memória nem a de disco. O "usados" da memória é o total menos o disponível, e pode divergir da coluna used que o `free -h` mostra no servidor.
- `.docs/ui-ux.md` fica para outra hora, como na tarefa 01. README e changelog continuam na seção **Depois**.
- Remover as seis caixas. A linha Carga não permanece ao lado da tabela.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Chega de load average na sua cara, Hil. Agora é porcentagem e barra.
