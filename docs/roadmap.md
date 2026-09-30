# Roadmap — CorePay

Guia sequencial para implementar **este repositório**. Não pule fases. Cada subfase tem o que construir, onde ler a regra e o critério de pronto.

Este produto é a reescrita do FolhaPay (legado Base44 / SPA JS). O comportamento de negócio vem de `docs/REGRAS_DE_NEGOCIO.md`; a stack, o RBAC e as decisões de arquitetura vêm de [`agents.md`](../agents.md). Não portar a sintaxe do legado — portar o **resultado numérico e o workflow**, na arquitetura já travada.

## Documentos obrigatórios

Leia **antes** de qualquer código:

| Arquivo | Uso |
|---|---|
| [`agents.md`](../agents.md) | Stack, pastas, RBAC, invariantes, endpoints, ordem resumida |
| [`REGRAS_DE_NEGOCIO.md`](./REGRAS_DE_NEGOCIO.md) | Fórmulas, workflow da folha, PIX, tráfego, caixa |
| [`IDENTIDADE_VISUAL.md`](./IDENTIDADE_VISUAL.md) | Tokens, shell, receitas de UI |
| [`design.md`](./design.md) | Contratos de API e rotas Blazor (criar na primeira API) |
| Este arquivo | Ordem detalhada de implementação |

O legado JS (`payrollCalc.js`, `base44/entities`, etc.) **não está neste repo**. Não procure esses arquivos. Extraia o comportamento das seções citadas em `REGRAS_DE_NEGOCIO.md`.

## Regras para a LLM executora

1. **Números do legado, arquitetura do CorePay.** Reproduza as fórmulas da §5–6 de `REGRAS_DE_NEGOCIO.md`. Implemente com a stack e os invariantes de `agents.md` (não com Base44, regex de nome, nem recálculo de folha aprovada).
2. **Cálculo no domínio, não na tela.** `PayrollCalculator` vive em `Core`, sem EF/HTTP/Blazor. A UI e as APIs só chamam o motor. Testes xUnit do motor existem **antes** das APIs e telas de folha.
3. **TDD.** Teste vermelho → implementação → docs da etapa. Sem os dois (testes verdes **e** documentação viva), a subfase não avança.
4. **Dark-first + Inter + magenta `#E92F8C`.** Siga `IDENTIDADE_VISUAL.md`. Marca na UI: **CorePay** / “Gestão de Pagamentos”. Não invente paleta.
5. **Uma subfase por vez.** Só avance se o “Pronto quando” da subfase atual passar.
6. **Não implemente Stripe/checkout.** Pagamento é PIX marcado manualmente (`Collaborator.PixKey`).
7. Locale: `pt-BR`, `R$ 1.234,56`, datas `dd/MM/yyyy`. Fuso `America/Bahia` (`BahiaTimeZone`).
8. **Perfil de cálculo é enum**, não regex. `Department.CalculationType` + `CareerLevel.Profile` (ou override no colaborador) selecionam `CalculationProfile`. O seed mapeia os nomes do legado para o enum. Regex **não** entra no motor de produção.
9. **Autorização por permissão**, nunca por e-mail. `rh@` / `trafego@` do legado viram `collaborators.*` e `traffic.*` no papel/usuário.
10. **Folha `approved`/`paid` trava valores.** Snapshot na aprovação. `GET` de detalhe **não recalcula**. Recálculo só em `draft`/`rejected` ou `POST .../recalculate` com `payrolls.write`.
11. **Percentuais padronizados:** no storage novo, `2.0m` = 2%. Gerência no legado usa decimal `0.012` = 1,2% — converter no seed, com teste de regressão (líquido 100000, factor 50, 2% → comissão 1000).
12. Documentação viva na **mesma entrega** (`agents.md`, `docs/design.md`, e o doc de domínio/visual se a mudança os tocar).
13. **IDs são sempre `Guid`.** PK, FK, `{id}` de rota e `id` de DTO usam UUID (`uniqueidentifier`). Nunca `int`/`long` IDENTITY sequencial. Identity gera UUID, não chave numérica. Ver `agents.md` §3.1.

## Ordem das fases

```
0 Fundação (.NET, SQL Server, Identity/JWT)
1 Identidade visual + componentes Blazor
2 RBAC, shell, papéis, rotas
3 Cadastros mestres (Settings)
4 Colaboradores
5 Motor de cálculo (núcleo)
6 Rateio por projeto
7 Folha — CRUD e formulário
8 Folha — workflow, snapshot, pagamento
9 Financeiro (PIX / NF / custo)
10 Faturamento
11 Investimento de tráfego
12 Fluxo de caixa + webhook
13 Dashboard + relatórios
14 Notificações
15 Paridade, exportação, endurecimento
```

Dependência dura: **5 antes de 7**. **6 antes de 9 e 13**. **3 e 4 antes de 7**.

Resumo alinhado a `agents.md` §6: 0–2 = fundação + RBAC + shell; 3–4 = mestres; 5–8 = calculadora + folha; 9–10 = financeiro; 11 = tráfego; 12 = caixa; 13–14 = dashboard/notificações.

---

# Fase 0 — Fundação do projeto

Objetivo: repositório compilando com a stack de `agents.md` §1 e §4, sem decisões implícitas.

### 0.1 Solução, pastas e Docker ✅

**Construir:**

