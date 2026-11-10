namespace PaddockSpace.Domain.Entities;

public sealed class Driver
{
    public required string DriverId { get; init; }
    public string? Code { get; init; }
    public int? PermanentNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? PhotoUrl { get; init; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Career summary metrics
    public int Championships { get; init; }
    public int Races { get; init; }
    public int Wins { get; init; }
    public int Podiums { get; init; }
    public int Poles { get; init; }
    public int FastestLaps { get; init; }
    public decimal CareerPoints { get; init; }
    public int? FirstSeason { get; init; }
    public int? LastSeason { get; init; }
    public decimal WinPercentage { get; init; }
}

public sealed class ConstructorTeam
{
    public required string TeamId { get; init; }
    public required string Name { get; init; }
    public string? Nationality { get; init; }
    public string? ColorHex { get; init; }

    public int ConstructorTitles { get; init; }
    public int Races { get; init; }
    public int Wins { get; init; }
    public int Podiums { get; init; }
    public int Poles { get; init; }
    public decimal TotalPoints { get; init; }
    public int? FirstSeason { get; init; }
    public int? LastSeason { get; init; }
}

public sealed class Circuit
{
    public required string CircuitId { get; init; }
    public required string Name { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int TotalRacesHosted { get; init; }
    public int? FirstSeasonHosted { get; init; }
    public int? LastSeasonHosted { get; init; }
    public string? LastWinner { get; init; }
}

public sealed class GrandPrix
{
    public int Season { get; init; }
    public int Round { get; init; }
    public required string Name { get; init; }
    public string? CircuitId { get; init; }
    public string? CircuitName { get; init; }
    public string? Country { get; init; }
    public DateTime? RaceDateUtc { get; init; }
    public bool HasSprint { get; init; }
    public string? Status { get; init; }
    public string? WinnerName { get; init; }
    public string? WinnerTeam { get; init; }
    public string? PoleName { get; init; }
    public string? FastestLapName { get; init; }
    public int? Laps { get; init; }
}

public sealed class SeasonSummary
{
    public int Season { get; init; }
    public int? RacesCount { get; init; }
    public string? DriverChampion { get; init; }
    public string? DriverChampionId { get; init; }
    public string? DriverNationality { get; init; }
    public string? DriverTeam { get; init; }
    public decimal? DriverPoints { get; init; }
    public int? DriverWins { get; init; }
    public string? ConstructorChampion { get; init; }
    public decimal? ConstructorPoints { get; init; }
    public bool HasBiography { get; init; }
}

public sealed class ChampionBio
{
    public int ChampionId { get; init; }
    public string? DriverId { get; init; }
    public required string OriginalName { get; init; }
    public string? DriverFullName { get; init; }
    public string? Nationality { get; init; }
    public string? TitleYears { get; init; }
    public string? Biography { get; init; }
    public string? Source { get; init; }
    public string? PhotoUrl { get; init; }
    public int TitlesCount { get; init; }
    public int CareerWins { get; init; }
    public int CareerPodiums { get; init; }
    public int CareerPoles { get; init; }
    public int CareerRaces { get; init; }
    public IReadOnlyList<HistoricalPhoto> HistoricalPhotos { get; init; } = [];
}

public sealed class HistoricalPhoto
{
    public int ChampionId { get; init; }
    public int Order { get; init; }
    public string? Location { get; init; }
    public string? Caption { get; init; }
}
