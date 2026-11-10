# Design

## Context

O banco SQL Server local `PaddockSpace` já possui 26 tabelas e 13 views analíticas criadas pelo agente inicial em `C:\AI\Agentes\Formula1` (`schema.sql` e `schema_historico.sql`). Os dados se dividem entre:
1. **Cadastros Mestres compartilhados**: `Pilotos`, `Equipes`, `Circuitos`.
2. **Temporada Atual (2026)**: `GrandesPremios`, `Sessoes`, `ResultadosCorrida`, `ResultadosSprint`, `Classificatorio`, `ClassificacaoPilotos`, `ClassificacaoEquipes`, `Voltas`, `PitStops`, `PilotosSessao`, `Stints`, `Clima`, `ControleProva`.
3. **Acervo Histórico (1950–2025)**: `HistTemporadas`, `HistGrandesPremios`, `HistResultadosCorrida`, `HistResultadosSprint`, `HistClassificatorio`, `HistClassificacaoPilotos`, `HistClassificacaoEquipes`.
4. **Editorial / Hall da Fama**: `CampeoesBiografia`, `FotosHistoricas`.
5. **Views Analíticas**: `vw_Calendario`, `vw_Vencedores`, `vw_ClassificacaoPilotos`, `vw_ClassificacaoEquipes`, `vw_EstatisticasPilotos`, `vw_PitStopsMaisRapidos`, `vw_EstrategiaPneus`, `vw_ClimaSessao`, `vw_ResultadosTodos`, `vw_GrandesPremiosTodos`, `vw_Campeoes`, `vw_CarreiraPilotos`, `vw_CarreiraEquipes`.

A solução precisa ser construída em **.NET 10** com **DDD, Clean Code e SOLID**, incluindo camadas de **BFF para Web e Mobile**, um **Worker de sincronização restrito a consultar dados externos e alimentar a base**, e um **front-end React** com Seletor Global de Categoria e Temporada.

## Goals / Non-Goals

**Goals:**
- Estruturar a solution .NET 10 seguindo DDD, Clean Architecture e SOLID, pronta para expansão multi-categoria (`Formula1`, `Formula2`, `Indy`, `Nascar`, `StockCar`, `Rally`, `MotoVelocidade`).
- Prover dois BFFs dedicados (`PaddockSpace.Bff.Web` e `PaddockSpace.Bff.Mobile`) que consomem a camada de Aplicação/Core e entregam contratos sob medida para cada canal.
- Aproveitar integralmente as tabelas e as 13 views existentes no banco `PaddockSpace` sem quebrar sua estrutura.
- Implementar o serviço de sincronização (`PaddockSpace.SyncWorker`) em .NET 10 com escopo estritamente limitado a consultar Jolpica/OpenF1/`mdb_extras.json` e gravar dados nas tabelas existentes.
- Construir o SPA em React + TypeScript com Seletor Global no topo e Dashboard de Estatísticas completo.

**Non-Goals:**
- Executar alterações destrutivas de schema ou migrações que quebrem a compatibilidade com o banco `PaddockSpace` atual.
- Implementar o aplicativo nativo Mobile nesta primeira change (aqui entregamos o `PaddockSpace.Bff.Mobile` pronto e documentado com OpenAPI/Swagger, mais o portal Web React completo).
- Ingerir dados das categorias futuras (F2, Indy, NASCAR, etc.) nesta fase inicial, mantendo apenas a arquitetura preparada para elas.

## Decisions

### 1. Organização da Solution .NET 10 e Camadas BFF
```text
+-----------------------------------------------------------------------------+
|                        ARQUITETURA PADDOCK SPACE                            |
+-----------------------------------------------------------------------------+
|                                                                             |
|   [React Web SPA]                                 [Futuro App Mobile]       |
|          |                                                 |                |
|          v (REST JSON Rico)                                v (REST Enxuto)  |
|  +-------------------------------+       +-------------------------------+  |
|  | PaddockSpace.Bff.Web          |       | PaddockSpace.Bff.Mobile       |  |
|  | - Agregacao p/ Dashboard Web  |       | - Payloads compactos/paginados|  |
|  | - Cache de resposta por visao |       | - Carregamento sob demanda    |  |
|  +-------------------------------+       +-------------------------------+  |
|                  |                                       |                  |
|                  +-------------------+-------------------+                  |
|                                      |                                      |
|                                      v                                      |
|  +-----------------------------------------------------------------------+  |
|  | PaddockSpace.Application                                              |  |
|  | - Casos de Uso / Queries CQRS (SeasonScope: Current | Year | AllTime) |  |
|  | - Contratos de Servico e Repositorios de Leitura/Sincronizacao        |  |
|  +-----------------------------------------------------------------------+  |
|                                      |                                      |
|                                      v                                      |
|  +-----------------------------------------------------------------------+  |
|  | PaddockSpace.Domain                                                   |  |
|  | - Entidades, Value Objects (SeasonScope, CategoryCode, LapTime)       |  |
|  | - Agregados: Driver, ConstructorTeam, Circuit, GrandPrix, HallOfFame  |  |
|  +-----------------------------------------------------------------------+  |
|                                      ^                                      |
|                                      |                                      |
|  +-----------------------------------------------------------------------+  |
|  | PaddockSpace.Infrastructure                                           |  |
|  | - Leitura SQL Server (EF Core AsNoTracking + Dapper p/ Views/Stats)   |  |
|  | - Clientes HTTP (JolpicaApiClient, OpenF1ApiClient c/ Rate Limiter)   |  |
|  | - Repositorio de Ingestao (SOMENTE Insert/Upsert nas tabelas de dados)|  |
|  +-----------------------------------------------------------------------+  |
|                                      ^                                      |
|                                      |                                      |
|  +-----------------------------------------------------------------------+  |
|  | PaddockSpace.SyncWorker (BackgroundService / CLI Mode)                |  |
|  | - Sincroniza temporada 2026, complementa 1950-2025 e mdb_extras.json  |  |
|  +-----------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------+
```

