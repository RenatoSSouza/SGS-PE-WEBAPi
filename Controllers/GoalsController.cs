using sgspe.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Goals;
using sgspe.Services.Goals;

namespace sgspe.Controllers;

[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/goals")]
public class GoalsController : ControllerBase
{
    private readonly GoalService _service;

    public GoalsController(GoalService service) => _service = service;

    private bool CanAccessCompany(Guid companyId)
        => User.IsAdmin() || User.GetCompanyId() == companyId;

    [HttpGet]
    public async Task<ActionResult<List<GoalResponse>>> GetAll(Guid companyId)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var items = await _service.GetAllByCompanyAsync(companyId);
        return Ok(items.Select(GoalResponse.FromEntity).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GoalResponse>> GetById(Guid companyId, Guid id)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        var item = await _service.GetByIdAsync(companyId, id);
        return item is null ? NotFound() : Ok(GoalResponse.FromEntity(item));
    }

    [HttpGet("{id:guid}/progress/{year:int}/{month:int}")]
    public async Task<ActionResult<GoalProgress>> GetProgress(Guid companyId, Guid id, int year, int month)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { message = "Mês inválido." });
        var progress = await _service.GetProgressAsync(companyId, id, year, month);
        return progress is null ? NotFound() : Ok(progress);
    }

    [HttpGet("alerts/{year:int}/{month:int}")]
    public async Task<ActionResult<List<GoalProgress>>> GetAlerts(Guid companyId, int year, int month)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { message = "Mês inválido." });
        return Ok(await _service.GetAlertsAsync(companyId, year, month));
    }

    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<GoalResponse>> Create(Guid companyId, CreateGoalRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        try
        {
            var item = await _service.CreateAsync(companyId, request);
            return CreatedAtAction(nameof(GetById), new { companyId, id = item.Id }, GoalResponse.FromEntity(item));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GoalResponse>> Update(Guid companyId, Guid id, UpdateGoalRequest request)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        try
        {
            var item = await _service.UpdateAsync(companyId, id, request);
            return item is null ? NotFound() : Ok(GoalResponse.FromEntity(item));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = AppRoles.ManagerOrAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid companyId, Guid id)
    {
        if (!CanAccessCompany(companyId)) return Forbid();
        return await _service.DeleteAsync(companyId, id) ? NoContent() : NotFound();
    }
}
