# Regras de negócio — FolhaPay (CorePay)

Documento extraído do código em vigor (`src/lib/payrollCalc.js`, entidades Base44, páginas de folha/financeiro/tráfego/fluxo de caixa).  
Valores de taxa listados como “padrão” são fallbacks do código quando o nível de carreira não tem o campo preenchido. A fonte da verdade operacional é o cadastro em **Configurações**.

---

## 1. O que o sistema faz

Aplicação interna para:

- montar e aprovar **folha de pagamento mensal por setor**;
- calcular **salário, comissão, bônus, desconto e rateio por projeto**;
- controlar **pagamento PIX, NF e custo por projeto**;
- registrar **faturamento** (iGaming + vendas);
- gerir **investimento de tráfego pago** (meta, depósitos, gasto, imposto);
- lançar **fluxo de caixa** (entradas/saídas), inclusive via webhook do Facilities.

Pagamento ao colaborador é via **PIX** (`Collaborator.pix_key`). Não há processamento de cartão no domínio de negócio.

---

## 2. Papéis e permissões

### 2.1 Papéis (`User.role`)

| Papel | Escopo |
|---|---|
| `admin` | Acesso total. Cria/edita/exclui setores, níveis, projetos, usuários, folhas. Aprova e paga. |
| `director` | Vê tudo. Aprova/reprova folha e colaborador. Não edita cadastros mestres (exceto formas de pagamento). |
| `financial` | Folhas, financeiro, fluxo de caixa, relatórios, faturamento. Marca pago/NF. Edita folha em rascunho/reprovada. Não aprova. |
| `manager` | Só os setores em `department_ids` (legado: `department_id`). Cria/edita folhas e colaboradores do seu setor. Não vê financeiro/relatórios/fluxo. |
| `user` | Sem menu operacional. |

### 2.2 Exceções por e-mail

- `rh@corepay.test` — menu **Colaboradores** mesmo sem papel `admin`/`manager`.
- `trafego@corepay.test` — menu **Investimento Tráfego** mesmo sem papel `admin`.

### 2.3 Quem pode o quê na folha

| Ação | Quem |
|---|---|
| Criar/editar folha (`draft` ou `rejected`) | manager (seu setor), admin, financial |
| Submeter para aprovação | quem edita (status vira `pending_approval`) |
| Aprovar folha inteira | admin, director |
| Aprovar colaborador individual (`is_approved`) | admin, director |
| Reprovar (exige comentário) | admin, director |
| Marcar pago / NF | admin, financial (folha **não** pode estar `pending_approval`) |
| Bônus/desconto na folha já aprovada | admin, financial e só se status = `approved` |
| Excluir folha | admin |
| Gerente ver folha de outro setor | bloqueado |

Após reprovação, o gerente **não vê** colaboradores já aprovados individualmente ao reeditar.

### 2.4 RLS (backend)

- **Department / CareerLevel / Project:** create/update/delete = admin; read = admin, manager, director, financial.
- **Payroll:** manager só no próprio setor; delete = admin.
- **Collaborator:** financial lê; não cria/edita/exclui. Manager só no próprio setor.
- **Notification:** o usuário lê as suas ou as do seu `role_target`.

---

## 3. Cadastros mestres

### 3.1 Setor (`Department`)

Campo `calculation_type` define a fórmula-base (pode ser **sobrescrita** pelo nome do cargo/setor — ver §5):

| Tipo | Significado |
|---|---|
| `fixed_bonus` | Salário fixo + bônus % da meta (+ regras Tipster/rateio) |
| `commission` | Só comissão % sobre faturamento dos projetos (sem salário) |
| `fixed_commission` | Fixo + comissão % (com/sem meta) |
| `fixed_commission_super_goal` | Fixo + comissão com meta e super meta (Líder de Projetos) |
| `fixed_commission_bonus` | Fixo + comissão + bônus R$ de meta; ou salário travado (`final_salary`) |
| `management` | Fixo + % sobre faturamento líquido (gerência/diretoria) |

Campos extras do setor:

- `goal_bonus_percentage` — % de bônus ao bater meta (setores `fixed_bonus`).
- `low_revenue_threshold` — limiar de faturamento do líder (padrão **R$ 200.000**).
- `low_revenue_bonus_pct` — acréscimo % abaixo do limiar (padrão **0,4**).

### 3.2 Nível de carreira (`CareerLevel`)

Pertence a um setor (`department_id`) ou é geral. Concentra salários, % de comissão, R$/FTD, R$/CPA, fatores de gerência e casas de tráfego.

A detecção de **Analista Comercial**, **Supervisor** e **Tráfego** no cálculo usa o **nome** do nível/setor (regex), não um flag.

### 3.3 Colaborador

Obrigatório: nome + setor. Opcional: nível, cargo, admissão, demissão, PIX, salário, e-mail, foto, `is_active`.

Colaborador inativo (`is_active = false`) permanece no histórico; a data de demissão entra no cálculo proporcional.

### 3.4 Projeto

Nome, cliente, plataforma (`lastlink` | `hubla`), `is_active`.  
A plataforma do projeto é copiada para a linha da folha e afeta o teto de % da Hubla.

Projeto especial de rateio: **Lima Karttos** (setores Automação / Contingência e fallback do complemento comercial).

---

## 4. Ciclo da folha

Uma folha = **um setor + um mês + um ano**.

### 4.0 Dados mínimos na criação

