# Roda tudo: validação do banco -> testes do site -> coleta -> comparação site x banco.
# Uso (PowerShell, dentro de C:\AI\PaddockSpace\qa):
#   .\run_all.ps1                                   # site local: vite (:5173) + BFF (:5180) já rodando
#   .\run_all.ps1 -BaseUrl https://paddockspace.com.br -Db .\vm\paddockspace.db
# Para comparar com PRODUÇÃO, use uma cópia do .db da VM (senão aparecem diferenças só de versão):
#   scp <usuario>@<ip-da-vm>:/var/www/paddockspace/bff-web/paddockspace.db .\vm\paddockspace.db
param(
  [string]$BaseUrl = "http://localhost:5173",
  [string]$Db = "..\deploy\paddockspace.db"
)
$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING = "utf-8"
$env:BASE_URL = $BaseUrl

# --- pré-requisitos ---
if (-not (Test-Path node_modules)) { npm install }
npx playwright install chromium --no-shell
if ($LASTEXITCODE -ne 0) {
  Write-Host "`nFalha ao baixar o navegador (certificado 'self-signed' = proxy/antivírus interceptando HTTPS)." -ForegroundColor Yellow
  Write-Host "Solução segura: exporte o certificado raiz da sua rede/antivírus em .pem e rode:"
  Write-Host '   $env:NODE_EXTRA_CA_CERTS = "C:\caminho\certificado-raiz.pem"; npx playwright install chromium --no-shell'
}

# --- o site está no ar? (evita 68 testes falhando por ECONNREFUSED) ---
try { Invoke-WebRequest "$BaseUrl/api/web/v1/context" -UseBasicParsing -TimeoutSec 15 | Out-Null }
catch {
  Write-Host "`nNão consegui acessar $BaseUrl/api/web/v1/context: $($_.Exception.Message)" -ForegroundColor Red
  Write-Host "Local: suba o BFF (dotnet run --project ..\src\PaddockSpace.Bff.Web) e o front (cd ..\src\PaddockSpace.Web; npm run dev), ou use -BaseUrl https://paddockspace.com.br"
  exit 2
}

$temDb = Test-Path $Db
if (-not $temDb) { Write-Host "`nBanco '$Db' não encontrado: passos 1 e 4 serão pulados." -ForegroundColor Yellow }

$r1 = $r4 = "pulado"
if ($temDb) {
  Write-Host "`n=== 1/4 Validação do banco ===" -ForegroundColor Cyan
  python scripts/validate_db.py $Db; $r1 = $LASTEXITCODE
}

Write-Host "`n=== 2/4 Testes do site (API + UI) ===" -ForegroundColor Cyan
npx playwright test; $r2 = $LASTEXITCODE

Write-Host "`n=== 3/4 Coleta de dados do site ===" -ForegroundColor Cyan
node scripts/collect.mjs; $r3 = $LASTEXITCODE

if ($temDb -and (Test-Path output\snapshot.json)) {
  Write-Host "`n=== 4/4 Comparação site x banco ===" -ForegroundColor Cyan
  python scripts/compare_site_db.py output/snapshot.json $Db; $r4 = $LASTEXITCODE
}

Write-Host "`nResumo: banco=$r1  testes=$r2  coleta=$r3  comparacao=$r4  (0 = ok)"
Write-Host "Relatório HTML dos testes: npx playwright show-report"
