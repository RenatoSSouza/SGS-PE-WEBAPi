using sgspe.Models.Companies;
using sgspe.Models.Enum;

namespace sgspe.Models.Equipments;
public class Equipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; } = EquipmentCategory.Other;
    public double PowerWatts { get; set; }
    public int Quantity { get; set; } = 1;
    public double AverageDailyHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;

    public double EstimatedMonthlyKwh (int daysInMonth)
        => (PowerWatts * AverageDailyHours * Quantity * daysInMonth) / 1000;
}