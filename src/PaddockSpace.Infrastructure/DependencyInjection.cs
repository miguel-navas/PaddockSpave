using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaddockSpace.Application.Abstractions;
using PaddockSpace.Application.Services;
using PaddockSpace.Infrastructure.Persistence;
using PaddockSpace.Infrastructure.Repositories;

namespace PaddockSpace.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaddockSpaceCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaddockSpace")
                               ?? SqlConnectionFactory.DefaultLocalConnectionString;

        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddDbContext<PaddockSpaceDbContext>(options =>
        {
            if (SqliteCompatibility.IsSqliteConnectionString(connectionString))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        services.AddScoped<IFormula1ReadRepository, Formula1ReadRepository>();
        services.AddScoped<IFormula1QueryService, Formula1QueryService>();

        return services;
    }
}

