#!/usr/bin/env python3
"""Valida a integridade do banco SQLite do Paddock Space (somente leitura).

Uso:  python scripts/validate_db.py [caminho.db]
Saída: ERRO (falha, exit code 1) | AVISO (esperado/investigar) | OK
"""
import sqlite3, sys, datetime as dt
from pathlib import Path

DB = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[2] / "deploy" / "paddockspace.db"
con = sqlite3.connect(f"file:{DB.as_posix()}?mode=ro", uri=True)
q = lambda s, *a: con.execute(s, a).fetchall()
one = lambda s, *a: con.execute(s, a).fetchone()[0]

results = []  # (nivel, nome, detalhe)
def check(name, sql, level="ERRO", detail_sql=None, expect_zero=True):
    n = one(sql)
    if n == 0:
        results.append(("OK", name, ""))
    else:
        det = ""
        if detail_sql:
            det = "; ".join(str(r) for r in q(detail_sql)[:5])
        results.append((level, name, f"{n} ocorrência(s) {det}"))

# ---------- Integridade física e referencial ----------
r = one("pragma integrity_check")
results.append(("OK" if r == "ok" else "ERRO", "SQLite integrity_check", r))

for nome, sql in {
    "Resultados históricos -> Pilotos": "select count(*) from HistResultadosCorrida r left join Pilotos p on p.PilotoId=r.PilotoId where p.PilotoId is null",
    "Resultados históricos -> Equipes": "select count(*) from HistResultadosCorrida r left join Equipes e on e.EquipeId=r.EquipeId where e.EquipeId is null",
    "Sprints históricas -> Pilotos": "select count(*) from HistResultadosSprint r left join Pilotos p on p.PilotoId=r.PilotoId where p.PilotoId is null",
    "Classificação histórica pilotos -> Pilotos": "select count(*) from HistClassificacaoPilotos r left join Pilotos p on p.PilotoId=r.PilotoId where p.PilotoId is null",
    "Classificação histórica equipes -> Equipes": "select count(*) from HistClassificacaoEquipes r left join Equipes e on e.EquipeId=r.EquipeId where e.EquipeId is null",
    "GPs históricos -> Circuitos": "select count(*) from HistGrandesPremios g left join Circuitos c on c.CircuitoId=g.CircuitoId where c.CircuitoId is null",
    "GPs 2026 -> Circuitos": "select count(*) from GrandesPremios g left join Circuitos c on c.CircuitoId=g.CircuitoId where c.CircuitoId is null",
    "Resultados 2026 -> Pilotos": "select count(*) from ResultadosCorrida r left join Pilotos p on p.PilotoId=r.PilotoId where p.PilotoId is null",
    "Resultados 2026 -> GPs": "select count(*) from ResultadosCorrida r left join GrandesPremios g on g.Rodada=r.Rodada where g.Rodada is null",
    "Biografias -> Pilotos": "select count(*) from CampeoesBiografia b where b.PilotoId is not null and not exists(select 1 from Pilotos p where p.PilotoId=b.PilotoId)",
    "Fotos históricas -> Biografias": "select count(*) from FotosHistoricas f where not exists(select 1 from CampeoesBiografia b where b.CampeaoId=f.CampeaoId)",
    "Sessões -> GPs 2026": "select count(*) from Sessoes s where not exists(select 1 from GrandesPremios g where g.Rodada=s.Rodada)",
}.items():
    check("FK órfã: " + nome, sql)

# ---------- Unicidade ----------
check("PilotoId duplicado", "select count(*) from (select 1 from Pilotos group by PilotoId having count(*)>1)")
check("EquipeId duplicada", "select count(*) from (select 1 from Equipes group by EquipeId having count(*)>1)")
check("CircuitoId duplicado", "select count(*) from (select 1 from Circuitos group by CircuitoId having count(*)>1)")
check("GP histórico duplicado (temporada, rodada)", "select count(*) from (select 1 from HistGrandesPremios group by Temporada,Rodada having count(*)>1)")
check("Resultado duplicado (temp, rodada, piloto) fora de pilotos que dividem carro (<=1967)",
      "select count(*) from (select 1 from HistResultadosCorrida where Temporada>1967 group by Temporada,Rodada,PilotoId having count(*)>1)")
check("Resultado duplicado (temp, rodada, piloto) <=1967 (carros divididos - esperado)",
      "select count(*) from (select 1 from HistResultadosCorrida where Temporada<=1967 group by Temporada,Rodada,PilotoId having count(*)>1)",
      level="AVISO")
