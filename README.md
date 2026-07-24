# Cashflow — Controle de Fluxo de Caixa (.NET)

Solução para o desafio de **Arquiteto de Soluções**: dois serviços .NET desacoplados via **Transactional Outbox + Apache Kafka**, PostgreSQL separado por serviço e projeção idempotente do saldo diário.

## Arquitetura

- **Frontend** (porta 3000): painel para lançamentos, saldos, outbox e eventos
- **Lancamentos.Api** (porta 5001): registra débitos/créditos, persiste outbox transacional
- **Consolidado.Api** (porta 5002): consome Kafka, projeta saldo diário, cache na leitura
- **PostgreSQL**: `lancamentos_db` + `consolidado_db`
- **Kafka**: tópico `cashflow.entries`
- **Kafka UI** (porta 8080): inspecionar mensagens do tópico
- **Adminer** (porta 8081): inspecionar tabelas nos bancos

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## Executar localmente (recomendado)

```powershell
# 1. Subir stack completa
.\scripts\start.ps1

# 2. Abrir o painel
start http://localhost:3000

# 3. Demonstração ponta a ponta (saldo esperado: R$ 74,50)
.\scripts\demo.ps1

# 4. Teste de carga (50 req concorrentes)
.\scripts\load-test.ps1
```

### Interfaces úteis

| URL | Uso |
|-----|-----|
| http://localhost:3000 | Painel (criar lançamento, ver saldo/outbox) |
| http://localhost:8080 | Kafka UI → tópico `cashflow.entries` |
| http://localhost:8081 | Adminer → Postgres (`cashflow`/`cashflow`) |

## Executar testes

```powershell
dotnet test Cashflow.sln
```

## APIs

### Lancamentos (5001)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/entries` | Registrar crédito ou débito |
| GET | `/entries?entry_date=` | Listar lançamentos |
| GET | `/health` | Status + outbox pendente |

### Consolidado (5002)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/balances/{date}` | Saldo consolidado do dia |
| GET | `/balances?start_date=&end_date=` | Listar saldos |
| GET | `/health` | Status do serviço |

### Exemplo

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5001/entries -ContentType "application/json" -Body '{"type":"Credit","amount":100.00,"date":"2026-01-25","description":"Venda"}'
Invoke-RestMethod -Method Post -Uri http://localhost:5001/entries -ContentType "application/json" -Body '{"type":"Debit","amount":25.50,"date":"2026-01-25","description":"Fornecedor"}'
Start-Sleep -Seconds 10
Invoke-RestMethod http://localhost:5002/balances/2026-01-25
```

## Resiliência

Se o consolidado cair, continue enviando lançamentos — o outbox acumula eventos e reprocessa quando o serviço voltar. Verifique `pendingOutboxCount` em `GET /health` do serviço de lançamentos.

## Documentação

- [Arquitetura](docs/architecture.md)
- [Requisitos](docs/requirements.md)
- [Mensageria (Kafka)](docs/messaging.md)
- [Arquitetura de transição](docs/transition-architecture.md)
- [FinOps, segurança e observabilidade](docs/finops-security-observability.md)
- [Evidências](docs/evidence.md)
- [ADRs](docs/adr/)
