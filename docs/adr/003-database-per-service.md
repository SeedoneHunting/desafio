# ADR-003: Database-per-service com PostgreSQL

## Status
Aceito

## Contexto
Cada serviço possui modelo de dados distinto (ledger vs projeção de saldo).

## Decisão
Bancos PostgreSQL separados: `lancamentos_db` e `consolidado_db`.

## Consequências
- Sem acoplamento de schema entre serviços
- Consistência eventual entre bancos
- Migrations independentes por serviço
