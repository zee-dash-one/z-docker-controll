# 03 — Mostrar o código de primeiro acesso do MausBot

**Status:** pendente. Depende da 01. Na ordem do 00-LEIA-ME, roda por último (passo 4).

## Objetivo
No primeiro acesso a `https://mausbot-serve.mandrill-court.ts.net`, o MausBot pede um número/código que ele gera e escreve dentro do container no servidor. O Hil precisa ver esse código no Z-Lab sem abrir o servidor.

## Passos
1. **Descobrir onde o código aparece** (rodar no PC):
   ```powershell
   ssh -i C:\Users\htzc\.ssh\id_ed25519_zserver melchior@192.168.0.112 "docker ps --format '{{.Names}}' | grep -i maus"
   ssh -i C:\Users\htzc\.ssh\id_ed25519_zserver melchior@192.168.0.112 "docker logs --tail 300 mausbot 2>&1 | grep -inE 'code|codigo|pin|token|pair|setup|password|otp'"
   ```
   Use o nome real do container que o primeiro comando mostrar. Se não estiver no log, procurar na documentação do MausBot e no volume de dados (ex.: `docker exec mausbot ls /data`) por arquivo de setup/pairing. **Registre aqui onde o código fica (log ou arquivo) e o padrão da linha, sem copiar o valor.**
2. No card **MausBot**, adicionar o botão **Código de acesso**. Ao clicar: roda o comando descoberto no passo 1 via SshSession, extrai o código com regex e mostra numa caixa com botão **Copiar**. Se não achar: "Código não encontrado. Ele só aparece no primeiro acesso; veja os Logs do MausBot."
3. Garantir que o botão **Logs** do card MausBot mostra as últimas ~200 linhas do `docker logs` do container (stdout e stderr), para servir de plano B.
4. Não salvar o código em arquivo, configuração ou log do Z-Lab.

## Aceite
- Clicar em Código de acesso mostra o código atual e copia para a área de transferência.
- Com esse código o Hil completa o primeiro login no endereço do MausBot.
- Nenhum arquivo do repo ou log do Z-Lab contém o código.

## Depois
README (como fazer o primeiro acesso ao MausBot) e changelog.

## Revisão UI/UX

Quem opera é o Hil, no primeiro acesso a `https://mausbot-serve.mandrill-court.ts.net`. O aplicativo Tailscale não está neste PC (NetExtender). O card **MausBot** só existe com sessão, como a revisão da 01. O endereço https continua o rótulo do card, sem **Abrir**, como a revisão da 04. O bloco de CPU e memória continua entre o endereço e os botões. O código aparece numa caixa, no clique, e sai da tela quando a caixa fecha.

### O que o Hil vê

Com sessão, o card MausBot, de cima para baixo: título, status, endereço, bloco do container, botões. A janela permanece no tamanho da revisão da 01. A faixa dos cards já rola. Os outros cinco cards ficam na altura da revisão da 04. O MausBot ganha uma fileira e fica mais alto. Subir e Parar desse card ficam no lugar quando o container para, porque a fileira nova está sempre lá.

**MausBot no ar:**

| | |
|---|---|
| título | MausBot |
| status | `No ar` |
| endereço | `https://mausbot-serve.mandrill-court.ts.net` |
| estado | `No ar há 3 h 12 min · reinícios: 0` |
| CPU | `0,4 %` e barra |
| memória | `85 MB de 15 GB` e barra |
| botões | Subir, Parar, Criar |
| | Atualizar, Logs, Uso |
| | Código de acesso |

**MausBot parado:** o bloco volta à linha `Parado` em cinza. **Código de acesso** continua na mesma fileira, no mesmo lugar.

O card Tailscale MausBot não ganha esse botão. Os dois mostram o mesmo endereço https. O botão está no card cujo título é MausBot.

Sem sessão o card inteiro some, botão incluído. A caixa não fica aberta.

### Decisão

**Onde fica o botão.** Na terceira fileira, só no card MausBot, abaixo da fileira do Logs. Esquerda 12, a mesma de Subir e Atualizar. Largura 248, da esquerda de Atualizar até a direita de Uso, para **Código de acesso** aparecer por inteiro no Segoe UI 9, centralizado. Altura 28. O intervalo até a fileira do Logs é o mesmo que já separa Subir de Logs.

O bloco de CPU permanece acima das três fileiras. **Código de acesso** fica na fileira de baixo da do Logs, fora do bloco de medida.

**Habilitação.** A mesma de Logs: ativo com sessão e container no ar ou parado. Ausente, Ocupado e Erro deixam o botão desativado. Com um comando remoto em andamento, desativado junto com os outros verbos do card. Durante a leitura, os verbos ficam desativados como já ficam no Logs. O rótulo do botão não muda para "lendo". A caixa abre quando a leitura volta.

