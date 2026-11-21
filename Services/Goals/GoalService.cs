using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Enum;
using sgspe.Models.Goals;
using sgspe.Services.Carbon;
using sgspe.Services.Waste;

namespace sgspe.Services.Goals;

public class GoalService
{
    private readonly AppDbContext _db;
    private readonly CarbonService _carbon;
    private readonly WasteService _waste;

    public GoalService(AppDbContext db, CarbonService carbon, WasteService waste)
    {
        _db = db;
        _carbon = carbon;
        _waste = waste;
    }

    public Task<List<Goal>> GetAllByCompanyAsync(Guid companyId)
        => _db.Goals.AsNoTracking().Where(g => g.CompanyId == companyId).OrderBy(g => g.Deadline).ToListAsync();

    public Task<Goal?> GetByIdAsync(Guid companyId, Guid id)
        => _db.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.CompanyId == companyId);

    public async Task<Goal> CreateAsync(Guid companyId, CreateGoalRequest r)
    {
        if (!await _db.Companies.AnyAsync(c => c.Id == companyId))
            throw new ArgumentException("Empresa não encontrada.");
        await ValidateResponsibleAsync(companyId, r.ResponsibleUserId);

        var goal = new Goal
        {
            CompanyId = companyId, Title = r.Title.Trim(), Metric = r.Metric,
            TargetValue = r.TargetValue, Deadline = r.Deadline, ResponsibleUserId = r.ResponsibleUserId
        };
        _db.Goals.Add(goal);
        await _db.SaveChangesAsync();
        return goal;
    }

    public async Task<Goal?> UpdateAsync(Guid companyId, Guid id, UpdateGoalRequest r)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.CompanyId == companyId);
        if (goal is null) return null;
        await ValidateResponsibleAsync(companyId, r.ResponsibleUserId);

        goal.Title = r.Title.Trim(); goal.Metric = r.Metric; goal.TargetValue = r.TargetValue;
        goal.Deadline = r.Deadline; goal.ResponsibleUserId = r.ResponsibleUserId; goal.IsActive = r.IsActive;
        await _db.SaveChangesAsync();
        return goal;
    }

    public async Task<bool> DeleteAsync(Guid companyId, Guid id)
    {
        var goal = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.CompanyId == companyId);
        if (goal is null) return false;
        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<GoalProgress?> GetProgressAsync(Guid companyId, Guid id, int year, int month)
    {
        var goal = await GetByIdAsync(companyId, id);
        return goal is null ? null : await EvaluateAsync(goal, year, month);
    }

    /// <summary>Metas ativas que estão em alerta (Warning/Off) ou vencidas no mês informado.</summary>
    public async Task<List<GoalProgress>> GetAlertsAsync(Guid companyId, int year, int month)
    {
        var goals = await _db.Goals.AsNoTracking().Where(g => g.CompanyId == companyId && g.IsActive).ToListAsync();
        var alerts = new List<GoalProgress>();
        foreach (var goal in goals)
        {
            var p = await EvaluateAsync(goal, year, month);
            if (p.Status is GoalStatus.Warning or GoalStatus.Off || p.IsOverdue) alerts.Add(p);
        }
        return alerts;
    }

    private async Task<GoalProgress> EvaluateAsync(Goal goal, int year, int month)
    {
        var value = await GetMetricValueAsync(goal, year, month);
        var progress = new GoalProgress
        {
            Goal = GoalResponse.FromEntity(goal), Year = year, Month = month, CurrentValue = value,
            IsOverdue = goal.IsActive && goal.Deadline < DateOnly.FromDateTime(DateTime.UtcNow)
        };

        if (value is null)
        {
            progress.Status = GoalStatus.NoData;
            progress.Message = "Sem dados registrados para o mês consultado.";
        }
        else if (goal.HigherIsBetter)
        {
            progress.Status = value >= goal.TargetValue ? GoalStatus.OnTrack
                : value >= goal.TargetValue * 0.9 ? GoalStatus.Warning : GoalStatus.Off;
        }
        else
        {
            progress.Status = value <= goal.TargetValue * 0.9 ? GoalStatus.OnTrack
                : value <= goal.TargetValue ? GoalStatus.Warning : GoalStatus.Off;
        }

        if (progress.IsOverdue)
            progress.Message = "Prazo da meta vencido.";
        else if (progress.Status == GoalStatus.Off)
            progress.Message = "Valor fora do limite da meta.";
        else if (progress.Status == GoalStatus.Warning)
            progress.Message = "Valor próximo do limite da meta.";

        return progress;
    }

    private async Task<double?> GetMetricValueAsync(Goal goal, int year, int month)
    {
        switch (goal.Metric)
        {
            case GoalMetric.EnergyKwh:
                return (await _db.EnergyRecords.AsNoTracking().FirstOrDefaultAsync(e =>
                    e.CompanyId == goal.CompanyId && e.Year == year && e.Month == month))?.NetConsumptionKwh;
            case GoalMetric.WaterM3:
                return (await _db.WaterRecords.AsNoTracking().FirstOrDefaultAsync(e =>
                    e.CompanyId == goal.CompanyId && e.Year == year && e.Month == month))?.NetConsumptionM3;
            case GoalMetric.WasteKg:
            {
                var s = await _waste.GetMonthlySummaryAsync(goal.CompanyId, year, month);
                return s.TotalKg > 0 ? s.TotalKg : null;
            }
            case GoalMetric.RecyclingRatePercent:
            {
                var s = await _waste.GetMonthlySummaryAsync(goal.CompanyId, year, month);
                return s.TotalKg > 0 ? s.RecyclingRatePercent : null;
            }
            case GoalMetric.CarbonKgCo2e:
            {
                var c = await _carbon.GetMonthlyAsync(goal.CompanyId, year, month);
                return c.TotalKgCo2e > 0 ? c.TotalKgCo2e : null;
            }
            default:
                return null;
        }
    }

    private async Task ValidateResponsibleAsync(Guid companyId, Guid? userId)
    {
        if (userId is null) return;
        if (!await _db.Users.AnyAsync(u => u.Id == userId && u.CompanyId == companyId))
            throw new ArgumentException("Responsável não encontrado nesta empresa.");
    }
}
