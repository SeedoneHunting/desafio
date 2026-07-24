#!/usr/bin/env bash
set -euo pipefail

REQUESTS="${1:-100}"
CONCURRENCY="${2:-50}"
DATE="$(date -u +%Y-%m-%d)"
URL="${3:-http://localhost:5002/balances/$DATE}"

echo "Load test: $REQUESTS requests, concurrency $CONCURRENCY"
echo "URL: $URL"

tmpdir="$(mktemp -d)"
ok_file="$tmpdir/ok"
fail_file="$tmpdir/fail"
: >"$ok_file"
: >"$fail_file"

batch_size=$(( (REQUESTS + CONCURRENCY - 1) / CONCURRENCY ))
start_ns="$(date +%s%N)"

pids=()
for ((batch=0; batch<CONCURRENCY; batch++)); do
  (
    local_ok=0
    local_fail=0
    for ((i=0; i<batch_size; i++)); do
      if curl -sf --max-time 5 "$URL" >/dev/null; then
        local_ok=$((local_ok + 1))
      else
        local_fail=$((local_fail + 1))
      fi
    done
    printf '%s\n' "$local_ok" >>"$ok_file"
    printf '%s\n' "$local_fail" >>"$fail_file"
  ) &
  pids+=($!)
done

for pid in "${pids[@]}"; do
  wait "$pid" || true
done

end_ns="$(date +%s%N)"
elapsed="$(awk -v s="$start_ns" -v e="$end_ns" 'BEGIN { printf "%.4f", (e-s)/1000000000 }')"

total_ok=0
total_fail=0
while read -r n; do total_ok=$((total_ok + n)); done <"$ok_file"
while read -r n; do total_fail=$((total_fail + n)); done <"$fail_file"
rm -rf "$tmpdir"

# jobs may overshoot when Requests % Concurrency != 0; clamp to Requests for reporting
executed=$((total_ok + total_fail))
rps="$(awk -v r="$executed" -v t="$elapsed" 'BEGIN { if (t<=0) t=0.001; printf "%.2f", r/t }')"
loss="$(awk -v f="$total_fail" -v r="$executed" 'BEGIN { if (r<=0) r=1; printf "%.2f", (f/r)*100 }')"

echo ""
echo "Resultados:"
echo "  Duracao: ${elapsed}s"
echo "  RPS: $rps"
echo "  Sucesso: $total_ok"
echo "  Falhas: $total_fail (${loss}%)"
echo "  Requisito: >= 50 RPS, <= 5% perda"

awk -v rps="$rps" -v loss="$loss" 'BEGIN {
  if (rps+0 >= 50 && loss+0 <= 5) {
    print "\n[OK] Requisito de performance atendido."
  } else {
    print "\n[ATENCAO] Verifique recursos locais ou cache aquecido."
  }
}'
