using System.ComponentModel.DataAnnotations;
using sgspe.Models.Enum;

namespace sgspe.Models.Equipments;

public class CreateEquipmentRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; } = EquipmentCategory.Other;
    [Range(0, double.MaxValue)] public double PowerWatts { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    [Range(0, 24)] public double AverageDailyHours { get; set; }
}

public class UpdateEquipmentRequest : CreateEquipmentRequest
{
    public bool IsActive { get; set; } = true;
}

public class EquipmentResponse
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EquipmentCategory Category { get; set; }
    public double PowerWatts { get; set; }
    public int Quantity { get; set; }
    public double AverageDailyHours { get; set; }
    public bool IsActive { get; set; }
    public double EstimatedMonthlyKwh { get; set; }

    public static EquipmentResponse FromEntity(Equipment e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, Name = e.Name, Category = e.Category,
        PowerWatts = e.PowerWatts, Quantity = e.Quantity, AverageDailyHours = e.AverageDailyHours,
        IsActive = e.IsActive, EstimatedMonthlyKwh = e.EstimatedMonthlyKwh(30)
    };
}
