# ADR-004: Dead Letter Queue com retry no consumer Kafka

## Status
Aceito

## Contexto
Falhas transitórias (timeout de DB, broker indisponível) e payloads inválidos no tópico
`cashflow.entries` não podem bloquear o consumer group nem perder eventos silenciosamente.

## Decisão
No `Consolidado.Api` (`KafkaConsumerWorker`):

1. Processar cada mensagem com até **3 tentativas**
2. Backoff exponencial entre tentativas: **1s → 2s → 4s**
3. Após esgotar retries (ou falha de desserialização), publicar envelope no tópico
   **`cashflow.entries.dlq`** e **sempre** fazer commit do offset original
4. Envelope DLQ inclui: `reason`, `originalTopic`, `partition`, `offset`, `key`, `payload`, `failedAt`

## Consequências
- Consumer não trava em poison message
- Operação pode reprocessar a partir da DLQ
- Offset avança mesmo em falha permanente (at-least-once + idempotência via `processed_events`)
