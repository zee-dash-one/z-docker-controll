# 04 — Mini-monitor por container (cards de serviço)

**Status:** pendente. Depende da 01 e da 02 (reaproveita a barra e as cores da tabela de recursos).

## Objetivo
Cada card de serviço (9Router, OmniRoute, MausBot, Tailscale zlab, Tailscale 9Router, Tailscale MausBot) ganha um resumo curtíssimo do que aquele container está consumindo, no mesmo estilo da tabela de recursos: só o que importa.

## Layout esperado dentro de cada card (logo abaixo do status e do endereço)
| Linha | Exemplo | Barra |
|---|---|---|
| Estado | No ar há 3 h · reinícios: 0 | — |
| CPU | 0,4 % | sim |
| Memória | 85 MB de 15 GB | sim |

- Quando o container estiver parado: uma linha só, "Parado" (cinza), sem barras.
- Reinícios > 0 em destaque amarelo; estado "unhealthy" ou "restarting" em vermelho.
- CPU: valor do `docker stats` dividido pelo número de núcleos (4), para ficar de 0 a 100 % do servidor. Mostrar 1 casa.
- Memória: `MemUsage` do `docker stats`; se não houver limite, o total é a RAM do servidor.
- Nada de rede/IO de disco na tela principal (pode ir num tooltip, opcional).

## Coleta (uma chamada SSH para todos os containers)
Criar `scripts/zlab-container-stats.sh` que imprime uma linha por container, separada por `|`:
```bash
#!/usr/bin/env bash
# nome|cpu%|mem_uso|mem_limite|estado|iniciado_em|reinicios|health
docker stats --no-stream --format '{{.Name}}|{{.CPUPerc}}|{{.MemUsage}}' | while IFS='|' read -r n cpu mem; do
  insp=$(docker inspect -f '{{.State.Status}}|{{.State.StartedAt}}|{{.RestartCount}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$n")
  echo "$n|$cpu|${mem% / *}|${mem#* / }|$insp"
done
docker ps -a --filter status=exited --format '{{.Names}}|0%|0B|0B|exited|-|0|none'
```
(Ajuste se o formato real divergir; registre no "Registro de execução".)

## Passos
1. Ler `ContainerService.cs`, `DockerCli.cs`, `ComposePsParser.cs`, `StatusWatcher.cs` e o código dos cards no `MainForm.cs`.
2. Criar o script acima em `scripts/` e o envio/execução dele pela `SshSession` (mesmo mecanismo da tarefa 02).
3. Criar `ContainerStats` (modelo tipado) e `ContainerStatsParser` com testes em `tests/` (incluindo container parado, unhealthy e unidades KiB/MiB/GiB).
4. Mapear cada card ao container pelo nome definido no catálogo (`ServiceCatalogStore`/`services.json`), não por texto fixo.
5. Atualizar junto com o monitor da tarefa 02 (a cada 5 s, só com sessão ativa). Uma chamada SSH serve todos os cards.
6. Reaproveitar o componente de barra/cores da tarefa 02 (verde < 70 %, amarelo 70–90 %, vermelho > 90 %).

## Aceite
- Todos os 6 cards mostram Estado, CPU e Memória, ou "Parado".
- Valores batem com `docker stats --no-stream` rodado no servidor (CPU dividido por 4).
- Parar um container pelo próprio card muda a linha para "Parado" em até 10 s.
- Testes do parser passam; `dotnet build` sem erros; app continua leve (uma chamada SSH por ciclo).

## Depois
README (o que cada número significa) e changelog.

## Revisão UI/UX

Quem opera é o Hil. Os seis cards — 9Router, OmniRoute, MausBot, Tailscale zlab, Tailscale 9Router, Tailscale MausBot — só existem com sessão. A revisão da 01 já os deixa com `Visible = false` sem sessão. O resumo do container entra logo abaixo do status e do endereço, na barra pintada da revisão da 02: faixa cinza claro, preenchimento na cor, verde 0–69, amarelo 70–90 inclusive, vermelho 91–100. Os números trocam a cada 5 s, no mesmo ciclo da tabela, e só enquanto a sessão existe.

### O que o Hil vê

