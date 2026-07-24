$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

if (-not (Test-Path ".env")) {
    if (-not (Test-Path ".env.example")) {
        throw ".env.example not found. Cannot bootstrap local secrets."
    }
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example." -ForegroundColor Yellow
}

Write-Host "Subindo PostgreSQL, Kafka e APIs..." -ForegroundColor Cyan
docker compose --env-file .env up -d --build

Write-Host "Aguardando servicos..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host ""
Write-Host "Servicos disponiveis:" -ForegroundColor Green
Write-Host "  Frontend:    http://localhost:3000"
Write-Host "  Lancamentos: http://localhost:5001"
Write-Host "  Consolidado: http://localhost:5002"
Write-Host "  Kafka UI:    http://localhost:8080"
Write-Host "  Adminer:     http://localhost:8081  (server: postgres-lancamentos | postgres-consolidado - user/password from your local .env)"
Write-Host ""
Write-Host "Health:" -ForegroundColor Green
Invoke-RestMethod http://localhost:5001/health | ConvertTo-Json
Invoke-RestMethod http://localhost:5002/health | ConvertTo-Json
