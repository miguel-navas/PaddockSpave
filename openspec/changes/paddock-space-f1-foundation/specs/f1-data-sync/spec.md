# Spec Delta

## Purpose

Realiza a sincronização e manutenção da base de dados PaddockSpace no SQL Server em background, restrita estritamente a consultar as fontes externas de dados da Fórmula 1 e alimentar as tabelas de dados existentes sem qualquer operação estrutural ou fora desse escopo.

## ADDED Requirements

### Requirement: Escopo estrito de consulta externa e alimentação da base
O serviço de sincronização SHALL executar exclusivamente consultas HTTP de leitura nas APIs externas configuradas (Jolpica F1 e OpenF1) e leitura do acervo local (`mdb_extras.json`), gravando os dados apenas via `INSERT` ou `UPSERT` (atualização de registros de dados) nas tabelas existentes do banco `PaddockSpace`. O serviço MUST NOT executar comandos DDL (`DROP`, `ALTER`, `TRUNCATE` de estrutura) nem modificar dados fora do conjunto sincronizado.

#### Scenario: Sincronização respeitando limites de escrita
- **WHEN** o ciclo de sincronização é acionado (manualmente ou por agendamento)
- **THEN** o serviço consulta apenas os endpoints de dados da Jolpica F1 e OpenF1, atualiza/insere os registros correspondentes nas tabelas de dados e registra o log de execução na tabela `ExecucoesAgente`

### Requirement: Sincronização incremental da temporada atual
O serviço SHALL monitorar e atualizar o calendário e as sessões concluídas da temporada atual (2026) respeitando os intervalos mínimos de requisição das APIs gratuitas.

#### Scenario: Sessão concluída pendente de carga
- **WHEN** uma sessão de pontuação ou classificação (`Race`, `Sprint`, `Qualifying`, `Sprint Qualifying`, `Sprint Shootout`) terminou há mais de 1 hora e está com `DadosCarregados = 0`
- **THEN** o serviço coleta resultados, voltas, pit stops, classificação do campeonato, pilotos da sessão, stints de pneus, clima e controle de prova, grava na base em transação e marca `DadosCarregados = 1`

### Requirement: Complementação de temporadas históricas e acervo do Hall da Fama
O serviço SHALL permitir complementar temporadas históricas faltantes (`1950` a `2025`) na tabela `HistTemporadas` e correlatas, além de importar biografias e legendas históricas para `CampeoesBiografia` e `FotosHistoricas`.

#### Scenario: Continuação da carga histórica de onde parou
- **WHEN** o modo de sincronização histórica é executado e existem temporadas entre 1950 e 2025 ausentes em `HistTemporadas` (como o intervalo 2014–2025)
- **THEN** o serviço consulta apenas as temporadas faltantes respeitando o rate limit da Jolpica (~480 req/h), grava cada temporada em transação atômica e vincula os registros de `CampeoesBiografia` e `FotosHistoricas`

