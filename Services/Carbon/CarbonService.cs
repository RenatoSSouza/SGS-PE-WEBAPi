using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using sgspe.Data;
using sgspe.Models.Carbon;

namespace sgspe.Services.Carbon;

public class CarbonService
{
    private readonly AppDbContext _db;
    private readonly EmissionFactors _f;

    public CarbonService(AppDbContext db, IOptions<EmissionFactors> factors)
    {
        _db = db;
        _f = factors.Value;
    }

    public async Task<CarbonFootprint> GetMonthlyAsync(Guid companyId, int year, int month)
    {
        var energy = await _db.EnergyRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        var water = await _db.WaterRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        var landfillKg = await _db.WasteEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.Year == year && e.Month == month && !e.Recycled)
            .SumAsync(e => (double?)e.QuantityKg) ?? 0;

        return Compute(year, month, energy?.NetConsumptionKwh ?? 0, water?.NetConsumptionM3 ?? 0, landfillKg);
    }

    public CarbonFootprint Compute(int year, int month, double energyNetKwh, double waterNetM3, double landfillKg)
    {
        var result = new CarbonFootprint
        {
            Year = year, Month = month, FactorsUsed = _f,
            EnergyKgCo2e = Math.Max(0, energyNetKwh) * _f.EnergyKgCo2PerKwh,
            WaterKgCo2e = waterNetM3 * _f.WaterKgCo2PerM3,
            WasteKgCo2e = landfillKg * _f.WasteLandfillKgCo2PerKg
        };
        result.TotalKgCo2e = result.EnergyKgCo2e + result.WaterKgCo2e + result.WasteKgCo2e;
        return result;
    }
}
