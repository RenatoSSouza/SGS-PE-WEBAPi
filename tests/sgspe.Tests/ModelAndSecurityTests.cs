using sgspe.Models.Energy;
using sgspe.Models.Equipments;
using sgspe.Models.Water;
using sgspe.Services.Users;

namespace sgspe.Tests;

public class ModelAndSecurityTests
{
    [Fact]
    public void Energy_NetConsumption_SubtractsSolarGeneration()
        => Assert.Equal(70, new EnergyRecord { GridConsumptionKwh = 100, SolarGenerationKwh = 30 }.NetConsumptionKwh);

    [Fact]
    public void Water_NetConsumption_NeverNegative()
        => Assert.Equal(0, new WaterRecord { ConsumptionM3 = 5, RainwaterHarvestedM3 = 9 }.NetConsumptionM3);

    [Fact]
    public void Equipment_EstimatedMonthlyKwh_UsesPowerHoursQuantityAndDays()
    {
        // 100 W x 10 h x 2 un x 30 dias = 60 kWh
        var e = new Equipment { PowerWatts = 100, AverageDailyHours = 10, Quantity = 2 };
        Assert.Equal(60, e.EstimatedMonthlyKwh(30), 6);
    }

    [Fact]
    public void PasswordHasher_VerifiesCorrectPassword_AndRejectsWrongOne()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("Segredo123");

        Assert.NotEqual("Segredo123", hash);
        Assert.True(hasher.VerifyPassword("Segredo123", hash));
        Assert.False(hasher.VerifyPassword("outra", hash));
    }
}
