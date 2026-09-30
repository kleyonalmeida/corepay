# Relatório técnico do CorePay

Levantamento do código do repositório em 30/09/2026. O relatório descreve a implementação presente, sem pressupor que uma instância esteja em execução. As configurações de produção devem ser conferidas no ambiente de implantação.

## Visão geral e arquitetura

O CorePay reúne folha de pagamento, financeiro, fluxo de caixa, faturamento de projetos, métricas de analistas, investimento de tráfego, notificações, relatórios e administração de usuários. Há duas aplicações independentes: uma API HTTP em `backend/` e uma SPA em `frontend/`. O navegador consome endpoints REST versionados em `/api/v1/...`; ele não acessa o banco diretamente.

| Camada | Implementação | Local principal |
| --- | --- | --- |
| API | C# / .NET 10, ASP.NET Core Minimal APIs, versionamento de rota, Swagger em desenvolvimento | `backend/src/WebAPI/` |
| Regras de negócio | Projeto `Core`, domínios e casos de uso, MediatR em fluxos CQRS, padrão `Result` | `backend/src/Core/`, `backend/src/BuildingBlocks/` |
| Persistência | EF Core 10, SQL Server, ASP.NET Core Identity, stores por módulo | `backend/src/Infrastructure/` |
| Interface | Blazor WebAssembly .NET 10, componentes Razor, CSS próprio, ícones Lucide | `frontend/src/WebApp.Blazor/` |
| Infraestrutura local | Docker Compose com SQL Server 2022 | `docker-compose.yml` |
| Implantação por contêiner | SQL Server, API ASP.NET e frontend servido por Nginx | `docker-compose.prod.yml`, `backend/Dockerfile`, `frontend/Dockerfile` |

O backend separa contratos e regras de domínio da infraestrutura. Os endpoints chamam serviços e casos de uso; a infraestrutura implementa acesso aos dados. O frontend organiza páginas em `Pages/`, componentes por área em `Components/`, serviços HTTP em `Services/` e políticas de acesso em `Auth/`. A separação permite compilar e publicar API e interface de forma independente, desde que a URL pública da API e as origens CORS estejam configuradas.

## Banco de dados e dados

O banco operacional é Microsoft SQL Server 2022. `AppDbContext` herda de `IdentityDbContext` e mapeia usuários, papéis e permissões junto com setores, níveis de carreira, projetos, colaboradores, folhas e lançamentos, custos, faturamento, métricas, investimento de tráfego e notificações. O EF Core define relacionamentos, precisão decimal para valores monetários, índices de busca e unicidade para combinações como folha por setor e competência. As alterações de esquema estão em `backend/src/Infrastructure/Migrations/`; a API executa `MigrateAsync` na inicialização fora do ambiente de testes.

Em `Testing`, o backend usa o provedor EF Core InMemory. Há seeds para permissões e SuperAdmin; fixtures e dados de demonstração são opcionais e executados somente em `Development`. As seeds de demonstração usam registros artificiais. A conexão SQL é obtida de `ConnectionStrings:DefaultConnection`, normalmente via variável de ambiente. No Compose de produção, o SQL Server usa volume persistente e uma pasta de backup montada; a configuração do repositório não representa, por si, uma política de backup ou restauração.

## Frontend

A interface é uma SPA Blazor WebAssembly. `Program.cs` registra serviços HTTP por módulo e um `HttpClient` com `AuthorizationMessageHandler` para enviar o token Bearer. As rotas usam autenticação e políticas de permissão. O estado de sessão fica em `localStorage` e existe encerramento por inatividade configurado no cliente. Há layout responsivo, tema claro/escuro, componentes reutilizáveis, CSS com tokens visuais e fontes externas Inter/Montserrat. O build de produção publica arquivos estáticos, servidos pelo Nginx; `API_BASE_URL` é incorporada ao build.

Áreas expostas pela interface incluem dashboard, folhas, financeiro, colaboradores, projetos e faturamento, tráfego e aportes, caixa, relatórios, notificações, configurações, usuários e papéis. Os relatórios de folha podem gerar XLSX com ClosedXML no backend.

