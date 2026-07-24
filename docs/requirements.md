# Requisitos refinados

## Funcionais

| ID | Requisito | Implementação |
|----|-----------|---------------|
| RF01 | Registrar lançamento de crédito | `POST /entries` (Lancamentos.Api) |
| RF02 | Registrar lançamento de débito | `POST /entries` (Lancamentos.Api) |
| RF03 | Listar lançamentos por data | `GET /entries?entry_date=` |
| RF04 | Consultar saldo diário consolidado | `GET /balances/{date}` (Consolidado.Api) |
| RF05 | Listar saldos por período | `GET /balances?start_date=&end_date=` |

## Não funcionais

| ID | Requisito | Meta | Como foi atendido |
|----|-----------|------|-------------------|
| RNF01 | Lançamentos disponível se consolidado cair | 100% uptime escrita | Outbox transacional; eventos ficam pendentes |
| RNF02 | Consolidado aguenta pico | 50 req/s, ≤5% perda | Cache + rate limit 100/s + script load-test |
| RNF03 | Consistência do saldo | Exatamente-uma-vez lógica | Idempotência por EventId |
| RNF04 | Rastreabilidade | Correlation ID | Middleware `X-Correlation-Id` + Serilog |
| RNF05 | Execução local reproduzível | Docker Compose | Postgres + Kafka + 2 APIs |

## Diferenciais

| ID | Requisito | Documento |
|----|-----------|-----------|
| D01 | Arquitetura de transição | [transition-architecture.md](transition-architecture.md) |
| D02 | Estimativa de custos | [finops-security-observability.md](finops-security-observability.md) |
| D03 | Observabilidade | Serilog + /health + doc OpenTelemetry |
| D04 | Segurança na integração | Kafka PLAINTEXT local; SASL/mTLS em produção |
