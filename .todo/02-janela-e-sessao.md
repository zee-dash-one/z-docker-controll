# 02: Casca da janela e sessão

Status: concluída em 2026-10-19

Depende de: 01. Leia antes o `00-LEIA-ME.md`.

## Objetivo

Trocar a linha de botões soltos do topo por uma janela organizada: cabeçalho com marca, estado da sessão e os controles de sessão; energia do servidor numa área própria; e um estado vazio limpo quando não há sessão. É o requisito do quadro vermelho do Hil.

## Referência visual

- `docs/mockups/mockup-sem-sessao.png` (e `-claro`): app recém-aberto.
- `docs/mockups/mockup-com-sessao.png` (e `-claro`): depois do login. Nesta tarefa a área de blocos e de cards pode ficar com um marcador simples ("Servidor" e "Serviços" com os títulos de seção); o conteúdo deles vem nas tarefas 03 e 04.

## Layout da janela

Tamanho inicial 1280 x 800, mínimo 1024 x 700, centralizada. `BackColor = Bg`. Estrutura (de cima para baixo, por `Dock`):

1. **`HeaderBar`** (novo, `src/ZLab/Ui/HeaderBar.cs`), `Dock = Top`, altura 60, fundo `Surface`, linha de 1 px `Stroke` embaixo, padding lateral 24. Da esquerda para a direita:
   - marca: quadrado 32 x 32 raio 8 com gradiente `Accent` → `#8B5CF6` e "Z" branco; ao lado "Z-Lab" (`Title`) e, embaixo, "Painel do z-server-01" (`Caption`, `Text3`);
   - **chip de sessão** (altura 28, pílula, fundo `Surface2`, borda `Stroke`): ponto verde + `melchior@192.168.2.37` + "· Sessão aberta" quando conectado; ponto `Muted` + "Sem sessão" quando não;
   - espaço flexível;
   - **grupo Sessão**: `ZButton` "Entrar" (glifo `Unlock`) e `ZButton` "Encerrar sessão" (glifo `Lock`), gap 8;
   - divisória vertical 1 x 28 `Stroke`;
   - `ZButton` ícone para alternar tema (sol/lua, tooltip "Tema claro/escuro");
   - `ZButton` Ghost "Fechar Z-Lab" (glifo `Cancel`, tooltip "Fecha o app inteiro, inclusive o ícone da bandeja").
2. **Painel "Saída"**, `Dock = Bottom`. Nesta tarefa basta mover o log atual para um painel de 40 px de altura com fundo `Surface` e linha `Stroke` em cima, mostrando "Saída" e a última linha. O recolher/expandir é da tarefa 06.
3. **Área de conteúdo**, `Dock = Fill`, `AutoScroll = true`, padding 24/20/24/0. Dois filhos que se alternam:
   - **`EmptyStatePanel`** (sem sessão), centralizado na área: `RoundedPanel` de 520 de largura, raio 16, padding 40. Ícone 64 x 64 raio 16 com fundo `AccentSoft` e glifo `Network` em `Accent`; título "Nenhuma sessão aberta" (16,5 pt Semibold); texto "Clique em **Entrar** (no alto, à direita) para conectar ao z-server-01. O painel do servidor e os serviços aparecem depois do login." (`Body`, `Text2`). Divisória e, embaixo, ponto + "z-server-01 não responde" (ou "z-server-01 responde") + `ZButton` Compact "Ligar servidor" (glifo `Power`, Wake-on-LAN). O botão "Ligar servidor" só aparece quando o servidor não responde.
   - **`DashboardPanel`** (com sessão): coluna com a seção "Servidor" e a seção "Serviços".
4. **Cabeçalho da seção "Servidor"** (dentro do `DashboardPanel`): "Servidor" (`Subtitle`) + legenda `Text3` "z-server-01 · Ubuntu 26.04 · Intel i3-4170, 4 núcleos" (use o que o app já lê; se não lê, deixe só o nome do host) e, à direita, o **grupo Energia**: `ZButton` Compact "Ligar" (desabilitado quando o servidor está no ar), "Reiniciar" (glifo `Refresh`) e Danger "Desligar" (glifo `Power`). Energia fica **aqui**, separada da sessão no cabeçalho.

## Comportamento

| Situação | Entrar | Encerrar sessão | Fechar Z-Lab | Conteúdo | Energia |
|---|---|---|---|---|---|
| App abriu, sem sessão | visível, habilitado, **Primary** | visível, **desabilitado** | visível | `EmptyStatePanel` | oculta (só "Ligar servidor" no estado vazio, se o servidor não responder) |
| Conectando | desabilitado, texto "Entrando…" | desabilitado | visível | `EmptyStatePanel` | oculta |
| Sessão aberta | visível, **desabilitado**, Secondary | habilitado | visível | `DashboardPanel` | visível |
| Encerrar sessão clicado | volta ao primeiro estado | | | | |

