# CorePay — Visão Geral do Projeto

Documento introdutório para entender **o que é**, **como funciona** e **quais fluxos** o sistema segue.  
Para implementação e fórmulas detalhadas, consulte os demais arquivos em `docs/`.

---

## 1. O que é o CorePay

O **CorePay** é um sistema interno do **Grupo Royalty** para gestão de pagamentos e finanças operacionais. Ele substitui o legado **FolhaPay** (Base44 / SPA JavaScript) com uma arquitetura moderna, mas **preservando o mesmo resultado numérico e os mesmos workflows de negócio**.

Em resumo, o sistema permite:

| Área | O que faz |
|---|---|
| **Folha de pagamento** | Montar, calcular, aprovar e pagar a remuneração mensal por setor |
| **Colaboradores** | Cadastrar pessoas, cargos, PIX, salários e perfis de cálculo |
| **Financeiro** | Controlar o que a empresa deve pagar via PIX, NF e custo por projeto |
| **Faturamento** | Registrar receita por projeto (iGaming + vendas) |
| **Tráfego pago** | Gerir metas, depósitos, gastos e impostos de investimento em mídia |
| **Fluxo de caixa** | Lançar entradas e saídas, com integração via webhook |
| **Relatórios** | Consolidar dados aprovados/pagos para análise |

**Pagamento ao colaborador:** sempre via **PIX** (chave cadastrada no colaborador). Não há cobrança automática, Stripe ou checkout — o financeiro **marca manualmente** o que foi pago.

**Locale:** português do Brasil, moeda `R$ 1.234,56`, datas `dd/MM/yyyy`, fuso `America/Bahia`.

---

## 2. Arquitetura em uma frase

Backend (.NET 10) e frontend (Blazor WebAssembly) **totalmente desacoplados**, comunicando-se apenas por **API REST** em `/api/v1/...`.

```
┌─────────────────┐         HTTP + JWT          ┌─────────────────┐
│  Blazor WASM    │  ◄──────────────────────►  │  WebAPI (.NET)  │
│  (navegador)    │         /api/v1/...         │  + Core (domínio)│
└─────────────────┘                             └────────┬────────┘
                                                         │
                                                         ▼
                                                ┌─────────────────┐
                                                │   SQL Server    │
                                                └─────────────────┘
```

### Stack principal

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10, C# 14, Minimal APIs, EF Core 10, MediatR (CQRS) |
| Frontend | Blazor WebAssembly .NET 10 |
| Banco | SQL Server (Docker local) |
| Auth | JWT + ASP.NET Core Identity |
| Testes | xUnit, FluentAssertions, bUnit (frontend) |

### Princípios arquiteturais importantes

1. **Cálculo no domínio** — toda fórmula de folha vive no `PayrollCalculator` (projeto `Core`), nunca na tela.
2. **IDs sempre Guid** — nenhuma chave sequencial (`int`/`long` IDENTITY).
3. **Autorização por permissão** — nunca por e-mail (`rh@`, `trafego@` do legado viraram permissões).
4. **Perfil de cálculo explícito** — enum `CalculationProfile`, não regex no nome do cargo.
5. **Folha aprovada trava valores** — após aprovação, os números não são recalculados ao consultar.
6. **TDD** — testes verdes + documentação atualizada = subfase concluída.

---

## 3. Módulos do sistema

### 3.1 Cadastros mestres (Configurações)

Base de tudo. Sem isso, não há cálculo correto.

| Entidade | Para que serve |
|---|---|
| **Setor** (`Department`) | Define o tipo de cálculo (comercial, tráfego, gerência, etc.) e regras de bônus |
| **Nível de carreira** (`CareerLevel`) | Salário, comissões, taxas FTD/CPA, fatores de gerência, casas de tráfego |
| **Projeto** (`Project`) | Cliente, plataforma (Lastlink/Hubla), flags de rateio (Feira, Lima Karttos, 3C Sports) |
| **Forma de pagamento** (`PaymentMethod`) | Usada no fluxo de caixa |
| **Usuário** (`AppUser`) | Login, papéis e setores vinculados (para gerentes) |

**Rota frontend:** `/settings` (hub) e `/admin/users` (usuários).

### 3.2 Colaboradores

Cadastro de quem recebe. Campos principais: nome, setor, nível, cargo, admissão, demissão, PIX, salário, ativo/inativo.

