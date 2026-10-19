# Plano de melhoria — z-one

Página estática (`index.html`, `css/styles.css`, `js/main.js`). A linguagem visual permanece: fundo `#0b0b0b`, texto `#f0ebe3`, acento `#c8161c`, Syne, IBM Plex Mono, régua, grade de 1px, cantos retos.

Não entram rebrand, framework, depoimento inventado, preço, chatbot, modelo no navegador, parallax, confete, contador ou widget.

## Página

- Ordem e menu: Sobre (01), Atuação (02), Projetos (03), Contato (04).
- Hero: botão primário “Falar com a z-one” → `#contato`. Ghost: “Ver projetos” → `#projetos`. Instagram sai do hero. O h1 pode continuar “Serviços de TI e automação orientados por IA”. Lead, sem sigla: “A z-one entrega sistemas e automações sob medida para empresas, do desenho da rotina até a ferramenta em uso.” Abaixo, links internos para z-team e Devotio.
- Sobre: um trabalho começa por conversa, especificação e ciclos. MCP sai deste bloco.
- Atuação, sem caso inventado: “A z-one pega uma rotina da empresa — atendimento, operação, um processo que ainda vive em planilha — e entrega o sistema que passa a fazer esse trabalho.” Três capacidades em linguagem comum, mais uma frase de exemplos de fluxo (pedido repetido no WhatsApp, planilha de comissão, estoque que não fecha), sem nome, número ou “fizemos para X”. Software aponta para Devotio; orquestração, para z-team. Jogos não entram na lista. A seção termina no botão de contato.
- Projetos: z-team (Ferramenta própria) e Devotio (Produto próprio) abrem a grade. Neon Maze, Cubu Dash e Ikarus ficam depois, como Jogo autoral, sob o título “Ofício”. Ikarus leva o badge “Pré-produção”. Bellspretty e Homeschooling saem da grade para uma linha: “Bellspretty e Homeschooling — co-criação, ainda sem página pública.” O link do repositório diz “Repositório”; a URL fica em `title` e `aria-label`.
- Siglas: hero, menu, Sobre, Atuação, Contato, botões e títulos não usam MCP, LangGraph, Godot, Canvas, Vite, Three.js nem TypeScript. No card do z-team, a primeira frase é comum (“Time de agentes que especifica, constrói e revisa o software e deixa registro do que cada um fez.”); MCP pode aparecer na segunda. Nos jogos, a frase descreve a experiência, sem nome de motor.

## Suco

Alvo 7/10, só mecânico.

- Barra de 2px no botão primário; `:active` desce 1px só em `.btn`.
- Aresta esquerda do card via `::before`; fundo `#161616` no hover e no `:focus-within`. O card não se desloca. z-team permanece com a aresta acesa em repouso.
- Seção visível com `aria-current` e fio de 2px no item do menu. O traço da régua salta entre quatro posições. Texto: `00` no hero, `SYS 0N` nas seções.
- Anel `:focus-visible` de 2px `#c8161c`, offset 3px. Skip-link permanece.
- Parágrafo longo em `#aea99f`. Título, menu e botão continuam na cor atual. `#9a968d` fica para rótulo e meta.
- `scroll-padding-top` cobre header + régua. No celular, o menu abre abaixo dessa pilha. Foco entra no primeiro link; Tab circula no menu aberto; Escape fecha e devolve o foco; o rótulo alterna “Menu” / “Fechar”.

Ficam de fora: parallax, canvas, rastro de cursor, degradê, `scale` no card, fade de seção, typewriter no h1. Com `prefers-reduced-motion`, somem smooth scroll, transição, barra e deslocamento. A régua só troca o texto.

## Contato

“Falar com a z-one” desce até `#contato`. Título: **Contato**. Uma linha acima das portas: “Escolha um canal e descreva o problema.”

Três portas na mesma lista, uma ação por linha. Hover: fio de 2px na borda esquerda. Press: 1px.

