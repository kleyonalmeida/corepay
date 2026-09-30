# Identidade visual — CorePay

Especificação para **recriar a mesma UI em qualquer linguagem** (SwiftUI, Flutter, Compose, Swift, Kotlin, Vue, etc.). Não copie classes Tailwind: implemente os tokens e as receitas abaixo.

**Tema padrão: escuro (Lumea).** Sem preferência salva, o app inicia em dark (`localStorage.theme`, fallback `dark`). O claro é uma contraparte derivada dos mesmos cyan/teal e neutros frios.

Fonte do código: `frontend/src/WebApp.Blazor/wwwroot/css/app.css`, `Components/Ui/*`, `Components/Layout/*`.

---

## 1. Personalidade

Dashboard financeiro interno, denso, noturno — **visionário, preciso e fluido** (Lumea).

- Superfícies oceânicas profundas (`#001718`), acento **cyan/aqua** (`#00DDD6`, `#00F2EA`).
- Glassmorphism leve: painéis translúcidos, blur 12px, borda interna branca @ 10%.
- Muito dado em tabela; cards com borda 1px e profundidade tonal.
- Ícones lineares (Lucide), 16px, nunca preenchidos.
- Status e dinheiro falam por **cor semântica em chips translúcidos**, não por blocos sólidos.
- Tipografia dual: **Montserrat** (títulos/marca) + **Inter** (UI e dados).
- Locale visual: **pt-BR** (R$, `1.234,56`, datas `dd/MM/yyyy`).

Interações neutras: **hover** com borda e texto cyan; **selecionado** (nav, tabs, accordions) persiste com `primary-container` e borda cyan.

---

## 2. Tokens de cor

Valores CSS são HSL **sem** a função — o app usa `hsl(var(--token))`. Hex é a conversão de referência.

### 2.1 Primária Lumea

| Token | Hex | Uso |
|---|---|---|
| `primary` / `primary-container` | `#00F2EA` | CTA, nav ativa, gradientes |
| `primary-foreground` / `on-primary-container` | `#003735` / `#006A66` | Texto sobre cyan |
| `ring` / `surface-tint` | `#00DDD6` | Hover, focus, bordas interativas |
| `destructive` | `#FFB4AB` | Erro, excluir |
| `destructive-foreground` | `#690005` | Texto sobre destructive |

Tokens extras: `--glass-blur` (12px), `--interactive-hover-bg`, `--ambient-shadow`, `--radius-lg` (16px), `--radius-xl` (24px).

### 2.2 Dark (default — Lumea)

| Token | Hex | Uso |
|---|---|---|
| `background` | `#001718` | Canvas |
| `foreground` | `#CDE8E9` | Texto principal |
| `card` | `#0A2324` | Superfície glass |
| `secondary` | `#152E2F` | Inputs, filtros |
| `muted-foreground` | `#B9CAC8` | Labels |
| `border` | `#3A4A48` | Divisores |
| `sidebar-background` | `#001112` | Sidebar |

### 2.3 Light (derivado Lumea)

Hue dominante **176–178** (cyan/teal). Fundos claros com tint oceânico; acentos mais escuros para contraste.

| Token | Uso |
|---|---|
| `background` | Canvas claro cyan |
| `foreground` | Teal escuro `#003735` |
| `ring` | Cyan médio para hover/focus |

### 2.4 Charts

Paleta derivada de cyan/teal + secondary blue (`#BAC8DC`). Tokens `chart-1` … `chart-5` mapeados em `app.css`.

### 2.5 Semântica operacional (Tailwind palette, 400 + 15% bg)

Estas cores **não** são tokens CSS; são a linguagem de status. Use as mesmas em qualquer tema.

| Papel | Fundo | Texto | Borda (quando chip) |
|---|---|---|---|
| Sucesso / pago / aprovado / ativo / entrada | `emerald-500` @ 15% `#10B98126` | `emerald-400` `#34D399` | `emerald-500` @ 30% |
| Pago (ênfase extra) | `emerald-500` @ 20% | `emerald-300` `#6EE7B7` | `emerald-500` @ 40% |
| Pendência / aguardando / solicitado | `yellow-500` @ 15% `#EAB30826` | `yellow-400` `#FACC15` | `yellow-500` @ 30% |
| Rascunho / info / cartão | `blue-500` @ 15% `#3B82F626` | `blue-400` `#60A5FA` | `blue-500` @ 30% |
| Erro / reprovada / inativo / saída | `red-500` @ 15% `#EF444426` | `red-400` `#F87171` | `red-500` @ 30% |
| Alerta / NF pendente / imposto / boleto | `orange-500` @ 15% `#F9731626` | `orange-400` `#FB923C` | — |
| Neutro extra / colaboradores / PIX | `purple-500` @ 15% `#A855F726` | `purple-400` `#C084FC` | — |
| Destaque de período (mudança de cargo) | `cyan-500` @ 5% bg, borda @ 30% | — | `cyan-500` @ 30% |
| Rev analista (supervisor) | `cyan-500` @ 10% | `cyan-400` `#22D3EE` | — |

Hex Tailwind 500/400 de referência:

```
emerald-500 #10B981   emerald-400 #34D399   emerald-300 #6EE7B7
yellow-500  #EAB308   yellow-400  #FACC15
blue-500    #3B82F6   blue-400    #60A5FA
red-500     #EF4444   red-400     #F87171
orange-500  #F97316   orange-400  #FB923C
purple-500  #A855F7   purple-400  #C084FC
cyan-500    #06B6D4   cyan-400    #22D3EE
```

