using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Enum;
using sgspe.Models.Reports;
using sgspe.Services.Carbon;
using sgspe.Services.Goals;
using sgspe.Services.Insights;

namespace sgspe.Services.Reports;

public class CertificationService
{
    private readonly AppDbContext _db;
    private readonly InsightsService _insights;
    private readonly CarbonService _carbon;
    private readonly GoalService _goals;

    public CertificationService(AppDbContext db, InsightsService insights, CarbonService carbon, GoalService goals)
    {
        _db = db;
        _insights = insights;
        _carbon = carbon;
        _goals = goals;
    }

    public async Task<CertificationData?> BuildAsync(Guid companyId, int year)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null) return null;

        var history = await _insights.GetHistoryAsync(companyId, 12, year, 12);
        var data = new CertificationData
        {
            CompanyName = string.IsNullOrWhiteSpace(company.TradingName) ? company.LegalName : company.TradingName,
            Sector = ReportLabels.Of(company.Sector),
            Year = year,
            Months = history.Points,
            Ods = history.Ods
        };

        data.MonthsWithEnergy = history.Points.Count(p => p.EnergyNetKwh is not null);
        data.MonthsWithWater = history.Points.Count(p => p.WaterNetM3 is not null);
        data.MonthsWithWaste = history.Points.Count(p => p.WasteTotalKg > 0);
        data.EnergyTotalKwh = history.Points.Sum(p => p.EnergyNetKwh ?? 0);
        data.WaterTotalM3 = history.Points.Sum(p => p.WaterNetM3 ?? 0);

        var waste = await _db.WasteEntries.AsNoTracking()
            .Where(w => w.CompanyId == companyId && w.Year == year).ToListAsync();
        data.WasteTotalKg = waste.Sum(w => w.QuantityKg);
        data.WasteRecycledKg = waste.Where(w => w.Recycled).Sum(w => w.QuantityKg);
        data.RecyclingRatePercent = data.WasteTotalKg > 0 ? Math.Round(data.WasteRecycledKg / data.WasteTotalKg * 100, 1) : 0;

        for (var m = 1; m <= 12; m++)
        {
            var c = await _carbon.GetMonthlyAsync(companyId, year, m);
            data.EnergyKgCo2e += c.EnergyKgCo2e;
            data.WaterKgCo2e += c.WaterKgCo2e;
            data.WasteKgCo2e += c.WasteKgCo2e;
            data.Factors = c.FactorsUsed;
        }
        data.TotalKgCo2e = data.EnergyKgCo2e + data.WaterKgCo2e + data.WasteKgCo2e;

        var materials = await _db.MaterialEntries.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Year == year).ToListAsync();
        data.Materials = materials.GroupBy(x => (x.Name.Trim().ToLowerInvariant(), x.Unit.Trim().ToLowerInvariant()))
            .Select(g => new CertificationMaterial
            {
                Name = g.First().Name, Unit = g.First().Unit,
                TotalQuantity = g.Sum(x => x.Quantity),
                RecycledOrRenewableQuantity = g.Where(x => x.RecycledOrRenewable).Sum(x => x.Quantity)
            }).OrderBy(x => x.Name).ToList();

        var equipments = await _db.Equipments.AsNoTracking().Where(e => e.CompanyId == companyId && e.IsActive).ToListAsync();
        var kwh = equipments.Sum(e => e.EstimatedMonthlyKwh(30));
        data.Equipments = new CertificationEquipmentSummary
        {
            ActiveItems = equipments.Sum(e => e.Quantity),
            EstimatedMonthlyKwh = kwh,
            LightingSharePercent = kwh > 0
                ? Math.Round(equipments.Where(e => e.Category == EquipmentCategory.Lighting).Sum(e => e.EstimatedMonthlyKwh(30)) / kwh * 100, 1)
                : 0
        };

        // Metas avaliadas no último mês do ano (ou no mês corrente, se o ano ainda não terminou).
        var now = DateTime.UtcNow;
        data.GoalsReferenceMonth = year == now.Year ? now.Month : 12;
        var goals = await _db.Goals.AsNoTracking().Where(g => g.CompanyId == companyId && g.IsActive).ToListAsync();
        foreach (var g in goals)
        {
            var p = await _goals.GetProgressAsync(companyId, g.Id, year, data.GoalsReferenceMonth);
            if (p is not null) data.Goals.Add(p);
        }
        return data;
    }
}
