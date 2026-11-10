# Publica o BFF corrigido na VM slotfy-demo, reinicia e confere os endpoints.
# Uso:  cd C:\AI\PaddockSpace\qa ; .\deploy_vm.ps1
# Opcional: -Project meu-projeto
param(
  [string]$Vm = "slotfy-demo",
  [string]$Zone = "us-central1-c",
  [string]$Project = "",
  [string]$Site = "https://paddockspace.com.br"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path -Parent $PSScriptRoot
$pub  = Join-Path $root "deploy\publish-bff-self-contained"
$tgz  = Join-Path $root "deploy\bff-self-contained-update.tar.gz"
$proj = @(); if ($Project) { $proj = @("--project", $Project) }

function Run($exe, $argList) {
  & $exe @argList
  if ($LASTEXITCODE -ne 0) { throw "Falhou: $exe $($argList -join ' ') (codigo $LASTEXITCODE)" }
}

Write-Host "1/5 dotnet publish (linux-x64, self-contained)" -ForegroundColor Cyan
Run "dotnet" @("publish", (Join-Path $root "src\PaddockSpace.Bff.Web"), "-c", "Release", "-r", "linux-x64", "--self-contained", "true", "-o", $pub)

Write-Host "2/5 empacotando (sem o banco)" -ForegroundColor Cyan
Get-ChildItem $pub -Filter "*.db" -ErrorAction SilentlyContinue | Remove-Item -Force
if (Test-Path $tgz) { Remove-Item $tgz -Force }
Run "tar" @("-czf", $tgz, "-C", $pub, ".")

Write-Host "3/5 enviando para a VM" -ForegroundColor Cyan
Run "gcloud" (@("compute","scp",$tgz,"${Vm}:/tmp/bff-self-contained-update.tar.gz","--zone",$Zone) + $proj)

Write-Host "4/5 instalando e reiniciando na VM (com backup do banco)" -ForegroundColor Cyan
$remote = @'
set -e
D=/var/www/paddockspace/bff-web
sudo systemctl stop paddockspace-bff
cp $D/paddockspace.db ~/paddockspace.db.bak-$(date +%Y%m%d-%H%M)
sudo tar -xzf /tmp/bff-self-contained-update.tar.gz -C $D
sudo chmod +x $D/PaddockSpace.Bff.Web
sudo chown -R detroytcorp:detroytcorp $D
sudo chmod -R u=rwX,g=rX,o=rX $D
sudo chmod +x $D/PaddockSpace.Bff.Web
sudo systemctl start paddockspace-bff
sleep 4
systemctl is-active paddockspace-bff
'@ -replace "`r",""
Run "gcloud" (@("compute","ssh",$Vm,"--zone",$Zone) + $proj + @("--command",$remote))

Write-Host "5/5 conferindo endpoints (esperado: 200 200 200 200 400)" -ForegroundColor Cyan
$urls = "drivers/ascari?season=all","teams/brabham?season=all","races?season=all","circuits/interlagos","races?season=1900"
foreach ($u in $urls) {
  $code = & curl.exe -s -o NUL -w "%{http_code}" "$Site/api/web/v1/f1/$u"
  Write-Host ("{0,-32} -> {1}" -f $u, $code)
}