## API, integrações e operação

Os endpoints são agrupados por recurso sob `/api/v{version:apiVersion}`. A API usa `Asp.Versioning.Http` e `Asp.Versioning.Mvc.ApiExplorer`; o Swagger fica habilitado em desenvolvimento. A integração de caixa com Facilities recebe webhook assinado. A API expõe endpoint de saúde usado pelo Compose para ordenar a subida dos serviços. Há tratamento global de exceções, respostas de problema e métricas de requisição/consulta no código.

O Compose local sobe apenas o banco; API e frontend podem ser executados pelo SDK .NET conforme `README.md`. O Compose de produção constrói três contêineres. A API depende da saúde do SQL Server e o frontend depende da saúde da API. O arquivo de produção publica a API na porta definida por `BACKEND_PORT` e o frontend em `FRONTEND_PORT`; TLS e proxy reverso precisam ser configurados no ambiente de implantação. Não há pipeline de CI versionado identificado neste repositório.

## Segurança implementada

- Autenticação com ASP.NET Core Identity e JWT Bearer. Há validação de emissor, audiência, assinatura e validade do token; a chave é validada na inicialização. Senhas são geridas pelo Identity, com requisitos mínimos e bloqueio após tentativas falhas.
- Autorização por papéis e permissões na API, com escopo de setor para operações de gerente. O frontend também usa políticas para menu e rotas, mas a decisão efetiva deve permanecer na API.
- Rate limit global e políticas específicas para login e webhook. CORS de produção exige origens explícitas, sem curinga ou `localhost`.
- Webhook Facilities com assinatura HMAC, janela de tempo, limite de corpo e chave de idempotência. A API limita o tamanho geral das requisições.
- Cabeçalhos `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy` e `X-Frame-Options`; em produção, a API envia HSTS. Segredos de JWT, webhook, banco e SuperAdmin são esperados via ambiente ou user secrets, não nos `appsettings.json` versionados.

### Pontos de atenção

1. O JWT da SPA é mantido em `localStorage`. Uma falha de XSS na origem poderia expô-lo. A CSP de `wwwroot/index.html` ainda permite `unsafe-inline` e `unsafe-eval` para scripts/estilos; convém reduzi-los quando a compatibilidade com Blazor e os scripts existentes for verificada.
2. `docker-compose.yml` e `.env.example` trazem credenciais demonstrativas para desenvolvimento. Elas não devem ser usadas em produção. O Compose de produção exige variáveis, mas o gerenciamento, rotação e armazenamento dos segredos dependem da implantação.
3. O contêiner do frontend serve HTTP interno; a segurança de transporte externa depende de TLS no proxy. Confirmar também a configuração de redirecionamento HTTPS e cabeçalhos encaminhados pela API atrás desse proxy.
4. Migrations automáticas na inicialização simplificam o deploy, mas exigem controle de permissões do usuário SQL, ordem de implantação e plano de rollback. Não há evidência de teste de restauração de backup neste levantamento.

Esses itens são observações de código e configuração, não resultado de teste de invasão ou auditoria de infraestrutura.

## Testes e manutenção

O backend tem projetos xUnit para blocos básicos, regras de negócio e testes HTTP da API, com FluentAssertions e integrações de teste em memória. O frontend usa xUnit, bUnit e MockHttp. Os comandos documentados são `dotnet test backend/CorePay.slnx` e `dotnet test frontend/CorePay.Frontend.slnx` a partir da raiz. Documentos de apoio ficam em `docs/`, especialmente `VISAO_GERAL.md`, `REGRAS_DE_NEGOCIO.md`, `design.md` e `IDENTIDADE_VISUAL.md`.

## Base da análise

Arquivos examinados: projetos `.csproj`, `Program.cs` de ambas as aplicações, `AppDbContext`, migrations, autenticação, middleware, endpoints, serviços do frontend, `Dockerfile`, arquivos Compose, `.env.example`, `README.md` e estrutura dos testes. Versões citadas refletem os manifestos do repositório na data do levantamento; não foi feita checagem de atualizações externas de pacotes.
