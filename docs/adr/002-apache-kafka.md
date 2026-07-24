# ADR-002: Apache Kafka como broker de eventos

## Status
Aceito

## Contexto
Precisamos de desacoplamento durável entre lançamentos e consolidado, com possibilidade de replay e novos consumidores.

## Decisão
Adotar Apache Kafka com tópico `cashflow.entries` e consumer group dedicado.

## Consequências
- Infraestrutura adicional no Docker Compose
- Idempotência obrigatória no consumer (at-least-once)
- Escalabilidade horizontal futura
