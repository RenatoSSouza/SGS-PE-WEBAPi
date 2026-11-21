using System.ComponentModel.DataAnnotations;

namespace sgspe.Models.Water;

public class CreateWaterRecordRequest
{
    [Range(1, 12)] public int Month { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [Range(0, double.MaxValue)] public double ConsumptionM3 { get; set; }
    [Range(0, double.MaxValue)] public double RainwaterHarvestedM3 { get; set; }
}

public class WaterRecordResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double ConsumptionM3 { get; set; }
    public double RainwaterHarvestedM3 { get; set; }
    public double NetConsumptionM3 { get; set; }

    public static WaterRecordResponse FromEntity(WaterRecord w) => new()
    {
        Id = w.Id, CompanyId = w.CompanyId, Month = w.Month, Year = w.Year,
        ConsumptionM3 = w.ConsumptionM3, RainwaterHarvestedM3 = w.RainwaterHarvestedM3,
        NetConsumptionM3 = w.NetConsumptionM3
    };
}