Com sessão, o card continua GroupBox de 340 px, Segoe UI 9, título do serviço. De cima para baixo: título, status, endereço, bloco do container, botões. A janela permanece no tamanho da revisão da 01. O card cresce o bastante para o bloco ficar entre o endereço e os botões, sem cobrir os botões. A faixa dos cards já rola. Os seis cards ficam com a mesma altura, a alta, para Subir e Parar não mudarem de lugar quando um container para.

**9Router no ar:**

| | |
|---|---|
| título | 9Router |
| status | `No ar` |
| endereço | `http://192.168.0.112:20128` |
| estado | `No ar há 3 h 12 min · reinícios: 0` |
| CPU | `0,4 %` e barra verde, curta |
| memória | `85 MB de 15 GB` e barra verde, quase vazia |
| botões | Subir, Parar, Criar / Atualizar, Logs, Uso, Abrir |

**MausBot parado:**

| | |
|---|---|
| título | MausBot |
| status | `Parado` |
| endereço | `https://mausbot-serve.mandrill-court.ts.net` |
| bloco | `Parado` em cinza, uma linha |
| botões | Subir, Parar, Criar / Atualizar, Logs, Uso |

OmniRoute repete o 9Router, com `http://192.168.0.112:20129` e Abrir. Tailscale zlab, Tailscale 9Router e Tailscale MausBot repetem o MausBot: endereço https, sem Abrir.

Sem sessão o card inteiro some, bloco incluído. Não fica a linha cinza, nem barra, nem o último `0,4 %`.

### Decisão

O status curto permanece a primeira linha de texto dentro do card: `No ar`, `Parado`, `Ausente`, `Ocupado`, `Erro`. É a palavra que os botões e a bandeja já usam. A frase com o relógio não entra nessa linha.

O endereço permanece o de hoje. 9Router e OmniRoute mostram a URL lan. MausBot e os três Tailscale mostram o rótulo https. Uma string só. O bloco não repete o endereço.

Com o container no ar, o bloco são três linhas, nesta ordem, uma linha cada:

1. `No ar há 3 h 12 min · reinícios: 0`
2. `CPU`, `0,4 %`, barra
3. `Memória`, `85 MB de 15 GB`, barra

A palavra `Estado` não entra na face. A frase já se lê sozinha. `CPU` e `Memória` entram, no papel da coluna Recurso da tabela. As duas barras começam no mesmo x e têm a mesma largura. A frase de estado não tem barra e não empurra esse x.

**Relógio.** O mesmo formato da linha Ligado há da tabela. Minutos sempre na frase. Abaixo de 1 min: `No ar há 0 min · reinícios: 0`. Abaixo de 1 h: `No ar há 44 min · reinícios: 0`. De 1 h até o fim do primeiro dia: `No ar há 3 h 12 min · reinícios: 0`, e `No ar há 3 h 0 min · reinícios: 0` quando os minutos fecham. De 1 d em diante: `No ar há 2 d 1 h 44 min · reinícios: 0`, com a hora mesmo em zero. O separador é `·`, com espaço dos dois lados. Sem ponto final. `3 h` sozinho não aparece.

**Reinícios.** O trecho `reinícios: 0` fica em toda leitura no ar, zero incluído. O rótulo é `reinícios:` também quando o número é 1. Acima de zero, só esse trecho usa o amarelo da barra, como aviso. O relógio fica na cor normal do rótulo. `reinícios: 2` é amarelo. `No ar há 3 h 12 min` permanece na cor normal.

**Não saudável.** Unhealthy com o container no ar. A frase inteira fica no vermelho da barra: `No ar há 3 h 12 min · não saudável · reinícios: 2`. O vermelho cobre o amarelo dos reinícios. CPU e memória continuam, com barra. `healthy`, `starting` e `none` não acrescentam palavra e não mudam a cor.

**Reiniciando.** Estado restarting, o mesmo momento em que o status curto diz `Ocupado`. A frase do relógio sai. No lugar, uma linha inteira no vermelho da barra: `Reiniciando · reinícios: 4`. CPU e memória ficam quando a amostra traz número. Sem número, as duas linhas ficam `—`, sem barra. Essa face não vira a linha cinza de parado.

