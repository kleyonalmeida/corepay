# Seed dos cadastros Base44

A seed carrega setores, níveis de carreira e colaboradores sem alterar o domínio,
as migrations, as APIs, o frontend ou as regras de cálculo do CorePay.

Em produção, a seed é opcional e fica desabilitada por padrão. Quando habilitada,
é executada na inicialização do backend depois das migrations. Os CSVs são
incorporados à imagem Docker em `/app/seed`.

## Garantias

- cada ID de origem é convertido em memória para um GUID RFC 4122 determinístico;
- referências de setor e nível são remapeadas para os novos GUIDs antes da escrita;
- nenhum ID Base44 é persistido nas entidades operacionais;
- a ordem de carga é Setores → Níveis → Colaboradores;
- cabeçalhos, tipos, duplicidades e referências são validados antes da transação;
- uma segunda execução ignora os mesmos GUIDs, sem duplicar nem sobrescrever correções;
- valores numéricos vazios são normalizados para os defaults exigidos pela V1;
- datas ausentes ou inconsistentes usam a nullabilidade já existente na V1 e são relacionadas em `pendingModifications` no relatório externo;
- nenhuma pendência cria tabela, migration, notificação ou regra nova no sistema.

## Pré-requisitos

1. Faça backup do SQL Server.
2. Coloque no mesmo diretório:
   - `Department_export.csv`
   - `CareerLevel_export.csv`
   - `Collaborator_export.csv`
3. Configure `ConnectionStrings__DefaultConnection` ou passe `--connection-string`.

Não há migration específica para a seed.

## Executar a seed no Docker

Habilite a seed somente na execução desejada:

```bash
Seed__LoadLegacyData=true docker compose -f docker-compose.prod.yml up -d --build
```

O diretório configurado é `Seed__LegacyDataDirectory=/app/seed`. No log da API é
exibida a quantidade criada e o total de pendências de datas.

Depois da primeira execução, reinicie normalmente sem a variável (ou defina
`Seed__LoadLegacyData=false`). A seed também é idempotente e nunca sobrescreve os
registros existentes, mas mantê-la desabilitada evita processamento desnecessário.

Erros estruturais ou referências órfãs impedem a abertura da transação. Pendências de
datas não impedem a seed e são contabilizadas no log do backend para correção posterior
pelo fluxo normal do sistema.

## Idempotência e rollback

O GUID é derivado do tipo da entidade e do ID de origem. Reexecutar os mesmos arquivos
mantém os registros existentes intactos. A seed não exclui dados automaticamente; para
rollback operacional, restaure o backup realizado antes da carga.
