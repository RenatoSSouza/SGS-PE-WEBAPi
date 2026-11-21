namespace sgspe.Models.Enum;

public enum GoalMetric
{
    EnergyKwh,          // consumo líquido mensal (menor é melhor)
    WaterM3,            // consumo líquido mensal (menor é melhor)
    WasteKg,            // resíduos gerados no mês (menor é melhor)
    CarbonKgCo2e,       // pegada de carbono mensal (menor é melhor)
    RecyclingRatePercent // taxa de reciclagem (maior é melhor)
}

public enum GoalStatus
{
    NoData,
    OnTrack,
    Warning,
    Off
}
