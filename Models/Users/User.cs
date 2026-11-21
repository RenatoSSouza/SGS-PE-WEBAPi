using sgspe.Models.Companies;
using sgspe.Models.Enum;

namespace sgspe.Models.Users;
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid CompanyId { get; set; } = Guid.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public EnumRole Role { get; set; } = EnumRole.Employee;
    public Company? Company { get; set; }
}
