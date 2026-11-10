namespace PaddockSpace.Domain.ValueObjects;

public sealed record CategoryCode
{
    public const string Formula1Code = "f1";
    public const string Formula2Code = "f2";
    public const string IndyCode = "indy";
    public const string NascarCode = "nascar";
    public const string StockCarCode = "stockcar";
    public const string RallyCode = "rally";
    public const string MotoVelocidadeCode = "motogp";

    private static readonly IReadOnlyDictionary<string, (string Name, bool IsAvailable)> SupportedCategories =
        new Dictionary<string, (string Name, bool IsAvailable)>(StringComparer.OrdinalIgnoreCase)
        {
            [Formula1Code] = ("Fórmula 1", true),
            [Formula2Code] = ("Fórmula 2", false),
            [IndyCode] = ("IndyCar", false),
            [NascarCode] = ("NASCAR", false),
            [StockCarCode] = ("Stock Car", false),
            [RallyCode] = ("Rally (WRC)", false),
            [MotoVelocidadeCode] = ("Motovelocidade (MotoGP)", false)
        };

    public string Code { get; }
    public string DisplayName { get; }
    public bool IsAvailable { get; }
    public bool IsFormula1 => string.Equals(Code, Formula1Code, StringComparison.OrdinalIgnoreCase);

    private CategoryCode(string code, string displayName, bool isAvailable)
    {
        Code = code.ToLowerInvariant();
        DisplayName = displayName;
        IsAvailable = isAvailable;
    }

    public static CategoryCode Formula1 => Parse(Formula1Code);

    public static CategoryCode Parse(string? raw)
    {
        var normalized = string.IsNullOrWhiteSpace(raw) ? Formula1Code : raw.Trim().ToLowerInvariant();
        if (!SupportedCategories.TryGetValue(normalized, out var info))
        {
            throw new ArgumentException($"Categoria '{raw}' não é suportada pelo PaddockSpace.", nameof(raw));
        }

        return new CategoryCode(normalized, info.Name, info.IsAvailable);
    }

    public static IReadOnlyList<CategoryCode> ListAll() =>
        SupportedCategories.Select(kv => new CategoryCode(kv.Key, kv.Value.Name, kv.Value.IsAvailable)).ToList();

    public override string ToString() => Code;
}

