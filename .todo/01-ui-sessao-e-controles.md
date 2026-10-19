# 01 — Tela inicial, sessão e controles do servidor

**Status:** pendente. Não depende de outra tarefa.

## Objetivo
Reorganizar a área de sessão (quadro vermelho do print): ao abrir só existe o login; o resto aparece apenas com sessão ativa. Separar controle de energia do servidor do controle de sessão.

## Comportamento esperado
1. **App aberto, sem sessão:** visível só o grupo **Sessão** com os botões **Entrar** (ativo) e **Encerrar sessão** (desativado), mais **Fechar Z-Lab**. Monitor de recursos, cards de serviços e controles de energia ficam **ocultos** (Visible = false, não só desativados). Texto do status: "Sem sessão".
2. **Entrar:** abre o login/conecta por SSH (fluxo atual do LoginForm/SshSession). Em caso de sucesso: Entrar fica **desativado**, Encerrar sessão **ativo**, e todo o resto aparece. Texto: "Sessão em melchior@192.168.0.112". Em falha: mostra o erro e continua tudo oculto.
3. **Encerrar sessão:** fecha a sessão SSH, para os pollings/watchers ligados à sessão (ServerMonitor, StatusWatcher) e volta ao estado 1 (tudo oculto).
4. **Grupo separado "Servidor"** (só visível com sessão): **Ligar** (Wake-on-LAN), **Desligar**, **Reiniciar**. Desligar e Reiniciar pedem confirmação. Ligar fica ativo só quando o servidor não responde.
   - Observação: Ligar precisa funcionar também sem sessão quando o servidor está desligado. Se for mais simples, deixe **Ligar** visível no estado 1 também, ao lado de Entrar. Decida pelo que o código permite e registre a decisão.
5. **Botão Sair** vira **Fechar Z-Lab**: encerra a sessão (se houver), para timers/agendadores e fecha o aplicativo inteiro, inclusive o ícone da bandeja (TrayApp). Confirmar antes se houver backup em andamento.

## Passos
1. Ler `src/ZLab/MainForm.cs`, `LoginForm.cs`, `TrayApp.cs` e `src/ZLab.Core/Services/SshSession.cs`.
2. Criar um método único de estado, por exemplo `ApplySessionState(bool connected)`, que mostra/oculta e ativa/desativa tudo; chamá-lo no load, após login, ao encerrar sessão e em queda de conexão.
3. Mover Ligar/Desligar/Reiniciar para um GroupBox "Servidor" separado do GroupBox "Sessão".
4. Renomear Sair para "Fechar Z-Lab" com o comportamento do item 5.
5. Se a conexão SSH cair sozinha, voltar ao estado 1 e avisar.

## Aceite
- Abrir o app: só Sessão (Entrar, Encerrar sessão desativado, Fechar Z-Lab) e talvez Ligar.
- Entrar com sucesso: Entrar desativado, resto visível.
- Encerrar sessão: tudo oculto de novo, sem erros no log.
- Fechar Z-Lab: processo some do Gerenciador de Tarefas e da bandeja.
- `dotnet build` sem erros.

## Depois
README (seção de uso) e changelog.

## Revisão UI/UX

Quem opera é o Hil, no Windows, com NetExtender em túnel dividido e sem o aplicativo Tailscale. O painel é o z-server-01 (`192.168.0.112`). Backup diário não existe nesta versão: nenhum texto da tela fala de backup.

### Decisão

**Liga fica visível no estado 1.** O texto do botão permanece **Liga** (é o "Ligar" desta tarefa).

Sem sessão não há SSH. Com o z-server-01 desligado, Entrar não conecta, e o diálogo que já existe manda usar Liga. Liga não pede senha nem sessão; hoje ele só habilita fora da sessão. Esconder Liga até o login deixaria o Hil sem como acordar a máquina.

Desligar e Reiniciar ficam ocultos no estado 1. Os dois só existem com SSH. Mostrar o grupo Servidor cheio de botão apagado foge da tela limpa.

O grupo **Servidor** fica na tela nos dois estados, ao lado do grupo **Sessão**, para Liga não mudar de lugar quando o login funciona. No estado 1 esse grupo mostra somente Liga. Desligar e Reiniciar usam `Visible = false` até haver sessão; com sessão, entram à direita de Liga.

A janela não muda de tamanho entre os estados. O vão abaixo dos dois grupos é o estado sem sessão.

### Sem sessão

Faixa, texto exato: `Sem sessão`.

Grupo **Sessão**, nesta ordem:

- **Entrar** — visível e ativo
- **Encerrar sessão** — visível e desativado
- **Fechar Z-Lab** — visível e ativo

Grupo **Servidor**: **Liga** visível e ativo, salvo na espera do pacote mágico.

Ocultos com `Visible = false`: rótulos e campos de Tempo no ar, Carga, Memória, Disco /, Disco Docker e Disco /data; os seis cards; a caixa Saída.

