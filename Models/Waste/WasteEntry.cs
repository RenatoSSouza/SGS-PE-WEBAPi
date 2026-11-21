using sgspe.Models.Companies;
using sgspe.Models.Enum;

namespace sgspe.Models.Waste;

public class WasteEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public WasteType Type { get; set; }
    public double QuantityKg { get; set; }
    public bool Recycled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
}
