using PaddockSpace.Domain.Entities;

namespace PaddockSpace.Application.DTOs;

public sealed record ScopeCapabilitiesDto(
    bool HasRichTelemetry,
    bool HasQualifyingSplits,
    bool HasSprintRaces,
    bool HasConstructorChampionship);

public sealed record CategoryOptionDto(
    string Code,
    string DisplayName,
    bool IsAvailable);

public sealed record SyncExecutionInfoDto(
    int ExecutionId,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? Mode,
    int? LoadedSessions,
    bool? Success,
    string? Message);

public sealed record GlobalContextDto(
    IReadOnlyList<CategoryOptionDto> Categories,
    int CurrentSeason,
    IReadOnlyList<int> AvailableSeasons,
    SyncExecutionInfoDto? LastSync);

public sealed record DashboardKpisDto(
    int TotalRaces,
    int CompletedRaces,
    int TotalDrivers,
    int TotalTeams,
    int TotalCircuits,
    string? LeaderOrChampionDriver,
    decimal? LeaderOrChampionDriverPoints,
    string? LeaderOrChampionTeam,
    decimal? LeaderOrChampionTeamPoints,
    string? NextOrLastGrandPrix,
    DateTime? NextOrLastGrandPrixDateBrasilia);

public sealed record DriverStandingDto(
    int Season,
    int Position,
    string DriverId,
    string DriverName,
    string? Code,
    string? Nationality,
    string? TeamId,
    string? TeamName,
    string? TeamColor,
    decimal Points,
    int Wins,
    int? AfterRound,
    string? PhotoUrl,
    int Poles = 0)
{
    public DriverStandingDto() : this(0, 0, string.Empty, string.Empty, null, null, null, null, null, 0m, 0, null, null, 0) { }
}

public sealed record TeamStandingDto(
    int Season,
    int Position,
    string TeamId,
    string TeamName,
    string? Nationality,
    string? ColorHex,
    decimal Points,
    int Wins,
    int? AfterRound)
{
    public TeamStandingDto() : this(0, 0, string.Empty, string.Empty, null, null, 0m, 0, null) { }
}

public sealed record DriverSeasonStatsDto(
    string DriverId,
    string DriverName,
    string? Code,
    int? ChampionshipPosition,
    decimal? Points,
    int? Races,
    int? Wins,
    int? Podiums,
    int? PointsFinishes,
    int? Retirements,
    int? Poles,
    int? FastestLaps,
    int? SprintWins,
    decimal? SprintPoints,
    decimal? AvgQualifying,
    decimal? AvgGrid,
    decimal? AvgFinish,
    int? PositionsGained,
    int? LapsCompleted);

public sealed record PitStopHighlightDto(
    string GrandPrix,
    string DriverName,
    string? TeamName,
    int? Lap,
    decimal? DurationSeconds)
{
    public PitStopHighlightDto() : this(string.Empty, string.Empty, null, null, null) { }
}

public sealed record TyreStrategyDto(
    int? Round,
    string? GrandPrix,
    string SessionName,
    string? DriverCode,
    string? TeamName,
    int StintNumber,
    string? Compound,
    int? StartLap,
    int? EndLap,
    int? LapsInStint,
    int? TyreAgeAtStart);

public sealed record SessionWeatherSummaryDto(
    int? Round,
    string? GrandPrix,
    string SessionName,
    decimal? AvgAirTemp,
    decimal? AvgTrackTemp,
    decimal? MaxTrackTemp,
    decimal? AvgHumidity,
    bool HadRain,
    decimal? AvgWindSpeed);

public sealed record CalendarSessionDto(
    int? Round,
    string? GrandPrix,
    string? Circuit,
    string? Country,
    string SessionName,
    DateTime? StartBrasilia,
    bool Cancelled,
    bool DataLoaded,
    bool HasSprint,
    int SessionKey);

public sealed record DashboardDataDto(
    string Category,
    string ScopeKey,
    string ScopeKind,
    int? Year,
    ScopeCapabilitiesDto Capabilities,
    DashboardKpisDto Kpis,
    IReadOnlyList<DriverStandingDto> DriverStandings,
    IReadOnlyList<TeamStandingDto> TeamStandings,
    IReadOnlyList<GrandPrix> Races,
    IReadOnlyList<DriverSeasonStatsDto> DriverSeasonStats,
    IReadOnlyList<PitStopHighlightDto> FastestPitStops,
    IReadOnlyList<TyreStrategyDto> TyreStrategies,
    IReadOnlyList<SessionWeatherSummaryDto> WeatherSummaries,
    IReadOnlyList<CalendarSessionDto> UpcomingOrRecentSessions,
    IReadOnlyList<Driver> CareerTopDrivers,
    IReadOnlyList<ConstructorTeam> CareerTopTeams,
    IReadOnlyList<SeasonSummary> RecentChampions);

