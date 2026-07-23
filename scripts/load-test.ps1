param(
    [int]$Requests = 100,
    [int]$Concurrency = 50,
    [string]$Url = "http://localhost:5002/balances/2026-01-25"
)

$ErrorActionPreference = "Stop"
$jobs = @()
$batchSize = [Math]::Ceiling($Requests / $Concurrency)

Write-Host "Load test: $Requests requests, concurrency $Concurrency" -ForegroundColor Cyan
$sw = [System.Diagnostics.Stopwatch]::StartNew()

for ($batch = 0; $batch -lt $Concurrency; $batch++) {
    $jobs += Start-Job -ScriptBlock {
        param($url, $count)
        $ok = 0; $fail = 0
        1..$count | ForEach-Object {
            try {
                Invoke-RestMethod $url -TimeoutSec 5 | Out-Null
                $ok++
            } catch { $fail++ }
        }
        return @{ Ok = $ok; Fail = $fail }
    } -ArgumentList $Url, $batchSize
}

$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
$sw.Stop()

$totalOk = ($results | ForEach-Object { $_.Ok } | Measure-Object -Sum).Sum
$totalFail = ($results | ForEach-Object { $_.Fail } | Measure-Object -Sum).Sum
$rps = [Math]::Round($Requests / $sw.Elapsed.TotalSeconds, 2)
$lossPct = [Math]::Round(($totalFail / $Requests) * 100, 2)

Write-Host ""
Write-Host "Resultados:" -ForegroundColor Green
Write-Host "  Duracao: $($sw.Elapsed.TotalSeconds)s"
Write-Host "  RPS: $rps"
Write-Host "  Sucesso: $totalOk"
Write-Host "  Falhas: $totalFail ($lossPct%)"
Write-Host "  Requisito: >= 50 RPS, <= 5% perda"

if ($rps -ge 50 -and $lossPct -le 5) {
    Write-Host "`n[OK] Requisito de performance atendido." -ForegroundColor Green
} else {
    Write-Host "`n[ATENCAO] Verifique recursos locais ou cache aquecido." -ForegroundColor Yellow
}
