using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Companies;
using sgspe.Models.Enum;
using sgspe.Services.Companies;

namespace sgspe.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetAll()
    {
        var companies = await _companyService.GetAllAsync();
        var response = companies
            .Select(CompanyResponse.FromEntity)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != id) return Forbid();
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
            return NotFound();

        return Ok(CompanyResponse.FromEntity(company));
    }
    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("sector/{sector}")]
    public async Task<ActionResult<List<CompanyResponse>>> GetBySector(Sector sector)
    {
        var companies = await _companyService.GetBySectorAsync(sector);

        if (companies.Count == 0)
            return NotFound("Nenhuma empresa encontrada para este setor");

        var response = companies
            .Select(CompanyResponse.FromEntity)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        return Ok(response);
    }
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request)
    {
        try
        {
            var company = await _companyService.CreateAsync(
                request.LegalName,
                request.TradingName,
                request.Sector,
                request.Size);
            return CreatedAtAction(nameof(GetById), new { id = company.Id }, CompanyResponse.FromEntity(company));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPut("{id}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, UpdateCompanyRequest request)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != id) return Forbid();
        // Setor e porte definem com quem a empresa é comparada no benchmark: só o Administrador altera.
        if (!User.IsAdmin() && (request.Sector is not null || request.Size is not null))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o administrador pode alterar o setor e o porte da empresa." });
        var company = await _companyService.UpdateAsync(
            id,
            request.LegalName,
            request.TradingName,
            request.Sector,
            request.Size);
        if (company == null)
            return NotFound();

        return Ok(CompanyResponse.FromEntity(company));
    }
    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var success = await _companyService.DeleteAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
