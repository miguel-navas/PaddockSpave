using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PaddockSpace.Infrastructure.Persistence;

public interface ISqlConnectionFactory
{
    DbConnection CreateConnection();
    string ConnectionString { get; }
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    public const string DefaultLocalConnectionString =
        "Server=localhost;Database=PaddockSpace;Trusted_Connection=True;TrustServerCertificate=True;";

    public string ConnectionString { get; }

    public SqlConnectionFactory(string? connectionString = null)
    {
        ConnectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DefaultLocalConnectionString
            : connectionString;
    }

    public DbConnection CreateConnection() =>
        SqliteCompatibility.IsSqliteConnectionString(ConnectionString)
            ? new SqliteCompatConnection(ConnectionString)
            : new SqlConnection(ConnectionString);
}

public sealed class PaddockSpaceDbContext : DbContext
{
    public PaddockSpaceDbContext(DbContextOptions<PaddockSpaceDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    // 26 Base Tables
    public DbSet<CircuitoRow> Circuitos => Set<CircuitoRow>();
    public DbSet<EquipeRow> Equipes => Set<EquipeRow>();
    public DbSet<PilotoRow> Pilotos => Set<PilotoRow>();
    public DbSet<GrandePremioRow> GrandesPremios => Set<GrandePremioRow>();
    public DbSet<SessaoRow> Sessoes => Set<SessaoRow>();
    public DbSet<ResultadoCorridaRow> ResultadosCorrida => Set<ResultadoCorridaRow>();
    public DbSet<ResultadoSprintRow> ResultadosSprint => Set<ResultadoSprintRow>();
    public DbSet<ClassificatorioRow> Classificatorio => Set<ClassificatorioRow>();
    public DbSet<VoltaRow> Voltas => Set<VoltaRow>();
    public DbSet<PitStopRow> PitStops => Set<PitStopRow>();
    public DbSet<PilotoSessaoRow> PilotosSessao => Set<PilotoSessaoRow>();
    public DbSet<StintRow> Stints => Set<StintRow>();
    public DbSet<ClimaRow> Clima => Set<ClimaRow>();
    public DbSet<ControleProvaRow> ControleProva => Set<ControleProvaRow>();
    public DbSet<ClassificacaoPilotoRow> ClassificacaoPilotos => Set<ClassificacaoPilotoRow>();
    public DbSet<ClassificacaoEquipeRow> ClassificacaoEquipes => Set<ClassificacaoEquipeRow>();
    public DbSet<ExecucaoAgenteRow> ExecucoesAgente => Set<ExecucaoAgenteRow>();
    public DbSet<HistTemporadaRow> HistTemporadas => Set<HistTemporadaRow>();
    public DbSet<HistGrandePremioRow> HistGrandesPremios => Set<HistGrandePremioRow>();
    public DbSet<HistResultadoCorridaRow> HistResultadosCorrida => Set<HistResultadoCorridaRow>();
    public DbSet<HistResultadoSprintRow> HistResultadosSprint => Set<HistResultadoSprintRow>();
    public DbSet<HistClassificatorioRow> HistClassificatorio => Set<HistClassificatorioRow>();
    public DbSet<HistClassificacaoPilotoRow> HistClassificacaoPilotos => Set<HistClassificacaoPilotoRow>();
    public DbSet<HistClassificacaoEquipeRow> HistClassificacaoEquipes => Set<HistClassificacaoEquipeRow>();
    public DbSet<CampeaoBiografiaRow> CampeoesBiografia => Set<CampeaoBiografiaRow>();
    public DbSet<FotoHistoricaRow> FotosHistoricas => Set<FotoHistoricaRow>();

    // 13 Analytical Views
    public DbSet<VwCalendarioRow> VwCalendario => Set<VwCalendarioRow>();
    public DbSet<VwVencedoresRow> VwVencedores => Set<VwVencedoresRow>();
    public DbSet<VwClassificacaoPilotosRow> VwClassificacaoPilotos => Set<VwClassificacaoPilotosRow>();
    public DbSet<VwClassificacaoEquipesRow> VwClassificacaoEquipes => Set<VwClassificacaoEquipesRow>();
    public DbSet<VwEstatisticasPilotosRow> VwEstatisticasPilotos => Set<VwEstatisticasPilotosRow>();
    public DbSet<VwPitStopsMaisRapidosRow> VwPitStopsMaisRapidos => Set<VwPitStopsMaisRapidosRow>();
    public DbSet<VwEstrategiaPneusRow> VwEstrategiaPneus => Set<VwEstrategiaPneusRow>();
    public DbSet<VwClimaSessaoRow> VwClimaSessao => Set<VwClimaSessaoRow>();
    public DbSet<VwResultadosTodosRow> VwResultadosTodos => Set<VwResultadosTodosRow>();
    public DbSet<VwGrandesPremiosTodosRow> VwGrandesPremiosTodos => Set<VwGrandesPremiosTodosRow>();
    public DbSet<VwCampeoesRow> VwCampeoes => Set<VwCampeoesRow>();
    public DbSet<VwCarreiraPilotosRow> VwCarreiraPilotos => Set<VwCarreiraPilotosRow>();
    public DbSet<VwCarreiraEquipesRow> VwCarreiraEquipes => Set<VwCarreiraEquipesRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CircuitoRow>(e =>
        {
            e.ToTable("Circuitos", "dbo");
            e.HasKey(x => x.CircuitoId);
        });

        modelBuilder.Entity<EquipeRow>(e =>
        {
            e.ToTable("Equipes", "dbo");
            e.HasKey(x => x.EquipeId);
        });

        modelBuilder.Entity<PilotoRow>(e =>
        {
            e.ToTable("Pilotos", "dbo");
            e.HasKey(x => x.PilotoId);
        });

        modelBuilder.Entity<GrandePremioRow>(e =>
        {
            e.ToTable("GrandesPremios", "dbo");
            e.HasKey(x => x.Rodada);
        });

        modelBuilder.Entity<SessaoRow>(e =>
        {
            e.ToTable("Sessoes", "dbo");
            e.HasKey(x => x.SessaoKey);
        });

        modelBuilder.Entity<ResultadoCorridaRow>(e =>
        {
            e.ToTable("ResultadosCorrida", "dbo");
            e.HasKey(x => new { x.Rodada, x.PilotoId });
        });

        modelBuilder.Entity<ResultadoSprintRow>(e =>
        {
            e.ToTable("ResultadosSprint", "dbo");
            e.HasKey(x => new { x.Rodada, x.PilotoId });
        });

        modelBuilder.Entity<ClassificatorioRow>(e =>
        {
            e.ToTable("Classificatorio", "dbo");
            e.HasKey(x => new { x.Rodada, x.PilotoId });
        });

        modelBuilder.Entity<VoltaRow>(e =>
        {
            e.ToTable("Voltas", "dbo");
            e.HasKey(x => new { x.Rodada, x.PilotoId, x.Volta });
        });

        modelBuilder.Entity<PitStopRow>(e =>
        {
            e.ToTable("PitStops", "dbo");
            e.HasKey(x => new { x.Rodada, x.PilotoId, x.Parada });
        });

        modelBuilder.Entity<PilotoSessaoRow>(e =>
        {
            e.ToTable("PilotosSessao", "dbo");
            e.HasKey(x => new { x.SessaoKey, x.NumeroCarro });
        });

        modelBuilder.Entity<StintRow>(e =>
        {
            e.ToTable("Stints", "dbo");
            e.HasKey(x => new { x.SessaoKey, x.NumeroCarro, x.Stint });
        });

        modelBuilder.Entity<ClimaRow>(e =>
        {
            e.ToTable("Clima", "dbo");
            e.HasKey(x => new { x.SessaoKey, x.DataHoraUTC });
        });

        modelBuilder.Entity<ControleProvaRow>(e =>
        {
            e.ToTable("ControleProva", "dbo");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<ClassificacaoPilotoRow>(e =>
        {
            e.ToTable("ClassificacaoPilotos", "dbo");
            e.HasKey(x => x.PilotoId);
        });

        modelBuilder.Entity<ClassificacaoEquipeRow>(e =>
        {
            e.ToTable("ClassificacaoEquipes", "dbo");
            e.HasKey(x => x.EquipeId);
        });

        modelBuilder.Entity<ExecucaoAgenteRow>(e =>
        {
            e.ToTable("ExecucoesAgente", "dbo");
            e.HasKey(x => x.ExecucaoId);
        });

        modelBuilder.Entity<HistTemporadaRow>(e =>
        {
            e.ToTable("HistTemporadas", "dbo");
            e.HasKey(x => x.Temporada);
        });

        modelBuilder.Entity<HistGrandePremioRow>(e =>
        {
            e.ToTable("HistGrandesPremios", "dbo");
            e.HasKey(x => new { x.Temporada, x.Rodada });
        });

        modelBuilder.Entity<HistResultadoCorridaRow>(e =>
        {
            e.ToTable("HistResultadosCorrida", "dbo");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<HistResultadoSprintRow>(e =>
        {
            e.ToTable("HistResultadosSprint", "dbo");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<HistClassificatorioRow>(e =>
        {
            e.ToTable("HistClassificatorio", "dbo");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<HistClassificacaoPilotoRow>(e =>
        {
            e.ToTable("HistClassificacaoPilotos", "dbo");
            e.HasKey(x => new { x.Temporada, x.PilotoId });
        });

        modelBuilder.Entity<HistClassificacaoEquipeRow>(e =>
        {
            e.ToTable("HistClassificacaoEquipes", "dbo");
            e.HasKey(x => new { x.Temporada, x.EquipeId });
        });

        modelBuilder.Entity<CampeaoBiografiaRow>(e =>
        {
            e.ToTable("CampeoesBiografia", "dbo");
            e.HasKey(x => x.CampeaoId);
        });

        modelBuilder.Entity<FotoHistoricaRow>(e =>
        {
            e.ToTable("FotosHistoricas", "dbo");
            e.HasKey(x => new { x.CampeaoId, x.Ordem });
        });

        // Views (Keyless)
        modelBuilder.Entity<VwCalendarioRow>().ToView("vw_Calendario", "dbo").HasNoKey();
        modelBuilder.Entity<VwVencedoresRow>().ToView("vw_Vencedores", "dbo").HasNoKey();
        modelBuilder.Entity<VwClassificacaoPilotosRow>().ToView("vw_ClassificacaoPilotos", "dbo").HasNoKey();
        modelBuilder.Entity<VwClassificacaoEquipesRow>().ToView("vw_ClassificacaoEquipes", "dbo").HasNoKey();
        modelBuilder.Entity<VwEstatisticasPilotosRow>().ToView("vw_EstatisticasPilotos", "dbo").HasNoKey();
        modelBuilder.Entity<VwPitStopsMaisRapidosRow>().ToView("vw_PitStopsMaisRapidos", "dbo").HasNoKey();
        modelBuilder.Entity<VwEstrategiaPneusRow>().ToView("vw_EstrategiaPneus", "dbo").HasNoKey();
        modelBuilder.Entity<VwClimaSessaoRow>().ToView("vw_ClimaSessao", "dbo").HasNoKey();
        modelBuilder.Entity<VwResultadosTodosRow>().ToView("vw_ResultadosTodos", "dbo").HasNoKey();
        modelBuilder.Entity<VwGrandesPremiosTodosRow>().ToView("vw_GrandesPremiosTodos", "dbo").HasNoKey();
        modelBuilder.Entity<VwCampeoesRow>().ToView("vw_Campeoes", "dbo").HasNoKey();
        modelBuilder.Entity<VwCarreiraPilotosRow>().ToView("vw_CarreiraPilotos", "dbo").HasNoKey();
        modelBuilder.Entity<VwCarreiraEquipesRow>().ToView("vw_CarreiraEquipes", "dbo").HasNoKey();
    }
}

public sealed class CircuitoRow
{
    public required string CircuitoId { get; set; }
    public required string Nome { get; set; }
    public string? Cidade { get; set; }
    public string? Pais { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed class EquipeRow
{
    public required string EquipeId { get; set; }
    public required string Nome { get; set; }
    public string? Nacionalidade { get; set; }
    public string? Cor { get; set; }
}

public sealed class PilotoRow
{
    public required string PilotoId { get; set; }
    public string? Codigo { get; set; }
    public int? NumeroPermanente { get; set; }
    public string? Nome { get; set; }
    public string? Sobrenome { get; set; }
    public DateTime? DataNascimento { get; set; }
    public string? Nacionalidade { get; set; }
    public string? FotoUrl { get; set; }
}

public sealed class GrandePremioRow
{
    public int Rodada { get; set; }
    public required string Nome { get; set; }
    public string? CircuitoId { get; set; }
    public DateTime? DataCorridaUTC { get; set; }
    public bool TemSprint { get; set; }
    public int? MeetingKey { get; set; }
    public string? Status { get; set; }
}

public sealed class SessaoRow
{
    public int SessaoKey { get; set; }
    public int? Rodada { get; set; }
    public required string Nome { get; set; }
    public string? Tipo { get; set; }
    public DateTime? InicioUTC { get; set; }
    public DateTime? FimUTC { get; set; }
    public bool Cancelada { get; set; }
    public bool DadosCarregados { get; set; }
}

public sealed class ResultadoCorridaRow
{
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Largada { get; set; }
    public int? Posicao { get; set; }
    public string? PosicaoTexto { get; set; }
    public decimal? Pontos { get; set; }
    public int? Voltas { get; set; }
    public string? Status { get; set; }
    public long? TempoMs { get; set; }
    public string? TempoTexto { get; set; }
    public int? VoltaRapidaRank { get; set; }
    public int? VoltaRapidaNum { get; set; }
    public int? VoltaRapidaMs { get; set; }
}

public sealed class ResultadoSprintRow
{
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Largada { get; set; }
    public int? Posicao { get; set; }
    public string? PosicaoTexto { get; set; }
    public decimal? Pontos { get; set; }
    public int? Voltas { get; set; }
    public string? Status { get; set; }
    public long? TempoMs { get; set; }
    public string? TempoTexto { get; set; }
    public int? VoltaRapidaRank { get; set; }
    public int? VoltaRapidaNum { get; set; }
    public int? VoltaRapidaMs { get; set; }
}

public sealed class ClassificatorioRow
{
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Posicao { get; set; }
    public int? Q1Ms { get; set; }
    public int? Q2Ms { get; set; }
    public int? Q3Ms { get; set; }
}

public sealed class VoltaRow
{
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public int Volta { get; set; }
    public int? Posicao { get; set; }
    public int? TempoMs { get; set; }
}

public sealed class PitStopRow
{
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public int Parada { get; set; }
    public int? Volta { get; set; }
    public string? HoraLocal { get; set; }
    public int? DuracaoMs { get; set; }
}

public sealed class PilotoSessaoRow
{
    public int SessaoKey { get; set; }
    public int NumeroCarro { get; set; }
    public string? Codigo { get; set; }
    public string? NomeCompleto { get; set; }
    public string? Equipe { get; set; }
    public string? CorEquipe { get; set; }
    public string? FotoUrl { get; set; }
}

public sealed class StintRow
{
    public int SessaoKey { get; set; }
    public int NumeroCarro { get; set; }
    public int Stint { get; set; }
    public string? Composto { get; set; }
    public int? VoltaInicio { get; set; }
    public int? VoltaFim { get; set; }
    public int? IdadePneuInicio { get; set; }
}

public sealed class ClimaRow
{
    public int SessaoKey { get; set; }
    public DateTime DataHoraUTC { get; set; }
    public decimal? TempAr { get; set; }
    public decimal? TempPista { get; set; }
    public decimal? Umidade { get; set; }
    public decimal? Pressao { get; set; }
    public bool? Chuva { get; set; }
    public decimal? VentoVelocidade { get; set; }
    public int? VentoDirecao { get; set; }
}

public sealed class ControleProvaRow
{
    public int Id { get; set; }
    public int SessaoKey { get; set; }
    public DateTime? DataHoraUTC { get; set; }
    public int? Volta { get; set; }
    public string? Categoria { get; set; }
    public string? Bandeira { get; set; }
    public int? NumeroCarro { get; set; }
    public string? Mensagem { get; set; }
}

public sealed class ClassificacaoPilotoRow
{
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Posicao { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
    public int? AposRodada { get; set; }
}

public sealed class ClassificacaoEquipeRow
{
    public required string EquipeId { get; set; }
    public int? Posicao { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
    public int? AposRodada { get; set; }
}

public sealed class ExecucaoAgenteRow
{
    public int ExecucaoId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string? Modo { get; set; }
    public int? SessoesCarregadas { get; set; }
    public bool? Sucesso { get; set; }
    public string? Mensagem { get; set; }
}

public sealed class HistTemporadaRow
{
    public int Temporada { get; set; }
    public int? Corridas { get; set; }
    public DateTime? CarregadaEm { get; set; }
}

public sealed class HistGrandePremioRow
{
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string Nome { get; set; }
    public string? CircuitoId { get; set; }
    public DateTime? DataCorridaUTC { get; set; }
    public bool TemSprint { get; set; }
}

public sealed class HistResultadoCorridaRow
{
    public int Id { get; set; }
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Largada { get; set; }
    public int? Posicao { get; set; }
    public string? PosicaoTexto { get; set; }
    public decimal? Pontos { get; set; }
    public int? Voltas { get; set; }
    public string? Status { get; set; }
    public long? TempoMs { get; set; }
    public string? TempoTexto { get; set; }
    public int? VoltaRapidaRank { get; set; }
    public int? VoltaRapidaNum { get; set; }
    public int? VoltaRapidaMs { get; set; }
}

public sealed class HistResultadoSprintRow
{
    public int Id { get; set; }
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Largada { get; set; }
    public int? Posicao { get; set; }
    public string? PosicaoTexto { get; set; }
    public decimal? Pontos { get; set; }
    public int? Voltas { get; set; }
    public string? Status { get; set; }
    public long? TempoMs { get; set; }
    public string? TempoTexto { get; set; }
    public int? VoltaRapidaRank { get; set; }
    public int? VoltaRapidaNum { get; set; }
    public int? VoltaRapidaMs { get; set; }
}

public sealed class HistClassificatorioRow
{
    public int Id { get; set; }
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Posicao { get; set; }
    public int? Q1Ms { get; set; }
    public int? Q2Ms { get; set; }
    public int? Q3Ms { get; set; }
}

public sealed class HistClassificacaoPilotoRow
{
    public int Temporada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Posicao { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
}

public sealed class HistClassificacaoEquipeRow
{
    public int Temporada { get; set; }
    public required string EquipeId { get; set; }
    public int? Posicao { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
}

public sealed class CampeaoBiografiaRow
{
    public int CampeaoId { get; set; }
    public string? PilotoId { get; set; }
    public required string NomeOriginal { get; set; }
    public string? Nacionalidade { get; set; }
    public string? AnosTitulo { get; set; }
    public string? Biografia { get; set; }
    public string? Fonte { get; set; }
}

public sealed class FotoHistoricaRow
{
    public int CampeaoId { get; set; }
    public int Ordem { get; set; }
    public string? Local { get; set; }
    public string? Legenda { get; set; }
}

// 13 Views
public sealed class VwCalendarioRow
{
    public int? Rodada { get; set; }
    public string? GrandePremio { get; set; }
    public string? Circuito { get; set; }
    public string? Pais { get; set; }
    public required string Sessao { get; set; }
    public DateTime? InicioBrasilia { get; set; }
    public bool Cancelada { get; set; }
    public bool DadosCarregados { get; set; }
    public bool? TemSprint { get; set; }
    public int SessaoKey { get; set; }
}

public sealed class VwVencedoresRow
{
    public int Rodada { get; set; }
    public required string GrandePremio { get; set; }
    public DateTime? DataBrasilia { get; set; }
    public string? Vencedor { get; set; }
    public string? Equipe { get; set; }
    public string? Pole { get; set; }
    public string? VoltaMaisRapida { get; set; }
}

public sealed class VwClassificacaoPilotosRow
{
    public int? Posicao { get; set; }
    public string? Piloto { get; set; }
    public string? Codigo { get; set; }
    public string? Equipe { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
    public int? AposRodada { get; set; }
}

public sealed class VwClassificacaoEquipesRow
{
    public int? Posicao { get; set; }
    public required string Equipe { get; set; }
    public decimal? Pontos { get; set; }
    public int? Vitorias { get; set; }
    public int? AposRodada { get; set; }
}

public sealed class VwEstatisticasPilotosRow
{
    public required string PilotoId { get; set; }
    public string? Piloto { get; set; }
    public string? Codigo { get; set; }
    public int? PosicaoCampeonato { get; set; }
    public decimal? Pontos { get; set; }
    public int? Corridas { get; set; }
    public int? Vitorias { get; set; }
    public int? Podios { get; set; }
    public int? NosPontos { get; set; }
    public int? Abandonos { get; set; }
    public int? Poles { get; set; }
    public int? VoltasMaisRapidas { get; set; }
    public int? VitoriasSprint { get; set; }
    public decimal? PontosSprint { get; set; }
    public decimal? ClassifMedia { get; set; }
    public decimal? LargadaMedia { get; set; }
    public decimal? ChegadaMedia { get; set; }
    public int? PosicoesGanhas { get; set; }
    public int? VoltasCompletadas { get; set; }
}

public sealed class VwPitStopsMaisRapidosRow
{
    public required string GrandePremio { get; set; }
    public string? Piloto { get; set; }
    public string? Equipe { get; set; }
    public int? Volta { get; set; }
    public decimal? DuracaoSeg { get; set; }
}

public sealed class VwEstrategiaPneusRow
{
    public int? Rodada { get; set; }
    public string? GrandePremio { get; set; }
    public required string Sessao { get; set; }
    public string? Piloto { get; set; }
    public string? Equipe { get; set; }
    public int Stint { get; set; }
    public string? Composto { get; set; }
    public int? VoltaInicio { get; set; }
    public int? VoltaFim { get; set; }
    public int? VoltasNoStint { get; set; }
    public int? IdadePneuInicio { get; set; }
}

public sealed class VwClimaSessaoRow
{
    public int? Rodada { get; set; }
    public string? GrandePremio { get; set; }
    public required string Sessao { get; set; }
    public decimal? TempArMedia { get; set; }
    public decimal? TempPistaMedia { get; set; }
    public decimal? TempPistaMax { get; set; }
    public decimal? UmidadeMedia { get; set; }
    public int? TeveChuva { get; set; }
    public decimal? VentoMedio { get; set; }
}

public sealed class VwResultadosTodosRow
{
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string PilotoId { get; set; }
    public string? EquipeId { get; set; }
    public int? Numero { get; set; }
    public int? Largada { get; set; }
    public int? Posicao { get; set; }
    public string? PosicaoTexto { get; set; }
    public decimal? Pontos { get; set; }
    public int? Voltas { get; set; }
    public string? Status { get; set; }
    public int? VoltaRapidaRank { get; set; }
}

public sealed class VwGrandesPremiosTodosRow
{
    public int Temporada { get; set; }
    public int Rodada { get; set; }
    public required string Nome { get; set; }
    public string? CircuitoId { get; set; }
    public DateTime? DataCorridaUTC { get; set; }
}

public sealed class VwCampeoesRow
{
    public int Temporada { get; set; }
    public string? CampeaoPilotos { get; set; }
    public string? Nacionalidade { get; set; }
    public string? EquipeDoCampeao { get; set; }
    public decimal? PontosCampeao { get; set; }
    public int? VitoriasCampeao { get; set; }
    public string? CampeaConstrutores { get; set; }
    public decimal? PontosConstrutores { get; set; }
    public int TemBiografia { get; set; }
}

public sealed class VwCarreiraPilotosRow
{
    public required string PilotoId { get; set; }
    public string? Piloto { get; set; }
    public string? Nacionalidade { get; set; }
    public int Titulos { get; set; }
    public int? Corridas { get; set; }
    public int? Vitorias { get; set; }
    public int? Podios { get; set; }
    public int? Poles { get; set; }
    public int? VoltasMaisRapidas { get; set; }
    public decimal? Pontos { get; set; }
    public int? PrimeiraTemporada { get; set; }
    public int? UltimaTemporada { get; set; }
    public decimal? PercVitorias { get; set; }
}

public sealed class VwCarreiraEquipesRow
{
    public required string EquipeId { get; set; }
    public required string Equipe { get; set; }
    public string? Nacionalidade { get; set; }
    public int TitulosConstrutores { get; set; }
    public int? Corridas { get; set; }
    public int? Vitorias { get; set; }
    public int? Podios { get; set; }
    public int? Poles { get; set; }
    public int? PrimeiraTemporada { get; set; }
    public int? UltimaTemporada { get; set; }
}