- **Folha de pagamento:** exige ao menos **um colaborador ativo do setor** em create, update (não pode remover o último), duplicate (origem vazia é rejeitada), submit e approve. Erro: `payrolls.entries_required`.
- **Faturamento (`ProjectRevenue`):** `value_igaming + value_vendas` deve ser **> 0** (não basta competência/projeto preenchidos). Erro: `projectrevenues.values_required`.
- **Investimento de tráfego:** exige **meta mensal > 0** ou ao menos um **depósito/gasto positivo** nas semanas. Erro: `trafficinvestments.content_required`.
- **Aporte por data (`TrafficProjectDeposit`):** `amount > 0`. Erro: `trafficdeposits.amount_required`.
- **Usuário Manager:** ao menos **um setor** em `department_ids`. Erro: `users.manager_departments_required`.
- **Papel dinâmico (POST/PUT roles):** ao menos **uma permissão** (`permissionKeys` não vazio). Papéis de referência (`SuperAdmin`, `Admin`, etc.) mantêm regras próprias. Erro: `roles.permissions_required`.
- **Fluxo de caixa (saída):** valor > 0, forma de pagamento e setor obrigatórios (backend + formulário). Entrada: projeto permanece opcional.
- **Forma de pagamento:** nome obrigatório no formulário.

### 4.1 Status

```
draft → pending_approval → approved → paid
                 ↘ rejected → (reedição) → pending_approval
```

- `rejected` exige `rejection_comment`.
- Se **todos** os colaboradores forem marcados `is_paid`, a folha vira `paid`.
- Se algum pago for desmarcado, volta para `approved` / `payment_status: pending`.
- Cada colaborador tem `is_approved`, `is_paid`, `nf_sent` independentes.

### 4.1.1 Duplicar folha

- Ação disponível na lista (`payrolls.write`): copia todas as entradas e o payload JSON para a **competência seguinte** do mesmo setor (dez/2025 → jan/2026).
- Nova folha sempre `draft`; zera `is_approved`, `is_paid`, `nf_sent` em cada linha e campos de workflow do cabeçalho (`submitted_by`, `approved_by`, etc.).
- Preserva valores de entrada, snapshots e `total_amount` da origem (sem recalcular na duplicação).
- Se já existir folha para setor+mês+ano destino → erro `payrolls.competence_duplicate`.
- A folha origem permanece intacta (ex.: `paid` continua `paid`).

### 4.2 Quem entra no financeiro e nos relatórios

- Folha `approved` ou `paid`: **todos** os colaboradores.
- Caso contrário: só os com `is_approved = true`.
- Relatórios deduplicam por setor+mês+ano, ficando a folha de maior prioridade: `paid` > `approved` > `pending_approval` > `draft`/`rejected`.
- Folhas **aprovadas/pagas** nos relatórios e na tela de detalhe usam valores **travados** (snapshot persistido na aprovação). `GET` de detalhe **não recalcula** em `pendingApproval`/`approved`/`paid`. Recálculo explícito só em `draft`/`rejected` (`POST .../recalculate`) ou ajuste pós-aprovação de bônus/desconto (`PUT .../entries/{entryId}` com `payrolls.pay`).

#### Relatório anual de folha (13.2)

- Endpoint: `GET /api/v1/reports/payroll?year=&departmentId=&projectId=` (`reports.read`). Sem `year`, usa o ano civil atual em `America/Bahia`.
- Visão anual: cards `totalYear` (bruto travado), `monthlyAverage = totalYear / 12`, maior setor, maior projeto e count de colaboradores distintos nas linhas incluídas.
- Matrizes por setor, projeto (`DisplayProjectTotals`) e colaborador; série mensal com 12 pontos.
- Com filtro `projectId`, todas as métricas usam somente o valor alocado naquele projeto.
- Manager (ou papel custom com `reports.read`) vê somente setores em `UserDepartments`.
- Relatório **nunca** invoca recálculo; snapshot ausente em linha elegível é erro de integridade.

#### Exportação XLSX (13.3)

- Endpoint: `GET /api/v1/reports/payroll/export?year=&departmentId=&projectId=` (`reports.read`).
- Reutiliza a mesma elegibilidade, dedup, snapshots travados, filtros e isolamento Manager do relatório JSON (13.2).
- O arquivo contém resumo, série mensal e as três matrizes agregadas (setor, projeto, colaborador) — **não** linhas granulares ocultas na UI.
- Permissão: `reports.read` (sem permissão separada de export).

#### Dashboard

- Competência padrão: mês civil anterior no fuso `America/Bahia`.
- Nas contas da competência, folhas `draft` são ignoradas. “Aprovadas” agrega `approved` + `paid`.
- Valores usam somente folhas `approved`/`paid` e o snapshot persistido das linhas, sem recálculo.
- `Total a pagar` = Σ `max(0, total_amount − platform_total)` das linhas ainda não pagas; `Total pago` aplica a mesma fórmula às linhas com `is_paid = true`.
- Manager vê folhas e colaboradores ativos somente dos setores vinculados em `UserDepartments`.
- “Folhas recentes” é global, independente da competência filtrada: até 8 registros de qualquer status, inclusive `draft`, ordenados por competência decrescente e setor.
- Widgets de folha exigem `payrolls.read`; colaboradores ativos exige `collaborators.read`. Usuário autenticado sem essas permissões não recebe os respectivos dados.

#### Colaborador avulso (Financeiro)