Entrar com a porta 22 fechada, ou com usuário/senha recusados, mostra o erro de hoje e a tela continua neste estado.

Na espera do Liga, a faixa passa a `Pacote enviado. Aguardando a porta 22 (até 2 minutos)…`. Liga e Entrar ficam desativados. Leituras, cards, Saída, Desligar e Reiniciar continuam ocultos. Se a porta abrir, segue o login de hoje. Cancelar o login, ou errar a senha, devolve este estado, com Liga ativo de novo.

O X da janela esconde o painel e mantém o ícone na bandeja. Sem diálogo. Isso não é Fechar Z-Lab.

### Com sessão

Faixa: `Sessão em ` + o usuário digitado no login + `@192.168.0.112`. Se o login for `melchior`, o Hil lê `Sessão em melchior@192.168.0.112`. O nome vem da sessão, não de um texto fixo.

Grupo **Sessão**: Entrar desativado; Encerrar sessão ativo; Fechar Z-Lab ativo.

Grupo **Servidor**: Liga visível e desativado. Desligar e Reiniciar visíveis e ativos. Cada um confirma antes, com padrão Não, nos textos que já estão em `RemoteCommands`.

Leituras, os seis cards e a Saída ficam visíveis.

Encerrar sessão não pede confirmação. A tela volta ao estado sem sessão, sem MessageBox.

Enquanto um comando remoto está em andamento, Desligar, Reiniciar, Encerrar sessão e os verbos dos cards ficam desativados, como hoje. Fechar Z-Lab continua ativo.

Liga habilita quando não há sessão SSH, não está na espera do pacote e não há comando em andamento. A tela não sonda a porta 22 em segundo plano só para acinzentar Liga. Se a máquina já está ligada e o Hil ainda não entrou, Liga pode ser clicado; o fluxo atual (pacote, espera da porta, login) permanece.

### Queda de SSH

A sessão cai sozinha, sem clique em Encerrar sessão: a tela volta ao estado sem sessão e um MessageBox avisa. A Saída está oculta nesse estado; uma linha no log o Hil não vê.

Texto: `A sessão SSH caiu. O painel voltou para Sem sessão.`
Título: `Z-Lab`.

Liga e Entrar voltam a ficar ativos. O texto não cita backup, Tailscale, NetExtender nem a causa da rede.

### Fechar Z-Lab

O botão **Sair** do painel e o item **Sair** da bandeja passam a **Fechar Z-Lab**. A confirmação aparece sempre, com ou sem sessão, padrão Não.

Texto: `Fechar o Z-Lab? Os containers no z-server-01 continuam em execução.`
Título: `Z-Lab — Fechar`.

O diálogo não fala de backup e não depende de backup em andamento.

Sim: encerra a sessão se houver uma, para o watcher, esconde o ícone da bandeja e encerra o processo. O Z-Lab some do Gerenciador de Tarefas.

O botão Sair de hoje tem 80 px. **Fechar Z-Lab** precisa aparecer por inteiro.

### O que conflita com a tela de hoje

- Uma fila só, sem GroupBox: Liga, Desligar, Reiniciar, Entrar, Encerrar sessão e Sair ficam sempre visíveis. Só `Enabled` muda.
- Sem sessão a faixa diz `Sem sessão. Com a máquina desligada, só Liga responde.`
- Entrar é variável local. `ApplyButtons` não o desativa. Com sessão, o clique em Entrar não muda nada na tela.
- Leituras, cards (rótulo "Sem sessão") e Saída continuam na janela, com botões desativados, em vez de `Visible = false`.
- O botão, o título `Z-Lab — Sair` e o item da bandeja ainda dizem Sair. A frase da confirmação já fala dos containers e já não fala de backup; o verbo é que troca para Fechar.
- Queda de SSH só troca a faixa e desativa botões. O painel não se esconde. Falha de autenticação vai para `Note` no log, que no estado sem sessão fica oculto.
- `.docs/ui-ux.md` ainda desenha essa faixa única e, sem sessão, deixa o miolo na tela com os verbos desativados. A implementação desta tarefa segue esta revisão.

## Notas do executor

- Na bandeja, trocar só **Sair** por **Fechar Z-Lab**, com o mesmo texto e o mesmo título do painel. Subir, Parar e Encerrar sessão do menu ficam como estão.
- O botão continua com o texto **Liga**. Diálogos e bandeja já usam essa palavra.
- A frase da tarefa sobre confirmar a saída "se houver backup em andamento" não entra no código. Backup diário não roda neste painel.
- `.docs/ui-ux.md` fica para outra hora. README e changelog continuam na seção **Depois**, quando a tarefa for concluída.
- Se a porta 22 não abrir em 2 minutos, permanece o aviso de Liga que já existe (tarefa 08 da BIOS).

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Tela limpa primeiro, Hil: entrou, aparece; saiu, some.
