using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Extensions;
using sgspe.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
.AddJsonFile(builder.Environment.IsDevelopment()
    ? "appsettings.Development.json"
    : "appsettings.Production.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllersConfig();
builder.Services.AddSwaggerConfig();
builder.Services.AddDatabaseConfig(builder.Configuration);
builder.Services.AddJwtAuthConfig(builder.Configuration);
builder.Services.AddCorsConfig(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.InitializeDatabase();

await app.SeedInitialAdminAsync();

app.UseSwaggerConfig(builder.Environment);
app.UseHttpsRedirection();
app.UseCorsConfig("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();