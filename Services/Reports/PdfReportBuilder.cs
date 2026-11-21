using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using sgspe.Models.Reports;

namespace sgspe.Services.Reports;

public static class PdfReportBuilder
{
    static PdfReportBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Build(ReportData d)
    {
        var period = $"{d.Month:00}/{d.Year}";
        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(t => t.FontSize(10));

            page.Header().Column(c =>
            {
                c.Item().Text("Relatório de Sustentabilidade").FontSize(20).SemiBold().FontColor(Colors.Green.Darken3);
                c.Item().Text($"{d.CompanyName}  |  {d.Sector}  |  Período: {period}").FontColor(Colors.Grey.Darken2);
                c.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Green.Darken3);
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(12);

                col.Item().Element(c => Section(c, "Energia", t =>
                {
                    var e = d.Insights.Energy;
                    if (e is null) { Row(t, "Sem registro no período", "-"); return; }
                    Row(t, "Consumo líquido (kWh)", Fmt(e.NetKwh));
                    Row(t, "Estimado pelos equipamentos (kWh)", Fmt(e.EstimatedKwh));
                    Row(t, "Mês anterior (kWh)", e.PreviousMonthKwh is null ? "-" : Fmt(e.PreviousMonthKwh.Value));
                    Row(t, "Variação", e.ChangePercent is null ? "-" : $"{Fmt(e.ChangePercent.Value)}%");
                }));

                col.Item().Element(c => Section(c, "Água", t =>
                {
                    var w = d.Insights.Water;
                    if (w is null) { Row(t, "Sem registro no período", "-"); return; }
                    Row(t, "Consumo líquido (m³)", Fmt(w.NetM3));
                    Row(t, "Água de chuva aproveitada (m³)", Fmt(w.RainwaterHarvestedM3));
                    Row(t, "Variação", w.ChangePercent is null ? "-" : $"{Fmt(w.ChangePercent.Value)}%");
                }));

                col.Item().Element(c => Section(c, "Resíduos", t =>
                {
                    var w = d.Insights.Waste;
                    if (w is null) { Row(t, "Sem registro no período", "-"); return; }
                    Row(t, "Total gerado (kg)", Fmt(w.TotalKg));
                    Row(t, "Reciclado (kg)", Fmt(w.RecycledKg));
                    Row(t, "Taxa de reciclagem", $"{Fmt(w.RecyclingRatePercent)}%");
                    foreach (var x in w.ByType) Row(t, $"  {ReportLabels.Of(x.Type)}", $"{Fmt(x.QuantityKg)} kg");
                }));

                col.Item().Element(c => Section(c, "Pegada de carbono (kg CO₂e)", t =>
                {
                    Row(t, "Energia", Fmt(d.Carbon.EnergyKgCo2e));
                    Row(t, "Água", Fmt(d.Carbon.WaterKgCo2e));
                    Row(t, "Resíduos", Fmt(d.Carbon.WasteKgCo2e));
                    Row(t, "Total", Fmt(d.Carbon.TotalKgCo2e));
                }));

                if (d.Materials.Count > 0)
                    col.Item().Element(c => Section(c, "Matérias-primas", t =>
                    {
                        foreach (var m in d.Materials)
                            Row(t, m.Name + (m.RecycledOrRenewable ? " (reciclado/renovável)" : ""), $"{Fmt(m.Quantity)} {m.Unit}");
                    }));

                if (d.Goals.Count > 0)
                    col.Item().Element(c => Section(c, "Metas", t =>
                    {
                        foreach (var g in d.Goals)
                            Row(t, $"{g.Goal.Title} (alvo {Fmt(g.Goal.TargetValue)}, prazo {g.Goal.Deadline:dd/MM/yyyy})",
                                $"{(g.CurrentValue is null ? "-" : Fmt(g.CurrentValue.Value))} - {ReportLabels.Of(g.Status)}");
                    }));

                if (d.Insights.Suggestions.Count > 0)
                    col.Item().Element(c => Section(c, "Sugestões de melhoria", t =>
                    {
                        foreach (var s in d.Insights.Suggestions)
                            t.Item().Text(text =>
                            {
                                text.Span($"[{s.Area}] {s.Title}: ").SemiBold();
                                text.Span(s.Description);
                            });
                    }));
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                t.Span($"SGS-PE - gerado em {d.GeneratedAtUtc:dd/MM/yyyy HH:mm} UTC - página ");
                t.CurrentPageNumber();
            });
        })).GeneratePdf();
    }

    private static string Fmt(double v) => v.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));

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
