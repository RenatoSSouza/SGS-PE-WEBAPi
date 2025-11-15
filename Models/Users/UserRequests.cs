using System.ComponentModel.DataAnnotations;
using sgspe.Models.Enum;

namespace sgspe.Models.Users;

public record CreateUserRequest(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, ErrorMessage = "Nome não pode exceder 100 caracteres")]
    string Name,
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email em formato inválido")]
    [StringLength(255, ErrorMessage = "Email não pode exceder 255 caracteres")]
    string Email,

    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    [StringLength(100, ErrorMessage = "Senha não pode exceder 100 caracteres")]
    string Password,

    [Required(ErrorMessage = "Role é obrigatória")]
    EnumRole Role,
    [Required(ErrorMessage = "CompanyId é obrigatório")]
    Guid CompanyId
);

public record UpdateUserRequest(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, ErrorMessage = "Nome não pode exceder 100 caracteres")]
    string Name
);

public record UpdateUserCompanyRequest(
    [Required(ErrorMessage = "CompanyId é obrigatório")]
    Guid CompanyId
);
public record LoginUserRequest(
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email em formato inválido")]
    string Email,

    [Required(ErrorMessage = "Senha é obrigatória")]
    string Password
);

