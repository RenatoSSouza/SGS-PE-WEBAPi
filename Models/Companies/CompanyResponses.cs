using sgspe.Models.Enum;

namespace sgspe.Models.Companies;

public record CompanyResponse(
    Guid Id,
    string LegalName,
    string TradingName,
    Sector Sector,
    DateTime CreatedAt,
    bool IsActive,
    CompanySize? Size = null
)
{
    public static CompanyResponse? FromEntity(Company? company)
    {
        if (company is null)
            return null;

        return new CompanyResponse(
            company.Id,
            company.LegalName,
            company.TradingName,
            company.Sector,
            company.CreatedAt,
            company.IsActive,
            company.Size
        );
    }
}