check("Resultado 2026 duplicado (rodada, piloto)", "select count(*) from (select 1 from ResultadosCorrida group by Rodada,PilotoId having count(*)>1)")

# ---------- Domínio / faixas de valores ----------
check("Pontos negativos", "select count(*) from HistResultadosCorrida where Pontos<0")
check("Posição <= 0", "select count(*) from HistResultadosCorrida where Posicao<=0")
check("Pilotos sem nome/sobrenome", "select count(*) from Pilotos where coalesce(trim(Nome),'')='' or coalesce(trim(Sobrenome),'')=''")
check("Data de nascimento fora de 1800–2015", "select count(*) from Pilotos where coalesce(DataNascimento,'')<>'' and (substr(DataNascimento,1,4)<'1800' or substr(DataNascimento,1,4)>'2015')")
check("Circuito sem coordenadas válidas", "select count(*) from Circuitos where Latitude is null or Longitude is null or abs(Latitude)>90 or abs(Longitude)>180")
check("Equipe sem cor definida", "select count(*) from Equipes where coalesce(trim(Cor),'')=''", level="AVISO")
check("Largada negativa", "select count(*) from HistResultadosCorrida where Largada<0")
check("Tempo de volta <= 0 (2026)", "select count(*) from Voltas where TempoMs is not null and TempoMs<=0")
check("Pit stop com duração absurda (<1s ou >120s)", "select count(*) from PitStops where DuracaoMs<1000 or DuracaoMs>120000",
      level="AVISO", detail_sql="select Rodada,PilotoId,Volta,DuracaoMs from PitStops where DuracaoMs<1000 or DuracaoMs>120000")

# ---------- Consistência entre tabelas (histórico) ----------
check("Temporada sem GP", "select count(*) from HistTemporadas t where not exists(select 1 from HistGrandesPremios g where g.Temporada=t.Temporada)")
check("HistTemporadas.Corridas != nº de GPs", "select count(*) from HistTemporadas t where t.Corridas<>(select count(*) from HistGrandesPremios g where g.Temporada=t.Temporada)",
      detail_sql="select t.Temporada,t.Corridas,(select count(*) from HistGrandesPremios g where g.Temporada=t.Temporada) from HistTemporadas t where t.Corridas<>(select count(*) from HistGrandesPremios g where g.Temporada=t.Temporada)")
check("Cobertura de temporadas 1950–2025 (esperado 76)", "select case when count(*)=76 and min(Temporada)=1950 and max(Temporada)=2025 then 0 else 1 end from HistTemporadas")
check("GP sem nenhum resultado", "select count(*) from HistGrandesPremios g where not exists(select 1 from HistResultadosCorrida r where r.Temporada=g.Temporada and r.Rodada=g.Rodada)",
      detail_sql="select Temporada,Rodada,Nome from HistGrandesPremios g where not exists(select 1 from HistResultadosCorrida r where r.Temporada=g.Temporada and r.Rodada=g.Rodada)")
MULTI_WIN_OK = "((g.Temporada=1951 and g.Rodada=4) or (g.Temporada=1956 and g.Rodada=1) or (g.Temporada=1957 and g.Rodada=5))"
check("GP sem exatamente 1 vencedor (exceto vitórias divididas conhecidas: FRA-51, ARG-56, GBR-57)",
      f"select count(*) from (select g.Temporada,g.Rodada,sum(r.Posicao=1) n from HistGrandesPremios g join HistResultadosCorrida r on r.Temporada=g.Temporada and r.Rodada=g.Rodada where not {MULTI_WIN_OK} group by 1,2 having n<>1)",
      detail_sql=f"select g.Temporada,g.Rodada,g.Nome,sum(r.Posicao=1) n from HistGrandesPremios g join HistResultadosCorrida r on r.Temporada=g.Temporada and r.Rodada=g.Rodada where not {MULTI_WIN_OK} group by 1,2 having n<>1")
check("Vitórias na classificação != vitórias nos resultados", "select count(*) from HistClassificacaoPilotos h where h.Vitorias<>(select count(*) from HistResultadosCorrida r where r.Temporada=h.Temporada and r.PilotoId=h.PilotoId and r.Posicao=1)",
      level="AVISO", detail_sql="select h.Temporada,h.PilotoId,h.Vitorias from HistClassificacaoPilotos h where h.Vitorias<>(select count(*) from HistResultadosCorrida r where r.Temporada=h.Temporada and r.PilotoId=h.PilotoId and r.Posicao=1)")
