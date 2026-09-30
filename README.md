# CorePay

Sistema interno de folha de pagamento, financeiro, investimento de tráfego e fluxo de caixa. Arquitetura desacoplada: **API REST (.NET 10)** + **SPA Blazor WebAssembly (.NET 10)**, comunicando-se via HTTP versionado (`/api/v1/...`).

## Stack

| Camada | Tecnologia |
|--------|------------|
| Backend | .NET 10, Minimal APIs, CQRS (MediatR), Result Pattern |
| Frontend | Blazor WebAssembly (.NET 10) |
| Banco | SQL Server 2022 (Docker) |
| Auth | JWT Bearer + ASP.NET Core Identity (login Fase 0.3) |
| ORM | Entity Framework Core 10 (migrations no startup) |
| Testes | xUnit, FluentAssertions, Moq, bUnit |

## Estrutura do repositório

```text
corepay/
├── backend/
│   ├── CorePay.slnx
│   ├── src/
│   │   ├── BuildingBlocks/
│   │   ├── Core/
│   │   ├── Infrastructure/
│   │   └── WebAPI/
│   └── tests/
├── frontend/
│   ├── CorePay.Frontend.slnx
│   ├── src/WebApp.Blazor/
│   └── tests/
├── docs/
├── agents.md
└── docker-compose.yml
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (SQL Server local)
- Navegador moderno (Chrome, Edge ou Firefox)

## Inicialização

### 1. Variáveis de ambiente e config local

Em **Development**, a API carrega automaticamente `.env` (se existir) ou, na falta dele, `.env.example` na raiz do repositório. Após clonar, já é possível subir a API **sem** `source .env` manual.

Para personalizar senhas/e-mails locais (recomendado antes de produção):

```bash
cp .env.example .env
# edite .env conforme necessário
```

Cada app usa **dois arquivos**:

| Arquivo | Git | Uso |
|---------|-----|-----|
| `appsettings.json` | Versionado | Defaults base (placeholders seguros) |
| `appsettings.Development.json` | Local (gitignored) | Config para rodar o projeto na máquina |

Crie os arquivos Development (se ainda não existirem):

**Backend** — `backend/src/WebAPI/appsettings.Development.json`:

```json
{
  "Seed": {
    "SuperAdmin": {
      "Email": "admin@example.local"
    },
    "LoadFixtures": true,
    "LoadDemoData": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Frontend** — `frontend/src/WebApp.Blazor/wwwroot/appsettings.Development.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5000"
}
```

> A senha do SuperAdmin **não** fica em `appsettings*.json` — use `Seed__SuperAdmin__Password` no `.env` ou user-secrets.

> **API:** com `ASPNETCORE_ENVIRONMENT=Development`, o ASP.NET Core mescla `appsettings.Development.json` automaticamente. **Frontend WASM:** em Debug, o `Program.cs` mescla `appsettings.Development.json` sobre `appsettings.json`. No frontend Blazor, `wwwroot` é público: nunca coloque segredos ali.

Valores padrão de desenvolvimento (em `.env.example`, alinhados ao `docker-compose.yml`):

```env
MSSQL_SA_PASSWORD=Your_strong_Password123!
ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=CorePay;User Id=sa;Password=Your_strong_Password123!;TrustServerCertificate=True;MultipleActiveResultSets=true
Jwt__Key=dev-only-signing-key-at-least-32-chars
Facilities__WebhookSecret=dev-only-facilities-webhook-secret-at-least-32-chars
Seed__SuperAdmin__Email=admin@example.local
Seed__SuperAdmin__Password=ChangeMe-SuperAdmin-Password-123!
Seed__LoadFixtures=true
Seed__LoadDemoData=true
DEV_ROLE_USERS_PASSWORD=ChangeMe-Role-Users-Password-123!
```

> A connection string **não** fica em `appsettings*.json` versionados — use `ConnectionStrings__DefaultConnection` com a **mesma senha** de `MSSQL_SA_PASSWORD`.

Mapeamento de configuração → variável de ambiente (ASP.NET Core usa `__` como separador de seção):

| Configuração | Variável de ambiente |
|--------------|----------------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Jwt:Key` | `Jwt__Key` |
| `Facilities:WebhookSecret` | `Facilities__WebhookSecret` |
| `Seed:SuperAdmin:Email` | `Seed__SuperAdmin__Email` |
| `Seed:SuperAdmin:Password` | `Seed__SuperAdmin__Password` |
| `Seed:LoadFixtures` | `Seed__LoadFixtures` |
| `Seed:LoadDemoData` | `Seed__LoadDemoData` |
| `Seed:RoleUsersPassword` | `DEV_ROLE_USERS_PASSWORD` (ou `Seed__RoleUsersPassword`) |

> Em produção, defina variáveis no ambiente/orquestrador — **não** dependa de `.env.example`.

### 2. Subir o SQL Server

```bash
docker compose up -d
docker compose ps
```

Aguarde o container ficar **healthy** antes de subir a API (evita falha de migration na primeira execução):

```bash
docker compose ps   # coluna STATUS deve mostrar (healthy)
# ou
docker compose logs -f sqlserver
```

### 3. Subir a API

```bash
cd backend
dotnet watch run --project src/WebAPI/WebAPI.csproj
```

Na primeira execução, a API aplica as migrations (`MigrateAsync`) e semeia RBAC + usuário SuperAdmin. Em `Development`, `Seed:LoadFixtures=true` adiciona dados mestres e `Seed:LoadDemoData=true` adiciona o cenário E2E completo. As duas seeds são opt-in, idempotentes e nunca executam em Production.

#### Usuários por papel (teste local de UI)

Com `Seed:LoadDemoData=true`, a API cria automaticamente:

| Papel | E-mail |
|-------|--------|
| Admin | `admin@corepay.local` |
| Director | `director@corepay.local` |
| Financial | `financial@corepay.local` |
| Manager | `manager@corepay.local` (escopo: *Analistas Comerciais*) |
| User | `user@corepay.local` |

Senha de todos: `DEV_ROLE_USERS_PASSWORD`. A seed também cria dados mestres, forma de pagamento, folhas `draft`/`pendingApproval`/`approved`/`paid`/`rejected`, notificações, faturamento, métricas, tráfego/aportes e caixa.

Como alternativa sem seed no startup, o script legado continua disponível (domínio `.test`, idempotente):

```bash
# No .env: DEV_ROLE_USERS_PASSWORD=CHANGE_ME_WITH_A_DEVELOPMENT_ONLY_PASSWORD
./scripts/seed-local-role-users.sh
```

| Papel | E-mail |
|-------|--------|
| Admin | `admin@corepay.test` |
| Director | `director@corepay.test` |
| Financial | `financial@corepay.test` |
| Manager | `manager@corepay.test` (escopo: setor *Analistas Comerciais Demo*) |
| User | `user@corepay.test` |

Senha de todos: valor de `DEV_ROLE_USERS_PASSWORD` no `.env`. O script também cria o setor demo do Manager, se necessário.

**Login SuperAdmin (dev):**

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.local","password":"CHANGE_ME_WITH_A_STRONG_ADMIN_PASSWORD"}'
```

| Serviço | URL |
|---------|-----|
| API HTTP | http://localhost:5000 |
| Swagger (Development) | http://localhost:5000/swagger |

**Health check:**

```bash
curl http://localhost:5000/api/v1/health
```

Resposta esperada:

```json
{
  "Status": "healthy",
  "Version": "v1",
  "Timestamp": "2026-09-09T18:00:00Z"
}
```

### 4. Subir o frontend (Blazor WASM)

Em outro terminal:

```bash
cd frontend
dotnet restore
dotnet run --project src/WebApp.Blazor/WebApp.Blazor.csproj
```

| Serviço | URL |
|---------|-----|
| Frontend | http://localhost:5173 |

Para dev local, use `appsettings.Development.json` (`ApiBaseUrl` → `http://localhost:5000`). O `appsettings.json` versionado mantém placeholder de produção (`https://api.example.local`).

### 5. Testes

#### Motor de cálculo (`PayrollCalculator`)

Testes unitários puros em `backend/tests/Core.Tests/PayrollCalculation/` — sem SQL Server, HTTP ou Blazor:

```bash
cd backend
dotnet test tests/Core.Tests/Core.Tests.csproj --filter "FullyQualifiedName~PayrollCalculation"
```

Entry point do motor: `backend/src/Core/Domain/PayrollCalculation/PayrollCalculator.cs`.

#### Suítes completas

```bash
cd backend
dotnet test CorePay.slnx
```

```bash
cd frontend
dotnet test CorePay.Frontend.slnx
```

Para executar os testes do motor localmente, use o filtro `PayrollCalculation` indicado acima.

## Documentação

| Arquivo | Uso |
|---------|-----|
| [agents.md](agents.md) | Stack, RBAC, invariantes, endpoints |
| [docs/VISAO_GERAL.md](docs/VISAO_GERAL.md) | Visão operacional e jornadas do sistema |
| [docs/REGRAS_DE_NEGOCIO.md](docs/REGRAS_DE_NEGOCIO.md) | Fórmulas e workflow da folha |
| [docs/IDENTIDADE_VISUAL.md](docs/IDENTIDADE_VISUAL.md) | Tokens e componentes UI |
| [docs/design.md](docs/design.md) | Contratos de API e rotas Blazor |
| [docs/roadmap.md](docs/roadmap.md) | Ordem de implementação |
| [RELATORIO_TECNICO.md](RELATORIO_TECNICO.md) | Tecnologias, arquitetura, banco, frontend e segurança |
