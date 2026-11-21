using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
namespace sgspe.Controllers;
 
using sgspe.Models.Energy;
using sgspe.Services.Energy;
 
[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/energy")]
public class EnergyController : ControllerBase
{
    private readonly EnergyService _service;
 
    public EnergyController(EnergyService service) => _service = service;
 
    private bool CanAccessCompany(Guid companyId)
        => User.IsAdmin() || User.GetCompanyId() == companyId;
 
    [HttpGet]
    public async Task<ActionResult<List<EnergyRecordResponse>>> GetAll(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.GetAllByCompanyAsync(companyId);
        return Ok(items.Select(EnergyRecordResponse.FromEntity).ToList());
    }
 
    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<EnergyRecordResponse>> GetByMonth(Guid companyId, int year, int month)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var item = await _service.GetByMonthAsync(companyId, year, month);
        if (item is null) return NotFound();
        return Ok(EnergyRecordResponse.FromEntity(item));
    }
 
    [HttpPost]
    public async Task<ActionResult<EnergyRecordResponse>> Create(Guid companyId, CreateEnergyRecordRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        try
        {
            var item = await _service.CreateAsync(companyId, request);
            return CreatedAtAction(nameof(GetByMonth), new { companyId, year = item.Year, month = item.Month }, EnergyRecordResponse.FromEntity(item));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
 
    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid companyId, Guid id)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var success = await _service.DeleteAsync(companyId, id);
        if (!success) return NotFound();
        return NoContent();
    }
}