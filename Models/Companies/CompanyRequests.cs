using System.ComponentModel.DataAnnotations;
using sgspe.Models.Enum;   
namespace sgspe.Models.Companies;

public record CreateCompanyRequest
(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, ErrorMessage = "Nome não pode exceder 200 caracteres")]
    string LegalName,
    [StringLength(200, ErrorMessage = "Nome fantasia não pode exceder 200 caracteres")]
    string TradingName,
    [Required(ErrorMessage = "Setor é obrigatório")]
    Sector Sector,
    CompanySize? Size = null
);

public record UpdateCompanyRequest
(
    [StringLength(200, ErrorMessage = "Nome não pode exceder 200 caracteres")]
    string? LegalName,
    [StringLength(200, ErrorMessage = "Nome fantasia não pode exceder 200 caracteres")]
    string? TradingName,
    Sector? Sector,
    CompanySize? Size = null
);