- **Entrar** abre o `LoginForm` atual (reaproveite) e conecta com `SshSession`. Se falhar, mostra o erro na Saída e volta ao estado sem sessão.
- **Encerrar sessão** fecha o SSH, **para todos os pollers/timers** (`StatusWatcher` e os da tarefa 03 e 04), limpa os dados exibidos e volta ao estado vazio.
- **Ligar / Reiniciar / Desligar** pedem confirmação com `MessageBox` antes ("Desligar o z-server-01 agora? Os serviços vão parar."). Reaproveite os comandos que já existem. Após Desligar ou Reiniciar, a sessão é encerrada como no botão Encerrar sessão.
- **Fechar Z-Lab** (era "Sair") encerra a sessão se houver, remove o ícone da bandeja (`NotifyIcon.Visible = false; Dispose()`) e chama `Application.Exit()`. Verifique em `TrayApp.cs` como o app termina hoje e garanta que não sobra processo.
- O botão de tema alterna `ThemeManager.Apply(Dark/Light)` e repinta tudo. A persistência da escolha é da tarefa 06.
- Remova da UI os botões antigos do topo ("Liga", "Desligar", "Reiniciar", "Entrar", "Encerrar sessão", "Sair") e o `GroupBox` que não for mais usado. Não apague lógica de `ZLab.Core`.

## Passos

1. Leia `MainForm.cs`, `LoginForm.cs`, `TrayApp.cs`, `Program.cs`, `SshSession.cs`, `StatusWatcher.cs`, `WakeOnLan.cs`.
2. Crie `src/ZLab/Ui/HeaderBar.cs`, `src/ZLab/Ui/EmptyStatePanel.cs`, `src/ZLab/Ui/SectionHeader.cs` (título + legenda + área de ações à direita, reaproveitado nas tarefas 03 e 04).
3. Centralize o estado numa enum `SessionState { Disconnected, Connecting, Connected }` no `MainForm`, com um único método `ApplySessionState(SessionState)` que liga e desliga tudo pela tabela acima. Nada de `Visible`/`Enabled` espalhado pelo código.
4. Reorganize o `MainForm` com a estrutura de `Dock` acima. Mantenha o comportamento de minimizar para a bandeja que já existir.
5. Testes: se a lógica de habilitação estiver numa classe pura (recomendado: `SessionUiRules` com um método que devolve um record de flags para cada estado), teste os 3 estados.
6. Build, testes, abra o app e compare com os dois mockups nos dois temas.

## Aceite

- [ ] Ao abrir: só cabeçalho, estado vazio e Saída. Entrar (azul) habilitado; Encerrar sessão visível e desabilitado; Fechar Z-Lab visível. Nada de servidor nem de serviços na tela. Igual a `mockup-sem-sessao.png`.
- [ ] Depois de entrar: Entrar desabilitado, Encerrar sessão habilitado, chip mostra `melchior@192.168.2.37 · Sessão aberta`, aparecem as seções Servidor (com Ligar/Reiniciar/Desligar à direita) e Serviços.
- [ ] Encerrar sessão: fecha o SSH, para os timers e esconde tudo de novo.
- [ ] Ligar, Reiniciar e Desligar pedem confirmação e ficam fora do cabeçalho.
- [ ] Fechar Z-Lab fecha o app inteiro e o ícone some da bandeja.
- [ ] Os botões antigos do topo não existem mais.

## Depois

A tarefa 03 preenche a seção "Servidor" com os blocos de métricas.

## Registro de execução

- Data: 2026-10-19
- Criados: `HeaderBar`, `EmptyStatePanel`, `DashboardPanel`, `SectionHeader` (já existia, mantido), `SessionUiRules` + `SessionState` em `ZLab.Core`.
- Alterados: `MainForm.cs` (casca Dock: Header / conteúdo / Saída 40 px), `TrayApp.cs` (Connecting, Fechar Z-Lab, dispose do ícone).
- Removidos da UI: botões soltos Liga/Desligar/Reiniciar/Entrar/Encerrar/Sair e GroupBoxes de serviço (placeholder até 03/04).
- Testes: `SessionUiRulesTests` (3 estados + busy). `dotnet test` → 124 ok (WSL).
- Fora: comparação visual manual com os mockups no Desktop Runtime do Hil; persistência de tema fica na 06.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, é o teu quadro vermelho: abriu, só Entrar; entrou, aparece tudo; Fechar Z-Lab fecha de verdade.