- Colaborador **inativo** permanece no histórico; a data de demissão entra no cálculo proporcional.
- Gerente só vê/edita colaboradores dos **seus setores** (`UserDepartments`).

**Rota frontend:** `/collaborators`

### 3.3 Folha de pagamento

Coração do sistema. Uma folha = **um setor + um mês + um ano**.

Cada folha contém **entradas de colaboradores** com:
- projetos e metas (FTD, vendas, faturamento);
- bônus e descontos manuais;
- mudanças de cargo no mês (`role_changes`);
- totais calculados pelo motor.

**Rotas frontend:** `/payrolls`, `/payrolls/new`, `/payrolls/{id}`, `/payrolls/{id}/edit`

### 3.4 Financeiro

Lista o que precisa ser pago após aprovação e exibe custo por projeto. Para analistas comerciais, separa:
- **Total bruto** da folha;
- **Valor da plataforma** (Lastlink/Hubla);
- **A receber via PIX da empresa** = total − plataforma.

Abas: **Colaboradores** (pagamentos PIX/NF) e **Custo por Projeto** (breakdown por colaborador → projetos, via motor Fase 6).

**Rota frontend:** `/financial`

### 3.5 Faturamento

Registro de receita por projeto/mês (iGaming + vendas). **Não alimenta a folha automaticamente** — o gestor digita os números na entrada da folha.

**Rota frontend:** `/project-revenues`

### 3.6 Investimento de tráfego

Controle mensal por projeto: meta, 4 semanas de gasto por plataforma, imposto (12,15%), depósitos e sugestão de próximo aporte.

**Rota frontend:** `/traffic-investment`

### 3.7 Fluxo de caixa

Lançamentos manuais de entrada/saída com categorias fixas. Saldo = entradas − saídas. Aceita webhook do Facilities para criar lançamentos automaticamente.

**Rota frontend:** `/cashflow`

### 3.8 Dashboard e relatórios

Visão consolidada: folhas recentes, totais a pagar, colaboradores ativos. Relatórios usam **valores travados** (não recalculam folhas aprovadas/pagas).

**Rotas frontend:** `/` (dashboard), `/reports`

---

## 4. Papéis e quem faz o quê

O acesso é controlado por **papéis** (`Role`) + **permissões** (`Permission`), não por cargo ou e-mail.

| Papel | Escopo típico |
|---|---|
| **SuperAdmin** | Acesso total (bypass de permissões) |
| **Admin** | Cadastros, folhas, aprovação, pagamento, tráfego, caixa, usuários |
| **Director** | Vê tudo; aprova/reprova folha; edita formas de pagamento; **não** edita setores/níveis/projetos |
| **Financial** | Folhas, financeiro, caixa, relatórios, faturamento; marca pago/NF; **não aprova** |
| **Manager** | Só os setores vinculados; cria/edita folhas, colaboradores e **faturamento** do seu setor; **sem** financeiro/relatórios/caixa |
| **User** | Autenticado, sem menu operacional |

### Isolamento por setor (Manager)

O gerente só acessa folhas e colaboradores dos setores em `UserDepartments`. Tentativa de acesso a outro setor retorna **403 Forbidden** — nunca um 404 disfarçado.

### Permissões principais (exemplos)

| Permissão | Permite |
|---|---|
| `payrolls.read` | Ver folhas |
| `payrolls.write` | Criar/editar folha em rascunho ou reprovada |
| `payrolls.approve` | Aprovar/reprovar folha ou colaborador |
| `payrolls.pay` | Marcar pago, NF, bônus pós-aprovação |
| `collaborators.read/write` | Listar/cadastrar colaboradores |
| `finance.read` | Tela financeira |
| `traffic.read/write` | Investimento de tráfego |
| `cashflow.read/write` | Fluxo de caixa |
| `departments.write` | CRUD de setores |

O menu lateral do Blazor **oculta** itens sem permissão, e rotas digitadas na URL também são bloqueadas.

---

## 5. Fluxo da folha de pagamento

Este é o fluxo central do sistema.

### 5.1 Ciclo de status

