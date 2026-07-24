# ADR-003: Database-per-service com PostgreSQL

## Status
Aceito

## Contexto
Cada serviço possui modelo de dados distinto (ledger vs projeção de saldo).
Compartilhar um único container Postgres com dois databases ainda acopla falha,
capacidade e ciclo de vida de infraestrutura.

## Decisão
- Bancos separados: `lancamentos_db` e `consolidado_db`
- Containers separados no Compose: `postgres-lancamentos` e `postgres-consolidado`
- Init scripts por instância (`init-lancamentos.sql` / `init-consolidado.sql`)
- Schema aplicado por migrations EF de cada API

## Consequências
- Sem acoplamento de schema nem de runtime entre serviços
- Consistência eventual via Kafka
- Migrations e scaling independentes por serviço
- Host ports locais: 5432 (lançamentos) e 5433 (consolidado)
