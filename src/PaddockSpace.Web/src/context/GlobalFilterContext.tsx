import React, { createContext, useContext, useEffect, useState } from 'react';
import { paddockApi } from '../api';
import type { GlobalContext } from '../types';

export type NavTab =
  | 'home'
  | 'dashboard'
  | 'drivers'
  | 'teams'
  | 'standings'
  | 'races'
  | 'circuits'
  | 'timeline'
  | 'hall-of-fame';

interface GlobalFilterState {
  category: string;
  seasonScope: string;
  activeTab: NavTab;
  contextData: GlobalContext | null;
  loadingContext: boolean;
  setCategory: (category: string) => void;
  setSeasonScope: (seasonScope: string) => void;
  setActiveTab: (tab: NavTab) => void;
  scopeLabel: string;
}

const GlobalFilterContext = createContext<GlobalFilterState | undefined>(undefined);

const FALLBACK_SEASONS = [
  2026,
  ...Array.from({ length: 2025 - 1950 + 1 }, (_, idx) => 2025 - idx)
];

export const GlobalFilterProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [category, setCategory] = useState<string>('f1');
  const [seasonScope, setSeasonScope] = useState<string>('2026');
  const [activeTab, setActiveTab] = useState<NavTab>('home');
  const [contextData, setContextData] = useState<GlobalContext | null>(null);
  const [loadingContext, setLoadingContext] = useState<boolean>(true);

  useEffect(() => {
    paddockApi
      .getContext()
      .then((ctx) => {
        setContextData(ctx);
      })
      .catch(() => {
        setContextData({
          categories: [
            { code: 'f1', displayName: 'Fórmula 1', isAvailable: true },
            { code: 'f2', displayName: 'Fórmula 2', isAvailable: false },
            { code: 'fe', displayName: 'Fórmula E', isAvailable: false },
            { code: 'indy', displayName: 'Formula Indy', isAvailable: false },
            { code: 'nascar', displayName: 'NASCAR', isAvailable: false },
            { code: 'stockcar', displayName: 'Stock Car', isAvailable: false },
            { code: 'rally', displayName: 'Rally (WRC)', isAvailable: false },
            { code: 'motogp', displayName: 'Motovelocidade (MotoGP)', isAvailable: false }
          ],
          currentSeason: 2026,
          availableSeasons: FALLBACK_SEASONS,
          lastSync: null
        });
      })
      .finally(() => setLoadingContext(false));
  }, []);

  const scopeLabel =
    seasonScope === 'all'
      ? 'Todas as Temporadas (1950–2026)'
      : seasonScope === '2026'
        ? 'Temporada Atual (2026 — Telemetria Ativa)'
        : `Temporada Histórica ${seasonScope}`;

  return (
    <GlobalFilterContext.Provider
      value={{
        category,
        seasonScope,
        activeTab,
        contextData,
        loadingContext,
        setCategory,
        setSeasonScope,
        setActiveTab,
        scopeLabel
      }}
    >
      {children}
    </GlobalFilterContext.Provider>
  );
};

export function useGlobalFilter(): GlobalFilterState {
  const ctx = useContext(GlobalFilterContext);
  if (!ctx) {
    throw new Error('useGlobalFilter deve ser usado dentro de GlobalFilterProvider');
  }
  return ctx;
}
