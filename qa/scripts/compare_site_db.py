#!/usr/bin/env python3
"""Compara o que o SITE entrega (output/snapshot.json) com o banco SQLite.

Uso:  python scripts/compare_site_db.py [snapshot.json] [paddockspace.db]
Dica: use o MESMO banco que o BFF está servindo (em produção, copie o .db da VM),
      senão aparecem diferenças que são só de versão.
"""
import json, sqlite3, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SNAP = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "output" / "snapshot.json"
DB = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT.parent / "deploy" / "paddockspace.db"
snap = json.loads(SNAP.read_text(encoding="utf-8"))
api = snap["api"]
con = sqlite3.connect(f"file:{DB.as_posix()}?mode=ro", uri=True)
q = lambda s, *a: con.execute(s, a).fetchall()

CURRENT = (api.get("context") or {}).get("currentSeason", 2026)
diffs, oks = [], 0
def eq(label, site, db, tol=0.01):
    global oks
    same = abs(float(site) - float(db)) <= tol if isinstance(site, (int, float)) and isinstance(db, (int, float)) else site == db
    if same: oks += 1
    else: diffs.append(f"{label}: site={site!r} banco={db!r}")

def seasons_in_snapshot(prefix):
    return sorted({k.split(":")[1] for k in api if k.startswith(prefix + ":")})

# ---------- Classificação de pilotos ----------
for s in seasons_in_snapshot("standings/drivers"):
    rows = api[f"standings/drivers:{s}"]
    if s == "all": continue
    year = int(s)
    if year == CURRENT:
        db = {r[0]: r for r in q("select PilotoId,Posicao,Pontos,Vitorias from ClassificacaoPilotos")}
    else:
        db = {r[0]: r for r in q("select PilotoId,Posicao,Pontos,Vitorias from HistClassificacaoPilotos where Temporada=?", year)}
    eq(f"[{s}] nº de pilotos na classificação", len(rows), len(db), 0)
    for r in rows:
        d = db.get(r["driverId"])
        if not d: diffs.append(f"[{s}] piloto {r['driverId']} está no site e não no banco"); continue
        eq(f"[{s}] {r['driverId']} posição", r["position"], d[1], 0)
        eq(f"[{s}] {r['driverId']} pontos", r["points"], d[2])
        eq(f"[{s}] {r['driverId']} vitórias", r["wins"], d[3], 0)

# ---------- Classificação de equipes ----------
for s in seasons_in_snapshot("standings/teams"):
    rows = api[f"standings/teams:{s}"]
    if s == "all": continue
    year = int(s)
    if year == CURRENT:
        db = {r[0]: r for r in q("select EquipeId,Posicao,Pontos,Vitorias from ClassificacaoEquipes")}
    else:
        db = {r[0]: r for r in q("select EquipeId,Posicao,Pontos,Vitorias from HistClassificacaoEquipes where Temporada=?", year)}
    eq(f"[{s}] nº de equipes na classificação", len(rows), len(db), 0)
    for r in rows:
        d = db.get(r["teamId"])
        if not d: diffs.append(f"[{s}] equipe {r['teamId']} está no site e não no banco"); continue
        eq(f"[{s}] {r['teamId']} posição", r["position"], d[1], 0)
        eq(f"[{s}] {r['teamId']} pontos", r["points"], d[2])
        eq(f"[{s}] {r['teamId']} vitórias", r["wins"], d[3], 0)

# ---------- Calendário (provas) ----------
for s in seasons_in_snapshot("races"):
    if s == "all": continue
    year = int(s); rows = api[f"races:{s}"]
    n = one = q("select count(*) from GrandesPremios")[0][0] if year == CURRENT else q("select count(*) from HistGrandesPremios where Temporada=?", year)[0][0]
    eq(f"[{s}] nº de GPs", len(rows), n, 0)
    # vencedor de cada GP conforme o site vs posição 1 no banco
    for r in rows:
        if not r.get("winnerName"): continue
        if year == CURRENT:
            w = q("select PilotoId from ResultadosCorrida where Rodada=? and Posicao=1", r["round"])
        else:
            w = q("select PilotoId from HistResultadosCorrida where Temporada=? and Rodada=? and Posicao=1", year, r["round"])
        if not w: diffs.append(f"[{s}] GP {r['round']} {r['name']}: site mostra vencedor '{r['winnerName']}' mas o banco não tem P1"); continue
        nomes = {x[0] for x in w}
        nomes_db = {f"{a} {b}".lower() for a, b in q(f"select Nome,Sobrenome from Pilotos where PilotoId in ({','.join('?'*len(nomes))})", *nomes)}
        if r["winnerName"].lower() in nomes_db: eq("", 1, 1, 0)
        else: diffs.append(f"[{s}] GP {r['round']} {r['name']}: vencedor site='{r['winnerName']}' banco={sorted(nomes_db)}")

# ---------- Linha do tempo / campeões ----------
champs = {r[0]: r for r in q("select Temporada,PontosCampeao,VitoriasCampeao,CampeaoPilotos,CampeaConstrutores from vw_Campeoes")}
tl = api.get("timeline") or []
eq("timeline: nº de temporadas", len(tl), len(champs), 0)
for r in tl:
    c = champs.get(r["season"])
    if not c: diffs.append(f"timeline {r['season']}: não existe em vw_Campeoes"); continue
    eq(f"timeline {r['season']} pontos do campeão", r.get("driverPoints"), c[1])
    eq(f"timeline {r['season']} vitórias do campeão", r.get("driverWins"), c[2], 0)

# ---------- Hall da Fama ----------
hof = api.get("hallOfFame") or []
eq("hall da fama: nº de campeões com biografia", len(hof), q("select count(*) from CampeoesBiografia")[0][0], 0)

# ---------- Catálogos ----------
if "circuits:all" in api:
    eq("circuitos (season=all)", len(api["circuits:all"]), q("select count(*) from Circuitos")[0][0], 0)
if "drivers:all" in api:
    eq("pilotos (season=all)", len(api["drivers:all"]), q("select count(*) from Pilotos")[0][0], 0)
if "teams:all" in api:
    eq("equipes (season=all)", len(api["teams:all"]), q("select count(*) from Equipes")[0][0], 0)

# ---------- Tela (UI) ----------
ui = snap.get("ui", {})
cl = ui.get("Classificação", {}).get("tables", [])
if cl and cl[0]["rows"]:
    cur = api.get(f"standings/drivers:{CURRENT}")
    if cur:
        eq("UI Classificação: nº de linhas da tabela de pilotos (temporada atual)", len(cl[0]["rows"]), len(cur), 0)
for aba, dados in ui.items():
    if dados.get("bodyTextLength", 1) < 50: diffs.append(f"UI '{aba}': tela praticamente vazia")
for e in snap.get("errors", []) + snap.get("uiErrors", []): diffs.append(f"coleta: {e}")

print(f"Snapshot: {SNAP.name} ({snap['collectedAt']}) | Banco: {DB.name}")
print(f"Comparações iguais: {oks} | Divergências: {len(diffs)}")
for d in diffs[:80]: print("  ✗", d)
if len(diffs) > 80: print(f"  ... e mais {len(diffs)-80}")
sys.exit(1 if diffs else 0)
