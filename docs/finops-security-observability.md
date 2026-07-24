# FinOps, Segurança e Observabilidade

## Estimativa de custos Azure (Brasil South)

| Recurso | SKU estimado | Custo mensal (R$) |
|---------|--------------|-------------------|
| App Service (2x) | B1 Linux | ~R$ 220 |
| PostgreSQL Flexible (2 DBs) | Burstable B1ms | ~R$ 180 |
| Event Hubs | Basic, 1 TU | ~R$ 45 |
| Redis Cache | Basic C0 | ~R$ 70 |
| Application Insights | Pay-as-you-go baixo volume | ~R$ 30 |
| **Total estimado** | | **~R$ 545/mês** |

Valores aproximados para MVP de baixo tráfego. Produção exigiria sizing por métricas reais.

## Segurança

### Integração entre serviços

| Camada | MVP local | Produção |
|--------|-----------|----------|
| Kafka | PLAINTEXT | SASL_SSL + ACLs por tópico |
| APIs públicas | Sem auth | JWT (Entra ID) via API Management |
| APIs internas | Rede Docker | Private Link / mTLS |
| Secrets | appsettings | Azure Key Vault |

### LGPD

- Dados financeiros em região Brasil South
- Retenção definida por política (ex: 5 anos)
- Logs sem PII em texto claro

## Observabilidade

| Sinal | Implementação MVP | Evolução |
|-------|-------------------|----------|
| Logs | Serilog estruturado + CorrelationId | OpenTelemetry Logs |
| Métricas | /health (outbox pending) | Prometheus / Azure Monitor |
| Traces | — | OpenTelemetry distributed tracing |
| Alertas | — | Lag Kafka > threshold, outbox > N |
