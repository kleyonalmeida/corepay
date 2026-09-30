# Plano de correções de segurança

## 1. Objetivo e regras invariantes

Este plano cobre autorização baseada no estado atual do banco, escalonamento de privilégios, IDOR, XSS, uploads/imagens, headers, webhook, autenticação e hardening HTTP.

Regras que devem permanecer verdadeiras depois da implementação:

- O frontend nunca decide se uma operação é permitida. Ele apenas reflete permissões e ações calculadas pela API.
- O JWT identifica a sessão, mas não é a fonte definitiva de roles, permissões, departamentos ou estado do usuário.
- Toda requisição protegida confirma no banco que o usuário existe e pode acessar a operação naquele momento.
- Rotas administrativas sensíveis exigem role atual `Admin` ou `SuperAdmin` consultada no banco, além da permissão necessária.
- Toda consulta/mutação por ID valida autorização sobre o recurso específico; conhecer um GUID não concede acesso.
- Arquivos são validados no backend pelo conteúdo real, nunca somente por extensão ou `Content-Type` enviado pelo cliente.
- Headers externos são tratados como entrada não confiável, com formato, cardinalidade e tamanho limitados.
- Nenhum segredo, senha, token ou dado sensível aparece em logs ou respostas de erro.

## 2. Prioridades

| Prioridade | Correção | Risco tratado |
|---|---|---|
| P0 | Autorização consultando banco em cada requisição | Token continuar válido após revogação de role, permissão ou usuário |
| P0 | Policy Admin/SuperAdmin para administração | Escalonamento de privilégios |
| P0 | Testes sistemáticos de rotas e IDOR | Acesso horizontal ou vertical indevido |
| P1 | Lockout e rate limiting | Força bruta e abuso de endpoints |
| P1 | Limites e validação do webhook/headers | DoS, ambiguidades de headers e respostas 500 |
| P1 | CSP e headers defensivos | XSS, framing e exposição desnecessária |
| P1 | Validação de URLs de imagem/anexo | Esquemas perigosos e conteúdo remoto não confiável |
| P2 | Pipeline de upload seguro, quando o upload existir | Arquivos maliciosos e conteúdo disfarçado |
| P2 | Revogação de sessão e redução de exposição do token | Roubo/reuso de JWT |

## 3. Fase P0 — banco como fonte de autorização

### 3.1 Criar um resolvedor da identidade atual

Criar:

- `backend/src/WebAPI/Auth/CurrentUserAuthorizationState.cs`
- `backend/src/WebAPI/Auth/ICurrentUserAuthorizationStateProvider.cs`
- `backend/src/WebAPI/Auth/CurrentUserAuthorizationStateProvider.cs`

O provider deve receber `AppDbContext` e/ou `UserManager<AppUser>` e, usando exclusivamente o `NameIdentifier/sub` do token:

1. validar que o identificador é um GUID/string de usuário válido;
2. consultar o usuário no banco;
3. rejeitar usuário inexistente, bloqueado ou não autorizado a entrar;
4. carregar roles atuais pelas tabelas do Identity;
5. carregar permissões atuais por `RolePermissions`;
6. carregar `UserDepartments` quando necessário;
7. devolver um objeto imutável com `UserId`, `DisplayName`, `Roles`, `Permissions` e `DepartmentIds`.

Não aceitar roles, permissions, display name ou departamentos enviados em body, query string ou headers.

Motivo: atualmente [`LoginService.cs`](../backend/src/WebAPI/Auth/LoginService.cs) coloca roles e permissões no JWT e os handlers confiam nessas claims até o token expirar.

### 3.2 Alterar os handlers de autorização

Modificar:

- `backend/src/WebAPI/Auth/PermissionAuthorizationHandler.cs`
- `backend/src/WebAPI/Auth/PermissionAnyAuthorizationHandler.cs`
- `backend/src/WebAPI/Auth/AuthServiceCollectionExtensions.cs`

Alterações:

- Injetar o provider do item 3.1 nos handlers.
- Consultar permissões atuais do banco em `HandleRequirementAsync`.
- Não autorizar `SuperAdmin` apenas porque existe uma claim `Role=SuperAdmin`; confirmar a role no banco.
- Falhar fechado quando o usuário, role ou permissão não puder ser carregado.
- Respeitar `CancellationToken` quando disponível.
- Registrar somente evento, user ID e policy; nunca o token.