```
                    ┌──────────────────┐
                    │      draft       │  ← rascunho (editável)
                    └────────┬─────────┘
                             │ submeter
                             ▼
                    ┌──────────────────┐
         reprovar   │ pending_approval │  ← aguardando diretoria
        ┌───────────│                  │
        │           └────────┬─────────┘
        │                    │ aprovar
        ▼                    ▼
┌──────────────┐    ┌──────────────────┐
│   rejected   │    │     approved     │  ← valores travados (snapshot)
└──────┬───────┘    └────────┬─────────┘
       │ reeditar           │ marcar todos pagos
       └──────────────────► │ pending_approval
                             ▼
                    ┌──────────────────┐
                    │       paid       │
                    └──────────────────┘
```

### 5.2 Passo a passo operacional

1. **Manager ou Financial** cria folha (`draft`) para setor + mês + ano.
2. Adiciona colaboradores do setor e preenche dados (projetos, metas, FTDs, vendas, etc.).
3. A cada alteração, o **motor recalcula** os totais (via API — a UI não implementa fórmulas).
4. **Submete** → status vira `pending_approval`; notificação vai para Director/Admin.
5. **Director/Admin** aprova (snapshot gravado) ou reprova (comentário obrigatório).
6. Se reprovada, gerente reabre e edita — **colaboradores já aprovados individualmente ficam ocultos**.
7. **Financial/Admin** marca PIX pago e NF enviada por colaborador.
8. Quando **todos** estão pagos → folha vira `paid`.

### 5.3 Regras importantes

| Situação | Comportamento |
|---|---|
| Folha `approved` ou `paid` | Valores **não recalculam** ao abrir detalhe |
| Folha `draft` ou `rejected` | Pode recalcular livremente |
| Bônus/desconto após aprovação | Só com permissão `payrolls.pay` e status `approved` |
| Marcar pago | Folha **não** pode estar `pending_approval` |
| Duplicar folha | Copia entradas, zera flags de aprovado/pago/NF |

### 5.4 Quem entra no financeiro

| Status da folha | Quem aparece na lista a pagar |
|---|---|
| `approved` ou `paid` | **Todos** os colaboradores |
| Outros status | Só os com `is_approved = true` |

---

## 6. Motor de cálculo (como a folha é calculada)

O `PayrollCalculator` (em `Core/Domain/PayrollCalculation/`) decide **qual fórmula aplicar** com base no **perfil explícito** do colaborador — não no nome do cargo (diferença em relação ao legado).

### 6.1 De onde vem o perfil

```
Collaborator.CalculationProfileOverride  (se preenchido)
         ↓ senão
CareerLevel.Profile
         ↓ senão
Department.CalculationType  →  mapeado para CalculationProfile
```

### 6.2 Ordem de decisão no cálculo

Para cada colaborador na folha, o motor segue esta sequência:

1. Sem setor → comissão 0
2. Mudança de cargo no mês → divide em períodos proporcionais
3. Perfil **Tráfego Pago** → comissão sobre investimento + CPA por casa
4. Perfil **Supervisor Comercial** → FTD, vendas, rev de analistas
5. Perfil **Analista Comercial** → FTD, CPA, vendas, mínimo garantido, plataforma
6. Perfil **Gerência** → % sobre faturamento líquido
7. Perfil **Líder de Projetos** → comissão com meta/super meta e teto 2,1%
8. Comissão pura / fixo+comissão / Affiliates
9. Demais (Tipster, rateado, Automação → Lima Karttos)

### 6.3 Perfis resumidos

| Perfil | Como calcula (simplificado) |
|---|---|
| **Analista Comercial** | FTD iGaming + Superbet + CPA + % vendas + rev; mínimo garantido vira complemento; Lastlink/Hubla abatem do PIX |
| **Supervisor** | Fixo + comissões por projeto (FTD, vendas, rev analistas); fixo não vai para projeto 3C Sports |
| **Tráfego Pago** | Fixo rateado + % sobre investimento + CPA por casa de apostas |
| **Gerência** | Fixo + % sobre faturamento líquido × fator (50 gerente / 100 diretora) |
| **Líder de Projetos** | Fixo proporcional + % sobre faturamento líquido (80% do bruto) por projeto; faixa `GoalTier` (sem/com/super meta); acréscimo +0,4 p.p. abaixo de R$ 200k por projeto; teto 2,1% |
| **Tipster** | Fixo + bônus por % de grupo + bônus VIP |
| **Rateado / Automação** | Fixo dividido entre projetos; Feira não entra na base de bônus; Automação/Contingência → 100% Lima Karttos |
| **Affiliates** | Fixo + bônus de meta; ou salário travado (`final_salary`) |

