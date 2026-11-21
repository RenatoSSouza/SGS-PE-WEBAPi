using sgspe.Models.Enum;
using sgspe.Models.Goals;

namespace sgspe.Tests;

public class GoalAndNotificationTests
{
    private static CreateGoalRequest EnergyGoal(double target, DateOnly? deadline = null) => new()
    {
        Title = "Reduzir energia", Metric = GoalMetric.EnergyKwh, TargetValue = target,
        Deadline = deadline ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
    };

    [Theory]
    [InlineData(80, GoalStatus.OnTrack)]   // <= 90% do alvo
    [InlineData(95, GoalStatus.Warning)]   // entre 90% e 100%
    [InlineData(120, GoalStatus.Off)]      // acima do alvo
    public async Task Goal_LowerIsBetter_StatusFollowsThresholds(double consumption, GoalStatus expected)
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, consumption);
        var goal = await t.Goals.CreateAsync(c.Id, EnergyGoal(100));

        var p = await t.Goals.GetProgressAsync(c.Id, goal.Id, 2026, 5);

        Assert.Equal(expected, p!.Status);
    }

    [Fact]
    public async Task Goal_WithoutData_IsNoData()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        var goal = await t.Goals.CreateAsync(c.Id, EnergyGoal(100));

        Assert.Equal(GoalStatus.NoData, (await t.Goals.GetProgressAsync(c.Id, goal.Id, 2026, 5))!.Status);
    }

    [Fact]
    public async Task Goal_RecyclingRate_HigherIsBetter()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddWasteAsync(c.Id, 2026, 5, 100, recycled: true);
        var goal = await t.Goals.CreateAsync(c.Id, new CreateGoalRequest
        {
            Title = "Reciclar", Metric = GoalMetric.RecyclingRatePercent, TargetValue = 50,
            Deadline = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        });

        Assert.Equal(GoalStatus.OnTrack, (await t.Goals.GetProgressAsync(c.Id, goal.Id, 2026, 5))!.Status);
    }

    [Fact]
    public async Task Goal_PastDeadline_IsOverdueAndAppearsInAlerts()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, 10);
        await t.Goals.CreateAsync(c.Id, EnergyGoal(100, new DateOnly(2020, 1, 1)));

        var alerts = await t.Goals.GetAlertsAsync(c.Id, 2026, 5);

        Assert.Single(alerts);
        Assert.True(alerts[0].IsOverdue);
    }

    [Fact]
    public async Task Goal_OnTrack_IsNotAnAlert()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, 10);
        await t.Goals.CreateAsync(c.Id, EnergyGoal(100));

        Assert.Empty(await t.Goals.GetAlertsAsync(c.Id, 2026, 5));
    }

    [Fact]
    public async Task Goal_ResponsibleFromAnotherCompany_IsRejected()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        var request = EnergyGoal(100);
        request.ResponsibleUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => t.Goals.CreateAsync(c.Id, request));
    }

    [Fact]
    public async Task Notifications_AreCreatedOnce_PerGoalSituationAndMonth()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, 500);
        await t.Goals.CreateAsync(c.Id, EnergyGoal(300)); // fora do limite

        var first = await t.Notifications.GenerateGoalNotificationsAsync(c.Id, 2026, 5);
        var second = await t.Notifications.GenerateGoalNotificationsAsync(c.Id, 2026, 5);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
    }

    [Fact]
    public async Task Notifications_ManagerSeesAll_EmployeeOnlyOwn_AndReadStateIsTracked()
    {
        using var t = new TestDb();
        var c = await t.AddCompanyAsync();
        await t.AddEnergyAsync(c.Id, 2026, 5, 500);
        await t.Goals.CreateAsync(c.Id, EnergyGoal(300));
        await t.Notifications.GenerateGoalNotificationsAsync(c.Id, 2026, 5);
        var employee = Guid.NewGuid();
        var manager = Guid.NewGuid();

        Assert.Single(await t.Notifications.ListAsync(c.Id, manager, isManager: true, onlyUnread: false));
        Assert.Empty(await t.Notifications.ListAsync(c.Id, employee, isManager: false, onlyUnread: false));

        Assert.Equal(1, await t.Notifications.UnreadCountAsync(c.Id, manager, true));
        await t.Notifications.MarkAllReadAsync(c.Id, manager, true);
        Assert.Equal(0, await t.Notifications.UnreadCountAsync(c.Id, manager, true));
    }

    [Fact]
    public async Task Notifications_AreIsolatedBetweenCompanies()
    {
        using var t = new TestDb();
        var a = await t.AddCompanyAsync();
        var b = await t.AddCompanyAsync();
        await t.AddEnergyAsync(a.Id, 2026, 5, 500);
        await t.Goals.CreateAsync(a.Id, EnergyGoal(300));
        await t.Notifications.GenerateGoalNotificationsAsync(a.Id, 2026, 5);

        Assert.Empty(await t.Notifications.ListAsync(b.Id, Guid.NewGuid(), isManager: true, onlyUnread: false));
    }
}
