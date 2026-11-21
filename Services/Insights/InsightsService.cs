using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Enum;
using sgspe.Models.Insights;
using sgspe.Services.Carbon;
using sgspe.Services.Waste;

namespace sgspe.Services.Insights;

public class InsightsService
{
    private const double AnomalyThresholdPercent = 20;
    private readonly AppDbContext _db;
    private readonly WasteService _waste;

    private readonly CarbonService _carbon;

    public InsightsService(AppDbContext db, WasteService waste, CarbonService carbon)
    {
        _db = db;
        _waste = waste;
        _carbon = carbon;
    }

    public async Task<InsightsResponse> GetInsightsAsync(Guid companyId, int year, int month)
    {
        var prev = new DateTime(year, month, 1).AddMonths(-1);
        var response = new InsightsResponse { Year = year, Month = month };

        var energy = await _db.EnergyRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        if (energy is not null)
        {
            var prevEnergy = await _db.EnergyRecords.AsNoTracking()
                .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == prev.Year && e.Month == prev.Month);
            var change = Change(energy.NetConsumptionKwh, prevEnergy?.NetConsumptionKwh);
            response.Energy = new EnergyInsight
            {
                NetKwh = energy.NetConsumptionKwh, Co2Kg = energy.Co2Kg,
                EstimatedKwh = energy.EstimatedConsumptionKwh,
                PreviousMonthKwh = prevEnergy?.NetConsumptionKwh,
                ChangePercent = change, Anomaly = change > AnomalyThresholdPercent
            };
        }

        response.TotalCo2Kg = (await _carbon.GetMonthlyAsync(companyId, year, month)).TotalKgCo2e;