- Ator com `payrolls.write` (ex.: Financial) pode adicionar colaborador **já cadastrado** a uma folha existente via `POST /api/v1/payrolls/{id}/entries`.
- A nova linha entra com **`is_approved = true`** imediatamente — exceção à aprovação individual por Director/Admin (`payrolls.approve`).
- Aparece na lista a pagar do financeiro mesmo quando a folha está em `draft`, `rejected`, `approved` ou `paid` (desde que atenda §4.2).
- **Não** permitido enquanto a folha está em `pending_approval`.
- Snapshots das linhas **já existentes** permanecem intactos; só a nova linha recebe cálculo persistido.
- Se a folha estava `paid` e a nova linha ainda não está paga, o status da folha volta para `approved`.

### 4.3 Notificações

Tipos: `payroll_submitted`, `payroll_approved`, `payroll_rejected`. Encaminhadas por `user_id` ou `role_target`.

| Evento | `type` | Destinatário |
|---|---|---|
| Folha submetida | `payroll_submitted` | `role_target`: **Director** e **Admin** (um registro por papel) |
| Folha aprovada | `payroll_approved` | `user_id` do gestor que submeteu (`SubmittedByUserId`) |
| Folha reprovada | `payroll_rejected` | `user_id` do gestor que submeteu (`SubmittedByUserId`) |

Campos persistidos: `title`, `message`, `payroll_id`, `user_id` e/ou `role_target`, `is_read`, `created_at`.

Visibilidade (RLS): o usuário lê as suas (`user_id`) **ou** as do seu `role_target` (§2.4). Cada nova submissão após reprovação emite nova notificação. Aprovação individual de colaborador **não** emite evento.

---

## 5. Regras transversais de cálculo

Implementação legado: `src/lib/payrollCalc.js` → `calcEntry`.  
**CorePay:** ordem por perfil explícito (`CalculationProfile`) em `PayrollCalculator.CalcEntry` — ver `docs/roadmap.md` Fase 5 e matriz §15.4 abaixo. O motor **não** usa regex de nome.

A ordem de decisão **legada** **não** é só `calculation_type`. O código legado testa nesta sequência:

1. Mudança de cargo no mês (`role_changes`)
2. Setor **Tráfego Pago** (nome)
3. Cargo **Supervisor** (nome do nível)
4. Cargo **Analista Comercial** (nome do nível, excluindo supervisor)
5. `calculation_type === 'management'`
6. `calculation_type === 'fixed_commission_super_goal'` (Líder)
7. `commission` / `fixed_commission` / `fixed_commission_bonus`
8. Demais (`fixed_bonus` e equivalentes): Tipster, setores rateados, etc.

### 5.1 Proporcional de admissão/demissão

`proportionalFactor(admissão, demissão, mês, ano)`:

- Sem as duas datas, ou nenhuma delas no mês de referência → fator **1**.
- Demissão **antes** do mês de referência → fator **0** (não paga).
- Admissão e/ou demissão no mês: `dias trabalhados / dias do mês` (inclusive o dia inicial e o final).
- Data de demissão usada no recálculo: **sempre a do cadastro do colaborador**, nunca um snapshot contaminado por mudança de cargo.

Salário base proporcional: `full_base_salary × fator`  
(`full_base_salary` = salário do nível, ou o informado na entrada).

### 5.2 Mudança de cargo no mês

`role_changes[]` ordenado por `change_date`:

- Período principal: admissão → **dia anterior** à 1ª mudança.
- Cada mudança: `change_date` → dia anterior à próxima (ou demissão real).
- Cada período calcula com o **setor e nível daquele cargo**, com os projetos daquele período.
- Bônus e descontos manuais da entrada principal aplicam-se **uma vez**, no total consolidado (não em cada período).
- O total da folha = soma dos períodos + bônus − descontos.

### 5.3 Bônus e descontos manuais

- `bonus_entries`: valor + projeto + justificativa. Somam no total e no custo do projeto informado.
- `deduction_entries`: valor + descrição/justificativa. Subtraem do total. **Não** são rateados por projeto.
- **Total agregado da folha** = soma dos `total_amount` de todos os colaboradores da competência (`RecalcAllEntries` no CorePay). Distinto do rendimento bruto de uma linha isolada.

### 5.4 PIX da empresa vs. plataforma (Analista Comercial)

- **Total da folha** (`total_amount`) = rendimento bruto do colaborador.
- **A receber (PIX da empresa)** = `total_amount − valor pago pela Lastlink/Hubla`.
- Hubla: % da plataforma limitada a **4%** (`HUBLA_MAX`), mesmo que o `% base` do nível seja maior.
- Lastlink: usa o `% base` do nível (padrão **4%**).
- A % da plataforma usa sempre o `% base`, não a % com meta.

---

## 6. Fórmulas por perfil

### 6.1 Analista Comercial

Detectado por nome do nível: `/analista comercial/i` e **não** supervisor.

**Por projeto** (`calcCommercialProject`):

| Componente | Regra | Padrão |
|---|---|---|
| FTD iGaming | `ftd_total − ftd_superbet` × taxa |  |
| Taxa FTD iGaming | 0 meta / 1 meta / 2 metas | R$ 2,00 / 2,50 / 3,00 |
| Metas FTD | pessoal (`is_ftd_goal_reached`) **e** projeto (`is_project_ftd_goal_reached`) | 1 meta = uma das duas; 2 = ambas |
| FTD Superbet | quantidade × taxa própria (não entra no iGaming) | R$ 5,00 |
| Bônus FTD do projeto | `floor(FTDs iGaming / X) × valor` | a cada 250 → R$ 350 |
| CPA | `cpa_count × default_cpa_value` |  |
| % vendas | 0 / 1 / 2 metas (pessoal + projeto) | 4% / 5% / 6% |
| Bônus vendas | `floor(vendas / X) × valor` | a cada R$ 20.000 → R$ 250 |
| Rev | `rev × %` | 1% |