Alpha 15% ≈ `26` hex; 10% ≈ `1A`; 20% ≈ `33`; 30% ≈ `4D`; 40% ≈ `66`; 50% overlay do drawer mobile = `#00000099`. Overlay de dialog = `#000000CC` (`black/80`).

---

## 3. Tipografia

**Dual:** Montserrat (display/títulos) + Inter (UI/dados), Google Fonts.

```
--font-heading:  Montserrat, Inter, ui-sans-serif, system-ui, sans-serif
--font-body:     Inter, ui-sans-serif, system-ui, sans-serif
--font-display:  Montserrat, Inter, ui-sans-serif, system-ui, sans-serif
--font-mono:     ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace
```

Mono só em chave PIX (`text-xs font-mono`).

| Papel | Size | Weight | Line / extras | Classe de origem |
|---|---|---|---|---|
| Título de página | 24px | 700 | cor `foreground` | `text-2xl font-bold` |
| Subtítulo de página | 14px | 400 | `muted-foreground`, margin-top 2px | `text-sm mt-0.5` |
| Título de card/seção | 16px implícito | 600 | `foreground` | `font-semibold` |
| Título da topbar | 14px | 600 | `foreground` @ 60% | `text-sm font-semibold text-foreground/60` |
| Marca FolhaPay | 14px | 700 | tracking wide, branco | `text-sm font-bold tracking-wide` |
| Tagline sidebar | 12px | 400 | `sidebar-foreground` @ 60% | `text-xs` |
| Nav item | 14px | 500 | | `text-sm font-medium` |
| Corpo / tabela | 14px | 400 | | `text-sm` |
| Label de campo (forms densos) | 12px | 400 | `muted-foreground` | `text-xs` |
| Label shadcn (auth) | 14px | 500 | | `text-sm font-medium` |
| Chip / badge | 12px | 600 | | `text-xs font-semibold` |
| Stat number | 20px | 700 | `tabular-nums`, nowrap | `text-xl font-bold tabular-nums` |
| Valor em tabela | 14px | 600 | `tabular-nums`, alinhado à direita | |
| Micro (CPA rows) | 11px | 400–600 | | `text-[11px]` |
| Auth H1 | 30px | 700 | tracking-tight | `text-3xl font-bold tracking-tight` |
| Empty state título | 18px | 500 | | `text-lg font-medium` |

**Números de dinheiro:** sempre `tabular-nums` + `pt-BR` com 2 casas: `R$ 1.234,56`.  
**Datas:** `dd/MM/yyyy`. Referência de folha: `Jan/2026` (mês abreviado pt).

---

## 4. Forma, espaço, elevação

### Raio

```
--radius: 0.625rem   /* 10px — botões, inputs, nav item, logo mark */
radius-lg = 10px
radius-md = 8px      /* calc(radius - 2px) */
radius-sm = 6px      /* calc(radius - 4px) */
```

No produto real:

| Elemento | Raio |
|---|---|
| Botão, input shadcn, select nativo denso | 8–10px (`rounded-md` / `rounded-lg`) |
| Card, tabela, filtro, stat | **12px** (`rounded-xl`) |
| Chip de status | **999px** (`rounded-full`) |
| Avatar | círculo |
| Logo mark (quadrado 32px) | 8px (`rounded-lg`) |
| Auth icon wrap 56px | 16px (`rounded-2xl`) |
| Card de login | 16px (`rounded-2xl`) |
| Dialog | 8px em sm+ (`sm:rounded-lg`) |
| Toggle custom de meta | 999px, 32×16px |

### Espaçamento

Escala 4px. Páginas: `space-y-6` (24px) entre blocos.

- Shell main padding: **24px**
- Sidebar width: **256px** (`w-64`)
- Sidebar nav item: px 12 / py 10
- Sidebar logo: px 24 / py 20
- Card padding stat: **20px**
- Header de lista/card: px 24 / py 16
- Célula de tabela: px 16–24 / py 12
- Gap de grid de stats: **16px**
- Gap header da página (título vs filtros): **16px**, wrap
- Formulário auth: `space-y-4`, card `p-8`

### Elevação

Glassmorphism tonal (Lumea).

- **Level 0:** `background` sólido oceânico.
- **Level 1:** painéis `card/72` + `backdrop-filter: blur(12px)` + borda branca @ 10%.
- **Level 2:** dialogs/popovers com `--ambient-shadow` cyan suave.
- Nav/tabs ativos: borda `ring` + fundo `primary-container/15–28%`.
- Topbar/sidebar: vidro translúcido com blur 12px.

---

## 5. Ícones

Biblioteca: **Lucide** (stroke, round caps). Tamanho padrão **16×16** (`w-4 h-4`). Topbar **20×20**. Auth hero **28×28** dentro de quadrado 56×56 primary.

Mapeamento de nav:

| Item | Ícone Lucide |
|---|---|
| Dashboard | `LayoutDashboard` |
| Folhas | `FileText` |
| Colaboradores | `Users` |
| Faturamento | `TrendingUp` |
| Investimento Tráfego | `Target` |
| Financeiro | `DollarSign` |
| Fluxo de Caixa | `Building2` |
| Relatórios | `BarChart3` |
| Configurações | `Settings` |
| Logo mark | `DollarSign` branco 16px em quadrado primary 32px |
| Sair | `LogOut` |
| Tema | `Sun` (quando dark) / `Moon` (quando light) |
| Notificações | `Bell` |
| Recolher sidebar | `PanelLeftClose` / `PanelLeftOpen` |

