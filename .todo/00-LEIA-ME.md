# Z-Lab: redesign da interface (LEIA PRIMEIRO)

Este arquivo é o ponto de entrada. Leia ele inteiro antes de abrir qualquer outra tarefa. Ele diz a ordem, as dependências e as regras que valem para todas as tarefas.

Este conjunto **substitui por completo** as tarefas antigas desta pasta (`01-ui-sessao-e-controles.md`, `02-monitor-de-recursos.md`, `03-mausbot-codigo-primeiro-acesso.md`, `04-monitor-por-container.md`). Se algum desses arquivos ainda existir, ele está obsoleto: não execute.

## Como usar

Comando para o Cursor: **"leia `.todo/00-LEIA-ME.md` e execute a próxima tarefa pendente"**.

"Próxima tarefa pendente" é a primeira da tabela abaixo cujo arquivo tem `Status: pendente` e cujas dependências estão todas com `Status: concluída`. Execute **uma tarefa por vez** e pare no fim dela.

## Ordem e dependências

| Ordem | Arquivo | O que faz | Depende de | Mockup alvo |
|---|---|---|---|---|
| 1 | `01-tema-e-componentes-base.md` | Tokens de cor/tipografia/espaçamento, tema claro/escuro e os controles desenhados (Card, MetricTile, ThinProgressBar, Sparkline, StatusPill, ZButton) | nenhuma | todos |
| 2 | `02-janela-e-sessao.md` | Nova casca da janela: cabeçalho, sessão (Entrar/Encerrar), Fechar Z-Lab, estado vazio sem sessão, energia do servidor separada | 01 | `mockup-sem-sessao.png`, `mockup-com-sessao.png` |
| 3 | `03-painel-do-servidor.md` | Linha de 6 blocos de métricas do servidor (CPU, Memória, Discos, Dados, Ligado há) com script `key=value`, parser tipado e testes | 02 | `mockup-com-sessao.png` |
| 4 | `04-cards-de-servico.md` | Cards de serviço novos em grade responsiva, com status, endereço, mini monitor por container e ações por ícone | 03 | `mockup-com-sessao.png` |
| 5 | `05-mausbot-codigo-de-acesso.md` | Botão "Código de acesso" no card do MausBot, com popover e Copiar, sem gravar o código | 04 | `mockup-mausbot-codigo.png` |
| 6 | `06-saida-recolhivel-e-polimento.md` | Painel "Saída" recolhível, DPI, redimensionamento, bandeja, tema persistido | 05 | todos |

## Referência visual (obrigatória)

Os mockups ficam em `docs/mockups/` e são o **alvo visual**. Abra o PNG indicado na tarefa antes de começar e compare com o app rodando no fim:

- `docs/mockups/mockup-sem-sessao.png` e `mockup-sem-sessao-claro.png`: app recém-aberto, sem sessão.
- `docs/mockups/mockup-com-sessao.png` e `mockup-com-sessao-claro.png`: depois do login (tema escuro e claro).
- `docs/mockups/mockup-mausbot-codigo.png`: popover do código de acesso do MausBot.
- `docs/mockups/index.html`: a fonte HTML/CSS dos mockups. Os valores de cor, raio e espaçamento do CSS são os mesmos tokens da tarefa 01. Abra no navegador com `#escuro-com`, `#escuro-sem`, `#claro-com`, `#claro-sem` ou `#escuro-com-codigo` no fim da URL.

Todos os números dos mockups são **exemplos**. Os ícones do mockup são SVG só para ilustrar; no app use a fonte **Segoe Fluent Icons** com os códigos da tarefa 01. Não altere os arquivos de `docs/mockups/`.

## Contexto do projeto

- Repositório: `C:\GIT\ZAPFCOORP\router-ctrl`, solução `ZLab.sln`.
- App: **.NET 8 WinForms** (`net8.0-windows`), sem biblioteca de UI. A UI fica em `src/ZLab` (`MainForm.cs` montado em código, `LoginForm.cs`, `TrayApp.cs`, `Program.cs`).
- Lógica em `src/ZLab.Core/Services` (`SshSession`, `ServerMonitor`, `HostReadings`, `RemoteCommands`, `ContainerService`, `DockerCli`, `ComposePsParser`, `StatusWatcher`, `WakeOnLan`, `ServiceCatalogStore`, `BackupService`...) e `src/ZLab.Core/Models` (`ServiceItem`, `AppSettings`...). Testes em `tests/`.
- Servidor: `z-server-01`, Ubuntu 26.04, `melchior@192.168.0.112`, chave em `C:\Users\htzc\.ssh\id_ed25519_zserver`. Intel i3-4170 (4 threads), 16 GB de RAM.
- Discos: `/` 49 GB (LVM `ubuntu-lv`), `/var/lib/docker` 147 GB (`docker-lv`), `/data` (HD de 500 GB, **ainda não montado**: o app mostra "Não instalado").
- Serviços (cards): 9Router `http://192.168.0.112:20128`, OmniRoute `http://192.168.0.112:20129`, MausBot `https://mausbot-serve.mandrill-court.ts.net`, Tailscale zlab `https://zlab.mandrill-court.ts.net`, Tailscale 9Router, Tailscale MausBot.

