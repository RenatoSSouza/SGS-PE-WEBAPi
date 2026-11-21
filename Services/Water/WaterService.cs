using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Water;

namespace sgspe.Services.Water;

public class WaterService
{
    private readonly AppDbContext _db;
    public WaterService(AppDbContext db) => _db = db;

    public Task<List<WaterRecord>> GetAllByCompanyAsync(Guid companyId)
        => _db.WaterRecords.AsNoTracking().Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).ToListAsync();

    public Task<WaterRecord?> GetByMonthAsync(Guid companyId, int year, int month)
        => _db.WaterRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);

    public async Task<WaterRecord> CreateAsync(Guid companyId, CreateWaterRecordRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new ArgumentException("Empresa não encontrada.");
        if (await _db.WaterRecords.AnyAsync(e => e.CompanyId == companyId && e.Year == r.Year && e.Month == r.Month))
            throw new ArgumentException("Já existe registro de água para este mês.");

        var item = new WaterRecord
        {
            CompanyId = companyId, Year = r.Year, Month = r.Month,
            ConsumptionM3 = r.ConsumptionM3, RainwaterHarvestedM3 = r.RainwaterHarvestedM3
        };
        _db.WaterRecords.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var item = await _db.WaterRecords.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (item is null) return false;
        _db.WaterRecords.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
