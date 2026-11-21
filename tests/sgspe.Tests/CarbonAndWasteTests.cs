using sgspe.Models.Enum;

namespace sgspe.Tests;

public class CarbonAndWasteTests
{
    [Fact]
    public async Task Carbon_Monthly_AppliesFactorsAndOnlyCountsLandfillWaste()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, grid: 1000, solar: 200);   // 800 kWh x 0.1 = 80
        await t.AddWaterAsync(c.Id, 2026, 5, m3: 10, rain: 4);            // 6 m3 x 1 = 6
        await t.AddWasteAsync(c.Id, 2026, 5, kg: 50, recycled: false);    // 50 x 2 = 100
        await t.AddWasteAsync(c.Id, 2026, 5, kg: 30, recycled: true);     // reciclado não conta

        var r = await t.Carbon.GetMonthlyAsync(c.Id, 2026, 5);

        Assert.Equal(80, r.EnergyKgCo2e, 6);
        Assert.Equal(6, r.WaterKgCo2e, 6);
        Assert.Equal(100, r.WasteKgCo2e, 6);
        Assert.Equal(186, r.TotalKgCo2e, 6);
    }

    [Fact]
    public async Task Carbon_Monthly_IsolatesCompaniesAndMonths()
    {
        using var t = new TestDb();
        var a = await t.AddCompanyAsync();
        var b = await t.AddCompanyAsync();
        await t.AddEnergyAsync(a.Id, 2026, 5, 1000);
        await t.AddEnergyAsync(b.Id, 2026, 5, 5000);
        await t.AddEnergyAsync(a.Id, 2026, 6, 9000);

        Assert.Equal(100, (await t.Carbon.GetMonthlyAsync(a.Id, 2026, 5)).TotalKgCo2e, 6);
    }

    [Fact]
    public async Task Carbon_Monthly_WithoutData_IsZero()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        Assert.Equal(0, (await t.Carbon.GetMonthlyAsync(c.Id, 2026, 1)).TotalKgCo2e);
    }

    [Fact]
    public async Task Waste_Summary_ComputesRecyclingRateAndTotalsByType()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddWasteAsync(c.Id, 2026, 3, 60, recycled: true, WasteType.Metal);
        await t.AddWasteAsync(c.Id, 2026, 3, 40, recycled: false, WasteType.Organic);

        var s = await t.Waste.GetMonthlySummaryAsync(c.Id, 2026, 3);

        Assert.Equal(100, s.TotalKg);
        Assert.Equal(60, s.RecycledKg);
        Assert.Equal(60, s.RecyclingRatePercent);
        Assert.Equal(2, s.ByType.Count);
    }

    [Fact]
    public async Task Waste_Summary_WithoutEntries_HasZeroRate()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        var s = await t.Waste.GetMonthlySummaryAsync(c.Id, 2026, 3);
        Assert.Equal(0, s.TotalKg);
        Assert.Equal(0, s.RecyclingRatePercent);
    }
}
