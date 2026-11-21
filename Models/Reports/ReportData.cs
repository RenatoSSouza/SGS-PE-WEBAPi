using sgspe.Models.Carbon;
using sgspe.Models.Goals;
using sgspe.Models.Insights;
using sgspe.Models.Materials;

namespace sgspe.Models.Reports;

public class ReportData
{
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public InsightsResponse Insights { get; set; } = new();
    public CarbonFootprint Carbon { get; set; } = new();
    public List<MaterialEntryResponse> Materials { get; set; } = new();
    public List<GoalProgress> Goals { get; set; } = new();
}
