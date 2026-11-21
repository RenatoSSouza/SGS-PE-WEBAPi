using System.ComponentModel.DataAnnotations;
using sgspe.Models.Enum;

namespace sgspe.Models.Waste;

public class CreateWasteEntryRequest
{
    [Range(1, 12)] public int Month { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    public WasteType Type { get; set; }
    [Range(0.001, double.MaxValue)] public double QuantityKg { get; set; }
    public bool Recycled { get; set; }
}

public class WasteEntryResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public WasteType Type { get; set; }
    public double QuantityKg { get; set; }
    public bool Recycled { get; set; }

    public static WasteEntryResponse FromEntity(WasteEntry w) => new()
    {
        Id = w.Id, CompanyId = w.CompanyId, Month = w.Month, Year = w.Year,
        Type = w.Type, QuantityKg = w.QuantityKg, Recycled = w.Recycled
    };
}

public class WasteTypeTotal
{
    public WasteType Type { get; set; }
    public double QuantityKg { get; set; }
}

public class WasteMonthlySummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double TotalKg { get; set; }
    public double RecycledKg { get; set; }
    public double RecyclingRatePercent { get; set; }
    public List<WasteTypeTotal> ByType { get; set; } = new();
}
