$ErrorActionPreference = "Stop"
$date = "2026-01-25"

Write-Host "=== Demo Fluxo de Caixa ===" -ForegroundColor Cyan

Write-Host "`n[1] Registrar credito R$ 100,00" -ForegroundColor Yellow
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/entries" -ContentType "application/json" -Body (@{
    type = "Credit"; amount = 100.00; date = $date; description = "Venda no caixa"
} | ConvertTo-Json) | ConvertTo-Json

Write-Host "`n[2] Registrar debito R$ 25,50" -ForegroundColor Yellow
Invoke-RestMethod -Method Post -Uri "http://localhost:5001/entries" -ContentType "application/json" -Body (@{
    type = "Debit"; amount = 25.50; date = $date; description = "Pagamento fornecedor"
} | ConvertTo-Json) | ConvertTo-Json

Write-Host "`n[3] Aguardando outbox + Kafka (10s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "`n[4] Consultar saldo consolidado" -ForegroundColor Yellow
$balance = Invoke-RestMethod "http://localhost:5002/balances/$date"
$balance | ConvertTo-Json

if ([decimal]$balance.balance -eq 74.50) {
    Write-Host "`n[OK] Saldo esperado: 74.50" -ForegroundColor Green
} else {
    Write-Host "`n[FALHA] Saldo esperado 74.50, obtido $($balance.balance)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[5] Health lancamentos (outbox pendente)" -ForegroundColor Yellow
Invoke-RestMethod http://localhost:5001/health | ConvertTo-Json

Write-Host "`nDemo concluida com sucesso." -ForegroundColor Green
