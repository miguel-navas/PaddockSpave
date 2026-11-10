import type {
  ChampionBio,
  Circuit,
  CircuitDetail,
  ConstructorTeam,
  DashboardData,
  Driver,
  DriverProfile,
  DriverStanding,
  GlobalContext,
  GrandPrix,
  GrandPrixDetail,
  SeasonSummary,
  TeamProfile,
  TeamStanding
} from './types';

const BASE_URL = import.meta.env.VITE_BFF_WEB_URL ?? '/api/web/v1';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`);
  if (!response.ok) {
    const errText = await response.text();
    throw new Error(`Erro HTTP ${response.status}: ${errText || response.statusText}`);
  }
  return response.json() as Promise<T>;
}

export const paddockApi = {
  getContext: () => getJson<GlobalContext>('/context'),

  getDashboard: (category: string, season: string) =>
    getJson<DashboardData>(`/${category}/dashboard?season=${encodeURIComponent(season)}`),

  getDrivers: (category: string, season: string, search?: string) => {
    const q = new URLSearchParams({ season });
    if (search) q.set('search', search);
    return getJson<Driver[]>(`/${category}/drivers?${q.toString()}`);
  },

  getDriverProfile: (category: string, driverId: string, season: string) =>
    getJson<DriverProfile>(`/${category}/drivers/${encodeURIComponent(driverId)}?season=${encodeURIComponent(season)}`),

  getTeams: (category: string, season: string, search?: string) => {
    const q = new URLSearchParams({ season });
    if (search) q.set('search', search);
    return getJson<ConstructorTeam[]>(`/${category}/teams?${q.toString()}`);
  },

  getTeamProfile: (category: string, teamId: string, season: string) =>
    getJson<TeamProfile>(`/${category}/teams/${encodeURIComponent(teamId)}?season=${encodeURIComponent(season)}`),

  getDriverStandings: (category: string, season: string) =>
    getJson<DriverStanding[]>(`/${category}/standings/drivers?season=${encodeURIComponent(season)}`),

  getTeamStandings: (category: string, season: string) =>
    getJson<TeamStanding[]>(`/${category}/standings/teams?season=${encodeURIComponent(season)}`),

  getRaces: (category: string, season: string) =>
    getJson<GrandPrix[]>(`/${category}/races?season=${encodeURIComponent(season)}`),

  getRaceDetail: (category: string, season: number, round: number) =>
    getJson<GrandPrixDetail>(`/${category}/races/${season}/${round}`),

  getCircuits: (category: string, season: string) =>
    getJson<Circuit[]>(`/${category}/circuits?season=${encodeURIComponent(season)}`),

  getCircuitDetail: (category: string, circuitId: string) =>
    getJson<CircuitDetail>(`/${category}/circuits/${encodeURIComponent(circuitId)}`),

  getTimeline: (category: string) =>
    getJson<SeasonSummary[]>(`/${category}/timeline`),

  getHallOfFame: (category: string) =>
    getJson<ChampionBio[]>(`/${category}/hall-of-fame`)
};

export function formatLapMs(ms?: number | null): string {
  if (!ms || ms <= 0) return '-';
  const totalSec = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSec / 60);
  const seconds = totalSec % 60;
  const millis = ms % 1000;
  return `${minutes}:${String(seconds).padStart(2, '0')}.${String(millis).padStart(3, '0')}`;
}

const COUNTRY_FLAG_MAP: Record<string, string> = {
  australia: '/flags/australia.jpg',
  austria: '/flags/austria.jpg',
  azerbaijan: '/flags/azerbaijan.jpg',
  bahrain: '/flags/bahrain.jpg',
  belgium: '/flags/belgium.jpg',
  brazil: '/flags/brazil.jpg',
  canada: '/flags/canada.jpg',
  china: '/flags/china.jpg',
  hungary: '/flags/hungary.jpg',
  italy: '/flags/italy.jpg',
  japan: '/flags/japan.jpg',
  malaysia: '/flags/malaysia.jpg',
  mexico: '/flags/mexico.jpg',
  monaco: '/flags/monaco.jpg',
  netherlands: '/flags/netherlands.jpg',
  qatar: '/flags/qatar.jpg',
  singapore: '/flags/singapore.jpg',
  spain: '/flags/spain.jpg',
  uae: '/flags/uae.jpg',
  'united arab emirates': '/flags/uae.jpg',
  uk: '/flags/uk.jpg',
  'united kingdom': '/flags/uk.jpg',
  'great britain': '/flags/uk.jpg',
  usa: '/flags/usa.jpg',
  'united states': '/flags/usa.jpg',
  'saudi arabia': '/flags/saudi_arabia.jpg',
  france: '/flags/france.jpg',
  germany: '/flags/germany.jpg',
  portugal: '/flags/portugal.jpg',
  argentina: '/flags/argentina.jpg',
  'south africa': '/flags/south_africa.jpg',
  sweden: '/flags/sweden.jpg',
  switzerland: '/flags/switzerland.jpg',
  russia: '/flags/russia.jpg',
  turkey: '/flags/turkey.jpg',
  india: '/flags/india.jpg',
  korea: '/flags/korea.jpg',
  morocco: '/flags/morocco.jpg'
};

export function getCountryFlagUrl(country?: string | null, gpName?: string | null): string | null {
  if (gpName && gpName.toLowerCase().includes('bahrain')) {
    return '/flags/bahrain.jpg';
  }
  if (!country) return null;
  const key = country.trim().toLowerCase();
  return COUNTRY_FLAG_MAP[key] ?? null;
}

