# Spec Delta

## Purpose

Fornece os casos de uso e consultas de domínio da Fórmula 1 sobre a base de dados PaddockSpace (1950 até a atualidade), servindo dados estruturados de pilotos, equipes, classificações, provas, circuitos, linha do tempo, hall da fama e estatísticas unificadas.

## ADDED Requirements

### Requirement: Consulta parametrizada por Categoria e Escopo de Temporada
O sistema SHALL permitir consultar estatísticas, classificações, provas, pilotos e equipes filtrando por categoria (inicialmente `f1`) e por escopo de temporada (`2026` para temporada atual, `1950`–`2025` para uma temporada histórica específica, ou `all` para acumulado histórico 1950–atualidade).

#### Scenario: Consulta da temporada atual (2026)
- **WHEN** um consumidor solicita dados da temporada `2026`
- **THEN** o sistema retorna o calendário com horários das sessões, classificação atual de pilotos e equipes, resultados de corrida e sprint, classificatório e dados detalhados de telemetria (voltas, pit stops, stints de pneus, clima e controle de prova)

#### Scenario: Consulta de temporada histórica (1950 a 2025)
- **WHEN** um consumidor solicita dados de uma temporada histórica entre `1950` e `2025`
- **THEN** o sistema retorna os Grandes Prêmios daquela temporada, resultados de corrida, classificatório (quando temporada >= 1994), resultados de sprint (quando temporada >= 2021) e classificações finais de pilotos e construtores (quando temporada >= 1958)

#### Scenario: Consulta de todas as temporadas (All-Time)
- **WHEN** um consumidor solicita o escopo `all` (1950 até a atualidade)
- **THEN** o sistema retorna recordes consolidados de carreira de pilotos e equipes, lista de campeões ano a ano, estatísticas agregadas de circuitos e acervo do Hall da Fama

### Requirement: Exposição de Pilotos, Equipes e Circuitos
O sistema SHALL disponibilizar listagens e perfis detalhados de pilotos, equipes (construtores) e circuitos com seus respectivos históricos de desempenho e estatísticas de carreira.

#### Scenario: Perfil completo de piloto
- **WHEN** um consumidor consulta o detalhe de um piloto pelo `PilotoId`
- **THEN** o sistema retorna dados cadastrais, números de carreira (títulos, corridas, vitórias, pódios, poles, voltas mais rápidas, pontos, primeira e última temporada), evolução por temporada e biografia/fotos históricas caso seja campeão

#### Scenario: Perfil completo de equipe
- **WHEN** um consumidor consulta o detalhe de uma equipe pelo `EquipeId`
- **THEN** o sistema retorna dados cadastrais (incluindo cor e nacionalidade), títulos de construtores, corridas, vitórias, pódios, poles e histórico de temporadas disputadas

#### Scenario: Catálogo e detalhe de circuitos
- **WHEN** um consumidor consulta circuitos
- **THEN** o sistema retorna localização (cidade, país, coordenadas geográficas) e o histórico de Grandes Prêmios e vencedores realizados naquele circuito

### Requirement: Linha do Tempo e Hall da Fama
O sistema SHALL fornecer dados estruturados para a Linha do Tempo da Fórmula 1 (1950–atualidade) e para o Hall da Fama dos Campeões Mundiais.

#### Scenario: Consulta da Linha do Tempo
- **WHEN** um consumidor solicita a linha do tempo histórica
- **THEN** o sistema retorna a sequência cronológica de temporadas desde 1950 com campeão de pilotos, equipe do campeão, campeã de construtores, total de corridas, pontos e vitórias

#### Scenario: Consulta do Hall da Fama
- **WHEN** um consumidor solicita o Hall da Fama
- **THEN** o sistema retorna os pilotos campeões mundiais com seus anos de título, estatísticas de carreira, biografia associada e galeria de fotos/legendas históricas

