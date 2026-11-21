namespace sgspe.Tests;

public class InsightsTests
{
    [Fact]
    public async Task Insights_FlagsAnomaly_WhenConsumptionRisesMoreThan20Percent()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 4, 100);
        await t.AddEnergyAsync(c.Id, 2026, 5, 130); // +30%

        var r = await t.Insights.GetInsightsAsync(c.Id, 2026, 5);

        Assert.NotNull(r.Energy);
        Assert.Equal(30, r.Energy!.ChangePercent);
        Assert.True(r.Energy.Anomaly);
        Assert.Contains(r.Suggestions, s => s.Title == "Consumo acima do padrão");
    }

    [Fact]
    public async Task Insights_NoAnomaly_ForSmallIncrease()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 4, 100);
        await t.AddEnergyAsync(c.Id, 2026, 5, 110);

        var r = await t.Insights.GetInsightsAsync(c.Id, 2026, 5);
        Assert.False(r.Energy!.Anomaly);
    }

    [Fact]
    public async Task Insights_WithoutPreviousMonth_HasNoChangePercent()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, 100);

        var r = await t.Insights.GetInsightsAsync(c.Id, 2026, 5);
        Assert.Null(r.Energy!.ChangePercent);
        Assert.False(r.Energy.Anomaly);
    }

    [Fact]
    public async Task Insights_SuggestsRainwaterHarvesting_WhenNoneIsCollected()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddWaterAsync(c.Id, 2026, 5, m3: 20, rain: 0);

        var r = await t.Insights.GetInsightsAsync(c.Id, 2026, 5);
        Assert.Contains(r.Suggestions, s => s.Title.Contains("pluvial"));
    }

    [Fact]
    public async Task Insights_SuggestsMoreRecycling_WhenRateBelow30Percent()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddWasteAsync(c.Id, 2026, 5, 90, recycled: false);
        await t.AddWasteAsync(c.Id, 2026, 5, 10, recycled: true);

        var r = await t.Insights.GetInsightsAsync(c.Id, 2026, 5);
        Assert.Contains(r.Suggestions, s => s.Title == "Aumentar a reciclagem");
    }

    [Fact]
    public async Task History_ReturnsRequestedMonthsInOrder_WithGapsAsNull()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 1, 100);
        await t.AddEnergyAsync(c.Id, 2026, 3, 300);

        var h = await t.Insights.GetHistoryAsync(c.Id, 3, endYear: 2026, endMonth: 3);

        Assert.Equal(3, h.Points.Count);
        Assert.Equal(new[] { 1, 2, 3 }, h.Points.Select(p => p.Month));
        Assert.Equal(100, h.Points[0].EnergyNetKwh);
        Assert.Null(h.Points[1].EnergyNetKwh);
        Assert.Equal(300, h.Points[2].EnergyNetKwh);
    }

    [Fact]
    public async Task History_CrossesYearBoundary_AndClampsMonthsTo36()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();

        var h = await t.Insights.GetHistoryAsync(c.Id, 500, endYear: 2026, endMonth: 2);

        Assert.Equal(36, h.Points.Count);
        Assert.Equal((2026, 2), (h.Points[^1].Year, h.Points[^1].Month));
        Assert.Equal((2023, 3), (h.Points[0].Year, h.Points[0].Month));
    }

    [Fact]
    public async Task History_MapsEachIndicatorToAnOds()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        var h = await t.Insights.GetHistoryAsync(c.Id, 1, 2026, 1);

        Assert.Equal(7, h.Ods["energia"].Number);
        Assert.Equal(6, h.Ods["agua"].Number);
        Assert.Equal(12, h.Ods["residuos"].Number);
        Assert.Equal(13, h.Ods["carbono"].Number);
    }
}
