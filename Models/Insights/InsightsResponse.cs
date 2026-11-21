namespace sgspe.Models.Insights;

public class EnergyInsight
{
    public double NetKwh { get; set; }
    public double Co2Kg { get; set; }
    public double EstimatedKwh { get; set; }
    public double? PreviousMonthKwh { get; set; }
    public double? ChangePercent { get; set; }
    public bool Anomaly { get; set; }
}

public class WaterInsight
{
    public double NetM3 { get; set; }
    public double RainwaterHarvestedM3 { get; set; }
    public double? PreviousMonthM3 { get; set; }
    public double? ChangePercent { get; set; }
    public bool Anomaly { get; set; }
}

public class Suggestion
{
    public string Area { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class InsightsResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public EnergyInsight? Energy { get; set; }
    public WaterInsight? Water { get; set; }
    public Waste.WasteMonthlySummary? Waste { get; set; }
    public double TotalCo2Kg { get; set; }
    public List<Suggestion> Suggestions { get; set; } = new();
}

public class OdsReference
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class HistoryPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double? EnergyNetKwh { get; set; }
    public double? WaterNetM3 { get; set; }
    public double WasteTotalKg { get; set; }
    public double WasteRecyclingRatePercent { get; set; }
    public double TotalCo2Kg { get; set; }
}

/// <summary>Série histórica mensal (do mais antigo ao mais recente) para os gráficos do dashboard.</summary>
public class HistoryResponse
{
    public List<HistoryPoint> Points { get; set; } = new();
    /// <summary>ODS da ONU relacionado a cada indicador (energia, agua, residuos, carbono).</summary>
    public Dictionary<string, OdsReference> Ods { get; set; } = new();
}
