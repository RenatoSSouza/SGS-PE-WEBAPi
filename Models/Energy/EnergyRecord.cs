using sgspe.Models.Companies;

namespace sgspe.Models.Energy;

public class EnergyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public double GridConsumptionKwh { get; set; }
    public double SolarGenerationKwh { get; set; } = 0;
    public double EstimatedConsumptionKwh { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }

    private const double Co2Factor = sgspe.Models.Carbon.EmissionFactors.DefaultEnergyKgCo2PerKwh;

    public double NetConsumptionKwh => GridConsumptionKwh - SolarGenerationKwh;
    public double Co2Kg => NetConsumptionKwh * Co2Factor;
}