1. **Instagram** — rótulo Instagram, ação “Mensagem no Instagram · @zeedashone”. Abre `https://ig.me/m/zeedashone` em nova aba. Reserva: `https://www.instagram.com/zeedashone/`.
2. **WhatsApp** — sem número publicado, a linha fica visível, sem `href`, sem `wa.me` e sem número na tela. Ação: “Número ainda não publicado.” Vira link só quando existir um número real numa constante.
3. **E-mail** — rótulo E-mail, ação “Escrever por e-mail”. Abre a ficha no próprio bloco.

Campos: Nome, E-mail (obrigatório), Telefone (opcional), Mensagem. Placeholder: “Qual problema você quer resolver?” Botão: **Enviar**.

Fora do padrão, uma linha:

- E-mail: “Esse e-mail não parece válido.”
- Telefone, só se preenchido: “Telefone incompleto. Use DDD e número.”

O envio é um POST para `https://formsubmit.co/ajax/zee.dash.one@gmail.com`. Sucesso só depois da resposta ok: a ficha some e fica “Mensagem enviada. A z-one responde no e-mail informado.” Falha (rede, 4xx/5xx ou endereço ainda não ativado): os campos permanecem e aparece “Não enviou. Tente de novo ou fale pelo Instagram.”

Saem deste bloco o LinkedIn como ação principal e o título “Presença”.

Não fazer: chatbot, modelo no browser, entrevista em várias etapas, “mensagem enviada” sem POST ok, widget, bolha, confete, número de WhatsApp inventado.

## Implementação

1. **`index.html`** — reordenar seções e menu; hero, Sobre e Atuação; rótulos e ids dos cards (`#projeto-z-team`, `#projeto-devotio`), grupo Ofício, badge do Ikarus, linha dos co-criados; `#contato` com as três portas e a ficha.
2. **`css/styles.css`** — `--header-stack`, `--text-body`, duração e curva; scroll-padding; seção ativa; hover de card, botão e porta; ficha; foco; menu móvel; reduced motion.
3. **`js/main.js`** — seção ativa e `SYS 0N`; Menu/Fechar, Escape e Tab; validação de e-mail e telefone; POST do Formsubmit e os dois estados (enviado / não enviou).

## Pendência do dono

Aceitar uma vez o e-mail de ativação do Formsubmit em zee.dash.one@gmail.com. Passar o número de WhatsApp, se a segunda porta deve abrir conversa. Um caso real de operação entra na página só no dia em que existir.

## Serviços locais — MausBot, 9Router e OmniRoute

Isto não entra na página. O Router Control, projeto local em C#, é quem sobe e supervisiona os Dockers. O objetivo é deixar MausBot, 9Router e OmniRoute na tailnet do Hil, só entre os dispositivos dele, e capazes de se chamar uns aos outros. Sem Funnel público, sem MausBot Cloud e sem misturar com a VPN corporativa.

O navegador do bot não alcança o localhost do PC sem túnel. Todo acesso remoto passa pelo Tailscale.

### O que já roda

Validado em 16/10/2026 no PC NBPITANG-491. Docker Engine 29.8.1 no WSL2, distro Ubuntu-24.04.

| Serviço | Onde escuta | Papel |
| --- | --- | --- |
| 9Router | `0.0.0.0:20128` | Docker no WSL. TTS gratuito via Edge. Exposição remota já planejada com Tailscale Serve (projeto tailscale-9router). |
| OmniRoute | `0.0.0.0:20129` | Gateway de modelos: escolhe o provedor mais barato antes de gastar token caro. |
| MausBot | `127.0.0.1:8799` (UI), `:8800` (webhooks) | Novo. Ainda sem provedor de IA. |

