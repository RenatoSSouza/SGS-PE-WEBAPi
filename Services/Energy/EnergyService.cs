using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Energy;

namespace sgspe.Services.Energy;

public class EnergyService
{
    private readonly AppDbContext _db;
    public EnergyService(AppDbContext db) => _db = db;

    public Task<List<EnergyRecord>> GetAllByCompanyAsync(Guid companyId)
        => _db.EnergyRecords.AsNoTracking().Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.Year).ThenByDescending(e => e.Month).ToListAsync();

    public Task<EnergyRecord?> GetByMonthAsync(Guid companyId, int year, int month)
        => _db.EnergyRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);

    public async Task<EnergyRecord> CreateAsync(Guid companyId, CreateEnergyRecordRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new ArgumentException("Empresa não encontrada.");
        if (await _db.EnergyRecords.AnyAsync(e => e.CompanyId == companyId && e.Year == r.Year && e.Month == r.Month))
            throw new ArgumentException("Já existe registro de energia para este mês.");

        var days = DateTime.DaysInMonth(r.Year, r.Month);
        var equipments = await _db.Equipments.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive).ToListAsync();

        var item = new EnergyRecord
        {
            CompanyId = companyId, Year = r.Year, Month = r.Month,
            GridConsumptionKwh = r.GridConsumptionKwh, SolarGenerationKwh = r.SolarGenerationKwh,
            EstimatedConsumptionKwh = equipments.Sum(e => e.EstimatedMonthlyKwh(days))
        };
        _db.EnergyRecords.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var item = await _db.EnergyRecords.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (item is null) return false;
        _db.EnergyRecords.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
