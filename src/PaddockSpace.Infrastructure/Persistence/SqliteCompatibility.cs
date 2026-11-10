using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace PaddockSpace.Infrastructure.Persistence;

public static partial class SqliteCompatibility
{
    private static int _handlersRegistered;

    public static void EnsureDapperHandlersRegistered()
    {
        if (Interlocked.Exchange(ref _handlersRegistered, 1) == 1)
        {
            return;
        }

        SqlMapper.AddTypeHandler(new SqliteDateTimeHandler());
        SqlMapper.AddTypeHandler(new SqliteNullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new SqliteDecimalHandler());
        SqlMapper.AddTypeHandler(new SqliteNullableDecimalHandler());
    }

    [GeneratedRegex(@"\[dbo\]\.|\bdbo\.", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DboPrefixRegex();

    [GeneratedRegex(@"\bISNULL\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IsNullRegex();

    [GeneratedRegex(@"\+\s*' '\s*\+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SpaceConcatRegex();

    [GeneratedRegex(@"\bSELECT\s+TOP\s+(\d+)\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SelectTopRegex();

    public static string TranslateSqlForSqlite(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return sql;
        }

        var translated = DboPrefixRegex().Replace(sql, string.Empty);
        translated = IsNullRegex().Replace(translated, "IFNULL(");
        translated = SpaceConcatRegex().Replace(translated, "|| ' ' ||");

        var topMatch = SelectTopRegex().Match(translated);
        if (topMatch.Success)
        {
            var limitCount = topMatch.Groups[1].Value;
            translated = SelectTopRegex().Replace(translated, "SELECT ", 1);
            translated = translated.TrimEnd();
            if (translated.EndsWith(';'))
            {
                translated = translated[..^1].TrimEnd() + $" LIMIT {limitCount};";
            }
            else
            {
                translated += $" LIMIT {limitCount}";
            }
        }

        return translated;
    }

    public static bool IsSqliteConnectionString(string connectionString) =>
        connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains(".sqlite", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("Mode=ReadWrite", StringComparison.OrdinalIgnoreCase);

    private sealed class SqliteDateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value) =>
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);

        public override DateTime Parse(object value) => value switch
        {
            DateTime dt => dt,
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) => dt,
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2) => dt2,
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
        };
    }

    private sealed class SqliteNullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime? value) =>
            parameter.Value = value?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value;

        public override DateTime? Parse(object value)
        {
            if (value is null or DBNull) return null;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return null;
            return value switch
            {
                DateTime dt => dt,
                string str when DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) => dt,
                string str when DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2) => dt2,
                _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            };
        }
    }

    private sealed class SqliteDecimalHandler : SqlMapper.TypeHandler<decimal>
    {
        public override void SetValue(IDbDataParameter parameter, decimal value) =>
            parameter.Value = (double)value;

        public override decimal Parse(object value) => value switch
        {
            decimal d => d,
            double db => Convert.ToDecimal(db, CultureInfo.InvariantCulture),
            float f => Convert.ToDecimal(f, CultureInfo.InvariantCulture),
            long l => l,
            int i => i,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
        };
    }

    private sealed class SqliteNullableDecimalHandler : SqlMapper.TypeHandler<decimal?>
    {
        public override void SetValue(IDbDataParameter parameter, decimal? value) =>
            parameter.Value = value.HasValue ? (double)value.Value : DBNull.Value;

        public override decimal? Parse(object value)
        {
            if (value is null or DBNull) return null;
            if (value is string s && string.IsNullOrWhiteSpace(s)) return null;
            return value switch
            {
                decimal d => d,
                double db => Convert.ToDecimal(db, CultureInfo.InvariantCulture),
                float f => Convert.ToDecimal(f, CultureInfo.InvariantCulture),
                long l => l,
                int i => i,
                string str when decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
            };
        }
    }
}

public sealed class SqliteCompatConnection : DbConnection
{
    private readonly SqliteConnection _inner;

    public SqliteCompatConnection(string connectionString)
    {
        SqliteCompatibility.EnsureDapperHandlersRegistered();
        _inner = new SqliteConnection(connectionString);
    }

