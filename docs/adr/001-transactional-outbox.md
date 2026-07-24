# ADR-001: Transactional Outbox para integração entre serviços

## Status
Aceito

## Contexto
Lançamentos e Consolidado são serviços independentes. O consolidado pode ficar indisponível.

## Decisão
Usar Transactional Outbox: gravar evento na mesma transação do lançamento e publicar via worker assíncrono.

## Consequências
- Escrita nunca bloqueia por falha do consolidado
- Eventual consistency no saldo
- Worker adicional para operar
