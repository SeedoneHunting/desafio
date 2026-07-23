$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host "Subindo PostgreSQL, Kafka e APIs..." -ForegroundColor Cyan
docker compose up -d --build

Write-Host "Aguardando servicos..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host ""
Write-Host "Servicos disponiveis:" -ForegroundColor Green
Write-Host "  Lancamentos: http://localhost:5001"
Write-Host "  Consolidado: http://localhost:5002"
Write-Host ""
Write-Host "Health:" -ForegroundColor Green
Invoke-RestMethod http://localhost:5001/health | ConvertTo-Json
Invoke-RestMethod http://localhost:5002/health | ConvertTo-Json
