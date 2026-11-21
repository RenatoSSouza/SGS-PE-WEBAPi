using sgspe.Models.Carbon;
using sgspe.Models.Goals;
using sgspe.Models.Insights;

namespace sgspe.Models.Reports;

public class CertificationMaterial
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double TotalQuantity { get; set; }
    public double RecycledOrRenewableQuantity { get; set; }
    public double RecycledOrRenewablePercent => TotalQuantity > 0 ? Math.Round(RecycledOrRenewableQuantity / TotalQuantity * 100, 1) : 0;
}

public class CertificationEquipmentSummary
{
    public int ActiveItems { get; set; }
    public double EstimatedMonthlyKwh { get; set; }
    public double LightingSharePercent { get; set; }
}

/// <summary>Consolidado anual para auditorias/certificações ambientais.</summary>
public class CertificationData
{
    public string CompanyName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public List<HistoryPoint> Months { get; set; } = new();
    public int MonthsWithEnergy { get; set; }
    public int MonthsWithWater { get; set; }
    public int MonthsWithWaste { get; set; }

    public double EnergyTotalKwh { get; set; }
    public double WaterTotalM3 { get; set; }
    public double WasteTotalKg { get; set; }
    public double WasteRecycledKg { get; set; }
    public double RecyclingRatePercent { get; set; }

    public double EnergyKgCo2e { get; set; }
    public double WaterKgCo2e { get; set; }
    public double WasteKgCo2e { get; set; }
    public double TotalKgCo2e { get; set; }
    public EmissionFactors Factors { get; set; } = new();

    public List<CertificationMaterial> Materials { get; set; } = new();
    public CertificationEquipmentSummary Equipments { get; set; } = new();
    public int GoalsReferenceMonth { get; set; }
    public List<GoalProgress> Goals { get; set; } = new();
    public Dictionary<string, OdsReference> Ods { get; set; } = new();
}
