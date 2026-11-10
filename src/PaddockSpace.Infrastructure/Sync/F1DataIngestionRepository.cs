using System.Text.RegularExpressions;
using Dapper;
using PaddockSpace.Application.Abstractions;
using PaddockSpace.Infrastructure.Persistence;

namespace PaddockSpace.Infrastructure.Sync;

/// <summary>
/// Repositório estritamente restrito à alimentação (INSERT/UPSERT) de registros de dados
/// nas tabelas existentes do banco PaddockSpace e registro de auditoria em ExecucoesAgente.
/// Nenhum comando DDL (CREATE, ALTER, DROP, TRUNCATE) é permitido.
/// </summary>
public sealed partial class F1DataIngestionRepository : IF1DataIngestionRepository
{
    private static readonly HashSet<string> AllowedTargetTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "dbo.ExecucoesAgente",
        "dbo.Circuitos",
        "dbo.Pilotos",
        "dbo.Equipes",
        "dbo.GrandesPremios",
        "dbo.ClassificacaoPilotos",
        "dbo.ClassificacaoEquipes",
        "dbo.CampeoesBiografia",
        "dbo.FotosHistoricas"
    };

    [GeneratedRegex(@"\b(DROP|ALTER|CREATE|TRUNCATE|GRANT|REVOKE|DENY|EXEC|EXECUTE|SP_|XP_)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ForbiddenDdlRegex();

    private readonly ISqlConnectionFactory _connectionFactory;

    public F1DataIngestionRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public static void ValidateSqlIsDataIngestionOnly(string sql, string targetTable)
    {
        if (!AllowedTargetTables.Contains(targetTable))
        {
            throw new InvalidOperationException($"Tabela '{targetTable}' não está na lista permitida de alimentação de dados.");
        }

        if (ForbiddenDdlRegex().IsMatch(sql))
        {
            throw new InvalidOperationException("Operação bloqueada: o repositório de ingestão proíbe estritamente comandos DDL ou procedures administrativas.");
        }
    }

    public async Task<int> BeginExecutionLogAsync(string mode, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.ExecucoesAgente (Inicio, Modo, SessoesCarregadas, Sucesso, Mensagem)
            OUTPUT INSERTED.ExecucaoId
            VALUES (SYSUTCDATETIME(), @Mode, 0, NULL, 'Em execução');";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.ExecucoesAgente");
        await using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { Mode = mode });
    }

    public async Task CompleteExecutionLogAsync(int executionId, int loadedCount, bool success, string message, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.ExecucoesAgente
            SET Fim = SYSUTCDATETIME(),
                SessoesCarregadas = @LoadedCount,
                Sucesso = @Success,
                Mensagem = @Message
            WHERE ExecucaoId = @ExecutionId;";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.ExecucoesAgente");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            ExecutionId = executionId,
            LoadedCount = loadedCount,
            Success = success,
            Message = message
        });
    }

    public async Task<IReadOnlySet<int>> GetLoadedHistoricalSeasonsAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var years = await conn.QueryAsync<int>("SELECT Temporada FROM dbo.HistTemporadas WITH (NOLOCK)");
        return years.ToHashSet();
    }

    public async Task<IReadOnlyDictionary<int, string>> GetChampionDriverBySeasonAsync(CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<(int Temporada, string PilotoId)>(
            "SELECT Temporada, PilotoId FROM dbo.HistClassificacaoPilotos WITH (NOLOCK) WHERE Posicao = 1");
        return rows
            .GroupBy(x => x.Temporada)
            .ToDictionary(g => g.Key, g => g.First().PilotoId);
    }

    public async Task UpsertCircuitosAsync(IEnumerable<CircuitoIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.Circuitos AS t
            USING (SELECT @CircuitoId AS CircuitoId) AS s
            ON (t.CircuitoId = s.CircuitoId)
            WHEN MATCHED THEN
                UPDATE SET Nome = COALESCE(@Nome, t.Nome),
                           Cidade = COALESCE(@Cidade, t.Cidade),
                           Pais = COALESCE(@Pais, t.Pais),
                           Latitude = COALESCE(@Latitude, t.Latitude),
                           Longitude = COALESCE(@Longitude, t.Longitude)
            WHEN NOT MATCHED THEN
                INSERT (CircuitoId, Nome, Cidade, Pais, Latitude, Longitude)
                VALUES (@CircuitoId, @Nome, @Cidade, @Pais, @Latitude, @Longitude);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.Circuitos");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertPilotosAsync(IEnumerable<PilotoIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.Pilotos AS t
            USING (SELECT @PilotoId AS PilotoId) AS s
            ON (t.PilotoId = s.PilotoId)
            WHEN MATCHED THEN
                UPDATE SET Codigo = COALESCE(@Codigo, t.Codigo),
                           NumeroPermanente = COALESCE(@NumeroPermanente, t.NumeroPermanente),
                           Nome = COALESCE(@Nome, t.Nome),
                           Sobrenome = COALESCE(@Sobrenome, t.Sobrenome),
                           DataNascimento = COALESCE(@DataNascimento, t.DataNascimento),
                           Nacionalidade = COALESCE(@Nacionalidade, t.Nacionalidade),
                           FotoUrl = COALESCE(@FotoUrl, t.FotoUrl)
            WHEN NOT MATCHED THEN
                INSERT (PilotoId, Codigo, NumeroPermanente, Nome, Sobrenome, DataNascimento, Nacionalidade, FotoUrl)
                VALUES (@PilotoId, @Codigo, @NumeroPermanente, @Nome, @Sobrenome, @DataNascimento, @Nacionalidade, @FotoUrl);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.Pilotos");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertEquipesAsync(IEnumerable<EquipeIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.Equipes AS t
            USING (SELECT @EquipeId AS EquipeId) AS s
            ON (t.EquipeId = s.EquipeId)
            WHEN MATCHED THEN
                UPDATE SET Nome = COALESCE(@Nome, t.Nome),
                           Nacionalidade = COALESCE(@Nacionalidade, t.Nacionalidade),
                           Cor = COALESCE(@Cor, t.Cor)
            WHEN NOT MATCHED THEN
                INSERT (EquipeId, Nome, Nacionalidade, Cor)
                VALUES (@EquipeId, @Nome, @Nacionalidade, @Cor);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.Equipes");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertGrandesPremiosAsync(IEnumerable<GrandePremioIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.GrandesPremios AS t
            USING (SELECT @Rodada AS Rodada) AS s
            ON (t.Rodada = s.Rodada)
            WHEN MATCHED THEN
                UPDATE SET Nome = COALESCE(@Nome, t.Nome),
                           CircuitoId = COALESCE(@CircuitoId, t.CircuitoId),
                           DataCorridaUTC = COALESCE(@DataCorridaUTC, t.DataCorridaUTC),
                           TemSprint = @TemSprint,
                           Status = COALESCE(@Status, t.Status)
            WHEN NOT MATCHED THEN
                INSERT (Rodada, Nome, CircuitoId, DataCorridaUTC, TemSprint, Status)
                VALUES (@Rodada, @Nome, @CircuitoId, @DataCorridaUTC, @TemSprint, @Status);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.GrandesPremios");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertClassificacaoPilotosAsync(IEnumerable<ClassificacaoPilotoIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.ClassificacaoPilotos AS t
            USING (SELECT @PilotoId AS PilotoId) AS s
            ON (t.PilotoId = s.PilotoId)
            WHEN MATCHED THEN
                UPDATE SET EquipeId = COALESCE(@EquipeId, t.EquipeId),
                           Posicao = COALESCE(@Posicao, t.Posicao),
                           Pontos = COALESCE(@Pontos, t.Pontos),
                           Vitorias = COALESCE(@Vitorias, t.Vitorias),
                           AposRodada = COALESCE(@AposRodada, t.AposRodada)
            WHEN NOT MATCHED THEN
                INSERT (PilotoId, EquipeId, Posicao, Pontos, Vitorias, AposRodada)
                VALUES (@PilotoId, @EquipeId, @Posicao, @Pontos, @Vitorias, @AposRodada);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.ClassificacaoPilotos");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertClassificacaoEquipesAsync(IEnumerable<ClassificacaoEquipeIngestionItem> items, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE dbo.ClassificacaoEquipes AS t
            USING (SELECT @EquipeId AS EquipeId) AS s
            ON (t.EquipeId = s.EquipeId)
            WHEN MATCHED THEN
                UPDATE SET Posicao = COALESCE(@Posicao, t.Posicao),
                           Pontos = COALESCE(@Pontos, t.Pontos),
                           Vitorias = COALESCE(@Vitorias, t.Vitorias),
                           AposRodada = COALESCE(@AposRodada, t.AposRodada)
            WHEN NOT MATCHED THEN
                INSERT (EquipeId, Posicao, Pontos, Vitorias, AposRodada)
                VALUES (@EquipeId, @Posicao, @Pontos, @Vitorias, @AposRodada);";

        ValidateSqlIsDataIngestionOnly(sql, "dbo.ClassificacaoEquipes");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(sql, items, transaction: tx);
        await tx.CommitAsync(ct);
    }

    public async Task UpsertChampionBiographiesAndPhotosAsync(
        IEnumerable<CampeaoBiografiaIngestionItem> biographies,
        IEnumerable<FotoHistoricaIngestionItem> photos,
        CancellationToken ct = default)
    {
        const string bioSql = @"
            MERGE dbo.CampeoesBiografia AS t
            USING (SELECT @CampeaoId AS CampeaoId) AS s
            ON (t.CampeaoId = s.CampeaoId)
            WHEN MATCHED THEN
                UPDATE SET PilotoId = COALESCE(@PilotoId, t.PilotoId),
                           NomeOriginal = @NomeOriginal,
                           Nacionalidade = COALESCE(@Nacionalidade, t.Nacionalidade),
                           AnosTitulo = COALESCE(@AnosTitulo, t.AnosTitulo),
                           Biografia = COALESCE(@Biografia, t.Biografia),
                           Fonte = COALESCE(@Fonte, t.Fonte)
            WHEN NOT MATCHED THEN
                INSERT (CampeaoId, PilotoId, NomeOriginal, Nacionalidade, AnosTitulo, Biografia, Fonte)
                VALUES (@CampeaoId, @PilotoId, @NomeOriginal, @Nacionalidade, @AnosTitulo, @Biografia, @Fonte);";

        const string photoSql = @"
            MERGE dbo.FotosHistoricas AS t
            USING (SELECT @CampeaoId AS CampeaoId, @Ordem AS Ordem) AS s
            ON (t.CampeaoId = s.CampeaoId AND t.Ordem = s.Ordem)
            WHEN MATCHED THEN
                UPDATE SET Local = @Local, Legenda = @Legenda
            WHEN NOT MATCHED THEN
                INSERT (CampeaoId, Ordem, Local, Legenda)
                VALUES (@CampeaoId, @Ordem, @Local, @Legenda);";

        ValidateSqlIsDataIngestionOnly(bioSql, "dbo.CampeoesBiografia");
        ValidateSqlIsDataIngestionOnly(photoSql, "dbo.FotosHistoricas");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(bioSql, biographies, transaction: tx);
        await conn.ExecuteAsync(photoSql, photos, transaction: tx);
        await tx.CommitAsync(ct);
    }
}

