using System.ComponentModel.DataAnnotations;
using sgspe.Models.Companies;

namespace sgspe.Models.Materials;

public class MaterialEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "kg";
    public double Quantity { get; set; }
    public bool RecycledOrRenewable { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
}

public class CreateMaterialEntryRequest
{
    [Range(1, 12)] public int Month { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Unit { get; set; } = "kg";
    [Range(0.001, double.MaxValue)] public double Quantity { get; set; }
    public bool RecycledOrRenewable { get; set; }
}

public class MaterialEntryResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public bool RecycledOrRenewable { get; set; }

    public static MaterialEntryResponse FromEntity(MaterialEntry m) => new()
    {
        Id = m.Id, CompanyId = m.CompanyId, Month = m.Month, Year = m.Year, Name = m.Name,
        Unit = m.Unit, Quantity = m.Quantity, RecycledOrRenewable = m.RecycledOrRenewable
    };
}
