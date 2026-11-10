import React, { useEffect } from 'react';
import { Topbar } from './components/Topbar';
import { GlobalFilterProvider, useGlobalFilter } from './context/GlobalFilterContext';
import { CircuitsView } from './views/CircuitsView';
import { DashboardView } from './views/DashboardView';
import { DriversView } from './views/DriversView';
import { HallOfFameView } from './views/HallOfFameView';
import { HomeView } from './views/HomeView';
import { RacesView } from './views/RacesView';
import { StandingsView } from './views/StandingsView';
import { TeamsView } from './views/TeamsView';
import { TimelineView } from './views/TimelineView';

const ShellContent: React.FC = () => {
  const { activeTab, setActiveTab } = useGlobalFilter();

  if (activeTab === 'home') {
    return <HomeView onEnterCategory={() => setActiveTab('dashboard')} />;
  }

  return (
    <>
      <Topbar />
      <main className="main-container">
        {activeTab === 'dashboard' && <DashboardView />}
        {activeTab === 'standings' && <StandingsView />}
        {activeTab === 'drivers' && <DriversView />}
        {activeTab === 'teams' && <TeamsView />}
        {activeTab === 'races' && <RacesView />}
        {activeTab === 'circuits' && <CircuitsView />}
        {activeTab === 'timeline' && <TimelineView />}
        {activeTab === 'hall-of-fame' && <HallOfFameView />}
      </main>
      <footer className="app-footer">PADDOCK SPACE - direitos reservados desde 2023</footer>
    </>
  );
};

export const App: React.FC = () => {
  useEffect(() => {
    const syncModalScrollLock = () => {
      const modalOpen = document.querySelector('.modal-backdrop') !== null;
      document.body.classList.toggle('modal-open', modalOpen);
    };

    syncModalScrollLock();
    const observer = new MutationObserver(syncModalScrollLock);
    observer.observe(document.body, { childList: true, subtree: true });

    return () => {
      observer.disconnect();
      document.body.classList.remove('modal-open');
    };
  }, []);

  return (
    <GlobalFilterProvider>
      <div className="app-shell">
        <ShellContent />
      </div>
    </GlobalFilterProvider>
  );
};

export default App;