Betano **não** entra no total do projeto; entra no total do colaborador:

- **Betano Interna:** R$ **200**/un. — **conta** para o mínimo garantido.
- **Betano Mundo Bet:** R$ **70**/un. — **não conta** para o mínimo.

**Bônus FTD combinado:** soma FTDs iGaming de **todos** os projetos; aplica a faixa sobre o total. Se o combinado for maior que a soma dos bônus por projeto, a diferença (`_extraFtdBonus`) é paga no total do colaborador.

**Mínimo garantido:**

- Base = `base_salary do nível × proporcional`.
- Compara **somente** a comissão da empresa (projetos + Betano Interna + extra FTD). **Bônus manual não entra** na comparação — é sempre pago por cima.
- `complemento = max(0, mínimo − comissão)`.
- `base_salary` gravado na entrada **é o complemento**, não o salário cheio.

**Total do colaborador:**

```
comissão + bônus manual + complemento
+ (se houver complemento: valor Lastlink/Hubla)
+ Betano Mundo Bet
− descontos
```

**Quando há complemento:** a empresa cobre o mínimo integralmente; o valor da plataforma é somado ao total (e depois abatido no PIX) e alocado no projeto pagador.  
**Quando não há complemento:** a empresa paga a comissão **menos** o que Lastlink/Hubla já pagou.

**Custo por projeto (comercial):**

- Comissão do projeto **menos** o pedaço da plataforma (custo líquido).
- O limiar de **R$ 100** compara a comissão **bruta** do projeto (antes do abate da plataforma). Se &lt; R$ 100 e houver `commission_paying_project_id`, o **custo líquido** vai para esse **projeto pagador** (não para o projeto de origem).
- Complemento + Betano Interna + Betano Mundo Bet + extra FTD combinado + (plataforma, se houver complemento) formam um bucket alocado via:
  1. `complement_paying_projects` (rateio por %; a soma deve ser 100%), ou
  2. o mesmo `commission_paying_project_id`, ou
  3. **Lima Karttos** (`IsDefaultAllocationTarget`) se nenhum pagador foi escolhido.
- **Extra FTD combinado** não duplica bônus por projeto — entra somente no bucket de complemento.
- **Descontos** reduzem `total_amount` mas **não** são rateados por projeto (exceto alocação manual na gerência).
- **Bônus manuais** com `project_id` somam no custo daquele projeto; sem projeto, entram só no total.
- **Invariante de reconciliação:** Σ custos por projeto ≈ `total_amount` + Σ descontos − Σ bônus sem projeto (tolerância de arredondamento R$ 0,01).

**Custo por projeto (rateado / Tipster — §6.9):**

- Fixo via `FixedAllocationCalculator`; Feira recebe só parcela do fixo (sem bônus de meta).
- Bônus de meta distribuído **proporcionalmente** à parcela de fixo de cada projeto elegível (não-Feira).
- Tipster: fixo via `rateio_project_entries` (fallback `project_entries`); comissão de grupo alocada por `group_percentage` (faixa VIP proporcional).

**Custo por projeto (gerência — §6.4):** somente `project_breakdown` manual informado pelo usuário em cada registro de faturamento líquido.

---

### 6.2 Supervisor (comercial)

Detectado por nome do nível: `/sup(ervisor)?\b/i`.

**Salário:** `base_salary × proporcional` (fixo, não é mínimo garantido).

**Por projeto** (`calcSupervisorProject`):

| Componente | Sem meta do projeto | Com meta |
|---|---|---|
| R$/FTD Superbet | 4,00 (ou `sup_ftd_superbet_no_goal`) | 5,00 |
| R$/FTD demais casas | 0,30 | 0,50 |
| % sobre faturamento bruto de vendas | 0,5% | 0,8% |
| % da Rev dos analistas | 10% (`sup_rev_pct`) | 10% |

FTD demais casas = `max(0, ftd_total − ftd_superbet)`.  
Meta de FTD do **projeto** (`is_project_ftd_goal_reached`) vale para Superbet e demais.  
Meta de vendas do **projeto** (`is_project_sales_goal_reached`) vale para o %.

Extras por projeto (valor direto em R$): `recarga_aparelho`, `bonus_cpa`.

**Rev da época de analista:** `supervisor_rev_analista` é um valor único **dividido igualmente** por todos os projetos.

**Total:** fixo + comissões dos projetos + Rev analista + bônus − descontos.

**Rateio do fixo:** o salário fixo **não** é atribuído a projetos cujo nome casa com `/3c\s*sports/i`. A Rev analista **é** dividida inclusive nesses projetos.

---

### 6.3 Tráfego Pago

**Legado:** detectado pelo nome do setor `/tráfego pago|trafego pago/i`.  
**CorePay:** perfil explícito `CalculationProfile.PaidTraffic` (override colaborador → nível → setor). O motor **não** usa regex.

**Salário:** fixo × proporcional. Rateado **estritamente igualitário** pelos projetos em `rateio_project_entries` (lista à parte, só para o fixo). `rateio_value` manual **não** é aceito no tráfego (`traffic.manual_rateio_not_allowed`).

**Por projeto de comissão** (`traffic_project_entries`):

```
comissão investimento = arredonda(valor investido × traffic_investment_commission_pct / 100, 2 casas)
comissão CPA         = Σ arredonda(qtd × R$/CPA da casa, 2 casas) por linha
total projeto        = investimento + CPA
```