O frontend pode continuar usando as claims para apresentação imediata, mas uma negativa da API sempre prevalece.

### 3.3 Reduzir claims de autorização no JWT

Modificar:

- `backend/src/WebAPI/Auth/JwtTokenService.cs`
- `backend/src/WebAPI/Auth/LoginService.cs`
- `backend/src/WebAPI/Auth/LoginContracts.cs`, se o contrato precisar ser separado entre UI e token.

Decisão recomendada:

- JWT: manter apenas identidade e metadados mínimos (`sub`, identificador, `jti`, `iat`, `exp`).
- Resposta de login: pode devolver roles/permissões atuais para montar a UI.
- Backend: nunca usar essas informações da resposta ou do token como autoridade.

Adicionar `jti` aleatório em cada token. Não incluir dados financeiros, PIX, departamentos ou informações pessoais desnecessárias.

### 3.4 Construir contextos de acesso pelo banco

Modificar os endpoints que hoje constroem contextos a partir de `ClaimsPrincipal`:

- `backend/src/WebAPI/Endpoints/PayrollsEndpoints.cs`
- `backend/src/WebAPI/Endpoints/CollaboratorsEndpoints.cs`
- `backend/src/WebAPI/Endpoints/AnalystMetricsEndpoints.cs`
- `backend/src/WebAPI/Endpoints/ReportsEndpoints.cs`
- `backend/src/WebAPI/Endpoints/DashboardEndpoints.cs`
- `backend/src/WebAPI/Endpoints/FinanceEndpoints.cs`
- `backend/src/WebAPI/Endpoints/NotificationsEndpoints.cs`

Substituir leitura de roles/permissões das claims pela injeção do provider do item 3.1. O ID do token serve apenas para localizar a identidade; roles, permissões e departamentos vêm do banco.

Motivo: hoje parte do escopo departamental já vem do banco, mas a decisão de tratar alguém como Manager/Admin ainda pode depender de uma claim antiga.

### 3.5 Cache opcional

Se a consulta por requisição causar custo relevante, adicionar cache com duração máxima de 30–60 segundos e invalidação obrigatória quando:

- usuário for atualizado/excluído/bloqueado;
- roles do usuário forem alteradas;
- permissões de uma role forem alteradas;
- departamentos do usuário forem alterados.

Não usar cache longo. Segurança tem precedência sobre redução de consultas.

## 4. Fase P0 — impedir escalonamento de privilégios

### 4.1 Criar policy de role administrativa atual

Criar:

- `backend/src/WebAPI/Auth/AdminRoleRequirement.cs`
- `backend/src/WebAPI/Auth/AdminRoleAuthorizationHandler.cs`

A policy deve consultar o banco e aceitar apenas role atual `Admin` ou `SuperAdmin`. Criar também uma policy exclusiva para `SuperAdmin` quando a operação puder conceder/remover poderes máximos.

### 4.2 Aplicar a policy às rotas administrativas

Modificar:

- `backend/src/WebAPI/Endpoints/UsersEndpoints.cs`
- `backend/src/WebAPI/Endpoints/RolesEndpoints.cs`
- `backend/src/WebAPI/Endpoints/PermissionsEndpoints.cs`

Regras:

- Leitura administrativa: `Admin/SuperAdmin` atual + permissão de leitura correspondente.
- Criar, editar e excluir usuários: `Admin/SuperAdmin` atual + `users.write`.
- Criar/editar roles e permissions: preferencialmente somente `SuperAdmin`; se Admin puder fazer isso por regra de negócio, documentar explicitamente e impedir que conceda algo acima do próprio conjunto de permissões.
- Somente `SuperAdmin` atual pode atribuir a role `SuperAdmin`.
- Admin não pode promover a si próprio, editar um SuperAdmin, remover o último SuperAdmin ou conceder permissões que não possui.
- Impedir autoexclusão e invalidar imediatamente sessões após mudança de role.

### 4.3 Remover confiança nas roles recebidas pelo command

Modificar:

- `backend/src/Core/Application/Admin/AdminCommands.cs`
- `backend/src/Core/Application/Admin/AdminHandlers.cs`
- `backend/src/Core/Application/Admin/IAdminIdentityStore.cs`
- `backend/src/Infrastructure/Admin/AdminIdentityStore.cs`

Não passar `actingUserRoles` originadas de `ClaimsPrincipal`. Passar apenas `actingUserId`; dentro da camada de infraestrutura, recarregar o operador e suas roles atuais antes de validar operações sensíveis.

Motivo: `UsersEndpoints` atualmente usa `GetUserRoles(user)`, que lê claims do JWT.

## 5. Fase P0 — matriz de rotas e IDOR

### 5.1 Criar teste agregador de rotas

Criar:

- `backend/tests/WebAPI.Tests/Security/ProtectedRoutesAuthorizationTests.cs`

Manter uma tabela explícita de cada endpoint com:

- método e rota;
- policy esperada;
- roles permitidas;
- roles proibidas;
- necessidade de escopo departamental;
- comportamento anônimo esperado (`401`);
- comportamento autenticado sem permissão (`403`).

As únicas rotas anônimas esperadas são login, health e webhook assinado. Qualquer nova rota sem policy deve fazer o teste falhar.

### 5.2 Testar IDOR em cada recurso por GUID

Ampliar:

- `backend/tests/WebAPI.Tests/Collaborators/CollaboratorsEndpointTests.cs`
- `backend/tests/WebAPI.Tests/Payroll/PayrollsEndpointTests.cs`
- `backend/tests/WebAPI.Tests/Reports/PayrollReportEndpointTests.cs`
- `backend/tests/WebAPI.Tests/AnalystMetrics/AnalystMetricsEndpointTests.cs`
- `backend/tests/WebAPI.Tests/Notifications/NotificationsEndpointTests.cs`
- `backend/tests/WebAPI.Tests/Cashflow/CashflowEndpointTests.cs`
- testes de revenues, traffic, users e master data correspondentes.

Para cada GET/PUT/POST/DELETE que recebe ID:

1. criar recurso permitido para usuário A;
2. criar recurso fora do escopo para usuário B;
3. chamar a rota com o ID válido do recurso proibido;
4. esperar `403` ou `404`, conforme a política de não revelar existência;
5. confirmar no banco que nenhuma alteração ocorreu.

Testar também IDs filhos trocados, por exemplo `payrollId` permitido com `entryId` pertencente a outra folha.

### 5.3 Testar revogação em tempo real

Criar:

- `backend/tests/WebAPI.Tests/Security/LiveAuthorizationStateTests.cs`

Cenários obrigatórios:

- emitir token, remover `Admin` no banco e confirmar `403` na requisição seguinte;
- emitir token, remover uma permission e confirmar `403`;
- emitir token, excluir/bloquear usuário e confirmar `401/403`;
- emitir token de Manager, remover departamento e confirmar bloqueio imediato;
- alterar apenas dados do frontend/localStorage e confirmar que a API continua negando acesso.

## 6. Fase P1 — login, sessão e força bruta

### 6.1 Lockout do Identity

Modificar:

- `backend/src/WebAPI/Auth/LoginService.cs`
- `backend/src/Infrastructure/DependencyInjection.cs`

Usar `lockoutOnFailure: true` e configurar, por exemplo:

- 5 falhas;
- bloqueio inicial de 15 minutos;
- `AllowedForNewUsers = true`.

Manter resposta genérica para email inexistente, senha incorreta ou conta bloqueada.

### 6.2 Rate limiting

Modificar:

- `backend/src/WebAPI/Program.cs`
- `backend/src/WebAPI/Endpoints/AuthEndpoints.cs`
- `backend/src/WebAPI/Endpoints/FacilitiesWebhookEndpoints.cs`

Adicionar `AddRateLimiter`/`UseRateLimiter` com policies separadas:

- login: limite por IP normalizado e, quando possível, email normalizado sem registrar o valor bruto;
- webhook: limite mais alto, mas finito;
- API autenticada: limite global defensivo.

Responder `429` com formato de erro estável. Se houver proxy reverso, configurar forwarded headers somente para proxies/redes conhecidos.

