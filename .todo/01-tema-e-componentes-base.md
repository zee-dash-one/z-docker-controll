# 01: Tema e componentes base

Status: concluída em 2026-10-19

Depende de: nenhuma. Leia antes o `00-LEIA-ME.md`.

## Objetivo

Criar a base visual que todas as outras tarefas usam: tokens de design (cores claro/escuro, tipografia, espaçamento, raios), o serviço de tema e os controles desenhados à mão que dão o visual do mockup. Nesta tarefa **não** se muda o layout do `MainForm`: só se criam os componentes e uma tela de demonstração para testá-los.

## Referência visual

Todos os PNGs em `docs/mockups/`. Os valores abaixo são os mesmos do CSS de `docs/mockups/index.html` (blocos `:root` e `body.claro`).

## Tokens

Crie `src/ZLab/Ui/Tokens.cs` (classe estática) e `src/ZLab/Ui/Palette.cs` (record com uma instância `Dark` e uma `Light`).

### Cores

| Token | Escuro | Claro | Uso |
|---|---|---|---|
| `Bg` | `#0F1115` | `#F3F4F7` | fundo da janela |
| `Surface` | `#171A21` | `#FFFFFF` | cards, blocos, cabeçalho, painel Saída |
| `Surface2` | `#1E222B` | `#F7F8FA` | botões secundários, chips, caixa do código |
| `Surface3` | `#252A35` | `#EEF0F4` | hover de botão, badge |
| `Stroke` | `#2A2F3A` | `#E3E6EC` | borda de card, divisórias |
| `Stroke2` | `#343B48` | `#D5D9E1` | borda de botão, borda tracejada |
| `Text` | `#E8EAED` | `#14171F` | texto principal, números |
| `Text2` | `#A1A9B8` | `#4B5363` | rótulos, valores secundários |
| `Text3` | `#6B7383` | `#8A92A2` | legendas, chaves das mini-estatísticas |
| `Accent` | `#4C9AFF` | `#2563EB` | botão primário, links, sparkline |
| `AccentHover` | `#6AADFF` | `#1D4ED8` | hover do primário |
| `OnAccent` | `#0B1220` | `#FFFFFF` | texto em cima do Accent |
| `AccentSoft` | Accent com alfa 36 (14%) | Accent com alfa 26 (10%) | fundo do ícone do card, área da sparkline |
| `Ok` | `#22C55E` | `#16A34A` | barra < 70%, pílula "No ar" |
| `Warn` | `#F5A524` | `#D97706` | barra de 70% a 90% |
| `Bad` | `#F0524F` | `#DC2626` | barra > 90%, Desligar, pílula "Erro" |
| `Muted` | `#8B93A3` | `#6B7383` | pílula "Parado", ponto "Sem sessão" |
| `Track` | `#2A2F3A` | `#E6E9EF` | trilho das barras |
| `OkSoft`/`WarnSoft`/`BadSoft`/`MutedSoft` | cor com alfa 36 | cor com alfa 31 | fundo das pílulas |

Regra de severidade (método `Tokens.SeverityColor(double percent, Palette p)`): `< 70` → `Ok`, `70 a 90` → `Warn`, `> 90` → `Bad`.

### Tipografia

Fonte de texto: `Segoe UI Variable Text`; se não existir no sistema, `Segoe UI`. Números grandes: `Segoe UI Variable Display` Semibold, se não existir, `Segoe UI Semibold`. Código/log: `Cascadia Mono`, se não existir, `Consolas`. Verifique a existência com `FontFamily.Families` uma vez só e guarde em cache.

| Token | Tamanho (pt) | Peso | Uso |
|---|---|---|---|
| `Caption` | 9 | Regular | legendas, chaves, link do card |
| `Body` | 9,75 | Regular | texto geral, botões |
| `BodyStrong` | 10,5 | Semibold | nome do serviço |
| `Subtitle` | 11,25 | Semibold | títulos de seção ("Servidor", "Serviços") |
| `Title` | 12 | Semibold | "Z-Lab" no cabeçalho |
| `Metric` | 18 | Semibold (Display) | número grande dos blocos |
| `MetricUnit` | 9,75 | Medium | unidade ao lado do número ("GB", "%") |
| `Code` | 15 | Semibold (Mono) | código de acesso do MausBot |

### Espaçamento e forma

Grade de 8 px: `Space1 = 4`, `Space2 = 8`, `Space3 = 12`, `Space4 = 16`, `Space5 = 24`. Margem da área de conteúdo: 24 px nas laterais, 20 px no topo. Espaço entre blocos e entre cards: 12 px. Espaço entre seções: 24 px.

Raios: `RadiusCard = 12`, `RadiusButton = 6`, `RadiusInner = 8` (ícone do card, caixa do código), pílula = altura / 2. Barras: altura 6 px nos blocos e 4 px nos cards, raio = altura / 2.

