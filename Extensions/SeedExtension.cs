using Microsoft.EntityFrameworkCore;
using sgspe.Data;
using sgspe.Models.Companies;
using sgspe.Models.Enum;
using sgspe.Models.Users;
using sgspe.Services.Users;

namespace sgspe.Extensions;

public static class SeedExtension
{
    /// <summary>
    /// Cria o primeiro administrador quando ainda não existe nenhum.
    /// As credenciais vêm da configuração (Seed:AdminEmail / Seed:AdminPassword), nunca do código.
    /// </summary>
    public static async Task SeedInitialAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
        var config = app.Configuration.GetSection("Seed");
        var logger = app.Logger;

        if (await db.Users.AnyAsync(u => u.Role == EnumRole.Admin)) return;

        var email = config["AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("Nenhum administrador cadastrado e Seed:AdminEmail/Seed:AdminPassword não configurados. " +
                              "Defina-os (ex.: variáveis de ambiente Seed__AdminEmail e Seed__AdminPassword) para criar o primeiro admin.");
            return;
        }
        if (password.Length < 8)
        {
            logger.LogError("Seed:AdminPassword deve ter ao menos 8 caracteres. Administrador inicial não foi criado.");
            return;
        }

        var companyName = config["AdminCompanyName"] ?? "SGS-PE Administração";
        var company = await db.Companies.FirstOrDefaultAsync(c => c.LegalName == companyName);
        if (company is null)
        {
            company = new Company { LegalName = companyName, TradingName = companyName, Sector = Sector.Outros };
            db.Companies.Add(company);
        }

        db.Users.Add(new User
        {
            Name = config["AdminName"] ?? "Administrador",
            Email = email,
            PasswordHash = hasher.HashPassword(password),
            Role = EnumRole.Admin,
            CompanyId = company.Id
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Administrador inicial criado: {Email}", email);
    }
}
