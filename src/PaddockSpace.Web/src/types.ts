export interface ScopeCapabilities {
  hasRichTelemetry: boolean;
  hasQualifyingSplits: boolean;
  hasSprintRaces: boolean;
  hasConstructorChampionship: boolean;
}

export interface CategoryOption {
  code: string;
  displayName: string;
  isAvailable: boolean;
}

export interface SyncExecutionInfo {
  executionId: number;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
  mode?: string | null;
  loadedSessions?: number | null;
  success?: boolean | null;
  message?: string | null;
}

export interface GlobalContext {
  categories: CategoryOption[];
  currentSeason: number;
  availableSeasons: number[];
  lastSync?: SyncExecutionInfo | null;
}

export interface DashboardKpis {
  totalRaces: number;
  completedRaces: number;
  totalDrivers: number;
  totalTeams: number;
  totalCircuits: number;
  leaderOrChampionDriver?: string | null;
  leaderOrChampionDriverPoints?: number | null;
  leaderOrChampionTeam?: string | null;
  leaderOrChampionTeamPoints?: number | null;
  nextOrLastGrandPrix?: string | null;
  nextOrLastGrandPrixDateBrasilia?: string | null;
}

export interface DriverStanding {
  season: number;
  position: number;
  driverId: string;
  driverName: string;
  code?: string | null;
  nationality?: string | null;
  teamId?: string | null;
  teamName?: string | null;
  teamColor?: string | null;
  points: number;
  wins: number;
  afterRound?: number | null;
  photoUrl?: string | null;
  poles?: number | null;
}

export interface TeamStanding {
  season: number;
  position: number;
  teamId: string;
  teamName: string;
  nationality?: string | null;
  colorHex?: string | null;
  points: number;
  wins: number;
  afterRound?: number | null;
}

export interface DriverSeasonStats {
  driverId: string;
  driverName: string;
  code?: string | null;
  championshipPosition?: number | null;
  points?: number | null;
  races?: number | null;
  wins?: number | null;
  podiums?: number | null;
  pointsFinishes?: number | null;
  retirements?: number | null;
  poles?: number | null;
  fastestLaps?: number | null;
  sprintWins?: number | null;
  sprintPoints?: number | null;
  avgQualifying?: number | null;
  avgGrid?: number | null;
  avgFinish?: number | null;
  positionsGained?: number | null;
  lapsCompleted?: number | null;
}

export interface PitStopHighlight {
  grandPrix: string;
  driverName: string;
  teamName?: string | null;
  lap?: number | null;
  durationSeconds?: number | null;
}

export interface TyreStrategy {
  round?: number | null;
  grandPrix?: string | null;
  sessionName: string;
  driverCode?: string | null;
  teamName?: string | null;
  stintNumber: number;
  compound?: string | null;
  startLap?: number | null;
  endLap?: number | null;
  lapsInStint?: number | null;
  tyreAgeAtStart?: number | null;
}

export interface SessionWeatherSummary {
  round?: number | null;
  grandPrix?: string | null;
  sessionName: string;
  avgAirTemp?: number | null;
  avgTrackTemp?: number | null;
  maxTrackTemp?: number | null;
  avgHumidity?: number | null;
  hadRain: boolean;
  avgWindSpeed?: number | null;
}

export interface CalendarSession {
  round?: number | null;
  grandPrix?: string | null;
  circuit?: string | null;
  country?: string | null;
  sessionName: string;
  startBrasilia?: string | null;
  cancelled: boolean;
  dataLoaded: boolean;
  hasSprint: boolean;
  sessionKey: number;
}

export interface Driver {
  driverId: string;
  code?: string | null;
  permanentNumber?: number | null;
  firstName?: string | null;
  lastName?: string | null;
  fullName: string;
  dateOfBirth?: string | null;
  nationality?: string | null;
  photoUrl?: string | null;
  championships: number;
  races: number;
  wins: number;
  podiums: number;
  poles: number;
  fastestLaps: number;
  careerPoints: number;
  firstSeason?: number | null;
  lastSeason?: number | null;
  winPercentage: number;
}

export interface ConstructorTeam {
  teamId: string;
  name: string;
  nationality?: string | null;
  colorHex?: string | null;
  constructorTitles: number;
  races: number;
  wins: number;
  podiums: number;
  poles: number;
  totalPoints?: number;
  firstSeason?: number | null;
  lastSeason?: number | null;
}

export interface GrandPrix {
  season: number;
  round: number;
  name: string;
  circuitId?: string | null;
  circuitName?: string | null;
  country?: string | null;
  raceDateUtc?: string | null;
  hasSprint: boolean;
  status?: string | null;
  winnerName?: string | null;
  winnerTeam?: string | null;
  poleName?: string | null;
  fastestLapName?: string | null;
  laps?: number | null;
}

