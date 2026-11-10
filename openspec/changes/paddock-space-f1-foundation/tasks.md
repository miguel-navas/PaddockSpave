# Tasks

## 1. Estrutura da Solution .NET 10, Domínio DDD e Aplicação Core

- [x] 1.1 Criar a solution `PaddockSpace.sln` em .NET 10 com os projetos `PaddockSpace.Domain`, `PaddockSpace.Application`, `PaddockSpace.Infrastructure`, `PaddockSpace.Bff.Web`, `PaddockSpace.Bff.Mobile`, `PaddockSpace.SyncWorker` e `PaddockSpace.Tests`, e verificar que `dotnet build` compila sem erros.
- [x] 1.2 Implementar em `PaddockSpace.Domain` os Value Objects (`SeasonScope`, `CategoryCode`) e Agregados/Entidades (`Driver`, `ConstructorTeam`, `Circuit`, `GrandPrix`, `SeasonSummary`, `ChampionBio`) seguindo DDD e SOLID, e verificar com testes unitários em `PaddockSpace.Tests` cobrindo as regras de `SeasonScope` (`2026`, `1950..2025`, `all`).
- [x] 1.3 Definir em `PaddockSpace.Application` os contratos de leitura e os Query Handlers/Use Cases para Dashboard, Pilotos, Equipes, Classificações, Provas/GPs, Circuitos, Linha do Tempo e Hall da Fama, e verificar com testes unitários que o roteamento por `SeasonScope` seleciona os contratos corretos.

## 2. Infraestrutura de Leitura SQL Server (`PaddockSpace`)

- [x] 2.1 Configurar em `PaddockSpace.Infrastructure` o acesso ao banco local `PaddockSpace` via EF Core (`AsNoTracking`) e Dapper mapeando as 26 tabelas e as 13 views analíticas (`vw_Calendario`, `vw_Vencedores`, `vw_ClassificacaoPilotos`, `vw_ClassificacaoEquipes`, `vw_EstatisticasPilotos`, `vw_PitStopsMaisRapidos`, `vw_EstrategiaPneus`, `vw_ClimaSessao`, `vw_ResultadosTodos`, `vw_GrandesPremiosTodos`, `vw_Campeoes`, `vw_CarreiraPilotos`, `vw_CarreiraEquipes`), e verificar a conexão e leitura real com testes de integração de leitura.
- [x] 2.2 Implementar os repositórios de leitura unificados para Temporada Atual (2026 com telemetria), Temporada Histórica (1950–2025) e All-Time, e verificar que consultas para `2026`, `1991` e `all` retornam dados consistentes do banco `PaddockSpace`.

## 3. Serviço de Sincronização e Manutenção de Dados (`PaddockSpace.SyncWorker`)

- [x] 3.1 Implementar em `PaddockSpace.Infrastructure` os clientes HTTP `JolpicaApiClient` e `OpenF1ApiClient` com rate-limiting (`0.6s`/`7.5s` e `0.7s`) e retry, mais o leitor de `mdb_extras.json`, e verificar com testes unitários que o rate limit e o parsing JSON funcionam conforme esperado.
- [x] 3.2 Implementar o repositório `F1DataIngestionRepository` restrito exclusivamente a `INSERT`/`UPSERT` de dados nas tabelas existentes e log em `ExecucoesAgente` (sem qualquer comando DDL), e verificar por testes e inspeção que nenhuma instrução fora do escopo de alimentação de dados é permitida.
- [x] 3.3 Configurar o `PaddockSpace.SyncWorker` (modo incremental 2026, complementação histórica 1950–2025 de onde parou e importação de `CampeoesBiografia` e `FotosHistoricas`), executar a importação das biografias/fotos e validar que `CampeoesBiografia` e `FotosHistoricas` estão populadas no banco `PaddockSpace`.

## 4. Camadas BFF para Web (`Bff.Web`) e Mobile (`Bff.Mobile`)

- [x] 4.1 Implementar os endpoints REST em `PaddockSpace.Bff.Web` (`/api/web/v1/{category}/...`) com payloads agregados para o Dashboard de Estatísticas, Pilotos, Equipes, Classificações, Provas (com telemetria), Circuitos, Linha do Tempo e Hall da Fama, e verificar via testes de endpoint e OpenAPI/Swagger que os contratos respondem com HTTP 200.
- [x] 4.2 Implementar os endpoints REST em `PaddockSpace.Bff.Mobile` (`/api/mobile/v1/{category}/...`) com payloads compactos, listas paginadas e carregamento sob demanda de telemetria, e verificar via testes de endpoint e documentação OpenAPI que o payload mobile é enxuto e funcional.

## 5. Portal Web em React (`PaddockSpace.Web`)

- [x] 5.1 Inicializar a aplicação React + TypeScript (Vite) em `src/PaddockSpace.Web`, configurar o cliente HTTP apontando para `PaddockSpace.Bff.Web` e implementar o layout principal com o **Seletor Global de Categoria e Temporada** no topo (`2026`, `1950..2025`, `Todas as Temporadas`), verificando que a mudança no seletor atualiza o contexto global da aplicação.
- [x] 5.2 Implementar o **Dashboard de Estatísticas** adaptativo (KPIs, gráficos de classificação, vitórias/poles, pit stops, estratégia de pneus e clima para 2026; recordes e dominância para histórico/All-Time), verificando a renderização para `2026`, um ano histórico e `Todas as Temporadas`.
- [x] 5.3 Implementar as páginas e visões de **Pilotos**, **Equipes**, **Classificação de Pilotos** e **Classificação de Equipes** com busca, ordenação e perfil detalhado, verificando a navegação e exibição dos dados reais da base.
- [x] 5.4 Implementar as páginas de **Provas (GPs)** (com abas de resultado, classificatório, sprint e telemetria), **Circuitos**, **Linha do Tempo** (1950–hoje) e **Hall da Fama** (biografias e fotos históricas), e verificar que o build de produção (`npm run build`) conclui sem erros.

