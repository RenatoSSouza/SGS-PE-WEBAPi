using Microsoft.OpenApi;

namespace sgspe.Extensions;
public static class SwaggerExtension
{
    public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SGS-PE",
                Version = "v1",
                Description = "API para gestão de sustentabilidade para pequenas empresas"
            });
 
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe apenas o token JWT, sem o prefixo Bearer."
            });
 
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
        });
 
        return services;
    }
 
    public static WebApplication UseSwaggerConfig(this WebApplication app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SGS-PE v1");
                options.DisplayRequestDuration();
                options.DefaultModelsExpandDepth(-1);
            });
        }
 
        return app;
    }
}