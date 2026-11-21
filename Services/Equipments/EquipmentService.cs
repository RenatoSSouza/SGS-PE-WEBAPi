using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Equipments;

namespace sgspe.Services.Equipments;

public class EquipmentService
{
    private readonly AppDbContext _db;
    public EquipmentService(AppDbContext db) => _db = db;

    public Task<List<Equipment>> GetAllByCompanyAsync(Guid companyId)
        => _db.Equipments.AsNoTracking().Where(e => e.CompanyId == companyId).OrderBy(e => e.Name).ToListAsync();

    public Task<Equipment?> GetByIdAsync(Guid id)
        => _db.Equipments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Equipment> CreateAsync(Guid companyId, CreateEquipmentRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new ArgumentException("Empresa não encontrada.");

        var item = new Equipment
        {
            CompanyId = companyId, Name = r.Name.Trim(), Category = r.Category,
            PowerWatts = r.PowerWatts, Quantity = r.Quantity, AverageDailyHours = r.AverageDailyHours
        };
        _db.Equipments.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<Equipment?> UpdateAsync(Guid companyId, Guid id, UpdateEquipmentRequest r)
    {
        var item = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (item is null) return null;
        item.Name = r.Name.Trim(); item.Category = r.Category; item.PowerWatts = r.PowerWatts;
        item.Quantity = r.Quantity; item.AverageDailyHours = r.AverageDailyHours; item.IsActive = r.IsActive;
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var item = await _db.Equipments.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == companyId);
        if (item is null) return false;
        _db.Equipments.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