### 6.4 Proporcional de admissão/demissão

Se o colaborador entrou ou saiu no meio do mês:

```
fator = dias trabalhados no mês / dias do mês
salário proporcional = salário cheio × fator
```

A data de demissão vem **sempre do cadastro do colaborador**, nunca de snapshot antigo.

### 6.5 Rateio por projeto

Após calcular o total do colaborador, funções separadas distribuem o **custo por projeto** — útil para o financeiro e relatórios. Regras incluem:
- Comercial: comissão < R$ 100 vai para projeto pagador escolhido;
- Complemento pode ser rateado por % ou ir para Lima Karttos;
- Gerência usa breakdown manual por projeto.

---

## 7. Fluxo financeiro (PIX)

```
Folha aprovada
      │
      ▼
┌─────────────────────────────────────┐
│  Para cada colaborador:             │
│  • total_amount (bruto)             │
│  • platformTotal (Lastlink/Hubla)   │  ← só analista comercial
│  • a receber = total − plataforma   │
└─────────────────────────────────────┘
      │
      ▼
Financial marca is_paid + nf_sent
      │
      ▼
Todos pagos → folha status = paid
```

**Hubla:** percentual da plataforma limitado a **4%**, mesmo que o nível tenha taxa maior.  
**Lastlink:** usa o % base do nível (padrão 4%).

---

## 8. Fluxo de tráfego pago

Independente da folha, mas usa dados do setor Tráfego Pago na folha para comissões.

```
Projeto + mês/ano
      │
      ├── Meta mensal
      ├── Semana 1..4
      │     ├── Gastos por plataforma (Telegram, Instagram, Story, etc.)
      │     ├── Imposto = gasto × 12,15%
      │     └── Depósitos (pending → requested → deposited)
      └── Sugestão próximo depósito = max(0, meta/4 − saldo disponível)
```

Na **folha**, o colaborador de tráfego informa valor investido + CPAs por casa → motor calcula comissão.

---

## 9. Fluxo de caixa

```
Entradas                          Saídas
────────                          ──────
Plataforma                        Folha de pagamento
iGaming                           Tráfego
Devoluções                        Ações, Viagens, Imposto...
                                  (+ 12 categorias fixas)

Saldo do período = entradas − saídas
```

**Webhook Facilities** (`POST /api/v1/webhooks/facilities/cashflow`):
- Autenticação por HMAC-SHA256 (sem JWT);
- Idempotência por `lancamento_id` (replay não duplica);
- Janela de timestamp ± 300 segundos.

---

## 10. Notificações

| Evento | Quem recebe |
|---|---|
| Folha submetida | Director / Admin |
| Folha aprovada | Manager que submeteu |
| Folha reprovada | Manager que submeteu |

Exibidas no sino da topbar com badge vermelho se houver não lidas.

---

## 11. Estrutura de pastas do repositório

```
corepay/
├── agents.md                 # Instruções de arquitetura e invariantes
├── docker-compose.yml        # SQL Server local
├── docs/
│   ├── VISAO_GERAL.md        # ← este arquivo
│   ├── roadmap.md            # Ordem de implementação (fases 0–15)
│   ├── REGRAS_DE_NEGOCIO.md  # Fórmulas e workflows (fonte da verdade)
│   ├── IDENTIDADE_VISUAL.md  # Tema, tokens, componentes UI
│   └── design.md             # Contratos de API e rotas Blazor
├── backend/
│   ├── src/
│   │   ├── BuildingBlocks/   # Result<T>, BahiaTimeZone, Money
│   │   ├── Core/             # Domínio, CQRS, PayrollCalculator
│   │   ├── Infrastructure/   # EF Core, Identity, stores
│   │   └── WebAPI/           # Minimal APIs, JWT, endpoints
│   └── tests/                # Core.Tests, WebAPI.Tests
└── frontend/
    └── src/WebApp.Blazor/    # Blazor WASM, componentes, páginas
```

---

## 12. Estado atual da implementação

Com base no `roadmap.md` (atualizado em set/2026):