Casas (`TRAFFIC_HOUSES`): Esportiva, Stake, Betano, BetMGM, Novibet, BetFair, Blaze, Superbet, Hiperbet.  
Campo no nível: `traffic_cpa_<key>`.

CPA tem tipo:

- `supervised` — usa a taxa do **nível atual** da entrada.
- `manager` — usa a taxa do nível **Sênior** do mesmo setor, fornecido como snapshot `TrafficSeniorLevel` na entrada (camada de aplicação resolve via seed/cadastro; o domínio não busca por regex de nome).

Legado: um único `betting_house` + `cpa_count` vira uma entrada em `cpa_entries`.

**Total:** fixo + comissões + bônus − descontos.  
`commission_amount` no tráfego inclui **comissão automática + bônus manuais** (semântica distinta dos demais perfis).

**Erros de negócio** propagados via `Result<PayrollEntryResult>`: `traffic.invalid_house`, `traffic.invalid_cpa_kind`, `traffic.senior_level_not_found`, `traffic.manual_rateio_not_allowed`.

**Exemplo de aceite (roadmap 5.7):** investido R$ 10.000 @ 2% + 3 CPA Betano @ R$ 50 → comissão = R$ 200 + R$ 150 = **R$ 350**.

> Campos `traffic_sup_bonus` e `traffic_sup_commission_pct` existem no cadastro do nível, mas **não entram** no cálculo automático atual.

---

### 6.4 Gerência (`calculation_type = management`)

`calcManagementEntry`:

```
base_por_registro = faturamento_líquido × (net_revenue_factor / 100)
                    fator 50 = gerente; 100 = diretora
pct = net_revenue_pct_with_goal se meta atingida
    = net_revenue_pct_no_goal senão
comissão_por_registro = base × (pct / 100)   ← CorePay: por cento explícito
comissão_total = Σ comissão_por_registro (arredondamento monetário por registro)
```

**Legado:** `pct` era decimal direto (ex.: `0.012` = 1,2%) — não dividia por 100 de novo.

**CorePay:** storage e motor usam **por cento explícito** (`2.0m` = 2%, `1.2m` = 1,2%). Regressão: líquido 100.000, factor 50, pct 2% → comissão R$ 1.000 (equivalente ao legado com `0.02`).

Cada registro de faturamento líquido tem `project_breakdown`: o usuário **aloca manualmente** comissão (+ parcela do fixo − parcela do desconto) entre projetos. O custo por projeto da gerência vem **só** desse breakdown, não da lista de projetos da folha.

**Total:** fixo + comissões + bônus − descontos.

---

### 6.5 Líder de Projetos

**CorePay:** perfil explícito `CalculationProfile.ProjectLeader` (legado: `fixed_commission_super_goal`; detecção por regex **não** entra no motor).

**Contrato de meta:** enum `GoalTier` na entrada da folha — `None` | `Goal` | `SuperGoal`. Apenas este perfil distingue as três faixas; perfis binários (§6.6–6.8) tratam `Goal` e `SuperGoal` como meta atingida.

**Por projeto** (acréscimo de baixo faturamento avaliado **individualmente** em cada `ProjectEntryInput.Value`):

```
faturamento líquido = faturamento × 0,80     (retém 20%)
% base              = sem meta / com meta / super meta (do CareerLevel)
acréscimo baixo fat.= se faturamento do projeto < limiar do setor: + low_revenue_bonus_pct
% final             = min(% base + acréscimo, 2,1%)     teto 2,10%
comissão_projeto    = líquido × % final / 100   (arredondada em moeda; soma-se ao total)
```

Limiar padrão: R$ 200.000 (`Department.LowRevenueThreshold`). Acréscimo padrão: 0,40 p.p. (`Department.LowRevenueBonusPct`). Igual ao limiar → **sem** acréscimo.

**Total:** fixo proporcional + Σ comissões por projeto + bônus manuais − descontos manuais. Não usa `GoalBonusValue` nem `Department.GoalBonusPercentage`.

No custo por projeto (Fase 6): `(fixo / qtd projetos) + comissão do projeto`.

---

### 6.6 Comissão pura (`commission`)

```
comissão = soma(valores dos projetos) × (com meta ? commission_with_goal : commission_without_goal) / 100
total    = comissão + bônus manuais − descontos
```

Sem salário. Sem proporcional. Bônus manuais entram no total (§5.3).

---

### 6.7 Fixo + comissão (`fixed_commission`)

```
total = (fixo × proporcional) + comissão % sobre faturamento + bônus manuais − descontos
```

`goal_reached` da **entrada** (não por projeto) escolhe a %. Bônus manuais entram no total (§5.3).

---

### 6.8 Fixo + comissão + bônus de meta (`fixed_commission_bonus`) — Affiliates

Se `final_salary > 0` (salário travado após aprovação):

```
total = final_salary + bônus manuais − descontos
```

Não recalcula fixo/comissão. Impede o recálculo automático voltar ao salário-base.

Senão:

```
total = fixo proporcional
      + comissão % sobre faturamento
      + (goal_reached ? goal_bonus_value do nível : 0)
      + bônus manuais
      − descontos
```

Custo: setor Affiliates atribui **tudo** ao projeto “Affiliates” (`getDisplayProjectEntries`).

---

### 6.9 Demais setores (`fixed_bonus` e fallback)

```
total = fixo proporcional + bônus de meta + bônus manuais + comissão de grupo (Tipster) − descontos
```

**Bônus de meta** (`goal_reached` e `dept.goal_bonus_percentage`):

Quando `goal_reached = true` e `goal_bonus_percentage > 0`:

```
bônus_meta = base × (goal_bonus_percentage / 100)
```

