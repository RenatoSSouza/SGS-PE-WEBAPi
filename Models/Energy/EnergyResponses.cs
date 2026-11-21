namespace sgspe.Models.Energy;

public class EnergyRecordResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double GridConsumptionKwh { get; set; }
    public double SolarGenerationKwh { get; set; }
    public double NetConsumptionKwh { get; set; }
    public double EstimatedConsumptionKwh { get; set; }
    public double Co2Kg { get; set; }

    public static EnergyRecordResponse FromEntity(EnergyRecord e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, Month = e.Month, Year = e.Year,
        GridConsumptionKwh = e.GridConsumptionKwh, SolarGenerationKwh = e.SolarGenerationKwh,
        NetConsumptionKwh = e.NetConsumptionKwh, EstimatedConsumptionKwh = e.EstimatedConsumptionKwh,
        Co2Kg = e.Co2Kg
    };
}