        var water = await _db.WaterRecords.AsNoTracking()
            .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == year && e.Month == month);
        if (water is not null)
        {
            var prevWater = await _db.WaterRecords.AsNoTracking()
                .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.Year == prev.Year && e.Month == prev.Month);
            var change = Change(water.NetConsumptionM3, prevWater?.NetConsumptionM3);
            response.Water = new WaterInsight
            {
                NetM3 = water.NetConsumptionM3, RainwaterHarvestedM3 = water.RainwaterHarvestedM3,
                PreviousMonthM3 = prevWater?.NetConsumptionM3,
                ChangePercent = change, Anomaly = change > AnomalyThresholdPercent
            };
        }

        var waste = await _waste.GetMonthlySummaryAsync(companyId, year, month);
        if (waste.TotalKg > 0) response.Waste = waste;

        response.Suggestions = await BuildSuggestionsAsync(companyId, response);
        return response;
    }

    public async Task<HistoryResponse> GetHistoryAsync(Guid companyId, int months, int? endYear = null, int? endMonth = null)
    {
        months = Math.Clamp(months, 1, 36);
        var end = new DateTime(endYear ?? DateTime.UtcNow.Year, endMonth ?? DateTime.UtcNow.Month, 1);
        var start = end.AddMonths(-(months - 1));
        int Key(int y, int m) => y * 12 + m;
        var (lo, hi) = (Key(start.Year, start.Month), Key(end.Year, end.Month));

        var energy = (await _db.EnergyRecords.AsNoTracking().Where(e => e.CompanyId == companyId).ToListAsync())
            .Where(e => Key(e.Year, e.Month) >= lo && Key(e.Year, e.Month) <= hi)
            .ToDictionary(e => Key(e.Year, e.Month));
        var water = (await _db.WaterRecords.AsNoTracking().Where(e => e.CompanyId == companyId).ToListAsync())
            .Where(e => Key(e.Year, e.Month) >= lo && Key(e.Year, e.Month) <= hi)
            .ToDictionary(e => Key(e.Year, e.Month));
        var waste = (await _db.WasteEntries.AsNoTracking().Where(e => e.CompanyId == companyId).ToListAsync())
            .Where(e => Key(e.Year, e.Month) >= lo && Key(e.Year, e.Month) <= hi)
            .GroupBy(e => Key(e.Year, e.Month)).ToDictionary(g => g.Key, g => g.ToList());

        var response = new HistoryResponse
        {
            Ods = new()
            {
                ["energia"] = new() { Number = 7, Name = "Energia limpa e acessível" },
                ["agua"] = new() { Number = 6, Name = "Água potável e saneamento" },
                ["residuos"] = new() { Number = 12, Name = "Consumo e produção responsáveis" },
                ["carbono"] = new() { Number = 13, Name = "Ação contra a mudança global do clima" }
            }
        };

        for (var d = start; d <= end; d = d.AddMonths(1))
        {
            var k = Key(d.Year, d.Month);
            energy.TryGetValue(k, out var e);
            water.TryGetValue(k, out var w);
            var entries = waste.TryGetValue(k, out var list) ? list : new();
            var total = entries.Sum(x => x.QuantityKg);
            var recycled = entries.Where(x => x.Recycled).Sum(x => x.QuantityKg);
            var landfill = entries.Where(x => !x.Recycled).Sum(x => x.QuantityKg);

            response.Points.Add(new HistoryPoint
            {
                Year = d.Year, Month = d.Month,
                EnergyNetKwh = e?.NetConsumptionKwh, WaterNetM3 = w?.NetConsumptionM3,
                WasteTotalKg = total,
                WasteRecyclingRatePercent = total > 0 ? Math.Round(recycled / total * 100, 1) : 0,
                TotalCo2Kg = _carbon.Compute(d.Year, d.Month, e?.NetConsumptionKwh ?? 0, w?.NetConsumptionM3 ?? 0, landfill).TotalKgCo2e
            });
        }
        return response;
    }

    private static double? Change(double current, double? previous)
        => previous is > 0 ? Math.Round((current - previous.Value) / previous.Value * 100, 1) : null;

    private async Task<List<Suggestion>> BuildSuggestionsAsync(Guid companyId, InsightsResponse r)
    {
        var list = new List<Suggestion>();
        var equipments = await _db.Equipments.AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive).ToListAsync();
        var days = DateTime.DaysInMonth(r.Year, r.Month);
        var totalKwh = equipments.Sum(e => e.EstimatedMonthlyKwh(days));

        double Share(EquipmentCategory c)
            => totalKwh > 0 ? equipments.Where(e => e.Category == c).Sum(e => e.EstimatedMonthlyKwh(days)) / totalKwh : 0;

        if (Share(EquipmentCategory.Lighting) > 0.15)
            list.Add(new Suggestion { Area = "Energia", Title = "Trocar iluminação por LED",
                Description = "A iluminação representa mais de 15% do consumo estimado. Lâmpadas LED reduzem até 80% do gasto." });
        if (Share(EquipmentCategory.AirConditioning) > 0.30)
            list.Add(new Suggestion { Area = "Energia", Title = "Otimizar o ar-condicionado",
                Description = "Ar-condicionado responde por mais de 30% do consumo. Faça manutenção e ajuste o termostato." });
        if (r.Energy is { Anomaly: true })
            list.Add(new Suggestion { Area = "Energia", Title = "Consumo acima do padrão",
                Description = $"O consumo subiu {r.Energy.ChangePercent}% em relação ao mês anterior. Verifique equipamentos ligados fora do expediente." });
        if (r.Energy is { EstimatedKwh: > 0 } e && e.NetKwh > e.EstimatedKwh * 1.3)
            list.Add(new Suggestion { Area = "Energia", Title = "Consumo real maior que o estimado",
                Description = "O consumo medido excede em mais de 30% o estimado pelos equipamentos cadastrados; pode haver equipamentos não cadastrados ou desperdício." });
        if (r.Energy is { NetKwh: > 500 })
            list.Add(new Suggestion { Area = "Energia", Title = "Avaliar energia solar",
                Description = "Com esse volume de consumo, a geração solar fotovoltaica tende a reduzir custos no médio prazo." });
        if (r.Water is { Anomaly: true })
            list.Add(new Suggestion { Area = "Água", Title = "Consumo de água acima do padrão",
                Description = "Possível vazamento ou uso excessivo. Verifique hidrômetro e instalações." });
        if (r.Water is { RainwaterHarvestedM3: 0, NetM3: > 0 })
            list.Add(new Suggestion { Area = "Água", Title = "Implantar captação de água pluvial",
                Description = "A água da chuva pode abastecer limpeza, jardinagem e descargas." });
        if (r.Waste is { RecyclingRatePercent: < 30 })
            list.Add(new Suggestion { Area = "Resíduos", Title = "Aumentar a reciclagem",
                Description = "Menos de 30% dos resíduos foram reciclados. Implante coleta seletiva e parcerias com cooperativas." });
        return list;
    }
}
