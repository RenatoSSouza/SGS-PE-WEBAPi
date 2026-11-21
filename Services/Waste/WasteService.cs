using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Waste;

namespace sgspe.Services.Waste;

public class WasteService
{
    private readonly AppDbContext _db;
    public WasteService(AppDbContext db) => _db = db;

    public Task<List<WasteEntry>> GetAllByCompanyAsync(Guid companyId)
        => _db.WasteEntries.AsNoTracking().Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).ToListAsync();

    public async Task<WasteMonthlySummary> GetMonthlySummaryAsync(Guid companyId, int year, int month)
    {
        var entries = await _db.WasteEntries.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.Year == year && e.Month == month).ToListAsync();

        var total = entries.Sum(e => e.QuantityKg);
        var recycled = entries.Where(e => e.Recycled).Sum(e => e.QuantityKg);
        return new WasteMonthlySummary
        {
            Year = year, Month = month, TotalKg = total, RecycledKg = recycled,
            RecyclingRatePercent = total > 0 ? Math.Round(recycled / total * 100, 1) : 0,
            ByType = entries.GroupBy(e => e.Type)
                .Select(g => new WasteTypeTotal { Type = g.Key, QuantityKg = g.Sum(x => x.QuantityKg) }).ToList()
        };
    }

    public async Task<WasteEntry> CreateAsync(Guid companyId, CreateWasteEntryRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new KeyNotFoundException("Empresa não encontrada.");

        var item = new WasteEntry
        {
            CompanyId = companyId, Year = r.Year, Month = r.Month,
            Type = r.Type, QuantityKg = r.QuantityKg, Recycled = r.Recycled
        };
        _db.WasteEntries.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var item = await _db.WasteEntries.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (item is null) return false;
        _db.WasteEntries.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