MausBot (OpenMausBot, [mausbot.com](https://mausbot.com), repositório `milind-soni/OpenMausBot`) é a alternativa gratuita e open source (Apache 2.0) ao Grok Bot. Monta uma equipe de bots com rotinas, triggers, webhooks, voz e apps via Composio. Roda na máquina, com as contas que o Hil já tem: Claude Code, Codex ou Grok CLI. O app e o servidor self-hosted são gratuitos. O MausBot Cloud (trial de 3 dias com cartão) não entra. O uso de modelo é cobrado só pelo provedor que for conectado.

Estado do container `mausbot`:

- Imagem `ghcr.io/milind-soni/openmausbot:latest`, edição open source, sem `OMB_LICENSE_KEY`.
- `--network host` e `--restart unless-stopped`. O servidor escuta só em `127.0.0.1` dentro do container, por isso host network em vez de `-p`.
- Volume `mausbot-data` em `/data`.
- UI em `http://localhost:8799` (HTTP 200 no Windows). Webhooks em `:8800`.
- A imagem foi baixada sem o build arg `ENGINES`, então nenhum provedor de IA está na imagem.
- Código de pareamento: `docker exec mausbot node dist-server/openmausbot.js pair`.

### O que o Router Control faz

Configuração declarativa num `services.json` ou `appsettings.json`: uma entrada por serviço, com nome, imagem, portas, volumes, variáveis, healthcheck e como expor no Tailscale.

Ciclo de vida do `mausbot`, via Docker.DotNet ou `wsl -d Ubuntu-24.04 -- docker ...`: pull, create, start, stop, restart, status, logs e update (pull + recreate preservando o volume).

- Healthcheck: GET em `http://localhost:8799/` espera 200, intervalo configurável. Se falhar, reinicia e registra no log.
- Engines opcionais: rebuild da imagem com o build arg `ENGINES` (Claude Code, Codex ou Grok CLI). Logins ficam no volume. Chave nunca em texto puro: DPAPI do Windows ou variável secreta.
- Pareamento: comando ou botão que roda o `docker exec` acima e mostra o código.
- Atalho no Windows: `.bat` ou `.lnk` que faz `docker start mausbot` e abre a URL local ou a da tailnet.
- Tailscale: detectar se está instalado e logado (`tailscale status --json`); aplicar ou remover o `serve` por serviço; listar as URLs da tailnet; guardar a auth key com DPAPI.

### Exposição na tailnet

Tailscale no Windows ou no WSL. `tailscale serve` faz proxy das portas locais. Resultado: `https://<maquina>.<tailnet>.ts.net:<porta>`.

- `tailscale serve --bg --https=8799 http://127.0.0.1:8799` — MausBot
- `tailscale serve --bg --https=20128 http://127.0.0.1:20128` — 9Router
- `tailscale serve --bg --https=20129 http://127.0.0.1:20129` — OmniRoute

Assim um serviço chama o outro pelo nome da tailnet, sem publicar nada na internet. Alternativa, se fizer falta um nó por serviço (`mausbot.<tailnet>.ts.net`): container sidecar `tailscale/tailscale` com `TS_AUTHKEY` e `TS_SERVE_CONFIG`, na mesma rede do serviço. A auth key também fica no DPAPI.

Webhooks do MausBot (`:8800`) só entram no Tailscale se forem necessários. Webhook vindo da internet exigiria Funnel, e isso só com pedido explícito do Hil.

O MausBot trata requisição local como “owner trust”. Atrás do `tailscale serve`, o tráfego chega como local. O limite é a ACL da tailnet (só os dispositivos do Hil) mais o pareamento. Nada de `0.0.0.0` novo sem necessidade.

### Aceite

- `http://localhost:8799` abre no Windows e `https://<maquina>.<tailnet>.ts.net:8799` abre no celular do Hil, na tailnet.
- 9Router e OmniRoute respondem pelas URLs da tailnet e os três se alcançam entre si por esses endereços.
- Depois de reiniciar o PC ou o WSL, o Router Control sobe de novo e o healthcheck fica verde.
- Nenhum custo: sem MausBot Cloud, no plano gratuito do Tailscale.
- Testes no Router Control para o healthcheck e para o parser da configuração.
