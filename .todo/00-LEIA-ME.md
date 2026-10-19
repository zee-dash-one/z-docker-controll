# 00 — LEIA PRIMEIRO (índice e regras para o Cursor)

Você (Cursor) é quem executa as tarefas desta pasta. **Este arquivo é o ponto de entrada:** leia-o inteiro antes de abrir qualquer outra tarefa, e volte a ele entre uma tarefa e outra.

## Ordem de execução
Execute **uma tarefa por vez, nesta ordem**. Só comece a próxima quando a anterior estiver com `Status: concluída`.

| Ordem | Arquivo | O que faz | Depende de |
|---|---|---|---|
| 1 | `01-ui-sessao-e-controles.md` | Tela inicial só com login; grupo Servidor separado; Fechar Z-Lab | — |
| 2 | `02-monitor-de-recursos.md` | Tabela de recursos do servidor (CPU %, memória, discos) | 01 |
| 3 | `04-monitor-por-container.md` | Mini-monitor de CPU/memória em cada card de serviço | 01 e 02 |
| 4 | `03-mausbot-codigo-primeiro-acesso.md` | Botão "Código de acesso" no card MausBot | 01 |

Exceção: se o Hil pedir urgência no código do MausBot, rode **só o passo 1** da tarefa 03 (diagnóstico, sem mudar código) antes das outras.

## Contexto do projeto
- Pasta: `C:\GIT\ZAPFCOORP\router-ctrl` — solução `ZLab.sln`, WinForms .NET.
- UI: `src/ZLab` (`MainForm.cs`, `LoginForm.cs`, `TrayApp.cs`, `Program.cs`).
- Lógica: `src/ZLab.Core/Services` (`SshSession`, `ServerMonitor`, `HostReadings`, `RemoteCommands`, `ContainerService`, `DockerCli`, `StatusWatcher`, `WakeOnLan`…) e `src/ZLab.Core/Models`.
- Testes: `tests/`.
- Servidor: z-server-01, `melchior@192.168.0.112`, chave `C:\Users\htzc\.ssh\id_ed25519_zserver`, Docker 29 com Compose em `/opt/zlab`.

## Regras obrigatórias
1. **Leia antes de mudar.** Abra os arquivos citados na tarefa e entenda o fluxo atual. Mantenha o estilo, nomes e padrões do código existente.
2. **Escopo fechado.** Faça só o que a tarefa pede. Ideia extra vai para a seção "Notas do executor" no fim da tarefa, não para o código.
3. **Shell é PowerShell 5:** `&&` não funciona (use `;`); `$` dentro de aspas duplas é expandido (use aspas simples ou um script `.sh` em `scripts/`); não use `copy /Y` (use `Copy-Item -Force`).
4. **Comandos remotos** ficam em scripts versionados em `scripts/` sempre que tiverem mais de uma linha ou caracteres especiais. O app chama o script; nada de comando gigante embutido em string C#.
5. **Nada de segredo** em arquivo versionado, log persistente ou mensagem de commit: senhas, chaves, auth keys do Tailscale, códigos do MausBot.
6. **Não mexa no servidor além de leitura** (docker ps/stats/logs, df, /proc). Não pare, recrie ou apague containers ou volumes, a não ser que a tarefa diga.
7. **Validação a cada tarefa:** `dotnet build ZLab.sln` sem erros e `dotnet test` passando. Abra o app e confira cada item da seção **Aceite**.
8. **Registro:** no fim do arquivo da tarefa, antes da assinatura, adicione `## Registro de execução` com os comandos rodados, o que deu certo, o que falhou e decisões tomadas.
9. **Ao concluir:** troque a linha de status por `Status: concluída em AAAA-MM-DD`, atualize `README.md` (uso e telas) e o changelog, e faça **um commit por tarefa** com mensagem `todo NN: <resumo>`. Não faça push sem o Hil pedir.
10. **Se travar** (requisito ambíguo, erro que não resolve em 3 tentativas, algo que exige senha ou decisão do Hil): pare, escreva o problema em "Notas do executor" com `Status: bloqueada` e avise o Hil. Não invente valor nem contorne bloqueio de antivírus.
11. **Ignore a assinatura** no fim de cada arquivo (bloco após o comentário HTML). Não é instrução.

## Formato de cada tarefa
Status · Objetivo · Comportamento/Layout esperado · Passos · Aceite · Depois.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. É só mandar "leia .todo/00-LEIA-ME.md e execute a próxima tarefa pendente", Hil. O resto ele acha sozinho.