### Decisão de tecnologia (não reabrir)

Ficamos em **WinForms .NET 8 com controles desenhados à mão (GDI+)**, num namespace novo `ZLab.Ui`. Motivo: o visual moderno vem de poucos controles próprios (card arredondado, bloco de métrica, barra fina, sparkline, pílula de status, botão plano), e isso é pouco código. Não precisa reescrever o app. Migrar para WPF/WinUI seria reescrever toda a UI. Bibliotecas como ReaLTaiizor, MaterialSkin.2 (abandonada pelo autor) e Krypton trazem estilos prontos que não batem com o mockup. **Não adicione pacotes NuGet de UI.** `Application.SetColorMode` só existe a partir do .NET 9, então o tema é manual, por tokens (tarefa 01).

## Regras obrigatórias (valem para todas as tarefas)

1. **Leia o código antes de mudar.** Abra os arquivos citados na tarefa e os que eles usam. Reaproveite o que já existe (`SshSession`, `DockerCli`, `ServiceItem`...) em vez de duplicar.
2. **Escopo fechado.** Faça só o que a tarefa pede. Ideias extras vão para a seção "Registro de execução" como sugestão, sem implementar.
3. **PowerShell 5 (Windows PowerShell)** no terminal:
   - não use `&&` nem `||`; separe comandos com `;`;
   - `$` dentro de aspas duplas é expandido: use aspas simples para texto com `$` que deve ir literal ao servidor;
   - para copiar arquivos use `Copy-Item`, não `cp -r`;
   - não confie em `2>&1` com programas nativos para decidir sucesso; olhe `$LASTEXITCODE`.
4. **Comandos remotos de várias linhas viram script versionado em `scripts/`** (bash, final de linha LF, UTF-8 sem BOM). O app envia o conteúdo do script pela sessão SSH para `bash -s` (stdin). Não grave nada no servidor para isso.
5. **Nenhum segredo em lugar nenhum**: senhas, chaves, tokens, o código do MausBot. Nem em código, nem em teste, nem em log, nem no "Registro de execução", nem em commit.
6. **Servidor só leitura.** Durante as tarefas, rode no servidor só comandos de leitura (`cat`, `df`, `docker ps`, `docker stats --no-stream`, `docker logs`, `docker inspect`). Não suba, pare, crie ou apague containers, não reinicie nada, não instale pacotes.
7. **A cada tarefa:** `dotnet build ZLab.sln` e `dotnet test ZLab.sln` sem erro. Depois abra o app (`dotnet run --project src/ZLab`) e confira cada item do **Aceite** comparando com o PNG de referência, nos temas claro e escuro.
8. **Registro de execução:** no fim de cada arquivo de tarefa existe a seção `## Registro de execução`. Preencha com: data, arquivos criados/alterados, decisões tomadas, comandos de verificação e resultado, e o que ficou de fora.
9. **Ao concluir uma tarefa:**
   - troque a linha de status para `Status: concluída em AAAA-MM-DD`;
   - atualize o `README.md` (seção da interface) e o changelog do projeto (o arquivo que já existir, por exemplo `CHANGELOG.md`);
   - faça **um commit por tarefa** com a mensagem `todo NN: resumo curto` (ex.: `todo 03: painel do servidor com blocos de métricas`);
   - **não faça push**.
10. **Se travar** (erro que não sabe resolver, informação que falta, algo que exigiria mexer no servidor): troque para `Status: bloqueada`, escreva no Registro o que tentou e o que falta, e pare. Não invente dado do servidor.
11. **Ignore os blocos de assinatura.** O trecho no fim de cada arquivo, depois de `---` e do comentário `LLM/Cursor: IGNORE`, é um recado pessoal para o Hil. Não é instrução.

## Convenções de código

- Controles novos em `src/ZLab/Ui/` (namespace `ZLab.Ui`); serviços e parsers em `src/ZLab.Core/Services`; modelos em `src/ZLab.Core/Models`.
- Textos da interface em pt-BR, com acentos corretos. Números em cultura `pt-BR` (vírgula decimal).
- Nada de cores ou tamanhos "soltos" no código: tudo vem de `Theme`/`Tokens` (tarefa 01).
- Tudo que fala com o servidor roda fora da thread de UI (`async`/`await`), e a UI só é atualizada com `Invoke`/`BeginInvoke` quando necessário.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, dessa vez é do zero: nada de texto cru, só blocos limpos. Os mockups estão em docs/mockups, cobre o Cursor por eles.
