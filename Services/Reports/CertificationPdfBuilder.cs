using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using sgspe.Models.Reports;

namespace sgspe.Services.Reports;

public static class CertificationPdfBuilder
{
    static CertificationPdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly System.Globalization.CultureInfo Br = new("pt-BR");
    private static string Fmt(double v) => v.ToString("N2", Br);

    public static byte[] Build(CertificationData d)
    {
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(c =>
            {
                c.Item().Text("Relatório Anual de Desempenho Ambiental").FontSize(20).SemiBold().FontColor(Colors.Green.Darken3);
                c.Item().Text("Documento de apoio a processos de certificação e auditoria ambiental").FontColor(Colors.Grey.Darken2);
                c.Item().Text($"{d.CompanyName}  |  {d.Sector}  |  Ano-base: {d.Year}").FontColor(Colors.Grey.Darken2);
                c.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Green.Darken3);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(c => Section(c, "1. Indicadores consolidados do ano", t =>
                {
                    Row(t, "Consumo líquido de energia elétrica (kWh)", Fmt(d.EnergyTotalKwh));
                    Row(t, "Consumo líquido de água (m³)", Fmt(d.WaterTotalM3));
                    Row(t, "Resíduos gerados (kg)", Fmt(d.WasteTotalKg));
                    Row(t, "Resíduos reciclados (kg)", Fmt(d.WasteRecycledKg));
                    Row(t, "Taxa de reciclagem", $"{Fmt(d.RecyclingRatePercent)}%");
                    Row(t, "Emissões totais (kg CO₂e)", Fmt(d.TotalKgCo2e));
                }));

                col.Item().Element(c => Section(c, "2. Completude dos dados (meses com registro)", t =>
                {
                    Row(t, "Energia", $"{d.MonthsWithEnergy} de 12");
                    Row(t, "Água", $"{d.MonthsWithWater} de 12");
                    Row(t, "Resíduos", $"{d.MonthsWithWaste} de 12");
                    if (d.MonthsWithEnergy < 12 || d.MonthsWithWater < 12 || d.MonthsWithWaste < 12)
                        t.Item().PaddingTop(3).Text("Atenção: há meses sem registro; os totais refletem apenas os dados informados.")
                            .FontColor(Colors.Red.Darken2);
                }));

                col.Item().Element(c => Section(c, "3. Evolução mensal", t =>
                {
                    t.Item().Table(tb =>
                    {
                        tb.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(50);
                            for (var i = 0; i < 5; i++) cd.RelativeColumn();
                        });
                        foreach (var h in new[] { "Mês", "Energia (kWh)", "Água (m³)", "Resíduos (kg)", "Reciclagem (%)", "CO₂e (kg)" })
                            tb.Cell().Background(Colors.Green.Lighten4).Padding(2).Text(h).SemiBold().FontSize(8);
                        foreach (var m in d.Months)
                        {
                            tb.Cell().Padding(2).Text($"{m.Month:00}/{m.Year}").FontSize(8);
                            tb.Cell().Padding(2).Text(m.EnergyNetKwh is null ? "-" : Fmt(m.EnergyNetKwh.Value)).FontSize(8);
                            tb.Cell().Padding(2).Text(m.WaterNetM3 is null ? "-" : Fmt(m.WaterNetM3.Value)).FontSize(8);
                            tb.Cell().Padding(2).Text(m.WasteTotalKg > 0 ? Fmt(m.WasteTotalKg) : "-").FontSize(8);
                            tb.Cell().Padding(2).Text(m.WasteTotalKg > 0 ? Fmt(m.WasteRecyclingRatePercent) : "-").FontSize(8);
                            tb.Cell().Padding(2).Text(Fmt(m.TotalCo2Kg)).FontSize(8);
                        }
                    });
                }));

                col.Item().Element(c => Section(c, "4. Inventário de emissões e metodologia", t =>
                {
                    Row(t, $"Energia (fator {d.Factors.EnergyKgCo2PerKwh} kg CO₂/kWh)", Fmt(d.EnergyKgCo2e));
                    Row(t, $"Água (fator {d.Factors.WaterKgCo2PerM3} kg CO₂/m³)", Fmt(d.WaterKgCo2e));
                    Row(t, $"Resíduos em aterro (fator {d.Factors.WasteLandfillKgCo2PerKg} kg CO₂/kg)", Fmt(d.WasteKgCo2e));
                    Row(t, "Total (kg CO₂e)", Fmt(d.TotalKgCo2e));
                    t.Item().PaddingTop(3).Text("Emissões = consumo registrado × fator de emissão. Os fatores são configuráveis; " +
                        "confirme a fonte oficial (ex.: MCTI, GHG Protocol Brasil) antes de apresentar a auditores.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                }));

                if (d.Materials.Count > 0)
                    col.Item().Element(c => Section(c, "5. Matérias-primas", t =>
                    {
                        foreach (var m in d.Materials)
                            Row(t, $"{m.Name} ({m.Unit}): {Fmt(m.RecycledOrRenewablePercent)}% reciclado/renovável", Fmt(m.TotalQuantity));
                    }));

                col.Item().Element(c => Section(c, "6. Eficiência energética (inventário de equipamentos)", t =>
                {
                    Row(t, "Equipamentos ativos cadastrados", d.Equipments.ActiveItems.ToString());
                    Row(t, "Consumo mensal estimado (kWh)", Fmt(d.Equipments.EstimatedMonthlyKwh));
                    Row(t, "Participação da iluminação", $"{Fmt(d.Equipments.LightingSharePercent)}%");
                }));

                if (d.Goals.Count > 0)
                    col.Item().Element(c => Section(c, $"7. Metas ambientais (situação em {d.GoalsReferenceMonth:00}/{d.Year})", t =>
                    {
                        foreach (var g in d.Goals)
                            Row(t, $"{g.Goal.Title} (alvo {Fmt(g.Goal.TargetValue)}, prazo {g.Goal.Deadline:dd/MM/yyyy})",
                                $"{(g.CurrentValue is null ? "-" : Fmt(g.CurrentValue.Value))} - {ReportLabels.Of(g.Status)}");
                    }));

                col.Item().Element(c => Section(c, "8. Alinhamento aos Objetivos de Desenvolvimento Sustentável (ONU)", t =>
                {
                    foreach (var kv in d.Ods)
                        Row(t, char.ToUpperInvariant(kv.Key[0]) + kv.Key[1..], $"ODS {kv.Value.Number} - {kv.Value.Name}");
                }));

                col.Item().PaddingTop(25).Column(s =>
                {
                    s.Item().Text("Declaração: as informações acima foram extraídas dos registros mantidos pela empresa no SGS-PE.").FontSize(9);
                    s.Item().PaddingTop(30).Width(250).LineHorizontal(0.5f);
                    s.Item().Text("Responsável legal / data").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                t.Span($"SGS-PE - gerado em {d.GeneratedAtUtc:dd/MM/yyyy HH:mm} UTC - página ");
                t.CurrentPageNumber();
            });
        })).GeneratePdf();
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> body)
        => container.Column(c =>
        {
            c.Item().Text(title).FontSize(13).SemiBold().FontColor(Colors.Green.Darken2);
            c.Item().PaddingTop(3).Column(body);
        });

    private static void Row(ColumnDescriptor t, string label, string value)
        => t.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).Row(r =>
        {
            r.RelativeItem(3).Text(label);
            r.RelativeItem(1).AlignRight().Text(value);
        });
}
