using sgspe.Models.Companies;

namespace sgspe.Models.Water;

public class WaterRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double ConsumptionM3 { get; set; }
    public double RainwaterHarvestedM3 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }

    public double NetConsumptionM3 => Math.Max(0, ConsumptionM3 - RainwaterHarvestedM3);
}
