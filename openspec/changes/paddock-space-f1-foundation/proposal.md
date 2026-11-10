# Proposal

## Why

O projeto **PaddockSpace** nasce para ser uma plataforma completa sobre velocidade (Fórmula 1, Fórmula 2, Indy, NASCAR, Stock Car, Rally e Motovelocidade), iniciando pela **Fórmula 1 (1950 até a atualidade)** sobre uma base SQL Server (`PaddockSpace`) já estruturada. Precisamos estabelecer a fundação arquitetural em **.NET 10 (DDD, Clean Code, SOLID)** com camadas **BFF dedicadas para Web e Mobile**, um **serviço de sincronização de dados restrito a consulta e carga**, e o **portal React** com seletor global de categoria/temporada e dashboard de estatísticas.

## What Changes

- **Criação do Core Backend em .NET 10 (DDD, Clean Code, SOLID)**:
  - Modelagem de domínio preparada para evolução multi-categoria, iniciando pelo contexto de **Fórmula 1**.
  - Camada de aplicação e infraestrutura mapeando as 26 tabelas e 13 views analíticas já existentes no banco SQL Server `PaddockSpace` (unificando temporada atual 2026, temporadas históricas 1950–2025 e visão All-Time).
- **Criação das Camadas BFF (Backend for Frontend) para Site e Mobile**:
  - `PaddockSpace.Bff.Web`: Endpoints agregados e otimizados para o portal e Dashboard React (payloads ricos combinando tabelas, séries temporais e telemetria).
  - `PaddockSpace.Bff.Mobile`: Endpoints enxutos, paginados e orientados a telas mobile (cards resumidos, menor payload de rede e cacheamento).
- **Serviço de Sincronização e Manutenção da Base (`PaddockSpace.SyncWorker`)**:
  - Assumir no backend a carga e manutenção contínua da base `PaddockSpace` (consumindo Jolpica F1 API, OpenF1 API e acervo `mdb_extras.json`), com a **ressalva estrita** de executar somente a consulta dos dados externos e alimentar (insert/upsert) as tabelas de dados existentes, sem alterar estrutura/schema nem executar operações fora desse escopo.
- **Criação do Portal Web em React (`PaddockSpace.Web`)**:
  - Topbar com **Seletor Global de Categoria e Temporada** (`2026`, `1950..2025` ou `Todas as Temporadas / All-Time`) que adapta todos os painéis conforme o ano escolhido.
  - Módulos de **Dashboard de Estatísticas**, **Pilotos**, **Equipes**, **Classificação de Pilotos**, **Classificação de Equipes**, **Provas (GPs e Telemetria)**, **Circuitos**, **Linha do Tempo** e **Hall da Fama**.

## Capabilities

### New Capabilities
- `f1-core-api`: Domínio DDD, casos de uso de consulta e infraestrutura de leitura no SQL Server `PaddockSpace` (tabelas e 13 views analíticas) cobrindo Temporada Atual, Histórico (1950–2025) e All-Time.
- `f1-data-sync`: Serviço background de consulta às APIs Jolpica F1 e OpenF1 (e acervo histórico `mdb_extras.json`) restrito exclusivamente à leitura externa e alimentação das tabelas da base `PaddockSpace`.
- `bff-web-and-mobile`: Camadas Backend-for-Frontend separadas para Web (`Bff.Web`) e Mobile (`Bff.Mobile`), adaptando contratos, agregações e granularidade de dados para cada canal.
- `web-portal-f1`: Aplicação React com Seletor Global de Categoria/Temporada no topo e módulos de Dashboard de Estatísticas, Pilotos, Equipes, Classificações, Provas, Circuitos, Linha do Tempo e Hall da Fama.

### Modified Capabilities
<!-- Nenhuma capability existente modificada (projeto greenfield). -->

## Impact

- **Banco de Dados**: Consumo de leitura das 26 tabelas e 13 views do banco `PaddockSpace` no SQL Server local, e escrita estritamente controlada pelo módulo de sincronização nas tabelas de dados existentes (`Pilotos`, `Equipes`, `Circuitos`, `GrandesPremios`, `Sessoes`, `Resultados*`, `Classifica*`, `Voltas`, `PitStops`, `Stints`, `Clima`, `ControleProva`, `Hist*`, `CampeoesBiografia`, `FotosHistoricas`, `ExecucoesAgente`).
- **Backend (.NET 10)**: Nova solution com projetos `Domain`, `Application`, `Infrastructure`, `Core.Api` (ou módulos internos), `Bff.Web`, `Bff.Mobile` e `SyncWorker`.
- **Frontend (React + TypeScript)**: Novo projeto SPA consumindo exclusivamente `PaddockSpace.Bff.Web`.
- **Integrações Externas**: Jolpica F1 API (`api.jolpi.ca`) e OpenF1 API (`api.openf1.org`) com controle de rate-limit.