| Fase | Descrição | Status |
|---|---|---|
| **0** | Fundação (.NET, SQL, Identity, JWT) | ✅ Concluída |
| **1** | Identidade visual + componentes UI | ✅ Concluída |
| **2** | RBAC, shell, menu por permissão | ✅ Concluída |
| **3** | Cadastros mestres (setores, níveis, projetos, usuários) | ✅ Concluída |
| **4** | Colaboradores (lista + CRUD) | ✅ Concluída |
| **5** | Motor de cálculo (núcleo) | ✅ Concluída |
| **6** | Rateio por projeto | ✅ Concluída |
| **7** | Folha — CRUD, formulário e detalhe | ✅ Concluída |
| **8** | Folha — workflow, snapshot, pagamento | ✅ Concluída |
| **9** | Financeiro (9.1–9.4) | ✅ Concluída |
| **10** | Faturamento (10.1 CRUD) | ✅ Concluída |
| **11** | Investimento de tráfego | ✅ Concluída |
| **12** | Fluxo de caixa + webhook Facilities | ✅ Concluída |
| **13** | Dashboard e relatórios | ✅ Concluída |
| **14** | Notificações | ✅ Concluída |
| **15** | Paridade, extras e endurecimento | ✅ Concluída |

**O que já funciona hoje:** login, shell autenticado, cadastros mestres, colaboradores, motor de cálculo completo, custo por projeto, folhas (CRUD + workflow + snapshot), financeiro, faturamento, investimento de tráfego, fluxo de caixa, dashboard, relatórios, notificações, métricas de analista, paridade visual/negócio e checklist de segurança.
**Entrega:** README reproduzível + CI do motor (`PayrollCalculator`) + documentação viva alinhada (Fase 15.6).

---

## 13. Diagrama resumido — jornada mensal típica

```
 INÍCIO DO MÊS
      │
      ▼
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  Cadastros  │────►│   Colabora-  │────►│   Criar     │
│  (setores,  │     │   dores      │     │   folha     │
│   níveis,   │     │   ativos     │     │   (draft)   │
│   projetos) │     └──────────────┘     └──────┬──────┘
└─────────────┘                                 │
                                                ▼
                                         ┌─────────────┐
                                         │  Preencher  │
                                         │  metas, FTD,│
                                         │  vendas...  │
                                         └──────┬──────┘
                                                │ motor calcula
                                                ▼
                                         ┌─────────────┐
                                         │  Submeter   │
                                         │  (pending)  │
                                         └──────┬──────┘
                                                │
                           ┌────────────────────┼────────────────────┐
                           ▼                                         ▼
                    ┌─────────────┐                           ┌─────────────┐
                    │  Aprovar    │                           │  Reprovar   │
                    │  (snapshot) │                           │  (coment.)  │
                    └──────┬──────┘                           └──────┬──────┘
                           │                                         │
                           ▼                                         └──► reeditar
                    ┌─────────────┐
                    │ Financeiro  │
                    │ marca PIX   │
                    │ + NF        │
                    └──────┬──────┘
                           │
                           ▼
                    ┌─────────────┐
                    │ Relatórios  │
                    │ + Caixa     │
                    └─────────────┘
```

---

## 14. O que o CorePay **não** faz (anti-escopo)

- Pagamento automático (Stripe, Open Finance, PIX cobrado).
- Self-signup público ou login com Google.
- Recalcular folha aprovada/paga ao consultar.
- Autorização por e-mail especial.
- IDs sequenciais numéricos.
- Copiar código JS/Tailwind do legado.

---

## 15. Documentos para aprofundar

| Arquivo | Conteúdo |
|---|---|
| [`agents.md`](../agents.md) | Stack, RBAC, invariantes, endpoints |
| [`REGRAS_DE_NEGOCIO.md`](./REGRAS_DE_NEGOCIO.md) | Fórmulas completas, tabelas de taxas, edge cases |
| [`roadmap.md`](./roadmap.md) | Ordem de implementação fase a fase |
| [`IDENTIDADE_VISUAL.md`](./IDENTIDADE_VISUAL.md) | Tema dark-first, magenta, componentes |
| [`design.md`](./design.md) | Contratos de API e rotas do frontend |

---

*Última atualização: setembro/2026 — alinhado ao estado do `roadmap.md`.*