Ações: `Plus`, `Pencil`, `Trash2` (hover `red-400` + fundo `red-500/10`), `Search`, `ExternalLink`, `Check`, `X`.

Spinner: anel 32px, borda 4px `border`, topo `primary`, `animate-spin`. Loading de página: texto `muted-foreground` centralizado.

### 5.1 Implementação Blazor (Fase 1.4)

- Pacote: `InfiniLore.Lucide` (`AddLucideIcons()` no startup WASM).
- Componente: `Icon` (`Components/Ui/Icon.razor`) — catálogo restrito via enum `IconKind`; tamanhos via `IconSize` (`Default` 16px, `Topbar` 20px, `AuthHero` 28px, `EmptyState` 32px).
- Estilo: `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`, caps/joins arredondados; classes `.ui-icon`, `.ui-icon--default|topbar|auth-hero|empty-state`.
- Defaults: `ThemeToggle` → `Sun`/`Moon` 20px; `StatCard` → ícone por `SemanticTone`; `EmptyState` → `Search` 32px; `Dialog` close → `X` 16px.
- Vitrine: seção **Ícones Lucide** em `/dev/kitchen` lista nav + ações + tamanhos da §5.
- Status auxiliares (fora nav, usados em badges): `UserCheck`, `UserX`, `CheckCircle2`, `Wallet`, `Calendar`.

---

## 6. Shell da aplicação

Layout fullscreen `h-screen`, `overflow-hidden`, flex row.

```
┌────────────┬──────────────────────────────────────┐
│ SIDEBAR    │ TOPBAR  (h ~ 64px, blur, border-b)   │
│ 256px      │  [menu] [título]     [sun] [bell]    │
│ #0A0A0A    ├──────────────────────────────────────┤
│            │ MAIN  padding 24px, scroll vertical  │
│ logo       │                                      │
│ nav        │  page header                         │
│            │  filters / stats / tables            │
│ user+sair  │                                      │
└────────────┴──────────────────────────────────────┘
```

### Sidebar

- Fundo `sidebar-background`, borda direita 1px `sidebar-border`.
- Logo: mark 32×32 `primary` `rounded-lg` + “CorePay” + “Gestão de Pagamentos”.
- Item inativo: `sidebar-foreground`, hover fundo `sidebar-accent` e texto **branco**.
- Item ativo: fundo `primary`, texto branco, `shadow-lg shadow-primary/20`, chevron 12px à direita.
- Gap ícone–label: 12px. `rounded-lg`. `space-y-1` entre itens.
- Rodapé: card `sidebar-accent` `rounded-lg` com avatar 28px círculo `primary/20` + inicial `primary` bold xs. Botão Sair full width, hover texto `red-400`.
- Mobile `< lg`: drawer off-canvas, overlay `black/60`, z-30. Desktop: estática; pode colapsar para width 0.

### Topbar

- `px-6 py-4`, `border-b`, `bg-card/50`, `backdrop-blur-sm`.
- Esquerda: hamburger (mobile) ou collapse (desktop).
- Centro-esquerda: nome da rota atual, 14px semibold @ 60% opacity — **só desktop**.
- Direita: toggle tema + sino. Badge de não-lidas: círculo 20px `destructive`, `text-xs`, absoluto `-top-1 -right-1`.

### Main

Fundo `background`. Conteúdo não é um card full-bleed: cada bloco (header, filtros, stats, tabela) é um card separado com 24px de ritmo vertical.

Breakpoint `lg` = 1024px para sidebar e grids.

---

## 7. Receitas de componente

### 7.1 Botão (shadcn New York)

Base: inline-flex, gap 8px, `rounded-md` (8px), `text-sm font-medium`, SVG 16px, focus ring 1px `ring`, disabled opacity 50%.

| Variant | Fundo | Texto | Extra |
|---|---|---|---|
| default (CTA) | `primary` | branco | shadow; hover `primary/90` |
| destructive | `destructive` | `destructive-foreground` | hover /90 |
| outline | transparente | foreground | borda `input`; hover `accent` |
| secondary | `secondary` | `secondary-foreground` | hover /80 |
| ghost | — | — | hover `accent` |
| link | — | `primary` | underline no hover |

Alturas: default 36px (`h-9` px-4); sm 32px texto xs; lg 40px px-8; icon 36×36.  
Auth Google: outline, **h-12**, full width.

CTA “Nova Folha”: default + ícone `Plus`.

### 7.2 Chip de status (`StatusBadge`)

Pill, `text-xs font-semibold`, `px-2.5 py-0.5`, borda 1px.

| Status | Label | Estilo |
|---|---|---|
| `draft` | Rascunho | blue 15/400 + borda 30% |
| `pending_approval` | Aguard. Aprovação | yellow |
| `approved` | Aprovada | emerald |
| `rejected` | Reprovada | red |
| `paid` | Paga | emerald 20% / 300 / borda 40% |
| `pending` | Pendente | yellow |

Ativo/Inativo de colaborador: mesmo padrão pill + ícone 12px `UserCheck` / `UserX`.

Pago vs pendente (financeiro): pill emerald vs yellow com ícone `CheckCircle2` / `Wallet`.  
NF: emerald “Enviada” vs orange “Pendente”.

