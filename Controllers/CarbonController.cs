using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sgspe.Extensions;
using sgspe.Models.Carbon;
using sgspe.Services.Carbon;

namespace sgspe.Controllers;

[Authorize]
[ApiController]
[Route("api/companies/{companyId:guid}/carbon")]
public class CarbonController : ControllerBase
{
    private readonly CarbonService _service;

    public CarbonController(CarbonService service) => _service = service;

    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<CarbonFootprint>> GetMonthly(Guid companyId, int year, int month)
    {
        if (!User.IsAdmin() && User.GetCompanyId() != companyId) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { message = "Mês inválido." });
        return Ok(await _service.GetMonthlyAsync(companyId, year, month));
    }
}
