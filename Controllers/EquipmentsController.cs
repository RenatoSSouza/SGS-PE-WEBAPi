using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Equipments;
using sgspe.Services.Equipments;
 
namespace sgspe.Controllers;
 
[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/equipments")]
public class EquipmentsController : ControllerBase
{
    private readonly EquipmentService _service;
 
    public EquipmentsController(EquipmentService service) => _service = service;
 
    private bool CanAccessCompany(Guid companyId)
        => User.IsAdmin() || User.GetCompanyId() == companyId;
 
    [HttpGet]
    public async Task<ActionResult<List<EquipmentResponse>>> GetAll(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.GetAllByCompanyAsync(companyId);
        return Ok(items.Select(EquipmentResponse.FromEntity).ToList());
    }
 
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> GetById(Guid companyId, Guid id)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var item = await _service.GetByIdAsync(id);
        if (item is null || item.CompanyId != companyId) return NotFound();
        return Ok(EquipmentResponse.FromEntity(item));
    }
 
    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<EquipmentResponse>> Create(Guid companyId, CreateEquipmentRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        try
        {
            var item = await _service.CreateAsync(companyId, request);
            return CreatedAtAction(nameof(GetById), new { companyId, id = item.Id }, EquipmentResponse.FromEntity(item));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
 
    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EquipmentResponse>> Update(Guid companyId, Guid id, UpdateEquipmentRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var item = await _service.UpdateAsync(companyId, id, request);
        if (item is null) return NotFound();
        return Ok(EquipmentResponse.FromEntity(item));
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