### 7.3 Stat card

Card 12px, borda, `p-5`, coluna.

1. Linha: label 12px medium `muted-foreground` à esquerda; ícone 16px em quadrado **32×32** `rounded-lg` com fundo semântico 15%.
2. Valor 20px bold tabular, `mt-3`.
3. Opcional: sub 12px muted `mt-1`.

Grid dashboard: 2 colunas mobile, 4 no `lg` na primeira fileira; 1 / 3 na segunda. Relatórios: até 5 colunas. Fluxo de caixa: 4 summary cards.

Cores de ícone no dashboard:

- Folhas → blue
- Aguardando → yellow
- Aprovadas → emerald
- Reprovadas → red
- Total a pagar → **primary**
- Total pago → emerald
- Colaboradores → purple

### 7.4 Card de lista / tabela

`bg-card border rounded-xl overflow-hidden`.

- Header: `px-6 py-4 border-b`, título semibold + link `text-sm text-primary hover:underline` à direita.
- Header de grupo (folhas por mês): `bg-secondary/40`, ícone `Calendar` primary 16px, título “Jan 2026”, micro count muted.
- Thead: `bg-secondary/40` (ou `/30`), `text-xs` ou `text-sm` `muted-foreground` `font-medium`, `py-3`.
- Linha: `border-b border-border/50`, hover `accent/30` (dashboard) ou `accent/20`. Cursor pointer se a linha navega.
- Linha paga (financeiro): fundo `emerald-500/10`.
- Empty: `p-8` ou `p-12` centralizado, ícone 32px opacity 40%, texto muted.

Filtros: um card `p-4` flex wrap `gap-3`. Search com ícone absoluto left-3. Selects nativos: `bg-secondary border border-border rounded-lg px-3 py-2 text-sm`.

### 7.5 Inputs densos (folha, caixa, tráfego)

Não usam o Input shadcn transparente: usam **`bg-secondary border border-border rounded-lg`**, `text-xs` ou `text-sm`, `px-2~3 py-1.5~2`, `text-foreground`. Placeholder `muted-foreground`.

Prefixo “R$” absoluto left-2 em campos monetários.

Focus: o app quase não mostra ring nos nativos; no shadcn, ring 1px `primary`.

### 7.6 Toggle de meta (custom, não o Switch Radix)

Trilho 32×16px, pill. Off: `muted`. On: `emerald-500` (não primary). Thumb 12px branco, sombra, left 2px / 16px.

Label 12px muted ao lado. Clique no row inteiro.

Switch shadcn (se usado): 36×20, checked = **primary**, thumb 16px.

### 7.7 Nested block (projeto na folha)

`border border-border rounded-lg p-3 space-y-3 bg-secondary/20`.  
Total do bloco à direita: `text-xs font-bold text-primary`.  
Lixeira: muted → `red-400`.

Mudança de cargo: mesma estrutura com `border-cyan-500/30 bg-cyan-500/5`.

**Implementação folha (Fase 7.4):** classes `.payroll-nested-block`, `.payroll-nested-block--inner`, `.payroll-nested-block--error`, `.payroll-role-change-block`, `.payroll-entry-totals__total` (bold primary xs), `.payroll-hubla-hint`, `.payroll-complement-error` em `wwwroot/css/app.css`.

### 7.8 Avatar de iniciais

Círculo. Fundo `primary/20`, texto `primary` bold xs. 28px (sidebar) ou 32px (tabela). Foto cobre o círculo (`object-cover`). Iniciais = 1–2 primeiras letras das palavras do nome, uppercase.

### 7.9 Tabs

Lista `h-9 rounded-lg bg-muted p-1`. Trigger ativo: `bg-background` + `shadow` + `foreground`. Inativo: `muted-foreground`.

Usado em Financeiro (Colaboradores | Custo por Projeto) e Fluxo de Caixa.

### 7.10 Dialog / modal

Overlay `black/80`. Painel centrado, `max-w-lg` (padrão) ou `max-w-3xl` (variante larga `.ui-dialog--wide`, `max-width: 48rem`, `max-height: 90vh`, scroll interno — usada em formulários extensos como Níveis de carreira), `p-6`, borda, `bg-background`, zoom 95→100. Close `X` 16px top-right opacity 70→100. Parâmetro Blazor: `Dialog.IsWide`.

### 7.11 Progresso de pagamento

Trilho `h-1.5 bg-secondary rounded-full`. Fill `emerald-500`. Percentual 11px muted tabular à direita.

### 7.12 Tipo entrada/saída (caixa)

Segmented 2 opções. Ativo entrada: `bg-emerald-500/20 border-emerald-500/50 text-emerald-400`. Ativo saída: equivalente red. Inativo: `secondary` + muted.

Valores: entrada emerald, saída red, sempre tabular.

**Implementação (Fase 12.1):** classes `.cashflow-type-segmented*`, `.cashflow-table__entrada`, `.cashflow-table__saida`, `.cashflow-stats-grid` (2→4 colunas), `.payment-method-chip--*` em `wwwroot/css/app.css`; componentes `CashflowSection`, `CashflowEntryFormDialog`, `PaymentMethodChip`.

Formas de pagamento (chips):

| Nome contém | Chip |
|---|---|
| cartão | blue |
| dinheiro | emerald |
| pix | purple |
| boleto | orange |
| transferência | yellow |

### 7.13 Notificações

