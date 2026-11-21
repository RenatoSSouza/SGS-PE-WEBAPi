using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using sgspe.Data;
using sgspe.Models.Carbon;
using sgspe.Models.Companies;
using sgspe.Models.Enum;
using sgspe.Models.Energy;
using sgspe.Models.Waste;
using sgspe.Models.Water;
using sgspe.Services.Benchmark;
using sgspe.Services.Carbon;
using sgspe.Services.Goals;
using sgspe.Services.Insights;
using sgspe.Services.Notifications;
using sgspe.Services.Reports;
using sgspe.Services.Waste;

namespace sgspe.Tests;

/// <summary>Banco SQLite em memória, isolado por teste, com todos os serviços já montados.</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;
    public AppDbContext Db { get; }
    public EmissionFactors Factors { get; } = new() { EnergyKgCo2PerKwh = 0.1, WaterKgCo2PerM3 = 1, WasteLandfillKgCo2PerKg = 2 };
    public CarbonService Carbon { get; }
    public WasteService Waste { get; }
    public GoalService Goals { get; }
    public InsightsService Insights { get; }
    public BenchmarkService Benchmark { get; }
    public NotificationService Notifications { get; }
    public CertificationService Certification { get; }

    public TestDb()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();
        Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);
        Db.Database.EnsureCreated();

        Carbon = new CarbonService(Db, Options.Create(Factors));
        Waste = new WasteService(Db);
        Goals = new GoalService(Db, Carbon, Waste);
        Insights = new InsightsService(Db, Waste, Carbon);
        Benchmark = new BenchmarkService(Db, Waste, Carbon);
        var email = new EmailSender(new ConfigurationBuilder().Build(), NullLogger<EmailSender>.Instance); // sem SMTP
        Notifications = new NotificationService(Db, Goals, email);
        Certification = new CertificationService(Db, Insights, Carbon, Goals);
    }

    private int _seq;

    public async Task<Company> AddCompanyAsync(Sector sector = Sector.Outros, CompanySize? size = null)
    {
        var c = new Company { LegalName = $"Empresa {++_seq}", TradingName = $"E{_seq}", Sector = sector, Size = size };
        Db.Companies.Add(c);
        await Db.SaveChangesAsync();
        return c;
    }

    public async Task AddEnergyAsync(Guid companyId, int year, int month, double grid, double solar = 0)
    {
        Db.EnergyRecords.Add(new EnergyRecord { CompanyId = companyId, Year = year, Month = month, GridConsumptionKwh = grid, SolarGenerationKwh = solar });
        await Db.SaveChangesAsync();
    }

    public async Task AddWaterAsync(Guid companyId, int year, int month, double m3, double rain = 0)
    {
        Db.WaterRecords.Add(new WaterRecord { CompanyId = companyId, Year = year, Month = month, ConsumptionM3 = m3, RainwaterHarvestedM3 = rain });
        await Db.SaveChangesAsync();
    }

    public async Task AddWasteAsync(Guid companyId, int year, int month, double kg, bool recycled, WasteType type = WasteType.Organic)
    {
        Db.WasteEntries.Add(new WasteEntry { CompanyId = companyId, Year = year, Month = month, QuantityKg = kg, Recycled = recycled, Type = type });
        await Db.SaveChangesAsync();
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
