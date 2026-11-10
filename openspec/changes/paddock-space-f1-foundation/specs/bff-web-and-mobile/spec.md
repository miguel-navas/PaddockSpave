# Spec Delta

## Purpose

Provê duas camadas Backend-for-Frontend dedicadas — uma para o site Web (`Bff.Web`) e outra para clientes Mobile (`Bff.Mobile`) — adaptando agregações, formato de resposta, paginação e peso de payload para as necessidades específicas de cada plataforma.

## ADDED Requirements

### Requirement: BFF dedicado para o Portal Web (Bff.Web)
A camada `Bff.Web` SHALL expor endpoints orientados às visões ricas do portal React e do Dashboard de Estatísticas, retornando dados agregados em uma única chamada por visão para minimizar round-trips no navegador.

#### Scenario: Carregamento do Dashboard Web por temporada
- **WHEN** o cliente Web requisita `GET /api/web/v1/{categoria}/dashboard?season={temporada}`
- **THEN** o `Bff.Web` retorna em um único payload os KPIs da temporada, classificação top de pilotos e equipes, evolução de pontuação, últimos/próximos GPs e destaques estatísticos (poles, voltas rápidas, pit stops e estratégias quando disponíveis)

#### Scenario: Carregamento de detalhe de Grande Prêmio no Web
- **WHEN** o cliente Web requisita os detalhes de uma rodada da temporada atual
- **THEN** o `Bff.Web` retorna o resultado completo, grid classificatório, série de voltas, pit stops, stints de pneus por piloto, resumo climático e linha do tempo do controle de prova

### Requirement: BFF dedicado para Clientes Mobile (Bff.Mobile)
A camada `Bff.Mobile` SHALL expor endpoints otimizados para dispositivos móveis, com payloads compactos, paginação enxuta e separação sob demanda de séries pesadas (como telemetria volta a volta).

#### Scenario: Carregamento da Home/Dashboard Mobile
- **WHEN** o cliente Mobile requisita `GET /api/mobile/v1/{categoria}/home?season={temporada}`
- **THEN** o `Bff.Mobile` retorna um payload enxuto contendo apenas os cards de resumo (líderes do campeonato, último vencedor, próxima sessão com horário local e top 5 da classificação), omitindo séries pesadas de voltas ou clima minuto a minuto

#### Scenario: Consulta paginada e sob demanda no Mobile
- **WHEN** o cliente Mobile navega pelos resultados de uma prova ou lista de pilotos históricos
- **THEN** o `Bff.Mobile` entrega listas paginadas com campos essenciais para renderização em lista mobile e carrega detalhes de telemetria/stints apenas quando a aba específica é solicitada

