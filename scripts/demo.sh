#!/usr/bin/env bash
set -euo pipefail

DATE="$(date -u +%Y-%m-%d)"

echo "=== Demo Fluxo de Caixa ==="
echo "Data do lancamento: $DATE"

echo ""
echo "[1] Registrar credito R$ 100,00"
curl -s -X POST http://localhost:5001/entries \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"$(uuidgen | tr '[:upper:]' '[:lower:]')\",\"type\":\"Credit\",\"amount\":100.00,\"date\":\"$DATE\",\"description\":\"Venda no caixa\"}"
echo ""

echo ""
echo "[2] Registrar debito R$ 25,50"
curl -s -X POST http://localhost:5001/entries \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"$(uuidgen | tr '[:upper:]' '[:lower:]')\",\"type\":\"Debit\",\"amount\":25.50,\"date\":\"$DATE\",\"description\":\"Pagamento fornecedor\"}"
echo ""

echo ""
echo "[3] Aguardando outbox + Kafka (10s)..."
sleep 10

echo ""
echo "[4] Consultar saldo consolidado"
BALANCE_JSON="$(curl -s "http://localhost:5002/balances/$DATE")"
echo "$BALANCE_JSON"

BALANCE="$(echo "$BALANCE_JSON" | sed -n 's/.*"balance"[[:space:]]*:[[:space:]]*\([0-9.]*\).*/\1/p')"
if [[ "$BALANCE" == "74.50" || "$BALANCE" == "74.5" ]]; then
  echo ""
  echo "[OK] Saldo esperado: 74.50"
else
  echo ""
  echo "[FALHA] Saldo esperado 74.50, obtido ${BALANCE:-unknown}"
  exit 1
fi

echo ""
echo "[5] Health lancamentos"
curl -s http://localhost:5001/health
echo ""
echo ""
echo "Demo concluida com sucesso."