### 6.3 Sessões e JWT

Modificar:

- `backend/src/WebAPI/Auth/AuthServiceCollectionExtensions.cs`
- `backend/src/WebAPI/Auth/JwtOptions.cs`
- `frontend/src/WebApp.Blazor/Auth/JsAuthSessionStorage.cs`
- `frontend/src/WebApp.Blazor/wwwroot/js/auth.js`

Correções mínimas:

- reduzir a duração do access token conforme risco operacional;
- validar `jti`/versão de segurança quando for necessária revogação imediata;
- confirmar usuário atual no banco em toda requisição;
- limpar sessão local ao receber `401`;
- documentar que `localStorage` aumenta o impacto de XSS.

Migração futura recomendada: BFF ou cookie `HttpOnly`, `Secure` e `SameSite`, com proteção CSRF. Não trocar para cookie sem implementar antiforgery.

## 7. Fase P1 — webhook e headers não confiáveis

### 7.1 Limitar o corpo antes de alocar memória

Modificar:

- `backend/src/WebAPI/Endpoints/FacilitiesWebhookEndpoints.cs`
- `backend/src/WebAPI/Program.cs`, se o limite for global/Kestrel.

Definir tamanho máximo compatível com o contrato, por exemplo 256 KB ou 1 MB. Verificar `Content-Length` quando presente e também limitar a leitura real para impedir bypass por chunked transfer.

Não copiar corpo ilimitado para `MemoryStream`.

### 7.2 Validar headers do webhook

Modificar:

- `backend/src/WebAPI/Endpoints/FacilitiesWebhookEndpoints.cs`
- `backend/src/Infrastructure/Facilities/FacilitiesWebhookSignatureValidator.cs`

Para `X-Facilities-Timestamp`, `X-Facilities-Signature` e `X-Idempotency-Key`:

- exigir exatamente um valor;
- rejeitar header duplicado;
- impor limite de comprimento;
- validar formato antes de converter;
- capturar timestamp fora do intervalo suportado;
- exigir assinatura com comprimento exato de SHA-256;
- manter comparação com `CryptographicOperations.FixedTimeEquals`;
- não devolver assinatura esperada nem segredo na resposta.

### 7.3 Proxy e headers encaminhados

Modificar apenas se houver proxy reverso:

- `backend/src/WebAPI/Program.cs`
- configuração de deploy do proxy.

Usar `UseForwardedHeaders` somente com `KnownProxies`/`KnownNetworks`. Nunca confiar livremente em `X-Forwarded-For`, `X-Forwarded-Host` ou `X-Forwarded-Proto` enviados pela internet.

Não tomar decisão de autorização a partir de `Origin`, `Referer`, `User-Agent`, `X-Role`, `X-User` ou headers equivalentes.

## 8. Fase P1 — XSS e headers HTTP defensivos

### 8.1 Content Security Policy do frontend

Modificar:

- `frontend/src/WebApp.Blazor/wwwroot/index.html`
- configuração do servidor/CDN que publica o frontend.

Definir CSP compatível com Blazor WASM e testar em modo de relatório antes de bloquear. A policy final deve restringir pelo menos:

- `default-src 'self'`;
- `object-src 'none'`;
- `base-uri 'self'`;
- `frame-ancestors 'none'`;
- `connect-src` somente para a API conhecida;
- `img-src` somente origens aprovadas, `self` e formatos realmente necessários;
- `font-src` e `style-src` somente origens necessárias.

Evitar `'unsafe-eval'` e reduzir `'unsafe-inline'` tanto quanto o runtime permitir.

### 8.2 Headers defensivos

Configurar no host da API e no host do frontend:

- `Content-Security-Policy`;
- `X-Content-Type-Options: nosniff`;
- `Referrer-Policy: no-referrer` ou `strict-origin-when-cross-origin`;
- `Permissions-Policy` mínima;
- `Strict-Transport-Security` em produção;
- `frame-ancestors 'none'` pela CSP.

Modificar:

- `backend/src/WebAPI/Program.cs` para HSTS e headers da API;
- configuração do servidor/CDN para os arquivos estáticos do frontend.