- **Racional para BFF em processo (Referência direta a `Application` + `Infrastructure`) vs. BFF via HTTP hop**:
  - Os projetos `PaddockSpace.Bff.Web` e `PaddockSpace.Bff.Mobile` são APIs .NET 10 independentes (podem rodar e escalar separadamente), referenciando a biblioteca `PaddockSpace.Application` e `PaddockSpace.Infrastructure`.
  - *Alternativa considerada*: Criar uma `Core.Api` intermediária e fazer os BFFs chamarem via HTTP interno. Descartado para o cenário atual porque adiciona latência de serialização desnecessária na mesma infraestrutura; referenciar `Application` diretamente nos dois BFFs preserva a separação física de deploy e contratos de API com latência mínima.

### 2. Estratégia Híbrida de Acesso a Dados (EF Core + Dapper)
- **Decisão**: Usar **EF Core (`AsNoTracking`)** para entidades de domínio e **Dapper** para consultas analíticas diretas sobre as 13 views (`vw_Campeoes`, `vw_CarreiraPilotos`, `vw_EstatisticasPilotos`, etc.) e agregações de Dashboard.
- **Racional**: As views do banco `PaddockSpace` já estão otimizadas em T-SQL. Consultar essas views via Dapper entrega performance máxima e mapeamento limpo para os ReadModels do CQRS.

### 3. Resolução Transparente do Seletor Global (`SeasonScope`)
- **Decisão**: Introduzir o Value Object `SeasonScope` (`Current` = 2026, `Historical(int year)` = 1950..2025, `AllTime` = 1950..2026).
- **Racional**: Como o banco separa as tabelas da temporada atual (`ResultadosCorrida`, `GrandesPremios`, etc.) das históricas (`HistResultadosCorrida`, `HistGrandesPremios`, etc.), os Query Handlers da camada `Application` selecionam automaticamente a fonte correta (ou as views unificadas `vw_ResultadosTodos` / `vw_GrandesPremiosTodos`) com base no `SeasonScope` recebido do BFF.

### 4. Isolamento e Guardrails do `PaddockSpace.SyncWorker`
- **Decisão**: O `SyncWorker` implementa um repositório dedicado `IF1DataIngestionRepository` que contém apenas instruções parametrizadas de `MERGE`/`UPDATE`/`INSERT` de linhas nas tabelas de dados e na tabela de auditoria `ExecucoesAgente`, com `RateLimiter` nativo (`System.Threading.RateLimiting`) configurado para `0.6s` (temporada atual) / `7.5s` (histórico Jolpica) e `0.7s` (OpenF1).
- **Racional**: Garante em código a restrição pedida pelo usuário: o backend executa **somente a consulta dos dados externos e alimenta a base, nada mais**.

## Risks / Trade-offs

- **[Risk] Rate Limit da API gratuita da Jolpica (500 req/h) durante a carga das temporadas restantes (2014–2025)** → **Mitigação**: Usar `TokenBucketRateLimiter` / intervalo de 7.5s entre chamadas no modo histórico e persistir cada temporada em uma transação individual em `HistTemporadas`, permitindo retomar exatamente de onde parou.
- **[Risk] Diferença de granularidade de dados entre 1950–1993, 1994–2020, 2021–2025 e 2026** → **Mitigação**: Os DTOs do `Bff.Web` e `Bff.Mobile` informam flags de disponibilidade (`hasTelemetry`, `hasQualifyingSplits`, `hasSprint`, `hasConstructorStandings`) para que a UI React renderize ou oculte abas de telemetria/sprint de forma elegante conforme o ano selecionado.