Percentual em **pontos percentuais explícitos** (`10` = 10%), distinto de `CareerLevel.GoalBonusValue` (R$ fixo — §6.8 Affiliates).

| Situação | Base do bônus |
|---|---|
| Setor rateado **com** projeto “Feira” | fixo − (parcela do fixo dos projetos Feira) — Feira **não** recebe bônus de meta |
| Setor rateado sem Feira | o fixo inteiro |
| **Tipster** | faturamento dos projetos; se zero, o fixo |
| Outros | o fixo |

Setores **rateados** (nome casa com, e **não** é Líder de Projetos):  
`suporte`, `social media`, `feira`, `assessoria`, `audiovisual`, `administrativo`, `automação`, `i.a`, `t.i` (e cargo com “suporte”).

Rateio do custo:

- Divisão igualitária do fixo, **ou** `rateio_value` manual na linha do projeto (valor absoluto em R$).
- **`rateio_value` parcial:** reserva os valores manuais; o saldo do fixo proporcional divide-se **igualmente** entre os projetos sem valor informado. Rejeitar se a soma dos manuais exceder o fixo.
- Projeto Feira: só a parcela do fixo (sem fatia do bônus de meta).
- Demais projetos: parcela do fixo + fatia do bônus de meta.

**Tipster — comissão de grupo:**

- Soma `group_percentage` dos projetos daquele colaborador.
- `group_commission_per_percent`: R$ × cada 1%.
- `group_commission_per_20_percent`: R$ × `floor(% / 20)` (faixas VIP).
- Rateio do **fixo** usa `rateio_project_entries`; se vazio, usa os próprios `project_entries`.

**Automação / Contingência:** o salário fixo inteiro vai para o projeto **Lima Karttos**, sem rateio.

---

## 7. Faturamento de projeto

Entidade `ProjectRevenue`, por projeto/mês/ano:

- **Unicidade:** no máximo um registro por `(project_id, month, year)`.
- `value_igaming`, `value_vendas` — valores monetários ≥ 0 (BRL, 2 casas); **pelo menos um deve ser > 0** (`value_igaming + value_vendas > 0`).
- `value` = `value_igaming + value_vendas` — calculado e persistido no backend; o cliente **não** envia `value`.
- `group_percentage` = % do grupo sobre o faturamento (0–100; contexto Tipster).
- `notes` — observações opcionais (até 2048 caracteres).

É cadastro de realização; a folha **não** puxa automaticamente esses valores — o gestor informa os números na entrada do colaborador.

**Permissões:** leitura `revenues.read` (Admin, Director, Financial, Manager); escrita `revenues.write` (Admin, Financial, Manager). Sem DELETE na API — correções via PUT.

**Exemplo:** iGaming R$ 1.000,00 + vendas R$ 500,00 → `value` R$ 1.500,00 na competência filtrada.

---

## 8. Investimento de tráfego

Entidade `TrafficInvestment`: um registro por projeto/mês/ano, com `monthly_target`.

### 8.1 Semanas

O mês tem **4 semanas** (`TOTAL_WEEKS = 4`). Cada semana tem:

- vários **depósitos** (`requested_amount`, `deposited_amount`, status `pending` | `requested` | `deposited`);
- **gasto** por canal de mídia: Telegram, Instagram, Story, Direto, Remarketing, Outros (distinto de `Project.platform` Lastlink/Hubla).

**Semântica dos depósitos (CorePay):**

- **`deposited_amount`** — único valor que entra no **saldo** semanal (`Σ deposited_amount − gasto − imposto`).
- **`requested_amount`** — estatística de solicitado; **não** altera saldo nem sugestão, independentemente do status.
- Status (`pending` | `requested` | `deposited`) é metadado de workflow (UI Fase 11.2); o motor 11.1 usa os valores numéricos.

**Legado:** semana com `requested_amount` no nível da semana e sem lista de depósitos → normalizar para **1 depósito** com `requested_amount` preenchido, `deposited_amount = 0`, status `requested` (`TrafficLegacyWeekAdapter`).

### 8.2 Imposto

```
imposto da semana = arredonda(gasto × 12,15%, 2 casas)
total da semana   = gasto + imposto
```

### 8.3 Sugestão da próxima solicitação

```
base semanal = meta mensal / 4
gasto        = Σ gastos por canal de mídia da semana
imposto      = arredonda(gasto × 12,15%, 2)
saldo        = Σ deposited_amount − gasto − imposto
sugestão     = max(0, base semanal − saldo)
```

Se o saldo já cobre a base semanal, sugere **0** (não pede depósito à toa).

**Exemplos de aceite (motor 11.1):** meta 4000, sem depósito efetivo, gasto 0 → sugestão **1000** (base semanal); Telegram 1000 → imposto **121,50**, total semana **1121,50**; saldo ≥ base semanal → sugestão **0**.

Entidade paralela `TrafficDeposit`: aportes por **data**, com valor por projeto (visão consolidada, distinta das semanas do card).

---

## 9. Fluxo de caixa

Entidade `ProjectCost`. Tipos: `entrada` | `saida`.

**Categorias de entrada:** Plataforma, Igaming, Devoluções e reembolso.

**Categorias de saída:** Folha de pagamento, Tráfego, Ações, Recargas de banca, Viagens, Imposto, Reembolso, Despesa alimentar, Móveis e equipamentos, Experts, Reforma, Aeronave, Administrativa, Plataformas digitais, Festas e eventos.

Campos de saída: forma de pagamento, setor, solicitante, local de compra, parcela (`1/3`), `compra_id` (grupo de parcelas), anexo, observações.

