import React, { useEffect, useState } from 'react';
import { PaddockLogo } from '../components/PaddockLogo';
import { useGlobalFilter } from '../context/GlobalFilterContext';

interface CategoryStripItem {
  code: string;
  shortLabel: string;
  subLabel?: string;
  fullName: string;
  accentColor: string;
  isAvailable: boolean;
  imageUrl: string;
  description: string;
}

const PADDOCK_CATEGORIES: CategoryStripItem[] = [
  {
    code: 'f1',
    shortLabel: 'F1',
    fullName: 'Fórmula 1 (1950–2026)',
    accentColor: '#e10600',
    isAvailable: true,
    imageUrl: '/categories/f1.jpg',
    description: '77 temporadas, 794 pilotos, 205 equipes, telemetria OpenF1 2026 e Hall da Fama.'
  },
  {
    code: 'f2',
    shortLabel: 'F2',
    fullName: 'FIA Formula 2 Championship',
    accentColor: '#0090d0',
    isAvailable: false,
    imageUrl: '/categories/f2.jpg',
    description: 'Principal categoria de acesso à Fórmula 1 com monopostos Dallara idênticos.'
  },
  {
    code: 'fe',
    shortLabel: 'FE',
    fullName: 'ABB FIA Formula E World Championship',
    accentColor: '#00e5ff',
    isAvailable: false,
    imageUrl: '/categories/fe.jpg',
    description: 'Campeonato mundial de monopostos 100% elétricos em circuitos de rua.'
  },
  {
    code: 'indy',
    shortLabel: 'INDY',
    fullName: 'NTT IndyCar Series',
    accentColor: '#f59e0b',
    isAvailable: false,
    imageUrl: '/categories/indy.jpg',
    description: 'Circuitos mistos, rua e superovais incluindo as 500 Milhas de Indianápolis.'
  },
  {
    code: 'nascar',
    shortLabel: 'NASCAR',
    fullName: 'NASCAR Cup Series',
    accentColor: '#ffcc00',
    isAvailable: false,
    imageUrl: '/categories/nascar.jpg',
    description: 'A principal categoria norte-americana de Stock Cars em superovais.'
  },
  {
    code: 'stock',
    shortLabel: 'STOCK CAR',
    fullName: 'Stock Car Pro Series',
    accentColor: '#22c55e',
    isAvailable: false,
    imageUrl: '/categories/stock.jpg',
    description: 'A maior categoria do automobilismo brasileiro com disputas intensas.'
  },
  {
    code: 'dtm',
    shortLabel: 'DTM',
    fullName: 'DTM Pro Series',
    accentColor: '#1368e7',
    isAvailable: false,
    imageUrl: '/categories/dtm.jpg',
    description: 'Tradicional campeonato alemão de turismo com carros GT3 de alta performance.'
  },
  {
    code: 'rally',
    shortLabel: 'RALLY',
    fullName: 'WRC — World Rally Championship',
    accentColor: '#f97316',
    isAvailable: false,
    imageUrl: '/categories/rally.jpg',
    description: 'Estágios especiais no asfalto, cascalho, neve e terra ao redor do mundo.'
  },
  {
    code: 'moto',
    shortLabel: 'MOTO',
    fullName: 'MotoGP World Championship',
    accentColor: '#a855f7',
    isAvailable: false,
    imageUrl: '/categories/moto.jpg',
    description: 'Categoria rainha do Mundial de Motovelocidade FIM.'
  }
];

function detectMobileOrTablet(): boolean {
  if (typeof window === 'undefined') return false;
  const ua = navigator.userAgent || '';
  const isTouchDevice =
    /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini|Mobile|Tablet/i.test(ua) ||
    (navigator.maxTouchPoints > 1 && window.innerWidth <= 1180);
  return window.innerWidth <= 1024 || isTouchDevice;
}

export const HomeView: React.FC<{
  onEnterCategory: (categoryCode: string) => void;
}> = ({ onEnterCategory }) => {
  const { setCategory } = useGlobalFilter();
  const [previewCategory, setPreviewCategory] = useState<CategoryStripItem | null>(null);
  const [isVerticalMode, setIsVerticalMode] = useState<boolean>(() => detectMobileOrTablet());

  useEffect(() => {
    const handleResize = () => {
      setIsVerticalMode(detectMobileOrTablet());
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const handleSelectStrip = (item: CategoryStripItem) => {
    if (item.isAvailable) {
      setCategory(item.code);
      onEnterCategory(item.code);
    } else {
      setPreviewCategory(item);
    }
  };

  return (
    <div className={`paddock-home-screen ${isVerticalMode ? 'is-vertical-layout' : ''}`}>
      <div className="paddock-home-header">
        <PaddockLogo size="lg" />
      </div>

      {/* Palco: horizontal no Desktop, vertical em Celulares e Tablets */}
      <div className="paddock-strips-stage">
        <div className="paddock-strips-row">
          {PADDOCK_CATEGORIES.map((cat) => (
            <button
              key={cat.code}
              type="button"
              className={`paddock-strip-col ${cat.isAvailable ? 'is-live' : 'is-soon'}`}
              style={{ '--strip-accent': cat.accentColor } as React.CSSProperties}
              onClick={() => handleSelectStrip(cat)}
            >
              {/* Faixa diagonal com foto real do veículo da categoria e efeito de brilho no hover/toque */}
              <div className="paddock-strip-card">
                <div className="paddock-strip-img-holder">
                  <img
                    src={cat.imageUrl}
                    alt={cat.fullName}
                    className="paddock-strip-img"
                    loading="eager"
                  />
                  <div className="paddock-strip-flare" />
                </div>

                {/* Overlay exibido no modo vertical (celular/tablet) para leitura imediata */}
                <div className="paddock-strip-mobile-overlay">
                  <div className="paddock-strip-mobile-texts">
                    <span className="paddock-strip-mobile-title">
                      {cat.shortLabel}
                      {cat.subLabel ? ` ${cat.subLabel}` : ''}
                    </span>
                    <span className="paddock-strip-mobile-sub">{cat.fullName}</span>
                  </div>
                  <span
                    className={`paddock-strip-mobile-badge ${cat.isAvailable ? 'live' : 'soon'}`}
                  >
                    {cat.isAvailable ? 'EXPLORAR →' : 'EM BREVE'}
                  </span>
                </div>
              </div>

              {/* Texto abaixo da faixa no modo Desktop horizontal */}
              <div className="paddock-strip-caption">
                <span className="paddock-strip-label">
                  {cat.shortLabel}
                  {cat.subLabel ? ` ${cat.subLabel}` : ''}
                </span>
              </div>
            </button>
          ))}
        </div>
      </div>

      {previewCategory && (
        <div className="modal-backdrop" onClick={() => setPreviewCategory(null)}>
          <div
            className="modal-card"
            style={{ maxWidth: '540px' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="panel-header">
              <div>
                <h2 className="panel-title">{previewCategory.fullName}</h2>
                <div className="brand-sub">Módulo planejado na plataforma Paddock Space</div>
              </div>
              <button
                type="button"
                className="close-btn"
                onClick={() => setPreviewCategory(null)}
              >
                Fechar ✕
              </button>
            </div>
            <p className="bio-text" style={{ marginBottom: '1.25rem' }}>
              {previewCategory.description}
            </p>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                className="quick-scope-btn active"
                onClick={() => {
                  setPreviewCategory(null);
                  setCategory('f1');
                  onEnterCategory('f1');
                }}
              >
                Explorar Fórmula 1 (1950–2026) →
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
