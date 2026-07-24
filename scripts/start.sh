#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

if [[ ! -f .env ]]; then
  if [[ ! -f .env.example ]]; then
    echo ".env.example not found. Cannot bootstrap local secrets." >&2
    exit 1
  fi
  cp .env.example .env
  echo "Created .env from .env.example."
fi

echo "Subindo PostgreSQL, Kafka e APIs..."
docker compose --env-file .env up -d --build

echo "Aguardando servicos..."
sleep 15

echo ""
echo "Servicos disponiveis:"
echo "  Frontend:    http://localhost:3000"
echo "  Lancamentos: http://localhost:5001"
echo "  Consolidado: http://localhost:5002"
echo "  Kafka UI:    http://localhost:8080"
echo "  Adminer:     http://localhost:8081  (server: postgres-lancamentos | postgres-consolidado)"
echo ""
echo "Health:"
curl -s http://localhost:5001/health
echo ""
curl -s http://localhost:5002/health
echo ""