check("Campeão (vw_Campeoes) != 1º da classificação", "select count(*) from vw_Campeoes v join HistClassificacaoPilotos h on h.Temporada=v.Temporada and h.Posicao=1 where abs(v.PontosCampeao-h.Pontos)>0.01")
check("Temporada sem 1º colocado na classificação de pilotos", "select count(*) from HistTemporadas t where not exists(select 1 from HistClassificacaoPilotos h where h.Temporada=t.Temporada and h.Posicao=1)")
check("Posição repetida na classificação de pilotos (empates - verificar critério)", "select count(*) from (select 1 from HistClassificacaoPilotos group by Temporada,Posicao having count(*)>1)",
      level="AVISO", detail_sql="select Temporada,Posicao,count(*) from HistClassificacaoPilotos group by 1,2 having count(*)>1 order by 1 desc")
check("Posição repetida na classificação de equipes (empates - verificar critério)", "select count(*) from (select 1 from HistClassificacaoEquipes group by Temporada,Posicao having count(*)>1)",
      level="AVISO", detail_sql="select Temporada,Posicao,count(*) from HistClassificacaoEquipes group by 1,2 having count(*)>1 order by 1 desc")
check("Pontos de pilotos (classificação) != soma de corrida+sprint, temporadas >= 2010",
      """select count(*) from HistClassificacaoPilotos h where h.Temporada>=2010 and abs(h.Pontos -
         (coalesce((select sum(Pontos) from HistResultadosCorrida r where r.Temporada=h.Temporada and r.PilotoId=h.PilotoId),0)
        + coalesce((select sum(Pontos) from HistResultadosSprint s where s.Temporada=h.Temporada and s.PilotoId=h.PilotoId),0)))>0.01""",
      level="AVISO", detail_sql="""select h.Temporada,h.PilotoId,h.Pontos,
         coalesce((select sum(Pontos) from HistResultadosCorrida r where r.Temporada=h.Temporada and r.PilotoId=h.PilotoId),0)
        +coalesce((select sum(Pontos) from HistResultadosSprint s where s.Temporada=h.Temporada and s.PilotoId=h.PilotoId),0) soma
         from HistClassificacaoPilotos h where h.Temporada>=2010 and abs(h.Pontos - soma)>0.01""")

# ---------- Temporada corrente (2026) ----------
check("Classificação 2026 pilotos: posições 1..N sem buracos", "select case when count(*)=max(Posicao) and count(distinct Posicao)=count(*) then 0 else 1 end from ClassificacaoPilotos")
check("Classificação 2026 equipes: posições 1..N sem buracos", "select case when count(*)=max(Posicao) and count(distinct Posicao)=count(*) then 0 else 1 end from ClassificacaoEquipes")
check("Pontos 2026 pilotos (classificação) != soma corrida+sprint",
      """select count(*) from ClassificacaoPilotos c where abs(c.Pontos -
         (coalesce((select sum(Pontos) from ResultadosCorrida r where r.PilotoId=c.PilotoId),0)
        + coalesce((select sum(Pontos) from ResultadosSprint s where s.PilotoId=c.PilotoId),0)))>0.01""",
      detail_sql="""select c.PilotoId,c.Pontos,
         coalesce((select sum(Pontos) from ResultadosCorrida r where r.PilotoId=c.PilotoId),0)
        +coalesce((select sum(Pontos) from ResultadosSprint s where s.PilotoId=c.PilotoId),0) soma
         from ClassificacaoPilotos c where abs(c.Pontos - soma)>0.01""")
check("Pontos 2026 equipes (classificação) != soma dos pilotos da equipe",
      """select count(*) from ClassificacaoEquipes c where abs(c.Pontos -
         (coalesce((select sum(Pontos) from ResultadosCorrida r where r.EquipeId=c.EquipeId),0)
        + coalesce((select sum(Pontos) from ResultadosSprint s where s.EquipeId=c.EquipeId),0)))>0.01""",
      level="AVISO", detail_sql="""select c.EquipeId,c.Pontos,
         coalesce((select sum(Pontos) from ResultadosCorrida r where r.EquipeId=c.EquipeId),0)
        +coalesce((select sum(Pontos) from ResultadosSprint s where s.EquipeId=c.EquipeId),0) soma
         from ClassificacaoEquipes c where abs(c.Pontos - soma)>0.01""")