- `docker-compose.yml` com SQL Server local.
- `backend/src`: `BuildingBlocks`, `Core`, `Infrastructure`, `WebAPI` (.NET 10, C# 14).
- `frontend/src/WebApp.Blazor` (Blazor WASM .NET 10).
- `GET /api/v1/health` anônimo.
- Auto-migrate no arranque (`MigrateAsync`).
- README curto: stack, `docker compose up`, como rodar a API e o Blazor.

Nomes de módulo alinhados ao domínio: `auth`, `users`, `departments`, `careerLevels`, `projects`, `collaborators`, `payroll`, `payrollCalc`, `traffic`, `cashflow`, `reports`, `notifications`.

**Pronto quando:** health responde; SQL sobe no Docker; README descreve a stack **deste** repo (não “escolha uma stack”).

### 0.2 Decisões já travadas (não reabrir)

Não criar `DECISIONS.md` paralelo. Confirmar na implementação o que `agents.md` já fixou:

| Tema | Decisão |
|---|---|
| Motor | `PayrollCalculator` em `Core`, mesmo processo da API, lib testável. Não calcular na UI. |
| Persistência | SQL Server + EF Core 10. Sem Base44. |
| Identificadores | Sempre `Guid` (`uniqueidentifier`). Proibido `int`/`long` IDENTITY. Rotas `{id}` e DTOs expõem UUID. Identity: UUID, não sequencial. |
| Auth | JWT + ASP.NET Core Identity. Email/senha. Sem self-signup. Sem OAuth Google na v1. |
| Autorização | Permissions + `RolePermission`. Manager: `UserDepartments`. Isolamento no **servidor** (403). |

**Pronto quando:** `Result<T>`, `BahiaTimeZone` e money/percentage em `BuildingBlocks`; JWT configurável (`Jwt:Key` não commitado).

### 0.3 Identity, SuperAdmin e fixture de teste ✅

**Construir:**

- `AppUser : IdentityUser` com `DisplayName` e `UserDepartments`.
- `IdentityDataSeeder`: role + usuário `SuperAdmin`; papéis de referência `Admin`, `Director`, `Financial`, `Manager`, `User` com o mapa de `RolePermission` de `agents.md` §2.
- `POST /api/v1/auth/login`.
- Fixtures nomeadas de forma estável (seed de teste / dados de desenvolvimento), já com **perfil explícito** (não regex):

| Fixture | Perfil / flag |
|---|---|
| Setor Tipster | `Tipster` |
| Tráfego Pago | `PaidTraffic` |
| Líderes de Projetos | `ProjectLeader` |
| Analistas Comerciais | `CommercialAnalyst` / supervisor via `CareerLevel.Profile` |
| Gerência | `Management` |
| Affiliates | `FixedCommissionBonus` |
| Administrativo, IA Automação, Contingência, Suporte | `AllocatedFixed` (+ Automação/Contingência roteiam fixo para Lima Karttos) |
| Níveis | Analista Comercial Júnior, Supervisor, Sênior (tráfego) |
| Projetos | Lima Karttos (alvo padrão de rateio), Feira X (sem bônus de meta), 3C Sports (supervisor sem rateio do fixo), Affiliates (custo 100% via ID explícito), um Lastlink, um Hubla |

**Pronto quando:** login SuperAdmin obtém JWT; fixtures carregam em teste sem UI; `UserName` = e-mail.

---

# Fase 1 — Identidade visual e design system

Fonte: `IDENTIDADE_VISUAL.md` inteiro. Checklist §14. Componentes em `frontend/.../Components/Ui/`.

### 1.1 Tokens ✅

Implementar light + dark (default **dark**) em CSS do Blazor. Primary `#E92F8C`. Variáveis da §11. Persistência de tema equivalente a `localStorage.theme`, fallback dark.

**Implementado:** tokens em `frontend/src/WebApp.Blazor/wwwroot/css/app.css`; bootstrap dark-first em `wwwroot/js/theme.js` (carregado em `index.html` antes do Blazor); `ThemeService` + `ThemeToggle`; amostra em `/dev/tokens`; testes bUnit em `ThemeTests.cs`.

**Pronto quando:** tela de amostra mostra background, card, primary, destructive, sidebar nos dois temas. Toggle persiste.

### 1.2 Tipografia e números ✅

Inter 300–800. Escala da §3. `tabular-nums` em dinheiro. Formatador `FormatMoney(n)` → `R$ 1.234,56`.

**Implementado:** Google Fonts Inter em `index.html`; tokens `--font-*` e classes tipográficas em `app.css`; `MoneyFormatter.FormatMoney` em `Formatting/MoneyFormatter.cs`; amostra em `/dev/typography`; testes em `MoneyFormatterTests.cs` e `TypographyTests.cs`.

**Pronto quando:** título 24/700, label 12 muted, valor 20/700 tabular renderizam iguais à spec.

### 1.3 Componentes base ✅

Na ordem, com receitas §7 (mapear shadcn → Razor, não copiar classes Tailwind à cegas):

1. Botão (default / outline / ghost / destructive / sm / icon)
2. Chip de status (`StatusBadge` — todos os status da tabela)
3. Stat card
4. Card de superfície (`rounded-xl` + borda 1px, sem sombra pesada)
5. Input denso (`bg-secondary`, não “material outlined”)
6. Select estilizado
7. Tabs
8. Dialog
9. Toggle de meta (verde 32×16, **não** primary)
10. Avatar de iniciais
11. Spinner (anel border-t primary)
12. Empty state (ícone opacity 40% + texto muted)

**Implementado:** 12 componentes em `frontend/src/WebApp.Blazor/Components/Ui/` (`Button`, `StatusBadge`, `StatCard`, `Card`, `Input`, `Select`, `Tabs`, `Dialog`, `GoalToggle`, `Avatar`, `Spinner`, `EmptyState`); enums em `UiComponentTypes.cs`; CSS semântico em `wwwroot/css/app.css`; vitrine `/dev/kitchen` → `Pages/Dev/Kitchen.razor`; testes bUnit em `UiComponentTests.cs` e `KitchenTests.cs`.

**Pronto quando:** página `/dev/kitchen` (ou equivalente) exibe todos. CTA não é pill; chip de status é pill.

### 1.4 Ícones ✅

Lucide (ou port 1:1 no Blazor). Tamanho 16px padrão, 20px topbar. Mapa da §5.

**Não fazer:** ícones filled, outra lib “parecida”.

**Implementado:** `InfiniLore.Lucide` registrado em `Program.cs`; wrapper `Icon` + enums `IconKind`/`IconSize` + `IconMetadata.cs` em `Components/Ui/`; estilos `.ui-icon*` em `app.css`; ícones aplicados em `ThemeToggle`, `StatCard`, `EmptyState`, `Dialog`; catálogo completo da §5 em `/dev/kitchen`; testes em `IconTests.cs`, `UiComponentTests.cs`, `KitchenTests.cs` e base `BlazorComponentTestContext.cs`.

**Pronto quando:** `/dev/kitchen` exibe catálogo Lucide linear (stroke, sem filled) nos tamanhos 16/20/28/32px; componentes base usam ícones reais em vez de placeholders Unicode.

---

# Fase 2 — RBAC, shell e navegação

Fonte: `agents.md` §2 e §4; `IDENTIDADE_VISUAL.md` §6 e §8.

### 2.1 APIs de admin ✅

CRUD versionado:

- `/api/v1/roles` — `roles.read` / `roles.write`
- `/api/v1/permissions` — `permissions.read` / `permissions.write`
- `/api/v1/users` — `users.read` / `users.write`

Cliente envia `displayName`. Role `SuperAdmin` não renomeável. SuperAdmin bypassa checks de permissão. IDs expostos em rotas são **GUID** (sem sequência).

**Implementado:** MediatR + `IAdminIdentityStore`; policies `permission:*` com bypass SuperAdmin; endpoints versionados; FK `UserDepartments → Departments`; testes em `backend/tests/WebAPI.Tests/Admin/`; contratos em `docs/design.md`.

**Pronto quando:** SuperAdmin cria um `Manager` com 2 setores (`UserDepartments`) e o GET do usuário relê os dois.

### 2.2 Auth no Blazor ✅

- Login email/senha → JWT no `HttpClient`.
- Logout.
- Sessão exigida em rotas autenticadas (`AuthorizeRouteView` + policies `permission:...`).
- Loading: spinner da identidade visual, texto “Carregando...”.
- Sem tela “não cadastrado” do Base44: usuário inexistente **não autentica**. Sem self-signup.

**Implementado:** `AuthService`, `CorePayAuthenticationStateProvider`, `AuthorizationMessageHandler`, `JsAuthSessionStorage` (`localStorage` key `corepay.auth.session`), policies dinâmicas `permission:*` com bypass SuperAdmin, `/login` (`EmptyLayout`), `LogoutButton`, init no startup; testes bUnit em `frontend/tests/WebApp.Blazor.Tests/Auth/`; contratos em `docs/design.md`.

**Pronto quando:** fluxo login → shell; logout; credencial inválida não entra.

### 2.3 Shell ✅

Sidebar 256px `#0A0A0A` (dark), logo DollarSign em quadrado magenta, marca “CorePay” / “Gestão de Pagamentos”. Topbar blur. Main padding 24px. Collapse desktop. Drawer mobile overlay `black/60`.

Nav ativa: fundo primary, texto branco, sombra primary 20%, chevron.

**Implementado:** `MainLayout` refatorado; `AppSidebar`, `AppHeader`, `SidebarTrigger` em `Components/Layout/`; `SidebarState` (collapse desktop + drawer mobile, sem persistência); catálogo `ShellNavigation`; CSS semântico em `app.css`; ícone `ChevronRight`; páginas stub autenticadas para todos os destinos do menu; testes bUnit em `Layout/ShellLayoutTests.cs` e `Layout/SidebarStateTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** layout bate o wireframe da §6 da identidade. Checklist §14 itens 1, 2, 8, 9.

### 2.4 Menu por permissão ✅

Itens visíveis só com a permissão correspondente. Rotas digitadas na URL também 403/redirect, não só ocultar o item. Manager **sem** `finance.read` / `reports.read` / `cashflow.read`.

| Rota Blazor | Permissão |
|---|---|
| `/` Dashboard | autenticado (widgets conforme permissões) |
| `/payrolls` | `payrolls.read` |
| `/collaborators` | `collaborators.read` |
| `/project-revenues` | `revenues.read` |
| `/traffic-investment` | `traffic.read` |
| `/financial` | `finance.read` |
| `/cashflow` (legado `/project-costs`) | `cashflow.read` |
| `/reports` | `reports.read` |
| `/settings` | cadastros mestres (`departments.write` etc., Admin no seed) |
| `/admin/users`, `/admin/roles` | `users.read` / `roles.read` |
| `/notifications` | autenticado |

**Implementado:** `AppPolicies` (constantes de policy); `AnyPermissionRequirement` + handler + prefixo `permission-any:` em `PermissionPolicyProvider` (Configurações = any-of `departments.write|careerlevels.write|projects.write|paymentmethods.write` — Director entra via `paymentmethods.write`); `ShellNavItem.Policy` em `ShellNavigation`; `AppSidebar` filtra itens via `IAuthorizationService` e oculta seção Administração quando vazia; páginas com `[Authorize(Policy = AppPolicies.*)]`; testes bUnit em `Auth/AnyPermissionPolicyTests.cs`, `Auth/RoutePermissionTests.cs`, `Layout/SidebarPermissionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** cada papel de referência vê só o seu menu; Manager **não** abre `/financial`; usuário com `traffic.read` vê Tráfego sem ser Admin.

### 2.5 Rotas da folha ✅

```
/payrolls
/payrolls/new
/payrolls/{id:guid}
/payrolls/{id:guid}/edit
```

404 autenticado dentro do shell (`NotFound` em português via `MainLayout`).

**Implementado:** páginas em `Pages/Payroll/` (`NewPayroll`, `PayrollDetail`, `PayrollEdit`); lista permanece em `Pages/Payrolls.razor`; policies `payrolls.read` / `payrolls.write` (`AppPolicies`); guarda compartilhada `PayrollResourceGuard` + `PayrollApiService` (`GET /api/v1/payrolls/{id}`) classifica 404 → `EmptyState` “Folha não encontrada”, 403 → `AccessDenied`; `ShellNavigation` resolve título “Folhas” em subrotas; constraint `{id:guid}` rejeita IDs inválidos (router → NotFound); testes bUnit em `Payroll/PayrollRouteTests.cs` e `Layout/PayrollShellNavigationTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** deep link em `{id}` inexistente mostra empty “Folha não encontrada” (id inexistente). Id existente fora do setor do manager → **403**, não 404 mascarado (`agents.md` §2). **Contrato frontend validado com HTTP mockado;** teste integrado ponta a ponta com API real e isolamento por setor entra na **Fase 7** (endpoint ainda não implementado).

---

# Fase 3 — Cadastros mestres (Configurações)

Fonte: `REGRAS_DE_NEGOCIO.md` §3; `agents.md` §3.4. APIs em `/api/v1/departments`, `/career-levels`, `/projects`, `/payment-methods`.

### 3.1 Entidades e enums ✅

Campos **completos** (paridade de dados com o legado, nomes em PascalCase no domínio):

- `Department`: `Name`, `CalculationType`, `GoalBonusPercentage`, `LowRevenueBonusPct`, `LowRevenueThreshold`, `Description`. Flags explícitas onde o legado usava nome (`IsAllocatedFixed`, roteio para Lima Karttos, Tipster via perfil).
- `CareerLevel`: **todos** os campos da §3.2 / cadastro de nível (comissões, FTD, vendas, supervisor, gerência, tráfego CPA por casa, `GoalBonusValue`, `Profile`, etc.).
- `Project`: `Name`, `Client`, `Platform` ∈ `lastlink|hubla`, `IsActive`, flags explícitas (alvo padrão de rateio, Feira sem bônus de meta, exclusão do fixo do supervisor).
- `PaymentMethod`: `Name`, `IsActive`.
- `AppUser`: roles + `UserDepartments[]`.

Enums rejeitam valor inválido no servidor. PK/FK de todas as entidades: `Guid` (`agents.md` §3.1).

**Implementado:** entidades em `Core/Domain/` (`CareerLevel` expandido, `PaymentMethod` novo, `Project.Client`, `TrafficHouse`); CQRS em `Core/Application/MasterData/` + `Infrastructure/MasterData/MasterDataStore.cs`; endpoints Minimal API (`DepartmentsEndpoints`, `CareerLevelsEndpoints`, `ProjectsEndpoints`, `PaymentMethodsEndpoints`); policies `permission:*` registradas para todas as keys de `AppPermissions.All`; enums JSON camelCase com rejeição de inteiros; migration `ExpandMasterDataEntities`; fixture Analista Júnior com `FtdRateBase=2`, `SalesPctBase=4`, `FtdBonusEvery=250`, `FtdBonusValue=350`; testes integração em `WebAPI.Tests/MasterData/`; contratos em `docs/design.md`.

**Pronto quando:** CRUD persistido; OpenAPI/Swagger lista os recursos; `design.md` atualizado.

### 3.2 Setores (UI) ✅

CRUD: nome, `CalculationType`, `% bônus de meta`, limiar e acréscimo de baixo faturamento (líder). Layout: card de seção com ícone 16px primary no header. Só `departments.write` (Admin no seed).

**Implementado:** `/settings` com hub de Configurações; seção Setores em `Components/Settings/` (`DepartmentsSection`, `DepartmentFormDialog`, `SectionCardHeader`) visível apenas com `AppPolicies.DepartmentsWrite`; `DepartmentApiService` + DTOs (`DepartmentContracts.cs`) consumindo `GET/POST/PUT /api/v1/departments`; labels pt-BR de `CalculationProfile` em `CalculationProfileLabels`; parsing pt-BR em `DecimalInputParser`; estilos `.ui-section-header`, `.settings-table`, `.settings-form` em `app.css`; testes bUnit em `Settings/DepartmentApiServiceTests.cs` e `Settings/DepartmentsSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** criar “Tipster” (`tipster`) e “Gerência” (`management`) via UI — validado com HTTP mockado (Admin); Director entra em `/settings` por `paymentmethods.write` mas **não** vê a seção Setores nem dispara GET de departments.

### 3.3 Níveis de carreira ✅

Formulário **contextual pelo `Profile` / `CalculationType`** (não pelo regex do nome):

- Sempre: salário/mínimo, % sem/com/super meta, R$/1% grupo, R$/20% VIP, R$/CPA.
- `CommercialAnalyst`: bloco FTD + vendas (todas as taxas da entidade).
- `CommercialSupervisor`: taxas `sup_*`.
- `Management`: `net_revenue_factor` (50 gerente / 100 diretora), `net_revenue_pct_*` armazenados como **por cento explícito** (1.2m = 1,2%).
- `PaidTraffic`: `% investimento`, CPA por casa (`TRAFFIC_HOUSES` como constantes C#), campos `traffic_sup_*` (cadastrar mesmo sem usar no cálculo — paridade com §13.1 das regras).

**Implementado:** seção Níveis em `Components/Settings/` (`CareerLevelsSection`, `CareerLevelFormDialog`) visível apenas com `AppPolicies.CareerLevelsWrite`; `CareerLevelApiService` + DTOs (`CareerLevelContracts.cs`) consumindo `GET/POST/PUT /api/v1/career-levels`; formulário contextual por perfil com `Dialog` largo (`IsWide`); sugestão de perfil a partir do setor (editável após escolha manual); labels CPA em `TrafficHouseLabels`; erros pt-BR em `CareerLevelErrorMessages`; testes bUnit em `Settings/CareerLevelApiServiceTests.cs` e `Settings/CareerLevelsSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** um Analista Comercial Júnior grava `FtdRateBase=2`, `SalesPctBase=4`, `FtdBonusEvery=250`, `FtdBonusValue=350` — validado com HTTP mockado (Admin); Director entra em `/settings` mas **não** vê a seção Níveis nem dispara GET de career-levels.

### 3.4 Projetos ✅

Nome, cliente, plataforma Lastlink/Hubla, ativo, flags de rateio. Chip ativo/inativo emerald/muted.

**Implementado:** seção Projetos em `Components/Settings/` (`ProjectsSection`, `ProjectFormDialog`) visível apenas com `AppPolicies.ProjectsWrite`; `ProjectApiService` + DTOs (`ProjectContracts.cs`) consumindo `GET/POST/PUT /api/v1/projects`; plataforma via enum `ProjectPlatform` (labels pt-BR em `ProjectPlatformLabels`); flags de rateio via `GoalToggle`; chip ativo/inativo emerald/muted; erros pt-BR em `ProjectErrorMessages`; testes bUnit em `Settings/ProjectApiServiceTests.cs` e `Settings/ProjectsSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** projeto Hubla e projeto “Lima Karttos” (flag de alvo padrão) existem — validado com HTTP mockado (Admin); Director entra em `/settings` mas **não** vê a seção Projetos nem dispara GET de projects.

### 3.5 Usuários (Settings / Admin) ✅

Admin lista usuários, edita roles e `department_ids` (`UserDepartments`). CRUD completo em `/admin/users` (não no hub `/settings`).

**Implementado:** página `/admin/users` com `UsersSection` + `UserFormDialog` em `Components/Admin/`; `UserApiService` + `RoleApiService` + DTOs (`UserContracts.cs`, `RoleContracts.cs`); policies `AppPolicies.UsersRead` / `UsersWrite`; multi-seleção de papéis (dinâmicos via API) e setores; senha só no create; DELETE com confirmação; proteção UI de SuperAdmin/autoexclusão; backend bloqueia atribuição de `SuperAdmin` por não-SuperAdmin (`users.superadmin_assignment_forbidden`); erros pt-BR em `UserErrorMessages`; testes integração em `WebAPI.Tests/Admin/UsersEndpointTests.cs`; testes bUnit em `Admin/UserApiServiceTests.cs` e `Admin/UsersSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** promover um user a Manager com 2 setores persiste — validado com HTTP mockado (Admin) e integração backend (Fase 2.1); usuário read-only vê lista sem ações de mutação.

---

# Fase 4 — Colaboradores

Fonte: `REGRAS_DE_NEGOCIO.md` §3.3; permissões `collaborators.read|write`. Isolamento: Manager só `UserDepartments`.

### 4.1 Lista ✅

Filtro por setor, busca, ativo/inativo. Tabela: avatar, nome+cargo, nível, admissão, demissão em `red-400` se inativo, PIX mono xs, salário tabular, chip Ativo/Inativo. “Editar” no hover da linha.

**Implementado:** entidade `Collaborator` + migration; CQRS/store em `Core/Application/Collaborators/` + `Infrastructure/Collaborators/CollaboratorStore.cs`; endpoints `GET /api/v1/collaborators?departmentId=&search=&isActive=` e `GET /api/v1/collaborators/{id}` com isolamento Manager via `UserDepartments` (403 `collaborators.department_forbidden` fora do escopo); fixtures Ana Comercial (ativo) e Bruno Tráfego (inativo) no `DevelopmentFixtureSeeder`; página `/collaborators` com `CollaboratorsSection`; `CollaboratorApiService` + DTOs; filtros server-side; botão “Editar” no hover/focus gated por `AppPolicies.CollaboratorsWrite`; erros pt-BR em `CollaboratorErrorMessages`; datas via `DateFormatter`; testes integração em `WebAPI.Tests/Collaborators/CollaboratorsEndpointTests.cs`; testes bUnit em `Collaborators/CollaboratorApiServiceTests.cs` e `Collaborators/CollaboratorsSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** gerente A não vê colaborador do setor B (API 403 em GET por id fora do escopo; lista filtrada) — validado com testes integração backend e HTTP mockado no frontend.

### 4.2 CRUD ✅

Campos: nome, setor, nível, cargo, admissão, demissão, PIX, salário, e-mail, foto, `IsActive`, opcional override de `CalculationProfile`. Inativar exige `DismissalDate` explícita; reativar limpa a demissão.

Data de demissão do **cadastro** entra no proporcional — nunca snapshot contaminado por mudança de cargo.

**Implementado:** `POST/PUT /api/v1/collaborators` + comandos/handlers/store com validações (`name`/`department` obrigatórios, FKs, nível compatível com setor, salário ≥ 0, datas coerentes, `dismissal_date_required` ao inativar); `calculationProfileOverride` exposto em todas as respostas; isolamento Manager em create/update/troca de setor; `CollaboratorFormDialog` (dialog largo) + botão “Novo colaborador”; `CollaboratorApiService` create/update; foto via URL; níveis filtrados por setor; GET `{id}` antes de editar; erros pt-BR em `CollaboratorErrorMessages`; `DateInputParser`; testes integração em `WebAPI.Tests/Collaborators/CollaboratorsEndpointTests.cs`; testes bUnit em `Collaborators/CollaboratorApiServiceTests.cs`, `Collaborators/CollaboratorsSectionTests.cs` e `Collaborators/CollaboratorFormDialogTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** colaborador inativo com demissão 15/03 aparece na lista como Inativo e a data de demissão visível — validado com testes integração backend e HTTP mockado no frontend.

---

# Fase 5 — Motor de cálculo (núcleo)

Fonte: `REGRAS_DE_NEGOCIO.md` §5–6. Código: `PayrollCalculator` em `Core`.

**Não construir UI nesta fase.** Apenas funções puras + testes xUnit.

Ordem de `CalcEntry` (obrigatória, agora por **perfil explícito**):

1. Sem departamento → comissão 0, total 0
2. Se `role_changes` com `change_date` → §5.2
3. Se perfil `PaidTraffic` → §6.3
4. Se perfil `CommercialSupervisor` → §6.2
5. Se perfil `CommercialAnalyst` → §6.1
6. Se perfil `Management` → §6.4
7. Se perfil `ProjectLeader` (`fixed_commission_super_goal`) → §6.5
8. Se `CommissionOnly` / `FixedCommission` / `FixedCommissionBonus` → §6.6–6.8
9. Senão `FixedBonus` / `Tipster` / `AllocatedFixed` → §6.9

O seed mapeia os nomes da fixture 0.3 para esses perfis. Testes **não** dependem de regex no motor.

### 5.1 Helpers de perfil e casas ✅

Portar o **efeito** da §11, como dados explícitos:

- `CommercialAnalyst`, `CommercialSupervisor`, `ProjectLeader`, `PaidTraffic`, `Tipster`, `AllocatedFixed`
- Casas `TRAFFIC_HOUSES` + `GetTrafficCpaRate` (tipo `supervised` vs `manager` → taxa Sênior)
- Projetos: Feira, Lima Karttos, 3C Sports, Affiliates via flags/ids do seed — não `/feira/i` em produção

**Implementado:** helpers puros em `Core/Domain/PayrollCalculation/` (`CalculationProfileResolver`, `TrafficCpaRateHelper`, `TrafficCpaKind`, `ProjectCalculationFlags`, `DepartmentCalculationFlags`); constantes `TrafficHouse.All`; projeto de testes `Core.Tests` (39 testes unitários); fixture Affiliates em `SeedKeys.Projects.Affiliates` + `DevelopmentFixtureSeeder`; testes integração em `WebAPI.Tests/Seed/PayrollCalculationFixturesTests.cs` e extensão de `DevelopmentFixtureSeederTests.cs`.

**Pronto quando:** testes escolhem o perfil certo para cada fixture da 0.3 **sem** inspecionar string de nome no calculator — validado com testes unitários e integração (98 testes backend verdes).

### 5.2 Proporcional ✅

`ProportionalFactor(admissão, demissão, mês, ano)` — §5.1.

Casos de teste mínimos:

| Admissão | Demissão | Mês ref | Fator |
|---|---|---|---|
| vazia | vazia | qualquer | 1 |
| 01/03 | — | março 31d | 1 |
| 16/03 | — | março 31d | 16/31 |
| — | 10/03 | março | 10/31 |
| 05/03 | 10/03 | março | 6/31 |
| — | 28/02 | março | **0** |
| 10/02 | — | março | 1 (admissão fora do mês) |

Demissão no recálculo: **sempre** `Collaborator.DismissalDate`, nunca snapshot de role_change.

**Implementado:** helper puro `ProportionalFactor.Calculate` em `Core/Domain/PayrollCalculation/`; contagem inclusiva de dias civis no mês de referência; retorno `decimal` fração 0–1 (sem arredondamento no fator); 7 testes unitários em `Core.Tests/PayrollCalculation/ProportionalFactorTests.cs`.

**Pronto quando:** os 7 casos passam — validado com testes unitários (46 testes Core.Tests verdes).

### 5.3 Comissão pura, fixo+comissão, Affiliates ✅

§6.6–6.8. Incluir `final_salary > 0` → total = final + bônus − descontos (não recalcula fixo).

**Implementado:** `PayrollCalculator.CalcEntry` + calculadores puros §6.6–6.8 em `Core/Domain/PayrollCalculation/` (`CommissionOnlyCalculator`, `FixedCommissionCalculator`, `FixedCommissionBonusCalculator`); contratos `PayrollEntryInput`/`PayrollEntryResult`; helpers `BaseSalaryResolver`, `CommissionPercentSelector`, `CommissionRevenueCalculator`, `ManualAdjustments`; bônus manuais nos três perfis (§5.3); `FinalSalary > 0` trava total Affiliates; `GoalBonusValue` quando meta atingida; perfis não implementados lançam `NotSupportedException`. Testes em `Core.Tests/PayrollCalculation/CommissionOnlyCalculatorTests.cs`, `FixedCommissionCalculatorTests.cs`, `FixedCommissionBonusCalculatorTests.cs`, `PayrollCalculatorCommissionProfilesTests.cs`.

**Pronto quando:** teste Affiliates com `goal_reached` soma `goal_bonus_value`; com `final_salary` ignora base — validado com 22 testes novos em Core.Tests (68 total Core.Tests, 185 backend verdes).

### 5.4 Fallback + Tipster + rateado + Feira + Lima Karttos ✅

§6.9:

- Bônus de meta % sobre a base correta.
- Projeto Feira **não** entra na base do bônus; no rateio só leva parcela do fixo.
- Tipster: `group_percentage` × R$/1% + `floor(%/20)` × R$/20%.
- `rateio_value` manual reserva valor absoluto; saldo divide-se igualmente entre projetos sem valor.
- Automação/Contingência → 100% Lima Karttos.

**Implementado:** `FixedBonusSectionCalculator` + helpers `FixedAllocationCalculator`, `GoalBonusCalculator`, `TipsterGroupCommissionCalculator`; contratos ampliados (`ProjectCalculationSnapshot`, `RateioProjectEntryInput`, `GroupPercentage`, `GoalBonusAmount`, `GroupCommissionAmount`); dispatch de `FixedBonus`/`Tipster`/`AllocatedFixed` em `PayrollCalculator`. Testes em `FixedAllocationCalculatorTests`, `GoalBonusCalculatorTests`, `TipsterGroupCommissionCalculatorTests`, `FixedBonusSectionCalculatorTests`, `PayrollCalculatorFallbackProfilesTests`.

**Pronto quando:** Administrativo com 2 projetos (um Feira) bate meta: bônus só no não-Feira — validado (fixo R$ 3.000, meta 10%, total R$ 3.150; 92 testes Core.Tests, 209 backend verdes). Breakdown público de custo por projeto permanece na Fase 6.

### 5.5 Líder de Projetos ✅

§6.5: líquido = fat × 0,8; % teto 2,1; limiar default 200000; acréscimo default 0,4; acréscimo **por projeto**.

**Implementado:** `GoalTier` (`None`/`Goal`/`SuperGoal`) em `PayrollEntryInput` (substitui `GoalReached`); `ProjectLeaderCalculator` + helpers `ProjectLeaderCommissionPercentSelector`, `ProjectLeaderCommissionCalculator`; dispatch de `ProjectLeader` em `PayrollCalculator`; perfis binários 5.3/5.4 migrados para `GoalTier`. Testes em `ProjectLeaderCommissionPercentSelectorTests`, `ProjectLeaderCommissionCalculatorTests`, `ProjectLeaderCalculatorTests`, `PayrollCalculatorProjectLeaderTests`.

**Pronto quando:** fat 100000 sem meta 1% → comissão = 80000 × min(1+0.4, 2.1)% ; fat 300000 não soma acréscimo — validado (18 testes ProjectLeader, 110 Core.Tests, 227 backend verdes). Breakdown por projeto permanece na Fase 6.

### 5.6 Gerência ✅

§6.4 com convenção CorePay: `comissão = líquido × (factor/100) × (pct/100)` se `pct` estiver em por cento explícito **ou** o equivalente testado na regressão abaixo. Não aplicar divisão dupla.

**Implementado:** `ManagementRevenueEntryInput` + `ManagementRevenueEntries` em `PayrollEntryInput`; `ManagementCalculator` + helpers `ManagementCommissionPercentSelector`, `ManagementCommissionCalculator`; dispatch de `Management` em `PayrollCalculator` (antes de `ProjectLeader`); meta global por linha via `GoalTier`; fixo proporcional + bônus/descontos manuais. Testes em `ManagementCommissionPercentSelectorTests`, `ManagementCommissionCalculatorTests`, `ManagementCalculatorTests`, `PayrollCalculatorManagementTests`.

**Pronto quando:** líquido 100000, factor 50, 2% → comissão 1000 (mesmo resultado do legado com `pct = 0.02`) — validado (19 testes Management, 129 Core.Tests, 246 backend verdes). Breakdown por projeto permanece na Fase 6.

### 5.7 Tráfego Pago ✅

§6.3. CPA `supervised` vs `manager` (manager usa nível Sênior via snapshot `TrafficSeniorLevel`). Legado `betting_house`+`cpa_count` → `TrafficCpaEntryInput`. Fixo rateado **igualitariamente** em `rateio_project_entries` (sem `rateio_value` manual). **Não** aplicar `traffic_sup_bonus` no total (paridade §13.1).

**Implementado:** contratos `TrafficProjectEntryInput`, `TrafficCpaEntryInput`, `TrafficSeniorLevel` em `PayrollEntryInput`; `PaidTrafficCalculator` + helpers `TrafficProjectCommissionCalculator`, `TrafficFixedAllocationCalculator`; dispatch `PaidTraffic` em `PayrollCalculator`; `CalcEntry` retorna `Result<PayrollEntryResult>`; perfis não implementados → `payroll.profile_not_implemented`; fixture Sênior com `TrafficInvestmentCommissionPct=2`, `TrafficCpaBetano=50`. Testes em `TrafficProjectCommissionCalculatorTests`, `TrafficFixedAllocationCalculatorTests`, `PaidTrafficCalculatorTests`, `PayrollCalculatorPaidTrafficTests`.

**Pronto quando:** investido 10000 @ 2% + 3 CPA Betano @ 50 = 200 + 150; supervisor com CPA tipo manager usa taxa sênior — validado (19 testes novos Core.Tests, 148 Core.Tests, 266 backend verdes). Breakdown por projeto permanece na Fase 6.

### 5.8 Supervisor comercial ✅

§6.2. FTD demais = `max(0, ftd_total - ftd_superbet)`. Recarga + bonus_cpa. `supervisor_rev_analista` dividido por **todos** os projetos. Fixo **não** vai para o projeto 3C Sports (flag).

**Implementado:** contratos `SupervisorProjectEntryInput`, `SupervisorAnalystRevenue`, `SupervisorProjectEntries` e `ProjectCalculationSnapshot.ExcludesSupervisorFixedAllocation` em `PayrollEntryInput`; `CommercialSupervisorCalculator` + helpers `CommercialSupervisorRateSelector`, `CommercialSupervisorProjectCommissionCalculator`, `SupervisorFixedAllocationCalculator`, `SupervisorRevAnalistaAllocationCalculator`; dispatch `CommercialSupervisor` em `PayrollCalculator` (após `PaidTraffic`). Metas FTD/vendas **por projeto**; arredondamento monetário por componente; `CommissionAmount` = comissões automáticas + Rev analista (bônus manual só no total). Testes em `CommercialSupervisorRateSelectorTests`, `CommercialSupervisorProjectCommissionCalculatorTests`, `SupervisorFixedAllocationCalculatorTests`, `SupervisorRevAnalistaAllocationCalculatorTests`, `CommercialSupervisorCalculatorTests`, `PayrollCalculatorCommercialSupervisorTests`.

**Pronto quando:** 1 projeto 3C Sports + 1 outro → fixo só no outro; Rev analista nos dois — validado (30 testes novos Core.Tests, 178 Core.Tests, 296 backend verdes). Breakdown por projeto permanece na Fase 6.

### 5.9 Analista Comercial ✅

§6.1 e §5.4. Implementar na ordem:

1. `CalcCommercialProject` (FTD iGaming vs Superbet, 0/1/2 metas, bônus FTD por projeto, CPA, % vendas 4/5/6, bônus vendas a cada 20k, rev %).
2. Betano Interna R$200 (conta mínimo); Mundo Bet R$70 (não conta).
3. Bônus FTD **combinado** na soma dos projetos; pagar só o extra vs soma dos bônus por projeto.
4. Mínimo: compara só comissão (projetos + Betano Interna + extra FTD). Bônus manual **fora**.
5. Complemento = max(0, mínimo×prorata − comissão).
6. Total = comissão + bônus + complemento + (se complemento: platformTotal) + Betano Mundo Bet − descontos.
7. Hubla: % plataforma = min(base, 4). Lastlink: `% base`. Plataforma usa sempre % **base**, não % com meta.

**Implementado:** `CommercialAnalystCalculator` + helpers `CommercialAnalystRateSelector`, `CommercialAnalystProjectCommissionCalculator`, `CommercialAnalystCombinedFtdBonusCalculator`, `CommercialAnalystPlatformCalculator`; contratos `CommercialAnalystProjectEntryInput`, `CommercialProjectEntries`, `BetanoInternaCount`, `BetanoMundoBetCount` em `PayrollEntryInput`; `PlatformTotal` em `PayrollEntryResult`; dispatch `CommercialAnalyst` em `PayrollCalculator` (após Supervisor). Testes em `CommercialAnalystRateSelectorTests`, `CommercialAnalystProjectCommissionCalculatorTests`, `CommercialAnalystCombinedFtdBonusCalculatorTests`, `CommercialAnalystPlatformCalculatorTests`, `CommercialAnalystCalculatorTests`, `PayrollCalculatorCommercialAnalystTests`.

**Pronto quando:** todos os testes desta subfase passam — validado (34 testes novos Core.Tests, 212 Core.Tests, 330 backend verdes). Breakdown por projeto permanece na Fase 6.

### 5.10 Mudança de cargo ✅

§5.2. Período principal até `dayBefore(primeira change_date)`. Bônus/desconto só no consolidado. Restaurar `dismissal_date` real no retorno. Cada período usa o **perfil do cargo daquele intervalo**.

**Implementado:** `RoleChangePayrollCalculator` + helpers `RoleChangePeriodSplitter`, `PayrollRoleSnapshot`, `RoleChangeEntryInput`, `PayrollEntryResultMerger`; contratos `RoleChanges`, `PeriodClipStart`/`PeriodClipEnd` em `PayrollEntryInput`; `ProportionalFactor.CalculateForEntry` com recorte por sub-período; `PayrollCalculator.CalcEntry` despacha §5.2 antes do perfil via `CalcEntryCore` (sem recursão). Sub-períodos sem bônus/desconto; consolidação soma todos os campos e aplica ajustes manuais uma vez; `Collaborator.DismissalDate` nunca mutado. Testes em `RoleChangePeriodSplitterTests`, `PayrollEntryResultMergerTests`, `PayrollCalculatorRoleChangeTests`, `ProportionalFactorTests` (clip).

**Pronto quando:** admissão 01/03, mudança 16/03 para outro setor, 31 dias → dois períodos 15/31 e 16/31; desconto 100 subtrai uma vez — validado (16 testes novos Core.Tests, 228 Core.Tests, 346 backend verdes).

### 5.11 Recálculo em lote ✅

`RecalcAllEntries`. Total da folha = soma dos `total_amount`.

**Implementado:** `PayrollCalculator.RecalcAllEntries` + `PayrollBatchResult` em `Core/Domain/PayrollCalculation/`; loop fail-fast sobre `CalcEntry`; `TotalAmount` agregado = Σ `PayrollEntryResult.TotalAmount`; lista vazia → total 0; `AddSingleton<PayrollCalculator>()` em `Core/DependencyInjection.cs`. Testes em `PayrollCalculatorRecalcAllEntriesTests`, `PayrollCalculatorDiTests`.

**Pronto quando:** 3 colaboradores mistos (comercial + fixo + tráfego) somam certo; `PayrollCalculator` registrado no DI sem DbContext — validado (5 testes novos Core.Tests, 1 WebAPI.Tests, 352 backend verdes).

---

# Fase 6 — Rateio / custo por projeto

Fonte: `REGRAS_DE_NEGOCIO.md` §6 (blocos de custo). Funções puras no mesmo assembly do motor.

### 6.1 `CalcProjectTotalsForEntry` ✅

Portar ramo a ramo: management (só breakdown), tráfego (comissão + rateio do fixo), Tipster (rateio list), Lima Karttos (automação/contingência), rateado+Feira, líder, supervisor (exceto 3C no fixo), comercial (abate plataforma; &lt; R$100 → projeto pagador; complemento/Betano/plataforma → complement_paying_projects ou pagador ou Lima Karttos). Somar `bonus_entries` no projeto informado. Recursão em `role_changes`.

**Implementado:** `PayrollCalculator.CalcProjectTotalsForEntry` + calculadores por perfil em `Core/Domain/PayrollCalculation/` (`CommercialAnalystProjectTotalsCalculator`, `CommercialSupervisorProjectTotalsCalculator`, `PaidTrafficProjectTotalsCalculator`, `ManagementProjectTotalsCalculator`, `ProjectLeaderProjectTotalsCalculator`, `FixedBonusSectionProjectTotalsCalculator`); helpers `ProjectTotalsAccumulator`, `ProjectTotalsMerger`, `ComplementAllocationCalculator`, `GoalBonusAllocationCalculator`, `TipsterGroupCommissionAllocationCalculator`, `RoleChangeProjectTotalsCalculator`; contratos `ProjectTotalAllocation`, `CommissionPayingProjectId`, `ComplementPayingProjects`, `ManagementProjectBreakdownInput`; `TrafficProjectCommissionCalculator.CalculateProject` e `ProjectLeaderCommissionCalculator.CalculateProject` extraídos. Testes em `PayrollCalculatorProjectTotalsTests`, `ProjectTotalsMergerTests`.

**Pronto quando:**

- Comercial comissão projeto 80 e pagador definido → 80 no pagador, 0 no original — validado.
- Complemento 500 sem lista de % → pagador único ou Lima Karttos — validado.
- `complement_paying_projects` 60/40 soma 100 → split proporcional — validado.
- Contingência → 100% Lima Karttos — validado (366 testes backend verdes).

### 6.2 Display helpers ✅

Affiliates → um projeto “Affiliates”. Contingência / IA Automação → Lima Karttos. Gerência com breakdown → agrega breakdown.

**Implementado:** `PayrollCalculator.GetDisplayProjectEntries` + `ProjectDisplayHelper` em `Core/Domain/PayrollCalculation/`; Affiliates via `affiliatesProjectId` explícito (reconciliação de custo); Automação/Contingência colapsados em Lima Karttos; Gerência com `AggregateManagementBreakdown`; passthrough dos demais perfis; role change por período + merge. Testes em `GetDisplayProjectEntriesTests`.

**Pronto quando:** testes de `GetDisplayProjectEntries` passam — validado (11 testes novos Core.Tests, 377 backend verdes).

---

# Fase 7 — Folha: lista, detalhe e formulário

Fonte: `REGRAS_DE_NEGOCIO.md` §4; `IDENTIDADE_VISUAL.md` §8; `agents.md` §3.3. Unique index: setor + mês + ano.

### 7.1 Modelo transacional ✅

`Payroll` + `collaborator_entries` aninhados: `project_entries`, `rateio_project_entries`, `bonus_entries`, `deduction_entries`, `role_changes`, `complement_paying_projects`, flags `IsApproved|IsPaid|NfSent`, `FinalSalary`, Betano, etc.

**Implementado:** entidades em `Core/Domain/` (`Payroll`, `PayrollCollaboratorEntry`, `PayrollStatus`, `PayrollCollaboratorEntryPayload`, `PayrollRoleChangeEntry`, `PayrollRoleChangeSnapshot`); blocos variáveis em coluna JSON `Payload` reutilizando records de `PayrollCalculation`; cabeçalho relacional com índice único `(DepartmentId, Month, Year)`; store `IPayrollStore`/`PayrollStore` em `Core/Application/Payrolls/` + `Infrastructure/Payrolls/`; serialização centralizada `PayrollJsonOptions`; migration `AddPayrollTransactions`; validação `payrolls.competence_duplicate`; testes integração em `WebAPI.Tests/Payroll/PayrollPersistenceTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** uma folha com 1 colaborador, 2 projetos, 1 bônus, 1 desconto, 1 role_change serializa e relê sem perda de campo (teste de integração) — validado (379 testes backend verdes).

### 7.2 Lista ✅

Filtros: busca, mês, ano, status, setor (setor oculto para manager). Agrupar por competência. Card por mês (`bg-secondary/40` + ícone Calendar). StatusBadge. Duplicar folha: copia entradas **zerando** `IsApproved`, `IsPaid`, `NfSent`.

**Implementado:** `GET /api/v1/payrolls` + `GET /api/v1/payrolls/{id}` + `POST /api/v1/payrolls/{id}/duplicate` (competência destino = **mês seguinte** automático); CQRS em `Core/Application/Payrolls/`; store estendido (`GetPayrollsAsync`, `GetSummaryByIdAsync`, `DuplicateToNextMonthAsync`); isolamento Manager via `CollaboratorAccessResolver` + `UserDepartments`; página `/payrolls` com `PayrollsSection` (filtros server-side, agrupamento `Jan/2026`, CTA Nova Folha, duplicar); `PayrollApiService`, `CompetenceFormatter`, `PayrollStatusMapper`, `PayrollErrorMessages`; CSS `.payrolls-*`; testes integração em `WebAPI.Tests/Payroll/PayrollsEndpointTests.cs`; testes bUnit em `Payroll/PayrollApiServiceTests.cs` e `Payroll/PayrollsSectionTests.cs`; contratos em `docs/design.md`.

**Pronto quando:** duplicar uma folha paga gera `draft` com flags falsos e mesmos valores — validado com testes integração backend e HTTP mockado no frontend.

### 7.3 Nova / editar — casca ✅

Campos: setor (manager: só os seus; 1 setor → pré-selecionado), mês, ano. Adicionar colaboradores **do setor** ainda não na folha. Snapshot: nome, nível, PIX, `FullBaseSalary`, admissão, **perfil de cálculo**.

Editar só se status `draft` ou `rejected` e `payrolls.write`. Setor/mês/ano **imutáveis** após criação.

Rejeitada + manager: **esconder** colaboradores com `IsApproved`.

**Implementado:** `GET /api/v1/payrolls/form-options`; `POST /api/v1/payrolls`; `PUT /api/v1/payrolls/{id}` (sync `collaboratorIds`); `GET /api/v1/payrolls/{id}` enriquecido com `entries[]`; `PayrollEntrySnapshotBuilder` + `CalculationProfileResolver`; guards de status/escopo/colaborador ativo; preservação de entradas aprovadas ocultas ao Manager em folha `rejected`; UI `/payrolls/new` e `/payrolls/{id}/edit` com `PayrollFormShell`, `PayrollHeaderFields`, `PayrollCollaboratorPicker`, `PayrollEntryShellRow`, `PayrollEditabilityGuard`; `PayrollApiService` estendido; CSS `.payroll-form*`; testes integração em `WebAPI.Tests/Payroll/PayrollsEndpointTests.cs`; bUnit em `PayrollFormShell`/`PayrollEditabilityGuard`/`PayrollApiService`/`PayrollRouteTests`; contratos em `docs/design.md`.

**Pronto quando:** manager não adiciona colaborador de outro setor; não edita `pending_approval` — validado (21 testes integração folha + 48 bUnit payroll).

### 7.4 UI por perfil (espelhar o motor) ✅

Cada linha de colaborador, conforme `CalculationProfile` (11 perfis):

- Comercial: projetos com FTD, Superbet, metas, vendas, plataforma, CPA, Rev; Betano no colaborador; projeto pagador &lt; R$100; complement_paying_projects (soma % = 100, UI vermelha se ≠ 100).
- Supervisor: FTD, vendas, metas de **projeto**, recarga, bonus_cpa, `supervisor_rev_analista`.
- Tráfego: valor investido + `cpa_entries` (casa, qtd, tipo supervised/manager) + `rateio_project_entries`.
- Gerência: registros de faturamento líquido + toggle meta + breakdown manual.
- Líder: faturamento + meta + super meta.
- Tipster: valores + `group_percentage` + rateio separado.
- Rateado: projetos + `rateio_value` opcional + toggle meta do colaborador.
- Affiliates: `final_salary` opcional.
- Comissão pura / fixo+comissão: faturamento por projeto + meta quando aplicável.

Blocos: nested `secondary/20`. Toggle meta verde. Total do bloco em `primary` bold xs.

Bônus manuais (projeto + valor + justificativa). Descontos (descrição + valor + justificativa). Role changes (data, novo setor/nível/perfil, projetos do período).

**Implementado:** `POST /api/v1/payrolls/{id}/entries/{entryId}/preview` (sem persistência); `PayrollEntryInputMapper`; `GET /api/v1/payrolls/{id}` com payload + `editorOptions`; UI `/payrolls/{id}/edit` com `PayrollEntryEditor` + blocos em `Components/Payroll/Blocks/`; debounce 400ms; hint Hubla 4%; CSS `.payroll-nested-block*`; testes integração + bUnit.

**Pronto quando:** um comercial com 2 projetos atualiza total ao vivo; Hubla mostra teto 4% na UI — validado.

### 7.5 Salvar e submeter ✅

Salvar: `RecalcAllEntries` → persistir → `TotalAmount` da folha.  
Submeter: status `pending_approval`, `SubmittedBy` = nome do usuário. Notificação `payroll_submitted` implementada na Fase 14.1.

**Implementado:** `PUT /api/v1/payrolls/{id}` estendido com `entries[]` em lote (sync colaboradores + persistência atômica + recálculo); `POST /api/v1/payrolls/{id}/submit`; `PayrollEntryInputMapper.ApplyRequest`/`ToPreviewRequest`; `PayrollAccessContext.DisplayName`; UI `PayrollFormShell` com botões “Salvar” e “Salvar e submeter” + total da folha; testes integração `PayrollsEndpointTests` + bUnit `PayrollFormShellTests`/`PayrollApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** reabrir a folha `draft` mostra os mesmos totais — validado.

### 7.6 Detalhe (leitura) ✅

`max-w-3xl`. Header: setor, competência, StatusBadge, submetido por. Lista colaboradores com totais, PIX, chips aprovado/pago. Expansão com breakdown.

**Diferença deliberada vs legado §13.2:** se `approved` ou `paid`, **não recalcular** — ler o snapshot. Draft/rejected podem recalcular.

Manager: 403 se `department_id` não é dele.

**Implementado:** `GET /api/v1/payrolls/{id}` enriquecido com `entries[].isPaid`, `nfSent`, `result`, `projectTotals`; snapshot mínimo `CalculatedResult` + `DisplayProjectTotals` em `PayrollCollaboratorEntryPayload` (persistido no save/submit); UI `/payrolls/{id}` com `PayrollDetailSection`, `PayrollDetailEntryRow`, `PayrollEntryBreakdownPanel`; botão “Editar” gated por `payrolls.write` + status editável; CSS `.payroll-detail*`; testes integração `PayrollsEndpointTests` + bUnit `PayrollDetailSectionTests`/`PayrollRouteTests`; contratos em `docs/design.md`.

**Pronto quando:** gerente de outro setor não abre a folha; folha aprovada relida com os valores gravados mesmo se o nível de carreira mudar depois — validado.

---

# Fase 8 — Workflow de aprovação e pagamento na folha

Fonte: `REGRAS_DE_NEGOCIO.md` §4; `agents.md` §3.3.

### 8.1 Permissões na tela e na API

| Ação | Permissão | Condição de status |
|---|---|---|
| Aprovar folha | `payrolls.approve` | `pending_approval` |
| Reprovar (comentário obrigatório) | `payrolls.approve` | `pending_approval` |
| Aprovar colaborador (`IsApproved`) | `payrolls.approve` | — |
| Editar | `payrolls.write` | `draft` \| `rejected` |
| Marcar pago / NF (folha ou item) | `payrolls.pay` | **não** `pending_approval` |
| Bônus/desconto pós-aprovação | `payrolls.pay` | `approved` |
| Excluir | `payrolls.delete` | — |
| Marcar folha inteira paga | `payrolls.pay` | não pending; seta todos `IsPaid`, status `paid` |
| Recalcular | `payrolls.write` | somente `draft`/`rejected` |

Seed: Financial não tem `payrolls.approve`; Director não precisa `payrolls.pay`.

**Implementado:** `PayrollCapabilitiesEvaluator` + `allowedActions` em `GET /api/v1/payrolls/{id}`; claims `permission` em `PayrollAccessContext`; UI `PayrollDetailWorkflowBar` / `PayrollDetailEntryActions` gated por API + policies (`AppPolicies.PayrollsApprove|Pay|Delete`); botões de mutação desabilitados (wire-up na 8.2); testes `PayrollCapabilitiesEvaluatorTests`, `PayrollsEndpointTests` (Financial/Director/Admin/Manager) e bUnit `PayrollDetailSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** Financial não vê botão Aprovar; Director não marca pago em `pending_approval`; testes de API batem a tabela — validado.

### 8.2 Transições e snapshot ✅

- Aprovar: `approved`, `ApprovedBy`, `ApprovedAt` ISO **e persistir snapshot** (JSON ou linhas calculadas).
- Reprovar: `rejected` + `RejectionComment` (bloquear se vazio).
- Todos `IsPaid` → `paid` (`PayrollStatus.Paid`; sem coluna `payment_status` separada).
- Desmarcar um pago estando `paid` → volta `approved`.

**Implementado:** `PayrollWorkflowTransitions` + métodos no `PayrollStore`; endpoints `POST .../approve|reject|pay|recalculate`, `DELETE .../{id}`, `POST .../entries/{entryId}/approve|pay`, `PUT .../entries/{entryId}/nf|{entryId}`; `approvedBy`/`approvedAt` no GET detalhe; UI `PayrollDetailSection` com dialogs de reprovação/exclusão e wire-up de `PayrollDetailWorkflowBar`/`PayrollDetailEntryActions`; testes `PayrollWorkflowTransitionsTests`, `PayrollWorkflowEndpointTests`, bUnit `PayrollDetailSectionTests`/`PayrollApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** teste de estado cobre o diagrama da §4.1; GET após aprovação ignora mudança posterior no `CareerLevel` — validado.

---

# Fase 9 — Financeiro

Fonte: `REGRAS_DE_NEGOCIO.md` §4.2 e §5.4. Identidade: valor riscado + “A Receber” primary. `GET /api/v1/finance/summary`.

### 9.1 Quem aparece ✅

Folha `approved` ou `paid` → todos. Senão → só `IsApproved`. Filtros mês/ano/setor/projeto.

**Implementado:** `FinanceEntryVisibility` + `GET /api/v1/finance/summary` (`FinanceStore`, `FinanceEndpoints`); UI `/financial` com `FinancialSection` + `FinanceApiService`; filtros server-side mês/ano/setor/projeto; opções de filtro embutidas na resposta; testes `FinanceEntryVisibilityTests`, `FinanceSummaryEndpointTests`, bUnit `FinancialSectionTests`/`FinanceApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** folha draft com 1 aprovado e 1 não → financeiro lista só o aprovado — validado.

### 9.2 PIX da empresa ✅

`CalcPlatformTotal` só Analista Comercial. A receber = `total_amount − platformTotal`. Header do grupo: total riscado se houver plataforma; “A Receber” em destaque.

Ações por linha: marcar pago, desmarcar, toggle NF. Progress bar emerald 6px.

**Implementado:** `FinanceEntryAmounts` + contratos/summary estendidos (`FinanceStore`, `FinanceContracts`); `allowedActions` via `PayrollCapabilitiesEvaluator`; UI `FinancialAmountDisplay`, `FinancialProgressBar`, `FinancialEntryActions`; `FinancialSection` com stats, coluna A Receber, progresso e mutações via `PayrollApiService`; testes `FinanceEntryAmountsTests`, `FinanceSummaryEndpointTests`, bUnit `FinancialSectionTests`/`FinanceApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** comercial com vendas Lastlink: PIX &lt; total; não-comercial: PIX = total — validado.

### 9.3 Custo por projeto (aba) ✅

Usar Fase 6. Agrupar colaborador → projetos. Tabs Colaboradores | Custo por Projeto.

**Implementado:** extensão aditiva de `GET /api/v1/finance/summary` com `entries[].projectTotals` (`FinanceStore` reutiliza `DisplayProjectTotals`); UI `/financial` com `Tabs` + `FinancialProjectCostPanel` + `FinancialProjectCostEntryRow` (reuso de `PayrollEntryBreakdownPanel`); filtros compartilhados; stats só na aba Colaboradores; testes `FinanceSummaryEndpointTests` (reconciliação comercial, snapshot, filtro), bUnit `FinancialSectionTests`/`FinanceApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** soma dos projetos de um comercial (após abate plataforma + pagador) bate o esperado do motor — validado.

### 9.4 Adicionar colaborador avulso ✅

Modal financial: entra já `IsApproved: true`. Admin pode excluir folha daqui (`payrolls.delete`).

**Implementado:** `POST /api/v1/payrolls/{id}/entries` (`AddCollaboratorEntryRequest`, `PayrollStore.AddCollaboratorEntryAsync`); `AddCollaborator` em `PayrollAllowedActionsResponse`/`PayrollCapabilitiesEvaluator` (`payrolls.write`, bloqueio em `pendingApproval`); UI `/financial` com `FinancialSection` + `PayrollCollaboratorPicker` + delete via `PayrollApiService.DeletePayrollAsync`; folha `paid` → `approved` ao incluir linha unpaid; snapshots existentes preservados; testes `PayrollCapabilitiesEvaluatorTests`, `AddCollaboratorEntryEndpointTests`, bUnit `FinancialSectionTests`/`FinanceApiServiceTests`/`PayrollApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** novo colaborador aparece na lista a pagar — validado.

---

# Fase 10 — Faturamento

Fonte: `REGRAS_DE_NEGOCIO.md` §7. `/api/v1/project-revenues`.

### 10.1 CRUD ✅

Por projeto + mês + ano: `ValueIgaming`, `ValueVendas`, `Value` = soma, `GroupPercentage`, notes. Não puxar automaticamente para a folha (o gestor redigita na entrada).

**Implementado:** entidade `ProjectRevenue` + migration `AddProjectRevenues`; CQRS `Application/Revenues/` + `ProjectRevenueStore`; `GET/POST /api/v1/project-revenues`, `GET/PUT /api/v1/project-revenues/{id}` (`revenues.read`/`revenues.write`; sem DELETE); UI `/project-revenues` com `ProjectRevenuesSection` + `ProjectRevenueFormDialog` + `ProjectRevenueApiService`; filtros mês/ano/projeto; total calculado server-side; testes `ProjectRevenuesEndpointTests`, bUnit `ProjectRevenueApiServiceTests`/`ProjectRevenuesSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** gravar 1000 iGaming + 500 vendas → value 1500; aparece na lista do mês — validado.

---

# Fase 11 — Investimento de tráfego

Fonte: `REGRAS_DE_NEGOCIO.md` §8; `agents.md` §3.6. Acesso: `traffic.read` / `traffic.write`.

### 11.1 Motor de tráfego (puro, em `Core`) ✅

- `TotalWeeks = 4`
- Canais de mídia: telegram, instagram, story, direto, remarketing, outros
- `ImpostoRate = 0.1215m`; imposto = round(gasto × rate, 2)
- Depósitos múltiplos; legado `requested_amount` → 1 depósito nos testes de paridade
- `GetSuggestedNext` = max(0, meta/4 − (Σ deposited_amount − gasto − imposto))

**Implementado:** `Core/Domain/TrafficInvestmentCalculation/` — `TrafficInvestmentCalculator`, `TrafficLegacyWeekAdapter`, enums `TrafficMediaChannel`/`TrafficDepositStatus`, contratos imutáveis e resultados semanais/mensais; erros `traffic.investment_*`; testes `TrafficInvestmentCalculatorTests`, `TrafficLegacyWeekAdapterTests`.

**Pronto quando:** meta 4000, sem depósito efetivo, gasto 0 → sugestão 1000; saldo ≥ base semanal → 0; Telegram 1000 → imposto 121,50 e total 1121,50 — validado.

### 11.2 UI e API ✅

Um `TrafficInvestment` por projeto/mês/ano. Meta mensal. Semanas 1–4: gastos por plataforma, imposto derivado, depósitos (status pending/requested/deposited). Stats: solicitado yellow, depositado/saldo/total emerald, gasto purple, imposto orange.

Entidade paralela `TrafficProjectDeposit`: aportes por **data** com valor por projeto (aba **Aportes**).

**Implementado:** entidades EF + migration `AddTrafficInvestments`; CQRS/store/endpoints `traffic-investments` e `traffic-deposits`; integração com `TrafficInvestmentCalculator`; UI `/traffic-investment` com tabs, dialog largo, 4 semanas, `TrafficInvestmentApiService`; RBAC `traffic.read`/`traffic.write`; testes `TrafficInvestmentsEndpointTests`, `TrafficDepositsEndpointTests`, bUnit `TrafficInvestment*Tests`; contratos em `docs/design.md`.

**Pronto quando:** editar Telegram 1000 gera imposto 121,50 e total semana 1121,50 — validado.

---

# Fase 12 — Fluxo de caixa + webhook

Fonte: `REGRAS_DE_NEGOCIO.md` §9; `agents.md` §3.7–3.8.

### 12.1 Lançamentos manuais ✅

Tipos `entrada|saida`. Categorias **fixas** da §9. Campos de saída: método, setor, solicitante, local, parcela, anexo, notes. CRUD PaymentMethod (`paymentmethods.write` = Admin ou Director no seed). Saldo = entradas − saídas. Summary cards emerald/red. Segmented entrada/saída. Chips de método (cartão blue, pix purple, …).

Parcelas: `InstallmentNumber` / `InstallmentTotal` / `CompraId`. Modal de grupo de parcelas.

**Implementado:** entidade `ProjectCost` + migration `AddProjectCosts`; CQRS/store/endpoints `GET/POST/PUT/DELETE /api/v1/cashflow` + `GET /api/v1/cashflow/installments/{compraId}`; summary server-side; geração atômica de parcelas a partir do valor total; UI `/cashflow` (`CashflowSection`, formulário segmented, modal de parcelas); `PaymentMethodsSection` em `/settings`; RBAC `cashflow.read|write`; testes `CashflowEndpointTests`, bUnit `CashflowSectionTests`/`CashflowApiServiceTests`/`PaymentMethodsSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** 1000 entrada + 300 saída → saldo 700 no mês filtrado — validado.

### 12.2 Relatório de caixa ✅

Por projeto e por método no período (competência mês/ano).

**Implementado:** `GET /api/v1/cashflow/report?month=&year=`; agregações server-side por `ProjectId` (entradas/saídas/saldo, bucket **Sem projeto**) e por `PaymentMethodId` (somente saídas); UI `/cashflow` com tabs **Lançamentos** \| **Relatório** (`CashflowReportSection`); `CashflowApiService.GetReportAsync`; RBAC `cashflow.read`; testes `CashflowEndpointTests` (agregação + filtro mensal + bucket nulo), bUnit `CashflowReportSectionTests`/`CashflowApiServiceTests`; contratos em `docs/design.md`.

**Pronto quando:** mês com 2 projetos, 2 métodos e 1 entrada sem projeto → totais por projeto/método e summary global reconciliam; outra competência não entra — validado.

### 12.3 Webhook Facilities ✅

`POST /api/v1/webhooks/facilities/cashflow`:

- HMAC-SHA256 de `timestamp + '.' + rawBody`; header `sha256=<hex>` lowercase
- Comparação tempo constante (`CryptographicOperations.FixedTimeEquals`)
- Timestamp ± 300s
- Headers: `X-Facilities-Timestamp`, `X-Facilities-Signature`, `X-Idempotency-Key` (= `lancamento_id`)
- Idempotência por `FacilitiesLancamentoId` (índice único filtrado + retry em race)
- Secret `Facilities:WebhookSecret` (configuração/env, nunca no frontend)
- Sem JWT (grava via `ProcessFacilitiesCashflowWebhookCommand` → `CashflowStore.CreateFromFacilitiesWebhookAsync`)
- Evento diferente → 422; GET → 405

**Implementado:** `FacilitiesWebhookEndpoints`, `FacilitiesWebhookSignatureValidator`, contratos CQRS em `Core/Application/Cashflow/FacilitiesCashflowWebhook*`, ingestão idempotente no `CashflowStore`; testes `FacilitiesWebhookEndpointTests`; contratos em `docs/design.md`.

**Pronto quando:** replay do mesmo `lancamento_id` retorna 200 `idempotent: true` e **não** duplica; assinatura errada → 401 — validado.

---

# Fase 13 — Dashboard e relatórios

Fonte: `REGRAS_DE_NEGOCIO.md` §4.2; `IDENTIDADE_VISUAL.md` §8.

### 13.1 Dashboard ✅

`GET /api/v1/dashboard`. Filtro mês/ano (default: **mês anterior**, timezone Bahia). Manager: só seus setores. **Ignorar drafts** nas contas. Stats: total folhas, aguardando, aprovadas (approved+paid), reprovadas, total a pagar (approved+paid), total pago, colaboradores ativos. Lista “Folhas recentes” (8), linha clicável.

Cores dos stats: blue / yellow / emerald / red / primary / emerald / purple.

**Implementado:** `GET /api/v1/dashboard` autenticado com widgets condicionados por `payrolls.read`/`collaborators.read`, competência anterior via `TimeProvider` + `BahiaTimeZone`, isolamento de Manager por `UserDepartments`, contas sem draft e valores líquidos travados por linha paga/pendente; lista global das 8 folhas mais recentes. UI `/` com `DashboardSection`, filtros mês/ano, grids 4+3 responsivos, stats semânticos e tabela navegável; testes `DashboardEndpointTests`, bUnit `DashboardApiServiceTests`/`DashboardSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** draft não entra no “Total de Folhas”; manager não vê folha alheia — validado.

### 13.2 Relatórios ✅

`GET /api/v1/reports/payroll`. Só folhas `approved` \| `paid` **ou** com algum `IsApproved`. Dedup setor+mês+ano pela prioridade `paid > approved > pending_approval > draft/rejected`.

Valores **sempre travados** no relatório (não recalcular). Alinhado ao detalhe da folha no CorePay — os dois leem snapshot quando aprovado/pago.

Matrizes: por setor, por projeto (Fase 6), por colaborador. Cards: total ano, média mensal, maior setor, maior projeto, count colaboradores. Gráficos paleta `chart-1`…`chart-5` (magenta→roxo).

**Implementado:** `GET /api/v1/reports/payroll` com visão anual (default ano Bahia), filtros setor/projeto, dedup/prioridade, snapshots travados (`CalculatedResult` + `DisplayProjectTotals`), cards/série mensal/matrizes, isolamento Manager; UI `/reports` com `ReportsSection`, gráficos CSS/SVG, tabs e filtros; testes `PayrollReportSelectionTests`/`PayrollReportAggregatorTests`, `PayrollReportEndpointTests`, bUnit `ReportsApiServiceTests`/`ReportsSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** duas folhas do mesmo setor/mês (uma paid, uma draft) → relatório usa só a paid — validado.

### 13.3 Export ✅

Export xlsx. Mesma regra de quem entra (aprovados / folha aprovada).

**Implementado:** `GET /api/v1/reports/payroll/export?year=&departmentId=&projectId=` reutiliza `PayrollReportStore` + `PayrollReportXlsxExporter` (ClosedXML); abas Resumo, Mensal, Por setor, Por projeto, Por colaborador; UI `/reports` com botão **Exportar Excel** (`ReportsApiService.ExportPayrollReportAsync`, `FileDownloadService`, `corepayDownload.js`); RBAC `reports.read`; testes `PayrollReportExportEndpointTests`, bUnit `ReportsApiServiceTests`/`ReportsSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** export contém só linhas que a tela de relatório mostra — validado.

---

# Fase 14 — Notificações

Fonte: `REGRAS_DE_NEGOCIO.md` §4.3; `agents.md` §3.9. `GET /api/v1/notifications`.

### 14.1 Emissão ✅

| Evento | type | role_target típico |
|---|---|---|
| Folha submetida | `payroll_submitted` | Director (e/ou Admin) |
| Aprovada | `payroll_approved` | manager que submeteu |
| Reprovada | `payroll_rejected` | manager que submeteu |

Campos: title, message, payroll_id, user_id e/ou role_target, is_read.

**Implementado:** entidade `Notification` + migration `AddNotifications`; coluna `Payroll.SubmittedByUserId`; `INotificationStore`/`NotificationStore` com emissão atômica em `PayrollStore` (submit → 2 registros Director+Admin; approve/reject → `user_id` submetedor); `GET /api/v1/notifications` autenticado com filtro `(user_id OR role_target)` + `unreadCount`; testes `NotificationVisibilityTests`, `PayrollNotificationContentBuilderTests`, `NotificationsEndpointTests`; contratos em `docs/design.md`.

**Pronto quando:** submeter folha cria notificação visível para Director — validado.

### 14.2 UI ✅

Lista; marcar lida. Sino na topbar com badge `destructive` se unread &gt; 0. Cores: rejected red/10, submitted yellow/10, approved emerald/10.

**Implementado:** `PUT /api/v1/notifications/{id}/read` com recibos individuais `NotificationReadReceipt`; UI `/notifications` com `NotificationsSection` + `NotificationRow`, `NotificationApiService`, `NotificationState`, badge no `AppHeader`, cores semânticas §7.13; testes `NotificationsEndpointTests`, `NotificationReadStateTests`, bUnit `NotificationApiServiceTests`/`NotificationsSectionTests`/`NotificationToneMapperTests` + extensão `ShellLayoutTests`; contratos em `docs/design.md`.

**Pronto quando:** submeter folha cria notificação visível para Director; badge incrementa — validado.

---

# Fase 15 — Paridade, extras e endurecimento

### 15.1 Métricas de analista ✅

`AnalystMetric`: FTD e CPA por colaborador+projeto+mês. Cadastro auxiliar; **não** alimenta a folha sozinho.

**Implementado:** entidade `AnalystMetric` + migration `AddAnalystMetrics`; CQRS `Application/AnalystMetrics/` + `AnalystMetricStore`; `GET/POST /api/v1/analyst-metrics`, `GET/PUT /api/v1/analyst-metrics/{id}` (`analystmetrics.read`/`analystmetrics.write`; sem DELETE); FTD/CPA como inteiros não negativos; unicidade colaborador+projeto+competência; Manager isolado por `UserDepartments`; UI `/analyst-metrics` com filtros, tabela, dialog e estados loading/error/empty; testes `AnalystMetricsEndpointTests`, bUnit `AnalystMetricApiServiceTests`/`AnalystMetricsSectionTests`; contratos em `docs/design.md`.

**Pronto quando:** Manager cadastra FTD 120 e CPA 35 no próprio setor, a métrica aparece na competência; duplicata retorna 409, acesso cruzado retorna 403 e a folha não é alterada — validado.

### 15.2 Estados de borda da UI ✅

- Empty states em todas as listas.
- Loading spinner padrão.
- Confirm dialog em delete.
- Folha travada visualmente quando não editável.
- Hover “Editar” em colaboradores.
- Financeiro: linha paga com fundo `emerald-500/10`.

**Implementado:** padronização `Spinner`/`EmptyState` nas listas operacionais e gaps (`PayrollCollaboratorPicker`, `TrafficWeekPanel`); spinners acessíveis em guards; confirmação de exclusão em `CashflowSection`; bloqueio visual reforçado em `PayrollEditabilityGuard` (`.payroll-form__readonly-card` + `StatusBadge`); hover “Editar” validado em colaboradores; linha paga financeiro `.financial-table__row--paid` com emerald/10 preservado no hover; testes bUnit estendidos; contratos em `docs/design.md` e `docs/IDENTIDADE_VISUAL.md`.

**Pronto quando:** todas as listas operacionais exibem empty/loading padrão; DELETE de caixa exige confirmação; folha fora de draft/rejected mostra bloqueio visual; linha paga no financeiro mantém fundo emerald — validado.

### 15.3 Checklist de paridade visual ✅

Percorrer `IDENTIDADE_VISUAL.md` §14 em: Login, Dashboard, Folhas, Detalhe, Financeiro, Caixa, Tráfego, Settings. Dark e light. Desktop ≥1024 e mobile drawer. Marca **CorePay**.

**Implementado:** matriz de auditoria rastreável em `docs/IDENTIDADE_VISUAL.md` §15.3; baselines `/dev/tokens`, `/dev/typography`, `/dev/kitchen`; testes bUnit `VisualParityChecklistTests` + extensões por tela (`LoginPageTests`, `ShellLayoutTests`, `DashboardSectionTests`, `PayrollsSectionTests`, `PayrollDetailSectionTests`, `FinancialSectionTests`, `CashflowSectionTests`, `TrafficInvestmentSectionTests`, `TrafficInvestmentFormDialogTests`, `TrafficWeekPanelTests`, `SettingsPageTests`, `UiComponentTests`); correções visuais: thead `secondary/40` + hover `accent/30` em `.settings-table`; merge de classes extras no `Button` (CTA login mantém `ui-button--default`); documentação atualizada para marca CorePay.

**Pronto quando:** as oito telas passam nos critérios §14 aplicáveis em dark/light e desktop/mobile drawer; marca CorePay visível; `dotnet test CorePay.Frontend.slnx` verde — validado.

### 15.4 Checklist de paridade de negócio ✅

Percorrer `REGRAS_DE_NEGOCIO.md` §5–6 com as fixtures da 0.3. Conferir o mapeamento da §11: **renomear** o setor “Tráfego Pago” no teste **não** pode mudar o cálculo (perfil explícito) — este é o comportamento **novo** do CorePay, invertendo o legado.

**Implementado:** matriz rastreável em `docs/REGRAS_DE_NEGOCIO.md` §15.4; checklist agregador `BusinessParityChecklistTests` + extensões em `ComplementAllocationCalculatorTests`, `CommercialAnalystProjectCommissionCalculatorTests`, `CommercialAnalystPlatformCalculatorTests`, `ProportionalFactorTests`, `FixedCommissionCalculatorTests`, `FixedCommissionBonusCalculatorTests`, `TrafficCpaRateHelperTests`, `PayrollCalculatorPaidTrafficTests`, `PayrollCalculatorProjectTotalsTests`; integração `ExplicitProfileParityTests` (rename persistido + preview HTTP com comissão R$350); §13.3 marcado resolvido; nota CorePay em §5 intro.

**Pronto quando:** matriz §5–6 + §11 completa; renomear Tráfego Pago não altera `PaidTraffic`/R$350; fixtures 0.3 mapeadas; `dotnet test CorePay.slnx` verde — validado.

### 15.5 Segurança ✅

- Webhook sem vazar secret.
- Manager isolation em **todas** as queries (folha, colaborador, dashboard, reports) → 403.
- Financial não aprova; Director não precisa pagar.
- Sem logs de PIX/senhas.
- JWT e `Facilities:WebhookSecret` só em configuração/ambiente.

**Implementado:** `FacilitiesOptionsValidator` + `ValidateOnStart` (exceto `Testing`); webhook HMAC retorna **401** sem corpo; `editorOptions`/`filterOptions.departments` escopados por `UserDepartments`; middleware `ManagerDepartmentAccessAuditMiddleware` (403 `*.department_forbidden` — loga só userId/método/rota); segredos removidos de `appsettings*.json`; `.env.example`/`README` documentam `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Facilities__WebhookSecret`, `Seed__SuperAdmin__Password`; checklist agregador `SecurityChecklistTests` + extensões em `PayrollsEndpointTests`, `CollaboratorsEndpointTests`, `DashboardEndpointTests`, `PayrollReportEndpointTests`, `PayrollWorkflowEndpointTests`, `FacilitiesWebhookEndpointTests`, `FinanceSummaryEndpointTests`, `ManagerDepartmentAccessAuditMiddlewareTests`, `PayrollCapabilitiesEvaluatorTests`, bUnit `PayrollDetailSectionTests`; contratos em `docs/design.md` §15.5.

**Pronto quando:** webhook nunca expõe secret; manager 403 em acesso cruzado com metadados escopados; Financial/Director respeitam approve/pay na API e UI; audit log sem PIX/senha; JWT/WebhookSecret/connection string só via ambiente; `dotnet test CorePay.slnx` e `dotnet test CorePay.Frontend.slnx` verdes — validado.

### 15.6 Entrega ✅

README: setup, env (`Jwt:Key`, `Facilities:WebhookSecret`, connection string), como rodar testes do motor. Linkar `agents.md` e os docs em `docs/`. CI com testes do `PayrollCalculator` verdes.

**Implementado:** README com setup reproduzível (env `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Facilities__WebhookSecret`, `Seed__SuperAdmin__*`, aguardar SQL healthy, comando motor `dotnet test tests/Core.Tests/Core.Tests.csproj --filter "FullyQualifiedName~PayrollCalculation"`, links para `agents.md` e todos os docs em `docs/` incl. `VISAO_GERAL.md`); tabela canônica de **83 rotas** em `agents.md` §4 (incl. `GET /api/v1/webhooks/facilities/cashflow` → 405); `docs/VISAO_GERAL.md` §12 alinhado ao roadmap.

**Pronto quando:** um humano consegue seguir só o README e os testes do motor passam em CI. Tabela de endpoints em `agents.md` reflete o que **existe** — validado.

---

# Matriz de aceite global (definição de “CorePay completo”)

A implementação está completa só se **todas** forem verdadeiras:

1. Os papéis de referência (`SuperAdmin` + 5) + permissões (incluindo traffic/collaborators sem e-mail especial) navegam certo.
2. Folha percorre draft → pending → approved/rejected → paid, com comentário na reprovação e **snapshot** na aprovação.
3. `CalcEntry` cobre comercial, supervisor, tráfego, líder, gerência, tipster, rateado/Feira, Affiliates, proporcional, role_change — com testes; perfil por enum.
4. PIX comercial abate Lastlink/Hubla (teto 4% Hubla); complemento e pagador &lt; R$100 batem as regras.
5. Caixa soma entradas/saídas; webhook HMAC + idempotência em `/api/v1/webhooks/facilities/cashflow`.
6. Tráfego: 4 semanas, imposto 12,15%, sugestão de depósito.
7. Relatório ignora draft; detalhe e relatório **não** recalculam approved/paid.
8. UI dark-first, magenta, chips de status, Inter, tabular money — checklist visual §14.
9. Manager nunca lê dados de outro setor (API 403 + UI).
10. Backend e frontend desacoplados; comunicação só via `/api/v1/...`.

---

# Anti-escopo (não fazer até o humano pedir)

- Reabrir as decisões de `agents.md` (stack, JWT, enum de perfil, snapshot, percentuais explícitos, **IDs Guid**).
- Introduzir IDs sequenciais (`int`/`long` IDENTITY) em entidades, FKs, rotas ou DTOs.
- Reintroduzir regex de nome no motor de produção, ou autorização por e-mail (`rh@`, `trafego@`).
- Recalcular folha `approved`/`paid` no GET de detalhe (legado §13.2 — **não** reproduzir).
- Usar `traffic_sup_bonus` / `traffic_sup_commission_pct` no cálculo automático (paridade §13.1).
- Pagamento automático (Stripe, Open Finance, PIX cobrado).
- App público / self-signup irrestrito / OAuth Google na v1.
- Redesign “moderno” (azul, cards com sombra, outra fonte).
- Microserviços por módulo na v1.
- Copiar arquivos JS/Tailwind do legado para este repo.

Se o humano pedir para ativar `traffic_sup_*` ou outras correções além das já decididas em `agents.md`, faça **depois** da paridade, numa fase extra 16, com testes que deixem o comportamento novo explícito.
