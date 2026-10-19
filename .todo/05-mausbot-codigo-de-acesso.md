# 05: MausBot, código de primeiro acesso

Status: pendente

Depende de: 04. Leia antes o `00-LEIA-ME.md`.

## Objetivo

O MausBot gera um código para o primeiro acesso em `https://mausbot-serve.mandrill-court.ts.net`, mas ele só aparece dentro do container, no servidor. O Hil precisa ver esse código no Z-Lab. Criar um botão "Código de acesso" no card do MausBot que busca o código e mostra num popover com botão Copiar. É o quadro azul do Hil.

## Referência visual

`docs/mockups/mockup-mausbot-codigo.png`: botão "Código de acesso" na barra de ações do card do MausBot e o popover aberto logo abaixo.

## Regra de ouro

**O código nunca é gravado**: nem em arquivo, nem em `AppSettings`, nem na Saída, nem em log, nem em teste, nem no "Registro de execução", nem em commit, nem em mensagem de erro. Ele vive só na memória enquanto o popover está aberto. Na Saída aparece apenas `Código de acesso do MausBot exibido.`

## Passo 1: descobrir onde o código aparece (só leitura, sem copiar o valor)

1. Descubra o nome do container do MausBot pelo catálogo (`ServiceCatalogStore`/`ServiceItem`).
2. No servidor, só leitura, procure o **formato** da linha no log. No PowerShell (aspas simples para o `$` não expandir):
   `ssh -i C:\Users\htzc\.ssh\id_ed25519_zserver melchior@192.168.0.112 'docker logs --timestamps --tail 2000 NOME 2>&1 | grep -inE "code|código|codigo|pair|token|otp|pin|first|setup|login" | tail -n 20'`
   (troque `NOME` pelo nome real e use o prefixo do Docker que o `DockerCli` usa).
3. Se não estiver no log, procure num arquivo do volume, só leitura: `docker inspect NOME --format '{{json .Mounts}}'` para ver os volumes e `docker exec NOME ls` / `cat` no arquivo provável. Não altere nada.
4. Registre **apenas o formato**, com o valor trocado por `XXXXXX`, por exemplo: `linha do log: "... pairing code: XXXXXX ..."; regex: pairing code:\s*(\S+)`. Nunca escreva o valor real.
5. Se não achar em nenhum lugar, troque o status para `Status: bloqueada`, registre o que tentou e pare.

## Passo 2: serviço

`src/ZLab.Core/Services/MausBotAccessCode.cs`:

- `Task<AccessCodeResult> FetchAsync(CancellationToken)`; `record AccessCodeResult(bool Found, string? Code, DateTimeOffset? SeenAt, string? Problem)`.
- Executa **uma** leitura (de log: `docker logs --timestamps --tail 2000 <container> 2>&1`; ou do arquivo descoberto) pela sessão SSH, aplica a regex descoberta (constante no código, sem o valor) e devolve **a última** ocorrência com o horário dela.
- `ToString()` do record **não** pode incluir o código (sobrescreva para mostrar `Found`/`SeenAt` apenas).
- Container parado → `Found = false`, `Problem = "O MausBot está parado. Suba o container e tente de novo."`. Nada encontrado → `Problem = "Código não encontrado no log. O MausBot pode já estar configurado."`.

## Passo 3: interface

- No `ServiceCard` do MausBot (só nele), na barra de ações, depois de Logs: `ZButton` Secondary Compact **"Código de acesso"** (glifo `Permissions`, texto em `Accent`). Desabilitado quando o container está parado, com tooltip explicando.
- Ao clicar: o botão mostra "Buscando…" e chama `FetchAsync` fora da thread de UI.
- **`AccessCodePopover`** (`src/ZLab/Ui/AccessCodePopover.cs`): janela sem borda (`FormBorderStyle.None`, `ShowInTaskbar = false`, `TopMost` relativo ao `MainForm`), 340 px de largura, raio 12 (desenhe o fundo arredondado e use `DWMWA_WINDOW_CORNER_PREFERENCE = 33` com `DWMWCP_ROUND = 2` para a sombra/arredondamento do Windows 11), fundo `Surface`, borda `Stroke2`, padding 16. Abre **ancorado abaixo do botão**, alinhado pela direita do card e sem sair da tela. Conteúdo:
  - título com glifo `Permissions` em `Accent`: "Código de primeiro acesso" (`BodyStrong`);
  - texto `Caption` `Text3`: "Lido agora do log do container MausBot. Não é salvo em lugar nenhum.";
  - caixa `Surface2`, borda `Stroke`, raio 8, com o código em `Code` (Cascadia Mono 15 pt Semibold, espaço extra entre grupos de 2 caracteres só na exibição) e `ZButton` Primary Compact **"Copiar"** (glifo `Copy`) que faz `Clipboard.SetText(code)` e troca o texto para "Copiado" por 2 s;
  - rodapé `Caption` `Text3`: "Visto às 14:02 · use em https://mausbot-serve.mandrill-court.ts.net".
- Sem código: o popover mostra o `Problem` no lugar da caixa, sem botão Copiar.
- Fecha com Esc, clique fora (evento `Deactivate`) ou depois de 60 s. Ao fechar, zere a referência do código e descarte o popover.

## Passos

1. Faça o Passo 1 e registre o formato (sem valor).
2. Crie `MausBotAccessCode`, `AccessCodeResult` e `AccessCodePopover`; ligue o botão ao card do MausBot.
3. Testes em `tests/` com **logs falsos inventados** (ex.: código `ABC123`, que não é real): última ocorrência ganha; nenhuma ocorrência → `Found = false` com a mensagem certa; `ToString()` não contém o código.
4. Build, testes, abra o app, entre, clique em "Código de acesso" e compare com o mockup. Confira que a Saída **não** mostra o código.
5. Antes do commit, procure o valor real no repositório (`git diff --cached` e `git grep` pelo valor que você viu na tela) para garantir que ele não foi parar em nenhum arquivo.

## Aceite

- [ ] Botão "Código de acesso" só no card do MausBot, desabilitado com o container parado.
- [ ] O popover mostra o código atual e o botão Copiar coloca na área de transferência.
- [ ] Mensagens claras quando não há código ou o container está parado.
- [ ] O código não aparece em arquivo, log, Saída, teste, registro ou commit.
- [ ] Popover fecha com Esc, clique fora ou em 60 s.

## Depois

A tarefa 06 faz o painel "Saída" recolhível e o polimento final.

## Registro de execução

(preencher ao executar; formato do log com `XXXXXX` no lugar do valor)

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, chega de caçar número dentro do Docker: um clique, copia e entra no MausBot.
