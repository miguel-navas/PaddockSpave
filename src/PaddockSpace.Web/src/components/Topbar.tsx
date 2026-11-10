import React, { useEffect, useState } from 'react';
import { type NavTab, useGlobalFilter } from '../context/GlobalFilterContext';
import { PaddockLogo } from './PaddockLogo';

const TABS: { id: NavTab; label: string }[] = [
  { id: 'home', label: '⬅ Categorias (Início)' },
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'teams', label: 'Equipes' },
  { id: 'drivers', label: 'Pilotos' },
  { id: 'standings', label: 'Classificação' },
  { id: 'races', label: 'Provas (GPs & Telemetria)' },
  { id: 'circuits', label: 'Circuitos' },
  { id: 'timeline', label: 'Linha do Tempo (1950–Hoje)' },
  { id: 'hall-of-fame', label: 'Hall da Fama' }
];

export const Topbar: React.FC = () => {
  const {
    category,
    seasonScope,
    activeTab,
    contextData,
    setCategory,
    setSeasonScope,
    setActiveTab
  } = useGlobalFilter();

  const [menuOpen, setMenuOpen] = useState<boolean>(false);

  // Fecha o menu retrátil ao trocar de aba ou redimensionar para desktop
  useEffect(() => {
    setMenuOpen(false);
  }, [activeTab]);

  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth > 1024) {
        setMenuOpen(false);
      }
    };
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);

  const categories = contextData?.categories ?? [
    { code: 'f1', displayName: 'Fórmula 1', isAvailable: true },
    { code: 'f2', displayName: 'Fórmula 2', isAvailable: false },
    { code: 'fe', displayName: 'Fórmula E', isAvailable: false },
    { code: 'indy', displayName: 'Formula Indy', isAvailable: false },
    { code: 'nascar', displayName: 'NASCAR', isAvailable: false },
    { code: 'stockcar', displayName: 'Stock Car', isAvailable: false },
    { code: 'rally', displayName: 'Rally (WRC)', isAvailable: false },
    { code: 'motogp', displayName: 'Motovelocidade (MotoGP)', isAvailable: false }
  ];

  const seasons = contextData?.availableSeasons ?? [2026];
  const currentTabLabel = TABS.find((t) => t.id === activeTab)?.label ?? 'Menu';

  const handleSelectTab = (tabId: NavTab) => {
    setActiveTab(tabId);
    setMenuOpen(false);
  };

  return (
    <header className={`topbar ${menuOpen ? 'menu-is-open' : ''}`}>
      <div className="topbar-row">
        <div className="brand">
          <PaddockLogo size="sm" onClick={() => handleSelectTab('home')} />
        </div>

        {/* Botão de menu retrátil visível em celulares e tablets */}
        <div className="mobile-topbar-actions">
          <span className="mobile-active-indicator">
            {currentTabLabel.replace('⬅ ', '')} • {seasonScope === 'all' ? '1950–2026' : seasonScope}
          </span>
          <button
            type="button"
            className={`mobile-menu-toggle ${menuOpen ? 'open' : ''}`}
            aria-expanded={menuOpen}
            aria-label={menuOpen ? 'Fechar menu de navegação' : 'Abrir menu de navegação'}
            onClick={() => setMenuOpen((prev) => !prev)}
          >
            <span className="hamburger-icon" aria-hidden="true">
              <span />
              <span />
              <span />
            </span>
            <span className="mobile-menu-toggle-text">{menuOpen ? 'Fechar' : 'Menu'}</span>
          </button>
        </div>

        <div
          className={`topbar-collapsible ${menuOpen ? 'open' : ''}`}
          aria-label="Menu e Filtros Globais"
        >
          <nav className="nav-bar" aria-label="Navegação Principal">
            {TABS.map((tab) => (
              <button
                key={tab.id}
                type="button"
                className={`nav-tab ${activeTab === tab.id ? 'active' : ''}`}
                onClick={() => handleSelectTab(tab.id)}
              >
                {tab.label}
              </button>
            ))}
          </nav>

          <div className="global-selectors" aria-label="Seletor Global de Categoria e Temporada">
            <div className="selector-group">
              <label htmlFor="category-select">Categoria:</label>
              <select
                id="category-select"
                className="selector-input"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
              >
                {categories.map((c) => (
                  <option key={c.code} value={c.code} disabled={!c.isAvailable}>
                    {c.displayName}
                  </option>
                ))}
              </select>
            </div>

            <div className="selector-group">
              <label htmlFor="season-select">Temporada:</label>
              <select
                id="season-select"
                className="selector-input"
                value={seasonScope}
                onChange={(e) => {
                  setSeasonScope(e.target.value);
                  setMenuOpen(false);
                }}
              >
                <option value="2026">2026</option>
                <option value="all">Todas (1950–2026)</option>
                <optgroup label="Histórico (1950–2025)">
                  {seasons
                    .filter((y) => y !== 2026)
                    .map((y) => (
                      <option key={y} value={String(y)}>
                        {y}
                      </option>
                    ))}
                </optgroup>
              </select>
            </div>

            {/* <div className="selector-group quick-scope-group">
              <button
                type="button"
                className={`quick-scope-btn ${seasonScope === '2026' ? 'active' : ''}`}
                onClick={() => {
                  setSeasonScope('2026');
                  setMenuOpen(false);
                }}
              >
                2026
              </button>
              <button
                type="button"
                className={`quick-scope-btn ${seasonScope === '1991' ? 'active' : ''}`}
                onClick={() => {
                  setSeasonScope('1991');
                  setMenuOpen(false);
                }}
              >
                1991
              </button>
              <button
                type="button"
                className={`quick-scope-btn ${seasonScope === 'all' ? 'active' : ''}`}
                onClick={() => {
                  setSeasonScope('all');
                  setMenuOpen(false);
                }}
              >
                All-Time
              </button>
            </div> */}
          </div>
        </div>
      </div>

      {menuOpen && (
        <div
          className="mobile-menu-backdrop"
          onClick={() => setMenuOpen(false)}
          aria-hidden="true"
        />
      )}
    </header>
  );
};
