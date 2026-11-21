using System.ComponentModel.DataAnnotations;

namespace sgspe.Models.Energy;

public class CreateEnergyRecordRequest
{
    [Range(1, 12)] public int Month { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [Range(0, double.MaxValue)] public double GridConsumptionKwh { get; set; }
    [Range(0, double.MaxValue)] public double SolarGenerationKwh { get; set; }
}