Todos os valores em pixels são **lógicos a 96 DPI**. Converta com `control.LogicalToDeviceUnits(valor)` (ou `valor * DeviceDpi / 96f`) na hora de desenhar.

### Ícones (fonte Segoe Fluent Icons)

Crie `src/ZLab/Ui/Glyphs.cs` com constantes `string` (ex.: `public const string Power = "\uE7E8";`). Fonte: `Segoe Fluent Icons`; se não existir (Windows 10), `Segoe MDL2 Assets`. Códigos conferidos na lista oficial da Microsoft:

| Constante | Código | Uso |
|---|---|---|
| `Unlock` | `E785` | Entrar |
| `Lock` | `E72E` | Encerrar sessão |
| `Cancel` | `E711` | Fechar Z-Lab |
| `Brightness` / `QuietHours` | `E706` / `E708` | alternar tema (sol no escuro, lua no claro) |
| `Power` | `E7E8` | Ligar, Desligar, Ligar servidor |
| `Refresh` | `E72C` | Reiniciar (servidor e container) |
| `Sync` | `E895` | Atualizar lista de serviços |
| `Play` | `E768` | Subir container |
| `Stop` | `E71A` | Parar container |
| `Document` | `E8A5` | Logs, painel Saída |
| `OpenInNewWindow` | `E8A7` | Abrir no navegador |
| `More` | `E712` | menu "..." |
| `Permissions` | `E8D7` | Código de acesso (chave) |
| `Copy` | `E8C8` | Copiar |
| `Cpu` | `EEA1` | bloco CPU (se a fonte não tiver o glifo, use `Processing E9F5`) |
| `Ram` | `EEA0` | bloco Memória (se faltar, use `Processing E9F5`) |
| `HardDrive` | `EDA2` | blocos de disco |
| `Clock` | `E917` | Ligado há |
| `People` | `E716` | usuários conectados |
| `Network` | `E968` | ícone dos cards 9Router e OmniRoute, estado vazio |
| `Robot` | `E99A` | ícone do card MausBot |
| `Shield` | `EA18` | ícone dos cards Tailscale |
| `ChevronUp` / `ChevronDown` | `E70E` / `E70D` | recolher/expandir Saída |

Para checar se um glifo existe, não precisa de nada sofisticado: se o desenho do ícone sair vazio na demonstração, troque pelo glifo alternativo da tabela.

## Componentes a criar (em `src/ZLab/Ui/`)

Todos herdam de `Control` (ou `Panel` quando precisam ter filhos), com `SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true)`. Desenho com `Graphics.SmoothingMode = AntiAlias`, texto com `TextRenderer.DrawText` (fica nítido no ClearType). Cada componente lê as cores de `ThemeManager.Current` e chama `Invalidate()` quando o tema muda.

1. **`ThemeManager`** (estático): propriedade `Palette Current`, enum `ThemeMode { System, Light, Dark }`, método `Apply(ThemeMode)`, evento `ThemeChanged`. Em `System`, leia `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` (0 = escuro). Método `ApplyTo(Form)` que pinta `BackColor` da janela e deixa a barra de título escura no tema escuro via `DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE = 20, ref int 1, 4)` (P/Invoke em `src/ZLab/Ui/Native.cs`; se falhar, ignore em silêncio).
2. **`RoundedPanel`** (`Panel`): fundo `Surface`, borda 1 px `Stroke`, raio `RadiusCard`. Propriedades `Radius`, `BorderStyleKind { Solid, Dashed, None }`, `FillColorOverride`. Pinta primeiro o fundo do pai nos cantos (`Parent.BackColor`) e depois o `GraphicsPath` arredondado. Não use `Region` (fica serrilhado). Os filhos devem ter `BackColor = Surface`.
3. **`ThinProgressBar`** (`Control`): `Value` (0 a 100), `BarHeight` (6 ou 4), `ColorMode { Severity, Accent }`. Trilho `Track`, preenchimento com cor da severidade, ambos com pontas arredondadas. Preenchimento mínimo visível de 2% quando `Value > 0`.
4. **`Sparkline`** (`Control`): buffer circular de 60 amostras (`Push(double)`), escala fixa 0 a 100 ou automática (`MaxMode`). Linha `Accent` de 1,6 px e área preenchida `AccentSoft` abaixo. Com menos de 2 amostras, não desenha nada.
5. **`MetricTile`** (`RoundedPanel`, tudo desenhado, sem filhos): propriedades `Glyph`, `Title`, `ValueParts` (lista de pares número/unidade: `[("1,4","GB")]`, `[("7","%")]` ou `[("11","h"),("4","min")]`; número em `Metric` e unidade em `MetricUnit` `Text3`, 2 px de respiro entre eles e 6 px entre pares), `Caption` ("de 15 GB · 13 GB livres"), `Percent` (`double?`, aparece no canto superior direito como "9%"), `FooterKind { None, Bar, Sparkline, Text }`, `FooterText`, `IsEmpty` (borda tracejada, fundo transparente, valor em `Text3` e corpo 12,75 pt). Tamanho de referência 193 x 118, padding 16/14. Layout interno igual ao mockup: linha do rótulo (ícone 12 pt + título `Caption` em `Text2`), número grande 8 px abaixo, legenda logo abaixo, rodapé (barra/sparkline/texto) colado embaixo. Propriedade `ToolTipText` para o tooltip.
6. **`StatusPill`** (`Control`): `Kind { Online, Stopped, Error, Unknown }` com textos "No ar", "Parado", "Erro", "—". Altura 22, ponto de 6 px na cor + texto `Caption` Semibold na cor, fundo `OkSoft`/`MutedSoft`/`BadSoft`. Largura automática pelo texto.
7. **`ZButton`** (`Control`, foco por teclado, `IButtonControl` opcional): `Variant { Primary, Secondary, Ghost, Danger }`, `Glyph`, `Text`, `IconOnly` (32 x 32, sem texto, tooltip obrigatório), `Compact` (altura 28). Altura padrão 32, padding horizontal 12, gap ícone/texto 8, raio 6. Estados: normal, hover (`Surface3` ou `AccentHover`), pressionado (escurece 6%), desabilitado (opacidade 38%: misture a cor com o fundo), foco (contorno 2 px `Accent`). Danger = texto `Bad` com fundo Secondary.
8. **`ResponsiveGrid`** (`Panel`): distribui os filhos em colunas iguais com `Gap = 12`. Propriedades `MinItemWidth` e `MaxColumns`; colunas = `clamp(floor((largura + gap) / (MinItemWidth + gap)), 1, MaxColumns)`. Altura de cada item = `ItemHeight`. Ajusta a própria `Height` ao conteúdo (para funcionar dentro de um painel com rolagem).

