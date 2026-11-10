# Spec Delta

## Purpose

Entrega a aplicação web em React do PaddockSpace com um Seletor Global de Categoria e Temporada no topo e módulos interativos de Dashboard de Estatísticas, Pilotos, Equipes, Classificações, Provas, Circuitos, Linha do Tempo e Hall da Fama.

## ADDED Requirements

### Requirement: Seletor Global de Categoria e Temporada no Topo
O portal React SHALL exibir na barra superior um seletor global de **Categoria** (iniciando com Fórmula 1 e preparado para Fórmula 2, Indy, NASCAR, Stock Car, Rally e Motovelocidade) e um seletor global de **Temporada** (`2026`, anos individuais de `1950` a `2025`, e `Todas as Temporadas`), adaptando automaticamente os painéis da página ativa quando o usuário altera a seleção.

#### Scenario: Troca de temporada no Seletor Global
- **WHEN** o usuário altera a temporada no seletor global do topo (por exemplo, de `2026` para `1991` ou para `Todas as Temporadas`)
- **THEN** todos os componentes e gráficos da visão ativa recarregam seus dados via `Bff.Web` refletindo o ano selecionado e exibem apenas os painéis aplicáveis àquele escopo temporal

### Requirement: Dashboard de Estatísticas Adaptativo
O portal SHALL apresentar um Dashboard de Estatísticas com indicadores, gráficos comparativos e rankings que se adaptam ao escopo selecionado no topo.

#### Scenario: Visualização do Dashboard na temporada 2026
- **WHEN** o usuário acessa o Dashboard com a temporada `2026` selecionada
- **THEN** o Dashboard exibe KPIs da temporada atual, classificação de pilotos e equipes, calendário/próxima sessão (horário de Brasília), vencedores/poles/voltas rápidas por etapa, pit stops mais rápidos e análise de compostos de pneus/clima

#### Scenario: Visualização do Dashboard em temporada histórica ou All-Time
- **WHEN** o usuário acessa o Dashboard com uma temporada histórica (`1950`–`2025`) ou `Todas as Temporadas`
- **THEN** o Dashboard exibe os recordes consolidados (vitórias, pódios, poles, aproveitamento, títulos), dominância de equipes e comparativos de performance do período

### Requirement: Módulos de Pilotos, Equipes, Classificações, Provas, Circuitos, Linha do Tempo e Hall da Fama
O portal SHALL disponibilizar navegação dedicada para todas as áreas funcionais definidas na visão do PaddockSpace.

#### Scenario: Navegação por Pilotos, Equipes e Classificações
- **WHEN** o usuário acessa as seções de Pilotos, Equipes, Classificação de Pilotos ou Classificação de Equipes
- **THEN** o portal exibe tabelas ordenáveis/filtráveis e páginas de perfil com estatísticas detalhadas respeitando a temporada ou escopo global ativo

#### Scenario: Navegação por Provas e Circuitos
- **WHEN** o usuário acessa Provas (GPs) ou Circuitos
- **THEN** o portal apresenta os resultados de cada etapa (incluindo classificatório, sprint e telemetria quando disponível para o ano) e o catálogo geográfico/histórico dos circuitos

#### Scenario: Navegação pela Linha do Tempo e Hall da Fama
- **WHEN** o usuário acessa a Linha do Tempo ou o Hall da Fama
- **THEN** o portal renderiza a cronologia interativa de campeões de pilotos e construtores desde 1950 e a galeria de lendas com biografias e fotos históricas

