using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Benchmark;
using sgspe.Models.Enum;
using sgspe.Services.Carbon;
using sgspe.Services.Reports;
using sgspe.Services.Waste;

namespace sgspe.Services.Benchmark;

public class BenchmarkService
{
    public const int MinimumPeers = 3;

    private readonly AppDbContext _db;
    private readonly WasteService _waste;
    private readonly CarbonService _carbon;

    public BenchmarkService(AppDbContext db, WasteService waste, CarbonService carbon)
    {
        _db = db;
        _waste = waste;
        _carbon = carbon;
    }

    private record Metrics(double? Energy, double? Water, double? Waste, double? Recycling, double? Co2);

    /// <summary>
    /// Compara a empresa com a média das demais do mesmo setor. Gestor e colaborador comparam sempre com o porte
    /// cadastrado da própria empresa; só o Administrador escolhe outro porte (<paramref name="size"/>) ou todos
    /// os portes (<paramref name="allSizes"/>).
    /// </summary>
    public async Task<BenchmarkResponse?> GetAsync(Guid companyId, int year, int month,
        bool isAdmin = false, CompanySize? size = null, bool allSizes = false)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null) return null;

        if (!isAdmin) { size = null; allSizes = false; }
        var filter = allSizes ? null : size ?? company.Size;

        var response = new BenchmarkResponse
        {
            Year = year, Month = month, Sector = ReportLabels.Of(company.Sector),
            Size = filter?.ToString(), AllSizes = filter is null, MinimumPeers = MinimumPeers
        };

        // Sem porte cadastrado, só o Administrador pode comparar com o setor inteiro.
        if (filter is null && !isAdmin)
        {
            response.Available = false;
            response.Message = "O porte da empresa não está cadastrado. Solicite ao administrador o cadastro do porte para usar o benchmark.";
            return response;
        }

        var peerIds = await _db.Companies.AsNoTracking()
            .Where(c => c.Sector == company.Sector && c.Id != companyId && c.IsActive
                        && (filter == null || c.Size == filter))
            .Select(c => c.Id).ToListAsync();
        response.PeerCompanies = peerIds.Count;

        if (filter is null)
            response.Message = "Comparação com empresas de todos os portes do setor, sem normalização por tamanho.";

        if (peerIds.Count < MinimumPeers)
        {
            response.Available = false;
            response.Message = filter is null
                ? $"São necessárias ao menos {MinimumPeers} outras empresas do mesmo setor para o comparativo."
                : $"São necessárias ao menos {MinimumPeers} outras empresas do mesmo setor e porte para o comparativo.";
            return response;
        }

        var mine = await GetMetricsAsync(companyId, year, month);
        var peers = new List<Metrics>();
        foreach (var id in peerIds) peers.Add(await GetMetricsAsync(id, year, month));

        response.Available = true;
        response.Indicators = new List<BenchmarkIndicator>
        {
            Build("Consumo de energia", "kWh", false, mine.Energy, peers.Select(p => p.Energy)),
            Build("Consumo de água", "m³", false, mine.Water, peers.Select(p => p.Water)),
            Build("Resíduos gerados", "kg", false, mine.Waste, peers.Select(p => p.Waste)),
            Build("Taxa de reciclagem", "%", true, mine.Recycling, peers.Select(p => p.Recycling)),
            Build("Pegada de carbono", "kg CO2e", false, mine.Co2, peers.Select(p => p.Co2)),
        };
        return response;
    }

    private async Task<Metrics> GetMetricsAsync(Guid companyId, int year, int month)
    {
        var energy = await _db.EnergyRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        var water = await _db.WaterRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        var waste = await _waste.GetMonthlySummaryAsync(companyId, year, month);
        var carbon = await _carbon.GetMonthlyAsync(companyId, year, month);

        return new Metrics(
            energy?.NetConsumptionKwh,
            water?.NetConsumptionM3,
            waste.TotalKg > 0 ? waste.TotalKg : null,
            waste.TotalKg > 0 ? waste.RecyclingRatePercent : null,
            carbon.TotalKgCo2e > 0 ? carbon.TotalKgCo2e : null);
    }

    private static BenchmarkIndicator Build(string name, string unit, bool higherIsBetter, double? mine, IEnumerable<double?> peers)
    {
        var values = peers.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var ind = new BenchmarkIndicator
        {
            Indicator = name, Unit = unit, HigherIsBetter = higherIsBetter,
            CompanyValue = mine is null ? null : Math.Round(mine.Value, 2), PeersWithData = values.Count
        };

        // Exige o mesmo mínimo de pares com dado para divulgar a média.
        if (values.Count < MinimumPeers) return ind;

        ind.SectorAverage = Math.Round(values.Average(), 2);
        if (mine is not null && ind.SectorAverage > 0)
        {
            ind.DifferencePercent = Math.Round((mine.Value - ind.SectorAverage.Value) / ind.SectorAverage.Value * 100, 1);
            if (Math.Abs(ind.DifferencePercent.Value) < 0.05) ind.Position = "Equal";
            else ind.Position = (ind.DifferencePercent > 0) == higherIsBetter ? "Better" : "Worse";
        }
        return ind;
    }
}
