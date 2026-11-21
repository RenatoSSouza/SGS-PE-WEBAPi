namespace sgspe.Extensions;

public static class CorsExtension
{
    public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static WebApplication UseCorsConfig(this WebApplication app, string policyName)
    {
        app.UseCors(policyName);
        return app;
    }
}