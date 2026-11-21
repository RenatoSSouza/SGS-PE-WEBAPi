using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
namespace sgspe.Controllers;
 
using sgspe.Models.Waste;
using sgspe.Services.Waste;
 
[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/waste")]
public class WasteController : ControllerBase
{
    private readonly WasteService _service;
 
    public WasteController(WasteService service) => _service = service;
 
    private bool CanAccessCompany(Guid companyId)
        => User.IsAdmin() || User.GetCompanyId() == companyId;
 
    [HttpGet]
    public async Task<ActionResult<List<WasteEntryResponse>>> GetAll(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.GetAllByCompanyAsync(companyId);
        return Ok(items.Select(WasteEntryResponse.FromEntity).ToList());
    }
 
    [HttpGet("{year:int}/{month:int}/summary")]
    public async Task<ActionResult<WasteMonthlySummary>> GetMonthlySummary(Guid companyId, int year, int month)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var summary = await _service.GetMonthlySummaryAsync(companyId, year, month);
        return Ok(summary);
    }
 
    [HttpPost]
    public async Task<ActionResult<WasteEntryResponse>> Create(Guid companyId, CreateWasteEntryRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var item = await _service.CreateAsync(companyId, request);
        return CreatedAtAction(nameof(GetAll), new { companyId }, WasteEntryResponse.FromEntity(item));
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