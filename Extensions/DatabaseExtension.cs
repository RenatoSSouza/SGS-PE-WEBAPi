using Microsoft.EntityFrameworkCore;
using sgspe.Data;

namespace sgspe.Extensions;

public static class DatabaseExtension
{
    /// <summary>True quando a configuração aponta para PostgreSQL (Database:Provider = "Postgres").</summary>
    public static bool UsesPostgres(this IConfiguration configuration)
        => string.Equals(configuration["Database:Provider"], "Postgres", StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (configuration.UsesPostgres())
                options.UseNpgsql(configuration.GetConnectionString("PostgresConnection"));
            else
                options.UseSqlite(configuration.GetConnectionString("SqliteConnection"));
        });

        return services;
    }

    /// <summary>
    /// SQLite (desenvolvimento) usa as migrations. As migrations atuais foram geradas para SQLite
    /// (Guid como TEXT, double como REAL), então no PostgreSQL o esquema é criado a partir do modelo
    /// com EnsureCreated, que mapeia Guid para uuid e double para double precision.
    /// </summary>
    public static void InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (app.Configuration.UsesPostgres()) db.Database.EnsureCreated();
        else db.Database.Migrate();
    }
}