Row com quadrado de ícone 40px-ish, fundo semântico 10%:

- `payroll_rejected` → red
- `payroll_submitted` → yellow
- `payroll_approved` → emerald

**Implementação Blazor (Fase 14.2):**

| Artefato | Caminho |
|---|---|
| Lista | `Components/Notifications/NotificationsSection.razor` + `NotificationRow.razor` |
| Badge topbar | `Components/Layout/AppHeader.razor` — `.app-header__notifications-badge` (`destructive`, `9+` cap) |
| Mapper de tom | `Formatting/NotificationToneMapper.cs` → `.notification-row__icon--{red\|yellow\|emerald}` |
| CSS | `wwwroot/css/app.css` — `.notification-row*`, `.notifications-page` |
| Página | `/notifications` → `Pages/Notifications.razor` |

Row não lida: fundo `accent/25`; lida: opacidade reduzida. Click na row marca lida (idempotente) e navega para `/payrolls/{id}`.

### 7.14 Auth

Canvas `background`, card max-w-md centrado. Ícone 56×56 `rounded-2xl` `primary`. Título 30px. Card `p-8 rounded-2xl shadow-sm`. Divider “or” com linha `border` e label muted uppercase xs. Erro: `bg-destructive/10 text-destructive` `p-3 rounded-lg text-sm`. Footer 14px muted, link `primary` medium underline hover.

---

## 8. Receitas de tela

Toda página autenticada começa igual:

```
[título 24/bold]                    [filtros mês/ano à direita]
[subtítulo 14 muted]
[24px]
[grid de stats]
[24px]
[card de filtros se houver]
[card(s) de tabela/conteúdo]
```

