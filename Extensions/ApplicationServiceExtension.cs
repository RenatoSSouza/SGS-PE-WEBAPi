using sgspe.Services.Companies;
using sgspe.Services.Energy;
using sgspe.Services.Equipments;
using sgspe.Services.Insights;
using sgspe.Services.Users;
using sgspe.Services.Waste;
using sgspe.Services.Water;

namespace sgspe.Extensions;

public static class ApplicationServicesExtension
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<sgspe.Models.Carbon.EmissionFactors>(configuration.GetSection("Emissions"));
        services.AddScoped<sgspe.Services.Carbon.CarbonService>();
        services.AddScoped<sgspe.Services.Materials.MaterialService>();
        services.AddScoped<sgspe.Services.Goals.GoalService>();
        services.AddScoped<sgspe.Services.Reports.ReportService>();
        services.AddScoped<sgspe.Services.Reports.CertificationService>();
        services.AddScoped<sgspe.Services.Notifications.EmailSender>();
        services.AddScoped<sgspe.Services.Notifications.NotificationService>();
        services.AddHostedService<sgspe.Services.Notifications.GoalAlertWorker>();
        services.AddScoped<sgspe.Services.Benchmark.BenchmarkService>();

        services.AddScoped<UserService>();
        services.AddScoped<PasswordHasher>();
        services.AddScoped<TokenService>();
        services.AddScoped<CompanyService>();
        services.AddScoped<EquipmentService>();
        services.AddScoped<EnergyService>();
        services.AddScoped<WaterService>();
        services.AddScoped<WasteService>();
        services.AddScoped<InsightsService>();

        return services;
    }
}