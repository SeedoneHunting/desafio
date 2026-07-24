# Evidências de execução

## Testes automatizados

```
dotnet test Cashflow.sln
Resultado: 6/6 aprovados (duracao ~4s)
```

Cenários cobertos:
- Criação de lançamento (sucesso e validação)
- Health com contagem de outbox
- Projeção idempotente de saldo
- Cálculo crédito − débito = 74,50
- Fluxo outbox → projeção consolidada

## Demo local (Docker)

```powershell
.\scripts\start.ps1
.\scripts\demo.ps1
```

Resultado esperado:
- Saldo consolidado em `2026-01-25`: **R$ 74,50**
- Outbox pendente tende a **0** após processamento

## Load test

```powershell
.\scripts\load-test.ps1 -Requests 100 -Concurrency 50
```

Métricas a registrar após execução local:
- RPS observado
- Taxa de falha (%)
- Requisito: ≥ 50 RPS, ≤ 5% perda

> Execute os scripts acima e atualize esta seção com os números reais do seu ambiente.