export interface SeasonSummary {
  season: number;
  racesCount?: number | null;
  driverChampion?: string | null;
  driverChampionId?: string | null;
  driverNationality?: string | null;
  driverTeam?: string | null;
  driverPoints?: number | null;
  driverWins?: number | null;
  constructorChampion?: string | null;
  constructorPoints?: number | null;
  hasBiography: boolean;
}

export interface HistoricalPhoto {
  championId: number;
  order: number;
  location?: string | null;
  caption?: string | null;
}

export interface ChampionBio {
  championId: number;
  driverId?: string | null;
  originalName: string;
  driverFullName?: string | null;
  nationality?: string | null;
  titleYears?: string | null;
  biography?: string | null;
  source?: string | null;
  photoUrl?: string | null;
  titlesCount: number;
  careerWins: number;
  careerPodiums: number;
  careerPoles: number;
  careerRaces: number;
  historicalPhotos: HistoricalPhoto[];
}

export interface DashboardData {
  category: string;
  scopeKey: string;
  scopeKind: 'Current' | 'Historical' | 'AllTime';
  year?: number | null;
  capabilities: ScopeCapabilities;
  kpis: DashboardKpis;
  driverStandings: DriverStanding[];
  teamStandings: TeamStanding[];
  races: GrandPrix[];
  driverSeasonStats: DriverSeasonStats[];
  fastestPitStops: PitStopHighlight[];
  tyreStrategies: TyreStrategy[];
  weatherSummaries: SessionWeatherSummary[];
  upcomingOrRecentSessions: CalendarSession[];
  careerTopDrivers: Driver[];
  careerTopTeams: ConstructorTeam[];
  recentChampions: SeasonSummary[];
}

export interface RaceResultItem {
  season: number;
  round: number;
  driverId: string;
  driverName: string;
  driverCode?: string | null;
  teamId?: string | null;
  teamName?: string | null;
  teamColor?: string | null;
  carNumber?: number | null;
  gridPosition?: number | null;
  position?: number | null;
  positionText?: string | null;
  points: number;
  laps?: number | null;
  status?: string | null;
  timeText?: string | null;
  fastestLapRank?: number | null;
  fastestLapNumber?: number | null;
  fastestLapMs?: number | null;
}

export interface QualifyingResultItem {
  season: number;
  round: number;
  driverId: string;
  driverName: string;
  driverCode?: string | null;
  teamId?: string | null;
  teamName?: string | null;
  carNumber?: number | null;
  position?: number | null;
  q1Ms?: number | null;
  q2Ms?: number | null;
  q3Ms?: number | null;
}

export interface RaceControlEvent {
  id: number;
  timestampUtc?: string | null;
  lap?: number | null;
  category?: string | null;
  flag?: string | null;
  carNumber?: number | null;
  message?: string | null;
}

export interface LapSummary {
  driverId: string;
  driverName: string;
  lapsCount: number;
  bestLapMs?: number | null;
  avgLapMs?: number | null;
}

export interface RaceTelemetry {
  season: number;
  round: number;
  pitStops: PitStopHighlight[];
  tyreStrategies: TyreStrategy[];
  weather: SessionWeatherSummary[];
  raceControlEvents: RaceControlEvent[];
  lapSummaries: LapSummary[];
}

export interface GrandPrixDetail {
  race: GrandPrix;
  capabilities: ScopeCapabilities;
  raceResults: RaceResultItem[];
  sprintResults: RaceResultItem[];
  qualifyingResults: QualifyingResultItem[];
  telemetry?: RaceTelemetry | null;
}

export interface DriverSeasonHistoryItem {
  season: number;
  teamName?: string | null;
  championshipPosition?: number | null;
  points: number;
  wins: number;
  podiums: number;
  poles: number;
  races: number;
}

export interface DriverProfile {
  driver: Driver;
  currentSeasonStats?: DriverSeasonStats | null;
  seasonHistory: DriverSeasonHistoryItem[];
  biography?: ChampionBio | null;
}

export interface TeamSeasonHistoryItem {
  season: number;
  championshipPosition?: number | null;
  points: number;
  wins: number;
  podiums: number;
  poles: number;
  races: number;
}

export interface TeamProfile {
  team: ConstructorTeam;
  seasonHistory: TeamSeasonHistoryItem[];
  drivers: Driver[];
}

export interface Circuit {
  circuitId: string;
  name: string;
  city?: string | null;
  country?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  totalRacesHosted: number;
  firstSeasonHosted?: number | null;
  lastSeasonHosted?: number | null;
  lastWinner?: string | null;
}

export interface CircuitRaceHistoryItem {
  season: number;
  round: number;
  grandPrixName: string;
  raceDateUtc?: string | null;
  winnerName?: string | null;
  winnerTeam?: string | null;
  poleName?: string | null;
  fastestLapName?: string | null;
  fastestLapTeam?: string | null;
  fastestLapNumber?: number | null;
  fastestLapMs?: number | null;
}

export interface CircuitDetail {
  circuit: Circuit;
  hostedRaces: CircuitRaceHistoryItem[];
}
