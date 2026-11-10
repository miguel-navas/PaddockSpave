namespace PaddockSpace.Application.Abstractions;

public sealed record SyncRunSummaryDto(
    string Mode,
    int LoadedSessionsOrSeasons,
    int BiographiesUpserted,
    int HistoricalPhotosInserted,
    bool Success,
    string Message);

public interface IF1DataIngestionRepository
{
    Task<int> BeginExecutionLogAsync(string mode, CancellationToken ct = default);
    Task CompleteExecutionLogAsync(int executionId, int loadedCount, bool success, string message, CancellationToken ct = default);

    Task<IReadOnlySet<int>> GetLoadedHistoricalSeasonsAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, string>> GetChampionDriverBySeasonAsync(CancellationToken ct = default);

    Task UpsertCircuitosAsync(IEnumerable<CircuitoIngestionItem> items, CancellationToken ct = default);
    Task UpsertPilotosAsync(IEnumerable<PilotoIngestionItem> items, CancellationToken ct = default);
    Task UpsertEquipesAsync(IEnumerable<EquipeIngestionItem> items, CancellationToken ct = default);
    Task UpsertGrandesPremiosAsync(IEnumerable<GrandePremioIngestionItem> items, CancellationToken ct = default);
    Task UpsertClassificacaoPilotosAsync(IEnumerable<ClassificacaoPilotoIngestionItem> items, CancellationToken ct = default);
    Task UpsertClassificacaoEquipesAsync(IEnumerable<ClassificacaoEquipeIngestionItem> items, CancellationToken ct = default);

    Task UpsertChampionBiographiesAndPhotosAsync(
        IEnumerable<CampeaoBiografiaIngestionItem> biographies,
        IEnumerable<FotoHistoricaIngestionItem> photos,
        CancellationToken ct = default);
}

public sealed record CircuitoIngestionItem(
    string CircuitoId,
    string Nome,
    string? Cidade,
    string? Pais,
    decimal? Latitude,
    decimal? Longitude);

public sealed record PilotoIngestionItem(
    string PilotoId,
    string? Codigo,
    int? NumeroPermanente,
    string? Nome,
    string? Sobrenome,
    DateTime? DataNascimento,
    string? Nacionalidade,
    string? FotoUrl = null);

public sealed record EquipeIngestionItem(
    string EquipeId,
    string Nome,
    string? Nacionalidade,
    string? Cor = null);

public sealed record GrandePremioIngestionItem(
    int Rodada,
    string Nome,
    string? CircuitoId,
    DateTime? DataCorridaUTC,
    bool TemSprint,
    string? Status);

public sealed record ClassificacaoPilotoIngestionItem(
    string PilotoId,
    string? EquipeId,
    int? Posicao,
    decimal? Pontos,
    int? Vitorias,
    int? AposRodada);

public sealed record ClassificacaoEquipeIngestionItem(
    string EquipeId,
    int? Posicao,
    decimal? Pontos,
    int? Vitorias,
    int? AposRodada);

public sealed record CampeaoBiografiaIngestionItem(
    int CampeaoId,
    string? PilotoId,
    string NomeOriginal,
    string? Nacionalidade,
    string? AnosTitulo,
    string? Biografia,
    string? Fonte);

public sealed record FotoHistoricaIngestionItem(
    int CampeaoId,
    int Ordem,
    string? Local,
    string? Legenda);

