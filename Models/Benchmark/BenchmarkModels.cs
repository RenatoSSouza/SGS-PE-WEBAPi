namespace sgspe.Models.Benchmark;

public class BenchmarkIndicator
{
    public string Indicator { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool HigherIsBetter { get; set; }
    public double? CompanyValue { get; set; }
    public double? SectorAverage { get; set; }
    public int PeersWithData { get; set; }
    /// <summary>(empresa - média) / média * 100. Null se não houver dado dos dois lados.</summary>
    public double? DifferencePercent { get; set; }
    /// <summary>Better, Worse, Equal ou null.</summary>
    public string? Position { get; set; }
}

public class BenchmarkResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Sector { get; set; } = string.Empty;
    /// <summary>Porte usado no filtro de pares (Micro/Small/Medium); nulo se a empresa não informou.</summary>
    public string? Size { get; set; }
    /// <summary>True quando a comparação considera todos os portes (só para o Administrador).</summary>
    public bool AllSizes { get; set; }
    public int PeerCompanies { get; set; }
    public int MinimumPeers { get; set; }
    /// <summary>False quando há poucas empresas no setor; evita expor dados de uma empresa específica.</summary>
    public bool Available { get; set; }
    public string? Message { get; set; }
    public List<BenchmarkIndicator> Indicators { get; set; } = new();
}
