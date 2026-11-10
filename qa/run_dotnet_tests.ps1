# Roda os testes .NET e grava a saída em qa\output\ (para o Claude ler direto da pasta).
# Enquanto os testes rodam, monitora o SQL Server (bloqueios/esperas) em qa\output\sql-bloqueios.txt.
# Uso: .\run_dotnet_tests.ps1        (de dentro de C:\AI\PaddockSpace\qa)
$raiz = Split-Path $PSScriptRoot -Parent
$saida = Join-Path $PSScriptRoot 'output'
New-Item -ItemType Directory -Force $saida | Out-Null
$arqMon = Join-Path $saida 'sql-bloqueios.txt'
Remove-Item $arqMon -ErrorAction SilentlyContinue
Set-Location $raiz

# Monitor em segundo plano (até ~3 min). Lista só requisições "reais", com quem bloqueia e o que esperam.
$mon = Start-Job -ArgumentList $arqMon -ScriptBlock {
  param($arq)
  $q = "SET NOCOUNT ON; SELECT r.session_id, r.blocking_session_id AS bloqueado_por, r.wait_type, r.wait_time AS espera_ms, r.status, r.command, LEFT(REPLACE(t.text, CHAR(10), ' '), 140) AS sql_texto FROM sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.session_id > 50 AND r.session_id <> @@SPID AND r.command NOT LIKE 'BRKR%' AND r.command NOT LIKE 'TASK MANAGER%' AND r.command NOT LIKE 'PARALLEL REDO%' AND r.command NOT LIKE 'HADR%'"
  1..250 | ForEach-Object {
    "--- " + (Get-Date -Format 'HH:mm:ss.fff') | Out-File $arq -Append -Encoding utf8
    sqlcmd -S localhost -E -d PaddockSpace -W -s "|" -Q $q 2>&1 | Out-File $arq -Append -Encoding utf8
    Start-Sleep -Milliseconds 700
  }
}

$sw = [Diagnostics.Stopwatch]::StartNew()
dotnet test --tl:off --nologo 2>&1 | ForEach-Object { $_ } | Out-File (Join-Path $saida 'dotnet-test.txt') -Encoding utf8
$sw.Stop()

Stop-Job $mon -ErrorAction SilentlyContinue; Remove-Job $mon -Force -ErrorAction SilentlyContinue

# Resumo só das falhas (nome + mensagem)
Select-String -Path (Join-Path $saida 'dotnet-test.txt') -Pattern '^\s*Failed ' -Context 0,12 |
  Out-File (Join-Path $saida 'dotnet-test-falhas.txt') -Encoding utf8

Get-Content (Join-Path $saida 'dotnet-test-falhas.txt')
Select-String -Path (Join-Path $saida 'dotnet-test.txt') -Pattern 'Passed!|Failed!' | ForEach-Object { $_.Line }
Write-Host ("`nDuração total: {0:n1}s" -f $sw.Elapsed.TotalSeconds)
Write-Host "Saída completa: $saida\dotnet-test.txt | Monitor SQL: $arqMon"
