# Prompt de Instruções para Desenvolvimento do CorePay (`agents.md`)

Este documento serve como instrução mestra (`agents.md`) para ser fornecido à LLM (ou equipe de engenharia) para o desenvolvimento autônomo do sistema interno de folha, financeiro e tráfego.

A fonte operacional das fórmulas e do workflow da folha é [`docs/REGRAS_DE_NEGOCIO.md`](docs/REGRAS_DE_NEGOCIO.md). Este arquivo define **arquitetura, stack, contratos e invariantes**. Não duplicar fórmulas aqui — implementar e testar a partir das regras de negócio.

---

# ARCHITECTURE & SYSTEM INSTRUCTIONS (`agents.md`)

## 1. Visão Geral e Arquitetura Desacoplada

Você é um **Agente Engenheiro de Software Senior** especialista em ecossistema .NET, Arquitetura Orientada a Domínio (DDD), CQRS, TDD e boas práticas de engenharia de software.
O seu objetivo é construir e manter o **CorePay**: sistema interno de folha de pagamento, financeiro, investimento de tráfego e fluxo de caixa.
**Regra Arquitetural Crítica:** O sistema opera com **Desacoplamento Total** entre Backend e Frontend. A comunicação entre as camadas ocorrerá exclusivamente via API RESTful versionada, garantindo escalabilidade, deploy independente e clareza de contratos.

Este repositório é a reescrita da aplicação FolhaPay (legado Base44 / SPA JS). O comportamento de negócio deve ser equivalente ao documentado em `docs/REGRAS_DE_NEGOCIO.md`; a implementação deve seguir esta stack, não o legado.