Não depender apenas de `X-Frame-Options`; usar CSP `frame-ancestors`.

### 8.3 Evitar novos sinks XSS

Criar teste/checklist em:

- `backend/tests/WebAPI.Tests/Security/SecurityChecklistTests.cs`
- novo teste frontend `frontend/tests/WebApp.Blazor.Tests/Auth/XssSafetyTests.cs`, se aplicável.

Proibir sem revisão explícita:

- `MarkupString` com dados externos;
- `innerHTML`, `outerHTML`, `insertAdjacentHTML`;
- `eval` e `new Function`;
- URLs `javascript:` ou HTML retornado pela API.

Blazor deve continuar renderizando textos normalmente para preservar encoding automático.

## 9. Fase P1 — URLs de foto e anexos

### 9.1 Criar validador central de URL externa

Criar, preferencialmente em Core ou Infrastructure:

- `backend/src/Core/Security/SafeExternalUrlValidator.cs`
- `backend/tests/Core.Tests/Security/SafeExternalUrlValidatorTests.cs`

Regras:

- aceitar apenas URI absoluta `https`;
- rejeitar `javascript:`, `data:`, `file:`, `ftp:` e esquemas desconhecidos;
- rejeitar usuário/senha embutidos na URL;
- permitir somente hosts configurados, se possível;
- normalizar antes de persistir;
- impor tamanho máximo;
- não buscar a URL no backend, evitando SSRF.

### 9.2 Aplicar em foto e anexo

Modificar:

- `backend/src/Infrastructure/Collaborators/CollaboratorStore.cs`
- `backend/src/Infrastructure/Cashflow/CashflowStore.cs`
- contratos correspondentes em `backend/src/Core/Application/Collaborators/` e `Cashflow/`, se necessário;
- `frontend/src/WebApp.Blazor/Components/Collaborators/CollaboratorFormDialog.razor`;
- `frontend/src/WebApp.Blazor/Components/Cashflow/CashflowEntryFormDialog.razor`;
- `frontend/src/WebApp.Blazor/Components/Ui/Avatar.razor`.

O frontend pode antecipar a validação para UX, mas o backend deve repetir toda validação. Em erro de carregamento, `Avatar` deve ocultar a imagem e mostrar iniciais.

## 10. Fase P2 — upload seguro de imagens

Hoje não existe endpoint de upload: há somente `PhotoUrl` e `AttachmentUrl`. Não criar upload implicitamente durante as fases anteriores.

Quando o produto exigir upload, criar componentes isolados, por exemplo:

- `backend/src/WebAPI/Endpoints/ImageUploadsEndpoints.cs`
- `backend/src/Core/Application/Uploads/`
- `backend/src/Infrastructure/Uploads/ImageUploadService.cs`
- `backend/tests/WebAPI.Tests/Uploads/ImageUploadsEndpointTests.cs`
- `frontend/src/WebApp.Blazor/Services/ImageUploadApiService.cs`
- componente frontend com `InputFile`.

Validações obrigatórias no backend:

- aceitar somente `.jpg`, `.jpeg`, `.png` e, após decisão explícita, `.webp`;
- não aceitar SVG, GIF, HTML ou arquivos executáveis;
- limitar quantidade e tamanho antes de ler o conteúdo;
- validar MIME declarado apenas como sinal auxiliar;
- validar magic bytes;
- decodificar a imagem completamente com biblioteca segura;
- validar dimensões e quantidade de pixels contra decompression bomb;
- remover metadados EXIF quando não forem necessários;
- reencodar a imagem para formato conhecido;
- gerar nome aleatório, sem reutilizar `FileName` recebido;
- armazenar fora da árvore executável e sem permissão de execução;
- servir com MIME fixo, `nosniff` e `Content-Disposition` apropriado;
- proteger upload por policy e associar o arquivo ao usuário/recurso autorizado;
- excluir arquivos órfãos com rotina controlada.

Não confiar apenas em extensão, MIME ou assinatura isoladamente.

## 11. Outras correções básicas

### 11.1 CORS e hosts

Modificar:

- `backend/src/WebAPI/Program.cs`
- `backend/src/WebAPI/appsettings.json` e configuração externa de produção.

Regras:

- produção deve exigir lista explícita de origens HTTPS;
- falhar no startup se produção usar origem localhost ou wildcard;
- não habilitar credentials sem necessidade;
- configurar `AllowedHosts` de produção explicitamente, em vez de `*`.

### 11.2 Tratamento de exceções

Modificar:

- `backend/src/WebAPI/Program.cs`
- criar `backend/src/WebAPI/Middleware/` ou usar `IExceptionHandler`.

Retornar `ProblemDetails` genérico em produção, com correlation ID. Nunca devolver stack trace, SQL, connection string, token, headers ou payload integral.

### 11.3 Validação e limites

Revisar todos os DTOs em `backend/src/Core/Application/**/**Contracts.cs` e stores correspondentes:

- comprimento máximo antes de persistência;
- ranges numéricos;
- enum definido;
- datas plausíveis;
- listas com quantidade máxima;
- paginação com teto;
- normalização de strings;
- rejeição de JSON excessivamente profundo/grande.

Limite de coluna no EF não substitui validação de entrada e resposta HTTP controlada.

### 11.4 Dependências e segredos

Adicionar ao CI:

- `dotnet list package --vulnerable --include-transitive`;
- scanner de segredos, como Gitleaks;
- falha do pipeline ao detectar segredo novo;
- atualização automatizada de dependências com revisão.

Nunca imprimir `.env` no CI.

## 12. Estratégia de implementação

Executar em PRs pequenos, nesta ordem:

1. Testes que reproduzem autorização desatualizada e escalonamento.
2. Provider de autorização atual do banco.
3. Handlers e policies Admin/SuperAdmin.
4. Migração dos contextos de acesso e testes IDOR.
5. Lockout e rate limiting.
6. Limites do webhook e validação de headers.
7. Validação de URLs.
8. CSP, HSTS e demais headers.
9. Upload seguro somente quando houver requisito funcional.

Cada PR deve preservar todas as mudanças locais não relacionadas e não deve misturar refatoração estética com segurança.

## 13. Critérios de aceite finais

- [ ] Token emitido antes da remoção de uma role perde acesso na requisição seguinte.
- [ ] Usuário removido/bloqueado não consegue continuar usando JWT antigo.
- [ ] Rotas de usuários, roles e permissions confirmam Admin/SuperAdmin no banco.
- [ ] Nenhuma decisão de backend usa role ou permission enviada pelo frontend.
- [ ] Todos os endpoints não públicos retornam `401` sem token.
- [ ] Usuário autenticado sem permissão recebe `403`.
- [ ] Todo endpoint por ID possui teste de acesso cruzado.
- [ ] IDs filhos são validados contra o recurso pai.
- [ ] Login possui lockout e rate limit testados.
- [ ] Webhook rejeita corpo grande, header duplicado/malformado e replay.
- [ ] API e frontend publicam headers defensivos.
- [ ] Não existem sinks XSS inseguros.
- [ ] `PhotoUrl` e `AttachmentUrl` aceitam somente URLs aprovadas.
- [ ] Se upload existir, conteúdo, tamanho, dimensões e magic bytes são validados no servidor.
- [ ] Logs e erros não contêm JWT, senha, PIX, assinatura HMAC ou payload sensível.
- [ ] Testes backend e frontend passam integralmente.

## 14. Comandos de validação

```bash
dotnet test backend/CorePay.slnx
dotnet test frontend/CorePay.Frontend.slnx
dotnet list backend/CorePay.slnx package --vulnerable --include-transitive
git diff --check
```

Executar também testes dinâmicos em ambiente isolado para:

- alteração manual de GUIDs;
- reutilização de token após revogação;
- tentativa de promover usuário sem ser SuperAdmin;
- payloads XSS persistentes em todos os campos textuais;
- headers duplicados e muito longos;
- corpos acima do limite;
- arquivos com extensão falsa, dupla extensão e magic bytes incorretos.

## 15. Fora de escopo deste documento

- Reescrever regras de negócio.
- Alterar migrations sem necessidade técnica da correção.
- Implementar upload antes de existir requisito funcional.
- Considerar ocultação de botões no frontend como controle de segurança.
- Reescrever histórico Git automaticamente.
