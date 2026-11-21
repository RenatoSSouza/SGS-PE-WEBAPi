using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Goals;
using sgspe.Models.Materials;
using sgspe.Models.Reports;
using sgspe.Services.Carbon;
using sgspe.Services.Goals;
using sgspe.Services.Insights;

namespace sgspe.Services.Reports;

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly InsightsService _insights;
    private readonly CarbonService _carbon;
    private readonly GoalService _goals;

    public ReportService(AppDbContext db, InsightsService insights, CarbonService carbon, GoalService goals)
    {
        _db = db;
        _insights = insights;
        _carbon = carbon;
        _goals = goals;
    }

    public async Task<ReportData?> BuildAsync(Guid companyId, int year, int month)
    {
        var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
        if (company is null) return null;

        var materials = await _db.MaterialEntries.AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.Year == year && m.Month == month)
            .OrderBy(m => m.Name).ToListAsync();

        var goals = await _db.Goals.AsNoTracking().Where(g => g.CompanyId == companyId && g.IsActive).ToListAsync();
        var progress = new List<GoalProgress>();
        foreach (var g in goals)
            progress.Add((await _goals.GetProgressAsync(companyId, g.Id, year, month))!);

        return new ReportData
        {
            CompanyName = string.IsNullOrWhiteSpace(company.TradingName) ? company.LegalName : company.TradingName,
            Sector = ReportLabels.Of(company.Sector),
            Year = year, Month = month,
            Insights = await _insights.GetInsightsAsync(companyId, year, month),
            Carbon = await _carbon.GetMonthlyAsync(companyId, year, month),
            Materials = materials.Select(MaterialEntryResponse.FromEntity).ToList(),
            Goals = progress
        };
    }
}
