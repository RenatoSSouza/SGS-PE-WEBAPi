using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using sgspe.Data;
using sgspe.Services.Companies;
using sgspe.Services.SustainabilityIndicators;
using sgspe.Services.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SGS-PE",
        Version = "v1",
        Description = "API para gestão de SGS-PE",
    });
});

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<SustainabilityIndicatorService>();
builder.Services.AddScoped<CompanyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SGS-PE v1");
        options.DefaultModelsExpandDepth(-1);
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
