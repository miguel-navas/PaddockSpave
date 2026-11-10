namespace PaddockSpace.Domain.ValueObjects;

public enum SeasonScopeKind
{
    Current,
    Historical,
    AllTime
}

public sealed record SeasonScope
{
    public const int FirstFormula1Season = 1950;
    public const int CurrentFormula1Season = 2026;

    public SeasonScopeKind Kind { get; }
    public int? Year { get; }

    public bool IsCurrent => Kind == SeasonScopeKind.Current;
    public bool IsHistorical => Kind == SeasonScopeKind.Historical;
    public bool IsAllTime => Kind == SeasonScopeKind.AllTime;

    public bool HasRichTelemetry => Kind == SeasonScopeKind.Current;
    public bool HasQualifyingSplits => Kind == SeasonScopeKind.Current || (Kind == SeasonScopeKind.Historical && Year >= 1994);
    public bool HasSprintRaces => Kind == SeasonScopeKind.Current || (Kind == SeasonScopeKind.Historical && Year >= 2021);
    public bool HasConstructorChampionship => Kind != SeasonScopeKind.Historical || Year >= 1958;

    private SeasonScope(SeasonScopeKind kind, int? year)
    {
        Kind = kind;
        Year = year;
    }

    public static SeasonScope Current() => new(SeasonScopeKind.Current, CurrentFormula1Season);
    public static SeasonScope AllTime() => new(SeasonScopeKind.AllTime, null);

    public static SeasonScope ForYear(int year)
    {
        if (year < FirstFormula1Season || year > CurrentFormula1Season)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                year,
                $"Temporada deve estar entre {FirstFormula1Season} e {CurrentFormula1Season}.");
        }

        return year == CurrentFormula1Season
            ? Current()
            : new SeasonScope(SeasonScopeKind.Historical, year);
    }

    public static SeasonScope Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Current();
        }

        var normalized = raw.Trim().ToLowerInvariant();
        if (normalized is "current" or "atual")
        {
            return Current();
        }

        if (normalized is "all" or "all-time" or "alltime" or "todas" or "*")
        {
            return AllTime();
        }

        if (int.TryParse(normalized, out var year))
        {
            return ForYear(year);
        }

        throw new ArgumentException($"Escopo de temporada inválido: '{raw}'. Use '2026', um ano entre 1950 e 2025, ou 'all'.", nameof(raw));
    }

    public string ToKey() => Kind switch
    {
        SeasonScopeKind.AllTime => "all",
        _ => Year!.Value.ToString()
    };

    public override string ToString() => ToKey();
}

