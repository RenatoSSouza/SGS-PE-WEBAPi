using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace sgspe.Extensions;

public static class ControllerExtension
{
    public static IServiceCollection AddControllersConfig(this IServiceCollection services)
    {
        services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                    );
                return new BadRequestObjectResult(new { message = "Validation failed", errors });
            };
        });
        return services;
    }
}