Saldo do período: `entradas − saídas`. Formas de pagamento são cadastro próprio (`PaymentMethod`); create/update/delete = admin ou director.

**Relatório de caixa (12.2):** competência = mês/ano civis (timezone Bahia). Agregação por **projeto** (entradas, saídas, saldo e contagem); lançamentos sem `projectId` compõem o bucket **Sem projeto**. Agregação por **forma de pagamento** considera **somente saídas** (entradas não possuem método). Cada parcela entra na competência da própria linha (`Month`/`Year`). Summary global = soma das entradas − soma das saídas no período filtrado.

### 9.1 Webhook Facilities (`fluxoCaixaWebhook`)

Evento aceito: `lancamento.criado` (outros eventos → **422**).

- POST + HMAC-SHA256 (`X-Facilities-Signature` = `sha256=` + hex lowercase de `timestamp + '.' + rawBody`); comparação em tempo constante.
- Headers obrigatórios: `X-Facilities-Timestamp` (Unix seconds), `X-Facilities-Signature`, `X-Idempotency-Key` (mesmo GUID de `lancamento_id`).
- Janela de replay: **±300 segundos**; assinatura/timestamp inválidos → **401**; GET na rota → **405**.
- Idempotência: se já existe `ProjectCost` com `FacilitiesLancamentoId = lancamento_id`, responde **200** `{ id, lancamentoId, idempotent: true }` sem duplicar.
- Payload canônico: `evento`, `lancamento_id`, `dados` (`tipo`, `categoria`, `valor`, `data_lancamento`, IDs opcionais de projeto/setor/forma de pagamento, textos opcionais). Competência (`Month`/`Year`) derivada de `data_lancamento`; uma linha por evento (sem parcelamento).
- Grava via serviço de aplicação (sem usuário logado). Secret em `Facilities:WebhookSecret`.

---

## 10. Métricas de analista

`AnalystMetric` registra `ftd_total` e `cpa_count` como contagens inteiras não negativas por colaborador + projeto + competência. Há no máximo um registro para cada combinação `(collaborator_id, project_id, month, year)`.

- Mês válido: 1–12; ano: 2000–2100.
- Admin e Manager podem cadastrar/editar; Director consulta. Manager fica restrito aos setores em `UserDepartments` e acesso direto fora do escopo retorna 403.
- O cadastro aceita histórico independentemente do perfil atual ou estado ativo do colaborador.
- É um cadastro auxiliar: não preenche entradas, não recalcula e não altera snapshots ou totais da folha. O gestor continua informando FTD/CPA manualmente na entrada comercial.

---

## 11. Identificadores por nome (frágeis)

> **CorePay:** o motor de cálculo **não** usa regex de nome. Perfil efetivo vem de `CalculationProfile` (override do colaborador → perfil do nível → perfil do setor); projetos especiais usam flags (`IsDefaultAllocationTarget`, `ExcludesGoalBonus`, `ExcludesSupervisorFixedAllocation`) e IDs estáveis do seed (`SeedKeys`); setores rateados/automação usam `IsAllocatedFixed` / `RoutesFixedToLimaKarttos`; Affiliates usa `CalculationProfile.FixedCommissionBonus` + projeto com ID explícito. A tabela abaixo documenta o **legado** (FolhaPay / `payrollCalc.js`).

Estas regras **quebram** se o nome do setor/cargo/projeto mudar:

| Conceito | Padrão no código |
|---|---|
| Analista Comercial | `/analista comercial/i` |
| Supervisor | `/sup(ervisor)?\b/i` |
| Líder de Projetos | `/l[ií]deres? de projetos/i` |
| Tráfego Pago | `/tr[áa]fego pago/i` |
| Tipster | nome **exato** `Tipster` |
| Setor rateado | `suporte\|social media\|feira\|assessoria\|audiovisual\|administrativo\|automação\|i\.a\|t\.?i` |
| Automação/Contingência → Lima Karttos | `/automação\|contingência/i` |
| Projeto Feira (sem bônus de meta) | `/feira/i` no nome do projeto |
| 3C Sports (supervisor sem rateio do fixo) | `/3c\s*sports/i` |
| Lima Karttos | `/lima\s*karttos/i` |
| Affiliates (custo 100% num projeto) | `/affiliat/i` no setor |
| Nível Sênior de tráfego | `/s[eê]nior/i` |

Ao criar setor/cargo/projeto novo, ou ao renomear, validar se alguma regex acima precisa ser atualizada em `payrollCalc.js` e `ProjectCostBreakdown.jsx`.

---

## 12. Onde está cada regra no código

| Regra | Arquivo |
|---|---|
| Orquestração da folha, proporcional, rateio | `src/lib/payrollCalc.js` |
| Comercial e supervisor (por projeto) | `src/components/CommercialProjectRow.jsx` |
| Gerência | `src/components/ManagementProjectRow.jsx` |
| Casas e R$/CPA de tráfego | `src/lib/trafficHouses.js` |
| Semanas, imposto 12,15%, sugestão de depósito | `src/lib/trafficExpense.js` |
| Display/custo por projeto (Affiliates, Lima Karttos) | `src/components/ProjectCostBreakdown.jsx` |
| PIX da empresa vs. plataforma | `src/pages/Financial.jsx` |
| Workflow aprovar/reprovar/pagar | `src/pages/PayrollDetail.jsx` |
| Permissões de menu | `src/components/Layout.jsx` |
| Esquemas e RLS | `base44/entities/*.jsonc` |
| Webhook de caixa | `base44/functions/fluxoCaixaWebhook/entry.ts` |