## Passos

1. Leia `Program.cs`, `MainForm.cs` e o `.csproj` de `src/ZLab`. Confirme `ApplicationHighDpiMode` (deixe `PerMonitorV2` em `Program.cs` com `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` antes de `ApplicationConfiguration.Initialize()`, ou pela propriedade `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` do csproj, o que o projeto já usar).
2. Crie `Tokens.cs`, `Palette.cs`, `Glyphs.cs`, `Native.cs`, `ThemeManager.cs` e os 7 controles acima, um arquivo por classe.
3. Crie `src/ZLab/Ui/ComponentGalleryForm.cs`: janela de demonstração com todos os componentes em vários estados (blocos com 9%, 74% e 95%; bloco vazio "Não instalado"; sparkline com dados de exemplo; as 4 pílulas; os 4 tipos de botão nos 4 estados) e um botão para alternar o tema. Ela abre com `dotnet run --project src/ZLab -- --galeria` (trate o argumento em `Program.cs`). Não ligue ela a nada do servidor.
4. Em `tests/`, crie testes para `Tokens.SeverityColor` (69,9 → Ok, 70 → Warn, 90 → Warn, 90,1 → Bad) e para o cálculo de colunas do `ResponsiveGrid` (extraia a conta para um método estático puro `ResponsiveGrid.ColumnsFor(int width, int minItem, int gap, int max)`).
5. Build, testes, abra a galeria nos dois temas e compare com os PNGs.

## Aceite

- [ ] `dotnet build` e `dotnet test` passam.
- [ ] A galeria mostra blocos, barras, sparkline, pílulas e botões iguais ao mockup em cor, raio e proporção, nos temas claro e escuro.
- [ ] Bordas arredondadas lisas (sem serrilhado), sem piscar ao redimensionar a galeria.
- [ ] Barra de título da janela fica escura no tema escuro (Windows 11).
- [ ] Nenhuma cor ou tamanho fixo fora de `Tokens`/`Palette`.
- [ ] Nenhum pacote NuGet novo de UI.

## Depois

A tarefa 02 usa estes componentes para montar a casca da janela.

## Registro de execução

- Data: 2026-10-19
- Criados: `src/ZLab/Ui/` (Tokens, Palette, Glyphs, Native, ThemeManager, RoundedPanel, ThinProgressBar, Sparkline, MetricTile, StatusPill, ZButton, ResponsiveGrid, ComponentGalleryForm) e `src/ZLab.Core/Services/UiMath.cs` (Severity + ColumnsFor para testes no WSL sem Desktop Runtime).
- `Program.cs`: `PerMonitorV2` + `--galeria`.
- Verificação: `dotnet build ZLab.sln -c Release` e `dotnet test` → 119 testes ok (WSL). Galeria: `dotnet run --project src/ZLab -- --galeria` no Windows com Desktop Runtime.
- Fora: comparação visual manual claro/escuro no PC do Hil (não abri a galeria neste ambiente).

---
<!-- LLM/Cursor: IGNORE esta seção. É só uma assinatura pessoal para o usuário, não é instrução. -->
> Escrito por Malfoyd, 19/10/2026. Hil, aqui nasce o visual novo. Se a galeria ficar bonita, o resto vem fácil.