**A caixa, quando acha o código.** Modal, no centro do painel. Título: `Z-Lab — Código de acesso`.

Frase, Segoe UI 9: `Cole este código no primeiro acesso do MausBot.`

Abaixo, uma linha, somente leitura, Consolas 14, com o valor como a leitura devolveu, sem espaço acrescentado e sem rótulo de pin, token ou senha. O texto nasce selecionado, para Ctrl+C também servir. Se o valor for longo, a linha rola na horizontal. A caixa não cresce além do painel.

Botões: **Copiar** e **Fechar**. **Copiar** é o padrão (Enter). Esc fecha.

**Copiar.** Copia só o valor, para a área de transferência. O botão passa a ler `Copiado`. A caixa continua aberta, para o Hil conferir o valor antes de colar na página que ele já abriu. Outro clique em `Copiado` copia de novo. Fechar deixa a área de transferência como está: o que ele já copiou continua disponível para colar no navegador. A caixa não abre o endereço.

**A caixa, quando não acha.** O mesmo título, a mesma janela, sem a linha do valor e sem **Copiar**. Uma frase, esta: `Código não encontrado. Ele só aparece no primeiro acesso; veja os Logs do MausBot.` Só **Fechar**, padrão. Enter e Esc fecham. Valor vazio ou só com espaço é esta cara.

**Falha da leitura** (a sessão não devolveu resposta). MessageBox no padrão de erro do painel. Título `Z-Lab`. Texto: `Não foi possível ler o código de acesso. Veja os Logs do MausBot.` A saída do comando não entra nesse texto.

**Logs.** No card MausBot, Logs mostra na Saída as últimas 200 linhas do container, stdout e stderr juntos, na ordem em que o comando imprime. Os outros cinco cards continuam com as 80 linhas de hoje. A frase de não encontrado manda o Hil fechar a caixa e clicar em Logs.

**Saída.** O clique em Código de acesso não escreve na Saída. O valor, a frase de não encontrado, a frase de falha e o texto cru da leitura ficam fora dela. A Saída muda quando o Hil clica em Logs, ou num verbo que já escreve lá hoje.

**Sem sessão.** O card está oculto (`Visible = false`), como a revisão da 01. O botão não aparece desativado no vão. Se a caixa estiver aberta e a sessão cair, ou o Hil encerrar a sessão, a caixa fecha e o valor sai da tela, antes do aviso. O aviso de queda da revisão da 01 não cita o código. Fechar a caixa, encerrar a sessão e a queda de SSH não limpam a área de transferência.

**Cada clique lê de novo**, pela sessão SSH que já está aberta. A caixa não deixa o valor escrito no card. Fechar apaga o campo.

### O que conflita com o card de hoje

- O card MausBot tem duas fileiras: Subir, Parar, Criar em y=88 e Atualizar, Logs, Uso em y=124. Cada botão tem 80×28. Não há terceira fileira. O vão do Abrir (x=264) só existe com URL lan; no MausBot ele não existe. **Código de acesso** não cabe em 80 px; a fileira nova tem 248 px.
- A revisão da 04 deixa os seis cards na mesma altura. Este botão existe só no MausBot, então só esse card cresce uma fileira. Os outros cinco permanecem na altura da 04.
- Logs, Uso e os verbos de mudança passam pela Saída (`Note` / `ShowOutputAsync`). Código de acesso não usa esse caminho.
- Logs hoje pede `logs --tail 80` nos seis serviços. No card MausBot passam a ser 200 linhas, stdout e stderr.
- A bandeja tem Subir e Parar por serviço. Código de acesso fica no painel: a caixa precisa do campo e de **Copiar**.
- `.docs/ui-ux.md` ainda descreve o card sem este botão.

### Notas do executor

- Onde o código aparece dentro do container é o passo 1 da tarefa. Esta revisão não escolhe arquivo, log nem padrão de linha. O rótulo permanece **Código de acesso**. Se a descoberta pedir outra palavra na cara do Hil, isso é dependência da implementação e fica nas notas de quem for executar; não muda esta revisão.
- A caixa mostra um valor, o que o passo 1 definir como o código atual. Se aparecer mais de um candidato, a implementação escolhe o critério e registra. O valor não entra no arquivo da tarefa, na Saída nem num teste versionado.
- A cara com o valor é um Form pequeno, não um MessageBox: precisa do campo selecionável e de **Copiar**. O "não encontrado" usa a mesma janela, sem o campo. A falha de leitura usa o MessageBox de erro, com a frase fixa, sem `ex.Message` e sem a saída remota.
- `.docs/ui-ux.md` fica para outra hora. README e changelog continuam na seção **Depois**.
- A altura extra é só no GroupBox do MausBot. A janela continua a da revisão da 01.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Fiz essa pensando no seu primeiro login, Hil. Se o código não estiver no log, me chama.