check("AposRodada da classificação != última rodada concluída", "select case when (select max(AposRodada) from ClassificacaoPilotos)=(select count(*) from GrandesPremios where Status='CONCLUIDO') then 0 else 1 end", level="AVISO",
      detail_sql="select (select max(AposRodada) from ClassificacaoPilotos),(select count(*) from GrandesPremios where Status='CONCLUIDO')")
agora = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S")
check("GP já realizado (data < agora - 1 dia) sem Status CONCLUIDO",
      f"select count(*) from GrandesPremios where Status is not 'CONCLUIDO' and DataCorridaUTC < strftime('%Y-%m-%dT%H:%M:%S','{agora.replace('T',' ')}','-1 day')",
      detail_sql=f"select Rodada,Nome,DataCorridaUTC,Status from GrandesPremios where Status is not 'CONCLUIDO' and DataCorridaUTC < strftime('%Y-%m-%dT%H:%M:%S','{agora.replace('T',' ')}','-1 day')")
check("GP CONCLUIDO sem resultados de corrida", "select count(*) from GrandesPremios g where g.Status='CONCLUIDO' and not exists(select 1 from ResultadosCorrida r where r.Rodada=g.Rodada)",
      detail_sql="select Rodada,Nome from GrandesPremios g where g.Status='CONCLUIDO' and not exists(select 1 from ResultadosCorrida r where r.Rodada=g.Rodada)")
check("Resultados 2026 em GP não concluído", "select count(*) from ResultadosCorrida r join GrandesPremios g on g.Rodada=r.Rodada where g.Status is not 'CONCLUIDO'")

# ---------- Agente de sincronização ----------
ult = q("select ExecucaoId,Fim,Sucesso,Mensagem from ExecucoesAgente order by ExecucaoId desc limit 1")
if not ult:
    results.append(("ERRO", "Agente: nenhuma execução registrada", ""))
else:
    _, fim, ok, msg = ult[0]
    results.append(("OK" if ok == 1 else "ERRO", "Agente: última execução com sucesso", f"fim={fim}"))
    try:
        idade = dt.datetime.now(dt.timezone.utc) - dt.datetime.fromisoformat(fim[:19]).replace(tzinfo=dt.timezone.utc)
        results.append(("OK" if idade.days < 3 else "AVISO", "Agente: última execução recente (<3 dias)", f"{idade.days} dia(s) atrás"))
    except Exception as e:
        results.append(("AVISO", "Agente: data da última execução ilegível", str(e)))

# ---------- Contrato de esquema: colunas que o BFF consulta ----------
# (causa do 500 'no such column: r.VoltaRapidaNum' em /circuits/{id} no log da VM de 02/11)
exigidas = {
    "vw_ResultadosTodos": ["Temporada", "Rodada", "PilotoId", "EquipeId", "Largada", "Posicao", "Voltas", "VoltaRapidaRank"],
    "ResultadosCorrida": ["VoltaRapidaNum", "VoltaRapidaMs"],
    "vw_GrandesPremiosTodos": ["Temporada", "Rodada", "Nome", "CircuitoId", "DataCorridaUTC"],
    "HistResultadosCorrida": ["VoltaRapidaNum", "VoltaRapidaMs", "TempoTexto"],
}
for tabela, colunas in exigidas.items():
    existentes = {r[1] for r in q(f"pragma table_info({tabela})")}
    faltam = [c for c in colunas if c not in existentes]
    results.append(("OK" if not faltam else "ERRO", f"Esquema: {tabela} tem as colunas usadas pelo BFF", f"faltam: {faltam}" if faltam else ""))

# ---------- Contagens (informativo) ----------
for t in ["Pilotos", "Equipes", "Circuitos", "HistGrandesPremios", "HistResultadosCorrida", "HistResultadosSprint", "ResultadosCorrida", "Voltas"]:
    results.append(("INFO", f"Linhas em {t}", str(one(f"select count(*) from {t}"))))

# ---------- Relatório ----------
cores = {"OK": "\033[32m", "ERRO": "\033[31m", "AVISO": "\033[33m", "INFO": "\033[36m"}
for nivel, nome, det in results:
    print(f"{cores[nivel]}[{nivel:5}]\033[0m {nome}" + (f"  -> {det}" if det else ""))
erros = sum(1 for r in results if r[0] == "ERRO"); avisos = sum(1 for r in results if r[0] == "AVISO")
print(f"\nBanco: {DB}\nResumo: {erros} erro(s), {avisos} aviso(s), {sum(1 for r in results if r[0]=='OK')} ok")
sys.exit(1 if erros else 0)
