using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Materials;
using sgspe.Services.Materials;

namespace sgspe.Controllers;

[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/materials")]
public class MaterialsController : ControllerBase
{
    private readonly MaterialService _service;

    public MaterialsController(MaterialService service) => _service = service;

    private bool CanAccessCompany(Guid companyId)
        => User.IsAdmin() || User.GetCompanyId() == companyId;

    [HttpGet]
    public async Task<ActionResult<List<MaterialEntryResponse>>> GetAll(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.GetAllByCompanyAsync(companyId);
        return Ok(items.Select(MaterialEntryResponse.FromEntity).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<MaterialEntryResponse>> Create(Guid companyId, CreateMaterialEntryRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        try
        {
            var item = await _service.CreateAsync(companyId, request);
            return CreatedAtAction(nameof(GetAll), new { companyId }, MaterialEntryResponse.FromEntity(item));
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
