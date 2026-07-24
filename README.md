# Cashflow — Controle de Fluxo de Caixa (.NET)

Solução para o desafio de **Arquiteto de Soluções**: dois serviços .NET desacoplados via **Transactional Outbox + Apache Kafka**, PostgreSQL separado por serviço e projeção idempotente do saldo diário.

## Arquitetura

- **Frontend** (porta 3000): painel para lançamentos, saldos, outbox e eventos
- **Lancamentos.Api** (porta 5001): registra débitos/créditos, persiste outbox transacional
- **Consolidado.Api** (porta 5002): consome Kafka, projeta saldo diário, cache na leitura
- **PostgreSQL**: containers `postgres-lancamentos` + `postgres-consolidado` (database-per-service)
- **Kafka**: tópico `cashflow.entries` (+ DLQ `cashflow.entries.dlq`)
- **Kafka UI** (porta 8080): inspecionar mensagens do tópico
- **Adminer** (porta 8081): inspecionar tabelas nos bancos

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — suficiente para subir a stack
- [.NET 9 SDK](https://dotnet.microsoft.com/download) — apenas para `dotnet test` / desenvolvimento fora do container

Portas no host: `3000`, `5001`, `5002`, `5432`, `5433`, `8080`, `8081`, `9092`.

## Segredos / credenciais

Credenciais **não** ficam no código nem no `docker-compose.yml`.

1. Copie o exemplo: `Copy-Item .env.example .env`
2. Ajuste usuário/senha no `.env` (arquivo ignorado pelo Git)
3. Suba com `.\scripts\start.ps1` (cria `.env` automaticamente se ainda não existir)

Não versione o arquivo `.env`.

Variáveis esperadas (valores de exemplo em `.env.example`): `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB_LANCAMENTOS`, `POSTGRES_DB_CONSOLIDADO`, além das configs de Kafka e CORS.

## Executar localmente (recomendado)

O primeiro build das imagens pode levar alguns minutos.

```powershell
# 1. Subir stack completa (usa .env local)
.\scripts\start.ps1

# 2. Abrir o painel
start http://localhost:3000

# 3. Demonstração ponta a ponta (saldo esperado: R$ 74,50)
.\scripts\demo.ps1

# 4. Teste de carga (leitura do saldo, 100 req / 50 concorrentes)
.\scripts\load-test.ps1
```

Linux/macOS (ou Git Bash):

```bash
chmod +x scripts/*.sh
make up
make demo
make load-test
make test
make down
```

### Parar e limpar

```powershell
# Parar containers (mantém volumes/dados)
docker compose --env-file .env down

# Remover também volumes (zera os bancos)
docker compose --env-file .env down -v
```

Se o schema do Postgres mudar (`init-*.sql` / migrations), use `down -v` e suba novamente — volumes antigos não reexecutam o init.

### Interfaces úteis

| URL | Uso |
|-----|-----|
| http://localhost:3000 | Painel (criar lançamento, ver saldo/outbox) |
| http://localhost:8080 | Kafka UI → tópico `cashflow.entries` |
| http://localhost:8081 | Adminer → ver tabelas |
| localhost:5432 | Postgres lançamentos |
| localhost:5433 | Postgres consolidado |

**Adminer:** sistema PostgreSQL; servidor `postgres-lancamentos` ou `postgres-consolidado`; usuário, senha e base conforme o `.env`.

## Executar testes

```powershell
dotnet test Cashflow.sln
```

Testes automatizados não dependem do Docker. A demo ponta a ponta (`demo.ps1`) depende.

## APIs

### Lancamentos (5001)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/entries` | Registrar crédito ou débito |
| GET | `/entries?entry_date=` | Listar lançamentos |
| GET | `/health` | Status + outbox pendente |
| GET | `/admin/outbox` | Mensagens do outbox |

Body do `POST /entries`: `externalId` (UUID, obrigatório, idempotência), `type` (`Credit`\|`Debit`), `amount` (> 0), `date` (`yyyy-MM-dd`), `description`.

### Consolidado (5002)

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/balances/{date}` | Saldo consolidado do dia |
| GET | `/balances?start_date=&end_date=` | Listar saldos |
| GET | `/health` | Status do serviço |
| GET | `/admin/processed-events` | Eventos já projetados |

### Exemplo

```powershell
$ext1 = [guid]::NewGuid().ToString()
$ext2 = [guid]::NewGuid().ToString()

Invoke-RestMethod -Method Post -Uri http://localhost:5001/entries -ContentType "application/json" -Body @"
{"externalId":"$ext1","type":"Credit","amount":100.00,"date":"2026-01-25","description":"Venda"}
"@

Invoke-RestMethod -Method Post -Uri http://localhost:5001/entries -ContentType "application/json" -Body @"
{"externalId":"$ext2","type":"Debit","amount":25.50,"date":"2026-01-25","description":"Fornecedor"}
"@

Start-Sleep -Seconds 10
Invoke-RestMethod http://localhost:5002/balances/2026-01-25
```

## Resiliência

Se o consolidado cair, continue enviando lançamentos — o outbox acumula eventos e reprocessa quando o serviço voltar. Verifique `pendingOutboxCount` em `GET /health` do serviço de lançamentos.

Falhas persistentes no consumo vão para o tópico DLQ `cashflow.entries.dlq` (visível no Kafka UI).

## Documentação

- [Arquitetura](docs/architecture.md)
- [Requisitos](docs/requirements.md)
- [Mensageria (Kafka)](docs/messaging.md)
- [Arquitetura de transição](docs/transition-architecture.md)
- [FinOps, segurança e observabilidade](docs/finops-security-observability.md)
- [Evidências](docs/evidence.md)
- [ADRs](docs/adr/)
- [Enunciado do desafio](docs/desafio.md)
