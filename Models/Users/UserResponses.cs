using sgspe.Models.Enum;

namespace sgspe.Models.Users;
public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    Guid CompanyId,
    DateTime CreatedAt,
    DateTime LastLoginAt,
    bool IsActive,
    EnumRole Role
);