### Stack Tecnológica e Separação
* **Backend (API RESTful):**
  * **Linguagem & Runtime:** .NET 10 (C# 14) minimal APIs.
  * **Banco de Dados:** Microsoft SQL Server (rodando em container Docker local).
  * **ORM & Persistência:** Entity Framework Core 10 com Auto-Migrations executadas no arranque. PKs/FKs de recurso: **`Guid`** (`uniqueidentifier`); nunca `IDENTITY` sequencial (ver §3.1).
  * **Padrões de Design:** CQRS (MediatR — admin Fase 2.1), porta `IAdminIdentityStore` e **Result Pattern** (sem uso de exceptions para regras de negócio, sempre retornando `Result<T>`).
  * **Autenticação:** Stateless via JWT, gerenciada pelo ASP.NET Core Identity.
  * **Versionamento:** Implementado explicitamente na rota (ex: `/api/v1/...`) com `Asp.Versioning.Http` + `Asp.Versioning.Mvc.ApiExplorer` (Minimal APIs + Swagger).
* **Frontend (SPA):**
  * **Framework:** Blazor WebAssembly (.NET 10).
  * **Comunicação HTTP:** Consumo estrito da API versionada via `HttpClient` e `AuthorizationMessageHandler` para injeção do token JWT Bearer.
  * **Autenticação (Fase 2.2):** `AuthService` + `CorePayAuthenticationStateProvider` + persistência em `localStorage` (`corepay.auth.session` via `wwwroot/js/auth.js`); login em `/login` (`EmptyLayout`); logout client-side; init de sessão no startup WASM.
  * **Arquitetura Visual (Fase 2.3–2.4):** Shell autenticado fullscreen (`MainLayout`, `AppSidebar`, `AppHeader`, `SidebarTrigger`, `SidebarState`); sidebar 256px dark, collapse desktop, drawer mobile com overlay; topbar blur; main padding 24px; nav ativa primary + chevron; **menu filtrado por permissão** (`ShellNavigation` + `IAuthorizationService`; policies `permission:*` e `permission-any:*` via `AppPolicies`; seção Administração oculta sem `users.read`/`roles.read`; rotas protegidas por `[Authorize(Policy = ...)]` — acesso direto por URL mostra `AccessDenied`). Componentes UI reutilizáveis em `Components/Ui/` (`Button`, `StatusBadge`, `StatCard`, `Card`, `Input`, `Select`, `Tabs`, `Dialog`, `GoalToggle`, `Avatar`, `Spinner`, `EmptyState`, `ThemeToggle`, `Icon`), ícones **Lucide** via `InfiniLore.Lucide` + wrapper tipado `Icon` (`IconKind`, `IconSize`, catálogo em `IconMetadata.cs`), tokens CSS semânticos (dark-first) em `wwwroot/css/app.css`, persistência de tema via `ThemeService` + `localStorage.theme` (fallback `dark`), amostras em `/dev/tokens`, `/dev/typography` e `/dev/kitchen`. Tipografia Inter 300–800 (Google Fonts), escala semântica em `app.css`, formatação monetária via `MoneyFormatter.FormatMoney(decimal)` (`Formatting/MoneyFormatter.cs`, cultura `pt-BR`, `tabular-nums` em valores).
  * **Financeiro (Fase 9.1–9.4):** `/financial` (`AppPolicies.FinanceRead`); `FinancialSection` + `FinanceApiService` consome `GET /api/v1/finance/summary` (filtros server-side mês/ano/setor/projeto; opções de filtro na resposta; campos `platformTotal`, `amountToReceive`, `projectTotals`, agregados por folha e `allowedActions` incl. `addCollaborator`/`delete`); regra §4.2 no backend (`FinanceEntryVisibility`): folha `approved`/`paid` → todos; senão → só `IsApproved`; folhas sem linha elegível omitidas; PIX empresa = `totalAmount − platformTotal` via `FinanceEntryAmounts`; `projectTotals` via `DisplayProjectTotals` (Fase 6.2); tabs `Colaboradores` \| `Custo por Projeto` (`Tabs` + `FinancialProjectCostPanel` + `FinancialProjectCostEntryRow`, reuso `PayrollEntryBreakdownPanel`); stats Total a pagar/pago só na aba Colaboradores; accordion por folha com total riscado + “A Receber” primary, progress bar emerald 6px (`paidCount/entryCount`); ações pago/NF por linha via `PayrollApiService` + `FinancialEntryActions` (gated por `allowedActions`/`payrolls.pay`; `pending_approval` bloqueado); **9.4 avulso:** `POST /api/v1/payrolls/{id}/entries` (`payrolls.write`) adiciona colaborador ativo com `IsApproved: true`, recalcula/persiste snapshot só da nova linha, atualiza total; bloqueado em `pending_approval`; folha `paid` → `approved`; UI “Adicionar colaborador avulso” via `PayrollCollaboratorPicker` + delete folha (`payrolls.delete`, Admin); testes `FinanceEntryAmountsTests`, `FinanceSummaryEndpointTests`, `AddCollaboratorEntryEndpointTests`, bUnit `FinancialSectionTests`/`FinanceApiServiceTests`/`PayrollApiServiceTests`.
  * **Rotas da folha (Fase 2.5 + 7.2–7.6 + 8.1–8.2):** `/payrolls`, `/payrolls/new`, `/payrolls/{id:guid}`, `/payrolls/{id:guid}/edit`; lista/detalhe exigem `payrolls.read`, criação/edição exigem `payrolls.write`; **lista (7.2):** `PayrollsSection` + `GET /api/v1/payrolls` (filtros server-side, agrupamento por competência, duplicar → mês seguinte); **casca nova/editar (7.3):** `PayrollFormShell` + `PayrollHeaderFields` + `PayrollCollaboratorPicker` + `PayrollEntryShellRow` + `PayrollEditabilityGuard`; `GET /api/v1/payrolls/form-options`, `POST /api/v1/payrolls`, `PUT /api/v1/payrolls/{id}` (sync colaboradores + save em lote com recálculo); **salvar/submeter (7.5):** botões “Salvar” / “Salvar e submeter” no edit; `POST /api/v1/payrolls/{id}/submit` → `pendingApproval` + `SubmittedBy`; **detalhe (7.6):** `PayrollDetailSection` + `PayrollDetailEntryRow` + `PayrollEntryBreakdownPanel`; `GET /api/v1/payrolls/{id}` enriquecido com `entries[].isPaid`, `nfSent`, `result`, `projectTotals`, `approvedBy`, `approvedAt`; snapshot calculado mínimo em `Payload.CalculatedResult`/`DisplayProjectTotals` (persistido no save/submit/aprovação); estados travados (`pendingApproval`/`approved`/`paid`) leem snapshot sem recalcular; `draft`/`rejected` recalculam em memória se ausente; layout `max-w-3xl`; header com competência (`CompetenceFormatter`), `StatusBadge`, submetido/aprovado por, total da folha; lista expansível com PIX, chips aprovado/pago/NF, breakdown por projeto; botão “Editar” outline só se `payrolls.write` + status editável; snapshot via `PayrollEntrySnapshotBuilder`; setor/mês/ano imutáveis após criação; só colaboradores ativos do setor; folha `rejected` + Manager oculta entradas `IsApproved`; detalhe/edição usam `PayrollResourceGuard` + `PayrollApiService` — 404 → `EmptyState` “Folha não encontrada”, 403 → `AccessDenied`, status não editável → `PayrollEditabilityGuard`; `{id:guid}` inválido cai em `NotFound` autenticado (`MainLayout`, pt-BR); topbar “Folhas” em subrotas via `ShellNavigation`; **permissões workflow (8.1):** `PayrollCapabilitiesEvaluator` + `allowedActions` no GET detalhe; policies `AppPolicies.PayrollsApprove|Pay|Delete`; “Editar” usa `allowedActions.edit` + `payrolls.write`; **transições (8.2):** `PayrollWorkflowTransitions` + endpoints approve/reject/pay/recalculate/delete e ações por linha; UI `PayrollDetailWorkflowBar` / `PayrollDetailEntryActions` wired com dialogs de reprovação/exclusão; `PayrollApiService` consome todas as mutações; ajuste pós-aprovação de bônus/desconto via `PUT .../entries/{entryId}` (`payrolls.pay`).
  * **Configurações — Setores (Fase 3.2):** `/settings` (`AppPolicies.MasterData`); seção Setores gated por `AppPolicies.DepartmentsWrite` (`Components/Settings/DepartmentsSection`, `DepartmentFormDialog`, `SectionCardHeader`); `DepartmentApiService` consome `GET/POST/PUT /api/v1/departments`; perfil de cálculo via enum `CalculationProfile` (labels pt-BR em `CalculationProfileLabels`); sem DELETE na UI; Director vê Configurações mas não a seção Setores.
  * **Configurações — Níveis de carreira (Fase 3.3):** seção Níveis gated por `AppPolicies.CareerLevelsWrite` (`CareerLevelsSection`, `CareerLevelFormDialog`); `CareerLevelApiService` consome `GET/POST/PUT /api/v1/career-levels`; formulário contextual por `profile` (blocos CommercialAnalyst / CommercialSupervisor / Management / PaidTraffic + campos comuns); perfil sugerido pelo setor no create, editável após escolha manual; `Dialog` largo (`IsWide`); labels CPA em `TrafficHouseLabels`; sem DELETE na UI; Director vê Configurações mas não a seção Níveis.
  * **Configurações — Projetos (Fase 3.4):** seção Projetos gated por `AppPolicies.ProjectsWrite` (`ProjectsSection`, `ProjectFormDialog`); `ProjectApiService` consome `GET/POST/PUT /api/v1/projects`; plataforma via enum `ProjectPlatform` (labels pt-BR em `ProjectPlatformLabels`); flags `isDefaultAllocationTarget`, `excludesGoalBonus`, `excludesSupervisorFixedAllocation`; chip ativo/inativo emerald/muted; sem DELETE na UI; Director vê Configurações mas não a seção Projetos.
  * **Administração — Usuários (Fase 3.5):** `/admin/users` (`AppPolicies.UsersRead`); CRUD em `Components/Admin/` (`UsersSection`, `UserFormDialog`); mutações gated por `AppPolicies.UsersWrite`; `UserApiService` + `RoleApiService` consomem `GET/POST/PUT/DELETE /api/v1/users` e `GET /api/v1/roles`; setores via `IDepartmentApiService`; multi-seleção de papéis/setores; senha só no create; DELETE com confirmação; UI oculta role `SuperAdmin` para não-SuperAdmin; bloqueia exclusão de SuperAdmin e autoexclusão; erros pt-BR em `UserErrorMessages`; testes bUnit em `Admin/UserApiServiceTests.cs` e `Admin/UsersSectionTests.cs`.
  * **Faturamento (Fase 10.1):** `/project-revenues` (`AppPolicies.RevenuesRead`); `ProjectRevenuesSection` + `ProjectRevenueFormDialog` + `ProjectRevenueApiService` consome `GET/POST /api/v1/project-revenues` e `GET/PUT /api/v1/project-revenues/{id}` (filtros server-side mês/ano/projeto; `value` = soma calculada no backend; unicidade projeto+competência; sem DELETE); mutações gated por `AppPolicies.RevenuesWrite` (Admin/Financial; Director só leitura); **não** integra com folha; testes `ProjectRevenuesEndpointTests`, bUnit `ProjectRevenueApiServiceTests`/`ProjectRevenuesSectionTests`; contratos em `docs/design.md`.
  * **Métricas de analista (Fase 15.1):** `/analyst-metrics` (`AppPolicies.AnalystMetricsRead`); `AnalystMetricsSection` + `AnalystMetricFormDialog` + `AnalystMetricApiService` consome `GET/POST /api/v1/analyst-metrics` e `GET/PUT /api/v1/analyst-metrics/{id}` (FTD/CPA inteiros, filtros competência/colaborador/projeto, unicidade colaborador+projeto+competência; sem DELETE); Admin/Manager escrevem, Director somente lê, Manager isolado por `UserDepartments`; **não** integra nem recalcula a folha; testes `AnalystMetricsEndpointTests`, bUnit `AnalystMetricApiServiceTests`/`AnalystMetricsSectionTests`; contratos em `docs/design.md`.
  * **Estados de borda da UI (Fase 15.2):** padronização frontend de empty/loading (`Spinner` + `EmptyState` + `.settings-section__loading`); gaps fechados em `PayrollCollaboratorPicker` (elegível vs busca sem resultado), `TrafficWeekPanel` (depósitos vazios) e spinners acessíveis em guards (`App.razor`, `PayrollResourceGuard`, `PayrollFormShell`); **DELETE com confirmação** em `CashflowSection` (demais deletes já confirmavam); folha não editável reforçada em `PayrollEditabilityGuard` (`StatusBadge` + `.payroll-form__readonly-card`); hover “Editar” em colaboradores (`.collaborators-table__row-action`); financeiro linha paga `.financial-table__row--paid` (`rgb(16 185 129 / 0.1)`, preservada no hover); testes bUnit `CashflowSectionTests`/`CashflowApiServiceTests`/`PayrollCollaboratorPickerTests`/`TrafficWeekPanelTests`/`PayrollEditabilityGuardTests`/`CollaboratorsSectionTests`/`FinancialSectionTests`; contratos em `docs/design.md` e `docs/IDENTIDADE_VISUAL.md`.
  * **Checklist de paridade visual (Fase 15.3):** auditoria §14 em Login, Dashboard, Folhas, Detalhe, Financeiro, Caixa, Tráfego, Settings (dark/light, desktop ≥1024px e drawer mobile); matriz rastreável em `docs/IDENTIDADE_VISUAL.md` §15.3; baselines `/dev/tokens`, `/dev/typography`, `/dev/kitchen`; correções: thead `secondary/40` + hover `accent/30` em `.settings-table`; `Button` faz merge de `class` em `AdditionalAttributes` (preserva variantes no CTA login); testes bUnit `VisualParityChecklistTests` + extensões por tela; contratos em `docs/IDENTIDADE_VISUAL.md`.
  * **Checklist de paridade de negócio (Fase 15.4):** auditoria §5–6 + §11 com fixtures 0.3; matriz rastreável em `docs/REGRAS_DE_NEGOCIO.md` §15.4; checklist agregador `BusinessParityChecklistTests` + extensões em `Core.Tests/PayrollCalculation/` (`ComplementAllocationCalculatorTests`, `CommercialAnalystProjectCommissionCalculatorTests`, `CommercialAnalystPlatformCalculatorTests`, `ProportionalFactorTests`, `FixedCommissionCalculatorTests`, `FixedCommissionBonusCalculatorTests`, `TrafficCpaRateHelperTests`, `PayrollCalculatorPaidTrafficTests`, `PayrollCalculatorProjectTotalsTests`); integração rename Tráfego Pago → preview HTTP inalterado (`ExplicitProfileParityTests`); critério §11: perfil explícito `PaidTraffic` — renomear setor **não** altera cálculo (R$350 aceite); §13.3 resolvido; aceite `dotnet test CorePay.slnx`; contratos em `docs/REGRAS_DE_NEGOCIO.md`.
  * **Segurança (Fase 15.5):** webhook HMAC **401** sem corpo; `FacilitiesOptionsValidator` + `ValidateOnStart`; JWT/`Facilities:WebhookSecret`/connection string só env/user-secrets (nunca `appsettings` versionado); `editorOptions`/`filterOptions.departments` escopados por `UserDepartments`; middleware `ManagerDepartmentAccessAuditMiddleware` (403 `*.department_forbidden` — log mínimo sem PIX/senha); Financial sem `payrolls.approve`, Director sem `payrolls.pay` (API + `allowedActions` + UI); checklist agregador `SecurityChecklistTests` + testes em `WebAPI.Tests/Security/`, `PayrollWorkflowEndpointTests`, `FacilitiesWebhookEndpointTests`, bUnit `PayrollDetailSectionTests`; contratos em `docs/design.md` §15.5.
  * **Entrega (Fase 15.6):** README com setup reproduzível (env `Jwt:Key`, `Facilities:WebhookSecret`, connection string, Seed SuperAdmin), comando do motor `dotnet test tests/Core.Tests/Core.Tests.csproj --filter "FullyQualifiedName~PayrollCalculation"`, links para `agents.md` e docs em `docs/`; tabela canônica de endpoints §4 abaixo; aceite `dotnet test CorePay.slnx` + filtro motor verde.
  * **Investimento de tráfego (Fase 11.2):** `/traffic-investment` (`AppPolicies.TrafficRead`); `TrafficInvestmentSection` + `TrafficInvestmentFormDialog` + `TrafficWeekPanel` + `TrafficDepositsSection` + `TrafficInvestmentApiService` consome `GET/POST /api/v1/traffic-investments`, `GET/PUT /api/v1/traffic-investments/{id}`, `GET/POST /api/v1/traffic-deposits` (filtros mês/ano/projeto; stats/imposto/total/saldo/sugestão calculados no backend via `TrafficInvestmentCalculator`; unicidade projeto+competência; aportes datados append-only); mutações gated por `AppPolicies.TrafficWrite` (Admin no seed; Director só leitura); tabs Investimentos \| Aportes; stats yellow/emerald/purple/orange; testes `TrafficInvestmentsEndpointTests`/`TrafficDepositsEndpointTests`, bUnit `TrafficInvestmentApiServiceTests`/`TrafficInvestmentSectionTests`; contratos em `docs/design.md`.
  * **Fluxo de caixa (Fase 12.1–12.3):** `/cashflow` (`AppPolicies.CashflowRead`); tabs **Lançamentos** \| **Relatório**; `CashflowSection` + `CashflowReportSection` + `CashflowEntryFormDialog` + `CashflowInstallmentGroupDialog` + `CashflowApiService` consome `GET/POST/PUT/DELETE /api/v1/cashflow`, `GET /api/v1/cashflow/report?month=&year=` e `GET /api/v1/cashflow/installments/{compraId}` (filtros server-side mês/ano/tipo/categoria/projeto/setor/busca; summary `totalEntradas`/`totalSaidas`/`saldo`/`count` calculado no backend; relatório agrega por projeto — entradas/saídas/saldo, bucket **Sem projeto** — e por forma de pagamento — somente saídas; saída parcelada gera grupo atômico por `CompraId` a partir do valor total); mutações gated por `AppPolicies.CashflowWrite` (Admin/Financial; Director só leitura); DELETE com dialog de confirmação (15.2); segmented entrada/saída + chips de forma de pagamento; modal de parcelas; **Configurações — Formas de pagamento:** `PaymentMethodsSection` + `PaymentMethodApiService` em `/settings` gated por `AppPolicies.PaymentMethodsWrite` (Admin/Director); anexo via `AttachmentUrl` (sem upload); **12.3 webhook Facilities:** `POST /api/v1/webhooks/facilities/cashflow` anônimo com HMAC (`FacilitiesWebhookSignatureValidator`, secret `Facilities:WebhookSecret`); idempotência por `FacilitiesLancamentoId`; CQRS `ProcessFacilitiesCashflowWebhookCommand` + `CashflowStore.CreateFromFacilitiesWebhookAsync`; testes `CashflowEndpointTests`/`FacilitiesWebhookEndpointTests`, bUnit `CashflowSectionTests`/`CashflowReportSectionTests`/`CashflowApiServiceTests`/`PaymentMethodsSectionTests`; contratos em `docs/design.md`.
  * **Dashboard (Fase 13.1):** `/` autenticado; `DashboardSection` + `DashboardApiService` consome `GET /api/v1/dashboard?month=&year=` (default mês anterior em `America/Bahia`); blocos de folha/lista exigem `payrolls.read`, colaboradores ativos exige `collaborators.read`, e blocos sem permissão retornam `null`; Manager isolado por `UserDepartments`; contas da competência ignoram draft; aprovadas = approved+paid; Total a pagar/pago usa PIX empresa líquido dos snapshots de linhas não pagas/pagas; lista global com 8 folhas por competência decrescente, inclusive draft; grids responsivos 4+3 e tabela navegável; testes `DashboardEndpointTests`, bUnit `DashboardApiServiceTests`/`DashboardSectionTests`; contratos em `docs/design.md`.
  * **Relatórios (Fase 13.2–13.3):** `/reports` (`AppPolicies.ReportsRead`); `ReportsSection` + `ReportsApiService` consome `GET /api/v1/reports/payroll?year=&departmentId=&projectId=` (default ano civil Bahia); visão anual com total bruto travado (`CalculatedResult`), média `/12`, matrizes por setor/projeto (`DisplayProjectTotals`)/colaborador, dedup por competência, filtros e gráficos CSS/SVG com `--chart-1`…`5`; **13.3 export:** `GET /api/v1/reports/payroll/export` (mesmos filtros/RBAC; XLSX via `PayrollReportXlsxExporter`/ClosedXML; abas Resumo/Mensal/Por setor/Por projeto/Por colaborador); UI botão **Exportar Excel** + `FileDownloadService` + `corepayDownload.js`; Manager isolado por `UserDepartments`; testes `PayrollReportEndpointTests`/`PayrollReportExportEndpointTests`, Core `PayrollReportSelectionTests`/`PayrollReportAggregatorTests`, bUnit `ReportsApiServiceTests`/`ReportsSectionTests`; contratos em `docs/design.md`.
  * **Notificações (Fase 14.2):** `/notifications` (autenticado); `NotificationsSection` + `NotificationRow` + `NotificationApiService` + `NotificationState` consome `GET /api/v1/notifications` e `PUT /api/v1/notifications/{id}/read` (recibo individual `NotificationReadReceipt`); badge `destructive` no `AppHeader` quando `unreadCount > 0`; cores por tipo submitted/approved/rejected; click na row marca lida e navega `/payrolls/{payrollId}`; testes `NotificationsEndpointTests`, `NotificationReadStateTests`, bUnit `NotificationApiServiceTests`/`NotificationsSectionTests`/`NotificationToneMapperTests`/`ShellLayoutTests`; contratos em `docs/design.md`.
  * **Colaboradores (Fase 4.1–4.2):** `/collaborators` (`AppPolicies.CollaboratorsRead`); `CollaboratorsSection` + `CollaboratorFormDialog` em `Components/Collaborators/`; filtros server-side (setor, busca, ativo/inativo) via `CollaboratorApiService` + DTOs (`CollaboratorContracts.cs`); tabela com avatar, nome+cargo, nível, datas, PIX mono xs, salário tabular, chip Ativo/Inativo; demissão em `red-400` se inativo; mutações gated por `AppPolicies.CollaboratorsWrite` (Novo colaborador + Editar em dialog largo); `POST/PUT /api/v1/collaborators`; inativar exige `dismissalDate` explícita; reativar limpa demissão; foto via `photoUrl` (sem upload); override opcional de `CalculationProfile`; níveis filtrados por setor; edição via GET `{id}` antes de abrir o dialog; Manager isolado por `UserDepartments` no backend (403 `collaborators.department_forbidden`); erros pt-BR em `CollaboratorErrorMessages`; datas via `DateFormatter` + `DateInputParser`; testes integração em `WebAPI.Tests/Collaborators/CollaboratorsEndpointTests.cs`; testes bUnit em `Collaborators/CollaboratorApiServiceTests.cs`, `Collaborators/CollaboratorsSectionTests.cs` e `Collaborators/CollaboratorFormDialogTests.cs`; contratos em `docs/design.md`.
  * **Autorização de rotas:** `AuthorizeRouteView` + policies por permissão (`permission:payrolls.read`, etc.); SuperAdmin bypassa checks de permissão.
* **Metodologia Comum:**
  * **TDD:** Testes unitários e de integração obrigatórios (xUnit, FluentAssertions, Moq) **antes** da implementação. O motor de cálculo da folha exige cobertura de cada perfil documentado nas regras de negócio.
  * **Documentação Viva (obrigatória):** Código sem docs atualizados **não está completo**. Toda mudança de comportamento, contrato, permissão, tela ou identidade visual deve atualizar `agents.md` e o arquivo correspondente em `docs/` **na mesma entrega**. Não adiar documentação para “depois”.

### Documentação Viva — regra obrigatória

A documentação do repositório é a fonte da verdade para o agente e para a equipe. Implementar, alterar ou remover comportamento **sem** atualizar a docs é violação deste `agents.md`.

**Obrigatório em toda entrega:**
1. Atualizar este `agents.md` quando mudar stack, invariante, RBAC, estrutura de pastas, endpoint, tela ou ordem de implementação.
2. Atualizar `docs/REGRAS_DE_NEGOCIO.md` quando mudar fórmula, workflow da folha, PIX, tráfego ou caixa.
3. Atualizar `docs/design.md` quando mudar contrato de API, DTO, status HTTP ou rota/página do frontend (criar o arquivo na primeira API).
4. Atualizar `docs/IDENTIDADE_VISUAL.md` quando mudar token, componente visual, layout ou tema.
5. Tabela de endpoints neste arquivo deve refletir o que **existe de fato** (não o plano antigo). Marcar como “implementado” só depois de código + teste.

**Definição de pronto:** testes verdes **e** docs alinhadas ao código. Sem os dois, a etapa não avança (ver seção 6).

### Fora de escopo (domínio)
* Pagamento ao colaborador é via **PIX** (`Collaborator.PixKey`). Não há processamento de cartão, Stripe Checkout ou Connect no domínio de negócio.
* Sem self-signup público — apenas usuários autenticados com permissão adequada cadastram outros usuários.

---

## 2. Segurança, Autenticação e RBAC

### Modelo de Segurança
1. **Controle por Papéis (RBAC dinâmico):**
   * Roles persistidas em `AspNetRoles` via `RoleManager<IdentityRole>` — cadastro dinâmico pela UI/API (não limitadas a um catálogo fixo no seed).
   * Constantes em `AppRoles` (`SuperAdmin`, `Admin`, `Director`, `Financial`, `Manager`, `User`) servem como referência de nomenclatura; demais papéis podem ser criados conforme necessidade.
   * **SuperAdmin Seed:** Na inicialização da API, criar role + usuário `SuperAdmin` (credenciais via `Seed:SuperAdmin:Email` / `Seed:SuperAdmin:Password`, nunca commitadas). SuperAdmin possui acesso irrestrito a endpoints (bypass por role; JWT inclui todas as permission keys).
   * **Papéis de referência (seed):** `Admin`, `Director`, `Financial`, `Manager`, `User` são criados com o mapa de `RolePermission` de §2 via `IdentityDataSeeder` — cadastro dinâmico adicional permanece permitido pela UI/API futura.
   * **Gestão de roles:** endpoints `GET/POST/PUT /api/v1/roles` protegidos por permissões `roles.read` / `roles.write` (SuperAdmin bypass).
   * **Gestão de permissões:** entidade `Permission` + junção `RolePermission`; CRUD em `/api/v1/permissions` protegido por `permissions.read` / `permissions.write`.
   * **Gestão de usuários:** CRUD interno em `/api/v1/users` protegido por `users.read` / `users.write`. O cliente envia `displayName`; `UserName` do Identity é sempre igual ao e-mail. Somente atores com role `SuperAdmin` podem atribuir a role `SuperAdmin` em POST/PUT (403 `users.superadmin_assignment_forbidden` para demais).
   * Role `SuperAdmin` é protegida (não renomeável). Exclusão de roles/permissões fora do escopo atual.
2. **Identity Framework Integration:**
   * Tabela estendida `AppUser : IdentityUser` para `DisplayName`, coleção de setores do gerente (`UserDepartments`) e flags operacionais mínimas. Escopo de gerente **não** usa e-mail hardcoded.
3. **Substituir exceções por e-mail do legado:**
   * `rh@corepay.test` → permissão `collaborators.read` (e write se aplicável), atribuída a um papel/usuário.
   * `trafego@corepay.test` → permissão `traffic.read` / `traffic.write`.
   * Nunca ramificar autorização por endereço de e-mail.

### Papéis de referência e políticas

| Papel | Intenção (seed de RolePermission) |
|---|---|
| `SuperAdmin` | Bypass total. |
| `Admin` | Cadastros mestres, folhas, aprovação, pagamento, tráfego, caixa, usuários. |
| `Director` | Leitura ampla; aprova/reprova folha e colaborador; edita formas de pagamento. Sem CRUD de setores/níveis/projetos. |
| `Financial` | Folhas, financeiro, fluxo de caixa, relatórios, faturamento. Marca pago/NF. Edita folha `draft`/`rejected`. Não aprova. |
| `Manager` | Apenas setores em `UserDepartments`. Cria/edita folhas e colaboradores do seu setor. Sem financeiro/relatórios/fluxo. |
| `User` | Sem menu operacional (apenas autenticação / perfil). |

### Isolamento por setor (equivalente ao RLS do legado)
* Manager só lê/escreve `Payroll` e `Collaborator` dos setores em `UserDepartments`.
* Tentativa de acesso cruzado retorna `Result.Forbidden` (403), nunca 404 mascarado para o próprio recurso quando o id existe fora do escopo — manter contrato consistente. Bloqueios `*.department_forbidden` de Manager são auditados via `ManagerDepartmentAccessAuditMiddleware` (userId + método + rota; sem dados sensíveis).
* Admin, Director e Financial não têm filtro de setor, salvo quando a regra de negócio restringir a ação (ex.: só Admin exclui folha).

### Keys de permissão (seed inicial)

Cadastros: `departments.read|write`, `careerlevels.read|write`, `projects.read|write`, `collaborators.read|write`, `paymentmethods.read|write`.
Folha: `payrolls.read`, `payrolls.write`, `payrolls.approve`, `payrolls.pay`, `payrolls.delete`.
Financeiro: `finance.read`, `revenues.read|write`, `reports.read`.
Métricas de analista: `analystmetrics.read|write`.
Tráfego: `traffic.read|write`.
Caixa: `cashflow.read|write`.
Admin: `users.read|write`, `roles.read|write`, `permissions.read|write`.
Webhook Facilities: endpoint anônimo com HMAC (não usa JWT); não expor write de caixa sem autenticação em outras rotas.

---

## 3. Diretrizes de Ingestão e Regras de Negócio Fundamentais

### 3.1. Regras Globais e Fuso Horário
* **Fuso Horário:** Negócio fixado em `America/Bahia` (`BahiaTimeZone` no backend). Folha, tráfego e caixa usam mês/ano civis neste timezone. O frontend exibe e o backend persiste/interpreta datas neste fuso.
* **Moeda:** BRL. Valores monetários em `decimal` (precisão 18,2 salvo quando a regra exigir mais casas em percentual).
* **Identificadores (GUID obrigatório):** Toda PK de entidade de domínio, FK, `{id}` de rota e campo `id` de DTO é `Guid` (`uniqueidentifier` no SQL Server). **Proibido** `int`/`long` com `IDENTITY` sequencial — evita enumeração de recursos e vazamento de volume. Identity (`AppUser`, roles): o Id é UUID (string GUID do ASP.NET Identity, ou `IdentityUser<Guid>` se a chave for tipada); nunca chave sequencial. Novos Ids: `Guid.NewGuid()` no domínio; Identity gera UUID no create. Códigos de negócio (mês/ano, PIX, permission key) **não** são Ids.
* **Sem Soft Delete genérico:** cadastros mestres usam `IsActive` onde o legado já distinguia inativo (colaborador, projeto). Folha não é apagada por manager — delete = Admin. Colaborador inativo permanece no histórico.
* **Fonte das fórmulas:** [`docs/REGRAS_DE_NEGOCIO.md`](docs/REGRAS_DE_NEGOCIO.md). Alterar cálculo exige teste + atualização desse doc.

### 3.2. Motor de cálculo (`PayrollCalculator`) — invariante crítica
* Implementar em `Core` (domínio/application), **sem** dependência de EF, HTTP ou Blazor. Única fonte de cálculo no backend.
* **Helpers Fase 5.1 (implementado):** `Core/Domain/PayrollCalculation/` — `CalculationProfileResolver` (override → nível → setor), `TrafficCpaRateHelper` + `TrafficCpaKind` (`supervised`/`manager` → taxa Sênior), `ProjectCalculationFlags` e `DepartmentCalculationFlags` (sem regex; IDs estáveis via `SeedKeys`). Testes unitários em `backend/tests/Core.Tests/`.
* **Helper Fase 5.2 (implementado):** `ProportionalFactor.Calculate(admissionDate, dismissalDate, month, year)` — fator proporcional de admissão/demissão por competência (REGRAS §5.1); retorna fração `decimal` 0–1; contagem inclusiva de dias civis; demissão anterior ao mês → 0; nenhuma data relevante no mês → 1. Caller deve passar `Collaborator.DismissalDate` (nunca snapshot de `role_change`). Testes em `ProportionalFactorTests` (7 casos obrigatórios).
* **Motor Fase 5.3 (implementado):** `PayrollCalculator.CalcEntry` em `Core/Domain/PayrollCalculation/` — ramos §6.6–6.8 (`CommissionOnly`, `FixedCommission`, `FixedCommissionBonus`/Affiliates). Contratos: `PayrollEntryInput` (competência, colaborador/setor/nível, `FullBaseSalary`, `GoalTier`, `FinalSalary`, `ProjectEntries`, bônus/descontos) → `Result<PayrollEntryResult>` (`TotalAmount`, `BaseSalary`, `CommissionAmount`). Helpers: `BaseSalaryResolver`, `CommissionPercentSelector`, `CommissionRevenueCalculator`, `ManualAdjustments`. Sem departamento → sucesso com zero; perfis não implementados → `Result.Failure` (`payroll.profile_not_implemented`). `FinalSalary > 0` trava total (Affiliates §6.8); `GoalBonusValue` do nível quando meta atingida. Bônus manuais somam em todos os três perfis (§5.3). Testes em `Core.Tests/PayrollCalculation/` (`CommissionOnlyCalculatorTests`, `FixedCommissionCalculatorTests`, `FixedCommissionBonusCalculatorTests`, `PayrollCalculatorCommissionProfilesTests`).
* **Motor Fase 5.4 (implementado):** ramo §6.9 (`FixedBonus`, `Tipster`, `AllocatedFixed`) via `FixedBonusSectionCalculator` + helpers `FixedAllocationCalculator` (rateio igualitário, `rateio_value` parcial com saldo dividido entre demais, Feira excluída da base de meta, Automação/Contingência → 100% Lima Karttos), `GoalBonusCalculator` (`base × GoalBonusPercentage / 100`; Tipster usa faturamento ou fixo), `TipsterGroupCommissionCalculator` (`Σ group_percentage × R$/1% + floor(%/20) × R$/20%`). Contratos ampliados: `ProjectCalculationSnapshot`, `RateioProjectEntryInput`, `GroupPercentage` em `ProjectEntryInput`; resultado com `GoalBonusAmount` e `GroupCommissionAmount`. Breakdown público de custo por projeto (`CalcProjectTotalsForEntry`) permanece na Fase 6; helpers de rateio já reutilizáveis internamente. Testes em `FixedAllocationCalculatorTests`, `GoalBonusCalculatorTests`, `TipsterGroupCommissionCalculatorTests`, `FixedBonusSectionCalculatorTests`, `PayrollCalculatorFallbackProfilesTests`.
* **Motor Fase 5.5 (implementado):** ramo §6.5 (`ProjectLeader`) via `ProjectLeaderCalculator` + helpers `ProjectLeaderCommissionPercentSelector` (3 faixas: `GoalTier.None`/`Goal`/`SuperGoal`), `ProjectLeaderCommissionCalculator` (líquido 80%, acréscimo de baixo faturamento **por projeto**, teto 2,1%). Contrato: `GoalTier` enum em `PayrollEntryInput`; perfis binários 5.3/5.4 tratam `Goal` e `SuperGoal` como meta atingida. Fixo proporcional + Σ comissões + bônus/descontos manuais. Testes em `ProjectLeaderCommissionPercentSelectorTests`, `ProjectLeaderCommissionCalculatorTests`, `ProjectLeaderCalculatorTests`, `PayrollCalculatorProjectLeaderTests`.
* **Motor Fase 5.6 (implementado):** ramo §6.4 (`Management`) via `ManagementCalculator` + helpers `ManagementCommissionPercentSelector` (binário: `NetRevenuePctNoGoal` / `NetRevenuePctWithGoal`; `Goal` e `SuperGoal` → com meta), `ManagementCommissionCalculator` (por registro: `líquido × (NetRevenueFactor/100) × (pct/100)`, arredondamento monetário por registro antes da soma). Contrato: `ManagementRevenueEntryInput` + `ManagementRevenueEntries` em `PayrollEntryInput`; meta global por linha via `GoalTier`. Fixo proporcional + Σ comissões + bônus/descontos manuais. Breakdown manual por projeto permanece na Fase 6. Testes em `ManagementCommissionPercentSelectorTests`, `ManagementCommissionCalculatorTests`, `ManagementCalculatorTests`, `PayrollCalculatorManagementTests`.
* **Motor Fase 5.7 (implementado):** ramo §6.3 (`PaidTraffic`) via `PaidTrafficCalculator` + helpers `TrafficProjectCommissionCalculator` (investimento × `TrafficInvestmentCommissionPct` + CPA por casa), `TrafficFixedAllocationCalculator` (rateio **igualitário** estrito de `RateioProjectEntries`; rejeita `rateio_value` manual), reutilizando `TrafficCpaRateHelper` (`supervised`/`manager` → snapshot `TrafficSeniorLevel`). Contratos: `TrafficProjectEntryInput`, `TrafficCpaEntryInput`, `TrafficSeniorLevel` em `PayrollEntryInput`. `CommissionAmount` inclui comissão automática **e** bônus manuais (semântica única deste perfil). `traffic_sup_*` e `GoalTier` não entram no cálculo. Falhas CPA/rateio propagadas via `Result`. Testes em `TrafficProjectCommissionCalculatorTests`, `TrafficFixedAllocationCalculatorTests`, `PaidTrafficCalculatorTests`, `PayrollCalculatorPaidTrafficTests`.
* **Motor Fase 5.8 (implementado):** ramo §6.2 (`CommercialSupervisor`) via `CommercialSupervisorCalculator` + helpers `CommercialSupervisorRateSelector` (metas **por projeto**: FTD e vendas), `CommercialSupervisorProjectCommissionCalculator` (FTD Superbet + `max(0, ftd_total − ftd_superbet)` × taxas + % vendas + % Rev analistas + recarga/bonus_cpa; arredondamento monetário por componente/projeto), `SupervisorFixedAllocationCalculator` (fixo proporcional rateado **igualitariamente** entre projetos elegíveis; exclui `ExcludesSupervisorFixedAllocation`), `SupervisorRevAnalistaAllocationCalculator` (Rev analista dividida **igualmente** por **todos** os projetos, inclusive 3C Sports). Contratos: `SupervisorProjectEntryInput`, `SupervisorAnalystRevenue`, `SupervisorProjectEntries` em `PayrollEntryInput`; `ProjectCalculationSnapshot.ExcludesSupervisorFixedAllocation`. `CommissionAmount` = Σ comissões automáticas + `SupervisorAnalystRevenue` (bônus manuais só em `TotalAmount`, como Gerência). `GoalTier` global não entra no cálculo. Breakdown público por projeto permanece na Fase 6. Testes em `CommercialSupervisorRateSelectorTests`, `CommercialSupervisorProjectCommissionCalculatorTests`, `SupervisorFixedAllocationCalculatorTests`, `SupervisorRevAnalistaAllocationCalculatorTests`, `CommercialSupervisorCalculatorTests`, `PayrollCalculatorCommercialSupervisorTests`.
* **Motor Fase 5.9 (implementado):** ramo §6.1 (`CommercialAnalyst`) via `CommercialAnalystCalculator` + helpers `CommercialAnalystRateSelector` (0/1/2 metas FTD e vendas), `CommercialAnalystProjectCommissionCalculator` (`CalcCommercialProject`: FTD iGaming, Superbet, bônus FTD por projeto, CPA, % vendas, bônus vendas, Rev; arredondamento por componente), `CommercialAnalystCombinedFtdBonusCalculator` (extra combinado vs soma por projeto), `CommercialAnalystPlatformCalculator` (Lastlink `% base`; Hubla `min(base, 4%)`; sempre `% base`, não com meta). Contratos: `CommercialAnalystProjectEntryInput`, `CommercialProjectEntries`, `BetanoInternaCount`, `BetanoMundoBetCount` em `PayrollEntryInput`; `PlatformTotal` em `PayrollEntryResult`. Mínimo = `BaseSalary × proporcional`; comparação só com comissão (projetos + Betano Interna + extra FTD); bônus manual fora. `BaseSalary` no resultado = **complemento** (`max(0, mínimo − comissão)`). `TotalAmount` = comissão + bônus + complemento + (`PlatformTotal` se complemento > 0) + Betano Mundo Bet − descontos. Dispatch após Supervisor, antes de Gerência. Breakdown por projeto permanece na Fase 6. Testes em `CommercialAnalystRateSelectorTests`, `CommercialAnalystProjectCommissionCalculatorTests`, `CommercialAnalystCombinedFtdBonusCalculatorTests`, `CommercialAnalystPlatformCalculatorTests`, `CommercialAnalystCalculatorTests`, `PayrollCalculatorCommercialAnalystTests`.
* **Motor Fase 5.10 (implementado):** ramo §5.2 (mudança de cargo) via `RoleChangePayrollCalculator` + helpers `RoleChangePeriodSplitter` (período principal até dia anterior à 1ª `change_date`; mudanças ordenadas por data; último período até demissão real ou fim da competência), `PayrollRoleSnapshot`/`RoleChangeEntryInput`, `PayrollEntryResultMerger` (soma todos os campos numéricos; bônus/desconto **uma vez** no consolidado). Contratos: `RoleChanges`, `PeriodClipStart`/`PeriodClipEnd` em `PayrollEntryInput`; `ProportionalFactor.CalculateForEntry` recorta proporcional por sub-período sem mutar `Collaborator.DismissalDate`. `PayrollCalculator.CalcEntry` despacha §5.2 **antes** do perfil; `CalcEntryCore` executa um período sem recursão. Sub-períodos calculados sem `RoleChanges`, `BonusEntries` ou `DeductionEntries`. Falhas de sub-período propagadas via `Result` fail-fast. Erros: `payroll.role_change_duplicate_date`. Breakdown por período permanece na Fase 6/7. Testes em `RoleChangePeriodSplitterTests`, `PayrollEntryResultMergerTests`, `PayrollCalculatorRoleChangeTests`, `ProportionalFactorTests` (clip).
* **Motor Fase 5.11 (implementado):** `PayrollCalculator.RecalcAllEntries(IReadOnlyList<PayrollEntryInput>)` → `Result<PayrollBatchResult>` (`EntryResults` na ordem de entrada + `TotalAmount` = Σ `PayrollEntryResult.TotalAmount`). Loop fail-fast via `CalcEntry` (reutiliza todos os perfis, §5.2 e validações existentes). Lista vazia → sucesso com total 0. Registro DI: `AddSingleton<PayrollCalculator>()` em `Core/DependencyInjection.cs` (stateless, sem DbContext). Testes em `PayrollCalculatorRecalcAllEntriesTests`, `PayrollCalculatorDiTests`.
* **Motor Fase 6.1 (implementado):** `PayrollCalculator.CalcProjectTotalsForEntry` → `Result<IReadOnlyList<ProjectTotalAllocation>>` — custo por `ProjectId` por linha de colaborador. Dispatch espelha `CalcEntryCore`; `RoleChangeProjectTotalsCalculator` recursão §5.2 + bônus manuais **uma vez** no consolidado. Contratos: `CommissionPayingProjectId`, `ComplementPayingProjects`, `ManagementProjectBreakdownInput` em `ManagementRevenueEntryInput`. Comercial: limiar R$ 100 sobre comissão **bruta**; custo líquido = comissão − plataforma; bucket complemento/Betano/extra FTD/plataforma (se complemento > 0) → `complement_paying_projects` (soma 100%) → pagador → Lima Karttos. Gerência: **somente** breakdown manual. Tráfego/líder/supervisor/rateado/Tipster reutilizam helpers Fase 5; bônus de meta rateado proporcional à parcela de fixo (Feira excluída); comissão de grupo Tipster por `GroupPercentage`. `CommissionOnly`/`FixedCommission`/`FixedCommissionBonus` retornam lista vazia em 6.1 (normalização na Fase 6.2). Erros: `payroll.complement_paying_projects_invalid_sum`, `payroll.complement_allocation_target_missing`. Testes em `PayrollCalculatorProjectTotalsTests`, `ProjectTotalsMergerTests`.
* **Motor Fase 6.2 (implementado):** `PayrollCalculator.GetDisplayProjectEntries(input, affiliatesProjectId)` → `Result<IReadOnlyList<ProjectTotalAllocation>>` — normaliza alocações de custo para exibição/relatório (REGRAS §6.4, §6.8, §6.9). Helper `ProjectDisplayHelper.Transform` reutiliza totais da 6.1 + `CalcEntry`; mudança de cargo via `RoleChangePeriodSplitter` + merge + bônus manuais **uma vez**. **Affiliates** (`FixedCommissionBonus`): 100% no `affiliatesProjectId` explícito (reconciliação `totalAmount + descontos − bônus sem projeto`; sem regex de nome). **Automação/Contingência** (`RoutesFixedToLimaKarttos`): colapsa em uma linha Lima Karttos (`IsDefaultAllocationTarget`). **Gerência:** agrega `ManagementRevenueEntries[].ProjectBreakdown` por `ProjectId` (+ bônus manuais da 6.1). Demais perfis: passthrough dos totais 6.1. `CalcProjectTotalsForEntry` permanece inalterado. Testes em `GetDisplayProjectEntriesTests`.
* **Folha Fase 7.1 (implementado):** agregado `Payroll` + `PayrollCollaboratorEntry` em `Core/Domain/`; cabeçalho relacional (`DepartmentId`, `Month`, `Year`, `PayrollStatus`, `TotalAmount`, campos de workflow reservados) com índice único `(DepartmentId, Month, Year)`; linha com snapshot leve (nome, PIX, admissão, nível, perfil, flags `IsApproved|IsPaid|NfSent`, Betano, `FinalSalary`, etc.) + coluna JSON `Payload` (`PayrollCollaboratorEntryPayload`) reutilizando records de `PayrollCalculation`; `PayrollRoleChangeSnapshot` persistível com FKs (sem entidades EF); store `IPayrollStore`/`PayrollStore` (`SaveAsync`, `GetByIdAsync`); serialização `PayrollJsonOptions`; migration `AddPayrollTransactions`; erro `payrolls.competence_duplicate`; testes integração em `WebAPI.Tests/Payroll/PayrollPersistenceTests.cs`; contratos em `docs/design.md`.
* **Folha Fase 7.2 (implementado):** `GET /api/v1/payrolls` (filtros `search`, `month`, `year`, `status`, `departmentId`; isolamento Manager via `UserDepartments`); `GET /api/v1/payrolls/{id}`; `POST /api/v1/payrolls/{id}/duplicate` → competência **mês seguinte** automática, `draft`, flags zeradas, payload/valores preservados; CQRS em `Core/Application/Payrolls/`; `PayrollsEndpoints`; UI `/payrolls` com `PayrollsSection` (filtros, agrupamento `Jan/2026`, `StatusBadge`, CTA Nova Folha, Duplicar); `PayrollApiService` + formatadores; testes integração `PayrollsEndpointTests.cs`; bUnit `PayrollApiServiceTests.cs`, `PayrollsSectionTests.cs`.
* **Folha Fase 7.3 (implementado):** `GET /api/v1/payrolls/form-options`; `POST /api/v1/payrolls`; `PUT /api/v1/payrolls/{id}` (sync colaboradores, status `draft`/`rejected`); `GET /api/v1/payrolls/{id}` com `entries[]`; `PayrollEntrySnapshotBuilder` + guards escopo/status/colaborador ativo; UI `/payrolls/new` e `/payrolls/{id}/edit` com `PayrollFormShell`; testes integração + bUnit (`PayrollRouteTests`, `PayrollEditabilityGuardTests`).
* **Folha Fase 7.4 (implementado):** `POST /api/v1/payrolls/{id}/entries/{entryId}/preview` (recálculo sem persistência via `PayrollCalculator`); `GET /api/v1/payrolls/{id}` enriquecido com `entries[].payload`, campos editáveis e `editorOptions` (projetos, setores, níveis); `PayrollEntryInputMapper` (plataforma/projeto confiável do backend); UI `/payrolls/{id}/edit` com `PayrollEntryEditor` + blocos por `CalculationProfile` (11 perfis), bônus/descontos, role changes, debounce 400ms; hint Hubla 4%; validação visual complemento ≠ 100%; CSS `.payroll-nested-block*`; testes integração `PayrollsEndpointTests` + bUnit `PayrollEntryEditorTests`, `PayrollApiServicePreviewTests`.
* **Folha Fase 7.5 (implementado):** `PUT /api/v1/payrolls/{id}` estendido com `entries[]` (`UpdatePayrollEntryRequest`) — aplica valores, `RecalcAllEntries`, persiste payload + `Payroll.TotalAmount` em transação atômica; `POST /api/v1/payrolls/{id}/submit` → `pending_approval`, `SubmittedBy` = display name JWT; `PayrollEntryInputMapper.ApplyRequest`/`ToPreviewRequest`; `PayrollAccessContext.DisplayName`; UI `PayrollFormShell` com “Salvar” / “Salvar e submeter”; testes integração + bUnit; contratos em `docs/design.md`.
* **Folha Fase 7.6 (implementado):** tela read-only `/payrolls/{id}` com `PayrollDetailSection`; `GET /api/v1/payrolls/{id}` retorna `entries[].isPaid`, `nfSent`, `result`, `projectTotals`; snapshot calculado em `Payload.CalculatedResult` + `DisplayProjectTotals` (gravado no save/submit); GET de estados travados não recalcula; `draft`/`rejected` calculam em memória se snapshot ausente; isolamento Manager 403; testes integração `PayrollsEndpointTests` + bUnit `PayrollDetailSectionTests`; CSS `.payroll-detail*`; contratos em `docs/design.md`.
* Entrada: snapshot do colaborador + setor + nível + projetos + metas + bônus/descontos + admissão/demissão + `role_changes`.
* Saída: `total_amount`, `base_salary` (no comercial = complemento, não o salário cheio), `commission_amount`, `platform_total`, PIX empresa vs plataforma, breakdown por projeto, flags de meta.
* **TDD obrigatório** por perfil: Analista Comercial (5.9 ✅), Supervisor (5.8 ✅), Tráfego Pago, Gerência, Líder de Projetos, comissão pura, fixo+comissão, Affiliates (`fixed_commission_bonus`), Tipster/rateados, Automação→Lima Karttos, proporcional, mudança de cargo no mês (5.10 ✅).
* **Perfil de cálculo é explícito**, não regex de nome:
  * Enum `CalculationProfile` (ex.: `CommercialAnalyst`, `CommercialSupervisor`, `PaidTraffic`, `Management`, `ProjectLeader`, `CommissionOnly`, `FixedCommission`, `FixedCommissionBonus`, `FixedBonus`, `Tipster`, `AllocatedFixed`).
  * `Department.CalculationType` + `CareerLevel.Profile` (ou override no colaborador) selecionam o perfil.
  * Seed/migração mapeia os nomes atuais do legado para o enum. Regex de nome **não** entra no motor de produção.
* **Folha aprovada/paga trava valores:** persistir snapshot JSON (ou tabelas de linhas calculadas) no momento da aprovação. `GET` de detalhe **não recalcula** `approved`/`paid`. Recálculo só em `draft` / `rejected` / comando explícito `POST .../recalculate` com `payrolls.write`.
* Percentuais: value object ou convenção única. Gerência no legado usa decimal `0.012` = 1,2%; demais usam `2` = 2%. No CorePay padronizar **por cento explícito** (`2.0m` = 2%) no storage novo e converter no seed a partir do legado, com testes de regressão.

### 3.3. Ciclo da folha
Uma folha = **um setor + um mês + um ano** (unique index).

```
draft → pending_approval → approved → paid
                 ↘ rejected → (reedição) → pending_approval
```

* `rejected` exige `rejection_comment`.
* Submeter: `draft`/`rejected` → `pending_approval` (`payrolls.write`).
* Aprovar folha ou colaborador (`is_approved`): `payrolls.approve` (Admin/Director no seed).
* Reprovar: `payrolls.approve` + comentário obrigatório.
* Marcar pago / NF: `payrolls.pay`; folha **não** pode estar `pending_approval`.
* Bônus/desconto após aprovação: só se status = `approved` e `payrolls.pay` ou policy equivalente (Admin/Financial).
* Se todos os colaboradores `is_paid` → folha `paid`. Se algum pago for desmarcado → `approved`.
* Após reprovação, o gerente **não vê** colaboradores já aprovados individualmente ao reeditar.
* Relatórios: folhas `approved`/`paid` incluem todos os colaboradores; senão só `is_approved`. Deduplicar setor+mês+ano pela prioridade `paid > approved > pending_approval > draft/rejected`. Relatórios usam valores **travados**.

### 3.4. Cadastros mestres
* **Department:** `calculation_type`, `goal_bonus_percentage`, `low_revenue_threshold` (padrão R$ 200.000), `low_revenue_bonus_pct` (padrão 0,4). CRUD: `departments.write` (Admin no seed).
* **CareerLevel:** pertence a um setor ou é geral; concentra salários, % comissão, R$/FTD, R$/CPA, fatores de gerência e casas de tráfego. CRUD: `careerlevels.write`.
* **Collaborator:** obrigatório nome + setor; opcional nível, cargo, admissão, demissão, PIX, salário, e-mail, foto, `is_active`. Data de demissão do **cadastro** entra no proporcional — nunca snapshot contaminado por mudança de cargo.
* **Project:** nome, cliente, plataforma (`lastlink` | `hubla`), `is_active`. Plataforma copiada para a linha da folha (teto Hubla 4%). Projeto especial de rateio: **Lima Karttos**.
* **ProjectRevenue:** realização por projeto/mês/ano (`value`, `value_igaming`, `value_vendas`, `group_percentage`). A folha **não** puxa automaticamente — o gestor informa os números na entrada.

### 3.5. PIX empresa vs plataforma (Analista Comercial)
* `total_amount` = rendimento bruto.
* A receber (PIX empresa) = `total_amount − valor pago pela Lastlink/Hubla`.
* Hubla: % da plataforma limitada a **4%** (`HublaMax`), mesmo que o % base do nível seja maior.
* Lastlink: usa o % base do nível (padrão 4%).
* A % da plataforma usa sempre o % base, não a % com meta.

### 3.6. Investimento de tráfego
* `TrafficInvestment`: um registro por projeto/mês/ano, com `monthly_target`.
* Mês com **4 semanas**. Cada semana: depósitos (`pending` | `requested` | `deposited`) e gasto por canal de mídia (Telegram, Instagram, Story, Direto, Remarketing, Outros) — **distinto** de `ProjectPlatform` (Lastlink/Hubla).
* Imposto da semana = arredonda(gasto × **12,15%**, 2 casas). Total = gasto + imposto.
* Saldo semanal = Σ `deposited_amount` − gasto − imposto. `requested_amount` compõe apenas o solicitado (não entra no saldo).
* Sugestão da próxima solicitação: `max(0, meta/4 − saldo)` via `TrafficInvestmentCalculator.GetSuggestedNext`.
* Entidade paralela `TrafficDeposit`: aportes por data, valor por projeto.
* Casas CPA e fórmulas da **folha** §6.3: `TrafficHouse` + `PayrollCalculation` — **não** confundir com o motor de investimento operacional §8.
* Campos `traffic_sup_bonus` e `traffic_sup_commission_pct` podem existir no cadastro do nível, mas **não entram** no cálculo automático até regra nova ser especificada.
* **Motor Fase 11.1 (implementado):** `Core/Domain/TrafficInvestmentCalculation/` — `TrafficInvestmentCalculator`, `TrafficLegacyWeekAdapter`, enums `TrafficMediaChannel`/`TrafficDepositStatus`, constantes `TotalWeeks=4`/`TaxRate=0.1215m`; contratos `TrafficInvestmentInput`/`TrafficWeekInput`/`TrafficDepositInput`/`TrafficChannelSpendInput` → `TrafficInvestmentResult`/`TrafficWeekResult`; legado `requested_amount` semanal → 1 depósito `Requested` via adaptador; erros `traffic.investment_*`; sem EF/HTTP/Blazor. Testes em `Core.Tests/TrafficInvestment/`.

### 3.7. Fluxo de caixa
* Entidade `ProjectCost`. Tipos: `entrada` | `saida`.
* Categorias de entrada: Plataforma, Igaming, Devoluções e reembolso.
* Categorias de saída: Folha de pagamento, Tráfego, Ações, Recargas de banca, Viagens, Imposto, Reembolso, Despesa alimentar, Móveis e equipamentos, Experts, Reforma, Aeronave, Administrativa, Plataformas digitais, Festas e eventos.
* Formas de pagamento: cadastro `PaymentMethod` (`paymentmethods.write` = Admin ou Director no seed).
* Saldo do período: entradas − saídas.

### 3.8. Webhook Facilities (`POST /api/v1/webhooks/facilities/cashflow`) — implementado (12.3)
* Evento aceito: `lancamento.criado` (outros → **422**).
* POST + HMAC-SHA256 (`X-Facilities-Signature` = `sha256=` + hex lowercase de `timestamp + '.' + rawBody`); comparação em tempo constante; janela de replay **±300 segundos**.
* Headers obrigatórios: `X-Facilities-Timestamp`, `X-Facilities-Signature`, `X-Idempotency-Key` (deve ser o mesmo GUID de `lancamento_id`).
* Idempotência: lookup por `FacilitiesLancamentoId` + índice único filtrado; replay → **200** `{ id, lancamentoId, idempotent: true }` sem duplicar.
* Assinatura/timestamp inválidos → **401**; GET na rota → **405**.
* Grava via `ProcessFacilitiesCashflowWebhookHandler` → `CashflowStore.CreateFromFacilitiesWebhookAsync` (sem JWT). Secret em `Facilities:WebhookSecret` (env/config), nunca no frontend.

### 3.9. Notificações
Tipos: `payroll_submitted`, `payroll_approved`, `payroll_rejected`. Destino por `user_id` ou `role_target`. O usuário lê as suas ou as do seu papel.
* **Notificações (Fase 14.1–14.2):** entidade `Notification` + `Payroll.SubmittedByUserId`; emissão atômica em `PayrollStore` (submit → 2 registros `role_target` Director+Admin; approve/reject → `user_id` submetedor); `GET /api/v1/notifications` autenticado retorna `items` + `unreadCount` filtrados por `(user_id OR role_target)`; leitura individual via `NotificationReadReceipt` + `PUT /api/v1/notifications/{id}/read`; UI `/notifications` com `NotificationsSection`, `NotificationApiService`, `NotificationState`, badge `destructive` no `AppHeader`; testes Core + WebAPI + bUnit; contratos em `docs/design.md`.

### 3.10. Métricas de analista
`AnalystMetric`: `FtdTotal` e `CpaCount` inteiros não negativos por colaborador + projeto + competência, com índice único `(CollaboratorId, ProjectId, Month, Year)`. Admin/Manager escrevem, Director lê; Manager fica restrito aos setores em `UserDepartments` (403 fora do escopo). Cadastro histórico sem restrição pelo perfil/estado atual do colaborador; **não** preenche, recalcula ou altera snapshots/totais da folha.

---

## 4. Estrutura de Diretórios Desacoplada

O repositório reflete a independência total dos sistemas:

```text
├── docker-compose.yml          # SQL Server (e Redis, se aplicável)
├── agents.md
├── docs/
│   ├── REGRAS_DE_NEGOCIO.md    # fórmulas e workflow (fonte da verdade de negócio)
│   ├── IDENTIDADE_VISUAL.md    # tokens, tema e receitas de UI
│   ├── roadmap.md              # ordem detalhada de implementação
│   └── design.md               # contratos de API + rotas do frontend
├── backend/
│   ├── CorePay.slnx
│   ├── src/
│   │   ├── BuildingBlocks/     # Domain base, Result<T>, BahiaTimeZone, Money/Percentage
│   │   ├── Core/               # Entities, enums CalculationProfile, CQRS, PayrollCalculator, validators, PayrollCalculation helpers (Fase 5.1), Payroll agregado (Fase 7.1)
│   │   ├── Infrastructure/     # EF Core DbContext, Identity, PayrollStore, Facilities webhook HMAC, file storage
│   │   └── WebAPI/             # Minimal APIs, API Versioning, Swagger, JWT, Migration Runner
│   └── tests/                  # xUnit: BuildingBlocks.Tests, Core.Tests (motor puro), WebAPI.Tests (integração)
└── frontend/
    ├── CorePay.Frontend.slnx
    ├── src/
    │   └── WebApp.Blazor/
    │       ├── Components/Layout/   # AppSidebar, AppHeader, SidebarTrigger (Fase 2.3)
    │       ├── Components/Ui/       # Button, StatusBadge, StatCard, Card, Input, Select, Tabs, Dialog, GoalToggle, Avatar, Spinner, EmptyState, ThemeToggle, Icon (Lucide)
    │       ├── Auth/                # contratos login, policies permission:*, session storage
    │       ├── Components/Auth/     # RedirectToLogin, AccessDenied, LogoutButton
    │       ├── Components/Payroll/  # linhas de projeto comercial, gerência, rateio
│       ├── Components/Financial/ # financeiro (9.1)
    │       ├── Components/Traffic/    # investimento de tráfego (11.2)
│       ├── Components/Cashflow/   # fluxo de caixa (12.1–12.2)
    │       ├── Components/Admin/    # administração (Usuários 3.5)
    │       ├── Components/Collaborators/ # lista de colaboradores (4.1)
    │       ├── Components/Settings/ # cadastros mestres em Configurações (Setores 3.2, Níveis 3.3, Projetos 3.4)
    │       ├── Components/Notifications/ # notificações (14.2)
    │       ├── Layout/              # MainLayout, EmptyLayout
    │       ├── Pages/               # Login, Home, Dev/Tokens, Payroll/, Financial/, Traffic/, CashFlow/, Settings/
    │       └── Services/            # ThemeService, SidebarState, AuthService, CorePayAuthenticationStateProvider, AuthorizationMessageHandler
    └── tests/                       # bUnit frontend
```

### Endpoints (v1)

Inventário canônico — **83 rotas** mapeadas em `backend/src/WebAPI/Endpoints/` (registradas via `Program.cs`). Detalhes de contrato e rotas frontend: [`docs/design.md`](docs/design.md).

**Identificadores:** recursos expostos por rota usam **GUID** (`{id:guid}`). Usuários e papéis Identity recebem `Id = Guid.NewGuid().ToString()` na criação; permissões e setores usam `Guid` nativo. Sem IDs sequenciais expostos.

#### Infraestrutura

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| GET | `/api/v1/health` | Anônimo | implementado |

#### Autenticação

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| POST | `/api/v1/auth/login` | Anônimo | implementado |

#### Administração (roles, permissions, users)

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| GET | `/api/v1/roles` | `roles.read` (+ SuperAdmin bypass) | implementado |
| POST | `/api/v1/roles` | `roles.write` | implementado |
| GET | `/api/v1/roles/{id}` | `roles.read` | implementado |
| PUT | `/api/v1/roles/{id}` | `roles.write` | implementado |
| GET | `/api/v1/permissions` | `permissions.read` | implementado |
| POST | `/api/v1/permissions` | `permissions.write` | implementado |
| GET | `/api/v1/permissions/{id}` | `permissions.read` | implementado |
| PUT | `/api/v1/permissions/{id}` | `permissions.write` | implementado |
| GET | `/api/v1/users` | `users.read` | implementado |
| POST | `/api/v1/users` | `users.write` | implementado |
| GET | `/api/v1/users/{id}` | `users.read` | implementado |
| PUT | `/api/v1/users/{id}` | `users.write` | implementado |
| DELETE | `/api/v1/users/{id}` | `users.write` | implementado |

#### Cadastros mestres

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| GET | `/api/v1/departments` | `departments.read` | implementado |
| POST | `/api/v1/departments` | `departments.write` | implementado |
| GET | `/api/v1/departments/{id}` | `departments.read` | implementado |
| PUT | `/api/v1/departments/{id}` | `departments.write` | implementado |
| GET | `/api/v1/career-levels` | `careerlevels.read` | implementado |
| POST | `/api/v1/career-levels` | `careerlevels.write` | implementado |
| GET | `/api/v1/career-levels/{id}` | `careerlevels.read` | implementado |
| PUT | `/api/v1/career-levels/{id}` | `careerlevels.write` | implementado |
| GET | `/api/v1/projects` | `projects.read` | implementado |
| POST | `/api/v1/projects` | `projects.write` | implementado |
| GET | `/api/v1/projects/{id}` | `projects.read` | implementado |
| PUT | `/api/v1/projects/{id}` | `projects.write` | implementado |
| GET | `/api/v1/payment-methods` | `paymentmethods.read` | implementado |
| POST | `/api/v1/payment-methods` | `paymentmethods.write` | implementado |
| GET | `/api/v1/payment-methods/{id}` | `paymentmethods.read` | implementado |
| PUT | `/api/v1/payment-methods/{id}` | `paymentmethods.write` | implementado |
| DELETE | `/api/v1/payment-methods/{id}` | `paymentmethods.write` | implementado |
| GET | `/api/v1/collaborators` | `collaborators.read` | implementado (4.1–4.2) |
| POST | `/api/v1/collaborators` | `collaborators.write` | implementado (4.1–4.2) |
| GET | `/api/v1/collaborators/{id}` | `collaborators.read` | implementado (4.1–4.2) |
| PUT | `/api/v1/collaborators/{id}` | `collaborators.write` | implementado (4.1–4.2) |

#### Folha

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| GET | `/api/v1/payrolls` | `payrolls.read` | implementado (7.2) |
| GET | `/api/v1/payrolls/form-options` | `payrolls.read` | implementado (7.3) |
| GET | `/api/v1/payrolls/{id}` | `payrolls.read` | implementado (7.2–8.2) |
| POST | `/api/v1/payrolls` | `payrolls.write` | implementado (7.3) |
| PUT | `/api/v1/payrolls/{id}` | `payrolls.write` | implementado (7.3 + 7.5) |
| POST | `/api/v1/payrolls/{id}/duplicate` | `payrolls.write` | implementado (7.2) |
| DELETE | `/api/v1/payrolls/{id}` | `payrolls.delete` | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/submit` | `payrolls.write` | implementado (7.5) |
| POST | `/api/v1/payrolls/{id}/approve` | `payrolls.approve` | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/reject` | `payrolls.approve` | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/pay` | `payrolls.pay` | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/entries/{entryId}/preview` | `payrolls.write` (somente `draft`/`rejected`) | implementado (7.4) |
| POST | `/api/v1/payrolls/{id}/recalculate` | `payrolls.write` (somente `draft`/`rejected`) | implementado (8.2) |
| PUT | `/api/v1/payrolls/{id}/entries/{entryId}` | `payrolls.write` ou `payrolls.pay` conforme status | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/entries` | `payrolls.write` (colaborador avulso; bloqueado em `pendingApproval`) | implementado (9.4) |
| POST | `/api/v1/payrolls/{id}/entries/{entryId}/approve` | `payrolls.approve` | implementado (8.2) |
| POST | `/api/v1/payrolls/{id}/entries/{entryId}/pay` | `payrolls.pay` | implementado (8.2) |
| PUT | `/api/v1/payrolls/{id}/entries/{entryId}/nf` | `payrolls.pay` | implementado (8.2) |

#### Financeiro, faturamento, métricas, relatórios, tráfego, caixa, notificações, dashboard

| Método | Rota | Auth | Status |
|--------|------|------|--------|
| GET | `/api/v1/finance/summary` | `finance.read` | implementado (9.1) |
| GET | `/api/v1/project-revenues` | `revenues.read` | implementado (10.1) |
| POST | `/api/v1/project-revenues` | `revenues.write` | implementado (10.1) |
| GET | `/api/v1/project-revenues/{id}` | `revenues.read` | implementado (10.1) |
| PUT | `/api/v1/project-revenues/{id}` | `revenues.write` | implementado (10.1) |
| GET | `/api/v1/analyst-metrics` | `analystmetrics.read` | implementado (15.1) |
| POST | `/api/v1/analyst-metrics` | `analystmetrics.write` | implementado (15.1) |
| GET | `/api/v1/analyst-metrics/{id}` | `analystmetrics.read` | implementado (15.1) |
| PUT | `/api/v1/analyst-metrics/{id}` | `analystmetrics.write` | implementado (15.1) |
| GET | `/api/v1/reports/payroll` | `reports.read` | implementado (13.2) |
| GET | `/api/v1/reports/payroll/export` | `reports.read` | implementado (13.3) |
| GET | `/api/v1/traffic-investments` | `traffic.read` | implementado (11.2) |
| POST | `/api/v1/traffic-investments` | `traffic.write` | implementado (11.2) |
| GET | `/api/v1/traffic-investments/{id}` | `traffic.read` | implementado (11.2) |
| PUT | `/api/v1/traffic-investments/{id}` | `traffic.write` | implementado (11.2) |
| GET | `/api/v1/traffic-deposits` | `traffic.read` | implementado (11.2) |
| POST | `/api/v1/traffic-deposits` | `traffic.write` | implementado (11.2) |
| GET | `/api/v1/cashflow` | `cashflow.read` | implementado (12.1) |
| POST | `/api/v1/cashflow` | `cashflow.write` | implementado (12.1) |
| GET | `/api/v1/cashflow/report` | `cashflow.read` | implementado (12.2) |
| GET | `/api/v1/cashflow/installments/{compraId}` | `cashflow.read` | implementado (12.1) |
| GET | `/api/v1/cashflow/{id}` | `cashflow.read` | implementado (12.1) |
| PUT | `/api/v1/cashflow/{id}` | `cashflow.write` | implementado (12.1) |
| DELETE | `/api/v1/cashflow/{id}` | `cashflow.write` | implementado (12.1) |
| POST | `/api/v1/webhooks/facilities/cashflow` | HMAC Facilities (anônimo JWT) | implementado (12.3) |
| GET | `/api/v1/webhooks/facilities/cashflow` | Anônimo | implementado (12.3) — **405** Method Not Allowed |
| GET | `/api/v1/notifications` | Autenticado (próprias + `role_target`) | implementado (14.1) |
| PUT | `/api/v1/notifications/{id}/read` | Autenticado (recibo individual) | implementado (14.2) |
| GET | `/api/v1/dashboard` | Autenticado — widgets conforme permissões | implementado (13.1) |

### Telas frontend (paridade com o legado)
* Login, Home/Dashboard.
* Folhas (lista + detalhe com workflow).
* Colaboradores, Setores, Níveis, Projetos.
* Financeiro (PIX empresa vs plataforma, pago/NF).
* Faturamento de projeto.
* Métricas de analista.
* Investimento de tráfego (semanas, depósitos, imposto).
* Fluxo de caixa.
* Relatórios.
* Configurações (formas de pagamento).
* Administração: usuários, papéis, permissões.

Sidebar oculta itens sem a permissão correspondente (Manager sem financeiro/relatórios/caixa).

---

## 5. Integração Contínua e Startup (Backend)

No `Program.cs` do `WebAPI`:
1. Inicializar o `dbContext.Database.MigrateAsync()` (ou `EnsureCreatedAsync` em ambiente `Testing`).
2. Executar `IdentityDataSeeder.SeedAsync()` — seed idempotente das permission keys + papéis de referência com `RolePermission` + usuário `SuperAdmin` (`UserName` = e-mail).
3. Em ambiente `Development` e somente por opt-in: `Seed:LoadFixtures=true` executa `DevelopmentFixtureSeeder` (dados mestres); `Seed:LoadDemoData=true` executa `DevelopmentDemoDataSeeder` (usuários por papel + cenário operacional E2E e também garante fixtures). Ambas usam `SeedEntities`/`SeedKeys`, são idempotentes e nunca rodam em Production. A senha dos usuários demo vem de `DEV_ROLE_USERS_PASSWORD` ou `Seed:RoleUsersPassword`, nunca do código.
4. Configurar autenticação JWT e habilitar o middleware de versionamento de API `builder.Services.AddApiVersioning(...)`.
5. Registrar `PayrollCalculator` como serviço de domínio stateless (sem DbContext).
6. Configurar validação HMAC do webhook Facilities (raw body).

`docker-compose.yml` sobe o SQL Server local. Connection string (`ConnectionStrings:DefaultConnection`), `Jwt:Key`, `Facilities:WebhookSecret`, `Seed:SuperAdmin:*` e `DEV_ROLE_USERS_PASSWORD` vêm de configuração/ambiente (`.env`, export ou user-secrets), nunca commitados em `appsettings*.json`.

**JWT (login):** claims `sub`, e-mail, nome, `ClaimTypes.Role` por papel e claim `permission` por permissão efetiva. SuperAdmin recebe todas as permission keys no token; demais papéis recebem apenas as vinculadas em `RolePermission`.

---

## 6. Ordem de implementação sugerida

Sequência detalhada (subfases, critérios de pronto, testes): [`docs/roadmap.md`](docs/roadmap.md). Resumo:

1. BuildingBlocks (`Result<T>`, `BahiaTimeZone`) + WebAPI health + Identity/JWT + SuperAdmin seed.
2. Identidade visual Blazor + RBAC (roles, permissions, users) + shell (login, sidebar, policies).
3. Cadastros mestres (departments, career-levels, projects, collaborators, payment-methods).
4. `PayrollCalculator` + rateio + testes de todos os perfis (antes das APIs de folha).
5. CRUD de folha, snapshot na aprovação, workflow e isolamento do manager.
6. Financeiro + faturamento + relatórios (valores travados).
7. Tráfego (semanas, imposto 12,15%, sugestão de depósito).
8. Fluxo de caixa + webhook Facilities (HMAC, replay, idempotência).
9. Notificações e dashboard.

Não avançar de etapa sem testes verdes **e** documentação viva atualizada da etapa anterior.
