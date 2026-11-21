namespace sgspe.Models.Carbon;

/// <summary>
/// Fatores de emissão configuráveis (seção "Emissions" do appsettings).
/// Energia: fator médio anual de 2025 do Sistema Interligado Nacional para inventários
/// corporativos, 0,0461 tCO2/MWh (MCTI/SIRENE, "Fator médio: inventários corporativos").
/// Água e resíduos: valores de referência sem fonte oficial única; devem ser ajustados
/// pelo administrador conforme a concessionária e a destinação local.
/// </summary>
public class EmissionFactors
{
    public const double DefaultEnergyKgCo2PerKwh = 0.0461;

    public double EnergyKgCo2PerKwh { get; set; } = DefaultEnergyKgCo2PerKwh;
    public double WaterKgCo2PerM3 { get; set; } = 0.5;
    public double WasteLandfillKgCo2PerKg { get; set; } = 0.5;
}

public class CarbonFootprint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double EnergyKgCo2e { get; set; }
    public double WaterKgCo2e { get; set; }
    public double WasteKgCo2e { get; set; }
    public double TotalKgCo2e { get; set; }
    public EmissionFactors FactorsUsed { get; set; } = new();
}
