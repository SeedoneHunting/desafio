# Mensageria — Outbox + Kafka

## Por que Kafka?

- **Desacoplamento temporal**: o consolidado pode estar offline sem bloquear lançamentos
- **Durabilidade**: eventos persistidos no broker até consumo
- **Escalabilidade**: múltiplos consumidores futuros (relatórios, auditoria, antifraude)
- **Replay**: reprocessamento de eventos para correção ou migração

## Fluxo

1. `POST /entries` → transação Postgres grava `entries` + `outbox_messages`
2. Outbox Worker publica no tópico `cashflow.entries`
3. Consolidado consome, projeta saldo, commit manual de offset
4. Se Kafka indisponível, outbox acumula e retenta

## Garantias

| Aspecto | Garantia |
|---------|----------|
| Entrega | At-least-once (Kafka) |
| Ordem | Por partição (single partition no MVP) |
| Duplicatas | Tratadas por idempotência (`processed_events`) |
| Consistência DB→Kafka | Outbox pattern (mesma transação) |

## Configuração

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "cashflow.entries",
    "ConsumerGroup": "consolidado-service"
  }
}
```

## Evolução

- Dead Letter Topic para mensagens inválidas
- Schema Registry (Avro) para evolução de contrato
- SASL_SSL em produção