**Parado.** Status curto `Parado` (exited, created, paused, dead, removing). O bloco troca as três linhas por uma só, texto exatamente `Parado`, na cor de texto desativado. Sem relógio, sem `CPU`, sem `Memória`, sem barra, sem o último número. O cinza é cor de texto. Não é uma faixa vazia.

O status curto e a linha cinza repetem `Parado` de propósito. O de cima é o controle: Subir habilita, Parar desabilita. O de baixo é o bloco de medida, para o `0,4 %` não congelar depois do Parar.

**Ausente.** Uma linha só, `Ausente`, no mesmo cinza, sem barras. Criar continua o verbo desse estado.

**Erro de estado.** Uma linha só, `Erro`, no vermelho da barra, sem barras.

**Antes da primeira leitura, já com sessão.** O status curto fica `—`. CPU e memória ficam `—`, sem barra. A palavra `Sem sessão` fica na faixa de cima, no estado sem sessão, com os cards ocultos. `—` não é uso zero.

**Relógio ou reinícios ainda sem número, container no ar.** Sem instante: `No ar · reinícios: 0`, sem `há`. Sem a contagem: `No ar há 3 h 12 min`, sem o trecho `reinícios:`. Sem os dois: `No ar`.

**Falha de leitura depois de um valor bom.** O bloco guarda a última frase, os últimos números e as últimas barras. A linha não recebe texto de erro. Isso vale com o container no ar. A linha `Erro` é o estado do container, não uma falha da coleta.

**CPU.** `0,4 %`: uma casa decimal, vírgula, espaço antes do `%`. Também `0,0 %` e `3,0 %`. Meio para cima na casa: `0,04` vira `0,0`; `0,05` vira `0,1`. O valor é o do docker stats dividido por 4 núcleos. O resultado é a porcentagem do servidor, de 0 a 100, a mesma escala da linha CPU da tabela. Acima de 100, o texto trava em `100,0 %` e a barra vai até o fim, vermelha. O card não escreve a porcentagem de um núcleo e não repete `(4 núcleos)`. A tabela já diz os 4 núcleos. `0,0 %` é medida real e tem barra verde vazia. `—` não tem barra.

**Memória.** `85 MB de 15 GB`. Sem `usados` e sem a porcentagem na frase. A barra carrega a porcentagem. Abaixo de 1 GB, MB inteiro, meio para cima: `85 MB`, `0 MB`. Abaixo de 0,5 MB entra em `0 MB`. De 1 GB em diante, o GB da tabela: uma casa, e a casa some quando é zero (`1,2 GB`, `2 GB`). Sem limite de container, o total é o total já escrito na linha Memória da tabela (`15 GB`), o mesmo texto. Com limite, o total é esse limite, na mesma regra de MB e GB. As unidades podem misturar: `85 MB de 15 GB`, `400 MB de 512 MB`, `1,2 GB de 2 GB`. Enquanto uso ou total não chegaram, a linha é `—`, sem barra. `0 MB de 15 GB` é container no ar e leva barra. A linha cinza `Parado` é a outra face.

**Cores da barra.** As da tabela, lendo a parte cheia:

- verde: 0 a 69
- amarelo: 70 a 90, os dois inclusive
- vermelho: 91 a 100

Na CPU a cor lê a casa decimal, para não abrir um buraco entre 90 e 91: verde de `0,0` a `69,9`, amarelo de `70,0` a `90,9`, vermelho de `91,0` a `100,0`. Na memória a cor lê a porcentagem inteira do uso sobre o total escrito, meio para cima, com os cortes da tabela. O preenchimento usa a fração real. `85 MB` de `15 GB` fica verde e quase vazia.

**Como a barra do card fala com a da tabela.** O desenho é um só. Faixa cinza claro, preenchimento na cor. Barras irmãs com a mesma largura e o mesmo começo: na tabela, CPU, memória e discos; no card, CPU e memória. A faixa do card é mais curta em pixels. O olho compara a fração cheia. Metade na tabela e metade no card estão igualmente cheias, cada uma na sua faixa.