- **Dashboard (Fase 13.1):** 4 stats + até 3 stats conforme permissões + card “Folhas recentes”; filtros mês/ano no topo; grades `.dashboard-stats-grid--primary` (2→4 colunas) e `--secondary` (1→3); tabela `.dashboard-recent-table` com linha clicável, hover `accent/30`, foco por teclado e paga em emerald/10. Estados loading/erro/sem permissão usam `Spinner`/`EmptyState`.
- **Relatórios (Fases 13.2–13.3):** header + filtros ano/setor/projeto; toolbar `.reports-toolbar` com botão **Exportar Excel** (`Button` outline `Sm`, ícone `Download`); banner de erro de ação `.reports-action-error`; grade `.reports-stats-grid` com 5 `StatCard` (2→3→5 colunas); gráficos de barras CSS/SVG com paleta `--chart-1`…`--chart-5` (evolução mensal + distribuição por setor); tabs **Por setor** \| **Por projeto** \| **Por colaborador** com tabela densa `.reports-table`; valores tabulares à direita via `MoneyFormatter`. Implementado em `/reports` (`ReportsSection`, `ReportsBarChart`, `ReportsApiService`, `FileDownloadService`).
- **Folhas:** header com CTA primary à direita → card filtros → grupos por mês (card por competência). Implementado em `/payrolls` (Fase 7.2): `.payrolls-filters`, `.payrolls-competence-header` (`bg-secondary/40` + ícone Calendar), `.payrolls-table` com linha paga `emerald-500/10`, competência `Jan/2026` via `CompetenceFormatter`.
- **Detalhe da folha (Fase 7.6):** `.payroll-detail` `max-w-3xl` centrado; header flex com título, competência (`CompetenceFormatter`), `StatusBadge`, botão “Editar” outline (só `payrolls.write` + draft/rejected); meta “Submetido por”; alerta de reprovação; card resumo (total + contagem); lista `.payroll-detail-entry` expansível (chevron rotate 90°, `aria-expanded`); linha paga `emerald-500/10`; chips aprovado/pago/NF via `StatusBadge`; PIX `.text-pix-key`; totais tabular; expansão com grid de totais + tabela “Custo por projeto”. Implementado em `Components/Payroll/PayrollDetailSection.razor`.
- **Formulário de folha (Fase 7.3 + 15.2):** `.payroll-form` `max-w-4xl` centrado; header título + `StatusBadge` (editar); card Competência (setor/mês/ano — bloqueado na edição); card Colaboradores com CTA “Adicionar colaborador” + linhas `.payroll-entry-shell` (`secondary/20`, avatar, PIX mono, salário tabular, perfil); picker em `Dialog` com `EmptyState`/`Spinner` padrão; bloqueio “Folha não editável” fora de draft/rejected via `PayrollEditabilityGuard` — card `.payroll-form__readonly-card` (borda + fundo `secondary/20`) + `StatusBadge` do status + link “Ver detalhe”. Implementado em `Components/Payroll/PayrollFormShell.razor` e `PayrollEditabilityGuard.razor`.
- **Financeiro (Fase 9.2–9.4 + 15.2):** tabs `Colaboradores` \| `Custo por Projeto` via `Tabs` (§7.9); aba Colaboradores: stats Total a pagar (primary) / Total pago (emerald) via `.financial-stats-grid`; accordion por folha com `.financial-amount__gross` riscado + `.financial-amount__receive` primary; progress bar `.financial-progress` emerald 6px + label `paidCount/entryCount`; tabela coluna “A Receber”; linha paga `.financial-table__row--paid` (`rgb(16 185 129 / 0.1)`, preservada no hover); ações pago/NF por linha; barra `.financial-payroll-card__admin-actions` com “Adicionar colaborador avulso” (outline + ícone `Users`) e “Excluir folha” (destructive + `Trash2`, só Admin); picker reutiliza `PayrollCollaboratorPicker` em `Dialog`; aba Custo por Projeto: accordion por folha + colaborador expansível (`.financial-project-cost-entry*`) com tabela “Custo por projeto” (reuso `.payroll-detail-breakdown*`); filtros compartilhados acima das tabs. Implementado em `Components/Financial/FinancialSection.razor`.
- **Faturamento (Fase 10.1):** header + card filtros (`.project-revenues-filters`: mês, ano, projeto) + card tabela (`.project-revenues-table`); colunas monetárias alinhadas à direita com `MoneyFormatter` + `tabular-nums`; competência via `CompetenceFormatter`; CTA “Novo faturamento” no header da tabela (só `revenues.write`); feedback emerald/red (`.project-revenues-action-message` / `-error`); dialog largo com total derivado em destaque (`.project-revenues-form__total`), campos iGaming/vendas, % grupo e textarea de observações. Implementado em `/project-revenues` (`ProjectRevenuesSection`, `ProjectRevenueFormDialog`).
- **Fluxo de caixa (Fases 12.1–12.2 + 15.2):** tabs **Lançamentos** \| **Relatório** (`Tabs` §7.9). Aba Lançamentos: 4 summary cards (`StatCard` emerald/red/blue); filtros mês/ano/tipo/projeto/setor/busca; tabela com chips de forma de pagamento; DELETE com dialog de confirmação (`Dialog` destructive); segmented entrada/saída no dialog; modal de parcelas. Aba Relatório: filtros mês/ano; 4 stats; grid 2 colunas (`lg`) com tabelas **Por projeto** (entradas/saídas/saldo) e **Por forma de pagamento** (saídas); bucket **Sem projeto**; empty states por tabela. CSS: `.cashflow-report`, `.cashflow-report-grid`, `.cashflow-report-table__count`. Implementado em `/cashflow` (`CashflowSection`, `CashflowReportSection`, `CashflowEntryFormDialog`, `CashflowInstallmentGroupDialog`).
- **Tráfego:** stats (solicitado yellow, depositado/saldo/total emerald, gasto purple, imposto orange) + cards por projeto.
- **Configurações:** cards seção com ícone primary 16px no header (`ui-section-header`, `ui-card__header`); tabela densa `.settings-table`; formulário em dialog `.settings-form` com toggles `GoalToggle`; erro inline `.settings-form__error`. Implementado nas seções Setores/Níveis/Projetos (`/settings`, Fases 3.2–3.4).
- **Administração — Usuários:** mesmo padrão visual de Configurações em `/admin/users`; tabela com chips de papel (`.settings-chip`, `.settings-chip-list`); multi-seleção em fieldset (`.settings-checkbox-list`); dialog largo (`Dialog.IsWide`) para papéis/setores; confirmação de exclusão com botão `Destructive`; ações agrupadas (`.settings-table__action-group`).
- **Colaboradores:** header + card de filtros (`.collaborators-filters`) + card de tabela (`.collaborators-table`); avatar 32px + nome/cargo; demissão inativa em `.text-dismissal-date` (`#F87171`); PIX `.text-pix-key`; salário `.text-table-money tabular-nums`; chip Ativo/Inativo; botão “Editar” em `.collaborators-table__row-action` (opacity 0→1 no hover/focus da linha). Implementado em `/collaborators` (Fase 4.1).
- **Empty/loading (Fase 15.2):** listas operacionais usam `Spinner` (`.ui-spinner`, `Label` acessível) + `EmptyState` (ícone Search 32px) dentro de `.settings-section__loading`; guards full-page mantêm `.auth-loading` + texto visível; sub-listas aninhadas (`TrafficWeekPanel` depósitos, `PayrollCollaboratorPicker`) seguem o mesmo padrão compacto quando aplicável.

Largura: a maioria das listas é full width da main. Detalhe/form restringe (3xl/4xl).

---

## 9. Motion

- Nav: `transition-all 150ms`.
- Sidebar: `300ms` translate/width.
- Hover de linha: `transition-colors` (sem slide).
- Accordion: `0.2s ease-out` height.
- Dialog: fade + zoom 95%.
- Collapse do sidebar: width 256 → 0.
- Sem bounce, sem spring exagerado, sem page transitions.

---

## 10. Acessibilidade visual mínima

- Contraste no dark: texto `#CDE8E9` / `#B9CAC8` sobre `#001718` / `#0A2324`.
- Foco visível: anel cyan 1px (`ring` / `#00DDD6`).
- Disabled: opacity 50%, `pointer-events: none`.
- Não transmitir status **só** por cor: chips têm label (“Aprovada”, “Pago”).
- Ícones decorativos: `aria-hidden` onde o código original marca.

---

## 11. CSS variables (copiar)

Ver implementação canônica em `frontend/src/WebApp.Blazor/wwwroot/css/app.css` — tokens Lumea dark/light, `--ring` / `--surface-tint`, `--glass-blur`, `--interactive-hover-bg`, `--ambient-shadow`, raios `--radius-lg` / `--radius-xl`.

Consumo: `hsl(var(--background))`.

### 11.1 Implementação Blazor (Fase 1.1)