---

## 13. Inconsistências conhecidas (código × cadastro)

1. `traffic_sup_bonus` e `traffic_sup_commission_pct` são configuráveis e **não são usados** em `calcTrafficProject` / `calcEntry`.
2. **Resolvido no CorePay (Fase 8.2):** detalhe de folha `pendingApproval`/`approved`/`paid` lê snapshot persistido; não recalcula ao abrir.
3. **Resolvido no CorePay (Fase 15.4):** perfil por enum `CalculationProfile` (override → nível → setor); renomear setor/cargo/projeto **não** altera cálculo; flags/IDs estáveis (`SeedKeys`) substituem regex — ver §11 e §15.4.
4. **Resolvido no CorePay (Fase 5.6):** Gerência passou a usar por cento explícito (`2.0m` = 2%) como os demais perfis; teste de regressão garante paridade numérica com o legado (`0.02` decimal).

---

## 15.4 Execução do checklist de paridade de negócio (Fase 15.4)

**Escopo:** §5.1–5.4 transversais + §6.1–6.9 por perfil, com fixtures da Fase 0.3 (`docs/roadmap.md` §0.3 / `docs/design.md` §Seed). Mecanismo CorePay: `CalculationProfile` explícito, flags de projeto/setor e `SeedKeys` — **sem regex** no motor (§11).

**Validação automatizada (Core + integração):** `BusinessParityChecklistTests`, suíte `Core.Tests/PayrollCalculation/*`, `PayrollCalculationFixturesTests`, `ExplicitProfileParityTests`.

**Critério-chave (comportamento novo vs legado):** renomear o setor fixture `"Tráfego Pago"` **não** altera `CalcEntry` — perfil permanece `PaidTraffic`; aceite numérico R$10.000×2% + 3 CPA Betano×R$50 = **R$350** de comissão (`ExplicitProfileParityTests`, `PayrollCalculatorPaidTrafficTests`).

### Matriz de auditoria

| Regra | Fixture 0.3 | Mecanismo CorePay | Teste principal | OK |
|---|---|---|---|---|
| §5.1 proporcional | Bruno Tráfego (demissão 15/03/2025) | `ProportionalFactor` | `ProportionalFactorTests` | ✅ |
| §5.2 mudança de cargo | — (input sintético) | `RoleChangePayrollCalculator` | `PayrollCalculatorRoleChangeTests`, `RoleChangePeriodSplitterTests` | ✅ |
| §5.3 bônus/desconto | — | `ManualAdjustments` | `*CalculatorTests`, `PayrollCalculatorRecalcAllEntriesTests` | ✅ |
| §5.4 PIX/plataforma | Projetos Lastlink/Hubla Demo | `CommercialAnalystPlatformCalculator` | `CommercialAnalystPlatformCalculatorTests` | ✅ |
| §6.1 comercial | Analistas + nível Júnior | `CommercialAnalyst` | `PayrollCalculatorCommercialAnalystTests`, `CommercialAnalystProjectCommissionCalculatorTests` | ✅ |
| §6.2 supervisor | nível Supervisor | `CommercialSupervisor` | `PayrollCalculatorCommercialSupervisorTests` | ✅ |
| §6.3 tráfego | Tráfego Pago + Sênior | `PaidTraffic` | `PayrollCalculatorPaidTrafficTests`, `PaidTrafficCalculatorTests` | ✅ |
| §6.4 gerência | Setor Gerência | `Management` | `PayrollCalculatorManagementTests`, `ManagementCommissionCalculatorTests` | ✅ |
| §6.5 líder | Líderes de Projetos | `ProjectLeader` | `PayrollCalculatorProjectLeaderTests` | ✅ |
| §6.6–6.8 comissão/fix/bonus | Affiliates | `CommissionOnly` / `FixedCommission` / `FixedCommissionBonus` | `PayrollCalculatorCommissionProfilesTests` | ✅ |
| §6.9 tipster/rateado | Tipster, Administrativo, IA Automação | `Tipster` / `AllocatedFixed` + flags | `PayrollCalculatorFallbackProfilesTests`, `FixedBonusSectionCalculatorTests` | ✅ |
| §6.1 custo por projeto | Lima Karttos, pagador, complemento | `ComplementAllocationCalculator`, `CommercialAnalystProjectTotalsCalculator` | `ComplementAllocationCalculatorTests`, `PayrollCalculatorProjectTotalsTests` | ✅ |
| §11 perfil por nome → enum | Tráfego Pago **renomeado** | `CalculationProfileResolver` | `CalculationProfileResolverTests`, `ExplicitProfileParityTests` | ✅ |
| §11 projetos especiais | Lima Karttos, Feira X, 3C Sports, Affiliates | flags + `SeedKeys` | `ProjectAndDepartmentFlagsTests`, `GetDisplayProjectEntriesTests`, `PayrollCalculationFixturesTests` | ✅ |
| §11 CPA manager | Sênior (tráfego) | `TrafficCpaRateHelper` + snapshot | `TrafficCpaRateHelperTests`, `PayrollCalculationFixturesTests` | ✅ |
| §13.1 `traffic_sup_*` ignorados | — | `PaidTrafficCalculator` | `PaidTrafficCalculatorTests`, `BusinessParityChecklistTests` | ✅ |

### Pronto quando

Matriz §5–6 + §11 preenchida; renomear `"Tráfego Pago"` não altera cálculo PaidTraffic (350); fixtures 0.3 mapeadas; `dotnet test CorePay.slnx` verde — **validado**.