public sealed record RaceResultItemDto(
    int Season,
    int Round,
    string DriverId,
    string DriverName,
    string? DriverCode,
    string? TeamId,
    string? TeamName,
    string? TeamColor,
    int? CarNumber,
    int? GridPosition,
    int? Position,
    string? PositionText,
    decimal Points,
    int? Laps,
    string? Status,
    string? TimeText,
    int? FastestLapRank,
    int? FastestLapNumber,
    int? FastestLapMs)
{
    public RaceResultItemDto() : this(0, 0, string.Empty, string.Empty, null, null, null, null, null, null, null, null, 0m, null, null, null, null, null, null) { }
}

public sealed record QualifyingResultItemDto(
    int Season,
    int Round,
    string DriverId,
    string DriverName,
    string? DriverCode,
    string? TeamId,
    string? TeamName,
    int? CarNumber,
    int? Position,
    int? Q1Ms,
    int? Q2Ms,
    int? Q3Ms)
{
    public QualifyingResultItemDto() : this(0, 0, string.Empty, string.Empty, null, null, null, null, null, null, null, null) { }
}

public sealed record RaceControlEventDto(
    int Id,
    DateTime? TimestampUtc,
    int? Lap,
    string? Category,
    string? Flag,
    int? CarNumber,
    string? Message)
{
    public RaceControlEventDto() : this(0, null, null, null, null, null, null) { }
}

public sealed record LapSummaryDto(
    string DriverId,
    string DriverName,
    int LapsCount,
    int? BestLapMs,
    int? AvgLapMs)
{
    public LapSummaryDto() : this(string.Empty, string.Empty, 0, null, null) { }
}

public sealed record RaceTelemetryDto(
    int Season,
    int Round,
    IReadOnlyList<PitStopHighlightDto> PitStops,
    IReadOnlyList<TyreStrategyDto> TyreStrategies,
    IReadOnlyList<SessionWeatherSummaryDto> Weather,
    IReadOnlyList<RaceControlEventDto> RaceControlEvents,
    IReadOnlyList<LapSummaryDto> LapSummaries);

public sealed record GrandPrixDetailDto(
    GrandPrix Race,
    ScopeCapabilitiesDto Capabilities,
    IReadOnlyList<RaceResultItemDto> RaceResults,
    IReadOnlyList<RaceResultItemDto> SprintResults,
    IReadOnlyList<QualifyingResultItemDto> QualifyingResults,
    RaceTelemetryDto? Telemetry);

public sealed record DriverSeasonHistoryItemDto(
    int Season,
    string? TeamName,
    int? ChampionshipPosition,
    decimal Points,
    int Wins,
    int Podiums,
    int Poles,
    int Races)
{
    public DriverSeasonHistoryItemDto() : this(0, null, null, 0m, 0, 0, 0, 0) { }
}

public sealed record DriverProfileDto(
    Driver Driver,
    DriverSeasonStatsDto? CurrentSeasonStats,
    IReadOnlyList<DriverSeasonHistoryItemDto> SeasonHistory,
    ChampionBio? Biography);

public sealed record TeamSeasonHistoryItemDto(
    int Season,
    int? ChampionshipPosition,
    decimal Points,
    int Wins,
    int Podiums,
    int Poles,
    int Races)
{
    public TeamSeasonHistoryItemDto() : this(0, null, 0m, 0, 0, 0, 0) { }
}

public sealed record TeamProfileDto(
    ConstructorTeam Team,
    IReadOnlyList<TeamSeasonHistoryItemDto> SeasonHistory,
    IReadOnlyList<Driver> Drivers);

public sealed record CircuitRaceHistoryItemDto(
    int Season,
    int Round,
    string GrandPrixName,
    DateTime? RaceDateUtc,
    string? WinnerName,
    string? WinnerTeam,
    string? PoleName,
    string? FastestLapName,
    string? FastestLapTeam,
    int? FastestLapNumber,
    int? FastestLapMs);

public sealed record CircuitDetailDto(
    Circuit Circuit,
    IReadOnlyList<CircuitRaceHistoryItemDto> HostedRaces);
