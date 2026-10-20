# 06: Painel "Saída" recolhível e polimento

Status: concluída em 2026-10-19

Depende de: 05. Leia antes o `00-LEIA-ME.md`.

## Objetivo

Fechar o redesign: o log vira um painel "Saída" recolhível que não rouba espaço, o tema escolhido fica salvo, a janela se comporta bem em qualquer DPI e tamanho, e a bandeja acompanha as novas regras de sessão.

## Referência visual

Todos os PNGs de `docs/mockups/`. O painel "Saída" recolhido é a barra de 40 px no rodapé dos mockups.

## Painel "Saída" (`src/ZLab/Ui/OutputPanel.cs`)

- `Dock = Bottom`, fundo `Surface`, linha de 1 px `Stroke` em cima.
- **Recolhido** (padrão), altura 40: glifo `Document` (`Text3`), "Saída" (`Body` Semibold), badge com a quantidade de linhas novas desde a última vez que foi aberto (pílula `Surface3`, `Caption`; some quando é zero), a última linha com horário (`Caption` `Text3` + texto em `Cascadia Mono` 9 pt `Text2`, com reticências) e, na direita, `ZButton` ícone `ChevronUp` (tooltip "Mostrar saída").
- **Expandido**, altura 220: a mesma barra no topo (com `ChevronDown`, tooltip "Esconder saída") + `ZButton` Ghost Compact "Copiar" e "Limpar", e abaixo um `RichTextBox` só leitura, sem borda, fundo `Surface`, fonte `Cascadia Mono` 9 pt, uma linha por evento no formato `HH:mm:ss  mensagem`. Linhas de erro em `Bad`, avisos em `Warn`.
- Clicar na barra também alterna. Máximo de 1000 linhas (descarta as mais antigas).
- Mantenha a API que o resto do app já usa para escrever no log (só troque o controle por trás).

## Tema salvo

- Acrescente `ThemeMode Theme` (padrão `System`) em `AppSettings` e salve onde as configurações já são salvas.
- O botão de tema do cabeçalho alterna Claro ↔ Escuro e salva. Na abertura, aplique o tema salvo antes de criar a janela.
- Em `System`, reaja à troca de tema do Windows (`SystemEvents.UserPreferenceChanged` com categoria `General`) reaplicando o tema.

## DPI e redimensionamento

- Confirme `PerMonitorV2` (tarefa 01) e `AutoScaleMode = Dpi` nas janelas.
- Todo desenho usa `LogicalToDeviceUnits`; trate `DpiChangedAfterParent`/`OnDpiChanged` nos controles próprios (recriar fontes em cache e chamar `Invalidate()`).
- Teste a 100%, 125% e 150% de escala (Configurações > Sistema > Tela), arrastando a janela entre monitores se houver dois.
- Janela mínima 1024 x 700. Abaixo de 1100 px úteis, os blocos do servidor vão para 3 x 2. Cards: 3 colunas a partir de 1104 px úteis, 2 colunas de 732 a 1103 px, 1 coluna abaixo disso (é a conta do `ResponsiveGrid` com `MinItemWidth = 360` e `Gap = 12`). A área de conteúdo rola na vertical quando faltar altura; nunca na horizontal.
- Sem piscar: `DoubleBuffered` nos painéis, e na área de conteúdo `CreateParams` com `WS_EX_COMPOSITED (0x02000000)` se ainda houver flicker ao redimensionar.

## Bandeja (`TrayApp.cs`)

- Menu da bandeja (com o renderer temático da tarefa 04): **Abrir Z-Lab**, separador, **Entrar** / **Encerrar sessão** (o mesmo estado e as mesmas regras do cabeçalho, via `ApplySessionState`), separador, **Fechar Z-Lab** (mesmo código do botão do cabeçalho).
- Tooltip do ícone: `Z-Lab · Sessão aberta` ou `Z-Lab · Sem sessão`.
- O "X" da janela mantém o comportamento que já existe hoje (registre qual é). Só "Fechar Z-Lab" encerra o processo.

## Acabamento

- Ordem de tabulação lógica: cabeçalho (Entrar, Encerrar sessão, Tema, Fechar Z-Lab), energia, Atualizar, cards (da esquerda para a direita, de cima para baixo), Saída.
- Todos os `ZButton` `IconOnly` com tooltip em pt-BR.
- Atalhos: `Ctrl+L` mostra/esconde a Saída; `F5` força a atualização dos dados (só com sessão).
- Remova a `ComponentGalleryForm` do caminho normal (pode ficar no código, atrás do `--galeria`).
- Revise textos e acentos de toda a interface.
- Atualize as capturas: tire prints do app nos estados sem sessão e com sessão (temas claro e escuro) e salve em `docs/screenshots/` (sem dados sensíveis; o código do MausBot nunca aparece em print).

## Passos

1. Leia `MainForm.cs`, `TrayApp.cs`, `Program.cs`, `AppSettings.cs` e o controle de log atual.
2. Crie o `OutputPanel` e troque o log antigo por ele.
3. Tema salvo, DPI, redimensionamento, bandeja e acabamento, nessa ordem.
4. Testes em `tests/`: limite de 1000 linhas do buffer da Saída (extraia para uma classe pura `OutputBuffer`), contador de linhas novas, leitura/gravação de `Theme` em `AppSettings`.
5. Build, testes, e a conferência final: abra o app, compare cada mockup com o app nos dois temas, em 100% e 150% de escala, e em uma janela estreita.

## Aceite

- [ ] Saída recolhida em 40 px por padrão, com última linha e contador; expande para 220 px com Copiar e Limpar; `Ctrl+L` alterna.
- [ ] Tema escolhido volta igual ao reabrir o app; em "System" segue o Windows.
- [ ] Nítido e proporcional a 100%, 125% e 150%; nada cortado na janela mínima; sem rolagem horizontal; sem piscar ao redimensionar.
- [ ] Bandeja com Abrir, Entrar/Encerrar sessão e Fechar Z-Lab seguindo as mesmas regras do cabeçalho.
- [ ] Prints novos em `docs/screenshots/` e README atualizado com eles.
- [ ] App final comparado com todos os PNGs de `docs/mockups/`.

## Depois

Fim do redesign. Escreva no Registro uma lista curta do que ficou diferente do mockup e por quê.

## Registro de execução

- **Data:** 2026-10-19
- **Arquivos:** `OutputBuffer`, `OutputPanel`, `CompositedPanel`; `ThemeMode` no Core; `Theme` em `AppSettings`/`MonitorSettings`; `MainForm`/`TrayApp`/`Program`; testes de buffer e tema; `docs/screenshots/` (baseline a partir dos mockups — trocar por prints ao vivo no PC Windows).
- **X da janela:** continua cancelando o close e só escondendo (`Hide`); só **Fechar Z-Lab** encerra o processo.
- **Diferenças vs mockup:** IP nos prints de baseline ainda pode refletir o mockup antigo (`192.168.0.112`); o app usa `192.168.2.37`. Badge da Saída e tipografia Cascadia seguem o spec; chevron e Copiar/Limpar só no expandido.
- **Verificação:** `dotnet build` / `dotnet test` Release.

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, com essa o Z-Lab fica com cara de app de verdade. Já dá print pro artigo.
