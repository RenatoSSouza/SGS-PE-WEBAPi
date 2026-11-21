using sgspe.Models.Enum;
using sgspe.Services.Reports;

namespace sgspe.Tests;

public class BenchmarkAndCertificationTests
{
    private static async Task AddPeersAsync(TestDb t, int count, Sector sector, CompanySize? size, double kwh)
    {
        for (var i = 0; i < count; i++)
        {
            var p = await t.AddCompanyAsync(sector, size);
            await t.AddEnergyAsync(p.Id, 2026, 5, kwh);
        }
    }

    [Fact]
    public async Task Benchmark_WithFewerThanThreePeers_IsUnavailable()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, CompanySize.Small);
        await AddPeersAsync(t, 2, Sector.Alojamento, CompanySize.Small, 100);

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5);

        Assert.False(r!.Available);
        Assert.Empty(r.Indicators);
    }

    [Fact]
    public async Task Benchmark_ComparesAgainstSectorAverage()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, CompanySize.Small);
        await t.AddEnergyAsync(me.Id, 2026, 5, 150);
        await AddPeersAsync(t, 3, Sector.Alojamento, CompanySize.Small, 100);

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5);
        var energy = r!.Indicators.Single(i => i.Indicator == "Consumo de energia");

        Assert.True(r.Available);
        Assert.Equal(100, energy.SectorAverage);
        Assert.Equal(50, energy.DifferencePercent);
        Assert.Equal("Worse", energy.Position); // consumir mais é pior
    }

    [Fact]
    public async Task Benchmark_OnlyConsidersSamePorte_WhenSizeIsInformed()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, CompanySize.Small);
        await AddPeersAsync(t, 2, Sector.Alojamento, CompanySize.Small, 100);
        await AddPeersAsync(t, 5, Sector.Alojamento, CompanySize.Medium, 900); // outro porte: ignorado

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5);

        Assert.Equal(2, r!.PeerCompanies);
        Assert.False(r.Available);
        Assert.Equal("Small", r.Size);
    }

    [Fact]
    public async Task Benchmark_WithoutSize_Admin_UsesWholeSector_AndWarns()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, size: null);
        await AddPeersAsync(t, 2, Sector.Alojamento, CompanySize.Small, 100);
        await AddPeersAsync(t, 1, Sector.Alojamento, CompanySize.Medium, 100);

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5, isAdmin: true);

        Assert.True(r!.Available);
        Assert.True(r.AllSizes);
        Assert.Equal(3, r.PeerCompanies);
        Assert.Contains("todos os portes", r.Message);
    }

    [Fact]
    public async Task Benchmark_WithoutSize_NonAdmin_IsUnavailable()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, size: null);
        await AddPeersAsync(t, 3, Sector.Alojamento, CompanySize.Small, 100);

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5);

        Assert.False(r!.Available);
        Assert.Contains("porte", r.Message);
    }

    [Fact]
    public async Task Benchmark_NonAdmin_CannotChooseOtherSizeOrAllSizes()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, CompanySize.Small);
        await AddPeersAsync(t, 2, Sector.Alojamento, CompanySize.Small, 100);
        await AddPeersAsync(t, 5, Sector.Alojamento, CompanySize.Medium, 900);

        var other = await t.Benchmark.GetAsync(me.Id, 2026, 5, isAdmin: false, size: CompanySize.Medium);
        var all = await t.Benchmark.GetAsync(me.Id, 2026, 5, isAdmin: false, allSizes: true);

        Assert.Equal("Small", other!.Size);
        Assert.Equal(2, other.PeerCompanies);
        Assert.Equal(2, all!.PeerCompanies);
        Assert.False(all.AllSizes);
    }

    [Fact]
    public async Task Benchmark_Admin_CanCompareOtherSizeOrAllSizes()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento, CompanySize.Small);
        await AddPeersAsync(t, 2, Sector.Alojamento, CompanySize.Small, 100);
        await AddPeersAsync(t, 5, Sector.Alojamento, CompanySize.Medium, 900);

        var medium = await t.Benchmark.GetAsync(me.Id, 2026, 5, isAdmin: true, size: CompanySize.Medium);
        var all = await t.Benchmark.GetAsync(me.Id, 2026, 5, isAdmin: true, allSizes: true);

        Assert.Equal("Medium", medium!.Size);
        Assert.Equal(5, medium.PeerCompanies);
        Assert.True(all!.AllSizes);
        Assert.Equal(7, all.PeerCompanies);
    }

    [Fact]
    public async Task Benchmark_IgnoresOtherSectorsAndInactiveCompanies()
    {
        using var t = new TestDb();
        var me = await t.AddCompanyAsync(Sector.Alojamento);
        await AddPeersAsync(t, 5, Sector.Agricultura, null, 100);
        var inactive = await t.AddCompanyAsync(Sector.Alojamento);
        inactive.IsActive = false;
        await t.Db.SaveChangesAsync();

        var r = await t.Benchmark.GetAsync(me.Id, 2026, 5);
        Assert.Equal(0, r!.PeerCompanies);
    }

    [Fact]
    public async Task Certification_ConsolidatesYearTotals_AndCountsMonthsWithData()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2025, 1, 100);
        await t.AddEnergyAsync(c.Id, 2025, 7, 200);
        await t.AddWaterAsync(c.Id, 2025, 7, 10);
        await t.AddWasteAsync(c.Id, 2025, 7, 80, recycled: true);
        await t.AddWasteAsync(c.Id, 2025, 7, 20, recycled: false);
        await t.AddEnergyAsync(c.Id, 2024, 1, 999); // outro ano: fora do consolidado

        var d = await t.Certification.BuildAsync(c.Id, 2025);

        Assert.Equal(12, d!.Months.Count);
        Assert.Equal(300, d.EnergyTotalKwh);
        Assert.Equal(2, d.MonthsWithEnergy);
        Assert.Equal(1, d.MonthsWithWater);
        Assert.Equal(80, d.RecyclingRatePercent);
        // 300 kWh x 0.1 + 10 m3 x 1 + 20 kg aterro x 2 = 80
        Assert.Equal(80, d.TotalKgCo2e, 6);
    }

    [Fact]
    public async Task Certification_UnknownCompany_ReturnsNull()
    {
        using var t = new TestDb();
        Assert.Null(await t.Certification.BuildAsync(Guid.NewGuid(), 2025));
    }

    [Fact]
    public async Task Certification_Pdf_IsAValidPdfDocument()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2025, 1, 100);

        var pdf = CertificationPdfBuilder.Build((await t.Certification.BuildAsync(c.Id, 2025))!);

        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.True(pdf.Length > 1000);
    }
}
