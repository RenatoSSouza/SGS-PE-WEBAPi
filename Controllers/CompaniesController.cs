using Microsoft.AspNetCore.Mvc;
using sgspe.Models.Companies;
using sgspe.Models.Enum;
using sgspe.Services.Companies;

namespace sgspe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

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
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
            return NotFound();

        return Ok(CompanyResponse.FromEntity(company));
    }
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
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request)
    {
        try
        {
            var company = await _companyService.CreateAsync(
                request.LegalName,
                request.TradingName,
                request.Sector);
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

    [HttpPut("{id}")]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, UpdateCompanyRequest request)
    {
        var company = await _companyService.UpdateAsync(
            id,
            request.LegalName,
            request.TradingName,
            request.Sector);
        if (company == null)
            return NotFound();

        return Ok(CompanyResponse.FromEntity(company));
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var success = await _companyService.DeleteAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