A CPU dos dois é quanto do servidor está em uso. A tabela é a máquina, inteira, em porcentagem inteira (`3 % em uso (4 núcleos)`). O card é a fatia daquele container, uma casa, já dividida por 4 (`0,4 %`). Verde ao lado de verde. Os seis cards não formam uma barra empilhada da linha da tabela.

A memória, quando o total do card é o `15 GB` da tabela, mede a mesma RAM. A lasca de `85 MB` cabe dentro do `1,4 GB` da tabela. Quando o total do card é um teto (`512 MB`), a barra muda de escala e passa a dizer o quanto daquele teto está cheio. Ela pode ir ao amarelo com a memória da tabela ainda verde. O total escrito é o aviso da escala.

Disco do sistema, disco Docker e disco de dados continuam só na tabela.

**Rede e IO do container.** Fora da face. Sem tooltip nesta versão. O botão Uso continua levando a leitura crua para a Saída.

**5 s.** Com sessão, os seis blocos e a tabela trocam no mesmo ciclo de 5 s. Uma leitura serve os seis cards. Sem sessão esse ciclo não roda. Durante Subir, Parar, Criar ou Atualizar o bloco continua na tela, com o último valor, até a leitura seguinte. Parar pelo próprio card troca o bloco para `Parado` cinza em até 10 s, e o número anterior sai nessa troca.

O status curto e o bloco pintam juntos. A face não fica com `No ar` em cima e `Parado` cinza no meio. Enquanto a sessão existe, essa leitura é a dona da palavra no card. O watcher não devolve `No ar` no tick seguinte com o bloco já em `Parado`. A bandeja e o aviso de caiu/subiu continuam no watcher.

**Botões.** Texto, ordem e habilitação permanecem. Primeira fileira: Subir, Parar, Criar. Segunda: Atualizar, Logs, Uso, e Abrir só em 9Router e OmniRoute. Uso segue habilitado com o container no ar.

O bloco segue o card do catálogo, pelo id do serviço. A frase não traz o nome do container.

### O que conflita com o card de hoje

O `BuildCard` monta um GroupBox de 340×200. O status sai em `StatusText`: `No ar`, `Parado`, `Ausente`, `Ocupado`, `Erro`, e o ramo restante `Sem sessão`. O endereço fica 22 px abaixo, altura 32, largura 270: `LanUrl` quando existe, senão `HttpsLabel`. Os botões são 80×28, em y=88 (Subir, Parar, Criar) e y=124 (Atualizar, Logs, Uso, e Abrir em x=264 só com URL lan). Entre o endereço e os botões cabem cerca de 10 px.

- O status é uma palavra. Relógio, reinícios e saúde não estão nessa linha.
- `restarting` no parser vira `Ocupado`. A linha nova diz `Reiniciando` em vermelho. O status curto e a bandeja continuam `Ocupado`.
- Uso abre a leitura crua na Saída, no clique. CPU e memória não andam no ciclo do card.
- O watcher dos cards anda em 8 s (`pollSeconds`). A tabela da 02 anda em 5 s. O bloco entra nos 5 s.
- 9Router e OmniRoute também têm rótulo https no catálogo. A face mostra só a URL lan, como hoje.
- `.docs/ui-ux.md` ainda descreve o card como nome, status, URL e os verbos.

### Notas do executor

- Pintar a faixa, no mesmo caminho da tabela. A barra pronta do WinForms permanece verde e ignora a cor pedida.
- `reinícios: N` com N > 0 é um rótulo à parte, na mesma linha, depois do `·`, no amarelo da barra. A frase do relógio fica na cor normal. Não saudável e reiniciando usam um rótulo só, inteiro no vermelho da barra.
- O cinza de `Parado` e `Ausente` é a cor de texto desativado do tema.
- A altura é a mesma nos seis cards, com barra ou com a linha cinza. Os botões não sobem.
- A largura continua 340. A janela continua a da revisão da 01.
- O total de memória sem limite copia o total já formatado da linha Memória da tabela.
- `.docs/ui-ux.md` fica para outra hora. README e changelog continuam na seção **Depois**.
- Esta revisão não desenha o script SSH.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Curtinho como você pediu, Hil: está no ar, quanto de CPU, quanto de memória. Só isso.