    [AllowNull]
    public override string ConnectionString
    {
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value ?? string.Empty;
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
    public override void Close() => _inner.Close();
    public override void Open() => _inner.Open();
    public override Task OpenAsync(CancellationToken cancellationToken) => _inner.OpenAsync(cancellationToken);

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        _inner.BeginTransaction(isolationLevel);

    protected override DbCommand CreateDbCommand() =>
        new SqliteCompatCommand(_inner.CreateCommand(), this);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

public sealed class SqliteCompatCommand : DbCommand
{
    private readonly SqliteCommand _inner;
    private DbConnection? _connection;

    public SqliteCompatCommand(SqliteCommand inner, DbConnection connection)
    {
        _inner = inner;
        _connection = connection;
    }

    [AllowNull]
    public override string CommandText
    {
        get => _inner.CommandText;
        set => _inner.CommandText = SqliteCompatibility.TranslateSqlForSqlite(value ?? string.Empty);
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override bool DesignTimeVisible
    {
        get => _inner.DesignTimeVisible;
        set => _inner.DesignTimeVisible = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value as SqliteTransaction;
    }

    public override void Cancel() => _inner.Cancel();
    public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();
    public override object? ExecuteScalar() => _inner.ExecuteScalar();
    public override void Prepare() => _inner.Prepare();

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
        _inner.ExecuteNonQueryAsync(cancellationToken);

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
        _inner.ExecuteScalarAsync(cancellationToken);

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        _inner.ExecuteReader(behavior);

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken) =>
        await _inner.ExecuteReaderAsync(behavior, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class SqliteDatabaseExporter
{
    public static async Task ExportSqlServerToSqliteAsync(
        string sqlServerConnString,
        string sqliteFilePath,
        CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(sqliteFilePath));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(sqliteFilePath))
        {
            File.Delete(sqliteFilePath);
        }

        await using var sqlConn = new SqlConnection(sqlServerConnString);
        await sqlConn.OpenAsync(ct);

        var sqliteConnStr = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteFilePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();

        await using var sqliteConn = new SqliteConnection(sqliteConnStr);
        await sqliteConn.OpenAsync(ct);

        await using (var pragmaCmd = sqliteConn.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA journal_mode = OFF; PRAGMA synchronous = OFF;";
            await pragmaCmd.ExecuteNonQueryAsync(ct);
        }

        var objects = (await sqlConn.QueryAsync<string>(@"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'dbo'
            ORDER BY CASE WHEN TABLE_TYPE = 'BASE TABLE' THEN 0 ELSE 1 END, TABLE_NAME;")).ToList();

        foreach (var objName in objects)
        {
            Console.WriteLine($"[SQLite Export] Exporting dbo.{objName}...");
            await ExportRelationAsync(sqlConn, sqliteConn, objName, ct);
        }

        await using (var idxCmd = sqliteConn.CreateCommand())
        {
            idxCmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_vw_ResultadosTodos_Temp ON vw_ResultadosTodos (Temporada, Rodada);
                CREATE INDEX IF NOT EXISTS IX_vw_ResultadosTodos_Piloto ON vw_ResultadosTodos (PilotoId);
                CREATE INDEX IF NOT EXISTS IX_vw_ResultadosTodos_Equipe ON vw_ResultadosTodos (EquipeId);
                CREATE INDEX IF NOT EXISTS IX_vw_GrandesPremiosTodos_Temp ON vw_GrandesPremiosTodos (Temporada, Rodada);
                CREATE INDEX IF NOT EXISTS IX_HistResultadosCorrida_Temp ON HistResultadosCorrida (Temporada, Rodada);
                CREATE INDEX IF NOT EXISTS IX_HistClassificacaoPilotos_Temp ON HistClassificacaoPilotos (Temporada, Posicao);
                CREATE INDEX IF NOT EXISTS IX_HistClassificacaoEquipes_Temp ON HistClassificacaoEquipes (Temporada, Posicao);
                PRAGMA journal_mode = DELETE;
                PRAGMA synchronous = NORMAL;
                VACUUM;";
            await idxCmd.ExecuteNonQueryAsync(ct);
        }

        Console.WriteLine($"[SQLite Export] Completed: {sqliteFilePath}");
    }

    private static async Task ExportRelationAsync(
        SqlConnection sqlConn,
        SqliteConnection sqliteConn,
        string objectName,
        CancellationToken ct)
    {
        await using var selectCmd = new SqlCommand($"SELECT * FROM [dbo].[{objectName}]", sqlConn)
        {
            CommandTimeout = 180
        };
        await using var reader = await selectCmd.ExecuteReaderAsync(ct);

        var fieldCount = reader.FieldCount;
        var colDefs = new List<string>(fieldCount);
        var colNames = new List<string>(fieldCount);
        var paramNames = new List<string>(fieldCount);

        for (var i = 0; i < fieldCount; i++)
        {
            var colName = reader.GetName(i);
            var clrType = reader.GetFieldType(i);
            var sqliteType = MapClrTypeToSqlite(clrType);
            colDefs.Add($"[{colName}] {sqliteType}");
            colNames.Add($"[{colName}]");
            paramNames.Add($"@p{i}");
        }

        await using (var createCmd = sqliteConn.CreateCommand())
        {
            createCmd.CommandText = $"CREATE TABLE [{objectName}] ({string.Join(", ", colDefs)});";
            await createCmd.ExecuteNonQueryAsync(ct);
        }

        await using var tx = await sqliteConn.BeginTransactionAsync(ct);
        await using var insertCmd = sqliteConn.CreateCommand();
        insertCmd.Transaction = (SqliteTransaction)tx;
        insertCmd.CommandText = $"INSERT INTO [{objectName}] ({string.Join(", ", colNames)}) VALUES ({string.Join(", ", paramNames)});";

        var parameters = new SqliteParameter[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            parameters[i] = insertCmd.Parameters.Add($"@p{i}", SqliteType.Text);
        }
        insertCmd.Prepare();

        while (await reader.ReadAsync(ct))
        {
            for (var i = 0; i < fieldCount; i++)
            {
                if (reader.IsDBNull(i))
                {
                    parameters[i].Value = DBNull.Value;
                    continue;
                }

                var val = reader.GetValue(i);
                parameters[i].Value = val switch
                {
                    bool b => b ? 1 : 0,
                    DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
                    decimal dec => (double)dec,
                    _ => val
                };
            }

            await insertCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    private static string MapClrTypeToSqlite(Type clrType)
    {
        var t = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(bool))
            return "INTEGER";
        if (t == typeof(decimal) || t == typeof(double) || t == typeof(float))
            return "REAL";
        if (t == typeof(byte[]))
            return "BLOB";
        return "TEXT";
    }
}

