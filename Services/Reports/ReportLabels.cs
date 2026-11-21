using sgspe.Models.Enum;

namespace sgspe.Services.Reports;

/// <summary>Nomes em português dos enums exibidos nos relatórios (os mesmos usados nas telas do front-end).</summary>
public static class ReportLabels
{
    /// <summary>Nome do setor com acentuação (o enum não pode ter acentos nem espaços).</summary>
    public static string Of(Sector sector) => sector switch
    {
        Sector.Administracao_Publica => "Administração pública",
        Sector.Agua_e_esgoto => "Água e esgoto",
        Sector.Agricultura => "Agricultura",
        Sector.Alojamento => "Alojamento",
        Sector.Artes => "Artes",
        Sector.Atividades => "Atividades",
        Sector.Atividades_de_Gestao_de_Residuos_de_descontaminacao => "Atividades de gestão de resíduos e descontaminação",
        Sector.Atividades_financeiras => "Atividades financeiras",
        Sector.Atividades_imobiliarias => "Atividades imobiliárias",
        Sector.Atividades_profissionais => "Atividades profissionais",
        Sector.Cientificas => "Científicas",
        Sector.Comercio => "Comércio",
        Sector.Comunicacao => "Comunicação",
        Sector.Construcao => "Construção",
        Sector.Cultura => "Cultura",
        Sector.Defesa_Seguridade_Social => "Defesa e seguridade social",
        Sector.Educacao => "Educação",
        Sector.Eletricidade => "Eletricidade",
        Sector.Esporte => "Esporte",
        Sector.Gas => "Gás",
        Sector.Industrias_de_transformacao => "Indústrias de transformação",
        Sector.Industrias_extrativas => "Indústrias extrativas",
        Sector.Informacao => "Informação",
        Sector.Organismos_internacionais => "Organismos internacionais",
        Sector.Outras_Instituicoes_extraterritoriais => "Outras instituições extraterritoriais",
        Sector.Pecuaria => "Pecuária",
        Sector.Pesca => "Pesca",
        Sector.Producao_florestal => "Produção florestal",
        Sector.Recreacao => "Recreação",
        Sector.Reparacao_de_veiculos_automotores => "Reparação de veículos automotores",
        Sector.Saude_humana => "Saúde humana",
        Sector.Servicos_complementares => "Serviços complementares",
        Sector.Servicos_domesticos => "Serviços domésticos",
        Sector.Servicos_relacionados => "Serviços relacionados",
        Sector.Servicos_sociais => "Serviços sociais",
        Sector.Tecnicas => "Técnicas",
        Sector.Transporte_armazenagem_correio => "Transporte, armazenagem e correio",
        Sector.Outros => "Outros",
        _ => sector.ToString().Replace('_', ' ')
    };

    public static string Of(WasteType type) => type switch
    {
        WasteType.Organic => "Orgânico",
        WasteType.Recyclable => "Reciclável",
        WasteType.Hazardous => "Perigoso",
        WasteType.Electronic => "Eletrônico",
        WasteType.Reject => "Rejeito",
        WasteType.Metal => "Metal",
        _ => type.ToString()
    };

    public static string Of(GoalMetric metric) => metric switch
    {
        GoalMetric.EnergyKwh => "Energia (kWh)",
        GoalMetric.WaterM3 => "Água (m³)",
        GoalMetric.WasteKg => "Resíduos (kg)",
        GoalMetric.CarbonKgCo2e => "Carbono (kg CO₂e)",
        GoalMetric.RecyclingRatePercent => "Taxa de reciclagem (%)",
        _ => metric.ToString()
    };

    public static string Of(GoalStatus status) => status switch
    {
        GoalStatus.OnTrack => "No caminho",
        GoalStatus.Warning => "Atenção",
        GoalStatus.Off => "Fora da meta",
        GoalStatus.NoData => "Sem dados",
        _ => status.ToString()
    };
}