| Artefato | Caminho |
|---|---|
| Tokens CSS | `frontend/src/WebApp.Blazor/wwwroot/css/app.css` |
| Bootstrap tema (sem flash) | `frontend/src/WebApp.Blazor/wwwroot/js/theme.js` — lê `localStorage.theme`, aplica classe `dark` em `<html>`, fallback `dark` |
| Serviço de tema | `frontend/src/WebApp.Blazor/Services/ThemeService.cs` |
| Toggle | `frontend/src/WebApp.Blazor/Components/Ui/ThemeToggle.razor` |
| Página de amostra | `/dev/tokens` → `Pages/Dev/Tokens.razor` |

Persistência: chave `localStorage.theme` com valores `light` | `dark`. Valores inválidos ou ausentes → `dark`.

### 11.2 Implementação Blazor (Fase 1.2)

| Artefato | Caminho |
|---|---|
| Google Fonts Montserrat 600–700 + Inter 300–800 | `frontend/src/WebApp.Blazor/wwwroot/index.html` |
| Tokens de fonte + escala tipográfica | `frontend/src/WebApp.Blazor/wwwroot/css/app.css` — classes `.text-page-title`, `.text-label`, `.text-stat-value`, `.text-table-money`, `.text-pix-key`, etc. |
| Utilitário tabular | `.tabular-nums { font-variant-numeric: tabular-nums; }` |
| Formatador monetário | `frontend/src/WebApp.Blazor/Formatting/MoneyFormatter.cs` — `FormatMoney(decimal)` → `R$ 1.234,56` (cultura `pt-BR` fixa) |
| Página de amostra | `/dev/typography` → `Pages/Dev/Typography.razor` |
| Testes | `frontend/tests/WebApp.Blazor.Tests/MoneyFormatterTests.cs`, `TypographyTests.cs` |

### 11.3 Implementação Blazor (Fase 1.3)

| Artefato | Caminho |
|---|---|
| Componentes base | `frontend/src/WebApp.Blazor/Components/Ui/` — `Button`, `StatusBadge`, `StatCard`, `Card`, `Input`, `Select`, `Tabs`, `Dialog`, `GoalToggle`, `Avatar`, `Spinner`, `EmptyState`, `ThemeToggle` |
| Enums / tipos | `frontend/src/WebApp.Blazor/Components/Ui/UiComponentTypes.cs` — `ButtonVariant`, `ButtonSize`, `StatusKind`, `SemanticTone`, `AvatarSize` |
| CSS semântico | `frontend/src/WebApp.Blazor/wwwroot/css/app.css` — classes `.ui-button`, `.ui-card`, `.status-badge`, `.ui-input`, `.ui-tabs`, `.ui-dialog`, `.goal-toggle`, `.ui-avatar`, `.ui-spinner`, `.empty-state` |
| Página de amostra | `/dev/kitchen` → `Pages/Dev/Kitchen.razor` |
| Testes | `frontend/tests/WebApp.Blazor.Tests/UiComponentTests.cs`, `KitchenTests.cs`, `VisualParity/VisualParityChecklistTests.cs` |

Contratos principais:

- **Button:** variants `Default`, `Outline`, `Ghost`, `Destructive`, `Secondary`; sizes `Default`, `Sm`, `Icon`; raio de controle (~8–10px), **não pill**; `AdditionalAttributes["class"]` é **mesclado** com as classes do variant (ex.: login `.auth-submit` + `ui-button--default`).
- **StatusBadge:** enum `StatusKind` com 10 variantes (folha + colaborador + NF); pill `rounded-full`; cores operacionais §2.5.
- **StatCard:** label `.text-label`, valor `.text-stat-value` tabular, ícone 32×32 com fundo semântico 15%.
- **GoalToggle:** trilho 32×16; on = emerald (`#10B981`), **não** primary; `role="switch"`.
- **Dialog:** overlay `black/80`, `role="dialog"`, `aria-modal="true"`; variante larga `.ui-dialog--wide` para formulários com muitos campos (Configurações — Níveis).

---

## 12. Mapa rápido para outras stacks

| Conceito FolhaPay | SwiftUI | Flutter | Compose |
|---|---|---|---|
| `background` | `Color(0xFF001718)` | `Color(0xFF001718)` | `Color(0xFF001718)` |
| `card` | `0xFF0A2324` | idem | idem |
| `primary` | `0xFF00F2EA` | idem | idem |
| `rounded-xl` | `RoundedRectangle(12)` | `BorderRadius.circular(12)` | `RoundedCornerShape(12.dp)` |
| `--radius` 10 | `10` | `10` | `10.dp` |
| Inter | `.custom("Inter")` | `google_fonts Inter` | `FontFamily` Inter |
| `tabular-nums` | `.monospacedDigit()` | `FontFeature.tabularFigures()` | `FontFeature.tabularFigures()` |
| Lucide 16 | SF Symbol / Lucide Swift | lucide_icons 16 | lucide 16.dp |
| Chip 15% | `.opacity(0.15)` no fill | `withValues(alpha: 0.15)` | `copy(alpha = 0.15f)` |
| Sidebar 256 | `NavigationSplitView` 256 | `NavigationRail`/`Drawer` 256 | `NavigationDrawer` 256.dp |
| Topbar blur | `ultraThinMaterial` | `BackdropFilter` | `RenderEffect` blur |

Componentes shadcn New York ≈ botões baixos (36px), ring fino, sem `rounded-full` em botão (só em chip/avatar).

---

## 13. Fazer / não fazer

**Fazer**

