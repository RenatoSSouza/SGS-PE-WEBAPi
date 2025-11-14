using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Companies;
using sgspe.Models.Enum;

namespace sgspe.Services.Companies;

public class CompanyService
{
    private readonly AppDbContext _context;

    public CompanyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _context.Companies.ToListAsync();
    }

    public async Task<List<Company>> GetBySectorAsync(Sector sector)
    {
        return await _context.Companies
            .Where(c => c.Sector == sector)
            .ToListAsync();
    }

    public async Task<Company> CreateAsync(string legalName, string tradingName, Sector sector)
    {
        if (await _context.Companies.AnyAsync(c => c.LegalName.ToLower() == legalName.ToLower()))
            throw new ArgumentException("Company with the same legal name already exists");

        var company = new Company
        {
            LegalName = legalName,
            TradingName = tradingName,
            Sector = sector
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<Company?> UpdateAsync(Guid id, string? legalName, string? tradingName, Sector? sector)
    {
        var company = await GetByIdAsync(id);
        if (company is null)
            return null;

        if (!string.IsNullOrEmpty(legalName) && 
            legalName != company.LegalName &&
            await _context.Companies.AnyAsync(c => c.LegalName.ToLower() == legalName.ToLower()))
        {
            throw new ArgumentException("Company with the same legal name already exists");
        }

        // Atualiza campos
        if (!string.IsNullOrEmpty(legalName))
            company.LegalName = legalName;

        if (!string.IsNullOrEmpty(tradingName))
            company.TradingName = tradingName;

        if (sector.HasValue)
            company.Sector = sector.Value;

        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await GetByIdAsync(id);
        if (company is null)
            return false;

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return true;
    }
}