
using sgspe.Models.Enum;

namespace sgspe.Models.Companies;
public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LegalName { get; set; } = string.Empty;
    public string TradingName { get; set; } = string.Empty;
    public Sector Sector { get; set; } = Sector.Outros;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