- Dark-first Lumea, cyan `#00DDD6` / `#00F2EA` como acento de marca.
- Cards 16px + glass (`card/72` + blur 12px) no dark.
- Status em pills translúcidas (nunca badge sólido primary para “aprovada”).
- Dinheiro com `tabular-nums` e locale pt-BR.
- Sidebar oceânica mais escura que o canvas.
- Nav/tabs ativos = borda cyan + fundo `primary-container/15–28%`.
- Hover neutro = borda + texto cyan (`--ring`).
- Toggle de meta = verde, não cyan.

**Não fazer**

- Magenta/rosa como acento de marca (legado).
- Sombra pesada estilo Material 2.
- Serif ou fontes display genéricas fora de Montserrat.
- Botões full-rounded (pills) para CTA — CTA é `rounded-md`.
- Ícones filled / duotone.
- Espaçamento generoso tipo marketing site; a UI é **densa**.
- Recolorir charts para paleta fria.

---

## 14. Checklist de paridade visual

Uma tela “parece CorePay” se:

1. Fundo `#001718`, cards `#0A2324`, borda `#3A4A48`.
2. Cyan `#00F2EA` / `#00DDD6` em CTA, hover e item de nav ativo.
3. Montserrat (títulos) + Inter (UI).
4. Stat cards com ícone 32px em fundo cor/15%.
5. Tabelas com thead `secondary` 40% e hover com borda cyan.
6. Status em pill, não em texto puro.
7. Valores R$ alinhados à direita, tabular.
8. Sidebar 256px oceânica, logo DollarSign no quadrado cyan gradient.
9. Raio 8px controles, 16px cards, 24px destaque, full em chips.
10. Densidade: 14px corpo, 12px labels, pouco whitespace além de 24px entre blocos.

---

## 15.3 Execução do checklist de paridade visual (Fase 15.3)

**Escopo:** Login, Dashboard, Folhas, Detalhe, Financeiro, Caixa, Tráfego, Settings — dark/light, desktop ≥1024px e mobile drawer. Marca **CorePay**.

**Papel de QA:** SuperAdmin (fixtures dev). Baselines: `/dev/tokens`, `/dev/typography`, `/dev/kitchen`.

**Validação automatizada (bUnit):** `VisualParityChecklistTests`, testes estendidos por tela (`LoginPageTests`, `ShellLayoutTests`, `DashboardSectionTests`, `PayrollsSectionTests`, `PayrollDetailSectionTests`, `FinancialSectionTests`, `CashflowSectionTests`, `TrafficInvestmentSectionTests`, `TrafficInvestmentFormDialogTests`, `TrafficWeekPanelTests`, `SettingsPageTests`, `UiComponentTests`).

**Validação manual complementar:** cores computadas (hex/HSL), contraste light e layout responsivo real (breakpoint 1024px) — bUnit não substitui inspeção visual.

### Matriz de auditoria

Legenda critérios §14: **1** tokens superfície · **2** cyan Lumea · **3** Montserrat+Inter · **4** stat cards · **5** thead/hover · **6** status pill · **7** R$ tabular · **8** sidebar/logo · **9** raios · **10** densidade · **—** não aplicável na tela.

| Tela | Rota | Critérios §14 | Dark desktop | Light desktop | Dark mobile | Light mobile | Observações |
|---|---|---|---|---|---|---|---|
| Login | `/login` | 1–3, 7–, 9–10 | OK | OK | OK | OK | Sem shell; hero 56×56 cyan gradient; CTA via `ui-button--default` + `.auth-submit` |
| Dashboard | `/` | 1–10 | OK | OK | OK | OK | `.dashboard-stats-grid--primary/secondary`, `.dashboard-recent-table`, stat cards |
| Folhas | `/payrolls` | 1–10 | OK | OK | OK | OK | `.payrolls-competence-header`, `StatusBadge`, `.text-table-money` |
| Detalhe | `/payrolls/{id}` | 1–10 | OK | OK | OK | OK | `.payroll-detail`, linha paga emerald, chips workflow |
| Financeiro | `/financial` | 1–10 | OK | OK | OK | OK | Tabs, stats, `.financial-table__row--paid`, progress emerald |
| Caixa | `/cashflow` | 1–10 | OK | OK | OK | OK | 4 summary cards, tabs, segmented entrada/saída, chips forma pagamento |
| Tráfego | `/traffic-investment` | 1–10 | OK | OK | OK | OK | Stats semânticos no dialog/semana; tabela com colunas monetárias |
| Settings | `/settings` | 1–10 | OK | OK | OK | OK | 4 seções Admin; `.settings-page`, `.ui-section-header`, tabelas densas |

**Shell transversal (telas autenticadas):** sidebar 256px glass (`.app-sidebar`), logo DollarSign (`.app-sidebar__logo-mark`), nav ativa cyan, drawer mobile (`.app-shell--mobile-open` + overlay), toggle tema (`ThemeToggle` + `ThemeService`).

### Correções aplicadas na 15.3

1. **`.settings-table thead tr`:** fundo `secondary/40` e hover de linha `accent/30` (critério §14.5).
2. **`Button`:** merge de `class` em `AdditionalAttributes` com variantes (`ui-button--default` preservado no login — `.auth-submit` não sobrescreve mais o estilo primário).

### Pronto quando

As oito telas passam nos dez critérios §14 aplicáveis; marca **CorePay** visível; drawer/tema funcionam; suíte frontend verde (`dotnet test CorePay.Frontend.slnx`) — **validado**.
