using ClosedXML.Excel;
using sgspe.Models.Reports;

namespace sgspe.Services.Reports;

public static class ExcelReportBuilder
{
    public static byte[] Build(ReportData d)
    {
        using var wb = new XLWorkbook();

        var ws = wb.AddWorksheet("Resumo");
        ws.Cell(1, 1).Value = "Relatório de Sustentabilidade";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = "Empresa"; ws.Cell(2, 2).Value = d.CompanyName;
        ws.Cell(3, 1).Value = "Setor"; ws.Cell(3, 2).Value = d.Sector;
        ws.Cell(4, 1).Value = "Período"; ws.Cell(4, 2).Value = $"{d.Month:00}/{d.Year}";

        var rows = new List<(string, string, double?)>();
        var e = d.Insights.Energy;
        rows.Add(("Energia", "Consumo líquido (kWh)", e?.NetKwh));
        rows.Add(("Energia", "Estimado equipamentos (kWh)", e?.EstimatedKwh));
        rows.Add(("Energia", "Variação vs mês anterior (%)", e?.ChangePercent));
        var w = d.Insights.Water;
        rows.Add(("Água", "Consumo líquido (m³)", w?.NetM3));
        rows.Add(("Água", "Chuva aproveitada (m³)", w?.RainwaterHarvestedM3));
        rows.Add(("Água", "Variação vs mês anterior (%)", w?.ChangePercent));
        var s = d.Insights.Waste;
        rows.Add(("Resíduos", "Total (kg)", s?.TotalKg));
        rows.Add(("Resíduos", "Reciclado (kg)", s?.RecycledKg));
        rows.Add(("Resíduos", "Taxa de reciclagem (%)", s?.RecyclingRatePercent));
        rows.Add(("Carbono", "Energia (kg CO2e)", d.Carbon.EnergyKgCo2e));
        rows.Add(("Carbono", "Água (kg CO2e)", d.Carbon.WaterKgCo2e));
        rows.Add(("Carbono", "Resíduos (kg CO2e)", d.Carbon.WasteKgCo2e));
        rows.Add(("Carbono", "Total (kg CO2e)", d.Carbon.TotalKgCo2e));

        int r = 6;
        Header(ws, r, "Área", "Indicador", "Valor");
        foreach (var (area, name, value) in rows)
        {
            r++;
            ws.Cell(r, 1).Value = area; ws.Cell(r, 2).Value = name;
            if (value is not null) { ws.Cell(r, 3).Value = value.Value; ws.Cell(r, 3).Style.NumberFormat.Format = "#,##0.00"; }
            else ws.Cell(r, 3).Value = "-";
        }
        ws.Columns().AdjustToContents();

        var wm = wb.AddWorksheet("Resíduos por tipo");
        Header(wm, 1, "Tipo", "Quantidade (kg)");
        int i = 1;
        foreach (var t in d.Insights.Waste?.ByType ?? new())
        { i++; wm.Cell(i, 1).Value = ReportLabels.Of(t.Type); wm.Cell(i, 2).Value = t.QuantityKg; }
        wm.Columns().AdjustToContents();

        var wmat = wb.AddWorksheet("Matérias-primas");
        Header(wmat, 1, "Material", "Quantidade", "Unidade", "Reciclado/renovável");
        i = 1;
        foreach (var m in d.Materials)
        {
            i++;
            wmat.Cell(i, 1).Value = m.Name; wmat.Cell(i, 2).Value = m.Quantity;
            wmat.Cell(i, 3).Value = m.Unit; wmat.Cell(i, 4).Value = m.RecycledOrRenewable ? "Sim" : "Não";
        }
        wmat.Columns().AdjustToContents();

        var wg = wb.AddWorksheet("Metas");
        Header(wg, 1, "Meta", "Métrica", "Alvo", "Prazo", "Valor atual", "Status");
        i = 1;
        foreach (var g in d.Goals)
        {
            i++;
            wg.Cell(i, 1).Value = g.Goal.Title; wg.Cell(i, 2).Value = ReportLabels.Of(g.Goal.Metric);
            wg.Cell(i, 3).Value = g.Goal.TargetValue; wg.Cell(i, 4).Value = g.Goal.Deadline.ToString("dd/MM/yyyy");
            if (g.CurrentValue is not null) wg.Cell(i, 5).Value = g.CurrentValue.Value; else wg.Cell(i, 5).Value = "-";
            wg.Cell(i, 6).Value = ReportLabels.Of(g.Status);
        }
        wg.Columns().AdjustToContents();

        var wsg = wb.AddWorksheet("Sugestões");
        Header(wsg, 1, "Área", "Sugestão", "Descrição");
        i = 1;
        foreach (var x in d.Insights.Suggestions)
        { i++; wsg.Cell(i, 1).Value = x.Area; wsg.Cell(i, 2).Value = x.Title; wsg.Cell(i, 3).Value = x.Description; }
        wsg.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void Header(IXLWorksheet ws, int row, params string[] cols)
    {
        for (int c = 0; c < cols.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = cols[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
        }
    }
}
