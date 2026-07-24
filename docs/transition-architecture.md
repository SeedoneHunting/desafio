# Arquitetura de transição

## Cenário atual (MVP)

- Monolito local substituído por 2 microsserviços .NET
- PostgreSQL + Kafka via Docker Compose
- Sem autenticação (desenvolvimento)

## Fase 1 — Containerização (atual)

- Docker Compose com Postgres, Kafka KRaft, 2 APIs
- EF Core Migrations versionadas
- CI GitHub Actions (build + test)

## Fase 2 — Cloud Azure

| Componente local | Serviço Azure |
|------------------|---------------|
| Lancamentos.Api | Azure App Service ou AKS |
| Consolidado.Api | Azure App Service ou AKS |
| PostgreSQL | Azure Database for PostgreSQL Flexible |
| Kafka | Azure Event Hubs (Kafka surface) ou Confluent Cloud |
| Cache | Azure Cache for Redis |

## Fase 3 — Segurança e governança

- Entra ID (JWT) no API Gateway
- mTLS entre serviços
- Private Endpoints para Postgres e Event Hubs
- Key Vault para secrets

## Fase 4 — Observabilidade

- OpenTelemetry → Azure Monitor / Application Insights
- Dashboards de lag Kafka, outbox pendente, latência P95

## Migração de legado (hipotético)

Se existisse um monolito com tabela única de lançamentos:

1. **Strangler Fig**: expor API de lançamentos na frente do legado
2. **Dual Write temporário**: legado + novo serviço (com feature flag)
3. **CDC** (Debezium) para backfill histórico no consolidado
4. **Cutover**: desligar escrita no legado após validação de saldos
