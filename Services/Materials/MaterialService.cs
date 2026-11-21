using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Materials;

namespace sgspe.Services.Materials;

public class MaterialService
{
    private readonly AppDbContext _db;
    public MaterialService(AppDbContext db) => _db = db;

    public Task<List<MaterialEntry>> GetAllByCompanyAsync(Guid companyId)
        => _db.MaterialEntries.AsNoTracking().Where(m => m.CompanyId == companyId)
            .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ThenBy(m => m.Name).ToListAsync();

    public async Task<MaterialEntry> CreateAsync(Guid companyId, CreateMaterialEntryRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new ArgumentException("Empresa não encontrada.");

        var item = new MaterialEntry
        {
            CompanyId = companyId, Year = r.Year, Month = r.Month, Name = r.Name.Trim(),
            Unit = r.Unit.Trim(), Quantity = r.Quantity, RecycledOrRenewable = r.RecycledOrRenewable
        };
        _db.MaterialEntries.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var item = await _db.MaterialEntries.FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == companyId);
        if (item is null) return false;
        _db.MaterialEntries.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }
}
